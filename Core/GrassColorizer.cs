namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>ha</c> (GrassColorizer) — maps (temperature, rainfall) to a packed RGB
/// grass tint colour by looking up a 256×256 pre-loaded image (<c>grasscolor.png</c>).
///
/// The pixel array must be loaded from the game JAR before any colour query is made.
/// If not loaded, a fixed Plains-equivalent green is returned.
///
/// Index formula (spec §2):
///   col = (int)((1.0 − temp)     × 255.0)
///   row = (int)((1.0 − rainfall × temp) × 255.0)
///   pixel = pixels[row × 256 + col]
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeGenBase_Spec.md §2
/// </summary>
public static class GrassColorizer
{
    // ── State (spec §2) ───────────────────────────────────────────────────────

    private static int[]? _pixels; // 65536 packed ARGB / RGB values

    // ── Loader (spec §2) ──────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int[] pixels)</c> — stores the 65536-entry pixel array from grasscolor.png.
    /// Called once at startup after the JAR image is decoded.
    /// </summary>
    public static void SetPixels(int[] pixels) => _pixels = pixels;

    /// <summary>True once <see cref="SetPixels"/> has been called with a non-null array.</summary>
    public static bool IsLoaded => _pixels != null;

    // ── Lookup (spec §2) ──────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(double temp, double rainfall)</c> — returns packed RGB grass tint.
    /// Falls back to a fixed Plains green (0x48B518) if the image is not loaded.
    /// </summary>
    public static int GetGrassColor(double temp, double rainfall)
    {
        // Clamp inputs to valid range
        temp     = Math.Clamp(temp,     0.0, 1.0);
        rainfall = Math.Clamp(rainfall, 0.0, 1.0);

        if (_pixels == null)
            return 0x48B518; // Plains fallback: (72, 181, 24)

        rainfall *= temp;
        int col = (int)((1.0 - temp)     * 255.0);
        int row = (int)((1.0 - rainfall)  * 255.0);
        col = Math.Clamp(col, 0, 255);
        row = Math.Clamp(row, 0, 255);
        return _pixels[row << 8 | col] & 0xFFFFFF;
    }
}
