using System.Collections.Generic;

namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>zx</c> (Chunk) — stores block IDs, metadata, light values, height map,
/// tile entities, and entities for one 16 × 128 × 16 column.
///
/// Block array index:  <c>(localX &lt;&lt; 11) | (localZ &lt;&lt; 7) | localY</c>
/// Height map entry:   <c>j[z&lt;&lt;4 | x] &amp; 255</c> = lowest Y where LightOpacity[id] ≠ 0
///
/// Quirks preserved (see spec §15):
///   1. SetBlock 5-arg writes metadata nibble TWICE — before AND after MarkDirtyColumn.
///   2. Height map stored as signed byte, always read with &amp; 255.
///   3. PrecipitationHeightAt returns −1 when no solid/liquid block is found from top.
///
/// Stubs (pending specs):
///   - Entity bucket management (ia spec pending)
///   - TileEntity map operations (bq, ba specs pending)
///   - Full sky-light BFS propagation (delegates to World, which stubs it)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Chunk_Spec.md
/// </summary>
public class Chunk
{
    // ── World-geometry constants for a 128-high world (from World_Spec.md §2) ─

    internal const int HeightBits  = 7;    // world.a = log₂(128)
    internal const int XShift      = 11;   // world.b = HeightBits + 4
    internal const int WorldHeight = 128;  // world.c
    internal const int HeightMask  = 127;  // world.d
    private  const int EntityBuckets = 8; // world.c / 16 = 8

    private const int BlockArraySize = 16 * WorldHeight * 16; // 32768

    // ── Static field (spec §2) ────────────────────────────────────────────────

    /// <summary>
    /// obf: a (static) — set true when any sky-light &gt; 0 is detected during
    /// <see cref="GetLightSubtracted"/>. Read by World lighting code.
    /// </summary>
    public static bool AnySkylightPresent; // obf: a

    // ── Instance fields (spec §2) ─────────────────────────────────────────────

    private byte[]      _blockIds;        // obf: b — 32768 block ID bytes
    private int[]       _precipCache;     // obf: c[256] — −999 = stale
    private bool[]      _dirtyColumns;    // obf: d[256] — dirty XZ column flags
    public  bool        IsLoaded;         // obf: e
    public  readonly World  World;        // obf: f
    private NibbleArray? _metadata;        // obf: g — 4-bit block metadata (null until 4-arg ctor)
    private NibbleArray? _skyLight;        // obf: h — 4-bit sky-light (null until 4-arg ctor)
    private NibbleArray? _blockLight;      // obf: i — 4-bit block-light (null until 4-arg ctor)
    private byte[]      _heightMap;       // obf: j[256] — signed byte, read with &255
    public  int         LowestHeightInChunk; // obf: k
    public  readonly int ChunkX;         // obf: l (final)
    public  readonly int ChunkZ;         // obf: m (final)
    // n: Map<am,bq> tile entity map — stub (bq spec pending)
    // o: List<ia>[] entity buckets   — stub (ia spec pending)
    public  bool        IsPopulated;      // obf: p — terrain features generated
    public  bool        IsModified;       // obf: q — dirty flag
    public  bool        IsLightPopulated; // obf: r — sky-light computed
    public  bool        HasEntities;      // obf: s
    public  long        LastSaveTime;     // obf: t — world-time at last save
#pragma warning disable CS0169 // spec-required field — purpose TBD
    private bool        _u;               // obf: u — purpose TBD
#pragma warning restore CS0169
    private bool        _hasDirtyColumns; // obf: v

    // ── Constructors (spec §6) ────────────────────────────────────────────────

