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

# EntityMobBase Spec
**Source classes:** `ww.java` (EntityAI), `zo.java` (EntityMonster), `fx.java` (EntityAnimal), plus 8 concrete mobs
**Superclass chain:** `ia` (Entity) → `nq` (LivingEntity) → `ww` (EntityAI) → `zo` or `fx` → concrete mob
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

This spec covers the abstract mob class hierarchy between `nq` (LivingEntity, already specced) and
the 8 concrete mobs needed to populate EntityRegistry: Zombie, Skeleton, Spider, Creeper (hostile),
and Pig, Sheep, Cow, Chicken (passive/breedable). The hierarchy has two intermediate abstract layers:

- `ww` (EntityAI) — shared by ALL mobs: pathfinding, target tracking, movement speed application.
- `zo` (EntityMonster) — extends `ww`; used by hostile mobs; adds attack strength and light-level spawning.
- `fx` (EntityAnimal) — extends `ww`; used by passive/breedable animals; adds breeding and age system.

AI tick logic (pathfinding detail, targeting algorithm) is documented here only to the extent needed
to understand fields and NBT. Full AI behaviour is out of scope for this spec.

---

## 2. Class Hierarchy Diagram

```
ia (Entity)
└── nq (LivingEntity)                    [abstract]
    └── ww                               [abstract — EntityAI]
        ├── zo                           [abstract — EntityMonster]
        │   ├── abh  EntityCreeper       ID 50
        │   ├── it   EntitySkeleton      ID 51
        │   ├── vq   EntitySpider        ID 52
        │   └── gr   EntityZombie        ID 54
        └── fx                           [abstract — EntityAnimal]
            ├── fd   EntityPig           ID 90
            ├── hm   EntitySheep         ID 91
            ├── adr  EntityCow           ID 92
            └── qh   EntityChicken       ID 93
```

The `bx` and `by` obfuscated names in this codebase refer to entirely different classes
(WorldChunkManager variant and a small interface impl) — not mob classes. Do not confuse them
with the ww field `by` (a primitive int declared in `ww`).

---

## 3. `ww` — EntityAI (abstract base for all AI mobs)

**Source:** `ww.java` — extends `nq` (LivingEntity)

### 3.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `dw` | null | Current pathfinder path result; reassigned when target changes or each 20 ticks if path fails |
| `h` | `ia` | null | Current target entity (attack or follow target); set by `o()` virtual |
| `i` | `boolean` | false | isPanicking: true when this mob is close to its target (strafing mode); set by `az()` virtual each tick |
| `by` | `int` | 0 | Panic/anger timer: decrements each tick; animal subclasses set to 60 on hit; while > 0 move speed doubles; suppresses new wander targets |

Fields inherited from `nq` relevant here:
- `bw` (float, default 0.7F) — base movement speed multiplier; subclasses override in constructor
- `aX` (int, default 0) — experienceValue dropped on death; hostile mobs set to 5

### 3.2 NBT serialisation

`ww.a(ik)` and `ww.b(ik)` both delegate entirely to `super.a/b(ik)` (i.e. `nq`). `ww` adds **no NBT fields** of its own. Fields `a`, `h`, `i`, `by` are transient.

### 3.3 Key method: `aw()` — movement speed with panic multiplier

Called by the movement system to get the effective speed for this tick:
1. Call `super.aw()` to get the base speed.
2. If `by > 0`: multiply result by 2.0F.
3. Return result.

### 3.4 Key method: `i()` — canSpawnHere check

Overrides nq: `super.i() AND a(floorX, floorY, floorZ) >= 0.0F` where `a(x,y,z)` is a virtual "prefer this position" score (default 0.0F, overridden by zo and fx).

---

## 4. `zo` — EntityMonster (abstract hostile mob intermediate)

**Source:** `zo.java` — extends `ww`

### 4.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | 2 | attackStrength — base melee damage; concrete subclasses override in constructor |

On construction: `this.aX = 5` — all hostile mobs drop 5 experience points on death.

### 4.2 NBT serialisation

`zo.a(ik)` and `zo.b(ik)` both delegate entirely to `super.a/b(ik)` (i.e. `ww` → `nq`). `zo` adds **no NBT fields** of its own. `attackStrength` is baked in at construction, not persisted.

### 4.3 Target selection (`o()`)

