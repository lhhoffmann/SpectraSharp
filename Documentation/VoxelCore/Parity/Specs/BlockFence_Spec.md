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

# BlockFence Spec
**Source class:** `nz.java`
**Superclass:** `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`nz` (BlockFence) implements fence collision and connectivity. The fence AABB is a tall thin post
(height 1.5, width 0.25) that expands to full width along any horizontal axis that connects to an
adjacent fence or solid block. The 1.5-unit height prevents entities from jumping over the fence.

Fence block IDs in 1.0:
- 85 — Oak Fence (material `p.d` = wood)
- 113 — Nether Brick Fence (material overridden via 3-arg constructor)

Fence gate (ID 107, class `fp`) is NOT the same class, but is included in the connectivity
check — see §5.

---

## 2. Fields

No instance fields beyond Block base. All behaviour is computed from world context.

---

## 3. Constructors

**Two-argument** (default, wood material):
```
nz(int blockId, int textureIndex)
```
Calls `super(blockId, textureIndex, p.d)` — wood material.

**Three-argument** (custom material):
```
nz(int blockId, int textureIndex, p material)
```
Calls `super(blockId, textureIndex, material)` — allows nether brick fence to use stone material.

---

## 4. Methods — Detailed Logic

### `a()` — isOpaqueCube

Returns false.

### `b()` — renderAsNormal

Returns false.

### `c()` — lightOpacity

Returns 11.

### `c(kq world, int x, int y, int z)` — canFenceConnect (connectivity check)

Determines whether the block at position (x,y,z) causes this fence to extend toward it.

```
blockId = world.getBlockId(x, y, z)

if blockId == this.bM:           // same fence type
    return true
if blockId == yy.bv.bM:         // fence gate (ID 107, yy.bv)
    return true

block = yy.k[blockId]
if block == null:
    return false

// Must satisfy all three:
// 1. Has solid material
// 2. renderAsNormal = true (full cube face)
// 3. Material is not p.y (glass material is excluded)
return block.bZ.j()              // material.isSolid()
    && block.b()                 // renderAsNormal
    && block.bZ != p.y           // not glass material
```

This means fences connect to:
- Other fence blocks of the same type
- Fence gates (`yy.bv`, ID 107)
- Any solid, full-cube block (stone, dirt, planks, etc.) — but NOT glass

Fences do NOT connect to:
- Glass (material `p.y` is excluded)
- Non-full-cube solid blocks (slabs, stairs — because `renderAsNormal` is false for those)
- Air or transparent blocks

### `b(ry world, int x, int y, int z)` — getCollisionBoundingBoxFromPool

Computes the per-world-position AABB based on adjacent connectivity. Uses `c(kq, ...)` for each
of the 4 horizontal neighbours. Note: this method casts `ry` to `kq` for the connectivity check.

```
south = c(world, x,   y, z-1)
north = c(world, x,   y, z+1)
west  = c(world, x-1, y, z  )
east  = c(world, x+1, y, z  )

xMin = 0.375F    xMax = 0.625F
zMin = 0.375F    zMax = 0.625F

if south: zMin = 0.0F
if north: zMax = 1.0F
if west:  xMin = 0.0F
if east:  xMax = 1.0F

return AABB( (x+xMin), y, (z+zMin),
             (x+xMax), y+1.5F, (z+zMax) )
```

The returned AABB is always world-space (absolute coordinates). Height is always 1.5F above the
block's base Y coordinate.

### `b(kq world, int x, int y, int z)` — setBlockBoundsBasedOnState

Identical logic to `b(ry...)` above but uses `kq` (IBlockAccess) directly — no cast needed.
Sets the shared block AABB (relative coordinates, base at 0):

```
south = c(world, x,   y, z-1)
north = c(world, x,   y, z+1)
west  = c(world, x-1, y, z  )
east  = c(world, x+1, y, z  )

xMin = 0.375F    xMax = 0.625F
zMin = 0.375F    zMax = 0.625F

if south: zMin = 0.0F
if north: zMax = 1.0F
if west:  xMin = 0.0F
if east:  xMax = 1.0F

this.a(xMin, 0.0F, zMin, xMax, 1.0F, zMax)
```

Note the height in the `a()` call is `1.0F` (not 1.5F). This sets the rendering/selection
bounds to 1.0 height. The **collision** AABB (from `b(ry...)`) uses `y+1.5F` as the upper bound.
The mismatch is intentional: the selection outline is 1 block tall, but the collision box extends
to 1.5 to prevent jumping over.

### `c(ry, int x, int y, int z)` — canBlockStay

Delegates to `super.c()` — standard stability check (solid block below).

---

## 5. Bitwise & Data Layouts

Fences have no metadata in 1.0. All fences are identical regardless of placement orientation.
Connectivity is computed dynamically each tick/render from adjacent block types.

---

## 6. Tick Behaviour

No tick behaviour. Fences are purely static collision objects with dynamic AABB computation.

---

## 7. AABB Summary

| Connectivity | xMin | xMax | zMin | zMax | Height |
|---|---|---|---|---|---|
| Isolated post | 0.375 | 0.625 | 0.375 | 0.625 | 1.5 |
| South extends | 0.375 | 0.625 | 0.0 | 0.625 | 1.5 |
| North extends | 0.375 | 0.625 | 0.375 | 1.0 | 1.5 |
| West extends | 0.0 | 0.625 | 0.375 | 0.625 | 1.5 |
| East extends | 0.375 | 1.0 | 0.375 | 0.625 | 1.5 |
| E+W rail | 0.0 | 1.0 | 0.375 | 0.625 | 1.5 |
| N+S rail | 0.375 | 0.625 | 0.0 | 1.0 | 1.5 |
| All four | 0.0 | 1.0 | 0.0 | 1.0 | 1.5 |

The collision AABB is world-space (`c b(ry, ...)`), so add the block's integer X and Z coordinates.

---

## 8. Known Quirks / Bugs to Preserve

1. **Selection box height = 1.0, collision height = 1.5:** `b(kq)` uses `this.a(..., 1.0F, ...)` for
   the block selection/render AABB (1 block tall), while `b(ry)` returns an AABB with upper Y =
   `blockY + 1.5F` for collision. Entities cannot jump over the fence (1.5F), but the block
   selection highlight is only 1 block tall. The discrepancy is vanilla.

2. **Nether brick fence does NOT connect to wood fence:** The `canFenceConnect` check uses
   `blockId == this.bM` (same ID). Fence ID 85 and fence ID 113 have different `bM` values, so
   they do not connect to each other. Two adjacent fence types form disconnected posts.

3. **Glass exclusion is material-based, not block-based:** Any block with material `p.y` (the
   glass/portal material) is excluded from fence connectivity, regardless of ID. This includes
   the End Portal block. Conversely, a custom block with material=`p.y` and `renderAsNormal=true`
   would still not connect.

4. **Fence gate `yy.bv` is hard-coded in the connectivity check:** The fence gate (ID 107) is
   checked by name `yy.bv` before the general solid-block check. If the fence gate were not
   hard-coded here, it would fail the `renderAsNormal` test (since gates are not full cubes).

---

## 9. Open Questions

1. **`c(ry, x,y,z)` — canBlockStay:** Delegates to `super`. The super implementation in `yy`
   (Block base) typically checks that the block below is solid. Confirm from Block_Spec whether
   this is the case or if the default is always-stable.

2. **Direction labelling for z-axis:** This spec uses south=z−1 and north=z+1 based on the
   Minecraft 1.0 coordinate convention where increasing Z is "north" in the renderer. Verify
   against the Block_Spec or World_Spec coordinate system documentation.
