using System;
using System.Collections.Generic;
using Xunit;
using SpectraSharp.Core;

namespace SpectraSharp.Tests;

// ---------------------------------------------------------------------------
// Hand-written fakes
// ---------------------------------------------------------------------------

/// <summary>Simple in-memory world for BlockSand tests.</summary>
file class FakeWorld : IWorld
{
    private readonly Dictionary<(int, int, int), int> _blocks = new();

    public int GetBlockId(int x, int y, int z)
        => _blocks.TryGetValue((x, y, z), out var id) ? id : 0;

    public bool SetBlock(int x, int y, int z, int id) { _blocks[(x, y, z)] = id; return true; }
    public void SetBlockRaw(int x, int y, int z, int id) => _blocks[(x, y, z)] = id;

    // Convenience: pre-place a block without triggering any game logic
    public void Place(int x, int y, int z, int id) => _blocks[(x, y, z)] = id;

    // ── IBlockAccess stubs ───────────────────────────────────────────────────
    public int      GetBlockMetadata(int x, int y, int z)              => 0;
    public int      GetLightValue(int x, int y, int z, int e)          => e;
    public float    GetBrightness(int x, int y, int z, int e)          => 1f;
    public Material GetBlockMaterial(int x, int y, int z)              => Material.Air;
    public bool     IsOpaqueCube(int x, int y, int z)                  => false;
    public bool     IsWet(int x, int y, int z)                         => false;
    public object?  GetTileEntity(int x, int y, int z)                 => null;
    public float    GetUnknownFloat(int x, int y, int z)               => 0f;
    public bool     GetUnknownBool(int x, int y, int z)                => false;
    public object   GetContextObject()                                  => new object();
    public int      GetHeight()                                         => 128;

    // ── IWorld stubs ─────────────────────────────────────────────────────────
    public bool         IsClientSide                                    { get; set; } = false;
    public JavaRandom   Random                                          { get; set; } = new JavaRandom(0);
    public bool         IsNether                                        { get; set; } = false;
    public bool         SuppressUpdates                                 { get; set; } = false;
    public int          DimensionId                                     { get; set; } = 0;
    public void SpawnEntity(Entity entity)                              { }
    public bool SetBlockAndMetadata(int x, int y, int z, int id, int m){ _blocks[(x, y, z)] = id; return true; }
    public bool SetMetadata(int x, int y, int z, int m)                => true;
    public void SetBlockSilent(int x, int y, int z, int id)            => _blocks[(x, y, z)] = id;
    public bool CanFreezeAtLocation(int x, int y, int z)               => false;
    public bool CanSnowAtLocation(int x, int y, int z)                 => false;
    public void ScheduleBlockUpdate(int x, int y, int z, int id, int d){ }
    public bool IsAreaLoaded(int x, int y, int z, int r)               => true;
    public void NotifyNeighbors(int x, int y, int z, int id)           { }
    public int  GetLightBrightness(int x, int y, int z)                => 15;
    public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d) { }
    public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)   => false;
    public bool IsRaining()                                             => false;
    public bool IsBlockExposedToRain(int x, int y, int z)              => false;
    public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool f) { }
}


// ---------------------------------------------------------------------------
// Test class
// ---------------------------------------------------------------------------

public class BlockSandTests
{
    // Block IDs referenced throughout
    private const int IdAir        = 0;
    private const int IdFlowWater  = 8;
    private const int IdStillWater = 9;
    private const int IdFlowLava   = 10;
    private const int IdStillLava  = 11;
    private const int IdFire       = 51;
    private const int IdSand       = 12;
    private const int IdStone      = 1;
    private const int IdDirt       = 3;

    private static BlockSand CreateSand()
        => new BlockSand(IdSand, /*texture=*/2, Material.Sand);

    private static JavaRandom Rng() => new JavaRandom(42);

    // -----------------------------------------------------------------------
    // §2 — Gravity Fall Logic: basic cases
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_DoesNothing_WhenYIsZero()
    {
        var world = new FakeWorld();
        world.Place(0, 0, 0, IdSand);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 0, 0, Rng());

