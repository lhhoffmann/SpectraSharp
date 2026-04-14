# LightPropagation Spec
Source: `ry.java` (World, 2788 lines), `zx.java` (Chunk, 781 lines), `bn.java` (LightType enum, 8 lines), `k.java` (WorldProvider, 120 lines)
Type: Algorithm reference — sky and block light storage, BFS propagation, rendering query

---

## 1. Overview

Minecraft 1.0 uses a hybrid BFS light system with two independent light channels:

- **Sky light** (`bn.a`) — sunlight, attenuates downward through opaque blocks; max 15 at sky-exposed positions.
- **Block light** (`bn.b`) — emitted by torches, lava, glowstone, etc.; max 15 at source, decays by opacity+1 per step.

Both channels are stored as 4-bit nibble values in per-chunk arrays. Propagation happens via a BFS over an `int[] H` queue in World. Rendering uses a combined value that subtracts a sky-darkening factor for time-of-day and weather.

---

## 2. Light Type Enum (`bn`)

```java
enum bn {
    a(15),   // SKY  — default (out-of-bounds) value = 15
    b(0);    // BLOCK — default (out-of-bounds) value = 0
    public final int c;  // default value
}
```

---

## 3. Chunk Light Storage

### 3.1 Nibble arrays (type `up` = NibbleArray)

| Field | Content | Notes |
|---|---|---|
| `g` | Metadata (4-bit per block) | — |
| `h` | **Sky light** (4-bit per block) | Zero in Nether (`y.e == true`) |
| `i` | **Block light** (4-bit per block) | — |

Each `up` stores one nibble per block index (same index formula as block array).

### 3.2 Block array index

```
index = (localX << f.b) | (localZ << f.a) | y
      = (localX << 11) | (localZ << 7) | y
```

Where `f.b = world.a + 4 = 11`, `f.a = world.a = 7`.

Equivalent to: `index = x * 2048 + z * 128 + y`.

### 3.3 Height map (`j[]`)

```java
j[z << 4 | x]   // byte array, 256 entries per chunk
```

Stores the **lowest Y where sky light can start** = the first Y from top with `yy.o[blockId] != 0`.
A block at Y has sky light if `y >= j[z<<4|x]`.

Chunk field `k` = minimum height map value across all columns (used for rendering).

### 3.4 Nibble read/write

```java
// Read:
int a(bn lightType, int localX, int y, int localZ)
    if (lightType == bn.a) return h.a(localX, y, localZ);   // sky
    if (lightType == bn.b) return i.a(localX, y, localZ);   // block

// Write:
void a(bn lightType, int localX, int y, int localZ, int level)
    if (lightType == bn.a && !world.y.e)  // sky only if not Nether
        h.a(localX, y, localZ, level);
    if (lightType == bn.b)
        i.a(localX, y, localZ, level);
```

---

## 4. Block Arrays Used by Light System

| Array | Meaning | Notes |
|---|---|---|
| `yy.o[id]` | **Light opacity** (0-15) | 0=fully transparent; 15=fully opaque |
| `yy.q[id]` | **Light emission** (0-15) | 0 for non-glowing blocks |
| `yy.s[id]` | **Use-neighbor-max flag** | If true, block samples neighbor light instead of self |

When `yy.o[id] == 0`, it is treated as 1 for attenuation purposes (every step loses at least 1).

---

## 5. World Light API

### 5.1 Read methods

```java
// Raw nibble read — no special cases
int b(bn type, int x, int y, int z)
    → chunk.a(type, x&15, y, z&15)

// Nibble read with yy.s neighbor-max
int a(bn type, int x, int y, int z)   // 4-arg
    if (out of Y bounds)  return type.c
    if (chunk not loaded) return 0
    if (yy.s[blockAt(x,y,z)]):        // transparent blocks use neighbor max
        return max of 6 neighbors' a(type, ...)
    else:
        return chunk.a(type, x&15, y, z&15)

// Combined light for rendering (sky minus darkening, vs block)
int c(int x, int y, int z, int skyDark)   // in Chunk, called for rendering
    skyVal = h.a(localX, y, localZ) - skyDark   // (0 if Nether)
    blockVal = i.a(localX, y, localZ)
    return max(skyVal, blockVal)

// World wrapper: combined with current sky darkening (world.k)
int a(int x, int y, int z, boolean specialCases)   // 4-arg, returns for rendering
    // if specialCases && block is glass/ice/water-flowing/etc:
    //   return max of adjacent a(x,y,z,false)
    // else:
    //   return chunk.c(x&15, y, z&15, world.k)

int n(int x, int y, int z)
    → a(x, y, z, true)    // getLightValue for rendering (with special-case transparent blocks)

int m(int x, int y, int z)
    → chunk.c(x&15, y, z&15, 0)   // combined raw (no sky darkening)
```

