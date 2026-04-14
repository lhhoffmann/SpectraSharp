# BlockRegistry Spec
Source: `yy.java` (Block class static initializer, lines 23–144, 630–671)
Type: Data / registry reference

---

## 1. Overview

The Block class (`yy`) owns a static array `k[256]` of all registered blocks.
Every block registers itself in the constructor: `k[bM] = this`. Slot 0 is air (null).
Blocks 1–122 are registered by `yy`'s static initializer.

Eight parallel arrays (also on `yy`, indexed by block ID):

| Array | Type | Meaning |
|---|---|---|
| `k[256]` | `yy[]` | Block instance (null = air) |
| `l[256]` | `boolean[]` | Unknown flag (set via `b(boolean)` builder, used by lockedchest) |
| `m[256]` | `boolean[]` | isOpaqueCube — set from `a()` result in constructor |
| `n[256]` | `boolean[]` | Unknown flag (initialized false) |
| `o[256]` | `int[]` | Light opacity — default 255 (fully opaque); set via `h(int)` |
| `p[256]` | `boolean[]` | Not-solid (= `!material.c()`) — set in constructor |
| `q[256]` | `int[]` | Light emission — set via `a(float lightFraction)` as `(int)(15.0F * val)` |
| `r[256]` | `boolean[]` | Tick-on-load — set via `l()` builder |
| `s[256]` | `boolean[]` | Special-drop flag — set in static block (see §3) |

Also `p[0] = true` is forced for air at the very end of the static block.

---

## 2. StepSound Instances (defined at top of yy)

| Field | Class | Sound key | Volume | Pitch |
|---|---|---|---|---|
| `b` | `wu` | `"stone"` | 1.0 | 1.0 |
| `c` | `wu` | `"wood"` | 1.0 | 1.0 |
| `d` | `wu` | `"gravel"` | 1.0 | 1.0 |
| `e` | `wu` | `"grass"` | 1.0 | 1.0 |
| `f` | `wu` | `"stone"` | 1.0 | 1.5 |
| `g` | `wu` | `"stone"` | 1.0 | 1.5 |
| `h` | `bj` | `"stone"` (glass subclass) | 1.0 | 1.0 |
| `i` | `wu` | `"cloth"` | 1.0 | 1.0 |
| `j` | `aeg` | `"sand"` | 1.0 | 1.0 |

---

## 3. Builder Method Reference

| Builder call | Effect |
|---|---|
| `c(float hardness)` | `bN = hardness`; if `bO < hardness*5` then `bO = hardness*5` |
| `b(float resistance)` | `bO = resistance * 3.0F` (unconditional — call order matters!) |
| `m()` | `c(-1.0F)` — indestructible (hardness = -1) |
| `a(float lightFrac)` | `q[bM] = (int)(15.0F * lightFrac)` — light emission |
| `h(int opacity)` | `o[bM] = opacity` — light opacity |
| `l()` | `r[bM] = true` — random tick enabled |
| `r()` | `bQ = false` — no stat tracking (water, fire, portal, etc.) |
| `b(boolean)` | `l[bM] = val` — used only by locked chest (ID 95) |
| `a(wu)` | `bX = val` — set StepSound |
| `a(String)` | Sets translation key to `"tile." + val` |

**Important:** `b(resistance)` overwrites `bO` unconditionally. Call order in source:
- `c(H).b(R)` → final `bO = R * 3` (resistance dominates if R*3 > H*5)
- `b(R).c(H)` → final `bO = max(R*3, H*5)`

---

## 4. Full Block Registry (IDs 1–122)

Columns:
- **ID** — block ID (slot in `k[]`)
- **Class** — Java class (obfuscated)
- **Texture** — primary `bL` texture index; `—` = class-specific (overrides `b(int)`)
- **Material** — `p.*` field name
- **StepSound** — `yy.*` field letter (see §2)
- **Hardness** — value passed to `c()`; `-1` = indestructible via `m()`; `—` = not called (bN=0)
- **Resistance** — value passed to `b()`; `—` = not called (bO = hardness*5 or 0)
- **LightEmit** — `q[bM]` value (0 = none); shown as `(int)(15*x)` if non-zero
- **LightOpacity** — `o[bM]` value (255 = default full; 0 = transparent)
- **Name key** — string passed to `a(String)` (prefixed with `"tile."`)
- **Flags** — `T`=tickable, `N`=noStats, `I`=indestructible (hardness=-1)

