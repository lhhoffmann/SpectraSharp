using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Replica of <c>dc</c> (MapGenStronghold) — places 3 strongholds per world.
///
/// Placement algorithm (spec §3.1):
///   3 strongholds at roughly 40–64 chunks (640–1024 blocks) from origin,
///   evenly distributed at 120° angles (2π/3 apart), with valid-biome preference.
///
/// Positions are computed once on first call (dc.f flag) and cached.
/// Block palette: Stone Brick (ID 98) — full piece implementation pending StrongholdPieces spec.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenStructures_Spec.md §3
/// </summary>
public sealed class MapGenStronghold
{
    private const int StrongholdCount = 3;

    private long _worldSeed;
    private bool _positionsComputed; // dc.f — computed only once

    /// <summary>Cached stronghold chunk coordinates (3 positions).</summary>
    private readonly (int chunkX, int chunkZ)[] _positions = new (int, int)[StrongholdCount];

    // Cache of generated structure pieces per chunk
    private readonly Dictionary<long, List<StructurePiece>> _cache = new();

    public void SetWorldSeed(long seed) => _worldSeed = seed;

    // ── Generate (populate hook) ──────────────────────────────────────────────

    public void Generate(World world, int chunkX, int chunkZ, JavaRandom rng)
    {
        EnsurePositionsComputed(world);

        for (int sx = chunkX - 1; sx <= chunkX + 1; sx++)
        for (int sz = chunkZ - 1; sz <= chunkZ + 1; sz++)
        {
            long key = (long)sx << 32 | (uint)sz;
            if (!_cache.TryGetValue(key, out var pieces))
            {
                pieces = TryGenerateStronghold(world, sx, sz);
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

    // ── Placement computation (spec §3.1) ────────────────────────────────────

    private void EnsurePositionsComputed(World world)
    {
        if (_positionsComputed) return;
        _positionsComputed = true;

        var rng = new JavaRandom(_worldSeed);
        double angle = rng.NextDouble() * Math.PI * 2.0;

        for (int i = 0; i < StrongholdCount; i++)
        {
            // Distance: (1.25 + rng.nextDouble()) × 32.0 chunks ≈ 40–64 chunks
            double dist = (1.25 + rng.NextDouble()) * 32.0;

            int chunkX = (int)Math.Round(Math.Cos(angle) * dist);
            int chunkZ = (int)Math.Round(Math.Sin(angle) * dist);

            // Biome preference: find nearest valid biome within 112 blocks (spec §3.1)
            // Stub: use the computed position directly (biome search requires WorldChunkManager)
            // Vanilla prints "Placed stronghold in INVALID biome" when no valid biome found — we skip the warning
            _positions[i] = (chunkX, chunkZ);

            // Advance angle by 2π/3 for even distribution
            angle += Math.PI * 2.0 / StrongholdCount;
        }
    }

    private bool ShouldGenerateHere(int chunkX, int chunkZ)
    {
        foreach (var (cx, cz) in _positions)
            if (cx == chunkX && cz == chunkZ)
                return true;
        return false;
    }

    private List<StructurePiece> TryGenerateStronghold(World world, int chunkX, int chunkZ)
    {
        if (!ShouldGenerateHere(chunkX, chunkZ)) return [];

        // Stub: place stone-brick starting room (aeh — StrongholdStartRoom pending piece spec)
        // The full piece expansion (kg / StrongholdStart) is deferred to StrongholdPieces spec.
        var rng = new JavaRandom((long)chunkX * 341873128712L ^ (long)chunkZ * 132897987541L ^ _worldSeed);
        var start = new StrongholdStartRoomStub(chunkX * 16 + 2, 50, chunkZ * 16 + 2, rng.NextInt(4), 0);
        return [start];
    }
}

// ── StrongholdStartRoomStub (aeh approximation) ──────────────────────────────

/// <summary>
/// Stub for the starting room (aeh) of a stronghold.
/// Places a minimal stone brick shell at the stronghold origin.
/// Full piece expansion requires StrongholdPieces_Spec (pending).
/// </summary>
internal sealed class StrongholdStartRoomStub : StructurePiece
{
    private const int StoneBrickId = 98; // yy.bm.bM

    public StrongholdStartRoomStub(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -4, -1, 0, 10, 5, 11, orientation), orientation, depth)
    { }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Shell: stone brick walls, floor, ceiling
        FillBox(world, bounds, 0, 0, 0, 9, 0, 10, StoneBrickId);   // floor
        FillBox(world, bounds, 0, 4, 0, 9, 4, 10, StoneBrickId);   // ceiling
        FillBox(world, bounds, 0, 1, 0, 0, 3, 10, StoneBrickId);   // west wall
        FillBox(world, bounds, 9, 1, 0, 9, 3, 10, StoneBrickId);   // east wall
        FillBox(world, bounds, 0, 1, 0, 9, 3, 0, StoneBrickId);    // north wall
        FillBox(world, bounds, 0, 1, 10, 9, 3, 10, StoneBrickId);  // south wall
        // Interior air
        ClearBox(world, bounds, 1, 1, 1, 8, 3, 9);
    }
}
