using SpectraEngine.Core.WorldGen;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>sr</c> (BiomeGenBase) — abstract base class for all biomes.
///
/// 16 static instances (IDs 0–15) are registered in <c>Biomes[256]</c> at static construction.
/// Each biome carries temperature, rainfall, height range, surface block IDs, and colour info.
///
/// Colour lookup is delegated to:
///   <see cref="GrassColorizer"/>   — maps (temp, rain) → packed RGB grass tint
///   <see cref="FoliageColorizer"/> — maps (temp, rain) → packed RGB foliage tint
///
/// Temperature guard: values in (0.1, 0.2) exclusive are forbidden (snow-level transition bug).
/// Swampland (<c>mk</c>) overrides both colour methods with a special blend formula.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeGenBase_Spec.md
/// </summary>
public class BiomeGenBase
{
    // ── Static registry (spec §8) ─────────────────────────────────────────────

    /// <summary>obf: <c>a[256]</c> — biome registry indexed by biome ID.</summary>
    public static readonly BiomeGenBase?[] Biomes = new BiomeGenBase?[256];

    // ── Static biome instances (spec §8) ──────────────────────────────────────

    public static readonly BiomeGenBase Ocean              = new BiomeGenBase(0)
        .SetBiomeName("Ocean")            .SetHeight(-1.0f, 0.4f) .SetTempRain(0.5f, 0.5f);
    public static readonly BiomeGenBase Plains             = new BiomeGenBase(1)
        .SetBiomeName("Plains")           .SetMapColor(9286496)   .SetHeight(0.1f, 0.3f) .SetTempRain(0.8f, 0.4f)
        .SetTreeCount(-999)               .SetFlowerCount(4)      .SetTallGrassCount(10);
    public static readonly BiomeGenBase Desert             = new BiomeGenBase(2)
        .SetBiomeName("Desert")           .SetMapColor(16421912)  .SetHeight(0.1f, 0.2f) .SetTempRain(2.0f, 0.0f) .SetNoWeather()
        .SetTreeCount(-999)               .SetDeadBushCount(2)    .SetReedCount(50)       .SetCactusCount(10);
    public static readonly BiomeGenBase ExtremeHills       = new BiomeGenBase(3)
        .SetBiomeName("Extreme Hills")    .SetMapColor(6316128)   .SetHeight(0.2f, 1.8f) .SetTempRain(0.2f, 0.3f);
    public static readonly BiomeGenBase Forest             = new ForestBiome(4)
        .SetBiomeName("Forest")           .SetMapColor(353825)    .SetHeight(0.1f, 0.3f) .SetTempRain(0.7f, 0.8f)
        .SetGrassColorOverride(5159473)   .SetTreeCount(10)       .SetTallGrassCount(2);
    public static readonly BiomeGenBase Taiga              = new TaigaBiome(5)
        .SetBiomeName("Taiga")            .SetMapColor(747097)    .SetHeight(0.1f, 0.4f) .SetTempRain(0.3f, 0.8f)
        .SetGrassColorOverride(5159473)   .SetTreeCount(10);      // B=1 same as default
    public static readonly BiomeGenBase Swampland          = new SwamplandBiome(6)
        .SetBiomeName("Swampland")        .SetMapColor(522674)    .SetHeight(-0.2f, 0.1f).SetTempRain(0.8f, 0.9f)
        .SetGrassColorOverride(9154376)   .SetWaterColor(14745456).SetTreeCount(2)
        .SetLilyPadCount(4)               .SetFlowerCount(-999)   .SetDeadBushCount(1)
        .SetMushroomCount(8)              .SetReedCount(10);       // I=1 same as default
    public static readonly BiomeGenBase River              = new BiomeGenBase(7)
        .SetBiomeName("River")            .SetHeight(-0.5f, 0.0f) .SetTempRain(0.5f, 0.5f);
    public static readonly BiomeGenBase Hell               = new BiomeGenBase(8)
        .SetBiomeName("Hell")             .SetMapColor(16711680)  .SetHeight(0.1f, 0.3f) .SetTempRain(2.0f, 0.0f) .SetNoWeather();
    public static readonly BiomeGenBase Sky                = new BiomeGenBase(9)
        .SetBiomeName("Sky")              .SetMapColor(8421631)   .SetHeight(0.1f, 0.3f) .SetTempRain(0.5f, 0.5f) .SetNoWeather();
    public static readonly BiomeGenBase FrozenOcean        = new BiomeGenBase(10)
        .SetBiomeName("FrozenOcean")      .SetMapColor(9474208)   .SetHeight(-1.0f, 0.5f).SetTempRain(0.0f, 0.5f);
    public static readonly BiomeGenBase FrozenRiver        = new BiomeGenBase(11)
        .SetBiomeName("FrozenRiver")      .SetMapColor(10526975)  .SetHeight(-0.5f, 0.0f).SetTempRain(0.0f, 0.5f);
    public static readonly BiomeGenBase IcePlains          = new BiomeGenBase(12)
        .SetBiomeName("Ice Plains")       .SetMapColor(16777215)  .SetHeight(0.1f, 0.3f) .SetTempRain(0.0f, 0.5f);
    public static readonly BiomeGenBase IceMountains       = new BiomeGenBase(13)
        .SetBiomeName("Ice Mountains")    .SetMapColor(10526880)  .SetHeight(0.2f, 1.8f) .SetTempRain(0.0f, 0.5f);
    public static readonly BiomeGenBase MushroomIsland     = new BiomeGenBase(14)
        .SetBiomeName("MushroomIsland")   .SetMapColor(16711935)  .SetHeight(0.2f, 1.0f) .SetTempRain(0.9f, 1.0f);
    public static readonly BiomeGenBase MushroomIslandShore = new BiomeGenBase(15)
        .SetBiomeName("MushroomIslandShore").SetMapColor(10486015).SetHeight(-1.0f, 0.1f).SetTempRain(0.9f, 1.0f);

