# EnumMovingObjectType Spec
Source class: `bo.java`
Superclass: `java.lang.Enum<bo>` (implicit for Java enums)

---

## 1. Purpose

`EnumMovingObjectType` is a two-value discriminator enum used solely by
`MovingObjectPosition` to indicate whether a ray cast hit a block (tile) or an entity.

---

## 2. Enum Constants

| Constant (obf) | Human name | Ordinal |
|---|---|---|
| `bo.a` | `TILE` | 0 |
| `bo.b` | `ENTITY` | 1 |

Java enums assign ordinal values implicitly in declaration order, starting from 0.
No explicit ordinal is set in the source. The ordinal itself is never read in the
codebase — only identity comparisons (`a == bo.a`, `a == bo.b`) are used.

---

## 3. Fields & Methods

The enum declares no additional fields, constructors, or methods beyond those inherited
from `java.lang.Enum` (`name()`, `ordinal()`, `values()`, `valueOf(String)`). None of
these inherited methods are called in the codebase.

---

## 4. Constants & Magic Numbers

None.

---

## 5. Methods — Detailed Logic

No custom methods.

---

## 6. Bitwise & Data Layouts

Not applicable.

---

## 7. Tick Behaviour

Not applicable.

---

## 8. Known Quirks / Bugs to Preserve

None. The enum is trivial.

---

## 9. Open Questions

None.

---

*Spec written by Analyst AI from `bo.java` (4 lines, decompiled). No C# implementation consulted.*
