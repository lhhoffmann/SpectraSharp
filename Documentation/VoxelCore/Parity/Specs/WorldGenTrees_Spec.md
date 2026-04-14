# WorldGenTrees Spec
Source: `gq.java` (WorldGenTrees), `yd.java` (WorldGenBigTree), `jp.java` (WorldGenForestTree),
        `qj.java` (WorldGenSwamp), `ty.java` (WorldGenTaiga1), `us.java` (WorldGenTaiga2),
        `fc.java` (WorldGenSandDisc), `adp.java` (WorldGenClay),
        `ig.java` (WorldGenerator base), `sr.java` (BiomeGenBase tree dispatch),
        `fo.java` (Forest), `qk.java` (Taiga), `mk.java` (Swampland), `ym.java` (Plains)
Type: Algorithm reference + class definition

---

## ⚠ Correction to ChunkProviderGenerate_Spec §10

The decoration table in ChunkProviderGenerate_Spec §10 **incorrectly labelled** `fc` and `adp`
as tree generators. They are **disk/patch generators**, not trees.
The corrected decoration meanings are:

| BiomeDecorator field | Generator | BiomeDecorator count field | Correct label |
|---|---|---|---|
| `g` | `fc(7, sand_id)` | `H` (default 3) | Sand disk patches |
| `f` | `adp(4)` | `I` (default 1) | Clay disk patches |
| `g` again | `fc(7, sand_id)` | `G` (default 1) | Additional sand disk patches |

Trees are generated exclusively through the **biome tree loop** (`z` count field, default 0)
which calls `biome.a(Random)` to get the tree generator instance for each attempt.

---

## 1. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `ig` | `WorldGenerator` | Abstract base; all feature generators extend this |
| `gq` | `WorldGenTrees` | Standard oak tree |
| `yd` | `WorldGenBigTree` | Fancy/branching oak |
| `jp` | `WorldGenForestTree` | Birch tree |
| `qj` | `WorldGenSwamp` | Swamp oak with vines |
| `ty` | `WorldGenTaiga1` | Spruce / thin pine (Taiga variant 1) |
| `us` | `WorldGenTaiga2` | Spruce / wide pine (Taiga variant 2) |
| `fc` | `WorldGenSandDisc` | Circular disk: replaces grass/dirt with another block |
| `adp` | `WorldGenClay` | Circular disk: replaces grass/clay with clay |

---

## 2. WorldGenerator Base (`ig`)

```java
public abstract class ig {
    private final boolean a;  // if true: use world.d() (silent set); false: use world.b() (notify)

    public ig(boolean silentPlacement) { this.a = silentPlacement; }

    public abstract boolean a(ry world, Random rand, int x, int y, int z);
    public void a(double scaleX, double scaleY, double scaleZ) {}  // override for scaling

    // Block placement helper — all subclasses use this to respect the silent flag
    protected void a(ry world, int x, int y, int z, int blockId, int meta) {
        if (a) world.d(x, y, z, blockId, meta);   // silent (no neighbor notifications)
        else   world.b(x, y, z, blockId, meta);   // with neighbor notifications
    }
}
```

**Silent flag convention:** All tree generators constructed with `false` (notify neighbors).
`yd` (WorldGenBigTree) bypasses the helper and calls `world.d()` directly in its main body.

---

## 3. WorldGenTrees — Standard Oak (`gq`)

```java
public gq(boolean silent) { super(silent); }
```

Registered on `sr` (BiomeGenBase) as field `G = new gq(false)`.
Used by most biomes as the primary tree generator (90% chance in `sr.a(Random)`).

### 3.1 Block IDs

| Block | Field | ID | Meta |
|---|---|---|---|
| Oak log | `yy.J` | 17 | 0 |
| Oak leaves | `yy.K` | 18 | 0 |

### 3.2 Algorithm (`a(world, rand, x, y, z)`)

**Step 1 — Choose height:**
```java
int height = rand.nextInt(3) + 4;   // [4, 6]
```

**Step 2 — Y bounds check:**
- Requires `y >= 1` AND `y + height + 1 <= world.c` (world height = 128)
- If fails: return false

