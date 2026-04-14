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

# BlockCrops + BlockFarmland Spec
**Source classes:** `aha.java` (BlockCrops), `ni.java` (BlockFarmland)
**Superclasses:** `aha` → `wg` (BlockFlower) → `yy` (Block); `ni` → `yy` (Block)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## Part A — BlockCrops (`aha`, ID 59)

---

### A.1 Purpose

`aha` (BlockCrops) implements wheat growth (ID 59). Growth stage 0–7 is stored in
block metadata. At stage 7 the crop is fully grown and drops wheat + seeds. Random-tick
growth requires light level ≥ 9 above the crop, farmland below, and a probability roll
based on a computed growth factor that rewards surrounding moist farmland.

---

### A.2 Superclass: `wg` (BlockFlower)

`aha` extends `wg` (BlockFlower). Relevant `wg` behaviour inherited or overridden:

- `wg` constructor: calls `Block(blockId, p.j)` — plant material. Sets AABB to
  `(0.3, 0, 0.3, 0.7, 0.6, 0.7)` (narrow flower shape). `aha` overrides this.
- `wg.d(int blockId)` — canBlockSurviveOn: allows grass (yy.u), dirt (yy.v), or farmland
  (yy.aA). `aha` overrides this to allow **only** farmland.
- `wg.c(ry, x, y, z)` — canBlockStay: `super.c() && this.d(blockBelowId)`.
- `wg.e(ry, x, y, z)` — canSurvive: needs light ≥ 8 OR sky access, AND valid block below.
- `wg.h(ry, x, y, z)` — checkAndDropBlock: if `!canSurvive`, drops item and removes block.
- `wg.a(ry, x, y, z, Random)` — random tick: calls `h()` (stability check).
- `wg.b(ry, x, y, z)` — getCollisionBoundingBox: returns null (no collision — plants are
  walk-through).

---

### A.3 Constructor

```
aha(int blockId, int textureIndex)
```

1. Calls `super(blockId, textureIndex)` — invokes `wg(blockId, textureIndex)`.
2. Sets `this.bL = textureIndex` (redundant with wg; explicit for clarity).
3. Calls `b(true)` — marks block as needing random tick.
4. Sets AABB: `a(0.0, 0.0, 0.0, 1.0, 0.25, 1.0)` — full XZ, 0.25 tall (quarter block).
   This overrides the narrower flower AABB set by `wg`.

---

### A.4 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `bL` (inherited) | int | textureIndex | Base texture atlas offset; texture for stage N = bL + N |

No additional instance fields beyond Block base and `wg`.

---

### A.5 Methods — Detailed Logic

#### `d(int blockId)` — canBlockSurviveOn (override)

```
return blockId == yy.aA.bM     // farmland block ID only (ID 60)
```

Overrides `wg.d()` which allows grass/dirt/farmland. Crops require farmland exclusively.

#### `a(ry world, int x, int y, int z, Random rng)` — randomTick

```
super.a(world, x, y, z, rng)            // wg's stability check (drops if invalid)

if world.getLightBrightness(x, y+1, z) >= 9:
    stage = world.getMetadata(x, y, z)
    if stage < 7:
        factor = j(world, x, y, z)      // compute growth factor
        if rng.nextInt((int)(25.0F / factor) + 1) == 0:
            world.setMetadata(x, y, z, stage + 1)
```

Light check uses `var1.n(x, y+1, z) >= 9` — the block **above** the crop at y+1 must have
light level ≥ 9. (`n()` = getLightBrightness or equivalent; exact method name see Open Questions.)

Stage 7 crops do not grow further.

#### `j(ry world, int x, int y, int z)` — computeGrowthFactor (private)

Returns a float ≥ 1.0 representing how favourable growing conditions are. Higher factor =
faster growth.

**Step 1 — Survey adjacent crop presence:**
```
westCrop  = block at (x-1, y, z) == this crop type (bM)
eastCrop  = block at (x+1, y, z) == this crop type (bM)
southCrop = block at (x, y, z-1) == this crop type (bM)
northCrop = block at (x, y, z+1) == this crop type (bM)

axisX = westCrop  OR eastCrop
axisZ = southCrop OR northCrop
diag  = NW-crop OR NE-crop OR SE-crop OR SW-crop
        (all four diagonals: (x±1, y, z±1))
```

**Step 2 — Accumulate farmland score:**

```
score = 1.0F

for bx in (x-1, x, x+1):
    for bz in (z-1, z, z+1):
        blockBelow = block at (bx, y-1, bz)
        contribution = 0.0F
        if blockBelow == farmland (yy.aA.bM):
            contribution = 1.0F
            if metadata(bx, y-1, bz) > 0:    // moist farmland
                contribution = 3.0F
        if (bx, bz) != (x, z):               // neighbour, not own block
            contribution /= 4.0F
        score += contribution
```

