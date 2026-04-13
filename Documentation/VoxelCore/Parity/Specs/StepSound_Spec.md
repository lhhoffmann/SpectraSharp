# StepSound Spec
Source class: `wu.java`
Superclass: none (implicit `java.lang.Object`)

> **Mapping correction:** `classes.md` previously listed `wu` as `Material`. This is wrong.
> `wu` is `StepSound`. The Material class is `p`. See Material_Spec.md for correction.
> Evidence: `wu.a()` returns `"step." + name`; subclasses override `a()` to return specific
> sound paths such as `"random.glass"` and `"step.gravel"`. `bj` and `aeg` extend `wu`.

---

## 1. Purpose

`StepSound` is an immutable descriptor for the set of sounds a block makes when walked on,
placed, or broken. Each block stores a `StepSound` reference in its `bX` field. The class
holds a base name string and two float multipliers (volume and pitch). Subclasses override
the place/break sound path while keeping the base name and floats.

---

## 2. Fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `String` (final) | Base sound name (e.g. `"stone"`, `"wood"`, `"gravel"`, `"grass"`, `"cloth"`) |
| `b` | `float` (final) | Volume multiplier (typically `1.0F`) |
| `c` | `float` (final) | Pitch multiplier (typically `1.0F` or `1.5F`) |

All three fields are set in the constructor and never changed.

---

## 3. Instances defined on Block (`yy`)

These are the `public static final wu` fields on Block. They are not on `wu` itself.

| Field on Block | Constructor args | Notes |
|---|---|---|
| `b` | `wu("stone", 1.0F, 1.0F)` | Stone step sound |
| `c` | `wu("wood", 1.0F, 1.0F)` | Wood step sound |
| `d` | `wu("gravel", 1.0F, 1.0F)` | Gravel step sound |
| `e` | `wu("grass", 1.0F, 1.0F)` | Grass step sound |
| `f` | `wu("stone", 1.0F, 1.5F)` | Stone step sound with higher pitch |
| `g` | `wu("stone", 1.0F, 1.5F)` | Same as `f` |
| `h` | `bj("stone", 1.0F, 1.0F)` | Glass/liquid break sound (`bj` subclass) |
| `i` | `wu("cloth", 1.0F, 1.0F)` | Cloth/wool step sound |
| `j` | `aeg("sand", 1.0F, 1.0F)` | Sand/gravel step sound (`aeg` subclass) |

---

## 4. Constructor

### `wu(String var1, float var2, float var3)`

1. `this.a = var1`
2. `this.b = var2`
3. `this.c = var3`

---

## 5. Methods

### getPlaceSound — `a()` → `String`

Default: returns `"step." + this.a`  
Examples: `"step.stone"`, `"step.wood"`, `"step.gravel"`, `"step.grass"`, `"step.cloth"`

Overridden by subclasses:
- `bj` (glass/liquid): returns `"random.glass"` — used for the glass/ice break sound
- `aeg` (sand): returns `"step.gravel"` — sand uses gravel break sound

---

### getVolume — `b()` → `float`

Returns `this.b` (volume multiplier).

---

### getPitch — `c()` → `float`

Returns `this.c` (pitch multiplier).

---

### getStepSound — `d()` → `String`

Default: returns `"step." + this.a`  
Neither `bj` nor `aeg` overrides `d()`. All step (walking) sounds follow the `"step." + name`
pattern regardless of subclass.

---

## 6. Subclasses

### `bj` — Glass/Liquid StepSound (`wu` subclass)

Source: `bj.java` (10 lines).

Overrides only `a()` to return `"random.glass"`.  
All other methods (`b()`, `c()`, `d()`) delegate to base class.  
Used for glass and ice blocks.

### `aeg` — Sand StepSound (`wu` subclass)

Source: `aeg.java` (10 lines).

Overrides only `a()` to return `"step.gravel"`.  
All other methods delegate to base class.  
Used for sand blocks (sand uses gravel step sound).

---

## 7. Bitwise & Data Layouts

None.

---

## 8. Tick Behaviour

No tick entry point. Purely passive data.

---

## 9. Known Quirks / Bugs to Preserve

| # | Location | Quirk | Must preserve? |
|---|---|---|---|
| 1 | `a()` vs `d()` | Both return `"step." + name` in the base class. `bj` overrides only `a()`, leaving `d()` still returning `"step.stone"`. This means glass has a different place/break sound (`"random.glass"`) but the same walk sound (`"step.stone"`) as stone. | **Yes** |
| 2 | `aeg.a()` | Sand's place/break sound is `"step.gravel"` (gravel), not `"step.sand"`. No `"step.sand"` sound exists. | **Yes** |

---

## 10. Open Questions

None. All methods fully resolved.

---

*Spec written by Analyst AI from `wu.java` (27 lines), `bj.java` (10 lines), `aeg.java` (10 lines). No C# implementation consulted.*
