namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>bn</c> (EnumSkyBlock / LightType).
/// Two-value enum distinguishing sky-light from block-light.
///
/// Confirmed from Chunk_Spec.md, World_Spec.md, and LightPropagation_Spec.md:
///   bn.a = sky-light, bn.b = block-light.
/// </summary>
public enum LightType
{
    /// <summary>obf: a — sky-light (0–15; attenuated by obstructions, zero in the Nether).</summary>
    Sky   = 0,

    /// <summary>obf: b — block-light (0–15; emitted by torches, lava, glowstone, etc.).</summary>
    Block = 1,
}
