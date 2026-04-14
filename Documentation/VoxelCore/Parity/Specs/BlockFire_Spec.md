# BlockFire Spec
Source: `wj.java` (BlockFire, 289 lines)
Type: Algorithm reference — fire placement, spread, age/burnout, flammability tables

---

## 1. Overview

`wj` (BlockFire) is the fire block (ID 51). It ticks every **40 game ticks** (2 seconds),
aging and spreading to flammable neighbours. Fire is extinguished by rain, by losing its
supporting block, or by reaching max age without a supporting surface.

Fire is permanently sustained on netherrack (or end-stone in the End dimension).

---

## 2. Class Identifier

| Obfuscated | Human name | ID | Notes |
|---|---|---|---|
| `wj` | `BlockFire` | 51 (`yy.ar`) | extends `yy` (Block); material `p.n` (fire) |

---

## 3. Flammability Tables

Two `int[256]` arrays stored as instance fields on the BlockFire singleton:

| Array | Name | Meaning |
|---|---|---|
| `a[blockId]` | Flammability / encouragement | How easily a block catches fire. Used to check if a face can sustain fire and to compute ignition probability. |
| `cb[blockId]` | Burnability / burn speed | How quickly a block is consumed once on fire. Used in the direct-face burn roll. |

### Populated values (via `x_()` static initializer):

| Block field | Human name | Flammability `a[]` | Burnability `cb[]` |
|---|---|---|---|
| `yy.x` | Wood Planks (ID 5) | 5 | 20 |
| `yy.aZ` | Bookshelf (ID 47) | 5 | 20 |
| `yy.at` | Wooden Fence (ID 85) | 5 | 20 |
| `yy.J` | TNT (ID 46) | 5 | 5 |
| `yy.K` | Leaves (ID 18) | 30 | 60 |
| `yy.an` | Wool / Cloth (ID 35) | 30 | 20 |
| `yy.am` | *(see note)* | 15 | 100 |
| `yy.X` | *(high flammability)* | 60 | 100 |
| `yy.ab` | *(medium flammability)* | 30 | 60 |
| `yy.bu` | Dead Bush (ID 32) | 15 | 100 |

All unregistered IDs default to 0 (not flammable, not burnable).

> **Note on `yy.am`:** When a block with ID `yy.am.bM` is burned (consumed), `yy.am.e(world, x, y, z, 1)` is called on it — a virtual method that triggers special behaviour on that block type (e.g., TNT detonation on the block, or sapling growth notification). Exact ID to be verified against BlockRegistry_Spec.

---

## 4. Helper Methods

### `c(IBlockAccess, x, y, z)` — isFlammable

```
return a[reader.getBlockId(x, y, z)] > 0
```

Returns true if that block can be set on fire (has any flammability).

### `f(world, x, y, z, best)` — getFlammability

```
flam = a[world.getBlockId(x, y, z)]
return max(flam, best)
```

Returns the higher of the block's flammability and the running maximum.

### `h(world, x, y, z)` — maxFlammabilityAround

```
if world.getBlockId(x, y, z) != 0 (air):
    return 0    // target must be air to place fire there

best = 0
for each of 6 faces:
    best = f(world, neighbor, best)
return best
```

Returns the highest flammability value of the 6 faces adjacent to (x,y,z),
or 0 if the position is not air. Used to compute ignition chance for area spread.

### `g(world, x, y, z)` — hasFlammableNeighbor

```
for each of 6 faces:
    if c(world, neighbor):    // a[blockId] > 0
        return true
return false
```

Returns true if any adjacent block is flammable.

### `c(World, x, y, z)` — canFireSurviveHere (3-arg, override)

```
return world.isBlockNormalCube(x, y-1, z)    // solid full-cube below
       || hasFlammableNeighbor(world, x, y, z)
```

Fire can exist at a position if it has solid ground OR an adjacent flammable block.

---

## 5. Block Properties

| Property | Value | Notes |
|---|---|---|
| Material | `p.n` | Fire material; not solid, not replaceable |
| `a()` = renderAsNormalBlock | `false` | Custom rendering |
| `b()` = isOpaqueCube | `false` | Never opaque |
| Tick delay (`d()`) | **40** ticks (2 seconds) | Always re-schedules itself |
| `bBoundingBox` | `null` | No collision box |
| Drop count | 0 | Fire drops nothing |
| Light emission | 15 | (set in block constructor, not overridden in wj) |

---

## 6. Fire Tick: `a(world, x, y, z, rand)` — Main Spread Algorithm

### Step 1: Determine if fire is permanent

```
isPermanent = (world.getBlockId(x, y-1, z) == yy.bb.bM)   // netherrack below
if world.y instanceof EndDimension AND blockBelow == yy.z.bM:
    isPermanent = true    // end-stone below in End dimension (beacon fire)
```

