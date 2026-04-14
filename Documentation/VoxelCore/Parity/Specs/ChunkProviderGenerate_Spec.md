# ChunkProviderGenerate Spec
Source: `xj.java` (ChunkProviderGenerate, 411 lines), `ql.java` (BiomeDecorator, 243 lines),
        `eb.java` (NoiseGeneratorOctaves), `agk.java` (PerlinNoiseGenerator)
Type: Class definition + algorithm reference

---

## 1. Overview

`xj` is the Overworld `ChunkProviderGenerate` (returns `"RandomLevelSource"` from `c()`).
It implements `ej` (IChunkProvider).

Chunk generation is a two-pass process:
1. **3D density noise** → stone/water column
2. **Surface pass** → replace top layers with biome-specific surface blocks

Then cave/ravine carving, and optionally structure generation.

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `xj` | `ChunkProviderGenerate` | Overworld generator; `c()` = "RandomLevelSource" |
| `jv` | `ChunkProviderHell` | Nether generator; `c()` = "HellRandomLevelSource" |
| `er` | `ChunkProviderFlat` | Debug/flat world; empty chunks only |
| `ej` | `IChunkProvider` | Interface implemented by all chunk providers |
| `eb` | `NoiseGeneratorOctaves` | N-octave Perlin noise generator |
| `agk` | `PerlinNoiseGenerator` | Single-octave Perlin (512-entry permutation table) |
| `cs` | `NoiseGeneratorBase` | Abstract base for all noise generators |

---

## 3. Fields

| Field | Type | Meaning |
|---|---|---|
| `n` | `Random` | Chunk seed RNG |
| `o` | `eb(16)` | 3D density noise A (16 octaves) |
| `p` | `eb(16)` | 3D density noise B (16 octaves) |
| `q` | `eb(8)` | 3D selector noise (8 octaves) |
| `r` | `eb(4)` | Surface noise (4 octaves, for dirt depth) |
| `a` | `eb(10)` | 2D biome blend noise (10 octaves) |
| `b` | `eb(16)` | 2D hill noise (16 octaves) |
| `c` | `eb(8)` | (unused field, 8 octaves) |
| `d` | `dc` | Stronghold generator |
| `e` | `xn` | Village generator |
| `f` | `kd` | Other structure generator |
| `w` | `bz` (`ln`) | Cave generator (MapGenCaves) |
| `x` | `bz` (`rf`) | Ravine generator (MapGenRavine) |
| `y` | `sr[]` | Biome cache array (populated per chunk) |
| `u` | `double[]` | 3D density output array |
| `l` | `float[25]` | 5×5 Gaussian kernel for biome height blending (lazy-init) |
| `t` | `boolean` | generateStructures flag (passed in constructor) |
| `s` | `ry` | World reference |

---

## 4. Constructor

```java
public xj(ry world, long seed, boolean generateStructures) {
    this.s = world;
    this.t = generateStructures;
    this.n = new Random(seed);
    this.o = new eb(n, 16);
    this.p = new eb(n, 16);
    this.q = new eb(n, 8);
    this.r = new eb(n, 4);
    this.a = new eb(n, 10);
    this.b = new eb(n, 16);
    this.c = new eb(n, 8);
}
```

All noise generators are seeded from the same `Random` sequence (same world seed).

---

## 5. `b(int chunkX, int chunkZ)` → `zx` — Main Generate

```java
n.setSeed((long)chunkX * 341873128712L + (long)chunkZ * 132897987541L);
byte[] blocks = new byte[16 * world.c * 16];
zx chunk = new zx(world, blocks, chunkX, chunkZ);
a(chunkX, chunkZ, blocks);                           // pass 1: density
y = world.a().a(y, chunkX*16, chunkZ*16, 16, 16);   // get biomes
a(chunkX, chunkZ, blocks, y);                         // pass 2: surface
w.a(this, world, chunkX, chunkZ, blocks);             // caves
x.a(this, world, chunkX, chunkZ, blocks);             // ravines
if (t) { f.a(); e.a(); d.a(); }                      // structures
chunk.c();                                            // recalculate heightmap
return chunk;
```

> `a(int, int)` also delegates to `b(int, int)` — both methods are identical.

---

## 6. Pass 1 — `a(int chunkX, int chunkZ, byte[] blocks)` — 3D Density

### Grid Resolution

The Overworld generates a 4×(c/8)×4 density grid (c = world height = 128):
- 4 samples along X (one per 4-block sub-cell)
- 16 samples along Y (one per 8-block sub-cell, since 128/8=16)
- 4 samples along Z

