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

# MobAI + PathFinder Spec
**Source classes:** `ww.java` (EntityAI), `zo.java` (EntityMonster), `fx.java` (EntityAnimal),
`rw.java` (PathFinder), `mo.java` (PathPoint), `dw.java` (PathEntity),
`zs.java` (PathHeap), `ob.java` (PathNodeCache), `xk.java` (ChunkCache)
**Superclass chain:** `nq` → `ww` → `zo` / `fx` → concrete mobs
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

This spec covers the complete mob AI pipeline: target acquisition, A* pathfinding, path
following, movement steering, and the concrete logic for hostile mobs (`zo`) and animals
(`fx`). All mobs extend `ww` (EntityAI) which provides the pathfinder and target fields.
The pathfinder runs inside `World.a(entity, target, range)` using a pre-built chunk cache
(`xk`) as the world view.

---

## 2. Class Hierarchy and Roles

```
nq (LivingEntity)
 └── ww (EntityAI)        — pathfinder field, target field, AI tick n()
      ├── zo (EntityMonster)  — hostile: target nearest player, melee attack
      └── fx (EntityAnimal)   — passive: breed, follow player with food, flee
```

Supporting data types (no entity inheritance):
- `rw` = PathFinder — A* algorithm
- `mo` = PathPoint — graph node
- `dw` = PathEntity — completed path (ordered mo[] array)
- `zs` = PathHeap — binary min-heap open set for A*
- `ob` = PathNodeCache — int-keyed hash map node cache (closed set/dedup)
- `xk` = ChunkCache — IBlockAccess snapshot for pathfinding, read from `world.c(cx,cz)`

---

## 3. Fields

### `ww` (EntityAI) instance fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `dw` | null | Current active path; null when idle |
| `h` | `ia` | null | Current target entity |
| `i` | `boolean` | false | isAngry / is-in-attack-range flag; set by `az()` each tick |
| `by` | `int` | 0 | Panic timer; decrements each tick; doubles move speed while > 0 |

### `mo` (PathPoint) fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | — | X coordinate (block) |
| `b` | `int` | — | Y coordinate (block) |
| `c` | `int` | — | Z coordinate (block) |
| `j` | `int` | `a(a,b,c)` | Packed hash key (computed at construction, immutable) |
| `d` | `int` | -1 | Index in heap array `zs.a[]`; -1 = not in heap |
| `e` | `float` | 0 | g-cost (total path cost from start to this node) |
| `f` | `float` | 0 | h-cost (heuristic: Euclidean distance to target) |
| `g` | `float` | 0 | f-cost = e + f; key used for heap ordering |
| `h` | `mo` | null | Parent node (backtrack chain for path reconstruction) |
| `i` | `boolean` | false | Closed flag (node has been expanded) |

`mo.a(int x, int y, int z)` static hash:
```
return (y & 0xFF) | ((x & 32767) << 8) | ((z & 32767) << 24)
       | (x < 0 ? Integer.MIN_VALUE : 0)
       | (z < 0 ? 32768 : 0)
```

### `dw` (PathEntity) fields

| Field | Type | Semantics |
|---|---|---|
| `b` | `mo[]` | Ordered array of path nodes from start to target |
| `a` | `int` | Total number of nodes (`b.length`) |
| `c` | `int` | Current position index (advances as mob follows path) |

### `zs` (PathHeap — open set) fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `mo[]` | new mo[1024] | Heap array; doubles when full |
| `b` | `int` | 0 | Current heap size |

### `ob` (PathNodeCache — node cache) fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `aei[]` | new aei[16] | Hash bucket array |
| `b` | `int` | 0 | Entry count |
| `c` | `int` | 12 | Resize threshold |
| `d` | `float` | 0.75F | Load factor |
| `f` | `Set` | new HashSet() | Key tracking set |

