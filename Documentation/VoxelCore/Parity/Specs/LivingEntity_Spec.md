# LivingEntity Spec
Source class: `nq.java`
Type: `abstract class`
Superclass: `ia` (= Entity)

---

## 1. Purpose

`LivingEntity` (`nq`) is the abstract base for all entities that have health, can be damaged,
play sounds, run basic AI, and participate in combat. It extends `Entity` (`ia`) with a health
system, invulnerability window, fall-damage sound, potion effects, knockback, and a movement
method that applies proper per-surface friction.

---

## 2. Fields

All fields from `ia` (Entity) are inherited. Additional fields:

### Health & Combat

| Field | Type | Default | Semantics |
|---|---|---|---|
| `aM` | `int` (protected) | `f_()` | Current health. Initialised from abstract `f_()` in constructor. |
| `aN` | `int` (public) | — | Health value before the most recent damage (used for damage flash). |
| `aO` | `int` (protected) | `0` | Armor absorption remainder accumulator for multi-hit calculations. |
| `aP` | `int` (public) | `0` | HurtTime countdown. Set to `aq` (default 20) on hit; counts down each tick. |
| `aQ` | `int` (public) | `10` | HurtDuration — the value written to `aP`/`aQ` simultaneously on hit (= 10). |
| `aq` | `int` (public) | `20` | Invulnerability period in ticks. Within `ac > aq/2`, repeated hits are filtered by damage amount. |
| `aR` | `float` (public) | `0.0F` | Knockback angle (degrees). Set on hit to atan2 of attacker direction. |
| `aS` | `int` (public) | `0` | DeathTime counter. Increments each tick when health ≤ 0. Entity is removed at `aS == 20`. |
| `aT` | `int` (public) | `0` | Attack cooldown counter. |
| `aW` | `boolean` (protected) | `false` | Set `true` in `a(pm)` (onDeath) — marks that death callback has fired. |
| `bp` | `int` (protected) | `0` | Last damage amount received (for multi-hit invulnerability comparison). |
| `bq` | `int` (protected) | `0` | Ticks since last valid proximity to player (for natural despawn check). |
| `aX` | `int` (protected) | — | XP amount dropped on death. Returned by `b(vi player)`. |
| `aF` | `int` (protected) | `0` | Looting bonus level to pass to the killer entity's loot-bonus. |
| `aY` | `int` (public) | `-1` | Arrow count embedded in entity (for visual). |

### Movement / Physics

| Field | Type | Default | Semantics |
|---|---|---|---|
| `aI` | `float` (public) | `0.1F` | Ground movement speed multiplier. |
| `aJ` | `float` (public) | `0.02F` | Air movement speed multiplier. |
| `bv` | `float` (protected) | `0.0F` | Look pitch for AI wandering (random heading). |
| `bw` | `float` (protected) | `0.7F` | Entity width factor used in pushback overlap calculation. |
| `br` | `float` (protected) | `0.0F` | AI movement input: forward. |
| `bs` | `float` (protected) | `0.0F` | AI movement input: strafe. |
| `bt` | `float` (protected) | `0.0F` | AI turn rate. |
| `bu` | `boolean` (protected) | `false` | Wants-to-jump flag (set by AI). |
| `d` | `int` (private) | `0` | Jump cooldown (10 ticks between jumps when on ground). |

### Animation / Rendering

