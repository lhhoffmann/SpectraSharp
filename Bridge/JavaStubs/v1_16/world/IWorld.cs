// Stubs for net.minecraft.world.IWorld, IBlockReader — Minecraft 1.16.5
// These are read-only / write-capable world interfaces mods target.

using net.minecraft.block;
using net.minecraft.util.math;

namespace net.minecraft.world;

/// <summary>
/// MinecraftStubs v1_16 — IBlockReader.
/// Read-only block access interface.
/// </summary>
public interface IBlockReader
{
    BlockState getBlockState(BlockPos pos);
    int getLightValue(BlockPos pos);
}

/// <summary>
/// MinecraftStubs v1_16 — IWorld.
/// Write-capable world interface. World implements this.
/// </summary>
public interface IWorld : IBlockReader
{
    bool setBlockState(BlockPos pos, BlockState state, int flags);
    void scheduleUpdate(BlockPos pos, net.minecraft.block.Block block, int delay);
}
