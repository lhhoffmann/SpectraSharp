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

# BlockPiston / BlockPistonExtension / BlockMovingPiston / TileEntityPiston Spec
Source classes: `abr.java` (BlockPiston), `acu.java` (BlockPistonExtension),
`qz.java` (BlockMovingPiston), `agb.java` (TileEntityPiston), `ot.java` (DirectionData)
Superclasses: `abr` extends `yy` (Block); `acu` extends `yy` (Block); `qz` extends `ba` (BlockContainer); `agb` extends `bq` (TileEntity)

---

## 1. Purpose

The piston system is a four-block cooperative mechanism:

- `abr` (BlockPiston, IDs 29/33): the base block that reacts to redstone and triggers push/pull.
- `acu` (BlockPistonExtension, ID 34): the arm block placed in front of the piston when extended. Visible as the piston head.
- `qz` (BlockMovingPiston, ID 36): an invisible proxy block placed during animation. Blocks being moved are temporarily replaced with this block while the TileEntity animates them.
- `agb` (TileEntityPiston): the tile entity attached to BlockMovingPiston; stores the original block, progress, and facing.

---

## 2. Block IDs and Registry Fields

| yy field | ID | Class | Description |
|---|---|---|---|
| `yy.V` | 29 | `abr(29, 106, true)` | Sticky piston |
| `yy.Z` | 33 | `abr(33, 107, false)` | Normal piston |
| `yy.aa` | 34 | `acu(34, 107)` | Piston arm (BlockPistonExtension) |
| `yy.ac` | 36 | `qz(36)` | Moving piston block |

---

## 3. Direction Data (ot)

`ot` is a static-only utility class holding face direction arrays. Used by abr, acu, agb throughout.

```
ot.a = { 1, 0, 3, 2, 5, 4 }    opposite face: a[face] = face on opposite side
ot.b = { 0, 0, 0, 0,-1, 1 }    X delta per face
ot.c = {-1, 1, 0, 0, 0, 0 }    Y delta per face
ot.d = { 0, 0,-1, 1, 0, 0 }    Z delta per face
```

Face → direction mapping (derived from ot.b/c/d):

| Face | X delta | Y delta | Z delta | Direction |
|------|---------|---------|---------|-----------|
| 0 | 0 | -1 | 0 | Down (-Y) |
| 1 | 0 | +1 | 0 | Up (+Y) |
| 2 | 0 | 0 | -1 | North (-Z) |
| 3 | 0 | 0 | +1 | South (+Z) |
| 4 | -1 | 0 | 0 | West (-X) |
| 5 | +1 | 0 | 0 | East (+X) |

---

## 4. Fields

### abr (BlockPiston)

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | boolean | (ctor) | isSticky — true for ID 29, false for ID 33 |
| `cb` | static boolean | false | Anti-reentrance guard — prevents recursive triggering during push/retract |

### acu (BlockPistonExtension)

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | int | -1 | Custom texture index override for front face; -1 = use default |

### agb (TileEntityPiston)

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | int | 0 | Stored block ID (the block being moved or the piston arm type) |
| `b` | int | 0 | Stored block metadata |
| `j` | int | 0 | Facing direction (0-5, same encoding as piston base) |
| `k` | boolean | false | isExtending: true = moving outward; false = retracting |
| `l` | boolean | false | isSource: true = this is the piston arm itself; false = a pushed block |
| `m` | float | 0.0F | Target progress (advances 0.5F per tick; 0=start, 1=done) |
| `n` | float | 0.0F | Previous-frame progress (for render interpolation) |
| `o` | static List | empty | Shared entity-push list (class-level, not instance) |

---

## 5. Constants & Magic Numbers

| Constant | Value | Meaning |
|---|---|---|
| Push limit | 13 | Maximum blocks a piston can push in a chain |
| Animation speed | +0.5F per tick | Progress increments per server tick |
| Animation finalize | progress >= 1.0F | When to commit the move |
| Entity push velocity | 0.25F | Velocity applied to entities in the path when extending |
| Extend sound | `"tile.piston.out"` | Played at volume 0.5F, pitch random [0.6, 0.85] |
| Retract sound | `"tile.piston.in"` | Played at volume 0.5F, pitch random [0.6, 0.75] |
| Piston hardness | 0.5F | Mining time multiplier |
| Moving block hardness | -1.0F | Cannot be mined while animating |
| Item drop (piston base) | ID 106 (sticky) / ID 107 (normal) | `s()` method returns these item IDs |

