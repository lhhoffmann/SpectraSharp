# MapGenRavine Spec
Source: `rf.java` (MapGenRavine, 186 lines) + `bz.java` (MapGenBase, 27 lines — already specced in MapGenCaves_Spec §3)
Type: Algorithm reference — ravine carver

---

## 1. Overview

`rf` (MapGenRavine) carves deep, tall, narrow chasms into the raw stone byte-array produced by
ChunkProviderGenerate's density pass. It uses the same `MapGenBase` infrastructure as `ln`
(MapGenCaves) but with different parameters that produce a distinctly ravine-like shape:

- **2% spawn probability** (vs 13% for caves) — ravines are rare.
- **Large horizontal radius** (up to ~12 blocks) — ravines are wide.
- **`thicknessMult = 3.0`** — the vertical extent is 3× the horizontal radius → very tall and narrow.
- **Irregular cross-section** from a per-Y noise array `d[]` — walls are rough, not smooth.
- **No branching** — ravines are single straight-ish corridors.
- **Y range [20, 68]** — ravines carve at mid-depth (not near bedrock or surface).

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `bz` | `MapGenBase` | Abstract base; 17×17 scan; seeded RNG; see MapGenCaves_Spec §3 |
| `rf` | `MapGenRavine` | Ravine carver; extends `bz` |

`MapGenBase` entry point and seed formula are identical to MapGenCaves — see
[MapGenCaves_Spec.md §3](MapGenCaves_Spec.md) for details.

---

## 3. Per-Source-Chunk Entry: `a(world, srcChunkX, srcChunkZ, tgtChunkX, tgtChunkZ, blocks)`

```
if bz.b.nextInt(50) != 0:
    return      // 98% of source chunks contribute no ravines (2% chance)

// Starting position within source chunk:
startX = srcChunkX * 16 + b.nextInt(16)
startY = b.nextInt(b.nextInt(40) + 8) + 20    // [20, 68]
startZ = srcChunkZ * 16 + b.nextInt(16)

count = 1   // always exactly 1 ravine per qualifying source chunk

for i in [0, count):
    yaw   = b.nextFloat() * 2π
    pitch = (b.nextFloat() - 0.5F) * 2.0F / 8.0F    // [-0.25, 0.25]
    radius = (b.nextFloat() * 2.0F + b.nextFloat()) * 2.0F   // [0, ~12)
    
    a(b.nextLong(), tgtChunkX, tgtChunkZ, blocks,
      startX, startY, startZ,
      radius, yaw, pitch,
      startStep=0, totalSteps=0, thicknessMult=3.0)
```

### Y range derivation

`nextInt(40)` → [0,39]; `nextInt(result+8)` → [0, 8–47]; `+20` → **[20, 67]**.
Ravines always start in the mid-depth range, avoiding bedrock and the surface.

### Radius range

`nextFloat()*2 + nextFloat()` → [0, 3); `*2.0` → **[0, ~6)**. Typical ≈ 2–4.
This is 3× the cave radius maximum — ravines are substantially wider.

---

## 4. Segment Method: `a(seed, tgtChunkX, tgtChunkZ, blocks, x, y, z, radius, yaw, pitch, startStep, totalSteps, thicknessMult)`

Called with `startStep=0`, `totalSteps=0`, `thicknessMult=3.0`.

### 4.1 Shared initialisation (identical to MapGenCaves §5.1)

```
chunkCenterX = tgtChunkX * 16 + 8
chunkCenterZ = tgtChunkZ * 16 + 8

pitchSpeed = 0.0
yawSpeed   = 0.0
rand = new Random(seed)

if totalSteps <= 0:
    range = a * 16 - 16 = 112
    totalSteps = 112 - rand.nextInt(28)    // [84, 111]

isMidpoint = (startStep == -1)   // always false for ravines (startStep=0)
if isMidpoint: startStep = totalSteps / 2

branchPoint = rand.nextInt(totalSteps / 2) + totalSteps / 4   // unused — no branching
```

### 4.2 Per-Y scale array `d[]` (size 1024)

Computed once before the step loop:

