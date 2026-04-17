using SpectraEngine.Core.Crafting;

namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>ace</c> (ContainerWorkbench) — the 3×3 crafting table window.
///
/// Slot layout (spec §4.1):
///   0       : SlotCrafting (output)
///   1–9     : 3×3 crafting grid
///   10–36   : player main inventory (slots 9–35)
///   37–45   : player hotbar (slots 0–8)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §4
/// </summary>
public sealed class ContainerWorkbench : Container
{
    private readonly CraftingInventory  _matrix;
    private readonly SingleSlotInventory _output;

    private readonly int _worldX, _worldY, _worldZ;
    private readonly IWorld _world;

    public ContainerWorkbench(EntityPlayer player, IWorld world, int x, int y, int z)
    {
        _world  = world;
        _worldX = x;
        _worldY = y;
        _worldZ = z;

        _matrix = new CraftingInventory(this, 3, 3);
        _output = new SingleSlotInventory();

        // Slot 0 — crafting output
        AddSlot(new SlotCrafting(_matrix, _output, 124, 35));
        // Slots 1–9 — 3×3 crafting grid
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                AddSlot(new Slot(_matrix, row * 3 + col, 30 + col * 18, 17 + row * 18));
        // Slots 10–36 — player main inventory (slots 9–35)
        for (int i = 9; i < 36; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + (i - 9) % 9 * 18, 84 + (i - 9) / 9 * 18));
        // Slots 37–45 — player hotbar (slots 0–8)
        for (int i = 0; i < 9; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + i * 18, 142));
    }

    // ── Crafting update (spec §4.2) ───────────────────────────────────────────

    public override void OnCraftMatrixChanged(IInventory inventory)
    {
        var result = VanillaCraftingManager.Instance.FindMatchingRecipe(_matrix.ToCraftingGrid());
        _output.SetInventorySlotContents(0, result);
    }

    // ── Container close (spec §4.3) ───────────────────────────────────────────

    public override void OnContainerClosed(EntityPlayer player)
    {
        base.OnContainerClosed(player);
        // Drop all 9 crafting grid items
        for (int i = 0; i < 9; i++)
        {
            var stack = _matrix.GetStackInSlot(i);
            if (stack != null) { player.DropPlayerItem(stack); _matrix.SetInventorySlotContents(i, null); }
        }
    }

    // ── Validity (spec §4.4) ─────────────────────────────────────────────────

    public override bool CanInteractWith(EntityPlayer player)
    {
        if (_world.GetBlockId(_worldX, _worldY, _worldZ) != 58) return false; // 58 = workbench
        double dx = player.PosX - (_worldX + 0.5);
        double dy = player.PosY - (_worldY + 0.5);
        double dz = player.PosZ - (_worldZ + 0.5);
        return dx * dx + dy * dy + dz * dz <= 64.0;
    }

    // ── Shift-click ───────────────────────────────────────────────────────────

    protected override void HandleShiftClick(EntityPlayer player, Slot slot)
    {
        var stack = slot.GetStack();
        if (stack == null) return;
        var copy = stack.Copy();

        if (slot.ContainerSlotIndex == 0)
        {
            // Shift-click output → try to move to hotbar then main inventory
            if (!MergeItemStack(copy, 37, 46, false)
             && !MergeItemStack(copy, 10, 37, false)) return;
            slot.OnPickupFromSlot(player, copy);
        }
        else if (slot.ContainerSlotIndex >= 10)
        {
            // Shift-click inventory → try crafting grid first
            if (!MergeItemStack(copy, 1, 10, false)) return;
        }
        else
        {
            // Shift-click grid → move to inventory
            if (!MergeItemStack(copy, 37, 46, false)
             && !MergeItemStack(copy, 10, 37, false)) return;
        }

        if (copy.StackSize == 0) slot.PutStack(null);
        else { slot.PutStack(copy); slot.OnSlotChanged(); }
    }
}
