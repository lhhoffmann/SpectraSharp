# BlockRedstone_Spec — Redstone System

**Source classes:** `kw.java` (wire), `ku.java` (torch), `bg.java` (torch base), `mz.java` (repeater),
`aaa.java` (lever), `wx.java` (pressure plate), `ahv.java` (button), `oc.java` (ore),
`ry.java` (world power query), `lf.java` (torch history), `lz.java` (direction arrays), `xb.java` (plate type enum)

**Obfuscated names:**
- `kw` = BlockRedstoneWire (ID 55)
- `ku` = BlockRedstoneTorch (IDs 75=off, 76=on)
- `bg` = BlockTorch (base class for torch and redstone torch)
- `mz` = BlockRedstoneDiode / Repeater (IDs 93=off, 94=on)
- `aaa` = BlockLever (ID 69)
- `wx` = BlockPressurePlate (IDs 70=stone, 72=wood)
- `ahv` = BlockButton (ID 77=stone; wood button added Beta 1.7+, absent in 1.0)
- `oc` = BlockOreRedstone (IDs 73=normal, 74=glowing)
- `lf` = TorchHistory (plain class — timestamp entry for burnout tracking)
- `lz` = Direction (static lookup arrays for direction math)
- `xb` = EnumPressurePlateType (3-value enum: a/b/c)

**Key yy fields:**
- `yy.av` = ID 55 BlockRedstoneWire
- `yy.aJ` = ID 69 BlockLever
- `yy.aK` = ID 70 BlockPressurePlate (stone)
- `yy.aM` = ID 72 BlockPressurePlate (wood)
- `yy.aN` = ID 73 BlockOreRedstone (normal)
- `yy.aO` = ID 74 BlockOreRedstone (glowing)
- `yy.aP` = ID 75 BlockRedstoneTorch (off)
- `yy.aQ` = ID 76 BlockRedstoneTorch (on)
- `yy.aR` = ID 77 BlockButton (stone)
- `yy.bh` = ID 93 BlockRedstoneDiode (off)
- `yy.bi` = ID 94 BlockRedstoneDiode (on)

---

## §1 Face / Direction Encoding (global, used throughout)

Face IDs used by all redstone blocks (0-based):

| Face ID | Axis | Direction |
|---|---|---|
| 0 | -Y | Down |
| 1 | +Y | Up |
| 2 | +Z | South |
| 3 | -Z | North |
| 4 | +X | East |
| 5 | -X | West |

**`lz` static lookup arrays:**
- `lz.a[4]` = `{0,-1,0,1}` — Z-delta per 4-facing direction
- `lz.b[4]` = `{1,0,-1,0}` — X-delta per 4-facing direction
- `lz.e[4]` = `{2,3,0,1}` — opposite facing index (0↔2, 1↔3)

---

## §2 World Power Query API (`ry.java`)

Four methods implement the redstone power query chain:

```
ry.k(x,y,z, face)  → getStrongPower: calls yy.k[id].c(world,x,y,z,face) = isProvidingStrongPower
ry.u(x,y,z)        → isStronglyPowered: calls k() for all 6 faces, OR of results
ry.l(x,y,z, face)  → getPower: opaque cube → u(x,y,z); else yy.k[id].b(world,x,y,z,face) = isProvidingWeakPower
ry.v(x,y,z)        → isBlockReceivingPower: calls l() for all 6 faces, OR of results
```

**Summary of semantics:**
- Strong power (`c()` / `k()` / `u()`): sourced from torches, buttons, levers, pressure plates when active, toward the block they're mounted on.
- Weak power (`b()` / `l()` / `v()`): sourced from all powered blocks to adjacent neighbors. For opaque cubes, this is the strong power (the cube acts as a repeater).
- Wire queries `v(x,y,z)` to check if it is receiving power.
- The repeater/torch query `l(x,y,z,face)` checks if the block at (x,y,z) provides power toward face `face`.

---

## §3 BlockRedstoneWire (`kw`, ID 55)

### §3.1 Fields

| Java | Type | Description |
|---|---|---|
| `a` | `boolean` | `canProvidePower` anti-reentrance flag (default true; set false during propagation) |
| `cb` | `Set<am>` | Dirty-block set — blocks needing neighbor notifications after power change |

### §3.2 Constructor

```java
kw(55, 164)  // blockId=55, textureIndex=164
// material = p.p (passable)
// AABB: (0, 0, 0, 1, 1/16, 1)
// hardness = 0.0
// canProvidePower = false (canSilk/no-silk flag via .r())
```

Block method overrides:
- `a()` isOpaqueCube = false
- `b()` renderAsNormal = false
- `c()` getLightOpacity = 5
- `b(ry,x,y,z)` getCollisionAABB = null (no collision box)

### §3.3 canBlockStay

```java
c(ry world, x, y, z):
    return world.g(x, y-1, z)  // solid block directly below
```

### §3.4 isProvidingWeakPower

