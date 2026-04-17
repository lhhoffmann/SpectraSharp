namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>hd</c> (ItemFishingRod) — item that casts/reels an <see cref="EntityFishHook"/>.
/// Item ID 346. Durability 64. Max stack 1.
///
/// Usage:
///   Right-click with no hook: cast (spawn EntityFishHook, play sound, swing arm).
///   Right-click with hook active: reel in (call hook.ReelIn(), damage rod, swing arm).
///
/// Durability cost on reel-in (spec §7.3):
///   0 = empty reel   1 = fish caught   2 = hook stuck in block   3 = entity hooked
///
/// Quirks preserved (spec §9.7):
///   Player arm swings in BOTH cast and reel-in paths.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md §5.5
/// </summary>
public sealed class ItemFishingRod : Item
{
    public ItemFishingRod(int id) : base(id)
    {
        SetInternalDurability(64);
        MaxStackSize = 1;
    }

    public override int GetMaxDamage() => 64;

    /// <summary>
    /// obf: <c>c(dk, ry, vi)</c> — onItemRightClick.
    /// If hook exists: reel in. Otherwise: cast a new hook.
    /// </summary>
    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is not EntityPlayer ep) return stack;

        if (ep.FishHook != null)
        {
            // ── Reel-in path ──────────────────────────────────────────────
            int cost = ep.FishHook.ReelIn();
            stack.DamageItem(cost);
            ep.SwingArm(); // player.m_()
        }
        else
        {
            // ── Cast path ─────────────────────────────────────────────────
            // Sound stub: "random.bow" at volume 0.5F
            world.PlaySoundAt(ep, "random.bow", 0.5f,
                0.4f / (world.Random.NextFloat() * 0.4f + 0.8f));

            if (!world.IsClientSide)
                world.SpawnEntity(new EntityFishHook(world, ep));

            ep.SwingArm(); // player.m_()
        }

        return stack;
    }
}
