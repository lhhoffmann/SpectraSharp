// Stub for obfuscated class sr (Item) — Minecraft 1.0

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_0 — Item (obf: sr).
/// </summary>
public class Item
{
    public static readonly ItemListProxy itemsList = new();

    public int itemID { get; set; }

    public virtual string getJavaClassName() => "net.minecraft.item.Item";
}

/// <summary>Proxy for Item.itemsList[] — same pattern as BlockListProxy.</summary>
public sealed class ItemListProxy
{
    public Item? this[int id]
    {
        get  => new Item { itemID = id };
        set
        {
            if (value == null) return;
            Console.WriteLine($"[JavaStubs] Item.itemsList[{value.itemID}] = {value.GetType().Name}");
        }
    }

    public int Length => 32000;
}
