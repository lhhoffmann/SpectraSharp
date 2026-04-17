// Stubs for net.minecraftforge.event.* — Minecraft Forge 1.12
//
// NOTE: "event" is a C# reserved keyword; C# source uses the @event escape.
// At the IL level this compiles to "net.minecraftforge.event" which matches
// what IKVM generates from the Java package name — no runtime difference.

using net.minecraft.block;
using net.minecraft.entity.player;
using net.minecraft.item;
using net.minecraft.util.math;
using net.minecraft.world;

// ── @SubscribeEvent + EventPriority (net.minecraftforge.common) ───────────────

namespace net.minecraftforge.common
{
    /// <summary>
    /// Marks a method as a Forge event handler.
    /// Placed on instance or static methods that the ForgeEventBus should invoke.
    /// Priority controls dispatch order; receiveCanceled allows handling cancelled events.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SubscribeEventAttribute : Attribute
    {
        public EventPriority priority        { get; init; } = EventPriority.NORMAL;
        public bool          receiveCanceled { get; init; } = false;
    }

    /// <summary>Dispatch priority for Forge events — lower ordinal fires first.</summary>
    public enum EventPriority { HIGHEST = 0, HIGH = 1, NORMAL = 2, LOW = 3, LOWEST = 4 }
}

// ── BlockEvent (net.minecraftforge.event → @event in C# source) ──────────────

namespace net.minecraftforge.@event
{
    /// <summary>
    /// Base class for block-related Forge events.
    /// Fields match the Java API so mods that access them compile correctly.
    /// </summary>
    public abstract class BlockEvent : net.minecraftforge.common.ForgeEvent
    {
        public World       world { get; }
        public BlockPos    pos   { get; }
        public IBlockState state { get; }

        protected BlockEvent(World world, BlockPos pos, IBlockState state)
        {
            this.world = world;
            this.pos   = pos;
            this.state = state;
        }

        /// <summary>
        /// Fired when a player breaks a block.
        /// Cancellable — cancel to prevent the break.
        /// </summary>
        public sealed class BreakEvent : BlockEvent
        {
            public EntityPlayer player    { get; }
            public int          expToDrop { get; set; }

            public BreakEvent(World world, BlockPos pos, IBlockState state, EntityPlayer player)
                : base(world, pos, state)
            {
                this.player = player;
            }
        }

        /// <summary>Fired after a block is placed. Not cancellable.</summary>
        public sealed class PlaceEvent : BlockEvent
        {
            public EntityPlayer               player { get; }
            public net.minecraft.block.Block  block  { get; }

            public PlaceEvent(World world, BlockPos pos, IBlockState state,
                              EntityPlayer player, net.minecraft.block.Block block)
                : base(world, pos, state)
            {
                this.player = player;
                this.block  = block;
            }
        }
    }
}

// ── PlayerEvent (net.minecraftforge.event.entity.player) ──────────────────────

namespace net.minecraftforge.@event.entity.player
{
    /// <summary>Base class for player-related Forge events.</summary>
    public abstract class PlayerEvent : net.minecraftforge.common.ForgeEvent
    {
        public EntityPlayer player { get; }

        protected PlayerEvent(EntityPlayer player)
        {
            this.player = player;
        }

        /// <summary>Fired when a player crafts an item.</summary>
        public sealed class ItemCraftedEvent : PlayerEvent
        {
            public ItemStack crafting { get; }
            public ItemCraftedEvent(EntityPlayer player, ItemStack crafting) : base(player)
                => this.crafting = crafting;
        }

        /// <summary>Fired when a player smelts an item.</summary>
        public sealed class ItemSmeltedEvent : PlayerEvent
        {
            public ItemStack smelting { get; }
            public ItemSmeltedEvent(EntityPlayer player, ItemStack smelting) : base(player)
                => this.smelting = smelting;
        }
    }
}
