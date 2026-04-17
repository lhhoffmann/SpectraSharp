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

# BowArrow + FishingRod Spec
**Source classes:** `il.java` (ItemBow), `ro.java` (EntityArrow), `hd.java` (ItemFishingRod),
`ael.java` (EntityFishHook), `it.java` (EntitySkeleton)
**Superclasses:** `il` → `acy` (Item); `ro` → `ia` (Entity); `hd` → `acy`; `ael` → `ia`; `it` → `zo` (EntityMonster)
**Analyst:** lhhoffmann
**Date:** 2026-04-16
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

This spec covers the ranged combat and fishing systems:
- **`il` (ItemBow, ID 261)** — charge-based bow item that fires an EntityArrow
- **`ro` (EntityArrow, entity ID 10)** — projectile entity with gravity, drag, damage, critical hits, and player pickup
- **`hd` (ItemFishingRod, ID 346)** — casts/reels in an EntityFishHook with variable durability cost
- **`ael` (EntityFishHook)** — fish hook projectile with water detection, fish bite RNG, and entity hooking
- **`it` (EntitySkeleton, entity ID 51)** — hostile mob that fires arrows on a 60-tick cooldown; burns in daylight

---

## 2. Item IDs and Icon Positions

| Obf class | Human name     | Item ID | Icon (col, row) | Max Stack | Durability |
|-----------|----------------|---------|-----------------|-----------|-----------|
| `il`      | ItemBow        | 261     | col 5, row 1    | 1         | 384        |
| `acy.k`   | Arrow (item)   | 262     | col 5, row 2    | 64        | n/a        |
| `hd`      | ItemFishingRod | 346     | (default)       | 1         | 64         |
| `acy.aT`  | Raw Fish       | 349     | col 9, row 5    | 64        | n/a        |
| `acy.aW`  | Bone           | 352     | col 12, row 1   | 64        | n/a        |

Entity class → EntityList string IDs:
- `ro` → "Arrow" (entity string ID 10)
- `ael` (EntityFishHook) → **not registered** in EntityList (`afw`); no NBT persistence across sessions

---

## 3. Fields

### 3.1 `il` (ItemBow)

No instance fields beyond base `acy`. Relevant base fields set in constructor:
- `bN = 1` — max stack size 1
- `i(384)` — max durability 384

---

### 3.2 `ro` (EntityArrow) Fields

| Field (obf) | Type     | Default | Semantics                                              |
|-------------|----------|---------|--------------------------------------------------------|
| `e`         | int      | -1      | xTile — X of block arrow is stuck in                  |
| `f`         | int      | -1      | yTile — Y of block arrow is stuck in                  |
| `g`         | int      | -1      | zTile — Z of block arrow is stuck in                  |
| `h`         | int      | 0       | inTile — block ID of block arrow is stuck in          |
| `i`         | int      | 0       | inData — metadata of block arrow is stuck in          |
| `aq`        | boolean  | false   | inGround — true when arrow has struck a block         |
| `a`         | boolean  | false   | isPlayerArrow — true when shot by a `vi` (player)     |
| `b`         | int      | 0       | shake — post-impact frame counter; counts down from 7 |
| `c`         | `ia`     | null    | shooter entity reference                              |
| `ar`        | int      | 0       | ticksInGround — age counter while stuck; despawn at 1200 |
| `as`        | int      | 0       | ticksFlying — excludes shooter collision for first 5 ticks |
| `d`         | boolean  | false   | isCritical — triggers crit particles + bonus damage   |

---

### 3.3 `hd` (ItemFishingRod) Fields

No instance fields beyond base. Relevant base:
- `i(64)` — max durability 64
- `h(1)` — max stack 1
- `a() = true` — item enchantable
- `b() = true` — unknown flag (possibly "hasEffect" glow)

---

### 3.4 `ael` (EntityFishHook) Fields

