# TerrainAtlas Spec
Source: `terrain.png` (extracted from spectraengine-1.0.jar), `yy.java` (Block class), `jb.java` (GrassBlock), `aip.java` (LogBlock)
Type: Visual / texture atlas reference

---

## 1. Atlas Dimensions

- File: `terrain.png` inside the game JAR
- Dimensions: **256 × 256 pixels**, RGBA (color_type=6, 8 bits per channel)
- Grid: **16 columns × 16 rows** = 256 tiles
- Tile size: **16 × 16 pixels**
- Format: Standard RGBA PNG (not palette-indexed)

### Tile index formula

```
col = index % 16
row = index / 16
pixelX = col * 16
pixelY = row * 16
```

---

## 2. Verified Tile Index Table

Indices relevant to currently implemented blocks. Visual descriptions from direct pixel analysis.
Alpha = 255 means fully opaque; lower values = semi-transparent or cutout.

| Index | Col | Row | Visual description | Used by |
|---|---|---|---|---|
| 0 | 0 | 0 | **Grass top — stored GRAY (~148), α=255. Un-tinted.** Requires biome grass color multiplication at runtime to appear green. | GrassBlock top face |
| 1 | 1 | 0 | Stone — medium gray (~124), α=255 | Stone |
| 2 | 2 | 0 | Dirt — red/brown (131,94,66), α=255 | Dirt; GrassBlock bottom face |
| 3 | 3 | 0 | Grass side — red/brown (129,93,65), α=255. The upper 2–4 rows of pixels are green (tinted grass); lower rows are brown (un-tinted dirt). Biome tint applies to top portion. | GrassBlock side face (default) |
| 4 | 4 | 0 | Planks — warm tan (154,125,77), α=255 | Wood planks |
| 5 | 5 | 0 | Stone slab top — light gray (~166), α=255 | Stone slab top |
| 7 | 7 | 0 | Brick — red/brown (146,99,86), α=255 | Brick block |
| 16 | 0 | 1 | Cobblestone — gray (~120) with lighter flecks, α=255 | Cobblestone |
| 17 | 1 | 1 | Bedrock — dark gray (~81), α=255 | Bedrock |
| 18 | 2 | 1 | Sand — bright yellowish-white (221,213,161), α=255 | Sand |
| 19 | 3 | 1 | Gravel — gray with dark spots (~127), α=255 | Gravel |
| 20 | 4 | 1 | Log side (oak) — brown wood grain (104,82,50), α=255 | Log side face, oak |
| 21 | 5 | 1 | Log top — circular cross-section, warm tan/brown, α=255 | Log top & bottom faces |
| 22 | 6 | 1 | Iron block — very light gray (~229), α=255 | Iron block |
| 23 | 7 | 1 | Gold block — yellow (254,249,83), α=255 | Gold block (appears yellow) |
| 24 | 8 | 1 | Diamond block — cyan/teal (136,229,225), α=255 | Diamond block |
| 32 | 0 | 2 | Gold ore — stone with gold flecks (160,153,126), α=255 | Gold ore |
| 33 | 1 | 2 | Iron ore — stone with rust spots (145,135,128), α=255 | Iron ore |
| 34 | 2 | 2 | Coal ore — stone with black patches (~105), α=255 | Coal ore |
| 36 | 4 | 2 | Mossy cobblestone — gray-green (98,122,98), α=255 | Mossy cobblestone |
| 49 | 1 | 3 | **Glass — CUTOUT texture. Center pixels nearly fully transparent (α≈27). Only border pixels are opaque (the glass frame).** Requires cutout-alpha rendering (discard pixels where α < threshold). | Glass |
| 50 | 2 | 3 | Diamond ore — stone with cyan specks (134,154,159), α=255 | Diamond ore |
| 51 | 3 | 3 | Redstone ore — stone with red dots (139,89,89), α=255 | Redstone ore |
| 52 | 4 | 3 | **Leaves — stored GRAY-GREEN (~79), α≈151 (semi-transparent). Must be multiplied by biome foliage color to appear green.** Semi-transparent = leaves allow sky light through. | Leaves |
| 66 | 2 | 4 | Snow — near-white (238,250,250), α=255 | Snow layer top; Snow block |
| 67 | 3 | 4 | Ice — blue-tinted semi-transparent (128,175,255, α≈159) | Ice |
| 68 | 4 | 4 | Grass side (snow-covered) — brown with white top strip, α=255 | GrassBlock side when snow is above |
| 70 | 6 | 4 | Cactus side — green (14,109,26), α=255 | Cactus |
| 72 | 8 | 4 | Clay — blue-gray (160,166,178), α=255 | Clay block |
| 73 | 9 | 4 | Reeds — green semi-transparent (~83,107,57, α≈143) | Sugar cane |
| 74 | 10 | 4 | Jukebox side — dark red/brown (111,70,49), α=255 | Jukebox |
| 76 | 12 | 4 | Water lily — gray-green, nearly opaque (α≈251) | Lily pad |
| 80 | 0 | 5 | Torch — very sparse pixels, mostly transparent (α≈47) | Torch |
| 83 | 3 | 5 | Ladder — brown cross-hatch, semi-transparent (α≈127) | Ladder |
| 85 | 5 | 5 | Iron bars — dark cutout (α≈63) | Iron fence / iron bars |
| 96 | 0 | 6 | Lever base — sparse, mostly transparent (α≈47) | Lever |
| 99 | 3 | 6 | Redstone torch (on) — orange glow, cutout (α≈71) | Redstone torch lit |
| 102 | 6 | 6 | Pumpkin side — orange carved (179,112,16), α=255 | Pumpkin |
| 103 | 7 | 6 | Netherrack — dark reddish (115,60,59), α=255 | Netherrack |
| 104 | 8 | 6 | Soul sand — dark brown (84,64,51), α=255 | Soul sand |
| 105 | 9 | 6 | Glowstone — tan/amber (152,124,74), α=255 | Glowstone |
| 115 | 3 | 7 | Redstone torch (off) — dark, cutout (α≈47) | Redstone torch unlit |
| 116 | 4 | 7 | Log side (spruce/pine) — dark brown with bark, α=255 | Log side, meta=1 (spruce) |
| 117 | 5 | 7 | Log side (birch) — white/silver birch pattern, α=255 | Log side, meta=2 (birch) |
| 121 | 9 | 7 | Cake top — pink/cream (230,189,190), α=255 | Cake |
| 128 | 0 | 8 | Rail (straight) — dark semi-transparent (α≈127) | Rail |
| 142 | 14 | 8 | Mushroom cap brown — warm tan (201,170,120), α=255 | Brown mushroom block |
| 144 | 0 | 9 | Lapis block — deep blue (28,70,168), α=255 | Lapis block |
| 160 | 0 | 10 | Lapis ore — stone with blue flecks (87,104,140), α=255 | Lapis ore |
| 164 | 4 | 10 | Redstone dust — gray semi-transparent (α≈139) | Redstone wire |
| 167 | 7 | 10 | Dragon egg — very dark (≈13,9,16), α=255 | Dragon egg |
| 175 | 15 | 10 | End stone — pale yellow (223,225,168), α=255 | End stone |
| 179 | 3 | 11 | Powered rail — dark brown with stripes (134,67,22, α≈191) | Powered rail (golden) |
| 195 | 3 | 12 | Detector rail — dark reddish semi-transparent (α≈175) | Detector rail |
| 224 | 0 | 14 | Nether brick — very dark reddish (45,23,27), α=255 | Nether brick |

