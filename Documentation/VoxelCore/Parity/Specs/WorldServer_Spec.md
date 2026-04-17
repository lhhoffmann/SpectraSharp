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

# WorldServer / WorldInfo Spec (Game Time, Weather, Auto-Save, Spawn)
**Source classes:** `ry.java` (World — server-side subclass), `si.java` (WorldInfo)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Map

| Class | Role |
|---|---|
| `ry` | World — the main simulation class (implements `kq`); handles tick logic |
| `si` | WorldInfo — persistent world state (time, spawn, weather, seed, game mode) |
| `dh` | BlockPos / SpawnPoint — triplet `(a, b, c)` of int x/y/z |

---

## 2. `si` — WorldInfo Fields

| Field | Type | NBT key | Accessor | Meaning |
|---|---|---|---|---|
| `a` | `long` | `RandomSeed` | `b()` | World seed |
| `b` | `int` | `SpawnX` | `c()` / `a(int)` | Spawn X |
| `c` | `int` | `SpawnY` | `d()` / `b(int)` | Spawn Y |
| `d` | `int` | `SpawnZ` | `e()` / `c(int)` | Spawn Z |
| `e` | `long` | `Time` | `f()` / `a(long)` | World time (ticks since creation) |
| `f` | `long` | `LastPlayed` | `l()` | Timestamp of last play session (ms) |
| `g` | `long` | `SizeOnDisk` | `g()` | On-disk world size in bytes |
| `h` | `ik` | `Player` | `h()` | Saved player NBT |
| `i` | `int` | `Player.Dimension` | `i()` | Player's current dimension |
| `j` | `String` | `LevelName` | `j()` / `a(String)` | World name |
| `k` | `int` | `version` | `k()` / `d(int)` | Save version |
| `l` | `boolean` | `raining` | `o()` / `b(boolean)` | Is it raining? |
| `m` | `int` | `rainTime` | `p()` / `f(int)` | Ticks until next rain toggle |
| `n` | `boolean` | `thundering` | `m()` / `a(boolean)` | Is it thundering? |
| `o` | `int` | `thunderTime` | `n()` / `e(int)` | Ticks until next thunder toggle |
| `p` | `int` | `GameType` | `q()` | Game mode (0=Survival, 1=Creative) |
| `q` | `boolean` | `MapFeatures` | `r()` | Generate structures? |
| `r` | `boolean` | `hardcore` | `s()` | Hardcore mode? |

---

## 3. `ry` — World Tick Loop Fields

| Field | Type | Meaning |
|---|---|---|
| `C` | `si` | WorldInfo instance |
| `u` | `int` | Auto-save interval (ticks); default `40` |
| `o` | `float` | Current rain strength (0.0–1.0, lerped) |
| `n` | `float` | Previous rain strength |
| `q` | `float` | Current thunder strength (0.0–1.0, lerped) |
| `p` | `float` | Previous thunder strength |
| `r` | `int` | Thunder "recharge" counter (ticks before next bolt allowed) |
| `l` | `int` | LCG state for per-chunk random (rain/ice/snow/tile) |
| `m` | `int` | LCG multiplier constant `1013904223` |
| `U` | `int` | Cave sound timer (random int 12000 on init) |

---

## 4. Game Time

### 4.1 World Time Increment

`ry.c()` is the main server tick method. Inside, each tick:

```java
long newTime = this.C.f() + 1L;   // C.f() = WorldInfo.Time getter
this.C.a(newTime);                  // C.a(long) = WorldInfo.Time setter
```

World time is a `long`, incremented by 1 every tick (20 Hz).

### 4.2 Day / Night Cycle

A full day is **24000 ticks** (20 min real-time at 20 Hz).

- Time 0 = dawn (sunrise).
- Time 6000 = noon.
- Time 12000 = dusk (sunset).
- Time 18000 = midnight.
- Time 24000 = next dawn.

When sleep skips to day (`/sleep` in Creative or beds in survival):

```java
long skipTo = this.C.f() + 24000L;
this.C.a(skipTo - skipTo % 24000L);  // round up to next dawn
```

### 4.3 Moon Phase

```java
moonPhase = (int)((worldTime / 24000L) % 8L);
```

Phase 0 = full moon; 4 = new moon. Used by mob spawn rates and slime spawning.

---

## 5. Auto-Save

### 5.1 Save Interval

The auto-save fires when `worldTime % u == 0`, where `u = 40` by default.

At 20 ticks/second, `u = 40` means **every 2 seconds**.

> Note: The value `u = 40` is the save *check* interval, not the save itself — the actual world save calls `ry.a(false, null)` which delegates to the save handler.

### 5.2 Save Method — `ry.a(boolean flush, IProgressUpdate callback)`

Saves all dirty chunks and WorldInfo NBT via `B.b()` (the `nh` save handler).

---