| Field (obf) | Type     | Default | Semantics                                                     |
|-------------|----------|---------|---------------------------------------------------------------|
| `d`         | int      | -1      | xTile                                                         |
| `e`         | int      | -1      | yTile                                                         |
| `f`         | int      | -1      | zTile                                                         |
| `g`         | int      | 0       | inTile — block ID when stuck                                  |
| `h`         | boolean  | false   | inGround                                                      |
| `a`         | int      | 0       | shake (public)                                                |
| `b`         | `vi`     | null    | owner player (`b.ci = this` on construction)                  |
| `i`         | int      | 0       | ticksInGround — despawn at 1200                               |
| `aq`        | int      | 0       | ticksFlying                                                   |
| `ar`        | int      | 0       | bobCountdown — ticks until next fish bite attempt / ticks remaining until bite |
| `c`         | `ia`     | null    | hookedEntity — entity grabbed by hook                         |
| `as`–`aA`  | double   | —       | client-side interpolation fields (8 values)                   |

---

## 4. Constants & Magic Numbers

### 4.1 ItemBow

| Value  | Meaning                                                      |
|--------|--------------------------------------------------------------|
| `384`  | Bow durability. Each shot consumes 1 durability.             |
| `72000`| `getMaxItemUseDuration` in ticks (= 3600 s; effectively unlimited). `remainingTicks` passed to `onPlayerStoppedUsing` is subtracted from this to get `ticksCharged`. |
| `20.0F`| Ticks per second for charge fraction computation             |
| `0.1F` | Minimum power threshold below which no arrow is fired        |
| `1.0F` | Maximum (clamped) charge power; equals `d=true` (critical)  |
| `2.0F` | Speed multiplier applied to charge power for arrow velocity  |

### 4.2 EntityArrow

| Value    | Meaning                                                      |
|----------|--------------------------------------------------------------|
| `0.5F`   | Arrow hitbox size (width and height)                         |
| `1.5F`   | Speed multiplier inside `a(v,w,x,speed,spread)` called in constructor; `finalSpeed = inputSpeed × 1.5` |
| `0.0075F`| Gaussian spread per unit of `spread` parameter (accuracy noise) |
| `0.99F`  | Air drag factor per tick (each velocity component × 0.99)    |
| `0.8F`   | Water drag factor per tick                                   |
| `0.05F`  | Gravity per tick (subtracted from `w` = Y-velocity)         |
| `0.3F`   | Entity collision box expansion (each axis) when testing hits |
| `0.05F`  | Backstep distance when embedding in block (position − direction/mag × 0.05) |
| `7`      | Initial shake value on block impact (counts down to 0)       |
| `1200`   | Ticks in ground before despawn (60 seconds)                  |
| `5`      | TicksFlying threshold below which shooter collision is ignored |

### 4.3 ItemFishingRod

| Value | Meaning                                                             |
|-------|---------------------------------------------------------------------|
| `64`  | Fishing rod durability; cost depends on reel-in result (see §7.3)  |
| `0.4F`| Cast initial speed (direction vector × 0.4, then multiplied by 1.5 in `a()`) |

### 4.4 EntityFishHook

| Value  | Meaning                                                              |
|--------|----------------------------------------------------------------------|
| `0.25F`| Hook hitbox size                                                     |
| `1.5F` | Speed multiplier in `a()` called from constructor (same as arrow)    |
| `0.92F`| Air drag per tick                                                    |
| `0.5F` | Ground drag per tick (when `D=onGround || E=onGround` sides)         |
| `0.04F`| Water buoyancy coefficient applied to `w` proportional to submersion |
| `0.8`  | Water drag applied to `w` when partially submerged                   |
| `500`  | Default fish-bite roll: `nextInt(500) == 0`                          |
| `300`  | Fish-bite roll when sky directly visible above: `nextInt(300) == 0`  |
| `10–39`| Fish bite countdown range: `nextInt(30) + 10` ticks                 |
| `0.2F` | Downward velocity impulse on fish bite (w -= 0.2)                    |
| `1200` | Ticks stuck in block before hook despawns                            |
| `1024.0`| Distance-squared threshold to owner; removes hook if exceeded (>32 blocks) |

---

## 5. Methods — Detailed Logic

### 5.1 ItemBow (`il`)

#### `c(dk stack, ry world, vi player) → dk` — onItemRightClick

