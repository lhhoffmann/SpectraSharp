# Spec: Block Texture Rendering — Terrain Atlas UV Mapping

**Source:** `terrain.png` (extracted from spectraengine-1.0.jar), `yy.java` (Block registry),
plus per-block class files for all multi-face overrides.
**Status:** PROVIDED
**Canonical name:** TerrainAtlas / BlockTextureIndex

---

## 1. Atlas Dimensions

- File: `terrain.png` inside the game JAR
- Dimensions: **256 × 256 pixels**, RGBA (color_type=6, 8 bits per channel)
- Grid: **16 columns × 16 rows** = 256 tiles
- Tile size: **16 × 16 pixels**
- Format: Standard RGBA PNG (not palette-indexed)

### Tile index formula

```
col    = index % 16
row    = index / 16
pixelX = col * 16
pixelY = row * 16
```

---

## 2. Complete Block ID → Default Texture Index (`bL`)

Sourced from `yy.java` static initializers. `bL` is the constructor argument passed to
`super(id, textureIndex, material)`. Blocks marked **multi-face** have per-face overrides
— see Section 3 for the exact indices. Blocks marked **special** use entity rendering,
metadata, or animated tiles — see Sections 4–6.

| Block ID | Java field | Class | Name | bL (default) | Notes |
|---|---|---|---|---|---|
| 1 | `t` | `gm` | Stone | **1** | |
| 2 | `u` | `jb` | Grass Block | *0* | **multi-face** (top=0, bottom=2, sides=3/68) |
| 3 | `v` | `agd` | Dirt | **2** | |
| 4 | `w` | `yy` | Cobblestone | **16** | |
| 5 | `x` | `yy` | Planks | **4** | |
| 6 | `y` | `aet` | Sapling | **15** | cutout sprite |
| 7 | `z` | `yy` | Bedrock | **17** | |
| 8 | `A` | `ahx` | Water (flowing) | **205** | **animated** (see Section 5) |
| 9 | `B` | `add` | Water (still) | **205** | **animated** |
| 10 | `C` | `ahx` | Lava (flowing) | **237** | **animated** |
| 11 | `D` | `add` | Lava (still) | **237** | **animated** |
| 12 | `E` | `cj` | Sand | **18** | |
| 13 | `F` | `kb` | Gravel | **19** | |
| 14 | `G` | `v` | Gold Ore | **32** | |
| 15 | `H` | `v` | Iron Ore | **33** | |
| 16 | `I` | `v` | Coal Ore | **34** | |
| 17 | `J` | `aip` | Log | **20** | **multi-face** (top/bottom=21, sides by meta) |
| 18 | `K` | `qo` | Leaves | **52** | biome tint + cutout |
| 19 | `L` | `wh` | Sponge | **48** | |
| 20 | `M` | `aho` | Glass | **49** | cutout, border only |
| 21 | `N` | `v` | Lapis Ore | **160** | |
| 22 | `O` | `yy` | Lapis Block | **144** | |
| 23 | `P` | `cu` | Dispenser | **45** | **multi-face** (top/bottom=62, front=46, sides=45) |
| 24 | `Q` | `aat` | Sandstone | **192** | **multi-face** (top=176, bottom=208, sides=192) |
| 25 | `R` | `yq` | Note Block | **74** | uniform all faces |
| 26 | `S` | `aab` | Bed | — | special entity-style rendering |
| 27 | `T` | `afr` | Powered Rail | **179** | cutout sprite |
| 28 | `U` | `ags` | Detector Rail | **195** | cutout sprite |
| 29 | `V` | `abr` | Sticky Piston | **106** | **multi-face** (front=107, sides vary) |
| 30 | `W` | `kc` | Cobweb | **11** | cutout sprite |
| 31 | — | — | Tall Grass | — | cutout sprite (meta-based) |
| 32 | — | — | Dead Bush | — | cutout sprite |
| 33 | `Z` | `abr` | Piston | **107** | **multi-face** |
| 34 | — | `qz` | Piston Head | — | special |
| 35 | `ab` | `fr` | Wool | **64** | **meta-based** (see Section 4) |
| 36 | `ac` | `qz` | Piston Extension | — | technical block |
| 37 | — | — | Dandelion | — | cutout sprite |
| 38 | — | — | Rose | — | cutout sprite |
| 39 | — | — | Brown Mushroom | — | cutout sprite |
| 40 | — | — | Red Mushroom | — | cutout sprite |
| 41 | `ah` | `rs` | Gold Block | **23** | |
| 42 | `ai` | `rs` | Iron Block | **22** | |
| 43 | `aj` | `xs` | Double Stone Slab | — | **meta-based** (see Section 4) |
| 44 | `ak` | `xs` | Stone Slab | — | **meta-based** (see Section 4) |
| 45 | `al` | `yy` | Brick | **7** | |
| 46 | `am` | `abm` | TNT | **8** | **multi-face** (sides=8, top=9, bottom=10) |
| 47 | `an` | `ay` | Bookshelf | **35** | **multi-face** (sides=35, top/bottom=4) |
| 48 | `ao` | `yy` | Mossy Cobblestone | **36** | |
| 49 | `ap` | `ain` | Obsidian | **37** | |
| 50 | `aq` | `bg` | Torch | **80** | cutout sprite |
| 51 | — | — | Fire | — | animated cutout sprite |
| 52 | `as` | `kk` | Mob Spawner | **65** | |
| 53 | `at` | `ahh` | Oak Wood Stairs | *5* | uses planks texture |
| 54 | `au` | `au` | Chest | — | tile entity rendering |
| 55 | `av` | `kw` | Redstone Wire | **164** | special rendering |
| 56 | `aw` | `v` | Diamond Ore | **50** | |
| 57 | `ax` | `rs` | Diamond Block | **24** | |
| 58 | `ay` | `rn` | Workbench | **59** | **multi-face** (top=43, bottom=4, S/E=60, N/W=59) |
| 59 | `az` | `aha` | Wheat Crops | **88** | cutout, stage 0–7 (bL+meta) |
| 60 | `aA` | `ni` | Farmland | **87** | **multi-face** (see Section 3) |
| 61 | `aB` | `eu` | Furnace (off) | **45** | **multi-face** (top/bottom=62, front=44, sides=45) |
| 62 | `aC` | `eu` | Furnace (on) | **45** | **multi-face** (top/bottom=62, front=61, sides=45) |
| 63 | `aD` | `mr` | Sign (floor) | — | tile entity rendering |
| 64 | `aE` | `uc` | Door (Wood) | — | special (uses door texture strip) |
| 65 | `aF` | `afu` | Ladder | **83** | cutout |
| 66 | `aG` | `afr` | Rail | **128** | cutout sprite |
| 67 | `aH` | `ahh` | Cobblestone Stairs | *16* | uses cobblestone texture |
| 68 | `aI` | `mr` | Wall Sign | — | tile entity rendering |
| 69 | `aJ` | `aaa` | Lever | **96** | cutout sprite |
| 70 | — | `wx` | Stone Pressure Plate | *1* | uses stone bL |
| 71 | `aL` | `uc` | Door (Iron) | — | special |
| 72 | — | `wx` | Wooden Pressure Plate | *4* | uses planks bL |
| 73 | `aN` | `oc` | Redstone Ore | **51** | |
| 74 | `aO` | `oc` | Glowing Redstone Ore | **51** | same texture, emits light |
| 75 | `aP` | `ku` | Redstone Torch (off) | **115** | cutout sprite |
| 76 | `aQ` | `ku` | Redstone Torch (on) | **99** | cutout sprite |
| 77 | — | — | Stone Button | — | cutout |
| 78 | `aS` | `aif` | Snow Layer | **66** | thin slab geometry |
| 79 | `aT` | `ahq` | Ice | **67** | semi-transparent |
| 80 | `aU` | `jk` | Snow Block | **66** | same tile as snow layer |
| 81 | `aV` | `ow` | Cactus | **70** | **multi-face** (sides=70, top/bottom=71) |
| 82 | `aW` | `pc` | Clay Block | **72** | |
| 83 | `aX` | `md` | Sugar Cane | **73** | cutout sprite |
| 84 | `aY` | `abl` | Jukebox | **74** | **multi-face** (sides=74, top=75) |
| 85 | `aZ` | `nz` | Fence | **4** | planks texture; special geometry |
| 86 | `ba` | `nf` | Pumpkin | **102** | **multi-face** (top/bottom=102, sides=118, south=119) |
| 87 | `bb` | `et` | Netherrack | **103** | |
| 88 | `bc` | `mq` | Soul Sand | **104** | |
| 89 | `bd` | `sk` | Glowstone | **105** | |
| 90 | — | — | Nether Portal | — | animated/special |
| 91 | `bf` | `nf` | Jack-o-Lantern | **102** | **multi-face** (top/bottom=102, sides=118, south=119) |
| 92 | `bg` | `aem` | Cake | **121** | special partial-block geometry |
| 93 | `bh` | `mz` | Repeater (off) | — | special; uses stone base + torch |
| 94 | `bi` | `mz` | Repeater (on) | — | special |
| 95 | `bj` | `vj` | Locked Chest | — | special |
| 96 | `bk` | `mf` | Trapdoor | — | special |
| 97 | `bl` | `vf` | Monster Egg | — | mimics host block |
| 98 | `bm` | `jh` | Stone Brick | **54** | **meta-based** (0=54, 1=100, 2=101) |
| 99 | `bn` | `wd` | Brown Mushroom Block | **142** | **multi-face** (see Section 3) |
| 100 | `bo` | `wd` | Red Mushroom Block | **142** | **multi-face** (see Section 3) |
| 101 | `bp` | `uh` | Iron Bars | **85** | cutout, special fence geometry |
| 102 | `bq` | `uh` | Glass Pane | **49** / **148** | thin pane; bL=49 (glass), alt=148 |
| 103 | `br` | `of` | Melon | **136** | **multi-face** (top/bottom=137, sides=136) |
| 104 | `bs` | `pu` | Pumpkin Stem | — | cutout, stage-based |
| 105 | `bt` | `pu` | Melon Stem | — | cutout, stage-based |
| 106 | `bu` | `ahl` | Vines | — | cutout, special |
| 107 | `bv` | `fp` | Fence Gate | **4** | planks; special geometry |
| 108 | `bw` | `ahh` | Brick Stairs | *7* | uses brick bL |
| 109 | `bx` | `ahh` | Stone Brick Stairs | *54* | uses stone brick bL |
| 110 | — | `ez` | Mycelium | **77** | **multi-face** (top=78, bottom=2, sides=77) |
| 111 | `bz` | `qi` | Lily Pad | **76** | cutout |
| 112 | `bA` | `yy` | Nether Brick | **224** | |
| 113 | `bB` | `nz` | Nether Brick Fence | **224** | |
| 114 | `bC` | `ahh` | Nether Brick Stairs | *224* | uses nether brick bL |
| 115 | `bD` | `vy` | Nether Wart | — | cutout, stage-based |
| 116 | `bE` | `sy` | Enchanting Table | **166** | **multi-face** (top=166, sides=182, bottom=183) |
| 117 | `bF` | `ahp` | Brewing Stand | **157** | special geometry; base tile=157 |
| 118 | `bG` | `ic` | Cauldron | **154** | **multi-face** (sides=154, bottom=155, top-inner=138) |
| 119 | `bH` | `aid` | End Portal | — | special void rendering |
| 120 | `bI` | `rl` | End Portal Frame | **159** | **multi-face** (sides=159, bottom=158, top: no-eye=175, with-eye=159) |
| 121 | `bJ` | `yy` | End Stone | **175** | |
| 122 | `bK` | `aci` | Dragon Egg | **167** | |

