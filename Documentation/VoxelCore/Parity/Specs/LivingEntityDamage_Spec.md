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

# LivingEntityDamage Spec
**Source class:** `nq.java` (LivingEntity), `pm.java` (DamageSource), `fq.java` / `qq.java` (subclasses)
**Superclass:** `nq` extends `ia` (Entity)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

This spec covers the damage / invulnerability system of `nq` (LivingEntity): the `a(pm, int)`
(attackEntityFrom) method, the fields `ia.ac` / `nq.aq` / `nq.bp` that govern the invulnerability
window, the armor reduction pipeline, and the `pm` (DamageSource) class with its subclasses and
factory methods.

This spec resolves the open question in `LivingEntity_Spec.md` §7: field `ac` was marked uncertain.
The answer: `ac` is in **`ia`** (Entity base), not `nq`. It is a separate counter from `nq.aq`.

---

## 2. Field Clarification: `ac` vs `aq` vs `bp`

These three fields work together to implement the invulnerability window and repeated-hit detection.

| Field | Declared in | Type | Default | Semantics |
|---|---|---|---|---|
| `ac` | `ia` (Entity) | `int` | 0 | **Invulnerability countdown.** Set to `aq` on a full hit; decrements by 1 each tick in `nq.c()`. Counts down to 0. |
| `aq` | `nq` (LivingEntity) | `int` | 20 | **Invulnerability window length** in ticks. Never changes after construction (no code seen modifying it). At 20 Hz this is exactly 1 second. |
| `bp` | `nq` (LivingEntity) | `int` | 0 | **lastDamageAmount.** The damage value that started the current invulnerability window. Used to allow higher-damage hits to partially apply during the window. |

They are **three distinct fields**, declared in two different classes (`ia` and `nq`). The LivingEntity_Spec
confusion arose because all three affect the same damage gate, but `ac` lives in the Entity base.

### Tick decrement (in `nq.c()`)

Each tick:
```
if (ac > 0) ac--
```

---

## 3. `pm` — DamageSource

**Source:** `pm.java` — no superclass

### 3.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `m` | `String` | (set in ctor) | Type string identifier (e.g. `"mob"`, `"fall"`, `"fire"`) |
| `n` | `boolean` | false | isUnblockable: if true, armor reduction (`c()`) is skipped |
| `o` | `boolean` | false | bypassesInvulnerability: if true, the out-of-world / void kill still applies when entity would normally be immune |
| `p` | `float` | 0.3F | hungerExhaustionAmount: how much food exhaustion this damage type adds; 0.0F for most non-physical sources |
| `q` | `boolean` | false | isFireDamage: if true and entity has Fire Resistance potion, damage is blocked entirely |
| `r` | `boolean` | false | isProjectileDamage (stored in `r`, set via `c()` builder) |

### 3.2 Builder Methods (return `this` for chaining)

| Method (obf) | Effect |
|---|---|
| `h()` | Sets `n=true` and `p=0.0F` — marks damage as unblockable (skips armor) and no hunger cost |
| `i()` | Sets `o=true` — marks damage as bypassing invulnerability |
| `j()` | Sets `q=true` — marks as fire damage |
| `c()` | Sets `r=true` — marks as projectile damage |

### 3.3 Instance Accessor Methods

| Method (obf) | Returns | Meaning |
|---|---|---|
| `b()` | `boolean` | `this.r` — isProjectile |
| `d()` | `boolean` | `this.n` — isUnblockable |
| `e()` | `float` | `this.p` — hungerExhaustion |
| `f()` | `boolean` | `this.o` — bypassesInvulnerability |
| `a()` | `ia` | null (base; overridden in subclasses to return the attacker entity) |
| `g()` | `ia` | delegates to `a()` — returns attacker entity |
| `k()` | `boolean` | `this.q` — isFireDamage |
| `l()` | `String` | `this.m` — type string |

### 3.4 Static Factory Methods

| Method signature | Returns | Description |
|---|---|---|
| `pm.a(nq attacker)` | `fq("mob", attacker)` | Mob melee attack — attacker is the LivingEntity dealing damage |
| `pm.a(vi player)` | `fq("player", player)` | Player melee attack |
| `pm.a(ro arrow, ia owner)` | `qq("arrow", arrow, owner).c()` | Arrow — projectile; `c()` sets isProjectile=true |
| `pm.a(aad fireball, ia owner)` | `qq("fireball", fireball, owner).j().c()` | Fireball — fire + projectile |
| `pm.a(ia thrown, ia owner)` | `qq("thrown", thrown, owner).c()` | Thrown snowball/egg — projectile |
| `pm.b(ia indirect, ia owner)` | `qq("indirectMagic", indirect, owner).h()` | Indirect magic (potions) — unblockable |

