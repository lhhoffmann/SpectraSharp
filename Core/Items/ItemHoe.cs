namespace SpectraSharp.Core.Items;

/// <summary>
/// Replica of <c>wr</c> (ItemHoe) — direct Item subclass (NOT ItemTool).
/// Has no weapon damage and no effective blocks array.
///
/// Constructor (spec §7.2): maxStackSize=1, durability=material.MaxUses.
/// onItemUse (spec §7.3): tills grass(2) or dirt(3) into farmland(60) on server side.
///   - Grass requires top-face click with air above.
///   - Dirt can be tilled regardless of face/block above.
///   - Costs 1 durability per successful till.
///
/// Quirk: step sound always plays (even client-side); block change only server-side.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §7
/// </summary>
public sealed class ItemHoe : Item
{
    private readonly EnumToolMaterial _material;

    /// <summary>
    /// Spec: <c>wr(int itemId, nu material)</c>.
    /// </summary>
    public ItemHoe(int id, EnumToolMaterial material) : base(id)
    {
        _material    = material;
        MaxStackSize = 1;
        SetInternalDurability(material.MaxUses);
    }

    // ── Durability ───────────────────────────────────────────────────────────

    public override int GetMaxDamage() => GetInternalDurabilityValue();

    // ── onItemUse (spec §7.3) ────────────────────────────────────────────────

    /// <summary>
    /// Till grass or dirt into farmland on right-click.
    /// Steps: check eligibility, play step sound (stub), server-side block set + durability cost.
    /// </summary>
    public override bool OnItemUse(ItemStack stack, object player, World world, int x, int y, int z, int face)
    {
        int blockId    = world.GetBlockId(x, y, z);
        int blockAbove = world.GetBlockId(x, y + 1, z);

        // Tilling eligibility: grass(2) top-face with air above, or dirt(3) any face
        bool canTill = (face == 1 && blockAbove == 0 && blockId == 2) || blockId == 3;
        if (!canTill) return false;

        // Step sound stub — SoundManager not yet implemented
        // world.PlayStepSound(x + 0.5, y + 0.5, z + 0.5, farmlandStepSound);

        if (!world.IsClientSide)
        {
            world.SetBlock(x, y, z, 60); // farmland (ID 60)
            stack.DamageItem(1);
        }

        return true;
    }

    // ── isItemTool (spec §7.4) ────────────────────────────────────────────────

    public override bool IsItemTool() => true;

    // ── Enchantability ───────────────────────────────────────────────────────

    public override int GetItemEnchantability() => _material.GetEnchantability();
}
