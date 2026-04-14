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

# ChunkProviderHell Spec
**Source class:** `jv.java`
**Implements:** `ej` (IChunkProvider)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`jv` (ChunkProviderHell) generates Nether (Hell) dimension terrain. The algorithm is
structurally similar to `ChunkProviderGenerate` (overworld) but produces the Nether's
characteristic double-ceiling hollowness via a Y-shape curve that compresses solid terrain
near the top and bottom of the world while leaving a large hollow middle space.

Block ID reference for this spec:

| Block | ID | `yy` field |
|---|---|---|
| Air | 0 | — |
| Bedrock | 7 | `yy.z` |
| Gravel | 13 | `yy.F` |
| Still Lava | 11 | `yy.D` |
| Flowing Lava | 10 | `yy.C` |
| Fire | 51 | `yy.ar` |
| Brown Mushroom | 39 | `yy.af` |
| Red Mushroom | 40 | `yy.ag` |
| Netherrack | 87 | `yy.bb` |
| Soul Sand | 88 | `yy.bc` |
| Glowstone | 89 | `yy.bd` |

---

## 2. Fields

| Field | Type | Constructor init | Semantics |
|---|---|---|---|
| `i` | `Random` | `new Random(seed)` | Per-chunk RNG seeded by `(chunkX * 341873128712L + chunkZ * 132897987541L)` |
| `j` | `eb` (NoiseOctaves) | `new eb(i, 16)` | 16-octave density noise A |
| `k` | `eb` | `new eb(i, 16)` | 16-octave density noise B |
| `l` | `eb` | `new eb(i, 8)` | 8-octave density blend |
| `m` | `eb` | `new eb(i, 4)` | 4-octave surface noise (q and r samples) |
| `n` | `eb` | `new eb(i, 4)` | 4-octave surface depth noise (s sample) |
| `a` | `eb` (public) | `new eb(i, 10)` | 10-octave shape noise X — computed but NOT used in final density (dead code) |
| `b` | `eb` (public) | `new eb(i, 16)` | 16-octave shape noise Z — computed but NOT used in final density (dead code) |
| `o` | `ry` (World) | from constructor | World reference |
| `p` | `double[]` | null | Density grid buffer: 5 × (worldHeight/8+1) × 5 |
| `q`, `r`, `s` | `double[]` | `new double[256]` | Surface noise buffers (16×16 each) |
| `d`, `e`, `f`, `g`, `h` | `double[]` | null | Working buffers for density computation |
| `t` | `cz` | `new cz()` | Nether cave carver — same tunnel algorithm as MapGenCaves |
| `c` | `ed` | `new ed()` | Nether Fortress generator (structure + mob spawn list) |

---

## 3. Constructor

```
jv(ry world, long seed)
```

1. `this.o = world`
2. `this.i = new Random(seed)` — master RNG seeded from world seed
3. In order: construct `j`, `k`, `l`, `m`, `n`, `a`, `b` — each constructor call advances the
   master RNG (`i`), so construction order is significant.

---

## 4. Methods — Detailed Logic

### `a(int chunkX, int chunkZ)` — provideChunk (via IChunkProvider)

Delegates: `return b(chunkX, chunkZ)`.

### `b(int chunkX, int chunkZ)` — generateChunk

Full chunk generation sequence:

```
i.setSeed(chunkX * 341873128712L + chunkZ * 132897987541L)
blockArray = new byte[16 * worldHeight * 16]

a(chunkX, chunkZ, blockArray)     // Pass 1: density terrain
b(chunkX, chunkZ, blockArray)     // Pass 2: surface + bedrock
t.a(this, world, chunkX, chunkZ, blockArray)   // Pass 3: cave carving (cz)
c.a(this, world, chunkX, chunkZ, blockArray)   // Pass 4: fortress outlines

return new Chunk(world, blockArray, chunkX, chunkZ)
```

---

## 5. Pass 1 — Density Terrain: `a(int chunkX, int chunkZ, byte[] blocks)`

