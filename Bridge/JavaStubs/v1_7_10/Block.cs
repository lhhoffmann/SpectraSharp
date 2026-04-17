// Stub for net.minecraft.block.Block — Minecraft 1.7.10
// Numeric block IDs; x/y/z integer API; metadata (0–15).

using net.minecraft.world;
using SpectraEngine.Core.Mods;
using CoreWorld = SpectraEngine.Core.IWorld;
using CoreRng   = SpectraEngine.Core.JavaRandom;

namespace net.minecraft.block;

/// <summary>
/// MinecraftStubs v1_7_10 — Block.
/// 1.7.10 blocks are still registered by numeric ID (up to 4095).
/// Implements IModBlockBehavior so ModBlockBridge can delegate ticks into the stub.
/// </summary>
public class Block : IModBlockBehavior
{
    // ── Numeric ID ────────────────────────────────────────────────────────────

    public int blockID { get; set; }

    public static readonly BlockListProxy blocksList = new();

    // ── Display ───────────────────────────────────────────────────────────────

    protected string blockName = "";

    public Block setBlockName(string name) { blockName = name; return this; }
    public virtual string getLocalizedName()   => blockName;
    public virtual string getUnlocalizedName() => $"tile.{blockName}";

    // ── Hardness / resistance ─────────────────────────────────────────────────

    protected float blockHardness   = 1.0f;
    protected float blockResistance = 1.0f;

    public Block setHardness(float v)    { blockHardness   = v;   return this; }
    public Block setResistance(float v)  { blockResistance = v;   return this; }
    public Block setBlockUnbreakable()   { blockHardness   = -1f; return this; }

    // ── Light ─────────────────────────────────────────────────────────────────

    protected float lightValue   = 0f;
    protected int   lightOpacity = 255;

    public Block setLightValue(float v)  { lightValue   = v;   return this; }
    public Block setLightOpacity(int v)  { lightOpacity = v;   return this; }

    // ── Drop behaviour ────────────────────────────────────────────────────────

    public virtual int idDropped(int meta, net.minecraft.src.JavaRandom rand, int fortune) => blockID;
    public virtual int quantityDropped(net.minecraft.src.JavaRandom rand) => 1;

    // ── Interaction & tick overrides (mods override these) ────────────────────

    public virtual bool onBlockActivated(World world, int x, int y, int z,
        net.minecraft.entity.player.EntityPlayer player, int side,
        float hitX, float hitY, float hitZ)
        => false;

    public virtual void onBlockAdded    (World world, int x, int y, int z) { }
    public virtual void onBlockRemoved  (World world, int x, int y, int z) { }

    public virtual void updateTick(World world, int x, int y, int z,
        net.minecraft.src.JavaRandom rand) { }

    public virtual void onNeighborBlockChange(World world, int x, int y, int z,
        Block neighborBlock) { }

    // ── IModBlockBehavior ─────────────────────────────────────────────────────

    float IModBlockBehavior.Hardness      => blockHardness;
    float IModBlockBehavior.Resistance    => blockResistance;
    float IModBlockBehavior.LightFraction => lightValue;
    int   IModBlockBehavior.LightOpacity  => lightOpacity;

    void IModBlockBehavior.OnBlockTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => updateTick(new World(world), x, y, z, new net.minecraft.src.JavaRandom());

    void IModBlockBehavior.OnUpdateTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => updateTick(new World(world), x, y, z, new net.minecraft.src.JavaRandom());

    public virtual string getJavaClassName() => "net.minecraft.block.Block";
}

/// <summary>
/// Proxy for Block.blocksList[].
/// Writes → ModBlockBridge.TryCreate() registers the stub in Core.Block.BlocksList.
/// Core ticks then dispatch back into the stub's updateTick via IModBlockBehavior.
/// </summary>
public sealed class BlockListProxy
{
    public Block? this[int id]
    {
        get
        {
            var core = id >= 0 && id < SpectraEngine.Core.Block.BlocksList.Length
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
