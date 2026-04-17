namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>abg</c> (Potion) — singleton effect-type definition, one per effect ID.
///
/// Static array <see cref="PotionTypes"/> is indexed by effect ID (1–19; 0 and 20–31 are null in 1.0).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PotionEffect_Spec.md §2
/// </summary>
public class Potion
{
    // ── Static registry ───────────────────────────────────────────────────────

    /// <summary>abg.a — effect registry indexed by ID (size 32). Slots 0 and 20-31 null in 1.0.</summary>
    public static readonly Potion?[] PotionTypes = new Potion?[32];

    // ── Static singletons (spec §2.2) ────────────────────────────────────────

    public static readonly Potion MoveSpeed       = new(1,  "potion.moveSpeed",      0x7CAFC6, iconCol: 0, iconRow: 0, factor: 1.00, isBad: false);
    public static readonly Potion MoveSlowdown    = new(2,  "potion.moveSlowdown",   0x5A6C81, iconCol: 1, iconRow: 0, factor: 0.50, isBad: true);
    public static readonly Potion DigSpeed        = new(3,  "potion.digSpeed",       0xD9C043, iconCol: 2, iconRow: 0, factor: 1.50, isBad: false);
    public static readonly Potion DigSlowDown     = new(4,  "potion.digSlowDown",    0x4A4217, iconCol: 3, iconRow: 0, factor: 0.50, isBad: true);
    public static readonly Potion DamageBoost     = new(5,  "potion.damageBoost",    0x932423, iconCol: 4, iconRow: 0, factor: 1.00, isBad: false);
    public static readonly Potion Heal            = new InstantPotion(6,  "potion.heal",          0xF82423, iconCol: -1, iconRow: -1, factor: 1.00, isBad: false);
    public static readonly Potion Harm            = new InstantPotion(7,  "potion.harm",          0x430A09, iconCol: -1, iconRow: -1, factor: 1.00, isBad: true);
    public static readonly Potion Jump            = new(8,  "potion.jump",           0x786297, iconCol: 2, iconRow: 1, factor: 1.00, isBad: false);
    public static readonly Potion Confusion       = new(9,  "potion.confusion",      0x551D4A, iconCol: 3, iconRow: 1, factor: 0.25, isBad: true);
    public static readonly Potion Regeneration    = new(10, "potion.regeneration",   0xCD5CAB, iconCol: 7, iconRow: 0, factor: 0.25, isBad: false);
    public static readonly Potion Resistance      = new(11, "potion.resistance",     0x99453A, iconCol: 6, iconRow: 1, factor: 1.00, isBad: false);
    public static readonly Potion FireResistance  = new(12, "potion.fireResistance", 0xE49A3A, iconCol: 7, iconRow: 1, factor: 1.00, isBad: false);
    public static readonly Potion WaterBreathing  = new(13, "potion.waterBreathing", 0x2E5299, iconCol: 0, iconRow: 2, factor: 1.00, isBad: false);
    public static readonly Potion Invisibility    = new(14, "potion.invisibility",   0x7F8392, iconCol: 0, iconRow: 1, factor: 1.00, isBad: false, isAmbient: true);
    public static readonly Potion Blindness       = new(15, "potion.blindness",      0x1F1F23, iconCol: 5, iconRow: 1, factor: 0.25, isBad: true);
    public static readonly Potion NightVision     = new(16, "potion.nightVision",    0x1F1FA1, iconCol: 4, iconRow: 1, factor: 1.00, isBad: false, isAmbient: true);
    public static readonly Potion Hunger          = new(17, "potion.hunger",         0x587653, iconCol: 1, iconRow: 1, factor: 0.50, isBad: true);
    public static readonly Potion Weakness        = new(18, "potion.weakness",       0x484D48, iconCol: 5, iconRow: 0, factor: 0.50, isBad: true);
    public static readonly Potion Poison          = new(19, "potion.poison",         0x4E9331, iconCol: 6, iconRow: 0, factor: 0.25, isBad: true);

    // ── Fields (spec §2.1) ────────────────────────────────────────────────────

    /// <summary>obf: <c>H</c> — effect ID.</summary>
    public readonly int Id;

    /// <summary>obf: <c>I</c> — name key.</summary>
    public readonly string NameKey;

    /// <summary>obf: <c>J</c> — icon index (col + row*8); -1 if not shown.</summary>
    public readonly int IconIndex;

