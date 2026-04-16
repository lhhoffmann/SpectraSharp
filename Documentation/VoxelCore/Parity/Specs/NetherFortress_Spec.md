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

# NetherFortress Spec
Source classes: `ed.java` (MapGenNetherBridge), `tg.java` (NetherFortressStart), `gc.java` (StartingPiece),
`rp.java` (PieceRegistry), `aaz.java` (PieceEntry), `rh.java` (FortressPiece base),
`ld.java` (DeadEnd), `ac.java`, `bw.java`, `ui.java`, `bl.java`, `kf.java`, `xr.java`,
`hg.java`, `yj.java`, `lu.java`, `ahw.java`, `tr.java`, `acs.java`, `io.java`
Mob classes: `qf.java` (Blaze), `jm.java` (ZombiePigman), `aea.java` (MagmaCube)
Superclasses: `ed` extends `hl` (MapGenStructureBase); piece classes extend `rh` extends `nk` (StructurePiece)

---

## 1. Purpose

The Nether Fortress (`MapGenNetherBridge`) generates multi-piece castle structures in the Nether dimension.
Each candidate chunk column has a 1-in-3 chance of containing a fortress. The fortress grows via a recursive
piece-expansion system: one starting piece spawns exits which can attach corridors or rooms from two weighted
piece pools. The total structure is constrained to a 224×224 block footprint (±112 from origin). Fortresses
contain Blaze spawners, a Nether Wart farm room, and decorative nether brick architecture placed between
Y=48 and Y=70.

---

## 2. Block Palette

All fortress pieces use exclusively these block IDs:

| Block | yy field | ID |
|---|---|---|
| Nether Brick | `yy.bA` | 112 |
| Nether Brick Fence | `yy.bB` | 113 |
| Nether Brick Stairs | `yy.bC` | 114 |
| Soul Sand | `yy.bc` | 88 |
| Nether Wart | `yy.bD` | 115 |
| MobSpawner | `yy.as` | 52 |
| Lava (flowing) | `yy.C` | 10 |
| Air (clear) | — | 0 |

Nether Brick (ID 112) is the primary fill block. Air (0) is used to hollow out interior spaces.

---

## 3. Piece System Architecture

### 3.1 nk (StructurePiece) — base for all pieces

Key fields (inherited by all piece subclasses):
- `e` (nl) — axis-aligned bounding box in world coordinates
- `f` (int, 0-3) — orientation (rotation); 0=south, 1=west, 2=north, 3=east
- `g` (int) — generation depth counter

### 3.2 rh (FortressPiece) — base for all Nether Fortress pieces

Adds the exit-spawning helpers:
- `a(gc start, List pieces, Random rng, int offsetForward, int offsetSide, boolean useRoomList)` — spawns a new piece forward from this piece
- `b(gc, List, Random, offsetSide, offsetForward, useRoomList)` — spawns a new piece to the left
- `c(gc, List, Random, offsetSide, offsetForward, useRoomList)` — spawns a new piece to the right

Radius guard (in internal `a()` method):
- If `|candidateX - start.origin.X| > 112` OR `|candidateZ - start.origin.Z| > 112` → place terminator `ld` instead.

Depth guard:
- If depth > 30 → place terminator `ld` instead of a normal piece.

Piece selection from weighted list (up to 5 attempts per exit):
- Sum total weight of pieces not yet at max count.
- If total <= 0 (all pieces at max) → place terminator `ld`.
- Pick random weighted entry; if depth ok and not the same consecutive type without `isTerminator` → create that piece.

### 3.3 aaz (PieceEntry) — piece metadata

Fields:
- `a` (Class) — the piece class
- `b` (int) — weight (higher = more common)
- `c` (int) — current count (reset to 0 at structure start)
- `d` (int) — max allowed count (0 = unlimited)
- `e` (boolean) — isTerminator: if true, this piece may be chosen even if it is the same type as the last placed piece

### 3.4 rp (PieceRegistry) — two static piece lists

**Corridor list (rp.a()) — used for main bridge extensions:**