| Field | Type | Default | Semantics |
|---|---|---|---|
| `ar` | `float` (public) | `random * 12398.0F` | Walking animation position (frame counter, randomised at spawn). |
| `as` | `float` (public) | `(random + 1.0) * 0.01F` | Animation speed multiplier (randomised). |
| `at` | `float` (public) | `0.0F` | Body yaw (rendered torso heading, lags behind `y`). |
| `au` | `float` (public) | `0.0F` | Previous body yaw. |
| `av` | `float` (protected) | `0.0F` | Previous limb swing amount. |
| `aw` | `float` (protected) | `0.0F` | Limb swing amount. |
| `ax` | `float` (protected) | `0.0F` | Accumulated limb distance (drives walking cycle angle). |
| `ay` | `float` (protected) | `0.0F` | Previous `ax`. |
| `aK` | `float` (public) | `0.0F` | Previous limb distance (for interpolation). |
| `aL` | `float` (public) | `0.0F` | Current limb distance. |
| `ba` | `float` (public) | `0.0F` | Previous swing progress. |
| `bb` | `float` (public) | `0.0F` | Swing progress (arm swing towards target). |
| `bc` | `float` (public) | `0.0F` | Limb rotation counter (accumulated swing). |
| `aZ` | `float` (public) | `random * 0.9F + 0.1F` | Random scale for child/baby mob rendering. |
| `aU` | `float` (public) | `0.0F` | Previous health for health bar rendering. |
| `aV` | `float` (public) | `0.0F` | Current health for health bar rendering. |

### AI / Targeting

| Field | Type | Default | Semantics |
|---|---|---|---|
| `aA` | `String` (protected) | `"/mob/char.png"` | Texture path. |
| `aH` | `boolean` (public) | `false` | NoAI flag — when true, `n()` (base AI) is skipped. |
| `az` | `boolean` (protected) | `true` | hasAI flag (internal). |
| `aE` | `float` (protected) | `1.0F` | Animation speed factor. |
| `bd` | `vi` (protected) | `null` | Last player to attack this entity (for loot tables). |
| `be` | `int` (protected) | `0` | Ticks since last player damage (counts down; `bd` is cleared when reaches 0). |
| `bf` | `int` (public) | `0` | Remaining fire resistance ticks. |
| `bg` | `int` (public) | `0` | Fire resistance render timer. |
| `e` | `ia` (private) | `null` | Gaze target entity (used by wander AI). |
| `bx` | `int` (protected) | `0` | How many ticks to keep gazing at `e`. |

### Potion Effects

| Field | Type | Default | Semantics |
|---|---|---|---|
| `bh` | `HashMap<Integer, s>` (protected) | new | Active potion effects: effectId → `s` (PotionEffect). |
| `b` | `boolean` (private) | `true` | Dirty flag for DataWatcher potion color sync. Set true when effects change. |
| `c` | `int` (private) | `0` | Packed RGB color for active potion effects, stored in DataWatcher index 8. |

### Client-Side Interpolation

| Field | Type | Default | Semantics |
|---|---|---|---|
| `bi` | `int` (protected) | `0` | Steps remaining for server-position interpolation. |
| `bj/bk/bl` | `double` (protected) | `0` | Target X/Y/Z from server packet. |
| `bm/bn` | `double` (protected) | `0` | Target yaw/pitch from server packet. |

---

## 3. Constructor

```
nq(World world)
    super(world)                           // Entity constructor
    aM = f_()                              // health = maxHealth (abstract)
    l = true                               // set Entity field 'l' = true (living flag)
    as = (random + 1.0) * 0.01F            // random animation speed
    d(s, t, u)                             // setPosition (recalculates AABB)
    ar = random * 12398.0F                 // random animation offset
    y = random * PI * 2.0F                 // random spawn yaw
    V = 0.5F                               // Entity step height = 0.5 blocks
```

---

## 4. DataWatcher Registration

`nq.b()` overrides Entity's `b()` method (DataWatcher setup):

```
ag.a(8, this.c)    // register index 8 as int — packed potion effect color (0 = no effects)
```

DataWatcher index 8 holds an int packed as `0xRRGGBB` (one byte per channel). 0 = no active
effects (no particle cloud rendered on client).

---

## 5. Abstract Method

### `f_()` → `int` — getMaxHealth
Must be implemented by every concrete subclass. Returns the entity's maximum health.

---

## 6. Health System

### `ag()` → `int`
Returns `aM` (current health).

### `h(int val)` — setHealth (direct assignment)
```
aM = val
if val > f_(): val = f_()   // NOTE: aM is already set; clamping only affects local var, not aM
```
> **Quirk:** the if-branch writes to the local `val`, not to `this.aM`. `aM` is therefore NOT
> clamped — callers can set health above maxHealth via `h(n)`. This appears to be a bug.

