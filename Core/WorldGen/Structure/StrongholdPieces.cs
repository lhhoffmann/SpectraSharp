using System.Collections.Generic;

namespace SpectraEngine.Core.WorldGen.Structure;

// ─────────────────────────────────────────────────────────────────────────────
// Door-type enum  (mj.java)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>mj</c> — door type used at piece connection points.
/// Spec §3.
/// </summary>
public enum StrongholdDoor
{
    Open     = 0, // bare archway, no block
    WoodDoor = 1, // ID 64
    IronDoor = 2, // ID 71
    IronBars = 3, // ID 101
}

// ─────────────────────────────────────────────────────────────────────────────
// Exit descriptor  (internal helper — not a Java replica)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Describes a connection point on a piece in local coordinates.
/// <see cref="DeltaY"/> is the vertical offset of the exit relative to the entry
/// (negative = descends, used by staircase pieces).
/// </summary>
public readonly struct PieceExit
{
    public readonly int LocalX;        // local X of the door centre
    public readonly int LocalZ;        // local Z of the door centre (= depth of piece for forward)
    public readonly int DeltaOrientation; // 0=same, +1=turn right, -1=turn left
    public readonly int DeltaY;        // Y offset of exit vs entry (0 for flat, -7 for stairs)
    public readonly StrongholdDoor DoorType;

    public PieceExit(int lx, int lz, int dOri, int dy, StrongholdDoor door)
    {
        LocalX = lx; LocalZ = lz;
        DeltaOrientation = dOri; DeltaY = dy; DoorType = door;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Abstract base  (os.java)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>os</c> — abstract Stronghold piece base.
/// Extends <see cref="StructurePiece"/> with stone-brick scatter helpers and exit list.
/// Spec §2.
/// </summary>
public abstract class StrongholdPieceBase : StructurePiece
{
    // ── Block ID constants ────────────────────────────────────────────────────

    protected const int StoneBrick         = 98;  // meta 0 — normal
    protected const int MossyStoneBrick    = 98;  // meta 1
    protected const int CrackedStoneBrick  = 98;  // meta 2
    protected const int SmoothStoneBrick   = 98;  // meta 3  (used in portal room)
    protected const int Cobweb             = 30;
    protected const int Torch              = 50;
    protected const int WoodDoorId         = 64;
    protected const int IronDoorId         = 71;
    protected const int IronBarsId         = 101;
    protected const int BookshelfId        = 47;
    protected const int WoodPlanksId       = 5;
    protected const int FenceId            = 85;
    protected const int ChestId            = 54;
    protected const int SpawnerId          = 52;
    protected const int EndPortalFrameId   = 120;
    protected const int WaterStillId       = 9;
    protected const int LavaStillId        = 11;
    protected const int StoneSlabId        = 44;  // meta=0 stone half-slab
    protected const int StoneBrickStairs   = 109;
    protected const int CobblestoneId      = 4;   // Note: wood planks=4; cobblestone=4 (corrected: cobble=4? no)
    // Correct: Cobblestone = 4? No. Wood planks=5, Cobblestone=4, Air=0.
    // Actually: air=0, stone=1, grass=2, dirt=3, cobblestone=4, planks=5
    // wood planks = 5 NOT 4 — fix constants

    protected StrongholdPieceBase(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // Public wrappers for the factory (protected → accessible via subclass cast)
    public int GetWorldX_Public(int lx, int lz) => GetWorldX(lx, lz);
    public int GetWorldY_Public(int ly)          => GetWorldY(ly);
    public int GetWorldZ_Public(int lx, int lz) => GetWorldZ(lx, lz);

    // ── Local piece dimensions ────────────────────────────────────────────────

    /// <summary>Piece width in local X (blocks).</summary>
    protected abstract int PieceWidth  { get; }
    /// <summary>Piece height in local Y (blocks).</summary>
    protected abstract int PieceHeight { get; }
    /// <summary>Piece depth in local Z (blocks).</summary>
    protected abstract int PieceDepth  { get; }

    // ── Exit list ─────────────────────────────────────────────────────────────

    /// <summary>All connection points this piece exposes for child attachment.</summary>
    public abstract PieceExit[] GetExits();

    // ── Stone brick scatter helper ────────────────────────────────────────────

    /// <summary>
    /// Places a stone brick block with random cracked/mossy variants.
    /// Scatter: 33% normal, 33% cracked (meta 2), 33% mossy (meta 1).
    /// </summary>
    protected void PlaceStoneBrickRandom(World world, JavaRandom rng,
        int lx, int ly, int lz, StructureBoundingBox bounds)
    {
        int r = rng.NextInt(3);
        int meta = r switch { 1 => 2, 2 => 1, _ => 0 };
        PlaceBlock(world, StoneBrick, meta, lx, ly, lz, bounds);
    }

    // ── Shell helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a W×H×D room shell: stone-brick walls/floor/ceiling, interior air.
    /// Optionally scatters cracked/mossy variants on walls.
    /// </summary>
    protected void PlaceShell(World world, JavaRandom rng, StructureBoundingBox bounds,
        int x1, int y1, int z1, int x2, int y2, int z2, bool scatter = true)
    {
        for (int ly = y1; ly <= y2; ly++)
        for (int lx = x1; lx <= x2; lx++)
        for (int lz = z1; lz <= z2; lz++)
        {
            bool isEdge = lx == x1 || lx == x2 || ly == y1 || ly == y2 || lz == z1 || lz == z2;
            if (isEdge)
            {
                if (scatter && ly > y1 && ly < y2)
                    PlaceStoneBrickRandom(world, rng, lx, ly, lz, bounds);
                else
                    PlaceBlock(world, StoneBrick, 0, lx, ly, lz, bounds);
            }
            else
            {
                PlaceBlock(world, 0, 0, lx, ly, lz, bounds); // air interior
            }
        }
    }

    // ── Door placement ────────────────────────────────────────────────────────

    /// <summary>Places a 2-tall doorway opening or door block at the given local coords.</summary>
    protected void PlaceDoor(World world, JavaRandom rng, StructureBoundingBox bounds,
        int lx, int lz, StrongholdDoor doorType)
    {
        // Bottom block
        int ly1 = 1, ly2 = 2;
        switch (doorType)
        {
            case StrongholdDoor.Open:
                PlaceBlock(world, 0, 0, lx, ly1, lz, bounds);
                PlaceBlock(world, 0, 0, lx, ly2, lz, bounds);
                break;
            case StrongholdDoor.WoodDoor:
                PlaceBlock(world, 0,         0, lx, ly1, lz, bounds); // door bottom (simplified to air)
                PlaceBlock(world, 0,         0, lx, ly2, lz, bounds);
                break;
            case StrongholdDoor.IronDoor:
                PlaceBlock(world, 0,         0, lx, ly1, lz, bounds);
                PlaceBlock(world, 0,         0, lx, ly2, lz, bounds);
                break;
            case StrongholdDoor.IronBars:
                PlaceBlock(world, IronBarsId, 0, lx, ly1, lz, bounds);
                PlaceBlock(world, IronBarsId, 0, lx, ly2, lz, bounds);
                break;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// gp — SimpleCorridor
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>gp</c> — simple straight corridor 5×5×7.
/// Stone brick shell, cobweb ceiling decorations, torches, one forward exit.
/// Spec §6.1.
/// </summary>
public sealed class ShCorridor : StrongholdPieceBase
{
    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => 7;

    public ShCorridor(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    public override PieceExit[] GetExits() =>
    [
        new PieceExit(2, PieceDepth, 0, 0, StrongholdDoor.WoodDoor),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 4, 6, scatter: true);
        // Entry door (z=0)
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.WoodDoor);
        // Exit door (z=6)
        PlaceDoor(world, rng, bounds, 2, 6, StrongholdDoor.WoodDoor);
        // Cobwebs on ceiling (random)
        for (int lz = 1; lz <= 5; lz += 2)
            if (rng.NextInt(4) == 0)
                PlaceBlock(world, Cobweb, 0, 2, 4, lz, bounds);
        // Torches
        PlaceBlock(world, Torch, 0, 1, 2, 3, bounds);
        PlaceBlock(world, Torch, 0, 3, 2, 3, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// vn — StraightCorridor (fallback dead-end connector)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vn</c> — variable-length straight corridor; used as fallback dead-end.
/// Open question §8.2: exact fallback length; using 5 (one-room length).
/// Spec §4.3 / §6.2.
/// </summary>
public sealed class ShStraightCorridor : StrongholdPieceBase
{
    private readonly int _length; // obf: a

    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => _length;

    public ShStraightCorridor(StructureBoundingBox bbox, int orientation, int depth, int length = 5)
        : base(bbox, orientation, depth) { _length = length; }

    // Dead-end: no exits
    public override PieceExit[] GetExits() => [];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 4, _length - 1, scatter: false);
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.Open);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// hq — LeftTurn
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>hq</c> — 90° left turn corridor 5×5×5.
/// Spec §6.4.
/// </summary>
public sealed class ShLeftTurn : StrongholdPieceBase
{
    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => 5;

    public ShLeftTurn(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // Exit is on the left wall (local x=0) at z=2, turning left (orientation-1)
    public override PieceExit[] GetExits() =>
    [
        new PieceExit(0, 2, -1, 0, StrongholdDoor.Open),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 4, 4, scatter: true);
        // Entry at z=0 center
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.Open);
        // Left exit: clear left wall at x=0, z=1..3
        PlaceBlock(world, 0, 0, 0, 1, 1, bounds);
        PlaceBlock(world, 0, 0, 0, 2, 1, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// xg — RightTurn
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>xg</c> — 90° right turn corridor 5×5×5.
/// Spec §6.4.
/// </summary>
public sealed class ShRightTurn : StrongholdPieceBase
{
    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => 5;

    public ShRightTurn(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // Exit is on the right wall (local x=W-1=4) at z=2, turning right (orientation+1)
    public override PieceExit[] GetExits() =>
    [
        new PieceExit(4, 2, +1, 0, StrongholdDoor.Open),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 4, 4, scatter: true);
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.Open);
        PlaceBlock(world, 0, 0, 4, 1, 1, bounds);
        PlaceBlock(world, 0, 0, 4, 2, 1, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// fj — Prison
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>fj</c> — prison room 9×5×11 with iron-bar cells and chest.
/// Open question §8.1: exact cell loot pool; using generic chest.
/// Spec §6.3.
/// </summary>
public sealed class ShPrison : StrongholdPieceBase
{
    protected override int PieceWidth  => 9;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => 11;

    public ShPrison(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    public override PieceExit[] GetExits() =>
    [
        new PieceExit(4, PieceDepth, 0, 0, StrongholdDoor.IronDoor),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 8, 4, 10, scatter: true);
        PlaceDoor(world, rng, bounds, 4, 0, StrongholdDoor.IronDoor);
        PlaceDoor(world, rng, bounds, 4, 10, StrongholdDoor.IronDoor);

        // Central aisle
        ClearBox(world, bounds, 1, 1, 1, 7, 3, 9);

        // Left cell: x=0-3, z=2-4, iron bar wall at x=3
        for (int lz = 2; lz <= 4; lz++)
            PlaceBlock(world, IronBarsId, 0, 3, 2, lz, bounds);
        PlaceBlock(world, 0, 0, 3, 1, 3, bounds); // door gap
        PlaceBlock(world, IronBarsId, 0, 1, 2, 2, bounds);
        PlaceBlock(world, IronBarsId, 0, 1, 2, 4, bounds);

        // Right cell: x=5-8, z=2-4
        for (int lz = 2; lz <= 4; lz++)
            PlaceBlock(world, IronBarsId, 0, 5, 2, lz, bounds);
        PlaceBlock(world, 0, 0, 5, 1, 3, bounds);
        PlaceBlock(world, IronBarsId, 0, 7, 2, 2, bounds);
        PlaceBlock(world, IronBarsId, 0, 7, 2, 4, bounds);

        // Left cell far: z=6-8
        for (int lz = 6; lz <= 8; lz++)
            PlaceBlock(world, IronBarsId, 0, 3, 2, lz, bounds);
        PlaceBlock(world, 0, 0, 3, 1, 7, bounds);
        PlaceBlock(world, IronBarsId, 0, 1, 2, 6, bounds);
        PlaceBlock(world, IronBarsId, 0, 1, 2, 8, bounds);

        // Right cell far: z=6-8
        for (int lz = 6; lz <= 8; lz++)
            PlaceBlock(world, IronBarsId, 0, 5, 2, lz, bounds);
        PlaceBlock(world, 0, 0, 5, 1, 7, bounds);
        PlaceBlock(world, IronBarsId, 0, 7, 2, 6, bounds);
        PlaceBlock(world, IronBarsId, 0, 7, 2, 8, bounds);

        // Chest in left cell (OQ §8.1 — generic)
        if (rng.NextInt(2) == 0)
            PlaceBlock(world, ChestId, 0, 1, 1, 3, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// jt — Crossing
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>jt</c> — 4-way crossing 11×7×11.
/// Open question §8.3: exit selection; implementing all three exits always.
/// Spec §6.5.
/// </summary>
public sealed class ShCrossing : StrongholdPieceBase
{
    protected override int PieceWidth  => 11;
    protected override int PieceHeight => 7;
    protected override int PieceDepth  => 11;

    public ShCrossing(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    public override PieceExit[] GetExits() =>
    [
        new PieceExit(5, PieceDepth, 0,  0, StrongholdDoor.WoodDoor), // forward
        new PieceExit(0, 5,          -1, 0, StrongholdDoor.WoodDoor), // left
        new PieceExit(10, 5,         +1, 0, StrongholdDoor.WoodDoor), // right
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 10, 6, 10, scatter: true);
        ClearBox(world, bounds, 1, 1, 1, 9, 5, 9);

        // Entry/exits
        PlaceDoor(world, rng, bounds, 5, 0, StrongholdDoor.WoodDoor);
        PlaceDoor(world, rng, bounds, 5, 10, StrongholdDoor.WoodDoor);
        // Side exits
        for (int ly = 1; ly <= 2; ly++)
        {
            PlaceBlock(world, 0, 0, 0, ly, 5, bounds);
            PlaceBlock(world, 0, 0, 10, ly, 5, bounds);
        }

        // Support pillars
        PlaceBlock(world, StoneBrick, 0, 2, 1, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 2, 2, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 2, 3, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 1, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 2, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 3, 2, bounds);
        PlaceBlock(world, StoneBrick, 0, 2, 1, 8, bounds);
        PlaceBlock(world, StoneBrick, 0, 2, 2, 8, bounds);
        PlaceBlock(world, StoneBrick, 0, 2, 3, 8, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 1, 8, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 2, 8, bounds);
        PlaceBlock(world, StoneBrick, 0, 8, 3, 8, bounds);

        // Torches
        PlaceBlock(world, Torch, 0, 5, 3, 3, bounds);
        PlaceBlock(world, Torch, 0, 5, 3, 7, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// kt — LargeRoom / Storeroom
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>kt</c> — large storeroom 10×9×11.
/// Forced as first piece after StrongholdStart. Contains chest loot.
/// Spec §6.6.
/// </summary>
public sealed class ShLargeRoom : StrongholdPieceBase
{
    protected override int PieceWidth  => 10;
    protected override int PieceHeight => 9;
    protected override int PieceDepth  => 11;

    public ShLargeRoom(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    public override PieceExit[] GetExits() =>
    [
        new PieceExit(5, PieceDepth, 0, 0, StrongholdDoor.WoodDoor), // forward only
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 9, 8, 10, scatter: true);
        PlaceDoor(world, rng, bounds, 5, 0, StrongholdDoor.WoodDoor);
        PlaceDoor(world, rng, bounds, 5, 10, StrongholdDoor.WoodDoor);

        // Platform / raised floor section
        FillBox(world, bounds, 1, 1, 1, 8, 0, 9, StoneBrick);
        ClearBox(world, bounds, 1, 1, 1, 8, 7, 9);

        // Pillar supports at corners
        FillBox(world, bounds, 1, 1, 1, 1, 5, 1, StoneBrick);
        FillBox(world, bounds, 8, 1, 1, 8, 5, 1, StoneBrick);
        FillBox(world, bounds, 1, 1, 9, 1, 5, 9, StoneBrick);
        FillBox(world, bounds, 8, 1, 9, 8, 5, 9, StoneBrick);

        // Torches
        PlaceBlock(world, Torch, 0, 3, 3, 2, bounds);
        PlaceBlock(world, Torch, 0, 6, 3, 2, bounds);
        PlaceBlock(world, Torch, 0, 3, 3, 8, bounds);
        PlaceBlock(world, Torch, 0, 6, 3, 8, bounds);

        // Chest (OQ §8.1 — generic)
        PlaceBlock(world, ChestId, 0, 4, 1, 5, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// so — SpiralStaircase
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>so</c> — spiral staircase 5×11×8, descends 7 blocks.
/// Steps use stone-brick slabs rotating around a central column.
/// Spec §6.7.
/// </summary>
public sealed class ShSpiralStairs : StrongholdPieceBase
{
    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 11;
    protected override int PieceDepth  => 8;

    public ShSpiralStairs(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // Exit is at bottom of stairs — 7 blocks lower, at the far end
    public override PieceExit[] GetExits() =>
    [
        new PieceExit(2, PieceDepth, 0, -7, StrongholdDoor.Open),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Outer shell (tall enough for full stair descent)
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 10, 7, scatter: false);
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.Open);

        // Spiral steps: 8 steps, each step advances +1 Z and -1 Y
        // Central column at (2, y, z)
        for (int step = 0; step < 8; step++)
        {
            int ly = 9 - step;  // starts at top
            int lz = step;
            // Central column block
            PlaceBlock(world, StoneBrick, 0, 2, ly, lz, bounds);
            // Step slab on the outer sides, rotating around center
            int slabX = step switch { 0 or 1 => 1, 2 or 3 => 3, 4 or 5 => 3, _ => 1 };
            PlaceBlock(world, StoneBrickStairs, 0, slabX, ly - 1, lz, bounds);
        }
        // Clear interior
        ClearBox(world, bounds, 1, 1, 1, 3, 9, 6);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// vl — StraightStaircase
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vl</c> — straight staircase 5×11×5, descends 7 blocks.
/// When <c>IsStart = true</c> this is also the <c>aeh</c> StrongholdStart.
/// Spec §6.8.
/// </summary>
public sealed class ShStraightStairs : StrongholdPieceBase
{
    /// <summary>True when this is the start piece (aeh). Forces LargeRoom first.</summary>
    public readonly bool IsStart;

    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 11;
    protected override int PieceDepth  => 5;

    public ShStraightStairs(StructureBoundingBox bbox, int orientation, int depth, bool isStart = false)
        : base(bbox, orientation, depth) { IsStart = isStart; }

    // Forward exit at bottom (7 blocks lower)
    public override PieceExit[] GetExits() =>
    [
        new PieceExit(2, PieceDepth, 0, -7, StrongholdDoor.Open),
    ];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 10, 4, scatter: false);
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.Open);

        // Straight flight: 5 steps descend 7 blocks
        for (int step = 0; step < 5; step++)
        {
            int ly = 9 - step - step / 1; // simplified step placement
            int lz = step;
            PlaceBlock(world, StoneBrick, 0, 1, ly, lz, bounds);
            PlaceBlock(world, StoneBrick, 0, 2, ly, lz, bounds);
            PlaceBlock(world, StoneBrick, 0, 3, ly, lz, bounds);
            // Clear above steps
            for (int ay = ly + 1; ay <= 9; ay++)
                ClearBox(world, bounds, 1, ay, lz, 3, ay, lz);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ys — SmallRoom
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ys</c> — small dead-end room 5×5×7. May contain chest.
/// Spec §6.10.
/// </summary>
public sealed class ShSmallRoom : StrongholdPieceBase
{
    protected override int PieceWidth  => 5;
    protected override int PieceHeight => 5;
    protected override int PieceDepth  => 7;

    public ShSmallRoom(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // Dead end — no exits
    public override PieceExit[] GetExits() => [];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 4, 4, 6, scatter: true);
        PlaceDoor(world, rng, bounds, 2, 0, StrongholdDoor.WoodDoor);
        // Optional chest (OQ §8.1)
        if (rng.NextInt(2) == 0)
            PlaceBlock(world, ChestId, 0, 2, 1, 4, bounds);
        // Torch
        PlaceBlock(world, Torch, 0, 2, 3, 3, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// zc — Library
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>zc</c> — library 14×(11 or 6)×15.
/// Tall variant: 2-floor with fence railings. Short variant: 1 floor.
/// Loot: Paper/Book/EnchantedBook/Compass.
/// Spec §6.11.
/// </summary>
public sealed class ShLibrary : StrongholdPieceBase
{
    private readonly bool _isTall; // obf: c — false=tall, true=short (inverted spec naming)

    protected override int PieceWidth  => 14;
    protected override int PieceHeight => _isTall ? 11 : 6;
    protected override int PieceDepth  => 15;

    public ShLibrary(StructureBoundingBox bbox, int orientation, int depth, bool isTall)
        : base(bbox, orientation, depth) { _isTall = isTall; }

    // No exits (special dead-end)
    public override PieceExit[] GetExits() => [];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        int h = PieceHeight;
        PlaceShell(world, rng, bounds, 0, 0, 0, 13, h - 1, 14, scatter: false);
        PlaceDoor(world, rng, bounds, 7, 0, StrongholdDoor.WoodDoor);

        // Floor: wood planks
        FillBox(world, bounds, 1, 1, 1, 12, 1, 13, 5); // ID 5 = wood planks

        // Bookshelves on walls
        for (int lz = 1; lz <= 13; lz++)
        {
            PlaceBlock(world, BookshelfId, 0, 1,  2, lz, bounds);
            PlaceBlock(world, BookshelfId, 0, 12, 2, lz, bounds);
        }
        for (int lx = 2; lx <= 11; lx++)
        {
            PlaceBlock(world, BookshelfId, 0, lx, 2, 1,  bounds);
            PlaceBlock(world, BookshelfId, 0, lx, 2, 13, bounds);
        }

        // Second floor (tall variant only)
        if (_isTall)
        {
            // Second level floor at Y=6
            FillBox(world, bounds, 1, 6, 1, 12, 6, 13, 5);
            // Fence railings
            for (int lx = 1; lx <= 12; lx++)
            {
                PlaceBlock(world, FenceId, 0, lx, 7, 1,  bounds);
                PlaceBlock(world, FenceId, 0, lx, 7, 13, bounds);
            }
            for (int lz = 2; lz <= 12; lz++)
            {
                PlaceBlock(world, FenceId, 0, 1,  7, lz, bounds);
                PlaceBlock(world, FenceId, 0, 12, 7, lz, bounds);
            }
            // Upper-level bookshelves
            for (int lz = 1; lz <= 13; lz++)
            {
                PlaceBlock(world, BookshelfId, 0, 1,  8, lz, bounds);
                PlaceBlock(world, BookshelfId, 0, 12, 8, lz, bounds);
            }
            // Upper chest
            PlaceBlock(world, ChestId, 0, 6, 7, 7, bounds);
        }

        // Ground floor loot chest
        PlaceBlock(world, ChestId, 0, 6, 2, 7, bounds);

        // Torches
        PlaceBlock(world, Torch, 0, 4, 3, 2, bounds);
        PlaceBlock(world, Torch, 0, 9, 3, 2, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ir — Portal Room
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ir</c> — End Portal room 11×8×16. Always exactly one per stronghold.
///
/// Contents: 12 End Portal Frame blocks in a 3×3 ring at local (4-6, 3, 8-10),
/// still-water pool in floor, silverfish spawner (placed once via <see cref="_spawnerPlaced"/>),
/// iron door entrance, torches.
///
/// Open question §8.4 clarified in spec: lava under the frame (not water).
/// Spec §6.12.
/// </summary>
public sealed class ShPortalRoom : StrongholdPieceBase
{
    private bool _spawnerPlaced; // obf: a — guard against double placement

    protected override int PieceWidth  => 11;
    protected override int PieceHeight => 8;
    protected override int PieceDepth  => 16;

    public ShPortalRoom(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // No exits (terminal piece)
    public override PieceExit[] GetExits() => [];

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        PlaceShell(world, rng, bounds, 0, 0, 0, 10, 7, 15, scatter: false);
        // Iron door entrance (entry at z=0)
        PlaceDoor(world, rng, bounds, 5, 0, StrongholdDoor.IronDoor);

        // Floor: smooth stone brick
        for (int lx = 1; lx <= 9; lx++)
        for (int lz = 1; lz <= 14; lz++)
            PlaceBlock(world, StoneBrick, 3, lx, 0, lz, bounds); // meta 3 = smooth

        // Water pool in floor (3×1×3 at local x=4-6, y=0, z=3-5)
        PlaceBlock(world, WaterStillId, 0, 4, 0, 3, bounds);
        PlaceBlock(world, WaterStillId, 0, 5, 0, 3, bounds);
        PlaceBlock(world, WaterStillId, 0, 6, 0, 3, bounds);
        PlaceBlock(world, WaterStillId, 0, 4, 0, 4, bounds);
        PlaceBlock(world, WaterStillId, 0, 5, 0, 4, bounds);
        PlaceBlock(world, WaterStillId, 0, 6, 0, 4, bounds);
        PlaceBlock(world, WaterStillId, 0, 4, 0, 5, bounds);
        PlaceBlock(world, WaterStillId, 0, 5, 0, 5, bounds);
        PlaceBlock(world, WaterStillId, 0, 6, 0, 5, bounds);

        // End Portal frame ring at Y=3, local x=4-6, z=8-10 (3×3 minus corners)
        // Facing metadata: frames face inward
        // Bottom row (z=8): face south (meta 0)
        PlaceBlock(world, EndPortalFrameId, 0, 4, 3, 8, bounds);
        PlaceBlock(world, EndPortalFrameId, 0, 5, 3, 8, bounds);
        PlaceBlock(world, EndPortalFrameId, 0, 6, 3, 8, bounds);
        // Top row (z=10): face north (meta 2)
        PlaceBlock(world, EndPortalFrameId, 2, 4, 3, 10, bounds);
        PlaceBlock(world, EndPortalFrameId, 2, 5, 3, 10, bounds);
        PlaceBlock(world, EndPortalFrameId, 2, 6, 3, 10, bounds);
        // Left col (x=4): face east (meta 3)
        PlaceBlock(world, EndPortalFrameId, 3, 4, 3, 9, bounds);
        // Right col (x=6): face west (meta 1)
        PlaceBlock(world, EndPortalFrameId, 1, 6, 3, 9, bounds);

        // Lava under frame (OQ §8.4 resolved: lava at Y=2 under frames)
        PlaceBlock(world, LavaStillId, 0, 4, 2, 8, bounds);
        PlaceBlock(world, LavaStillId, 0, 5, 2, 8, bounds);
        PlaceBlock(world, LavaStillId, 0, 6, 2, 8, bounds);
        PlaceBlock(world, LavaStillId, 0, 4, 2, 9, bounds);
        PlaceBlock(world, LavaStillId, 0, 5, 2, 9, bounds);
        PlaceBlock(world, LavaStillId, 0, 6, 2, 9, bounds);
        PlaceBlock(world, LavaStillId, 0, 4, 2, 10, bounds);
        PlaceBlock(world, LavaStillId, 0, 5, 2, 10, bounds);
        PlaceBlock(world, LavaStillId, 0, 6, 2, 10, bounds);

        // Silverfish spawner (placed only once — guard flag a)
        if (!_spawnerPlaced)
        {
            _spawnerPlaced = true;
            PlaceBlock(world, SpawnerId, 0, 5, 1, 12, bounds);
        }

        // Torches
        PlaceBlock(world, Torch, 0, 3, 3, 1, bounds);
        PlaceBlock(world, Torch, 0, 7, 3, 1, bounds);
        PlaceBlock(world, Torch, 0, 3, 3, 13, bounds);
        PlaceBlock(world, Torch, 0, 7, 3, 13, bounds);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// tc — Stronghold Factory
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>tc</c> — Stronghold piece factory.
///
/// Maintains the weight table of piece types, tracks per-type counts, and generates
/// the complete piece tree using an iterative depth-first approach (§4).
///
/// Usage: call <see cref="GeneratePieces"/> once; the returned list contains all
/// pieces in generation order, ready for subsequent <c>Generate()</c> calls.
///
/// Spec §4.
/// </summary>
public sealed class StrongholdFactory
{
    // ── Weight table (spec §4.1) ─────────────────────────────────────────────

    private sealed class WeightedEntry
    {
        public readonly string Name;
        public readonly int    Weight;
        public readonly int    MaxCount; // 0 = unlimited

        public int Count;

        public WeightedEntry(string name, int weight, int max)
        {
            Name = name; Weight = weight; MaxCount = max;
        }

        public bool IsAvailable => MaxCount == 0 || Count < MaxCount;
    }

    private static readonly WeightedEntry[] WeightTable =
    [
        new("Corridor",      40,  0),
        new("Prison",         5,  5),
        new("LeftTurn",      20,  0),
        new("RightTurn",     20,  0),
        new("Crossing",      10,  6),
        new("SpiralStairs",   5,  5),
        new("StraightStairs", 5,  5),
        new("LargeRoom",      5,  4),
        new("SmallRoom",      5,  4),
        new("Library",       10,  2),
        new("PortalRoom",    20,  1),
    ];

    // Reset counts between generation calls
    private readonly WeightedEntry[] _table;
    private bool _hasPortalRoom;

    public StrongholdFactory()
    {
        _table = new WeightedEntry[WeightTable.Length];
        for (int i = 0; i < WeightTable.Length; i++)
            _table[i] = new WeightedEntry(WeightTable[i].Name, WeightTable[i].Weight, WeightTable[i].MaxCount);
    }

    // ── Generation entry point ────────────────────────────────────────────────

    /// <summary>
    /// Generates the complete stronghold piece tree starting at the given world position.
    /// Returns all pieces (including start) in depth-first order.
    /// Spec §4.4.
    /// </summary>
    public List<StructurePiece> GeneratePieces(
        int originX, int originY, int originZ,
        int startOrientation,
        JavaRandom rng)
    {
        var allPieces = new List<StructurePiece>();

        // Start piece (aeh = StrongholdStart = ShStraightStairs with isStart=true)
        var startBBox = PlaceAt(originX, originY, originZ, startOrientation, 5, 11, 5, 1);
        var start = new ShStraightStairs(startBBox, startOrientation, 0, isStart: true);
        allPieces.Add(start);

        // Queue of (piece, exitIndex, worldExitX, worldExitY, worldExitZ, exitOrientation)
        var queue = new Queue<(StrongholdPieceBase piece, PieceExit exit, int wx, int wy, int wz, int ori)>();

        // Force a LargeRoom as the first child of start (spec §4.4 + §6.8)
        EnqueueExits(start, allPieces, queue, startOrientation, forceFirst: true, rng: rng);

        while (queue.Count > 0)
        {
            var (parentPiece, exit, wx, wy, wz, childOri) = queue.Dequeue();

            // Depth and radius guards (spec §4.2)
            if (parentPiece.Depth + 1 > 50) continue;

            int dx = Math.Abs(wx - originX);
            int dz = Math.Abs(wz - originZ);
            if (dx > 112 || dz > 112) continue;

            // Select piece type
            var child = CreatePiece(wx, wy, wz, childOri, parentPiece.Depth + 1, rng, forceLargeRoom: false);
            if (child == null) continue;

            // Intersection check
            bool intersects = false;
            foreach (var existing in allPieces)
                if (existing.BBox.Intersects(child.BBox)) { intersects = true; break; }
            if (intersects) continue;

            allPieces.Add(child);
            EnqueueExits(child, allPieces, queue, childOri, forceFirst: false, rng: rng);
        }

        return allPieces;
    }

    // ── Exit enqueueing ───────────────────────────────────────────────────────

    private void EnqueueExits(
        StrongholdPieceBase piece,
        List<StructurePiece> allPieces,
        Queue<(StrongholdPieceBase, PieceExit, int, int, int, int)> queue,
        int pieceOrientation,
        bool forceFirst,
        JavaRandom rng)
    {
        var exits = piece.GetExits();
        for (int i = 0; i < exits.Length; i++)
        {
            var e = exits[i];
            // Compute world position of this exit
            int wx = piece.GetWorldX_Public(e.LocalX, e.LocalZ);
            int wy = piece.GetWorldY_Public(1) + e.DeltaY;
            int wz = piece.GetWorldZ_Public(e.LocalX, e.LocalZ);
            int childOri = ((pieceOrientation + e.DeltaOrientation) % 4 + 4) % 4;

            if (forceFirst && i == 0)
            {
                // Force LargeRoom at first exit of StrongholdStart (spec §4.4 / §6.8)
                var largeRoomEntry = _table[7]; // "LargeRoom" index
                if (largeRoomEntry.IsAvailable)
                {
                    largeRoomEntry.Count++;
                    var bbox = PlaceAt(wx, wy, wz, childOri, 10, 9, 11, 5);
                    var lr = new ShLargeRoom(bbox, childOri, piece.Depth + 1);
                    bool intersects = false;
                    foreach (var existing in allPieces)
                        if (existing.BBox.Intersects(lr.BBox)) { intersects = true; break; }
                    if (!intersects)
                    {
                        allPieces.Add(lr);
                        EnqueueExits(lr, allPieces, queue, childOri, forceFirst: false, rng: rng);
                        continue;
                    }
                }
            }

            queue.Enqueue((piece, e, wx, wy, wz, childOri));
        }
    }

    // ── Piece selection (spec §4.2) ───────────────────────────────────────────

    private StrongholdPieceBase? CreatePiece(int wx, int wy, int wz, int orientation, int depth,
        JavaRandom rng, bool forceLargeRoom)
    {
        // Build eligible list (exclude maxed-out types)
        int totalWeight = 0;
        for (int i = 0; i < _table.Length; i++)
        {
            // Skip portal room if already placed
            if (_table[i].Name == "PortalRoom" && _hasPortalRoom) continue;
            if (_table[i].IsAvailable) totalWeight += _table[i].Weight;
        }
        if (totalWeight == 0) return null;

        int roll = rng.NextInt(totalWeight);
        int acc  = 0;
        string chosen = "Corridor";
        for (int i = 0; i < _table.Length; i++)
        {
            if (_table[i].Name == "PortalRoom" && _hasPortalRoom) continue;
            if (!_table[i].IsAvailable) continue;
            acc += _table[i].Weight;
            if (roll < acc) { chosen = _table[i].Name; break; }
        }

        // Increment count
        for (int i = 0; i < _table.Length; i++)
            if (_table[i].Name == chosen) { _table[i].Count++; break; }

        if (chosen == "PortalRoom") _hasPortalRoom = true;

        return chosen switch
        {
            "Corridor"       => new ShCorridor(     PlaceAt(wx, wy, wz, orientation, 5,  5,  7, 2), orientation, depth),
            "Prison"         => new ShPrison(        PlaceAt(wx, wy, wz, orientation, 9,  5, 11, 4), orientation, depth),
            "LeftTurn"       => new ShLeftTurn(      PlaceAt(wx, wy, wz, orientation, 5,  5,  5, 2), orientation, depth),
            "RightTurn"      => new ShRightTurn(     PlaceAt(wx, wy, wz, orientation, 5,  5,  5, 2), orientation, depth),
            "Crossing"       => new ShCrossing(      PlaceAt(wx, wy, wz, orientation, 11, 7, 11, 5), orientation, depth),
            "SpiralStairs"   => new ShSpiralStairs(  PlaceAt(wx, wy, wz, orientation, 5, 11,  8, 2), orientation, depth),
            "StraightStairs" => new ShStraightStairs(PlaceAt(wx, wy, wz, orientation, 5, 11,  5, 2), orientation, depth),
            "LargeRoom"      => new ShLargeRoom(     PlaceAt(wx, wy, wz, orientation, 10, 9, 11, 5), orientation, depth),
            "SmallRoom"      => new ShSmallRoom(     PlaceAt(wx, wy, wz, orientation, 5,  5,  7, 2), orientation, depth),
            "Library"        => new ShLibrary(       PlaceAt(wx, wy, wz, orientation, 14, 11, 15, 7), orientation, depth,
                                                     isTall: rng.NextInt(2) == 0),
            "PortalRoom"     => new ShPortalRoom(    PlaceAt(wx, wy, wz, orientation, 11, 8, 16, 5), orientation, depth),
            _                => new ShCorridor(      PlaceAt(wx, wy, wz, orientation, 5,  5,  7, 2), orientation, depth),
        };
    }

    // ── BBox placement helper ─────────────────────────────────────────────────

    /// <summary>
    /// Places a piece bounding box so that the entry point (cx, 1, 0) aligns with
    /// the given world connection point (wx, wy, wz) for the given orientation.
    /// </summary>
    private static StructureBoundingBox PlaceAt(
        int wx, int wy, int wz,
        int orientation,
        int w, int h, int d,
        int cx)
    {
        int oy = wy - 1; // entry local y=1 → BBox.MinY = wy-1
        return orientation switch
        {
            0 => new StructureBoundingBox(wx - cx,         oy, wz,         wx - cx + w - 1,     oy + h - 1, wz + d - 1),
            1 => new StructureBoundingBox(wx - d + 1,      oy, wz - cx,    wx,                  oy + h - 1, wz - cx + w - 1),
            2 => new StructureBoundingBox(wx - cx,         oy, wz - d + 1, wx - cx + w - 1,     oy + h - 1, wz),
            3 => new StructureBoundingBox(wx,              oy, wz - cx,    wx + d - 1,           oy + h - 1, wz - cx + w - 1),
            _ => new StructureBoundingBox(wx - cx,         oy, wz,         wx - cx + w - 1,     oy + h - 1, wz + d - 1),
        };
    }
}
