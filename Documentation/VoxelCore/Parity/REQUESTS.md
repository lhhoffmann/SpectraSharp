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

## BlockRegistry
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Block.cs` static initializer — `Block.BlocksList[id]` is null for every ID
except air (0). Physics, collision, light, and random-tick all fail silently for any block
without a registered instance. This is the single highest-priority gap.

**Questions:**
- Full static field list: for every ID from 1 to ~126, what is:
  - The obfuscated field name (t, u, v, …)
  - The Java block class (e.g. `yy` base or subclass like `jb`, `qi`, `aip`, …)
  - The Material passed to the constructor (`wu` constant letter)
  - The StepSound passed (obf letter b–j)
  - Any builder calls chained on the instance (hardness, resistance, light, opacity, name)
- Which IDs are deliberately left null (air-like gaps, e.g. ID 6 missing between `z`=7 and `A`=8)?
- Do blocks 1–20 match the IDs in the partial list from Block_Spec §9?
  (stone=1, grass=2, dirt=3, cobble=4, planks=5, sapling=6?, bedrock=7, water=8/9, lava=10/11 …)
- Which block IDs use the base `yy` class vs. a named subclass?
- Does the static block call `ny.b()` at the end — can it be safely stubbed as a no-op?

**Expected deliverable:** `Specs/BlockRegistry_Spec.md` with a complete table:
`ID | field | class | material | sound | hardness | resistance | light | opacity | name | notes`

---

## EntityPlayer
[STATUS:IMPLEMENTED]
**Needed for:** `Core/EntityPlayer.cs` — the player entity. Required for:
- `Block.getHardness(vi var1)` (mining speed)
- `ItemStack.damageItem(int, nq)` (player is the entity taking damage)
- Any future block-placement / block-breaking logic
- Camera position (eye height), inventory reference

**Questions:**
- Obfuscated class name? (suspected `vi`)
- Superclass: `nq` (LivingEntity) directly?
- Unique fields on the player:
  - Inventory reference: `InventoryPlayer` type, obf field name?
  - Eye height: is it 1.62F above feet? Field name?
  - `onGround` flag (also on Entity base — does player override)?
  - Creative/survival mode flag?
  - `foodLevel`, `foodSaturation`, `foodExhaustionLevel` — on player or sub-object?
- Key methods:
  - `b(yy block)` → boolean : canHarvestBlock — which tools can harvest which materials?
  - `a(yy block)` → float   : getMiningSpeed — tool efficiency multiplier
  - `l()` / `m()` — is there an attack range or reach-distance constant?
  - Constructor signature: `vi(ry world, String username)`?
- Does `vi` extend `nq` directly or is there an intermediate `EntityHuman`?
- What is the player's default AABB size (width/height)?

**Expected deliverable:** `Specs/EntityPlayer_Spec.md`

---

## ChunkProviderGenerate
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ChunkProviderGenerate.cs` — replaces the flat `DebugChunkLoader` with
real procedural terrain. Required before the world looks like Minecraft.

**Questions:**
- Obfuscated class name? (suspected `amu` or similar)
- Which noise generators are used?
  - How many octave Perlin layers for base terrain, hills, and scale?
  - Are there separate `NoiseGeneratorOctaves` (obf: unknown) instances for terrain, surface, beach?
- Height-map formula: how are the three noise values combined into a final column height?
- Biome influence: does ChunkProviderGenerate call into `WorldChunkManager` for biome data?
  If yes, what data does it request (temperature, rainfall, height modifier)?
- Surface builder: what layer of blocks is placed on top of solid terrain?
  (grass on top, dirt 3–4 deep, stone below — configurable per biome?)
- Cave generation: is it a separate class (`MapGenCaves`)? Same chunk provider method?
- Ore generation: how many ore veins per chunk, which IDs, min/max Y?
- Structure generation (trees, lakes, dungeons): which are generated in `populate()` vs.
  the main generation pass?
- What is the chunk generation method signature: `a(int chunkX, int chunkZ)` → `Chunk`?

**Expected deliverable:** `Specs/ChunkProviderGenerate_Spec.md`

---

## IInventory
[STATUS:IMPLEMENTED]
**Needed for:** `Core/IInventory.cs` — interface shared by all inventory holders (player,
chest, furnace, crafting table). Required before `InventoryPlayer` or any container can exist.

**Questions:**
- Obfuscated interface/class name?
- Full method list with signatures and semantics:
  - `getSizeInventory()` → int
  - `getStackInSlot(int slot)` → ItemStack?
  - `setInventorySlotContents(int slot, ItemStack?)`
  - `getInventoryName()` → String
  - `isInventoryEmpty()` → boolean
  - Any other methods?
- Is there a companion `IInventoryListener` or callback interface for slot-change events?

**Expected deliverable:** `Specs/IInventory_Spec.md`

---

## InventoryPlayer
[STATUS:IMPLEMENTED]
**Needed for:** `Core/InventoryPlayer.cs` — the player's 36-slot hotbar+main inventory
plus 4 armor slots. Required by `EntityPlayer`.

**Questions:**
- Obfuscated class name?
- Slot layout: 9 hotbar slots (0–8) + 27 main (9–35) + 4 armor (36–39)?
  Or different ordering?
- `mainInventory` array: size 36? `armorInventory`: size 4?
- `currentItem` field: int index into hotbar (0–8)?
- `getCurrentItem()` → ItemStack? : returns `mainInventory[currentItem]`?
- `addItemStackToInventory(ItemStack)` → boolean : tries to stack, then finds empty slot?
- `consumeInventoryItem(int itemId)` → boolean : removes one item of that ID?
- `getFirstEmptyStack()` → int : slot index or -1?
- `changeCurrentItem(int direction)` — cycles hotbar with scroll wheel?
- Drop logic: `dropCurrentItem()` drops the held item as EntityItem?

**Expected deliverable:** `Specs/InventoryPlayer_Spec.md`

---

