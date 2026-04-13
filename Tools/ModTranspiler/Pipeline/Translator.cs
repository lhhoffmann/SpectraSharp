using System.Text.RegularExpressions;
using SpectraSharp.ModTranspiler.Mappings;
using SpectraSharp.ModTranspiler.Model;
using SpectraSharp.ModTranspiler.Templates;

namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 4 — Converts a ModManifest into a list of (filename, C# source) pairs.
/// Delegates to Templates for structure; applies VanillaApiMap for method bodies.
/// </summary>
static class Translator
{
    public static List<(string File, string Source)> Run(ModManifest manifest)
    {
        Console.WriteLine("[Translator] Generating C# source...");

        var output = new List<(string, string)>();

        output.Add(EntryPointTemplate.Emit(manifest));

        foreach (var block in manifest.NewBlocks)
            output.Add(BlockTemplate.Emit(manifest.ModName, block));

        foreach (var item in manifest.NewItems)
            output.Add(ItemTemplate.Emit(manifest.ModName, item));

        foreach (var hook in manifest.Overrides)
            output.Add(HookTemplate.Emit(manifest.ModName, hook));

        if (manifest.NewRecipes.Count > 0)
            output.Add(RecipeTemplate.Emit(manifest.ModName, manifest.NewRecipes));

        Console.WriteLine($"[Translator] {output.Count} source files generated.");
        return output;
    }

    // ── Body translation (used by Templates) ────────────────────────────────

    /// <summary>
    /// Translates a list of Java source lines to C#.
    /// Applies VanillaApiMap field substitutions and method call rewrites.
    /// Unknown constructs become // TODO comments.
    /// </summary>
    public static string TranslateBody(IEnumerable<string> javaLines, int indent = 3)
    {
        var sb = new System.Text.StringBuilder();
        string pad = new(' ', indent * 4);

        foreach (string raw in javaLines)
        {
            string line = VanillaApiMap.TranslateLine(raw);
            line = RewriteMethodCalls(line);
            line = RewriteTypes(line);
            line = RewriteControlFlow(line);

            // If still contains Java-isms, wrap in TODO
            if (HasUnknownJavaPattern(line))
            {
                sb.AppendLine($"{pad}// TODO: MANUAL REVIEW REQUIRED — {line.Trim()}");
                continue;
            }

            sb.AppendLine($"{pad}{line}");
        }

        return sb.ToString();
    }

    static string RewriteMethodCalls(string line)
    {
        foreach (var (javaPattern, csPattern) in VanillaApiMap.MethodCalls)
        {
            // Convert '?' wildcards to regex capture groups
            string regexStr = "(?<!" + @"\w)" +
                Regex.Escape(javaPattern).Replace(@"\?", @"([^,)]+)");

            var m = Regex.Match(line, regexStr);
            if (!m.Success) continue;

            string replacement = csPattern;
            for (int i = 1; i < m.Groups.Count; i++)
                replacement = replacement.ReplaceFirst("?", m.Groups[i].Value.Trim());

            line = line[..m.Index] + replacement + line[(m.Index + m.Length)..];
        }
        return line;
    }

    static string RewriteTypes(string line)
    {
        // Cast patterns: (int) → (int) unchanged, (boolean) → (bool)
        line = Regex.Replace(line, @"\(boolean\)", "(bool)");
        line = Regex.Replace(line, @"\(String\)",  "(string)");
        line = line.Replace("true == ", "").Replace("false == !", "");
        // Java ternary is same as C# — no change needed
        return line;
    }

    static string RewriteControlFlow(string line)
    {
        // Java enhanced for: for (Type x : collection) → foreach (Type x in collection)
        line = Regex.Replace(line,
            @"for\s*\(\s*([\w<>\[\]]+)\s+(\w+)\s*:\s*([^)]+)\)",
            "foreach ($1 $2 in $3)");

        // instanceof → is
        line = Regex.Replace(line, @"(\w+)\s+instanceof\s+(\w+)", "$1 is $2");

        return line;
    }

    static bool HasUnknownJavaPattern(string line) =>
        line.Contains("new ") && line.Contains("(") && !line.Contains("new ItemStack") ||
        line.Contains(".getClass()") ||
        line.Contains("synchronized") ||
        line.Contains("throws ") ||
        line.Contains("import ");
}

static class StringExtensions
{
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search, StringComparison.Ordinal);
        return pos < 0 ? text : text[..pos] + replace + text[(pos + search.Length)..];
    }
}
