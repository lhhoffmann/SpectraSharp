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

# Snow Layer / Ice / Cold-Biome Generation Spec
Source classes: `aif.java` (BlockSnow, ID 78), `ahq.java` (BlockIce, ID 79)
Generation: inline in `xj.java` (ChunkProviderGenerate.populateChunk), via `ry.p()` and `ry.r()`

Superclasses: `aif` extends `yy` (Block); `ahq` extends `aaf`

---

## 1. Purpose

BlockSnow (`aif`, ID 78) is the thin snow layer placed on top of solid surface blocks in cold
biomes during chunk population. BlockIce (`ahq`, ID 79) freezes surface water in those same
biomes.

Both are placed **at the end of `ChunkProviderGenerate.populateChunk`**, after all biome
decoration has been applied. They are not a separate `WorldGenerator` — they are an inline
16×16 pass over the chunk surface.

---

## 2. BlockSnow (`aif`) — Fields

| Field (obf) | Value / Type | Semantics |
|---|---|---|
| `bM` | 78 | block ID |
| material | `p.u` | snow material |
| texture index | 66 | terrain.png |
| AABB base | (0, 0, 0, 1, 0.125, 1) | initial 2/16 height — overwritten by layer count |
| `b(true)` | — | `setBlocksMovement(true)` |

**No custom fields** beyond Block base. Layer count is stored in chunk metadata (3 bits, value 0–7).

---

## 3. BlockSnow — Constants & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `0.125F` (= 2/16) | AABB init | base height for one snow layer |
| `(2 * (1 + layers)) / 16.0F` | setBlockBounds | height formula per layer; layer 0 = 2/16, layer 7 = 16/16 (full block) |
| `3` | collision AABB threshold | layers ≥ 3 have collision (height up to 0.5F); layers 0–2 have none (returns null) |
| `0.5F` | collision AABB max Y | collision ceiling when layers ≥ 3 |
| `> 11` | randomTick melt threshold | melts when block light exceeds 11 |
| `0.7F` | harvest item scatter | jitter range for snowball spawn position |
| `10` | EntityItem pickup delay | harvested snowball cannot be picked up for 10 ticks |

---

## 4. BlockSnow — Methods

### canBlockStay (`c(ry, int, int, int)`)
**Called by:** `onNeighborBlockChange`, `world.r()` (snow placement check)

1. Get blockId at (x, y-1, z) = `var5`
2. If `var5 == 0` (air below): return false
3. If `yy.k[var5].a()` (block below renders-as-normal): return `world.e(x, y-1, z).d()` (material is solid)
4. Else: return false

→ Snow can stay only on solid, render-as-normal blocks (not glass, not slabs, not fences etc.).

### setBlockBoundsBasedOnState (`b(kq, int, int, int)`)
Called before collision/visual AABB use.

```
layers = world.getBlockMetadata(x, y, z) & 7
height = (2 * (1 + layers)) / 16.0F
setBounds(0, 0, 0, 1, height, 1)
```

Layer → visual height table:
| meta & 7 | height (blocks) | height (units/16) |
|---|---|---|
| 0 | 2/16 = 0.125 | 2 |
| 1 | 4/16 = 0.250 | 4 |
| 2 | 6/16 = 0.375 | 6 |
| 3 | 8/16 = 0.500 | 8 |
| 4 | 10/16 = 0.625 | 10 |
| 5 | 12/16 = 0.750 | 12 |
| 6 | 14/16 = 0.875 | 14 |
| 7 | 16/16 = 1.000 | 16 |

### getCollisionBoundingBoxFromPool (`b(ry, int, int, int)`)
1. `layers = world.getBlockMetadata(x, y, z) & 7`
2. If `layers >= 3`: return AABB(x+bR, y+bS, z+bT, x+bU, y+0.5F, z+bW) — **collision up to 0.5F**
3. Else: return **null** — no collision for thin snow (0–2 layers)

