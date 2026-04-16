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

# BlockPortal / PortalTravelAgent / ItemFlintAndSteel Spec

Source classes:
- `sc.java` — BlockPortal (extends `aaf`, ID 90)
- `aim.java` — PortalTravelAgent (standalone logic class)
- `ou.java` — ItemFlintAndSteel (extends `acy`)
- `vi.java` (partial) — EntityPlayer portal fields (`bY`, `bZ`) and `S()` method

---

## 1. Purpose

- **`sc`** (BlockPortal): the animated purple portal block (ID 90). Validates its obsidian frame on each neighbor change; handles entity teleportation trigger; renders portal particles.
- **`aim`** (PortalTravelAgent): finds or creates a portal on the destination side of a dimension crossing. Handles both Nether (find existing / create new frame) and End (obsidian platform) arrival.
- **`ou`** (ItemFlintAndSteel): places fire on adjacent air block; used to ignite Nether portal frames.
- **`vi.bY`/`bZ`/`S()`** — player portal state: cooldown counter and teleport flag.

---

## 2. Constants

| Constant | Value | Source |
|---|---|---|
| Portal block ID | 90 | `yy.be.bM` |
| Obsidian block ID | 49 | `yy.ap.bM` |
| Fire block ID | 51 | `yy.ar.bM` |
| Portal light level | 1 | `sc.h()` |
| Portal frame width | 2 inner + 2 wall = 4 | `sc.g()` |
| Portal frame height | 3 inner + 2 wall = 5 | `sc.g()` |
| Minimum obsidian required | 10 (corners optional) | `sc.g()` |
| Portal interior | 2×3 = 6 blocks | `sc.g()` |
| Nether portal search radius | 128 blocks | `aim.b()` — `short var3 = 128` |
| New portal search radius | 16 blocks | `aim.c()` |
| New portal floor Y min | 70 | `aim.c()` — fallback |
| Player portal cooldown initial | 20 ticks | `vi.bY = 20` |
| Player portal cooldown after travel | 10 (floor during re-entry) | `vi.S()` |
| FlintAndSteel max durability | 64 | `ou.i(64)` |
| FlintAndSteel max stack | 1 | `ou.bN = 1` |

---

## 3. BlockPortal (`sc`, ID 90)

Extends `aaf` (some base block class). Material `p.A`.

### 3.1 Constructor

```
super(id, texture, p.A, false)
```

`p.A` is an unresolved material constant (likely a special portal material). The `false` flag's semantics in `aaf` are not resolved.

### 3.2 `b(world, x, y, z)` — getCollisionBoundingBox

Returns `null` — the portal has no physical collision. Entities and projectiles pass through freely.

### 3.3 `b(kq, x, y, z)` — setBlockBoundsBasedOnState (visual bounds)

Determines orientation from neighbors and sets visual AABB:

```
if (neighbor at x-1 != portal AND neighbor at x+1 != portal):
    // Z-facing portal (frame runs along X axis, portal face on Z)
    AABB = (0.5-0.125, 0, 0) → (0.5+0.125, 1, 1)   = 0.25 wide in X, full in Z
else:
    // X-facing portal (frame runs along Z axis, portal face on X)
    AABB = (0, 0, 0.5-0.125) → (1, 1, 0.5+0.125)   = full in X, 0.25 wide in Z
```

Thickness = 0.125 blocks each side = 0.25 total.

### 3.4 `a()` / `b()` — isOpaqueCube / renderAsNormal

Both return `false`.

### 3.5 `g(world, x, y, z)` — tryToCreatePortal

Called by `ItemFlintAndSteel.onItemUse()` indirectly (via the world event that ignites fire inside a valid frame). Actually this method is called when fire is placed inside an obsidian frame — it checks validity and places portal blocks.

**Step 1 — Determine orientation:**
```
obsidianOnX = (world.getBlockId(x-1, y, z) == obsidian OR world.getBlockId(x+1, y, z) == obsidian)
obsidianOnZ = (world.getBlockId(x, y, z-1) == obsidian OR world.getBlockId(x, y, z+1) == obsidian)

if (obsidianOnX == obsidianOnZ): return false   // both or neither → not a valid 1D frame
```

