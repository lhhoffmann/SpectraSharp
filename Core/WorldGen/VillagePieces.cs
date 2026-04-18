using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen;

// ── Village piece weight record ────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aan</c> — weighted piece type entry.
/// Tracks how many of this type have been placed vs the limit.
/// </summary>
public sealed class WeightedVillagePiece(int weight, int minCount, int maxCount, PieceFactory factory)
{
    public readonly int    Weight   = weight;
    public          int    Count    = 0;
    public readonly int    MinCount = minCount;
    public readonly int    MaxCount = maxCount;
    public readonly PieceFactory Factory = factory;

    public bool IsExhausted => Count >= MaxCount;
}

public delegate VillagePiece? PieceFactory(int x, int y, int z, int facing, int depth, JavaRandom rng);

// ── Abstract bases ─────────────────────────────────────────────────────────────

/// <summary>
/// Abstract base for all village building pieces. Replica of <c>xf</c>.
/// Source spec: Documentation/VoxelCore/Parity/Specs/VillagePieces_Spec.md
/// </summary>
public abstract class VillagePiece : StructurePiece
{
    protected VillagePiece(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    /// <summary>
    /// Snaps piece Y to ground level. Sets BBox.MinY to ground height - 1.
    /// Shared across all building piece factory methods.
    /// </summary>
    protected void AdjustToGround(World world, StructureBoundingBox bounds)
    {
        int groundY = 0;
        int count   = 0;
        for (int x = BBox.MinX; x <= BBox.MaxX; x++)
        for (int z = BBox.MinZ; z <= BBox.MaxZ; z++)
        {
            groundY += GetGroundAt(world, x, z);
            count++;
        }
        if (count > 0)
        {
            int avgY = groundY / count;
            BBox.Offset(0, avgY - BBox.MinY - 1, 0);
        }
    }

    private static int GetGroundAt(World world, int x, int z)
    {
        for (int y = 128; y >= 0; y--)
        {
            int id = world.GetBlockId(x, y, z);
            if (id != 0 && id != 18 && id != 17) return y + 1;
        }
        return 64;
    }

    // Buildings do not add exits — override if needed.
    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng) { }
}

/// <summary>
/// Abstract base for road/street pieces. Replica of <c>za</c>.
/// Serves as type marker so roads can be filtered from buildings.
/// </summary>
public abstract class RoadBase : StructurePiece
{
    protected RoadBase(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }
}

// ── Block ID constants (resolved from spec open questions) ────────────────────

internal static class VBlock
{
    public const int Air        = 0;
    public const int Stone      = 1;
    public const int Grass      = 2;
    public const int Dirt       = 3;
    public const int Cobble     = 4;
    public const int Planks     = 5;
    public const int Log        = 17;
    public const int Gravel     = 13;
    public const int Sandstone  = 24;
    public const int Farmland   = 60;
    public const int Wheat      = 59;  // stage 7 = full grown
    public const int WaterSrc   = 9;
    public const int Fence      = 85;
    public const int DoorWood   = 64;
    public const int Torch      = 50;
    public const int Workbench  = 58;
    public const int Bookshelf  = 47;
    public const int Chest      = 54;
    public const int StairsWood = 53;
    public const int StairsCob  = 67;
    public const int SlabStone  = 44;  // meta 0 = stone slab
    public const int StoneBrick = 98;
    public const int Glass      = 20;
    public const int Glowstone  = 89;
    public const int StoneId    = 1;
}

// ── Well piece ─────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>abj</c> — village well. 3W×4H×2D.</summary>
public sealed class WellPiece : VillagePiece
{
    public WellPiece(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 3, 4, 2, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Cobblestone column at local (1, 0–2, 0)
        for (int ly = 0; ly <= 2; ly++) PlaceBlock(world, VBlock.Cobble, 0, 1, ly, 0, bounds);
        // Top slab
        PlaceBlock(world, VBlock.SlabStone, 0, 1, 3, 0, bounds);
        // Water pool (plus pattern around well head)
        PlaceBlock(world, VBlock.WaterSrc, 0, 0, 3, 0, bounds);
        PlaceBlock(world, VBlock.WaterSrc, 0, 2, 3, 0, bounds);
        PlaceBlock(world, VBlock.WaterSrc, 0, 1, 3, 1, bounds);
    }
}

