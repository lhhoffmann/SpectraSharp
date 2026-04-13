# Material Spec
Source class: `p.java`
Superclass: none (implicit `java.lang.Object`)

> **Mapping correction:** `classes.md` previously listed `wu` as `Material`. This is wrong.
> `wu` is `StepSound`. The Material class is `p` (not listed in classes.md before this spec).
> Evidence: `p` has boolean methods for liquid/solid/replaceable/mobility — all Material
> semantics. `p` instances are created with `aav` (MapColor) arguments. `wu` instances are
> created with sound strings and volume/pitch floats.

---

## 1. Purpose

`Material` defines the physical properties of a block's substance: whether it is liquid,
solid, flammable, replaceable, passable, and how pistons treat it. Each block stores a
`Material` reference in its `bZ` field (set in the Block constructor). Material also carries
a `MapColor` reference (`aav`) used for the in-game map display.

---

## 2. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `E` | `aav` (final) | set in constructor | Map color for this material |
| `F` | `boolean` | `false` | isBurnable/flammable (set by `f()`) |
| `G` | `boolean` | `false` | isReplaceable — can be replaced by placing (set by `h()`) |
| `H` | `boolean` | `false` | noCollision / isPassable (set by private `o()`) |
| `I` | `boolean` | `true` | blocksLight / isOpaque (set to `false` by `e()`) |
| `J` | `int` | `0` | Mobility / piston behaviour: 0=pushable, 1=immovable, 2=push-destroys |

---

## 3. Static Material Instances

All defined as `public static final p` fields on the `p` class itself.

| Field (obf) | Subclass | MapColor | Modifiers | Inferred human name |
|---|---|---|---|---|
| `a` | `br` | `aav.b` (black) | — | unknown (`br` subclass) |
| `b` | base `p` | `aav.c` (green) | — | `materialGrass` |
| `c` | base `p` | `aav.l` (tan) | — | `materialWood` / `materialGround` |
| `d` | base `p` | `aav.o` (brown-grey) | `f()` | flammable — `materialPlants`? |
| `e` | base `p` | `aav.m` (grey) | `e()` | I=false (transparent) — `materialRock` |
| `f` | base `p` | `aav.h` (tan-green) | `e()` | I=false — another rock/stone variant |
| `g` | `sn` | `aav.n` (dark blue) | `m()` | liquid, immovable — `materialWater` |
| `h` | `sn` | `aav.f` (red) | `m()` | liquid, immovable — `materialLava` |
| `i` | base `p` | `aav.i` (dark green) | `f()`, `o()`, `m()` | flammable, passable, immovable — `materialLeaves`? |
| `j` | `mw` | `aav.i` (dark green) | `m()` | `mw` subclass, immovable |
| `k` | `mw` | `aav.i` (dark green) | `f()`, `m()`, `h()` | flammable, immovable, replaceable — `materialVine`? |
| `l` | base `p` | `aav.e` (brown-green) | — | — |
| `m` | base `p` | `aav.e` (brown-green) | `f()` | flammable |
| `n` | `br` | `aav.b` (black) | `m()` | `br` subclass, immovable |
| `o` | base `p` | `aav.d` (light yellow) | — | — |
| `p` (field) | `mw` | `aav.b` (black) | `m()` | `mw` subclass, immovable |
| `q` | base `p` | `aav.b` (black) | `o()` | passable — `materialAir`? |
| `r` | base `p` | `aav.f` (red) | `f()`, `o()` | flammable, passable |
| `s` | base `p` | `aav.i` (dark green) | `m()` | immovable |
| `t` | base `p` | `aav.g` (cyan-blue) | `o()` | passable |
| `u` | `mw` | `aav.j` (white) | `h()`, `o()`, `e()`, `m()` | replaceable, passable, transparent, immovable — `materialSnow`? |
| `v` | base `p` | `aav.j` (white) | `e()` | transparent — `materialIce`? |
| `w` | base `p` | `aav.i` (dark green) | `o()`, `m()` | passable, immovable |
| `x` | base `p` | `aav.k` (blue-grey) | — | — |
| `y` | base `p` | `aav.i` (dark green) | `m()` | immovable |
| `z` | base `p` | `aav.i` (dark green) | `m()` | immovable |
| `A` | `bk` | `aav.b` (black) | `n()` | J=2, push-destroys — `materialPortal`? |
| `B` | base `p` | `aav.b` (black) | `m()` | immovable |
| `C` | `tx` | `aav.e` (brown-green) | `e()`, `m()` | `tx` subclass, transparent, immovable |
| `D` | base `p` | `aav.m` (grey) | `n()` | J=2, push-destroys |

---

## 4. Constructor

### `p(aav var1)`

1. `this.E = var1`

---

## 5. Builder Methods (all `protected` or `private`, all return `this`)

| Method (obf) | Effect | Access |
|---|---|---|
| `e()` | `I = false` (material does not block light) | `protected` |
| `f()` | `F = true` (flammable) | `protected` |
| `h()` | `G = true` (replaceable) | `public` |
| `m()` | `J = 1` (immovable by pistons) | `protected` |
| `n()` | `J = 2` (push-destroys: piston breaks it) | `protected` |
| `o()` | `H = true` (no collision / passable) | `private` |

