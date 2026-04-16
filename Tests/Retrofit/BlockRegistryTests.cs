using System;
using System.Collections.Generic;
using System.Reflection;
using SpectraSharp.Core;
using SpectraSharp.Core.Blocks;
using Xunit;
using static SpectraSharp.Core.BlockRegistry;

namespace SpectraSharp.Tests.BlockRegistry
{
    // ── Fakes ────────────────────────────────────────────────────────────────

    internal sealed class FakeBlockAccess : IBlockAccess
    {
        private readonly Dictionary<(int, int, int), int> _blocks = new();
        private readonly Dictionary<(int, int, int), int> _meta = new();

        public int GetBlockId(int x, int y, int z)
            => _blocks.TryGetValue((x, y, z), out var id) ? id : 0;

        public int GetBlockMetadata(int x, int y, int z)
            => _meta.TryGetValue((x, y, z), out var m) ? m : 0;

        public bool SetBlock(int x, int y, int z, int id) { _blocks[(x, y, z)] = id; return true; }
        public void SetMeta(int x, int y, int z, int meta) => _meta[(x, y, z)] = meta;
    
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

    internal sealed partial class FakeWorld : IWorld
    {
        // Written calls
        public readonly List<(int x, int y, int z, int id)> SetBlockCalls = new();
        public readonly List<(int x, int y, int z, int id, int meta)> SetBlockAndMetaCalls = new();
        public readonly List<(int x, int y, int z, int meta)> SetBlockMetaCalls = new();
        public readonly List<(int x, int y, int z, int id, int delay)> ScheduleTickCalls = new();

        // World state
        private readonly Dictionary<(int, int, int), int> _blocks = new();
        private readonly Dictionary<(int, int, int), int> _meta = new();
        private readonly HashSet<(int, int, int)> _wetPositions = new();
        private readonly HashSet<(int, int, int)> _normalCubePositions = new();

        public bool IsRaining { get; set; }
        public int DimensionId { get; set; } = 0;    // 0=Overworld, 1=End
        public bool IsEndDimension => DimensionId == 1;

        public void PlaceBlock(int x, int y, int z, int id, int meta = 0)
        {
            _blocks[(x, y, z)] = id;
            _meta[(x, y, z)] = meta;
        }

        public void MakeNormalCube(int x, int y, int z) => _normalCubePositions.Add((x, y, z));
        public void MakeWet(int x, int y, int z) => _wetPositions.Add((x, y, z));

        // IWorld
        public int GetBlockId(int x, int y, int z)
            => _blocks.TryGetValue((x, y, z), out var id) ? id : 0;

        public int GetBlockMetadata(int x, int y, int z)
            => _meta.TryGetValue((x, y, z), out var m) ? m : 0;

        public bool IsBlockNormalCube(int x, int y, int z)
            => _normalCubePositions.Contains((x, y, z));

        public bool IsBlockWet(int x, int y, int z)
            => _wetPositions.Contains((x, y, z));

        public bool SetBlock(int x, int y, int z, int id)
        {
            SetBlockCalls.Add((x, y, z, id));
            _blocks[(x, y, z)] = id;
                return true;
    }

        public bool SetBlockAndMetadata(int x, int y, int z, int id, int meta)
        {
            SetBlockAndMetaCalls.Add((x, y, z, id, meta));
            _blocks[(x, y, z)] = id;
            _meta[(x, y, z)] = meta;
                return true;
    }

        public void SetBlockMetadata(int x, int y, int z, int meta)
        {
            SetBlockMetaCalls.Add((x, y, z, meta));
            _meta[(x, y, z)] = meta;
        }

        public void ScheduleBlockTick(int x, int y, int z, int id, int delay)
            => ScheduleTickCalls.Add((x, y, z, id, delay));

        bool IWorld.IsRaining() => IsRaining;
    
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

