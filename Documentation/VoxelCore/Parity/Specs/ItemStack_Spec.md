# ItemStack Spec
Source class: `dk.java`
Type: `final class`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`ItemStack` (`dk`) is the universal item-slot container. Every inventory slot, dropped-item
entity, and block drop holds a `dk`. It pairs an item type (by numeric ID) with a quantity
and a damage/metadata value.

---

## 2. Fields

| Field | Visibility | Type | Semantics |
|---|---|---|---|
| `c` | (package) | `int` | Item ID — index into `acy.d[]` (Item registry) |
| `a` | (package) | `int` | Stack size (quantity). When `a <= 0` the stack is considered empty/consumed. |
| `e` | `private` | `int` | Item damage / metadata value |
| `b` | (package) | `int` | Use timer (countdown while item is being used, e.g. eating) |
| `d` | (package) | `ik` (NBTTagCompound) | Optional NBT tag for enchantments, custom data |

> **Naming quirk:** the field order is non-alphabetical — `c`=itemId, `a`=stackSize,
> `e`=damage. This is a common source of confusion. `a` does NOT mean the item type.

---

## 3. Constructors

### Primary constructor
```java
dk(int itemId, int count, int damage)
    c = itemId;
    a = count;
    e = damage;
```

### Secondary constructor (no damage)
```java
dk(int itemId, int count)
    → delegates to dk(itemId, count, 0)
```

### Tertiary constructor (count=1, no damage)
```java
dk(int itemId)
    → delegates to dk(itemId, 1, 0)
```

### Copy constructor
```java
dk(dk source)
    c = source.c;
    a = source.a;
    e = source.e;
    if (source.d != null) d = source.d.copy();
```

---

## 4. Methods

### Item access — `a()` → `acy`
```java
return acy.d[c];
```
Returns the `Item` instance for this stack's item ID. `acy` is the Item base class;
`acy.d[]` is the static item registry array indexed by item ID.

### Stack-size getters

| Method | Returns |
|---|---|
| `a` field directly | stackSize (package-visible) |

No dedicated getter method for stackSize in the primary API — callers read `dk.a` directly.

### Damage / metadata getters

| Method | Signature | Returns |
|---|---|---|
| `h()` | `→ int` | `e` (itemDamage) |
| `i()` | `→ int` | `e` (itemDamage) — identical to `h()`; two names for same value |

### Max stack size — `f()` → `int`
```java
return Math.min(a().e(), 64);
```
Delegates to the Item's max stack size, capped at 64. (`e()` on Item returns the item's own
max stack size — may be 1 for tools/armour, 16 for some items, 64 for general items.)

### Max damage — `j()` → `int`
```java
return a().c();
```
Delegates to the Item's max durability (`c()` on Item). Returns 0 for undamageable items.

### Split stack — `a(int count)` → `dk`
```java
int n = Math.min(count, a);
dk split = copy();
split.a = n;
this.a -= n;
return split;
```
Mutates the original stack's count. Returns the split-off portion.

### Copy — `b()` → `dk`
```java
dk copy = new dk(c, a, e);
if (d != null) copy.d = d.copy();
return copy;
```

### Damage item — `a(int damage, nq entity)` → `boolean`

`nq` is the `LivingEntity` base class. This method applies durability damage:

```java
if (a().c() == 0) return false;   // undamageable
if (entity != null) {
    // Unbreaking enchantment check:
    int level = EnchantmentHelper.getLevel(Enchantment.unbreaking, this);
    if (level > 0) {
        int chance = 100 / (level + 1);
        if (random.nextInt(100) >= chance) return false;   // damage avoided
    }
}
e += damage;
return e > j();   // returns true if item broke (damage exceeded maxDamage)
```

Returns `true` if the item should break (caller is responsible for destroying the stack).

### NBT serialisation — `b(ik)` → `ik` (writeToNBT)
```java
tag.setShort("id",    (short)c);
tag.setByte ("Count", (byte)a);
tag.setShort("Damage",(short)e);
if (d != null) tag.setCompoundTag("tag", d);
return tag;
```

### NBT deserialisation — `a(ik)` → `dk` (static readFromNBT)
```java
c = tag.getShort("id");
a = tag.getByte("Count");
e = tag.getShort("Damage");
if (tag.hasKey("tag")) d = tag.getCompoundTag("tag");
```

### Enchantment helpers

| Method | Returns |
|---|---|
| `n()` | `d != null && d.hasKey("ench")` — hasEnchantments |
| `o()` | `d` — getTagCompound (the full NBT root) |
| `p()` | `d.getTagList("ench")` — getEnchantmentTagList |
| `u()` | `n()` — isEnchanted (same predicate as `n()`) |

### Use timer

| Method | Semantics |
|---|---|
| `b` field | Use timer — decremented by the using entity each tick |

No dedicated setter in the spec; callers write `dk.b` directly.

---

## 5. Equality and Matching

No `equals()` override is present in the base `dk` class (uses `Object.equals` = reference
equality). Item merging in inventories uses a separate helper that checks
`c == other.c && e == other.e` (same ID and damage).

---

## 6. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | Field names are non-intuitive: `c`=itemId, `a`=stackSize, `e`=itemDamage. `a` (stackSize) conflicts with the `a()` (getItem) method — the field and the method share the same letter but different signatures. |
| 2 | `h()` and `i()` both return `e` (itemDamage) — two identical getters with different names. Both are called in different contexts in the codebase. |
| 3 | `a(int damage, nq entity)` returns `boolean` indicating breakage, but does NOT mutate the stack to empty — the caller must destroy/remove the stack if it returns `true`. |
| 4 | `splitStack(int count)` mutates the source stack (`this.a -= n`). Callers must not call it on a stack they do not own. |
| 5 | Copy constructor deep-copies the NBT tag (`d.copy()`), but `b()` (copy method) also does so. There are thus two copy paths. |

---

## 7. Open Questions

1. **`acy`** — Item base class. `acy.d[]` is the static registry. `acy.e()` = maxStackSize,
   `acy.c()` = maxDamage. Full Item spec pending if needed.
2. **`ik`** — NBTTagCompound. Used for enchantment/custom data storage. Spec pending.
3. **`nq`** — LivingEntity base class (extends `ia`). Spec pending.

---

*Spec written by Analyst AI from `dk.java` (346 lines). No C# implementation consulted.*