*Italic bL values* are inherited from a referenced block's bL, not set directly.
*Dashes* indicate the block uses special or entity-based rendering that does not map to a single `bL`.

---

## 3. Multi-Face Texture Overrides

All indices are terrain.png tile indices. Face IDs: 0=down(−Y), 1=up(+Y), 2=north(−Z), 3=south(+Z), 4=west(−X), 5=east(+X).

---

### Grass Block (`jb`) — ID 2

Method: `a(kq, x, y, z, int face)` (world-context)

| Face | Condition | Index | Notes |
|---|---|---|---|
| 1 (top) | — | **0** | Gray stored; requires grass biome tint |
| 0 (bottom) | — | **2** | Dirt |
| 2–5 (sides) | no snow above | **3** | Grass side (top strip needs tint) |
| 2–5 (sides) | snow above (`p.u`/`p.v`) | **68** | Snow-covered side |

---

### Log Block (`aip`) — ID 17

Method: `a(int face, int meta)`

| Face | Meta | Index | Notes |
|---|---|---|---|
| 0, 1 (top/bottom) | any | **21** | Log end cross-section |
| 2–5 (sides) | 0 (oak) | **20** | Oak bark |
| 2–5 (sides) | 1 (spruce) | **116** | Spruce bark |
| 2–5 (sides) | 2 (birch) | **117** | Birch bark |

