namespace SpectraSharp.Core.Mods;

/// <summary>
/// Stores mod-registered furnace smelting recipes at load time.
/// Vanilla recipes are handled separately by the furnace tile-entity logic.
/// </summary>
public sealed class SmeltingManager : ISmeltingManager
{
    private readonly List<SmeltingRecipe> _recipes = [];

    public IReadOnlyList<SmeltingRecipe> Recipes => _recipes;

    public void AddSmeltingRecipe(int inputId, ItemStack output, float xp)
    {
        if (output.IsEmpty || inputId <= 0) return;
        _recipes.Add(new SmeltingRecipe(inputId, output, xp));
    }
}

/// <summary>Furnace smelting recipe registered by a mod.</summary>
public sealed record SmeltingRecipe(int InputId, ItemStack Output, float Xp);