### `a_(int amount)` — heal
```
if aM > 0:
    aM += amount
    if aM > f_(): aM = f_()
    ac = aq / 2    // ac = Entity field for invulnerability counter
```

### `K()` → `boolean` — isAlive (override)
```
return !K && aM > 0
```

### `a(pm source, int amount)` → `boolean` — attackEntityFrom (damage)

Full logic:

```
if world.isClientSide (o.I): return false
bq = 0
if aM <= 0: return false
if source.isFireDamage() && hasEffect(Water Breathing): return false   // waterbreathing = fire immune? 
// (actually: source.k() = isFireDamage, a(abg.n) = has Water Breathing effect)

bb = 1.5F   // swing arm
handled = true

if ac > aq / 2:                         // inside invulnerability window
    if amount <= bp: return false       // weaker or equal hit rejected
    innerDamage = amount - bp           // only the delta above previous
    applyDamage(source, innerDamage)
    bp = amount
    handled = false                     // don't play full hit effects
else:
    bp = amount
    aN = aM
    ac = aq
    applyDamage(source, amount)
    aP = aQ = 10

aR = 0.0F
attacker = source.a()                   // get attacking entity
if attacker instanceof Player (vi):
    be = 60; bd = (vi)attacker
elif attacker instanceof throwable (aik) and aik.aG():
    be = 60; bd = null

if handled:
    world.sendPacket(entity, byte=2)    // hurt packet
    knockback(attacker)
    if aM <= 0:
        world.playSound(this, deathSound, volume, pitch)
        onDeath(source)
    else:
        world.playSound(this, hurtSound, volume, pitch)

if aM <= 0 and handled: onDeath(source)
return true
```

### `b(pm source, int amount)` (protected) — applyDamage (inner)
```
amount = c(source, amount)   // armor absorption
amount = d(source, amount)   // enchantment (Protection/Resistance) reduction
aM -= amount
```

### Armor absorption — `c(pm source, int amount)` → `int`
```
if source is not projectile (not source.d()):
    var3 = 25 - o_()           // o_() = getTotalArmorValue (default 0)
    accumulated = amount * var3 + aO
    i(amount)                  // hook for subclass (e.g. damage armor items)
    amount = accumulated / 25
    aO = accumulated % 25      // carry remainder forward
return amount
```

### Enchantment reduction — `d(pm source, int amount)` → `int`
```
if hasEffect(Resistance enchantment / abg.m):
    level = getEffect(abg.m).c() + 1   // amplifier + 1
    var3 = (level) * 5                 // 5% per level
    absorbed = 25 - var3               // effective factor out of 25
    accumulated = amount * absorbed + aO
    amount = accumulated / 25
    aO = accumulated % 25
return amount
```

### `a(pm source)` — onDeath
```
ia killer = source.a()
if aF >= 0 and killer != null:
    killer.b(this, aF)      // grant looting bonus
if killer != null:
    killer.a(this)          // notify killer of kill
aW = true                   // mark died
if !world.isClientSide:
    looting = (killer instanceof vi) ? EnchantmentHelper.lootingBonus(...) : 0
    if !isSpecial (not q_()):
        a(bd != null, looting)   // drop items (be > 0 means player caused death)
world.sendPacket(this, byte=3)   // death packet
```

### `a(boolean playerKill, int looting)` (protected) — dropItems
```
xp = k()            // k() = getXPValue (default 0; returns aX via subclass override)
if xp > 0:
    rolls = random.nextInt(3) + looting
    for i in [0, rolls): drop(xp, 1 each)
```
> `k()` returns 0 in base; mobs override it to return their XP value.

---

## 7. Knockback — `a(ia target, int damage, double dx, double dz)`

Applied to `this` entity after being hit by `target`:

```
ap = true                   // Entity field: just got knocked back
dist = sqrt(dx*dx + dz*dz)
strength = 0.4F
v /= 2.0; w /= 2.0; x /= 2.0
v -= dx / dist * strength
w += 0.4F
x -= dz / dist * strength
if w > 0.4F: w = 0.4F
```

---

## 8. Movement — `d(float fwd, float strafe)` — livingMove

Called each tick with AI-computed forward/strafe inputs. Replaces raw Entity physics.

### In water (`D()` = inWater):
```
a(fwd, strafe, 0.02F)    // apply input acceleration
b(v, w, x)               // sweep collision
v *= 0.8F; w *= 0.8F; x *= 0.8F
w -= 0.02
if onGround and canJumpStep:
    w = 0.3F             // climb step
```

### In lava (`F()` = inLava):
Identical to water path but same coefficients.

### On ground/air:
```
friction = 0.91F
if onGround (D field):
    friction = 0.54600006F
    blockBelow = world.getBlockId(floor(x), floor(AABB.minY) - 1, floor(z))
    if blockBelow > 0:
        friction = Block.ca[blockBelow] * 0.91F

speedFactor = 0.16277136F / (friction * friction * friction)
groundSpeed = aI * speedFactor        // if on ground
airSpeed = aJ                         // if in air
inputScale = onGround ? groundSpeed : airSpeed

a(fwd, strafe, inputScale)            // apply input acceleration
b(v, w, x)                           // sweep collision

// Second friction lookup (after move):
friction = 0.91F
if onGround:
    friction = 0.54600006F
    blockBelow = world.getBlockId(...)
    if blockBelow > 0:
        friction = Block.ca[blockBelow] * 0.91F

if climbing (ah()):
    clamp v to [-0.15, 0.15]
    clamp x to [-0.15, 0.15]
    Q = 0.0F
    if w < -0.15: w = -0.15
    if onGround and w < 0: w = 0.0

w -= 0.08        // gravity
w *= 0.98F
v *= friction
x *= friction
```

### Jump — `ak()`
```
w = 0.42F
if hasEffect(Jump Boost / abg.j):
    w += (amplifier + 1) * 0.1F
if sprinting (X()):
    yawRad = y * PI / 180
    v -= sin(yawRad) * 0.2F
    x += cos(yawRad) * 0.2F
ap = true   // mark jumped
```

Jump cooldown: `d` field counts down from 10. Jump fires only when `d == 0` on ground.

### Climbable detection — `ah()` → `boolean`
```
blockId = world.getBlockId(floor(x), floor(AABB.minY), floor(z))
return blockId == Block.ladder.bM    // ladder block check
```

---

## 9. Main Tick — `a()` (override)

Runs after `super.a()` (Entity tick). Key steps:

1. Fire resistance counters: `bf--`/`bg--` countdown.
2. Client interpolation step via `c()` — smoothly moves toward server-side position.
3. AI tick: if not AI-disabled, call `n()` (wandering AI). Otherwise zero AI inputs.
4. Jump: if `bu` and on ground with `d == 0`, call `ak()` and set `d = 10`.
5. `br *= 0.98F; bs *= 0.98F; bt *= 0.9F` — decay movement inputs.
6. Apply speed enchantment via `aw()` multiplier on `aI`.
7. Call `d(br, bs)` — full movement with friction.
8. Entity-push: scan nearby entities in expanded AABB and call `e(this)` on each.

### Base tick — `w()` (separate override, called from within `a()`)

1. `aK = aL` — prev limb.
2. `super.w()` — Entity base tick (fire, void, tick increment).
3. Ambient sound counter: `random.nextInt(1000) < a++` → play ambient sound, reset `a = -p_()`.
4. In water and on fire: `y()` (extinguish).
5. Regeneration: if underwater and has water breathing effect blocked, else restore air.
6. `aU = aV` — animation.
7. Countdown timers: `aT--`, `aP--`, `ac--`, `be--` (if `be > 0`, else `bd = null`).
8. `aS++` if dead (`aM <= 0`): at `aS == 20`, drop items and call `v()` (setDead).
9. Potion effects: `as()` — tick each effect, remove expired, sync DataWatcher index 8.
10. Limb animation: update `ay = ax`, `au = at`, `A = y`, `B = z`.

