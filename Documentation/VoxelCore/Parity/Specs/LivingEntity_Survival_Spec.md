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

# LivingEntity Survival Spec (Drowning, Fire, Fall Damage)
**Source classes:** `ia.java` (Entity base), `nq.java` (LivingEntity)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Air Supply — Drowning

### 1.1 Storage

Air supply is stored in **DataWatcher slot 1** (`short`, default `300`).

- `ia.Z()` → `ag.b(1)` = getAir.
- `ia.g(int)` → `ag.b(1, value)` = setAir.

Initialised: `ag.a(1, (short)300)` in constructor.

### 1.2 Drowning Tick Logic (from `nq.w()`)

Per tick, checked **before super.w()**:

```
Condition: K() [alive] AND a(p.g) [head in water] AND !h() [not fire immune]
           AND Water Breathing effect NOT active (abg.o.H = ID 13 not in bh map)
```

If condition is **true** (entity drowning):
1. `g(d(Z()))` — decrement air by 1 (via `d(int)` = subtract 1).
2. If current air `Z() == -20`:
   - Reset air to 0: `g(0)`.
   - Spawn 8 bubble particles.
   - Deal **2 drowning damage**: `a(pm.e, 2)` where `pm.e` = DamageSource.drown.
3. Extinguish fire: `y()`.

If condition is **false** (not drowning):
- `g(300)` — instantly restore air to full.

### 1.3 Summary of Air Cycle

| Air value | State |
|---|---|
| 300 | Full air, not in water |
| 1–299 | Depleting (1 per tick while head in water) |
| 0 | Air exhausted, about to damage |
| -1 to -19 | Air below zero (still depleting) |
| -20 | Deal 2 drowning damage; reset to 0 |

The entity takes 2 drowning damage every 20 ticks once air is depleted.
At 20 ticks per damage = 1 damage per second effectively, but in bursts of 2 every second.

---

## 2. Fire Damage

### 2.1 Fire Tick Storage

Fire ticks stored in private field `ia.c` (int):
- Positive: entity is burning.
- Zero or negative: not burning (negative = fire immunity period).

`ia.aa` = `fireResistance` seconds (default 1 second).

### 2.2 Fire Tick Logic (from `ia.w()`)

Per tick:
```
if (fireTicks > 0):
    if (af [fire immune]):
        fireTicks -= 4     // fire-immune entities shed fire 4× faster
        if (fireTicks < 0): fireTicks = 0
    else:
        if (fireTicks % 20 == 0):
            deal 1 fire damage (pm.b = DamageSource.onFire)
        fireTicks--
```

Fire damage: **1 HP every 20 ticks** = 1 HP/second (while burning).

### 2.3 Setting on Fire

`ia.e(int seconds)`:
```
fireTicks = max(fireTicks, seconds * 20)
```
Does not reduce if already burning longer.

### 2.4 Extinguishing Fire

**Water extinguish** (in `ia.w()` via `ia.b()` move end):
```
if (inWater && fireTicks > 0):
    play "random.fizz" sound
    fireTicks = -aa   // set immunity: e.g. -20 (1 second)
```

**Manual extinguish** `ia.y()`:
```
fireTicks = 0
```

### 2.5 Lava Exposure (from `ia.w()`)

`ia.a(su lavaSource)` is called by lava block's entity-collision callback:
```
a(pm.a, 5)    // deal 5 fall/generic damage if not fire-immune
c++           // increment lava counter
if (c == 0):  // when counter wraps through 0 (was at max negative)
    e(8)      // set on fire for 8 seconds
```

---

## 3. Fall Damage

### 3.1 Fall Distance Tracking — `ia.a(double dy, boolean onGround)`

Called at end of every move:
```
if (onGround):
    if (fallDistance > 0):
        c(fallDistance)    // deal fall damage
        fallDistance = 0
else if (dy < 0.0):
    fallDistance -= dy     // dy negative while falling → fallDistance increases
```

### 3.2 Fall Damage Application — `c(float fallDistance)` in `nq`

LivingEntity overrides `c()` to add armor reduction:
```
int damage = MathHelper.ceil(fallDistance - 3.0F)
if (damage > 0):
    deal damage (DamageSource.fall = pm.a?)
```

**Damage threshold:** 4 blocks of free-fall = first damage at exactly 4 blocks.
(Fall of 3.X = 0.X damage = rounded up to 1.)

### 3.3 Jump Boost Potion Effect

Jump Boost (abg.j, ID 8) reduces effective fall distance:
- Implementation: offset fallDistance by `(amplifier + 1) * some factor` in the fall check.
- Exact formula: see open question below.

### 3.4 Armor Fall Damage Reduction

Armor absorbs fall damage through the normal armor calculation path (same as any physical damage).
See LivingEntity damage pipeline (separate spec needed).

---

## 4. Suffocation in Blocks

When an entity's head is inside a solid opaque block:
- `ia.c(double x, double y, double z)` checks if entity center is in a solid block.
- If yes and entity is not dead: push the entity out via `u()` (nudge to open space).
- Suffocation damage is dealt by `nq` per tick via: `a(pm.d, 1)` = 1 HP per tick.

`pm.d` = DamageSource.inWall (suffocation damage source).

---

## 5. DamageSource Constants (`pm`)

| Field | Source | Notes |
|---|---|---|
| `pm.a` | generic / fall | Default physical damage |
| `pm.b` | onFire | Fire tick damage |
| `pm.c` | lava | Lava contact (instant damage) |
| `pm.d` | inWall | Suffocation in blocks |
| `pm.e` | drown | Drowning damage |
| `pm.l` | magic | Used by poison, harm potion |

> Open Question: confirm all DamageSource field names from `pm.java`.

---

## 6. Open Questions

| # | Question |
|---|---|
| 6.1 | `nq.c(float)` exact override — does it include armor reduction inline or via a separate method? |
| 6.2 | Fall threshold: is it exactly `fallDistance - 3.0F` or `fallDistance - 3`? |
| 6.3 | Jump Boost: where does it subtract from fallDistance — in nq.c() or in the entity tick? |
| 6.4 | `nq.h()` — is this the fire immunity check? Returns `af` field value? |
| 6.5 | `a(p.g)` on nq — is this "head in water" check? Confirm it tests eye height, not body. |
| 6.6 | Air supply for players: does the player HUD show the air bubble bar? Where is that drawn? |
| 6.7 | Suffocation: is the `u()` nudge called each tick? What direction does it push? |
