<!--
  SpectraEngine Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraEngine Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# ChunkProviderEnd / WorldProviderEnd / WorldGenEndSpike / BiomeSky / BlockEndPortalFrame / BlockEndPortal Spec

Source classes:
- `a.java` — ChunkProviderEnd (implements `ej`)
- `ol.java` — WorldProviderEnd (extends `k`)
- `uu.java` — BiomeSky decorator (extends `ql`)
- `oh.java` — WorldGenEndSpike (extends `ig`)
- `rl.java` — BlockEndPortalFrame (extends `yy`, ID 120)
- `aid.java` — BlockEndPortal (extends `ba`, ID 119)
- `aag.java` — ItemEnderEye (extends `acy`)
- `aim.java` — PortalTravelAgent (obsidian platform placement for End entry)

---

## 1. Purpose

This spec covers the generation, population, and entry/exit logic for The End dimension:

- **`a`** (ChunkProviderEnd): generates the floating End island using a density-noise function with circular shaping; fills with end stone.
- **`ol`** (WorldProviderEnd): dimension ID 1; no sun/sky; default spawn (100, 50, 0).
- **`uu`** (BiomeSky): decorator for the End biome; places obsidian spikes with End Crystals; spawns the Ender Dragon on chunk (0,0).
- **`oh`** (WorldGenEndSpike): places a cylindrical obsidian pillar with a bedrock cap and one End Crystal on top.
- **`rl`** (BlockEndPortalFrame, ID 120): the frame block placed in strongholds; stores facing + hasEye in metadata; unbreakable.
- **`aid`** (BlockEndPortal, ID 119): the activated portal; thin (1/16 high); teleports players to dimension 1 on contact.
- **`aag`** (ItemEnderEye): inserts eye into frame; checks for complete 12-frame ring; activates portal; throws entity toward stronghold.
- **`aim`** (PortalTravelAgent): for End dimension — places 5×5 obsidian platform at the entity arrival position.

---

## 2. WorldProviderEnd (`ol`)

Extends `k` (WorldProvider).

### 2.1 `b()` — initialise

```
this.b = new bx(sr.k, 0.5F, 0.0F)   // biome provider = BiomeSky (sr.k = gu)
this.g = 1                            // dimension ID = 1 (The End)
this.e = true                         // isEnd / has no sky flag (same field as Nether's isNether)
this.c = true                         // sleeping disabled
```

### 2.2 `c()` — createChunkProvider

Returns `new a(this.a, this.a.t())` — ChunkProviderEnd constructed with the world and the world's seed (`world.t()`).

### 2.3 `a(long celestialAngle, float partialTick)` — getSunAngle

Returns `0.0F` — no sun movement.

### 2.4 `a(float angle, float partialTick)` — getSkyColor

Returns `null` — no sky colour (renders void background).

### 2.5 `b(float angle, float partialTick)` — getFogColor

Returns a dim grey Vec3. Formula:
```
basePacked = 0x808080 (= r=128, g=128, b=128)
scale = max(0, min(1, sin(angle*2π)*2 + 0.5)) * 0.0 + 0.15
r = (basePacked >> 16 & 0xFF) / 255.0F * scale
g = (basePacked >>  8 & 0xFF) / 255.0F * scale
b = (basePacked       & 0xFF) / 255.0F * scale
```
Result: constant dark grey fog at RGB ≈ (0.075, 0.075, 0.075) regardless of time of day.

### 2.6 `f()` — hasClouds

Returns `false`.

### 2.7 `d()` — hasSky

Returns `false`.

### 2.8 `e()` — getLightMultiplier (or fog density)

Returns `8.0F`.

### 2.9 `a(int x, int z)` — isSurfaceBlock

Returns `yy.k[getBlockId(x,z)].bZ.d()` — whether the block's material is solid.

### 2.10 `g()` — getSpawnPoint

Returns `new dh(100, 50, 0)` — X=100, Y=50, Z=0.

This is used as the initial spawn position when arriving in the End. The actual obsidian platform is placed at the entity's coordinates by `aim.a()` (see §8).