```java
b(kq world, x, y, z, int face):
    if (!this.a) return false              // reentrance guard
    if (world.d(x,y,z) == 0) return false // meta = 0 (unpowered)
    if (face == 1) return true             // always powers upward (face 1 = up)
    // For lateral faces:
    boolean west  = connectsTo(x-1,y,z, 1) || (!solid[x-1] && connectsTo(x-1,y-1,z,-1))
    boolean east  = connectsTo(x+1,y,z, 3) || (!solid[x+1] && connectsTo(x+1,y-1,z,-1))
    boolean north = connectsTo(x,y,z-1, 2) || (!solid[z-1]  && connectsTo(x,y-1,z-1,-1))
    boolean south = connectsTo(x,y,z+1, 0) || (!solid[z+1]  && connectsTo(x,y-1,z+1,-1))
    // Also check staircase-up connections
    if (not above-solid) {
        if solid[x-1] && connectsTo(x-1,y+1,z,-1): west=true
        if solid[x+1] && connectsTo(x+1,y+1,z,-1): east=true
        if solid[z-1] && connectsTo(x,y+1,z-1,-1): north=true
        if solid[z+1] && connectsTo(x,y+1,z+1,-1): south=true
    }
    // Return true only if wire connects only toward the queried face:
    if (none connected) return face in [2,3,4,5]  // isolated dot: connects all lateral faces
    if (face==2 && south && !west && !east) return true
    if (face==3 && north && !west && !east) return true  // note: lz.e mapping check
    if (face==4 && west  && !north && !south) return true
    if (face==5 && east  && !north && !south) return true
    return false
```

`connectsTo(x,y,z, fromDir)` helper `c(kq,x,y,z,fromDir)`:
- `x,y,z` is `yy.av` (wire): return true
- block is 0: return false
- block has canProvidePower AND fromDir!=-1: return true (any powered block)
- block is repeater (`yy.bh` or `yy.bi`) AND `fromDir == (meta & 3)` (input facing) OR `fromDir == lz.e[meta & 3]` (output facing): return true
- `d(kq,x,y,z,dir)` variant: also checks repeater output direction via `(meta & 3) == dir`

### §3.5 isProvidingStrongPower

```java
c(ry, x, y, z, face):
    return !this.a ? false : b(world, x, y, z, face)
    // Strong power = same as weak power (guarded by a flag)
```

### §3.6 canProvidePower override

```java
g(): return this.a   // returns the anti-reentrance field
```

### §3.7 Main Propagation Algorithm

Called as `kw.g(ry world, x, y, z)` which:
1. Calls `a(world, x, y, z, x, y, z)` (private, self-origin exclusion)
2. Copies and clears `cb`, then calls `world.j()` on each stored position

Private `a(world, x, y, z, originX, originY, originZ)`:

```
1. readMeta = world.d(x, y, z)        // current power level (0-15)
2. this.a = false
   isPowered = world.v(x, y, z)       // isBlockReceivingPower (any adjacent strongly powers wire)
   this.a = true
3. if isPowered:
       newMeta = 15
   else:
       newMeta = 0
       for each of 4 horizontal neighbors (skip if == origin):
           newMeta = max(newMeta, wireMetaAt(neighbor))
       check staircase-up neighbors (block above is opaque AND neighbor below is wire):
           newMeta = max(newMeta, wireMetaAt(neighbor_below))
       else (block not solid at neighbor):
           newMeta = max(newMeta, wireMetaAt(neighbor_below))
       if newMeta > 0: newMeta--   // attenuation: -1 per block

4. if readMeta != newMeta:
       world.t = true                            // suppress cascades
       world.f(x, y, z, newMeta)               // setMetaWithoutUpdate
       world.c(x, y, z, x, y, z)              // notifyNeighborsForChange
       world.t = false
       // Recurse into changed neighbors:
       for each of 4 horizontal neighbors (and their staircase y±1):
           neigh_meta = wireMetaAt(neighbor)
           cur_minus1 = max(0, newMeta - 1)
           if neigh_meta >= 0 && neigh_meta != cur_minus1:
               recurse a(world, neighbor, x, y, z)
       // If transition crosses 0:
       if (readMeta == 0 || newMeta == 0):
           cb.add(self + 6 neighbors)           // schedule notifyBlocks
```

`wireMetaAt(x,y,z)` helper `f(ry,x,y,z,current)`:
- If block at (x,y,z) is not wire: return `current` unchanged
- Else: return `max(current, world.d(x,y,z))`

### §3.8 onBlockAdded / onNeighborBlockChange

Both call `g(world, x, y, z)` then notify additional staircase blocks:

```java
a(ry, x, y, z):  // onBlockAdded
    super.a(...)
    if (!world.I):   // !isRemote
        g(world, x, y, z)
        world.j(above, bM);  world.j(below, bM)
        h(world, x-1,y,z); h(world, x+1,y,z)
        h(world, x,y,z-1); h(world, x,y,z+1)
        // staircase neighbors:
        if solid[x-1]: h(world, x-1,y+1,z); else: h(world, x-1,y-1,z)
        if solid[x+1]: h(world, x+1,y+1,z); else: h(world, x+1,y-1,z)
        if solid[z-1]: h(world, x,y+1,z-1); else: h(world, x,y-1,z-1)
        if solid[z+1]: h(world, x,y+1,z+1); else: h(world, x,y-1,z+1)

d(ry, x, y, z):  // onNeighborBlockChange — identical pattern
```