Each 4×8×4 voxel cell is trilinearly interpolated to fill 128 blocks.

### Noise Sampling

Five noise arrays are sampled for each XZ column of the (5×(c/8+1)×5) voxel grid:

| Array | Generator | Scale X/Y/Z | Purpose |
|---|---|---|---|
| `j` | `a` (10 oct) | 1.121, 0.0, 1.121 | 2D biome blend (X-Z only) |
| `k` | `b` (16 oct) | 200.0, 0.0, 200.0 | 2D hill factor (X-Z only) |
| `g` | `q` (8 oct) | 8.555, 4.278, 8.555 | 3D density selector |
| `h` | `o` (16 oct) | 684.412, 684.412, 684.412 | 3D density A |
| `i` | `p` (16 oct) | 684.412, 684.412, 684.412 | 3D density B |

### Biome Height Smoothing (5×5 Kernel)

For each XZ point in the density grid, the surrounding 5×5 biome neighbourhood is
sampled and averaged using a Gaussian-like weight kernel:

```java
float weight = 10.0F / sqrt(dx*dx + dz*dz + 0.2F);
if (neighbour.minH > center.minH) weight /= 2.0F;  // tall biomes blend less
```

Weighted averages computed:
- `var16` = smoothed `x` (maxHeight / amplitude)
- `var17` = smoothed `w` (minHeight)

Then transformed:
```java
var16 = var16 * 0.9F + 0.1F;                  // amplitude clamped to [0.1..1.0]
var17 = (var17 * 4.0F - 1.0F) / 8.0F;         // minHeight → normalized
```

### Density Calculation (per Y slice)

```java
double hillFactor = (k[xz] / 8000.0);
if (hillFactor < 0) hillFactor = -hillFactor * 0.3;
hillFactor = hillFactor * 3.0 - 2.0;
if (hillFactor < 0) {
    hillFactor = clamp(hillFactor / 2.0, -1.0) / 1.4 / 2.0;
} else {
    hillFactor = min(hillFactor, 1.0) / 8.0;
}
hillFactor += 0.2 * amplitudeSmoothed;

double midY = c/2 + minHeightSmoothed * 4.0 * (c/8);  // baseline centre
double heightGradient = ((y - midY) * 12.0 * 128.0 / c) / amplitude;
if (heightGradient < 0) heightGradient *= 4.0;          // sharper below midY

// Selector blend between density A and density B
double selector = (g[xyz] / 10.0 + 1.0) / 2.0;
double density;
if (selector < 0.0)      density = h[xyz] / 512.0;
else if (selector > 1.0) density = i[xyz] / 512.0;
else                     density = h[xyz]/512.0 + (i[xyz]/512.0 - h[xyz]/512.0) * selector;

density -= heightGradient;

// Force atmosphere at top 4 Y slices
if (y > c/8 - 4) {
    double blend = (float)(y - (c/8 - 4)) / 3.0F;
    density = density * (1.0 - blend) + -10.0 * blend;
}
```

### Block Placement

```java
if (density > 0.0)  block = stone (yy.t.bM = 1);
else if (y < seaLevel) block = still_water (yy.B.bM = 9);
else                block = air (0);
```

Sea level = `world.e` (field on `ry`).

---

## 7. Pass 2 — `a(int chunkX, int chunkZ, byte[] blocks, sr[] biomes)` — Surface

Uses 2D noise `r` (4 octaves) at scale 0.03125 (= 1/32) to compute per-column dirt depth:
```java
int dirtDepth = (int)(noise[col] / 3.0 + 3.0 + rand.nextDouble() * 0.25);
```
Expected range: ~1–5 blocks. Default: 3.

Scans each column downward from `world.d` (= max Y - 1 = 127):
1. Bottom bedrock: `y <= nextInt(5)` → bedrock (ID 7)
2. Top bedrock: `y >= world.d - nextInt(5)` → bedrock (ID 7)
3. Air (block == 0): reset depth counter to -1, no surface block
4. Stone (block == 1 = `yy.t.bM`), depth == -1 (first stone hit from above):
   - If `dirtDepth == 0`: surface = air (`var15 = 0`), filler = stone
   - If near sea level (y ∈ [seaLevel-4, seaLevel+1]):
     - `var15 = biome.t` (topBlock), `var16 = biome.u` (fillerBlock)
     - If temperature < 0.15 and below sea level: surface = ice (ID 79 = `yy.aT.bM`)
     - If below sea level: surface = still_water (ID 9 = `yy.B.bM`)
   - If `y >= seaLevel - 1`: place `var15` (top block)
   - Else: place `var16` (filler block)
   - Set depth = `dirtDepth`
