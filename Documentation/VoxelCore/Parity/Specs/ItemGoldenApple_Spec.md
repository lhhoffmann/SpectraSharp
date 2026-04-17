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

# ItemGoldenApple Spec
**Source classes:** `afk.java` (ItemGoldenApple), `agu.java` (ItemFood base)
**Item ID:** 322 (constructor arg 66 → 256+66 = 322)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

```
acy (Item)
  └─ agu (ItemFood)
       └─ afk (ItemGoldenApple)
```

Constructor: `afk(int itemId, int foodPoints, float saturationModifier, boolean isWolfFood)`.

---

## 2. `agu` (ItemFood) — Base Class

### 2.1 Fields

| Field | Type | Meaning |
|---|---|---|
| `b` | `int` | Food points restored (hunger bars × 2) |
| `bR` | `float` | Saturation modifier |
| `bS` | `boolean` | Is wolf food (can feed wolves) |
| `bT` | `boolean` | Always edible (can eat even when full) |
| `bU` | `int` | Potion effect ID to apply on eat (0 = none) |
| `bV` | `int` | Potion effect duration in seconds (stored × 20 = ticks) |
| `bW` | `int` | Potion effect amplifier (0 = level I, 1 = level II) |
| `bX` | `float` | Probability of effect applying per eat |

### 2.2 Effect Registration

`.a(int effectId, int durationSec, int amplifier, float probability)` → sets `bU/bV/bW/bX`.

### 2.3 Eating Duration — `b(dk)`

Returns `32` ticks. (Standard food = 32 ticks; golden apple is same length.)

### 2.4 Eat Action — `c(dk)` (UseAction enum)

Returns `ps.b` = EAT.

### 2.5 Right-Click — `c(dk, ry, vi player)`

Called every tick while player holds right-click:
- `player.b(bT)` — returns true if player can eat (checks hunger < max or `bT == true`).
- `player.c(itemStack, duration)` — starts the eating animation.

### 2.6 Finish Eating — `a(dk, ry, vi player)`

Called when eating completes (32 ticks elapsed):
1. `itemStack.a--` — consume one item.
2. `player.aO().a(this)` — award food stats (food/saturation).
3. Play burp sound `"random.burp"`.
4. Server side: if `bU > 0` AND `rand.nextFloat() < bX`:
   - Apply potion effect: `player.a(new s(bU, bV * 20, bW))`.

### 2.7 Accessors

| Method | Returns |
|---|---|
| `o()` | food points (`b`) |
| `p()` | saturation modifier (`bR`) |
| `q()` | is wolf food (`bS`) |

---

## 3. `afk` (ItemGoldenApple) — Overrides

### 3.1 Always Edible — `g(dk)`

Returns `true`. This sets the `bT` flag via `r()` or overrides the hunger check directly.
The golden apple can be eaten even when the hunger bar is full.

### 3.2 Rarity — `d(dk)`

Returns `ja.d` — `EnumRarity.EPIC`. The item name renders with a purple glow in the tooltip.

---

## 4. Effect Profile (from `agu.a()` call in `acy.java`)

> **Open Question:** exact `agu.a()` call parameters for the golden apple were not visible in the
> `acy.java` read (file cut at line 180). Below is from behavioural observation:

| Variant | Meta | Potion Effect | Duration | Amplifier | Probability |
|---|---|---|---|---|---|
| Regular Golden Apple | 0 | Regeneration (ID 10) | ~30 s (600t) | 1 (level II) | 1.0F |
| Enchanted Golden Apple | 1 | See note | — | — | — |

> Open Question 4.1: Confirm exact effect ID, duration, amplifier, probability from `acy.java` static block.
> Open Question 4.2: Does the 1.0 golden apple also grant Absorption (ID 22)? Absorption was added in 1.6.

---

## 5. Open Questions

| # | Question |
|---|---|
| 5.1 | Confirm constructor params from `acy.java`: `new afk(66, foodPoints, saturation, isWolfFood)`. |
| 5.2 | Effect parameters from `.a(effectId, durationSec, amplifier, prob)` call after construction. |
| 5.3 | Enchanted golden apple (crafted with 8 gold blocks in 1.0): separate item instance or same item with meta 1? |
| 5.4 | Does golden apple call `r()` on the `agu` instance to set `bT = true`, or does `afk.g()` override the hunger check directly? |
| 5.5 | `ja.d` = EnumRarity.EPIC — confirm enum name `ja` and value `.d`. |
