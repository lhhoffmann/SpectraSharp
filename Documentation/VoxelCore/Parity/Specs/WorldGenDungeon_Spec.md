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

# WorldGenDungeon Spec
**Source class:** `acj.java`
**Superclass:** `ig` (WorldGenerator)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`acj` (WorldGenDungeon) generates a single underground dungeon room at the given world
coordinates. A dungeon is a rectangular stone room (cobblestone and mossy cobblestone walls,
floor, ceiling) containing up to 2 chests with randomised loot and a mob spawner in the
centre. The generator validates that the site is solid-floored, solid-ceilinged, and has
between 1 and 5 natural wall openings (doors/corridors) before placing anything.

In the overworld populate step (`ChunkProviderGenerate.populate()`), 8 dungeon attempts are
made per chunk at random (x, y, z) positions.

---

## 2. Fields

No instance fields beyond `ig` (WorldGenerator) base. The `ig` class has a boolean field
`silent` — set to false by default; when true, block placements do not trigger lighting
updates.

---

## 3. Methods — Detailed Logic

### `a(ry world, Random rng, int x, int y, int z)` — generate

**Returns:** true if the dungeon was placed, false if the site was invalid.

```
HEIGHT = 3                         // fixed dungeon height
xRadius = rng.nextInt(2) + 2       // 2 or 3
zRadius = rng.nextInt(2) + 2       // 2 or 3
doorCount = 0
```

#### Phase 1 — Site validation

Scans the entire bounding box inclusive of 1-block shell:
```
for bx in (x - xRadius - 1)..(x + xRadius + 1):
    for by in (y - 1)..(y + HEIGHT + 1):
        for bz in (z - zRadius - 1)..(z + zRadius + 1):

            material = world.getMaterialAt(bx, by, bz)

            if by == y - 1 AND NOT material.isSolid():
                return false       // floor layer must be entirely solid

            if by == y + HEIGHT + 1 AND NOT material.isSolid():
                return false       // ceiling layer must be entirely solid

            if (bx == x - xRadius - 1 OR bx == x + xRadius + 1
                OR bz == z - zRadius - 1 OR bz == z + zRadius + 1)
               AND by == y:
                // border column at floor height
                if world.isBlockAir(bx, by, bz)        // both floor tile
                   AND world.isBlockAir(bx, by + 1, bz) // and tile above are air
                    doorCount++
```

If `doorCount < 1 OR doorCount > 5`: return false.

Only sites with 1–5 natural openings into the dungeon are valid. Sites that are completely
enclosed (0 doors) or too open (>5 doors) are rejected.

#### Phase 2 — Room construction

Fills the bounding box including walls, floor, and ceiling. The scan order is from top
(y + HEIGHT) down to bottom (y - 1):

```
for bx in (x - xRadius - 1)..(x + xRadius + 1):
    for by in (y + HEIGHT) downto (y - 1):
        for bz in (z - zRadius - 1)..(z + zRadius + 1):

            isInterior = bx != x - xRadius - 1
                      AND by != y - 1
                      AND bz != z - zRadius - 1
                      AND bx != x + xRadius + 1
                      AND by != y + HEIGHT + 1
                      AND bz != z + zRadius + 1

            if isInterior:
                world.setBlockToAir(bx, by, bz)   // carve interior

            else if by >= 0 AND NOT material(bx, by-1, bz).isSolid():
                world.setBlockToAir(bx, by, bz)   // hanging wall: remove (no support)

            else if material(bx, by, bz).isSolid():
                if by == y - 1 AND rng.nextInt(4) != 0:
                    world.setBlock(bx, by, bz, yy.ao.bM)  // mossy cobblestone (ID 48): 75% of floor
                else:
                    world.setBlock(bx, by, bz, yy.w.bM)   // cobblestone (ID 4): walls and 25% of floor
```

The floor (`by == y-1`) is 75% mossy cobblestone, 25% plain cobblestone.
Walls and ceiling are always plain cobblestone.
Interior tiles are set to air regardless of what was there before.

#### Phase 3 — Chest placement (2 chests, up to 3 tries each)

```
for attempt in 0..1:
    for try in 0..2:
        cx = x + rng.nextInt(xRadius * 2 + 1) - xRadius   // random X within interior
        cz = z + rng.nextInt(zRadius * 2 + 1) - zRadius   // random Z within interior
        if world.isBlockAir(cx, y, cz):
            // count solid neighbours in 4 horizontal directions at y:
            solidNeighbours = 0
            if material(cx-1, y, cz).isSolid(): solidNeighbours++
            if material(cx+1, y, cz).isSolid(): solidNeighbours++
            if material(cx, y, cz-1).isSolid(): solidNeighbours++
            if material(cx, y, cz+1).isSolid(): solidNeighbours++

            if solidNeighbours == 1:    // exactly one wall adjacent
                world.setBlock(cx, y, cz, yy.au.bM)       // place chest (ID 54)
                chest = getTileEntity(cx, y, cz) as TileEntityChest
                if chest != null:
                    for slot in 0..7:                      // 8 loot rolls
                        item = rollLoot(rng)
                        if item != null:
                            chest.setSlot(rng.nextInt(chest.size()), item)
                break    // stop trying for this attempt
```