---

## 3. ChunkProviderEnd (`a`)

Implements `ej` (IChunkProvider).

### 3.1 Fields

| Field | Type | Initialised | Semantics |
|---|---|---|---|
| `i` | `Random` | `new Random(seed)` | Per-chunk seeded RNG |
| `j` | `eb` | `new eb(i, 16)` | 16-octave density noise A (low) |
| `k` | `eb` | `new eb(i, 16)` | 16-octave density noise B (high) |
| `l` | `eb` | `new eb(i, 8)` | 8-octave selector noise |
| `a` | `eb` (public) | `new eb(i, 10)` | 10-octave island-shape noise X |
| `b` | `eb` (public) | `new eb(i, 16)` | 16-octave island-shape noise Y |
| `m` | `ry` | constructor | World reference |
| `n` | `double[]` | null | Density grid buffer |
| `o` | `sr[]` | null | Biome array buffer |
| `c,d,e,f,g` | `double[]` | null | Noise sample buffers |
| `h` | `int[32][32]` | new | Unused integer array (retained for RNG parity) |

### 3.2 `b(chunkX, chunkZ)` — generateChunk (main generation)

Called by `a(chunkX, chunkZ)` which delegates directly.

1. Seed per-chunk RNG: `i.setSeed(chunkX * 341873128712L + chunkZ * 132897987541L)`
2. Allocate `byte[]` of size `16 × world.c × 16` (world.c = world height = 128)
3. Create chunk `zx` from byte array
4. Fill biome array via `world.a().a(o, chunkX*16, chunkZ*16, 16, 16)`
5. Call `a(chunkX, chunkZ, bytes, biomes)` — density fill (§3.3)
6. Call `b(chunkX, chunkZ, bytes, biomes)` — surface pass (§3.4, no-op for End)
7. Call `chunk.c()` — recalculate height map
8. Return chunk

### 3.3 `a(chunkX, chunkZ, bytes, biomes)` — density fill

**Density grid dimensions:** The grid is 3×(worldHeight/4+1)×3 in (X, Y, Z). For height=128: 3×33×3.

**Step 1: generate noise samples.**

The private method `a(double[], baseX, baseY, baseZ, sizeX, sizeY, sizeZ)` computes the density grid:

```
scale = 684.412 * 2.0 = 1368.824

f = a.sample(f, baseX, baseZ, sizeX, sizeZ, 1.121, 1.121, 0.5)
g = b.sample(g, baseX, baseZ, sizeX, sizeZ, 200.0, 200.0, 0.5)
c = l.sample(c, baseX, baseY, baseZ, sizeX, sizeY, sizeZ, scale/80, scale/160, scale/80)
d = j.sample(d, baseX, baseY, baseZ, sizeX, sizeY, sizeZ, scale, scale, scale)
e = k.sample(e, baseX, baseY, baseZ, sizeX, sizeY, sizeZ, scale, scale, scale)
```

Called with: `baseX=chunkX*2, baseY=0, baseZ=chunkZ*2, sizeX=3, sizeY=worldHeight/4+1, sizeZ=3`.

**Step 2: per-XZ column, compute island shaping factor.**

```
var16 = (f[xzIndex] + 256.0) / 512.0    clamped to [0.0, 1.0]
var18 = g[xzIndex] / 8000.0
if (var18 < 0.0): var18 = -var18 * 0.3
var18 = var18 * 3.0 - 2.0
if (var18 > 1.0): var18 = 1.0
var18 /= 8.0
var18 = 0.0                              ← FORCED TO ZERO (dead code path, always 0)
var16 += 0.5                             ← var16 now in [0.5, 1.5]

// Circular island shaping (distances in noise grid units):
localX = (gridX + baseX) / 1.0F         (grid unit = 1, local coords)
localZ = (gridZ + baseZ) / 1.0F
circleValue = 100.0F - sqrt(localX² + localZ²) * 8.0F
circleValue = clamp(circleValue, -100.0F, 80.0F)
```

