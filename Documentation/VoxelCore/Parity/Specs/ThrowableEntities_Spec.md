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

# Throwable Entities Spec
**Source classes:**
- `fm.java` — abstract ThrowableBase
- `aah.java` — EntitySnowball
- `qw.java` — EntityEgg
- `tm.java` — EntityEnderPearl
- `aad.java` — EntityFireball
- `yn.java` — EntitySmallFireball
- `bs.java` — EntityEyeOfEnder (EyeOfEnderSignal)

**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

```
ia (Entity)
 ├─ fm   ThrowableBase (abstract)
 │   ├─ aah  EntitySnowball       EntityList "Snowball"     ID 11
 │   ├─ qw   EntityEgg            (NOT in EntityList — no NBT persistence)
 │   └─ tm   EntityEnderPearl     EntityList "ThrownEnderpearl"  ID 14
 ├─ aad  EntityFireball           EntityList "Fireball"    ID 12
 │   └─ yn   EntitySmallFireball  EntityList "SmallFireball" ID 13
 └─ bs   EntityEyeOfEnder         EntityList "EyeOfEnderSignal"  ID 15
```

---

## 2. ThrowableBase (`fm`) — Abstract Class

### 2.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `c` | `nq` | `null` | Owner entity (thrower) |
| `a` | `boolean` | `false` | `inGround` — true once the projectile embeds in a block |
| `b` | `int` | `0` | Shake counter — counts down after impact; used for arrow-shake animation |
| `d` | `int` | `-1` | `xTile` — X coordinate of block the projectile is stuck in |
| `e` | `int` | `-1` | `yTile` — Y coordinate |
| `f` | `int` | `-1` | `zTile` — Z coordinate |
| `g` | `int` | `0` | `inTile` — block ID at the stuck position |
| `h` | `int` | `0` | Despawn tick counter while `inGround` |
| `i` | `int` | `0` | Flight tick counter — owner cannot be hit until `i >= 5` |

### 2.2 Physics Constants

| Value | Meaning |
|---|---|
| `0.25F × 0.25F` | Default hitbox width and height |
| `0.99F` | Air drag factor per tick (applied to all three velocity components) |
| `0.8F` | Water drag factor (replaces 0.99F when submerged) |
| `0.03F` | Gravity (from `b_()` — subtracted from Y velocity each tick) |
| `0.0075F` | Spread factor per unit of inaccuracy parameter |
| `1200` | Despawn tick count when inGround (60 seconds) |
| `5` | Minimum flight ticks before the owner entity can be hit |

### 2.3 Spawn Velocity — `a(double dx, double dy, double dz, float speed, float inaccuracy)`

This method is called from the player when throwing:

1. Normalise direction vector `(dx, dy, dz)`.
2. Apply Gaussian noise to each component: `component += nextGaussian() × 0.0075F × inaccuracy`.
3. Multiply normalised (noisy) vector by `speed`: final velocity vector.
4. Set `v,w,x` to this vector.
5. Compute yaw/pitch from velocity and set `y,z,A,B`.

**Owner-spawned constructor `fm(World, nq owner)`:**
1. Sets `c = owner`.
2. Positions entity at owner's eye position, offset backward by `sin(yaw)×0.16F / cos(yaw)×0.16F` and down by `0.1F`.
3. Calls `a(v, w, x, c_(), 1.0F)` where `c_()` = 1.5F (default throw speed).

### 2.4 Tick `a()`

Each tick:

1. Save previous position (R=s, S=t, T=u).
2. Call `super.a()` (Entity.tick — handles fire ticks, etc.).
3. Decrement shake counter `b` if `b > 0`.
4. **In-ground state (`a == true`):**
   - Read block at `(d, e, f)`.
   - If block ID unchanged (== `g`): increment despawn counter `h`. If `h == 1200` → remove entity. Return.
   - If block changed: leave ground state (`a = false`), randomise velocity slightly (`v *= rand(0,0.2)` etc.), reset `h` and `i`.
5. **Flying state (`a == false`):** increment flight counter `i`.
6. Ray-trace from current position `(s,t,u)` to next position `(s+v, t+w, u+x)`.
7. Expand entity AABB by velocity vector and expand by 1.0 block; collect entity list.
8. For each entity in list:
   - Skip if `e_() == false` (not damageable).
   - Skip if entity is owner AND `i < 5`.
   - Expand entity AABB by 0.3 and ray-trace.
   - Keep nearest hit.
