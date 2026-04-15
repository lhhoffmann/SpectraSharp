namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>kw</c> (BlockRedstoneWire) — Block ID 55.
/// Propagates redstone power 0–15 through DFS; attenuates by 1 per block.
///
/// Key fields:
///   <see cref="_canProvidePower"/> (obf: a) — anti-reentrance flag; set false during v() query.
///   <see cref="s_dirty"/> (obf: cb) — shared dirty-block set; positions needing NotifyNeighbors.
///
/// Quirks preserved (spec §12):
///   1. Reentrance guard: a=false during world.v() to prevent infinite recursion.
///   4. NotifyNeighbors only added on 0-crossing transitions (0→N or N→0).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §3
/// </summary>
public sealed class BlockRedstoneWire : Block
{
    // ── Anti-reentrance / dirty tracking (spec §3.1) ─────────────────────────

    /// <summary>
    /// obf: <c>a</c> — canProvidePower. Set false while querying v() to break cycles.
    /// Default: true (wire provides power normally).
    /// </summary>
    private bool _canProvidePower = true;

    /// <summary>
    /// obf: <c>cb</c> — positions that need NotifyNeighbors after power change.
    /// Only added for transitions across 0 (spec quirk §12.4).
    /// </summary>
    private static readonly HashSet<(int, int, int)> s_dirty = new();

    // ── Construction (spec §3.2) ──────────────────────────────────────────────

    public BlockRedstoneWire(int id) : base(id, 164, Material.Grass_)
    {
        SetHardness(0.0f);
        ClearNeedsRandomTick();
        SetBlockName("redstoneDust");
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z) => null;
    public override bool CanProvidePower() => _canProvidePower;

    // ── canBlockStay (spec §3.3) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => (world is World w) && w.IsBlockNormalCube(x, y - 1, z);

    // ── isProvidingWeakPower (spec §3.4) ──────────────────────────────────────

    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
    {
        if (!_canProvidePower) return false;
        int meta = world.GetBlockMetadata(x, y, z);
        if (meta == 0) return false;
        if (face == 1) return true; // always powers upward

        // Determine connection flags in 4 horizontal directions
        bool west  = ConnectsTo(world, x - 1, y, z, 1) || (!IsBlockOpaque(world, x - 1, y, z) && ConnectsTo(world, x - 1, y - 1, z, -1));
        bool east  = ConnectsTo(world, x + 1, y, z, 3) || (!IsBlockOpaque(world, x + 1, y, z) && ConnectsTo(world, x + 1, y - 1, z, -1));
        bool north = ConnectsTo(world, x, y, z - 1, 2) || (!IsBlockOpaque(world, x, y, z - 1) && ConnectsTo(world, x, y - 1, z - 1, -1));
        bool south = ConnectsTo(world, x, y, z + 1, 0) || (!IsBlockOpaque(world, x, y, z + 1) && ConnectsTo(world, x, y - 1, z + 1, -1));

        // Staircase-up connections (only if not solid above)
        if (!IsBlockOpaque(world, x, y + 1, z))
        {
            if (IsBlockOpaque(world, x - 1, y, z) && ConnectsTo(world, x - 1, y + 1, z, -1)) west  = true;
            if (IsBlockOpaque(world, x + 1, y, z) && ConnectsTo(world, x + 1, y + 1, z, -1)) east  = true;
            if (IsBlockOpaque(world, x, y, z - 1) && ConnectsTo(world, x, y + 1, z - 1, -1)) north = true;
            if (IsBlockOpaque(world, x, y, z + 1) && ConnectsTo(world, x, y + 1, z + 1, -1)) south = true;
        }

        // Isolated wire: connects all lateral faces
        if (!west && !east && !north && !south)
            return face is 2 or 3 or 4 or 5;

        // Directional: powers face only if wire runs toward that face and is not branching perpendicularly
        if (face == 2 && south && !west && !east) return true;
        if (face == 3 && north && !west && !east) return true;
        if (face == 4 && west  && !north && !south) return true;
        if (face == 5 && east  && !north && !south) return true;
        return false;
    }

    // ── isProvidingStrongPower (spec §3.5) ────────────────────────────────────

    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
        => _canProvidePower && IsProvidingWeakPower(world, x, y, z, face);

    // ── Main propagation (spec §3.7) ──────────────────────────────────────────

    /// <summary>
    /// obf: <c>kw.g(ry,x,y,z)</c> — entry point for wire propagation.
    /// Runs DFS from (x,y,z), then notifies all dirty positions. Spec §3.7.
    /// </summary>
    public void Propagate(World world, int x, int y, int z)
    {
        PropagateInternal(world, x, y, z, x, y, z);

        // Copy and clear dirty set, then notify each position's 6 neighbors
        var snapshot = new List<(int, int, int)>(s_dirty);
        s_dirty.Clear();
        foreach (var (px, py, pz) in snapshot)
            world.NotifyNeighbors(px, py, pz, BlockID);
    }