1. Check if player can charge:
   - `player.capabilities.d` (isCreativeMode) OR
   - `player.inventory.hasItem(arrowItemId)` — `player.by.c(acy.k.bM)`
2. If yes: `player.setItemInUse(stack, 72000)` — registers the item for use animation and starts charge timer.
3. Return `stack` unchanged.

#### `a(dk stack, ry world, vi player, int remainingTicks)` — onPlayerStoppedUsing

Called when the player releases right-click.

1. `ticksCharged = 72000 − remainingTicks`
2. `chargeFraction = float(ticksCharged) / 20.0F`
3. `power = (chargeFraction² + chargeFraction × 2.0F) / 3.0F`
4. If `power < 0.1F`: return (no shot).
5. `if (power > 1.0F): power = 1.0F`
6. Check condition: Creative OR has arrows (same check as `c()`). If false: return.
7. Create arrow: `new ro(world, player, power × 2.0F)`
8. If `power == 1.0F`: set `arrow.d = true` (critical).
9. Damage bow: `stack.a(1, player)` = 1 durability consumed.
10. Play sound: `"random.bow"` at player position, volume `1.0F`, pitch = `1.0F / (rng.nextFloat() × 0.4F + 1.2F) + power × 0.5F`.
11. Consume 1 arrow from inventory: `player.inventory.consumeItem(acy.k.bM)` = `player.by.b(acy.k.bM)`.
12. If server-side: `world.spawnEntity(arrow)`.

#### `b(dk stack) → int` — getMaxItemUseDuration

Returns `72000`.

#### `c(dk stack) → ps` — getItemUseAction

Returns `ps.e` (bow-draw use animation).

#### `a(dk, vi, ry, int, int, int, int) → boolean` — onItemUse (block placement)

Always returns `false`. Bow cannot be used on blocks.

---

### 5.2 EntityArrow (`ro`) — Constructor `ro(ry world, nq shooter, float speed)`

1. `owner (c) = shooter`
2. `isPlayerArrow (a) = (shooter instanceof vi)`
3. Size = 0.5×0.5
4. Eye-level starting position:
   - `s = shooter.s − sin(yaw) × cos(pitch) × 0.16`
   - `t = shooter.t + shooter.eyeHeight − 0.1`
   - `u = shooter.u − cos(yaw) × cos(pitch) × 0.16`
   - `L = 0.0F` (override to zero)
5. Set initial direction as unit-sphere vector × speed, then call `a(v, w, x, speed×1.5, 1.0)` to normalize and apply final velocity. See §5.2.1.

**Note on actual final speed:**
The direction components in step 4 already have magnitude ≈ `speed`. `a()` normalizes them (÷ speed) then multiplies by `speed × 1.5`. Net result: final velocity magnitude ≈ `speed × 1.5`.

From bow at full charge: `speed = 1.0 × 2.0 = 2.0`; final velocity = `2.0 × 1.5 = 3.0` blocks/tick.

#### 5.2.1 `a(double vx, vy, vz, float speed, float spread)` — setShootingVector

1. Normalize: `len = sqrt(vx² + vy² + vz²)`; each ÷ len.
2. Add Gaussian noise per component: `nextGaussian() × 0.0075F × spread`.
3. Multiply each by `speed`.
4. Assign to `v, w, x`.
5. Compute `yaw = atan2(v, x) × 180/π`; `pitch = atan2(w, sqrt(v²+x²)) × 180/π`.
6. Assign `A = y = yaw`, `B = z = pitch`.
7. Reset `ar = 0`.

---

### 5.3 EntityArrow (`ro`) — Tick (`a()`)

All tick logic runs in the overridden `a()` method.

**A. Orientation initialisation** (first tick only, when `B == 0 && A == 0`):
- Recompute yaw/pitch from current velocity.

**B. Stuck-in-ground state (`aq == true`):**

1. Verify the block at `(e, f, g)` has the same ID as `h` and same meta as `i`. If changed:
   - `aq = false`; scatter velocity: each × `nextFloat() × 0.2`; reset `ar = 0`, `as = 0`.
