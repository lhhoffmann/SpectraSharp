<!--
  SpectraSharp Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraSharp Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# EntityNBT Spec
**Source classes:** `ia.java` (Entity base, methods c/d/e), `ih.java` (EntityItem, methods a/b),
  `dk.java` (ItemStack, methods b/c), `nq.java` (LivingEntity, methods a/b),
  `afw.java` (EntityList, 121 lines)
**Superclass:** n/a (documents NBT serialisation across the entity hierarchy)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** DRAFT
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

Documents the complete NBT serialisation protocol for entities stored in chunk `.dat` files:
- `ia.c(ik)` / `ia.d(ik)` / `ia.e(ik)` — base entity write/load chain
- `nq.a(ik)` / `nq.b(ik)` — LivingEntity extension
- `ih.a(ik)` / `ih.b(ik)` — EntityItem (dropped item)
- `dk.b(ik)` / `dk.c(ik)` — ItemStack format (used wherever items are stored)
- `afw` — entity factory: string ID → class mapping + integer spawn ID table

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `ia` | `Entity` | Abstract base; NBT chain entry point |
| `nq` | `LivingEntity` | Extends `ia`; health/effects |
| `ih` | `EntityItem` | Extends `ia` directly; dropped item entity |
| `dk` | `ItemStack` | Item container (id + count + damage + optional tag compound) |
| `afw` | `EntityList` | Static registry: string ID ↔ class ↔ integer ID |

---

## 3. Entity Save/Load Call Chain

```
ia.c(tag)             ← Public entry point: write one entity to a compound tag
    if !K AND id != null:
        tag.put("id", entityStringId)
        ia.d(tag)     ← Write base fields
            [base fields: Pos, Motion, Rotation, FallDistance, Fire, Air, OnGround]
            ia.a(tag)  ← Abstract: subclass writes extra fields
                           nq.a(tag)  → [Health, HurtTime, DeathTime, AttackTime, ActiveEffects]
                               nq.a(tag)  ← abstract: mob-specific data
                           ih.a(tag)  → [Health, Age, Item]
        return true
    else:
        return false   ← entity not saved (dead or unregistered class)

ia.e(tag)             ← Public entry point: read one entity from a compound tag
    [reads base fields into fields]
    ia.b(tag)          ← Abstract: subclass reads extra fields
                           nq.b(tag)  → [Health, HurtTime, DeathTime, AttackTime, ActiveEffects]
                           ih.b(tag)  → [Health, Age, Item]
```

`afw.a(ik, ry)` is the factory that reads `"id"` from the compound, instantiates the class,
then calls `entity.e(tag)` to load all fields.

---

## 4. `ia.c(ik)` — Public Write Gate

```
stringId = afw.b(this)              // looks up class in b-map; null if unregistered
if K == false AND stringId != null: // K = isDead; unregistered entities are skipped
    tag.put("id", stringId)         // TAG_String: entity type name
    d(tag)                          // write all data
    return true
return false
```

`K` (field `ia.K`, boolean, default false) is set to `true` on death / removal.
Entities whose class is not in the `afw` registry return null from `afw.b()` and are not saved.

---

## 5. `ia.d(ik)` — Base Entity Write

Fields written (in order):

| NBT key | Tag type | Source field | Notes |
|---|---|---|---|
| `"Pos"` | TAG_List(TAG_Double) | `[s, t+U, u]` | x, **y+yOffset**, z — see Quirk §12.1 |
| `"Motion"` | TAG_List(TAG_Double) | `[v, w, x]` | motionX, motionY, motionZ |
| `"Rotation"` | TAG_List(TAG_Float) | `[y, z]` | rotationYaw, rotationPitch |
| `"FallDistance"` | TAG_Float | `Q` | Accumulated fall distance |
| `"Fire"` | TAG_Short | `c` | Fire ticks remaining (negative = fireproof) |
| `"Air"` | TAG_Short | `Z()` | Air supply ticks |
| `"OnGround"` | TAG_Byte (boolean) | `D` | 1 if on ground, 0 if airborne |

After all base fields, calls abstract `a(tag)` (subclass extra data).

---

## 6. `ia.e(ik)` — Base Entity Read

Fields read (in order):

