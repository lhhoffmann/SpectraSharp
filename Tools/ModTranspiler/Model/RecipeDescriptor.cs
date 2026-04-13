namespace SpectraSharp.ModTranspiler.Model;

enum RecipeType { Shaped, Shapeless, Smelting }

sealed class RecipeDescriptor
{
    public RecipeType Type       { get; set; }
    public int        OutputId   { get; set; }
    public int        OutputCount { get; set; } = 1;
    public float      SmeltXp    { get; set; } = 0f;

    /// <summary>For SHAPED: 3×3 grid using char keys, ' ' = empty.</summary>
    public string[]   Pattern    { get; set; } = [];
    /// <summary>Char key → item/block ID mapping for shaped recipes.</summary>
    public Dictionary<char, int> Ingredients { get; } = [];
    /// <summary>For SHAPELESS: flat list of ingredient IDs.</summary>
    public List<int>  ShapelessIds { get; } = [];
    /// <summary>For SMELTING: input item/block ID.</summary>
    public int        SmeltInputId { get; set; }
}