### 5.2 IBlockAccess override (packed light for shading)

```java
@Override
int a(int x, int y, int z, int minBlockLight)     // kq method (used by renderer)
    skyPacked  = a(bn.a, x, y, z)
    blockPacked = max(a(bn.b, x, y, z), minBlockLight)
    return (skyPacked << 20) | (blockPacked << 4)
```

The renderer receives a packed int: bits 23-20 = sky level, bits 7-4 = block level.

### 5.3 Sky exposure check

```java
boolean l(int x, int y, int z)
    → chunk.c(x&15, y, z&15)    // returns y >= heightMap[z<<4|x]

int d(int x, int z)              // get height map Y
    → chunk.b(x&15, z&15)       // returns j[z<<4|x] & 0xFF
```

---

## 6. BFS Propagation — `World.c(bn type, int x, int y, int z)`

The main propagation entry point. Updates light at `(x,y,z)` and propagates changes to neighbors.

### 6.1 Precondition

```
if not e(x, y, z, 17):   // chunk + radius-17 loaded
    return
```

### 6.2 Queue entry format (int[] H, size 32768)

Packed as a single int:
```
entry = (dx + 32)
      | ((dy + 32) << 6)
      | ((dz + 32) << 12)
      | (lightLevel << 18)   // only used in Phase 1 (decrease)
```

Origin `(0,0,0)` = `32 | (32<<6) | (32<<12)` = **133152**.

### 6.3 Algorithm

```
readHead = 0
writeHead = 0
current = b(type, x, y, z)       // current stored level
blockId = a(x, y, z)
opacity = max(yy.o[blockId], 1)  // min 1

// Compute what the correct value SHOULD be:
if (type == sky) correct = skyLightValue(current, x, y, z, blockId, opacity)
else             correct = blockLightValue(current, x, y, z, blockId, opacity)

if (correct > current):
    // Increased: add origin to queue, skip Phase 1
    H[writeHead++] = 133152

else if (correct < current):
    // Decreased: Phase 1 — BFS to zero out light that came from this source
    H[writeHead++] = 133152 | (current << 18)

    while (readHead < writeHead):
        entry = H[readHead++]
        px = ((entry & 63) - 32) + x
        py = ((entry >> 6 & 63) - 32) + y
        pz = ((entry >> 12 & 63) - 32) + z
        oldLevel = (entry >> 18) & 15
        storedLevel = b(type, px, py, pz)
        
        if (storedLevel == oldLevel):
            a(type, px, py, pz, 0)    // zero out this block
            if (oldLevel > 0 && |px-x| + |py-y| + |pz-z| < 17):
                for each of 6 neighbors (nx, ny, nz):
                    neighOpacity = max(yy.o[a(nx,ny,nz)], 1)
                    neighLevel = b(type, nx, ny, nz)
                    if (neighLevel == oldLevel - neighOpacity):
                        H[writeHead++] = packed(nx-x, ny-y, nz-z) | ((oldLevel - neighOpacity) << 18)
    
    readHead = 0   // reset → Phase 2 reads same queue positions starting from 0

// Phase 2: propagate upward — recompute correct value for each queued position
while (readHead < writeHead):
    entry = H[readHead++]
    px, py, pz = unpack(entry, x, y, z)
    storedLevel = b(type, px, py, pz)
    blockId2 = a(px, py, pz)
    opacity2 = max(yy.o[blockId2], 1)
    
    if (type == sky) correct2 = skyLightValue(storedLevel, px, py, pz, blockId2, opacity2)
    else             correct2 = blockLightValue(storedLevel, px, py, pz, blockId2, opacity2)
    
    if (correct2 != storedLevel):
        a(type, px, py, pz, correct2)   // write to nibble
        if (correct2 > storedLevel):     // increased: add neighbors needing update
            if (|dx| + |dy| + |dz| < 17 && writeHead < H.length - 6):
                for each neighbor (nx, ny, nz):
                    if b(type, nx, ny, nz) < correct2:
                        H[writeHead++] = packed(nx-x, ny-y, nz-z)   // no level
```

### 6.4 6-Neighbor iteration pattern

Used consistently throughout (faces ±X, ±Y, ±Z):
```
for i in 0..5:
    sign = (i % 2) * 2 - 1      // alternates -1, +1
    nx = x + (i/2 % 3 / 2) * sign
    ny = y + ((i/2+1) % 3 / 2) * sign
    nz = z + ((i/2+2) % 3 / 2) * sign
```

---