**Key effect of `circleValue`:** At distance 0 (centre), circleValue=80 (big bonus → solid island). At distance 100/8=12.5 grid units (=50 blocks), circleValue=0. At distance>12.5, circleValue<0 (density strongly negative → air). This creates a circular island roughly 50 blocks in radius from grid origin (chunk 0,0).

**Step 3: per-Y, compute final density.**

```
halfY = worldHeight / 4 / 2 = 16      (half of Y grid)

for Y in 0..worldHeight/4:
    density = 0.0
    yDelta = (Y - halfY) * 8.0 / var16    // deviation from midpoint, scaled
    if (yDelta < 0.0): yDelta = -yDelta   // absolute value
    
    densityA = d[idx] / 512.0
    densityB = e[idx] / 512.0
    selector = (c[idx] / 10.0 + 1.0) / 2.0
    
    if (selector < 0.0): density = densityA
    elif (selector > 1.0): density = densityB
    else: density = densityA + (densityB - densityA) * selector
    
    density -= 8.0        // bias downward (most columns are air)
    density += circleValue
    
    // Top ceiling pull (Y > halfY*2 - 2 = 30):
    if (Y > worldHeight/4/2*2 - 2):
        pullFrac = clamp((Y - 30) / 64.0F, 0.0, 1.0)
        density = density * (1.0 - pullFrac) + (-3000.0) * pullFrac
    
    // Bottom floor pull (Y < 8):
    if (Y < 8):
        pullFrac = (8 - Y) / 7.0F
        density = density * (1.0 - pullFrac) + (-30.0) * pullFrac
    
    grid[idx] = density
```

**Step 4: trilinear interpolation from 3×33×3 grid to 16×128×16 blocks.**

Each 2×4×2 block cell in grid coordinates expands to 8×4×8 voxels.

X step: 1/8 = 0.125, Y step: 1/4 = 0.25, Z step: 1/8 = 0.125.

If `density > 0.0`: block = `yy.bJ.bM` (end stone, ID 121). Else: block = 0 (air).

Block index formula: `(xInChunk << worldBitB) | (zInChunk << worldBitA) | y`
where `worldBitB = world.b`, `worldBitA = world.a`.

### 3.4 `b(chunkX, chunkZ, bytes, biomes)` — surface pass

Scans from top to bottom checking for `yy.t.bM` (stone ID 1). Since the density fill only places end stone (ID 121), not stone, this pass is effectively a **no-op** — the condition `byte == stone` never triggers. The End island has no surface coating; it is pure end stone throughout.

### 3.5 `c(chunkX, chunkZ)` — isChunkPresent

Returns `true` always.

### 3.6 `a(provider, chunkX, chunkZ)` — populate

```
cj.a = true    // suppress block updates during population
blockX = chunkX * 16
blockZ = chunkZ * 16
biome = world.getBiomeAt(blockX+16, blockZ+16)
biome.a(world, world.w, blockX, blockZ)   // calls BiomeSky decorator (uu.a())
cj.a = false
```

The `uu.a()` (BiomeSky decorator) handles dragon spawn and spike generation — see §4.

### 3.7 `a()` / `b()` — save helpers

`a()` returns `false` (nothing to tick).
`b()` returns `true` (always ready to save).

### 3.8 `c()` — debugName

Returns `"RandomLevelSource"` (same string as overworld — note: this is not meaningful for the End).

---

## 4. BiomeSky (`uu`) — decorator for The End

Extends `ql` (BiomeDecorator).

### 4.1 Fields

| Field | Type | Initialised | Semantics |
|---|---|---|---|
| `L` | `ig` | `new oh(yy.bJ.bM)` | Spike generator; seeded with end stone ID |

### 4.2 `a()` — decoration step

Called by `a.populate()` via `biome.a(world, rng, x, z)`.

