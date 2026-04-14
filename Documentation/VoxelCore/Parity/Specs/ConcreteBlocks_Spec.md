# ConcreteBlocks Spec
Source: `jb.java` (GrassBlock), `cj.java` (Sand/Gravel), `qo.java` (Leaves), `aip.java` (Log)
Type: Block subclass behaviour reference

---

## 1. GrassBlock (`jb`) — Block ID 2

### Registration
```java
new jb(2).c(0.6F).a(p.e).a("grass")
```
No explicit texture index; all faces resolved by overriding `b(int face)`.

### Multi-Face Textures

| Face ID | Condition | Texture index | Notes |
|---|---|---|---|
| 1 (top) | always | **0** | Grass top — stored gray, needs biome tint |
| 0 (bottom) | always | **2** | Dirt |
| 2–5 (sides) | default | **3** | Grass side — upper strip needs biome tint |
| 2–5 (sides) | snow above | **68** | Snow-covered grass side |

Snow condition check: the block at (x, y+1, z) has material `p.u` or `p.v` (snow materials).
Detected via `world.e(x, y+1, z)` (getMaterial) — if it equals snow material, use texture 68.

### Biome Color Tinting
- Grass top (texture 0): multiply by biome grass color.
- Grass side upper strip (texture 3): multiply top ~4 pixel rows only.
- Reference value (plains biome): `(72, 181, 24)`.
- See TerrainAtlas spec §3 for multiplication formula.

### Tick Behaviour (`a(ry, x, y, z, Random)`)
Spread tick: if light level above block ≥ 9, tries to spread grass to adjacent dirt blocks
within a 3×5×3 volume. Target block must be dirt (ID 3) with light above ≥ 4 and no opaque block on top.
Converts target dirt to grass: `world.d(tx, ty, tz, 2)`.

If light above this block < 7 and block above is opaque (`o[above] >= 255`), convert self back to dirt:
`world.d(x, y, z, 3)`.

---

## 2. SandBlock (`cj`) and GravelBlock (`kb`) — Block IDs 12, 13

### SandBlock Registration
```java
new cj(12, 18).c(0.5F).a(j).a("sand")
```
Texture 18 = sand. Material `p.o` (sand material). StepSound = `j` (aeg/sand).

### GravelBlock Registration
```java
new kb(13, 19).c(0.6F).a(d).a("gravel")
```
Texture 19 = gravel. `kb` extends `cj` — identical gravity behaviour, different drops.

### Gravity Fall Logic (`a(ry, x, y, z, Random)`)

On random tick:
1. Check if block below (y-1) can be fallen into: block ID must be 0 (air), 8/9 (water), 10/11 (lava), or 51 (fire).
2. If yes and `!yy.a` (static flag false, server-side default):
   - Spawn `uo` (EntityFallingSand) at center of block: `x+0.5, y, z+0.5`.
   - Remove the block: `world.d(x, y, z, 0)`.
3. If `yy.a` (static flag true, used during world generation):
   - Scan downward from y-1 until hitting a non-replaceable block or y=0.
   - Place block at the lowest air/liquid position found.

**Static flag `a`:**
```java
public static boolean a = false;
```
`a=false` = normal runtime (spawn entity).
`a=true` = world-gen mode (instant teleport).

**Tick delay:** blocks are registered with `l()` — tickable. The actual tick scheduling is via
`world` random-tick system (every ~`1/tickRate` chance per block per tick). No explicit delay
value beyond the standard random-tick rate.

### GravelBlock drops (`a(Random)`)
`kb` overrides to drop flint (item ID 318) with 1/10 chance; otherwise drops gravel (ID 13).

---

## 3. BlockLeaves (`qo`) — Block ID 18

### Registration
```java
new qo(18, 52).c(0.2F).h(1).a(p.e).a("leaves")
```
Texture 52 = leaves (stored gray-green, needs foliage biome tint). LightOpacity = 1.

### Texture / Biome Color