### `rw` (PathFinder) fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `kq` | World view (IBlockAccess, typically `xk` ChunkCache) |
| `b` | `zs` | PathHeap open set (reused, cleared per search) |
| `c` | `ob` | PathNodeCache (reused, cleared per search) |
| `d` | `mo[32]` | Scratch array for neighbor expansion (max 4 used per step) |

### `xk` (ChunkCache) fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `int` | Minimum chunk X index (bbox.minX >> 4) |
| `b` | `int` | Minimum chunk Z index (bbox.minZ >> 4) |
| `c` | `zx[][]` | Pre-fetched chunks: c[cx - a][cz - b] |
| `d` | `ry` | World reference (for world height, sky-darkening, etc.) |

---

## 4. PathFinder A* Algorithm — `rw`

### `world.a(ia entity, ia target, float range)` — path request (entity target)

Called by `ww.n()` to get a path to a target entity.

```
entityX = floor(entity.s)
entityY = floor(entity.t)
entityZ = floor(entity.u)
margin = (int)(range + 16)

bboxMin = (entityX - margin, entityY - margin, entityZ - margin)
bboxMax = (entityX + margin, entityY + margin, entityZ + margin)

chunkCache = new xk(world, bboxMin.x, bboxMin.y, bboxMin.z,
                           bboxMax.x, bboxMax.y, bboxMax.z)
return new rw(chunkCache).a(entity, target, range)
```

### `world.a(ia entity, int x, int y, int z, float range)` — path request (coordinate target)

Called by `ww.aA()` (stroll). Same as above but target is a fixed coordinate.

```
margin = (int)(range + 8)    // note: 8 not 16 for coordinate-target version
// ... same chunkCache construction ...
return new rw(chunkCache).a(entity, x, y, z, range)
```

### `rw.a(ia entity, double targetX, double targetY, double targetZ, float range)` — core A*

```
// Start node: entity's AABB minimum corner
startNode = getOrCreate(floor(entity.aabb.minX), floor(entity.aabb.minY), floor(entity.aabb.minZ))

// Target node: centered on target position
targetNode = getOrCreate(
    floor(targetX - (entity.width / 2.0F)),
    floor(targetY),
    floor(targetZ - (entity.width / 2.0F))
)

// Size node: entity's occupied block footprint (used for collision testing)
sizeNode = new mo(
    ceil(entity.width + 1.0F),
    ceil(entity.height + 1.0F),
    ceil(entity.width + 1.0F)
)

return a(entity, startNode, targetNode, sizeNode, range)
```

### `rw.a(entity, startNode, targetNode, sizeNode, range)` — A* main loop

```
startNode.e = 0                          // g-cost = 0
startNode.f = startNode.a(targetNode)    // h-cost = Euclidean dist to target
startNode.g = startNode.f               // f-cost = g + h
heap.clear()
heap.add(startNode)
closestNode = startNode                 // tracks best partial path

while NOT heap.isEmpty():
    current = heap.poll()               // extract min-f node

    if current.equals(targetNode):      // goal reached
        return reconstructPath(startNode, targetNode)

    if current.a(targetNode) < closestNode.a(targetNode):
        closestNode = current           // track closest reached

    current.i = true                    // mark closed

    neighborCount = expandNeighbors(entity, current, sizeNode, targetNode, range)

    for i in 0..neighborCount-1:
        neighbor = scratch[i]
        if neighbor.i: continue                       // already closed
        if neighbor.a(targetNode) >= range: continue  // outside search range

        tentativeG = current.e + current.a(neighbor)  // Euclidean step cost
        if NOT neighbor.a() OR tentativeG < neighbor.e:
            // Better path found to neighbor
            neighbor.h = current          // set parent
            neighbor.e = tentativeG       // update g
            neighbor.f = neighbor.a(targetNode)  // update h
            if neighbor.a():              // already in heap (has index d >= 0)
                heap.update(neighbor, tentativeG + neighbor.f)  // re-sort
            else:
                neighbor.g = tentativeG + neighbor.f  // f-cost
                heap.add(neighbor)

// No direct path found — return partial path to closest node
if closestNode == startNode:
    return null     // no reachable node at all
else:
    return reconstructPath(startNode, closestNode)
```

