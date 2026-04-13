# Vec3 Spec
Source class: `fb.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`Vec3` is a three-component double-precision vector used for positions, directions, and
ray endpoints throughout the engine. Like `AxisAlignedBB`, it maintains a **static
object pool** (identical pool pattern) to avoid per-frame heap allocation. It provides
geometric operations (subtract, dot, cross, normalize, length, distance) and three
segment-plane intersection methods that are called directly by `AxisAlignedBB.rayTrace`.
Two rotation methods mutate the vector in-place.

---

## 2. Fields

### Instance fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `double` | X component |
| `b` | `double` | Y component |
| `c` | `double` | Z component |

### Static (pool) fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `d` | `List` (of `fb`) | Object pool — ArrayList that grows monotonically |
| `e` | `int` | Pool cursor — index of next slot to hand out |

---

## 3. Object Pool — Lifecycle

Identical pattern to `AxisAlignedBB`. The two pools are independent (separate lists).

### clearPool — static `a()` → void

1. Call `d.clear()`
2. Set `e = 0`

### resetPool — static `b()` → void

1. Set `e = 0` only; leave `d` intact

### getFromPool — static `b(double var0, double var2, double var4)` → `fb`

Parameters: (X, Y, Z).

1. If `e >= d.size()`: call `a(0.0, 0.0, 0.0)` and append to `d`
2. Retrieve `d.get(e)` and increment `e`
3. Call the private `e(X, Y, Z)` setter on the retrieved instance
4. Return the instance

**The pool setter `e(...)` does NOT normalize negative zero.** Only the heap constructor
does (see quirks). Pooled instances may therefore contain `-0.0` components if negative
zero was passed in.

### Non-pooled factory — static `a(double var0, double var2, double var4)` → `fb`

Creates a heap-allocated instance via the private constructor. Safe to hold across
tick/pool-reset boundaries.

---

## 4. Constructor

### Private constructor `fb(double var1, double var3, double var5)`

**Negative-zero normalisation (constructor only):**
1. If `var1 == -0.0`: set `var1 = 0.0`
2. If `var3 == -0.0`: set `var3 = 0.0`
3. If `var5 == -0.0`: set `var5 = 0.0`
4. Set `a = var1, b = var3, c = var5`

The comparison `var1 == -0.0` is a Java `double` comparison. IEEE 754 defines `-0.0 == 0.0`
as `true`, so this check uses a different mechanism: in Java bytecode, comparing a double
to the literal `-0.0` with `==` evaluates to `true` only when the bit pattern is exactly
negative zero (`0x8000000000000000`). This is the standard Java behaviour and must be
matched exactly.

### Private pool setter — `e(double var1, double var3, double var5)` → `fb`

1. Set `a = var1, b = var3, c = var5`
2. Return `this`

No normalisation. No guard. Pure assignment.

---

## 5. Methods — Detailed Logic

---

### subtract — instance `a(fb var1)` → `fb`

**Purpose:** Returns the pooled vector from `this` to `var1` (i.e. `var1 minus this`).

Step-by-step:
1. Return pooled `b(var1.a - this.a, var1.b - this.b, var1.c - this.c)`

The result points **from** `this` **toward** `var1`. This is NOT `this - var1`.

---

### normalize — instance `c()` → `fb`

**Purpose:** Returns the unit vector in the same direction as `this`.

Step-by-step:
1. Compute squared length: `sq = this.a * this.a + this.b * this.b + this.c * this.c`
   (double arithmetic)
2. Compute length: `var1 = me.a(sq)` — calls `MathHelper.sqrt_double(double)` which
   returns a `float`
3. If `var1 < 1.0E-4` (float vs float comparison, threshold is `1.0E-4F`):
   return pooled `b(0.0, 0.0, 0.0)`
4. Else return pooled `b(this.a / (double)var1, this.b / (double)var1, this.c / (double)var1)`

**Precision note:** `var1` is a `float` (MathHelper returns float). The division
`this.a / (double)var1` widens `var1` to double before dividing. The normalization
therefore inherits the ~7 decimal digits of precision of the float sqrt.

---

### dot — instance `b(fb var1)` → `double`

**Purpose:** Dot product of `this` and `var1`.

Step-by-step:
1. Return `this.a * var1.a + this.b * var1.b + this.c * var1.c`

Pure double arithmetic.

---

### cross — instance `c(fb var1)` → `fb`

**Purpose:** Cross product of `this` × `var1`, returned as a pooled Vec3.

Step-by-step:
1. Return pooled `b(b*var1.c - c*var1.b, c*var1.a - a*var1.c, a*var1.b - b*var1.a)`

Standard right-hand rule: `this × var1`.

---

### add — instance `c(double var1, double var3, double var5)` → `fb`

**Purpose:** Returns `this + (var1, var3, var5)` as a pooled Vec3.

Step-by-step:
1. Return pooled `b(a + var1, b + var3, c + var5)`

---

### distanceTo — instance `d(fb var1)` → `double`

**Purpose:** Euclidean distance from `this` to `var1`.

Step-by-step:
1. `dx = var1.a - this.a; dy = var1.b - this.b; dz = var1.c - this.c`
2. Compute `me.a(dx*dx + dy*dy + dz*dz)` — `sqrt_double(double)` returns `float`
3. Return `(double)result` (widened to double)

The result is float-precision despite the double inputs.

---

### squaredDistanceTo — instance `e(fb var1)` → `double`

**Purpose:** Squared Euclidean distance from `this` to `var1`. No sqrt. Used for
distance comparisons where exact distance is not needed (avoids sqrt cost).

Step-by-step:
1. `dx = var1.a - this.a; dy = var1.b - this.b; dz = var1.c - this.c`
2. Return `dx*dx + dy*dy + dz*dz` (pure double, no sqrt)

This is the method called as `var1.e(var9)` in `AxisAlignedBB.rayTrace` to find the
closest hit point. It is squared distance, NOT Euclidean distance.

---

### squaredDistanceTo (coords) — instance `d(double var1, double var3, double var5)` → `double`

**Purpose:** Squared distance from `this` to the point (var1, var3, var5).

Step-by-step:
1. `dx = var1 - this.a; dy = var3 - this.b; dz = var5 - this.c`
2. Return `dx*dx + dy*dy + dz*dz`

---

### length — instance `d()` → `double`

**Purpose:** Magnitude of this vector.

Step-by-step:
1. Return `(double)me.a(a*a + b*b + c*c)` — float sqrt widened to double

Same float-precision result as `distanceTo`.

---

### getIntermediateWithXValue — instance `a(fb var1, double var2)` → `fb` or `null`

**Purpose:** Find the point on segment [this, var1] where X = var2. Returns null if
the segment does not cross that X plane, or if the segment is nearly parallel to the YZ plane.

Parameters: `var1` = end of segment, `var2` = target X value.

Step-by-step:
1. Compute deltas: `dx = var1.a - this.a; dy = var1.b - this.b; dz = var1.c - this.c`
2. Guard: if `dx * dx < 1.0E-7F` → return `null`
   - `dx * dx` is a `double` product
   - `1.0E-7F` is a **float** literal (value ≈ 1.0000000116860974e-7 when widened to double)
   - Comparison: double < double (float widened)
   - This guard fires when the segment's X extent is negligible (nearly parallel to YZ plane)
3. Compute parametric t: `t = (var2 - this.a) / dx`
4. If `t < 0.0` OR `t > 1.0`: return `null` (intersection is outside the segment)
5. Return pooled `b(this.a + dx*t, this.b + dy*t, this.c + dz*t)`

---

### getIntermediateWithYValue — instance `b(fb var1, double var2)` → `fb` or `null`

**Purpose:** Find the point on segment [this, var1] where Y = var2.

Step-by-step:
1. `dx = var1.a - this.a; dy = var1.b - this.b; dz = var1.c - this.c`
2. Guard: if `dy * dy < 1.0E-7F` → return `null`
3. `t = (var2 - this.b) / dy`
4. If `t < 0.0` OR `t > 1.0`: return `null`
5. Return pooled `b(this.a + dx*t, this.b + dy*t, this.c + dz*t)`

---

### getIntermediateWithZValue — instance `c(fb var1, double var2)` → `fb` or `null`

**Purpose:** Find the point on segment [this, var1] where Z = var2.

Step-by-step:
1. `dx = var1.a - this.a; dy = var1.b - this.b; dz = var1.c - this.c`
2. Guard: if `dz * dz < 1.0E-7F` → return `null`
3. `t = (var2 - this.c) / dz`
4. If `t < 0.0` OR `t > 1.0`: return `null`
5. Return pooled `b(this.a + dx*t, this.b + dy*t, this.c + dz*t)`

All three `getIntermediate*` methods are identical in structure; only the axis component
changes.

---

### rotateAroundX — instance `a(float var1)` → void

**Purpose:** Rotate `this` vector around the X axis by angle `var1` (radians).
Mutates `this` in-place; returns void.

Step-by-step:
1. `cos = me.b(var1)` — `MathHelper.cos(var1)` (float)
2. `sin = me.a(var1)` — `MathHelper.sin(var1)` (float)
3. Store `newA = this.a` (unchanged)
4. Store `newB = this.b * (double)cos + this.c * (double)sin`
5. Store `newC = this.c * (double)cos - this.b * (double)sin`
6. Set `this.a = newA; this.b = newB; this.c = newC`

X coordinate is untouched. The rotation matrix is:
```
| 1    0     0   |   |a|
| 0   cos  sin   | × |b|
| 0  -sin  cos   |   |c|
```

Note: the sign convention here places `+sin` in the `b` row third column and `-sin` in
the `c` row second column. This corresponds to rotating the YZ plane clockwise when
viewed from the positive X direction.

---

### rotateAroundY — instance `b(float var1)` → void

**Purpose:** Rotate `this` vector around the Y axis by angle `var1` (radians).
Mutates `this` in-place; returns void.

Step-by-step:
1. `cos = me.b(var1)` (float); `sin = me.a(var1)` (float)
2. Store `newA = this.a * (double)cos + this.c * (double)sin`
3. Store `newB = this.b` (unchanged)
4. Store `newC = this.c * (double)cos - this.a * (double)sin`
5. Set `this.a = newA; this.b = newB; this.c = newC`

Y coordinate is untouched.

---

### toString — `toString()` → `String`

Returns: `"(" + this.a + ", " + this.b + ", " + this.c + ")"`

Example: `(1.0, 2.0, 3.0)`

---

## 6. Bitwise & Data Layouts

No bitwise operations.

---

## 7. Tick Behaviour

No tick entry point. `b()` pool-reset is expected to be called once per tick alongside
the AABB pool reset. Both pool resets must happen before any geometry that uses pooled
Vec3s or AABBs.

---

## 8. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | Private constructor | Normalises `-0.0` to `+0.0` on all axes. The pool setter `e(...)` does NOT. Heap-allocated Vec3s are always clean; pooled Vec3s may carry `-0.0`. | **Yes** |
| 2 | `getIntermediateWith*` guard | Threshold is the **float** literal `1.0E-7F`, compared against a **double** product. The effective threshold is `≈ 1.0000000116860974e-7` (not exactly `1e-7`). | **Yes** |
| 3 | `normalize` / `distanceTo` / `length` | Square root is computed as `float` (MathHelper.sqrt_double returns float). All distances and normalizations have float precision (~7 decimal digits) despite double-precision inputs. | **Yes** |
| 4 | `subtract` direction | `a(fb var1)` returns `var1 - this`, not `this - var1`. The method name "subtract" from the caller's perspective means "give me the vector pointing from me to target." | Note for Coder |
| 5 | `rotateAroundX/Y` | Both rotation methods return `void` and mutate `this`. They do NOT return a new (pooled) instance. Callers must copy before rotating if the original is needed. | **Yes** |
| 6 | `squaredDistanceTo` used in ray-trace | `AxisAlignedBB.rayTrace` calls `e(fb)` (squared distance) to find the closest face hit — it does NOT use Euclidean distance. Both give the same ordering for positive distances, so the result is correct, but the implementation must use squared distance. | **Yes** |

---

## 9. Open Questions

None. All methods are fully resolved from the source.

---

*Spec written by Analyst AI from `fb.java` (165 lines, decompiled). No C# implementation consulted.*
