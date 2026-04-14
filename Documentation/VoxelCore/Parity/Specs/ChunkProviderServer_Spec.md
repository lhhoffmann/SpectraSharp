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

# ChunkProviderServer Spec
**Source class:** `jz.java` (213 lines)
**Interface:** `ej` (= `IChunkProvider`)
**Superclass:** none (implements `ej`)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** DRAFT
**License:** [CC BY 4.0](../../../LICENSE.md)

Additional sources: `ej.java` (interface, 23 lines), `acm.java` (ChunkCoordIntPair, 37 lines),
`wv.java` (LongHashMap, ~130 lines), `hn.java` (EmptyChunk, partial), `zx.java` (chunk fields/methods)

---

## 1. Purpose

`jz` (ChunkProviderServer) is the central chunk manager for server-side world access. It:
- Maintains an in-memory LRU cache (`wv`) mapping long chunk keys → `zx` chunk objects.
- Loads chunks from disk via `d` (IChunkLoader / `gy` ChunkLoader).
- Falls back to terrain generation via an inner `ej` (terrain generator / `xj` ChunkProviderGenerate).
- Triggers chunk population (decoration) when a 2×2 block of chunks is all generated.
- Tracks which chunks are far from any player and queues them for unloading.
- Saves dirty chunks on every tick (up to 24 per normal tick, unlimited on save-all).

---

## 2. Class Identifier — Interface vs. Implementation

> **The REQUESTS.md named `ej` as the class. Correction: `ej` is the `IChunkProvider` interface.
> The concrete server-side implementation is `jz`.**

| Obfuscated | Human name | Role |
|---|---|---|
| `ej` | `IChunkProvider` | Interface; 10 method signatures |
| `jz` | `ChunkProviderServer` | Concrete server-side implementation |
| `xj` | `ChunkProviderGenerate` | Terrain generator; stored as `jz.c` |
| `gy` | `ChunkLoader` / `DiskChunkLoader` | Disk persistence; stored as `jz.d` |
| `wv` | `LongHashMap` | Custom hash map with `long` keys |
| `acm` | `ChunkCoordIntPair` | Chunk coordinate pair + long key formula |
| `hn` | `EmptyChunk` | Extends `zx`; all-air, never saves; used for out-of-bounds |

---

## 3. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `Set<Long>` | `new HashSet<>()` | Chunk keys queued for unload (distance > 128 blocks from all players) |
| `b` | `zx` | `new hn(world, byte[256*worldHeight], 0, 0)` | Sentinel empty chunk; returned for out-of-bounds requests |
| `c` | `ej` | terrain generator (`xj`) passed at construction | Terrain generator; called for raw chunk generation and population |
| `d` | `d` | disk loader (`gy`) passed at construction | Disk persistence; may be null for generator-only worlds |
| `e` | `wv` | `new wv()` | LongHashMap cache: long key → zx chunk |
| `f` | `List<zx>` | `new ArrayList<>()` | All currently loaded chunks (for iteration, save loop, unload check) |
| `g` | `ry` | world passed at construction | World reference |
| `h` | `int` | 0 | Rolling cursor into `f` for the per-tick player-distance sweep |

### Empty chunk sentinel (`b`)
Constructed as `new hn(world, new byte[256 * world.c], 0, 0)` where `world.c` = world height = 128.
The byte array is 32 768 bytes, all zero (all air). `hn` sets `chunk.r = true`, preventing it from
ever being saved.

---

## 4. Constants and Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `1875004` | `a(x,z)` bounds check | Maximum valid chunk coordinate (exclusive). World extends from `−1875004` to `+1875003` in both X and Z. Requests outside this range return the empty sentinel. |
| `128` | `d(x,z)` unload threshold | Half-width of the keep-loaded square in blocks. If a chunk's centre is more than 128 blocks from the player in X or Z, it is queued for unload. |
| `288.0` | `a()` player-distance check | Radius in blocks for `world.getClosestPlayer(x,y,z,radius)`. If no player is within 288 blocks of a chunk's centre (at y=64), the chunk is queued for unload. |
| `100` | `a()` unload loop | Maximum chunks unloaded per call of `a()`. |
| `10` | `a()` distance-sweep loop | Number of chunks inspected per call of `a()` for the player-distance test. |
| `24` | `a(boolean,rz)` save throttle | Maximum dirty chunks saved per call when `saveAll = false`. |
| `600L` | `zx.a(boolean)` save interval | Ticks between re-saves of entity-containing chunks in normal mode (30 seconds at 20 Hz). |

