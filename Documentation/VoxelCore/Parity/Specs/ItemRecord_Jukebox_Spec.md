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

# ItemRecord / BlockJukebox / TileEntityJukebox Spec
Source classes: `pe.java` (ItemRecord), `abl.java` (BlockJukebox), `agc.java` (TileEntityJukebox)
Superclasses: `pe` extends `acy` (Item); `abl` extends `ba` (BlockContainer); `agc` extends `bq` (TileEntity)

---

## 1. Purpose

`pe` (ItemRecord) represents a music disc item. When used on an empty jukebox block, it inserts
itself into the jukebox and begins playback. `abl` (BlockJukebox) is a container block (ID 84)
that stores one record and can eject it when activated. `agc` (TileEntityJukebox) stores the
record's item ID as a single integer field persisted to NBT. There are 11 distinct music disc
items in 1.0, with item IDs 2256–2266.

---

## 2. Fields

### pe (ItemRecord)

| Field (obf) | Type   | Default | Semantics                                 |
|-------------|--------|---------|-------------------------------------------|
| `a`         | String | (ctor)  | Short disc name: "13", "cat", "blocks", … |
| `bN`        | int    | 1       | Max stack size — overridden to 1 (no stacking) |

All other fields (`bM`, `bL`, etc.) are inherited from `acy` (Item base class).

### agc (TileEntityJukebox)

| Field (obf) | Type | Default | Semantics                                             |
|-------------|------|---------|-------------------------------------------------------|
| `a`         | int  | 0       | Item ID of the currently-loaded record (0 = empty)   |

### abl (BlockJukebox) — no new instance fields beyond what ba/yy provide.

---

## 3. Constants & Magic Numbers

### Block constants (abl)

| Constant | Value | Meaning |
|---|---|---|
| Block ID | 84 | Jukebox block ID in `yy.aY` |
| Hardness | 2.0F | Constructor: `.c(2.0F)` — same as wood planks |
| Resistance | 10.0F | Constructor: `.b(10.0F)` |
| Material | `p.d` | Wood material |
| Base texture index | 74 | `bL` set by constructor — items.png-unrelated; terrain atlas index 74 = jukebox side |
| Top texture | bL + 1 = 75 | Terrain atlas index for top face |

### World event IDs used by jukebox

| Event ID | Used for |
|---|---|
| 1005 | Jukebox sound event — broadcast to nearby clients. Data = record item ID to play; data = 0 to stop. |

### Record items in acy (Item registry)

All 11 records are declared as static fields on `acy`. They are created with `new pe(internalId, name)`.
The actual item ID `bM` = internalId + 256 (standard Item offset).

| acy field | Constructor arg | bM (item ID) | Name string | Texture atlas col | Row |
|-----------|-----------------|--------------|-------------|-------------------|-----|
| `bB`      | 2000            | 2256         | `"13"`      | 0                 | 15  |
| `bC`      | 2001            | 2257         | `"cat"`     | 1                 | 15  |
| `bD`      | 2002            | 2258         | `"blocks"`  | 2                 | 15  |
| `bE`      | 2003            | 2259         | `"chirp"`   | 3                 | 15  |
| `bF`      | 2004            | 2260         | `"far"`     | 4                 | 15  |
| `bG`      | 2005            | 2261         | `"mall"`    | 5                 | 15  |
| `bH`      | 2006            | 2262         | `"mellohi"` | 6                 | 15  |
| `bI`      | 2007            | 2263         | `"stal"`    | 7                 | 15  |
| `bJ`      | 2008            | 2264         | `"strad"`   | 8                 | 15  |
| `bK`      | 2009            | 2265         | `"ward"`    | 9                 | 15  |
| `bL`      | 2010            | 2266         | `"11"`      | 10                | 15  |

**"wait" (disc) is ABSENT in 1.0.** Added in a later version. Total: 11 discs (IDs 2256–2266).

Each record is registered with:
- `.a(col, 15)` — sets items.png atlas coordinates (column, row 15)
- `.a("record")` — sets item name key

---

## 4. Methods — Detailed Logic

### 4.1 pe.onItemUse (obf: `a(dk, vi, ry, int, int, int, int)`)

**Called by:** player right-clicks on a block while holding a record item.
**Parameters:** `item`=ItemStack, `player`=vi, `world`=ry, `x`, `y`, `z`, `face` (int)
**Returns:** boolean — true if consumed, false if no action

Step-by-step logic:

1. Check `world.a(x, y, z)` (getBlockId) == `yy.aY.bM` (84). If not the jukebox block → return false.
2. Check `world.d(x, y, z)` (getBlockMetadata) == 0. If not 0 (already loaded) → return false.
3. If `world.I` is true (client-side) → return true (the client acknowledges but does not act).
4. Cast `yy.aY` (the BlockJukebox instance) to `abl` and call its `f(world, x, y, z, this.bM)`.
   - This inserts the disc's item ID into the jukebox tile entity and sets meta to 1.
