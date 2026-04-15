<!--
  SpectraSharp Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraSharp Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# ItemTool / ItemSword / ItemArmor Spec
Source classes: `ads.java` (ItemTool), `nu.java` (EnumToolMaterial), `zp.java` (ItemSword),
`adb.java` (ItemSpade), `zu.java` (ItemPickaxe), `ago.java` (ItemAxe), `wr.java` (ItemHoe),
`agi.java` (ItemArmor), `dj.java` (EnumArmorMaterial)

Supporting methods in: `vi.java` (EntityPlayer.getMiningSpeed), `dk.java` (ItemStack.damageItem),
`x.java` (InventoryPlayer.getStrVsBlock / canHarvestBlock)

Superclass: `acy` (= `Item`)

---

## 1. Purpose

This spec covers all tool and armor items. Tools provide:
- A **mining speed multiplier** for specific block types (via `getStrVsBlock`)
- A **harvest capability** flag for blocks that require a minimum tool tier (via `canHarvestBlock`)
- **Weapon damage** — an integer added to the base attack damage
- **Durability** — a limited number of uses before the item breaks

Armor provides a **protection value** per slot (helmet/chest/legs/boots) and a **durability** pool.

---

## 2. EnumToolMaterial (`nu`)

Five constants; fields:
- `f` = harvestLevel (int)
- `g` = maxUses / durability (int)
- `h` = efficiencyOnProperMaterial (float)
- `i` = damageVsEntity (int) — added to weapon base damage
- `j` = enchantability (int)

| Constant | Name (human) | harvestLevel (f) | maxUses (g) | efficiency (h) | damageBonus (i) | enchantability (j) |
|---|---|---|---|---|---|---|
| `nu.a` | WOOD | 0 | 59 | 2.0F | 0 | 15 |
| `nu.b` | STONE | 1 | 131 | 4.0F | 1 | 5 |
| `nu.c` | IRON | 2 | 250 | 6.0F | 2 | 14 |
| `nu.d` | DIAMOND | 3 | 1561 | 8.0F | 3 | 10 |
| `nu.e` | GOLD | 0 | 32 | 12.0F | 0 | 22 |

Accessors:
- `a()` = getMaxUses() → `g`
- `b()` = getEfficiencyOnProperMaterial() → `h`
- `c()` = getDamageVsEntity() → `i`
- `d()` = getHarvestLevel() → `f`
- `e()` = getEnchantability() → `j`

---

## 3. ItemTool (`ads`) — Base Class

Extends `acy` (Item).

### 3.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `bR` | `yy[]` | set in constructor | blocks this tool mines at efficiency speed |
| `a` | `float` | `4.0F` (overwritten) | mining efficiency on effective blocks |
| `bS` | `int` | — | weapon damage = `baseDamage + material.damageVsEntity` |
| `b` | `nu` | — | tool material |

### 3.2 Constructor

```
ads(int itemId, int baseDamage, nu material, yy[] blocksArray)
```

Steps:
1. Call `super(itemId)` → Item base
2. `this.b = material`
3. `this.bR = blocksArray`
4. `this.bN = 1` — maxStackSize = 1 (tools never stack)
5. `this.i(material.a())` — set item max durability to `material.maxUses`
6. `this.a = material.b()` — set efficiency to `material.efficiencyOnProperMaterial`
7. `this.bS = baseDamage + material.c()` — weapon damage = baseDamage + damageVsEntity

### 3.3 Methods

#### `a(dk, yy)` — getStrVsBlock (obfuscated: `a`)
**Called by:** InventoryPlayer.getStrVsBlock → mining speed formula  
**Parameters:** ItemStack, Block  
**Returns:** float mining speed multiplier  

1. Iterate `bR` array
2. If any element `== var2` (exact reference equality to Block singleton): return `this.a` (efficiency)
3. If no match: return `1.0F`

#### `a(dk, nq, nq)` — hitEntity
**Called by:** `dk.a(nq, vi)` = ItemStack after entity hit  
**Parameters:** ItemStack, attacker entity, target entity  
**Returns:** `true`  
**Side effects:** `var1.a(2, var3)` — decrement item durability by **2** (mining tool hits cost twice)

