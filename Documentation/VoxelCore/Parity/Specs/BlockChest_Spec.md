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

# BlockChest Spec (Single and Double Chest)
**Source class:** `au.java` (BlockChest), `ba.java` (BlockContainer base)
**Superclass:** `ba` (BlockContainer → `yy`)
**Block ID:** 54
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`au` is the chest block. It has a TileEntity (`tu` = TileEntityChest) holding 27 item slots.
Two adjacent chests form a "large chest" (double chest) with 54 slots. The block handles
placement facing, double-chest detection, and inventory opening logic.

---

## 2. Material and Properties

| Property | Value |
|---|---|
| Material | `p.d` (wood) |
| Texture index | 26 (side texture; lid and latch are separate) |
| Opaque cube | `false` |
| Normal cube | `false` |
| Render type | 22 |
| TileEntity class | `tu` (TileEntityChest) |

---

## 3. Metadata — Facing Direction

| Value | Facing |
|---|---|
| 2 | North (-Z) |
| 3 | South (+Z) |
| 4 | West (-X) |
| 5 | East (+X) |

Metadata does not encode open/closed; that is handled by the TileEntity's animation counter.

---

## 4. Placement `a(World, x, y, z, LivingEntity placer)`

1. Compute facing from placer yaw (same formula as furnace):
   ```
   dir = (floor(yaw * 4/360 + 0.5) & 3)
   ```
2. Mapping:
   - `dir == 0` → metadata 2 (north)
   - `dir == 1` → metadata 5 (east)
   - `dir == 2` → metadata 3 (south)
   - `dir == 3` → metadata 4 (west)
3. If adjacent chests exist: re-orient both this chest AND the neighbor so they face the
   same direction (parallel to the double-chest pair axis).
   - If neighbor is on Z axis: force both to face 4 or 5 (E-W).
   - If neighbor is on X axis: force both to face 2 or 3 (N-S).

---

## 5. Placement Change Notification `a(World, x, y, z)` (block added)

Calls `super.a()`, then calls `a_(world, x, y, z)` to re-evaluate facing of self.
Also calls `a_(world, ...)` on each of the 4 adjacent chests (if any) — ensuring double-chest
pairs update their facing when a new neighbor is placed.

---

## 6. Right-Click `a(World, x, y, z, Player)`

Server side only. Logic:

**Step 1 — Get TileEntity:**
```
var6 = (tu) world.b(x, y, z)
```
If null: return `true` (no chest TE, ignore).

**Step 2 — Obstruction check:**
The chest cannot be opened if:
- Block above (y+1) is solid (`world.g(x, y+1, z)` returns `true`).
- OR: adjacent chest at x-1 has a solid block above it.
- OR: adjacent chest at x+1 has a solid block above it.
- OR: adjacent chest at z-1 has a solid block above it.
- OR: adjacent chest at z+1 has a solid block above it.

If obstructed: return `true` (consume click but do nothing).

**Step 3 — Build inventory:**
Check all 4 adjacent positions for another chest block. On first match, create double-chest:
```
var6 = new adv("Large chest", neighborTE, thisTE)   // or (thisTE, neighborTE)
```
Order depends on direction (neighbor at z-1: neighbor is "left", neighbor at z+1: neighbor
is "right", etc.).

If no adjacent chest: use single TileEntity `tu`.

**Step 4 — Open GUI:**
Client side: return `true` (client handles animation).
Server side: `player.a((de) var6)` — opens the inventory GUI.

---

## 7. Break Logic `d(World, x, y, z)` (pre-break item scatter)

Before removing the block:
1. Get TileEntity `tu` via `world.b(x, y, z)`.
2. If not null: for each slot 0–26, scatter items as `ih` (EntityItem) entities with Gaussian velocity.
3. Then call `super.d()` (standard break — drops the chest item).

---

## 8. Double Chest — `adv` (InventoryLargeChest)

`adv` is the large chest inventory. Constructor: `adv(String name, de left, de right)`.
- Combines two 27-slot inventories into 54 slots.
- Slot 0–26 → left chest.
- Slot 27–53 → right chest.
- All standard `de` (IInventory) methods delegate to the appropriate half.

---

## 9. TileEntity — `tu` (TileEntityChest)

- 27 item slots.
- Lid animation counter: increments when a player opens, decrements when closed.
- NBT: standard `"Items"` array (same as minecart chest).
- Method `n()` — "numPlayersUsing" update: called when neighbor block changes, used to
  control lid animation.

---

## 10. Open Questions

| # | Question |
|---|---|
| 10.1 | `world.g(x, y, z)` — is this "is solid face on top" (isSolidFace)? |
| 10.2 | Placement direction: does the chest face the player (opposite of walking direction) or away? Needs confirmation from au.java placement `a(LivingEntity)`. |
| 10.3 | Double-chest orientation: which chest is "left" and which is "right" in the adv constructor for each direction? |
| 10.4 | Does `au` support wall-placement (metadata values 0/1), or only floor placement with directional facing? |