```
var27 = 1.0F    // current scale factor
for y in [0, world.c):     // 0..127
    if (y == 0 || rand.nextInt(3) == 0):
        var27 = 1.0F + rand.nextFloat() * rand.nextFloat() * 1.0F   // [1.0, 2.0]
    d[y] = var27 * var27    // [1.0, 4.0]
```

`d[y]` is reset roughly every 3 Y levels. Values in `[1.0, 4.0]` independently vary the
horizontal extent of the cross-section per Y level, producing rough irregular walls.

### 4.3 Step loop

**A. Cross-section diameters:**
```
sinePhase = sin(step * π / totalSteps)    // [0, 1] peak at midpoint

horRadius = 1.5 + sinePhase * radius * 1.0   // sine-bulge horizontal
verRadius = horRadius * thicknessMult         // = horRadius * 3.0 before random scaling

// Both get independent random ±25% scaling:
horRadius *= rand.nextFloat() * 0.25 + 0.75
verRadius *= rand.nextFloat() * 0.25 + 0.75
```

The effective horizontal radius is approximately `1.5..radius` blocks,
the vertical radius is `3×` that → very tall relative to width.

**B. Advance position** (identical to caves):
```
x += cos(pitch) * cos(yaw)
y += sin(pitch)
z += cos(pitch) * sin(yaw)
```

**C. Pitch damping and direction perturbation** (identical to caves):
```
pitch *= 0.7F
pitch += pitchSpeed * 0.05F
yaw   += yawSpeed   * 0.05F
pitchSpeed *= 0.8F
yawSpeed   *= 0.5F
pitchSpeed += (rand.nextFloat() - rand.nextFloat()) * rand.nextFloat() * 2.0F
yawSpeed   += (rand.nextFloat() - rand.nextFloat()) * rand.nextFloat() * 4.0F
```

Note: pitch damping is **always 0.7** (no isStraight mode since there are no branches).

**D. No branching**: Ravines never spawn branches. The `branchPoint` variable is computed
but never checked. The segment continues from step 0 through `totalSteps-1`.

**E. Skip step** (25% of iterations skipped, same as caves):
```
if rand.nextInt(4) == 0: continue
```
But only if NOT isMidpoint — since isMidpoint is always false for ravines, the 25% skip always applies.

**F. Distance culling** (identical to caves):
```
dx = x - chunkCenterX; dz = z - chunkCenterZ
stepsRemaining = totalSteps - step
maxReach = radius + 2.0 + 16.0
if dx*dx + dz*dz - stepsRemaining*stepsRemaining > maxReach*maxReach: return
```

**G. Bounds check** (identical to caves):
```
if x < chunkCenterX - 16 - horRadius*2: skip
if z < chunkCenterZ - 16 - horRadius*2: skip
if x > chunkCenterX + 16 + horRadius*2: skip
if z > chunkCenterZ + 16 + horRadius*2: skip
```

**H. Bounding box computation:**
```
xMin = clamp(floor(x - horRadius) - tgtChunkX*16 - 1, 0, 16)
xMax = clamp(floor(x + horRadius) - tgtChunkX*16 + 1, 0, 16)
yMin = clamp(floor(y - verRadius) - 1,                1, worldHeight - 8)
yMax = clamp(floor(y + verRadius) + 1,                1, worldHeight - 8)
zMin = clamp(floor(z - horRadius) - tgtChunkZ*16 - 1, 0, 16)
zMax = clamp(floor(z + horRadius) - tgtChunkZ*16 + 1, 0, 16)
```

**I. Water proximity abort** (identical to caves — scans border, skips if water/flowing-water found).

**J. Carving with ravine-specific ellipsoid test:**

