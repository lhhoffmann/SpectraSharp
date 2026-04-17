// Stub for net.minecraft.world.World — Minecraft 1.7.10
// x/y/z integer coordinate API (no BlockPos in 1.7).

using SpectraEngine.Core;
using net.minecraft.block;

namespace net.minecraft.world;

/// <summary>
/// MinecraftStubs v1_7_10 — World.
/// Wraps SpectraEngine.Core.IWorld.
/// All coordinates are int x, int y, int z — BlockPos does not exist in 1.7.
/// </summary>
public class World
{
    internal readonly IWorld _core;

    public World(IWorld core) => _core = core;

    // ── Block access ──────────────────────────────────────────────────────────

    public virtual int getBlockId(int x, int y, int z)
        => _core.GetBlockId(x, y, z);

    public virtual int getBlockMetadata(int x, int y, int z)
        => _core.GetBlockMetadata(x, y, z);

    public virtual bool setBlock(int x, int y, int z, int blockId, int meta, int flags)
    {
        _core.SetBlock(x, y, z, blockId);
        return true;
    }

    public virtual bool setBlock(int x, int y, int z, int blockId)
        => setBlock(x, y, z, blockId, 0, 3);

    public virtual bool setBlockWithNotify(int x, int y, int z, int blockId)
        => setBlock(x, y, z, blockId, 0, 3);

    public virtual bool setBlockMetadataWithNotify(int x, int y, int z, int meta, int flags)
        => true; // CODER: delegate to IWorld.SetBlockMeta

    public virtual int getLightValue(int x, int y, int z)
        => _core.GetLightValue(x, y, z, 0);

    public virtual bool isAirBlock(int x, int y, int z)
        => _core.GetBlockId(x, y, z) == 0;

    public virtual bool blockExists(int x, int y, int z)
        => y >= 0 && y < 256;

    // ── Entity spawning ───────────────────────────────────────────────────────

    public virtual bool spawnEntityInWorld(net.minecraft.entity.Entity entity)
    {
        // CODER: delegate to IWorld.SpawnEntity
        return false;
    }

    // ── World info ────────────────────────────────────────────────────────────

    public virtual bool isRemote => false;

    public net.minecraft.src.JavaRandom rand { get; } = new();

    public virtual string getJavaClassName() => "net.minecraft.world.World";
}