`h(world,x,y,z)` helper: if `world.a(x,y,z) == bM` (wire), calls `world.j()` on self + 4 lateral neighbors (recalculate those wires too).

### §3.9 onNeighborBlockChange (with canBlockStay)

```java
a(ry, x, y, z, int changedId):
    if (!world.I):
        meta = world.d(x,y,z)
        if (!canBlockStay(world,x,y,z)):
            b(world, x,y,z, meta, 0)  // drop item
            world.g(x,y,z, 0)          // remove block
        else:
            g(world, x, y, z)           // propagate
        super.a(...)
```

### §3.10 Drops

```java
a(int fortune, Random rand, int meta):
    return acy.aB.bM   // redstone dust item ID (= acy.aB)
```

### §3.11 randomDisplayTick (particles)

```java
b(ry, x,y,z, Random rand):
    int meta = world.d(x,y,z)
    if (meta > 0):
        float t = meta / 15.0F
        R = t*0.6F + 0.4F
        G = max(0, t*t*0.7F - 0.5F)
        B = max(0, t*t*0.6F - 0.7F)
        spawn "reddust" particle at (x+0.5±0.1, y+0.0625, z+0.5±0.1) with color (R,G,B)
```

---

## §4 BlockTorch base (`bg`)

### §4.1 Overview

Base class for both regular torch (ID 50) and redstone torch (IDs 75/76). Handles placement meta, canBlockStay, and AABB.

Material: `p.p` (passable). Tick flag: `b(true)` (schedules ticks). isOpaqueCube=false, renderAsNormal=false, lightOpacity=2.

### §4.2 Meta Encoding (common to all torch types)

| Meta | Attachment | Block attached to |
|---|---|---|
| 1 | West wall | block at x-1 |
| 2 | East wall | block at x+1 |
| 3 | North wall | block at z-1 |
| 4 | South wall | block at z+1 |
| 5 | Floor | block at y-1 |

### §4.3 Placement (`b(ry,x,y,z,face)` / `a(ry,x,y,z)`)

Placement called when block is placed. Sets meta from face:

```
face 1 (clicked top): if floor-or-bed below → meta 5
face 2 (+Z face):     if solid at z+1 → meta 4
face 3 (-Z face):     if solid at z-1 → meta 3
face 4 (+X face):     if solid at x+1 → meta 2
face 5 (-X face):     if solid at x-1 → meta 1
```

`g(ry,x,y,z)` for floor check: `world.b(x,y,z, true)` OR block is `yy.aZ` (bed foot) OR `yy.bB` (bed head).

### §4.4 canBlockStay (`c(ry,x,y,z)`)

```java
world.b(x-1,y,z, true) || world.b(x+1,y,z, true) ||
world.b(x,y,z-1, true) || world.b(x,y,z+1, true) ||
g(world, x, y-1, z)  // floor check
```

### §4.5 onNeighborBlockChange (`a(ry,x,y,z,int)`)

Calls `h()` which:
1. If canBlockStay → return true
2. Else: drop item + remove block, return false

### §4.6 Selection AABB (`a(ry,x,y,z,fb,fb)`)

Width = 0.15F, height = 0.6F:
- meta 1 (west wall): x=[0, 0.3], z=[0.35, 0.65]
- meta 2 (east wall): x=[0.7, 1.0], z=[0.35, 0.65]
- meta 3 (north wall): x=[0.35, 0.65], z=[0, 0.3]
- meta 4 (south wall): x=[0.35, 0.65], z=[0.7, 1.0]
- meta 5 (floor): width=0.1, x=[0.4, 0.6], y=[0, 0.6], z=[0.4, 0.6]

---

## §5 BlockRedstoneTorch (`ku`, IDs 75/76)

### §5.1 Fields

| Java | Type | Description |
|---|---|---|
| `a` | `boolean` | isOn — true=active (ID 76), false=burnt-out (ID 75) |
| `cb` | `static List<lf>` | Shared history list for burnout tracking (class-level, not per-instance) |

`lf` = torch history entry: fields `a=x, b=y, c=z, d=worldTime (long)`.

### §5.2 Constructor

```java
new ku(75, 115, false)   // off torch, texture 115
new ku(76, 99,  true)    // on torch,  texture 99; light emission 0.5 (.a(0.5F))
```

Hardness = 0.0 for both. Inherits bg behavior.

### §5.3 isProvidingWeakPower (`b(kq,x,y,z,face)`)

Powers all faces EXCEPT the face toward the block it is attached to:

```java
if (!this.a) return false   // off torch never provides power

meta = world.d(x,y,z)
if (meta==5 && face==1) return false  // floor torch doesn't power up
if (meta==3 && face==3) return false  // north wall torch doesn't power north
if (meta==4 && face==2) return false  // south wall torch doesn't power south
if (meta==1 && face==5) return false  // west wall torch doesn't power west
if (meta==2 && face==4) return false  // east wall torch doesn't power east
return true  // all other directions are powered
```

### §5.4 isProvidingStrongPower (`c(ry,x,y,z,face)`)

```java
return face == 0 ? b(world, x, y, z, face) : false
// Strong power only downward (face 0 = down)
```

### §5.5 isPowered Check (`g(ry,x,y,z)` private)

Checks if the block the torch is attached to is providing power toward the torch:

