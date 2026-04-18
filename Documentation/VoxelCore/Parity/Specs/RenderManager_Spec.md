# Spec: RenderManager / WorldRenderer

**Java classes:** `afv` (WorldRenderer / RenderGlobal), `adt` (EntityRenderer)
**Status:** PROVIDED — PARTIAL (entity-renderer dispatch map not found)
**Canonical name:** RenderManager / RenderGlobal

---

## Overview

Rendering in 1.0 is split across several classes. This spec covers the primary world-rendering
class `afv` (WorldRenderer / RenderGlobal) which implements `IWorldAccess` and manages chunk
rendering, world events, and entity dispatch.

The `adt` (EntityRenderer) class manages first-person view setup, FOV, camera bobbing, and
hand rendering. It is documented in `EntityRenderer_Spec.md`.

A dedicated entity-renderer dispatch map (equivalent to 1.8+ `RenderManager`) was not
identified — entity rendering may be handled differently in 1.0.

---

## WorldRenderer / RenderGlobal (`afv`)

### Role

`afv` is the central client-side rendering coordinator. It:
1. Implements `bd` (`IWorldAccess`) to receive world change notifications.
2. Manages a list of `RenderChunk` objects (chunk sections ready for GL draw).
3. Handles `WorldEvent` dispatch → sounds + particles (see `SoundManager_Spec.md`).
4. Renders entities, block entities (tile entities), and particles.

### IWorldAccess implementation

See `IWorldAccess_Spec.md` for the full `bd` interface and dispatch method signatures.

`afv.j(int x, int y, int z)` — marks the chunk section containing `(x, y, z)` dirty for rebuild.

### WorldEvent handler

```java
afv.a(vi player, int eventId, int x, int y, int z, int data)
```

Handles event IDs 1000–2004. See `SoundManager_Spec.md` (sound table) and
`ParticleSystem_Spec.md` (particle table) for the full event mappings.

### Chunk rendering

- Chunk sections (16×16×16) are built into a display list or VBO.
- Dirty sections are rebuilt on demand (or each frame up to a budget).
- Opaque and translucent passes are separate.

---

## Entity-Renderer Dispatch

In later Minecraft versions, `RenderManager` maintains a `Map<Class, Renderer>` keyed by entity
class. In 1.0 this mechanism was not identified in the available decompiled files.

**Hypothesis:** The entity rendering dispatch in 1.0 may be an `instanceof` chain or a similar
ad-hoc dispatch in `afv` or a sibling class.

**Action for next research session:** Search for a class containing `instanceof` chains or a
`HashMap` mapping entity classes to renderer instances.

---

## Known Entity Renderers (inferred from 1.0 entity set)

| Entity | Renderer class (unknown) |
|---|---|
| EntityPlayer / EntityOtherPlayer | Biped model renderer |
| EntityCreeper / EntityZombie / EntitySkeleton | Biped variant |
| EntitySpider | Spider model renderer |
| EntityEnderman | Enderman model renderer |
| EntityArrow | Arrow renderer |
| EntityItem | Item sprite / 3D model renderer |
| EntityXPOrb | Sprite renderer |
| EntityTNTPrimed | Block renderer |
| EntityFallingBlock | Block renderer |
| EntityBoat | Boat model renderer |
| EntityMinecart | Minecart model renderer |
| EntityPainting | Painting renderer |

---

## C# Mapping

| Java | C# |
|---|---|
| `afv` | `WorldRenderer` (implements `IWorldAccess`) |
| `afv.a(vi,int,int,int,int,int)` | `WorldRenderer.OnWorldEvent(Player, int id, BlockPos, int data)` |
| Chunk section list | `WorldRenderer.ChunkSections : List<RenderChunk>` |
| Dirty mark | `RenderChunk.IsDirty : bool` |

---

## Open Questions

- Java class name for the entity-renderer registry/dispatch class.
- Whether chunk sections use display lists or vertex arrays in 1.0.
- How translucency sorting is handled for water/glass faces.
- Whether `afv` also renders block entities (tile entities) or a separate class does.