// ── SmallHut ──────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>uy</c> — small hut. 5W×6H×5D.</summary>
public sealed class SmallHut : VillagePiece
{
    private readonly bool _hasFenceRing;

    public SmallHut(int x, int y, int z, int facing, int depth, bool fenceRing)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 5, 6, 5, facing), facing, depth)
    {
        _hasFenceRing = fenceRing;
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Walls: cobblestone shell, clear interior
        FillBox(world, bounds, 0, 0, 0, 4, 5, 4, VBlock.Air);
        FillBox(world, bounds, 0, 0, 0, 4, 5, 4, VBlock.Cobble, VBlock.Cobble, false);
        // Interior air
        FillBox(world, bounds, 1, 1, 1, 3, 4, 3, VBlock.Air);
        // Planks floor
        FillBox(world, bounds, 1, 0, 1, 3, 0, 3, VBlock.Planks);
        // Door at front (facing side)
        PlaceBlock(world, VBlock.DoorWood, 0, 2, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 2, 2, 0, bounds);
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 1, 3, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 3, 3, 1, bounds);
        // Crafting table
        PlaceBlock(world, VBlock.Workbench, 0, 1, 1, 2, bounds);
        if (_hasFenceRing)
        {
            for (int lx = 0; lx <= 4; lx++) { PlaceBlock(world, VBlock.Fence, 0, lx, 5, 0, bounds); PlaceBlock(world, VBlock.Fence, 0, lx, 5, 4, bounds); }
            for (int lz = 1; lz <= 3; lz++) { PlaceBlock(world, VBlock.Fence, 0, 0, 5, lz, bounds); PlaceBlock(world, VBlock.Fence, 0, 4, 5, lz, bounds); }
        }
    }
}

// ── LargeHouse ─────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>uz</c> — large house. 5W×12H×9D.</summary>
public sealed class LargeHouse : VillagePiece
{
    public LargeHouse(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 5, 12, 9, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Ground floor shell: planks
        FillBox(world, bounds, 0, 0, 0, 4, 5, 8, VBlock.Planks, VBlock.Planks, false);
        FillBox(world, bounds, 1, 1, 1, 3, 4, 7, VBlock.Air);
        // Upper floor shell: cobblestone
        FillBox(world, bounds, 0, 6, 0, 4, 11, 8, VBlock.Cobble, VBlock.Cobble, false);
        FillBox(world, bounds, 1, 7, 1, 3, 10, 7, VBlock.Air);
        // Planks floor at y=6
        FillBox(world, bounds, 0, 6, 0, 4, 6, 8, VBlock.Planks);
        // Windows on sides
        PlaceBlock(world, VBlock.Glass, 0, 0, 2, 3, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 4, 2, 3, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 0, 8, 3, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 4, 8, 3, bounds);
        // Door at front lower
        PlaceBlock(world, VBlock.DoorWood, 0, 2, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 2, 2, 0, bounds);
        // Chest
        PlaceBlock(world, VBlock.Chest, 0, 3, 1, 7, bounds);
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 1, 4, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 3, 4, 7, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 1, 10, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 3, 10, 7, bounds);
    }
}