`yy.bb` = netherrack (ID 87). Fire on netherrack never burns out.

### Step 2: Existence check

```
if !canFireSurviveHere(world, x, y, z):
    world.setBlock(x, y, z, 0)    // remove if no support
    return
```

### Step 3: Rain check

```
isWet = world.isRaining()
       AND world.isBlockWet(x,y,z)
       AND world.isBlockWet(x-1,y,z)
       AND world.isBlockWet(x+1,y,z)
       AND world.isBlockWet(x,y,z-1)
       AND world.isBlockWet(x,y,z+1)

if (!isPermanent AND isWet):
    world.setBlock(x, y, z, 0)   // rain extinguishes
    return
```

Rain only extinguishes if ALL 5 positions (self + 4 horizontal) are wet. Fire under cover
(even partial) from rain survives.

### Step 4: Age the fire

```
age = world.getBlockMetadata(x, y, z)
if age < 15:
    age = age + rand.nextInt(3) / 2    // += 0 or 1 each tick
world.setBlockMetadata(x, y, z, age)
world.scheduleBlockTick(x, y, z, fireId, 40)
```

Age advances by 0 or 1 per tick. Reaches maximum (15) after approximately 15–30 ticks.

### Step 5: Burnout check (non-permanent only)

```
if !isPermanent:
    if !hasFlammableNeighbor(world, x, y, z):
        // No adjacent flammable block
        if !world.isBlockNormalCube(x, y-1, z) OR age > 3:
            world.setBlock(x, y, z, 0)    // remove: no support and getting old
            return
    elif !world.isBlockNormalCube(x, y-1, z)   // not on solid floor
         AND age == 15
         AND rand.nextInt(4) == 0:             // 25% chance at max age
        world.setBlock(x, y, z, 0)            // burnout
        return
```

Non-permanent fire eventually burns out when it reaches max age, especially without ground.

### Step 6: Spread to 6 direct neighbours

```
burnBlock(world, x+1, y, z, divisor=300, rand, age)
burnBlock(world, x-1, y, z, divisor=300, rand, age)
burnBlock(world, x, y-1, z, divisor=250, rand, age)
burnBlock(world, x, y+1, z, divisor=250, rand, age)
burnBlock(world, x, y, z-1, divisor=300, rand, age)
burnBlock(world, x, y, z+1, divisor=300, rand, age)
```

Up and down use divisor 250 (slightly higher chance than horizontal 300).

### Step 7: Area fire spread (3×6×3 box)

```
for bx in [x-1, x+1]:
    for bz in [z-1, z+1]:
        for by in [y-1, y+4]:
            if (bx, by, bz) == (x, y, z): continue

            baseDivisor = 100
            if by > y+1:
                baseDivisor += (by - (y+1)) * 100   // 200, 300, 400 for y+2, y+3, y+4

            flam = maxFlammabilityAround(world, bx, by, bz)
            if flam > 0:
                igniteChance = (flam + 40) / (age + 30)
                if igniteChance > 0
                   AND rand.nextInt(baseDivisor) <= igniteChance
                   AND (!world.isRaining() OR !world.isBlockWet(bx, by, bz)):
                    newAge = min(age + rand.nextInt(5)/4, 15)   // age + 0 or 1
                    world.setBlockAndMetadata(bx, by, bz, fireId, newAge)
```

Blocks higher than y+1 are progressively harder to ignite (baseDivisor grows by 100 per level).
The `maxFlammabilityAround` check ensures only **air** positions can receive fire, with at least one flammable adjacent face.

---

## 7. `burnBlock(world, x, y, z, divisor, rand, age)` — Consume Adjacent Block

Called for the 6 direct faces of the fire block:

```
burnSpeed = cb[world.getBlockId(x, y, z)]
if rand.nextInt(divisor) < burnSpeed:
    isSpecialBlock = (blockId == yy.am.bM)

    if rand.nextInt(age + 10) < 5 AND !world.isBlockWet(x, y, z):
        // Place fire at consumed block's position
        newAge = min(age + rand.nextInt(5)/4, 15)
        world.setBlockAndMetadata(x, y, z, fireId, newAge)
    else:
        // Consume block (remove it)
        world.setBlock(x, y, z, 0)

    if isSpecialBlock:
        yy.am.e(world, x, y, z, 1)   // special action (e.g., TNT prime)
```

**Interpretation:**
- Roll `rand.nextInt(divisor) < cb[id]`: higher burnability → higher chance to trigger.
- If triggered: 1/(age+10) * 5 chance to **spread fire** to that position; otherwise **destroy** the block.
- At low fire age: more likely to spread fire. At high age: more likely to just destroy.
- Wet blocks are never replaced with fire (only destroyed).

---

## 8. `onBlockAdded` — `a(world, x, y, z)`

