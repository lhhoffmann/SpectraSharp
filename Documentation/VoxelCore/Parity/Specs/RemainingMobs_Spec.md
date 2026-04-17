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

# RemainingMobs Spec (Batch)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

Covers: EntitySlime (55), EntityGhast (56), EntityPigZombie (57), EntityEnderman (58),
EntityCaveSpider (59), EntitySilverfish (60), EntityBlaze (61), EntityMagmaCube (62),
EntitySquid (94), EntityWolf (95), EntityMooshroom (96), EntitySnowMan (97)

---

## 1. EntitySlime — `aed` — EntityList "Slime" ID 55

### 1.1 Class Hierarchy
`aed` extends `nq` (LivingEntity) implements `aey` (interface — no additional fields).

### 1.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW16 | `int` | 1 | Size of the slime (1=tiny, 2=small, 4=big) |
| `d` | `int` | varied | Jump timer; counts down from random value; on zero the slime jumps |

### 1.3 Constants

| Value | Meaning |
|---|---|
| `0.6F × size` | Hitbox width and height |
| `size²` | Max HP (size=1 → 1HP, size=2 → 4HP, size=4 → 16HP) |
| `2 + rand(3)` | Children spawned on death (range 2–4) |
| `size / 2` | Child slime size |
| `1` | Minimum size that drops items |
| `y < 40` | Vertical spawn constraint in slime chunks |

### 1.4 DataWatcher
- Slot 16 (`int`): slime size. Default 1.

### 1.5 Tick / AI Behaviour

**Jump AI:**
- A counter `d` is decremented each tick.
- When `d` reaches 0, the slime performs a jump: sets `w` (Y velocity) proportional to size,
  XZ velocity set toward target (or random if no target).
- Timer is reset to a random value dependent on size (bigger slimes wait longer).

**Attack:**
- Attacks player on contact.
- Damage dealt equals `size`.
- Size-1 slimes have `size = 0` damage → do not deal damage (tiny slimes are harmless).

**Visibility:**
- Size-1 slimes are not aggressive (attack strength 0). Size ≥ 2 will attack.

### 1.6 Death / Split Logic

On death, if `size > 1`:
1. Compute `childCount = 2 + rand(3)`.
2. Compute `childSize = size / 2`.
3. Spawn `childCount` instances of `aed` at the death position with `size = childSize`.
4. Each child is given random XZ velocity and a small upward Y velocity.

If `size == 1`: drop 0–2 slimeballs (`acy.ak`).

### 1.7 NBT

| Key | Type | Semantics |
|---|---|---|
| `"Size"` | `int` | Slime size. Read with `& 255` for legacy compatibility; written as-is. |

### 1.8 Spawn Conditions

- Spawns underground (Y < 40) only in **slime chunks** (chunk coordinates produce a specific
  RNG seed that yields `rand(10) == 0` — exact formula: `long seed = (world seed + chunkX²×4987142L + chunkX×5947611L + chunkZ²×4392871L + chunkZ×389711L) ^ 987234911L`; new Random(seed).nextInt(10) == 0).
- Also spawns naturally in swamp biomes at night on any Y level.
- Particle on jump: `"slime"`.

---

## 2. EntityGhast — `is` — EntityList "Ghast" ID 56

### 2.1 Class Hierarchy
`is` extends `wk` (abstract flying mob base, which extends `nq`).

### 2.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW16 | `int` | 0 | Is charging (1 = charging, 0 = not) |
| `f` | `int` | 0 | Attack cooldown / charge counter |

### 2.3 Constants

| Value | Meaning |
|---|---|
| `4.0F × 4.0F` | Hitbox (4 blocks wide, 4 blocks tall) |
| `10` | Max HP |
| `6` | Base attack damage (explosion) |
| `±16` | Random wander range (XYZ cube) |
| `10` | Charge-up ticks before firing |
| `20` | `f` value that triggers fireball shot |
| `-40` | `f` value after firing (40-tick cooldown until next charge cycle) |

### 2.4 DataWatcher
- Slot 16 (`int`): isCharging flag (0 or 1). Used for texture selection on client.

### 2.5 Tick / AI Behaviour

**Movement:**
- Wanders in a ±16 XYZ cube around current position.
- Line-of-sight check required before initiating attack.
- Fire-immune.

**Attack Cycle:**
1. When player is in range and LOS is clear: increment `f` each tick.
2. At `f == 10`: set DW16 = 1 (begin charging face texture).
3. At `f == 20`: spawn `aad` (EntityFireball) aimed at target. Set DW16 = 0. Set `f = -40`.
4. `f` increments from -40 toward 20 again before next shot (60-tick total cycle).

