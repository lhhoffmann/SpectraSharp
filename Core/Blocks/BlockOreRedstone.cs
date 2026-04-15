namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>oc</c> (BlockOreRedstone) — Block IDs 73 (normal) and 74 (glowing).
/// Touching or mining normal ore switches it to glowing temporarily.
/// Glowing ore reverts to normal after a random tick (~30 ticks).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §10
/// </summary>
public sealed class BlockOreRedstone : Block
{
    private readonly bool _isGlowing; // obf: a

    public BlockOreRedstone(int id, bool isGlowing) : base(id, 51, Material.RockTransp)
    {
        _isGlowing = isGlowing;
        SetHardness(3.0f).SetResistance(5.0f);
        SetStepSound(SoundStoneHighPitch);
        if (isGlowing) SetLightValue(0.625f); // light 9
        SetBlockName("oreRedstone");
    }

    // ── Activation triggers (spec §10.3) ──────────────────────────────────────

    public override void OnBlockDestroyedByPlayer(IWorld world, int x, int y, int z, int meta)
        => TryActivate(world, x, y, z);

    public override void OnEntityWalking(IWorld world, int x, int y, int z, Entity entity)
        => TryActivate(world, x, y, z);

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        TryActivate(world, x, y, z);
        return false; // pass activation through
    }

    private void TryActivate(IWorld world, int x, int y, int z)
    {
        if (!_isGlowing && !world.IsClientSide)
            world.SetBlock(x, y, z, 74); // switch to glowing variant (ID 74)
        // Particle stub: spawn reddust particles (visual only)
    }

    // ── randomTick (spec §10.6) ───────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (_isGlowing && !world.IsClientSide)
            world.SetBlock(x, y, z, 73); // revert to normal ore
    }

    // ── Drops (spec §10.8) ────────────────────────────────────────────────────

    /// <summary>Always drops redstone dust item. Spec §10.8.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => Item.ItemsList[331]?.RegistryIndex ?? 331; // redstone dust ID 331

    public override int QuantityDropped(JavaRandom rng) => 4 + rng.NextInt(2); // 4 or 5

    public override int QuantityDroppedWithBonus(int fortune, JavaRandom rng)
        => QuantityDropped(rng) + rng.NextInt(fortune + 1);
}