---

### Dispenser (`cu`) — ID 23

Method: `a(kq, x, y, z, int face)` (world-context, facing stored in meta)

| Face | Condition | Index | Notes |
|---|---|---|---|
| 0, 1 (top/bottom) | — | **62** | Furnace top texture |
| facing side | facing = this face | **46** | Dispenser front (hole) |
| other sides | — | **45** | Furnace side (same as furnace) |

---

### Sandstone (`aat`) — ID 24

Method: `b(int face)`

| Face | Index | Notes |
|---|---|---|
| 1 (top) | **176** | Sandstone top (smooth) |
| 0 (bottom) | **208** | Sandstone bottom (rough) |
| 2–5 (sides) | **192** | Sandstone carved side |

---

### TNT (`abm`) — ID 46

Method: `b(int face)` — constructor `abm(46, 8)` so bL=8

| Face | Index | Notes |
|---|---|---|
| 0 (bottom) | **10** | bL+2 |
| 1 (top) | **9** | bL+1 |
| 2–5 (sides) | **8** | bL (TNT side label) |

---

### Bookshelf (`ay`) — ID 47

Method: `b(int face)` — bL=35

| Face | Index | Notes |
|---|---|---|
| 0, 1 (top/bottom) | **4** | Planks |
| 2–5 (sides) | **35** | Bookshelf shelves texture |

