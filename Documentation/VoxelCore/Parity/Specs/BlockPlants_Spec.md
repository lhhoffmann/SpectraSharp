<!--
  SpectraEngine Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraEngine Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# BlockPlants Spec (Batch)
**Source classes:** `wg.java` (base), `aet.java` (BlockSapling), `js.java` (BlockMushroom),
`md.java` (BlockReed), `vy.java` (BlockNetherWart), `pu.java` (BlockStem)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

Covers: BlockFlower (wg instances), BlockSapling (aet), BlockMushroom (js),
BlockReed (md), BlockNetherWart (vy), BlockStem (pu).

---

## 1. BlockFlower — `wg` instances

### 1.1 Block IDs

| Block | ID | Texture |
|---|---|---|
| Dandelion | 37 | (from yy static) |
| Rose | 38 | (from yy static) |

Both are plain `wg(blockId, textureIndex)` instances with no subclass overrides.

### 1.2 Behaviour

Inherits all `wg` base behaviour:
- Cross-plant render (type 1), no collision.
- Hitbox: `(0.3, 0.0, 0.3, 0.7, 0.6, 0.7)`.
- Survives on: grass (ID 2), dirt (ID 3), farmland (`yy.aA`).
- Removed if light < 8 AND no sky visibility, OR invalid soil below.
- Drops one flower item on break.

---

## 2. BlockSapling — `aet`

### 2.1 Block ID and Hierarchy

`aet` extends `wg`. Block ID 6.

### 2.2 Hitbox

`(0.1, 0.0, 0.1, 0.9, 0.8, 0.9)` — `0.5 ± 0.4` wide, `0.4 × 2 = 0.8` tall.

### 2.3 Metadata Encoding

| Bits | Meaning |
|---|---|
| Bits 0–1 (mask `0x3`) | Tree type: 0=Oak, 1=Spruce, 2=Birch |
| Bit 3 (mask `0x8`) | Growth flag: 0=young, 1=ready to grow |

### 2.4 Texture

`a(int, int meta)`:
- `meta & 3 == 1` → texture index 63 (spruce sapling).
- `meta & 3 == 2` → texture index 79 (birch sapling).
- Others → super (oak sapling default).

The `a(int)` strip method returns `meta & 3` (removes growth bit for texture lookup).

### 2.5 Random Tick

Server side only. Delegates survival check to `super.a()` first.

Then:
1. Check light above: `world.n(x, y+1, z) >= 9`.
2. Check 1/7 random chance.
3. If both true:
   - If bit 3 is clear: set bit 3 (`world.f(x, y, z, meta | 8)`).
   - If bit 3 already set: call `c()` (attempt tree grow).

### 2.6 Tree Growth — `c(World, x, y, z, Random)`

1. Read `treeType = meta & 3`.
2. Set block to air: `world.d(x, y, z, 0)`.
3. Choose generator:
   - Type 1 (Spruce): `new ty(true)` (WorldGenTaiga2).
   - Type 2 (Birch): `new jp(true)` (WorldGenForestTree).
   - Type 0 (Oak): `new gq(true)`, OR `new yd(true)` if `rand.nextInt(10) == 0` (big oak, 10% chance).
4. Attempt `generator.a(world, rand, x, y, z)`.
5. If returns `false` (placement failed): restore sapling `world.b(x, y, z, bM, treeType)`.

---

## 3. BlockMushroom — `js`

### 3.1 Block IDs and Hierarchy

`js` extends `wg`.

| Block | ID |
|---|---|
| Brown Mushroom (`yy.af`) | 39 |
| Red Mushroom (`yy.ag`) | 40 |

### 3.2 Hitbox

`(0.3, 0.0, 0.3, 0.7, 0.4, 0.7)` — `0.5 ± 0.2` wide, `0.2 × 2 = 0.4` tall.

### 3.3 Valid Placement — `d(int blockId)` override

Returns `yy.m[blockId]` — any opaque/solid block. Mushrooms can grow on any opaque surface.

### 3.4 Survival — `e(World, x, y, z)` override

Mushroom survives if:
1. `y >= 0 && y < world.height`.
2. Block below is mycelium (`yy.by.bM`), **OR** (light level < 13 AND block below is any solid `yy.m[]` block).

Mycelium always supports mushrooms regardless of light. Solid blocks support them only in dim light (< 13).

