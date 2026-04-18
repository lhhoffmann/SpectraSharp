using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Replica of <c>kd</c> (MapGenMineshaft) — Mineshaft structure generator.
///
/// Placement algorithm (spec §2.1):
///   1. Per chunk: 1% base chance (nextInt(100) == 0)
///   2. Then: nextInt(80) &lt; max(|chunkX|, |chunkZ|) — increases with distance from origin
///   Result: no mineshafts near 0,0; up to 1% at distance >= 80 chunks.
///
/// Piece types (MineshaftPieces_Spec.md):
///   - uk  (MineshaftStart):     starting node; spawns corridor exits along walls
///   - aba (MineshaftCorridor):  70% — 10/15/20 block tunnel with supports, rails, cobwebs
///   - ra  (MineshaftCrossing):  10% — 3×8×8 junction with 1 forward exit
///   - id  (MineshaftStaircase): 20% — 5×3×5 (75%) or 5×7×5 (25%) with 3 exits
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MineshaftPieces_Spec.md
/// </summary>
public sealed class MapGenMineshaft
{
    private long _worldSeed;
    private readonly JavaRandom _structureRng = new(0);

    // Cache: generated piece lists per chunk
    private readonly Dictionary<long, List<StructurePiece>> _cache = new();

    public void SetWorldSeed(long seed) => _worldSeed = seed;

    // ── Generate (populate hook) ──────────────────────────────────────────────

    public void Generate(World world, int chunkX, int chunkZ, JavaRandom rng)
    {
        for (int sx = chunkX - 1; sx <= chunkX + 1; sx++)
        for (int sz = chunkZ - 1; sz <= chunkZ + 1; sz++)
        {
            long key = (long)sx << 32 | (uint)sz;
            if (!_cache.TryGetValue(key, out var pieces))
            {
                pieces = TryGenerateMineshaft(sx, sz);
                _cache[key] = pieces;
            }

            if (pieces.Count == 0) continue;

            var genBounds = new StructureBoundingBox(
                chunkX * 16, 10, chunkZ * 16,
                chunkX * 16 + 15, World.WorldHeight - 1, chunkZ * 16 + 15);

            foreach (var piece in pieces)
                if (piece.BBox.Intersects(genBounds))
                    piece.Generate(world, rng, genBounds);
        }
    }

    // ── Placement check ───────────────────────────────────────────────────────

    private bool ShouldGenerateHere(int chunkX, int chunkZ)
    {
        long seed = (long)chunkX * 341873128712L ^ (long)chunkZ * 132897987541L ^ _worldSeed;
        _structureRng.SetSeed(seed);

        if (_structureRng.NextInt(100) != 0) return false;

        int distFactor = Math.Max(Math.Abs(chunkX), Math.Abs(chunkZ));
        return _structureRng.NextInt(80) < distFactor;
    }

    // ── Structure construction ────────────────────────────────────────────────

    private List<StructurePiece> TryGenerateMineshaft(int chunkX, int chunkZ)
    {
        if (!ShouldGenerateHere(chunkX, chunkZ)) return [];

        int originX = chunkX * 16 + 2;
        int originZ = chunkZ * 16 + 2;
        int orientation = _structureRng.NextInt(4);

        var pieces = new List<StructurePiece>();
        var start  = new MineshaftStart(originX, 50, originZ, orientation, _structureRng);
        pieces.Add(start);

        start.ExpandExits(pieces, originX, originZ, _structureRng, 1);

        return pieces;
    }
}

// ── MineshaftPieceFactory (aez) helpers ──────────────────────────────────────

internal static class MineshaftFactory
{
    private const int MaxDepth  = 8;
    private const int MaxRadius = 80;

