# WorldGenMinable Spec
Source: `ky.java` (WorldGenMinable, 56 lines), `ig.java` (WorldGenerator base)
Type: Algorithm reference — ore vein generator

---

## 1. Overview

`ky` is the universal ore vein generator. One instance is created per ore type in
`ql` (BiomeDecorator) and reused for every placement attempt. It generates a single
continuous ellipsoidal vein — a series of overlapping spheres along a randomly-oriented
capsule axis — and replaces stone blocks with the target ore block.

Used for: dirt, gravel, coal, iron, gold, redstone, diamond, lapis (see ChunkProviderGenerate_Spec §9).

---

## 2. Class Identifiers

| Obfuscated | Human name |
|---|---|
| `ky` | `WorldGenMinable` |
| `ig` | `WorldGenerator` (base class) |
| `me` | `MathHelper` |

---

## 3. Fields

| Field | Type | Set by | Meaning |
|---|---|---|---|
| `a` | `int` | constructor | Block ID to place (the ore) |
| `b` | `int` | constructor | Vein size (number of spheres along the capsule) |

Constructor: `ky(int blockId, int veinsSize)` → `this.a = blockId; this.b = veinsSize;`

---

## 4. Algorithm — `a(world, rand, x, y, z)` → boolean

Always returns `true`.

### Step 1 — Random capsule axis

A random angle `theta` is chosen to orient the vein in XZ:
```
theta = rand.nextFloat() * PI
```

Two endpoints of the capsule axis (in world space):
```
startX = (x + 8) + sin(theta) * b / 8.0
endX   = (x + 8) - sin(theta) * b / 8.0
startZ = (z + 8) + cos(theta) * b / 8.0
endZ   = (z + 8) - cos(theta) * b / 8.0
```

`sin` and `cos` here are `me.a()` and `me.b()` (MathHelper lookup table sine/cosine).

Two Y endpoints (independent of theta, slight random Y spread):
```
startY = y + rand.nextInt(3) - 2    // range: [y-2, y]
endY   = y + rand.nextInt(3) - 2    // range: [y-2, y]
```

### Step 2 — Sphere loop (b+1 iterations, i = 0 to b inclusive)

For each step `i`:

**2a. Interpolated center position** (linear interpolation along the capsule axis):
```
centerX = startX + (endX - startX) * i / b
centerY = startY + (endY - startY) * i / b
centerZ = startZ + (endZ - startZ) * i / b
```

**2b. Sphere radius** at this step:
```
sinePhase  = sin(i * PI / b)                          // 0 at both ends, 1 at midpoint
randFactor = rand.nextDouble() * b / 16.0             // [0, b/16)
diameter   = (sinePhase + 1.0) * randFactor + 1.0    // always >= 1.0; peaks at centre
```

Both the X/Z radius and the Y radius use the same `diameter`:
```
radiusXZ = diameter   (= var28)
radiusY  = diameter   (= var30, computed identically)
```
Result: the ellipsoid is a sphere (equal radii in all three axes).

**2c. Bounding box** (inclusive integer ranges):
```
xMin = floor(centerX - radiusXZ/2)      xMax = floor(centerX + radiusXZ/2)
yMin = floor(centerY - radiusY/2)       yMax = floor(centerY + radiusY/2)
zMin = floor(centerZ - radiusXZ/2)      zMax = floor(centerZ + radiusXZ/2)
```
`floor()` here is `me.c()` (MathHelper floor-to-int, which rounds toward negative infinity).

**2d. Block replacement** — for each (bx, by, bz) in the bounding box:

Normalised distances:
```
dx = (bx + 0.5 - centerX) / (radiusXZ / 2.0)
dy = (by + 0.5 - centerY) / (radiusY  / 2.0)
dz = (bz + 0.5 - centerZ) / (radiusXZ / 2.0)
```

Inside-sphere test (early-exit optimisation applied in order: X, then XY, then XYZ):
```
if dx*dx < 1.0:
    if dx*dx + dy*dy < 1.0:
        if dx*dx + dy*dy + dz*dz < 1.0:
            if world.getBlockId(bx, by, bz) == stone_id (yy.t.bM = 1):
                world.d(bx, by, bz, this.a)   // silent set, no neighbor notification
```

Only stone is replaced. Dirt, gravel, air, etc. are left untouched.

---

## 5. Key Constants and Ranges

| Parameter | Formula | Typical range |
|---|---|---|
| theta | `rand.nextFloat() * PI` | [0, π) radians — vein lies at random XZ angle |
| Axis XZ spread | `b / 8.0` blocks from center | For b=16: ±2 blocks; b=32: ±4 blocks |
| Y spread | `rand.nextInt(3) - 2` per endpoint | Start/end Y independently offset by [-2, 0] |
| randFactor | `rand.nextDouble() * b / 16.0` | For b=7: [0, 0.4375); b=16: [0, 1.0) |
| Min diameter | 1.0 (when sinePhase or randFactor = 0) | Always at least radius 0.5 per step |
| Peak diameter | `(2.0) * (b/16) + 1.0` (at midpoint) | For b=7: ~1.875; b=32: ~5.0 |

---

## 6. Ore Table (from BiomeDecorator — ChunkProviderGenerate_Spec §9)

| Ore | `ky` args | Vein size | Attempts/chunk | Y range |
|---|---|---|---|---|
| Dirt | `ky(dirt_id, 32)` | 32 | 20 | 0–128 |
| Gravel | `ky(gravel_id, 32)` | 32 | 10 | 0–128 |
| Coal | `ky(coal_id, 16)` | 16 | 20 | 0–128 |
| Iron | `ky(iron_id, 8)` | 8 | 20 | 0–64 |
| Gold | `ky(gold_id, 8)` | 8 | 2 | 0–32 |
| Redstone | `ky(redstone_id, 7)` | 7 | 8 | 0–16 |
| Diamond | `ky(diamond_id, 7)` | 7 | 1 | 0–16 |
| Lapis | `ky(lapis_id, 6)` | 6 | 1 | triangular ~Y16 |

Block IDs (from BlockRegistry_Spec):
- Dirt: `yy.v.bM` = 3
- Gravel: `yy.F.bM` = 13
- Coal Ore: `yy.I.bM` = 16
- Iron Ore: `yy.H.bM` = 15
- Gold Ore: `yy.G.bM` = 14
- Redstone Ore: `yy.aN.bM` = 73
- Diamond Ore: `yy.aw.bM` = 56
- Lapis Ore: `yy.N.bM` = 21

---

## 7. Quirks to Preserve

- The `+8` offset on startX/endX/startZ/endZ centres the vein within the chunk-relative area
  rather than at the exact call position. This distributes veins more evenly when called with
  X/Z in [chunkOrigin, chunkOrigin+15].
- Both Y endpoints are independently random, so the vein can have a slight vertical tilt.
- All placement uses `world.d()` (silent) — no neighbor block-update notifications.
- The method always returns `true`, even if it placed zero blocks.

---

*Spec written by Analyst AI from `ky.java` (56 lines). No C# implementation consulted.*
*(Proactive — not yet requested by Coder)*
