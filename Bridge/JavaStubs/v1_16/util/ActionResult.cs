// Stubs for net.minecraft.util.ActionResultType, BlockRayTraceResult — Minecraft 1.16.5

using net.minecraft.util.math;

namespace net.minecraft.util;

/// <summary>
/// MinecraftStubs v1_16 — ActionResultType.
/// Return value for block / item interaction handlers.
/// </summary>
public enum ActionResultType
{
    SUCCESS,
    CONSUME,
    PASS,
    FAIL,
}

/// <summary>
/// MinecraftStubs v1_16 — BlockRayTraceResult.
/// Hit information passed to Block.use(). Stub with face and position.
/// </summary>
public sealed class BlockRayTraceResult
{
    public Direction     getDirection()       => Direction.UP;
    public BlockPos      getBlockPos()        => new BlockPos(0, 0, 0);
    public bool          isInside()           => false;
    public double        getLocation()        => 0.0;
}
