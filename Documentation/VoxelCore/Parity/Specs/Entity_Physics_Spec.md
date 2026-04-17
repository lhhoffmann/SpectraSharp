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

# Entity Physics Spec (`ia.b()` Move Algorithm)
**Source class:** `ia.java` (Entity base)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Entity Fields — `ia`

| Field | Type | Meaning |
|---|---|---|
| `s, t, u` | `double` | Position (x, y, z) — feet level |
| `v, w, x` | `double` | Velocity (motionX, motionY, motionZ) |
| `p, q, r` | `double` | Position last tick (prevX, prevY, prevZ) |
| `y, z` | `float` | Rotation (yaw, pitch) |
| `A, B` | `float` | Previous yaw/pitch |
| `C` | `c` (AABB) | Current collision box |
| `D` | `boolean` | `onGround` |
| `E` | `boolean` | `isCollidedHorizontally` |
| `F` | `boolean` | `isCollidedVertically` |
| `G` | `boolean` | `isCollided` (E OR F) |
| `I` | `boolean` | `isInWeb` — velocity reduced by cobweb |
| `J` | `boolean` | `noClip` — skip collision (default `true`, set `false` for physics entities) |
| `K` | `boolean` | `isDead` |
| `L` | `float` | `yOffset` — AABB offset from feet (default 0.0F) |
| `M` | `float` | `width` (default 0.6F) |
| `N` | `float` | `height` (default 1.8F) |
| `O, P` | `float` | prevDistanceWalkedModified, distanceWalkedModified |
| `Q` | `float` | `fallDistance` |
| `U` | `float` | `ySize` — step height adjustment (accumulated from steps) |
| `V` | `float` | `stepHeight` — how high the entity can auto-step (default 0.0F; mobs/player = 0.5F) |
| `W` | `boolean` | `noPhysics` (alternate no-clip for physics entities) |
| `Z` | `int` | `ticksExisted` |
| `aa` | `int` | `fireResistance` (seconds; fire ticks set to `-aa * 20` when fire-immune) |
| `c` | `int` (private) | `fireTicks` (positive = burning; negative = immune cooldown) |
| `ab` | `boolean` | `inWater` |

---

## 2. `ia.b(double dx, double dy, double dz)` — Move Method

### 2.1 Cobweb Slowdown

If `I` (isInWeb) is true:
```
dx *= 0.25; dy *= 0.05F; dz *= 0.25
v = w = x = 0 (zero velocity)
I = false
```

### 2.2 Sneak Clipping

If `D` (onGround) AND entity is sneaking (`q()`):
```
// Prevent stepping off edges while sneaking
// Test X and Z movement in steps of 0.05 blocks
// If movement would fall off the edge, zero it out
```
Specifically: reduce dx/dz by 0.05 until horizontal movement no longer causes the entity
to move off a solid block.

### 2.3 Block AABB Collection

Get all blocking AABBs in the swept volume:
```
List<AABB> aabbs = world.a(entity, this.C.expand(dx, dy, dz))
```

### 2.4 Y Clip (vertical collision)

For each AABB in the list, clip `dy` via `aabb.b(entityAABB, dy)`.
Move entity AABB vertically by clipped dy.

**Vertical collision flags:**
- If `!noClip && originalDy != dy`: zero out dy.

### 2.5 Step-Up Logic

If **step-up is possible** (`V > 0.0` AND (`D` OR `dy < 0`) AND (horizontal collision)):

Save current AABB. Try again with `dy = V` (step up):
1. Re-collect blocks.
2. Re-clip X.
3. Re-clip Z.
4. Check: does step-up path give more horizontal movement than flat path?
   - If yes: use the step-up path; set `U += fractional_y_of_new_AABB + 0.01`
   - If no: revert to flat path.

### 2.6 X and Z Clip

For each AABB: clip `dx` via `aabb.a(entityAABB, dx)`.
Move AABB horizontally.
If `!noClip && originalDx != dx`: zero out dx.

Repeat for Z.

### 2.7 Position Update