// ── Blacksmith ─────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>gs</c> — blacksmith. 9W×9H×6D.</summary>
public sealed class Blacksmith : VillagePiece
{
    public Blacksmith(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 9, 9, 6, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Main walls: sandstone (OQ 8.3 resolved: sandstone)
        FillBox(world, bounds, 0, 0, 0, 8, 8, 5, VBlock.Sandstone, VBlock.Sandstone, false);
        FillBox(world, bounds, 1, 1, 1, 7, 7, 4, VBlock.Air);
        // Floor: stone
        FillBox(world, bounds, 1, 0, 1, 7, 0, 4, VBlock.StoneId);
        // Open front face (clear front wall except corners)
        for (int lx = 1; lx <= 7; lx++) for (int ly = 1; ly <= 7; ly++) PlaceBlock(world, VBlock.Air, 0, lx, ly, 0, bounds);
        // Pitched roof: stair rows
        for (int lx = 0; lx <= 8; lx++)
        {
            PlaceBlock(world, VBlock.StairsCob, 3, lx, 8, 2, bounds);
            PlaceBlock(world, VBlock.StairsCob, 2, lx, 7, 1, bounds);
            PlaceBlock(world, VBlock.StairsCob, 3, lx, 7, 3, bounds);
        }
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 1, 4, 2, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 7, 4, 2, bounds);
        // Workbench
        PlaceBlock(world, VBlock.Workbench, 0, 2, 1, 4, bounds);
        // Chest with loot (OQ 8.1 resolved: yes)
        PlaceBlock(world, VBlock.Chest, 0, 6, 1, 4, bounds);
    }
}

// ── HouseSmall2 ───────────────────────────────────────────────────────────────

/// <summary>Replica of <c>wi</c> — small house variant 2. 4W×6H×5D.</summary>
public sealed class HouseSmall2 : VillagePiece
{
    private readonly int _roofVariant; // 0, 1, or 2

    public HouseSmall2(int x, int y, int z, int facing, int depth, int roofVariant)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 4, 6, 5, facing), facing, depth)
    {
        _roofVariant = roofVariant;
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        FillBox(world, bounds, 0, 0, 0, 3, 5, 4, VBlock.Planks, VBlock.Planks, false);
        FillBox(world, bounds, 1, 1, 1, 2, 4, 3, VBlock.Air);
        // Door
        PlaceBlock(world, VBlock.DoorWood, 0, 1, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 1, 2, 0, bounds);
        // Roof
        int roofBlock = _roofVariant switch { 0 => VBlock.Cobble, 1 => VBlock.Planks, _ => VBlock.StairsCob };
        FillBox(world, bounds, 0, 5, 0, 3, 5, 4, roofBlock);
        PlaceBlock(world, VBlock.Torch, 0, 1, 3, 1, bounds);
    }
}

// ── Library ───────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>acz</c> — library. 9W×7H×11D.</summary>
public sealed class Library : VillagePiece
{
    public Library(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 9, 7, 11, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        FillBox(world, bounds, 0, 0, 0, 8, 6, 10, VBlock.Planks, VBlock.Planks, false);
        FillBox(world, bounds, 1, 1, 1, 7, 5, 9, VBlock.Air);
        // Bookshelves along inner walls
        for (int lz = 2; lz <= 8; lz++) { PlaceBlock(world, VBlock.Bookshelf, 0, 1, 2, lz, bounds); PlaceBlock(world, VBlock.Bookshelf, 0, 7, 2, lz, bounds); }
        for (int lz = 2; lz <= 8; lz++) { PlaceBlock(world, VBlock.Bookshelf, 0, 1, 3, lz, bounds); PlaceBlock(world, VBlock.Bookshelf, 0, 7, 3, lz, bounds); }
        // Door
        PlaceBlock(world, VBlock.DoorWood, 0, 4, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 4, 2, 0, bounds);
        // Windows
        PlaceBlock(world, VBlock.Glass, 0, 0, 2, 5, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 8, 2, 5, bounds);
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 2, 4, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 6, 4, 9, bounds);
    }
}