    internal static StructurePiece? Create(
        List<StructurePiece> pieces,
        int x, int y, int z,
        int orientation, int depth,
        int rootX, int rootZ,
        JavaRandom rng)
    {
        if (depth > MaxDepth) return null;
        if (Math.Abs(x - rootX) > MaxRadius || Math.Abs(z - rootZ) > MaxRadius) return null;

        int choice = rng.NextInt(100);
        StructurePiece next = choice switch
        {
            < 70 => new MineshaftCorridor(x, y, z, orientation, depth, rng),
            < 80 => new MineshaftCrossing(x, y, z, orientation, depth),
            _    => new MineshaftStaircase(x, y, z, orientation, depth, rng),
        };

        // Overlap check
        if (pieces.Any(p => p.BBox.Intersects(next.BBox))) return null;

        return next;
    }
}

// ── MineshaftStart (uk) ───────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>uk</c> (MineshaftStart) — root node; spawns corridor exits along walls.
/// BBox: 7–12 wide, 4–9 high, 7–12 deep. Y start = 50. Spec §MineshaftStart.
/// </summary>
internal sealed class MineshaftStart : StructurePiece
{
    public MineshaftStart(int ox, int oy, int oz, int orientation, JavaRandom rng)
        : base(CreateBBox(ox, oy, oz, rng), orientation, 0) { }

    private static StructureBoundingBox CreateBBox(int ox, int oy, int oz, JavaRandom rng)
    {
        int w = 7 + rng.NextInt(6);
        int h = 4 + rng.NextInt(6);
        int d = 7 + rng.NextInt(6);
        return new StructureBoundingBox(ox, oy, oz, ox + w - 1, oy + h - 1, oz + d - 1);
    }

    public void ExpandExits(List<StructurePiece> pieces, int rootX, int rootZ, JavaRandom rng, int depth)
    {
        // Iterate 4 walls, stepping along each
        for (int wall = 0; wall < 4; wall++)
        {
            int step = rng.NextInt(BBox.SizeX);
            while (step + 3 <= BBox.SizeX)
            {
                int ex, ey = BBox.MinY, ez;
                int orientation;

                switch (wall)
                {
                    case 0: ex = BBox.MinX + step; ez = BBox.MinZ - 1; orientation = 0; break;
                    case 1: ex = BBox.MinX + step; ez = BBox.MaxZ + 1; orientation = 2; break;
                    case 2: ex = BBox.MinX - 1;    ez = BBox.MinZ + step; orientation = 3; break;
                    default: ex = BBox.MaxX + 1;   ez = BBox.MinZ + step; orientation = 1; break;
                }

                var next = MineshaftFactory.Create(pieces, ex, ey, ez, orientation, depth, rootX, rootZ, rng);
                if (next != null)
                {
                    pieces.Add(next);
                    if (next is MineshaftCorridor c) c.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
                    else if (next is MineshaftStaircase s) s.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
                }

                step += rng.NextInt(BBox.SizeX);
            }
        }
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        ClearBox(world, bounds, 0, 0, 0, BBox.SizeX - 1, BBox.SizeY - 1, BBox.SizeZ - 1);
    }
}

// ── MineshaftCorridor (aba) ───────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aba</c> — primary corridor segment. 3W × 3H × (10|15|20)L.
/// Supports every 5 blocks; rails; cobwebs when flagged; optional spawner.
/// Spec: MineshaftPieces_Spec.md §MineshaftCorridor.
/// </summary>
internal sealed class MineshaftCorridor : StructurePiece
{
    private readonly bool _hasCobwebs;  // a — 1/3 chance
    private readonly bool _hasSpawner;  // b — 1/23 if !hasCobwebs
    private bool _spawnerPlaced;

    public MineshaftCorridor(int ox, int oy, int oz, int orientation, int depth, JavaRandom rng)
        : base(CreateBBox(ox, oy, oz, orientation, rng), orientation, depth)
    {
        _hasCobwebs = rng.NextInt(3) == 0;
        _hasSpawner = !_hasCobwebs && rng.NextInt(23) == 0;
    }

