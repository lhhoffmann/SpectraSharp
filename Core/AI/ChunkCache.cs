namespace SpectraEngine.Core.AI;

/// <summary>
/// Replica of <c>xk</c> (ChunkCache) — a pre-fetched chunk grid snapshot used by
/// <see cref="PathFinder"/> as its world view. Avoids repeated chunk-map lookups
/// during A* by caching a contiguous 2-D chunk array at construction.
///
/// The array <c>_chunks[cx - MinChunkX][cz - MinChunkZ]</c> holds every chunk in
/// the search bounding box. Out-of-bounds or unloaded slots are null.
///
/// Fields (spec §6):
///   a = MinChunkX (bbox.minX >> 4)
///   b = MinChunkZ (bbox.minZ >> 4)
///   c = _chunks[cx-a][cz-b]
///   d = world reference
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §6
/// </summary>
public sealed class ChunkCache
{
    // ── Fields ────────────────────────────────────────────────────────────────

    /// <summary>obf: a — minimum chunk X index.</summary>
    private readonly int _minChunkX;

    /// <summary>obf: b — minimum chunk Z index.</summary>
    private readonly int _minChunkZ;

    /// <summary>obf: c — pre-fetched chunk grid [cx - minChunkX][cz - minChunkZ].</summary>
    private readonly Chunk?[][] _chunks;

    /// <summary>obf: d — world reference (for height, etc.).</summary>
    private readonly World _world;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a ChunkCache spanning the block-coordinate bounding box
    /// (minX, minY, minZ) – (maxX, maxY, maxZ).
    /// All chunks in the box are fetched eagerly from <paramref name="world"/>.
    /// </summary>
    public ChunkCache(World world, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        _world     = world;
        _minChunkX = minX >> 4;
        _minChunkZ = minZ >> 4;

        int maxChunkX = maxX >> 4;
        int maxChunkZ = maxZ >> 4;

        int countX = maxChunkX - _minChunkX + 1;
        int countZ = maxChunkZ - _minChunkZ + 1;

        _chunks = new Chunk?[countX][];
        for (int xi = 0; xi < countX; xi++)
        {
            _chunks[xi] = new Chunk?[countZ];
            for (int zi = 0; zi < countZ; zi++)
            {
                int cx = _minChunkX + xi;
                int cz = _minChunkZ + zi;
                // world.c(chunkX, chunkZ) — get or generate chunk
                _chunks[xi][zi] = world.IsChunkLoaded(cx, cz)
                    ? world.GetChunkFromChunkCoords(cx, cz)
                    : null;
            }
        }
    }

    // ── World view API ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the block ID at (x, y, z), or 0 if out-of-bounds / unloaded.
    /// Mirrors <c>xk.a(x,y,z)</c> — named getBlockId in the spec.
    /// </summary>
    public int GetBlockId(int x, int y, int z)
    {
        if (y < 0 || y >= World.WorldHeight) return 0;

        int cx  = (x >> 4) - _minChunkX;
        int cz  = (z >> 4) - _minChunkZ;

        if (cx < 0 || cx >= _chunks.Length) return 0;
        var col = _chunks[cx];
        if (cz < 0 || cz >= col.Length)    return 0;

        Chunk? chunk = col[cz];
        if (chunk == null) return 0;

        return chunk.GetBlockId(x & 15, y, z & 15);
    }

    /// <summary>
    /// Returns the material at (x, y, z). Used by walkability check.
    /// Mirrors <c>xk.e(x,y,z)</c> — named getMaterial in the spec.
    /// </summary>
    public Material GetMaterial(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        if (id == 0) return Material.Air;
        Block? block = Block.BlocksList[id];
        return block?.BlockMaterial ?? Material.Air;
    }

    /// <summary>
    /// Returns block metadata at (x, y, z).  Used for door-open check.
    /// </summary>
    public int GetBlockMetadata(int x, int y, int z)
    {
        if (y < 0 || y >= World.WorldHeight) return 0;

        int cx  = (x >> 4) - _minChunkX;
        int cz  = (z >> 4) - _minChunkZ;

        if (cx < 0 || cx >= _chunks.Length) return 0;
        var col = _chunks[cx];
        if (cz < 0 || cz >= col.Length)    return 0;

        Chunk? chunk = col[cz];
        return chunk?.GetMetadata(x & 15, y, z & 15) ?? 0;
    }

    /// <summary>World height (needed by <see cref="GetBlockId"/> bounds check).</summary>
    public int Height => World.WorldHeight;
}
