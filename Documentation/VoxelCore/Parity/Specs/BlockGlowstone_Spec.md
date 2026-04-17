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

# BlockGlowstone Spec
**Source class:** `sk.java` (BlockGlowstone)
**Superclass:** `yy` (Block)
**Block ID:** 89
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`sk` is the glowstone block. It is used primarily for its light-emitting property (level 15)
and its special drop behavior: it drops 2–4 glowstone dust, with fortune affecting the upper
bound.

---

## 2. Material and Properties

`sk` extends `yy` (Block). It is a standard opaque cube with no special tick behavior or
interaction overrides beyond the drop methods.

---

## 3. Drop Behavior

### Base drop count — `a(Random)`

```
return 2 + random.nextInt(3);   // returns 2, 3, or 4
```

Range: **2–4 glowstone dust**.

### Drop count with Fortune — `a(int fortune, Random)`

```
return me.a(a(random) + random.nextInt(fortune + 1), 1, 4);
```

Computed as:
1. Take base count (2–4).
2. Add `random.nextInt(fortune + 1)` (0 to fortune).
3. Clamp to range `[1, 4]`.

At Fortune III: effective range becomes `2 + rand(3) + rand(4)` → clamped to [1, 4].
Maximum is always **4** regardless of fortune level.

### Drop item ID — `a(int, Random, int meta)`

Returns `acy.aS.bM` = **Glowstone Dust** item ID.

---

## 4. Full Block Reconstruction

To recover a full glowstone block (instead of dust), the player must use Silk Touch.
The Silk Touch behavior is handled by the base `yy` class — when the tool has Silk Touch,
the block drops itself (ID 89, count 1) instead of calling the drop methods above.

---

## 5. Light Level

Defined in the `yy` (Block) static initializer (not in `sk.java`). Glowstone emits
light level **15** (maximum). This is set via `bW` (lightValue field) during block registration.

---

## 6. Quirks

**Quirk 6.1 — Fortune capped at 4:**
No matter how high the Fortune level, maximum glowstone dust from one block is **4**.
The clamp `me.a(..., 1, 4)` enforces this.

**Quirk 6.2 — Minimum 1 dust:**
The lower clamp bound is **1**, not the base minimum of 2. This means in theory the clamped
formula could produce 1 dust if the random values were extremely bad, but in practice
`base + fortune >= 2` is almost always true.

---

## 7. Open Questions

| # | Question |
|---|---|
| 7.1 | `me.a(val, min, max)` — confirm this is `MathHelper.clamp(int, int, int)`. |
| 7.2 | Block ID 89 — confirm from `yy` static field list. |
| 7.3 | `acy.aS.bM` — confirm this is item ID 348 (Glowstone Dust). |