    private void PropagateInternal(World world, int x, int y, int z, int ox, int oy, int oz)
    {
        int readMeta = world.GetBlockMetadata(x, y, z);

        // Query external power with reentrance guard
        _canProvidePower = false;
        bool powered = world.IsBlockReceivingPower(x, y, z);
        _canProvidePower = true;

        int newMeta;
        if (powered)
        {
            newMeta = 15;
        }
        else
        {
            newMeta = 0;

            // Check 4 horizontal neighbors (skip origin)
            int[] dx = { -1, 1, 0, 0 };
            int[] dz = {  0, 0,-1, 1 };
            for (int d = 0; d < 4; d++)
            {
                int nx = x + dx[d], nz = z + dz[d];
                if (nx == ox && y == oy && nz == oz) continue;

                newMeta = MaxWireMeta(world, nx, y, nz, newMeta);

                // Staircase connections
                if (IsBlockOpaque(world, nx, y, nz))
                    newMeta = MaxWireMeta(world, nx, y + 1, nz, newMeta);
                else
                    newMeta = MaxWireMeta(world, nx, y - 1, nz, newMeta);
            }

            if (newMeta > 0) newMeta--; // attenuation: -1 per block
        }

        if (readMeta == newMeta) return;

        // Apply metadata change quietly, then notify
        world.SuppressUpdates = true;
        world.SetMetadataQuiet(x, y, z, newMeta);
        world.NotifyNeighbors(x, y, z, BlockID);
        world.SuppressUpdates = false;

        // Recurse into neighbors that need updating
        int[] dx2 = { -1, 1, 0, 0 };
        int[] dz2 = {  0, 0,-1, 1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + dx2[d], nz = z + dz2[d];
            int neighMeta = GetWireMeta(world, nx, y, nz);
            int wantMeta  = Math.Max(0, newMeta - 1);
            if (neighMeta >= 0 && neighMeta != wantMeta)
                PropagateInternal(world, nx, y, nz, x, y, z);

            // Also staircase
            if (IsBlockOpaque(world, nx, y, nz))
            {
                neighMeta = GetWireMeta(world, nx, y + 1, nz);
                if (neighMeta >= 0 && neighMeta != wantMeta)
                    PropagateInternal(world, nx, y + 1, nz, x, y, z);
            }
            else
            {
                neighMeta = GetWireMeta(world, nx, y - 1, nz);
                if (neighMeta >= 0 && neighMeta != wantMeta)
                    PropagateInternal(world, nx, y - 1, nz, x, y, z);
            }
        }

        // On 0-crossing, schedule dirty notifications (spec quirk §12.4)
        if (readMeta == 0 || newMeta == 0)
        {
            s_dirty.Add((x, y, z));
            for (int d = 0; d < 4; d++)
                s_dirty.Add((x + dx2[d], y, z + dz2[d]));
            s_dirty.Add((x, y - 1, z));
            s_dirty.Add((x, y + 1, z));
        }
    }

    // ── onBlockAdded / onNeighborBlockChange (spec §3.8, §3.9) ──────────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        base.OnBlockAdded(world, x, y, z);
        if (world.IsClientSide || world is not World w) return;
        Propagate(w, x, y, z);
        NotifyStaircaseNeighbors(w, x, y, z);
    }

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (world.IsClientSide || world is not World w) return;
        int meta = world.GetBlockMetadata(x, y, z);
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItemWithChance(world, x, y, z, meta, 1.0f, 0);
            world.SetBlock(x, y, z, 0);
        }
        else
        {
            Propagate(w, x, y, z);
        }
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);
    }

    // ── Drops (spec §3.10) ────────────────────────────────────────────────────

    /// <summary>Always drops redstone dust item. Spec §3.10.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => Item.ItemsList[331]?.RegistryIndex ?? 331; // acy.aB = redstone dust (ID 331)

    // ── Private helpers ───────────────────────────────────────────────────────

    private static bool IsBlockOpaque(IBlockAccess world, int x, int y, int z)
        => Block.IsOpaqueCubeArr[world.GetBlockId(x, y, z) & 0xFF];

    /// <summary>
    /// Returns the wire meta at (x,y,z) if block is wire, else returns <paramref name="current"/>.
    /// obf: <c>f(ry,x,y,z,current)</c> — wireMetaAt. Spec §3.7.
    /// </summary>
    private static int MaxWireMeta(IBlockAccess world, int x, int y, int z, int current)
    {
        int id = world.GetBlockId(x, y, z);
        if (id != 55) return current; // not wire
        return Math.Max(current, world.GetBlockMetadata(x, y, z));
    }

    /// <summary>Returns wire meta at (x,y,z) or -1 if not wire.</summary>
    private static int GetWireMeta(IBlockAccess world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        return id == 55 ? world.GetBlockMetadata(x, y, z) : -1;
    }

    /// <summary>
    /// Checks if the block at (x,y,z) connects to redstone wire from direction <paramref name="fromDir"/>.
    /// fromDir -1 means "any" (staircase check). obf: <c>c(kq,x,y,z,fromDir)</c>. Spec §3.4.
    /// </summary>
    private bool ConnectsTo(IBlockAccess world, int x, int y, int z, int fromDir)
    {
        int id = world.GetBlockId(x, y, z);
        if (id == 55) return true; // wire always connects
        if (id == 0)  return false;
        Block? block = Block.BlocksList[id];
        if (block == null) return false;
        if (block.CanProvidePower() && fromDir != -1) return true;
        // Repeater connects only toward its facing direction
        if (id == 93 || id == 94)
        {
            int meta   = world.GetBlockMetadata(x, y, z);
            int facing = meta & 3;
            int opp    = (facing + 2) % 4; // opposite direction
            return fromDir == facing || fromDir == opp;
        }
        return false;
    }

    private static void NotifyStaircaseNeighbors(World world, int x, int y, int z)
    {
        // Notify lateral neighbors and their staircase y±1 positions
        int[] dx = { -1, 1, 0, 0 };
        int[] dz = {  0, 0,-1, 1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + dx[d], nz = z + dz[d];
            world.NotifyBlock(nx, y, nz, 55);
            if (world.IsBlockNormalCube(nx, y, nz))
                world.NotifyBlock(nx, y + 1, nz, 55);
            else
                world.NotifyBlock(nx, y - 1, nz, 55);
        }
    }
}
