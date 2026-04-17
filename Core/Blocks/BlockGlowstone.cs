namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>sk</c> (BlockGlowstone) — Block ID 89.
/// Drops 2–4 glowstone dust (item ID 348) with fortune-capped behavior.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockGlowstone_Spec.md
/// </summary>
public sealed class BlockGlowstone : Block
{
    public BlockGlowstone() : base(89, 105, Material.MatPass_Q)
    {
        SetHardness(0.3f);
        SetLightValue(1.0f);
        SetStepSound(SoundGlass);
        SetBlockName("lightgem");
    }

    // ── Drop count (spec §3) ─────────────────────────────────────────────────

    /// <summary>Base drop count: 2 + rand(3) → range [2, 4]. Spec: <c>sk.a(Random)</c>.</summary>
    public override int QuantityDropped(JavaRandom rng) => 2 + rng.NextInt(3);

    /// <summary>
    /// Fortune-modified drop count: clamp(base + rand(fortune+1), 1, 4).
    /// Spec: <c>sk.a(int fortune, Random)</c>.
    /// </summary>
    public override int QuantityDroppedWithBonus(int fortune, JavaRandom rng)
    {
        int baseCount = QuantityDropped(rng) + rng.NextInt(fortune + 1);
        return Math.Clamp(baseCount, 1, 4);
    }

    /// <summary>Drop item: Glowstone Dust (ID 348). Spec: <c>sk.a(int, Random, int meta)</c>.</summary>
    public override int IdDropped(int metadata, JavaRandom rng, int fortune) => 348;
}
