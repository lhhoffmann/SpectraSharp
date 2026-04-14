namespace SpectraSharp.Core.Mods;

/// <summary>
/// Stores mod-registered crafting recipes at load time.
/// Vanilla recipes are handled separately by the crafting grid logic.
/// </summary>
public sealed class CraftingManager : ICraftingManager
{
    private readonly List<ShapedRecipe>    _shaped    = [];
    private readonly List<ShapelessRecipe> _shapeless = [];

    public IReadOnlyList<ShapedRecipe>    Shaped    => _shaped;
    public IReadOnlyList<ShapelessRecipe> Shapeless => _shapeless;

    public void AddShapedRecipe(ItemStack output, string[] pattern, Dictionary<char, int> key)
    {
        if (output.IsEmpty) return;
        _shaped.Add(new ShapedRecipe(output, [.. pattern], new Dictionary<char, int>(key)));
    }

    public void AddShapelessRecipe(ItemStack output, int[] ingredients)
    {
        if (output.IsEmpty) return;
        _shapeless.Add(new ShapelessRecipe(output, [.. ingredients]));
    }
}

/// <summary>Shaped (3×3 pattern) crafting recipe registered by a mod.</summary>
public sealed record ShapedRecipe(ItemStack Output, string[] Pattern, Dictionary<char, int> Key);

/// <summary>Shapeless (ingredient-list) crafting recipe registered by a mod.</summary>
public sealed record ShapelessRecipe(ItemStack Output, int[] Ingredients);
