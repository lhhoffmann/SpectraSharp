# Spec: BlockDispenser

**Java class:** `cu`
**Status:** PROVIDED
**Canonical name:** BlockDispenser

---

## Overview

The Dispenser is a directional container block that ejects items when powered by a Redstone
signal. It fires projectiles for certain items and drops others as EntityItem. Facing direction
is encoded in block metadata.

---

## Block Properties

| Property | Value |
|---|---|
| Block ID (`c()`) | (not recorded — not the render type) |
| Material | (solid) |
| Has TileEntity | Yes — `bp` (TileEntityDispenser) |

---

## Facing Metadata

| Meta value | Direction | Notes |
|---|---|---|
| 2 | North (−Z) | |
| 3 | South (+Z) | |
| 4 | West (−X) | |
| 5 | East (+X) | |

> Values 0 and 1 (down/up) are not used in 1.0 dispenser logic.

---

## Dispense Logic

Triggered by Redstone power change (rising edge). Retrieves `bp` (TileEntityDispenser)
from the world at the block position.

### Select slot

```java
bp.a(Random)  // returns slot index of a random non-empty slot
              // returns -1 if all slots empty
```

### Dispatch by item type

| Item condition | Action | Entity class |
|---|---|---|
| `acy.k` (Arrow) | Spawn `ro` projectile, vel = facing × (1.1 × 6.0F), `isPlayerArrow = false` | `ro` (EntityArrow) |
| `acy.aO` (Snowball) | Spawn `qw`, vel = facing × default snowball speed | `qw` (EntitySnowball) |
| `acy.aC` (Egg) | Spawn `aah`, vel = facing × default egg speed | `aah` (EntityEgg) |
| `acy.br` + splash flag | Spawn `ab` (splash potion), vel = facing × (1.375 × 3.0F) | `ab` (EntityPotion) |
| Any other item | Drop as `EntityItem`, velocity = facing direction | (EntityItem) |
| No item (empty) | Fire world event 1001 ("random.click" higher pitch), do nothing | — |

After any successful dispatch (non-empty): fire world event **2000** (smoke particles at dispenser face).

### Projectile velocity computation

Facing direction vectors:
```
North (2): (0, 0, -1)
South (3): (0, 0, +1)
West  (4): (-1, 0, 0)
East  (5): (+1, 0, 0)
```

Projectile position offset: ½ block from centre toward facing direction.

Arrow multiplier: `1.1 × 6.0 = 6.6`
Splash potion multiplier: `1.375 × 3.0 = 4.125`

---

## World Events Fired

| Condition | Event ID | Effect |
|---|---|---|
| Item dispensed | 2000 | Smoke particles at dispenser face (10 particles, direction encoded in data) |
| Slot was empty | 1000 | "random.click" vol 1.0 pitch 1.0 |
| Slot was empty | 1001 | "random.click" vol 1.0 pitch 1.2 |

> Events 1000 and 1001 are both fired in sequence for the empty case, per `afv` event table.
> (Verify: may be only 1001.)

---

## Break Behaviour

When the dispenser block is broken, it drops the entire inventory as EntityItem stacks:
iterates all 9 slots of `bp` and spawns an EntityItem for each non-null stack.

---

## TileEntity: `bp` (TileEntityDispenser)

| Slot | Count |
|---|---|
| Inventory size | 9 |

`bp.a(Random)` — selects a random non-empty slot index; returns −1 if inventory is empty.

---

## C# Mapping

| Java | C# |
|---|---|
| `cu` | `DispenserBlock` |
| `bp` | `DispenserBlockEntity` |
| `acy.k` | `ItemRegistry.Arrow` |
| `acy.aO` | `ItemRegistry.Snowball` |
| `acy.aC` | `ItemRegistry.Egg` |
| `acy.br` | `ItemRegistry.SplashPotion` |
| `ro` | `EntityArrow` |
| `qw` | `EntitySnowball` |
| `aah` | `EntityEgg` |
| `ab` | `EntityPotion` |
| facing meta | `DispenserBlock.Facing : Direction` |

---

## Open Questions

- Whether events 1000 and 1001 are both fired for empty dispenser or only 1001.
- Exact splash potion detection condition (`acy.br` + which flag on the ItemStack?).
- Y offset of projectile spawn position (flat or centre of facing face?).
- Whether `isPlayerArrow=false` on arrow affects damage/pickup behaviour.
