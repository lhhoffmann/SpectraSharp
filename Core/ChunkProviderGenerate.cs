using SpectraEngine.Core.WorldGen;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>xj</c> (ChunkProviderGenerate) — Overworld procedural terrain generator.
/// Implements <see cref="IChunkLoader"/> so it can be passed directly to <see cref="World"/>.
///
/// Two-pass generation (spec §5):
///   Pass 1 — 3D density noise → stone / water column
///   Pass 2 — Surface pass → replace top layers with biome-specific blocks
///
/// Carvers: caves (MapGenCaves) and ravines (MapGenRavine) run between passes 1 and 2.
/// Structure generation (villages, strongholds) is not implemented.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderGenerate_Spec.md
/// </summary>
public class ChunkProviderGenerate : IChunkLoader
{
    // ── Fields (spec §3) ──────────────────────────────────────────────────────

    private readonly JavaRandom _rand;              // n
    private readonly NoiseGeneratorOctaves _noiseA; // o — 16 oct, 3D density A
    private readonly NoiseGeneratorOctaves _noiseB; // p — 16 oct, 3D density B
    private readonly NoiseGeneratorOctaves _noiseQ; // q —  8 oct, 3D selector
    private readonly NoiseGeneratorOctaves _noiseR; // r —  4 oct, surface (dirt depth)
    private readonly NoiseGeneratorOctaves _noiseBlend; // a — 10 oct, 2D biome blend
    private readonly NoiseGeneratorOctaves _noiseHill;  // b — 16 oct, 2D hill factor

    private World _world;
    private readonly long  _worldSeed;
    private readonly bool  _generateStructures; // t

    private const int SeaLevel = 64;

    // Working array for the 5×17×5 density grid (425 entries)
    private double[]? _densityBuf;

    // Chunk cache
    private readonly Dictionary<long, Chunk> _chunks = new();

    // Re-entrancy guard: true while a chunk is being generated.
    // Adjacent-chunk block reads during ore placement return an empty placeholder
    // instead of recursively triggering generation — matches vanilla's unloaded-chunk behaviour.
    private bool _isGenerating;

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    /// <param name="world">
    /// Optional at construction; set via <see cref="SetWorld"/> after the World is created
    /// if a chicken-and-egg dependency would otherwise occur.
    /// </param>
    public ChunkProviderGenerate(long seed = 0L, bool generateStructures = false, World? world = null)
    {
        _world              = world!;
        _worldSeed          = seed;
        _generateStructures = generateStructures;

        _rand = new JavaRandom(seed);
        _noiseA     = new NoiseGeneratorOctaves(_rand, 16);
        _noiseB     = new NoiseGeneratorOctaves(_rand, 16);
        _noiseQ     = new NoiseGeneratorOctaves(_rand, 8);
        _noiseR     = new NoiseGeneratorOctaves(_rand, 4);
        _noiseBlend = new NoiseGeneratorOctaves(_rand, 10);
        _noiseHill  = new NoiseGeneratorOctaves(_rand, 16);
        // c field (8 oct, unused) — consume from rand to stay in sync with the original
        _ = new NoiseGeneratorOctaves(_rand, 8);

        _mineshaft .SetWorldSeed(seed);
        _village   .SetWorldSeed(seed);
        _stronghold.SetWorldSeed(seed);
    }

    /// <summary>Sets the world reference after construction. Call before the first chunk is requested.</summary>
    public virtual void SetWorld(World world) => _world = world;

    // ── IChunkLoader ──────────────────────────────────────────────────────────

    public virtual Chunk GetChunk(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (_chunks.TryGetValue(key, out Chunk? existing)) return existing;

        // During ore placement, WorldGenMineable may call world.GetBlockId() on an adjacent
        // chunk that is not yet in the cache.  Generating it recursively would cause infinite
        // recursion because that chunk's own populate would again hit unloaded neighbours.
        // Instead, return an empty placeholder (all air → stone check fails → no ore placed
        // in that region).  The real chunk is generated on the next explicit request.
        if (_isGenerating) return new Chunk(_world, chunkX, chunkZ);

        _isGenerating = true;
        try
        {
            var chunk = GenerateChunkTerrain(chunkX, chunkZ);
            _chunks[key] = chunk;      // store before populate so self-access hits cache
            PopulateChunk(chunkX, chunkZ);
            return chunk;
        }
        finally
        {
            _isGenerating = false;
        }
    }