9. If nearest entity hit found, override the block hit result with an entity hit result.
10. If any hit result exists: call abstract `a(gv hitResult)` — subclass handles impact.
11. Update position: `s += v; t += w; u += x`.
12. Compute yaw/pitch from velocity; smooth toward previous yaw/pitch by factor 0.2F.
13. Apply drag and gravity:
    - If in water: drag = `0.8F`; spawn 4 bubble particles.
    - Otherwise: drag = `0.99F`.
    - Apply: `v *= drag; w *= drag; x *= drag`.
    - Apply gravity: `w -= b_()` (0.03F default).
14. Call `d(s, t, u)` to update AABB.

### 2.5 NBT

All subclasses save/load via `fm.a(ik)` / `fm.b(ik)`:

| NBT key | Type | Field |
|---|---|---|
| `"xTile"` | short | `d` |
| `"yTile"` | short | `e` |
| `"zTile"` | short | `f` |
| `"inTile"` | byte | `g` (unsigned) |
| `"shake"` | byte | `b` |
| `"inGround"` | byte | `a` (1=true, 0=false) |

**Owner entity `c` is NOT saved to NBT.** After world reload, the owner reference is lost
and the entity can hit any target from tick 0.

---

## 3. EntitySnowball (`aah`)

**EntityList:** `"Snowball"`, ID **11**
**Hitbox:** inherited 0.25×0.25

### 3.1 Impact Logic `a(gv hitResult)`

1. If hit result contains an entity (`var1.g != null`):
   - If entity is a Blaze (`qf`): deal 3 damage via `a(DamageSource.causeEntityDamage(this, c), 3)`.
   - Else: deal 0 damage (still calls the damage pipeline — may apply knockback).
2. Spawn 8 `"snowballpoof"` particles at impact position.
3. If server side (`!o.I`): remove entity (`v()`).

### 3.2 No Special NBT

Uses `fm` NBT exactly.

---

## 4. EntityEgg (`qw`)

**EntityList:** **not registered** — no NBT persistence (same as EntityFishHook).
**Hitbox:** inherited 0.25×0.25 from `fm`.

### 4.1 Impact Logic `a(gv hitResult)`

1. If hit result contains an entity: deal 0 damage.
2. If server side:
   a. Roll `nextInt(8) == 0` — 1/8 chance to spawn chicken(s):
      - Roll `nextInt(32) == 0` — 1/32 chance: spawn **4** chickens.
      - Otherwise: spawn **1** chicken.
      - Each chicken (`qh`) is spawned as a **baby**: age set to `-24000` via `b(-24000)`.
      - Each chicken is positioned and faced at the impact point.
3. Spawn 8 `"snowballpoof"` particles at impact position.
4. If server side: remove entity.

### 4.2 Chicken Count Probabilities

| Outcome | Probability |
|---|---|
| 0 chickens | 7/8 |
| 1 baby chicken | (1/8) × (31/32) = 31/256 ≈ 12.1% |
| 4 baby chickens | (1/8) × (1/32) = 1/256 ≈ 0.4% |

---

## 5. EntityEnderPearl (`tm`)

**EntityList:** `"ThrownEnderpearl"`, ID **14**
**Hitbox:** inherited 0.25×0.25

### 5.1 Impact Logic `a(gv hitResult)`

1. If hit result contains an entity: deal 0 damage.
2. Spawn 32 `"portal"` particles in a vertical spread at impact position.
3. If server side:
   - If owner `c` is not null:
     a. Teleport owner to impact position: `c.i(s, t, u)`.
     b. Reset owner's fall-damage accumulator: `c.Q = 0.0F` (sets `fallDistance` to zero).
     c. Deal 5 fall damage to owner: `c.a(DamageSource.fall, 5)`.
   - Remove entity.

### 5.2 NBT

Uses `fm` NBT exactly. Owner reference is not persisted.

---

## 6. EntityFireball (`aad`) — Large Ghast Projectile

**EntityList:** `"Fireball"`, ID **12**
**Superclass:** `ia` (Entity) — does **not** extend `fm`; has its own physics
**Hitbox:** 1.0F × 1.0F

