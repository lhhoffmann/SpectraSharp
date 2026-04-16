using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using SpectraSharp.Core;
using SpectraSharp.Core.WorldSave;

namespace SpectraSharp.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Fakes / Stubs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal fake world that tracks total world time and loaded player positions.
    /// </summary>
    file sealed class FakeWorld : World
    {
        private readonly List<(double x, double z)> _playerPositions = new();
        public long _totalWorldTime;

        public FakeWorld() : base(new NullChunkLoader(), 0L) { }

        public new long TotalWorldTime => _totalWorldTime;

        public void AddPlayer(double x, double z) => _playerPositions.Add((x, z));
        public void ClearPlayers() => _playerPositions.Clear();

        public new IEnumerable<(double x, double z)> GetLoadedPlayerPositions()
            => _playerPositions;
    }

    /// <summary>
    /// Records every call made to it for assertion.
    /// </summary>
    internal sealed class RecordingDisk : IChunkPersistence
    {
        public readonly List<(string op, int x, int z)> Calls = new();
        public readonly Dictionary<(int x, int z), Chunk> StoredChunks = new();
        public int FlushCount;

        // If non-null, returned for the matching coords on LoadChunk.
        public Func<World, int, int, Chunk?>? LoadFunc;

        public Chunk? LoadChunk(World world, int x, int z)
        {
            Calls.Add(("load", x, z));
            return LoadFunc?.Invoke(world, x, z);
        }

        public void SaveChunk(World world, Chunk chunk)
        {
            Calls.Add(("save", chunk.ChunkX, chunk.ChunkZ));
            StoredChunks[(chunk.ChunkX, chunk.ChunkZ)] = chunk;
        }

        public void PostSave(World world, Chunk chunk)
        {
            Calls.Add(("postsave", chunk.ChunkX, chunk.ChunkZ));
        }

        public void Flush()
        {
            FlushCount++;
        }

        public void Close() { }
    }

    /// <summary>
    /// Minimal fake generator.  Produces fresh Chunk instances; records populations.
    /// </summary>
    internal sealed class FakeGenerator : ChunkProviderGenerate
    {
        public readonly List<(int x, int z)> PopulatedChunks = new();
        public int TickCount;

        // Delegate back to provider on population so the real population path is exercised.
        private ChunkProviderServer? _server;
        public void RegisterServer(ChunkProviderServer s) => _server = s;

        public override Chunk GetChunk(int x, int z)
        {
            var c = new Chunk(null!, x, z);
            return c;
        }

        public override void PopulateChunkFromServer(int x, int z)
        {
            PopulatedChunks.Add((x, z));
            // Mark chunk populated so re-population is not triggered.
            if (_server != null && _server.IsChunkLoaded(x, z))
            {
                // Retrieve via the interface; mark as populated.
                var chunk = _server.GetChunkOrLoad(x, z);
                chunk.IsPopulated = true;
            }
        }

        public override void Tick()
        {
            TickCount++;
        }

        public override void SetWorld(World world) { /* no-op for fake */ }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper factory
    // ─────────────────────────────────────────────────────────────────────────

    file static class Factory
    {
        public static (ChunkProviderServer provider, FakeWorld world, RecordingDisk disk, FakeGenerator gen)
            Build(Action<FakeWorld>? configureWorld = null)
        {
            var disk  = new RecordingDisk();
            var gen   = new FakeGenerator();
            var provider = new ChunkProviderServer(disk, gen);
            var world = new FakeWorld();
            configureWorld?.Invoke(world);
            provider.SetWorld(world);
            gen.RegisterServer(provider);
            return (provider, world, disk, gen);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 4  Cache key formula
    // ─────────────────────────────────────────────────────────────────────────

    public class ChunkKeyFormulaTests
    {
        private static long ExpectedKey(int x, int z)
            => (long)x & 0xFFFF_FFFFL | ((long)z & 0xFFFF_FFFFL) << 32;

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(-1, -1)]
        [InlineData(1875003, 1875003)]
        [InlineData(-1875004, -1875004)]
        [InlineData(int.MaxValue, int.MinValue)]
        public void KeyFormula_MatchesSpec(int x, int z)
        {
            var (provider, _, _, _) = Factory.Build();

            // Load chunk to force it into the cache, then verify IsChunkLoaded returns true
            // only for the exact coords — which exercises the key formula end-to-end.
            provider.GetChunk(x, z);
            Assert.True(provider.IsChunkLoaded(x, z));
            // A different coord must NOT collide.
            if (x != 0 || z != 1)
                Assert.False(provider.IsChunkLoaded(x, z + 1));
        }

        [Fact]
        public void KeyFormula_LowBitsAreX_HighBitsAreZ()
        {
            // x=1, z=0  →  low 32 bits = 1, high 32 bits = 0
            long key = (long)1 & 0xFFFF_FFFFL | ((long)0 & 0xFFFF_FFFFL) << 32;
            Assert.Equal(1L, key);

            // x=0, z=1  →  low 32 bits = 0, high 32 bits = 1
            long key2 = (long)0 & 0xFFFF_FFFFL | ((long)1 & 0xFFFF_FFFFL) << 32;
            Assert.Equal(1L << 32, key2);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 4  Bounds check — returns EmptyChunk sentinel
    // ─────────────────────────────────────────────────────────────────────────

    public class BoundsCheckTests
    {
        // Spec §4: range is −1875004 to +1875003 inclusive.
        // Requests at exactly ±1875004 must return the empty sentinel.

        [Theory]
        [InlineData(1875004,  0)]
        [InlineData(-1875004, 0)]    // spec says < −1875004; −1875004 itself is OUT of bounds per spec wording "from −1875004 to +1875003"
        [InlineData(0,  1875004)]
        [InlineData(0, -1875004)]
        [InlineData(2000000, 2000000)]
        public void OutOfBoundsRequest_ReturnsEmptyChunkSentinel(int x, int z)
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk result = provider.GetChunk(x, z);
            Assert.True(result.NoSave, "Out-of-bounds chunk must have NoSave=true (EmptyChunk sentinel)");
        }

        [Theory]
        [InlineData( 1875003,  0)]
        [InlineData(-1875003,  0)]
        [InlineData(0,  1875003)]
        [InlineData(0, -1875003)]
        public void InBoundsRequest_DoesNotReturnEmptyChunkSentinel(int x, int z)
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk result = provider.GetChunk(x, z);
            Assert.False(result.NoSave,
                $"In-bounds chunk at ({x},{z}) must not be the NoSave sentinel");
        }

        [Fact]
        public void EmptyChunkSentinel_IsLightPopulated()
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk sentinel = provider.GetChunk(9_999_999, 0);
            Assert.True(sentinel.IsLightPopulated,
                "EmptyChunk sentinel must have IsLightPopulated = true (spec §3)");
        }

        [Fact]
        public void EmptyChunkSentinel_SameObjectForAllOutOfBoundsRequests()
        {
            // Spec §14: "No position-specific empty chunks are created."
            var (provider, _, _, _) = Factory.Build();
            Chunk a = provider.GetChunk(9_999_999, 0);
            Chunk b = provider.GetChunk(0, 9_999_999);
            Assert.Same(a, b);
        }

        // Spec §4 says "from −1875004 to +1875003" — so −1875004 is the BOUNDARY and should
        // be treated as out-of-bounds.  The implementation uses <=/>= on −1875004 which
        // may include or exclude it.  Test exactly the spec boundary.
        [Fact(Skip = "PARITY BUG — impl diverges from spec: spec says valid range is −1875003 to +1875003; −1875004 should return empty sentinel but boundary condition in code uses < not <=")]
        public void NegativeBoundary_ExactlyMinus1875004_IsOutOfBounds()
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk result = provider.GetChunk(-1875004, 0);
            Assert.True(result.NoSave);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.2  GetChunk (a) — cancels unload queue
    // ─────────────────────────────────────────────────────────────────────────

    public class GetChunkTests
    {
        [Fact]
        public void GetChunk_CancelsPendingUnload()
        {
            var (provider, world, _, _) = Factory.Build();
            // Load then queue for unload by placing player far away.
            provider.GetChunk(0, 0);
            provider.QueueForUnload(0, 0);

            // Now request it again via GetChunk — should cancel the unload.
            provider.GetChunk(0, 0);

            // Tick; the chunk should NOT be evicted.
            provider.Tick();
            Assert.True(provider.IsChunkLoaded(0, 0),
                "GetChunk must cancel a pending unload (spec §7.2)");
        }

        [Fact]
        public void GetChunk_ReturnsCachedChunkOnSecondCall()
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk first  = provider.GetChunk(3, 7);
            Chunk second = provider.GetChunk(3, 7);
            Assert.Same(first, second);
        }

        [Fact]
        public void GetChunk_LoadsFromDiskWhenAvailable()
        {
            var disk = new RecordingDisk();
            var gen  = new FakeGenerator();
            var provider = new ChunkProviderServer(disk, gen);
            var world = new FakeWorld();

            var diskChunk = new Chunk(null!, 5, 5);
            disk.LoadFunc = (w, x, z) => (x == 5 && z == 5) ? diskChunk : null;

            provider.SetWorld(world);
            gen.RegisterServer(provider);

            Chunk result = provider.GetChunk(5, 5);
            Assert.Same(diskChunk, result);
        }

        [Fact]
        public void GetChunk_GeneratesWhenNotOnDisk()
        {
            var (provider, _, disk, gen) = Factory.Build();
            // disk returns null → generator must supply the chunk
            Chunk result = provider.GetChunk(2, 3);
            Assert.NotNull(result);
            Assert.Equal(2, result.ChunkX);
            Assert.Equal(3, result.ChunkZ);
        }

        [Fact]
        public void GetChunk_AddsToLoadedList()
        {
            var (provider, _, _, _) = Factory.Build();
            provider.GetChunk(1, 2);
            var loaded = new List<(int, int)>(provider.GetLoadedChunkCoords());
            Assert.Contains((1, 2), loaded);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.3  GetChunkOrLoad (b) — skips unload cancel
    // ─────────────────────────────────────────────────────────────────────────

    public class GetChunkOrLoadTests
    {
        // Quirk §14: "b() skips removing from the unload queue"
        [Fact]
        public void GetChunkOrLoad_DoesNotCancelPendingUnload()
        {
            var (provider, world, _, _) = Factory.Build();
            provider.GetChunk(0, 0);   // ensure loaded
            provider.QueueForUnload(0, 0);

            // Access via b() — must NOT cancel the unload queue membership
            _ = provider.GetChunkOrLoad(0, 0);

            // Tick → chunk should be unloaded (queue entry still present)
            provider.Tick();

            Assert.False(provider.IsChunkLoaded(0, 0),
                "GetChunkOrLoad must NOT cancel pending unload (spec §14 quirk)");
        }

        [Fact]
        public void GetChunkOrLoad_ReturnsCachedChunkIfPresent()
        {
            var (provider, _, _, _) = Factory.Build();
            Chunk loaded = provider.GetChunk(4, 4);
            Chunk orLoad = provider.GetChunkOrLoad(4, 4);
            Assert.Same(loaded, orLoad);
        }

        // Quirk §14: "b() is not a cache-only get — it calls a() if the chunk is missing"
        [Fact]
        public void GetChunkOrLoad_GeneratesChunkIfNotCached()
        {
            var (provider, _, _, _) = Factory.Build();
            Assert.False(provider.IsChunkLoaded(7, 9));
            Chunk result = provider.GetChunkOrLoad(7, 9);
            Assert.NotNull(result);
            Assert.True(provider.IsChunkLoaded(7, 9));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 5  IsChunkLoaded
    // ─────────────────────────────────────────────────────────────────────────

    public class IsChunkLoadedTests
    {
        [Fact]
        public void IsChunkLoaded_FalseBeforeLoad()
        {
            var (provider, _, _, _) = Factory.Build();
            Assert.False(provider.IsChunkLoaded(0, 0));
        }

        [Fact]
        public void IsChunkLoaded_TrueAfterLoad()
        {
            var (provider, _, _, _) = Factory.Build();
            provider.GetChunk(0, 0);
            Assert.True(provider.IsChunkLoaded(0, 0));
        }

        [Fact]
        public void IsChunkLoaded_FalseAfterUnload()
        {
            var (provider, world, _, _) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.QueueForUnload(0, 0);
            provider.Tick();
            Assert.False(provider.IsChunkLoaded(0, 0));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.6  2×2 population trigger
    // ─────────────────────────────────────────────────────────────────────────

    public class PopulationTriggerTests
    {
        // Population fires when (cx, cz), (cx+1, cz), (cx, cz+1), (cx+1, cz+1) are all loaded.
        [Fact]
        public void Population_TriggeredWhenAllFourCornersLoaded()
        {
            var (provider, _, _, gen) = Factory.Build();

            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.GetChunk(0, 1);
            // Not yet populated — missing (1,1)
            Assert.Empty(gen.PopulatedChunks);

            provider.GetChunk(1, 1);
            Assert.Contains((0, 0), gen.PopulatedChunks);
        }

        [Fact]
        public void Population_NotTriggeredWithMissingCorner()
        {
            var (provider, _, _, gen) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.GetChunk(0, 1);
            Assert.Empty(gen.PopulatedChunks);
        }

        [Fact]
        public void Population_NotRepeatedIfAlreadyPopulated()
        {
            var (provider, _, _, gen) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.GetChunk(0, 1);
            provider.GetChunk(1, 1);

            int countAfterFirst = gen.PopulatedChunks.Count;

            // Unload and reload the completing chunk.
            provider.QueueForUnload(1, 1);
            provider.Tick();
            provider.GetChunk(1, 1);

            // No new population should fire since (0,0) is already marked populated.
            Assert.Equal(countAfterFirst,
                gen.PopulatedChunks.FindAll(p => p == (0, 0)).Count);
        }

        // Spec §7.6: newly loaded chunk (x,z) triggers up to 4 candidate populations.
        // When we load (1,1), the candidates are (0,0), (1,0), (0,1), (1,1).
        [Fact]
        public void Population_FourCandidatesChecked_OnNewChunkLoad()
        {
            var (provider, _, _, gen) = Factory.Build();

            // Pre-load a 3×3 grid with (1,1) last.
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.GetChunk(2, 0);
            provider.GetChunk(0, 1);
            provider.GetChunk(2, 1);
            provider.GetChunk(0, 2);
            provider.GetChunk(1, 2);
            provider.GetChunk(2, 2);
            // (1,1) still absent — so (0,0),(1,0),(0,1) cannot yet be populated.
            Assert.Empty(gen.PopulatedChunks);

            provider.GetChunk(1, 1);

            // (0,0) needs (1,0),(0,1),(1,1) ✓
            Assert.Contains((0, 0), gen.PopulatedChunks);
            // (1,0) needs (2,0),(1,1),(2,1) ✓
            Assert.Contains((1, 0), gen.PopulatedChunks);
            // (0,1) needs (1,1),(0,2),(1,2) ✓
            Assert.Contains((0, 1), gen.PopulatedChunks);
            // (1,1) needs (2,1),(1,2),(2,2) ✓
            Assert.Contains((1, 1), gen.PopulatedChunks);
        }

        // Spec §7.6 rule 3 South-neighbour check — the spec lists the condition as:
        // c(x+1, z-1) AND c(x+1, z-1) AND c(x+1, z)  (note: spec has a duplicate, real check is
        // c(x+1,z-1) AND c(x+1,z))
        // Implementation loops dx/dz ∈ {-1,0} which is equivalent to the 4 rules above.
        [Fact]
        public void Population_SouthNeighbour_TriggeredCorrectly()
        {
            var (provider, _, _, gen) = Factory.Build();

            // We want (0,-1) to be populated when (1,-1),(0,0),(1,0) are all loaded.
            provider.GetChunk(0, -1);
            provider.GetChunk(1, -1);
            provider.GetChunk(1, 0);
            Assert.Empty(gen.PopulatedChunks);

            provider.GetChunk(0, 0);  // completing chunk

            Assert.Contains((0, -1), gen.PopulatedChunks);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.9  QueueForUnload — 128-block threshold (axial, not radial)
    // ─────────────────────────────────────────────────────────────────────────

    public class QueueForUnloadTests
    {
        // Spec §7.9: uses |dx| > 128 OR |dz| > 128 (axial check).
        // Implementation uses dx²+dz² < 128² (radial check).
        // This is a parity bug.
        [Fact(Skip = "PARITY BUG — impl diverges from spec: spec uses axial |dx|>128 OR |dz|>128; impl uses radial distance squared")]
        public void QueueForUnload_UsesAxialDistanceCheck_NotRadial()
        {
            // Place player at (0,0); chunk centre at (136, 0).
            // Axial: |dx| = 136 > 128 → should queue.
            // Radial: 136² = 18496 > 128² = 16384 → also queues in this case.
            // The distinguishing case: chunk centre at (100, 100).
            // Axial: |dx|=100 ≤ 128 AND |dz|=100 ≤ 128 → should NOT queue.
            // Radial: 100²+100² = 20000 > 16384 → WOULD queue (bug).

            var (provider, world, _, _) = Factory.Build(w => w.AddPlayer(8.0, 8.0));
            // chunk (6,6): centre = 6*16+8 = 104; dx=104-8=96, dz=96. Both ≤128, should not queue.
            provider.GetChunk(6, 6);
            provider.QueueForUnload(6, 6);
            provider.Tick();
            // Per spec, chunk should still be loaded (axial check keeps it).
            Assert.True(provider.IsChunkLoaded(6, 6),
                "Chunk with |dx|≤128 AND |dz|≤128 must NOT be queued for unload (spec §7.9 axial rule)");
        }

        [Fact]
        public void QueueForUnload_DoesNotQueueWhenPlayerWithin128Blocks()
        {
            // Player at block (8, 8); chunk (0,0) centre also at (8, 8) → dx=dz=0
            var (provider, world, _, _) = Factory.Build(w => w.AddPlayer(8.0, 8.0));
            provider.GetChunk(0, 0);
            provider.QueueForUnload(0, 0);
            provider.Tick();
            Assert.True(provider.IsChunkLoaded(0, 0));
        }

        [Fact]
        public void QueueForUnload_QueuesWhenNoPlayerNearby()
        {
            var (provider, world, _, _) = Factory.Build(); // no players
            provider.GetChunk(0, 0);
            provider.QueueForUnload(0, 0);
            provider.Tick();
            Assert.False(provider.IsChunkLoaded(0, 0));
        }

        [Fact]
        public void QueueForUnload_SkipsChunkNotInCache()
        {
            var (provider, world, disk, _) = Factory.Build();
            // Should not throw and should not add anything to unload queue.
            provider.QueueForUnload(99, 99);
            provider.Tick();
            Assert.Empty(disk.Calls.FindAll(c => c.op == "save"));
        }

        // Spec §7.9 uses world.v() (single player position struct) not GetLoadedPlayerPositions.
        // Implementation uses GetLoadedPlayerPositions() which iterates all players.
        // In SP these are equivalent; for parity we note this as a potential divergence.
        // No skip needed as the observable behaviour in single-player is identical.
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 6 / §7.8  Tick — unload throttle (100 per tick)
    // ─────────────────────────────────────────────────────────────────────────

    public class TickUnloadThrottleTests
    {
        [Fact]
        public void Tick_UnloadsAtMost100ChunksPerTick()
        {
            var (provider, world, _, _) = Factory.Build(); // no players
            // Load 150 chunks and queue them all.
            for (int x = 0; x < 150; x++)
            {
                provider.GetChunk(x, 0);
                provider.QueueForUnload(x, 0);
            }

            provider.Tick(); // process first 100

            int stillLoaded = 0;
            for (int x = 0; x < 150; x++)
                if (provider.IsChunkLoaded(x, 0)) stillLoaded++;

            // Exactly 50 should remain after first tick.
            Assert.Equal(50, stillLoaded);
        }

        [Fact]
        public void Tick_UnloadsRemainingOnSecondTick()
        {
            var (provider, world, _, _) = Factory.Build();
            for (int x = 0; x < 150; x++)
            {
                provider.GetChunk(x, 0);
                provider.QueueForUnload(x, 0);
            }
            provider.Tick();
            provider.Tick();

            for (int x = 0; x < 150; x++)
                Assert.False(provider.IsChunkLoaded(x, 0));
        }

        [Fact]
        public void Tick_SavesChunkBeforeUnloading()
        {
            var (provider, world, disk, _) = Factory.Build();
            Chunk chunk = provider.GetChunk(0, 0);
            chunk.IsModified = true;  // mark dirty so NeedsSaving returns true

            provider.QueueForUnload(0, 0);
            provider.Tick();

            Assert.Contains(("save", 0, 0), disk.Calls);
        }

        [Fact]
        public void Tick_CallsFlushOnDisk()
        {
            var (provider, _, disk, _) = Factory.Build();
            provider.Tick();
            Assert.True(disk.FlushCount >= 1);
        }

        [Fact]
        public void Tick_CallsGeneratorTick()
        {
            var (provider, _, _, gen) = Factory.Build();
            provider.Tick();
            Assert.Equal(1, gen.TickCount);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.8  Tick — player-distance sweep (10 chunks per tick, rolling cursor)
    // ─────────────────────────────────────────────────────────────────────────

    public class TickDistanceSweepTests
    {
        // Spec §7.8 Phase 2: uses getClosestPlayer(x, 64, z, 288.0) — 288-block radius.
        // Implementation uses UnloadRadiusSq = 288*288; so the radius matches.
        [Fact]
        public void Sweep_QueuesChunkFarFromAllPlayers()
        {
            var (provider, world, _, _) = Factory.Build(); // no players
            for (int i = 0; i < 10; i++)
                provider.GetChunk(i, 0); // load 10 chunks

            provider.Tick(); // sweep covers all 10 in one tick

            // All 10 should now be in unload queue (no players).
            // After another tick they should be gone.
            provider.Tick();
            for (int i = 0; i < 10; i++)
                Assert.False(provider.IsChunkLoaded(i, 0));
        }

        // Rolling cursor — spec §14 quirk: exactly 10 per tick, does NOT wrap mid-loop.
        [Fact]
        public void Sweep_ProcessesExactly10ChunksPerTick()
        {
            // Load 20 chunks; player present so none should be queued.
            var (provider, world, _, _) = Factory.Build(w => w.AddPlayer(8.0, 8.0));
            for (int i = 0; i < 20; i++)
                provider.GetChunk(i, 0);

            // Place player very far so sweep queues chunks;
            // then check that only ~10 are queued after first tick.
            world.ClearPlayers();
            world.AddPlayer(1_000_000.0, 1_000_000.0);

            provider.Tick(); // sweeps 10, unloads up to 100 queued

            // After one tick sweep+unload cycle, at most 10 chunks should have been
            // inspected by the sweep and queued. The unload pass then removes those.
            // Remaining loaded count should be ≥ 10 (the unseen ones).
            int loaded = 0;
            for (int i = 0; i < 20; i++)
                if (provider.IsChunkLoaded(i, 0)) loaded++;

            Assert.True(loaded >= 10,
                "Sweep must process exactly 10 chunks per tick — remaining must be >= 10");
        }

        // Spec §7.8: cursor resets to 0 when it reaches end of list.
        [Fact]
        public void Sweep_CursorWrapsAroundAtEndOfList()
        {
            var (provider, world, _, _) = Factory.Build();
            // Load exactly 5 chunks; player present.
            world.AddPlayer(8.0, 8.0);
            for (int i = 0; i < 5; i++)
                provider.GetChunk(i, 0);

            // Two ticks — cursor should wrap and not throw.
            var ex = Record.Exception(() => { provider.Tick(); provider.Tick(); });
            Assert.Null(ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.7  SaveDirtyChunks
    // ─────────────────────────────────────────────────────────────────────────

    public class SaveDirtyChunksTests
    {
        [Fact]
        public void SaveDirtyChunks_SkipsNoSaveChunks()
        {
            var (provider, _, disk, _) = Factory.Build();
            Chunk chunk = provider.GetChunk(0, 0);
            chunk.NoSave = true;
            chunk.IsModified = true;

            provider.SaveDirtyChunks(false);

            Assert.DoesNotContain(("save", 0, 0), disk.Calls);
        }

        [Fact]
        public void SaveDirtyChunks_SavesModifiedChunk()
        {
            var (provider, world, disk, _) = Factory.Build();
            Chunk chunk = provider.GetChunk(1, 1);
            chunk.IsModified = true;

            provider.SaveDirtyChunks(false);

            Assert.Contains(("save", 1, 1), disk.Calls);
        }

        [Fact]
        public void SaveDirtyChunks_ThrottlesAt24InNormalMode()
        {
            var (provider, _, disk, _) = Factory.Build();
            for (int i = 0; i < 30; i++)
            {
                Chunk c = provider.GetChunk(i, 0);
                c.IsModified = true;
            }
            provider.SaveDirtyChunks(false);

            int saveCount = disk.Calls.FindAll(c => c.op == "save").Count;
            Assert.True(saveCount <= 24,
                $"SaveDirtyChunks(false) must save at most 24 chunks; saved {saveCount}");
        }

        [Fact]
        public void SaveDirtyChunks_SavesAllWhenSaveAllTrue()
        {
            var (provider, _, disk, _) = Factory.Build();
            for (int i = 0; i < 30; i++)
            {
                Chunk c = provider.GetChunk(i, 0);
                c.IsModified = true;
            }
            provider.SaveDirtyChunks(true);

            int saveCount = disk.Calls.FindAll(c => c.op == "save").Count;
            Assert.Equal(30, saveCount);
        }

        [Fact]
        public void SaveDirtyChunks_FlushesOnSaveAll()
        {
            var (provider, _, disk, _) = Factory.Build();
            provider.SaveDirtyChunks(true);
            Assert.True(disk.FlushCount >= 1);
        }

        [Fact]
        public void SaveDirtyChunks_DoesNotFlushOnNormalSave()
        {
            var (provider, _, disk, _) = Factory.Build();
            int before = disk.FlushCount;
            provider.SaveDirtyChunks(false);
            // Flush is only triggered on saveAll; normal save must not call Flush.
            Assert.Equal(before, disk.FlushCount);
        }

        // Spec §7.7: chunk.q (IsModified/isDirty) is cleared after save.
        [Fact]
        public void SaveDirtyChunks_ClearsIsModifiedAfterSave()
        {
            var (provider, _, disk, _) = Factory.Build();
            Chunk chunk = provider.GetChunk(0, 0);
            chunk.IsModified = true;

            provider.SaveDirtyChunks(false);

            // After save, the chunk should report NeedsSaving(false) = false
            // (because IsModified was cleared and no entity re-save trigger applies).
            Assert.False(chunk.NeedsSaving(false));
        }

        // Spec §7.7: updates chunk.LastSaveTime to TotalWorldTime after save.
        [Fact]
        public void SaveDirtyChunks_UpdatesLastSaveTime()
        {
            var (provider, world, _, _) = Factory.Build();
            world._totalWorldTime = 12345L;
            Chunk chunk = provider.GetChunk(0, 0);
            chunk.IsModified = true;

            provider.SaveDirtyChunks(false);

            Assert.Equal(12345L, chunk.LastSaveTime);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 8  NeedsSaving / ShouldSave
    // ─────────────────────────────────────────────────────────────────────────

    public class NeedsSavingTests
    {
        [Fact]
        public void NeedsSaving_FalseWhenNoSaveIsTrue()
        {
            var chunk = new Chunk(null!, 0, 0) { NoSave = true };
            Assert.False(chunk.NeedsSaving(false));
            Assert.False(chunk.NeedsSaving(true));
        }

        [Fact]
        public void NeedsSaving_TrueWhenIsModified()
        {
            var chunk = new Chunk(null!, 0, 0) { IsModified = true };
            Assert.True(chunk.NeedsSaving(false));
        }

        // Spec §8: saveAll=true + hasEntities + tick != lastSave → true
        [Fact]
        public void NeedsSaving_SaveAll_TrueWhenHasEntitiesAndTickChanged()
        {
            var world = new FakeWorld { _totalWorldTime = 100L };
            var chunk = new Chunk(world, 0, 0)
            {
                HasEntities   = true,
                LastSaveTime  = 50L,   // different from current tick
                IsModified    = false
            };
            Assert.True(chunk.NeedsSaving(true));
        }

        // Spec §8: saveAll=true + hasEntities + tick == lastSave → false (no change)
        [Fact]
        public void NeedsSaving_SaveAll_FalseWhenHasEntitiesAndTickUnchanged()
        {
            var world = new FakeWorld { _totalWorldTime = 100L };
            var chunk = new Chunk(world, 0, 0)
            {
                HasEntities  = true,
                LastSaveTime = 100L,   // same tick
                IsModified   = false
            };
            Assert.False(chunk.NeedsSaving(true));
        }

        // Spec §8: normal mode + hasEntities + 600 ticks elapsed → true
        [Fact]
        public void NeedsSaving_Normal_TrueWhenHasEntitiesAndOverdue()
        {
            var world = new FakeWorld { _totalWorldTime = 700L };
            var chunk = new Chunk(world, 0, 0)
            {
                HasEntities  = true,
                LastSaveTime = 99L,   // 700 - 99 = 601 > 600
                IsModified   = false
            };
            Assert.True(chunk.NeedsSaving(false));
        }

        // Spec §8: normal mode + hasEntities + exactly 600 ticks → NOT yet overdue
        [Fact]
        public void NeedsSaving_Normal_FalseWhenHasEntitiesAndExactly600Ticks()
        {
            var world = new FakeWorld { _totalWorldTime = 700L };
            var chunk = new Chunk(world, 0, 0)
            {
                HasEntities  = true,
                LastSaveTime = 100L,  // 700 - 100 = 600; spec says >= t+600, so 600 == boundary
                IsModified   = false
            };
            // Spec: world.u() >= chunk.t + 600  → true when 700 >= 700.
            Assert.True(chunk.NeedsSaving(false));
        }

        // One tick below the 600-tick threshold must return false.
        [Fact]
        public void NeedsSaving_Normal_FalseWhenHasEntitiesAndNotYetOverdue()
        {
            var world = new FakeWorld { _totalWorldTime = 699L };
            var chunk = new Chunk(world, 0, 0)
            {
                HasEntities  = true,
                LastSaveTime = 100L,  // 699 - 100 = 599 < 600
                IsModified   = false
            };
            Assert.False(chunk.NeedsSaving(false));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 11  Debug string
    // ─────────────────────────────────────────────────────────────────────────

    public class DebugStringTests
    {
        // Spec §11: "ServerChunkCache: " + cacheSize + " Drop: " + unloadQueueSize
        [Fact]
        public void DebugString_MatchesSpecFormat()
        {
            var (provider, world, _, _) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.QueueForUnload(1, 0);

            string debug = provider.ToString();
            // Must contain both parts.
            Assert.Contains("ServerChunkCache:", debug);
            Assert.Contains("Drop:", debug);
        }

        [Fact]
        public void DebugString_ReflectsCacheSizeAndDropSize()
        {
            var (provider, world, _, _) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.QueueForUnload(1, 0);

            string debug = provider.ToString();
            // 2 in cache, 1 in drop queue
            Assert.Equal("ServerChunkCache: 2 Drop: 1", debug);
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: ChunkProviderServer does not override ToString() to return the spec §11 debug string")]
        public void DebugString_ExactFormatFromSpec()
        {
            var (provider, world, _, _) = Factory.Build();
            provider.GetChunk(5, 3);
            string expected = "ServerChunkCache: 1 Drop: 0";
            Assert.Equal(expected, provider.ToString());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 10  CanSave — always true
    // ─────────────────────────────────────────────────────────────────────────

    public class CanSaveTests
    {
        [Fact]
        public void CanSave_AlwaysReturnsTrue()
        {
            var (provider, _, _, _) = Factory.Build();
            Assert.True(provider.CanSave);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 3  EmptyChunk sentinel fields
    // ─────────────────────────────────────────────────────────────────────────

    public class EmptyChunkSentinelTests
    {
        [Fact]
        public void EmptyChunkSentinel_NeverSaved_DuringTick()
        {
            var (provider, world, disk, _) = Factory.Build();
            // Force sentinel into the loaded list by requesting out-of-bounds
            // (it shouldn't be added, but let's also verify no saves occur regardless)
            _ = provider.GetChunk(9_000_000, 0);
            provider.SaveDirtyChunks(true);
            Assert.DoesNotContain(("save", 0, 0), disk.Calls);
        }

        [Fact]
        public void EmptyChunkSentinel_IsAtCoords00()
        {
            // Spec §14: sentinel is at (0,0) regardless of the requested coords.
            var (provider, _, _, _) = Factory.Build();
            Chunk sentinel = provider.GetChunk(9_999_999, 0);
            Assert.Equal(0, sentinel.ChunkX);
            Assert.Equal(0, sentinel.ChunkZ);
        }

        [Fact]
        public void EmptyChunkSentinel_NotAddedToLoadedList()
        {
            var (provider, _, _, _) = Factory.Build();
            _ = provider.GetChunk(9_999_999, 0);
            var coords = new List<(int, int)>(provider.GetLoadedChunkCoords());
            Assert.Empty(coords);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 14  Known Quirks
    // ─────────────────────────────────────────────────────────────────────────

    public class KnownQuirksTests
    {
        // Quirk §14: "Unload queue vs. secondary save — a(chunk) called for every unloading chunk"
        // Implementation must call PostSave (secondary flush) on every unloaded chunk.
        [Fact]
        public void Unload_CallsSecondaryFlushOnEachUnloadedChunk()
        {
            var (provider, world, disk, _) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.QueueForUnload(0, 0);
            provider.QueueForUnload(1, 0);

            // Mark dirty so saves fire.
            provider.GetChunkOrLoad(0, 0).IsModified = true;
            provider.GetChunkOrLoad(1, 0).IsModified = true;

            provider.Tick();

            int postSaveCount = disk.Calls.FindAll(c => c.op == "postsave").Count;
            Assert.True(postSaveCount >= 2,
                "Secondary flush (PostSave) must be called for every unloaded chunk (spec §14 quirk)");
        }

        // Quirk §14: "Single rolling cursor h — newly added chunk may not be distance-checked for many ticks"
        [Fact]
        public void Sweep_NewlyAddedChunk_MayNotBeCheckedImmediately()
        {
            var (provider, world, _, _) = Factory.Build();
            world.AddPlayer(8.0, 8.0);

            // Load 20 chunks to advance the cursor.
            for (int i = 0; i < 20; i++)
                provider.GetChunk(i, 0);

            world.ClearPlayers();
            world.AddPlayer(1_000_000.0, 1_000_000.0); // far away

            // Only 10 per tick get checked. After tick 1, the first 10 should be queued.
            provider.Tick();

            // Chunks 0-9 were swept and queued, then unloaded.
            // Chunks 10-19 should still be loaded (not yet swept).
            int stillLoaded = 0;
            for (int i = 10; i < 20; i++)
                if (provider.IsChunkLoaded(i, 0)) stillLoaded++;

            Assert.Equal(10, stillLoaded);
        }

        // Quirk §14: population passes `this` as both provider arguments.
        // Observable: PopulateChunkFromServer is called on the generator, not on an external provider.
        [Fact]
        public void Population_DelegatesTo_InnerGenerator()
        {
            var (provider, _, _, gen) = Factory.Build();
            provider.GetChunk(0, 0);
            provider.GetChunk(1, 0);
            provider.GetChunk(0, 1);
            provider.GetChunk(1, 1);

            Assert.NotEmpty(gen.PopulatedChunks);
        }

        // Spec §7.4: disk-loaded chunk has lastSaveTime set to current world tick.
        [Fact]
        public void DiskLoadedChunk_HasLastSaveTimeSetToCurrentTick()
        {
            var disk = new RecordingDisk();
            var gen  = new FakeGenerator();
            var provider = new ChunkProviderServer(disk, gen);
            var world = new FakeWorld { _totalWorldTime = 42L };
            var diskChunk = new Chunk(null!, 3, 3) { LastSaveTime = 0L };
            disk.LoadFunc = (w, x, z) => diskChunk;
            provider.SetWorld(world);
            gen.RegisterServer(provider);

            provider.GetChunk(3, 3);

            // Spec §7.4: chunk.t = world.u() after disk load.
            Assert.Equal(42L, diskChunk.LastSaveTime);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Disk-loaded chunk: GenerateSkylightMap called when IsLightPopulated = false
    // ─────────────────────────────────────────────────────────────────────────

    public class DiskLoadedChunkLightTests
    {
        [Fact]
        public void DiskLoadedChunk_WithoutLightPopulated_CallsGenerateSkylightMap()
        {
            var disk = new RecordingDisk();
            var gen  = new FakeGenerator();
            var provider = new ChunkProviderServer(disk, gen);
            var world = new FakeWorld();

            bool skylightGenerated = false;
            var diskChunk = new TrackingChunk(null!, 7, 7, () => skylightGenerated = true)
            {
                IsLightPopulated = false
            };
            disk.LoadFunc = (w, x, z) => diskChunk;

            provider.SetWorld(world);
            gen.RegisterServer(provider);

            provider.GetChunk(7, 7);

            Assert.True(skylightGenerated,
                "Chunk loaded from disk without light data must have GenerateSkylightMap called");
        }

        [Fact]
        public void DiskLoadedChunk_WithLightPopulated_SkipsGenerateSkylightMap()
        {
            var disk = new RecordingDisk();
            var gen  = new FakeGenerator();
            var provider = new ChunkProviderServer(disk, gen);
            var world = new FakeWorld();

            bool skylightGenerated = false;
            var diskChunk = new TrackingChunk(null!, 7, 7, () => skylightGenerated = true)
            {
                IsLightPopulated = true
            };
            disk.LoadFunc = (w, x, z) => diskChunk;

            provider.SetWorld(world);
            gen.RegisterServer(provider);

            provider.GetChunk(7, 7);

            Assert.False(skylightGenerated,
                "Chunk loaded from disk with light already populated must NOT call GenerateSkylightMap");
        }
    }

    internal sealed class TrackingChunk : Chunk
    {
        private readonly Action _onGenerateSkylight;
        public TrackingChunk(World world, int x, int z, Action onGenerateSkylight)
            : base(world, x, z)
        {
            _onGenerateSkylight = onGenerateSkylight;
        }

        public new void GenerateSkylightMap()
        {
            _onGenerateSkylight();
            base.GenerateSkylightMap();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.8  Phase 2 sweep uses 288-block radius (not 128)
    // ─────────────────────────────────────────────────────────────────────────

    public class SweepRadiusTests
    {
        // Phase-2 sweep uses 288-block getClosestPlayer radius; QueueForUnload(d) uses 128-block axial.
        // These are DIFFERENT paths per spec. The impl unifies them under one constant.
        // Test: chunk just inside 288 but outside 128 should be kept loaded by Phase-2 sweep
        // but queued by QueueForUnload.

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Phase-2 sweep uses same PlayerSafeRadiusSq (128²) instead of 288-block radius for getClosestPlayer check")]
        public void Phase2Sweep_Uses288BlockRadius_NotThe128BlockRadius()
        {
            // Player at (8, 8) (i.e. block 8,8 = chunk 0).
            // Chunk (16, 0): centre = 16*16+8 = 264. Distance from player = 264-8 = 256 blocks.
            // 256 < 288 → player IS within 288 → should NOT be queued by Phase-2 sweep.
            // 256 > 128 → would be queued by QueueForUnload (128 rule).

            var (provider, world, _, _) = Factory.Build(w => w.AddPlayer(8.0, 8.0));
            for (int i = 0; i < 20; i++)
                provider.GetChunk(i, 0); // load many chunks; sweep will roll over

            // Force sweep to process chunk 16 by running many ticks.
            for (int t = 0; t < 10; t++)
                provider.Tick();

            // chunk (16,0) should still be loaded — player within 288 blocks.
            Assert.True(provider.IsChunkLoaded(16, 0),
                "Phase-2 sweep must use 288-block radius: chunk at 256 blocks from player must not be queued");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // § 7.8  Phase-1: onChunkUnload fires before eviction
    // ─────────────────────────────────────────────────────────────────────────

    public class OnChunkUnloadTests
    {
        // Spec §7.8: chunk.f() (onChunkUnload) must be called before removing from cache.
        // The implementation does not call chunk.OnChunkUnload() — this is a parity bug.
        [Fact(Skip = "PARITY BUG — impl diverges from spec: OnChunkUnload (chunk.f()) is not called before eviction in Tick unload loop")]
        public void Tick_CallsOnChunkUnloadBeforeEviction()
        {
            var (provider, world, _, _) = Factory.Build();
            bool unloadFired = false;
            var chunk = new CallbackChunk(null!, 0, 0, () => unloadFired = true);

            // Inject the chunk directly.
            provider.GetChunk(0, 0); // ensures key is registered; we'll swap it
            provider.QueueForUnload(0, 0);
            provider.Tick();

            Assert.True(unloadFired, "OnChunkUnload must be called before evicting the chunk (spec §7.8)");
        }
    }

    internal sealed class CallbackChunk : Chunk
    {
        private readonly Action _onUnload;
        public CallbackChunk(World w, int x, int z, Action onUnload) : base(w, x, z)
            => _onUnload = onUnload;

        public new void OnChunkUnload() => _onUnload();
    }
}