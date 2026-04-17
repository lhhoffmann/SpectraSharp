<!--
  SpectraEngine Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraEngine Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# Container System Spec
**Source classes:** `pj.java` (Container base), `vv.java` (Slot), `ace.java` (ContainerWorkbench),
`gd.java` (ContainerPlayer), `eg.java` (ContainerFurnace), `ak.java` (ContainerChest),
`afe.java` (SlotCrafting), `ie.java` (SlotFurnaceOutput), `pi.java` (SlotArmor),
`lm.java` (CraftingInventory), `iy.java` (OutputInventory single-slot)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Map

| Class | Role |
|---|---|
| `pj` | Container — abstract base |
| `vv` | Slot — one inventory cell in a container |
| `ace` | ContainerWorkbench — 3×3 crafting grid (opened by workbench block) |
| `gd` | ContainerPlayer — 2×2 crafting grid + 4 armor slots (opened by `E` key) |
| `eg` | ContainerFurnace — 3 slots: input/fuel/output |
| `ak` | ContainerChest — 27 or 54 slots (single/double chest) |
| `afe` | SlotCrafting — output slot; reads from `sl` (CraftingManager) on any change |
| `ie` | SlotFurnaceOutput — output slot for smelted items |
| `pi` | SlotArmor — one of 4 player armor slots |
| `lm` | CraftingInventory — NxM grid inventory for crafting grids |
| `iy` | SingleInventory — 1-slot output buffer for crafting result |
| `sl` | CraftingManager — singleton; finds recipes |
| `mt` | FurnaceRecipes — singleton; furnace input→output map |
| `abd` | ICrafting — listener interface; notified on slot change |

---

## 2. `pj` — Container Base

### 2.1 Fields

| Field | Type | Meaning |
|---|---|---|
| `e` | `List<vv>` | All slots in this container |
| `d` | `List<dk>` | Last-seen item snapshots for change detection |
| `f` | `int` | Unused in base; subclasses may use |
| `g` | `List<abd>` | Registered ICrafting listeners (players watching this container) |

### 2.2 Slot Registration — `a(vv slot)`

Appends slot to `e`; assigns `slot.d = e.size()` (slot index); adds `null` placeholder to `d`.

### 2.3 Change Detection — `a()` (detectAndSendChanges)

For each slot index, compares current `vv.b()` against last snapshot in `d`.
If changed: update snapshot and notify all listeners via `abd.a(pj, slotIndex, ItemStack)`.

### 2.4 Click Handler — `a(int slotId, int button, boolean shift, vi player)`

Returns the item the player picks up (may be `null`). `button`: 0=left, 1=right. `shift`=true for shift-click.

**If `slotId == -999`** (click outside inventory window):
- button 0: drop entire held cursor stack.
- button 1: drop one item from cursor.

**If `shift == true`** (shift-click):
- Get slot item; call `b(slotId, button, shift, player)` (delegated to subclass shift-click logic).

**Normal left/right click (`shift == false`):**
- If `slotId < 0`: return null.
- Get slot `var12` and its contents `var13`. Get cursor stack `var14 = player.by.i()`.
- If slot empty AND cursor has item AND slot accepts item: place cursor into slot.
  - button 0: place all (or up to slot max).
  - button 1: place 1.
- If slot has item AND cursor empty: take from slot.
  - button 0: take all.
  - button 1: take half (rounded up).
- If slot and cursor both have items:
  - Same item ID + compatible damage: merge (add cursor into slot up to max stack).
  - Different item IDs: swap cursor ↔ slot if cursor stack ≤ slot max.
  - Same item in cursor with slot full, stackable: split from slot into cursor (remaining logic).

### 2.5 Container Close — `b(vi player)`

Drops any item in the player's cursor slot (`player.by.i()`) and clears it.

### 2.6 Merge Helper — `a(dk itemStack, int startSlot, int endSlot, boolean reverse)`

Attempts to merge `itemStack` into all slots in range `[startSlot, endSlot)`:
1. First pass: merge into existing stacks of the same item.
2. Second pass: place into empty slots.
Returns `true` if any items were moved.

### 2.7 Counter — `a(x playerInventory)`

Returns the container's unique transaction counter `a` (short), incrementing it each call.
Used to synchronise window data between server and client.

---

## 3. `vv` — Slot

