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

# EnderDragon Spec
**Source classes:** `oo.java` (EntityDragon), `adh.java` (EntityBoss base), `vc.java` (EntityBodyPart), `sf.java` (EntityEnderCrystal)
**Superclass:** `adh` (= EntityBoss) → `nq` (= LivingEntity)
**Analyst:** lhhoffmann
**Date:** 2026-04-16
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

```
nq (LivingEntity)
  └─ adh (EntityBoss)
        └─ oo (EntityDragon)

ia (Entity)
  └─ vc (EntityBodyPart)   — 7 instances owned by oo

ia (Entity)
  └─ sf (EntityEnderCrystal)
```

---

## 2. EntityBoss Base (`adh.java`)

A thin wrapper over `nq` that:
- Holds `bI` (maxHealth, default 100)
- Returns `bI` from `f_()` (getMaxHealth)
- **Overrides `a(pm, int)` to return false** — the boss is immune to all direct damage
- Provides `e(pm, int)` = `super.a(pm, int)` as a bypass — subclasses call this to actually apply damage
- Accepts `a(vc bodyPart, pm source, int damage)` as the entry point for body-part hits; default implementation just calls `a(source, damage)` (blocked by the immunity)

EntityDragon (`oo`) overrides `a(vc, pm, int)` with conditional logic and calls `e(pm, int)` to bypass the immunity.

---

## 3. EntityDragon (`oo.java`)

### 3.1 Identity

- Entity class: `oo`
- EntityList name: `"EnderDragon"`, ID 63
- Texture: `"/mob/enderdragon/ender.png"`
- Hitbox: 16.0 × 8.0 (set via `a(16.0F, 8.0F)`)
- Max health (`bI`): 200
- Fire immune: `W = true`
- Explosion immune: `ao = true`
- Not pushed by water: `e_() = false`
- Cannot trigger walking: `d_() = false` (inferred)

### 3.2 Fields

| Field | Type | Default | Meaning |
|-------|------|---------|---------|
| `a` | double | 0.0 | target X position |
| `b` | double | 100.0 | target Y position |
| `c` | double | 0.0 | target Z position |
| `d[64][3]` | double[][] | — | ring buffer: d[i][0]=yaw, d[i][1]=Y, d[i][2]=unused |
| `e` | int | −1 | current ring buffer write index |
| `f[]` | vc[7] | see §3.3 | body part array |
| `g` | vc | — | head part (6.0×6.0) |
| `h` | vc | — | body part (8.0×8.0) |
| `i` | vc | — | tail segment 1 (4.0×4.0) |
| `by` | vc | — | tail segment 2 (4.0×4.0) |
| `bz` | vc | — | tail segment 3 (4.0×4.0) |
| `bA` | vc | — | left wing (4.0×4.0) |
| `bB` | vc | — | right wing (4.0×4.0) |
| `bC` | float | 0.0 | wing flap amount — previous tick |
| `bD` | float | 0.0 | wing flap amount — current tick |
| `bE` | boolean | false | arrival/stuck flag — triggers new waypoint |
| `bF` | boolean | false | inBlock flag (head or body inside solid) |
| `bJ` | ia | null | current target entity (player) |
| `bG` | int | 0 | death animation tick counter |
| `bH` | sf | null | nearest Ender Crystal being focused |

### 3.3 DataWatcher

| Slot | Type | Meaning |
|------|------|---------|
| 16 | Integer | current health (synced for boss health bar display) |

DataWatcher slot 16 is written in `b()` (initDataWatcher) with value `bI` (200), and updated each tick on the server via `ag.b(16, aM)`.

### 3.4 Body Part Array

All 7 parts are created in the constructor and stored in `f[]`:

```
f[0] = g = head     (6.0×6.0)
f[1] = h = body     (8.0×8.0)
f[2] = i = tail[0]  (4.0×4.0)
f[3] = by = tail[1] (4.0×4.0)
f[4] = bz = tail[2] (4.0×4.0)
f[5] = bA = wing-L  (4.0×4.0)
f[6] = bB = wing-R  (4.0×4.0)
```

`ab()` returns `f` so the World entity list can track all 7 parts as individual entities.