| ID | Field | Class | Texture | Material | StepSound | Hardness | Resistance | LightEmit | LightOpacity | Name key | Flags |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `t` | `gm` (Stone) | 1 | `p.e` | `f` | 1.5 | 10.0 | 0 | 255 | `stone` | |
| 2 | `u` | `jb` (Grass) | — | `p.e`(grass) | `e` | 0.6 | — | 0 | 255 | `grass` | |
| 3 | `v` | `agd` (Dirt) | 2 | `p.c`(gravel) | `d` | 0.5 | — | 0 | 255 | `dirt` | |
| 4 | `w` | `yy` (base) | 16 | `p.e` | — | 2.0 | 10.0 | 0 | 255 | `stonebrick` | |
| 5 | `x` | `yy` (base) | 4 | `p.d`(wood) | — | 2.0 | 5.0 | 0 | 255 | `wood` | T |
| 6 | `y` | `aet` (Sapling) | 15 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `sapling` | T |
| 7 | `z` | `yy` (base) | 17 | `p.e` | — | -1 | 6000000.0 | 0 | 255 | `bedrock` | N,I |
| 8 | `A` | `ahx` (FluidWater) | — | `p.g`(water) | — | 100.0 | — | 0 | 3 | `water` | N,T |
| 9 | `B` | `add` (StatWater) | — | `p.g`(water) | — | 100.0 | — | 0 | 3 | `water` | N,T |
| 10 | `C` | `ahx` (FluidLava) | — | `p.h`(lava) | — | 0.0 | — | 15 | 255 | `lava` | N,T |
| 11 | `D` | `add` (StatLava) | — | `p.h`(lava) | — | 100.0 | — | 15 | 255 | `lava` | N,T |
| 12 | `E` | `cj` (Sand) | 18 | `p.o`(sand) | `j` | 0.5 | — | 0 | 255 | `sand` | |
| 13 | `F` | `kb` (Gravel) | 19 | `p.c`(gravel) | `d` | 0.6 | — | 0 | 255 | `gravel` | |
| 14 | `G` | `v` (Ore) | 32 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreGold` | |
| 15 | `H` | `v` (Ore) | 33 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreIron` | |
| 16 | `I` | `v` (Ore) | 34 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreCoal` | |
| 17 | `J` | `aip` (Log) | — | `p.d`(wood) | `c` | 2.0 | — | 0 | 255 | `log` | T |
| 18 | `K` | `qo` (Leaves) | 52 | `p.e`(grass) | `e` | 0.2 | — | 0 | 1 | `leaves` | T |
| 19 | `L` | `wh` (Sponge) | — | `p.e`(grass) | `e` | 0.6 | — | 0 | 255 | `sponge` | |
| 20 | `M` | `aho` (Glass) | 49 | `p.q` | `h`(glass) | 0.3 | — | 0 | 255 | `glass` | |
| 21 | `N` | `v` (Ore) | 160 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreLapis` | |
| 22 | `O` | `yy` (base) | 144 | `p.e` | — | 3.0 | 5.0 | 0 | 255 | `blockLapis` | |
| 23 | `P` | `cu` (Dispenser) | — | `p.e` | `f` | 3.5 | — | 0 | 255 | `dispenser` | T |
| 24 | `Q` | `aat` (Sandstone) | — | `p.e` | `f` | 0.8 | — | 0 | 255 | `sandStone` | |
| 25 | `R` | `yq` (NoteBlock) | — | — | — | 0.8 | — | 0 | 255 | `musicBlock` | T |
| 26 | `S` | `aab` (Bed) | — | — | — | 0.2 | — | 0 | 255 | `bed` | N,T |
| 27 | `T` | `afr` (PoweredRail) | 179 | `p.g`(stone2) | `g` | 0.7 | — | 0 | 255 | `goldenRail` | T |
| 28 | `U` | `ags` (DetectorRail) | 195 | `p.g`(stone2) | `g` | 0.7 | — | 0 | 255 | `detectorRail` | T |
| 29 | `V` | `abr` (Piston) | 106 | — | — | — | — | 0 | 255 | `pistonStickyBase` | T |
| 30 | `W` | `kc` (Web) | 11 | — | — | 4.0 | — | 0 | 1 | `web` | |
| 31 | `X` | `kv` (TallGrass) | 39 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `tallgrass` | |
| 32 | `Y` | `jl` (DeadBush) | 55 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `deadbush` | |
| 33 | `Z` | `abr` (Piston) | 107 | — | — | — | — | 0 | 255 | `pistonBase` | T |
| 34 | `aa` | `acu` (PistonMoving) | 107 | — | — | — | — | 0 | 255 | — | T |
| 35 | `ab` | `fr` (Cloth/Wool) | — | `p.i`(cloth) | `i` | 0.8 | — | 0 | 255 | `cloth` | T |
| 36 | `ac` | `qz` (?) | — | — | — | — | — | 0 | 255 | — | |
| 37 | `ad` | `wg` (Flower) | 13 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `flower` | |
| 38 | `ae` | `wg` (Rose) | 12 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `rose` | |
| 39 | `af` | `js` (Mushroom) | 29 | `p.e`(grass) | `e` | 0.0 | — | 1 (0.125F) | 255 | `mushroom` | |
| 40 | `ag` | `js` (Mushroom) | 28 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `mushroom` | |
| 41 | `ah` | `rs` (MetalBlock) | 23 | `p.f`(iron) | `g` | 3.0 | 10.0 | 0 | 255 | `blockGold` | |
| 42 | `ai` | `rs` (MetalBlock) | 22 | `p.f`(iron) | `g` | 5.0 | 10.0 | 0 | 255 | `blockIron` | |
| 43 | `aj` | `xs` (Slab) double | — | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `stoneSlab` | |
| 44 | `ak` | `xs` (Slab) single | — | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `stoneSlab` | |
| 45 | `al` | `yy` (base) | 7 | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `brick` | |
| 46 | `am` | `abm` (TNT) | 8 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `tnt` | |
| 47 | `an` | `ay` (Bookshelf) | 35 | `p.d`(wood) | `c` | 1.5 | — | 0 | 255 | `bookshelf` | |
| 48 | `ao` | `yy` (base) | 36 | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `stoneMoss` | |
| 49 | `ap` | `ain` (Obsidian) | 37 | `p.e` | `f` | 50.0 | 2000.0 | 0 | 255 | `obsidian` | |
| 50 | `aq` | `bg` (Torch) | 80 | `p.d`(wood) | `c` | 0.0 | — | 14 (0.9375F) | 255 | `torch` | T |
| 51 | `ar` | `wj` (Fire) | 31 | `p.d`(wood) | `c` | 0.0 | — | 15 | 255 | `fire` | N |
| 52 | `as` | `kk` (MobSpawner) | 65 | `p.f`(iron) | `g` | 5.0 | — | 0 | 255 | `mobSpawner` | N |
| 53 | `at` | `ahh` (Stairs/Wood) | — | — | — | — | — | 0 | 255 | `stairsWood` | T |
| 54 | `au` | `au` (Chest) | — | `p.d`(wood) | `c` | 2.5 | — | 0 | 255 | `chest` | T |
| 55 | `av` | `kw` (RedstoneDust) | 164 | `p.b`(stone) | `b` | 0.0 | — | 0 | 255 | `redstoneDust` | N,T |
| 56 | `aw` | `v` (Ore) | 50 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreDiamond` | |
| 57 | `ax` | `rs` (MetalBlock) | 24 | `p.f`(iron) | `g` | 5.0 | 10.0 | 0 | 255 | `blockDiamond` | |
| 58 | `ay` | `rn` (Workbench) | — | `p.d`(wood) | `c` | 2.5 | — | 0 | 255 | `workbench` | |
| 59 | `az` | `aha` (Crops) | 88 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `crops` | N,T |
| 60 | `aA` | `ni` (Farmland) | — | `p.c`(gravel) | `d` | 0.6 | — | 0 | 255 | `farmland` | T |
| 61 | `aB` | `eu` (Furnace) off | — | `p.e` | `f` | 3.5 | — | 0 | 255 | `furnace` | T |
| 62 | `aC` | `eu` (Furnace) on | — | `p.e` | `f` | 3.5 | — | 13 (0.875F) | 255 | `furnace` | T |
| 63 | `aD` | `mr` (Sign) wall | — | `p.d`(wood) | `c` | 1.0 | — | 0 | 255 | `sign` | N,T |
| 64 | `aE` | `uc` (Door) wood | — | `p.d`(wood) | `c` | 3.0 | — | 0 | 255 | `doorWood` | N,T |
| 65 | `aF` | `afu` (Ladder) | 83 | `p.d`(wood) | `c` | 0.4 | — | 0 | 255 | `ladder` | T |
| 66 | `aG` | `afr` (Rail) | 128 | `p.g`(stone2) | `g` | 0.7 | — | 0 | 255 | `rail` | T |
| 67 | `aH` | `ahh` (Stairs/Stone) | — | — | — | — | — | 0 | 255 | `stairsStone` | T |
| 68 | `aI` | `mr` (Sign) floor | — | `p.d`(wood) | `c` | 1.0 | — | 0 | 255 | `sign` | N,T |
| 69 | `aJ` | `aaa` (Lever) | 96 | `p.d`(wood) | `c` | 0.5 | — | 0 | 255 | `lever` | T |
| 70 | `aK` | `wx` (PressurePlate) | stone | `p.e` | `f` | 0.5 | — | 0 | 255 | `pressurePlate` | T |
| 71 | `aL` | `uc` (Door) iron | — | `p.f`(iron) | `g` | 5.0 | — | 0 | 255 | `doorIron` | N,T |
| 72 | `aM` | `wx` (PressurePlate) | wood | `p.d`(wood) | `c` | 0.5 | — | 0 | 255 | `pressurePlate` | T |
| 73 | `aN` | `oc` (RedstoneOre) off | 51 | `p.e` | `f` | 3.0 | 5.0 | 0 | 255 | `oreRedstone` | T |
| 74 | `aO` | `oc` (RedstoneOre) on | 51 | `p.e` | `f` | 3.0 | 5.0 | 9 (0.625F) | 255 | `oreRedstone` | T |
| 75 | `aP` | `ku` (RedstoneTorch) off | 115 | `p.d`(wood) | `c` | 0.0 | — | 0 | 255 | `notGate` | T |
| 76 | `aQ` | `ku` (RedstoneTorch) on | 99 | `p.d`(wood) | `c` | 0.0 | — | 7 (0.5F) | 255 | `notGate` | T |
| 77 | `aR` | `ahv` (Button) | stone | `p.e` | `f` | 0.5 | — | 0 | 255 | `button` | T |
| 78 | `aS` | `aif` (SnowLayer) | 66 | `p.i`(cloth) | `i` | 0.1 | — | 0 | 0 | `snow` | |
| 79 | `aT` | `ahq` (Ice) | 67 | `p.q`(glass) | `h`(glass) | 0.5 | — | 0 | 3 | `ice` | |
| 80 | `aU` | `jk` (SnowBlock) | 66 | `p.i`(cloth) | `i` | 0.2 | — | 0 | 255 | `snow` | |
| 81 | `aV` | `ow` (Cactus) | 70 | `p.i`(cloth) | `i` | 0.4 | — | 0 | 255 | `cactus` | |
| 82 | `aW` | `pc` (Clay) | 72 | `p.c`(gravel) | `d` | 0.6 | — | 0 | 255 | `clay` | |
| 83 | `aX` | `md` (Reed) | 73 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `reeds` | N |
| 84 | `aY` | `abl` (Jukebox) | 74 | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `jukebox` | T |
| 85 | `aZ` | `nz` (Fence) | 4 | `p.d`(wood) | `c` | 2.0 | 5.0 | 0 | 255 | `fence` | |
| 86 | `ba` | `nf` (Pumpkin) unlit | 102 | `p.d`(wood) | `c` | 1.0 | — | 0 | 255 | `pumpkin` | T |
| 87 | `bb` | `et` (Netherrack) | 103 | `p.e` | `f` | 0.4 | — | 0 | 255 | `hellrock` | |
| 88 | `bc` | `mq` (SoulSand) | 104 | `p.o`(sand) | `j` | 0.5 | — | 0 | 255 | `hellsand` | |
| 89 | `bd` | `sk` (Glowstone) | 105 | `p.q`(glass) | `h`(glass) | 0.3 | — | 15 | 255 | `lightgem` | |
| 90 | `be` | `sc` (Portal) | 14 | `p.q`(glass) | `h`(glass) | -1 | — | 11 (0.75F) | 255 | `portal` | I |
| 91 | `bf` | `nf` (Pumpkin) lit | 102 | `p.d`(wood) | `c` | 1.0 | — | 15 | 255 | `litpumpkin` | T |
| 92 | `bg` | `aem` (Cake) | 121 | `p.i`(cloth) | `i` | 0.5 | — | 0 | 255 | `cake` | N,T |
| 93 | `bh` | `mz` (Diode) off | — | `p.d`(wood) | `c` | 0.0 | — | 0 | 255 | `diode` | N,T |
| 94 | `bi` | `mz` (Diode) on | — | `p.d`(wood) | `c` | 0.0 | — | 9 (0.625F) | 255 | `diode` | N,T |
| 95 | `bj` | `vj` (LockedChest) | — | `p.d`(wood) | `c` | 0.0 | — | 15 | 255 | `lockedchest` | T |
| 96 | `bk` | `mf` (TrapDoor) | — | `p.d`(wood) | `c` | 3.0 | — | 0 | 255 | `trapdoor` | N,T |
| 97 | `bl` | `vf` (Silverfish) | — | — | — | 0.75 | — | 0 | 255 | — | |
| 98 | `bm` | `jh` (StoneBrick) | — | `p.e` | `f` | 1.5 | 10.0 | 0 | 255 | `stonebricksmooth` | |
| 99 | `bn` | `wd` (MushroomCap) brown | 142 | `p.d`(wood) | `c` | 0.2 | — | 0 | 255 | `mushroom` | T |
| 100 | `bo` | `wd` (MushroomCap) red | 142 | `p.d`(wood) | `c` | 0.2 | — | 0 | 255 | `mushroom` | T |
| 101 | `bp` | `uh` (Pane) IronBars | 85/85 | `p.f`(iron) | `g` | 5.0 | 10.0 | 0 | 255 | `fenceIron` | |
| 102 | `bq` | `uh` (Pane) ThinGlass | 49/148 | `p.q`(glass) | `h`(glass) | 0.3 | — | 0 | 255 | `thinGlass` | |
| 103 | `br` | `of` (Melon) | — | `p.d`(wood) | `c` | 1.0 | — | 0 | 255 | `melon` | |
| 104 | `bs` | `pu` (Stem) pumpkin | — | `p.d`(wood) | `c` | 0.0 | — | 0 | 255 | `pumpkinStem` | T |
| 105 | `bt` | `pu` (Stem) melon | — | `p.d`(wood) | `c` | 0.0 | — | 0 | 255 | `pumpkinStem` | T |
| 106 | `bu` | `ahl` (Vine) | — | `p.e`(grass) | `e` | 0.2 | — | 0 | 255 | `vine` | T |
| 107 | `bv` | `fp` (FenceGate) | 4 | `p.d`(wood) | `c` | 2.0 | 5.0 | 0 | 255 | `fenceGate` | T |
| 108 | `bw` | `ahh` (Stairs/Brick) | — | — | — | — | — | 0 | 255 | `stairsBrick` | T |
| 109 | `bx` | `ahh` (Stairs/StoneBrick) | — | — | — | — | — | 0 | 255 | `stairsStoneBrickSmooth` | T |
| 110 | `by` | `ez` (Mycelium) | — | `p.e`(grass) | `e` | 0.6 | — | 0 | 255 | `mycel` | |
| 111 | `bz` | `qi` (LilyPad) | 76 | `p.e`(grass) | `e` | 0.0 | — | 0 | 255 | `waterlily` | |
| 112 | `bA` | `yy` (base) | 224 | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `netherBrick` | |
| 113 | `bB` | `nz` (NetherFence) | 224 | `p.e` | `f` | 2.0 | 10.0 | 0 | 255 | `netherFence` | |
| 114 | `bC` | `ahh` (Stairs/Nether) | — | — | — | — | — | 0 | 255 | `stairsNetherBrick` | T |
| 115 | `bD` | `vy` (NetherWart) | — | — | — | — | — | 0 | 255 | `netherStalk` | T |
| 116 | `bE` | `sy` (EnchantTable) | — | — | — | 5.0 | 2000.0 | 0 | 255 | `enchantmentTable` | |
| 117 | `bF` | `ahp` (BrewingStand) | — | — | — | 0.5 | — | 1 (0.125F) | 255 | `brewingStand` | T |
| 118 | `bG` | `ic` (Cauldron) | — | — | — | 2.0 | — | 0 | 255 | `cauldron` | T |
| 119 | `bH` | `aid` (EndPortal) | — | `p.A` | — | -1 | 6000000.0 | 0 | 255 | — | I |
| 120 | `bI` | `rl` (EndPortalFrame) | — | `p.q`(glass) | `h`(glass) | -1 | 6000000.0 | 1 (0.125F) | 255 | `endPortalFrame` | T,I |
| 121 | `bJ` | `yy` (base) | 175 | `p.e` | `f` | 3.0 | 15.0 | 0 | 255 | `whiteStone` | |
| 122 | `bK` | `aci` (DragonEgg) | 167 | `p.e` | `f` | 3.0 | 15.0 | 1 (0.125F) | 255 | `dragonEgg` | |

> Pressure plate ID 70 uses `t.bL` (stone texture index) and `xb.b` type; ID 72 uses `x.bL` (wood planks texture) and `xb.a` type.
> Button ID 77 uses `t.bL` (stone texture index).

---

## 5. s[] Special Drop Flag

The `s[blockId]` flag is set to `true` for blocks where the drop item differs from the block ID. Set in the static block:

```java
if (var0 > 0 && k[var0].c() == 10) var1 = true;   // c() override returns 10
if (var0 > 0 && k[var0] instanceof xs) var1 = true;  // Slab (IDs 43, 44)
if (var0 == aA.bM) var1 = true;                      // Farmland (ID 60)
```

Confirmed `s[]=true`: ID 43 (double slab), ID 44 (single slab), ID 60 (farmland).
Additional IDs: any block whose `c()` override returns 10 (class-specific, not enumerated here).

---

## 6. Item Default Registration Loop

After all blocks with special `Item` overrides are registered explicitly (cloth, log, stonebrick,
slab, sapling, leaves, vine, tallgrass, waterlily, piston), the static block runs:

```java
for (int var0 = 0; var0 < 256; var0++) {
    if (k[var0] != null && acy.d[var0] == null) {
        acy.d[var0] = new uw(var0 - 256);   // default ItemBlock
        k[var0].x_();                         // called when ItemBlock is created
    }
    // populate s[var0] ...
}
```

`x_()` is a no-op in the base `yy` class; subclasses may override.

---

## 7. Special Explicitly-Registered Items

| Block ID | Item class | Notes |
|---|---|---|
| 35 (Wool) | `ahb` | custom wool item |
| 17 (Log) | `afm` | log item, takes block param |
| 98 (StoneBrick) | `afm` | stonebrick item |
| 44 (Slab single) | `r` | slab item |
| 6 (Sapling) | `z` | sapling item |
| 18 (Leaves) | `og` | leaves item |
| 106 (Vine) | `hf` | vine item, `false` |
| 31 (TallGrass) | `hf` | tallgrass item, `true` with subtypes `["shrub","grass","fern"]` |
| 111 (LilyPad) | `pd` | lily pad item |
| 33 (Piston) | `afi` | piston item |
| 29 (StickyPiston) | `afi` | sticky piston item |

All other blocks default to `uw` (ItemBlock).

---

*Spec written by Analyst AI from `yy.java` static initializer and field declarations. No C# implementation consulted.*
