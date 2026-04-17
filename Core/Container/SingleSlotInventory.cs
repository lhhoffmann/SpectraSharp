namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>iy</c> — a 1-slot output buffer used for the crafting result slot.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1 (class map, iy)
/// </summary>
public sealed class SingleSlotInventory : IInventory
{
    private ItemStack? _slot;

    public int        GetSizeInventory()                 => 1;
    public ItemStack? GetStackInSlot(int slot)            => _slot;
    public string     GetInvName()                        => "craftResult";
    public int        GetInventoryStackLimit()             => 64;
    public bool       IsUseableByPlayer(EntityPlayer p)   => true;
    public void       OpenChest()  { }
    public void       CloseChest() { }
    public void       OnInventoryChanged() { }

    public void SetInventorySlotContents(int slot, ItemStack? stack) => _slot = stack;

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (_slot == null) return null;
        var s = _slot;
        if (s.StackSize <= count) { _slot = null; return s; }
        return s.SplitStack(count);
    }
}