    // ── Instance fields (spec §5) ─────────────────────────────────────────────

    /// <summary>obf: <c>F</c> — biome ID (0–15).</summary>
    public readonly int BiomeId;

    /// <summary>obf: <c>r</c> — biome display name.</summary>
    public string BiomeName = "Unnamed";

    /// <summary>obf: <c>s</c> — map colour (minimap).</summary>
    public int MapColor;

    /// <summary>obf: <c>t</c> — top block ID. Default: Grass (2).</summary>
    public byte TopBlockId   = 2;

    /// <summary>obf: <c>u</c> — filler block ID. Default: Dirt (3).</summary>
    public byte FillerBlockId = 3;

    /// <summary>obf: <c>v</c> — water colour multiplier. Default: 5169201 (vanilla blue).</summary>
    public int WaterColor = 5169201;

    /// <summary>obf: <c>w</c> — minimum height offset from sea level.</summary>
    public float MinHeight = 0.1f;

    /// <summary>obf: <c>x</c> — maximum height / terrain amplitude.</summary>
    public float MaxHeight = 0.3f;

    /// <summary>obf: <c>y</c> — temperature (0.0 arctic → 2.0 desert).</summary>
    public float Temperature = 0.5f;

    /// <summary>obf: <c>z</c> — rainfall (0.0 arid → 1.0 tropical).</summary>
    public float Rainfall = 0.5f;

    /// <summary>obf: <c>A</c> — grass/foliage color override; 0xFFFFFF = no override.</summary>
    public int ColorOverride = 0xFFFFFF;

    /// <summary>obf: <c>K</c> — is raining right now in this biome.</summary>
    public bool IsRaining;

    /// <summary>obf: <c>L</c> — biome has weather at all (false for Nether/End/Desert).</summary>
    public bool HasWeather = true;

    // ── BiomeDecorator config fields (spec: ql fields, §2) ──────────────────────
    // All fields mirror the BiomeDecorator (ql) instance fields. Each biome
    // subclass adjusts these in the static initialiser via the builder methods.

