# Spec: FontRenderer

**Java class:** Unknown (NOT `zh` — `zh` is TextureManager/atlas manager)
**Status:** PROVIDED — PARTIAL (class not found)
**Canonical name:** FontRenderer

---

## IMPORTANT: Research Finding

The FontRenderer class was **not found** during this research session. `zh.java` was
investigated but found to be the TextureManager / texture atlas manager, not FontRenderer.

---

## What Is Known (from call sites and conventions)

### Usage context

FontRenderer is referenced in:
- `xe` (GuiScreen) — renders button labels and screen text.
- `qd` (GuiIngame) — renders health numerals, coordinates (F3), chat.
- `adt` (EntityRenderer) — renders entity name tags above heads (if applicable in 1.0).

### Expected API (from 1.0 conventions)

```java
fontRenderer.drawString(String text, int x, int y, int colour)
fontRenderer.drawStringWithShadow(String text, int x, int y, int colour)
fontRenderer.getStringWidth(String text)    → int
fontRenderer.FONT_HEIGHT                   → int (constant, = 9)
```

### Rendering mechanism

- Uses a bitmap font from `textures/font/ascii.png` (128×128, 16×16 glyph grid).
- Each glyph occupies 8×8 pixels in the texture; rendered at scaled size.
- Character widths are variable (the font is proportional, not monospace).
- Character width table: 256-entry array, one per ASCII value, extracted from the image
  (width = rightmost non-transparent pixel column + 1).

### Colour encoding

Colour argument is packed ARGB (0xAARRGGBB). When the text contains `§` (section sign)
followed by a hex digit 0–f, the colour changes mid-string:

| Code | Colour |
|---|---|
| §0 | Black |
| §1 | Dark Blue |
| §2 | Dark Green |
| §3 | Dark Aqua |
| §4 | Dark Red |
| §5 | Dark Purple |
| §6 | Gold |
| §7 | Gray |
| §8 | Dark Gray |
| §9 | Blue |
| §a | Green |
| §b | Aqua |
| §c | Red |
| §d | Light Purple |
| §e | Yellow |
| §f | White |

### Shadow rendering

`drawStringWithShadow` renders the text twice: once at (x+1, y+1) in a darkened colour,
then at (x, y) in the main colour.

---

## TextureManager (`zh`) — What Was Found

`zh.java` is the texture atlas manager (not FontRenderer). It handles:
- Binding GL textures by resource path.
- Managing animated texture tiles (lava, water, fire, portal).
- Terrain and item texture stitching.

This is referenced as `Minecraft.textureManager` or similar.

---

## C# Mapping

| Java | C# |
|---|---|
| FontRenderer class (unknown) | `FontRenderer` |
| `drawString(...)` | `FontRenderer.DrawString(string text, int x, int y, int colour)` |
| `drawStringWithShadow(...)` | `FontRenderer.DrawStringWithShadow(string text, int x, int y, int colour)` |
| `getStringWidth(String)` | `FontRenderer.GetStringWidth(string text) : int` |
| `FONT_HEIGHT` | `FontRenderer.FontHeight = 9` |
| Colour format 0xAARRGGBB | `Color` or `int` packed |

---

## Open Questions

- Java class name for FontRenderer (obfuscated name unknown).
- Whether character widths are stored as a field array or recomputed from texture each load.
- Exact `ascii.png` glyph layout dimensions (8×8 per glyph confirmed? or 6×8?).
- Italic / bold formatting codes (§l bold, §o italic) — present in 1.0 or added later?
- Whether FontRenderer is a field on `Minecraft` instance or instantiated per-screen.

**Action for next research session:** Search decompiled files for a class containing:
- `FONT_HEIGHT` or equivalent int constant ≈ 9
- `drawString` overloads
- A `char[]` or `int[]` array of length 256 (character width table)
