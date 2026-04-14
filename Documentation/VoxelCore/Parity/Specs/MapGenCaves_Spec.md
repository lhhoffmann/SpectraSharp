# MapGenCaves Spec
Source: `ln.java` (MapGenCaves, 230 lines), `bz.java` (MapGenBase, 27 lines)
Type: Algorithm reference — underground cave carver

---

## 1. Overview

`ln` (MapGenCaves) carves caves into the raw stone byte-array produced by
ChunkProviderGenerate's density pass. It is called **before** the surface pass,
operating directly on the `byte[]` block array rather than on a live World.

Caves are generated **from surrounding chunks**: for any target chunk, the 17×17
neighbourhood of source chunks (±8 in each axis) each independently decide whether
to spawn cave starts, and their corridors can reach into the target chunk.
This makes caves continuous across chunk boundaries.

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `bz` | `MapGenBase` | Abstract base for cave + ravine generators |
| `ln` | `MapGenCaves` | Cave carver; extends `bz` |
| `rf` | `MapGenRavine` | Ravine carver; also extends `bz` (not specced here) |

---

## 3. MapGenBase (`bz`) Fields

| Field | Type | Default | Meaning |
|---|---|---|---|
| `a` | `int` | 8 | Chunk search radius (17×17 = 289 source chunks per target) |
| `b` | `Random` | `new Random()` | Seeded per source chunk |
| `c` | `ry` | — | World reference (set on first call) |

### MapGenBase entry point: `a(provider, world, tgtChunkX, tgtChunkZ, blocks)`

```
c = world
b.setSeed(world.t())          // world.t() = getWorldSeed() (long)
r1 = b.nextLong()
r2 = b.nextLong()

for srcX in [tgtChunkX - a .. tgtChunkX + a]:
    for srcZ in [tgtChunkZ - a .. tgtChunkZ + a]:
        seed = (srcX * r1) XOR (srcZ * r2) XOR world.t()
        b.setSeed(seed)
        a(world, srcX, srcZ, tgtChunkX, tgtChunkZ, blocks)  // protected, overridden by ln
```

Each source chunk gets a reproducible seed derived from the world seed and its own
coordinates — caves look the same regardless of generation order.

---

## 4. MapGenCaves — Source Chunk Entry: `a(world, srcChunkX, srcChunkZ, tgtChunkX, tgtChunkZ, blocks)`

### 4.1 Cave count for this source chunk

```
count = b.nextInt(b.nextInt(b.nextInt(40) + 1) + 1)
// triple-nested: very skewed toward 0, max theoretical = 40
if b.nextInt(15) != 0:
    count = 0    // 87% of source chunks contribute nothing
```

Distribution: approximately 87% contribute 0, 13% contribute at least 1 cave start
(average across the 13% ≈ geometric, usually 1–3, rarely more).

### 4.2 Per cave start

For each of `count` starts:

**Starting position** (within source chunk):
```
startX = srcChunkX * 16 + b.nextInt(16)   // any block in the source chunk
startY = b.nextInt(b.nextInt(worldHeight - 8) + 8)   // any Y; biased toward lower half
startZ = srcChunkZ * 16 + b.nextInt(16)
```
`worldHeight` = `world.c` = 128.

**Room + branch pattern** (25% of starts):
```
if b.nextInt(4) == 0:
    a(b.nextLong(), tgtChunkX, tgtChunkZ, blocks, startX, startY, startZ)  // room segment
    extraBranches += b.nextInt(4)   // 0-3 additional tunnels from same point
```
A "room" is a single cave segment using the default (large) radius.

**Tunnel generation** (always; 1 + extraBranches tunnels):
```
for each tunnel:
    yaw   = b.nextFloat() * 2PI          // random horizontal direction
    pitch = (b.nextFloat() - 0.5) * 2 / 8   // [-0.25, 0.25] slight vertical angle
    radius = b.nextFloat() * 2.0 + b.nextFloat()   // base: [0, 4); usually ~1-3

    if b.nextInt(10) == 0:
        radius *= b.nextFloat() * b.nextFloat() * 3.0 + 1.0   // ~10% chance: extra-wide cave
    
    a(b.nextLong(), tgtChunkX, tgtChunkZ, blocks, startX, startY, startZ,
      radius, yaw, pitch, 0, 0, 1.0)
```

---

## 5. Cave Segment Method: `a(seed, tgtChunkX, tgtChunkZ, blocks, x, y, z, radius, yaw, pitch, startStep, totalSteps, thicknessMult)`

This is the core recursive tunnel-carving function. It can be called as:
- A **room** (radius = 1-7 random, thicknessMult = 0.5, startStep = -1)
- A **tunnel** (radius = 0-4 from caller, thicknessMult = 1.0, startStep = 0, totalSteps = 0)
- A **branch** (radius = parent*0.5, startStep = branchStep, totalSteps = parent totalSteps)

