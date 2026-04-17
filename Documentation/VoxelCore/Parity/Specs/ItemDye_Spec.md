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

# ItemDye (Dye / Bonemeal) Spec
**Source class:** `xv.java`
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

`xv` extends `acy` (Item). Stack size: 64 (`a(true)` call); texture base row offset uses `i(0)`.

---

## 2. Dye Color Registry

16 metadata values (0–15). Static arrays `a[]` (name strings) and `b[]` (RGB ints):

| Meta | Name | RGB (hex) |
|---|---|---|
| 0 | black | 0x1E1B1B |
| 1 | red | 0xB3312C |
| 2 | green | 0x3B511A |
| 3 | brown | 0x51301A |
| 4 | blue | 0x253192 |
| 5 | purple | 0x7B2FBE |
| 6 | cyan | 0x287697 |
| 7 | silver | 0x287697 |
| 8 | gray | 0x434343 |
| 9 | pink | 0xD88198 |
| 10 | lime | 0x41CD34 |
| 11 | yellow | 0xDECD87 |
| 12 | lightBlue | 0x6689D3 |
| 13 | magenta | 0xC354CD |
| 14 | orange | 0xEB8844 |
| 15 | white | 0xF0F0F0 |

> Note: meta 6 (cyan) and meta 7 (silver) share the same RGB value in the source array — this appears intentional.

---

## 3. Texture Selection — `a(int meta)`

Texture index = `bO + (meta % 8) * 16 + meta / 8`.

Maps the 16 dye variants to a 2-row × 8-column section of `items.png`.

---

## 4. Item Name — `a(dk itemStack)`

Returns `super.d() + "." + a[clamp(meta, 0, 15)]`.

Example: `"item.dyePowder.white"` for meta 15.

---

## 5. `OnItemUse` — `a(dk, vi player, ry world, x, y, z, face)`

Only activates for **meta 15 (bonemeal / bone meal = white dye)**. All other meta values return `false` (no block interaction; dye is applied to entities instead — see §6).

Server side only (`!var3.I` guard).

### 5.1 Sapling (yy.y — ID 6)

- Calls `((aet)yy.y).c(world, x, y, z, world.w)` → triggers tree growth attempt.
- Decrements item count.
- Returns `true`.

### 5.2 Brown Mushroom (yy.af) / Red Mushroom (yy.ag)

- Calls `((js)yy.k[blockId]).c(world, x, y, z, world.w)` → tries to grow huge mushroom.
- Decrements item count (only if growth succeeds — see mushroom spec).
- Returns `true`.

### 5.3 Pumpkin Stem (yy.bt) / Melon Stem (yy.bs)

- Calls `((pu)yy.k[blockId]).g(world, x, y, z)` → forces stem to stage 7 and places fruit.
- Decrements item count.
- Returns `true`.

### 5.4 Wheat / Crop (yy.az — ID 59)

- Calls `((aha)yy.az).g(world, x, y, z)` → instantly grows crop to max stage.
- Decrements item count.
- Returns `true`.

### 5.5 Grass Block (yy.u — ID 2)

Scatter 128 random plants on the surface above and around the target position:

```
for (int i = 0; i < 128; i++) {
    tx = x; ty = y + 1; tz = z;
    for (int step = 0; step < i / 16; step++) {
        tx += rand(3) - 1;
        ty += (rand(3) - 1) * rand(3) / 2;
        tz += rand(3) - 1;
        if (world.a(tx, ty-1, tz) != grass OR world.g(tx, ty, tz)) break inner;
    }
    if (world.a(tx, ty, tz) == 0) {
        if (rand(10) != 0) → place tall grass (yy.X, meta 1)
        else if (rand(3) != 0) → place dandelion (yy.ad)
        else → place rose (yy.ae)
    }
}
```

Decrements item count. Returns `true`.

---

## 6. Entity Interaction — `a(dk, nq entity)`

Only activates for **entity `instanceof hm`** (EntitySheep):

```java
hm sheep = (hm) entity;
int color = fr.d_(item.i());   // convert dye meta to wool color
if (!sheep.v_() && sheep.l() != color) {
    sheep.c(color);            // set sheep wool color
    item.a--;                  // consume 1 dye
}
```

- `hm` = EntitySheep.
- `fr.d_()` = maps dye metadata → wool block metadata (color index).
- Only applies if sheep is not already sheared AND not already that color.

---

## 7. Open Questions

| # | Question |
|---|---|
| 7.1 | Confirm meta 7 = silver vs meta 7 = light gray — raw array has duplicate `2651799` for meta 6 and 7. Is this a source bug or intentional? |
| 7.2 | Does bonemeal on sapling always succeed, or is it the same 1/7 probability as random tick? |
| 7.3 | Does bonemeal on fully-grown crop (stage 7) still consume the item? |
| 7.4 | `yy.X` in grass scatter — confirm this is tall grass (meta 1) not fern. |
| 7.5 | `yy.ad` / `yy.ae` — confirm these are dandelion and rose (IDs 37/38). |
