namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>pj</c> (Container) — abstract base class for all inventory windows.
///
/// Manages a flat slot list, change detection, and item click/shift-click dispatch.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §2
/// </summary>
public abstract class Container
{
    // ── Fields (spec §2.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>e</c> — ordered list of all slots in this container.</summary>
    public readonly List<Slot> Slots = [];

    /// <summary>obf: <c>d</c> — last-seen snapshots for change detection.</summary>
    private readonly List<ItemStack?> _lastSeen = [];

    /// <summary>obf: <c>g</c> — registered ICrafting listeners (players watching).</summary>
    protected readonly List<ICraftingListener> _listeners = [];

    /// <summary>Transaction counter — incremented each call to <see cref="GetNextTransactionId"/>.</summary>
    private short _transactionId;

    // ── Slot registration (spec §2.2) ─────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(vv)</c> — registers a slot into this container.
    /// Assigns the slot its container index and adds a null snapshot.
    /// </summary>
    protected Slot AddSlot(Slot slot)
    {
        slot.ContainerSlotIndex = Slots.Count;
        Slots.Add(slot);
        _lastSeen.Add(null);
        return slot;
    }

    // ── Listeners ─────────────────────────────────────────────────────────────

    public void AddCraftingListener(ICraftingListener listener) => _listeners.Add(listener);
    public void RemoveCraftingListener(ICraftingListener listener) => _listeners.Remove(listener);

    // ── Change detection (spec §2.3) ─────────────────────────────────────────

    /// <summary>
    /// obf: <c>a()</c> — detects changed slots and notifies all listeners.
    /// </summary>
    public virtual void DetectAndSendChanges()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            var current = Slots[i].GetStack();
            var last    = _lastSeen[i];

