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

# EntityBoat Spec
**Source class:** `no.java` (EntityBoat)
**Superclass:** `ia` (Entity — NOT LivingEntity)
**EntityList string:** `"Boat"` — integer ID **41**
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`no` is the rideable boat entity. It floats on water surfaces, is propelled by the rider's
movement keys, can carry one passenger, and shatters into planks and sticks if hit
hard enough or driven into a wall at speed.

---

## 2. Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW17 | `int` | 0 | Forward momentum input (from passenger) |
| DW18 | `int` | 1 | Side momentum input (from passenger) |
| DW19 | `int` | 0 | Damage accumulator (used for shake animation and break threshold) |
| `a` | `int` | 0 | Client interpolation step counter |
| `b` | `double` | 0 | Target X for client interpolation |
| `c` | `double` | 0 | Target Y for client interpolation |
| `d` | `double` | 0 | Target Z for client interpolation |
| `e` | `double` | 0 | Target yaw (client) |
| `f` | `double` | 0 | Target pitch (client) |
| `g` | `double` | 0 | Velocity X (server internal copy) |
| `h` | `double` | 0 | Velocity Y (server internal copy) |
| `i` | `double` | 0 | Velocity Z (server internal copy) |

### DataWatcher Slots

| Slot | Type | Default | Semantics |
|---|---|---|---|
| 17 | `int` | 0 | Time-to-live for shake effect (ticks remaining) |
| 18 | `int` | 1 | Shake direction multiplier |
| 19 | `int` | 0 | Damage hit-points (break threshold 40) |

---

## 3. Constants

| Value | Meaning |
|---|---|
| `1.5F × 0.6F` | Hitbox width × height |
| `0.5F` | Eye height offset (L = N / 2.0F) |
| `0.0F` | Sound volume override (`i_()` returns 0 — silent) |
| `5` | Number of Y-slice samples for water fraction computation |
| `0.125` | Y-sample offset correction |
| `0.4` | Max XZ velocity per tick |
| `0.2F` | Passenger XZ input contribution factor |
| `0.04F` | Buoyancy upward acceleration when partially submerged |
| `0.007F` | Slow upward float acceleration when fully in water |
| `0.99F` | XZ drag (per tick, each of v and x) |
| `0.95F` | Y drag (per tick, w component) |
| `0.5` | onGround velocity halving (all axes) |
| `20.0°` | Max yaw turn per tick |
| `40` | Damage threshold for boat destruction |
| `3` | Planks dropped on break (`yy.x.bM`) |
| `2` | Sticks dropped on break (`acy.C.bM`) |
| `-0.3F` | Rider eye offset (`P()`) |
| `0.2F` | AABB expansion for collision with other boats |
| `0.8F` | Min dot product for same-direction boat collision |

---

## 4. Tick Logic `a()`

### 4.1 Shared pre-logic (server and client)

1. Decrement DW17 (shake timer) if > 0.
2. Decrement DW19 (damage) if > 0.

### 4.2 Client-side interpolation

If `a > 0` (interpolation steps remain):
- Advance position by `1/a` of the remaining gap toward `(b, c, d)`.
- Advance yaw and pitch similarly toward target.
- Decrement `a`.

Otherwise: add velocity directly to position.

Apply drag on onGround:
- `v *= 0.5, w *= 0.5, x *= 0.5`.

Apply drag always:
- `v *= 0.99F, w *= 0.95F, x *= 0.99F`.

### 4.3 Server-side physics

**Water fraction sampling:**
Sample 5 Y-slices of the boat's AABB from bottom to top. For each slice, check if the
horizontal cross-section overlaps any `p.g` (water-like block). Accumulate fraction:
`waterFraction += 1.0 / numSlices`. The result is in `[0.0, 1.0]`.

**Buoyancy Y velocity:**
- If `waterFraction < 1.0`:
  - `w += 0.04F * (waterFraction * 2.0 - 1.0)` — upward push proportional to submersion.
  - If `waterFraction == 0`: full gravity applies (no water at all).
- If `waterFraction == 1.0` (fully submerged):
  - If `w < 0.0`: halve it (dampen downward).
  - `w += 0.007F` (slow upward float).

