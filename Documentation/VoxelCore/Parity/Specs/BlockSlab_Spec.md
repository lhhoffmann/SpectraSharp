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

# BlockSlab Spec
**Source class:** `xs.java`
**Superclass:** `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`xs` handles both single slabs (ID 44, half-block height) and double slabs (ID 43, full block).
The same class is instantiated twice — once with `isDouble=false` for ID 44, once with `isDouble=true`
for ID 43. Metadata bits 0–2 select the material variant (stone / sandstone / wooden / cobblestone /
brick / smooth stone brick). In 1.0 there is no top-half slab — a single slab always occupies the
bottom half of a block space.

---

## 2. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `cb` | `boolean` | ctor arg 2 | isDouble: if true, this is the double-slab variant (ID 43); if false, single slab (ID 44) |

Static field:
- `a` (String[6]) = material names: `{"stone","sand","wood","cobble","brick","smoothStoneBrick"}` — index matches metadata bits 0–2.

---

## 3. Constants & Magic Numbers

| Value | Meaning |
|---|---|
| `0.5F` | Half-block height for the single-slab AABB upper bound |
| `yy.ak` | Block reference for the single slab (ID 44) — used as the drop item |
| `255` | Passed to `h()` — see §6 |

---

## 4. Constructors

**Single slab:** `xs(44, false)`
- Calls `super(44, 6, p.e)` — Block constructor with textureIndex=6, material=`p.e` (stone material).
- Sets AABB via `a(0.0F, 0.0F, 0.0F, 1.0F, 0.5F, 1.0F)` — bottom half only.
- Calls `h(255)`.

**Double slab:** `xs(43, true)`
- Calls `super(43, 6, p.e)`.
- Sets `m[43] = true` — marks block ID 43 in the Block opacity array as opaque.
- Calls `h(255)`.

**`h(255)` effect:** Sets `yy.s[blockId] = 255` — marks the block in the neighbor-max light array.
This means both slab variants receive maximum light from their neighbours rather than computing
propagation normally. Single slabs therefore do not create shadow pockets beneath them.

---

## 5. Methods — Detailed Logic

### `a()` — isOpaqueCube

Returns `cb`. Double slab (cb=true) is a fully opaque cube. Single slab (cb=false) is NOT.

### `b()` — renderAsNormal

Returns `cb`. Double slab renders as a normal full block. Single slab does not.

### `a(int meta, int face)` — getTextureForFaceWithMeta

Returns a texture atlas tile index based on the metadata variant (bits 0–2) and which face is being rendered.

| meta | face 0 (top/bottom generic) | face 1 (side) | face 2 | face 3 | face 4 | face 5 |
|---|---|---|---|---|---|---|
| 0 (stone) | 6 (stone top) | 208 | 4 | 16 | — | — |
| 1 (sandstone) | 6 | 176 | 4 | 16 | — | — |
| 2–5 (wood/cobble/brick/smooth) | 5 | 192 | 4 | 16 | `yy.al.bL` | `yy.bm.bL` |

Precise table for face 0: `meta <= 1 ? 6 : 5`.
Precise table for face 1: `meta==0 → 208; meta==1 → 176; else → 192`.
Faces 2 and 3 are always 4 and 16 regardless of meta.
Faces 4 and 5 use block references `yy.al` and `yy.bm` respectively.

`b(int meta)` (icon for inventory) = `a(meta, 0)` — delegates to the face-0 texture.

### `a(ry, int, int, int)` — onRandomTick (override)

Empty — slabs do not have random-tick behaviour.

### `a(int meta, Random random, int fortune)` — getItemDropped

Returns `yy.ak.bM` (= ID 44) unconditionally. Both single and double slabs drop the single slab item.

### `a(Random)` — quantityDropped

Returns `cb ? 2 : 1`. Double slab drops 2 items; single slab drops 1.

### `a(int meta)` — getDamageDropped (metadata of drop item)

Returns `meta` unchanged — the dropped item preserves the original material variant metadata.

### `a_(kq, int x, int y, int z, int face)` — shouldSideBeRendered

Determines whether an adjacent face of this slab should be rendered (skip if hidden behind solid geometry).

Logic for the slab-specific override:
1. If `this != yy.ak` (this is the double slab, ID 43): calls `super.a_()` first — but result is discarded (call for side effects only).
2. If `face == 1` (top face): return true — always render the top of a slab.
3. If `super.a_()` returns false: return false.
4. If `face == 0` (bottom face): return `world.getBlockId(x,y,z) != this.bM` — don't render the bottom face of a single slab if the block directly below is also a slab (avoids z-fighting between stacked slabs forming a double).
5. Other faces: return the super result.

### `c_(int meta)` — createStackedBlock (creative mode item)

Returns `new dk(yy.ak.bM, 1, meta)` — a slab item stack preserving the variant metadata.

---

## 6. Bitwise & Data Layouts

Single slab metadata (bits 0–2 only; bit 3 unused in 1.0):

```
Bits 2..0 = material variant:
  000 = stone
  001 = sandstone
  010 = wooden
  011 = cobblestone
  100 = brick
  101 = smooth stone brick
  110, 111 = undefined
```

**No top-half bit in 1.0.** The top-half slab (meta bit 3 = 8) was introduced in a later version.
A single slab is always in the BOTTOM half of the block space: y=[0.0, 0.5].

---

## 7. Tick Behaviour

Neither variant ticks. Random-tick override is empty.

---

## 8. Block Parameters Summary

| Property | Single slab (xs, ID 44) | Double slab (xs, ID 43) |
|---|---|---|
| AABB | (0,0,0) → (1, 0.5, 1) | (0,0,0) → (1, 1, 1) |
| isOpaqueCube | false | true |
| renderAsNormal | false | true |
| lightOpacity | set via `h(255)` | set via `h(255)` |
| drop item | yy.ak (ID 44) ×1 | yy.ak (ID 44) ×2 |
| drop meta | preserves input meta | preserves input meta |
| material | `p.e` (stone) | `p.e` (stone) |

---

## 9. Known Quirks / Bugs to Preserve

1. **Both variants call `h(255)`:** Even the single slab sets `h(255)`. This means single slabs
   are in the `yy.s[]` neighbor-max array. In practice this means slabs don't create dark areas
   beneath a slab ceiling — the light treats the slab as if it were a non-opaque block for neighbour-max
   purposes. This is intentional vanilla behaviour.

2. **`isOpaqueCube` vs `h(255)` mismatch:** Single slabs are NOT opaque cubes (grass below can exist,
   entities can move through the top half) but they do participate in neighbor-max light propagation.
   Do not conflate these two concepts.

3. **No top-half slab in 1.0:** The constructor only ever sets the bottom-half AABB. There is no
   metadata bit to flip the slab to the top half of the block space. This is confirmed by the absence
   of any metadata bit test in the AABB constructor path.

---

## 10. Open Questions

None. The slab class is compact and fully resolved.
