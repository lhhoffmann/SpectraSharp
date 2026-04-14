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

# BlockDoor Spec
**Source class:** `uc.java`
**Superclass:** `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`uc` (BlockDoor) implements the two-block-tall wood door (ID 64) and iron door (ID 71). Each door
occupies two block positions: a bottom half and a top half. The bottom half stores the facing
direction (bits 0–1); the open/closed state (bit 2) is mirrored between both halves on each
toggle. The top half is identified by bit 3 being set.

Collision is a thin slab (0.1875-wide) along one face, oriented perpendicular to the stored
facing direction; the AABB rotates 90° when the door is open. Wood doors can be activated by
player right-click; iron doors can only be activated by redstone.

---

## 2. Fields

No instance fields beyond Block base. The wood/iron distinction is carried entirely by the
material (`bZ`) that is passed to the constructor:
- Wood door (ID 64): `p.e` (wood material)
- Iron door (ID 71): `p.f` (iron material)

---

## 3. Constructor

```
uc(int blockId, p material)
```

1. Calls `super(blockId, material)`.
2. Sets `bL = 97` (texture index; 97 = wood door bottom frame).
3. If `material == p.f` (iron): `bL = 98` (iron door bottom frame).
4. Sets initial block AABB via `a(0.0, 0.0, 0.0, 1.0, 1.0, 1.0)` — full cube default.

Note: the texture index table is described in §4 (getTextureIndex).

---

## 4. Methods — Detailed Logic

### `a()` — isOpaqueCube

Returns false.

### `b()` — renderAsNormal

Returns false.

### `c()` — lightOpacity

Returns 7. Doors partially block light.

### `b(kq world, int x, int y, int z)` — setBlockBoundsBasedOnState

```
meta = world.getMetadata(x, y, z)
e(f(meta))
```

Reads the block metadata at (x,y,z), passes it through `f()` to get the effective facing
direction (0–3), then calls `e()` to set the shared AABB.

### `b(ry world, int x, int y, int z)` — getCollisionBoundingBoxFromPool

```
b((kq)world, x, y, z)         // sets bounds via setBlockBoundsBasedOnState
return super.b(world, x, y, z) // returns the now-set AABB
```

### `c_(ry world, int x, int y, int z)` — getSelectedBoundingBoxFromPool

Same as `b(ry...)` — calls `b(kq)` to set bounds, then delegates to super.

### `e(int effectiveFacing)` — setBlockBoundsForFacing

Sets the shared block AABB (`a(xMin, yMin, zMin, xMax, yMax, zMax)`) based on effective
facing direction. Always starts with a reset call, then immediately overwrites it:

```
a(0.0, 0.0, 0.0, 1.0, 2.0, 1.0)    // reset (always overridden by one of the 4 cases below)

var2 = 0.1875F                        // thickness constant

if effectiveFacing == 0:  a(0.0,   0.0, 0.0,  1.0,   1.0, var2)   // south face: panel at z=0 side
if effectiveFacing == 1:  a(1.0-var2, 0.0, 0.0, 1.0, 1.0, 1.0)   // east face:  panel at x=1 side
if effectiveFacing == 2:  a(0.0,   0.0, 1.0-var2, 1.0, 1.0, 1.0) // north face: panel at z=1 side
if effectiveFacing == 3:  a(0.0,   0.0, 0.0,  var2, 1.0, 1.0)    // west face:  panel at x=0 side
```

`1.0F - 0.1875F = 0.8125F`

The height in all four cases is 1.0F (one block tall). Each door half has its own separate
1.0-high collision box. The door is visually 2 blocks tall because the top half sits at y+1.

The initial `a(0, 0, 0, 1, 2, 1)` is always immediately overridden and has no observable effect.
It is documented for exact code parity.

### `f(int meta)` — computeEffectiveFacing

```
if (meta & 4) == 0:   return (meta - 1) & 3    // closed: facing rotated back by 1
else:                  return meta & 3            // open:   raw facing bits
```

Maps raw metadata to the effective panel direction used by `e()`:

| meta bits 0–2 | isOpen | f() result | Panel position |
|---|---|---|---|
| 0 (facing=0) | 0 | 3 | West face |
| 1 (facing=1) | 0 | 0 | South face |
| 2 (facing=2) | 0 | 1 | East face |
| 3 (facing=3) | 0 | 2 | North face |
| 4 (facing=0) | 1 | 0 | South face |
| 5 (facing=1) | 1 | 1 | East face |
| 6 (facing=2) | 1 | 2 | North face |
| 7 (facing=3) | 1 | 3 | West face |

Opening the door rotates the panel 90° clockwise (when viewed from above):
- Facing 0: closed → west panel, open → south panel
- Facing 1: closed → south panel, open → east panel
- Facing 2: closed → east panel, open → north panel
- Facing 3: closed → north panel, open → west panel

Top-half metadata (bit 3 = 1, values 8–15): `f()` uses only bits 0–2, so bit 3 (isTopHalf)
has no effect on the facing computation. Both halves produce the same XZ panel shape.

### `a(ry world, int x, int y, int z, vi player)` — onBlockActivated (right-click toggle)

```
if material == p.f (iron door):
    return true              // iron doors cannot be activated by right-click

