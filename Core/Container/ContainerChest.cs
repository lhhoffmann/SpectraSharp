namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>ak</c> (ContainerChest) — single (27 slots) or double chest (54 slots).
///
/// Slot layout (spec §7.1, b = numRows):
///   0..(b*9−1)        : chest slots
///   (b*9)..(b*9+26)   : player main inventory (slots 9–35)
///   (b*9+27)..(b*9+35): player hotbar (slots 0–8)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §7
/// </summary>
public sealed class ContainerChest : Container
{
    private readonly IInventory _chestInventory;
    private readonly int        _numRows;

    public ContainerChest(EntityPlayer player, IInventory chestInventory)
    {
        _chestInventory = chestInventory;
        _numRows        = chestInventory.GetSizeInventory() / 9;

        // Chest slots
        for (int row = 0; row < _numRows; row++)
            for (int col = 0; col < 9; col++)
                AddSlot(new Slot(chestInventory, row * 9 + col, 8 + col * 18, 18 + row * 18));

        int chestSlotCount = _numRows * 9;

        // Player main inventory (slots 9–35)
        for (int i = 9; i < 36; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + (i - 9) % 9 * 18, chestSlotCount + 18 + (i - 9) / 9 * 18));

        // Player hotbar (slots 0–8)
        for (int i = 0; i < 9; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + i * 18, chestSlotCount + 76));

        chestInventory.OpenChest();
    }

    // ── Validity ─────────────────────────────────────────────────────────────

    public override bool CanInteractWith(EntityPlayer player)
        => _chestInventory.IsUseableByPlayer(player);

    // ── Close ─────────────────────────────────────────────────────────────────

    public override void OnContainerClosed(EntityPlayer player)
    {
        base.OnContainerClosed(player);
        _chestInventory.CloseChest();
    }

    // ── Shift-click ───────────────────────────────────────────────────────────

    protected override void HandleShiftClick(EntityPlayer player, Slot slot)
    {
        var stack = slot.GetStack();
        if (stack == null) return;
        var copy = stack.Copy();

        int chestEnd    = _numRows * 9;
        int playerStart = chestEnd;
        int playerEnd   = playerStart + 27;
        int hotbarStart = playerEnd;
        int hotbarEnd   = hotbarStart + 9;

        if (slot.ContainerSlotIndex < chestEnd)
        {
            // Chest → player inventory (hotbar first, then main)
            if (!MergeItemStack(copy, hotbarStart, hotbarEnd, false)
             && !MergeItemStack(copy, playerStart, playerEnd, false)) return;
        }
        else
        {
            // Player inventory → chest
            if (!MergeItemStack(copy, 0, chestEnd, false)) return;
        }

        if (copy.StackSize == 0) slot.PutStack(null);
        else { slot.PutStack(copy); slot.OnSlotChanged(); }
    }
}
