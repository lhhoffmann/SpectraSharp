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

# ItemFood Spec
**Source classes:** `agu.java` (ItemFood), `eq.java` (FoodStats) — tick logic addendum
**Superclass:** `agu` extends `acy` (Item)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 0. Obfuscated Name Correction

**The REQUESTS.md entry incorrectly identified `sv` as ItemFood.**
`sv` is `EnumArt` — a Java enum listing all 25 painting variants (Kebab, Aztec, …, DonkeyKong).
The actual ItemFood class is **`agu`**. All implementations must target `agu`.

---

## 1. Purpose

`agu` (ItemFood) extends `acy` (Item) and provides the eat-animation and hunger-restoration
logic for all consumable food items. `eq` (FoodStats) is the data object that tracks the
player's hunger state; this spec also covers the missing tick logic for `eq` that was
not included in PlayerNBT_Spec.

---

## 2. `agu` — ItemFood Fields

| Field (obf) | Type | Default / Set | Semantics |
|---|---|---|---|
| `a` | `int` (public final) | 32 | Eat animation duration in ticks (always 32 — 1.6 seconds at 20 Hz) |
| `b` | `int` (private final) | ctor arg 2 | healAmount — hunger half-hearts restored (1 unit = 0.5 hunger icon) |
| `bR` | `float` (private final) | ctor arg 3 | saturationModifier — used in saturation gain formula |
| `bS` | `boolean` (private final) | ctor arg 4 | isWolfFood — whether tamed wolves can eat this item |
| `bT` | `boolean` (private) | false; set by `r()` | alwaysEdible — if true, item can be eaten even at full hunger |
| `bU` | `int` (private) | 0; set by `a(int,int,int,float)` | potionId — potion effect applied on eat (0 = no effect) |
| `bV` | `int` (private) | 0; set same | potionDuration in seconds (multiplied by 20 for ticks internally) |
| `bW` | `int` (private) | 0; set same | potionAmplifier (0 = level I) |
| `bX` | `float` (private) | 0.0F; set same | potionChance (0.0–1.0 probability) |

### Constructors

**4-argument (primary):**
```
agu(int itemId, int healAmount, float saturationModifier, boolean isWolfFood)
```
Calls `super(itemId)` → registers at `acy.d[itemId]`.

**3-argument (convenience):**
```
agu(int itemId, int healAmount, boolean isWolfFood)
```
Delegates to the 4-argument constructor with `saturationModifier = 0.6F`.

### Builder Methods (return `this`)

| Method (obf) | Effect |
|---|---|
| `r()` | Sets `bT = true` — marks item as alwaysEdible |
| `a(int potId, int durSec, int amp, float chance)` | Sets `bU`, `bV`, `bW`, `bX` — adds an on-eat potion effect |

---

## 3. `agu` — Methods

### `b(dk stack)` — getMaxItemUseDuration

Returns 32 unconditionally — all food items take exactly 32 ticks to eat.

### `c(dk stack)` — getItemUseAction

Returns `ps.b` — the "eat" use action (second value of the `ps` enum). Other values cover block, bow, drink. Used by rendering and by the "use item in hand" trigger to start the eat animation.

### `c(dk stack, ry world, vi player)` — onItemRightClick / startEating

Called when the player right-clicks while holding the food item.

```
if player.b(bT):          // canEat(alwaysEdible) — see §4.1
    player.c(stack, 32)   // startUsingItem — begins 32-tick countdown
return stack
```

`player.b(boolean alwaysEdible)` logic:
- Returns `(alwaysEdible OR foodStats.c()) AND NOT abilities.invulnerable`
- `foodStats.c()` = `foodLevel < 20` (player is not full)
- `abilities.invulnerable` (= creative mode) blocks eating

### `a(dk stack, ry world, vi player)` — onEaten (called when eat animation completes)

This is invoked at the end of the 32-tick eat animation:

1. `stack.a--` — decrement stack size by 1.
2. `player.aO().a(this)` — calls `FoodStats.a(agu item)` which internally calls `a(healAmount, satMod)` (§5.1).
3. `world.a(player, "random.burp", 0.5F, world.random.nextFloat() * 0.1F + 0.9F)` — plays burp sound with slight pitch variation.
4. If NOT client-side (`!world.I`) AND `bU > 0` (has potion effect) AND `world.random.nextFloat() < bX` (probability check):
   - `player.a(new s(bU, bV * 20, bW))` — applies potion effect `bU` for `bV×20` ticks at amplifier `bW`.
5. Return modified stack.

---

## 4. All Food Items — Registry Table

Item IDs: constructor arg N → actual item ID = N + 256 (Item base class adds 256 offset).