Fills `blocks[]` with netherrack, still lava, and air based on a 3D density grid interpolated
via trilinear interpolation.

#### Step 1 — Compute 5×(Y+1)×5 density grid

```
gridX = 4 + 1 = 5
gridZ = 4 + 1 = 5
gridY = worldHeight / 8 + 1      (= 17 for worldHeight=128)
lavaLevel = 32                    // var5

p = computeDensityGrid(p, chunkX*4, 0, chunkZ*4, gridX, gridY, gridZ)
```

**`computeDensityGrid(out, startX, startY, startZ, sizeX, sizeY, sizeZ)` — density function:**

Fills a `sizeX × sizeY × sizeZ` double array. Index formula: `[x * sizeZ * sizeY + z * sizeY + y]`

Noise samples (all via named generators):
```
g[] = a.a(g, startX, startY, startZ, sizeX, 1, sizeZ, 1.0, 0.0, 1.0)
      // shape X: 10 octaves, Y-scale=0 (flat in Y)
h[] = b.a(h, startX, startY, startZ, sizeX, 1, sizeZ, 100.0, 0.0, 100.0)
      // shape Z: 16 octaves
d[] = l.a(d, startX, startY, startZ, sizeX, sizeY, sizeZ, 684.412/80, 2053.236/60, 684.412/80)
      // blend: 8 octaves, scale 8.555, 34.22, 8.555
e[] = j.a(e, startX, startY, startZ, sizeX, sizeY, sizeZ, 684.412, 2053.236, 684.412)
      // density A: 16 octaves, scale 684.412, 2053.236, 684.412
f[] = k.a(f, startX, startY, startZ, sizeX, sizeY, sizeZ, 684.412, 2053.236, 684.412)
      // density B: 16 octaves, same scale as A
```

Note: `g[]` and `h[]` are computed and used to derive `var17` and `var21` but these values
are NOT used in the actual density output. Their computation is dead code in the Nether
generator. See §9 Open Questions.

**Y-shape curve `yShape[y]` for y in 0..(sizeY-1):**

```
for y in 0..(sizeY-1):
    yShape[y] = cos(y * PI * 6 / sizeY) * 2.0

    mirror = y
    if y > sizeY / 2:
        mirror = sizeY - 1 - y      // fold around midpoint

    if mirror < 4:
        mirror = 4 - mirror
        yShape[y] -= mirror^3 * 10.0   // cubic pull-down near top and bottom
```

For worldHeight=128 (sizeY=17): midpoint = 8.
- y=0: mirror=0 → extra = 4^3 * 10 = 640 subtracted
- y=1: mirror=1 → extra = 3^3 * 10 = 270 subtracted
- y=2: mirror=2 → extra = 2^3 * 10 = 80 subtracted
- y=3: mirror=3 → extra = 1^3 * 10 = 10 subtracted
- y=4..12: mirror >= 4, no extra subtraction (cosine curve only)
- y=13..16: mirror of y=3..0 (same pulls)

This strongly suppresses positive density near the top and bottom of the grid, creating
the Nether's double-floor structure (floor at y~0-31, ceiling at y~100-127 for 128-height).

**Per grid point density:**

```
for xCell in 0..(sizeX-1):
    for zCell in 0..(sizeZ-1):
        xzIdx++

        // The following are computed but NOT used in output:
        var17 = clamp((g[xzIdx] + 256.0) / 512.0, 0, 1)
        var21 = h[xzIdx] / 8000.0
        var21 = abs(var21) * 3 - 3
        [further var17/var21 processing — dead code]

        for yCell in 0..(sizeY-1):
            yShape_val = yShape[yCell]
            densityA = e[xyzIdx] / 512.0
            densityB = f[xyzIdx] / 512.0
            blend = clamp01((d[xyzIdx] / 10.0 + 1.0) / 2.0)

            // Lerp between A and B based on blend
            if blend < 0: density = densityA
            elif blend > 1: density = densityB
            else: density = densityA + (densityB - densityA) * blend

            density -= yShape_val    // apply Y curve (subtracts → forces solid/air)

            // Top fade: last 4 Y cells
            if yCell > sizeY - 4:
                t = (yCell - (sizeY - 4)) / 3.0     // 0..1 over last 3 cells
                density = density * (1 - t) + (-10.0) * t

            // var19 = 0.0 always → this condition is never true (dead code):
            // if yCell < 0.0: density = lerp toward -10 based on gap

            out[xyzIdx] = density
```

