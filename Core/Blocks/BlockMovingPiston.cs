using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>qz</c> (BlockMovingPiston) — Block ID 36.
///
/// An invisible proxy block placed during piston animation. Each instance stores the
/// original block (and its metadata) in a <see cref="TileEntityPiston"/> attached to
/// the same position. Replaced by the original block when the animation finalizes.
///
/// Key behaviours:
///   - Hardness −1.0F → cannot be mined while animating.
///   - Drop: drops the stored block (reads from TileEntityPiston).
///   - Collision AABB: offset from base block AABB by progress × facing direction.
///   - IsBlockContainer → blocks are not pushable while animating (spec §7.7 rule 6).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPiston_Spec.md §7.16-§7.17 / §7.22
/// </summary>
public sealed class BlockMovingPiston : Block
{
    // ── Direction data (ot arrays, spec §3) ───────────────────────────────────

    private static readonly int[] DirX = {  0, 0,  0, 0, -1, 1 }; // ot.b
    private static readonly int[] DirY = { -1, 1,  0, 0,  0, 0 }; // ot.c
    private static readonly int[] DirZ = {  0, 0, -1, 1,  0, 0 }; // ot.d

    // ── Construction ──────────────────────────────────────────────────────────

    public BlockMovingPiston(int id) : base(id, 0, Material.RockTransp)
    {
        SetHardness(-1.0f);      // cannot be mined
        SetBlockName("movingPiston");
        SetHasTileEntity();      // required for TileEntityPiston
        SetIsContainer(true);    // prevents pushing (spec §7.7 rule 6)
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Drop (spec §7.16) ─────────────────────────────────────────────────────

    /// <summary>
    /// Drops the stored block as an item. Spec: <c>qz.a(ry, x, y, z, meta, chance, fortune)</c>.
    /// </summary>
    public override void DropBlockAsItemWithChance(IWorld world, int x, int y, int z, int meta, float chance, int fortune)
    {
        if (world.IsClientSide) return;
        if (world.GetTileEntity(x, y, z) is not TileEntityPiston te) return;
        if (te.StoredBlockId == 0) return;

        Block? stored = BlocksList[te.StoredBlockId];
        stored?.DropBlockAsItemWithChance(world, x, y, z, te.StoredBlockMeta, chance, 0);
    }

    // ── Collision AABB (spec §7.17) ───────────────────────────────────────────

    /// <summary>
    /// Collision box is offset from the stored block's AABB by
    /// <c>progress × facing direction</c>. Spec: <c>qz.b(ry, x, y, z)</c>.
    /// </summary>
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        if (world.GetTileEntity(x, y, z) is not TileEntityPiston te) return null;

        float progress = te.Progress; // current animation progress
        if (te.IsExtending) progress = 1.0f - progress;

        return GetMovingAABB(world, x, y, z, te.StoredBlockId, progress, te.Facing);
    }

    /// <summary>
    /// Static AABB builder used by both <see cref="GetCollisionBoundingBoxFromPool"/> and
    /// <see cref="TileEntityPiston"/> entity-push logic. Spec: <c>qz.b(ry, x, y, z, blockId, progress, facing)</c>.
    /// </summary>
    public static AxisAlignedBB? GetMovingAABB(IWorld world, int x, int y, int z, int blockId, float progress, int facing)
    {
        if (blockId == 0) return null;
        Block? stored = BlocksList[blockId];
        if (stored == null) return null;

        AxisAlignedBB? base_ = stored.GetCollisionBoundingBoxFromPool(world, x, y, z);
        if (base_ == null) return null;

        double ox = progress * DirX[facing];
        double oy = progress * DirY[facing];
        double oz = progress * DirZ[facing];

        return AxisAlignedBB.GetFromPool(
            base_.MinX - ox, base_.MinY - oy, base_.MinZ - oz,
            base_.MaxX - ox, base_.MaxY - oy, base_.MaxZ - oz);
    }

    // ── No random tick, no light ───────────────────────────────────────────────

    public override int GetTextureIndex(int face) => 0; // never rendered directly
}