**Note:** `mo.a()` (boolean) checks `d >= 0` which means the node is currently in the heap.

### `expandNeighbors(entity, current, sizeNode, targetNode, range)` — 4-directional

Expands only 4 horizontal neighbors (N/S/E/W — no diagonal movement, no vertical steps
as first-class moves):

```
climbOffset = 0
if checkWalkability(entity, current.a, current.b + 1, current.c, sizeNode) == 1:
    climbOffset = 1     // one block step-up available

neighborCount = 0

// Attempt 4 horizontal neighbors
for (dx, dz) in [(0,1), (-1,0), (1,0), (0,-1)]:
    node = tryNeighbor(entity, current.a+dx, current.b, current.c+dz, sizeNode, climbOffset)
    if node != null AND NOT node.i AND node.a(targetNode) < range:
        scratch[neighborCount++] = node

return neighborCount
```

### `tryNeighbor(entity, x, y, z, sizeNode, climbOffset)` — walkability + step-down

```
node = null

if checkWalkability(entity, x, y, z, sizeNode) == 1:
    node = getOrCreate(x, y, z)

if node == null AND climbOffset > 0 AND checkWalkability(entity, x, y+climbOffset, z, sizeNode) == 1:
    // Step up by climbOffset
    node = getOrCreate(x, y+climbOffset, z)
    y += climbOffset

if node != null:
    // Step-down loop: find solid floor
    stepDownCount = 0
    floorResult = 0

    while y > 0 AND (floorResult = checkWalkability(entity, x, y-1, z, sizeNode)) == 1:
        stepDownCount++
        if stepDownCount >= 4:
            return null    // drop too large
        y--
        node = getOrCreate(x, y, z)

    if floorResult == -2:
        return null    // floor is lava/danger: reject

return node
```

### `checkWalkability(entity, x, y, z, sizeNode)` — bounding-box block scan

Scans the entity's full bounding box at position (x, y, z):

```
for bx in x .. x + sizeNode.a - 1:
    for by in y .. y + sizeNode.b - 1:
        for bz in z .. z + sizeNode.c - 1:
            blockId = world.getBlockId(bx, by, bz)
            if blockId > 0:
                // Special case: doors (yy.aL = woodDoor ID 64, yy.aE = ironDoor ID 71)
                if blockId == woodDoorId OR blockId == ironDoorId:
                    meta = world.getBlockMeta(bx, by, bz)
                    if NOT BlockDoor.isOpen(meta):
                        return 0    // closed door = blocked
                else:
                    material = Block.registry[blockId].material
                    if material.isSolid():
                        return 0    // solid = blocked
                    if material == Material.Water:
                        return -1   // water = passable but avoid
                    if material == Material.Lava:
                        return -2   // lava = danger

return 1    // all clear
```

**Constants referenced:**
- `yy.aL.bM` = wooden door block ID (64)
- `yy.aE.bM` = iron door block ID (71)
- `p.g` = water material
- `p.h` = lava material
- `uc.g(meta)` = `BlockDoor.isOpen(meta)` = `(meta & 4) != 0`

### `reconstructPath(startNode, endNode)` — backtrack chain

```
length = 1
for n = endNode; n.h != null; n = n.h:
    length++

result = new mo[length]
n = endNode
for i = length-1 downto 0:
    result[i] = n
    n = n.h

return new dw(result)
```

### `getOrCreate(x, y, z)` — node deduplication

```
hash = mo.a(x, y, z)
existing = nodeCache.get(hash)
if existing == null:
    existing = new mo(x, y, z)
    nodeCache.put(hash, existing)
return existing
```

---

## 5. PathHeap (`zs`) — Binary Min-Heap

Sorted by `mo.g` (f-cost). Initial capacity 1024, doubles on overflow.