**Step 3 — Space check (clearance):**
Scans from `y` to `y + height + 1` (inclusive). For each Y level:
- Y == base (y): check only center column (radius 0)
- Y >= `y + height - 1` (top 2 canopy layers + 1 above): check 5×5 area (radius 2)
- All other Y: check 3×3 area (radius 1)

Any non-air, non-leaves block (`id != 0 && id != yy.K.bM`) causes abort.

**Step 4 — Ground check:**
```java
int below = world.getBlockId(x, y - 1, z);
boolean ok = (below == grass_id || below == dirt_id) && y < world.c - height - 1;
```
If not grass or dirt: return false.

**Step 5 — Convert ground to dirt:**
```java
world.d(x, y - 1, z, dirt_id);  // grass → dirt (always, not using the helper)
```

**Step 6 — Place leaves (canopy):**
Iterates 4 leaf layers: from `y + height - 3` to `y + height` (4 levels total).

For each leaf layer at height `lY`:
```java
int dy = lY - (y + height);         // dy in {-3, -2, -1, 0}
int radius = 1 - dy / 2;            // int division: see table below
```

| dy | dy/2 (Java int) | radius |
|----|-----------------|--------|
| 0  | 0               | 1      |
| -1 | 0               | 1      |
| -2 | -1              | 2      |
| -3 | -1              | 2      |

For each (dx, dz) in the ±radius square:
- **Corner randomization:** if `|dx| == radius && |dz| == radius && dy != 0 && rand.nextInt(2) == 0` → skip (50% chance to skip corners at non-top layers)
- At top layer (`dy == 0`): corners are always placed
- Only place if block is not opaque: `!yy.m[blockId]` (the isOpaqueCube array)
- Place: `yy.K.bM` (oak leaves, meta 0)

**Step 7 — Place trunk:**
```java
for (int i = 0; i < height; i++) {
    int id = world.getBlockId(x, y + i, z);
    if (id == 0 || id == leaves_id) {
        place(world, x, y + i, z, log_id, 0);  // oak log meta 0
    }
}
```
Replaces only air or leaves, never other solid blocks.

---

## 4. WorldGenBigTree — Fancy Oak (`yd`)

Registered on `sr` as field `H = new yd(false)`.
Selected 10% of the time by `sr.a(Random)` (all biomes that have trees).

This is a complex branching algorithm. Key constants and parameters:

### 4.1 Fields

| Field | Default | Meaning |
|---|---|---|
| `g` | 0.618 | Trunk split height fraction (golden ratio) |
| `h` | 1.0 | (unused multiplier) |
| `i` | 0.381 | Branch start height fraction |
| `j` | 1.0 | Branch horizontal distance multiplier |
| `k` | 1.0 | Branch vertical distance / size multiplier |
| `l` | 1 | Trunk width (1=single, 2=2×2 trunk) |
| `m` | 12 | Height range max (total height = `5 + rand.nextInt(m)`) |
| `n` | 4 | Leaf cluster height (4 levels) |
| `e` | computed | Final tree height |
| `f` | computed | Trunk top (= `e * g` ≈ 61.8% of height) |

**Scale override** (`a(double scaleX, double scaleY, double scaleZ)`):
```java
m = (int)(scaleX * 12.0);   // adjusts max height
if (scaleX > 0.5) n = 5;   // wider leaf clusters for larger trees
j = scaleY;                  // branch distance multiplier
k = scaleZ;                  // size/height multiplier
```

### 4.2 High-Level Algorithm

1. **Compute height:** `e = 5 + rand.nextInt(m)` (if not overridden)
2. **Ground + clearance check** (`e()`): block below must be grass or dirt; line-of-sight from base to top must be clear (using Bresenham-like DDA line algorithm)
3. **`a()`** — compute branch endpoints stored in `o[][]`:
   - Main trunk split is at `f = e * 0.618`
   - For each Y level from trunk top down to `e * 0.3`:
     - Compute leaf cluster radius from ellipsoidal formula
     - Place `~(1.382 + (k*e/13)²)` branches per level using random angles
     - Each branch endpoint chosen using polar coordinates + golden-ratio angle
     - Branch must have clear line-of-sight to be accepted
