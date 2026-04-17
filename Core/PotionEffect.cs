namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>s</c> (PotionEffect) — active potion effect carried by a <see cref="LivingEntity"/>.
///
/// Fields:
///   a = effectId, b = duration (ticks remaining), c = amplifier (0 = level I).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PotionEffect_Spec.md §5
/// </summary>
public sealed class PotionEffect
{
    // ── Fields (spec §5.1) ────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — effect ID (index into <see cref="Potion.PotionTypes"/>).</summary>
    public readonly int EffectId;

    /// <summary>obf: <c>b</c> — remaining duration in ticks.</summary>
    public int Duration;

    /// <summary>obf: <c>c</c> — amplifier (0 = level I).</summary>
    public readonly int Amplifier;

    // ── Constructor ───────────────────────────────────────────────────────────

    public PotionEffect(int effectId, int duration, int amplifier = 0)
    {
        EffectId  = effectId;
        Duration  = duration;
        Amplifier = amplifier;
    }

    // ── Tick (spec §5.2) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(nq entity)</c> — ticks this effect. Returns true while still active.
    /// </summary>
    public bool Tick(LivingEntity entity)
    {
        if (Duration > 0)
        {
            var type = Potion.PotionTypes[EffectId];
            if (type != null && type.ShouldTriggerEffect(Duration, Amplifier))
                type.PerformEffect(entity, Amplifier);
            Duration--;
        }
        return Duration > 0;
    }

    // ── Combine (spec §5.3) ───────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(s other)</c> — merges a newly applied effect into this one.
    /// Higher amplifier always wins; same amplifier keeps longer duration.
    /// Returns the updated instance.
    /// </summary>
    public PotionEffect Combine(PotionEffect other)
    {
        if (other.Amplifier > Amplifier)
        {
            return new PotionEffect(EffectId, other.Duration, other.Amplifier);
        }
        if (other.Amplifier == Amplifier && other.Duration > Duration)
        {
            return new PotionEffect(EffectId, other.Duration, Amplifier);
        }
        return this;
    }

    // ── Accessors (spec §5.4) ─────────────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — effect ID.</summary>
    public int GetEffectId() => EffectId;

    /// <summary>obf: <c>b()</c> — remaining duration ticks.</summary>
    public int GetDuration() => Duration;

    /// <summary>obf: <c>c()</c> — amplifier.</summary>
    public int GetAmplifier() => Amplifier;

    /// <summary>obf: <c>d()</c> — name key string from effect type.</summary>
    public string GetEffectName() => Potion.PotionTypes[EffectId]?.NameKey ?? $"effect.{EffectId}";

    // ── ToString (spec §5.5) ─────────────────────────────────────────────────

    public override string ToString()
    {
        string name = GetEffectName();
        string base_ = Amplifier > 0 ? $"{name} x {Amplifier + 1}, Duration: {Duration}" : $"{name}, Duration: {Duration}";
        var type = Potion.PotionTypes[EffectId];
        return (type?.IsAmbient == true) ? $"({base_})" : base_;
    }
}