---

## 4. Tick Logic — `c()` (main update method)

### 4.1 Dead state (`aM <= 0`)

When health reaches 0, normal tick spawns random "largeexplode" particles at the dragon's position
with offsets: X ± 0–4.0, Y ± 0–2.0 offset upward, Z ± 0–4.0.

### 4.2 Alive state

Each tick in order:

**a) Wing flap animation:**

```
bC = bD   // save previous value

wingSpeed = 0.2 / (length(v, x) * 10.0 + 1.0) * 2^(w)   // w = vertical velocity
if bF (inBlock):
    bD += wingSpeed * 0.5
else:
    bD += wingSpeed
```

**b) Position ring buffer:**

On each tick, if `e < 0`: pre-fill all 64 entries with current yaw and Y.
Then: `e = (e + 1) % 64`; write `d[e][0] = y` (yaw), `d[e][1] = t` (Y position).

**c) Client-side interpolation:**

On client, use the standard `bi` (interpolation steps) counter to lerp position and yaw from the last server packet.

**d) Server-side AI:**

*Target update:*
- If `bJ != null` (has player target):
  - Set `a = bJ.s`, `c = bJ.u` (follow XZ)
  - Compute XZ distance to player: `var14 = sqrt(dx² + dz²)`
  - Target Y: `b = bJ.C.b + clamp(0.4 + var14/80.0 - 1.0, max=10.0)` (above player, scales with distance)
- If `bJ == null` (random wander):
  - `a += nextGaussian() * 2.0`
  - `c += nextGaussian() * 2.0`

*Waypoint refresh trigger:* if `bE` (stuck flag) OR distance² < 100 OR distance² > 22500 OR `E` (in water) OR `F` (in lava): call `aA()` (pick new waypoint).

*Y-axis velocity adjustment:*

```
dy = (target.b - t) / sqrt(target.a-s)²+(target.c-u)²)
clamp dy to [-0.6, +0.6]
w += dy * 0.1
```

*Yaw steering:*

```
targetYaw = 180 - atan2(dx, dz) * (180/π)
delta = targetYaw - y   // normalized to [-180, +180]
clamp delta to [-50, +50]
speed = sqrt(v²+x²)
var19 = clamp(speed + 1.0, max=40.0)
bt += delta * 0.7 / var19 / (speed+1.0)
y += bt * 0.1
```

*Forward thrust:*

```
forwardDir = normalize(sin(y), w, -cos(y))
toTarget = normalize(target - pos)
alignment = dot(forwardDir, toTarget)     // -1 to +1
var17 = clamp((alignment + 0.5) / 1.5, min=0.0, max=1.0)

baseThrust = 0.06F
speedFactor = 2.0 / (speed + 1.0)
thrustMag = baseThrust * (var17 * speedFactor + (1.0 - speedFactor))

applyForce(0.0, -1.0, thrustMag)   // accelerate in forward direction
```

*Drag:*

```
velocity_forward = dot(normalize(v,w,x), forwardDir)
speedMultiplier = 0.8 + 0.15 * (velocity_forward + 1.0) / 2.0
v *= speedMultiplier
x *= speedMultiplier
w *= 0.91
```

If `bF` (in block): multiply v/w/x by 0.8 instead.

### 4.3 Body Part Positioning

After AI, update part AABBs using the ring buffer:

**AABB sizes (width/height, set per tick):**

| Part | M (width) | N (height) |
|------|-----------|------------|
| head (g) | 3.0 | 3.0 |
| body (h) | 5.0 | 3.0 |
| tail1 (i) | 2.0 | 2.0 |
| tail2 (by) | 2.0 | 2.0 |
| tail3 (bz) | 2.0 | 2.0 |
| wingL (bA) | 4.0 | 2.0 |
| wingR (bB) | 4.0 | 3.0 |

**Body (h):** positioned 0.5 blocks behind the dragon along its yaw.

**Wings:**
- Left (bA): `(s + sin(y)*4.5, t+2.0, u + cos(y)*4.5)`
- Right (bB): `(s - sin(y)*4.5, t+2.0, u - cos(y)*4.5)`

