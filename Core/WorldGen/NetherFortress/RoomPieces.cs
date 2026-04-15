using SpectraSharp.Core.WorldGen.Structure;

namespace SpectraSharp.Core.WorldGen.NetherFortress;

// ── Room list pieces ──────────────────────────────────────────────────────────
// Spec §7.7-§7.13: hg, yj, lu, ahw, tr, acs, io

/// <summary>
/// RoomCrossing (forward only) — <c>hg</c>. Room list, weight 25, unlimited, isTerminator=true.
/// W=5, H=7, D=5. One forward room exit.
/// Source spec: §7.7
/// </summary>
internal sealed class RoomCrossing : FortressPiece
{
    public RoomCrossing(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, 0, 0, 5, 7, 5, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnForward(sp, pieces, rng, 0, 2, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        FillBox(world, bounds, 0, 0, 0, 4, 1, 4, NF.NetherBrick);
        // Two side walls (closed box except front/back openings)
        FillBox(world, bounds, 0, 2, 0, 0, 5, 4, NF.NetherBrick);
        FillBox(world, bounds, 4, 2, 0, 4, 5, 4, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 2, 0, 3, 5, 4);
        // Top cap
        FillBox(world, bounds, 0, 6, 0, 4, 6, 4, NF.NetherBrick);
        // Fence corner decorations at alternating heights
        PlaceBlock(world, NF.NetherFence, 0, 0, 3, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 4, 3, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 0, 3, 3, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 4, 3, 3, bounds);
        // Foundation
        for (int x = 0; x <= 4; x++)
        for (int z = 0; z <= 4; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// RoomCrossing3 (forward + left + right) — <c>yj</c>. Room list, weight 15, max 5.
/// W=5, H=7, D=5. Three room exits.
/// Source spec: §7.8
/// </summary>
internal sealed class RoomCrossing3 : FortressPiece
{
    public RoomCrossing3(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, 0, 0, 5, 7, 5, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is not StartingPiece sp) return;
        SpawnForward(sp, pieces, rng, 0, 2, useRoomList: true);
        SpawnLeft   (sp, pieces, rng, 2, 2, useRoomList: true);
        SpawnRight  (sp, pieces, rng, 2, 2, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // All 4 walls open — just corner pillars
        FillBox(world, bounds, 0, 0, 0, 4, 1, 4, NF.NetherBrick);
        ClearBox(world, bounds, 0, 2, 0, 4, 5, 4);
        // Corner pillars
        for (int x = 0; x <= 4; x += 4)
        for (int z = 0; z <= 4; z += 4)
            FillBox(world, bounds, x, 2, z, x, 5, z, NF.NetherBrick);
        // Top cap
        FillBox(world, bounds, 0, 6, 0, 4, 6, 4, NF.NetherBrick);
        // Foundation
        for (int x = 0; x <= 4; x++)
        for (int z = 0; z <= 4; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// RoomCrossingRight (right exit only) — <c>lu</c>. Room list, weight 5, max 10.
/// W=5, H=7, D=5. One right room exit.
/// Source spec: §7.9
/// </summary>
internal sealed class RoomCrossingRight : FortressPiece
{
    public RoomCrossingRight(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, 0, 0, 5, 7, 5, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnRight(sp, pieces, rng, 2, 2, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        FillBox(world, bounds, 0, 0, 0, 4, 1, 4, NF.NetherBrick);
        // Left wall closed
        FillBox(world, bounds, 0, 2, 0, 0, 5, 4, NF.NetherBrick);
        ClearBox(world, bounds, 1, 2, 0, 4, 5, 4);
        FillBox(world, bounds, 0, 6, 0, 4, 6, 4, NF.NetherBrick);
        // Fence rails on left wall
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 3, bounds);
        // Pillars on open right side
        PlaceBlock(world, NF.NetherFence, 0, 4, 4, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 4, 4, 3, bounds);
        for (int x = 0; x <= 4; x++)
        for (int z = 0; z <= 4; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// RoomCrossingLeft (left exit only) — <c>ahw</c>. Room list, weight 5, max 10.
/// W=5, H=7, D=5. One left room exit. Mirror of RoomCrossingRight.
/// Source spec: §7.10
/// </summary>
internal sealed class RoomCrossingLeft : FortressPiece
{
    public RoomCrossingLeft(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, 0, 0, 5, 7, 5, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnLeft(sp, pieces, rng, 2, 2, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        FillBox(world, bounds, 0, 0, 0, 4, 1, 4, NF.NetherBrick);
        // Right wall closed
        FillBox(world, bounds, 4, 2, 0, 4, 5, 4, NF.NetherBrick);
        ClearBox(world, bounds, 0, 2, 0, 3, 5, 4);
        FillBox(world, bounds, 0, 6, 0, 4, 6, 4, NF.NetherBrick);
        // Fence rails on right wall
        PlaceBlock(world, NF.NetherFence, 0, 4, 4, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 4, 4, 3, bounds);
        // Pillars on open left side
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 1, bounds);
        PlaceBlock(world, NF.NetherFence, 0, 0, 4, 3, bounds);
        for (int x = 0; x <= 4; x++)
        for (int z = 0; z <= 4; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// StaircaseDown — <c>tr</c>. Room list, weight 10, max 3, isTerminator=true.
/// W=5, H=14, D=10. Descends 7 blocks over 10 Z steps. One forward room exit.
/// Source spec: §7.11
/// </summary>
internal sealed class StaircaseDown : FortressPiece
{
    public StaircaseDown(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, -7, 0, 5, 14, 10, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is StartingPiece sp)
            SpawnForward(sp, pieces, rng, 0, 2, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        for (int z = 0; z <= 9; z++)
        {
            int floorY = Math.Max(1, 7 - z);
            int ceilY  = Math.Min(Math.Max(floorY + 5, 14 - z), 13);

            // Floor row
            FillBox(world, bounds, 0, floorY, z, 4, floorY, z, NF.NetherBrick);
            // Clear between floor and ceiling
            if (ceilY > floorY + 1)
                ClearBox(world, bounds, 1, floorY + 1, z, 3, ceilY - 1, z);
            // Stairs (for first 7 steps)
            if (z < 7)
            {
                PlaceBlock(world, NF.NetherStairs, 3, 1, floorY + 1, z, bounds); // meta=3: north
                PlaceBlock(world, NF.NetherStairs, 3, 2, floorY + 1, z, bounds);
                PlaceBlock(world, NF.NetherStairs, 3, 3, floorY + 1, z, bounds);
            }
            // Ceiling row
            FillBox(world, bounds, 0, ceilY, z, 4, ceilY, z, NF.NetherBrick);
            // Side walls
            FillBox(world, bounds, 0, floorY, z, 0, ceilY, z, NF.NetherBrick);
            FillBox(world, bounds, 4, floorY, z, 4, ceilY, z, NF.NetherBrick);
            // Fence decorations at even Z steps
            if (z % 2 == 0 && z < 8)
            {
                PlaceBlock(world, NF.NetherFence, 0, 0, floorY + 2, z, bounds);
                PlaceBlock(world, NF.NetherFence, 0, 4, floorY + 2, z, bounds);
                PlaceBlock(world, NF.NetherFence, 0, 0, floorY + 3, z, bounds);
                PlaceBlock(world, NF.NetherFence, 0, 4, floorY + 3, z, bounds);
            }
            // Foundation fill
            for (int x = 0; x <= 4; x++)
                PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, floorY - 1, z, bounds);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// CorridorRoofed — <c>acs</c>. Room list, weight 7, max 2.
/// W=9, H=7, D=9. Left + right room exits (or corridor list, 7/8 chance room).
/// Source spec: §7.12
/// </summary>
internal sealed class CorridorRoofed : FortressPiece
{
    public CorridorRoofed(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -3, 0, 0, 9, 7, 9, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is not StartingPiece sp) return;
        bool leftRoom  = rng.NextInt(8) > 0;
        bool rightRoom = rng.NextInt(8) > 0;
        SpawnLeft (sp, pieces, rng, 4, 5, useRoomList: leftRoom);
        SpawnRight(sp, pieces, rng, 4, 5, useRoomList: rightRoom);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Floor (2-layer)
        FillBox(world, bounds, 0, 0, 0, 8, 1, 8, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 0, 2, 0, 8, 5, 8);
        // Partial roof
        FillBox(world, bounds, 0, 6, 0, 8, 6, 5, NF.NetherBrick);
        // Front wall with arched openings
        FillBox(world, bounds, 0, 2, 0, 8, 5, 0, NF.NetherBrick);
        ClearBox(world, bounds, 1, 2, 0, 2, 3, 0);
        ClearBox(world, bounds, 6, 2, 0, 7, 3, 0);
        // Back fence
        for (int x = 1; x <= 7; x += 2)
            PlaceBlock(world, NF.NetherFence, 0, x, 3, 8, bounds);
        // Side pillars
        for (int z = 2; z <= 6; z += 2)
        {
            PlaceBlock(world, NF.NetherBrick, 0, 0, 4, z, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 0, 5, z, bounds);
            PlaceBlock(world, NF.NetherBrick, 0, 8, 4, z, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 8, 5, z, bounds);
        }
        // Foundation
        for (int x = 0; x <= 8; x++)
        for (int z = 0; z <= 8; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// NetherWartRoom (large wart farm) — <c>io</c>. Room list, weight 5, max 2.
/// W=13, H=14, D=13. Same shell as FortressRoom. Nether Wart farm gallery.
/// Source spec: §7.13
/// </summary>
internal sealed class NetherWartRoom : FortressPiece
{
    public NetherWartRoom(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -5, -3, 0, 13, 14, 13, orientation), orientation, depth) { }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        if (startPiece is not StartingPiece sp) return;
        SpawnForward(sp, pieces, rng, 0, 5, useRoomList: true);
        SpawnForward(sp, pieces, rng, 0, 11, useRoomList: true);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Same outer shell as FortressRoom (spec §7.13)
        FillBox(world, bounds, 0, 3, 0, 12, 4, 12, NF.NetherBrick);
        ClearBox(world, bounds, 1, 5, 1, 11, 12, 11);
        FillBox(world, bounds, 0, 0, 0, 0, 12, 12, NF.NetherBrick);
        FillBox(world, bounds, 12, 0, 0, 12, 12, 12, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 0, 12, 12, 0, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 12, 12, 12, 12, NF.NetherBrick);
        FillBox(world, bounds, 0, 13, 0, 12, 13, 12, NF.NetherBrick);

        // Fence battlement
        for (int i = 1; i <= 11; i += 2)
        {
            PlaceBlock(world, NF.NetherFence, 0, 0, 11, i, bounds);
            PlaceBlock(world, NF.NetherFence, 0, 12, 11, i, bounds);
            PlaceBlock(world, NF.NetherFence, 0, i, 11, 0, bounds);
            PlaceBlock(world, NF.NetherFence, 0, i, 11, 12, bounds);
        }

        // Sub-floor cross
        FillBox(world, bounds, 5, 2, 0, 7, 2, 12, NF.NetherBrick);
        FillBox(world, bounds, 0, 2, 5, 12, 2, 7, NF.NetherBrick);

        // Nether Wart farm (spec §7.13)
        // Soul sand at Y=4, Nether Wart at Y=5 for X=5-7, var6=4..10
        for (int var5 = 0; var5 <= 6; var5++)
        {
            int var6 = var5 + 4; // world Z offset within piece (4..10)
            for (int x = 5; x <= 7; x++)
            {
                PlaceBlock(world, NF.SoulSand, 0, x, 4, var6, bounds);
                PlaceBlock(world, NF.NetherWart, 0, x, 5, var6, bounds);
            }
        }

        // Soul sand corners and additional rows
        for (int x = 3; x <= 4; x++)
        {
            PlaceBlock(world, NF.SoulSand, 0, x, 4, 2, bounds);
            PlaceBlock(world, NF.SoulSand, 0, x, 4, 3, bounds);
            PlaceBlock(world, NF.NetherWart, 0, x, 5, 2, bounds);
            PlaceBlock(world, NF.NetherWart, 0, x, 5, 3, bounds);
        }
        for (int x = 8; x <= 9; x++)
        {
            PlaceBlock(world, NF.SoulSand, 0, x, 4, 2, bounds);
            PlaceBlock(world, NF.SoulSand, 0, x, 4, 3, bounds);
            PlaceBlock(world, NF.NetherWart, 0, x, 5, 2, bounds);
            PlaceBlock(world, NF.NetherWart, 0, x, 5, 3, bounds);
        }
        // Top exit wart column
        PlaceBlock(world, NF.SoulSand, 0, 5, 4, 11, bounds);
        PlaceBlock(world, NF.SoulSand, 0, 6, 4, 11, bounds);
        PlaceBlock(world, NF.SoulSand, 0, 7, 4, 11, bounds);
        PlaceBlock(world, NF.NetherWart, 0, 5, 12, 11, bounds);
        PlaceBlock(world, NF.NetherWart, 0, 6, 12, 11, bounds);
        PlaceBlock(world, NF.NetherWart, 0, 7, 12, 11, bounds);

        // Foundation
        for (int x = 0; x <= 12; x++)
        for (int z = 0; z <= 12; z++)
            PlaceBlockIfInWorld(world, NF.NetherBrick, 0, x, -1, z, bounds);
    }
}