**Step 1 — Obsidian spike (1/5 chance per chunk):**
```
if (rng.nextInt(5) == 0):
    x = chunkBlockX + rng.nextInt(16) + 8
    z = chunkBlockZ + rng.nextInt(16) + 8
    surfaceY = world.getHeightValue(x, z)
    if (surfaceY > 0):
        L.a(world, rng, x, surfaceY, z)    // see §5
```

**Step 2 — Dragon spawn (only for chunk 0, 0):**
```
if (this.c == 0 && this.d == 0):   // chunkX==0 and chunkZ==0
    dragon = new oo(world)
    dragon.setPositionAndRotation(0.0, 128.0, 0.0, rng.nextFloat()*360.0F, 0.0F)
    world.spawnEntityInWorld(dragon)
```

The dragon spawns exactly once: when chunk (0,0) is populated for the first time.

---

## 5. WorldGenEndSpike (`oh`)

Extends `ig` (WorldGenerator).

### 5.1 Fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `int` | Base block ID to check against the floor (= end stone ID 121) |

### 5.2 `a(world, rng, x, y, z)` — generate

`y` is the surface height (first air block above ground).

**Guard conditions:**
1. Block at (x, y, z) is air: `world.h(x, y, z)` → `isAirBlock`
2. Block at (x, y-1, z) is end stone: `world.a(x, y-1, z) == this.a`

**If guards pass:**

```
height = rng.nextInt(32) + 6        // spike height [6, 37]
radius = rng.nextInt(4) + 1         // spike radius [1, 4]
```

**Validate footprint:** For every position (bx, bz) within radius `radius` of (x, z):
```
for bx in (x-radius)..(x+radius):
    for bz in (z-radius)..(z+radius):
        if (bx-x)² + (bz-z)² <= radius² + 1:
            if world.getBlockId(bx, y-1, bz) != endStoneId:
                return false   // abort: not enough end stone below
```

**Build obsidian cylinder:**
```
for yy in y..(y + height - 1):     // height blocks tall
    for bx in (x-radius)..(x+radius):
        for bz in (z-radius)..(z+radius):
            if (bx-x)² + (bz-z)² <= radius² + 1:
                world.setBlockWithNotify(bx, yy, bz, yy.ap.bM)   // obsidian ID 49
```

**Place End Crystal and bedrock cap:**
```
crystal = new sf(world)
crystal.setPositionAndRotation(x + 0.5, y + height, z + 0.5, rng.nextFloat()*360.0F, 0.0F)
world.spawnEntityInWorld(crystal)
world.setBlockWithNotify(x, y + height, z, yy.z.bM)   // bedrock cap, ID 7
```

Returns `true`.

### 5.3 Spike Statistics

| Parameter | Min | Max |
|---|---|---|
| Height | 6 | 37 |
| Radius | 1 | 4 |
| Obsidian count | ~31 | ~6030 |

---

## 6. BlockEndPortalFrame (`rl`, ID 120)

Extends `yy` (Block base).

### 6.1 Constructor

```
super(120, 159, p.q)        // ID=120, base texture=159, material=p.q (rock)
.a(stepSoundH)              // set step sound h
.a(0.125F)                  // set light emission = 0.125F (≈ level 2 of 16)
.c(-1.0F)                   // setHardness(-1) = unbreakable (like bedrock)
.a("endPortalFrame")        // name
.l()                        // setBlockUnbreakable? or l() some other flag
.b(6000000.0F)              // resistance = 6000000 (like bedrock)
```

Hardness = -1 → unbreakable by player (same flag as bedrock).

### 6.2 Texture (`a(face, meta)`)

| Face | Returns | Description |
|---|---|---|
| 1 (top) | `bL - 1` = 158 | Top face (same for all meta states) |
| 0 (bottom) | `bL + 16` = 175 | Bottom face — same index as end stone texture |
| 2–5 (sides) | `bL` = 159 | Side faces — frame side texture |

### 6.3 Metadata Layout

```
Bits 0-1: facing
  0 = south
  1 = west
  2 = north
  3 = east
Bit 2: hasEye (0 = empty, 1 = Eye of Ender inserted)
```

### 6.4 `e(meta)` — hasEye