#### Step 2 — Trilinear interpolation to 16×worldHeight×16

Iterates over 4×4 XZ cells × worldHeight/8 Y cells, each 4×4×8 voxels:

```
for xCell in 0..3:
    for zCell in 0..3:
        for yCell in 0..(worldHeight/8 - 1):
            step = 1/8 = 0.125

            d000 = grid[xCell,   yCell,   zCell  ]
            d001 = grid[xCell,   yCell,   zCell+1]
            d100 = grid[xCell+1, yCell,   zCell  ]
            d101 = grid[xCell+1, yCell,   zCell+1]
            dY00 = (grid[xCell,   yCell+1, zCell  ] - d000) * 0.125
            dY01 = (grid[xCell,   yCell+1, zCell+1] - d001) * 0.125
            dY10 = (grid[xCell+1, yCell+1, zCell  ] - d100) * 0.125
            dY11 = (grid[xCell+1, yCell+1, zCell+1] - d101) * 0.125

            for fy in 0..7:
                dX0 = d000
                dX1 = d001
                dZ0 = (d100 - d000) * 0.25
                dZ1 = (d101 - d001) * 0.25

                for fx in 0..3:
                    dZ = dX0
                    dZstep = (dX1 - dX0) * 0.25

                    for fz in 0..3:
                        absX = fx + xCell * 4
                        absY = yCell * 8 + fy
                        absZ = fz + zCell * 4
                        blockIdx = chunk array index(absX, absY, absZ)

                        block = 0
                        if absY < lavaLevel (32):
                            block = yy.D.bM     // still lava (ID 11)
                        if dZ > 0.0:
                            block = yy.bb.bM    // netherrack (ID 87) — overrides lava

                        blocks[blockIdx] = block
                        dZ += dZstep
                    dX0 += dZ0
                    dX1 += dZ1
                d000 += dY00; d001 += dY01; d100 += dY10; d101 += dY11
```

Netherrack density > 0 takes priority over the lava fill: solid netherrack appears even
below lava level (Y < 32) when density is positive.

---

## 6. Pass 2 — Surface + Bedrock: `b(int chunkX, int chunkZ, byte[] blocks)`

Adds bedrock at the top and bottom of the world, and patches soul sand / gravel at
the ceiling surface zone (Y ≈ [worldHeight-68, worldHeight-63]).

```
ceilingRef = worldHeight - 64       // = 64 for worldHeight=128

q[] = m.a(q, chunkX*16, chunkZ*16, 0, 16, 16, 1, 0.03125, 0.03125, 1.0)
r[] = m.a(r, chunkX*16, 109, chunkZ*16, 16, 1, 16, 0.03125, 1.0, 0.03125)
s[] = n.a(s, chunkX*16, chunkZ*16, 0, 16, 16, 1, 0.0625, 0.0625, 0.0625)
```

For each XZ column (x in 0..15, z in 0..15):