4. **`b()`** — place leaf clusters at each branch endpoint
   - Cluster is an ellipsoidal disk at each of `n` height levels  
   - Disk radius: `n=4` → [2, 3, 3, 2] (edge levels) / [3, 3] (inner levels)
5. **`c()`** — place trunk from base to `f`:
   - Single trunk: one vertical line using DDA
   - Double trunk (`l=2`): 2×2 trunk using 4 DDA lines
6. **`d()`** — place branch trunks from stem base to branch endpoints using DDA

**Block IDs used:** Oak log (`yy.J.bM`, meta 0), Oak leaves (`yy.K.bM`, meta 0).
**All block placement uses `world.d()` directly (silent, no notifications).**

---

## 5. WorldGenForestTree — Birch (`jp`)

Registered on `sr` as field `I = new jp(false)`.
Used exclusively in Forest biome (20% of its tree attempts).

**Nearly identical to `gq` (WorldGenTrees)** with two differences:
1. **Height:** `rand.nextInt(3) + 5` = [5, 7] (oak is [4, 6])
2. **Block meta:** all logs and leaves use meta **2** (birch variant)

```java
place(world, x, y + i, z, log_id, 2);      // log meta 2 = birch
place(world, dx, lY, dz, leaves_id, 2);    // leaves meta 2 = birch
```

Ground check, clearance check, and leaf radius algorithm are **identical** to `gq`.

---

## 6. WorldGenSwamp — Swamp Oak with Vines (`qj`)

Registered on `sr` as field `J = new qj()` (default constructor, `silent = false`).
Used exclusively in Swampland biome (`mk` overrides `a(Random)` to always return `J`).

### 6.1 Differences from standard oak

