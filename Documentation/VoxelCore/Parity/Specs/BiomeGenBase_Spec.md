# BiomeGenBase Spec
Source: `sr.java` (BiomeGenBase, 165 lines), `mk.java` (Swampland override),
        `ha.java` (GrassColorizer), `db.java` (FoliageColorizer), `vh.java` (WorldChunkManager)
Type: Class definition + colour system

---

## 1. Overview

`sr` is the `BiomeGenBase` abstract base class. 16 static instances are registered at
startup in `a[256]` (IDs 0–15). Biomes carry temperature, rainfall, height range,
surface block IDs, and colour information.

Colour lookup is delegated to two helper singletons:
- `ha` (GrassColorizer) — maps (temp, rainfall) → packed RGB grass tint
- `db` (FoliageColorizer) — maps (temp, rainfall) → packed RGB foliage tint

Both helpers read from a 256×256 pre-loaded image (`grasscolor.png` / `foliagecolor.png`
from the game JAR), set externally via static setter before world loading.

---

## 2. `ha` — GrassColorizer

```java
public class ha {
    private static int[] a = new int[65536];  // 256×256 RGB data

    public static void a(int[] pixels) { a = pixels; }

    public static int a(double temp, double rainfall) {
        rainfall *= temp;
        int col = (int)((1.0 - temp)    * 255.0);
        int row = (int)((1.0 - rainfall) * 255.0);
        return a[row << 8 | col];   // = a[row * 256 + col]
    }
}
```

**Index formula:**
- `col = (int)((1.0 - temp) * 255.0)` — left (col 0) = hot, right (col 255) = cold
- `row = (int)((1.0 - rainfall * temp) * 255.0)` — top (row 0) = wet, bottom = dry
- lookup: `a[row * 256 + col]`

**Image source:** `grasscolor.png` (256×256, RGBA, separate file in game JAR — not inside terrain.png).

---

## 3. `db` — FoliageColorizer

Identical structure to `ha` but uses `foliagecolor.png`.

Additionally exposes three hardcoded lookup-free values for use when biome
blending is not available (e.g., ItemStack icon rendering):

| Method | Return value | Decimal | Meaning |
|---|---|---|---|
| `db.a()` | 6396257 | 0x619961 | Oak foliage — medium green |
| `db.b()` | 8431445 | 0x80A755 | Birch foliage — lighter yellow-green |
| `db.c()` | 4764952 | 0x489B18 | Spruce foliage — dark green |

`BlockLeaves` uses `db.a()` for oak, `db.b()` for birch, `db.c()` for spruce when
no world context is available (inventory icon).

---

## 4. `vh` — WorldChunkManager

`vh` is the `WorldChunkManager`. `kq.a()` (IBlockAccess method) returns `vh`. It manages
per-column biome assignment using its own noise generators.

Key methods used by BiomeGenBase's colour methods:

| Method | Meaning |
|---|---|
| `a(int x, int z)` → `sr` | getGenBiome — biome at block column (X, Z) |
| `b(int x, int z)` → `float` | getTemperatureAtHeight — temperature at block column |
| `a(int x, int y, int z)` → `float` | getTemperatureAtHeight with Y (decreasing at altitude) |
| `b(int x, int z)` → `float` (2-arg) | getRainfallAtHeight — rainfall at block column |
| `a(sr[], int x, int z, int w, int h)` → `sr[]` | getBiomesForGeneration — fills biome array for chunk gen |
| `b(sr[], int x, int z, int w, int h)` → `sr[]` | loadBlockGeneratorData — fills w×h biome array for surface pass |

The `sr.a(kq, x, y, z)` method calls:
```java
double temp     = world.a().a(x, y, z);  // vh.a(x, y, z) — temp at height
double rainfall = world.a().b(x, z);      // vh.b(x, z) — rainfall
return ha.a(temp, rainfall);
```

---

## 5. Fields on `sr`

| Field | Type | Default | Meaning |
|---|---|---|---|
| `F` | `int` | (constructor) | Biome ID |
| `r` | `String` | — | Biome name |
| `s` | `int` | — | Map colour (minimap) |
| `t` | `byte` | `yy.u.bM` = 2 | Top block ID (Grass) |
| `u` | `byte` | `yy.v.bM` = 3 | Filler block ID (Dirt) |
| `v` | `int` | 5169201 | Water colour multiplier |
| `w` | `float` | 0.1F | Min height (Y offset from sea, used in terrain gen) |
| `x` | `float` | 0.3F | Max height (terrain amplitude) |
| `y` | `float` | 0.5F | Temperature (0.0 = arctic, 2.0 = desert) |
| `z` | `float` | 0.5F | Rainfall (0.0 = arid, 1.0 = tropical) |
| `A` | `int` | 16777215 | Grass/foliage colour override; 0xFFFFFF = no override |
| `B` | `ql` | `a()` | BiomeDecorator instance |
| `K` | `boolean` | false | Is raining |
| `L` | `boolean` | true | Has weather |

> **Temperature guard:** `a(float temp, float rain)` throws `IllegalArgumentException`
> if `temp` is in range 0.1–0.2 (exclusive). This range causes snow-level transition bugs.
> No biome should have temperature between 0.1 and 0.2.

---

## 6. Builder Methods