---

### Workbench (`rn`) — ID 58

Method: `b(int face)` — bL=59

| Face | Index | Notes |
|---|---|---|
| 1 (top) | **43** | bL−16 (workbench top with tools) |
| 0 (bottom) | **4** | Planks |
| 2 (north), 4 (west) | **59** | Plain side |
| 3 (south), 5 (east) | **60** | Side with tools (bL+1) |

---

### Farmland (`ni`) — ID 60

Method: `a(int face, int meta)` — bL=87

| Face | Meta | Index | Notes |
|---|---|---|---|
| 1 (top) | >0 (moist) | **86** | Moist farmland (bL−1) |
| 1 (top) | 0 (dry) | **87** | Dry farmland |
| 0 (bottom), 2–5 (sides) | any | **2** | Dirt |

---

### Furnace (`eu`) — IDs 61 (off), 62 (on)

Method: `a(kq, x, y, z, int face)` — bL=45, `cb` flag set for lit

| Face | Condition | Index | Notes |
|---|---|---|---|
| 0, 1 (top/bottom) | — | **62** | bL+17 |
| facing side | lit (`cb`=true) | **61** | Lit furnace front (bL+16) |
| facing side | unlit | **44** | Unlit furnace front (bL−1) |
| other sides | — | **45** | Furnace side |

---

### Stone Brick (`jh`) — ID 98

