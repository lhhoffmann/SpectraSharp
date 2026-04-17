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

# BlockGrassPlant Spec (canBlockStay base class)
**Source class:** `wg.java` (BlockGrassPlant — abstract base for decorative plants)
**Superclass:** `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`wg` is the abstract base class for decorative plant blocks that sit on the ground
(tall grass, dead bush, ferns, etc.). It enforces a soil validity check and a light/sky
requirement. If either check fails, the plant removes itself (dropping items).

Subclasses that use `wg`:
- Tall Grass, Dead Bush, Fern
- Wild wheat / flowers may share this base (verify in subclass specs)

---

## 2. Material and Properties

| Property | Value |
|---|---|
| Material | `p.j` (plant material) |
| Tickable | `true` |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 1 (cross / X-shape) |
| Collision AABB | `null` |

**Hitbox (visual):** `(0.3, 0.0, 0.3, 0.7, 0.6, 0.7)` (0.4 wide, 0.6 tall, centered).

---

## 3. Constructor

```
wg(int blockId, int textureIndex) {
    super(blockId, p.j);
    bL = textureIndex;
    b(true);          // tickable
    float var3 = 0.2F;
    a(0.5F - var3, 0.0F, 0.5F - var3, 0.5F + var3, var3 * 3.0F, 0.5F + var3);
    // = a(0.3, 0.0, 0.3, 0.7, 0.6, 0.7)
}
```

---

## 4. Valid Soil — `d(int blockId)`

The plant can only stand on these block IDs:
- `yy.u.bM` — Grass block (ID 2)
- `yy.v.bM` — Dirt (ID 3)
- `yy.aA.bM` — Farmland (ID 60) *or whichever block `aA` maps to*

Returns `true` if `blockId` equals any of these three.

---

## 5. Placement Validity — `c(World, x, y, z)`

Returns `true` if:
1. `super.c(world, x, y, z)` returns `true` (base check — not occupied, etc.).
2. `d(world.a(x, y-1, z))` returns `true` (block below is valid soil).

---

## 6. Survival Check — `e(World, x, y, z)` (canBlockStay)

Returns `true` if:
1. `world.m(x, y, z) >= 8` (light level at position is at least 8), **OR** `world.l(x, y, z)` (sky is visible directly above — sky light check).
2. `d(world.a(x, y-1, z))` returns `true` (valid soil below).

Both conditions must be true (AND).

---

## 7. Removal Logic — `h(World, x, y, z)`

Called internally when checking survival.

1. Call `e(world, x, y, z)`.
2. If returns `false`:
   - Drop block via `b(world, x, y, z, world.d(x,y,z), 0)` (triggers drop logic).
   - Set block to air: `world.g(x, y, z, 0)`.

---

## 8. Triggers for Survival Check

**Neighbor update `a(World, x, y, z, fromFace)`:**
- Calls `super.a()` first, then `h()`.

**Random tick `a(World, x, y, z, Random)`:**
- Calls `h()`.

Both triggers result in the same removal logic if the plant can no longer survive.

---

## 9. Quirks

**Quirk 9.1 — Light check is OR:**
The plant survives if EITHER the block light level is >= 8, OR the sky is directly visible.
Covered areas with torches (light >= 8) are valid; open sky is always valid regardless of
light level. Dark caves with no sky and no torches will remove the plant.

**Quirk 9.2 — Farmland as valid soil:**
Plants can be placed on farmland (not just grass/dirt). This allows decorative plants
to exist in tilled fields.

---

## 10. Open Questions

| # | Question |
|---|---|
| 10.1 | `yy.aA` — confirm this is farmland (ID 60) or mycelium (ID 110). |
| 10.2 | `world.m(x, y, z)` — is this total light level (torch + sky combined)? |
| 10.3 | `world.l(x, y, z)` — is this "can see sky directly" (sky light = 15)? |
| 10.4 | Which subclasses extend `wg` beyond Tall Grass? Dead Bush uses different soil check? |
