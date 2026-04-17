// Stub for net.minecraft.item.Item + ItemStack — Minecraft 1.16.5

using SpectraEngine.Core.Mods;
using SpectraEngine.ModRuntime.AllocGuard;
using net.minecraft.util;

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_16 — Item.
/// Uses Item.Properties builder; registered via DeferredRegister or ForgeRegistries.
/// Implements IModItemBehavior so ModItemBridge can register it in Core.Item.ItemsList.
/// </summary>
public class Item : IModItemBehavior
{
    internal ResourceLocation? _registryName;
    internal int itemID = 0; // legacy compat for FramePool

    public Item(Properties props) { }
    public Item() { }

    public Item setRegistryName(string name)         { _registryName = new ResourceLocation(name);     return this; }
    public Item setRegistryName(ResourceLocation r)  { _registryName = r;                              return this; }
    public Item setRegistryName(string ns, string p) { _registryName = new ResourceLocation(ns, p);    return this; }
    public ResourceLocation? getRegistryName() => _registryName;

    public virtual string getTranslationKey() => $"item.{_registryName?.Path ?? "unknown"}";
    public virtual string getJavaClassName()  => "net.minecraft.item.Item";

    // ── IModItemBehavior ──────────────────────────────────────────────────────

    int IModItemBehavior.ItemId       => itemID;
    int IModItemBehavior.MaxStackSize => 64;
    int IModItemBehavior.MaxDamage    => 0;
    int IModItemBehavior.IconIndex    => 0;

    // ── Properties builder ────────────────────────────────────────────────────

    public sealed class Properties
    {
        public Properties food(FoodProperties food)      => this;
        public Properties maxStackSize(int size)         => this;
        public Properties maxDamage(int damage)          => this;
        public Properties containerItem(Item item)       => this;
        public Properties group(net.minecraft.item.ItemGroup tab) => this;
        public Properties rarity(Rarity rarity)          => this;
        public Properties fireResistant()                => this;
        public Properties setNoRepair()                  => this;
        public Properties tab(net.minecraft.item.ItemGroup tab) => this;
    }
}

/// <summary>ItemBlock — wraps a Block so it appears in the inventory.</summary>
public class BlockItem(net.minecraft.block.Block block, Item.Properties props) : Item(props)
{
    public net.minecraft.block.Block Block => block;
    public override string getJavaClassName() => "net.minecraft.item.BlockItem";
}

/// <summary>MinecraftStubs v1_16 — ItemStack.</summary>
public sealed class ItemStack
{
    public Item?  item      { get; set; }
    public int    itemID    { get; set; }
    public int    stackSize { get; set; }
    public int    itemDamage { get; set; }

    public ItemStack(Item item, int count = 1)
    {
        var pooled  = FramePool.RentItemStack(item?.itemID ?? 0, count);
        this.item   = item;
        itemID      = pooled.ItemId;
        stackSize   = pooled.Count;
    }

    public ItemStack(net.minecraft.block.Block block, int count = 1)
        : this(new BlockItem(block, new Item.Properties()), count) { }

    public ItemStack(int itemId, int count = 1) { var p = FramePool.RentItemStack(itemId, count); itemID = p.ItemId; stackSize = p.Count; }

    public Item?  getItem()      => item;
    public int    getCount()     => stackSize;
    public int    getDamage()    => itemDamage;
    public bool   isEmpty()      => (item == null && itemID == 0) || stackSize <= 0;

    public static readonly ItemStack EMPTY = new(0, 0);
    public string getJavaClassName() => "net.minecraft.item.ItemStack";
}

// ── Supporting types ──────────────────────────────────────────────────────────

public sealed class FoodProperties { }
public sealed class ItemGroup(string name)
{
    public string Name { get; } = name;
    public static readonly ItemGroup MISC = new("misc");
}
public enum Rarity { COMMON, UNCOMMON, RARE, EPIC }
