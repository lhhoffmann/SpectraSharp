# BlockFluid Spec
Source: `agw.java` (BlockFluidBase, ~300 lines), `ahx.java` (BlockFluid flowing, 265 lines), `add.java` (BlockStationary still, 59 lines)
Type: Algorithm reference — fluid spreading, source detection, water+lava interactions

---

## 1. Overview

Fluid blocks are split into two classes per fluid:

| Java class | Human name | Block IDs | Notes |
|---|---|---|---|
| `agw` | `BlockFluidBase` | — | Abstract base; material, render, physics helpers |
| `ahx` | `BlockFluid` | 8 (water flowing), 10 (lava flowing) | Tick-driven spreading logic |
| `add` | `BlockStationary` | 9 (water still), 11 (lava still) | Stable blocks; converts to flowing on neighbour change |

Flowing and still versions of the same fluid always differ by exactly 1 in block ID:
- `still = flowing + 1`  (8→9, 10→11)
- When `ahx` stabilises it writes `bM+1` (its own ID + 1).
- When `add` is disturbed it writes `bM-1` (its own ID - 1).

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `agw` | `BlockFluidBase` | Abstract; extends `yy` (Block) |
| `ahx` | `BlockFluid` | Flowing fluid; extends `agw` |
| `add` | `BlockStationary` | Still fluid; extends `agw` |
| `p.g` | `Material.water` | Water material constant |
| `p.h` | `Material.lava` | Lava material constant |

---

## 3. Material Constants (relevant)

| `p` field | Human name | Notes |
|---|---|---|
| `p.g` | `water` | isLiquid; !isSolid; !isFlammable; replaceable |
| `p.h` | `lava` | isLiquid; !isSolid; !isFlammable; replaceable |
| `p.A` | (fire?) | Treated as a solid blocker for fluid flow |
| `p.t` | (unknown) | Skipped in fluid-face rendering check |

---

## 4. Block IDs and Metadata

| Block | ID | Material | Notes |
|---|---|---|---|
| Flowing water | 8 (`yy.A`) | `p.g` | meta = flow level (see §5) |
| Still water | 9 (`yy.B`) | `p.g` | meta = flow level |
| Flowing lava | 10 (`yy.C`) | `p.h` | meta = flow level |
| Still lava | 11 (`yy.D`) | `p.h` | meta = flow level |
| Obsidian | 49 (`yy.ap`) | — | Created by lava source + water |
| Cobblestone | 4 (`yy.w`) | — | Created by lava flowing + water |
| Fire | 51 (`yy.ar`) | — | Spread from still lava tick |

### Flow level encoding

| Meta value | Meaning |
|---|---|
| 0 | **Source block** (infinite, never drains unless removed) |
| 1–7 | Flowing; higher = farther from source = weaker |
| 8 | **Falling bit** — combined with levels: `meta 8` = falling source, `meta 9`–`15` = falling at levels 1–7 |

`meta >= 8` means the block is falling vertically from above.
After removing the falling bit: effective level = `meta - 8` (or 0 if `meta == 8`).

### Level-to-height (`agw.e(meta)`)

```java
float e(int meta):
    if (meta >= 8) meta = 0
    return (meta + 1) / 9.0F    // 1/9 to 8/9 = source fills to top
```

Fluid height for rendering = `1.0 - e(meta)` (source block fills to top = 1.0).

---

## 5. Tick Rates and Flow Distance

| Fluid | Tick delay | Decay per step (var7) | Max flow distance | Notes |
|---|---|---|---|---|
| Water (Overworld) | 5 ticks | 1 | 7 blocks | `d()` = 5 |
| Lava (Overworld) | 30 ticks | 2 | 3–4 blocks | Also 75% skip chance |
| Lava (Nether) | 30 ticks | 1 | 7 blocks | `world.y.d == true` |
| Water (Nether) | 5 ticks | 1 | 7 blocks | Water in Nether behaves normally |

`world.y.d` = WorldProvider boolean flag; `true` in Nether (`WorldProviderHell`).

---

## 6. Helper Methods

### `g(world, x, y, z)` — getFluidLevel (agw, protected)