    /// <summary>
    /// 3-arg constructor — empty chunk, no block data yet. Spec: <c>zx(ry, int, int)</c>.
    /// Allocates structural arrays; block array is zeroed (all air).
    /// </summary>
    public Chunk(World world, int chunkX, int chunkZ)
    {
        World  = world;
        ChunkX = chunkX;
        ChunkZ = chunkZ;

        _blockIds     = new byte[BlockArraySize];
        _heightMap    = new byte[256];
        _precipCache  = new int[256];
        _dirtyColumns = new bool[256];

        for (int i = 0; i < 256; i++) _precipCache[i] = -999;

        // Entity bucket lists — stub (ia spec pending)
        // TileEntity map     — stub (bq spec pending)
    }

    /// <summary>
    /// 4-arg constructor — chunk loaded from storage. Spec: <c>zx(ry, byte[], int, int)</c>.
    /// Delegates to 3-arg, then attaches the provided block data and allocates nibble arrays.
    /// </summary>
    public Chunk(World world, byte[] blockData, int chunkX, int chunkZ)
        : this(world, chunkX, chunkZ)
    {
        _blockIds   = blockData;
        _metadata   = new NibbleArray(blockData.Length, HeightBits);
        _skyLight   = new NibbleArray(blockData.Length, HeightBits);
        _blockLight = new NibbleArray(blockData.Length, HeightBits);
    }

    // ── Block access (spec §7) ────────────────────────────────────────────────

    /// <summary>
    /// Block ID at chunk-local (x, y, z). Spec: <c>a(int, int, int)</c> → int.
    /// Index: <c>(x &lt;&lt; 11) | (z &lt;&lt; 7) | y</c>.
    /// </summary>
    public int GetBlockId(int x, int y, int z)
        => _blockIds[(x << XShift) | (z << HeightBits) | y] & 0xFF;

    /// <summary>
    /// Sets block ID and metadata, updates height map and marks the column dirty.
    /// Returns false if nothing changed. Spec: <c>a(int, int, int, int, int)</c> → bool.
    ///
    /// Quirk 1: metadata nibble write called TWICE — once before, once after MarkDirtyColumn.
    /// </summary>
    public bool SetBlock(int x, int y, int z, int blockId, int meta)
    {
        int index   = (x << XShift) | (z << HeightBits) | y;
        int oldId   = _blockIds[index] & 0xFF;
        int oldMeta = _metadata?.Get(x, y, z) ?? 0;

        if (oldId == blockId && oldMeta == meta) return false;

        // Invalidate precipitation cache if affected Y range
        int cacheKey = (z << 4) | x;
        if (y >= (_precipCache[cacheKey] - 1)) _precipCache[cacheKey] = -999;

        _blockIds[index] = (byte)blockId;

        // Block-added / block-removed callbacks — stub (needs World event dispatch)

        // Step 5 (quirk 1 — first metadata write)
        _metadata?.Set(x, y, z, meta);

        // Update height map
        UpdateHeightMapAt(x, y, z, blockId);

        // Light propagation — delegated to World (World.PropagateLight stubs BFS)
        // World.PropagateLight(LightType.Sky,   worldX, y, worldZ)  ← called by World.SetBlock
        // World.PropagateLight(LightType.Block, worldX, y, worldZ)  ← called by World.SetBlock

        // Mark XZ column dirty
        MarkDirtyColumn(x, z);

        // Step 9 (quirk 1 — second metadata write, redundant but spec-required)
        _metadata?.Set(x, y, z, meta);

        // TileEntity creation/removal — stub (ba/bq spec pending)

        IsModified = true;
        return true;
    }

    /// <summary>
    /// Sets block ID only; metadata cleared to 0. Spec: <c>a(int, int, int, int)</c> → bool.
    /// </summary>
    public bool SetBlock(int x, int y, int z, int blockId)
        => SetBlock(x, y, z, blockId, 0);

    /// <summary>Block metadata at chunk-local (x, y, z). Spec: <c>b(int, int, int)</c> → int.</summary>
    public int GetMetadata(int x, int y, int z)
        => _metadata?.Get(x, y, z) ?? 0;

