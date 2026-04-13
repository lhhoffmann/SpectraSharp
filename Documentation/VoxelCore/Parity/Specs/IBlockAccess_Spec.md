# IBlockAccess Spec
Source class: `kq.java`
Type: `interface`
Superclass: none

---

## 1. Purpose

`IBlockAccess` is a read-only interface over world data. It is the parameter type for all
Block rendering and bounds-query methods (e.g. `shouldSideBeRendered`, `getAmbientOcclusion`,
`getTextureForFace`). It allows the Block class to query neighbouring block state without
depending on the concrete `World` class. The `World` class (`ry`) implements this interface.

---

## 2. Method Signatures

All methods are abstract (no default implementations — Java 7 interface). Parameters are
always world coordinates (x=var1, y=var2, z=var3) unless noted.

---

### getBlockId — `a(int x, int y, int z)` → `int`

Returns the block ID at (x, y, z).  
- `0` = air  
- `1`–`255` = registered block IDs

**Confirmed usage:** `Block.canReplace` (`yy.c(ry,x,y,z)`): `var1.a(x,y,z)` to get the block ID.

---

### getTileEntity — `b(int x, int y, int z)` → `bq`

Returns type `bq`. Exact semantics uncertain from Block source alone.  
`bq` is likely the `TileEntity` base class. Returns `null` for blocks without tile entities.

**Usage in Block:** not observed in base class. Called by subclasses (furnace, chest, etc.).

---

### getLightValueAt — `a(int x, int y, int z, int blockLightValue)` → `int`

Takes world coordinates AND the calling block's own light-emission value (e.g. `q[bM]`).
Returns a combined light level as an `int`.

**Confirmed usage:** `Block.e(kq,x,y,z)` (getLightForBlock, line 256): `var1.a(x,y,z, q[bM])`.

---

### getBrightness — `b(int x, int y, int z, int blockLightValue)` → `float`

Takes world coordinates AND the calling block's light-emission value.
Returns a brightness float (range likely 0.0–1.0) used for ambient occlusion.

**Confirmed usage:** `Block.d(float,kq,x,y,z)` (getAmbientOcclusionLightValue): `var1.b(x,y,z, q[bM])`.

---

### unknown float — `c(int x, int y, int z)` → `float`

Returns a float at the given coordinates. Exact semantics unknown from Block base class.
Possibly sky-light level, render brightness, or a biome-related value.

**Usage in Block:** not observed in base class.

---

### getBlockMetadata — `d(int x, int y, int z)` → `int`

Returns the metadata (0–15) stored for the block at (x, y, z).

**Confirmed usage:** `Block.a(kq,x,y,z,int)` (getTextureForFaceInWorld): `var1.d(x,y,z)`.

---

### getBlockMaterial — `e(int x, int y, int z)` → `p`

Returns the Material (`p`) of the block at (x, y, z).

**Confirmed usage:** `Block.e(kq,x,y,z,int)` (isNormalCube): `var1.e(x,y,z).b()`.

---

### isOpaqueCube — `f(int x, int y, int z)` → `boolean`

Returns `true` if the block at (x, y, z) is a fully opaque solid cube.

**Confirmed usage:** `Block.a_(kq,x,y,z,int)` (shouldSideBeRendered): `!var1.f(x,y,z)` —
render the face if the adjacent block is NOT opaque.

---

### isWet (or isFluid) — `g(int x, int y, int z)` → `boolean`

Returns `true` if the block at (x, y, z) is wet / submerged in liquid.

**Confirmed usage:** `Block.f(kq,x,y,z)` (getSlipperiness): `var1.g(x,y,z) ? 0.2F : 1.0F`.

---

### unknown boolean — `h(int x, int y, int z)` → `boolean`

Returns a boolean at the given coordinates. Exact semantics unknown from Block base class.

**Usage in Block:** not observed in base class.

---

### getWorldChunkManager (or similar) — `a()` → `vh`

Returns type `vh`. No-argument. Possibly returns a `WorldChunkManager` or chunk cache
context object used by rendering. Exact semantics unknown.

**Usage in Block:** not observed in base class.

---

### getHeight — `b()` → `int`

Returns an int with no coordinate argument. Possibly world height (128 for classic worlds)
or chunk count. Exact semantics unknown.

**Usage in Block:** not observed in base class.

---

## 3. Summary Table

| Method | Signature | Confirmed semantics | Confidence |
|---|---|---|---|
| `a(x,y,z)` | `int` | getBlockId | Confirmed |
| `b(x,y,z)` | `bq` | getTileEntity | Inferred |
| `a(x,y,z,int)` | `int` | getLightValue(pos, emissionHint) | Confirmed |
| `b(x,y,z,int)` | `float` | getBrightness(pos, emissionHint) | Confirmed |
| `c(x,y,z)` | `float` | unknown | Unknown |
| `d(x,y,z)` | `int` | getBlockMetadata | Confirmed |
| `e(x,y,z)` | `p` | getBlockMaterial | Confirmed |
| `f(x,y,z)` | `boolean` | isOpaqueCube | Confirmed |
| `g(x,y,z)` | `boolean` | isWet/isFluid | Confirmed |
| `h(x,y,z)` | `boolean` | unknown | Unknown |
| `a()` | `vh` | unknown context object | Unknown |
| `b()` | `int` | unknown (possibly world height) | Unknown |

---

## 4. Constants & Magic Numbers

None defined by the interface.

---

## 5. Bitwise & Data Layouts

The light-value method `a(x,y,z,int)` returns an `int` that is used in lighting
calculations. Its exact bit layout (whether it packs block-light + sky-light into one int)
is not determined from Block base class usage alone.

---

## 6. Tick Behaviour

Interface only — no tick behaviour.

---

## 7. Known Quirks / Bugs to Preserve

None identified from Block base class usage. Quirks may exist in the `World` implementation.

---

## 8. Open Questions

1. **`bq` (TileEntity?) spec needed.** Return type of `b(x,y,z)` — needs spec to confirm
   it is the TileEntity base class and what fields it exposes.

2. **`vh` spec needed.** Return type of `a()` — purpose of this context object is unknown.

3. **`c(x,y,z)` → float** and **`h(x,y,z)` → boolean** semantics unknown. Require `World`
   or renderer source to confirm.

4. **`b()` → int** semantics unknown. Possibly `getActualHeight()` (128 for classic map size).

---

*Spec written by Analyst AI from `kq.java` (25 lines) and cross-referenced with `yy.java` (Block). No C# implementation consulted.*
