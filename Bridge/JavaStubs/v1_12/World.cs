// Stub for net.minecraft.world.World — Minecraft 1.12
// Java package: net.minecraft.world

using SpectraEngine.Core;
using net.minecraft.block;
using net.minecraft.util.math;

namespace net.minecraft.world;

/// <summary>
/// MinecraftStubs v1_12 — World.
///
/// Wraps SpectraEngine.Core.IWorld.
/// Mod code receives this via block callbacks (onBlockActivated, updateTick, etc.)
/// and entity tick methods.
///
/// CODER NOTE: add IWorld delegating methods as mod usage reveals what is needed.
/// </summary>
public class World
{
    internal readonly IWorld _core;

    public World(IWorld core) => _core = core;

    /// <summary>Returns the block state at the given position.</summary>
    public virtual net.minecraft.block.IBlockState getBlockState(BlockPos pos)
    {
        // CODER: delegate to IWorld.GetBlockId + IBlockRegistry.Get(id).GetDefaultState()
        return new net.minecraft.block.SimpleBlockState(new net.minecraft.block.Block());
    }

    /// <summary>Sets the block state at the given position.</summary>
    public virtual bool setBlockState(BlockPos pos, net.minecraft.block.IBlockState state, int flags)
    {
        // CODER: delegate to IWorld.SetBlock(pos.X, pos.Y, pos.Z, ...)
        return false;
    }

    /// <summary>Returns true if the block at pos is opaque.</summary>
    public virtual bool isBlockOpaqueCube(BlockPos pos) => false;

    /// <summary>Returns the light level at the given position (0–15).</summary>
    public virtual int getLightFor(EnumSkyBlock type, BlockPos pos)
        => _core.GetLightValue(pos.X, pos.Y, pos.Z, 0);

    /// <summary>Schedules a block tick update.</summary>
    public virtual void scheduleUpdate(BlockPos pos, net.minecraft.block.Block block, int delay) { }

    /// <summary>Notifies neighbours that the block at pos changed.</summary>
    public virtual void notifyNeighborsRespectDebug(BlockPos pos, net.minecraft.block.Block block, bool updateObservers) { }

    /// <summary>Returns true if this is the server-side world (not a remote client world).</summary>
    public virtual bool isRemote => false;

    public virtual string getJavaClassName() => "net.minecraft.world.World";
}

/// <summary>Enum for sky-block light type — used in getLightFor.</summary>
public enum EnumSkyBlock { SKY = 0, BLOCK = 1 }