// ── FarmLarge ─────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>ec</c> — large farm. 13W×4H×9D.</summary>
public sealed class FarmLarge : VillagePiece
{
    public FarmLarge(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 13, 4, 9, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Fence perimeter
        for (int lx = 0; lx <= 12; lx++) { PlaceBlock(world, VBlock.Fence, 0, lx, 1, 0, bounds); PlaceBlock(world, VBlock.Fence, 0, lx, 1, 8, bounds); }
        for (int lz = 1; lz <= 7; lz++) { PlaceBlock(world, VBlock.Fence, 0, 0, 1, lz, bounds); PlaceBlock(world, VBlock.Fence, 0, 12, 1, lz, bounds); }
        // Farmland rows and water irrigation
        for (int lz = 1; lz <= 7; lz++)
        for (int lx = 1; lx <= 11; lx++)
        {
            if (lx == 6) PlaceBlock(world, VBlock.WaterSrc, 0, lx, 0, lz, bounds);
            else         PlaceBlock(world, VBlock.Farmland,  0, lx, 0, lz, bounds);
        }
        // Wheat crops at stage 7 (meta=7)
        for (int lz = 1; lz <= 7; lz += 2)
        for (int lx = 1; lx <= 11; lx++)
            if (lx != 6) PlaceBlock(world, VBlock.Wheat, 7, lx, 1, lz, bounds);
    }
}

// ── FarmSmall ─────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>agr</c> — small farm. 7W×4H×9D.</summary>
public sealed class FarmSmall : VillagePiece
{
    public FarmSmall(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 7, 4, 9, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        // Fence perimeter
        for (int lx = 0; lx <= 6; lx++) { PlaceBlock(world, VBlock.Fence, 0, lx, 1, 0, bounds); PlaceBlock(world, VBlock.Fence, 0, lx, 1, 8, bounds); }
        for (int lz = 1; lz <= 7; lz++) { PlaceBlock(world, VBlock.Fence, 0, 0, 1, lz, bounds); PlaceBlock(world, VBlock.Fence, 0, 6, 1, lz, bounds); }
        // Farmland + water channel
        for (int lz = 1; lz <= 7; lz++)
        for (int lx = 1; lx <= 5; lx++)
        {
            if (lx == 3) PlaceBlock(world, VBlock.WaterSrc, 0, lx, 0, lz, bounds);
            else         PlaceBlock(world, VBlock.Farmland,  0, lx, 0, lz, bounds);
        }
        // Crops (OQ 8.4 resolved: yes, same pattern as large)
        for (int lz = 1; lz <= 7; lz += 2)
        for (int lx = 1; lx <= 5; lx++)
            if (lx != 3) PlaceBlock(world, VBlock.Wheat, 7, lx, 1, lz, bounds);
    }
}

// ── HouseLarge2 ───────────────────────────────────────────────────────────────

/// <summary>Replica of <c>ko</c> — large house 2. 10W×6H×7D.</summary>
public sealed class HouseLarge2 : VillagePiece
{
    public HouseLarge2(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 10, 6, 7, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        FillBox(world, bounds, 0, 0, 0, 9, 5, 6, VBlock.Planks, VBlock.Planks, false);
        FillBox(world, bounds, 1, 1, 1, 8, 4, 5, VBlock.Air);
        // Stone brick roof band
        FillBox(world, bounds, 0, 5, 0, 9, 5, 6, VBlock.StoneBrick);
        // Sandstone upper walls (spec §5.2: yy.at = sandstone on ko)
        for (int lx = 0; lx <= 9; lx++) for (int lz = 0; lz <= 6; lz++) PlaceBlock(world, VBlock.Sandstone, 0, lx, 4, lz, bounds);
        // Door
        PlaceBlock(world, VBlock.DoorWood, 0, 5, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 5, 2, 0, bounds);
        // Windows
        PlaceBlock(world, VBlock.Glass, 0, 0, 2, 3, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 9, 2, 3, bounds);
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 2, 3, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 7, 3, 5, bounds);
    }
}

// ── Church ────────────────────────────────────────────────────────────────────