**Passenger contribution:**
If a passenger (`m != null`) is riding:
- `v += m.v * 0.2`
- `x += m.x * 0.2`

**Speed cap:**
```
v = clamp(v, -0.4, 0.4)
x = clamp(x, -0.4, 0.4)
```

**On-ground halving:**
If `D == true` (on ground):
- `v *= 0.5, w *= 0.5, x *= 0.5`

**Physics step:**
Call `b(v, w, x)` — inherited sweep-movement.

**Wall collision break:**
If `E == true` (collidedHorizontally) AND `sqrt(v² + x²) > 0.2` AND server side:
- Call `v()` (mark dead).
- Drop 3× `yy.x.bM` (planks).
- Drop 2× `acy.C.bM` (sticks).

**Drag (post-physics):**
```
v *= 0.99F
w *= 0.95F
x *= 0.99F
```

**Yaw alignment:**
- Compute target yaw from movement direction: `atan2(z_delta, x_delta) * 180 / π`.
- Max turn rate: ±20° per tick toward target.
- Update `y` (yaw) accordingly.

**Snow block destruction:**
Check 4 corner positions of the boat's XZ footprint. If any corner lands on block
`yy.aS.bM` (snow), call `world.g(x, y, z, 0)` (remove the snow block).

**Passenger eject:**
If `m != null && m.K` (dead passenger): set `m = null`.

---

## 5. Passenger Positioning `N()`

If passenger `m != null`:
```
offsetX = cos(y * π / 180) * 0.4
offsetZ = sin(y * π / 180) * 0.4
m.d(s + offsetX, t + P() + m.O(), u + offsetZ)
```

Where `P()` returns `-0.3F` (rider sits below boat center).

---

## 6. Damage and Destruction `a(Entity attacker, int damage)`

Server side only, if not dead:
1. Apply knockback: `d(-i())` (direction from DW18 multiplier).
2. Increment shake: `c(10)` (set DW17 = 10 ticks of shake).
3. Accumulate damage: `b(g() + damage × 10)`.
4. Play hit sound: `G()`.
5. If damage total > 40:
   - Eject passenger if any.
   - Drop 3 planks (`yy.x.bM`), 2 sticks (`acy.C.bM`).
   - Call `v()`.

Returns `true`.

---

## 7. Riding — `c(EntityPlayer player)`

When player right-clicks to mount:
- If already occupied by a different player: return `true` (refused).
- Server side: call `player.g(this)` (mount player onto boat).
- Returns `true`.

---

## 8. Boat-Boat Collision

During tick, the boat queries nearby entities in `C.expand(0.2, 0, 0.2)`.
For each nearby entity that is another `no` (boat) and is NOT this boat's passenger:
- Call `entity.e(this)` — apply push impulse.

The push logic in `e(ia)` checks dot product of relative direction against boat heading;
only pushes if `|dot| >= 0.8` (roughly same direction). If a furnace minecart hits a boat,
the minecart gets boosted and the boat slows.

---

## 9. NBT

No unique NBT fields. Boat state (damage, shake) is not persisted between saves.

---

## 10. Quirks

**Quirk 10.1 — Silent entity:**
`i_()` returns `0.0F`. Boats make no ambient, hurt, or step sounds. The splash particle
effect is purely visual.

**Quirk 10.2 — Collision break vs. damage break:**
Wall collision (`E == true` at speed) destroys the boat instantly without the shake
animation. Direct attack uses the 40-damage accumulator and shows the shake effect.

**Quirk 10.3 — Passenger offset in front:**
The passenger is placed at `(cos(yaw) * 0.4, P(), sin(yaw) * 0.4)` offset from boat
center — slightly forward, not centered.

**Quirk 10.4 — Snow destruction:**
Boats destroy snow blocks at their four corners each tick. This allows boats to ride
through shallow snow without stopping.

---

## 11. Open Questions

| # | Question |
|---|---|
| 11.1 | `p.g` — is this the water material flag, or a specific water block check? |
| 11.2 | Exact item IDs: `yy.x.bM` = Planks (ID 5?), `acy.C.bM` = Stick (item). Confirm. |
| 11.3 | Is DW17 the shake-ticks field and DW18 the shake-direction, or vice versa? The decompiler method aliases (`c()/h()` vs `b()/i()`) need cross-referencing. |