5. Call `world.a(null, 1005, x, y, z, this.bM)`:
   - This broadcasts world event 1005 to nearby clients with the record's item ID as the data value.
   - Clients use the event to begin playing the corresponding music track.
6. Decrement `item.a` (stack size) by 1.
7. Return true.

---

### 4.2 pe.addInformation (obf: `a(dk, List)`)

**Called by:** item tooltip rendering.
**Parameters:** `stack`=dk, `tooltip`=List
**Returns:** void

Appends the string `"C418 - " + this.a` to the tooltip list.
Example: disc "13" → tooltip line `"C418 - 13"`.

---

### 4.3 pe.getRarity (obf: `d(dk)`)

**Called by:** item name colouring.
**Returns:** `ja.c`

`ja.c` = RARE rarity → renders the item name in light-blue/aqua colour in tooltips.

---

### 4.4 abl.getBlockTexture (obf: `b(int)`)

**Parameters:** `face` (0–5)
**Returns:** int (terrain atlas texture index)

- If `face == 1` (top face): return `bL + 1` (= 75)
- All other faces: return `bL` (= 74)

---

### 4.5 abl.onBlockActivated (obf: `a(ry, int, int, int, vi)`)

**Called by:** player right-clicks on the jukebox.
**Parameters:** `world`, `x`, `y`, `z`, `player`
**Returns:** boolean

1. If `world.d(x, y, z)` (metadata) == 0 → return false (jukebox is empty, player must use disc item).
2. If `world.I` is true (client-side) → return true (acknowledge without acting).
3. Call `this.g(world, x, y, z)` (ejectRecord — see §4.7).
4. Return true.

---

### 4.6 abl.insertRecord (obf: `f(ry, int, int, int, int)`)

**Called by:** `pe.onItemUse` after validity checks pass.
**Parameters:** `world`, `x`, `y`, `z`, `recordItemId` (int)
**Returns:** void

Server-side only (`if !world.I`):

1. Get TileEntity at (x, y, z) cast to `agc`. If null → do nothing (safety check).
2. Set `tileEntity.a = recordItemId`.
3. Call `tileEntity.h()` (markDirty — signals chunk save needed).
4. Call `world.f(x, y, z, 1)` (setBlockMetadataWithNotify with meta=1, notifyNeighbours=1).
   - Meta 1 = jukebox has a disc loaded.

---

### 4.7 abl.ejectRecord (obf: `g(ry, int, int, int)`)

**Called by:** `onBlockActivated` (player right-clicks loaded jukebox), `onBlockPreDestroy`.
**Parameters:** `world`, `x`, `y`, `z`
**Returns:** void

Server-side only (`if !world.I`):

1. Get TileEntity cast to `agc`. If null → do nothing.
2. Read `var6 = tileEntity.a`. If `var6 == 0` → do nothing (already empty).
3. Call `world.g(1005, x, y, z, 0)`:
   - Broadcasts world event 1005 with data=0 to nearby clients.
   - Clients stop the currently-playing record.
4. Call `world.a(null, x, y, z)`:
   - Stops any server-side sound logic (exact method signature in World — see §8).
5. Set `tileEntity.a = 0`.
6. Call `tileEntity.h()` (markDirty).
7. Call `world.f(x, y, z, 0)` (setBlockMetadataWithNotify with meta=0).
   - Meta 0 = jukebox empty.
8. Compute ejection position using `world.w` (World RNG):
   - `offsetX = world.w.nextFloat() * 0.7F + (1.0F - 0.7F) * 0.5` = random in [0.15, 0.85]
   - `offsetY = world.w.nextFloat() * 0.7F + (1.0F - 0.7F) * 0.2 + 0.6` = random in [0.66, 1.26]
   - `offsetZ = world.w.nextFloat() * 0.7F + (1.0F - 0.7F) * 0.5` = random in [0.15, 0.85]
9. Create `ih` (EntityItem) at `(x + offsetX, y + offsetY, z + offsetZ)` with `ItemStack(var6, 1, 0)` (damage=0).
10. Set `entityItem.c = 10` (pickup delay = 10 ticks).
11. Spawn the EntityItem via `world.a(entityItem)`.

The `0.7F` factor is named `var8` in source. The exact RNG call sequence is:
- nextFloat() for X first, nextFloat() for Y second, nextFloat() for Z third.

---

### 4.8 abl.onBlockPreDestroy (obf: `d(ry, int, int, int)`)

**Called by:** block removal (by player or explosion).
**Returns:** void

