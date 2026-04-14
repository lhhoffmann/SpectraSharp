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

    // Entity lists (spec §3)
    private readonly List<Entity> _loadedEntityList  = [];  // g — all loaded entities
    private readonly List<Entity> _globalEntityList  = [];  // j — always ticked, never unloaded
    private readonly List<Entity> _playerList        = [];  // i — players (EntityPlayer subset)
    private readonly List<Entity> _entityRemovalQueue = []; // J — flushed at start of TickEntities

    private readonly SortedSet<ScheduledUpdate>          _scheduledUpdates;    // K
    private readonly HashSet<(int x, int y, int z, int id)> _scheduledSet;     // L

    private int  _tickLcgState;     // l — random tick LCG state (init: worldRandom.NextInt())
    private const int TickLcgAdd = 1013904223; // m — LCG addend (quirk 2)

#pragma warning disable CS0169, CS0649 // spec-required weather/state fields — logic pending WorldProvider spec
    private float _prevRainStrength;    // n
    private float _rainStrength;        // o  — read by UpdateSkyDarkening; assigned by weather tick (pending)
    private float _prevThunderStrength; // p
    private float _thunderStrength;     // q  — read by UpdateSkyDarkening; assigned by weather tick (pending)
    private int   _thunderFlashTimer;   // r
    private int   _thunderFlashCount;   // s
    private bool  _isTileEntityTicking; // S — guard flag
#pragma warning restore CS0169, CS0649
#pragma warning disable CS0414
    private int  _autoSaveInterval = 40; // u — auto-save interval ticks
