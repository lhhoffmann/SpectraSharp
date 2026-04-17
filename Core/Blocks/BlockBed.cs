namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aab</c> (BlockBed) — Block ID 26.
/// Two-block horizontal structure. Players right-click to sleep through the night.
/// In the Nether / dimensions without sky-light, the bed explodes instead (power=5, incendiary).
///
/// Metadata layout (spec §4):
///   Bits 0-1 = facing (0=south, 1=west, 2=north, 3=east)
///   Bit  2   = isOccupied
///   Bit  3   = isHead (1=head half, 0=foot half)
///
/// Quirks preserved (spec §19):
///   1. Double-block explosion: removes head, then foot if present, explodes from midpoint.
///   2. Occupied flag is set on HEAD only.
///   3. Foot half drops item; head half drops nothing.
///   4. Stale occupied flag cleared on re-entry scan.
///   5. Player shrunk to 0.2×0.2 while sleeping.
///   6. Wake counter a=100 on normal wake.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockBed_Spec.md
/// </summary>
public sealed class BlockBed : Block
{
    // ── Facing direction table: {dx, dz} from foot to head (spec §5) ─────────

    private static readonly int[][] DirTable =
    [
        [0, 1], [-1, 0], [0, -1], [1, 0]   // 0=south, 1=west, 2=north, 3=east
    ];

    // ── Metadata helpers (spec §4) ───────────────────────────────────────────

    /// <summary>obf: aab.e(int meta) — facing = meta &amp; 3.</summary>
    public static int GetFacing(int meta) => meta & 3;

    /// <summary>obf: aab.f(int meta) — isHead = (meta &amp; 8) != 0.</summary>
    public static bool IsHead(int meta) => (meta & 8) != 0;

    /// <summary>obf: aab.g(int meta) — isOccupied = (meta &amp; 4) != 0.</summary>
    public static bool IsOccupied(int meta) => (meta & 4) != 0;

    // ── Construction (spec §2) ────────────────────────────────────────────────

    public BlockBed(int id) : base(id, 134, Material.Mat_M) // p.m = cloth/bed material
    {
        SetHardness(0.2f);
        ClearNeedsRandomTick();
        SetHasTileEntity(); // T flag from registry
    }

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 14;
    public override bool RenderAsNormalBlock() => false;

    // ── Bounds (spec §6) — fixed 9/16 height ─────────────────────────────────

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
        => SetBounds(0, 0, 0, 1, 0.5625f, 1);

    // ── Drops (spec §10, quirk 3) ─────────────────────────────────────────────

    /// <summary>Foot half drops bed item (ID 355); head half drops nothing.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => IsHead(meta) ? 0 : 355; // acy.aZ.bM = 355

    /// <summary>Only drop from foot half (head half override prevents double-drop, quirk 3).</summary>
    public override void DropBlockAsItemWithChance(IWorld world, int x, int y, int z, int meta, float chance, int fortune)
    {
        if (!IsHead(meta))
            base.DropBlockAsItemWithChance(world, x, y, z, meta, chance, fortune);
    }

    // ── onBlockActivated (spec §7) ────────────────────────────────────────────

    /// <summary>
    /// obf: a(ry, int, int, int, vi) — right-click on bed.
    /// Handles Nether explosion, occupied check, and trySleep call.
    /// Always returns true.
    /// </summary>
    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        int meta = world.GetBlockMetadata(x, y, z);

        // Move to head half if we're on the foot (spec §7 step 3)
        if (!IsHead(meta))
        {
            int facing = GetFacing(meta);
            x += DirTable[facing][0];
            z += DirTable[facing][1];
            if (world.GetBlockId(x, y, z) != 26) return true; // orphaned foot
            meta = world.GetBlockMetadata(x, y, z);
        }

        // We are now at the HEAD position.

        // Nether / End: explode instead of sleep — spec §7 step 5, quirk 1
        // world.IsNether is true whenever HasSkyLight() returns false (Nether + End)
        if (world.IsNether)
        {
            // Remove head block
            double midX = x + 0.5, midY = y + 0.5, midZ = z + 0.5;
            world.SetBlock(x, y, z, 0);

            // Remove foot block if present, average explosion position
            int facing = GetFacing(meta);
            int fx = x - DirTable[facing][0], fz = z - DirTable[facing][1];
            if (world.GetBlockId(fx, y, fz) == 26)
            {
                world.SetBlock(fx, y, fz, 0);
                midX = (midX + fx + 0.5) / 2.0;
                midZ = (midZ + fz + 0.5) / 2.0;
            }

            world.CreateExplosion(null, midX, midY, midZ, 5.0f, true);
            return true;
        }

