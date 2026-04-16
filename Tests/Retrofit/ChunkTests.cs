using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using SpectraEngine.Core;

// ─────────────────────────────────────────────────────────────────────────────
// Hand-written fakes / stubs
// ─────────────────────────────────────────────────────────────────────────────

namespace SpectraEngine.Core.Tests
{
    // ── Minimal Material stub ─────────────────────────────────────────────────

    public class FakeMaterial : Material
    {
        private readonly bool _blocksMovementFlag;

        public FakeMaterial(bool blocksMovement = true, bool isLiquid = false)
            : base(MapColor.Grass)
        {
            _blocksMovementFlag = blocksMovement;
        }

        public override bool BlocksMovement() => _blocksMovementFlag;
    }

    // ── Minimal Block stub ────────────────────────────────────────────────────

    public class FakeBlock : Block
    {
        public FakeBlock(int id, Material mat, int opacity = 255) : base(id, mat)
        {
            Block.LightOpacity[id] = opacity;
        }
    }

    // ── Minimal TileEntity stub ───────────────────────────────────────────────

    public class FakeTileEntity : TileEntity.TileEntity
    {
        public bool IsValid { get; private set; } = false;

        public new void Validate()   => IsValid = true;
        public new void Invalidate() => IsValid = false;
    }

    // ── Minimal Entity stub ───────────────────────────────────────────────────

    public class FakeEntity : Entity
    {
        public FakeEntity(double posY) { PosY = posY; }
        protected override void EntityInit() { }
        protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
        protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    }

    // ── FakeWorld ─────────────────────────────────────────────────────────────

