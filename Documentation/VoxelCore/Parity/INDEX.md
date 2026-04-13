# SpectraSharp Parity Documentation

This folder is the communication hub between the two AIs in the clean-room workflow.

## Workflow

```
Analysis AI                        Coder AI
(reads decompiled code)            (reads specs only, never sees original)
        |                                  |
        v                                  v
   Specs/*.md  ──────────────────►  implements C# from spec
                                           |
                                           v
                                     REQUESTS.md
                                   (asks for missing specs)
```

## Index

### Specs

| File | Subject | Status |
|---|---|---|
| [MathHelper_Spec.md](Specs/MathHelper_Spec.md) | Sine/cosine lookup table + numeric helpers (floor, sqrt, clamp, abs, floor-division, RNG range) | [STATUS:IMPLEMENTED] `Core/MathHelper.cs` |
| [AxisAlignedBB_Spec.md](Specs/AxisAlignedBB_Spec.md) | Axis-aligned bounding box — fields, object pool, sweep collision (X/Y/Z offset), intersects, isVecInside, ray trace | [STATUS:IMPLEMENTED] `Core/AxisAlignedBB.cs` |
| [Vec3_Spec.md](Specs/Vec3_Spec.md) | 3D double vector — pool, subtract, normalize, dot, cross, add, distance, segment-plane intersection, in-place rotation | [STATUS:IMPLEMENTED] `Core/Vec3.cs` |
| [MovingObjectPosition_Spec.md](Specs/MovingObjectPosition_Spec.md) | Ray-cast result container — block hit and entity hit constructors, face ID layout, pooled Vec3 hit position | [STATUS:IMPLEMENTED] `Core/MovingObjectPosition.cs` |
| [EnumMovingObjectType_Spec.md](Specs/EnumMovingObjectType_Spec.md) | Two-value enum: TILE (0) and ENTITY (1) | [STATUS:IMPLEMENTED] `Core/HitType.cs` |
| [Block_Spec.md](Specs/Block_Spec.md) | Block base class — static registry (256 slots), 8 parallel arrays, instance fields, builder pattern, collision/ray-trace/tick/drop/rendering virtual methods | [STATUS:IMPLEMENTED] `Core/Block.cs` (StepSound/Material/World as placeholders) |
| [StepSound_Spec.md](Specs/StepSound_Spec.md) | StepSound (`wu`) — sound name + volume/pitch floats; `bj` (glass) and `aeg` (sand) subclasses. **Note: classes.md had wu↔p swapped — corrected.** | [STATUS:IMPLEMENTED] `Core/StepSound.cs` + static instances on `Block` |
| [Material_Spec.md](Specs/Material_Spec.md) | Material (`p`) — map color, liquid/solid/flammable/replaceable/passable flags, mobility (0/1/2); 30 static instances; `sn`/`mw`/`br`/`bk`/`tx` subclasses | [STATUS:IMPLEMENTED] `Core/Material.cs` + `Core/MapColor.cs` |
| [IBlockAccess_Spec.md](Specs/IBlockAccess_Spec.md) | IBlockAccess (`kq`) interface — 12 methods for world read access; 7 confirmed, 5 uncertain | [STATUS:IMPLEMENTED] `Core/IBlockAccess.cs` |
| [Chunk_Spec.md](Specs/Chunk_Spec.md) | Chunk (`zx`) — 16×128×16 block/light/entity storage; block array index formula; nibble arrays; height map; full method list | [STATUS:IMPLEMENTED] `Core/Chunk.cs` + `Core/NibbleArray.cs` + `Core/LightType.cs` |
| [World_Spec.md](Specs/World_Spec.md) | World (`ry`) — implements IBlockAccess; all entity/TE/tick/light/raytrace/weather methods; 2788 lines fully analysed | [STATUS:IMPLEMENTED] `Core/World.cs` + `Core/IChunkLoader.cs` (entity/WorldProvider/BFS stubs) |
| [Entity_Spec.md](Specs/Entity_Spec.md) | Entity (`ia`) — abstract base; all fields, AABB layout, move/sweep physics, fire, mount/rider, DataWatcher flags, drop items | [STATUS:IMPLEMENTED] `Core/Entity.cs` |
| [WorldProvider_Spec.md](Specs/WorldProvider_Spec.md) | WorldProvider (`k`) — abstract; brightness table formula, sky colour, sun angle, moon phase, dim factory | [STATUS:IMPLEMENTED] `Core/WorldProvider.cs` |

| [ItemStack_Spec.md](Specs/ItemStack_Spec.md) | ItemStack (`dk`) — item + count + damage container; fields c=itemId/a=stackSize/e=damage; NBT serialisation; Unbreaking check | [STATUS:IMPLEMENTED] `Core/ItemStack.cs` |
| [EntityItem_Spec.md](Specs/EntityItem_Spec.md) | EntityItem (`ih`) — dropped-item entity; 0.25×0.25 size; gravity/bounce/friction tick; pickup delay; despawn at age 6000 | [STATUS:IMPLEMENTED] `Core/EntityItem.cs` |
| [DataWatcher_Spec.md](Specs/DataWatcher_Spec.md) | DataWatcher (`cr`) — per-entity synchronized data; 7 types; typeId<<5|entryId wire header; 0x7F terminator | [STATUS:IMPLEMENTED] `Core/DataWatcher.cs` |
| [Item_Spec.md](Specs/Item_Spec.md) | Item (`acy`) — base item class; d[32000] registry (offset 256); maxStackSize; icon atlas; virtual use/hit/place methods | [STATUS:IMPLEMENTED] `Core/Item.cs` |
| [LivingEntity_Spec.md](Specs/LivingEntity_Spec.md) | LivingEntity (`nq`) — abstract; health/invulnerability/armor/knockback/potions/AI/friction movement | [STATUS:IMPLEMENTED] `Core/LivingEntity.cs` |

### Mappings

| File | Description |
|---|---|
| [Mappings/classes.md](Mappings/classes.md) | Obfuscated class name → human-readable name |

## How to add a spec

1. Analysis AI writes `Specs/<Topic>.md` following the spec template.
2. Analysis AI adds a row to the table above.
3. Coder AI picks it up and crosses off the corresponding REQUESTS.md entry.
