# AxisAlignedBB Spec
Source class: `c.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`AxisAlignedBB` (AABB) is the sole bounding-box type for all block and entity collision,
physics sweep, and ray intersection in the engine. It stores two corners of a box aligned
to the world axes as six `double` fields. The class maintains a **static object pool** to
avoid per-frame heap allocation. The API mixes in-place mutation (one method only) with
pooled-return semantics (all geometry operations). Callers must respect the pool lifecycle.

---

## 2. Fields

### Instance fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `double` | minX — west-most X coordinate of the box |
| `b` | `double` | minY — bottom Y coordinate |
| `c` | `double` | minZ — north-most Z coordinate |
| `d` | `double` | maxX — east-most X coordinate |
| `e` | `double` | maxY — top Y coordinate |
| `f` | `double` | maxZ — south-most Z coordinate |

Field order: (minX, minY, minZ, maxX, maxY, maxZ).

### Static (pool) fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `g` | `List` (of `c`) | Object pool — a flat ArrayList that grows monotonically. Never shrinks except via `a()` clear. |
| `h` | `int` | Pool cursor — index of the next slot to hand out. Always in [0, `g.size()`]. |

---

## 3. Constants & Magic Numbers

None. All numeric literals in this class are `0.0`, `0`, or `-1` (the face-ID sentinel),
which are self-explanatory.

---

## 4. Object Pool — Lifecycle

The pool is **globally shared** across all AABB operations within a tick/frame.

### clearPool — static `a()` → void

1. Call `g.clear()` — discards all pooled instances (Java GC reclaims them).
2. Set `h = 0`.

Called at scene/world transitions to allow the pool to shrink.

### resetPool — static `b()` → void

1. Set `h = 0` only; leave `g` untouched.

Called at the start of each tick to make all existing pool slots available for reuse.
No allocations occur if the pool already has enough slots from previous ticks.

### getFromPool — static `b(double, double, double, double, double, double)` → `c`

Parameters: (minX, minY, minZ, maxX, maxY, maxZ).

1. If `h >= g.size()`:  
   a. Create a new instance: `a(0.0, 0.0, 0.0, 0.0, 0.0, 0.0)` (calls the non-pooled factory).  
   b. Append to `g`.
2. Retrieve `g.get(h)`.
3. Increment `h`.
4. Call the instance setter `c(minX, minY, minZ, maxX, maxY, maxZ)` on the retrieved instance
   to overwrite its fields.
5. Return the instance.

**Critical:** any pooled instance returned by this method is only valid until `b()` is
called again and the same pool slot is re-handed out. Callers must not hold pooled
references across pool resets. Only use the non-pooled factory `a(6 doubles)` when the
AABB must outlive a single tick.

---

## 5. Constructors & Factories

### Non-pooled factory — static `a(double var0, double var2, double var4, double var6, double var8, double var10)` → `c`

Creates and returns a **new, heap-allocated** AABB that is NOT in the pool. Safe to hold
across tick boundaries.

1. Call `new c(var0, var2, var4, var6, var8, var10)`.
2. Return the new instance.

### Private constructor — `c(double var1, double var3, double var5, double var7, double var9, double var11)`

Sets `a=var1, b=var3, c=var5, d=var7, e=var9, f=var11`. Only callable from within this
class (via the static factories).

---

## 6. Methods — Detailed Logic

Method names in this class are highly overloaded. Each method's signature (parameter types)
is the disambiguation key. Return types are always stated explicitly.

---

### set — instance `c(double var1, double var3, double var5, double var7, double var9, double var11)` → `c`

**Purpose:** Reset all 6 fields in-place. Used by the pool to reconfigure a recycled instance.

1. Set `a=var1, b=var3, c=var5, d=var7, e=var9, f=var11`.
2. Return `this`.

---

### addCoord — instance `a(double var1, double var3, double var5)` → `c`

**Purpose:** Produce a box that fully contains both the current box and its displaced
copy. Used for motion sweep — the box is enlarged along the movement direction only.

Parameters: dx=var1, dy=var3, dz=var5 (signed displacements).

Step-by-step:
1. Start: newMinX=this.a, newMinY=this.b, newMinZ=this.c, newMaxX=this.d, newMaxY=this.e, newMaxZ=this.f
2. If var1 < 0.0: newMinX += var1 (extend min in negative X direction)
3. If var1 > 0.0: newMaxX += var1 (extend max in positive X direction)
4. If var3 < 0.0: newMinY += var3
5. If var3 > 0.0: newMaxY += var3
6. If var5 < 0.0: newMinZ += var5
7. If var5 > 0.0: newMaxZ += var5
8. Return **pooled** `b(newMinX, newMinY, newMinZ, newMaxX, newMaxY, newMaxZ)`

When var1 = 0.0 exactly, neither branch fires — the X extent is unchanged. Same for the
other axes. Zero displacement → no expansion on that axis.

---

### expand — instance `b(double var1, double var3, double var5)` → `c`

**Purpose:** Symmetric inflation — grow the box by a fixed amount on all sides.

Step-by-step:
1. Return **pooled** `b(a - var1, b - var3, c - var5, d + var1, e + var3, f + var5)`

Passing negative values contracts the box. There is no guard preventing min > max.

---

### offset (new instance) — instance `c(double var1, double var3, double var5)` → `c`

**Purpose:** Translate the box by a vector, returning a new (pooled) instance.

Step-by-step:
1. Return **pooled** `b(a + var1, b + var3, c + var5, d + var1, e + var3, f + var5)`

Does NOT modify `this`.

---

### offset (in-place) — instance `d(double var1, double var3, double var5)` → `c`

**Purpose:** Translate the box **in-place**, mutating `this` and returning `this`.

Step-by-step:
1. `a += var1; b += var3; c += var5`
2. `d += var1; e += var3; f += var5`
3. Return `this`

**Critical:** this is the **only** method in the class that mutates `this`. It also
returns `this`, not a new instance. Calling code often chains it but both the original
and any chained reference point to the same now-modified object.

---

### contract — instance `e(double var1, double var3, double var5)` → `c`

**Purpose:** Symmetric contraction — shrink the box by a fixed amount on all sides
(inverse of `expand`).

Step-by-step:
1. Return **pooled** `b(a + var1, b + var3, c + var5, d - var1, e - var3, f - var5)`

---

### copy — instance `d()` → `c`

**Purpose:** Return a pooled copy of this box with identical coordinates.

Step-by-step:
1. Return **pooled** `b(a, b, c, d, e, f)`

---

### calculateXOffset — instance `a(c var1, double var2)` → `double`

**Purpose:** Given `var1` moving by `var2` along the X axis, return the maximum
movement it can actually make before hitting `this` box (sweep collision, X axis).

Parameters:
- `var1` — the moving AABB
- `var2` — proposed X delta (positive = +X, negative = −X)

Step-by-step:
1. **Y-axis guard:** if `var1.e <= this.b` OR `var1.b >= this.e`, return `var2` unchanged
   (boxes are not overlapping or touching on Y; no collision possible).
2. **Z-axis guard:** if `var1.f <= this.c` OR `var1.c >= this.f`, return `var2` unchanged
   (no overlap on Z).
3. If `var2 > 0.0` AND `var1.d <= this.a`:
   - gap = `this.a - var1.d` (distance from var1's east face to this box's west face)
   - if `gap < var2`: set `var2 = gap`
4. If `var2 < 0.0` AND `var1.a >= this.d`:
   - gap = `this.d - var1.a` (distance from var1's west face to this box's east face; negative)
   - if `gap > var2`: set `var2 = gap`
5. Return `var2`

The guard conditions use strict inequality on one side (`<=`, `>=`) and strict on the
other (`<=`, `>=`) — see quirks section.

---

### calculateYOffset — instance `b(c var1, double var2)` → `double`

**Purpose:** Sweep collision along Y axis.

Step-by-step:
1. **X-axis guard:** if `var1.d <= this.a` OR `var1.a >= this.d`, return `var2`
2. **Z-axis guard:** if `var1.f <= this.c` OR `var1.c >= this.f`, return `var2`
3. If `var2 > 0.0` AND `var1.e <= this.b`:
   - gap = `this.b - var1.e`
   - if `gap < var2`: set `var2 = gap`
4. If `var2 < 0.0` AND `var1.b >= this.e`:
   - gap = `this.e - var1.b`
   - if `gap > var2`: set `var2 = gap`
5. Return `var2`

---

### calculateZOffset — instance `c(c var1, double var2)` → `double`

**Purpose:** Sweep collision along Z axis.

Step-by-step:
1. **X-axis guard:** if `var1.d <= this.a` OR `var1.a >= this.d`, return `var2`
2. **Y-axis guard:** if `var1.e <= this.b` OR `var1.b >= this.e`, return `var2`
3. If `var2 > 0.0` AND `var1.f <= this.c`:
   - gap = `this.c - var1.f`
   - if `gap < var2`: set `var2 = gap`
4. If `var2 < 0.0` AND `var1.c >= this.f`:
   - gap = `this.f - var1.c`
   - if `gap > var2`: set `var2 = gap`
5. Return `var2`

---

### intersects — instance `a(c var1)` → `boolean`

**Purpose:** Test whether two AABBs overlap (have any common interior volume).

Step-by-step:
1. If `var1.d <= this.a` OR `var1.a >= this.d`: return `false`
2. If `var1.e <= this.b` OR `var1.b >= this.e`: return `false`
3. If `var1.f <= this.c` OR `var1.c >= this.f`: return `false`
4. Return `true`

Interval type: **open** — two boxes that share exactly one face (touching) are **not**
considered to intersect. Both `<=` and `>=` are used, so no overlap at all on an axis
triggers the early exit.

---

### isVecInside — instance `a(fb var1)` → `boolean`

**Purpose:** Test whether a point (`fb` = Vec3) lies strictly inside the box.

Parameters: `var1` is a Vec3 where `var1.a` = X, `var1.b` = Y, `var1.c` = Z.

Step-by-step:
1. If `var1.a <= this.a` OR `var1.a >= this.d`: return `false`
2. If `var1.b <= this.b` OR `var1.b >= this.e`: return `false`
3. If `var1.c <= this.c` OR `var1.c >= this.f`: return `false`
4. Return `true`

Interval type: **strictly open** — a point exactly on a face is NOT inside.

---

### averageEdgeLength — instance `c()` → `double`

**Purpose:** Returns the arithmetic mean of the three side lengths. Used as a size
metric (e.g. for XP orb detection radius).

Step-by-step:
1. Return `((d - a) + (e - b) + (f - c)) / 3.0`

---

### setBB — instance `b(c var1)` → `void`

**Purpose:** Copy all 6 coordinates from another AABB into `this` (in-place overwrite).

Step-by-step:
1. `a = var1.a; b = var1.b; c = var1.c; d = var1.d; e = var1.e; f = var1.f`

Returns void. Does not use the pool. Mutates `this`.

---

### rayTrace — instance `a(fb var1, fb var2)` → `gv` (or `null`)

**Purpose:** Find the point (and face) where a line segment [var1, var2] first intersects
the surface of this box.

Parameters:
- `var1` — ray start point (Vec3; fields: a=X, b=Y, c=Z)
- `var2` — ray end point (Vec3)

The method relies on Vec3's plane-intersection helpers (see Open Questions):
- `var1.a(var2, planeX)` — returns the Vec3 on segment [var1,var2] at X=planeX, or null
- `var1.b(var2, planeY)` — returns the Vec3 at Y=planeY, or null
- `var1.c(var2, planeZ)` — returns the Vec3 at Z=planeZ, or null
- `var1.e(fb target)` — returns the squared (or Euclidean) distance from var1 to target

Step-by-step:
1. Compute six face-plane intersection candidates:
   - `var3 = var1.a(var2, this.a)` — intersection with X = minX plane
   - `var4 = var1.a(var2, this.d)` — intersection with X = maxX plane
   - `var5 = var1.b(var2, this.b)` — intersection with Y = minY plane
   - `var6 = var1.b(var2, this.e)` — intersection with Y = maxY plane
   - `var7 = var1.c(var2, this.c)` — intersection with Z = minZ plane
   - `var8 = var1.c(var2, this.f)` — intersection with Z = maxZ plane

2. Validate each candidate with the private helpers (closed-interval bounds check).
   If validation fails, set the candidate to null:
   - `var3` valid if `b(var3)` — point's Y in [minY,maxY] AND Z in [minZ,maxZ]
   - `var4` valid if `b(var4)` — same YZ check
   - `var5` valid if `c(var5)` — point's X in [minX,maxX] AND Z in [minZ,maxZ]
   - `var6` valid if `c(var6)` — same XZ check
   - `var7` valid if `d(var7)` — point's X in [minX,maxX] AND Y in [minY,maxY]
   - `var8` valid if `d(var8)` — same XY check

3. Among the non-null candidates, find `var9` = the one closest to `var1`
   (lowest `var1.e(candidate)` value). Comparisons only update `var9` when strictly less
   than the current best, so the first valid face in the enumeration order wins ties:
   order is var3, var4, var5, var6, var7, var8.

4. If no valid candidate: return `null`.

5. Assign face ID `var10` (byte) based on which candidate is `var9`:
   - `var3` (minX face) → `var10 = 4`
   - `var4` (maxX face) → `var10 = 5`
   - `var5` (minY face) → `var10 = 0`
   - `var6` (maxY face) → `var10 = 1`
   - `var7` (minZ face) → `var10 = 2`
   - `var8` (maxZ face) → `var10 = 3`
   
   Face ID assignment is sequential — if `var9` matches multiple (identity comparison),
   the last matching assignment wins. In practice this does not occur since each candidate
   is a distinct object reference.

6. Return `new gv(0, 0, 0, var10, var9)`
   where `gv` = `MovingObjectPosition`, constructed with block coords (0,0,0), face ID,
   and hit point.

---

### Private validation helpers (used only by rayTrace)

All use **closed** intervals (≤ / ≥), unlike `isVecInside` which uses open intervals.

#### `b(fb var1)` — X-face YZ bounds check
Returns false if var1 is null.  
Returns `var1.b >= this.b && var1.b <= this.e && var1.c >= this.c && var1.c <= this.f`

#### `c(fb var1)` — Y-face XZ bounds check
Returns false if var1 is null.  
Returns `var1.a >= this.a && var1.a <= this.d && var1.c >= this.c && var1.c <= this.f`

#### `d(fb var1)` — Z-face XY bounds check
Returns false if var1 is null.  
Returns `var1.a >= this.a && var1.a <= this.d && var1.b >= this.b && var1.b <= this.e`

---

### toString — `toString()` → `String`

Returns: `"box[" + a + ", " + b + ", " + c + " -> " + d + ", " + e + ", " + f + "]"`

Example: `box[0.0, 0.0, 0.0 -> 1.0, 1.0, 1.0]`

---

## 7. Bitwise & Data Layouts

No bitwise operations. All fields are `double`; all arithmetic is floating-point.

---

## 8. Tick Behaviour

The AABB class itself has no tick entry point. The pool's `b()` reset is expected to be
called once per tick (or per physics pass) by the engine before any sweep or collision
query. The `a()` full clear is for world/level transitions.

---

## 9. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | `intersects`, `calculateXOffset/Y/Z` | Touching boxes (sharing an exact face) are **not** overlapping — `<=`/`>=` guards exit early. Two blocks placed adjacent never register as penetrating each other. This is intentional and must be exact. | **Yes** |
| 2 | `isVecInside` vs private validators | `isVecInside` uses strict open intervals (`<` / `>`); the three private ray-face validators use closed intervals (`<=` / `>=`). A point exactly on a face is "not inside" yet IS on the face for ray-trace purposes. | **Yes** |
| 3 | `calculateXOffset` guard symmetry | The guard checks `var1.e <= this.b` (<=) and `var1.b >= this.e` (>=). Both use non-strict. This means a box whose top face exactly equals another box's bottom face is treated as non-overlapping in Y, so X-movement is unrestricted. | **Yes** |
| 4 | `d(double,double,double)` in-place | Only this offset variant mutates `this`. All other geometry methods return a new (pooled) instance. Confusing the two causes silent state corruption. The naming is `d` (the fourth overload of single-letter methods). | Note for Coder |
| 5 | Pool is globally shared static state | All AABB operations in a tick draw from the same list. A missed `b()` reset causes the pool to grow unboundedly; a `b()` called too early invalidates live references. | **Yes** |
| 6 | `addCoord` with exact zero | `var1 == 0.0` triggers neither branch. The result box is identical to the original on that axis. IEEE 754 negative zero (`-0.0`) evaluates `< 0.0` as false and `> 0.0` as false — also triggers no expansion. | **Yes** |

---

## 10. Open Questions

1. **`fb` (Vec3) spec needed.** The ray-trace method calls three plane-intersection methods
   (`a(fb, double)`, `b(fb, double)`, `c(fb, double)`) and a distance method (`e(fb)`) on
   `fb`. Their exact semantics (return null vs return endpoint outside segment? distance
   squared vs Euclidean?) are required to implement `rayTrace` correctly.
   → Request spec for `fb` = `Vec3`.

2. **`gv` (MovingObjectPosition) spec needed.** The ray-trace return type `gv` is
   constructed as `new gv(0, 0, 0, faceId, hitPoint)`. Its field layout (especially which
   constructor parameter is the face ID and which is the hit Vec3) must be confirmed.
   → Request spec for `gv` = `MovingObjectPosition`.

3. **Pool reset cadence.** The source does not show where `b()` / `a()` are called.
   Likely the engine's tick loop or the World class calls `b()` before each physics pass.
   Confirm in `ry` (World) spec.

---

*Spec written by Analyst AI from `c.java` (334 lines, decompiled). No C# implementation consulted.*
