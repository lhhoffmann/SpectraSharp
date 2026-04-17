// Stub for obfuscated class sr (Item) — Minecraft 1.0

using SpectraEngine.Core.Mods;

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_0 — Item (obf: sr).
/// Implements IModItemBehavior so ModItemBridge can register it in Core.Item.ItemsList.
/// </summary>
public class Item : IModItemBehavior
{
    public static readonly ItemListProxy itemsList = new();

    public int itemID     { get; set; }
    public int maxStackSize { get; set; } = 64;
    public int maxDamage    { get; set; } = 0;
    protected int iconIndex  { get; set; } = 0;

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
        get => new Item { itemID = id };
        set
        {
            if (value is not IModItemBehavior behavior) return;
            ModItemBridge.TryCreate(value.itemID, behavior);
        }
    }

    public int Length => 32000;
}