| Field | Type | Meaning |
|---|---|---|
| `c` | `de` (IInventory) | The inventory this slot belongs to |
| `a` | `int` (private) | Slot index within the inventory |
| `e` | `int` | Screen X position (pixels from left) |
| `f` | `int` | Screen Y position (pixels from top) |

Key methods:
- `b()` — get current ItemStack (`inventory.d(index)`).
- `c(dk)` — set ItemStack (`inventory.a(index, item)`) then call `d()`.
- `d()` — mark dirty (`inventory.h()`).
- `a()` — max stack size (`inventory.e()`).
- `a(dk)` — can this item be placed here? Default: `true` (all items accepted).
- `a(int count)` — take `count` items from this slot (`inventory.a(index, count)`).
- `c()` — has item? (`b() != null`).

---

## 4. `ace` — ContainerWorkbench

Opened by `BlockWorkbench.OnBlockActivated` at world position `(h, i, j)`.

### 4.1 Slot Layout

| Slot | Class | Inventory | Index | X | Y |
|---|---|---|---|---|---|
| 0 | `afe` (SlotCrafting) | output `iy` | 0 | 124 | 35 |
| 1–9 | `vv` | `lm` (3×3 grid) | 0–8 | grid positions | 17–53 |
| 10–36 | `vv` | player inventory | 9–35 | 8+ | 84–138 |
| 37–45 | `vv` | player hotbar | 0–8 | 8+ | 142 |

### 4.2 Crafting Update — `a(de inv)`

When any slot changes (`onCraftMatrixChanged`):
- Calls `sl.a().a(this.lm)` — CraftingManager finds matching recipe.
- Sets output slot `b` to the result (or null if no recipe matches).

### 4.3 Container Close — `b(vi player)`

Calls `super.b()` (drop cursor), then drops all 9 crafting grid items if not on server.

### 4.4 Validity — `a(vi player)`

Returns false if workbench block at `(h, i, j)` is no longer block `yy.ay`, or player is > 8 blocks away (distance² > 64).

---

## 5. `gd` — ContainerPlayer

Player inventory screen (opened via `E` key). Similar to `ace` but with a 2×2 crafting grid.

### 5.1 Slot Layout

| Slot | Class | Inventory | Index | Description |
|---|---|---|---|---|
| 0 | `afe` (SlotCrafting) | output `iy` | 0 | Crafting output |
| 1–4 | `vv` | `lm` (2×2) | 0–3 | Crafting grid |
| 5–8 | `pi` (SlotArmor) | player inventory | last 4 slots | Armor slots |
| 9–35 | `vv` | player inventory | 9–35 | Main inventory |
| 36–44 | `vv` | player inventory | 0–8 | Hotbar |

### 5.2 Validity — `a(vi player)`

Always returns `true` (player inventory is always accessible).

---

## 6. `eg` — ContainerFurnace

Opened by `BlockFurnace.OnBlockActivated`. Wraps TileEntityFurnace (`oe var2`).

### 6.1 Slot Layout

| Slot | Class | Inventory | Index | X | Y | Description |
|---|---|---|---|---|---|---|
| 0 | `vv` | `oe` furnace | 0 | 56 | 17 | Input item |
| 1 | `vv` | `oe` furnace | 1 | 56 | 53 | Fuel |
| 2 | `ie` (SlotFurnaceOutput) | `oe` furnace | 2 | 116 | 35 | Output |
| 3–29 | `vv` | player | 9–35 | 8+ | 84+ | Player inventory |
| 30–38 | `vv` | player | 0–8 | 8+ | 142 | Hotbar |

### 6.2 Progress Sync — `a()` (detectAndSendChanges)

Calls `super.a()` first, then sends data IDs to all ICrafting listeners:

| Data ID | `oe` field | Meaning |
|---|---|---|
| 0 | `a.j` | `cookTime` — progress 0–200 ticks |
| 1 | `a.a` | `burnTime` — current fuel ticks remaining |
| 2 | `a.b` | `currentBurnTime` — full burn duration of current fuel |

### 6.3 Server → Client — `a(int id, int value)`

Sets the corresponding `oe` field from the server update.

### 6.4 Validity — `a(vi player)`

Delegates to `oe.b_(player)` — same TileEntity position + distance check as chest.

---

## 7. `ak` — ContainerChest

