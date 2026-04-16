// Stub for obfuscated class ry (World) — Minecraft 1.0
// Java package: net.minecraft.world (human-readable name used after remapping)
// Maps to SpectraEngine.Core.IWorld

using SpectraEngine.Core;
using SpectraEngine.ModRuntime.AllocGuard;
using SpectraEngine.ModRuntime.Sandbox;
using net.minecraft.src;

namespace net.minecraft.world;

/// <summary>
/// MinecraftStubs v1_0 — World (obf: ry).
///
/// Every public method here matches the Java API signature exactly as seen by
/// mod bytecode after name remapping. All calls delegate to <see cref="IWorld"/>.
///
/// CODER NOTE: If this stub calls an IWorld method that does not exist yet,
/// add it to Core/IWorld.cs and implement it in Core/World.cs.
/// </summary>
public class World
{
    // The real engine world — injected by ModRuntime at mod load time.
    // Internal so stubs in the same assembly can access it.
    internal readonly IWorld _core;

    public World(IWorld core) => _core = core;

    // ── Java identity (for getClass().getName() calls from mod code) ──────────

    public virtual string getJavaClassName() => "net.minecraft.world.World";

    // ── Read-only block access ────────────────────────────────────────────────

    /// <summary>obf: a(III)I — returns block ID at position. 0 = air.</summary>
    public virtual int getBlockId(int x, int y, int z)
    {
        if (!ThreadGuard.EnsureTickThread(() => { /* read — safe to discard */ }, nameof(getBlockId)))
            return 0;
        return _core.GetBlockId(x, y, z);
    }

    /// <summary>obf: b(III)I — returns block metadata (0–15).</summary>
    public virtual int getBlockMetadata(int x, int y, int z)
        => _core.GetBlockMetadata(x, y, z);

    /// <summary>obf: f(III)I — returns raw light value.</summary>
    public virtual int getBlockLightValue(int x, int y, int z)
        => _core.GetLightValue(x, y, z, 0);

    /// <summary>obf: — true if block at position is air (ID 0).</summary>
    public virtual bool isAirBlock(int x, int y, int z)
        => _core.GetBlockId(x, y, z) == 0;

    /// <summary>obf: — true if chunk can see sky at (x,z) down from y.</summary>
    public virtual bool canSeeSky(int x, int y, int z)
    {
        // TODO: delegate to IWorld.CanSeeSky once spec is written — REQUESTS.md
        for (int yy = 127; yy > y; yy--)
            if (_core.GetBlockId(x, yy, z) != 0) return false;
        return true;
    }

    /// <summary>obf: — true if the block is a full opaque solid cube.</summary>
    public virtual bool isBlockNormalCube(int x, int y, int z)
        => _core.IsOpaqueCube(x, y, z);

    // ── Block writes ──────────────────────────────────────────────────────────

    /// <summary>obf: d(IIII)Z — setBlock (no meta change).</summary>
    public virtual bool setBlock(int x, int y, int z, int blockId)
    {
        if (!ThreadGuard.EnsureTickThread(
                () => _core.SetBlock(x, y, z, blockId), nameof(setBlock)))
            return false;
        return _core.SetBlock(x, y, z, blockId);
    }

    /// <summary>obf: b(IIIII)Z — setBlock with notify.</summary>
    public virtual bool setBlockWithNotify(int x, int y, int z, int blockId)
        => setBlock(x, y, z, blockId); // 1.0 had no separate notify path in mods

    /// <summary>obf: — setBlockMetadata with notify.</summary>
    public virtual bool setBlockMetadataWithNotify(int x, int y, int z, int meta)
    {
        if (!ThreadGuard.EnsureTickThread(
                () => _core.SetMetadata(x, y, z, meta), nameof(setBlockMetadataWithNotify)))
            return false;
        return _core.SetMetadata(x, y, z, meta);
    }

    // ── Tick scheduling ───────────────────────────────────────────────────────

    /// <summary>obf: a(IIIII)V — scheduleBlockUpdate.</summary>
    public virtual void scheduleBlockUpdate(int x, int y, int z, int blockId, int delay)
        => _core.ScheduleBlockUpdate(x, y, z, blockId, delay);

    // ── Rendering / lighting ──────────────────────────────────────────────────

    /// <summary>obf: — marks a block range dirty for re-render.</summary>
    public virtual void markBlocksDirty(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        // Rendering notification — no Core equivalent yet.
        // TODO: delegate to IEngine.MarkDirty once renderer spec exists.
    }

    // ── Sound ─────────────────────────────────────────────────────────────────

    /// <summary>obf: — plays sound at entity position.</summary>
    public virtual void playSoundAtEntity(object entity, string soundName, float vol, float pitch)
    {
        // TODO: delegate to IAudio once audio spec exists — REQUESTS.md
    }

    /// <summary>obf: — plays sound effect at world position.</summary>
    public virtual void playSoundEffect(double x, double y, double z,
                                         string soundName, float vol, float pitch)
    {
        // TODO: delegate to IAudio
    }

    // ── World state ───────────────────────────────────────────────────────────

    /// <summary>obf: I — isRemote / isClientSide.</summary>
    public virtual bool isRemote => _core.IsClientSide;

    /// <summary>obf: f — world random (Java Random).</summary>
    public virtual java.util.Random rand =>
        JavaRandomAdapter.Wrap(_core.Random);
}
