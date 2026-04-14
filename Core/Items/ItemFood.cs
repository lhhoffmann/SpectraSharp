namespace SpectraSharp.Core.Items;

/// <summary>
/// Replica of <c>agu</c> (ItemFood) — base class for all edible items.
/// Extends <see cref="Item"/>. Provides eat-animation duration, hunger restoration,
/// saturation gain, wolf-food flag, optional always-edible flag, and on-eat potion effect.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemFood_Spec.md §2–3
/// </summary>
public class ItemFood : Item
{
    // ── Instance fields (spec §2) ────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — eat animation duration in ticks. Always 32 (1.6 s at 20 Hz).</summary>
    public readonly int EatDuration = 32;

    /// <summary>obf: <c>b</c> — hunger half-hearts restored (1 unit = 0.5 hunger icon).</summary>
    private readonly int _healAmount;

    /// <summary>obf: <c>bR</c> — saturation modifier. Saturation gain = heal × satMod × 2.</summary>
    private readonly float _saturationModifier;

    /// <summary>obf: <c>bS</c> — whether tamed wolves can eat this item.</summary>
    public readonly bool IsWolfFood;

    /// <summary>obf: <c>bT</c> — if true, item can be eaten even when hunger is full.</summary>
    private bool _alwaysEdible;

    // Potion effect on eat (all zero = no effect).
    private int   _potionId;        // obf: bU
    private int   _potionDuration;  // obf: bV — seconds (×20 for ticks)
    private int   _potionAmplifier; // obf: bW — 0 = level I
    private float _potionChance;    // obf: bX — 0.0–1.0

    // ── Constructors (spec §2) ───────────────────────────────────────────────

    /// <summary>
    /// 4-argument primary constructor.
    /// <paramref name="id"/> is the item ID (RegistryIndex = id + 256).
    /// </summary>
    public ItemFood(int id, int healAmount, float saturationModifier, bool isWolfFood)
        : base(id)
    {
        _healAmount          = healAmount;
        _saturationModifier  = saturationModifier;
        IsWolfFood           = isWolfFood;
        MaxStackSize         = 64;
    }

    /// <summary>
    /// 3-argument convenience constructor — delegates with saturationModifier = 0.6F.
    /// </summary>
    public ItemFood(int id, int healAmount, bool isWolfFood)
        : this(id, healAmount, 0.6f, isWolfFood) { }

    // ── Builder methods (spec §2) ─────────────────────────────────────────────

    /// <summary>obf: <c>r()</c> — marks this item as always edible (usable at full hunger).</summary>
    public ItemFood SetAlwaysEdible() { _alwaysEdible = true; return this; }

    /// <summary>
    /// obf: <c>a(int potId, int durSec, int amp, float chance)</c> — attaches an on-eat potion effect.
    /// </summary>
    public ItemFood SetOnEatPotion(int potionId, int durationSeconds, int amplifier, float chance)
    {
        _potionId        = potionId;
        _potionDuration  = durationSeconds;
        _potionAmplifier = amplifier;
        _potionChance    = chance;
        return this;
    }

    // ── Methods (spec §3) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>b(dk stack)</c> — getMaxItemUseDuration. Returns 32 unconditionally.
    /// </summary>
    public override int GetMaxItemUseDuration(ItemStack stack) => 32;