## ItemBlock
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ItemBlock.cs` — the item that represents a placeable block in inventory.
Every solid block has a corresponding ItemBlock registered in `Item.ItemsList`.
Required for block-placement logic and for the static initializer `uw` stubs in Block's
static block.

**Questions:**
- Obfuscated class name? (suspected `uw`)
- Does it extend `acy` (Item) directly?
- Constructor: `new uw(int itemId)` where `itemId = blockId - 256`? Or `new uw(yy block)`?
- How does it map back to the block ID? Field `itemID + 256`?
- `placeBlockAt(ItemStack, Player, World, x, y, z, face, hitX, hitY, hitZ)` → boolean : places the block?
- Does it override `getIconFromDamage` to use the block's texture?
- Does it call `Block.canBlockStay` before placing?

**Expected deliverable:** `Specs/ItemBlock_Spec.md`

---

## ConcreteBlocks
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/` — concrete Block subclasses with custom tick, physics, or
drop logic. The base `Block` class handles all defaults; these are the overrides.

**Needed subclasses (one spec per class is fine, or combine into one file):**

### BlockGrass (`jb`, ID 2)
- Tick logic: spreads to adjacent dirt? Which conditions prevent spread (light, opaque above)?
- When does it convert back to dirt (opaque block placed on top)?
- Face texture selection: confirmed top=0, side=3, bottom=2 from TerrainAtlas spec.
  Does `jb.a(int face, int meta)` match this exactly?
- Does `jb` have a `canBlockStay` override?

### BlockSand / BlockGravel (gravity blocks)
- Obfuscated class names?
- Do they extend a shared `BlockFalling` base?
- Tick behaviour: check block below, if air → `onBlockAdded` schedules a tick → falls as entity?
- When falling: spawned as `EntityFallingSand`? Or block just teleports down?
- `EntityFallingSand` needed or can it be stubbed initially?

### BlockLog (`aip`, ID 17)
- Multi-face confirmed: top/bottom = index 21, sides = 20/116/117 per meta.
- Does the log have a tick or is it purely static?
- Any special drops (drops itself)?

### BlockLeaves (`qo`, ID 18)
- Decay logic: how many ticks until leaves decay without adjacent log?
- Light opacity: 1 (not fully opaque) — confirmed?
- IsOpaqueCube = false?
- Drop logic: drops saplings with chance, apples with chance on oak?

### BlockFluid — Water (`aam`, IDs 8/9) and Lava (`aam` subclass, IDs 10/11)
- Can be stubbed initially as non-ticking, non-spreading?
- Minimum needed for IBlockAccess.isWet to work (water blocks → wet = true)?
- Material already set (`materialWater` / `materialLava`)?

**Expected deliverable:** `Specs/ConcreteBlocks_Spec.md`

---

## BiomeGenBase
[STATUS:IMPLEMENTED]
**Needed for:** `Core/BiomeGenBase.cs` — biome definitions with temperature/rainfall values
used to look up grass and foliage color from `grasscolor.png` / `foliagecolor.png`.

Currently the biome tint is hardcoded to `(72, 181, 24)` in `Bridge/Overrides/BlockBase.cs`.
The biome system is needed to make different biomes look different.

**Questions:**
- Obfuscated class name for BiomeGenBase? (`ha`?)
- Static biome list: how many biomes? Which IDs?
- Fields per biome: `temperature` (float), `rainfall` (float), biome name?
- `a(double temp, double rainfall)` → int : returns packed RGB grass color from a lookup table?
  Is the lookup table a 256×256 image (`grasscolor.png`) or a formula?
- `b(double temp, double rainfall)` → int : foliage color (from `foliagecolor.png`)?
- Are these color images in terrain.png or separate files in the JAR?
- Are grasscolor.png / foliagecolor.png standard Java Edition files?
- Does Chunk store biome ID per XZ column? If yes, what array? (byte[256]?)

**Expected deliverable:** `Specs/BiomeGenBase_Spec.md`

---

## TerrainAtlas
[STATUS:IMPLEMENTED]
**Needed for:** `Bridge/Overrides/SimpleBlocks.cs` — every block's `TextureIndex` must match the
actual tile position in terrain.png, otherwise blocks render with wrong or gray textures.

**Problem observed:**
- `GrassBlock` TextureIndex=0 → renders gray (assumed grass_top, but clearly wrong)
- `LeavesBlock` TextureIndex=52 → always rendered gray (wrong index or transparent tile)
- Other blocks may also be off — no ground truth yet for this game version's atlas layout

**Questions:**
- Extract `terrain.png` from the game JAR and document the tile index for every block
  we currently use. Tile index formula: `col = index % 16`, `row = index / 16`, each tile 16×16 px.
- Correct indices needed for (current guess in parentheses):
  - grass_top (0), grass_side (3), dirt (2), stone (1)
  - wood_log_side (20), leaves (52), sand (18), gravel (19)
  - gold_ore (32), iron_ore (33), coal_ore (34)
  - planks (4), cobblestone (16), bedrock (17), glass (49)
- Are leaves/glass tiles partially transparent in the PNG (alpha channel)?
  If so, how should the renderer handle cutout-alpha blocks?
- Is terrain.png a standard RGBA PNG or palette-indexed?

**Expected deliverable:** `Specs/TerrainAtlas_Spec.md` with a verified index table for all blocks above.

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

---