## 6. Weather System

### 6.1 Rain Toggle — `ry.h()`

Called every tick (inside `ry.c()`). Uses `WorldInfo.rainTime` (`si.m`) and `WorldInfo.raining` (`si.l`) as a countdown timer + state flag:

```
if not hardcore:
    decrement si.m (rainTime) by 1
    if si.m reaches 0:
        toggle si.l (raining)
        if now raining:    si.m = rand.nextInt(12000) + 3600    // 3–9 min of rain
        if now not raining: si.m = rand.nextInt(168000) + 12000  // 10–150 min of clear
```

### 6.2 Rain Strength Interpolation

Every tick, the rain render strength `o` is lerped toward the target state:

```java
if (raining) o += 0.01F;
else         o -= 0.01F;
o = clamp(o, 0.0F, 1.0F);
```

Rain fully fades in/out over 100 ticks (5 seconds).

### 6.3 Thunder Toggle

Same pattern as rain but uses `si.n` (thundering) and `si.o` (thunderTime):

```
if si.o reaches 0:
    toggle si.n (thundering)
    if now thundering:     si.o = rand.nextInt(12000) + 12000   // 10–20 min of thunder
    if now not thundering: si.o = rand.nextInt(168000) + 12000  // 10–150 min of calm
```

Thunder is only visible/audible if also raining (`C.o()` AND `C.m()`).

### 6.4 Thunder Strike (Lightning)

Per tick, when `rand.nextInt(100000) == 0 AND raining AND thundering`:

```java
x = playerX + (lcg >> 2) & 15
z = playerZ + (lcg >> 8) & 15
y = getTopSolidOrLiquidBlock(x, z)
if (canLightningStrikeAt(x, y, z)):
    spawn EntityLightningBolt(world, x, y, z)
    r = 2  // recharge: disallow lightning for 2 ticks
```

### 6.5 Ice and Snow Formation — Per-Chunk Tick

Each loaded chunk per tick, one random XZ position is chosen via LCG.
At the surface height:
- `q(x, y-1, z)` — is this a water block that should freeze? → place ice (`yy.aT`).
- `r(x, y, z)` — is this exposed to sky + cold biome? → place snow layer (`yy.aS`).

---

## 7. Spawn Point Generation — `ry.i()`

Called once when world is new (`WorldInfo == null` at load time).

```
1. Get valid biome positions via WorldChunkManager.findBiomePosition(0, 0, 256, validBiomes, rand)
2. Walk random XZ offsets (up to 1000 tries) until:
       WorldProvider.canCoordinateBeSpawn(x, z) returns true
       (checks: block at x, y, z is solid/grass, not ocean, not ice)
3. Set WorldInfo spawn point: (x, worldHeight/2, z)
4. Find actual safe Y: e() method adjusts spawn Y until non-solid block found above
```

`worldHeight / 2` as initial Y = 64 in a standard 128-block-tall world.

---

## 8. Mob Spawner Tick

`ry.c()` calls `we.a(world, hostile, passive)`:
- `hostile = F` (hostiles enabled flag, default `true`).
- `passive = G && C.f() % 400L == 0L` (passive spawning every 400 ticks = 20 seconds).

---

## 9. Sky Light Level — `ry.a(float partialTick)`

Returns the current sky light multiplier based on time of day and rain:

```java
angle = getSunAngle(partialTick)
skyLight = sin(angle) * 0.5 + 0.5
skyLight = skyLight * 0.8 + 0.2
skyLight = lerp(skyLight, 1.0, rainStrength * 5)
return skyLight
```

Rain darkens sky by blending toward lower values.

---

## 10. `WorldInfo` — NBT Format

```
Level:
  RandomSeed: long
  SpawnX: int
  SpawnY: int
  SpawnZ: int
  Time: long
  LastPlayed: long
  SizeOnDisk: long
  LevelName: string
  version: int
  rainTime: int
  raining: byte (bool)
  thunderTime: int
  thundering: byte (bool)
  GameType: int
  MapFeatures: byte (bool)
  hardcore: byte (bool)
  Player: compound (optional — only if single-player)
```

---

## 11. Open Questions

| # | Question |
|---|---|
| 11.1 | What is `ry.F` (hostile spawn flag)? Set at construction? Always `true` for normal worlds? |
| 11.2 | `ry.u = 40` — is this really the save interval in ticks? Seems very frequent (every 2s). Confirm. |
| 11.3 | Moon phase `(worldTime / 24000) % 8` — which `ry` field or accessor exposes this? Is it computed inline? |
| 11.4 | Cave sound: `U = rand.nextInt(12000)` on start; resets to `rand.nextInt(12000) + 6000` after playing — what method plays the cave sound? |
| 11.5 | `ry.D` (boolean) — appears to be a "world initialising" flag during spawn search. Confirm it blocks entity spawning while `true`. |
