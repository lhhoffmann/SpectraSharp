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

# BlockTrapDoor Spec
**Source class:** `mf.java` (BlockTrapDoor)
**Superclass:** `yy` (Block)
**Block IDs:** 96 (wooden trapdoor), 167 (iron trapdoor)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`mf` handles the trapdoor block (both wood and iron variants). Trapdoors are flat 1×1 panels
that can be placed against any horizontal wall face and toggled open/closed. The wooden
trapdoor can be opened by hand; the iron trapdoor requires a redstone signal.

---

## 2. Material and Properties

| Property | Value |
|---|---|
| Material | `p.b` (wood) or `p.f` (iron) — determined by constructor parameter |
| Texture index | 84 (wood), 85 (iron: `bL++` if material == p.f) |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 0 (standard block renderer with custom AABB) |

---

## 3. Metadata Encoding

| Bits | Meaning |
|---|---|
| Bits 0–1 (mask `0x3`) | Which wall the hinge is against: 0=north/+Z, 1=south/-Z, 2=east/+X, 3=west/-X |
| Bit 2 (mask `0x4`) | Open flag: 0=closed, 1=open |

Helper methods:
- `e(meta)` → `(meta & 4) != 0` — returns `true` if trapdoor is open.

**Placement `b(World, x, y, z, face)`:**
| Placement face | Direction bits |
|---|---|
| 2 (south) | 0 |
| 3 (north) | 1 |
| 4 (east) | 2 |
| 5 (west) | 3 |

---

## 4. AABB / Collision

The AABB is computed dynamically by `d(meta)`:

**Thickness constant:** `var2 = 0.1875F`

**Closed state (`e(meta) == false`):**
- Flat horizontal panel: `(0.0, 0.0, 0.0, 1.0, 0.1875, 1.0)`.

**Open state, per direction (`e(meta) == true`):**

| Direction bits | AABB description |
|---|---|
| `0x0` (+Z/north wall) | `(0.0, 0.0, 1-0.1875, 1.0, 1.0, 1.0)` — panel against +Z wall |
| `0x1` (-Z/south wall) | `(0.0, 0.0, 0.0, 1.0, 1.0, 0.1875)` — panel against -Z wall |
| `0x2` (+X/east wall) | `(1-0.1875, 0.0, 0.0, 1.0, 1.0, 1.0)` — panel against +X wall |
| `0x3` (-X/west wall) | `(0.0, 0.0, 0.0, 0.1875, 1.0, 1.0)` — panel against -X wall |

The `b(kq, x, y, z)` and `c_(World, x, y, z)` methods both call `d(meta)` to set the
correct AABB before delegating to the parent.

---

## 5. Support Validation

**`a(World, x, y, z, fromFace)` — neighbor update:**

Server side only. Compute which block this trapdoor depends on for support:

Based on `meta & 3`:
- `0x0`: support block at `(x, y, z+1)`.
- `0x1`: support block at `(x, y, z-1)`.
- `0x2`: support block at `(x+1, y, z)`.
- `0x3`: support block at `(x-1, y, z)`.

If `f(supportBlock)` returns `false` (support is not solid/full-cube):
- Remove trapdoor: `world.g(x, y, z, 0)`.
- Drop item: `b(world, x, y, z, meta, 0)`.

Then, if `fromFace > 0`: check redstone/door signal and potentially toggle.

---

## 6. Right-Click Interaction `a(World, x, y, z, Player)`

- **Iron trapdoor** (`bZ == p.f`): return `true` without doing anything (cannot be opened by hand).
- **Wooden trapdoor**: toggle bit 2 with XOR: `world.f(x, y, z, meta ^ 4)`. Play sound event 1003.
- Returns `true`.

---

## 7. Redstone Behaviour `a(World, x, y, z, boolean powered)`

If current open state `(meta & 4) != 0` does not match `powered`:
- Toggle bit 2: `world.f(x, y, z, meta ^ 4)`.
- Play sound event 1003 (null source = redstone sound variant).

---

## 8. Support Block Validity — `f(blockId)` (private static)

Returns `true` if:
- `blockId > 0` AND
- (`yy.k[blockId].bZ.j()` AND `yy.k[blockId].b()`) OR `yy.k[blockId] == yy.bd` (door block).

Effectively: solid full-cube block, or a door block.

---

## 9. Drops

Standard single-item drop: one trapdoor item (inherited).

---

## 10. Quirks

**Quirk 10.1 — AABB set on query:**
The AABB fields of `mf` are mutated by calls to `d(meta)` before every collision or
ray-cast query (`b()`, `c_()`). This means the block has mutable shared state — not
thread-safe if `b()` and `c_()` are called concurrently.

**Quirk 10.2 — Iron trapdoor ignores hand clicks:**
An iron trapdoor returns `true` from right-click (consumes the interaction) but does
nothing. It appears to "absorb" the click without opening.

**Quirk 10.3 — Door as valid support:**
The `f()` check explicitly allows door blocks (`yy.bd`) as valid support. This allows
trapdoors to attach to doors, a quirk of the support logic.

---

## 11. Open Questions

| # | Question |
|---|---|
| 11.1 | Block IDs: 96 for wood trapdoor, 167 for iron — confirm from `yy` static list. |
| 11.2 | Sound event 1003 — same as fence gate/door creak? |
| 11.3 | Bit 3 (0x8) — is there a "top half" flag (trapdoor placed on ceiling vs floor)? Not visible in `mf.java` but present in later versions. Confirm absence. |
| 11.4 | `world.v(x, y, z)` in the neighbor update — is this "is receiving redstone power"? |