    public bool IsChunkLoaded(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        return _chunks.ContainsKey(key);
    }

    public virtual void Tick() { }

    public IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords()
    {
        foreach (long key in _chunks.Keys)
            yield return ((int)(key >> 32), (int)key);
    }

    // ── Cave and ravine carvers (spec §5, §8 step 1) ─────────────────────────

    private readonly MapGenCaves  _caveGen   = new();
    private readonly MapGenRavine _ravineGen = new();

    // ── Structure generators (WorldGenStructures_Spec §1) ────────────────────
    private readonly MapGenMineshaft  _mineshaft  = new();
    private readonly MapGenVillage    _village    = new();
    private readonly MapGenStronghold _stronghold = new();

    // ── Ore generators (BiomeDecorator §4 step 1) ────────────────────────────

    private static readonly WorldGenMineable _genDirt      = new(3,  32);  // i: dirt patches
    private static readonly WorldGenMineable _genGravel    = new(13, 32);  // j: gravel patches
    private static readonly WorldGenMineable _genCoal      = new(16, 16);  // k: coal ore
    private static readonly WorldGenMineable _genIron      = new(15,  8);  // l: iron ore
    private static readonly WorldGenMineable _genGold      = new(14,  8);  // m: gold ore
    private static readonly WorldGenMineable _genRedstone  = new(73,  7);  // n: redstone ore
    private static readonly WorldGenMineable _genDiamond   = new(56,  7);  // o: diamond ore
    private static readonly WorldGenMineable _genLapis     = new(21,  6);  // p: lapis lazuli ore

    // ── Decoration generators (BiomeDecorator §2, shared instances) ──────────

    private static readonly WorldGenSandDisc   _genSand        = new(7, 12);   // g: sand disc (ID 12)
    private static readonly WorldGenClay       _genClay        = new(4);       // f: clay disc
    private static readonly WorldGenFlowers    _genDandelion   = new(37);      // q: dandelion
    private static readonly WorldGenFlowers    _genRose        = new(38);      // r: rose
    private static readonly WorldGenFlowers    _genBrownShroom = new(39);      // s: brown mushroom
    private static readonly WorldGenFlowers    _genRedShroom   = new(40);      // t: red mushroom
    private static readonly WorldGenHugeMushroom _genHugeMush  = new();        // u: random type
    private static readonly WorldGenReed       _genReed        = new();        // v: sugar cane
    private static readonly WorldGenCactus     _genCactus      = new();        // w: cactus
    private static readonly WorldGenLilyPad    _genLilyPad     = new();        // x: lily pad

    // ── Main Generate (spec §5) ───────────────────────────────────────────────

    /// <summary>
    /// Generates terrain only (Pass 1 + Pass 2). Does NOT populate ores.
    /// Called by <see cref="GetChunk"/> which stores the result and then calls
    /// <see cref="PopulateChunk"/> separately, after the chunk is safely in the cache.
    /// </summary>
    private Chunk GenerateChunkTerrain(int chunkX, int chunkZ)
    {
        _rand.SetSeed((long)chunkX * 341873128712L + (long)chunkZ * 132897987541L);

        var blocks = new byte[16 * World.WorldHeight * 16];
        var chunk  = new Chunk(_world, chunkX, chunkZ);

        // Pass 1: density noise → stone / water
        FillDensity(chunkX, chunkZ, blocks);

        // Pass 1b: cave carving (before surface so caves cut through correct blocks)
        _caveGen.Generate  (_world, chunkX, chunkZ, blocks);
        _ravineGen.Generate(_world, chunkX, chunkZ, blocks);

        // Get biome array (16×16 block columns)
        BiomeGenBase[] biomes = _world.ChunkManager != null
            ? _world.ChunkManager.GetBiomesForGeneration(null, chunkX * 16, chunkZ * 16, 16, 16)
            : FillDefaultBiomes(16, 16);

        // Pass 2: surface pass
        ApplySurface(chunkX, chunkZ, blocks, biomes);

        // Copy blocks into chunk
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int y = 0; y < World.WorldHeight; y++)
        {
            byte id = blocks[(x * 16 + z) * World.WorldHeight + y];
            if (id != 0)
                chunk.SetBlock(x, y, z, id);
        }

        chunk.GenerateSkylightMap(); // initialises height map + sky-light nibbles (spec §13)
        chunk.IsLoaded    = true;
        chunk.IsPopulated = true;