---

## 6. Meta Layouts

### abr (BlockPiston) and acu (BlockPistonExtension) meta layout

```
Bits [2..0]: facing direction (0-5, see §3)
Bit  [3]:    isExtended (abr) / isSticky arm (acu)
             abr: 0 = retracted, 1 = extended
             acu: 0 = normal arm texture, 1 = sticky arm texture (bL-1)
```

Extraction helpers:
- `abr.e(meta)` = `meta & 7` → facing (bits 0-2)
- `abr.f(meta)` = `(meta & 8) != 0` → isExtended bit
- `acu.f(meta)` = `meta & 7` → facing (bits 0-2)

---

## 7. Methods — Detailed Logic

### 7.1 abr.determineFacing (obf: static `c(ry, int, int, int, EntityPlayer)`)

**Called by:** placement handler to compute which way the piston should face.
**Returns:** facing index (0-5)

Step-by-step:
1. If `|player.X - blockX| < 2.0F` AND `|player.Z - blockZ| < 2.0F`:
   - `playerEyeY = player.Y + 1.82 - player.eyeHeight` (eye level in world)
   - If `playerEyeY - blockY > 2.0` → return 1 (facing up: arm points up)
   - If `blockY - playerEyeY > 0.0` → return 0 (facing down: arm points down)
2. Compute yaw quadrant: `quadrant = floor(player.yaw * 4.0 / 360.0 + 0.5) & 3`
   - quadrant 0 → return 2 (arm points north, -Z)
   - quadrant 1 → return 5 (arm points east, +X)
   - quadrant 2 → return 3 (arm points south, +Z)
   - quadrant 3 → return 4 (arm points west, -X)
   - else → return 0

### 7.2 abr.onBlockPlaced (obf: `a(ry, x, y, z, Entity, ...)`)

Server-side only (not reentrant):
1. Compute `facing = c(world, x, y, z, player)`
2. `world.f(x, y, z, facing)` — set block metadata to facing (isExtended=0)
3. Call `g(world, x, y, z)` to check redstone and maybe extend immediately

### 7.3 abr.onNeighborBlockChange / onBlockAdded (obf: `a(ry, x, y, z, int)` and `a(ry, x, y, z)`)

Both call `g(world, x, y, z)` if server and not reentrant (`!cb`).

### 7.4 abr.checkAndTrigger (obf: `g(ry, x, y, z)`)

**Called by:** all placement/neighbor-change hooks.
**Parameters:** world, x, y, z

1. Read `meta = world.d(x, y, z)`, `facing = e(meta)`, `isExtended = f(meta)`
2. If `meta == 7` → return (guard against invalid facing)
3. Compute `isPowered = f(world, x, y, z, facing)` (see §7.5)
4. If `isPowered && !isExtended`:
   - If `g(world, x, y, z, facing)` (canPush check) → call `a(world, x, y, z, 0, facing)` (trigger extend)
   - Else just set meta: `world.c(x, y, z, facing | 8)` (mark as extended without physical push — cosmetic)
5. If `!isPowered && isExtended`:
   - `world.c(x, y, z, facing)` — clear extended bit
   - Call `a(world, x, y, z, 1, facing)` — trigger retract

### 7.5 abr.isPowered (obf: `f(ry, x, y, z, facing)`)

Returns true if any of the following provides power (OR logic):

Checked positions (using `world.l(nx, ny, nz, faceIndex)` = isProvidingPower):
1. If facing ≠ 0: check block at (x, y-1, z) face 0 (below)
2. If facing ≠ 1: check block at (x, y+1, z) face 1 (above)
3. If facing ≠ 2: check block at (x, y, z-1) face 2 (north)
4. If facing ≠ 3: check block at (x, y, z+1) face 3 (south)
5. If facing ≠ 5: check block at (x+1, y, z) face 5 (east)
6. If facing ≠ 4: check block at (x-1, y, z) face 4 (west)
7. Check block at (x, y, z) face 0 (redstone directly on top?)
8. Check block at (x, y+2, z) face 1 (two above)
9. Check block at (x, y+1, z-1) face 2
10. Check block at (x, y+1, z+1) face 3
11. Check block at (x-1, y+1, z) face 4
12. Check block at (x+1, y+1, z) face 5

