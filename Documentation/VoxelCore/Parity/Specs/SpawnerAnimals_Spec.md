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

# SpawnerAnimals Spec
**Source class:** `we.java`
**Type:** Final utility class (no instances — all methods static)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`we` (SpawnerAnimals) is the passive mob and hostile mob spawning system. It runs every tick
via the world update loop and manages the ongoing population of living entities. It is
distinct from the initial populate-time spawn (see §5) and from mob spawner blocks.

There are two entry points:
- **`a(ry, boolean, boolean)`** — called on every server tick to maintain population.
- **`a(ry, sr, int, int, int, int, Random)`** — called during chunk populate to place initial animals.

---

## 2. Fields

| Field | Type | Semantics |
|---|---|---|
| `b` | `static HashMap` | Chunk coordinate accumulator — reused per tick, cleared at start |
| `a` | `static Class[]` | `{vq.class, gr.class, it.class}` = `{Spider, Zombie, Skeleton}` — hostile mob classes used for density tracking |

---

## 3. Creature Type Enum — `jf` (EnumCreatureType)

Three enum values control which mob class, cap, material, and passive flag apply:

| Enum | Base class | Cap | Material | Passive |
|---|---|---|---|---|
| `jf.a` | `aey` (hostile base) | 70 | `p.a` (solid) | false |
| `jf.b` | `fx` (EntityAnimal) | 15 | `p.a` (solid) | true |
| `jf.c` | `dn` (water creature) | 5 | `p.g` (water) | true |

- `jf.d()` returns the `passive` boolean.
- `jf.b()` returns the population cap.
- `jf.c()` returns the required material for spawn location.
- `jf.a()` returns the base class for entity counting.

---

## 4. Methods — Detailed Logic

### `a(ry world, boolean spawnHostile, boolean spawnPassive)` — tickSpawn

Called every server tick to maintain the per-biome mob population. Returns the number of
mobs successfully spawned this tick.

```
if NOT spawnHostile AND NOT spawnPassive:
    return 0

b.clear()    // reuse HashMap between ticks

// Step 1: Build chunk map around all players
for each player vi in world.playerList:
    playerChunkX = floor(player.x / 16)
    playerChunkZ = floor(player.z / 16)
    radius = 8

    for dx in -8..8:
        for dz in -8..8:
            coord = new ChunkCoordIntPair(playerChunkX + dx, playerChunkZ + dz)
            isBorder = (dx == -8 OR dx == 8 OR dz == -8 OR dz == 8)

            if NOT isBorder:
                b.put(coord, false)           // inner chunk: eligible for spawning
            else if NOT b.containsKey(coord):
                b.put(coord, true)            // border chunk: tracking only

// b now contains all 17×17 chunk squares around each player.
// Value false = inner (eligible); value true = border (population tracking only).

totalSpawned = 0
spawnPoint = world.getSpawnPoint()

// Step 2: For each creature type, check cap and spawn
for each jf type in [jf.a, jf.b, jf.c]:
    isPassive = type.d()
    if NOT spawnPassive AND isPassive: continue
    if NOT spawnHostile AND NOT isPassive: continue

    // Population cap scales with number of loaded eligible+border chunks
    currentCount = world.countEntitiesOfType(type.a())   // world.b(jf.a())
    cap = type.b() * b.size() / 256

    if currentCount > cap: continue    // already at or above cap

    for each coord (acm) in b where b.get(coord) == false:    // inner chunks only
        // Pick random position within this chunk
        spawnX = coord.x * 16 + world.random.nextInt(16)
        spawnY = world.random.nextInt(world.height)    // world.c = height
        spawnZ = coord.z * 16 + world.random.nextInt(16)

        // Check basic validity at starting position
        if world.isSolidBlock(spawnX, spawnY, spawnZ): continue
        if world.getMaterial(spawnX, spawnY, spawnZ) != type.c(): continue

        // Attempt 3 packs at this anchor
        for pack in 0..2:
            px = spawnX
            py = spawnY
            pz = spawnZ
            spreadRange = 6
            group = null
            packCount = 0

            for packAttempt in 0..3:
                // Random walk from current position
                px += world.random.nextInt(spreadRange) - world.random.nextInt(spreadRange)
                py += world.random.nextInt(1) - world.random.nextInt(1)    // ±1 Y
                pz += world.random.nextInt(spreadRange) - world.random.nextInt(spreadRange)

                if isValidSpawnPosition(type, world, px, py, pz):
                    cx = px + 0.5F
                    cy = (float)py
                    cz = pz + 0.5F

                    // Must be >24 blocks from all players (576 = 24²)
                    distSq = (cx - spawnPoint.x)² + (cy - spawnPoint.y)² + (cz - spawnPoint.z)²
                    if nearest player within 24 blocks (world.a(cx, cy, cz, 24) != null): continue
                    if distSq < 576.0F: continue    // too close to spawn point

                    // Lazy-init: get spawn group for this biome position
                    if group == null:
                        group = world.getSpawnableList(type, px, py, pz)  // world.a(jf, x, y, z)
                        if group == null: break

                    // Instantiate mob via reflection
                    entry = pickRandom(group)
                    mob = entry.class.getConstructor(World.class).newInstance(world)
                    mob.setLocationAndAngles(cx, cy, cz, world.random * 360, 0)

                    if mob.getCanSpawnHere():
                        packCount++
                        world.addEntity(mob)
                        postSpawnSetup(mob, world, cx, cy, cz)
                        if packCount >= mob.getMaxSpawnedInChunk():
                            continue to next coord
                    totalSpawned += packCount
```

