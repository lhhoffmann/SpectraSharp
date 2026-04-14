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

# BlockStairs Spec
**Source class:** `ahh.java`
**Superclass:** `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`ahh` (BlockStairs) creates L-shaped stair blocks. A stair block stores one metadata value (0–3)
that encodes which direction the stair ascends. The stair is composed of two axis-aligned bounding
boxes for collision: a lower half-block and an upper half-block, positioned to form the step.
Textures, drops, hardness, and resistance are all delegated to a parent block reference.

Stair block IDs registered in 1.0:
- 53 — Wood Stairs (parent: planks block)
- 67 — Cobblestone Stairs (parent: cobblestone block)
- 108 — Brick Stairs (parent: bricks block)
- 109 — Stone Brick Stairs (parent: stone brick block)
- 114 — Nether Brick Stairs (parent: nether brick block)

---

## 2. Fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `yy` (Block) | parentBlock: the full block whose properties this stair inherits for textures, hardness, resistance, sounds, tool type, drops, etc. |

---

## 3. Constructor

```
ahh(int blockId, yy parentBlock)
```

1. Calls `super(blockId, parentBlock.bL, parentBlock.bZ)` — uses parent's texture index and material.
2. Sets `this.a = parentBlock`.
3. `c(parentBlock.bN)` — copies parent hardness.
4. `b(parentBlock.bO / 3.0F)` — copies parent resistance divided by 3 (stairs are slightly less blast resistant).
5. `a(parentBlock.bX)` — copies parent step sound.
6. `h(255)` — sets `yy.s[blockId] = 255` (neighbor-max light, same as slabs).

---

## 4. Methods — Detailed Logic

### `a()` — isOpaqueCube

Returns false. Stair blocks are not opaque cubes (sky light passes through the open corner).

### `b()` — renderAsNormal

Returns false. Stair blocks have custom rendering.

### `c()` — lightOpacity

Returns 10. Stairs partially block light (small opacity value, not zero and not full 255).

### `b(kq, int x, int y, int z)` — setBlockBoundsForItemRender / selection box

Sets the block AABB to full cube `(0,0,0) → (1,1,1)` via `a(0,0,0,1,1,1)`. The selection
highlight (outline when looking at the stair) is a full cube regardless of the stair orientation.

Note: `c b(ry, ...)` (the collision pool variant) delegates to `super` — the bounding box
used for physics/raytrace is the standard Block one (currently-set AABB).

### `a(ry, int x, int y, int z, c boundingBox, ArrayList result)` — getCollidingBoundingBoxes

This is the multi-AABB collision method. For each stair orientation (read from world metadata
`var7 = world.getMetadata(x,y,z)`), two half-block AABBs are added to the result list.

#### Meta 0 — Ascending East (low step on west half):
```
Box A: (0.0, 0.0, 0.0) → (0.5, 0.5, 1.0)   // lower west half-column
Box B: (0.5, 0.0, 0.0) → (1.0, 1.0, 1.0)   // full-height east half
```

#### Meta 1 — Ascending West (low step on east half):
```
Box A: (0.0, 0.0, 0.0) → (0.5, 1.0, 1.0)   // full-height west half
Box B: (0.5, 0.0, 0.0) → (1.0, 0.5, 1.0)   // lower east half-column
```

#### Meta 2 — Ascending South (low step on north half):
```
Box A: (0.0, 0.0, 0.0) → (1.0, 0.5, 0.5)   // lower north half-column
Box B: (0.0, 0.0, 0.5) → (1.0, 1.0, 1.0)   // full-height south half
```

#### Meta 3 — Ascending North (low step on south half):
```
Box A: (0.0, 0.0, 0.0) → (1.0, 1.0, 0.5)   // full-height north half
Box B: (0.0, 0.0, 0.5) → (1.0, 0.5, 1.0)   // lower south half-column
```

After adding both boxes, the method calls `this.a(0,0,0,1,1,1)` to reset the AABB to full.
This is cleanup for the shared AABB state (the two boxes were added via `super.a(...)` which
reads the currently-set AABB).

### `a(ry, int x, int y, int z, nq entity)` — onBlockPlacedBy (placement orientation)

Sets the block metadata based on the placing entity's yaw angle:

```
var6 = floor(entity.yaw * 4.0 / 360.0 + 0.5) & 3
if var6 == 0: setMeta(2)   // player faces south → stair meta 2
if var6 == 1: setMeta(1)   // player faces west  → stair meta 1
if var6 == 2: setMeta(3)   // player faces north → stair meta 3
if var6 == 3: setMeta(0)   // player faces east  → stair meta 0
```

The stair ascending direction matches the direction the player is facing when placing:
- Player looks south → stair ascends southward (meta 2, low on north side).
- Player looks west → stair ascends westward (meta 1, low on east side).
- Player looks north → stair ascends northward (meta 3, low on south side).
- Player looks east → stair ascends eastward (meta 0, low on west side).

`world.f(x, y, z, meta)` = setBlockMetadataWithNotify.

### Delegated methods (all forward to `this.a` parentBlock)

All of the following methods are overridden to call the equivalent on `this.a`:

| Method | Delegates to |
|---|---|
| `b(ry, x,y,z, Random)` — randomDisplayTick | `parentBlock.b()` |
| `b(ry, x,y,z, vi)` — onBlockActivated (use) | `parentBlock.b()` |
| `e(ry, x,y,z, int)` — onBlockDestroyedByPlayer | `parentBlock.e()` |
| `e(kq, x,y,z)` — getLightValue | `parentBlock.e()` |
| `d(kq, x,y,z)` — getMiningSpeed | `parentBlock.d()` |
| `a(ia)` — getPlayerRelativeBlockHardness | `parentBlock.a()` |
| `h()` — getLightEmitted | `parentBlock.h()` |
| `a(int meta, int face)` — getTexture | `parentBlock.a(meta, 0)` (always face 0) |
| `b(int meta)` — getIconIndex | `parentBlock.a(meta, 0)` |
| `d()` — getMobSpawnType | `parentBlock.d()` |
| `c_(ry, x,y,z)` — createStackedBlockForWorldGen | `parentBlock.c_()` |
| `a(ry, x,y,z, ia, fb)` — onEntityCollidedWithBlock | `parentBlock.a()` |
| `k()` — hasTileEntity | `parentBlock.k()` |
| `a(int meta, boolean side)` — canProvidePower | `parentBlock.a()` |
| `c(ry, x,y,z)` — canBlockStay | `parentBlock.c()` |
| `a(ry, x,y,z)` — onBlockAdded | calls `a(ry,x,y,z,0)` then `parentBlock.a()` |
| `d(ry, x,y,z)` — onBlockRemoved | `parentBlock.d()` |
| `b(ry, x,y,z, ia)` — onEntityWalking | `parentBlock.b()` |
| `a(ry, x,y,z, Random)` — randomTick | `parentBlock.a()` |
| `a(ry, x,y,z, vi)` — onBlockActivated (right-click) | `parentBlock.a()` |
| `i(ry, x,y,z)` — onNeighborBlockChange | `parentBlock.i()` |

---

## 5. Bitwise & Data Layouts

Stair metadata (2 bits used):

```
Bits 1..0 = ascent direction:
  00 (0) = ascending east   (low on west side;  full height on east side)
  01 (1) = ascending west   (low on east side;  full height on west side)
  10 (2) = ascending south  (low on north side; full height on south side)
  11 (3) = ascending north  (low on south side; full height on north side)
