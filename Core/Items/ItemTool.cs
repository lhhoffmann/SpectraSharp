namespace SpectraEngine.Core.Items;

// ── ItemTool (ads) ────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ads</c> (ItemTool) — base class for shovel, pickaxe, and axe.
/// Extends <see cref="Item"/>.
///
/// Fields (spec §3.1): bR=effectiveBlockIds, a=efficiency, bS=weaponDamage, b=material.
/// Constructor (spec §3.2): sets durability, efficiency, weapon damage = baseDamage + material.i.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §3-6
/// </summary>
public class ItemTool : Item
{
    /// <summary>obf: bR — IDs of blocks mined at efficiency speed. Looked up via Block.BlocksList[].</summary>
    private readonly int[] _effectiveBlockIds;

    /// <summary>obf: a — mining speed multiplier on effective blocks.</summary>
    private readonly float _efficiency;

    /// <summary>obf: bS — weapon damage = baseDamage + material.DamageBonus.</summary>
    public readonly int WeaponDamage;

    /// <summary>obf: b — tool material.</summary>
    private readonly EnumToolMaterial _material;

    /// <summary>
    /// Spec: <c>ads(int itemId, int baseDamage, nu material, yy[] blocksArray)</c>.
    /// </summary>
    protected ItemTool(int id, int baseDamage, EnumToolMaterial material, int[] effectiveBlockIds)
        : base(id)
    {
        _material         = material;
        _effectiveBlockIds = effectiveBlockIds;
        MaxStackSize       = 1;
        SetInternalDurability(material.MaxUses);
        _efficiency        = material.Efficiency;
        WeaponDamage       = baseDamage + material.DamageBonus;
    }

    // ── Durability (spec §3.2, step 5) ──────────────────────────────────────

    /// <summary>Returns max durability for DamageItem calculations.</summary>
    public override int GetMaxDamage() => GetInternalDurabilityValue();

    // ── Mining speed (spec §3.3 getStrVsBlock) ───────────────────────────────

    /// <summary>
    /// obf: a(dk, yy) — getStrVsBlock.
    /// Iterates effective block IDs; returns efficiency if block matches, else 1.0F.
    /// Uses reference equality against Block.BlocksList[] singletons (spec §3.3 step 2).
    /// </summary>
    public override float GetMiningSpeed(ItemStack stack, Block block)
    {
        foreach (int id in _effectiveBlockIds)
        {
            if (id < Block.BlocksList.Length && Block.BlocksList[id] == block)
                return _efficiency;
        }
        return 1.0f;
    }

    // ── Entity hit (spec §3.3 hitEntity): cost 2 durability ─────────────────

    public override bool HitEntity(ItemStack stack, object target, object attacker)
    {
        stack.DamageItem(2);
        return true;
    }

    // ── Block break (spec §3.3 onBlockDestroyed): cost 1 durability ──────────

    public override bool OnBlockDestroyed(ItemStack stack, int x, int y, int z, object entity)
    {
        stack.DamageItem(1);
        return true;
    }

    // ── isItemTool (spec §3.3) ───────────────────────────────────────────────

    public override bool IsItemTool() => true;

    // ── Enchantability (spec §3.3 getEnchantability) ─────────────────────────

    public override int GetItemEnchantability() => _material.GetEnchantability();
}

// ── ItemShovel (adb) ──────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>adb</c> (ItemSpade/Shovel) — baseDamage = 1.
///
/// Effective blocks: grass(2), dirt(3), sand(12), gravel(13), snow_layer(78),
///   snow_block(80), clay(82), farmland(60), soul_sand(88), mycelium(110).
///
/// canHarvestBlock: only snow_layer(78) and snow_block(80) — spec §4, quirk 4.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §4
/// </summary>
public sealed class ItemShovel : ItemTool
{
    private static readonly int[] EffectiveIds = [2, 3, 12, 13, 78, 80, 82, 60, 88, 110];

    public ItemShovel(int id, EnumToolMaterial material) : base(id, 1, material, EffectiveIds) { }