- `add(mo)`: place at end, sift up. Throws if mo.d >= 0 (already in heap).
- `poll()`: swap root with last, shrink, sift down. Sets mo.d = -1 on removal.
- `update(mo, newG)`: update mo.g; sift up if decreased, sift down if increased.
- `isEmpty()`: return b == 0.
- `clear()`: set b = 0 (nodes remain in array but are overwritten on next use).

Parent of index i: `(i - 1) >> 1`.
Left child of index i: `1 + (i << 1)`.
Right child of index i: `2 + (i << 1)`.

---

## 6. ChunkCache (`xk`) — World View for Pathfinding

Constructed in `world.a(entity, target, range)` before calling `rw`. Pre-fetches all chunks
in the search bounding box via `world.c(chunkX, chunkZ)` (get-or-generate chunk).

The chunk array `c[cx - minChunkX][cz - minChunkZ]` is indexed by chunk coordinate offset.

`getBlockId(x, y, z)`:
- Returns 0 if y < 0 or y >= worldHeight.
- Computes chunk offsets. Returns 0 if offset out of bounds or chunk is null.
- Returns `chunk.getBlock(x & 15, y, z & 15)`.

`getMaterial(x, y, z)` → `e(x,y,z)`:
- Reads block ID, returns `Block.registry[id].material` (or `Material.Air` if id=0).

---

## 7. PathEntity (`dw`) — Path Navigation

`dw.a(entity)` — get current waypoint as Vec3:
```
mo node = b[c]
halfWidth = (int)(entity.width + 1.0F) * 0.5
x = node.a + halfWidth
y = node.b
z = node.c + halfWidth
return Vec3.pool(x, y, z)
```

`dw.a()` — advance to next node: `c++`

`dw.b()` — path exhausted: `return c >= b.length`

---

## 8. EntityAI (`ww`) — AI Tick `n()`

Called every tick from `nq.c()` when `!isClient && !isPassive`.

```
// Step 1: Decrement panic timer
if by > 0:
    by--

// Step 2: Update isAngry flag
i = az()    // az() returns false in ww base; subclasses may override

// Step 3: Target management
searchRange = 16.0F

if h == null:
    h = o()             // virtual: get target (zo or fx override)
    if h != null:
        a = world.a(this, h, searchRange)    // request path to new target
else if h.K():          // target is dead (isDead)
    h = null

// Step 4: If target alive, call attack or follow
if h != null:
    dist = h.c(this)    // distance from h to this
    if i(h):            // check attack range (virtual)
        a(h, dist)      // attack behavior
    else:
        b(h, dist)      // approach behavior

// Step 5: Path following (followpath)
if a != null AND random.nextInt(100) != 0:
    currentY = floor(entity.aabb.minY + 0.5)
    inWater = D()       // isInWater
    inLava = F()        // isInLava

    waypoint = a.a(this)    // current waypoint Vec3

    // Skip close waypoints (within 2*width radius)
    while waypoint != null AND waypoint.distSq2D(s, waypoint.y, u) < (width*2)^2:
        a.a()              // advance
        if a.b():          // path exhausted
            waypoint = null
            a = null
        else:
            waypoint = a.a(this)

    bu = false    // reset jump flag

    if waypoint != null:
        dx = waypoint.x - s
        dz = waypoint.z - u
        dy = waypoint.y - currentY

        yawToWaypoint = atan2(dz, dx) * 180/PI - 90
        yawDelta = yawToWaypoint - y   // target yaw - current yaw
        clamp yawDelta to [-30, +30]
        y += yawDelta                  // rotate toward waypoint

        // If isAngry (i): face directly toward target entity instead of waypoint
        if i AND h != null:
            targetDx = h.s - s
            targetDz = h.u - u
            savedYaw = y
            y = atan2(targetDz, targetDx) * 180/PI - 90
            angleDiff = (savedYaw - y + 90) * PI / 180
            br = -sin(angleDiff) * bw    // strafe X
            bs =  cos(angleDiff) * bw    // strafe Z

        // Jump if waypoint is above current position
        if dy > 0:
            bu = true

    // Look at target entity within 30 degrees
    if h != null:
        a(h, 30.0F, 30.0F)    // look-at helper

    // Jump if in water or lava
    if E (isCollidedHorizontally) AND NOT aB():
        bu = true
    if random.nextFloat() < 0.8 AND (inWater OR inLava):
        bu = true

else:   // no path
    super.n()    // nq base: random look-around
    a = null     // clear stale path

// Step 6: Wander (stroll) — called when no target or path refresh
if i OR (h == null AND (a==null AND random.nextInt(180)==0 OR random.nextInt(120)==0 OR by>0))
   AND bq < 100:
    if NOT i:
        aA()    // stroll
```