1. **Height:** `rand.nextInt(4) + 5` = [5, 8]
2. **Water descent:** Before space check, descends y while `world.getMaterial(x, y-1, z) == p.g` (water material) — allows placing on submerged ground
3. **Space check:** allows water blocks (`yy.A.bM` = flowing water, `yy.B.bM` = still water) *at or below the base Y level*; they are treated as passable during clearance scan
4. **Canopy radius:** formula uses `2 - dy/2` (vs oak's `1 - dy/2`) → wider canopy (radius 2 or 3)
5. **Top canopy radius:** 3 (vs oak's 2 — the space-check top-layer radius is 3)
6. **Block meta:** log meta 0 (oak), leaves meta 0 (oak) — no variant
7. **Vine placement:** After leaves are placed, iterates all leaf blocks and with 1/4 chance per side places a vine on each empty neighbour:

```java
// Private helper: places vine + hangs down up to 4 blocks
private void a(ry world, int x, int y, int z, int faceMeta) {
    world.d(x, y, z, vine_id, faceMeta);
    for (int depth = 4; world.getBlockId(x, --y, z) == 0 && depth > 0; depth--) {
        world.d(x, y, z, vine_id, faceMeta);
    }
}
```

Vine face meta values: `8` = west, `2` = east (south?), `1` = north, `4` = south (east?).
*(Note: vine face meta bits in MC 1.0: bit 0=south, bit 1=west, bit 2=north, bit 3=east — 
verify against actual rendering; source values 8/2/1/4 come from `qj.java` directly.)*

**Block IDs:**
- Oak log `yy.J.bM` (ID 17, meta 0)
- Oak leaves `yy.K.bM` (ID 18, meta 0) — placed via `world.d()` (no meta overload → meta=0)
- Vine `yy.bu.bM` (ID 106, meta = face bit)

---

## 7. WorldGenTaiga1 — Thin Spruce (`ty`)

Used by Taiga biome (`qk`) with 67% probability.

### 7.1 Parameters

```java
int height = rand.nextInt(4) + 6;     // [6, 9]
int bareTrunk = 1 + rand.nextInt(2);  // [1, 2] bare trunk below canopy
int foliageLayers = height - bareTrunk;
int maxRadius = 2 + rand.nextInt(2);  // [2, 3]
```

### 7.2 Clearance check
Radius 0 for the first `bareTrunk` levels, then `maxRadius` above that. Aborts if non-air/non-leaves found.

### 7.3 Canopy placement
Iterates from bottom to top of foliage (`foliageLayers` levels total, bottom-up).
Uses a growing/shrinking radius pattern:

```java
int curRadius = rand.nextInt(2);   // start at 0 or 1
int nextRadius = 1;
int peakRadius = 0;

for each foliage level (bottom-up) {
    place square of radius curRadius (excluding corners if curRadius > 0 and all-corner condition)
    
    if (curRadius >= nextRadius) {
        curRadius = peakRadius;
        peakRadius = 1;
        nextRadius = min(nextRadius + 1, maxRadius);
    } else {
        curRadius++;
    }
}
```

Result: the canopy expands from near-zero radius at the bottom, reaches maxRadius, then contracts. This creates a cone/layered Christmas-tree shape.

### 7.4 Trunk placement
```java
int skipBottom = rand.nextInt(3);   // skip 0-2 bottom trunk blocks
for (i = 0; i < height - skipBottom; i++) {
    if (block is air or leaves) place log meta 1 (spruce)
}
```

**Block IDs:** Spruce log `yy.J.bM` meta 1, Spruce leaves `yy.K.bM` meta 1.

---

## 8. WorldGenTaiga2 — Wide Pine (`us`)

Used by Taiga biome (`qk`) with 33% probability (creates `new us()` each time — no shared instance).

### 8.1 Parameters

```java
int height = rand.nextInt(5) + 7;               // [7, 11]
int foliageStart = height - rand.nextInt(2) - 3; // varies
int foliageLayers = height - foliageStart;        // 3-4 layers
int maxRadius = 1 + rand.nextInt(foliageLayers + 1);
```

### 8.2 Canopy placement (top-down)

Unlike `ty`, iterates from **top to bottom** of foliage, expanding radius downward:

```java
int curRadius = 0;
for (y = top; y >= foliageStart; y--) {
    place square of radius curRadius (skipping corners if curRadius > 0)
    
    if (curRadius >= 1 && y == foliageStart + 1) curRadius--;   // shrink near bottom
    else if (curRadius < maxRadius) curRadius++;                  // grow downward
}
```

Uses `world.b()` (with notification, since `us` has no explicit parent `ig` constructor — uses default `a=false`).

### 8.3 Trunk placement
```java
int skipTop = rand.nextInt(3);
for (i = 0; i < height - 1; i++) {
    if (block is air or leaves) world.b(x, y+i, z, log_id, 1)  // spruce log
}
```

**Block IDs:** Spruce log `yy.J.bM` meta 1, Spruce leaves `yy.K.bM` meta 1.
**Note:** `us` uses `world.b()` directly (notifies neighbors).

---

## 9. Disk Generators (not trees)

### 9.1 WorldGenSandDisc (`fc`)

```java
public fc(int maxRadius, int replacementBlockId) {
    this.b = maxRadius;      // constructor param order: (size, block)
    this.a = replacementBlockId;
}
```

Usage in BiomeDecorator: `g = new fc(7, yy.E.bM)` = sand (ID 12), size 7.

**Algorithm:**
1. Check: if material at (x, y, z) is water (`p.g`): return false
2. `radius = rand.nextInt(b - 2) + 2` = [2, b-2] exclusive (for b=7: [2, 5])
3. For each block in circle of radius `radius` around (x, z), and for y-2 to y+2:
   - If block is grass (`yy.u`) or dirt (`yy.v`): replace with `this.a`

**Purpose:** Creates disk-shaped patches of sand (or gravel) replacing topsoil — used for
riverbed sand, river gravel, and desert border effects.

### 9.2 WorldGenClay (`adp`)

```java
private int a = yy.aW.bM;  // clay (ID 82)
private int b;              // max radius

public adp(int maxRadius) { this.b = maxRadius; }
```

Usage: `f = new adp(4)` → clay, size 4.

**Algorithm (identical structure to `fc`):**
1. Check: if material at (x, y, z) is water (`p.g`): return false
2. `radius = rand.nextInt(b - 2) + 2` = [2, b-2] (for b=4: [2, 2], always radius 2)
3. For each block in circle, and for y-1 to y+1 (half-height range = 1 vs fc's 2):
   - If block is grass (`yy.u`) or clay (`yy.aW`): replace with clay

**Purpose:** Creates small clay patches in riverbeds.

---

## 10. Per-Biome Tree Configuration

Tree generation is controlled by two things on each `sr` (BiomeGenBase):
1. Field `B.z` on its `ql` (BiomeDecorator) — how many tree attempts per chunk
2. Override of `a(Random)` — which tree generator to return for each attempt

### 10.1 BiomeDecorator tree loop (from `ql.a()`)

```java
int treeCount = B.z;
if (rand.nextInt(10) == 0) treeCount++;   // occasional bonus tree (10% chance)
for (int i = 0; i < treeCount; i++) {
    int tx = chunkX + rand.nextInt(16) + 8;
    int tz = chunkZ + rand.nextInt(16) + 8;
    ig gen = biome.a(rand);
    gen.a(1.0, 1.0, 1.0);   // default scale (no-op for gq/jp/ty/us; only yd uses it)
    gen.a(world, rand, tx, world.d(tx, tz), tz);   // world.d = getHeightValue (top opaque block)
}
```

### 10.2 Per-biome table

| Biome | ID | `B.z` | `a(Random)` result |
|---|---|---|---|
| Ocean | 0 | 0 (default) | 90% oak / 10% fancy oak |
| Plains | 1 | -999 (no trees) | — |
| Desert | 2 | -999 (no trees) | — |
| Extreme Hills | 3 | 0 (default) | 90% oak / 10% fancy oak |
| Forest | 4 | 10 | 20% birch / 72% oak / 8% fancy oak |
| Taiga | 5 | 10 | 67% wide spruce (`us`) / 33% thin spruce (`ty`) |
| Swampland | 6 | 2 | 100% swamp oak with vines (`qj`) |
| River | 7 | 0 (default) | 90% oak / 10% fancy oak |
| Hell | 8 | 0 (default) | (no trees grow in Nether anyway) |
| Sky | 9 | 0 (default) | — |
| FrozenOcean | 10 | 0 (default) | 90% oak / 10% fancy oak |
| FrozenRiver | 11 | 0 (default) | 90% oak / 10% fancy oak |
| Ice Plains | 12 | 0 (default) | 90% oak / 10% fancy oak |
| Ice Mountains | 13 | 0 (default) | 90% oak / 10% fancy oak |
| MushroomIsland | 14 | 0 (default) | 90% oak / 10% fancy oak |
| MushroomIslandShore | 15 | 0 (default) | 90% oak / 10% fancy oak |

**Forest `a(Random)` breakdown:**
```java
if (rand.nextInt(5) == 0) return I;  // 20% birch (jp)
else return rand.nextInt(10) == 0 ? H : G;  // 8% fancy oak (yd) / 72% oak (gq)
```

**Taiga `a(Random)`:**
```java
return rand.nextInt(3) == 0 ? new us() : new ty(false);  // 33% us / 67% ty
```
Note: `us` is instantiated fresh each call (not a shared field).

### 10.3 Default `sr.a(Random)`:
```java
return rand.nextInt(10) == 0 ? H : G;   // 10% yd / 90% gq
```

### 10.4 z = -999 semantics
The variable `treeCount` starts at -999 and may reach -998 after the +1 bonus. Since the
loop condition `i < treeCount` is `0 < -999` (false), no iterations occur. This is the
intended way to disable tree generation entirely.

---

## 11. Block ID Reference

| Block | Field on `yy` | ID | Log meta | Leaves meta |
|---|---|---|---|---|
| Oak | `J` (log), `K` (leaves) | 17 / 18 | 0 | 0 |
| Spruce | `J` (log), `K` (leaves) | 17 / 18 | 1 | 1 |
| Birch | `J` (log), `K` (leaves) | 17 / 18 | 2 | 2 |
| Vine | `bu` | 106 | — | face bit |
| Sand | `E` | 12 | — | — |
| Gravel | `F` | 13 | — | — |
| Clay | `aW` | 82 | — | — |

---

*Spec written by Analyst AI from `gq.java`, `yd.java`, `jp.java`, `qj.java`, `ty.java`,
`us.java`, `fc.java`, `adp.java`, `ig.java`, `sr.java`, `fo.java`, `qk.java`, `mk.java`,
`ym.java`, `ada.java`. No C# implementation consulted.*