Called when fire is placed (e.g., by flint-and-steel, lava spread, or lightning):

```java
void a(world, x, y, z):
    // Special End portal case: skip if on obsidian+End frame
    if world.y.g > 0 OR blockBelow != yy.ap.bM OR !EndPortalFrame.generate(world, x, y, z):
        if !isBlockNormalCube(below) AND !hasFlammableNeighbor(world, x, y, z):
            world.setBlock(x, y, z, 0)   // invalid placement → remove immediately
        else:
            world.scheduleBlockTick(x, y, z, fireId, 40)
```

`world.y.g > 0` = dimension ID check (0 = Overworld; the End has a different ID).

---

## 9. `onNeighborChange` — `a(world, x, y, z, neighborId)`

```java
if !isBlockNormalCube(below) AND !hasFlammableNeighbor(world, x, y, z):
    world.setBlock(x, y, z, 0)
```

Fire removes itself if its support disappears (e.g., the flammable block it was on is broken).

---

## 10. Ignition Probability Formula

For area spread (Step 7), the chance to ignite a position per tick:

```
igniteChance = (flammability + 40) / (age + 30)
passChance   = 1 / baseDivisor   // where baseDivisor = 100 + 100*(y-offset above y+1)
fires if rand.nextInt(baseDivisor) <= igniteChance
```

At age=0 (new fire): `igniteChance = (flam + 40) / 30` → very aggressive spread.
At age=15 (old fire): `igniteChance = (flam + 40) / 45` → slower spread.

Higher flammability blocks (leaves=30, wool=30 → igniteChance ≈ 2.3 at age=0) spread rapidly.
Lower flammability (planks=5 → igniteChance ≈ 1.5 at age=0) spread more slowly.

---

## 11. Rain and Wetness

| Call | Meaning |
|---|---|
| `world.E()` | `isRaining()` — global rain flag |
| `world.w(x, y, z)` | `isBlockWet(x,y,z)` — block has rain exposure (sky-visible + raining) |

Rain extinguishes fire only when ALL 5 positions (fire + 4 horizontal) are wet.
Wet blocks can still be destroyed by fire but cannot have fire placed on them.

---

## 12. Permanent Fire

| Condition | Block | Notes |
|---|---|---|
| Overworld | Netherrack (`yy.bb`, ID 87) | Fire below | Fire below is netherrack → `isPermanent = true` |
| End | End stone (`yy.z`, ID 121?) | Only when `world.y instanceof EndDimension` |

Permanent fire skips age-based burnout (steps 4 and 5 still run for aging and scheduling,
but the burnout paths are skipped). Permanent fire still spreads normally.

---

## 13. Quirks to Preserve

- **Rain requires 5-position wetness**: fire in a 1-wide gap or under partial cover can survive rain even if adjacent blocks are wet.
- **Age passed to new fire**: when area-spreading, new fire starts at `age + 0 or 1` — not at 0. Fire spread from old fire burns out faster.
- **Divisor = 100 per Y above y+1**: fire ignites blocks at y+2, y+3, y+4 with 2×, 3×, 4× harder rolls. Upward spread is only significantly slowed for blocks 2+ levels above.
- **Face consume vs spread**: `burnBlock()` either replaces the consumed block with fire OR destroys it, weighted by age. Young fire is more likely to spread (re-place fire); old fire is more likely to just eat the block.
- **`yy.am` special action**: burning this specific block calls `yy.am.e(world, x, y, z, 1)` which triggers some extra effect on that block type. Verify against BlockRegistry.
- **`isBlockNormalCube`**: `world.g(x, y, z)` — only full-cube solid blocks count as "ground". Stairs, slabs, glass, etc. do not sustain fire from below.
- **No bounding box**: `b(ry, x, y, z)` returns null (no AABB). Fire is a purely visual, non-collidable block.

---

## 14. World Method Reference

| Call | Meaning |
|---|---|
| `world.g(x,y,z)` (boolean) | `isBlockNormalCube` — is it a full solid cube? |
| `world.g(x,y,z,id)` (void) | `setBlock` (no meta) |
| `world.d(x,y,z,id,meta)` | `setBlockAndMetadata` (silent) |
| `world.c(x,y,z,meta)` | `setBlockMetadata` only |
| `world.a(x,y,z,id,delay)` | `scheduleBlockTick` |
| `world.d(x,y,z)` | `getBlockMetadata` |
| `world.E()` | `isRaining()` |
| `world.w(x,y,z)` | `isBlockWet(x,y,z)` — exposed to rain |
| `world.y` | `WorldProvider` — check `instanceof ol` for End dimension |
| `world.y.g` | dimension ID (0=Overworld, -1=Nether, 1=End) |

---

*Spec written by Analyst AI from `wj.java` (289 lines). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — BlockFire)*