**Note:** `bq` is the walk-stun timer from `nq` (non-zero while stunned/hurt); strolling is
suppressed while stunned.

### `aA()` — stroll (random wander)

Tries 10 random positions within ±6 XZ, ±3 Y of current position:

```
bestScore = -99999
bestX = bestY = bestZ = -1
found = false

for attempt in 0..9:
    rx = floor(s + nextInt(13) - 6)
    ry = floor(t + nextInt(7) - 3)
    rz = floor(u + nextInt(13) - 6)
    score = a(rx, ry, rz)     // virtual: position desirability score
    if score > bestScore:
        bestScore = score
        bestX = rx; bestY = ry; bestZ = rz
        found = true

if found:
    a = world.a(this, bestX, bestY, bestZ, 10.0F)    // request path to wander target
```

### `ww.i()` — canSpawnHere override

```
return super.i() AND a(floor(s), floor(aabb.minY), floor(u)) >= 0.0F
// i.e., must pass normal spawn check AND position score ≥ 0
```

### `ww.aw()` — panic speed doubling

```
baseSpeed = super.aw()    // nq.aw() base speed
if by > 0:
    baseSpeed *= 2.0F     // double speed while panicking
return baseSpeed
```

---

## 9. EntityMonster (`zo`) — Hostile AI

### `o()` — target acquisition

```
player = world.b(this, 16.0)    // get nearest player within 16 blocks
if player != null AND i(player):
    return player
return null
```

`world.b(entity, range)` = `getClosestVulnerablePlayerToEntity`. Returns nearest `vi`
(EntityPlayer) that is not in creative mode and within `range` blocks.

### `a(target, dist)` — attack behavior

```
if aT <= 0 AND dist < 2.0F AND target.aabb.maxY > aabb.minY AND target.aabb.minY < aabb.maxY:
    aT = 20           // attack cooldown: 20 ticks (1 second)
    b(target)         // deal damage
```

`b(target)` = melee attack method:
```
attackStrength = this.a    // base = 2 (overridden per mob)
if has(Strength potion effect abg.g):
    attackStrength += 3 << strengthLevel    // +3 per level
if has(Weakness potion effect abg.t):
    attackStrength -= 2 << weaknessLevel    // -2 per level
target.a(DamageSource.MobAttack(this), attackStrength)
```

### `a(x, y, z)` — position desirability score

```
return 0.5F - world.getBrightness(x, y, z)    // prefer dark locations
```

A position scores 0.5 when brightness = 0 (completely dark). A fully lit position scores −0.5.
Negative scores are rejected by `ww.i()` (canSpawnHere).

### `u_()` — light-level spawn check

```
skyLight = world.getSkyLight(floor(s), floor(aabb.minY), floor(u))    // world.b(bn.a, x,y,z)
if skyLight > random.nextInt(32):
    return false    // too much sky light

combinedBrightness = world.getCombinedLight(x, y, z)    // world.n()
if world.isThundering():
    // temporarily set sky darkening to 10 for the check
    savedDarkening = world.k
    world.k = 10
    combinedBrightness = world.n(x, y, z)
    world.k = savedDarkening

return combinedBrightness <= random.nextInt(8)
```