Method: `a(int face, int meta)` — bL=54

| Meta | Index | Notes |
|---|---|---|
| 0 (default) | **54** | Plain stone brick |
| 1 (mossy) | **100** | Mossy stone brick |
| 2 (cracked) | **101** | Cracked stone brick |

---

### Pumpkin / Jack-o-Lantern (`nf`) — IDs 86, 91

Method: `b(int face)` — bL=102 for both

| Face | Index | Notes |
|---|---|---|
| 0 (bottom), 1 (top) | **102** | Pumpkin top/bottom |
| 3 (south, default facing) | **119** | bL+1+16 — carved/glowing face |
| 2, 4, 5 (other sides) | **118** | bL+16 — plain side |

World-context override additionally checks metadata for actual facing direction;
the carved face follows the meta-encoded facing.

---

### Brown Mushroom Block (`wd`) — ID 99

Method: `a(kq, x, y, z, int face)` — bL=142 (`this.a` = 0)

| Face | Condition | Index | Notes |
|---|---|---|---|
| 0 (bottom) | — | **141** | bL−1 (underside/pore) |
| 1 (top), 2–5 (sides) | face exposed (no same-block neighbor) | **142** | Cap face |
| 1 (top), 2–5 (sides) | face interior | **126** | bL−16 (brown pore/stem) |

---

### Red Mushroom Block (`wd`) — ID 100

Same logic as brown; `this.a` = 1.

| Face | Condition | Index | Notes |
|---|---|---|---|
| 0 (bottom) | — | **141** | Underside |
| exposed face | — | **142** | Red cap face |
| interior face | — | **125** | bL−16−1 (red pore) |

---

### Melon (`of`) — ID 103

Method: `b(int face)` — bL=136

| Face | Index | Notes |
|---|---|---|
| 0 (bottom), 1 (top) | **137** | Melon top (bL+1) |
| 2–5 (sides) | **136** | Melon side |

---

### Mycelium (`ez`) — ID 110

Method: `b(int face)` + world-context — bL=77

| Face | Condition | Index | Notes |
|---|---|---|---|
| 1 (top) | — | **78** | Mycelium top |
| 0 (bottom) | — | **2** | Dirt |
| 2–5 (sides) | no snow neighbor | **77** | Mycelium side |
| 2–5 (sides) | snow neighbor (`p.u`/`p.v`) | **68** | Snow-covered side (same as grass) |

---

### Enchanting Table (`sy`) — ID 116

Method: `b(int face)` — bL=166

| Face | Index | Notes |
|---|---|---|
| 0 (bottom) | **183** | bL+17 |
| 1 (top) | **166** | Enchanting table top (book) |
| 2–5 (sides) | **182** | bL+16 — side with runes |

---

### Cauldron (`ic`) — ID 118

Method: `b(int face)` — bL=154

| Face | Index | Notes |
|---|---|---|
| 1 (top) | **138** | Inner top (water/empty surface) |
| 0 (bottom) | **155** | bL+1 |
| 2–5 (sides) | **154** | Cauldron side |

---

### End Portal Frame (`rl`) — ID 120

Method: `b(int face)` + world-context — bL=159

| Face | Condition | Index | Notes |
|---|---|---|---|
| 0 (bottom) | — | **158** | bL−1 |
| 1 (top) | no eye (meta bit clear) | **175** | End stone top |
| 1 (top) | with eye (meta bit set) | **159** | Portal frame top |
| 2–5 (sides) | — | **159** | bL — frame side |

---

## 4. Metadata-Based Textures

### Wool (`fr`) — ID 35

Formula: `index = 113 + ((meta & 8) >> 3) + (meta & 7) * 16`

| Meta | Color | Index |
|---|---|---|
| 0 | White | 113 |
| 1 | Orange | 129 |
| 2 | Magenta | 145 |
| 3 | Light Blue | 161 |
| 4 | Yellow | 177 |
| 5 | Lime | 193 |
| 6 | Pink | 209 |
| 7 | Gray | 225 |
| 8 | Light Gray | 114 |
| 9 | Cyan | 130 |
| 10 | Purple | 146 |
| 11 | Blue | 162 |
| 12 | Brown | 178 |
| 13 | Green | 194 |
| 14 | Red | 210 |
| 15 | Black | 226 |

