namespace SpectraEngine.Core.Enchantments;

// ── Protection group (`ii.java`) — IDs 0-4 ───────────────────────────────────

/// <summary>
/// Replica of <c>ii</c> — armor protection enchantments (IDs 0–4).
///
/// Subtypes:
///   0 = Protection      — all non-fire damage; target: armor; max level 4
///   1 = FireProtection  — fire damage;         target: armor; max level 4
///   2 = FeatherFalling  — fall damage;         target: boots; max level 4
///   3 = BlastProtection — explosion damage;    target: armor; max level 4
///   4 = ProjectileProtection — projectile;     target: armor; max level 4
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §7
/// </summary>
public sealed class EnchantmentProtection : Enchantment
{
    private readonly int _type; // 0=Protection,1=Fire,2=Feather,3=Blast,4=Projectile

    public EnchantmentProtection(int id, int weight, int type)
        : base(id, weight, type == 2 ? EnchantmentTarget.Boots : EnchantmentTarget.Armor)
    {
        _type = type;
    }

    public override int GetMaxLevel() => 4;

    public override int GetMinPower(int level) => _type switch
    {
        0 => 1  + (level - 1) * 16,
        1 => 10 + (level - 1) * 8,
        2 => 5  + (level - 1) * 6,
        3 => 5  + (level - 1) * 8,
        4 => 3  + (level - 1) * 6,
        _ => base.GetMinPower(level),
    };

    public override int GetMaxPower(int level) => _type switch
    {
        0 => GetMinPower(level) + 20,
        1 => GetMinPower(level) + 12,
        2 => GetMinPower(level) + 10,
        3 => GetMinPower(level) + 12,
        4 => GetMinPower(level) + 15,
        _ => GetMinPower(level) + 5,
    };

    /// <summary>
    /// Mutual exclusivity (spec §7): any two Protection types block each other,
    /// EXCEPT FeatherFalling (type 2) can coexist with non-FeatherFalling protection.
    /// </summary>
    public override bool IsCompatibleWith(Enchantment other)
    {
        if (other is not EnchantmentProtection otherP) return true;
        // FeatherFalling (2) coexists with any non-FeatherFalling
        if (_type == 2 || otherP._type == 2) return true;
        return false;
    }

    public override int GetDamageReduction(int level, DamageSource source)
    {
        bool applies = _type switch
        {
            0 => !source.IsFireDamage,
            1 => source.IsFireDamage,
            2 => source.IsFallDamage,
            3 => source.IsExplosionDamage,
            4 => source.IsProjectileDamage,
            _ => false,
        };
        if (!applies) return 0;
        int reduction = (6 + level * level) / 2;
        // FeatherFalling doubles fall protection (spec §9)
        if (_type == 2) reduction *= 2;
        return reduction;
    }
}

// ── Helmet enchantments ───────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vu</c> — Respiration (ID 5). Helmet only. Max level 3.
/// MinPower(L) = 10×L; MaxPower(L) = 10×L + 30.
/// </summary>
public sealed class EnchantmentRespiration : Enchantment
{
    public EnchantmentRespiration(int id, int weight)
        : base(id, weight, EnchantmentTarget.Helmet) { }

    public override int GetMaxLevel() => 3;
    public override int GetMinPower(int level) => 10 * level;
    public override int GetMaxPower(int level) => GetMinPower(level) + 30;
}

/// <summary>
/// Replica of <c>adz</c> — AquaAffinity (ID 6). Helmet only. Max level 1.
/// MinPower = 1; MaxPower = 41.
/// </summary>
public sealed class EnchantmentAquaAffinity : Enchantment
{
    public EnchantmentAquaAffinity(int id, int weight)
        : base(id, weight, EnchantmentTarget.Helmet) { }

    public override int GetMinPower(int level) => 1;
    public override int GetMaxPower(int level) => 41;
}

// ── Sword damage group (`ap.java`) — IDs 16-18 ───────────────────────────────

/// <summary>
/// Replica of <c>ap</c> — sword damage enchantments (Sharpness/Smite/BaneOfArthropods).
///
/// Subtypes:
///   0 = Sharpness (ID 16)      — always;         bonus = level×3
///   1 = Smite (ID 17)          — undead only;    bonus = level×4
///   2 = BaneOfArthropods (ID 18) — arthropods;  bonus = level×4
///
/// All three are mutually exclusive with each other. Max level 5.
/// Source spec: §7 + §9.
/// </summary>
public sealed class EnchantmentDamage : Enchantment
{
    private readonly int _type; // 0=Sharpness,1=Smite,2=Bane

    public EnchantmentDamage(int id, int weight, int type)
        : base(id, weight, EnchantmentTarget.Sword)
    {
        _type = type;
    }

    public override int GetMaxLevel() => 5;

    public override int GetMinPower(int level) => _type switch
    {
        0 => 1 + (level - 1) * 16,
        1 => 5 + (level - 1) * 8,
        2 => 5 + (level - 1) * 8,
        _ => base.GetMinPower(level),
    };

