using System.Text.RegularExpressions;
using SpectraSharp.ModTranspiler.Mappings;
using SpectraSharp.ModTranspiler.Model;

namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 3 — Parses decompiled Java source and builds a ModManifest.
/// Uses regex-based extraction on Vineflower output (readable, structured Java).
/// Handles the specific patterns of 1.0-era mods reliably without a full ANTLR4 grammar.
/// </summary>
static class ManifestBuilder
{
    public static ModManifest Run(
        string modName,
        string decompiledDir,
        Dictionary<string, ClassTag> tags)
    {
        Console.WriteLine("[ManifestBuilder] Building manifest...");

        var manifest = new ModManifest { ModName = modName };

        foreach (var (className, tag) in tags)
        {
            if (tag is ClassTag.Library or ClassTag.Passthrough)
                continue;

            string shortName = className.Contains('.')
                ? className[(className.LastIndexOf('.') + 1)..]
                : className;
            shortName = shortName.Contains('$') ? shortName[..shortName.IndexOf('$')] : shortName;

            string javaPath = Path.Combine(decompiledDir, shortName + ".java");
            if (!File.Exists(javaPath))
                continue;

            string source = File.ReadAllText(javaPath);

            if (tag == ClassTag.NewContent)
                ParseNewContent(manifest, shortName, source);
            else if (tag == ClassTag.Override)
                ParseOverride(manifest, shortName, source);
        }

        Console.WriteLine($"[ManifestBuilder] {manifest.NewBlocks.Count} blocks, " +
                          $"{manifest.NewItems.Count} items, " +
                          $"{manifest.Overrides.Count} overrides.");
        return manifest;
    }

    // ── NEW CONTENT ──────────────────────────────────────────────────────────

    static void ParseNewContent(ModManifest manifest, string className, string source)
    {
        string? superClass = ExtractSuperClass(source);

        switch (superClass)
        {
            case "yy": // Block
                manifest.NewBlocks.Add(ParseBlock(className, source));
                break;
            case "sr": // Item
                manifest.NewItems.Add(ParseItem(className, source));
                break;
            case "aef": // Entity
            case "vm":
                manifest.NewEntities.Add(ParseEntity(className, source));
                break;
            default:
                // Check for world gen hooks (generateSurface method)
                if (source.Contains("generateSurface") || source.Contains("generate("))
                    manifest.WorldGenHooks.Add(ParseWorldGen(className, source));
                break;
        }

        // Always check for recipe registrations regardless of class type
        ParseRecipes(manifest, source);
    }

