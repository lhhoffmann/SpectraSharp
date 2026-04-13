namespace SpectraSharp.ModTranspiler.Model;

sealed class BlockDescriptor
{
    public string ClassName       { get; set; } = "";  // obfuscated Java class name
    public string SuperClass      { get; set; } = "";  // obfuscated superclass
    public int    BlockId         { get; set; }
    public int    TextureIndex    { get; set; }
    public string UnlocalizedName { get; set; } = "";
    public float  Hardness        { get; set; } = 1f;
    public float  BlastResistance { get; set; } = 1f;
    public float  LightEmission   { get; set; } = 0f;
    public int    LightOpacity    { get; set; } = 255;
    public string Material        { get; set; } = "stone";
    public bool   IsOpaque        { get; set; } = true;
    public bool   IsRandomTick    { get; set; } = false;

    /// <summary>Raw Java method bodies that need translation (tick, onUse, getDrops).</summary>
    public List<MethodBody> Methods { get; } = [];
}