## WorldGenTrees
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/WorldGenTrees.cs` — tree generation called by `ChunkProviderGenerate.PopulateChunk()`.
ChunkProviderGenerate_Spec §10 lists trees as a decoration item but the generator class itself is unspecced.
Without this, generated chunks have no trees.

**Questions:**
- Obfuscated class name? (spec §10 references `fc` for large trees, `adp` for big trees, `e.a(rand)` for standard — is `fc` the primary tree generator?)
- What block IDs does a standard tree place? Log (17) trunk + Leaves (18) canopy?
- Tree height: is there a min/max trunk height? (suspected 4–7 blocks?)
- Canopy shape: how are leaves placed — sphere, cross-pattern, or layer-by-layer box?
- `Generate(IWorld, Random, int x, int y, int z)` signature — same as `WorldGenMineable`?
- Does it check for space before placing (abort if blocked)?
- Does it check that the block below is dirt/grass before placing?
- What is the `fc` "size 7" parameter from the spec — tree height base?
- How does `adp` (big tree) differ from `fc` (standard tree)?

**Expected deliverable:** `Specs/WorldGenTrees_Spec.md`

---

## LightPropagation
[STATUS:IMPLEMENTED]
**Needed for:** `Core/World.cs` — `PropagateLight()` BFS stubs (quirk 3 in World_Spec).
Currently every `SetBlock` call has two TODO comments for sky-light and block-light BFS.
`GetBrightness()` returns the emitted light only — actual world lighting is broken.
`BlockGrass.BlockTick` approximates light with `IsOpaqueCubeArr` instead of real levels.

**Questions:**
- Sky-light propagation: which class drives it? Is it `World.updateLightByType(EnumSkyBlock, x, y, z)`?
  - How does the initial fill work when a chunk is first loaded — full column sky fill?
  - What triggers re-propagation: only `SetBlock` calls, or also block metadata changes?
  - Is sky light stored per-block as a nibble (0–15) in `Chunk.skylightMap` (NibbleArray)?
  - Exact BFS rule: each propagation step subtracts `max(1, opacityOfCurrentBlock)`?
  - How is the "sky obstruction" handled — once a block is hit, all blocks below it get sky-light 0?
- Block-light propagation:
  - Same BFS as sky, seeded by `Block.lightValue[]` (emitted light)?
  - How does removal work — "dark BFS" to pull back light, then re-flood from remaining sources?
  - Does the propagation use a `LinkedList<long>` queue with packed positions?
- Which Chunk fields hold light data? (`skylightMap` / `blocklightMap` both as `NibbleArray`?)
- `World.getSkyBlockTypeBrightness(EnumSkyBlock type, x, y, z)` — what is the full formula?
  - Does it clamp at 0? Does it apply the celestial angle multiplier for sky (day/night)?
- `World.getBlockLightValue_do(x, y, z, useNeighboursForEmpty)` — what does the boolean do?
- `World.getBrightness(x, y, z)` — final formula: `max(skyLight, blockLight)` → `WorldProvider.brightnessTable[]`?
- Does Chunk have a `isLightPopulated` flag separate from `isTerrainPopulated`?
- EnumSkyBlock enum: values? (`SKY` and `BLOCK`?)

**Expected deliverable:** `Specs/LightPropagation_Spec.md` — full BFS algorithm, Chunk light storage, World query methods, update triggers.

---

## BlockFluid
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockWater.cs` + `Core/Blocks/BlockLava.cs` — water and lava currently
exist as static blocks (IDs 8/9 and 10/11) but never spread. No flow levels, no source detection.
All water in generated terrain is `still_water` (ID 9) that never moves.

**Questions:**
- Class hierarchy: is there a `BlockFluid` base (`aam`?) with `BlockWater` and `BlockLava` subclasses?
- Flow level (meta): 0 = source, 1–7 = flowing (7 = farthest)? Or reversed (0 = farthest)?
  - How does meta 8 (falling bit) work? Is bit 3 of meta set for vertically falling water?
- Spreading algorithm — exact tick logic:
  - Does it scan all 4 horizontal neighbours for lower flow levels?
  - Does it prefer flowing straight down over spreading laterally?
  - What triggers a tick: `scheduleBlockUpdate` on placement, or `Block.onNeighborBlockChange`?
  - How many ticks between spreads? (Water: 4? Lava: 20-30?)
- Source block creation: do 2 non-falling water sources adjacent to an air block create a new source?
  - Exact rule for this "infinite water" mechanic?
- Lava differences:
  - Tick rate (much slower — what multiple)?
  - Sets adjacent flammable blocks on fire?
  - Water+lava = cobblestone (or obsidian if lava source)?
- Block IDs: flowing water = 8, still water = 9, flowing lava = 10, still lava = 11 — confirmed?
  - Or is 8 = still, 9 = flowing?
- Does `BlockFluid` override `getBoundingBox` to return null (non-solid)?
- `BlockFluid.isOpaqueCube()` — false? `renderAsNormalBlock()` — false?
- Does the fluid tick schedule itself on `onBlockAdded`?
- Water `canDisplace` which materials (replaces fire, plants, but not stone)?

**Expected deliverable:** `Specs/BlockFluid_Spec.md` — full tick algorithm, spread rules, source detection, water+lava interactions, lava fire spread.

---

## BiomeDecorator
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ChunkProviderGenerate.PopulateChunk()` — currently only places ores and trees.
The vanilla decoration sequence also places: flowers, tall grass, dead bushes, mushrooms, reeds,
cacti, sand/clay disc patches, water/lava springs, pumpkins. Without this, chunks look empty.
The exact RNG consumption order matters for reproducibility — one wrong call breaks all others.

**Questions:**
- Full `b()` (decorate) method sequence in `ql` (BiomeDecorator) — exact order of every decoration call, including the arguments to each generator?
- Sand disc patches (`fc`): at what Y, how many per biome? Which biomes get sand (beach biomes vs rivers)?
- Clay disc (`adp`): same questions — Y level, count, which biomes?
- Flowers: which block IDs? Dandelion=37, rose=38? How many per chunk per biome?
- Tall grass: block ID 31, meta 1? How many per chunk? Which biomes skip it (desert)?
- Dead bushes: ID 32, meta 0? Desert only?
- Mushrooms (red/brown): IDs 39/40? How many per chunk?
- Reeds (sugar cane): ID 83? How many, and what placement rules (adjacent water)?
- Cacti: ID 81? Desert only, on sand?
- Water springs: ID 49 (spring flowing water)? Where do they spawn (underground Y range)?
- Lava springs: same range or different?
- Pumpkins: ID 86? How rare?
- Snow/ice generation: is there a separate pass that freezes water and places snow at surface level
  for cold biomes? (The `we.java` = SpawnerAnimals confusion — what actually does the snow/ice pass?)
- For each decoration: what is the `BiomeDecorator` field name (obf), how many per chunk, Y range?
- `a(int x, int z, ry world)` — sets origin, calls `b()` — confirmed?

**Expected deliverable:** `Specs/BiomeDecorator_Spec.md` — complete ordered decoration sequence with all field names, counts, Y ranges, and per-biome variations.

---

## WorldSave
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldSave/` — world persistence. Every restart currently regenerates the
world from scratch (seed is deterministic so terrain is the same, but placed blocks / entities /
time / inventory are lost). This is the single biggest gap for playability.