    public override int GetMaxPower(int level) => GetMinPower(level) + 20;

    /// <summary>All three damage types are mutually exclusive with each other.</summary>
    public override bool IsCompatibleWith(Enchantment other)
        => other is not EnchantmentDamage;

    public override int GetAttackBonus(int level, LivingEntity entity)
    {
        return _type switch
        {
            0 => level * 3,
            1 => entity.IsUndead ? level * 4 : 0,
            2 => entity.IsArthropod ? level * 4 : 0,
            _ => 0,
        };
    }
}

// ── Sword utility enchantments ────────────────────────────────────────────────

/// <summary>
/// Replica of <c>dz</c> — Knockback (ID 19). Sword. Max level 2.
/// MinPower(L) = 5 + 20×(L−1); MaxPower uses super.a(L)+50 = 1+L×10+50.
/// </summary>
public sealed class EnchantmentKnockback : Enchantment
{
    public EnchantmentKnockback(int id, int weight)
        : base(id, weight, EnchantmentTarget.Sword) { }

    public override int GetMaxLevel() => 2;
    public override int GetMinPower(int level) => 5 + 20 * (level - 1);
    public override int GetMaxPower(int level) => 1 + level * 10 + 50; // super.a(L)+50
}

/// <summary>
/// Replica of <c>aie</c> — FireAspect (ID 20). Sword. Max level 2.
/// MinPower(L) = 10 + 20×(L−1); MaxPower = 1+L×10+50.
/// </summary>
public sealed class EnchantmentFireAspect : Enchantment
{
    public EnchantmentFireAspect(int id, int weight)
        : base(id, weight, EnchantmentTarget.Sword) { }

    public override int GetMaxLevel() => 2;
    public override int GetMinPower(int level) => 10 + 20 * (level - 1);
    public override int GetMaxPower(int level) => 1 + level * 10 + 50;
}

/// <summary>
/// Replica of <c>qn</c> — LootBonus enchantment used by both Looting (ID 21, sword)
/// and Fortune (ID 35, digger). Max level 3.
/// MinPower(L) = 20 + (L−1)×12; MaxPower = 1+L×10+50.
/// SilkTouch ↔ Fortune mutual exclusivity handled here (spec §7 tool section).
/// </summary>
public sealed class EnchantmentLootBonus : Enchantment
{
    public EnchantmentLootBonus(int id, int weight, EnchantmentTarget target)
        : base(id, weight, target) { }

    public override int GetMaxLevel() => 3;
    public override int GetMinPower(int level) => 20 + (level - 1) * 12;
    public override int GetMaxPower(int level) => 1 + level * 10 + 50;

    public override bool IsCompatibleWith(Enchantment other)
    {
        // Fortune (ID 35) is incompatible with SilkTouch (ID 33)
        if (Id == 35 && other.Id == 33) return false;
        return base.IsCompatibleWith(other);
    }
}

// ── Tool enchantments ─────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>kr</c> — Efficiency (ID 32). Digger. Max level 5.
/// MinPower(L) = 1 + 15×(L−1); MaxPower = 1+L×10+50.
/// </summary>
public sealed class EnchantmentEfficiency : Enchantment
{
    public EnchantmentEfficiency(int id, int weight)
        : base(id, weight, EnchantmentTarget.Digger) { }

    public override int GetMaxLevel() => 5;
    public override int GetMinPower(int level) => 1 + 15 * (level - 1);
    public override int GetMaxPower(int level) => 1 + level * 10 + 50;
}

/// <summary>
/// Replica of <c>gi</c> — SilkTouch (ID 33). Digger. Max level 1.
/// MinPower = 25; MaxPower = 61 (1×10+1+50).
/// Incompatible with Fortune (ID 35).
/// </summary>
public sealed class EnchantmentSilkTouch : Enchantment
{
    public EnchantmentSilkTouch(int id, int weight)
        : base(id, weight, EnchantmentTarget.Digger) { }

    public override int GetMinPower(int level) => 25;
    public override int GetMaxPower(int level) => 61; // 1×10+1+50

    public override bool IsCompatibleWith(Enchantment other)
    {
        // SilkTouch is incompatible with Fortune (ID 35)
        if (other.Id == 35) return false;
        return base.IsCompatibleWith(other);
    }
}

/// <summary>
/// Replica of <c>dq</c> — Unbreaking (ID 34). Digger. Max level 3.
/// MinPower(L) = 5 + (L−1)×10; MaxPower = 1+L×10+50.
/// </summary>
public sealed class EnchantmentDurability : Enchantment
{
    public EnchantmentDurability(int id, int weight)
        : base(id, weight, EnchantmentTarget.Digger) { }

    public override int GetMaxLevel() => 3;
    public override int GetMinPower(int level) => 5 + (level - 1) * 10;
    public override int GetMaxPower(int level) => 1 + level * 10 + 50;
}
