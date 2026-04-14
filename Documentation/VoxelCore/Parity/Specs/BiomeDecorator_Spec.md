# BiomeDecorator Spec
Source: `ql.java` (BiomeDecorator, 243 lines),
        `bu.java` (WorldGenFlowers, 23 lines), `ahu.java` (WorldGenTallGrass, 31 lines),
        `mb.java` (WorldGenShrub/DeadBush, 29 lines), `ib.java` (WorldGenSpring, 63 lines),
        `tw.java` (WorldGenReed, 29 lines), `sz.java` (WorldGenPumpkin, 17 lines),
        `ade.java` (WorldGenCactus, 23 lines), `jj.java` (WorldGenLilyPad, 17 lines),
        `acp.java` (WorldGenHugeMushroom, 150 lines),
        plus biome subclasses: `fo.java`, `qk.java`, `mk.java`, `ym.java`, `ada.java`
Type: Algorithm reference — chunk decoration sequence

---

## ⚠ Corrections to ChunkProviderGenerate_Spec

Previous specs had several class labels wrong. Corrected identities:

| Obf | Previously labelled | Correct identity |
|---|---|---|
| `ade` | Pumpkin stems / WorldGenPumpkin | **WorldGenCactus** — places cactus height 1-3 |
| `acp` | (unspecced) | **WorldGenHugeMushroom** — type 0=brown, 1=red |
| `sz` | (unspecced) | **WorldGenPumpkin** — places pumpkin patch on grass |
| `w` (ql field) | (unspecced) | Cactus generator (`ade`) — controlled by field `F` |
| `u` (ql field) | (unspecced) | Huge mushroom generator (`acp`) — controlled by field `J` |

Also: **`we.java` = SpawnerAnimals** (mob spawning), NOT a snow/ice generator.
The snow/ice surface freeze step labeled in ChunkProviderGenerate_Spec §7 as `we.a()`
is incorrect. See §11 below.

---

## 1. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `ql` | `BiomeDecorator` | Per-biome chunk decorator |
| `bu` | `WorldGenFlowers` | Scatter N blocks; 64 attempts; canBlockStay check |
| `ahu` | `WorldGenTallGrass` | Scatter tall grass; 128 attempts; descend to surface |
| `mb` | `WorldGenShrub` | Scatter 1 block type; 4 attempts; descend to surface |
| `ib` | `WorldGenSpring` | Place water/lava spring; requires 3 stone + 1 air neighbors |
| `tw` | `WorldGenReed` | Sugar cane; 20 attempts; needs adjacent water at y-1 |
| `sz` | `WorldGenPumpkin` | Pumpkin patch; 64 attempts; needs grass below |
| `ade` | `WorldGenCactus` | Cactus; 10 attempts; height [1, 3]; canBlockStay |
| `jj` | `WorldGenLilyPad` | Lily pad; 10 attempts; needs water below |
| `acp` | `WorldGenHugeMushroom` | Huge mushroom; type 0=brown/1=red; height [4, 6] |
| `ig` | `WorldGenerator` | Abstract base |

---

## 2. BiomeDecorator Fields

All fields are on `ql` (BiomeDecorator). Defaults apply unless a biome subclass overrides.

### Generator instances (shared, reused per call):

| Field | Instance | Purpose |
|---|---|---|
| `f` | `new adp(4)` | Clay disk (from WorldGenTrees_Spec) |
| `g` | `new fc(7, yy.E.bM)` | Sand disk, size 7 (from WorldGenTrees_Spec) |
| `h` | `new fc(6, yy.F.bM)` | Gravel disk — **defined but not called in base ql.a()**; biome subclass use only |
| `i`–`p` | `new ky(...)` | Ore generators (see ChunkProviderGenerate_Spec §9) |
| `q` | `new bu(yy.ad.bM)` | Dandelion flower (ID 37) |
| `r` | `new bu(yy.ae.bM)` | Rose flower (ID 38) |
| `s` | `new bu(yy.af.bM)` | Brown mushroom (ID 39) |
| `t` | `new bu(yy.ag.bM)` | Red mushroom (ID 40) |
| `u` | `new acp()` | Huge mushroom (random type) |
| `v` | `new tw()` | Reed / sugar cane |
| `w` | `new ade()` | Cactus |
| `x` | `new jj()` | Lily pad |