---

## 5. Chunk Key Formula

```
key = (long)x & 0xFFFF_FFFF | ((long)z & 0xFFFF_FFFF) << 32
```

`acm.a(int x, int z)` implements this. The low 32 bits encode X, the high 32 bits encode Z.
Both values are unsigned 32-bit (sign-extended ints become their two's-complement bit patterns).

---

## 6. IChunkProvider Interface (`ej`) Method Signatures

| Method | Signature | Purpose |
|---|---|---|
| `c` | `boolean c(int x, int z)` | IsChunkLoaded — cache-only check |
| `b` | `zx b(int x, int z)` | GetOrCreate — ensure loaded (load or generate) |
| `a` | `zx a(int x, int z)` | GetOrCreate with unload-queue cancel and event firing |
| `a` | `void a(ej provider, int x, int z)` | Populate chunk (run decoration) |
| `a` | `boolean a(boolean saveAll, rz listener)` | Save dirty chunks; returns false if throttled |
| `a` | `boolean a()` | Tick: unload queued, check distances, flush disk, tick generator |
| `b` | `boolean b()` | CanSave — always true in `jz` |
| `c` | `String c()` | Debug string |
| `a` | `List a(jf spawnable, int x, int z, int count)` | Get possible creature spawns; delegates to generator |
| `a` | `am a(ry world, String structure, int x, int y, int z)` | Find closest structure; delegates to generator |

---

## 7. Methods — Detailed Logic

### 7.1 `c(int x, int z)` — IsChunkLoaded

```
return cache.containsKey(acm.a(x, z))
```

Simple cache presence check. Does not trigger any load or generation.

---

### 7.2 `a(int x, int z)` — GetOrCreateChunk (primary API)

Full load-or-generate path. Called whenever the engine needs a chunk.

```
key = acm.a(x, z)
unloadQueue.remove(key)         // cancel pending unload if present

chunk = cache.get(key)
if chunk != null:
    return chunk

// Bounds check
if x < −1875004 OR x >= 1875004 OR z < −1875004 OR z >= 1875004:
    return emptyChunk  // sentinel

// Try disk load
chunk = tryLoadFromDisk(x, z)   // calls d.a(world, x, z); null if not on disk

// Fall back to terrain generation
if chunk == null:
    if c == null:
        chunk = emptyChunk
    else:
        chunk = c.b(x, z)           // terrain generator: provideChunk (raw terrain only, no decoration)

// Register in cache and iteration list
cache.put(key, chunk)
iterationList.add(chunk)

// Fire load events
if chunk != null:
    chunk.d()       // onChunkLoad (currently empty in zx base)
    chunk.e()       // notifyWorld: registers TileEntities + entities into world lists (sets chunk.e=true)

// Trigger 2×2 population attempts
chunk.a(this, this, x, z)

return chunk
```

---

### 7.3 `b(int x, int z)` — GetOrCreateChunk (secondary)

```
chunk = cache.get(acm.a(x, z))
return chunk != null ? chunk : a(x, z)
```

Used internally (e.g., in population checks) when the caller may have already ensured the chunk
is loaded. If not cached, falls back to `a()`. Does **not** cancel unload-queue membership or
fire events separately (those happen inside `a()`).

---

### 7.4 `e(int x, int z)` — TryLoadFromDisk (private)

```
if d == null: return null

try:
    chunk = d.a(world, x, z)     // ChunkLoader.loadChunk
    if chunk != null:
        chunk.t = world.u()      // set lastSaveTime = current world tick
    return chunk
catch Exception:
    printStackTrace()
    return null
```

---

### 7.5 `a(ej, int, int)` — PopulateChunk

Runs biome decoration on a chunk. Called by the chunk itself (`chunk.a(this,this,x,z)`)
after generation; also called externally.

```
chunk = b(x, z)      // ensure chunk is loaded
if !chunk.p:         // if not yet decorated
    chunk.p = true   // mark as populated
    if c != null:
        c.a(this, x, z)   // generator.populate: runs ore/tree/flower/mob-spawner passes
    chunk.g()             // markDirty (chunk.q = true) — needs saving after decoration
```

---

### 7.6 `chunk.a(ej provider1, ej provider2, int x, int z)` — Deferred Population Trigger

Called on every newly loaded/generated chunk. Attempts to populate the newly loaded chunk
and up to 3 of its SW neighbours, if their required neighbours are now all loaded.

A chunk at `(cx, cz)` can be populated when `(cx+1, cz)`, `(cx, cz+1)`, and `(cx+1, cz+1)`
are all present in the cache. The newly loaded chunk at `(x, z)` enables population of 4
candidate chunks — itself as well as the chunks that have the new chunk as their missing
`+X`, `+Z`, or `+X+Z` neighbour.

Population trigger rules (applied in order, all four independent):

```
1. This chunk (x, z):
   if !this.p AND c(x+1, z) AND c(x, z+1) AND c(x+1, z+1):
       provider1.a(provider2, x, z)

2. West neighbour (x-1, z):
   if c(x-1, z) AND !b(x-1,z).p AND c(x-1, z+1) AND c(x, z+1) AND c(x-1, z+1):
       provider1.a(provider2, x-1, z)

3. South neighbour (x, z-1):
   if c(x, z-1) AND !b(x,z-1).p AND c(x+1, z-1) AND c(x+1, z-1) AND c(x+1, z):
       provider1.a(provider2, x, z-1)

4. Southwest neighbour (x-1, z-1):
   if c(x-1, z-1) AND !b(x-1,z-1).p AND c(x, z-1) AND c(x-1, z):
       provider1.a(provider2, x-1, z-1)
```

**Effect:** decoration never runs on a chunk until it and the 3 chunks to its
`+X`, `+Z`, and `+X+Z` sides are all generated. This prevents trees and ores from
being placed in chunks that have not yet been terrain-generated.

---

### 7.7 `a(boolean saveAll, rz listener)` — SaveDirtyChunks

Called by the engine/server on every save tick. `saveAll=true` means flush everything
(world close, forced save). Returns `false` if throttled (more dirty chunks remain).

```
savedCount = 0

for each chunk in iterationList:
    // Entity re-save if saveAll
    if saveAll AND !chunk.r:
        a(chunk)                    // private: calls d.b(world, chunk) — secondary flush

    // Decide whether to save this chunk now
    if chunk.a(saveAll):            // zx.shouldSave(saveAll) — see §8
        b(chunk)                    // private: saves chunk to disk via d.a(world, chunk)
        chunk.q = false             // clear isDirty flag
        if ++savedCount == 24 AND !saveAll:
            return false            // throttle: stop after 24 in normal mode

// If saveAll: flush disk loader
if saveAll AND d != null:
    d.b()           // IChunkLoader.flush

return true
```

**Note:** `chunk.r = true` on the empty sentinel means it is never passed to `a(chunk)`.
In the loop, the `!chunk.r` guard prevents trying to secondary-flush the empty chunk.

---

### 7.8 `a()` — Tick (called every server tick)

Two-phase tick:
**Phase 1 — Process unload queue (up to 100 per tick):**

```
for up to 100 iterations while unloadQueue not empty:
    key = unloadQueue.iterator().next()
    chunk = cache.get(key)
    chunk.f()                   // onChunkUnload: de-registers TEs + entities from world (chunk.e = false)
    b(chunk)                    // save to disk (private)
    a(chunk)                    // secondary flush (private, calls d.b)
    unloadQueue.remove(key)
    cache.remove(key)           // cache.d(key)
    iterationList.remove(chunk)
```

**Phase 2 — Player-distance sweep (10 chunks per tick, rolling cursor `h`):**

```
for up to 10 iterations:
    if h >= iterationList.size():
        h = 0
        break
    chunk = iterationList[h++]
    centerX = chunk.l * 16 + 8
    centerZ = chunk.m * 16 + 8
    player = world.getClosestPlayer(centerX, 64.0, centerZ, 288.0)
    if player == null:                       // no player within 288 blocks
        d(chunk.l, chunk.m)                  // queue for unload (private)
```

**Phase 3 — Flush and pass-through:**

```
if d != null:
    d.a()                       // IChunkLoader.tick/flush

return c.a()                    // terrain generator's tick (always true in xj)
```

---

### 7.9 `d(int x, int z)` — QueueForUnload (private)

```
playerPos = world.v()    // dh (player position/direction struct); a=posX, c=posZ
dx = x * 16 + 8 - playerPos.a    // chunk centre X minus player X
dz = z * 16 + 8 - playerPos.c    // chunk centre Z minus player Z
threshold = 128
if |dx| > threshold OR |dz| > threshold:
    unloadQueue.add(acm.a(x, z))
```

Uses the world's cached player position (`world.v()`) rather than querying all players.
If the chunk centre is more than 128 blocks in X or Z from the player, queue for unload.

> **Note:** Phase 2 of `a()` uses `getClosestPlayer(radius=288)` to check any player;
> `d()` uses `world.v()` (single player position). In SP the two are equivalent.

---

### 7.10 `a(zx)` — SecondaryFlush (private)

```
if d == null: return
try: d.b(world, chunk)      // IChunkLoader second-pass (empty in gy)
catch: printStackTrace()
```

---

### 7.11 `b(zx)` — SaveToDisk (private)

```
if d == null: return
try:
    chunk.t = world.u()     // update lastSaveTime before writing
    d.a(world, chunk)       // ChunkLoader.saveChunk
catch IOException: printStackTrace()
```

---

## 8. `zx.a(boolean saveAll)` — ShouldSave (Chunk method)

Called from the save loop in §7.7. Returns true if this chunk needs saving now.

```
if chunk.r:             // EmptyChunk or NoSave flag
    return false

if saveAll:
    if chunk.s AND world.u() != chunk.t:    // has entities AND tick changed since last save
        return true
else:
    if chunk.s AND world.u() >= chunk.t + 600:   // has entities AND 30 seconds since last save
        return true

return chunk.q          // isDirty (blocks modified)
```

Chunk flags used here:
| Flag | Field | Meaning |
|---|---|---|
| `r` | `chunk.r` | NoSave / EmptyChunk; never serialise |
| `s` | `chunk.s` | HasEntities — set to true during chunk save if any entity has serialisable state |
| `q` | `chunk.q` | IsDirty — set by `chunk.g()` after any block change or decoration |
| `t` | `chunk.t` | LastSaveTime (world ticks); set by `jz.b(chunk)` and by disk load |

**Consequence:**
- Chunks with entities are re-saved every 30 seconds in normal operation.
- Any block change (q=true) causes a save on the next save-tick pass.
- On save-all (world close), every entity-chunk is re-saved regardless of timing.

---

## 9. Chunk Lifecycle

```
 Request chunk(x,z)
       │
       ├─ In cache? ──YES──► return cached chunk
       │
       └─ NO
           │
           ├─ Try disk (gy.load) ──found──► set lastSaveTime; cache; notify; fire populate
           │
           └─ NOT on disk
               │
               └─ Generate raw terrain (xj.provideChunk)
                       │
                       └─ cache; add to iterationList; fire load events
                               │
                               └─ Attempt 2×2 population for self + 3 SW neighbours
                                       │
                                       └─ Each populated chunk gets markDirty()

 Player moves away (> 128 blocks) ──► QueueForUnload
       │
       └─ Tick unload (up to 100/tick):
             onChunkUnload → save to disk → remove from cache + list
```

---

## 10. `b()` — CanSave

```
return true
```

Always returns true. The empty-chunk sentinel's `r=true` flag prevents it from being saved
at the individual-chunk level; there is no world-level save guard in `jz`.

---

## 11. `c()` — Debug String

```
return "ServerChunkCache: " + cache.size() + " Drop: " + unloadQueue.size()
```

---

## 12. Creature-Spawn and Structure Delegations

Both delegate entirely to the inner terrain generator `c`:

```
getPossibleCreatures(biomeType, x, z, count) → c.a(biomeType, x, z, count)
findClosestStructure(world, name, x, y, z)  → c.a(world, name, x, y, z)
```

---

## 13. LongHashMap (`wv`) API Reference

| Method | Signature | Meaning |
|---|---|---|
| `a()` | `int a()` | Number of entries in map |
| `a(long)` | `Object a(long key)` | Get value by key; null if absent |
| `b(long)` | `boolean b(long key)` | ContainsKey |
| `a(long, Object)` | `void a(long key, Object val)` | Put / overwrite |
| `d(long)` | `Object d(long key)` | Remove by key; returns old value |

Initial capacity: 16 buckets. Load factor: 0.75. Doubles on resize up to 1 073 741 824 buckets.
Hash function: spread via XOR shifts (`var ^= var >>> 20 ^ var >>> 12; return var ^ var >>> 7 ^ var >>> 4`).

---

## 14. Known Quirks / Bugs to Preserve

- **`b(x,z)` generates**: unlike many chunk-manager implementations, `b()` is not a "cache-only"
  get — it calls `a()` if the chunk is missing. The only difference from `a()` is that `b()` skips
  removing from the unload queue (it relies on `a()` to do so on the next real access).
- **Single rolling cursor `h`**: the player-distance sweep processes exactly 10 chunks per tick
  from the current position in `iterationList`, then stops. The list is NOT iterated in full
  each tick. This means a newly added chunk may not be distance-checked for many ticks.
- **Unload queue vs. secondary save**: `a(chunk)` (secondary flush, calls `d.b`) is called for
  every chunk being unloaded, even if `d.b` is a no-op in the `gy` implementation. This is a
  vestigial hook for region-file chunk sector flushing that does nothing in 1.0.
- **Empty chunk at bounds**: the sentinel `b` is at coords (0,0) but returned for any
  out-of-bounds request. No position-specific empty chunks are created.
- **Population uses both `this` as provider1 and provider2**: `chunk.a(this, this, x, z)` passes
  `jz` as both the chunk-loading provider and the populate-method receiver. This means the
  population trigger calls `jz.a(jz, x, z)` which calls `jz.b(x,z)` to get the chunk and then
  delegates to `c.a(jz, x, z)` (terrain generator's populate).
- **tmp_chunk.dat race**: chunk saves go through `gy` which writes via a single temp file.
  With multiple chunks saved per tick, each overwrites the same temp file before rename.
  This is safe only in a single-threaded environment.

---

## 15. Open Questions

- **`world.v()`**: returns a `dh` struct with fields `a` (playerX int?) and `c` (playerZ int?).
  Exact type and field layout of `dh` not verified.
- **`rz` listener parameter**: the `rz` parameter in `a(boolean, rz)` is unused in `jz` — no
  progress callbacks are fired. Its purpose (possibly a save-progress UI callback) is irrelevant
  for the core implementation.
- **`chunk.d()`**: `d()` on `zx` is an empty method in the base class. Overrides (if any) in
  `hn` or server subclasses not verified. Safe to implement as no-op.

---

*Spec written by Analyst AI from `jz.java` (213 lines), `ej.java` (23 lines), `acm.java` (37 lines),
`wv.java` (~130 lines), `hn.java` (partial), `zx.java` (selected methods). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — ChunkProviderServer)*