    private static StructureBoundingBox CreateBBox(int ox, int oy, int oz, int orientation, JavaRandom rng)
    {
        int length = (rng.NextInt(3) + 2) * 5; // 10, 15, or 20
        return StructureBoundingBox.Create(ox, oy, oz, -1, -1, 0, 3, 3, length, orientation);
    }

    public void ExpandExits(List<StructurePiece> pieces, int rootX, int rootZ, JavaRandom rng, int depth)
    {
        int len = BBox.SizeZ;

        // Forward exit
        TryAddExit(pieces, GetWorldX(1, len), BBox.MinY, GetWorldZ(1, len), Orientation, depth, rootX, rootZ, rng);
        // Left exit
        TryAddExit(pieces, GetWorldX(-1, len / 2), BBox.MinY, GetWorldZ(-1, len / 2), (Orientation + 3) & 3, depth, rootX, rootZ, rng);
        // Right exit
        TryAddExit(pieces, GetWorldX(3, len / 2), BBox.MinY, GetWorldZ(3, len / 2), (Orientation + 1) & 3, depth, rootX, rootZ, rng);
    }

    private static void TryAddExit(List<StructurePiece> pieces,
        int x, int y, int z, int orientation, int depth,
        int rootX, int rootZ, JavaRandom rng)
    {
        var next = MineshaftFactory.Create(pieces, x, y, z, orientation, depth, rootX, rootZ, rng);
        if (next == null) return;
        pieces.Add(next);
        if (next is MineshaftCorridor c) c.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
        else if (next is MineshaftStaircase s) s.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        if (!BBox.Intersects(bounds)) return;

        // Carve tunnel
        ClearBox(world, bounds, 0, 0, 0, 2, 2, BBox.SizeZ - 1);

        int segCount = BBox.SizeZ / 5;

        for (int seg = 0; seg < segCount; seg++)
        {
            int p = 2 + seg * 5; // support position along length

            // Fence posts at floor, left + right
            PlaceBlock(world, 85, 0, 0, 0, p, bounds);
            PlaceBlock(world, 85, 0, 0, 1, p, bounds);
            PlaceBlock(world, 85, 0, 2, 0, p, bounds);
            PlaceBlock(world, 85, 0, 2, 1, p, bounds);

            // Ceiling planks crossbeam
            if (rng.NextInt(4) != 0)
            {
                PlaceBlock(world, 5, 0, 0, 2, p, bounds);
                PlaceBlock(world, 5, 0, 1, 2, p, bounds);
                PlaceBlock(world, 5, 0, 2, 2, p, bounds);
            }
            else
            {
                PlaceBlock(world, 5, 0, 0, 2, p, bounds);
                PlaceBlock(world, 5, 0, 2, 2, p, bounds);
            }

            // Cobwebs near crossbeam (only when _hasCobwebs flag set, spec §2.4)
            if (_hasCobwebs)
            {
                if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 0, 2, p - 1, bounds);
                if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 2, 2, p - 1, bounds);
                if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 0, 2, p + 1, bounds);
                if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 2, 2, p + 1, bounds);
                if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 0, 2, p - 2, bounds);
                if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 2, 2, p - 2, bounds);
                if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 0, 2, p + 2, bounds);
                if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 2, 2, p + 2, bounds);
            }

            // Torches (5% chance per side)
            if (rng.NextInt(20) == 0) PlaceBlock(world, 50, 0, 1, 2, p - 1, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 50, 0, 1, 2, p + 1, bounds);

            // Cave spider spawner (once)
            if (_hasSpawner && !_spawnerPlaced)
            {
                int spawnZ = p - 1 + rng.NextInt(3);
                int wx = GetWorldX(1, spawnZ);
                int wy = GetWorldY(0);
                int wz = GetWorldZ(1, spawnZ);
                if (bounds.Contains(wx, wy, wz))
                {
                    world.SetBlock(wx, wy, wz, 52);
                    if (world.GetTileEntity(wx, wy, wz) is TileEntity.TileEntityMobSpawner sp)
                        sp.EntityTypeId = "CaveSpider";
                    _spawnerPlaced = true;
                }
            }

            // Loot chest (1% per support)
            if (rng.NextInt(100) == 0)
            {
                int cx = GetWorldX(0, p - 1);
                int cy = GetWorldY(0);
                int cz = GetWorldZ(0, p - 1);
                if (bounds.Contains(cx, cy, cz))
                    world.SetBlock(cx, cy, cz, 54);
            }
        }

        // Rails along floor (70% chance per block)
        for (int z = 0; z < BBox.SizeZ; z++)
            if (rng.NextInt(10) < 7)
                PlaceBlock(world, 66, 0, 1, 0, z, bounds);
    }
}