/// <summary>Replica of <c>tf</c> — church. 9W×7H×12D.</summary>
public sealed class Church : VillagePiece
{
    public Church(int x, int y, int z, int facing, int depth)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 9, 7, 12, facing), facing, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        AdjustToGround(world, bounds);
        FillBox(world, bounds, 0, 0, 0, 8, 6, 11, VBlock.StoneBrick, VBlock.StoneBrick, false);
        FillBox(world, bounds, 1, 1, 1, 7, 5, 10, VBlock.Air);
        // Stone brick floor
        FillBox(world, bounds, 1, 0, 1, 7, 0, 10, VBlock.StoneId);
        // Tower pinnacle
        for (int i = 0; i < 3; i++) PlaceBlock(world, VBlock.StoneBrick, 0, 4, 7 + i, 6, bounds);
        // Door
        PlaceBlock(world, VBlock.DoorWood, 0, 4, 1, 0, bounds);
        PlaceBlock(world, VBlock.DoorWood, 8, 4, 2, 0, bounds);
        // Windows
        PlaceBlock(world, VBlock.Glass, 0, 0, 3, 6, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 8, 3, 6, bounds);
        PlaceBlock(world, VBlock.Glass, 0, 4, 5, 0, bounds);
        // Glowstone altar
        PlaceBlock(world, VBlock.Glowstone, 0, 4, 1, 9, bounds);
        // Torches
        PlaceBlock(world, VBlock.Torch, 0, 1, 3, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 7, 3, 1, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 1, 3, 10, bounds);
        PlaceBlock(world, VBlock.Torch, 0, 7, 3, 10, bounds);
    }
}

// ── StreetBetweenPieces ───────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ahz</c> — road segment between buildings. Extends RoadBase.
/// Places gravel surface; tries to add buildings on both sides.
/// </summary>
public sealed class StreetBetweenPieces : RoadBase
{
    private readonly int _roadLength;

    public StreetBetweenPieces(int x, int y, int z, int facing, int depth, int length)
        : base(StructureBoundingBox.FromOrigin(x, y, z, 3, 3, length, facing), facing, depth)
    {
        _roadLength = length;
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Fill gravel road surface along the length
        for (int lz = 0; lz < _roadLength; lz++)
        for (int lx = 0; lx < 3; lx++)
        {
            // Snap to ground
            int wx = GetWorldX(lx, lz);
            int wz = GetWorldZ(lx, lz);
            int wy = GetSurfaceY(world, wx, wz);
            if (wy >= 0) world.SetBlockAndMetadata(wx, wy, wz, VBlock.Gravel, 0);
        }
    }

    private static int GetSurfaceY(World world, int x, int z)
    {
        for (int y = 127; y >= 0; y--)
            if (world.GetBlockId(x, y, z) != 0) return y;
        return -1;
    }

    public override void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng)
    {
        // Spec §6.2: try placing buildings on both sides
        if (startPiece is not VillageComponent centre) return;
        int start = rng.NextInt(5);
        while (start < _roadLength - 8)
        {
            var piece = VillagePieceRegistry.TryPlaceBuilding(centre, pieces, rng,
                BBox.MinX, BBox.MinY, BBox.MinZ, Orientation, Depth + 1);
            if (piece != null) { start += System.Math.Max(piece.BBox.SizeX, piece.BBox.SizeZ); pieces.Add(piece); }
            start += 2 + rng.NextInt(5);
        }
    }
}

// ── VillagePieceRegistry ──────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>xy</c> — static registry with weighted piece selection.
/// </summary>
public static class VillagePieceRegistry
{
    // Spec §4.1 weight table (depth=0 always in 1.0)
    private static List<WeightedVillagePiece> BuildWeightList(int depth)
    {
        return
        [
            new WeightedVillagePiece(4,  2 + depth, 4 + depth * 2,
                (x, y, z, f, d, rng) => new SmallHut(x, y, z, f, d, rng.NextInt(2) == 0)),
            new WeightedVillagePiece(20, 0 + depth, 1 + depth,
                (x, y, z, f, d, _)   => new LargeHouse(x, y, z, f, d)),
            new WeightedVillagePiece(20, 0 + depth, 2 + depth,
                (x, y, z, f, d, _)   => new Blacksmith(x, y, z, f, d)),
            new WeightedVillagePiece(3,  2 + depth, 5 + depth * 3,
                (x, y, z, f, d, rng) => new HouseSmall2(x, y, z, f, d, rng.NextInt(3))),
            new WeightedVillagePiece(15, 0 + depth, 2 + depth,
                (x, y, z, f, d, _)   => new Library(x, y, z, f, d)),
            new WeightedVillagePiece(3,  1 + depth, 4 + depth,
                (x, y, z, f, d, _)   => new FarmLarge(x, y, z, f, d)),
            new WeightedVillagePiece(3,  2 + depth, 4 + depth * 2,
                (x, y, z, f, d, _)   => new FarmSmall(x, y, z, f, d)),
            new WeightedVillagePiece(15, 0,         1 + depth,
                (x, y, z, f, d, _)   => new HouseLarge2(x, y, z, f, d)),
            new WeightedVillagePiece(8,  0 + depth, 3 + depth * 2,
                (x, y, z, f, d, _)   => new Church(x, y, z, f, d)),
        ];
    }