### 6.1 Fields (Additional to Entity Base)

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `nq` | `null` | Owner entity |
| `b` | `double` | — | X-axis acceleration (per-tick direction) |
| `c` | `double` | — | Y-axis acceleration |
| `d` | `double` | — | Z-axis acceleration |
| `e` | `int` | `-1` | xTile |
| `f` | `int` | `-1` | yTile |
| `g` | `int` | `-1` | zTile |
| `h` | `int` | `0` | inTile block ID |
| `i` | `boolean` | `false` | inGround flag |
| `aq` | `int` | `0` | Despawn counter while inGround |
| `ar` | `int` | `0` | Flight tick counter (owner-exclusion timer) |

### 6.2 Construction

**`aad(World, nq owner, double dirX, double dirY, double dirZ)`:**
1. Sets owner `a = owner`.
2. Positions at owner's location; zero velocity.
3. Applies Gaussian spread: `dirX += nextGaussian() × 0.4`, same for Y and Z.
4. Normalises direction and stores as acceleration: `b = dirX/len × 0.1`, etc.

**`aad(World, double x, double y, double z, double dirX, double dirY, double dirZ)`:**
Direct-position constructor. Normalises direction, scales to 0.1.

### 6.3 Physics

The fireball uses an **acceleration model**, not a velocity model:

Each tick:
1. Accumulate velocity: `v += b; w += c; x += d`.
2. Ray-trace and entity-collision check (same pattern as `fm`, but owner-exclusion is `i >= 25` ticks).
3. Move: `s += v; t += w; u += x`.
4. Yaw/pitch computed from velocity (smoothed 0.2F).
5. Air drag: `v *= 0.95F; w *= 0.95F; x *= 0.95F`.
6. Water drag: `0.8F` (replaces 0.95F per axis, applied in the water check block).
7. Emit `"smoke"` particle at `(s, t+0.5, u)` each tick.

**No gravity.** The fireball flies in a straight-ish direction, slowing due to drag.

### 6.4 Impact `a(gv hitResult)`

Server-side only:
1. If entity hit: deal 4 damage via `a(DamageSource.causeEntityDamage(this, a), 4)`.
2. Create explosion at `(s, t, u)` with **power 1.0F**, **incendiary = true** (sets fire to blocks).
3. Remove entity.

### 6.5 NBT

| NBT key | Type | Field |
|---|---|---|
| `"xTile"` | short | `e` |
| `"yTile"` | short | `f` |
| `"zTile"` | short | `g` |
| `"inTile"` | byte | `h` |
| `"inGround"` | byte | `i` |

Owner is NOT saved.

### 6.6 Other

- `e_() = true` — can be targeted by players and mobs.
- `Q() = 1.0F` — brightness factor 1.0.
- `a(float) = 15728880` — fully bright (hardcoded sky+block light value).
- When hit by a DamageSource that has an attacker entity: sets its own velocity to the attacker's velocity and updates acceleration accordingly (gets "deflected").

---

## 7. EntitySmallFireball (`yn`) — Blaze Projectile