### 3.5 Random Tick — 1/25 Spread

Procedure:
1. 1/25 probability check.
2. **Density limit:** Count all instances of this mushroom ID within a 4-block horizontal
   radius ±1 Y. If count reaches 5 (decrements from 5): abort spread.
3. **Spread attempt:** Pick random target: `(x + rand(3)-1, y + rand(2)-rand(2), z + rand(3)-1)`.
4. Repeat up to 4 random walk steps.
5. Final step: if target is air (`world.h()`) AND `e(world, target)` passes: place mushroom there.

### 3.6 Huge Mushroom Growth — `c(World, x, y, z, Random)` (bonemeal-triggered)

1. Replace self with air: `world.d(x, y, z, 0)`.
2. Create appropriate generator:
   - `bM == yy.af.bM` (brown): `new acp(0)` (WorldGenHugeMushroom type 0).
   - `bM == yy.ag.bM` (red): `new acp(1)` (WorldGenHugeMushroom type 1).
3. If generation fails: restore mushroom block.

---

## 4. BlockReed — `md`

### 4.1 Block ID and Hierarchy

`md` extends `yy` directly (NOT `wg`). Block ID 83.

### 4.2 Properties

| Property | Value |
|---|---|
| Material | `p.j` (plant) |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 1 (cross) |
| Collision AABB | `null` |
| Tickable | `true` |
| Hitbox | `(0.125, 0.0, 0.125, 0.875, 1.0, 0.875)` — `0.5 ± 0.375`, full height |

### 4.3 Metadata

Metadata 0–15 is a growth counter. At 15, the reed grows.

### 4.4 Placement Validity `c(World, x, y, z)`

Valid placement if:
- Block directly below is also reed (`bM`) → valid at any height.
- OR:
  - Block below is grass (`yy.u`), dirt (`yy.v`), or sand (`yy.E`).
  - AND one of the four horizontally adjacent blocks at `y-1` has material `p.g` (water).

### 4.5 Random Tick

1. If space above is replaceable (`world.h(x, y+1, z)`):
2. Count how many consecutive reed blocks are directly below: `var6 = 1, 2, 3...` while `world.a(x, y-var6, z) == bM`.
3. If `var6 < 3` (height limit not reached):
   - If `meta == 15`: place new reed above (`world.g(x, y+1, z, bM)`), reset meta to 0.
   - Else: increment meta by 1 (`world.f(x, y, z, meta+1)`).

Reed grows max 3 blocks tall. Max height enforced by counting from current position downward.

### 4.6 Neighbor Update `a(World, x, y, z, fromFace)`

