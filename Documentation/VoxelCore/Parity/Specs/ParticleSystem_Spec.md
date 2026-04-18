# Spec: ParticleSystem

**Java classes:** `afv` (WorldRenderer — particle spawn), `ry` (World — spawnParticle dispatch)
**Status:** PROVIDED
**Canonical name:** ParticleSystem / EntityFX

---

## Overview

Particles in 1.0 are spawned through the `IWorldAccess` (`bd`) listener. The World (`ry`)
provides a `spawnParticle` method; `afv` (WorldRenderer/RenderGlobal) receives it and
instantiates the appropriate `EntityFX` subclass by name string.

---

## Spawn API (`ry`)

```java
ry.a(String particleName,
     double x, double y, double z,
     double velX, double velY, double velZ)
```

Dispatches to all `bd` listeners:
```java
bd.a(particleName, x, y, z, velX, velY, velZ)
```

The receiver (`afv`) maps `particleName` → `EntityFX` subclass and adds it to the particle list.

---

## Particle Names

### Identified from WorldEvent handler (`afv`)

| Name | Context |
|---|---|
| `"smoke"` | Dispenser fire (event 2000), explosion aftermath (event 2004) |
| `"flame"` | Post-explosion (event 2004) |
| `"blockcrack_<blockId>"` | Block breaking (event 2001) — name includes block ID |
| `"iconcrack_<itemId>"` | Splash potion (event 2002), Eye of Ender (event 2003) |
| `"spell"` | Splash potion particles (event 2002) |
| `"portal"` | Eye of Ender break (event 2003) — ring of portal particles |

### Inferred from standard 1.0 particle set

| Name | Usage |
|---|---|
| `"bubble"` | Underwater / splash |
| `"splash"` | Water surface impact |
| `"wake"` | Swimming / fishing float |
| `"suspend"` | In/near water |
| `"depthsuspend"` | Deep water |
| `"crit"` | Critical hit melee |
| `"magicCrit"` | Critical hit with sword enchantment |
| `"dripWater"` | Dripping from wet block |
| `"dripLava"` | Dripping from lava block |
| `"snowballpoof"` | Snowball impact |
| `"hugeexplosion"` | Large explosion |
| `"largeexplode"` | Medium explosion |
| `"explode"` | Small explosion particle |
| `"fireworksSpark"` | (May not exist in 1.0) |
| `"heart"` | Animal breeding |
| `"angryVillager"` | Villager disturbed |
| `"happyVillager"` | Villager trading |
| `"note"` | Note block plays |
| `"enchantmenttable"` | Near enchanting table |
| `"snowshovel"` | Breaking snow layer |
| `"slime"` | Slime bounce |
| `"reddust"` | Redstone dust |
| `"townaura"` | Mycelium surface |
| `"witchMagic"` | (May not exist in 1.0) |

> Note: Names marked "(inferred)" are based on standard 1.0 particle conventions observed in
> other versions. Only names in the first table are directly confirmed from decompiled source.

---

## WorldEvent Particle Table

Particles spawned as part of WorldEvents in `afv.a(vi, int, int, int, int, int)`:

### Event 2000 — Dispenser smoke

Spawns **10 smoke particles** at the dispenser face.

Direction encoded in `data` = `(dx+1) + (dz+1)*3` where dx, dz ∈ {−1, 0, +1}:

| data | Direction | dx | dz |
|---|---|---|---|
| 0 | SW | −1 | −1 |
| 1 | S  | 0  | −1 |
| 2 | SE | +1 | −1 |
| 3 | W  | −1 |  0 |
| 4 | Centre | 0 | 0 |
| 5 | E  | +1 |  0 |
| 6 | NW | −1 | +1 |
| 7 | N  |  0 | +1 |
| 8 | NE | +1 | +1 |

Each of the 10 particles spawned with small random velocity spread around the facing direction.

### Event 2001 — Block break

Spawns **block crack particles** (`"blockcrack_<id>"`) in a scatter pattern around the broken
block position. Also plays the block's step sound at reduced volume.

Particle count: fills a 4×4×4 grid pattern with velocity = (rand−0.5)×0.2 per axis.

### Event 2002 — Splash potion impact

- Spawns **8 `"iconcrack_<potionItemId>"`** particles (the potion bottle texture)
- Spawns **8 `"spell"`** particles (magical mist)
- Plays `"random.glass"` sound

### Event 2003 — Eye of Ender breaks

- Spawns a ring of **`"portal"`** particles spiralling outward
- Spawns **`"iconcrack_<eyeItemId>"`** shards
- No sound

### Event 2004 — Explosion aftermath (smoke + flame)

Spawns **20 particles** alternating `"smoke"` and `"flame"` with random velocity spread.

---

## Particle Rendering

Particles (`EntityFX`) are rendered in `afv` each frame:
- Sorted by distance (farthest first for alpha-blending correctness)
- Use their own texture atlas region
- Affected by gravity (varies per particle type — e.g. smoke rises, gravel falls)
- Have a lifetime (in ticks) after which they are removed

---

## C# Mapping

| Java | C# |
|---|---|
| `ry.a(String, double*3, double*3)` | `World.SpawnParticle(string name, Vector3d pos, Vector3d vel)` |
| `bd.a(String, double*3, double*3)` | `IWorldAccess.SpawnParticle(string name, Vector3d pos, Vector3d vel)` |
| Particle name string | `ParticleType` enum or string-keyed registry |
| `EntityFX` base | `ParticleEntity` base class |

---

## Open Questions

- Full `EntityFX` class hierarchy — which particle names map to which subclasses.
- Exact per-particle physics constants (gravity, drag, lifetime).
- Whether `"blockcrack_"` concatenates the numeric block ID or the string name.
- `"iconcrack_"` ID source for splash potions — potion item ID or damage value?
