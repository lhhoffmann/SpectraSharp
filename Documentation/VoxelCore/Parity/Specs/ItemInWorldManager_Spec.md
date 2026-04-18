# Spec: ItemInWorldManager (Block Breaking Progress)

**Java classes:** `aes` (abstract base), `dm` (Survival), `uq` (Creative)
**Status:** PROVIDED
**Canonical name:** ItemInWorldManager

---

## Class Hierarchy

```
aes   (abstract ItemInWorldManager base)
├─ dm   (Survival)
└─ uq   (Creative)
```

`Minecraft.c` holds the active instance. Type is checked via `c.i()` (isCreative).

---

## Abstract Base: `aes`

### Fields

| Field | Type | Purpose |
|---|---|---|
| `a` | `final Minecraft` | Game singleton |

### Key Methods

#### `a(int x, int y, int z, int face)` — actual block break

The shared break implementation called when break is confirmed:

```java
// 1. Fire world event (break particles + sound)
world.a(this.a.h, 2001, x, y, z, blockId + meta*256);

// 2. Remove block
world.b(x, y, z, 0);   // setBlockToAir

// 3. Call block drop logic
yy.k[blockId].e(world, x, y, z, meta);   // dropBlockAsItem with meta
```

#### `b(ry world)` — create EntityPlayerSP

```java
return new di(this.a, world, this.a.k, world.y.g);
```

Creates a new player entity when loading or respawning.

#### `d(vi player)` — post-load setup (called after world loaded)

#### `i()` — isCreative, returns false in `aes`, overridden in `uq`

#### `c()` — reach distance, returns 4.0F in base

---

## Survival: `dm`

### Fields

| Field | Type | Initial | Purpose |
|---|---|---|---|
| `c` | `int` | -1 | curBlockX (target block X, -1 = none) |
| `d` | `int` | -1 | curBlockY |
| `e` | `int` | -1 | curBlockZ |
| `f` | `float` | 0.0F | block damage accumulator (0.0–1.0) |
| `g` | `float` | 0.0F | prevDamage (for crack overlay interpolation) |
| `h` | `float` | 0.0F | sound tick counter |
| `i` | `int` | 0 | post-break cooldown (5 ticks) |

### `b(int x, int y, int z, int face)` — single click / instant-break path

Called on left-mouse-press (not hold). Checks if the block can be broken instantly:

```java
yy block = yy.k[world.a(x,y,z)];
if (block.a(player) >= 1.0F) {
    // instant-break: delegate to super.a(x,y,z,face)
    a(x, y, z, face);
}
// else: just start tracking; per-tick damage handled by c()
```

`block.a(player)` = player-specific mining speed for this block type.
Returns ≥ 1.0F when the player can break it instantly (e.g., correct tool, weak block).

### `c(int x, int y, int z, int face)` — per-tick damage (left-hold)

Called once per game tick while left mouse button is held:

```java
f += block.a(player);      // add mining speed fraction
h += block.a(player);      // sound accumulator

if (h >= 4.0F) {            // every 4 ticks: play crack sound
    h = 0.0F;
    world.a("dig." + block.material, x + 0.5, y + 0.5, z + 0.5, 1.0F, 1.0F);
}

if (f >= 1.0F) {            // break threshold reached
    a(x, y, z, face);       // break the block
    b();                     // reset state
}
```

**There is no division by hardness here** — `block.a(player)` already incorporates
hardness and tool efficiency and returns the fraction of block to break per tick.

At f values 0.0 → 1.0, the crack overlay is updated by `a(float partialTick)`.

### `b()` — reset

```java
c = d = e = -1;
f = g = h = 0.0F;
i = 0;
```

Called when: mouse released, player moves too far, or block changes.

### `c()` — reach distance

Returns `4.0F`.

### `a(float partialTick)` — update crack overlay

```java
if (c != -1) {
    float crackStage = g + (f - g) * partialTick;   // interpolate
    // stage 0-9 = floor(crackStage * 10)
    afv.damagePartialTime = crackStage;  // RenderGlobal field for crack rendering
}
```

The crack overlay has 10 visual stages (0-9). Stage is `floor(f * 10)`.

### Reset conditions

- Player moves more than `c()` (4.0F) blocks from the target block
- Block ID at target position changes (checked each tick)
- Left-mouse released

---

## Creative: `uq`

### `i()` — isCreative

Returns `true`.

### `b(vi player)` static — set Creative abilities

```java
player.cc.c = true;   // invulnerable
player.cc.d = true;   // mayfly
player.cc.a = true;   // instabuild (instantBreak)
```

### `c(vi player)` static — clear abilities (switch to Survival)

Clears all three flags.

### Behaviour differences

- In Creative mode, `b(x,y,z,face)` immediately breaks the block (instabuild).
- No damage accumulation.
- `c.a()` in `Minecraft.k()` checks `c instanceof uq` to skip damage UI.

---

## Mining Speed Formula

`block.a(player)` encapsulates the full formula:

```
miningSpeed = base_efficiency_for_tool / hardness / 30
```

- Base efficiency: 1.0 for bare hands on stone, higher for matching tools.
- Hardness: `yy.bN` field on the block.
- Division by 30 converts the mining speed to a per-tick fraction (20 ticks/sec × 1.5).

The caller (`dm.c`) simply accumulates `f += block.a(player)` each tick until f ≥ 1.0.

---

## C# Mapping

| Java | C# |
|---|---|
| `aes` | `ItemInWorldManager` (abstract base) |
| `dm` | `SurvivalItemInWorldManager` |
| `uq` | `CreativeItemInWorldManager` |
| `dm.f` | `blockDamage` (0.0–1.0) |
| `dm.g` | `prevBlockDamage` |
| `dm.c/d/e` | `curBlockX/Y/Z` |
| `dm.i` | `breakCooldown` (5 ticks) |
| `a(x,y,z,face)` | `BreakBlock(x,y,z,face)` |
| `b(x,y,z,face)` | `OnPlayerDamageBlock(...)` |
| `c(x,y,z,face)` | `BlockDamageProgressTick(...)` |
| `afv.damagePartialTime` | `RenderGlobal.damagePartialTime` |
