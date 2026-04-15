namespace SpectraSharp.Core.Items;

/// <summary>
/// Replica of <c>zp</c> (ItemSword) — direct Item subclass (NOT ItemTool).
///
/// Fields (spec §8.1): a=weaponDamage=4+material.DamageBonus, b=material.
/// Constructor (spec §8.2): maxStackSize=1, durability=material.MaxUses, damage=4+DamageBonus.
///
/// Mining speed (spec §8.3): cobweb(30)=15.0F, all other blocks=1.5F.
/// hitEntity cost: 1 durability (quirk 1 — opposite of tools which cost 2).
/// onBlockDestroyed cost: 2 durability (quirk 1).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §8
/// </summary>
public sealed class ItemSword : Item
{
    /// <summary>obf: a — weapon damage = 4 + material.DamageBonus.</summary>
    public readonly int WeaponDamage;

    /// <summary>obf: b — tool material.</summary>
    private readonly EnumToolMaterial _material;

    /// <summary>
    /// Spec: <c>zp(int itemId, nu material)</c>.
    /// </summary>
    public ItemSword(int id, EnumToolMaterial material) : base(id)
    {
        _material    = material;
        MaxStackSize = 1;
        SetInternalDurability(material.MaxUses);
        WeaponDamage = 4 + material.DamageBonus;
    }

    // ── Durability ───────────────────────────────────────────────────────────

    public override int GetMaxDamage() => GetInternalDurabilityValue();

    // ── Mining speed (spec §8.3) ─────────────────────────────────────────────

    /// <summary>
    /// obf: a(dk, yy) — cobweb(ID 30)=15.0F, everything else=1.5F.
    /// </summary>
    public override float GetMiningSpeed(ItemStack stack, Block block)
        => block.BlockID == 30 ? 15.0f : 1.5f;

    // ── hitEntity: cost 1 durability (spec §8.3, quirk 1) ───────────────────

    public override bool HitEntity(ItemStack stack, object target, object attacker)
    {
        stack.DamageItem(1);
        return true;
    }

    // ── onBlockDestroyed: cost 2 durability (spec §8.3, quirk 1) ─────────────

    public override bool OnBlockDestroyed(ItemStack stack, int x, int y, int z, object entity)
    {
        stack.DamageItem(2);
        return true;
    }

    // ── canHarvestBlock: only cobweb (spec §8.3) ─────────────────────────────

    public override bool CanHarvestBlock(Block block) => block.BlockID == 30;

    // ── isItemTool (spec §8.3) ───────────────────────────────────────────────

    public override bool IsItemTool() => true;

    // ── Enchantability (spec §8.3) ───────────────────────────────────────────

    public override int GetItemEnchantability() => _material.GetEnchantability();
}
