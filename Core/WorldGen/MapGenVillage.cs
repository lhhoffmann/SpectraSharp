using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Replica of <c>xn</c> (MapGenVillage) — Village structure generator.
///
/// Placement algorithm (spec §4.1):
///   32-chunk (512-block) grid. Per chunk: compute grid cell, generate per-cell RNG,
///   offset origin within [0,23] in X and [0,23] in Z. If chunk matches grid origin
///   AND biome is plains (sr.c) or desert (sr.d) → place village.
///
/// Returns a boolean from populate() — true suppresses dungeon spawning in that chunk.
///
/// Note: Full village piece list (yp/xy.a — roads, houses, well, farm, blacksmith) is
/// deferred to a VillagePieces spec. Current stub places no actual blocks.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenStructures_Spec.md §4
/// </summary>
public sealed class MapGenVillage
{
    private const int GridSize = 32; // 32-chunk = 512-block grid

    private long _worldSeed;

    // Known village biome IDs (sr.c = plains, sr.d = desert)
    // Biome IDs from BiomeGenBase: Plains=1, Desert=2
    private static readonly HashSet<int> ValidBiomeIds = new() { 1, 2 }; // plains, desert

    private readonly Dictionary<long, bool>          _isVillageChunk = new();
    private readonly Dictionary<(int,int), VillageStart> _villageStarts  = new();

    public void SetWorldSeed(long seed) => _worldSeed = seed;

    // ── Generate: returns true if this chunk contains a village (spec §4) ────

    /// <summary>
    /// Attempts to generate village structures in the chunk.
    /// Returns true if a village origin was found (suppresses dungeon spawn).
    /// </summary>
    public bool Generate(World world, int chunkX, int chunkZ, JavaRandom rng)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (_isVillageChunk.TryGetValue(key, out bool cached))
            return cached;

        bool result = IsVillageOriginChunk(world, chunkX, chunkZ);
        _isVillageChunk[key] = result;

        if (result)
        {
            var start = new VillageStart(world, rng, chunkX, chunkZ);
            if (start.IsValid)
                _villageStarts[(chunkX, chunkZ)] = start;
            else
                result = false;
        }

        return result;
    }

    // ── Grid placement check (spec §4.1) ─────────────────────────────────────

    private bool IsVillageOriginChunk(World world, int chunkX, int chunkZ)
    {
        // Compute grid cell with negative-coordinate support
        int gridX = Math.DivRem(chunkX, GridSize, out int remX);
        if (remX < 0) { gridX--; remX += GridSize; }
        int gridZ = Math.DivRem(chunkZ, GridSize, out int remZ);
        if (remZ < 0) { gridZ--; remZ += GridSize; }

        // Per-cell RNG (world.x(gridX, gridZ, 10387312) equivalent)
        long cellSeed = (long)gridX * 341873128712L ^ (long)gridZ * 132897987541L
                      ^ 10387312L ^ _worldSeed;
        var cellRng = new JavaRandom(cellSeed);

        // Offset within cell [0, 23]
        int targetX = gridX * GridSize + cellRng.NextInt(GridSize - 8);
        int targetZ = gridZ * GridSize + cellRng.NextInt(GridSize - 8);

        if (chunkX != targetX || chunkZ != targetZ) return false;

        // Biome check at chunk centre (spec §4.1 step 6)
        int biomeId = world.ChunkManager?.GetBiomeAt(chunkX * 16 + 8, chunkZ * 16 + 8).BiomeId ?? 1;
        return ValidBiomeIds.Contains(biomeId);
    }

    /// <summary>
    /// Generates village blocks into the given chunk (called during populate phase).
    /// Finds any village starts whose pieces overlap this chunk and calls their Generate.
    /// </summary>
    public void Populate(World world, JavaRandom rng, int chunkX, int chunkZ)
    {
        foreach (var kvp in _villageStarts)
        {
            var start = kvp.Value;
            // Only generate if any piece in this start touches this chunk
            var chunkBounds = new Structure.StructureBoundingBox(
                chunkX * 16, 0, chunkZ * 16,
                chunkX * 16 + 15, 255, chunkZ * 16 + 15);
            if (start.AllPieces.Any(p => p.BBox.Intersects(chunkBounds)))
                start.Generate(world, rng, chunkX, chunkZ);
        }
    }
}