**`world.a(cx, cy, cz, 24)`** = find nearest entity within 24 blocks radius. The spawn is
rejected if any player is within 24 blocks.

**`world.getSpawnableList(jf, x, y, z)`** = `world.a(jf, x, y, z)` — returns the biome's
spawn list for the given creature type. This is delegated to the chunk provider.

### `a(jf type, ry world, int x, int y, int z)` — isValidSpawnPosition (private static)

Checks if position (x, y, z) is a valid spawn location for the given creature type:

```
if type.c() == p.g (water creature):
    return material(x, y, z).isLiquid()          // must be in liquid
       AND NOT world.isSolidBlock(x, y+1, z)      // must have air above

else (land creature):
    return world.isSolidBlock(x, y-1, z)          // must have solid floor
       AND NOT world.isSolidBlock(x, y, z)         // must not be inside solid
       AND NOT material(x, y, z).isLiquid()        // must not be in water
       AND NOT world.isSolidBlock(x, y+1, z)       // must have air above (2-block space)
```

### `a(nq entity, ry world, float x, float y, float z)` — postSpawnSetup (private static)

Performs special post-spawn configuration for certain entity types:

```
if entity instanceof vq (Spider) AND world.random.nextInt(100) == 0:
    // 1% chance: Spider Jockey — spawn a Skeleton riding the Spider
    skeleton = new EntitySkeleton(world)
    skeleton.setLocationAndAngles(x, y, z, entity.yaw, 0)
    world.addEntity(skeleton)
    skeleton.setMountedEntity(entity)   // var5.g(var0) = sets Spider as mount

else if entity instanceof hm (Sheep):
    entity.setFleeceColor(Sheep.getRandomFleeceColor(world.random))  // hm.a(world.w)
```

### `a(ry world, sr biome, int x, int z, int width, int depth, Random rng)` — initialPopulate

Called during chunk population to place initial passive animals. Uses the biome's passive
spawn list (`jf.b` entries) and a probability-based repeat loop.

