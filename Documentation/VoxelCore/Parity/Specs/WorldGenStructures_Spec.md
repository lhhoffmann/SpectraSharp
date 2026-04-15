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

# WorldGenStructures Spec — Mineshaft, Stronghold, Village
Source classes:
- Mineshaft: `kd.java` (MapGenMineshaft), `ns.java` (MineshaftStart), `uk.java` (starting piece), `aba.java` (corridor), `aez.java` (piece factory + chest loot), `id.java`, `ra.java`
- Stronghold: `dc.java` (MapGenStronghold), `kg.java` (StrongholdStart), `aeh.java` (starting room)
- Village: `xn.java` (MapGenVillage), `yo.java` (VillageStart), `yp.java` (VillageStartPiece)
- Shared base: `hl.java` (MapGenStructureBase), `oa.java` (StructureStart), `nk.java` (StructurePiece), `nl.java` (BoundingBox), `acm.java` (ChunkCoord)
- ChunkProviderGenerate integration: `xj.java` fields `d` (Stronghold), `e` (Village), `f` (Mineshaft)

---

## 1. ChunkProviderGenerate Integration

`xj` (ChunkProviderGenerate) holds three structure generators:
- `this.d = new dc()` — Stronghold
- `this.e = new xn()` — Village
- `this.f = new kd()` — Mineshaft

All three are guarded by `this.t` (boolean hasStructures flag, controlled by world/level options).

**Phase 1 — provideChunk (populates block arrays):**
```
if (this.t) {
    this.f.a(IChunkProvider, world, chunkX, chunkZ, ...);
    this.e.a(IChunkProvider, world, chunkX, chunkZ, ...);
    this.d.a(IChunkProvider, world, chunkX, chunkZ, ...);
}
```
This call registers structures in the generation queue without placing blocks yet.

**Phase 2 — populate (places actual blocks):**
```
if (this.t) {
    this.f.a(world, rng, chunkX, chunkZ);      // Mineshaft — no return value
    var11 = this.e.a(world, rng, chunkX, chunkZ);  // Village — returns isVillage boolean
    this.d.a(world, rng, chunkX, chunkZ);      // Stronghold — no return value
}
if (!var11 && rng.nextInt(4) == 0) {
    // spawn dungeon (suppressed when village present)
}
```
The Mineshaft and Stronghold RNG seed for this chunk is set before populate:
`rng.setSeed(chunkX * var7 + chunkZ * var9 ^ worldSeed)` where var7/var9 are derived from worldSeed at startup.

---

## 2. Mineshaft (kd — MapGenMineshaft)

### 2.1 Placement — shouldGenerateHere (obf: `a(int chunkX, int chunkZ)`)

The structure RNG `this.b` is reseeded per-chunk during the structure-check pass (via hl base class). Then:

1. If `rng.nextInt(100) != 0` → false (1% base chance)
2. Return `rng.nextInt(80) < max(|chunkX|, |chunkZ|)`

Effect: mineshafts are very rare near spawn (0% at origin) and become more common at distance. At |coord|=40 chunks (640 blocks), probability = 1% × 50% = 0.5%. At |coord|≥80 chunks (1280 blocks), probability = 1% × 100% = 1%.

### 2.2 Structure Start (ns — MineshaftStart)

Constructor:
1. Creates `uk` (starting piece) at world position `(chunkX * 16 + 2, chunkZ * 16 + 2)`
2. Adds uk to piece list, calls `uk.a(uk, list, rng)` to expand
3. Calls `this.c()` (compute bounding box)
4. Calls `this.a(world, rng, 10)` — generate all pieces into world with minimum Y = 10

### 2.3 Piece Selection (aez.b — recursive expansion)

Each corridor segment calls `aez.b(root, list, rng, x, y, z, facing, depth+1)` for exits.

Depth guard: if `depth > 8` → return null (no more expansion)
Radius guard: if `|x - root.X| > 80` OR `|z - root.Z| > 80` → return null

Piece type selection (aez.a — per exit):
- `nextInt(100)`:
  - [80, 99] (20%) → try `id` piece (room / side room)
  - [70, 79] (10%) → try `ra` piece (staircase / descent)
  - [0, 69] (70%) → try `aba` piece (corridor — primary)