    /// <summary>
    /// Builds the initial weighted piece list for a new village.
    /// Removes entries whose max count resolves to 0. Depth is always 0 in 1.0.
    /// Spec: <c>xy.a(Random rand, int depth)</c>.
    /// </summary>
    public static List<WeightedVillagePiece> CreatePieceList(JavaRandom rng, int depth = 0)
    {
        var list = BuildWeightList(depth);
        list.RemoveAll(p => p.MaxCount <= 0);
        return list;
    }

    /// <summary>
    /// Weighted random piece selection. Falls back to WellPiece after 5 failed tries.
    /// Spec: <c>xy.c()</c>.
    /// </summary>
    public static VillagePiece? SelectPiece(
        VillageComponent centre,
        List<WeightedVillagePiece> pool,
        List<StructurePiece> all,
        JavaRandom rng,
        int x, int y, int z, int facing, int depth)
    {
        if (pool.Count == 0) return null;

        int total = pool.Sum(p => p.Weight);
        WeightedVillagePiece? lastPicked = null;

        for (int tries = 0; tries < 5; tries++)
        {
            int pick = rng.NextInt(total);
            foreach (var wp in pool)
            {
                pick -= wp.Weight;
                if (pick >= 0) continue;
                // Skip repeat if more than one type available
                if (wp == lastPicked && pool.Count > 1) break;

                VillagePiece? piece = wp.Factory(x, y, z, facing, depth, rng);
                if (piece == null) break;

                wp.Count++;
                lastPicked = wp;
                if (wp.IsExhausted) { pool.Remove(wp); total -= wp.Weight; }

                return piece;
            }
        }

        // Fallback: well
        return new WellPiece(x, y, z, facing, depth);
    }

    /// <summary>
    /// Attempts to place a building piece near a road position.
    /// Applies 112-block radius and depth-50 limit from spec §4.3.
    /// </summary>
    public static VillagePiece? TryPlaceBuilding(
        VillageComponent centre,
        List<StructurePiece> all,
        JavaRandom rng,
        int x, int y, int z, int facing, int depth)
    {
        if (depth > 50) return null;
        if (System.Math.Abs(x - centre.CentreX) > 112 ||
            System.Math.Abs(z - centre.CentreZ) > 112) return null;
        return SelectPiece(centre, centre.PiecePool, all, rng, x, y, z, rng.NextInt(4), depth);
    }

    /// <summary>
    /// Creates a road segment if within limits (spec §4.4).
    /// </summary>
    public static StreetBetweenPieces? TryPlaceStreet(
        VillageComponent centre,
        List<StructurePiece> all,
        JavaRandom rng,
        int x, int y, int z, int facing, int depth,
        int length)
    {
        if (depth > 3 + centre.Depth) return null;
        if (System.Math.Abs(x - centre.CentreX) > 112 ||
            System.Math.Abs(z - centre.CentreZ) > 112) return null;
        if (length <= 10) return null;
        return new StreetBetweenPieces(x, y, z, facing, depth, length);
    }
}

