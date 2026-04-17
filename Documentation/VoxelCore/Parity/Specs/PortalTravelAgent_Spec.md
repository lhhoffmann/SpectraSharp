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

# PortalTravelAgent Spec
**Source class:** `aim.java`
**Superclass:** none (`Object`)
**Analyst:** lhhoffmann
**Date:** 2026-04-16
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`aim` is the portal link manager. It is instantiated fresh on every portal transition
(not a singleton). Its sole responsibility is to ensure a valid portal structure exists
on the destination side of a Nether or End transition, and to place the entity at the
correct position within that structure.

Coordinate scaling between Overworld and Nether is NOT performed inside `aim` — it is
performed by the caller (`Minecraft.a(int)`) before `aim` is invoked. See §10.

---

## 2. Fields

| Field (obf) | Type    | Default          | Semantics                              |
|-------------|---------|------------------|----------------------------------------|
| `a`         | `Random`| `new Random()`   | Unseed RNG used only in `c()` for starting orientation (`nextInt(4)`) |

No world reference, no per-world state. Each instance is discarded after a single call.

---

## 3. Constants & Magic Numbers

| Value | Location | Meaning |
|-------|----------|---------|
| `128` | `b()` search radius | XZ scan range in blocks for findPortal |
| `16`  | `c()` search radius | XZ scan range in blocks for createPortal |
| `4`   | `c()` inner loop count | Number of orientation trials (0 to 3) |
| `2`   | `c()` phase-2 loop count | Number of orientation trials in fallback phase |
| `70`  | `c()` emergency Y | Minimum Y for emergency portal placement |
| `0.125` | `Minecraft.a(int)` | Overworld→Nether X/Z scale factor (= 1/8) |
| `8.0` | `Minecraft.a(int)` | Nether→Overworld X/Z scale factor |
| `5×5` | `a()` End platform | Platform footprint in XZ |
| `3`   | `a()` End clearance | Air layers above obsidian floor |
| `4×5` | `c()` frame | Portal frame footprint: 4 wide × 5 tall |
| `2×3` | `c()` interior | Portal air interior: 2 wide × 3 tall |

---

## 4. Methods — Detailed Logic

### 4.1 Method `a` (obfuscated: `a`) — Portal Placement Entry Point

```
a(world: ry, entity: ia) → void
```

**Called by:** `Minecraft.a(int)` (the dimension-change method), invoked only when the
destination dimension ID is < 1 (i.e., Overworld or Nether). For End arrival the End
branch in this same method handles it; see §4.1.1.

**Parameters:**
- `world` — the destination world (already switched to)
- `entity` — the entity being transferred (may be any `ia`, but in practice always `vi`/player)

**Dispatch logic:**

1. Check `world.y.g`:
   - If `== 1` (End dimension): execute **End Platform Placement** (§4.1.1).
   - Otherwise (Overworld `0` or Nether `-1`): execute **Nether Portal Logic** (§4.1.2).

---

#### 4.1.1 End Platform Placement

Triggered when the destination world is the End (`world.y.g == 1`).

**Variables:**
- `var3 = floor(entity.s)` — entity X, truncated to int
- `var4 = floor(entity.t) - 1` — entity Y minus 1 (floor level is one below entity feet)
- `var5 = floor(entity.u)` — entity Z, truncated to int
- `var6 = 1`, `var7 = 0` — fixed orientation vectors (always axis-aligned: X-axis)

**Platform geometry triple loop:**

Outer loops:
- `var8` iterates −2 to +2 (5 steps, the "depth" / Z axis of platform)
- `var9` iterates −2 to +2 (5 steps, the "width" / X axis of platform)
- `var10` iterates −1 to +2 (4 steps, the Y layers)

Block coordinates:
- `var11 = var3 + var9` (X)
- `var12 = var4 + var10` (Y)
- `var13 = var5 − var8` (Z)

Block placed: `world.setBlock(var11, var12, var13, blockId)` where:
- `var10 < 0` (i.e., `var10 == −1`): blockId = obsidian (`yy.ap.bM` = 49)
- `var10 >= 0` (i.e., `var10 == 0, 1, 2`): blockId = air (0) — clears any existing blocks

