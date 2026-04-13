namespace SpectraSharp.ModTranspiler.Pipeline;

sealed class PipelineResult
{
    public bool     Success          { get; init; }
    public string   DllPath          { get; init; } = "";
    public int      BlocksGenerated  { get; init; }
    public int      ItemsGenerated   { get; init; }
    public int      HooksGenerated   { get; init; }
    public int      TodoCount        { get; init; }
    public string[] Errors           { get; init; } = [];
}

/// <summary>
/// Orchestrates all six pipeline phases end-to-end.
/// </summary>
static class TranspilerPipeline
{
    const string TempRoot   = "temp/mods";
    const string OutputRoot = "Bridge/Mods";
    const string CompiledDir = "mods/compiled";

    public static PipelineResult Run(string jarPath, string modName, string outputRoot)
    {
        // Phase 1 — Decompile
        string decompiledDir = Decompiler.Run(jarPath, TempRoot);

        // Phase 2 — Diff
        var tags = ModDiffer.Run(jarPath, decompiledDir);

        // Phase 3 — Build manifest
        var manifest = ManifestBuilder.Run(modName, decompiledDir, tags);

        if (manifest.TotalObjects == 0)
        {
            return new PipelineResult
            {
                Success = true,
                DllPath = "",
                Errors  = ["No translatable content found in this JAR."],
            };
        }

        // Phase 4 — Translate to C#
        var sources = Translator.Run(manifest);

        // Phase 5 — Emit .cs files
        string sourceDir = CodeEmitter.Run(modName, sources, outputRoot);

        // Count TODOs in generated source
        int todoCount = sources.Sum(s =>
            s.Source.Split('\n').Count(l => l.Contains("// TODO:")));

        // Phase 6 — Compile to DLL
        bool success = ModCompiler.Run(modName, sourceDir, CompiledDir, out string dllPath);

        return new PipelineResult
        {
            Success         = success,
            DllPath         = dllPath,
            BlocksGenerated = manifest.NewBlocks.Count,
            ItemsGenerated  = manifest.NewItems.Count,
            HooksGenerated  = manifest.Overrides.Count,
            TodoCount       = todoCount,
            Errors          = success ? [] : ["Compilation failed — see output above."],
        };
    }
}
