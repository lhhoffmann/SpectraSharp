using SpectraEngine.Core.WorldSave;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>jz</c> (ChunkProviderServer) — the live-world chunk manager.
/// Wraps a <see cref="DiskChunkLoader"/> and a <see cref="ChunkProviderGenerate"/> and
/// implements <see cref="IChunkLoader"/> so <see cref="World"/> needs no direct reference
/// to either backing system.
///
/// Cache key formula (spec §4): <c>(long)x &amp; 0xFFFF_FFFF | ((long)z &amp; 0xFFFF_FFFF) &lt;&lt; 32</c>
///
/// Population rule (spec §5): a chunk is populated only when it AND its (+1,0), (0,+1),
/// (+1,+1) neighbours are all in the cache. The trigger is fired from <see cref="LoadOrCreateChunk"/>
/// for each of the four possible "bottom-left" positions.
///
/// Quirks preserved:
///   1. <see cref="GetChunkOrLoad"/> skips the unload-cancel step (vanilla <c>b</c> method).
///   2. EmptyChunk sentinel has <see cref="Chunk.NoSave"/> = true; never saved or unloaded.
///   3. ShouldSave: NoSave → false; saveAll+hasEntities → true; entities+600 ticks → true;
///      IsModified → true.  (Matches <see cref="Chunk.NeedsSaving"/>.)
///   4. QueueForUnload skips chunks within 128 blocks of any player (128² = 16384).
///   5. Tick unloads up to 100 chunks per tick; distance sweep checks 10 chunks per tick
///      (rolling cursor wraps around loaded list).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderServer_Spec.md
/// </summary>
public sealed class ChunkProviderServer : IChunkLoader
{
    // ── Bounds ────────────────────────────────────────────────────────────────

    private const int ChunkBoundRadius = 1875004; // ±1875004 → EmptyChunk

    // ── Unload / save throttle constants ─────────────────────────────────────

    private const int MaxUnloadsPerTick = 100;   // spec §6.4
    private const int MaxSavesPerTick   = 24;    // spec §6.3
    private const int UnloadRadiusSq    = 288 * 288; // 288-block radius squared
    private const int PlayerSafeRadiusSq = 128 * 128; // 128-block safe zone squared

    // ── Fields (spec §3) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — set of chunk keys queued for unloading.</summary>
    private readonly HashSet<long> _unloadQueue = [];

    /// <summary>obf: <c>b</c> — shared empty-chunk sentinel returned for out-of-bounds coords.</summary>
    private Chunk _emptyChunk;

    /// <summary>obf: <c>c</c> — procedural terrain generator.</summary>
    private readonly ChunkProviderGenerate _generator;

    /// <summary>obf: <c>d</c> — disk I/O layer.</summary>
    private readonly IChunkPersistence _disk;

    /// <summary>obf: <c>e</c> — LongHashMap equivalent (key → Chunk).</summary>
    private readonly Dictionary<long, Chunk> _cache = [];

    /// <summary>obf: <c>f</c> — iteration list for save/unload sweeps.</summary>
    private readonly List<Chunk> _loadedList = [];

    /// <summary>obf: <c>g</c> — the owning world. Set via <see cref="SetWorld"/> after construction.</summary>
    private World? _world;

    /// <summary>obf: <c>h</c> — rolling cursor for the 10-chunk-per-tick distance sweep.</summary>
    private int _sweepCursor;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Construct the server before <see cref="World"/> exists.
    /// Call <see cref="SetWorld"/> immediately after constructing <see cref="World"/>.
    /// </summary>
    public ChunkProviderServer(IChunkPersistence disk, ChunkProviderGenerate generator)
    {
        _disk      = disk;
        _generator = generator;

        // EmptyChunk: world reference filled in by SetWorld; allocated here so the field is
        // never null after SetWorld is called.
        _emptyChunk = new Chunk(null!, 0, 0);
        _emptyChunk.NoSave           = true;
        _emptyChunk.IsLightPopulated = true;
    }