**Result:** A 5×5 obsidian floor at Y = `var4 − 1`, with 3 layers of air above it
(Y = `var4`, `var4+1`, `var4+2`). The footprint covers X ∈ [var3−2, var3+2],
Z ∈ [var5−2, var5+2].

**Entity repositioning:**

After placing the platform:
- `entity.setPosition(var3, var4, var5, entity.yaw, 0.0F)` — entity placed at floor level,
  pitch reset to 0
- `entity.motionX = entity.motionY = entity.motionZ = 0.0` — velocity zeroed

**End spawn coordinate context:** When entering the End from the Overworld, the caller sets
the entity's position to the End world spawn point before calling `a()`. The End spawn is
`dh(100, 50, 0)` (from `ol.g()` = `WorldProviderEnd.getSpawnPoint()`). Therefore the
platform is centred at approximately (100, 49, 0) in End coordinates.

---

#### 4.1.2 Nether Portal Logic

Triggered when destination is Overworld or Nether (not End).

Step 1: Call `b(world, entity)` (findPortal):
- If `b()` returns `true` — portal found and entity repositioned; done.
- If `b()` returns `false` — no portal found; call `c(world, entity)` (createPortal),
  then call `b(world, entity)` again to reposition entity inside the newly built portal.

---

### 4.2 Method `b` (obfuscated: `b`) — findPortal

```
b(world: ry, entity: ia) → boolean
```

**Purpose:** Scan a 256×256×128 XZ area around the entity's position for the nearest
existing portal block column. If found, reposition the entity at the portal's centre.

**Variables:**
- `var3 = 128` — search radius (short constant)
- `var4 = −1.0` — best distance-squared; negative = nothing found yet
- `var6, var7, var8` — best X, Y, Z found
- `var9 = floor(entity.s)`, `var10 = floor(entity.u)` — entity XZ as int

**Search loop:**

Outer X loop: `var11` from `var9 − 128` to `var9 + 128` inclusive.
- `var12 = (var11 + 0.5) − entity.s` (X offset double)

Outer Z loop (nested): `var14` from `var10 − 128` to `var10 + 128` inclusive.
- `var15 = (var14 + 0.5) − entity.u` (Z offset double)

Y loop (innermost): `var17` from `world.c − 1` down to `0` (top to bottom, `world.c` = 128).
- Check if `world.getBlockId(var11, var17, var14) == portalBlockId` (ID 90).
  - If yes: walk `var17` downward while the block one below is also portal ID.
    This finds the bottom block in the portal column.
  - Compute distance-squared:
    - `var18 = (var17 + 0.5) − entity.t` (Y offset)
    - `var20 = var12² + var18² + var15²`
  - If `var4 < 0.0 OR var20 < var4`: record this as best candidate
    (`var4 = var20`, `var6/7/8 = var11/var17/var14`).

**Result processing (portal found: `var4 >= 0.0`):**

Initial centre:
- `var22 = var6 + 0.5` (X centre)
- `var16 = var7 + 0.5` (Y centre)
- `var23 = var8 + 0.5` (Z centre)

Portal axis centering — check four neighbouring positions at the same Y for portal blocks:
- If `world.getBlockId(var6 − 1, var7, var8) == portalId` → `var22 −= 0.5`
- If `world.getBlockId(var6 + 1, var7, var8) == portalId` → `var22 += 0.5`
- If `world.getBlockId(var6, var7, var8 − 1) == portalId` → `var23 −= 0.5`
- If `world.getBlockId(var6, var7, var8 + 1) == portalId` → `var23 += 0.5`

This biases the entity position toward the interior centre of the portal opening.

Reposition entity:
- `entity.setPosition(var22, var16, var23, entity.yaw, 0.0F)`
- `entity.motionX = entity.motionY = entity.motionZ = 0.0`

Return `true`.

**No portal found:** Return `false` without modifying entity.

---

### 4.3 Method `c` (obfuscated: `c`) — createPortal

```
c(world: ry, entity: ia) → boolean (always returns true)
```

**Purpose:** Find a suitable flat location near the entity and build a 4×5 obsidian
portal frame with a 2×3 portal interior. Falls back to Y=70 if no suitable site is found.