**Head (g):** uses ring buffer entries 0 and 5 to compute pitch angle for the neck:
```
pitch = atan((d[5][1] - d[10][1]) * 10 / 180 * π)
var3 = cos(pitch)
var4 = -sin(pitch)
yaw_adjusted = y - bt * 0.01
head_x = s + sin(yaw_adjusted) * 5.5 * var3
head_y = t + (d[0][1] - d[5][1]) + var4 * 5.5
head_z = u - cos(yaw_adjusted) * 5.5 * var3
```

**Tail segments (i, by, bz):** indexed at ring buffer positions 12, 14, 16 respectively.
Each tail segment's yaw is computed from the angle between its ring position and the head ring position.

### 4.4 Block Destruction — `a(AABB aabb)`

Called when the head or body AABB intersects the world.

For every block position within the AABB:
- If block == 0: skip
- If block == obsidian (ID 49) OR bedrock (ID 7) OR end portal (ID 119): **do not destroy**; set `var8 = true`
- Otherwise: destroy block (`world.setBlock(x, y, z, 0)`); spawn "largeexplode" particle at a random point within the AABB

Returns `var8` (true if the AABB hit any indestructible block).

The return value controls `bF` (inBlock):
```
bF = a(head.AABB) | a(body.AABB)
```

### 4.5 Entity Collision — Per-Tick

Called every tick on server when `aQ == 0`:

**Wing entities** (from `a(List)`) — entities in wing AABBs inflated by 4×2×4 and shifted -2 Y:
```
for each living entity:
    dx = entity.x - body.center.x
    dz = entity.z - body.center.z
    distSq = dx² + dz²
    entity.addVelocity(dx/distSq * 4.0, 0.2, dz/distSq * 4.0)
```

**Head entities** (from `b(List)`) — entities in head AABB inflated by 1×1×1:
```
for each living entity:
    entity.attackEntityFrom(DamageSource.causeMobDamage(dragon), 10)
```

---

## 5. Crystal Healing — `ay()`

Called every tick when alive.

**Crystal targeting (every 10 ticks):**
```
if nextInt(10) == 0:
    scan world for sf entities (EnderCrystal) within 32 blocks
    bH = nearest crystal by distance²
```

**Healing from crystal:**
```
if bH != null and NOT bH.isDead:
    if tickAge % 10 == 0 AND health < maxHealth:
        health++    // heal 1 HP every 10 ticks
else if bH != null and bH.isDead:
    a(head, DamageSource.MAGIC, 10)  // take 10 damage when focused crystal dies
    bH = null
```

So: the dragon heals 1 HP every 10 ticks (0.5 HP/second) while a crystal is alive and targeted. Destroying a crystal that the dragon is focused on deals 10 damage to the dragon.

---

## 6. Damage Routing — `a(vc bodyPart, pm source, int damage)`

Called when any of the 7 body parts is hit.

```
if bodyPart != head (g):
    damage = damage / 4 + 1    // quarter damage for non-head, minimum 1

// Redirect target toward new random point near head:
yaw_rad = y * π / 180
a = s + sin(yaw_rad) * 5.0 + nextFloat(-1, +1)
b = t + nextFloat * 3.0 + 1.0
c = u - cos(yaw_rad) * 5.0 + nextFloat(-1, +1)
bJ = null

if source.attacker is EntityPlayer (vi) OR source == DamageSource.FIRE:
    e(source, damage)   // bypass immunity and apply damage
```

Damage from non-player non-fire sources is completely absorbed (no call to `e()`).

---

## 7. New Waypoint — `aA()`

```
bE = false

if nextInt(2) == 0 AND world.playerList.size() > 0:
    bJ = random player from world.i (player list)
else:
    repeat:
        a = 0.0
        b = 70.0 + nextFloat * 50.0   // Y in [70, 120]
        c = 0.0
        a += nextFloat * 120.0 - 60.0  // X in [-60, +60]
        c += nextFloat * 120.0 - 60.0  // Z in [-60, +60]
        dx = s - a; dy = t - b; dz = u - c
    until (dx² + dy² + dz²) > 100.0   // must be at least ~10 blocks away
    bJ = null
```

---

## 8. Death Sequence — `ad()`