```
if world.getMaterial(x,y,z) != this.material: return -1
return world.getMetadata(x,y,z)
```

Returns the stored metadata if same material, or -1 if different fluid/solid.

### `c(reader, x, y, z)` — getEffectiveLevel (agw, protected)

```
if material doesn't match: return -1
meta = reader.getMetadata(x,y,z)
if (meta >= 8) meta = 0    // treat falling as source for flow calculations
return meta
```

Used for flow gradient calculation.

### `l(world, x, y, z)` — isBlocked (ahx, private)

Returns `true` if the block at (x,y,z) is an impassable barrier for fluid:
```
blockId = world.getBlockId(x,y,z)
if blockId is in {yy.aE, yy.aL, yy.aD, yy.aF, yy.aX}:
    return true    // signs, doors, fence gates — fluid-passable thin blocks?
if blockId == 0:
    return false   // air → passable
material = block.material
if material == p.A:
    return true    // fire → blocked
return material.isSolid()
```

### `m(world, x, y, z)` — canFlowInto (ahx, private)

```
mat = world.getMaterial(x,y,z)
if mat == this.material: return false    // already same fluid → no
if mat == p.h (lava): return false       // can't flow into lava
return !isBlocked(world, x, y, z)
```

### `f(world, x, y, z, bestSoFar)` — aggregateNeighborLevel (ahx, protected)

Called for each of 4 horizontal neighbors during level computation:
```
level = g(world, x, y, z)
if level < 0: return bestSoFar    // different material → ignore
if level == 0: this.a++           // count adjacent sources
if level >= 8: level = 0         // treat falling as source for min-level tracking
if bestSoFar < 0: return level   // bestSoFar=-100 initially → accept any
return min(bestSoFar, level)
```

`this.a` = source-block counter (reset to 0 at tick start).

---

## 7. `BlockFluid.tick()` — `ahx.a(world, x, y, z, rand)`

The main spreading tick. Only called on **flowing** blocks (IDs 8, 10).

### Step 1: Read current state

```
currentLevel = g(world, x, y, z)   // -1 if removed, 0-7 source/flowing, 8-15 falling
var7 = 1                            // decay per step
if (material == lava && !world.y.d):
    var7 = 2                        // lava in Overworld: double decay

convertToStill = true
```

### Step 2: Compute new level (only if currentLevel > 0, i.e. not source)

```
if (currentLevel > 0):
    neighborMin = -100
    this.a = 0                          // source counter

    // scan 4 horizontal neighbors
    neighborMin = f(world, x-1, y, z, neighborMin)
    neighborMin = f(world, x+1, y, z, neighborMin)
    neighborMin = f(world, x, y, z-1, neighborMin)
    neighborMin = f(world, x, y, z+1, neighborMin)
    
    newLevel = neighborMin + var7
    if (newLevel >= 8 || neighborMin < 0):
        newLevel = -1     // no valid source → remove block

    // Check block above
    aboveLevel = g(world, x, y+1, z)
    if (aboveLevel >= 0):
        if (aboveLevel >= 8): newLevel = aboveLevel      // falling above → propagate falling
        else:                 newLevel = aboveLevel + 8  // non-falling above → become falling

    // Infinite water source rule (water only):
    if (this.a >= 2 && material == water):
        if world.getMaterial(x, y-1, z).isSolid():
            newLevel = 0     // 2+ adjacent sources, solid floor → create source
        elif world.getMaterial(x, y-1, z) == water && world.getMetadata(x, y, z) == 0:
            newLevel = 0     // 2+ adjacent sources, water below, self is source → keep source

    // Lava slow-flow (Overworld lava only):
    if (material == lava && currentLevel < 8 && newLevel < 8 && newLevel > currentLevel
        && rand.nextInt(4) != 0):        // 75% chance to stay at current level
        newLevel = currentLevel
        convertToStill = false

    // Apply level change
    if (newLevel != currentLevel):
        currentLevel = newLevel
        if (newLevel < 0):
            world.setBlock(x, y, z, 0)   // remove
        else:
            world.setBlockMetadata(x, y, z, newLevel)
            world.setBlock(x, y, z, this.id, this.getTickDelay())
            world.notifyNeighbors(x, y, z, this.id)
    elif (convertToStill):
        convertToStill_action(world, x, y, z)    // see §9

else:
    // Source block (level 0) → also try to convert to still if stable
    convertToStill_action(world, x, y, z)
```