| Obf field | Item name | ID | healAmount | satMod | isWolfFood | alwaysEdible | On-eat potion effect |
|---|---|---|---|---|---|---|---|
| `acy.i` | Apple | 260 | 4 | 0.3F | false | no | — |
| `acy.T` | Bread | 297 | 5 | 0.6F | false | no | — |
| `acy.ap` | Raw Porkchop | 319 | 3 | 0.3F | true | no | — |
| `acy.aq` | Cooked Porkchop | 320 | 8 | 0.8F | true | no | — |
| `acy.aT` | Raw Fish | 349 | 2 | 0.3F | false | no | — |
| `acy.aU` | Cooked Fish | 350 | 5 | 0.6F | false | no | — |
| `acy.bb` | Cookie | 357 | 1 | 0.1F | false | no | — |
| `acy.be` | Melon Slice | 360 | 2 | 0.3F | false | no | — |
| `acy.bh` | Raw Beef | 363 | 3 | 0.3F | true | no | — |
| `acy.bi` | Steak (Cooked Beef) | 364 | 8 | 0.8F | true | no | — |
| `acy.bj` | Raw Chicken | 365 | 2 | 0.3F | true | no | `abg.s` (Hunger), 30s, amp 0, 30% chance |
| `acy.bk` | Cooked Chicken | 366 | 6 | 0.6F | true | no | — |
| `acy.bl` | Rotten Flesh | 367 | 4 | 0.1F | true | no | `abg.s` (Hunger), 30s, amp 0, 80% chance |
| `acy.bt` | Spider Eye | 375 | 2 | 0.8F | false | no | `abg.u` (Poison), 5s, amp 0, 100% chance |

**Notes:**
- Spider Eye additionally calls `.b(pk.d)` which sets a flag (likely marks it as not usable in Creative/survival under certain conditions — exact `pk.d` semantics not analysed).
- `abg.s` = Hunger potion effect; `abg.u` = Poison potion effect (from the LivingEntity_Spec potions enum).
- Melon Slice icon coordinates `(13, 6)` suggest it uses the sprite at atlas tile column 13, row 6.

---

## 5. `eq` — FoodStats — Complete Tick Logic

The PlayerNBT_Spec covers `eq` NBT serialisation. This section adds the missing tick method and eat method.

### 5.1 Fields (full list)

| Field (obf) | Type | Default | Semantics | Persisted |
|---|---|---|---|---|
| `a` | `int` | 20 | foodLevel (0–20) | yes — `"foodLevel"` |
| `b` | `float` | 5.0F | foodSaturationLevel (0.0–foodLevel) | yes — `"foodSaturationLevel"` |
| `c` | `float` | 0.0F | foodExhaustionLevel (0.0–40.0) | yes — `"foodExhaustionLevel"` |
| `d` | `int` | 0 | heal/starvation tick counter | yes — `"foodTickTimer"` |
| `e` | `int` | 20 | previousFoodLevel snapshot (set at start of tick) | no |

Constructor sets: `a=20`, `e=20`, `b=5.0F`. Fields `c` and `d` default to 0.

### 5.2 `a(int heal, float satMod)` — Add Food (eat handler)

Called from `a(agu item)` which passes `item.o()` and `item.p()`.

```
a = min(heal + a, 20)                        // add hunger, cap at 20
b = min(b + heal * satMod * 2.0F, (float)a)  // add saturation, cap at new foodLevel
```

Note the saturation cap: saturation is bounded by the **new** (post-restore) food level.
Saturation can never exceed foodLevel.

### 5.3 `a(vi player)` — Per-Tick Update

Called once per tick by `EntityPlayer.c()` (the LivingEntity tick).

`var2 = player.world.v` — difficulty int (0=Peaceful, 1=Easy, 2=Normal, 3=Hard).

```
e = a                           // snapshot previous food level

if c > 4.0F:
    c -= 4.0F                   // consume 4 exhaustion
    if b > 0.0F:
        b = max(b - 1.0F, 0.0F) // deplete 1 saturation point first
    else if difficulty > 0:     // not Peaceful
        a = max(a - 1, 0)       // then deplete 1 hunger unit
```

Healing check (well-fed regeneration):
```
if a >= 18 AND player.aP():     // food >= 18 AND health < maxHealth
    d++
    if d >= 80:                 // after 4 seconds
        player.a_(1)            // heal 1 HP (= 0.5 heart)
        d = 0
```

Starvation check:
```
else if a <= 0:                 // foodLevel = 0 (starving)
    d++
    if d >= 80:                 // after 4 seconds
        if (health > 10) OR (difficulty >= 3) OR (health > 1 AND difficulty >= 2):
            player.a(pm.f, 1)   // starvation damage (1 HP)
        d = 0
else:
    d = 0                       // reset counter when neither healing nor starving
```

**Key point:** Field `d` serves double duty — it is the counter for BOTH the regeneration path (food ≥ 18) and the starvation path (food = 0). Since only one branch can be active at a time, this works correctly. The counter resets to 0 in the `else` branch (mid-range food).