**Questions:**
- Region file format: `.mcr` (Anvil pre-cursor) or `.mca`? For 1.0, which format?
  - Header: 4096-byte sector table? Each entry = sector offset (3 bytes) + sector count (1 byte)?
  - Chunk data: 4-byte length + 1-byte compression type + zlib-compressed NBT?
- Level.dat: which fields are stored?
  - `LevelName`, `RandomSeed` (long), `SpawnX/Y/Z` (int), `Time` (long), `LastPlayed` (long)?
  - Is it NBT wrapped in a root `Data` compound?
- Chunk NBT structure:
  - Root tag name? (`Level`?)
  - `xPos` (int), `zPos` (int) — chunk coordinates?
  - `Blocks` — byte[32768] or byte[16×128×16]?
  - `Data` (metadata nibble array), `BlockLight`, `SkyLight` (both nibble arrays)?
  - `HeightMap` — int[256] or byte[256]?
  - `Entities` — list of entity compound tags?
  - `TileEntities` — list of tile entity compound tags?
  - `LastUpdate` (long), `TerrainPopulated` (byte)?
- NBT format: is there a Java NBT library we can port, or do we implement our own binary reader/writer?
  - Tag types: 0=END, 1=BYTE, 2=SHORT, 3=INT, 4=LONG, 5=FLOAT, 6=DOUBLE, 7=BYTE_ARRAY, 8=STRING, 9=LIST, 10=COMPOUND?
- Player data: stored in `level.dat`? Or a separate `players/<name>.dat` file?
- Which fields on EntityPlayer are persisted (inventory, health, position, XP)?

**Expected deliverable:** `Specs/WorldSave_Spec.md` — full region file layout, NBT binary format, chunk compound structure, level.dat layout, player data. Include byte-level layout for region header and chunk sector encoding.

---

## MapGenRavine
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/MapGenRavine.cs` — ravine carver. The `rf` class is already
referenced in `ChunkProviderGenerate_Spec` but was not included in the `MapGenCaves` spec.
Together, caves + ravines complete underground world gen.

**Questions:**
- Obfuscated class name: `rf` — confirmed?
- Does it extend `bz` (MapGenBase) and use the same 17×17 source-chunk scan?
- How does a ravine differ from a cave:
  - No branching?
  - Much taller cross-section (deep cuts instead of round tunnels)?
  - Fixed horizontal direction (straighter)?
  - Different probability than caves?
- Specific parameters vs MapGenCaves:
  - Ravine length (totalSteps range)?
  - Cross-section shape: tall and narrow vs round?
  - Vertical diameter multiplier?
  - Same water-abort rule as caves?
  - Same lava-at-Y<10 rule?
  - Same grass surface restoration?
- Count per source chunk: how many ravines? Much rarer than caves?
- Are there floor/ceiling guards different from cave's `-0.7` floor guard?

**Expected deliverable:** `Specs/MapGenRavine_Spec.md` — full segment algorithm, how it differs from MapGenCaves, probability and count.

---

## BlockFire
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockFire.cs` — fire spreading. Currently fire (ID 51) is a static
block with no tick behavior. Lava should ignite adjacent flammable blocks. Fire should spread
and burn out.

**Questions:**
- Obfuscated class name for BlockFire? (`ij`?)
- Flammability table: is it a static `int[]` array indexed by block ID?
  - Two separate arrays: one for "chance to catch fire" (ignite), one for "chance to burn away"?
  - What are the values for key blocks: planks (wood), leaves, wool, log, fence, bookshelf?
- Tick logic:
  - How often does a fire block tick?
  - Does it check all 6 neighbours + above for flammable blocks and attempt to spread?
  - Formula: `rand.nextInt(300)` < `flammability[neighborId]` → ignite?
  - Does the fire block decrement an "age" counter and eventually die (burn out)?
  - Max spread age cap?
- `BlockFire.onBlockAdded`: does it schedule itself with `scheduleBlockUpdate(delay=30)`?
- Does fire spread upward more aggressively than horizontal?
- Does rain prevent fire spread?
- Block IDs for neighbor checks: does fire check all 6 faces of each adjacent block?
- What does fire place on a flammable block: ID 51 at the flammable block position, or adjacent?
- Lava-to-fire: does `BlockLava` handle igniting adjacent blocks in its own tick, or does BlockFire handle it?
- `isOpaqueCube()` = false, `renderAsNormalBlock()` = false?
- Does fire have an age stored in metadata (0–15)?

**Expected deliverable:** `Specs/BlockFire_Spec.md` — flammability tables, tick algorithm, spread formula, age/burnout, lava ignition, rain cancellation.

---

