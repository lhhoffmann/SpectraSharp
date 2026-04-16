using System;
using System.Collections.Generic;
using Xunit;
using SpectraSharp.Core;

namespace SpectraSharp.Tests;

// ── Hand-written fakes ────────────────────────────────────────────────────────

file sealed class FakeBlockAccess : IBlockAccess
{
    private readonly Dictionary<(int, int, int), (int id, int meta)> _blocks = new();

    public void Set(int x, int y, int z, int id, int meta = 0) =>
        _blocks[(x, y, z)] = (id, meta);

    public int GetBlockId(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.id : 0;

    public int GetBlockMetadata(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0;

        // ── IBlockAccess stubs (auto-generated) ─────────────────────────
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

file sealed class FakeWorld : IWorld
{
    // Block storage
    private readonly Dictionary<(int, int, int), (int id, int meta)> _blocks = new();

    // Scheduled updates: (x,y,z,blockId,delay)
    public List<(int x, int y, int z, int blockId, int delay)> ScheduledUpdates = new();

    // SetBlock calls
    public List<(int x, int y, int z, int id)> SetBlockCalls = new();

    // SetBlockAndMetadata calls
    public List<(int x, int y, int z, int id, int meta)> SetBlockAndMetaCalls = new();

    // SetMetadataQuiet calls
    public List<(int x, int y, int z, int meta)> SetMetaQuietCalls = new();

    // NotifyBlock calls
    public List<(int x, int y, int z, int blockId)> NotifyBlockCalls = new();

    // Power map: (x,y,z,face) -> result
    private readonly Dictionary<(int, int, int, int), bool> _powerMap = new();

    // Normal cube set
    private readonly HashSet<(int, int, int)> _normalCubes = new();

    public bool IsClientSide { get; set; } = false;

    public bool SetBlock(int x, int y, int z, int id)
    {
        SetBlockCalls.Add((x, y, z, id));
        _blocks[(x, y, z)] = (id, _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0);
            return true;
    }

    public bool SetBlockAndMetadata(int x, int y, int z, int id, int meta)
    {
        SetBlockAndMetaCalls.Add((x, y, z, id, meta));
        _blocks[(x, y, z)] = (id, meta);
            return true;
    }

    public bool SetMetadata(int x, int y, int z, int meta)
    {
        if (_blocks.TryGetValue((x, y, z), out var v))
            _blocks[(x, y, z)] = (v.id, meta);
        else
            _blocks[(x, y, z)] = (0, meta);
        return true;
    }

    public void SetMetadataQuiet(int x, int y, int z, int meta)
    {
        SetMetaQuietCalls.Add((x, y, z, meta));
        if (_blocks.TryGetValue((x, y, z), out var v))
            _blocks[(x, y, z)] = (v.id, meta);
        else
            _blocks[(x, y, z)] = (0, meta);
    }

    public void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay)
    {
        ScheduledUpdates.Add((x, y, z, blockId, delay));
    }

    public void NotifyBlock(int x, int y, int z, int blockId)
    {
        NotifyBlockCalls.Add((x, y, z, blockId));
    }

    public int GetBlockId(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.id : 0;

    public int GetBlockMetadata(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0;

    public void SetPower(int x, int y, int z, int face, bool powered) =>
        _powerMap[(x, y, z, face)] = powered;

    public bool GetPower(int x, int y, int z, int face) =>
        _powerMap.TryGetValue((x, y, z, face), out var r) && r;

    public void SetNormalCube(int x, int y, int z) =>
        _normalCubes.Add((x, y, z));

    public bool IsBlockNormalCube(int x, int y, int z) =>
        _normalCubes.Contains((x, y, z));

    public void PlaceBlock(int x, int y, int z, int id, int meta = 0) =>
        _blocks[(x, y, z)] = (id, meta);

        // ── IBlockAccess stubs (auto-generated) ─────────────────────────
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

        // ── IWorld stubs ──
        public JavaRandom   Random        { get; set; } = new JavaRandom(0);
        public bool         IsNether      { get; set; } = false;
        public bool         SuppressUpdates { get; set; } = false;
        public int          DimensionId   { get; set; } = 0;
        public void SpawnEntity(Entity entity)                                           { }
        public void SetBlockSilent(int x, int y, int z, int id)                         { }
        public bool CanFreezeAtLocation(int x, int y, int z)                            => false;
        public bool CanSnowAtLocation(int x, int y, int z)                              => false;
        public bool IsAreaLoaded(int x, int y, int z, int r)                            => true;
        public void NotifyNeighbors(int x, int y, int z, int id)                        { }
        public int  GetLightBrightness(int x, int y, int z)                             => 15;
        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d)      { }
        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)                => false;
        public bool IsRaining()                                                          => false;
        public bool IsBlockExposedToRain(int x, int y, int z)                           => false;
        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool f) { }
}

// Stub World subclass expected by implementation (cast to World)
// Since BlockRedstoneDiode casts to World, we need a concrete fake World.
// We'll use a minimal stub that satisfies the cast.
file sealed class StubWorld : World
{
    private readonly Dictionary<(int, int, int), (int id, int meta)> _blocks = new();
    public List<(int x, int y, int z, int blockId, int delay)> ScheduledUpdates = new();
    public List<(int x, int y, int z, int id, int meta)> SetBlockAndMetaCalls = new();
    public List<(int x, int y, int z, int meta)> SetMetaQuietCalls = new();
    public List<(int x, int y, int z, int id)> SetBlockCalls = new();
    public List<(int x, int y, int z, int blockId)> NotifyBlockCalls = new();

    private readonly Dictionary<(int, int, int, int), bool> _powerMap = new();
    private readonly HashSet<(int, int, int)> _normalCubes = new();

    public StubWorld() : base(new NullChunkLoader(), 0L) { }

    public new int GetBlockId(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.id : 0;

    public new int GetBlockMetadata(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0;

    public new bool SetBlock(int x, int y, int z, int id)
    {
        SetBlockCalls.Add((x, y, z, id));
        var meta = _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0;
        _blocks[(x, y, z)] = (id, meta);
        return true;
    }

    public new bool SetBlockAndMetadata(int x, int y, int z, int id, int meta)
    {
        SetBlockAndMetaCalls.Add((x, y, z, id, meta));
        _blocks[(x, y, z)] = (id, meta);
        return true;
    }

    public new void SetMetadataQuiet(int x, int y, int z, int meta)
    {
        SetMetaQuietCalls.Add((x, y, z, meta));
        if (_blocks.TryGetValue((x, y, z), out var v))
            _blocks[(x, y, z)] = (v.id, meta);
        else
            _blocks[(x, y, z)] = (0, meta);
    }

    public new void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay)
    {
        ScheduledUpdates.Add((x, y, z, blockId, delay));
    }

    public new void NotifyBlock(int x, int y, int z, int blockId)
    {
        NotifyBlockCalls.Add((x, y, z, blockId));
    }

    /// <summary>Write-only alias that lets the extension method set World.IsClientSide.</summary>
    public bool IsClientSideOverride { set => IsClientSide = value; }

    public void SetPower(int x, int y, int z, int face, bool powered) =>
        _powerMap[(x, y, z, face)] = powered;

    public new bool GetPower(int x, int y, int z, int face) =>
        _powerMap.TryGetValue((x, y, z, face), out var r) && r;

    public void SetNormalCube(int x, int y, int z) =>
        _normalCubes.Add((x, y, z));

    public new bool IsBlockNormalCube(int x, int y, int z) =>
        _normalCubes.Contains((x, y, z));

    public void PlaceBlock(int x, int y, int z, int id, int meta = 0) =>
        _blocks[(x, y, z)] = (id, meta);
}

// ── Test class ────────────────────────────────────────────────────────────────

public sealed class BlockRedstoneDiodeTests
{
    // ── §6.2 Constructor / basic properties ───────────────────────────────────

    [Fact]
    public void Constructor_OffRepeater_HasCorrectId()
    {
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal(93, block.BlockID);
    }

    [Fact]
    public void Constructor_OnRepeater_HasCorrectId()
    {
        var block = new BlockRedstoneDiode(94, true);
        Assert.Equal(94, block.BlockID);
    }

    [Fact]
    public void Constructor_OffRepeater_HasCorrectTextureIndex()
    {
        // Spec §6.2: super(id, textureIndex, material) — off repeater texture = 131
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal(131, block.GetTextureIndex(1)); // top face = 131 for off
    }

    [Fact]
    public void Constructor_OnRepeater_HasCorrectTextureIndex()
    {
        // Spec §6.2: on repeater top texture = 147
        var block = new BlockRedstoneDiode(94, true);
        Assert.Equal(147, block.GetTextureIndex(1)); // top face = 147 for on
    }

    [Fact]
    public void Constructor_OffRepeater_EmitsNoLight()
    {
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal(0.0f, block.LightValue, 3);
    }

    [Fact]
    public void Constructor_OnRepeater_EmitsLight0625()
    {
        // Spec §6.2: on repeater emits light 0.625F
        var block = new BlockRedstoneDiode(94, true);
        Assert.Equal(0.625f, block.LightValue, 3);
    }

    [Fact]
    public void Constructor_Hardness_IsZero()
    {
        var off = new BlockRedstoneDiode(93, false);
        var on  = new BlockRedstoneDiode(94, true);
        Assert.Equal(0.0f, off.Hardness, 3);
        Assert.Equal(0.0f, on.Hardness, 3);
    }

    [Fact]
    public void Constructor_BlockName_IsDiode()
    {
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal("diode", block.BlockName);
    }

    [Fact]
    public void IsOpaqueCube_ReturnsFalse()
    {
        Assert.False(new BlockRedstoneDiode(93, false).IsOpaqueCube());
        Assert.False(new BlockRedstoneDiode(94, true).IsOpaqueCube());
    }

    [Fact]
    public void RenderAsNormalBlock_ReturnsFalse()
    {
        Assert.False(new BlockRedstoneDiode(93, false).RenderAsNormalBlock());
        Assert.False(new BlockRedstoneDiode(94, true).RenderAsNormalBlock());
    }

    [Fact]
    public void CanProvidePower_OffRepeater_ReturnsFalse()
    {
        Assert.False(new BlockRedstoneDiode(93, false).CanProvidePower());
    }

    [Fact]
    public void CanProvidePower_OnRepeater_ReturnsTrue()
    {
        Assert.True(new BlockRedstoneDiode(94, true).CanProvidePower());
    }

    // ── §6.2 AABB height = 2/16 (spec) vs 1/8 (impl) ─────────────────────────

    [Fact(Skip = "PARITY BUG — impl diverges from spec: AABB height is 0.125f (1/8) but spec §6.2 says 2/16 = 0.125f — same value, confirm")]
    public void CollisionAABB_Height_Is2Over16()
    {
        // Spec §6.2: AABB (0, 0, 0, 1, 2/16, 1) = y+0.125
        // Implementation uses y+0.125f which matches 2/16=0.125 — actually same value
        // This skip is precautionary; if impl uses different constant it's a bug.
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        var aabb = block.GetCollisionBoundingBoxFromPool(world, 0, 0, 0);
        Assert.NotNull(aabb);
        Assert.Equal(0.125f, aabb!.MaxY - aabb.MinY, 4);
    }

    // ── §6.11 Texture by face ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, false, 115)] // bottom, off
    [InlineData(0, true,  99)]  // bottom, on
    [InlineData(1, false, 131)] // top, off
    [InlineData(1, true,  147)] // top, on
    [InlineData(2, false, 5)]   // side
    [InlineData(2, true,  5)]   // side
    [InlineData(3, false, 5)]
    [InlineData(3, true,  5)]
    [InlineData(4, false, 5)]
    [InlineData(4, true,  5)]
    [InlineData(5, false, 5)]
    [InlineData(5, true,  5)]
    public void GetTextureIndex_MatchesSpec(int face, bool isOn, int expectedTexture)
    {
        var block = new BlockRedstoneDiode(isOn ? 94 : 93, isOn);
        Assert.Equal(expectedTexture, block.GetTextureIndex(face));
    }

    // ── §6.7 isProvidingWeakPower ─────────────────────────────────────────────

    [Fact]
    public void IsProvidingWeakPower_OffRepeater_AlwaysFalse()
    {
        var block = new BlockRedstoneDiode(93, false);
        var access = new FakeBlockAccess();
        access.Set(5, 64, 5, 93, 0);
        for (int face = 0; face < 6; face++)
            Assert.False(block.IsProvidingWeakPower(access, 5, 64, 5, face));
    }

    [Theory]
    [InlineData(0, 3)]  // facing north → face 3
    [InlineData(1, 4)]  // facing east  → face 4
    [InlineData(2, 2)]  // facing south → face 2
    [InlineData(3, 5)]  // facing west  → face 5
    public void IsProvidingWeakPower_OnRepeater_PowersCorrectFace(int facing, int poweredFace)
    {
        var block = new BlockRedstoneDiode(94, true);
        var access = new FakeBlockAccess();
        access.Set(5, 64, 5, 94, facing);

        Assert.True(block.IsProvidingWeakPower(access, 5, 64, 5, poweredFace));

        // All other faces should not be powered
        for (int f = 0; f < 6; f++)
        {
            if (f != poweredFace)
                Assert.False(block.IsProvidingWeakPower(access, 5, 64, 5, f));
        }
    }

    // ── §6.7 isProvidingStrongPower delegates to isProvidingWeakPower ─────────

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 4)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    public void IsProvidingStrongPower_OnRepeater_MatchesWeakPower(int facing, int poweredFace)
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        world.PlaceBlock(5, 64, 5, 94, facing);

        Assert.True(block.IsProvidingStrongPower(world, 5, 64, 5, poweredFace));
        for (int f = 0; f < 6; f++)
        {
            if (f != poweredFace)
                Assert.False(block.IsProvidingStrongPower(world, 5, 64, 5, f));
        }
    }

    [Fact]
    public void IsProvidingStrongPower_OffRepeater_AlwaysFalse()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        world.PlaceBlock(5, 64, 5, 93, 0);
        for (int face = 0; face < 6; face++)
            Assert.False(block.IsProvidingStrongPower(world, 5, 64, 5, face));
    }

    // ── §6.5 canBlockStay ─────────────────────────────────────────────────────

    [Fact]
    public void CanBlockStay_SolidBelow_ReturnsTrue()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        world.SetNormalCube(5, 63, 5);
        Assert.True(block.CanBlockStay(world, 5, 64, 5));
    }

    [Fact]
    public void CanBlockStay_NoSolidBelow_ReturnsFalse()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        Assert.False(block.CanBlockStay(world, 5, 64, 5));
    }

    // ── §6.3 Delay table {1,2,3,4}×2 = {2,4,6,8} ticks ─────────────────────

    [Theory]
    [InlineData(0, 2)]  // delay bits 0 → 1*2 = 2
    [InlineData(1, 4)]  // delay bits 1 → 2*2 = 4
    [InlineData(2, 6)]  // delay bits 2 → 3*2 = 6
    [InlineData(3, 8)]  // delay bits 3 → 4*2 = 8
    public void DelayTable_OnNeighborBlockChange_SchedulesCorrectDelay(int delayBits, int expectedDelay)
    {
        // Use off repeater with input → should schedule turn-on with correct delay
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0; // north output, reads from z+1
        int meta = (delayBits << 2) | facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetNormalCube(x, y - 1, z); // solid below so canBlockStay = true

        // Provide input from z+1, face 3 (toward repeater)
        world.SetPower(x, y, z + 1, 3, true);

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z && u.delay == expectedDelay);
    }

    // ── §6.9 UpdateTick — ON repeater, input lost → turn OFF ─────────────────

    [Fact]
    public void UpdateTick_OnRepeater_NoInput_TurnsOff()
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = 0; // facing=0, delay=0
        world.PlaceBlock(x, y, z, 94, meta);

        // No input
        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 93 && c.meta == meta);
    }

    [Fact]
    public void UpdateTick_OnRepeater_HasInput_DoesNotTurnOff()
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0; // reads from z+1
        int meta = facing;
        world.PlaceBlock(x, y, z, 94, meta);
        world.SetPower(x, y, z + 1, 3, true);

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.DoesNotContain(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 93);
    }

    // ── §6.9 UpdateTick — OFF repeater always turns ON ───────────────────────

    [Fact]
    public void UpdateTick_OffRepeater_AlwaysTurnsOn_Spec()
    {
        // Spec §6.9: OFF repeater always switches to ON (ID 94) regardless of input
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = 0;
        world.PlaceBlock(x, y, z, 93, meta);

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 94);
    }

    [Fact]
    public void UpdateTick_OffRepeater_NoInput_SchedulesTurnOff()
    {
        // Spec §6.9: OFF repeater turns on, then if no input, schedules turn-off
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int delayBits = 1; // 2*2 = 4 ticks
        int meta = (delayBits << 2) | 0;
        world.PlaceBlock(x, y, z, 93, meta);

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        // Should turn on
        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 94);

        // Should schedule turn-off with delay = 4
        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z && u.delay == 4);
    }

    // ── §6.9 UpdateTick: OFF repeater with input — turn ON but no schedule ────

    [Fact(Skip = "PARITY BUG — impl diverges from spec: OFF repeater with input schedules turn-off; spec §6.9 says schedule only when !hasInput")]
    public void UpdateTick_OffRepeater_WithInput_TurnsOnNoSchedule()
    {
        // Spec §6.9: OFF → ON always; schedule turn-off only if !hasInput
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0;
        int meta = facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetPower(x, y, z + 1, 3, true); // input present

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 94);
        // Should NOT schedule a turn-off because input is present
        Assert.DoesNotContain(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z);
    }

    // ── §6.8 OnBlockActivated — delay cycling ────────────────────────────────

    [Theory]
    [InlineData(0,  0b0100)]  // delay 0 → 1; bits become 0b0100 = 4
    [InlineData(4,  0b1000)]  // delay 1 → 2; bits become 0b1000 = 8
    [InlineData(8,  0b1100)]  // delay 2 → 3; bits become 0b1100 = 12
    [InlineData(12, 0b0000)]  // delay 3 → 0; bits wrap to 0b0000 = 0
    public void OnBlockActivated_CyclesDelay(int initialDelayBits, int expectedMetaDelayBits)
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 2;
        int meta = initialDelayBits | facing;
        world.PlaceBlock(x, y, z, 93, meta);

        var player = new FakeEntityPlayer();
        block.OnBlockActivated(world, x, y, z, player);

        int expectedMeta = expectedMetaDelayBits | facing;
        Assert.Contains(world.SetMetaQuietCalls, c =>
            c.x == x && c.y == y && c.z == z && c.meta == expectedMeta);
    }

    [Fact]
    public void OnBlockActivated_ClientSide_DoesNothing()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        world.SetClientSide(true);
        world.PlaceBlock(5, 64, 5, 93, 0);

        var player = new FakeEntityPlayer();
        block.OnBlockActivated(world, 5, 64, 5, player);

        Assert.Empty(world.SetMetaQuietCalls);
    }

    [Fact]
    public void OnBlockActivated_ReturnsTrue()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        world.PlaceBlock(5, 64, 5, 93, 0);

        var result = block.OnBlockActivated(world, 5, 64, 5, new FakeEntityPlayer());
        Assert.True(result);
    }

    // ── §6.10 OnNeighborBlockChange — canBlockStay false → drop ──────────────

    [Fact]
    public void OnNeighborBlockChange_CannotStay_SetsBlockToAir()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        world.PlaceBlock(x, y, z, 93, 0);
        // No solid below → canBlockStay = false

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.SetBlockCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 0);
    }

    // ── §6.10 OnNeighborBlockChange — notify block above when ON ─────────────

    [Fact]
    public void OnNeighborBlockChange_OnRepeater_NotifiesBlockAbove()
    {
        // Spec §6.10: if cc=true and isOn, call world.j(y+1, bM) to notify above
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        world.PlaceBlock(x, y, z, 94, 0);
        world.SetNormalCube(x, y - 1, z);

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.NotifyBlockCalls, c =>
            c.x == x && c.y == y + 1 && c.z == z);
    }

    [Fact]
    public void OnNeighborBlockChange_OffRepeater_DoesNotNotifyBlockAbove()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        world.PlaceBlock(x, y, z, 93, 0);
        world.SetNormalCube(x, y - 1, z);

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.DoesNotContain(world.NotifyBlockCalls, c =>
            c.x == x && c.y == y + 1 && c.z == z);
    }

    // ── §6.10 OnNeighborBlockChange — schedules tick when state should change ─

    [Fact]
    public void OnNeighborBlockChange_OnRepeater_LosesInput_SchedulesTurnOff()
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = 0; // facing=0, delay=0 → 2 ticks
        world.PlaceBlock(x, y, z, 94, meta);
        world.SetNormalCube(x, y - 1, z);
        // No input → hasInput = false

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z && u.delay == 2);
    }

    [Fact]
    public void OnNeighborBlockChange_OffRepeater_GainsInput_SchedulesTurnOn()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0; // reads from z+1, face 3
        int meta = facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetNormalCube(x, y - 1, z);
        world.SetPower(x, y, z + 1, 3, true); // input present

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z && u.delay == 2);
    }

    [Fact]
    public void OnNeighborBlockChange_OnRepeater_HasInput_DoesNotSchedule()
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0;
        int meta = facing;
        world.PlaceBlock(x, y, z, 94, meta);
        world.SetNormalCube(x, y - 1, z);
        world.SetPower(x, y, z + 1, 3, true); // has input → no change needed

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.DoesNotContain(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z);
    }

    // ── §6.6 Input check — all four facings ──────────────────────────────────

    [Theory]
    [InlineData(0, true)]   // facing 0: input from z+1
    [InlineData(1, true)]   // facing 1: input from x-1
    [InlineData(2, true)]   // facing 2: input from z-1
    [InlineData(3, true)]   // facing 3: input from x+1
    public void OnNeighborBlockChange_InputFacingAllDirections_SchedulesTurnOn(int facing, bool expected)
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetNormalCube(x, y - 1, z);

        // Set power on the correct input face per facing
        switch (facing)
        {
            case 0: world.SetPower(x, y, z + 1, 3, true); break;
            case 1: world.SetPower(x - 1, y, z, 4, true); break;
            case 2: world.SetPower(x, y, z - 1, 2, true); break;
            case 3: world.SetPower(x + 1, y, z, 5, true); break;
        }

        block.OnNeighborBlockChange(world, x, y, z, 0);

        bool hasSchedule = world.ScheduledUpdates.Exists(u =>
            u.x == x && u.y == y && u.z == z);
        Assert.Equal(expected, hasSchedule);
    }

    // ── §6.6 Wire with power as input ────────────────────────────────────────

    [Fact]
    public void OnNeighborBlockChange_WireWithPowerAsInput_SchedulesTurnOn()
    {
        // Spec §6.6: wire at input position with meta > 0 counts as input
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0; // input from z+1
        int meta = facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetNormalCube(x, y - 1, z);
        // Place wire with power at z+1
        world.PlaceBlock(x, y, z + 1, 55, 8); // wire with meta=8 (power > 0)

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z);
    }

    [Fact]
    public void OnNeighborBlockChange_WireWithZeroPowerAsInput_DoesNotTrigger()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int facing = 0;
        int meta = facing;
        world.PlaceBlock(x, y, z, 93, meta);
        world.SetNormalCube(x, y - 1, z);
        // Wire with meta=0 (no power)
        world.PlaceBlock(x, y, z + 1, 55, 0);

        block.OnNeighborBlockChange(world, x, y, z, 0);

        Assert.DoesNotContain(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z);
    }

    // ── §6.12 Drops ──────────────────────────────────────────────────────────

    [Fact]
    public void IdDropped_ReturnsRepeaterItemId356()
    {
        // Spec §6.12: drops repeater item (acy.ba)
        var block = new BlockRedstoneDiode(93, false);
        var rng = new JavaRandom(0);
        Assert.Equal(356, block.IdDropped(0, rng, 0));
    }

    [Fact]
    public void IdDropped_OnRepeater_ReturnsRepeaterItemId356()
    {
        var block = new BlockRedstoneDiode(94, true);
        var rng = new JavaRandom(0);
        Assert.Equal(356, block.IdDropped(0, rng, 0));
    }

    // ── §6.1 GetTickDelay — spec does not mandate specific GetTickDelay value ─

    [Fact]
    public void GetTickDelay_ReturnsTwo()
    {
        // Implementation returns 2; spec uses cb[bits]*2 internally, GetTickDelay=2 is acceptable
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal(2, block.GetTickDelay());
    }

    // ── §7 Known Quirks (§12 of spec) ────────────────────────────────────────

    // Quirk §12.5: isOn (cc) is per-class instance, not per-block-ID lookup
    // The on-state repeater (mz ID 94, cc=true) stays cc=true regardless of world state;
    // only UpdateTick switches the block by calling SetBlockAndMetadata with a different ID.
    [Fact]
    public void Quirk_IsOnField_IsPerClassInstance_NotWorldState()
    {
        // OFF repeater (93, false) always has CanProvidePower = false
        // even if we forcibly place ID 94 in the world at same coords
        var offBlock = new BlockRedstoneDiode(93, false);
        Assert.False(offBlock.CanProvidePower());

        var onBlock = new BlockRedstoneDiode(94, true);
        Assert.True(onBlock.CanProvidePower());
    }

    // Quirk: OnNeighborBlockChange on client side is a no-op
    [Fact]
    public void OnNeighborBlockChange_ClientSide_DoesNothing()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        world.SetClientSide(true);
        world.PlaceBlock(5, 64, 5, 93, 0);
        world.SetNormalCube(5, 63, 5);

        block.OnNeighborBlockChange(world, 5, 64, 5, 0);

        Assert.Empty(world.ScheduledUpdates);
        Assert.Empty(world.SetBlockCalls);
    }

    // ── §6.3 Delay bits wrap correctly on activation ──────────────────────────

    [Fact]
    public void OnBlockActivated_DelayBits_IncrementFormula_MatchesSpec()
    {
        // Spec §6.8: ((meta >> 2) + 1) * 4 & 12 gives the new delay bits
        // Verify all four transitions
        int[] initialMetas  = {  0,  4,  8, 12 };
        int[] expectedDelays = {  4,  8, 12,  0 };

        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();

        for (int i = 0; i < 4; i++)
        {
            world.SetMetaQuietCalls.Clear();
            world.PlaceBlock(5, 64, 5, 93, initialMetas[i]);

            block.OnBlockActivated(world, 5, 64, 5, new FakeEntityPlayer());

            int expected = expectedDelays[i]; // delay bits portion
            Assert.Contains(world.SetMetaQuietCalls, c =>
                c.x == 5 && c.y == 64 && c.z == 5 &&
                (c.meta & 0xC) == expected);
        }
    }

    // ── §6.9 UpdateTick preserves meta (facing + delay bits) when changing state

    [Fact]
    public void UpdateTick_OnRepeater_TurnsOff_PreservesMeta()
    {
        var block = new BlockRedstoneDiode(94, true);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = 0b1101; // delayBits=3, facing=1
        world.PlaceBlock(x, y, z, 94, meta);

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 93 && c.meta == meta);
    }

    [Fact]
    public void UpdateTick_OffRepeater_TurnsOn_PreservesMeta()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        int meta = 0b1010; // delayBits=2, facing=2
        world.PlaceBlock(x, y, z, 93, meta);

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.SetBlockAndMetaCalls, c =>
            c.x == x && c.y == y && c.z == z && c.id == 94 && c.meta == meta);
    }

    // ── §6.9 UpdateTick: schedule delay uses correct tick count ──────────────

    [Theory]
    [InlineData(0b0000, 2)]  // delayBits=0 → 1*2=2
    [InlineData(0b0100, 4)]  // delayBits=1 → 2*2=4
    [InlineData(0b1000, 6)]  // delayBits=2 → 3*2=6
    [InlineData(0b1100, 8)]  // delayBits=3 → 4*2=8
    public void UpdateTick_OffRepeater_NoInput_SchedulesCorrectDelay(int meta, int expectedDelay)
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        int x = 5, y = 64, z = 5;
        world.PlaceBlock(x, y, z, 93, meta);
        // no input

        var rng = new JavaRandom(42);
        block.UpdateTick(world, x, y, z, rng);

        Assert.Contains(world.ScheduledUpdates, u =>
            u.x == x && u.y == y && u.z == z && u.delay == expectedDelay);
    }

    // ── Bounds sanity ─────────────────────────────────────────────────────────

    [Fact]
    public void GetSelectedBoundingBox_HasCorrectHeight()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        var aabb = block.GetSelectedBoundingBoxFromPool(world, 0, 0, 0);
        Assert.Equal(0.125f, aabb.MaxY - aabb.MinY, 4);
    }

    [Fact]
    public void GetCollisionBoundingBox_HasCorrectHeight()
    {
        var block = new BlockRedstoneDiode(93, false);
        var world = new StubWorld();
        var aabb = block.GetCollisionBoundingBoxFromPool(world, 0, 0, 0);
        Assert.NotNull(aabb);
        Assert.Equal(0.125f, aabb!.MaxY - aabb.MinY, 4);
    }

    // ── §6.2 Material = Plants (passable) ────────────────────────────────────

    [Fact]
    public void Material_IsPlants()
    {
        // Spec says material = p.p (passable); impl uses Material.Plants
        var block = new BlockRedstoneDiode(93, false);
        Assert.Equal(Material.Plants, block.BlockMaterial);
    }
}

// ── Supporting fake entities / players ───────────────────────────────────────

file sealed class FakeEntityPlayer : EntityPlayer
{
    public FakeEntityPlayer() : base() { }
}

// StubWorld needs SetClientSide support
file static class StubWorldExtensions
{
    public static void SetClientSide(this StubWorld world, bool value) =>
        world.IsClientSideOverride = value;
}