### Stone Slab / Double Slab (`xs`) — IDs 43, 44

Texture selected by metadata:

| Meta | Material | Index |
|---|---|---|
| 0 | Stone | **6** (stone slab top) |
| 1 | Sandstone | **18** |
| 2 | Wood | **4** |
| 3 | Cobblestone | **16** |
| 4 | Brick | **7** |
| 5 | Stone Brick | **54** |

---

## 5. Animated Tiles (Water and Lava)

Water and lava textures are animated — the game cycles through frames stored either as
sequential rows in `terrain.png` or as a separate animation spritesheet.

| Fluid | Class | Top/bottom index | Side index | Notes |
|---|---|---|---|---|
| Water (still/flowing) | `add`/`ahx` | **205** | **206** | bL = (12\*16)+13 = 205 |
| Lava (still/flowing) | `add`/`ahx` | **237** | **238** | bL = (14\*16)+13 = 237 |

Source (from `agw.java`):
```java
// agw constructor:
super(var1, (var2 == p.h ? 14 : 12) * 16 + 13, var2);
// lava (p.h): 14*16+13 = 237
// water (p.g): 12*16+13 = 205

// agw.b(int face):
return var1 != 0 && var1 != 1 ? this.bL + 1 : this.bL;
// sides = bL+1 (206 / 238), top/bottom = bL (205 / 237)
```

The renderer must treat tile indices 205, 206, 237, 238 as animated and swap frames
at the engine's tick cadence.

---

## 6. Biome Tint

### Tiles requiring runtime color multiplication

| Index | Block | Tint type |
|---|---|---|
| **0** | Grass top | Grass color from `ha.a(temp, rainfall)` |
| **3** | Grass side (top strip only) | Grass color |
| **52** | Leaves | Foliage color from `ha.a(temp, rainfall)` |

`ha.a(double temp, double rainfall)` returns a packed 0xRRGGBB integer.

**Default plains biome placeholders** (temp=0.5, rainfall=1.0):
- Grass tint: `(72, 181, 24)` = `0x48B518`
- Foliage tint: `(78, 164, 0)` = `0x4EA400`

Multiplication formula (per pixel):
```
outR = (texR * biomeR) / 255
outG = (texG * biomeG) / 255
outB = (texB * biomeB) / 255
```

`Block.c(int meta)` returns `0xFFFFFF` (no tint) for blocks that don't require it.
Return value ≠ `0xFFFFFF` means apply as a multiplier.

---

## 7. Rendering Classification

### Type A — Opaque solid (no tint)

Render as fully opaque textured quads. α=255 guaranteed. Includes the majority of blocks:
stone, dirt, planks, cobblestone, bedrock, sand, gravel, all ores, log, sponge,
lapis ore/block, gold/iron/diamond blocks, bookshelf, obsidian, mossy cobblestone, brick,
netherrack, soul sand, glowstone, end stone, nether brick, etc.

### Type B — Opaque, biome color multiplication

| Index | Block | Tint |
|---|---|---|
| 0 | Grass top | Grass |
| 3 | Grass side | Grass (top strip only) |
| 52 | Leaves | Foliage |

### Type C — Cutout alpha

Discard pixels with α < 128; render remainder fully opaque.

| Index | Block | Notes |
|---|---|---|
| **49** | Glass, Glass Pane | Border-only; needs two-sided rendering |
| **52** | Leaves | Also Type B |
| 73 | Reeds/Sugar Cane | |
| 83 | Ladder | |
| 85 | Iron Bars | |
| 128 | Rail | |

### Type D — Animated

Indices 205, 206 (water), 237, 238 (lava). Swap frame each tick.

### Type E — Sprite / cross-billboard

