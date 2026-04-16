# MathHelper Spec
Source class: `me.java`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`MathHelper` is a pure static utility class. It provides a sine/cosine lookup table
(built once at class-load time) plus a collection of numeric helper functions used
throughout the engine for physics, entity movement, and world generation. There are no
instances and no instance state. Every method is `public static`.

---

## 2. Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `float[]` | populated by static initializer | Sine table; 65536 entries; index → sin value in the range [-1.0, +1.0]. |

---

## 3. Constants & Magic Numbers

| Literal | Location | Meaning |
|---|---|---|
| `65536` | Table size; array length | Number of entries in the sine table. Also used as the modular mask as `65535` (= `65536 - 1`). |
| `10430.378F` | `sin` and `cos` index conversion | Radians-to-table-index scale factor. Closest `float` representation of `65536 / (2π)` ≈ 10430.3784… This value is a `float` literal in the source; all intermediate arithmetic uses IEEE 754 single precision. |
| `16384.0F` | `cos` phase offset | Quarter-period offset: 65536 / 4 = 16384. Adding this index offset before masking shifts the sine lookup by a quarter turn, producing cosine. |
| `1024.0` | `b(double)` fast-floor bias | Used only in the fast-floor overload. Adds 1024 before truncation so that the Java `(int)` cast (which truncates toward zero) behaves like floor for the range `[-1024, +∞)`. |
| `Math.PI` | Static initializer | `java.lang.Math.PI` (double, 64-bit). Used only during table construction; not stored. |

---

## 4. Static Initializer

Runs exactly once, before any method is callable, when the class is first loaded.

Step-by-step:
1. Allocate `float[65536]` and assign to `a`.
2. For each index `i` from `0` to `65535` (inclusive):
   - Compute `angle = (double)i * Math.PI * 2.0 / 65536.0`  
     (all arithmetic in `double` precision)
   - Compute `value = Math.sin(angle)`  
     (`java.lang.Math.sin` — result is a `double`)
   - Cast to `float`: `a[i] = (float)value`

The result is a uniformly-sampled single-precision sine wave over one full period.
Entry `a[0]` = `(float)sin(0)` = `0.0f`.
Entry `a[16384]` = `(float)sin(π/2)` = `1.0f`.
Entry `a[32768]` = `(float)sin(π)` ≈ `−1.2246468e-16f` → rounds to `0.0f` after cast to float (implementation-dependent; the key point is it is very close to zero, not exactly zero from the double).
Entry `a[49152]` = `(float)sin(3π/2)` = `−1.0f`.

---

## 5. Methods — Detailed Logic

The class has multiple overloads of the same obfuscated name `a`. They are distinguished
by their parameter types. All return types are listed explicitly.

---

### sin — `a(float var0)` → `float`

**Called by:** entity movement, particle rotation, rendering angles, world-gen.  
**Parameters:** `var0` — angle in radians (float).  
**Returns:** Sine of the angle as a float, read from the lookup table.

Step-by-step:
1. Multiply: `var0 * 10430.378F`  
   Both operands are `float`; result is `float`.
2. Cast to `int`: `(int)(result from step 1)`  
   Java `(int)` truncates toward zero (not floor). If the product is negative, the
   truncation is toward zero (e.g. `(int)-0.9f` = `0`, not `-1`).
3. Bitwise AND with `65535`: `result & 65535`  
   Wraps the index into [0, 65535]. Because `&` on a negative int with 65535 picks
   the low 16 bits, negative indices wrap correctly into the table range.
4. Read `a[index]` and return.

---

### cos — `b(float var0)` → `float`

**Called by:** entity movement, rotation interpolation.  
**Parameters:** `var0` — angle in radians (float).  
**Returns:** Cosine of the angle as a float, read from the lookup table.

Step-by-step:
1. Multiply: `var0 * 10430.378F` (float arithmetic)
2. Add: `result + 16384.0F` (float arithmetic)
3. Cast to `int` (truncation toward zero)
4. Bitwise AND with `65535`
5. Read `a[index]` and return.

The `+16384.0F` offset is applied **before** the `(int)` cast. There is no rounding
step between the multiply, the add, and the cast — it is one expression.

---

### sqrt_float — `c(float var0)` → `float`

**Parameters:** `var0` — non-negative float value.  
**Returns:** Square root as a float.

Step-by-step:
1. Widen `var0` to `double`
2. Call `java.lang.Math.sqrt(double)` → `double` result
3. Narrow to `float` and return

No special-casing for negative input; behaviour is delegated to `Math.sqrt`
(which returns `NaN` for negative doubles).

---

### sqrt_double — `a(double var0)` → `float`

**Parameters:** `var0` — non-negative double value.  
**Returns:** Square root as a float.