The chest placement requires exactly 1 solid neighbour to ensure the chest is placed
against a wall, not floating in the middle.

#### Phase 4 — Mob spawner placement

```
world.setBlock(x, y, z, yy.as.bM)    // mob spawner (ID 52) at centre

spawner = getTileEntity(x, y, z) as TileEntityMobSpawner
if spawner != null:
    spawner.setEntityId(rollMobType(rng))
else:
    print error to stderr: "Failed to fetch mob spawner entity at (x, y, z)"
```

---

## 4. Loot Table — `a(Random rng)`

Rolls once to determine which item stack to create. Returns an `ItemStack (dk)` or `null`.

| Roll (0–10) | Condition | Item | Count | Note |
|---|---|---|---|---|
| 0 | — | `acy.az` (Saddle) | 1 | — |
| 1 | — | `acy.n` (Iron Ingot) | `rng.nextInt(4) + 1` = 1–4 | — |
| 2 | — | `acy.T` (Bread) | 1 | — |
| 3 | — | `acy.S` (Wheat) | `rng.nextInt(4) + 1` = 1–4 | — |
| 4 | — | `acy.L` (Gunpowder) | `rng.nextInt(4) + 1` = 1–4 | — |
| 5 | — | `acy.J` (String) | `rng.nextInt(4) + 1` = 1–4 | — |
| 6 | — | `acy.av` (Bucket) | 1 | — |
| 7 | `rng.nextInt(100) == 0` | `acy.as` (Golden Apple) | 1 | 1% chance; else null |
| 8 | `rng.nextInt(2) == 0` | `acy.aB` (Redstone Dust) | `rng.nextInt(4) + 1` = 1–4 | 50% chance; else null |
| 9 | `rng.nextInt(10) == 0` | `acy.d[acy.bB.bM + rng.nextInt(2)]` | 1 | 10% chance; music disc (ID = acy.bB.bM + 0 or 1); else null |
| 10 | — | `acy.aV` (item) | 1, damage=3 | — |

Roll is `rng.nextInt(11)` (0–10 inclusive). Slots 7, 8, 9 have additional probability guards
and return null on failure. Null items are silently skipped (not placed in chest).

---

## 5. Mob Spawner Type Table — `b(Random rng)`

Returns the entity type string for the mob spawner.

```
roll = rng.nextInt(4)
0 → "Skeleton"
1 → "Zombie"
2 → "Zombie"
3 → "Spider"
```

Effective probabilities: 25% Skeleton, 50% Zombie, 25% Spider.

---

## 6. Bitwise & Data Layouts

No metadata used. All dungeon blocks are placed with metadata 0.

---

## 7. Tick Behaviour

No tick behaviour. The generator runs once during chunk populate and places a static structure.

---

## 8. Known Quirks / Bugs to Preserve

1. **Chest-against-wall requirement:** Chests that cannot find a wall-adjacent position after
   3 tries for each of the 2 attempts are silently dropped. A dungeon can have 0, 1, or 2
   chests depending on the random positions drawn. It is possible (though rare) to place
   both chests in the same slot (second attempt overwrites first).

2. **Chest slot collision:** `chest.setSlot(rng.nextInt(chest.size()), item)` picks a random
   slot for each of the 8 loot rolls. Rolls can land on the same slot, silently overwriting
   the previous item. A chest may contain fewer than 8 distinct items.

3. **Dungeon height is always 3:** The height byte `var6 = 3` is hardcoded. The room is
   always 3 blocks tall internally (4 blocks from floor bottom to ceiling bottom = y-1 to y+3).

4. **Door count includes two-block openings:** The door counter counts positions where both
   the border-column block at y and the border-column block at y+1 are both air. A single
   2-block-tall gap counts as 1 door.

---

## 9. Resolved Questions

1. **`acy.aV` = Dye item (ID 351):** `acy.aV = new xv(95)` → `bM = 256 + 95 = 351`. Class `xv`
   is `ItemDye`. Item name "dyePowder". Loot slot 10 places `new dk(acy.aV, 1, 3)` = 1× Dye with
   damage 3. Damage value 3 = Cocoa Beans (brown dye). No open questions remain on this slot.

2. **`acy.bB.bM` = 2256 ("13"), `acy.bB.bM + 1` = 2257 ("cat"):** `acy.bB = new pe(2000, "13")` →
   bM = 256 + 2000 = 2256. `acy.bC = new pe(2001, "cat")` → bM = 2257. Loot slot 9 uses
   `acy.d[2256 + rng.nextInt(2)]` = either record "13" (ID 2256) or record "cat" (ID 2257).
   These are the two music discs available in 1.0.
