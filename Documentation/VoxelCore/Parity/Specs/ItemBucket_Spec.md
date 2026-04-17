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

# ItemBucket Spec
**Source class:** `en.java` (ItemBucket), `om.java` (ItemMilkBucket)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

`en` extends `acy` (Item). Stack size forced to 1 (`bN = 1`).

Field `a` (int) encodes the liquid type:
- `a == 0` — empty bucket.
- `a > 0` — block ID of the liquid to place (still water = 9, still lava = 11).
- `a < 0` — special empty-and-do-nothing branch (dead code in practice).

Milk bucket uses a separate class `om` (no `en` subtype).

---

## 2. Item Instances

| Field | Constructor | Item ID | Description |
|---|---|---|---|
| `acy.av` | `new en(69, 0)` | 325 | Empty bucket |
| `acy.aw` | `new en(?, 8 or 9)` | 326 | Water bucket (still water block ID) |
| `acy.ax` | `new en(?, 10 or 11)` | 327 | Lava bucket (still lava block ID) |
| `acy.aF` | `new om(79)` | 335 | Milk bucket (`om.java`) |

> Open Question 5.1: Confirm exact constructor args for `acy.aw` and `acy.ax` — static water block ID is 9, still lava is 11 (flowing = 8/10).

---

## 3. Right-Click Logic — `c(dk itemStack, ry world, vi player)`

Performs a ray-cast (`this.a(world, player, allowWater)` with `allowWater = (this.a == 0)`)
to get a `gv` hit result (`var12`).

### 3.1 Hit Type: Block (`var12.a == bo.a`)

**Empty bucket (`this.a == 0`):**

1. Check permission: `world.a(player, x, y, z)` (interact permission).
2. If hit block has material `p.g` (water) AND `world.d(x, y, z) == 0` (still water, meta 0):
   - Remove the block: `world.g(x, y, z, 0)`.
   - Creative mode: return unchanged item.
   - Return water bucket (`acy.aw`).
3. Else if hit block has material `p.h` (lava) AND meta == 0 (still lava):
   - Remove the block: `world.g(x, y, z, 0)`.
   - Creative mode: return unchanged item.
   - Return lava bucket (`acy.ax`).

**Full bucket (`this.a > 0`):**

1. If `this.a < 0`: return empty bucket immediately (dead branch in normal play).
2. Adjust target position by hit face offset (face 0→y-1, 1→y+1, 2→z-1, 3→z+1, 4→x-1, 5→x+1).
3. Check player permission at the adjusted position.
4. If adjusted position is passable (`world.h()`) OR is a non-solid block (`!world.e().b()`):
   - **Server side, placing lava in water context** (`world.y.d` AND `this.a == yy.A.bM`):
     - Play sound `"random.fizz"` at offset center.
     - Spawn 8 `"largesmoke"` particles.
   - **Normal placement:**
     - Place block: `world.d(x, y, z, this.a, 0)`.
   - Creative mode: return unchanged item.
   - Return empty bucket (`acy.av`).

### 3.2 Hit Type: Entity (`var12.g instanceof adr`)

Only checked when bucket is **empty** (`this.a == 0`):
- Entity class `adr` = EntityCow.
- Returns milk bucket (`acy.aF`).

---

## 4. Milk Bucket — `om.java`

`om` is a separate class (not a subtype of `en`). Item ID 335.

On drink (`onItemRightClick` equivalent):
- Clears all active potion effects from the player.
- Heals player (exact amount: see open question).
- Returns empty bucket (`acy.av`).

> Open Question 5.2: Does milk remove ALL active potion effects, or only harmful ones? Check `om.java` and `nq.clearEffects()`.

---

## 5. Open Questions

| # | Question |
|---|---|
| 5.1 | Exact constructor args for water/lava bucket `en` instances — confirm still water = block 9, still lava = block 11. |
| 5.2 | Milk bucket (`om.java`) exact effect: does it heal HP in addition to clearing effects? |
| 5.3 | Can dispensers dispense water/lava buckets in 1.0? Confirm `BlockDispenser` handler. |
| 5.4 | The `this.a < 0` branch — is this ever reachable in 1.0 builds, or is it truly dead code? |
