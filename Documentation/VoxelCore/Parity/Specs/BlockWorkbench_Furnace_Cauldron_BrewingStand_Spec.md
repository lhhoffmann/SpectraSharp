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

# BlockWorkbench, BlockFurnace, BlockCauldron, BlockBrewingStand Spec
**Source classes:** `rn.java` (BlockWorkbench), `eu.java` (BlockFurnace), `ic.java` (BlockCauldron), `ahp.java` (BlockBrewingStand)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. BlockWorkbench — `rn`

### 1.1 Class Hierarchy

`rn` extends `yy` directly. **No TileEntity.** Block ID 58.

### 1.2 Material and Properties

| Property | Value |
|---|---|
| Material | `p.d` (wood) |
| Texture index | 59 |

### 1.3 Texture Per Face — `b(int face)`

| Face | Texture | Description |
|---|---|---|
| 0 (bottom) | `yy.x.b(0)` | Oak planks bottom face |
| 1 (top) | `bL - 16` = 43 | Workbench top (grid pattern) |
| 2 (south) | `bL + 1` = 60 | Side with tool art |
| 3 (north) | `bL` = 59 | Blank side |
| 4 (west) | `bL` = 59 | Blank side |
| 5 (east) | `bL + 1` = 60 | Side with tool art |

### 1.4 Right-Click `a(World, x, y, z, Player)`

Server side:
- `player.a(x, y, z)` — opens the 3×3 crafting grid GUI for this block position.
- Returns `true`.

No TileEntity interaction; the crafting grid is purely client/player-side state.

---

## 2. BlockFurnace — `eu`

### 2.1 Class Hierarchy

`eu` extends `ba` (BlockContainer). Two block instances:
- `yy.aB` = Furnace (unlit, `cb = false`).
- `yy.aC` = Furnace active (lit, `cb = true`).

### 2.2 Material and Properties

| Property | Value |
|---|---|
| Material | `p.e` (stone) |
| Texture index | 45 |
| TileEntity class | `oe` (TileEntityFurnace) |

### 2.3 Metadata — Facing Direction

Same encoding as chest:
| Value | Facing |
|---|---|
| 2 | North (-Z) |
| 3 | South (+Z) |
| 4 | West (-X) |
| 5 | East (+X) |

### 2.4 Placement `a(World, x, y, z, LivingEntity placer)`

Computes direction from placer yaw:
- `dir == 0` → metadata 2
- `dir == 1` → metadata 5
- `dir == 2` → metadata 3
- `dir == 3` → metadata 4

### 2.5 Texture Per Face — `a(kq, x, y, z, face)` and `b(face)`

| Face | Unlit (cb=false) | Lit (cb=true) |
|---|---|---|
| 0 (bottom) | `bL + 17` = 62 | `bL + 17` = 62 |
| 1 (top) | `bL + 17` = 62 | `bL + 17` = 62 |
| Front (facing) | `bL - 1` = 44 (dark) | `bL + 16` = 61 (glowing) |
| Other sides | `bL` = 45 | `bL` = 45 |

The front face is determined by `face == meta`. For the `b(face)` static override (facing=3):
`bL - 1` (default unlit facing) or 45 (other sides).

### 2.6 Right-Click `a(World, x, y, z, Player)`

Server side:
- Get `oe` TileEntity via `world.b(x, y, z)`.
- Call `player.a(oe)` — opens furnace GUI.

### 2.7 Lit/Unlit Transition — `a(boolean isLit, World, x, y, z)` (static)

When the furnace changes state (TileEntity tick logic triggers this):
1. Read current facing metadata.
2. Get current TileEntity reference.
3. Set `cc = true` (prevents break-drop during block swap).
4. Replace block:
   - `isLit == true` → set block to `yy.aC.bM` (lit furnace).
   - `isLit == false` → set block to `yy.aB.bM` (unlit furnace).
5. `cc = false`.
6. Restore facing metadata.
7. Re-attach TileEntity.

### 2.8 Random Tick (Lit Furnace Only — `b(World, x, y, z, Random)`)

If `cb == true` (lit instance):
- Emit `"smoke"` and `"flame"` particles at the front face position.
- Offset direction depends on facing metadata (2,3,4,5).

### 2.9 Break Logic `d(World, x, y, z)`

`cc` guard prevents drop during lit→unlit transition swap.
If `cc == false`: scatter inventory items from TileEntity `oe`, then drop furnace block.
Always drops `yy.aB.bM` item (unlit furnace) regardless of lit/unlit state.

