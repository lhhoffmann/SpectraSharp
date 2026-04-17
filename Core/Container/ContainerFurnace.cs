using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>eg</c> (ContainerFurnace) — the furnace window.
///
/// Slot layout (spec §6.1):
///   0      : input (TileEntityFurnace slot 0)
///   1      : fuel  (TileEntityFurnace slot 1)
///   2      : SlotFurnaceOutput (slot 2)
///   3–29   : player main inventory (slots 9–35)
///   30–38  : player hotbar (slots 0–8)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §6
/// </summary>
public sealed class ContainerFurnace : Container
{
    private readonly TileEntityFurnace _furnace;

    public ContainerFurnace(EntityPlayer player, TileEntityFurnace furnace)
    {
        _furnace = furnace;

        // Slots 0–2 — furnace input/fuel/output
        AddSlot(new Slot       (furnace, 0, 56,  17)); // input
        AddSlot(new Slot       (furnace, 1, 56,  53)); // fuel
        AddSlot(new SlotFurnaceOutput(furnace, 116, 35)); // output

        // Slots 3–29 — player main inventory (slots 9–35)
        for (int i = 9; i < 36; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + (i - 9) % 9 * 18, 84 + (i - 9) / 9 * 18));
        // Slots 30–38 — hotbar
        for (int i = 0; i < 9; i++)
            AddSlot(new Slot(player.Inventory, i, 8 + i * 18, 142));

        furnace.OpenChest();
    }

    // ── Data sync for progress / burn time (spec §6.2) ───────────────────────

    public override void DetectAndSendChanges()
    {
        base.DetectAndSendChanges();
        foreach (var listener in GetListeners())
        {
            listener.OnContainerDataChanged(this, 0, _furnace.CookTime);
            listener.OnContainerDataChanged(this, 1, _furnace.BurnTime);
            listener.OnContainerDataChanged(this, 2, _furnace.CurrentBurnTime);
        }
    }

    // ── Client update (spec §6.3) ─────────────────────────────────────────────

    public void UpdateProgressFromServer(int dataId, int value)
    {
        switch (dataId)
        {
            case 0: _furnace.CookTime        = value; break;
            case 1: _furnace.BurnTime        = value; break;
            case 2: _furnace.CurrentBurnTime = value; break;
        }
    }

    // ── Validity (spec §6.4) ─────────────────────────────────────────────────

    public override bool CanInteractWith(EntityPlayer player) => _furnace.IsUseableByPlayer(player);

    // ── Close ─────────────────────────────────────────────────────────────────

    public override void OnContainerClosed(EntityPlayer player)
    {
        base.OnContainerClosed(player);
        _furnace.CloseChest();
    }

    // ── Helper to expose listeners for DetectAndSendChanges ──────────────────

    private IEnumerable<ICraftingListener> GetListeners() => _listeners;
}