2. If still same block:
   - Increment `ar`. If `ar == 1200`: `remove()`.
3. If `b > 0`: decrement `b` (shake countdown).

**C. In-flight state (`aq == false`):**

Increment `as` (ticksFlying).

1. **Block collision ray-trace:**
   - Ray from current pos to `pos + velocity`.
   - `world.rayTrace(start, end, false, true)` = `world.a(start, end, false, true)`.

2. **Entity collision scan:**
   - Get entities in expanded AABB: current AABB expanded by velocity, then +1.0 on each axis.
   - For each entity: skip if `!e_()` (not targetable); skip if `entity == shooter AND as < 5`.
   - Test each entity's AABB expanded by 0.3 for ray intersection.
   - Select nearest entity by distance-squared to start point.

3. **Process nearest hit (block or entity):**

   **Entity hit:**
   - `speed = sqrt(v² + w² + x²)`
   - `damage = ceil(speed × 2.0)`
   - If `d (isCritical)`: `damage += nextInt(damage/2 + 2)` (random bonus)
   - DamageSource: `pm.a(this, shooter)` = arrow source with shooter attribution
   - `target.a(source, damage)`:
     - If damage applied successfully: `target.knockback++ (bf++)` if LivingEntity; play "random.bowhit"; remove arrow (`v()`).
     - If damage blocked: reflect arrow — `v × −0.1, w × −0.1, x × −0.1`; rotate `y += 180, A += 180`; reset `as = 0`.

   **Block hit:**
   - Record: `e/f/g = blockX/Y/Z`, `h = blockId`, `i = blockMeta`.
   - Zero velocity to hit-point delta ÷ magnitude × 0.05 for embedding position.
   - Set `aq = true`, `b = 7` (shake = 7), `d = false`.
   - Play "random.bowhit".

4. **Critical particles** (if `d == true` and in-flight):
   - 4 "crit" particles along the arrow trail per tick.

5. **Physics update** (always, after collision handling):
   - `s += v; t += w; u += x` (position advance)
   - Recompute rotation from velocity.
   - Smooth rotation: `z = prevZ + (z − prevZ) × 0.2; y = prevY + (y − prevY) × 0.2`
   - Apply drag:
     - `drag = 0.99F` (air); if in water: `drag = 0.8F` (4 "bubble" particles emitted)
     - `v *= drag; w *= drag; x *= drag`
   - Apply gravity: `w −= 0.05F`
   - Update entity position and bounding box.

**D. Pickup check (`a(vi player)`):**
- Condition: `aq == true AND a == true AND b <= 0 AND player.inventory.addItem(new arrow item, 1)`
- If all true: play "random.pop" sound; `player.addStat(1, this)`; remove arrow.

---

### 5.4 EntityArrow (`ro`) — NBT

**Write (`a(ik tag)`):**
```
"xTile" → short(e)
"yTile" → short(f)
"zTile" → short(g)
"inTile" → byte(h)
"inData" → byte(i)
"shake" → byte(b)
"inGround" → byte(aq ? 1 : 0)
"player" → boolean(a)
```

**Read (`b(ik tag)`):**
```
e = tag.getShort("xTile")
f = tag.getShort("yTile")
g = tag.getShort("zTile")
h = tag.getByte("inTile") & 255
i = tag.getByte("inData") & 255
b = tag.getByte("shake") & 255
aq = tag.getByte("inGround") == 1
a = tag.getBoolean("player")
```

**Missing from NBT** (not saved/loaded):
- `c` (shooter entity reference) — lost on save; arrow becomes ownerless
- `d` (isCritical) — not saved
- `ar` (ticksInGround) — not saved; resets on reload
- `as` (ticksFlying) — not saved

---

### 5.5 ItemFishingRod (`hd`) — onItemRightClick `c(dk, ry, vi)`