---

## 3. Multi-Face Block Textures

### GrassBlock (`jb`) — Block ID 2

Face selection method: `a(int face, int meta)` and world-context `a(kq, x, y, z, int face)`

| Face ID | Condition | Texture index | Description |
|---|---|---|---|
| 1 | top | **0** | Grass top (gray, needs biome tint) |
| 0 | bottom | **2** | Dirt |
| 2–5 | sides (default) | **3** | Grass side |
| 2–5 | sides (snow above) | **68** | Snow-covered grass side |

> Face IDs follow Minecraft convention: 0=down, 1=up, 2=north, 3=south, 4=west, 5=east.

### Log Block (`aip`) — Block ID 17

Face selection method: `a(int face, int meta)`

| Face | Meta | Texture index | Description |
|---|---|---|---|
| 1 (top) | any | **21** | Log end (circular cross-section) |
| 0 (bottom) | any | **21** | Log end |
| sides | 0 (oak) | **20** | Oak log bark |
| sides | 1 (spruce) | **116** | Spruce log bark |
| sides | 2 (birch) | **117** | Birch log bark |

---

## 4. Rendering Requirements by Texture Type

### Type A — Opaque, no tint

All tiles with α=255 and no biome dependency. Render as solid textured quads.

Includes: stone (1), dirt (2), planks (4), cobblestone (16), bedrock (17), sand (18), gravel (19),
log top (21), all ores (32/33/34/50/51/160), iron block (22), gold block (23), diamond block (24),
lapis block (144), netherrack (103), soul sand (104), glowstone (105), etc.