meta = world.getMetadata(x, y, z)

if (meta & 8) != 0:          // clicked the top half
    if block at (x, y-1, z) == this block:
        a(world, x, y-1, z, player)    // delegate to bottom half
    return true

// Bottom half logic:
topMeta = (meta ^ 4) + 8     // toggle open bit, add top-half marker
if block at (x, y+1, z) == this block:
    world.setBlockMetadata(x, y+1, z, topMeta)

world.setBlockMetadata(x, y, z, meta ^ 4)         // toggle open bit on bottom half
world.notifyBlocksOfNeighborChange(x, y-1, z, x, y, z)
world.playAuxSFX(player, 1003, x, y, z, 0)        // door sound
return true
```

Both halves are always updated simultaneously. Sound event 1003 = door open/close sound.

### `a(ry world, int x, int y, int z, boolean open)` — setDoorState (redstone-driven toggle)

Sets the door to the specified open state. Called by the neighbor-change handler when
a redstone signal changes.

```
if (meta & 8) != 0:          // top half: delegate to bottom
    if block at (x, y-1, z) == this:
        a(world, x, y-1, z, open)
    return

current = (meta & 4) > 0     // current open state
if current != open:          // only act if state would change
    if block at (x, y+1, z) == this:
        world.setBlockMetadata(x, y+1, z, (meta ^ 4) + 8)
    world.setBlockMetadata(x, y, z, meta ^ 4)
    world.notifyBlocksOfNeighborChange(x, y-1, z, x, y, z)
    world.playAuxSFX(null, 1003, x, y, z, 0)     // null player = ambient sound
```

### `b(ry world, int x, int y, int z, vi player)` — onEntityWalking

Delegates: `a(world, x, y, z, player)` — same as `onBlockActivated`.

### `a(ry world, int x, int y, int z, int neighborId)` — onNeighborBlockChange

Handles structural integrity and redstone-triggered state changes.

```
meta = world.getMetadata(x, y, z)

if (meta & 8) != 0:          // TOP HALF
    if block at (x, y-1, z) != this:
        world.removeBlock(x, y, z)        // orphaned top — remove self
    if neighborId > 0 AND Block.byId[neighborId].isPowerSource():
        a(world, x, y-1, z, neighborId)  // propagate to bottom half
    return

// BOTTOM HALF
removed = false
if block at (x, y+1, z) != this:
    world.removeBlock(x, y, z)           // orphaned bottom — remove self
    removed = true

if !world.isBlockSolidOnSide(x, y-1, z):
    world.removeBlock(x, y, z)           // no support below
    removed = true
    if block at (x, y+1, z) == this:
        world.removeBlock(x, y+1, z)     // also remove top half

if removed:
    if !world.isRemote:
        dropBlockAsItem(world, x, y, z, meta, 0)  // drop door item
else if neighborId > 0:
    powered = world.isBlockIndirectlyReceivingPower(x, y, z)
            OR world.isBlockIndirectlyReceivingPower(x, y+1, z)
    a(world, x, y, z, powered)          // set door state from redstone
```

`world.isRemote` = `ry.I` (client-side flag). Drops are suppressed on client.

### `c(ry world, int x, int y, int z)` — canBlockStay

```
if y >= world.height - 1:      return false    // too high (no room for top half)
if !world.isBlockSolidOnTop(x, y-1, z):  return false
if !super.canBlockStay(x, y, z):         return false
if !super.canBlockStay(x, y+1, z):       return false
return true
```

`world.height` = `ry.c` (world height limit = 128 in 1.0).

Requires: solid block below, structural validity at both y and y+1.

### `a(int face, int meta)` — getTextureIndex

Returns the texture atlas index for each face of the door. Faces 0 and 1 (bottom/top Y faces)
always return `bL`. For lateral faces (2–5):

```
effectiveFacing = f(meta)

condition = ((effectiveFacing == 0 || effectiveFacing == 2) XOR (face <= 3))

if condition:
    return bL                              // "flat" face: use base texture

else:
    var4 = effectiveFacing / 2 + (face & 1 XOR effectiveFacing)
    var4 += (meta & 4) / 4                // +1 if door is open
    var5 = bL - (meta & 8) * 2            // bL-16 if top half, bL if bottom
    if (var4 & 1) != 0:
        var5 = -var5                       // negate = horizontally mirrored
    return var5
