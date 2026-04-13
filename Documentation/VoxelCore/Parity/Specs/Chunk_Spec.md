# Chunk Spec
Source class: `zx.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`Chunk` stores the block data, light data, metadata, height maps, tile entities, and entities
for one 16 × world.c × 16 column. In a 128-high world, each chunk holds 16 × 128 × 16 =
32 768 blocks. The `World` class (`ry`) delegates all block/light reads and writes to
individual Chunk objects obtained through its `ChunkLoader` (`ej`).

---

## 2. Fields

All coordinates below are **chunk-local** (x ∈ 0–15, y ∈ 0–127, z ∈ 0–15) unless stated.

| Field (obf) | Java type | Default / init | Semantics |
|---|---|---|---|
| `a` (static) | `boolean` | — | Static flag: any sky-light present in any chunk (set during getLightSubtracted) |
| `b` | `byte[]` | allocated in 4-arg constructor | Block ID storage: 32 768 bytes (16 × 128 × 16) |
| `c[256]` | `int[]` | filled with `−999` | Precipitation height cache. `c[z<<4|x]` = top solid-or-liquid Y+1; `−999` = stale |
| `d[256]` | `boolean[]` | `false` | Dirty XZ columns: `d[x + z*16]`. Set when height may need recalculation |
| `e` | `boolean` | `false` | isLoaded: set `true` by `e()` (onChunkLoad), `false` by `f()` (onChunkUnload) |
| `f` | `ry` | ctor arg | World reference |
| `g` | `up` | allocated in 4-arg ctor | Nibble array: block **metadata** (4 bits per block) |
| `h` | `up` | allocated in 4-arg ctor | Nibble array: **sky-light** (4 bits per block) |
| `i` | `up` | allocated in 4-arg ctor | Nibble array: **block-light** (4 bits per block) |
| `j[256]` | `byte[]` | computed | Height map: `j[z<<4|x]` = lowest Y where `Block.o[id] != 0` searching from top; stored as signed byte, read back with `& 255` |
| `k` | `int` | computed | lowestHeightInChunk: minimum of all `j[]` values (used to skip sky-light work) |
| `l` | `int` (final) | ctor arg | chunkX (chunk-grid X coordinate; block X = chunkX × 16 + localX) |
| `m` | `int` (final) | ctor arg | chunkZ (chunk-grid Z coordinate; block Z = chunkZ × 16 + localZ) |
| `n` | `Map<am, bq>` | new HashMap | TileEntity map: key = `am(localX, y, localZ)`, value = TileEntity (`bq`) |
| `o[]` | `List<ia>[]` | new ArrayList per slot | Entity bucket lists: `o[yBucket]` where `yBucket = clamp(floor(entity.t / 16), 0, o.length-1)`. Length = `world.c / 16` = 8 |
| `p` | `boolean` | `false` | isPopulated (terrain features generated) |
| `q` | `boolean` | `false` | isModified / dirty — set `true` by any write or `g()`, used by `a(bool)` |
| `r` | `boolean` | (default false) | isLightPopulated (sky-light has been computed) |
| `s` | `boolean` | `false` | hasEntities: set `true` when first entity added |
| `t` | `long` | `0L` | Last-save world-time (`ry.u()` value at last save) |
| `u` | `boolean` | `false` | (internal flag) |
| `v` | `boolean` (private) | `false` | hasDirtyColumns: set `true` whenever a column in `d[]` is marked dirty |

---

## 3. Block Array Layout

The block ID byte array `b[]` is indexed as:

```
index = (localX << world.b) | (localZ << world.a) | localY
      = (localX << 11) | (localZ << 7) | localY