    /// <summary>
    /// obf: <c>c(dk stack, ry world, vi player)</c> — onItemRightClick.
    /// Starts the eat animation if the player can eat.
    /// </summary>
    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is EntityPlayer ep && CanEat(ep))
            ep.StartUsingItem(stack, 32);
        return stack;
    }

    /// <summary>
    /// obf: <c>a(dk stack, ry world, vi player)</c> — onEaten / finishUsingItem.
    /// Called when the 32-tick eat animation completes.
    /// Decrements stack, restores food, plays burp sound, optionally applies potion effect.
    /// </summary>
    public override ItemStack FinishUsingItem(ItemStack stack, World world, object player)
    {
        stack.StackSize--;

        if (player is EntityPlayer ep)
        {
            ep.FoodStats.AddFood(_healAmount, _saturationModifier);

            world.PlaySoundAt(ep, "random.burp", 0.5f,
                world.Random.NextFloat() * 0.1f + 0.9f);

            if (!world.IsClientSide && _potionId > 0
                && world.Random.NextFloat() < _potionChance)
            {
                // Stub: potion effect application — PotionEffect system not yet implemented.
                // ep.AddPotionEffect(new PotionEffect(_potionId, _potionDuration * 20, _potionAmplifier));
            }
        }

        return stack;
    }

    // ── Accessors ─────────────────────────────────────────────────────────────

    /// <summary>Returns heal amount (hunger units).</summary>
    public int GetHealAmount() => _healAmount;

    /// <summary>Returns saturation modifier.</summary>
    public float GetSaturationModifier() => _saturationModifier;

    /// <summary>Returns true if this item is always edible (even when full).</summary>
    public bool IsAlwaysEdible() => _alwaysEdible;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="player"/> may eat this item.
    /// Spec: <c>player.b(bT)</c> — true when (alwaysEdible OR hungry) AND NOT invulnerable (creative).
    /// </summary>
    private bool CanEat(EntityPlayer player)
        => (_alwaysEdible || player.FoodStats.IsHungry()) && !player.Abilities.Invulnerable;

    // ── Static food item registry (spec §4 — 14 items) ───────────────────────

    /// <summary>obf: <c>acy.i</c>   — Apple           (ID 260). heal=4, sat=0.3F.</summary>
    public static readonly ItemFood Apple
        = new ItemFood(4, 4, 0.3f, false)
            .SetIcon(10, 0) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.T</c>   — Bread           (ID 297). heal=5, sat=0.6F.</summary>
    public static readonly ItemFood Bread
        = new ItemFood(41, 5, 0.6f, false)
            .SetIcon(13, 0) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.ap</c>  — Raw Porkchop    (ID 319). heal=3, sat=0.3F, wolfFood.</summary>
    public static readonly ItemFood PorkRaw
        = new ItemFood(63, 3, 0.3f, true)
            .SetIcon(7, 1) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.aq</c>  — Cooked Porkchop (ID 320). heal=8, sat=0.8F, wolfFood.</summary>
    public static readonly ItemFood PorkCooked
        = new ItemFood(64, 8, 0.8f, true)
            .SetIcon(8, 1) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.aT</c>  — Raw Fish        (ID 349). heal=2, sat=0.3F.</summary>
    public static readonly ItemFood FishRaw
        = new ItemFood(93, 2, 0.3f, false)
            .SetIcon(9, 4) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.aU</c>  — Cooked Fish     (ID 350). heal=5, sat=0.6F.</summary>
    public static readonly ItemFood FishCooked
        = new ItemFood(94, 5, 0.6f, false)
            .SetIcon(10, 4) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.bb</c>  — Cookie          (ID 357). heal=1, sat=0.1F.</summary>
    public static readonly ItemFood Cookie
        = new ItemFood(101, 1, 0.1f, false)
            .SetIcon(13, 5) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.be</c>  — Melon Slice     (ID 360). heal=2, sat=0.3F.</summary>
    public static readonly ItemFood MelonSlice
        = new ItemFood(104, 2, 0.3f, false)
            .SetIcon(13, 6) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.bh</c>  — Raw Beef        (ID 363). heal=3, sat=0.3F, wolfFood.</summary>
    public static readonly ItemFood BeefRaw
        = new ItemFood(107, 3, 0.3f, true)
            .SetIcon(10, 6) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>obf: <c>acy.bi</c>  — Steak           (ID 364). heal=8, sat=0.8F, wolfFood.</summary>
    public static readonly ItemFood BeefCooked
        = new ItemFood(108, 8, 0.8f, true)
            .SetIcon(11, 6) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>
    /// obf: <c>acy.bj</c>  — Raw Chicken     (ID 365). heal=2, sat=0.3F, wolfFood.
    /// 30% chance of Hunger effect for 30 s.
    /// </summary>
    public static readonly ItemFood ChickenRaw
        = new ItemFood(109, 2, 0.3f, true)
            .SetOnEatPotion(17 /* Hunger */, 30, 0, 0.30f);

    /// <summary>obf: <c>acy.bk</c>  — Cooked Chicken  (ID 366). heal=6, sat=0.6F, wolfFood.</summary>
    public static readonly ItemFood ChickenCooked
        = new ItemFood(110, 6, 0.6f, true)
            .SetIcon(9, 6) as ItemFood ?? throw new InvalidOperationException();

    /// <summary>
    /// obf: <c>acy.bl</c>  — Rotten Flesh    (ID 367). heal=4, sat=0.1F, wolfFood.
    /// 80% chance of Hunger effect for 30 s.
    /// </summary>
    public static readonly ItemFood RottenFlesh
        = new ItemFood(111, 4, 0.1f, true)
            .SetOnEatPotion(17 /* Hunger */, 30, 0, 0.80f);

    /// <summary>
    /// obf: <c>acy.bt</c>  — Spider Eye      (ID 375). heal=2, sat=0.8F.
    /// 100% chance of Poison effect for 5 s.
    /// </summary>
    public static readonly ItemFood SpiderEye
        = new ItemFood(119, 2, 0.8f, false)
            .SetOnEatPotion(19 /* Poison */, 5, 0, 1.00f);
}
