namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>dk</c> (ItemStack) — universal item-slot container pairing an item type,
/// quantity, and damage/metadata value.
///
/// Field naming quirk (spec §2 / quirk 1):
///   <c>c</c> = itemId, <c>a</c> = stackSize, <c>e</c> = itemDamage — non-alphabetical order.
///
/// Quirks preserved (see spec §6):
///   1. Field names are non-intuitive: <c>c</c>=itemId, <c>a</c>=stackSize, <c>e</c>=itemDamage.
///   2. <c>h()</c> and <c>i()</c> both return <c>e</c> — two identical getters, different names.
///   3. <c>DamageItem</c> returns true if item broke but does NOT zero the stack.
///   4. <c>SplitStack</c> mutates the source (<c>this.StackSize -= n</c>).
///   5. Copy constructor deep-copies NBT; <c>Copy()</c> also deep-copies — two paths.
///
/// Open stubs (specs pending):
///   - Item registry (<c>acy.d[]</c>): GetItem(), GetMaxStackSize(), GetMaxDamage() are stubs.
///   - NBT (<c>ik</c>): WriteToNBT / ReadFromNBT are stubs.
///   - EnchantmentHelper: DamageItem Unbreaking check is skipped.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemStack_Spec.md
/// </summary>
public sealed class ItemStack
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    public  int     ItemId;      // obf: c
    public  int     StackSize;   // obf: a
    private int     _itemDamage; // obf: e (private)
    public  int     UseTimer;    // obf: b
#pragma warning disable CS0649 // NBT field intentionally unused (ik spec pending)
    private object? _nbtTag;     // obf: d — NBTTagCompound (ik spec pending)
#pragma warning restore CS0649

    // ── Constructors (spec §3) ────────────────────────────────────────────────

    /// <summary>Primary constructor: item ID, count, and damage.</summary>
    public ItemStack(int itemId, int count, int damage)
    {
        ItemId      = itemId;
        StackSize   = count;
        _itemDamage = damage;
    }

    /// <summary>No-damage shorthand. Spec: <c>dk(int itemId, int count)</c>.</summary>
    public ItemStack(int itemId, int count) : this(itemId, count, 0) { }

    /// <summary>Single-item shorthand. Spec: <c>dk(int itemId)</c>.</summary>
    public ItemStack(int itemId) : this(itemId, 1, 0) { }

    /// <summary>
    /// Copy constructor. Deep-copies NBT tag (quirk 5). Spec: <c>dk(dk source)</c>.
    /// </summary>
    public ItemStack(ItemStack source)
    {
        ItemId      = source.ItemId;
        StackSize   = source.StackSize;
        _itemDamage = source._itemDamage;
        // _nbtTag  = source._nbtTag?.Copy() — stub (ik spec pending)
    }

    // ── Item access (spec §4) ─────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a()</c> — returns Item from static registry <c>acy.d[c]</c>.
    /// Stub: Item (<c>acy</c>) spec pending. Returns null.
    /// </summary>
    public object? GetItem() => null; // TODO: return Item.ItemsList[ItemId]

    // ── Damage / metadata (spec §4) ───────────────────────────────────────────

    /// <summary>obf: <c>h()</c> — returns itemDamage (quirk 2: same as <see cref="GetMetadata"/>).</summary>
    public int GetItemDamage() => _itemDamage;

    /// <summary>obf: <c>i()</c> — returns itemDamage. Identical to <see cref="GetItemDamage"/> (quirk 2).</summary>
    public int GetMetadata() => _itemDamage;

    // ── Stack size / durability stubs (spec §4) ───────────────────────────────

    /// <summary>
    /// obf: <c>f()</c> → <c>min(item.maxStackSize, 64)</c>.
    /// Stub: returns 64 until Item spec is available.
    /// </summary>
    public int GetMaxStackSize() => 64; // TODO: Math.Min(item.GetMaxStackSize(), 64)

    /// <summary>
    /// obf: <c>j()</c> → <c>item.maxDamage</c>.
    /// Stub: returns 0 (undamageable) until Item spec is available.
    /// </summary>
    public int GetMaxDamage() => 0; // TODO: item.GetMaxDamage()

    // ── Damage (spec §4 / quirk 3) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int damage, nq entity)</c> — applies durability damage.
    /// Returns <c>true</c> if item broke; caller must destroy the stack (quirk 3).
    /// Unbreaking enchantment check skipped (<c>ik</c> / EnchantmentHelper spec pending).
    /// </summary>
    public bool DamageItem(int damage)
    {
        if (GetMaxDamage() == 0) return false; // undamageable
        _itemDamage += damage;
        return _itemDamage > GetMaxDamage();
    }

    // ── Split / copy (spec §4) ────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int count)</c> — splits <paramref name="count"/> items off this stack.
    /// Mutates <see cref="StackSize"/> of the original (quirk 4).
    /// Returns the split-off portion.
    /// </summary>
    public ItemStack SplitStack(int count)
    {
        int n     = Math.Min(count, StackSize);
        var split = Copy();
        split.StackSize = n;
        StackSize      -= n;
        return split;
    }

    /// <summary>obf: <c>b()</c> — deep copy. Also deep-copies NBT (quirk 5).</summary>
    public ItemStack Copy() => new ItemStack(this);

    // ── Enchantment helpers (spec §4) — stubs (ik spec pending) ──────────────

    /// <summary>obf: <c>n()</c> / <c>u()</c> — true if NBT tag contains enchantments. Stub: false.</summary>
    public bool HasEnchantments() => false;

    /// <summary>obf: <c>o()</c> — returns the NBT root tag. Stub: null.</summary>
    public object? GetTagCompound() => _nbtTag;

    // ── toString ──────────────────────────────────────────────────────────────

    public override string ToString()
        => $"ItemStack{{id={ItemId}, count={StackSize}, dmg={_itemDamage}}}";
}