`zo.i()` = `u_() AND super.i()`.

### `zo.c()` — entity tick override

```
lightDamage = b(1.0F)    // current fire damage? likely daylight burn check
if lightDamage > 0.5F:
    bq += 2    // stun timer: add 2 per tick in sunlight

super.c()    // nq.c() full tick
```

### `zo.a(DamageSource, int)` — on-damage: set target to attacker

```
if super.a(dm, amount):
    attacker = dm.a()    // get entity attacker
    if attacker != this.m AND attacker != this.n:    // not self, not mount
        if attacker != this:
            this.h = attacker    // retarget to attacker
    return true
return false
```

(`this.m` = riding entity, `this.n` = ridden entity)

---

## 10. EntityAnimal (`fx`) — Passive AI

### `o()` — target acquisition (three modes based on state)

```
if by > 0:
    return null    // panicking: no target

searchRange = 8.0F

if inLove (a > 0):
    // Mode 1: find same-species with inLove > 0 (breed partner)
    candidates = world.getEntitiesWithinAABB(this.class, aabb.expand(8,8,8))
    for each fx candidate:
        if candidate != this AND candidate.a > 0:
            return candidate

else if age == 0 (m() == 0):
    // Mode 2: adult, not cooling down — find player holding food
    players = world.getEntitiesWithinAABB(EntityPlayer.class, aabb.expand(8,8,8))
    for each player:
        if player.getHeldItem() != null AND a(player.getHeldItem()):
            return player

else if age > 0 (m() > 0):
    // Mode 3: breeding cooldown — find same-species baby
    candidates = world.getEntitiesWithinAABB(this.class, aabb.expand(8,8,8))
    for each fx candidate:
        if candidate != this AND candidate.m() < 0:    // baby
            return candidate

return null
```

`fx.a(dk item)` (can be overridden per species): default checks `item.c == acy.S.bM` (wheat ID).

### `fx.c(vi player)` — player right-click interaction (feeding)

```
heldItem = player.getHeldItem()
if heldItem != null AND a(heldItem) AND m() == 0:    // adult, holding food
    heldItem.stackSize--
    if heldItem.stackSize <= 0:
        player.inventory.setInventorySlotContents(currentSlot, null)
    a = 600                   // enter love mode for 30 seconds
    h = null                  // clear target
    spawn 7 heart particles
    return true
return super.c(player)
```

### `a(target, dist)` — approach behavior (meeting target)

#### Case 1: Target is player (vi)

```
if dist < 3.0F:
    face toward player (compute yaw from dx/dz)
    i = true    // isAngry flag: forces facing player in followpath
if player.getHeldItem() == null OR NOT a(player.getHeldItem()):
    h = null    // player dropped food: abandon target
```

#### Case 2: Target is animal (fx) — breed partner approach and breeding

```
if this.a > 0 AND target.m() < 0:
    // Approaching a baby (mode 3 target)
    if dist < 2.5:
        i = true    // face toward target

else if this.a > 0 AND target.a > 0:
    // Both in love: breed sequence
    if target.h == null:
        target.h = this    // make target aware of us

    if target.h == this:
        // We are locked-on as each other's partners
        if dist < 3.5:
            breedingCounter (b) ++
            target.b++
            if b % 4 == 0:
                spawn heart particle (random gaussian offset)
            if b == 60:
                breedWith(target)    // call b(fx)
        else:
            b = 0    // too far: reset counter
    else:
        b = 0    // target has different partner
else:
    b = 0
```

### `b(fx partner)` — breed and spawn offspring

```
offspring = a(partner)    // abstract: create species-specific offspring
if offspring == null: return

// Reset both parents
this.a = 0; this.b = 0; this.h = null
partner.h = null; partner.b = 0; partner.a = 0

// Apply breeding cooldown
this.b(6000)      // set age = 6000 (30-second cooldown, counts down)
partner.b(6000)

// Set offspring as baby
offspring.b(-24000)    // age = -24000 (counts up to 0 over 20 minutes)
offspring.setPosition(this.s, this.t, this.u, this.y, this.z)

// Spawn 7 heart particles
for i in 0..6:
    spawn "heart" particle at random position within entity bounds

world.spawnEntity(offspring)
```

