namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>il</c> (ItemBow) — charge-based ranged weapon. Item ID 261.
///
/// Usage: hold right-click to charge; release to fire an <see cref="EntityArrow"/>.
/// Requires at least one arrow (ID 262) in inventory, or creative mode.
///
/// Charge formula (spec §7.2):
///   f = ticksCharged / 20.0
///   power = (f² + f×2) / 3.0     → clamp to [0.0, 1.0]
///   arrowSpeed = power × 2.0     → passed to EntityArrow constructor (×1.5 inside = 3.0 max)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md §5.1
/// </summary>
public sealed class ItemBow : Item
{
    // Arrow item registry index (acy.k.bM = 256 + 6 = 262)
    private const int ArrowItemId = 262;

    public ItemBow(int id) : base(id)
    {
        MaxStackSize = 1;
        SetInternalDurability(384); // il.i(384)
    }

    public override int GetMaxDamage() => 384;

    /// <summary>
    /// obf: <c>b(dk)</c> — max use duration 72000 ticks (effectively unlimited).
    /// </summary>
    public override int GetMaxItemUseDuration(ItemStack stack) => 72000;

    /// <summary>
    /// obf: <c>c(dk, ry, vi)</c> — onItemRightClick. Starts the charge if player has arrows.
    /// </summary>
    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is not EntityPlayer ep) return stack;

        if (ep.Abilities.Invulnerable || ep.Inventory.HasItem(ArrowItemId))
            ep.StartUsingItem(stack, 72000);

        return stack;
    }

    /// <summary>
    /// obf: <c>a(dk, ry, vi, int)</c> — onPlayerStoppedUsing. Fires the arrow on release.
    /// Called when right-click is released; remainingTicks = 72000 minus ticks held.
    /// </summary>
    public override void OnPlayerStoppedUsing(ItemStack stack, World world, object player, int remainingTicks)
    {
        if (player is not EntityPlayer ep) return;

        int   ticksCharged    = 72000 - remainingTicks;
        float chargeFraction  = ticksCharged / 20.0f;
        float power           = (chargeFraction * chargeFraction + chargeFraction * 2.0f) / 3.0f;

        if (power < 0.1f) return; // below minimum threshold — no shot

        if (power > 1.0f) power = 1.0f;

        // Require arrows or creative mode (same check as onItemRightClick)
        if (!ep.Abilities.Invulnerable && !ep.Inventory.HasItem(ArrowItemId)) return;

        // Create and configure arrow
        var arrow = new EntityArrow(world, ep, power * 2.0f);

        if (power >= 1.0f) arrow.IsCritical = true;

        // Damage bow (always 1 per shot)
        stack.DamageItem(1);

        // Sound stub: "random.bow"
        world.PlaySoundAt(ep, "random.bow", 1.0f,
            1.0f / (ep.EntityRandom.NextFloat() * 0.4f + 1.2f) + power * 0.5f);

        // Consume one arrow (non-creative only)
        if (!ep.Abilities.Invulnerable)
            ep.Inventory.ConsumeItem(ArrowItemId);

        // Spawn arrow server-side
        if (!world.IsClientSide)
            world.SpawnEntity(arrow);
    }

    /// <summary>
    /// obf: <c>a(dk, vi, ry, ...)</c> — onItemUse on block. Always false (cannot place block).
    /// </summary>
    public override bool OnItemUse(ItemStack stack, object player, World world,
                                   int x, int y, int z, int face) => false;
}
