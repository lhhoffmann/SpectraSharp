using SpectraEngine.Core.WorldGen.Structure;
using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.WorldGen.NetherFortress;

// ── Corridor list pieces ──────────────────────────────────────────────────────
// Spec §7.1-§7.6: ac, bw, ui, bl, kf, xr

/// <summary>
/// BridgeStraight — <c>ac</c>. Corridor list, weight 30, unlimited, isTerminator=true.
/// W=5, H=10, D=19. One forward exit.
/// Source spec: §7.1
/// </summary>
internal sealed class BridgeStraight : FortressPiece
{
    public BridgeStraight(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, -3, 0, 5, 10, 19, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnForward(sp, pieces, rng, 0, 2, false);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Floor (local Y=3, full width and depth)
        FillBox(world, bounds, 0, 3, 0, 4, 3, 18, NF.NetherBrick);
        // Outer walls
        FillBox(world, bounds, 0, 0, 0, 0, 9, 18, NF.NetherBrick);
        FillBox(world, bounds, 4, 0, 0, 4, 9, 18, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 4, 0, 3, 8, 18);
        // Ceiling pillars at near end (Z=0-5)
        FillBox(world, bounds, 0, 0, 0, 4, 2, 5, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 13, 4, 2, 18, NF.NetherBrick);
        // Fence columns at intervals
        for (int z = 0; z <= 18; z += 4)
        {
            PlaceBlock(world, NF.NetherFence, 0, 0, 4, z, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 4, 4, z, bounds);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// BridgeCrossing — <c>bw</c>. Corridor list, weight 10, max 4.
/// W=19, H=10, D=19. Forward + left + right exits.
/// Also the geometry of the StartingPiece (gc).
/// Source spec: §7.2
/// </summary>
internal class BridgeCrossing : FortressPiece
{
    public BridgeCrossing(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -8, -3, 0, 19, 10, 19, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is not StartingPiece sp) return;
        SpawnForward(sp, pieces, rng, 0, 8, false);
        SpawnLeft   (sp, pieces, rng, 8, 8, false);
        SpawnRight  (sp, pieces, rng, 8, 8, false);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Central forward bridge (W=3, full depth)
        FillBox(world, bounds, 7, 3, 0, 11, 4, 18, NF.NetherBrick);
        // Perpendicular bridge (full width, D=3)
        FillBox(world, bounds, 0, 3, 7, 18, 4, 11, NF.NetherBrick);
        // Clear interior cross
        ClearBox(world, bounds, 8, 5, 0, 10, 7, 18);
        ClearBox(world, bounds, 0, 5, 8, 18, 7, 10);
        // Corner fills (nether brick frame)
        FillBox(world, bounds, 0, 0, 0, 6, 4, 6, NF.NetherBrick);
        FillBox(world, bounds, 12, 0, 0, 18, 4, 6, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 12, 6, 4, 18, NF.NetherBrick);
        FillBox(world, bounds, 12, 0, 12, 18, 4, 18, NF.NetherBrick);
        // Foundation pillars
        for (int x = 7; x <= 11; x++)
            for (int z = 0; z <= 18; z++)
                PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, 2, z, bounds);
        for (int z = 7; z <= 11; z++)
            for (int x = 0; x <= 18; x++)
                PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, 2, z, bounds);
        // Fence railings on bridge edges
        for (int z = 0; z <= 18; z += 3)
        {
            PlaceBlock(world, NF.NetherFence, 0, 7, 5, z, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 11, 5, z, bounds);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// BridgeCrossing3 — <c>ui</c>. Corridor list, weight 10, max 4.
/// W=7, H=9, D=7. Forward + left + right exits.
/// Source spec: §7.3
/// </summary>
internal sealed class BridgeCrossing3 : FortressPiece
{
    public BridgeCrossing3(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -2, 0, 0, 7, 9, 7, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is not StartingPiece sp) return;
        SpawnForward(sp, pieces, rng, 0, 3, false);
        SpawnLeft   (sp, pieces, rng, 3, 3, false);
        SpawnRight  (sp, pieces, rng, 3, 3, false);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Floor (2-block thick)
        FillBox(world, bounds, 0, 0, 0, 6, 1, 6, NF.NetherBrick);
        // Outer frame
        FillBox(world, bounds, 0, 2, 0, 0, 7, 6, NF.NetherBrick);
        FillBox(world, bounds, 6, 2, 0, 6, 7, 6, NF.NetherBrick);
        FillBox(world, bounds, 0, 2, 0, 6, 7, 0, NF.NetherBrick);
        FillBox(world, bounds, 0, 2, 6, 6, 7, 6, NF.NetherBrick);
        // Top cap
        FillBox(world, bounds, 0, 8, 0, 6, 8, 6, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 2, 1, 5, 7, 5);
        // Fence corner decorations
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 0, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 6, 4, 0, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 6, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 6, 4, 6, bounds);
        // Foundation fill
        for (int x = 0; x <= 6; x++)
        for (int z = 0; z <= 6; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// BridgeStaircase — <c>bl</c>. Corridor list, weight 10, max 3.
/// W=7, H=11, D=7. One right exit.
/// Source spec: §7.4
/// </summary>
internal sealed class BridgeStaircase : FortressPiece
{
    public BridgeStaircase(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -2, 0, 0, 7, 11, 7, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnRight(sp, pieces, rng, 3, 5, false);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Outer shell
        FillBox(world, bounds, 0, 0, 0, 6, 8, 6, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 1, 1, 5, 7, 5);
        // Staircase (descending in Z direction)
        for (int z = 0; z < 5; z++)
        {
            int stairY = 2 + (4 - z); // descending
            for (int x = 1; x <= 4; x++)
                PlaceBlock(world, NF.NetherBrick, 0, x, stairY, z + 1, bounds);
        }
        // Top platform
        FillBox(world, bounds, 1, 7, 2, 5, 7, 5, NF.NetherBrick);
        // Fence on east wall
        for (int z = 1; z <= 5; z += 2)
            PlaceBlock(world, NF.NetherFence, 0, 5, 4, z, bounds);
        // Foundation
        for (int x = 0; x <= 6; x++)
        for (int z = 0; z <= 6; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// BlazeSpawnerCorridor — <c>kf</c>. Corridor list, weight 5, max 2, terminal.
/// W=7, H=8, D=9. No exits (terminal). Places a Blaze mob spawner.
/// Quirk §10.2: spawner placed at most once per piece.
/// Source spec: §7.5
/// </summary>
internal sealed class BlazeSpawnerCorridor : FortressPiece
{
    private bool _spawnerPlaced; // obf: a — one-time flag

    public BlazeSpawnerCorridor(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -2, 0, 0, 7, 8, 9, orientation), orientation, depth) { }

    // No AddExits — terminal piece

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Floor
        FillBox(world, bounds, 0, 0, 0, 6, 1, 7, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 2, 1, 5, 6, 7);
        // Side walls with arched windows
        FillBox(world, bounds, 0, 2, 0, 0, 6, 7, NF.NetherBrick);
        FillBox(world, bounds, 6, 2, 0, 6, 6, 7, NF.NetherBrick);
        // Stepped rising wall (staircase-like from spec)
        for (int z = 0; z <= 4; z++)
        {
            int wallH = 2 + z;
            FillBox(world, bounds, 0, 2, z, 0, wallH, z, NF.NetherBrick);
            FillBox(world, bounds, 6, 2, z, 6, wallH, z, NF.NetherBrick);
        }
        // Fence columns at staircase junctions
        for (int z = 1; z <= 5; z += 2)
        {
            PlaceBlock(world, NF.NetherFence, 0, 0, 4, z, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 6, 4, z, bounds);
        }
        // Top arch cap
        FillBox(world, bounds, 0, 7, 0, 6, 7, 8, NF.NetherBrick);

        // Blaze spawner (spec §7.5, quirk §10.2)
        if (!_spawnerPlaced)
        {
            int wx = GetWorldX(5, 3);
            int wy = GetWorldY(6);
            int wz = GetWorldZ(5, 3);
            if (bounds.Contains(wx, wy, wz))
            {
                _spawnerPlaced = true;
                world.SetBlock(wx, wy, wz, NF.MobSpawner);
                if (world.GetTileEntity(wx, wy, wz) is TileEntityMobSpawner spawner)
                    spawner.EntityTypeId = "Blaze";
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// FortressRoom (large open room) — <c>xr</c>. Corridor list, weight 5, max 1.
/// W=13, H=14, D=13. One forward exit using room list. Has a lava pool.
/// Quirk §10.3: lava placement uses world.SuppressUpdates flag.
/// Source spec: §7.6
/// </summary>
internal sealed class FortressRoom : FortressPiece
{
    public FortressRoom(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -5, -3, 0, 13, 14, 13, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnForward(sp, pieces, rng, 0, 5, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Floor (2-layer)
        FillBox(world, bounds, 0, 3, 0, 12, 4, 12, NF.NetherBrick);
        // Clear interior
        ClearBox(world, bounds, 1, 5, 1, 11, 12, 11);
        // Outer walls
        FillBox(world, bounds, 0, 0, 0, 0, 12, 12, NF.NetherBrick);
        FillBox(world, bounds, 12, 0, 0, 12, 12, 12, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 0, 12, 12, 0, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 12, 12, 12, 12, NF.NetherBrick);
        // Ceiling
        FillBox(world, bounds, 0, 13, 0, 12, 13, 12, NF.NetherBrick);
        // Fence battlement on outer walls
        for (int i = 1; i <= 11; i += 2)
        {
            PlaceBlock(world, NF.NetherFence, 0, 0, 11, i, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 12, 11, i, bounds);
            PlaceBlock(world, NF.NetherFence, 0, i, 11, 0, bounds);
            PlaceBlock(world, NF.NetherFence, 0, i, 11, 12, bounds);
        }
        // Inner courtyard fence towers
        PlaceBlock(world, NF.NetherFence, 0, 1, 8, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 11, 8, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 1, 8, 11, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 11, 8, 11, bounds);
        // Sub-floor cross support
        FillBox(world, bounds, 5, 2, 0, 7, 2, 12, NF.NetherBrick);
        FillBox(world, bounds, 0, 2, 5, 12, 2, 7, NF.NetherBrick);

        // Lava pool (spec §7.6, quirk §10.3)
        FillBox(world, bounds, 5, 5, 5, 7, 5, 7, NF.NetherBrick);
        ClearBox(world, bounds, 6, 1, 6, 6, 4, 6);
        PlaceBlock(world, NF.NetherBrick, 0, 6, 0, 6, bounds);
        bool oldSuppress = world.SuppressUpdates;
        world.SuppressUpdates = true;
        PlaceBlock(world, NF.Lava, 0, 6, 5, 6, bounds);
        world.SuppressUpdates = oldSuppress;

        // Foundation fill
        for (int x = 0; x <= 12; x++)
        for (int z = 0; z <= 12; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}
