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

# TileEntity Spec
**Source classes:** `bq.java` (base, 129 lines), `oe.java` (Furnace, 219 lines),
  `tu.java` (Chest, 231 lines), `bp.java` (Dispenser, ~116 lines),
  `u.java` (Sign, 27 lines), `ze.java` (MobSpawner, 103 lines),
  `nj.java` (NoteBlock, 52 lines), `mt.java` (FurnaceRecipes, partial)
**Superclass:** n/a (documents the TileEntity hierarchy)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** DRAFT
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

Specifies every TileEntity (TE) class registered in 1.0, covering:
- `bq` base: fields, save/load chain, factory
- Inventory-slot NBT format shared by Chest, Furnace, Dispenser
- Per-TE: fields, NBT layout, tick logic

TileEntities are stored in the `"TileEntities"` TAG_List of a chunk compound.
Each element is a TAG_Compound produced by `bq.a(ik)` (write) / loaded via `bq.c(ik)` (factory).

---

## 2. Class Identifiers

| Obfuscated | Human name | Registry string ID | Block IDs |
|---|---|---|---|
| `bq` | `TileEntity` | — (abstract base) | — |
| `oe` | `TileEntityFurnace` | `"Furnace"` | 61 (unlit), 62 (lit) |
| `tu` | `TileEntityChest` | `"Chest"` | 54 |
| `bp` | `TileEntityDispenser` | `"Trap"` | 23 |
| `u` | `TileEntitySign` | `"Sign"` | 63 (wall), 68 (standing) |
| `ze` | `TileEntityMobSpawner` | `"MobSpawner"` | 52 |
| `nj` | `TileEntityNote` | `"Music"` | 25 |
| `agb` | `TileEntityPiston` | `"Piston"` | 33/34/36 |
| `tt` | `TileEntityBrewingStand` | `"Cauldron"` | 117 |
| `rq` | `TileEntityEnchantmentTable` | `"EnchantTable"` | 116 |
| `agc` | `TileEntityRecordPlayer` | `"RecordPlayer"` | 84 |
| `yg` | `TileEntityEndPortal` | `"Airportal"` | 119 |
| `mt` | `FurnaceRecipes` | — (singleton; smelting table) | — |

---

## 3. `bq` — TileEntity Base

### 3.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` (static Map) | `Map<String, Class>` | — | String ID → Class (for factory) |
| `b` (static Map) | `Map<Class, String>` | — | Class → String ID (for save) |
| `c` | `ry` | null | World reference (set when chunk loads) |
| `d` | `int` | — | Block X coordinate |
| `e` | `int` | — | Block Y coordinate |
| `f` | `int` | — | Block Z coordinate |
| `g` | `boolean` | false | IsInvalid / toDrop flag; `i()` getter, `l()` sets true, `m()` sets false |
| `h` | `int` | -1 | Cached block ID; -1 = not yet read; refreshed lazily via `f()` |
| `i` | `yy` | null | Cached Block reference; refreshed lazily via `g()` |

### 3.2 Static Registry

Populated in a static initializer:
```
a(oe.class,  "Furnace")
a(tu.class,  "Chest")
a(agc.class, "RecordPlayer")
a(bp.class,  "Trap")
a(u.class,   "Sign")
a(ze.class,  "MobSpawner")
a(nj.class,  "Music")
a(agb.class, "Piston")
a(tt.class,  "Cauldron")
a(rq.class,  "EnchantTable")
a(yg.class,  "Airportal")
```
Duplicate ID strings throw `IllegalArgumentException`.

### 3.3 `bq.a(ik)` — Base Write

Called by subclass `a(tag)` via `super.a(tag)`:
```
stringId = b.get(this.getClass())     // look up class in reverse map
if stringId == null:
    throw RuntimeException            // unregistered TE = bug

tag.put("id", stringId)              // TAG_String
tag.put("x", d)                      // TAG_Int
tag.put("y", e)                      // TAG_Int
tag.put("z", f)                      // TAG_Int
```

### 3.4 `bq.b(ik)` — Base Read

Called by subclass `b(tag)` via `super.b(tag)`:
```
d = tag.e("x")     // TAG_Int getter; default 0 if absent
e = tag.e("y")
f = tag.e("z")
```

### 3.5 `bq.c(ik)` — Factory (static)

Creates a TE from a TAG_Compound read from disk:
```
stringId = tag.i("id")              // TAG_String getter
clazz = a.get(stringId)
if clazz == null:
    println "Skipping TileEntity with id X"
    return null
te = clazz.newInstance()            // no-arg constructor
te.b(tag)                           // read all fields (base + subclass)
return te
```