```

No "inverted" or "upside-down" stair bit in 1.0.

---

## 6. Tick Behaviour

`randomTick` delegates to parent. Stairs themselves have no autonomous tick behaviour.

---

## 7. Known Quirks / Bugs to Preserve

1. **Full-cube selection highlight:** `b(kq, ...)` always sets AABB to (0,0,0)→(1,1,1) regardless of
   orientation. The player's block-selection outline is always a full cube even though the actual
   collision geometry is L-shaped. This is vanilla behaviour.

2. **`a(int meta, int face)` ignores face:** The texture method always calls `parentBlock.a(meta, 0)` —
   face argument is discarded and face 0 is always used. All faces of the stair use the parent block's
   "top" texture. For wooden stairs this is the planks texture; for cobblestone stairs, the cobblestone
   texture. There is no special face-dependent texture for stair blocks.

3. **Resistance divided by 3:** `b(parentBlock.bO / 3.0F)` — stair blocks have 1/3 the blast resistance
   of the parent block. Wood stairs (planks resistance=15) have resistance=5. Cobblestone stairs
   (resistance=30) have resistance=10.

4. **AABB reset after collision boxes:** The multi-AABB method resets the shared AABB to (0,0,0,1,1,1)
   after adding the two collision boxes. The Coder must ensure this shared state does not cause
   thread-safety issues if collision detection ever runs concurrently.

5. **`h(255)` — neighbor-max light:** Same as slab. Stairs do not create dark corners at the open
   L-shaped area — light propagates as if the block were non-opaque for neighbor-max purposes.

---

## 8. Open Questions

1. **`c_(ry, x,y,z)` return type:** The override returns `this.a.c_(ry, x,y,z)` — a `c` (AxisAlignedBB)
   from the parent. This seems unusual for a method named `createStackedBlock`. The return type `c`
   is AxisAlignedBB, suggesting this might actually be `getSelectedBoundingBoxFromPool()` (world-space
   AABB for selection). Worth verifying in context.

2. **Inverted stairs (upside-down):** Not present in 1.0. First added in Beta 1.6. Confirm no stair
   block in the 1.0 registry has the `isInverted` flag — no such field exists in `ahh.java`.