#### `a(dk, int, int, int, int, nq)` — onBlockDestroyed
**Called by:** `dk.a(x,y,z,face,vi)` = ItemStack after block broken  
**Parameters:** ItemStack, x, y, z, face, entity  
**Returns:** `true`  
**Side effects:** `var1.a(1, var6)` — decrement item durability by **1**

#### `a(ia)` — getDamage
**Called by:** `dk.a(ia)` = ItemStack.getDamage  
**Parameters:** Entity (unused)  
**Returns:** `this.bS` = baseDamage + material.damageVsEntity

#### `a()` — isItemTool (no-arg override)
**Returns:** `true`

#### `c()` — getEnchantability
**Returns:** `this.b.e()` = material enchantability

---

## 4. ItemSpade / Shovel (`adb`)

Extends `ads`. Constructor: `super(itemId, 1, material, bR)` — baseDamage = **1**.

### 4.1 Effective Blocks (`bR`)

| yy field | Block name | ID |
|---|---|---|
| `yy.u` | grass | 2 |
| `yy.v` | dirt | 3 |
| `yy.E` | sand | 12 |
| `yy.F` | gravel | 13 |
| `yy.aS` | snow_layer | 78 |
| `yy.aU` | snow_block | 80 |
| `yy.aW` | clay | 82 |
| `yy.aA` | farmland | 60 |
| `yy.bc` | soul_sand | 88 |
| `yy.by` | mycelium | 110 |

### 4.2 canHarvestBlock (`a(yy)`)

Returns `true` only if block == `yy.aS` (snow_layer) OR block == `yy.aU` (snow_block).

**Significance:** Shovel is effective (fast) against all 10 blocks above, but only **harvests** snow — i.e., snow can only drop when broken with a shovel. All other shovel targets can be broken by any tool (just slower).

---

## 5. ItemPickaxe (`zu`)

Extends `ads`. Constructor: `super(itemId, 2, material, bR)` — baseDamage = **2**.

### 5.1 Effective Blocks (`bR`)

| yy field | Block name | ID | Notes |
|---|---|---|---|
| `yy.w` | stonebrick | 4 | material=rock |
| `yy.aj` | stoneSlab_double | 43 | material=rock |
| `yy.ak` | stoneSlab | 44 | material=rock |
| `yy.t` | stone | 1 | material=rock |
| `yy.Q` | sandstone | 24 | material=rock |
| `yy.ao` | mossyCobblestone | 48 | material=rock |
| `yy.H` | oreIron | 15 | material=rock |
| `yy.ai` | blockIron | 42 | material=metal |
| `yy.I` | oreCoal | 16 | material=rock |
| `yy.ah` | blockGold | 41 | material=metal |
| `yy.G` | oreGold | 14 | material=rock |
| `yy.aw` | oreDiamond | 56 | material=rock |
| `yy.ax` | blockDiamond | 57 | material=metal |
| `yy.aT` | ice | 79 | material=ice |
| `yy.bb` | hellrock | 87 | material=rock |
| `yy.N` | oreLapis | 21 | material=rock |
| `yy.O` | blockLapis | 22 | material=rock |
| `yy.aN` | oreRedstone | 73 | material=rock |
| `yy.aO` | oreRedstone_lit | 74 | material=rock |
| `yy.aG` | rail | 66 | material=metal |
| `yy.U` | detectorRail | 28 | material=metal |
| `yy.T` | goldenRail | 27 | material=metal |

### 5.2 canHarvestBlock (`a(yy)`)

Full harvest-level tier-gate:

```
if block == yy.ap (obsidian, ID 49):
    return material.harvestLevel == 3  (diamond only)
else if block == yy.ax (blockDiamond) OR yy.aw (oreDiamond):
    return material.harvestLevel >= 2  (iron+)
else if block == yy.ah (blockGold) OR yy.G (oreGold):
    return material.harvestLevel >= 2  (iron+)
else if block == yy.ai (blockIron) OR yy.H (oreIron):
    return material.harvestLevel >= 1  (stone+)
else if block == yy.O (blockLapis) OR yy.N (oreLapis):
    return material.harvestLevel >= 1  (stone+)
else if block == yy.aN (oreRedstone) OR yy.aO (oreRedstone_lit):
    return material.harvestLevel >= 2  (iron+)
else:
    return block.bZ == p.e (rock material) OR block.bZ == p.f (metal material)
```