### Count fields (int):

| Field | Default | Meaning |
|---|---|---|
| `z` | 0 | Trees (biome tree generator via `e.a(rand)`) |
| `y` | 0 | Lily pad attempts |
| `A` | 2 | Flower attempts (dandelion + possible rose) |
| `B` | 1 | Tall grass scatter calls |
| `C` | 0 | Dead bush scatter calls |
| `D` | 0 | Mushroom base count (see §4 for formula) |
| `E` | 0 | Reed scatter calls (plus hardcoded 10) |
| `F` | 0 | Cactus scatter calls |
| `G` | 1 | Extra sand disk count (calls `g`, same as H) |
| `H` | 3 | Sand disk count |
| `I` | 1 | Clay disk count |
| `J` | 0 | Huge mushroom count |
| `K` | `true` | Enable water+lava springs |

---

## 3. Entry Point: `a(world, rand, chunkX, chunkZ)`

```
if this.a != null: throw RuntimeException("Already decorating!!")
this.a = world
this.b = rand
this.c = chunkX   // chunk origin X (in blocks)
this.d = chunkZ   // chunk origin Z (in blocks)
this.a()          // run decoration
this.a = null; this.b = null
```

`chunkX` and `chunkZ` here are **block coordinates of chunk origin**, i.e. `chunkIndexX * 16`.

All random positions in the decoration loop are offset by +8 to center within the chunk:
```
worldX = chunkX + rand.nextInt(16) + 8    // [chunkX+8, chunkX+23]
worldZ = chunkZ + rand.nextInt(16) + 8
```
This means decoration can spill 8 blocks into adjacent chunks.

---

## 4. Decoration Sequence — `a()` (exact RNG call order)

**Critical:** This is the exact sequential order. One wrong RNG call shifts all subsequent placements.

### Step 1 — Ore generation: `b()`
Calls `a(count, generator, yMin, yMax)` helper 8 times. Each helper call consumes:
- `count` × 3 nextInt calls for X/Y/Z position
- Plus internal RNG inside `ky.a()` (uses the passed `rand` directly)

Full order (matches BiomeDecorator `b()` method):
1. `a(20, i, 0, worldHeight)` — dirt (ky, size 32)
2. `a(10, j, 0, worldHeight)` — gravel (ky, size 32)
3. `a(20, k, 0, worldHeight)` — coal (ky, size 16)
4. `a(20, l, 0, worldHeight/2)` — iron (ky, size 8)
5. `a(2, m, 0, worldHeight/4)` — gold (ky, size 8)
6. `a(8, n, 0, worldHeight/8)` — redstone (ky, size 7)
7. `a(1, o, 0, worldHeight/8)` — diamond (ky, size 7)
8. `b(1, p, worldHeight/8, worldHeight/8)` — lapis, triangular dist.

Helper `a(count, gen, yMin, yMax)`:
```
for count times:
    x = nextInt(16) + chunkX         // no +8 offset in ore helper!
    y = nextInt(yMax - yMin) + yMin
    z = nextInt(16) + chunkZ
    gen.a(world, rand, x, y, z)      // ore vein (consumes rand internally)
```

Helper `b(count, gen, yCenter, ySpread)` — triangular Y:
```
for count times:
    x = nextInt(16) + chunkX
    y = nextInt(ySpread) + nextInt(ySpread) + (yCenter - ySpread)
    z = nextInt(16) + chunkZ
    gen.a(world, rand, x, y, z)
```

