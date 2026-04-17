namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>pi</c> (SlotArmor) — one of 4 player armor slots.
/// Accepts only items whose armor type matches the slot.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1, §9.3
/// </summary>
public sealed class SlotArmor : Slot
{
    private readonly int _armorType; // 0=boots, 1=leggings, 2=chestplate, 3=helmet

    public SlotArmor(IInventory inventory, int slotIndex, int armorType, int x, int y)
        : base(inventory, slotIndex, x, y)
    {
        _armorType = armorType;
    }

    /// <summary>
    /// Accepts only items that are armor pieces of the matching armor type.
    /// </summary>
    public override bool IsItemValid(ItemStack stack)
    {
        var item = stack.GetItem();
        if (item is not Items.ItemArmor armor) return false;
        return armor.ArmorType == _armorType;
    }

    /// <summary>Armor slots hold at most 1 item.</summary>
    public override int GetSlotStackLimit() => 1;
}
