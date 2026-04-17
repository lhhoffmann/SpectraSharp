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

# EnchantingXP Spec
**Source classes:** `fk.java`, `vi.java`, `sy.java`, `rq.java`, `ahk.java`, `ml.java`, `aef.java` + subclasses
**Superclass:** varies per class — see section headers
**Analyst:** lhhoffmann
**Date:** 2026-04-16
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

**Sources:** `fk.java` (EntityXPOrb), `vi.java` (EntityPlayer — XP fields),
`sy.java` (BlockEnchantmentTable), `rq.java` (TileEntityEnchantmentTable),
`ahk.java` (ContainerEnchantment), `ml.java` (EnchantmentHelper),
`aef.java` (Enchantment base + registry),
`ii.java`, `vu.java`, `adz.java`, `ap.java`, `dz.java`, `aie.java`,
`qn.java`, `kr.java`, `gi.java`, `dq.java` (enchantment subclasses),
`vs.java` (EnchantmentData), `dk.java` (ItemStack — enchantment NBT),
`q.java` (EnchantmentTarget enum), `acy.java` (Item base)

---

## 1. EntityXPOrb (`fk.java`, entity ID 2)

Registered in EntityList (`afw.java`) as `"XPOrb"` with ID 2.

### Identity and Size

- Entity class: `fk`
- AABB: 0.5 × 0.5 (both width and height)
- Eye height: `L = N / 2` (half the height)

### Fields

| Field | Meaning | Default |
|-------|---------|---------|
| `a` | age (ticks alive) | 0 |
| `b` | despawn counter | 0 |
| `c` | pickup cooldown | 0 |
| `d` | health | 5 |
| `e` | xpValue | (set at spawn) |

### Physics

- Gravity: `w -= 0.03F` per tick (velocity Y decremented each tick)
- Lava behaviour: bounce upward (standard lava float logic)
- Player attraction: scans for nearest player within 8 blocks;
  attraction force = `(1 − normalizedDist)² × 0.1` per tick toward player

### Despawn

- `b >= 6000` ticks (300 seconds at 20 Hz) → entity removed

### Pickup

Condition: `c == 0 AND player.bM == 0`  
Sequence:
1. `player.bM = 2` (short invulnerability delay)
2. Play sound `"random.orb"`
3. `player.k(e)` — add `e` XP points to player
4. Remove entity

### XP Value Tiers

Static method `h()` returns a size index 0–10 based on `e` (xpValue).  
Thresholds (exclusive lower bound per tier): `[3, 7, 17, 37, 73, 149, 307, 617, 1237, 2477]`

- Tier 0: e < 3
- Tier 1: 3 ≤ e < 7
- Tier 2: 7 ≤ e < 17
- …
- Tier 10: e ≥ 2477

Static method `b(int)` rounds a value DOWN to the nearest tier threshold value.

### NBT

| Key | Tag type | Meaning |
|-----|----------|---------|
| `"Health"` | TAG_Short | entity health |
| `"Age"` | TAG_Short | value of field `b` (despawn counter) |
| `"Value"` | TAG_Short | xpValue (`e`) |

---

## 2. Player XP System (`vi.java`)

### Fields and NBT Keys

| Field | Type | NBT key | Meaning |
|-------|------|---------|---------|
| `cd` | int | `"XpLevel"` | current level |
| `cf` | float | `"XpP"` | level progress (0.0 – 1.0) |
| `ce` | int | `"XpTotal"` | total XP points collected lifetime |

### Adding XP — `k(int value)`

```
bE += value            // (bE = some stat field, presumably totalXp for stats)
cf += value / aN()     // advance progress fractionally
ce += value            // accumulate lifetime total
while cf >= 1.0:
    cf -= 1.0
    cd++               // level up
```

### XP Required per Level — `aN()`

```
required = 7 + (cd * 7 >> 1)
         = 7 + floor(cd * 3.5)
```

Examples:
- Level 0 → 7 XP to reach level 1
- Level 1 → 10 XP
- Level 5 → 24 XP
- Level 10 → 42 XP

### Deducting Levels — `l(int value)`

```
cd -= value   (minimum 0, no underflow)
```

Level progress (`cf`) and total (`ce`) are NOT adjusted — only `cd` changes.

### XP Drop on Death — `b(vi)`

```
drop = cd * 7
drop = min(drop, 100)
```

---

## 3. BlockEnchantmentTable (`sy.java`, block ID 116)

### Block Properties