### 3.6 Other Base Methods

| Method | Behaviour |
|---|---|
| `f()` | Returns cached block ID (`h`); if -1, reads `world.d(x,y,z)` and caches. |
| `h()` | Refreshes cached block ID from world; calls `world.b(x,y,z,this)` to re-register TE. |
| `g()` | Returns cached Block ref (`i`); if null, reads `yy.k[world.a(x,y,z)]`. |
| `b()` | Tick method — empty in base; overridden by Furnace, MobSpawner, etc. |
| `l()` | Sets `g = true` (marks TE as invalid/to-remove). |
| `m()` | Sets `g = false` (un-invalidates). |
| `n()` | Resets `i = null` and `h = -1` (clears cached block/ID). |
| `i()` | Returns `g` (isInvalid). |
| `a(double,double,double)` | Returns squared distance from TE centre to given point. |

---

## 4. Inventory Slot Format (shared by Chest, Furnace, Dispenser)

All three inventory TEs store slots in a TAG_List of TAG_Compounds under the key `"Items"`.
Only **non-null** slots are written (sparse encoding). Slots are identified by a `"Slot"` byte.

### Write pattern (used by all three):
```
items = new TAG_List()
for slotIndex in 0..N-1:
    if slot[slotIndex] != null:
        compound = new TAG_Compound()
        compound.put("Slot", (byte)slotIndex)
        slot[slotIndex].b(compound)        // ItemStack write (EntityNBT_Spec §9)
        items.add(compound)
tag.put("Items", items)
```

### Read pattern (used by all three):
```
items = tag.l("Items")                    // TAG_List getter; empty list if absent
for i in 0..items.size-1:
    compound = items.get(i)
    slotIndex = compound.c("Slot") [& 255 for Chest/Dispenser]
    if 0 <= slotIndex < arraySize:
        slot[slotIndex] = dk.a(compound)  // ItemStack factory
```

**Slot byte range note:** Furnace reads `"Slot"` as a signed byte (no `& 255` mask).
Chest and Dispenser read `"Slot"` with `& 255` (unsigned). In practice slots are
0–26 (chest), 0–2 (furnace), 0–8 (dispenser), so the distinction is academic.

### ItemStack slot compound format:
Each slot compound contains the standard ItemStack fields (from EntityNBT_Spec §9):
```
"Slot"   → TAG_Byte   (slot index)
"id"     → TAG_Short  (block/item ID)
"Count"  → TAG_Byte   (stack size)
"Damage" → TAG_Short  (damage/metadata)
["tag"   → TAG_Compound (enchantments/extra data, if present)]
```

---

## 5. `oe` — TileEntityFurnace

### 5.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `k` | `dk[3]` | new dk[3] | Slot 0 = input, Slot 1 = fuel, Slot 2 = output |
| `a` | `int` | 0 | `burnTime` — ticks of fuel remaining (counts down to 0) |
| `b` | `int` | 0 | `currentItemBurnTime` — total ticks for the current fuel item (used for progress bar) |
| `j` | `int` | 0 | `cookTime` — ticks the current input item has been cooked (0 → 200) |

### 5.2 NBT Layout

Written by `oe.a(ik)` (calls `super.a(tag)` first):

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"Furnace"` (from base) |
| `"x"`, `"y"`, `"z"` | TAG_Int | block coordinates (from base) |
| `"BurnTime"` | TAG_Short | `a` (burnTime) |
| `"CookTime"` | TAG_Short | `j` (cookTime) |
| `"Items"` | TAG_List | slots 0–2 (sparse; see §4) |

Read by `oe.b(ik)`:
- Calls `super.b(tag)` first (reads x,y,z).
- Reads `"Items"` list into `k[]`.
- Reads `"BurnTime"` → `a`.
- Reads `"CookTime"` → `j`.
- Recomputes `b = a(k[1])` (fuel burn time for current fuel item in slot 1).

### 5.3 Tick Logic (`oe.b()`)

Called once per world tick (20 Hz) on the server side. Skip if `world.I == true` (client-side flag).

```
wasBurning = (a > 0)

// Burn-down
if a > 0: a--

// Re-fuel: if burn ran out and can smelt
if a == 0 AND canSmelt():
    b = a = getFuelValue(k[1])    // both currentItemBurnTime and burnTime = fuel ticks
    if a > 0:
        changed = true
        if k[1] != null:
            k[1].stackSize--
            if k[1].stackSize == 0:
                k[1] = null

