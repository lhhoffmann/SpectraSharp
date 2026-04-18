# Spec: Chunk Loading Radius Loop

**Java class:** `jz` (ServerChunkProvider, implements `ej`)
**Status:** PROVIDED
**Canonical name:** ChunkProviderServer

---

## Class Identity

`jz implements ej` is the server-side chunk provider. It wraps:
- `ej c` — the underlying generator (e.g. `jv` ChunkProviderGenerate)
- `d` — chunk I/O (disk storage)
- `wv e` — LRU chunk cache
- `Set a` — pending unload queue (chunk keys)
- `List f` — all loaded chunks (for tick scanning)
- `ry g` — world reference
- `hn b` — a blank "empty" chunk (returned when out of world bounds)

---

## Load / Retrieve: `a(int chunkX, int chunkZ)`

```java
@Override
public zx a(int var1, int var2) {
    long key = acm.a(var1, var2);
    a.remove(key);               // cancel pending unload if re-requested

    zx chunk = e.a(key);         // check LRU cache
    if (chunk == null) {
        // World boundary: x or z beyond ±1875004
        if (out of bounds) return b;  // return empty blank chunk

        chunk = e(var1, var2);   // try to load from disk
        if (chunk == null) {
            chunk = c.b(var1, var2);   // generate from generator
        }

        e.a(key, chunk);         // store in cache
        f.add(chunk);            // add to active list

        chunk.d();               // onChunkLoad
        chunk.e();               // populateSurrounding if all 4 neighbours present
        chunk.a(this, this, var1, var2);  // populate if ready
    }
    return chunk;
}
```

- `b(int x, int z)` = `provideChunk`: same as `a` but checks cache first without
  triggering generation if not loaded.

---

## Population Trigger

Population is deferred until a chunk is explicitly populated via `a(ej, int, int)`:

```java
@Override
public void a(ej var1, int var2, int var3) {
    zx chunk = b(var2, var3);
    if (!chunk.p) {   // p = isPopulated flag
        chunk.p = true;
        if (c != null) {
            c.a(var1, var2, var3);   // call generator's populate
            chunk.g();               // markDirty
        }
    }
}
```

Population fires once when a chunk is first accessed for population.

---

## Per-Tick Maintenance: `a()` (boolean overload = `a(boolean, rz)`)

The save/unload tick is driven by `ry` calling `A.a(boolean, rz)` periodically.

### `a(boolean forceSave, rz progressReporter)`

```java
int saved = 0;
for (zx chunk : f) {
    if (forceSave && !chunk.r) { save(chunk); }    // save to disk
    if (chunk.a(forceSave)) {                       // chunk.a = canUnload
        saveDirty(chunk);
        chunk.q = false;
        if (++saved == 24 && !forceSave) return false;  // budget: 24 per tick
    }
}
if (forceSave && d != null) d.b();   // flush IO
return true;
```

**Save budget: 24 chunks per tick** in normal operation. Unlimited on forced save.

### `a()` — unload queue processor (called as part of world tick)

```java
@Override
public boolean a() {
    // Phase 1: process up to 100 pending unloads per tick
    for (int i = 0; i < 100; i++) {
        if (!a.isEmpty()) {
            Long key = a.iterator().next();
            zx chunk = e.a(key);
            chunk.f();           // onChunkUnload
            save(chunk);
            evictIO(chunk);
            a.remove(key);
            e.d(key);            // remove from LRU
            f.remove(chunk);
        }
    }

    // Phase 2: scan up to 10 chunks for nearby-player check
    for (int i = 0; i < 10; i++) {
        if (h >= f.size()) { h = 0; break; }
        zx chunk = f.get(h++);
        // check if any player within 288 blocks of chunk centre
        vi player = g.a(chunkCentreX, 64, chunkCentreZ, 288.0);
        if (player == null) {
            d(chunk.l, chunk.m);  // mark for unload
        }
    }

    if (d != null) d.a();      // tick IO
    return c.a();              // tick underlying generator
}
```

**Unload budget: up to 100 chunks per tick** removed from unload queue.
**Scan budget: up to 10 chunks per tick** checked for player proximity.

---

## Mark for Unload: `d(int chunkX, int chunkZ)`

```java
public void d(int var1, int var2) {
    dh spawn = g.v();                   // world spawn point
    int dx = var1 * 16 + 8 - spawn.a;  // dx from spawn centre
    int dz = var2 * 16 + 8 - spawn.c;
    short radius = 128;
    if (dx < -radius || dx > radius || dz < -radius || dz > radius) {
        a.add(acm.a(var1, var2));       // add to unload queue
    }
    // chunks within 128 blocks of spawn are NOT marked for unload
}
```

Chunks within **128 blocks of the spawn point** are never added to the unload queue.

---

## Player Proximity Rule

A chunk stays loaded if any player is within **288 blocks** (18 chunks) of its centre.
This is checked for up to 10 chunks per tick via the scan in `a()`.

---

## Summary: Chunk Loading Lifecycle

```
Request chunk (a/b):
  → Cancel pending unload
  → Check LRU cache
  → Load from disk (e)
  → Generate (c.b) if not on disk
  → Register in cache + active list
  → Fire onLoad + populate triggers

Per tick (a()):
  → Unload ≤100 queued chunks
  → Scan ≤10 chunks for player proximity
  → Save ≤24 dirty chunks (a(bool,rz))

Player-driven (ry.g every 30 ticks):
  → Keep 5×5 = 25 chunks around player loaded
```

---

## C# Mapping

| Java | C# |
|---|---|
| `jz` | `ChunkProviderServer` |
| `jz.a(int,int)` | `ProvideChunk(chunkX, chunkZ)` |
| `jz.b(int,int)` | `LoadChunk(chunkX, chunkZ)` |
| `jz.d(int,int)` | `MarkForUnload(chunkX, chunkZ)` |
| `jz.a()` (boolean) | `SaveAllChunks(force, progress)` |
| `jz.a()` (void overload) | `UnloadQueuedChunks()` |
| `jz.a` (Set) | `pendingUnloadSet` |
| `jz.e` (wv) | `chunkCache` (LRU) |
| Save budget 24 | constant in `SaveAllChunks` |
| Unload budget 100 | constant in `UnloadQueuedChunks` |
| Scan budget 10 | constant in `UnloadQueuedChunks` |
| Player radius 288 | constant in proximity check |
| Spawn safe radius 128 | constant in `MarkForUnload` |