    /// <summary>obf: <c>B.z</c> — tree attempts per chunk. −999 = no trees.</summary>
    public int TreeCount         = 0;
    /// <summary>obf: <c>B.y</c> — lily pad scatter calls. Default: 0.</summary>
    public int LilyPadCount      = 0;
    /// <summary>obf: <c>B.A</c> — flower scatter calls (dandelion + optional rose). Default: 2.</summary>
    public int FlowerCount       = 2;
    /// <summary>obf: <c>B.B</c> — tall-grass scatter calls. Default: 1.</summary>
    public int TallGrassCount    = 1;
    /// <summary>obf: <c>B.C</c> — dead-bush scatter calls. Default: 0.</summary>
    public int DeadBushCount     = 0;
    /// <summary>obf: <c>B.D</c> — mushroom base count (each triggers 25%/12.5% rolls). Default: 0.</summary>
    public int MushroomCount     = 0;
    /// <summary>obf: <c>B.E</c> — biome-specific reed scatter calls (plus 10 hardcoded). Default: 0.</summary>
    public int ReedCount         = 0;
    /// <summary>obf: <c>B.F</c> — cactus scatter calls. Default: 0.</summary>
    public int CactusCount       = 0;
    /// <summary>obf: <c>B.G</c> — extra sand disc calls (same generator as H). Default: 1.</summary>
    public int ExtraSandCount    = 1;
    /// <summary>obf: <c>B.H</c> — sand disc calls. Default: 3.</summary>
    public int SandDiscCount     = 3;
    /// <summary>obf: <c>B.I</c> — clay disc calls. Default: 1.</summary>
    public int ClayDiscCount     = 1;
    /// <summary>obf: <c>B.J</c> — huge mushroom placement calls. Default: 0.</summary>
    public int HugeMushroomCount = 0;
    /// <summary>obf: <c>B.K</c> — enable water+lava spring generation. Default: true.</summary>
    public bool EnableSprings    = true;

    // ── Spawn lists (spec: SpawnerAnimals_Spec §3) ───────────────────────────

    /// <summary>
    /// One entry in a biome's creature spawn list.
    /// Replica of <c>nc</c> (BiomeGenBase.SpawnListEntry).
    /// </summary>
    public sealed record SpawnListEntry(Type EntityType, int Weight, int MinCount, int MaxCount);

    // Default spawn lists shared by all biomes unless overridden.
    private static readonly List<SpawnListEntry> DefaultHostileSpawns =
    [
        new(typeof(Mobs.EntitySpider),   10, 4, 4),
        new(typeof(Mobs.EntityZombie),   10, 4, 4),
        new(typeof(Mobs.EntitySkeleton), 10, 4, 4),
        new(typeof(Mobs.EntityCreeper),  10, 4, 4),
    ];
    private static readonly List<SpawnListEntry> DefaultPassiveSpawns =
    [
        new(typeof(Mobs.EntitySheep),   12, 4, 4),
        new(typeof(Mobs.EntityPig),     10, 4, 4),
        new(typeof(Mobs.EntityChicken), 10, 4, 4),
        new(typeof(Mobs.EntityCow),      8, 4, 4),
    ];
    private static readonly List<SpawnListEntry> DefaultWaterSpawns = []; // Squid not yet implemented

    /// <summary>obf: hostile spawn list — creatures that spawn at low light levels.</summary>
    public List<SpawnListEntry> HostileSpawnList;
    /// <summary>obf: passive spawn list — animals that spawn at high light levels.</summary>
    public List<SpawnListEntry> PassiveSpawnList;
    /// <summary>obf: water creature spawn list — squids etc.</summary>
    public List<SpawnListEntry> WaterSpawnList;

    /// <summary>
    /// Returns the spawn list for the given creature type.
    /// Used by <see cref="SpawnerAnimals"/>. Spec: <c>a(jf)</c>.
    /// </summary>
    public List<SpawnListEntry> GetSpawnList(EnumCreatureType type) => type switch
    {
        EnumCreatureType.Hostile => HostileSpawnList,
        EnumCreatureType.Passive => PassiveSpawnList,
        EnumCreatureType.Water   => WaterSpawnList,
        _                        => [],
    };

    // Shared generator instances (one per biome class, spec §10.3)
    private static readonly WorldGenTrees       _oakGen    = new(false); // G
    private static readonly WorldGenBigTree     _bigOakGen = new();      // H
    private static readonly WorldGenForestTree  _birchGen  = new(false); // I (Forest only)
    private static readonly WorldGenSwamp       _swampGen  = new();      // J (Swamp only)

    /// <summary>
    /// obf: <c>a(Random)</c> on <c>sr</c> — returns the tree generator to use for one attempt.
    /// Default: 10% fancy oak, 90% standard oak. Biome subclasses override for special trees.
    /// </summary>
    public virtual WorldGenerator GetTreeGenerator(JavaRandom rand)
        => rand.NextInt(10) == 0 ? _bigOakGen : _oakGen;

