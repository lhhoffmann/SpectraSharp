namespace SpectraEngine.Core.Crafting;

/// <summary>
/// Replica of <c>lm</c> (InventoryCrafting) — a 2D grid of <see cref="ItemStack"/>
/// slots used during recipe matching.
///
/// The 3×3 crafting table uses Width=3, Height=3.
/// The 2×2 player inventory crafting uses Width=2, Height=2.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/CraftingRecipes_Spec.md §1.2
/// </summary>
public sealed class CraftingGrid
{
    private readonly ItemStack?[] _slots;

    public readonly int Width;
    public readonly int Height;

    public CraftingGrid(int width, int height)
    {
        Width  = width;
        Height = height;
        _slots = new ItemStack?[width * height];
    }

    /// <summary>Returns the item stack at grid position (x, y), or null for empty.</summary>
    public ItemStack? GetSlot(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        return _slots[y * Width + x];
    }

    /// <summary>Sets the item stack at grid position (x, y). Null means empty.</summary>
    public void SetSlot(int x, int y, ItemStack? stack)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _slots[y * Width + x] = stack;
    }

    /// <summary>Returns true if all slots are empty.</summary>
    public bool IsEmpty()
    {
        foreach (var s in _slots)
            if (s != null && s.StackSize > 0) return false;
        return true;
    }
}
