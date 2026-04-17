// Stub for net.minecraft.block.Block — Minecraft 1.16.5
// Uses Block.Properties builder instead of setter chain.

using SpectraEngine.Core.Mods;
using net.minecraft.entity;
using net.minecraft.entity.player;
using net.minecraft.item;
using net.minecraft.util;
using net.minecraft.util.math;
using net.minecraft.world;
using CoreWorld = SpectraEngine.Core.IWorld;
using CoreRng   = SpectraEngine.Core.JavaRandom;
using JavaRandom = net.minecraft.util.JavaRandom;
using ItemStack  = net.minecraft.item.ItemStack;

namespace net.minecraft.block;

/// <summary>
/// MinecraftStubs v1_16 — Block.
/// Mod blocks extend this class, take Block.Properties in the constructor,
/// and register via DeferredRegister or ForgeRegistries.
/// Implements IModBlockBehavior so ModBlockBridge can register it in Core.
/// </summary>
public class Block : IModBlockBehavior
{
    internal ResourceLocation? _registryName;

    // Scalar properties read from Properties at construction time.
    protected float _hardness   = 1.0f;
    protected float _resistance = 1.0f;
    protected int   _lightValue = 0;
    protected int   _lightOpacity = 255;

    public Block(Properties props)
    {
        _hardness   = props._hardness;
        _resistance = props._resistance;
    }
    public Block() { }

    public Block setRegistryName(string name)
        { _registryName = new ResourceLocation(name); return this; }
    public Block setRegistryName(ResourceLocation r)
        { _registryName = r; return this; }
    public Block setRegistryName(string ns, string path)
        { _registryName = new ResourceLocation(ns, path); return this; }
    public ResourceLocation? getRegistryName() => _registryName;

    public virtual string getTranslationKey() => $"block.{_registryName?.Path ?? "unknown"}";

    // ── Interaction overrides (1.16 method names) ─────────────────────────────

    /// <summary>Right-click handler (1.16 renamed onBlockActivated → use).</summary>
    public virtual net.minecraft.util.ActionResultType use(
        BlockState state, World world, BlockPos pos,
        PlayerEntity player, Hand hand,
        net.minecraft.util.BlockRayTraceResult hit)
        => net.minecraft.util.ActionResultType.PASS;

    /// <summary>Called on placement.</summary>
    public virtual void setPlacedBy(
        World world, BlockPos pos, BlockState state,
        LivingEntity placer, ItemStack stack) { }

    /// <summary>Random tick callback.</summary>
    public virtual void randomTick(
        BlockState state, net.minecraft.world.server.ServerWorld world,
        BlockPos pos, JavaRandom rand) { }

    /// <summary>Scheduled tick callback.</summary>
    public virtual void tick(
        BlockState state, net.minecraft.world.server.ServerWorld world,
        BlockPos pos, JavaRandom rand) { }

    /// <summary>Neighbour block changed.</summary>
    public virtual BlockState updateShape(
        BlockState state, Direction facing,
        BlockState facingState, net.minecraft.world.IWorld world,
        BlockPos pos, BlockPos facingPos)
        => state;

    /// <summary>Neighbour notification (Forge compat name).</summary>
    public virtual void neighborChanged(
        BlockState state, World world, BlockPos pos,
        Block block, BlockPos fromPos, bool isMoving) { }

    public virtual BlockState getDefaultState() => new BlockState(this);

    public virtual string getJavaClassName() => "net.minecraft.block.Block";

    // ── IModBlockBehavior ─────────────────────────────────────────────────────

    float IModBlockBehavior.Hardness      => _hardness;
    float IModBlockBehavior.Resistance    => _resistance;
    float IModBlockBehavior.LightFraction => _lightValue / 15f;
    int   IModBlockBehavior.LightOpacity  => _lightOpacity;