```

Where `world.b = 11` and `world.a = 7` (derived from world height = 128).

The world keeps `b = a + 4 = 11` and `a = 7` as instance fields.

Reading a block ID: `b[localX << 11 | localZ << 7 | localY] & 0xFF`

---

## 4. Height Map Layout

`j[z << 4 | x]` stores the **lowest** Y where `Block.o[id] != 0` (i.e. the block at Y–1 is
the top opaque block). Specifically, it is the smallest Y such that all blocks `y ≥ j` have
`Block.o[id] == 0`. Stored as a signed byte; always read back with `& 255`.

The height map is recomputed by `b()` (without sky-light) and `c()` (with sky-light).

---

## 5. Nibble Array (`up`) Construction

All three nibble arrays are created in the 4-arg constructor:

```java
this.g = new up(blockData.length, world.a);  // metadata
this.h = new up(blockData.length, world.a);  // sky-light
this.i = new up(blockData.length, world.a);  // block-light
```

`blockData.length = 32768`, `world.a = 7`. The `up` class stores 4 bits per index in a
backing `byte[] a` of length `size >> 1 = 16384`.

---

## 6. Constructors

### `zx(ry world, int chunkX, int chunkZ)` — 3-arg (empty chunk)

1. Allocates `o[]` with `world.c / 16 = 8` ArrayList slots
2. `f = world`, `l = chunkX`, `m = chunkZ`
3. `j = new byte[256]`
4. Fills `c[]` with `−999`

### `zx(ry world, byte[] blockData, int chunkX, int chunkZ)` — 4-arg (data chunk)

1. Calls 3-arg constructor
2. `b = blockData`
3. Allocates `g`, `h`, `i` nibble arrays

---

## 7. Methods — Block Access

### getBlockId — `a(int localX, int y, int localZ)` → `int`

```
return b[localX << world.b | localZ << world.a | y] & 0xFF
```

### setBlock (with metadata) — `a(int localX, int y, int localZ, int blockId, int meta)` → `boolean`

1. Bounds: invalidates `c[z<<4|x]` if `y >= c[z<<4|x] - 1`
2. If old ID == new ID AND old meta == new meta: return `false` (no change)
3. Store new ID in `b[...]`
4. If old block was non-air:
   - Server: call `Block.d(world, worldX, y, worldZ)` = `onBlockRemoved`
   - Client AND block is `ba` (TileEntityBlock): call `world.o(worldX, y, worldZ)` = removeTileEntity
5. Store new meta in `g` nibble array
6. If not nether (`world.y.e == false`):
   - If new block is opaque (`Block.o[id] != 0`) and `y >= oldHeight`: call `g(localX, y+1, localZ)` (updateHeightMap)
   - Else if `y == oldHeight - 1`: call `g(localX, y, localZ)`
   - Propagate sky-light at block position: `world.a(bn.a, worldX, y, worldZ, worldX, y, worldZ)` (area relight)
7. Propagate block-light: `world.a(bn.b, worldX, y, worldZ, worldX, y, worldZ)`
8. Mark XZ column dirty: `d(localX, localZ)` sets `d[x + z*16] = true` and `v = true`
9. Store meta again in `g` (called twice — second time after `d()`)
10. If new block is non-air:
    - Server: call `Block.a(world, worldX, y, worldZ)` = `onBlockAdded`
    - If block instanceof `ba` (TileEntityBlock): create/update TileEntity via `ba.j_()`, call `world.a(x, y, z, te)`, then `te.n()` (validate)
11. Else if old block was `ba`: call `te.n()` (validate/invalidate)
12. Set `q = true` (dirty)
13. Return `true`

### setBlock (without metadata) — `a(int localX, int y, int localZ, int blockId)` → `boolean`

Same flow as 5-arg but always clears metadata to `0` (step 5 calls `g.a(x,y,z,0)`).
Does **not** pass meta into the removed-block test (early return checks only ID).

### getMetadata — `b(int localX, int y, int localZ)` → `int`

```
return g.a(localX, y, localZ)
```

### setMetadata — `b(int localX, int y, int localZ, int meta)` → `boolean`

Writes to `g`, updates TileEntity `h` field if block is a `ba` instance. Returns `false` if
meta unchanged.

---

## 8. Methods — Light

### getLight — `a(bn type, int localX, int y, int localZ)` → `int`

```
if type == bn.a: return h.a(localX, y, localZ)   // sky-light
if type == bn.b: return i.a(localX, y, localZ)   // block-light
else:            return 0
```

### setLight — `a(bn type, int localX, int y, int localZ, int value)`

Sky-light only written if not nether (`world.y.e == false`).

### getLightSubtracted — `c(int localX, int y, int localZ, int subtraction)` → `int`

```
skyLight = (world.y.e ? 0 : h.a(localX, y, localZ))
if skyLight > 0: Chunk.a = true
skyLight -= subtraction
blockLight = i.a(localX, y, localZ)
return max(skyLight, blockLight)
```

---

## 9. Methods — Height Map

### getHeightAt — `b(int localX, int localZ)` → `int`

```
return j[localZ << 4 | localX] & 0xFF
```

### isAboveHeightMap — `c(int localX, int y, int localZ)` → `boolean`

```
return y >= (j[localZ << 4 | localX] & 255)
```

### precipitationHeightAt — `c(int localX, int localZ)` → `int`

Returns the cached precipitation height (for rain/snow tests). Cache key `c[localX | localZ<<4]`.

If cache is `−999` (stale):
1. Start from `world.c − 1` (top of world)
2. Walk down. For each block: get Material `p`. Check `!p.d() && !p.a()`
   (`d()` = blocksMovement ≈ is-solid-for-physics, `a()` = isLiquid)
3. First block where `p.d() || p.a()`: set `c[...] = y + 1`; if none found: `c[...] = −1`

---

## 10. Methods — Entity Management

### addEntity — `a(ia entity)`

1. Validates entity is in correct chunk (prints warning otherwise)
2. Bucket = `clamp(floor(entity.t / 16), 0, o.length - 1)` via `me.c`
3. Sets `entity.ah = true`, `entity.ai = l`, `entity.aj = bucket`, `entity.ak = m`
4. Adds entity to `o[bucket]`
5. Sets `s = true`

### removeEntityOld — `b(ia entity)`

Calls `a(entity, entity.aj)` — removes from entity's recorded bucket.

### removeEntity — `a(ia entity, int oldBucket)`

Removes entity from `o[clamp(oldBucket, 0, o.length-1)]`.

### getEntitiesWithinAABB — `a(ia exclude, c box, List out)`

Iterates buckets overlapping the Y range of `box`. For each entity: adds to `out` if
`entity != exclude` and `entity.C.a(box)` (AABB intersects). Also adds riders via `entity.ab()`.

### getEntitiesOfType — `a(Class type, c box, List out)`

Like above but tests `type.isAssignableFrom(entity.getClass())`.

---

## 11. Methods — Tile Entities

### getTileEntity — `d(int localX, int y, int localZ)` → `bq`

Looks up `n` HashMap. If absent and block is `ba`, creates via `ba.j_()` and registers via
`world.a(worldX, y, worldZ, te)`. Returns `null` if tile entity is invalidated (`te.i()` = true).

### addTileEntity (from load) — `a(bq te)`

Computes local coords from `te.d/e/f`, calls 4-arg version. If chunk is loaded, also adds
to `world.h` (world tile entity list).

### addTileEntity (by local coords) — `a(int localX, int y, int localZ, bq te)`

Stores in `n` HashMap. Sets `te.c = world`, `te.d/e/f = worldX/Y/Z`. Only stores if block
at location is still a `ba` block.

### removeTileEntity — `e(int localX, int y, int localZ)`

Removes from `n`. If loaded, calls `te.l()` (invalidate).

---

## 12. Methods — Lifecycle

### onChunkLoad — `e()`

Sets `e = true`. Adds all tile entities from `n.values()` to `world.h` (world TE list).
Adds all entities from each `o[]` bucket to `world.g` (world entity list).

### onChunkUnload — `f()`

Sets `e = false`. Queues all tile entities for removal via `world.a(bq)` (add to removal list).
Queues all entity buckets for removal via `world.b(List)`.

### markDirty — `g()`

Sets `q = true`.

### generateSkylightMap (heights only) — `b()`

Recalculates `j[]` height map for all 256 XZ columns. Does NOT propagate sky-light.

### generateSkylightMap (full, with skylight) — `c()`

Recalculates `j[]` AND propagates sky-light column by column. Also marks all XZ columns
dirty (`d(x,z)`) and sets `q = true`.

### updateSkylight — `j()`

If `v` (hasDirtyColumns) and not nether: calls private `l()` which processes the `d[]`
dirty-column list. Computes new height values and re-propagates sky-light in modified range.

---

## 13. Methods — Serialisation

### needsSaving — `a(boolean forceCheck)` → `boolean`

```
if r (isLightNotPopulated): return false
if forceCheck:
    return (s && world.u() != t) || q
