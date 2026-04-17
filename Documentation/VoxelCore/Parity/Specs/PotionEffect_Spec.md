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

# PotionEffect System Spec
**Source classes:** `abg.java` (Potion effect registry), `py.java` (InstantPotion subclass),
`s.java` (PotionEffect active instance), `abk.java` (ItemPotion), `pk.java` (PotionHelper)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Map

| Class | Role |
|---|---|
| `abg` | Potion — effect type definition (singleton per effect ID) |
| `py` | InstantPotion — subclass for instant effects (heal/harm) |
| `s` | PotionEffect — active effect on an entity (ID + duration + amplifier) |
| `abk` | ItemPotion — item class for drinkable and splash potions |
| `pk` | PotionHelper — decodes potion metadata into effect lists |

---

## 2. `abg` — Potion Effect Registry

### 2.1 Fields

| Field | Type | Meaning |
|---|---|---|
| `H` | `int` (public) | Effect ID |
| `I` | `String` (private) | Name key (e.g. `"potion.moveSpeed"`) |
| `J` | `int` (private) | Texture icon index in effect icons atlas (col + row × 8); -1 if none |
| `K` | `boolean` (private) | `isBadEffect` (harmful = true) |
| `L` | `double` (private) | Effect factor — controls tick interval |
| `M` | `boolean` (private) | `isAmbient` (particles are faint/ambient) |
| `N` | `int` (private) | Liquid color (RGB packed int) |

Static array: `abg.a[32]` — indexed by effect ID (slots 0 and 20-31 are `null` in 1.0).

### 2.2 Complete Effect Registry (1.0)

| ID | Field | Name key | Bad? | Color (hex) | Icon (col,row) | Factor | Ambient |
|---|---|---|---|---|---|---|---|
| 1 | `abg.c` | `potion.moveSpeed` | No | `0x7CAFC6` | (0,0) | 1.0 | No |
| 2 | `abg.d` | `potion.moveSlowdown` | Yes | `0x5A6C81` | (1,0) | 0.5 | No |
| 3 | `abg.e` | `potion.digSpeed` | No | `0xD9C043` | (2,0) | 1.5 | No |
| 4 | `abg.f` | `potion.digSlowDown` | Yes | `0x4A4217` | (3,0) | 0.5 | No |
| 5 | `abg.g` | `potion.damageBoost` | No | `0x932423` | (4,0) | 1.0 | No |
| 6 | `abg.h` | `potion.heal` | No | `0xF82423` | — | — | No |
| 7 | `abg.i` | `potion.harm` | Yes | `0x430A09` | — | — | No |
| 8 | `abg.j` | `potion.jump` | No | `0x786297` | (2,1) | 1.0 | No |
| 9 | `abg.k` | `potion.confusion` | Yes | `0x551D4A` | (3,1) | 0.25 | No |
| 10 | `abg.l` | `potion.regeneration` | No | `0xCD5CAB` | (7,0) | 0.25 | No |
| 11 | `abg.m` | `potion.resistance` | No | `0x99453A` | (6,1) | 1.0 | No |
| 12 | `abg.n` | `potion.fireResistance` | No | `0xE49A3A` | (7,1) | 1.0 | No |
| 13 | `abg.o` | `potion.waterBreathing` | No | `0x2E5299` | (0,2) | 1.0 | No |
| 14 | `abg.p` | `potion.invisibility` | No | `0x7F8392` | (0,1) | 1.0 | Yes |
| 15 | `abg.q` | `potion.blindness` | Yes | `0x1F1F23` | (5,1) | 0.25 | No |
| 16 | `abg.r` | `potion.nightVision` | No | `0x1F1FA1` | (4,1) | 1.0 | Yes |
| 17 | `abg.s` | `potion.hunger` | Yes | `0x587653` | (1,1) | 0.5 | No |
| 18 | `abg.t` | `potion.weakness` | Yes | `0x484D48` | (5,0) | 0.5 | No |
| 19 | `abg.u` | `potion.poison` | Yes | `0x4E9331` | (6,0) | 0.25 | No |
| 20–31 | — | — | — | — | — | — | — |

> Effects 20–31 are `null` in 1.0. Wither (20), Health Boost (21), Absorption (22) were added in later versions.

---

## 3. `abg.a(nq entity, int amplifier)` — performEffect (per-tick)

Only called when `shouldTriggerEffect` returns true. Switch on effect ID:

| Effect | Action |
|---|---|
| Regeneration (10) | If `health < maxHealth`: heal 1 HP (`nq.a_(1)`) |
| Poison (19) | If `health > 1`: deal 1 damage (`nq.a(DamageSource.magic, 1)`) |
| Hunger (17) | If entity is player (`vi`): `player.g(0.025F * (amplifier+1))` (add exhaustion) |
| Instant Health (6) | If entity is undead (`av()`): deal `6 << amplifier` damage. Else: heal `6 << amplifier` HP |
| Instant Damage (7) | If entity is undead: heal `6 << amplifier`. Else: deal `6 << amplifier` damage |

All other effects (speed, slowness, etc.) are applied as **attribute modifiers** — they do not call `performEffect`, they are handled by the movement/attack code reading active effect lists.