    /// <summary>
    /// Sets block metadata. Returns false if unchanged. Spec: <c>b(int, int, int, int)</c> → bool.
    /// </summary>
    public bool SetMetadata(int x, int y, int z, int meta)
    {
        if (_metadata == null || _metadata.Get(x, y, z) == meta) return false;
        _metadata.Set(x, y, z, meta);
        IsModified = true;
        return true;
    }

    // ── Light access (spec §8) ────────────────────────────────────────────────

    /// <summary>
    /// Read light value of given type at chunk-local (x, y, z). Spec: <c>a(bn, int, int, int)</c> → int.
    /// </summary>
    public int GetLight(LightType type, int x, int y, int z) => type switch
    {
        LightType.Sky   => _skyLight?.Get(x, y, z)   ?? 0,
        LightType.Block => _blockLight?.Get(x, y, z) ?? 0,
        _               => 0,
    };

    /// <summary>
    /// Write light value. Sky-light is skipped in the Nether. Spec: <c>a(bn, int, int, int, int)</c>.
    /// </summary>
    public void SetLight(LightType type, int x, int y, int z, int value)
    {
        if (type == LightType.Sky)
        {
            if (World.IsNether) return;
            _skyLight?.Set(x, y, z, value);
        }
        else if (type == LightType.Block)
        {
            _blockLight?.Set(x, y, z, value);
        }
    }

    /// <summary>
    /// Combined light at position, subtracting <paramref name="subtraction"/> from sky-light.
    /// Sets <see cref="AnySkylightPresent"/> if sky &gt; 0. Spec: <c>c(int, int, int, int)</c> → int.
    /// </summary>
    public int GetLightSubtracted(int x, int y, int z, int subtraction)
    {
        int sky = World.IsNether ? 0 : (_skyLight?.Get(x, y, z) ?? 0);
        if (sky > 0) AnySkylightPresent = true;
        sky -= subtraction;
        int block = _blockLight?.Get(x, y, z) ?? 0;
        return Math.Max(sky, block);
    }

    // ── Height map (spec §9) ──────────────────────────────────────────────────

    /// <summary>
    /// Top solid-block Y+1 at column (x, z). Spec: <c>b(int, int)</c> → int.
    /// Stored as signed byte, read with &amp; 255 (quirk 2).
    /// </summary>
    public int GetHeightAt(int x, int z)
        => _heightMap[(z << 4) | x] & 0xFF;

    /// <summary>
    /// True if y ≥ height-map value at (x, z) — i.e., block is at or above the sky-visible level.
    /// Spec: <c>c(int, int, int)</c> → bool.
    /// </summary>
    public bool IsAboveHeightMap(int x, int y, int z)
        => y >= (_heightMap[(z << 4) | x] & 0xFF);

    /// <summary>
    /// Returns the precipitation height (top solid-or-liquid Y+1) at column (x, z), cached.
    /// Returns −1 if no solid/liquid block found searching from the top (quirk 3).
    /// Spec: <c>c(int, int)</c> → int.
    /// </summary>
    public int PrecipitationHeightAt(int x, int z)
    {
        int key = x | (z << 4);
        if (_precipCache[key] != -999) return _precipCache[key];

        for (int y = WorldHeight - 1; y >= 0; y--)
        {
            int id = GetBlockId(x, y, z);
            if (id == 0) continue;
            Block? block = Block.BlocksList[id];
            if (block == null) continue;
            Material mat = block.BlockMaterial ?? Material.Air;
            if (mat.BlocksMovement() || mat.IsLiquid())
            {
                _precipCache[key] = y + 1;
                return y + 1;
            }
        }
        _precipCache[key] = -1;
        return -1;
    }

    // ── Lifecycle (spec §12) ──────────────────────────────────────────────────

    /// <summary>Called when the chunk is loaded into the world. Spec: <c>e()</c>.</summary>
    public void OnChunkLoad()
    {
        IsLoaded = true;
        // Add tile entities to world.h and entities to world.g — stub (bq/ia spec pending)
    }