Called every tick once health reaches 0 (overrides `nq` death method).

```
bG++

if bG in [180, 200]:
    spawn "hugeexplosion" particle at (s ± 0–4, t + 2.0 ± 0–2, u ± 0–4)

if server AND bG > 150 AND bG % 5 == 0:
    spawn XP orbs totalling 1000 XP at (s, t, u)
    (each orb value chosen via fk.b(remaining) — rounds down to nearest tier)

apply velocity (0.0, +0.1, 0.0)   // drift upward
y += 20.0; at = y                 // rotate 20° per tick

if bG == 200:
    spawn XP orbs totalling 10000 XP at (s, t, u)
    generate exit portal at (floor(s), floor(u))
    drop items: none (al() is empty)
    remove entity from world
```

**Total XP dropped:**
- 9 × 1000 = 9000 XP (ticks 155, 160, 165, 170, 175, 180, 185, 190, 195)
- 1 × 10000 XP (tick 200)
- **Total: 12000 XP**

Wait — counting precisely: bG > 150 means bG ≥ 151. bG % 5 == 0 means bG ∈ {155, 160, 165, 170, 175, 180, 185, 190, 195, 200}. But bG=200 also triggers the final block (if bG == 200). Looking at code order: the `% 5 == 0` block runs first (spawns 1000 XP at tick 200), then the `if bG == 200` block (spawns 10000 XP). So at tick 200 both fire.

**Precise XP total:** 10 × 1000 + 10000 = **20000 XP**

---

## 9. Exit Portal Generator — `a(int x, int z)` (internal method)

Called at death tick 200 to place the end exit portal at the dragon's floor position.

`var3 = world.c / 2` — world height is 128, so `var3 = 64`.

```
aid.a = true    // BlockEndPortal.staticActive = true (suppress entity teleport during construction)
var4 = 4        // portal radius
```

**Loop:** Y from (var3 − 1) = 63 to (var3 + 32) = 96; XZ within var4 = 4 blocks:

For each position where `sqrt(dx² + dz²) ≤ 3.5`:

| Y | sqrt(dx²+dz²) | Block placed |
|---|---|---|
| Y < 64 (Y=63) | ≤ 2.5 | Bedrock (ID 7) |
| Y < 64 (Y=63) | > 2.5 | nothing |
| Y = 64 | > 2.5 | Bedrock (ID 7) — outer ring |
| Y = 64 | ≤ 2.5 | End Portal (ID 119) — inner disc |
| Y > 64 | ≤ 3.5 | Air (ID 0) — clear column |

**Fixed structure (placed after loop, overrides center):**

| Position | Block |
|----------|-------|
| (x, 64, z) | Bedrock (ID 7) |
| (x, 65, z) | Bedrock (ID 7) |
| (x, 66, z) | Bedrock (ID 7) |
| (x−1, 66, z) | Torch (ID 50) |
| (x+1, 66, z) | Torch (ID 50) |
| (x, 66, z−1) | Torch (ID 50) |
| (x, 66, z+1) | Torch (ID 50) |
| (x, 67, z) | Bedrock (ID 7) |
| (x, 68, z) | Dragon Egg (ID 122) |

```
aid.a = false    // restore
```

Result: a circular disc of End Portal blocks at Y=64 (radius ~2.5 blocks), ringed by bedrock, with a 5-block-tall bedrock pillar at the center topped with 4 torches and a dragon egg.

---

## 10. EntityBodyPart (`vc.java`)

A multi-part collision entity owned by EntityDragon.

### Fields

| Field | Type | Meaning |
|-------|------|---------|
| `a` | adh | parent dragon |
| `b` | String | part name ("head", "body", "tail", "wing") |

### Behaviour

- Shares the parent's world but is a separate entity in the world list
- `e_()` = true (always within render range)
- No NBT save/load (empty overrides for `a(NbtCompound)` and `b(NbtCompound)`)
- No DataWatcher entries (empty `b()`)
- `a(pm, int)` → delegates to `parent.a(this, pm, int)` — all hits are routed through the dragon's damage filter
- `h(ia other)` = part-of-same-team check: returns true if `other == this` OR `other == parent dragon`