### Step 2 — Sand disk patches (H times, default 3)
```
for H times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    g.a(world, rand, x, world.f(x, z), z)   // world.f = getTopSolidOrLiquidBlock
```
Generator `g` = `fc(7, sand_id)`. See WorldGenTrees_Spec §9.1 for algorithm.

### Step 3 — Clay disk patches (I times, default 1)
```
for I times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    f.a(world, rand, x, world.f(x, z), z)
```
Generator `f` = `adp(4)`. See WorldGenTrees_Spec §9.2.

### Step 4 — Extra sand patches (G times, default 1)
```
for G times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    g.a(world, rand, x, world.f(x, z), z)   // same g generator as step 2
```

### Step 5 — Trees (z + occasional bonus)
```
treeCount = z
if nextInt(10) == 0: treeCount++   // 10% chance of one extra tree

for treeCount times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    gen = biome.a(rand)               // biome selects tree type (consumes rand)
    gen.a(1.0, 1.0, 1.0)             // set default scale (no rand)
    gen.a(world, rand, x, world.d(x, z), z)   // world.d = getHeightValue (top opaque block)
```

### Step 6 — Huge mushrooms (J times, default 0)
```
for J times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    u.a(world, rand, x, world.d(x, z), z)
```

### Step 7 — Flowers (A times, default 2)
```
for A times:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)                // any Y — bu.a() internally does no descend
    z = nextInt(16) + chunkZ + 8
    q.a(world, rand, x, y, z)              // dandelion, 64 attempts spread around (x, y, z)
    
    if nextInt(4) == 0:                     // 25% chance of rose at separate location
        x = nextInt(16) + chunkX + 8
        y = nextInt(worldHeight)
        z = nextInt(16) + chunkZ + 8
        r.a(world, rand, x, y, z)          // rose, 64 attempts
```

### Step 8 — Tall grass (B times, default 1)
```
for B times:
    meta = 1      // constant — tall grass (not fern, not dead bush)
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    new ahu(yy.X.bM, meta).a(world, rand, x, y, z)   // 128 attempts; descends to surface
```
A new `ahu` instance is created each iteration.

### Step 9 — Dead bushes (C times, default 0)
```
for C times:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    new mb(yy.Y.bM).a(world, rand, x, y, z)   // 4 attempts; descends to surface
```

### Step 10 — Lily pads (y field, default 0)
```
for y times:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    startY = nextInt(worldHeight)           // random starting Y
    while (startY > 0 AND world.getBlockId(x, startY-1, z) == 0 OR leaves):
        startY--                            // descend through air/leaves (no rand)
    x.a(world, rand, x, startY, z)         // 10 attempts spread around
```

### Step 11 — Mushrooms (D times, default 0)
```
for D times:
    if nextInt(4) == 0:                     // 25% chance brown mushroom
        x = nextInt(16) + chunkX + 8
        z = nextInt(16) + chunkZ + 8
        y = world.d(x, z)                   // surface height
        s.a(world, rand, x, y, z)           // brown mushroom, 64 attempts

    if nextInt(8) == 0:                     // 12.5% chance red mushroom
        x = nextInt(16) + chunkX + 8
        y = nextInt(worldHeight)             // any Y
        z = nextInt(16) + chunkZ + 8
        t.a(world, rand, x, y, z)           // red mushroom, 64 attempts

// Unconditional extra mushrooms (always, not inside the D loop):
if nextInt(4) == 0:
    x = nextInt(16) + chunkX + 8
    z = nextInt(16) + chunkZ + 8
    s.a(world, rand, x, world.d(x, z), z)  // brown at surface

if nextInt(8) == 0:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    t.a(world, rand, x, y, z)              // red any Y
```

### Step 12 — Reeds / Sugar Cane (E + 10 times)
```
// E-count reeds (biome-specific)
for E times:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    v.a(world, rand, x, y, z)

// Hardcoded 10 more reeds (always)
for 10 times:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    v.a(world, rand, x, y, z)
```