**Note:** The general-case `p.e || p.f` means iron/gold/diamond/coal/redstone ore, stone variants, and metal blocks all count as harvestable by any pickaxe tier — but the tier-gates above override for specific high-value ores.

---

## 6. ItemAxe (`ago`)

Extends `ads`. Constructor: `super(itemId, 3, material, bR)` — baseDamage = **3**.

### 6.1 Effective Blocks (`bR`)

| yy field | Block name | ID |
|---|---|---|
| `yy.x` | planks | 5 |
| `yy.an` | bookshelf | 47 |
| `yy.J` | log | 17 |
| `yy.au` | chest | 54 |
| `yy.aj` | stoneSlab_double | 43 |
| `yy.ak` | stoneSlab | 44 |
| `yy.ba` | pumpkin | 86 |
| `yy.bf` | lit_pumpkin | 91 |

### 6.2 getStrVsBlock override (`a(dk, yy)`)

Overrides ads.a:
```
if block != null AND block.bZ == p.d (wood material):
    return this.a  (efficiency)
else:
    return super.a(var1, var2)  (base ItemTool check against bR array)
```

**Significance:** Axe mines ANY wood-material block at efficiency speed, not just those in `bR`. The `bR` list is a fallback for blocks with wood-like behaviour but not wood material.

---

## 7. ItemHoe (`wr`)

Extends `acy` (Item directly, NOT `ads`). Has no weapon damage, no effective blocks array.

### 7.1 Fields

None beyond `acy` base. Sets `bN = 1` (maxStackSize=1). Sets max durability = `material.a()` = material.maxUses.

### 7.2 Constructor

```
wr(int itemId, nu material)
```

Steps:
1. `super(itemId)`
2. `this.bN = 1`
3. `this.i(material.a())` — set max durability = material.maxUses

**No baseDamage, no efficiency, no effective blocks.** Hoe durability matches its tool material but deals no bonus weapon damage.

### 7.3 onItemUse (`a(dk, vi, ry, int, int, int, int)`)

**Parameters:** ItemStack, player, world, x, y, z, face  
**Returns:** `true` if tilled; `false` otherwise

Steps:
1. If `!player.e(x, y, z)` (player cannot reach block): return false
2. Get blockId at (x, y, z) = `var8`
3. Get blockId at (x, y+1, z) = `var9`
4. Check tilling eligibility:
   ```
   canTill = (face == 1 AND blockAbove == 0 AND blockId == yy.u.bM) OR blockId == yy.v.bM
   ```
   - `face == 1` = top face
   - `blockAbove == 0` = air above  
   - `yy.u.bM` = grass (ID 2)
   - `yy.v.bM` = dirt (ID 3)
   - Dirt can be tilled regardless of face or block above. Grass requires top-face click with air above.
5. If canTill is false: return false
6. Play step sound: `world.a(x+0.5, y+0.5, z+0.5, block.bX.d(), (block.bX.b()+1.0)/2.0, block.bX.c()*0.8F)`
   where `block = yy.aA` (farmland)
7. If not client-side (`!world.I`):
   - `world.g(x, y, z, yy.aA.bM)` — set block to farmland (ID 60), no block update notification
   - `var1.a(1, player)` — decrement durability by **1**
8. Return true

**Quirk:** Step sound is always played (even client-side), but block change only happens server-side.

### 7.4 isItemTool (`a()`)
Returns `true`.

---

## 8. ItemSword (`zp`)

Extends `acy` (Item directly, NOT `ads`).

### 8.1 Fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `int` | weapon damage = `4 + material.damageVsEntity` |
| `b` | `nu` | tool material |

### 8.2 Constructor

```
zp(int itemId, nu material)
```

Steps:
1. `super(itemId)`
2. `this.b = material`
3. `this.bN = 1`
4. `this.i(material.a())` — max durability = material.maxUses
5. `this.a = 4 + material.c()` — weapon damage = 4 + material.damageVsEntity

