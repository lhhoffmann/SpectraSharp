// Stub for obfuscated class dk (ItemStack) — Minecraft 1.0

using SpectraSharp.ModRuntime.AllocGuard;

namespace net.minecraft.item;

/// <summary>
/// MinecraftStubs v1_0 — ItemStack (obf: dk).
/// Routes through FramePool to eliminate per-call GC pressure.
/// </summary>
public sealed class ItemStack
{
    public int itemID    { get; set; }
    public int stackSize { get; set; }
    public int itemDamage { get; set; }

    // Called by mod code: new dk(itemId, count)
    // IKVM routes the Java constructor to this C# constructor.
    public ItemStack(int itemId, int count)
    {
        var pooled = FramePool.RentItemStack(itemId, count);
        // Copy pool values — the stub is the Java-visible object;
        // pool tracks the data slot.
        itemID    = pooled.ItemId;
        stackSize = pooled.Count;
    }

    public ItemStack(int itemId) : this(itemId, 1) { }

    /// <summary>Java: stack.getItemDamage()</summary>
    public int getItemDamage() => itemDamage;

    /// <summary>Java: stack.stackSize</summary>
    public int getCount() => stackSize;

    public string getJavaClassName() => "net.minecraft.item.ItemStack";
}
