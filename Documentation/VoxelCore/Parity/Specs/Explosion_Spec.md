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

# Explosion Spec
**Source classes:** `xp.java` (Explosion), `dd.java` (EntityTNTPrimed), `abm.java` (BlockTNT),
`abh.java` (EntityCreeper — fuse / explode logic), `am.java` (BlockPos helper)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

This spec covers the full explosion pipeline used by Creepers, TNT, Ghast fireballs, and
Beds-in-Nether. The `xp` (Explosion) class is the central algorithm. `dd` (EntityTNTPrimed)
is the primed TNT entity. `abm` (BlockTNT) handles ignition triggers. EntityCreeper's
fuse-and-explode behavior is documented in §6.

---

## 2. Fields

### `xp` (Explosion)

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `boolean` | false | isIncendiary — if true, fire is randomly placed on exposed floors after destruction |
| `b` | `double` | — | Origin X |
| `c` | `double` | — | Origin Y |
| `d` | `double` | — | Origin Z |
| `e` | `ia` | — | Source entity (null = world trigger, e.g. TNT) |
| `f` | `float` | — | Blast power (radius in blocks at full strength) |
| `g` | `Set<am>` | new HashSet | Set of block coordinates to destroy (populated by `a()`) |
| `h` | `Random` | new Random | Local RNG (used for incendiary fire; NOT world random) |
| `i` | `ry` | — | World reference |

### `dd` (EntityTNTPrimed)

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | 80 | Fuse timer in ticks; counts down to 0; at 0 → explode |
| (from `ia`) | `l` | true | — (`l` in Entity = `preventEntitySpawning`? or no-clip; set true in ctor) |
| (from `ia`) | `v/w/x` | random | Initial velocity: horizontal random, w=0.2F upward |

### `am` (BlockPos)

| Field | Type | Semantics |
|---|---|---|
| `a` | `int` | X block coordinate |
| `b` | `int` | Y block coordinate |
| `c` | `int` | Z block coordinate |

hashCode = `a*8976890 + b*981131 + c`. equals = all three match.

---

## 3. Constants & Magic Numbers

| Value | Meaning |
|---|---|
| `16` (`var2`) | Ray grid dimensions: 16×16×16 grid → 1352 surface-only rays |
| `0.3F` (`var21`) | Ray step size in blocks (each step advances 0.3 blocks along direction) |
| `0.75F` | Ray attenuation factor per step: `strength -= (blockResistance + 0.3) * 0.3 * 0.75` wait — see §4 |
| `0.7F + rand*0.6F` | Per-ray starting strength multiplier (world RNG: `world.w.nextFloat()`) |
| `4.0F` | Explosion sound volume |
| `0.3F` | Block item drop chance during explosion (30%) |
| `4.0F` | EntityTNTPrimed explosion power |
| `80` | EntityTNTPrimed initial fuse ticks (4 seconds at 20 Hz) |
| `3.0F` / `6.0F` | Creeper explosion power (normal / powered) |
| `30` | Creeper fuse threshold in ticks (1.5 seconds) |
| `3.0F` / `7.0F` | Creeper ignite distance (normal / powered, in blocks) |
| `(1-dist)*exposure*(power²+power)*4+1` | Entity damage formula (see §4) |

---

## 4. `xp.a()` — Compute Affected Blocks and Apply Entity Damage

This method is called first (from `world.a(entity, x, y, z, power)`) to populate `g`
(the affected block set) and deal damage to entities. It does NOT destroy blocks yet.

### Part 1 — Ray-cast block collection