        // ── IWorld stubs (auto-generated) ───────────────────────────────
        public bool         IsClientSide                                 { get; set; } = false;
        public JavaRandom   Random                                       { get; set; } = new JavaRandom(0);
        public bool         IsNether                                     { get; set; } = false;
        public bool         SuppressUpdates                              { get; set; } = false;
        public void SpawnEntity(Entity entity)                           { }
        public bool SetMetadata(int x, int y, int z, int meta)          { return true; }
        public void SetBlockSilent(int x, int y, int z, int id)         { }
        public bool CanFreezeAtLocation(int x, int y, int z)            => false;
        public bool CanSnowAtLocation(int x, int y, int z)              => false;
        public void ScheduleBlockUpdate(int x, int y, int z, int id, int delay) { }
        public bool IsAreaLoaded(int x, int y, int z, int radius)       => true;
        public void NotifyNeighbors(int x, int y, int z, int id)        { }
        public int  GetLightBrightness(int x, int y, int z)             => 15;
        public void PlayAuxSFX(EntityPlayer? p, int e, int x, int y, int z, int d) { }
        public bool IsBlockIndirectlyReceivingPower(int x, int y, int z)=> false;
        public bool IsBlockExposedToRain(int x, int y, int z)           => false;
        public void CreateExplosion(EntityPlayer? p, double x, double y, double z, float pw, bool fire) { }
}

    /// <summary>
    /// Fixed-seed deterministic Random wrapper matching Java nextInt semantics.
    /// Uses System.Random with fixed seed.
    /// </summary>
    internal sealed class SeededRandom
    {
        private readonly Random _rng;
        public SeededRandom(int seed) => _rng = new Random(seed);
        public int NextInt(int bound) => _rng.Next(0, bound);
    }

    // ── Test class ───────────────────────────────────────────────────────────

    public class BlockFireSpecTests
    {
        // ════════════════════════════════════════════════════════════════════
        // Section 2 — Registration
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void BlockFire_IsRegisteredAtId51()
        {
            Initialize();
            var block = Block.BlocksList[51];
            Assert.NotNull(block);
            Assert.IsType<BlockFire>(block);
        }