| Property | Value |
|----------|-------|
| Block ID | 116 |
| Registry name | `"enchantmentTable"` |
| Hardness | 5.0 |
| Blast resistance | 2000 |
| AABB | 0–1 × 0–0.75 × 0–1 (partial height) |
| Opaque | false (`b() = false`) |
| Transparent override | false (`a() = false`) |
| TileEntity class | `rq` (TileEntityEnchantmentTable) |

### Random Tick — Bookshelf Particle

Called via `b(world, x, y, z, Random)`:

1. Scan the 5×5 ring at the table's Y level (excluding inner 3×3):
   - `var6` iterates `x−2 .. x+2`; `var7` iterates `z−2 .. z+2`
   - Inner skip: when `var6 > x−2 AND var6 < x+2 AND var7 == z−1`, jump to `z+2`
2. Per position: roll `nextInt(16) == 0`
3. Check block ID 47 (bookshelf) at `(var6, y, var7)` OR `(var6, y+1, var7)`
4. LOS check: midpoint `((var6−x)/2+x, (var7−z)/2+z)` must be air
5. Spawn `"enchantmenttable"` particle flying from above the table toward the bookshelf

### OnBlockActivated

Calls `player.openEnchantingGui(x, y, z)` (obfuscated: `var5.b(x, y, z)`).  
Returns `true`.

---

## 4. TileEntityEnchantmentTable (`rq.java`)

Handles the floating animated book.

### Fields

| Field | Meaning |
|-------|---------|
| `a` | tick counter (incremented every tick) |
| `b` / `j` | book open amount: current / previous |
| `k` | target open amount |
| `l` | open speed |
| `m` / `n` | book lift: current / previous |
| `o` / `p` | book rotation: current / previous |
| `q` | target yaw angle |

### Tick Behaviour (`b()`)

Each tick:

- Save previous state: `n = m`, `p = o`
- Check for a player within 3.0 blocks of block center
- **Player found:**
  - Compute target angle `q = atan2(dz, dx)` toward player
  - `m += 0.1` (open book)
  - Every `nextInt(40) == 0` OR when `m < 0.5`: pick new random page target
    (`k += nextInt(4) − nextInt(4)`)
- **No player:**
  - `q += 0.02` (slow drift)
  - `m -= 0.1` (close book)
- Clamp `m` to `[0.0, 1.0]`
- Smoothly rotate: `o += (q − o) * 0.4` (with wrap-around to `[−π, π]`)
- Page flip: `l += (k − b) * 0.4`; clamp `l` to `[−0.2, +0.2]`; `b += l`

---

## 5. ContainerEnchantment (`ahk.java`)

### Structure

- Slot 0 (index 0): enchantment input slot — single item ("Enchant" inventory)
- Slots 1–36: player inventory (3×9 + hotbar)
- `c[3]` (int array): level costs for the three enchantment options; synced to all clients
- `b` (long): random seed sent to client for visual display
- `l` (Random): per-container RNG for slot-level calculation

### Item Placement Trigger (`a(de)` called when slot 0 changes)

Fires when the item in slot 0 changes and the item is valid (`dk.t() == true`):
- `dk.t()` = `item.isEnchantable(stack) AND NOT alreadyEnchanted`

**Seed refresh:**
```
b = l.nextLong()
```

**Bookshelf counting** (skipped in creative mode; `world.I == false`):

Loop over the 8 adjacent blocks (var4 ∈ {−1,0,1}, var5 ∈ {−1,0,1}, excluding center):
1. Check `world.isAir(i+var5, j, k+var4)` AND `world.isAir(i+var5, j+1, k+var4)`
   (the adjacent gap must be clear at both y and y+1)
2. If clear, check bookshelves at ±2 distance:
   - Always: `(i+var5*2, j, k+var4*2)` and `(i+var5*2, j+1, k+var4*2)`
   - If diagonal (`var5 ≠ 0` AND `var4 ≠ 0`): additionally check
     `(i+var5*2, j, k+var4)`, `(i+var5*2, j+1, k+var4)`,
     `(i+var5, j, k+var4*2)`, `(i+var5, j+1, k+var4*2)`
3. Each position that equals block ID 47 (bookshelf): `var6++`

**Slot level calculation:**
```
for slot in 0..2:
    c[slot] = ml.a(l, slot, var6, itemStack)
```
(See Section 6 for `ml.a` formula.)

**If item is absent or not enchantable:** `c[0] = c[1] = c[2] = 0`