Variables:
- `var5` = 1, `var6` = 0 → Z-axis frame (obsidian on X sides → portal faces along Z)
- `var5` = 0, `var6` = 1 → X-axis frame (obsidian on Z sides → portal faces along X)

**Step 2 — Find base corner:**
If the block one step away along the portal axis is air, shift the origin one step in that direction (centers the scan on the frame's left wall).

**Step 3 — Validate frame (4 wide × 5 tall scan):**
```
for var7 = -1..2:           // along portal width axis
    for var8 = -1..3:       // Y: -1=below floor, 0-2=interior, 3=above ceiling
        isEdge = (var7 == -1 OR var7 == 2 OR var8 == -1 OR var8 == 3)
        isStrictCorner = (var7 == -1 OR var7 == 2) AND (var8 == -1 OR var8 == 3)
        
        if NOT isStrictCorner:  // corners are not checked
            blockId = world.getBlockId(x + var5*var7, y + var8, z + var6*var7)
            if isEdge:
                if blockId != obsidian: return false   // edge must be obsidian
            else:
                if blockId != 0 AND blockId != fire: return false   // interior must be air or fire
```

**Frame geometry** (var7 is X-of-portal axis, var8 is Y):
```
var7:  -1   0   1   2
var8:
  3:  [?] [O] [O] [?]    ? = corner (unchecked), O = must be obsidian
  2:  [O] [ ] [ ] [O]    = interior (must be air/fire)
  1:  [O] [ ] [ ] [O]    = interior
  0:  [O] [ ] [ ] [O]    = interior
 -1:  [?] [O] [O] [?]    = floor edges
```

Required obsidian: 3 (left column) + 3 (right column) + 2 (bottom middle) + 2 (top middle) = **10 blocks minimum**. The 4 corners are optional (can be any block or missing).

**Step 4 — Place portal blocks:**
If validation passes:
```
world.t = true   // suppress update notifications during placement
for var11 = 0..1:      // 2 blocks wide (inner width)
    for var12 = 0..2:  // 3 blocks tall (inner height)
        world.setBlockWithNotify(x + var5*var11, y + var12, z + var6*var11, portalId)
world.t = false
```

Returns `true` on success, `false` if frame is invalid.

### 3.6 `a(world, x, y, z, neighborId)` — onNeighborBlockChange

Called when an adjacent block changes. Validates whether the portal block should still exist.

**Orientation detection:**
```
var6 = 0, var7 = 1   // default: portal runs along Z
if (portal at x-1 OR portal at x+1):
    var6 = 1, var7 = 0   // portal runs along X
```

**Walk to bottom of portal column:**
```
var8 = y
while (world.getBlockId(x, var8-1, z) == portalId): var8--
```

**Validation chain:**
1. If block at (x, var8-1, z) is not obsidian → destroy self (portal has no floor)
2. Count portal column height from `var8`: count upward while portal ID; must be exactly 3
3. If count == 3 AND block at (x, var8+3, z) == obsidian (valid ceiling):
   - Check if portal blocks exist in BOTH X and Z directions simultaneously → destroy (invalid crossing)
   - Check if portal block's lateral sides lack obsidian → destroy
4. Any other case → destroy

If validation fails: `world.setBlockWithNotify(x, y, z, 0)` — removes itself.

### 3.7 `a_(kq, x, y, z, face)` — canBlockStay

Checks that the portal has valid obsidian framing in at least one axis and the face is correct:
```
hasObsidianOnX = (portal at x-1 with no portal at x-2) OR (portal at x+1 with no portal at x+2)
hasObsidianOnZ = (portal at z-1 with no portal at z-2) OR (portal at z+1 with no portal at z+2)

if (hasObsidianOnX): allow faces 4 and 5 (east/west)
if (hasObsidianOnZ): allow faces 2 and 3 (north/south)
```

### 3.8 `a(rng)` — quantityDropped

Returns `0` — portal drops nothing.

### 3.9 `h()` — getLightValue (internal index)

Returns `1` — portal emits a small amount of light.

### 3.10 `a(world, x, y, z, entity)` — onEntityCollidedWithBlock

```
if entity.vehicle == null AND entity.rider == null:
    entity.S()   // call EntityPlayer.inPortal() / Entity.S()
```

Entities without riders or vehicles trigger the portal. The `S()` method sets up the teleportation state.

### 3.11 `b(world, x, y, z, rng)` — randomDisplayTick (particles and sound)

```
// Sound (1% chance):
if (rng.nextInt(100) == 0):
    world.playSound(x+0.5, y+0.5, z+0.5, "portal.portal", 0.5F, rng.nextFloat()*0.4 + 0.8F)

// 4 portal particles:
for i = 0..3:
    x = blockX + rng.nextFloat()
    y = blockY + rng.nextFloat()
    z = blockZ + rng.nextFloat()
    dx = dy = dz = 0
    
    if (no portal neighbors in X axis):
        // Z-facing portal: particles drift along X
        x = blockX + 0.5 + 0.25 * sign
        dx = rng.nextFloat() * 2.0 * sign
    else:
        // X-facing portal: particles drift along Z
        z = blockZ + 0.5 + 0.25 * sign
        dz = rng.nextFloat() * 2.0 * sign
    
    world.spawnParticle("portal", x, y, z, dx, dy, dz)
```

---

## 4. EntityPlayer portal state (`vi.bY`, `vi.bZ`, `vi.S()`)

### 4.1 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `bY` | `int` | 20 | Portal cooldown counter; prevents immediate re-teleportation |
| `bZ` | `boolean` | `false` | Teleportation trigger flag; set when cooldown expires during portal contact |

### 4.2 `S()` — inPortal

Called each tick by the portal block while the player is inside it.

```java
public void S() {
    if (this.bY > 0) {
        this.bY = 10;    // hold cooldown at 10 while still inside portal
    } else {
        this.bZ = true;  // trigger: initiate dimension transfer
    }
}
```

**Mechanics:**
- If `bY > 0` (cooldown active): the counter is FORCED to 10 each tick, not decremented. The actual decrement happens in the server tick manager (outside this class). Setting `bY = 10` prevents the cooldown from going below 10 ticks while the player remains in the portal.
- If `bY == 0` (no cooldown): `bZ = true` — the server tick reads this flag and initiates `travelToDimension`.

**Cooldown lifecycle:**
1. Player first enters portal: `bY = 20` (initial default, set at player spawn)
2. Server tick decrements `bY` by 1 each tick while `bZ == false`
3. After 20 ticks in portal with `bY` reaching 0: `S()` sets `bZ = true`
4. Server processes `bZ = true`: calls `aim.a(world, player)` → teleports player → sets `bY = 200` (post-teleport cooldown) and `bZ = false`
5. If player re-enters portal immediately: `bY > 0` → `bY = 10` (resists dropping below 10 during re-entry)

**Note:** The exact value set post-teleportation (step 4) is not confirmed from `vi.java` alone. The 200-tick (10 second) cooldown is the documented vanilla behaviour.

---

## 5. PortalTravelAgent (`aim`)

A non-block helper class managing dimension travel. Has one instance-level `Random a` field.

### 5.1 `a(world, entity)` — main entry point

Called when `vi.bZ == true` (player triggered dimension travel).

```
if (world.dimension != 1):
    // Overworld ↔ Nether travel
    if NOT b(world, entity):       // try finding existing portal
        c(world, entity)            // create new portal
        b(world, entity)            // then use the new portal
else:
    // Arriving in The End
    [place 5×5 obsidian platform — see §5.4]
```

### 5.2 `b(world, entity)` — findNearestPortal

Searches for an existing Nether portal (portal block = `yy.be.bM`) within 128 blocks.

```
radius = 128
for searchX = (entity.x - 128)..(entity.x + 128):
    for searchZ = (entity.z - 128)..(entity.z + 128):
        for searchY = (world.height - 1)..0 (descending):
            if world.getBlockId(searchX, searchY, searchZ) == portalId:
                // walk down to find the bottom of the portal column
                while getBlockId(searchX, searchY-1, searchZ) == portalId: searchY--
                
                // compute squared distance from entity
                dist² = Δx² + Δy² + Δz²
                if dist² < bestDist²: record (searchX, searchY, searchZ) as best
```

After finding the nearest portal column bottom:
- Snap entity to portal center: adjust X or Z position toward the portal's solid side by ±0.5 if obsidian is adjacent
- Set entity position + reset velocity: `entity.setPositionAndRotation(x+0.5, y+0.5, z+0.5, yaw, 0)`

Returns `true` if a portal was found and the entity was placed; `false` otherwise.

**Coordinate scaling:** The caller (server tick manager) handles the 8:1 Overworld→Nether coordinate scale. This method searches near the entity's coordinates in the DESTINATION world, not the source world.

### 5.3 `c(world, entity)` — createNewPortal

Creates a new Nether portal at the closest valid location within 16 blocks.

**Phase 1 — Find full 4×5 portal opening:**
```
radius = 16
// scan 33×33 area at all heights, descending
// for each surface position, try 4 orientations (var21 = var13 to var13+3):
//   check 3×4×5 air column exists (3 deep, 4 wide, 5 tall)
//   the check: for var24=0..2 (depth), var25=0..3 (width), var26=-1..3 (height):
//     var26 < 0: must be solid (floor)
//     var26 >= 0: must be air (not isAirBlock returns false)
// Score best location by squared distance
```

**Phase 2 — Fall back to minimal 1×4 opening (if phase 1 failed):**
```
// Narrower search: depth 0..1, width 0..3, height -1..3
// Same solid floor + air column check
```

**Phase 3 — Emergency fallback (if both phases failed):**
```
Y = clamp(entity.y, 70, world.height - 10)
// Build a 3-wide × 4-tall obsidian foundation:
for dx = -1..1:                       // 3 blocks wide
    for dy = -1..2:                   // from 1 below to 2 above
        for dz = 1..2:               // 2 blocks deep
            if (dy < 0): place obsidian
            else: place air
```

**Building the portal frame (all phases):**
```
// Place the full 4×5 obsidian frame with interior portal blocks
// 4 passes (for var39 = 0..3):
world.t = true   // suppress updates
for var42 = 0..3:                     // 4 positions along width axis
    for var46 = -1..3:               // 5 positions along Y
        isEdge = (var42==0 OR var42==3 OR var46==-1 OR var46==3)
        if isEdge:
            place obsidian at position
        else:
            place portal block (yy.be.bM) at position
world.t = false

// Fire world.j() (block neighbor notify) for each placed block
```

The 4×5 frame:
```
O O O O    (y+3 = ceiling)
O P P O    (y+2)
O P P O    (y+1)
O P P O    (y+0, = floor level)
O O O O    (y-1 = below floor)
```
O = obsidian, P = portal block. Width = 4 (positions 0..3), height = 5 (positions -1..3).

### 5.4 Obsidian Platform Placement (End arrival)

When `world.dimension == 1`:

```
x = floor(entity.x)
y = floor(entity.y) - 1     // one block below spawn point
z = floor(entity.z)

// var6=1, var7=0 (Z-aligned facing variables, always the same for End)
for dx = -2..2:              // 5 positions in X
    for dz = -2..2:          // 5 positions in Z
        for dy = -1..2:      // 4 positions in Y (1 below, 3 above)
            blockX = x + dz*var6 + dx*var7    = x + dz
            blockY = y + dy
            blockZ = z + dz*var7 - dx*var6    = z - dx
            place (dy < 0) ? obsidian : air
```

Result: 5×5 obsidian floor at y-1, 5×5×3 air above it.

Entity position set to: `(x, y-1+1, z)` = standing on the obsidian surface.
Entity velocity reset to zero.

---

## 6. ItemFlintAndSteel (`ou`)

Extends `acy` (Item).

### 6.1 Fields

| Field | Type | Semantics |
|---|---|---|
| `bN` | `int` (set to 1) | Max stack size = 1 |
| `i(64)` | via `acy.i()` | Max damage (durability) = 64 |

### 6.2 `a(stack, player, world, x, y, z, face)` — onItemUse

**Offset target based on face clicked:**
```
face 0 (bottom): y -= 1
face 1 (top):    y += 1
face 2:          z -= 1
face 3:          z += 1
face 4:          x -= 1
face 5:          x += 1
```

Target position (x, y, z) is now adjacent to the clicked block face.

**Guard:** If player cannot reach (x, y, z) → return false.

**Place fire:**
```
blockId = world.getBlockId(x, y, z)
if (blockId == 0):   // only if air
    world.playSound(x+0.5, y+0.5, z+0.5, "fire.ignite", 1.0F, rng*0.4+0.8)
    world.setBlockWithNotify(x, y, z, fireId)   // yy.ar.bM = fire ID 51
```

**Damage item (always, regardless of placement success):**
```
stack.damageItem(1, player)   // stack.a(1, player)
```

Return `true`.

**Quirk:** Durability is consumed even if the adjacent position was not air (fire not placed). Clicking flint and steel on any solid block surface always costs 1 durability.

### 6.3 Portal Ignition

When fire is placed inside an obsidian frame, `BlockFire.onBlockAdded()` is called, which calls `sc.g(world, x, y, z)` (tryToCreatePortal). If the frame is valid, portal blocks replace the fire. This is the indirect path from ItemFlintAndSteel to portal creation.

**Note:** The fire block's `onBlockAdded` calling `sc.g()` is how the portal activation works — `ItemFlintAndSteel.onItemUse` does not call `g()` directly. It places fire, and fire checks if it's inside a valid portal frame.

---

## 7. Bitwise / Data Layouts

### 7.1 Portal block metadata (`sc`)

No metadata is used by the portal block itself. Orientation is determined dynamically from neighbors each time.

### 7.2 Player portal fields

| Field | In NBT? | Notes |
|---|---|---|
| `bY` | **Not confirmed** — PlayerNBT may persist it | Cooldown counter |
| `bZ` | Not persisted | Transient flag, reset on login |

---

## 8. Known Quirks

### 8.1 Corner obsidian is optional

The `g()` validation scan skips strict corners (var7=-1/2 AND var8=-1/3). A portal frame can activate with air or any block at all 4 corners — only the 10 non-corner edge cells must be obsidian.

### 8.2 FlintAndSteel always consumes durability

`stack.damageItem(1, player)` is called outside the `if (blockId == 0)` guard. Clicking on any non-portal surface with flint and steel costs durability regardless of whether fire is placed.

### 8.3 Portal validation is per-block (no global scan)

`onNeighborBlockChange()` in `sc` validates only the immediate column's obsidian frame. There is no stored "this portal is part of frame F" reference. Each portal block independently verifies its surroundings each time a neighbor changes.

### 8.4 `a.nextInt(4)` orientation randomisation in `c()`

The portal creation in `aim.c()` starts orientation search at `var13 = a.nextInt(4)` (random of 4 orientations). This makes the portal placement orientation non-deterministic even for the same position, depending on the `a` Random's current state.

### 8.5 No coordinate scale in `aim` itself

`aim` does not divide/multiply by 8 for Overworld↔Nether coordinate conversion. This conversion must be performed by the caller (the server manager) before invoking `aim.a()`. The entity's coordinates are already scaled when `aim` searches for a portal.

---

## 9. Open Questions

### 9.1 `aaf` base class for BlockPortal

`sc` extends `aaf`. The `aaf` class is not analysed in this spec. It likely provides some common logic for fluid-like passable blocks. Its `false` constructor flag is unknown.

### 9.2 Exact post-teleport bY value

The value set to `vi.bY` after teleportation (step 4 of the portal cooldown lifecycle) is not confirmed from the code analysed. Standard vanilla 1.0 uses 200 ticks (10 seconds). The decrement mechanism is in an unanalysed server tick handler.

### 9.3 `aim.c()` phase 1 search — exact condition

The 3-depth `var24=0..2` check in `aim.c()` phase 1 was not fully traced. The condition at line 137 (`var26 < 0 && !world.e(...).b() || var26 >= 0 && !world.h(...)`) — `e().b()` may be `getMaterial().isSolid()` and `h()` may be `isAirBlock()`. The exact check (must be solid floor + air above) is inferred from context.

### 9.4 BlockFire's `onBlockAdded` calling `sc.g()`

This spec states that fire triggers `sc.g()` on placement. The specific method in `BlockFire` (`wj.java`) that makes this call was not re-read for this spec. See `BlockFire_Spec.md` for confirmation.