### 3.5 Static Singleton Damage Sources

| Field (obf) | Type string | Unblockable | Fire | Notes |
|---|---|---|---|---|
| `pm.a` | `"inFire"` | no | yes | Standing in fire block |
| `pm.b` | `"onFire"` | yes | yes | Burning (fire tick) |
| `pm.c` | `"lava"` | no | yes | Lava contact |
| `pm.d` | `"inWall"` | yes | no | Suffocation in block |
| `pm.e` | `"drown"` | yes | no | Drowning |
| `pm.f` | `"starve"` | yes | no | Starvation (food=0) |
| `pm.g` | `"cactus"` | no | no | Cactus contact |
| `pm.h` | `"fall"` | yes | no | Fall damage |
| `pm.i` | `"outOfWorld"` | yes | no | Void (bypassesInvulnerability=true) |
| `pm.j` | `"generic"` | yes | no | Generic / unknown |
| `pm.k` | `"explosion"` | no | no | Explosion |
| `pm.l` | `"magic"` | yes | no | Magic / potion area effect |

---

## 4. `fq` — EntityDamageSource (entity attacker)

**Source:** `fq.java` — extends `pm`

Constructor: `fq(String typeString, ia attacker)` — calls `super(typeString)`; stores attacker in private field `n`.
`a()` override: returns `this.n` (the attacker entity).

---

## 5. `qq` — EntityDamageSourceIndirect (projectile with owner)

**Source:** `qq.java` — extends `fq`

Constructor: `qq(String typeString, ia projectile, ia owner)` — calls `super(typeString, projectile)`; stores `owner` in private field `n`.
`a()` override: returns `this.n` (the **owner** entity, not the projectile itself).

Note: `fq` also stores the projectile in **its own** private `n` field (different from `qq.n`). The projectile entity can be retrieved via `g()` on base `pm` which calls `a()` on the `fq` super — but `qq.a()` shadows that to return the owner. To get the projectile, cast to `fq` and call `super.a()`. In practice the Coder only needs `pm.a()` (= `qq.a()` = owner) for damage attribution.

---

## 6. `nq.a(pm source, int amount)` — attackEntityFrom Full Logic

### Precondition checks (return false immediately if any fails):

1. If `world.I` (isRemote / client-side): return false.
2. Set `this.bq = 0` — resets no-jump delay.
3. If `aM <= 0` (entity is dead): return false.
4. If `source.k()` (isFireDamage) AND entity has Fire Resistance potion effect (`this.a(abg.n)` = true): return false.

### Invulnerability check:

Set `bb = 1.5F` (hurt animation scale — used for rendering).
Declare local `var3 = true` (fullHitApplied flag).

```
if (ac > aq / 2.0F):       // still in "immune half" of invulnerability window
    if (amount <= bp):      // not more damage than last hit
        return false        // fully absorbed
    else:
        b(source, amount - bp)   // deal only the DIFFERENCE
        bp = amount              // record new lastDamage
        var3 = false             // mark as partial hit (no knockback, no sound)
else:                       // invulnerability window has expired (or exactly at midpoint)
    bp = amount             // record new lastDamage
    aN = aM                 // save old health (used by some callers to compute delta)
    ac = aq                 // start new invulnerability window (= 20 ticks)
    b(source, amount)       // apply full damage
    aP = aQ = 10            // hurt flash timer (10 ticks)
```

### Post-damage processing (only if `var3 = true` — full hit):

`aR = 0.0F` — reset knockback angle accumulator.

Track retaliating attacker:
- If attacker is a player (`vi`): set `bd = (vi)attacker`, `be = 60` (60-tick follow-attacker timer).
- If attacker is a wolf (`aik`) AND wolf is angry: set `bd = null`, `be = 60`.

Apply full-hit effects:
- `world.a(this, (byte)2)` — broadcasts hurt event to clients (triggers hurt animation).
- `G()` — virtual (stub in base).
- Apply knockback via `a(attacker, amount, dx, dz)` where dx/dz are direction from attacker.
  If attacker entity is null: use a random direction.

Play sound and handle death:
- If `aM <= 0` (dead after damage):
  - If `var3`: play death sound `g()` at volume `w_()`, pitch `ax()`.
  - Call `a(source)` — death handler (grants XP to killer, processes drops, etc.).
- Else if `var3`: play hurt sound `f()` at volume/pitch.

Return true.

---