    /// <summary>Shared oak generator instance for biome subclasses.</summary>
    protected static WorldGenTrees      OakGen    => _oakGen;
    /// <summary>Shared big oak generator instance for biome subclasses.</summary>
    protected static WorldGenBigTree    BigOakGen => _bigOakGen;
    /// <summary>Shared birch generator instance for biome subclasses.</summary>
    protected static WorldGenForestTree BirchGen  => _birchGen;
    /// <summary>Shared swamp generator instance for biome subclasses.</summary>
    protected static WorldGenSwamp      SwampGen  => _swampGen;

    // ── Constructor (spec §5) ─────────────────────────────────────────────────

    protected BiomeGenBase(int id)
    {
        BiomeId = id;
        Biomes[id] = this;
        // Default spawn lists — biome subclasses or builder can replace these
        HostileSpawnList = DefaultHostileSpawns;
        PassiveSpawnList = DefaultPassiveSpawns;
        WaterSpawnList   = DefaultWaterSpawns;
    }

    // ── Builder methods (spec §6) ─────────────────────────────────────────────

    public BiomeGenBase SetBiomeName(string name)              { BiomeName          = name;        return this; }
    public BiomeGenBase SetMapColor(int color)                 { MapColor           = color;       return this; }
    public BiomeGenBase SetWaterColor(int color)               { WaterColor         = color;       return this; }
    public BiomeGenBase SetHeight(float min, float max)        { MinHeight          = min; MaxHeight = max; return this; }
    public BiomeGenBase SetNoWeather()                         { HasWeather         = false;       return this; }
    public BiomeGenBase SetGrassColorOverride(int color)       { ColorOverride      = color;       return this; }
    public BiomeGenBase SetTreeCount(int count)                { TreeCount          = count;       return this; }
    public BiomeGenBase SetLilyPadCount(int count)             { LilyPadCount       = count;       return this; }
    public BiomeGenBase SetFlowerCount(int count)              { FlowerCount        = count;       return this; }
    public BiomeGenBase SetTallGrassCount(int count)           { TallGrassCount     = count;       return this; }
    public BiomeGenBase SetDeadBushCount(int count)            { DeadBushCount      = count;       return this; }
    public BiomeGenBase SetMushroomCount(int count)            { MushroomCount      = count;       return this; }
    public BiomeGenBase SetReedCount(int count)                { ReedCount          = count;       return this; }
    public BiomeGenBase SetCactusCount(int count)              { CactusCount        = count;       return this; }
    public BiomeGenBase SetExtraSandCount(int count)           { ExtraSandCount     = count;       return this; }
    public BiomeGenBase SetSandDiscCount(int count)            { SandDiscCount      = count;       return this; }
    public BiomeGenBase SetClayDiscCount(int count)            { ClayDiscCount      = count;       return this; }
    public BiomeGenBase SetHugeMushroomCount(int count)        { HugeMushroomCount  = count;       return this; }
    public BiomeGenBase SetEnableSprings(bool enable)          { EnableSprings      = enable;      return this; }

    /// <summary>
    /// obf: <c>a(float temp, float rain)</c> — sets Temperature and Rainfall.
    /// Throws if temp is in the forbidden range (0.1, 0.2) — snow-level transition bug.
    /// </summary>
    public BiomeGenBase SetTempRain(float temp, float rain)
    {
        if (temp > 0.1f && temp < 0.2f)
            throw new ArgumentException($"Biome temperature {temp} is in forbidden range (0.1, 0.2)");
        Temperature = temp;
        Rainfall    = rain;
        return this;
    }

    // ── Colour methods (spec §7) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(kq world, int x, int y, int z)</c> — getGrassColor.
    /// Default: looks up GrassColorizer using temperature and rainfall from WorldChunkManager.
    /// </summary>
    public virtual int GetGrassColor(IBlockAccess world, int x, int y, int z)
    {
        double temp = (double)Temperature;
        double rain = (double)Rainfall;

        // Try to get per-column climate data from WorldChunkManager
        if (world is World w && w.ChunkManager != null)
        {
            temp = w.ChunkManager.GetTemperatureAtHeight(x, y, z);
            rain = w.ChunkManager.GetRainfallAtHeight(x, z);
        }

        return GrassColorizer.GetGrassColor(temp, rain);
    }

