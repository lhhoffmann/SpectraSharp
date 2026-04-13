# Entity Spec
Source class: `ia.java`
Type: `abstract class`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`Entity` (`ia`) is the base class for every in-world object: mobs, players, projectiles,
dropped items, falling sand, boats, minecarts, etc. It owns the position, velocity,
rotation, bounding box, fire/air timers, and the mount/rider relationship. The `World`
stores all entities in `g` (List\<ia\>), ticks them via `f(entity)` (which calls `a()`), and
manages chunk assignment using the `ah/ai/aj/ak` fields.

---

## 2. Fields

### Identity

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` (static) | `int` | auto-incremented | Global entity ID counter; each new entity gets `a++` |
| `j` | `int` | `a++` | entityId — unique per JVM session; used for equals/hashCode |

### World

| Field | Type | Default | Semantics |
|---|---|---|---|
| `o` | `ry` | ctor arg | World reference |

### Position and motion

| Field | Type | Default | Semantics |
|---|---|---|---|
| `p` | `double` | 0 | prevPosX |
| `q` | `double` | 0 | prevPosY |
| `r` | `double` | 0 | prevPosZ |
| `s` | `double` | 0 | posX |
| `t` | `double` | 0 | posY |
| `u` | `double` | 0 | posZ |
| `v` | `double` | 0 | motionX |
| `w` | `double` | 0 | motionY |
| `x` | `double` | 0 | motionZ |
| `R` | `double` | 0 | lastTickPosX (for render interpolation) |
| `S` | `double` | 0 | lastTickPosY |
| `T` | `double` | 0 | lastTickPosZ |

### Rotation

| Field | Type | Default | Semantics |
|---|---|---|---|
| `y` | `float` | 0 | rotationYaw (degrees, wrapped mod 360) |
| `z` | `float` | 0 | rotationPitch (degrees, clamped ±90) |
| `A` | `float` | 0 | prevRotationYaw |
| `B` | `float` | 0 | prevRotationPitch |

### Bounding box

| Field | Type | Default | Semantics |
|---|---|---|---|
| `C` | `c` (AABB, final) | `c.a(0,0,0,0,0,0)` | Axis-aligned bounding box — **final** instance, mutated in place by `d(x,y,z)` |
| `M` | `float` | `0.6F` | Entity width (box = M × M footprint, symmetric around posX/posZ) |
| `N` | `float` | `1.8F` | Entity height |
| `L` | `float` | `0.0F` | yOffset — downward eye/foot shift (positive = feet are below posY) |
| `U` | `float` | `0.0F` | ySize — vertical climbing expansion (grows when partially inside a block) |

### Movement state flags

| Field | Type | Default | Semantics |
|---|---|---|---|
| `D` | `boolean` | `false` | onGround |
| `E` | `boolean` | — | horizontalMoved (set each tick in `b()` move) |
| `F` | `boolean` | — | verticalMoved |
| `G` | `boolean` | `false` | moved (E or F) |
| `H` | `boolean` | `false` | isCollidedHorizontally |
| `I` | `boolean` (protected) | — | isCollidedVertically / isInWall |
| `J` | `boolean` | `true` | If `false`, any blocked axis zeroes all motion (used for web/wall-clip detection) |
| `W` | `boolean` | `false` | isInWeb — when `true`, overrides `b()` to scale motion by 0.25/0.05/0.25 and zero velocity |

### State flags

| Field | Type | Default | Semantics |
|---|---|---|---|
| `K` | `boolean` | `false` | isDead — set by `v()`, checked by World when removing |
| `l` | `boolean` | `false` | Unknown (possibly isImmuneToFire subtype flag) |
| `ab` (protected) | `boolean` | `false` | isInWater — set `true` by `h_()` when overlapping water material |
| `af` (protected) | `boolean` | `false` | isImmuneToFire — fire ticks decrease 4× faster; no fire damage |
| `d` (private) | `boolean` | `true` | firstUpdate flag — set `false` after first `b()` (move) call |
| `ap` | `boolean` | — | velocityChanged — set `true` by `h()` (addVelocity); used by networking |
| `ao` | `boolean` | — | unknown |

### Timers and counters

| Field | Type | Default | Semantics |
|---|---|---|---|
| `Z` | `int` | `0` | ticksExisted — incremented every `w()` call |
| `c` (private) | `int` | `0` | fireTicks: positive = on fire (decrements each tick, deals damage every 20 ticks); negative = cooldown |
| `aa` | `int` | `1` | fireImmuneTicks — used as initial negative cooldown after fire extinguished |
| `Q` | `float` | `0.0F` | fallDistance — accumulated while falling (decremented on landing) |
| `O` | `float` | `0.0F` | prevDistanceWalkedModified |
| `P` | `float` | `0.0F` | distanceWalkedModified — accumulated horizontal movement for step-sound timing |
| `b` (private) | `int` | `1` | stepSoundTimer — step sound fires when `P > b` |
| `V` | `float` | `0.0F` | stepHeight — max height the entity auto-steps up (0=none, 0.5 for players/mobs) |

### Size and render

| Field | Type | Default | Semantics |
|---|---|---|---|
| `k` | `double` | `1.0` | Render distance multiplier: entity is culled beyond `AABB.c() * 64 * k` blocks |
| `X` | `float` | `0.0F` | entityCollisionReduction — 0=full push-back, 1=no push-back in `e(ia)` |

### Mount / rider

| Field | Type | Default | Semantics |
|---|---|---|---|
| `m` | `ia` | `null` | mount — the entity this entity is riding ON |
| `n` | `ia` | `null` | rider — the entity riding ON TOP of this entity |

### Chunk tracking

| Field | Type | Default | Semantics |
|---|---|---|---|
| `ah` | `boolean` | `false` | addedToChunk |
| `ai` | `int` | — | chunkCoordX (chunk-grid X of containing chunk) |
| `aj` | `int` | — | chunkCoordY (entity bucket index = floor(posY / 16)) |
| `ak` | `int` | — | chunkCoordZ |
| `al/am/an` | `int` | — | Additional position storage (exact semantics not confirmed from base class) |

### DataWatcher

| Field | Type | Default | Semantics |
|---|---|---|---|
| `ag` | `cr` (DataWatcher) | new cr() | Synchronized entity data for client/server |

DataWatcher registers in constructor:
- Index `0` (byte) — entity flags (bit field, see §6)
- Index `1` (short) — air supply (default `300`)

### Names and misc

| Field | Type | Default | Semantics |
|---|---|---|---|
| `ad` | `String` | — | Unknown string (possibly entity class name or type tag) |
| `ae` | `String` | — | Entity UUID (string form) |
| `Y` | `Random` (protected) | `new Random()` | Entity's own Random instance |
| `ac` | `int` | `0` | Unknown int |
| `e/f` (private double) | — | 0 | Yaw/pitch accumulator for mount rotation (used in `M()`) |

---

## 3. Constructor

```java
ia(ry world) {
    this.o = world;
    this.d(0.0, 0.0, 0.0);   // setPosition, initialises AABB
    this.ag.a(0, (byte)0);    // register flags byte
    this.ag.a(1, (short)300); // register air supply short
    this.b();                 // abstract entityInit()
}
```

The AABB `C` is `final` — the same instance is used for the entity's entire lifetime.
`d(0,0,0)` initialises it to a zero-size box at origin; it is resized when `a(width, height)`
(setSize) is called by the subclass's `b()` (entityInit).

---

## 4. Core Tick Methods

### tick — `a()`

Default implementation:
```java
this.w();
```

Subclasses override `a()` and typically call `super.a()` or `super.w()` first.

### entityBaseTick — `w()`

Called every tick. Key steps:
1. If riding a dead mount: `n = null`
2. Increment `Z` (ticksExisted)
3. Save `O = P` (prevDistanceWalked), `p/q/r = s/t/u` (prevPos), `B/A = z/y` (prevRot)
4. If sprinting (`X()` = dataWatcher bit 3): spawn block-crumbling particles at feet
5. If entered water (`h_()`): play splash sound, spawn bubble/splash particles, reset `Q = 0`, set `ab = true`; else `ab = false`
6. Fire tick processing (server-side):
   - If `c > 0` and not immune: every 20 ticks deal fire damage (`a(pm.b, 1)`); decrement `c`
   - If immune: `c -= 4` per tick
7. If in lava (`F()`): call `x()` (setOnFireFromLava), halve `Q`
8. If `t < -64.0`: call `z()` (kill — falls into void)
9. Update DataWatcher: flag bit 0 = `c > 0` (isOnFire), bit 2 = `n != null` (isRiding)
10. Set `d = false` (firstUpdate done)

### setDead — `v()`

```java
K = true;
```

---

## 5. Movement — `b(double dx, double dy, double dz)`

Full sweep-collision movement. Called with intended motion deltas:

1. If `W` (isInWeb): scale motion: `dx *= 0.25`, `dy *= 0.05F`, `dz *= 0.25`; zero velocity
2. Save `dx0 = dx`, `dy0 = dy`, `dz0 = dz`, original AABB copy
3. Step-assist setup if `D` (onGround) and `V > 0`: save AABB for possible step-up attempt
4. Get colliding boxes: `List = world.a(this, AABB.expand(dx, dy, dz))`
5. Sweep Y: for each box: `dy = box.calculateYOffset(AABB, dy)`; move AABB: `AABB.offset(0, dy, 0)`
6. If `J == false` and `dy != dy0`: `dx = dy = dz = 0` (**noClip gate**)
7. Sweep X: `dx = box.calculateXOffset(AABB, dx)`; move: `AABB.offset(dx, 0, 0)`
8. If `J == false` and `dx != dx0`: `dx = dy = dz = 0`
9. Sweep Z: `dz = box.calculateZOffset(AABB, dz)`; move: `AABB.offset(0, 0, dz)`
10. If `J == false` and `dz != dz0`: `dx = dy = dz = 0`
11. **Step-assist** (if `V > 0`, was-on-ground or was-blocked-Y, and X or Z still blocked):
    - Reset AABB to original, retry with `dy = V` then step down by −V
    - If resulting X²+Z² ≥ original X²+Z²: discard step; else accept and accumulate `U` += fractional Y
12. Update position from AABB centre:
    ```
    s = (C.a + C.d) / 2.0
    t = C.b + L - U
    u = (C.c + C.f) / 2.0
    ```
13. Set flags: `E = (dx0 != dx || dz0 != dz)`, `F = (dy0 != dy)`, `D = (dy0 != dy && dy0 < 0)`, `G = E || F`
14. Call `a(dy, D)` — fall damage check
15. Zero velocity components where motion was blocked: if `dx0 != dx → v=0`, `dy0 != dy → w=0`, `dz0 != dz → x=0`
16. If `d_()` (canPlayStepSound) and not in web and not riding: accumulate `P`, play step sound when `P > b`
17. Walk through blocks: notify `Block.a(world, bx, by, bz, this)` = entityCollidedWithBlock
18. Check fire damage: if inside lava-adjacent blocks with material = fire

---

## 6. DataWatcher Entity Flags (byte at index 0)

| Bit | Method | Name |
|---|---|---|
| 0 | `V()` = `f(0)` | isOnFire |
| 1 | `q()` = `f(1)` | isSneaking / isCrouching |
| 2 | `W()` uses `f(2)` | isRiding (also checks `n != null`) |
| 3 | `X()` = `f(3)` | isSprinting |
| 4 | `Y()` = `f(4)` | isEating / isUsingItem |

Read: `(ag.a(0) & (1 << bit)) != 0`  
Write: `ag.b(0, (byte)(current | (1 << bit)))` or `& ~(1 << bit)` to clear.

---

## 7. AABB Layout

The AABB `C` is updated by `d(double posX, double posY, double posZ)`:

```java
float halfW = M / 2.0F;
float h = N;
C.c(
    posX - halfW,
    posY - L + U,
    posZ - halfW,
    posX + halfW,
    posY - L + U + h,
    posZ + halfW
);
```

So for a default entity with `L=0`, `U=0`, `M=0.6`, `N=1.8`:
- AABB minX = posX − 0.3
- AABB minY = posY
- AABB maxX = posX + 0.3
- AABB maxY = posY + 1.8
- posX = centre of AABB on X axis
- posY = bottom of AABB (`C.b`)

For entities with non-zero `L`: posY = `C.b + L` (the entity "stands" L units above the
bottom of the bounding box — used by mounts/riders so the rider sits at the right height).

---

## 8. Mount / Rider System

| Link | Meaning |
|---|---|
| `m` | The entity this is currently riding ON |
| `n` | The entity currently riding ON TOP of this entity |

At most one rider per entity (no branching). The chain is `rider.m = mount`, `mount.n = rider`.

### mountEntity — `g(ia target)`

- If `target == null`: dismount (clear `n` link from old mount, reset position to mount top)
- If `n == target` (already riding): also dismount (resets to mount-top position)
- Else: establish new link (`n = target`, `target.m = this`)

### rideTick — `M()`

Called by World for entities with `n != null` (the RIDER, not the mount). Steps:
1. If mount is dead: `n = null`, return
2. Zero own velocity; call `a()` (tick as rider)
3. If still mounted: call `n.N()` (mountUpdate), accumulate yaw/pitch delta from mount
4. Apply capped rotation delta (±10 degrees/tick) to own yaw/pitch

### mountUpdate — `N()`

Updates the rider's position to sit on top of the mount:
```java
mount.d(mount.s, mount.t + mount.P() + mount.m.O(), mount.u);
//  P() = N * 0.75 = mount's rider offset
//  O() = L = rider's vertical offset (own feet level)
```

### getRiderOffset — `P()` → `double`

```java
return (double)N * 0.75;
```

Returns how high a rider sits above the mount's posY (3/4 of mount's height).

### getMountOffset — `O()` → `double`

```java
return (double)L;
```

---

## 9. Item Drop Methods

### dropItem — `b(int itemId, int count)` → `ih`

```java
return a(itemId, count, 0.0F);
```

### dropItemWithOffset — `a(int itemId, int count, float yOffset)` → `ih`

```java
return a(new dk(itemId, count, 0), yOffset);
```

### dropItemStack — `a(dk itemStack, float yOffset)` → `ih`

```java
ih entityItem = new ih(world, posX, posY + yOffset, posZ, itemStack);
entityItem.c = 10;   // pickup delay = 10 ticks
world.a(entityItem); // spawnEntityInWorld
return entityItem;
```

`dk` = ItemStack, `ih` = EntityItem. Pickup delay of 10 ticks means no entity can pick
up the item for the first 10 ticks after spawning.

---

## 10. Distance Methods

| Method | Signature | Returns |
|---|---|---|
| `c(ia)` | `float c(ia other)` | Euclidean distance (float, via MathHelper sqrt) |
| `f(double,double,double)` | `double f(x,y,z)` | Squared distance to point |
| `g(double,double,double)` | `double g(x,y,z)` | Euclidean distance to point (via MathHelper sqrt, float precision) |
| `d(ia)` | `double d(ia other)` | Squared distance to other entity's position |

### isInRangeToRender — `a(fb cameraPos)` → `boolean`

```java
double distSq = squaredDistanceTo(cameraPos);
double renderDist = C.c() * 64.0 * k;  // c() = mean size of AABB
return distSq < renderDist * renderDist;
```

### isInRangeToRenderByDist — `a(double distanceSq)` → `boolean`

```java
double d = C.c() * 64.0 * k;
return distanceSq < d * d;
```

---

## 11. Other Key Methods

### setPosition — `d(double x, double y, double z)`

Updates `s/t/u` AND resizes `C` (AABB) to match. **Always use this instead of setting s/t/u directly.**

### setSize (protected) — `a(float width, float height)`

```java
M = width; N = height;
```

Called by subclass `b()` (entityInit). Note: must follow with `d(s,t,u)` to resize the AABB.

### setLocationAndAngles — `b(double x, double y, double z, float yaw, float pitch)`

Sets pos and rotation; syncs `R/S/T` (lastTickPos) to new pos; sets `U=0`; calls `d(x,y,z)`.

### setPositionAndRotation — `c(double x, double y, double z, float yaw, float pitch)`

Same but also sets `R = p = s = x` etc. (all prev pos = new pos). Note: `t` is offset by `L`:
`S = q = t = y + L`.

### addVelocity — `h(double dx, double dy, double dz)`

```java
v += dx; w += dy; x += dz; ap = true;
```

### applyEntityCollision — `e(ia other)`

Pushes both entities apart based on XZ distance. Reduction factor: `1 - X`, `1 - other.X`.
Scale 0.05, then calls `h(-dx, 0, -dz)` on self and `h(dx, 0, dz)` on other.
Skips if they are mount/rider pair.

### isInside8CornerTest — `L()` → `boolean`

Tests all 8 corners of the entity's bounding box for opacity:
```
corners: (±0.4*M, ±0.1, ±0.4*M) offsets from centre
```
Returns `true` if any corner is inside an opaque block (`world.g(x,y,z)` = isWet actually... wait, `o.g(x,y,z)` = `world.g(x,y,z)` which from World spec is isWet. Let me re-check.)

**Correction:** `this.o.g(var5, var6, var7)` in ia.java line 906 — `world.g(x,y,z)` is the `isWet` IBlockAccess method. So `L()` tests if the entity corners are in wet/liquid blocks, not opaque blocks. Returns `true` if any of 8 corners is in a wet block.

### isEntityAlive — `K()` → `boolean`

```java
return !K;
```

### getFallDamageThreshold — `E()` → `float`

```java
return 0.0F;
```

Eye height above posY. Overridden by mobs/players to offset eye level.

### isImmuneToFire — `B()` → `boolean` (final)

```java
return af;
```

### setFire — `e(int seconds)`

```java
int ticks = seconds * 20;
if (c < ticks) c = ticks;
```

### extinguish — `y()`

```java
c = 0;
```

### isOnFire — `V()` → `boolean`

```java
return c > 0 || f(0);  // fireTicks > 0 OR dataWatcher flag set
```

### setPositionInWall — `aa()`

```java
I = true;  // isCollidedVertically → next move() will scale motion by 0.25
```

### getParts — `ab()` → `ia[]`

Returns `null`. Overridden by multi-part entities (e.g., EnderDragon).

### writeToNBT / readFromNBT (abstract hook)

- `a(ik)` — abstract: write subclass NBT  
- `b(ik)` — abstract: read subclass NBT

Base `d(ik)` writes: Pos, Motion, Rotation, FallDistance, Fire, Air, OnGround.  
Base `e(ik)` reads same, with NaN/Inf guards on motion (zeroed if |component| > 10).

---

## 12. Abstract Methods

| Signature | Purpose |
|---|---|
| `protected abstract void b()` | entityInit — called from constructor; set size, register data watcher entries |
| `protected abstract void b(ik var1)` | readEntityFromNBT |
| `protected abstract void a(ik var1)` | writeEntityToNBT |

---

## 13. Known Quirks / Bugs to Preserve

| # | Location | Quirk |
|---|---|---|
| 1 | `b()` move, step 6 | When `J == false` (web/wall clip): blocking on ANY axis zeroes ALL three velocity components simultaneously |
| 2 | `w()` entityBaseTick | `d = false` (firstUpdate) is set at the END of `w()`, not the start. On the very first call, `d` is `true` during the entire first tick |
| 3 | Constructor | `d(0,0,0)` is called before `b()` (entityInit); the AABB is therefore zero-size until the subclass calls `a(width, height)` inside `b()` |
| 4 | Fire ticks | NBT reads/writes fire as a `short` (line 839: `this.c = var1.d("Fire")`). Values that don't fit in a short are truncated |
| 5 | `c(ia)` distance | Uses MathHelper sqrt (float precision from double squared distance) — not the same as Math.sqrt |
| 6 | Mount offset | Rider's Y is `mount.t + mount.N * 0.75 + rider.L`. With `L=0` (default), rider stands at 75% of mount's height |
| 7 | DataWatcher flag write | `a(int bit, bool)` always reads the full byte, masks, and writes back — not an atomic operation |

---

## 14. Open Questions

1. **`ih` (EntityItem) spec needed** — specifically: `c = 10` (pickup delay), `dk` ItemStack
   field, and the `v()` (age/despawn) logic.

2. **`vi` (PlayerEntity) spec** — extends `ia` with many overrides. Methods called on
   `vi` from World: `ar()` (isSleeping), `aK()` (isPlayerFullyAsleep), `a(false,false,true)`
   (wakeUpPlayer).

3. **`pm` (DamageSource) class** — used in `a(pm, int)`. `pm.b` = fire damage source,
   `pm.c` = lava damage source. Static factory/singletons expected.

4. **`cr` (DataWatcher)** — synchronized data container. Methods: `a(int, byte/short/int/float/String)` = register; `b(int, value)` = set; `a(int)` = get byte; `b(int)` = get short. Spec pending.

5. **`dk` (ItemStack)** — `new dk(itemId, count, meta)`. Spec pending.

6. **Fields `al`, `am`, `an`** — three additional ints after the chunk tracking fields. Purpose not observed in base class. Possibly damage-related or rendering-related.

---

*Spec written by Analyst AI from `ia.java` (1214 lines). No C# implementation consulted.*