1. Call `this.g(world, x, y, z)` (ejectRecord — spawns disc item and stops music).
2. Call `super.d(world, x, y, z)` (BlockContainer base — removes TileEntity from world).

---

### 4.9 abl.dropBlockAsItem (obf: `a(ry, int, int, int, int, float, int)`)

**Called by:** block destruction drop logic.
**Parameters:** `world`, `x`, `y`, `z`, `meta`, `chance`, `fortune`

Server-side only (`if !world.I`):

Calls `super.a(world, x, y, z, meta, chance, 0)` always with **damage=0** (overrides fortune=0 regardless of input).

The disc itself is ejected separately by `onBlockPreDestroy` → `ejectRecord`. This method only drops the jukebox block item.

---

### 4.10 abl.createNewTileEntity (obf: `j_()`)

**Returns:** new `agc()` instance — a fresh, empty TileEntityJukebox.

---

### 4.11 agc.readFromNBT (obf: `b(ik)`)

**Called by:** chunk load.
**Parameters:** `nbt`=ik (NBTTagCompound)

1. Call `super.b(nbt)` (base TileEntity reads id/x/y/z).
2. Set `this.a = nbt.e("Record")` (reads NBT int tag named "Record").
   - If tag absent, Java returns 0 (default int) — field stays 0 = empty.

---

### 4.12 agc.writeToNBT (obf: `a(ik)`)

**Called by:** chunk save.
**Parameters:** `nbt`=ik (NBTTagCompound)

1. Call `super.a(nbt)` (base TileEntity writes id/x/y/z).
2. If `this.a > 0`: write `nbt.a("Record", this.a)` (NBT int tag named "Record").
   - Disc ID is NOT written when 0 (empty jukebox saves no "Record" tag at all).

---

## 5. Bitwise & Data Layouts

### Block metadata (abl)

```
Bit 0 (value 1): hasRecord
  0 = jukebox empty (no TileEntity data to read)
  1 = jukebox contains a disc
Bits 1–3: unused (always 0 in 1.0)
```

The metadata is used only as a presence flag. The actual record item ID is stored in the TileEntity (`agc.a`), not in block metadata.

---

## 6. Tick Behaviour

None of these three classes is ticked. The jukebox has no `randomTick` or `scheduledTick`. Music plays purely client-side in response to world event 1005.

`ba` (BlockContainer) does not set `isBlockContainer = true` in any scheduled-tick sense — it only registers the TileEntity.

---

## 7. Known Quirks / Bugs to Preserve

### 7.1 Ejection RNG uses World RNG (world.w), not a local Random

The ejection position offsets in `g()` consume three values from the world's shared RNG `world.w`.
This means ejecting a disc advances the global world RNG state by 3 calls.
The Coder must use the world RNG (not a fresh `new Random()`) in the same call order:
nextFloat→X, nextFloat→Y, nextFloat→Z.

### 7.2 disc ID stored in TileEntity.a, not in metadata

Block metadata only holds a boolean (0/1). The actual record identity is in `agc.a`.
If a chunk is saved with meta=1 but no TileEntity (corrupt save), ejectRecord does nothing
(null check on TileEntity prevents crash but disc is lost).

### 7.3 No "wait" disc in 1.0

The request mentions IDs 2256–2267. The 12th disc ("wait") is absent in 1.0. Only 11 discs
exist (IDs 2256–2266, acy fields bB through bL).

### 7.4 dropBlockAsItem always passes damage=0 to super

Even if the block was broken with a Fortune pickaxe, `fortune` is hardcoded to 0 in the super
call. The jukebox block always drops as a plain jukebox item (no fortune interaction).

### 7.5 Pickup delay on ejected disc = 10 ticks (not the standard 40)

Normal `ItemBlock.onItemUse` sets pickup delay to 40 ticks. The jukebox eject uses
`entityItem.c = 10`, so the disc can be picked up much sooner (0.5 s vs 2 s).

---

## 8. Open Questions

### 8.1 world.a(null, x, y, z) signature in ejectRecord

In `g()`, the call `var1.a(null, var2, var3, var4)` is made before ejecting the disc.
The exact signature of this World method (4-arg, first arg null) is not resolved from abl.java
alone. It may be `removeSound(Entity, x, y, z)` or similar. The Coder should search World.java
for a 4-argument method that accepts a nullable Entity followed by three ints.

### 8.2 World event 1005 client-side handling

The spec documents that event 1005 with data=recordId starts playback, and data=0 stops it.
The actual sound resource name mapping (event 1005 → which .ogg file) is client-side rendering
logic outside the scope of this spec. The Coder should bind sound names to the record `a` field
(disc name string e.g. "13" → "records.13").