---

## 10. Wandering AI — `n()` (protected)

Called each tick when `!aH`. Default ("dumb") wandering behaviour:

```
bq++
al()                // check proximity despawn (kills if > 128 blocks from any player)

if random < 0.02:
    player = world.getNearestPlayer(this, 8.0)
    if player != null:
        e = player; bx = 10 + random.nextInt(20)
    else:
        bt = (random - 0.5) * 20     // random turn

if e != null:
    lookAt(e, 10.0F, am())          // am() = look speed = 40
    if bx-- <= 0 or e.isDead or distance(e) > 8²:
        e = null
else:
    if random < 0.05: bt = (random - 0.5) * 20
    y += bt
    z = bv

if inWater or inLava: bu = random < 0.8F   // want to jump
```

### Despawn check — `al()` (protected)
```
player = world.getNearestPlayer(this, -1.0)
if player != null:
    if d() and distance² > 16384 (128 blocks): v()   // immediate despawn
    if bq > 600 and random.nextInt(800) == 0 and distance² > 1024 (32 blocks) and d():
        v()                                           // random despawn
    else if distance² < 1024: bq = 0
```

`d()` → `true` in base — can despawn naturally. Player mobs override to `false`.

---

## 11. Potion Effect System

### `a(s effect)` — addPotionEffect
```
if b(effect) (isPotionApplicable):
    if bh.containsKey(effectId):
        bh.get(effectId).a(effect)   // merge (take longer duration / higher amplifier)
        d(bh.get(effectId))          // dirty flag
    else:
        bh.put(effectId, effect)
        c(effect)                    // dirty flag
```

### `b(s effect)` → `boolean` — isPotionApplicable
Returns `false` for Wither and Poison if entity type is UNDEAD (`r_() == el.b`).

### `as()` — tickPotionEffects
```
for each effect in bh:
    if !effect.a(this):              // tick effect; returns false when expired
        if !world.isClientSide:
            remove effect, call e(effect) → dirty
if dirty (b == true):
    if !bh.isEmpty:
        color = pk.a(bh.values())    // compute packed RGB from active effects
        ag.b(8, color)               // update DataWatcher index 8
    else:
        ag.b(8, 0)
    b = false
if random.nextBoolean() and DataWatcher[8] > 0:
    spawn "mobSpell" particle at random offset
```

---

## 12. Fall Damage — `c(float distance)` (override)
```
super.c(distance)
damage = ceil(distance - 3.0F)
if damage > 0:
    if damage > 4: playSound("damage.fallbig")
    else: playSound("damage.fallsmall")
    a(DamageSource.fall, damage)
    blockBelow = world.getBlockId(floor(x), floor(y - 0.2 - L), floor(z))
    if blockBelow > 0:
        sound = Block.k[blockBelow].bX   // StepSound
        playSound(sound.d(), sound.b() * 0.5F, sound.c() * 0.75F)
```

Fall damage threshold: 3.0 blocks. Formula: `ceil(height - 3)`. 4-block fall → 1 damage.

---

## 13. Death Sequence — `ad()` (protected)

Called when `aM <= 0` and `aS == 20`:

```
if !world.isClientSide and (be > 0 or ae()) and !q_():
    xp = b(bd)              // get XP for killing player; bd = last attacker
    while xp > 0:
        chunk = fk.b(xp)    // split XP into orb-size chunks
        xp -= chunk
        world.spawn(new fk(world, x, y, z, chunk))
ap()                        // apPhysicsDeath (no-op in base)
v()                         // setDead
spawn 20 "explode" particles
```

---

## 14. NBT Serialisation

Writes/reads: `"Health"` (short), `"HurtTime"` (short), `"DeathTime"` (short),
`"AttackTime"` (short), `"ActiveEffects"` (list of compound tags with `"Id"` byte,
`"Amplifier"` byte, `"Duration"` int).