---

## 4. `abg.a(int ticksRemaining, int amplifier)` — shouldTriggerEffect

Controls whether `performEffect` fires this tick:

- **Regeneration (10) and Poison (19):**
  - `interval = 25 >> amplifier`
  - If `interval > 0`: fires when `ticksRemaining % interval == 0`
  - If `interval == 0` (high amplifier): fires every tick
- **Hunger (17):** always returns `true` (fires every tick).
- **All others:** returns `false` (never triggers per-tick; handled as attribute modifiers).

---

## 5. `s` — PotionEffect (Active Effect Instance)

### 5.1 Fields

| Field | Type | Meaning |
|---|---|---|
| `a` | `int` | Effect ID (index into `abg.a[]`) |
| `b` | `int` | Remaining duration (ticks) |
| `c` | `int` | Amplifier (0 = level I, 1 = level II, …) |

### 5.2 Tick — `a(nq entity)` → `boolean` (still active)

```
if (duration > 0):
    if (abg.shouldTriggerEffect(duration, amplifier)):
        performEffect(entity, amplifier)
    duration--
return duration > 0
```

### 5.3 Combine — `a(s other)`

Called when the same effect is applied again while already active:
- If `other.amplifier > this.amplifier`: take other's amplifier AND duration.
- Else if same amplifier AND `other.duration > this.duration`: take other's duration only.

### 5.4 Accessors

- `a()` → effect ID.
- `b()` → remaining duration ticks.
- `c()` → amplifier.
- `d()` → name key string (`abg.c()`).

### 5.5 Display Format (`toString`)

`"EffectName x (amplifier+1), Duration: ticks"` for amplifier > 0.
`"EffectName, Duration: ticks"` for amplifier 0.
Ambient effects displayed in parentheses.

---

## 6. `py` — InstantPotion (Subclass)

Overrides `a()` → returns `true` (isInstant).

Instant effects (heal/harm) use `entity.a_(hp)` for healing and `entity.a(DamageSource, dmg)` for damage.
The formula `6 << amplifier` = 6 at level I, 12 at level II, 24 at level III.

---

## 7. `abk` — ItemPotion

Item ID: 373 (constructor arg 117 → 256+117 = 373, confirmed from `acy.java`: `acy.br = new abk(117)`).

### 7.1 Properties

- Stack size: 1.
- Always edible: `a(true)`.
- Texture base column: 0.
- Eating duration: 32 ticks.
- Eat action: `ps.c` (DRINK).

### 7.2 Splash Detection — `e(int meta)`

```java
return (meta & 16384) != 0;   // bit 14
```

A potion is splash if bit 14 of its metadata is set.

### 7.3 Texture Index — `a(int meta)`

- Splash: 154.
- Drinkable: 140.

### 7.4 Drink — `a(dk, ry, vi player)` (onFinishUsing)

1. Decrement item count.
2. Server side: get effect list `pk.b(meta, false)`.
3. Apply each effect to player: `player.a(new s(effect))`.
4. If item count ≤ 0: return empty glass bottle `new dk(acy.bs)`.
5. Else: add glass bottle to player inventory separately, return original stack.

### 7.5 Right-Click — `c(dk, ry, vi player)` (onItemRightClick each tick)

- If `e(meta)` (splash): spawn `ab` (EntityPotion) and consume 1.
- Else: `player.c(stack, duration)` (start drinking animation).

### 7.6 Effect List

Effects are decoded from metadata by `pk.b(int meta, boolean isSplash)` which returns a
`List<s>`. The formula strings in `pk` encode how each ingredient bit combination maps to
specific effects and amplifiers.

---

## 8. `pk` — PotionHelper (Metadata Decoder)

PotionHelper decodes the potion metadata integer into a list of effects. The metadata encodes
the brewing recipe (which ingredients were used):

- Bits 0–5: ingredient combination (which of 6 brew ingredients present).
- Bits 6–7: tier (extended/upgraded flags).
- Bit 14: splash flag.

Formula strings (e.g. `"-0+1-2-3&4-4+13"`) define which effect ID is produced for each
bit combination. Each clause `±N` checks/modifies ingredient bit N; `&` is an AND condition.

The 32 prefix name strings (`potion.prefix.*`) map metadata values 0–31 to mundane/useless
potion names.

> Full decode algorithm is complex — recommend delegating potion metadata decoding to `pk.b()`
> directly rather than reimplementing the formula parser.

---

## 9. Open Questions

| # | Question |
|---|---|
| 9.1 | `py.java` full source — confirm only overrides `a()` to return `true`, and uses parent `performEffect`. |
| 9.2 | `ab` (EntityPotion/splash) — full spec: trajectory, explosion radius, effect application fraction. |
| 9.3 | `nq.a(s effect)` — does applying the same effect twice stack or combine (via `s.a(s other)`)? |
| 9.4 | `nq.av()` — is this the "isUndead" check (zombies, skeletons)? Confirm method name. |
| 9.5 | Effect attribute integration: where does Speed affect movement? Is it in `nq` tick or Entity.b()? |
| 9.6 | `acy.bs` = glass bottle (empty) — confirm ID (374?). |