Torches (80), sapling (15), flowers, mushrooms, crops (88+meta), sugar cane (73),
vines — rendered as crossed-plane quads, not full block faces.

---

## 8. Complete Tile Index Reference

All tile indices confirmed as used by block rendering in 1.0:

| Index | Description | Used by |
|---|---|---|
| 0 | Grass top (gray; tinted) | GrassBlock top |
| 1 | Stone | Stone, pressure plate |
| 2 | Dirt | Dirt, GrassBlock bottom, Farmland sides, Mycelium bottom |
| 3 | Grass side (tinted strip) | GrassBlock sides |
| 4 | Planks | Planks, Fence, Fence Gate, Workbench bottom, Bookshelf top/bottom, Slab meta=2 |
| 5 | Stone slab top | Slab top (full slab stone face) |
| 6 | Stone slab side | Slab meta=0 |
| 7 | Brick | Brick block, Slab meta=4 |
| 8 | TNT side | TNT |
| 9 | TNT top | TNT |
| 10 | TNT bottom | TNT |
| 11 | Cobweb | Cobweb |
| 15 | Sapling | Sapling |
| 16 | Cobblestone | Cobblestone, Slab meta=3, Stone Stairs |
| 17 | Bedrock | Bedrock |
| 18 | Sand | Sand, Slab meta=1 |
| 19 | Gravel | Gravel |
| 20 | Log side (oak) | Log sides meta=0 |
| 21 | Log top/bottom | Log top/bottom |
| 22 | Iron block | Iron Block |
| 23 | Gold block | Gold Block |
| 24 | Diamond block | Diamond Block |
| 32 | Gold ore | Gold Ore |
| 33 | Iron ore | Iron Ore |
| 34 | Coal ore | Coal Ore |
| 35 | Bookshelf | Bookshelf sides |
| 36 | Mossy cobblestone | Mossy Cobblestone |
| 37 | Obsidian | Obsidian |
| 43 | Workbench top | Workbench top |
| 44 | Furnace front (off) | Furnace off |
| 45 | Furnace side | Furnace/Dispenser sides |
| 46 | Dispenser front | Dispenser front |
| 48 | Sponge | Sponge |
| 49 | Glass / Glass Pane | Glass, Glass Pane |
| 50 | Diamond ore | Diamond Ore |
| 51 | Redstone ore | Redstone Ore (both) |
| 52 | Leaves (tinted) | Leaves |
| 54 | Stone brick | Stone Brick meta=0, Slab meta=5, Stone Brick Stairs |
| 59 | Workbench side N/W | Workbench |
| 60 | Workbench side S/E | Workbench |
| 61 | Furnace front (on) | Furnace lit |
| 62 | Furnace top | Furnace/Dispenser top+bottom |
| 65 | Mob spawner | Mob Spawner |
| 66 | Snow | Snow layer, Snow block |
| 67 | Ice | Ice |
| 68 | Grass side snow | GrassBlock/Mycelium sides (snow above) |
| 70 | Cactus side | Cactus |
| 72 | Clay | Clay |
| 73 | Sugar cane | Sugar Cane |
| 74 | Note block / Jukebox | Note Block, Jukebox |
| 76 | Lily pad | Lily Pad |
| 77 | Mycelium side | Mycelium |
| 78 | Mycelium top | Mycelium |
| 80 | Torch | Torch |
| 83 | Ladder | Ladder |
| 85 | Iron bars | Iron Bars |
| 86 | Farmland moist top | Farmland (meta>0) |
| 87 | Farmland dry top | Farmland (meta=0) |
| 88 | Wheat stage 0 | Crops (bL+meta for stage 0–7) |
| 96 | Lever | Lever |
| 99 | Redstone torch (on) | Redstone Torch lit |
| 100 | Mossy stone brick | Stone Brick meta=1 |
| 101 | Cracked stone brick | Stone Brick meta=2 |
| 102 | Pumpkin top/bottom | Pumpkin, Jack-o-Lantern |
| 103 | Netherrack | Netherrack |
| 104 | Soul sand | Soul Sand |
| 105 | Glowstone | Glowstone |
| 113–226 | Wool colors | Wool (formula in Section 4) |
| 115 | Redstone torch (off) | Redstone Torch unlit |
| 116 | Log side (spruce) | Log meta=1 |
| 117 | Log side (birch) | Log meta=2 |
| 118 | Pumpkin side | Pumpkin / Jack-o-Lantern sides |
| 119 | Pumpkin face | Pumpkin / Jack-o-Lantern south face |
| 121 | Cake top | Cake |
| 125 | Red mushroom pore | Red Mushroom Block (interior) |
| 126 | Brown mushroom pore | Brown Mushroom Block (interior) |
| 128 | Rail | Rail |
| 136 | Melon side | Melon |
| 137 | Melon top | Melon |
| 138 | Cauldron inner top | Cauldron |
| 141 | Mushroom underside | Mushroom blocks (bottom) |
| 142 | Mushroom cap | Brown and Red Mushroom Block (cap face) |
| 144 | Lapis block | Lapis Block |
| 154 | Cauldron side | Cauldron |
| 155 | Cauldron bottom | Cauldron |
| 157 | Brewing stand | Brewing Stand |
| 158 | End portal frame bottom | End Portal Frame |
| 159 | End portal frame side | End Portal Frame |
| 160 | Lapis ore | Lapis Ore |
| 164 | Redstone dust | Redstone Wire |
| 166 | Enchanting table top | Enchanting Table |
| 167 | Dragon egg | Dragon Egg |
| 175 | End stone | End Stone; End Portal Frame top (no eye) |
| 176 | Sandstone top | Sandstone |
| 179 | Powered rail | Powered Rail |
| 182 | Enchanting table side | Enchanting Table |
| 183 | Enchanting table bottom | Enchanting Table |
| 192 | Sandstone side | Sandstone |
| 195 | Detector rail | Detector Rail |
| 205 | Water top/bottom | Water (animated) |
| 206 | Water side | Water (animated) |
| 208 | Sandstone bottom | Sandstone |
| 224 | Nether brick | Nether Brick, Nether Brick Fence/Stairs |
| 237 | Lava top/bottom | Lava (animated) |
| 238 | Lava side | Lava (animated) |

