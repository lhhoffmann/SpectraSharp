using System.Collections.Generic;
using SpectraEngine.Core.Mobs;

namespace SpectraEngine.Core;

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
///   - IWorldAccess renderer notifications: implemented (IWorldAccess_Spec.md)
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

    // IWorldAccess listeners (spec: IWorldAccess_Spec.md) — obf: ry.z
    private readonly List<IWorldAccess> _worldAccessListeners = [];

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

    public  bool         IsClientSide   { get; set; }   // I
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

    /// <summary>Exposes the WorldProvider for dimension-specific checks (e.g. SleepingDisabled).</summary>
    public WorldProvider? WorldProvider => _worldProvider;

    /// <summary>
    /// obf: <c>v</c> — difficulty int (0=Peaceful, 1=Easy, 2=Normal, 3=Hard).
    /// Affects hunger drain, starvation damage, and mob spawn rates.
    /// Default: 2 (Normal).
    /// </summary>
    public int Difficulty { get; set; } = 2;

    // ── World spawn point (spec: v — dh spawn coordinate) ────────────────────

    /// <summary>obf: <c>v.a / .b / .c</c> — world spawn point coordinates.</summary>
    public int SpawnX { get; set; }
    public int SpawnY { get; set; } = 64;
    public int SpawnZ { get; set; }

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
        if (!IsInBounds(x, y, z)) return null;
        return GetChunkFromBlockCoords(x, z).GetTileEntity(x & 15, y, z & 15);
    }

    /// <summary>
    /// Sets (or replaces) a TileEntity at world position (x, y, z).
    /// Replica of <c>ry.a(bq te)</c> — used by piston extension to install a specific TE
    /// after placing the moving-block proxy (ID 36) via SetBlockAndMetadata.
    /// </summary>
    public void SetTileEntity(int x, int y, int z, TileEntity.TileEntity te)
    {
        if (!IsInBounds(x, y, z)) return;
        GetChunkFromBlockCoords(x, z).AddTileEntity(x & 15, y, z & 15, te);
    }

    /// <summary>
    /// Removes the TileEntity at world position (x, y, z) without removing the block.
    /// Replica of <c>ry.o(int x, int y, int z)</c> — used by TileEntityPiston finalisation.
    /// </summary>
    public void RemoveTileEntity(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return;
        GetChunkFromBlockCoords(x, z).RemoveTileEntity(x & 15, y, z & 15);
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
        if (oldId != blockId && oldId != 0)
            Block.BlocksList[oldId]?.OnBlockPreDestroy(this, x, y, z);
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
        if (oldId != blockId && oldId != 0)
            Block.BlocksList[oldId]?.OnBlockPreDestroy(this, x, y, z);
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
    /// Sets block metadata WITHOUT triggering neighbour notifications.
    /// Replica of <c>ry.f(x,y,z,meta)</c> — setMetadataWithoutUpdate.
    /// Used by redstone wire propagation during batch updates (spec §3.7).
    /// </summary>
    public void SetMetadataQuiet(int x, int y, int z, int meta)
    {
        if (!IsInBounds(x, y, z)) return;
        GetChunkFromBlockCoords(x, z).SetMetadata(x & 15, y, z & 15, meta);
    }

    /// <summary>
    /// Notifies one specific block at (x,y,z) that <paramref name="changedBlockId"/> changed nearby.
    /// Replica of <c>ry.j(x,y,z,changedBlockId)</c> — markBlockForUpdate / notifyBlock.
    /// Used by redstone blocks to trigger neighbour recalculation.
    /// </summary>
    public void NotifyBlock(int x, int y, int z, int changedBlockId)
    {
        if (!IsInBounds(x, y, z)) return;
        int id = GetBlockId(x, y, z);
        Block.BlocksList[id]?.OnNeighborBlockChange(this, x, y, z, changedBlockId);
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

    // ── IWorldAccess listener registration + dispatch (IWorldAccess_Spec.md) ──

    /// <summary>Registers a world-access listener. obf: <c>ry.a(bd)</c></summary>
    public void AddWorldAccess(IWorldAccess listener) => _worldAccessListeners.Add(listener);

    /// <summary>Removes a world-access listener. obf: <c>ry.b(bd)</c></summary>
    public void RemoveWorldAccess(IWorldAccess listener) => _worldAccessListeners.Remove(listener);

    /// <summary>Single-block invalidation. obf: <c>ry.j(x,y,z)</c></summary>
    public void NotifyBlockChange(int x, int y, int z)
    {
        foreach (var l in _worldAccessListeners) l.OnBlockChanged(x, y, z);
    }

    /// <summary>Region invalidation. obf: <c>ry.c(x1,y1,z1,x2,y2,z2)</c> (region overload)</summary>
    public void NotifyBlocksChanged(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        foreach (var l in _worldAccessListeners) l.OnBlockRangeChanged(x1, y1, z1, x2, y2, z2);
    }

    /// <summary>Spawn particle at world position. obf: <c>ry.a(String,double*3,double*3)</c></summary>
    public void SpawnParticle(string name, double x, double y, double z,
                               double velX, double velY, double velZ)
    {
        foreach (var l in _worldAccessListeners) l.SpawnParticle(name, x, y, z, velX, velY, velZ);
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

    /// <summary>Returns the (chunkX, chunkZ) of every currently loaded chunk.</summary>
    public IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords()
        => _chunkLoader.GetLoadedChunkCoords();

    /// <summary>obf: g(chunkX, chunkZ) → bool — is chunk currently loaded?</summary>
    public bool IsChunkLoaded(int chunkX, int chunkZ)
        => _chunkLoader.IsChunkLoaded(chunkX, chunkZ);

    // ── Entity management (spec §9) ───────────────────────────────────────────

    /// <summary>
    /// Adds entity to the world. Spec: <c>a(ia entity)</c> → bool.
    /// Players skip the chunk-loaded check (always added).
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

        foreach (var l in _worldAccessListeners) l.OnEntityAdded(entity);
    }

    /// <summary>
    /// Removes entity from the world and notifies IWorldAccess listeners. obf: <c>ry.d(ia)</c>
    /// </summary>
    public void DespawnEntity(Entity entity)
    {
        _loadedEntityList.Remove(entity);
        _playerList.Remove(entity);
        if (entity.AddedToChunk)
        {
            Chunk chunk = GetChunkFromBlockCoords((int)Math.Floor(entity.PosX),
                                                 (int)Math.Floor(entity.PosZ));
            chunk.RemoveEntity(entity);
        }
        foreach (var l in _worldAccessListeners) l.OnEntityRemoved(entity);
    }

    /// <summary>
    /// obf: <c>a(ia origin, String name, float volume, float pitch)</c> — plays a named sound
    /// at the entity's position. Stub: no-op until SoundManager is implemented.
    /// </summary>
    public void PlaySoundAt(Entity origin, string name, float volume, float pitch)
    {
        foreach (var l in _worldAccessListeners)
            l.PlaySound(name, origin.PosX, origin.PosY, origin.PosZ, volume, pitch);
    }

    /// <summary>
    /// obf: <c>a(c box, oi material)</c> — containsAnyLiquid / containsMaterial.
    /// Returns true if any block in the AABB matches the given material type.
    /// Stub: only checks for Water material via flood-fill scan of the box extents.
    /// </summary>
    public bool ContainsMaterial(AxisAlignedBB box, Material material)
    {
        int x0 = (int)Math.Floor(box.MinX);
        int y0 = (int)Math.Floor(box.MinY);
        int z0 = (int)Math.Floor(box.MinZ);
        int x1 = (int)Math.Ceiling(box.MaxX);
        int y1 = (int)Math.Ceiling(box.MaxY);
        int z1 = (int)Math.Ceiling(box.MaxZ);

        for (int x = x0; x < x1; x++)
        for (int y = y0; y < y1; y++)
        for (int z = z0; z < z1; z++)
        {
            int id = GetBlockId(x, y, z);
            if (id <= 0 || id >= Block.BlocksList.Length) continue;
            var block = Block.BlocksList[id];
            if (block != null && block.BlockMaterial == material)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Combined light level 0–15: max(sky − SkyDarkening, block). Spec: <c>world.getLightBrightness(x,y,z)</c>.
    /// </summary>
    public int GetLightBrightness(int x, int y, int z)
    {
        int sky   = ReadLightWithNeighborMax(LightType.Sky,   x, y, z) - SkyDarkening;
        int block = ReadLightWithNeighborMax(LightType.Block, x, y, z);
        return Math.Clamp(Math.Max(sky, block), 0, 15);
    }

    /// <summary>
    /// World event / auxiliary SFX. obf: <c>ry.a(String,int,int,int,int)</c>.
    /// Dispatches via <see cref="IWorldAccess.MarkBlocksDirty"/> with player=null and data packed
    /// into the second coord per spec. See SoundManager_Spec for event name table.
    /// </summary>
    public void PlayAuxSFX(EntityPlayer? player, int eventId, int x, int y, int z, int data)
    {
        foreach (var l in _worldAccessListeners)
            l.MarkBlocksDirty(player, x, y, z, x + data, y, z);
    }

    /// <summary>
    /// Plays a sound at world coordinates. obf: <c>ry.a(double,double,double,String,float,float)</c>.
    /// </summary>
    public void PlaySoundAtCoords(double x, double y, double z, string name, float volume, float pitch)
    {
        foreach (var l in _worldAccessListeners) l.PlaySound(name, x, y, z, volume, pitch);
    }

    // ── Redstone power query chain (spec: BlockRedstone_Spec §2) ─────────────

    /// <summary>
    /// obf: <c>ry.k(x,y,z,face)</c> — getStrongPower.
    /// Returns the strong power level provided by the block at (x,y,z) toward <paramref name="face"/>.
    /// Spec §2.
    /// </summary>
    public bool GetStrongPower(int x, int y, int z, int face)
    {
        int id = GetBlockId(x, y, z);
        if (id == 0) return false;
        return Block.BlocksList[id]?.IsProvidingStrongPower(this, x, y, z, face) ?? false;
    }

    /// <summary>
    /// obf: <c>ry.u(x,y,z)</c> — isStronglyPowered.
    /// True if any of the 6 adjacent faces provides strong power to (x,y,z).
    /// Spec §2.
    /// </summary>
    public bool IsStronglyPowered(int x, int y, int z)
        => GetStrongPower(x, y - 1, z, 1)   // block below powers face 1 (up)
        || GetStrongPower(x, y + 1, z, 0)    // block above powers face 0 (down)
        || GetStrongPower(x, y, z - 1, 2)    // south powers face 2 (+Z)
        || GetStrongPower(x, y, z + 1, 3)    // north powers face 3 (-Z)
        || GetStrongPower(x - 1, y, z, 4)    // east powers face 4 (+X)
        || GetStrongPower(x + 1, y, z, 5);   // west powers face 5 (-X)

    /// <summary>
    /// obf: <c>ry.l(x,y,z,face)</c> — getPower.
    /// For opaque cubes: delegates to <see cref="IsStronglyPowered"/>; else calls IsProvidingWeakPower.
    /// Spec §2.
    /// </summary>
    public bool GetPower(int x, int y, int z, int face)
    {
        int id = GetBlockId(x, y, z);
        if (id == 0) return false;
        Block? block = Block.BlocksList[id];
        if (block == null) return false;
        return block.IsOpaqueCube()
            ? IsStronglyPowered(x, y, z)
            : block.IsProvidingWeakPower(this, x, y, z, face);
    }

    /// <summary>
    /// obf: <c>ry.v(x,y,z)</c> — isBlockReceivingPower.
    /// True if any adjacent face provides power to (x,y,z). Used by wire + TNT.
    /// Spec §2.
    /// </summary>
    public bool IsBlockReceivingPower(int x, int y, int z)
        => GetPower(x, y - 1, z, 1)
        || GetPower(x, y + 1, z, 0)
        || GetPower(x, y, z - 1, 2)
        || GetPower(x, y, z + 1, 3)
        || GetPower(x - 1, y, z, 4)
        || GetPower(x + 1, y, z, 5);

    /// <summary>IWorld stub — delegates to <see cref="IsBlockReceivingPower"/>. Spec: <c>world.isBlockIndirectlyReceivingPower(x,y,z)</c>.</summary>
    public bool IsBlockIndirectlyReceivingPower(int x, int y, int z) => IsBlockReceivingPower(x, y, z);

    /// <summary>
    /// obf: <c>ry.g(x,y,z)</c> — isBlockNormalCube: true if block at (x,y,z) is a full
    /// opaque solid cube that can support placed blocks (torches, wire, plates, etc.).
    /// Used by redstone canBlockStay checks. Spec: BlockRedstone_Spec §3.3, §4.4, §5.5, etc.
    /// </summary>
    public bool IsBlockNormalCube(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        return id != 0 && Block.IsOpaqueCubeArr[id];
    }

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

    /// <summary>Returns all loaded players as EntityPlayer instances.</summary>
    public IReadOnlyList<Entity> GetPlayerList() => _playerList;

    // ── Mob spawning helpers (spec: SpawnerAnimals_Spec) ─────────────────────

    /// <summary>
    /// Counts all loaded entities whose type is assignable to <paramref name="baseType"/>.
    /// Spec: <c>world.b(jf.a())</c>.
    /// </summary>
    public int CountEntitiesOfType(Type baseType)
    {
        int count = 0;
        foreach (Entity e in _loadedEntityList)
            if (baseType.IsAssignableFrom(e.GetType())) count++;
        return count;
    }

    /// <summary>
    /// Returns the nearest EntityPlayer within <paramref name="range"/> blocks of (x,y,z),
    /// or null if none found. Spec: <c>world.a(float x, float y, float z, float range)</c>.
    /// </summary>
    public EntityPlayer? FindNearestPlayerWithinRange(double x, double y, double z, double range)
    {
        double r2 = range * range;
        EntityPlayer? nearest = null;
        double nearest2 = double.MaxValue;
        foreach (Entity e in _playerList)
        {
            if (e is not EntityPlayer ep) continue;
            double dx = ep.PosX - x, dy = ep.PosY - y, dz = ep.PosZ - z;
            double d2 = dx * dx + dy * dy + dz * dz;
            if (d2 <= r2 && d2 < nearest2) { nearest = ep; nearest2 = d2; }
        }
        return nearest;
    }

    /// <summary>
    /// Returns the biome's spawn list for the given creature type at world position (x,y,z).
    /// Returns null if the list is empty or the biome has no spawns for that type.
    /// Spec: <c>world.a(jf type, int x, int y, int z)</c>.
    /// </summary>
    public List<BiomeGenBase.SpawnListEntry>? GetSpawnableList(EnumCreatureType type, int x, int y, int z)
    {
        BiomeGenBase biome = ChunkManager != null
            ? ChunkManager.GetBiomeAt(x, z)
            : BiomeGenBase.Plains;
        var list = biome.GetSpawnList(type);
        return list.Count > 0 ? list : null;
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
        // 1. Advance world time (spec WorldServer §4.1 — running long, never wraps)
        _worldTime      += 1;
        _totalWorldTime += 1;
        if (WorldInfo != null) WorldInfo.Time = _worldTime;

        // 2. Weather tick (spec WorldServer §6)
        if (!IsClientSide)
        {
            TickWeather();

            // Auto-save every 40 ticks (spec §5.1) — saves WorldInfo (level.dat)
            if (_totalWorldTime % _autoSaveInterval == 0 && WorldInfo != null)
            {
                SaveHandler?.SaveLevelDat(WorldInfo);
            }
        }

        // 3. Recompute sky darkening for time-of-day / weather (spec: p())
        UpdateSkyDarkening();

        // 4. ChunkLoader tick

        // 5. Tick entities + tile entities (spec: m(), called once per game tick)
        TickEntities();

        // 6. Process scheduled block updates (up to 1000 per tick)
        ProcessScheduledTicks(false);

        // 7. Tick loaded chunks (random block ticks)
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
        // Snapshot due entries before any iteration — UpdateTick may call ScheduleBlockUpdate,
        // which would modify _scheduledUpdates and invalidate a live enumerator.
        var due = new List<ScheduledUpdate>();
        foreach (var entry in _scheduledUpdates)
        {
            if (!force && entry.FireTime > _totalWorldTime) break;
            if (due.Count >= 1000) break;
            due.Add(entry);
        }

        foreach (var entry in due)
        {
            _scheduledUpdates.Remove(entry);
            _scheduledSet.Remove((entry.X, entry.Y, entry.Z, entry.BlockId));

            if (!IsAreaLoaded(entry.X, entry.Y, entry.Z, 8)) continue;

            int id = GetBlockId(entry.X, entry.Y, entry.Z);
            if (id == entry.BlockId)
                Block.BlocksList[id]?.UpdateTick(this, entry.X, entry.Y, entry.Z, Random);
        }

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
        int best = Block.LightValueTable[blockId]; // self-emission (yy.q[id])
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
        NotifyBlocksChanged(x, minY, z, x, maxY, z);
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

    /// <summary>
    /// Moon phase index 0–7. Phase 0 = full moon, 4 = new moon.
    /// Spec: WorldServer §4.3 — <c>(worldTime / 24000) % 8</c>.
    /// Used by mob spawn rates and slime spawning.
    /// </summary>
    public int MoonPhase => (int)((_worldTime / 24000L) % 8L);

    /// <summary>
    /// Weather toggle + rain/thunder strength lerp. Spec: WorldServer §6.
    /// Runs server-side only; reads and writes WorldInfo rain/thunder state.
    /// </summary>
    private void TickWeather()
    {
        if (WorldInfo == null || WorldInfo.Hardcore) return;

        // ── Rain toggle (spec §6.1) ───────────────────────────────────────────
        WorldInfo.RainTime--;
        if (WorldInfo.RainTime <= 0)
        {
            WorldInfo.Raining = !WorldInfo.Raining;
            WorldInfo.RainTime = WorldInfo.Raining
                ? Random.NextInt(12000) + 3600   // 3–9 min of rain
                : Random.NextInt(168000) + 12000; // 10–150 min of clear
        }

        // ── Thunder toggle (spec §6.3) ────────────────────────────────────────
        WorldInfo.ThunderTime--;
        if (WorldInfo.ThunderTime <= 0)
        {
            WorldInfo.Thundering = !WorldInfo.Thundering;
            WorldInfo.ThunderTime = WorldInfo.Thundering
                ? Random.NextInt(12000) + 12000   // 10–20 min of thunder
                : Random.NextInt(168000) + 12000; // 10–150 min of calm
        }

        // ── Rain strength lerp (spec §6.2) ± 0.01F per tick ──────────────────
        _prevRainStrength = _rainStrength;
        _rainStrength = WorldInfo.Raining
            ? Math.Min(_rainStrength + 0.01f, 1.0f)
            : Math.Max(_rainStrength - 0.01f, 0.0f);

        // ── Thunder strength lerp ─────────────────────────────────────────────
        _prevThunderStrength = _thunderStrength;
        _thunderStrength = WorldInfo.Thundering
            ? Math.Min(_thunderStrength + 0.01f, 1.0f)
            : Math.Max(_thunderStrength - 0.01f, 0.0f);
    }

    // ── Weather queries (spec §15 / BlockFire_Spec §11) ──────────────────────

    /// <summary>
    /// True when it is raining (rain strength > 0.2).
    /// Spec: World_Spec §15 <c>E()</c> — <c>j(1.0F) > 0.2F</c>.
    /// </summary>
    public bool IsRaining() => _rainStrength > 0.2f;

    /// <summary>
    /// obf: <c>ry.p(x,y,z)</c> — canFreezeAtLocation.
    /// Returns true if still/flowing water (meta=0) at (x,y,z) should freeze to ice.
    /// Conditions: biome temp ≤ 0.15, block light &lt; 10. Spec: SnowIce_Spec §9.
    /// </summary>
    public bool CanFreezeAtLocation(int x, int y, int z)
    {
        if (ChunkManager == null) return false;
        double temp = ChunkManager.GetTemperatureAtHeight(x, y, z);
        if (temp > 0.15) return false;
        if (y < 0 || y >= WorldHeight) return false;
        int blockLight = GetLightBrightness(LightType.Block, x, y, z);
        if (blockLight >= 10) return false;
        int id = GetBlockId(x, y, z);
        if (id != 8 && id != 9) return false; // only water
        if (GetBlockMetadata(x, y, z) != 0) return false;
        return true;
    }

    /// <summary>
    /// obf: <c>ry.r(x,y,z)</c> — canSnowAtLocation.
    /// Returns true if an air block at (x,y,z) should receive a snow layer.
    /// Conditions: biome temp ≤ 0.15, block light &lt; 10, block below is solid (not ice).
    /// Spec: SnowIce_Spec §9.
    /// </summary>
    public bool CanSnowAtLocation(int x, int y, int z)
    {
        if (ChunkManager == null) return false;
        double temp = ChunkManager.GetTemperatureAtHeight(x, y, z);
        if (temp > 0.15) return false;
        if (y < 0 || y >= WorldHeight) return false;
        int blockLight = GetLightBrightness(LightType.Block, x, y, z);
        if (blockLight >= 10) return false;
        int current = GetBlockId(x, y, z);
        if (current != 0) return false; // must be air
        int below = GetBlockId(x, y - 1, z);
        if (below == 0) return false;     // nothing below
        if (below == 79) return false;    // no snow on ice (quirk 4)
        if (!Block.IsOpaqueCubeArr[below]) return false;
        return GetBlockMaterial(x, y - 1, z).IsSolid();
    }

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

        // Notify render listeners (IWorldAccess_Spec.md)
        NotifyBlockChange(x, y, z);
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

    // ── Sleep / bed helpers (BlockBed_Spec §12, §17) ─────────────────────────

    /// <summary>
    /// obf: <c>ry.l()</c> — isDaytime = skyDarkening &lt; 4.
    /// Sleep is only possible when this returns false (k >= 4 = dark enough).
    /// Spec: BlockBed_Spec §17.
    /// </summary>
    public bool IsDaytime() => SkyDarkening < 4;

    /// <summary>
    /// obf: <c>ry.A()</c> — checkAllPlayersSleeping stub.
    /// When all players are sleeping this would advance time to dawn and wake everyone.
    /// Full implementation pending (requires time-skip and wake-all-players logic).
    /// </summary>
    public void CheckAllPlayersSleeping() { /* stub — Explosion/WorldTick spec pending */ }

    /// <summary>
    /// Checks whether any <see cref="EntityMonster"/> is within the bed safety radius
    /// (±8 XZ, ±5 Y) of the given bed position. Spec: BlockBed_Spec §12.1.e.
    /// </summary>
    public bool HasMonstersNearBed(int bedX, int bedY, int bedZ)
    {
        double minX = bedX - 8, maxX = bedX + 8;
        double minY = bedY - 5, maxY = bedY + 5;
        double minZ = bedZ - 8, maxZ = bedZ + 8;
        foreach (Entity e in _loadedEntityList)
        {
            if (e is not EntityMonster) continue;
            if (e.PosX >= minX && e.PosX <= maxX &&
                e.PosY >= minY && e.PosY <= maxY &&
                e.PosZ >= minZ && e.PosZ <= maxZ)
                return true;
        }
        return false;
    }

    // ── Explosion helpers (Explosion_Spec §4, §5) ────────────────────────────

    /// <summary>
    /// obf: <c>ry.a(ia exclude, AxisAlignedBB box)</c> — returns all entities within
    /// the given AABB, excluding <paramref name="exclude"/> (may be null).
    /// Used by <see cref="Explosion"/> to query entities for damage.
    /// </summary>
    public List<Entity> GetEntitiesWithinAABBExcluding(Entity? exclude, AxisAlignedBB box)
    {
        var result = new List<Entity>();
        foreach (Entity e in _loadedEntityList)
        {
            if (e == exclude || e.IsDead) continue;
            if (e.BoundingBox.MaxX >= box.MinX && e.BoundingBox.MinX <= box.MaxX &&
                e.BoundingBox.MaxY >= box.MinY && e.BoundingBox.MinY <= box.MaxY &&
                e.BoundingBox.MaxZ >= box.MinZ && e.BoundingBox.MinZ <= box.MaxZ)
                result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// obf: <c>ry.a(Vec3 start, Vec3 end)</c> — ray-trace through blocks.
    /// Returns null if the ray reaches <paramref name="end"/> without hitting an opaque block.
    /// Used by <see cref="Explosion.GetExposure"/> for line-of-sight checks.
    /// Spec: Explosion_Spec §5.
    /// </summary>
    public MovingObjectPosition? RayTraceBlocks(Vec3 start, Vec3 end)
    {
        // Walk the ray in small steps along each axis using DDA traversal
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double dz = end.Z - start.Z;
        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist < 1e-4) return null;

        // Walk in steps of at most 0.5 blocks
        int steps = (int)(dist / 0.5) + 2;
        double stepX = dx / steps;
        double stepY = dy / steps;
        double stepZ = dz / steps;

        int prevBx = int.MinValue, prevBy = int.MinValue, prevBz = int.MinValue;
        for (int i = 0; i <= steps; i++)
        {
            double rx = start.X + stepX * i;
            double ry = start.Y + stepY * i;
            double rz = start.Z + stepZ * i;
            int bx = (int)Math.Floor(rx);
            int by = (int)Math.Floor(ry);
            int bz = (int)Math.Floor(rz);
            if (bx == prevBx && by == prevBy && bz == prevBz) continue;
            prevBx = bx; prevBy = by; prevBz = bz;
            if (Block.IsOpaqueCubeArr[GetBlockId(bx, by, bz)])
                return new MovingObjectPosition(bx, by, bz, 0, start);
        }
        return null;
    }

    /// <summary>
    /// obf: <c>ry.a(Vec3 origin, AxisAlignedBB entityBounds)</c> — exposure fraction.
    /// Samples a grid of rays from <paramref name="origin"/> to points on <paramref name="entityBounds"/>.
    /// Returns [0, 1] fraction of rays that reach without hitting an opaque block.
    /// Spec: Explosion_Spec §5.
    /// </summary>
    public float GetExplosionExposure(Vec3 origin, AxisAlignedBB entityBounds)
    {
        double sizeX = entityBounds.MaxX - entityBounds.MinX;
        double sizeY = entityBounds.MaxY - entityBounds.MinY;
        double sizeZ = entityBounds.MaxZ - entityBounds.MinZ;

        double stepX = 1.0 / (sizeX * 2.0 + 1.0);
        double stepY = 1.0 / (sizeY * 2.0 + 1.0);
        double stepZ = 1.0 / (sizeZ * 2.0 + 1.0);

        int total = 0, hits = 0;
        for (double tx = 0.0; tx <= 1.0; tx += stepX)
        for (double ty = 0.0; ty <= 1.0; ty += stepY)
        for (double tz = 0.0; tz <= 1.0; tz += stepZ)
        {
            double px = entityBounds.MinX + sizeX * tx;
            double py = entityBounds.MinY + sizeY * ty;
            double pz = entityBounds.MinZ + sizeZ * tz;
            if (RayTraceBlocks(Vec3.GetFromPool(px, py, pz), origin) == null)
                hits++;
            total++;
        }
        return total == 0 ? 0f : (float)hits / total;
    }

    // ── Pathfinding entry points (spec MobAI_PathFinder_Spec §4) ─────────────

    /// <summary>
    /// obf: <c>ry.a(ia entity, ia target, float range)</c> — request A* path to a target entity.
    /// Builds a ChunkCache snapshot and delegates to <see cref="AI.PathFinder"/>.
    /// margin = (int)(range + 16)
    /// </summary>
    public AI.PathEntity? GetPathToEntity(Entity entity, Entity target, float range)
    {
        int ex = (int)Math.Floor(entity.PosX);
        int ey = (int)Math.Floor(entity.PosY);
        int ez = (int)Math.Floor(entity.PosZ);
        int margin = (int)(range + 16);

        var cache = new AI.ChunkCache(this,
            ex - margin, ey - margin, ez - margin,
            ex + margin, ey + margin, ez + margin);

        return new AI.PathFinder(cache).FindPath(entity, target, range);
    }

    /// <summary>
    /// obf: <c>ry.a(ia entity, int x, int y, int z, float range)</c> — request A* path
    /// to a fixed coordinate (used by stroll).
    /// margin = (int)(range + 8)
    /// </summary>
    public AI.PathEntity? GetPathToCoords(Entity entity, int x, int y, int z, float range)
    {
        int ex = (int)Math.Floor(entity.PosX);
        int ey = (int)Math.Floor(entity.PosY);
        int ez = (int)Math.Floor(entity.PosZ);
        int margin = (int)(range + 8);

        var cache = new AI.ChunkCache(this,
            ex - margin, ey - margin, ez - margin,
            ex + margin, ey + margin, ez + margin);

        return new AI.PathFinder(cache).FindPath(entity, x, y, z, range);
    }

    /// <summary>
    /// obf: <c>ry.b(ia entity, double range)</c> — returns the nearest living EntityPlayer
    /// that is not in creative mode and is within <paramref name="range"/> blocks.
    /// Returns null if no such player exists.
    /// </summary>
    public EntityPlayer? GetClosestPlayer(Entity entity, double range)
    {
        double bestDist = range * range; // compare squared distances
        EntityPlayer? best = null;

        foreach (Entity e in _playerList)
        {
            if (e is not EntityPlayer player || player.IsDead) continue;

            double dist2 = entity.SquaredDistanceTo(player.PosX, player.PosY, player.PosZ);
            if (dist2 <= bestDist)
            {
                bestDist = dist2;
                best = player;
            }
        }

        return best;
    }

    /// <summary>
    /// obf: <c>ry.a(ia entity, double range)</c> — returns the nearest EntityPlayer that
    /// can be targeted (alive, not immune). Same as <see cref="GetClosestPlayer"/> for
    /// 1.0 (no creative mode).
    /// </summary>
    public EntityPlayer? GetClosestVulnerablePlayer(Entity entity, double range)
        => GetClosestPlayer(entity, range);

    /// <summary>
    /// Returns all entities of type <typeparamref name="T"/> whose bounding boxes intersect
    /// the given AABB. Used by animal AI (breed partner / baby scan).
    /// </summary>
    public List<T> GetEntitiesWithinAABB<T>(AxisAlignedBB box) where T : Entity
    {
        var result = new List<T>();
        foreach (Entity e in _loadedEntityList)
        {
            if (e is not T typed || e.IsDead) continue;
            if (e.BoundingBox.MaxX >= box.MinX && e.BoundingBox.MinX <= box.MaxX &&
                e.BoundingBox.MaxY >= box.MinY && e.BoundingBox.MinY <= box.MaxY &&
                e.BoundingBox.MaxZ >= box.MinZ && e.BoundingBox.MinZ <= box.MaxZ)
                result.Add(typed);
        }
        return result;
    }

    /// <summary>
    /// obf: <c>ry.a(ia entity, double x, double y, double z, float power)</c> — creates
    /// and immediately executes an explosion at the given position.
    /// Spec: Explosion_Spec §1, §4, §6.
    /// </summary>
    public void CreateExplosion(EntityPlayer? player, double x, double y, double z, float power, bool isIncendiary)
    {
        var explosion = new Explosion(this, player, x, y, z, power, isIncendiary);
        explosion.ComputeAffectedBlocksAndDamageEntities();
        explosion.DestroyBlocksAndSpawnParticles(doParticles: !IsClientSide);
    }

    // ── World Spawn (spec: WorldSpawn_Spec.md) ────────────────────────────────

    /// <summary>
    /// Finds a valid surface spawn point near (startX, startZ) using the biome-walk algorithm.
    /// Spec: <c>si</c> spawn search: Phase 1 biome radius 256, Phase 2 up to 1000 random-walk attempts.
    ///
    /// Sets <see cref="SpawnX"/>, <see cref="SpawnY"/>, <see cref="SpawnZ"/>.
    /// </summary>
    public void FindSpawnPoint(int startX, int startZ)
    {
        // Phase 1: biome-valid search within radius 256 (stub: skip biome check, accept start)
        int candidateX = startX;
        int candidateZ = startZ;

        // Phase 2: up to 1000 random-walk attempts to find a non-air surface block
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int surfaceY = GetTopSolidOrLiquidBlock(candidateX, candidateZ);
            if (surfaceY > 0)
            {
                SpawnX = candidateX;
                SpawnY = surfaceY;
                SpawnZ = candidateZ;
                return;
            }

            // Walk within ±63 in X and Z
            candidateX = startX + Random.NextInt(64) - Random.NextInt(64);
            candidateZ = startZ + Random.NextInt(64) - Random.NextInt(64);
        }

        // Fallback: use whatever surface exists at start coords
        SpawnX = startX;
        SpawnY = Math.Max(64, GetTopSolidOrLiquidBlock(startX, startZ));
        SpawnZ = startZ;
    }

    /// <summary>
    /// Forces the 5×5 chunk area around the player's position to stay loaded.
    /// Spec: <c>ry.g(vi player)</c> — called every 30 ticks.
    /// 5×5 = 25 chunks centred on the player's chunk.
    /// </summary>
    public void EnsureChunksAroundPlayer(EntityPlayer player)
    {
        int cx = (int)Math.Floor(player.PosX) >> 4;
        int cz = (int)Math.Floor(player.PosZ) >> 4;

        for (int dx = -2; dx <= 2; dx++)
        for (int dz = -2; dz <= 2; dz++)
            _chunkLoader.GetChunk(cx + dx, cz + dz);
    }

    /// <summary>
    /// Pre-loads spawn chunks before the player enters the world.
    /// Survival: 17×17 (radius 8 = 128 blocks).
    /// Creative:  9×9  (radius 4 =  64 blocks).
    /// Spec: WorldSpawn_Spec.md §4.
    /// </summary>
    public void PreloadSpawnChunks(bool isCreative)
    {
        int radius = isCreative ? 4 : 8;
        int spawnCx = SpawnX >> 4;
        int spawnCz = SpawnZ >> 4;

        for (int dx = -radius; dx <= radius; dx++)
        for (int dz = -radius; dz <= radius; dz++)
            _chunkLoader.GetChunk(spawnCx + dx, spawnCz + dz);
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