**Fireball parameters:**
- Acceleration vector `(b, c, d)` = normalized vector × 0.1 toward player.
- Explosion power 1.0F, incendiary.

### 2.6 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.aY` (Ghast Tear) | 0–1 | Always |
| `acy.O` (Gunpowder) | 0–2 | Always |

### 2.7 Spawn Conditions

- Nether only.
- Spawns in open air at Y 10–120.
- Completely bright (does not require any light level).

### 2.8 NBT

No unique NBT fields beyond inherited `LivingEntity` fields.

---

## 3. EntityPigZombie — `jm` — EntityList "PigZombie" ID 57

### 3.1 Class Hierarchy
`jm` extends `gr` (EntityZombie, which extends `zo` → `nq`).

### 3.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `b` | `int` | 0 | Anger timer; counts down to 0 → neutral again |
| `c` | `int` | 0 | Attack cooldown; set to `rand(40)` on each attack |

### 3.3 Constants

| Value | Meaning |
|---|---|
| `0.6F × 1.8F` | Inherited zombie hitbox |
| `20` | HP |
| `400 + rand(400)` | Anger duration (ticks) when first provoked |
| `32.0` | Group aggro radius |
| `rand(40)` | Per-attack cooldown reset |

### 3.4 Tick / AI Behaviour

**Neutral by default:**
- Does not attack players unless provoked.

**Provocation:**
- Triggered when player hits the entity (`a(pm source)` handler).
- Sets `b = 400 + rand(400)` on self.
- Calls all PigZombies within 32 blocks: each one also sets its own `b` timer (group aggro).

**Anger Countdown:**
- Each tick: if `b > 0`, decrement `b`.
- When `b == 0`, entity returns to neutral (stops targeting the player).

**Attack Cooldown:**
- After attacking, sets `c = rand(40)`. Will not attack again until `c == 0`.
- Each tick: if `c > 0`, decrement `c`.

**Equipment:**
- Holds gold sword (`acy.F` = gold sword) in main hand at spawn.

### 3.5 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.af` (Rotten Flesh) | 0–2 | Always |
| `acy.bb` (Gold Nugget) | 0–1 | Always |

Rare drop: `acy.F` (Gold Sword) with a small probability (standard rare equipment drop logic).

### 3.6 NBT

| Key | Type | Semantics |
|---|---|---|
| `"Anger"` | `short` | Current anger timer value |

---

## 4. EntityEnderman — `aii` — EntityList "Enderman" ID 58

### 4.1 Class Hierarchy
`aii` extends `zo` (hostile mob base → `nq`).

### 4.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW16 | `short` | 0 | Carried block ID (0 = none) |
| DW17 | `byte` | 0 | Carried block metadata |
| `a` | `boolean` | false | Is screaming (targeting player by stare) |

### 4.3 Constants

| Value | Meaning |
|---|---|
| `0.6F × 2.9F` | Hitbox width × height |
| `40` | Max HP |
| `7` | Attack damage |
| `1/20` | Probability of picking up a block each tick (when near a carryable block) |
| `1/2000` | Probability of placing carried block each tick |
| `1 - 0.025 / distance` | Dot product threshold for stare detection |
| `yy.ba` | Block ID = pumpkin (helmet bypass block) |
| `15` | Teleport attempts when hit by projectile |
| `64.0` | Teleport range (random XZ ± 64, Y ± 8) |

### 4.4 DataWatcher
- Slot 16 (`short`): carried block ID.
- Slot 17 (`byte`): carried block metadata.

### 4.5 Carryable Block IDs

The following 14 block IDs can be carried by Endermen:

| Block ID | Name |
|---|---|
| 2 | Grass |
| 3 | Dirt |
| 12 | Sand |
| 13 | Gravel |
| 37 | Yellow Flower |
| 38 | Red Rose |
| 39 | Brown Mushroom |
| 40 | Red Mushroom |
| 46 | TNT |
| 82 | Clay |
| 86 | Pumpkin |
| 103 | Melon |
| 110 | Mycelium |
| 111 | Lily Pad |

### 4.6 Tick / AI Behaviour

