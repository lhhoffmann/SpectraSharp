# Spec: Mineshaft Structure Pieces

**Java classes:** `uk` (Start), `aba` (Corridor), `ra` (Crossing), `id` (Staircase), `aez` (Factory)
**Status:** PROVIDED
**Canonical name:** MineshaftPieces / WorldGenMineshaft pieces

---

## Overview

Mineshafts are procedurally generated underground structures composed of up to four piece types
assembled recursively by a factory. Depth is capped at 8; the root-centre radius is capped at 80
blocks. All pieces extend base class `nk`.

---

## Piece Factory (`aez`)

`aez.a(nk parent, List pieces, Random rand, int x, int y, int z, int facing, int depth)`

### Piece selection (var7 = rand.nextInt(100))

| var7 range | Piece class | Type | Probability |
|---|---|---|---|
| 0–69 | `aba` | Corridor | 70% |
| 70–79 | `ra` | Crossing | 10% |
| 80–99 | `id` | Staircase | 20% |

Depth guard: returns `null` if `depth > 8`.
Radius guard: returns `null` if distance from start > 80 blocks.

### Loot table

Chests are filled with `agq` weighted entries (11 total) using item IDs from `acy.*`.
Items include: rail (`acy.aL`), bread, coal, melon seeds, pumpkin seeds, apple, gold ingot,
iron ingot, iron pickaxe, gold, redstone. (Exact weights to be confirmed from `agq` entries.)

---

## Mineshaft Start (`uk`)

**Java class:** `uk extends nk`

### Construction

```
new nl(x, 50, z,  x + 7 + rand(6),  54 + rand(6),  z + 7 + rand(6))
```

- Fixed Y start = **50** (underground, near lava layer)
- Width:  `7 + rand.nextInt(6)` → 7–12 blocks (X)
- Height: `4 + rand.nextInt(6)` → 4–9 blocks (Y) — top = 54+rand(6)
- Depth:  `7 + rand.nextInt(6)` → 7–12 blocks (Z)

### Expansion

Iterates all 4 horizontal walls (-Z, +Z, -X, +X), stepping along each wall:

```
step += rand.nextInt(bb.width)
if step + 3 > bb.width: break
```

At each step position, calls `aez.a(...)` with the appropriate facing (0=−Z, 1=−X, 2=+Z, 3=+X).
Successful results have their bounding-box ends clamped flush to the start wall, creating a
doorway connection region of depth 2.

---

## Mineshaft Corridor (`aba`)

**Java class:** `aba extends nk`

### Bounding box

- Width: **3 blocks** (X, fixed)
- Height: **3 blocks** (Y, fixed)
- Length: `(rand.nextInt(3) + 2) × 5` = **10, 15, or 20 blocks** (Z/facing axis)

### Flags (set in constructor, probability)

| Field | Probability | Meaning |
|---|---|---|
| `a` (cobweb) | 1/3 | Spawn cobwebs near supports |
| `b` (spawner) | 1/23 if !cobweb | Place one cave spider spawner |

### Generation

**Supports:** placed every 5 blocks. Count = `length / 5`.

Per support at block position `p` (p = 2, 7, 12, …):

| Element | Block | Condition |
|---|---|---|
| Fence posts (×2) | `yy.aZ` (Oak Fence) | At floor (y=bb.minY), at x=bb.minX+1 and x=bb.maxX-1 |
| Crossbeam planks | `yy.x` (Oak Planks) | At ceiling (y=bb.minY+2), fill x from minX+1 to maxX-1 |
| Cobwebs ±1 | `yy.aN` (Cobweb) | 10% chance per side (x=0, x=2), if `a` flag set |
| Cobwebs ±2 | `yy.aN` (Cobweb) | 5% chance per side (±2 from crossbeam), if `a` flag set |

**Rails:** `yy.aG` (Rail) placed at floor centre (z-axis, every block) with 70% chance per block.

**Cave spider spawner:** placed once at a random support position if `b` flag set.

**Loot chest:** 1% chance per support (1/100). Contains `agq` table items.

### Exits

At most 3 exits generated (forward + optional perpendicular branches):
- Forward: calls `aez.a(...)` at the far end, depth+1
- Left/right sides: each has independent random chance, depth+1
- Guards: `depth > 8` → no further exits

---

## Mineshaft Crossing (`ra`)

**Java class:** `ra extends nk`

### Bounding box (facing-dependent)

| Dimension | Value |
|---|---|
| Width | 3 blocks |
| Depth | 8 blocks |
| Height | 8 blocks |

### Generation

- Fills bounding box walls/floor/ceiling with Stone Bricks or cobblestone (exact block TBD).
- Interior is air.
- Diagonal staircase fill: fills from `y = bb.minY+5` stepping down toward `y = bb.maxY-7` in a
  stair-step pattern (bottom→top, diagonal).
- **Only 1 exit** (forward direction, `depth+1`).

---

## Mineshaft Staircase (`id`)

**Java class:** `id extends nk`

### Bounding box

Two variants selected at construction time (`b = rand.nextInt(4) > 2` — 25% tall):

| Variant | Width | Height | Length |
|---|---|---|---|
| Normal (75%) | 5 | 3 | 5 |
| Tall (25%) | 5 | 7 | 5 |

### Exits

Always generates **3 exits** in the three perpendicular directions (left, right, forward).
The tall variant additionally generates exits at `y+4` for vertical connections.

---

## Placement Algorithm

1. `MineshaftStart.generate()` checks `this.a(world, bb)` — returns false if terrain is
   already occupied (overlap check), to avoid overlap with other structures.
2. Carves the bounding box air interior.
3. Places floor (dirt/gravel mix? — exact block TBD).
4. Each piece generates its specific features (supports, rails, etc.) after bounding box fill.

---

## Open Questions

- Exact block IDs used for walls/floor/ceiling of crossings and staircases.
- Full loot table weight values.
- Overlap check method details (`nk.a(world, bb)`).
- Rail placement: single rail block, or `RailLogic` orientation computed?