## 7. Sky Light Value Computation

```java
private int a(int current, int x, int y, int z, int blockId, int opacity):
    if l(x, y, z):    // sky-exposed (y >= heightMap)
        return 15
    
    best = 0
    for each of 6 neighbors (nx, ny, nz):
        neighSky = b(bn.a, nx, ny, nz)
        candidate = neighSky - opacity
        if (candidate > best) best = candidate
    return best
```

Sky light is **15** at or above the height map. Below, it attenuates by the block's opacity (minimum 1 per step).

---

## 8. Block Light Value Computation

```java
private int d(int current, int x, int y, int z, int blockId, int opacity):
    best = yy.q[blockId]    // self-emission
    
    best = max(best, b(bn.b, x-1, y, z) - opacity)
    best = max(best, b(bn.b, x+1, y, z) - opacity)
    best = max(best, b(bn.b, x, y-1, z) - opacity)
    best = max(best, b(bn.b, x, y+1, z) - opacity)
    best = max(best, b(bn.b, x, y, z-1) - opacity)
    best = max(best, b(bn.b, x, y, z+1) - opacity)
    return best
```

Block light = max of self-emission and all neighbors minus opacity.

---

## 9. Update Triggers on SetBlock

When `Chunk.a(localX, y, localZ, newId, meta)` is called (full setBlock with metadata):

```
1. Write newId to b[] array
2. Call old block's onBlockRemoved (if not generating)
3. Write meta to metadata nibble g
4. Height map update (if not Nether):
   - If new block is opaque (yy.o[newId] != 0) AND y >= oldHeight:
       g(localX, y+1, localZ)   // height rose — update upward
   - If new block is transparent (yy.o[newId] == 0) AND y == oldHeight-1:
       g(localX, y, localZ)     // height fell — update at y
5. Trigger sky BFS (if not Nether):
       world.a(bn.a, worldX, y, worldZ, worldX, y, worldZ)   // 7-arg — empty in base class
6. Trigger block BFS:
       world.a(bn.b, worldX, y, worldZ, worldX, y, worldZ)   // 7-arg — empty in base class
7. Call new block's onBlockAdded (if not generating)
```

**Important:** The 7-arg `World.a(bn, x1,y1,z1, x2,y2,z2)` body is empty in base `ry`.
Sky light updates happen through step 4 (height map update), not step 5.
Block light updates happen lazily through the `checkLight` random tick (see §10).

The simpler `Chunk.a(localX, y, localZ, newId)` (4-arg, no meta) follows the same pattern.

### Height map update: `Chunk.g(localX, changedY, localZ)`

```
oldHeight = j[localZ<<4 | localX] & 255
startY = max(changedY, oldHeight)

// Walk down to find new height
newY = startY
while (newY > 0 && yy.o[b[(x<<11)|(z<<7)|(newY-1)] & 255] == 0):
    newY--

if (newY != oldHeight):
    j[localZ<<4 | localX] = (byte) newY
    update k (min height across chunk)
    
    if (not Nether):
        // Update sky nibbles in the changed range
        if (newY < oldHeight):  // height fell (block removed) → add sky light
            for y in [newY, oldHeight): h.a(localX, y, localZ, 15)
        else:                   // height rose (block placed) → remove sky light
            for y in [oldHeight, newY): h.a(localX, y, localZ, 0)
        
        // Re-propagate sky attenuation below new height
        level = 15
        y = newY - 1
        while (y >= 0 && level > 0):
            level -= max(yy.o[blockAt(localX,y,localZ)], 1)
            level = max(level, 0)
            h.a(localX, y, localZ, level)
            y--
        
        // Propagate BFS to neighbours for each Y in changed range
        world.i(worldX-1, worldZ, min(newY,oldHeight), max(newY,oldHeight))
        world.i(worldX+1, worldZ, ...)
        world.i(worldX, worldZ-1, ...)
        world.i(worldX, worldZ+1, ...)
        world.i(worldX, worldZ, ...)    // the column itself
```

### `World.i(x, z, minY, maxY)`

```
if (minY > maxY): swap
if (not Nether):
    for y in [minY, maxY]:
        c(bn.a, x, y, z)    // sky BFS for each Y
c(x, minY, z, x, maxY, z)   // notify render listeners (bd.a)
```

---

## 10. Random Light Check (`World.s`)

Called once per tick per chunk (line 1902-1903) for a random block in that chunk:

```java
void s(int x, int y, int z):
    if (not Nether):
        c(bn.a, x, y, z)   // sky BFS
    c(bn.b, x, y, z)       // block BFS
```