| NBT key | Tag method | Target field | Notes |
|---|---|---|---|
| `"Motion"[0]` | TAG_Double | `v` | Clamped: if `|v| > 10.0`, set `v = 0` |
| `"Motion"[1]` | TAG_Double | `w` | Clamped: if `|w| > 10.0`, set `w = 0` |
| `"Motion"[2]` | TAG_Double | `x` | Clamped: if `|x| > 10.0`, set `x = 0` |
| `"Pos"[0]` | TAG_Double | `p, R, s` | All three position copies set to same value |
| `"Pos"[1]` | TAG_Double | `q, S, t` | All three copies set directly — see Quirk §12.1 |
| `"Pos"[2]` | TAG_Double | `r, T, u` | All three copies set to same value |
| `"Rotation"[0]` | TAG_Float | `A, y` | rotationYaw (prev + current both set) |
| `"Rotation"[1]` | TAG_Float | `B, z` | rotationPitch (prev + current both set) |
| `"FallDistance"` | `g()` (TAG_Float getter) | `Q` | Falls back to 0.0 if absent |
| `"Fire"` | `d()` (TAG_Short getter) | `c` | Falls back to 0 if absent |
| `"Air"` | `d()` (TAG_Short getter) | via `g(short)` setter | Falls back to 0 if absent |
| `"OnGround"` | `m()` (boolean getter) | `D` | Falls back to false if absent |

After base fields:
```
setPosition(s, t, u)    // ia.d(double, double, double) — recalculates AABB
setRotation(y, z)       // ia.b(float, float) — normalises angles
b(tag)                  // abstract: subclass reads extra fields
```

---

## 7. `nq.a(ik)` / `nq.b(ik)` — LivingEntity Extension

Called from `ia.a(tag)` (write) and `ia.b(tag)` (read) in LivingEntity subclasses.

### Write (`nq.a(ik)`):

| NBT key | Tag type | Value | Notes |
|---|---|---|---|
| `"Health"` | TAG_Short | `aM` (float cast to short) | Current health points |
| `"HurtTime"` | TAG_Short | `aP` | Ticks of invulnerability after damage |
| `"DeathTime"` | TAG_Short | `aS` | Death animation counter |
| `"AttackTime"` | TAG_Short | `aT` | Attack cooldown |
| `"ActiveEffects"` | TAG_List(TAG_Compound) | `bh.values()` | Only written if `bh` is non-empty |

Each ActiveEffects compound:

| Key | Type | Value |
|---|---|---|
| `"Id"` | TAG_Byte | Potion effect ID |
| `"Amplifier"` | TAG_Byte | Effect level (0 = level I) |
| `"Duration"` | TAG_Int | Ticks remaining |

### Read (`nq.b(ik)`):

- `"Health"`: if key absent, defaults to `f_()` (= max health; 20 for players, mob-specific otherwise).
- `"HurtTime"`, `"DeathTime"`, `"AttackTime"`: read as TAG_Short; default 0 if absent.
- `"ActiveEffects"`: read back into `bh` map (effect ID → `s` instance).

---

## 8. `ih.a(ik)` / `ih.b(ik)` — EntityItem Extension

### Write (`ih.a(ik)`):

| NBT key | Tag type | Value | Notes |
|---|---|---|---|
| `"Health"` | TAG_Short | `(short)(byte)f` | Remaining "health" of the item entity (default 5). Cast: `f` (int) → byte → short |
| `"Age"` | TAG_Short | `b` (short) | Despawn counter (0 → 6000 = 5 min) |
| `"Item"` | TAG_Compound | `a.b(new ik())` | ItemStack NBT (see §9) |

**Note:** There is no `"PickupDelay"` field in 1.0. The Coder's REQUESTS.md query about this can be confirmed: no such field exists in `ih.a()` or `ih.b()`.

### Read (`ih.b(ik)`):

- `"Health"`: read as TAG_Short, then `& 255` (unsigned byte extraction).
- `"Age"`: read as TAG_Short.
- `"Item"`: read compound, then `dk.a(compound)` to deserialise. If result is null (invalid ItemStack), entity calls `this.v()` (= setDead/kill).

---

## 9. ItemStack NBT Format (`dk.b(ik)` / `dk.c(ik)`)