**Initialisation:**
- `var3 = 16` — search radius
- `var4 = −1.0` — best score (negative = nothing found)
- `var6 = floor(entity.s)`, `var7 = floor(entity.t)`, `var8 = floor(entity.u)` — integer entity pos
- `var9 = var6`, `var10 = var7`, `var11 = var8` — best candidate (updated as search runs)
- `var12 = 0` — best orientation index
- `var13 = nextInt(4)` — random starting orientation (0–3)

---

#### Phase 1: Full 3D suitability scan (3-deep)

XZ loop: `var14` from `var6 − 16` to `var6 + 16`; `var17` from `var8 − 16` to `var8 + 16`.

For each XZ column, Y loop from top (`world.c − 1`) downward:
- Skip non-air blocks; seek the topmost contiguous air block from above
  (`world.isAirBlock(x, y, z)` — call `world.h(x, y, z)`).
- Walk `var20` down while the block below is also air.

For each candidate base Y, try 4 orientations (loop `var21` from `var13` to `var13 + 3`):

**Orientation vectors** (computed from `var21 % 4`):
| `var21 % 4` | var22 | var23 | Axis |
|-------------|-------|-------|------|
| 0           | 0     | 1     | Z-aligned |
| 1           | 1     | 0     | X-aligned |
| 2           | 0     | −1    | Z-aligned (reversed) |
| 3           | −1    | 0     | X-aligned (reversed) |

`var22 = (var21 % 2)`, `var23 = 1 − var22`; if `(var21 % 4) >= 2`: negate both.

**Validity check — 3×4×5 volume** (var24=0..2, var25=0..3, var26=−1..3):

For each (var24, var25, var26):
- World X: `var27 = var14 + (var25 − 1) × var22 + var24 × var23`
- World Y: `var28 = var20 + var26`
- World Z: `var29 = var17 + (var25 − 1) × var23 − var24 × var22`

Rules:
- `var26 < 0` (foundation layer, Y one below base): block material at (var27, var28, var29)
  must be solid (`world.getMaterial(x,y,z).isSolid()` = `world.e(x,y,z).b()`). If not solid →
  reject this orientation (continue outer loop).
- `var26 >= 0` (interior volume, 3 layers of height): block at (var27, var28, var29) must be
  air (`world.isAirBlock(x,y,z)` = `world.h(x,y,z)`). If not air → reject.

If all checks pass: score this candidate.
- `var52 = (var20 + 0.5) − entity.t` (Y distance)
- `var62 = var15² + var52² + var18²` (distance-squared to entity)
- If `var4 < 0.0 OR var62 < var4`: record best (`var4 = var62`, `var9/10/11 = var14/var20/var17`,
  `var12 = var21 % 4`).

---

#### Phase 2: Fallback 2D column scan (2 orientations)

Runs only if Phase 1 found no candidate (`var4 < 0.0`).

Same XZ loop. For each candidate Y, try only 2 orientations (loop `var40` from `var13` to `var13 + 1`).

**Validity check — 1-deep column, 4×5** (var53=0..3, var58=−1..3):

For each (var53, var58):
- World coords derived from `var30, var37, var33` and orientation vectors.
- Same rules as Phase 1: foundation solid, interior air.

No depth dimension (var24 loop absent) — checks only a 4×5 vertical slice, not a 3×4×5 box.

Score identical to Phase 1. Best orientation recorded as `var12 = var40 % 2`.

---

#### Emergency fallback (Y=70, no site found)

Runs only if both phases failed (`var4 < 0.0` after Phase 2).

Clamp best-candidate Y:
- `if (var10 < 70): var10 = 70`
- `if (var10 > world.c − 10): var10 = world.c − 10`
- `var16 = var10` (committed Y)

Build a minimal clearance platform around `(var32, var16, var34)` using the random orientation
from the search (var36/var19 from var12):

Triple loop: `var38` = −1 to 1 (3), `var41` = 1 to 2 (2), `var45` = −1 to 2 (4):
- Block X: `var49 = var32 + (var41 − 1) × var36 + var38 × var19`
- Block Y: `var55 = var16 + var45`
- Block Z: `var59 = var34 + (var41 − 1) × var19 − var38 × var36`
- `var65 = (var45 < 0)` → floor layer?
- Place obsidian if `var65 == true`, air (0) if `var65 == false`

