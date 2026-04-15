namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>bg</c> (BlockTorch) — base class for regular torch (ID 50) and
/// redstone torch (IDs 75/76). Handles placement meta encoding, canBlockStay,
/// neighbor-change removal, and AABB.
///
/// Meta encoding (spec §4.2):
///   1 = west wall, 2 = east wall, 3 = north wall, 4 = south wall, 5 = floor
///
/// Material: p.p (passable). isOpaqueCube=false. renderAsNormal=false.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §4
/// </summary>
public class BlockTorchBase : Block
{
    // ── Construction (spec §4.1) ──────────────────────────────────────────────

    protected BlockTorchBase(int id, int textureIndex) : base(id, textureIndex, Material.Plants)
    {
        SetHardness(0.0f);
        ClearNeedsRandomTick();
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;
    public override int GetMobilityFlag() => 1; // can be pushed by pistons

    // ── Bounds — no collision box (spec §4.6) ────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z) => null;

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        return meta switch
        {
            1 => AxisAlignedBB.GetFromPool(x + 0.0f, y + 0.2f, z + 0.35f, x + 0.3f,  y + 0.8f, z + 0.65f),
            2 => AxisAlignedBB.GetFromPool(x + 0.7f, y + 0.2f, z + 0.35f, x + 1.0f,  y + 0.8f, z + 0.65f),
            3 => AxisAlignedBB.GetFromPool(x + 0.35f, y + 0.2f, z + 0.0f, x + 0.65f, y + 0.8f, z + 0.3f),
            4 => AxisAlignedBB.GetFromPool(x + 0.35f, y + 0.2f, z + 0.7f, x + 0.65f, y + 0.8f, z + 1.0f),
            _ => AxisAlignedBB.GetFromPool(x + 0.4f,  y + 0.0f, z + 0.4f, x + 0.6f,  y + 0.6f, z + 0.6f),
        };
    }

    // ── canBlockStay (spec §4.4) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => CanSupportTorch(world, x - 1, y, z)
        || CanSupportTorch(world, x + 1, y, z)
        || CanSupportTorch(world, x, y, z - 1)
        || CanSupportTorch(world, x, y, z + 1)
        || CanSupportFloor(world, x, y - 1, z);

    // ── onNeighborBlockChange (spec §4.5) ─────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            int meta = world.GetBlockMetadata(x, y, z);
            DropBlockAsItemWithChance(world, x, y, z, meta, 1.0f, 0);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>True if the block at (x,y,z) has a solid face that can hold a wall torch.</summary>
    protected static bool CanSupportTorch(IBlockAccess world, int x, int y, int z)
        => Block.IsOpaqueCubeArr[world.GetBlockId(x, y, z) & 0xFF];

    /// <summary>
    /// Floor check: solid block (isNormalCube), or bed foot (ID 26, meta&8=0), or bed head (ID 26, meta&8=8).
    /// obf: <c>g(ry,x,y,z)</c>. Spec §4.3.
    /// </summary>
    protected static bool CanSupportFloor(IBlockAccess world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        if (id == 26) return true; // any bed half can support floor torch
        return Block.IsOpaqueCubeArr[id & 0xFF];
    }
}
