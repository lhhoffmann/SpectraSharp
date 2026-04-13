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
| [MovingObjectPosition_Spec.md](Specs/MovingObjectPosition_Spec.md) | Ray-cast result container — block hit and entity hit constructors, face ID layout, pooled Vec3 hit position | [STATUS:IMPLEMENTED] `Core/MovingObjectPosition.cs` (Entity typed as object — ia spec pending) |
| [EnumMovingObjectType_Spec.md](Specs/EnumMovingObjectType_Spec.md) | Two-value enum: TILE (0) and ENTITY (1) | Ready |
| [Block_Spec.md](Specs/Block_Spec.md) | Block base class — static registry (256 slots), 8 parallel arrays, instance fields, builder pattern, collision/ray-trace/tick/drop/rendering virtual methods | Ready |

### Mappings

| File | Description |
|---|---|
| [Mappings/classes.md](Mappings/classes.md) | Obfuscated class name → human-readable name |

## How to add a spec

1. Analysis AI writes `Specs/<Topic>.md` following the spec template.
2. Analysis AI adds a row to the table above.
3. Coder AI picks it up and crosses off the corresponding REQUESTS.md entry.
