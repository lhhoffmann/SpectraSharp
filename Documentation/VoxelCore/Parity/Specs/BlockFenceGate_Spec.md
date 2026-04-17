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

# BlockFenceGate Spec
**Source class:** `fp.java` (BlockFenceGate)
**Superclass:** `yy` (Block)
**Block ID:** 107
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`fp` is the wooden fence gate block. It can be opened and closed by right-clicking, and
can also be opened/closed by redstone signals. When open it has no collision; when closed
it acts as a 1.5-block-tall barrier.

---

## 2. Material and Properties

| Property | Value |
|---|---|
| Material | `p.d` (wood) |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 21 (fence gate renderer) |

---

## 3. Metadata Encoding

| Bits | Meaning |
|---|---|
| Bits 0–1 (mask `0x3`) | Facing direction: 0=south, 1=west, 2=north, 3=east |
| Bit 2 (mask `0x4`) | Open flag: 0=closed, 1=open |

Helper methods:
- `b_(meta)` → `(meta & 4) != 0` — returns `true` if gate is open.
- `d(meta)` → `meta & 3` — returns facing direction (0–3).

---

## 4. AABB / Collision

**Method `b(World, x, y, z)`:**
- If open (`b_(meta) == true`): returns `null` (no collision box — entities pass through).
- If closed: returns `AxisAlignedBB(x, y, z, x+1, y+1.5, z+1)` (full width, 1.5 blocks tall).

---

## 5. Placement

**Placement validity `c(World, x, y, z)`:**
- Requires solid, opaque block below: `!world.e(x, y-1, z).b() → false`.
- Delegates to `super.c()` for any additional checks.

**On-place callback `a(World, x, y, z, LivingEntity placer)`:**
- Compute direction from placer yaw: `dir = (floor(placer.yaw * 4/360 + 0.5) & 3) % 4`.
- Set metadata: `world.f(x, y, z, dir)` (closed by default).

---

## 6. Right-Click Interaction `a(World, x, y, z, Player)`

1. Read current metadata `var6`.
2. If currently **open** (`b_(var6) == true`):
   - Close: `world.f(x, y, z, var6 & ~4)` (clear bit 2).
3. If currently **closed** (`b_(var6) == false`):
   - Compute player-facing direction `var7 = (floor(player.y * 4/360 + 0.5) & 3) % 4`.
   - If the gate's current direction equals `(playerDir + 2) % 4` (player faces opposite side):
     - Override gate direction to `var7` (flip so it opens toward the player).
   - Open: `world.f(x, y, z, newDir | 4)` (set bit 2, update direction if changed).
4. Play sound event 1003 at position.
5. Return `true`.

---

## 7. Redstone Behaviour

No explicit redstone override in `fp.java`. Redstone toggling must be handled by the
parent class or a neighbor-update path.

*Open question: does the gate respond to redstone signals via `a(World, x, y, z, face)` in
the parent Block class?*

---

## 8. Drops

Standard single-block drop: one fence gate item (inherited from `yy`).

---

## 9. Open Questions

| # | Question |
|---|---|
| 9.1 | Block ID confirmation: `fp` registered at which ID in `yy` static initializer? Likely 107 based on parity ordering. |
| 9.2 | Redstone interaction: does `fp` implement `a(World, x, y, z, int fromFace)` or rely on a signal? |
| 9.3 | Sound event 1003 — is this the door sound (same as wooden door)? |