### `fx.b(ia target, float dist)` — flee behavior

Empty in base `fx`. Animals do not flee in vanilla 1.0 (panic timer from damage causes
speed doubling but no directed flee movement — the mob just runs toward its last stroll target).

### `a(x, y, z)` — position desirability score

```
if world.getBlockId(x, y-1, z) == yy.u.bM:    // yy.u = grass block (ID 2)
    return 10.0F    // strongly prefer grass
return world.getBrightness(x, y, z) - 0.5F     // otherwise prefer light
```

### `fx.i()` — canSpawnHere

```
return world.getBlockId(x, y-1, z) == grassId    // grass block directly below
    AND world.getLightValue(x, y, z) > 8
    AND super.i()
```

### `fx.c()` — entity tick (age and love timer)

```
super.c()    // nq.c()

age = m()    // DataWatcher 12
if age < 0: b(age + 1)    // baby: increment age toward 0
if age > 0: b(age - 1)    // cooling down: decrement toward 0

if inLove (a > 0):
    a--    // decrement love timer
    if a % 10 == 0:
        spawn "heart" particle with random Gaussian velocity (×0.02)
else:
    b = 0    // reset breeding counter when not in love
```

### `fx.a(DamageSource, int)` — on-damage: panic

```
by = 60       // panic for 3 seconds (60 ticks)
h = null      // abandon current target
a = 0         // cancel love mode
return super.a(dm, amount)
```

### `fx.p_()` — maxSpawnedInChunk

Returns 120.

### `fx.q_()` — isBaby (for rendering scale)

Returns `m() < 0`.

### `b(vi player)` — XP dropped on death

```
return 1 + world.random.nextInt(3)
```

---

## 11. DataWatcher Assignments

| Slot | Type | Class | Semantics |
|---|---|---|---|
| 12 | `Integer` | `fx` | Age: negative=baby (counts toward 0); 0=adult; positive=cooldown (counts toward 0) |

---

## 12. Constants & Magic Numbers

| Value | Source | Meaning |
|---|---|---|
| `16.0F` | `ww.n()` | Mob target search range (blocks) |
| `2.0F` | `zo.a()` | Melee attack range (blocks) |
| `20` | `zo.a()` | Attack cooldown ticks (1 second) |
| `8.0F` | `fx.o()` | Animal target search range (blocks) |
| `600` | `fx.c(vi)` | Love-mode duration after feeding (ticks = 30 seconds) |
| `6000` | `fx.b()` | Post-breeding cooldown age (ticks = 5 minutes) |
| `-24000` | `fx.b()` | Baby age at birth (ticks = 20 minutes to grow up) |
| `60` | `fx.b` | Breeding counter threshold — triggers offspring spawn |
| `3.5F` | `fx.a()` | Max distance for breeding counter to increment |
| `2.5F` | `fx.a()` | Distance at which animal faces toward baby target |
| `3.0F` | `fx.a()` | Distance at which animal faces toward player |
| `10` | `ww.aA()` | Number of stroll candidate positions tried |
| `180` | `ww.n()` | Reciprocal stroll probability (1/180 per tick ≈ every 9 seconds) |
| `120` | `ww.n()` | Secondary stroll probability (1/120 per tick ≈ every 6 seconds) |
| `30.0F` | `ww.n()` | Maximum yaw rotation per tick toward waypoint (degrees) |
| `(int)(range + 16)` | `world.a()` | ChunkCache bbox margin for entity-target pathfinding |
| `(int)(range + 8)` | `world.a()` | ChunkCache bbox margin for coordinate-target pathfinding |
| `1024` | `zs` | PathHeap initial capacity (doubles on overflow) |
| `16` | `ob` | PathNodeCache initial bucket count |
| `0.75F` | `ob` | PathNodeCache load factor |
| `32` | `rw.d` | Scratch neighbor array capacity (max 4 used per expansion) |
| `4` | `rw` | Maximum step-down distance before rejecting path node |

