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
[STATUS:IMPLEMENTED]
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
[STATUS:IMPLEMENTED]
**Needed for:** `Core/HitType.cs` — confirm whether `bo` is a Java enum with ordinal or class with static constants
**Questions:**
- Is `bo` a Java `enum` type with `bo.a` and `bo.b` as enum constants?
- Or is it a class with `public static final` int/Object fields?
- Are there any other values beyond TILE (a) and ENTITY (b)?
- Java class name: `net.minecraft.src.EnumMovingObjectType`? (obf: `bo`)

## StepSound
[STATUS:IMPLEMENTED]
**Needed for:** `Core/StepSound.cs` — step sound groups used by Block constructor and getLightOpacity
**Questions:**
- Fields: name string? pitch/volume floats? Which fields exist?
- `c()` → bool : called in Block constructor as `!stepSound.c()` for canBlockGrass — is this isLiquid?
- `i()` → bool : called in Block.canReplace — isReplaceable?
- `l()` → int  : called in Block.getLightOpacity — returns what value?
- Java class name: `net.minecraft.src.StepSound`? (obf: `p`)

## Material
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Material.cs` — block material type used by Block.bX field
**Questions:**
- Fields: any instance state beyond type identity?
- `c()` → bool : called somewhere relating to liquid — isLiquid?
- `b()` → bool : called in Block.isNormalCube — what does it check?
- Are Material instances singletons (static finals on Block)?
- Java class name: `net.minecraft.src.Material`? (obf: `wu`)

## IBlockAccess
[STATUS:IMPLEMENTED]
**Needed for:** `Core/IBlockAccess.cs` — read-only world view used by Block rendering and bounds queries
**Questions:**
- Full method list: `a(x,y,z)` getBlockId, `d(x,y,z)` getBlockMetadata, `e(x,y,z)` getMaterial, `f(x,y,z)` isOpaqueCube, `g(x,y,z)` isWet, `b(x,y,z,int)` getBrightness — all confirmed?
- Any additional methods?
- Java class name / interface: `net.minecraft.src.IBlockAccess`? (obf: `kq`)

## Chunk
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Chunk.cs` — stores block IDs, light data, and height map for one 16×128×16 column
**Questions:**
- Storage layout: single byte[] for block IDs (16×128×16 = 32768 bytes)? Or nibble arrays?
- Light data: is sky-light and block-light stored as nibble arrays (4-bit per block)?
- Height map: int[] of 256 values (one per XZ column), what does each value represent (top solid Y)?
- Dirty flags: does Chunk track isDirty, isLightPopulated, isTerrainPopulated as separate booleans?
- Neighbour references: does Chunk hold direct references to adjacent chunks?
- Any ChunkSection (16×16×16 sub-chunk) structure, or is it one flat 16×128×16 array?
- Java class name / obf name of Chunk?

## World
[STATUS:IMPLEMENTED]
**Needed for:** `Core/IWorld.cs` + `Core/World.cs` — implements IBlockAccess; manages chunks and tick loop
**Questions:**
- Does World implement `kq` (IBlockAccess) directly?
- Key fields: chunk map (HashMap<long, Chunk>?), worldRandom (JavaRandom), isClientSide (boolean I), worldTime (long), spawn coords?
- `a(x,y,z)` → int : getBlockId — does it delegate to Chunk.getBlockId?
- `e(x,y,z)` → Material : getBlockMaterial — does it look up Block.blocksList[id].bZ?
- `spawnEntityInWorld` — signature?
- `scheduleBlockUpdate(x,y,z,blockId,delay)` — does this exist?
- Random tick logic: how are random ticks distributed per chunk per tick?
- Java class name / obf name of World?

## Entity
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Entity.cs` — base class for all entities; required to un-stub World entity management,
Chunk entity buckets, and Block.dropBlockAsItemWithChance (EntityItem spawn).
**Questions:**
- Obfuscated class name of Entity base class? (suspected `ia`)
- Position fields: `s`=posX (double), `t`=posY (double), `u`=posZ (double)?
  Or different field names? Are prevPos fields also present (`q`, `r`, `p`)?
- Velocity fields: `H`=motionX, `I`=motionY, `J`=motionZ (double)?
- AABB: `C` field = AxisAlignedBB (`c`)? How is it sized/positioned?
- Chunk tracking fields: `ah`=addedToChunk, `ai`=chunkCoordX, `aj`=chunkCoordY (bucket), `ak`=chunkCoordZ?
- Dead flag: `K`=isDead (boolean)?
- Entity tick: `a()` — main tick method, called by World per tick?
- Mount/rider: `ab()` returns rider entity? How is mount link stored?
- `v()` — marks entity as dead?
- Is `ia` abstract or concrete? Any subclasses needed immediately (EntityItem `ih`, Player `vi`)?

## WorldProvider
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldProvider.cs` — dimension rules used by World and Chunk; required to
un-stub `GetBrightness`, `GetLightValue`, sky-light propagation, and weather.
**Questions:**
- Obfuscated class name? (suspected `k`)
- `e` field: boolean — is this dimension the Nether (no sky-light)?
- `b` field: WorldChunkManager (`vh`) — needed for `IBlockAccess.GetContextObject()`?
- `f[]` field: float[] sky-brightness lookup table (length 16) — maps light level 0–15 to
  brightness float 0.0–1.0? What are the actual values?