| Class | Human name | Weight | Max | Terminator | Bounding box (local WxHxD) | Exits |
|---|---|---|---|---|---|---|
| `ac` | BridgeStraight | 30 | ∞ | yes | 5×10×19 | 1 forward |
| `bw` | BridgeCrossing | 10 | 4 | no | 19×10×19 | forward + left + right |
| `ui` | BridgeCrossing3 | 10 | 4 | no | 7×9×7 | forward + left + right |
| `bl` | BridgeStaircase | 10 | 3 | no | 7×11×7 | 1 right |
| `kf` | BlazeSpawnerCorridor | 5 | 2 | no | 7×8×9 | none (terminal) |
| `xr` | FortressRoom | 5 | 1 | no | 13×14×13 | 1 forward (via room list) |

**Room list (rp.b()) — used for interior room expansions:**

| Class | Human name | Weight | Max | Terminator | Bounding box (local WxHxD) | Exits |
|---|---|---|---|---|---|---|
| `hg` | RoomCrossing | 25 | ∞ | yes | 5×7×5 | 1 forward (room list) |
| `yj` | RoomCrossing3 | 15 | 5 | no | 5×7×5 | forward + left + right (room list) |
| `lu` | RoomCrossingRight | 5 | 10 | no | 5×7×5 | 1 right (room list) |
| `ahw` | RoomCrossingLeft | 5 | 10 | no | 5×7×5 | 1 left (room list) |
| `tr` | StaircaseDown | 10 | 3 | yes | 5×14×10 | 1 forward (room list) |
| `acs` | CorridorRoofed | 7 | 2 | no | 9×7×9 | left + right (room list) |
| `io` | NetherWartRoom | 5 | 2 | no | 13×14×13 | 2 exits forward (room list) |

### 3.5 ld (DeadEnd) — fallback terminator

Used when: depth > 30, radius > 112, or all weighted pieces at max count.
- Bounding box: 5×10×8 local
- No exits
- Block layout: random-length nether brick columns using a deterministic `Random` seeded from the piece's own saved seed field (`this.a`).
  - The random seed is captured at piece creation via `var2.nextInt()`.
  - During generation, a fresh `new Random(this.a)` is used — so the dead end looks the same regardless of world RNG state at generation time.

---

## 4. Placement Algorithm (ed — MapGenNetherBridge)

### 4.1 shouldGenerateHere (obf: `a(int blockX, int blockZ)`)

Called per (blockX, blockZ) pair during chunk population in the Nether.

Step-by-step:
1. `chunkX = blockX >> 4`
2. `chunkZ = blockZ >> 4`
3. Seed the structure RNG `this.b` (a `Random`):
   `this.b.setSeed((long)(chunkX ^ (chunkZ << 4)) ^ this.c.t())`
   where `this.c.t()` = world seed (long).
4. Call `this.b.nextInt()` once (consume and discard — for RNG advancement parity).
5. If `this.b.nextInt(3) != 0` → return false. (Probability: 2/3 no fortress here.)
6. Compute `expectedX = (chunkX << 4) + 4 + this.b.nextInt(8)` (offset 4-11 within chunk).
7. Compute `expectedZ = (chunkZ << 4) + 4 + this.b.nextInt(8)`.
8. Return `blockX == expectedX && blockZ == expectedZ`.

This means: each 16×16 chunk column has a 1/3 chance of containing a fortress, and if so,
the fortress origin within that chunk is at a deterministic random offset [4, 11] in both X and Z.

### 4.2 createStart (obf: `b(int chunkX, int chunkZ)`)

Returns `new tg(this.c, this.b, chunkX, chunkZ)` — creates the structure start object.

---

## 5. Structure Start (tg — NetherFortressStart)

Constructor:
1. Create `gc` (starting piece) at world position `(chunkX * 16 + 2, chunkZ * 16 + 2)`.
   - `gc` is a subclass of `bw` (BridgeCrossing) with the same 19×10×19 geometry.
   - Initial Y for bounding box: hardcoded 64.
