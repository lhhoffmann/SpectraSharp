namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>acy</c> (Item) — base class for all non-block items.
/// Holds the static registry array, per-item metadata, and virtual use/hit/place methods.
///
/// Registry layout (spec §2):
///   <c>ItemsList[0–255]</c>  — block items (ItemBlock); populated by ItemBlock constructors.
///   <c>ItemsList[256–31999]</c> — pure item instances; stored at <c>ItemsList[256 + id]</c>.
///   ItemStack.ItemId is always the FULL registry index (direct index into ItemsList).
///
/// Quirks preserved (see spec §11):
///   1. Registry conflict only prints a warning — no exception; last registration wins.
///   2. GetMaxDamage() returns 0 unconditionally in the base class (not field-backed).
///      GetInternalDurabilityValue() returns the private <c>a</c> field (separate path).
///   3. SetCraftingResult throws if MaxStackSize > 1 at call time, not at registration.
///   4. GetIconIndex(metadata) ignores metadata in the base class — always returns IconIndex.
///   5. Records use IDs 2000–2010 → stored at ItemsList[2256]–ItemsList[2266].
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Item_Spec.md
/// </summary>
public class Item
{
    // ── Static registry (spec §2) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>d[32000]</c> — full item registry.
    /// Indexed by registry key. Use <see cref="RegistryIndex"/> to look up an item.
    /// </summary>
    public static readonly Item?[] ItemsList = new Item?[32000];

    /// <summary>
    /// Shared random used by item logic. obf: static <c>c</c>.
    /// </summary>
    protected static readonly JavaRandom SharedRandom = new JavaRandom();

    // ── Instance fields (spec §3) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>bM</c> — registry index = 256 + itemId. ItemId = RegistryIndex − 256.
    /// </summary>
    public readonly int RegistryIndex; // obf: bM

    /// <summary>obf: <c>bN</c> — maximum stack size. Default 64.</summary>
    protected int MaxStackSize = 64; // obf: bN

    /// <summary>
    /// obf: private <c>a</c> — internal durability value. When > 0 and !SuppressDamage,
    /// IsDamageable returns true. Set by builder <see cref="SetInternalDurability"/>.
    /// </summary>
    private int _internalDurabilityValue; // obf: a

    /// <summary>obf: <c>bO</c> — icon atlas index. Packed as col + row × 16.</summary>
    protected int IconIndex; // obf: bO

    /// <summary>obf: <c>bP</c> — true if item has subtypes (per-damage icon/behaviour).</summary>
    protected bool HasSubtypes; // obf: bP

    /// <summary>
    /// obf: <c>bQ</c> — suppresses damageability even when <c>a > 0</c>.
    /// When true, IsDamageable returns false.
    /// </summary>
    protected bool SuppressDamage; // obf: bQ

    /// <summary>obf: <c>b</c> — crafting remainder (e.g. empty bucket).</summary>
    private Item? _craftingResult; // obf: b

    /// <summary>obf: <c>bR</c> — auxiliary string (fuel key, potion ingredient, etc.).</summary>
    private string? _auxiliaryString; // obf: bR

    /// <summary>obf: <c>bS</c> — unlocalized name (e.g. "item.arrow").</summary>
    private string? _unlocalizedName; // obf: bS

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    /// <summary>
    /// Spec: <c>protected acy(int id)</c>.
    /// Stores this at <c>ItemsList[256 + id]</c>. Conflict is non-fatal (quirk 1).
    /// </summary>
    protected Item(int id)
    {
        RegistryIndex = 256 + id;
        if (ItemsList[RegistryIndex] != null)
            Console.WriteLine($"[Item] CONFLICT: registry slot {RegistryIndex} (id={id}) already occupied");
        ItemsList[RegistryIndex] = this;
    }

    // ── Builder methods (spec §5, all return this) ────────────────────────────

    /// <summary>
    /// obf: <c>a(int col, int row)</c> — sets IconIndex = col + row × 16.
    /// Despite the Java parameter names (row, col), col is the atlas X and row is Y.
    /// </summary>
    public Item SetIcon(int col, int row) { IconIndex = col + row * 16; return this; }

    /// <summary>obf: <c>g(int val)</c> — direct icon index setter (alternative to SetIcon).</summary>
    public Item SetIconIndex(int val) { IconIndex = val; return this; }

    /// <summary>obf: <c>h(int n)</c> — setMaxStackSize.</summary>
    public Item SetMaxStackSize(int n) { MaxStackSize = n; return this; }

    /// <summary>obf: <c>i()</c> (no-arg) — marks item as having subtypes.</summary>
    public Item MarkHasSubtypes() { HasSubtypes = true; return this; }

    /// <summary>
    /// obf: <c>protected i(int n)</c> — sets internal durability value.
    /// When > 0 and !SuppressDamage, IsDamageable returns true.
    /// </summary>
    protected Item SetInternalDurability(int n) { _internalDurabilityValue = n; return this; }

    /// <summary>obf: <c>protected a(boolean flag)</c> — sets SuppressDamage flag.</summary>
    protected Item SetSuppressDamage(bool flag) { SuppressDamage = flag; return this; }

    /// <summary>obf: <c>a(String name)</c> — sets unlocalized name to "item." + name.</summary>
    public Item SetUnlocalizedName(string name) { _unlocalizedName = "item." + name; return this; }

