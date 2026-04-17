namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ahl</c> (BlockVine) — Block ID 106.
/// Vines cling to solid block faces, have no collision, spread slowly,
/// and drop nothing when broken without shears.
///
/// Metadata bit encoding (spec §3):
///   bit 0 (0x1) = East (+X)
///   bit 1 (0x2) = South (+Z)
///   bit 2 (0x4) = West (-X)
///   bit 3 (0x8) = North (-Z)
///   meta == 0   = hanging (attached to block above)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockVine_Spec.md
/// </summary>
public sealed class BlockVine : Block
{
    // Horizontal directions indexed 0–3: North, East, South, West
    // Corresponding metadata bits:      North=0x8, East=0x1, South=0x2, West=0x4
    private static readonly int[] _dirDx  = {  0,  1,  0, -1 };
    private static readonly int[] _dirDz  = { -1,  0,  1,  0 };
    private static readonly int[] _dirBit = { 0x8, 0x1, 0x2, 0x4 };

    public BlockVine() : base(106, 143, Material.Vine)
    {
        SetHardness(0.2f);
        SetStepSound(SoundGrass);
        SetBlockName("vine");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 15;
    public override bool RenderAsNormalBlock() => false;

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => null; // no collision (spec §2)

    // ── Attachment validity (spec §4) ────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="blockId"/> is a valid attachment surface:
    /// non-air, solid, and opaque material (full cube).
    /// Spec: <c>ahl.e(int blockId)</c>.
    /// </summary>
    private static bool IsValidAttachment(int blockId)
    {
        if (blockId == 0) return false;
        Block? block = BlocksList[blockId];
        if (block == null) return false;
        return block.IsOpaqueCube() && (block.BlockMaterial?.IsSolid() ?? false);
    }

    // ── Survival check (spec §6) ─────────────────────────────────────────────

    /// <summary>
    /// Checks each set metadata bit; clears bits where the adjacent attachment is gone.
    /// If all bits cleared and no valid top attachment: returns false (remove).
    /// Spec: <c>ahl.g(World, x, y, z)</c>.
    /// </summary>
    private bool CheckSurvival(IWorld world, int x, int y, int z)
    {
        int meta    = world.GetBlockMetadata(x, y, z);
        int newMeta = meta;

        // Check each horizontal attachment bit
        for (int i = 0; i < 4; i++)
        {
            int bit = _dirBit[i];
            if ((meta & bit) == 0) continue;

            int nx = x + _dirDx[i];
            int nz = z + _dirDz[i];

            if (IsValidAttachment(world.GetBlockId(nx, y, nz))) continue;

            // Exception (quirk 12.1): keep bit if vine above also has this bit
            int aboveId   = world.GetBlockId(x, y + 1, z);
            int aboveMeta = world.GetBlockMetadata(x, y + 1, z);
            if (aboveId == BlockID && (aboveMeta & bit) != 0) continue;

            newMeta &= ~bit;
        }

        if (newMeta != meta)
            world.SetMetadata(x, y, z, newMeta);

        if (newMeta == 0)
            return IsValidAttachment(world.GetBlockId(x, y + 1, z));

        return true;
    }

    // ── Neighbor update (spec §7) ─────────────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);
        if (!CheckSurvival(world, x, y, z))
        {
            DropBlockAsItem(world, x, y, z, world.GetBlockMetadata(x, y, z), 0);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── Random tick spread (spec §8) ─────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // Density cap: count vines in 9×3×9 area (spec §8 — 4×3×4 radius)
        int density = 0;
        for (int bx = x - 4; bx <= x + 4; bx++)
        for (int by = y - 1; by <= y + 1; by++)
        for (int bz = z - 4; bz <= z + 4; bz++)
        {
            if (world.GetBlockId(bx, by, bz) == BlockID)
                if (++density >= 5) return;
        }

        int meta = world.GetBlockMetadata(x, y, z);
        int pick = rng.NextInt(6);

        if (pick == 4) // Upward spread (spec §8 direction 1)
        {
            if (y >= world.GetHeight() - 1) return;
            if (world.GetBlockId(x, y + 1, z) != 0) return;

            int spreadMeta = rng.NextInt(16) & meta;
            for (int i = 0; i < 4; i++)
            {
                int bit = _dirBit[i];
                if ((spreadMeta & bit) == 0) continue;
                if (!IsValidAttachment(world.GetBlockId(x + _dirDx[i], y + 1, z + _dirDz[i])))
                    spreadMeta &= ~bit;
            }
            if (spreadMeta != 0)
                world.SetBlockAndMetadata(x, y + 1, z, BlockID, spreadMeta);
        }
        else if (pick == 5) // Downward spread (spec §8 direction 0)
        {
            if (y <= 1) return;
            int below = world.GetBlockId(x, y - 1, z);
            if (below == 0)
            {
                int spreadMeta = rng.NextInt(16) & meta;
                if (spreadMeta != 0)
                    world.SetBlockAndMetadata(x, y - 1, z, BlockID, spreadMeta);
            }
            else if (below == BlockID)
            {
                int belowMeta = world.GetBlockMetadata(x, y - 1, z);
                int mergeMeta = belowMeta | (rng.NextInt(16) & meta);
                if (mergeMeta != belowMeta)
                    world.SetMetadata(x, y - 1, z, mergeMeta);
            }
        }
        else // Horizontal spread directions 0–3 (spec §8 directions 2–5)
        {
            int dir = pick; // 0–3 = N/E/S/W
            int bit = _dirBit[dir];
            int nx  = x + _dirDx[dir];
            int nz  = z + _dirDz[dir];

            if ((meta & bit) == 0)
            {
                int targetId = world.GetBlockId(nx, y, nz);
                if (targetId == 0)
                {
                    // Lateral air — spread with bits that still have a wall at the new position
                    int newMeta = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int pBit = _dirBit[i];
                        if ((meta & pBit) == 0) continue;
                        if (IsValidAttachment(world.GetBlockId(nx + _dirDx[i], y, nz + _dirDz[i])))
                            newMeta |= pBit;
                    }
                    if (newMeta != 0)
                        world.SetBlockAndMetadata(nx, y, nz, BlockID, newMeta);
                }
                else if (IsValidAttachment(targetId))
                {
                    // Solid wall in this direction — extend current vine attachment
                    world.SetMetadata(x, y, z, meta | bit);
                }
            }
        }
    }

    // ── Drops (spec §9) ─────────────────────────────────────────────────────

    /// <summary>Vines drop nothing without shears. Spec: <c>ahl.f()</c> = 0.</summary>
    public override int QuantityDropped(JavaRandom rng) => 0;
}