### 7.6 abr.canPush (obf: static `g(ry, x, y, z, facing)`)

Walks from `(x + ot.b[facing], y + ot.c[facing], z + ot.d[facing])` forward (up to 13 times):

Loop:
- If Y <= 0 or Y >= worldHeight - 1 → return false
- Get `blockId = world.a(nx, ny, nz)`
- If blockId == 0 → space found, break loop (success path)
- If `!a(blockId, world, nx, ny, nz, true)` (not pushable) → return false
- If `yy.k[blockId].i() == 1` (liquid) → remove liquid block (`world.b(nx,ny,nz,0,meta)`), break loop (success path — liquids are displaced)
- Else: step forward, increment counter
- If counter == 12 (walked 13 blocks without finding space) → return false

Return true (space found within 13-block chain).

### 7.7 abr.pushability check (obf: static `a(blockId, ry, x, y, z, checkSelf)`)

Returns true if the block can be pushed.

Rules (check in order; any false returns NOT pushable):
1. If `blockId == yy.ap.bM` (bedrock) → NOT pushable
2. If `blockId == yy.Z.bM` (normal piston) OR `blockId == yy.V.bM` (sticky piston):
   - If `f(world.d(x,y,z))` = isExtended bit set → NOT pushable (extended pistons cannot be pushed)
   - Else: pushable
3. If `yy.k[blockId].n() == -1.0F` (hardness = -1, unbreakable e.g. bedrock/obsidian) → NOT pushable
4. If `yy.k[blockId].i() == 2` → NOT pushable (some non-pushable type)
5. If `checkSelf == false` AND `yy.k[blockId].i() == 1` → NOT pushable (liquids not in main chain)
6. If `yy.k[blockId] instanceof ba` (BlockContainer: chest, furnace, dispenser, jukebox, etc.) → NOT pushable
7. Else → pushable

### 7.8 abr.doExtend (obf: `h(ry, x, y, z, facing)`)

Performs the actual block-moving sequence for extension. Called by `a(world, x, y, z, 0, facing)`.

Sets `cb = true` at start, `cb = false` at end.

Phase 1 — find endpoint:
Walk from `(x + dx, y + dy, z + dz)` forward, same loop as `g()`.
When liquid is encountered: remove it (`world.b(nx,ny,nz,0,meta)`). When air is found: break.

Phase 2 — slide blocks forward (backward pass from endpoint to origin):
Walk backward from found endpoint toward (x, y, z):
- `prev = (current - delta)`
- Get `prevBlockId = world.a(prev)`, `prevMeta = world.d(prev)`
- If prev IS the piston base itself:
  - Place `yy.ac` (moving block) at `current` with meta = `facing | (isSticky ? 8 : 0)`
  - Create TileEntityPiston at current: `qz.a(yy.aa.bM, facing|(isSticky?8:0), facing, true, false)` (stores piston arm block)
- Else:
  - Place `yy.ac` at current with `prevMeta`
  - Create TileEntityPiston at current: `qz.a(prevBlockId, prevMeta, facing, true, false)` (stores pushed block)
- Set `current = prev` (step backward)

When done, the whole chain has been shifted forward by one block, with each original block replaced by `yy.ac` and an `agb` TE recording what to finalize.

### 7.9 abr.onBlockActivated — extend/retract dispatch (obf: `a(ry, x, y, z, int type, int facing)`)

**type == 0 (extend):**
Sets `cb = true`.
- If `h(world, x, y, z, facing)` succeeds:
  - `world.c(x, y, z, facing | 8)` (set extended bit in base piston)
  - `world.f(x, y, z, 0, facing)` — notify neighbors (custom call with 2 extra args?)
  - Play extend sound
- Else: `world.c(x, y, z, facing)` (no extended bit — failed push)
Sets `cb = false`.