### Step 3: Flow downward

```
if canFlowInto(world, x, y-1, z):
    // Lava + water below → solid interaction
    if (material == lava && world.getMaterial(x, y-1, z) == water):
        world.setBlock(x, y-1, z, stone)     // cobblestone/obsidian handled elsewhere
        fizz_effect(world, x, y-1, z)
        return

    if (currentLevel >= 8):
        world.placeFluid(x, y-1, z, this.id, currentLevel)    // falling: keep level
    else:
        world.placeFluid(x, y-1, z, this.id, currentLevel + 8) // start falling: level + 8

elif (currentLevel >= 0 && (currentLevel == 0 || !canFlowInto(world, x, y-1, z))):
    // Cannot fall → try lateral spreading
    // (Step 4)
```

### Step 4: Flow laterally (only if cannot fall, or is source)

```
directions = k(world, x, y, z)    // see §8 — which dirs are shortest-path-to-drop
spreadLevel = currentLevel + var7
if (currentLevel >= 8): spreadLevel = 1   // falling water spreads at level 1
if (spreadLevel >= 8): return             // can't spread further

for each dir in {-x, +x, -z, +z}:
    if directions[dir]:
        placeFluid(world, neighbor, this.id, spreadLevel)
```

`placeFluid(world, x, y, z, id, level)` (private `g()`):
```
if canFlowInto(world, x, y, z):
    existingId = world.getBlockId(x, y, z)
    if (existingId > 0):
        if (material == lava):
            fizz_effect(world, x, y, z)
        else:
            block.dropItem(world, x, y, z, meta, 0)   // non-lava: drops items
    world.setBlockAndMetadata(x, y, z, id, level)
```

---

## 8. Flow Direction Algorithm — `k(world, x, y, z)`

Determines which of the 4 horizontal directions fluid will flow into.
Uses a BFS flood-fill (max depth 4) to find the nearest block from which it can fall.

```
for each dir in {-x, +x, -z, +z}:
    nx, nz = neighbor in that dir
    
    if isBlocked(nx, y, nz):
        distance[dir] = 1000    // blocked → infinite
    elif world.getMaterial(nx,y,nz) == this.material && world.getMetadata(nx,y,nz) == 0:
        distance[dir] = 1000    // source block of same fluid → infinite
    else:
        if !isBlocked(nx, y-1, nz):
            distance[dir] = 0   // can fall immediately from that cell → best
        else:
            distance[dir] = c(world, nx, y, nz, 1, dir)  // recursive search

minDist = min(all 4 distances)
cb[dir] = (distance[dir] == minDist)    // enable dirs with shortest path
return cb
```

### Recursive flood-fill `c(world, x, y, z, depth, fromDir)`

```
if depth > 4: return 1000    // search depth limit

for each dir in {0,1,2,3} excluding the reverse of fromDir:
    nx = x + dir_offset_x[dir]
    nz = z + dir_offset_z[dir]

    if (!isBlocked(nx, y, nz)
        && NOT (same material AND meta==0)):    // not a source of same fluid
        
        if !isBlocked(nx, y-1, nz):
            return depth    // found a drop → return current depth
        elif depth < 4:
            result = c(world, nx, y, nz, depth+1, dir)
            minDist = min(minDist, result)

return minDist    // 1000 if no drop found within 4 steps
```

Flow directions with the shortest path to a drop all get flow enabled simultaneously.
If no drop within 4 steps: distance remains 1000 (none enabled → fluid pools).

---

## 9. Stabilisation: `convertToStill_action` — `ahx.j()`

Called when a flowing block's level is stable (not changed this tick):

```java
private void j(world, x, y, z):
    meta = world.getMetadata(x, y, z)
    world.setBlockAndMetadata(x, y, z, this.id + 1, meta)    // id+1 = still variant
    world.notifyRenderListeners(x, y, z)
    world.notifyNeighbors(x, y, z)
```