### Type B — Opaque, requires biome color multiplication

Stored gray in PNG. At runtime multiply each RGB pixel by the biome color (a green or foliage
tone returned by `ha.a(temperature, rainfall)`).

| Index | Block | Biome color type |
|---|---|---|
| **0** | Grass top | Grass color (green) |
| **3** | Grass side (top strip only) | Grass color |
| **52** | Leaves | Foliage color |

**Default biome values (plains/generic):** `ha.a(0.5, 1.0)` — returns a mid-green.
The exact RGB is computed from a lookup table inside `ha.java`. Without implementing the full
biome system, a usable placeholder value is `(0x48, 0xB5, 0x18)` = `(72, 181, 24)`.

Multiplication formula per pixel:
```
outR = (texR * biomeR) / 255
outG = (texG * biomeG) / 255
outB = (texB * biomeB) / 255
```

### Type C — Cutout alpha (binary transparency)

Semi-transparent tiles that should render with cutout: discard pixels where α < 128, render
remaining pixels as fully opaque.

| Index | Block | Avg center α | Notes |
|---|---|---|---|
| **49** | Glass | ~27 | Center is empty; border ring is opaque. Requires back-face culling disabled or two-pass. |
| **52** | Leaves | ~151 | Also needs biome tint (Type B+C combined) |
| 73 | Reeds | ~143 | |
| 83 | Ladder | ~127 | |
| 85 | Iron bars | ~63 | |
| 128 | Rail | ~127 | |

### Type D — Truly transparent / decoration

Tiles used for torches, redstone dust, levers, vines — stored as very sparse pixels on a
transparent background. Renderer must preserve all alpha values (not cutout).

---

## 5. Root Cause of Gray Rendering

The observed gray rendering of GrassBlock and LeavesBlock is **not caused by wrong texture indices**
— the indices (0 for grass top, 52 for leaves) are correct and match the Java source.

The cause is **missing biome color multiplication**:
- Texture index 0 (grass top) is stored as neutral gray in terrain.png and is designed to be
  multiplied by the grass biome color at render time.
- Texture index 52 (leaves) is stored as gray-green with partial transparency and requires the
  foliage biome color multiplier.

**Fix required:** Before applying the texture, multiply the sampled color by the biome tint
color for tiles that require it. In the reference engine, this is done by:
1. `Block.c(int meta)` on the Block instance returns a packed RGB multiplier color.
2. For GrassBlock (`jb`), `c(int meta)` calls `f()` which calls `ha.a(0.5, 1.0)` (biome lookup).
3. For LeavesBlock (`qo`), similar override.
4. A return value of `16777215` (= `0xFFFFFF`) means no tint — use texture as-is.

For an immediate fix without biome system: hardcode the tint to `(72, 181, 24)` for grass and
`(78, 164, 0)` for foliage (Minecraft's default plains biome values at temp=0.5, rainfall=1.0).

---

## 6. Glass Rendering Note

Index 49 (glass) is a **border-only texture** — the 16×16 tile has opaque pixels only along
its outer 1–2 pixel border; the interior is fully transparent. Rendering requires:
- Enable alpha blending or cutout mode.
- For cutout mode: pixels with α < 128 are discarded (renderer sees only the border frame).
- Back-faces must be visible (disable face culling) or use two-sided rendering so the inside
  of the glass border is visible when adjacent glass faces meet.
- Adjacent glass blocks should NOT render the shared interior faces (this requires the block's
  `isOpaqueCube()` → false to trigger face-culling in the renderer).

---

## 7. Open Questions

1. **`ha.a(double temp, double rainfall)`** — BiomeColor lookup. Returns packed int RGB. The
   exact lookup table (likely a 256×256 gradient image `grasscolor.png` / `foliagecolor.png`)
   needs a spec if full biome color support is required.
2. **Leaves alpha blending** — in the reference engine, leaves use cutout (not smooth alpha).
   The alpha channel controls whether a pixel is drawn, not its translucency.
3. **Snow-covered grass side (index 68)** — verified present in atlas. The GrassBlock uses it
   when the material of the block directly above is snow (`p.u` or `p.v`).

---

*Spec written by Analyst AI from direct pixel analysis of terrain.png (256×256 RGBA) and `yy.java`/`jb.java`/`aip.java`. No C# implementation consulted.*