```
spawnList = biome.getSpawnList(jf.b)   // biome.a(jf.b)
if spawnList.isEmpty(): return

while rng.nextFloat() < biome.spawnChance:    // biome.d()
    entry = pickRandom(world.random, spawnList)   // nc.a(world.w, list)
    count = entry.minCount + rng.nextInt(1 + entry.maxCount - entry.minCount)
    // count in [entry.b, entry.c]

    startX = x + rng.nextInt(width)    // anchor within chunk
    startZ = z + rng.nextInt(depth)
    anchorX = startX
    anchorZ = startZ

    for i in 0..count-1:
        placed = false
        for attempt in 0..3:
            surfaceY = world.getTopSolidOrLiquidBlock(spawnX, spawnZ)   // world.f(x, z)
            if isValidSpawnPosition(jf.b, world, spawnX, surfaceY, spawnZ):
                mob = entry.class.getConstructor(World.class).newInstance(world)
                mob.setLocationAndAngles(spawnX + 0.5, surfaceY, spawnZ + 0.5,
                                         rng.nextFloat() * 360, 0)
                world.addEntity(mob)
                postSpawnSetup(mob, world, ...)
                placed = true

            // Random walk within [x, x+width) × [z, z+depth)
            spawnX += rng.nextInt(5) - rng.nextInt(5)
            spawnZ += rng.nextInt(5) - rng.nextInt(5)
            while spawnX < x OR spawnX >= x + width OR spawnZ < z OR spawnZ >= z + depth:
                spawnX = anchorX + rng.nextInt(5) - rng.nextInt(5)
                spawnZ = anchorZ + rng.nextInt(5) - rng.nextInt(5)
```

`biome.spawnChance` = `sr.d()` — the probability that the while loop iterates again (spawn
density per chunk). Typical value is around 0.1 (10%) for most biomes.

---

## 5. Constants & Magic Numbers

| Value | Meaning |
|---|---|
| `radius = 8` | Chunk scan radius per player: 8 chunks → 17×17 = 289 total chunks in map |
| `cap = type.b() * b.size() / 256` | Population cap scales: at 256 loaded chunks = base cap; more chunks = higher allowed population |
| `24.0F` (= sqrt(576)) | Minimum distance in blocks from nearest player for a spawn to occur |
| `spreadRange = 6` | Pack spawn scatter radius in blocks |
| `packAttempts = 4` | Maximum position attempts per pack member |
| `packs = 3` | Maximum pack groups attempted per anchor position |
| `4` | Fallback attempts in `initialPopulate` before skipping a mob in the group |

---

## 6. Known Quirks / Bugs to Preserve

1. **Spawn point distance check uses spawn point, not nearest player:** The condition
   `distSq < 576.0F` compares against `world.v()` (spawn point — a fixed `dh` coordinate),
   NOT against each player's position. The `world.a(cx, cy, cz, 24)` call separately checks
   for nearby players. A mob may still spawn within 24 blocks of the spawn point if a player
   is elsewhere — but the two checks are independent.

2. **Cap scales with chunk count including border chunks:** `b.size()` includes both inner
   (eligible) and border (tracking-only) chunks. With 1 player, that is 17×17 = 289 chunks.
   The nominal cap of 70 hostile mobs applies at 256 chunks, so at 289 chunks the actual cap
   is 70 * 289 / 256 ≈ 79.

3. **`world.random` is used directly in tickSpawn:** `a()` method uses `world.w.nextInt(16)`
   directly (not the passed-in Random). This affects the world RNG state.

4. **Spider Jockey 1% per Spider spawn:** The check fires for every Spider, not just on the
   first spawn attempt. In pack spawns, each Spider individually rolls 1%.

5. **`initialPopulate` uses `world.f(x, z)` for surface Y:** This returns the topmost
   solid-or-liquid block Y. Animals placed here may land on water if the column is mostly water.

---

## 7. Open Questions

1. **`dn` class (water creature):** The water creature base class `dn` is used in `jf.c`.
   Likely `EntitySquid`. The spawn list for water creatures in each biome needs to be
   verified from the biome spec.

2. **`aey` class (hostile base):** The hostile base class `aey` is used in `jf.a` for
   entity counting. Likely `EntityMob` or an equivalent abstract class above `zo`.

3. **`biome.d()` spawn probability:** Each `sr` (BiomeGenBase) subclass may override the
   spawn probability field. The exact values per biome should be confirmed from the
   BiomeGenBase spec.
