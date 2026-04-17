// Stub for obfuscated class yy (Block) — Minecraft 1.0
// Maps to SpectraEngine.Core.BlockBase

using SpectraEngine.Core;
using SpectraEngine.Core.Mods;
using CoreWorld = SpectraEngine.Core.IWorld;
using CoreRng   = SpectraEngine.Core.JavaRandom;

namespace net.minecraft.block;

/// <summary>
/// MinecraftStubs v1_0 — Block (obf: yy).
/// Mod blocks extend this class and override updateTick / onBlockActivated etc.
/// Implements IModBlockBehavior so the Core's ModBlockBridge can delegate ticks.
/// </summary>
public class Block : IModBlockBehavior
{
    // ── Static registry (blocksList proxy) ───────────────────────────────────

    public static readonly BlockListProxy blocksList = new();

    // ── Instance fields ───────────────────────────────────────────────────────

    public int   blockID          { get; set; }
    public float blockHardness    { get; set; } = 1.0f;
    public float blockResistance  { get; set; } = 5.0f;
    public float lightValue       { get; set; } = 0f;   // 0-1 fraction
    public int   lightOpacity     { get; set; } = 255;

    public virtual string getJavaClassName() => "net.minecraft.block.Block";

    // ── Fluent builder helpers ────────────────────────────────────────────────

    public Block setHardness(float v)    { blockHardness   = v;   return this; }
    public Block setResistance(float v)  { blockResistance = v;   return this; }
    public Block setLightValue(float v)  { lightValue      = v;   return this; }
    public Block setLightOpacity(int v)  { lightOpacity    = v;   return this; }
    public Block setBlockUnbreakable()   { blockHardness   = -1f; return this; }

    // ── Tick & interaction overrides (mods override these) ───────────────────

    /// <summary>
    /// Java: updateTick(World world, int x, int y, int z, Random rand)
    /// Called on random ticks AND on scheduled ticks (same method in 1.0).
    /// </summary>
    public virtual void updateTick(
        net.minecraft.world.World world, int x, int y, int z,
        net.minecraft.src.JavaRandom rand) { }

    public virtual bool onBlockActivated(
        net.minecraft.world.World world, int x, int y, int z,
        net.minecraft.entity.player.EntityPlayer player) => false;

    public virtual void onBlockAdded(
        net.minecraft.world.World world, int x, int y, int z) { }

    public virtual void onBlockRemoved(
        net.minecraft.world.World world, int x, int y, int z) { }

    // ── IModBlockBehavior ─────────────────────────────────────────────────────

    float IModBlockBehavior.Hardness      => blockHardness;
    float IModBlockBehavior.Resistance    => blockResistance;
    float IModBlockBehavior.LightFraction => lightValue;
    int   IModBlockBehavior.LightOpacity  => lightOpacity;

    void IModBlockBehavior.OnBlockTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => updateTick(new net.minecraft.world.World(world), x, y, z,
                      new net.minecraft.src.JavaRandom());

    void IModBlockBehavior.OnUpdateTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => updateTick(new net.minecraft.world.World(world), x, y, z,
                      new net.minecraft.src.JavaRandom());

    // ── Standard vanilla block singletons ─────────────────────────────────────

    public static readonly Block stone      = new() { blockID = 1  };
    public static readonly Block grass      = new() { blockID = 2  };
    public static readonly Block dirt       = new() { blockID = 3  };
    public static readonly Block cobblestone= new() { blockID = 4  };
    public static readonly Block sand       = new() { blockID = 12 };
    public static readonly Block gravel     = new() { blockID = 13 };
    public static readonly Block goldOre    = new() { blockID = 14 };
    public static readonly Block ironOre    = new() { blockID = 15 };
    public static readonly Block coalOre    = new() { blockID = 16 };
}

/// <summary>
/// Proxy for Block.blocksList[].
/// Writes → creates a ModBlockBridge in Core.Block.BlocksList so the engine
///          sees the mod block and dispatches ticks correctly.
/// </summary>
public sealed class BlockListProxy
{
    public Block? this[int id]
    {
        get
        {
            // Mirror the Core's lookup when possible
            var core = SpectraEngine.Core.Block.BlocksList.Length > id
                ? SpectraEngine.Core.Block.BlocksList[id] : null;
            return core != null ? new Block { blockID = id } : null;
        }
        set
        {
            if (value is not IModBlockBehavior behavior) return;
            ModBlockBridge.TryCreate(value.blockID, behavior);
        }
    }

    public int Length => 4096;
}