### Step 13 — Pumpkin patch (1/32 chance, unconditional)
```
if nextInt(32) == 0:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    new sz().a(world, rand, x, y, z)    // pumpkin patch, 64 attempts
```

### Step 14 — Cactus (F times, default 0)
```
for F times:
    x = nextInt(16) + chunkX + 8
    y = nextInt(worldHeight)
    z = nextInt(16) + chunkZ + 8
    w.a(world, rand, x, y, z)           // cactus, 10 attempts
```

### Step 15 — Water and Lava Springs (when K == true)
```
if K:
    // Water springs (50 per chunk)
    for 50 times:
        x = nextInt(16) + chunkX + 8
        y = nextInt(nextInt(worldHeight - 8) + 8)    // [0, worldHeight), biased low
        z = nextInt(16) + chunkZ + 8
        new ib(yy.A.bM).a(world, rand, x, y, z)     // flowing water spring

    // Lava springs (20 per chunk)
    for 20 times:
        x = nextInt(16) + chunkX + 8
        y = nextInt(nextInt(nextInt(worldHeight - 16) + 8) + 8)  // triply nested, very low bias
        z = nextInt(16) + chunkZ + 8
        new ib(yy.C.bM).a(world, rand, x, y, z)     // flowing lava spring
```

Water spring Y: `nextInt(nextInt(worldHeight-8)+8)` with worldHeight=128:
- Inner: `nextInt(120+8)` = [0,127]; outer: `nextInt([0,127])` = [0,126]. Roughly uniform but trimmed.

Lava spring Y: triple nesting → very strongly biased toward Y=0. Lava pools appear at depth.

---

## 5. Generator Algorithms

### 5.1 WorldGenFlowers / WorldGenMushrooms — `bu`

```
// Constructor: bu(int blockId) → this.a = blockId
a(world, rand, x, y, z):
    for 64 attempts:
        bx = x + nextInt(8) - nextInt(8)    // spread ±7
        by = y + nextInt(4) - nextInt(4)    // spread ±3
        bz = z + nextInt(8) - nextInt(8)
        if world.h(bx, by, bz)              // isAirBlock(bx, by, bz)
           AND ((wg)yy.k[a]).e(world, bx, by, bz):   // flower.canBlockStay
            world.d(bx, by, bz, a)          // place (silent, no meta)
    return true
```

`world.h(x, y, z)` = `isAirBlock` — returns true if block ID == 0.
`(wg)yy.k[a]` = cast the block at ID `a` to `wg` (BlockFlower subclass); `.e()` = `canBlockStay`.

### 5.2 WorldGenTallGrass — `ahu`

```
// Constructor: ahu(int blockId, int meta) → this.a = blockId, this.b = meta
a(world, rand, x, y, z):
    // Descend to surface
    while (world.getBlockId(x, y, z) == 0 OR leaves) AND y > 0:
        y--

    for 128 attempts:
        bx = x + nextInt(8) - nextInt(8)
        by = y + nextInt(4) - nextInt(4)
        bz = z + nextInt(8) - nextInt(8)
        if world.h(bx, by, bz)
           AND ((wg)yy.k[a]).e(world, bx, by, bz):
            world.b(bx, by, bz, a, b)       // place (notify, with meta)
    return true
```

Placement is `world.b()` (with neighbor notifications) — unlike `bu` which uses `world.d()`.

### 5.3 WorldGenShrub / WorldGenDeadBush — `mb`

Same algorithm as `ahu` but:
- Only **4 attempts** (not 128)
- Uses `world.d()` (silent, no meta)

### 5.4 WorldGenSpring — `ib`

Places a water or lava spring pocket in cave walls.