    /// <summary>obf: <c>K</c> — harmful effect flag.</summary>
    public readonly bool IsBadEffect;

    /// <summary>obf: <c>L</c> — effect factor (controls tick interval).</summary>
    public readonly double EffectFactor;

    /// <summary>obf: <c>M</c> — ambient/faint particles.</summary>
    public readonly bool IsAmbient;

    /// <summary>obf: <c>N</c> — liquid color (packed RGB).</summary>
    public readonly int LiquidColor;

    // ── Constructor ───────────────────────────────────────────────────────────

    protected Potion(int id, string nameKey, int color, int iconCol, int iconRow,
                     double factor, bool isBad, bool isAmbient = false)
    {
        Id          = id;
        NameKey     = nameKey;
        LiquidColor = color;
        IconIndex   = (iconCol >= 0) ? iconCol + iconRow * 8 : -1;
        EffectFactor = factor;
        IsBadEffect  = isBad;
        IsAmbient    = isAmbient;

        PotionTypes[id] = this;
    }

    // ── Virtual: IsInstant ────────────────────────────────────────────────────

    /// <summary>Returns true for instant effects (Heal/Harm). obf: <c>a()</c> on py.</summary>
    public virtual bool IsInstant => false;

    // ── ShouldTriggerEffect (spec §4) ─────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int ticksRemaining, int amplifier)</c> — whether performEffect fires this tick.
    /// </summary>
    public virtual bool ShouldTriggerEffect(int ticksRemaining, int amplifier)
    {
        return Id switch
        {
            10 or 19 => TriggerByInterval(ticksRemaining, amplifier, 25), // Regen / Poison
            17       => true,                                               // Hunger: every tick
            _        => false                                               // attribute modifiers only
        };
    }

    private static bool TriggerByInterval(int ticks, int amplifier, int baseInterval)
    {
        int interval = baseInterval >> amplifier;
        return interval > 0 ? (ticks % interval == 0) : true;
    }

    // ── PerformEffect (spec §3) ───────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(nq entity, int amplifier)</c> — applies the per-tick effect.
    /// Only called when <see cref="ShouldTriggerEffect"/> returns true.
    /// </summary>
    public virtual void PerformEffect(LivingEntity entity, int amplifier)
    {
        switch (Id)
        {
            case 10: // Regeneration
                if (entity.GetCurrentHealth() < entity.GetMaxHealth())
                    entity.Heal(1);
                break;

            case 19: // Poison
                if (entity.GetCurrentHealth() > 1)
                    entity.AttackEntityFrom(DamageSource.Magic, 1);
                break;

            case 17: // Hunger
                if (entity is EntityPlayer player)
                    player.FoodStats.AddExhaustion(0.025f * (amplifier + 1));
                break;

            case 6: // Instant Health (handled in InstantPotion override)
            case 7: // Instant Damage
                PerformInstantEffect(entity, amplifier);
                break;
        }
    }

    /// <summary>
    /// Called for instant effects (Heal/Harm) — overridden in <see cref="InstantPotion"/>.
    /// </summary>
    protected virtual void PerformInstantEffect(LivingEntity entity, int amplifier) { }
}

/// <summary>
/// Replica of <c>py</c> — InstantPotion subclass. Overrides IsInstant → true.
/// Performs heal/harm immediately at application.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PotionEffect_Spec.md §6
/// </summary>
public sealed class InstantPotion : Potion
{
    public InstantPotion(int id, string nameKey, int color, int iconCol, int iconRow,
                         double factor, bool isBad, bool isAmbient = false)
        : base(id, nameKey, color, iconCol, iconRow, factor, isBad, isAmbient) { }

    public override bool IsInstant => true;

    public override bool ShouldTriggerEffect(int ticksRemaining, int amplifier) => true;

    protected override void PerformInstantEffect(LivingEntity entity, int amplifier)
    {
        int magnitude = 6 << amplifier; // 6 at level I, 12 at level II, etc.

        bool isUndead = entity.IsUndead;

        if (Id == 6) // Instant Health
        {
            if (isUndead) entity.AttackEntityFrom(DamageSource.Magic, magnitude);
            else          entity.Heal(magnitude);
        }
        else if (Id == 7) // Instant Damage
        {
            if (isUndead) entity.Heal(magnitude);
            else          entity.AttackEntityFrom(DamageSource.Magic, magnitude);
        }
    }
}
