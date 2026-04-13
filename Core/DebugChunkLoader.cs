namespace SpectraSharp.Core;

/// <summary>
/// Simple in-memory IChunkLoader for the debug world.
/// All requested chunks are created on-demand and filled with a flat terrain:
///   Y=0 — Stone (ID 1)
///   Y=1 — Grass (ID 2)
///   Y≥2 — Air  (ID 0)
/// World reference must be set via <see cref="SetWorld"/> before the first chunk request.
/// </summary>
public sealed class DebugChunkLoader : IChunkLoader
{
    private readonly Dictionary<long, Chunk> _chunks = new();
    private World? _world;

    /// <summary>Set immediately after World construction (before any block operations).</summary>
    public void SetWorld(World world) => _world = world;

    // ── IChunkLoader ──────────────────────────────────────────────────────────

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (_chunks.TryGetValue(key, out Chunk? existing)) return existing;

        if (_world == null)
            throw new InvalidOperationException("DebugChunkLoader: World not set before first GetChunk call");

        var chunk = new Chunk(_world, chunkX, chunkZ);
        chunk.IsLoaded    = true;
        chunk.IsPopulated = true;
        _chunks[key] = chunk;
        return chunk;
    }

    public bool IsChunkLoaded(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        return _chunks.ContainsKey(key);
    }

    public void Tick() { }
}
