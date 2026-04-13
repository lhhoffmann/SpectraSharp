namespace SpectraSharp.ModTranspiler.Model;

sealed class WorldGenDescriptor
{
    public string ClassName   { get; set; } = "";
    public List<string> JavaLines { get; } = [];
}