**EntityList:** `"SmallFireball"`, ID **13**
**Superclass:** `aad` (EntityFireball)
**Hitbox:** 0.3125F × 0.3125F (overrides parent's 1.0×1.0)

### 7.1 Differences from EntityFireball

- Smaller hitbox: `a(0.3125F, 0.3125F)`.
- `e_() = false` — cannot be targeted; not push-damageable.
- `a(pm, int) = false` — immune to all damage; cannot be destroyed by weapons.

### 7.2 Impact `a(gv hitResult)` (overrides parent)

**Entity hit:**
1. If entity is not fire-immune (`!var1.g.B()`):
   a. Deal 5 fire damage: `var1.g.a(DamageSource.causeEntityDamage(this, a), 5)`.
   b. Set entity on fire for 5 seconds: `var1.g.e(5)`.

**Block hit:**
1. Compute the block position adjacent to the hit face (face 0=down, 1=up, 2=north, etc.).
2. If that adjacent block is replaceable (air/plants): place fire block (`yy.ar`, fire ID) there.

3. Remove entity (always, whether or not fire was placed).

---

## 8. EntityEyeOfEnder (`bs`)

**EntityList:** `"EyeOfEnderSignal"`, ID **15**
**Superclass:** `ia` (Entity) — does **not** extend `fm`
**Hitbox:** 0.25F × 0.25F

### 8.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | `0` | Age counter |
| `b` | `double` | — | Target X coordinate |
| `c` | `double` | — | Target Y coordinate |
| `d` | `double` | — | Target Z coordinate |
| `e` | `int` | `0` | Despawn tick counter |
| `f` | `boolean` | — | Drop mode: true = drop Eye of Ender item on despawn; false = play break particles |

### 8.2 Target Setting `a(double targetX, int ?, double targetZ)`

Sets the flight target:
1. Compute XZ distance from entity to `(targetX, targetZ)`.
2. If distance > 12: cap the target at 12 blocks ahead along the XZ line; target Y = `t + 8.0`.
3. If distance ≤ 12: use the target directly (with passed Y value `var3` as Y).
4. Reset `e = 0`.
5. Set `f = nextInt(5) > 0` — 4/5 chance the entity drops its item on despawn; 1/5 chance of break particles.

### 8.3 Tick `a()`

Each tick:
1. Save previous position.
2. Call `super.a()`.
3. Advance position: `s += v; t += w; u += x`.
4. Compute yaw/pitch from velocity; smooth toward previous by 0.2F.
5. If server side:
   - Steer toward target:
     - Compute XZ distance `var6` to target `(b, d)`.
     - New speed = current XZ speed + `(var6 - current_XZ_speed) × 0.0025`.
     - If `var6 < 1.0F`: reduce speed × 0.8; reduce Y velocity × 0.8.
     - Apply: `v = cos(atan2) × new_speed; x = sin(atan2) × new_speed`.
     - Y steering: if below target Y → accelerate up (`w += (1.0 - w) × 0.015F`); else accelerate down (`w += (-1.0 - w) × 0.015F`).
   - Update AABB: `d(s, t, u)`.
   - Increment `e`.
   - If `e > 80`:
     - Remove entity (`v()`).
     - If `f == true`: spawn Eye of Ender item entity at current position.
     - If `f == false`: play world event 2003 ("eye of ender death particles") at rounded position.

### 8.4 Particle Trail

Each tick while in air: spawn `"portal"` particle at a randomised offset from `(s - v×0.25, t - w×0.25 - 0.5, u - x×0.25)`.

When submerged in water: spawn `"bubble"` particles instead.

### 8.5 NBT

`a(ik)` and `b(ik)` are empty — **no NBT persistence**. The eye of ender despawns on chunk unload.

---

## 9. Summary Table

| Class | ID | Extends | Hitbox | Gravity | Air drag | Despawn |
|---|---|---|---|---|---|---|
| `aah` Snowball | 11 | `fm` | 0.25×0.25 | 0.03F/t | 0.99/t | 1200t inGround |
| `qw` Egg | — | `fm` | 0.25×0.25 | 0.03F/t | 0.99/t | 1200t inGround |
| `tm` EnderPearl | 14 | `fm` | 0.25×0.25 | 0.03F/t | 0.99/t | 1200t inGround |
| `aad` Fireball | 12 | `ia` | 1.0×1.0 | none | 0.95/t | 1200t inGround |
| `yn` SmallFireball | 13 | `aad` | 0.3125×0.3125 | none | 0.95/t | 1200t inGround |
| `bs` EyeOfEnder | 15 | `ia` | 0.25×0.25 | none | none | 80t flight |

---

## 10. Open Questions

| # | Question |
|---|---|
| 10.1 | EnderPearl `c.i(s,t,u)` — exact method name and semantics for teleporting the owner. Verify fall-damage immunity from teleport (Q=0 resets fallDistance before 5 damage). |
| 10.2 | Fireball deflection: the code reads the attacker's velocity `R()` when hit. Confirm which method `R()` maps to in Entity (likely `getLookVec()` or `getVelocity()`). |
| 10.3 | EntityEgg (`qw`) is not in `afw` (EntityList). Confirm it is intentionally absent and will not be NBT-persisted. |
| 10.4 | Fireball `a(DamageSource.causeEntityDamage(this, a), 4)` — confirm damage amount is 4 (same as the Explosion spec's entity-hit logic or a separate constant). |