// Cook progress: advance only if burning AND can smelt
if isBurning() AND canSmelt():
    j++
    if j == 200:                  // fully cooked
        j = 0
        outputSmeltedItem()       // produce output
        changed = true
else:
    j = 0                         // reset cook progress if not burning

// Lit/unlit block state switch
if wasBurning != (a > 0):
    changed = true
    eu.a(a > 0, world, x, y, z)  // switch between block ID 61 (unlit) and 62 (lit)

if changed: h()                   // mark dirty + notify world
```

`canSmelt()` = `p()` method:
```
if k[0] == null: return false
recipe = FurnaceRecipes.getResult(k[0].itemId)
if recipe == null: return false
if k[2] == null: return true
if k[2].itemId != recipe.itemId: return false
return k[2].stackSize < maxStackSize AND k[2].stackSize < recipe.maxStackSize
```

`outputSmeltedItem()` = `o()` method:
```
recipe = FurnaceRecipes.getResult(k[0].itemId)
if k[2] == null: k[2] = recipe.copy()
else: k[2].stackSize++
k[0].stackSize--
if k[0].stackSize <= 0: k[0] = null
```

### 5.4 Fuel Value Table (`a(dk)` private method)

| Condition | Ticks | Seconds at 20 Hz |
|---|---|---|
| `var2 < 256 AND yy.k[var2].material == p.d` (any wooden block) | 300 | 15 s |
| `var2 == acy.C.id` (Sticks) | 100 | 5 s |
| `var2 == acy.l.id` (Coal) | 1600 | 80 s |
| `var2 == acy.ax.id` (Lava Bucket) | 20000 | 1000 s |
| `var2 == yy.y.id` (Sapling) | 100 | 5 s |
| `var2 == acy.bn.id` (Blaze Rod) | 2400 | 120 s |
| All others | 0 | not fuel |

> **Obfuscated IDs to verify against BlockRegistry_Spec/Item:** `acy.C`, `acy.l`, `acy.ax`, `yy.y`, `acy.bn`.
> The fuel check for wooden blocks uses `material == p.d` (wood material) which covers
> planks, logs, fences, stairs, doors, pressure plates, trapdoors, chests, crafting tables, etc.

### 5.5 Smelting Recipes (`mt` — FurnaceRecipes singleton)

`mt.a()` returns the singleton. `mt.a(inputItemId)` returns the output `dk` (ItemStack), or null.

Registered recipes (from `mt` static block):

| Input (obf) | Output (obf) | Notes |
|---|---|---|
| `yy.H` | `acy.n` | Iron Ore → Iron Ingot |
| `yy.G` | `acy.o` | Gold Ore → Gold Ingot |
| `yy.aw` | `acy.m` | Sand → Glass |
| `yy.E` | `yy.M` | Clay (block) → Hardened Clay (Bricks) |
| `acy.ap` | `acy.aq` | Raw Pork → Cooked Pork |
| `acy.bh` | `acy.bi` | Raw Fish → Cooked Fish |
| `acy.bj` | `acy.bk` | Raw Chicken → Cooked Chicken |
| `acy.aT` | `acy.aU` | Raw Beef → Steak |
| `yy.w` | `yy.t` | Wood (log) → Charcoal (output: coal item type 1) |
| `acy.aH` | `acy.aG` | Potato → Baked Potato |
| `yy.aV` | `acy.aV, 1, 2` | Cactus → Green Dye (output count 2) |
| `yy.J` | `acy.l, 1, 1` | TNT-like block → Coal (item type 1) |
| `yy.I` | `acy.l` | Gravel-like block → Coal |
| `yy.aN` | `acy.aB` | Netherrack → Nether Brick item |
| `yy.N` | `acy.aV, 1, 4` | Sponge-like block → Green Dye (count 4) |

> **Note:** Obfuscated fields require cross-referencing with BlockRegistry_Spec and Item_Spec for exact IDs.

---

## 6. `tu` — TileEntityChest

### 6.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `p` | `dk[36]` | new dk[36] | Inventory array; only slots 0–26 exposed via `de` interface |
| `a` | `boolean` | false | `adjacentChestChecked` flag; set on first neighbour check |
| `b,j,k,l` | `tu` | null | Adjacent chest references for double-chest logic |
| `m,n` | `float` | 0 | Lid animation angles (current and previous) |
| `o` | `int` | 0 | Number of players viewing the chest (for lid open state) |
| `q` | `int` | 0 | Internal tick counter |

### 6.2 NBT Layout

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"Chest"` |
| `"x"`, `"y"`, `"z"` | TAG_Int | coordinates |
| `"Items"` | TAG_List | slots 0–26 (sparse; slot byte 0–26) |

