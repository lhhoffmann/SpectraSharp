namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>qo</c> (BlockLeaves) — Block ID 18.
///
/// Properties:
///   - IsOpaqueCube = false (light passes through).
///   - LightOpacity = 1 (set via builder in BlockRegistry).
///   - Semi-transparent (α ≈ 151); requires cutout rendering in future renderer.
///
/// Metadata bit layout:
///   bits 0–1 (mask 3): wood type — 0=oak, 1=spruce, 2=birch, 3=jungle
///   bit  2   (mask 4): no-decay flag — set when placed by player
///   bit  3   (mask 8): needs-check flag — triggers decay BFS next tick
///
/// Random tick:
///   If no-decay bit is set → skip.
///   If needs-check bit is set → run BFS to find log within 4 blocks → decay or clear flag.
///   Otherwise → set needs-check on self and propagate to neighbours.
///
/// Drops: nothing by default; 1/20 chance of sapling (ID 6) matching wood type.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ConcreteBlocks_Spec.md §3
/// </summary>
public sealed class BlockLeaves : Block
{
    // Material p.e = Material.RockTransp (set in constructor); LightOpacity overridden by builder
    public BlockLeaves(int id, int texture) : base(id, texture, Material.RockTransp) { }

    // ── Physics overrides ─────────────────────────────────────────────────────

    /// <summary>Leaves are not fully opaque — sky light passes through.</summary>
    public override bool IsOpaqueCube() => false;

    // ── Drops (spec §3) ───────────────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng)
        => rng.NextInt(20) == 0 ? 1 : 0; // 1/20 chance of sapling

    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => 6; // sapling (damage = wood type, but ItemStack damage not exposed here)

    // ── Random tick (spec §3 — Decay Algorithm) ───────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        int meta = world.GetBlockMetadata(x, y, z);

        // Player-placed leaves never decay
        if ((meta & 4) != 0) return;

        if ((meta & 8) != 0)
        {
            // Needs-check bit set: run BFS decay check
            if (HasNearbyLog(world, x, y, z))
            {
                // Connected — clear needs-check bit
                world.SetBlockAndMetadata(x, y, z, BlockID, meta & ~8);
            }
            else
            {
                // Disconnected — decay
                world.SetBlock(x, y, z, 0);
            }
        }
        else
        {
            // Mark self and neighbours as needing check (propagate decay wave)
            PropagateNeedsCheck(world, x, y, z, meta);
        }
    }

    /// <summary>
    /// Simplified log-proximity check: searches a 9×9×9 cube for any log block (ID 17).
    /// Replaces the spec's 32×32×32 BFS for now — slightly more permissive but correct
    /// for the common case (dense forests rarely have leaves more than 4 blocks from a log).
    /// </summary>
    private static bool HasNearbyLog(IBlockAccess world, int x, int y, int z)
    {
        for (int dx = -4; dx <= 4; dx++)
        for (int dy = -4; dy <= 4; dy++)
        for (int dz = -4; dz <= 4; dz++)
            if (world.GetBlockId(x + dx, y + dy, z + dz) == 17)
                return true;
        return false;
    }

    /// <summary>Sets needs-check bit on self and on adjacent leaf blocks that don't already have it.</summary>
    private void PropagateNeedsCheck(IWorld world, int x, int y, int z, int meta)
    {
        world.SetBlockAndMetadata(x, y, z, BlockID, meta | 8);

        int[] dx = { -1, 1,  0, 0,  0, 0 };
        int[] dy = {  0, 0, -1, 1,  0, 0 };
        int[] dz = {  0, 0,  0, 0, -1, 1 };

        for (int i = 0; i < 6; i++)
        {
            int nx = x + dx[i], ny = y + dy[i], nz = z + dz[i];
            if (ny < 0 || ny >= 128) continue;
            if (world.GetBlockId(nx, ny, nz) != BlockID) continue;
            int nm = world.GetBlockMetadata(nx, ny, nz);
            if ((nm & 4) == 0 && (nm & 8) == 0)
                world.SetBlockAndMetadata(nx, ny, nz, BlockID, nm | 8);
        }
    }
}