---

## 3. BlockCauldron — `ic`

### 3.1 Class Hierarchy

`ic` extends `yy` directly. **No TileEntity.** Block ID 118.

### 3.2 Material and Properties

| Property | Value |
|---|---|
| Material | `p.f` (iron) |
| Texture index | 154 |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 24 |

### 3.3 Texture Per Face — `a(int, int meta)`

| Face | Texture |
|---|---|
| 0 (bottom) | 155 |
| 1 (top) | 138 |
| 2-5 (sides) | 154 |

### 3.4 Metadata — Water Level

| Value | State |
|---|---|
| 0 | Empty |
| 1 | One-third full |
| 2 | Two-thirds full |
| 3 | Full |

### 3.5 AABB (Composite)

The `a(World, x, y, z, AABB, list)` method adds five sub-AABBs:
1. Bottom slab: `(0.0, 0.0, 0.0, 1.0, 0.3125, 1.0)`.
2. West wall: `(0.0, 0.0, 0.0, 0.125, 1.0, 1.0)`.
3. South wall (near): `(0.0, 0.0, 0.0, 1.0, 1.0, 0.125)`.
4. East wall: `(0.875, 0.0, 0.0, 1.0, 1.0, 1.0)`.
5. North wall (far): `(0.0, 0.0, 0.875, 1.0, 1.0, 1.0)`.

The `e()` default AABB resets to full block `(0,0,0,1,1,1)`.

### 3.6 Right-Click `a(World, x, y, z, Player)`

Server side. Read held item `var6 = player.inventory.getCurrentItem()`:

**Water Bucket (`acy.aw`):**
- If current level < 3: replace held item with empty bucket (`acy.av`), set level to 3.
- Always returns `true`.

**Glass Bottle (with water, `acy.bs`):**
- If current level > 0:
  - Add water bottle (`acy.br`) to player inventory, or drop it.
  - Decrement `var6.a` (bottle count). If empty: set slot to null.
  - Set level to `level - 1`.
- Always returns `true`.

Any other item: returns `true` (no action, click consumed).

### 3.7 Drops

`a(int, Random, int)` → `acy.by.bM` = cauldron item.

---

## 4. BlockBrewingStand — `ahp`

### 4.1 Class Hierarchy

`ahp` extends `ba` (BlockContainer). Block ID 117.

### 4.2 Material and Properties

| Property | Value |
|---|---|
| Material | `p.f` (iron) |
| Texture index | 157 |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 25 |
| TileEntity class | `tt` (TileEntityBrewingStand) |

### 4.3 AABB (Composite)

`a(World, x, y, z, AABB, list)`:
1. Central rod: `(0.4375, 0.0, 0.4375, 0.5625, 0.875, 0.5625)`.
2. Base slab: `(0.0, 0.0, 0.0, 1.0, 0.125, 1.0)`.

`e()` default = full block.

### 4.4 Right-Click `a(World, x, y, z, Player)`

Server side:
- Get `tt` TileEntity via `world.b(x, y, z)`.
- Call `player.a(tt)` — opens brewing stand GUI.
- Returns `true`.

### 4.5 Random Tick — Smoke Particle

`b(World, x, y, z, Random)`:
- Emit `"smoke"` particle at random position above the stand:
  `(x + 0.4 + rand*0.2, y + 0.7 + rand*0.3, z + 0.4 + rand*0.2)`.

### 4.6 Break Logic `d(World, x, y, z)`

If TileEntity `tt` is not null: scatter all items with Gaussian velocity (same pattern as
furnace/chest scatter).
Then call `super.d()`.

### 4.7 Drops

`a(int, Random, int)` → `acy.bx.bM` = brewing stand item.

---

## 5. Open Questions

| # | Question |
|---|---|
| 5.1 | BlockWorkbench ID 58 — confirm from `yy` static initializer. |
| 5.2 | `player.a(x, y, z)` — is this the method that opens the crafting grid at a position? |
| 5.3 | Furnace facing: does player face toward or away from the furnace? Confirm placement math from eu.java. |
| 5.4 | `acy.aw` = Water Bucket, `acy.av` = Bucket (empty), `acy.bs` = Glass Bottle, `acy.br` = Water Bottle — confirm IDs. |
| 5.5 | BlockCauldron ID 118, BlockBrewingStand ID 117 — confirm. |
| 5.6 | TileEntityBrewingStand (`tt`) — slot layout: ingredient, bottle 1, bottle 2, bottle 3? Separate spec needed. |
