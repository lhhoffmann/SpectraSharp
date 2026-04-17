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

# Village Pieces Spec
**Source classes:** `yo.java` (VillageStart), `yp.java` (VillageComponent),
`xy.java` (VillagePieceRegistry), `uy/uz/gs/wi/acz/ec/agr/ko/tf.java` (building pieces),
`abj.java` (WellPiece), `ahz.java` (StreetPiece), `za.java` (RoadBase)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Map

| Class | Role |
|---|---|
| `yo` | VillageStart — extends `oa` (StructureStart base) |
| `yp` | VillageComponent — the centre starting node |
| `xy` | VillagePieceRegistry — static helper; registers piece types with weights |
| `xf` | VillagePiece — abstract base for all building pieces |
| `za` | RoadBase — abstract base for road/street pieces |
| `ahz` | StreetBetweenPieces — extends `za`; branching road segment |
| `abj` | WellPiece — extends `xf`; village well |
| `uy` | SmallHut — `xf` concrete; the smallest house type |
| `uz` | LargeHouse — `xf` concrete; 2-storey building |
| `gs` | Blacksmith — `xf` concrete; large open-fronted building |
| `wi` | HouseSmall2 — `xf` concrete; small house with roof variant |
| `acz` | Library — `xf` concrete; large rectangular building with bookshelves |
| `ec` | FarmLarge — `xf` concrete; 13-wide farm with crop rows |
| `agr` | FarmSmall — `xf` concrete; 7-wide small farm |
| `ko` | HouseLarge2 — `xf` concrete; large house with stone-brick roof tier |
| `tf` | Church — `xf` concrete; tallest building |
| `aan` | WeightedPiece — weight + count + max-count record |
| `nk` | StructurePiece — abstract base (shared with Stronghold) |

---

## 2. Village Generation Start — `yo`

`yo(ry world, Random rand, int chunkX, int chunkZ)` is called by `MapGenVillage`.

### 2.1 Startup Sequence

```
1. Generate piece list: xy.a(rand, 0) → List<aan> weighted pieces
2. Create centre VillageComponent yp at world coords:
     x = (chunkX << 4) + 2
     z = (chunkZ << 4) + 2
3. Call yp.a(yp, allPieces, rand) — the centre node expands recursively
4. Two expansion queues:
     yp.i — "building" queue (buildings added by StreetPieces)
     yp.h — "road" queue (streets added by buildings)
5. While either queue non-empty: pick random piece from non-empty queue, call a(yp, all, rand)
6. After all queues drain: call c() to compute AABB bounds
7. Count non-road pieces; village is valid if count > 2
```

### 2.2 `d()` → `boolean` — is valid

Returns `true` if the generated village contains more than 2 non-road pieces.

---

## 3. `yp` — VillageComponent (Centre Node)

Fields:

| Field | Type | Meaning |
|---|---|---|
| `a` | `vh` (WorldChunkManager) | Biome info for checking ocean/placement |
| `b` | `int` | Depth level (0 at start) |
| `c` | `aan` | Last selected piece type (avoid immediate repeat) |
| `d` | `ArrayList<aan>` | Available piece types with weights |
| `h` | `ArrayList<nk>` | Road expansion queue |
| `i` | `ArrayList<nk>` | Building expansion queue |

`yp` extends `aia` (abstract structure component) with a 2×2 bounding box at the start position.

---

## 4. `xy` — VillagePieceRegistry

### 4.1 Piece Weight Table

`xy.a(Random rand, int depth)` builds the initial piece list:

| Class | Weight | Min count | Max count formula | Piece type |
|---|---|---|---|---|
| `uy` | 4 | 2+depth | 4+(depth×2) | SmallHut |
| `uz` | 20 | 0+depth | 1+depth | LargeHouse |
| `gs` | 20 | 0+depth | 2+depth | Blacksmith |
| `wi` | 3 | 2+depth | 5+(depth×3) | HouseSmall2 |
| `acz` | 15 | 0+depth | 2+depth | Library |
| `ec` | 3 | 1+depth | 4+depth | FarmLarge |
| `agr` | 3 | 2+depth | 4+(depth×2) | FarmSmall |
| `ko` | 15 | 0 | 1+depth | HouseLarge2 |
| `tf` | 8 | 0+depth | 3+(depth×2) | Church |