```
return (meta & 4) != 0
```

### 6.5 `a()` — isOpaqueCube

Returns `false` (non-opaque; slightly shorter than full block).

### 6.6 `c()` — getLightValue

Returns `26` — the frame emits a small amount of light at all times (dim glow).

### 6.7 AABB

Base bounds: `(0, 0, 0)` → `(1, 0.8125, 1)` — 13/16 tall.

If hasEye: an additional `(0.3125, 0.8125, 0.3125)` → `(0.6875, 1.0, 0.6875)` AABB is added for the crystal on top.

### 6.8 `a(int quantity, Random, int)` — quantityDropped

Returns `0` — the frame drops nothing when destroyed (unbreakable anyway).

### 6.9 `a(world, x, y, z, player)` — onBlockPlacedBy

Sets facing metadata from player yaw:
```
facing = ((floor(player.yaw * 4.0 / 360.0 + 0.5) & 3) + 2) % 4
world.setBlockMetadata(x, y, z, facing)
```

---

## 7. BlockEndPortal (`aid`, ID 119)

Extends `ba` (BlockContainer) — has TileEntity.

### 7.1 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `boolean` (static) | `false` | Spawn guard — prevents recursive activation during placement |

### 7.2 Constructor

```
super(119, 0, p.A)   // ID=119, texture=0, material=p.A (unknown, likely rock or portal)
.a(1.0F)             // setOpacity(1.0F) or light opacity
```

### 7.3 `j_()` — createTileEntity

Returns `new yg()` — End Portal TileEntity (type `yg`; likely manages the teleportation tick).

### 7.4 `b(world, x, y, z)` — setBlockBoundsBasedOnState

Sets AABB to `(0, 0, 0)` → `(1, 0.0625, 1)` — very thin (1/16 block high).

### 7.5 `a_(world, x, y, z, face)` — canPlaceAt

Returns `false` if `face != 0` (only placeable on top of a block, from below).
Otherwise delegates to `super.a_()`.

### 7.6 `a(world, x, y, z, bbox, list)` — addCollisionBoxes

**Empty method** — the portal has no physical collision. Entities and projectiles pass through.

### 7.7 `a()` / `b()` — isOpaqueCube / renderAsNormal

Both return `false`.

### 7.8 `a(rng)` — quantityDropped

Returns `0` — drops nothing.

### 7.9 `a(world, x, y, z, entity)` — onEntityCollidedWithBlock

Teleportation trigger:
```
if entity.vehicle == null AND entity.rider == null
   AND entity instanceof vi (EntityPlayer)
   AND NOT world.isClient:
    player.c(1)    // EntityPlayer.travelToDimension(1) → teleport to The End
```

Only players teleport (not mobs or items). Not triggered client-side.

### 7.10 `b(world, x, y, z, rng)` — randomDisplayTick

Spawns one smoke particle:
```
x = blockX + rng.nextFloat()
y = blockY + 0.8F
z = blockZ + rng.nextFloat()
world.spawnParticle("smoke", x, y, z, 0, 0, 0)
```

### 7.11 `c()` — getBlockColor

Returns `-1` (black; used for map rendering).

### 7.12 `a(world, x, y, z)` — onBlockAdded

If NOT `a` (spawn guard) AND world dimension is not 0:
```
world.setBlock(x, y, z, 0)   // self-destruct if placed outside overworld
```

The End Portal block only exists in the overworld stronghold. If somehow placed in Nether/End, it removes itself.

---

## 8. ItemEnderEye (`aag`)

Extends `acy` (Item).

### 8.1 `a(stack, player, world, x, y, z, face)` — onItemUse (insert eye into frame)

**Guards:**
1. Player can reach block (distance check via `player.e(x,y,z)`)
2. Block at (x,y,z) == End Portal Frame (`yy.bI.bM` = 120)
3. Frame does NOT already have an eye (`!rl.e(meta)`, i.e., bit 2 not set)
4. Client-side: return true without acting