### Enchanting a Slot (`a(vi player, int slotIndex)`)

1. Guard: `c[slot] > 0 AND item != null AND player.XpLevel >= c[slot]`
2. `enchantments = ml.a(l, item, c[slot])` — weighted random list
3. `player.l(c[slot])` — deduct `c[slot]` levels
4. For each `vs` (EnchantmentData) in list: `item.a(enchantment, level)` — apply to NBT
5. Refresh slot display: call `a(this.a)` again
6. Valid range check: table block must still be ID 116 AND player within 8 blocks
   (`player.distanceSq(x+0.5, y+0.5, z+0.5) <= 64.0`)

---

## 6. EnchantmentHelper (`ml.java`)

### Slot Level Formula — `a(Random rng, int slot, int bookshelfBonus, dk item)`

```
enchantability = item.getItem().c()     // item enchantability value
if enchantability <= 0: return 0

if bookshelfBonus > 30: bookshelfBonus = 30

base = 1 + nextInt(bookshelfBonus / 2 + 1) + nextInt(bookshelfBonus + 1)
noise = nextInt(5) + base

slot 0 (top):    return (noise >> 1) + 1           // ≈ noise / 2
slot 1 (middle): return noise * 2 / 3 + 1
slot 2 (bottom): return noise
```

Key observations:
- Enchantability only gates whether enchanting is possible (> 0) — it does NOT affect the level value
- Bookshelf bonus drives the level range; capped at 30
- Slot 0 always yields the lowest level, slot 2 the highest
- RNG state from `l` continues after `b = l.nextLong()`, so seed and slot levels share state

### Enchantment Selection — `a(Random rng, dk item, int power)`

```
enchantability = item.getItem().c()
if enchantability <= 0: return null

base = 1 + nextInt(enchantability / 2 + 1) + nextInt(enchantability / 2 + 1)
adjusted = base + power
float fuzz = (nextFloat() + nextFloat() - 1.0F) * 0.25F
finalPower = (int)((float)adjusted * (1.0F + fuzz) + 0.5F)

candidates = all enchantments where finalPower ∈ [enchant.minPower(lvl), enchant.maxPower(lvl)]
             AND enchantment.canApplyTo(item)

if candidates empty: return null

enchantments = [ weightedRandom(candidates) ]

// try to add more enchantments:
threshold = finalPower / 2
while nextInt(50) <= threshold:
    threshold >>= 1
    remove from candidates any incompatible with already-chosen enchantments
    if candidates empty: break
    enchantments.add( weightedRandom(candidates) )

return enchantments
```

---

## 7. Enchantment Catalog (all 19 in 1.0)

### Base class `aef`

Default formulae (used when subclass does not override):
- `a(L)` = min power: `1 + L × 10`
- `b(L)` = max power: `a(L) + 5`
- `d()` = min level: 1
- `a()` = max level: 1
- Weight (`c()`): set per-instance at construction

Note: several subclasses call `super.a(var1) + 50` for their max power, which evaluates to
`1 + L×10 + 50` regardless of the subclass's own `a()` formula.

### Protection Group (`ii.java`) — target: armor (boots for FeatherFalling)

All have `a() = 4` (max level 4).

| ID | Field | Name | Weight | Target | Min Power (L) | Max Power (L) |
|----|-------|------|--------|--------|--------------|--------------|
| 0 | `aef.c` | Protection | 10 | armor | `1 + (L−1)×16` | min + 20 |
| 1 | `aef.d` | FireProtection | 5 | armor | `10 + (L−1)×8` | min + 12 |
| 2 | `aef.e` | FeatherFalling | 5 | boots | `5 + (L−1)×6` | min + 10 |
| 3 | `aef.f` | BlastProtection | 2 | armor | `5 + (L−1)×8` | min + 12 |
| 4 | `aef.g` | ProjectileProtection | 5 | armor | `3 + (L−1)×6` | min + 15 |

Mutual exclusivity within group: any two Protection-type enchantments block each other,
EXCEPT FeatherFalling (subtype 2) can coexist with any non-FeatherFalling protection.

### Helmet Enchantments

| ID | Field | Class | Name | Weight | Max Lvl | Min Power (L) | Max Power (L) |
|----|-------|-------|------|--------|---------|--------------|--------------|
| 5 | `aef.h` | `vu` | Respiration | 2 | 3 | `10 × L` | `10×L + 30` |
| 6 | `aef.i` | `adz` | AquaAffinity | 2 | 1 | `1` | `41` |