        // Sand must remain at y=0
        Assert.Equal(IdSand, world.GetBlockId(0, 0, 0));
    }

    [Fact]
    public void BlockTick_DoesNothing_WhenBlockBelowIsSolid()
    {
        var world = new FakeWorld();
        world.Place(0, 1, 0, IdSand);
        world.Place(0, 0, 0, IdStone);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 1, 0, Rng());

        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 0, 0));
    }

    [Fact]
    public void BlockTick_Falls_WhenBlockBelowIsAir()
    {
        var world = new FakeWorld();
        world.Place(0, 5, 0, IdSand);
        // y=0..4 are all air (default)
        var sand = CreateSand();

        sand.BlockTick(world, 0, 5, 0, Rng());

        // Sand must be removed from y=5
        Assert.Equal(IdAir, world.GetBlockId(0, 5, 0));
        // Sand must rest at y=1 (y=0 is the lowest non-zero position the while loop stops at)
        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
    }

    [Fact]
    public void BlockTick_InstantFall_StopsOnSolidBlock()
    {
        var world = new FakeWorld();
        world.Place(0, 5, 0, IdSand);
        world.Place(0, 2, 0, IdStone);
        // y=3, y=4 are air
        var sand = CreateSand();

        sand.BlockTick(world, 0, 5, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 5, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 3, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 2, 0));
    }

    [Fact]
    public void BlockTick_Falls_OneSingleBlock_WhenOneAirBelow()
    {
        var world = new FakeWorld();
        world.Place(0, 2, 0, IdSand);
        world.Place(0, 1, 0, IdAir);
        world.Place(0, 0, 0, IdStone);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 2, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 2, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 0, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Fallable block IDs (all six must allow fall)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(IdAir,        "air")]
    [InlineData(IdFlowWater,  "flowing water")]
    [InlineData(IdStillWater, "still water")]
    [InlineData(IdFlowLava,   "flowing lava")]
    [InlineData(IdStillLava,  "still lava")]
    [InlineData(IdFire,       "fire")]
    public void BlockTick_Falls_IntoFallableBlock(int belowId, string description)
    {
        var world = new FakeWorld();
        world.Place(0, 2, 0, IdSand);
        world.Place(0, 1, 0, belowId);
        world.Place(0, 0, 0, IdStone); // solid bottom
        var sand = CreateSand();

        sand.BlockTick(world, 0, 2, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 2, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
    }

    [Theory]
    [InlineData(IdStone, "stone")]
    [InlineData(IdDirt,  "dirt")]
    [InlineData(IdSand,  "sand (itself)")]
    public void BlockTick_DoesNotFall_IntoSolidOrSameBlock(int belowId, string description)
    {
        var world = new FakeWorld();
        world.Place(0, 2, 0, IdSand);
        world.Place(0, 1, 0, belowId);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 2, 0, Rng());

        // Sand stays at y=2
        Assert.Equal(IdSand, world.GetBlockId(0, 2, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Instant-fall scans to lowest free position
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_InstantFall_SkipsMultipleFallableBlocks()
    {
        var world = new FakeWorld();
        world.Place(0, 6, 0, IdSand);
        // y=5 -> fire, y=4 -> water, y=3 -> lava, y=2 -> air, y=1 -> air, y=0 = ground
        world.Place(0, 5, 0, IdFire);
        world.Place(0, 4, 0, IdStillWater);
        world.Place(0, 3, 0, IdFlowLava);
        world.Place(0, 2, 0, IdAir);
        world.Place(0, 1, 0, IdAir);
        world.Place(0, 0, 0, IdStone);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 6, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 6, 0));
        // Lowest non-solid position above stone is y=1
        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
    }

    [Fact]
    public void BlockTick_InstantFall_LandsAtY1_WhenAllBelowAreAir()
    {
        // The while loop condition is targetY > 0, so it stops at targetY==1
        // when y==0 would also be air. Resting position is y=1.
        var world = new FakeWorld();
        world.Place(0, 4, 0, IdSand);
        // y=0..3 all air
        var sand = CreateSand();

        sand.BlockTick(world, 0, 4, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 4, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 1, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Y boundary: y==1 with air below (y==0)
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_YEqualsOne_AirBelow_FallsToY1Target()
    {
        // Sand at y=1, air at y=0. IsFallable passes for y-1=0 (air).
        // The while loop: targetY starts at 0, condition is targetY > 0 — false immediately.
        // So sand is placed at y=0.
        var world = new FakeWorld();
        world.Place(0, 1, 0, IdSand);
        // y=0 is air (default)
        var sand = CreateSand();

        sand.BlockTick(world, 0, 1, 0, Rng());

        Assert.Equal(IdAir, world.GetBlockId(0, 1, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 0, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — BlockID property preserved
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockSand_BlockID_IsCorrect()
    {
        var sand = CreateSand();
        Assert.Equal(IdSand, sand.BlockID);
    }

    // -----------------------------------------------------------------------
    // §2 — No side-effects when sand does not fall
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_NoOtherBlocksModified_WhenNotFalling()
    {
        var world = new FakeWorld();
        world.Place(0, 1, 0, IdSand);
        world.Place(0, 0, 0, IdStone);
        world.Place(1, 1, 0, IdDirt);
        world.Place(0, 1, 1, IdDirt);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 1, 0, Rng());

        Assert.Equal(IdSand,  world.GetBlockId(0, 1, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 0, 0));
        Assert.Equal(IdDirt,  world.GetBlockId(1, 1, 0));
        Assert.Equal(IdDirt,  world.GetBlockId(0, 1, 1));
    }

    [Fact]
    public void BlockTick_NoOtherBlocksModified_WhenFalling()
    {
        var world = new FakeWorld();
        world.Place(0, 3, 0, IdSand);
        world.Place(0, 2, 0, IdAir);
        world.Place(0, 1, 0, IdStone);
        world.Place(1, 3, 0, IdDirt);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 3, 0, Rng());

        Assert.Equal(IdAir,   world.GetBlockId(0, 3, 0));
        Assert.Equal(IdSand,  world.GetBlockId(0, 2, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 1, 0));
        Assert.Equal(IdDirt,  world.GetBlockId(1, 3, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Sand does not fall upward or sideways (constructor/identity check)
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockSand_IsSubclassOfBlock()
    {
        var sand = CreateSand();
        Assert.IsAssignableFrom<Block>(sand);
    }

    // -----------------------------------------------------------------------
    // §2 — IsFallable: block IDs not in the set must NOT allow fall
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(2,  "grass")]
    [InlineData(3,  "dirt")]
    [InlineData(4,  "cobblestone")]
    [InlineData(5,  "planks")]
    [InlineData(7,  "bedrock")]
    [InlineData(12, "sand")]
    [InlineData(13, "gravel")]
    [InlineData(17, "log")]
    [InlineData(49, "obsidian")]
    public void BlockTick_DoesNotFall_IntoNonFallableBlockId(int belowId, string name)
    {
        var world = new FakeWorld();
        world.Place(0, 2, 0, IdSand);
        world.Place(0, 1, 0, belowId);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 2, 0, Rng());

        Assert.Equal(IdSand, world.GetBlockId(0, 2, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Instant-fall does NOT chain into a new sand block already resting
    //       (the block at targetY is replaced, not appended below)
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_InstantFall_ReplacesWaterAtTarget()
    {
        var world = new FakeWorld();
        world.Place(0, 3, 0, IdSand);
        world.Place(0, 2, 0, IdStillWater);
        world.Place(0, 1, 0, IdStone);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 3, 0, Rng());

        Assert.Equal(IdAir,  world.GetBlockId(0, 3, 0));
        Assert.Equal(IdSand, world.GetBlockId(0, 2, 0));
        Assert.Equal(IdStone, world.GetBlockId(0, 1, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Y==0: sand at bedrock level must not fall (guard y <= 0)
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_YEqualsZero_NeverFalls()
    {
        var world = new FakeWorld();
        world.Place(0, 0, 0, IdSand);
        var sand = CreateSand();

        sand.BlockTick(world, 0, 0, 0, Rng());

        Assert.Equal(IdSand, world.GetBlockId(0, 0, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — Negative Y guard (should be treated same as y==0 — no fall)
    // -----------------------------------------------------------------------

    [Fact]
    public void BlockTick_NegativeY_NeverFalls()
    {
        var world = new FakeWorld();
        world.Place(0, -1, 0, IdSand);
        var sand = CreateSand();

        sand.BlockTick(world, 0, -1, 0, Rng());

        Assert.Equal(IdSand, world.GetBlockId(0, -1, 0));
    }

    // -----------------------------------------------------------------------
    // §2 — X/Z position is preserved exactly after fall
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(7,  0,  5)]
    [InlineData(-3, 0, -9)]
    [InlineData(0,  0,  0)]
    [InlineData(15, 0, 15)]
    public void BlockTick_PreservesXZPosition(int x, int startY, int z)
    {
        var world = new FakeWorld();
        int sy = startY + 3;
        world.Place(x, sy, z, IdSand);
        world.Place(x, startY, z, IdStone);
        var sand = CreateSand();

        sand.BlockTick(world, x, sy, z, Rng());

        // Sand should land directly above stone
        Assert.Equal(IdSand, world.GetBlockId(x, startY + 1, z));
        Assert.Equal(IdAir,  world.GetBlockId(x, sy, z));
    }
}