// Stub for net.minecraft.item.Item + ItemStack — Minecraft 1.7.10

using SpectraEngine.Core.Mods;
using SpectraEngine.ModRuntime.AllocGuard;

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_7_10 — Item. Numeric ID based.
/// Implements IModItemBehavior so ModItemBridge can register it in Core.Item.ItemsList.
/// </summary>
public class Item : IModItemBehavior
{
    public static readonly ItemListProxy itemsList = new();

    public int itemID       { get; set; }
    public int maxStackSize { get; set; } = 64;
    public int maxDamage    { get; set; } = 0;
    protected int iconIndex { get; set; } = 0;

    protected string itemName = "";
    public Item setItemName(string name) { itemName = name; return this; }
    public virtual string getUnlocalizedName() => $"item.{itemName}";

    public virtual string getJavaClassName() => "net.minecraft.item.Item";

    // ── IModItemBehavior ──────────────────────────────────────────────────────

    int  IModItemBehavior.ItemId       => itemID;
    int  IModItemBehavior.MaxStackSize => maxStackSize;
    int  IModItemBehavior.MaxDamage    => maxDamage;
    int  IModItemBehavior.IconIndex    => iconIndex;
}

/// <summary>Proxy for Item.itemsList[] — writes trigger ModItemBridge.TryCreate().</summary>
public sealed class ItemListProxy
{
    public Item? this[int id]
    {
        get  => null;
        set
        {
            if (value is not IModItemBehavior behavior) return;
            ModItemBridge.TryCreate(value.itemID, behavior);
        }
    }
    public int Length => 32000;
}

/// <summary>MinecraftStubs v1_7_10 — ItemStack.</summary>
public sealed class ItemStack
{
    public int  itemID    { get; set; }
    public int  stackSize { get; set; }
    public int  itemDamage { get; set; }

    public ItemStack(int itemId, int count = 1, int damage = 0)
    {
        var pooled  = FramePool.RentItemStack(itemId, count);
        itemID      = pooled.ItemId;
        stackSize   = pooled.Count;
        itemDamage  = damage;
    }

    public ItemStack(net.minecraft.block.Block block, int count = 1)
        : this(block.blockID, count) { }

    public ItemStack(Item item, int count = 1, int damage = 0)
        : this(item.itemID, count, damage) { }

    public int  getItemDamage() => itemDamage;
    public int  stackSize2      => stackSize;
    public bool isEmpty()       => itemID == 0 || stackSize <= 0;

    public string getJavaClassName() => "net.minecraft.item.ItemStack";
}