```
// Constructor: ib(int blockId) → this.a = blockId
a(world, rand, x, y, z):
    // Validity checks — all must pass
    if world.getBlockId(x, y+1, z) != stone (yy.t): return false
    if world.getBlockId(x, y-1, z) != stone (yy.t): return false
    if world.getBlockId(x, y, z) != 0 AND != stone:  return false  // must be air or stone

    // Count adjacent stone faces (horizontal only)
    stoneCount = count of {W, E, N, S} neighbors that == stone
    // Count adjacent air faces
    airCount = count of {W, E, N, S} neighbors where world.h() == true

    if stoneCount == 3 AND airCount == 1:
        world.g(x, y, z, this.a)            // place fluid block (world.g = setBlock?)
        world.f = true                       // enable some flag (fluid flow trigger?)
        yy.k[this.a].a(world, x, y, z, rand)  // onBlockAdded → starts fluid tick
        world.f = false
    return true
```

**Note on `world.g()` and `world.f`:**
- `world.g(x, y, z, blockId)` is a World method not yet specced. Based on context, it likely
  performs a raw block set without triggering normal update logic.
- `world.f = true` toggles a boolean field. Its exact semantics need the World spec to confirm.
  Likely it prevents recursive fluid updates from triggering during world generation.

Block IDs:
- Water spring: `yy.A.bM` = 8 (flowing water)
- Lava spring: `yy.C.bM` = 10 (flowing lava)

After `onBlockAdded()` is called, the fluid schedules itself for ticking via `scheduleBlockUpdate`.

### 5.5 WorldGenReed — `tw`

```
a(world, rand, x, y, z):
    for 20 attempts:
        bx = x + nextInt(4) - nextInt(4)    // spread ±3 (smaller than flowers)
        bz = z + nextInt(4) - nextInt(4)
        by = y                              // Y unchanged from call position

        // Place if: air at (bx, by, bz) AND water adjacent at y-1
        if world.h(bx, by, bz):
            hasAdjacentWater = (world.e(bx-1, by-1, bz) == p.g)   // water material
                            OR (world.e(bx+1, by-1, bz) == p.g)
                            OR (world.e(bx,   by-1, bz-1) == p.g)
                            OR (world.e(bx,   by-1, bz+1) == p.g)
            if hasAdjacentWater:
                height = 2 + nextInt(nextInt(3) + 1)   // [2, 4], biased toward 2
                for i in [0, height):
                    if yy.aX.e(world, bx, by+i, bz):   // reed.canBlockStay
                        world.d(bx, by+i, bz, yy.aX.bM)  // place reed (ID 83)
    return true
```

`p.g` = water material. `yy.aX` = reed block (ID 83).
Height distribution: `nextInt(nextInt(3)+1)` = 0, 1, or 2 with geometric bias toward 0.
So height = 2, 3, or 4 blocks; 2 is most common.

### 5.6 WorldGenPumpkin — `sz`

```
a(world, rand, x, y, z):
    for 64 attempts:
        bx = x + nextInt(8) - nextInt(8)
        by = y + nextInt(4) - nextInt(4)
        bz = z + nextInt(8) - nextInt(8)
        if world.h(bx, by, bz)
           AND world.getBlockId(bx, by-1, bz) == grass (yy.u.bM = 2)
           AND yy.ba.c(world, bx, by, bz):      // pumpkin.canBlockStay
            world.b(bx, by, bz, yy.ba.bM, nextInt(4))   // pumpkin with random facing meta [0,3]
    return true
```

`yy.ba` = pumpkin unlit (ID 86). Facing meta: 0=south, 1=west, 2=north, 3=east (verify with rendering).

### 5.7 WorldGenCactus — `ade`

```
a(world, rand, x, y, z):
    for 10 attempts:
        bx = x + nextInt(8) - nextInt(8)
        by = y + nextInt(4) - nextInt(4)
        bz = z + nextInt(8) - nextInt(8)
        if world.h(bx, by, bz):
            height = 1 + nextInt(nextInt(3) + 1)   // [1, 3], biased toward 1
            for i in [0, height):
                if yy.aV.e(world, bx, by+i, bz):   // cactus.canBlockStay
                    world.d(bx, by+i, bz, yy.aV.bM)
    return true
```

