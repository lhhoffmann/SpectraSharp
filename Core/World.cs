using System.Collections.Generic;

namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>ry</c> (World) — root game object.
/// Implements <see cref="IBlockAccess"/> (<c>kq</c>) and <see cref="IWorld"/>.
/// Owns all chunks, entities, tile entities, and the tick schedule.
///
/// World geometry constants (128-high world, from spec §2):
///   a=7 (HeightBits), b=11 (XShift), c=128 (height), d=127 (heightMask), e=63 (midY)
///
/// Quirks preserved (see spec §17):
///   1. GetBlockRandom returns the SHARED random w — not re-entrant; use immediately.
///   2. Random tick LCG: l = l * 3 + 1013904223 — NOT Java's standard LCG.
///   3. SetBlock triggers two BFS light passes even for non-emitting/non-opaque blocks.
///   4. Ray-trace off-by-one: face IDs 1/3/5 require blockCoord-- and hitVec+=1.
///   5. IsBlockEmpty returns id==0 (air only, not leaves or any other block).
///
/// Open stubs (specs pending):
///   - Entity management (ia spec)
///   - TileEntity operations (bq spec)
///   - WorldProvider sky-brightness table (k spec)
///   - LevelData / WorldInfo (si / nh specs)
///   - IWorldAccess renderer notifications (bd spec)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/World_Spec.md
/// </summary>
public class World : IWorld
{
    // ── World geometry constants (spec §2) ────────────────────────────────────

    public const int HeightBits  = 7;    // a
    public const int XShift      = 11;   // b
    public const int WorldHeight = 128;  // c
    public const int HeightMask  = 127;  // d
    public const int MidWorldY   = 63;   // e

    private const int XzBoundary = 30_000_000;

    // ── Fields (spec §3) ──────────────────────────────────────────────────────

    // Entity / tile entity lists — stub (ia/bq spec pending)
    // g:  List<ia>  all loaded entities
    // h:  List<bq>  active tile entities
    // i:  List<vi>  players
    // j:  List<ia>  global entities (always ticked)
    // J:  List<ia>  entity removal queue
    // M/N: tile entity pending add/remove queues

    private readonly SortedSet<ScheduledUpdate>          _scheduledUpdates;    // K
    private readonly HashSet<(int x, int y, int z, int id)> _scheduledSet;     // L

    private int  _tickLcgState;     // l — random tick LCG state (init: worldRandom.NextInt())
    private const int TickLcgAdd = 1013904223; // m — LCG addend (quirk 2)

#pragma warning disable CS0169 // spec-required weather/state fields — logic pending WorldProvider spec
    private float _prevRainStrength;    // n
    private float _rainStrength;        // o
    private float _prevThunderStrength; // p
    private float _thunderStrength;     // q
    private int   _thunderFlashTimer;   // r
    private int   _thunderFlashCount;   // s
    private bool  _isTileEntityTicking; // S — guard flag
#pragma warning restore CS0169
#pragma warning disable CS0414
    private int  _autoSaveInterval = 40; // u — auto-save interval ticks
#pragma warning restore CS0414

    private long _worldTime;            // via LevelData C.b()
    private long _totalWorldTime;       // via LevelData C.f()

    // ── Public state ──────────────────────────────────────────────────────────

    public  bool         IsClientSide { get; }          // I
    public  JavaRandom   Random       { get; }          // w
    public  bool         IsNether     { get; set; }     // y.e (WorldProvider.isNether)
    public  long         WorldSeed    { get; }
    public  long         WorldTime    => _worldTime;
    public  long         TotalWorldTime => _totalWorldTime;

    private readonly IChunkLoader  _chunkLoader;    // A
    private readonly WorldProvider? _worldProvider;  // y — dimension rules (k)

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Main constructor. Spec: <c>ry(IChunkLoader, WorldProvider, long seed, bool isClient)</c>.
    /// Calls <see cref="WorldProvider.RegisterWorld"/> which fills the brightness table.
    /// </summary>
    public World(IChunkLoader chunkLoader, long worldSeed, bool isClientSide = false,
                 WorldProvider? worldProvider = null)
    {
        _chunkLoader  = chunkLoader;
        _worldProvider = worldProvider;
        WorldSeed     = worldSeed;
        IsClientSide  = isClientSide;

        Random = new JavaRandom();
        Random.SetSeed(worldSeed);

        _tickLcgState = Random.NextInt();

        _scheduledUpdates = new SortedSet<ScheduledUpdate>();
        _scheduledSet     = new HashSet<(int, int, int, int)>();

        if (worldProvider != null)
        {
            worldProvider.RegisterWorld(this); // fills BrightnessTable, sets WorldRef
            IsNether = worldProvider.IsNether;
        }
    }