**Sword damage per material:**
| Material | Formula | Damage |
|---|---|---|
| WOOD | 4 + 0 | 4 |
| STONE | 4 + 1 | 5 |
| IRON | 4 + 2 | 6 |
| DIAMOND | 4 + 3 | 7 |
| GOLD | 4 + 0 | 4 |

### 8.3 Methods

#### `a(dk, yy)` — getStrVsBlock
```
if block.bM == yy.W.bM (cobweb, ID 30):
    return 15.0F
else:
    return 1.5F
```
Sword mines cobweb at 15× speed, all other blocks at 1.5× (still slower than a specific tool, but faster than bare fist at 1.0×).

#### `a(dk, nq, nq)` — hitEntity
`var1.a(1, var3)` — decrement durability by **1** (note: less than tools which cost 2)  
Returns `true`.

#### `a(dk, int, int, int, int, nq)` — onBlockDestroyed
`var1.a(2, var6)` — decrement durability by **2**  
Returns `true`.

#### `a(ia)` — getDamage
Returns `this.a` = 4 + material.damageVsEntity.

#### `a()` — isItemTool
Returns `true`.

#### `c(dk)` — getItemUseAction
Returns `ps.d` = blocking action (shield/block stance).

#### `b(dk)` — getMaxItemUseDuration
Returns **72000** ticks.

#### `c(dk, ry, vi)` — onItemRightClick
Calls `player.c(stack, 72000)` — start using item (blocking stance) for up to 72000 ticks.

#### `a(yy)` — canHarvestBlock
Returns `true` only if `block.bM == yy.W.bM` (cobweb).

#### `c()` — getEnchantability
Returns `this.b.e()` = material enchantability.

---

## 9. EnumArmorMaterial (`dj`)

Five constants; fields:
- `f` = durabilityFactor (int) — multiplied by slot base durability
- `g` = protectionAmounts (int[4]) — armor protection per slot [helmet, chest, legs, boots]
- `h` = enchantability (int)

| Constant | Name | durabilityFactor (f) | helmet protect | chest protect | legs protect | boots protect | enchantability (h) |
|---|---|---|---|---|---|---|---|
| `dj.a` | LEATHER | 5 | 1 | 3 | 2 | 1 | 15 |
| `dj.b` | CHAIN | 15 | 2 | 5 | 4 | 1 | 12 |
| `dj.c` | IRON | 15 | 2 | 6 | 5 | 2 | 9 |
| `dj.d` | GOLD | 7 | 2 | 5 | 3 | 1 | 25 |
| `dj.e` | DIAMOND | 33 | 3 | 8 | 6 | 3 | 10 |

**Methods:**
- `a(int slotType)` = getDurability(slot): `agi.o()[slotType] * this.f`
- `b(int slotType)` = getProtection(slot): `this.g[slotType]`
- `a()` = getEnchantability(): `this.h`

---

## 10. ItemArmor (`agi`)

Extends `acy` (Item).

### 10.1 Static Field

```
private static final int[] bS = {11, 16, 15, 13}
```

Base durability per slot type (helmet=0, chest=1, legs=2, boots=3).  
Accessed externally via static method `agi.o()`.

### 10.2 Fields

| Field (obf) | Type | Semantics |
|---|---|---|
| `a` | `int` | armorType (0=helmet, 1=chest, 2=legs, 3=boots) |
| `b` | `int` | protection value = `material.b(armorType)` |
| `bR` | `int` | armorSlot/material tier index |
| `bT` | `dj` | armor material |

### 10.3 Constructor

```
agi(int itemId, dj material, int armorSlot, int armorType)
```

Steps:
1. `super(itemId)`
2. `this.bT = material`
3. `this.a = armorType` (0-3)
4. `this.bR = armorSlot` (material tier index)
5. `this.b = material.b(armorType)` — protection value from material array
6. `this.i(material.a(armorType))` — max durability = `bS[armorType] * material.durabilityFactor`
7. `this.bN = 1` — no stacking

### 10.4 Durability Table

Formula: `bS[slot] * dj.f`

