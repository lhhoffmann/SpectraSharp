using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Replica of <c>kd</c> (MapGenMineshaft) — Mineshaft structure generator.
///
/// Placement algorithm (spec §2.1):
///   1. Per chunk: 1% base chance (nextInt(100) == 0)
///   2. Then: nextInt(80) &lt; max(|chunkX|, |chunkZ|) — increases with distance from origin
///   Result: no mineshafts near 0,0; up to 1% at distance ≥ 80 chunks.
///
/// Structure: corridor-based tunnel system with wooden supports, rails, cobwebs, torches,
/// cave spider spawners, and chest wagons (loot table in spec §2.5).
///
/// Piece types:
///   - aba (MineshaftCorridor): primary piece; 10-30 long, 3 wide, 3 high
///   - id  (MineshaftRoom): 20% chance; stub
///   - ra  (MineshaftStairs): 10% chance; stub
///
/// Note: Full piece-by-piece geometry (aba/id/ra) is complex; aba is the primary piece.
/// The other pieces (id/ra) are stubbed as dead ends pending a dedicated mineshaft piece spec.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenStructures_Spec.md §2
/// </summary>
public sealed class MapGenMineshaft
{
    // Block ID constants
    private const int WoodPlanksId   = 5;
    private const int WoodFenceId    = 85;
    private const int RailId         = 66;
    private const int CobwebId       = 30;
    private const int TorchId        = 50;
    private const int MobSpawnerId   = 52;
    private const int ChestId        = 54;

    private long _worldSeed;
    private readonly JavaRandom _structureRng = new(0);

    // Cache: generated piece lists per chunk
    private readonly Dictionary<long, List<StructurePiece>> _cache = new();

    public void SetWorldSeed(long seed) => _worldSeed = seed;

    // ── Generate (populate hook) (spec §1) ───────────────────────────────────