```java
meta = world.d(x,y,z)
if (meta==5): return world.l(x, y-1, z, 0)      // block below → powered face 0(down)
if (meta==3): return world.l(x, y, z-1, 2)      // block at z-1 → powered face 2(south)
if (meta==4): return world.l(x, y, z+1, 3)      // block at z+1 → powered face 3(north)
if (meta==1): return world.l(x-1, y, z, 4)      // block at x-1 → powered face 4(east)
if (meta==2): return world.l(x+1, y, z, 5)      // block at x+1 → powered face 5(west)
return false
```

### §5.6 onBlockAdded (`a(ry,x,y,z)`)

```java
if (meta == 0): super.a(...)          // run bg placement logic
if (this.a) isOn:                     // active torch
    notify all 6 neighbors: world.j(±x,±y,±z, bM)
```

### §5.7 onNeighborBlockChange (`a(ry,x,y,z,int)`)

Schedules a tick via `world.a(x,y,z, bM, d())` with delay `d()` = 2 ticks.

### §5.8 Burnout Check (`a(ry,x,y,z,bool addEntry)` private)

```java
if (addEntry): cb.add(new lf(x, y, z, world.u()))   // world.u() = worldTime long

count = occurrences of (x,y,z) in cb
return count >= 8   // burnt out: ≥8 flips in window
```

### §5.9 randomTick (`a(ry,x,y,z,Random)`)

```java
// Trim old entries (older than 100 ticks)
while (cb.size() > 0 && world.u() - cb[0].d > 100):
    cb.remove(0)

if (this.a):  // on torch
    powered = g(world, x, y, z)
    if (powered):
        world.d(x,y,z, yy.aP.bM, meta)    // switch to OFF (ID 75), preserve meta
        if (burnout(world,x,y,z, addEntry=true)):
            play "random.fizz" sound at (x+0.5, y+0.5, z+0.5), vol=0.5, pitch=2.6±0.8
            spawn 5 "smoke" particles at random positions
        // Note: burns out silently without sound if not 8 flips
else:         // off torch
    powered = g(world, x, y, z)
    if (!powered && !burnout(world,x,y,z, addEntry=false)):
        world.d(x,y,z, yy.aQ.bM, meta)    // switch to ON (ID 76), preserve meta
```

**Burnout summary:**
- Torch toggles ≥8 times within 100 ticks → stays off permanently (until world.j forces recalc)
- `cb` is STATIC (shared across all torches) — all torches share one burnout history list

### §5.10 Drops

```java
a(int fortune, Random rand, int meta):
    return yy.aQ.bM   // always drops the ON torch item (ID 76)
```

### §5.11 canProvidePower

```java
g(): return true   // always (unlike wire which uses flag)
```

---

## §6 BlockRedstoneDiode / Repeater (`mz`, IDs 93/94)

### §6.1 Fields

| Java | Type | Description |
|---|---|---|
| `a[]` | `static double[4]` | `{-0.0625, 0.0625, 0.1875, 0.3125}` — particle offset per delay step |
| `cb[]` | `static int[4]` | `{1, 2, 3, 4}` — tick delay multiplier per delay step |
| `cc` | `boolean` | isOn — true=powered (ID 94), false=unpowered (ID 93) |

### §6.2 Constructor

```java
new mz(93, false)  // off repeater; hardness 0, light 0
new mz(94, true)   // on  repeater; hardness 0, light 0.625
// texture index 6 passed to super (stone side)
// AABB: (0, 0, 0, 1, 2/16, 1)  (2 pixels high)
// material = p.p (passable), renderAsNormal=false
```

### §6.3 Meta Bit Layout

```
bits 1-0 (meta & 3):  facing direction (output direction)
bits 3-2 (meta >> 2 & 3): delay index (0-3)
```

**Facing / Output direction:**

| meta & 3 | Output direction | Reads from (input) |
|---|---|---|
| 0 | North (-Z) | z+1 (south) |
| 1 | East (+X) | x-1 (west) |
| 2 | South (+Z) | z-1 (north) |
| 3 | West (-X) | x+1 (east) |

**Delay (ticks):** `cb[(meta >> 2) & 3] * 2` = {2, 4, 6, 8}

### §6.4 Placement (`a(ry,x,y,z,nq entity)`)

```java
facing = ((MathHelper.floor(entity.yaw * 4.0F / 360.0F + 0.5) & 3) + 2) % 4
world.setMetadataWithoutUpdate(x,y,z, facing)
// If already has input: schedule tick with delay 1
```

Facing = direction the output faces = direction the player is looking when placing.

### §6.5 canBlockStay

Requires solid block directly below: `world.g(x, y-1, z)`.

### §6.6 Input Check (`f(ry,x,y,z,meta)` private)

```java
facing = meta & 3
case 0: return world.l(x, y, z+1, 3) || (wire at z+1 with meta>0)
case 1: return world.l(x-1, y, z, 4) || (wire at x-1 with meta>0)
case 2: return world.l(x, y, z-1, 2) || (wire at z-1 with meta>0)
case 3: return world.l(x+1, y, z, 5) || (wire at x+1 with meta>0)
// world.l(bx,by,bz, face) = isPoweringFace; "face" is toward the repeater
```