Converts flowing (8/10) to still (9/11). Still blocks don't tick — they rely on
neighbor-change events to convert back to flowing (see §10).

---

## 10. `BlockStationary.onNeighborChange` — `add.a(world, x, y, z, sourceBlockId)`

Called when any adjacent block changes:

```java
void a(world, x, y, z, sourceId):
    if world.getBlockId(x, y, z) == this.id:
        j(world, x, y, z)    // convert back to flowing

private void j(world, x, y, z):
    meta = world.getMetadata(x, y, z)
    world.t = true                               // suppress entity notifications
    world.setBlockAndMetadata(x, y, z, id-1, meta)   // id-1 = flowing variant (9→8, 11→10)
    world.notifyRenderListeners(x, y, z)
    world.scheduleBlockTick(x, y, z, id-1, delay)    // begin flowing tick
    world.t = false
```

`world.t = true` is a "static update mode" flag that suppresses certain notifications.
Still fluid also schedules itself: the flowing block will do the first actual spread tick.

---

## 11. Lava + Water Interaction — `agw.j()` (onBlockAdded / onNeighborChanged)

Called for both `ahx` and `add` when added or when a neighbor changes:

```java
// Base class agw handles lava-specific interactions
if (material == lava && blockAt(x,y,z) == this.id):
    waterAdjacent = false
    check: (x,y,z-1), (x,y,z+1), (x-1,y,z), (x+1,y,z), (x,y+1,z)
    if any has water material:
        waterAdjacent = true
    
    if waterAdjacent:
        meta = world.getMetadata(x, y, z)
        if meta == 0:
            world.setBlock(x, y, z, obsidian)    // yy.ap = 49
        elif meta <= 4:
            world.setBlock(x, y, z, cobblestone) // yy.w = 4
        
        play "random.fizz" sound
        spawn 8 largesmoke particles
```

**Rules:**
- Lava **source** (meta 0) + adjacent water → Obsidian (ID 49)
- Lava **flowing** (meta 1–4) + adjacent water → Cobblestone (ID 4)
- Higher-level flowing lava (meta 5–7) does NOT create cobblestone via this path

Also in `ahx.a()` (the tick), when flowing downward into a water block:
```
if material == lava && world.getMaterial(x, y-1, z) == water:
    world.setBlock(x, y-1, z, stone)    // yy.t.bM = 1
    fizz_effect(...)
    return
```

---

## 12. Still Lava Random Tick — Fire Spread (`add.a(world, x,y,z,rand)`)

Only for lava (`p.h`). On random tick, attempts to ignite nearby air blocks:

```java
int steps = rand.nextInt(3)    // 0, 1, or 2 steps
for (i = 0; i < steps; i++):
    x += rand.nextInt(3) - 1   // ±1 lateral each step
    y += 1                      // always moves upward
    z += rand.nextInt(3) - 1

    blockId = world.getBlock(x, y, z)
    if blockId == 0 (air):
        // Check 6 faces for flammable material
        if any adjacent block has isFlammable material:
            world.setBlock(x, y, z, fire)    // yy.ar.bM = 51
            return
    elif block.material.isSolid():
        return    // hit a solid → stop walk
```

---

## 13. BlockFluidBase Properties (`agw`)

| Property | Value | Notes |
|---|---|---|
| `b()` = isOpaqueCube | `false` | Never fully opaque |
| `a()` = renderAsNormalBlock | `false` | Custom rendering |
| Bounding box | 0,0,0 → 1,1,1 (full) | Set in constructor |
| Light opacity (`d()`) for rendering | `5` (water) / `30` (lava) | Used for face culling and light absorption |
| Tick delay (`d()`) | `5` (water) / `30` (lava) | In game ticks |
| isTickable | `true` (via `b(true)` in constructor) | Flowing blocks always tick |
| Drop count | 0 | Neither fluid drops anything |
| Texture index (water) | `12*16+13 = 205` | |
| Texture index (lava) | `14*16+13 = 237` | |
| Water colour | from biome `A` field | `sr.A` = waterColor |
| Lava colour | `16777215` (white) | |

### Texture variant (`agw.b(meta)`)