Used wherever an ItemStack is serialised (EntityItem, chest, furnace, player inventory).

### Write (`dk.b(ik)`):

| NBT key | Tag type | Value | Notes |
|---|---|---|---|
| `"id"` | TAG_Short | `c` | Block/item ID |
| `"Count"` | TAG_Byte | `a` | Stack size (1–64) |
| `"Damage"` | TAG_Short | `e` | Metadata / durability damage |
| `"tag"` | TAG_Compound | `d` | Extra NBT (enchantments etc.); only written if `d != null` |

### Read (`dk.c(ik)`):

| NBT key | Getter | Target | Notes |
|---|---|---|---|
| `"id"` | `d()` (short) | `c` | Item/block ID |
| `"Count"` | `c()` (byte) | `a` | Stack size |
| `"Damage"` | `d()` (short) | `e` | Damage value |
| `"tag"` | `k()` (compound) | `d` | Only read if key exists (`b("tag")`) |

`dk.a(ik tag)` = factory: creates a private `dk()`, calls `c(tag)`, returns the result if
`a().item != null` (i.e., the item ID maps to a registered item), else returns null.

---

## 10. EntityList (`afw`) — Entity ID Table

### String ID → Class mapping (used for chunk NBT serialisation):

| String `"id"` | Obfuscated class | Human name | Integer ID |
|---|---|---|---|
| `"Item"` | `ih` | EntityItem | 1 |
| `"XPOrb"` | `fk` | EntityXPOrb | 2 |
| `"Painting"` | `tj` | EntityPainting | 9 |
| `"Arrow"` | `ro` | EntityArrow | 10 |
| `"Snowball"` | `aah` | EntitySnowball | 11 |
| `"Fireball"` | `aad` | EntityFireball (large) | 12 |
| `"SmallFireball"` | `yn` | EntitySmallFireball | 13 |
| `"ThrownEnderpearl"` | `tm` | EntityEnderPearl | 14 |
| `"EyeOfEnderSignal"` | `bs` | EntityEnderEye | 15 |
| `"PrimedTnt"` | `dd` | EntityTNTPrimed | 20 |
| `"FallingSand"` | `uo` | EntityFallingBlock | 21 |
| `"Minecart"` | `vm` | EntityMinecart | 40 |
| `"Boat"` | `no` | EntityBoat | 41 |
| `"Mob"` | `nq` | LivingEntity (abstract; registered but not directly instantiable) | 48 |
| `"Monster"` | `zo` | EntityMob (abstract) | 49 |
| `"Creeper"` | `abh` | EntityCreeper | 50 |
| `"Skeleton"` | `it` | EntitySkeleton | 51 |
| `"Spider"` | `vq` | EntitySpider | 52 |
| `"Giant"` | `abc` | EntityGiantZombie | 53 |
| `"Zombie"` | `gr` | EntityZombie | 54 |
| `"Slime"` | `aed` | EntitySlime | 55 |
| `"Ghast"` | `is` | EntityGhast | 56 |
| `"PigZombie"` | `jm` | EntityPigZombie | 57 |
| `"Enderman"` | `aii` | EntityEnderman | 58 |
| `"CaveSpider"` | `aco` | EntityCaveSpider | 59 |
| `"Silverfish"` | `gl` | EntitySilverfish | 60 |
| `"Blaze"` | `qf` | EntityBlaze | 61 |
| `"LavaSlime"` | `aea` | EntityMagmaCube | 62 |
| `"EnderDragon"` | `oo` | EntityDragon | 63 |
| `"Pig"` | `fd` | EntityPig | 90 |
| `"Sheep"` | `hm` | EntitySheep | 91 |
| `"Cow"` | `adr` | EntityCow | 92 |
| `"Chicken"` | `qh` | EntityChicken | 93 |
| `"Squid"` | `yv` | EntitySquid | 94 |
| `"Wolf"` | `aik` | EntityWolf | 95 |
| `"MushroomCow"` | `tb` | EntityMooshroom | 96 |
| `"SnowMan"` | `ahd` | EntitySnowman | 97 |
| `"Villager"` | `ai` | EntityVillager | 120 |
| `"EnderCrystal"` | `sf` | EntityEnderCrystal | 200 |

### Four Maps in `afw`:

| Map | Key | Value | Purpose |
|---|---|---|---|
| `a` | String `"id"` | `Class` | String → class (for NBT load) |
| `b` | `Class` | String | Class → string (for NBT save) |
| `c` | Integer entityId | `Class` | Integer → class (for network spawn) |
| `d` | `Class` | Integer | Class → integer (for network spawn) |

### Factory `afw.a(ik tag, ry world)`:
1. Read `tag.i("id")` (TAG_String getter → empty string if absent).
2. Look up class in map `a`. If null: print warning "Skipping Entity with id X", return null.
3. Instantiate via `constructor(ry)`.
4. Call `entity.e(tag)` to load all fields.
5. Return entity (may be null if instantiation fails).

---

## 11. `nq.K()` — IsAlive override

LivingEntity overrides `ia.K()`:
```
return !K AND aM > 0    // not dead-flagged AND health > 0
```
This means a LivingEntity with health ≤ 0 is considered dead and will not be saved
(the `c(ik)` gate checks `K == false` but calls `K()` which returns false for dead mobs).

Actually: the save gate in `ia.c(ik)` checks `this.K` (the field, not the method).
`K()` the method (overridden in `nq`) is used elsewhere. The save gate uses the field directly.
Dead LivingEntities (`aM ≤ 0`) that have not yet set `K = true` may still be saved.

---

## 12. Known Quirks / Bugs to Preserve

### 12.1 Y-Position Drift on Reload

`ia.d(ik)` writes `Pos[1] = t + U` (feet Y + yOffset).
`ia.e(ik)` reads `t = Pos[1]` directly, without subtracting `U`.

**Effect:** after every save/load cycle, the entity's Y position increases by `U`.
For most entities `U = 0.0f`, so no visible drift. For any entity with `U ≠ 0`,
the entity rises by `U` blocks on each reload. This is a vanilla bug; preserve it exactly.

### 12.2 Motion Clamping on Load

Velocities with `|v| > 10.0`, `|w| > 10.0`, or `|x| > 10.0` are reset to 0.
This silently discards any velocity exceeding 10 blocks/tick on load, preventing
corrupted saves from launching entities into space.

### 12.3 No `"Riding"` Compound

In 1.0, the base entity save (`ia.d`) does not write a `"Riding"` TAG_Compound for mounted
entities. Mount/rider relationships are not persisted — both entities are saved independently.
On reload, no rider is automatically re-attached. This matches vanilla 1.0 behaviour.

### 12.4 `"Health"` in EntityItem is Byte Cast to Short

`var1.a("Health", (short)((byte)this.f))` — `f` (int) is first narrowed to `byte` (signed,
so values > 127 wrap to negative), then widened to `short`. Reading uses `d("Health") & 255`
to reverse the byte narrowing. The Health field has a range of 0–255 after the `& 255`.

### 12.5 PickupDelay Not Persisted

`EntityItem` (`ih`) does not write or read a `"PickupDelay"` field. The delay resets to 0
on every chunk reload, meaning items are immediately pickable after a world reload
(even items that were freshly dropped before the save).

### 12.6 Abstract/Monster Classes in Registry

`"Mob"` (48) and `"Monster"` (49) map to `nq` and `zo` which are abstract. They cannot
be directly instantiated and would throw an exception if encountered in a save file.
These entries exist for completeness of the ID table but should never appear in practice.

---

## 13. Open Questions

- **`ia.U` (yOffset)**: confirmed as a float field from the write code. Exact default values
  for specific entity types not verified here (EntityPlayer, specific mobs). The Entity_Spec
  documents this as the eye-height offset.
- **`ia.Z()` (Air supply getter)**: returns a short. The actual field name and default
  (typically 300 ticks = 15 seconds) not cross-verified here.
- **Individual mob `a(ik)` / `b(ik)` overrides**: each mob class overrides these with its
  own fields (e.g., Creeper fuse, Sheep colour, Wolf tame/owner). These are NOT documented
  here — mob-specific NBT should be specced per-mob when needed.

---

*Spec written by Analyst AI from `ia.java` (NBT methods), `nq.java` (NBT methods),
`ih.java` (NBT methods), `dk.java` (NBT methods), `afw.java` (121 lines). No C# consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — EntityNBT)*