Handles single chest (27 slots) and double chest (54 slots = `adv` combined inventory).

### 7.1 Slot Layout

`b = inventory.c() / 9` = number of chest rows.

| Slot | Class | Inventory | Description |
|---|---|---|---|
| 0..(b*9-1) | `vv` | chest `de` | Chest slots (27 or 54) |
| b*9..(b*9+26) | `vv` | player | Main inventory |
| (b*9+27)..(b*9+35) | `vv` | player | Hotbar |

### 7.2 Open / Close Inventory

Constructor calls `inventory.j()` (openInventory — increments `numPlayersUsing`).
`b(vi player)` calls `super.b()` then `inventory.k()` (closeInventory — decrements `numPlayersUsing`).

---

## 8. TileEntityFurnace — `oe`

Extends `bq` (TileEntity), implements `de` (IInventory). 3 item slots.

| Field | Type | Meaning |
|---|---|---|
| `k[3]` | `dk[]` | Slots: [0]=input, [1]=fuel, [2]=output |
| `a` | `int` | `burnTime` — remaining fuel ticks |
| `b` | `int` | `currentBurnTime` — burn duration of last fuel |
| `j` | `int` | `cookTime` — smelting progress (0–200) |

### 8.1 Tick — `b()` (updateEntity)

Per tick (server only):
1. If `a > 0`: decrement `a` (burn one tick of fuel).
2. If `a == 0` AND can smelt (`p()`): consume next fuel: `b = a = a(k[1])`.
3. If burning AND can smelt: increment `j`. At `j == 200`: smelt (call `o()`), reset `j = 0`.
4. Else: reset `j = 0` (progress resets if can't smelt).
5. If lit state changed: call `eu.a(isLit, world, x, y, z)` (swap lit/unlit block).
6. If state changed: mark dirty.

### 8.2 Can Smelt — `p()`

1. If `k[0]` (input) is null: false.
2. Look up output from `mt.a().a(k[0].itemId)` — FurnaceRecipes.
3. If no recipe: false.
4. If `k[2]` (output) is null: true.
5. If output slot item type doesn't match: false.
6. If output count < slot max (64 or item stack max): true.

### 8.3 Smelt — `o()`

1. Look up output `dk var1 = mt.a().a(k[0])`.
2. If `k[2]` is null: set `k[2] = var1.copy()`.
3. Else if same item: `k[2].a++` (increment count).
4. Decrement input: `k[0].a--`. If 0: `k[0] = null`.

### 8.4 Fuel Values — `a(dk item)` → ticks

| Item | Ticks |
|---|---|
| Any block with material `p.d` (wood) | 300 |
| `acy.C` (stick?) | 100 |
| `acy.l` (coal) | 1600 |
| `acy.ax` (lava bucket?) | 20000 |
| `yy.y` (sapling) | 100 |
| `acy.bn` (blaze rod) | 2400 |

> Open Question 8.1: Confirm `acy.C` = stick (ID 280) and `acy.ax` = lava bucket (ID 327).
> Note: `acy.ax` in item registry = lava bucket (confirmed by `en.java` analysis).

### 8.5 Convenience Methods for GUI

- `a(int totalTicks)` → `cookTime * totalTicks / 200` (progress bar width).
- `b(int totalTicks)` → `burnTime * totalTicks / currentBurnTime` (fuel bar height).
- `a()` → `burnTime > 0` (is currently burning).

### 8.6 NBT

```
b(nbt):  BurnTime (short), CookTime (short), Items array (standard slot format)
a(nbt):  saves same; loads b = a(k[1]) to recalculate currentBurnTime
```

---

## 9. Open Questions

| # | Question |
|---|---|
| 9.1 | `afe` (SlotCrafting) — exact logic for on-take: decrements all 9 grid items, triggers `onCrafting`? |
| 9.2 | `ie` (SlotFurnaceOutput) — same but for furnace output? |
| 9.3 | `pi` (SlotArmor) — accepts only items of matching armor type? Which `a(dk)` override? |
| 9.4 | What is `x` (InventoryPlayer)? Field `d` is the main inventory IInventory. Confirm field names. |
| 9.5 | Is `abd` (ICrafting) an interface? Only one method `a(pj, int, dk)` observed. |
| 9.6 | The `lm` (CraftingInventory) constructor param — is it `lm(pj container, int width, int height)`? |
