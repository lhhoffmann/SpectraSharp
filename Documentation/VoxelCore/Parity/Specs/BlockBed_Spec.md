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

# BlockBed Spec
Source class: `aab.java`
Superclass: `yy` (= `Block`)
Block ID: 26
Item: `kn` (item ID 99, `acy.aZ`, `bM` = 355)
Placed as: `yy.S = new aab(26).c(0.2F).a("bed").r().l()`

Supporting methods in: `vi.java` (EntityPlayer — trySleep, wakeUpPlayer)
Related enum: `qy.java` (EnumStatus — sleep attempt result)
Related enum: `dh.java` (block coordinate triple — bed position)

---

## 1. Purpose

BlockBed is a two-block-tall (horizontal) structure that allows players to sleep through the
night, setting their spawn point and (when all players sleep) skipping time to dawn.
In the Nether or dimensions without a sky, right-clicking a bed triggers an explosion
instead of sleeping.

---

## 2. Fields

| Field (obf) | Value / Type | Semantics |
|---|---|---|
| `bM` | 26 | block ID |
| material | `p.m` | cloth/bed material |
| base texture | 134 | terrain.png index (`bL`) |
| AABB base | (0, 0, 0, 1, 0.5625, 1) | height = 9/16 |
| `a` (static) | `int[][]` | direction offsets per facing (see §5) |

---

## 3. Constants & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `0.5625F` (= 9/16) | AABB height | bed is 9/16 of a block tall |
| `134` | `bL` | terrain.png texture base index |
| `8` | metadata bit 3 | isHead flag (1 = head half, 0 = foot half) |
| `4` | metadata bit 2 | isOccupied flag |
| `3` | metadata bits 0-1 | facing (0=south, 1=west, 2=north, 3=east) |
| `5.0F` | explosion power | Nether bed explosion power |
| `true` | explosion isIncendiary | Nether explosion is incendiary |
| `3.0` | trySleep XZ distance limit | player must be within 3 blocks XZ |
| `2.0` | trySleep Y distance limit | player must be within 2 blocks Y |
| `8.0` | monster scan XZ radius | mob-safety check radius |
| `5.0` | monster scan Y radius | mob-safety check Y half-height |
| `0.2F` | sleep size (width, height) | player is shrunk when sleeping |
| `0.9375F` | sleep Y offset | player center placed at y + 0.9375 |
| `1.8F` | sleep offset magnitude | position offset applied per facing direction (bV/bX fields) |
| `9/16 = 0.5625` | wakeup Y offset | player placed at `var5.b + L + 0.1` where L=0.9375 ≈ 1.0 above bed |

---

## 4. Metadata Bit Layout

```
Bit 7 .. 4  = unused
Bit 3       = isHead (0 = foot half, 1 = head half)
Bit 2       = isOccupied (0 = free, 1 = occupied — set when player enters sleep)
Bits 1-0    = facing (0 = south [+Z], 1 = west [-X], 2 = north [-Z], 3 = east [+X])
```

Static helper methods:
- `e(int meta)` = `meta & 3` → facing
- `f(int meta)` = `(meta & 8) != 0` → isHead
- `g(int meta)` = `(meta & 4) != 0` → isOccupied

---

## 5. Facing Direction Table

```
static int[][] a = {{0,1}, {-1,0}, {0,-1}, {1,0}}
```

Indexed by facing (bits 0-1). Gives `{dx, dz}` from **foot to head**:
| facing | dx | dz | human direction |
|---|---|---|---|
| 0 | 0 | +1 | south |
| 1 | -1 | 0 | west |
| 2 | 0 | -1 | north |
| 3 | +1 | 0 | east |

---

## 6. AABB

### setBlockBoundsBasedOnState (`b(kq, int, int, int)`)
Resets to standard bed bounds: `(0, 0, 0, 1, 0.5625, 1)` — no metadata-dependent shape.

### isOpaqueCube (`a()`) → `false`
### renderAsNormalBlock (`b()`) → `false`
### `c()` → `14` (purpose unclear — possibly `getLightOpacity` or `getFlammability`)
### `i()` → `1` (purpose unclear)

---

## 7. onBlockActivated (`a(ry, int, int, int, vi)`)

Full sleep-attempt / Nether-explode logic. Always returns `true`.