If `"Health"` key absent on read: defaults health to `f_()` (max health).

---

## 15. Key Overrides / Virtual Surface

| Method | Returns | Semantics |
|---|---|---|
| `E()` | `N * 0.85F` | getEyeHeight — 85% of entity height |
| `I()` | `aA` | getTexturePath |
| `K()` | `!K && aM > 0` | isAlive |
| `H()` | `!K` | canBePushed |
| `f_()` | abstract | getMaxHealth — MUST implement |
| `k()` | `0` | getXPValue — override to return XP |
| `o_()` | `0` | getTotalArmorValue — override for armour entities |
| `q_()` | `false` | isSpecialMob (boss etc.) — no natural despawn |
| `r_()` | `el.a` | getCreatureAttribute — `el.a`=normal, `el.b`=undead, `el.c`=arthropod |
| `e()` (sound) | `null` | getAmbientSound — override to return string |
| `f()` (sound) | `"damage.hurtflesh"` | getHurtSound |
| `g()` (sound) | `"damage.hurtflesh"` | getDeathSound |
| `w_()` | `1.0F` | getSoundVolume |
| `s()` | `null` | getEquippedItem — return held ItemStack if applicable |
| `i()` | world-check | isSpawnable — no fluids, no block collision |
| `p_()` | `80` | getAmbientSoundInterval (ticks between ambient sounds) |

---

## 16. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | `h(int)` (setHealth direct) does NOT clamp `aM` — the clamping branch writes to the local variable only. Health can be set above max. |
| 2 | Invulnerability filter: damage `amount <= bp` is rejected only when `ac > aq/2`. The filter compares total damage, not delta — a second hit for exactly `bp` (same strength) is rejected, but `bp+1` applies only the delta `1`. |
| 3 | Armor absorption carries a remainder in `aO` across hits. This means fractional absorbed damage "accumulates" and the next hit loses more to armor. |
| 4 | The friction constant `0.54600006F` = `0.6F * 0.91F` computed at compile time. The second friction lookup after `b(v,w,x)` is a duplicate block lookup — same block is read twice per tick. |
| 5 | `aR` (knockback angle field on `this`) is set in `a(pm, int)` but `aR` on the TARGET entity (via the `a(ia, int, dx, dz)` method) is also set. The field on `this` (the entity being hit) is unrelated to the field set on the knockback recipient. |
| 6 | `as()` (potion tick) spawns particles only when `random.nextBoolean()` — 50% of ticks. This is the source of the flicker in the particle cloud. |
| 7 | DataWatcher index 8 is an `int` (not `byte`), despite being compared with 0 in client code. It holds a full RGB packed value. |

---

## 17. Open Questions

1. **`pm`** — DamageSource class. `pm.d` = fire, `pm.h` = fall, `pm.i` = lava, `pm.e` = drown,
   `pm.j` = generic. `pm.k()` = isFireDamage, `pm.d()` = isProjectile. Spec pending.
2. **`abg`** — Potion effect enum/registry. `abg.n` = Water Breathing, `abg.j` = Jump Boost,
   `abg.c` = Speed, `abg.d` = Slowness, `abg.m` = Resistance. Full list spec pending.
3. **`el`** — CreatureAttribute enum. `el.a`=NORMAL, `el.b`=UNDEAD, `el.c`=ARTHROPOD. Spec pending.
4. **`s`** — PotionEffect class. Has `a()` = effectId, `b()` = duration, `c()` = amplifier. Spec pending.
5. **`fk`** — ExperienceOrb entity. Constructor `fk(world, x, y, z, xpValue)`. Spec pending.
6. **`vi`** — Player class (extends `nq`). `vi.by` = inventory. Spec pending.
7. **`l` field on `ia`** — set to `true` in `nq` constructor. Semantics need confirmation from
   Entity spec (likely "isLiving" or "hasRider" — see Entity_Spec.md).

---

*Spec written by Analyst AI from `nq.java` (1257 lines). No C# implementation consulted.*
