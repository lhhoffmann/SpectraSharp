namespace SpectraEngine.Core.Enchantments;

/// <summary>
/// Replica of <c>aef</c> (Enchantment) — base class for all enchantments.
///
/// Static registry: array of 36 slots (IDs 0–35). Each enchantment registers
/// itself in the constructor by writing to <see cref="EnchantmentsList"/>.
///
/// Default formulae (spec §7 base class):
///   MinPower(L) = 1 + L×10
///   MaxPower(L) = MinPower(L) + 5
///   MinLevel    = 1
///   MaxLevel    = 1
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §7
/// </summary>
public abstract class Enchantment
{
    // ── Static registry (spec §7) ─────────────────────────────────────────────

    public static readonly Enchantment?[] EnchantmentsList = new Enchantment?[36];

    // ── Static instances (spec §7 field names on aef) ─────────────────────────

    /// <summary>obf: <c>aef.c</c> — Protection (ID 0).</summary>
    public static readonly Enchantment Protection;
    /// <summary>obf: <c>aef.d</c> — FireProtection (ID 1).</summary>
    public static readonly Enchantment FireProtection;
    /// <summary>obf: <c>aef.e</c> — FeatherFalling (ID 2).</summary>
    public static readonly Enchantment FeatherFalling;
    /// <summary>obf: <c>aef.f</c> — BlastProtection (ID 3).</summary>
    public static readonly Enchantment BlastProtection;
    /// <summary>obf: <c>aef.g</c> — ProjectileProtection (ID 4).</summary>
    public static readonly Enchantment ProjectileProtection;
    /// <summary>obf: <c>aef.h</c> — Respiration (ID 5).</summary>
    public static readonly Enchantment Respiration;
    /// <summary>obf: <c>aef.i</c> — AquaAffinity (ID 6).</summary>
    public static readonly Enchantment AquaAffinity;
    /// <summary>obf: <c>aef.j</c> — Sharpness (ID 16).</summary>
    public static readonly Enchantment Sharpness;
    /// <summary>obf: <c>aef.k</c> — Smite (ID 17).</summary>
    public static readonly Enchantment Smite;
    /// <summary>obf: <c>aef.l</c> — BaneOfArthropods (ID 18).</summary>
    public static readonly Enchantment BaneOfArthropods;
    /// <summary>obf: <c>aef.m</c> — Knockback (ID 19).</summary>
    public static readonly Enchantment Knockback;
    /// <summary>obf: <c>aef.n</c> — FireAspect (ID 20).</summary>
    public static readonly Enchantment FireAspect;
    /// <summary>obf: <c>aef.o</c> — Looting (ID 21).</summary>
    public static readonly Enchantment Looting;
    /// <summary>obf: <c>aef.p</c> — Efficiency (ID 32).</summary>
    public static readonly Enchantment Efficiency;
    /// <summary>obf: <c>aef.q</c> — SilkTouch (ID 33).</summary>
    public static readonly Enchantment SilkTouch;
    /// <summary>obf: <c>aef.r</c> — Unbreaking (ID 34).</summary>
    public static readonly Enchantment Unbreaking;
    /// <summary>obf: <c>aef.s</c> — Fortune (ID 35).</summary>
    public static readonly Enchantment Fortune;

    static Enchantment()
    {
        Protection           = new EnchantmentProtection(0,  10, 0);
        FireProtection       = new EnchantmentProtection(1,   5, 1);
        FeatherFalling       = new EnchantmentProtection(2,   5, 2);
        BlastProtection      = new EnchantmentProtection(3,   2, 3);
        ProjectileProtection = new EnchantmentProtection(4,   5, 4);
        Respiration          = new EnchantmentRespiration(5,   2);
        AquaAffinity         = new EnchantmentAquaAffinity(6, 2);
        Sharpness            = new EnchantmentDamage(16, 10, 0);
        Smite                = new EnchantmentDamage(17,  5, 1);
        BaneOfArthropods     = new EnchantmentDamage(18,  5, 2);
        Knockback            = new EnchantmentKnockback(19, 5);
        FireAspect           = new EnchantmentFireAspect(20, 2);
        Looting              = new EnchantmentLootBonus(21,  2, EnchantmentTarget.Sword);
        Efficiency           = new EnchantmentEfficiency(32, 10);
        SilkTouch            = new EnchantmentSilkTouch(33,  1);
        Unbreaking           = new EnchantmentDurability(34,  5);
        Fortune              = new EnchantmentLootBonus(35,  2, EnchantmentTarget.Digger);
    }