`yy.aV` = cactus (ID 81). No meta. Height 1-3, biased toward 1.

### 5.8 WorldGenLilyPad — `jj`

```
a(world, rand, x, y, z):
    for 10 attempts:
        bx = x + nextInt(8) - nextInt(8)
        by = y + nextInt(4) - nextInt(4)
        bz = z + nextInt(8) - nextInt(8)
        if world.h(bx, by, bz) AND yy.bz.c(world, bx, by, bz):
            world.d(bx, by, bz, yy.bz.bM)
    return true
```

`yy.bz` = lily pad (ID 111). `c(world, x, y, z)` = `canBlockStay` — requires water at (x, y-1, z).

### 5.9 WorldGenHugeMushroom — `acp`

Complex algorithm for brown (type 0) and red (type 1) giant mushrooms.

```
// Constructor: acp(int type) → this.a = type; acp() → this.a = -1 (random)
a(world, rand, x, y, z):
    type = (a == -1) ? nextInt(2) : a       // 0=brown round, 1=red dome
    height = nextInt(3) + 4                  // [4, 6]
    
    // Ground check
    below = world.getBlockId(x, y-1, z)
    if below != dirt AND != grass AND != mycelium: return false
    if NOT yy.af.canBlockStay(world, x, y, z): return false   // yy.af = brown mushroom
    
    // Space check (radius 3 above trunk level)
    for y to y+height+1:
        radius = (level == y) ? 0 : 3
        check 7×7 area; if any opaque block: return false

    // Convert ground to dirt
    world.d(x, y-1, z, dirt_id)
    
    // Place cap layers
    // Brown (type 0): 3 full layers at top; red (type 1): starts 3 below top
    capStart = (type == 0) ? y+height-2 : y+height-3
    
    for each cap level and position within radius:
        meta = face-direction bits (1-9 for cap faces, 10 for stem)
        if not opaque: world.b(x, capLevel, z, mushroomCap_id + type, meta)
    
    // Place stem (trunk)
    for i in [0, height):
        if not opaque: world.b(x, y+i, z, mushroomCap_id + type, 10)   // meta 10 = stem
```

Block ID: `yy.bn.bM + type`:
- type 0 → `yy.bn.bM` = 99 (brown mushroom cap)
- type 1 → `yy.bn.bM + 1` = 100 (red mushroom cap)

Cap meta bits (face selection):
- 1=NW, 2=N, 3=NE, 4=W, 5=top, 6=E, 7=SW, 8=S, 9=SE, 10=stem, 0=interior (no texture)

---

## 6. Per-Biome Overrides

| Biome | z | y | A | B | C | D | E | F | G | H | I | J | K |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Default | 0 | 0 | 2 | 1 | 0 | 0 | 0 | 0 | 1 | 3 | 1 | 0 | T |
| Plains | -999 | — | 4 | 10 | — | — | — | — | — | — | — | — | — |
| Desert | -999 | — | — | — | 2 | — | 50 | 10 | — | — | — | — | — |
| Forest | 10 | — | — | 2 | — | — | — | — | — | — | — | — | — |
| Taiga | 10 | — | — | 1 | — | — | — | — | — | — | — | — | — |
| Swamp | 2 | 4 | -999 | — | 1 | 8 | 10 | — | — | — | 1 | — | — |

**Desert**: `D.clear()` also removes the mob list (separate from decorator fields).
**Swamp**: A=-999 means the flower loop runs -999 times → 0 iterations (same as z=-999 for trees).

Biome-specific `a(Random)` tree generators (see WorldGenTrees_Spec §10 for full table).

---

## 7. World Method Reference

Methods called by BiomeDecorator that need clarification:

