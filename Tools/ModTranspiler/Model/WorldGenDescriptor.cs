namespace SpectraSharp.ModTranspiler.Model;

sealed class WorldGenDescriptor
{
    public string ClassName   { get; set; } = "";
    public List<string> JavaLines { get; } = [];
    /// <summary>Instance fields extracted from the BaseMod class (e.g. CopperVeinCount = 18).</summary>
    public Dictionary<string, string> Fields { get; } = [];
}
