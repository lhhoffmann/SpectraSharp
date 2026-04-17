namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>uh</c> (BlockPane) — Block IDs 101 (iron bars) and 102 (glass pane).
/// Forms a thin cross-pillar that extends toward adjacent connecting blocks.
///
/// The <paramref name="dropsItem"/> flag distinguishes the two variants:
///   true  = iron bars (drops itself on break)
///   false = glass pane (drops nothing)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPane_Spec.md
/// </summary>
public sealed class BlockPane : Block
{
    private readonly bool _dropsItem;

    public BlockPane(int id, int textureIndex, Material material, bool dropsItem)
        : base(id, textureIndex, material)
    {
        _dropsItem = dropsItem;
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 18;
    public override bool RenderAsNormalBlock() => false;

    // ── Connection logic (spec §4) ────────────────────────────────────────────

    /// <summary>
    /// Returns true if this pane connects to the block with the given ID.
    /// Connects to: opaque solid blocks, same block type, or full glass (ID 20).
    /// Spec: <c>uh.e(int blockId)</c>.
    /// </summary>
    private bool CanConnect(int blockId)
        => IsOpaqueCubeArr[blockId]
        || blockId == BlockID
        || blockId == 20; // glass block (yy.M)

    // ── AABB / Collision (spec §5) ────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        const float lo = 0.4375f;
        const float hi = 0.5625f;
        return AxisAlignedBB.GetFromPool(x + lo, y, z + lo, x + hi, y + 1, z + hi);
    }

    public override void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB queryBox, List<AxisAlignedBB> list)
    {
        bool west  = CanConnect(world.GetBlockId(x - 1, y, z));
        bool east  = CanConnect(world.GetBlockId(x + 1, y, z));
        bool north = CanConnect(world.GetBlockId(x, y, z - 1));
        bool south = CanConnect(world.GetBlockId(x, y, z + 1));

        const float lo = 0.4375f;
        const float hi = 0.5625f;

        if (west || east)
        {
            float minX = west ? 0.0f : 0.5f;
            float maxX = east ? 1.0f : 0.5f;
            AddIfIntersects(x + minX, y, z + lo, x + maxX, y + 1, z + hi, queryBox, list);
        }

        if (north || south)
        {
            float minZ = north ? 0.0f : 0.5f;
            float maxZ = south ? 1.0f : 0.5f;
            AddIfIntersects(x + lo, y, z + minZ, x + hi, y + 1, z + maxZ, queryBox, list);
        }

        if (!west && !east && !north && !south)
        {
            // Standalone centre pillar
            AddIfIntersects(x + lo, y, z + lo, x + hi, y + 1, z + hi, queryBox, list);
        }
    }

    private static void AddIfIntersects(
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ,
        AxisAlignedBB query, List<AxisAlignedBB> list)
    {
        var bb = AxisAlignedBB.GetFromPool(minX, minY, minZ, maxX, maxY, maxZ);
        if (query.Intersects(bb))
            list.Add(bb);
    }

    // ── Drops (spec §7) ─────────────────────────────────────────────────────

    /// <summary>
    /// Glass pane drops nothing; iron bars drop themselves.
    /// Spec: <c>uh.a(int fortune, Random, int meta)</c>.
    /// </summary>
    public override int QuantityDropped(JavaRandom rng) => _dropsItem ? 1 : 0;
}
