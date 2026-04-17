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

# Stronghold Pieces Spec
**Source classes:** `os.java` (abstract base), `tc.java` (piece factory), `mj.java` (door enum),
  `aeh.java` (StrongholdStart), `vl.java` (StraightStairs), `so.java` (SpiralStairs),
  `gp.java` (SimpleCorridor), `vn.java` (StraightCorridor), `fj.java` (Prison),
  `hq.java` (LeftTurn), `xg.java` (RightTurn), `jt.java` (Crossing),
  `kt.java` (LargeRoom), `ys.java` (SmallRoom), `zc.java` (Library), `ir.java` (PortalRoom)
**Analyst:** lhhoffmann
**Date:** 2026-04-16
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

```
os  (abstract StrongholdPiece base)
 ├─ gp   SimpleCorridor
 ├─ vn   StraightCorridor       (variable length)
 ├─ fj   Prison
 ├─ hq   LeftTurn
 ├─ xg   RightTurn
 ├─ jt   Crossing
 ├─ kt   LargeRoom
 ├─ so   SpiralStairs
 ├─ vl   StraightStairs
 │   └─ aeh  StrongholdStart   (extends vl)
 ├─ ys   SmallRoom
 ├─ zc   Library
 └─ ir   PortalRoom

tc  (static factory — not a piece)
mj  (door-type enum — used by os subclasses)
```

---

## 2. Abstract Base (`os`)

Every piece stores:
- An `nl` (StructureBoundingBox) — axis-aligned bounding box in world coordinates.
- A facing direction (N/S/E/W as integer 0–3).
- A `mj` door type for each connection point (open, wood door, iron door, iron grating).
- A depth counter, incremented by the factory when a child piece is appended.

The abstract `generate(World, Random, BoundingBox)` method is overridden by each subclass to place blocks.

---

## 3. Door Type Enum (`mj`)

| Constant | English name | Description |
|---|---|---|
| `a` | Open | No door block — bare archway |
| `b` | Wood door | `yy.bp` (ID 64) wooden door block |
| `c` | Iron door | `yy.bq` (ID 71) iron door block |
| `d` | Iron bars | `yy.bE` (ID 101) iron bar grating |

Door type is chosen per-connection by the individual piece's generation logic.
The portal room always uses type `c` (iron door) for its entrance.

---

## 4. Piece Factory (`tc`)

### 4.1 Weight Table

`tc` maintains a static array of `ci` (WeightedPiece) descriptors:

| Class | English name | Weight | Max count | Notes |
|---|---|---|---|---|
| `gp` | Simple Corridor | 40 | unlimited | Most common corridor |
| `fj` | Prison | 5 | 5 | Cells with iron bars |
| `hq` | Left Turn | 20 | unlimited | 90° left |
| `xg` | Right Turn | 20 | unlimited | 90° right |
| `jt` | Crossing | 10 | 6 | 4-way junction |
| `so` | Spiral Stairs | 5 | 5 | Descends 7 blocks |
| `vl` | Straight Stairs | 5 | 5 | Descends 7 blocks |
| `kt` | Large Room | 5 | 4 | Storeroom with chest |
| `ys` | Small Room | 5 | 4 | Dead-end room |
| `zc` | Library | 10 | 2 | Special `iz`; bookshelves + loot chest |
| `ir` | Portal Room | 20 | 1 | Special `th`; goal destination |

**`iz`** and **`th`** are subclasses of `ci` that override the instantiation or placement logic.
`iz` (Library wrapper) and `th` (PortalRoom wrapper) both enforce their respective max counts.

### 4.2 Piece Selection (`tc.c`)

When extending a piece from an open doorway the factory:

1. Removes the max-capped piece types that have already reached their limit from the eligible set.
2. Chooses a random piece from the remaining eligible types, weighted by their `weight` value.
3. Attempts to place the bounding box; if it intersects existing pieces the attempt is discarded.
4. Returns `null` (terminates the branch) when:
   - `depth > 50` (hard depth limit), OR
   - `|piece.x − start.x| > 112` OR `|piece.z − start.z| > 112` (XZ radius cap).

### 4.3 Fallback Piece (`vn`)