| Call | Effect |
|---|---|
| `a(String name)` | Sets `r` |
| `b(int mapColor)` | Sets `s` |
| `a(int waterColor)` | Sets `v` |
| `a(float temp, float rain)` | Sets `y`, `z` (throws if temp ∈ (0.1, 0.2)) |
| `b(float minH, float maxH)` | Sets `w`, `x` |
| `g()` | Sets `L = false` (no weather / no precipitation) |

---

## 7. Instance Methods

### `a(kq world, int x, int y, int z)` → int — getGrassColor
Default: `return ha.a(temp, rainfall)` using `world.a()` (WorldChunkManager).
Swampland (`mk`) overrides: `return ((ha.a(t,r) & 0xFEFEFE) + 5115470) / 2` — averages with the swamp tint colour.

### `b(kq world, int x, int y, int z)` → int — getFoliageColor
Default: `return db.a(temp, rainfall)`.
Swampland (`mk`) overrides: `return ((db.a(t,r) & 0xFEFEFE) + 5115470) / 2`.

### `a(Random)` → `ig` — getRandomWorldGenForTrees
Returns `H` (OakTree, 10-in-1 chance) or `G` (BigTree, 1-in-10 chance).

### `a(ry, Random, int x, int z)` — decorate
Calls `B.a(world, rand, x, z)` — the BiomeDecorator.

### `c()` → boolean — getEnableSnow
Returns `!K && L` — true for temperate/cold biomes with weather.

### `e()` → int — getRainfall (as fixed-point)
Returns `(int)(z * 65536.0F)`.

### `f()` → int — getTemperature (as fixed-point)
Returns `(int)(y * 65536.0F)`.

---

## 8. Biome Registry (IDs 0–15)

| ID | Field | Class | Name | Map color | Temp | Rain | MinH | MaxH | Notes |
|---|---|---|---|---|---|---|---|---|---|
| 0 | `b` | `aeq` | Ocean | — | 0.5 | 0.5 | -1.0 | 0.4 | deep water |
| 1 | `c` | `ym` | Plains | 9286496 | 0.8 | 0.4 | 0.1 | 0.3 | default |
| 2 | `d` | `ada` | Desert | 16421912 | 2.0 | 0.0 | 0.1 | 0.2 | no weather |
| 3 | `e` | `az` | Extreme Hills | 6316128 | 0.2 | 0.3 | 0.2 | 1.8 | mountains |
| 4 | `f` | `fo` | Forest | 353825 | 0.7 | 0.8 | 0.1 | 0.3 | grassColor=5159473 |
| 5 | `g` | `qk` | Taiga | 747097 | 0.3 | 0.8 | 0.1 | 0.4 | grassColor=5159473 |
| 6 | `h` | `mk` | Swampland | 522674 | 0.8 | 0.9 | -0.2 | 0.1 | grassColor=9154376, special color blend |
| 7 | `i` | `lq` | River | — | 0.5 | 0.5 | -0.5 | 0.0 | |
| 8 | `j` | `av` | Hell | 16711680 | 2.0 | 0.0 | 0.1 | 0.3 | Nether, no weather |
| 9 | `k` | `gu` | Sky | 8421631 | 0.5 | 0.5 | 0.1 | 0.3 | End, no weather |
| 10 | `l` | `aeq` | FrozenOcean | 9474208 | 0.0 | 0.5 | -1.0 | 0.5 | |
| 11 | `m` | `lq` | FrozenRiver | 10526975 | 0.0 | 0.5 | -0.5 | 0.0 | |
| 12 | `n` | `ce` | Ice Plains | 16777215 | 0.0 | 0.5 | 0.1 | 0.3 | |
| 13 | `o` | `ce` | Ice Mountains | 10526880 | 0.0 | 0.5 | 0.2 | 1.8 | |
| 14 | `p` | `aev` | MushroomIsland | 16711935 | 0.9 | 1.0 | 0.2 | 1.0 | |
| 15 | `q` | `aev` | MushroomIslandShore | 10486015 | 0.9 | 1.0 | -1.0 | 0.1 | |

> Biome IDs 16+ exist in later versions but not in 1.0.
> `t` (topBlock) and `u` (fillerBlock) default to grass (ID 2) and dirt (ID 3).
> Overrides exist in subclasses (e.g., Desert uses sand/sandstone, Ice Plains uses snow).

---

## 9. Swampland Special Color Formula

`mk` (Swampland, ID 6) overrides both color methods:
```
grassColor = ((ha.a(temp, rainfall) & 0xFEFEFE) + 5115470) / 2
```
- Mask `0xFEFEFE` = `16711422` clears the low bit of each channel (prevents overflow on add).
- `5115470` = the swamp base colour (a dark olive green).
- Dividing by 2 averages the biome color with the swamp tint.

Swampland also sets `A = 14745456` = `0xE0FF70` — a yellowish-green used for some colour overrides.

---

## 10. IBlockAccess.a() — WorldChunkManager Access

The 13th method on `kq` (IBlockAccess) — added to IBlockAccess spec:
```
vh a();   // returns WorldChunkManager
```
Called by `sr.a(kq,x,y,z)` to get climate data for colour lookup.

---

*Spec written by Analyst AI from `sr.java`, `ha.java`, `db.java`, `mk.java`, `vh.java`. No C# implementation consulted.*
