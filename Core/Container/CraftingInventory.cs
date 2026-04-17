using SpectraEngine.Core.Crafting;

namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>lm</c> (InventoryCrafting) — an NxM grid IInventory used as the
/// backing store for crafting input slots.
///
/// Notifies the parent container when any slot changes so the output slot can be refreshed.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1 (class map, lm)
/// </summary>
public sealed class CraftingInventory : IInventory
{
    private readonly ItemStack?[] _slots;
    private readonly Container    _parent;

    public readonly int Width;
    public readonly int Height;

    public CraftingInventory(Container parent, int width, int height)
    {
        _parent = parent;
        Width   = width;
        Height  = height;
        _slots  = new ItemStack?[width * height];
    }

    // ── IInventory ───────────────────────────────────────────────────────────

    public int        GetSizeInventory()              => _slots.Length;
    public ItemStack? GetStackInSlot(int slot)        => _slots[slot];
    public string     GetInvName()                    => "crafting";
    public int        GetInventoryStackLimit()         => 64;
    public bool       IsUseableByPlayer(EntityPlayer p) => true;
    public void       OpenChest()  { }
    public void       CloseChest() { }

    public void SetInventorySlotContents(int slot, ItemStack? stack)
    {
        _slots[slot] = stack;
        OnInventoryChanged();
    }

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (_slots[slot] == null) return null;
        var s = _slots[slot]!;
        if (s.StackSize <= count) { _slots[slot] = null; return s; }
        var split = s.SplitStack(count);
        OnInventoryChanged();
        return split;
    }

    public void OnInventoryChanged()
    {
        _parent.OnCraftMatrixChanged(this);
    }

    // ── Bridge to CraftingGrid ────────────────────────────────────────────────

    /// <summary>Returns a <see cref="CraftingGrid"/> snapshot of the current contents.</summary>
    public CraftingGrid ToCraftingGrid()
    {
        var grid = new CraftingGrid(Width, Height);
        for (int i = 0; i < _slots.Length; i++)
            grid.SetSlot(i % Width, i / Width, _slots[i]);
        return grid;
    }
}
