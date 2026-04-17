namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>abk</c> (ItemPotion) — drinkable and splash potions.
///
/// Item ID: 373 (constructor arg 117 → 256+117).
/// Stack size: 1.
/// Metadata bit 14 = splash flag.
/// Drinking returns an empty glass bottle (ID 374 = 256+118).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PotionEffect_Spec.md §7
/// </summary>
public sealed class ItemPotion : Item
{
    private const int GlassBottleId = 374; // acy.bs: 256+118

    public ItemPotion() : base(117) // RegistryIndex = 373
    {
        MaxStackSize = 1;
        SetUnlocalizedName("potion");
    }

    // ── Splash detection (spec §7.2) ─────────────────────────────────────────

    /// <summary>Returns true when bit 14 of metadata is set (splash potion). obf: <c>e(int meta)</c>.</summary>
    public static bool IsSplash(int meta) => (meta & 16384) != 0;

    // ── Texture (spec §7.3) ──────────────────────────────────────────────────

    /// <summary>Splash=154, drinkable=140.</summary>
    public override int GetIconIndex(int meta) => IsSplash(meta) ? 154 : 140;

    // ── Right-click (spec §7.5) ───────────────────────────────────────────────

    /// <summary>
    /// If splash: spawn EntityPotion (stub — ThrowableEntities_Spec pending) and consume.
    /// If drinkable: start 32-tick drink animation.
    /// </summary>
    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is not EntityPlayer ep) return stack;

        if (IsSplash(stack.Damage))
        {
            // EntityPotion spawn stub — ThrowableEntities_Spec pending
            if (!world.IsClientSide)
                stack.StackSize--;
            return stack.StackSize <= 0 ? null! : stack;
        }

        // Drinkable — start eat/drink animation
        ep.StartUsingItem(stack, 32);
        return stack;
    }

    /// <summary>obf: <c>b(dk)</c> — 32 ticks drink duration.</summary>
    public override int GetMaxItemUseDuration(ItemStack stack) => 32;

    // ── Finish drinking (spec §7.4) ───────────────────────────────────────────

    /// <summary>
    /// Called when the 32-tick drink animation completes.
    /// Applies effects from PotionHelper and returns a glass bottle.
    /// </summary>
    public override ItemStack FinishUsingItem(ItemStack stack, World world, object player)
    {
        if (player is EntityPlayer ep && !world.IsClientSide)
        {
            var effects = PotionHelper.GetEffectsFromMeta(stack.Damage, isSplash: false);
            foreach (var fx in effects)
                ep.AddPotionEffect(new PotionEffect(fx.EffectId, fx.Duration, fx.Amplifier));
        }

        stack.StackSize--;
        if (stack.StackSize <= 0)
            return new ItemStack(GlassBottleId);

        if (player is EntityPlayer ep2)
            ep2.Inventory.AddItemStackToInventory(new ItemStack(GlassBottleId));
        return stack;
    }
}