    // ── Instance fields ───────────────────────────────────────────────────────

    /// <summary>obf: <c>t</c> — enchantment ID (slot in EnchantmentsList).</summary>
    public readonly int Id;

    /// <summary>obf: <c>c()</c> return value — weight for weighted random selection.</summary>
    public readonly int Weight;

    /// <summary>Which item types this enchantment can be applied to.</summary>
    public readonly EnchantmentTarget Target;

    // ── Constructor ───────────────────────────────────────────────────────────

    protected Enchantment(int id, int weight, EnchantmentTarget target)
    {
        Id     = id;
        Weight = weight;
        Target = target;
        EnchantmentsList[id] = this;
    }

    // ── Level range (spec §7 defaults) ───────────────────────────────────────

    /// <summary>obf: <c>d()</c> — minimum enchantment level (default 1).</summary>
    public virtual int GetMinLevel() => 1;

    /// <summary>obf: <c>a()</c> — maximum enchantment level (default 1).</summary>
    public virtual int GetMaxLevel() => 1;

    // ── Power range (spec §7 defaults) ───────────────────────────────────────

    /// <summary>obf: <c>a(int level)</c> — minimum enchantability power for this level.</summary>
    public virtual int GetMinPower(int level) => 1 + level * 10;

    /// <summary>obf: <c>b(int level)</c> — maximum enchantability power for this level.</summary>
    public virtual int GetMaxPower(int level) => GetMinPower(level) + 5;

    // ── Applicability / compatibility ─────────────────────────────────────────

    /// <summary>Returns true if this enchantment can be applied to the given item stack.</summary>
    public virtual bool CanApplyTo(ItemStack stack)
    {
        if (stack.GetItem() is not Item item) return false;
        return Target switch
        {
            EnchantmentTarget.Armor   => item is Items.ItemArmor,
            EnchantmentTarget.Boots   => item is Items.ItemArmor armor  && armor.ArmorType  == 3, // boots slot
            EnchantmentTarget.Helmet  => item is Items.ItemArmor armor2 && armor2.ArmorType == 0, // helmet slot
            EnchantmentTarget.Sword   => item is Items.ItemSword,
            EnchantmentTarget.Digger  => item is Items.ItemPickaxe or Items.ItemShovel or Items.ItemAxe,
            EnchantmentTarget.Fishing => item is Items.ItemFishingRod,
            _                         => false,
        };
    }

    /// <summary>
    /// obf: <c>a(aef other)</c> — returns true if this enchantment can coexist with <paramref name="other"/>.
    /// Default: incompatible with same type only.
    /// </summary>
    public virtual bool IsCompatibleWith(Enchantment other) => other != this;

    // ── Damage reduction (spec §9 — Protection group) ────────────────────────

    /// <summary>
    /// obf: <c>a(int level, pm source)</c> — damage reduction points for this enchantment
    /// against the given damage source. Default: 0 (non-Protection enchantments do not reduce damage).
    /// </summary>
    public virtual int GetDamageReduction(int level, DamageSource source) => 0;

    // ── Attack bonus (spec §9 — Damage group) ────────────────────────────────

    /// <summary>
    /// obf: <c>a(int level, nq entity)</c> — bonus attack damage against the given entity.
    /// Default: 0.
    /// </summary>
    public virtual int GetAttackBonus(int level, LivingEntity entity) => 0;
}

// ── EnchantmentTarget enum ────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>q</c> (EnchantmentTarget) — the set of item types an enchantment can apply to.
/// Spec: §7.
/// </summary>
public enum EnchantmentTarget
{
    Armor,
    Boots,
    Helmet,
    Sword,
    Digger,
    Fishing,
}
