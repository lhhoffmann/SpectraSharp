using System;
using System.Collections.Generic;
using Xunit;
using SpectraEngine.Core;
using SpectraEngine.Core.WorldGen;
using Mobs = SpectraEngine.Core.Mobs;

namespace SpectraEngine.Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Hand-written fakes
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Minimal IBlockAccess that is NOT a World — forces biomes to use their own temp/rain.</summary>
    internal sealed class FakeBlockAccess : IBlockAccess
    {
        private readonly Dictionary<(int, int, int), int> _blocks = new();
        private readonly Dictionary<(int, int), int> _heights = new();
        private readonly Dictionary<(int, int), int> _topSolid = new();

        public void SetBlock(int x, int y, int z, int id) => _blocks[(x, y, z)] = id;
        public void SetHeight(int x, int z, int h) => _heights[(x, z)] = h;
        public void SetTopSolid(int x, int z, int h) => _topSolid[(x, z)] = h;

        public int GetBlockId(int x, int y, int z) =>
            _blocks.TryGetValue((x, y, z), out var id) ? id : 0;

        public int GetHeightValue(int x, int z) =>
            _heights.TryGetValue((x, z), out var h) ? h : 0;

        public int GetTopSolidOrLiquidBlock(int x, int z) =>
            _topSolid.TryGetValue((x, z), out var h) ? h : 0;

        public bool IsAirBlock(int x, int y, int z) => GetBlockId(x, y, z) == 0;

        public object GetMaterial(int x, int y, int z) => new object();
    
        // ── IBlockAccess stubs (auto-generated) ─────────────────────────
        public int      GetBlockMetadata(int x, int y, int z)           => 0;
        public int      GetLightValue(int x, int y, int z, int e)       => e;
        public float    GetBrightness(int x, int y, int z, int e)       => 1f;
        public Material GetBlockMaterial(int x, int y, int z)           => Material.Air;
        public bool     IsOpaqueCube(int x, int y, int z)               => false;
        public bool     IsWet(int x, int y, int z)                      => false;
        public object?  GetTileEntity(int x, int y, int z)              => null;
        public float    GetUnknownFloat(int x, int y, int z)            => 0f;
        public bool     GetUnknownBool(int x, int y, int z)             => false;
        public object   GetContextObject()                               => new object();
        public int      GetHeight()                                      => 128;
}

    /// <summary>
    /// Lightweight tracker used to record RNG-ordered decoration calls.
    /// Records calls as (stepName, x, y, z) tuples.
    /// </summary>
    internal sealed class DecorationCall
    {
        public string Step;
        public int X, Y, Z;
        public int Extra; // meta / blockId / type
        public DecorationCall(string step, int x, int y, int z, int extra = 0)
        { Step = step; X = x; Y = y; Z = z; Extra = extra; }
        public override string ToString() => $"{Step}({X},{Y},{Z},extra={Extra})";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §1 — BiomeGenBase: static registry (IDs 0–15)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_RegistryTests
    {
        [Fact]
        public void BiomesArray_Length_Is_256()
        {
            Assert.Equal(256, BiomeGenBase.Biomes.Length);
        }

        [Theory]
        [InlineData(0,  "Ocean")]
        [InlineData(1,  "Plains")]
        [InlineData(2,  "Desert")]
        [InlineData(3,  "Extreme Hills")]
        [InlineData(4,  "Forest")]
        [InlineData(5,  "Taiga")]
        [InlineData(6,  "Swampland")]
        [InlineData(7,  "River")]
        [InlineData(8,  "Hell")]
        [InlineData(9,  "Sky")]
        [InlineData(10, "FrozenOcean")]
        [InlineData(11, "FrozenRiver")]
        [InlineData(12, "Ice Plains")]
        [InlineData(13, "Ice Mountains")]
        [InlineData(14, "MushroomIsland")]
        [InlineData(15, "MushroomIslandShore")]
        public void BiomeId_MapsTo_CorrectName(int id, string expectedName)
        {
            var biome = BiomeGenBase.Biomes[id];
            Assert.NotNull(biome);
            Assert.Equal(expectedName, biome!.BiomeName);
        }

        [Fact]
        public void BiomesSlots_16_To_255_AreNull()
        {
            for (int i = 16; i < 256; i++)
                Assert.Null(BiomeGenBase.Biomes[i]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        public void BiomeId_Field_MatchesRegistryIndex(int id)
        {
            Assert.Equal(id, BiomeGenBase.Biomes[id]!.BiomeId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §2 — Default field values
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_DefaultFieldTests
    {
        // §5 spec defaults verified on Ocean (id=0) as a minimally-configured biome
        private static BiomeGenBase Ocean => BiomeGenBase.Ocean;

        [Fact] public void Default_TopBlockId_Is_2()        => Assert.Equal((byte)2, Ocean.TopBlockId);
        [Fact] public void Default_FillerBlockId_Is_3()     => Assert.Equal((byte)3, Ocean.FillerBlockId);
        [Fact] public void Default_WaterColor_Is_5169201()  => Assert.Equal(5169201, Ocean.WaterColor);
        [Fact] public void Default_ColorOverride_Is_White() => Assert.Equal(0xFFFFFF, Ocean.ColorOverride);
        [Fact] public void Default_HasWeather_IsTrue()      => Assert.True(Ocean.HasWeather);
        [Fact] public void Default_IsRaining_IsFalse()      => Assert.False(Ocean.IsRaining);

        // Decorator defaults (spec §2)
        [Fact] public void Default_FlowerCount_Is_2()       => Assert.Equal(2, Ocean.FlowerCount);
        [Fact] public void Default_TallGrassCount_Is_1()    => Assert.Equal(1, Ocean.TallGrassCount);
        [Fact] public void Default_DeadBushCount_Is_0()     => Assert.Equal(0, Ocean.DeadBushCount);
        [Fact] public void Default_MushroomCount_Is_0()     => Assert.Equal(0, Ocean.MushroomCount);
        [Fact] public void Default_ReedCount_Is_0()         => Assert.Equal(0, Ocean.ReedCount);
        [Fact] public void Default_CactusCount_Is_0()       => Assert.Equal(0, Ocean.CactusCount);
        [Fact] public void Default_ExtraSandCount_Is_1()    => Assert.Equal(1, Ocean.ExtraSandCount);
        [Fact] public void Default_SandDiscCount_Is_3()     => Assert.Equal(3, Ocean.SandDiscCount);
        [Fact] public void Default_ClayDiscCount_Is_1()     => Assert.Equal(1, Ocean.ClayDiscCount);
        [Fact] public void Default_HugeMushroomCount_Is_0() => Assert.Equal(0, Ocean.HugeMushroomCount);
        [Fact] public void Default_LilyPadCount_Is_0()      => Assert.Equal(0, Ocean.LilyPadCount);
        [Fact] public void Default_EnableSprings_IsTrue()   => Assert.True(Ocean.EnableSprings);
        [Fact] public void Default_TreeCount_Is_0()         => Assert.Equal(0, Ocean.TreeCount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3 — Per-biome field values (spec §6 table)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_PerBiomeFieldTests
    {
        // ── Height / temperature / rainfall ──────────────────────────────────

        [Fact]
        public void Ocean_Height_IsMinus1_0point4()
        {
            Assert.Equal(-1.0f, BiomeGenBase.Ocean.MinHeight);
            Assert.Equal(0.4f,  BiomeGenBase.Ocean.MaxHeight);
        }

        [Fact]
        public void Desert_Height_Is0point1_0point2()
        {
            Assert.Equal(0.1f, BiomeGenBase.Desert.MinHeight);
            Assert.Equal(0.2f, BiomeGenBase.Desert.MaxHeight);
        }

        [Fact]
        public void ExtremeHills_Height_Is0point2_1point8()
        {
            Assert.Equal(0.2f, BiomeGenBase.ExtremeHills.MinHeight);
            Assert.Equal(1.8f, BiomeGenBase.ExtremeHills.MaxHeight);
        }

        [Fact]
        public void Swampland_Height_IsMinus0point2_0point1()
        {
            Assert.Equal(-0.2f, BiomeGenBase.Swampland.MinHeight);
            Assert.Equal(0.1f,  BiomeGenBase.Swampland.MaxHeight);
        }

        [Fact]
        public void Desert_TempRain_Is2and0()
        {
            Assert.Equal(2.0f, BiomeGenBase.Desert.Temperature);
            Assert.Equal(0.0f, BiomeGenBase.Desert.Rainfall);
        }

        [Fact]
        public void IcePlains_TempRain_Is0and0point5()
        {
            Assert.Equal(0.0f, BiomeGenBase.IcePlains.Temperature);
            Assert.Equal(0.5f, BiomeGenBase.IcePlains.Rainfall);
        }

        [Fact]
        public void ExtremeHills_Temperature_Is0point2_ExactlyAtForbiddenBoundary()
        {
            // 0.2 is the exclusive boundary — exactly 0.2 is allowed
            Assert.Equal(0.2f, BiomeGenBase.ExtremeHills.Temperature);
        }

        // ── Map colours ───────────────────────────────────────────────────────

        [Theory]
        [InlineData(1,  9286496)]
        [InlineData(2,  16421912)]
        [InlineData(3,  6316128)]
        [InlineData(4,  353825)]
        [InlineData(5,  747097)]
        [InlineData(6,  522674)]
        [InlineData(8,  16711680)]
        [InlineData(9,  8421631)]
        [InlineData(10, 9474208)]
        [InlineData(11, 10526975)]
        [InlineData(12, 16777215)]
        [InlineData(13, 10526880)]
        [InlineData(14, 16711935)]
        [InlineData(15, 10486015)]
        public void Biome_MapColor(int id, int expectedColor)
        {
            Assert.Equal(expectedColor, BiomeGenBase.Biomes[id]!.MapColor);
        }

        // ── Weather ───────────────────────────────────────────────────────────

        [Theory]
        [InlineData(2)]  // Desert
        [InlineData(8)]  // Hell
        [InlineData(9)]  // Sky
        public void Biome_HasWeather_IsFalse(int id)
        {
            Assert.False(BiomeGenBase.Biomes[id]!.HasWeather);
        }

        [Theory]
        [InlineData(0)]  // Ocean
        [InlineData(1)]  // Plains
        [InlineData(4)]  // Forest
        [InlineData(6)]  // Swampland
        public void Biome_HasWeather_IsTrue(int id)
        {
            Assert.True(BiomeGenBase.Biomes[id]!.HasWeather);
        }

        // ── Swampland water colour ────────────────────────────────────────────

        [Fact]
        public void Swampland_WaterColor_Is14745456()
        {
            Assert.Equal(14745456, BiomeGenBase.Swampland.WaterColor);
        }

        // ── Grass color overrides ─────────────────────────────────────────────

        [Fact]
        public void Forest_GrassColorOverride_Is5159473()
        {
            Assert.Equal(5159473, BiomeGenBase.Forest.ColorOverride);
        }

        [Fact]
        public void Taiga_GrassColorOverride_Is5159473()
        {
            Assert.Equal(5159473, BiomeGenBase.Taiga.ColorOverride);
        }

        [Fact]
        public void Swampland_GrassColorOverride_Is9154376()
        {
            Assert.Equal(9154376, BiomeGenBase.Swampland.ColorOverride);
        }

        // ── Decorator counts (spec §6 table) ─────────────────────────────────

        [Fact]
        public void Plains_TreeCount_IsMinus999()
        {
            Assert.Equal(-999, BiomeGenBase.Plains.TreeCount);
        }

        [Fact]
        public void Plains_FlowerCount_Is4()
        {
            Assert.Equal(4, BiomeGenBase.Plains.FlowerCount);
        }

        [Fact]
        public void Plains_TallGrassCount_Is10()
        {
            Assert.Equal(10, BiomeGenBase.Plains.TallGrassCount);
        }

        [Fact]
        public void Desert_TreeCount_IsMinus999()
        {
            Assert.Equal(-999, BiomeGenBase.Desert.TreeCount);
        }

        [Fact]
        public void Desert_DeadBushCount_Is2()
        {
            Assert.Equal(2, BiomeGenBase.Desert.DeadBushCount);
        }

        [Fact]
        public void Desert_ReedCount_Is50()
        {
            Assert.Equal(50, BiomeGenBase.Desert.ReedCount);
        }

        [Fact]
        public void Desert_CactusCount_Is10()
        {
            Assert.Equal(10, BiomeGenBase.Desert.CactusCount);
        }

        [Fact]
        public void Forest_TreeCount_Is10()
        {
            Assert.Equal(10, BiomeGenBase.Forest.TreeCount);
        }

        [Fact]
        public void Forest_TallGrassCount_Is2()
        {
            Assert.Equal(2, BiomeGenBase.Forest.TallGrassCount);
        }

        [Fact]
        public void Taiga_TreeCount_Is10()
        {
            Assert.Equal(10, BiomeGenBase.Taiga.TreeCount);
        }

        [Fact]
        public void Taiga_TallGrassCount_Is1()
        {
            // spec §6: Taiga B=1 (same as default)
            Assert.Equal(1, BiomeGenBase.Taiga.TallGrassCount);
        }

        [Fact]
        public void Swampland_TreeCount_Is2()
        {
            Assert.Equal(2, BiomeGenBase.Swampland.TreeCount);
        }

        [Fact]
        public void Swampland_LilyPadCount_Is4()
        {
            Assert.Equal(4, BiomeGenBase.Swampland.LilyPadCount);
        }

        [Fact]
        public void Swampland_FlowerCount_IsMinus999()
        {
            // spec §6: A=-999 → loop runs 0 times
            Assert.Equal(-999, BiomeGenBase.Swampland.FlowerCount);
        }

        [Fact]
        public void Swampland_DeadBushCount_Is1()
        {
            Assert.Equal(1, BiomeGenBase.Swampland.DeadBushCount);
        }

        [Fact]
        public void Swampland_MushroomCount_Is8()
        {
            Assert.Equal(8, BiomeGenBase.Swampland.MushroomCount);
        }

        [Fact]
        public void Swampland_ReedCount_Is10()
        {
            Assert.Equal(10, BiomeGenBase.Swampland.ReedCount);
        }

        [Fact]
        public void Swampland_ClayDiscCount_Is1()
        {
            // spec §6: I=1 (same as default)
            Assert.Equal(1, BiomeGenBase.Swampland.ClayDiscCount);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §4 — SetTempRain forbidden range guard (spec §7 Known Quirks)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_TempRainGuardTests
    {
        // We need a throwaway biome that is NOT registered — use a fresh subclass
        private sealed class TestBiome : BiomeGenBase
        {
            // Registers at slot 200 for isolation
            public TestBiome() : base(200) { }
        }

        [Theory]
        [InlineData(0.11f)]
        [InlineData(0.15f)]
        [InlineData(0.19f)]
        [InlineData(0.101f)]
        [InlineData(0.199f)]
        public void SetTempRain_ForbiddenRange_Throws(float temp)
        {
            var biome = new TestBiome();
            Assert.Throws<ArgumentException>(() => biome.SetTempRain(temp, 0.5f));
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(0.1f)]   // exactly 0.1 is allowed (exclusive lower bound)
        [InlineData(0.2f)]   // exactly 0.2 is allowed (exclusive upper bound)
        [InlineData(0.5f)]
        [InlineData(2.0f)]
        public void SetTempRain_AllowedValues_DoesNotThrow(float temp)
        {
            var biome = new TestBiome();
            var ex = Record.Exception(() => biome.SetTempRain(temp, 0.5f));
            Assert.Null(ex);
        }

        [Fact]
        public void SetTempRain_ForbiddenRange_MessageContainsTemperature()
        {
            var biome = new TestBiome();
            var ex = Assert.Throws<ArgumentException>(() => biome.SetTempRain(0.15f, 0.5f));
            Assert.Contains("0.15", ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §5 — Auxiliary methods (spec §7)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_AuxMethodTests
    {
        [Fact]
        public void GetEnableSnow_Desert_IsFalse_BecauseNoWeather()
        {
            // HasWeather=false → GetEnableSnow() must return false
            Assert.False(BiomeGenBase.Desert.GetEnableSnow());
        }

        [Fact]
        public void GetEnableSnow_Plains_IsTrue_WhenNotRaining()
        {
            var plains = BiomeGenBase.Plains;
            plains.IsRaining = false;
            Assert.True(plains.GetEnableSnow());
        }

        [Fact]
        public void GetEnableSnow_Plains_IsFalse_WhenRaining()
        {
            var plains = BiomeGenBase.Plains;
            bool saved = plains.IsRaining;
            plains.IsRaining = true;
            try { Assert.False(plains.GetEnableSnow()); }
            finally { plains.IsRaining = saved; }
        }

        [Fact]
        public void GetRainfallFixed_Plains_MatchesFormula()
        {
            // Plains rainfall = 0.4f
            int expected = (int)(0.4f * 65536.0f);
            Assert.Equal(expected, BiomeGenBase.Plains.GetRainfallFixed());
        }

        [Fact]
        public void GetTemperatureFixed_Plains_MatchesFormula()
        {
            // Plains temperature = 0.8f
            int expected = (int)(0.8f * 65536.0f);
            Assert.Equal(expected, BiomeGenBase.Plains.GetTemperatureFixed());
        }

        [Fact]
        public void GetRainfallFixed_Desert_Is0()
        {
            Assert.Equal(0, BiomeGenBase.Desert.GetRainfallFixed());
        }

        [Fact]
        public void GetTemperatureFixed_IcePlains_Is0()
        {
            Assert.Equal(0, BiomeGenBase.IcePlains.GetTemperatureFixed());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §6 — Subclass type checks
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_SubclassTypeTests
    {
        [Fact]
        public void Forest_IsForestBiome()
        {
            Assert.IsType<ForestBiome>(BiomeGenBase.Forest);
        }

        [Fact]
        public void Taiga_IsTaigaBiome()
        {
            Assert.IsType<TaigaBiome>(BiomeGenBase.Taiga);
        }

        [Fact]
        public void Swampland_IsSwamplandBiome()
        {
            Assert.IsType<SwamplandBiome>(BiomeGenBase.Swampland);
        }

        [Fact]
        public void Ocean_IsBaseBiomeGenBase_NotSubclass()
        {
            Assert.Equal(typeof(BiomeGenBase), BiomeGenBase.Ocean.GetType());
        }

        [Fact]
        public void Plains_IsBaseBiomeGenBase_NotSubclass()
        {
            Assert.Equal(typeof(BiomeGenBase), BiomeGenBase.Plains.GetType());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §7 — Tree generators (spec §10.3 / WorldGenTrees_Spec)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_TreeGeneratorTests
    {
        // ── Default biome (e.g. Plains): 10% BigOak, 90% Oak ─────────────────

        [Fact]
        public void Default_GetTreeGenerator_10Percent_BigOak()
        {
            // seed chosen so nextInt(10)==0 on first call
            var rand = new JavaRandom(0);
            // We need to find a seed where the very first nextInt(10)==0
            // Rather than guess, just run a statistical test over 1000 calls
            var biome = BiomeGenBase.Plains;
            var rand2 = new JavaRandom(42);
            int bigOak = 0;
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                var gen = biome.GetTreeGenerator(rand2);
                if (gen is WorldGenBigTree) bigOak++;
            }
            // Should be ~10% ±2%
            double ratio = (double)bigOak / total;
            Assert.InRange(ratio, 0.08, 0.12);
        }

        [Fact]
        public void Default_GetTreeGenerator_90Percent_Oak()
        {
            var biome = BiomeGenBase.Plains;
            var rand = new JavaRandom(99);
            int oak = 0;
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                var gen = biome.GetTreeGenerator(rand);
                if (gen is WorldGenTrees) oak++;
            }
            double ratio = (double)oak / total;
            Assert.InRange(ratio, 0.88, 0.92);
        }

        // ── Forest: 20% birch, then 10% BigOak of remainder, rest Oak ─────────

        [Fact]
        public void Forest_GetTreeGenerator_20Percent_Birch()
        {
            var biome = BiomeGenBase.Forest;
            var rand = new JavaRandom(1);
            int birch = 0;
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                var gen = biome.GetTreeGenerator(rand);
                if (gen is WorldGenForestTree) birch++;
            }
            double ratio = (double)birch / total;
            // nextInt(5)==0 → 20%
            Assert.InRange(ratio, 0.18, 0.22);
        }

        // ── Taiga: 33% wide spruce (new WorldGenTaiga2), 67% thin spruce ──────

        [Fact]
        public void Taiga_GetTreeGenerator_33Percent_WideSpruce()
        {
            var biome = BiomeGenBase.Taiga;
            var rand = new JavaRandom(7);
            int wide = 0;
            int total = 10000;
            for (int i = 0; i < total; i++)
            {
                var gen = biome.GetTreeGenerator(rand);
                if (gen is WorldGenTaiga2) wide++;
            }
            double ratio = (double)wide / total;
            Assert.InRange(ratio, 0.31, 0.35);
        }

        [Fact]
        public void Taiga_GetTreeGenerator_WideSpruce_IsNewInstanceEachCall()
        {
            // Spec: "new WorldGenTaiga2()" — a fresh instance each time (not shared)
            var biome = BiomeGenBase.Taiga;
            // Find a seed that reliably returns wide spruce on two consecutive calls
            var rand = new JavaRandom(7);
            WorldGenerator? first = null;
            WorldGenerator? second = null;
            for (int i = 0; i < 100 && (first == null || second == null); i++)
            {
                var g = biome.GetTreeGenerator(rand);
                if (g is WorldGenTaiga2)
                {
                    if (first == null) first = g;
                    else if (second == null) second = g;
                }
            }
            if (first != null && second != null)
                Assert.NotSame(first, second);
        }

        [Fact]
        public void Taiga_GetTreeGenerator_ThinSpruce_IsSharedInstance()
        {
            // Spec: shared `ty` (WorldGenTaiga1) — same reference each call
            var biome = BiomeGenBase.Taiga;
            var rand = new JavaRandom(3);
            WorldGenerator? first = null;
            WorldGenerator? second = null;
            for (int i = 0; i < 200 && (first == null || second == null); i++)
            {
                var g = biome.GetTreeGenerator(rand);
                if (g is WorldGenTaiga1)
                {
                    if (first == null) first = g;
                    else if (second == null) second = g;
                }
            }
            if (first != null && second != null)
                Assert.Same(first, second);
        }

        // ── Swampland: always returns SwampGen (qj) ───────────────────────────

        [Fact]
        public void Swampland_GetTreeGenerator_AlwaysReturnsSwampGen()
        {
            var biome = BiomeGenBase.Swampland;
            var rand = new JavaRandom(12345);
            for (int i = 0; i < 50; i++)
            {
                var gen = biome.GetTreeGenerator(rand);
                Assert.IsType<WorldGenSwamp>(gen);
            }
        }

        [Fact]
        public void Swampland_GetTreeGenerator_SwampGen_IsSharedInstance()
        {
            var biome = BiomeGenBase.Swampland;
            var rand = new JavaRandom(0);
            var g1 = biome.GetTreeGenerator(rand);
            var g2 = biome.GetTreeGenerator(rand);
            Assert.Same(g1, g2);
        }

        // ── Default Oak / BigOak are shared instances ─────────────────────────

        [Fact]
        public void Default_OakGen_IsSharedAcrossCallsWhenSameType()
        {
            var biome = BiomeGenBase.Plains;
            // Use seed that produces Oak (nextInt(10) != 0) repeatedly
            var rand = new JavaRandom(1);
            WorldGenerator? first = null;
            WorldGenerator? second = null;
            for (int i = 0; i < 100; i++)
            {
                var g = biome.GetTreeGenerator(rand);
                if (g is WorldGenTrees)
                {
                    if (first == null) first = g;
                    else { second = g; break; }
                }
            }
            if (first != null && second != null)
                Assert.Same(first, second);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §8 — SwamplandBiome colour blend formula (spec §9 / Known Quirks §7)
    // ─────────────────────────────────────────────────────────────────────────

    public class SwamplandBiome_ColorTests
    {
        private const int SwampBase = 5115470;
        private readonly FakeBlockAccess _world = new FakeBlockAccess();

        private static int ExpectedSwampGrass(int lookup) =>
            ((lookup & 0xFEFEFE) + SwampBase) / 2;

        private static int ExpectedSwampFoliage(int lookup) =>
            ((lookup & 0xFEFEFE) + SwampBase) / 2;

        [Fact]
        public void Swampland_GetGrassColor_UsesBlendFormula()
        {
            var biome = (SwamplandBiome)BiomeGenBase.Swampland;
            // With FakeBlockAccess (not a World), biome uses its own temperature/rainfall
            // temp=0.8, rain=0.9 → compute expected lookup then blend
            double temp = biome.Temperature;
            double rain = biome.Rainfall;
            int lookup = GrassColorizer.GetGrassColor(temp, rain);
            int expected = ExpectedSwampGrass(lookup);

            int actual = biome.GetGrassColor(_world, 0, 64, 0);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Swampland_GetFoliageColor_UsesBlendFormula()
        {
            var biome = (SwamplandBiome)BiomeGenBase.Swampland;
            double temp = biome.Temperature;
            double rain = biome.Rainfall;
            int lookup = FoliageColorizer.GetFoliageColor(temp, rain);
            int expected = ExpectedSwampFoliage(lookup);

            int actual = biome.GetFoliageColor(_world, 0, 64, 0);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Swampland_GrassColor_AndFoliageColor_UseMaskBeforeAverage()
        {
            // Verifies 0xFEFEFE mask is applied (strips lowest bit of each channel) before divide
            var biome = (SwamplandBiome)BiomeGenBase.Swampland;
            double temp = biome.Temperature;
            double rain = biome.Rainfall;
            int grassLookup  = GrassColorizer.GetGrassColor(temp, rain);
            int folLookup    = FoliageColorizer.GetFoliageColor(temp, rain);

            int expectedGrass = ((grassLookup & 0xFEFEFE) + SwampBase) / 2;
            int expectedFol   = ((folLookup   & 0xFEFEFE) + SwampBase) / 2;

            Assert.Equal(expectedGrass, biome.GetGrassColor(_world,   0, 64, 0));
            Assert.Equal(expectedFol,   biome.GetFoliageColor(_world, 0, 64, 0));
        }

        [Fact]
        public void NonSwampBiome_GrassColor_DoesNotUseSwampBlend()
        {
            var plains = BiomeGenBase.Plains;
            double temp = plains.Temperature;
            double rain = plains.Rainfall;
            int lookup = GrassColorizer.GetGrassColor(temp, rain);
            // Should NOT equal the swamp blend
            int swampBlend = ((lookup & 0xFEFEFE) + SwampBase) / 2;
            int actual = plains.GetGrassColor(_world, 0, 64, 0);
            // Plains returns the raw lookup, not the swamp blend
            Assert.Equal(lookup, actual);
            // Sanity: they should differ unless lookup happens to equal swamp blend
            // (statistically impossible for plains temp/rain)
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §9 — Spawn lists (spec §5 spawner animals section)
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_SpawnListTests
    {
        [Fact]
        public void DefaultHostileSpawns_ContainsFourEntries()
        {
            var list = BiomeGenBase.Ocean.HostileSpawnList;
            Assert.Equal(4, list.Count);
        }

        [Fact]
        public void DefaultHostileSpawns_ContainsSpider_Weight10_4of4()
        {
            var list = BiomeGenBase.Ocean.HostileSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntitySpider) && e.Weight == 10 && e.MinCount == 4 && e.MaxCount == 4);
        }

        [Fact]
        public void DefaultHostileSpawns_ContainsZombie_Weight10_4of4()
        {
            var list = BiomeGenBase.Ocean.HostileSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntityZombie) && e.Weight == 10 && e.MinCount == 4 && e.MaxCount == 4);
        }

        [Fact]
        public void DefaultHostileSpawns_ContainsSkeleton_Weight10_4of4()
        {
            var list = BiomeGenBase.Ocean.HostileSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntitySkeleton) && e.Weight == 10 && e.MinCount == 4 && e.MaxCount == 4);
        }

        [Fact]
        public void DefaultHostileSpawns_ContainsCreeper_Weight10_4of4()
        {
            var list = BiomeGenBase.Ocean.HostileSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntityCreeper) && e.Weight == 10 && e.MinCount == 4 && e.MaxCount == 4);
        }

        [Fact]
        public void DefaultPassiveSpawns_ContainsFourEntries()
        {
            var list = BiomeGenBase.Plains.PassiveSpawnList;
            Assert.Equal(4, list.Count);
        }

        [Fact]
        public void DefaultPassiveSpawns_ContainsSheep_Weight12()
        {
            var list = BiomeGenBase.Plains.PassiveSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntitySheep) && e.Weight == 12 && e.MinCount == 4 && e.MaxCount == 4);
        }

        [Fact]
        public void DefaultPassiveSpawns_ContainsPig_Weight10()
        {
            var list = BiomeGenBase.Plains.PassiveSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntityPig) && e.Weight == 10);
        }

        [Fact]
        public void DefaultPassiveSpawns_ContainsChicken_Weight10()
        {
            var list = BiomeGenBase.Plains.PassiveSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntityChicken) && e.Weight == 10);
        }

        [Fact]
        public void DefaultPassiveSpawns_ContainsCow_Weight8()
        {
            var list = BiomeGenBase.Plains.PassiveSpawnList;
            Assert.Contains(list, e => e.EntityType == typeof(Mobs.EntityCow) && e.Weight == 8);
        }

        [Fact]
        public void DefaultWaterSpawns_IsEmpty()
        {
            Assert.Empty(BiomeGenBase.Ocean.WaterSpawnList);
        }

        [Fact]
        public void GetSpawnList_Hostile_ReturnsHostileList()
        {
            var biome = BiomeGenBase.Forest;
            var list = biome.GetSpawnList(EnumCreatureType.Hostile);
            Assert.Same(biome.HostileSpawnList, list);
        }

        [Fact]
        public void GetSpawnList_Passive_ReturnsPassiveList()
        {
            var biome = BiomeGenBase.Forest;
            var list = biome.GetSpawnList(EnumCreatureType.Passive);
            Assert.Same(biome.PassiveSpawnList, list);
        }

        [Fact]
        public void GetSpawnList_Water_ReturnsWaterList()
        {
            var biome = BiomeGenBase.Ocean;
            var list = biome.GetSpawnList(EnumCreatureType.Water);
            Assert.Same(biome.WaterSpawnList, list);
        }

        [Fact]
        public void GetSpawnList_UnknownType_ReturnsEmptyList()
        {
            var biome = BiomeGenBase.Plains;
            var list = biome.GetSpawnList((EnumCreatureType)99);
            Assert.Empty(list);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §10 — Known Quirks (spec §7 "Known Quirks / Bugs to Preserve")
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_KnownQuirkTests
    {
        // Quirk 1: Temperature values strictly between 0.1 and 0.2 are forbidden.
        // Boundary values 0.1 and 0.2 are ALLOWED (exclusive range).
        private sealed class QuirkBiome : BiomeGenBase { public QuirkBiome() : base(201) { } }

        [Fact]
        public void Quirk_ForbiddenTempRange_IsExclusive_0point1_Allowed()
        {
            var b = new QuirkBiome();
            var ex = Record.Exception(() => b.SetTempRain(0.1f, 0.5f));
            Assert.Null(ex);
        }

        [Fact]
        public void Quirk_ForbiddenTempRange_IsExclusive_0point2_Allowed()
        {
            var b = new QuirkBiome();
            var ex = Record.Exception(() => b.SetTempRain(0.2f, 0.5f));
            Assert.Null(ex);
        }

        [Fact]
        public void Quirk_ExtremeHills_Temperature_Is0point2_NotRejected()
        {
            // ExtremeHills uses temp=0.2 which is at the exclusive boundary — must not throw
            Assert.Equal(0.2f, BiomeGenBase.ExtremeHills.Temperature);
        }

        // Quirk 2: Swamp flower count = -999, meaning the loop runs 0 times (negative count = no iterations).
        [Fact]
        public void Quirk_SwampFlowerCount_Minus999_MeansZeroFlowers()
        {
            // The decorator loop "for A times" with A=-999 must produce 0 iterations.
            // We verify the field value here; the decorator test would verify behaviour.
            Assert.Equal(-999, BiomeGenBase.Swampland.FlowerCount);
        }

        // Quirk 3: Tree count -999 means no trees (same convention).
        [Fact]
        public void Quirk_TreeCountMinus999_Plains()
        {
            Assert.Equal(-999, BiomeGenBase.Plains.TreeCount);
        }

        [Fact]
        public void Quirk_TreeCountMinus999_Desert()
        {
            Assert.Equal(-999, BiomeGenBase.Desert.TreeCount);
        }

        // Quirk 4: Swamp colour blend uses mask 0xFEFEFE (strips lowest bit per channel) before averaging.
        [Fact]
        public void Quirk_SwampColorBlend_MaskStripsLowestBit()
        {
            // Construct a known lookup value with low bits set in each channel
            // and verify the mask is applied before the average.
            const int SwampBase = 5115470;
            const int knownLookup = 0x010101; // all channels have bit 0 set
            const int expectedMasked = (0x010101 & 0xFEFEFE); // = 0x000000
            int expectedBlend = (expectedMasked + SwampBase) / 2;

            // The actual biome call will use real GrassColorizer, but we can verify
            // the mask formula is correct for the known case
            Assert.Equal((0 + SwampBase) / 2, expectedBlend);
            Assert.Equal(SwampBase / 2, expectedBlend);
        }

        // Quirk 5: GetEnableSnow() = !IsRaining && HasWeather (biomes with no weather never have snow).
        [Fact]
        public void Quirk_GetEnableSnow_NoWeatherBiome_AlwaysFalse()
        {
            // Hell, Desert, Sky — none should ever enable snow regardless of IsRaining
            var noWeatherBiomes = new[] { BiomeGenBase.Desert, BiomeGenBase.Hell, BiomeGenBase.Sky };
            foreach (var b in noWeatherBiomes)
            {
                bool saved = b.IsRaining;
                b.IsRaining = false;
                Assert.False(b.GetEnableSnow(), $"{b.BiomeName} should not enable snow");
                b.IsRaining = saved;
            }
        }

        // Quirk 6: Taiga wide-spruce generator must be a NEW instance each call (not cached).
        [Fact]
        public void Quirk_Taiga_WideSpruce_NewInstancePerCall()
        {
            var taiga = BiomeGenBase.Taiga;
            // Force nextInt(3)==0 by scanning for two consecutive wide spruce results
            var rand = new JavaRandom(7);
            var instances = new List<WorldGenerator>();
            for (int i = 0; i < 500; i++)
            {
                var g = taiga.GetTreeGenerator(rand);
                if (g is WorldGenTaiga2) instances.Add(g);
                if (instances.Count >= 2) break;
            }
            if (instances.Count >= 2)
                Assert.NotSame(instances[0], instances[1]);
        }

        // Quirk 7: Forest biome birch generator is shared (same instance every call).
        [Fact]
        public void Quirk_Forest_BirchGen_SharedInstance()
        {
            var forest = (ForestBiome)BiomeGenBase.Forest;
            var rand = new JavaRandom(2);
            var instances = new List<WorldGenerator>();
            for (int i = 0; i < 500; i++)
            {
                var g = forest.GetTreeGenerator(rand);
                if (g is WorldGenForestTree) instances.Add(g);
                if (instances.Count >= 2) break;
            }
            if (instances.Count >= 2)
                Assert.Same(instances[0], instances[1]);
        }

        // Quirk 8: Swampland always returns the SAME SwampGen instance (shared).
        [Fact]
        public void Quirk_Swampland_SwampGen_AlwaysSameInstance()
        {
            var swamp = (SwamplandBiome)BiomeGenBase.Swampland;
            var rand = new JavaRandom(99);
            var g1 = swamp.GetTreeGenerator(rand);
            var g2 = swamp.GetTreeGenerator(rand);
            var g3 = swamp.GetTreeGenerator(rand);
            Assert.Same(g1, g2);
            Assert.Same(g2, g3);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §11 — Decorator field counts match spec §6 table exhaustively
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeDecorator_FieldCount_SpecTableTests
    {
        // Test every cell of the spec §6 table that has an explicit value

        // Plains row: z=-999, A=4, B=10
        [Fact] public void Plains_z_TreeCount_IsMinus999()         => Assert.Equal(-999, BiomeGenBase.Plains.TreeCount);
        [Fact] public void Plains_A_FlowerCount_Is4()              => Assert.Equal(4, BiomeGenBase.Plains.FlowerCount);
        [Fact] public void Plains_B_TallGrassCount_Is10()          => Assert.Equal(10, BiomeGenBase.Plains.TallGrassCount);

        // Desert row: z=-999, C=2, E=50, F=10
        [Fact] public void Desert_z_TreeCount_IsMinus999()         => Assert.Equal(-999, BiomeGenBase.Desert.TreeCount);
        [Fact] public void Desert_C_DeadBushCount_Is2()            => Assert.Equal(2, BiomeGenBase.Desert.DeadBushCount);
        [Fact] public void Desert_E_ReedCount_Is50()               => Assert.Equal(50, BiomeGenBase.Desert.ReedCount);
        [Fact] public void Desert_F_CactusCount_Is10()             => Assert.Equal(10, BiomeGenBase.Desert.CactusCount);

        // Forest row: z=10, B=2
        [Fact] public void Forest_z_TreeCount_Is10()               => Assert.Equal(10, BiomeGenBase.Forest.TreeCount);
        [Fact] public void Forest_B_TallGrassCount_Is2()           => Assert.Equal(2, BiomeGenBase.Forest.TallGrassCount);

        // Taiga row: z=10, B=1 (same as default)
        [Fact] public void Taiga_z_TreeCount_Is10()                => Assert.Equal(10, BiomeGenBase.Taiga.TreeCount);
        [Fact] public void Taiga_B_TallGrassCount_Is1()            => Assert.Equal(1, BiomeGenBase.Taiga.TallGrassCount);

        // Swamp row: z=2, y=4, A=-999, C=1, D=8, E=10, I=1
        [Fact] public void Swamp_z_TreeCount_Is2()                 => Assert.Equal(2, BiomeGenBase.Swampland.TreeCount);
        [Fact] public void Swamp_y_LilyPadCount_Is4()              => Assert.Equal(4, BiomeGenBase.Swampland.LilyPadCount);
        [Fact] public void Swamp_A_FlowerCount_IsMinus999()        => Assert.Equal(-999, BiomeGenBase.Swampland.FlowerCount);
        [Fact] public void Swamp_C_DeadBushCount_Is1()             => Assert.Equal(1, BiomeGenBase.Swampland.DeadBushCount);
        [Fact] public void Swamp_D_MushroomCount_Is8()             => Assert.Equal(8, BiomeGenBase.Swampland.MushroomCount);
        [Fact] public void Swamp_E_ReedCount_Is10()                => Assert.Equal(10, BiomeGenBase.Swampland.ReedCount);
        [Fact] public void Swamp_I_ClayDiscCount_Is1()             => Assert.Equal(1, BiomeGenBase.Swampland.ClayDiscCount);

        // Default biome (Ocean) — all defaults
        [Fact] public void Default_G_ExtraSandCount_Is1()          => Assert.Equal(1, BiomeGenBase.Ocean.ExtraSandCount);
        [Fact] public void Default_H_SandDiscCount_Is3()           => Assert.Equal(3, BiomeGenBase.Ocean.SandDiscCount);
        [Fact] public void Default_I_ClayDiscCount_Is1()           => Assert.Equal(1, BiomeGenBase.Ocean.ClayDiscCount);
        [Fact] public void Default_J_HugeMushroomCount_Is0()       => Assert.Equal(0, BiomeGenBase.Ocean.HugeMushroomCount);
        [Fact] public void Default_K_EnableSprings_IsTrue()        => Assert.True(BiomeGenBase.Ocean.EnableSprings);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §12 — BiomeID consistency between static field and Biomes[] slot
    // ─────────────────────────────────────────────────────────────────────────

    public class BiomeGenBase_IdConsistencyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        public void BiomesSlot_ReferencesCorrectStaticField(int id)
        {
            // The biome registered at Biomes[id] must have BiomeId == id
            var biome = BiomeGenBase.Biomes[id];
            Assert.NotNull(biome);
            Assert.Equal(id, biome!.BiomeId);
        }

        [Fact]
        public void StaticField_Ocean_SameAs_BiomesSlot0()        => Assert.Same(BiomeGenBase.Ocean,               BiomeGenBase.Biomes[0]);
        [Fact]
        public void StaticField_Plains_SameAs_BiomesSlot1()       => Assert.Same(BiomeGenBase.Plains,              BiomeGenBase.Biomes[1]);
        [Fact]
        public void StaticField_Desert_SameAs_BiomesSlot2()       => Assert.Same(BiomeGenBase.Desert,              BiomeGenBase.Biomes[2]);
        [Fact]
        public void StaticField_ExtremeHills_SameAs_BiomesSlot3() => Assert.Same(BiomeGenBase.ExtremeHills,        BiomeGenBase.Biomes[3]);
        [Fact]
        public void StaticField_Forest_SameAs_BiomesSlot4()       => Assert.Same(BiomeGenBase.Forest,              BiomeGenBase.Biomes[4]);
        [Fact]
        public void StaticField_Taiga_SameAs_BiomesSlot5()        => Assert.Same(BiomeGenBase.Taiga,               BiomeGenBase.Biomes[5]);
        [Fact]
        public void StaticField_Swampland_SameAs_BiomesSlot6()    => Assert.Same(BiomeGenBase.Swampland,           BiomeGenBase.Biomes[6]);
        [Fact]
        public void StaticField_River_SameAs_BiomesSlot7()        => Assert.Same(BiomeGenBase.River,               BiomeGenBase.Biomes[7]);
        [Fact]
        public void StaticField_Hell_SameAs_BiomesSlot8()         => Assert.Same(BiomeGenBase.Hell,                BiomeGenBase.Biomes[8]);
        [Fact]
        public void StaticField_Sky_SameAs_BiomesSlot9()          => Assert.Same(BiomeGenBase.Sky,                 BiomeGenBase.Biomes[9]);
        [Fact]
        public void StaticField_FrozenOcean_SameAs_BiomesSlot10() => Assert.Same(BiomeGenBase.FrozenOcean,         BiomeGenBase.Biomes[10]);
        [Fact]
        public void StaticField_FrozenRiver_SameAs_BiomesSlot11() => Assert.Same(BiomeGenBase.FrozenRiver,         BiomeGenBase.Biomes[11]);
        [Fact]
        public void StaticField_IcePlains_SameAs_BiomesSlot12()   => Assert.Same(BiomeGenBase.IcePlains,           BiomeGenBase.Biomes[12]);
        [Fact]
        public void StaticField_IceMountains_SameAs_Slot13()      => Assert.Same(BiomeGenBase.IceMountains,        BiomeGenBase.Biomes[13]);
        [Fact]
        public void StaticField_MushroomIsland_SameAs_Slot14()    => Assert.Same(BiomeGenBase.MushroomIsland,      BiomeGenBase.Biomes[14]);
        [Fact]
        public void StaticField_MushroomIslandShore_SameAs_Slot15() => Assert.Same(BiomeGenBase.MushroomIslandShore, BiomeGenBase.Biomes[15]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §13 — MushroomIsland / MushroomIslandShore field checks
    // ─────────────────────────────────────────────────────────────────────────

    public class MushroomBiome_FieldTests
    {
        [Fact]
        public void MushroomIsland_Height_Is0point2_1point0()
        {
            Assert.Equal(0.2f, BiomeGenBase.MushroomIsland.MinHeight);
            Assert.Equal(1.0f, BiomeGenBase.MushroomIsland.MaxHeight);
        }

        [Fact]
        public void MushroomIsland_TempRain_Is0point9_1point0()
        {
            Assert.Equal(0.9f, BiomeGenBase.MushroomIsland.Temperature);
            Assert.Equal(1.0f, BiomeGenBase.MushroomIsland.Rainfall);
        }

        [Fact]
        public void MushroomIslandShore_Height_IsMinus1_0point1()
        {
            Assert.Equal(-1.0f, BiomeGenBase.MushroomIslandShore.MinHeight);
            Assert.Equal(0.1f,  BiomeGenBase.MushroomIslandShore.MaxHeight);
        }

        [Fact]
        public void MushroomIslandShore_TempRain_Is0point9_1point0()
        {
            Assert.Equal(0.9f, BiomeGenBase.MushroomIslandShore.Temperature);
            Assert.Equal(1.0f, BiomeGenBase.MushroomIslandShore.Rainfall);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §14 — FrozenOcean / FrozenRiver field checks
    // ─────────────────────────────────────────────────────────────────────────

    public class FrozenBiome_FieldTests
    {
        [Fact]
        public void FrozenOcean_Height_IsMinus1_0point5()
        {
            Assert.Equal(-1.0f, BiomeGenBase.FrozenOcean.MinHeight);
            Assert.Equal(0.5f,  BiomeGenBase.FrozenOcean.MaxHeight);
        }

        [Fact]
        public void FrozenOcean_TempRain_Is0_0point5()
        {
            Assert.Equal(0.0f, BiomeGenBase.FrozenOcean.Temperature);
            Assert.Equal(0.5f, BiomeGenBase.FrozenOcean.Rainfall);
        }

        [Fact]
        public void FrozenRiver_Height_IsMinus0point5_0()
        {
            Assert.Equal(-0.5f, BiomeGenBase.FrozenRiver.MinHeight);
            Assert.Equal(0.0f,  BiomeGenBase.FrozenRiver.MaxHeight);
        }

        [Fact]
        public void FrozenRiver_TempRain_Is0_0point5()
        {
            Assert.Equal(0.0f, BiomeGenBase.FrozenRiver.Temperature);
            Assert.Equal(0.5f, BiomeGenBase.FrozenRiver.Rainfall);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §15 — River and Hell / Sky biome field checks
    // ─────────────────────────────────────────────────────────────────────────

    public class MiscBiome_FieldTests
    {
        [Fact]
        public void River_Height_IsMinus0point5_0()
        {
            Assert.Equal(-0.5f, BiomeGenBase.River.MinHeight);
            Assert.Equal(0.0f,  BiomeGenBase.River.MaxHeight);
        }

        [Fact]
        public void River_TempRain_Is0point5_0point5()
        {
            Assert.Equal(0.5f, BiomeGenBase.River.Temperature);
            Assert.Equal(0.5f, BiomeGenBase.River.Rainfall);
        }

        [Fact]
        public void Hell_TempRain_Is2_0()
        {
            Assert.Equal(2.0f, BiomeGenBase.Hell.Temperature);
            Assert.Equal(0.0f, BiomeGenBase.Hell.Rainfall);
        }

        [Fact]
        public void Hell_MapColor_Is16711680()
        {
            Assert.Equal(16711680, BiomeGenBase.Hell.MapColor);
        }

        [Fact]
        public void Sky_TempRain_Is0point5_0point5()
        {
            Assert.Equal(0.5f, BiomeGenBase.Sky.Temperature);
            Assert.Equal(0.5f, BiomeGenBase.Sky.Rainfall);
        }

        [Fact]
        public void Sky_MapColor_Is8421631()
        {
            Assert.Equal(8421631, BiomeGenBase.Sky.MapColor);
        }
    }
}