- If chosen piece cannot fit (bounding box overlap) → return null (no piece placed)

### 2.4 aba — Corridor Piece

**Bounding box:** Length varies (2-6 segments × 5 blocks = 10-30 blocks), width = 3, height = 3

**Constructor fields:**
- `a` (boolean) = isMain (1/3 chance: `nextInt(3) == 0`) — has special floor
- `b` (boolean) = hasSpawner (only when NOT isMain, `nextInt(23) == 0` ≈ 4.3%) — places cave spider spawner
- `c` (boolean) = spawnerPlaced (set to true once spawner is placed)
- `d` (int) = segmentCount (2-6, from bounding box width/5)

**Generated layout per segment (segment position = local Z = 2 + segIndex * 5):**

At each support position `var10 = 2 + segIndex * 5`:
1. Left fence post: wood fence (ID 85) at (X=0, Y=0, Z=var10) through (X=0, Y=1, Z=var10)
2. Right fence post: wood fence at (X=2, Y=0, Z=var10)
3. Ceiling planks: if `nextInt(4) != 0` (75% chance) — 3-wide planks at (X=0–2, Y=2, Z=var10); else planks only at sides (X=0 and X=2, Y=2, Z=var10)
4. Rails along Z axis: Rail (ID 66) at (X=0 and X=2, Y=0, Z=var10) — vertical support rails

**Cobweb placement (probabilistic, single block):**
At each support:
- 10% chance: cobweb at Z=var10-1, ceiling (Y=2), X=0
- 10% chance: cobweb at Z=var10-1, ceiling, X=2
- 10% chance: cobweb at Z=var10+1, ceiling, X=0
- 10% chance: cobweb at Z=var10+1, ceiling, X=2
- 5% chance: cobweb at Z=var10-2, ceiling, X=0
- 5% chance: cobweb at Z=var10-2, ceiling, X=2
- 5% chance: cobweb at Z=var10+2, ceiling, X=0
- 5% chance: cobweb at Z=var10+2, ceiling, X=2
- 5% chance: torch (ID 50) at Z=var10-1, ceiling, X=1
- 5% chance: torch at Z=var10+1, ceiling, X=1

**Chest wagon placement (1% chance per support, twice):**
- `nextInt(100) == 0` → place chest wagon at (X=2, Y=0, Z=var10-1) using aez.a() loot table
- `nextInt(100) == 0` → place chest wagon at (X=0, Y=0, Z=var10+1)