---

## 6. Methods — Detailed Logic

### isLiquid — `a()` → `boolean`

Default: `return false`  
Overridden by `sn` (MaterialLiquid) to return `true`.  
Called by water/lava movement logic.

---

### isSolid — `b()` → `boolean`

Default: `return true`  
Used in `Block.isNormalCube` (via `kq.e(x,y,z).b()`).

---

### blocksMovement — `c()` → `boolean`

Default: `return true`  
Used in `Block` constructor: `p[blockId] = !var2.c()`. Since this returns `true`, the
`p[]` (canPassThrough) array is `false` for all blocks by default. Only `p[0]` (air) is
set to `true` explicitly in the static initializer.

---

### unknown — `d()` → `boolean`

Default: `return true`  
Called by `j()`: `return !H && d()`.

---

### isBurnable — `g()` → `boolean`

Returns `this.F`.

---

### isReplaceable — `i()` → `boolean`

Returns `this.G`.  
Called in `Block.canReplace`: `k[blockId].bZ.i()`. If `true`, the block can be replaced
by placing another block on top of it (tall grass, water, snow layer, etc.).

---

### canBurn (canBePushed?) — `j()` → `boolean`

Step-by-step:
1. If `H == true`: return `false`
2. Else return `d()` (which returns `true` by default)

The semantics of this method are uncertain. `H` being true (passable, like air/water)
prevents whatever this method grants. The default result is `true`.

---

### blocksLight — `k()` → `boolean`

Returns `this.I`.  
Default `true` (set to `false` by builder `e()` for transparent materials like ice, glass).

---

### getMobility — `l()` → `int`

Returns `this.J`.

| Value | Meaning |
|---|---|
| 0 | Normal — piston can push and pull |
| 1 | Immovable — piston cannot push or pull |
| 2 | Push-destroys — piston breaks the block rather than moving it |

Called in `Block.getLightOpacity` (`Block.i()` method): `this.bZ.l()`. Returns 0, 1, or 2.

---

## 7. Known Subclasses of `p`

All are package-private and defined in separate files. Only their observable differences
from the base class are noted here.

| Class (obf) | Inferred human name | Known overrides |
|---|---|---|
| `sn` | `MaterialLiquid` | Overrides `a()` to return `true` (isLiquid = true) |
| `mw` | unknown Material subclass | Unknown overrides |
| `br` | unknown Material subclass | Unknown overrides |
| `bk` | unknown Material subclass | Unknown overrides |
| `tx` | unknown Material subclass | Unknown overrides |

---

## 8. MapColor (`aav`) — Summary

`aav` (= MapColor) is used as the `E` field of Material. It has:
- Static array `a[16]` — 16 colour slots
- 14 defined instances (`b` through `o`), at indices 0–13
- Each instance has `q` = index (int) and `p` = RGB colour (int, 24-bit)

| Instance | Index (`q`) | RGB (`p`) | Approximate colour |
|---|---|---|---|
| `b` | 0 | 0 (black) | Black / transparent |
| `c` | 1 | 8368696 | Dark grass green |
| `d` | 2 | 16247203 | Sand yellow |
| `e` | 3 | 10987431 | Dirt brown |
| `f` | 4 | 16711680 | Red (lava / redstone) |
| `g` | 5 | 10526975 | Ice blue |
| `h` | 6 | 10987431 | Dirt brown (duplicate of e) |
| `i` | 7 | 31744 | Leaf green |
| `j` | 8 | 16777215 | White (snow/ice) |
| `k` | 9 | 10791096 | Blue-grey |
| `l` | 10 | 12020271 | Wood tan |
| `m` | 11 | 7368816 | Stone grey |
| `n` | 12 | 4210943 | Water blue |
| `o` | 13 | 6837042 | Gravel brown-grey |

---

## 9. Bitwise & Data Layouts

No bitwise operations.

---

## 10. Tick Behaviour

No tick entry point.

---

## 11. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | `l()` used for light opacity | `Block.i()` returns `this.bZ.l()` — the Material mobility flag (0, 1, 2) is returned as the block's light opacity override. This means some immovable blocks report light opacity of 1, and push-destroy blocks report 2. This double-use of the mobility field is intentional. | **Yes** |
| 2 | `c()` always `true` | `blocksMovement` always returns `true` in the base class and no visible subclass overrides it to `false`. The `p[]` (passThrough) array therefore has `true` only for air (slot 0, set explicitly). | **Yes** |

---

## 12. Open Questions

1. **`sn`, `mw`, `br`, `bk`, `tx` specs needed** if any of their overridden methods are
   called by Block physics. `sn.a()` = isLiquid = true is the only confirmed override;
   the others are unconfirmed.

---

*Spec written by Analyst AI from `p.java` (108 lines), `aav.java` (25 lines). No C# implementation consulted.*
