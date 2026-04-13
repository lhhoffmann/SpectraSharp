# Coder AI — Open Requests

The coder AI adds entries here when it needs a spec that does not yet exist.
The analysis AI works through this list and creates the corresponding file in `Specs/`.

## Format

```
## <Topic>
**Needed for:** <which C# class/feature needs this>
**Questions:** <specific unknowns that block implementation>
```

---

## MathHelper
[STATUS:IMPLEMENTED]
**Needed for:** `Core/MathHelper.cs` — deterministic trig used by physics, entity movement, world-gen
**Questions:**
- Sine table: how many entries? (suspected 65536, but must be confirmed)
- Is the table indexed as `sin[(int)(angle * TABLE_SIZE / TAU) & MASK]` or different?
- Does the class expose `cos` as a phase-shifted sine lookup or a separate table?
- Are there any other constants or helper methods (e.g. `floor`, `abs`, clamp) that must match Java's behaviour exactly?
- Which Java class name does this map to? (`net.minecraft.src.MathHelper`? or `Mth`?)

## AxisAlignedBB
[STATUS:IMPLEMENTED]
**Needed for:** `Core/AxisAlignedBB.cs` — axis-aligned bounding boxes for block collision, entity physics, raycasting
**Questions:**
- Field layout: minX/minY/minZ/maxX/maxY/maxZ as doubles? Any cached centre or size fields?
- Is there a static factory (`getBoundingBox`) or only constructors?
- Which methods exist? Expected: expand, offset, intersects, isVecInside, calculateXOffset/YOffset/ZOffset (sweep), addCoord
- Do the offset/expand methods mutate in-place or return new instances?
- Does `calculateXOffset` (sweep) clamp or return unchanged delta when no collision?
- Any quirks in the intersection / overlap tests (e.g. open vs closed interval)?
- Java class name: `net.minecraft.src.AxisAlignedBB`?

## Vec3
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Vec3.cs` — used by `AxisAlignedBB.isVecInside` and `AxisAlignedBB.rayTrace`
**Questions:**
- Fields: `a=X, b=Y, c=Z` as doubles — confirmed in AABB spec. Any other fields?
- `a(Vec3, double)` → Vec3? : plane-clip at X=value along segment [this, other] — returns null if segment doesn't reach the plane?
- `b(Vec3, double)` → Vec3? : same for Y plane
- `c(Vec3, double)` → Vec3? : same for Z plane
- `e(Vec3)` → double : distance to target — squared or Euclidean?
- Is Vec3 pooled? Mutable or immutable?
- Java class name: `net.minecraft.src.Vec3`? (obf: `fb`)

## MovingObjectPosition
[STATUS:IMPLEMENTED]
**Needed for:** `Core/MovingObjectPosition.cs` — return type of `AxisAlignedBB.rayTrace`
**Questions:**
- Constructor signature: `new gv(int blockX, int blockY, int blockZ, int faceId, Vec3 hitPoint)` — confirmed from AABB spec?
- Which fields does it expose?
- Is there a second constructor for entity hits?
- Java class name: `net.minecraft.src.MovingObjectPosition`? (obf: `gv`)

## Block
[STATUS:PROVIDED]
**Needed for:** `Core/Block.cs` — base class for all block types; required by Chunk, World, and physics
**Questions:**
- Static registry: is there a `Block[] blocksList` array indexed by block ID? What is the max ID (256?)?
- Instance fields: blockID (int), hardness (float), resistance (float), stepSound, lightOpacity, lightValue — which are per-instance vs static?
- `getBoundingBox(World, int x, int y, int z)` → AxisAlignedBB — does the default return a full unit cube [0,1]³?
- `blockTick(World, int x, int y, int z, Random)` — signature correct? Default is no-op?
- `onBlockAdded`, `onBlockRemoved`, `onNeighborBlockChange` — do these exist on the base class? Signatures?
- `canCollideCheck`, `isOpaqueCube`, `renderAsNormalBlock` — boolean queries on the base class?
- `getCollisionBoundingBoxFromPool(World, int x, int y, int z, AxisAlignedBB entityBox)` → AxisAlignedBB? — is this separate from getBoundingBox?
- Java class name: `net.minecraft.src.Block` (obf: `aku`?)

## EnumMovingObjectType
[STATUS:PROVIDED]
**Needed for:** `Core/HitType.cs` — confirm whether `bo` is a Java enum with ordinal or class with static constants
**Questions:**
- Is `bo` a Java `enum` type with `bo.a` and `bo.b` as enum constants?
- Or is it a class with `public static final` int/Object fields?
- Are there any other values beyond TILE (a) and ENTITY (b)?
- Java class name: `net.minecraft.src.EnumMovingObjectType`? (obf: `bo`)

## JavaRandom
[STATUS:IMPLEMENTED]
**Needed for:** `Core/JavaRandom.cs`
**Notes:** Implemented from Java SE public specification (LCG algorithm is normative). No decompiled source consulted.
