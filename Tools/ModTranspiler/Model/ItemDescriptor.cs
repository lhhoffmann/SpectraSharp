namespace SpectraEngine.ModTranspiler.Model;

sealed class ItemDescriptor
{
    public string ClassName        { get; set; } = "";
    public int    ItemId           { get; set; }
    public int    TextureIndex     { get; set; }
    public string UnlocalizedName  { get; set; } = "";
    public int    MaxStackSize     { get; set; } = 64;
    public int    MaxDamage        { get; set; } = 0;
    public float  AttackDamage     { get; set; } = 1f;
    public bool   IsTool           { get; set; } = false;
    public string ToolType         { get; set; } = "";   // pickaxe, axe, shovel, sword, hoe
    public int    ToolLevel        { get; set; } = 0;    // 0=wood,1=stone,2=iron,3=diamond,4=gold

    public List<MethodBody> Methods { get; } = [];
}