### §6.7 isProvidingWeakPower / StrongPower

```java
b(kq, x,y,z, face):  // weak power
    if (!this.cc) return false   // off repeater never provides power
    facing = meta & 3
    if (facing==0 && face==3) return true   // output north → face 3(north)
    if (facing==1 && face==4) return true   // output east  → face 4(east)... wait, face 4=east(+X); output east means face toward east neighbor
```

Wait — face 4 = East (+X direction). If facing=1 output=East, then the output block is at x+1, and the repeater provides power to face 4 (east direction). Actually face 4 means "the east face of this block provides power" which means power flows east. Let me re-document:

```java
facing==0 → output north(-Z): face 3 (north face)
facing==1 → output east(+X):  face 4 (east face) — QUIRK: see §6.10
facing==2 → output south(+Z): face 2 (south face)
facing==3 → output west(-X):  face 5 (west face)

c(ry,x,y,z,face): delegates to b()   // strong power same as weak for repeater
```

### §6.8 onBlockActivated (right-click to change delay)

```java
delayBits = ((meta >> 2) + 1) * 4 & 12   // increment delay, wrap 3→0
world.setMetadataWithoutUpdate(x,y,z, delayBits | facing)
return true
```

Cycles delay: 1-tick (0) → 2-tick (1) → 3-tick (2) → 4-tick (3) → back to 1-tick.

### §6.9 randomTick (`a(ry,x,y,z,Random)`)

```java
meta = world.d(x,y,z)
hasInput = f(world, x, y, z, meta)
delayBits = (meta >> 2) & 3
delay = cb[delayBits] * 2   // {2,4,6,8} ticks

if (this.cc):    // currently ON
    if (!hasInput):
        world.d(x,y,z, yy.bh.bM, meta)    // switch to OFF (ID 93)
    else: // still has input, reschedule? No — see below
        // ON→ON schedule: world.a(x,y,z, yy.bi.bM, delay) called by neighbor-change path

else:            // currently OFF
    world.d(x,y,z, yy.bi.bM, meta)         // switch to ON (ID 94), regardless of input
    if (!hasInput):
        delay = cb[delayBits] * 2
        world.a(x,y,z, yy.bi.bM, delay)    // schedule turn-off
```

### §6.10 onNeighborBlockChange (`a(ry,x,y,z,int)`)

```java
if (!canBlockStay): drop + remove
else:
    meta = world.d(x,y,z)
    hasInput = f(world,x,y,z,meta)
    delayBits = (meta >> 2) & 3
    if (this.cc && !hasInput):
        world.a(x,y,z, bM, cb[delayBits]*2)   // schedule turn-off
    elif (!this.cc && hasInput):
        world.a(x,y,z, bM, cb[delayBits]*2)   // schedule turn-on
    // Also: if cc=true and isOn, call world.j(y+1,bM) to notify above
```

### §6.11 Texture

```java
a(face, meta):
    face == 0: isOn ? 99  : 115   // bottom texture
    face == 1: isOn ? 147 : 131   // top texture
    else:      5                   // stone side (atlas index 5)
```

### §6.12 Drops

```java
a(fortune, Random, meta): return acy.ba.bM   // repeater item
```

---

## §7 BlockLever (`aaa`, ID 69)

### §7.1 Constructor

```java
new aaa(69, 96)   // blockId=69, texture=96
// material = p.p, hardness 0.5, step sound = stone
// isOpaqueCube=false, renderAsNormal=false, lightOpacity=12(?)
// No AABB set → uses default collision=null approach
```

### §7.2 Meta Bit Layout

```
bits 2-0 (meta & 7): facing/position
bit 3 (meta & 8): isOn (0=off, 8=on)
```

**Facing values:**

| meta & 7 | Position | Block attached to |
|---|---|---|
| 1 | West wall | x-1 |
| 2 | East wall | x+1 |
| 3 | North wall | z-1 |
| 4 | South wall | z+1 |
| 5 | Floor (pointing south) | y-1 |
| 6 | Floor (pointing east) | y-1 |

Floor lever has two variants (5/6) for perpendicular orientations; chosen randomly on placement.

### §7.3 canBlockStay (`c(ry,x,y,z)`)

```java
solid(-X) || solid(+X) || solid(-Z) || solid(+Z) || solid(y-1)
// "solid" = world.g(dx,dy,dz) = isBlockNormalCube
```

### §7.4 canPlace (`d(ry,x,y,z,face)`)

```java
face 1: solid(y-1)    // on top
face 2: solid(z+1)    // +Z wall
face 3: solid(z-1)    // -Z wall
face 4: solid(x+1)    // +X wall
face 5: solid(x-1)    // -X wall
```

### §7.5 Placement (`b(ry,x,y,z,face)`)

```java
existing_on_bit = meta & 8
facing = -1
if (face==1 && solid(y-1)):  facing = 5 + world.w.nextInt(2)   // 5 or 6, random
if (face==2 && solid(z+1)):  facing = 4
if (face==3 && solid(z-1)):  facing = 3
if (face==4 && solid(x+1)):  facing = 2
if (face==5 && solid(x-1)):  facing = 1
if (facing == -1): drop item + remove
else: world.f(x,y,z, facing + existing_on_bit)
```