    // ── IBlockAccess implementation (spec §5) ─────────────────────────────────

    /// <summary>
    /// obf: kq.a() — WorldChunkManager (vh). [UNCERTAIN] — WorldProvider spec pending.
    /// Returns null stub until WorldProvider is implemented.
    /// </summary>
    public object GetContextObject() => null!;

    /// <summary>obf: kq.b() — world height = 128.</summary>
    public int GetHeight() => WorldHeight;

    /// <summary>
    /// obf: kq.a(x,y,z) — block ID at world position.
    /// Returns 0 for out-of-bounds (±30M XZ, 0–127 Y). Spec: World_Spec §5.
    /// </summary>
    public int GetBlockId(int x, int y, int z)
    {
        if (x < -XzBoundary || x >= XzBoundary || z < -XzBoundary || z >= XzBoundary) return 0;
        if (y < 0 || y >= WorldHeight) return 0;
        return GetChunkFromBlockCoords(x, z).GetBlockId(x & 15, y, z & 15);
    }

    /// <summary>
    /// obf: kq.b(x,y,z) — tile entity. [UNCERTAIN] — bq spec pending. Returns null stub.
    /// </summary>
    public object? GetTileEntity(int x, int y, int z)
    {
        // Delegates to chunk.d(x&15, y, z&15) + M fallback — stub (bq spec pending)
        return null;
    }

    /// <summary>
    /// obf: kq.a(x,y,z,emissionHint) — combined light value.
    /// [UNCERTAIN] — full implementation requires WorldProvider sky-brightness table (spec §18).
    /// Returns emission value as stub.
    /// </summary>
    public int GetLightValue(int x, int y, int z, int blockLightEmission)
    {
        // TODO: implement BFS light query + transparent-block max-6-neighbours path
        return blockLightEmission;
    }

    /// <summary>
    /// obf: kq.b(x,y,z,emissionHint) — ambient occlusion brightness float 0–1.
    /// Uses WorldProvider.BrightnessTable[lightLevel] when WorldProvider is available.
    /// Full BFS light query is still stubbed — uses GetLightValue as placeholder.
    /// </summary>
    public float GetBrightness(int x, int y, int z, int blockLightEmission)
    {
        if (_worldProvider == null) return 1.0f;
        int lightLevel = Math.Clamp(GetLightValue(x, y, z, blockLightEmission), 0, 15);
        return _worldProvider.BrightnessTable[lightLevel];
    }

    /// <summary>
    /// obf: kq.c(x,y,z) — combined brightness float. [UNCERTAIN] stub. Returns 1.0f.
    /// </summary>
    public float GetUnknownFloat(int x, int y, int z) => 1.0f;

    /// <summary>
    /// obf: kq.d(x,y,z) — block metadata at world position.
    /// Returns 0 for out-of-bounds.
    /// </summary>
    public int GetBlockMetadata(int x, int y, int z)
    {
        if (x < -XzBoundary || x >= XzBoundary || z < -XzBoundary || z >= XzBoundary) return 0;
        if (y < 0 || y >= WorldHeight) return 0;
        return GetChunkFromBlockCoords(x, z).GetMetadata(x & 15, y, z & 15);
    }

    /// <summary>
    /// obf: kq.e(x,y,z) — Material of the block at position.
    /// Air (id 0) → Material.Air. Spec: World_Spec §5.
    /// </summary>
    public Material GetBlockMaterial(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        if (id == 0) return Material.Air;
        return Block.BlocksList[id]?.BlockMaterial ?? Material.Air;
    }

    /// <summary>
    /// obf: kq.f(x,y,z) — true if the block is a fully opaque solid cube. Spec: World_Spec §5.
    /// </summary>
    public bool IsOpaqueCube(int x, int y, int z)
    {
        Block? block = Block.BlocksList[GetBlockId(x, y, z)];
        return block != null && block.IsOpaqueCube();
    }

    /// <summary>
    /// obf: kq.g(x,y,z) — true if the block at position is wet / in liquid.
    /// Spec: World_Spec §5 — <c>block.bZ.j() &amp;&amp; block.b()</c>.
    /// </summary>
    public bool IsWet(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        Block? block = Block.BlocksList[id];
        return block != null && (block.BlockMaterial?.CanBePushed() ?? false) && block.IsCollidable();
    }

