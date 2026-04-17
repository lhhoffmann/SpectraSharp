namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>abo</c> (ItemShears) — shears tool for harvesting leaves, plants, wool, and vines.
///
/// Item ID: 359 (itemId=103). Stack size: 1. Durability: 238.
///
/// Mining speed: 15.0 for tall grass + leaves, 5.0 for wool.
/// CanHarvestBlock: true for tall grass (ID 31) only — cobweb handled by its block.
/// Block drops (leaves/vines/cobweb/dead bush): handled in respective Block subclasses
///   which check the harvesting tool via `block.BonemealGrow` or dedicated flags.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemShears_Spec.md
/// </summary>
public sealed class ItemShears : Item
{
    public ItemShears() : base(103) // RegistryIndex = 359
    {
        MaxStackSize = 1;
        SetInternalDurability(238);
        SetUnlocalizedName("shears");
    }

    // ── Mining speed (spec §4) ─────────────────────────────────────────────────

    public override float GetMiningSpeed(ItemStack stack, Block block)
    {
        return block.BlockID switch
        {
            31 or 18 => 15.0f, // tall grass / leaves
            35       => 5.0f,  // wool
            _        => base.GetMiningSpeed(stack, block)
        };
    }

    // ── Can harvest (spec §3) ─────────────────────────────────────────────────

    /// <summary>Returns true for tall grass (ID 31) — allows normal-speed harvest.</summary>
    public override bool CanHarvestBlock(Block block) => block.BlockID == 31;

    // ── Durability (spec §2) ──────────────────────────────────────────────────

    /// <summary>Damage shears by 1 on every block break.</summary>
    public override bool OnBlockDestroyed(ItemStack stack, int x, int y, int z, object entity)
    {
        stack.DamageItem(1);
        return true;
    }
}