```
for bx in [xMin, xMax):
    normX = ((bx + tgtChunkX*16) + 0.5 - x) / horRadius
    if normX*normX >= 1.0: continue    // outside in X

    for bz in [zMin, zMax):
        normZ = ((bz + tgtChunkZ*16) + 0.5 - z) / horRadius
        if normX*normX + normZ*normZ >= 1.0: continue   // outside in XZ

        markedGrass = false
        idx = (bx * 16 + bz) * 128 + yMax - 1

        for by in [yMax-1 downto yMin]:
            normY = (by + 0.5 - y) / verRadius

            // *** RAVINE-SPECIFIC ELLIPSOID TEST ***
            if (normX*normX + normZ*normZ) * d[by] + normY*normY / 6.0 < 1.0:
                block = blocks[idx]
                if block == grass (yy.u): markedGrass = true
                if block == stone OR dirt OR grass:
                    if by < 10:
                        blocks[idx] = lava_still (yy.C.bM = 11)
                    else:
                        blocks[idx] = 0    // air
                        if markedGrass AND blocks[idx-1] == dirt:
                            blocks[idx-1] = biome.topBlock

            idx--    // decrement Y
```

**K. No isMidpoint early exit**: Ravines never break early — they always complete all steps.

---

## 5. Ellipsoid Test Explained

The ravine uses a **modified ellipsoid test** instead of the sphere test used by caves:

```
Caves:   normX² + normY² + normZ² < 1.0              (unit sphere)
Ravines: (normX² + normZ²) * d[y] + normY²/6.0 < 1.0 (squashed/scaled)
```

Effects:
- `d[y] ∈ [1.0, 4.0]` **narrows** the horizontal cross-section by up to 2× per Y level.
  At `d[y]=4`: effective horizontal width = `horRadius / 2`. At `d[y]=1`: full `horRadius`.
- `/ 6.0` **widens** the vertical cross-section: the block passes if `normY < sqrt(6) ≈ 2.45`.
  Combined with `verRadius = horRadius * 3.0`, the actual vertical extent is enormous.

The result: the ravine is **tall and narrow**, with irregular walls that vary by depth —
the characteristic look of a ravine vs the rounded tunnels of a cave.

---

## 6. Key Numeric Differences vs MapGenCaves

| Parameter | MapGenCaves | MapGenRavine |
|---|---|---|
| Source chunk probability | 13% (nextInt(15)==0 fails) | **2%** (nextInt(50)==0) |
| Start Y range | Any Y, biased low | **[20, 68]** |
| Base radius | [0, 4) | **[0, ~12)** |
| thicknessMult | 1.0 (tunnel) / 0.5 (room) | **3.0** |
| Pitch damping | 0.92 (straight) or 0.70 | **0.70 always** |
| Branching | Yes (at branchPoint) | **None** |
| isMidpoint / room | Yes (25% of starts) | **Never** |
| Ellipsoid test | `normX²+normY²+normZ² < 1` | **`(normX²+normZ²)*d[y] + normY²/6 < 1`** |
| Extra noise | None | **d[] per-Y array** |

---

## 7. Shared Behaviours (identical to MapGenCaves)

- **Lava below Y=10:** carved blocks below Y<10 become lava_still (ID 11).
- **Grass surface restoration:** when a ravine breaks to the surface, the exposed dirt below
  former grass is replaced with `biome.topBlock`.
- **Water abort:** entire step is skipped (not just overlapping blocks) if any water found in bbox.
- **Distance culling:** early return if clearly outside target chunk's reachable area.
- **Block array index:** `(localX * 16 + localZ) * 128 + y`.
- **Only carves stone, dirt, grass:** other blocks (ores, bedrock, etc.) are left untouched.
- **17×17 source chunk scan** from `MapGenBase`.

---

## 8. Summary Table

| Value | Source | Meaning |
|---|---|---|
| 2% | `nextInt(50)==0` | Probability a source chunk contributes a ravine |
| [20, 68] | `nextInt(nextInt(40)+8)+20` | Ravine start Y range |
| [0, ~12) | `(rnd*2+rnd)*2.0` | Ravine radius range |
| 3.0 | constant | `thicknessMult` → vertical 3× horizontal |
| [84, 111] | same as caves | Total steps per segment |
| [1.0, 4.0] | `d[y] = var27²` | Per-Y horizontal scale factor |
| 6.0 | constant | Vertical divisor in ellipsoid test → widens height |
| Y < 10 | hardcoded | Below: lava; above: air |

---

*Spec written by Analyst AI from `rf.java` (186 lines). `bz.java` (MapGenBase) already specced in MapGenCaves_Spec §3.*
*(Addresses Coder request [STATUS:REQUIRED] — MapGenRavine)*