### isOpaqueCube (`a()`) → `false`
### renderAsNormalBlock (`b()`) → `false`

### onNeighborBlockChange (`a(ry, int, int, int, int)`)
Calls `g(world, x, y, z)` = stability check.

### stability check (`g(ry, int, int, int)`)
1. If `!canBlockStay(world, x, y, z)`:
   - `this.b(world, x, y, z, world.getBlockMetadata(x,y,z), 0)` — drop items from metadata
   - `world.g(x, y, z, 0)` — set block to air
   - Return false
2. Else: return true

### harvestBlock / onBlockHarvested (`a(ry, vi, int, int, int, int)`)
Called when player breaks the snow layer.

1. `var7 = acy.aC.bM` = snowball item ID (= 256 + 76 = 332)
2. Scatter within block: `var9 = rand * 0.7 + 0.15`, `var11` similarly, `var13` similarly (jitter = nextFloat() * 0.7 + 0.15 per axis)
3. Spawn `EntityItem` at `(x + var9, y + var11, z + var13)` with `ItemStack(snowball, count=1, damage=0)`
4. Set pickup delay `var15.c = 10`
5. `world.a(var15)` — add entity to world
6. `world.g(x, y, z, 0)` — remove snow block
7. `player.a(ny.C[bM], 1)` — add mining stat

**Note:** The item count is always exactly **1** snowball regardless of layer count. The meta value for layering is not used to compute drop count in this method.

### getItemDropped (`a(int, Random, int)`) → `acy.aC.bM` (snowball)
### quantityDropped (`a(Random)`) → `0`
The standard drop mechanism yields nothing (quantityDropped=0). All drops are via `harvestBlock`.

### randomTick (`a(ry, int, int, int, Random)`)
```
if world.getBlockLight(x, y, z) > 11:
    this.b(world, x, y, z, metadata, 0)   // standard item drop (yields 0 items)
    world.g(x, y, z, 0)                    // remove block (melt)
```
Snow melts silently with no item drop (quantityDropped=0 applies here).

### isBlockSolidOnSide (`a_(kq, int, int, int, int)`)
Returns `true` if face == 1 (top), else `super.a_()`.  
→ Snow is solid on its top face only (allows block placement on top).

---

## 5. BlockIce (`ahq`) — Fields

| Field (obf) | Value / Type | Semantics |
|---|---|---|
| `bM` | 79 | block ID |
| material | `p.t` | ice material |
| texture index | 67 | terrain.png |
| `ca` | `0.98F` | slipperiness (normal blocks = 0.6F; ice is extremely slippery) |
| `b(true)` | — | setBlocksMovement(true) |
| superclass | `aaf` | likely abstract transparent/glass block base |

---

## 6. BlockIce — Constants & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `0.98F` | `ca` field | ice friction coefficient — entity speed multiplied by this instead of 0.6F |
| `> 11 - yy.o[bM]` | randomTick melt | threshold = 11 minus ice's light opacity |
| `yy.o[79]` | opacity | ice opacity as registered; `h()` returns 1 → `yy.o[79] = 1` |
| melt threshold | `11 - 1 = 10` | ice melts when block light > 10 |
| `yy.B.bM` | ID 9 | still water — placed when ice melts by randomTick |
| `yy.A.bM` | ID 8 | flowing water — placed when ice is mined over liquid/air |

---

## 7. BlockIce — Methods

### lightOpacity (`h()`) → `1`
Ice is slightly opaque — allows some light through but less than air.

### isBlockSolidOnSide (`a_(kq, int, int, int, int)`)
Calls `super.a_(world, x, y, z, 1 - face)` — inverted face lookup.

### harvestBlock (`a(ry, vi, int, int, int, int)`)
1. Call `super.a(...)` — standard drop sequence (yields nothing since `quantityDropped=0`)
2. Get material of block at (x, y-1, z) = `var7`
3. If `var7.d()` (is liquid material) OR `var7.a()` (is air):
   - `world.g(x, y, z, yy.A.bM)` — place flowing water (ID 8)
   - (ice over air/liquid → water appears when mined)