**Stare Detection:**
- Each tick, checks if the player is looking at the Enderman.
- Compute direction vector from player eye to Enderman center; normalize.
- Compute player look vector (yaw/pitch).
- If `dot(playerLook, directionToEnderman) > 1.0 - 0.025 / distance`, player is staring.
- Exception: if player wears pumpkin helmet (helmet item ID corresponds to carved pumpkin), bypass stare detection — Enderman does not become angry.
- If staring detected: set `a = true`, begin attacking player.

**Projectile Avoidance:**
- When hit by any projectile (`qq` source), attempts to teleport:
  - Try up to 15 random positions (`rand(-64..64)` XZ, `rand(-8..8)` Y relative).
  - On each attempt: if landing position has solid block below and 3 blocks of air above, teleport.
- Also teleports away from water on contact.

**Water Damage:**
- Each tick in rain or water: take 1 damage. Immediately attempts teleport.

**Block Carrying:**
- Each tick: if carrying no block and near a carryable block, 1/20 chance to pick it up (remove from world, store in DW16/17).
- If carrying a block, 1/2000 chance per tick to place it at a nearby position.

**Sunlight:**
- Does not burn in sunlight (unlike Zombies/Skeletons). Teleports away instead — checked in inherited AI.

### 4.7 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.aS` (Ender Pearl) | 0–1 | Always |

If carrying a block: block is dropped as item on death.

### 4.8 NBT

| Key | Type | Semantics |
|---|---|---|
| `"carried"` | `short` | Carried block ID |
| `"carriedData"` | `short` | Carried block metadata |

---

## 5. EntityCaveSpider — `aco` — EntityList "CaveSpider" ID 59

### 5.1 Class Hierarchy
`aco` extends `vq` (EntitySpider → `zo` → `nq`).

### 5.2 Fields

No unique fields beyond inherited Spider fields.

### 5.3 Constants

| Value | Meaning |
|---|---|
| `0.7F × 0.5F` | Hitbox width × height (smaller than normal spider 1.4×0.9) |
| `12` | Max HP |
| `0.7F` | Movement speed multiplier |
| `7` ticks | Poison duration on difficulty 2 (Easy) — **Note: Cave spider does NOT poison on Easy** |
| `140` ticks | Poison duration on difficulty 2 (Normal) = 7 seconds |
| `300` ticks | Poison duration on difficulty 3 (Hard) = 15 seconds |

### 5.4 Tick / AI Behaviour

Inherits all Spider behaviour (wall-climbing, target acquisition).

**Poison Attack:**
- On successful melee attack:
  - Difficulty 1 (Easy): no poison.
  - Difficulty 2 (Normal): apply Poison effect for 140 ticks (7 seconds).
  - Difficulty 3 (Hard): apply Poison effect for 300 ticks (15 seconds).
- Poison deals 1 damage every 25 ticks, cannot kill (minimum 1 HP).

### 5.5 Drops

Inherits Spider drops:
| Item | Amount | Condition |
|---|---|---|
| `acy.aL` (String) | 0–2 | Always |
| `acy.aS` (Spider Eye) | 0–1 | Rarely (1/3 chance from `zo` override) |

### 5.6 NBT

No unique NBT beyond inherited fields.

---

## 6. EntitySilverfish — `gl` — EntityList "Silverfish" ID 60

### 6.1 Class Hierarchy
`gl` extends `zo` (hostile mob base → `nq`). Creature type: `el.c` (arthropod).

### 6.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `b` | `int` | 20 | Group call timer — counts down; when 0, screams to nearby silverfish |

### 6.3 Constants

| Value | Meaning |
|---|---|
| `0.3F × 0.7F` | Hitbox width × height |
| `8` | Max HP |
| `1` | Attack damage |
| `8.0` | Detection range for player |
| `5.0` | Radius for group call alert |
| `20` | Group call timer reset value |
| `yy.bl` | Block ID = Monster Egg (infested stone) |

### 6.4 Tick / AI Behaviour

**Attack:**
- Attacks any player within 8 blocks.

**Group Call:**
- `b` timer decrements each tick when attacking.
- When `b == 0`: search all `gl` entities within 5 blocks; for each found silverfish that is
  not already in combat, alert it (set its target to the player).
- Reset `b = 20`.

**Hiding in Blocks:**
- When a silverfish takes damage: with some probability, if an adjacent `yy.bl` (infested stone)
  block exists, the silverfish can enter it (remove entity, record in block).
- Not a per-tick behaviour — occurs on damage event.

**Emerging from Blocks:**
- When a `yy.bl` block is broken, spawns a silverfish at the break position.

### 6.5 Drops

Nothing on death (no item drops).