---

## 11. EntityEnderCrystal (`sf.java`)

### Identity

- Entity class: `sf`
- EntityList name: `"EnderCrystal"`, ID 200 (to be confirmed — not checked in afw.java)
- Hitbox: 2.0 × 2.0; eye height = N/2 (center)
- `l = true` (no clip — passes through blocks)
- `e_()` = true

### Fields

| Field | Type | Default | Meaning |
|-------|------|---------|---------|
| `a` | int | nextInt(100000) | tick counter (random start offset for beam animation) |
| `b` | int | 5 | health / state (synced via DataWatcher slot 8) |

### DataWatcher

| Slot | Type | Meaning |
|------|------|---------|
| 8 | Integer | health/state value `b` |

### Tick — `a()`

Each tick:
1. `p = s; q = t; r = u` (save previous position)
2. `a++` (advance tick counter for visual beam animation)
3. Sync `b` to DataWatcher slot 8
4. Check block at block position `(floor(s), floor(t), floor(u))`:
   - If NOT fire (block ID 51): place fire block at that position
   - This keeps fire perpetually burning beneath the crystal

### Damage — `a(pm, int)`

Any damage (any source, any amount):
1. If not already dead and server side:
   - `b = 0` (kill health)
   - Create explosion: `world.createExplosion(null, s, t, u, 6.0F)` (power 6.0, no entity attacker)
   - Remove entity (`v()`)
2. Returns true

The crystal is a 1-hit kill regardless of damage amount. The explosion (power 6.0) deals damage to the dragon if in range and was the selected crystal (`bH`), triggering the 10-damage retaliation in `ay()`.

### No NBT

Empty `a(NbtCompound)` and `b(NbtCompound)` — crystals do not persist across sessions.

---

## 12. Quirks and Parity Notes

1. **`az()` is dead code** — called every 20 ticks when alive, declares variables but performs no side effects. Must still be "called" (i.e., the tick counter check) for RNG state parity if any RNG is added later.

2. **Death XP is 20000 total** — 10 batches of 1000 (ticks 155–200 by 5s) plus one batch of 10000 at tick 200. Tick 200 triggers BOTH the `% 5` batch AND the final batch.

3. **Dragon egg position** — the egg is placed at `(floor(dragon.s), 68, floor(dragon.u))`, which is the dragon's XZ floor position at moment of death. Since the dragon hovers around (0, 128, 0), this is nearly always (0, 68, 0).

4. **Portal at world height / 2** — hardcoded to `world.c / 2 = 64`. Any world with non-standard height would place the portal differently.

5. **Crystal explosion kills dragon** — if the dragon is focusing a crystal and the crystal dies, the dragon takes 10 damage. At full health (200 HP) with all crystals destroyed, the dragon must take 200 damage total. With 10 crystals each dealing 10 damage when destroyed (100 total), the remaining 100 HP requires direct attacks.

6. **EnderCrystal `b` field bug** — in `a(pm, int)`: `this.b = 0;` then immediately `if (this.b <= 0)` — this is always true since b was just set to 0. The redundant check does nothing but must be preserved.

7. **Block destruction whitelist** — only obsidian, bedrock, and end portal blocks are immune. All other blocks (including End Stone) are destroyed by the dragon's body collision.

---

## 13. Open Questions

### 13.1 EnderCrystal EntityList ID

`sf.java` was not cross-referenced in `afw.java`. The entity ID 200 is an assumption; the actual string name and numeric ID should be verified from `afw.java`.

### 13.2 `af` field meaning

`this.af = true` in the dragon constructor — likely `isMultipartEntity` or similar flag. Needs confirmation from `ia.java` or `nq.java` field listing.

### 13.3 `aQ` field condition

`if (!this.o.I && this.aQ == 0)` gates wing and head collision. `aQ` is likely `noClip` or `ticksSinceLastDamage`. The condition means "only push entities when aQ is 0 (normal state)."

### 13.4 Spawning

`ChunkProviderEnd_Spec.md` notes the dragon is spawned at `(0.0, 128.0, 0.0)` during chunk (0,0) populate. Whether a second dragon spawns if the first is killed is not confirmed from the source.