### §7.6 onBlockActivated (`a(ry,x,y,z,vi)`)

```java
if (world.I) return true    // client-side: no-op (return true to cancel item use)
meta = world.d(x,y,z)
facing = meta & 7
on_bit = 8 - (meta & 8)     // toggles: 0→8 or 8→0
world.f(x,y,z, facing + on_bit)
world.c(x,y,z, x,y,z)       // notifyNeighborsForChange
play "random.click", vol=0.3F, pitch = (on_bit > 0 ? 0.6F : 0.5F)
world.j(x,y,z, bM)           // scheduleBlockUpdate self
// Notify axis-aligned neighbor:
if (facing==1): world.j(x-1,y,z, bM)
elif (facing==2): world.j(x+1,y,z, bM)
...etc
return true
```

### §7.7 onBlockRemoval (`d(ry,x,y,z)`)

```java
if (meta & 8 > 0):   // was ON
    world.j(self + axis neighbor, bM)   // signal neighbors to update
super.d(...)
```

### §7.8 isProvidingWeakPower / StrongPower

```java
b(kq, x,y,z, face):    // weak power
    return (world.d(x,y,z) & 8) > 0   // any face: powered if on_bit set

c(ry, x,y,z, face):    // strong power (to attached block only)
    meta & 8 == 0 → false
    meta & 7 == 6 && face==1 → true   // floor east, power up
    meta & 7 == 5 && face==1 → true   // floor south, power up
    meta & 7 == 4 && face==2 → true   // south wall → power south (+Z)
    meta & 7 == 3 && face==3 → true   // north wall → power north (-Z)
    meta & 7 == 2 && face==4 → true   // east wall  → power east (+X)
    meta & 7 == 1 && face==5 → true   // west wall  → power west (-X)
    else: false

g(): return true   // canProvidePower
```

### §7.9 AABB (`b(kq,x,y,z)`)

Width = 0.1875F (3/16), determined by facing:
- meta 1 (west): x=[0, 0.375], z=[0.3125, 0.6875], y=[0.2, 0.8]
- meta 2 (east): x=[0.625, 1], z=[0.3125, 0.6875], y=[0.2, 0.8]
- meta 3 (north): x=[0.3125, 0.6875], z=[0, 0.375], y=[0.2, 0.8]
- meta 4 (south): x=[0.3125, 0.6875], z=[0.625, 1], y=[0.2, 0.8]
- meta 5/6 (floor): x=[0.25, 0.75], y=[0, 0.6], z=[0.25, 0.75]

---

## §8 BlockPressurePlate (`wx`, IDs 70/72)

### §8.1 Constructor

```java
// Stone pressure plate:
new wx(70, t.bL, xb.b, p.e)   // t=stone block, xb.b=living mob sensor, material=stone
// Wood pressure plate:
new wx(72, x.bL, xb.a, p.d)   // x=planks block, xb.a=all-entity sensor, material=wood
// hardness 0.5, AABB: (1/16, 0, 1/16, 15/16, 1/32, 15/16)
// isOpaqueCube=false, renderAsNormal=false
```

### §8.2 EnumPressurePlateType (`xb`)

| Value | Entity query | Used by |
|---|---|---|
| `xb.a` | All entities (`world.b(null, bbox)`) | Wood plate (ID 72) |
| `xb.b` | Living mobs (`world.a(nq.class, bbox)`) | Stone plate (ID 70) |
| `xb.c` | Players only (`world.a(vi.class, bbox)`) | Unused in 1.0 |

### §8.3 Meta

```
0 = unpressed
1 = pressed
```

### §8.4 canBlockStay (`c(ry,x,y,z)`)

```java
world.g(x, y-1, z) || world.a(x, y-1, z) == yy.aZ.bM
// solid block below OR redstone wire below (yy.aZ = redstone wire?)
```

Note: `yy.aZ` = redstone wire field. Pressure plate can be placed on wire.

### §8.5 Tick Rate

`d()` = 20 ticks (1 second)

### §8.6 Sensor Tick (`g(ry,x,y,z)` private)

```java
wasPressed = (world.d(x,y,z) == 1)
isPressed = false

// Scan AABB slightly smaller than full block (±0.125):
bbox = (x+0.125, y, z+0.125, x+0.875, y+0.25, z+0.875)

if (this.a == xb.a): entities = world.b(null, bbox)            // all entities
if (this.a == xb.b): entities = world.a(nq.class, bbox)        // mobs
if (this.a == xb.c): entities = world.a(vi.class, bbox)        // players
isPressed = entities.size() > 0

if (isPressed && !wasPressed):
    world.f(x,y,z, 1)           // setMeta = pressed
    world.j(x,y,z, bM)          // update self
    world.j(x,y-1,z, bM)        // update below
    world.c(x,y,z, x,y,z)       // notifyNeighborsForChange
    play "random.click", 0.3F, pitch=0.6F

if (!isPressed && wasPressed):
    world.f(x,y,z, 0)           // setMeta = unpressed
    world.j(x,y,z, bM)
    world.j(x,y-1,z, bM)
    world.c(x,y,z, x,y,z)
    play "random.click", 0.3F, pitch=0.5F

if (isPressed):
    world.a(x,y,z, bM, d())     // reschedule check after 20 ticks
```