---

## 9. Previous Diagnostic Notes

### Root Cause of Gray Rendering (Grass / Leaves)

The gray appearance of GrassBlock and Leaves is **not a wrong texture index** — indices 0
and 52 are correct. The cause is **missing biome color multiplication** at render time.

- Index 0 (grass top): stored as neutral gray — must be multiplied by grass biome color.
- Index 52 (leaves): stored gray-green with partial alpha — must be multiplied by foliage color.

**Immediate fix (no biome system):** hardcode tint `(72, 181, 24)` for grass, `(78, 164, 0)`
for foliage.

### Glass Rendering

Index 49 is border-only (opaque ring, transparent interior). Requires:
- Cutout alpha mode (discard α < 128)
- Back-face culling disabled (or two-sided rendering)
- Shared interior faces between adjacent glass blocks should be omitted (`isOpaqueCube()` = false)

---

## 10. Open Questions

1. **`ha.a(temp, rainfall)` full biome color table** — likely a 256×256 gradient PNG
   (`grasscolor.png` / `foliagecolor.png`) in the JAR. Needs a separate spec if full biome
   color support is required.
2. **Leaves alpha blending** — reference engine uses cutout (not smooth alpha). The alpha
   channel is a mask only, not a translucency value.
3. **Animated tile implementation** — exact frame count and frame duration for water/lava
   animations. The reference engine uses a 32-frame or 64-frame strip stored as additional
   rows or a companion PNG.

---

*Spec written by Analyst AI from `yy.java` static block registry and per-block class files.
Direct pixel analysis of `terrain.png` (256×256 RGBA) used for visual descriptions in Section 8.
No C# implementation consulted. All texture indices are Java source–derived.*
