# EntityPlayer Spec
Source: `vi.java` (499+ lines); extends `nq` (LivingEntity)
Type: Abstract class definition

---

## 1. Overview

`vi` is the abstract `EntityPlayer` base class. It extends `nq` (LivingEntity).
Concrete subclasses: server-side player and client-side player.
This spec covers all fields and logic present in `vi` itself.

---

## 2. Key Fields

| Field | Type | Default | Meaning |
|---|---|---|---|
| `by` | `x` | `new x(this)` | Inventory (InventoryPlayer) |
| `bz` | `pj` | `new gd(by, !world.I)` | Primary container (inventory screen) |
| `bA` | `pj` | `= bz` | Currently open container |
| `bB` | `eq` | new | Food stats |
| `bC` | `int` | 0 | Cooldown counter |
| `bD` | `byte` | 0 | Unknown flag |
| `bE` | `int` | 0 | XP score (total damage dealt etc.) |
| `bH` | `boolean` | false | `isUsingItem` |
| `bI` | `int` | 0 | `itemInUseCount` |
| `bJ` | `String` | — | Player name / username |
| `bK` | `int` | — | Dimension |
| `bL` | `String` | — | Cloak URL |
| `bM` | `int` | 0 | Sleeping timer (counts up to `bY=20` while sleeping) |
| `bN/bO/bP` | `double` | — | Prev tick position X/Y/Z (for camera interpolation) |
| `bQ/bR/bS` | `double` | — | Camera-interpolation target X/Y/Z |
| `bT` | `boolean` | — | isSleeping |
| `bU` | `dh` | — | Sleep bed position |
| `bY` | `int` | 20 | Sleep ticks required |
| `bZ` | `boolean` | false | Sneaking flag (client only) |
| `cg` | `float` | 0.1F | `speedInAir` |
| `ch` | `float` | 0.02F | `jumpStrength` in air |
| `ci` | `ael` | null | `fishEntity` (fishing rod bobber) |
| `d` (private) | `dk` | null | Currently using ItemStack |
| `e` (private) | `int` | 0 | `itemInUseTicks` countdown |
| `a` (private) | `int` | 0 | Swim/drown animation counter |

---

## 3. Inherited Fields (from nq / LivingEntity)

- `aM` — health (int, max 20)
- `aa` — maxHealth set to **20** in constructor
- `aD` — entity type string set to `"humanoid"` in constructor  
- `aA` — skin texture path set to `"/mob/char.png"` in constructor
- `aC` — render yaw set to 180.0F in constructor
- `ag` — DataWatcher

---

## 4. Constructor

```java
public vi(ry world) {
    super(world);
    this.by  = new x(this);
    this.bz  = new gd(this.by, !world.I);   // player inventory screen
    this.bA  = this.bz;
    this.L   = 1.62F;          // eye height
    // spawn at world spawn point +0.5 center, +1 Y
    this.aD  = "humanoid";
    this.aC  = 180.0F;
    this.aa  = 20;             // maxHealth
    this.aA  = "/mob/char.png";
}
```

---

## 5. `u()` — respawn / reset

Called on respawn. Resets:
```java
this.L = 1.62F;
this.a(0.6F, 1.8F);    // setSize: width=0.6, height=1.8
super.u();
this.h(this.f_());      // setHealth(maxHealth=20)
this.aS = 0;            // deathTimer reset
```

So entity size is **0.6 wide × 1.8 tall**, eye height **L = 1.62F** above feet.

---

## 6. `f_()` — maxHealth

```java
@Override
public int f_() { return 20; }
```

Player maximum health is always 20 (10 hearts).

---

## 7. DataWatcher Indices (beyond LivingEntity base)

| Index | Type | Default | Meaning |
|---|---|---|---|
| 16 | byte | 0 | Unknown (player-specific flag) |
| 17 | byte | 0 | Unknown (player-specific flag) |

Registered in `b()` (entity init):
```java
this.ag.a(16, (byte)0);
this.ag.a(17, (byte)0);
```

---

## 8. `a(yy block)` — getMiningSpeed

Returns the effective dig speed of the player against a block:

```java
public float a(yy block) {
    float speed = this.by.a(block);         // tool speed from inventory
    float adjusted = speed;
    int eff = ml.b(this.by);                // Efficiency enchant level on held tool
    if (eff > 0 && this.by.b(block)) {      // b(block) = canHarvest
        adjusted = speed + (float)(eff * eff + 1);
    }
    if (this.a(abg.e)) {                    // has Haste effect
        adjusted *= 1.0F + (float)(hasteLevel + 1) * 0.2F;
    }
    if (this.a(abg.f)) {                    // has Mining Fatigue effect
        adjusted *= 1.0F - (float)(miningFatigueLevel + 1) * 0.2F;
    }
    if (this.a(p.g) && !ml.g(this.by)) {   // in water and not holding waterproof tool
        adjusted /= 5.0F;
    }
    if (!this.D) {                           // not on ground
        adjusted /= 5.0F;
    }
    return adjusted;
}
```

- `ml.b(inventory)` — gets Efficiency enchant level of held item
- `ml.g(inventory)` — returns true if held item has Aqua Affinity
- `abg.e` — Haste potion effect; `abg.f` — Mining Fatigue

---

## 9. `a(dk, boolean)` — dropItem

Spawns an EntityItem (`ih`) at player position:

```java
public void a(dk stack, boolean randomDirection) {
    ih drop = new ih(world, s, t - 0.3F + E(), u, stack);
    drop.c = 40;   // pickup delay = 40 ticks (2 seconds)
    // if randomDirection: random horizontal velocity, w=0.2F
    // else: throw forward in look direction at 0.3F speed
    world.a(drop);
}
```

- `E()` — eye height offset (= L = 1.62F), so item spawns at approximately eye level.
- Pickup delay `c = 40` ticks prevents the player immediately picking up what they dropped.

---

## 10. Death Behaviour

`a(pm cause)` — called on death:
```java
this.a(0.2F, 0.2F);            // shrink AABB to 0.2×0.2
this.d(s, t, u);               // teleport in place (AABB update)
this.w = 0.1F;                 // small upward velocity
// special: if player name is "Notch", spawn apple (acy.i)
this.by.g();                   // drop all inventory items
// throw velocity in look direction
this.L = 0.1F;                 // eye height shrinks to 0.1F
```

`by.g()` iterates all inventory slots and calls `a(dk, false)` for each non-null stack.

---

## 11. Breath / Drowning

From `ai()`:
- Player can hold breath (`ag()` = 0) or be using an item (`ar()`)
- Food/breath handled in `c()` — regen 1 HP every `20*12 = 240` ticks if food is full (v=0) and health < max

---

## 12. Camera Interpolation (client)

`bN/bO/bP` store previous-tick position; `bQ/bR/bS` track smoothed camera position:
```java
bQ += (s - bQ) * 0.25;
bR += (t - bR) * 0.25;
bS += (u - bS) * 0.25;
```
If delta > 10 blocks, snap immediately (no interpolation).

---

*Spec written by Analyst AI from `vi.java`. No C# implementation consulted.*