5. Stone, depth > 0: place `var16` (filler); if depth reaches 0 and filler == sand → switch to sandstone for 1–3 more blocks

**Biome surface blocks (`t` = top, `u` = filler):**
- Most biomes: t=grass(2), u=dirt(3)  
- Desert: t=sand(12), u=sand(12) (plus sandstone beneath via the extra `if` above)
- Mushroom Island: t=mycelium(110), u=dirt(3)
- (Others determined by biome subclass overrides of `t`/`u` fields)

---

## 8. `a(ej provider, int chunkX, int chunkZ)` — Populate (Decoration)

Called after neighbouring chunks are generated so decorations can spill into adjacent chunks.
Chunk seed is re-derived from world seed:

```java
n.setSeed(worldSeed);
long xSeed = (n.nextLong() / 2L * 2L + 1L);
long zSeed = (n.nextLong() / 2L * 2L + 1L);
n.setSeed((long)chunkX * xSeed + (long)chunkZ * zSeed ^ worldSeed);
```

Steps:
1. `cj.a = true` — enable sand/gravel instant-fall mode
2. If `t` (generateStructures): strongholds, villages, other structures
3. Water lake (1/4 chance)
4. Lava lake (1/8 chance, biased below sea level)
5. 8 dungeon attempts (`acj`)
6. `biome.a(world, rand, x, z)` — biome decoration (trees, flowers, grass, ores)
7. Snow/ice surface (`we.a()`)
8. `cj.a = false`

---

## 9. Ore Generation (via `ql.b()` — BiomeDecorator)

Called inside biome decoration. Uses helper methods:
- `a(count, gen, yMin, yMax)` — uniform Y distribution in [yMin, yMax)
- `b(count, gen, yCenter, ySpread)` — triangular Y distribution ≈ yCenter

| Ore | Generator | Vein size | Count per chunk | Y range |
|---|---|---|---|---|
| Dirt | `i` (ky, 32) | 32 | 20 | 0–128 |
| Gravel | `j` (ky, 32) | 32 | 10 | 0–128 |
| Coal Ore | `k` (ky, 16) | 16 | 20 | 0–128 |
| Iron Ore | `l` (ky, 8) | 8 | 20 | 0–64 |
| Gold Ore | `m` (ky, 8) | 8 | 2 | 0–32 |
| Redstone Ore | `n` (ky, 7) | 7 | 8 | 0–16 |
| Diamond Ore | `o` (ky, 7) | 7 | 1 | 0–16 |
| Lapis Ore | `p` (ky, 6) | 6 | 1 | ~16 (triangular) |

Lapis Y formula: `nextInt(16) + nextInt(16) + 0` → range [0, 30], peak ~15.

`ky` = `WorldGenMinable` — places a vein of the given block ID within the radius.

---

## 10. Decoration Items (via `ql.a()` — BiomeDecorator)

Runs after ores. Counts driven by biome-specific fields on `ql`:

| Item | Generator | Biome field | Default count |
|---|---|---|---|
| Large trees | `g` (fc, 7) | `H` | 3 |
| Bonus trees | `g` (fc) | `G` | 1 |
| Big trees | `f` (adp) | `I` | 1 |
| Standard trees | `e.a(rand)` | `z` | 0 (biome-specific) |
| Sugar cane | `u` (acp) | `J` | 0 |
| Flowers | `q` (bu) | `A` | 2 |
| Roses | `r` (bu) | `A` | 1 in 4 |
| TallGrass | `ahu` | `B` | 1 |
| Dead bushes | `mb` | `C` | 0 |
| Lily pads | `x` (jj) | `y` | 0 |
| Mushrooms | `s/t` (bu) | `D` | per-biome |
| Reed | `v` (tw) | `E` | 0 |
| Pumpkins | `w` (ade) | `F` | 0 (1/32 chance) |
| Water spring | `ib` | — | 50 |
| Lava spring | `ib` | — | 20 |

---

## 11. NoiseGeneratorOctaves (`eb`)

```java
public eb(Random rand, int octaves) {
    // creates octave PerlinNoiseGenerator instances
}

public double[] a(double[] result, int x, int y, int z,
                   int sizeX, int sizeY, int sizeZ,
                   double scaleX, double scaleY, double scaleZ) {
    // fills result array with summed octave noise
    // each octave: amplitude *= 2, frequency *= 0.5 → lower octaves add large features
}
```

The 2D noise overload (sizeY=1) is used for surface/biome data.

---

*Spec written by Analyst AI from `xj.java`, `ql.java`, `eb.java`, `agk.java`. No C# implementation consulted.*