### 5.1 Initialisation

```
chunkCenterX = tgtChunkX * 16 + 8   (world X of target chunk centre)
chunkCenterZ = tgtChunkZ * 16 + 8

pitchSpeed = 0.0   // angular velocity for pitch
yawSpeed   = 0.0   // angular velocity for yaw

rand = new Random(seed)    // fresh RNG from seed, NOT from bz.b

if totalSteps <= 0:
    range = a * 16 - 16               // = 8*16-16 = 112
    totalSteps = range - rand.nextInt(range / 4)   // [84, 111]

isMidpoint = (startStep == -1)
if isMidpoint:
    startStep = totalSteps / 2

branchPoint = rand.nextInt(totalSteps / 2) + totalSteps / 4
```

### 5.2 Step loop (`step` from `startStep` to `totalSteps - 1`)

On each step:

**A. Compute cross-section diameters:**
```
sinePhase = sin(step * PI / totalSteps)   // 0 at ends → 1 at midpoint

horDiameter = 1.5 + sinePhase * radius * 1.0
verDiameter = horDiameter * thicknessMult
// thicknessMult = 1.0 → round tunnel; 0.5 → flat/wide cave room
```

**B. Advance position along tunnel direction:**
```
x += cos(pitch) * cos(yaw)
y += sin(pitch)
z += cos(pitch) * sin(yaw)
```
`sin`/`cos` = `me.a()`/`me.b()` (MathHelper lookup table).

**C. Smooth and perturb direction:**
```
// Gravity damping on pitch (two modes)
if isStraight (25% chance, rand.nextInt(6)==0 at outer start):
    pitch *= 0.92
else:
    pitch *= 0.70

// Apply angular velocities
pitch += pitchSpeed * 0.1
yaw   += yawSpeed * 0.1

// Dampen velocities
pitchSpeed *= 0.9
yawSpeed   *= 0.75

// Random perturbation
pitchSpeed += (rand.nextFloat() - rand.nextFloat()) * rand.nextFloat() * 2.0
yawSpeed   += (rand.nextFloat() - rand.nextFloat()) * rand.nextFloat() * 4.0
```

**D. Branch spawning** (at `step == branchPoint`):
```
if not isMidpoint AND step == branchPoint AND radius > 1.0 AND totalSteps > 0:
    // Spawn left branch
    a(rand.nextLong(), tgtChunkX, tgtChunkZ, blocks, x, y, z,
      rand.nextFloat() * 0.5 + 0.5,    // radius 50-100% of parent (but capped to ~0.5-1.0)
      yaw - PI/2,                        // 90° left
      pitch / 3.0,
      step, totalSteps, 1.0)

    // Spawn right branch
    a(rand.nextLong(), tgtChunkX, tgtChunkZ, blocks, x, y, z,
      rand.nextFloat() * 0.5 + 0.5,
      yaw + PI/2,                        // 90° right
      pitch / 3.0,
      step, totalSteps, 1.0)
    
    return   // parent segment terminates here
```

**E. Skip step** (75% of iterations do no carving, for performance):
```
if rand.nextInt(4) == 0:
    continue   // skip carving this step
```

**F. Distance culling** (early exit if clearly outside target chunk):
```
dx = x - chunkCenterX
dz = z - chunkCenterZ
stepsRemaining = totalSteps - step
maxReach = radius + 2.0 + 16.0

if dx*dx + dz*dz - stepsRemaining*stepsRemaining > maxReach*maxReach:
    return
```

**G. Bounds check** (skip if too far from target chunk area):
```
if x < chunkCenterX - 16 - horDiameter*2: skip this step
if z < chunkCenterZ - 16 - horDiameter*2: skip this step
if x > chunkCenterX + 16 + horDiameter*2: skip this step
if z > chunkCenterZ + 16 + horDiameter*2: skip this step
// No Y bounds check here — Y is clamped in the carving loop
```

**H. Water proximity check** (abort carving if near water):

Compute integer bounding box within the target chunk [0, 16):
```
xMin = clamp(floor(x - horDiameter) - tgtChunkX*16 - 1, 0, 16)
xMax = clamp(floor(x + horDiameter) - tgtChunkX*16 + 1, 0, 16)
yMin = clamp(floor(y - verDiameter) - 1, 1, worldHeight - 8)
yMax = clamp(floor(y + verDiameter) + 1, 1, worldHeight - 8)
zMin = clamp(floor(z - horDiameter) - tgtChunkZ*16 - 1, 0, 16)
zMax = clamp(floor(z + horDiameter) - tgtChunkZ*16 + 1, 0, 16)
```