4. Else: block below is solid → place nothing (block is simply removed by super)

### quantityDropped (`a(Random)`) → `0`
Ice always drops nothing.

### randomTick (`a(ry, int, int, int, Random)`)
```
opacityMod = yy.o[this.bM]   // = 1 for ice
if world.getBlockLight(x, y, z) > (11 - opacityMod):   // > 10
    this.b(world, x, y, z, metadata, 0)   // drop (nothing)
    world.g(x, y, z, yy.B.bM)             // replace with still water (ID 9)
```

### `i()` → `0`
Unknown override — possibly `getTickRate()` or `getLightValue()`; returns 0.

---

## 8. Cold-Biome Generation — Inline Pass

**Location:** End of `xj.populateChunk` (`xj.java`), after `BiomeDecorator.decorate` and `SpawnerAnimals.initialPopulate` calls.

**This is NOT a WorldGenerator class.** It is a 16×16 inline loop at the end of chunk population.

### Algorithm

```
for x in [0, 16):
    for z in [0, 16):
        worldX = chunkOriginX + x
        worldZ = chunkOriginZ + z
        surfaceY = world.getHeightValue(worldX, worldZ)   // world.e(worldX, worldZ)

        // Ice pass — checks the surface block itself
        if world.p(worldX, surfaceY - 1, worldZ):
            world.g(worldX, surfaceY - 1, worldZ, yy.aT.bM)   // place ice (ID 79)

        // Snow pass — checks the air above the surface
        if world.r(worldX, surfaceY, worldZ):
            world.g(worldX, surfaceY, worldZ, yy.aS.bM)        // place snow layer (ID 78)
```

`world.g()` = setBlockWithoutNotify (silent set, no neighbour updates).

---

## 9. World Methods — `ry.p()` and `ry.r()`

### `p(int x, int y, int z)` — canFreezeAtLocation
Calls `this.c(x, y, z, false)`.

### `c(int x, int y, int z, boolean fullWaterCheck)` — shared freeze test

```
temp = this.a().a(x, y, z)   // WorldChunkManager.getTemperatureAtHeight
if temp > 0.15F: return false   // too warm

if y in [0, worldHeight) AND blockLight(x, y, z) < 10:
    blockId = world.getBlockId(x, y, z)
    if (blockId == yy.B.bM (still water, ID 9) OR blockId == yy.A.bM (flowing water, ID 8))
       AND world.getBlockMetadata(x, y, z) == 0:

        if NOT fullWaterCheck: return true   // p() path — simple check

        // fullWaterCheck path (used by q()):
        // Returns true only if at least one horizontal neighbour is NOT water
        allNeighborsWater = true
        if material(x-1, y, z) != p.g: allNeighborsWater = false
        if material(x+1, y, z) != p.g: allNeighborsWater = false
        if material(x, y, z-1) != p.g: allNeighborsWater = false
        if material(x, y, z+1) != p.g: allNeighborsWater = false
        if NOT allNeighborsWater: return true   // edge water freezes
        // center of pool → falls through to false

return false
```

**For ice placement (`p()`):** freezes any surface water (still or flowing, meta=0) when temperature ≤ 0.15 and block light < 10.

**Temperature thresholds:** `WorldChunkManager.getTemperatureAtHeight(x, y, z)` adjusts for altitude — higher elevations are colder. Cold biomes (ice plains, ice mountains, tundra) have base temp < 0.15. Warm biomes (desert, jungle) have temp >> 0.15.

### `r(int x, int y, int z)` — canSnowAtLocation

