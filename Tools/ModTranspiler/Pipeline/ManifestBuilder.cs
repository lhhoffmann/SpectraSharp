using System.Text.RegularExpressions;
using SpectraSharp.ModTranspiler.Mappings;
using SpectraSharp.ModTranspiler.Model;

namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 3 — Parses decompiled Java source and builds a ModManifest.
/// Handles two patterns common to all 1.0-era ModLoader mods:
///   1. Dedicated block/item class files (extends yy / extends sr / extends acy)
///   2. BaseMod entry-point files (mod_*.java) containing inline item and recipe declarations
/// </summary>
static class ManifestBuilder
{
    // Obfuscated vanilla class names for 1.0
    static readonly HashSet<string> ItemBaseClasses = ["sr", "acy", "acx", "acz", "aco"];
    static readonly HashSet<string> BlockBaseClasses = ["yy"];
    static readonly HashSet<string> EntityBaseClasses = ["aef", "vm", "aam"];

    public static ModManifest Run(
        string modName,
        string decompiledDir,
        Dictionary<string, ClassTag> tags)
    {
        Console.WriteLine("[ManifestBuilder] Building manifest...");

        var manifest = new ModManifest { ModName = modName };

        // Collect all mod-defined class names for transitive inheritance resolution
        var modClasses = tags
            .Where(kv => kv.Value == ClassTag.NewContent)
            .Select(kv => kv.Key.Contains('.') ? kv.Key[(kv.Key.LastIndexOf('.') + 1)..] : kv.Key)
            .ToHashSet();

        // Build a map of all Java sources for superclass resolution
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (className, tag) in tags)
        {
            if (tag is ClassTag.Library or ClassTag.Passthrough) continue;
            string shortName = ShortName(className);
            string path = Path.Combine(decompiledDir, shortName + ".java");
            if (File.Exists(path))
                sources[shortName] = File.ReadAllText(path);
        }

