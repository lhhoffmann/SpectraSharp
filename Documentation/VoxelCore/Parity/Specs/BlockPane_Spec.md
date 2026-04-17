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

# BlockPane Spec (Iron Bars & Glass Pane)
**Source class:** `uh.java` (BlockPane)
**Superclass:** `yy` (Block)
**Block IDs:** 101 (iron bars), 102 (glass pane)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`uh` handles both iron bars (ID 101) and glass pane (ID 102). Both use the same class with
a `cb` (drops-item) flag to differentiate behavior. The pane forms a cross-shaped thin
pillar that visually extends toward any adjacent connecting block.

---

## 2. Fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `int` | Secondary texture index (unused in collision; used in rendering lookup) |
| `cb` | `boolean` | `true` = drops itself on break (glass pane = false; iron bars = true) |

---

## 3. Material and Properties

| Property | Value |
|---|---|
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 18 (pane/bars renderer) |

---

## 4. Connection Logic — `e(int blockId)`

A pane connects to a neighbor if:
- `yy.m[blockId]` is true (opaque/solid flag), OR
- `blockId == this.bM` (same block — iron bars connect to iron bars, pane to pane), OR
- `blockId == yy.M.bM` (glass block).

---

## 5. AABB / Collision

The collision AABB is dynamic based on neighboring blocks. Method `a(World, x, y, z, AABB, list)` 
adds multiple sub-AABBs per pane. Method `b(kq, x, y, z)` computes the bounding box for
rendering/cursor queries.

**Dimension constants:**
- Center strip half-width: `0.4375F` to `0.5625F` (i.e., 7/16 to 9/16).
- Extension range: `0.0F` to `0.5F` or `0.5F` to `1.0F`.

**Connection directions:** check north (z-1), south (z+1), west (x-1), east (x+1).

**AABB rule:**
- Check `west = e(x-1)`, `east = e(x+1)`, `north = e(z-1)`, `south = e(z+1)`.

*East-West axis:*
- If `west || east` (at least one E-W connection):
  - West only: AABB extends from `0.0` to `0.5` in X.
  - East only: AABB extends from `0.5` to `1.0` in X.
  - Both: AABB spans full `0.0` to `1.0` in X.
- Always Y: full `0.0` to `1.0`.
- Z: `0.4375` to `0.5625` (center strip).

*North-South axis (added as second box):*
- If `north || south`:
  - North only: Z from `0.0` to `0.5`.
  - South only: Z from `0.5` to `1.0`.
  - Both: full `0.0` to `1.0` in Z.
- X: `0.4375` to `0.5625` (center strip).

If neither E-W nor N-S have any connection at all, the center pillar is still `0.4375`–`0.5625`
on both X and Z axes (standalone post).

---

## 6. Default AABB `e()`

Returns full 1×1×1 box for placement checks only (`e()` sets `(0,0,0,1,1,1)`).

---

## 7. Drops `a(int fortune, Random, int meta)`

- If `!cb` (glass pane): returns `0` (no drops — glass pane drops nothing).
- Else: returns `super.a()` (drops one item = iron bars drop themselves).

---

## 8. Placement Validity `a_(kq, x, y, z, face)`

Returns `false` if a pane already exists at `(x, y, z)` (prevents placing over self).
Else delegates to `super.a_()`.

---

## 9. Quirks

**Quirk 9.1 — Glass pane drops nothing:**
When a glass pane is broken without Silk Touch, it drops nothing. Only iron bars (cb=true)
drop their item.

**Quirk 9.2 — Connection to glass:**
Panes connect to `yy.M` (full glass block) as a special case, allowing pane-glass junctions
without a gap in the visual connection.

**Quirk 9.3 — Same-type connection only:**
A glass pane (`bM == 102`) does NOT connect to iron bars (`bM == 101`) and vice versa,
because the connection check uses `blockId == this.bM`. They both connect to solid walls.

---

## 10. Open Questions

| # | Question |
|---|---|
| 10.1 | `yy.m[blockId]` — is this the per-block opacity/solid flag array? Confirm this is the opaque-cube array. |
| 10.2 | `yy.M` — confirm this is the full glass block (ID 20). |
| 10.3 | `j()` method returns `this.a` — what is this used for? Possibly a texture index for a connected texture lookup. |
| 10.4 | Silk Touch behavior — does `uh` override the fortune/drop logic for glass pane to drop itself with Silk Touch, or is it always 0? |