    /// <summary>
    /// obf: <c>b(kq world, int x, int y, int z)</c> — getFoliageColor.
    /// Default: looks up FoliageColorizer using temperature and rainfall.
    /// </summary>
    public virtual int GetFoliageColor(IBlockAccess world, int x, int y, int z)
    {
        double temp = (double)Temperature;
        double rain = (double)Rainfall;

        if (world is World w && w.ChunkManager != null)
        {
            temp = w.ChunkManager.GetTemperatureAtHeight(x, y, z);
            rain = w.ChunkManager.GetRainfallAtHeight(x, z);
        }

        return FoliageColorizer.GetFoliageColor(temp, rain);
    }

    // ── Auxiliary methods (spec §7) ───────────────────────────────────────────

    /// <summary>obf: <c>c()</c> — getEnableSnow = !IsRaining &amp;&amp; HasWeather.</summary>
    public bool GetEnableSnow() => !IsRaining && HasWeather;

    /// <summary>obf: <c>e()</c> — rainfall as fixed-point int.</summary>
    public int GetRainfallFixed() => (int)(Rainfall * 65536.0f);

    /// <summary>obf: <c>f()</c> — temperature as fixed-point int.</summary>
    public int GetTemperatureFixed() => (int)(Temperature * 65536.0f);
}

// ── Biome subclasses ──────────────────────────────────────────────────────────

/// <summary>
/// Forest biome (ID 4) — spawns birch and oak trees, uses birch generator 20% of the time.
/// </summary>
public sealed class ForestBiome : BiomeGenBase
{
    public ForestBiome(int id) : base(id) { }

    public override WorldGenerator GetTreeGenerator(JavaRandom rand)
        => rand.NextInt(5) == 0 ? BirchGen : (rand.NextInt(10) == 0 ? BigOakGen : OakGen);
}

/// <summary>
/// Taiga biome (ID 5) — 67% thin spruce (shared), 33% wide spruce (fresh instance per call).
/// Spec: <c>rand.nextInt(3) == 0 ? new us() : ty</c>.
/// </summary>
public sealed class TaigaBiome : BiomeGenBase
{
    private static readonly WorldGenTaiga1 _thinSpruce = new(false); // shared ty instance

    public TaigaBiome(int id) : base(id) { }

    public override WorldGenerator GetTreeGenerator(JavaRandom rand)
        => rand.NextInt(3) == 0 ? new WorldGenTaiga2() : _thinSpruce; // 33% wide / 67% thin
}

// ── Swampland subclass (spec §9) ──────────────────────────────────────────────

/// <summary>
/// Replica of <c>mk</c> (Swampland) — biome ID 6.
/// Overrides grass and foliage colour with a special swamp-green blend formula.
/// </summary>
public sealed class SwamplandBiome : BiomeGenBase
{
    private const int SwampBase = 5115470; // dark olive green

    public SwamplandBiome(int id) : base(id) { }

    /// <summary>100% swamp oak with vines. Spec: Swampland always calls qj.</summary>
    public override WorldGenerator GetTreeGenerator(JavaRandom rand) => SwampGen;

    /// <summary>
    /// Swamp grass: averages the lookup result with <c>SwampBase</c>.
    /// Formula: <c>((ha.a(t,r) &amp; 0xFEFEFE) + 5115470) / 2</c>.
    /// </summary>
    public override int GetGrassColor(IBlockAccess world, int x, int y, int z)
    {
        double temp = (double)Temperature;
        double rain = (double)Rainfall;

        if (world is World w && w.ChunkManager != null)
        {
            temp = w.ChunkManager.GetTemperatureAtHeight(x, y, z);
            rain = w.ChunkManager.GetRainfallAtHeight(x, z);
        }

        int lookup = GrassColorizer.GetGrassColor(temp, rain);
        return ((lookup & 0xFEFEFE) + SwampBase) / 2;
    }

    /// <summary>
    /// Swamp foliage: same blend formula applied to foliage lookup.
    /// </summary>
    public override int GetFoliageColor(IBlockAccess world, int x, int y, int z)
    {
        double temp = (double)Temperature;
        double rain = (double)Rainfall;

        if (world is World w && w.ChunkManager != null)
        {
            temp = w.ChunkManager.GetTemperatureAtHeight(x, y, z);
            rain = w.ChunkManager.GetRainfallAtHeight(x, z);
        }

        int lookup = FoliageColorizer.GetFoliageColor(temp, rain);
        return ((lookup & 0xFEFEFE) + SwampBase) / 2;
    }
}