Any entry whose max count resolves to 0 is removed.

`depth` is always 0 on initial generation (no scaling in 1.0).

### 4.2 Piece Selection — `xy.c()`

```
total = sum of weights of still-available piece types
loop up to 5 tries:
    pick random int in [0, total)
    walk weighted list; subtract each weight; first to go negative is selected
    if selected type == last selected AND more than 1 type remains: skip
    call piece factory; if not null: increment count, update last, remove if exhausted
    return piece
if no piece found after 5 tries: create a WellPiece (abj) instead
```

### 4.3 Radius Limit — `xy.d()`

Building pieces are only placed if:
- `depth ≤ 50`
- `|x - centre.x| ≤ 112` AND `|z - centre.z| ≤ 112`

Otherwise returns `null`.

### 4.4 Street Expansion — `xy.e()`

Road pieces (`ahz`) are placed if:
- `depth ≤ 3 + yp.b` (deeper road limit than buildings)
- Same 112-block radius check
- Road segment length `> 10` (short segments discarded)

---

## 5. Building Pieces

All pieces extend `xf` and follow the same pattern:
1. Constructor stores bounding box (`nl`) and facing (`int var4`).
2. `a(List, Random, x, y, z, facing, depth)` — static factory: creates bounding box at coordinates, rejects if overlaps existing pieces (`nk.a(list, nl) != null`).
3. `a(ry, Random, nl)` — generate method: fills bounding box with blocks.
4. `a(nk, List, Random)` — expansion hook: empty for all building pieces (buildings do not expand further).
5. `b(ry, nl)` — finds ground height under the bounding box.

### 5.1 Bounding Box Dimensions

Dimensions from `nl.a(x, y, z, 0, 0, 0, W, H, D, facing)`:

| Class | W | H | D | Description |
|---|---|---|---|---|
| `uy` | 5 | 6 | 5 | SmallHut — 1 room, cobblestone/plank |
| `uz` | 5 | 12 | 9 | LargeHouse — 2 floors, oak + glass windows |
| `gs` | 9 | 9 | 6 | Blacksmith — open front, sandstone/stone |
| `wi` | 4 | 6 | 5 | HouseSmall2 — random roof variant |
| `acz` | 9 | 7 | 11 | Library — bookshelves, fence-enclosed |
| `ec` | 13 | 4 | 9 | FarmLarge — crop rows + fence perimeter |
| `agr` | 7 | 4 | 9 | FarmSmall — smaller crop field |
| `ko` | 10 | 6 | 7 | HouseLarge2 — stone-brick roof band |
| `tf` | 9 | 7 | 12 | Church — tall structure |
| `abj` (well) | 3 | 4 | 2 | Well — cobblestone column + water |

### 5.2 SmallHut (`uy`) — Notable Features

- Wall material: `yy.w` (cobblestone).
- Roof/upper: `yy.J` (oak log) + `yy.x` (glass or planks).
- Door (`yy.aH`) placed at front face if air below.
- Torch (`yy.bq`) at 3 interior positions.
- Crafting table (`yy.aq`) inside.
- Randomised `b` flag: adds an extra fence-ring (`yy.aZ`) level at top if true.
- Randomised `b` flag: adds wooden stair (`yy.aF`) decorations.

### 5.3 LargeHouse (`uz`) — Notable Features

- Two storeys; bounding box 5W × 12H × 9D.
- Upper storey uses cobblestone (`yy.w`); lower uses planks/glass.
- Multiple doors (via `yy.aH`); windows.
- Chest (`yy.aE`) placed at a fixed position.
- Fence gate or door variant at front.
- 4 torches across the exterior.

