namespace SpectraSharp.ModTranspiler.Model;

/// <summary>
/// A raw Java method captured from the decompiled source.
/// Stored as lines for line-by-line translation in Phase 4.
/// </summary>
sealed class MethodBody
{
    public string       Name       { get; set; } = "";
    public string       ReturnType { get; set; } = "void";
    public List<string> Parameters { get; } = [];
    public List<string> JavaLines  { get; } = [];
}
