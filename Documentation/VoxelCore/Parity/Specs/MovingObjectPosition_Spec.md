# MovingObjectPosition Spec
Source class: `gv.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`MovingObjectPosition` is a plain data container that holds the result of a ray-cast
or entity-intersection query. It describes either a **block face hit** or an **entity
hit**. The type of hit is recorded in an enum-like field (`bo`). The class has no
methods beyond constructors; all fields are public and read directly by callers.

---

## 2. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `bo` | set in constructor | Hit type: `bo.a` = block/tile hit, `bo.b` = entity hit |
| `b` | `int` | 0 | Block X coordinate (meaningful only for block hits) |
| `c` | `int` | 0 | Block Y coordinate (meaningful only for block hits) |
| `d` | `int` | 0 | Block Z coordinate (meaningful only for block hits) |
| `e` | `int` | 0 | Face ID / side hit (meaningful only for block hits; 0–5) |
| `f` | `fb` | set in constructor | Hit position as Vec3 — a **pooled copy** (see notes) |
| `g` | `ia` | null | The entity hit (meaningful only for entity hits) |

### Face ID values for field `e`

| Value | Face |
|---|---|
| 0 | −Y (bottom / minY) |
| 1 | +Y (top / maxY) |
| 2 | −Z (north / minZ) |
| 3 | +Z (south / maxZ) |
| 4 | −X (west / minX) |
| 5 | +X (east / maxX) |

These values are assigned by `AxisAlignedBB.rayTrace` (see `AxisAlignedBB_Spec.md`,
section 6, `rayTrace`). They are not defined inside `gv` itself.

---

## 3. Constants & Magic Numbers

None defined within this class. The face IDs 0–5 are assigned by the ray-cast caller.

---

## 4. Dependency — `bo` (EnumMovingObjectType)

`bo` is the hit-type discriminator. It exposes at minimum two constants:
- `bo.a` — TILE (block hit)
- `bo.b` — ENTITY (entity hit)

`bo` is referenced but not defined in this file. A separate spec for `bo` is required
to confirm whether it is a Java `enum` or a class with static constants.

---

## 5. Constructors

### Block-hit constructor — `gv(int var1, int var2, int var3, int var4, fb var5)`

Used when a ray hits a block face. Called from `AxisAlignedBB.rayTrace` as
`new gv(0, 0, 0, faceId, hitPoint)`.

Step-by-step:
1. Set `a = bo.a` (TILE type)
2. Set `b = var1` (blockX)
3. Set `c = var2` (blockY)
4. Set `d = var3` (blockZ)
5. Set `e = var4` (face ID)
6. Set `f = fb.b(var5.a, var5.b, var5.c)` — creates a **pooled copy** of `var5`

Field `g` is left at its Java default value: `null`.

**Note on the hit-position copy:** The constructor does not store `var5` directly. It
calls `fb.b(...)` to obtain a pooled Vec3 with the same coordinates. This means `f`
points into the Vec3 pool and will be invalidated when the pool is reset. Callers that
need the hit position beyond the current tick must copy it to a heap-allocated Vec3
using `fb.a(x, y, z)`.

**Note on block coordinates when called from AABB:** `AxisAlignedBB.rayTrace` always
passes `(0, 0, 0)` for the block coordinates. The world-level ray-cast code (in `World`
or block-breaking logic) is responsible for supplying real block coordinates when
constructing a `MovingObjectPosition` for a specific block.

### Entity-hit constructor — `gv(ia var1)`

Used when a ray hits an entity. `ia` = Entity base class.

Step-by-step:
1. Set `a = bo.b` (ENTITY type)
2. Set `g = var1` (the entity)
3. Set `f = fb.b(var1.s, var1.t, var1.u)` — pooled copy of the entity's position

Fields `b`, `c`, `d`, `e` are left at their Java default value: `0`.

Entity position fields: `var1.s` = X, `var1.t` = Y, `var1.u` = Z (confirmed from AABB
spec, which reads these same fields when constructing a MovingObjectPosition for entity hits).

---

## 6. Bitwise & Data Layouts

No bitwise operations.

---

## 7. Tick Behaviour

`MovingObjectPosition` is a passive data container with no tick logic. Instances created
during a tick are typically consumed immediately (within the same tick) or held by the
player for block-breaking state. Because `f` uses a pooled Vec3, any `gv` instance that
survives beyond the current pool cycle holds a stale Vec3 reference.

---

## 8. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | Block-hit constructor | Hit position `f` is a **pooled** Vec3 copy, not a stable heap allocation. Becomes invalid after the next `fb.b()` pool-reset. | **Yes** — do not change to a heap allocation |
| 2 | Entity-hit constructor | Block coordinate fields `b`, `c`, `d`, `e` are left as `0` (Java int default). Code that checks these fields for an entity hit receives zeros, not a meaningful position. Callers must check `a == bo.b` before reading block fields. | **Yes** |
| 3 | AABB ray-trace always passes (0,0,0) | When `AxisAlignedBB.rayTrace` creates a `gv`, the block coordinates are always `(0, 0, 0)`. The real-world block lookup is done at a higher level. | Note for Coder |

---

## 9. Open Questions

1. **`bo` spec needed.** Is `bo` a Java `enum` with an `ordinal()`, or a class with
   static `int`/`Object` constants? The distinction affects how the type check
   (`a == bo.a`) is implemented. → Request spec for `bo` = `EnumMovingObjectType`.

2. **`ia` (Entity) fields `s`, `t`, `u`.** Used in the entity-hit constructor as the
   entity's X/Y/Z position. These are confirmed as position doubles from other
   observations (AABB spec, `aad.java`), but the full Entity spec should confirm
   the field layout.

---

*Spec written by Analyst AI from `gv.java` (24 lines, decompiled). No C# implementation consulted.*