```
power = f    // snapshot original power
gridSize = 16

for i in 0..15:
    for j in 0..15:
        for k in 0..15:
            // Only process surface voxels of the 16³ cube
            if NOT (i == 0 OR i == 15 OR j == 0 OR j == 15 OR k == 0 OR k == 15):
                continue

            // Compute normalized direction from cube corner
            dx = (float)i / (gridSize - 1) * 2.0 - 1.0    // maps [0,15] → [-1.0, 1.0]
            dy = (float)j / (gridSize - 1) * 2.0 - 1.0
            dz = (float)k / (gridSize - 1) * 2.0 - 1.0
            len = sqrt(dx*dx + dy*dy + dz*dz)
            dx /= len; dy /= len; dz /= len               // normalize

            // Per-ray random starting strength
            strength = f * (0.7 + world.w.nextFloat() * 0.6)    // world.w = world RNG

            // Walk the ray
            rx = b; ry = c; rz = d    // start at explosion origin

            stepSize = 0.3F
            while strength > 0:
                bx = floor(rx); by = floor(ry); bz = floor(rz)
                blockId = world.getBlockId(bx, by, bz)
                if blockId > 0:
                    blastResistance = Block.registry[blockId].getExplosionResistance(sourceEntity)
                    strength -= (blastResistance + 0.3) * stepSize

                if strength > 0:
                    g.add(new am(bx, by, bz))    // block is within blast radius

                rx += dx * stepSize
                ry += dy * stepSize
                rz += dz * stepSize
                strength -= stepSize * 0.75    // fixed attenuation per step
```

**Note:** The `while strength > 0` loop steps in increments of `stepSize = 0.3F`. Each
iteration subtracts `stepSize * 0.75 = 0.225` from strength regardless of block content,
and additionally subtracts `(blastResistance + 0.3) * stepSize` if a block is hit.
A ray terminates when `strength <= 0`.

**Total rays:** The 16³ grid surface has exactly `16³ - 14³ = 4096 - 2744 = 1352` unique
outer voxels. This is the number of ray directions cast.

### Part 2 — Entity damage

After the ray loop, `f` is temporarily doubled for the entity query bounding box:

```
f *= 2.0F

// Build entity query AABB: power*2 in each direction
queryMin = (floor(b - f - 1), floor(c - f - 1), floor(d - f - 1))
queryMax = (floor(b + f + 1), floor(c + f + 1), floor(d + f + 1))

entities = world.getEntitiesWithinAABBExcluding(sourceEntity, AABB(queryMin, queryMax))
origin = Vec3.pool(b, c, d)

for each entity in entities:
    distRatio = entity.distanceTo(b, c, d) / f    // normalized distance 0..1+
    if distRatio <= 1.0:
        // Compute knockback direction
        kx = entity.x - b
        ky = entity.y - c    // entity.t = pos Y
        kz = entity.z - d
        kLen = sqrt(kx*kx + ky*ky + kz*kz)
        kx /= kLen; ky /= kLen; kz /= kLen    // normalize knockback dir

        // Compute exposure fraction (line-of-sight sampling)
        exposure = world.a(origin, entity.aabb)
        // See §5 for world.a(Vec3, AABB) implementation

        // Damage formula
        intensity = (1.0 - distRatio) * exposure
        damage = (int)((intensity * intensity + intensity) / 2.0 * 8.0 * f + 1.0)
        // Note: f here is STILL the doubled value (f = originalPower * 2)

        entity.attackEntityFrom(DamageSource.Explosion, damage)
        entity.motionX += kx * intensity
        entity.motionY += ky * intensity
        entity.motionZ += kz * intensity

f = power    // restore original power
```

**Damage formula expanded (using original power P, where f = 2P at this point):**
```
intensity = (1 - distRatio) * exposure
damage = (int)((intensity² + intensity) / 2 * 8 * 2P + 1)
       = (int)((intensity² + intensity) * 8P + 1)
```

At intensity = 1.0 (point-blank, full exposure): `damage = (1+1) * 8P + 1 = 16P + 1`.
For Creeper (P=3): max damage = 49. For TNT (P=4): max damage = 65.

---

## 5. `world.a(Vec3 origin, AABB entityBounds)` — Exposure Fraction

Computes the fraction of rays from `origin` to the corners/edges of `entityBounds`
that reach without being blocked by terrain. This is the entity's "line-of-sight fraction"
relative to the explosion centre.

```
stepX = 1.0 / ((entityBounds.maxX - entityBounds.minX) * 2.0 + 1.0)
stepY = 1.0 / ((entityBounds.maxY - entityBounds.minY) * 2.0 + 1.0)
stepZ = 1.0 / ((entityBounds.maxZ - entityBounds.minZ) * 2.0 + 1.0)

totalRays = 0
hitRays = 0

for tx = 0.0 to 1.0 step stepX:
    for ty = 0.0 to 1.0 step stepY:
        for tz = 0.0 to 1.0 step stepZ:
            // Interpolate a point on the entity AABB surface
            px = entityBounds.minX + (entityBounds.maxX - entityBounds.minX) * tx
            py = entityBounds.minY + (entityBounds.maxY - entityBounds.minY) * ty
            pz = entityBounds.minZ + (entityBounds.maxZ - entityBounds.minZ) * tz

            // Ray trace from px/py/pz to explosion origin
            if world.rayTraceBlocks(Vec3.pool(px,py,pz), origin) == null:
                hitRays++    // no obstruction
            totalRays++

return hitRays / totalRays    // exposure fraction [0.0, 1.0]
```