The centre tile (the crop's own farmland) contributes its full value (1.0 dry, 3.0 moist).
Each of the 8 surrounding tiles contributes 1/4 of its value (0.25 dry, 0.75 moist).

**Step 3 — Crowding penalty:**

```
if diag OR (axisX AND axisZ):
    score /= 2.0F
```

If any diagonal neighbour has the same crop type, OR if both perpendicular axes have
neighbours, the growth factor is halved.

**Return:** `score`

**Growth probability per random tick:** `1 / ((int)(25.0F / score) + 1)`

| Scenario | score | probability |
|---|---|---|
| Own dry farmland only | 1.0 | 1/26 ≈ 3.8% |
| Own moist farmland only | 3.0 | 1/9 ≈ 11.1% |
| Own moist + 4 axis moist neighbours | 3.0 + 4×0.75 = 6.0 | 1/5 = 20% |
| Own moist + all 8 moist neighbours | 3.0 + 8×0.75 = 9.0 | 1/3 ≈ 33.3% |
| Above, no crowding penalty | same | same |
| With crowding penalty (÷2) | halved | lower |

The integer cast `(int)(25.0F / score)` truncates toward zero before adding 1.

#### `g(ry world, int x, int y, int z)` — instantGrow (bonemeal)

```
world.setMetadata(x, y, z, 7)    // directly set to stage 7 (fully grown)
```

#### `a(int face, int meta)` — getTextureIndex

```
if meta < 0: meta = 7        // negative meta: use fully-grown texture (inventory render)
return bL + meta             // texture = base + growth stage (0–7)
```

Eight consecutive texture atlas slots starting at `bL` represent stages 0–7.

#### `c()` — lightOpacity

Returns 6. Crops partially block light.

#### `a(ry world, int x, int y, int z, int meta, float damage, int fortune)` — harvestBlock

Drops seeds as item entities. Server-side only.

```
super.a(world, x, y, z, meta, damage, 0)    // fortune passed as 0 to super

if !world.isRemote:
    attempts = 3 + fortune
    for i in 0..attempts-1:
        if world.random.nextInt(15) <= meta:  // probability = (meta+1)/16 … approx
            // spawn item entity at random position within block:
            rx = 0.3F + world.random.nextFloat() * 0.4F + x
            ry_pos = 0.3F + world.random.nextFloat() * 0.4F + y
            rz = 0.3F + world.random.nextFloat() * 0.4F + z
            entity = new ih(world, rx, ry_pos, rz, new dk(seedsItem))   // ih = EntityItem
            entity.c = 10    // pickup delay ticks
            world.addEntity(entity)
```

`seedsItem` = `acy.R` (wheat seeds, item ID 295).

Each of the (3 + fortune) attempts succeeds with probability `(meta / 15)`. At stage 7:
chance = 7/15 ≈ 46.7% per attempt, mean ≈ 1.4 seeds from fortune-0 base.

#### `a(int meta, Random rng, int fortune)` — getItemDropped

```
if meta == 7: return acy.S.bM     // wheat item ID (296)
else:          return -1           // no item drop (only seeds via harvestBlock)
```

Only stage-7 crops drop wheat. All other stages drop only seeds (via `harvestBlock` above).

#### `a(Random rng)` — quantityDropped

Returns 1. Always drop 1 wheat if stage 7.

---

### A.6 Bitwise & Data Layouts

```
Metadata (3 bits):
  Bits 2..0 = growth stage 0–7
  Stage 0 = just planted (smallest)
  Stage 7 = fully grown (harvestable; drops wheat)
  No other bits used.
```

---

### A.7 Tick Behaviour

Ticked by the random-tick system. `b(true)` in constructor enables random ticks.
Each random tick: stability check (wg.h) → light check → probability roll → optional grow.

---

### A.8 Known Quirks / Bugs to Preserve

1. **Full XZ AABB (0,0,0,1,1,0.25):** Unlike the flower parent's narrow AABB, crops use
   full block width. Despite this, `wg.b(ry)` returns null for collision — crops have no
   physical collision. The AABB set in the constructor is used only for selection/rendering.

2. **Fortune is passed as 0 to super:** `super.a(..., 0)` ignores the `fortune` parameter
   for the base drop. Fortune only affects seed drop count (via the loop).

3. **Seed probability uses `<= meta` not `< meta`:** `rng.nextInt(15) <= meta` means:
   at stage 0 → 1/16 seed chance, at stage 7 → 8/16 = 50% chance. The number of values
   `0..meta` inclusive from range `0..14` is `(meta+1)`.

4. **`wg.b(ry)` returns null:** Crops have no collision AABB — entities walk through them.
   The block AABB set in the constructor is purely for the selection highlight.

---

### A.9 Open Questions

1. **`var1.n(x, y+1, z)`:** The light check uses method `n()` on the world. This is likely
   `getBlockLightValue()`, `getLightFromNeighbors()`, or `getFullBlockLightOpacity()`. The
   exact method determines whether it measures block light, sky light, or combined. The
   threshold of 9 must be checked against the correct light channel. Confirm from World spec.

2. **TextureIndex for ID 59:** The `aha` constructor receives `textureIndex` as second arg.
   The block registration in `acy.java` sets the specific value. Stages 0–7 texture = bL + 0..7.
   Verify the exact base index from the block registration.

---

## Part B — BlockFarmland (`ni`, ID 60)

---

### B.1 Purpose

`ni` (BlockFarmland) is the tilled soil beneath crops. Its metadata (0–7) encodes moisture
level: 0 = dry, 1–7 = increasingly moist. On each random tick it checks for nearby water
to maintain or increase moisture; without water it dries out one step per tick. When fully
dry (metadata 0) and no crops above, it reverts to dirt (ID 3). Entity footsteps have a 25%
chance per step to trample farmland back to dirt. Placing a liquid directly above triggers
immediate reversion to dirt.

---

### B.2 Constructor

```
ni(int blockId)
```

1. Calls `super(blockId, p.c)` — earth/ground material.
2. Sets `bL = 87` (texture index base).
3. Calls `b(true)` — requires random tick.
4. Sets block AABB: `a(0.0, 0.0, 0.0, 1.0, 0.9375, 1.0)` — full XZ, height = 15/16.
5. Calls `h(255)` — neighbor-max light (same as slabs and stairs).

---

### B.3 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `bL` (inherited) | int | 87 | Base texture index: 87=dry top, 86=moist top, 2=side |

No additional instance fields.

---

### B.4 Methods — Detailed Logic

#### `a()` — isOpaqueCube

Returns false.

#### `b()` — renderAsNormal

Returns false.

#### `b(ry world, int x, int y, int z)` — getCollisionBoundingBoxFromPool

```
return AABB(x, y, z, x+1, y+1, z+1)    // full-cube world-space collision
```

Despite the visual/selection AABB being 15/16 height (set in constructor), the collision
AABB returned here is always a full 1×1×1 block. Entities do not sink into farmland.

#### `a(int face, int meta)` — getTextureIndex

```
if face == 1 (top):
    if meta > 0: return bL - 1 = 86     // moist top texture (87-1)
    else:        return bL = 87          // dry top texture
else:
    return 2                             // all side faces: standard dirt texture (index 2)
```

#### `a(ry world, int x, int y, int z, Random rng)` — randomTick

```
if h(world, x, y, z) OR isWaterAbove(world, x, y+1, z):
    world.setMetadata(x, y, z, 7)        // max moisture
else:
    meta = world.getMetadata(x, y, z)
    if meta > 0:
        world.setMetadata(x, y, z, meta - 1)    // dry out one step
    else:                                        // fully dry (meta == 0)
        if !g(world, x, y, z):                  // no crops above
            world.setBlock(x, y, z, yy.v.bM)   // revert to dirt (ID 3)
```

`h(world, x, y, z)` — water range check (see below).
`isWaterAbove` — `var1.w(x, y+1, z)` — exact method unclear; see Open Questions.

Drying timeline: without water, farmland loses 1 moisture per random tick. At maximum
moisture (7), it takes at least 7 random ticks to dry out completely.

#### `h(ry world, int x, int y, int z)` — isWaterNearby (private)

Checks a 9×2×9 area (4-block XZ radius, at Y and Y+1) for water material:

```
for bx in (x-4)..(x+4):
    for by2 in y..(y+1):
        for bz in (z-4)..(z+4):
            if world.getMaterial(bx, by2, bz) == p.g (water material):
                return true
return false
```

Returns true if any water block (water material `p.g`) exists within the search volume.
The search covers the farmland's own Y level and one level above it.

#### `g(ry world, int x, int y, int z)` — hasCropsAbove (private)

Checks if the block directly above is a valid crop type:

```
var5 = 0 (byte)
for bx in (x-0)..(x+0):      // loop runs exactly once: bx = x
    for bz in (z-0)..(z+0):  // loop runs exactly once: bz = z
        blockAbove = world.getBlockId(bx, y+1, bz)
        if blockAbove == yy.az.bM (wheat crops, 59)
           OR blockAbove == yy.bt.bM (melon stem, 106)
           OR blockAbove == yy.bs.bM (pumpkin stem, 105):
            return true
return false
```

Despite the loop structure, `var5 = 0` means the search range is 0 — only the single block
directly above is checked. This is equivalent to a direct comparison.

The three crop types that prevent farmland from reverting to dirt:
- `yy.az` = wheat crops (ID 59, class `aha`)
- `yy.bt` = melon stem (ID 106)
- `yy.bs` = pumpkin stem (ID 105)

#### `b(ry world, int x, int y, int z, ia entity)` — onEntityWalking (trampling)

```
if world.random.nextInt(4) == 0:
    world.setBlock(x, y, z, yy.v.bM)    // 25% chance: revert to dirt
```

Any entity walking on farmland has a 25% chance per step to convert it back to dirt.
This check uses `world.w` (the world's Random field).

#### `a(ry world, int x, int y, int z, int neighborId)` — onNeighborBlockChange

```
super.a(world, x, y, z, neighborId)
material = world.getMaterial(x, y+1, z)
if material.isLiquid():                   // p.b() = isLiquid
    world.setBlock(x, y, z, yy.v.bM)     // revert to dirt immediately
```

If a liquid block is placed directly above the farmland, it reverts to dirt on the
same tick as the neighbor-change event.

#### `a(int meta, Random rng, int fortune)` — getItemDropped

```
return yy.v.a(0, rng, fortune)    // dirt block's getItemDropped(meta=0, ...)
```

Farmland always drops as dirt (ID 3) with the same quantity as a dirt block.

---

### B.5 Bitwise & Data Layouts

```
Metadata (3 bits):
  Bits 2..0 = moisture level:
    0 = dry (reverts to dirt if no crops above)
    1–6 = intermediate moisture (no visual change; only texture changes at >0)
    7 = maximum moisture (moist top texture, bL-1 = 86)
```

The moist vs dry texture uses only the binary: `meta > 0` → moist (86), `meta == 0` → dry (87).
Moisture levels 1–6 visually appear identical to level 7.

---

### B.6 Tick Behaviour

- **Random tick:** moisture management — increase to 7 if water nearby, else dry out one step,
  or revert to dirt if fully dry with no crops.
- **Entity walk:** 25% chance to trample to dirt per step.
- **Neighbor change:** immediate reversion if liquid placed above.

---

### B.7 AABB Summary

| Context | AABB |
|---|---|
| Visual / selection (constructor) | (0, 0, 0) → (1, 0.9375, 1) — 15/16 tall |
| Collision (`b(ry)` override) | (x, y, z) → (x+1, y+1, z+1) — full cube |

`0.9375 = 15/16`.

Entities collide at full height (cannot step over) but the selection highlight shows the
slightly lower profile.

---

### B.8 Known Quirks / Bugs to Preserve

1. **`g()` loop over zero range:** The `hasCropsAbove` check uses a loop with `var5 = 0`
   (byte). The loop body `for bx = x-0 to x+0` iterates exactly once. The loop structure
   is a decompiler artefact of what could be a simple single-block check. Preserve the
   single-block-above semantics.

2. **Moisture levels 1–6 are visually identical to 7:** The texture function only branches
   on `meta > 0`, not on the exact value. Only stage 0 (dry) shows the dry texture. All
   other stages (1–7) show the moist texture. The 7-step moisture decay is mechanical only.

3. **Trampling uses `world.w.nextInt(4)`:** The RNG call is directly on the world's random
   field (`ry.w`), not a passed-in `Random` argument. This is consistent with other 1.0
   entity/block interactions but means trampling can affect the random seed for subsequent
   world operations on the same tick.

4. **`h(255)` = neighbor-max light:** Farmland calls `h(255)` in constructor, same as slabs
   and stairs. Crops growing above it will not create dark patches at ground level.

5. **Collision box is full height but visual is 15/16:** `b(ry)` returns `AABB(x,y,z,x+1,y+1,z+1)`.
   Players standing on farmland do not sink in. The 15/16 AABB is purely cosmetic.

---

### B.9 Open Questions

1. **`var1.w(x, y+1, z)` — the second hydration condition:** In the random tick, the condition
   `var1.w(x, y+1, z)` is checked alongside `h()` as a reason to set moisture to 7. This call
   takes block coordinates, implying it tests something about the block at y+1. Candidates:
   - `isBlockNormalCube()` — unlikely (would be false for water)
   - `canBlockSeeTheSky()` / rain hydration — possible, but rain was added post-1.0
   - Some form of `isBlockFluid()` or `isBlockWater()`
   The exact method name must be confirmed from the World spec or Block spec.

2. **`yy.v.bM`:** The drop method and revert-to-dirt logic reference `yy.v` as the dirt block.
   Confirm `yy.v` = dirt (ID 3) from the block registry.