2. Add `gc` to the piece list `this.a`.
3. Call `gc.a(gc, this.a, rng)` — `gc` places its exit stubs into the pending list `gc.d`.
4. While `gc.d` is not empty:
   - Pick a random index from `gc.d`.
   - Remove it and call `.a(gc, this.a, rng)` on it — this piece expands, possibly adding more to `gc.d`.
5. Call `this.c()` — recalculate overall bounding box from all piece bounding boxes.
6. Call `this.a(world, rng, 48, 70)` — iterate all pieces and call their `generate(world, rng, bounds)` method, restricting to Y=[48, 70].

### 5.1 gc (StartingPiece — extends bw)

`gc` copies both piece lists from `rp` on construction, resetting all `c` (count) fields to 0:
- `gc.b` = fresh copy of corridor piece list (rp.a() entries, all counts = 0)
- `gc.c` = fresh copy of room piece list (rp.b() entries, all counts = 0)
- `gc.d` = pending exits list (ArrayList)
- `gc.a` = the last-chosen piece entry (used to prevent consecutive same-type placement)

The starting piece generates as BridgeCrossing (see §7.2).

---

## 6. Mob Spawn List (ed.b())

Returns a list of 3 `yx` (SpawnListEntry) entries:

| Entity class | Human name | Weight | Min group | Max group |
|---|---|---|---|---|
| `qf` | Blaze | 10 | 2 | 3 |
| `jm` | ZombiePigman | 10 | 4 | 4 |
| `aea` | MagmaCube | 3 | 4 | 4 |

These are used by `ChunkProviderHell.populate()` for fortress-region spawning.

---

## 7. Piece Descriptions

All coordinates below are **local** (piece-relative). The structural system rotates them to world space
using the orientation field `this.f` (0=south, 1=west, 2=north, 3=east). Block placement helpers:
- `this.a(world, bounds, x1, y1, z1, x2, y2, z2, fillId, outlineId, keepAir)` — fill box with outline ID on edges, fill ID in interior (if keepAir=false, replaces all; if true, skips air blocks)
- `this.a(world, blockId, metadata, localX, localY, localZ, bounds)` — place single block
- `this.b(world, blockId, metadata, localX, localY, localZ, bounds)` — place only if in world bounds (flood-fill foundation)

### 7.1 ac — BridgeStraight

**Registry:** Corridor list, weight 30, unlimited, isTerminator=true
**Bounding box offset:** `nl.a(x, y, z, -1, -3, 0, 5, 10, 19, orientation)`
**Exits:** 1 forward at (local Z+8, Y+3)

Layout (W=5, H=10, D=19):
- Floor: 5×1×19 Nether Brick at Y=3
- Outer walls: 1×10×19 Nether Brick left (X=0) and right (X=4), top/bottom sealed
- Interior clear: 3×5×19 air at X=1–3, Y=5–9
- Ceiling pillars at ends (Z=0–5 and Z=13–18): 5×2×6 Nether Brick at Y=0–2
- Floor pillar extensions: 5×1×3 at Y=0–1 at both short ends
- Fence rails along sides at intervals: Nether Brick Fence columns at Z=1, Z=4, Z=14, Z=17

### 7.2 bw — BridgeCrossing (also the StartingPiece geometry via gc)

**Registry:** Corridor list, weight 10, max 4
**Bounding box offset:** `nl.a(x, y, z, -8, -3, 0, 19, 10, 19, orientation)`
**Exits:** forward at (local Z+8, Y+3), left at (local X-1, Y+3), right at (local X+19, Y+3)

Layout (W=19, H=10, D=19):
- Central cross bridge: 5×2×19 nether brick at X=7–11, Y=3–4 (main forward bridge)
- Perpendicular bridge: 19×2×5 nether brick at Z=7–11, Y=3–4
- Interior clearance: 3×3×19 air at X=8–10 and 19×3×3 at Z=8–10
- Corner towers: 4 sets of diagonal nether brick walls at corners
- Floor pillar fill (foundation): fills nether brick below bridge sections at Y=-1, Y=-2, Y=-3 (via `b()` calls)
- Fence decorations at outer edges of bridges

### 7.3 ui — BridgeCrossing3

