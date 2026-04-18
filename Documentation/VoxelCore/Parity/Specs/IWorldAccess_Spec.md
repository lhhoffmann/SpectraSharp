# Spec: IWorldAccess (World Event Listener Interface)

**Java class:** `bd` (interface), `ry.z` (listener list), `afv` (primary implementor)
**Status:** PROVIDED
**Canonical name:** IWorldAccess

---

## Overview

`bd` is an event-listener interface that the World (`ry`) notifies when observable changes occur:
block updates, sound events, particle spawns, and entity add/remove events. Implementors are
registered on the world and receive dispatch calls for client-side effects.

The primary implementor is `afv` (WorldRenderer / RenderGlobal). Multiple listeners may be
registered simultaneously.

---

## Interface Definition

```java
public interface bd {

    // Block invalidation — single block
    void a(int x, int y, int z);

    // Block invalidation — AABB region
    void a(int x1, int y1, int z1, int x2, int y2, int z2);

    // Play sound at coordinates (with volume/pitch)
    void a(String soundName, double x, double y, double z, float volume, float pitch);

    // Spawn particle at coordinates with velocity
    void a(String particleName, double x, double y, double z,
           double velX, double velY, double velZ);

    // Notify entity added to world
    void a(ia entity);

    // Notify entity removed from world
    void b(ia entity);

    // World event / auxiliary SFX (eventId + position, no data)
    void a(String name, int x, int y, int z);

    // Play record / jukebox sound
    void a(int x, int y, int z, bq sound);

    // Dirty region for a player (excludes self-render)
    void a(vi player, int x1, int y1, int z1, int x2, int y2, int z2);
}
```

> Note: Java resolves overloads by parameter types. The 9 overloads of `a` are unambiguous
> at call sites in `ry`.

---

## Listener Registration (`ry`)

| Method | Signature | Action |
|---|---|---|
| `ry.a(bd)` | `addWorldAccess(IWorldAccess)` | Appends to `ry.z` (List<bd>) |
| `ry.b(bd)` | `removeWorldAccess(IWorldAccess)` | Removes from `ry.z` |

`ry.z` is a plain `List<bd>`. Dispatch iterates all registered listeners.

---

## World-side Dispatch Methods (`ry`)

The following `ry` methods dispatch to all `bd` listeners in `ry.z`:

### Block invalidation — single block

```java
ry.j(int x, int y, int z)
// → bd.a(x, y, z)  for all listeners
```

Called when a single block changes and the renderer must update that chunk section.

### Block invalidation — region

```java
ry.c(int x1, int y1, int z1, int x2, int y2, int z2)
// → bd.a(x1,y1,z1, x2,y2,z2)  for all listeners
```

### Play sound at entity position

```java
ry.a(ia entity, String soundName, float volume, float pitch)
// → bd.a(soundName, entity.x, entity.y, entity.z, volume, pitch)
```

### Play sound at coordinates

```java
ry.a(double x, double y, double z, String soundName, float volume, float pitch)
// → bd.a(soundName, x, y, z, volume, pitch)
```

### World event / auxiliary SFX

```java
ry.a(String eventName, int x, int y, int z, int data)
// → bd.a(vi=null, x, y, z, x+data, y, z)  (encoded as player-dirty overload)
```

> The world event dispatch reuses the `a(vi,int,int,int,int,int,int)` overload with
> `player=null` and `data` packed into the second AABB coordinate. See WorldEvents_Spec for
> the event table.

### Spawn particle

```java
ry.a(String particleName, double x, double y, double z,
     double velX, double velY, double velZ)
// → bd.a(particleName, x, y, z, velX, velY, velZ)
```

### Entity added

```java
ry.c(ia entity)
// → bd.a(entity)  for all listeners
```

### Entity removed

```java
ry.d(ia entity)
// → bd.b(entity)  for all listeners
```

---

## Primary Implementor: `afv` (WorldRenderer / RenderGlobal)

`afv` implements `bd` and maintains a list of render chunk objects. On receiving block
invalidation calls it marks the appropriate chunk sections dirty for rebuild.

Sound, particle, and world-event dispatch in `afv` is described in `SoundManager_Spec.md`
and `ParticleSystem_Spec.md`.

The `a(vi, int,int,int, int,int,int)` overload in `afv` is the world-event handler —
see `WorldEvents_Spec.md`.

---

## C# Mapping

| Java | C# |
|---|---|
| `bd` interface | `IWorldAccess` |
| `ry.z` | `World.WorldAccessListeners : List<IWorldAccess>` |
| `ry.a(bd)` | `World.AddWorldAccess(IWorldAccess)` |
| `ry.b(bd)` | `World.RemoveWorldAccess(IWorldAccess)` |
| `ry.j(x,y,z)` | `World.NotifyBlockChange(int x, int y, int z)` |
| `ry.c(x1..z2)` | `World.NotifyBlocksChanged(BlockPos min, BlockPos max)` |
| `bd.a(String,double*3,float,float)` | `IWorldAccess.PlaySound(string name, Vector3d pos, float vol, float pitch)` |
| `bd.a(String,double*3,double*3)` | `IWorldAccess.SpawnParticle(string name, Vector3d pos, Vector3d vel)` |
| `bd.a(ia)` | `IWorldAccess.OnEntityAdded(Entity entity)` |
| `bd.b(ia)` | `IWorldAccess.OnEntityRemoved(Entity entity)` |
| `bd.a(String,int,int,int)` | `IWorldAccess.PlayWorldEvent(string name, BlockPos pos)` |
| `bd.a(int,int,int,bq)` | `IWorldAccess.PlayRecord(BlockPos pos, ISound sound)` |
| `bd.a(vi,int*6)` | `IWorldAccess.MarkBlocksDirty(Player player, BlockPos min, BlockPos max)` |

---

## Open Questions

- Exact call site for `ry.a(String,int,int,int,int data)` — how data is encoded in the
  7-argument BD overload needs cross-checking with `afv` event handler parameter reading.
- Are there other implementors of `bd` besides `afv` (e.g. sound-only listener)?