```
s = (C.minX + C.maxX) / 2.0
t = C.minY + L - U
u = (C.minZ + C.maxZ) / 2.0
```

`U` decays per tick: `U *= 0.4F` at start of move.

### 2.8 Collision Flags

```
E = (originalDx != dx || originalDz != dz)   // horizontal collision
F = (originalDy != dy)                        // vertical collision
D = (originalDy != dy && originalDy < 0.0)   // on ground (was falling, hit floor)
G = E || F
```

### 2.9 Velocity Zeroing

```
if (originalDx != dx): v = 0
if (originalDy != dy): w = 0
if (originalDz != dz): x = 0
```

### 2.10 Fall Distance Update — `a(double dy, boolean onGround)`

```java
if (onGround) {
    if (fallDistance > 0.0F):
        c(fallDistance);    // deal fall damage
        fallDistance = 0.0F;
} else if (dy < 0.0):
    fallDistance -= dy;  // dy is negative while falling → fallDistance increases
```

### 2.11 Block Walking Sound / Footsteps

Tracks `P` (distanceWalkedModified):
```
P += sqrt(Δx² + Δz²) * 0.6
```
If `P > b` threshold AND block below is solid: play `block.stepSound`.
Also calls `block.onEntityWalking(world, x, y, z, entity)`.

### 2.12 Block Overlap Callbacks

For all blocks touching the current AABB:
```
block.onEntityCollision(world, x, y, z, entity)
```
(Examples: fire block sets entity on fire; cactus deals damage.)

### 2.13 Water Extinguish

If entity is in water (`C()` check) AND `fireTicks > 0`:
- Play sizzle sound.
- `c = -aa` (set fire immunity cooldown).

---

## 3. `ia.w()` — Entity Base Tick (fire logic)

Per tick:
```
if (fireTicks > 0):
    if (af [fire immune]):
        fireTicks -= 4
    else:
        if (fireTicks % 20 == 0): deal 1 fire damage (pm.b)
        fireTicks--
```

Lava exposure `a(su lava)`:
- Deal 5 damage (if not fire-immune).
- Increment lava depth counter `c`. At `c == 0`: set on fire 8 seconds.

`e(int seconds)` = setOnFire: `fireTicks = max(fireTicks, seconds * 20)`.
`y()` = extinguishFire: `fireTicks = 0`.

---

## 4. Step Height

| Entity type | `V` (stepHeight) |
|---|---|
| Base entity | 0.0F |
| Mobs / Player | 0.5F |
| Minecart | Varies (follows rails, not normal step) |

---

## 5. Air Supply (DataWatcher slot 1)

Accessed via `ia.Z()` (getAir) and `ia.g(int)` (setAir). Default: 300.

Drowning is handled by `nq` (LivingEntity) — see LivingEntity_Survival_Spec.

---

## 6. Entity AABB — `a(float width, float height)`

AABB is centered on `(s, t, u)` (feet at `t`):
```
minX = s - width/2
minY = t - L + U
maxX = s + width/2
maxY = t - L + U + height
minZ = u - width/2
maxZ = u + width/2
```

---

## 7. Slipperiness

Slipperiness is applied in `nq` (LivingEntity) moveEntityWithHeading, not in `ia.b()` directly:
```
friction = slipperiness * 0.91F   // default block slipperiness = 0.6F
motionX *= friction
motionZ *= friction
```
Ice block slipperiness: 0.98F.

---

## 8. Open Questions

| # | Question |
|---|---|
| 8.1 | `q()` — is this the isSneaking check? What DataWatcher slot? |
| 8.2 | `c(float fallDistance)` — is this the fall damage method on ia, or overridden in nq? |
| 8.3 | `world.a(entity, aabb)` — returns `List<AABB>` of block collision boxes in the area? |
| 8.4 | `c.b(AABB, dy)` — is this the clipYCollide method? Confirm parameter order. |
| 8.5 | `noClip` field: `J = true` by default seems backwards. Confirm — does true = noClip or clips? |
| 8.6 | Slipperiness — where exactly is `blockBelow.slipperiness` accessed in nq? |