**Registry:** Corridor list, weight 10, max 4
**Bounding box offset:** `nl.a(x, y, z, -2, 0, 0, 7, 9, 7, orientation)`
**Exits:** forward (Y+0), left (Y+0), right (Y+0)

Layout (W=7, H=9, D=7):
- Floor: 7×2×7 Nether Brick at Y=0–1
- Wall frame: 1×5×7 Nether Brick columns on each face (closed corners)
- Interior: 5×6×5 air at X=1–5, Y=2–7
- Corner columns with Nether Brick Fence decorations at alternating heights
- Top cap: 7×1×7 Nether Brick at Y=8
- Foundation fill at Y=-1

### 7.4 bl — BridgeStaircase

**Registry:** Corridor list, weight 10, max 3
**Bounding box offset:** `nl.a(x, y, z, -2, 0, 0, 7, 11, 7, orientation)`
**Exits:** 1 right at (local X+6, Y+2)

Layout (W=7, H=11, D=7):
- Outer shell: 7×9×7 nether brick frame
- Interior hollowed
- Staircase: descending nether brick step pattern at Z=5 (X=1–4, Y varies 2–6)
- Fence decoration on east wall
- Top platform: 5×1×4 nether brick at Y=7
- Foundation fill at Y=-1

### 7.5 kf — BlazeSpawnerCorridor

**Registry:** Corridor list, weight 5, max 2, isTerminator=false
**Bounding box offset:** `nl.a(x, y, z, -2, 0, 0, 7, 8, 9, orientation)`
**Exits:** none

Layout (W=7, H=8, D=9):
- Floor: 7×2×8 nether brick at Y=0–1
- Wall clearing: full interior hollow
- Staircase-like rising wall on Z axis (Y=2–4 per Z level)
- Side walls with gaps at Y=2–4 (arched windows)
- Nether Brick Fence columns at staircase junctions
- Top arch cap with fences

**Blaze Spawner placement:**
- Position: local (X=5, Y_local=6, Z=3) — resolved to world coordinates
- Block placed: MobSpawner (ID 52, `yy.as.bM`)
- TileEntity (`ze` = TileEntityMobSpawner) set entity name "Blaze"
- Only placed once (boolean field `a` prevents re-generation on chunk reload)
- Placement guarded by `var3.b(worldX, worldY, worldZ)` — bounds check (must be within generation bounds)

### 7.6 xr — FortressRoom (Large Open)

**Registry:** Corridor list, weight 5, max 1
**Bounding box offset:** `nl.a(x, y, z, -5, -3, 0, 13, 14, 13, orientation)`
**Exits:** 1 forward using room list at (local Z+5, Y+3)

Layout (W=13, H=14, D=13):
- Outer shell: 13×2×13 nether brick floor at Y=3–4; ceiling frame
- Full clearance: 13×9×13 air interior
- Left/right walls: 2×8×13 nether brick
- Fence columns alternating at Z=1,3,5,7,9,11 on all 4 faces at Y=10–11
- Upper battlement: individual nether brick/fence blocks at Y=13 alternating
- Inner courtyard walls at Y=7: 1×2×5 fence towers at X=1,11
- Cross-floor supports: nether brick X-cross at Y=2 (subfloor)
- Foundation fill: nether brick at Y=-1 under center sections

**Lava pool:**
- 3×1×3 nether brick platform at local (5,5,5)–(7,5,7)
- Air column cleared at (6,1,6)–(6,4,6)
- Nether brick at (6,0,6) (lava pedestal)
- Flowing Lava (ID 10) placed at (6,5,6) — top of platform
- Placement: `world.f = true; BlockLava.onBlockAdded(world, x, y, z, rng); world.f = false`
  (The `world.f` flag allows lava flow to spread during generation)

### 7.7 hg — RoomCrossing (small, forward only)

**Registry:** Room list, weight 25, unlimited, isTerminator=true
**Bounding box offset:** `nl.a(x, y, z, -1, 0, 0, 5, 7, 5, orientation)`
**Exits:** 1 forward (room list) at (Y+1)