`world.rayTraceBlocks(start, end)` returns null if no opaque block is hit along the ray.

---

## 6. `xp.a(boolean doParticles)` — Destroy Blocks and Spawn Particles

Called immediately after `xp.a()` from `world.a()`.

```
// Sound effect
world.playSound(b, c, d, "random.explode", 4.0F,
    pitch = (0.7F + (world.w.nextFloat() - world.w.nextFloat()) * 0.2F))

// Explosion particle at origin
world.spawnParticle("hugeexplosion", b, c, d, 0.0, 0.0, 0.0)

// Process affected blocks (reverse iteration over ArrayList copy of g)
blocks = new ArrayList(g)

for i = blocks.size()-1 downto 0:
    block = blocks[i]
    bx = block.a; by = block.b; bz = block.c
    blockId = world.getBlockId(bx, by, bz)

    if doParticles:
        // Particle position: random within block, then push outward from origin
        px = bx + world.w.nextFloat()
        py = by + world.w.nextFloat()
        pz = bz + world.w.nextFloat()
        pdx = px - b; pdy = py - c; pdz = pz - d
        pdist = sqrt(pdx*pdx + pdy*pdy + pdz*pdz)
        pdx /= pdist; pdy /= pdist; pdz /= pdist
        scale = 0.5 / (pdist / f + 0.1)
        scale *= world.w.nextFloat() * world.w.nextFloat() + 0.3F
        // "explode" particle at midpoint between block pos and origin
        world.spawnParticle("explode", (px + b) / 2, (py + c) / 2, (pz + d) / 2,
                            pdx*scale, pdy*scale, pdz*scale)
        // "smoke" particle at block pos
        world.spawnParticle("smoke", px, py, pz, pdx*scale, pdy*scale, pdz*scale)

    if blockId > 0:
        // Drop items at 30% chance
        Block.registry[blockId].dropBlockAsItemWithChance(
            world, bx, by, bz,
            world.getBlockMetadata(bx, by, bz),
            0.3F, 0)
        // Remove block
        world.setBlockToAir(bx, by, bz)
        // Notify block
        Block.registry[blockId].onBlockDestroyedByExplosion(world, bx, by, bz)

// Incendiary fire pass (only if xp.a == true)
if isIncendiary:
    for each block in blocks:
        bx = block.a; by = block.b; bz = block.c
        currentId = world.getBlockId(bx, by, bz)
        floorId = world.getBlockId(bx, by-1, bz)
        if currentId == 0 AND Block.opaqueCubeLookup[floorId] AND h.nextInt(3) == 0:
            world.setBlock(bx, by, bz, FireBlockId)    // yy.ar.bM = fire block ID
```

**Note:** The incendiary check uses the local `h` (new Random()) not the world RNG.
`Block.opaqueCubeLookup[floorId]` = `yy.m[floorId]` — the static opaque-cube array.

---

## 7. `dd` (EntityTNTPrimed) — Primed TNT Entity

### Constructor `dd(ry world, double x, double y, double z)`

```
super(world)
l = true                              // unknown flag (canTriggerWalking=false?)
setSize(0.98F, 0.98F)                 // width 0.98, height 0.98
L = N / 2.0F                         // eye height = height/2 = 0.49

// Initial velocity: random horizontal direction at 0.02 speed, 0.2 upward
angle = random() * PI * 2
v = -sin(angle * PI / 180) * 0.02F   // motionX
w = 0.2F                             // motionY (upward)
x = -cos(angle * PI / 180) * 0.02F   // motionZ

a = 80                               // fuse: 80 ticks = 4 seconds
// Store spawn position in p/q/r (previous-position fields)
p = x; q = y; r = z
```

**Note:** `random()` here is `Math.random()` (Java static), not the world RNG.