```

**Key values:**
- Bottom half, wood door: `bL = 97` → top half uses `bL - 16 = 81`
- Bottom half, iron door: `bL = 98` → top half uses `bL - 16 = 82`
- A negative return value signals the renderer to mirror the texture horizontally. This is a
  vanilla rendering convention used for doors and other directional blocks.

### `a(int meta, Random rng, int fortune)` — getItemDropped

```
if (meta & 8) != 0:   return 0         // top half: no drop
if material == p.f:   return ironDoorItemId   (acy.aA.bM)
else:                  return woodDoorItemId   (acy.au.bM)
```

Only the bottom half drops the door item. Top half drops nothing (prevents double-drop).

### `i()` — unknown single-value method

Returns 1. Likely `getMobilityFlag()` (piston pushability) = 1 (can be pushed). See §8.

### `g(int meta)` — static: isOpen

```
return (meta & 4) != 0
```

Static utility used by other classes to check door open state from metadata alone.

### `a(ry world, int x, int y, int z, fb a, fb b)` — getMovingObjectPositionFromPool (ray test)

```
b((kq)world, x, y, z)              // update bounds to current state
return super.a(world, x, y, z, a, b)
```

---

## 5. Bitwise & Data Layouts

```
Bottom half metadata (bit 3 = 0):

  Bit 3 (0x8): isTopHalf = 0 (bottom half)
  Bit 2 (0x4): isOpen    = 1 if open, 0 if closed
  Bits 1–0    : facing   = 0–3 (see table below)

Top half metadata (bit 3 = 1):

  Bit 3 (0x8): isTopHalf = 1 (top half)
  Bit 2 (0x4): isOpen    = mirrored from bottom half on toggle
  Bits 1–0    : facing   = 0–3 (same as bottom half)

Facing values (bits 1–0):

  00 (0) → closed: west panel,  open: south panel
  01 (1) → closed: south panel, open: east panel
  10 (2) → closed: east panel,  open: north panel
  11 (3) → closed: north panel, open: west panel
```

The facing value in bits 0–1 encodes the direction the player was looking when placing the
door. The `f(meta)` function translates this to the physical panel direction. The item code
(not the block class) sets the initial facing at placement time.

---

## 6. Tick Behaviour

No random-tick or scheduled-tick behaviour. Door state is changed only by:
- Player right-click (`onBlockActivated`)
- Redstone signal change (`onNeighborBlockChange`)
- Structural integrity loss (`onNeighborBlockChange`)

---

## 7. AABB Summary

| effectiveFacing | Panel position | xMin | xMax | zMin | zMax | Height |
|---|---|---|---|---|---|---|
| 0 | South face (z=0 side) | 0.0 | 1.0 | 0.0 | 0.1875 | 1.0 |
| 1 | East face (x=1 side) | 0.8125 | 1.0 | 0.0 | 1.0 | 1.0 |
| 2 | North face (z=1 side) | 0.0 | 1.0 | 0.8125 | 1.0 | 1.0 |
| 3 | West face (x=0 side) | 0.0 | 0.1875 | 0.0 | 1.0 | 1.0 |

`0.1875 = 3/16` (3 pixels wide in a 16×16 texel grid).

Both the top and bottom half use the same XZ dimensions. The two halves are stacked at y and y+1.

---

## 8. Known Quirks / Bugs to Preserve

1. **Dead-code reset in `e()`:** `e()` always calls `a(0,0,0,1,2,1)` first (2-block-tall AABB),
   then immediately overwrites it with a 1-block-tall AABB. Since all four facing cases (0–3)
   always fire for any valid meta, the 2.0F height is never observable. It is preserved verbatim.

2. **Iron door right-click is silently swallowed:** `onBlockActivated` returns `true` for iron
   doors without doing anything. This suppresses any secondary action (e.g., placing a block
   behind the door from the player's perspective) — the click is consumed. Vanilla behaviour.

3. **Top half has no independent drop:** `getItemDropped` returns 0 for the top half. If a
   player breaks the top half first, the door item is lost. The bottom half always drops the
   full item regardless of which half was broken (because the break event is always dispatched
   with the metadata of the broken block; if top half is broken, `meta & 8` guards are applied
   in the item-drop logic).

4. **Sound uses null player for redstone activation:** `setDoorState` calls `playAuxSFX(null, 1003, ...)`
   (null player) whereas `onBlockActivated` uses the actual player. The sound emitted is the
   same (1003), but the source entity differs.

5. **`canBlockStay` checks world height:** If placed near the top of the world (y ≥ 127 in 1.0),
   `canBlockStay` returns false even if y is structurally valid — the top half cannot fit.

---

## 9. Open Questions

1. **`i()` return value 1:** Block base `i()` is likely `getMobilityFlag()` (0=immovable, 1=normal,
   2=destroy-only for pistons). A value of 1 for the door seems correct (doors can be pushed by
   pistons in later versions), but the field may have a different semantic in 1.0. Confirm from
   Block base spec when available.

2. **Placement facing logic:** The initial metadata (facing bits 0–1) is set by the door *item*
   class (not the block), based on the placing entity's yaw. The block class has no
   `onBlockPlacedBy` override. The item class also places the top half at y+1 with metadata
   `facing + 8`. The exact facing→bits mapping should be confirmed from the door item spec
   when analysed.

3. **`b(ry, x, y, z, ia)` — onEntityWalking delegates to `a()`:** This means walking into a
   door triggers `onBlockActivated`. For iron doors this is a no-op (returns true without acting).
   For wood doors this would toggle the door open/closed on each step — identical to right-clicking.
   Confirm whether this is intentional or a vanilla quirk in 1.0.