`qo` overrides `c(int meta)` (getRenderColor) to return biome foliage color:
- `meta & 3 == 0` → oak leaves → `db.b()` foliage color (oak)
- `meta & 3 == 2` → birch leaves → `db.a()` foliage color (birch, slightly blue-green)
- `meta & 3 == 1` → spruce leaves → `db.c()` foliage color (spruce, very dark green)

Placeholder foliage colors (no biome system):
- Oak: `(78, 164, 0)` — default plains foliage
- Birch: `(128, 167, 85)` — lighter green
- Spruce: `(97, 153, 97)` — dark green

### Metadata Bit Layout

| Bit | Mask | Meaning |
|---|---|---|
| 0–1 | `meta & 3` | Wood type: 0=oak, 1=spruce, 2=birch, 3=jungle |
| 2 | `meta & 4` | No-decay flag: set when placed by player, never decays |
| 3 | `meta & 8` | Needs-check flag: must re-check decay distance next tick |

### Decay Algorithm (BFS — runs on random tick)

Triggered when `(meta & 8) != 0` (needs-check bit set) and `(meta & 4) == 0` (not player-placed).

Decay check:
1. Build a `32×32×32` cache array centred at the leaves block.
2. BFS from all connected logs within the 32³ volume, marking each leaves block with distance 1–4.
3. If this leaves block is not marked within distance 4 from a log, remove it: `world.d(x, y, z, 0)`.
4. If marked, clear the needs-check bit: `world.d(x, y, z, meta & ~8)`.

Decay propagation: adjacent leaves blocks that are not player-placed and don't have needs-check set
get the needs-check bit added: `world.d(nx, ny, nz, adjacentMeta | 8)`.

### Drops
- Default: **no drop** (leaves disappear silently).
- 1 in 20 chance: drops **sapling** (item determined by `meta & 3`).
  - 0=oak sapling (ID 6, damage 0), 1=spruce (damage 1), 2=birch (damage 2).
- With Shears: drops the leaves block itself (ID 18, preserving `meta & 3`).

### Rendering
- Semi-transparent (`α ≈ 151`); use cutout mode (discard α < 128).
- Also requires biome foliage color multiplication (see TerrainAtlas spec §3).
- `isOpaqueCube()` returns false — allows sky light through.

---

## 4. LogBlock (`aip`) — Block ID 17

### Registration
```java
new aip(17).c(2.0F).a(p.d).a("log").l()
```
No texture index in constructor — all faces resolved by multi-face override.

### Multi-Face Textures

`a(int face, int meta)` override:

| Face | Texture index | Notes |
|---|---|---|
| 0 (bottom) | **21** | Log end (circular cross-section) |
| 1 (top) | **21** | Log end |
| 2–5 (sides) — meta&3==0 | **20** | Oak bark |
| 2–5 (sides) — meta&3==1 | **116** | Spruce bark (dark) |
| 2–5 (sides) — meta&3==2 | **117** | Birch bark (white) |
| 2–5 (sides) — meta&3==3 | **20** | Jungle bark (same as oak in 1.0) |

### Metadata Bit Layout

| Bits | Mask | Meaning |
|---|---|---|
| 0–1 | `meta & 3` | Wood type: 0=oak, 1=spruce, 2=birch, 3=jungle |
| 2–3 | `meta & 12` | Orientation: 0=vertical (Y), 4=X-axis, 8=Z-axis |

For rendering, only `meta & 3` affects texture; orientation does not change texture selection
(only the axis the end-faces point).

### Tick Behaviour
Log has `l()` (tickable) but the `a(ry, x, y, z, Random)` base tick is a no-op in `yy`.
`aip` does not appear to override the random tick method — tickable flag may be for future use
or a no-op inherited from the base.

### Drops
Default `yy` drop: 1 log item (ID 17) with damage = `meta & 3` (preserves wood type).

---

*Spec written by Analyst AI from `jb.java`, `cj.java`, `qo.java`, `aip.java`. No C# implementation consulted.*