**Effect:** Carves a 2×3×4 air pocket with an obsidian floor at the emergency location.

---

#### Frame Construction (always runs)

After the three phases (regardless of which found a spot), the frame is built.

Committed position: `var32 = var9`, `var16 = var10`, `var34 = var11`.
Orientation: `var36 = var12 % 2`, `var19 = 1 − var36`.
If `var12 % 4 >= 2`: negate both (`var36 = −var36`, `var19 = −var19`).

The outer loop (`var39 = 0` to `3`) runs **4 times** — each iteration places the complete
structure and triggers neighbor notifications. This ensures portal blocks activate.

**Each iteration:**

Step A — Suppress update notifications: `world.t = true`.

Step B — Place 4×5 structure (`var42` = 0..3, `var46` = −1..3):
- Block X: `var50 = var32 + (var42 − 1) × var36`
- Block Y: `var56 = var16 + var46`
- Block Z: `var60 = var34 + (var42 − 1) × var19`
- Edge condition: `var66 = (var42 == 0 OR var42 == 3 OR var46 == −1 OR var46 == 3)`
- If edge (`var66 == true`): place obsidian (ID 49)
- If interior (`var66 == false`): place portal block (ID 90)

Interior positions: var42 ∈ {1, 2} AND var46 ∈ {0, 1, 2} → 2×3 = 6 portal blocks.
Edge positions: everything else → obsidian.

Step C — Re-enable notifications: `world.t = false`.

Step D — Trigger neighbor changes for the entire 4×5 structure:
For each block (`var43` = 0..3, `var47` = −1..3):
- `world.notifyBlockChange(var51, var57, var61, world.getBlockId(var51, var57, var61))`

Always returns `true`.

---

## 5. Caller Context — Coordinate Scaling (`Minecraft.a(int)`)

Coordinate scaling happens in `Minecraft.a(int)` (the dimension-travel method),
**before** `aim` is called. The logic is:

```
oldDim = player.bK        // player's previous dimension
player.bK = newDim        // set new dimension
var3 = player.s           // X position
var5 = player.u           // Z position

scaleFactor = 1.0
if (oldDim > -1 AND newDim == -1):   scaleFactor = 0.125  // Overworld → Nether
if (oldDim == -1 AND newDim > -1):   scaleFactor = 8.0    // Nether → Overworld
// all other combos (including End): scaleFactor = 1.0

var3 *= scaleFactor
var5 *= scaleFactor
player.setPosition(var3, player.t, var5, ...)
```

**Summary table:**

| Transition              | X/Z scale | Note |
|-------------------------|-----------|------|
| Overworld (0) → Nether (-1) | × 0.125 (÷ 8) | |
| Nether (-1) → Overworld (0) | × 8.0 | |
| Overworld (0) → End (1)     | × 1.0  | Position overridden by End spawn point |
| End (1) → Overworld (0)     | × 1.0  | No aim.a() call; player returns to entry X/Z |
| Nether (-1) → End (1)       | Not applicable — not possible in 1.0 | |

`aim.a()` is called only when the old dimension is < 1 (`oldDim < 1`), which covers
Overworld→Nether, Nether→Overworld, and Overworld→End. It is NOT called when leaving the
End (oldDim = 1).

---

## 6. Dimension Travel Full Sequence

### Entering Nether (Overworld → Nether):
1. Entity position X/Z scaled × 0.125 (÷ 8)
2. New Nether world created/loaded
3. Entity added to Nether world at scaled position
4. `new aim().a(netherWorld, entity)` called
5. `aim.a()`: `world.y.g = −1` ≠ 1 → Nether portal logic
6. `b()` scans ±128 blocks for portal; if found: entity placed at portal centre
7. If not found: `c()` creates new portal, then `b()` repositions entity

### Leaving Nether (Nether → Overworld):
1. Entity position X/Z scaled × 8.0
2. New Overworld loaded
3. Entity added at scaled position
4. `new aim().a(overworldWorld, entity)` called
5. Same find/create logic as above

