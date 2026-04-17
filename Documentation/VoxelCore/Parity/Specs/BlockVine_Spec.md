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

# BlockVine Spec
**Source class:** `ahl.java` (BlockVine)
**Superclass:** `yy` (Block)
**Block ID:** 106
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`ahl` is the vine block. Vines cling to solid block faces, have no collision, spread slowly,
and drop nothing when broken without shears. They can grow downward indefinitely and spread
horizontally or upward when conditions permit.

---

## 2. Material and Properties

| Property | Value |
|---|---|
| Material | `p.k` (vine / plant-like) |
| Texture index | 143 |
| Opaque cube | `false` |
| Normal cube | `false` |
| Tickable | `true` |
| Collision AABB | `null` (no collision — entities pass through) |
| Render type | 20 |

---

## 3. Metadata Encoding

Each of the lower 4 bits represents attachment to one horizontal face:

| Bit | Mask | Attached face |
|---|---|---|
| 0 | `0x1` | East (+X) |
| 1 | `0x2` | South (+Z) |
| 2 | `0x4` | West (-X) |
| 3 | `0x8` | North (-Z) |

**Top attachment:** A vine with metadata `0` (no side bits set) is attached to the block
directly above it (hanging downward). If any side bit is set, attachment is on that face.

The visual shape reflects the bits: each set bit creates a thin 0.0625-block offset (visual
only — no hitbox).

---

## 4. Attachment Validity `d(World, x, y, z, face)`

Returns whether the vine can exist at this position attached to the given face:
- Face 1 (top): valid if block above is solid and full cube (`e(world.a(x, y+1, z))`).
- Face 2 (+Z/south): valid if block at z+1 is solid full cube.
- Face 3 (-Z/north): valid if block at z-1 is solid full cube.
- Face 4 (+X/east): valid if block at x+1 is solid full cube.
- Face 5 (-X/west): valid if block at x-1 is solid full cube.
- Face 0 (bottom): always `false` (cannot attach from below).