## ChunkProviderServer
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ChunkProviderServer.cs` — the server-side `IChunkLoader` that wires together
disk persistence (`DiskChunkLoader`) and terrain generation (`ChunkProviderGenerate`).
Currently the engine uses these two independently and neither saves nor loads from disk.
WorldSave is entirely dead code until this class exists.

**Obfuscated class:** `ej` — referenced in World_Spec.md §11 (open question 4) as the
`IChunkLoader` implementation used in server-mode play.

**Questions:**

Chunk cache:
- In-memory cache structure: `HashMap<Long, zx>` with key formula `chunkX | (chunkZ << 32L)`?
- Is there a separate `ArrayList<zx>` for iteration (e.g. random-tick, unload sweep)?

GetChunk algorithm (`b(int x, int z)`):
- Step 1: return from cache if present?
- Step 2: try `gy.a(world, x, z)` (DiskChunkLoader) — null means not saved yet?
- Step 3: if null, call `xj.b(x, z)` or `xj.a(x, z)` (generate raw terrain)?
- Step 4: population/decoration — called immediately after generation, or deferred until all 8 neighbours exist?
  - Java world gen populates only when the chunk AND all 8 surrounding chunks are generated. Confirm this?
  - Which method: `xj.a(world, x, z)` triggers decoration?
- Step 5: any post-load/post-generate call on the chunk itself (e.g. `chunk.i()`, `chunk.e = true`)?

Dirty tracking and save loop (`a()` tick method):
- How is "dirty" tracked: flag on chunk (`chunk.q` = IsModified), or a separate set in `ej`?
- How many chunks are saved per tick call? Budget per tick, or save all pending?
- Is the save loop in `a()` at all, or triggered externally (e.g. from MinecraftServer)?

Auto-save:
- Is there an auto-save tick counter in `ej` itself?
- What triggers level.dat save (`nh.a(si)`)? Same counter?
- Exact auto-save interval in ticks?

Chunk unloading (SP):
- Does `ej` ever unload chunks in single-player? If yes: what triggers it (player distance threshold)?
- If no unloading in SP: just confirm that and note any unload method stubs.

IsChunkLoaded (`c(int x, int z)`):
- Just a cache contains-key check?

Startup:
- Does `ej` pre-generate a spawn area on first load? If yes: radius and which thread?

**Expected deliverable:** `Specs/ChunkProviderServer_Spec.md` — full `ej` analysis: cache fields,
GetChunk algorithm (load vs generate vs populate ordering), dirty/save-tick logic,
auto-save trigger and interval, unload policy.

---

## EntityNBT
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Entity.cs` (`SaveToNbt` / `LoadFromNbt`) and `Core/WorldSave/DiskChunkLoader.cs`
(entity list in chunk NBT). Currently `Entity.SaveToNbt()` returns false — no entities survive
a restart. Required for dropped items, mobs, and paintings.

**Obfuscated class:** `ia` — base entity. Methods: `c(ik tag)` = write, `d(ik tag)` = read.
Entity factory: `afw` (EntityList) — method `a(ik tag, ry world)` creates entity from "id" tag.

**Questions:**

Base entity fields written by `ia.c(ik)`:
- `"Pos"` — TAG_List of 3 TAG_Double (x, y, z)?
- `"Motion"` — TAG_List of 3 TAG_Double?
- `"Rotation"` — TAG_List of 2 TAG_Float (yaw, pitch)?
- `"FallDistance"` — TAG_Float?
- `"Fire"` — TAG_Short (fire ticks)?
- `"Air"` — TAG_Short?
- `"OnGround"` — TAG_Byte (0/1)?
- `"id"` string — written by base or subclass?
- Is the entity integer ID (runtime-only) serialized at all, or omitted?
- Rider: is there a recursive `"Riding"` TAG_Compound for the mounted entity?
- DataWatcher: serialized or reconstructed on spawn?

EntityItem (`ih`) specific:
- `"Item"` — TAG_Compound (ItemStack NBT)?
- `"Health"` — TAG_Short?
- `"Age"` — TAG_Short (despawn counter; 6000 max)?
- `"PickupDelay"` — TAG_Short?

ItemStack NBT format (used in EntityItem, inventory, chest, etc.):
- `"id"` — TAG_Short (block/item ID)?
- `"Count"` — TAG_Byte (stack size)?
- `"Damage"` — TAG_Short (metadata / durability damage)?

EntityList (`afw`) factory:
- Full "id" string → obfuscated class table. Needed at minimum:
  - Passive: Pig, Sheep, Cow, Chicken, Squid, Wolf
  - Hostile: Zombie, Skeleton, Spider, Creeper, Ghast, Slime, PigZombie, Enderman, Silverfish, CaveSpider, Blaze
  - Other: Item (`ih`), Arrow, Snowball, Painting, Boat, Minecart, FallingSand (`hz`), PrimedTnt
- What happens for unknown "id": returns null?

**Expected deliverable:** `Specs/EntityNBT_Spec.md` — `ia.c`/`ia.d` field-by-field list;
EntityItem compound; ItemStack format; EntityList "id" string table with obfuscated class names.

---