        // Occupied check (spec §7 step 6, quirk 4)
        if (IsOccupied(meta))
        {
            EntityPlayer? sleepingPlayer = null;
            if (world is World worldImpl)
            {
                foreach (Entity e in worldImpl.GetPlayerList())
                {
                    if (e is EntityPlayer ep && ep.IsSleeping &&
                        ep.BedPosition == (x, y, z))
                    {
                        sleepingPlayer = ep;
                        break;
                    }
                }
            }

            if (sleepingPlayer != null)
            {
                player.SendMessage("tile.bed.occupied");
                return true;
            }

            // Stale flag — clear it
            SetOccupied(world, x, y, z, false);
        }

        // TrySleep (spec §7 step 7-8)
        EnumSleepResult result = player.TrySleep(x, y, z);

        if (result == EnumSleepResult.Ok)
        {
            SetOccupied(world, x, y, z, true);
        }
        else if (result == EnumSleepResult.NotNight)
        {
            player.SendMessage("tile.bed.noSleep");
        }
        else if (result == EnumSleepResult.NotSafe)
        {
            player.SendMessage("tile.bed.notSafe");
        }

        return true;
    }

    // ── onNeighborBlockChange (spec §9) ──────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborId)
    {
        int meta   = world.GetBlockMetadata(x, y, z);
        int facing = GetFacing(meta);

        if (IsHead(meta))
        {
            // Head: check foot is still a bed
            int fx = x - DirTable[facing][0], fz = z - DirTable[facing][1];
            if (world.GetBlockId(fx, y, fz) != 26)
                world.SetBlock(x, y, z, 0); // remove orphaned head (no drop)
        }
        else
        {
            // Foot: check head is still a bed
            int hx = x + DirTable[facing][0], hz = z + DirTable[facing][1];
            if (world.GetBlockId(hx, y, hz) != 26)
            {
                world.SetBlock(x, y, z, 0); // remove orphaned foot
                if (!world.IsClientSide)
                    DropBlockAsItemWithChance(world, x, y, z, meta, 1.0f, 0);
            }
        }
    }

    // ── setOccupied static helper (spec §8) ─────────────────────────────────

    /// <summary>
    /// obf: static <c>aab.a(ry, int, int, int, bool)</c> — set/clear occupied bit on head block.
    /// Quirk 2: only writes to head half.
    /// </summary>
    public static void SetOccupied(IWorld world, int x, int y, int z, bool occupied)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        meta = occupied ? (meta | 4) : (meta & ~4);
        world.SetMetadata(x, y, z, meta);
    }

    // ── findWakeupPosition static helper (spec §15) ──────────────────────────

    /// <summary>
    /// obf: static <c>aab.f(ry, int, int, int, int)</c> — finds a safe position near the bed.
    /// Searches 3×3 area centered on foot then head half; skips <paramref name="offset"/> valid candidates.
    /// Returns null if no safe spot found.
    /// Spec: BlockBed_Spec §15.
    /// </summary>
    public static (int x, int y, int z)? FindWakeupPosition(IWorld world, int x, int y, int z, int offset)
    {
        int meta   = world.GetBlockMetadata(x, y, z);
        int facing = GetFacing(meta);

        for (int pass = 0; pass <= 1; pass++)
        {
            int cx = x - DirTable[facing][0] * pass - 1;
            int cz = z - DirTable[facing][1] * pass - 1;

            for (int x2 = cx; x2 <= cx + 2; x2++)
            for (int z2 = cz; z2 <= cz + 2; z2++)
            {
                // Solid floor below, passable at y and y+1
                if (!Block.IsOpaqueCubeArr[world.GetBlockId(x2, y - 1, z2)]) continue;
                if ( Block.IsOpaqueCubeArr[world.GetBlockId(x2, y,     z2)]) continue;
                if ( Block.IsOpaqueCubeArr[world.GetBlockId(x2, y + 1, z2)]) continue;

                if (offset <= 0)
                    return (x2, y, z2);
                offset--;
            }
        }

        return null;
    }
}