1. Check if player already has a hook cast (`player.ci != null`):

   **Reel-in path:**
   - `durabilityDamage = player.ci.g()` — calls `ael.g()` (see §5.7)
   - `stack.a(durabilityDamage, player)` — damages rod by the returned amount
   - `player.m_()` — swing arm / trigger use animation

   **Cast path:**
   - Play sound: `"random.bow"` at player, volume `0.5F`, pitch randomised
   - If server-side: `world.spawnEntity(new ael(world, player))`
   - `player.m_()` — swing arm

2. Return `stack` unchanged.

---

### 5.6 EntityFishHook (`ael`) — Constructor `ael(ry world, vi player)`

1. Set owner: `b = player; player.ci = this`
2. Size = 0.25×0.25
3. Starting position: `player.eyePos − 0.1` (same offset as arrow)
4. Initial velocity: direction × `0.4F`, then `a(v, w, x, 1.5F, 1.0F)` → final ≈ 0.6 blocks/tick

---

### 5.7 EntityFishHook (`ael`) — `g() → int` — reelIn

Returns the rod durability cost.

```
durability = 0
if (hookedEntity != null):
    pull hooked entity toward player:
        dx = player.s − hook.s
        dy = player.t − hook.t
        dz = player.u − hook.u
        dist = sqrt(dx²+dy²+dz²)
        entity.v += dx × 0.1
        entity.w += dy × 0.1 + sqrt(dist) × 0.08
        entity.x += dz × 0.1
    durability = 3
else if (ar > 0):    // fish bite active
    spawn EntityItem(acy.aT = RawFish) at hook position
    velocity toward player: v = dx×0.1, w = dy×0.1+sqrt(dist)×0.08, x = dz×0.1
    player.addStat(ny.B, 1)    // awards XP (confirm: ny.B = stat)
    durability = 1
if (h == true):    // hook stuck in block
    durability = 2
destroy hook entity
player.ci = null
return durability
```

---

### 5.8 EntityFishHook (`ael`) — Tick Physics

**If server-side and hook is not in client interpolation (`as == 0`):**

**Auto-remove check:**
```
if (player.isDead || !player.isAlive || heldItem == null || heldItem.item != fishingRodItem || distance² > 1024):
    remove hook; player.ci = null; return
```

**If hookedEntity (`c`) is set:**
- If entity dead (`c.K` = isDead): clear `c = null`.
- Else: lock hook to entity position: `s = c.s; t = c.AABB.minY + c.N × 0.8; u = c.u`. Return.

**If stuck in block (`h == true`):**
- Verify block at (d, e, f) still matches id `g`. If not: unstick, scatter velocity.
- If still stuck: increment `i`. If `i == 1200`: remove.

**In-flight physics:**

1. Ray-trace for block and entity hits (same algorithm as EntityArrow §5.3, step 1-3).

   **Entity hit:** `target.a(DamageSource.arrow(this, player), 0)` — 0 damage. If hit: `c = target` (hook attaches).
   **Block hit:** `h = true` (stuck).

2. **Water submersion check:**
   - Divide hook's vertical AABB into 5 equal segments.
   - For each segment: check if `world.containsLiquid(bb, Material.water)` = `world.b(bb, p.g)`.
   - `submersionFraction = count / 5.0` (0.0 to 1.0).

3. **Fish bite logic** (only if `submersionFraction > 0.0`):

   If `ar > 0`: decrement `ar`.
   Else:
   - Roll: `nextInt(500)`. If sky visible directly above: `nextInt(300)`.
   - If roll == 0:
     - `ar = nextInt(30) + 10` (bite countdown)
     - `w −= 0.2F` (hook dips down)
     - Play "random.splash" at hook
     - Spawn bubble+splash particles (count = `1 + hook.width×20`)

   If `ar > 0`: apply extra downward force: `w −= nextFloat() × nextFloat() × nextFloat() × 0.2`

4. **Buoyancy + drag:**
   - `buoyancy = submersionFraction × 2.0 − 1.0`
   - `w += 0.04F × buoyancy`
   - If `submersionFraction > 0.0`: `drag × 0.9`; `w × 0.8`
   - `v × drag; w × drag; x × drag` (`drag = 0.92F`, or `0.5F` if on ground/wall)
   - If not stuck: update position.

---

## 6. EntitySkeleton (`it`)