### 6.6 Spawn Conditions

- Spawns from monster egg blocks when broken.
- Naturally spawns in Strongholds.

### 6.7 NBT

No unique NBT fields.

---

## 7. EntityBlaze — `qf` — EntityList "Blaze" ID 61

### 7.1 Class Hierarchy
`qf` extends `zo` (hostile mob base → `nq`).

### 7.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW16 | `byte` | 0 | Bit 0 = isOnFire (flame effect visible) |
| `d` | `int` | 0 | Phase counter for burst attack cycle |
| `e` | `int` | 0 | Tick counter within current phase |
| `Y` (Y oscillation) | `float` | 0 | Controls vertical movement phase |

### 7.3 Constants

| Value | Meaning |
|---|---|
| `0.6F × 1.8F` | Hitbox width × height |
| `20` | Max HP |
| `6` | Attack damage (melee) |
| `60` | Charge phase duration (d=1) |
| `6` | Shooting phase duration per shot (d=2,3,4) |
| `100` | Cooldown phase duration (d=5) |
| `yn` | SmallFireball class used for shots |
| `15` | Fully bright (light level emitted — always rendered at full brightness) |

### 7.4 DataWatcher
- Slot 16 (`byte`): flame effect flag (bit 0 = 1 means visually on fire).

### 7.5 Tick / AI Behaviour

**Y Oscillation:**
- Each tick the Blaze adjusts Y velocity to hover and bob vertically.
- `Y` counter increments by 0.0625F each tick. Vertical velocity updated from sin(Y).

**Burst Attack Cycle (`d` counter):**
- `d = 0`: Idle — when target is in range, set `d = 1`, `e = 0`.
- `d = 1`: Charge phase — set DW16 bit0 = 1 (flames on). Lasts 60 ticks.
  After 60 ticks: `d = 2, e = 0`.
- `d = 2`: Fire shot 1 — spawn `yn` (SmallFireball) toward target. `e` counts 6 ticks, then `d = 3`.
- `d = 3`: Fire shot 2 — same as d=2. After 6 ticks, `d = 4`.
- `d = 4`: Fire shot 3 — same as d=2. After 6 ticks, `d = 5`, DW16 bit0 = 0.
- `d = 5`: Cooldown — lasts 100 ticks. After 100 ticks: `d = 0`.

**Fire Immunity:**
- Blaze is immune to all fire and lava damage.
- Snowball deals 3 damage to Blaze (special override in `aah.java`).

**Fully Bright:**
- `u_()` returns `true` — Blaze is always fully lit regardless of ambient light.

### 7.6 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.aG` (Blaze Rod) | 0–1 | Always |

### 7.7 Spawn Conditions

- Nether Fortress only (spawner-based).
- Fully bright — no light-level restriction.

### 7.8 NBT

No unique NBT fields.

---

## 8. EntityMagmaCube — `aea` — EntityList "LavaSlime" ID 62

### 8.1 Class Hierarchy
`aea` extends `aed` (EntitySlime). Overrides several Slime methods.

### 8.2 Constants (overrides / additions)

| Value | Meaning |
|---|---|
| Fire-immune | `true` (lava and fire do no damage) |
| `"flame"` | Jump particle instead of `"slime"` |
| `size + 2` | Attack damage (vs. Slime which uses `size`) |
| `4 ×` | Jump timer multiplier — waits ~4× longer between jumps |
| `0.42F + size × 0.1F` | Jump Y velocity (slightly higher than base slime) |

### 8.3 Tick / AI Behaviour

Identical to EntitySlime except:
- Always attacks any target regardless of size (size-1 MagmaCubes deal 3 damage, unlike harmless Slimes).
- Jump particle is `"flame"` not `"slime"`.
- Fire immune.
- Jump timer 4× longer than Slime equivalent.

**Split on death:** same as Slime — size/2 children, 2+rand(3) count.
**Size-1 drops:** nothing (unlike Slime which drops slimeballs).

### 8.4 Drops

| Item | Amount | Condition |
|---|---|---|
| (none) | — | No drops from any size |

### 8.5 Spawn Conditions

- Nether only.
- Fully bright (light-level independent, same as Blaze).

### 8.6 NBT

Inherits Slime NBT: `"Size"` (`int`).

---

## 9. EntitySquid — `yv` — EntityList "Squid" ID 94

### 9.1 Class Hierarchy
`yv` extends `dn` (WaterCreature → `nq`). `dn` provides water-specific movement.