- `a(ry)` method: `registerWorld(world)` — what does it do?
- Any other fields/methods called directly by World or Chunk?
- Is WorldProvider abstract with subclasses for Overworld / Nether / End?

## JavaRandom
[STATUS:IMPLEMENTED]
**Needed for:** `Core/JavaRandom.cs`
**Notes:** Implemented from Java SE public specification (LCG algorithm is normative). No decompiled source consulted.

## ItemStack
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ItemStack.cs` — item + count + damage container; used by EntityItem, Block drop methods, and inventory
**Questions:**
- Obfuscated class name? (suspected `dk`)
- Fields: item reference (zx?), count (int), itemDamage (int)? Any other fields?
- `b()` → int : stackSize / item count?
- `a()` → Item : the item type?
- Static factory or constructor: `new dk(int itemId, int count, int damage)`?
- Is ItemStack mutable (setters) or immutable?
- Java class name: `net.minecraft.src.ItemStack`?

## EntityItem
[STATUS:IMPLEMENTED]
**Needed for:** `Core/EntityItem.cs` — dropped item entity; spawned by `Block.DropBlockAsItemWithChance`
and by World block-break logic
**Questions:**
- Obfuscated class name? (suspected `ih`)
- Does it extend Entity (`ia`) directly?
- Constructor: `new ih(ry world, double x, double y, double z, dk itemStack)`?
- Fields: itemStack (dk), age (int), delayBeforePickup (int), thrower (string)?
- Tick behaviour: despawn at age 6000? Gravity + bounce physics?
- `a(dk)` — setEntityItemStack? `b()` — getEntityItemStack?
- Java class name: `net.minecraft.src.EntityItem`?

## DataWatcher
[STATUS:IMPLEMENTED]
**Needed for:** `Core/DataWatcher.cs` — per-entity synchronized data store; currently inlined as
`_entityFlags` (byte, index 0) and `_airSupply` (short, index 1) stubs in `Core/Entity.cs`.
**Questions:**
- Obfuscated class name? (suspected `cr`)
- How are entries registered? `addObject(int id, Object value)` with type code?
- How are entries read/written? `getWatchableObjectByte(int id)` / `updateObject(int id, Object)`?
- Is the underlying storage an ArrayList or HashMap?
- Which data types are supported? (byte, short, int, float, string, ItemStack, ChunkCoordinates?)
- How does the dirty flag work for network sync?
- Java class name: `net.minecraft.src.DataWatcher`?

## Item
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Item.cs` — base class for all items; required to un-stub `ItemStack.GetItem()`,
`ItemStack.GetMaxStackSize()`, `ItemStack.GetMaxDamage()`, and `Block.DropBlockAsItemWithChance`.
**Questions:**
- Obfuscated class name? (suspected `acy`)
- Static registry: `acy.d[]` — item array indexed by item ID? What is the max ID?
- Instance fields: itemID (int)? maxStackSize (int)? maxDamage (int)? iconIndex (int)?
- `e()` → int : getMaxStackSize?
- `c()` → int : getMaxDamage (0 for undamageable)?
- Is there a `setMaxDamage(int)` builder method?
- Any subclasses needed immediately (ItemBlock, ItemFood, ItemTool, ItemArmor)?
- Java class name: `net.minecraft.src.Item`?

## LivingEntity
[STATUS:IMPLEMENTED]
**Needed for:** `Core/LivingEntity.cs` — entity subclass with health, armor, AI; required by
`ItemStack.DamageItem(int, nq)` parameter type and as base for Player and mobs.
**Questions:**
- Obfuscated class name? (suspected `nq`)
- Does it extend `ia` (Entity) directly?
- Health fields: `bH` = health (float)? `bJ` = maxHealth? Or int-based?
- Is there a DataWatcher entry for health?
- Key methods: `a(pm, float)` (attackEntityFrom — apply damage)? `bO()` (getHealth)?
  `a(float)` (heal)? `bQ()` (getMaxHealth)?
- AI tick: does the base `nq` drive pathfinding or is that in subclasses?
- Java class name: `net.minecraft.src.EntityLiving`? (obf: `nq`)