| Slot | bS base | Leather | Chain | Iron | Gold | Diamond |
|---|---|---|---|---|---|---|
| Helmet (0) | 11 | 55 | 165 | 165 | 77 | 363 |
| Chest (1) | 16 | 80 | 240 | 240 | 112 | 528 |
| Legs (2) | 15 | 75 | 225 | 225 | 105 | 495 |
| Boots (3) | 13 | 65 | 195 | 195 | 91 | 429 |

### 10.5 getEnchantability (`c()`)
Returns `this.bT.a()` = armor material enchantability.

---

## 11. ItemStack Durability Decrement (`dk.a(int, nq)`)

This is the method tools call to damage themselves.

### Full Logic

```
a(int amount, nq entity):
```

1. If item is NOT damageable (`!this.e()`): return immediately (do nothing)
2. If amount > 0 AND entity is a player (`instanceof vi`):
   - Get Unbreaking level: `var3 = ml.c(player.inventory)` — enchantment level (0 if none)
   - If `var3 > 0`:
     - Roll `entity.o.w.nextInt(var3 + 1)` — world RNG
     - If roll > 0: return (skip this durability point — **Unbreaking protection**)
3. `this.e += amount` — add to current damage
4. If `this.e > this.j()` (current damage exceeds max durability):
   - Call `entity.b(this)` = entity.renderBrokenItem(stack) — plays breaking sound / effect
   - If entity is a player:
     - `player.a(ny.F[this.c], 1)` = add stat BreakItem[itemId]
   - Decrement stack size: `this.a--`
   - If `this.a < 0`: set `this.a = 0`
   - Reset damage: `this.e = 0`

**Notes:**
- `this.e()` = item has durability (checks `bN == 1` i.e. not stackable AND is not indestructible)
- `this.j()` = getMaxDamage = the value set by `acy.i(int)` in constructor
- After decrement, if `this.a < 0`, it stays at 0 but the item slot is now empty (checked externally)
- The Unbreaking enchant uses the world RNG (`entity.o.w.nextInt`), not a separate RNG

---

## 12. InventoryPlayer Block Mining Helpers

### `x.a(yy)` — getStrVsBlock
```
float var2 = 1.0F
if held item stack != null:
    var2 *= stack.a(block)   → item.getStrVsBlock(stack, block)
return var2
```
Returns 1.0F for bare fist (no item).

### `x.b(yy)` — isItemEffectiveAgainst (canHarvestBlock)
```
if block.bZ.k():    (material has "no tool required" flag)
    return true
else:
    dk var2 = this.d(this.c)   (held stack)
    return var2 != null ? var2.b(block) : false
        where dk.b(yy) = item.canHarvestBlock(block) = item.a(yy)
```

---

## 13. EntityPlayer Mining Speed Formula (`vi.a(yy)`)

```
a(yy block):
```

1. `var2 = this.by.a(block)` = `InventoryPlayer.getStrVsBlock(block)` → held item's multiplier (1.0F bare fist)
2. `var3 = var2`
3. `var4 = ml.b(this.by)` = Efficiency enchantment level from inventory (0 if none)
4. If `var4 > 0` AND `this.by.b(block)` (isItemEffectiveAgainst):
   - `var3 += (var4 * var4 + 1)` — Efficiency bonus; level-squared + 1
5. If player has Haste effect (`abg.e`):
   - `amplifier = this.b(abg.e).c()` = effect amplifier (0-indexed)
   - `var3 *= 1.0F + (amplifier + 1) * 0.2F`
6. If player has Mining Fatigue effect (`abg.f`):
   - `amplifier = this.b(abg.f).c()`
   - `var3 *= 1.0F - (amplifier + 1) * 0.2F`
7. If player is in water (`this.a(p.g)` = player overlaps water material) AND does NOT have Aqua Affinity enchant (`!ml.g(this.by)`):
   - `var3 /= 5.0F`
8. If player is NOT on ground (`!this.D`):
   - `var3 /= 5.0F`
9. Return `var3`

**Effect:** Steps 7 and 8 are multiplicative with step 6, so swimming+floating in water = ×1/25 speed.

---

## 14. Item Registry Assignments (`acy` static fields)

All item IDs below use offset `256 + id`, so item ID field `bM = 256 + id`.

