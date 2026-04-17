// Stub for net.minecraft.item.Item and net.minecraft.item.ItemStack — Minecraft 1.12

using SpectraEngine.Core.Mods;
using SpectraEngine.ModRuntime.AllocGuard;
using net.minecraft.util;

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_12 — Item.
/// In 1.12 items are registered by ResourceLocation, not numeric ID.
/// Implements IModItemBehavior so ModItemBridge can register it in Core.Item.ItemsList.
/// </summary>
public class Item : IModItemBehavior
{
    internal ResourceLocation? _registryName;

    public Item setRegistryName(string name)       { _registryName = new ResourceLocation(name); return this; }
    public Item setRegistryName(ResourceLocation r){ _registryName = r;                           return this; }
    public Item setRegistryName(string domain, string path) { _registryName = new ResourceLocation(domain, path); return this; }
    public ResourceLocation? getRegistryName() => _registryName;

    /// <summary>
    /// Legacy numeric ID — always 0 in 1.12 (registry names are primary).
    /// Kept so that FramePool routing and GameRegistry.ResolveId() compile without change.
    /// </summary>
    internal int itemID = 0;

    internal string _unlocalizedName = "";
    public Item setUnlocalizedName(string name) { _unlocalizedName = name; return this; }
    public virtual string getUnlocalizedName()  => $"item.{_unlocalizedName}";

    public virtual string getJavaClassName() => "net.minecraft.item.Item";

    // ── IModItemBehavior ──────────────────────────────────────────────────────

    int IModItemBehavior.ItemId       => itemID;
    int IModItemBehavior.MaxStackSize => 64;
    int IModItemBehavior.MaxDamage    => 0;
    int IModItemBehavior.IconIndex    => 0;
}

/// <summary>
/// ItemBlock — wraps a Block so it can appear in the inventory.
/// Required for every block that should be placeable by the player.
/// </summary>
public class ItemBlock(net.minecraft.block.Block block) : Item
{
    public net.minecraft.block.Block Block => block;

    public override string getJavaClassName() => "net.minecraft.item.ItemBlock";
}

/// <summary>
/// MinecraftStubs v1_12 — ItemStack.
/// Routes through FramePool to eliminate per-call GC pressure.
///
/// In 1.12 ItemStack tracks the Item by reference (not numeric ID).
/// The itemID field is kept for backward compatibility but item is primary.
/// </summary>
public sealed class ItemStack
{
    public Item?  item      { get; set; }
    public int    itemID    { get; set; }   // legacy compat — 1.12 uses Item reference
    public int    stackSize { get; set; }
    public int    itemDamage { get; set; }

    public ItemStack(Item item, int count = 1, int damage = 0)
    {
        var pooled    = FramePool.RentItemStack(item?.itemID ?? 0, count);
        this.item      = item;
        this.itemID    = pooled.ItemId;
        this.stackSize = pooled.Count;
        this.itemDamage = damage;
    }

    public ItemStack(net.minecraft.block.Block block, int count = 1)
        : this(new ItemBlock(block), count) { }

    public ItemStack(int itemId, int count = 1)
    {
        var pooled    = FramePool.RentItemStack(itemId, count);
        this.itemID    = pooled.ItemId;
        this.stackSize = pooled.Count;
    }

    public Item?  getItem()          => item;
    public int    getCount()         => stackSize;
    public int    getItemDamage()    => itemDamage;
    public bool   isEmpty()          => item == null && itemID == 0 || stackSize <= 0;

    public string getJavaClassName() => "net.minecraft.item.ItemStack";

    /// <summary>The empty ItemStack constant (1.12 replaced null with EMPTY).</summary>
    public static readonly ItemStack EMPTY = new(0, 0);
}