```
1. If client-side (world.I): return true.

2. Get metadata var6 = world.getBlockMetadata(x, y, z).

3. If this is the FOOT half (!f(var6)):
   - facing = e(var6)
   - x += a[facing][0];  z += a[facing][1]   // move to head
   - If world.getBlockId(x, y, z) != 26: return true  // orphaned foot half
   - var6 = world.getBlockMetadata(x, y, z)   // use head metadata

4. We are now at the HEAD position (x, y, z).

5. If dimension has no sky (!world.y.d()):     // Nether / End
   - midX = x + 0.5,  midY = y + 0.5,  midZ = z + 0.5
   - world.g(x, y, z, 0)  // remove head
   - facing = e(var6);  fx = x + a[facing][0];  fz = z + a[facing][1]
   - If world.getBlockId(fx, y, fz) == 26:
       world.g(fx, y, fz, 0)  // remove foot too
       midX = (midX + fx + 0.5) / 2;  midY = ...; midZ = ...   // average position
   - world.a(null, midX, midY, midZ, 5.0F, true)  // EXPLODE — power 5, incendiary
   - return true

6. If occupied (g(var6)):
   - Scan world.i (player list); find player where ar()==true AND bU == (x,y,z)
   - If found sleeping player var16:
       player.b("tile.bed.occupied")   // send message
       return true
   - Else (stale occupied flag): clear it — a(world, x, y, z, false)

7. result = player.d(x, y, z)   // trySleep

8. If result == qy.a (OK):
   - a(world, x, y, z, true)   // set occupied flag
   - return true
   Else if result == qy.c: player.b("tile.bed.noSleep")
   Else if result == qy.f: player.b("tile.bed.notSafe")
   return true
```

---

## 8. setOccupied (static `a(ry, int, int, int, boolean)`)

Sets or clears bit 2 (isOccupied) in head block metadata:

```
meta = world.getBlockMetadata(x, y, z)
if isOccupied: meta |= 4
else: meta &= ~4   (= meta & 0xFFFFFFFB)
world.f(x, y, z, meta)   // setBlockMetadataWithNotify
```

---

## 9. onNeighborBlockChange (`a(ry, int, int, int, int)`)

Removes orphaned halves when the other half is missing.

```
meta = world.getBlockMetadata(x, y, z)
facing = e(meta)
if f(meta):   // this is head half
    if world.getBlockId(x - a[facing][0], y, z - a[facing][1]) != 26:
        world.g(x, y, z, 0)   // remove orphaned head
else:   // this is foot half
    if world.getBlockId(x + a[facing][0], y, z + a[facing][1]) != 26:
        world.g(x, y, z, 0)   // remove orphaned foot
        if !client: this.b(world, x, y, z, meta, 0)   // drop item
```

---

## 10. Drops

### getItemDropped (`a(int meta, Random, int fortune)`)
```
if f(meta): return 0          // head half drops nothing
else: return acy.aZ.bM        // foot half drops bed item (bM = 355)
```

### dropBlockAsItemWithChance (`a(ry, x, y, z, meta, float, int)`)
```
if !f(meta):   // foot half only
    super.a(...)   // standard drop
// head half: do nothing (override prevents double-drop)
```

