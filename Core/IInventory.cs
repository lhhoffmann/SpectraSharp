namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>de</c> (IInventory) — interface implemented by all inventory containers.
///
/// Method name mapping (Java obfuscated → semantic):
///   c()          → GetSizeInventory
///   d(int)       → GetStackInSlot
///   a(int, int)  → DecrStackSize
///   a(int, dk)   → SetInventorySlotContents
///   d()          → GetInvName     (note: same obfuscated name as d(int); disambiguated by arity)
///   e()          → GetInventoryStackLimit
///   h()          → OnInventoryChanged
///   b_(vi)       → IsUseableByPlayer
///   j()          → OpenChest
///   k()          → CloseChest
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/IInventory_Spec.md
/// </summary>
public interface IInventory
{
    /// <summary>obf: <c>c()</c> — total number of slots.</summary>
    int GetSizeInventory();

    /// <summary>
    /// obf: <c>d(int slot)</c> — returns the ItemStack at the given slot; null if empty.
    /// </summary>
    ItemStack? GetStackInSlot(int slot);

    /// <summary>
    /// obf: <c>a(int slot, int count)</c> — removes up to <paramref name="count"/> items
    /// from the slot. If the slot has ≤ count items, the entire stack is removed and returned.
    /// If it has more, <see cref="ItemStack.SplitStack"/> is called and the slot is mutated.
    /// Never returns null when the slot was non-null.
    /// </summary>
    ItemStack? DecrStackSize(int slot, int count);

    /// <summary>
    /// obf: <c>a(int slot, dk stack)</c> — replaces the slot with the given ItemStack.
    /// Pass null to clear.
    /// </summary>
    void SetInventorySlotContents(int slot, ItemStack? stack);

    /// <summary>obf: <c>d()</c> — display name (e.g. "Inventory", "container.chest").</summary>
    string GetInvName();

    /// <summary>obf: <c>e()</c> — maximum stack size for this inventory (usually 64).</summary>
    int GetInventoryStackLimit();

    /// <summary>
    /// obf: <c>h()</c> — called after any slot modification.
    /// Used by tile entities to push changes to clients.
    /// </summary>
    void OnInventoryChanged();

    /// <summary>
    /// obf: <c>b_(vi player)</c> — returns true if the player is allowed to interact
    /// with this inventory (e.g. close enough to a chest).
    /// </summary>
    bool IsUseableByPlayer(EntityPlayer player);

    /// <summary>obf: <c>j()</c> — called when a player opens this container.</summary>
    void OpenChest();

    /// <summary>obf: <c>k()</c> — called when a player closes this container.</summary>
    void CloseChest();
}
