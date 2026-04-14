namespace SpectraSharp.Core;

/// <summary>
/// Interface for <c>ej</c> (ChunkLoader) — loads, caches, and saves chunks on behalf of World.
///
/// Confirmed method signatures from World_Spec.md §11:
///   b(chunkX, chunkZ) → zx — load/get chunk (may generate if absent)
///   c(chunkX, chunkZ) → bool — true if chunk is currently loaded
///   a()               → void — tick (background loading/saving work per game tick)
///
/// Full <c>ej</c> spec pending (see REQUESTS.md / World_Spec.md §18 open question 4).
/// </summary>
public interface IChunkLoader
{
    /// <summary>
    /// Returns the chunk at the given chunk-grid coordinates, loading or generating it if needed.
    /// Spec: <c>b(int chunkX, int chunkZ)</c> → <c>zx</c>.
    /// </summary>
    Chunk GetChunk(int chunkX, int chunkZ);

    /// <summary>
    /// True if the chunk at the given chunk-grid coordinates is currently loaded in memory.
    /// Spec: <c>c(int chunkX, int chunkZ)</c> → bool.
    /// </summary>
    bool IsChunkLoaded(int chunkX, int chunkZ);

    /// <summary>
    /// Per-tick work: background loading, saving, generation queues.
    /// Spec: <c>a()</c>.
    /// </summary>
    void Tick();

    /// <summary>
    /// Returns the chunk-grid coordinates (chunkX, chunkZ) of every chunk currently held
    /// in memory. Used by <see cref="World.TickChunks"/> to iterate loaded chunks for
    /// random block ticks without requiring a player-proximity list.
    /// </summary>
    IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords();
}
