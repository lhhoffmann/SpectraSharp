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

# EntityPainting Spec
**Source classes:** `tj.java` (EntityPainting), `sv.java` (EnumArt)
**Superclass:** `ia` (Entity — NOT LivingEntity)
**EntityList string:** `"Painting"` — integer ID **9**
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`tj` represents a decorative painting entity placed on a wall. It is not a mob — it extends
`ia` (base Entity) directly and has no health, AI, or physics. A painting validates its
placement once per 100 ticks and removes itself if the wall behind it disappears, dropping
a painting item.

---

## 2. Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | 0 | Facing direction (0=south, 1=west, 2=north, 3=east) |
| `b` | `int` | — | Tile X of anchor block |
| `c` | `int` | — | Tile Y of anchor block |
| `d` | `int` | — | Tile Z of anchor block |
| `e` | `sv` | — | Painting variant (EnumArt enum value) |
| `f` | `int` | 0 | Validity check timer — increments each tick; validated at 100 |

Inherited fields used:
- `s/t/u` (entity position) — set by `b(int dir)` from tile + direction offset
- `C` (AABB) — set by `b(int dir)` based on painting dimensions
- `L` (eye height) — set to 0.0F (no eye offset)
- `y/A` (yaw/prevYaw) — set to `dir × 90`

---

## 3. Constants

| Value | Location | Meaning |
|---|---|---|
| `0.5F × 0.5F` | constructor | Initial AABB (reset in `b(dir)`) |
| `0.5625F` | `b(int)` | Depth offset — painting sticks out 0.5625 blocks from wall |
| `-0.00625F` | `b(int)` | Tiny AABB expansion on all sides (precision clearance) |
| `100` | tick | Placement check period (every 100 ticks) |
| `acy.ar` | drop | Item ID: Painting item |

---

## 4. EnumArt — `sv` — Painting Variants

Each enum constant stores: name string `A`, width pixels `B`, height pixels `C`,
texture sheet X `D`, texture sheet Y `E`.

All widths/heights are multiples of 16 pixels = 1 block.

| Enum | Name | Width (px) | Height (px) | Tile W | Tile H | Sheet X | Sheet Y |
|---|---|---|---|---|---|---|---|
| `a` | `"Kebab"` | 16 | 16 | 1 | 1 | 0 | 0 |
| `b` | `"Aztec"` | 16 | 16 | 1 | 1 | 16 | 0 |
| `c` | `"Alban"` | 16 | 16 | 1 | 1 | 32 | 0 |
| `d` | `"Aztec2"` | 16 | 16 | 1 | 1 | 48 | 0 |
| `e` | `"Bomb"` | 16 | 16 | 1 | 1 | 64 | 0 |
| `f` | `"Plant"` | 16 | 16 | 1 | 1 | 80 | 0 |
| `g` | `"Wasteland"` | 16 | 16 | 1 | 1 | 96 | 0 |
| `h` | `"Pool"` | 32 | 16 | 2 | 1 | 0 | 32 |
| `i` | `"Courbet"` | 32 | 16 | 2 | 1 | 32 | 32 |
| `j` | `"Sea"` | 32 | 16 | 2 | 1 | 64 | 32 |
| `k` | `"Sunset"` | 32 | 16 | 2 | 1 | 96 | 32 |
| `l` | `"Creebet"` | 32 | 16 | 2 | 1 | 128 | 32 |
| `m` | `"Wanderer"` | 16 | 32 | 1 | 2 | 0 | 64 |
| `n` | `"Graham"` | 16 | 32 | 1 | 2 | 16 | 64 |
| `o` | `"Match"` | 32 | 32 | 2 | 2 | 0 | 128 |
| `p` | `"Bust"` | 32 | 32 | 2 | 2 | 32 | 128 |
| `q` | `"Stage"` | 32 | 32 | 2 | 2 | 64 | 128 |
| `r` | `"Void"` | 32 | 32 | 2 | 2 | 96 | 128 |
| `s` | `"SkullAndRoses"` | 32 | 32 | 2 | 2 | 128 | 128 |
| `t` | `"Fighters"` | 64 | 32 | 4 | 2 | 0 | 96 |
| `u` | `"Pointer"` | 64 | 64 | 4 | 4 | 0 | 192 |
| `v` | `"Pigscene"` | 64 | 64 | 4 | 4 | 64 | 192 |
| `w` | `"BurningSkull"` | 64 | 64 | 4 | 4 | 128 | 192 |
| `x` | `"Skeleton"` | 64 | 48 | 4 | 3 | 192 | 64 |
| `y` | `"DonkeyKong"` | 64 | 48 | 4 | 3 | 192 | 112 |

Total: **25 painting variants**.

`sv.z` = max name length = `"SkullAndRoses".length()` = 13 (used for NBT string buffer sizing).

---

## 5. Constructors

### `tj(World)`
Basic constructor. Sets `L = 0.0F`, hitbox `0.5 × 0.5`.

### `tj(World, int x, int y, int z, int dir)`
Placement constructor for random painting selection:
1. Sets anchor `b=x, c=y, d=z`.
2. Iterates all `sv` values; for each, temporarily sets `e = variant` and calls `b(dir)`.
3. Calls `g()` (validity check) — if valid, adds to candidate list.
4. Randomly picks one valid variant (`Y.nextInt(candidates.size())`).
5. Calls `b(dir)` with the final chosen variant.