#pragma warning restore CS0414

    private long _worldTime;            // via LevelData C.b()
    private long _totalWorldTime;       // via LevelData C.f()

    // ── Light BFS (spec: LightPropagation_Spec.md) ───────────────────────────

    /// <summary>
    /// obf: <c>H[32768]</c> — packed int queue for BFS light propagation.
    /// Entry format: <c>(dx+32) | ((dy+32)&lt;&lt;6) | ((dz+32)&lt;&lt;12) | (level&lt;&lt;18)</c>.
    /// Origin constant: 32 | (32&lt;&lt;6) | (32&lt;&lt;12) = 133152.
    /// </summary>
    private readonly int[] _lightBfsQueue = new int[32768];

    /// <summary>
    /// obf: <c>k</c> — sky darkening amount (0–11) subtracted from sky-light for rendering.
    /// 0 = full day; 11 = midnight or heavy storm.
    /// </summary>
    public int SkyDarkening { get; private set; }

    // 6-neighbour offsets used by light BFS (spec §6.4 iteration pattern: Z-, Z+, Y-, Y+, X-, X+)
    private static readonly (int dx, int dy, int dz)[] BfsNeighbors =
    {
        ( 0,  0, -1), ( 0,  0,  1),
        ( 0, -1,  0), ( 0,  1,  0),
        (-1,  0,  0), ( 1,  0,  0),
    };

    // ── Public state ──────────────────────────────────────────────────────────

    public  bool         IsClientSide   { get; }        // I
    public  JavaRandom   Random         { get; }        // w
    public  bool         IsNether       { get; set; }   // y.e (WorldProvider.isNether)
    /// <summary>
    /// obf: <c>t</c> — static update mode; suppresses notifications during atomic fluid conversions.
    /// Set true before still→flowing swap, false after. Spec: BlockFluid_Spec §10 / §16.
    /// </summary>
    public  bool         SuppressUpdates { get; set; }  // t
    public  long         WorldSeed      { get; }
    public  long         WorldTime    => _worldTime;
    public  long         TotalWorldTime => _totalWorldTime;

    private readonly IChunkLoader  _chunkLoader;    // A
    private readonly WorldProvider? _worldProvider;  // y — dimension rules (k)

    /// <summary>
    /// WorldChunkManager (<c>vh</c>) — biome and climate data for this world.
    /// Set by the caller after construction (e.g. Engine) before chunks are generated.
    /// </summary>
    public WorldChunkManager? ChunkManager { get; set; }

    /// <summary>Dimension ID from the WorldProvider (0=Overworld, -1=Nether, 1=End).</summary>
    public int DimensionId => _worldProvider?.DimensionId ?? 0;

    /// <summary>
    /// obf: <c>v</c> — difficulty int (0=Peaceful, 1=Easy, 2=Normal, 3=Hard).
    /// Affects hunger drain, starvation damage, and mob spawn rates.
    /// Default: 2 (Normal).
    /// </summary>
    public int Difficulty { get; set; } = 2;

    // ── Save system (spec: WorldSave_Spec.md) ─────────────────────────────────

    /// <summary>
    /// Active save handler. Set by <see cref="WorldSave.SaveHandler"/> after construction.
    /// Null → NullSaveHandler (world will not persist).
    /// </summary>
    public WorldSave.ISaveHandler? SaveHandler { get; set; }

    /// <summary>Level metadata (si). Set alongside SaveHandler.</summary>
    public WorldSave.WorldInfo? WorldInfo { get; set; }

    /// <summary>
    /// Verifies the session lock. Called by DiskChunkLoader before each chunk save.
    /// Spec: <c>world.s()</c> — delegates to <c>nh.b()</c>.
    /// </summary>
    public void VerifySessionLock() => SaveHandler?.VerifySessionLock();

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
    /// obf: kq.a() — WorldChunkManager (vh).
    /// Returns the <see cref="ChunkManager"/> (may be null before world setup).
    /// </summary>
    public object GetContextObject() => ChunkManager!;

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
    /// obf: kq.a(x,y,z,emissionHint) — combined packed-int light value for the renderer.
    /// Packs sky light (bits 23-20) and block light (bits 7-4) into a single int.
    /// Spec: LightPropagation_Spec.md §5.2.
    /// </summary>
    public int GetLightValue(int x, int y, int z, int blockLightEmission)
    {
        int sky   = ReadLightWithNeighborMax(LightType.Sky,   x, y, z);
        int block = Math.Max(ReadLightWithNeighborMax(LightType.Block, x, y, z), blockLightEmission);
        return (sky << 20) | (block << 4);
    }

    /// <summary>
    /// obf: kq.b(x,y,z,emissionHint) — ambient occlusion brightness float 0–1.
    /// Uses WorldProvider.BrightnessTable indexed by the effective light level (0–15).
    /// Effective level = max(sky − SkyDarkening, block, emissionHint). Spec: §5.1 / §12.
    /// </summary>
    public float GetBrightness(int x, int y, int z, int blockLightEmission)
    {
        if (_worldProvider == null) return 1.0f;
        int sky   = ReadLightWithNeighborMax(LightType.Sky,   x, y, z) - SkyDarkening;
        int block = Math.Max(ReadLightWithNeighborMax(LightType.Block, x, y, z), blockLightEmission);
        int level = Math.Clamp(Math.Max(sky, block), 0, 15);
        return _worldProvider.BrightnessTable[level];
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
        Chunk chunk   = GetChunkFromBlockCoords(x, z);
        int   oldId   = chunk.GetBlockId(x & 15, y, z & 15);
        bool  changed = chunk.SetBlock(x & 15, y, z & 15, blockId, meta);
        if (changed) OnBlockChanged(x, y, z, blockId, oldId);
        return changed;
    }

    /// <summary>
    /// Sets block ID only (metadata → 0).
    /// Spec: <c>d(int x, int y, int z, int blockId)</c> → bool.
    /// </summary>
    public bool SetBlock(int x, int y, int z, int blockId)
    {
        if (!IsInBounds(x, y, z)) return false;
        Chunk chunk   = GetChunkFromBlockCoords(x, z);
        int   oldId   = chunk.GetBlockId(x & 15, y, z & 15);
        bool  changed = chunk.SetBlock(x & 15, y, z & 15, blockId);
        if (changed) OnBlockChanged(x, y, z, blockId, oldId);
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

    /// <summary>
    /// Direct chunk write with no light propagation or neighbour notifications.
    /// Used by world-gen passes (ore veins, cave carvers) for bulk silent writes.
    /// Equivalent to vanilla's generation-context <c>world.d()</c>.
    /// </summary>
    public void SetBlockSilent(int x, int y, int z, int blockId)
    {
        if (!IsInBounds(x, y, z)) return;
        GetChunkFromBlockCoords(x, z).SetBlock(x & 15, y, z & 15, blockId);
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

    /// <summary>
    /// Returns all pending scheduled ticks whose block position falls within
    /// the given chunk's 16×16 XZ column. Used by DiskChunkLoader to serialise TileTicks.
    /// Spec: <c>world.a(chunk, false)</c> — returns <c>List&lt;ahn&gt;</c>.
    /// <paramref name="consume"/> = false → query only; true = also remove from queue (not used in 1.0).
    /// </summary>
    internal List<ScheduledUpdate> GetPendingTicksInChunk(int chunkX, int chunkZ, bool consume)
    {
        int minX = chunkX << 4;
        int minZ = chunkZ << 4;
        int maxX = minX + 16;
        int maxZ = minZ + 16;

        var result = new List<ScheduledUpdate>();
        foreach (var entry in _scheduledUpdates)
        {
            if (entry.X >= minX && entry.X < maxX && entry.Z >= minZ && entry.Z < maxZ)
                result.Add(entry);
        }

        if (consume)
        {
            foreach (var entry in result)
            {
                _scheduledUpdates.Remove(entry);
                _scheduledSet.Remove((entry.X, entry.Y, entry.Z, entry.BlockId));
            }
        }

        return result;
    }

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
    /// Players skip the chunk-loaded check (always added). Chunk entity-bucket and
    /// IWorldAccess notifications are stubbed pending those specs.
    /// </summary>
    public void SpawnEntity(Entity entity)
    {
        int chunkX = (int)Math.Floor(entity.PosX) >> 4;
        int chunkZ = (int)Math.Floor(entity.PosZ) >> 4;
        bool isPlayer = entity is EntityPlayer;

        if (!isPlayer && !_chunkLoader.IsChunkLoaded(chunkX, chunkZ)) return;

        if (isPlayer) _playerList.Add(entity);
        _loadedEntityList.Add(entity);

        // Add to chunk entity bucket (sets AddedToChunk + ChunkCoord fields)
        Chunk chunk = GetChunkFromBlockCoords((int)Math.Floor(entity.PosX),
                                             (int)Math.Floor(entity.PosZ));
        chunk.AddEntity(entity);
    }

    /// <summary>
    /// obf: <c>a(ia origin, String name, float volume, float pitch)</c> — plays a named sound
    /// at the entity's position. Stub: no-op until SoundManager is implemented.
    /// </summary>
    public void PlaySoundAt(Entity origin, string name, float volume, float pitch) { }

    /// <summary>
    /// Combined light level 0–15: max(sky − SkyDarkening, block). Spec: <c>world.getLightBrightness(x,y,z)</c>.
    /// </summary>
    public int GetLightBrightness(int x, int y, int z)
    {
        int sky   = ReadLightWithNeighborMax(LightType.Sky,   x, y, z) - SkyDarkening;
        int block = ReadLightWithNeighborMax(LightType.Block, x, y, z);
        return Math.Clamp(Math.Max(sky, block), 0, 15);
    }

    /// <summary>Stub — no-op until SoundManager is implemented. Spec: <c>world.playAuxSFX(...)</c>.</summary>
    public void PlayAuxSFX(EntityPlayer? player, int eventId, int x, int y, int z, int data) { }

    /// <summary>Stub — returns false until redstone is implemented. Spec: <c>world.isBlockIndirectlyReceivingPower(x,y,z)</c>.</summary>
    public bool IsBlockIndirectlyReceivingPower(int x, int y, int z) => false;

    /// <summary>
    /// Marks an entity for removal at the start of the next <see cref="TickEntities"/> call.
    /// Spec: entity calls <c>v()</c> (setDead) on itself; World flushes queue via <c>J</c>.
    /// </summary>
    public void MarkEntityForRemoval(Entity entity)
    {
        entity.IsDead = true;
        _entityRemovalQueue.Add(entity);
    }

    /// <summary>
    /// Ticks all entities and tile entities. Spec: <c>m()</c> — called once per 20 Hz tick,
    /// separately from <see cref="MainTick"/> in vanilla. Called from <see cref="MainTick"/> here
    /// since we have no separate server/client split.
    ///
    /// Step 1 — Tick global entities (j list): always present, never chunk-unloaded.
    /// Step 2 — Flush removal queue (J): remove dead entities from g, chunk, listeners.
    /// Step 3 — Tick regular entities (g list): call Tick(), remove dead.
    /// Step 4 — Tile entity ticking: stub (bq spec pending).
    /// </summary>
    public void TickEntities()
    {
        // Step 1: tick global entities
        for (int i = _globalEntityList.Count - 1; i >= 0; i--)
        {
            Entity e = _globalEntityList[i];
            e.Tick();
            if (e.IsDead) _globalEntityList.RemoveAt(i);
        }

        // Step 2: flush removal queue
        foreach (Entity dead in _entityRemovalQueue)
        {
            _loadedEntityList.Remove(dead);
            _playerList.Remove(dead);

            // Remove from chunk entity bucket (only if entity was tracked in a chunk)
            if (dead.AddedToChunk)
            {
                Chunk deadChunk = GetChunkFromBlockCoords((int)Math.Floor(dead.PosX),
                                                         (int)Math.Floor(dead.PosZ));
                deadChunk.RemoveEntity(dead);
            }
        }
        _entityRemovalQueue.Clear();

        // Step 3: tick regular entities (iterate backwards so RemoveAt is safe)
        for (int i = _loadedEntityList.Count - 1; i >= 0; i--)
        {
            Entity e = _loadedEntityList[i];
            if (e.IsDead)
            {
                _loadedEntityList.RemoveAt(i);
                _playerList.Remove(e);
                continue;
            }
            TickEntityWithPartialTick(e);
        }

        // Step 4: tile entity tick — stub (bq spec pending)
    }

    /// <summary>
    /// Saves old position, calls <see cref="Entity.Tick"/>, updates chunk bucket on move.
    /// Spec: <c>f(ia entity)</c> — tickEntityWithPartialTick.
    /// </summary>
    private void TickEntityWithPartialTick(Entity entity)
    {
        double prevX = entity.PosX;
        double prevY = entity.PosY;
        double prevZ = entity.PosZ;

        entity.Tick();

        // Update chunk assignment if entity crossed a chunk boundary
        int newChunkX = (int)Math.Floor(entity.PosX) >> 4;
        int newChunkZ = (int)Math.Floor(entity.PosZ) >> 4;
        if (entity.AddedToChunk && (newChunkX != entity.ChunkCoordX || newChunkZ != entity.ChunkCoordZ))
        {
            // Transfer entity between chunk buckets
            Chunk oldChunk = GetChunkFromChunkCoords(entity.ChunkCoordX, entity.ChunkCoordZ);
            Chunk newChunk = GetChunkFromChunkCoords(newChunkX, newChunkZ);
            oldChunk.RemoveEntity(entity);
            newChunk.AddEntity(entity);
        }

        _ = (prevX, prevY, prevZ); // suppress unused warning — needed for future IWorldAccess dirty-rect
    }

    // ── Entity queries ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all player entities within <paramref name="radius"/> of (x, y, z).
    /// Used by EntityItem to trigger pickup checks.
    /// </summary>
    public List<EntityPlayer> GetNearbyPlayers(double x, double y, double z, double radius)
    {
        var result = new List<EntityPlayer>();
        double r2 = radius * radius;
        foreach (Entity e in _playerList)
        {
            if (e is not EntityPlayer ep) continue;
            double dx = ep.PosX - x, dy = ep.PosY - y, dz = ep.PosZ - z;
            if (dx * dx + dy * dy + dz * dz <= r2)
                result.Add(ep);
        }
        return result;
    }

    /// <summary>
    /// Returns the world-space (X, Z) positions of all loaded players.
    /// Used by <see cref="ChunkProviderServer"/> to determine chunk-unload proximity.
    /// </summary>
    public IEnumerable<(double x, double z)> GetLoadedPlayerPositions()
    {
        foreach (Entity e in _playerList)
            yield return (e.PosX, e.PosZ);
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
            var blk = Block.BlocksList[id];
            if (blk != null)
                blk.AddCollisionBoxesToList(this, bx, by, bz, box, list);
            else
            {
                // Block ID is set but no Core.Block instance registered — default to solid 1×1×1 cube
                var fallback = AxisAlignedBB.GetFromPool(bx, by, bz, bx + 1, by + 1, bz + 1);
                if (box.Intersects(fallback)) list.Add(fallback);
            }
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
    /// Top opaque-block Y from the heightmap (does not include liquid surfaces).
    /// Spec: <c>d(int x, int z)</c> → int (<c>getHeightValue</c>).
    /// Used by tree and decoration generators for surface placement.
    /// </summary>
    public int GetHeightValue(int x, int z)
        => GetChunkFromBlockCoords(x, z).GetHeightAt(x & 15, z & 15);

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
        // 1. Advance world time
        _worldTime      = (_worldTime + 1) % 24000;
        _totalWorldTime += 1;

        // 2. Recompute sky darkening for time-of-day / weather (spec: p())
        UpdateSkyDarkening();

        // 3. ChunkLoader tick

        // 4. Tick entities + tile entities (spec: m(), called once per game tick)
        TickEntities();

        // 5. Process scheduled block updates (up to 1000 per tick)
        ProcessScheduledTicks(false);

        // 6. Tick loaded chunks (random block ticks)
        TickChunks();
    }

    /// <summary>
    /// Dispatches 20 random block ticks per loaded chunk using the world-specific LCG.
    /// Quirk 2: LCG is <c>l = l * 3 + 1013904223</c> (NOT Java's standard LCG). Spec: §10.3.
    /// Note: vanilla iterates only chunks within player view distance; we iterate all loaded
    /// chunks because there is no player proximity list yet (vi spec pending). Parity-correct
    /// for single-player or headless simulation.
    /// </summary>
    private void TickChunks()
    {
        foreach (var (chunkX, chunkZ) in _chunkLoader.GetLoadedChunkCoords())
        {
            Chunk chunk = _chunkLoader.GetChunk(chunkX, chunkZ);
            if (!chunk.IsLoaded || !chunk.IsPopulated) continue;

            for (int i = 0; i < 20; i++)
            {
                _tickLcgState = _tickLcgState * 3 + TickLcgAdd; // quirk 2
                int localX = (_tickLcgState >>  2) & 15;
                int localZ = (_tickLcgState >> 10) & 15;
                int localY = (_tickLcgState >> 18) & HeightMask;

                int id = chunk.GetBlockId(localX, localY, localZ);
                if (id != 0 && Block.RenderSpecial[id])
                {
                    Block.BlocksList[id]!.BlockTick(
                        this,
                        chunkX * 16 + localX,
                        localY,
                        chunkZ * 16 + localZ,
                        Random);
                }
            }

            // Random checkLight once per chunk per tick (spec §10)
            CheckLight(
                chunkX * 16 + Random.NextInt(16),
                Random.NextInt(WorldHeight),
                chunkZ * 16 + Random.NextInt(16));
        }
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

    // ── Light read helpers (spec §5) ─────────────────────────────────────────

    /// <summary>
    /// obf: World.a(bn type, int x, int y, int z) 4-arg — read with neighbor-max for
    /// transparent blocks. Spec: LightPropagation §5.1 (yy.s[id] check).
    /// Non-opaque-cube blocks (leaves, glass, water, ice) return the maximum of their 6 neighbors
    /// to prevent hard light boundaries at their surface.
    /// </summary>
    private int ReadLightWithNeighborMax(LightType type, int x, int y, int z)
    {
        if (y < 0 || y >= WorldHeight) return type == LightType.Sky ? 15 : 0;
        if (!IsInBounds(x, y, z))     return 0;
        int id = GetBlockId(x, y, z);
        // Non-opaque-cube blocks sample neighbor max (spec: yy.s[id])
        if (!Block.IsOpaqueCubeArr[id])
        {
            int max = 0;
            foreach (var (dx, dy, dz) in BfsNeighbors)
                max = Math.Max(max, GetLightBrightness(type, x + dx, y + dy, z + dz));
            return max;
        }
        return GetLightBrightness(type, x, y, z);
    }

    // ── Sky/block light value computation (spec §7, §8) ──────────────────────

    /// <summary>
    /// obf: World.a(int current, int x, int y, int z, int blockId, int opacity) — sky.
    /// Spec: LightPropagation §7.
    /// Sky-exposed positions get 15; below height map = neighbor max minus opacity.
    /// </summary>
    private int ComputeSkyLightValue(int x, int y, int z, int opacity)
    {
        if (IsBlockAboveGroundLevel(x, y, z)) return 15;
        int best = 0;
        foreach (var (dx, dy, dz) in BfsNeighbors)
        {
            int nx = x + dx, ny = y + dy, nz = z + dz;
            if (ny < 0 || ny >= WorldHeight) continue;
            int candidate = GetLightBrightness(LightType.Sky, nx, ny, nz) - opacity;
            if (candidate > best) best = candidate;
        }
        return best;
    }

    /// <summary>
    /// obf: World.d(int current, int x, int y, int z, int blockId, int opacity) — block.
    /// Spec: LightPropagation §8.
    /// Block light = self-emission vs max(neighbor block light − opacity).
    /// </summary>
    private int ComputeBlockLightValue(int x, int y, int z, int blockId, int opacity)
    {
        int best = Block.LightValue[blockId]; // self-emission (yy.q[id])
        foreach (var (dx, dy, dz) in BfsNeighbors)
        {
            int nx = x + dx, ny = y + dy, nz = z + dz;
            if (ny < 0 || ny >= WorldHeight) continue;
            int candidate = GetLightBrightness(LightType.Block, nx, ny, nz) - opacity;
            if (candidate > best) best = candidate;
        }
        return best;
    }

    // ── BFS propagation (spec §6) ─────────────────────────────────────────────

    /// <summary>
    /// Main light BFS. obf: World.c(bn type, int x, int y, int z).
    /// Spec: LightPropagation §6. Two-phase algorithm: Phase 1 zeros dependent cells
    /// (decrease path); Phase 2 re-propagates new correct values (increase path).
    /// BFS queue H[32768] is shared — packed as (dx+32)|((dy+32)&lt;&lt;6)|((dz+32)&lt;&lt;12)|(level&lt;&lt;18).
    /// Origin constant 133152 = 32|(32&lt;&lt;6)|(32&lt;&lt;12).
    /// Distance cap 17: no neighbours enqueued beyond Manhattan distance 17 (spec §15).
    /// </summary>
    public void PropagateLight(LightType type, int x, int y, int z)
    {
        if (y < 0 || y >= WorldHeight) return;
        if (!IsAreaLoaded(x, y, z, 17))  return;

        int readHead  = 0;
        int writeHead = 0;

        int current = GetLightBrightness(type, x, y, z);
        int blockId  = GetBlockId(x, y, z);
        int opacity  = Math.Max(Block.LightOpacity[blockId], 1);

        int correct = type == LightType.Sky
            ? ComputeSkyLightValue  (x, y, z, opacity)
            : ComputeBlockLightValue(x, y, z, blockId, opacity);

        const int Origin = 133152; // (0+32)|((0+32)<<6)|((0+32)<<12)

        if (correct > current)
        {
            // Light increased — skip Phase 1, queue origin for Phase 2
            _lightBfsQueue[writeHead++] = Origin;
        }
        else if (correct < current)
        {
            // Light decreased — Phase 1: zero out all cells that drew from this source
            _lightBfsQueue[writeHead++] = Origin | (current << 18);

            while (readHead < writeHead)
            {
                int entry    = _lightBfsQueue[readHead++];
                int px       = ((entry        & 63) - 32) + x;
                int py       = ((entry >>  6  & 63) - 32) + y;
                int pz       = ((entry >> 12  & 63) - 32) + z;
                int oldLevel = (entry >> 18) & 15;
                int stored   = GetLightBrightness(type, px, py, pz);

                if (stored == oldLevel)
                {
                    SetLightValue(type, px, py, pz, 0);
                    if (oldLevel > 0
                        && Math.Abs(px - x) + Math.Abs(py - y) + Math.Abs(pz - z) < 17)
                    {
                        foreach (var (dx, dy, dz) in BfsNeighbors)
                        {
                            int nx = px + dx, ny = py + dy, nz = pz + dz;
                            if (ny < 0 || ny >= WorldHeight) continue;
                            int nOp    = Math.Max(Block.LightOpacity[GetBlockId(nx, ny, nz)], 1);
                            int nLevel = GetLightBrightness(type, nx, ny, nz);
                            if (nLevel == oldLevel - nOp && writeHead < _lightBfsQueue.Length - 6)
                            {
                                _lightBfsQueue[writeHead++] =
                                    ((nx - x + 32)
                                    | ((ny - y + 32) <<  6)
                                    | ((nz - z + 32) << 12))
                                    | ((oldLevel - nOp) << 18);
                            }
                        }
                    }
                }
            }
            readHead = 0; // reset → Phase 2 re-reads same positions
        }
        else
        {
            return; // stored value already correct
        }

        // Phase 2: propagate new correct values outward
        while (readHead < writeHead)
        {
            int entry  = _lightBfsQueue[readHead++];
            int px     = ((entry        & 63) - 32) + x;
            int py     = ((entry >>  6  & 63) - 32) + y;
            int pz     = ((entry >> 12  & 63) - 32) + z;
            if (py < 0 || py >= WorldHeight) continue;

            int stored   = GetLightBrightness(type, px, py, pz);
            int bid2     = GetBlockId(px, py, pz);
            int op2      = Math.Max(Block.LightOpacity[bid2], 1);

            int correct2 = type == LightType.Sky
                ? ComputeSkyLightValue  (px, py, pz, op2)
                : ComputeBlockLightValue(px, py, pz, bid2, op2);

            if (correct2 != stored)
            {
                SetLightValue(type, px, py, pz, correct2);
                if (correct2 > stored)
                {
                    int mdist = Math.Abs(px - x) + Math.Abs(py - y) + Math.Abs(pz - z);
                    if (mdist < 17 && writeHead < _lightBfsQueue.Length - 6)
                    {
                        foreach (var (dx, dy, dz) in BfsNeighbors)
                        {
                            int nx = px + dx, ny = py + dy, nz = pz + dz;
                            if (ny < 0 || ny >= WorldHeight) continue;
                            if (GetLightBrightness(type, nx, ny, nz) < correct2)
                            {
                                _lightBfsQueue[writeHead++] =
                                    (nx - x + 32)
                                    | ((ny - y + 32) <<  6)
                                    | ((nz - z + 32) << 12);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// obf: World.i(int x, int z, int minY, int maxY) — sky BFS for every Y in a column range.
    /// Called from <see cref="Chunk.UpdateHeightMapAt"/> after the height map changes.
    /// Spec: LightPropagation §9.
    /// </summary>
    public void PropagateColumnRange(int x, int z, int minY, int maxY)
    {
        if (minY > maxY) (minY, maxY) = (maxY, minY);
        if (!IsNether)
        {
            for (int y = minY; y <= maxY; y++)
                PropagateLight(LightType.Sky, x, y, z);
        }
        // Render listener notification (bd.a) — stub (bd spec pending)
    }

    /// <summary>
    /// obf: World.s(int x, int y, int z) — random checkLight tick.
    /// Propagates sky (Overworld only) and block light at the given position.
    /// Spec: LightPropagation §10 — called once per chunk per tick on a random block.
    /// </summary>
    public void CheckLight(int x, int y, int z)
    {
        if (!IsNether) PropagateLight(LightType.Sky,   x, y, z);
        PropagateLight(LightType.Block, x, y, z);
    }

    /// <summary>
    /// obf: World.p() — recompute <see cref="SkyDarkening"/> each tick.
    /// Spec: LightPropagation §11.
    /// sunAngle = worldTime / 24000 in [0, 1); brightness = cos(angle * 2π) * 2 + 0.5,
    /// clamped [0, 1], dimmed by rain and thunder; SkyDarkening = (int)((1 − brightness) × 11).
    /// </summary>
    private void UpdateSkyDarkening()
    {
        float sunAngle  = (_worldTime % 24000L) / 24000f;
        float brightness = (float)Math.Cos(sunAngle * Math.PI * 2.0) * 2f + 0.5f;
        brightness = Math.Clamp(brightness, 0f, 1f);
        brightness *= 1f - _rainStrength    * (5f / 16f);
        brightness *= 1f - _thunderStrength * (5f / 16f);
        SkyDarkening = (int)((1f - brightness) * 11f);
    }

    // ── Weather queries (spec §15 / BlockFire_Spec §11) ──────────────────────

    /// <summary>
    /// True when it is raining (rain strength > 0.2).
    /// Spec: World_Spec §15 <c>E()</c> — <c>j(1.0F) > 0.2F</c>.
    /// </summary>
    public bool IsRaining() => _rainStrength > 0.2f;

    /// <summary>
    /// True if the block at (x, y, z) is exposed to sky rainfall.
    /// Spec: BlockFire_Spec §11 <c>w(x,y,z)</c> — block is at or above the precipitation height map.
    /// </summary>
    public bool IsBlockExposedToRain(int x, int y, int z)
        => y >= GetTopSolidOrLiquidBlock(x, z);

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool IsInBounds(int x, int y, int z)
        => x >= -XzBoundary && x < XzBoundary
        && z >= -XzBoundary && z < XzBoundary
        && y >= 0 && y < WorldHeight;

    /// <summary>
    /// Called after any block change. Fires Block lifecycle hooks and notifies 6 neighbours.
    /// Quirk 3: BFS light passes always run (stubbed until BFS is implemented).
    /// </summary>
    private void OnBlockChanged(int x, int y, int z, int newId, int oldId)
    {
        // Lifecycle hooks
        if (oldId != 0) Block.BlocksList[oldId]?.OnBlockRemoved(this, x, y, z);
        if (newId != 0) Block.BlocksList[newId]?.OnBlockAdded  (this, x, y, z);

        // Neighbour notifications — each adjacent block is told which block ID changed
        NotifyNeighboursOfChange(x, y, z, newId);

        // Quirk 3: always trigger both BFS light passes (spec §9)
        PropagateLight(LightType.Sky,   x, y, z);
        PropagateLight(LightType.Block, x, y, z);
    }

    /// <summary>
    /// Notifies the 6 axis-aligned neighbours of a block change at (x, y, z).
    /// Each neighbour receives its own position and the ID of the block that changed.
    /// Spec: <c>world.j(int x, int y, int z, int changedBlockId)</c>.
    /// </summary>
    public void NotifyNeighbors(int x, int y, int z, int changedBlockId)
        => NotifyNeighboursOfChange(x, y, z, changedBlockId);

    private void NotifyNeighboursOfChange(int x, int y, int z, int changedBlockId)
    {
        Block.BlocksList[GetBlockId(x - 1, y, z)]?.OnNeighborBlockChange(this, x - 1, y, z, changedBlockId);
        Block.BlocksList[GetBlockId(x + 1, y, z)]?.OnNeighborBlockChange(this, x + 1, y, z, changedBlockId);
        Block.BlocksList[GetBlockId(x, y - 1, z)]?.OnNeighborBlockChange(this, x, y - 1, z, changedBlockId);
        Block.BlocksList[GetBlockId(x, y + 1, z)]?.OnNeighborBlockChange(this, x, y + 1, z, changedBlockId);
        Block.BlocksList[GetBlockId(x, y, z - 1)]?.OnNeighborBlockChange(this, x, y, z - 1, changedBlockId);
        Block.BlocksList[GetBlockId(x, y, z + 1)]?.OnNeighborBlockChange(this, x, y, z + 1, changedBlockId);
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