Step-by-step:
1. Call `java.lang.Math.sqrt(var0)` → `double` result
2. Narrow to `float` and return

**Note:** This overload takes a `double` and shares the method name `a` with several
other overloads. Callers are distinguished by compile-time argument type. The return
type is `float` in both sqrt overloads — the double precision is used only during
the sqrt computation itself.

---

### floor_float — `d(float var0)` → `int`

**Parameters:** `var0` — any float.  
**Returns:** Largest integer ≤ `var0` (mathematical floor).

Step-by-step:
1. Truncate: `var1 = (int)var0` (Java truncation toward zero)
2. Conditional: if `var0 < (float)var1`, return `var1 - 1`; else return `var1`

The conditional check corrects for negative non-integers:
- `(int)(-1.5f)` = `-1` (truncation toward zero)
- `-1.5f < -1.0f` → true → return `-1 - 1` = `-2` ✓

No correction is needed for exact negative integers:
- `(int)(-2.0f)` = `-2`
- `-2.0f < -2.0f` → false → return `-2` ✓

---

### floor_double_fast — `b(double var0)` → `int`

**Parameters:** `var0` — a double value. MUST be ≥ -1024.0 for a correct result.  
**Returns:** Largest integer ≤ `var0`, BUT ONLY CORRECT for `var0 ≥ -1024.0`.

Step-by-step:
1. Add bias: `var0 + 1024.0` (double arithmetic)
2. Truncate to int: `(int)(result)` (truncation toward zero)
3. Subtract bias: `result - 1024`

**Known quirk / bug to preserve:**  
For `var0 < -1024.0` the bias is insufficient to push the value positive before
truncation, so the `(int)` cast truncates in the wrong direction and the final
subtraction produces an off-by-one. Specifically, for any `var0` in `(-1025.0, -1024.0)`:
- `var0 + 1024.0` is in `(-1.0, 0.0)`
- `(int)` of that range = `0` (truncation toward zero)
- `0 - 1024` = `-1024`, but the correct floor is `-1025` → **wrong by 1**

The original code uses this fast path knowingly for coordinates that are expected to
be within reasonable world bounds (y and x/z dimensions that stay within ±1024 blocks
during the operations that call this overload). **This bug must be preserved.**

---

### floor_double — `c(double var0)` → `int`

**Parameters:** `var0` — any double.  
**Returns:** Largest integer ≤ `var0` (correct for all inputs within `int` range).

Step-by-step:
1. Truncate: `var2 = (int)var0`
2. Conditional: if `var0 < (double)var2`, return `var2 - 1`; else return `var2`

Identical logic to `d(float)` but operates on a `double` argument and the comparison
is also `double`. No known quirks.

---

### floor_long — `d(double var0)` → `long`

**Parameters:** `var0` — any double.  
**Returns:** Largest long ≤ `var0`.

Step-by-step:
1. Truncate: `var2 = (long)var0`
2. Conditional: if `var0 < (double)var2`, return `var2 - 1L`; else return `var2`

Same floor logic, returns `long` instead of `int`. Used for world seed and chunk
coordinate computations where int range is insufficient.

---

### abs_float — `e(float var0)` → `float`

**Parameters:** `var0` — any float.  
**Returns:** Absolute value.

Step-by-step:
1. If `var0 >= 0.0F`, return `var0`; else return `-var0`

Manual implementation; does **not** call `Math.abs`. The behaviour for `-0.0F` is:
`-0.0F >= 0.0F` → true → returns `-0.0F` unchanged (Java float comparison treats
+0.0 and -0.0 as equal). This is consistent with IEEE 754.

---

### abs_int — `a(int var0)` → `int`

**Parameters:** `var0` — any int.  
**Returns:** Absolute value.

Step-by-step:
1. If `var0 >= 0`, return `var0`; else return `-var0`

**Known quirk / bug to preserve:**  
For `var0 = Integer.MIN_VALUE` (−2147483648), `-var0` overflows back to
`Integer.MIN_VALUE` in Java two's-complement arithmetic. The method returns
`Integer.MIN_VALUE` — a negative number — as the "absolute value". Do not correct this.

---

### clamp_int — `a(int var0, int var1, int var2)` → `int`

**Parameters:**  
- `var0` — value to clamp  
- `var1` — minimum (inclusive)  
- `var2` — maximum (inclusive)  

**Returns:** `var0` clamped to [var1, var2].

Step-by-step:
1. If `var0 < var1`, return `var1`
2. Else if `var0 > var2`, return `var2`
3. Else return `var0`

If `var1 > var2`, behaviour is undefined (the original has no guard). In practice the
caller is responsible for passing `var1 ≤ var2`.

---

### abs_max — `a(double var0, double var2)` → `double`

**Parameters:** `var0`, `var2` — any doubles.  
**Returns:** The larger of `|var0|` and `|var2|`.

