# InventoryPlayer Spec
Source: `x.java` (313 lines); implements `de` (IInventory)
Type: Class definition

---

## 1. Overview

`x` is the player's inventory. It is created inside `vi` (EntityPlayer):
```java
public x by = new x(this);
```

Two arrays hold items:
- `a` — `dk[36]` — main inventory (slots 0–35)
- `b` — `dk[4]` — armor slots (0–3, where 0=boots, 1=leggings, 2=chestplate, 3=helmet)

The currently selected hotbar slot is `c` (int, 0–8).

---

## 2. Fields

| Field | Type | Meaning |
|---|---|---|
| `a` | `dk[36]` | Main inventory; slots 0–8 are the hotbar |
| `b` | `dk[4]` | Armor slots: index 0=boots, 1=leggings, 2=chestplate, 3=helmet |
| `c` | `int` | Currently selected hotbar slot index (0–8) |
| (owner) | `vi` | Back-reference to the owning EntityPlayer |

---

## 3. IInventory Implementations

| Method | Return / Effect |
|---|---|
| `c()` | Returns **40** (36 main + 4 armor = total slot count) |
| `d(int slot)` | Returns `a[slot]` for slots 0–35; `b[slot-36]` for slots 36–39 |
| `a(int slot, int count)` | decrStackSize — see IInventory spec §3 |
| `a(int slot, dk)` | Sets slot; same 0–35 / 36–39 mapping as `d(int)` |
| `d()` | Returns `"Inventory"` |
| `e()` | Returns **64** (max stack size) |
| `h()` | onInventoryChanged — no-op at inventory level (no tile entity to sync) |
| `b_(vi player)` | Returns `true` always (player always has access to own inventory) |
| `j()` | openChest — no-op |
| `k()` | closeChest — no-op |

---

## 4. Key Methods

### `a()` — getStackInSelectedSlot
Returns `a[c]` — the ItemStack in the currently selected hotbar slot.

### `b()` — decrementAnimations
Iterates all main slots (0–35); if `animationsToGo > 0` decrement it.
Also called each tick from `vi.c()`.

### `b(yy block)` — canHarvestBlock
Returns true if the currently held item can harvest the given block effectively.
Delegates to `acy.d[a[c].c].b(block)` if a[c] is non-null; otherwise returns false.

### `a(yy block)` — getStrVsBlock  
Returns the break-speed multiplier of the currently held item against a block.
Delegates to `acy.d[a[c].c].a(block, a[c])` if a[c] is non-null; otherwise returns 1.0F.

### `g()` — dropAllItems (called on player death)
Drops all stacks from `a[]` and `b[]` into the world. Sets each slot to null after dropping.
Calls `vi.a(dk, false)` (the drop helper) per stack.

### `a(dk, boolean)` — addItemStackToInventory
Tries to merge the given stack into existing partial stacks, then into empty slots.
Returns true if the entire stack was absorbed; false if any items remain.

### NBT serialisation
- `a(yi nbt)` — write: serialises all non-null stacks from both `a[]` and `b[]`.
  Each compound tag has keys: `"id"` (short), `"Count"` (byte), `"Damage"` (short),
  `"Slot"` (byte — 0–35 for main, **100–103 for armor** slots 0–3).
- `b(yi nbt)` — read: reads all compound tags from the list; slot byte determines
  destination: 100–103 → `b[slot-100]`, otherwise `a[slot]`.

> **Armor NBT index quirk:** Armor slots are stored at NBT indices 100–103, not 36–39.
> This is intentional — vanilla compatibility. The `c()` method still returns 40 (uses 36–39 in-memory).

---

## 5. Hotbar Slot Selection

- `c` is changed by the client input system (scroll wheel / number keys 1–9).
- `c` is clamped to [0, 8] at all times.
- `a()` returns `a[c]` — the held item.

---

*Spec written by Analyst AI from `x.java`. No C# implementation consulted.*
