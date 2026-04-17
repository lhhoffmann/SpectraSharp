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

# ItemShears Spec
**Source class:** `abo.java`
**Item ID:** 359 (constructor arg 103 → 256+103 = 359)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

`abo` extends `acy` (Item).

Constructor:
- `h(1)` → max stack size = 1.
- `i(238)` → max durability = 238.

---

## 2. Block Hit — `a(dk itemStack, int blockId, x, y, z, nq entity)`

Called when the player successfully mines a block with shears.

**Special blocks (damage item by 1 durability, return `true`):**

| `yy` field | Block | ID |
|---|---|---|
| `yy.K` | Leaves | 18 |
| `yy.W` | Tall Grass | 31 |
| `yy.X` | Fern (tall grass meta 2) | 31 |
| `yy.bu` | Dead Bush | 32 |

For these blocks, calls `itemStack.a(1, entity)` (damage 1) and returns `true` (shears handled the break; no normal break drop).

All other blocks: falls through to `super.a()`.

---

## 3. Can Harvest Block — `a(yy block)`

Returns `true` **only** for `yy.W` (tall grass, ID 31).

This allows shears to mine tall grass at normal speed without being a tool.

---

## 4. Mining Speed — `a(dk itemStack, yy block)`

| Block | Speed |
|---|---|
| `yy.W` (tall grass, ID 31) | 15.0F |
| `yy.K` (leaves, ID 18) | 15.0F |
| `yy.ab` (cloth/wool, ID 35) | 5.0F |
| Any other | `super.a()` (1.0F default) |

---

## 5. Drop Behaviour by Block Type

When shears break a block, the `onBlockHit` handler (`§2`) returns `true` for the special blocks,
preventing the normal drop. Each block must check `isShearsHarvest` separately:

| Block | Normal drop | Shears drop |
|---|---|---|
| Leaves (`yy.K`) | 1/20 sapling | Leaves block itself |
| Tall Grass / Fern (`yy.W`/`yy.X`) | None | Tall grass / fern block |
| Dead Bush (`yy.bu`) | None | Dead bush block |
| Vines (`ahl`) | None | Vine block |
| Cobweb (ID 30) | Nothing | String (confirmed in block spec) |
| Wool (`yy.ab`) | Not via shears; this is via sheep | 1 wool block |

> Note: the vine and cobweb drop logic is in their respective block classes (`ahl`, BlockCobweb)
> which check `world.isHarvestingWithShears`. Shears' `a(yy)` (canHarvest) returns `false` for
> cobweb — cobweb has its own break check.

---

## 6. Open Questions

| # | Question |
|---|---|
| 6.1 | Does shearing a sheep use `abo.a(dk, nq entity)` (entity interaction), or is it in EntitySheep.interact()? |
| 6.2 | Shearing Mooshroom: does `abo` have a direct entity handler, or is it in EntityMooshroom? |
| 6.3 | Silk touch + leaves: is it shears OR silk touch that drops leaf blocks? Confirm shears is the leaf-collection method in 1.0. |
| 6.4 | `yy.X` — is this the fern variant of tall grass (same block ID 31, meta 2), or a separate block? |
| 6.5 | `yy.bu` — confirm dead bush block ID. |