    /// <summary>Called when the chunk is unloaded from the world. Spec: <c>f()</c>.</summary>
    public void OnChunkUnload()
    {
        IsLoaded = false;
        // Queue tile entity / entity removal — stub
    }

    /// <summary>Marks this chunk dirty (forces save). Spec: <c>g()</c>.</summary>
    public void MarkDirty() => IsModified = true;

    /// <summary>
    /// Recomputes height map for all 256 XZ columns (no sky-light). Spec: <c>b()</c>.
    /// </summary>
    public void GenerateHeightMap()
    {
        int min = int.MaxValue;
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        {
            int y = WorldHeight;
            while (y > 0 && Block.LightOpacity[GetBlockId(x, y - 1, z)] == 0) y--;
            _heightMap[(z << 4) | x] = (byte)y;  // stored as signed byte (quirk 2)
            if (y < min) min = y;
        }
        LowestHeightInChunk = min == int.MaxValue ? 0 : min;
    }

    /// <summary>
    /// Recomputes height map AND marks all XZ columns dirty for sky-light propagation.
    /// Sky-light BFS deferred to World. Spec: <c>c()</c>.
    /// </summary>
    public void GenerateSkylightMap()
    {
        GenerateHeightMap();
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
            MarkDirtyColumn(x, z);
        IsModified = true;
    }

    /// <summary>
    /// Processes dirty sky-light columns if any exist. Spec: <c>j()</c>.
    /// Sky-light BFS deferred — dirty flags cleared, full propagation pending.
    /// </summary>
    public void UpdateSkylight()
    {
        if (!_hasDirtyColumns || World.IsNether) return;
        _hasDirtyColumns = false;
        for (int i = 0; i < 256; i++) _dirtyColumns[i] = false;
        // Full sky-light column propagation deferred — needs World.PropagateLight BFS
    }

    // ── Serialisation (spec §13) ──────────────────────────────────────────────

    /// <summary>
    /// True if this chunk should be persisted to disk. Spec: <c>a(bool forceCheck)</c> → bool.
    /// </summary>
    public bool NeedsSaving(bool forceCheck)
    {
        if (!IsLightPopulated) return false;
        long now = World.TotalWorldTime;
        return forceCheck
            ? (HasEntities && now != LastSaveTime) || IsModified
            : (HasEntities && now >= LastSaveTime + 600) || IsModified;
    }

    /// <summary>
    /// Returns a deterministic chunk-local Random seeded from the world seed and chunk coords.
    /// Spec: <c>a(long seed)</c> → Random.
    /// </summary>
    public JavaRandom GetChunkRandom(long seed)
    {
        long ws   = World.WorldSeed;
        long s    = ws
            + (long)ChunkX * ChunkX * 4987142L
            + (long)ChunkX * 5947611L
            + (long)ChunkZ * ChunkZ * 4392871L
            + (long)ChunkZ * 389711L
            ^ seed;
        var rng = new JavaRandom();
        rng.SetSeed(s);
        return rng;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void MarkDirtyColumn(int x, int z)
    {
        _dirtyColumns[x + z * 16] = true;
        _hasDirtyColumns = true;
    }

    private void UpdateHeightMapAt(int x, int y, int z, int newBlockId)
    {
        int key       = (z << 4) | x;
        int oldHeight = _heightMap[key] & 0xFF;

        if (newBlockId != 0 && Block.LightOpacity[newBlockId] != 0 && y >= oldHeight)
        {
            // New opaque block raises the height map
            _heightMap[key] = (byte)(y + 1);
            if (y + 1 < LowestHeightInChunk) LowestHeightInChunk = y + 1;
        }
        else if (newBlockId == 0 && y == oldHeight - 1)
        {
            // Block removed at the top — search downward for new top
            int search = y;
            while (search > 0 && Block.LightOpacity[GetBlockId(x, search - 1, z)] == 0)
                search--;
            _heightMap[key] = (byte)search;
            if (search < LowestHeightInChunk) LowestHeightInChunk = search;
        }
    }
}