### 6.1 Fields

| Field (obf) | Type  | Default | Semantics                                    |
|-------------|-------|---------|----------------------------------------------|
| `aT`        | int   | 0       | Attack cooldown; reload time between shots   |

Skeleton holds: `new dk(acy.j, 1)` = 1× bow in hand slot (`s()` returns it).

### 6.2 Arrow Attack (`a(ia target, float distance)`)

Called when `distance < 10.0F` (targeting range).

1. If `aT == 0`:
   a. Create arrow: `new ro(world, this, 1.0F)`
   b. Aim elevation correction:
      - `xzDist = sqrt((target.x − skeleton.x)² + (target.z − skeleton.z)²)`
      - `dy = target.y + target.eyeHeight − 0.7 − arrow.y` = adjusted target Y
      - `arcCorrection = xzDist × 0.2F`
   c. Fire arrow: `arrow.a(dx, dy + arcCorrection, dz, 1.6F, 12.0F)` — speed 1.6, spread 12.0
   d. Play "random.bow" sound
   e. Spawn arrow
   f. `aT = 60` (60-tick reload)
2. Face target: `yaw = atan2(dz, dx) × 180/π − 90`; `isAggressive = true`.

### 6.3 Sunlight Burning (`c()`)

Called once per tick (mob AI update).

1. `lightLevel = skeleton.getBrightness(1.0F)`
2. If `world.isDaytime() AND lightLevel > 0.5F AND world.canSeeSky(skeleton_pos)`:
   - `roll = nextFloat() × 30.0F`
   - If `roll < (lightLevel − 0.4F) × 2.0F`: set skeleton on fire (`e(8)` = 8 ticks)
3. Proceed with normal AI tick (`super.c()`).

### 6.4 Drops (`a(boolean killed, int lootLevel)`)

```
arrowCount = nextInt(3 + lootLevel)
for each: dropItem(acy.k.bM, 1)  // arrows

boneCount = nextInt(3 + lootLevel)
for each: dropItem(acy.aW.bM, 1) // bones
```

Ranges: 0 to (2 + lootLevel) of arrows and bones each.

### 6.5 Skeleton Achievement Trigger

In `a(pm damageSource)` — called when skeleton is hurt:
- If damageSource projectile is `ro` (arrow) AND original attacker is `vi` (player):
  - `dx = player.x − skeleton.x`, `dz = player.z − skeleton.z`
  - If `dx² + dz² >= 2500.0` (≥ 50 blocks horizontal):
    - `player.triggerAchievement(ut.v)` — Sniper achievement

---

## 7. Bitwise & Data Layouts

### 7.1 Arrow — no metadata.

### 7.2 Bow charge power formula

Input: `ticksCharged` (ticks bow was held, 0 to 72000)

```
f = ticksCharged / 20.0          (seconds held)
power = (f*f + f*2) / 3.0        (cubic-ish curve; reaches 1.0 at f=1.0, i.e., 20 ticks)
if power < 0.1: no shot
power = min(power, 1.0)
arrowSpeed = power * 3.0         (blocks/tick after 1.5× constructor multiplier)
baseDamage = ceil(arrowSpeed * 2.0)
if critical: baseDamage += nextInt(baseDamage/2 + 2)
```

Minimum shot: `power = 0.1` → speed = 0.3 → damage = ceil(0.6) = 1
Full charge: `power = 1.0` (at exactly 20 ticks) → speed = 3.0 → damage = 6 + nextInt(5)

### 7.3 Fishing rod damage per reel-in

| Return | Condition | Durability cost |
|--------|-----------|-----------------|
| 0      | Hook empty, not in block | 0 |
| 1      | Fish caught (ar > 0) | 1 |
| 2      | Hook stuck in block | 2 |
| 3      | Entity hooked | 3 |

Note: The `h (inGround)` check is a separate `if`, not `else if`. Priority: ground check overrides fish/entity values.

---

## 8. Tick Behaviour

