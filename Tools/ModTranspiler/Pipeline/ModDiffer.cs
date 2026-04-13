using System.IO.Compression;
using SpectraSharp.ModTranspiler.Mappings;

namespace SpectraSharp.ModTranspiler.Pipeline;

enum ClassTag { NewContent, Override, Passthrough, Library }

/// <summary>
/// Phase 2 — Compares every class in the mod JAR against the vanilla class list.
/// Tags each class as NewContent / Override / Passthrough / Library.
/// Does NOT decompile — operates on the raw .class entries in the ZIP.
/// </summary>
static class ModDiffer
{
    public static Dictionary<string, ClassTag> Run(string jarPath, string decompiledDir)
    {
        Console.WriteLine("[ModDiffer] Classifying mod classes...");

        var result = new Dictionary<string, ClassTag>();

        using var zip = ZipFile.OpenRead(jarPath);

        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".class", StringComparison.Ordinal))
                continue;

            // Convert ZIP path to class name: com/example/Foo.class → com.example.Foo
            string className = entry.FullName
                .Replace('/', '.')
                .Replace('\\', '.')
                [..^6]; // strip .class

            // Strip inner class suffixes for lookup: Foo$Bar → Foo
            string lookupName = className.Contains('$')
                ? className[..className.IndexOf('$')]
                : className;

            if (VanillaClassList.IsLibraryPrefix(className))
            {
                result[className] = ClassTag.Library;
                continue;
            }

            if (!VanillaClassList.Contains(lookupName))
            {
                result[className] = ClassTag.NewContent;
                continue;
            }

            // Vanilla class exists — check if decompiled source exists and differs
            string javaFile = Path.Combine(decompiledDir, entry.FullName.Replace(".class", ".java"));
            result[className] = File.Exists(javaFile)
                ? ClassTag.Override
                : ClassTag.Passthrough;
        }

        int newContent = result.Values.Count(t => t == ClassTag.NewContent);
        int overrides  = result.Values.Count(t => t == ClassTag.Override);
        int libraries  = result.Values.Count(t => t == ClassTag.Library);

        Console.WriteLine($"[ModDiffer] {newContent} new, {overrides} overrides, {libraries} library classes.");
        return result;
    }
}