## TileEntity
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldSave/DiskChunkLoader.cs` (TileEntities list) and future block-interaction
specs. Currently TileEntities are written as an empty list. Needed for chest/furnace/sign
content to persist across restarts.

**Obfuscated classes:**
- `bq` — base TileEntity
- `ba` — static factory (TileEntityRegistry / createAndLoadEntity)
- Specific TEs for 1.0: `jb` (chest), `li`/`fz` (furnace lit/unlit?), `ad` (sign), `br` (dispenser), `gu` (mob spawner)

**Questions:**

Base TileEntity (`bq`):
- Fields: world ref, x, y, z, what else? A `tileEntityId` int or string?
- `bq.c(ik tag)` write: writes `"id"` string, `"x"` int, `"y"` int, `"z"` int — then subclass adds more?
- `bq.d(ik tag)` read: reads those 4 fields?
- `ba.c(ik tag)` static factory: reads `"id"`, looks up class, instantiates, calls `d(tag)`, returns?
- ID string table: what string each TE uses?
  - Chest, Furnace, Sign, Trap (dispenser?), MobSpawner, RecordPlayer, Piston?
- `bq.e()` = updateEntity tick: is it called from World or from Chunk? Once per tick per TE?

Chest (`jb`, ID 54):
- 27-slot IInventory?
- NBT: `"Items"` — TAG_List of TAG_Compound slots; each slot: `"Slot"` byte, `"id"` short, `"Count"` byte, `"Damage"` short?
- Does the chest TE have a custom name field? (`"CustomName"` string — or is that a later addition?)

Furnace (`li`, IDs 61/62):
- 3-slot inventory (input slot 0, fuel slot 1, output slot 2)?
- `"BurnTime"` short (ticks of current fuel remaining), `"CookTime"` short (0-200)?
- `"Items"` same format as chest?
- Tick logic in `bq.e()`: burn item → smelt item → produce output?
  - Fuel values: which items are fuel and how many ticks?
  - Smelting recipes: are they hardcoded in the furnace class or in a static table?

Sign (`ad`, IDs 63/68):
- 4 text lines: `"Text1"` through `"Text4"` TAG_String?
- Max length per line?

Dispenser (`br`, ID 23):
- 9 slots (same as chest format)?
- Dispensing logic: on redstone pulse, ejects item from random non-empty slot?

Mob Spawner (`gu`, ID 52):
- `"EntityId"` string (entity type to spawn)?
- `"Delay"` short (ticks until next spawn attempt)?
- Spawn radius/conditions?

**Expected deliverable:** `Specs/TileEntity_Spec.md` — `bq` base fields; `ba` factory; ID string table;
full NBT layout for chest, furnace (+ tick logic + fuel/smelting tables), sign, dispenser, mob spawner.

---

## PlayerNBT
[STATUS:IMPLEMENTED]
**Needed for:** `Core/EntityPlayer.cs` (`SaveToNbt` / `LoadFromNbt`) and `Core/WorldSave/SaveHandler.cs`
(the `"Player"` compound in `level.dat`). Currently no player state persists across restarts.

**Obfuscated class:** `vi` — EntityPlayer. Methods: `d(ik tag)` = write, `e(ik tag)` = read.
Called from `SaveHandler.WriteLevelDat` and `WorldInfo` constructor (WorldSave_Spec §6/§7).

**Questions:**

Does `vi.d` call `super.c(tag)` (base entity `ia.c`) first?
- If yes, base entity fields (Pos, Motion, Rotation, Fire, etc.) are inherited from EntityNBT_Spec.

Player-specific fields written by `vi.d`:
- `"playerGameType"` int (0=Survival, 1=Creative)?
- `"Score"` int?
- `"SpawnX"`, `"SpawnY"`, `"SpawnZ"` int — personal bed-respawn coords (only written if set)?
- `"Inventory"` — TAG_List of slot compounds:
  - Slots 0–8: hotbar
  - Slots 9–35: main inventory
  - Slots 100–103: armor (feet=100, legs=101, chest=102, head=103)?
  - Same slot-compound format as chest (Slot byte + id short + Count byte + Damage short)?
- `"SelectedItemSlot"` int (hotbar cursor, 0–8)?
- `"HealF"` float (fractional health regen accumulator)?
- `"Health"` — short or float?
- `"FoodLevel"` int?
- `"FoodExhaustionLevel"` float?
- `"FoodSaturationLevel"` float?
- `"XpLevel"` int, `"XpP"` float (progress 0.0–1.0), `"XpTotal"` int?
- `"Sleeping"` byte, `"SleepTimer"` short?
- `"Dimension"` int (read by WorldInfo constructor to determine which world the player was in)?

Any DataWatcher entries that are serialized?

**Expected deliverable:** `Specs/PlayerNBT_Spec.md` — `vi.d`/`vi.e` complete field list;
inventory slot encoding; confirm super call chain. Cover exactly what is and is not serialized.

---

## EntityMobBase
[STATUS:IMPLEMENTED]
**Needed for:** `Core/EntityMob.cs` — abstract base for all AI-driven entities. Required to fill the
`RegisterId` placeholders in `EntityRegistry` with real types, so that mobs load from and save
to chunk NBT. Currently `CreateFromNbt` and `CreateMobByStringId` silently return null for every
mob because `_stringToType` has no entry for any mob string ID (Zombie, Skeleton, Pig, …).

**Questions:**

EntityMob hierarchy — which abstract layers exist between `nq` (LivingEntity) and a concrete mob?
- Is there an `EntityCreature` (`bx`?) between LivingEntity and passive mobs?
- Is there an `EntityMob` / `EntityMonster` (`by`?) between LivingEntity and hostile mobs?
- What fields does each intermediate class add (e.g. `attackStrength`, `moveSpeed`, pathfinder reference)?

For each concrete mob needed to un-stub `EntityRegistry` (Zombie/54, Skeleton/51, Spider/52,
Creeper/50, Pig/90, Sheep/91, Cow/92, Chicken/93):
- Obfuscated class name
- Superclass chain (e.g. `Zombie extends EntityMob extends LivingEntity`)
- Constructor signature: `new X(ry world)` — always single-arg World?
- Any NBT fields beyond the `nq` base set (Health/HurtTime/DeathTime/AttackTime)?
  For example: Creeper `Fuse` short, Sheep `Sheared` byte, Wolf `Sitting`/`Angry` bytes, etc.

**What is NOT needed for this spec:**
- AI tick logic (pathfinding, targeting, attacking) — stubs are fine
- Rendering (texture, model) — stubs are fine
- Mob drops / XP — stubs are fine

**Expected deliverable:** `Specs/EntityMobBase_Spec.md`
- Abstract class hierarchy diagram (LivingEntity → ? → concrete)
- Field table per intermediate abstract class (obf name, type, default, purpose)
- Per-mob section for each of the 8 mobs above: class name, superclass, extra NBT fields
  (use format: `obf | key | type | default | notes`)
- Constructor signatures

---

## LivingEntityDamage
[STATUS:IMPLEMENTED]
**Needed for:** `Core/LivingEntity.cs` line 190 — `AttackEntityFrom` has a `TODO` for field `ac`
(invulnerability counter). The invulnerability window logic is currently broken: the counter
is always 0, so the "if ac > InvulnerabilityTicks/2" branch is never taken and entities can
be hit for full damage every tick.

**Context:** `LivingEntity_Spec.md` already covers the broad damage flow, but the exact field
mapping for `ac` (the per-tick-decrementing invulnerability counter that decides whether a hit
is absorbed or applies) was marked uncertain. The Coder needs to know:
- Is `ac` the same field as `InvulnerabilityTicks` (`bS`) or a separate counter?
  The spec lists `bS` = invulnerability ticks remaining (set on hit). Is `ac` just `bS`?
  Or is `ac` a different field (e.g. the legacy `entityAge`/`ticksExisted` field)?
- The exact condition: `if (ac > bS / 2)` — is this correct? What does `ac` represent in plain terms?
- `DamageSource` type (`pm`): is it a class or enum? What information does it carry?
  The Coder only needs the constructor/factory methods used in `LivingEntity` itself
  (e.g. `pm.a` for generic/fall damage, not the full API).

**Expected deliverable:** `Specs/LivingEntityDamage_Spec.md`
- Clarify `ac` vs `bS`: are they the same field or two different counters?
- If `ac` is a field of `ia` (Entity base), document it in the Entity field table
- `pm` (DamageSource): obf class name; factory constants/methods used inside `nq.a(pm,float)`;
  `getEntity()` method if it exists (needed for XP/drop attribution); `isDamageAbsolute()` boolean

---

## ItemFood
[STATUS:IMPLEMENTED]
**Needed for:** `Core/FoodStats.cs` is implemented but `FoodStats.FoodLevel` never changes
in-game. Eating requires `ItemFood` (`sv`) to call `FoodStats` methods. Also needed so that
`FoodStats.FoodTickTimer` and exhaustion are ever decremented.

**Questions:**

`sv` (ItemFood):
- Superclass: extends `acy` (Item) directly?
- Fields: `healAmount` int (hunger points restored, 2× half-shanks), `saturationModifier` float?
- `onEaten(dk stack, ry world, vi player)` — method that calls `FoodStats.addStats(heal, sat)`?
- `a(dk, ry, vi)` — is this the use-tick method (called each tick the item is held + right-click)?
  How does the 32-tick eat-animation work: `ItemInUseCount` countdown?
- Which item IDs are `ItemFood` instances? Need at least: bread (ID 297), apple (ID 260),
  cooked pork (ID 320), raw pork (ID 319), cookie (ID 357), steak (ID 364), cooked chicken (ID 366).
  For each: `healAmount` and `saturationModifier`.

`eq` (FoodStats) — missing tick logic:
- `a(vi player)` — the per-tick method: when/how does it decrement `FoodLevel`?
  (Expected: once per tick when `FoodExhaustionLevel >= 4`: subtract 1 from saturation; if
  saturation = 0: subtract 1 from foodLevel; if foodLevel = 0: apply starvation damage)
- `a(int heal, float saturation)` — the eat method: how does it add heal + saturation?
  (Does saturation cap at foodLevel? Does it first restore hunger, then saturation?)
- `b(float exhaustion)` — addExhaustion: just `FoodExhaustionLevel += exhaustion; clamp at 40`?
- Which actions generate exhaustion? (walking, sprinting, jumping, attack, breaking blocks)

**Expected deliverable:** `Specs/ItemFood_Spec.md`
- `sv` field table + `onEaten` + eat-animation trigger method
- Per-food-item table: ID | name | healAmount | saturationModifier
- `eq` tick method full pseudocode
- `eq.a(heal, sat)` full pseudocode (with saturation cap)
- `eq.b(exhaustion)` full pseudocode

---

## BlockSlab
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockSlab.cs` — IDs 44 (single slab) and 43 (double slab).
Currently registered as plain `Block` instances, so collision is a full 1×1×1 cube. A single
slab should have a 1×0.5×1 AABB (bottom half) or 1×0.5×1 shifted up (top half, meta bit 8).