### §8.7 Triggers

- `a(ry,x,y,z,Random)` randomTick: if meta != 0, call `g()`
- `a(ry,x,y,z,ia)` onEntityWalk: if meta != 1, call `g()`

### §8.8 Power Output

```java
b(kq,x,y,z,face): return meta > 0                // weak: all faces
c(ry,x,y,z,face): return meta==0 ? false : face==1  // strong: upward only
g(): return true   // canProvidePower
```

### §8.9 AABB

```java
b(kq,x,y,z):
    pressed → (1/16, 0, 1/16, 15/16, 1/32, 15/16)
    unpressed → (1/16, 0, 1/16, 15/16, 1/16, 15/16)
// Entity selection box (e()): full 0.5-height centered box
i(): return 1   // getMobilityFlag = 1
```

---

## §9 BlockButton (`ahv`, ID 77)

### §9.1 Constructor

```java
new ahv(77, t.bL)   // stone button, texture from stone block
// hardness 0.5, material = stone, step sound = stone
// isOpaqueCube=false, renderAsNormal=false
// b(true) → tick-scheduled
// No AABB in constructor (handled in b(kq,...) dynamically)
```

**Note:** Wood button (ID 143) was added in Beta 1.7+. Only ID 77 exists in 1.0.

### §9.2 Meta Bit Layout

```
bits 2-0 (meta & 7): facing (1-4, wall only)
bit 3 (meta & 8):    isPressed (0=off, 8=pressed)
```

**Facing:**

| meta & 7 | Wall side | Block attached to |
|---|---|---|
| 1 | West | x-1 |
| 2 | East | x+1 |
| 3 | North | z-1 |
| 4 | South | z+1 |

No floor placement for buttons (wall-only in 1.0).

### §9.3 canBlockStay / canPlace

```java
c(ry,x,y,z):   solid(-X) || solid(+X) || solid(-Z) || solid(+Z)  (4 sides, no floor)
d(ry,x,y,z,face): face in {2,3,4,5}  (no face 1 = no floor placement)
```

### §9.4 Placement (`b(ry,x,y,z,face)`)

```java
existing_on = meta & 8
if (face==2 && solid(z+1)):  facing=4
elif (face==3 && solid(z-1)): facing=3
elif (face==4 && solid(x+1)): facing=2
elif (face==5 && solid(x-1)): facing=1
else: facing = g()   // auto-detect from adjacent solids
world.f(x,y,z, facing + existing_on)
```

`g()` auto-detect: checks -X,+X,-Z,+Z in order; returns 1/2/3/4 for first solid found; default 1.

### §9.5 onBlockActivated (`a(ry,x,y,z,vi)`)

```java
meta = world.d(x,y,z)
facing = meta & 7
on_bit = 8 - (meta & 8)
if (on_bit == 0) return true    // already pressed: no-op
world.f(x,y,z, facing + on_bit)
world.c(x,y,z, x,y,z)
play "random.click", 0.3F, 0.6F
world.j(x,y,z, bM)
world.j(facing-neighbor, bM)
world.a(x,y,z, bM, d())        // schedule release after d()=20 ticks
return true
```

### §9.6 randomTick (`a(ry,x,y,z,Random)`)

Auto-release:
```java
if (meta & 8 != 0):   // was pressed
    world.f(x,y,z, meta & 7)   // clear on_bit
    world.j(self + facing-neighbor, bM)
    play "random.click", 0.3F, 0.5F
    world.c(x,y,z, x,y,z)
```

### §9.7 onBlockRemoval (`d(ry,x,y,z)`)

```java
if (meta & 8 > 0):
    world.j(self + facing-neighbor, bM)
super.d(...)
```

### §9.8 Power Output

```java
b(kq,x,y,z,face): return (meta & 8) > 0   // weak: all faces
c(ry,x,y,z,face):                           // strong: toward attached block
    meta & 8 == 0: false
    meta & 7 == 5 && face==1: true  // dead code (meta 5 unreachable in 1.0)
    meta & 7 == 4 && face==2: true  // south wall → power south
    meta & 7 == 3 && face==3: true  // north wall → power north
    meta & 7 == 2 && face==4: true  // east wall  → power east
    meta & 7 == 1 && face==5: true  // west wall  → power west
g(): return true
```

### §9.9 AABB (`b(kq,x,y,z)`)

Width = 0.375F (6/16), depth = 0.125F (2/16) or 0.0625F (1/16) when pressed:

```
meta & 7 == 1 (west):  x=[0, depth], y=[0.375, 0.625], z=[0.3125, 0.6875]
meta & 7 == 2 (east):  x=[1-depth, 1], ...
meta & 7 == 3 (north): x=[0.3125, 0.6875], y=[0.375, 0.625], z=[0, depth]
meta & 7 == 4 (south): x=[0.3125, 0.6875], ..., z=[1-depth, 1]
// depth = pressed ? 0.0625F : 0.125F
```

---

## §10 BlockOreRedstone (`oc`, IDs 73/74)

### §10.1 Overview

Two block IDs: normal ore (ID 73, `yy.aN`) and glowing ore (ID 74, `yy.aO`). Touching or mining the normal ore switches it to the glowing variant temporarily.

