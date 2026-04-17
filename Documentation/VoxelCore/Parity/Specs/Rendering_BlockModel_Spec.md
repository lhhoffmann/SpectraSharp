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

# Rendering — Block Model Spec (RenderBlocks Render Types)
**Source class:** `acr.java` (RenderBlocks — client-side renderer)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Overview

`acr` (RenderBlocks) is the client-side class responsible for rendering all block geometry.
The entry point `acr.b(yy block, int x, int y, int z)` dispatches to per-type methods
based on `block.c()` — the block's render type integer.

The render type system allows blocks to share geometry code: dozens of block IDs can
use the same render type without duplicating code.

---

## 2. Render Type Dispatch — `acr.b(yy, int, int, int)`

```
int renderType = block.c();   // Block.getRenderType()
switch (renderType):
    0  → o()  full cube
    1  → j()  cross/X sprite
    2  → c()  torch
    3  → f()  fire
    4  → n()  fluid (liquid)
    5  → g()  redstone wire
    6  → l()  crops
    7  → r()  door
    8  → h()  ladder
    9  → a(afr)  rail
   10  → q()  stairs
   11  → a(nz)  fence
   12  → e()  lever
   13  → p()  cactus
   14  → t()  bed
   15  → u()  vine
   16  → b(yy,x,y,z,false)  repeater unpowered
   17  → c(yy,x,y,z,true)   repeater powered (same method, flag=true)
   18  → a(uh)  pane/iron bars
   19  → k()  lilypad
   20  → i()  cauldron
   21  → a(fp)  fence gate
   23  → m()  dragon egg
   24  → a(ic)  cocoa pod
   25  → a(ahp)  end portal frame (with/without eye)
   26  → s()  sleeping/enchantment-table-style slab
   27  → a(aci)  activator/detector rail (variant of rail)
   other → return false (not rendered)
```

> Render type 22 is not present in the dispatch chain — either unused in 1.0 or an internal type.

---

## 3. Render Type Reference Table

