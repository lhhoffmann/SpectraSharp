namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>afe</c> (SlotCrafting) — the output slot for a crafting grid.
/// Read-only (nothing may be placed here); when the player takes the result,
/// all 9 input slots are decremented by 1.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1, §9.1
/// </summary>
public sealed class SlotCrafting : Slot
{
    private readonly CraftingInventory _matrix;

    public SlotCrafting(CraftingInventory matrix, SingleSlotInventory output, int x, int y)
        : base(output, 0, x, y)
    {
        _matrix = matrix;
    }

    /// <summary>Output slot is read-only — nothing can be placed here by the player.</summary>
    public override bool IsItemValid(ItemStack stack) => false;

    /// <summary>On pickup: decrement all input slots by 1 (consume crafting ingredients).</summary>
    public override void OnPickupFromSlot(EntityPlayer player, ItemStack stack)
    {
        for (int i = 0; i < _matrix.GetSizeInventory(); i++)
        {
            var ingredient = _matrix.GetStackInSlot(i);
            if (ingredient == null) continue;
            ingredient.StackSize--;
            if (ingredient.StackSize <= 0)
                _matrix.SetInventorySlotContents(i, null);
        }
    }
}