**Questions:**
- Obfuscated class name for the slab block (`xs`?).
- Exact AABB: is the bottom half at y=[0, 0.5] and top half at y=[0.5, 1.0]? Meta bit for top?
- `isOpaqueCube()` — false for single slab (sky light passes over it)?
- `canBlockStay()` override or is it always stable?
- Drop behaviour: single slab drops 1 slab item (ID 44). Double drops 2. Confirmed?
- Texture per face: does meta affect texture (stone/sandstone/cobble/brick variants from metadata bits 0-5)?
- Does the stair/slab hitbox affect which block is placed when right-clicking the top or bottom face?

**Expected deliverable:** `Specs/BlockSlab_Spec.md`
- Obf class name + constructor
- AABB bottom and top variants; meta layout
- isOpaqueCube / renderAsNormal flags
- Drop table (single slab → 1 slab, double slab → 2 slabs)
- Texture atlas layout per metadata variant

---

## BlockStairs
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockStairs.cs` — IDs 53 (wood), 67 (stone), 108 (brick),
109 (stone brick), 114 (nether brick). Currently all are plain `Block`, so players cannot
climb stairs correctly — they are treated as full-block walls.

**Questions:**
- Obfuscated class name (`ahh`?).
- AABB shape: Minecraft stairs are an L-shape (bottom half full + top back half). How is this
  represented — as a single bounding box for collision, or two overlapping boxes?
- Metadata encoding: 4 directions (0=east, 1=west, 2=south, 3=north) × inverted bit (meta 4)?
- Texture source: delegates to the underlying block's texture (e.g. wood stairs → planks texture)?
- `isOpaqueCube()` — false?
- `canBlockStay()` — always stable?
- Does `onEntityCollidedWithBlock` affect movement (like a ramp)?

**Expected deliverable:** `Specs/BlockStairs_Spec.md`
- Obf class name + constructor signature
- AABB per direction/orientation
- Metadata layout (direction + inversion bits)
- Texture mapping per face
- isOpaqueCube / renderAsNormal flags

---

## BlockFence
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockFence.cs` — IDs 85 (wood fence), 113 (nether brick fence).
Currently plain `Block` (full 1×1×1 cube). Fences should be 1×1.5×1 for collision and connect
to adjacent fence/solid blocks visually and physically.

