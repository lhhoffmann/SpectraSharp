using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SpectraSharp.Core;

namespace SpectraSharp.Tests;

// ---------------------------------------------------------------------------
// Hand-written fakes
// ---------------------------------------------------------------------------

/// <summary>
/// Minimal fake World that satisfies whatever DebugChunkLoader needs from it.
/// We only need to pass it to Chunk construction; we do not simulate full World logic.
/// </summary>
file sealed class FakeWorld : World
{
    public FakeWorld(IChunkLoader loader) : base(loader, 0L) { }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

public sealed class DebugChunkLoaderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (DebugChunkLoader loader, World world) BuildSystem()
    {
        var loader = new DebugChunkLoader();
        var world  = new FakeWorld(loader);
        loader.SetWorld(world);
        return (loader, world);
    }

    // ── 1. World-not-set guard ────────────────────────────────────────────────

    [Fact]
    public void GetChunk_BeforeSetWorld_ThrowsInvalidOperationException()
    {
        var loader = new DebugChunkLoader(); // no SetWorld call

        var ex = Assert.Throws<InvalidOperationException>(() => loader.GetChunk(0, 0));
        Assert.Contains("World not set", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── 2. SetWorld / GetChunk basic flow ─────────────────────────────────────

    [Fact]
    public void GetChunk_AfterSetWorld_ReturnsNonNullChunk()
    {
        var (loader, _) = BuildSystem();

        Chunk chunk = loader.GetChunk(0, 0);

        Assert.NotNull(chunk);
    }

    [Fact]
    public void GetChunk_SameCoordinates_ReturnsSameInstance()
    {
        var (loader, _) = BuildSystem();

        Chunk first  = loader.GetChunk(3, -5);
        Chunk second = loader.GetChunk(3, -5);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetChunk_DifferentCoordinates_ReturnsDifferentInstances()
    {
        var (loader, _) = BuildSystem();

        Chunk a = loader.GetChunk(0, 0);
        Chunk b = loader.GetChunk(1, 0);
        Chunk c = loader.GetChunk(0, 1);

        Assert.NotSame(a, b);
        Assert.NotSame(a, c);
        Assert.NotSame(b, c);
    }

    // ── 3. Chunk coordinate properties ───────────────────────────────────────

    [Theory]
    [InlineData(0,  0)]
    [InlineData(1,  0)]
    [InlineData(0,  1)]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(7, -13)]
    [InlineData(-100, 200)]
    public void GetChunk_ChunkHasCorrectChunkCoordinates(int cx, int cz)
    {
        var (loader, _) = BuildSystem();

        Chunk chunk = loader.GetChunk(cx, cz);

        Assert.Equal(cx, chunk.ChunkX);
        Assert.Equal(cz, chunk.ChunkZ);
    }

    // ── 4. IsLoaded / IsPopulated flags ──────────────────────────────────────

    [Fact]
    public void GetChunk_NewChunk_IsLoadedTrue()
    {
        var (loader, _) = BuildSystem();

        Chunk chunk = loader.GetChunk(0, 0);

        Assert.True(chunk.IsLoaded);
    }

    [Fact]
    public void GetChunk_NewChunk_IsPopulatedTrue()
    {
        var (loader, _) = BuildSystem();

        Chunk chunk = loader.GetChunk(0, 0);

        Assert.True(chunk.IsPopulated);
    }

    // ── 5. IsChunkLoaded ─────────────────────────────────────────────────────

    [Fact]
    public void IsChunkLoaded_BeforeGetChunk_ReturnsFalse()
    {
        var (loader, _) = BuildSystem();

        Assert.False(loader.IsChunkLoaded(5, 5));
    }

    [Fact]
    public void IsChunkLoaded_AfterGetChunk_ReturnsTrue()
    {
        var (loader, _) = BuildSystem();

        loader.GetChunk(5, 5);

        Assert.True(loader.IsChunkLoaded(5, 5));
    }

    [Fact]
    public void IsChunkLoaded_DifferentCoordNotLoaded_ReturnsFalse()
    {
        var (loader, _) = BuildSystem();

        loader.GetChunk(0, 0);

        Assert.False(loader.IsChunkLoaded(1, 0));
        Assert.False(loader.IsChunkLoaded(0, 1));
    }

    // ── 6. Chunk key collision — negative coordinates ─────────────────────────
    // The key packing is: (long)chunkX << 32 | (uint)chunkZ
    // We verify that (1, 0) and (0, 1) do not collide, and that negative Z
    // values are handled correctly (stored as uint).

    [Theory]
    [InlineData( 1,  0,   0,  1)]
    [InlineData(-1,  0,   0, -1)]
    [InlineData( 1, -1,  -1,  1)]
    [InlineData(int.MaxValue, 0, 0, int.MaxValue)]
    public void GetChunk_DistinctNegativeCoordinatePairs_AreStoredSeparately(
        int cx1, int cz1, int cx2, int cz2)
    {
        var (loader, _) = BuildSystem();

        Chunk a = loader.GetChunk(cx1, cz1);
        Chunk b = loader.GetChunk(cx2, cz2);

        Assert.NotSame(a, b);
        Assert.True(loader.IsChunkLoaded(cx1, cz1));
        Assert.True(loader.IsChunkLoaded(cx2, cz2));
    }

    // ── 7. GetLoadedChunkCoords ───────────────────────────────────────────────

    [Fact]
    public void GetLoadedChunkCoords_InitiallyEmpty()
    {
        var (loader, _) = BuildSystem();

        IEnumerable<(int, int)> coords = loader.GetLoadedChunkCoords();

        Assert.Empty(coords);
    }

    [Fact]
    public void GetLoadedChunkCoords_AfterSeveralGetChunks_ContainsExactlyThoseCoords()
    {
        var (loader, _) = BuildSystem();

        var expected = new (int, int)[] { (0, 0), (1, -1), (-3, 7) };

        foreach (var (cx, cz) in expected)
            loader.GetChunk(cx, cz);

        var actual = loader.GetLoadedChunkCoords().OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToList();
        var sorted = expected.OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToList();

        Assert.Equal(sorted, actual);
    }

    [Fact]
    public void GetLoadedChunkCoords_DuplicateGetChunk_DoesNotDuplicateEntry()
    {
        var (loader, _) = BuildSystem();

        loader.GetChunk(2, 3);
        loader.GetChunk(2, 3);
        loader.GetChunk(2, 3);

        var coords = loader.GetLoadedChunkCoords().ToList();

        Assert.Single(coords);
        Assert.Equal((2, 3), coords[0]);
    }

    [Fact]
    public void GetLoadedChunkCoords_RoundTripsNegativeChunkZ()
    {
        var (loader, _) = BuildSystem();

        loader.GetChunk(0, -1);

        var coords = loader.GetLoadedChunkCoords().ToList();
        Assert.Single(coords);
        // Z must round-trip; the key stores (uint)chunkZ so casting back must
        // reproduce the original signed value.
        Assert.Equal((0, -1), coords[0]);
    }

    // ── 8. Tick is a no-op — does not throw ──────────────────────────────────

    [Fact]
    public void Tick_DoesNotThrow()
    {
        var (loader, _) = BuildSystem();

        var ex = Record.Exception(() => loader.Tick());

        Assert.Null(ex);
    }

    [Fact]
    public void Tick_DoesNotAffectLoadedChunks()
    {
        var (loader, _) = BuildSystem();
        loader.GetChunk(0, 0);
        loader.GetChunk(1, 1);

        loader.Tick();

        Assert.True(loader.IsChunkLoaded(0, 0));
        Assert.True(loader.IsChunkLoaded(1, 1));
        Assert.Equal(2, loader.GetLoadedChunkCoords().Count());
    }

    // ── 9. SetWorld called multiple times — last value is used (no explosion) ─

    [Fact]
    public void SetWorld_CalledTwice_DoesNotThrow()
    {
        var loader  = new DebugChunkLoader();
        var world1  = new FakeWorld(loader);
        var world2  = new FakeWorld(loader);

        loader.SetWorld(world1);
        var ex = Record.Exception(() => loader.SetWorld(world2));

        Assert.Null(ex);
    }

    // ── 10. Large coordinate stress — key space does not collide ─────────────

    [Fact]
    public void GetChunk_ManyDistinctCoords_AllStoredSeparately()
    {
        var (loader, _) = BuildSystem();

        const int range = 10;
        int expectedCount = (2 * range + 1) * (2 * range + 1);

        for (int cx = -range; cx <= range; cx++)
        for (int cz = -range; cz <= range; cz++)
            loader.GetChunk(cx, cz);

        Assert.Equal(expectedCount, loader.GetLoadedChunkCoords().Count());
    }

    // ── 11. Chunk returned by GetChunk is the same world reference ────────────

    [Fact]
    public void GetChunk_ChunkWorldProperty_MatchesSetWorld()
    {
        var (loader, world) = BuildSystem();

        Chunk chunk = loader.GetChunk(0, 0);

        Assert.Same(world, chunk.World);
    }
}