// ── MineshaftCrossing (ra) ────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ra</c> — 4-way junction. 3W × 8H × 8D. 1 forward exit.
/// Spec: MineshaftPieces_Spec.md §MineshaftCrossing.
/// </summary>
internal sealed class MineshaftCrossing : StructurePiece
{
    public MineshaftCrossing(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, -1, 0, 3, 8, 8, orientation), orientation, depth) { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        if (!BBox.Intersects(bounds)) return;

        // Carve interior air
        ClearBox(world, bounds, 0, 0, 0, 2, 6, 7);

        // Diagonal staircase fill: blocks from y=5 down to y=1 (spec §MineshaftCrossing)
        for (int step = 0; step < 5; step++)
        {
            int ly = 5 - step;
            int lz = step;
            FillBox(world, bounds, 0, ly, lz, 2, 7, lz, 98, 98, false); // stone brick
        }
    }
}

// ── MineshaftStaircase (id) ───────────────────────────────────────────────────

/// <summary>
/// Replica of <c>id</c> — staircase junction. 5W × (3|7)H × 5D. 3 exits.
/// Normal (75%): 5×3×5. Tall (25%): 5×7×5.
/// Spec: MineshaftPieces_Spec.md §MineshaftStaircase.
/// </summary>
internal sealed class MineshaftStaircase : StructurePiece
{
    private readonly bool _isTall;

    public MineshaftStaircase(int ox, int oy, int oz, int orientation, int depth, JavaRandom rng)
        : base(CreateBBox(ox, oy, oz, orientation, rng, out bool tall), orientation, depth)
    {
        _isTall = tall;
    }

    private static StructureBoundingBox CreateBBox(
        int ox, int oy, int oz, int orientation, JavaRandom rng, out bool tall)
    {
        tall = rng.NextInt(4) > 2; // 25% tall
        int height = tall ? 7 : 3;
        return StructureBoundingBox.Create(ox, oy, oz, -2, -1, 0, 5, height, 5, orientation);
    }

    public void ExpandExits(List<StructurePiece> pieces, int rootX, int rootZ, JavaRandom rng, int depth)
    {
        // Left, right, forward exits
        for (int dir = -1; dir <= 1; dir++)
        {
            int orientation = (Orientation + dir + 4) & 3;
            int ex = GetWorldX(2, BBox.SizeZ);
            int ey = BBox.MinY;
            int ez = GetWorldZ(2, BBox.SizeZ);

            var next = MineshaftFactory.Create(pieces, ex, ey, ez, orientation, depth, rootX, rootZ, rng);
            if (next == null) continue;
            pieces.Add(next);
            if (next is MineshaftCorridor c) c.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
        }

        // Tall variant: additional exits at y+4
        if (_isTall)
        {
            var next = MineshaftFactory.Create(pieces,
                GetWorldX(2, BBox.SizeZ), BBox.MinY + 4, GetWorldZ(2, BBox.SizeZ),
                Orientation, depth, rootX, rootZ, rng);
            if (next != null)
            {
                pieces.Add(next);
                if (next is MineshaftCorridor c) c.ExpandExits(pieces, rootX, rootZ, rng, depth + 1);
            }
        }
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        if (!BBox.Intersects(bounds)) return;
        ClearBox(world, bounds, 0, 0, 0, 4, BBox.SizeY - 1, 4);
    }
}
