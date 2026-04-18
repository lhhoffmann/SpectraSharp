# Spec: SoundManager

**Java classes:** `ry` (World — dispatch side), `afv` (WorldRenderer — receiver/playback side)
**Status:** PROVIDED
**Canonical name:** SoundManager / SoundEventDispatch

---

## Overview

Sound in 1.0 is dispatched through the `IWorldAccess` (`bd`) listener system. The World (`ry`)
provides convenience methods that forward to all registered listeners. The actual audio playback
is handled by the client implementor (`afv` / WorldRenderer) which maps string sound names to
LWJGL audio resources.

This spec covers:
1. The World-side dispatch API (what game logic calls)
2. The known sound name strings
3. The WorldEvent sound table (events that play sounds as a side effect)

---

## Sound Dispatch API (`ry`)

### playSound — at entity position

```java
ry.a(ia entity, String soundName, float volume, float pitch)
```

Dispatches `bd.a(soundName, entity.x, entity.y, entity.z, volume, pitch)` to all listeners.

### playSound — at coordinates

```java
ry.a(double x, double y, double z, String soundName, float volume, float pitch)
```

Dispatches `bd.a(soundName, x, y, z, volume, pitch)` to all listeners.

### playWorldEvent / playAuxSFX

```java
ry.a(String eventName, int x, int y, int z, int data)
```

WorldEvent dispatch — plays sounds and spawns particles depending on event ID.
The `eventName` parameter is the integer event ID encoded as a string in some call sites,
or the string-based event name depending on version. The receiver (`afv`) decodes by integer.

See WorldEvents table below.

---

## Known Sound Name Strings

The following sound names were identified from `afv.java` (WorldRenderer event handler)
and other call sites:

### UI / Random

| Sound name | Usage |
|---|---|
| `"random.click"` | Button click (GUI), Dispenser fire with data=1000 |
| `"random.click"` (pitch 1.2) | Dispenser empty (event 1001) |
| `"random.bow"` | Arrow fired (event 1002), pitch 1.2 |
| `"random.door_open"` | Door opened (event 1003) |
| `"random.door_close"` | Door closed (event 1003) |
| `"random.fizz"` | Water+lava, splash (event 1004), vol 0.5, pitch ~2.6 |
| `"random.glass"` | Splash potion impact (event 2002) |
| `"random.fuse"` | TNT ignition |
| `"random.pop"` | XP orb pickup |
| `"random.burp"` | Eating |
| `"random.splash"` | Splash in water |

### Liquid

| Sound name | Usage |
|---|---|
| `"liquid.water"` | Swimming / walking in water |

### Footsteps

| Sound name | Usage |
|---|---|
| `"step.gravel"` | Walking on gravel |
| `"step.stone"` | Walking on stone (inferred) |
| `"step.wood"` | Walking on wood (inferred) |
| `"step.grass"` | Walking on grass (inferred) |
| `"step.sand"` | Walking on sand (inferred) |
| `"step.cloth"` | Walking on wool (inferred) |
| `"step.snow"` | Walking on snow (inferred) |

> Note: Most step sounds inferred from common 1.0 sound path conventions.
> Only `"step.gravel"` directly observed in source.

### Portal

| Sound name | Usage |
|---|---|
| `"portal.trigger"` | Entering portal |
| `"portal.travel"` | Teleporting through portal |

### Mob

| Sound name | Usage |
|---|---|
| `"mob.cow"` | Cow idle |
| `"mob.cowhurt"` | Cow hurt |
| `"mob.slime"` | Slime movement |
| `"mob.creeper"` | Creeper idle |
| `"mob.creeperdeath"` | Creeper death |
| `"mob.ghast.charge"` | Ghast charge-up, vol 10.0 (event 1007) |
| `"mob.ghast.fireball"` | Ghast fireball launch, vol 10.0 (event 1008) |
| `"mob.ghast.fireball"` | Ghast fireball impact, vol 1.0 (event 1009) |
| `"mob.villager.default"` | Villager idle |

---

## WorldEvent Sound Table

WorldEvents are dispatched from game logic via `ry.a(eventName, x, y, z, data)` and handled
in `afv.a(vi player, int id, int x, int y, int z, int data)`.

| Event ID | Sound name | Volume | Pitch | Notes |
|---|---|---|---|---|
| 1000 | `"random.click"` | 1.0 | 1.0 | Dispenser fires |
| 1001 | `"random.click"` | 1.0 | 1.2 | Dispenser empty |
| 1002 | `"random.bow"` | 1.0 | 1.2 | Bow fired |
| 1003 | `"random.door_open"` or `"random.door_close"` | 1.0 | 1.0 | data=0→open, data=1→close |
| 1004 | `"random.fizz"` | 0.5 | ~2.6 | Water+fire/lava contact |
| 1005 | record item sound or stop | — | — | data = item ID of record; 0 = stop jukebox |
| 1007 | `"mob.ghast.charge"` | 10.0 | (1.0) | Ghast charges |
| 1008 | `"mob.ghast.fireball"` | 10.0 | (1.0) | Ghast shoots |
| 1009 | `"mob.ghast.fireball"` | 1.0 | (1.0) | Fireball impact |
| 2000 | — (particles only) | — | — | Smoke puff from dispenser |
| 2001 | block step sound | — | — | Block break: uses block's step sound + `"dig."` prefix |
| 2002 | `"random.glass"` | — | — | Splash potion shatter |
| 2003 | — (particles only) | — | — | Eye of Ender breaks |
| 2004 | — (particles only) | — | — | Explosion smoke+flame |

> Note: Events 1006, 1010–1006, 2005+ are not observed in `afv.java`.

---

## Pitch Randomisation

In most world-event and entity-sound calls, pitch is computed as:
```
basePitch * (0.8 + rand.nextFloat() * 0.4)
```
giving ±20% random variation. Where exact pitch is given above it is the base pitch before
randomisation (unless marked as fixed).

---

## C# Mapping

| Java | C# |
|---|---|
| `ry.a(ia, String, float, float)` | `World.PlaySoundAtEntity(Entity e, string name, float vol, float pitch)` |
| `ry.a(double*3, String, float, float)` | `World.PlaySound(Vector3d pos, string name, float vol, float pitch)` |
| `ry.a(String, int,int,int, int)` | `World.PlayWorldEvent(int eventId, BlockPos pos, int data)` |
| `bd.a(String, double*3, float, float)` | `IWorldAccess.PlaySound(string name, Vector3d pos, float vol, float pitch)` |
| `afv.a(vi, int, int,int,int, int)` | `WorldRenderer.OnWorldEvent(Player self, int id, BlockPos pos, int data)` |

---

## Open Questions

- Full list of step sound names for all block materials.
- Exact pitch computation for fizz sound (event 1004) — observed as ~2.6, may be
  `(float)(rand * 0.4 + 2.0)` or similar.
- Whether `SoundManager` is a separate class or entirely handled inline in `afv`.
- Record item ID → sound file name mapping.