**Result:** Exactly one bed item drops regardless of how the bed is broken (from foot half or head half). Specifically: if foot is broken directly, it drops. If head is broken, it drops nothing (but foot's onNeighborBlockChange removes foot and drops there).

---

## 11. Texture Mapping (`a(int face, int meta)`)

Bed has complex face→texture mapping based on facing and half. Referenced lookup table `lz.h[facing][face]` maps block face IDs per orientation. Base texture `bL = 134`.

```
if face == 0 (bottom): return yy.x.bL   // wood planks texture (generic bed bottom)

facing = e(meta);  faceDir = lz.h[facing][face]
if f(meta):   // head half
    if faceDir == 2: return bL + 2 + 16    // pillow top (mirrored)
    else if faceDir == 5 OR 4: return bL + 1 + 16   // head side (mirrored)
    else: return bL + 1                     // head side (normal)
else:   // foot half
    if faceDir == 3: return bL - 1 + 16    // foot top (mirrored)
    else if faceDir == 5 OR 4: return bL + 16   // foot side (mirrored)
    else: return bL                         // foot side (normal)
```

---

## 12. EntityPlayer — trySleep (`vi.d(int, int, int)`)

Returns `qy` (EnumStatus) enum value.

```
d(int bedX, int bedY, int bedZ):

1. If NOT client-side:
   a. If already sleeping (ar()) OR dead (!K()): return qy.e
   b. If world.y.c == true (dimension prevents sleeping — set by Nether/End WorldProvider):
      return qy.b
   c. If world.l() (isDaytime: skyDarkeningValue < 4): return qy.c
   d. If |player.s - bedX| > 3.0 OR |player.t - bedY| > 2.0 OR |player.u - bedZ| > 3.0:
      return qy.d   // too far
   e. If any zo (EntityMonster) within AABB (bedX±8, bedY±5, bedZ±8):
      return qy.f   // not safe

2. Set player size: a(0.2F, 0.2F);  L = 0.2F

3. If bed block at (bedX, bedY, bedZ) exists (world.i(bedX, bedY, bedZ)):
   facing = aab.e(world.getBlockMetadata(bedX, bedY, bedZ))
   // Position player inside bed based on facing:
   offsetX = 0.5F;  offsetZ = 0.5F
   switch facing:
     case 0: offsetZ = 0.9F
     case 1: offsetX = 0.1F
     case 2: offsetZ = 0.1F
     case 3: offsetX = 0.9F
   b(facing)   // set bV/bX for sleeping pose (offset ±1.8F)
   d(bedX + offsetX, bedY + 0.9375F, bedZ + offsetZ)   // teleport into bed

4. Else (bed missing during client-predicted call):
   d(bedX + 0.5F, bedY + 0.9375F, bedZ + 0.5F)   // center

5. bT = true        // isSleeping = true
6. a = 0            // sleep tick counter = 0
7. bU = new dh(bedX, bedY, bedZ)   // bed head position
8. v = x = w = 0.0  // velocity = zero

9. If server: world.A()   // checkAllPlayersSleeping (may advance time)

10. Return qy.a
```

---

## 13. EntityPlayer — Sleeping Pose Offsets (`vi.b(int facing)`)

Sets `bV` and `bX` fields (sleep pose visual offsets):

| facing | bV | bX | effect |
|---|---|---|---|
| 0 (south) | 0.0F | -1.8F | shift toward foot (+Z) |
| 1 (west) | 1.8F | 0.0F | shift toward foot (-X) |
| 2 (north) | 0.0F | 1.8F | shift toward foot (-Z) |
| 3 (east) | -1.8F | 0.0F | shift toward foot (+X) |

---

## 14. EntityPlayer — wakeUpPlayer (`vi.a(boolean setSpawn, boolean broadcastWake, boolean setSpawnpoint)`)

```
1. Restore player size: a(0.6F, 1.8F);  aF()   // resetPositionToBB

2. var4 = bU (bed position)
   var5 = bU (wake position — same initially)

3. If bU != null AND world.getBlockId(bU.a, bU.b, bU.c) == 26 (bed still exists):
   a. Clear occupied flag: aab.a(world, bU.a, bU.b, bU.c, false)
   b. var5 = aab.f(world, bU.a, bU.b, bU.c, 0)   // find safe wake position
   c. If var5 == null: var5 = new dh(bU.a, bU.b + 1, bU.c)   // fallback: one block above
   d. Teleport: d(var5.a + 0.5F, var5.b + L + 0.1F, var5.c + 0.5F)

4. bT = false   // isSleeping = false

5. If server AND broadcastWake: world.A()   // check all-sleeping again

6. If setSpawn: a = 0
   Else: a = 100   // brief "waking up" penalty counter

7. If setSpawnpoint: this.a(bU)   // set spawn point = bed head position
```

---

## 15. Static — findWakeupPosition (`aab.f(ry, int, int, int, int offset)`)

Finds a safe position for the player to wake up near the bed. Searches a 3×3 area in two
attempts (once centered on foot half, once centered on head half).

```
for var7 in [0, 1]:   // two search centers
    cx = x - a[facing][0] * var7 - 1
    cz = z - a[facing][1] * var7 - 1

    for x2 in [cx, cx+2]:
        for z2 in [cz, cz+2]:
            if world.g(x2, y-1, z2):    // solid floor (opaque cube below)
               AND world.h(x2, y, z2)   // air at y (not solid)
               AND world.h(x2, y+1, z2): // air at y+1
                if offset <= 0: return new dh(x2, y, z2)
                else: offset--   // skip this many valid candidates

return null
```

Called with `offset=0` → returns the first valid position found.

---

## 16. qy Enum — Sleep Result

| Constant | Meaning | Message shown |
|---|---|---|
| `qy.a` | OK — sleeping started | (none) |
| `qy.b` | Wrong dimension (Nether/End) | (none — trySleep not reached, prevented by WorldProvider.c) |
| `qy.c` | Not night time | "tile.bed.noSleep" |
| `qy.d` | Too far from bed | (none) |
| `qy.e` | Already sleeping / dead | (none) |
| `qy.f` | Monsters nearby | "tile.bed.notSafe" |

**Note:** `qy.b` is returned from `trySleep` when `world.y.c == true`. But in the Nether, `onBlockActivated` triggers an explosion BEFORE calling `trySleep`, so `qy.b` would only be reached if the bed were somehow activated in a dimension without a sky that also has `WorldProvider.c = true`.

---

## 17. isDayTime — `ry.l()`

```
return this.k < 4
```

Where `this.k` = sky darkening value (0 = full daylight, increases at dusk). Sleep is only
possible when `l()` is false, i.e., `k >= 4` (dark enough).

---

## 18. Tick Behaviour

BlockBed has no random tick and no scheduled tick. It is entirely event-driven:
- Activated by player right-click → `onBlockActivated`
- Updated by neighbor changes → `onNeighborBlockChange`

Player sleep state is handled entirely in `EntityPlayer` (`vi`).

---

## 19. Known Quirks / Bugs to Preserve

1. **Double-block explosion**: When a bed explodes in the Nether, it removes the head block first, then checks if the foot block is still a bed and removes that too, computing the midpoint between the two halves as the explosion center. This means a complete two-block bed explodes from its center, not its head position.
2. **Occupied flag is set on HEAD only**: `setOccupied` always writes to the head position. The foot half's metadata does not have bit 2 meaningful.
3. **Foot half drops item, not head half**: Even if you mine the head first (which triggers foot removal via `onNeighborBlockChange`), the dropped item still comes from the foot's own `getItemDropped`. Head half always returns item 0.
4. **Stale occupied flag cleared on re-entry**: If a player was sleeping and disconnected, bit 2 may remain set. `onBlockActivated` detects this by scanning the player list — if no matching sleeping player is found, it clears the flag and proceeds normally.
5. **Player is shrunk during sleep**: Size set to (0.2 × 0.2) and L = 0.2 while sleeping. This is restored in `wakeUpPlayer`. The tiny size means sleeping players don't block other players from walking through them.
6. **wake counter `a = 100`**: On normal wake-up (non-setSpawn path), sleep counter is set to 100 rather than 0. The purpose of this counter and its downstream effects are not fully resolved — see Open Questions.

---

## 20. Open Questions

1. **`dh` class**: Used as `new dh(x, y, z)` with fields `.a`, `.b`, `.c` = x, y, z integer coordinates. Likely `ChunkPosition` or a dedicated bed-position triple — confirm class name and whether it is identical to `am` (BlockPos from Explosion spec).
2. **`world.y.c` vs `!world.y.d()`**: Two different checks for non-overworld dimensions in the same method flow. Confirm which WorldProvider subclass sets `c = true` (Nether? End? Both?) and which overrides `d()` to return false.
3. **`world.l()` exact trigger point**: `this.k` is the sky darkening integer. Confirm the exact value of `k` at dusk/dawn and what world time values correspond to `k >= 4` (sleeping allowed).
4. **Sleep counter `a`**: After wake-up, `a` is set to either 0 or 100. Confirm whether `a` is the `sleepTimer` and what behaviour the value 100 triggers (prevent immediate re-sleep? starvation delay?).
5. **`world.A()`**: Called on sleep start and wake-up. Confirm it is `checkAllPlayersSleeping()` and what it does when all players are sleeping (presumably: advance world time to dawn, fire thunder/rain reset, wake all players).
6. **Bed placement prerequisites**: The spec covers the bed block itself; it does not cover `ItemBlock.onItemUse` for placing the bed. Confirm that placing a bed requires two adjacent air blocks and that the foot→head orientation is set at placement time based on player yaw.
