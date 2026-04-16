// Tests/Retrofit/TestShared.cs
// Shared infrastructure for all Retrofit tests.

using SpectraEngine.Core;

namespace SpectraEngine.Tests;

/// <summary>
/// IChunkLoader that returns empty chunks. Used to construct World instances in tests
/// where chunk content is either irrelevant or managed by the test FakeWorld directly.
/// </summary>
internal sealed class NullChunkLoader : IChunkLoader
{
    private readonly Dictionary<(int, int), Chunk> _cache = new();
    private World? _world;

    public void SetWorld(World w) => _world = w;

    public Chunk GetChunk(int cx, int cz)
    {
        if (_cache.TryGetValue((cx, cz), out var c)) return c;
        var chunk = new Chunk(_world!, cx, cz);
        _cache[(cx, cz)] = chunk;
        return chunk;
    }

    public bool IsChunkLoaded(int cx, int cz) => _cache.ContainsKey((cx, cz));
    public void Tick() { }
    public IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords() => _cache.Keys;
}