---

## 13. Tick Behaviour

- **`ww.n()`** is called from `nq.c()` (LivingEntity tick) when `!isClient && !isPassive (aH)`.
- **`zs`/`ob`/`rw`** run synchronously in the world tick when a path is requested.
- **`mo`/`dw`** are allocated per-path; paths persist until exhausted or cleared.
- **Path refresh**: when `h != null && random.nextInt(20) == 0 && a == null` → re-request path.
  Reading `ww.n()` more carefully: the path refresh fires in the outer block:
  `if NOT (i OR h == null OR a != null && random.nextInt(20) != 0)`: re-request.
  So path is refreshed with probability 1/20 per tick when: not angry, has target, has a path.

---

## 14. Known Quirks / Bugs to Preserve

1. **No diagonal pathfinding:** `expandNeighbors` tries exactly 4 horizontal directions.
   Mobs cannot take diagonal steps — they always move axis-aligned.

2. **Stroll range asymmetry:** Entity-target path uses margin `range+16`; coordinate-target
   uses `range+8`. The 16 vs 8 difference is hardcoded, not derived from entity size.

3. **Partial path return:** If A* exhausts the open set without reaching the target, the path
   to the closest-reached node is returned (not null). Only when no node was reachable at all
   (closest == start) does the method return null.

4. **`random.nextInt(100) != 0` path-follow skip:** Path following is skipped 1% of ticks.
   During those ticks the mob runs `super.n()` (nq base: random look, no movement).

5. **Breeding partner lock-on:** `target.h = this` sets the target's own `h` field (its
   AI target) to point back at the initiating animal. This means both animals target each
   other mutually. If the target acquires a different partner first (`target.h != this`),
   the breeding counter resets to 0.

6. **Love-mode cancels on hit:** `fx.a(DamageSource, int)` sets `a = 0` (clears love mode)
   and `by = 60` (panic). An animal that is hit while in love mode immediately stops seeking
   a partner.

7. **Baby age -24000:** Baby mobs grow up after 20 minutes (24000 ticks). Age is stored in
   DataWatcher slot 12 and persisted as NBT "Age". Negative = baby; positive = breeding
   cooldown; zero = normal adult.

---

## 15. Open Questions

1. **`world.b(entity, range)` implementation:** `zo.o()` calls `this.o.b(this, 16.0)` to
   find the nearest player. The World method `b(ia, double)` has not been separately specced —
   it likely calls `world.a(EntityPlayer.class, entity.aabb.expand(r,r,r))` and then finds the
   nearest among results.

2. **`az()` override in subclasses:** Base `ww.az()` returns false (never angry). No concrete
   mob checked appears to override it. Unknown whether Wolf or Enderman overrides this.

3. **`zo.c()` sunlight burn:** The call `this.b(1.0F)` in `zo.c()` may be the `getBrightness`
   method or a separate fire/burn check. If it returns > 0.5F, `bq` (stun timer) is incremented
   by 2. This may be the zombie/skeleton daylight burn mechanism. Needs cross-reference with
   the concrete mob tick for EntityZombie/EntitySkeleton.

4. **`this.o.v`** in `zo.a()` (entity tick): `if (!this.o.I && this.o.v == 0)` — `I` is
   likely `isRemote` (client flag); `v` is unknown (possibly game difficulty, world time mod
   counter, or a flag that gates the per-entity `v()` call). The `v()` call itself is unresolved.

5. **`ww.i(ia)` check:** The method `i(ia target)` is used in `ww.n()` to decide attack vs
   approach. This may be a line-of-sight check or a simple distance check. It is not defined
   in `ww.java` — it may be inherited from `nq` or defined in a subclass.