| acy field | Item (human) | itemId | Texture (col, row) | Tool class | Material |
|---|---|---|---|---|---|
| `acy.e` | Iron Shovel | 0 | (2,5) | adb | nu.c (IRON) |
| `acy.f` | Iron Pickaxe | 1 | (2,6) | zu | nu.c |
| `acy.g` | Iron Axe | 2 | (2,7) | ago | nu.c |
| `acy.p` | Iron Sword | 11 | (2,4) | zp | nu.c |
| `acy.q` | Wood Sword | 12 | (0,4) | zp | nu.a (WOOD) |
| `acy.r` | Wood Shovel | 13 | (0,5) | adb | nu.a |
| `acy.s` | Wood Pickaxe | 14 | (0,6) | zu | nu.a |
| `acy.t` | Wood Axe | 15 | (0,7) | ago | nu.a |
| `acy.u` | Stone Sword | 16 | (1,4) | zp | nu.b (STONE) |
| `acy.v` | Stone Shovel | 17 | (1,5) | adb | nu.b |
| `acy.w` | Stone Pickaxe | 18 | (1,6) | zu | nu.b |
| `acy.x` | Stone Axe | 19 | (1,7) | ago | nu.b |
| `acy.y` | Diamond Sword | 20 | (3,4) | zp | nu.d (DIAMOND) |
| `acy.z` | Diamond Shovel | 21 | (3,5) | adb | nu.d |
| `acy.A` | Diamond Pickaxe | 22 | (3,6) | zu | nu.d |
| `acy.B` | Diamond Axe | 23 | (3,7) | ago | nu.d |
| `acy.F` | Gold Sword | 27 | (4,4) | zp | nu.e (GOLD) |
| `acy.G` | Gold Shovel | 28 | (4,5) | adb | nu.e |
| `acy.H` | Gold Pickaxe | 29 | (4,6) | zu | nu.e |
| `acy.I` | Gold Axe | 30 | (4,7) | ago | nu.e |
| `acy.M` | Wood Hoe | 34 | (0,8) | wr | nu.a |
| `acy.N` | Stone Hoe | 35 | (1,8) | wr | nu.b |
| `acy.O` | Iron Hoe | 36 | (2,8) | wr | nu.c |
| `acy.P` | Diamond Hoe | 37 | (3,8) | wr | nu.d |
| `acy.Q` | Gold Hoe | 38 | (4,8) | wr | nu.e |

**Armor items:**

| acy field | Item (human) | itemId | Texture (col, row) | Material | Slot |
|---|---|---|---|---|---|
| `acy.U` | Leather Helmet | 42 | (0,0) | dj.a (LEATHER) | 0 |
| `acy.V` | Leather Chestplate | 43 | (0,1) | dj.a | 1 |
| `acy.W` | Leather Leggings | 44 | (0,2) | dj.a | 2 |
| `acy.X` | Leather Boots | 45 | (0,3) | dj.a | 3 |
| `acy.Y` | Chain Helmet | 46 | (1,0) | dj.b (CHAIN) | 0 |
| `acy.Z` | Chain Chestplate | 47 | (1,1) | dj.b | 1 |
| `acy.aa` | Chain Leggings | 48 | (1,2) | dj.b | 2 |
| `acy.ab` | Chain Boots | 49 | (1,3) | dj.b | 3 |
| `acy.ac` | Iron Helmet | 50 | (2,0) | dj.c (IRON) | 0 |
| `acy.ad` | Iron Chestplate | 51 | (2,1) | dj.c | 1 |
| `acy.ae` | Iron Leggings | 52 | (2,2) | dj.c | 2 |
| `acy.af` | Iron Boots | 53 | (2,3) | dj.c | 3 |
| `acy.ag` | Diamond Helmet | 54 | (3,0) | dj.e (DIAMOND) | 0 |
| `acy.ah` | Diamond Chestplate | 55 | (3,1) | dj.e | 1 |
| `acy.ai` | Diamond Leggings | 56 | (3,2) | dj.e | 2 |
| `acy.af` | Diamond Boots | 57 | (3,3) | dj.e | 3 |

**Note:** Gold armor uses `dj.d` (not listed in acy.java excerpt above — refer to the full `acy.java` static initializer for gold armor IDs 58-61).

