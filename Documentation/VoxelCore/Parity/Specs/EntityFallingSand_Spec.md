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

# EntityFallingSand Spec
**Source class:** `uo.java` (EntityFallingSand)
**Superclass:** `ia` (Entity)
**EntityList string:** `"FallingSand"` — integer ID **21**
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`uo` represents a falling block entity. When sand (ID 12), gravel (ID 13), or any other
block flagged as gravity-sensitive falls through air, the block is replaced with air and
a `uo` entity is spawned in its place. The entity falls under gravity, then attempts to
place the block at its landing position; if placement is not possible the block is dropped
as an item.

---

## 2. Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | — | Block ID of the falling block (NBT: `"Tile"` byte) |
| `b` | `int` | `0` | Age counter — incremented each tick; used for item-drop timeout |

Inherited fields used by `uo`:
- `l` (`noClip`) — set to `true` in constructor; entity does not clip against entities
- `L` (eye height) — set to `N/2` (half the entity height, 0.49F)
- `s/t/u` (current XYZ), `v/w/x` (velocity XYZ)
- `p/q/r` (previous XYZ) — initialised to spawn position
- `D` (`onGround`) — set by the physics engine after `b(v,w,x)` call
- `K` (`isDead`) — set by `v()`
- `o` (World reference)
- `I` (isClientSide on World — `o.I`)

---

## 3. Constants & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `0.98F × 0.98F` | constructor `a(0.98F, 0.98F)` | Hitbox width and height (just under 1 block) |
| `0.04F` | tick gravity | Downward acceleration per tick (m/t²) |
| `0.98F` | tick drag | Velocity decay factor per axis per tick |
| `0.7F` | onGround horizontal damp | Horizontal velocity multiplied by 0.7 when onGround |
| `-0.5` | onGround vertical bounce | Y velocity multiplied by -0.5 when onGround (weak bounce) |
| `100` | age timeout | Entity drops block as item after 100 ticks if never placed |

---

## 4. Methods — Detailed Logic

### Constructor `uo(World, double x, double y, double z, int blockId)`

1. Calls `super(world)`.
2. Sets field `a = blockId`.
3. Sets `l = true` (noClip).
4. Sets hitbox to 0.98×0.98 via `a(0.98F, 0.98F)`.
5. Sets eye height `L = N / 2.0F` (N = hitbox height; result = 0.49F).
6. Calls `d(x, y, z)` to set position.
7. Sets velocity `v = 0, w = 0, x = 0` (starts stationary; gravity pulls it down on tick 1).
8. Sets previous position `p = x, q = y, r = z`.

### Tick `a()`

Called once per game tick (20 Hz) while the entity exists.

**Step 1 — Immediate-remove guard:**
If `a == 0` (block ID is 0/air), call `v()` (mark dead) and return.

**Step 2 — Position bookkeeping:**
```
p = s;  q = t;  r = u;   (save previous position)
b++;                       (increment age)
```

**Step 3 — Apply gravity:**
`w -= 0.04F`

**Step 4 — Apply physics:**
Call `b(v, w, x)` — the inherited Entity sweep-movement method. This performs AABB-based
collision against world blocks and updates `D` (onGround), `s/t/u` (position).

**Step 5 — Apply drag:**
```
v *= 0.98F;
w *= 0.98F;
x *= 0.98F;
```

**Step 6 — Compute block coordinates:**
```
var1 = floor(s);   (block X)
var2 = floor(t);   (block Y — entity's feet)
var3 = floor(u);   (block Z)
```

**Step 7 — First-tick block removal (age == 1):**

On the very first tick after spawning (`b == 1` after the increment):
- If the block at (var1, var2, var3) equals the stored block ID `a`:
  → Call `world.setBlock(var1, var2, var3, 0)` — remove the source block (replace with air).
- Else, if `o.I == false` (server side):
  → Call `v()` — remove entity. The block was already gone or changed; no further action.

*Purpose: on the server, the spawning code normally already sets the source block to air
before creating the entity. This first-tick check clears any race condition. If the block
is already gone, the entity vanishes immediately.*

**Step 8 — Landing logic (onGround, `D == true`):**

When the entity has landed on a solid surface:
1. Dampen horizontal velocity: `v *= 0.7F; x *= 0.7F;`
2. Bounce Y velocity: `w *= -0.5` (note: double multiply, not float; sign flip creates weak upward bounce)
3. **Block-placement check:**
   If the block at (var1, var2, var3) is NOT block `yy.ac` *(see Open Question 9.1)*:
   a. Call `v()` — mark entity for removal.
   b. Attempt placement:
      - Condition 1: `world.canBlockBeSet(a, var1, var2, var3, true, 1)` is **false**, OR
      - Condition 2: `BlockSand.isFallingBelow(world, var1, var2-1, var3)` is **true**, OR
      - Condition 3: `world.setBlock(var1, var2, var3, a)` returns **false**
      - If any of the above is true AND server side: drop block as item (`b(a, 1)`).

   *Condition 2 prevents stacking a falling block on another falling block that has not yet
   landed — the new block drops as item instead.*

**Step 9 — Timeout (age > 100, not yet landed):**

If `b > 100` AND `o.I == false` (server side) AND entity is not onGround:
1. Drop block as item: `b(a, 1)`.
2. Remove entity: `v()`.

---

### NBT Write `a(NbtCompound)`

Writes `"Tile"` as a **byte** = `(byte) a` (block ID, low 8 bits only).

### NBT Read `b(NbtCompound)`

Reads `"Tile"` as byte: `a = tag.getByte("Tile") & 255` (unsigned).

---

## 5. Bitwise & Data Layouts

None. The only data value is the raw block ID stored as a single byte in NBT.
Block IDs > 127 are stored as signed byte but read back with `& 255` to restore the
full range 0–255.

---

## 6. Tick Behaviour

Ticked every server game tick (20 Hz). The entity exists purely server-side for physics;
the client receives position packets. Client instances do run ticks (for rendering) but
the item-drop and block-place paths are guarded by `!o.I`.

---

## 7. Known Quirks / Bugs to Preserve

**Quirk 7.1 — Bounce without placement:**
When onGround and the position is occupied by block `yy.ac`, the entity *does not* remove
itself: it bounces indefinitely (velocity multiplied each tick) until it moves to a position
where the block IS something else. In practice this creates a "vibrating" entity against
certain block types.

**Quirk 7.2 — Gravity before movement:**
`w -= 0.04F` is applied *before* `b(v,w,x)`, so the entity begins falling immediately from
the first tick. Combined with tick-1 block-removal, there is exactly zero visual delay
between the block disappearing and the entity starting to fall.

**Quirk 7.3 — Drag applied after physics:**
Drag (`v *= 0.98F` etc.) is applied after `b()`. This means the drag affects the *next*
tick's initial velocity, not the velocity used for this tick's movement.

**Quirk 7.4 — noClip = true:**
The entity bypasses entity-entity collision. Two falling sand entities can occupy the same
position simultaneously without pushing each other.

---

## 8. Open Questions

| # | Question |
|---|---|
| 9.1 | `yy.ac.bM` — which block ID is this? The placement logic skips when the destination equals this ID. Likely air (0) or a block the decompiler assigned field `ac`. Coder should verify against the Block static field table. |
| 9.2 | `world.canBlockBeSet(a, x, y, z, true, 1)` — exact method signature and return semantics need verification in the World spec or Block spec. |
| 9.3 | `BlockSand.isFallingBelow(world, x, y-1, z)` — exact static method name and logic (checks if block below would also fall) needs confirmation in the BlockSand source. |