```
soulSandFlag = q[x + z*16] + i.nextDouble()*0.2 > 0   // i = chunk RNG (seeded)
gravelFlag   = r[x + z*16] + i.nextDouble()*0.2 > 0
depthNoise   = s[x + z*16]
depth = (int)(depthNoise / 3.0 + 3.0 + i.nextDouble()*0.25)
        // typically 1..7

countdown = -1         // surface countdown; -1 = not yet encountered netherrack
surfaceBlock = netherrack
fillBlock    = netherrack

for y = worldHeight-1 downto 0:
    idx = blockArrayIndex(x, y, z)

    // Bedrock: top and bottom layers with random thickness
    if y >= worldHeight - 1 - i.nextInt(5):
        blocks[idx] = bedrock (ID 7)
        continue
    if y <= 0 + i.nextInt(5):
        blocks[idx] = bedrock (ID 7)
        continue

    current = blocks[idx]

    if current == 0:
        countdown = -1      // air: reset countdown

    elif current == netherrack:
        if countdown == -1:   // first netherrack from top
            if depth <= 0:
                surfaceBlock = air
                fillBlock    = netherrack
            elif y in [ceilingRef-4, ceilingRef+1]:   // ceiling surface zone
                surfaceBlock = netherrack
                fillBlock    = netherrack
                if gravelFlag:
                    surfaceBlock = gravel (ID 13)     // gravel ceiling patches
                    fillBlock    = netherrack
                if soulSandFlag:                       // soul sand overrides gravel
                    surfaceBlock = soul sand (ID 88)
                    fillBlock    = soul sand

            // Below ceiling zone: if no surface block, fill with lava
            if y < ceilingRef AND surfaceBlock == air:
                surfaceBlock = still lava (ID 11)

            countdown = depth       // start depth countdown

            if y >= ceilingRef - 1:
                blocks[idx] = surfaceBlock
            else:
                blocks[idx] = fillBlock

        elif countdown > 0:
            countdown--
            blocks[idx] = fillBlock
```

**Key notes:**
- Bedrock random thickness: uses `i.nextInt(5)` — between 0 and 4 extra bedrock rows.
  Top bedrock: Y ≥ `worldHeight - 1 - rand(0..4)`. Bottom bedrock: Y ≤ `rand(0..4)`.
- Soul sand overrides gravel: if both `soulSandFlag` and `gravelFlag` are true, soul sand wins
  (because the soul sand `if` executes after the gravel `if`).
- Surface zone depth: `depth` is in [1..7] (approx). Blocks at Y ≥ `ceilingRef-1` get
  `surfaceBlock`; blocks below get `fillBlock`.
- `gravelFlag` from noise sampled at fixed Y=109. This gives a consistent ceiling pattern
  regardless of the Y value being processed.
- The `i.nextDouble()*0.2` jitter uses the chunk's seeded Random (`this.i`), NOT the noise
  generator's internal random. The RNG state is consumed 3 times per XZ column per Y scan
  (var9, var10, var11) AND consumed by the bedrock rows. Column order (x*16+z) must match.

---

## 7. Pass 3 — Cave Carving: `cz` (MapGenCaves variant)

`cz` extends `bz` (MapGenBase) — the same cave carving infrastructure as the Overworld
`MapGenCaves` (class `ln`). Differences from overworld caves:

- Initial radius: `1.0F + rng.nextFloat() * 6.0F` (same range as overworld)
- Thickness multiplier: `0.5` (overworld uses `1.0`) — thinner tunnels
- Carving replaces netherrack (not stone as in the overworld); bedrock is not carved

The scan radius, direction perturbation, branching, and lava-floor logic from MapGenCaves_Spec
apply with these modifications.

---

## 8. Pass 4 — Fortress Outlines: `ed` (NetherFortress generator)

`ed` generates Nether Fortress structures into the block array. This spec does not cover
the fortress geometry; see a future NetherFortress_Spec.md. The fortress is handled by
`c.a(provider, world, chunkX, chunkZ, blockArray)` in `generateChunk`.

The `ed` instance also provides the mob spawn list for the fortress area:
- `qf` — 10 per group, min 2 max 3
- `jm` — 10 per group, min 4 max 4
- `aea` — 3 per group, min 4 max 4

---

## 9. Populate Step: `a(ej provider, int chunkX, int chunkZ)`

Decorates an already-generated chunk with features. All placements use `this.i` seeded at
the start of `generateChunk`.