### Sword Damage Group (`ap.java`) — target: sword

All have `a() = 5` (max level 5).

| ID | Field | Name | Weight | Min Power (L) | Max Power (L) |
|----|-------|------|--------|--------------|--------------|
| 16 | `aef.j` | Sharpness | 10 | `1 + (L−1)×16` | min + 20 |
| 17 | `aef.k` | Smite | 5 | `5 + (L−1)×8` | min + 20 |
| 18 | `aef.l` | BaneOfArthropods | 5 | `5 + (L−1)×8` | min + 20 |

All three are mutually exclusive (any two block each other).

### Sword Utility Enchantments

| ID | Field | Class | Name | Weight | Max Lvl | Min Power (L) | Max Power (L) |
|----|-------|-------|------|--------|---------|--------------|--------------|
| 19 | `aef.m` | `dz` | Knockback | 5 | 2 | `5 + 20×(L−1)` | `1 + L×10 + 50` |
| 20 | `aef.n` | `aie` | FireAspect | 2 | 2 | `10 + 20×(L−1)` | `1 + L×10 + 50` |
| 21 | `aef.o` | `qn(g)` | Looting | 2 | 3 | `20 + (L−1)×12` | `1 + L×10 + 50` |

### Tool Enchantments

| ID | Field | Class | Name | Weight | Max Lvl | Min Power (L) | Max Power (L) |
|----|-------|-------|------|--------|---------|--------------|--------------|
| 32 | `aef.p` | `kr` | Efficiency | 10 | 5 | `1 + 15×(L−1)` | `1 + L×10 + 50` |
| 33 | `aef.q` | `gi` | SilkTouch | 1 | 1 | `25` | `1×10+1 + 50 = 61` |
| 34 | `aef.r` | `dq` | Unbreaking | 5 | 3 | `5 + (L−1)×10` | `1 + L×10 + 50` |
| 35 | `aef.s` | `qn(h)` | Fortune | 2 | 3 | `20 + (L−1)×12` | `1 + L×10 + 50` |

**SilkTouch ↔ Fortune mutual exclusivity:**
- `gi.a(aef other)`: returns false if `other.t == aef.s.t` (Fortune ID 35)
- `qn.a(aef other)` (Fortune): returns false if `other.t == aef.q.t` (SilkTouch ID 33)

### No Bow Enchantments in 1.0

Power, Punch, Flame, and Infinity were not added until Beta 1.8+.
There are **zero** bow enchantments in the 1.0 registry.

---

## 8. Enchantment NBT Format (`dk.java`)

Applied via `dk.a(aef enchantment, int level)`:

```
itemStack.nbt["ench"] = TAG_List {
    TAG_Compound { "id": TAG_Short(enchantment.t), "lvl": TAG_Short((byte)level) }
    ...
}
```

Key detail: `lvl` is stored as `short((byte)level)` — the level is first cast to byte (truncates
values > 127 to negative), then widened to short. At normal enchantment levels this has no effect.

`dk.t()` = item can be enchanted in the table:
- `item.isEnchantable(stack)` returns true (item type allows enchanting)
- AND the item does NOT already have an `"ench"` NBT tag (`!dk.u()`)

An item can only be enchanted once via the table.

---

## 9. Damage Application (from `ii.java` and `ap.java`)

### Protection Damage Reduction — `a(int level, pm damageSource)`

Per protection type, returns damage reduction points (applied against incoming damage):

| Type | Condition | Formula |
|------|-----------|---------|
| Protection (0) | any non-fire damage | `(6 + level²) / 2` |
| FireProtection (1) | fire damage (`pm.k()`) | `(6 + level²) / 2` |
| FeatherFalling (2) | fall damage (`pm.h`) | `(6 + level²) / 2 × 2` |
| BlastProtection (3) | explosion damage (`pm.k` explosive) | `(6 + level²) / 2` |
| ProjectileProtection (4) | projectile damage (`pm.b()`) | `(6 + level²) / 2` |

Returns 0 if damage source does not match or if `pm.f()` is true (fire-immune entity).

### Sharpness/Smite/BaneOfArthropods — `a(int level, nq entity)`

| Type | Condition | Bonus damage |
|------|-----------|-------------|
| Sharpness (0) | always | `level × 3` |
| Smite (1) | entity `el.b` (undead) | `level × 4` |
| BaneOfArthropods (2) | entity `el.c` (arthropod) | `level × 4` |
