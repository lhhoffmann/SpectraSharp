// Stub for net.minecraft.world.World — Minecraft 1.16.5
// BlockPos-based coordinate API; World / ServerWorld split.

using net.minecraft.block;
using net.minecraft.util;
using net.minecraft.util.math;

namespace net.minecraft.world;

/// <summary>
/// MinecraftStubs v1_16 — World.
/// Delegates to SpectraEngine.Core.IWorld (numeric-ID layer).
/// Registry-name → numeric-ID mapping is a CODER TODO.
/// </summary>
public class World : IWorld
{
    internal SpectraEngine.Core.IWorld? _core;

    public World() { }
    public World(SpectraEngine.Core.IWorld core) { _core = core; }

    // ── Block access ──────────────────────────────────────────────────────────

    public BlockState getBlockState(BlockPos pos)
        => new BlockState(null); // CODER: registry lookup not yet wired

    public void setBlockState(BlockPos pos, BlockState state)
    {
        // CODER: map state.Block registry name → numeric id
        _core?.SetBlock(pos.x, pos.y, pos.z, 0);
    }

    public bool setBlockState(BlockPos pos, BlockState state, int flags)
    {
        setBlockState(pos, state);
        return true;
    }

    // ── Lighting ──────────────────────────────────────────────────────────────

    public int getLightFor(EnumSkyBlock type, BlockPos pos)
        => _core?.GetLightValue(pos.x, pos.y, pos.z, 0) ?? 0;

    public int getLightValue(BlockPos pos)
        => getLightFor(EnumSkyBlock.BLOCK, pos);

    // ── Scheduling & notifications ────────────────────────────────────────────

    public void scheduleUpdate(BlockPos pos, net.minecraft.block.Block block, int delay)
    { /* CODER: delegate when block-id resolved */ }

    public void notifyNeighborsOfStateChange(BlockPos pos, net.minecraft.block.Block block) { }
    public void notifyNeighborsOfStateChange(BlockPos pos, net.minecraft.block.Block block, bool b) { }

    // ── Misc ──────────────────────────────────────────────────────────────────

    public bool isRemote => false;

    public net.minecraft.world.biome.Biome getBiome(BlockPos pos)
        => new net.minecraft.world.biome.Biome();

    public virtual string getJavaClassName() => "net.minecraft.world.World";
}

// ── Supporting types ──────────────────────────────────────────────────────────

/// <summary>BlockState — wraps a Block reference.</summary>
public sealed class BlockState(net.minecraft.block.Block? block)
{
    public net.minecraft.block.Block? Block => block;
    public net.minecraft.block.Block getBlock() => block ?? new net.minecraft.block.Block();
}

/// <summary>EnumSkyBlock — sky vs block light.</summary>
public enum EnumSkyBlock { SKY, BLOCK }
