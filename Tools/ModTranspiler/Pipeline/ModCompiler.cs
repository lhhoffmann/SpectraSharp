using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SpectraSharp.Core.Mods;
using HarmonyLib;

namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 6 — Compiles the generated C# source files to a DLL using Roslyn in-process.
/// No dotnet CLI required.
/// </summary>
static class ModCompiler
{
    public static bool Run(string modName, string sourceDir, string outputDir, out string dllPath)
    {
        Directory.CreateDirectory(outputDir);
        dllPath = Path.Combine(outputDir, $"{modName}.dll");

        Console.WriteLine($"[ModCompiler] Compiling {modName}...");

        var sources = Directory
            .GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f),
                path: f))
            .ToList();

        if (sources.Count == 0)
        {
            Console.Error.WriteLine("[ModCompiler] No .cs files found to compile.");
            return false;
        }

        var refs = BuildReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: modName,
            syntaxTrees: sources,
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Enable));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var diag in result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.Error.WriteLine($"[ModCompiler] {diag}");
            return false;
        }

        File.WriteAllBytes(dllPath, ms.ToArray());
        Console.WriteLine($"[ModCompiler] Built → {dllPath}");
        return true;
    }

    static List<MetadataReference> BuildReferences()
    {
        var refs = new List<MetadataReference>();

        // .NET runtime — skip native DLLs (no managed metadata)
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        foreach (string dll in Directory.GetFiles(runtimeDir, "*.dll"))
        {
            try
            {
                // GetAssemblyName throws BadImageFormatException for native DLLs
                System.Reflection.AssemblyName.GetAssemblyName(dll);
                refs.Add(MetadataReference.CreateFromFile(dll));
            }
            catch { /* native or unreadable — skip */ }
        }

        // SpectraSharp engine (contracts: ISpectraMod, BlockBase, etc.)
        refs.Add(MetadataReference.CreateFromFile(typeof(ISpectraMod).Assembly.Location));

        // HarmonyLib
        refs.Add(MetadataReference.CreateFromFile(typeof(Harmony).Assembly.Location));

        return refs;
    }
}