Overrides `ww.o()`:
1. Call `world.b(this, 16.0)` to find the nearest player within 16 blocks.
2. If player found AND `this.i(player)` (canSeeEntity / line-of-sight passes): return player.
3. Otherwise return null.

### 4.4 Attack logic (`b(ia target)`)

Called from `ww.n()` (AI tick) when target is in range:
1. Compute effective damage = `attackStrength` (`this.a`).
2. If Strength potion (`abg.g`) active: add `3 << level`.
3. If Weakness potion (`abg.t`) active: subtract `2 << level`.
4. Call `target.a(pm.a(this), damage)` — deals entity-damage to target.
   `pm.a(nq)` creates a `fq("mob", attacker)` DamageSource (see LivingEntityDamage_Spec).

### 4.5 canSpawnHere — light level check (`u_()`)

Returns false (too bright, don't spawn) if:
`world.getSkyLight(x, floorY, z) > random.nextInt(32)`

Returns true (dark enough) if the combined brightness `n(x,y,z)` ≤ `random.nextInt(8)`.
In the Nether (`world.D()` = true): temporarily sets sky darkening `k = 10` when computing brightness (makes Nether always dark enough for hostile spawning).

The `i()` override chains: `u_() AND super.i()`.

---

## 5. `fx` — EntityAnimal (abstract breedable animal intermediate)

**Source:** `fx.java` — extends `ww`

### 5.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | 0 | inLoveTimer: set to 600 ticks when fed breeding food; counts down each tick to 0 |
| `b` | `int` | 0 | breedingProximityCounter: increments while two in-love animals are adjacent (≤ 3.5 blocks); triggers breed at 60; transient — NOT persisted |

Age is stored in **DataWatcher entry 12** (type int, registered in `b()` with initial value 0):
- 0 = adult
- Negative = baby (starts at −24000, counts up to 0 over ~20 min = 24000 ticks)
- Positive = breeding cooldown (set to 6000 after breeding; counts down to 0)

`m()` = `ag.c(12)` — reads age from DataWatcher.
`b(int)` = `ag.b(12, v)` — writes age to DataWatcher.

### 5.2 NBT serialisation

`fx.a(ik)` writes (on top of nq base fields):
- `"Age"` TAG_Int = `m()` (DataWatcher 12 age value)
- `"InLove"` TAG_Int = `this.a` (breeding readiness timer)

`fx.b(ik)` reads:
- `"Age"` TAG_Int → calls `b(value)` to set DataWatcher 12; note: uses `e()` (= `getInt`) not `d()` (= `getShort`)
- `"InLove"` TAG_Int → sets `this.a`

### 5.3 Breeding food check (`a(dk stack)`)

Virtual method, default: returns `stack.c == acy.S.bM` (wheat ID).
Subclasses that accept different food override this.

### 5.4 canSpawnHere (`i()`)

Returns true only if:
- The block at (x, floorY−1, z) is grass (`yy.u` = grass block)
- AND light level `m(x, floorY, z) > 8`
- AND `super.i()` (ww / nq checks)

### 5.5 Panic timer reset on hit

`fx.a(pm, int)` (attackEntityFrom override):
1. Sets `this.by = 60` — activates 60-tick panic sprint (doubles move speed via `ww.aw()`).
2. Sets `this.h = null` — clears current target (stops following/breeding pursuit).
3. Sets `this.a = 0` — clears inLoveTimer.
4. Calls `super.a(pm, int)`.

---

## 6. Concrete Mobs — Field Tables

### 6.1 gr — EntityZombie (String ID: `"Zombie"`, integer ID: 54)

**Superclass:** `zo` (EntityMonster)
**Constructor signature:** `gr(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/zombie.png"` |
| maxHealth (`f_()`) | 20 |
| attackStrength (`a`) | 4 |
| moveSpeed (`bw`) | 0.5F |
| experienceValue (`aX`) | 5 (set by zo) |
| AABB width / height | default (not overridden; uses nq/ia default) |

**Extra NBT fields:** None. `a(ik)` and `b(ik)` delegate entirely to `super`.

**Quirk:** Burns in daylight — each tick, if the world `l()` (isDayTime) and the block at the zombie's
position has sky light > 0.5F (biome-brightness), there is a `random.nextFloat() * 30.0F < (brightness − 0.4F) * 2.0F` chance to call `e(8)` (set on fire for 8 seconds). This ignition is tick logic, not NBT.

---

### 6.2 it — EntitySkeleton (String ID: `"Skeleton"`, integer ID: 51)

**Superclass:** `zo` (EntityMonster)
**Constructor signature:** `it(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/skeleton.png"` |
| maxHealth (`f_()`) | 20 |
| attackStrength (`a`) | 2 (zo default; Skeleton attacks via bow, not melee) |
| moveSpeed (`bw`) | 0.7F (nq default, not overridden) |
| experienceValue (`aX`) | 5 |
| AABB | default |
| Held item (`s()`) | Static `dk(acy.j, 1)` — a bow (Item ID for bow); this is the ranged weapon |

**Extra NBT fields:** None. `a(ik)` and `b(ik)` delegate entirely to `super`.

**Attack behaviour:** Skeleton does NOT use `zo.b(ia)` melee; overrides `a(ia, float)` (approach handler)
to shoot a `ro` (EntityArrow) when target distance < 10.0F. Arrow is fired at angle adjusted by distance.
`this.aT = 60` (60-tick attack cooldown) after shooting.

---

### 6.3 vq — EntitySpider (String ID: `"Spider"`, integer ID: 52)

**Superclass:** `zo` (EntityMonster)
**Constructor signature:** `vq(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/spider.png"` |
| maxHealth (`f_()`) | 16 |
| attackStrength (`a`) | 2 (zo default) |
| moveSpeed (`bw`) | 0.8F |
| AABB width | 1.4F (half = 0.7F) |
| AABB height | 0.9F |
| experienceValue (`aX`) | 5 |

**DataWatcher entry 16** (byte, registered in `b()`, initial value 0):
- Bit 0 = isClimbing: set true/false each tick via `b(this.E)` where `E` = isCollidedHorizontally.
- This is rendering/movement state only — **NOT persisted to NBT**.

**Extra NBT fields:** None. `a(ik)` and `b(ik)` delegate entirely to `super`.

**Special: targeting in daylight:** `o()` overrides zo — if brightness at spider position > 0.5F, returns null (passive during day). Only targets at night or when brightness ≤ 0.5F.

**Special: immunity to Poison (`abg.u`):** `b(s potion)` returns false for Poison — spiders cannot be poisoned.

---

### 6.4 abh — EntityCreeper (String ID: `"Creeper"`, integer ID: 50)

**Superclass:** `zo` (EntityMonster)
**Constructor signature:** `abh(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/creeper.png"` |
| maxHealth (`f_()`) | 20 |
| attackStrength (`a`) | 2 (zo default; Creeper kills via explosion, not melee) |
| moveSpeed (`bw`) | 0.7F (nq default) |
| experienceValue (`aX`) | 5 |
| AABB | default |

**Instance fields (transient — not persisted):**

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `b` | `int` | 0 | fuseCountdown: increments toward 30 when approaching target, decrements when retreating; explosion at 30 |
| `c` | `int` | 0 | prevFuseCountdown: set to `b` at start of each tick; used for render interpolation |

**DataWatcher entries:**
- Entry 16 (byte, initial −1): `ay()` getter; `b(int)` setter. Stores the fuseCountdown for client sync (−1 = not fusing, 0–30 = active fuse). Range: −1 to 30.
- Entry 17 (byte, initial 0): `ax()` getter. Bit 0 = isPowered (charged by lightning).

**Extra NBT fields:**

| NBT key | Type | Notes |
|---|---|---|
| `"powered"` | TAG_Byte (as boolean) | Written only if `ag.a(17) == 1` (i.e. only if powered=true; absent key on load = false). Read back as `m("powered")` = boolean → sets DataWatcher 17. |

Fusedown fields `b` and `c` are **NOT persisted**. They reset to 0 on every load.

**Explosion behaviour:** When fuseCountdown `b >= 30`:
- If powered (DW17 = 1): `world.a(entity, x, y, z, 6.0F)` — explosion radius 6.
- Else: `world.a(entity, x, y, z, 3.0F)` — explosion radius 3.
- Then calls `v()` (kill self).

**Lightning interaction:** `a(su lightning)` override — sets `ag.b(17, byte 1)` (marks powered).

---

### 6.5 fd — EntityPig (String ID: `"Pig"`, integer ID: 90)

**Superclass:** `fx` (EntityAnimal)
**Constructor signature:** `fd(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/pig.png"` |
| maxHealth (`f_()`) | 10 |
| moveSpeed (`bw`) | 0.7F (nq default) |
| AABB width | 0.9F (half = 0.45F) |
| AABB height | 0.9F |

**DataWatcher entry 16** (byte, registered in `b()`, initial 0):
- Bit 0 = hasSaddle: `t_()` = `(ag.a(16) & 1) != 0`; `b(boolean)` sets/clears bit 0.

**Extra NBT fields** (beyond `fx` Age + InLove):

| NBT key | Type | Notes |
|---|---|---|
| `"Saddle"` | TAG_Byte (boolean) | Written as `var1.a("Saddle", this.t_())` — true if saddled. Read as `var1.m("Saddle")` → calls `b(boolean)` to set DataWatcher 16 bit 0. |

**Death behaviour:** If killed by lightning while a player is riding: drops cooked pork chop instead of raw — `V()` = isOnFire check. Standard drop is raw pork (`acy.ap`) or cooked (`acy.aq`).

---

### 6.6 hm — EntitySheep (String ID: `"Sheep"`, integer ID: 91)

**Superclass:** `fx` (EntityAnimal)
**Constructor signature:** `hm(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/sheep.png"` |
| maxHealth (`f_()`) | 8 |
| moveSpeed (`bw`) | 0.7F (nq default) |
| AABB width | 0.9F (half = 0.45F) |
| AABB height | 1.3F |

**DataWatcher entry 16** (byte, registered in `b()`, initial 0 via `new Byte((byte)0)`):
- Bits 3..0 = wool colour (0–15); `l()` getter = `ag.a(16) & 15`; `c(int)` setter masks with `0xF`.
- Bit 4 = isSheared: `v_()` = `(ag.a(16) & 16) != 0`; `b(boolean)` sets/clears bit 4.

**Extra NBT fields** (beyond `fx` Age + InLove):

| NBT key | Type | Notes |
|---|---|---|
| `"Sheared"` | TAG_Byte (boolean) | Written as `var1.a("Sheared", this.v_())`. Read back via `b(var1.m("Sheared"))`. |
| `"Color"` | TAG_Byte | Written as `var1.a("Color", (byte)this.l())` — the 4-bit wool colour (0–15). Read via `c(var1.c("Color"))` where `c(ik)` = `getByte()`. |

**Colour distribution** — static `a(Random)` method gives spawn probabilities:
- < 5% → colour 15 (black)
- 5–10% → colour 7 (grey)
- 10–15% → colour 8 (light grey)
- 15–18% → colour 12 (brown)
- 1/500 → colour 6 (pink)
- Otherwise → colour 0 (white)

**Shearing interaction:** `c(vi player)` — if player holds shears (`acy.bd`) and sheep is not already sheared:
drops 2 + `nextInt(3)` wool items of this sheep's colour; sets sheared=true; consumes 1 shear durability.

---

### 6.7 adr — EntityCow (String ID: `"Cow"`, integer ID: 92)

**Superclass:** `fx` (EntityAnimal)
**Constructor signature:** `adr(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/cow.png"` |
| maxHealth (`f_()`) | 10 |
| AABB width | 0.9F (half = 0.45F) |
| AABB height | 1.3F |

**Extra NBT fields:** None beyond `fx` Age + InLove. `a(ik)` and `b(ik)` delegate entirely to `super`.

**Milk bucket interaction:** `c(vi player)` — if player holds an empty bucket (`acy.av`): replaces bucket
with a full milk bucket (`acy.aF`). Uses `var1.by.a(var1.by.c, new dk(acy.aF))` (replaces selected slot).

---

### 6.8 qh — EntityChicken (String ID: `"Chicken"`, integer ID: 93)

**Superclass:** `fx` (EntityAnimal)
**Constructor signature:** `qh(ry world)`

| Property | Value |
|---|---|
| Texture | `"/mob/chicken.png"` |
| maxHealth (`f_()`) | 4 |
| AABB width | 0.3F (half = 0.15F) |
| AABB height | 0.7F |

**Instance fields (transient — not persisted):**

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `boolean` | false | isChickenJockey (spawned from a zombie ride) |
| `b` | `float` | 0.0F | wingAngle (current) — render field |
| `c` | `float` | 0.0F | oWingAngle — render field, previous tick's `b` |
| `d` | `float` | — | prevBodyRoll — render field |
| `e` | `float` | — | bodyRoll — render field |
| `f` | `float` | 1.0F | wingSpeed — render field |
| `g` | `int` | random [6000, 12000) | eggLayTimer: decrements each tick; lays egg and resets when it hits 0 |

**Egg laying:** Each tick (in `c()`):
- Decrement `g` if not a baby (`!q_()`) and not client-side.
- When `g <= 0`: play `"mob.chickenplop"` sound; drop 1 egg (`acy.aO`); reset `g = nextInt(6000) + 6000`.

**Extra NBT fields:** None beyond `fx` Age + InLove. `a(ik)` and `b(ik)` delegate entirely to `super`.
`eggLayTimer` (`g`) is **not persisted** — resets to a random value on every load.

---

## 7. DataWatcher Slot Summary

| Slot | Declared by | Type | Mobs using it |
|---|---|---|---|
| 0–11 | `nq` / `ia` | various | all (health flash, fire, etc. — see LivingEntity_Spec) |
| 12 | `fx` | `int` | Pig, Sheep, Cow, Chicken — age timer |
| 16 | `abh` / `fd` / `hm` / `vq` | `byte` | Each uses bit-fields differently (see per-mob sections above) |
| 17 | `abh` | `byte` | Creeper powered flag only |

Note: entries 12, 16, 17 are registered via `ag.a(slotId, defaultValue)` in `b()` (the `initEntity` override). The DataWatcher type ID is determined by the Java type of the default value:
- `new Integer(0)` → type 2 (int, 4 bytes)
- `(byte)0` or `new Byte((byte)0)` → type 0 (byte, 1 byte)

---

## 8. Constructor Signatures

All mob constructors take exactly one argument: `(ry world)`. No mob in this spec has a multi-arg constructor or a no-arg constructor.

```
new gr(world)   // Zombie
new it(world)   // Skeleton
new vq(world)   // Spider
new abh(world)  // Creeper
new fd(world)   // Pig
new hm(world)   // Sheep
new adr(world)  // Cow
new qh(world)   // Chicken
```

---

## 9. Known Quirks / Bugs to Preserve

1. **Creeper fuseCountdown not persisted:** The explosion fuse resets to 0 every time a creeper is loaded from NBT. A creeper mid-fuse does not continue fusing after chunk unload/reload. This is vanilla behaviour.

2. **Chicken eggLayTimer not persisted:** Resets to a random value in [6000, 12000) on every load. A chicken loaded from disk may lay its next egg sooner or later than expected.

3. **Sheep's `new Byte((byte)0)` vs `(byte)0`:** The DataWatcher registration for Sheep uses `new Byte((byte)0)` (boxed) while Pig uses unboxed `(byte)0`. Both register as type ID 0 (byte) in DataWatcher — no functional difference.

4. **Spider targeting during day:** Spider's `o()` returns null (no target) when block brightness > 0.5F. This is brightness, not time-of-day: a spider in a dark room targets at noon; a spider in sunlight at night is passive.

5. **ww.by vs nq.bx:** The field `by` declared in `ww` is an int panic/anger timer. The field `bx` declared in `nq` (line 71) is a separate unrelated field. They do NOT alias each other — different classes, different semantics.

---

## 10. Open Questions

1. **Default AABB for mobs that don't call `a(float, float)`:** Zombie (`gr`), Skeleton (`it`), and Creeper (`abh`) do not call `this.a(width, height)` in their constructors. Their size is whatever `ia` (Entity base) sets as default. The Entity_Spec should document the default `M` and `N` values set in the `ia` constructor. If those values are 0/0, the mob collision box would be broken — likely the Entity_Spec default is width=0.6, height=1.8 (matching EntityPlayer which explicitly calls `a(0.6F, 1.8F)` but may be duplicating an already-correct default).

2. **`zo`'s `aey` interface:** `zo` declares `implements aey`. This interface's methods were not analysed — likely a mob spawning eligibility or event interface. Not needed for the 8 NBT stubs but may be needed for mob spawning system.

3. **`ww`'s `dw` pathfinder:** The `dw` type (pathfinding result object) was not analysed. It is transient and not needed for NBT parity.

4. **`fx` breeding food per species:** Default is wheat (`acy.S`). Cows confirm wheat. Pigs accept `acy.ak` (potatoes? carrots?) — unconfirmed from this reading; `fd` does not override `a(dk)`.