        return chunk;
    }

    // ── Decorate / Populate — full BiomeDecorator sequence (spec §4) ────────

    /// <summary>
    /// Public entry point called by <see cref="ChunkProviderServer"/> when the 2×2 neighbour
    /// condition is met from outside the generator.  Sets <see cref="Chunk.IsPopulated"/>
    /// before decorating so re-entry never double-populates.
    /// Spec: the chunk's <c>a(ej, ej, int, int)</c> population hook.
    /// </summary>
    public virtual void PopulateChunkFromServer(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (_chunks.TryGetValue(key, out Chunk? c) && c.IsPopulated) return;
        if (c != null) c.IsPopulated = true;
        PopulateChunk(chunkX, chunkZ);
    }

    /// Returns the deterministic seed that PopulateChunk uses for the given chunk coordinates.
    /// Used by tests to verify seed derivation (spec §8).
    public long GetPopulateSeedForChunk(int chunkX, int chunkZ)
    {
        var rng = new JavaRandom(_worldSeed);
        rng.SetSeed(_worldSeed);
        long xSeed = (rng.NextLong() / 2L * 2L) + 1L;
        long zSeed = (rng.NextLong() / 2L * 2L) + 1L;
        return (long)chunkX * xSeed + (long)chunkZ * zSeed ^ _worldSeed;
    }

    private void PopulateChunk(int chunkX, int chunkZ)
    {
        // Derive deterministic per-chunk seed from world seed (spec §8)
        _rand.SetSeed(_worldSeed);
        long xSeed = (_rand.NextLong() / 2L * 2L) + 1L;
        long zSeed = (_rand.NextLong() / 2L * 2L) + 1L;
        _rand.SetSeed((long)chunkX * xSeed + (long)chunkZ * zSeed ^ _worldSeed);

        int originX = chunkX * 16;
        int originZ = chunkZ * 16;
        int wh      = World.WorldHeight; // 128

        // Resolve centre biome for all decorator parameters
        BiomeGenBase biome = _world.ChunkManager != null
            ? _world.ChunkManager.GetBiomeAt(originX + 8, originZ + 8)
            : BiomeGenBase.Plains;

        // ── Step 0: Structures — WorldGenStructures_Spec §1 ─────────────────────
        // Runs before ores. Village returns true → suppress dungeon spawn below.
        bool villagePresent = false;
        if (_generateStructures)
        {
            _mineshaft .Generate(_world, chunkX, chunkZ, _rand);
            villagePresent = _village.Generate(_world, chunkX, chunkZ, _rand);
            _stronghold.Generate(_world, chunkX, chunkZ, _rand);
        }

        // Water lakes: 1/4 chunks (WorldGenLakes_Spec.md)
        if (_rand.NextInt(4) == 0)
        {
            int lx = originX + _rand.NextInt(16);
            int ly = _rand.NextInt(wh);
            int lz = originZ + _rand.NextInt(16);
            new WorldGenLakes(8).Generate(_world, _rand, lx, ly, lz);
        }

        // Lava lakes: 1/8 chunks, doubly-biased-low Y (WorldGenLakes_Spec.md)
        if (_rand.NextInt(8) == 0)
        {
            int lakeY = _rand.NextInt(_rand.NextInt(120) + 8);
            if (lakeY < SeaLevel || _rand.NextInt(10) == 0)
            {
                int lx = originX + _rand.NextInt(16);
                int lz = originZ + _rand.NextInt(16);
                new WorldGenLakes(10).Generate(_world, _rand, lx, lakeY, lz);
            }
        }

        // Dungeon spawner (spec §1 — suppressed when village is present in this chunk)
        // nextInt(4)==0 gives ~25% chance per chunk
        if (!villagePresent && _rand.NextInt(4) == 0)
        {
            int dx = originX + _rand.NextInt(16) + 8;
            int dy = _rand.NextInt(wh);
            int dz = originZ + _rand.NextInt(16) + 8;
            new WorldGenDungeon().Generate(_world, _rand, dx, dy, dz);
        }

        // ── Step 1: Ore generation — BiomeDecorator.b() (spec §4 step 1) ─────
        // Helper a(count, gen, yMin, yMax): no +8 offset on ore helper positions
        OreHelper(_genDirt,     20, originX, originZ, 0,    wh);      // dirt
        OreHelper(_genGravel,   10, originX, originZ, 0,    wh);      // gravel
        OreHelper(_genCoal,     20, originX, originZ, 0,    wh);      // coal
        OreHelper(_genIron,     20, originX, originZ, 0,    wh / 2);  // iron   Y[0,64)
        OreHelper(_genGold,      2, originX, originZ, 0,    wh / 4);  // gold   Y[0,32)
        OreHelper(_genRedstone,  8, originX, originZ, 0,    wh / 8);  // redstone Y[0,16)
        OreHelper(_genDiamond,   1, originX, originZ, 0,    wh / 8);  // diamond  Y[0,16)
        // Lapis — triangular Y: nextInt(spread)+nextInt(spread)+(center-spread), center=16, spread=16
        LapisHelper(1, _genLapis, originX, originZ, wh / 8, wh / 8);

        // ── Step 2: Sand disc patches (H times) ───────────────────────────────
        for (int i = 0; i < biome.SandDiscCount; i++)
        {
            int sx = originX + _rand.NextInt(16) + 8;
            int sz = originZ + _rand.NextInt(16) + 8;
            _genSand.Generate(_world, _rand, sx, _world.GetTopSolidOrLiquidBlock(sx, sz), sz);
        }

        // ── Step 3: Clay disc patches (I times) ───────────────────────────────
        for (int i = 0; i < biome.ClayDiscCount; i++)
        {
            int cx = originX + _rand.NextInt(16) + 8;
            int cz = originZ + _rand.NextInt(16) + 8;
            _genClay.Generate(_world, _rand, cx, _world.GetTopSolidOrLiquidBlock(cx, cz), cz);
        }

        // ── Step 4: Extra sand patches (G times, same generator as step 2) ───
        for (int i = 0; i < biome.ExtraSandCount; i++)
        {
            int sx = originX + _rand.NextInt(16) + 8;
            int sz = originZ + _rand.NextInt(16) + 8;
            _genSand.Generate(_world, _rand, sx, _world.GetTopSolidOrLiquidBlock(sx, sz), sz);
        }

        // ── Step 5: Trees ─────────────────────────────────────────────────────
        int treeCount = biome.TreeCount;
        if (treeCount >= 0 && _rand.NextInt(10) == 0) treeCount++;

        for (int i = 0; i < treeCount; i++)
        {
            int tx = originX + _rand.NextInt(16) + 8;
            int tz = originZ + _rand.NextInt(16) + 8;
            WorldGenerator treeGen = biome.GetTreeGenerator(_rand);
            treeGen.SetScale(1.0, 1.0, 1.0);
            treeGen.Generate(_world, _rand, tx, _world.GetHeightValue(tx, tz), tz);
        }

        // ── Step 6: Huge mushrooms (J times) ──────────────────────────────────
        for (int i = 0; i < biome.HugeMushroomCount; i++)
        {
            int mx = originX + _rand.NextInt(16) + 8;
            int mz = originZ + _rand.NextInt(16) + 8;
            _genHugeMush.Generate(_world, _rand, mx, _world.GetHeightValue(mx, mz), mz);
        }

        // ── Step 7: Flowers (A times; 25% chance of rose alongside dandelion) ─
        for (int i = 0; i < biome.FlowerCount; i++)
        {
            int fx = originX + _rand.NextInt(16) + 8;
            int fy = _rand.NextInt(wh);
            int fz = originZ + _rand.NextInt(16) + 8;
            _genDandelion.Generate(_world, _rand, fx, fy, fz);

            if (_rand.NextInt(4) == 0)
            {
                fx = originX + _rand.NextInt(16) + 8;
                fy = _rand.NextInt(wh);
                fz = originZ + _rand.NextInt(16) + 8;
                _genRose.Generate(_world, _rand, fx, fy, fz);
            }
        }

        // ── Step 8: Tall grass (B times; new instance each call per spec) ─────
        for (int i = 0; i < biome.TallGrassCount; i++)
        {
            int gx = originX + _rand.NextInt(16) + 8;
            int gy = _rand.NextInt(wh);
            int gz = originZ + _rand.NextInt(16) + 8;
            new WorldGenTallGrass(31, 1).Generate(_world, _rand, gx, gy, gz); // meta 1 = tall grass
        }

        // ── Step 9: Dead bushes (C times; new instance each call per spec) ────
        for (int i = 0; i < biome.DeadBushCount; i++)
        {
            int dx = originX + _rand.NextInt(16) + 8;
            int dy = _rand.NextInt(wh);
            int dz = originZ + _rand.NextInt(16) + 8;
            new WorldGenShrub(32).Generate(_world, _rand, dx, dy, dz); // ID 32 = dead bush
        }

        // ── Step 10: Lily pads (y times) ─────────────────────────────────────
        for (int i = 0; i < biome.LilyPadCount; i++)
        {
            int lx = originX + _rand.NextInt(16) + 8;
            int lz = originZ + _rand.NextInt(16) + 8;
            int ly = _rand.NextInt(wh);
            // Descend through air/leaves to water surface (spec §4 step 10)
            while (ly > 0)
            {
                int id = _world.GetBlockId(lx, ly - 1, lz);
                if (id != 0 && id != 18) break; // stop above first non-air/non-leaf
                ly--;
            }
            _genLilyPad.Generate(_world, _rand, lx, ly, lz);
        }

        // ── Step 11: Mushrooms (D times, plus unconditional extras) ───────────
        for (int i = 0; i < biome.MushroomCount; i++)
        {
            if (_rand.NextInt(4) == 0)
            {
                int mx = originX + _rand.NextInt(16) + 8;
                int mz = originZ + _rand.NextInt(16) + 8;
                _genBrownShroom.Generate(_world, _rand, mx, _world.GetHeightValue(mx, mz), mz);
            }
            if (_rand.NextInt(8) == 0)
            {
                int mx = originX + _rand.NextInt(16) + 8;
                int my = _rand.NextInt(wh);
                int mz = originZ + _rand.NextInt(16) + 8;
                _genRedShroom.Generate(_world, _rand, mx, my, mz);
            }
        }
        // Unconditional extra mushroom rolls (always, outside D loop)
        if (_rand.NextInt(4) == 0)
        {
            int mx = originX + _rand.NextInt(16) + 8;
            int mz = originZ + _rand.NextInt(16) + 8;
            _genBrownShroom.Generate(_world, _rand, mx, _world.GetHeightValue(mx, mz), mz);
        }
        if (_rand.NextInt(8) == 0)
        {
            int mx = originX + _rand.NextInt(16) + 8;
            int my = _rand.NextInt(wh);
            int mz = originZ + _rand.NextInt(16) + 8;
            _genRedShroom.Generate(_world, _rand, mx, my, mz);
        }

        // ── Step 12: Reeds / Sugar Cane (E biome-specific + 10 hardcoded) ────
        for (int i = 0; i < biome.ReedCount + 10; i++)
        {
            int rx = originX + _rand.NextInt(16) + 8;
            int ry = _rand.NextInt(wh);
            int rz = originZ + _rand.NextInt(16) + 8;
            _genReed.Generate(_world, _rand, rx, ry, rz);
        }

        // ── Step 13: Pumpkin patch (1/32 chance) ─────────────────────────────
        if (_rand.NextInt(32) == 0)
        {
            int px = originX + _rand.NextInt(16) + 8;
            int py = _rand.NextInt(wh);
            int pz = originZ + _rand.NextInt(16) + 8;
            new WorldGenPumpkin().Generate(_world, _rand, px, py, pz);
        }

        // ── Step 14: Cactus (F times) ─────────────────────────────────────────
        for (int i = 0; i < biome.CactusCount; i++)
        {
            int cx = originX + _rand.NextInt(16) + 8;
            int cy = _rand.NextInt(wh);
            int cz = originZ + _rand.NextInt(16) + 8;
            _genCactus.Generate(_world, _rand, cx, cy, cz);
        }

        // ── Step 15: Springs (when K == true) ────────────────────────────────
        if (biome.EnableSprings)
        {
            // 50 water springs — Y biased toward surface
            for (int i = 0; i < 50; i++)
            {
                int wx = originX + _rand.NextInt(16) + 8;
                int wy = _rand.NextInt(_rand.NextInt(wh - 8) + 8);
                int wz = originZ + _rand.NextInt(16) + 8;
                new WorldGenSpring(8).Generate(_world, _rand, wx, wy, wz); // ID 8 = flowing water
            }

            // 20 lava springs — Y triply biased toward bedrock
            for (int i = 0; i < 20; i++)
            {
                int lx = originX + _rand.NextInt(16) + 8;
                int ly = _rand.NextInt(_rand.NextInt(_rand.NextInt(wh - 16) + 8) + 8);
                int lz = originZ + _rand.NextInt(16) + 8;
                new WorldGenSpring(10).Generate(_world, _rand, lx, ly, lz); // ID 10 = flowing lava
            }
        }

        // ── Snow/Ice pass (SnowIce_Spec §8) ─────────────────────────────────
        // Inline 16×16 pass at end of populateChunk — no separate WorldGenerator class.
        // SpawnerAnimals.InitialPopulate is also called here per spec (after decoration).
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        {
            int worldX = originX + x;
            int worldZ = originZ + z;
            int surfaceY = _world.GetHeightValue(worldX, worldZ);

            // Ice pass: freeze surface water one block below height map
            if (_world.CanFreezeAtLocation(worldX, surfaceY - 1, worldZ))
                _world.SetBlockSilent(worldX, surfaceY - 1, worldZ, 79); // ice

            // Snow pass: place snow layer at height map surface (if air)
            if (_world.CanSnowAtLocation(worldX, surfaceY, worldZ))
                _world.SetBlockSilent(worldX, surfaceY, worldZ, 78); // snow layer
        }
    }

    /// <summary>
    /// Ore helper <c>a(count, gen, yMin, yMax)</c> — uniform Y distribution, no +8 offset.
    /// Spec §4 step 1.
    /// </summary>
    private void OreHelper(WorldGenMineable gen, int count, int originX, int originZ,
                            int yMin, int yMax)
    {
        for (int i = 0; i < count; i++)
        {
            int x = originX + _rand.NextInt(16);
            int y = yMin + _rand.NextInt(yMax - yMin);
            int z = originZ + _rand.NextInt(16);
            gen.Generate(_world, _rand, x, y, z);
        }
    }

    /// <summary>
    /// Lapis helper <c>b(count, gen, yCenter, ySpread)</c> — triangular Y distribution.
    /// Spec §4 step 1.
    /// </summary>
    private void LapisHelper(int count, WorldGenMineable gen, int originX, int originZ,
                              int yCenter, int ySpread)
    {
        for (int i = 0; i < count; i++)
        {
            int x = originX + _rand.NextInt(16);
            int y = _rand.NextInt(ySpread) + _rand.NextInt(ySpread) + (yCenter - ySpread);
            int z = originZ + _rand.NextInt(16);
            gen.Generate(_world, _rand, x, y, z);
        }
    }

    // ── Pass 1: 3D density (spec §6) ─────────────────────────────────────────

    private void FillDensity(int chunkX, int chunkZ, byte[] blocks)
    {
        const int GridX  = 5;
        const int GridZ  = 5;
        const int GridY  = 17; // 128 / 8 + 1
        const int CellY  = 8;  // vertical cell size in blocks
        const int CellXZ = 4;  // horizontal cell size in blocks

        int originX = chunkX * 4;
        int originZ = chunkZ * 4;

        // Sample 2D noise arrays for biome blending and hills
        double[] blend = _noiseBlend.Generate2D(null,    originX, originZ, GridX, GridZ, 1.121, 1.121);
        double[] hill  = _noiseHill .Generate2D(null,    originX, originZ, GridX, GridZ, 200.0, 200.0);

        // Sample 3D noise arrays
        _densityBuf = _noiseQ.Generate3D(_densityBuf, originX, 0, originZ, GridX, GridY, GridZ,
                                          8.555, 4.278, 8.555);
        double[] dA = _noiseA.Generate3D(null, originX, 0, originZ, GridX, GridY, GridZ,
                                          684.412, 684.412, 684.412);
        double[] dB = _noiseB.Generate3D(null, originX, 0, originZ, GridX, GridY, GridZ,
                                          684.412, 684.412, 684.412);

        // Evaluate density for each cell corner of the 4×16×4 grid
        double[] density = new double[GridX * GridY * GridZ];

        // Build per-XZ biome smoothing using the 5×5 kernel
        for (int gx = 0; gx < GridX; gx++)
        for (int gz = 0; gz < GridZ; gz++)
        {
            float wSum = 0;
            float ampAcc = 0, minAcc = 0;

            // 5×5 neighbour kernel around (gx, gz)
            for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            {
                int nx = gx + dx; int nz = gz + dz;
                if (nx < 0 || nx >= GridX || nz < 0 || nz >= GridZ) continue;

                BiomeGenBase? nb = _world.ChunkManager?.GetBiomeAt(
                    (chunkX * 4 + nx) * 4, (chunkZ * 4 + nz) * 4);
                nb ??= BiomeGenBase.Plains;

                float w = 10.0f / MathF.Sqrt(dx * dx + dz * dz + 0.2f);
                if (nb.MinHeight > BiomeGenBase.Plains.MinHeight) w /= 2.0f;

                wSum   += w;
                ampAcc += nb.MaxHeight * w;
                minAcc += nb.MinHeight * w;
            }

            float ampS = (ampAcc / wSum) * 0.9f + 0.1f;
            float minS = ((minAcc / wSum) * 4.0f - 1.0f) / 8.0f;

            for (int gy = 0; gy < GridY; gy++)
            {
                int gIdx2D = (gx * GridZ + gz);
                int gIdx3D = (gx * GridZ + gz) * GridY + gy;

                double hillRaw = hill[gIdx2D] / 8000.0;
                if (hillRaw < 0) hillRaw = -hillRaw * 0.3;
                hillRaw = hillRaw * 3.0 - 2.0;
                if (hillRaw < 0)
                    hillRaw = Math.Clamp(hillRaw / 2.0, -1.0, 0.0) / 1.4 / 2.0;
                else
                    hillRaw = Math.Min(hillRaw, 1.0) / 8.0;
                hillRaw += 0.2 * ampS;

                double blockY     = gy * CellY; // convert grid-Y to block-Y (0..128)
                double midY       = World.WorldHeight / 2.0 + minS * 4.0 * (World.WorldHeight / 8.0);
                double heightGrad = ((blockY - midY) * 12.0 * 128.0 / World.WorldHeight) / ampS;
                if (heightGrad < 0) heightGrad *= 4.0;

                double sel = (_densityBuf[gIdx3D] / 10.0 + 1.0) / 2.0;
                double d;
                if (sel <= 0.0)      d = dA[gIdx3D] / 512.0;
                else if (sel >= 1.0) d = dB[gIdx3D] / 512.0;
                else                 d = dA[gIdx3D] / 512.0 + (dB[gIdx3D] / 512.0 - dA[gIdx3D] / 512.0) * sel;

                d -= heightGrad;

                // Force atmosphere at top 4 Y slices
                if (gy > GridY - 4)
                {
                    double blend2 = (gy - (GridY - 4)) / 3.0;
                    d = d * (1.0 - blend2) + -10.0 * blend2;
                }

                density[gIdx3D] = d;
            }
        }

        // Trilinear interpolation: fill the 16×128×16 block array
        int seaLevel = World.MidWorldY;

        for (int gx = 0; gx < GridX - 1; gx++)
        for (int gz = 0; gz < GridZ - 1; gz++)
        for (int gy = 0; gy < GridY - 1; gy++)
        {
            // 8 corners of this density cell
            double d000 = density[(gx * GridZ + gz)         * GridY + gy];
            double d100 = density[((gx + 1) * GridZ + gz)   * GridY + gy];
            double d010 = density[(gx * GridZ + gz)         * GridY + gy + 1];
            double d110 = density[((gx + 1) * GridZ + gz)   * GridY + gy + 1];
            double d001 = density[(gx * GridZ + (gz + 1))   * GridY + gy];
            double d101 = density[((gx + 1) * GridZ + gz + 1) * GridY + gy];
            double d011 = density[(gx * GridZ + (gz + 1))   * GridY + gy + 1];
            double d111 = density[((gx + 1) * GridZ + gz + 1) * GridY + gy + 1];

            for (int cellZ = 0; cellZ < CellXZ; cellZ++)
            for (int cellX = 0; cellX < CellXZ; cellX++)
            for (int cellY = 0; cellY < CellY; cellY++)
            {
                double tx = cellX / (double)CellXZ;
                double ty = cellY / (double)CellY;
                double tz = cellZ / (double)CellXZ;

                // Trilinear interpolation
                double d = Trilinear(d000, d100, d010, d110,
                                     d001, d101, d011, d111,
                                     tx, ty, tz);

                int bx = gx * CellXZ + cellX;
                int bz = gz * CellXZ + cellZ;
                int by = gy * CellY  + cellY;
                if (bx >= 16 || bz >= 16 || by >= World.WorldHeight) continue;

                int bIdx = (bx * 16 + bz) * World.WorldHeight + by;
                if (d > 0.0)
                    blocks[bIdx] = 1; // stone
                else if (by < seaLevel)
                    blocks[bIdx] = 9; // still water
                else
                    blocks[bIdx] = 0; // air
            }
        }
    }

    // ── Pass 2: surface blocks (spec §7) ─────────────────────────────────────

    private void ApplySurface(int chunkX, int chunkZ, byte[] blocks, BiomeGenBase[] biomes)
    {
        double[] surfaceNoise = _noiseR.Generate2D(null,
            chunkX * 16, chunkZ * 16, 16, 16, 0.03125, 0.03125);

        int seaLevel = World.MidWorldY;

        for (int bx = 0; bx < 16; bx++)
        for (int bz = 0; bz < 16; bz++)
        {
            BiomeGenBase biome = biomes[bz * 16 + bx];
            int dirtDepth = (int)(surfaceNoise[bx * 16 + bz] / 3.0 + 3.0 + _rand.NextDouble() * 0.25);

            int depth = -1;
            byte topBlock    = biome.TopBlockId;
            byte fillerBlock = biome.FillerBlockId;

            for (int by = World.WorldHeight - 1; by >= 0; by--)
            {
                int idx = (bx * 16 + bz) * World.WorldHeight + by;

                // Bottom bedrock
                if (by <= _rand.NextInt(5))
                {
                    blocks[idx] = 7; // bedrock
                    continue;
                }

                byte current = blocks[idx];

                if (current == 0) // air
                {
                    depth = -1;
                    continue;
                }

                if (current != 1) // not stone → skip (water etc.)
                    continue;

                if (depth == -1) // first stone hit from above
                {
                    if (dirtDepth == 0)
                    {
                        // dirtDepth == 0: bare stone surface
                        topBlock    = 0;
                        fillerBlock = 1;
                    }
                    else if (by >= seaLevel - 4 && by <= seaLevel + 1)
                    {
                        topBlock    = biome.TopBlockId;
                        fillerBlock = biome.FillerBlockId;

                        // Ice at sea-level if cold biome
                        if (biome.Temperature < 0.15f && by == seaLevel - 1)
                            topBlock = 79; // ice
                    }

                    depth = dirtDepth;
                    blocks[idx] = by >= seaLevel - 1 ? topBlock : fillerBlock;
                    if (by < seaLevel && topBlock == 0)
                        blocks[idx] = 9; // still water if no top block
                }
                else if (depth > 0)
                {
                    depth--;
                    blocks[idx] = fillerBlock;

                    // Desert: sandstone below sand
                    if (depth == 0 && fillerBlock == 12)
                    {
                        int extraSandstone = 1 + _rand.NextInt(4);
                        for (int sy = by - 1; sy >= by - extraSandstone && sy >= 0; sy--)
                        {
                            int sIdx = (bx * 16 + bz) * World.WorldHeight + sy;
                            if (blocks[sIdx] == 1) blocks[sIdx] = 24; // sandstone
                            else break;
                        }
                    }
                }
            }

            // Top bedrock layer
            int topIdx = (bx * 16 + bz) * World.WorldHeight + (World.WorldHeight - 1);
            if (blocks[topIdx] != 0)
                blocks[topIdx] = 7; // bedrock
            else
            {
                // Find top non-air block and guarantee bedrock there
                for (int by = World.WorldHeight - 1; by >= World.WorldHeight - 5; by--)
                {
                    int idx = (bx * 16 + bz) * World.WorldHeight + by;
                    if (blocks[idx] != 0)
                    {
                        if (_rand.NextInt(5) == 0) blocks[idx] = 7;
                        break;
                    }
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static double Trilinear(
        double v000, double v100, double v010, double v110,
        double v001, double v101, double v011, double v111,
        double tx, double ty, double tz)
    {
        double x00 = v000 + tx * (v100 - v000);
        double x10 = v010 + tx * (v110 - v010);
        double x01 = v001 + tx * (v101 - v001);
        double x11 = v011 + tx * (v111 - v011);
        double xy0 = x00  + ty * (x10  - x00);
        double xy1 = x01  + ty * (x11  - x01);
        return xy0 + tz * (xy1 - xy0);
    }

    private static BiomeGenBase[] FillDefaultBiomes(int w, int h)
    {
        var arr = new BiomeGenBase[w * h];
        for (int i = 0; i < arr.Length; i++) arr[i] = BiomeGenBase.Plains;
        return arr;
    }
}