**type == 1 (retract):**
Sets `cb = true`.
1. Check one block ahead (arm position): `(x+dx, y+dy, z+dz)`
   - Get TileEntity at arm position. If it is an `agb`, call `.j()` on it (instant-finalize the arm animation).
2. Place `yy.ac` at piston base position with meta = `facing`
3. Create TileEntityPiston: `qz.a(bM, facing, facing, false, true)` (stores piston block ID, isRetract=true)
4. If sticky piston (`this.a == true`):
   - Check position 2 blocks ahead: `(x + 2*dx, y + 2*dy, z + 2*dz)`
   - Get block there (`blockId2`, `meta2`):
     - If it is `yy.ac` (moving block): get its agb TE; if `agb.d() == facing` (same direction) and `agb.c() == true` (extending): call `agb.j()` (finalize); use `agb.a()` as blockId2 and `agb.f()` as meta2 to retrieve the block it was carrying.
   - If `blockId2 > 0` AND `a(blockId2, world, nx, ny, nz, false)` (pushable) AND NOT (blockId2 == lava/water AND isExtended):
     - Place `yy.ac` at arm position with stored meta2
     - Create TileEntityPiston at arm position: `qz.a(blockId2, meta2, facing, false, false)` (isRetract)
     - Remove the block at 2-blocks-ahead position: `world.g(nx2, ny2, nz2, 0)`
   - Else: remove arm position block: `world.g(arm_x, arm_y, arm_z, 0)`
5. If not sticky: remove arm block: `world.g(arm_x, arm_y, arm_z, 0)`
6. Play retract sound.
Sets `cb = false`.

### 7.10 abr.getBlockTexture (obf: `a(int face, int meta)`)

- `facing = e(meta)` (bits 0-2)
- If `facing > 5` → return `bL` (base texture — should not happen)
- If `face == facing` (looking at the piston front/output face):
  - If NOT extended (`!f(meta)`) AND AABB is exactly 0,0,0,1,1,1 (not partially visible through animation) → return `bL` (standard push-face texture)
  - Else → return 110 (animated/extended front texture)
- If `face == ot.a[facing]` (back face, opposite side) → return 109 (piston back texture)
- All other faces → return 108 (piston side texture)

AABB detection: `!(bR > 0.0) && !(bS > 0.0) && !(bT > 0.0) && !(bU < 1.0) && !(bV < 1.0) && !(bW < 1.0)` — checking all 6 AABB components equal default (full block). During animation the AABB is modified, making this condition false.

---

### 7.11 acu.getBlockTexture (obf: `a(int face, int meta)`)

- `facing = f(meta)` (= `meta & 7`)
- If `face == facing` (front of arm):
  - If `a >= 0` (custom override) → return `a`
  - If `(meta & 8) != 0` (isSticky bit) → return `bL - 1` (= 106 = sticky front texture)
  - Else → return `bL` (= 107 = normal arm front texture)
- If `face == ot.a[facing]` (back of arm, pointing into piston) → return 107
- All other faces → return 108

### 7.12 acu.getCollisionBoxes (obf: `a(ry, x, y, z, c clipBox, ArrayList list)`)

Returns two AABB parts per facing direction. Pair = (face plate) + (shaft):

| Facing | Face plate AABB | Shaft AABB |
|--------|-----------------|------------|
| 0 (down) | (0,0,0)–(1,0.25,1) | (0.375,0.25,0.375)–(0.625,1,0.625) |
| 1 (up) | (0,0.75,0)–(1,1,1) | (0.375,0,0.375)–(0.625,0.75,0.625) |
| 2 (north) | (0,0,0)–(1,1,0.25) | (0.25,0.375,0.25)–(0.75,0.625,1) |
| 3 (south) | (0,0,0.75)–(1,1,1) | (0.25,0.375,0)–(0.75,0.625,0.75) |
| 4 (west) | (0,0,0)–(0.25,1,1) | (0.375,0.25,0.25)–(0.625,0.75,1) |
| 5 (east) | (0.75,0,0)–(1,1,1) | (0,0.375,0.25)–(0.75,0.625,0.75) |

After iterating both boxes, always resets AABB to full block (0,0,0)–(1,1,1).