This is how block light eventually propagates after block placement — it is NOT immediate.
The block at `(x, y, z)` is chosen randomly: `x = chunkOriginX + rand.nextInt(16)`, etc.

---

## 11. Sky Darkening (`World.k`)

`k` = sky darkening amount (0-11), subtracted from sky light level for rendering.

Updated each tick:
```java
void p():
    k = a(1.0F)     // a() = getSkyDarkenAmount

int a(float partialTick):
    sunAngle = c(partialTick)        // angle in [0,1] based on time of day
    brightness = cos(sunAngle * 2π) * 2 + 0.5   // clamp [0,1]
    darkening = 1 - brightness
    darkening *= 1 - rain * 5/16      // rain reduces sky brightness
    darkening *= 1 - thunder * 5/16   // thunder further reduces
    darkening = 1 - darkening
    return (int)(darkening * 11)      // 0 (full day) to 11 (midnight/storm)
```

---

## 12. Brightness Table (`WorldProvider.f[16]`)

Maps light level (0-15) to a `float` brightness multiplier for rendering:

```java
protected void a():   // called at world init
    for level in 0..15:
        t = 1 - level / 15.0F
        f[level] = (1 - t) / (t * 3 + 1) * (1 - ambient) + ambient
```

Where `ambient = 0.0F` for Overworld. Produces an S-curve mapping:
- `f[0]` ≈ 0.0 (pitch black)
- `f[7]` ≈ 0.55
- `f[15]` = 1.0 (maximum brightness)

Used in `World.b(x,y,z,minLight)` and `World.c(x,y,z)` for biome sky color and fog.

---

## 13. Chunk Initialization (`c()` — full recalculate on generate)

Called when a freshly generated chunk is first built. Initialises both height map and sky nibbles:

```
for localX in 0..15:
    for localZ in 0..15:
        // Find height
        y = 127
        while (y > 0 && yy.o[block(localX, y-1, localZ)] == 0): y--
        j[localZ<<4|localX] = (byte) y
        
        if (not Nether):
            // Fill sky light downward from top
            level = 15
            scanY = 127
            while (scanY >= 0 && level > 0):
                level -= max(yy.o[block(localX, scanY, localZ)], 1)
                if (level < 0) level = 0
                if (level > 0) h.a(localX, scanY, localZ, level)
                scanY--
        
        // Mark all columns as needing BFS neighbour update
        d(localX, localZ)    // sets d[localX + localZ*16] = true

// Process deferred neighbour BFS (if adjacent chunks loaded)
// called from l() during chunk tick
```

---

## 14. Key Constant Summary

| Value | Source | Meaning |
|---|---|---|
| `bn.a` = sky, `bn.b` = block | `bn.java` | Light type identifiers |
| `15` | — | Maximum light level |
| `0` | — | Minimum (dark) |
| `world.k` | Computed each tick | Sky darkening 0-11 |
| `yy.o[id]` | Block class array | Opacity per block ID |
| `yy.q[id]` | Block class array | Emission per block ID |
| `j[z<<4\|x]` | Chunk height map | First opaque Y from top |
| `H[32768]` | World BFS queue | Packed (dx,dy,dz,level) |
| `133152` | Queue constant | Packed (0,0,0) offset = `32\|(32<<6)\|(32<<12)` |

---

## 15. Quirks to Preserve

- **Sky light is 15 at and above the height map**, not computed from neighbors. The height map defines the sky boundary, not the actual stored sky nibble.
- **Sky light update is synchronous; block light is lazy.** SetBlock triggers sky re-propagation immediately via height-map updates. Block light only updates via random `checkLight` ticks — takes up to several ticks to converge.
- **Minimum opacity 1:** Even fully transparent blocks (opacity 0) reduce light by 1 per step. Air is never perfectly transmissive over distance.
- **Nether has no sky light:** `y.e == true` suppresses sky nibble writes and sky BFS. All light in Nether is block light only.
- **`yy.s` neighbor sampling:** Leaves, glass, flowing water sample neighbor max instead of their own stored value. This prevents these blocks from creating hard shadows — they appear as lit as their surroundings.
- **BFS queue is reused across both phases:** Phase 1 zeroes (decrease) and Phase 2 propagation (increase) share the same H[] buffer. After Phase 1, readHead resets to 0 so Phase 2 re-processes the same positions.
- **Distance cap 17:** BFS does not enqueue neighbors beyond Manhattan distance 17 from the origin. Propagation farther than 17 blocks is deferred to adjacent setBlock triggers or random checkLight.

---

*Spec written by Analyst AI from `ry.java` (World, 2788 lines), `zx.java` (Chunk, 781 lines), `bn.java`, `k.java` (WorldProvider, 120 lines). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — LightPropagation)*