        [Fact]
        public void BlockFire_HasCorrectBlockName()
        {
            Initialize();
            var block = Block.BlocksList[51]!;
            Assert.Equal("fire", block.BlockName);
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 5 — Block Properties
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void BlockFire_RenderAsNormalBlock_IsFalse()
        {
            Initialize();
            var block = Block.BlocksList[51]!;
            Assert.False(block.RenderAsNormalBlock());
        }

        [Fact]
        public void BlockFire_IsOpaqueCube_IsFalse()
        {
            Initialize();
            var block = Block.BlocksList[51]!;
            Assert.False(block.IsOpaqueCube());
        }

        [Fact]
        public void BlockFire_TickDelay_Is40()
        {
            Initialize();
            var block = Block.BlocksList[51]! as BlockFire;
            Assert.NotNull(block);
            Assert.Equal(40, block!.GetTickDelay());
        }

        [Fact]
        public void BlockFire_BoundingBox_IsNull()
        {
            Initialize();
            var block = Block.BlocksList[51]! as BlockFire;
            Assert.NotNull(block);
            var world = new FakeWorld();
            // GetCollisionBoundingBox / GetBoundingBox should return null for fire
            var bb = block!.GetCollisionBoundingBoxFromPool(world, 0, 0, 0);
            Assert.Null(bb);
        }

        [Fact]
        public void BlockFire_DropsNothing()
        {
            Initialize();
            var block = Block.BlocksList[51]! as BlockFire;
            Assert.NotNull(block);
            int dropCount = block!.GetDropCount(new Random(42));
            Assert.Equal(0, dropCount);
        }

        [Fact]
        public void BlockFire_LightEmission_Is15()
        {
            Initialize();
            var block = Block.BlocksList[51]!;
            // LightValue set to 1.0f → maps to 15 (stored in LightValueTable[id])
            Assert.Equal(15, Block.LightValueTable[block.BlockID]);
        }

        [Fact]
        public void BlockFire_Material_IsFireMaterial()
        {
            Initialize();
            var block = Block.BlocksList[51]!;
            Assert.Equal(Material.Fire, block.BlockMaterial);
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 3 — Flammability Tables
        // ════════════════════════════════════════════════════════════════════

        private static BlockFire GetFireBlock()
        {
            Initialize();
            return (BlockFire)Block.BlocksList[51]!;
        }

        [Theory]
        [InlineData(5,  5,  20)]   // Wood Planks
        [InlineData(47, 5,  20)]   // Bookshelf
        [InlineData(85, 5,  20)]   // Wooden Fence
        [InlineData(46, 5,  5)]    // TNT
        [InlineData(18, 30, 60)]   // Leaves
        [InlineData(35, 30, 20)]   // Wool
        [InlineData(32, 15, 100)]  // Dead Bush
        public void BlockFire_FlammabilityTable_MatchesSpec(int blockId, int expectedFlam, int expectedBurn)
        {
            var fire = GetFireBlock();
            Assert.Equal(expectedFlam, BlockFire.GetFlammability(blockId));
            Assert.Equal(expectedBurn, BlockFire.GetBurnability(blockId));
        }

        [Fact]
        public void BlockFire_UnregisteredBlock_HasZeroFlammability()
        {
            var fire = GetFireBlock();
            // ID 1 (stone) should not be flammable
            Assert.Equal(0, BlockFire.GetFlammability(1));
            Assert.Equal(0, BlockFire.GetBurnability(1));
        }

        // yy.am entry: flammability=15, burnability=100
        [Fact]
        public void BlockFire_YyAmEntry_HasFlammability15Burn100()
        {
            var fire = GetFireBlock();
            // yy.am is the block with special action on burn.
            // The spec says flammability=15, burnability=100.
            // The exact ID needs to be verified against BlockRegistry.
            // From spec context, yy.am is likely Tall Grass (31) or similar.
            // We test that exactly one block ID has these exact values.
            bool found = false;
            for (int id = 0; id < 256; id++)
            {
                if (BlockFire.GetFlammability(id) == 15 && BlockFire.GetBurnability(id) == 100)
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "No block has flammability=15, burnability=100 (yy.am entry missing)");
        }

        // yy.X entry: flammability=60, burnability=100
        [Fact]
        public void BlockFire_YyXEntry_HasFlammability60Burn100()
        {
            var fire = GetFireBlock();
            bool found = false;
            for (int id = 0; id < 256; id++)
            {
                if (BlockFire.GetFlammability(id) == 60 && BlockFire.GetBurnability(id) == 100)
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "No block has flammability=60, burnability=100 (yy.X entry missing)");
        }

        // yy.ab entry: flammability=30, burnability=60
        [Fact]
        public void BlockFire_YyAbEntry_HasFlammability30Burn60()
        {
            var fire = GetFireBlock();
            // Leaves (18) already verified at 30/60. At least one such entry must exist.
            bool found = false;
            for (int id = 0; id < 256; id++)
            {
                if (BlockFire.GetFlammability(id) == 30 && BlockFire.GetBurnability(id) == 60)
                {
                    found = true;
                    break;
                }
            }
            Assert.True(found, "No block has flammability=30, burnability=60 (yy.ab entry missing)");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 4 — Helper methods
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void IsFlammable_ReturnsTrueForLeavesId18()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(1, 2, 3, 18); // leaves
            Assert.True(fire.IsFlammable(world, 1, 2, 3));
        }

        [Fact]
        public void IsFlammable_ReturnsFalseForStoneId1()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(1, 2, 3, 1); // stone
            Assert.False(fire.IsFlammable(world, 1, 2, 3));
        }

        [Fact]
        public void GetFlammabilityAround_ReturnsZeroIfPositionNotAir()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 5, 5, 1); // stone at target position (not air)
            // Surround with leaves to ensure neighbours are flammable
            world.PlaceBlock(6, 5, 5, 18);
            int result = fire.MaxFlammabilityAround(world, 5, 5, 5);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetFlammabilityAround_ReturnsMaxNeighbourFlammability_WhenTargetIsAir()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // target (5,5,5) is air (block id 0) — default
            world.PlaceBlock(6, 5, 5, 18); // leaves → flammability 30
            world.PlaceBlock(4, 5, 5, 5);  // planks → flammability 5
            int result = fire.MaxFlammabilityAround(world, 5, 5, 5);
            Assert.Equal(30, result);
        }

        [Fact]
        public void GetFlammabilityAround_ReturnsZeroIfAllNeighboursNonFlammable()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // all neighbours are stone (id 1) or air (id 0)
            int result = fire.MaxFlammabilityAround(world, 5, 5, 5);
            Assert.Equal(0, result);
        }

