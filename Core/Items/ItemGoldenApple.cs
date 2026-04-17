namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>afk</c> (ItemGoldenApple) — golden apple, always edible, EPIC rarity.
///
/// Item ID: 322 (itemId=66). Stack size: 64. Extends <see cref="ItemFood"/>.
///
/// Always edible (can be eaten at full hunger).
/// On eat: applies Regeneration II (ID 10, 600 ticks, amplifier 1) with 100% probability.
/// Tooltip name renders in light purple (EPIC rarity).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemGoldenApple_Spec.md
/// </summary>
public sealed class ItemGoldenApple : ItemFood
{
    public ItemGoldenApple()
        : base(66, 4, 1.2f, false) // itemId=66, heal=4, satMod=1.2F, wolfFood=false
    {
        SetAlwaysEdible();
        SetOnEatPotion(10 /* Regeneration */, 30 /* seconds → 600 ticks */, 1 /* amplifier: level II */, 1.0f);
        SetUnlocalizedName("goldenApple");
    }

    // ── Rarity (spec §3.2) ────────────────────────────────────────────────────

    /// <summary>Returns <see cref="ItemRarity.Epic"/> — purple tooltip. obf: <c>d(dk)</c></summary>
    public override ItemRarity GetRarity(ItemStack? stack = null) => ItemRarity.Epic;
}