    /// <summary>
    /// obf: <c>a(acy item)</c> — sets crafting remainder.
    /// Throws if MaxStackSize > 1 at call time (quirk 3).
    /// </summary>
    public Item SetCraftingResult(Item item)
    {
        if (MaxStackSize > 1)
            throw new InvalidOperationException("Cannot set crafting result on a stackable item (MaxStackSize > 1)");
        _craftingResult = item;
        return this;
    }

    /// <summary>obf: <c>b(String s)</c> — sets auxiliary string (fuel key, potion marker, etc.).</summary>
    public Item SetAuxiliaryString(string s) { _auxiliaryString = s; return this; }

    // ── Getters (spec §6) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>e()</c> — getMaxStackSize → bN.</summary>
    public int GetMaxStackSize() => MaxStackSize;

    /// <summary>
    /// obf: <c>c()</c> — getMaxDamage. Base always returns 0 (quirk 2).
    /// Tool/weapon subclasses override to return durability.
    /// </summary>
    public virtual int GetMaxDamage() => 0;

    /// <summary>obf: <c>g()</c> — returns internal durability value (private <c>a</c> field). Quirk 2.</summary>
    public int GetInternalDurabilityValue() => _internalDurabilityValue;

    /// <summary>obf: <c>h()</c> — isDamageable = _internalDurabilityValue > 0 &amp;&amp; !SuppressDamage.</summary>
    public bool IsDamageable() => _internalDurabilityValue > 0 && !SuppressDamage;

    /// <summary>obf: <c>f()</c> — returns SuppressDamage flag.</summary>
    public bool GetSuppressDamage() => SuppressDamage;

    /// <summary>obf: <c>a()</c> — hasSubtypes.</summary>
    public bool GetHasSubtypes() => HasSubtypes;

    /// <summary>
    /// obf: <c>a(int metadata)</c> — getIconIndex. Base ignores metadata (quirk 4).
    /// </summary>
    public virtual int GetIconIndex(int metadata) => IconIndex;

    /// <summary>obf: <c>b(int)</c> — getItemEnchantability. Base returns 0 (not enchantable).</summary>
    public virtual int GetItemEnchantability() => 0;

    /// <summary>obf: <c>j()</c> — getCraftingResult (the remainder item, e.g. empty bucket).</summary>
    public Item? GetCraftingResult() => _craftingResult;

    /// <summary>obf: <c>k()</c> — hasCraftingResult.</summary>
    public bool HasCraftingResult() => _craftingResult != null;

    /// <summary>obf: <c>d()</c> — getUnlocalizedName (e.g. "item.arrow").</summary>
    public string? GetUnlocalizedName() => _unlocalizedName;

    /// <summary>obf: <c>m()</c> — getAuxiliaryString.</summary>
    public string? GetAuxiliaryString() => _auxiliaryString;

    /// <summary>obf: <c>n()</c> — hasAuxiliaryString.</summary>
    public bool HasAuxiliaryString() => _auxiliaryString != null;

    /// <summary>
    /// obf: <c>c(int meta)</c> — getColorFromItemStack. Base returns 0xFFFFFF (white).
    /// </summary>
    public virtual int GetColorFromItemStack(int meta) => 0xFFFFFF;

    // ── Virtual use / interaction methods (spec §7) — base all no-op / default ─

    /// <summary>
    /// obf: <c>a(dk stack, vi player, ry world, int x,y,z, int face)</c> — onItemUse.
    /// Called on right-click on a block. Base returns false.
    /// </summary>
    public virtual bool OnItemUse(ItemStack stack, object player, World world, int x, int y, int z, int face)
        => false;

    /// <summary>obf: <c>a(dk, yy)</c> — getMiningSpeed. Base returns 1.0F.</summary>
    public virtual float GetMiningSpeed(ItemStack stack, Block block) => 1.0f;

    /// <summary>obf: <c>a(dk, ry, vi)</c> — onItemRightClick. Base returns stack unchanged.</summary>
    public virtual ItemStack OnItemRightClick(ItemStack stack, World world, object player) => stack;

    /// <summary>obf: <c>c(dk, ry, vi)</c> — finishUsingItem. Base returns stack unchanged.</summary>
    public virtual ItemStack FinishUsingItem(ItemStack stack, World world, object player) => stack;

    /// <summary>obf: <c>b(dk)</c> — getMaxItemUseDuration. Base returns 0.</summary>
    public virtual int GetMaxItemUseDuration(ItemStack stack) => 0;

    /// <summary>obf: <c>b(dk, ry, vi)</c> — onUpdate. No-op base.</summary>
    public virtual void OnUpdate(ItemStack stack, World world, object player) { }

    /// <summary>obf: <c>a(dk, nq, nq)</c> — hitEntity. Returns false base.</summary>
    public virtual bool HitEntity(ItemStack stack, object target, object attacker) => false;

    /// <summary>obf: <c>a(yy)</c> — canHarvestBlock. Returns false base.</summary>
    public virtual bool CanHarvestBlock(Block block) => false;

    /// <summary>obf: <c>a(ia)</c> — itemInteractionForEntity. Returns 1 base.</summary>
    public virtual int ItemInteractionForEntity(object entity) => 1;

    // ── toString ──────────────────────────────────────────────────────────────

    public override string ToString()
        => $"Item{{id={RegistryIndex - 256}, regIdx={RegistryIndex}, name={_unlocalizedName}}}";
}