**Cave Spider Spawner (if `this.b == true` and not yet placed):**
- Find a Z position: `var12 = var10 - 1 + nextInt(3)` (randomly near current support)
- If world bounds include this position: place MobSpawner (ID 52) at (X=1, Y=0, Z=var12), set entity name "CaveSpider"
- Set `this.c = true` (won't try again)

**Floor planks (if `this.a == true`):**
- For each Z in [0, depth]: if block at Y=-1 is solid and has grass (`yy.m[blockId] == true`) → place torch (ID 50) at Y=0 with metadata `c(yy.aG.bM, 0)` (rotated torch facing)

**Exits:** Each aba piece has 1 forward exit, optional left exit, optional right exit (each has 1/5 chance of spawning per support using `aez.a` recursively)

### 2.5 Chest Loot Table (aez.a[])

11-entry weighted loot list for mineshaft chest wagons:

| acy field | Item description | Min | Max | Weight |
|---|---|---|---|---|
| `acy.n` | Torch | 1 | 5 | 10 |
| `acy.o` | Rails (item) | 1 | 3 | 5 |
| `acy.aB` | Redstone | 4 | 9 | 5 |
| `acy.aV` | Lapis (dye dam=4) | 4 | 9 | 5 |
| `acy.m` | Feather | 1 | 2 | 3 |
| `acy.l` | Rails | 3 | 8 | 10 |
| `acy.T` | Bread | 1 | 3 | 15 |
| `acy.f` | (unknown — rare) | 1 | 1 | 1 |
| `yy.aG` | Rail block | 4 | 8 | 1 |
| `acy.bg` | (unknown) | 2 | 4 | 10 |
| `acy.bf` | (unknown) | 2 | 4 | 10 |

Note: `agq(itemId, damageMin, countMin, countMax, weight)` — damage values apply.

---

## 3. Stronghold (dc — MapGenStronghold)

### 3.1 Placement

3 strongholds per world, placed once on first access (`this.f` flag prevents re-computation).

**Positioning algorithm:**
1. Seed RNG with world seed.
2. Initial angle = `rng.nextDouble() * π * 2` (random start angle)
3. For each of the 3 strongholds:
   - Distance = `(1.25 + rng.nextDouble()) * 32.0` chunks ≈ 40–64 chunks (640–1024 blocks)
   - `chunkX = round(cos(angle) * distance)`
   - `chunkZ = round(sin(angle) * distance)`
   - Find nearest valid biome: search biome map for one of 7 biomes within 112 blocks of `(chunkX*16+8, chunkZ*16+8)`
     - Valid biomes: `sr.d, sr.f, sr.e, sr.h, sr.g, sr.n, sr.o` (forest, taiga, extreme hills, jungle, plains, desert, and one more — sr.o)
     - If found: update chunkX/chunkZ to the biome position
     - If NOT found: print warning `"Placed stronghold in INVALID biome"` and use original position
   - Store `this.g[i] = new acm(chunkX, chunkZ)`
   - Advance angle by `2π / 3` (even angular distribution)

**shouldGenerateHere:** returns true if (chunkX, chunkZ) equals any of the 3 stored positions.

### 3.2 Structure Start (kg — StrongholdStart)

Constructor:
1. Calls `tc.a()` — initializes stronghold piece registry (resets all piece counts to 0)
2. Creates `aeh` (starting room/staircase piece) at `(chunkX * 16 + 2, chunkZ * 16 + 2)`
3. Expands via pending list `aeh.c` (similar to Nether Fortress expansion)
4. Calls `this.c()` and `this.a(world, rng, 10)` — generates all pieces with min Y=10

**Stronghold pieces (registered in tc):** Multiple pieces including:
- `aeh` — starting staircase room
- `vn` — straight corridor (5×5×N stone brick)
- Other pieces for: crossing rooms, prison cells, library rooms, portal room, storage rooms
- Block palette: Stone Brick (ID 98, yy.bm) primarily

Note: Full per-piece details are in the stronghold piece classes (`aeh`, `vn`, and others extending `os`). A separate stronghold-piece spec may be needed for the Coder.

### 3.3 vn — Straight Corridor Piece

**Fields:** `a` (int) = depth (corridor length in blocks); computed from bounding box dimension:
- If orientation 1 or 3 (horizontal): `a = bounds.W / 5`
- If orientation 0 or 2 (perpendicular): `a = bounds.D / 5`

**Generated layout:**
For each layer Z (0 to a-1):
- 5×1×1 stone brick floor at Y=0, X=0–4
- 3×3×1 air corridor at X=1–3, Y=1–3
- 1×3×1 stone brick walls at X=0 and X=4, Y=1–3
- 5×1×1 stone brick ceiling at Y=4

**Exits:** none — straight corridor terminates or connects.

---

## 4. Village (xn — MapGenVillage)

### 4.1 Placement

**Grid system:** Villages use a 32-chunk (512-block) grid.

Per candidate chunk (chunkX, chunkZ):
1. Compute grid cell: `gridX = floor(chunkX / 32)`, `gridZ = floor(chunkZ / 32)` (with negative adjustment)
2. Per-cell RNG: `cellRng = world.x(gridX, gridZ, 10387312)` — seeded consistently per cell
3. `gridX *= 32`, `gridZ *= 32`
4. `gridX += cellRng.nextInt(32 - 8)` = `gridX + nextInt(24)` — random position within [0, 23] of cell
5. `gridZ += cellRng.nextInt(24)` — same
6. If `chunkX == gridX && chunkZ == gridZ`:
   - Biome check at `(chunkX * 16 + 8, chunkZ * 16 + 8)`: must be in list `xn.e`
   - `xn.e = [sr.c, sr.d]` — plains and desert biomes only
   - If valid biome → return true (place village)
7. Return false

**Village returns a boolean** from `populate` (`var11`) — when true, suppresses dungeon spawning in that chunk.

### 4.2 Structure Start (yo — VillageStart)

Constructor:
1. Starting variant: `byte var5 = 0` (hardcoded — always type 0)
2. Gets piece list for this variant: `xy.a(rng, var5)` — returns a shuffled list of piece types
3. Creates `yp` (VillageStartPiece) at `(chunkX * 16 + 2, chunkZ * 16 + 2)` with piece list
4. Expands via two pending queues `yp.i` (road pieces) and `yp.h` (building pieces), alternating:
   - Road pieces come from queue `i`, building pieces from queue `h`
   - Random selection within each queue
5. Computes bounding box and generates into world (Y determined during generation)

**Village biomes supported:** Plains and Desert only in 1.0.

**Village structure:** Roads, houses, wells, farms, blacksmiths, libraries. Exact piece layout depends on `xy.a()` shuffle and `yp` expansion.

---

## 5. Piece Coordinate System

All three structure types use the same piece-coordinate → world-coordinate transformation system via `nl` (BoundingBox) and `nk.b()` / `nk.e()` helper methods. The `nl.a(x, y, z, -dx, -dy, 0, width, height, depth, orientation)` factory creates a rotated bounding box.

**Orientation values (same as Nether Fortress):** 0=south, 1=west, 2=north, 3=east

**Minimum Y:** All three structures use `this.a(world, rng, 10)` which enforces minimum Y = 10 (pieces are not placed below Y=10).

---

## 6. Known Quirks / Bugs to Preserve

### 6.1 Stronghold biome validation is advisory only

If no valid biome is found within 112 blocks of the chosen position, the stronghold is placed anyway with a System.out.println warning. The Coder must not throw an exception here.

### 6.2 Stronghold positions computed only once, on first shouldGenerateHere call

`dc.f` prevents re-computation. The positions are cached for the session lifetime. On world reload, this must be recomputed (since dc is re-instantiated with ChunkProviderGenerate).

### 6.3 Mineshaft suppresses nothing; Village suppresses dungeons

The Village generator returns a boolean from populate. When true, the dungeon spawn (`nextInt(4) == 0`) is skipped for that chunk. This is the only suppress interaction between structure generators.

### 6.4 aba cave spider spawner uses Y=0 in local coords

The spawner is placed at local Y=0 (floor level). If the corridor is at Y<1 in world coordinates, the spawner could end up at Y<1 — the bounds check prevents this only if the full world-space bounds check fails.

### 6.5 Mineshaft distance scaling creates dead zone near spawn

At chunk coordinates |c| < 1 (close to 0,0), `max(|chunkX|, |chunkZ|) = 0`, so `nextInt(80) < 0` is always false — no mineshafts within the first 16 blocks of world origin. At |c| = 1, probability = 1% × (1/80) ≈ 0.01%.

---

## 7. Open Questions

### 7.1 Full stronghold piece list

Only `aeh` (starting room) and `vn` (straight corridor) were analyzed. The full list of stronghold pieces (crossing, prison, library, portal room, staircase, store room) requires reading all classes extending `os`. A separate stronghold-piece spec should be requested.

### 7.2 Village piece list

`yp` and `xy.a()` control piece selection. Village buildings, the well, paths, and villager spawns are not fully specced. A separate village-piece spec should be requested when village implementation begins.

### 7.3 acy.f, acy.bg, acy.bf item IDs

These mineshaft loot items are unresolved from acy.java without reading that file in full context. The Coder should consult the existing item mappings in classes.md.

### 7.4 world.x() seeding method

The village uses `world.x(gridX, gridZ, salt)` for a per-cell RNG. This is a seeded positional RNG. Its exact implementation (likely `new Random((gridX * K + gridZ * M + salt) ^ worldSeed)`) should be confirmed from World.java.

### 7.5 sr biome constants for Stronghold biome list

`sr.d, sr.f, sr.e, sr.h, sr.g, sr.n, sr.o` — 7 biomes valid for stronghold placement. Without the full sr enum listing, exact biome names are unknown. The Coder should look them up in BiomeGenBase constants when implementing the biome filter.
