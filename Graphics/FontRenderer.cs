namespace SpectraEngine.Graphics;

/// <summary>
/// Replica of the FontRenderer class (Java class name unknown — not `zh`).
/// Renders bitmap text using the <c>ascii.png</c> glyph texture (16×16 glyph grid,
/// 8×8 pixels per glyph, variable-width proportional font).
///
/// Colour argument is packed 0xAARRGGBB. Inline colour codes: §0–§f change colour mid-string.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/FontRenderer_Spec.md
/// </summary>
public class FontRenderer
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Height of one text line in pixels. obf: <c>FONT_HEIGHT</c> = 9.</summary>
    public const int FontHeight = 9;

    // ── Colour table (§0–§f) ──────────────────────────────────────────────────

    private static readonly int[] ColourTable = unchecked(new int[]
    {
        (int)0xFF000000, // §0 Black
        (int)0xFF0000AA, // §1 Dark Blue
        (int)0xFF00AA00, // §2 Dark Green
        (int)0xFF00AAAA, // §3 Dark Aqua
        (int)0xFFAA0000, // §4 Dark Red
        (int)0xFFAA00AA, // §5 Dark Purple
        (int)0xFFFFAA00, // §6 Gold
        (int)0xFFAAAAAA, // §7 Gray
        (int)0xFF555555, // §8 Dark Gray
        (int)0xFF5555FF, // §9 Blue
        (int)0xFF55FF55, // §a Green
        (int)0xFF55FFFF, // §b Aqua
        (int)0xFFFF5555, // §c Red
        (int)0xFFFF55FF, // §d Light Purple
        (int)0xFFFFFF55, // §e Yellow
        (int)0xFFFFFFFF, // §f White
    });

    // ── Character width table (256 entries, pixels) ───────────────────────────

    /// <summary>
    /// Per-character advance width in pixels (variable-width font).
    /// Populated from <c>ascii.png</c> at load time by scanning each glyph column.
    /// Default 6 px is a safe stub until the texture is scanned.
    /// </summary>
    public readonly int[] CharWidths = new int[256];

    public FontRenderer()
    {
        Array.Fill(CharWidths, 6); // stub — real widths loaded from ascii.png
        CharWidths[' '] = 4;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the pixel width of <paramref name="text"/> accounting for colour codes.
    /// obf: <c>getStringWidth(String)</c>.
    /// </summary>
    public int GetStringWidth(string text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\u00A7' && i + 1 < text.Length) { i++; continue; } // skip §X
            width += CharWidths[c < 256 ? c : 63]; // '?' fallback
        }
        return width;
    }

    /// <summary>
    /// Draws text at (x, y) with the given packed ARGB colour.
    /// Stub — actual GL calls not yet implemented.
    /// obf: <c>drawString(String, int, int, int)</c>.
    /// </summary>
    public void DrawString(string text, int x, int y, int colour)
    {
        // Stub — route to GL texture draw once glyph atlas is loaded.
    }

    /// <summary>
    /// Draws text with a one-pixel drop shadow at (x+1, y+1) in darkened colour.
    /// obf: <c>drawStringWithShadow(String, int, int, int)</c>.
    /// </summary>
    public void DrawStringWithShadow(string text, int x, int y, int colour)
    {
        int shadow = DarkenColour(colour);
        DrawString(text, x + 1, y + 1, shadow);
        DrawString(text, x,     y,     colour);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int DarkenColour(int argb)
    {
        int a =  (argb >> 24) & 0xFF;
        int r = ((argb >> 16) & 0xFF) / 4;
        int g = ((argb >>  8) & 0xFF) / 4;
        int b =  (argb        & 0xFF) / 4;
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
}