**Questions:**
- Obfuscated class name (`nz`?).
- AABB: is the collision box 1×1.5×1 (height 1.5 to prevent jumping over)?
  Or is it smaller (0.375 half-width)?
- Connectivity: does the block connect to adjacent solid blocks and other fence blocks?
  Is connectivity purely visual (render) or does the AABB change per connection?
- `isOpaqueCube()` — false?
- `canPlaceBlockAt` / `canBlockStay` constraints?
- Fence gate (ID 107) — same class or different? Does it connect to fences?

**Expected deliverable:** `Specs/BlockFence_Spec.md`
- Obf class name + AABB dimensions (exact floats)
- Connectivity rules (which adjacent blocks connect)
- isOpaqueCube flag
- Fence gate interaction (ID 107)

---

## BlockDoor
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockDoor.cs` — IDs 64 (wood door), 71 (iron door).
Currently plain `Block`. Doors occupy two block positions (bottom and top half), have open/closed
state, and have a directional face. Iron doors can only be opened by redstone.

**Questions:**
- Obfuscated class name (`uc`?).
- Metadata encoding: bottom half: bits 0-1 = facing (0=east,1=south,2=west,3=north), bit 2=isOpen?
  Top half: bit 3=isTopHalf, bit 4=hingeRight?
- AABB when closed: thin (0.1875 wide) slab along one face edge?
- AABB when open: rotated 90°?
- `onBlockActivated` — toggles open state for wood door? Iron door only via redstone?
- When placed: places bottom at clicked position and auto-places top at y+1?
- Drop: drops 1 door item regardless of which half is broken?
- `canBlockStay` — requires solid block below bottom half?

**Expected deliverable:** `Specs/BlockDoor_Spec.md`
- Obf class name + metadata layout (full bit table)
- AABB for each state (closed/open × 4 directions)
- onBlockActivated logic (wood vs iron)
- canBlockStay + placement logic
- Drop behaviour

---

## BlockCrops
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockCrops.cs` + `BlockFarmland.cs` — IDs 59 (crops) and 60 (farmland).
Currently plain `Block` stubs. Crops need random-tick growth (stages 0–7 via metadata),
farmland needs moisture tracking and should revert to dirt when dried.

**Questions for crops (`aha`):**
- Obfuscated class name.
- Growth stages: 0–7 via metadata? Stage 7 = fully grown (drops wheat + seeds).
- Random tick growth: what probability / conditions (light level, adjacent water)?
- AABB: tiny box at each growth stage, or no collision at all?
- canBlockStay: requires farmland (ID 60) below?
- Drop on break: stage 7 → 1 wheat (ID 296) + 0–2 seeds (ID 295); other stages → 0–1 seeds only?

**Questions for farmland (`ni`):**
- Obfuscated class name.
- Metadata: 0 = dry, 1–7 = moisture? How does water nearby affect moisture?
  Maximum water range: 4 blocks in any direction?
- Reverts to dirt (ID 3) when: dried out (moisture drops to 0) OR entity walks on it?
- AABB: slightly lower than 1.0 (15/16 height)?
- random tick: decrements moisture; checks for water in 4-block radius to refill.

**Expected deliverable:** `Specs/BlockCrops_Spec.md`
- Obf class names for both
- Crops: AABB per stage, growth probability, drop table per stage
- Farmland: metadata moisture encoding, water-scan radius, revert-to-dirt conditions, AABB

---

## WorldGenDungeon
[STATUS:PROVIDED]
**Needed for:** `Core/WorldGen/WorldGenDungeon.cs` — underground dungeon room generator (`acj`).
Dungeons are small rooms with cobblestone/mossy-cobblestone walls, a mob spawner at the centre,
and 1–2 chests with a fixed 11-slot loot table. Currently not generated (no call site in
`ChunkProviderGenerate`). Required to achieve full 1.0 terrain parity.

**Expected deliverable:** `Specs/WorldGenDungeon_Spec.md`
- Obf class name + constructor
- Room dimensions, wall construction (cobble/mossy-cobble ratio)
- Opening/entrance validity check
- Spawner placement + mob-type table
- Chest count/placement + full loot table (all 11 slots with probability/count ranges)
- Call site in ChunkProviderGenerate (Y range, attempt count)

---

## SpawnerAnimals
[STATUS:PROVIDED]
**Needed for:** `Core/SpawnerAnimals.cs` — passive/hostile/water mob tick-spawner (`we`).
Currently mobs are never spawned into a live world — there is no runtime spawning system.
The spawner runs each tick and maintains caps per creature type.

**Expected deliverable:** `Specs/SpawnerAnimals_Spec.md`
- Obf class name + constructor
- Per-type caps (hostile 70, passive 15, water 5) and the `jf` enum layout
- 17×17 chunk map per player, distance constraints (>24 blocks for spawning)
- Pack spawning algorithm
- Spider Jockey check (1%)
- `initialPopulate` call and biome spawn list structure

---

## ChunkProviderHell
[STATUS:PROVIDED]
**Needed for:** `Core/ChunkProviderHell.cs` — Nether terrain generator (`jv`).
Currently the Nether dimension has no generator — entering the Nether would produce empty chunks.
Required for full dimension support.

**Expected deliverable:** `Specs/ChunkProviderHell_Spec.md`
- Obf class name + field list
- All noise generator fields (7 total; which 2 are dead/state-only)
- Density grid dimensions (5×17×5) + Y-shape cosine + cubic edge-pull formula
- Trilinear interpolation details
- Lava sea level (y<32), surface bedrock + soul sand/gravel/netherrack ceiling zone
- Cave carving (thickness=0.5)
- NetherFortress (`ed`) call site
- Populate: 8 lava pools, fire, 2 glowstone cluster algorithms, mushroom bug (nextInt(1))