### `dd.a()` — Entity Tick

```
// Save prev position
p = s; q = t; r = u

// Gravity
w -= 0.04F

// Apply velocity + move + friction
move(v, w, x)     // standard entity movement with collision
v *= 0.98F
w *= 0.98F
x *= 0.98F

// Ground friction
if D (onGround):
    v *= 0.7F
    x *= 0.7F
    w *= -0.5

// Fuse countdown
a--
if a <= 0:
    if NOT world.isRemote:
        v()        // remove entity (isDead = true)
        g()        // explode (server-only)
    else:
        v()        // client: just remove (no explosion)
else:
    // Smoke particle while fuse burning
    world.spawnParticle("smoke", s, t + 0.5, u, 0.0, 0.0, 0.0)
```

### `dd.g()` — Explode

```
power = 4.0F
world.a(null, s, t, u, power)    // source entity = null
```

### NBT — `dd.a(NbtCompound)` / `dd.b(NbtCompound)`

Write: `"Fuse"` TAG_Byte = a (fuse ticks)
Read: `a = tag.getByte("Fuse")`

### `dd.i_()` — Eye Height

Returns `0.0F` (no eye).

### `dd.e_()` — canBeAttackedByPlayer

Returns `!K` (can be attacked while alive).

### `dd.d_()` — shouldSetFire

Returns `false` (TNT does not catch fire on its own).

---

## 8. `abm` (BlockTNT) — TNT Block

### Constructor

`super(id, textureIndex, p.r)` — material `p.r` (TNT material).

### `b(int face)` — Texture by Face

```
if face == 0: return bL + 2    // bottom: cross texture
if face == 1: return bL + 1    // top: cross texture (different)
else:         return bL         // sides: TNT label texture
```

### `a(ry, x, y, z)` — onBlockAdded

```
super.a(world, x, y, z)
if world.isBlockPowered(x, y, z):    // world.v(x,y,z)
    e(world, x, y, z, 1)             // ignite
    world.setBlockToAir(x, y, z)
```

### `a(ry, x, y, z, neighborId)` — onNeighborBlockChange

```
if neighborId > 0 AND Block.registry[neighborId].canDropFromExplosion()    // yy.k[neighborId].g()
   AND world.isBlockPowered(x, y, z):
    e(world, x, y, z, 1)    // ignite
    world.setBlockToAir(x, y, z)
```

### `a(Random)` — getItemDropped

Returns `0` (no drop through normal mining — handled in `e()` below).

### `e(ry, x, y, z, meta)` — harvestBlock / explodeBlock

```
if world.isRemote: return

if (meta & 1) == 0:
    // Non-ignited break: drop TNT block as item
    spawnItemStack(world, x, y, z, new ItemStack(yy.am.bM, 1, 0))
else:
    // Ignited: spawn EntityTNTPrimed
    tnt = new dd(world, x + 0.5, y + 0.5, z + 0.5)
    world.spawnEntity(tnt)
    world.playSound(tnt, "random.fuse", 1.0F, 1.0F)
```

### `i(ry, x, y, z)` — onBlockDestroyedByExplosion

Called when another explosion destroys a TNT block. Spawns primed TNT with reduced
random fuse (shorter delay before this TNT also explodes):

```
tnt = new dd(world, x + 0.5, y + 0.5, z + 0.5)
tnt.a = world.w.nextInt(tnt.a / 4) + tnt.a / 8
// tnt.a was 80 at construction; fuse = nextInt(20) + 10 = 10..30 ticks (0.5..1.5 seconds)
world.spawnEntity(tnt)
```

### `b(ry, x, y, z, player)` — onPlayerDestroyed

```
if player.getHeldItem() != null AND player.getHeldItem().itemId == acy.h.bM:
    // Player holding flint+steel (acy.h = flint+steel item)
    world.c(x, y, z, 1)    // ignite with meta=1 → calls e() which spawns dd
return
super.b(world, x, y, z, player)
```

### `a(ry, x, y, z, player)` — onBlockActivated

Returns `super.a(...)` = false (no direct activation).

### `c_(meta)` — getItem

Returns `null` (drops handled manually in `e()`).

---

## 9. EntityCreeper (`abh`) — Fuse and Explode Logic

### Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `b` | `int` | 0 | Current fuse countdown (server) / rendered interpolation base (client) |
| `c` | `int` | 0 | Previous fuse value (saved each tick for smooth client interpolation) |
| (DW 16) | `byte` | -1 | Networked fuseCountdown delta: +1 = approaching, -1 = retreating |
| (DW 17) | `byte` | 0 | isPowered: 0=normal, 1=charged (set on lightning strike) |

### `b()` — DataWatcher init

```
super.b()
dataWatcher.addObject(16, (byte)-1)    // fuseCountdown = -1 (not fusing)
dataWatcher.addObject(17, (byte)0)     // isPowered = false
```

### `a()` — Entity Tick

```
c = b    // save previous fuse for interpolation

if world.isRemote (client):
    dw16 = ay()    // read DW16 (fuseCountdown delta)
    if dw16 > 0 AND b == 0:
        world.playSound(this, "random.fuse", 1.0F, 0.5F)    // fuse start sound (client)
    b += dw16
    if b < 0: b = 0
    if b >= 30: b = 30

super.a()    // ww/nq entity tick (runs AI via n())

// If fuse is burning but we lost our target: defuse
if h == null AND b > 0:
    b(-1)    // DW16 = -1
    b--
    if b < 0: b = 0
```

### `a(ia target, float dist)` — In-Range Behavior (fuse tick)

This overrides `zo.a(ia,dist)`. Creeper does NOT melee attack — it fuses instead.

```
if world.isRemote: return

igniteRange = (ay() <= 0) ? 3.0F : 7.0F    // normal=3, powered=7
if dist < igniteRange:
    // Start/continue fusing
    if b == 0:
        world.playSound(this, "random.fuse", 1.0F, 0.5F)    // fuse start sound (server)
    b(1)    // DW16 = 1 (notify client: fusing)
    b++
    if b >= 30:
        // Explode
        power = ax() ? 6.0F : 3.0F    // ax() = isPowered
        world.a(this, s, t, u, power)  // create explosion
        v()                            // remove creeper entity

    i = true    // mark as in-attack-range (overrides facing in followpath)
else:
    // Out of ignite range: defuse
    b(-1)    // DW16 = -1
    b--
    if b < 0: b = 0
```

### `b(ia target, float dist)` — Out-of-Range Behavior (defuse)

This overrides `zo.b(ia,dist)` (called when target is NOT in attack range):

```
if world.isRemote: return
b(-1)    // DW16 = -1
b--
if b < 0: b = 0
```

### `a(pm, int)` — on-death: drop music disc if killed by Skeleton

```
super.a(deathSource, amount)    // call parent death handler
if deathSource.getEntity() instanceof it:    // killed by Skeleton
    // Drop a random music disc
    spawnDropItem(acy.bB.bM + world.Y.nextInt(2), 1)
    // bB.bM = 2256 ("13"), bB.bM+1 = 2257 ("cat")
```

### `ax()` — isPowered

Returns `dataWatcher.getByte(17) == 1`.

### `ay()` — getFuseCountdown (DW16)

Returns `dataWatcher.getByte(16)`.

### `a(SoundEvent)` / `b(SoundEvent)` — private DW setters

`b(int val)` = sets DW16 to `(byte)val`.

### `g(float partialTick)` — Fuse Interpolation (render)

Used by the renderer to animate fuse flash:

```
return ((float)c + (float)(b - c) * partialTick) / 28.0F
// Returns 0..1 as fuse progresses; 1.0 = 28/28, slightly before 30 max
```

### `a(LightningBolt)` — onStruckByLightning

```
super.a(lightning)
dataWatcher.set(17, (byte)1)    // become powered/charged
```

### `k()` — getDropItemId

Returns `acy.L.bM` (gunpowder item ID).

### `f_()` — getMaxHealth

Returns `20`.

### NBT

Write: if DW17==1, write `"powered": true`
Read: set DW17 = tag.getBoolean("powered") ? 1 : 0

---

## 10. Bitwise & Data Layouts

### BlockTNT `e(meta)` ignition flag

```
Bit 0 of meta:
  0 = normal break (drop item)
  1 = ignited break (spawn EntityTNTPrimed, play fuse sound)
```

### EntityCreeper DataWatcher