```
temp = this.a().a(x, y, z)   // WorldChunkManager.getTemperatureAtHeight
if temp > 0.15F: return false

if y in [0, worldHeight) AND blockLight(x, y, z) < 10:
    blockBelow = world.getBlockId(x, y-1, z)
    currentBlock = world.getBlockId(x, y, z)

    if currentBlock == 0 (air)
       AND yy.aS.c(world, x, y, z)   // BlockSnow.canBlockStay at this position
       AND blockBelow != 0 (not air)
       AND blockBelow != yy.aT.bM (not ice, ID 79)
       AND yy.k[blockBelow].bZ.d() (block-below material is solid):
        return true

return false
```

**Key note:** `yy.aS.c(world, x, y, z)` calls `aif.canBlockStay()` at the candidate position — requires block below to be non-air, render-as-normal, and have solid material. The additional check `blockBelow != yy.aT.bM` prevents snow on ice. This means: snow cannot stack directly on ice, only on solid opaque blocks.

---

## 10. Bitwise & Data Layouts

Snow layer metadata (3 bits):
```
Bits [2..0] = layer count (0–7)
Bits [7..3] = unused (always 0 in 1.0)
```

Layer 0 = single layer (2/16 height). Layer 7 = full-block height (16/16). In 1.0, snow is always placed at layer 0 (single layer) by the chunk population pass.

Ice has no meaningful metadata.

---

## 11. Tick Behaviour

| Block | Ticked? | Trigger | Effect |
|---|---|---|---|
| Snow layer (`aif`) | Yes (random tick) | blockLight > 11 | Silently remove (no item drop) |
| Ice (`ahq`) | Yes (random tick) | blockLight > 10 (= 11 − opacity 1) | Replace with still water (ID 9) |

Both use the **block light** value (not sky light directly). In practice, a block exposed to sunlight and not shadowed has block light 0 or very low, so the threshold effectively means "exposed to artificial light > 11 or 10 respectively."

---

## 12. Known Quirks / Bugs to Preserve

1. **Snow melts silently** (random tick path): The `randomTick` in `aif` calls the standard drop method which yields 0 items, so melted snow drops nothing. Snow mined by player always drops 1 snowball regardless of layer count.
2. **Ice melts to still water (ID 9)**, not flowing water — the randomTick path places `yy.B` (still). When mined over air/liquid, `harvestBlock` places `yy.A` (flowing water, ID 8).
3. **No snow on ice** (quirk in `r()`): The `blockBelow != yy.aT.bM` guard prevents snow from generating on ice surfaces. This can leave gaps at frozen lake shores (lake is frozen, but adjacent land may still get snow).
4. **Snow only at layer 0 at generation**: The chunk population pass always places a single layer (meta=0). Multi-layer snow in 1.0 is not generated by the world generator.
5. **Ice generation uses `p()` not `q()`**: The simple check — all surface water cells freeze, not just edge cells. The full-neighbor-check variant `q()` is called from somewhere else (unknown — see Open Questions §13).
6. **Silent block set** (`world.g()`): Both ice and snow placement use `setBlockWithoutNotify` — no neighbor updates triggered at generation time. Neighbor-update cascades happen only after the player interacts.

---

## 13. Open Questions

1. **`world.q(x,y,z)` caller**: The full-water-check variant is defined but not called from ChunkProviderGenerate. It may be used by a weather/time tick system (overnight freezing). Confirm call site.
2. **`aaf` superclass of `ahq`**: Not traced. Likely an abstract transparent-block class. Confirm whether `aaf` provides any extra fields relevant to ice rendering (e.g., translucency flag).
3. **`p.t` ice material**: Confirm `isSolid()` and `isLiquid()` return values for ice material — matters for snow placement on top (`bZ.d()` check).
4. **`p.u` snow material**: Same — confirm solidity flags for snow material, which affects what blocks snow can be placed on top of.
5. **Multi-layer snow accumulation**: Is there any tick/weather system in 1.0 that raises the layer count above 0 over time, or is multi-layer snow only achievable via commands/creative placement?
