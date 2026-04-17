// Stub for net.minecraft.block.Block — Minecraft 1.12
// Java package: net.minecraft.block

using SpectraEngine.Core.Mods;
using net.minecraft.entity;
using net.minecraft.entity.player;
using net.minecraft.util;
using net.minecraft.util.math;
using net.minecraft.world;
using CoreWorld = SpectraEngine.Core.IWorld;
using CoreRng   = SpectraEngine.Core.JavaRandom;
using JavaRandom = net.minecraft.util.JavaRandom;

namespace net.minecraft.block;

/// <summary>
/// MinecraftStubs v1_12 — Block.
///
/// In 1.12, blocks are registered by <see cref="ResourceLocation"/> (registry name)
/// not by numeric ID. The ID still exists for chunk storage but is internal.
///
/// Mod blocks extend this class and override:
///   - <c>getUnlocalizedName()</c>     — translation key
///   - <c>getItemDropped(...)</c>       — what item drops
///   - <c>quantityDropped(...)</c>      — how many items drop
///   - <c>onBlockActivated(...)</c>     — right-click handler
///   - <c>onBlockPlaced(...)</c>        — on placement
///
/// CODER NOTE: When IBlockRegistry supports registry names, update
/// <see cref="setRegistryName"/> to delegate to it.
/// </summary>
public class Block : IModBlockBehavior
{
    // ── Registry name (set by mod, read by GameRegistry) ─────────────────────

    internal ResourceLocation? _registryName;

    /// <summary>Sets the registry name — fluent, returns this.</summary>
    public Block setRegistryName(string name)
    {
        _registryName = new ResourceLocation(name);
        return this;
    }

    public Block setRegistryName(ResourceLocation name)
    {
        _registryName = name;
        return this;
    }

    public Block setRegistryName(string domain, string path)
    {
        _registryName = new ResourceLocation(domain, path);
        return this;
    }

    public ResourceLocation? getRegistryName() => _registryName;

    // ── Display / translation ─────────────────────────────────────────────────

    internal string _unlocalizedName = "";

    public Block setUnlocalizedName(string name) { _unlocalizedName = name; return this; }
    public virtual string getUnlocalizedName() => $"tile.{_unlocalizedName}";
    public virtual string getLocalizedName()   => _unlocalizedName;

    // ── Hardness / resistance ─────────────────────────────────────────────────

    protected float blockHardness   = 1.0f;
    protected float blockResistance = 1.0f;

    public Block setHardness(float hardness)     { blockHardness   = hardness;    return this; }
    public Block setResistance(float resistance) { blockResistance = resistance;  return this; }
    public Block setBlockUnbreakable()           { blockHardness   = -1f;         return this; }

    // ── Light ─────────────────────────────────────────────────────────────────

    protected int lightValue   = 0;
    protected int lightOpacity = 255;

    public Block setLightLevel(float value)   { lightValue   = (int)(value * 15f); return this; }
    public Block setLightOpacity(int opacity) { lightOpacity = opacity;            return this; }

    // ── Creative tab / material ───────────────────────────────────────────────

    public virtual string getCreativeTabToString() => "buildingBlocks";

    // ── Drop behaviour ────────────────────────────────────────────────────────

    /// <summary>Which item drops when this block is broken. Return null for nothing.</summary>
    public virtual net.minecraft.item.Item? getItemDropped(IBlockState state, JavaRandom rand, int fortune) => null;

    /// <summary>How many of the dropped item to give.</summary>
    public virtual int quantityDropped(JavaRandom rand) => 1;

    // ── Interaction ───────────────────────────────────────────────────────────

    /// <summary>Right-click handler. Return true if the action was handled.</summary>
    public virtual bool onBlockActivated(
        World world, BlockPos pos, IBlockState state,
        EntityPlayer player, EnumHand hand,
        EnumFacing facing, float hitX, float hitY, float hitZ)
        => false;

    /// <summary>Called when the block is placed in the world.</summary>
    public virtual void onBlockPlacedBy(
        World world, BlockPos pos, IBlockState state,
        EntityLivingBase placer, net.minecraft.item.ItemStack stack) { }

    /// <summary>Called every game tick for blocks that have random ticks.</summary>
    public virtual void randomTick(
        World world, BlockPos pos, IBlockState state, JavaRandom rand) { }

    /// <summary>Called on a scheduled tick.</summary>
    public virtual void updateTick(
        World world, BlockPos pos, IBlockState state, JavaRandom rand) { }

    /// <summary>Called when a neighbouring block changes.</summary>
    public virtual void neighborChanged(
        IBlockState state, World world, BlockPos pos,
        Block block, BlockPos fromPos) { }

    // ── Block state ───────────────────────────────────────────────────────────

    public virtual IBlockState getDefaultState() => new SimpleBlockState(this);

    // ── IModBlockBehavior ─────────────────────────────────────────────────────

    float IModBlockBehavior.Hardness      => blockHardness;
    float IModBlockBehavior.Resistance    => blockResistance;
    float IModBlockBehavior.LightFraction => lightValue / 15f;
    int   IModBlockBehavior.LightOpacity  => lightOpacity;

    void IModBlockBehavior.OnBlockTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => randomTick(new World(world), new BlockPos(x, y, z), getDefaultState(), new JavaRandom());

    void IModBlockBehavior.OnUpdateTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => updateTick(new World(world), new BlockPos(x, y, z), getDefaultState(), new JavaRandom());

    // ── Java identity ─────────────────────────────────────────────────────────

    public virtual string getJavaClassName() => "net.minecraft.block.Block";
}

/// <summary>
/// Minimal IBlockState stub for 1.12 mods that inspect block state.
/// Full property system is not yet implemented — extend when needed.
/// </summary>
public interface IBlockState
{
    Block getBlock();
    int   getValue();
}

public sealed class SimpleBlockState(Block block) : IBlockState
{
    public Block getBlock()  => block;
    public int   getValue()  => 0;
}

/// <summary>Enum for which hand the player is using.</summary>
public enum EnumHand { MAIN_HAND = 0, OFF_HAND = 1 }