### Entering End (Overworld → End):
1. No coordinate scaling (factor = 1.0)
2. Entity position overridden to End spawn: `(100.0, 50.0, 0.0)` (from `WorldProviderEnd.j()`)
3. New End world created
4. Entity added to End world at (100, 50, 0)
5. `new aim().a(endWorld, entity)` called
6. `aim.a()`: `world.y.g == 1` → End platform branch
7. Platform placed centred at floor(100), floor(50)−1, floor(0) = (100, 49, 0)
8. Entity repositioned to (100, 49, 0), velocity zeroed

### Leaving End (End → Overworld):
1. No coordinate scaling
2. New Overworld loaded
3. `aim.a()` is NOT called (condition `oldDim < 1` fails for oldDim = 1)
4. Player returned to their pre-End X/Z (stored from before entering End)

---

## 7. Bitwise & Data Layouts

No metadata or bitfields in `aim` itself. Orientation encoding (in `c()`):

| `var21 % 4` | `var22` | `var23` | Direction of frame width-axis |
|-------------|---------|---------|-------------------------------|
| 0           | 0       | 1       | Z-positive |
| 1           | 1       | 0       | X-positive |
| 2           | 0       | −1      | Z-negative |
| 3           | −1      | 0       | X-negative |

---

## 8. Tick Behaviour

`aim` is not ticked. It is instantiated once per portal transition and discarded.

---

## 9. Known Quirks / Bugs to Preserve

1. **`aim` is NOT a singleton.** `new aim()` is instantiated on every portal transition.
   The internal `Random a` field is freshly seeded from `new Random()` (system time/random
   seed) on each construction — not from the world seed. Portal orientation is therefore
   non-deterministic across sessions.

2. **Double `b()` call after `c()`.** After building a portal, `b()` is called again to
   position the entity. If `b()` fails to find the newly built portal (possible in theory due
   to search radius), the entity is never repositioned. This is a vanilla edge case that
   should be preserved.

3. **End platform clears blocks.** The platform-building loop places air (ID 0) for all
   non-floor layers. Any blocks already in those positions (e.g., previously placed End Stone)
   are destroyed without drops.

4. **Frame built 4 times.** `c()` outer loop runs 4 iterations, rebuilding and re-notifying
   the 4×5 structure on each pass. This is verbatim behaviour — likely intended to trigger
   portal block activation via neighbor changes.

5. **Portal axis centering in `b()` shifts by ±0.5 on both adjacent sides.** If portal
   extends in both the −X AND +X directions, `var22` shifts −0.5 then +0.5 (net=0). Only
   asymmetric portals get a meaningful shift.

6. **`world.t` suppression flag.** `c()` sets `world.t = true` during block placement and
   `false` after. This suppresses whatever update system `t` controls (likely light or block
   update notifications). The flag is a direct field assignment with no lock.

---

## 10. Open Questions

1. **`world.h(x, y, z)`** — confirmed as `isAirBlock`? Or `isBlockAir`? Check `ry.h()`.

2. **`world.e(x, y, z)`** — returns material? Confirm `ry.e()` return type and `.b()` is
   `isSolid()`.

3. **`world.t`** — what exactly does this field suppress? Light updates? Neighbor change
   propagation? Tick scheduling?

4. **`entity.c(x, y, z, yaw, pitch)`** — setPosition method on `ia`. Confirm signature.

5. **`world.j(x, y, z, blockId)`** — notifyBlockChange. Confirm this is neighbor notification
   and not something else.

6. **Entity exclusion** — `aim.a()` receives `ia` (any entity). What happens if a non-player
   mob walks through a portal? The `entity.S()` trigger in `sc` (BlockPortal) calls `S()` on
   ANY entity. Does `S()` exist on non-player `ia`? Check `ia.S()` (currently unknown).

7. **`this.h.K()`** — in `Minecraft.a(int)`, `aim.a()` is only called inside
   `if (this.h.K() && var2 < 1)`. What does `K()` check? If the player is local/active?
   Creative-mode guard? Survival only?

8. **End return coordinates** — when leaving the End, the player is returned to their
   overworld position. How is this stored? As `entity.o` (the previous world reference)?
   Confirm the exact field that stores the overworld re-entry position.