Step-by-step:
1. If `var0 < 0.0`, set `var0 = -var0`
2. If `var2 < 0.0`, set `var2 = -var2`
3. Return `var0 > var2 ? var0 : var2`

The "negate if negative" approach is the same manual abs as above. Not the same as
`Math.max(Math.abs(var0), Math.abs(var2))` due to potential -0.0 edge cases, but
functionally equivalent for all non-NaN inputs.

---

### bucketInt (floor division) — `a(int var0, int var1)` → `int`

**Parameters:**  
- `var0` — dividend (any int)  
- `var1` — divisor (must be > 0; no guard in source)  

**Returns:** Mathematical floor of `var0 / var1` (integer floor division, not
truncation-toward-zero division).

Step-by-step:
1. If `var0 < 0`:  
   a. Negate: `-var0`  
   b. Subtract 1: `(-var0) - 1`  
   c. Integer-divide by `var1`: `((-var0) - 1) / var1` (truncation toward zero, non-negative ÷ positive = floor)  
   d. Negate: `-(result)`  
   e. Subtract 1: `-(result) - 1`  
   f. Return that value  
2. Else: return `var0 / var1` (truncation toward zero = floor for non-negative dividend)

Verification for `var0 = -3, var1 = 2`:  
Step 1a: `3`  
Step 1b: `2`  
Step 1c: `2 / 2` = `1`  
Step 1d: `-1`  
Step 1e: `-2`  
→ Returns `-2`. Correct: floor(-1.5) = -2.

Verification for `var0 = -4, var1 = 2`:  
Step 1a: `4`; 1b: `3`; 1c: `3/2 = 1`; 1d: `-1`; 1e: `-2`  
→ Returns `-2`. Correct: floor(-2.0) = -2.

---

### isNullOrEmpty — `a(String var0)` → `boolean`

**Parameters:** `var0` — any String reference, may be `null`.  
**Returns:** `true` if `var0` is `null` OR has length 0; `false` otherwise.

Step-by-step:
1. If `var0 == null`, return `true` (reference equality with null)
2. If `var0.length() == 0`, return `true`
3. Else return `false`

---

### getRandomIntegerInRange — `a(Random var0, int var1, int var2)` → `int`

**Parameters:**  
- `var0` — `java.util.Random` instance (not SpectraEngine's `JavaRandom`)  
- `var1` — minimum value (inclusive)  
- `var2` — maximum value (inclusive)  

**Returns:** A random integer in [var1, var2] inclusive.

Step-by-step:
1. If `var1 >= var2`, return `var1` (no randomness; degenerate range)
2. Else compute `range = var2 - var1 + 1`
3. Call `var0.nextInt(range)` → result in [0, range-1]
4. Return `result + var1`

The range is **inclusive on both ends**: passing `(rng, 3, 5)` can return 3, 4, or 5.

---

## 6. Bitwise & Data Layouts

The only bitwise operation in this class is the mask in `sin` and `cos`:

```
(int)(angle * 10430.378F) & 65535
```

Bit layout of the 32-bit int produced by the cast:
- Bits [31..16]: upper 16 bits — discarded by `& 65535`
- Bits [15..0]:  lower 16 bits — used as the table index [0, 65535]

The `&` with `65535` (= `0x0000FFFF`) zeros bits 31..16 and preserves bits 15..0,
wrapping any angle into the table period regardless of how many full rotations are
contained in the input angle.

---

## 7. Tick Behaviour

This class has no tick entry point and no per-frame state. Its methods are called
on-demand from other systems that have their own tick cadence. The static initializer
runs once at JVM class-load time. After that, the sine table is read-only.

---

## 8. Known Quirks / Bugs to Preserve

| # | Method | Quirk | Must preserve? |
|---|---|---|---|
| 1 | `b(double)` (fast floor) | Only correct for `var0 ≥ -1024.0`; gives floor off-by-one for inputs below that threshold. | **Yes** — callers rely on speed, not correctness at extreme values. |
| 2 | `a(int)` (abs_int) | Returns `Integer.MIN_VALUE` unchanged when given `Integer.MIN_VALUE` (overflow). | **Yes** — do not insert a guard. |
| 3 | `sin` / `cos` | Table is single-precision only (float). Small angles have quantisation error of up to ±3×10⁻⁵ radians in the index before lookup. This is intentional for performance. | **Yes** — do not promote to double. |
| 4 | `cos` phase offset | The `+16384.0F` addition is float arithmetic, applied before truncation. Floating-point rounding in this addition is part of the original behaviour. | **Yes** — do not reorder operations. |

---

## 9. Open Questions

None. All methods are fully resolved from the source.

---

*Spec written by Analyst AI from `me.java` (decompiled). No C# implementation consulted.*