| Call | Obf | Meaning |
|---|---|---|
| `world.f(x, z)` | `f` | `getTopSolidOrLiquidBlock` — top non-air Y (including liquid surface) |
| `world.d(x, z)` | `d` | `getHeightValue` — top opaque block Y from heightmap |
| `world.h(x, y, z)` | `h` | `isAirBlock` — returns true if blockId == 0 |
| `world.e(x, y, z)` | `e` | `getMaterial(x, y, z)` → Material instance |
| `world.g(x, y, z, id)` | `g` | Unknown — used by spring generator; probably `setBlock` raw |
| `world.f = true/false` | `f` | Boolean field — unknown semantics; spring generator toggles it |
| `world.c` | `c` | World height (= 128) |

---

## 8. Block ID Reference

| Block | Field | ID | Used by |
|---|---|---|---|
| Dandelion | `yy.ad` | 37 | Flower generator q |
| Rose | `yy.ae` | 38 | Flower generator r |
| Brown mushroom | `yy.af` | 39 | Mushroom generator s |
| Red mushroom | `yy.ag` | 40 | Mushroom generator t |
| TallGrass | `yy.X` | 31 | Tall grass (ahu), meta 1 |
| Dead Bush | `yy.Y` | 32 | Dead bush (mb) |
| Reed | `yy.aX` | 83 | Reed generator v (tw) |
| Pumpkin | `yy.ba` | 86 | Pumpkin generator (sz) |
| Cactus | `yy.aV` | 81 | Cactus generator w (ade) |
| Lily Pad | `yy.bz` | 111 | Lily pad generator x (jj) |
| Brown MushroomCap | `yy.bn` | 99 | Huge mushroom (acp, type 0) |
| Red MushroomCap | `yy.bo` | 100 | Huge mushroom (acp, type 1) |
| Flowing water | `yy.A` | 8 | Water spring (ib) |
| Flowing lava | `yy.C` | 10 | Lava spring (ib) |

---

## 9. Biome-Specific Decorator Subclasses

The base `ql` (BiomeDecorator) is always used. Biome subclasses modify **fields on the shared ql instance** (`this.B.*`), not subclass it. The field `B` on each `sr` (BiomeGenBase) holds the decorator.

All biome-specific tree configuration is in `sr.a(Random)` (not in ql). See WorldGenTrees_Spec §10.

---

## 10. Gravel Disc (`h` field) — Not Used in Base

The field `h = new fc(6, yy.F.bM)` (gravel disk, size 6) is declared in ql but **never called
by `ql.a()` directly**. It may be used by biome subclasses that override `a()` or `b()`.
At this time no biome subclass override of `b()` or direct `h` usage has been identified.
The Coder should stub `h` as available but not wire it into the default flow.

---

## 11. Snow / Ice Freeze Pass — UNRESOLVED

ChunkProviderGenerate_Spec §7 ("Snow/ice surface — `we.a()`") was incorrect.
`we.java` = SpawnerAnimals (mob spawning), not a snow/ice generator.

The snow/ice surface freeze (converting surface water to ice, placing snow on solid surfaces
in cold biomes based on biome temperature < 0.15) is executed during chunk population
but the responsible class has not yet been identified.

**Known facts:**
- It is NOT part of `ql.a()` (BiomeDecorator) — no call to freeze passes observed.
- It is NOT `we.java` (SpawnerAnimals).
- It likely occurs as a separate step in `xj.java` populate(), reading biome temperature per column.
- Expected behaviour: for each surface column in the 16×16 chunk, if biome temperature < 0.15:
  freeze still water (ID 9) to ice (ID 79 = `yy.aT.bM`) and place snow layer (ID 78 = `yy.aS.bM`)
  on the first solid block above the surface.

A follow-up read of `xj.java`'s populate() method is needed to identify the freeze class/code.

---

*Spec written by Analyst AI from `ql.java`, `bu.java`, `ahu.java`, `mb.java`, `ib.java`,
`tw.java`, `sz.java`, `ade.java`, `jj.java`, `acp.java`, and biome subclasses.
No C# implementation consulted.*