    /// <summary>
    /// Places mineshaft blocks in the given chunk column.
    /// Checks a 3×3 neighbourhood of chunk origins for shafts that may overlap.
    /// </summary>
    public void Generate(World world, int chunkX, int chunkZ, JavaRandom rng)
    {
        for (int sx = chunkX - 1; sx <= chunkX + 1; sx++)
        for (int sz = chunkZ - 1; sz <= chunkZ + 1; sz++)
        {
            long key = (long)sx << 32 | (uint)sz;
            if (!_cache.TryGetValue(key, out var pieces))
            {
                pieces = TryGenerateMineshaft(world, sx, sz);
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

    // ── Placement check (spec §2.1) ───────────────────────────────────────────

    private bool ShouldGenerateHere(int chunkX, int chunkZ)
    {
        long seed = (long)chunkX * 341873128712L ^ (long)chunkZ * 132897987541L ^ _worldSeed;
        _structureRng.SetSeed(seed);

        if (_structureRng.NextInt(100) != 0) return false; // 1% base chance

        int distFactor = Math.Max(Math.Abs(chunkX), Math.Abs(chunkZ));
        return _structureRng.NextInt(80) < distFactor;
    }

    // ── Structure construction (spec §2.2) ────────────────────────────────────

    private List<StructurePiece> TryGenerateMineshaft(World world, int chunkX, int chunkZ)
    {
        if (!ShouldGenerateHere(chunkX, chunkZ)) return [];

        int originX = chunkX * 16 + 2;
        int originZ = chunkZ * 16 + 2;
        int orientation = _structureRng.NextInt(4);

        var pieces = new List<StructurePiece>();
        var start = new MineshaftCorridor(originX, 50, originZ, orientation, 0, _structureRng);
        pieces.Add(start);

        // Expand exits recursively up to depth 8, radius 80 (spec §2.3)
        ExpandExits(pieces, start, originX, originZ, _structureRng, 1);

        return pieces;
    }

    private void ExpandExits(List<StructurePiece> pieces, MineshaftCorridor piece,
                              int rootX, int rootZ, JavaRandom rng, int depth)
    {
        if (depth > 8) return;

        foreach (var (ex, ey, ez, eOrientation) in piece.GetExits())
        {
            // Radius guard (spec §2.3)
            if (Math.Abs(ex - rootX) > 80 || Math.Abs(ez - rootZ) > 80) continue;

            int choice = rng.NextInt(100);
            StructurePiece? next = null;

            if (choice >= 80)        // 20% room (stub — just a dead-end corridor)
                next = new MineshaftCorridor(ex, ey, ez, eOrientation, depth, rng);
            else if (choice >= 70)   // 10% staircase (stub — same as corridor)
                next = new MineshaftCorridor(ex, ey, ez, eOrientation, depth, rng);
            else                     // 70% corridor
                next = new MineshaftCorridor(ex, ey, ez, eOrientation, depth, rng);

            if (next == null) continue;

            // Check bounding-box overlap (spec §2.3 — if chosen piece cannot fit)
            bool overlaps = pieces.Any(p => p.BBox.Intersects(next.BBox));
            if (overlaps) continue;

            pieces.Add(next);
            if (next is MineshaftCorridor corr)
                ExpandExits(pieces, corr, rootX, rootZ, rng, depth + 1);
        }
    }
}

// ── MineshaftCorridor (aba) ───────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aba</c> — primary mineshaft corridor segment.
/// Length: 10–30 blocks (2–6 segments × 5 per segment). Width: 3. Height: 3.
/// Contains wooden fence supports, rails, cobwebs, torches, optional spawner.
/// Source spec: WorldGenStructures_Spec §2.4
/// </summary>
internal sealed class MineshaftCorridor : StructurePiece
{
    private readonly bool _isMain;         // a — 1/3 chance: special floor
    private readonly bool _hasSpawner;     // b — 1/23 chance (when !isMain): cave spider spawner
    private bool _spawnerPlaced;           // c

    private static readonly List<(int weight, int[] items)> s_lootTable =
    [
        (10, [50, 1, 5]),   // torch    1-5
        (5,  [66, 1, 3]),   // rails    1-3
        (5,  [331, 4, 9]),  // redstone 4-9
        (5,  [351, 4, 9]),  // lapis    4-9 (dye dam=4)
        (3,  [288, 1, 2]),  // feather  1-2
        (10, [66, 3, 8]),   // rails    3-8
        (15, [297, 1, 3]),  // bread    1-3
    ];

    // Exits: list of (worldX, worldY, worldZ, orientation) for child pieces
    private readonly List<(int, int, int, int)> _exits = [];

    public IReadOnlyList<(int, int, int, int)> GetExits() => _exits;

    public MineshaftCorridor(int ox, int oy, int oz, int orientation, int depth, JavaRandom rng)
        : base(CreateBoundingBox(ox, oy, oz, orientation, rng), orientation, depth)
    {
        _isMain     = rng.NextInt(3) == 0;
        _hasSpawner = !_isMain && rng.NextInt(23) == 0;

        // Register exits (forward + optional left/right) at far end
        int segCount = (BBox.SizeZ - 2) / 5; // approx
        RegisterExits(ox, oy, oz, orientation, segCount);
    }

    private static StructureBoundingBox CreateBoundingBox(int ox, int oy, int oz, int orientation, JavaRandom rng)
    {
        int segments = rng.NextInt(5) + 2; // 2–6
        int length   = segments * 5 + 2;   // 12–32
        return StructureBoundingBox.Create(ox, oy, oz, -1, -1, 0, 3, 3, length, orientation);
    }

    private void RegisterExits(int ox, int oy, int oz, int orientation, int segCount)
    {
        // Forward exit at far end
        (int fwdX, int fwdZ) = StepForward(ox, oz, orientation, BBox.SizeZ - 1);
        _exits.Add((fwdX, oy, fwdZ, orientation));

        // Left / right exits (1/5 chance each, at each support along corridor — simplified to 1 each)
        _exits.Add((ox, oy, oz, (orientation + 3) & 3));
        _exits.Add((ox, oy, oz, (orientation + 1) & 3));
    }

    private static (int, int) StepForward(int ox, int oz, int orientation, int steps)
        => orientation switch
        {
            0 => (ox, oz + steps),
            1 => (ox - steps, oz),
            2 => (ox, oz - steps),
            3 => (ox + steps, oz),
            _ => (ox, oz + steps),
        };

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        if (!BBox.Intersects(bounds)) return;

        int segCount = (BBox.SizeZ - 2) / 5;

        // Per-segment layout (spec §2.4)
        for (int seg = 0; seg < segCount; seg++)
        {
            int var10 = 2 + seg * 5;

            // Fence posts (left + right)
            PlaceBlock(world, 85, 0, 0, 0, var10, bounds); // left post bottom
            PlaceBlock(world, 85, 0, 0, 1, var10, bounds); // left post top
            PlaceBlock(world, 85, 0, 2, 0, var10, bounds); // right post bottom
            PlaceBlock(world, 85, 0, 2, 1, var10, bounds); // right post top

            // Ceiling planks (75% full, 25% sides only)
            if (rng.NextInt(4) != 0)
            {
                PlaceBlock(world, 5, 0, 0, 2, var10, bounds);
                PlaceBlock(world, 5, 0, 1, 2, var10, bounds);
                PlaceBlock(world, 5, 0, 2, 2, var10, bounds);
            }
            else
            {
                PlaceBlock(world, 5, 0, 0, 2, var10, bounds);
                PlaceBlock(world, 5, 0, 2, 2, var10, bounds);
            }

            // Rails
            PlaceBlock(world, 66, 0, 0, 0, var10, bounds);
            PlaceBlock(world, 66, 0, 2, 0, var10, bounds);

            // Cobwebs (spec §2.4 probabilities)
            if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 0, 2, var10 - 1, bounds);
            if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 2, 2, var10 - 1, bounds);
            if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 0, 2, var10 + 1, bounds);
            if (rng.NextInt(10) == 0) PlaceBlock(world, 30, 0, 2, 2, var10 + 1, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 0, 2, var10 - 2, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 2, 2, var10 - 2, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 0, 2, var10 + 2, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 30, 0, 2, 2, var10 + 2, bounds);

            // Torches (5% chance)
            if (rng.NextInt(20) == 0) PlaceBlock(world, 50, 0, 1, 2, var10 - 1, bounds);
            if (rng.NextInt(20) == 0) PlaceBlock(world, 50, 0, 1, 2, var10 + 1, bounds);

            // Cave spider spawner (if flagged and not yet placed)
            if (_hasSpawner && !_spawnerPlaced)
            {
                int spawnZ = var10 - 1 + rng.NextInt(3);
                int wx = GetWorldX(1, spawnZ);
                int wy = GetWorldY(0);
                int wz = GetWorldZ(1, spawnZ);
                if (bounds.Contains(wx, wy, wz))
                {
                    world.SetBlock(wx, wy, wz, 52);
                    if (world.GetTileEntity(wx, wy, wz) is TileEntity.TileEntityMobSpawner spawner)
                        spawner.EntityTypeId = "CaveSpider";
                    _spawnerPlaced = true;
                }
            }
        }
    }
}