    /// <summary>
    /// obf: kq.h(x,y,z) — true if the block is air (id == 0). Quirk 5 preserved.
    /// </summary>
    public bool GetUnknownBool(int x, int y, int z) => GetBlockId(x, y, z) == 0;

    // ── IWorld block writes (spec §6) ─────────────────────────────────────────

    /// <summary>
    /// Sets block ID and metadata, propagates light, notifies neighbours.
    /// Spec: <c>b(int x, int y, int z, int blockId, int meta)</c> → bool.
    /// Quirk 3: always triggers two BFS light passes (sky + block).
    /// </summary>
    public bool SetBlockAndMetadata(int x, int y, int z, int blockId, int meta)
    {
        if (!IsInBounds(x, y, z)) return false;
        bool changed = GetChunkFromBlockCoords(x, z).SetBlock(x & 15, y, z & 15, blockId, meta);
        if (changed) OnBlockChanged(x, y, z);
        return changed;
    }

    /// <summary>
    /// Sets block ID only (metadata → 0).
    /// Spec: <c>d(int x, int y, int z, int blockId)</c> → bool.
    /// </summary>
    public bool SetBlock(int x, int y, int z, int blockId)
    {
        if (!IsInBounds(x, y, z)) return false;
        bool changed = GetChunkFromBlockCoords(x, z).SetBlock(x & 15, y, z & 15, blockId);
        if (changed) OnBlockChanged(x, y, z);
        return changed;
    }

    /// <summary>
    /// Sets block metadata and triggers neighbour notification.
    /// Spec: <c>c(int x, int y, int z, int meta)</c> → bool.
    /// </summary>
    public bool SetMetadata(int x, int y, int z, int meta)
    {
        if (!IsInBounds(x, y, z)) return false;
        return GetChunkFromBlockCoords(x, z).SetMetadata(x & 15, y, z & 15, meta);
    }

    // ── Tick scheduling (spec §10) ────────────────────────────────────────────