```
if meta == 0 || meta == 1: return bL      // still/nearly-still → base texture
else:                       return bL + 1  // flowing → animated texture
```

---

## 14. Fluid Flow Gradient (`agw.g(kq, x, y, z)`) — For Rendering

Computes a 3D flow direction vector for surface rendering (wave direction):

```
effectiveLevel = c(reader, x, y, z)   // level at current position
for each of 4 horizontal neighbors:
    neighborLevel = c(reader, nx, y, nz)
    if neighborLevel < 0 AND NOT solid:   // fluid flows outward here
        // also try one below: c(nx, y-1, nz)
        if found: delta = neighborLevel - (effectiveLevel - 8)
        vector += (neighbor_dir * delta)
    else if neighborLevel >= 0:
        delta = neighborLevel - effectiveLevel
        vector += (neighbor_dir * delta)

// Falling: if meta >= 8 AND solid face above → tip vector downward
if meta >= 8 AND any solid face on sides or above diagonal:
    vector = normalize(vector) + (0, -6, 0)

return normalize(vector)
```

Used by the entity physics update (`ia.a` calls `agw.a(world, x,y,z, entity, vel)`) to apply current drag.

---

## 15. Quirks to Preserve

- **Flowing and still differ by ID+1:** always `still = flowing + 1`. This means Block 8/10 tick, 9/11 don't. Conversion is explicit in code.
- **Lava 75% skip:** Even when lava level could increase, 75% of ticks it stays the same. This makes Overworld lava feel extremely slow even beyond the 30-tick rate.
- **Lava Nether speed:** In Nether (`world.y.d == true`), lava uses `var7 = 1` like water — it spreads 7 blocks and relatively quickly. Overworld lava uses `var7 = 2` → max 3–4 block reach.
- **Infinite water:** Exactly 2 adjacent source blocks (meta 0) + solid floor below = new source. This makes bucket-filling lakes possible. Does NOT apply to lava.
- **Still block suppresses entities (`t = true`):** When `add.j()` converts still to flowing, it temporarily sets `world.t = true`. This prevents sound/particle events that would normally fire on SetBlock.
- **Flood-fill depth cap 4:** Fluid searches at most 4 blocks horizontally for a drop before giving up and spreading in all valid directions equally.
- **Reverse direction excluded in flood-fill:** `c()` never looks back where it came from, preventing infinite recursion via back-tracking.
- **Still blocks schedule via flowing block:** `add.j()` writes the flowing ID and schedules a tick for it — still blocks themselves never tick on a schedule.
- **Lava source only makes obsidian, flowing makes cobblestone:** Meta 0 lava → obsidian; meta 1-4 → cobblestone; meta 5-7 flows but makes nothing on contact.

---

## 16. World Method Reference (used by fluid classes)

| Call | Meaning |
|---|---|
| `world.d(x,y,z)` | getBlockMetadata |
| `world.e(x,y,z)` | getMaterial (returns `p`) |
| `world.a(x,y,z)` | getBlockId |
| `world.g(x,y,z,id)` | setBlock (no meta, notifies) |
| `world.f(x,y,z,meta)` | setBlockMetadata only |
| `world.b(x,y,z,id,meta)` | setBlockAndMetadata (notifies) |
| `world.d(x,y,z,id,meta)` | setBlockAndMetadata **silent** (world-gen variant?) |
| `world.a(x,y,z,id,delay)` | scheduleBlockTick |
| `world.c(x1,y1,z1,x2,y2,z2)` | notifyRenderListeners (range) |
| `world.j(x,y,z)` | notifyNeighbors (6 faces, block-change event) |
| `world.j(x,y,z,id)` | notifyNeighborsOf (pass source block ID) |
| `world.t` | boolean flag: static update mode (suppresses some notifications) |
| `world.y.d` | WorldProvider flag: `true` in Nether |
| `world.f(x,y,z)` | boolean: isBlockIndirectlyGettingPowered? (used in lava neighbour fire check) |
| `world.g(x,y,z)` | boolean: isBlockNormalCube? (used in drip particle logic) |

---

*Spec written by Analyst AI from `agw.java` (~300 lines), `ahx.java` (265 lines), `add.java` (59 lines). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — BlockFluid)*