### 7.13 acu.getPickBox (obf: `b(kq, x, y, z)`)

Returns only the face plate part (first AABB) per facing direction.

### 7.14 acu.onBlockRemoved (obf: `d(ry, x, y, z)`)

Called when the arm block is broken externally.

1. `facing = f(world.d(x,y,z))`
2. Compute base piston position: `(x - ot.b[facing], y - ot.c[facing], z - ot.d[facing])`
3. Get block at base position.
4. If block is `yy.Z` or `yy.V` AND that piston has `f(meta) == true` (extended bit): retract it via `yy.k[baseId].b(world, bx, by, bz, meta, 0)` and remove block.

### 7.15 acu.onNeighborBlockChange (obf: `a(ry, x, y, z, int neighborId)`)

1. `facing = f(world.d(x,y,z))`
2. Check block at base position `(x - dx, y - dy, z - dz)`.
3. If it is NOT piston base (Z or V) → `world.g(x, y, z, 0)` (remove arm — orphaned).
4. If it IS a piston base → forward the neighbor-change call to the base piston.

---

### 7.16 qz.dropBlockAsItem (obf: `a(ry, x, y, z, int meta, float chance, int fortune)`)

Server-side only. Gets TileEntityPiston (`agb`) from the world at this position.
If found: drops the stored block item via `yy.k[agb.a()].b(world, x, y, z, agb.f(), 0)`.

### 7.17 qz.getCollisionBox (obf: `b(ry, x, y, z)`)

Gets the agb TE. If null → return null.
Computes `progress = agb.a(0.0F)` (current render progress).
If extending: actual progress = 1.0F - progress.
Delegates to `qz.b(world, x, y, z, agb.a(), progress, agb.d())` — the static version that builds the AABB.

The static `b(ry, x, y, z, blockId, progress, facing)`:
- Gets the base AABB for `blockId` from `yy.k[blockId].b(world, x, y, z)`.
- Offsets all 6 AABB planes by `-(ot.b/c/d[facing] * progress)`.

---

### 7.18 agb.tick (obf: `b()`)

Called every server tick for each loaded moving-piston TE.

1. `this.n = this.m` (save previous progress for interpolation)
2. If `this.n >= 1.0F` (already done):
   - Call `this.a(1.0F, 0.25F)` — push entities using progress=1.0 and velocity=0.25
   - Call `world.o(x, y, z)` — remove TileEntity from world
   - Call `this.l()` (mark dirty / notify)
   - If block at (x,y,z) is still `yy.ac` → `world.d(x,y,z, storedBlockId, storedMeta)` (finalize block)
3. Else:
   - `this.m += 0.5F`; clamp to 1.0F
   - If extending (`this.k == true`): call `this.a(this.m, this.m - this.n + 0.0625F)` — push entities

### 7.19 agb.instantFinalize (obf: `j()`)

Immediately finalizes the animation (for external interruption — e.g. when the block is removed).

1. If `this.n >= 1.0F` OR world is null → return (already done)
2. Set `this.n = this.m = 1.0F`
3. `world.o(x, y, z)` — remove TE
4. `this.l()` (mark dirty)
5. If block at (x,y,z) is `yy.ac` → replace with stored block: `world.d(x,y,z, a, b)`

### 7.20 agb.entityPush (private `a(float progress, float velocity)`)

Computes effective progress:
- If NOT extending (`!k`): `effectiveProgress = progress - 1.0F` (negative, approaching 0)
- If extending: `effectiveProgress = 1.0F - progress`

Gets AABB from `qz.b(world, x, y, z, f, storedBlockId, effectiveProgress, j)`.
Finds all entities within that AABB: `world.b(null, aabb)`.
For each entity: `entity.b(dx, dy, dz)` where `dx = velocity * ot.b[j]`, `dy = velocity * ot.c[j]`, `dz = velocity * ot.d[j]`.

### 7.21 agb NBT (readFromNBT / writeToNBT)

Read: `a = nbt.e("blockId")`, `b = nbt.e("blockData")`, `j = nbt.e("facing")`, `n = m = nbt.g("progress")`, `k = nbt.m("extending")`
Write: `nbt.a("blockId", a)`, `nbt.a("blockData", b)`, `nbt.a("facing", j)`, `nbt.a("progress", n)`, `nbt.a("extending", k)`

