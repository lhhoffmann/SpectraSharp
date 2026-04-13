# Block Spec
Source class: `yy.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`Block` is the base class for every block type in the game. It serves three distinct roles:

1. **Static registry** — holds a global `Block[256]` array (indexed by block ID) plus eight
   parallel boolean/int arrays for per-ID metadata that is queried at high frequency.
2. **Block definitions** — all block singletons (stone, grass, water, …) are `public static final`
   fields of this class, initialised in declaration order and in the static initializer.
3. **Virtual behaviour contract** — defines default no-op or sensible-default implementations
   of every block event (tick, neighbour change, placement, removal, collision, ray cast, drops).
   Subclasses override what they need.

---

## 2. Static Material Constants

These `wu` (Material) instances are defined as static finals and used as constructor arguments
for block instances. They are declared before the block registry arrays.

| Field (obf) | Human name | Material string | Notes |
|---|---|---|---|
| `b` | `materialStone` | `"stone"` | Standard stone/rock |
| `c` | `materialWood` | `"wood"` | Wood |
| `d` | `materialGround` | `"gravel"` | Dirt/gravel |
| `e` | `materialGrass` | `"grass"` | Grass |
| `f` | `materialRock` | `"stone"` | Stone variant (used for iron-like blocks) |
| `g` | `materialIron` | `"stone"` | Stone with resistance 1.5× |
| `h` | `materialGlass` | `"stone"` | `bj` subclass (liquid behaviour, used for glass/ice) |
| `i` | `materialCloth` | `"cloth"` | Wool |
| `j` | `materialSand` | `"sand"` | `aeg` subclass (gravity physics) |

---

## 3. Static Registry Arrays

All arrays have size 256. Index 0 = air (all arrays default to false/0 for air, except
`p[0]` which is set to `true` in the static initializer).

| Array (obf) | Type | Human name | How set | Meaning |
|---|---|---|---|---|
| `k` | `yy[]` | `blocksList` | Set in constructor | Global block registry. `k[id]` = the block instance, or null for unregistered IDs. Air (0) = null. |
| `l` | `boolean[]` | `isBlockContainer`? | Builder `b(true)` | True for blocks that are containers (chests etc.); false by default. |
| `m` | `boolean[]` | `isOpaqueCube` | Constructor: `this.a()` | True if the block is a full opaque cube (default true). |
| `n` | `boolean[]` | (unknown) | Constructor: always `false` | Initialized false; never set true in constructor or any builder seen. Always false for vanilla blocks. |
| `o` | `int[]` | `lightOpacity` | Constructor: 255 if opaque, else 0; builder `h(int)` | How much light this block blocks. 0–255. |
| `p` | `boolean[]` | `canBlockGrass` | Constructor: `!material.c()` | True for non-liquid solid blocks. Used in grass spread checks. `p[0] = true` (air, set in static initializer). |
| `q` | `int[]` | `lightValue` | Builder `a(float)`: `(int)(15.0F * multiplier)` | Light emitted by the block; 0–15. |
| `r` | `boolean[]` | `hasTileEntity` | Builder `l()`: sets `r[bM] = true` | True for blocks with tile entities (furnace, chest, …). |
| `s` | `boolean[]` | (rendering special) | Static initializer | True for blocks with non-standard rendering (farmland, slabs, blocks returning `c() == 10`). |

---

## 4. Instance Fields

| Field (obf) | Type | Default | Human name | Semantics |
|---|---|---|---|---|
| `bL` | `int` | 0 | `blockIndexInTexture` | Texture atlas index. Set by 3-arg constructor or directly by subclass. |
| `bM` | `int` (final) | set in constructor | `blockID` | Block ID (0–255). Immutable. |
| `bN` | `float` | 0.0F | `blockHardness` | Mining time multiplier. Negative (-1.0F) means unbreakable. |
| `bO` | `float` | 0.0F | `blockResistance` | Blast resistance × 3. See builder interaction in section 6. |
| `bP` | `boolean` | `true` | (unknown) | Not set by any visible builder; always `true` in base class. |
| `bQ` | `boolean` | `true` | `needsRandomTick`? | Set to `false` by builder `r()`. |
| `bR` | `double` | 0.0 | `minX` | West face of block-local bounding box. |
| `bS` | `double` | 0.0 | `minY` | Bottom face. |
| `bT` | `double` | 0.0 | `minZ` | North face. |
| `bU` | `double` | 1.0 | `maxX` | East face. |
| `bV` | `double` | 1.0 | `maxY` | Top face. |
| `bW` | `double` | 1.0 | `maxZ` | South face. |
| `bX` | `wu` | `yy.b` (stone) | `blockMaterial` | The material of this block. Default is stone material. |
| `bY` | `float` | 1.0F | (unknown) | Not set by any builder seen. |
| `bZ` | `p` (final) | set in constructor | `stepSound` | Sound group for walking on / placing / breaking. |
| `ca` | `float` | 0.6F | (unknown, possibly slipperiness) | Not set by any builder. |
| `a` | `String` | null | `blockName` | Translation key, stored as `"tile." + name`. null until `a(String)` builder is called. |

---

## 5. Constructors

### 2-argument constructor — `yy(int var1, p var2)`

Parameters: block ID (var1), step sound (var2).

Step-by-step:
1. Guard: if `k[var1] != null`, throw `IllegalArgumentException("Slot " + var1 + " is already occupied by " + k[var1] + " when adding " + this)`.
2. `this.bZ = var2`
3. `k[var1] = this`
4. `this.bM = var1`
5. Call `this.a(0.0F, 0.0F, 0.0F, 1.0F, 1.0F, 1.0F)` — sets default full-unit-cube bounds
6. `m[var1] = this.a()` — stores result of virtual `isOpaqueCube()` at construction time
7. `o[var1] = this.a() ? 255 : 0` — lightOpacity: full opacity if opaque
8. `p[var1] = !var2.c()` — canBlockGrass: true if material is not liquid
9. `n[var1] = false`

Note: `this.a()` is called virtually at step 6/7. If a subclass overrides `a()` and the
subclass constructor runs before calling `super()`, the virtual call resolves to the
subclass override. In Java, virtual calls in constructors use the final object's type.

### 3-argument constructor — `yy(int var1, int var2, p var3)`

Parameters: block ID (var1), texture index (var2), step sound (var3).

Step-by-step:
1. Call `this(var1, var3)` — delegates to 2-arg constructor
2. `this.bL = var2`

---

## 6. Builder Methods (all return `this`)

Builders are called on the newly-created instance via method chaining in the static
field initializers. They configure the instance and return `this`.

### `c(float var1)` — setHardness

1. `this.bN = var1`
2. If `this.bO < var1 * 5.0F`: set `this.bO = var1 * 5.0F`
3. Return `this`

The second step sets a minimum blast resistance of 5× the hardness. Calling `b(float)`
after `c(float)` will always overwrite `bO` regardless.

### `b(float var1)` — setResistance

1. `this.bO = var1 * 3.0F`
2. Return `this`

### `m()` — setUnbreakable

1. Calls `this.c(-1.0F)` — hardness = -1.0F means unbreakable in mining calculations
2. Return `this`

### `a(float var1)` — setLightValue

1. `q[this.bM] = (int)(15.0F * var1)`
2. Return `this`

The parameter is a fraction 0.0–1.0. Multiply by 15 and truncate to get the 0–15 light level.

### `h(int var1)` — setLightOpacity

1. `o[this.bM] = var1`
2. Return `this`

Overrides the default set in the constructor.

### `a(wu var1)` — setMaterial (overrides `bX`)

Wait: this sets `this.bX = var1`. The material was already set via the step-sound constructor
path, but this builder overrides it. Only some blocks call it; most set material via the `p`
(step sound) constructor parameter.

1. `this.bX = var1`
2. Return `this`

### `l()` — setHasTileEntity

1. `r[this.bM] = true`
2. Return `this`

### `r()` — clearNeedsRandomTick (sets `bQ = false`)

1. `this.bQ = false`
2. Return `this`

### `b(boolean var1)` — setIsContainer

1. `l[this.bM] = var1`
2. Return `this`

### `a(String var1)` — setBlockName

1. `this.a = "tile." + var1`
2. Return `this`

The prefix `"tile."` is always prepended. The stored string is the full translation key.

---

## 7. Methods — Detailed Logic

### isOpaqueCube — `a()` → `boolean`

Default: returns `true`.  
Subclasses that are non-full or transparent override to return `false`.
Result is cached in `m[bM]` at construction time.

---

### renderAsNormalBlock — `k()` → `boolean`

Default: returns `true`.  
Subclasses that use a non-cube render model override to return `false`.

---

### isCollidable — `b()` → `boolean`

Default: returns `true`.

---

### getTickRandomly — `c()` → `int`

Default: returns `0`.  
Used in static initializer to flag blocks with `c() == 10` into `s[]`.

---

### getHardness — `n()` → `float`

Returns `this.bN`.

---

### getHardness (player-relative) — `a(vi var1)` → `float`

Parameters: `vi` = Player.

Step-by-step:
1. If `this.bN < 0.0F`: return `0.0F` (unbreakable block — no progress)
2. If `!var1.b(this)` (player cannot harvest this block type): return `1.0F / bN / 100.0F`
3. Else: return `var1.a(this) / bN / 30.0F`

`var1.b(yy)` = `canHarvestBlock(block)`.
`var1.a(yy)` = `getMiningSpeed(block)` (tool efficiency multiplier, ≥ 1.0F).

The caller accumulates this value over ticks. When the sum reaches 1.0 the block breaks.

---

### setBounds — `a(float, float, float, float, float, float)` → `void`

Sets the per-block bounding box in block-local space [0,1]³.

Step-by-step:
1. `bR = (double)var1` (minX)
2. `bS = (double)var2` (minY)
3. `bT = (double)var3` (minZ)
4. `bU = (double)var4` (maxX)
5. `bV = (double)var5` (maxY)
6. `bW = (double)var6` (maxZ)

Values are cast from float to double. The default call `a(0F,0F,0F,1F,1F,1F)` in the
constructor sets the full unit cube.

---

### setBlockBoundsBasedOnState — `b(kq var1, int var2, int var3, int var4)` → `void`

Parameters: world-like accessor `kq`, block coords.

Default: empty no-op.  
Override point for blocks whose shape depends on block metadata or neighbours (pistons,
fences, etc.). Called at the start of `collisionRayTrace` to ensure bounds are up to date.

---

### getCollisionBoundingBoxFromPool — `c_(ry var1, int var2, int var3, int var4)` → `c`

Returns the world-space collision AABB (pooled) for this block at position (var2, var3, var4).

Step-by-step:
1. Return `c.b((double)var2 + bR, (double)var3 + bS, (double)var4 + bT, (double)var2 + bU, (double)var3 + bV, (double)var4 + bW)`

Uses `AxisAlignedBB.getFromPool`. Adds block position offset to local bounds.

---

### getSelectedBoundingBoxFromPool — `b(ry var1, int var2, int var3, int var4)` → `c`

Returns the world-space selection highlight AABB. Default is identical to the collision box.

Step-by-step:
1. Return `c.b(var2 + bR, var3 + bS, var4 + bT, var2 + bU, var3 + bV, var4 + bW)`

---

### addCollisionBoxesToList — `a(ry var1, int var2, int var3, int var4, c var5, ArrayList var6)` → `void`

Adds the block's collision box to `var6` if it intersects `var5` (the entity's expanded sweep box).

Step-by-step:
1. `c var7 = this.b(var1, var2, var3, var4)` — get collision box via `getSelectedBoundingBoxFromPool`
2. If `var7 != null` AND `var5.a(var7)` (boxes intersect): `var6.add(var7)`

---

### collisionRayTrace — `a(ry var1, int var2, int var3, int var4, fb var5, fb var6)` → `gv` or `null`

World-space ray intersection with this block's bounding box.

Parameters: World, blockX, blockY, blockZ, ray start (Vec3), ray end (Vec3).

Step-by-step:
1. Call `this.b((kq)var1, var2, var3, var4)` — `setBlockBoundsBasedOnState` to refresh bounds
2. Translate ray into block-local space:
   - `var5 = var5.c(-(double)var2, -(double)var3, -(double)var4)` — `Vec3.add` with negated coords
   - `var6 = var6.c(-(double)var2, -(double)var3, -(double)var4)`
3. Compute 6 face-plane intersections using the block-local bounds (`bR/bS/bT/bU/bV/bW`):
   - `var7 = var5.a(var6, bR)` — X = minX face
   - `var8 = var5.a(var6, bU)` — X = maxX face
   - `var9 = var5.b(var6, bS)` — Y = minY face
   - `var10 = var5.b(var6, bV)` — Y = maxY face
   - `var11 = var5.c(var6, bT)` — Z = minZ face
   - `var12 = var5.c(var6, bW)` — Z = maxZ face
4. Validate each candidate with private helpers (closed-interval bounds check, see section 7 below). Null-out invalid candidates.
5. Find the closest valid candidate `var13` by comparing `var5.d(candidate)` (Euclidean distance, float precision via `Vec3.distanceTo`). Uses strict `<` — first candidate wins ties in enumeration order (var7, var8, var9, var10, var11, var12).
6. If `var13 == null`: return `null`.
7. Assign face ID `var14` (byte, starts at -1):
   - var13 == var7 → 4 (−X / minX)
   - var13 == var8 → 5 (+X / maxX)
   - var13 == var9 → 0 (−Y / minY)
   - var13 == var10 → 1 (+Y / maxY)
   - var13 == var11 → 2 (−Z / minZ)
   - var13 == var12 → 3 (+Z / maxZ)
   Assignments are sequential; last match wins.
8. Translate hit point back to world space: `var13.c((double)var2, (double)var3, (double)var4)`
   (`Vec3.add` with block position)
9. Return `new gv(var2, var3, var4, var14, translatedHitPoint)` — with real block coordinates.

**Critical difference from `AxisAlignedBB.rayTrace`:**
- This method uses `Vec3.d(fb)` (Euclidean distance, float result) for closest-hit selection.
- `AxisAlignedBB.rayTrace` uses `Vec3.e(fb)` (squared distance) for the same purpose.
- Both give the same ordering for positive distances; the method called is different and must be preserved.

#### Private face validators (Block-local)

**`a(fb var1)` — X-face YZ bounds check**  
Returns `false` if var1 is null.  
Returns `var1.b >= bS && var1.b <= bV && var1.c >= bT && var1.c <= bW`

**`b(fb var1)` — Y-face XZ bounds check**  
Returns `false` if var1 is null.  
Returns `var1.a >= bR && var1.a <= bU && var1.c >= bT && var1.c <= bW`

**`c(fb var1)` — Z-face XY bounds check**  
Returns `false` if var1 is null.  
Returns `var1.a >= bR && var1.a <= bU && var1.b >= bS && var1.b <= bV`

All use closed intervals (≤ / ≥), same as `AxisAlignedBB`'s private validators.

---

### blockTick (random update) — `a(ry var1, int var2, int var3, int var4, Random var5)` → `void`

Default: empty no-op. Override in subclasses for random tick logic (grass spread, leaf decay, etc.)

---

### updateTick (scheduled update) — `b(ry var1, int var2, int var3, int var4, Random var5)` → `void`

Default: empty no-op. Override for scheduled (non-random) ticks.

---

### onNeighborBlockChange — `e(ry var1, int var2, int var3, int var4, int var5)` → `void`

Parameters: World, x, y, z, neighbour block ID (var5).  
Default: empty no-op.

---

### onBlockAdded — `a(ry var1, int var2, int var3, int var4)` → `void`

Called when the block is placed in the world.  
Default: empty no-op.

---

### onBlockRemoved — `d(ry var1, int var2, int var3, int var4)` → `void`

Called when the block is removed/replaced.  
Default: empty no-op.

---

### onBlockDestroyedByPlayer — `a(ry var1, int var2, int var3, int var4, int var5)` → `void`

Parameters: World, x, y, z, metadata.  
Default: empty no-op.

---

### quantityDropped — `a(Random var1)` → `int`

Returns `1` (drop one item by default).

---

### idDropped — `a(int var1, Random var2, int var3)` → `int`

Parameters: metadata (var1), random (var2), fortune level (var3).  
Returns `this.bM` (drop the block itself by default).

---

### quantityDroppedWithBonus — `a(int var1, Random var2)` → `int`

Parameters: fortune level (var1), random (var2).  
Calls `this.a(var2)` (quantityDropped). Default ignores fortune.

---

### dropBlockAsItem — `b(ry, int, int, int, int, int)` → `void` (final)

Parameters: World, x, y, z, metadata, fortune.  
Calls `a(ry, x, y, z, metadata, 1.0F, fortune)` — 100% drop rate.

---

### dropBlockAsItemWithChance — `a(ry var1, int var2, int var3, int var4, int var5, float var6, int var7)` → `void`

Parameters: World, x, y, z, metadata, dropChance, fortune.

Step-by-step:
1. If `var1.I` (world is client-side): return immediately
2. `var8 = this.a(var7, var1.w)` — quantityDroppedWithBonus(fortune, worldRandom)
3. For `var9` from 0 to var8-1:
   a. If `var1.w.nextFloat() > var6`: skip (probabilistic drop gate)
   b. `var10 = this.a(var5, var1.w, var7)` — idDropped(metadata, random, fortune)
   c. If `var10 > 0`: spawn `EntityItem` at a randomised position within the block:
      - `var7_pos = random * 0.7 + 0.15` per axis (range ≈ [0.15, 0.85])
      - Entity is created at `(x + jitter, y + jitter, z + jitter)`

The jitter formula: `(random.nextFloat() * 0.7F) + (1.0F - 0.7F) * 0.5F`  
= `random * 0.7 + 0.15`, range [0.15, 0.85].

---

### spawnAsEntity — `a(ry var1, int var2, int var3, int var4, dk var5)` → `void` (protected)

Parameters: World, x, y, z, ItemStack.

Step-by-step:
1. If `var1.I`: return
2. Compute random offset per axis: `(random * 0.7) + 0.15` (same formula as above)
3. Create `new ih(world, x+jitter, y+jitter, z+jitter, itemstack)` (EntityItem)
4. Set `entityItem.c = 10` (delay before the item can be picked up — 10 ticks = 0.5 s)
5. Call `var1.a(entityItem)` (spawnEntityInWorld)

---

### getTextureIndex (by face) — `b(int var1)` → `int`

Default: returns `this.bL` (ignores face parameter).

---

### getTextureForFaceAndMeta — `a(int var1, int var2)` → `int`

Default: calls `this.b(var1)` — returns `bL`, ignores metadata.

---

### getTextureIndex (by face, meta, world) — `a(kq var1, int var2, int var3, int var4, int var5)` → `int`

Returns `this.a(var5, var1.d(x,y,z))` — calls `getTextureForFaceAndMeta` with the block's stored metadata.

---

### shouldSideBeRendered — `a_(kq var1, int var2, int var3, int var4, int var5)` → `boolean`

Returns `true` if this face (var5) should be rendered (exposed / not fully covered by bounds).

Face ID mapping:
- 0 (−Y / bottom): return `true` if `bS > 0.0`
- 1 (+Y / top): return `true` if `bV < 1.0`
- 2 (−Z / north): return `true` if `bT > 0.0`
- 3 (+Z / south): return `true` if `bW < 1.0`
- 4 (−X / west): return `true` if `bR > 0.0`
- 5 (+X / east): return `true` if `bU < 1.0`
- Any other face ID: fall through to `!var1.f(x,y,z)`

The default case (`!var1.f(x,y,z)`) calls a method on `kq` (world-like) that returns
whether the adjacent block is fully opaque. The face is rendered if the adjacent block
is NOT opaque.

---

### canReplace — `c(ry var1, int var2, int var3, int var4)` → `boolean`

Returns `true` if the block at (x,y,z) is air or its material is replaceable.

Step-by-step:
1. `var5 = var1.a(x,y,z)` — block ID at position
2. Return `var5 == 0 || k[var5].bZ.i()` — air, or material is replaceable

`bZ.i()` is a method on the step-sound (`p`) class. The field `bZ` is the step sound, and its `i()` method returns a boolean related to replaceability. (See dependency note.)

---

### canBlockStay — `e(ry var1, int var2, int var3, int var4)` → `boolean`

Default: returns `true`.

---

### getTickDelay — `d()` → `int`

Default: returns `10`. This is the random tick delay denominator.

---

### getMobilityFlag — `h()` → `int`

Default: returns `0`.
- 0 = can be pushed by pistons
- 1 = cannot be pushed or pulled
- 2 = can be pushed but not pulled (sticky piston)

---

### getLightBrightness (AO) — `d(float var1, kq var2, int var3, int var4, int var5)` → `float`

Returns `var2.b(x,y,z, q[bM])` — ambient occlusion value from world.

---

### hasTileEntity — `g()` → `boolean`

Default: returns `false`. Overridden by blocks that use tile entities.
(Note: `r[]` array is the fast-path equivalent; `g()` is the virtual override route.)

---

### isNormalCube (for side rendering) — `e(kq var1, int var2, int var3, int var4, int var5)` → `boolean`

Returns `var1.e(x,y,z).b()` — gets the block's material from the world and calls `b()` on it.

---

### renderColorMultiplier variants

Three methods that return a colour tint for block rendering:

- `f()` → `int`: returns `16777215` (white, 0xFFFFFF)
- `c(int var1)` → `int`: returns `16777215` (ignores metadata)
- `a(kq var1, int var2, int var3, int var4)` → `int`: returns `16777215` (ignores world/position)

---

### isSideSolid — `b(kq var1, int var2, int var3, int var4, int var5)` → `boolean`

Default: returns `false`.

---

### canProvideSupport (for torches, etc.) — `c(ry, int, int, int, int)` → `boolean`

Default: returns `false`.

---

### setBlockName — `a(String var1)` → `yy`

Sets `this.a = "tile." + var1`. Returns `this`.

---

### getLocalizedName — `o()` → `String`

Returns `hj.a(this.p() + ".name")` — looks up the translation of `tileKey + ".name"`.

---

### getUnlocalizedName — `p()` → `String`

Returns `this.a` (the raw translation key, e.g. `"tile.stone"`).

---

### isNeedsRandomTick — `q()` → `boolean`

Returns `this.bQ` (default `true`; set to `false` by builder `r()`).

---

### getLightOpacity — `i()` → `int`

Returns `this.bZ.l()` — delegates to the step-sound class.  
(Dependency: need `p` / `StepSound` spec to confirm what `l()` returns.)

---

### getSlipperiness — `f(kq var1, int var2, int var3, int var4)` → `float`

Returns `0.2F` if `var1.g(x,y,z)` (some "wet" or liquid condition), else `1.0F`.

---

## 8. Static Initializer

Runs once when the class is first loaded. The class has both static field initializers
(for `b`..`bK` block definitions, lines 5–144) and an explicit `static { }` block
(lines 630–671).

### Static field initialization order

1. Material constants (`b` through `j`)
2. Array allocations (`k` through `s`)
3. All block singletons (`t` through `bK`) in declaration order.
   Each block constructor registers itself into `k[]`.
   Blocks that reference other block singletons (`ahh(53, x)` = wooden stairs based on
   plank block `x`) require those referenced blocks to be already initialised — which is
   guaranteed by declaration order.

### Explicit static block

1. Create three blocks that require other blocks as constructor args and can't use
   inline initialization due to forward references:
   - `aK` (pressure plate, stone) — uses `t.bL` and `p.e`
   - `aM` (pressure plate, wood) — uses `x.bL` and `p.d`
   - `aR` (stone button) — uses `t.bL`
2. Register item drop variants for specific blocks in `acy.d[]` array (wool, log,
   stone brick, stone slab, sapling, leaves, vine, tall grass, lily pad, piston).
3. Loop over all 256 IDs:
   - If `acy.d[var0] == null` (no special item variant registered): create default item stub `uw(blockId - 256)`; call `x_()` on the block (no-op in base class).
   - Determine `s[var0]` = `true` if any of:
     - blockId > 0 AND `k[var0].c() == 10`
     - blockId > 0 AND block is instance of `xs` (slab)
     - blockId == `aA.bM` (farmland, ID 60)
4. `p[0] = true` (air slot is passable)
5. Call `ny.b()` (initialise something; `ny` not yet specced)

---

## 9. Block Registry — ID Map (partial)

Key block IDs referenced by the spec:

| ID | Field | Block name string |
|---|---|---|
| 0 | (null) | air |
| 1 | `t` | `"stone"` |
| 2 | `u` | `"grass"` |
| 3 | `v` | `"dirt"` |
| 4 | `w` | `"stonebrick"` (cobblestone) |
| 5 | `x` | `"wood"` (oak planks) |
| 7 | `z` | `"bedrock"` |
| 8 | `A` | `"water"` (flowing) |
| 9 | `B` | `"water"` (still) |
| 10 | `C` | `"lava"` (flowing) |
| 11 | `D` | `"lava"` (still) |
| 17 | `J` | `"log"` |
| 18 | `K` | `"leaves"` |
| 51 | `ar` | `"fire"` |
| 60 | `aA` | `"farmland"` |
| 90 | `be` | `"portal"` |

Full ID-to-field mapping can be derived from the static field declarations (lines 23–144).
IDs not listed in `k[]` after static init = unregistered (null).

---

## 10. Bitwise & Data Layouts

No bitwise operations in the base class. Subclasses may use block metadata (0–15).

---

## 11. Tick Behaviour

The base class has two tick entry points, both no-ops by default:

- `a(World, x, y, z, Random)` — random tick, called at a rate governed by world tick logic per chunk section. Most blocks with `q()` returning `true` may receive this.
- `b(World, x, y, z, Random)` — scheduled tick, called when the world schedules a block update.

---

## 12. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | `c(float)` / `b(float)` interaction | `c(hardness)` raises `bO` to at least `hardness * 5.0F`. If `b(resistance)` is called after `c(hardness)`, it overwrites `bO` to `resistance * 3.0F` regardless. Call order in the builder chain determines the final value. | **Yes** |
| 2 | `collisionRayTrace` distance metric | Uses Euclidean distance (`Vec3.d(fb)` = float-precision sqrt) for closest-hit selection. `AxisAlignedBB.rayTrace` uses squared distance (`Vec3.e(fb)`). Both give the same face ordering but the method called differs. | **Yes** |
| 3 | `collisionRayTrace` ray translation | Subtracts block position BEFORE intersecting, then adds it back AFTER. The plane intersection operates in block-local [0,1]³ space. | **Yes** |
| 4 | `dropBlockAsItemWithChance` jitter | Item spawn position uses `(rnd * 0.7) + 0.15` per axis, NOT `rnd * 1.0 + 0.0`. Items appear in the centre third of the block. | **Yes** |
| 5 | `spawnAsEntity` pickup delay | Spawned EntityItem has a 10-tick pickup delay (`c = 10`). Do not set to 0. | **Yes** |
| 6 | `shouldSideBeRendered` face IDs | Uses face IDs 0–5 with the same mapping as ray-trace: 0=bottom, 1=top, 2=north, 3=south, 4=west, 5=east. | **Yes** |
| 7 | Constructor virtual call | `this.a()` (isOpaqueCube) is called virtually in the base constructor. If a subclass overrides `a()` and is registered in `k[]`, the virtual call uses the subclass version at the time of construction. | **Yes** |

---

## 13. Open Questions

1. **`p` (StepSound) spec needed.** The `bZ` field is a step sound, and `bZ.i()` is
   called in `canReplace` and `getLightOpacity`. Its method signatures must be confirmed.

2. **`wu` (Material) spec needed.** The `c()` method on Material is called in the
   constructor to set `p[]`. Need to confirm what `wu.c()` returns (isLiquid? isReplaceable?).

3. **`kq` interface spec needed.** Methods `f()`, `g()`, `e()`, `a()`, `b()` are called
   on `kq` in several block methods. `kq` appears to be a read-only world view used during
   rendering and bounds queries.

4. **`vi` (Player) spec needed.** The mining method `a(vi)` calls `var1.b(this)` and
   `var1.a(this)`. These are `canHarvestBlock` and `getMiningSpeed`.

5. **`ny` initialisation.** The static block calls `ny.b()` at the end. Purpose unknown.
   Likely block rendering or name registration. Does not affect block physics.

---

*Spec written by Analyst AI from `yy.java` (673 lines, decompiled). No C# implementation consulted.*