    static BlockDescriptor ParseBlock(string className, string source)
    {
        var block = new BlockDescriptor { ClassName = className };

        // Block ID: super(id, ...) or new yy(id, ...)
        var idMatch = Regex.Match(source, @"super\s*\(\s*(\d+)");
        if (idMatch.Success) block.BlockId = int.Parse(idMatch.Groups[1].Value);

        // Texture index: second int in super() call
        var texMatch = Regex.Match(source, @"super\s*\(\s*\d+\s*,\s*(\d+)");
        if (texMatch.Success) block.TextureIndex = int.Parse(texMatch.Groups[1].Value);

        // Hardness: .c(1.5F) or setHardness(1.5F)
        var hardMatch = Regex.Match(source, @"\.c\s*\(\s*([\d.]+)[Ff]?\s*\)");
        if (hardMatch.Success) block.Hardness = float.Parse(hardMatch.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        // Blast resistance: .b(10.0F)
        var resMatch = Regex.Match(source, @"\.b\s*\(\s*([\d.]+)[Ff]?\s*\)");
        if (resMatch.Success) block.BlastResistance = float.Parse(resMatch.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        // Unlocalized name: .a("stone") — last string call in constructor chain
        var nameMatches = Regex.Matches(source, @"\.a\s*\(\s*""([^""]+)""\s*\)");
        if (nameMatches.Count > 0)
            block.UnlocalizedName = nameMatches[^1].Groups[1].Value;

        // Light emission: .a(0.9375F)
        var lightMatch = Regex.Match(source, @"\.a\s*\(\s*(0\.\d+)[Ff]?\s*\)");
        if (lightMatch.Success) block.LightEmission = float.Parse(lightMatch.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        // Random tick: .l() suffix or randomTick override
        block.IsRandomTick = source.Contains(".l()") || source.Contains("randomDisplayTick");

        // Capture method bodies for translation
        block.Methods.AddRange(ExtractMethods(source));

        return block;
    }

    static ItemDescriptor ParseItem(string className, string source)
    {
        var item = new ItemDescriptor { ClassName = className };

        var idMatch = Regex.Match(source, @"super\s*\(\s*(\d+)");
        if (idMatch.Success) item.ItemId = int.Parse(idMatch.Groups[1].Value);

        var texMatch = Regex.Match(source, @"this\.iconIndex\s*=\s*(\d+)");
        if (texMatch.Success) item.TextureIndex = int.Parse(texMatch.Groups[1].Value);

        var stackMatch = Regex.Match(source, @"setMaxStackSize\s*\(\s*(\d+)");
        if (stackMatch.Success) item.MaxStackSize = int.Parse(stackMatch.Groups[1].Value);

        var dmgMatch = Regex.Match(source, @"setMaxDamage\s*\(\s*(\d+)");
        if (dmgMatch.Success) item.MaxDamage = int.Parse(dmgMatch.Groups[1].Value);

        var nameMatches = Regex.Matches(source, @"\.a\s*\(\s*""([^""]+)""\s*\)");
        if (nameMatches.Count > 0)
            item.UnlocalizedName = nameMatches[^1].Groups[1].Value;

        item.Methods.AddRange(ExtractMethods(source));
        return item;
    }

    static EntityDescriptor ParseEntity(string className, string source)
    {
        var entity = new EntityDescriptor { ClassName = className };
        entity.Methods.AddRange(ExtractMethods(source));
        return entity;
    }

    static WorldGenDescriptor ParseWorldGen(string className, string source)
    {
        var wg = new WorldGenDescriptor { ClassName = className };
        var lines = ExtractMethodBody(source, "generateSurface") ??
                    ExtractMethodBody(source, "generate");
        if (lines != null) wg.JavaLines.AddRange(lines);
        return wg;
    }

    static void ParseRecipes(ModManifest manifest, string source)
    {
        // Shaped recipes: addRecipe(new ItemStack(id, count), "AAA", "BBB", ...)
        foreach (Match m in Regex.Matches(source,
            @"addRecipe\s*\(\s*new\s+ItemStack\s*\(\s*(\d+)\s*,\s*(\d+)"))
        {
            manifest.NewRecipes.Add(new RecipeDescriptor
            {
                Type        = RecipeType.Shaped,
                OutputId    = int.Parse(m.Groups[1].Value),
                OutputCount = int.Parse(m.Groups[2].Value),
            });
        }

        // Smelting: addSmelting(inputId, new ItemStack(outputId, 1), xp)
        foreach (Match m in Regex.Matches(source,
            @"addSmelting\s*\(\s*(\d+)\s*,\s*new\s+ItemStack\s*\(\s*(\d+).*?([\d.]+)[Ff]?\s*\)"))
        {
            manifest.NewRecipes.Add(new RecipeDescriptor
            {
                Type        = RecipeType.Smelting,
                SmeltInputId = int.Parse(m.Groups[1].Value),
                OutputId    = int.Parse(m.Groups[2].Value),
                SmeltXp     = float.TryParse(m.Groups[3].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float xp) ? xp : 0f,
            });
        }
    }

    // ── OVERRIDES ────────────────────────────────────────────────────────────

    static void ParseOverride(ModManifest manifest, string className, string source)
    {
        string csClass = VanillaClassList.ToHumanName(className);

        // Find all @Override methods
        foreach (Match m in Regex.Matches(source, @"@Override\s+\w[\w\s<>]*?\s+(\w+)\s*\(([^)]*)\)\s*\{"))
        {
            string methodName = m.Groups[1].Value;
            string csMethod   = MapMethodName(methodName);

            var lines = ExtractMethodBody(source, methodName);
            if (lines == null || lines.Count == 0) continue;

            var inj = new InjectionDescriptor
            {
                TargetClass    = className,
                TargetClassCs  = csClass,
                TargetMethod   = methodName,
                TargetMethodCs = csMethod,
                Position       = InjectionPosition.Append,
            };
            inj.JavaLines.AddRange(lines);
            manifest.Overrides.Add(inj);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static string? ExtractSuperClass(string source)
    {
        var m = Regex.Match(source, @"class\s+\w+\s+extends\s+(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    static List<MethodBody> ExtractMethods(string source)
    {
        var methods = new List<MethodBody>();
        foreach (Match m in Regex.Matches(source,
            @"(?:public|protected|private)\s+[\w<>\[\]]+\s+(\w+)\s*\(([^)]*)\)\s*\{"))
        {
            var body = ExtractMethodBody(source, m.Groups[1].Value);
            if (body == null) continue;
            var mb = new MethodBody { Name = m.Groups[1].Value };
            mb.JavaLines.AddRange(body);
            methods.Add(mb);
        }
        return methods;
    }

    static List<string>? ExtractMethodBody(string source, string methodName)
    {
        int idx = source.IndexOf(methodName + "(", StringComparison.Ordinal);
        if (idx < 0) return null;

        int braceStart = source.IndexOf('{', idx);
        if (braceStart < 0) return null;

        int depth = 0;
        int i     = braceStart;
        while (i < source.Length)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[(braceStart + 1)..i]
                        .Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0)
                        .ToList();
            }
            i++;
        }
        return null;
    }

    static string MapMethodName(string java) => java switch
    {
        "updateTick"      => "BlockTick",
        "randomDisplayTick" => "BlockTick",
        "onBlockActivated" => "OnUse",
        "onBlockClicked"  => "OnClick",
        "quantityDropped" => "GetDropCount",
        "idDropped"       => "GetDropId",
        "onEntityCollidedWithBlock" => "OnEntityCollide",
        "onNeighborBlockChange" => "OnNeighborChange",
        _                 => java,
    };
}