### 5.4 Blacksmith (`gs`) — Notable Features

- Dimensions: 9W × 9H × 6D.
- Uses `yy.at` (sandstone or stone brick) for main walls — distinct material from other pieces.
- Pitched roof: stair blocks `yy.at` rows ascending and descending.
- Large open front face.
- Multiple torches; crafting table inside.
- Possibly contains a chest with loot (open question — see section 8).

### 5.5 FarmLarge (`ec`) — Notable Features

- Dimensions: 13W × 4H × 9D.
- Floor: `yy.aA` (farmland, ID 60).
- Fence (`yy.J`) perimeter posts at regular intervals.
- Crop rows (wheat blocks at stage 7?) across the field.
- Water irrigation channel `yy.A` (water, ID 9) at specific positions.

### 5.6 WellPiece (`abj`) — Notable Features

- Dimensions: 3W × 4H × 2D.
- Cobblestone column `yy.aZ` at x=1, heights 0–2.
- Top cobblestone slab `yy.ab` at (1,3,0).
- Water source blocks `yy.aq` at (0,3,0), (1,3,1), (2,3,0), (1,3,-1).
  - These form a plus pattern — likely the well water pool.

> Open question: `yy.aq` at well = water source (ID 9)? Or crafting table? Positional context suggests water.

---

## 6. `ahz` — StreetBetweenPieces (Road Segments)

`ahz` extends `za` (road abstract base). A road segment is placed between buildings.

### 6.1 Fields

| Field | Type | Meaning |
|---|---|---|
| `a` | `int` | Road length = max(bounding box width, depth) |

### 6.2 Expansion

A road segment calls `xy.b()` to try placing buildings on either side at random intervals:

```
for each side (two passes):
    start = rand.nextInt(5)
    while start < length - 8:
        try to place a building piece on left/right side
        if placed: advance by building's longer dimension
        start += 2 + rand.nextInt(5)
```

If buildings were placed on either side and `rand.nextInt(3) > 0`:
- Place a new `ahz` road segment extending perpendicularly at the start or end of this segment.

The 4 facing directions (0–3) control whether perpendicular roads extend from the east, west, north, or south end.

### 6.3 `za` (RoadBase) — abstract

Empty abstract class; only purpose is to serve as a type marker so that road pieces can be filtered from buildings in `yo.d()`.

---

## 7. Spawn Point Assignment

In `ry.i()` (first-time world init):
1. Call `WorldChunkManager.findBiomePosition(0, 0, 256, validBiomes, rand)`.
2. Walk random offsets (up to 1000 tries) until `WorldProvider.canCoordinateBeSpawn(x, z)` returns true.
3. `WorldInfo.setSpawnPoint(x, heightAt(x, z) / 2, z)`.

Valid spawn biomes exclude ocean, ice plains, and mushroom island.

> The village biome check uses the same WorldChunkManager; valid village biomes are plains and desert in 1.0.

---

## 8. Open Questions

| # | Question |
|---|---|
| 8.1 | `gs` (Blacksmith): does it contain a chest with loot? Which items? Confirm by reading full gs.java. |
| 8.2 | What is `yy.aq` used in `abj` (well)? Water source (ID 9) or crafting table (ID 58)? |
| 8.3 | `yy.at` = sandstone (ID 24) or stone brick (ID 98)? Used in `gs` and `ko` roofs. |
| 8.4 | Does `agr` (FarmSmall) also place crops? Partial read showed farmland but no explicit crop block placement. |
| 8.5 | Does `tf` (Church) place any noteworthy loot or special blocks? Not fully read. |
| 8.6 | `acy.bm` piece type `wi.c` — is this the hay-bale variant? `wi.c = rand.nextInt(3)` selects roof material. |
| 8.7 | Villager spawning: is a villager entity spawned in any piece during generation, or only when player approaches? |
| 8.8 | Road material: `ahz` road surface — what block ID fills the ground under the road? Gravel (13)? |
