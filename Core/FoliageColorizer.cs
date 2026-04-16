namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>db</c> (FoliageColorizer) — maps (temperature, rainfall) to a packed RGB
/// foliage tint colour by looking up a 256×256 pre-loaded image (<c>foliagecolor.png</c>).
///
/// Identical structure to <see cref="GrassColorizer"/> but for leaf blocks.
///
/// Additionally exposes three hardcoded fallback values used by <c>BlockLeaves</c> when
/// no world context is available (spec §3):
///   OakFoliage     = 0x619961 (6396257) — medium green
///   BirchFoliage   = 0x80A755 (8431445) — lighter yellow-green
///   SpruceFoliage  = 0x489B18 (4764952) — dark green
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeGenBase_Spec.md §3
/// </summary>
public static class FoliageColorizer
{
    // ── Hardcoded fallback values (spec §3) ───────────────────────────────────

    /// <summary>obf: <c>db.a()</c> — oak foliage (medium green).</summary>
    public const int OakFoliage    = 0x619961; // 6396257

    /// <summary>obf: <c>db.b()</c> — birch foliage (lighter yellow-green).</summary>
    public const int BirchFoliage  = 0x80A755; // 8431445

    /// <summary>obf: <c>db.c()</c> — spruce foliage (dark green).</summary>
    public const int SpruceFoliage = 0x489B18; // 4764952

    // ── State (spec §3) ───────────────────────────────────────────────────────

    private static int[]? _pixels;

    // ── Loader (spec §3) ──────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int[] pixels)</c> — stores the 65536-entry pixel array from foliagecolor.png.
    /// </summary>
    public static void SetPixels(int[] pixels) => _pixels = pixels;

    /// <summary>True once <see cref="SetPixels"/> has been called.</summary>
    public static bool IsLoaded => _pixels != null;

    // ── Lookup (spec §3) ──────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(double temp, double rainfall)</c> — returns packed RGB foliage tint.
    /// Falls back to <see cref="OakFoliage"/> if the image is not loaded.
    /// </summary>
    public static int GetFoliageColor(double temp, double rainfall)
    {
        temp     = Math.Clamp(temp,     0.0, 1.0);
        rainfall = Math.Clamp(rainfall, 0.0, 1.0);

        if (_pixels == null)
            return OakFoliage; // fallback

        rainfall *= temp;
        int col = (int)((1.0 - temp)     * 255.0);
        int row = (int)((1.0 - rainfall)  * 255.0);
        col = Math.Clamp(col, 0, 255);
        row = Math.Clamp(row, 0, 255);
        return _pixels[row << 8 | col] & 0xFFFFFF;
    }
}