**Server-side:**
1. Set metadata: `world.setBlockMetadata(x, y, z, meta | 4)` → sets bit 2 (hasEye)
2. `stack.a--` → decrement stack size by 1
3. Spawn 16 smoke particles at `(x + rng*0.375 + 0.3125, y + 0.8125, z + rng*0.375 + 0.3125)`
4. Scan the 12-frame ring and activate portal if complete (see §8.2)
5. Return true

### 8.2 Portal Activation Check

After inserting an eye, checks whether all 12 End Portal Frames in the ring have eyes.

**Frame ring layout (3+3+3+3 = 12 frames):**

The ring is a 5×5 border with empty corners — frames at:
- Top row: 3 frames in a line
- Bottom row: 3 frames in a line (4 units perpendicular from top row)
- Left side: 3 frames (1 unit before top row, 3 perpendicular steps)
- Right side: 3 frames (1 unit after top row, 3 perpendicular steps)

**Algorithm:**

Uses `lz` direction arrays:
- `var23 = meta & 3` = facing of the frame just activated (0=south,1=west,2=north,3=east)
- `var26 = lz.f[var23]` = perpendicular direction (opposite-of-opposite → rotated 90°)

```
// Scan the parallel row (5 positions along var26 direction):
for i = -2..2:
    check frame at (x + lz.a[var26]*i, y, z + lz.b[var26]*i)
    if it's a frame: must have eye → track first (var24) and last (var12) found

// Require exactly 3 consecutive frames: var12 == var24 + 2

// Scan the opposite parallel row (shifted 4 blocks in var23 direction):
for i = var24..var12:
    check frame at (x + lz.a[var26]*i + lz.a[var23]*4, y, z + lz.b[var26]*i + lz.b[var23]*4)
    must have eye

// Scan left side (at var26-position var24-1, 3 positions in var23 direction):
for j = 1..3:
    check frame at (x + lz.a[var26]*(var24-1) + lz.a[var23]*j, ...)
    must have eye

// Scan right side (at var26-position var12+1, 3 positions in var23 direction):
for j = 1..3:
    check frame at (x + lz.a[var26]*(var12+1) + lz.a[var23]*j, ...)
    must have eye
```

If all 12 pass: fill the 3×3 interior with End Portal blocks:
```
for i = var24..var12:       // 3 positions along var26
    for j = 1..3:           // 3 positions along var23
        world.setBlock(interior_x, y, interior_z, yy.bH.bM)   // ID 119
```

### 8.3 `c(stack, world, player)` — onItemRightClick (throw eye toward stronghold)

1. Ray-trace: if targeting End Portal Frame block → do nothing (handled by `a()`)
2. Server-side:
   - Find nearest stronghold: `world.b("Stronghold", playerX, playerY, playerZ)` → returns `am` (BlockPos)
   - If found: spawn `bs` (EntityEnderEye) aimed at stronghold coordinates
   - Fire world event 1002 (bow-release sound)
   - If player is not creative (`!player.cc.d`): `stack.a--`
3. Return the (possibly decremented) stack

---

## 9. PortalTravelAgent (`aim`) — End Dimension Platform

`aim.a(world, entity)` handles dimension teleportation. For the End (`world.y.g == 1`):

### 9.1 Obsidian Platform Placement

```
x = floor(entity.x)
y = floor(entity.y) - 1    // one below entity spawn point
z = floor(entity.z)

for dx = -2..2:
    for dz = -2..2:
        for dy = -1..2:
            blockX = x + dz * facingX + dx * perpX
            blockY = y + dy
            blockZ = z + dz * facingZ + dx * perpZ
            if (dy < 0): place obsidian (yy.ap.bM, ID 49)
            else:        place air (0)
```

Result: a 5×5 obsidian floor at y-1, with a 5×5×3 air column above it.

After placement, the entity is repositioned to (x, y-1, z) with velocity zeroed.

### 9.2 Nether Portal Linking (`world.y.g != 1`)

See `BlockPortal_Spec.md` — `aim.b()` (find existing portal) and `aim.c()` (create new portal).