    /// <summary>
    /// Schedules a block tick for <paramref name="blockId"/> at (x, y, z) after
    /// <paramref name="delay"/> ticks. Only adds if not already scheduled. Spec: §10.
    /// </summary>
    public void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay)
    {
        var key = (x, y, z, blockId);
        if (_scheduledSet.Contains(key)) return;
        var entry = new ScheduledUpdate(x, y, z, blockId, _totalWorldTime + delay);
        _scheduledSet.Add(key);
        _scheduledUpdates.Add(entry);
    }

    /// <summary>
    /// Like <see cref="ScheduleBlockUpdate"/> but skips player-proximity check (used on chunk load).
    /// Spec: <c>e(int x, int y, int z, int blockId, int delay)</c>.
    /// </summary>
    public void ScheduleBlockUpdateFromLoad(int x, int y, int z, int blockId, int delay)
        => ScheduleBlockUpdate(x, y, z, blockId, delay);

    // ── Area / chunk queries (spec §11) ──────────────────────────────────────

    /// <summary>
    /// True if all chunks within <paramref name="radius"/> blocks of (x, y, z) are loaded.
    /// Spec: <c>e(int x, int y, int z, int radius)</c> → bool.
    /// </summary>
    public bool IsAreaLoaded(int x, int y, int z, int radius)
        => IsAreaLoadedByBox(x - radius, y - radius, z - radius,
                             x + radius, y + radius, z + radius);

    /// <summary>
    /// True if all chunks overlapping the given block-space bounding box are loaded.
    /// Spec: <c>b(int x0, int y0, int z0, int x1, int y1, int z1)</c> → bool.
    /// </summary>
    public bool IsAreaLoadedByBox(int x0, int y0, int z0, int x1, int y1, int z1)
    {
        if (y1 < 0 || y0 >= WorldHeight) return false;
        int cxMin = x0 >> 4, czMin = z0 >> 4;
        int cxMax = x1 >> 4, czMax = z1 >> 4;
        for (int cx = cxMin; cx <= cxMax; cx++)
        for (int cz = czMin; cz <= czMax; cz++)
            if (!_chunkLoader.IsChunkLoaded(cx, cz)) return false;
        return true;
    }

    /// <summary>obf: b(blockX, blockZ) → zx — get chunk containing block coords.</summary>
    public Chunk GetChunkFromBlockCoords(int blockX, int blockZ)
        => _chunkLoader.GetChunk(blockX >> 4, blockZ >> 4);

    /// <summary>obf: c(chunkX, chunkZ) → zx — get chunk by chunk-grid coords.</summary>
    public Chunk GetChunkFromChunkCoords(int chunkX, int chunkZ)
        => _chunkLoader.GetChunk(chunkX, chunkZ);

    /// <summary>obf: g(chunkX, chunkZ) → bool — is chunk currently loaded?</summary>
    public bool IsChunkLoaded(int chunkX, int chunkZ)
        => _chunkLoader.IsChunkLoaded(chunkX, chunkZ);

    // ── Entity management (spec §9) ───────────────────────────────────────────

    /// <summary>
    /// Adds entity to the world. Spec: <c>a(ia entity)</c> → bool.
    /// Full implementation pending (chunk entity buckets — ia/vi list management).
    /// </summary>
    public void SpawnEntity(Entity entity)
    {
        // TODO: chunk.AddEntity(entity), entityList.Add(entity), notify IWorldAccess listeners
        // Requires entity list fields (g/h/i/j) and chunk bucket wiring (ia spec §5)
    }

    // ── Collision query (spec §9 / Entity.Move) ───────────────────────────────

    /// <summary>
    /// Returns all block bounding boxes that intersect <paramref name="box"/>.
    /// Called by <see cref="Entity.Move"/> for sweep-collision. Spec: <c>a(ia, c box)</c>.
    /// Entity-entity collision boxes are excluded (entity list management pending).
    /// </summary>
    public List<AxisAlignedBB> GetCollidingBoundingBoxes(Entity entity, AxisAlignedBB box)
    {
        var list = new List<AxisAlignedBB>();

        int x0 = (int)Math.Floor(box.MinX) - 1;
        int x1 = (int)Math.Ceiling(box.MaxX) + 1;
        int y0 = Math.Max(0,           (int)Math.Floor(box.MinY) - 1);
        int y1 = Math.Min(WorldHeight - 1, (int)Math.Ceiling(box.MaxY) + 1);
        int z0 = (int)Math.Floor(box.MinZ) - 1;
        int z1 = (int)Math.Ceiling(box.MaxZ) + 1;

        for (int bx = x0; bx <= x1; bx++)
        for (int by = y0; by <= y1; by++)
        for (int bz = z0; bz <= z1; bz++)
        {
            int id = GetBlockId(bx, by, bz);
            if (id == 0) continue;
            Block.BlocksList[id]?.AddCollisionBoxesToList(this, bx, by, bz, box, list);
        }

        return list;
    }

    // ── Deterministic block random (spec §16) ─────────────────────────────────

    /// <summary>
    /// Returns the shared world random re-seeded at (x, y, z).
    /// Quirk 1: returns the SHARED instance — use the reference immediately; not re-entrant.
    /// Spec: <c>x(int x, int y, int z)</c> → Random.
    /// </summary>
    public JavaRandom GetBlockRandom(int x, int y, int z)
    {
        long seed = (long)x * 341873128712L
                  + (long)y * 132897987541L
                  + WorldSeed
                  + (long)z;
        Random.SetSeed(seed);
        return Random;
    }

    // ── Height queries (spec §12) ─────────────────────────────────────────────

    /// <summary>
    /// True if the block at (x, y, z) is at or above the height-map level for its column.
    /// Spec: <c>l(int x, int y, int z)</c> → bool.
    /// </summary>
    public bool IsBlockAboveGroundLevel(int x, int y, int z)
        => GetChunkFromBlockCoords(x, z).IsAboveHeightMap(x & 15, y, z & 15);

    /// <summary>
    /// Top solid-or-liquid Y+1 at column (x, z) (for precipitation checks).
    /// Spec: <c>e(int x, int z)</c> → int.
    /// </summary>
    public int GetTopSolidOrLiquidBlock(int x, int z)
        => GetChunkFromBlockCoords(x, z).PrecipitationHeightAt(x & 15, z & 15);

    // ── Time queries (spec §15) ───────────────────────────────────────────────

    /// <summary>obf: t() — world time (0–23999, day/night cycle).</summary>
    public long GetWorldTime() => _worldTime;

    /// <summary>obf: u() — total world time (absolute tick counter).</summary>
    public long GetTotalWorldTime() => _totalWorldTime;

    // ── Tick loop (spec §10) ──────────────────────────────────────────────────

    /// <summary>
    /// Main 20 Hz tick. Spec: <c>c()</c>.
    /// Entity management, mob spawning, weather, and ChunkLoader are partially stubbed.
    /// </summary>
    public void MainTick()
    {
        // 1. Weather update — stub (WorldProvider/weather spec pending)

        // 2. ChunkLoader tick
        _chunkLoader.Tick();

        // 3. Advance world time
        _worldTime      = (_worldTime + 1) % 24000;
        _totalWorldTime += 1;

        // 4. Process scheduled block updates (up to 1000 per tick)
        ProcessScheduledTicks(false);

        // 5. Tick loaded chunks (random block ticks)
        TickChunks();
    }

    /// <summary>
    /// Dispatches 20 random block ticks per loaded chunk using the world-specific LCG.
    /// Quirk 2: LCG is <c>l = l * 3 + 1013904223</c> (NOT Java's standard LCG). Spec: §10.3.
    /// </summary>
    private void TickChunks()
    {
        // Full chunk-proximity iteration requires player list (vi spec pending).
        // Structure documented here for correctness — entity-driven iteration stubbed.
        //
        // Per chunk per tick (20 random ticks):
        //   _tickLcgState = _tickLcgState * 3 + TickLcgAdd   (quirk 2)
        //   localX = (_tickLcgState >>  2) & 15
        //   localZ = (_tickLcgState >> 10) & 15
        //   localY = (_tickLcgState >> 18) & 127
        //   if Block.s[id] (needsRandomTick): Block.a(world, worldX, worldY, worldZ, random)
    }

    /// <summary>
    /// Processes up to 1000 scheduled ticks that are due. Spec: <c>a(bool force)</c> → bool.
    /// </summary>
    private bool ProcessScheduledTicks(bool force)
    {
        int processed = 0;
        var toRemove  = new List<ScheduledUpdate>();

        foreach (var entry in _scheduledUpdates)
        {
            if (!force && entry.FireTime > _totalWorldTime) break;
            if (processed >= 1000) break;

            toRemove.Add(entry);
            _scheduledSet.Remove((entry.X, entry.Y, entry.Z, entry.BlockId));

            if (!IsAreaLoaded(entry.X, entry.Y, entry.Z, 8)) continue;

            int id = GetBlockId(entry.X, entry.Y, entry.Z);
            if (id == entry.BlockId)
            {
                Block.BlocksList[id]?.UpdateTick(this, entry.X, entry.Y, entry.Z, Random);
            }
            processed++;
        }

        foreach (var e in toRemove) _scheduledUpdates.Remove(e);
        return _scheduledUpdates.Count > 0;
    }

    // ── Light propagation (spec §7) ───────────────────────────────────────────

    /// <summary>
    /// Delegated light write to chunk. Spec: <c>a(bn type, int x, int y, int z, int value)</c>.
    /// </summary>
    public void SetLightValue(LightType type, int x, int y, int z, int value)
    {
        if (!IsInBounds(x, y, z)) return;
        GetChunkFromBlockCoords(x, z).SetLight(type, x & 15, y, z & 15, value);
    }

    /// <summary>
    /// Read light value from chunk. Spec: <c>b(bn type, int x, int y, int z)</c> → int.
    /// </summary>
    public int GetLightBrightness(LightType type, int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return 0;
        return GetChunkFromBlockCoords(x, z).GetLight(type, x & 15, y, z & 15);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool IsInBounds(int x, int y, int z)
        => x >= -XzBoundary && x < XzBoundary
        && z >= -XzBoundary && z < XzBoundary
        && y >= 0 && y < WorldHeight;

    private void OnBlockChanged(int x, int y, int z)
    {
        // Trigger light propagation and neighbour notifications.
        // Quirk 3: always two BFS passes (sky + block) even for non-opaque/non-emitting blocks.
        // PropagateLight(LightType.Sky,   x, y, z);  — BFS stub
        // PropagateLight(LightType.Block, x, y, z);  — BFS stub
        // NotifyNeighbours(x, y, z);                 — stub (needs Block.onNeighborBlockChange)
    }
}

// ── Supporting types ──────────────────────────────────────────────────────────

/// <summary>
/// Scheduled block update entry. Replica of <c>ahn</c> (TickNextTickEntry).
/// Sorted by fire-time; secondary sort by position ensures no SortedSet collisions.
/// </summary>
internal readonly record struct ScheduledUpdate(int X, int Y, int Z, int BlockId, long FireTime)
    : IComparable<ScheduledUpdate>
{
    public int CompareTo(ScheduledUpdate other)
    {
        int c = FireTime.CompareTo(other.FireTime);
        if (c != 0) return c;
        c = X.CompareTo(other.X); if (c != 0) return c;
        c = Y.CompareTo(other.Y); if (c != 0) return c;
        c = Z.CompareTo(other.Z); if (c != 0) return c;
        return BlockId.CompareTo(other.BlockId);
    }
}