**Difficulty effects on starvation:**
- Peaceful (0): exhaustion and saturation deplete but `a` never drops (the `difficulty > 0` guard).
- Easy (1): hunger depletes; starvation deals damage only if health > 10 (doesn't kill below 5 hearts).
- Normal (2): starvation deals damage if health > 1 (doesn't kill at 0.5 heart) OR health > 10 (always).
  Combined: damage if `health > 1 AND difficulty >= 2` OR `health > 10` = damages down to 1 HP.
- Hard (3): `difficulty >= 3` → always applies starvation damage → can kill.

### 5.4 `a(float exhaustion)` — addExhaustion

```
c = min(c + exhaustion, 40.0F)
```

Cap: 40.0F. Exhaustion never exceeds 40. Once it does, the main tick drains it in 4-unit chunks.

**Exhaustion costs per action** (from the game's expected behaviour — not all confirmed from source in this session; verify if implementing full physics):
- Walking: ~0.01 per block
- Sprinting: ~0.1 per block
- Jumping: 0.2 per jump; sprinting jump: 0.8
- Attacking: 0.3
- Taking damage: 0.3
- Mining (breaking blocks): 0.005

### 5.5 `c()` — isHungry

```
return a < 20
```

Used by `agu.c()` to determine if the player can eat (see §3).

### 5.6 `a()` — getFoodLevel

Returns `this.a` (foodLevel int).

### 5.7 `b()` — getPreviousFoodLevel

Returns `this.e` (previousFoodLevel snapshot, updated at the start of each tick).

### 5.8 `d()` — getSaturation

Returns `this.b` (foodSaturationLevel float).

---

## 6. Bitwise & Data Layouts

No bitwise fields in `agu` or `eq`. All fields are plain types.

---

## 7. Tick Behaviour

- `agu`: not ticked. It is a stateless item instance.
- `eq.a(vi)`: called once per entity tick by EntityPlayer. At 20 Hz this means:
  - Exhaustion threshold 4.0F: at 0.8 exhaustion/tick (sprinting), saturation drains in 5 ticks.
  - Heal/starvation counter 80: triggers every 4 seconds.

---

## 8. Known Quirks / Bugs to Preserve

1. **Saturation cap uses new foodLevel, not old:** In `a(int, float)`, the saturation cap is computed after `a` (foodLevel) is updated. Eating food that restores hunger allows proportionally more saturation to be stored. Example: at food=16 eating steak (heal=8): food becomes min(16+8,20)=20; saturation can gain up to `8*0.8*2=12.8` but is capped at 20 (not 16).

2. **`d` counter resets to 0 in the mid-range food case:** If a player is at food=10 (neither ≥18 nor ≤0), `d` resets to 0 every tick. If the player drops from 18 to 17, any accumulated heal-ticks are lost; the 80-tick window restarts.

3. **Peaceful mode blocks hunger loss but not exhaustion/saturation:** On difficulty 0, exhaustion accumulates and depletes saturation normally. Only the final step (saturation→hunger) is blocked by `difficulty > 0` guard.

4. **Starvation sound/particle:** No starvation hurt sound is played. `pm.f` ("starve") is an unblockable damage source (`h()` = isUnblockable=true, `p=0.0F` = no exhaustion from the damage itself). The hurt animation does play (via `nq.a(pm, int)` normal flow).

5. **`sv` ≠ ItemFood:** `sv` is `EnumArt` (painting variants). REQUESTS.md incorrectly named the class. Correct class is `agu`.

---

## 9. Open Questions

1. **`ps.b` exact meaning:** `ps` is an enum with 5 values (a–e). Value `b` (index 1) is returned by `agu.c(dk)`. The `ps` enum values appear to correspond to use-action types (none, eat, drink, block, bow?), but their exact names were not confirmed from source. The Coder must map `ps.b` to the "eat" use action.

2. **`pk.d` on Spider Eye:** `acy.bt` calls `.b(pk.d)` on the ItemFood instance. `pk` appears to be an item-group/creative-tab enum and `d` may mean "the 'food' creative tab" or "no tab". Functional impact for survival eating is nil; it only affects item categorisation.

3. **Exhaustion from specific actions:** The `a(float)` method signature is confirmed but the callers (walking/sprinting/jumping physics in EntityPlayer) were not read in this session. The values listed in §5.4 are expected from game knowledge, not confirmed from source. Verify against `vi.java` physics tick when implementing `IFoodStats.addExhaustion()` callers.

4. **`player.c(stack, 32)` — startUsingItem:** The `vi.c(dk, int)` method that receives the start-eating call was not analysed in detail. It presumably sets an `ItemInUseCount = 32` field and begins decrementing per tick, calling `a(dk, world, player)` when it reaches 0. Confirm the exact field name and callback mechanism from `vi.java`.
