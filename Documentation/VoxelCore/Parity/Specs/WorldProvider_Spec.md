# WorldProvider Spec
Source class: `k.java`
Type: `abstract class`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`WorldProvider` encapsulates all dimension-specific rules: whether the dimension has sky-light,
the sky/fog colour function, moon phase, the spawn-validity predicate, and which `ChunkLoader`
to use. The `World` class holds a `WorldProvider` reference in field `y`. Three concrete
subclasses exist: `ix` (Overworld, dim 0), `aau` (Nether, dim −1), `ol` (End, dim 1).

---

## 2. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `ry` | set by `a(ry)` | World reference — assigned on registration |
| `b` | `vh` | created by `b()` | WorldChunkManager — returned by `IBlockAccess.a()` |
| `c` | `boolean` | `false` | Unknown dimension flag (unused in base class) |
| `d` | `boolean` | `false` | Unknown dimension flag (unused in base class) |
| `e` | `boolean` | `false` | **isNether** — when `true`, sky-light is suppressed everywhere. Referenced throughout `World` and `Chunk` as `world.y.e`. Must be `true` for the Nether subclass (`aau`). |
| `f[16]` | `float[]` | filled by `a()` | Sky-to-brightness lookup: `f[lightLevel]` = brightness float for sky-light level 0–15 |
| `g` | `int` | `0` | Dimension ID (−1 / 0 / 1) — exact usage unclear from this file |
| `h[4]` | `float[]` (private) | reused buffer | Sunrise/sunset colour RGBA buffer; returned from `a(angle, rain)` |

---

## 3. Brightness Table

Filled by protected `a()` called during `registerWorld`:

```java
float var1 = 0.0F;  // ambient minimum (overworld = 0; Nether subclass may override)
for (int var2 = 0; var2 <= 15; var2++) {
    float var3 = 1.0F - (float)var2 / 15.0F;
    f[var2] = (1.0F - var3) / (var3 * 3.0F + 1.0F) * (1.0F - var1) + var1;
}
```

Computed values (for overworld, `var1 = 0.0F`):

| Level | Brightness |
|---|---|
| 0 | 0.0000 |
| 1 | 0.0526 |
| 2 | 0.1111 |
| 3 | 0.1765 |
| 4 | 0.2500 |
| 5 | 0.3333 |
| 6 | 0.4286 |
| 7 | 0.5385 |
| 8 | 0.6667 |
| 9 | 0.8182 |
| 10 | 0.7273 … (continues non-linearly) |

The formula is a non-linear ramp: `f[15] = 1.0`, `f[0] = 0.0`. Used by `World.getBrightness()` and `World.c(x,y,z)` via `y.f[]`.

---

## 4. Methods

### registerWorld (final) — `a(ry world)`

```java
this.a = world;
b();   // createWorldChunkManager
a();   // calculateBrightnesTable
```

This is `final` — subclasses must not override it. Subclasses customise by overriding `b()` or `a()`.

### createWorldChunkManager (protected) — `b()`

```java
this.b = new vh(this.a);
```

Creates a default `WorldChunkManager` for the Overworld. Nether subclass overrides to create
a fixed single-biome manager.

### calculateBrightnessTable (protected) — `a()`

Fills `f[0..15]` as described in §3. Subclasses override `var1` (ambient minimum) for the
Nether (typically `0.1F`) to prevent complete darkness.

### createChunkLoader — `c()` → `ej`

```java
return new xj(a, a.t(), a.z().r());
```

Creates the default file-based `ChunkLoader` (`xj`) for this dimension. Arguments are
the world reference, world seed (`a.t()`), and save directory (`a.z().r()`).

### isValidSpawnBlock — `a(int x, int z)` → `boolean`

```java
int blockId = a.a(x, a.a(x, z));  // getTopBlock
return blockId == Block.u.bM;      // must be grass
```

Returns `true` if the topmost non-air block at (x, z) is grass (`yy.u`). Used during world
generation to find a valid spawn point.

### getSunAngle — `a(long worldTime, float partialTick)` → `float`

Computes the normalized celestial angle (0.0–1.0) for the current world time:

```java
int timeOfDay = (int)(worldTime % 24000L);
float t = (timeOfDay + partialTick) / 24000.0F - 0.25F;
if (t < 0.0F) t += 1.0F;
if (t > 1.0F) t -= 1.0F;
float s = 1.0F - (float)((Math.cos(t * Math.PI) + 1.0) / 2.0);
return t + (s - t) / 3.0F;
```

