// Stub for net.minecraft.world.server.ServerWorld — Minecraft 1.16.5
// 1.16 moved ServerWorld into a sub-package.

using net.minecraft.util.math;

namespace net.minecraft.world.server;

/// <summary>
/// MinecraftStubs v1_16 — net.minecraft.world.server.ServerWorld.
/// Subclass of World (net.minecraft.world); lives in server sub-package.
/// </summary>
public class ServerWorld : net.minecraft.world.World
{
    public ServerWorld() { }
    public ServerWorld(SpectraEngine.Core.IWorld core) : base(core) { }

    public override string getJavaClassName() => "net.minecraft.world.server.ServerWorld";
}
