namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>vv</c> (Slot) — one cell in a container, backed by an <see cref="IInventory"/>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §3
/// </summary>
public class Slot
{
    // ── Fields (spec §3) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>c</c> — the backing inventory.</summary>
    protected readonly IInventory Inventory;

    /// <summary>obf: <c>a</c> — slot index within the backing inventory.</summary>
    public readonly int SlotIndex;

    /// <summary>Screen X position (pixels). obf: <c>e</c>.</summary>
    public readonly int XDisplayPosition;

    /// <summary>Screen Y position (pixels). obf: <c>f</c>.</summary>
    public readonly int YDisplayPosition;

    /// <summary>Index within the parent container's slot list. Set by <see cref="Container.AddSlot"/>.</summary>
    public int ContainerSlotIndex;

    // ── Constructor ───────────────────────────────────────────────────────────

    public Slot(IInventory inventory, int slotIndex, int x, int y)
    {
        Inventory        = inventory;
        SlotIndex        = slotIndex;
        XDisplayPosition = x;
        YDisplayPosition = y;
    }

    // ── IInventory delegation (spec §3) ──────────────────────────────────────

    /// <summary>obf: <c>b()</c> — get current ItemStack.</summary>
    public virtual ItemStack? GetStack() => Inventory.GetStackInSlot(SlotIndex);

    /// <summary>obf: <c>c(dk)</c> — set ItemStack, mark dirty.</summary>
    public virtual void PutStack(ItemStack? stack)
    {
        Inventory.SetInventorySlotContents(SlotIndex, stack);
        OnSlotChanged();
    }

    /// <summary>obf: <c>d()</c> — mark inventory dirty.</summary>
    public void OnSlotChanged() => Inventory.OnInventoryChanged();

    /// <summary>obf: <c>a()</c> — maximum stack size (from backing inventory).</summary>
    public virtual int GetSlotStackLimit() => Inventory.GetInventoryStackLimit();

    /// <summary>
    /// obf: <c>a(dk)</c> — returns true if <paramref name="stack"/> may be placed here.
    /// Base: always true.
    /// </summary>
    public virtual bool IsItemValid(ItemStack stack) => true;

    /// <summary>
    /// obf: <c>a(int count)</c> — removes up to <paramref name="count"/> items.
    /// </summary>
    public virtual ItemStack? DecrStackSize(int count)
        => Inventory.DecrStackSize(SlotIndex, count);

    /// <summary>obf: <c>c()</c> — returns true if the slot has an item.</summary>
    public bool HasStack() => GetStack() != null;

    /// <summary>Called when the player takes an item from this slot (e.g. from output slot).</summary>
    public virtual void OnPickupFromSlot(EntityPlayer player, ItemStack stack) { }
}