### 9.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `float` | 0 | Tentacle animation phase 1 |
| `b` | `float` | 0 | Tentacle animation phase 2 |
| `c` | `float` | 1.0F | Random thrust direction (target tentacle state) |
| `d` | `float` | 0 | Thrust decay counter |

### 9.3 Constants

| Value | Meaning |
|---|---|
| `0.95F × 0.95F` | Hitbox width × height |
| `10` | Max HP |
| `Y > 45` | Approximate spawn height (ocean depth) |

### 9.4 Tick / AI Behaviour

**Swim AI:**
- Squid swims toward a random target direction when `d` reaches 0.
- Picks a new random direction; sets velocity toward it.
- `d` resets to `10 + rand(20)` after each direction change.

**Out of water:**
- Flopping behaviour: applies gravity, XZ velocity randomised.
- Takes asphyxiation damage (standard `dn` logic).

**Silent:**
- No ambient sounds. No step sounds.

**Tentacle Animation:**
- `a` and `b` animate per tick for rendering. Purely visual, no gameplay effect.

### 9.5 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.aV` (Ink Sac) | 1–3 (+ looting bonus) | Always |

### 9.6 Spawn Conditions

- Spawns in ocean biomes, Y > 45.
- Requires water block.

### 9.7 NBT

No unique NBT fields.

---

## 10. EntityWolf — `aik` — EntityList "Wolf" ID 95

### 10.1 Class Hierarchy
`aik` extends `fx` (Animal → `nq`).

### 10.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| DW16 | `byte` | 0 | Bit flags: bit0=isSitting, bit1=isAngry, bit2=isTamed |
| DW17 | `String` | `""` | Owner name |
| DW18 | `int` | 0 | Current HP (for collar colour rendering in tamed state) |

### 10.3 Constants

| Value | Meaning |
|---|---|
| `0.8F × 0.8F` | Hitbox width × height |
| `8` | Max HP when untamed |
| `20` | Max HP when tamed |
| `1/3` | Taming success probability per bone use |
| `2` | Melee damage (untamed / sitting) |
| `4` | Melee damage (tamed and following) |
| `16.0` | Group aggro radius |
| `DW16 bit0 = 1` | isSitting |
| `DW16 bit1 = 1` | isAngry (neutral hostile) |
| `DW16 bit2 = 1` | isTamed |

### 10.4 DataWatcher
- Slot 16 (`byte`): bitfield (sitting / angry / tamed).
- Slot 17 (`String`): owner player name.
- Slot 18 (`int`): current health (used client-side for collar colour health indicator).

### 10.5 Tick / AI Behaviour

**Sitting:**
- When `bit0 (isSitting) = 1`, wolf stays in place and does not follow owner.
- Right-click by owner toggles sitting.

**Taming:**
- When untamed wolf is right-clicked with a bone (`acy.X`):
  - 1/3 probability: tame successfully. Set DW16 bit2=1, bit1=0. Set owner = player name.
    Increase max HP to 20, heal to 20. Spawn heart particles.
  - 2/3 probability: fail. Spawn smoke particles. Bone is consumed regardless.

**Following Owner:**
- When tamed and not sitting: follows the owner player.
- Target distance: approaches within 2 blocks, stops following if within 4 blocks.
- Teleports to owner if gap exceeds 12 blocks.

**Attack — Sheep:**
- Tamed wolves attack sheep autonomously.

**Attack — Group Aggro:**
- When a tamed wolf is attacked, alerts all wolves within 16 blocks belonging to same owner.

**Attack — Players:**
- Untamed wolf is neutral. Becomes aggressive (bit1=1) if hit.

### 10.6 Drops

Nothing on death.

### 10.7 NBT

| Key | Type | Semantics |
|---|---|---|
| `"Owner"` | `String` | Owner player name (empty string if untamed) |
| `"Sitting"` | `byte` | 1 if sitting |
| `"Angry"` | `byte` | 1 if angry |
| `"Tame"` | `byte` | 1 if tamed |

---

## 11. EntityMooshroom — `tb` — EntityList "MushroomCow" ID 96

### 11.1 Class Hierarchy
`tb` extends `adr` (EntityCow → `fx` → `nq`).

### 11.2 Constants

| Value | Meaning |
|---|---|
| `0.9F × 1.3F` | Hitbox width × height |
| `10` | Max HP (same as Cow) |
| `5` | Number of red mushroom items dropped on shearing |

### 11.3 Tick / AI Behaviour

Inherits all Cow behaviour (breed with wheat, baby growth, etc.).