The returned angle is in the range [0, 1). `0.0` = sunrise, `0.25` ≈ noon, `0.5` = sunset,
`0.75` = midnight. The formula skews the speed so noon and midnight are shorter than the
transition periods (standard Minecraft day length parity).

### getMoonPhase — `b(long worldTime, float partialTick)` → `int`

```java
return (int)(worldTime / 24000L) % 8;
```

Returns moon phase 0–7 (cycles every 8 days).

### getSunriseColor — `a(float sunAngle, float rain)` → `float[]`

Returns a float[4] (RGBA) or `null`. Only non-null during sunrise/sunset window:

```java
float y = MathHelper.sin(sunAngle * PI * 2.0F);  // celestial Y
if (y >= -0.4F && y <= 0.4F) {                   // sunrise/sunset window
    float t = (y / 0.4F) * 0.5F + 0.5F;
    float alpha = 1.0F - (1.0F - MathHelper.sin(t * PI)) * 0.99F;
    alpha *= alpha;
    h[0] = t * 0.3F + 0.7F;   // R
    h[1] = t * t * 0.7F + 0.2F;  // G
    h[2] = t * t * 0.0F + 0.2F;  // B
    h[3] = alpha;               // A
    return h;  // reuses instance buffer!
}
return null;
```

**Quirk:** Returns the private `h[]` buffer by reference — callers must not store the reference
across ticks (the buffer is reused).

### getSkyColor (fog base) — `b(float sunAngle, float rain)` → `fb` (Vec3)

Computes the overworld sky/fog base colour at the given celestial angle, modulated by rain:

```java
float brightness = MathHelper.sin(sunAngle * PI * 2.0F) * 2.0F + 0.5F;
brightness = clamp(brightness, 0.0F, 1.0F);
float r = 0.7529412F;
float g = 0.84705883F;
float b = 1.0F;
// Apply rain desaturation if rain > 0
r *= brightness * 0.94F + 0.06F;
g *= brightness * 0.94F + 0.06F;
b *= brightness * 0.91F + 0.09F;
return Vec3.createVectorHelper(r, g, b);
```

Base overworld fog colour at noon (brightness=1.0): R≈0.753, G≈0.847, B=1.0.

### hasSkyLight — `d()` → `boolean`

Returns `true` for the Overworld. Nether subclass overrides to return `false`.
This is the conceptual meaning; the actual sky-light suppression is done via `e` field
(`isNether`), not this method.

### getWorldHeight — `e()` → `float`

```java
return (float)a.c;  // = 128.0F
```

### unknown — `f()` → `boolean`

Returns `true`. Purpose unknown from this class alone.

### getSpawnPoint — `g()` → `dh`

Returns `null` in base class. Nether/End subclasses may override to return a fixed spawn.

---

## 5. Static Factory

### `k.a(int dimensionId)` → `k`

```java
if (dimId == -1) return new aau();   // Nether
if (dimId == 0)  return new ix();    // Overworld
if (dimId == 1)  return new ol();    // End
return null;
```

---

## 6. Subclass Summary

| Class | DimId | Key differences |
|---|---|---|
| `ix` | 0 | Overworld — default; all base-class behaviours |
| `aau` | −1 | Nether — `e = true` (no sky-light); higher ambient brightness; single biome |
| `ol` | 1 | End — `e = true`?; void fog; different sky |

---

## 7. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | `getSunriseColor` returns a reference to the reused private `h[4]` buffer. Two successive calls overwrite the same array. |
| 2 | `getSunAngle` uses `Math.cos` (double), not `MathHelper.cos`. The result is double-precision then cast to float. |
| 3 | The brightness formula `(1 - var3) / (var3 * 3 + 1)` is a Perceptual gamma-like curve, not linear. Must match exactly for lighting parity. |

---

## 8. Open Questions

1. **`aau`, `ix`, `ol` subclass details** — specifically: does `aau` override `a()` with
   `var1 = 0.1F`? Does it override `b()` with a single-biome WorldChunkManager? Does it
   set `e = true` in a constructor or field initializer?

2. **`vh` (WorldChunkManager)** — constructor `new vh(world)`, method `a(x, z)` for
   temperature (used in World.canSnowAt), method `a(x, y, z)` for precipitation height.
   Spec pending.

3. **`g` field** — int, default 0. May be the dimension ID stored on the provider itself.

---

*Spec written by Analyst AI from `k.java` (120 lines). No C# implementation consulted.*
