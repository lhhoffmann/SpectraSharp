namespace SpectraEngine.Core.Enchantments;

/// <summary>
/// Replica of <c>vs</c> (EnchantmentData) — pairs an enchantment with its applied level.
/// Produced by <see cref="EnchantmentHelper.SelectEnchantments"/> and consumed by
/// <see cref="ContainerEnchantment"/>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §6
/// </summary>
public sealed class EnchantmentData
{
    /// <summary>The enchantment to apply.</summary>
    public readonly Enchantment Enchantment;

    /// <summary>The level at which the enchantment was selected.</summary>
    public readonly int Level;

    public EnchantmentData(Enchantment enchantment, int level)
    {
        Enchantment = enchantment;
        Level       = level;
    }
}