Layout (W=5, H=7, D=5):
- Floor: 5×2×5 nether brick at Y=0–1
- Wall frame on 2 sides (closed box except front/forward opening)
- Interior: 3×4×3 air
- Fence decorations at all 4 corners at Y=3, alternating Z=1 and Z=3
- Top cap: 5×1×5 nether brick at Y=6
- Foundation fill at Y=-1

### 7.8 yj — RoomCrossing3 (forward + left + right)

**Registry:** Room list, weight 15, max 5
**Bounding box offset:** `nl.a(x, y, z, -1, 0, 0, 5, 7, 5, orientation)`
**Exits:** forward, left, right (all room list at Y+1)

Layout (W=5, H=7, D=5):
- Floor: 5×2×5 nether brick
- All 4 walls open (no wall blocks except the minimal corner pillars)
- Top cap: 5×1×5 nether brick at Y=6
- Foundation fill at Y=-1

### 7.9 lu — RoomCrossing (right exit only)

**Registry:** Room list, weight 5, max 10
**Bounding box offset:** `nl.a(x, y, z, -1, 0, 0, 5, 7, 5, orientation)`
**Exits:** 1 right (room list, Y+0)

Layout (W=5, H=7, D=5):
- Floor: 5×2×5 nether brick
- Left wall closed: 1×5×5 nether brick on X=0 face
- Right, front, back: open
- Fence rails on left wall at Z=1, Z=3
- Right (open) side: pillars with fence at Z=1, Z=3
- Top cap: 5×1×5 nether brick at Y=6
- Foundation fill at Y=-1

### 7.10 ahw — RoomCrossing (left exit only)

**Registry:** Room list, weight 5, max 10
**Bounding box offset:** `nl.a(x, y, z, -1, 0, 0, 5, 7, 5, orientation)`
**Exits:** 1 left (room list, Y+0)

Layout (W=5, H=7, D=5):
- Mirror of `lu` — right wall closed, left open
- Fence rails on right wall at Z=1, Z=3
- Left (open) side: pillars with fence at Z=1, Z=3

### 7.11 tr — StaircaseDown

**Registry:** Room list, weight 10, max 3, isTerminator=true
**Bounding box offset:** `nl.a(x, y, z, -1, -7, 0, 5, 14, 10, orientation)`
**Exits:** 1 forward (room list) at far end

Layout (W=5, H=14, D=10) — staircase descending 7 blocks over 10 Z steps:

Per Z step (var5 = 0 to 9):
- `floorY = max(1, 7 - var5)` — rising floor profile (Y=7 at Z=0, Y=1 at Z=6+)
- `ceilY = min(max(floorY + 5, 14 - var5), 13)` — ceiling: at least 5 blocks clearance, capped at 13
- Floor row: 5×1×1 nether brick at (X=0–4, Y=floorY, Z=var5)
- Clear row: 3×(ceilY-floorY-1)×1 air at (X=1–3, Y=floorY+1 to ceilY-1, Z=var5)
- Nether Brick Stairs (ID 114) placed at X=1, X=2, X=3, Y=floorY+1, Z=var5 (for Z=0–6)
- Ceiling row: 5×1×1 nether brick at (X=0–4, Y=ceilY, Z=var5)
- Side walls at X=0 and X=4
- Every even Z step: Nether Brick Fence decorations at Y=floorY+2 and Y=floorY+3 on both walls
- Foundation at Y=-1 per column

### 7.12 acs — CorridorRoofed

**Registry:** Room list, weight 7, max 2
**Bounding box offset:** `nl.a(x, y, z, -3, 0, 0, 9, 7, 9, orientation)`
**Exits:** left (room list) and right (room list)

Left/right exit Y offset depends on orientation:
- If `this.f == 1 || this.f == 2`: side offset = 5
- Else: side offset = 1

With probability 7/8 for each exit (`rng.nextInt(8) > 0`), the exit uses the room list; otherwise corridor list.

Layout (W=9, H=7, D=9):
- Floor: 9×2×9 nether brick at Y=0–1
- Interior clear: 9×4×9 air at Y=2–5
- Roof: 9×1×6 nether brick (partial — closed toward Z=0 half)
- Front wall (Z=0): closed with 2 arched openings (clear at X=1–2 and X=6–7, Y=1–2)
- Back wall (Z=8): fence rail at Y=3
- Side pillars with fences
- Foundation fill at Y=-1

