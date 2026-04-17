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

# ItemSign Spec
**Source class:** `my.java`
**Item ID:** 323 (constructor arg 67 → 256+67 = 323)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

`my` extends `acy` (Item). Stack size: `bN = 1`.

---

## 2. Block Types

| Block | `yy` field | ID | Description |
|---|---|---|---|
| Floor sign | `yy.aD` | 63 | Standing sign post; metadata = 0–15 yaw |
| Wall sign | `yy.aI` | 68 | Sign attached to wall; metadata = face 2–5 |

---

## 3. `OnItemUse` — `a(dk itemStack, vi player, ry world, x, y, z, face)`

### 3.1 Pre-checks

1. If `face == 0` (bottom of block clicked): return `false` — cannot place on underside.
2. If the target block is not solid (`!world.e(x, y, z).b()`): return `false`.
3. Adjust target position by face:
   - `face == 1` (top): `y++`
   - `face == 2` (south, -Z): `z--`
   - `face == 3` (north, +Z): `z++`
   - `face == 4` (west, -X): `x--`
   - `face == 5` (east, +X): `x++`
4. If adjusted position is outside player reach (`!player.e(x, y, z)`): return `false`.
5. If the target position cannot receive the sign (`!yy.aD.c(world, x, y, z)`): return `false`.

### 3.2 Placement

**Top face (`face == 1`) → floor sign `yy.aD`:**

Metadata = player yaw quantised to 16 steps:
```
int meta = me.c((double)((player.yaw + 180.0F) * 16.0F / 360.0F) + 0.5) & 15
world.d(x, y, z, yy.aD.bM, meta)
```

**Side faces (`face == 2/3/4/5`) → wall sign `yy.aI`:**

Metadata = face value directly:
```
world.d(x, y, z, yy.aI.bM, face)
```

### 3.3 Post-placement

1. Decrement item: `itemStack.a--`.
2. Get TileEntity at the placed position: `u te = (u) world.b(x, y, z)`.
3. If TileEntity is not null: call `player.a(te)` — opens the sign-editing GUI.
4. Return `true`.

---

## 4. Yaw → Metadata Mapping (Floor Signs)

```
meta = floor((yaw + 180) * 16 / 360 + 0.5) & 15
```

| Meta | Facing direction |
|---|---|
| 0 | South (+Z, player facing north) |
| 4 | West (-X) |
| 8 | North (-Z) |
| 12 | East (+X) |

16 steps of 22.5° each.

---

## 5. TileEntity — `u` (TileEntitySign)

- 4 lines of text, each max 15 characters.
- NBT: `Text1`–`Text4` (string tags).
- The sign editing GUI opens immediately after placement.

---

## 6. Open Questions

| # | Question |
|---|---|
| 6.1 | Stack size: `bN = 1` observed in constructor — confirm. Vanilla 1.0 signs stack to 1? |
| 6.2 | `u` = TileEntitySign — confirm obfuscated class name. |
| 6.3 | Sign editing GUI: is it client-side only? Is there a server packet to open it? |
| 6.4 | `yy.aD` = floor sign ID 63 and `yy.aI` = wall sign ID 68 — confirm both IDs. |
| 6.5 | `me.c()` — is this `MathHelper.floor_double()`? Confirm method identity. |
