namespace SpectraSharp.ModTranspiler.Model;

enum InjectionPosition { Prepend, Append, ReplaceBody }

sealed class InjectionDescriptor
{
    /// <summary>Obfuscated vanilla class being patched.</summary>
    public string TargetClass  { get; set; } = "";
    /// <summary>Human-readable C# class name (resolved via VanillaClassList).</summary>
    public string TargetClassCs { get; set; } = "";
    public string TargetMethod { get; set; } = "";
    public string TargetMethodCs { get; set; } = "";
    public InjectionPosition Position { get; set; } = InjectionPosition.Append;

    /// <summary>
    /// The changed method body as raw Java source lines.
    /// Translator will apply VanillaApiMap substitutions line-by-line.
    /// </summary>
    public List<string> JavaLines { get; } = [];
}
