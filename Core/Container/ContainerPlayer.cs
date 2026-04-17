using SpectraEngine.Core.Crafting;

namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>gd</c> (ContainerPlayer) — the player inventory screen (E key).
///
/// Slot layout (spec §5.1):
///   0       : SlotCrafting (2×2 output)
///   1–4     : 2×2 crafting grid
///   5–8     : 4 armor slots (head=5, chest=6, legs=7, feet=8) — spec uses indices 3→0
///   9–35    : player main inventory (slots 9–35)
///   36–44   : player hotbar (slots 0–8)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §5
/// </summary>
public sealed class ContainerPlayer : Container
{
    private readonly CraftingInventory   _matrix;
    private readonly SingleSlotInventory _output;

    // Armor type order in slot display: 3=helmet, 2=chestplate, 1=leggings, 0=boots
    private static readonly int[] ArmorSlotOrder = [3, 2, 1, 0];

    public ContainerPlayer(EntityPlayer player)
    {
        _matrix = new CraftingInventory(this, 2, 2);
        _output = new SingleSlotInventory();

        // Slot 0 — crafting output
        AddSlot(new SlotCrafting(_matrix, _output, 144, 36));
        // Slots 1–4 — 2×2 crafting grid
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 2; col++)
                AddSlot(new Slot(_matrix, row * 2 + col, 88 + col * 18, 26 + row * 18));
        // Slots 5–8 — armor slots (top = helmet, bottom = boots)
        for (int i = 0; i < 4; i++)
            AddSlot(new SlotArmor(player.Inventory, 36 + ArmorSlotOrder[i], ArmorSlotOrder[i], 8, 8 + i * 18));
        // Slots 9–35 — player main inventory (rows 1–3)
        for (int i = 9; i < 36; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + (i - 9) % 9 * 18, 84 + (i - 9) / 9 * 18));
        // Slots 36–44 — hotbar
        for (int i = 0; i < 9; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + i * 18, 142));
    }

    // ── Crafting update ───────────────────────────────────────────────────────

    public override void OnCraftMatrixChanged(IInventory inventory)
    {
        var result = VanillaCraftingManager.Instance.FindMatchingRecipe(_matrix.ToCraftingGrid());
        _output.SetInventorySlotContents(0, result);
    }

    // ── Container close — drop crafting grid items ────────────────────────────

    public override void OnContainerClosed(EntityPlayer player)
    {
        base.OnContainerClosed(player);
        for (int i = 0; i < 4; i++)
        {
            var stack = _matrix.GetStackInSlot(i);
            if (stack != null) { player.DropPlayerItem(stack); _matrix.SetInventorySlotContents(i, null); }
        }
    }

    // ── Validity (spec §5.2) — always true ───────────────────────────────────

    public override bool CanInteractWith(EntityPlayer player) => true;
}