## 7. `nq.b(pm source, int amount)` — Damage Application Pipeline

Called by `a()` to apply the actual HP reduction after the invulnerability check.

Steps:
1. `amount = c(source, amount)` — apply armor reduction.
2. `amount = d(source, amount)` — apply Resistance enchantment reduction.
3. `aM -= amount` — subtract from current health.

---

## 8. `nq.c(pm source, int amount)` — Armor Reduction

Called from `b()`.

If `source.d()` (isUnblockable): skip entirely, return `amount` unchanged.

Otherwise:
```
armorValue = o_()                  // total armor points (0 in base; overridden by EntityPlayer)
numerator = amount * (25 - armorValue) + aO
aO = numerator % 25                // fractional remainder carried over to next hit
return numerator / 25
```

Where `aO` (int, default 0, not persisted) is the armor rounding carry. This implements
integer truncation with carry so repeated small hits lose no more total damage than one big hit.

`o_()` virtual, default returns 0. EntityPlayer overrides to return the sum of equipped armor values.

---

## 9. `nq.d(pm source, int amount)` — Resistance Enchantment Reduction

Called from `b()`.

If entity has Resistance potion effect (`abg.m`):
```
enchLevel = b(abg.m).c()         // amplifier (0 = level 1)
reduction = (enchLevel + 1) * 5  // ticks-per-level formula
numerator = amount * (25 - reduction) + aO
aO = numerator % 25
return numerator / 25
```

Otherwise: return `amount` unchanged.

Note: both `c()` and `d()` modify and read the same carry field `aO`. In practice only one runs
per hit (unblockable sources skip `c()`; potion reduction only applies if active). If both run
on the same hit (non-unblockable source + Resistance active), `aO` from `c()` feeds into `d()`.

---

## 10. `nq.a(ia attacker, int amount, double dx, double dz)` — Knockback Application

Called when a full hit lands and `attacker != null`.

```
ap = true                       // isAirBorne flag
normalize = sqrt(dx² + dz²)
multiplier = 0.4F
v /= 2.0                        // halve current horizontal velocities
w /= 2.0
x /= 2.0
v -= dx / normalize * multiplier
w += 0.4F                       // push upward
x -= dz / normalize * multiplier
if (w > 0.4F): w = 0.4F        // cap vertical knockback
```

---

## 11. Tick Decrement of `ac` (in `nq.c()` — the main entity tick)

Each server tick, before any other logic:
```
if (aT > 0): aT--    // attackCooldown
if (aP > 0): aP--    // hurtFlashTimer
if (ac > 0): ac--    // invulnerability countdown
```

These three decrements happen unconditionally at the top of `nq.c()`.

---

## 12. Known Quirks / Bugs to Preserve

1. **"Higher damage wins" during invulnerability:** If entity is hit for 5 damage, then hit again for 8
   while still invulnerable: only `8 − 5 = 3` additional damage is applied, not 8. No knockback or
   sound plays for the 3-point partial hit. This allows two rapid hits to deal combined damage but
   prevents full re-application.

2. **`aO` carry persists between hits:** The armor rounding carry `aO` is never reset and is not
   persisted to NBT. It carries fractional damage across multiple hits within a session, which means
   the total damage dealt over many small hits equals the correct integer sum — no permanent rounding loss.

3. **`ac` in `ia`, not `nq`:** The countdown field `ac` is declared in `ia` (Entity base) even though
   it is only meaningful for LivingEntities. Plain Entity objects (paintings, item frames) carry this
   field but it has no effect unless `a(pm, int)` is called on them.

4. **`aN` is "old health" snapshot:** `aN = aM` is recorded at the moment of the full hit (before damage
   is subtracted). Some callers use `aN - aM` to compute actual damage dealt. This is the only purpose
   of `aN` in the damage path.

---

## 13. Open Questions

1. **`o_()` in hostile mobs:** `zo` and its concrete subclasses do not override `o_()` — hostile mobs
   have no armor (returns 0). EntityPlayer overrides it. This was not explicitly verified for all
   mob classes but is consistent with game behaviour (zombies don't reduce damage via worn armor in 1.0).

2. **`aF` (experience value) vs `aX`:** `nq.aF` was noted in the field list (line 23 of nq.java: `protected int aF = 0`). `zo` constructor sets `this.aX = 5`. Are `aF` and `aX` the same field? `aX` is used in `a(pm)` (death handler) at line 565: `var2.b(this, this.aF)`. If `aX` and `aF` are different, `aX=5` has no effect on XP drop. Needs verification — could be `aF` is the true experience field and `aX` is something else (attack range?).
