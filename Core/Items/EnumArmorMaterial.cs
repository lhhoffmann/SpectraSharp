namespace SpectraSharp.Core.Items;

/// <summary>
/// Replica of <c>dj</c> (EnumArmorMaterial) — five armor-material constants.
///
/// Fields (spec §9):
///   f = durabilityFactor, g = protectionAmounts[4], h = enchantability
///
/// Slot indices: 0=helmet, 1=chestplate, 2=leggings, 3=boots.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §9
/// </summary>
public sealed class EnumArmorMaterial
{
    /// <summary>obf: f — durability factor; multiplied by slot base to get max durability.</summary>
    public readonly int DurabilityFactor;

    /// <summary>obf: g — protection values per slot [helmet, chest, legs, boots].</summary>
    public readonly int[] ProtectionAmounts;

    /// <summary>obf: h — enchantability value (higher = better enchants).</summary>
    public readonly int Enchantability;

    /// <summary>Slot base durability constants: {11, 16, 15, 13}. Spec: agi.bS.</summary>
    private static readonly int[] SlotBase = { 11, 16, 15, 13 };

    private EnumArmorMaterial(int durabilityFactor, int[] protectionAmounts, int enchantability)
    {
        DurabilityFactor  = durabilityFactor;
        ProtectionAmounts = protectionAmounts;
        Enchantability    = enchantability;
    }

    // ── Accessors (spec §9) ─────────────────────────────────────────────────

    /// <summary>obf: a(int slotType) — getDurability = SlotBase[slot] * DurabilityFactor.</summary>
    public int GetDurability(int slotType) => SlotBase[slotType] * DurabilityFactor;

    /// <summary>obf: b(int slotType) — getProtection = ProtectionAmounts[slot].</summary>
    public int GetProtection(int slotType) => ProtectionAmounts[slotType];

    /// <summary>obf: a() — getEnchantability.</summary>
    public int GetEnchantability() => Enchantability;

    // ── Constants (spec §9 table) ────────────────────────────────────────────

    /// <summary>obf: dj.a — LEATHER: durFactor=5, protect={1,3,2,1}, enchant=15.</summary>
    public static readonly EnumArmorMaterial Leather = new(5,  [1, 3, 2, 1], 15);

    /// <summary>obf: dj.b — CHAIN: durFactor=15, protect={2,5,4,1}, enchant=12.</summary>
    public static readonly EnumArmorMaterial Chain   = new(15, [2, 5, 4, 1], 12);

    /// <summary>obf: dj.c — IRON: durFactor=15, protect={2,6,5,2}, enchant=9.</summary>
    public static readonly EnumArmorMaterial IronMat = new(15, [2, 6, 5, 2],  9);

    /// <summary>obf: dj.d — GOLD: durFactor=7, protect={2,5,3,1}, enchant=25.</summary>
    public static readonly EnumArmorMaterial GoldMat = new(7,  [2, 5, 3, 1], 25);

    /// <summary>obf: dj.e — DIAMOND: durFactor=33, protect={3,8,6,3}, enchant=10.</summary>
    public static readonly EnumArmorMaterial Diamond = new(33, [3, 8, 6, 3], 10);
}