    void IModBlockBehavior.OnBlockTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => randomTick(getDefaultState(), new net.minecraft.world.server.ServerWorld(world),
                      new BlockPos(x, y, z), new JavaRandom());

    void IModBlockBehavior.OnUpdateTick(CoreWorld world, int x, int y, int z, CoreRng rng)
        => tick(getDefaultState(), new net.minecraft.world.server.ServerWorld(world),
                new BlockPos(x, y, z), new JavaRandom());

    // ── Properties builder ────────────────────────────────────────────────────

    public class Properties
    {
        internal float _hardness = 1f, _resistance = 1f;

        protected Properties() { }

        public static Properties create(Material material) => new();
        public static Properties of(Material material)     => new();
        public static Properties from(Block block)         => new();

        public Properties hardnessAndResistance(float h, float r) { _hardness = h; _resistance = r; return this; }
        public Properties hardnessAndResistance(float v)          => hardnessAndResistance(v, v);
        public Properties zeroHardnessAndResistance()             => hardnessAndResistance(0f, 0f);
        public Properties strength(float h, float r)              => hardnessAndResistance(h, r);
        public Properties strength(float v)                       => hardnessAndResistance(v, v);
        public Properties setRequiresTool()                       => this;
        public Properties requiresCorrectToolForDrops()           => this;
        public Properties noDrops()                               => this;
        public Properties lightLevel(int v)                       => this;
        public Properties notSolid()                              => this;
        public Properties doesNotBlockMovement()                  => this;
        public Properties noOcclusion()                           => this;
        public Properties tickRandomly()                          => this;
        public Properties variableOpacity()                       => this;
        public Properties slipperiness(float v)                   => this;
        public Properties speedFactor(float v)                    => this;
        public Properties jumpFactor(float v)                     => this;
        public Properties sound(SoundType sound)                  => this;
        public Properties air()                                   => this;
        public Properties dynamicShape()                          => this;
        public Properties isSuffocating(System.Func<BlockState, net.minecraft.world.IBlockReader, BlockPos, bool> fn) => this;
    }
}

/// <summary>AbstractBlock — introduced in 1.16 as Block's base.</summary>
public class AbstractBlock
{
    public class Properties : Block.Properties
    {
        new public static Properties create(Material material) => new();
    }
}

/// <summary>Stub material enum — common materials only.</summary>
public sealed class Material
{
    public static readonly Material AIR     = new("air");
    public static readonly Material ROCK    = new("rock");
    public static readonly Material IRON    = new("iron");
    public static readonly Material ORGANIC = new("organic");
    public static readonly Material EARTH   = new("earth");
    public static readonly Material WOOD    = new("wood");
    public static readonly Material WATER   = new("water");
    public static readonly Material LAVA    = new("lava");
    public static readonly Material LEAVES  = new("leaves");
    public static readonly Material PLANTS  = new("plants");
    public static readonly Material SAND    = new("sand");
    public static readonly Material SNOW    = new("snow");
    public static readonly Material GLASS   = new("glass");
    public static readonly Material CLAY    = new("clay");
    public static readonly Material WOOL    = new("cloth");
    public static readonly Material FIRE    = new("fire");
    public static readonly Material NETHER  = new("portal");

    readonly string _name;
    Material(string n) => _name = n;
    public override string ToString() => _name;
}

/// <summary>Sound type stub.</summary>
public sealed class SoundType
{
    public static readonly SoundType STONE  = new();
    public static readonly SoundType WOOD   = new();
    public static readonly SoundType SAND   = new();
    public static readonly SoundType GRASS  = new();
    public static readonly SoundType METAL  = new();
    public static readonly SoundType GLASS  = new();
    public static readonly SoundType LADDER = new();
}

/// <summary>IBlockState interface (Forge compat name).</summary>
public interface IBlockState
{
    Block getBlock();
}

public sealed class SimpleBlockState(Block block) : IBlockState
{
    public Block getBlock() => block;
}
