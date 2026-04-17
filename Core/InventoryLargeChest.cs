namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>adv</c> (InventoryLargeChest) — combines two 27-slot chests into a
/// single 54-slot inventory. Slot 0–26 → left chest; slot 27–53 → right chest.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockChest_Spec.md §8
/// </summary>
public sealed class InventoryLargeChest : IInventory
{
    private readonly string      _name;
    private readonly IInventory  _upper; // slots 0–26
    private readonly IInventory  _lower; // slots 27–53

    public InventoryLargeChest(string name, IInventory upper, IInventory lower)
    {
        _name  = name;
        _upper = upper;
        _lower = lower;
    }

    // ── IInventory ────────────────────────────────────────────────────────────

    public int GetSizeInventory() => _upper.GetSizeInventory() + _lower.GetSizeInventory();

    public ItemStack? GetStackInSlot(int slot)
        => slot < _upper.GetSizeInventory()
            ? _upper.GetStackInSlot(slot)
            : _lower.GetStackInSlot(slot - _upper.GetSizeInventory());

    public ItemStack? DecrStackSize(int slot, int count)
        => slot < _upper.GetSizeInventory()
            ? _upper.DecrStackSize(slot, count)
            : _lower.DecrStackSize(slot - _upper.GetSizeInventory(), count);

    public void SetInventorySlotContents(int slot, ItemStack? stack)
    {
        if (slot < _upper.GetSizeInventory())
            _upper.SetInventorySlotContents(slot, stack);
        else
            _lower.SetInventorySlotContents(slot - _upper.GetSizeInventory(), stack);
    }

    public string GetInvName()             => _name;
    public int    GetInventoryStackLimit() => _upper.GetInventoryStackLimit();
    public void   OnInventoryChanged()     { _upper.OnInventoryChanged(); _lower.OnInventoryChanged(); }

    public bool IsUseableByPlayer(EntityPlayer player)
        => _upper.IsUseableByPlayer(player) && _lower.IsUseableByPlayer(player);

    public void OpenChest()  { _upper.OpenChest();  _lower.OpenChest(); }
    public void CloseChest() { _upper.CloseChest(); _lower.CloseChest(); }
}