```
DW slot 16 (byte): fuseCountdown delta
  -1 = defusing (b is decreasing)
   0 = idle (b unchanged from external source)
  +1 = fusing (b is increasing each tick in target range)

DW slot 17 (byte): isPowered
   0 = normal
   1 = charged (struck by lightning)
```

---

## 11. Tick Behaviour

- **`xp`** is instantiated and run synchronously within the same tick it is created (by `world.a(entity, x, y, z, power)`). It is not a ticking entity.
- **`dd`** ticks every server tick; fuse counts down from 80 (4 s) to 0, then explodes.
- **`abm`** has no random tick; responds only to neighbor-change and redstone power events.
- **Creeper fuse** advances 1 per tick while in range; retreats 1 per tick while out of range.

---

## 12. Known Quirks / Bugs to Preserve

1. **World RNG used for ray strength:** `world.w.nextFloat()` (the world RNG) is consumed
   once per ray (1352 times). This advances the world random state by 1352 calls per
   explosion and affects any subsequent RNG calls that session tick.

2. **Entity damage uses doubled power:** When computing entity damage, `f` is temporarily
   multiplied by 2 (`f *= 2`) for the entity query bbox, and this doubled value is also used
   in the damage formula `* 8.0 * (double)this.f`. The power is restored after. This means
   entity damage scales with `2P` not `P` — effectively `damage ≈ 16P + 1` at full intensity.

3. **Incendiary fire uses local Random, not world RNG:** The `h = new Random()` in `xp` is
   seeded from Java's default (nondeterministic). Incendiary fire placement is not
   deterministic from the world seed.

4. **TNT `i()` fuse reduction:** When a TNT block is destroyed by another explosion, its
   EntityTNTPrimed fuse is `nextInt(fuse/4) + fuse/8`. At construction `a=80`, so the primed
   fuse = `nextInt(20) + 10` = [10, 29] ticks (0.5–1.45 seconds), much shorter than 4 seconds.

5. **Creeper fuse caps at 30:** The fuse counter `b` is clamped to [0, 30]. It does not
   continue past 30 — the explosion fires exactly when `b` reaches or exceeds 30.

6. **Music disc drop from Skeleton-kill:** Creeper drops a disc only when killed by an arrow
   from an `it` (Skeleton), not by any other projectile or attack. The disc is selected from
   `acy.bB.bM + nextInt(2)` = record "13" or "cat".

7. **Client-only sound for fuse start:** The fuse start sound check `if dw16 > 0 AND b == 0`
   runs on the client (from DW16 network update). The server also plays the sound in
   `a(ia,dist)` when `b == 0`. Both fire — double-sound is expected vanilla behaviour.

---

## 13. Open Questions

1. **`world.v(x,y,z)` implementation:** Called in `abm.a()` and `abm.a(ry,x,y,z,neighborId)`
   to check if the TNT block is powered. Likely `world.isBlockProvidingPowerTo(x,y,z,face)`
   or `world.isBlockIndirectlyReceivingPower(x,y,z)`. Needs `BlockRedstone_Spec.md` to resolve.

2. **`world.c(x,y,z,meta)` in `abm.b()` (onPlayerDestroyed):** When a player with flint+steel
   right-clicks TNT, `world.c(x,y,z,1)` is called. This likely sets the block at the position
   to the same block but with new metadata — i.e., calls `setBlockMetadataWithNotify(x,y,z,1)`.
   The metadata change triggers `onBlockAdded` which calls `e(world,x,y,z,1)` → ignite.

3. **`dd.l = true` field:** Entity `l` set to true in EntityTNTPrimed constructor. In Entity,
   `l` may be `preventEntitySpawning`, `noClip`, or `canTriggerWalking = false`. Its exact
   effect needs verification from the Entity spec.

4. **`Block.getExplosionResistance(entity)` / `yy.a(entity)`:** The explosion ray attenuation
   uses `Block.registry[blockId].a(sourceEntity)` for per-block blast resistance. This method
   may return different values for different source entity types (e.g. TNT vs Ghast). Needs
   verification — likely returns `blastResistance / 5.0F` (a standard conversion factor).

5. **`world.rayTraceBlocks(start, end)` in exposure calculation:** `world.a(Vec3, origin)`
   returns null on no hit. The exact method signature and which ray-trace function this maps
   to in `ry.java` needs confirmation.
