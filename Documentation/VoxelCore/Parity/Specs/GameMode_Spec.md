# Spec: GameMode / PlayerAbilities

**Java classes:** `vi` (EntityPlayer), `wq` (PlayerAbilities)
**Status:** PROVIDED — PARTIAL (ItemInWorldManager not found)
**Canonical name:** GameMode / PlayerAbilities / ItemInWorldManager

---

## Overview

Game mode in 1.0 is not a single enum but a combination of `PlayerAbilities` flags on each player.
Survival and Creative modes differ in these flags. The `ItemInWorldManager` class manages
block-breaking progress for the local player but was not located during this research session.

---

## PlayerAbilities (`wq`)

Stored at `vi.cc` (EntityPlayer field).

### Fields

| Field | Type | Java NBT key | Meaning |
|---|---|---|---|
| `a` | `boolean` | `"invulnerable"` | Player cannot take damage |
| `b` | `boolean` | `"flying"` | Player is currently flying |
| `c` | `boolean` | `"mayfly"` | Player is allowed to fly (Creative) |
| `d` | `boolean` | `"instabuild"` | Instant block breaking (Creative) |

### NBT persistence

```java
wq.a(NBTTagCompound nbt)   // write: nbt.setBoolean("invulnerable", a) etc.
wq.b(NBTTagCompound nbt)   // read:  a = nbt.getBoolean("invulnerable") etc.
```

### Game mode states

| Mode | invulnerable | mayfly | instabuild | flying |
|---|---|---|---|---|
| Survival | false | false | false | false |
| Creative | true | true | true | false (initial) |

---

## EntityPlayer (`vi`)

### Key fields

| Field | Type | Meaning |
|---|---|---|
| `by` | `InventoryPlayer` | Player inventory (hotbar, armour, crafting) |
| `bz` | `Container` | Currently open container GUI (null if none) |
| `L` | `float` = 1.62F | Eye height (camera offset from feet) |
| `cc` | `wq` | PlayerAbilities instance |

### Construction

```java
vi(gd context)
```

Player is constructed with a `gd` parameter (ContainerPlayer — the crafting/inventory container).
`this.bz` is initially set to the `gd` (open inventory container).

### Eye height

Camera is positioned at `entity.y + 1.62F`. This is the exact value used for ray-casting and
view-matrix construction.

---

## Block Breaking — ItemInWorldManager

**Class not located during this research session.**

The ItemInWorldManager manages the progress of block breaking on the server side. Known behaviour
from gameplay observation:

- Tracks `curBlockX`, `curBlockY`, `curBlockZ` — block currently being mined.
- Tracks `breakProgress` (float 0.0–1.0) that increases each tick proportionally to the
  player's mining speed for that block + tool.
- At `breakProgress ≥ 1.0`: break the block, drop items, reset state.
- In Creative mode (`instabuild = true`): block breaks instantly (single tick).
- Client-side: fires WorldEvent 2001 (block crack particles + step sound) during mining.

**Action for next research session:** Search decompiled files for a class containing fields
`curBlockX`, `curBlockY`, `curBlockZ` and a float mining progress field.

---

## Mining Speed Formula (observed behaviour)

```
ticksToBreak = blockHardness × 1.5 / toolEfficiency
```

Where:
- No tool: `toolEfficiency = 1.0`
- Correct tool tier: `toolEfficiency = tier multiplier` (stone=4, iron=6, diamond=8)
- Underwater without Aqua Affinity: `×5` penalty
- In air (not on ground): `×5` penalty

---

## C# Mapping

| Java | C# |
|---|---|
| `wq` | `PlayerAbilities` |
| `wq.a` | `PlayerAbilities.Invulnerable : bool` |
| `wq.b` | `PlayerAbilities.Flying : bool` |
| `wq.c` | `PlayerAbilities.MayFly : bool` |
| `wq.d` | `PlayerAbilities.InstaBuild : bool` |
| `vi` | `EntityPlayer` |
| `vi.L` (1.62F) | `EntityPlayer.EyeHeight = 1.62f` |
| `vi.cc` | `EntityPlayer.Abilities : PlayerAbilities` |
| `vi.by` | `EntityPlayer.Inventory : InventoryPlayer` |
| `vi.bz` | `EntityPlayer.OpenContainer : Container` |

---

## Open Questions

- Java class name for `ItemInWorldManager` (block breaking server-side manager).
- Exact mining speed formula confirmation from source.
- Whether `gd` (ContainerPlayer) is also `vi.by.craftMatrix` or a separate container.
- Spectator mode — does not exist in 1.0; Adventure mode also not present.