- **ItemBow** — not ticked; event-driven via `onItemRightClick` + `onPlayerStoppedUsing`.
- **EntityArrow** — ticked every game tick via `a()` until removed.
- **ItemFishingRod** — not ticked.
- **EntityFishHook** — ticked every game tick via `a()` until removed.
- **EntitySkeleton** — ticked via mob AI; arrow attack checked in melee range method `a(ia, float)`.

---

## 9. Known Quirks / Bugs to Preserve

1. **Arrow NBT loses shooter.** The shooter entity reference (`c`) is not serialised. On world reload, an in-flight arrow becomes ownerless — critical status, duplicate detection, and player attribution are all lost.

2. **Critical flag not serialised.** `d = true` (isCritical) is not written to NBT. A critical arrow that outlasts a session (embedded in a block) would no longer be critical if the world is reloaded before it is picked up.

3. **Arrow `shake` field (`b`) double-entry.** In NBT write (`a(ik)`), `b` is written as "shake". In the tick's in-ground logic, `b` is decremented separately from `ar`. Shake counts down from 7 (visual wobble), independent of despawn timer.

4. **Fishing hook NOT in EntityList.** `ael` is not registered in `afw` (EntityList). It has no string entity ID. This means fish hooks cannot be saved/loaded from NBT — if the world saves while a hook is in the air, it is lost on reload. The rod will have `ci != null` pointing to a dead entity that no longer exists in the world.

5. **Skeleton spread = 12.0.** Skeletons fire with spread factor 12.0 (vs 1.0 for player arrows). This makes skeleton arrows very inaccurate at long range but they still aim for the target Y-position at any range.

6. **Sunlight burn is per-tick random.** The skeleton burn check runs every tick, not every N ticks. At high daylight levels, the probability per tick = `(lightLevel − 0.4) × 2 / 30`. At full light (1.0): probability ≈ `0.6 × 2 / 30 ≈ 4%` per tick. Sets fire for 8 ticks (not long, but rerolled each tick).

7. **Fishing rod swing arm in both paths.** `player.m_()` is called both when casting AND reeling. On cast, this swings the arm as expected. On reel-in, the arm also swings.

8. **Fish bite downward force with `ar` check mismatch.** The downward extra force (`w -= rand³ × 0.2`) runs whenever `ar > 0`, which includes the post-bite countdown phase AND the moment the bite activates. The force also runs during the tick the countdown expires (after decrement to 0 in the same tick), creating a 1-tick gap.

---

## 10. Open Questions

1. **`ny.B`** — the stat added on fish catch (`player.addStat(ny.B, 1)`). What is `ny.B`? If it is an XP stat, it grants 1 XP point per fish caught. Confirm `ny.B` by reading `ny.java`.

2. **`ps.e`** — the use action enum for bow. What is the full `ps` enum? Confirm `ps.e` = BOW animation. Other values likely: NONE, EAT, DRINK, BLOCK.

3. **`player.by.c(id)` vs `player.by.b(id)`** — `c()` checks if inventory contains item; `b()` consumes one. Confirm method signatures on the inventory class.

4. **`player.m_()`** — what exactly does this do? Swing item arm animation? Or something else?

5. **`world.b(bb, p.g)`** — confirms `world.b(AABB, Material)` = `containsMaterial`. `p.g` = `Material.water`. Confirm.

6. **`entity.N`** — in `ael` hook position tracking: `hook.t = hooked.AABB.minY + hooked.N × 0.8`. What is `ia.N`? Entity height / bounding box height? Likely `entity.height`.

7. **`acy.j` icon position** — `a(5, 1)`: is icon at items.png column 5, row 1 (0-indexed)? Or is it row-major? Confirm the icon index formula for items.

8. **Skeleton attack range** — `distance < 10.0F` in the range-check. Is this block distance or squared? The parameter `var2` passed is `float var2` — need to confirm if pre-squared or linear.

9. **`this.b(1.0F)` in skeleton sunlight check** — `var1 = getBrightness(1.0F)`. What does the `1.0F` argument mean? Partial-tick interpolation? Confirm.

10. **`world.l(x, y, z)`** — used by skeleton to check `canSeeSky`. Confirm this is `isSkyVisible` / `canBlockSeeTheSky`.