Internal array is 36 but only indices 0–26 are accessible and saved. Indices 27–35 appear
to be reserved for double-chest extension but are NOT saved separately — the adjacent chest
saves its own 27 slots independently.

### 6.3 Tick

No server-side tick logic (lid animation is client-only). `b()` not overridden.

---

## 7. `bp` — TileEntityDispenser

### 7.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `dk[9]` | new dk[9] | 9-slot inventory (3×3 grid) |
| `b` | `Random` | new Random() | RNG for dispensing |

### 7.2 NBT Layout

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"Trap"` |
| `"x"`, `"y"`, `"z"` | TAG_Int | coordinates |
| `"Items"` | TAG_List | slots 0–8 (sparse; slot byte 0–8) |

### 7.3 Dispense Logic (not in NBT spec but relevant)

On redstone trigger: picks a random non-null slot and dispenses the item. Tick not documented here
— the dispenser is triggered externally (redstone), not by its own tick.

---

## 8. `u` — TileEntitySign

### 8.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `String[4]` | `{"","","",""}` | Four text lines |
| `b` | `int` | -1 | Edit state (which player is editing; -1 = none) |
| `j` | `boolean` | true | Editable flag; set false when loaded from NBT |

### 8.2 NBT Layout

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"Sign"` |
| `"x"`, `"y"`, `"z"` | TAG_Int | coordinates |
| `"Text1"` | TAG_String | line 0 |
| `"Text2"` | TAG_String | line 1 |
| `"Text3"` | TAG_String | line 2 |
| `"Text4"` | TAG_String | line 3 |

### 8.3 Text Truncation

On read, each line is truncated to 15 characters:
```
if a[i].length() > 15:
    a[i] = a[i].substring(0, 15)
```

On write, no truncation — whatever is in `a[]` is written as-is.
`j = false` is set at the start of `b(ik)` (marks sign as no-longer-editable after first load).

### 8.4 Tick

No tick logic. `b()` not overridden.

---

## 9. `ze` — TileEntityMobSpawner

### 9.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | -1 → after first d(): 200 to 800 | `spawnDelay` — ticks until next spawn attempt; -1 = not yet initialised |
| `k` | `String` | `"Pig"` | Entity type string ID (from `afw` table) |
| `b` | `double` | 0 | Current rotation (visual, degrees) |
| `j` | `double` | 0 | Previous rotation (for interpolation) |

### 9.2 NBT Layout

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"MobSpawner"` |
| `"x"`, `"y"`, `"z"` | TAG_Int | coordinates |
| `"EntityId"` | TAG_String | entity type string (e.g., `"Pig"`, `"Zombie"`) |
| `"Delay"` | TAG_Short | current spawn delay in ticks |

### 9.3 Tick Logic (`ze.b()`)

Only active when a player is within 16 blocks (sphere check). Server-side only (`!world.I`).

```
j = b                                   // save prev rotation for interpolation
b += 1000.0 / (a + 200.0)              // spin animation (faster when delay low)
while b > 360.0: b -= 360.0

if a == -1:                             // first tick: randomise initial delay
    resetDelay()                        // a = 200 + rand.nextInt(600)
    return

if a > 0:
    a--
    return

// Attempt spawn (up to 4 mobs)
for 0..3:
    entity = afw.a(k, world)           // create mob by string ID
    if entity == null: return

    // Count existing mobs of this type within 8×4×8 area
    count = world.getEntitiesOfType(entity.class, AABB(x,y,z).expand(8,4,8))
    if count >= 6:
        resetDelay()
        return

    // Spawn at random offset within ±4 in X/Z, [y-1, y+1] in Y
    sx = x + (rand.nextDouble() - rand.nextDouble()) * 4.0
    sy = y + rand.nextInt(3) - 1
    sz = z + (rand.nextDouble() - rand.nextDouble()) * 4.0
    entity.setLocationAndAngles(sx, sy, sz, rand.nextFloat() * 360.0, 0.0)

    if entity.isValid():                // collision/spawn check
        world.spawnEntity(entity)
        world.playEffect(2004, x, y, z, 0)   // smoke particles
        entity.spawnExplosionParticle()
        resetDelay()