        // Pass 1 — Collect item field declarations from BaseMod files
        // (BaseMod files declare items as static fields, needed to resolve recipe IDs)
        var fieldToItemId = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (name, src) in sources)
        {
            if (IsBaseMod(src))
                CollectItemFields(src, fieldToItemId);
        }

        // Pass 2 — Process all classes
        foreach (var (name, src) in sources)
        {
            string? super = ExtractSuperClass(src);

            if (IsBaseMod(src))
            {
                ParseBaseMod(manifest, name, src, fieldToItemId);
            }
            else if (super != null && BlockBaseClasses.Contains(super))
            {
                manifest.NewBlocks.Add(ParseBlock(name, src));
            }
            else if (super != null && (ItemBaseClasses.Contains(super) || IsIndirectItem(super, sources, modClasses)))
            {
                manifest.NewItems.Add(ParseItem(name, src));
            }
            else if (super != null && EntityBaseClasses.Contains(super))
            {
                manifest.NewEntities.Add(ParseEntity(name, src));
            }
            else if (tags.TryGetValue(name, out var tag) && tag == ClassTag.Override)
            {
                ParseOverride(manifest, name, src);
            }
        }

        Console.WriteLine($"[ManifestBuilder] {manifest.NewBlocks.Count} blocks, " +
                          $"{manifest.NewItems.Count} items, " +
                          $"{manifest.NewRecipes.Count} recipes, " +
                          $"{manifest.Overrides.Count} overrides, " +
                          $"{manifest.WorldGenHooks.Count} worldgen hooks.");
        return manifest;
    }

    // ── BaseMod Entry-Point Parsing ───────────────────────────────────────────

    static bool IsBaseMod(string source) =>
        Regex.IsMatch(source, @"class\s+\w+\s+extends\s+BaseMod");

    /// <summary>
    /// Collects block and item field declarations from a BaseMod class.
    /// Patterns:
    ///   static final acy FieldName = new SomeClass(itemId, ...)  → item
    ///   static final yy  FieldName = new SomeBlock(blockId, ...) → block
    ///   static int XxxID = 125;                                   → int constant
    /// Builds a unified field name → ID map for recipe and WorldGen body resolution.
    /// </summary>
    static void CollectItemFields(string source, Dictionary<string, int> fieldToItemId)
    {
        // Collect named int constants first (e.g. static int BlockCopperVeinID = 125;)
        var constants = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(source,
            @"static\s+(?:final\s+)?int\s+(\w+)\s*=\s*(\d+)"))
        {
            constants.TryAdd(m.Groups[1].Value, int.Parse(m.Groups[2].Value));
        }

        // Item fields: static final acy FieldName = new SomeClass(id, ...)
        foreach (Match m in Regex.Matches(source,
            @"static\s+final\s+acy\s+(\w+)\s*=\s*new\s+\w+\s*\(\s*(\w+)"))
        {
            string fieldName = m.Groups[1].Value;
            string rawId     = m.Groups[2].Value;
            int id = int.TryParse(rawId, out int direct) ? direct
                   : constants.TryGetValue(rawId, out int cval) ? cval : -1;
            if (id >= 0) fieldToItemId.TryAdd(fieldName, id);
        }

        // Block fields: static final yy FieldName = new SomeBlock(id, ...)
        foreach (Match m in Regex.Matches(source,
            @"static\s+final\s+yy\s+(\w+)\s*=\s*new\s+\w+\s*\(\s*(\w+)"))
        {
            string fieldName = m.Groups[1].Value;
            string rawId     = m.Groups[2].Value;
            int id = int.TryParse(rawId, out int direct) ? direct
                   : constants.TryGetValue(rawId, out int cval) ? cval : -1;
            if (id >= 0) fieldToItemId.TryAdd(fieldName, id);
        }
    }

    static void ParseBaseMod(ModManifest manifest, string className, string source,
        Dictionary<string, int> fieldToItemId)
    {
        // Extract inline item declarations
        foreach (Match m in Regex.Matches(source,
            @"static\s+final\s+acy\s+(\w+)\s*=\s*new\s+(\w+)\s*\(\s*(\d+)[^)]*\)([^;]*)"))
        {
            string fieldName   = m.Groups[1].Value;
            string ctorClass   = m.Groups[2].Value;
            int    itemId      = int.Parse(m.Groups[3].Value);
            string chain       = m.Groups[4].Value;

            var item = new ItemDescriptor
            {
                ClassName        = fieldName,
                ItemId           = itemId,
                UnlocalizedName  = ExtractNameFromChain(chain) ?? fieldName,
            };

            // Texture coords from .a(row, col) on InfiTexture / similar
            var tex = Regex.Match(chain, @"\.a\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)");
            if (tex.Success)
                item.TextureIndex = int.Parse(tex.Groups[1].Value) * 16 + int.Parse(tex.Groups[2].Value);

            manifest.NewItems.Add(item);
        }

        // Extract ModLoader recipes
        ParseModLoaderRecipes(manifest, source, fieldToItemId);

        // Extract WorldGen from GenerateSurface method
        var wgLines = ExtractMethodBody(source, "GenerateSurface") ??
                      ExtractMethodBody(source, "generateSurface");
        if (wgLines != null && wgLines.Count > 0)
        {
            var wg = new WorldGenDescriptor { ClassName = className };

            // Resolve FieldName.bM references to integer literals in the body
            // so the WorldGen class doesn't need access to BaseMod static fields
            var resolvedLines = wgLines.Select(line =>
            {
                foreach (Match fm in Regex.Matches(line, @"(\w+)\.bM"))
                {
                    string fieldName = fm.Groups[1].Value;
                    if (fieldToItemId.TryGetValue(fieldName, out int resolvedId))
                        line = line.Replace(fm.Value, resolvedId.ToString());
                }
                return line;
            }).ToList();
            wg.JavaLines.AddRange(resolvedLines);

            // Capture instance int/float fields (e.g. "int CopperVeinCount = 18;")
            // so WorldGenTemplate can emit them as class fields with default values
            foreach (Match fm in Regex.Matches(source,
                @"(?<!static\s)(?:int|float|double)\s+(\w+)\s*=\s*([\d.]+)"))
            {
                wg.Fields.TryAdd(fm.Groups[1].Value, fm.Groups[2].Value);
            }

            manifest.WorldGenHooks.Add(wg);
        }
    }

    /// <summary>
    /// Extracts ModLoader.AddRecipe / AddShapelessRecipe / AddSmelting calls.
    /// Resolves field-name references to item IDs using the collected field map.
    /// </summary>
    static void ParseModLoaderRecipes(ModManifest manifest, string source,
        Dictionary<string, int> fieldToItemId)
    {
        // Smelting: ModLoader.AddSmelting(inputField.bM, new dk(outputField, count))
        // Also handles numeric IDs directly
        foreach (Match m in Regex.Matches(source,
            @"ModLoader\.AddSmelting\s*\(\s*([\w.]+)\s*,\s*new\s+dk\s*\(\s*([\w]+)\s*,\s*(\d+)"))
        {
            int? inputId  = ResolveItemRef(m.Groups[1].Value, fieldToItemId);
            int? outputId = ResolveItemRef(m.Groups[2].Value, fieldToItemId);
            if (inputId == null || outputId == null) continue;

            manifest.NewRecipes.Add(new RecipeDescriptor
            {
                Type         = RecipeType.Smelting,
                SmeltInputId = inputId.Value,
                OutputId     = outputId.Value,
                OutputCount  = int.Parse(m.Groups[3].Value),
                SmeltXp      = 0.7f, // standard 1.0 ore xp
            });
        }

        // Shapeless: ModLoader.AddShapelessRecipe(new dk(outputField, count), new Object[]{ing1, ing2})
        foreach (Match m in Regex.Matches(source,
            @"ModLoader\.AddShapelessRecipe\s*\(\s*new\s+dk\s*\(\s*([\w]+)\s*,\s*(\d+)[^)]*\)\s*,\s*new Object\[\]\s*\{([^}]+)\}"))
        {
            int? outputId = ResolveItemRef(m.Groups[1].Value, fieldToItemId);
            if (outputId == null) continue;

            var ingredientIds = m.Groups[3].Value
                .Split(',')
                .Select(s => ResolveItemRef(s.Trim(), fieldToItemId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (ingredientIds.Count == 0) continue;

            var recipe = new RecipeDescriptor
            {
                Type        = RecipeType.Shapeless,
                OutputId    = outputId.Value,
                OutputCount = int.Parse(m.Groups[2].Value),
            };
            recipe.ShapelessIds.AddRange(ingredientIds);
            manifest.NewRecipes.Add(recipe);
        }

        // Shaped: ModLoader.AddRecipe(new dk(outputField, count), new Object[]{pattern lines, mappings})
        foreach (Match m in Regex.Matches(source,
            @"ModLoader\.AddRecipe\s*\(\s*new\s+dk\s*\(\s*([\w]+)\s*,\s*(\d+)[^)]*\)\s*,\s*new Object\[\]\s*\{([^}]+)\}"))
        {
            int? outputId = ResolveItemRef(m.Groups[1].Value, fieldToItemId);
            if (outputId == null) continue;

            string objArray = m.Groups[3].Value;

            // Extract pattern strings ("XXX", "X X", etc.)
            var patterns = Regex.Matches(objArray, @"""([^""]{1,3})""")
                .Select(p => p.Groups[1].Value)
                .ToList();

            var recipe = new RecipeDescriptor
            {
                Type        = RecipeType.Shaped,
                OutputId    = outputId.Value,
                OutputCount = int.Parse(m.Groups[2].Value),
                Pattern     = [.. patterns],
            };

            // Extract char→item mappings ('X', someField)
            foreach (Match ing in Regex.Matches(objArray, @"'(\w)'\s*,\s*([\w.]+)"))
            {
                int? ingId = ResolveItemRef(ing.Groups[2].Value, fieldToItemId);
                if (ingId.HasValue)
                    recipe.Ingredients[ing.Groups[1].Value[0]] = ingId.Value;
            }
            manifest.NewRecipes.Add(recipe);
        }
    }

    /// <summary>
    /// Resolves a Java item reference (field name, field.bM, or numeric literal) to an item ID.
    /// </summary>
    static int? ResolveItemRef(string token, Dictionary<string, int> fieldToItemId)
    {
        token = token.Trim();

        // Numeric literal
        if (int.TryParse(token, out int id)) return id;

        // field.bM → strip .bM suffix (bM = itemID field in 1.0 ItemStack)
        string field = token.Contains('.') ? token[..token.IndexOf('.')] : token;

        return fieldToItemId.TryGetValue(field, out int fid) ? fid : null;
    }

    static string? ExtractNameFromChain(string chain)
    {
        var m = Regex.Match(chain, @"\.a\s*\(\s*""([^""]+)""\s*\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    // ── Dedicated Class Parsing ───────────────────────────────────────────────

    static BlockDescriptor ParseBlock(string className, string source)
    {
        var block = new BlockDescriptor { ClassName = className };

        var idMatch = Regex.Match(source, @"super\s*\(\s*(\d+)");
        if (idMatch.Success) block.BlockId = int.Parse(idMatch.Groups[1].Value);

        var texMatch = Regex.Match(source, @"super\s*\(\s*\d+\s*,\s*(\d+)");
        if (texMatch.Success) block.TextureIndex = int.Parse(texMatch.Groups[1].Value);

        var hardMatch = Regex.Match(source, @"\.c\s*\(\s*([\d.]+)[Ff]?\s*\)");
        if (hardMatch.Success) block.Hardness = float.Parse(hardMatch.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        var resMatch = Regex.Match(source, @"\.b\s*\(\s*([\d.]+)[Ff]?\s*\)");
        if (resMatch.Success) block.BlastResistance = float.Parse(resMatch.Groups[1].Value,
            System.Globalization.CultureInfo.InvariantCulture);

        var nameMatches = Regex.Matches(source, @"\.a\s*\(\s*""([^""]+)""\s*\)");
        if (nameMatches.Count > 0)
            block.UnlocalizedName = nameMatches[^1].Groups[1].Value;

        block.IsRandomTick = source.Contains(".l()") || source.Contains("randomDisplayTick");
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

        var dmgMatch = Regex.Match(source, @"setMaxDamage\s*\(\s*(\d+)|\.i\s*\(\s*(\d+)");
        if (dmgMatch.Success)
            item.MaxDamage = int.Parse(dmgMatch.Groups[1].Success
                ? dmgMatch.Groups[1].Value : dmgMatch.Groups[2].Value);

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

    // ── Override Parsing ─────────────────────────────────────────────────────

    static void ParseOverride(ModManifest manifest, string className, string source)
    {
        string csClass = VanillaClassList.ToHumanName(className);

        foreach (Match m in Regex.Matches(source,
            @"@Override\s+\w[\w\s<>]*?\s+(\w+)\s*\(([^)]*)\)\s*\{"))
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

    static string ShortName(string className)
    {
        string s = className.Contains('.') ? className[(className.LastIndexOf('.') + 1)..] : className;
        return s.Contains('$') ? s[..s.IndexOf('$')] : s;
    }

    static string? ExtractSuperClass(string source)
    {
        var m = Regex.Match(source, @"class\s+\w+\s+extends\s+(\w+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// Returns true if the given superclass is itself (directly or transitively) an item class.
    /// Handles mod-defined tool base classes like MetallurgyItemTool extends acy.
    /// </summary>
    static bool IsIndirectItem(string superClass, Dictionary<string, string> sources, HashSet<string> modClasses)
    {
        if (!modClasses.Contains(superClass)) return false;
        if (!sources.TryGetValue(superClass, out string? parentSrc)) return false;

        string? grandParent = ExtractSuperClass(parentSrc);
        if (grandParent == null) return false;
        if (ItemBaseClasses.Contains(grandParent)) return true;

        // One more level (e.g. A extends B extends C extends acy)
        if (modClasses.Contains(grandParent) && sources.TryGetValue(grandParent, out string? gpSrc))
        {
            string? ggp = ExtractSuperClass(gpSrc);
            return ggp != null && ItemBaseClasses.Contains(ggp);
        }

        return false;
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

        int depth = 0, i = braceStart;
        while (i < source.Length)
        {
            if      (source[i] == '{') depth++;
            else if (source[i] == '}') { depth--; if (depth == 0) break; }
            i++;
        }

        return source[(braceStart + 1)..i]
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
    }

    static string MapMethodName(string java) => java switch
    {
        "updateTick"               => "BlockTick",
        "randomDisplayTick"        => "BlockTick",
        "onBlockActivated"         => "OnUse",
        "onBlockClicked"           => "OnClick",
        "quantityDropped"          => "GetDropCount",
        "idDropped"                => "GetDropId",
        "onEntityCollidedWithBlock" => "OnEntityCollide",
        "onNeighborBlockChange"    => "OnNeighborChange",
        "onItemUse"                => "OnUseOnBlock",
        "onItemUseFirst"           => "OnUseOnBlock",
        _                          => java,
    };
}