Scan bounding box border only (inner blocks skipped via early jump to yMin):
```
foundWater = false
for bx in [xMin, xMax):
    for bz in [zMin, zMax):
        for by in [yMax+1 downto yMin-1]:
            idx = (bx * 16 + bz) * 128 + by
            if blocks[idx] == water_flowing (yy.A) OR blocks[idx] == water_still (yy.B):
                foundWater = true; break
            // inner blocks optimisation: if not on boundary face, jump to yMin
            if by is not on any face (not border bx/bz and not yMin-1/yMax+1):
                by = yMin   // skip inner column
```

If `foundWater`: skip all carving for this step (continue outer loop).

**I. Carve blocks:**

Iterate X from `xMin` to `xMax-1`, Z from `zMin` to `zMax-1`:
```
normX = ((bx + tgtChunkX*16) + 0.5 - x) / (horDiameter / 2.0)
```
If `normX * normX >= 1.0`: skip column (outside ellipsoid in X).

Then iterate Z:
```
normZ = ((bz + tgtChunkZ*16) + 0.5 - z) / (horDiameter / 2.0)
```

Then iterate Y **downward** from `yMax-1` to `yMin`:
```
idx = (bx * 16 + bz) * 128 + by
normY = ((by) + 0.5 - y) / (verDiameter / 2.0)

// Floor guard: skip if normY <= -0.7 (prevents carving through bedrock floor)
if normY <= -0.7: skip

if normX*normX + normY*normY + normZ*normZ < 1.0:
    block = blocks[idx]
    
    if block == grass (yy.u.bM = 2):
        markedGrass = true     // remember: grass was here
    
    if block == stone (yy.t.bM = 1) OR dirt (yy.v.bM = 3) OR grass (yy.u.bM = 2):
        if by < 10:
            blocks[idx] = lava_still (yy.C.bM = 11)   // below Y=10: fill with lava
        else:
            blocks[idx] = 0   // air
            
            // Restore surface: if we just carved where grass was and dirt is one below
            if markedGrass AND blocks[idx - 1] == dirt (yy.v.bM = 3):
                blocks[idx - 1] = biome.topBlock    // from world.WorldChunkManager.getBiome
```

Note: `idx - 1` in the block array decrements Y by 1 (since array is indexed `(x*16+z)*128 + y`).
`biome.topBlock` = `world.a().a(bx + tgtChunkX*16, bz + tgtChunkZ*16).t` — the biome at that XZ.

**J. Room exit:**
```
if isMidpoint:
    break   // rooms only process one step (the midpoint step)
```

---

## 6. Block Array Layout

The `byte[]` passed to MapGenCaves uses the same index formula as Chunk:
```
index = (localX * 16 + localZ) * 128 + y
```
Where `localX` = `worldX - chunkOriginX`, etc.

This is the **raw generation array** — the cave carver writes to it directly before
any Chunk object is created.

---

## 7. Key Numeric Values

| Value | Source | Meaning |
|---|---|---|
| `a = 8` | bz default | Chunk search radius → 17×17 = 289 source chunks |
| `87%` | `nextInt(15) != 0` | Probability a source chunk contributes zero caves |
| `[84, 111]` | `112 - nextInt(28)` | Default segment length (steps) |
| Y < 10 | hardcoded | Below this: caves fill with lava instead of air |
| -0.7 | floor guard | Prevents carving through very bottom of ellipsoid → no flat floors |
| `branchPoint` | `nextInt(steps/2) + steps/4` | Branch occurs in [steps/4, steps*3/4] |
| thicknessMult = 0.5 | room calls | Flattens vertical radius (wider-than-tall rooms) |
| thicknessMult = 1.0 | tunnel calls | Round tunnels |

---

## 8. Quirks to Preserve

- **87% empty sources:** Most source chunks contribute nothing; this is intentional and produces
  cave density comparable to the original game.
- **Floor lava seam:** At Y < 10, carved blocks become lava_still (not air). This creates the
  lava layer at the bottom of caves without any explicit ocean-of-lava pass.
- **Grass surface restoration:** When a cave opens directly at the surface, the exposed dirt
  below where grass was placed gets replaced with the biome's topBlock. This prevents
  bare dirt being visible at cave entrances.
- **Water abort:** The entire step (one sphere along the tunnel) is skipped — not just the
  overlapping blocks — if any water is detected in the bounding box. This prevents
  underwater pocket carving but may leave partial cave openings near ocean floors.
- **Branch parent terminates:** When a segment spawns two branches, it immediately returns.
  The branches continue independently with their own seeds.
- **`-0.7` floor guard:** `normY > -0.7` means the bottom ~15% of the ellipsoid is never
  carved. This creates naturally rounded (not flat) cave floors.

---

*Spec written by Analyst AI from `ln.java` (230 lines) and `bz.java` (27 lines). No C# implementation consulted.*
*(Proactive — not yet requested by Coder)*