**Milking with Bowl:**
- When player right-clicks Mooshroom with an empty bowl (`acy.ap`):
  - Replace bowl in inventory with Mushroom Stew (`acy.aq`).
  - No cooldown; can be done repeatedly.

**Shearing:**
- When player right-clicks with Shears (`acy.aH`):
  - Drop 5 `acy.av` (Red Mushroom) items at entity position.
  - Remove `tb` entity.
  - Spawn a standard `adr` (Cow) entity at the same position.
  - Consume shears durability (1 point).

### 11.4 Drops

Inherits Cow drops:
| Item | Amount | Condition |
|---|---|---|
| `acy.af` (Leather) | 0–2 | Always |
| `acy.aj` (Raw Beef) | 1–3 | Always |
| `acy.ab` (Steak) | 1–3 | If on fire at death |

### 11.5 Spawn Conditions

- Spawns in Mushroom Island biome only.

### 11.6 NBT

No unique NBT fields beyond inherited Cow/Animal fields.

---

## 12. EntitySnowMan — `ahd` — EntityList "SnowMan" ID 97

### 12.1 Class Hierarchy
`ahd` extends `aeo` which extends `ww` (Golem base → `nq`). NOT hostile (`zo`) — it is a
constructed golem.

### 12.2 Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| (none unique) | — | — | All fields inherited |

### 12.3 Constants

| Value | Meaning |
|---|---|
| `0.4F × 1.8F` | Hitbox width × height |
| `4` | Max HP |
| `10.0` | Snowball throw range (targets mobs within 10 blocks) |
| `1` | Damage from rain per tick |
| `1` | Damage from warm biome per tick (biome temperature > 1.0F) |
| `1.0F` | Temperature threshold above which Golem takes melt damage |
| `yy.aS` | Snow block ID (placed as snow trail) |
| `0–15` | Snowballs dropped on death (`acy.aC`) |

### 12.4 Tick / AI Behaviour

**Target Selection:**
- Each tick, searches for `zo` (hostile mob) instances within 10 blocks.
- Selects nearest as throw target.

**Snowball Throwing:**
- When target is selected, throws `aah` (Snowball) toward target.
- Throw rate governed by cooldown (approximately once every 20 ticks).
- Snowball spawned at eye height with direction vector toward target plus slight upward arc.

**Snow Trail:**
- Each tick, checks if the block at the SnowGolem's feet position is below temperature threshold.
- If biome temperature ≤ 1.0F and block below is solid: place `yy.aS` (snow layer) at feet.
- Checks 4 adjacent blocks as well (exact pattern: current + 4 cardinals).

**Rain Damage:**
- If `pm.b` (isRaining) is true: take 1 damage per tick.

**Warm Biome Damage:**
- Each tick: query biome temperature at current position.
- If temperature > 1.0F: take 1 damage per tick.
- This kills SnowGolems in Desert, Savanna, Nether, etc.

### 12.5 Drops

| Item | Amount | Condition |
|---|---|---|
| `acy.aC` (Snowball) | 0–15 | Always |

### 12.6 Construction (crafting)

Not a spawn egg entity. Created by player placing Snow Blocks + Pumpkin in the pattern:
```
[Pumpkin]
[Snow Block]
[Snow Block]
```
(vertical column, pumpkin on top). When pumpkin is placed last, the `ahd` entity spawns.

### 12.7 Spawn Conditions

- Created by player construction only (no natural spawning).

### 12.8 NBT

No unique NBT fields.

---

## 13. Open Questions

| # | Question |
|---|---|
| 13.1 | `aed` Slime — exact jump timer formula (what values does `d` reset to per size?). Observed: bigger slimes wait longer, but exact multiplier not confirmed. |
| 13.2 | `is` Ghast — exact wander AI (does it have a max wander radius or is it truly unlimited in the Nether?). |
| 13.3 | `jm` PigZombie — exact group aggro range confirmed as 32 blocks? Verify in wk.java pathfinding code. |
| 13.4 | `aii` Enderman — `1/20` pickup and `1/2000` place probabilities need confirmation (exact RNG call structure). |
| 13.5 | `qf` Blaze — SmallFireball spawn offset (spawns at body center? eye height?). |
| 13.6 | `aik` Wolf — tamed collar colour: does DW18 drive colour selection directly or is it a packed RGB? |
| 13.7 | `ahd` SnowGolem — snow trail: exact 4-adjacent pattern confirmed? Or just current feet position? |