```
cj.a = true    // lighting suppression flag

// Populate Nether Fortress (chests, spawners, etc.)
c.a(world, i, chunkX, chunkZ)

// 8 lava pool attempts
for 8:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight - 8) + 4
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenNetherLavaPool(yy.C.bM).a(world, i, x, y, z)
    // ey: place flowing lava (ID 10) in a 1-block pocket backed by 4 netherrack, 1 air

// Fire patches (variable count 1..10)
fireCount = i.nextInt(i.nextInt(10) + 1) + 1
for fireCount:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight - 8) + 4
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenNetherFire().a(world, i, x, y, z)
    // pl: 64 attempts to place fire (ID 51) on netherrack floor

// Glowstone clusters type 1 (variable count 0..9)
gsCount1 = i.nextInt(i.nextInt(10) + 1)
for gsCount1:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight - 8) + 4
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenGlowStone1().a(world, i, x, y, z)
    // pt: grow glowstone cluster downward from netherrack ceiling

// Glowstone clusters type 2 (always 10)
for 10:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight)
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenGlowStone2().a(world, i, x, y, z)
    // aew: same algorithm as WorldGenGlowStone1

// Brown mushroom (always placed — see §11 quirk)
if i.nextInt(1) == 0:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight)
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenFlowers(yy.af.bM).a(world, i, x, y, z)

// Red mushroom (always placed — see §11 quirk)
if i.nextInt(1) == 0:
    x = chunkX*16 + i.nextInt(16) + 8
    y = i.nextInt(worldHeight)
    z = chunkZ*16 + i.nextInt(16) + 8
    new WorldGenFlowers(yy.ag.bM).a(world, i, x, y, z)

cj.a = false
```

---

## 10. Populate Generators — Sub-Algorithms

### WorldGenNetherLavaPool (`ey`, parametric block)

**Construct:** `ey(blockId)` stores `a = blockId`. Called with `yy.C.bM` (flowing lava, ID 10).

**`a(world, rng, x, y, z)` logic:**
```
if block above (y+1) != netherrack: return false
if block at (x,y,z) != air AND != netherrack: return false

count netherrack neighbors among: W, E, S, N, below (5 faces total) → var6
count air neighbors among: W, E, S, N, below (5 faces total) → var7

if var6 == 4 AND var7 == 1:
    world.setBlock(x, y, z, a)     // place the stored block (flowing lava)
    world.f = true
    Block.byId[a].onBlockAdded(world, x, y, z, rng)   // trigger lava spread
    world.f = false

return true
```

The lava pool requires exactly 4 netherrack neighbours and exactly 1 air neighbour (creating
a single-exit pocket). `world.f = true` suppresses neighbour updates during onBlockAdded.

### WorldGenNetherFire (`pl`)

**`a(world, rng, x, y, z)` logic:**
```
for 64 attempts:
    tx = x + rng.nextInt(8) - rng.nextInt(8)
    ty = y + rng.nextInt(4) - rng.nextInt(4)
    tz = z + rng.nextInt(8) - rng.nextInt(8)
    if world.isAirBlock(tx, ty, tz) AND block at (tx, ty-1, tz) == netherrack:
        world.setBlock(tx, ty, tz, fire ID 51)
return true
```

Places fire in up to 64 random positions within ±8 XZ and ±4 Y.

### WorldGenGlowStone1 (`pt`) and WorldGenGlowStone2 (`aew`)

Both classes have identical code — they are functionally the same glowstone cluster generator.

**`a(world, rng, x, y, z)` logic:**
```
if NOT world.isAirBlock(x, y, z): return false
if block at (x, y+1, z) != netherrack: return false   // must hang from netherrack ceiling

world.setBlock(x, y, z, glowstone ID 89)    // seed block

for 1500 attempts:
    bx = x + rng.nextInt(8) - rng.nextInt(8)    // ±7 XZ
    by = y - rng.nextInt(12)                      // only goes DOWN from seed (never up)
    bz = z + rng.nextInt(8) - rng.nextInt(8)

    if world.isAirBlock(bx, by, bz):
        glowNeighbours = count of 6 faces that are glowstone
        if glowNeighbours == 1:
            world.setBlock(bx, by, bz, glowstone ID 89)   // grow cluster

return true
```