### 7.13 io — NetherWartRoom (Large Wart Farm)

**Registry:** Room list, weight 5, max 2
**Bounding box offset:** `nl.a(x, y, z, -5, -3, 0, 13, 14, 13, orientation)`
**Exits:** 2 exits forward (room list) at local Z+5, Y+3 for Z=3 and Z=11

Layout (W=13, H=14, D=13) — same shell as `xr` (large room):
- Outer shell: 13×2×13 nether brick floor, ceiling frame
- Full clearance interior
- Fence battlement on outer walls at Y=10–11 alternating
- Inner wall structure at Y=7
- Cross-floor supports at Y=2

**Nether Wart farm — gallery at local X=4 to X=10, Z=4 to Z=11:**
- 3 columns of wart (at X=5, X=6, X=7) per Z level
- For Z offset 0–6 (world Z = 4+offset):
  - Soul sand (ID 88) at Y=4 or Y=4+offset range depending on Z
  - Nether Wart (ID 115) at Y=5, at X=5, 6, 7 and corresponding Z
- Additional soul sand platform at Y=4 for X=3–4 and X=8–9

**Nether Wart placement detail (var5=0 to 6, var6 = var5+4):**
- Wart columns at (X=5 to 7, Y=5+var5, Z=var6): 3 blocks wide each Z level
- If var6 in [5,8]: nether brick cap at (X=5–7, Y=5, Z=var6) and (X=5–7, Y=var5+4, Z=var6)
- If var6 in [9,10]: nether brick cap at Y=8 level
- Clearance above wart columns (var5 >= 1): air cleared above to let wart grow

**Also wart column at top:** X=5–7, Y=12, Z=11 (3 wart blocks at the exit gallery)

**Soul sand + wart corners:**
- Uses metadata from `this.c(yy.bC.bM, N)` for stairs orientation
- Soul Sand rows at Y=4 on both sides (X=3–4, Z=2–3, 9–10 and X=8–9, same Z ranges)
- Nether Wart at Y=5 above those soul sand positions
- Additional soul sand strips: X=3–4, Z=4–8 and X=8–9, Z=4–8

---

## 8. Mob Classes

### 8.1 qf (Blaze)

- Texture: `/mob/fire.png`
- `af = true` (fire immune)
- Health: `a = 6` → maxHealth derived from parent
- `aX = 10` → some AI field
- DataWatcher slot 16 (byte): bit 0 = isCharging flag (`ax()` = returns `(DW16 & 1) != 0`)
- Combat: melee if target Y overlaps; ranged fireball attack in bursts of 3 (`yn` = SmallFireball) at range < 30
  - Burst pattern: nextFire=60 (charge up), 3 shots with 6-tick gaps, then 100-tick cooldown
  - Fireballs fired with Gaussian spread: `nextGaussian() * (0.5 * sqrt(distance))`
- Drops: `acy.bn` (Blaze Rod) — 0 to (1+looting) items
- Full-brightness rendering: `a(float)` returns 15728880 (max light)

### 8.2 jm (ZombiePigman)

- Texture: `/mob/pigzombie.png`
- `af = true` (fire immune)
- Passive unless attacked; aggro triggers aggro on all nearby ZombiePigmen within 32×32×32 AABB
- NBT tag "Anger" (short): anger timer 400-800 ticks; random broadcast delay `c` 0-39 ticks
- Drops: `acy.bl` (gold nugget) + `acy.bp` (cooked porkchop) — 0 to (1+looting) each
- Held item: `acy.F` (gold sword), 1 item, damage 0 — used for attack

### 8.3 aea (MagmaCube)

- Extends `aed` (Slime base class)
- Texture: `/mob/lava.png`
- `af = true` (fire immune)
- `aI = 0.2F` — some physics coefficient
- No drops (k() returns 0)
- Full-brightness rendering
- Splits into smaller MagmaCubes on death (via `ay()` which returns `new aea(world)`)
- Flame particle effect (inherits from Slime particle system, particle type = "flame")
- Jump height: `0.42 + size * 0.1` (size = MagmaCube size field)

