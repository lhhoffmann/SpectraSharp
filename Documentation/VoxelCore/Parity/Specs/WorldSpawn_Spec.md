# Spec: WorldServer SpawnSearch and Chunk Preloading

**Java classes:** `ry` (World), `si` (WorldInfo)
**Status:** PROVIDED
**Canonical name:** World.findSpawn / WorldInfo

---

## WorldInfo (`si`) — Spawn Storage

`si` stores spawn coordinates in `level.dat` NBT. Fields:

| Field | Type | NBT key | Purpose |
|---|---|---|---|
| `b` | `int` | `"SpawnX"` | Spawn block X |
| `c` | `int` | `"SpawnY"` | Spawn block Y |
| `d` | `int` | `"SpawnZ"` | Spawn block Z |

### Access methods

```java
int  si.c()        // getSpawnX
int  si.d()        // getSpawnY
int  si.e()        // getSpawnZ
void si.a(int, int, int)  // setSpawnPoint(x, y, z)
void si.a(int)     // setSpawnX
void si.b(int)     // setSpawnY
void si.c(int)     // setSpawnZ
```

`ry.C` is the `si` instance. `ry.v()` returns spawn as `dh(C.c(), C.d(), C.e())`.

---

## Initial Spawn Search (ry constructor / world creation)

Called once when a new world is created (not on load from existing save).

### Phase 1 — Biome position search

```java
vh biomeProvider = this.a();               // get biome provider
List validBiomes = biomeProvider.a();      // list of habitable biomes
Random r = new Random(this.t());           // seeded with world seed
am pos = biomeProvider.a(0, 0, 256, validBiomes, r);  // find valid biome within 256 radius

int spawnX = 0, spawnY = c/2, spawnZ = 0;
if (pos != null) { spawnX = pos.a; spawnZ = pos.c; }
else { System.out.println("Unable to find spawn biome"); }
```

### Phase 2 — Surface validity walk (up to 1000 attempts)

```java
int attempts = 0;
while (!y.a(spawnX, spawnZ)) {   // y = SpawnPoint checker (checks block type at surface)
    spawnX += r.nextInt(64) - r.nextInt(64);  // ±63 random walk per step
    spawnZ += r.nextInt(64) - r.nextInt(64);
    if (++attempts == 1000) break;
}
C.a(spawnX, spawnY, spawnZ);  // store in WorldInfo
D = false;
```

`y.a(x, z)` validates the spawn column (typically checks for grass or non-ocean surface).
If 1000 attempts exhausted without a valid position, uses the last walked position.

---

## Spawn Y Refinement (`ry.e()`)

Called during world load (after chunk preloading) to find a valid surface Y.

```java
public void e() {
    if (C.d() <= 0) { C.b(c / 2); }     // ensure Y is sane

    int x = C.c(), z = C.e();
    int attempts = 0;
    while (this.a(x, z) == 0) {           // a(x,z) = getTopSolidOrLiquidBlock height
        x += w.nextInt(8) - w.nextInt(8); // ±7 random walk
        z += w.nextInt(8) - w.nextInt(8);
        if (++attempts == 10000) break;
    }
    C.a(x);   // store refined X
    C.c(z);   // store refined Z
}
```

`this.a(int x, int z)` = `getTopSolidOrLiquidBlock(x, z)`: scans down from max world
height and returns Y of the first non-air block. Returns 0 if no solid block found.

---

## Spawn Chunk Preloading (`Minecraft.e(String)`)

Called when a world is first loaded or created, before the game loop starts.

```java
private void e(String progressLabel) {
    short radius = 128;              // 256-block diameter (Survival)
    if (c.i()) radius = 64;         // 128-block diameter (Creative)

    int chunksPerAxis = radius * 2 / 16 + 1;   // 17 for Survival, 9 for Creative
    int totalChunks = chunksPerAxis * chunksPerAxis;  // 289 or 81 (for progress bar)

    // Center ChunkProviderClient (mv) on spawn
    if (f.x() instanceof mv) {
        ((mv)f.x()).d(spawnX >> 4, spawnZ >> 4);
    }

    // Load all chunks in radius (in rectangular scan order)
    for (int x = -radius; x <= radius; x += 16) {
        for (int z = -radius; z <= radius; z += 16) {
            f.a(spawnX + x, 64, spawnZ + z);   // ensure chunk at (spawnX+x, 64, spawnZ+z) loaded
            if (!c.i()) {
                while (f.F()) {}    // drain pending generation tasks
            }
        }
    }

    // Survival only: run world simulation step
    if (!c.i()) {
        f.r();   // simulate — runs pending ticks and schedules (not a 2000-tick loop)
    }
}
```

**Summary:**
- Survival: 17×17 = 289 chunks (radius 128 blocks, 8 chunks each side + centre)
- Creative: 9×9 = 81 chunks (radius 64 blocks)
- Rectangular scan, not spiral
- Each non-creative step drains the generation queue before moving to next chunk

---

## Per-Tick Spawn Chunk Keep-Loaded (`ry.g(ia player)`)

Called every **30 ticks** from `Minecraft.k()` to keep chunks near the player loaded.

```java
public void g(ia var1) {  // var1 = player (ia = Entity)
    int chunkX = Math.floor(var1.s / 16.0);  // me.c = floor(x/16)
    int chunkZ = Math.floor(var1.u / 16.0);
    byte radius = 2;

    for (int cx = chunkX - 2; cx <= chunkX + 2; cx++) {
        for (int cz = chunkZ - 2; cz <= chunkZ + 2; cz++) {
            this.c(cx, cz);   // load/retain chunk
        }
    }

    if (!g.contains(var1)) { g.add(var1); }
}
```

This keeps a **5×5 = 25 chunk area** around the player loaded.
Called at: `if (++al == 30) { al = 0; f.g(h); }` inside `Minecraft.k()`.

---

## No Dedicated "Force-Loaded" Set

There is no explicit spawn-chunk force-loaded set in the client-side provider (`mv`).
Spawn chunks stay loaded through the combination of:
1. Initial preload loop `e()` loads them all upfront.
2. `g(player)` at player's spawn position refreshes every 30 ticks.
3. `jz.d(chunkX, chunkZ)` marks far chunks for unload only when >128 blocks from spawn.

---

## C# Mapping

| Java | C# |
|---|---|
| `si` | `WorldInfo` |
| `si.b/c/d` | `SpawnX/Y/Z` |
| `ry.e()` | `World.FindSpawnPoint()` |
| `ry.v()` | `World.GetSpawnPoint()` returning `BlockPos` |
| `ry.g(player)` | `World.EnsureChunksAroundPlayer(player)` |
| `Minecraft.e(string)` | `Engine.PreloadSpawnChunks(label)` |
| `mv.d(cx, cz)` | `ChunkProviderClient.CenterOn(chunkX, chunkZ)` |