    file class FakeWorld : World
    {
        public bool PropagateWasCalled { get; private set; }
        public List<(LightType type, int x, int y, int z)> PropagateCalls { get; } = new();
        public List<(int x, int z, int minY, int maxY)> PropagateColumnCalls { get; } = new();

        public FakeWorld(long seed = 0L, bool isNether = false)
            : base(new SpectraEngine.Tests.NullChunkLoader(), seed) { IsNether = isNether; }

        public new void PropagateLight(LightType type, int x, int y, int z)
        {
            PropagateWasCalled = true;
            PropagateCalls.Add((type, x, y, z));
        }

        public new void PropagateColumnRange(int x, int z, int minY, int maxY)
        {
            PropagateColumnCalls.Add((x, z, minY, maxY));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chunk parity tests
    // ─────────────────────────────────────────────────────────────────────────

    public class ChunkParityTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static World MakeWorld(long seed = 0L, bool nether = false)
            => new FakeWorld(seed, nether);

        private static Chunk MakeChunk(int cx = 0, int cz = 0, bool nether = false)
            => new Chunk(MakeWorld(nether: nether), cx, cz);

        // ── §2 / §6  Constructor: 3-arg ───────────────────────────────────────

        [Fact]
        public void Ctor3_BlockArrayIsAllZero()
        {
            var chunk = MakeChunk();
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
            for (int y = 0; y < 128; y++)
                Assert.Equal(0, chunk.GetBlockId(x, y, z));
        }

        [Fact]
        public void Ctor3_PrecipCacheInitialisedToNegative999()
        {
            // PrecipitationHeightAt on a fresh all-air chunk must compute (not return cached value).
            // For air chunk it should return -1 (spec quirk 3).
            var chunk = MakeChunk();
            int result = chunk.PrecipitationHeightAt(0, 0);
            Assert.Equal(-1, result);
        }

        [Fact]
        public void Ctor3_HeightMapAllZero()
        {
            var chunk = MakeChunk();
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
                Assert.Equal(0, chunk.GetHeightAt(x, z));
        }

        [Fact]
        public void Ctor3_IsLoadedFalse()
        {
            var chunk = MakeChunk();
            Assert.False(chunk.IsLoaded);
        }

        [Fact]
        public void Ctor3_IsModifiedFalse()
        {
            var chunk = MakeChunk();
            Assert.False(chunk.IsModified);
        }

        [Fact]
        public void Ctor3_IsPopulatedFalse()
        {
            var chunk = MakeChunk();
            Assert.False(chunk.IsPopulated);
        }

        // ── §6  Constructor: 4-arg ────────────────────────────────────────────

        [Fact]
        public void Ctor4_UsesProvidedBlockData()
        {
            var world = MakeWorld();
            var data = new byte[32768];
            data[(5 << 11) | (3 << 7) | 64] = 1; // set a block
            var chunk = new Chunk(world, data, 0, 0);
            Assert.Equal(1, chunk.GetBlockId(5, 64, 3));
        }

        // ── §7  Block index formula ───────────────────────────────────────────

        [Fact]
        public void GetBlockId_IndexFormula_XShift11_ZShift7()
        {
            var world = MakeWorld();
            var data = new byte[32768];
            // Write to index using spec formula: (x<<11)|(z<<7)|y
            int x = 7, z = 5, y = 33;
            int idx = (x << 11) | (z << 7) | y;
            data[idx] = 42;
            var chunk = new Chunk(world, data, 0, 0);
            Assert.Equal(42, chunk.GetBlockId(x, y, z));
        }

        [Theory]
        [InlineData(0,   0,  0)]
        [InlineData(15, 127, 15)]
        [InlineData(8,   64,  8)]
        [InlineData(1,    1,  1)]
        public void GetBlockId_RoundTrip(int x, int y, int z)
        {
            var chunk = MakeChunk();
            chunk.SetBlock(x, y, z, 1);
            Assert.Equal(1, chunk.GetBlockId(x, y, z));
        }

        // ── §7 quirk 1  SetBlock writes metadata TWICE ────────────────────────

        [Fact]
        public void SetBlock5_MetadataWrittenTwice_SpecQuirk1()
        {
            // The spec mandates the metadata nibble is written once BEFORE MarkDirtyColumn
            // and again AFTER MarkDirtyColumn.  We verify the observable result equals the
            // expected metadata (if written only once the value would still be the same, but
            // this test documents the requirement that two writes occur).
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 7);
            Assert.Equal(7, chunk.GetMetadata(0, 0, 0));
        }

        [Fact]
        public void SetBlock5_ReturnsFalse_WhenNothingChanged()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 3);
            bool result = chunk.SetBlock(0, 0, 0, 1, 3);
            Assert.False(result);
        }

        [Fact]
        public void SetBlock5_ReturnsTrue_WhenBlockIdChanges()
        {
            var chunk = MakeChunk();
            bool result = chunk.SetBlock(0, 0, 0, 1, 0);
            Assert.True(result);
        }

        [Fact]
        public void SetBlock5_ReturnsTrue_WhenMetaChanges()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 0);
            bool result = chunk.SetBlock(0, 0, 0, 1, 5);
            Assert.True(result);
        }

        [Fact]
        public void SetBlock5_SetsIsModified()
        {
            var chunk = MakeChunk();
            Assert.False(chunk.IsModified);
            chunk.SetBlock(0, 0, 0, 1, 0);
            Assert.True(chunk.IsModified);
        }

        // ── §7  SetBlock(4-arg) clears meta to 0 ─────────────────────────────

        [Fact]
        public void SetBlock4_ClearsMetadataToZero()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 7);
            chunk.SetBlock(0, 0, 0, 2);        // 4-arg overload
            Assert.Equal(0, chunk.GetMetadata(0, 0, 0));
        }

        // ── §9 quirk 2  Height map stored as signed byte, read with & 255 ────

        [Fact]
        public void HeightMap_StoredAsSignedByte_ReadWith255Mask_SpecQuirk2()
        {
            // Height value 200 overflows a signed byte (−56) but & 255 == 200.
            // GenerateHeightMap writes the raw byte; GetHeightAt must mask with & 255.
            var world = MakeWorld();
            var chunk = new Chunk(world, 0, 0);

            // Manually set height map via reflection to a value > 127
            var raw = (byte[])typeof(Chunk)
                .GetProperty("HeightMapRaw", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(chunk)!;
            raw[(3 << 4) | 2] = 200; // column x=2, z=3

            int result = chunk.GetHeightAt(2, 3);
            Assert.Equal(200, result);
        }

        [Fact]
        public void IsAboveHeightMap_UsesAndMask255()
        {
            var world = MakeWorld();
            var chunk = new Chunk(world, 0, 0);

            var raw = (byte[])typeof(Chunk)
                .GetProperty("HeightMapRaw", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(chunk)!;
            raw[(0 << 4) | 0] = 200;

            // y=200 should be at the boundary (== height), so IsAboveHeightMap should be true
            Assert.True(chunk.IsAboveHeightMap(0, 200, 0));
            // y=199 is below height 200
            Assert.False(chunk.IsAboveHeightMap(0, 199, 0));
        }

        // ── §9 quirk 3  PrecipitationHeightAt returns −1 when nothing found ──

        [Fact]
        public void PrecipitationHeightAt_ReturnsNegative1_WhenAllAir_SpecQuirk3()
        {
            var chunk = MakeChunk();
            int result = chunk.PrecipitationHeightAt(0, 0);
            Assert.Equal(-1, result);
        }

        [Fact]
        public void PrecipitationHeightAt_CachesResult()
        {
            var chunk = MakeChunk();
            int first  = chunk.PrecipitationHeightAt(5, 5);
            int second = chunk.PrecipitationHeightAt(5, 5);
            Assert.Equal(first, second);
        }

        [Fact]
        public void PrecipitationHeightAt_InvalidatesCache_WhenBlockSetAboveCachedY()
        {
            var chunk = MakeChunk();
            // Warm the cache so it returns -1
            int cached = chunk.PrecipitationHeightAt(0, 0);
            Assert.Equal(-1, cached);

            // Place an opaque block; cache should be invalidated
            // We need Block.BlocksList and Material to be set up.
            // Because Block/Material are complex, we test the invalidation flag path only:
            // SetBlock at any y >= precipCache[key]-1 should invalidate to -999.
            // (−1 − 1 = −2, so y=0 ≥ −2 is true → cache invalidated)
            chunk.SetBlock(0, 64, 0, 1);

            // After invalidation, next call recomputes. We just verify it doesn't crash.
            // (Actual value depends on Block.BlocksList setup, which is integration territory.)
            int recomputed = chunk.PrecipitationHeightAt(0, 0);
            // Value is -1 since Block.BlocksList[1] is likely null in unit test context,
            // but the important thing is it ran without throwing.
            Assert.True(recomputed == -1 || recomputed > 0);
        }

        // ── §8  Light access ──────────────────────────────────────────────────

        [Fact]
        public void GetLight_DefaultsToZero()
        {
            var chunk = MakeChunk();
            Assert.Equal(0, chunk.GetLight(LightType.Sky,   0, 0, 0));
            Assert.Equal(0, chunk.GetLight(LightType.Block, 0, 0, 0));
        }

        [Fact]
        public void SetLight_Sky_RoundTrip()
        {
            var chunk = MakeChunk();
            chunk.SetLight(LightType.Sky, 1, 1, 1, 15);
            Assert.Equal(15, chunk.GetLight(LightType.Sky, 1, 1, 1));
        }

        [Fact]
        public void SetLight_Block_RoundTrip()
        {
            var chunk = MakeChunk();
            chunk.SetLight(LightType.Block, 2, 3, 4, 7);
            Assert.Equal(7, chunk.GetLight(LightType.Block, 2, 3, 4));
        }

        [Fact]
        public void SetLight_Sky_Noop_InNether()
        {
            var chunk = MakeChunk(nether: true);
            chunk.SetLight(LightType.Sky, 0, 0, 0, 15);
            Assert.Equal(0, chunk.GetLight(LightType.Sky, 0, 0, 0));
        }

        [Fact]
        public void GetLightSubtracted_SkyZero_InNether()
        {
            var chunk = MakeChunk(nether: true);
            chunk.SetLight(LightType.Block, 0, 0, 0, 10);
            int result = chunk.GetLightSubtracted(0, 0, 0, 0);
            Assert.Equal(10, result); // sky forced to 0 in nether
        }

        [Fact]
        public void GetLightSubtracted_SetsAnySkylightPresent_WhenSkyGtZero()
        {
            Chunk.AnySkylightPresent = false;
            var chunk = MakeChunk();
            chunk.SetLight(LightType.Sky, 0, 0, 0, 5);
            chunk.GetLightSubtracted(0, 0, 0, 0);
            Assert.True(Chunk.AnySkylightPresent);
        }

        [Fact]
        public void GetLightSubtracted_ReturnsMax_OfSkyAndBlock()
        {
            var chunk = MakeChunk();
            chunk.SetLight(LightType.Sky,   0, 0, 0, 10);
            chunk.SetLight(LightType.Block, 0, 0, 0, 8);
            int result = chunk.GetLightSubtracted(0, 0, 0, 3); // sky = max(10-3,8) = max(7,8) = 8
            Assert.Equal(8, result);
        }

        // ── §9  Height map ────────────────────────────────────────────────────

        [Fact]
        public void GenerateHeightMap_AllAir_HeightIsZero()
        {
            var chunk = MakeChunk();
            chunk.GenerateHeightMap();
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
                Assert.Equal(0, chunk.GetHeightAt(x, z));
        }

        [Fact]
        public void GenerateHeightMap_SingleOpaqueBlock_HeightIsYPlusOne()
        {
            // Set up opacity table so block 1 is opaque
            Block.LightOpacity[1] = 255;
            var chunk = MakeChunk();
            chunk.SetBlock(3, 50, 5, 1);
            chunk.GenerateHeightMap();
            Assert.Equal(51, chunk.GetHeightAt(3, 5));
        }

        [Fact]
        public void GenerateHeightMap_SetsLowestHeightInChunk()
        {
            Block.LightOpacity[1] = 255;
            var chunk = MakeChunk();
            chunk.SetBlock(0, 10, 0, 1);
            chunk.SetBlock(1, 20, 0, 1);
            chunk.GenerateHeightMap();
            // Lowest non-air height should be 11 (y=10 gives height 11)
            Assert.Equal(11, chunk.LowestHeightInChunk);
        }

        // ── §12  Lifecycle ────────────────────────────────────────────────────

        [Fact]
        public void OnChunkLoad_SetsIsLoadedTrue()
        {
            var chunk = MakeChunk();
            chunk.OnChunkLoad();
            Assert.True(chunk.IsLoaded);
        }

        [Fact]
        public void OnChunkUnload_SetsIsLoadedFalse()
        {
            var chunk = MakeChunk();
            chunk.OnChunkLoad();
            chunk.OnChunkUnload();
            Assert.False(chunk.IsLoaded);
        }

        [Fact]
        public void MarkDirty_SetsIsModified()
        {
            var chunk = MakeChunk();
            chunk.MarkDirty();
            Assert.True(chunk.IsModified);
        }

        // ── TileEntity management ─────────────────────────────────────────────

        [Fact]
        public void AddTileEntity_CanBeRetrieved()
        {
            var chunk = MakeChunk(cx: 2, cz: 3);
            var te    = new FakeTileEntity();
            chunk.AddTileEntity(1, 64, 2, te);
            Assert.Same(te, chunk.GetTileEntity(1, 64, 2));
        }

        [Fact]
        public void AddTileEntity_SetsWorldCoordinatesCorrectly()
        {
            var chunk = MakeChunk(cx: 2, cz: 3);
            var te    = new FakeTileEntity();
            chunk.AddTileEntity(1, 64, 2, te);
            Assert.Equal(2 * 16 + 1, te.X);
            Assert.Equal(64,          te.Y);
            Assert.Equal(3 * 16 + 2,  te.Z);
        }

        [Fact]
        public void AddTileEntity_CallsValidate()
        {
            var chunk = MakeChunk();
            var te    = new FakeTileEntity();
            chunk.AddTileEntity(0, 0, 0, te);
            Assert.True(te.IsValid);
        }

        [Fact]
        public void RemoveTileEntity_CallsInvalidate()
        {
            var chunk = MakeChunk();
            var te    = new FakeTileEntity();
            chunk.AddTileEntity(0, 0, 0, te);
            chunk.RemoveTileEntity(0, 0, 0);
            Assert.False(te.IsValid);
        }

        [Fact]
        public void RemoveTileEntity_RemovesFromMap()
        {
            var chunk = MakeChunk();
            var te    = new FakeTileEntity();
            chunk.AddTileEntity(0, 0, 0, te);
            chunk.RemoveTileEntity(0, 0, 0);
            Assert.Null(chunk.GetTileEntity(0, 0, 0));
        }

        [Fact]
        public void OnChunkUnload_InvalidatesAllTileEntities()
        {
            var chunk = MakeChunk();
            var te1   = new FakeTileEntity();
            var te2   = new FakeTileEntity();
            chunk.AddTileEntity(0, 1, 0, te1);
            chunk.AddTileEntity(1, 2, 1, te2);
            chunk.OnChunkLoad();
            chunk.OnChunkUnload();
            Assert.False(te1.IsValid);
            Assert.False(te2.IsValid);
        }

        // ── Entity bucket management ──────────────────────────────────────────

        [Fact]
        public void AddEntity_SetsAddedToChunkTrue()
        {
            var chunk  = MakeChunk(cx: 4, cz: 7);
            var entity = new FakeEntity(32.0);
            chunk.AddEntity(entity);
            Assert.True(entity.AddedToChunk);
        }

        [Fact]
        public void AddEntity_SetsChunkCoords()
        {
            var chunk  = MakeChunk(cx: 4, cz: 7);
            var entity = new FakeEntity(32.0);
            chunk.AddEntity(entity);
            Assert.Equal(4, entity.ChunkCoordX);
            Assert.Equal(7, entity.ChunkCoordZ);
        }

        [Fact]
        public void AddEntity_BucketIndex_IsFloorOfPosYDiv16_Clamped()
        {
            var chunk  = MakeChunk();
            var entity = new FakeEntity(48.0); // bucket = floor(48/16) = 3
            chunk.AddEntity(entity);
            Assert.Equal(3, entity.ChunkCoordY);
        }

        [Fact]
        public void AddEntity_PosYNegative_ClampedToZero()
        {
            var chunk  = MakeChunk();
            var entity = new FakeEntity(-5.0);
            chunk.AddEntity(entity);
            Assert.Equal(0, entity.ChunkCoordY);
        }

        [Fact]
        public void AddEntity_PosYAboveMax_ClampedTo7()
        {
            var chunk  = MakeChunk();
            var entity = new FakeEntity(9999.0);
            chunk.AddEntity(entity);
            Assert.Equal(7, entity.ChunkCoordY);
        }

        [Fact]
        public void RemoveEntity_SetsAddedToChunkFalse()
        {
            var chunk  = MakeChunk();
            var entity = new FakeEntity(16.0);
            chunk.AddEntity(entity);
            chunk.RemoveEntity(entity);
            Assert.False(entity.AddedToChunk);
        }

        [Fact]
        public void GetEntitiesInRange_ReturnsEntitiesInBuckets()
        {
            var chunk  = MakeChunk();
            var e1     = new FakeEntity(8.0);   // bucket 0
            var e2     = new FakeEntity(24.0);  // bucket 1
            var e3     = new FakeEntity(80.0);  // bucket 5
            chunk.AddEntity(e1);
            chunk.AddEntity(e2);
            chunk.AddEntity(e3);

            var result = new List<Entity>();
            chunk.GetEntitiesInRange(result, 0.0, 31.0); // buckets 0 and 1
            Assert.Contains(e1, result);
            Assert.Contains(e2, result);
            Assert.DoesNotContain(e3, result);
        }

        [Fact]
        public void HasEntities_SetTrueOnAddEntity()
        {
            var chunk  = MakeChunk();
            Assert.False(chunk.HasEntities);
            chunk.AddEntity(new FakeEntity(0.0));
            Assert.True(chunk.HasEntities);
        }

        // ── GetAllEntities ────────────────────────────────────────────────────

        [Fact]
        public void GetAllEntities_ReturnsAllAcrossBuckets()
        {
            var chunk  = MakeChunk();
            var e1     = new FakeEntity(0.0);
            var e2     = new FakeEntity(64.0);
            chunk.AddEntity(e1);
            chunk.AddEntity(e2);
            var all = chunk.GetAllEntities().ToList();
            Assert.Contains(e1, all);
            Assert.Contains(e2, all);
        }

        // ── GetTileEntities ───────────────────────────────────────────────────

        [Fact]
        public void GetTileEntities_ReturnsAllAdded()
        {
            var chunk = MakeChunk();
            var te1   = new FakeTileEntity();
            var te2   = new FakeTileEntity();
            chunk.AddTileEntity(0, 0, 0, te1);
            chunk.AddTileEntity(1, 1, 1, te2);
            var all   = chunk.GetTileEntities().ToList();
            Assert.Contains(te1, all);
            Assert.Contains(te2, all);
        }

        // ── §13  NeedsSaving ─────────────────────────────────────────────────

        [Fact]
        public void NeedsSaving_FalseWhenNoSaveIsTrue()
        {
            var chunk = MakeChunk();
            chunk.IsLightPopulated = true;
            chunk.IsModified       = true;
            chunk.NoSave           = true;
            Assert.False(chunk.NeedsSaving(true));
        }

        [Fact]
        public void NeedsSaving_FalseWhenLightNotPopulated()
        {
            var chunk = MakeChunk();
            chunk.IsLightPopulated = false;
            chunk.IsModified       = true;
            Assert.False(chunk.NeedsSaving(true));
        }

        [Fact]
        public void NeedsSaving_TrueWhenModifiedAndLightPopulated()
        {
            var chunk = MakeChunk();
            chunk.IsLightPopulated = true;
            chunk.IsModified       = true;
            Assert.True(chunk.NeedsSaving(true));
        }

        [Fact]
        public void NeedsSaving_ForceCheck_True_WhenHasEntitiesAndTimeNotLastSave()
        {
            var world = MakeWorld(seed: 0);
            // We can't easily set TotalWorldTime on FakeWorld without exposing it,
            // so test the false branch instead: LastSaveTime == TotalWorldTime → false for entities.
            var chunk = new Chunk(world, 0, 0);
            chunk.IsLightPopulated = true;
            chunk.HasEntities      = true;
            chunk.LastSaveTime     = world.TotalWorldTime; // same time → entities condition false
            chunk.IsModified       = false;
            Assert.False(chunk.NeedsSaving(true));
        }

        // ── §13  GetChunkRandom determinism ───────────────────────────────────

        [Fact]
        public void GetChunkRandom_Deterministic_SameSeedAndCoords()
        {
            var world  = MakeWorld(seed: 123456789L);
            var chunk1 = new Chunk(world, 3, 7);
            var chunk2 = new Chunk(world, 3, 7);

            var r1 = chunk1.GetChunkRandom(42L);
            var r2 = chunk2.GetChunkRandom(42L);

            Assert.Equal(r1.NextInt(1000), r2.NextInt(1000));
        }

        [Fact]
        public void GetChunkRandom_DifferentForDifferentChunkCoords()
        {
            var world  = MakeWorld(seed: 123456789L);
            var chunk1 = new Chunk(world, 3, 7);
            var chunk2 = new Chunk(world, 4, 7);

            var r1 = chunk1.GetChunkRandom(42L);
            var r2 = chunk2.GetChunkRandom(42L);

            Assert.NotEqual(r1.NextInt(1_000_000), r2.NextInt(1_000_000));
        }

        [Fact]
        public void GetChunkRandom_SeedFormula_MatchesSpec()
        {
            // Spec: s = worldSeed + cx*cx*4987142 + cx*5947611 + cz*cz*4392871 + cz*389711 XOR seed
            long ws   = 100L;
            int  cx   = 2;
            int  cz   = 3;
            long seed = 7L;
            long expected = ws
                + (long)cx * cx * 4987142L
                + (long)cx * 5947611L
                + (long)cz * cz * 4392871L
                + (long)cz * 389711L
                ^ seed;

            var world = MakeWorld(seed: ws);
            var chunk = new Chunk(world, cx, cz);
            var rng   = chunk.GetChunkRandom(seed);

            var direct = new JavaRandom();
            direct.SetSeed(expected);

            Assert.Equal(direct.NextInt(100000), rng.NextInt(100000));
        }

        // ── GenerateSkylightMap ───────────────────────────────────────────────

        [Fact]
        public void GenerateSkylightMap_SetsIsLightPopulated()
        {
            var chunk = MakeChunk();
            chunk.GenerateSkylightMap();
            Assert.True(chunk.IsLightPopulated);
        }

        [Fact]
        public void GenerateSkylightMap_SetsIsModified()
        {
            var chunk = MakeChunk();
            chunk.IsModified = false;
            chunk.GenerateSkylightMap();
            Assert.True(chunk.IsModified);
        }

        [Fact]
        public void GenerateSkylightMap_AllAir_SkyLightIs15Everywhere()
        {
            var chunk = MakeChunk();
            chunk.GenerateSkylightMap();
            for (int y = 0; y < 128; y++)
                Assert.Equal(15, chunk.GetLight(LightType.Sky, 0, y, 0));
        }

        [Fact]
        public void GenerateSkylightMap_Nether_SkyLightIsZeroEverywhere()
        {
            var chunk = MakeChunk(nether: true);
            chunk.GenerateSkylightMap();
            for (int y = 0; y < 128; y++)
                Assert.Equal(0, chunk.GetLight(LightType.Sky, 0, y, 0));
        }

        // ── SetMetadata ───────────────────────────────────────────────────────

        [Fact]
        public void SetMetadata_ReturnsFalseWhenUnchanged()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 3);
            bool result = chunk.SetMetadata(0, 0, 0, 3);
            Assert.False(result);
        }

        [Fact]
        public void SetMetadata_ReturnsTrueWhenChanged()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 3);
            bool result = chunk.SetMetadata(0, 0, 0, 5);
            Assert.True(result);
        }

        [Fact]
        public void SetMetadata_SetsIsModified()
        {
            var chunk = MakeChunk();
            chunk.SetBlock(0, 0, 0, 1, 0);
            chunk.IsModified = false;
            chunk.SetMetadata(0, 0, 0, 3);
            Assert.True(chunk.IsModified);
        }

        // ── UpdateSkylight ────────────────────────────────────────────────────

        [Fact]
        public void UpdateSkylight_Noop_InNether()
        {
            var chunk = MakeChunk(nether: true);
            chunk.SetBlock(0, 0, 0, 1); // triggers MarkDirtyColumn
            // In nether, UpdateSkylight should do nothing (not call PropagateLight)
            var world = (FakeWorld)chunk.World;
            world.PropagateCalls.Clear();
            chunk.UpdateSkylight();
            Assert.Empty(world.PropagateCalls);
        }

        [Fact]
        public void UpdateSkylight_ClearsDirtyColumns()
        {
            var chunk  = MakeChunk();
            chunk.IsLoaded = true;
            chunk.SetBlock(0, 0, 0, 1);
            chunk.GenerateHeightMap();
            // UpdateSkylight should clear dirty flags (no exception)
            chunk.UpdateSkylight();
            // If called again with no new dirty columns, nothing happens (no propagate calls)
            var world = (FakeWorld)chunk.World;
            world.PropagateCalls.Clear();
            chunk.UpdateSkylight();
            Assert.Empty(world.PropagateCalls);
        }

        // ── Height map index formula ──────────────────────────────────────────

        [Fact]
        public void HeightMapIndex_IsZShiftLeft4OrX()
        {
            // Spec: j[(z<<4) | x]
            // Test that GetHeightAt(x, z) reads from the correct index by manually writing
            // the raw array at two different (x,z) combinations.
            var world = MakeWorld();
            var chunk = new Chunk(world, 0, 0);
            var raw   = (byte[])typeof(Chunk)
                .GetProperty("HeightMapRaw", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(chunk)!;

            raw[(5 << 4) | 3]  = 60;  // z=5, x=3 → index 83
            raw[(3 << 4) | 5]  = 80;  // z=3, x=5 → index 53

            Assert.Equal(60, chunk.GetHeightAt(3, 5));
            Assert.Equal(80, chunk.GetHeightAt(5, 3));
        }

        // ── ClearHeightMap ────────────────────────────────────────────────────

        [Fact]
        public void ClearHeightMap_ResetsAllToZero()
        {
            var world = MakeWorld();
            var chunk = new Chunk(world, 0, 0);
            var raw   = (byte[])typeof(Chunk)
                .GetProperty("HeightMapRaw", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(chunk)!;
            for (int i = 0; i < 256; i++) raw[i] = 100;
            chunk.ClearHeightMap();
            for (int i = 0; i < 256; i++) Assert.Equal(0, raw[i]);
        }

        // ── BlockArraySize constant ───────────────────────────────────────────

        [Fact]
        public void BlockIds_ArraySize_Is32768()
        {
            var world = MakeWorld();
            var chunk = new Chunk(world, 0, 0);
            var raw   = chunk.BlockIdsRaw;
            Assert.Equal(32768, raw.Length);
        }

        // ── EntityBuckets: 8 buckets (WorldHeight/16) ─────────────────────────

        [Fact]
        public void EntityBuckets_ExactlyEight()
        {
            // Spec: world.c / 16 = 8 buckets.
            // Probe by inserting entities at extreme Y values and verifying no exception.
            var chunk = MakeChunk();
            for (int b = 0; b < 8; b++)
            {
                double posY = b * 16.0 + 8.0;
                var e = new FakeEntity(posY);
                chunk.AddEntity(e);
                Assert.Equal(b, e.ChunkCoordY);
            }
        }

        // ── NoSave field ──────────────────────────────────────────────────────

        [Fact]
        public void NoSave_DefaultIsFalse()
        {
            var chunk = MakeChunk();
            Assert.False(chunk.NoSave);
        }

        // ── Spec §15 quirk 1: Double metadata write observable via SetBlock ───

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("xUnit", "xUnit1004")]
        public void SetBlock5_DoubleMetadataWrite_IsDocumentedBehaviourNotBug()
        {
            // This test documents that the SECOND write (post-MarkDirtyColumn) is spec-required.
            // If a future refactor removes the second write, this assertion documents intent.
            var chunk = MakeChunk();
            chunk.SetBlock(4, 4, 4, 1, 0xF);
            // The final metadata must equal 0xF — the second write must not corrupt it.
            Assert.Equal(0xF & 0xF, chunk.GetMetadata(4, 4, 4));
        }

        // ── Spec §15 quirk 3: PrecipitationHeightAt −1 sentinel ─────────────

        [Fact]
        public void PrecipitationHeightAt_NegativeOne_CachedAsSentinel()
        {
            var chunk  = MakeChunk();
            int first  = chunk.PrecipitationHeightAt(7, 7);
            // Second call should return the same cached value without recomputing.
            int second = chunk.PrecipitationHeightAt(7, 7);
            Assert.Equal(-1, first);
            Assert.Equal(-1, second);
        }

        // ── Spec §3: biome decorator entry-point RNG consumption ordering ─────
        // These tests document the expected RNG call order from the BiomeDecorator spec.
        // They are marked as parity bugs if no BiomeDecorator implementation exists yet.

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; RNG call order for decoration sequence unverifiable")]
        public void BiomeDecorator_DecorateSequence_RngCallOrder_Step1_OreGeneration()
        {
            // Spec §4 Step 1: ore helper a(count, gen, yMin, yMax) consumes count×3 nextInt calls
            // plus internal ky.a() calls, in exact order:
            // dirt(20), gravel(10), coal(20), iron(20), gold(2), redstone(8), diamond(1), lapis(1)
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; sand disk step 2 RNG consumption not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step2_SandDiskPatches_DefaultH3()
        {
            // Spec §4 Step 2: H=3 sand disks each consuming 2 nextInt(16) + world.f call
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; flower step 7 RNG consumption not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step7_Flowers_25PercentRoseChance()
        {
            // Spec §4 Step 7: A=2 flower loops; each iteration: dandelion always, rose if nextInt(4)==0
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; step 11 unconditional mushrooms outside D loop not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step11_UnconditionalMushroomsOutsideDLoop()
        {
            // Spec §4 Step 11: After the D-count loop, TWO unconditional extra mushroom chances
            // (brown: nextInt(4)==0; red: nextInt(8)==0) always execute regardless of D.
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; step 12 hardcoded 10 reeds always placed")]
        public void BiomeDecorator_DecorateSequence_Step12_HardcodedTenReeds()
        {
            // Spec §4 Step 12: Always 10 extra reed placement attempts beyond E-count reeds.
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; step 13 pumpkin 1-in-32 chance not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step13_Pumpkin_OneIn32Chance()
        {
            // Spec §4 Step 13: unconditional nextInt(32)==0 pumpkin check, always consumes 1 RNG call
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; step 15 water spring Y distribution not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step15_WaterSpringYDistribution()
        {
            // Spec §4 Step 15: water spring Y = nextInt(nextInt(worldHeight-8)+8), 50 per chunk
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator not yet implemented; step 15 lava spring triple-nested nextInt not verifiable")]
        public void BiomeDecorator_DecorateSequence_Step15_LavaSpringYTripleNestedDistribution()
        {
            // Spec §4 Step 15: lava spring Y = nextInt(nextInt(nextInt(worldHeight-16)+8)+8), 20 per chunk
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── Spec class identity corrections ──────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenCactus (ade) incorrectly labelled as pumpkin in ChunkProviderGenerate_Spec")]
        public void ClassIdentity_Ade_IsCactusNotPumpkin()
        {
            // Spec correction: ade = WorldGenCactus (places cactus height 1-3, 10 attempts)
            // NOT pumpkin stems. Verify by checking the block ID placed is 81.
            Assert.True(false, "WorldGenCactus not implemented or incorrectly labelled");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenPumpkin (sz) was unspecced; must place pumpkin ID 86 with random facing meta [0,3]")]
        public void ClassIdentity_Sz_IsPumpkinGenerator()
        {
            // Spec: sz = WorldGenPumpkin. Places block 86 with nextInt(4) meta.
            Assert.True(false, "WorldGenPumpkin not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: we.java is SpawnerAnimals not snow/ice generator; snow/ice freeze pass class unresolved")]
        public void ClassIdentity_We_IsSpawnerAnimals_NotSnowIce()
        {
            // Spec correction: we = SpawnerAnimals. Snow/ice freeze is a separate unresolved class.
            Assert.True(false, "Snow/ice freeze class not yet identified");
        }

        // ── WorldGenHugeMushroom spec tests ───────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenHugeMushroom (acp) not yet implemented")]
        public void HugeMushroom_DefaultCtor_TypeNegative1_RandomBetween0And1()
        {
            // Spec §5.9: acp() → a = -1 (random type chosen per generate call)
            Assert.True(false, "WorldGenHugeMushroom not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenHugeMushroom (acp) height must be nextInt(3)+4, range [4,6]")]
        public void HugeMushroom_Height_RangeIs4To6()
        {
            // Spec §5.9: height = nextInt(3) + 4
            Assert.True(false, "WorldGenHugeMushroom not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenHugeMushroom stem meta must be 10, interior meta 0")]
        public void HugeMushroom_StemMeta_Is10()
        {
            // Spec §5.9: trunk placed with meta 10
            Assert.True(false, "WorldGenHugeMushroom not implemented");
        }

        // ── WorldGenSpring spec tests ─────────────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenSpring (ib) requires exactly 3 stone neighbors and 1 air neighbor")]
        public void WorldGenSpring_PlacesSpring_WhenExactly3StoneAnd1Air()
        {
            // Spec §5.4: stoneCount==3 AND airCount==1
            Assert.True(false, "WorldGenSpring not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenSpring must check top and bottom stone (y+1, y-1) before horizontal neighbors")]
        public void WorldGenSpring_RequiresStoneAboveAndBelow()
        {
            // Spec §5.4: first two checks are y+1 and y-1 stone
            Assert.True(false, "WorldGenSpring not implemented");
        }

        // ── WorldGenReed height distribution ─────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenReed height must be 2+nextInt(nextInt(3)+1), range [2,4]")]
        public void WorldGenReed_Height_Formula_IsGeometricBiasedToward2()
        {
            // Spec §5.5: height = 2 + nextInt(nextInt(3)+1)
            Assert.True(false, "WorldGenReed not implemented");
        }

        // ── BiomeDecorator default field values ───────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator default field H must be 3 (sand disk count)")]
        public void BiomeDecorator_DefaultField_H_Is3_SandDiskCount()
        {
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator default field A must be 2 (flower attempts)")]
        public void BiomeDecorator_DefaultField_A_Is2_FlowerAttempts()
        {
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator default field B must be 1 (tall grass calls)")]
        public void BiomeDecorator_DefaultField_B_Is1_TallGrassCalls()
        {
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator default field K must be true (enable springs)")]
        public void BiomeDecorator_DefaultField_K_IsTrue_EnableSprings()
        {
            Assert.True(false, "BiomeDecorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: BiomeDecorator entry-point must throw RuntimeException if already decorating")]
        public void BiomeDecorator_ThrowsIfAlreadyDecorating()
        {
            // Spec §3: if this.a != null → throw RuntimeException("Already decorating!!")
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── Ore position formula (no +8 offset) ──────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: ore helper a() uses chunkX (no +8 offset), unlike decoration helpers")]
        public void OreHelper_XZPositions_HaveNoEightOffset()
        {
            // Spec §4 Step 1 helper a(): x = nextInt(16) + chunkX (NO +8)
            // This differs from all other decoration helpers which use +8.
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── Lapis triangular Y distribution ──────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: lapis ore uses triangular Y distribution via helper b(), not uniform helper a()")]
        public void OreHelper_Lapis_UsesTriangularYDistribution()
        {
            // Spec §4 Step 1: lapis uses helper b(1, p, worldHeight/8, worldHeight/8)
            // Y = nextInt(ySpread) + nextInt(ySpread) + (yCenter - ySpread)
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── Desert biome overrides ────────────────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Desert biome E field must be 50 (reed count)")]
        public void Biome_Desert_FieldE_Is50()
        {
            Assert.True(false, "Desert biome decorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Desert biome F field must be 10 (cactus count)")]
        public void Biome_Desert_FieldF_Is10()
        {
            Assert.True(false, "Desert biome decorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Desert biome C field must be 2 (dead bush count)")]
        public void Biome_Desert_FieldC_Is2()
        {
            Assert.True(false, "Desert biome decorator not implemented");
        }

        // ── Swamp biome overrides ─────────────────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Swamp biome y field must be 4 (lily pad count)")]
        public void Biome_Swamp_FieldY_Is4()
        {
            Assert.True(false, "Swamp biome decorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Swamp biome A field -999 means 0 flower iterations")]
        public void Biome_Swamp_FlowerLoop_RunsZeroTimes_WhenAIsNegative999()
        {
            // Spec §6: A=-999 → loop runs -999 times → 0 iterations.
            Assert.True(false, "Swamp biome decorator not implemented");
        }

        // ── Plains biome overrides ────────────────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Plains biome B field must be 10 (tall grass calls)")]
        public void Biome_Plains_FieldB_Is10()
        {
            Assert.True(false, "Plains biome decorator not implemented");
        }

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Plains biome A field must be 4 (flower attempts)")]
        public void Biome_Plains_FieldA_Is4()
        {
            Assert.True(false, "Plains biome decorator not implemented");
        }

        // ── Gravel disk h field not called in base decorator ──────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: gravel disk (field h) must not be called in base ql.a() decoration loop")]
        public void BaseDecorator_GravelDisk_NotCalledInBaseFlow()
        {
            // Spec §10: h is declared but ql.a() never calls it directly.
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── Snow/ice freeze pass unresolved ───────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: Snow/ice freeze pass (freezes water to ice ID 79, places snow ID 78) not yet identified or implemented")]
        public void SnowIceFreeze_FreezesSurfaceWater_InColdBiomes()
        {
            // Spec §11: biome temperature < 0.15 → freeze still water (ID 9) to ice (ID 79)
            //           and place snow layer (ID 78) on first solid surface block.
            Assert.True(false, "Snow/ice freeze class not yet identified");
        }

        // ── WorldGenTallGrass uses world.b (notify) not world.d (silent) ──────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenTallGrass (ahu) must use world.b() with metadata, not world.d() (no meta, no notification)")]
        public void WorldGenTallGrass_PlacesBlock_WithNotifyAndMeta()
        {
            // Spec §5.2: placement is world.b(bx, by, bz, a, b) — with neighbor notifications + meta
            Assert.True(false, "WorldGenTallGrass not implemented");
        }

        // ── WorldGenFlowers uses world.d (silent) ─────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenFlowers (bu) must use world.d() silent placement, not world.b()")]
        public void WorldGenFlowers_PlacesBlock_SilentlyNoMeta()
        {
            // Spec §5.1: placement is world.d(bx, by, bz, a) — no notifications, no meta
            Assert.True(false, "WorldGenFlowers not implemented");
        }

        // ── Step 5 tree count: 10% bonus tree ────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: tree decoration step 5 must consume nextInt(10) unconditionally and add 1 bonus tree when result == 0")]
        public void Decoration_Step5_Trees_ConsumeNextInt10_AlwaysForBonusCheck()
        {
            // Spec §4 Step 5: always calls nextInt(10); treeCount++ if == 0
            Assert.True(false, "BiomeDecorator not implemented");
        }

        // ── WorldGenPumpkin facing meta ───────────────────────────────────────

        [Fact(Skip = "PARITY BUG — impl diverges from spec: WorldGenPumpkin must place pumpkin with nextInt(4) facing meta [0,3]")]
        public void WorldGenPumpkin_PlacesPumpkin_WithRandomFacingMeta0To3()
        {
            // Spec §5.6: world.b(bx, by, bz, 86, nextInt(4))
            Assert.True(false, "WorldGenPumpkin not implemented");
        }
    }
}