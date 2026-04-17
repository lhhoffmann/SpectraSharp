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

# BlockRail Spec (Normal Rail, Powered Rail, Detector Rail)
**Source class:** `afr.java` (BlockRail — shared base for all rail types)
**Superclass:** `yy` (Block)
**Block IDs:** 66 (Rail `yy.aG`), 27 (PoweredRail `yy.T`), 28 (DetectorRail `yy.U`)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`afr` is the shared base class for all rail blocks. Subclasses (or constructors with flags)
differentiate normal rails (curves allowed), powered rails (no curves; redstone-powered),
and detector rails (no curves; activates redstone when a minecart rides over them).

---

## 2. Static Helpers

### `g(World, x, y, z)` — is-rail at position

Returns `true` if the block at (x,y,z) is any of the three rail IDs:
`yy.aG.bM || yy.T.bM || yy.U.bM`.

### `e(int blockId)` — is-rail block ID

Returns `true` for the same three block IDs, without a world lookup.

---

## 3. Fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `boolean` | `true` if this is a "special" rail (powered rail or detector rail). Normal rail: `false`. |

Method `s()` returns `this.a`.

---

## 4. Material and Properties

| Property | Value |
|---|---|
| Material | `p.p` (rail material) |
| Opaque cube | `false` |
| Normal cube (`b()`) | `false` |
| Collision AABB (`b(World,x,y,z)`) | `null` |
| Render type | 9 |
| Drops | 1 rail item per block |

---

## 5. Metadata Encoding

### Normal Rail (metadata 0–9)

| Value | Shape |
|---|---|
| 0 | Flat, N-S |
| 1 | Flat, E-W |
| 2 | Ascending to E (+X) |
| 3 | Ascending to W (-X) |
| 4 | Ascending to N (-Z) |
| 5 | Ascending to S (+Z) |
| 6 | Curve, NE corner |
| 7 | Curve, SE corner |
| 8 | Curve, SW corner |
| 9 | Curve, NW corner |

### Powered/Detector Rail (metadata 0–5 + bit 3)

Uses shape values 0–5 only (no curves). Bit 3 (`0x8`):
- Powered Rail: `0x8` = powered/active (boosting).
- Detector Rail: `0x8` = minecart is on rail (output active).

If `s() == true`, metadata is masked with `& 7` before shape lookup.

---

## 6. AABB / Height

**Method `b(kq, x, y, z)` — visual hitbox:**
- Slope shapes (meta 2–5): `(0.0, 0.0, 0.0, 1.0, 0.625, 1.0)`.
- Flat/curve shapes: `(0.0, 0.0, 0.0, 1.0, 0.125, 1.0)`.

**Method `b(World, x, y, z)` — collision:** returns `null` (no collision).

---

## 7. Placement Validity

**`c(World, x, y, z)`:**
- Requires solid opaque block directly below: `world.g(x, y-1, z)`.

---

## 8. Neighbor Update and Auto-Connect `a(World, x, y, z)`

Called on block placement (server side):
1. Calls `a(world, x, y, z, true)` which triggers the auto-connect rail algorithm (`aiq`).

---

## 9. Redstone Update `a(World, x, y, z, fromFace)`

Server side. Computes current metadata shape (`var7 = meta & 7` for special rails).

**Support check:**
If floor under rail is gone, or if slope rail has no support on the elevated side:
- Remove self: drop and set air.

**Powered rail (yy.T) redstone check:**
1. Check direct redstone: `world.v(x, y, z)` or `world.v(x, y+1, z)`.
2. Check propagated signal: `a(world, x, y, z, meta, true, 0)` or `...false, 0` (scan along rail track up to 8 rails).
3. If powered AND bit 3 was 0: set bit 3 (active). If not powered AND bit 3 was 1: clear bit 3.
4. If state changed: notify adjacent blocks below and slope-neighbor.

**Normal rail / Detector rail (neighbor change):**
- If `fromFace > 0` AND the notifying block is redstone-conducting: trigger rail shape auto-connect.

---

## 10. Powered Rail Signal Propagation

The method `a(World, x, y, z, int meta, boolean forward, int depth)` scans along the
rail track recursively:
- `depth` cap: 8 (maximum 8 rail segments searched).
- Follows the rail's direction (meta bits 0–5) — from the `vm.java` direction table.
- At each step: check if the next block is a powered rail with bit 3 set.
  - If yes AND already has direct redstone power → return `true` (signal found).
  - If yes AND no direct power: recurse further along that rail.
- Returns `true` if a powered/active rail is found within 8 segments.

---

## 11. Texture Selection `a(int, int meta)`

- Normal rail (a=false):
  - meta 6–9 (curves): `bL - 16` (alternate curve texture).
  - else: `bL`.
- Powered/Detector rail (a=true):
  - Powered rail: if `(meta & 8) == 0` (unpowered): `bL - 16` (unlit golden rail).
  - Else: `bL`.
  - Detector rail: presumably same pattern.

---

## 12. Rail Auto-Connect — `aiq` class

The `aiq` class (RailLogic) handles auto-connecting rails. It is instantiated with the
rail block and world position, then:
1. Scans adjacent positions for other rail blocks.
2. Determines the correct shape (metadata value) based on which neighbors are also rails.
3. Sets the rail's metadata to achieve the optimal connection.

This allows placing rails to auto-curve toward connected rails. The logic is delegated
to `aiq` — not duplicated in `afr`.

---

## 13. Drop

`a(Random)` → `1`. All three rail types drop 1 item (their respective rail item).
`i()` → `0` (light opacity 0 — rails pass light).

---

## 14. Open Questions

| # | Question |
|---|---|
| 14.1 | `world.v(x, y, z)` — is this "is receiving strong redstone power" or "is receiving any power"? |
| 14.2 | Block IDs: 66=Rail, 27=PoweredRail, 28=DetectorRail — confirm from `yy` static list. |
| 14.3 | `aiq.a(new aiq(...)) == 3` in neighbor update — what does value 3 mean (possibly "T-junction, can be connected")? |
| 14.4 | Detector rail activation: triggered by minecart riding over it — is this in `vm.java` or in `afr.java`? |