    /// <summary>
    /// Wires the owning world after it has been constructed.
    /// Must be called exactly once, before any chunk operations.
    /// Mirrors <see cref="ChunkProviderGenerate.SetWorld"/>.
    /// </summary>
    public void SetWorld(World world)
    {
        _world = world;
        _generator.SetWorld(world);
        // Re-point the sentinel's world field so it is consistent with live queries.
        _emptyChunk = new Chunk(world, 0, 0);
        _emptyChunk.NoSave           = true;
        _emptyChunk.IsLightPopulated = true;
    }

    // ── Chunk key ─────────────────────────────────────────────────────────────

    private static long ChunkKey(int x, int z)
        => (long)x & 0xFFFF_FFFFL | ((long)z & 0xFFFF_FFFFL) << 32;

    // ── IChunkLoader ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads or generates the chunk at (chunkX, chunkZ). Cancels any pending unload.
    /// Spec: <c>jz.a(int x, int z)</c>.
    /// </summary>
    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        long key = ChunkKey(chunkX, chunkZ);

        // Cancel pending unload
        _unloadQueue.Remove(key);

        if (_cache.TryGetValue(key, out Chunk? cached)) return cached;

        return LoadOrCreateChunk(chunkX, chunkZ, key);
    }

    /// <summary>
    /// Cache lookup, falling back to <see cref="GetChunk"/> if not present.
    /// Does NOT cancel pending unloads. Spec: <c>jz.b(int x, int z)</c>.
    /// </summary>
    public Chunk GetChunkOrLoad(int chunkX, int chunkZ)
        => _cache.TryGetValue(ChunkKey(chunkX, chunkZ), out Chunk? cached)
            ? cached
            : GetChunk(chunkX, chunkZ);

    /// <inheritdoc/>
    public bool IsChunkLoaded(int chunkX, int chunkZ)
        => _cache.ContainsKey(ChunkKey(chunkX, chunkZ));

    /// <inheritdoc/>
    public void Tick()
    {
        // Step 1 — unload up to MaxUnloadsPerTick queued chunks
        int unloaded = 0;
        foreach (long key in _unloadQueue)
        {
            if (unloaded >= MaxUnloadsPerTick) break;
            if (_cache.TryGetValue(key, out Chunk? chunk))
            {
                // Save before evicting
                if (chunk.NeedsSaving(true))
                {
                    _disk.SaveChunk(_world!, chunk);
                    _disk.PostSave(_world!, chunk);
                }
                _cache.Remove(key);
                _loadedList.Remove(chunk);
            }
            unloaded++;
        }
        // Remove processed keys
        if (unloaded > 0)
        {
            int removed = 0;
            _unloadQueue.RemoveWhere(_ => ++removed <= unloaded);
        }

        // Step 2 — sweep 10 chunks from rolling cursor; queue far chunks for unload
        if (_loadedList.Count > 0)
        {
            for (int i = 0; i < 10; i++)
            {
                if (_sweepCursor >= _loadedList.Count) _sweepCursor = 0;
                Chunk c = _loadedList[_sweepCursor];
                _sweepCursor++;
                QueueForUnloadIfFar(c);
            }
        }

        // Step 3 — flush disk I/O
        _disk.Flush();

        // Step 4 — generator tick (returns bool in spec, unused here)
        _generator.Tick();
    }

    /// <inheritdoc/>
    public IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords()
    {
        foreach (Chunk c in _loadedList)
            yield return (c.ChunkX, c.ChunkZ);
    }

    // ── Dirty-chunk save (spec §6.3) ──────────────────────────────────────────

    /// <summary>
    /// Saves dirty chunks. When <paramref name="saveAll"/> is true, saves all chunks with
    /// entities regardless of age; when false, only saves overdue-entity or modified chunks.
    /// Throttled to <see cref="MaxSavesPerTick"/> per call unless <paramref name="saveAll"/>.
    /// Spec: <c>jz.a(bool saveAll, rz progressMonitor)</c>.
    /// </summary>
    public void SaveDirtyChunks(bool saveAll)
    {
        int saved = 0;
        foreach (Chunk chunk in _loadedList)
        {
            if (chunk.NoSave) continue;
            if (chunk.NeedsSaving(saveAll))
            {
                _disk.SaveChunk(_world!, chunk);
                _disk.PostSave(_world!, chunk);
                chunk.LastSaveTime = _world!.TotalWorldTime;
                saved++;
                if (!saveAll && saved >= MaxSavesPerTick) break;
            }
        }
        if (saveAll) _disk.Flush();
    }

    // ── Unload queueing (spec §6.5) ───────────────────────────────────────────

    /// <summary>
    /// Queues the chunk at (chunkX, chunkZ) for unload if no player is within 128 blocks.
    /// Spec: <c>jz.d(int x, int z)</c>.
    /// </summary>
    public void QueueForUnload(int chunkX, int chunkZ)
    {
        long key  = ChunkKey(chunkX, chunkZ);
        if (!_cache.ContainsKey(key)) return;
        QueueForUnloadKey(chunkX, chunkZ, key);
    }

    /// <summary>
    /// Spec §10: canSave() — always returns true for ChunkProviderServer.
    /// </summary>
    public bool CanSave => true;

    // ── Private helpers ───────────────────────────────────────────────────────

    private Chunk LoadOrCreateChunk(int chunkX, int chunkZ, long key)
    {
        // Out-of-bounds guard → EmptyChunk sentinel
        if (chunkX < -ChunkBoundRadius || chunkX > ChunkBoundRadius ||
            chunkZ < -ChunkBoundRadius || chunkZ > ChunkBoundRadius)
            return _emptyChunk;

        // Try disk first
        Chunk? chunk = _disk.LoadChunk(_world!, chunkX, chunkZ);

        if (chunk == null)
        {
            // Generate fresh
            chunk = _generator.GetChunk(chunkX, chunkZ);
        }
        else
        {
            // Freshly loaded from disk — build sky-light map if not already populated
            if (!chunk.IsLightPopulated)
                chunk.GenerateSkylightMap();
        }

        _cache[key] = chunk;
        _loadedList.Add(chunk);

        // Trigger 2×2 population: this chunk could be the (+1,+1) corner of up to 4 candidate pairs
        TryPopulateAround(chunkX, chunkZ);

        return chunk;
    }

    /// <summary>
    /// For each of the four (x-1,z-1), (x-1,z), (x,z-1), (x,z) "bottom-left" positions,
    /// check whether that chunk and its (+1,0), (0,+1), (+1,+1) neighbours are all cached.
    /// If so, call <see cref="ChunkProviderGenerate.PopulateChunkFromServer"/> on the origin.
    /// Spec: <c>zx.a(ej, ej, int, int)</c> — the 2×2 population trigger.
    /// </summary>
    private void TryPopulateAround(int cx, int cz)
    {
        for (int dx = -1; dx <= 0; dx++)
        for (int dz = -1; dz <= 0; dz++)
        {
            int ox = cx + dx;
            int oz = cz + dz;

            // All four corners must be cached
            if (_cache.ContainsKey(ChunkKey(ox,     oz    )) &&
                _cache.ContainsKey(ChunkKey(ox + 1, oz    )) &&
                _cache.ContainsKey(ChunkKey(ox,     oz + 1)) &&
                _cache.ContainsKey(ChunkKey(ox + 1, oz + 1)))
            {
                Chunk origin = _cache[ChunkKey(ox, oz)];
                if (!origin.IsPopulated)
                    _generator.PopulateChunkFromServer(ox, oz);
            }
        }
    }

    private void QueueForUnloadIfFar(Chunk chunk)
    {
        if (chunk.NoSave) return;
        double cx = chunk.ChunkX * 16 + 8.0;
        double cz = chunk.ChunkZ * 16 + 8.0;

        if (IsPlayerNear(cx, cz, UnloadRadiusSq)) return;

        _unloadQueue.Add(ChunkKey(chunk.ChunkX, chunk.ChunkZ));
    }

    private void QueueForUnloadKey(int chunkX, int chunkZ, long key)
    {
        double cx = chunkX * 16 + 8.0;
        double cz = chunkZ * 16 + 8.0;
        if (!IsPlayerNear(cx, cz, PlayerSafeRadiusSq))
            _unloadQueue.Add(key);
    }

    private bool IsPlayerNear(double cx, double cz, int radiusSq)
    {
        foreach ((double px, double pz) in _world!.GetLoadedPlayerPositions())
        {
            double dx = px - cx;
            double dz = pz - cz;
            if (dx * dx + dz * dz < radiusSq) return true;
        }
        return false;
    }
}