`vn` (StraightCorridor) is used as a fallback connector when no weighted piece fits.
Its length (`a` field) is set to a small fixed value to cap the dead end.
`vn` is **not** in the weight table and is never chosen randomly; it only appears as a terminator.

### 4.4 Start Sequence

1. `aeh` (StrongholdStart, a `vl` with `isStart=true`) is placed first.
2. Because `isStart=true`, `vl.generate()` forces a `kt` (Large Room) piece at the upper exit before any random selection occurs.
3. The factory then recursively extends all open doorways depth-first until all branches terminate.

---

## 5. Piece Dimensions

Bounding boxes are parameterised as `(width, height, depth)` in the piece's local facing
coordinate system. `nl.a(x,y,z,−offX,−offY,0,W,H,D,dir)` converts to world AABB.

| Class | W × H × D | Depth offset | Notes |
|---|---|---|---|
| `gp` Simple Corridor | 5 × 5 × 7 | — | 1 forward exit |
| `vn` Straight Corridor | 5 × 5 × var | — | Length = `a` field |
| `fj` Prison | 9 × 5 × 11 | — | Side cells; 1 forward exit |
| `hq` Left Turn | 5 × 5 × 5 | — | 1 left exit |
| `xg` Right Turn | 5 × 5 × 5 | — | 1 right exit |
| `jt` Crossing | 11 × 7 × 11 | — | Up to 3 exits (F/L/R) |
| `kt` Large Room | 10 × 9 × 11 | — | 2 exits; chest loot |
| `so` Spiral Stairs | 5 × 11 × 8 | −7 (descends) | 1 forward exit at bottom |
| `vl` Straight Stairs | 5 × 11 × 5 | −7 (descends) | 1 forward exit at bottom |
| `ys` Small Room | 5 × 5 × 7 | — | 0 exits (dead end) |
| `zc` Library (tall) | 14 × 11 × 15 | — | `c=false`; 2-floor |
| `zc` Library (short) | 14 × 6 × 15 | — | `c=true`; 1-floor |
| `ir` Portal Room | 11 × 8 × 16 | −4,−1 offset | 1 entrance; no exits |

The Library's height variant is selected by the factory based on available vertical space
(`c = boundingBox.height() <= 6`).

---

## 6. Piece Details

### 6.1 Simple Corridor (`gp`)

- Primary building material: stone brick (`yy.bm`, ID 98).
- Cracked stone brick (`yy.bn`, ID 98:2) and mossy stone brick (`yy.bo`, ID 98:1) scattered randomly.
- Cobweb (`yy.bz`, ID 30) decorations on ceiling.
- Torch (`yy.aq`, ID 50) placements on walls.
- One forward exit door, type varies by depth.

### 6.2 Straight Corridor (`vn`)

- Same material palette as `gp`.
- Used as a fallback dead-end connector; no loot or special features.

### 6.3 Prison (`fj`)

- 9×5×11 room divided into iron-bar-gated cells.
- Each cell has a chance of containing a chest with generic loot.
- Iron bars (`yy.bE`, ID 101) form the cell walls.
- One forward exit.

### 6.4 Left Turn / Right Turn (`hq` / `xg`)

- Minimal 5×5×5 corner piece.
- No features beyond the turn itself.
- `hq` exits left, `xg` exits right (relative to entry facing).

### 6.5 Crossing (`jt`)

- Largest corridor junction: 11×7×11.
- Up to three exits: forward, left, right (one or more may be walled off).
- Torches on columns; cobblestone pillar supports.

### 6.6 Large Room / Storeroom (`kt`)

- 10×9×11; forced as the first piece after `aeh` (start).
- Contains a chest with loot (same pool as Prison cells).
- Two exits on opposite walls.

### 6.7 Spiral Staircase (`so`)

- Descends 7 blocks over 8 blocks of depth.
- Staircase constructed from stone brick slabs (`yy.bL`, ID 109) rotating around a central column.
- One exit at the bottom end.

### 6.8 Straight Staircase (`vl`)

- Descends 7 blocks over 5 blocks of depth with a straight flight of stairs.
- Same material palette as corridors.
- One exit at the bottom.
- When `isStart=true` (only for `aeh`): forces a `kt` (Large Room) at the **upper** exit
  before any randomised selection.