---

## 15. Constants & Magic Numbers

| Value | Location | Meaning |
|---|---|---|
| `1` | ads.hitEntity durability cost | tools cost 2, swords cost 1 per entity hit |
| `2` | ads.hitEntity durability | mining tool: 2 per hit; zp.onBlockDestroyed: 2 per block |
| `1` | ads.onBlockDestroyed durability | 1 per block broken (for all tool types) |
| `4` | zp.a field base | sword base damage; adds material bonus on top |
| `15.0F` | zp.getStrVsBlock cobweb | sword on cobweb = 15× speed (special case) |
| `1.5F` | zp.getStrVsBlock default | sword on all other blocks = 1.5× speed |
| `72000` | zp.getMaxItemUseDuration | 3600 seconds of blocking if held; effectively unlimited |
| `{11,16,15,13}` | agi.bS | slot base durability: helmet=11, chest=16, legs=15, boots=13 |

---

## 16. Bitwise & Data Layouts

No metadata encoding for tools or armor items in 1.0. Durability is stored in `dk.e` (ItemStack damage short), not in the item class itself.

---

## 17. Tick Behaviour

Tools and armor items are not ticked. Durability is decremented only on use (hitEntity / onBlockDestroyed / tillBlock).

---

## 18. Known Quirks / Bugs to Preserve

1. **Sword hitEntity costs 1, onBlockDestroyed costs 2**: The opposite of mining tools (tools: entity=2, block=1). This is intentional — swords are optimised for entity combat, not block breaking.
2. **Hoe has no weapon damage**: `wr` does not extend `ads` and sets no `bS`. Hoe attacks deal 0 bonus damage regardless of material. The `a(ia)` method falls through to `acy.a(ia)` which presumably returns 0 or is undefined.
3. **Gold tools highest efficiency, lowest durability**: GOLD: efficiency=12.0F > DIAMOND: 8.0F, but maxUses=32 vs 1561. Preserve exact values.
4. **Shovel canHarvestBlock is only snow**: Other shovel targets (dirt/sand/gravel/clay etc.) can drop their items without a shovel; snow requires a shovel. Do not extend canHarvestBlock to all effective blocks.
5. **Efficiency enchant check requires `canHarvestBlock`**: If `isItemEffectiveAgainst` returns false (material has no-tool-required flag is not set, and item.canHarvestBlock returns false), the Efficiency bonus does NOT apply — even if getStrVsBlock returns the efficiency value. Both conditions must be true.
6. **Water penalty + airborne penalty are independent multipliers**: Mining in water while jumping applies `/= 5.0 * 5.0 = 25.0`, not `/= 10.0`.
7. **Axe efficiency applies to all wood-material blocks**: The axe override `a(dk, yy)` checks `block.bZ == p.d` (wood material) — any wood-material block gets efficiency speed, even if not in the `bR` array.

---

## 19. Open Questions

1. **`acy.i(int)` exact semantics**: Called in constructors as "set durability" — confirm whether this sets `getMaxDamage()` or the initial `dk.e` (damage) field. Likely sets `maxDamage` field on Item base.
2. **`p.k()` meaning**: Used in `x.b(yy)` (canHarvestBlock) to check if material requires no tool. Confirm which Material instances have `k()=true` — likely ground/dirt/sand materials.
3. **Gold armor IDs**: The acy.java excerpt shows leather/chain/iron/diamond armor but the full list must include gold armor (estimated IDs 58-61 with `dj.d`). Verify exact IDs and acy field names.
4. **`ml.b()` and `ml.c()`**: Enchantment helper methods — `ml.b(inventory)` = Efficiency level, `ml.c(inventory)` = Unbreaking level, `ml.g(inventory)` = Aqua Affinity present. Confirm class `ml` = EnchantmentHelper and these method signatures.
5. **EntityPlayer attack integration**: How does the player's attack method call `dk.a(ia)` to get damage and then apply it to the target? The chain must be: player.attack → `stack.getDamage(entity)` → `item.a(ia)` → returns damage integer → `target.attackEntityFrom(source, damage)`. The spec covers item side; the player attack method itself needs to be confirmed.