Note: field `l` (isSource/arm) is NOT saved to NBT.

### 7.22 qz.a (static factory — creates agb TileEntity)

`public static bq a(int blockId, int blockMeta, int facing, boolean isExtending, boolean isSource)`

Returns `new agb(blockId, blockMeta, facing, isExtending, isSource)`.
Used by abr extension/retraction code to construct the TE for the moving block.

---

## 8. Bitwise & Data Layouts

### abr/acu metadata

```
Bit 3 (value 8):
  abr: isExtended flag  (0=retracted, 1=extended)
  acu: isSticky arm flag (0=normal arm texture, 1=sticky arm texture)
Bits 2-0 (value 0-7):
  both: facing direction (0-5; values 6-7 not used normally)
```

---

## 9. Tick Behaviour

**agb (TileEntityPiston):**
- Ticked every server tick via `b()`.
- Animation completes in 2 ticks (0→0.5→1.0).
- Entities in the path are pushed on each extending tick.
- At completion, the proxy block (qz/ID 36) is replaced with the stored block.

**abr (BlockPiston):**
- Not ticked — purely reactive to neighbor changes.
- `g()` re-evaluates power state on every neighbor change.

---

## 10. Known Quirks / Bugs to Preserve

### 10.1 Anti-reentrance via static `cb` field

`cb` is a static boolean on `abr`. While any piston is doing a push/retract, all other pistons are suppressed from responding to neighbor changes. This prevents the chain reaction from causing immediate re-evaluation of neighboring pistons. The Coder must replicate this class-level static guard.

### 10.2 Animation progress in agb is NOT saved correctly

NBT writes `n` (previous-frame progress) as "progress", not `m` (current-frame progress). On reload, both `m` and `n` are set to the saved `n` value. In practice the animation rarely persists across saves (usually completes in 2 ticks), but if it does, the animation resumes from the previous-frame value.

### 10.3 Entity push uses static shared list `o`

The entity push in `agb.a()` uses `static List o` — a class-level ArrayList. Entities are accumulated into it, pushed, then cleared. This means if a push is somehow re-entered, entities could be double-pushed (similar to the torch burnout bug). The Coder must use a class-level list, not a local one.

### 10.4 Extended piston cannot be pushed

The pushability check for piston blocks only prevents pushing if the piston is extended (bit 8 in meta is set). A retracted piston CAN be pushed by another piston. This is intentional.

### 10.5 Sticky piston pulls through moving blocks

During retraction, if the block 2 positions ahead is a `yy.ac` (moving block) AND its TileEntity has the right facing and is extending, the sticky piston FINALIZES that moving block and then pulls the stored block. This allows for chain-pulling when two pistons face each other and retract simultaneously.

### 10.6 acu defers neighbor trigger to base piston

When a neighbor of the arm changes, `acu.onNeighborBlockChange` routes the event to the base piston rather than evaluating power itself. This means the base piston is the sole power-evaluation node.

---

## 11. Open Questions

### 11.1 world.c() vs world.f() for meta updates

`abr` uses `world.c(x,y,z, meta)` in some places (no neighbor notify) and `world.f(x,y,z, meta)` in others (with notify). The exact difference is in World.java which is not fully analyzed here. `c` appears to be "set meta silently" and `f` is "set meta and notify neighbors."

### 11.2 yy.k[blockId].i() semantics

`i()` is called to check block type. `i() == 1` appears to mean liquid (water/lava). `i() == 2` appears to block pushing (portal, possibly). The exact semantics of `i()` need to be confirmed from Block.java.

### 11.3 world.f() with extra arguments in extend dispatch

In `a(world, x, y, z, 0, facing)` (extend type), the code calls `world.f(x, y, z, 0, facing)` — a 5-argument version of f(). This extra argument may mean "notify with face 0" or "suppress certain neighbor types." The Coder should match the World.java implementation.

### 11.4 Block texture indices 107, 108, 109, 110

These texture atlas indices appear hardcoded in abr/acu. Their exact atlas positions need to be confirmed against the terrain atlas layout for the Nether Brick/Piston textures.
