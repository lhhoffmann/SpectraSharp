namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>pk</c> (PotionHelper) — decodes potion metadata into a list of active effects.
///
/// The full metadata decode algorithm encodes the brewing recipe via bit-fields and formula strings.
/// This is a functional stub that returns an empty list until the full decode algorithm is specified.
///
/// Metadata bit layout (spec §8):
///   bits 0–5  = ingredient combination
///   bits 6–7  = tier (extended/upgraded flags)
///   bit 14    = splash flag
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PotionEffect_Spec.md §8
/// </summary>
public static class PotionHelper
{
    /// <summary>
    /// obf: <c>pk.b(int meta, boolean isSplash)</c> — returns effect list for a given metadata.
    /// Stub: returns an empty list until the full formula-string decoder is implemented.
    /// </summary>
    public static IReadOnlyList<PotionEffect> GetEffectsFromMeta(int meta, bool isSplash)
    {
        // Full decode algorithm (formula strings) is noted as complex in spec OQ §8 and deferred.
        // When Container_Spec + BrewingStand are fully spec'd, this should be replaced.
        return Array.Empty<PotionEffect>();
    }
}
