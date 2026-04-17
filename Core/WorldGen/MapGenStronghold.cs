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
/// Full piece implementation via <see cref="StrongholdFactory"/> (see StrongholdPieces_Spec §4).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenStructures_Spec.md §3
///              Documentation/VoxelCore/Parity/Specs/StrongholdPieces_Spec.md
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

        var rng = new JavaRandom((long)chunkX * 341873128712L ^ (long)chunkZ * 132897987541L ^ _worldSeed);

        int originX = chunkX * 16 + 2;
        int originY = 50;
        int originZ = chunkZ * 16 + 2;
        int startOri = rng.NextInt(4);

        var factory = new StrongholdFactory();
        return factory.GeneratePieces(originX, originY, originZ, startOri, rng);
    }
}