**Helper `e(blockId)`:** returns `true` if:
- Block is not air (`blockId != 0`).
- `yy.k[blockId].b() == true` (solid).
- `yy.k[blockId].bZ.d() == true` (material is solid/opaque — checks that it's a full cube material).

---

## 5. Placement `b(World, x, y, z, face)`

Sets initial metadata based on placement face:
| Placement face | Metadata set |
|---|---|
| 2 (south/+Z) | `0x1` |
| 3 (north/-Z) | `0x4` |
| 4 (east/+X) | `0x8` |
| 5 (west/-X) | `0x2` |

If placement face is 0 or 1 (top/bottom): metadata remains 0 (hanging vine, attached above).

---

## 6. Survival Check `g(World, x, y, z)`

Called internally when deciding to remove a vine block.

For each bit `i` (0–3) in current metadata:
- If bit `i` is set but the corresponding adjacent block is no longer a valid attachment
  surface (`!e(neighbor)`): clear bit `i` from metadata.
- Exception: if the block above this position is also a vine with the same bit set, keep it.
  (Vine below can persist even if the side attachment is gone, as long as the vine above
  is also attached there — the vine chain above is the anchor.)

After filtering:
- If all bits cleared and block above is not a valid top attachment (`!e(above)`): return `false` (remove).
- Else: if metadata changed, update with `world.f(x, y, z, newMeta)`.
- Return `true` (survive).

---

## 7. Neighbor Update `a(World, x, y, z, fromFace)`

Called on any adjacent block change (server side).
Calls `g(world, x, y, z)`:
- If `g()` returns `false`: drop (call `b()` to drop items, here nothing), set block to air.

---

## 8. Random Tick Spread `a(World, x, y, z, Random)`

Called with 1/4 probability from the base tick system.

**Density cap:** Count all vine blocks within a 4×3×4 area (x±4, y-1 to y+1, z±4). If
the count reaches 5 or more (decrements from 5 for each found), the spread is suppressed.

**Direction selection:** Pick a random value `var16` from 0–5 using `world.w.nextInt(6)`.
Look up direction index `var17 = lz.d[var16]` (maps to 0–3, representing N/S/E/W).

**Direction 1 — Upward spread:**
If `var3 < world.height - 1` and block above is air (`world.h(x, y+1, z)`):
- Pick a random subset of current metadata bits (`world.w.nextInt(16) & currentMeta`).
- For each set bit in the subset, verify the adjacent block at `(y+1)` level still connects:
  if `!e(adjacent block at y+1)`, clear that bit.
- If any bits remain: place vine at `(x, y+1, z)` with those bits.

**Directions 2–5 — Horizontal spread:**
For the chosen horizontal direction `var17`:
- If the current metadata does NOT have the bit for direction `var17` set:
  - Check target block at `(x + dx, y, z + dz)` (offset by lz.a/b).
  - If target is air or null:
    - Check if adjacent vine (current position) has perpendicular bits set that allow connecting
      to a block in that direction.
    - If so: place vine block at target with appropriate connecting bits.
    - Also handles corner spread: if the target's neighbor is a solid wall.
  - If target is a solid, full-cube block (`yy.k[target].bZ.j() && yy.k[target].b()`):
    - Add bit for this direction to current vine's metadata: `world.f(x, y, z, meta | bit)`.

**Direction 0 — Downward spread:**
If Y > 1 and block below is air:
- Copy a random subset of current metadata bits to the block below.
- If block below is already vine: merge bits with OR.

---

## 9. Drops

**Without shears:**
- `a(Random)` → `0` (no drops).
- `f()` → `0` (no drops).
- `c(int, Random)` → `0`.

**With shears (`a(World, Player, x, y, z, face)` override):**
- If player holds item `acy.bd` (Shears):
  - Force-drop vine block: `this.a(world, x, y, z, new dk(yy.bu, 1, 0))`.
  - Add to player's drops counter: `player.a(ny.C[bM], 1)`.
  - Return without calling super (skip normal drop logic).
- Else: call `super.a()` (normal break — drops nothing).

---

## 10. Texture / Rendering

The rendering method `b(kq, x, y, z)` sets the AABB to reflect which faces have vines.
Each bit in metadata produces a thin 0.0625-block slab on the corresponding face:
- Bit 1 (0x2) set: minX = 0.0F, maxX = 0.0625F (panel on -X face). minZ/maxZ full.
- Bit 3 (0x8) set: maxX = 1.0F, minX = 0.9375F (panel on +X face).
- Bit 2 (0x4) set: minZ = 0.0F, maxZ = 0.0625F.
- Bit 0 (0x1) set: maxZ = 1.0F, minZ = 0.9375F.
- No bits: hanging flat panel — checks block above; if attached above: `maxY = 0.9375F, minY = 0.0F`.

---

## 11. Light Opacity

`a(kq, x, y, z)` — returns the biome sky light value at the position. Vines do not block
sky light for purposes of light propagation; light is computed through biome context.

---

## 12. Quirks

**Quirk 12.1 — Vine chain persistence:**
When a vine's side attachment block is removed, the vine is kept if the vine block above
also has that side bit set. This creates chains of "hanging" vines that appear to defy
the missing wall, until the top anchor is removed.

**Quirk 12.2 — Spread density cap:**
The 5-vine cap counts vines in an 8×8 horizontal area ±1 Y. Dense jungles will stop
vines from spreading further even if there is technically room.

**Quirk 12.3 — Downward spread copies random bits:**
The downward-spread path takes `rand(16) & currentMeta` — so a vine may spread downward
with fewer attachment bits than its parent, or not at all if the random mask zeroes
all bits.

---

## 13. Open Questions

| # | Question |
|---|---|
| 13.1 | `lz.a[]` and `lz.b[]` — confirm these are the 4-direction X/Z offset arrays (index 0-3 for N/E/S/W). |
| 13.2 | `lz.d[]` — confirm this maps random value 0-5 to direction index 0-3 for the spread tick. |
| 13.3 | Block ID 106 — confirm from `yy` static field list. |
| 13.4 | `world.h(x, y, z)` — is this "is block replaceable" (returns true for air/grass/etc.)? |
| 13.5 | `world.w` — is this the world's Random instance used for random ticks? |
