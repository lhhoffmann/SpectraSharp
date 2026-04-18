# Spec: WorldGenLakes

**Java class:** `qv extends ig`
**Status:** PROVIDED
**Canonical name:** WorldGenLakes

---

## Overview

`WorldGenLakes` carves lake-shaped cavities into the terrain and fills them with a fluid (water
or lava). It uses overlapping ellipsoids in a 16×16×8 working space to produce irregular,
natural-looking lake shapes.

Called from `xj` (ChunkProviderGenerate) during chunk population:
- **Water lakes:** 1 in 4 chunks. Block ID = `yy.B.bM` (Water)
- **Lava lakes:** 1 in 8 chunks. Block ID = `yy.D.bM` (Lava), restricted to underground
  (y < seaLevel or 1/10 chance to spawn at any height)

---

## Algorithm

### Step 1 — Find ground level

Starting at y=255, scan downward until a non-air, non-leaf block is found.
Subtract 4 from the found y → `baseY`.
If `baseY` ≤ 0, abort generation.

### Step 2 — Working space

Allocate `boolean[2048]` representing a `16 × 16 × 8` voxel volume (x∈[0,15], y∈[0,7], z∈[0,15]).

### Step 3 — Place ellipsoids

Repeat **4 to 7 times** (random):

Generate one ellipsoid with random parameters:
```
centerX = rand(15) + 0.5
centerY = rand(7)  + 0.5
centerZ = rand(15) + 0.5
radiusX = rand(7)  + 3.0   → [3.0, 9.0]
radiusY = rand(5)  + 2.0   → [2.0, 6.0]
radiusZ = rand(7)  + 3.0   → [3.0, 9.0]
```

Mark all cells inside the ellipsoid as `true`:
```
for each cell (x, y, z):
    dx = (x - centerX) / radiusX
    dy = (y - centerY) / radiusY
    dz = (z - centerZ) / radiusZ
    if dx²+dy²+dz² < 1.0: mark cell
```

### Step 4 — Validity check

**Top half (y ≥ 4):** No marked cell may be open-air in the world at `(chunkX*16+x, baseY+y, chunkZ*16+z)`.
If any top-half marked cell is open air → abort generation.

**Bottom half (y < 4):** The border ring (x=0, x=15, z=0, z=15, or y=0) must all be solid
(non-air, non-fluid) in the world. This ensures the lake has containment walls.

### Step 5 — Carve and fill

For each marked cell `(x, y, z)`:
- If `y < 4`: place fluid block (`a` = the fluid block ID passed to constructor)
- If `y ≥ 4`: place Air

This creates a bowl shape: fluid in the bottom 4 layers, air carved above.

### Step 6 — Post-processing

For each cell adjacent to fluid placements:
- **Grass → Dirt** or **Mycelium → Dirt**: if the block directly above is water, convert surface
- **Lava + above is air at surface level**: place Fire (`yy.w`)
- **Water at surface (y = surfaceY)**: check temperature → place Ice (`yy.ae`) if cold biome

---

## Spawn Conditions (from `xj`)

```java
// Water lake
if (rand.nextInt(4) == 0) {
    new qv(yy.B.bM).a(world, rand, chunkX*16 + rand(16), rand(128), chunkZ*16 + rand(16));
}

// Lava lake
if (rand.nextInt(8) == 0) {
    int lakeY = rand(rand(120) + 8);  // biased low
    if (lakeY < seaLevel || rand.nextInt(10) == 0) {
        new qv(yy.D.bM).a(world, rand, chunkX*16 + rand(16), lakeY, chunkZ*16 + rand(16));
    }
}
```

Lava Y is doubly random: `rand(rand(120)+8)` heavily biases toward y=0–30.

---

## C# Mapping

| Java | C# |
|---|---|
| `qv` | `WorldGenLakes` |
| `qv(int fluidId)` | `WorldGenLakes(BlockId fluidBlock)` |
| `qv.a(ry, Random, int, int, int)` | `WorldGenLakes.Generate(IWorld world, Random rng, BlockPos origin)` |
| Working array `boolean[2048]` | `bool[16, 8, 16]` or flat `Span<bool>` with index math |
| `yy.B.bM` | `BlockRegistry.Water.Id` |
| `yy.D.bM` | `BlockRegistry.Lava.Id` |

---

## Open Questions

- Exact seaLevel constant used in lava check (likely 64).
- Whether post-processing (grass→dirt, ice) is done by `qv` itself or the ChunkProvider.
- Mycelium handling — present in 1.0 or mushroom island is later?
