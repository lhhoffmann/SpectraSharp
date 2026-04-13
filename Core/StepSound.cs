namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>wu</c> (StepSound) — immutable descriptor for block walk/place/break sounds.
///
/// Note: The obfuscated name was previously misidentified as Material in classes.md.
/// Corrected: <c>wu</c> = StepSound, <c>p</c> = Material. See StepSound_Spec.md §0.
///
/// Quirks preserved (see spec §9):
///   1. GetPlaceSound() and GetStepSound() both return "step."+name in the base class.
///      GlassStepSound overrides only GetPlaceSound() → "random.glass", leaving
///      GetStepSound() still returning "step.stone" (glass walks like stone).
///   2. SandStepSound.GetPlaceSound() returns "step.gravel" — no "step.sand" exists.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/StepSound_Spec.md
/// </summary>
public class StepSound
{
    // ── Instance fields (spec §2) ─────────────────────────────────────────────

    /// <summary>Base sound name. obf: a</summary>
    public readonly string Name;   // obf: a

    /// <summary>Volume multiplier. obf: b</summary>
    public readonly float Volume;  // obf: b

    /// <summary>Pitch multiplier. obf: c</summary>
    public readonly float Pitch;   // obf: c

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    public StepSound(string name, float volume, float pitch)
    {
        Name   = name;
        Volume = volume;
        Pitch  = pitch;
    }

    // ── Methods (spec §5) ─────────────────────────────────────────────────────

    /// <summary>
    /// Sound played when placing/breaking. Default: "step." + Name.
    /// Overridden by subclasses (quirk 1 and 2). Spec: obf <c>a()</c>.
    /// </summary>
    public virtual string GetPlaceSound() => "step." + Name;

    /// <summary>Volume multiplier. Spec: obf <c>b()</c>.</summary>
    public float GetVolume() => Volume;

    /// <summary>Pitch multiplier. Spec: obf <c>c()</c>.</summary>
    public float GetPitch() => Pitch;

    /// <summary>
    /// Sound played when walking on the block. Always "step." + Name.
    /// Neither subclass overrides this method (quirk 1). Spec: obf <c>d()</c>.
    /// </summary>
    public string GetStepSound() => "step." + Name;

    // ── Subclasses (spec §6) ──────────────────────────────────────────────────

    /// <summary>
    /// bj — Glass/liquid StepSound. Overrides GetPlaceSound() → "random.glass".
    /// Walk sound remains "step.stone" (quirk 1).
    /// </summary>
    public sealed class GlassStepSound(string name, float volume, float pitch)
        : StepSound(name, volume, pitch)
    {
        public override string GetPlaceSound() => "random.glass";
    }

    /// <summary>
    /// aeg — Sand StepSound. Overrides GetPlaceSound() → "step.gravel".
    /// No "step.sand" sound exists (quirk 2).
    /// </summary>
    public sealed class SandStepSound(string name, float volume, float pitch)
        : StepSound(name, volume, pitch)
    {
        public override string GetPlaceSound() => "step.gravel";
    }
}
