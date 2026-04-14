# IInventory Spec
Source: `de.java` (interface, ~20 lines)
Type: Interface definition

---

## 1. Overview

`de` is the `IInventory` interface. All inventory containers implement it.
`vi` (EntityPlayer) is the only entity type that can access inventories (`b_(vi)` method).

---

## 2. Interface Methods

```java
public interface de {
    int c();
    dk d(int slot);
    dk a(int slot, int count);
    void a(int slot, dk stack);
    String d();
    int e();
    void h();
    boolean b_(vi player);
    void j();
    void k();
}
```

| Method signature | Semantic name | Notes |
|---|---|---|
| `int c()` | getSizeInventory | Total number of slots |
| `dk d(int slot)` | getStackInSlot | Returns ItemStack at slot; null if empty |
| `dk a(int slot, int count)` | decrStackSize | Removes up to `count` items from slot; returns removed stack; mutates or nulls the slot |
| `void a(int slot, dk stack)` | setInventorySlotContents | Replaces slot with given ItemStack |
| `String d()` | getInvName | Display name (e.g., `"Inventory"`, `"container.chest"`) |
| `int e()` | getInventoryStackLimit | Maximum stack size for this inventory (usually 64) |
| `void h()` | onInventoryChanged | Called after slot modification; triggers tile entity sync etc. |
| `boolean b_(vi player)` | isUseableByPlayer | Returns true if the player is close enough to interact |
| `void j()` | openChest | Called when a player opens the container (hook for chest lid animation etc.) |
| `void k()` | closeChest | Called when a player closes the container |

> **Naming conflict note:** Both `dk d(int)` and `String d()` are named `d` in the obfuscated bytecode. Java allows this because return-type differs — method resolution is by parameter list. The `getStackInSlot` takes an int; `getInvName` takes no args.

---

## 3. Parameter Details

### `a(int slot, int count)` — decrStackSize

- If the slot's stack size ≤ count: remove the entire stack, set slot to null, return the original stack.
- If the slot's stack size > count: call `splitStack(count)` on the ItemStack (returns a new stack with `count` items, reduces source), set `onInventoryChanged`, return the split stack.
- Never returns null when slot is non-null; caller must check that slot is non-null first.

---

## 4. Implementing Classes

| Class | Human name |
|---|---|
| `x` | InventoryPlayer |
| `au` | BlockChest (tile entity) |
| (others) | Furnace, Dispenser, etc. |

---

*Spec written by Analyst AI from `de.java`. No C# implementation consulted.*