### `tj(World, int x, int y, int z, int dir, String motiveName)`
NBT-load / specific-motif constructor:
1. Sets anchor `b=x, c=y, d=z`.
2. Iterates all `sv` values; on name match, sets `e = variant`.
3. Calls `b(dir)`.

---

## 6. Method `b(int dir)` — Position and AABB Setup

Called after `e` and anchor (`b/c/d`) are set. Computes entity position and AABB.

1. Store direction: `a = dir`. Set yaw: `y = A = dir × 90`.
2. Retrieve painting pixel dimensions: `widthPx = e.B`, `heightPx = e.C`.
3. Compute half-extents in blocks: `halfW = widthPx / 32.0F`, `halfH = heightPx / 32.0F`.
   - Special case: if `dir == 0 || dir == 2` (south/north facing), `halfW` depth = `0.5F / 32.0F`.
   - If `dir == 1 || dir == 3` (west/east facing), `halfW` depth = same override.
4. Compute center position from anchor tile:
   - Base: `(b + 0.5, c + 0.5, d + 0.5)`.
   - Offset by `0.5625F` in the facing direction (painting thickness).
   - Offset horizontally by `c(e.B)` (centering offset for 32px or 64px wide paintings: +0.5 blocks).
   - Offset vertically by `c(e.C)` (centering offset for 32px or 64px tall paintings: +0.5 blocks).
5. Call `d(posX, posY, posZ)` to set entity position.
6. Set AABB: `C.c(posX - halfW - ε, posY - halfH - ε, posZ - depth - ε,
               posX + halfW + ε, posY + halfH + ε, posZ + depth + ε)`
   where ε = -0.00625F (all sides shrunk inward by this amount).

**Helper `c(int pixels)`:**
Returns centering offset:
- `pixels == 32` → `0.5F`
- `pixels == 64` → `0.5F`
- otherwise → `0.0F`

This offsets the center so multi-block paintings align to block grid.

---

## 7. Tick Logic `a()`

1. Increment `f`.
2. If `f == 100`:
   - Reset `f = 0`.
   - If **server side** (`!o.I`): call `g()` (placement validity check).
     - If `g()` returns `false`: call `v()` (mark dead) and spawn painting item drop
       at entity position: `new ih(world, s, t, u, new dk(acy.ar))`.

---

## 8. Placement Validity — `g()`

Returns `true` if placement is valid, `false` otherwise.

**Step 1 — AABB entity collision:**
If `world.a(entity, C)` returns any entities in the AABB → return `false`.

**Step 2 — Wall blocks check:**
Compute `tileW = e.B / 16` (painting width in blocks), `tileH = e.C / 16`.
For each column `col` in `[0, tileW)` and each row `row` in `[0, tileH)`:
  - Look up block at the wall position (behind the painting, based on direction).
  - If block is not solid (`!block.b()`) → return `false`.

**Step 3 — Painting overlap check:**
Get all entities in `world.b(entity, C)`.
If any is an instance of `tj` (another painting) → return `false`.

Return `true` if all checks pass.

---

## 9. Damage Handler `a(Entity attacker, int damage)`

When any entity attacks the painting (server side, not already dead):
1. Call `v()` — mark dead.
2. Call `G()` — play break sound.
3. Spawn painting item: `new ih(world, s, t, u, new dk(acy.ar))`.

Returns `true` regardless.

---

## 10. Push / Explosion Handlers

`b(double dx, double dy, double dz)` — called on entity push:
- If server side and push is non-zero: call `v()` + drop item.

`h(double dx, double dy, double dz)` — called on explosion impulse:
- Same behaviour as push handler.

---

## 11. NBT

### Write `a(NbtCompound)`

| Key | Type | Value |
|---|---|---|
| `"Dir"` | `byte` | `a` (direction 0–3) |
| `"Motive"` | `String` | `e.A` (variant name string, e.g. `"Kebab"`) |
| `"TileX"` | `int` | `b` |
| `"TileY"` | `int` | `c` |
| `"TileZ"` | `int` | `d` |

### Read `b(NbtCompound)`

1. `a = tag.getByte("Dir")`.
2. `b = tag.getInt("TileX")`, `c = tag.getInt("TileY")`, `d = tag.getInt("TileZ")`.
3. Read `"Motive"` string; iterate `sv` values; on match, set `e = variant`.
4. If no match found: `e = sv.a` (fallback to "Kebab").
5. Call `b(a)` to reposition entity.

---

## 12. Quirks

**Quirk 12.1 — Painting hitbox is not a full block:**
The depth (0.5625 blocks) and the small negative AABB expansion (-0.00625) mean the painting
sits very slightly inside the wall. This is intentional for visual flush-mounting.

**Quirk 12.2 — Validity check is server-only and periodic:**
Paintings do not check validity every tick — only every 100 ticks on the server. If the wall
block is removed, the painting survives for up to 5 seconds before despawning.

**Quirk 12.3 — Random variant selection during placement:**
All valid variants are collected first, then one is chosen randomly. If no variant fits
(wall too small or blocked), no painting spawns (the constructor will have `e == null` after
the loop — handled by the caller checking for a valid entity).

---

## 13. Open Questions

| # | Question |
|---|---|
| 13.1 | EntityList ID for Painting: confirmed as ID 9 from `afw.java` — verify. |
| 13.2 | `world.a(entity, AABB)` vs `world.b(entity, AABB)` — what is the difference? First appears to be "get entities intersecting (solid)" and second is "get all entities". Needs confirmation. |
| 13.3 | `block.b()` in validity check — is this `isOpaqueCube()` or `isSolid()`? Paintings need a solid backing. |
