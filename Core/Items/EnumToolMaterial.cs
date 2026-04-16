namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>nu</c> (EnumToolMaterial) — five tool-material constants.
///
/// Fields (spec §2):
///   f = harvestLevel, g = maxUses/durability, h = efficiencyOnProperMaterial,
///   i = damageVsEntity (weapon bonus), j = enchantability
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §2
/// </summary>
public sealed class EnumToolMaterial
{
    /// <summary>obf: f — minimum harvest level (0=any, 1=stone+, 2=iron+, 3=diamond).</summary>
    public readonly int HarvestLevel;

    /// <summary>obf: g — maximum durability uses before breaking.</summary>
    public readonly int MaxUses;

    /// <summary>obf: h — mining speed multiplier on effective blocks.</summary>
    public readonly float Efficiency;

    /// <summary>obf: i — weapon damage bonus added to base damage.</summary>
    public readonly int DamageBonus;

    /// <summary>obf: j — enchantability value (higher = better enchants).</summary>
    public readonly int Enchantability;

    private EnumToolMaterial(int harvestLevel, int maxUses, float efficiency, int damageBonus, int enchantability)
    {
        HarvestLevel   = harvestLevel;
        MaxUses        = maxUses;
        Efficiency     = efficiency;
        DamageBonus    = damageBonus;
        Enchantability = enchantability;
    }

    // ── Accessors (spec §2, Java obf method names) ──────────────────────────────

    /// <summary>obf: a() — getMaxUses → MaxUses.</summary>
    public int GetMaxUses() => MaxUses;

    /// <summary>obf: b() — getEfficiencyOnProperMaterial → Efficiency.</summary>
    public float GetEfficiency() => Efficiency;

    /// <summary>obf: c() — getDamageVsEntity → DamageBonus.</summary>
    public int GetDamageVsEntity() => DamageBonus;

    /// <summary>obf: d() — getHarvestLevel → HarvestLevel.</summary>
    public int GetHarvestLevel() => HarvestLevel;

    /// <summary>obf: e() — getEnchantability → Enchantability.</summary>
    public int GetEnchantability() => Enchantability;

    // ── Constants (spec §2 table) ────────────────────────────────────────────

    /// <summary>obf: nu.a — WOOD: harvestLevel=0, maxUses=59, efficiency=2.0F, damageBonus=0, enchant=15.</summary>
    public static readonly EnumToolMaterial Wood    = new(0,   59,  2.0f, 0, 15);

    /// <summary>obf: nu.b — STONE: harvestLevel=1, maxUses=131, efficiency=4.0F, damageBonus=1, enchant=5.</summary>
    public static readonly EnumToolMaterial Stone   = new(1,  131,  4.0f, 1,  5);

    /// <summary>obf: nu.c — IRON: harvestLevel=2, maxUses=250, efficiency=6.0F, damageBonus=2, enchant=14.</summary>
    public static readonly EnumToolMaterial Iron    = new(2,  250,  6.0f, 2, 14);

    /// <summary>obf: nu.d — DIAMOND: harvestLevel=3, maxUses=1561, efficiency=8.0F, damageBonus=3, enchant=10.</summary>
    public static readonly EnumToolMaterial Diamond = new(3, 1561,  8.0f, 3, 10);

    /// <summary>obf: nu.e — GOLD: harvestLevel=0, maxUses=32, efficiency=12.0F, damageBonus=0, enchant=22. Quirk 3: highest speed, lowest durability.</summary>
    public static readonly EnumToolMaterial Gold    = new(0,   32, 12.0f, 0, 22);
}