// ── VillageComponent ──────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>yp</c> — village centre node. Holds queues for expansion.
/// Spec: Documentation/VoxelCore/Parity/Specs/VillagePieces_Spec.md §3.
/// </summary>
public sealed class VillageComponent : StructurePiece
{
    public readonly int CentreX, CentreZ;
    public new readonly int Depth;
    public readonly List<WeightedVillagePiece> PiecePool;
    public readonly Queue<StructurePiece> BuildingQueue = new();
    public readonly Queue<StructurePiece> RoadQueue     = new();
#pragma warning disable CS0169
    private WeightedVillagePiece? _lastPicked; // reserved for future weighted-rotation logic
#pragma warning restore CS0169

    public VillageComponent(int centreX, int centreY, int centreZ, int depth, JavaRandom rng)
        : base(StructureBoundingBox.FromOrigin(centreX - 1, centreY, centreZ - 1, 2, 1, 2, 0), 0, depth)
    {
        CentreX   = centreX;
        CentreZ   = centreZ;
        Depth     = depth;
        PiecePool = VillagePieceRegistry.CreatePieceList(rng, depth);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds) { }

    /// <summary>
    /// Kicks off the village expansion from the centre.
    /// Adds the initial well + 4 street stubs around it.
    /// </summary>
    public void Expand(List<StructurePiece> all, JavaRandom rng)
    {
        // Well at centre
        var well = new WellPiece(CentreX, 64, CentreZ, rng.NextInt(4), 0);
        BuildingQueue.Enqueue(well);
        all.Add(well);

        // Seed 4 road stubs outward
        foreach (int facing in new[] { 0, 1, 2, 3 })
        {
            int sx = CentreX + (facing == 1 ? -8 : facing == 3 ? 8 : 0);
            int sz = CentreZ + (facing == 0 ? 8 : facing == 2 ? -8 : 0);
            var street = VillagePieceRegistry.TryPlaceStreet(this, all, rng, sx, 64, sz, facing, 1, 8 + rng.NextInt(8));
            if (street != null) { RoadQueue.Enqueue(street); all.Add(street); }
        }
    }
}

// ── VillageStart ──────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>yo</c> — top-level village structure start.
/// Called by <see cref="MapGenVillage"/> to build the full piece list.
/// Spec: Documentation/VoxelCore/Parity/Specs/VillagePieces_Spec.md §2.
/// </summary>
public sealed class VillageStart
{
    public readonly List<StructurePiece> AllPieces = [];
    public readonly VillageComponent     Centre;
    public          bool                 IsValid;

    public VillageStart(World world, JavaRandom rng, int chunkX, int chunkZ)
    {
        int wx = (chunkX << 4) + 2;
        int wz = (chunkZ << 4) + 2;
        int wy = world.GetHeightValue(wx, wz);

        Centre = new VillageComponent(wx, wy, wz, 0, rng);
        AllPieces.Add(Centre);
        Centre.Expand(AllPieces, rng);

        // Process queues alternately until both empty (spec §2.1 step 5)
        int safety = 0;
        while ((Centre.BuildingQueue.Count > 0 || Centre.RoadQueue.Count > 0) && safety++ < 500)
        {
            StructurePiece? piece;
            if (Centre.BuildingQueue.Count > 0 && (Centre.RoadQueue.Count == 0 || rng.NextInt(2) == 0))
                piece = Centre.BuildingQueue.Dequeue();
            else
                piece = Centre.RoadQueue.Dequeue();

            piece.AddExits(Centre, AllPieces, rng);
        }

        // Valid if more than 2 non-road pieces (spec §2.2)
        int nonRoad = AllPieces.Count(p => p is not RoadBase);
        IsValid = nonRoad > 2;
    }

    /// <summary>
    /// Generates all pieces in the village that intersect the given chunk bounds.
    /// Called by MapGenVillage.Populate.
    /// </summary>
    public void Generate(World world, JavaRandom rng, int chunkX, int chunkZ)
    {
        var chunkBounds = new StructureBoundingBox(
            chunkX * 16, 0, chunkZ * 16,
            chunkX * 16 + 15, 255, chunkZ * 16 + 15);

        foreach (var piece in AllPieces)
            if (piece.BBox.Intersects(chunkBounds))
                piece.Generate(world, rng, chunkBounds);
    }
}