else:
    return (s && world.u() >= t + 600) || q
```

### getChunkRandom — `a(long seed)` → `Random`

Deterministic chunk-local random:
```
seed = worldSeed + chunkX*chunkX*4987142L + chunkX*5947611L
       + chunkZ*chunkZ*4392871L + chunkZ*389711L ^ seed
return new Random(seed)
```

### bulkBlockCopy — `a(byte[] src, int localX0, int y0, int localZ0, int localX1, int y1, int localZ1, int offset)` → `int`

Used by network chunk packets. Copies block IDs, then metadata nibble data, then block-light
nibble data, then sky-light nibble data from the byte array. Calls `b()` (height map only)
after copying block IDs.

### getChunkCoord — `k()` → `acm`

Returns `new acm(l, m)` — a ChunkCoordIntPair.

---

## 14. Bitwise Summary

| Expression | Value | Purpose |
|---|---|---|
| `world.a` | 7 | Height bits (log₂ 128) |
| `world.b` | 11 | X shift in block array (= `world.a + 4`, i.e. `7 + 4`) |
| `world.c` | 128 | World height |
| `world.d` | 127 | Height mask |
| `j[z<<4\|x]` & 0xFF | 0–128 | Height map entry |
| `o.length` | 8 | Entity bucket count (= 128/16) |

---

## 15. Known Quirks / Bugs to Preserve

| # | Location | Quirk |
|---|---|---|
| 1 | `setBlock` 5-arg, step 9 | `g.a(x,y,z, meta)` is called **twice** — once before `d(localX,localZ)` and once after. The second call is redundant but must be preserved for structural parity. |
| 2 | Height map encoding | Stored as signed `byte` but always read with `& 255`. Values above 127 would be stored negative but read correctly. In practice, world height is 128 so values ≤ 128 fit unsigned. |
| 3 | `precipitationHeightAt` returns `−1` | If no solid/liquid block is found searching from top, the cache entry is `−1`. Callers must handle this. |

---

## 16. Open Questions

1. **`up` class (`up.java`)** — nibble array. Methods `a(x,y,z)` → int (read) and
   `a(x,y,z,value)` (write) confirmed from Chunk usage, but exact nibble packing formula
   not yet confirmed from source. Expected: index = `x<<11|z<<7|y`, nibble = high or low
   4 bits of `a[index>>1]` depending on `index & 1`.

2. **`am` class** — ChunkPosition/BlockCoord used as HashMap key. Expected: holds `a=x`,
   `b=y`, `c=z`; equals/hashCode by value. Spec needed for `bq` (TileEntity) implementation.

3. **`ba` interface** — marker/interface for TileEntityBlock. Method `j_()` returns `bq`.
   Spec pending.

4. **`bn` enum** — LightType. Two constants: `bn.a` = sky-light, `bn.b` = block-light.
   Spec pending.

---

*Spec written by Analyst AI from `zx.java` (781 lines). No C# implementation consulted.*