    /// <summary>
    /// obf: a(yy) — canHarvestBlock. Only true for snow_layer(78) and snow_block(80).
    /// Quirk 4: all other shovel targets drop without needing a shovel.
    /// </summary>
    public override bool CanHarvestBlock(Block block)
        => block.BlockID is 78 or 80;
}

// ── ItemPickaxe (zu) ──────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>zu</c> (ItemPickaxe) — baseDamage = 2.
///
/// Effective blocks: 22 rock/metal block IDs (see spec §5.1 table).
/// canHarvestBlock: tier-gated by harvest level for obsidian, diamond, gold, iron, lapis,
///   redstone ores; general case = material is rock(RockTransp) or metal(RockTransp2).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §5
/// </summary>
public sealed class ItemPickaxe : ItemTool
{
    private static readonly int[] EffectiveIds =
    [
        4,   // cobblestone (stonebrick)
        43,  // stoneSlab_double
        44,  // stoneSlab
        1,   // stone
        24,  // sandstone
        48,  // mossyCobblestone
        15,  // oreIron
        42,  // blockIron
        16,  // oreCoal
        41,  // blockGold
        14,  // oreGold
        56,  // oreDiamond
        57,  // blockDiamond
        79,  // ice
        87,  // hellrock
        21,  // oreLapis
        22,  // blockLapis
        73,  // oreRedstone
        74,  // oreRedstone_lit
        66,  // rail
        28,  // detectorRail
        27,  // goldenRail
    ];

    private readonly int _harvestLevel;

    public ItemPickaxe(int id, EnumToolMaterial material) : base(id, 2, material, EffectiveIds)
        => _harvestLevel = material.HarvestLevel;

    /// <summary>
    /// obf: a(yy) — canHarvestBlock. Tier-gated for key ores; general rock/metal fallback.
    /// Spec §5.2.
    /// </summary>
    public override bool CanHarvestBlock(Block block)
    {
        int bid = block.BlockID;

        // Obsidian (49): diamond only (harvestLevel == 3)
        if (bid == 49)  return _harvestLevel == 3;

        // Diamond ore/block (56, 57): iron+ (level >= 2)
        if (bid is 56 or 57) return _harvestLevel >= 2;

        // Gold ore/block (14, 41): iron+ (level >= 2)
        if (bid is 14 or 41) return _harvestLevel >= 2;

        // Iron ore/block (15, 42): stone+ (level >= 1)
        if (bid is 15 or 42) return _harvestLevel >= 1;

        // Lapis ore/block (21, 22): stone+ (level >= 1)
        if (bid is 21 or 22) return _harvestLevel >= 1;

        // Redstone ore/lit (73, 74): iron+ (level >= 2)
        if (bid is 73 or 74) return _harvestLevel >= 2;

        // General case: rock or metal material (p.e / p.f)
        return block.BlockMaterial == Material.RockTransp
            || block.BlockMaterial == Material.RockTransp2;
    }
}

// ── ItemAxe (ago) ─────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ago</c> (ItemAxe) — baseDamage = 3.
///
/// Effective blocks: planks(5), bookshelf(47), log(17), chest(54),
///   stoneSlab_double(43), stoneSlab(44), pumpkin(86), lit_pumpkin(91).
///
/// getStrVsBlock override: any wood-material block mines at efficiency speed
///   (not just those in bR list) — spec §6.2, quirk 7.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §6
/// </summary>
public sealed class ItemAxe : ItemTool
{
    private static readonly int[] EffectiveIds = [5, 47, 17, 54, 43, 44, 86, 91];

    private readonly float _efficiency;

    public ItemAxe(int id, EnumToolMaterial material) : base(id, 3, material, EffectiveIds)
        => _efficiency = material.Efficiency;

    /// <summary>
    /// obf: a(dk, yy) — getStrVsBlock override.
    /// Wood-material block? → efficiency. Otherwise fall through to base (bR list check).
    /// Spec §6.2, quirk 7.
    /// </summary>
    public override float GetMiningSpeed(ItemStack stack, Block block)
    {
        if (block.BlockMaterial == Material.Plants)
            return _efficiency;
        return base.GetMiningSpeed(stack, block);
    }
}