---

## 10. Constants Summary

| Constant | Value | Source |
|---|---|---|
| End Stone block ID | 121 | `yy.bJ.bM` |
| End Portal block ID | 119 | `yy.bH.bM` |
| End Portal Frame ID | 120 | `yy.bI.bM` |
| Obsidian block ID | 49 | `yy.ap.bM` |
| Bedrock block ID | 7 | `yy.z.bM` |
| Dimension ID (End) | 1 | `ol.g` |
| Default End spawn | (100, 50, 0) | `ol.g()` |
| Dragon spawn position | (0.0, 128.0, 0.0) | `uu.a()` |
| Dragon spawn trigger | chunk (0, 0) only | `uu.a()` — `this.c==0 && this.d==0` |
| Spike height range | [6, 37] | `nextInt(32)+6` |
| Spike radius range | [1, 4] | `nextInt(4)+1` |
| Spike chance | 1/5 per chunk | `nextInt(5)==0` |
| Obsidian platform size | 5×5 floor | `aim.a()` loops -2..2 |
| Portal frame ring | 12 frames | 3+3+3+3 ring |
| Portal interior | 3×3 = 9 blocks | `aag.a()` fill loop |

---

## 11. Known Quirks

### 11.1 `var18` forced to zero in noise computation

In `a.java`'s density function, `var18` (derived from noise `b`) is computed and then immediately set to `var18 = 0.0`. This means the second island-shape noise has NO effect on the density. The End island shape depends entirely on the `f`/`g` circular factor and the trilinear-interpolated density. This appears to be an incomplete feature (similar to how the overworld has unused noise paths).

### 11.2 Dragon spawns only for chunk (0, 0)

The dragon spawn is gated on `this.c == 0 && this.d == 0` in `uu.a()`. This means the dragon spawns exactly once per world: when chunk (0,0) is first populated. Repopulating (e.g., after a bug) would spawn additional dragons if the chunk's populate flag were somehow reset.

### 11.3 `b()` surface pass is a no-op for the End

The surface pass in `a.java` checks for `yy.t.bM` (stone ID 1) to replace with a surface coat. Since the density fill places `yy.bJ.bM` (end stone ID 121), the condition never triggers. The pass iterates all 16×16 columns doing nothing. It is safe to skip this pass, but retaining it maintains RNG-parity for future code that might read `i` after generation.

### 11.4 Portal frame facing uses `lz` perp-direction array

The activation scan uses `lz.f[facing]` for the perpendicular direction. `lz` is the same Direction helper used by redstone wire. The facing mapping (meta bits 0-1) is separate from the torch/piston face encoding.

### 11.5 End Portal self-destructs outside overworld

`aid.onBlockAdded()` checks `world.y.g != 0` and removes itself. A portal block placed in the Nether or End will immediately disappear. The static guard `a` prevents this during activation.

---

## 12. Open Questions

### 12.1 TileEntity `yg` for BlockEndPortal

`aid.j_()` returns `new yg()`. The class `yg` (EndPortalTileEntity) was not analysed. It likely handles the teleportation cooldown or particle effects. The block already teleports via `onEntityCollided`, so `yg` may be a stub or handle particle spawning only.

### 12.2 WorldProviderEnd field `a(int, int)` method purpose

`ol.a(int x, int z)` checks `yy.k[blockId].bZ.d()` (material is solid). The WorldProvider base class has this method but its call site was not traced. Possibly used for portal surface placement or spawn safety.

### 12.3 `ol.e()` return value 8.0F

The method `e()` returns `8.0F` in `ol`. In the WorldProvider base, this may be `getLightBrightness` or `setFogDensity`. The downstream rendering effect is not traced in this spec.

### 12.4 `aim.b()` search radius in Nether vs Overworld

`aim.b()` uses a hardcoded `short var3 = 128` block search radius. Whether this applies to overworld→nether or nether→overworld equally, and the 8:1 coordinate scale division, is documented in `BlockPortal_Spec.md`.