The cluster grows downward only (never upward) from the netherrack ceiling attachment point.
Each block is placed only if it has exactly 1 glowstone neighbour (no internal filling, only
outer growth). With 1500 attempts and ±7 XZ / 12 Y range, clusters can reach considerable size.

---

## 11. IChunkProvider Interface Methods

| Method | Implementation |
|---|---|
| `c(chunkX, chunkZ)` — isChunkLoaded | Always returns true |
| `a(hostile, save)` — saveTick | Always returns true |
| `a()` — canSave | Returns false |
| `b()` — tick | Returns true |
| `c()` — getDebugName | Returns `"HellRandomLevelSource"` |
| `a(jf type, x, y, z)` — getSpawnableList | Returns fortress list for hostile type at fortress positions; otherwise delegates to biome via WorldChunkManager |
| `a(world, name, x, y, z)` — findClosestStructure | Returns null |

---

## 12. Bitwise & Data Layouts

No per-block metadata is written during terrain generation. Netherrack, soul sand, gravel, and
bedrock are all placed with metadata 0.

---

## 13. Known Quirks / Bugs to Preserve

1. **`i.nextInt(1) == 0` always true:** `Random.nextInt(1)` always returns 0 (the only value
   in range [0,1)). Both mushroom placements execute on every Nether chunk. The conditional is
   dead code — it looks like it was intended to be `nextInt(2)` or higher but was not corrected.

2. **Dead shape code (`g[]`/`h[]`):** The shape noise arrays `g` (from `a` generator, 10 octaves)
   and `h` (from `b` generator, 16 octaves) are computed per chunk in the density function,
   including the processing of `var17` and `var21`. However, neither `var17` nor `var21` appear
   in the density output array. This computation wastes ~2 noise evaluations per grid point.
   Preserve it to match RNG state (the generators advance their internal state during sampling).

3. **Glowstone cluster grows downward only:** `by = y - rng.nextInt(12)` means the cluster never
   grows above the seed point. Glowstone clusters are always bottom-heavy.

4. **`WorldGenGlowStone1` (`pt`) and `WorldGenGlowStone2` (`aew`) are identical:** The two classes
   share identical implementations. The populate step calls `pt` for variable count (0–9) and
   `aew` for a fixed count of 10. Both implementations must be preserved separately for RNG
   parity (each generator instance has its own internal state via `ig`).

5. **Lava pool trigger `onBlockAdded`:** After placing flowing lava, `jv.a` temporarily sets
   `world.f = true` to suppress normal notifications, then calls `onBlockAdded` directly. This
   starts lava propagation in a controlled way. `world.f` must be restored to `false` afterward.

6. **Cave carver uses netherrack (not stone) as the carve-through block:** `cz` replaces
   netherrack where caves would be. If the chunk contains other blocks (e.g. gravel soul sand
   placed by the surface pass), those may also be carved through since `cz` carves any non-air
   block the cave volume touches (same behaviour as the overworld carver).

---

## 14. Open Questions

1. **Dead shape code intention:** `g[]`/`h[]` appear designed to bias terrain toward biome-like
   height variation (as in the overworld generator), but neither value reaches the density output.
   This may be a copy-paste from the overworld density function that was never adapted for the
   Nether. Confirm that the generators `a` and `b` still advance their RNG state (they are called
   via `.a()`), so they affect subsequent noise queries even if their output is unused.

2. **`ed` (NetherFortress) class hierarchy:** `ed` extends `hl`. The full fortress generation
   algorithm (structure layout, room types, chest/spawner placement, `qf`/`jm`/`aea` mob classes)
   is not covered in this spec. A separate NetherFortress_Spec.md is needed.

3. **`cj.a` flag:** `cj.a` is set to `true` during populate and restored to `false`. This appears
   to be a global "suppress lighting" or "suppress block update" flag. Confirm the exact semantics
   of `cj.a` from the World or Chunk class.

4. **Mob classes `qf`, `jm`, `aea`:** The fortress spawn list contains `qf` (likely Blaze or
   PigZombie), `jm` (PigZombie), and `aea` (likely MagmaCube). Confirm from entity registry.