### §10.2 Constructor

```java
new oc(73, 51, false)  // normal ore, texture 51, isGlowing=false; light=0
new oc(74, 51, true)   // glowing ore, texture 51, isGlowing=true; light=0.625 (.a(0.625F))
// material = p.e (stone, requires pickaxe), hardness 3.0, blast resistance 5.0
```

### §10.3 Activation Triggers

All three trigger `g(ry,x,y,z)` → `h()`:
- `b(ry,x,y,z,vi)` onPlayerDestroyed
- `b(ry,x,y,z,ia)` onEntityWalk
- `a(ry,x,y,z,vi)` onBlockActivated (right-click)

### §10.4 `g(ry,x,y,z)` private

```java
if (this.bM == yy.aN.bM):           // is normal ore
    world.g(x,y,z, yy.aO.bM)        // switch to glowing variant
h(world, x, y, z)                   // spawn particles
```

### §10.5 `h(ry,x,y,z)` particle effect

Spawns 6 "reddust" particles near block faces (positioned just outside each face using `world.w.nextFloat()` with face-normal offset 0.0625).

### §10.6 randomTick

```java
if (this.bM == yy.aO.bM):   // is glowing variant
    world.g(x,y,z, yy.aN.bM)  // revert to normal
```

Glowing ore decays back to normal after ~30 ticks (random tick rate).

### §10.7 `b` particle tick

```java
if (this.a):   // isGlowing
    h(world, x, y, z)   // continuous reddust particle effect each frame
```

### §10.8 Drops

```java
a(int fortune, Random rand, int meta):
    return acy.aB.bM   // redstone dust item

a(Random rand, int fortune):
    return a(rand) + rand.nextInt(fortune + 1)  // base + fortune bonus

a(Random rand):
    return 4 + rand.nextInt(2)   // 4 or 5 dust
```

### §10.9 Pick Block

```java
c_(int meta): return new dk(yy.aN)   // always picks normal ore
```

---

## §11 Constants Summary

| Constant | Value | Description |
|---|---|---|
| Wire power | 0–15 | Metadata = signal strength |
| Wire attenuation | -1 per block | Power decreases one per wire length |
| Torch burnout | 8 flips / 100 ticks | Shared static list `cb` |
| Torch tick delay | 2 ticks | `d()` = 2 |
| Repeater delays | 2/4/6/8 ticks | `cb[bits]*2` where `cb={1,2,3,4}` |
| Button press duration | 20 ticks | `d()` = 20 |
| Pressure plate check | 20 ticks | `d()` = 20 |
| Ore glow duration | ~30 ticks | Random tick rate |

---

## §12 Quirks

1. **Wire reentrance guard**: `kw.a` is set false during `v()` query to prevent infinite loop when wire checks its own power. The pattern is: `this.a=false → world.v() → this.a=true`.

2. **Torch burnout `cb` is static**: All redstone torches share ONE burnout history list. A fast-pulsing circuit can "poison" the static list and cause other unrelated torches to malfunction if they share coordinates with the first 8 entries. This is a vanilla bug.

3. **Burnout: fizz sound only on 8th flip**: If the torch burns out on the 8th flip, it plays the fizz sound and smoke particles. If it was already burnt out (9th+ check), it silently stays off without sound.

4. **Wire notifies 0/nonzero transitions only**: The neighbor notification (`cb` HashSet add) only happens when the power level crosses the 0-boundary (0→N or N→0). Interior changes (e.g. 5→4) update the wire metadata but don't add to the dirty set.

5. **Repeater isOn flag vs block ID**: `cc` (isOn field) is per-class, not per-block. The on-state repeater (`mz(94, true)`) always has `cc=true` even if the input signal disappeared — the `randomTick` must fire to switch back. This is the same delay mechanism.

6. **Pressure plate can be placed on redstone wire**: `yy.aZ` = redstone wire appears in the canBlockStay check. Pressure plates placed on wire survive.

7. **Floor lever randomized orientation**: meta 5/6 chosen randomly (nextInt(2)) giving two different visual orientations. Both behave identically for power output.

8. **Lever toggle: bit 8 = 8 - (bit & 8)**: `8 - 0 = 8` (off → on), `8 - 8 = 0` (on → off). Clean toggle.

---

## §13 Open Questions

1. **`world.aZ` vs `yy.av`**: `yy.aZ` appears in pressure plate canBlockStay — verify that `yy.aZ` = `yy.av` (both redstone wire, same field)?

2. **Repeater facing direction for `bi.cc=true`**: The powered repeater's `isProvidingWeakPower` with facing 1 → face 4(+X) appears to indicate output going east (+X). Confirm face 4 = +X direction for neighbor notification purposes.

3. **`world.b(kq,x,y,z)` torch attachment check**: `bg` uses `world.b(x,y,z, true)` which appears to be `isBlockSolidOnSide(x,y,z, sideIndex=true?)`. Clarify signature.

4. **`kw.c()` = 5 light opacity**: RedstoneWire returns light opacity 5. Does this cause visible light absorption in front of wires?

5. **Wood button (ID 143)**: Absent from `yy.java` 1.0 static init. The Coder's stub for 143 should remain as air/no-op until Beta 1.7 parity is targeted.
