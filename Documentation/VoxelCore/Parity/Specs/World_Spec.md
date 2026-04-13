# World Spec
Source class: `ry.java`
Type: `class` — implements `kq` (IBlockAccess)
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`World` is the root game object. It owns all chunks, entities, tile entities, and the tick
schedule. It implements `IBlockAccess` (`kq`) so Block rendering and physics methods can
query world state through the abstract interface. The 20 Hz game loop calls `c()` (tick)
and `m()` (tickEntities) each step.

---

## 2. Constants and Computed Fields

Declared as **instance fields** (not static), but functionally constant for a given world:

| Field | Value | Semantics |
|---|---|---|
| `a` | `7` | Height bits: log₂(worldHeight) |
| `b` | `a + 4 = 11` | X/Z shift in Chunk block array |
| `c` | `1 << a = 128` | World height (blocks) |
| `d` | `c − 1 = 127` | Height mask |
| `e` | `c / 2 − 1 = 63` | Mid-world Y for spawn search |

---

## 3. Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `f` | `boolean` | `false` | Unknown server/client routing flag (used in tick scheduler) |
| `g` | `List<ia>` | ArrayList | All loaded entities |
| `J` | `List<ia>` | ArrayList (private) | Entity removal queue — processed at start of `m()` |
| `K` | `TreeSet<ahn>` | TreeSet (private) | Scheduled block updates sorted by fire-time |
| `L` | `Set<ahn>` | HashSet (private) | Scheduled block update uniqueness set (parallel to K) |
| `h` | `List<bq>` | ArrayList | Active tile entities |
| `M` | `List<bq>` | ArrayList (private) | Pending tile entity additions (during tick guard) |
| `N` | `List<bq>` | ArrayList (private) | Pending tile entity removals |
| `i` | `List<vi>` | ArrayList | Players (vi = PlayerEntity) |
| `j` | `List<ia>` | ArrayList | Global entities — ticked before regular entities, never chunk-unloaded |
| `O` | `long` | `16777215L` | Fog/sky base colour (0xFFFFFF = white) |
| `k` | `int` | `0` | Current sky brightness (0–11) |
| `l` | `int` | `new Random().nextInt()` | LCG state for per-chunk random ticks |
| `m` | `int` (final) | `1013904223` | LCG addend (same constant as Java's java.util.Random) |
| `n` | `float` | `0.0` | Previous-tick rain strength (interpolation source) |
| `o` | `float` | `0.0` | Current rain strength (target, ±0.01 per tick) |
| `p` | `float` | `0.0` | Previous-tick thunder strength |
| `q` | `float` | `0.0` | Current thunder strength |
| `r` | `int` | `0` | Thunder flash timer (ticks remaining) |
| `s` | `int` | `0` | Thunder flash visible count |
| `t` | `boolean` | `false` | Debug flag (unknown purpose) |
| `u` | `int` | `40` | Auto-save interval in ticks |
| `v` | `int` | — | Difficulty (0=peaceful … 3=hard) |
| `w` | `Random` | `new Random()` | Main world random |
| `x` | `boolean` | `false` | mapFeaturesEnabled |
| `y` | `k` (final) | ctor arg | WorldProvider — controls dimension rules (sky, nether, etc.) |
| `z` | `List<bd>` | ArrayList | IWorldAccess listeners (renderer notifiers) |
| `A` | `ej` | from `d()` | ChunkLoader — loads/saves/caches chunks |
| `B` | `nh` (final) | ctor arg | WorldInfo — persisted world metadata |
| `C` | `si` | from `B` or new | LevelData — difficulty, time, spawn coords, weather state |
| `D` | `boolean` | — | isSpawnSet (used during world creation) |
| `Q` | `boolean` (private) | — | allPlayersAsleep |
| `E` | `ew` | — | Scoreboard manager |
| `R` | `ArrayList` (private) | — | Reused temporary list for bounding-box queries |
| `S` | `boolean` (private) | — | isTileEntityTicking guard (prevents concurrent-modification) |
| `F` | `boolean` (protected) | `true` | spawnHostileMobs |
| `G` | `boolean` (protected) | `true` | spawnPeacefulMobs |
| `T` | `Set<acm>` (private) | HashSet | Chunk coords for current tick pass (player proximity) |
| `U` | `int` (private) | random 0–12000 | Ambient cave-sound cooldown |
| `H` | `int[32768]` | — | Light propagation BFS queue (packed int entries) |
| `V` | `List` (private) | ArrayList | Reused temporary list for entity queries |
| `I` | `boolean` | `false` | isClientSide (server = false, client = true) |

Key type mappings: `ia` = Entity, `vi` = PlayerEntity, `bq` = TileEntity, `ba` = TileEntityBlock interface,
`ahn` = TickNextTickEntry, `acm` = ChunkCoordIntPair, `bd` = IWorldAccess, `k` = WorldProvider,
`bn` = LightType (sky/block enum), `ej` = ChunkLoader, `nh` = WorldInfo, `si` = LevelData.

---

## 4. Constructors

Three meaningful constructors. All call `y.a(this)` (WorldProvider.registerWorld), then
`A = d()` (creates ChunkLoader), then `p()` (computeSkyBrightness), then `H()` (applyWeatherState).

The primary constructor used by the server:
```java
ry(nh worldInfo, String saveName, k worldProvider, yw settings)
```
- `C = new si(settings, saveName)` — creates fresh LevelData

The client constructor (copies existing LevelData):
```java
ry(nh worldInfo, String saveName, yw settings, k worldProvider)
```
- `C = worldInfo.c()` — retrieves persisted LevelData; if null, creates new

---

## 5. IBlockAccess Implementation

All 12 interface methods are implemented (`@Override` in the source):

### `kq.a()` → `vh` (getWorldChunkManager)

```
return y.b
```
Returns `WorldProvider.worldChunkMgr` — a `vh` (WorldChunkManager).

### `kq.b()` → `int` (getHeight)

```
return c  // = 128
```

### `kq.a(x,y,z)` → `int` (getBlockId)

```
if x < -30M or x >= 30M or z < -30M or z >= 30M: return 0
if y < 0: return 0
if y >= c: return 0
return chunk(x>>4, z>>4).a(x&15, y, z&15)
```

### `kq.b(x,y,z)` → `bq` (getTileEntity)

Delegates to `chunk.d(x&15, y, z&15)`. Also checks pending list `M` as fallback.

### `kq.a(x,y,z,emissionHint)` → `int` (getLightValue)

Queries combined light. For transparent blocks in `Block.s[]`, takes max of 6 neighbours.

### `kq.b(x,y,z,emissionHint)` → `float` (getBrightness)

Uses `WorldProvider.f[]` (sky-light to brightness float lookup table).

### `kq.c(x,y,z)` → `float` (unknown — combined brightness)

Also uses `WorldProvider.f[]` lookup for combined light value.

### `kq.d(x,y,z)` → `int` (getBlockMetadata)

```
if out-of-bounds: return 0
return chunk(x>>4, z>>4).b(x&15, y, z&15)
```

### `kq.e(x,y,z)` → `p` (getBlockMaterial)

```
id = getBlockId(x,y,z)
return id == 0 ? p.a : Block.k[id].bZ
```

### `kq.f(x,y,z)` → `boolean` (isOpaqueCube)

```
block = Block.k[getBlockId(x,y,z)]
return block == null ? false : block.a()
```

### `kq.g(x,y,z)` → `boolean` (isWet)

```
block = Block.k[getBlockId(x,y,z)]
return block == null ? false : block.bZ.j() && block.b()
```

`Material.j()` = canBePushedBy/canBurn, `Block.b()` = isWet override.

### `kq.h(x,y,z)` → `boolean` (unknown — isEmpty)

```
return getBlockId(x,y,z) == 0
```

---

## 6. Block Write Methods

### setBlockAndMetadata (primary) — `b(int x, int y, int z, int blockId, int meta)` → `boolean`

Full bounds check (±30M XZ, 0 ≤ y < c). Delegates to `chunk.a(x&15, y, z&15, blockId, meta)`.
After delegate returns, calls `s(x, y, z)` to trigger light update + neighbour notify.

### setBlock (no meta, with notify) — `d(int x, int y, int z, int blockId)` → `boolean`

Delegates to `chunk.a(x&15, y, z&15, blockId)` (4-arg, clears meta to 0).
After delegate, calls `s(x, y, z)`.

### setBlockWithoutNeighborUpdates — `g(int x, int y, int z, int blockId, int meta)` → `boolean`

Alias. Calls `a(null, x, y, z, blockId, meta)` which is the same path as `b(x,y,z,id,meta)`.

### setMetadata — `c(int x, int y, int z, int meta)` → `boolean`

Bounds check, delegates to `chunk.b(x&15, y, z&15, meta)`.
If true: dispatches neighbour change (if `Block.r[id]`: `h(x,y,z,id)`; else `j(x,y,z,id)`).

### setMetadata with notify — `g(int x, int y, int z, int meta)` → `boolean`

Calls `d(x,y,z,meta)` then on success calls `h(x,y,z, meta)`.

`s(x,y,z)` = propagateLight + notifyBlockChange; `j(x,y,z,id)` = notifyNeighboursOfChange;
`h(x,y,z,id)` = notifyBlockChange + notifyNeighboursOfChange.

---

## 7. Lighting

### propagateLightColumnRange — `i(int x, int z, int yMin, int yMax)`

Called when height map changes (block placed/removed). Propagates sky-light through the
Y range: calls `c(bn.a, x, y, z)` for each Y in range (if not nether). Then notifies
renderers of changed region.

### propagateLight — `c(bn type, int x, int y, int z)`

BFS flood-fill light propagation using the `H[]` int queue. Queue entries pack coords
(offset from origin by +32, 6 bits each) and light value (4 bits) into a single int:
```
entry = (dx+32) | (dy+32)<<6 | (dz+32)<<12 | lightValue<<18
```

Radius limit: Manhattan distance from origin ≤ 17. Calls `e(x, y, z, 17)` (isAreaLoaded)
before starting; skips if area not loaded.

For sky-light (`bn.a`): uses private `a(currentLight, x,y,z, blockId, opacity)` to compute
expected light — returns 15 if sky-visible, else max of 6 neighbours minus opacity.

For block-light (`bn.b`): uses private `d(currentLight, x,y,z, blockId, opacity)` — takes
max of 6 neighbours minus opacity, also starts from `Block.q[id]` emission value.

### setLightValue — `a(bn type, int x, int y, int z, int value)`

Delegates to `chunk.a(type, x&15, y, z&15, value)`.

### getLightBrightness — `b(bn type, int x, int y, int z)` → `int`

Delegates to `chunk.a(type, x&15, y, z&15)`.

### getLightSubtractedBrightness — `m(int x, int y, int z)` → `int`

Returns `chunk.c(x&15, y, z&15, 0)` — raw combined light.

---

## 8. Raytrace

### rayTrace — `a(fb fromVec, fb toVec, boolean stopAtLiquid, boolean stopAtNonSolid)` → `gv`

DDA voxel traversal (up to 200 steps). Starting from `fromVec`, advances along the ray one
face-crossing at a time:

1. Compute direction `(var27, var29, var31)` = to − from
2. Each step: pick the axis with the smallest parameter to next face crossing (X, Y, or Z)
3. Assign face ID based on direction of crossing:
   - X crossing: face = 4 (−X/west) if moving +X; face = 5 (+X/east) if moving −X
   - Y crossing: face = 0 (−Y/bottom) if moving +Y; face = 1 (+Y/top) if moving −Y
   - Z crossing: face = 2 (−Z/north) if moving +Z; face = 3 (+Z/south) if moving −Z
4. Step the ray; create Vec3 `var34` at new position using `fb.b(x,y,z)` from pool
5. Off-by-one correction:
   - face = 5 (east, entering from −X): `blockX--`, `var34.a++`
   - face = 1 (top, entering from −Y): `blockY--`, `var34.b++`
   - face = 3 (south, entering from −Z): `blockZ--`, `var34.c++`
6. Look up block ID and metadata, check `Block.a(meta, stopAtNonSolid)` = canRayPassThrough
7. If block passes test: call `Block.a(world, blockX, blockY, blockZ, fromVec, toVec)` → `gv`
8. Return first non-null hit, or null after 200 steps

---

## 9. Entity Management

### spawnEntityInWorld — `a(ia entity)` → `boolean`

```
chunkX = floor(entity.s / 16)
chunkZ = floor(entity.u / 16)
isPlayer = entity instanceof vi
if !isPlayer && !isChunkLoaded(chunkX, chunkZ): return false
if isPlayer: add to i (player list), call A() (updateAllPlayersAsleep)
chunk.a(entity)  // add to chunk entity bucket
g.add(entity)    // add to world entity list
c(entity)        // notify IWorldAccess listeners
return true
```

### removeEntityFromWorld — `b(ia entity)`

Disconnects mount/rider links. Calls `entity.v()` (mark dead). Removes from player list `i`.

### tickEntityWithPartialTick — `f(ia entity)`

Calls `a(entity, true)` — saves old position, calls `entity.a()` (tick), updates chunk
assignment if entity moved to new chunk.

### markEntityForRemoval

Entities mark themselves as dead (`K = true`). The removal queue `J` is flushed at the
start of `m()`.

---

## 10. Tick Loop

### tickEntities — `m()`

Processes per tick (called separately from `c()`):

1. Tick **global entities** (`j` list): `entity.a()`, remove if dead
2. Flush entity removal queue `J`: remove from `g`, remove from chunk, fire listener `d(entity)`
3. Tick **regular entities** (`g` list): call `f(entity)`, remove dead ones
4. Tick **tile entities** (`h` list): call `te.b()` if loaded; remove invalidated ones
5. Process **pending tile entity additions** (`M`): add to `h` and register with chunk
6. Process **pending tile entity removals** (`N`)

Guard flag `S` is set `true` during step 4; additions during that window go to `M` instead
of `h`.

### mainTick — `c()`

Called once per 20 Hz tick:

1. Update sleep state; check for day-skip
2. Advance WorldProvider weather (via `a().b()`)
3. Update weather (`h()`)
4. Check all-players-asleep (`C()`); if true, advance time to next morning
5. Spawn mobs (`we.a(…)`)
6. Advance ChunkLoader (`A.a()`)
7. Update sky brightness
8. Auto-save every `u = 40` ticks
9. Advance world time (`C.a(worldTime + 1)`)
10. Process scheduled block updates (`a(false)`)
11. Tick chunks (`f()`)

### tickChunks — `f()`

Per-player-proximity chunk tick:

1. Build chunk-coord set `T`: radius 7 around each player
2. For each chunk in `T`:
   - Call `chunk.j()` (updateSkylight)
   - Ambient cave sound (if cooldown `U == 0`)
   - Thunder spawn (probability 1/100 000)
   - Ice/snow formation
   - Light spot check
   - **20 random block ticks** per chunk:
     - LCG: `l = l * 3 + 1013904223`
     - Block coords: `x = (l>>2) & 15`, `z = (l>>10) & 15`, `y = (l>>18) & 127`
     - If `Block.l[id]` (needsRandomTick): call `Block.a(world, x, y, z, random)` = blockTick

### processScheduledTicks — `a(boolean force)` → `boolean`

Processes up to 1000 entries from `K` (sorted by fire-time). For each:
- Skip if not yet due (unless `force = true`)
- Check area loaded (radius 8)
- If block still matches ID: call `Block.a(world, x, y, z, random)` = blockTick
Returns `true` if scheduled ticks remain.

### scheduleBlockUpdate — `a(int x, int y, int z, int blockId, int delay)`

Creates `ahn(x, y, z, blockId)`. If server (`f == true`):  
checks player proximity (radius 8) before adding. If not server: always adds.
Adds to `L` and `K` only if not already present. Delay added to current `C.f()` (total time).

### scheduleBlockUpdateFromLoad — `e(int x, int y, int z, int blockId, int delay)`

Like above but skips player-proximity check (used when loading chunk data).

---

## 11. Chunk Management

### getChunkFromBlockCoords — `b(int blockX, int blockZ)` → `zx`

```
return c(blockX >> 4, blockZ >> 4)
```

### getChunkFromChunkCoords — `c(int chunkX, int chunkZ)` → `zx`

```
return A.b(chunkX, chunkZ)
```

### isChunkLoaded — `g(int chunkX, int chunkZ)` → `boolean`

```
return A.c(chunkX, chunkZ)
```

### isAreaLoaded — `e(int x, int y, int z, int radius)` → `boolean`

```
return b(x-radius, y-radius, z-radius, x+radius, y+radius, z+radius)
```

### isAreaLoadedByBox — `b(int x0, int y0, int z0, int x1, int y1, int z1)` → `boolean`

Converts to chunk coords, checks all overlapping chunks are loaded.
Returns `false` if Y range entirely out of bounds.

---

## 12. Height and Geometry Queries

### getTopBlock — `a(int x, int z)` → `int`

Returns block ID at `(x, world.e + 1, z)` searching upward until `h(x, y, z)` (isEmpty) is false. Used during spawn search.

### getTopSolidOrLiquidBlock — `e(int x, int z)` → `int`

```
return chunk(x>>4, z>>4).c(x&15, z&15)  // precipitationHeightAt
```

### getHighestNonEmptyBlockY — `f(int x, int z)` → `int`

Searches from `world.c − 1` downward. Returns Y+1 of first block where
`Block.k[id].bZ.d() && Block.k[id].bZ != p.i` (solid movement + not leaves material).
Returns `−1` if none found.

### isBlockAboveGroundLevel — `l(int x, int y, int z)` → `boolean`

```
return chunk.c(x&15, y, z&15)  // isAboveHeightMap
```

### canSnowAt — `c(int x, int y, int z, boolean checkNeighbours)` → `boolean`

Checks temperature (`WorldChunkMgr.a() > 0.15F` = too warm → false). Then checks:
- y ∈ [0, c), sky-light ≥ 10
- Block at (x, y, z) is still/flowing water (`yy.B` or `yy.A`) with meta 0
- If `checkNeighbours`: all 4 horizontal neighbours must also be water (`p.g`)

### canPlaceSnowLayerAt — `r(int x, int y, int z)` → `boolean`

Temperature check; block at (x,y) is air; block at (x,y−1) is non-air, non-snow, solid.

---

## 13. Collision and Physics Queries

### getCollidingBoundingBoxes — `a(ia entity, c box)` → `List`

Iterates all blocks in the AABB region, calls `Block.a(world, x, y, z, box, list)` for each
non-null block. Also queries entity AABBs via `b(entity, expandedBox)`.

### getEntitiesWithinAABBExcludingEntity — `b(ia exclude, c box)` → `List`

Gathers entities from chunks overlapping the box (chunks rounded out by 2 blocks).

### getEntitiesOfType — `a(Class type, c box)` → `List`

Like above, filtered by class.

### handleMaterialAcceleration — `a(c box, p material, ia entity)` → `boolean`

For each matching-material block overlapping the box: if entity is deep enough, applies
push velocity from `Block.a(world, x, y, z, entity, velocity)`.

### isLiquidInBox — `b(c box)` → `boolean`

True if any block in the box has `Block.bZ.a() == true` (isLiquid).

### isMaterialInBox — `a(c box, p material)` → `boolean`

True if any block matches the material.

### canBlockSeeSky — `l(int x, int y, int z)` → `boolean` (overload)

Takes 4 args `l(x,y,z,face)`. If chunk loaded: calls `u(x,y,z)` = checks 6-face redstone power.
Else: queries `Block.b(kq, x, y, z, face)`.

---

## 14. Raytrace Exposure Fraction

### getExposureFraction — `a(fb from, c box)` → `float`

Casts rays from `from` to a uniform grid of points on the surface of `box` (step sizes
1/width etc., inclusive of corners). Returns fraction of rays that reach the box without
hitting an opaque block.

---

## 15. Time and Weather

### getWorldTime — `t()` → `long`

```
return C.b()
```

### getTotalWorldTime — `u()` → `long`

```
return C.f()
```

### getSpawnPoint — `v()` → `dh`

```
return new dh(C.c(), C.d(), C.e())
```

### isRaining — `E()` → `boolean`

```
return j(1.0F) > 0.2F  // rainStrength > 0.2
```

### isThundering — `D()` → `boolean`

```
return i(1.0F) > 0.9   // thunderStrength > 0.9
```

### getRainStrength (interpolated) — `j(float partialTick)` → `float`

```
return n + (o - n) * partialTick
```

### getThunderStrength (interpolated) — `i(float partialTick)` → `float`

```
return (p + (q - p) * partialTick) * j(partialTick)
```

---

## 16. Deterministic Block Random

### getBlockRandom — `x(int x, int y, int z)` → `Random`

Re-seeds world random `w` with a position-dependent value:

```
seed = (long)x * 341873128712L + (long)y * 132897987541L + worldSeed + (long)z
w.setSeed(seed)
return w
```

Note: returns the shared `w` instance — not thread-safe and not re-entrant.

---

## 17. Known Quirks / Bugs to Preserve

| # | Location | Quirk |
|---|---|---|
| 1 | `getBlockRandom` | Returns the shared `w` Random — calling it twice in a row gives the second call a different seed. Callers must use the returned reference immediately. |
| 2 | Random tick LCG | `l = l * 3 + 1013904223` (multiplier 3, addend 1013904223). This is NOT Java's standard LCG (which uses multiplier 0x5DEECE66DL). |
| 3 | `setBlock` → `s(x,y,z)` | `s()` calls both `propagateLight(sky)` and `propagateLight(block)`. This means every block placement triggers two BFS passes even if the block is non-emitting and non-opaque. |
| 4 | Off-by-one in ray-trace | After stepping the ray, face IDs 1 (top), 3 (south), and 5 (east) require `blockCoord--` and `hitVec += 1.0`. Faces 0, 2, 4 do not. |
| 5 | `h(x,y,z)` IBlockAccess method | Returns `getBlockId(x,y,z) == 0` — i.e., is-air. NOT "is-air-or-leaves" or any other interpretation. |

---

## 18. Open Questions

1. **`bn` enum spec needed** — LightType. `bn.a` = sky, `bn.b` = block. Full enum class needed.

2. **`k` (WorldProvider) spec needed** — supplies `y.e` (isNether), `y.b` (WorldChunkManager),
   sky-to-brightness float table, sun angle, sky/fog colour computations.

3. **`nh` (WorldInfo) and `si` (LevelData) specs needed** — WorldInfo wraps persisted game data.
   LevelData exposes: `f()` = totalWorldTime, `b()` = worldTime, `c/d/e()` = spawnX/Y/Z,
   `m()` = isThundering, `n()` = thunderTimer, `o()` = isRaining, `p()` = rainTimer,
   `i()` = generatorType.

4. **`ej` (ChunkLoader) interface spec needed** — `b(chunkX, chunkZ)` → zx (load/get),
   `c(chunkX, chunkZ)` → bool (isLoaded), `a()` = tick, `a(bool, rz)` = save.

5. **`bd` (IWorldAccess) spec needed** — notification interface for renderers:
   `a(entity)` = onEntityAdded, `b(entity)` = onEntityRemoved, `a(x,y,z)` = onBlockChanged,
   `a(x,y,z,x,y,z)` = onBlockRangeChanged, etc.

---

*Spec written by Analyst AI from `ry.java` (2788 lines). No C# implementation consulted.*