| Type | Method | Description | Example blocks |
|---|---|---|---|
| 0 | `o()` | Full opaque cube — 6 axis-aligned faces | Stone, dirt, sand, wood, cobblestone, wool, glass (no tinting), planks, ores |
| 1 | `j()` | Cross / X sprite — two quads crossed at 45°, no collision face culling | Sapling, flower, rose, mushroom, tall grass, dead bush, crops (small stages), wheat (early), torch flame |
| 2 | `c()` | Torch — upright (top face only) or leaning 4 directions (meta 1-4) | Torch (50), Redstone torch (76/75) |
| 3 | `f()` | Fire — animated multi-face; generated from adjacent solid faces | Fire (51) |
| 4 | `n()` | Fluid — variable height, animated flow/still textures | Water (8/9), Lava (10/11) |
| 5 | `g()` | Redstone wire — flat quads, 4 connection arms; colour from signal strength | Redstone (55) |
| 6 | `l()` | Crops — hash-pattern (#) of 4 quads per block; denser than cross | Wheat (59) at all stages |
| 7 | `r()` | Door — thin panel (3/16 thick) with UV rotation based on facing/open | Oak door (64), Iron door (71) |
| 8 | `h()` | Ladder — flat face quad pressed against a wall | Ladder (65) |
| 9 | `a(afr)` | Rail — flat ground quad; curved variants from metadata | Rail (66) |
| 10 | `q()` | Stairs — two AABB slabs: lower half + back upper quarter | Oak stairs (53), Cobblestone stairs (67) |
| 11 | `a(nz)` | Fence — post core + dynamic arms toward adjacent fences/blocks | Fence (85), Nether Brick Fence (113) |
| 12 | `e()` | Lever — stick + cobblestone base; 2 orientations | Lever (69) |
| 13 | `p()` | Cactus — inset faces (2/16 gap on all sides) | Cactus (81) |
| 14 | `t()` | Bed — two-piece low (9/16 tall) with foot/head orientation | Bed (26) |
| 15 | `u()` | Vine — flat quads pressed against each attached wall face (N/S/E/W/top) | Vine (106) |
| 16 | `b(…,false)` | Redstone repeater unpowered — flat base + two mini-torches | Unpowered repeater (93) |
| 17 | `c(…,true)` | Redstone repeater powered — same shape, powered state | Powered repeater (94) |
| 18 | `a(uh)` | Glass pane / iron bars — 2/16-thick post + arms | Glass Pane (102), Iron Bars (101) |
| 19 | `k()` | Lilypad — flat 1/64 quad on water surface; slightly raised | Lilypad (111) |
| 20 | `i()` | Cauldron — hollow vessel; outer walls + inner cavity | Cauldron (118) |
| 21 | `a(fp)` | Fence gate — two pillar posts + thin centre panel; open or closed | Fence Gate (107) |
| 22 | — | Not dispatched in 1.0; possibly unused slot | — |
| 23 | `m()` | Dragon egg — stepped pyramid shape (stacked decreasing cubes) | Dragon Egg (122) |
| 24 | `a(ic)` | Cocoa pod — curved bump attached to log face; 3 size stages | Cocoa (127) |
| 25 | `a(ahp)` | End Portal Frame — flat base (0.875 tall) + optional eye inset | End Portal Frame (120) |
| 26 | `s()` | Flat slab with optional extended piece — used for enchantment table | Enchantment Table (116) |
| 27 | `a(aci)` | Rail variant — detector/activator rail geometry | Powered Rail (27), Detector Rail (28) |

---

## 4. Full Cube (Type 0) — `o()`

The default render. For each of the 6 faces (bottom, top, north, south, west, east):
1. Check if adjacent block is opaque. If opaque, skip face (culling).
2. Calculate ambient occlusion per vertex (using 3 neighbours per corner).
3. Determine brightness from adjacent sky/block light.
4. Render quad with texture from `block.getBlockTextureFromSideAndMetadata(face, meta)`.

Light multipliers per face (approximate vanilla):
- Top: 1.0
- Bottom: 0.5
- North/South: 0.8
- East/West: 0.6

---

## 5. Cross Sprite (Type 1) — `j()`

Two quads at ±45° through the block centre:
- Quad 1: from (0.15, 0, 0.85) to (0.85, 1, 0.15) — SW–NE diagonal.
- Quad 2: from (0.15, 0, 0.15) to (0.85, 1, 0.85) — NW–SE diagonal.
- Both quads are double-sided (rendered from both directions).
- No face culling.
- Texture: full terrain atlas tile.
- No ambient occlusion.

---

## 6. Torch (Type 2) — `c()`

Upright torch (meta 5) or leaning torch (meta 1–4 = south/north/west/east):

- Upright: `a = 7/16`, `b = 0`, flame at top.
- Leaning: tilted 2/16 toward the wall face; base presses against wall.
- Render as: 4 narrow quads (the stick) + 1 top quad (flame texture).

---

## 7. Fluid (Type 4) — `n()`

Variable-height liquid:
- Top height = fluid level; level 0 = full (1.0), level 7 = lowest (source = still texture; flowing = animated flow texture).
- 5 quads: 4 sides + 1 top. Bottom never rendered (always against solid).
- Top face uses still texture when source block; flowing texture when level > 0.
- Sides are skewed to match the height gradient toward neighbours.

---

## 8. Pane / Iron Bars (Type 18) — `a(uh)`

Thin connectivity geometry:
- Centre post: 2/16 × 16/16 × 2/16 (pillar at block centre).
- Arms: extend to full block edge in each connected direction; 2/16 thick.
- Connected to: same pane/bar block adjacent, OR any solid opaque block adjacent.
- Glass pane connects to glass blocks (same material).
- Full-height (1.0F), unlike fence (1.5F).

---

## 9. TileEntitySpecialRenderers (TESR)

Some blocks bypass the `acr` system entirely and use dedicated renderers in the `ItemRenderer` or entity-style paths:

| Block | Description |
|---|---|
| Chest (ID 54) | Lid animation driven by `TileEntityChest.lidAngle` — rendered as entity model |
| Enchantment Table (ID 116) | Floating book TESR — book position/rotation animated per tick |
| Sign (ID 63/68) | Text rendered via font renderer in 3D; sign post/wall model is static |
| Skull / Head (ID 144) | Entity model for 5 skull types — not in 1.0 (added 1.4) |

In 1.0 the chest is confirmed to use a TESR for the lid open animation (the `numPlayersUsing` field in `TileEntityChest` drives the `lidAngle`).

---

## 10. `Block.c()` — getRenderType Return Values

Each `yy` block subclass overrides `c()` to return its render type integer. The default in `yy` (Block base) returns `0` (full cube).

Blocks with non-zero render types (selected examples):

| Render type | Block class | Block ID(s) |
|---|---|---|
| 1 | `ack` (BlockFlower/Sapling) | 6, 37, 38, 39, 40 |
| 1 | `aqj` (BlockTallGrass) | 31 |
| 2 | `bq` (BlockTorch) | 50, 75, 76 |
| 3 | `sf` (BlockFire) | 51 |
| 4 | `km` (BlockFluid) | 8, 9, 10, 11 |
| 5 | `mz` (BlockRedstoneWire) | 55 |
| 6 | `yx` (BlockCrops) | 59 |
| 7 | `rl` (BlockDoor) | 64, 71 |
| 8 | `io` (BlockLadder) | 65 |
| 9 | `afr` (BlockRail) | 66 |
| 10 | `ais` (BlockStairs) | 53, 67 |
| 11 | `nz` (BlockFence) | 85, 113 |
| 12 | `ab` (BlockLever) | 69 |
| 13 | `ag` (BlockCactus) | 81 |
| 14 | `rl` subclass (BlockBed) | 26 |
| 15 | `ahl` (BlockVine) | 106 |
| 18 | `uh` (BlockPane) | 101, 102 |
| 20 | BlockCauldron | 118 |
| 21 | `fp` (BlockFenceGate) | 107 |

---

## 11. Open Questions

| # | Question |
|---|---|
| 11.1 | Render type 22 — is it used anywhere in 1.0? What does it render? |
| 11.2 | Render type 27 (`a(aci)`) — confirm `aci` = powered/detector rail variant. Is it distinct from plain rail type 9? |
| 11.3 | `s()` (type 26) — besides enchantment table, is this used for any other block? What is the "extended piece" that type 26 adds? |
| 11.4 | Chest TESR: does the chest model file live in a resource pack? Or is it a hardcoded tessellator path? |
| 11.5 | Ambient occlusion: is AO enabled by default in 1.0, or is it the "Fancy" graphics option toggle? |
| 11.6 | `acr.d[256]` (boolean array) — is this the "override AO per block" table? What populates it? |