            if (!ItemStack.AreItemStacksEqual(current, last))
            {
                _lastSeen[i] = current?.Copy();
                foreach (var listener in _listeners)
                    listener.OnContainerSlotChanged(this, i, current?.Copy());
            }
        }
    }

    // ── Click handler (spec §2.4) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int slotId, int button, boolean shift, vi player)</c>
    /// Handles all left/right/shift-click interactions.
    /// Returns the item picked up (informational), or null.
    /// </summary>
    public virtual ItemStack? SlotClick(int slotId, int button, bool shiftClick, EntityPlayer player)
    {
        ItemStack? cursor = player.Inventory.GetItemStack();

        // ── Outside window (spec §2.4: slotId == -999) ────────────────────────
        if (slotId == -999)
        {
            if (cursor != null)
            {
                if (button == 0) { player.DropPlayerItem(cursor); player.Inventory.SetItemStack(null); }
                else             { player.DropPlayerItem(cursor.SplitStack(1)); }
            }
            return null;
        }

        if (slotId < 0) return null;
        var slot = Slots[slotId];

        // ── Shift-click (spec §2.4) ───────────────────────────────────────────
        if (shiftClick)
        {
            HandleShiftClick(player, slot);
            return null;
        }

        // ── Normal click ──────────────────────────────────────────────────────
        var slotStack = slot.GetStack();

        if (slotStack == null && cursor == null) return null;

        if (slotStack == null)
        {
            // Slot empty, cursor has item → place into slot
            if (!slot.IsItemValid(cursor!)) return null;
            int toPlace = button == 0 ? cursor!.StackSize : 1;
            toPlace = Math.Min(toPlace, slot.GetSlotStackLimit());
            toPlace = Math.Min(toPlace, cursor!.StackSize);
            slot.PutStack(cursor.SplitStack(toPlace));
            if (cursor.StackSize <= 0) player.Inventory.SetItemStack(null);
        }
        else if (cursor == null)
        {
            // Slot has item, cursor empty → pick up from slot
            int toTake = button == 0 ? slotStack.StackSize : (slotStack.StackSize + 1) / 2;
            var taken = slot.DecrStackSize(toTake);
            player.Inventory.SetItemStack(taken);
            slot.OnPickupFromSlot(player, taken ?? new ItemStack(slotStack.ItemId));
        }
        else
        {
            // Both slot and cursor have items
            if (cursor.ItemId == slotStack.ItemId
                && ItemStack.AreDamagesEqual(cursor, slotStack)
                && ItemStack.AreItemStackTagsEqual(cursor, slotStack))
            {
                // Same item — merge cursor into slot
                int limit = Math.Min(slot.GetSlotStackLimit(), slotStack.GetMaxStackSize());
                int canAdd = limit - slotStack.StackSize;
                if (canAdd > 0)
                {
                    int adding = button == 0 ? Math.Min(cursor.StackSize, canAdd) : Math.Min(1, canAdd);
                    cursor.StackSize -= adding;
                    slotStack.StackSize += adding;
                    slot.OnSlotChanged();
                    if (cursor.StackSize <= 0) player.Inventory.SetItemStack(null);
                }
            }
            else
            {
                // Different items — swap if cursor fits in slot
                if (cursor.StackSize <= slot.GetSlotStackLimit() && slot.IsItemValid(cursor))
                {
                    player.Inventory.SetItemStack(slotStack);
                    slot.PutStack(cursor);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Shift-click logic. Override in subclasses for specific behaviour.
    /// Default: attempt to merge the slot's item into the range of the other inventory half.
    /// </summary>
    protected virtual void HandleShiftClick(EntityPlayer player, Slot slot) { }

    // ── Merge helper (spec §2.6) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(dk, int start, int end, boolean reverse)</c> — merges <paramref name="stack"/>
    /// into slots [start, end). First pass merges into existing stacks; second pass fills empty slots.
    /// Returns true if any items were moved.
    /// </summary>
    protected bool MergeItemStack(ItemStack stack, int startSlot, int endSlot, bool reverse)
    {
        bool moved = false;
        int begin = reverse ? endSlot - 1 : startSlot;
        int step  = reverse ? -1 : 1;

        // First pass: merge into existing stacks
        for (int i = begin; i >= startSlot && i < endSlot && stack.StackSize > 0; i += step)
        {
            var target = Slots[i].GetStack();
            if (target == null) continue;
            if (target.ItemId != stack.ItemId) continue;
            if (!ItemStack.AreDamagesEqual(target, stack)) continue;
            if (!ItemStack.AreItemStackTagsEqual(target, stack)) continue;

            int limit = Math.Min(Slots[i].GetSlotStackLimit(), target.GetMaxStackSize());
            int canAdd = limit - target.StackSize;
            if (canAdd <= 0) continue;

            int adding = Math.Min(stack.StackSize, canAdd);
            stack.StackSize -= adding;
            target.StackSize += adding;
            Slots[i].OnSlotChanged();
            moved = true;
        }

        // Second pass: fill empty slots
        if (stack.StackSize > 0)
        {
            for (int i = begin; i >= startSlot && i < endSlot && stack.StackSize > 0; i += step)
            {
                if (Slots[i].HasStack()) continue;
                if (!Slots[i].IsItemValid(stack)) continue;

                int toPlace = Math.Min(stack.StackSize, Slots[i].GetSlotStackLimit());
                Slots[i].PutStack(stack.SplitStack(toPlace));
                moved = true;
            }
        }

        return moved;
    }

    // ── Transaction ID (spec §2.7) ────────────────────────────────────────────

    /// <summary>obf: <c>a(x)</c> — returns next short transaction ID.</summary>
    public short GetNextTransactionId(InventoryPlayer _) => _transactionId++;

    // ── Craft matrix changed ──────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="CraftingInventory.OnInventoryChanged"/> when any crafting input changes.
    /// Subclasses that contain crafting grids override this to refresh the output slot.
    /// </summary>
    public virtual void OnCraftMatrixChanged(IInventory inventory) { }

    // ── Validity ──────────────────────────────────────────────────────────────

    /// <summary>Returns true if this container is still valid for the given player.</summary>
    public abstract bool CanInteractWith(EntityPlayer player);

    // ── Container close (spec §2.5) ───────────────────────────────────────────

    /// <summary>
    /// obf: <c>b(vi player)</c> — drops cursor item and clears it.
    /// Subclasses that clean up crafting grids call super first, then drop grid items.
    /// </summary>
    public virtual void OnContainerClosed(EntityPlayer player)
    {
        var cursor = player.Inventory.GetItemStack();
        if (cursor != null)
        {
            player.DropPlayerItem(cursor);
            player.Inventory.SetItemStack(null);
        }
    }
}
