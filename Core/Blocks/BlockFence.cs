namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>nz</c> (BlockFence) — tall thin post that expands toward adjacent
/// connecting blocks, with a 1.5-unit collision height to prevent jumping over.
///
/// Fence block IDs: 85 (oak wood), 113 (nether brick).
/// Fence gate (ID 107) is NOT the same class but IS included in connectivity.
///
/// Connectivity: same fence type, fence gate (ID 107), or any solid full-cube non-glass block.
/// Nether brick fence (113) does NOT connect to oak fence (85) — IDs differ (spec §8 quirk 2).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFence_Spec.md
/// </summary>
public class BlockFence : Block
{
    // ── Constructors (spec §3) ────────────────────────────────────────────────

    /// <summary>2-arg: defaults to wood material (<c>p.d</c>).</summary>
    public BlockFence(int id, int textureIndex)
        : base(id, textureIndex, Material.Plants)
    {
        Init();
    }

    /// <summary>3-arg: custom material (used by nether brick fence with stone material).</summary>
    public BlockFence(int id, int textureIndex, Material material)
        : base(id, textureIndex, material)
    {
        Init();
    }

    private void Init()
    {
        SetLightOpacity(11); // spec §4.3
    }

    // ── Properties (spec §4.1–4.3) ───────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — isOpaqueCube. Always false.</summary>
    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 11;

    /// <summary>obf: <c>b()</c> — renderAsNormal. Always false.</summary>
    public override bool RenderAsNormalBlock() => false;

    // ── Connectivity check (spec §4.4) ────────────────────────────────────────

    /// <summary>
    /// obf: <c>c(kq world, int x, int y, int z)</c> — returns true if the block at
    /// (x,y,z) causes this fence to extend toward it.
    /// </summary>
    protected bool CanFenceConnect(IBlockAccess world, int x, int y, int z)
    {
        int blockId = world.GetBlockId(x, y, z);

        // Same fence type connects
        if (blockId == BlockID) return true;

        // Fence gate (ID 107 = yy.bv) hard-coded connection (spec §8 quirk 4)
        if (blockId == 107) return true;

        Block? block = BlocksList[blockId];
        if (block == null) return false;

        // Must be: solid material + renderAsNormal + NOT glass material
        return block.BlockMaterial?.IsSolid() == true
            && block.RenderAsNormalBlock()
            && block.BlockMaterial != Material.Mat_Y; // p.y = glass — excluded (spec §8 quirk 3)
    }

    // ── Dynamic collision box (spec §4.5) — world-space, height 1.5 ──────────

    /// <summary>
    /// obf: <c>b(ry world, int x, int y, int z)</c> — world-space AABB for physics collision.
    /// Height is always 1.5F above the block's base Y (spec §7 / quirk 1).
    /// </summary>
    public override AxisAlignedBB GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        bool south = CanFenceConnect(world, x,     y, z - 1);
        bool north = CanFenceConnect(world, x,     y, z + 1);
        bool west  = CanFenceConnect(world, x - 1, y, z    );
        bool east  = CanFenceConnect(world, x + 1, y, z    );

        double xMin = south || north ? (west ? 0.0 : 0.375) : 0.375;
        double xMax = south || north ? (east ? 1.0 : 0.625) : 0.625;
        double zMin = west  || east  ? (south ? 0.0 : 0.375) : 0.375;
        double zMax = west  || east  ? (north ? 1.0 : 0.625) : 0.625;

        // Recompute properly per spec
        xMin = west  ? 0.0 : 0.375;
        xMax = east  ? 1.0 : 0.625;
        zMin = south ? 0.0 : 0.375;
        zMax = north ? 1.0 : 0.625;

        return AxisAlignedBB.GetFromPool(
            x + xMin, y,
            z + zMin,
            x + xMax, y + 1.5,
            z + zMax);
    }

    // ── Render/selection AABB (spec §4.6) — local-space, height 1.0 ──────────

    /// <summary>
    /// obf: <c>b(kq world, int x, int y, int z)</c> — sets local-space AABB for rendering/selection.
    /// Height 1.0F (selection outline 1 block tall — spec §8 quirk 1).
    /// </summary>
    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        bool south = CanFenceConnect(world, x,     y, z - 1);
        bool north = CanFenceConnect(world, x,     y, z + 1);
        bool west  = CanFenceConnect(world, x - 1, y, z    );
        bool east  = CanFenceConnect(world, x + 1, y, z    );

        float xMin = west  ? 0.0f : 0.375f;
        float xMax = east  ? 1.0f : 0.625f;
        float zMin = south ? 0.0f : 0.375f;
        float zMax = north ? 1.0f : 0.625f;

        SetBounds(xMin, 0.0f, zMin, xMax, 1.0f, zMax); // height 1.0 for selection (spec §4.6)
    }
}
