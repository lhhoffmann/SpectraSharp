namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>agi</c> (ItemArmor) — helmet, chestplate, leggings, boots.
/// Extends <see cref="Item"/>.
///
/// Slot indices (armorType): 0=helmet, 1=chestplate, 2=leggings, 3=boots.
/// Durability formula: SlotBase[armorType] * material.DurabilityFactor.
///   SlotBase = {11, 16, 15, 13}  (helmet=11, chest=16, legs=15, boots=13).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §10
/// </summary>
public sealed class ItemArmor : Item
{
    /// <summary>obf: a — armor slot type: 0=helmet, 1=chest, 2=legs, 3=boots.</summary>
    public readonly int ArmorType;

    /// <summary>obf: b — protection value from material for this slot.</summary>
    public readonly int Protection;

    /// <summary>obf: bR — armor texture/material tier index.</summary>
    public readonly int ArmorSlot;

    /// <summary>obf: bT — armor material.</summary>
    private readonly EnumArmorMaterial _material;

    /// <summary>
    /// Spec: <c>agi(int itemId, dj material, int armorSlot, int armorType)</c>.
    /// </summary>
    public ItemArmor(int id, EnumArmorMaterial material, int armorSlot, int armorType)
        : base(id)
    {
        _material    = material;
        ArmorType    = armorType;
        ArmorSlot    = armorSlot;
        Protection   = material.GetProtection(armorType);
        MaxStackSize = 1;
        SetInternalDurability(material.GetDurability(armorType));
    }

    // ── Durability ───────────────────────────────────────────────────────────

    public override int GetMaxDamage() => GetInternalDurabilityValue();

    // ── Enchantability (spec §10.5) ──────────────────────────────────────────

    public override int GetItemEnchantability() => _material.GetEnchantability();
}
