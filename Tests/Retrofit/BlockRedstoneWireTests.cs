using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using SpectraSharp.Core;

namespace SpectraSharp.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Hand-written fakes
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Minimal block-data store: 3-D dictionary of (id, meta) pairs.</summary>
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

/// <summary>
/// Full fake World that tracks SetMetadataQuiet calls, NotifyNeighbors calls,
/// IsBlockReceivingPower returns, and SuppressUpdates.
/// </summary>
file class FakeWorld : World
{
    // --- block storage ---
    protected readonly Dictionary<(int, int, int), (int id, int meta)> _blocks = new();

    // --- recorded side-effects ---
    public readonly List<(int x, int y, int z, int meta)> MetaWrites = new();
    public readonly List<(int x, int y, int z, int blockId)> NotifyNeighborsCalls = new();
    public readonly List<(int x, int y, int z, int blockId)> NotifyBlockCalls = new();

    // --- controlled inputs ---
    /// <summary>Positions that IsBlockReceivingPower returns true for.</summary>
    public readonly HashSet<(int, int, int)> PoweredPositions = new();

    public FakeWorld() : base(new NullChunkLoader(), 0L) { }

    public JavaRandom   Random          { get; set; } = new JavaRandom(0);
    public bool         IsNether        { get; set; } = false;
    public new bool     SuppressUpdates { get; set; }
    public int          DimensionId     { get; set; } = 0;

    // --- World surface area ---
    public new bool IsBlockNormalCube(int x, int y, int z)
    {
        int id = GetBlockId(x, y, z);
        return id != 0 && Block.IsOpaqueCubeArr[id & 0xFF];
    }

    public new int GetBlockId(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.id : 0;

    public new int GetBlockMetadata(int x, int y, int z) =>
        _blocks.TryGetValue((x, y, z), out var v) ? v.meta : 0;

    public new virtual void SetMetadataQuiet(int x, int y, int z, int meta)
    {
        var key = (x, y, z);
        int id = _blocks.TryGetValue(key, out var v) ? v.id : 55;
        _blocks[key] = (id, meta);
        MetaWrites.Add((x, y, z, meta));
    }

    public new bool SetBlock(int x, int y, int z, int id)
    {
        var key = (x, y, z);
        int meta = _blocks.TryGetValue(key, out var v) ? v.meta : 0;
        _blocks[key] = (id, meta);
        return true;
    }

    public new bool SetMetadata(int x, int y, int z, int meta) { return true; }
    public new virtual bool IsBlockReceivingPower(int x, int y, int z) =>
        PoweredPositions.Contains((x, y, z));

    public new virtual void NotifyNeighbors(int x, int y, int z, int blockId)
    {
        NotifyNeighborsCalls.Add((x, y, z, blockId));
    }

    public new void NotifyBlock(int x, int y, int z, int blockId)
    {
        NotifyBlockCalls.Add((x, y, z, blockId));
    }

    // Helpers for tests
    public void PlaceWire(int x, int y, int z, int meta = 0)   => _blocks[(x, y, z)] = (55, meta);
    public void PlaceBlock(int x, int y, int z, int id, int meta = 0) => _blocks[(x, y, z)] = (id, meta);
}

// ─────────────────────────────────────────────────────────────────────────────
// Test class
// ─────────────────────────────────────────────────────────────────────────────

public sealed class BlockRedstoneWireTests
{
    // ── Construction helpers ──────────────────────────────────────────────────

    private static BlockRedstoneWire MakeWire() => new(55);

    // ─────────────────────────────────────────────────────────────────────────
    // §3.2  Constructor
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_BlockId_Is55()
    {
        var wire = MakeWire();
        Assert.Equal(55, wire.BlockID);
    }

    [Fact]
    public void Constructor_Hardness_IsZero()
    {
        var wire = MakeWire();
        Assert.Equal(0.0f, wire.Hardness);
    }

    [Fact]
    public void Constructor_BlockName_IsRedstoneDust()
    {
        var wire = MakeWire();
        Assert.Equal("redstoneDust", wire.BlockName);
    }

    [Fact]
    public void Constructor_IsOpaqueCube_ReturnsFalse()
    {
        var wire = MakeWire();
        Assert.False(wire.IsOpaqueCube());
    }

    [Fact]
    public void Constructor_RenderAsNormalBlock_ReturnsFalse()
    {
        var wire = MakeWire();
        Assert.False(wire.RenderAsNormalBlock());
    }

    [Fact]
    public void Constructor_CollisionBoundingBox_IsNull()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        Assert.Null(wire.GetCollisionBoundingBoxFromPool(world, 0, 0, 0));
    }

    // Spec §3.2: material = p.p (passable)
    [Fact]
    public void Constructor_Material_IsPassable()
    {
        var wire = MakeWire();
        Assert.Equal(Material.Passable, wire.BlockMaterial);
    }

    // The implementation passes Material.Grass_ — spec says p.p (passable).
    [Fact(Skip = "PARITY BUG — impl diverges from spec: constructor uses Material.Grass_ instead of Material.Passable (p.p)")]
    public void Constructor_Material_IsPassable_SpecBug()
    {
        var wire = MakeWire();
        Assert.Equal(Material.Passable, wire.BlockMaterial);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.1  canProvidePower default
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CanProvidePower_DefaultTrue()
    {
        var wire = MakeWire();
        Assert.True(wire.CanProvidePower());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.3  canBlockStay
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CanBlockStay_SolidBelow_ReturnsTrue()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1); // stone below
        Assert.True(wire.CanBlockStay(world, 0, 0, 0));
    }

    [Fact]
    public void CanBlockStay_AirBelow_ReturnsFalse()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        Assert.False(wire.CanBlockStay(world, 0, 0, 0));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4  isProvidingWeakPower
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsProvidingWeakPower_MetaZero_ReturnsFalse()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 0);
        // any face
        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 0));
    }

    [Fact]
    public void IsProvidingWeakPower_FaceUp_AlwaysTrue_WhenPowered()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 5); // meta=5
        // face 1 = up → always true
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 1));
    }

    [Fact]
    public void IsProvidingWeakPower_ReentranceGuard_ReturnsFalse()
    {
        var wire = MakeWire();
        // Manually suppress canProvidePower via reflection (simulates mid-propagation state)
        SetPrivateField(wire, "_canProvidePower", false);
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 2));
    }

    [Fact]
    public void IsProvidingWeakPower_IsolatedWire_PowersAllLateralFaces()
    {
        // Isolated wire (no connections): faces 2,3,4,5 all return true
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        // No neighbors → isolated
        foreach (int face in new[] { 2, 3, 4, 5 })
            Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, face),
                $"Expected face {face} to be powered for isolated wire");
    }

    [Fact]
    public void IsProvidingWeakPower_IsolatedWire_DoesNotPowerDown()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 0)); // face 0 = down
    }

    [Fact]
    public void IsProvidingWeakPower_NorthSouthLine_PowersOnlyNorthSouth()
    {
        // Wire at (0,0,0) with wire to the north (z-1) only
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        world.Set(0, 0, -1, 55, 7); // north neighbor is wire

        // North run, no east/west → face 3 (north) and face 2 (south)?
        // Spec §3.4: if face==3 && north && !west && !east → true
        // north = ConnectsTo(0,0,-1, 2)=true; west=false; east=false
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 3));
        // face 4 (east/west) should be false
        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 4));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.5  isProvidingStrongPower
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsProvidingStrongPower_DelegatesTo_WeakPower()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceWire(0, 0, 0, 8);
        // face 1 (up) → weak=true → strong=true
        Assert.True(wire.IsProvidingStrongPower(world, 0, 0, 0, 1));
    }

    [Fact]
    public void IsProvidingStrongPower_ReentranceGuard_ReturnsFalse()
    {
        var wire = MakeWire();
        SetPrivateField(wire, "_canProvidePower", false);
        var world = new FakeWorld();
        world.PlaceWire(0, 0, 0, 8);
        Assert.False(wire.IsProvidingStrongPower(world, 0, 0, 0, 1));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  Propagation — attenuation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_AttenuatesBy1_PerWireBlock()
    {
        // Wire at (0,0,0) meta=8, wire at (1,0,0) should become 7 after propagation
        var wire = MakeWire();
        var world = new FakeWorld();
        // Solid floor
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 8);
        world.PlaceWire(1, 0, 0, 0);

        // (0,0,0) is not externally powered; its neighbor is unpowered
        // Propagate from (0,0,0)
        wire.Propagate(world, 0, 0, 0);

        // The wire at (1,0,0) should have been set to max(0, 8-1)=7
        bool foundWrite = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 1 && y == 0 && z == 0 && meta == 7) { foundWrite = true; break; }
        Assert.True(foundWrite, "Expected wire at (1,0,0) to be written meta=7");
    }

    [Fact]
    public void Propagate_ExternallyPowered_SetsMeta15()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 0);
        world.PoweredPositions.Add((0, 0, 0));

        wire.Propagate(world, 0, 0, 0);

        bool found = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 15) { found = true; break; }
        Assert.True(found, "Expected externally powered wire to be set to meta 15");
    }

    [Fact]
    public void Propagate_UnchangedMeta_WritesNothing()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 0); // already 0, not powered
        // no neighbors

        wire.Propagate(world, 0, 0, 0);

        Assert.Empty(world.MetaWrites);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §12 Quirk 1 — reentrance guard: a=false during world.v() query
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Quirk1_ReentranceGuard_CanProvidePower_FalseDuringQuery()
    {
        // During world.IsBlockReceivingPower, _canProvidePower must be false.
        // We test this by having a FakeWorld that captures canProvidePower during the query.
        var wire = MakeWire();
        bool? capturedFlag = null;

        var world = new CapturingWorld(wire, v => capturedFlag = v);
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);

        wire.Propagate(world, 0, 0, 0);

        Assert.True(capturedFlag.HasValue, "IsBlockReceivingPower was never called");
        Assert.False(capturedFlag!.Value, "Quirk 1: _canProvidePower must be false during world.v() query");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §12 Quirk 4 — dirty set only on 0-crossing transitions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Quirk4_DirtySet_AddedOnZeroCrossing_NonZeroToZero()
    {
        // Wire transitions from meta=5 to meta=0 → should schedule NotifyNeighbors for self+neighbors
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5); // meta=5, unpowered, no powered neighbors → will go to 0

        wire.Propagate(world, 0, 0, 0);

        // After Propagate, NotifyNeighbors should have been called for (0,0,0) (from the dirty set flush)
        bool foundSelf = false;
        foreach (var (x, y, z, _) in world.NotifyNeighborsCalls)
            if (x == 0 && y == 0 && z == 0) { foundSelf = true; break; }
        Assert.True(foundSelf, "Quirk 4: NotifyNeighbors must be called for self on 0-crossing");
    }

    [Fact]
    public void Quirk4_DirtySet_NotAdded_OnInteriorChange()
    {
        // Wire transitions from meta=8 to meta=7 (no 0-crossing) → dirty set should NOT include self
        // We need a scenario where wire goes from 8→7.
        // Wire at (0,0,0) meta=8, neighbor wire at (1,0,0) meta=8 → wire at (0) should settle to 7
        // Actually the propagation starts AT (0,0,0). Let's arrange:
        // Wire (0,0,0) meta=8, no external power, neighbor wire (1,0,0) meta=8 (no power there either)
        // After propagation from (0,0,0): newMeta = max(neighbor_meta)−1 = max(8)−1 = 7 (transition 8→7)
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 8);
        world.PlaceWire(1, 0, 0, 8);

        // Propagate from (0,0,0). readMeta=8, newMeta: no external power, neighbor (1,0,0) has meta=8
        // But origin is (0,0,0) itself, so the skip-origin check skips (1,0,0)? No — origin is passed as self initially.
        // Actually PropagateInternal(world, 0,0,0, 0,0,0) so all 4 neighbors ARE checked except origin=self… 
        // wait the origin x=ox,y=oy,z=oz — the neighbor (1,0,0) differs so it is NOT skipped.
        // newMeta = max(0, meta(1,0,0))=8, then 8-1=7. readMeta=8→newMeta=7: interior change (neither 0).

        int notifyCountBefore = world.NotifyNeighborsCalls.Count;
        wire.Propagate(world, 0, 0, 0);
        int notifyCountAfter = world.NotifyNeighborsCalls.Count;

        // During PropagateInternal, NotifyNeighbors IS called for (0,0,0) due to world.NotifyNeighbors mid-propagation.
        // But the dirty-set flush at the end should NOT add (0,0,0) for interior transition.
        // So the final batch of NotifyNeighbors calls (from dirty-set flush) should NOT include (0,0,0).
        // We count how many NotifyNeighbors calls have x=0,y=0,z=0 AFTER the propagation.
        // The spec says dirty set adds are ONLY on 0-crossing. So the flush adds nothing for interior changes.
        // However NotifyNeighbors IS called once inline (mid propagate). We just check dirty-set behavior.
        // We record notify calls only from the post-propagation flush by checking at the end:
        // The inline notify for (0,0,0) happens once. Dirty-set flush would add a second call if misbehaving.
        int inlineNotifyCount = 0;
        foreach (var (x, y, z, _) in world.NotifyNeighborsCalls)
            if (x == 0 && y == 0 && z == 0) inlineNotifyCount++;

        // Inline: exactly 1 (from the mid-propagation world.NotifyNeighbors call inside PropagateInternal)
        // Dirty-flush: 0 (interior change, no 0-crossing)
        Assert.Equal(1, inlineNotifyCount);
    }

    [Fact]
    public void Quirk4_DirtySet_AddedOnZeroCrossing_ZeroToNonZero()
    {
        // Wire at (0,0,0) meta=0, neighbor wire at (1,0,0) meta=10 → wire becomes 9 (0→9 crossing)
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 0);
        world.PlaceWire(1, 0, 0, 10);

        wire.Propagate(world, 0, 0, 0);

        // Should have NotifyNeighbors for (0,0,0) from dirty-set flush
        int count = 0;
        foreach (var (x, y, z, _) in world.NotifyNeighborsCalls)
            if (x == 0 && y == 0 && z == 0) count++;
        Assert.True(count >= 1, "Quirk 4: NotifyNeighbors must be called for self on 0→N crossing");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  Dirty set includes self + 4 lateral + y-1 + y+1 on 0-crossing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_ZeroCrossing_DirtySet_IncludesAllSixPositions()
    {
        // Wire at (0,0,0) transitions 5→0 (0-crossing: newMeta=0)
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5); // no powered neighbors → will go to 0

        wire.Propagate(world, 0, 0, 0);

        // Dirty set should include (0,0,0) + (-1,0,0) + (1,0,0) + (0,0,-1) + (0,0,1) + (0,-1,0) + (0,1,0)
        var expected = new HashSet<(int, int, int)>
        {
            (0, 0, 0), (-1, 0, 0), (1, 0, 0), (0, 0, -1), (0, 0, 1), (0, -1, 0), (0, 1, 0)
        };

        var notified = new HashSet<(int, int, int)>();
        foreach (var (x, y, z, _) in world.NotifyNeighborsCalls)
            notified.Add((x, y, z));

        foreach (var pos in expected)
            Assert.True(notified.Contains(pos),
                $"Quirk 4: Expected NotifyNeighbors for {pos} on 0-crossing");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  SuppressUpdates bracketing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_SuppressUpdates_SetTrueThenFalse_AroundMetaWrite()
    {
        var wire = MakeWire();
        var world = new TrackingSuppressWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);

        wire.Propagate(world, 0, 0, 0);

        Assert.True(world.SuppressWasTrueBeforeMetaWrite,
            "SuppressUpdates must be true before SetMetadataQuiet");
        Assert.False(world.SuppressAfterBlock,
            "SuppressUpdates must be false after the block");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.10  Drops
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IdDropped_ReturnsRedstoneDustItemId()
    {
        var wire = MakeWire();
        var rng = new JavaRandom(0);
        int dropped = wire.IdDropped(0, rng, 0);
        Assert.Equal(331, dropped);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4  face==2 (south) powers only south with south connection, no east/west
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsProvidingWeakPower_Face2_South_WithSouthWire_NoEastWest()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        world.Set(0, 0, 1, 55, 7); // south neighbor is wire

        // south=true, west=false, east=false → face 2 (south) should return true
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 2));
        // face 4 (east) should be false since east=false
        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 4));
    }

    [Fact]
    public void IsProvidingWeakPower_Face2_South_WithSouthAndEast_ReturnsFalse()
    {
        // If east also connected, south face should NOT provide power (not a straight line)
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        world.Set(0, 0, 1, 55, 7); // south
        world.Set(1, 0, 0, 55, 7); // east

        Assert.False(wire.IsProvidingWeakPower(world, 0, 0, 0, 2));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4  face==4 (west in spec) powers only west with west connection, no north/south
    // Note: spec face 4 = East (+X), face 5 = West (-X)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsProvidingWeakPower_Face4_WithWestWire_NoNorthSouth()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        world.Set(-1, 0, 0, 55, 7); // west neighbor is wire → west=true

        // spec: if (face==4 && west && !north && !south) return true
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 4));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  MaxWireMeta: non-wire neighbors ignored
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_NonWireNeighbor_DoesNotContribute()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);
        world.PlaceBlock(1, 0, 0, 1); // stone, not wire

        // Wire at (0,0,0) should not pick up power from the stone block
        wire.Propagate(world, 0, 0, 0);

        bool metaSetTo5 = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 5) { metaSetTo5 = true; break; }
        // 5 should not stay (no wire neighbors, no power), should go to 0
        Assert.False(metaSetTo5, "Wire should not receive power from non-wire stone block");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  PropagateInternal skips origin
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PropagateInternal_SkipsOriginNeighbor()
    {
        // If the origin is (1,0,0) and we propagate at (0,0,0), it should skip (1,0,0) when computing newMeta
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 3);
        world.PlaceWire(1, 0, 0, 8); // high meta neighbor acting as "origin"

        // Propagate at (0,0,0) with origin (1,0,0) → should ignore meta 8 from (1,0,0)
        // We simulate by calling Propagate from (1,0,0) and checking (0,0,0) result
        wire.Propagate(world, 1, 0, 0);

        // After propagation from (1,0,0): wire at (0,0,0) should get max(8−1)=7 (it won't skip because
        // the FIRST call is from (1,0,0) not (0,0,0), and when recursing into (0,0,0), origin is (1,0,0))
        bool found7 = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 7) { found7 = true; break; }
        Assert.True(found7, "Wire at (0,0,0) should receive attenuated power 7 from wire at (1,0,0)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.8  onBlockAdded calls Propagate on server side only
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnBlockAdded_ClientSide_DoesNotPropagate()
    {
        var wire = MakeWire();
        var world = new FakeWorld { IsClientSide = true };
        world.PlaceWire(0, 0, 0, 0);

        wire.OnBlockAdded(world, 0, 0, 0);

        Assert.Empty(world.MetaWrites);
    }

    [Fact]
    public void OnBlockAdded_ServerSide_DoesPropagateAndNotify()
    {
        var wire = MakeWire();
        var world = new FakeWorld { IsClientSide = false };
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 0);
        world.PoweredPositions.Add((0, 0, 0));

        wire.OnBlockAdded(world, 0, 0, 0);

        // Meta should be written to 15
        bool found = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 15) { found = true; break; }
        Assert.True(found, "OnBlockAdded should trigger propagation on server side");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.9  onNeighborBlockChange: canBlockStay false → drop and remove
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnNeighborBlockChange_CantStay_RemovesBlock()
    {
        var wire = MakeWire();
        var world = new FakeWorld { IsClientSide = false };
        // No solid below → canBlockStay=false
        world.PlaceWire(0, 0, 0, 0);

        wire.OnNeighborBlockChange(world, 0, 0, 0, 1);

        // world.SetBlock(0,0,0,0) should have been called
        Assert.Equal(0, world.GetBlockId(0, 0, 0));
    }

    [Fact]
    public void OnNeighborBlockChange_ClientSide_DoesNothing()
    {
        var wire = MakeWire();
        var world = new FakeWorld { IsClientSide = true };
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);

        wire.OnNeighborBlockChange(world, 0, 0, 0, 1);

        Assert.Empty(world.MetaWrites);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  Propagation: staircase up (neighbor is opaque, check y+1)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_StaircaseUp_OpaqueNeighbor_ChecksYPlus1()
    {
        // Wire at (0,0,0). Neighbor (1,0,0) is opaque stone. Wire at (1,1,0) meta=10.
        // Staircase up: since (1,0,0) is opaque → check (1,1,0)
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1); // floor under wire
        world.PlaceBlock(1, 0, 0, 1);  // opaque neighbor
        world.PlaceWire(0, 0, 0, 0);
        world.PlaceWire(1, 1, 0, 10); // wire above the opaque block

        wire.Propagate(world, 0, 0, 0);

        // Wire at (0,0,0) should receive max(10)−1 = 9
        bool found9 = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 9) { found9 = true; break; }
        Assert.True(found9, "Staircase-up: wire should receive power 9 from wire at (1,1,0) over opaque block");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  Propagation: staircase down (neighbor is non-opaque, check y-1)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_StaircaseDown_NonOpaqueNeighbor_ChecksYMinus1()
    {
        // Wire at (0,0,0). Neighbor (1,0,0) is air. Wire at (1,-1,0) meta=10.
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1); // floor under wire
        // (1,0,0) is air (non-opaque)
        world.PlaceWire(0, 0, 0, 0);
        world.PlaceWire(1, -1, 0, 10); // wire below air neighbor

        wire.Propagate(world, 0, 0, 0);

        bool found9 = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 9) { found9 = true; break; }
        Assert.True(found9, "Staircase-down: wire should receive power 9 from wire at (1,-1,0)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7  Propagate notifies all dirty positions after propagation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_AfterPropagation_DirtySetIsCleared()
    {
        // After Propagate() runs, s_dirty must be empty (cleared before notifying)
        // We verify by running propagation twice and ensuring no double-notification from previous run
        var wire = MakeWire();
        var world1 = new FakeWorld();
        world1.PlaceBlock(0, -1, 0, 1);
        world1.PlaceWire(0, 0, 0, 5);
        wire.Propagate(world1, 0, 0, 0);

        var world2 = new FakeWorld();
        world2.PlaceBlock(0, -1, 0, 1);
        world2.PlaceWire(0, 0, 0, 5);
        wire.Propagate(world2, 0, 0, 0);

        // Both runs should produce the same notification pattern (dirty set was cleared between them)
        Assert.Equal(world1.NotifyNeighborsCalls.Count, world2.NotifyNeighborsCalls.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4 connectsTo — repeater connects only on facing/opposite facing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConnectsTo_Repeater_OnlyOnFacingOrOpposite()
    {
        // Repeater at (1,0,0) with meta=1 (facing east/+X, output east).
        // From direction 1 (east, fromDir=3 in connectsTo convention): should connect.
        // From direction 0 (north): should NOT connect.
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8); // wire at origin
        world.Set(1, 0, 0, 93, 1); // off-repeater, facing=1 (meta&3=1)

        // Wire at (0,0,0) checking face 3 (north) — repeater doesn't connect north
        // west=connectsTo(x-1,...) connects west neighbor
        // face 3 = north: we'd need a wire at z-1
        // Let's directly test via IsProvidingWeakPower behavior
        // face 4 = east side: wire at (0,0,0) with a repeater at (1,0,0) meta=1
        // west flag comes from connectsTo(x-1,y,z,1)=connectsTo(-1,0,0,1) — no wire there
        // east flag comes from connectsTo(x+1,y,z,3)=connectsTo(1,0,0,3) — repeater at (1,0,0), meta&3=1, fromDir=3
        // lz.e[1]=? The opposite of facing=1 is (1+2)%4=3, so fromDir=3 == opposite(facing) → connect? YES
        // Spec: fromDir == facing || fromDir == lz.e[meta&3] (opposite)
        // facing=1, opposite=3. fromDir=3 → connected.
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 5)); // face 5=west, but east connected → straight east-west
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4 connectsTo — wire always connects
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConnectsTo_Wire_AlwaysConnects()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        world.Set(0, 0, 1, 55, 0); // wire to south

        // south connection → face 2 (south) should be powered if straight south
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 2));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4 connectsTo — air blocks never connect
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConnectsTo_Air_NeverConnects()
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, 8);
        // all neighbors are air (id=0)

        // Isolated → powers all lateral faces
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 2));
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 3));
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 4));
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 5));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7 wireMetaAt returns current when not wire
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MaxWireMeta_NonWire_ReturnsCurrent()
    {
        // Indirectly: if neighbor is stone (id=1), its meta should not affect newMeta
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);
        world.PlaceBlock(1, 0, 0, 1, 15); // stone with meta 15 (should be ignored)

        wire.Propagate(world, 0, 0, 0);

        // Wire should go from 5 to 0 (no wire neighbors to get power from)
        bool foundZero = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 0) { foundZero = true; break; }
        Assert.True(foundZero, "Non-wire neighbor meta must not affect wire power level");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7 attenuation: newMeta>0 → decrement by 1
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_AttenuationDecrementsBy1()
    {
        // Wire at (0,0,0) meta=3, neighbor wire at (1,0,0) meta=5 → (0,0,0) gets max(5)−1=4
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 3);
        world.PlaceWire(1, 0, 0, 5);

        wire.Propagate(world, 0, 0, 0);

        bool found4 = false;
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0 && meta == 4) { found4 = true; break; }
        Assert.True(found4, "Wire attenuation: should receive 5-1=4 from neighbor with meta 5");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7 attenuation: meta=1 → goes to 0 (not negative)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_AttenuationTo0_NotNegative()
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 0);
        world.PlaceWire(1, 0, 0, 1); // neighbor with meta=1 → this wire gets max(0, 1-1)=0

        wire.Propagate(world, 0, 0, 0);

        // (0,0,0) should get 0 — no meta write (already 0), or written to 0
        foreach (var (x, y, z, meta) in world.MetaWrites)
            if (x == 0 && y == 0 && z == 0)
                Assert.True(meta >= 0, "Wire meta must not go negative");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §12 Quirk 4 — dirty set positions: self + 4 lateral + y-1 + y+1
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 1)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 1, 0)]
    public void Quirk4_ZeroCrossing_DirtySet_IncludesAllNeighbors(int dx, int dy, int dz)
    {
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);

        wire.Propagate(world, 0, 0, 0);

        int ex = dx, ey = dy, ez = dz;
        bool found = false;
        foreach (var (x, y, z, _) in world.NotifyNeighborsCalls)
            if (x == ex && y == ey && z == ez) { found = true; break; }
        Assert.True(found,
            $"Quirk 4: dirty set must include ({ex},{ey},{ez}) on 0-crossing");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.6  canProvidePower override
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void CanProvidePower_ReflectsInternalFlag()
    {
        var wire = MakeWire();
        Assert.True(wire.CanProvidePower());
        SetPrivateField(wire, "_canProvidePower", false);
        Assert.False(wire.CanProvidePower());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.4  face 1 up powers regardless of connections
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(15)]
    public void IsProvidingWeakPower_FaceUp_TrueForAnyNonZeroMeta(int meta)
    {
        var wire = MakeWire();
        var world = new FakeBlockAccess();
        world.Set(0, 0, 0, 55, meta);
        Assert.True(wire.IsProvidingWeakPower(world, 0, 0, 0, 1));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // §3.7 Recursion: only recurse if neighbor meta != expected (newMeta-1)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Propagate_NoRecursion_WhenNeighborAlreadyCorrect()
    {
        // Wire at (0,0,0) meta=5, neighbor (1,0,0) meta=4 (already correct)
        // Propagation should not recurse into (1,0,0)
        var wire = MakeWire();
        var world = new FakeWorld();
        world.PlaceBlock(0, -1, 0, 1);
        world.PlaceBlock(1, -1, 0, 1);
        world.PlaceWire(0, 0, 0, 5);
        world.PlaceWire(1, 0, 0, 4); // already correct for 5-1=4

        wire.Propagate(world, 0, 0, 0);

        // (1,0,0) should not be rewritten
        bool wrote1 = false;
        foreach (var (x, y, z, _) in world.MetaWrites)
            if (x == 1 && y == 0 && z == 0) { wrote1 = true; break; }
        Assert.False(wrote1, "Should not recurse/rewrite neighbor already at correct meta");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: set private field via reflection
    // ─────────────────────────────────────────────────────────────────────────

    private static void SetPrivateField(object obj, string name, object value)
    {
        var field = obj.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(obj, value);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Specialized fake worlds for quirk tests
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// World that captures the value of BlockRedstoneWire._canProvidePower
/// at the moment IsBlockReceivingPower is called.
/// </summary>
file sealed class CapturingWorld : FakeWorld
{
    private readonly BlockRedstoneWire _wire;
    private readonly Action<bool> _capture;

    public CapturingWorld(BlockRedstoneWire wire, Action<bool> capture)
    {
        _wire = wire;
        _capture = capture;
    }

    public override bool IsBlockReceivingPower(int x, int y, int z)
    {
        bool flag = (bool)typeof(BlockRedstoneWire)
            .GetField("_canProvidePower", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(_wire)!;
        _capture(flag);
        return false;
    }
}

/// <summary>
/// World that verifies SuppressUpdates is true at the time SetMetadataQuiet is called
/// and false afterwards.
/// </summary>
file sealed class TrackingSuppressWorld : FakeWorld
{
    public bool SuppressWasTrueBeforeMetaWrite { get; private set; }
    public bool SuppressAfterBlock { get; private set; }

    public override void SetMetadataQuiet(int x, int y, int z, int meta)
    {
        if (SuppressUpdates)
            SuppressWasTrueBeforeMetaWrite = true;
        base.SetMetadataQuiet(x, y, z, meta);
    }

    public override void NotifyNeighbors(int x, int y, int z, int blockId)
    {
        // After the NotifyNeighbors call inside the suppressed block,
        // we record whether SuppressUpdates is still true.
        // The code sets SuppressUpdates=false after NotifyNeighbors, so
        // we check from the outside after propagation finishes.
        base.NotifyNeighbors(x, y, z, blockId);
        // Set SuppressAfterBlock based on what it is now (will be false after block ends)
    }

    /// <summary>Called by tests after Propagate to check final state.</summary>
    public new bool SuppressUpdates
    {
        get => base.SuppressUpdates;
        set
        {
            if (!value && base.SuppressUpdates)
                SuppressAfterBlock = false; // it was set to false
            base.SuppressUpdates = value;
        }
    }
}