### 6.9 StrongholdStart (`aeh`, extends `vl`)

Additional fields:

| Field | Type | Purpose |
|---|---|---|
| `a` | `ci` | Tracks the last piece type chosen, for variety enforcement |
| `b` | `ir` | Reference to the Portal Room, set when `ir` is generated |
| `c` | `ArrayList<os>` | Complete list of all generated pieces |

`aeh` does not override `generate()`; all generation logic is inherited from `vl`.

### 6.10 Small Room (`ys`)

- 5×5×7 dead-end alcove.
- May contain a chest; no exits.

### 6.11 Library (`zc`)

**Height selection:** If the available vertical space at placement time allows a height > 6,
the tall variant is used (`c=false`, H=11). Otherwise the short variant (`c=true`, H=6).

**Loot chest** (both variants):

| Item | Quantity | Weight |
|---|---|---|
| Paper (`acy.aK`) | 1–3 | 20 |
| Book (`acy.aJ`) | 2–7 | 20 |
| Enchanted Book (`acy.bc`) | 1 | 1 |
| Compass (`acy.aP`) | 1 | 1 |

**Features:**
- Bookshelves (`yy.an`, ID 47) lining the walls.
- Wooden planks (`yy.w`, ID 4) floor.
- Fence posts (`yy.y`, ID 85) as railings on the upper floor (tall variant only).
- One loot chest per floor.

### 6.12 Portal Room (`ir`)

The terminal piece of every stronghold — there is always exactly one.

**Registration:** During `generate()`, the portal room calls `((aeh) parent).b = this`,
storing itself as the stronghold's portal room reference.

**Contents:**

| Feature | Block | ID | Notes |
|---|---|---|---|
| End Portal Frame | `yy.bI` | 120 | 12 frames in 3×3 ring minus corners; facing metadata |
| Water pool | `yy.C` | 9 | 3×1×3 pool in floor pit |
| Silverfish spawner | `yy.as` | 52 | `ze` TileEntity; placed once only (`a` flag guard) |
| Entrance | — | — | Iron door (`mj.c`) on entry wall |
| Torches | `yy.aq` | 50 | Wall-mounted |
| Lava source | `yy.C` | 11 | Under the portal frame (lava, not water) |

**`a` flag:** A boolean field on `ir` prevents the silverfish spawner `ze` from being written
twice if `generate()` is called more than once (e.g., during chunk boundary retries).

**Portal frame orientation:** Each of the 12 frame blocks stores directional metadata so
that `ir` frames face inward. The portal activates when all 12 frames contain an Eye of Ender.

---

## 7. Generation Constraints

| Constraint | Value | Source |
|---|---|---|
| Max depth | 50 | `tc.c()` depth guard |
| XZ radius from start | 112 | `tc.c()` coordinate check |
| Max Portal Rooms | 1 | `th` wrapper in weight table |
| Max Libraries | 2 | `iz` wrapper in weight table |
| Primary material | stone brick `yy.bm` ID 98 | All structural pieces |
| Cracked / mossy variants | IDs 98:2 / 98:1 | Scattered randomly per piece |
| Cobweb block | `yy.bz` ID 30 | Corridors and corridors only |

---

## 8. Open Questions

| # | Question |
|---|---|
| 8.1 | Exact chest loot pool for corridors, prisons, and large rooms — not yet confirmed. |
| 8.2 | `vn` exact fallback length value when used as dead-end terminator. |
| 8.3 | Whether `jt` (Crossing) selects its active exits randomly or always spawns all three. |
| 8.4 | Portal room lava vs. water distinction — source class line 36–39 ambiguous in summary. |

---

## 9. Relationship to WorldGenStructures_Spec

This spec fully resolves **Open Question 7.1** from `WorldGenStructures_Spec.md`:
> "The full list of stronghold pieces and their construction rules are not yet documented.
> A separate stronghold-piece spec should be requested."

The `WorldGenStructures_Spec` covers stronghold placement (how many strongholds, where they
spawn in the world). This spec covers the internal structural generation (piece types and
their layout rules). Both must be implemented together for a complete stronghold.