Calls `f(world, x, y, z)` → if `!e(world, x, y, z)` (can't stay): drop and remove.

### 4.7 Drops

`a(int, Random, int)` → `acy.aI.bM` = **Sugar Cane** item.

---

## 5. BlockNetherWart — `vy`

### 5.1 Block ID and Hierarchy

`vy` extends `wg`. Block ID 115.

### 5.2 Hitbox

`(0.0, 0.0, 0.0, 1.0, 0.25, 1.0)` — full width, 0.25 tall (flat cluster).

### 5.3 Valid Soil — `d(int blockId)` override

Returns `blockId == yy.bc.bM` — only **soul sand** (ID 88).

### 5.4 Metadata (Growth Stages)

| Stage | Texture | Description |
|---|---|---|
| 0 | `bL` | Young |
| 1 | `bL + 1` | Intermediate |
| 2 | `bL + 1` | Intermediate (same texture) |
| 3 | `bL + 2` | Ripe / mature |

Texture selection: `a(int, int meta)`:
- `meta >= 3` → `bL + 2`.
- `meta > 0` → `bL + 1`.
- `meta == 0` → `bL`.

### 5.5 Random Tick

Server side. If `meta < 3`:
1. Get chunk provider `vh = world.a()`.
2. Get biome at chunk coords.
3. If biome is `av` instance (Nether biome) AND `rand.nextInt(15) == 0`:
   - Increment stage: `world.f(x, y, z, ++meta)`.
4. Call `super.a()` (survival check from `wg`).

### 5.6 Drops `a(World, x, y, z, meta, brightness, fortune)` override

Server side only:
- If `meta >= 3` (ripe):
  - Drop count = `2 + rand(3)` (2–4) plus `rand(fortune + 1)` for fortune.
- Else:
  - Drop count = 1.
- Item: `acy.bq` = Nether Wart item.

Standard `a(Random)` and `a(int, Random)` return 0 (suppressed; the fortune override handles all drops).

### 5.7 Render type

Returns 6 (crop-style render — uses single plane or specific nether wart model).

---

## 6. BlockStem — `pu`

### 6.1 Block ID and Hierarchy

`pu` extends `wg`. Two instances:
- Pumpkin stem (produce = `yy.ba`, ID pumpkin block).
- Melon stem (produce = `yy.br`, ID melon block).

### 6.2 Hitbox

Thin central post, grows taller with stage:
- Default: `(0.375, 0.0, 0.375, 0.625, 0.25, 0.625)`.
- Visual height: `(meta * 2 + 2) / 16.0` blocks (computed in `b(kq, x, y, z)`).
- At stage 0: tiny sprout. At stage 7: near full height.

### 6.3 Valid Soil — `d(int blockId)` override

Returns `blockId == yy.aA.bM` — only **farmland**.

### 6.4 Metadata

Metadata 0–7 = growth stage. Stage 7 = ready to produce.

### 6.5 Random Tick

Server side. Delegates survival check to `super.a()` first.

Growth condition: `world.n(x, y+1, z) >= 9` (light above >= 9).

If condition met: `if (rand.nextInt((int)(25.0 / fertility) + 1) == 0)`:
- If `meta < 7`: increment stage.
- If `meta == 7`:
  - Check all 4 adjacent positions at same Y for existing produce block. If any → abort.
  - Pick random direction (0–3).
  - If target position is air AND farmland directly below target: place produce block at target.

### 6.6 Fertility Formula `j(World, x, y, z)`

Returns a float representing soil fertility (higher = faster growth):

1. Base = 1.0.
2. Sample 3×3 area at `y-1` (centered on stem):
   - Farmland = 1.0; hydrated farmland (metadata > 0) = 3.0.
   - Diagonal cells: divide by 4. Non-diagonal (same row/col but not center): full value.
   - Center cell (under stem): full value.
3. Add all sampled values to base.
4. If adjacent stems in both axes (N+S AND E+W) OR diagonal stems: divide result by 2.

Typical solo stem on hydrated farmland: fertility ≈ 1 + 3 = 4 → chance `rand(7+1)==0` → ~12.5% per tick.

### 6.7 Drops `a(World, x, y, z, meta, brightness, fortune)` override

Server side. Drop 1–3 seeds:
- If `world.w.nextInt(15) <= meta` (up to 3 times per break): drop one seed item.
- Item: `acy.bf` (pumpkin seeds) or `acy.bg` (melon seeds) depending on produce block.

Standard `a(int, Random, int)` returns -1 (no item from this path).

### 6.8 Stem Color

`c(int stage)` returns packed RGB:
```
red   = stage * 32
green = 255 - stage * 8
blue  = stage * 4
```
Young stems (stage 0) are green (0, 255, 0). Mature stems (stage 7) shift toward yellow (224, 199, 28).

### 6.9 Render Type

Returns 19 (stem renderer — uses special model, not cross).

### 6.10 Direction Result `c(kq, x, y, z)`

For rendering, returns which adjacent direction the mature stem has a connected produce block:
- `< 0` → no connection (not yet produced).
- `0` → west (-X).
- `1` → east (+X).
- `2` → north (-Z).
- `3` → south (+Z).

---

## 7. Open Questions

| # | Question |
|---|---|
| 7.1 | `yy.E` (sand block) — confirm ID 12 for reed placement check. |
| 7.2 | `yy.bc` (soul sand) — confirm ID 88. |
| 7.3 | `yy.af` (brown mushroom) / `yy.ag` (red mushroom) — confirm IDs 39 and 40. |
| 7.4 | `yy.by` (mycelium) — confirm ID 110. |
| 7.5 | `acy.bq` — confirm this is Nether Wart item (ID 372). |
| 7.6 | `acy.aI` — confirm this is Sugar Cane item (ID 338). |
| 7.7 | `world.n(x, y, z)` — is this total light level (includes both sky and block light)? |
| 7.8 | Sapling `ty`, `jp`, `gq`, `yd` — confirm class mappings: ty=WorldGenTaiga2, jp=WorldGenBirch, gq=WorldGenTrees (oak), yd=WorldGenBigTree. |
