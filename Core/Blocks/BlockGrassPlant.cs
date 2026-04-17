namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>wg</c> (BlockGrassPlant) — abstract base class for decorative plant
/// blocks that sit on the ground (tall grass, dead bush, ferns, flowers).
///
/// Enforces valid-soil and light/sky checks every neighbor-update and random tick.
/// If either check fails the plant removes itself and drops its item.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockGrassPlant_Spec.md
/// </summary>
public abstract class BlockGrassPlant : Block
{
    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    protected BlockGrassPlant(int id, int textureIndex) : base(id, textureIndex, Material.Plants)
    {
        SetHardness(0.0f);
        SetStepSound(SoundGrass);
        // NeedsRandomTick = true by default (spec §2 tickable)

        // AABB: (0.3, 0.0, 0.3, 0.7, 0.6, 0.7) — spec §3
        const float v = 0.2f;
        SetBounds(0.5f - v, 0.0f, 0.5f - v, 0.5f + v, v * 3.0f, 0.5f + v);
    }

    // ── Properties (spec §2) ─────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 1; // cross sprite
    public override bool RenderAsNormalBlock() => false;

    // Block has no collision box — returns null so entities pass through.
    // (AABB is visual only; collision box is null in vanilla.)
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => null;

    // ── Valid soil check (spec §4) ────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="blockId"/> is a valid soil block.
    /// Valid soils: Grass (2), Dirt (3), Farmland (60).
    /// Spec: <c>wg.d(int blockId)</c>.
    /// </summary>
    protected virtual bool IsValidSoil(int blockId)
        => blockId == 2 || blockId == 3 || blockId == 60;

    // ── Placement validity (spec §5) ─────────────────────────────────────────

    /// <summary>
    /// Returns true if the plant can be placed at (x, y, z).
    /// Spec: <c>wg.c(World, x, y, z)</c>.
    /// </summary>
    public override bool CanBlockStay(IWorld world, int x, int y, int z)
    {
        if (!IsValidSoil(world.GetBlockId(x, y - 1, z))) return false;
        return base.CanBlockStay(world, x, y, z);
    }

    // ── Survival check (spec §6) ─────────────────────────────────────────────

    /// <summary>
    /// Returns true if the plant can survive at its current position.
    /// Conditions (both required):
    ///   1. Block light ≥ 8 OR sky is directly visible above (quirk 9.1).
    ///   2. Valid soil directly below.
    /// Spec: <c>wg.e(World, x, y, z)</c>.
    /// </summary>
    protected virtual bool CanSurviveAt(IWorld world, int x, int y, int z)
    {
        bool hasSoil  = IsValidSoil(world.GetBlockId(x, y - 1, z));
        if (!hasSoil) return false;

        // Light check: combined light >= 8 OR sky visible (sky light value == 15 at y means open sky)
        bool enoughLight = world.GetLightBrightness(x, y, z) >= 8;
        bool canSeeSky   = world is World concreteWorld
            && concreteWorld.GetHeightValue(x, z) <= y;
        return enoughLight || canSeeSky;
    }

    // ── Removal (spec §7) ────────────────────────────────────────────────────

    /// <summary>
    /// If the plant cannot survive, drops its item and sets the block to air.
    /// Spec: <c>wg.h(World, x, y, z)</c>.
    /// </summary>
    private void RemoveIfUnsurvivable(IWorld world, int x, int y, int z)
    {
        if (!CanSurviveAt(world, x, y, z))
        {
            // Drop items (pass current meta; 100% drop chance, no fortune)
            DropBlockAsItem(world, x, y, z, world.GetBlockMetadata(x, y, z), 0);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── Neighbor update (spec §8) ─────────────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborBlockId)
    {
        base.OnNeighborBlockChange(world, x, y, z, neighborBlockId);
        RemoveIfUnsurvivable(world, x, y, z);
    }

    // ── Random tick (spec §8) ─────────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        RemoveIfUnsurvivable(world, x, y, z);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>kv</c> (BlockTallGrass) — Block ID 31. Texture 39.
/// Subtypes via metadata: 0=shrub, 1=tallgrass, 2=fern.
/// Source spec: BlockGrassPlant_Spec.md
/// </summary>
public sealed class BlockTallGrass : BlockGrassPlant
{
    public BlockTallGrass() : base(31, 39) => SetBlockName("tallgrass");
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>jl</c> (BlockDeadBush) — Block ID 32. Texture 55.
/// Can only stand on sand (ID 12) in addition to the standard soils.
/// Source spec: BlockGrassPlant_Spec.md
/// </summary>
public sealed class BlockDeadBush : BlockGrassPlant
{
    public BlockDeadBush() : base(32, 55) => SetBlockName("deadbush");

    /// <summary>Dead bush can only stand on sand (or standard soils).</summary>
    protected override bool IsValidSoil(int blockId)
        => blockId == 12 || base.IsValidSoil(blockId);
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>wg</c> subclass — Dandelion (yellow flower). Block ID 37. Texture 13.
/// Source spec: BlockGrassPlant_Spec.md
/// </summary>
public sealed class BlockDandelion : BlockGrassPlant
{
    public BlockDandelion() : base(37, 13) => SetBlockName("flower");
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>wg</c> subclass — Rose (red flower). Block ID 38. Texture 12.
/// Source spec: BlockGrassPlant_Spec.md
/// </summary>
public sealed class BlockRose : BlockGrassPlant
{
    public BlockRose() : base(38, 12) => SetBlockName("rose");
}
