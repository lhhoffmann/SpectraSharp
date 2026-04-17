namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>ie</c> (SlotFurnaceOutput) — the output slot for a furnace.
/// Read-only; tracks smelting stats when the player takes the result.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1, §9.2
/// </summary>
public sealed class SlotFurnaceOutput : Slot
{
    public SlotFurnaceOutput(IInventory furnaceInventory, int x, int y)
        : base(furnaceInventory, 2, x, y) { }

    /// <summary>Output slot is read-only — items are placed here by the furnace tick, not the player.</summary>
    public override bool IsItemValid(ItemStack stack) => false;
}