resetDelay() = a = 200 + rand.nextInt(600)   // next delay: 200–799 ticks (10–40 seconds)
```

---

## 10. `nj` — TileEntityNote (NoteBlock)

### 10.1 Fields

| Field (obf) | Type | Default | Semantics |
|---|---|---|---|
| `a` | `byte` | 0 | Note pitch (0–24; 25 distinct pitches; wraps on increment) |
| `b` | `boolean` | false | Internal flag (purpose: editing/display state) |

### 10.2 NBT Layout

| NBT key | Type | Value |
|---|---|---|
| `"id"` | TAG_String | `"Music"` |
| `"x"`, `"y"`, `"z"` | TAG_Int | coordinates |
| `"note"` | TAG_Byte | pitch 0–24 |

On read: `a` is clamped to [0, 24].

### 10.3 Note Increment

`a()` method: `a = (byte)((a + 1) % 25)` — cycles 0→1→…→24→0. Calls `h()` (markDirty) after.

### 10.4 Sound Selection (`a(ry, x, y, z)`)

Determines instrument based on block below:
| Block material below | Instrument byte |
|---|---|
| `p.a` (air / default) | 0 (harp) |
| `p.e` (stone) | 1 (bass drum) |
| `p.o` (sand/gravel) | 2 (snare) |
| `p.q` (glass/ice) | 3 (hi-hat) |
| `p.d` (wood) | 4 (bass guitar) |

Then calls `world.f(x, y, z, instrument, note)` (play sound effect).
Only plays if block above is air (`material == p.a`).

---

## 11. Remaining TileEntities (NBT-only reference)

These TEs are registered in `bq` but not fully analysed here.
They store no additional NBT beyond base (x, y, z, id) unless overridden.

| Obfuscated | Name | Notes |
|---|---|---|
| `agb` | `TileEntityPiston` | Piston extension block; may store extension state in NBT |
| `tt` | `TileEntityBrewingStand` | Brewing stand (1.0 potions); probably stores 4 slots + brew time |
| `rq` | `TileEntityEnchantmentTable` | Enchanting table; animated book rendering; no inventory stored |
| `agc` | `TileEntityRecordPlayer` | Jukebox; stores playing record |
| `yg` | `TileEntityEndPortal` | End portal frame; no additional NBT expected |

---

## 12. Known Quirks / Bugs to Preserve

- **Furnace re-reads fuel burn time on load**: `b = a(k[1])` is called at end of `b(ik)` to
  restore `currentItemBurnTime`. This means if the fuel item in slot 1 changes type while the
  furnace is burning, loading the save will recalculate `b` from the current slot 1 item, not
  from what was burning when saved. The actual `burnTime` (`a`) is preserved correctly.
- **Furnace cook reset on idle**: if the furnace has no fuel or cannot smelt, `j = 0` (cookTime
  resets). A partially-cooked item loses progress on any tick without active burning.
- **Sign text truncated on load, not on write**: signs can be written with strings > 15 chars
  (e.g., by a hacked client) but are silently truncated to 15 chars on the next load.
- **Sign `j = false` set before base read**: `u.b(ik)` sets `j = false` before calling
  `super.b(tag)`. This means signs become non-editable as soon as any NBT is read into them.
- **MobSpawner initial delay quirk**: if `"Delay"` is read as -1 from disk (e.g., a freshly
  placed spawner that was saved before its first tick), the spawner randomises its delay on the
  very next tick and starts the countdown. It does not spawn on the first tick after load.
- **Chest array is 36 but only 27 used**: internal `p[36]` allows double-chest linking without
  reallocating. Indices 27–35 are never written to NBT. They serve as scratch space for the
  adjacent-chest double-chest view.

---

## 13. Open Questions

- **`agb` (TileEntityPiston) NBT**: piston extension block likely stores `"extending"` boolean
  and `"facing"` int and `"progress"` float. Not analysed.
- **`tt` (TileEntityBrewingStand) NBT**: slots + brew time not confirmed. 1.0 brewing system
  may be incomplete or non-functional.
- **`agc` (TileEntityRecordPlayer) NBT**: probably stores `"Record"` int (item ID). Not confirmed.
- **Dispense trigger**: the actual redstone-to-dispense path is not documented here. Lives in
  the Block class for dispenser (`yy` subclass), not in the TE itself.
- **`acy.C`, `acy.l`, `acy.ax`, `yy.y`, `acy.bn`** (fuel item IDs): need cross-reference with
  Item_Spec / BlockRegistry_Spec for authoritative human names and numeric IDs.

---

*Spec written by Analyst AI from `bq.java` (129 lines), `oe.java` (219 lines), `tu.java` (231 lines),
`bp.java` (~116 lines), `u.java` (27 lines), `ze.java` (103 lines), `nj.java` (52 lines),
`mt.java` (partial). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — TileEntity)*