---

## 9. Constants Summary

| Constant | Value | Context |
|---|---|---|
| Fortress spawn probability | 1/3 | per chunk column |
| Seed formula | `(chunkX ^ (chunkZ << 4)) ^ worldSeed` | structure RNG seed |
| Spawn offset range | [4, 11] | within-chunk origin offset |
| Max radius from origin | 112 blocks | in each cardinal direction |
| Max recursion depth | 30 | piece expansion depth |
| Generation Y bounds | [48, 70] | piece placement range |
| Starting position offset | +2 from chunk origin | gc start `(chunkX*16+2, chunkZ*16+2)` |
| Starting bounding box Y | 64 | hardcoded in gc constructor |

---

## 10. Known Quirks / Bugs to Preserve

### 10.1 Starting piece always bw-geometry

`gc` extends `bw` and uses `bw`'s protected constructor, which always creates a 19×10×19 bounding box regardless of orientation. The `switch(orientation)` in the constructor has the same case for 0/2 and default — all orientations produce the same AABB size. This is not a bug per se but means the starting piece can be misaligned with respect to local geometry for certain orientations.

### 10.2 BlazeSpawnerCorridor one-time placement

The `a` boolean field in `kf` prevents the spawner from being placed again if the chunk is re-generated or the piece is revisited. If the bounds check `var3.b(x,y,z)` fails (spawner is outside generation bounds), no spawner is placed at all.

### 10.3 Lava placement uses world.f flag

In `xr`, placing lava requires `world.f = true` to allow lava flow physics during world generation. After the block is placed, `world.f = false` restores normal behaviour. The Coder must replicate this flag-wrapping.

### 10.4 Piece count tracking is per-structure-instance

`gc` resets all piece counts to 0 at construction (`var7.c = 0` loop). This means each fortress starts with fresh counts — the max limits are per-fortress, not global.

### 10.5 StaircaseDown uses deterministic Random for wart metadata

`tr` calls `this.c(yy.bC.bM, 2)` at the start of generate to get the stairs metadata. This is a structural rotation operation — metadata value 2 is the base orientation, which gets rotated by the piece's `this.f` field. The Coder must implement the same stair metadata rotation.

---

## 11. Open Questions

### 11.1 nl.a() exact signature

The bounding box construction method `nl.a(x, y, z, -dx, -dy, 0, width, height, depth, orientation)` performs a rotation of the box based on orientation. The exact algorithm for how orientation 0-3 maps the offset parameters (-dx, -dy) to world coordinates is in `nl.java`. The Coder needs to implement this transformation correctly or the pieces will be misaligned.

### 11.2 Nether Wart metadata values in io

The wart placement calls `this.c(yy.bC.bM, N)` and `this.c(yy.bc.bM, N)` and `this.a(yy.bC.bM, var10, ...)` with metadata `var10`, `var12`, `var13`. The exact values of these metadata depend on how the rotation system translates stair/wart orientation. The Coder should implement stair rotation as: stair metadata 0 = south, 1 = north, 2 = west, 3 = east; rotated by adding `this.f` mod 4.

### 11.3 Wart metadata semantics

Nether Wart (ID 115) uses metadata 0–3 for growth age (0 = seed, 3 = mature). During fortress generation, specific ages are placed. The `c(yy.bD.bM, N)` call result should be age N (0–3).

### 11.4 ze TileEntityMobSpawner entity name

In `kf.generate()`: `ze var7 = (ze)world.b(wx, wy, wz); if (var7 != null) { var7.a("Blaze"); }`. The method `var7.a(String)` sets the entity name (mob type) for the spawner. The `ze` class is TileEntityMobSpawner from another spec.

### 11.5 Spawn list usage

The `ed.b()` spawn list is returned but how `ChunkProviderHell` uses it to actually spawn mobs near fortresses is not specified here. The Coder should consult the `ChunkProviderHell_Spec.md` for the call site.
