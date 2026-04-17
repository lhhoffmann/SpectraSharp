namespace SpectraEngine.Core.Crafting;

/// <summary>
/// Replica of <c>bc</c> (ShapelessRecipes) — a shapeless crafting recipe where
/// the grid must contain exactly the listed ingredients in any arrangement.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/CraftingRecipes_Spec.md §1.1
/// </summary>
public sealed class VanillaShapelessRecipe : ICraftingRecipe
{
    private readonly CraftingIngredient[] _ingredients;
    private readonly ItemStack            _result;

    public VanillaShapelessRecipe(CraftingIngredient[] ingredients, ItemStack result)
    {
        _ingredients = ingredients;
        _result      = result;
    }

    // ── ICraftingRecipe ───────────────────────────────────────────────────────

    public ItemStack GetResult() => _result.Copy();

    public bool Matches(CraftingGrid grid)
    {
        // Collect all non-empty slots from the grid
        var gridItems = new List<ItemStack>(9);
        for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                var s = grid.GetSlot(x, y);
                if (s != null && s.StackSize > 0)
                    gridItems.Add(s);
            }

        if (gridItems.Count != _ingredients.Length) return false;

        // For each ingredient, find one matching grid item (remove it to prevent re-use)
        var remaining = new List<ItemStack>(gridItems);
        foreach (var ing in _ingredients)
        {
            int idx = remaining.FindIndex(s => ing.Matches(s));
            if (idx < 0) return false;
            remaining.RemoveAt(idx);
        }

        return remaining.Count == 0;
    }
}