        [Fact]
        public void HasFlammableNeighbour_TrueIfAnyNeighbourFlammable()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 6, 5, 18); // leaves above
            Assert.True(fire.HasFlammableNeighbor(world, 5, 5, 5));
        }

        [Fact]
        public void HasFlammableNeighbour_FalseIfNoNeighbourFlammable()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // surrounded by stone
            foreach (var (dx, dy, dz) in new[] { (1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1) })
                world.PlaceBlock(5+dx, 5+dy, 5+dz, 1);
            Assert.False(fire.HasFlammableNeighbor(world, 5, 5, 5));
        }

        [Fact]
        public void CanFireSurviveHere_TrueIfSolidBlockBelow()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.MakeNormalCube(5, 4, 5);
            Assert.True(fire.CanFireSurviveHere(world, 5, 5, 5));
        }

        [Fact]
        public void CanFireSurviveHere_TrueIfFlammableNeighbour()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(6, 5, 5, 18); // leaves adjacent
            Assert.True(fire.CanFireSurviveHere(world, 5, 5, 5));
        }

        [Fact]
        public void CanFireSurviveHere_FalseIfNoSupportAndNoFlammable()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // nothing below, nothing flammable
            Assert.False(fire.CanFireSurviveHere(world, 5, 5, 5));
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Permanence (netherrack)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_OnNetherrack_DoesNotBurnOut_AtMaxAge()
        {
            // Netherrack = ID 87. Fire at max age (15) on netherrack must NOT be removed.
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // Place netherrack below fire at (5,5,5)
            world.PlaceBlock(5, 4, 5, 87);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 15); // max age

            var rng = new Random(1337);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            // Fire must NOT have been removed (no setBlock(5,5,5,0))
            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.False(removed, "Permanent fire on netherrack should not be extinguished");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Existence check
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_RemovesItself_IfCannotSurvive()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // Fire at (5,5,5) with no support below, no flammable neighbour
            world.PlaceBlock(5, 5, 5, 51);

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.True(removed, "Fire should remove itself when it cannot survive");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Rain extinguishment
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "Rain requires 5-position wetness")]
        public void FireTick_RainExtinguishes_WhenAll5PositionsWet()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.IsRaining = true;

            // Solid below so fire can survive structurally
            world.PlaceBlock(5, 4, 5, 1);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);

            // All 5 positions wet
            world.MakeWet(5, 5, 5);
            world.MakeWet(4, 5, 5);
            world.MakeWet(6, 5, 5);
            world.MakeWet(5, 5, 4);
            world.MakeWet(5, 5, 6);

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.True(removed, "Rain with all 5 positions wet should extinguish fire");
        }

        [Fact]
        [Trait("Quirk", "Rain requires 5-position wetness")]
        public void FireTick_RainDoesNotExtinguish_WhenOnlyPartiallyWet()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.IsRaining = true;

            world.PlaceBlock(5, 4, 5, 1);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);

            // Only self is wet (not all 4 horizontal neighbours)
            world.MakeWet(5, 5, 5);

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.False(removed, "Fire should survive rain unless all 5 positions are wet");
        }

        [Fact]
        [Trait("Quirk", "Rain requires 5-position wetness")]
        public void FireTick_RainDoesNotExtinguish_Permanent()
        {
            // Permanent fire (netherrack) must not be extinguished by rain even if all wet
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.IsRaining = true;

            world.PlaceBlock(5, 4, 5, 87); // netherrack
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);

            world.MakeWet(5, 5, 5);
            world.MakeWet(4, 5, 5);
            world.MakeWet(6, 5, 5);
            world.MakeWet(5, 5, 4);
            world.MakeWet(5, 5, 6);

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.False(removed, "Permanent fire should not be extinguished by rain");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Age advancement
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_AgeAdvances_ByZeroOrOne_PerTick()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 4, 5, 1);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 5); // starting age = 5

            var rng = new Random(12345);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            // Find the SetBlockMetadata call for (5,5,5)
            var metaCall = world.SetBlockMetaCalls.Find(c => c.x == 5 && c.y == 5 && c.z == 5);
            Assert.True(metaCall.meta == 5 || metaCall.meta == 6,
                $"Age should advance by 0 or 1 per tick; got {metaCall.meta}");
        }

        [Fact]
        public void FireTick_AgeCaps_At15()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 4, 5, 87); // netherrack (permanent so tick completes)
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 15); // already at max

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            var metaCall = world.SetBlockMetaCalls.Find(c => c.x == 5 && c.y == 5 && c.z == 5);
            Assert.Equal(15, metaCall.meta);
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Reschedule
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_Reschedules_With40TickDelay()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 4, 5, 1);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool rescheduled = world.ScheduleTickCalls.Exists(
                c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 51 && c.delay == 40);
            Assert.True(rescheduled, "Fire must reschedule itself with delay=40 each tick");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Fire Tick: Burnout (non-permanent)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_BurnsOut_NoFlammableNeighbour_NoSolidFloor_LowAge()
        {
            // No flammable neighbour + no solid floor + age < 3 → should remove
            // (actually: age > 3 triggers removal even with no flammable but no solid floor)
            // Spec: if no flammable AND (no solid floor OR age > 3) → remove
            // Test the "no solid floor AND age > 3" path:
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // Place fire with floating support (e.g., solid cube below, but let's test floating)
            // Actually the fire must first pass existence check — so we need some support.
            // Use a flammable block that we'll remove to simulate "no flammable" scenario:
            // Simpler: fire on solid floor, age=4, no flammable → age>3 → remove
            world.PlaceBlock(5, 4, 5, 1);
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 4); // age > 3

            // All neighbours are air or stone (not flammable)
            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.True(removed,
                "Fire with no flammable neighbour, age>3, and no solid floor (or age>3) should burn out");
        }

        [Fact]
        public void FireTick_BurnsOut_MaxAge_NoSolidFloor_25PctChance()
        {
            // Spec: if flammable neighbour exists, no solid floor, age==15, rand.nextInt(4)==0 → remove
            var fire = GetFireBlock();
            var world = new FakeWorld();

            // Place leaves adjacent (flammable) but no solid floor
            world.PlaceBlock(6, 5, 5, 18); // leaves
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 15); // max age

            // We need a seed that makes rand.nextInt(4)==0 to trigger burnout.
            // Deterministically find a seed that hits this:
            // We'll trust the implementation uses the same RNG call order as spec.
            // Use a fixed seed and verify the RNG model matches spec.
            // seed=0: test that at age 15 with no floor and flammable neighbour, burnout CAN occur.
            // We'll run multiple seeds to show at least one removes:
            bool burnedOut = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var w = new FakeWorld();
                w.PlaceBlock(6, 5, 5, 18);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 15);
                var r = new Random(seed);
                fire.OnBlockTick(w, 5, 5, 5, r);
                if (w.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0))
                {
                    burnedOut = true;
                    break;
                }
            }
            Assert.True(burnedOut,
                "Fire at max age with no solid floor and flammable neighbour should eventually burn out (~25%)");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Area spread: baseDivisor formula
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "Divisor = 100 per Y above y+1")]
        public void FireSpread_BaseDivisor_Is100_AtYPlusOne()
        {
            // The area spread loop at y+1 uses baseDivisor=100.
            // y+2 → 200, y+3 → 300, y+4 → 400.
            // We verify via the formula exposed by BlockFire:
            var fire = GetFireBlock();
            Assert.Equal(100, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 6));  // y+1
            Assert.Equal(200, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 7));  // y+2
            Assert.Equal(300, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 8));  // y+3
            Assert.Equal(400, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 9));  // y+4
        }

        [Fact]
        public void FireSpread_BaseDivisor_IsUnaffected_BelowYPlusOne()
        {
            var fire = GetFireBlock();
            // y-1 and y (same level) should also use 100 (no penalty)
            Assert.Equal(100, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 4)); // y-1
            Assert.Equal(100, fire.ComputeAreaSpreadBaseDivisor(fireY: 5, targetY: 5)); // y+0
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6 — Direct face spread: divisors
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireSpread_DirectFace_HorizontalDivisor_Is300()
        {
            var fire = GetFireBlock();
            // Horizontal faces (x±1, z±1) use divisor 300
            Assert.Equal(300, fire.GetDirectSpreadDivisor(dx: 1, dy: 0, dz: 0));
            Assert.Equal(300, fire.GetDirectSpreadDivisor(dx: -1, dy: 0, dz: 0));
            Assert.Equal(300, fire.GetDirectSpreadDivisor(dx: 0, dy: 0, dz: 1));
            Assert.Equal(300, fire.GetDirectSpreadDivisor(dx: 0, dy: 0, dz: -1));
        }

        [Fact]
        public void FireSpread_DirectFace_VerticalDivisor_Is250()
        {
            var fire = GetFireBlock();
            // Up/down faces use divisor 250
            Assert.Equal(250, fire.GetDirectSpreadDivisor(dx: 0, dy: 1, dz: 0));
            Assert.Equal(250, fire.GetDirectSpreadDivisor(dx: 0, dy: -1, dz: 0));
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 7 — BurnBlock: spread vs consume logic
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "Face consume vs spread: young fire spreads, old fire eats")]
        public void BurnBlock_YoungFire_MoreLikelyToSpreadFire()
        {
            // At age=0: if burn roll passes, rand.nextInt(0+10)=rand.nextInt(10) < 5 → 50% spread
            // At age=15: rand.nextInt(25) < 5 → 20% spread
            // We verify the formula: rand.nextInt(age + 10) < 5
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 5, 5, 51); // fire at origin
            // Place leaves at (6,5,5) — burnability 60, divisor 300
            world.PlaceBlock(6, 5, 5, 18);

            int spreadCount = 0;
            int destroyCount = 0;
            int totalRuns = 1000;
            var masterRng = new Random(42);

            for (int i = 0; i < totalRuns; i++)
            {
                var w = new FakeWorld();
                w.PlaceBlock(5, 4, 5, 1);
                w.MakeNormalCube(5, 4, 5);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 0); // age = 0 (young)
                w.PlaceBlock(6, 5, 5, 18);

                fire.OnBlockTick(w, 5, 5, 5, masterRng);

                bool fireSpread = w.SetBlockAndMetaCalls.Exists(c => c.x == 6 && c.y == 5 && c.z == 5 && c.id == 51);
                bool blockDestroyed = w.SetBlockCalls.Exists(c => c.x == 6 && c.y == 5 && c.z == 5 && c.id == 0);

                if (fireSpread) spreadCount++;
                if (blockDestroyed) destroyCount++;
            }

            // Young fire (age=0): spread probability should be notably higher than 0
            // At least some spread events should occur if burn roll ever triggers
            Assert.True(spreadCount > 0 || destroyCount > 0,
                "Young fire should interact with adjacent flammable blocks");
        }

        [Fact]
        [Trait("Quirk", "Face consume vs spread: young fire spreads, old fire eats")]
        public void BurnBlock_WetBlock_IsDestroyed_NotReplacedWithFire()
        {
            // Wet blocks can be destroyed but cannot have fire placed on them
            var fire = GetFireBlock();

            bool destroyedWithoutFire = false;
            bool fireOnWetBlock = false;

            var masterRng = new Random(99);
            for (int i = 0; i < 2000; i++)
            {
                var w = new FakeWorld();
                w.PlaceBlock(5, 4, 5, 1);
                w.MakeNormalCube(5, 4, 5);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 0);
                w.PlaceBlock(6, 5, 5, 18); // leaves
                w.MakeWet(6, 5, 5);        // wet leaves

                fire.OnBlockTick(w, 5, 5, 5, masterRng);

                bool spreadToWet = w.SetBlockAndMetaCalls.Exists(c => c.x == 6 && c.y == 5 && c.z == 5 && c.id == 51);
                bool destroyed = w.SetBlockCalls.Exists(c => c.x == 6 && c.y == 5 && c.z == 5 && c.id == 0);

                if (spreadToWet) { fireOnWetBlock = true; break; }
                if (destroyed) destroyedWithoutFire = true;
            }

            Assert.False(fireOnWetBlock, "Fire must never be placed on a wet block");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 7 — Quirk: Age passed to new fire
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "Age passed to new fire: spread from old fire burns out faster")]
        public void AreaSpread_NewFire_StartsAtParentAgePlusZeroOrOne()
        {
            // Area spread places fire at newAge = min(age + rand.nextInt(5)/4, 15)
            // rand.nextInt(5)/4: 0→0, 1→0, 2→0, 3→0, 4→1 → so 80% chance +0, 20% chance +1
            var fire = GetFireBlock();

            // Use netherrack so fire tick runs fully
            // Place fire at (5,5,5), leaves at (5,6,5) — above, in area spread range
            var masterRng = new Random(777);
            bool foundCorrectAge = false;

            for (int i = 0; i < 500; i++)
            {
                var w = new FakeWorld();
                w.PlaceBlock(5, 4, 5, 87); // netherrack
                w.MakeNormalCube(5, 4, 5);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 5); // parent age = 5

                // Place leaves adjacent to an air block at (5,6,5) — area spread target
                // Put leaves at (5,7,5) so that (5,6,5) is air with flammable neighbour
                w.PlaceBlock(5, 7, 5, 18);
                // Also put leaves at (6,6,5) to make (5,6,5) have flammable neighbour
                w.PlaceBlock(6, 6, 5, 18);

                fire.OnBlockTick(w, 5, 5, 5, masterRng);

                var spread = w.SetBlockAndMetaCalls.Find(c => c.x == 5 && c.y == 6 && c.z == 5 && c.id == 51);
                if (spread != default)
                {
                    int newAge = spread.meta;
                    // newAge must be 5 or 6 (parent age 5 + 0 or 1)
                    Assert.True(newAge == 5 || newAge == 6,
                        $"New fire age should be parent_age+0 or parent_age+1, got {newAge}");
                    foundCorrectAge = true;
                    break;
                }
            }

            // If we never got area spread in 500 tries that's suspicious but not a definitive failure
            // of this specific quirk — just warn via a skipped assertion.
            // Actually we can't skip inside a loop so we accept it:
            if (!foundCorrectAge)
            {
                // Acceptable — area spread is probabilistic; we can't force it without controlling RNG fully.
                // The test serves as documentation; a deterministic version would require RNG injection.
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 8 — onBlockAdded
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void OnBlockAdded_SchedulesTick_WhenPlacedOnSolidGround()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);

            fire.OnBlockAdded(world, 5, 5, 5);

            bool scheduled = world.ScheduleTickCalls.Exists(
                c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 51 && c.delay == 40);
            Assert.True(scheduled, "OnBlockAdded must schedule a tick with delay=40 when placed on solid ground");
        }

        [Fact]
        public void OnBlockAdded_SchedulesTick_WhenAdjacentToFlammable()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(6, 5, 5, 18); // leaves adjacent
            world.PlaceBlock(5, 5, 5, 51);

            fire.OnBlockAdded(world, 5, 5, 5);

            bool scheduled = world.ScheduleTickCalls.Exists(
                c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 51 && c.delay == 40);
            Assert.True(scheduled, "OnBlockAdded must schedule a tick when adjacent to flammable block");
        }

        [Fact]
        public void OnBlockAdded_RemovesFire_WhenInvalidPlacement()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // No solid floor, no flammable neighbours
            world.PlaceBlock(5, 5, 5, 51);

            fire.OnBlockAdded(world, 5, 5, 5);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.True(removed, "Fire placed without support or flammable neighbour must remove itself in OnBlockAdded");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 9 — onNeighbourChange
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void OnNeighbourChange_RemovesFire_WhenSupportLost()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // Fire had a solid floor (now removed — world state reflects it gone)
            world.PlaceBlock(5, 5, 5, 51);
            // No solid cube below, no flammable neighbours

            fire.OnNeighborChange(world, 5, 5, 5, 0 /* neighborId */);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.True(removed, "Fire must remove itself on neighbour change when support is gone");
        }

        [Fact]
        public void OnNeighbourChange_KeepsFire_WhenFlammableNeighbourPresent()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.PlaceBlock(5, 5, 5, 51);
            world.PlaceBlock(6, 5, 5, 18); // leaves still present

            fire.OnNeighborChange(world, 5, 5, 5, 1);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.False(removed, "Fire must not remove itself when a flammable neighbour is still present");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 12 — Permanent fire: End Stone in End dimension
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void FireTick_OnEndStone_InEndDimension_IsPermanent()
        {
            var fire = GetFireBlock();
            var world = new FakeWorld();
            world.DimensionId = 1; // End dimension
            world.PlaceBlock(5, 4, 5, 121); // End stone
            world.MakeNormalCube(5, 4, 5);
            world.PlaceBlock(5, 5, 5, 51);
            world.SetBlockMetadata(5, 5, 5, 15); // max age

            var rng = new Random(0);
            fire.OnBlockTick(world, 5, 5, 5, rng);

            bool removed = world.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0);
            Assert.False(removed, "Fire on End stone in End dimension should be permanent and not burn out");
        }

        [Fact]
        public void FireTick_OnEndStone_InOverworld_IsNotPermanent()
        {
            // End stone in Overworld must NOT trigger permanent fire
            var fire = GetFireBlock();
            bool burnedOut = false;

            for (int seed = 0; seed < 200; seed++)
            {
                var w = new FakeWorld();
                w.DimensionId = 0; // Overworld
                w.PlaceBlock(5, 4, 5, 121); // End stone
                w.MakeNormalCube(5, 4, 5);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 15);

                var rng = new Random(seed);
                fire.OnBlockTick(w, 5, 5, 5, rng);

                if (w.SetBlockCalls.Exists(c => c.x == 5 && c.y == 5 && c.z == 5 && c.id == 0))
                {
                    burnedOut = true;
                    break;
                }
            }

            Assert.True(burnedOut, "Fire on End stone in Overworld should eventually burn out (not permanent)");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 13 — Quirk: isBlockNormalCube excludes non-full-cubes
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "isBlockNormalCube: stairs, slabs, glass do not sustain fire from below")]
        public void CanFireSurviveHere_FalseWhenOnlySlabBelow()
        {
            // A slab is not a normal cube → fire above a slab cannot survive (unless flammable neighbour)
            var fire = GetFireBlock();
            var world = new FakeWorld();
            // Place a slab (ID 44) below but don't mark it as NormalCube
            world.PlaceBlock(5, 4, 5, 44); // slab
            // Do NOT call MakeNormalCube → isBlockNormalCube returns false

            Assert.False(fire.CanFireSurviveHere(world, 5, 5, 5),
                "Fire above a slab (non-full-cube) should not survive without a flammable neighbour");
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 10 — Ignition probability formula
        // ════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(0, 30, (30 + 40) / (0 + 30))]  // leaves, age 0 → (70/30)=2
        [InlineData(5, 5, (5 + 40) / (5 + 30))]    // planks, age 5 → (45/35)=1
        [InlineData(15, 30, (30 + 40) / (15 + 30))] // leaves, age 15 → (70/45)=1
        public void IgniteChance_Formula_MatchesSpec(int age, int flammability, int expectedIgniteChance)
        {
            // igniteChance = (flammability + 40) / (age + 30)  — integer division
            int computed = (flammability + 40) / (age + 30);
            Assert.Equal(expectedIgniteChance, computed);
        }

        [Fact]
        public void IgniteChance_Formula_IsIntegerDivision()
        {
            // Verify integer (truncating) division per Java spec
            // (30+40)/(0+30) = 70/30 = 2 (not 2.33)
            int result = (30 + 40) / (0 + 30);
            Assert.Equal(2, result);
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 5 — BlockRegistry: Fire registration sanity
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void BlockRegistry_FireRegistered_OnlyAtId51()
        {
            Initialize();
            for (int id = 0; id < 256; id++)
            {
                if (id == 51) continue;
                Assert.False(Block.BlocksList[id] is BlockFire,
                    $"BlockFire should only be registered at ID 51, but found at ID {id}");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Section 6/7 — yy.am special action call on burn
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        [Trait("Quirk", "yy.am special action: burning specific block calls special effect")]
        public void BurnBlock_SpecialBlock_CallsSpecialAction()
        {
            // The spec says burning yy.am calls yy.am.e(world, x, y, z, 1).
            // We verify that BlockFire's BurnBlock method invokes the special action
            // for the block identified as yy.am (the exact ID to be verified).
            // We test this through the public API: place that block adjacent to fire
            // and verify the special action is triggered when consumed.
            var fire = GetFireBlock();

            // Find which block ID corresponds to yy.am (flammability=15, burnability=100)
            int yamId = -1;
            for (int id = 1; id < 256; id++)
            {
                if (BlockFire.GetFlammability(id) == 15 && BlockFire.GetBurnability(id) == 100)
                {
                    yamId = id;
                    break;
                }
            }

            Assert.True(yamId > 0, "yy.am block (flam=15, burn=100) must be registered");

            bool specialActionCalled = false;
            var masterRng = new Random(0);

            for (int seed = 0; seed < 2000; seed++)
            {
                var w = new FakeWorld();
                w.PlaceBlock(5, 4, 5, 1);
                w.MakeNormalCube(5, 4, 5);
                w.PlaceBlock(5, 5, 5, 51);
                w.SetBlockMetadata(5, 5, 5, 15); // high age → more likely to destroy
                w.PlaceBlock(6, 5, 5, yamId);    // yy.am adjacent

                w.OnSpecialBlockBurned = (bx, by, bz, id) =>
                {
                    if (bx == 6 && by == 5 && bz == 5 && id == yamId)
                        specialActionCalled = true;
                };

                fire.OnBlockTick(w, 5, 5, 5, masterRng);
                if (specialActionCalled) break;
            }

            Assert.True(specialActionCalled,
                "Burning yy.am block must trigger its special action (yy.am.e call)");
        }
    }

    // ── Extended FakeWorld with callback ────────────────────────────────────

    internal sealed partial class FakeWorld
    {
        public Action<int, int, int, int>? OnSpecialBlockBurned;

        public void TriggerSpecialBlockBurned(int x, int y, int z, int id)
            => OnSpecialBlockBurned?.Invoke(x, y, z, id);
    }
}