namespace SpectraSharp.Core;

/// <summary>
/// Partial replica of <c>bn</c> (EnumSkyBlock / LightType).
/// Two-value enum distinguishing sky-light from block-light.
///
/// Confirmed from Chunk_Spec.md and World_Spec.md usage:
///   bn.a = sky-light, bn.b = block-light.
/// Full <c>bn</c> spec pending (see REQUESTS.md).
/// </summary>
public enum LightType
{
    /// <summary>obf: a — sky-light (0–15; attenuated by obstructions, zero in the Nether).</summary>
    Sky   = 0,

    /// <summary>obf: b — block-light (0–15; emitted by torches, lava, glowstone, etc.).</summary>
    Block = 1,
}
