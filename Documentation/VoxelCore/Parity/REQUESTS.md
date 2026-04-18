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
[STATUS:IMPLEMENTED]
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
[STATUS:IMPLEMENTED]
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
[STATUS:IMPLEMENTED]
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

---

## MobAI_PathFinder
[STATUS:IMPLEMENTED]
**Deliverable:** `Specs/MobAI_PathFinder_Spec.md`
**Corrections to request:** Coder's obf guesses were wrong — correct names: PathFinder=`rw`, PathEntity=`dw`, PathPoint=`mo`, PathHeap=`zs`, PathNodeCache=`ob`, ChunkCache=`xk`. `lb`=TexturePack GUI screen; `nb`=a particle entity; `ij` = unknown.
Full spec covers: A* algorithm in `rw`, `ww` AI tick `n()` complete sequence, `zo` target+attack, `fx` breed+follow, `dw` navigation, path waypoint → movement steering. 5 open questions documented.

---

## Explosion
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Explosion.cs` + `Core/Entities/EntityTNTPrimed.cs`
Creeper explosion is referenced in `EntityCreeper` (fuse countdown, `IsPowered`) but no `Explosion` class exists. TNT block (ID 46) is a plain stub in `BlockRegistry`.

**Expected deliverable:** `Specs/Explosion_Spec.md`
- Obf class name and fields
- Sphere ray-cast algorithm: number of rays (1352?), attenuation-per-block, total damage falloff
- Block destruction: which blocks survive (obsidian resistance threshold), drop probability formula
- Entity damage: distance-based damage + knockback formula
- Creeper fuse: `_fuseCountdown` tick sequence, ignite distance, normal vs powered radius (3 vs 6?)
- TNT entity (`abv`?): fields (fuse=80, power=4), lit-by-fire/flame/explosion, drop behavior
- `BlockTNT` (`vm`?, ID 46): `onBlockActivated` / `onNeighborBlockChange` ignite triggers

**Resolution:** `Specs/Explosion_Spec.md` written. Confirmed obf names: `xp`=Explosion, `dd`=EntityTNTPrimed, `abm`=BlockTNT (corrects Coder guesses `abv`/`vm`). Key findings: 1352 rays from 16³ surface-only grid; entity damage uses `f*=2` (doubled power) — max damage = 16P+1; incendiary fire uses non-deterministic local `Random h` (not world RNG); chain-TNT fuse = `nextInt(20)+10`; Creeper power 3 (normal) / 6 (powered), 30-tick threshold, dist 3/7.

---

## ItemTool
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Items/ItemTool.cs` + `Core/Items/ItemSword.cs` + `Core/Items/ItemArmor.cs`
Mining speed is always the base player speed because `EntityPlayer.GetMiningSpeed` has no tool-speed multiplier. Combat damage is missing the sword bonus. Armor reduction in `LivingEntity.AttackEntityFrom` is missing armor-value lookup.

**Expected deliverable:** `Specs/ItemTool_Spec.md`
- Obf class names: ItemTool (`acq`?), ItemPickaxe, ItemAxe, ItemShovel, ItemHoe, ItemSword (`acp`?), ItemArmor (`acr`?)
- `EnumToolMaterial` (`ah`): WOOD/STONE/IRON/DIAMOND/GOLD — harvestLevel, maxUses, efficiency, damage bonus, enchantability table
- `ItemTool.getStrVsBlock(ItemStack, Block)` — returns efficiency if block in `blocksEffectiveAgainst`, else 1.0
- `ItemTool.hitEntity` — damage application + durability decrement
- `ItemSword`: damage bonus per material, `getStrVsBlock` always 1.5
- `EnumArmorMaterial` (CLOTH/CHAIN/IRON/DIAMOND/GOLD): durability/reduction per slot
- `ItemArmor`: `ArmorIndex` (0-3 = helmet/chest/legs/boots), icon texture formula
- Item durability: `ItemStack.damage` decrement path, break threshold, `onItemBreak`
- `EntityPlayer.getMiningSpeed` full formula (tool efficiency x haste/fatigue potion factor)

**Resolution:** `Specs/ItemTool_Spec.md` written. All Coder guesses wrong: real classes = `ads`=ItemTool, `nu`=EnumToolMaterial, `zp`=ItemSword, `adb`=ItemSpade, `zu`=ItemPickaxe, `ago`=ItemAxe, `wr`=ItemHoe, `agi`=ItemArmor, `dj`=EnumArmorMaterial. Key corrections: sword (`zp`) does NOT extend `ads` — extends `acy` directly; sword getStrVsBlock returns 1.5F (not "always 1.5" as spec requested — cobweb is 15.0F); ItemHoe (`wr`) also extends `acy` not `ads`; axe efficiency applies to ALL wood-material blocks not just `bR`; Unbreaking uses world RNG. Full mining speed formula with Efficiency/Haste/Fatigue/water/airborne modifiers documented.

---

## NetherFortress
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/NetherFortress.cs`
`ChunkProviderHell.Populate` has a stub comment `// Pass 4: Fortress outlines — stub (NetherFortress spec pending)`.

**Expected deliverable:** `Specs/NetherFortress_Spec.md`
- Obf class name `ed`, full field list
- Room/corridor piece list: bridge segments, crossing, staircase, nether-wart room, blaze spawner corridor, entrance
- Grid-based placement: periodic grid spacing in chunk coordinates
- Spawn list: `qf` (Blaze?), `jm` (ZombiePigman?), `aea` (unresolved) — full class names + `EnumCreatureType`
- Nether-wart farm placement rules (soul sand + wart blocks)
- Blaze spawner placement and spawn count

**Resolution:** Spec at `Specs/NetherFortress_Spec.md`. 13 piece classes (ac/bw/ui/bl/kf/xr in corridor list; hg/yj/lu/ahw/tr/acs/io in room list) + ld (dead-end terminator) + gc (starting piece extending bw). Spawn: qf=Blaze, jm=ZombiePigman, aea=MagmaCube (NOT unresolved). Placement: 1/3 chance per chunk, seed=(chunkX^(chunkZ<<4))^worldSeed, offset [4,11]. Y=[48,70]. Radius ≤112 blocks. Nether wart farm in `io` class (soul sand + ID 115). Blaze spawner in `kf` (max 2 per fortress).

---

## WorldGenStructures
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/WorldGenMineshaft.cs` + `Core/WorldGen/WorldGenStronghold.cs` + `Core/WorldGen/WorldGenVillage.cs`
`ChunkProviderGenerate` has the comment "Structure generation (villages, strongholds) is not implemented."

**Expected deliverable:** `Specs/WorldGenStructures_Spec.md` (three sections or three separate files)
- **Mineshaft** (`wr`?): corridor-room recursive generator; rail + torch + chest-wagon placement; wooden plank supports; cobweb density; obf class name
- **Stronghold** (`vn`?): large multi-room; portal room; library rooms; iron-bar cells; stone-brick staircase; chest loot table; 3 strongholds per world at radius 640-1152 blocks
- **Village** (`acf`?): street/house/farm/well/church/library/butcher/blacksmith pieces; villager spawn list; call site in ChunkProviderGenerate
- For each: Y range, attempt count, how populate triggers them

**Resolution:** Spec at `Specs/WorldGenStructures_Spec.md`. All Coder class guesses wrong. Real names: `kd`=WorldGenMineshaft (NOT `wr`=ItemHoe), `dc`=WorldGenStronghold (NOT `vn`=StrongholdCorridor-piece), `xn`=WorldGenVillage (NOT `acf`=GUI class).
Mineshaft: nextInt(100)==0 && nextInt(80)<max(|cX|,|cZ|); start=`ns`; piece factory `aez` gives `aba`/`ra`/`id` at 70%/10%/20%; max depth 8, radius 80; cave-spider spawner ~4.3% (not isMain + 1/23); 11-entry chest loot table.
Stronghold: 3 per world; angular placement (2π/3 apart, randomised start angle); distance=(1.25+nextDouble())×32 chunks; 7 valid biomes; start=`kg`; opening piece=`aeh`; stone brick ID 98.
Village: 32-chunk grid; offset nextInt(24) in X and Z; valid biomes sr.c+sr.d (plains+desert); cell RNG=world.x(gX,gZ,10387312); start=`yo`; starting piece=`yp`; returns boolean that suppresses dungeon spawn.

---

## ChunkProviderEnd
[STATUS:IMPLEMENTED]
**Needed for:** `Core/ChunkProviderEnd.cs`
The End dimension (dimension ID 1 in `WorldProvider`) has no chunk generator.

**Expected deliverable:** `Specs/ChunkProviderEnd_Spec.md`
- Obf class name (`io`?)
- End island noise: parameters, floating island shape (sphere-like density function), main island radius
- Obsidian platform placement (emergency spawn at 0, 55, 0)
- Block palette: end stone (ID 121) fill
- Populate: Ender Dragon boss spawn; obsidian pillars + End Crystal placement; no biome decoration
- `BlockEndPortalFrame` (ID 120) + `BlockEndPortal` (ID 119): placement in stronghold, activation condition (all 12 frames with Eyes of Ender)

**Resolution:** Spec at `Specs/ChunkProviderEnd_Spec.md`. Real obf name: `a` (NOT `io`; `io` is NetherWartRoom piece). 5 noise generators (eb octaves): j=16-oct, k=16-oct, l=8-oct, a=10-oct (public), b=16-oct (public). Density grid 3×33×3, 8-cell trilinear interpolation. Island shaping: `var22 = 100.0F - sqrt(x²+z²) * 8.0F`, clamped [-100,30]. Dead code: `var18 = 0.0` (noise b has no effect on output). Palette: pure end stone (yy.bJ.bM=121). Surface pass is a no-op loop. WorldProviderEnd=`ol` (dim=1, fog 0x808080×0.15F). BiomeSky=`uu`; WorldGenEndSpike=`oh` (height[6,37], radius[1,4], obsidian cylinder + EntityEnderCrystal `sf` + bedrock cap). BlockEndPortalFrame=`rl` (ID 120, meta bits 0-1=facing, bit 2=hasEye, AABB 0-0.8125, drops nothing). BlockEndPortal=`aid` (ID 119, TileEntity yg, 1/16 AABB, teleports via player.c(1), self-destructs outside overworld). ItemEnderEye=`aag`: sets meta|4, 12-frame ring (3+3+3+3), activates 3×3 interior with yy.bH.bM. Obsidian platform: `aim.java` places 5×5 floor + 3-high air column for dim==1.

---

## BlockRedstone
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockRedstoneWire.cs`, `BlockRedstoneTorch.cs`, `BlockRepeater.cs`, + simple redstone blocks
`World.IsBlockIndirectlyReceivingPower` is a stub returning false. Blocks 55, 69, 70, 72, 75, 76, 77, 93, 94, 143 are all plain stubs in `BlockRegistry`.

**Expected deliverable:** `Specs/BlockRedstone_Spec.md`
- **BlockRedstoneWire** (`zl`, ID 55): BFS signal propagation (max strength 15, -1/block), cross-connections, texture by power level, dot vs line shape, `canProvidePower`
- **BlockRedstoneTorch** (`wk`, IDs 75/76): power output direction (all faces except support), burnout mechanic (8 flips/60 ticks → goes out)
- **BlockRepeater** (`ahl`, IDs 93/94): 1-4 tick delay (meta bits 0-1), facing (bits 2-3), lock behavior
- **Buttons** (IDs 77/143), **Lever** (ID 69), **Pressure plates** (IDs 70/72): `onActivated` trigger, hold duration, power output
- `World.isBlockIndirectlyGettingPowered` full algorithm: 6-face scan, direct vs indirect power, `getBlockPowerInput` vs `isProvidingWeakPower`

**Resolution:** Spec delivered at `Specs/BlockRedstone_Spec.md` (13 sections).
Real obf names: wire=`kw`, torch=`ku`, torch-base=`bg`, repeater=`mz`, lever=`aaa`, pressure-plate=`wx`, button=`ahv`, ore=`oc`. Coder guesses (`zl`,`wk`,`ahl`,`abr`) were all wrong.
World power query chain: `k(face)`=getStrongPower → `u()`=isStronglyPowered (6-face OR) → `l(face)`=getPower (opaque→u() else weak) → `v()`=isBlockReceivingPower (6-face OR).
Wire propagation: recursive DFS with anti-reentrance flag `a`; dirty HashSet `cb`; attenuation -1/block; 0-crossing notifies neighbours.
Torch burnout: STATIC class-level list `cb` shared across ALL torch instances — vanilla bug, can cross-contaminate.
Repeater delay: meta bits 2-3 = index into {1,2,3,4} × 2 = {2,4,6,8} ticks.
Lever floor: meta 5 (pointing south) or meta 6 (pointing east) — random on placement.
Pressure-plate xb enum: a=all entities (wood ID 72), b=living mobs (stone ID 70), c=players (unused in 1.0).
**ID 143 (wood button) ABSENT in 1.0** — only ID 77 (stone button) exists. Added Beta 1.7+.
Ore-redstone touch-to-glow: touch/walk/click → ID 74; randomTick → ID 73.

---

## BlockPiston
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockPiston.cs` + related
IDs 29 (sticky), 33 (normal), 34 (extension), 36 (moving) are all plain stubs in `BlockRegistry`.

**Expected deliverable:** `Specs/BlockPiston_Spec.md`
- Obf class name (`abr`?): fields `_isSticky`, facing (meta 0-5)
- Push logic: max 12 blocks, unpushable blocks list (bedrock, obsidian, piston head, moving block)
- Pull logic: sticky piston pulls 1 block on retract
- Extension block (`abq`?): face texture, AABB, drops nothing
- Moving block (ID 36, `qz`?): tile entity with stored block ID/meta, facing, progress, isExtending
- Neighbor-change trigger: `onNeighborBlockChange` checks `isBlockPowered` → extend/retract

**Resolution:** Spec at `Specs/BlockPiston_Spec.md`. Real obf names: abr=BlockPiston ✓, acu=BlockPistonExtension (NOT abq), qz=BlockMovingPiston ✓, agb=TileEntityPiston. Push limit = 13 (not 12 — counter loops 0-12). Static `cb` anti-reentrance on abr. isPowered: 12-position check (6 sides + 6 overhead). Sticky pull via double-ahead block check. Animation: +0.5F/tick, 2-tick total. agb NBT saves n (prev progress) not m (current) — potential quirk.

---

## BlockBed
[STATUS:IMPLEMENTED]
**Spec:** `Specs/BlockBed_Spec.md`
**Resolution:** `aab`=BlockBed confirmed. Metadata bits 0-1=facing, 2=occupied, 3=isHead. Static direction array `a={{0,1},{-1,0},{0,-1},{1,0}}`. AABB 9/16 height. onBlockActivated: foot→head redirect via meta bit 3, Nether explosion (`world.a(null,x,y,z,5.0F,true)`) when `world.y.c==true` (WorldProvider.c field, sleeping-disabled). trySleep (`vi.d`): 6 `qy` enum results (a=OK, b=NOT_POSSIBLE_HERE, c=NOT_POSSIBLE_NOW=daytime via `world.l()=skyDarkening<4`, d=TOO_FAR_AWAY, e=OTHER_PROBLEM, f=NOT_SAFE=monster `zo` within 8×5×8). wakeUpPlayer (`vi.a`): restore AABB 0.6×1.8, clear occupied flag, findWakeupPosition 3×3 scan (solid floor+2 air), optionally set spawn. Bed item=`kn`(99)=acy.aZ=bM 355; drops from foot half only. WorldProvider.d()=hasSky (default true; Nether=false). WorldProvider.c=boolean field (sleeping-disabled; default false; Nether sets true).

---

## BlockPortal
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Blocks/BlockPortal.cs` + `Core/WorldGen/PortalTravelAgent.cs`
Nether portal creation and dimension travel are completely absent. `WorldProvider` dimension routing exists but no portal-linking logic.

**Expected deliverable:** `Specs/BlockPortal_Spec.md`
- Obf class name (`mc`?, ID 90): AABB, tick, entity teleport trigger
- Frame detection (`tryToCreatePortal`): minimum 2×3 obsidian frame, both-orientation scan
- `PortalTravelAgent` (`acx`?): find/create matching portal on destination side; search radius (128 overworld / 16 Nether); coordinate scale factor (÷8 / ×8)
- Teleport cooldown (10 s / 200 tick `portalCooldown` on entity)
- `BlockFlintAndSteel` (`ahe`?, item ID 259): `onItemUse` ignites adjacent obsidian frame

**Resolution:** Spec at `Specs/BlockPortal_Spec.md`. Real obf names: BlockPortal=`sc` (NOT `mc`), PortalTravelAgent=`aim` (NOT `acx`), ItemFlintAndSteel=`ou` (NOT `ahe`). BlockPortal extends `aaf` (unknown base). `g()` tryToCreatePortal: 10 obsidian minimum (corners optional), 4×5 frame scan (var7=-1..2, var8=-1..3), places 2×3 portal interior at interior positions. onNeighborChange destroys invalid columns (all blocks in column must be portal or interior). Entity contact: `entity.S()` not direct teleport. `S()` in `vi`: if bY>0 → bY=10; else → bZ=true (bY=20 default, bZ=false). PortalTravelAgent `b()` findPortal radius=128; `c()` createPortal radius=16, 2-phase + emergency at Y=70, builds 4×5 obsidian frame with 2×3 interior; End arrival = 5×5 obsidian floor. ItemFlintAndSteel `ou`: durability 64, always damages 1 per use, places fire on adjacent air; portal ignition indirect via BlockFire→sc.g(). No coordinate scaling found in `aim` — scale likely in WorldServer dimension routing. Coder guesses for class names were all wrong.

---

## SnowIce_Generation
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/WorldGenSnowIce.cs` or inline in `BiomeDecorator`
`BiomeDecorator_Spec.md` open question: is snow/ice a separate pass or decoration step 15? Snow and ice do not appear in cold biomes currently.

**Expected deliverable:** `Specs/SnowIce_Spec.md`
- Which class handles it: separate `WorldGenerator` or inline in `BiomeDecorator.decorate()`?
- Obf method/class name
- Snow layer (ID 78) placement: condition (solid opaque block, biome temp < 0.15, sky-exposed)
- Ice (ID 79): replaces still-water (ID 9) in cold biome if sky-exposed
- `BlockSnow` (`aak`?, ID 78): thin AABB, no collision, drops snowballs, melts at block-light > 11
- `BlockIce` (`aaj`?, ID 79): melts to water at light > 11, slippery friction value (0.98?)

**Resolution:** `Specs/SnowIce_Spec.md` written. Key findings: (1) NOT a WorldGenerator and NOT in BiomeDecorator — inline 16×16 loop at end of `xj.populateChunk` after all decoration; (2) real block classes: `aif`=BlockSnow (corrects guess `aak`), `ahq`=BlockIce (corrects guess `aaj`); (3) ice slipperiness confirmed 0.98F; (4) ice melt threshold = >10 (not >11 — subtract opacity 1); (5) snow has no collision for layers 0-2, collision up to 0.5F for layers 3-7; (6) no snow on ice (explicit guard in `ry.r()`); (7) snow always placed at layer 0 during generation.

---

## ItemRecord_Jukebox
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Items/ItemRecord.cs` + `Core/Blocks/BlockJukebox.cs` + `Core/TileEntity/TileEntityJukebox.cs`
`WorldGenDungeon` loot uses `MusicDisc13Id = 2256` / `MusicDiscCatId = 2257`. These item IDs exist in loot tables but have no `Item.d[]` entries. Jukebox (ID 84) is a plain `BlockBase` stub.

**Expected deliverable:** `Specs/ItemRecord_Jukebox_Spec.md`
- `ItemRecord` (`acb`?): `onItemUse` inserts disc into jukebox; `getRecordNameLocal` → disc name string
- All record item IDs (2256–2267?) with obf field names and display names (13/cat/blocks/chirp/far/mall/mellohi/stal/strad/ward/11/wait)
- `BlockJukebox` (`aas`?, ID 84): `onBlockActivated` = eject disc; `onBlockDestroyed` = eject item
- `TileEntityJukebox`: single `ItemStack` field "RecordItem", write/read NBT

**Resolution:** Spec at `Specs/ItemRecord_Jukebox_Spec.md`. Real obf names: ItemRecord=`pe` (NOT `acb`), BlockJukebox=`abl` (confirmed), TileEntityJukebox=`agc`.
11 discs only (IDs 2256–2266); "wait" ABSENT in 1.0. TileEntity stores int (not ItemStack). Ejection RNG uses world.w (consumes 3 nextFloat calls in X/Y/Z order). Pickup delay = 10 ticks (not standard 40). Tooltip = "C418 - <name>". World event 1005: data=recordId → play, data=0 → stop.

---

## OpenQuestion_AcyAV
[STATUS:IMPLEMENTED]
**Resolved:** `acy.aV = new xv(95)` — class `xv` = ItemDye; item ID = `bM = 256 + 95 = 351`. Name "dyePowder".
Loot slot 10 drops `new dk(acy.aV, 1, 3)` = 1× Dye, damage 3 = **Cocoa Beans** (brown dye).
Music disc open question also resolved: `acy.bB.bM = 2256` ("13") and bM+1 = 2257 ("cat").
See `WorldGenDungeon_Spec.md §9` for the full answers. `WorldGenDungeon_Spec.md §9` updated from "Open Questions" to "Resolved Questions".

---

## PortalTravelAgent
[STATUS:IMPLEMENTED]
**Needed for:** `Core/WorldGen/PortalTravelAgent.cs` + `Core/EntityPlayer.cs` (`TravelToDimension`)
`BlockPortal` is implemented but dimension travel is a stub. `EntityPlayer.TravelToDimension(int)` throws nothing — it simply does nothing. The portal-linking and spawn-platform logic is missing entirely.

**Questions:**
- Obf class name for PortalTravelAgent (believed to be `aim` — confirm)
- `findPortal(world, x, y, z, radius)`: search radius 128 blocks; exact search algorithm (spiral? column scan?); what coordinate is returned if no portal found?
- `createPortal(world, x, y, z, radius)`: search radius 16; 2-phase placement (find flat area, fallback to Y=70); exact obsidian frame layout (4×5 with 2×3 air interior)
- Overworld→Nether coordinate scaling: divide X/Z by 8 (confirmed?) or different ratio?
- Nether→Overworld: multiply X/Z by 8?
- End arrival: obsidian platform 5×5 at fixed coords (0, 60, 0)? Or relative to exit portal?
- Is PortalTravelAgent a singleton or per-world instance?
- What happens when `TravelToDimension` is called on a non-player entity (minecart, mob)?

**Resolution:** Spec at `Specs/PortalTravelAgent_Spec.md`. Obf name `aim` confirmed. NOT a singleton — `new aim()` per transition. Coordinate scaling is in `Minecraft.a(int)` NOT in aim: Overworld→Nether ×0.125, Nether→Overworld ×8.0, End transitions ×1.0. findPortal (`b()`): grid scan ±128 XZ, full-height Y scan per column, 3D distance-squared minimisation, returns false if not found. createPortal (`c()`): Phase 1 = 3D suitability check (solid floor + 3-deep air box ×4 orientations); Phase 2 = 2D column check ×2 orientations; Emergency = Y clamped to [70, height-10], clears 2×3×3 pocket. Frame always built 4× (loop) to trigger activation. Frame: 4×5 obsidian+portal, 2×3 interior. End platform: placed at entity arrival position (floor(X), floor(Y)−1, floor(Z)); 5×5 obsidian floor + 3 air layers; centred on spawn (100, 49, 0). aim.a() not called when leaving End (oldDim=1 fails condition oldDim<1).

---

## EnchantingXP
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Items/ItemEnchanted.cs` + `Core/EnchantmentHelper.cs` + `Core/EntityPlayer.cs` (XP fields)
Enchanting table and XP system are completely absent. `LivingEntity` has a stub `DropXP(int)` but no XP is tracked or consumed anywhere.

**Questions:**
- Obf class names: EnchantmentHelper (`?`), EnchantingTable block (`?`), ItemEnchanted (`?`)
- XP: how is `xpLevel` / `xpTotal` stored on EntityPlayer? Which DataWatcher slots?
- XP orb entity: obf name, size categories, pickup radius, merge radius, despawn age
- `xpSeed` per-player for randomizing enchantment options — how is it seeded/reseeded?
- Enchantment table: 3-slot UI, bookshelf bonus (15 max), level formula per slot
- Which enchantments exist in 1.0 final? (Protection/FireProtection/FeatherFalling/BlastProtection/ProjectileProtection/Respiration/AquaAffinity/Sharpness/Smite/BaneOfArthropods/Knockback/FireAspect/Looting/Efficiency/SilkTouch/Unbreaking/Fortune/Power/Punch/Flame/Infinity?)
- Enchantment ID table (obf constants)
- `ItemEnchanted` wrapper: NBT `ench` list format (id short + lvl short per entry)

**Resolution:** Spec at `Specs/EnchantingXP_Spec.md`. Real obf names: EntityXPOrb=`fk` (entity ID 2), BlockEnchantmentTable=`sy` (block ID 116), TileEntityEnchantmentTable=`rq`, ContainerEnchantment=`ahk`, EnchantmentHelper=`ml`, Enchantment base=`aef`.
XP orb: size 0.5×0.5; gravity 0.03; attraction radius 8 blocks; despawn at age2≥6000 ticks; 10 value tiers via threshold array [3,7,17,37,73,149,307,617,1237,2477].
Player XP fields: cd=XpLevel, cf=XpP(progress 0–1), ce=XpTotal. Level formula: `7+floor(level×3.5)` XP per level. Death drop: min(level×7, 100).
No xpSeed per-player — enchantment seed is per-container (ContainerEnchantment.b=nextLong(), fresh on each item placement). Items can only be enchanted once via table.
Bookshelf bonus: gap check (adjacent air at y and y+1), then ±2-block bookshelf scan including diagonal corners. Slot levels via ml.a(): base=1+nextInt(bonus/2+1)+nextInt(bonus+1), noise+=nextInt(5); slot0=(noise>>1)+1, slot1=noise*2/3+1, slot2=noise. Enchantability gates whether enchanting is possible at all.
19 enchantments: IDs 0–6 (armor/helmet), 16–21 (sword), 32–35 (tool). NO bow enchantments (Power/Punch/Flame/Infinity absent in 1.0). SilkTouch↔Fortune mutually exclusive. NBT: "ench" TAG_List of {id:short, lvl:short((byte)level)}.

---

## BowArrow
[STATUS:IMPLEMENTED]
**Needed for:** `Core/Items/ItemBow.cs` + `Core/EntityArrow.cs` + `Core/Items/ItemFishingRod.cs` + `Core/EntityFishHook.cs`
Ranged combat is completely absent. `ItemRegistry` has no bow or fishing rod. No projectile entity exists for arrows or hooks.

**Questions:**
- Obf class names: ItemBow (`?`), EntityArrow (`?`), ItemFishingRod (`?`), EntityFishHook (`?`)
- Bow: charge mechanic — how many ticks to full charge? 3 stages (0/1/2)? Damage formula at full charge?
- Arrow entity: gravity (0.05?), drag per tick, hitbox size, critical hit flag (from full-charge bow), fire-arrow from flame enchantment
- Arrow pickup: only when `inGround` and `ticksInGround > 7`; is the arrow ItemStack recoverable?
- Arrow NBT: `inGround`, `xTile/yTile/zTile`, `inTile`, `shake`, `life` (despawn 1200 ticks in ground), owner entity ID
- Skeleton AI: does it use the same `zo` hostile base? How does it select bow vs melee?
- Fishing rod: cast mechanic, hook entity physics, catch table (fish types, treasure, junk — is treasure/junk in 1.0 or later?), reel-in trigger
- ItemFishingRod durability cost per cast vs per catch?

**Resolution:** Spec at `Specs/BowArrow_Spec.md`. Real obf names: ItemBow=`il`, EntityArrow=`ro`, ItemFishingRod=`hd`, EntityFishHook=`ael`, EntitySkeleton=`it`.
Bow: charge formula `power=(f²+2f)/3` where f=ticksCharged/20; threshold 0.1; full charge at 20 ticks; arrow final speed=power×3.0; damage=ceil(speed×2.0); critical at power==1.0 adds nextInt(dmg/2+2). Durability 384, costs 1/shot.
Arrow: gravity 0.05/tick, air drag 0.99, water drag 0.8; hitbox 0.5×0.5; pickup when inGround+isPlayerArrow+shake==0; despawn 1200t in ground. NO fire-arrow in 1.0 (no Flame enchantment). NBT: xTile/yTile/zTile/inTile/inData/shake/inGround/player — shooter entity NOT saved (lost on reload).
Fishing: hook NOT in EntityList (no NBT persistence). Cast velocity 0.6 b/t. Fish bite roll 1/500 (1/300 with sky), gives RawFish (acy.aT=ID 349) + 1 XP stat. Durability 0 (miss) / 1 (fish) / 2 (ground) / 3 (entity). No treasure/junk in 1.0. Skeleton: extends zo; 60-tick reload; speed=1.6 spread=12; drops 0–2 arrows+bones; sunlight burn check per-tick random.

---

## RemainingMobs_Batch
[STATUS:PROVIDED]
**Needed for:** `Core/Mobs/` — 12 mob entity classes still registered via `RegisterId` stubs
in `EntityRegistry`. Without them, mobs from chunk NBT return null on load, and biome spawn lists
for passive/hostile packs are incomplete (SpawnerAnimals skips unknown types silently).

Mobs needed (obfuscated class names unknown for most):
- `Slime` (ID 55) — splits on death into smaller slimes; cube shape; jumps; size field
- `Ghast` (ID 56) — flying; fireball attack at player; 10 HP; sound-based; no pathfinding
- `PigZombie` (ID 57) — neutral by default; group aggro when any individual is attacked; NBT anger field
- `Enderman` (ID 58) — can pick up / place blocks; teleports away from water or projectiles; provoked by direct stare (DW bit); screams and attacks; height 2.9
- `CaveSpider` (ID 59) — extends Spider; smaller hitbox 0.7×0.5; can squeeze through 1-high gaps; poison attack (effect amplifier depends on difficulty)
- `Silverfish` (ID 60) — spawns from ID-97 monster eggs (stone/cobble/stone brick); calls nearby silverfish when attacked; very small 0.3×0.7
- `Blaze` (ID 61) — fires 3 small fireballs in burst; floats via vertical Y oscillation; drops blaze rods; immune to fire; can spawn from spawner in NetherFortress
- `LavaSlime` / MagmaCube (ID 62) — like Slime but in Nether; fire-immune; size field; no-pathfinding jump AI; drops magma cream
- `Squid` (ID 94) — passive water mob; ink sac drops; random swimming direction changes; no land AI; drowning on land
- `Wolf` (ID 95) — tamed/untamed/angry states; taming with bones; follows owner; attacks sheep/hostile if tamed; sitting with right-click; pack behaviour; collar DW color
- `MushroomCow` (ID 96) — like Cow but red/mushroom skin; mushroom biome only; milkable with mushroom soup; shear converts to normal Cow + drops mushrooms
- `SnowGolem` (ID 97) — player-built (2 snow blocks + pumpkin); throws snowballs at hostiles; melts in rain/warm biomes; leaves snow trail

**Questions per mob:**
- Obfuscated class name?
- Superclass chain (ww → zo/fx → concrete)?
- All DataWatcher slots used?
- Extra NBT fields beyond nq base (Health/HurtTime/DeathTime/AttackTime)?
- Hitbox dimensions (width × height)?
- Max HP and attack strength?
- Drop table (item IDs, quantities, conditions)?
- Special AI or tick behaviour not covered by the ww/zo/fx base?
- Spawn biome list (what `jf` creature type, what biome spawn lists)?

For Wolf specifically:
- Taming mechanic: which item? How many uses before tamed? Probability per bone?
- Collar color DataWatcher slot and default? Sits/stands toggle?
- Anger state: triggered by what? Persisted in NBT?
- NBT fields: Owner (string), Sitting (byte), Angry (byte)?

For Enderman specifically:
- Block pickup: which block IDs can Enderman carry? Can it place the block back?
- Teleportation: triggered by projectiles hitting it? By water? By player stare? Random idle?
- Stare detection: is it a direct camera-direction dot-product check? Range?
- DW slot for carried block ID and metadata?

For Slime/MagmaCube specifically:
- Size field: stored in DW and/or NBT? Values 0/1/2 (tiny/small/big)?
- Jump AI: how is jump force calculated from size? Tick interval?
- Split logic: when a slime dies, how many children? At what size does splitting stop?

**Expected deliverable:** `Specs/RemainingMobs_Spec.md` — one section per mob with all fields,
AI description, NBT layout, drop table, spawn type assignment.

---

## ThrowableEntities_Batch
[STATUS:PROVIDED]
**Needed for:** `Core/EntitySnowball.cs`, `Core/EntityEgg.cs`, `Core/EntityEnderPearl.cs`,
`Core/EntityFireball.cs`, `Core/EntitySmallFireball.cs` — all currently `RegisterId` stubs.
Also needed to implement `ItemSnowball`, `ItemEgg`, `ItemEnderPearl` which are plain `Item`
stubs in `ItemRegistry` (IDs 332, 344, 368).

**Entities needed:**
- Snowball (ID 11) — launched by player; creates impact particles; no damage; extinguishes Blazes
- Egg (ID 12 in 1.0?) — launched by player; 1/8 chance to spawn a chicken; breaks on impact
- ThrownEnderpearl (ID 14) — teleports thrower to landing position; 5 fall damage to thrower; entity ID in EntityList
- Fireball (ID 12 in entity list?) — Ghast projectile; large; continues flying until blocked; creates explosion power 1 (no block damage in peaceful?); sets target on fire
- SmallFireball (ID 13) — Blaze projectile; smaller; same rules as Fireball but no block damage

**Questions:**
- Obfuscated class names for all five?
- Common abstract throwable base class — does one exist? (suspected `ro` is Arrow, separate from throwable)
- For all throwables: gravity constant, drag, hitbox size, max range before despawn?
- Snowball: does it deal any damage to Blazes specifically, or just extinguish them?
- Egg: exact spawn rules: 1/8 chicken, on top of that a separate 1/32 for 4 chickens?
- EnderPearl: exact fall-damage amount? Teleport offset (feet position = impact position)?
- Fireball: who is the "owner" entity for explosion damage attribution? Power 1 — does it destroy blocks?
- SmallFireball: same as Fireball? Or no block destruction at all?
- Eye of Ender Signal (ID 15) — is this also a throwable? What does it do exactly?
- Are Snowball/Egg/EnderPearl in EntityList (have NBT)? Or similar to EntityFishHook (no NBT)?

**Expected deliverable:** `Specs/ThrowableEntities_Spec.md` — abstract base (if any) + one section
per entity: obf name, gravity/drag/hitbox, impact logic, NBT (or confirmed no-NBT), owner tracking.

---

## EntityFallingSand
[STATUS:PROVIDED]
**Needed for:** `Core/EntityFallingSand.cs` — currently `RegisterId("FallingSand", 21)` stub.
Without this, sand and gravel blocks that should fall (e.g. placed over an air gap, or created
by explosions) instead hang stationary in mid-air. The sand block's `BlockTick` calls
`world.spawnEntity(new EntityFallingSand(...))` but the class does not exist yet.

**Obfuscated class:** suspected `hz` — used in BlockSand `e()` tick method.

**Questions:**
- Class name (confirm or correct `hz`)?
- Fields: stored block ID? Metadata? Fall distance?
- Tick: gravity acceleration (same 0.04 as EntityItem?); drag/friction?
- Landing: when the entity hits a solid block, what happens?
  - If the block below is solid: place the block, remove entity
  - If the block below is air/liquid: keep falling
  - If the block cannot be placed (e.g. another block already occupies the position): drop as item?
  - Does it harm entities it lands on?
- Which block IDs can be "falling"? Just sand (12) and gravel (13)? Or others (concrete powder — not in 1.0)?
- NBT: `"TileID"` byte? `"Data"` byte? Is it in EntityList (afw)?
- Does it tick on the client too, or only server-side placement?
- What happens when a FallingSand entity is in a chunk that gets saved — does it persist via NBT or despawn?

**Expected deliverable:** `Specs/EntityFallingSand_Spec.md`

---

## EntityPainting
[STATUS:PROVIDED]
**Needed for:** `Core/EntityPainting.cs` — currently `RegisterId("Painting", 9)` stub.
Paintings are placeable decorative entities. The EnumArt `sv` class (25 variants) was partially
analysed in the ItemFood session but never specced for implementation.

**Obfuscated classes:** `sv` (EnumArt enum), painting entity class unknown.

**Questions:**
- Obfuscated class name for the painting entity?
- `sv` (EnumArt): complete table of all 25 variants — enum name, atlas tile X/Y offset, width×height in blocks?
- Placement: which face is the painting placed on? How is the position snapped to the wall?
- Hitbox: matches the painting's dimensions exactly?
- Right-click places with ItemPainting (obf: unknown) — what item ID?
- On placement: random variant selected from those that fit in the available wall space?
- When the supporting block is removed: does the painting drop as an item?
- NBT: `"Motive"` string (variant name), `"Dir"` byte (facing 0-3), `"TileX"/"TileY"/"TileZ"` ints?
- Is EntityPainting in the `afw` EntityList? What int entity ID?

**Expected deliverable:** `Specs/EntityPainting_Spec.md` — entity fields + EnumArt full table +
placement algorithm + NBT layout.

---

## EntityBoat
[STATUS:PROVIDED]
**Needed for:** `Core/EntityBoat.cs` — currently `RegisterId("Boat", 41)` stub.
Boats are rideable water vehicles craftable from wooden planks.

**Questions:**
- Obfuscated class name?
- Physics: how does buoyancy work in water (partial submersion lift)?
- Riding: player enters via right-click; driver controls yaw/speed via WASD?
- Speed: faster than swimming on open water?
- Damage model: boats are destroyed by attacks or collision above a speed threshold?
- On destruction: drops 3 wooden planks?
- Behaviour in different fluids: water = normal; lava = instant destroy + no drops?
- Mouse input forwarding to entity from rider (`vi.bA`)?
- NBT: just base entity fields? Any boat-specific fields?
- EntityList int ID = 41?

**Expected deliverable:** `Specs/EntityBoat_Spec.md`

---

## EntityMinecart
[STATUS:PROVIDED]
**Needed for:** `Core/EntityMinecart.cs` — currently `RegisterId("Minecart", 40)` stub.
Three minecart variants exist in 1.0: empty minecart, storage minecart (chest), powered minecart (furnace).

**Obfuscated class:** base minecart class unknown; sub-types likely single class with `type` field.

**Questions:**
- Is there one class with a `type` field (0=empty, 1=chest, 2=furnace) or three separate classes?
  - If one class: what int type values?
  - If separate: class names?
- Rail interaction: how does the minecart track along BlockRail (ID 27)?
  - Rail metadata 0-9: which encode straight, curve, slope?
  - Does the minecart read the rail's metadata to compute its next position?
  - Slope ascent/descent: gradient force and speed cap?
- Physics on flat rail: friction deceleration? Max speed?
- Powered rail (ID 27 variant or ID 28?): boosts minecart; reversed if unpowered (brake)?
- Detector rail (ID 28 or another ID?): emits redstone signal when minecart is above?
- Off-rail: does the minecart fall off rail and slide/fall as normal entity?
- Rider: player enters via right-click; exits via sneak key?
- Storage minecart: 27-slot IInventory (like chest)?
- Powered minecart: accepts coal as fuel; pushes attached minecarts?
- NBT: type field? Inventory items? Fuel ticks?
- On destruction: drops empty minecart + chest/furnace contents?
- EntityList int ID = 40?

Also needed: BlockRail variants — what IDs are used for powered rail, detector rail, activator rail?
Are they separate block IDs or metadata on ID 27?

**Expected deliverable:** `Specs/EntityMinecart_Spec.md` — physics, rail interaction algorithm,
storage/powered subtypes, NBT. Include rail ID table if BlockRail metadata is non-obvious.

---

## Container_System
[STATUS:PROVIDED]
**Needed for:** `Core/Container/` directory (does not yet exist). Crafting, smelting, and all
inventory interaction require a `Container` base class and concrete subclasses. Currently
`BlockWorkbench.OnBlockActivated` and `BlockFurnace.OnBlockActivated` open nothing — there is
no container layer at all. The enchanting container (`ContainerEnchantment`) exists but is
isolated.

**Obfuscated classes:** container base is likely `bs`; crafting grid `ag`; crafting container unknown;
furnace container unknown; chest container unknown.

**Questions:**

Container base (`bs`):
- Fields: list of Slot objects? Player inventory reference?
- Slot class: fields (inventory ref, slot index, display x/y)?
- Core click logic `b(int slotId, int button, boolean shift, vi player)`:
  - Normal left-click: swap cursor with slot?
  - Normal right-click: split stack into slot, or take half?
  - Shift-click: move to other inventory section automatically?
  - Double-click: collect same items into cursor?
- `canInteractWith(vi player)` — distance check?
- Listener pattern: `onContentsChanged` callback?

CraftingGrid (`ag`):
- A 2×2 or configurable-size inventory?
- When contents change, does it auto-check recipes?
- Does the output slot refuse direct placement?

ContainerCrafting (workbench, 3×3):
- SlotCrafting (output slot): decrements all inputs by 1 when taken; triggers `onCrafting`?
- Which CraftingRecipes class holds the recipe table?

CraftingRecipes / CraftingManager:
- Full table: all vanilla 1.0 shaped and shapeless recipes?
- Shaped recipe format: pattern strings + ingredient map?
- Shapeless recipe: just a set of ingredients?
- Does the recipe system handle ore dictionary or just exact item IDs?
- How is mirroring handled (shaped recipes can be mirrored)?

ContainerFurnace:
- 3 slots: input (0), fuel (1), output (2)?
- Progress bars: `cookTime` (0-200) and `burnTime` / `currentBurnTime`?
- Method to transfer state between server and client (`a(int id, int data)` / `b(int id, int data)`)?

ContainerChest:
- 27 or 54 slots (27 for single, 54 for double chest)?
- How are double-chest inventories combined — two `IInventory` references stitched?

ContainerDispenser:
- 9 slots in a 3×3 grid?

**Expected deliverable:** `Specs/Container_Spec.md` — ContainerBase slot logic, click handling
(all 4 cases: left/right/shift/double), SlotCrafting trigger, ContainerCrafting, ContainerFurnace,
ContainerChest, ContainerDispenser. Full vanilla 1.0 crafting recipe table in an appendix.

---

## CraftingRecipes
[STATUS:PROVIDED]
**Needed for:** `Core/CraftingManager.cs` — the `CraftingManager` class exists with empty lists.
Without recipes, players cannot craft any item. This is the single largest gameplay gap remaining.

**Questions:**
- Obfuscated class holding the recipe table? (suspected `mt` singleton — but that is FurnaceRecipes;
  there should be a separate class for crafting)
- Shaped recipe format in source: pattern as string array? `' '` = empty, letter = ingredient?
- Shapeless recipe format: just a varargs item list?
- Does the recipe check handle `ItemStack.damage` for tool materials, or only item IDs?
- Can multiple items match the same shaped-recipe slot (e.g. "any plank")?
- Are any recipes mirror-symmetric (L-shaped items that work flipped)?
- Complete list of all shaped recipes in 1.0 acy.java static initializer (all `a(new Object[] {...})` calls)
- Complete list of all shapeless recipes
- Any recipes that produce items with non-zero damage/metadata (dyed wool, specific records)?

**Expected deliverable:** `Specs/CraftingRecipes_Spec.md` — complete recipe table for all
vanilla 1.0 crafting recipes, organized by output item. Include both shaped and shapeless.
Format: output item ID + count + meta | input pattern or ingredient list.

---

## PotionEffect_System
[STATUS:PROVIDED]
**Needed for:** `Core/Potions/` directory (does not exist yet). LivingEntity has `_activeEffects`
list and stubs for `AddPotionEffect` / `GetActivePotionEffect` / `IsPotionActive`, but the
`PotionEffect` and `Potion` classes do not exist. ItemFood already uses a placeholder potion
effect for Spider Eye; EntityCaveSpider needs Poison; Milk bucket removes all effects.

**Obfuscated classes:** `Potion` registry likely `ad`; PotionEffect carrier likely `kd`.

**Questions:**

Potion class (`ad` or similar):
- Static registry: how many potion effect types in 1.0? (suspected 23)
- Fields per Potion: int ID, liquid color, is_instant, is_bad_effect?
- All potion IDs (0-23+) with names and colors — e.g. Speed=1, Slowness=2, Haste=3, etc.?
- Method to get color from active effects list (mixed blend for splash)?

PotionEffect (`kd` or similar):
- Fields: int effectId, int duration (ticks), int amplifier (0=level I, 1=level II)?
- `performEffect(nq entity)` — called per tick or per N ticks depending on effect type?
- Per-effect tick logic:
  - Speed (1): movement multiplier?
  - Slowness (2)?
  - Haste (3): mining speed factor?
  - Mining Fatigue (4)?
  - Strength (5): attack bonus?
  - Instant Health (6): instant +4 HP per amplifier?
  - Instant Damage (7): instant damage?
  - Jump Boost (8)?
  - Nausea (9): visual only?
  - Regeneration (10): HP/tick formula?
  - Resistance (11): damage reduction already in LivingEntity damage pipeline?
  - Fire Resistance (12): prevents fire/lava damage?
  - Water Breathing (13): prevents drowning?
  - Invisibility (14)?
  - Blindness (15)?
  - Night Vision (16)?
  - Hunger (17): increases exhaustion?
  - Weakness (18)?
  - Poison (19): damage per 25t, not below 1 HP?
  - Wither (20): damage per 40t, CAN kill?
  - Health Boost (21)?
  - Absorption (22)?

ItemPotion (ID 373):
- Metadata encodes effect type + potion tier + splash/drinkable flags?
- Splash: thrown projectile entity; splash radius; exposure fraction?
- Drinkable: OnItemRightClick directly applies effects?
- Duration formula from metadata?
- Is splash potion a separate class or same class with flag?

**Expected deliverable:** `Specs/PotionEffect_Spec.md` — Potion registry (all effect IDs + colors),
PotionEffect fields + performEffect per-tick logic, ItemPotion metadata decoding, splash vs drink.

---

## BlockPlants_Batch
[STATUS:PROVIDED]
**Needed for:** Multiple plain `Block` stubs in `BlockRegistry` that have no custom behaviour:
- ID 6 — Sapling (oak/spruce/birch/jungle meta 0-3): grows into tree via random tick + bonemeal
- ID 37 — Dandelion (yellow flower): simple plant; canBlockStay requires soil
- ID 38 — Rose (red flower): same as Dandelion
- ID 39 — Brown Mushroom: can spread if <5 in 9×9×3 volume; survives in low light
- ID 40 — Red Mushroom: same spread rules
- ID 83 — Reed / SugarCane: grows up to 3 tall; requires water adjacent at base; random tick grow
- ID 115 — Nether Wart: 4 growth stages (meta 0-3) on soul sand only; no bonemeal growth in 1.0
- ID 104 — Melon Stem: 8 growth stages; when fully grown attempts to place melon in adjacent cell
- ID 105 — Pumpkin Stem: same as melon stem

Currently all these are plain `Block` stubs with no tick behaviour. None of them fall when their
support block is removed, grow, or check canBlockStay.

**Questions:**

BlockSapling (ID 6):
- Obfuscated class name?
- How is sapling type (oak/spruce/birch) stored — metadata?
- Random tick: what probability? Is it affected by bonemeal?
- Bonemeal (ItemDye meta 15): guarantees instant growth?
- Tree type selected by meta: meta 0=oak (90% gq / 10% yd), meta 1=spruce (ty or us), meta 2=birch (jp)?
- canBlockStay: requires dirt/grass/farmland below?

BlockFlower / BlockRose (IDs 37/38):
- Obfuscated class name (likely shared base)?
- canBlockStay: specific soil block types?
- Do they have any random tick behaviour?
- Drops itself as item?

BlockMushroom (IDs 39/40):
- Spread logic: max 5 in 9×9×3 before spreading stops?
- Light level requirement for survival? Above what blockLight level does it die?
- Drops itself as item?

BlockReed (ID 83):
- Obfuscated class name?
- Water adjacency check: water block at same Y level on any of 4 horizontal sides?
- Max height: 3 blocks total?
- Growth: each random tick, if top reed and below full height → place new block above?
- canBlockStay: dirt/grass/sand below AND water adjacent at y-1 level?

BlockNetherWart (ID 115):
- Obfuscated class name?
- Soul sand only (block ID 88)?
- Growth stages 0-3 via random tick only (no bonemeal in 1.0)?
- Drops: meta 0 = 1 nether wart; meta 3 = 2-4 nether wart (fortune applies)?

BlockStem (IDs 104/105):
- Obfuscated class name?
- Growth stages 0-7 via random tick; bonemeal works?
- At stage 7: attempts to place melon (ID 103) or pumpkin (ID 86) in random adjacent air cell?
- Placed fruit must have solid block below?
- When the placed fruit is harvested: does the stem reset to stage 0 or stay at 7?
- Drops: melon seeds (ID 362) or pumpkin seeds (ID 361)?

**Expected deliverable:** `Specs/BlockPlants_Spec.md` — one section per plant type: obf class name,
canBlockStay conditions, random tick growth/spread logic, bonemeal response, drops table.

---

## BlockVine
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockVine.cs` — currently a plain `Block` stub (ID 106).
Vines have complex multi-face connectivity, climbing behaviour, and spread AI.

**Obfuscated class:** suspected `ahl` — confirmed by the Analyst session correction
("BlockVine: guess `ahl`…real `mz`" — wait, that was BlockRedstoneDiode. Vine is `ahl` confirmed?).
Actually the BlockRedstone spec noted "`ahl`=BlockVine". So obf name is `ahl`.

**Questions:**
- Confirm obfuscated class is `ahl`?
- Metadata: bitmask — which bit = which face (N/S/E/W, top)?
- Vine has no bottom face — must be attached to N/S/E/W solid block OR to the block above?
- `canBlockStay`: requires at least one face with an adjacent solid opaque block?
- Climbing: does Entity.isOnLadder check for vine block (same as ladder ID 65)?
- Spread (random tick): tries to attach to adjacent blocks? Up to 4 horizontal + above?
- Can vines grow downward without attachment? Or must the block above be vine/solid?
- Collision: no collision box (like ladder — entity walks through)?
- Selection/ray-trace: does it have a collision box for ray-trace (right-click, breaking) even if walk-through?
- Breaking: drops nothing (unless sheared with ItemShears)?
- Light opacity?
- Material: `p.n` (same as air) or a plant material?

**Expected deliverable:** `Specs/BlockVine_Spec.md`

---

## BlockFenceGate
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockFenceGate.cs` — currently a plain `Block` stub (ID 107).
The `BlockFence` class already has hard-coded gate connectivity checking for ID 107, so the
gate geometry must be compatible. The gate also accepts redstone signals.

**Questions:**
- Obfuscated class name?
- Metadata: facing (2 bits) + isOpen (1 bit)? Or just facing?
- AABB when closed: same as fence post (0.375–0.625 in one axis, full height 1.5)?
- AABB when open: no collision (entity can walk through)?
- Is it redstone-activatable (like iron door)?
- Does right-click toggle open/closed?
- When open: does the fence-gate "face" rotate so it lies along the fence line?
- Fence connectivity: BlockFence already checks `if id==107 → true`. Does BlockFenceGate reciprocate?
- Breaking drops 1 fence gate item?
- Light opacity?

**Expected deliverable:** `Specs/BlockFenceGate_Spec.md`

---

## BlockPane
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockPane.cs` — iron bars (ID 101) and glass pane (ID 102) are
plain `Block` stubs. They use thin cross-shaped geometry like fences but with different connectivity
rules. The `BlockFence` spec described fences; panes/bars are similar but thinner (2/16 thick vs 3/16).

**Obfuscated classes:** iron bars class unknown; glass pane class unknown (may be shared base `fp`?).

**Questions:**
- Obfuscated class name(s)? Shared base or separate?
- Post width: 2/16 (0.0625 on each side of center) — confirm?
- Arm thickness: also 2/16?
- Connectivity: connects to same block type AND to any solid opaque block?
- Does glass pane connect to iron bars and vice versa?
- Does glass pane connect to glass blocks?
- Height: full 1.0 (unlike fence at 1.5)?
- AABB: post core = 0.4375–0.5625 (2/16 centered), extends to 0/1 per connected direction?
- Selection box (ray-trace): full cube like fence?
- `isOpaqueCube`: false? `renderAsNormal`: false?
- Light opacity: 0 for glass pane, something for iron bars?
- Drops: glass pane drops itself; iron bars drops itself?
- Does the rendering use the face texture for the post/arm, or a special thin-bar texture?

**Expected deliverable:** `Specs/BlockPane_Spec.md` — shared base class geometry (if any),
connectivity rules, AABB logic, rendering differences between iron bars and glass pane.

---

## BlockChest_Full
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockChest.cs` — currently a plain `Block` stub (ID 54).
`TileEntityChest` already exists (27 slots), but:
1. Double chest detection is missing — two adjacent chests merge into 54-slot inventory
2. Opening animation (lid lifts) uses TileEntityChest fields not yet linked
3. `OnBlockActivated` does not open any container

Also needed: an `InventoryLargeChest` combining two `IInventory` instances into a 54-slot view.

**Questions:**
- Obfuscated class name for BlockChest?
- How does double-chest detection work?
  - When placed adjacent to another chest: which axis is checked (Z and/or X only, not Y)?
  - Is there a priority rule for which chest is "left" vs "right" in the combined view?
  - Can you open a single chest that is adjacent to another? Or always treated as double?
- `OnBlockActivated`: opens ContainerChest — how is the container title determined?
- Opening animation: is it stored in TileEntityChest? Which field?
  - `numPlayersUsing` counter (number of players with chest open) — incremented on open, decremented on close?
  - Lid angle interpolated from numPlayersUsing > 0?
- Breaking: chest drops all its contents as EntityItem entities + drops itself?
- `InventoryLargeChest` (obf: unknown): wraps two `IInventory`; slot 0-26 → left chest, 27-53 → right?
- Can cats (ocelots) sit on chests to prevent opening in 1.0? (probably not — cats added 1.2)
- NBT: chest is a TileEntity already — no additional block metadata needed?

**Expected deliverable:** `Specs/BlockChest_Spec.md` — double-chest placement detection, combined
inventory class, opening/closing animation fields, container wiring.

---

## BlockWorkbench_Furnace_Interaction
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockWorkbench.cs` (ID 58) and `Core/Blocks/BlockFurnace.cs`
(IDs 61/62). Both are plain `Block` stubs. Their `OnBlockActivated` methods do nothing.
Container_Spec (above) covers the Container classes themselves; this spec covers the block side.

**Questions:**

BlockWorkbench (ID 58):
- Obfuscated class name?
- `OnBlockActivated`: opens a ContainerCrafting (3×3)?
- Any special fields on the block? Or purely a passthrough to container?
- Drops: itself (1 workbench)?
- Texture: faces? (top=60, front=59, sides=43, bottom=4 in terrain.png?)

BlockFurnace (IDs 61/62 — unlit and lit):
- Obfuscated class name?
- Facing: stored in metadata (meta 0-5 = facing down/up/north/south/west/east)?
- Placement: placed facing the player (like BlockDispenser)?
- `OnBlockActivated`: opens ContainerFurnace?
- Lit state (ID 62) persists after reload — when does it switch from unlit→lit and back?
  (TileEntityFurnace already handles the block switch via `eu.a()` — confirm this is correct)
- Light emission: lit furnace emits light level 13?
- Texture: unlit front=45, lit front=62; sides=43, top/bottom=62 in terrain.png?

**Expected deliverable:** `Specs/BlockWorkbench_Furnace_Spec.md`

---

## BlockSapling_Growth
[STATUS:PROVIDED]
**Needed for:** Sapling is currently a plain `Block` stub (ID 6). Even if BlockPlants_Spec covers
the basics, the tree-growth call from sapling needs extra detail: exactly which WorldGenXxx
generator is instantiated, with which parameters, for each sapling type.

Note: this may be fully covered by `BlockPlants_Spec` above — if the Analyst feels the sapling
detail there is sufficient, this request may be skipped.

**Questions:**
- When the sapling random-tick fires for growth: is the decision `nextInt(N)==0`? What is N?
- Is the exact sapling-to-tree generator dispatch the same as the biome tree dispatch, or hardcoded?
  - Meta 0 (oak): `nextInt(10)==0` → WorldGenBigTree, else WorldGenTrees?
  - Meta 1 (spruce): WorldGenTaiga1 or WorldGenTaiga2?
  - Meta 2 (birch): WorldGenForestTree?
  - Meta 3 (jungle): WorldGenMegaJungle? (if jungle saplings exist in 1.0 — they may not)
- Bonemeal: does bonemeal on sapling try to grow immediately? 100% success or still random?
- 2×2 spruce/jungle tree: does vanilla 1.0 support 2×2 saplings for larger trees?

**Expected deliverable:** Can be appended to `Specs/BlockPlants_Spec.md` as an extra section,
or delivered as a standalone `Specs/BlockSapling_Spec.md`.

---

## BlockGlowstone_Drops
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockGlowstone.cs` — glowstone (ID 89) is a plain `Block` stub.
The only custom behaviour is its drop mechanic (yields dust, not the block itself).

**Questions:**
- Obfuscated class name?
- Drops: `nextInt(4) + 2` glowstone dust (ID 348)? Fortune increases max?
- Silk touch: drops glowstone block directly instead of dust?
- Hardness: 0.3?
- Light emission: 15?
- Material: `p.p` (glass) — correct?
- Placing glowstone dust back into the block: is there a recipe (4 dust → 1 glowstone)?
  (This would be in CraftingRecipes, not in the block spec)

**Expected deliverable:** Can be a short section in `Specs/CraftingRecipes_Spec.md` under drops,
or a short standalone `Specs/BlockGlowstone_Spec.md`.

---

## BlockTrapDoor
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockTrapDoor.cs` — trapdoor (ID 96) is a plain `Block` stub.
Trapdoors are hatch-style single-block doors that open horizontally (lie flat) or vertically (stand up).

**Obfuscated class:** unknown.

**Questions:**
- Obfuscated class name?
- Metadata: bit 0-1 = placement side (attached to bottom or top of block? N/S/E/W?), bit 2 = isOpen?
- AABB closed (bottom): 0–3/16 thick slab at floor?
- AABB closed (top): 0–3/16 thick slab at ceiling?
- AABB open: 0–3/16 thick slab at one wall (which wall)?
- Can entities climb through an open trapdoor? Any collision with open trapdoor?
- Right-click toggles; redstone also controls?
- Wood only in 1.0 (iron trapdoor added in 1.8)?
- Drops itself on break?

**Expected deliverable:** `Specs/BlockTrapDoor_Spec.md`

---

## ItemBucket
[STATUS:PROVIDED]
**Needed for:** `Core/Items/ItemBucket.cs` — bucket items (IDs 325/326/327) are plain `Item`
stubs. Buckets are essential for water/lava placement and milk collection from cows.

**Obfuscated classes:** bucket base likely `rc`; sub-items for water/lava bucket may share same
class with metadata, or be separate classes.

**Questions:**
- Is there one bucket class with a "fluid" field, or three separate item classes?
  - Empty bucket (ID 325): `new rc(325)`?
  - Water bucket (ID 326): `new rc(326, 8/9)` storing water block ID?
  - Lava bucket (ID 327): `new rc(327, 10/11)` storing lava block ID?
- Empty bucket `OnItemRightClick`:
  - Ray-cast to find water or lava source block (material check)?
  - Picks up still water (ID 9) and still lava (ID 11)?
  - Replaces that block with air?
  - Returns the filled bucket (ID 326 or 327)?
- Water/lava bucket `OnItemRightClick`:
  - Places source block at target position?
  - Returns empty bucket (ID 325)?
  - Can it overwrite grass/replaceable blocks?
- Milk bucket (ID 335 or different?):
  - Obtained by right-clicking a cow?
  - `OnItemRightClick` drunk by player: removes all potion effects + 3 heal?
  - Is milk a separate class or same `rc` with fluid type "milk"?
- Dispensing: can a Dispenser dispense water/lava buckets?
- Stack size: 1 (no stacking for filled buckets)?

**Expected deliverable:** `Specs/ItemBucket_Spec.md`

---

## ItemDye_Bonemeal
[STATUS:PROVIDED]
**Needed for:** `Core/Items/ItemDye.cs` — item ID 351 (dye) is a plain `Item` stub. Dye is the
most important coloring item; damage/metadata 15 = bonemeal which is critical for farming.

**Obfuscated class:** confirmed `xv` (from the OpenQuestion_AcyAV resolution session).

**Questions:**
- Bonemeal (meta 15) `OnItemUse`:
  - Applied to crops (ID 59): calls InstantGrow() → stage 7?
  - Applied to sapling (ID 6): triggers tree growth attempt?
  - Applied to grass block (ID 2): spawns WorldGenTallGrass/WorldGenFlowers on surface?
  - Applied to melon/pumpkin stem: advances to fully grown?
  - Applied to mushroom: grows into huge mushroom if space allows?
  - Applied to any other block: no effect?
- Other dye colors (meta 0-14): applied to wool (ID 35)?
  - How is dyed wool metadata set?
  - Can dye be applied to sheep directly to change their wool color?
- Item texture: each of 16 dye types has its own items.png icon?
- Stack size: 64?
- ItemDye also dropped by squids (ink sac, meta 0)?

**Expected deliverable:** `Specs/ItemDye_Spec.md`

---

## ItemShears
[STATUS:PROVIDED]
**Needed for:** `Core/Items/ItemShears.cs` — ItemShears (ID 359) is a plain `Item` stub.
Shears are required for wool harvesting (without killing sheep), leaf collection, and vine breaking.

**Obfuscated class:** unknown.

**Questions:**
- Obfuscated class name?
- Durability: 238 uses?
- `OnItemUse` on blocks:
  - Leaves (IDs 18/161): drops leaves block directly (instead of sapling/nothing)?
  - Vines (ID 106): drops vines?
  - Cobweb (ID 30): drops string (ID 287)?
  - Any other blocks?
- `OnItemUse` on entities:
  - Sheep: shears wool (drops 1-3 wool of the sheep's current color), sets `Sheared=true` flag?
  - Mooshroom: shears 5 mushrooms + converts to normal cow?
  - Snow Golem: removes pumpkin head, revealing its "face"?
- Does shearing cost 1 durability per use?
- `isItemTool`: true (so correct durability damage from `damageItem`)?
- `canHarvestBlock`: true for cobweb?

**Expected deliverable:** `Specs/ItemShears_Spec.md`

---

## ItemSign_Placement
[STATUS:PROVIDED]
**Needed for:** `Core/Items/ItemSign.cs` — sign item (ID 323) is a plain `Item` stub.
Sign placement is unusual: wall signs (ID 68) and floor signs (ID 63) use separate block IDs,
with floor signs storing yaw as metadata.

**Questions:**
- Obfuscated class name?
- `OnItemUse`: places sign on the targeted block face?
  - Placed on side faces → wall sign (ID 68), metadata = facing direction (2-5)?
  - Placed on top face → floor sign (ID 63), metadata = Math.floor(player.yaw/22.5+0.5) & 15
    → 16 yaw positions (each 22.5°)?
- After placing the sign block, does the game open a "sign editing GUI"?
  - If yes: what triggers the GUI open — a server packet? A block activation?
  - Or does the block just place with empty text?
- Sign item drops: floor/wall sign blocks both drop sign item (ID 323), not block form?
- Stack size: 16?

**Expected deliverable:** `Specs/ItemSign_Spec.md`

---

## VillagePieces
[STATUS:PROVIDED]
**Needed for:** `Core/WorldGen/Village/` directory (does not exist). `MapGenVillage` is
implemented but calls a `VillageFactory` that does not exist yet — it currently generates nothing
(no village structures appear in plains/desert biomes).

The WorldGenStructures_Spec §3 documented the village placement algorithm but noted:
> "start=yo; starting piece=yp" — piece list for Village pending dedicated spec.

**Obfuscated classes:** village start `yo`, piece registry `yp`, pieces unknown.

**Questions:**

VillageFactory / piece registry:
- What is the equivalent of `tc` (StrongholdPieceFactory) for villages — is it `yp`?
- How does the factory choose pieces: is there a weight table? Or a fixed template?
- Depth limit?
- XZ radius from start?

Road pieces:
- Are there straight road segments and turns?
- What blocks form the road (gravel ID 13? Dirt? Cobblestone)?

House pieces (multiple sizes):
- Small house, large house, hut?
- Dimensions per house type?
- Chest loot (if any)?
- Door placement and direction?
- Any NPC (Villager) spawned in houses?

Well piece:
- Dimensions?
- Water source blocks?

Blacksmith / forge piece:
- Does it exist in 1.0?
- Chest loot table (if yes)?

Farm pieces:
- Crops + farmland layout?
- Water source?

Library piece (if in 1.0):
- Bookshelves + crafting table?

Church / tower piece (if in 1.0)?

**Expected deliverable:** `Specs/VillagePieces_Spec.md` — piece registry, weight/template table,
full dimensions and block palette for each piece type, loot tables, door placement algorithm.

---

## EntityVillager
[STATUS:PROVIDED]
**Needed for:** `Core/Mobs/EntityVillager.cs` — Villager (ID 120) is a `RegisterId` stub.
Villagers populate generated villages and offer trades.

**Questions:**
- Obfuscated class name?
- Profession metadata (0-4 or 0-5): farmer/librarian/priest/blacksmith/butcher?
- Trading system:
  - `MerchantRecipe` class: buy1 + buy2 + sell ItemStacks?
  - `MerchantRecipeList`: list of trades per villager?
  - Trades unlocked based on profession?
  - Use limit per trade before it resets?
- AI: villager wanders, returns inside at night, flees from zombies?
- Zombie villager in 1.0? (Zombie that infects villagers added 1.4 — probably not in 1.0)
- NBT: profession int, trade list?
- Breeding: do villagers reproduce when village has enough houses in 1.0?

**Expected deliverable:** `Specs/EntityVillager_Spec.md`

---

## BlockCauldron
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockCauldron.cs` — cauldron (ID 118) is a plain `Block` stub.
Cauldrons were added in Beta 1.9 and are present in 1.0.

**Questions:**
- Obfuscated class name?
- Water level metadata 0-3 (0=empty, 3=full)?
- Right-click with water bucket: fills one level, returns empty bucket?
- Right-click with empty bucket when full: takes one level, gives water bucket?
- Right-click with leather armor: washes one dye off the leather color (or removes damage)?
- Fills from rain: random tick increments level when raining?
- AABB: partial height? What dimensions?
- Drops itself when broken?

**Expected deliverable:** `Specs/BlockCauldron_Spec.md`

---

## BlockBrewingStand
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockBrewingStand.cs` (ID 117) and `Core/TileEntity/TileEntityBrewingStand.cs`
— both are stubs. Brewing stands were added in Beta 1.9 and are in 1.0. TileEntityBrewingStand
stub already exists in TileEntityStubs.cs but has no logic.

**Questions:**
- Obfuscated class name for the block?
- Slots: 4 — ingredient slot + 3 potion bottle slots?
- Brewing animation: `brewTime` counter (400 ticks)?
- Recipe system: ingredient + base potion → output potion — full recipe table in 1.0?
- Which potions can be brewed in 1.0 (some were added later)?
- Fuel? (Blaze powder fuel was added in 1.9 snapshot, not in release 1.0 — confirm)
- Light emission when active?
- NBT: `Items` (4 slots), `BrewTime` short?

**Expected deliverable:** `Specs/BlockBrewingStand_Spec.md`

---

## EntitySnowGolem
[STATUS:PROVIDED]
**Needed for:** promoted `Register<EntitySnowGolem>` in EntityRegistry (ID 97 "SnowMan").
Snow golems are player-buildable utility mobs (2 snow blocks + pumpkin) that throw snowballs at
hostiles and leave a snow trail.

**Questions:**
- Obfuscated class name?
- Superclass: extends `zo` (EntityMonster)? Or `ww` (EntityAI)?
- Build detection: does the world monitor for pumpkin placement on top of 2 snow blocks?
  Or does the player use a pumpkin item to summon it?
- AI: targets and throws snowballs at EntityMonster entities within range?
- Snow trail: places snow layer (ID 78, meta 0) at the golem's position each tick? Condition?
- Melting: dies in warm biomes (temp > 1.0)? Dies in rain?
- NBT: just base entity fields?
- Hitbox: same as player (0.6×1.8)?
- HP: 4?

**Expected deliverable:** `Specs/EntitySnowGolem_Spec.md`

---

## ItemGoldenApple
[STATUS:PROVIDED]
**Needed for:** `Core/Items/ItemGoldenApple.cs` — golden apple (ID 322) is a plain `Item` stub.
The golden apple is special food that grants regeneration effects.

**Questions:**
- Obfuscated class name? Is it a subclass of ItemFood?
- Regular golden apple (meta 0): heals how much? Grants Regeneration II for how long?
- Enchanted golden apple (meta 1, "Notch apple"): heals? Grants Absorption, Fire Resistance, Regeneration?
- Is meta 1 craftable in 1.0? (8 gold blocks + apple)?
- In 1.0 was the regular golden apple crafted with 8 gold nuggets or 8 gold ingots?
- Does it use the ItemFood `FinishUsingItem` path or its own implementation?

**Expected deliverable:** Can be a section in `Specs/CraftingRecipes_Spec.md`, or short standalone
`Specs/ItemGoldenApple_Spec.md`.

---

## BlockGrassPlant_CanBlockStay
[STATUS:PROVIDED]
**Needed for:** Multiple plain block stubs that share a "canBlockStay requires solid block below"
base class pattern (`wg` = BlockFlower base). These blocks currently never drop themselves when
their support is removed:
- ID 31 — Tall Grass (meta 0=dead shrub, 1=tall grass, 2=fern)
- ID 32 — Dead Bush
- ID 37, 38 — Yellow Flower, Rose (may share `wg` base)
- ID 27, 28 — Rails and detector rails (different base but also "detach on support removal")

**Questions:**
- What is `wg` (BlockFlower base class)? Fields, canBlockStay, onNeighborChange, drops?
- Does `wg` shared by flowers AND tall grass AND dead bush?
- Tall grass (ID 31): drops nothing normally; drops itself with shears; rare wheat seed drop with Fortune?
- Dead bush (ID 32): drops stick? Or nothing?
- Are rails (IDs 27/28) a subclass of `wg` or a completely separate class?

**Expected deliverable:** Can be merged into `Specs/BlockPlants_Spec.md` or delivered as
`Specs/BlockFlowerBase_Spec.md`.

---

## BlockRail_Full
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockRail.cs` — plain `Block` stubs for IDs 27 (rail), 28 (powered
rail), 66 (detector rail), 67 (activator rail — if in 1.0; may not exist until 1.5).

**Questions:**
- Obfuscated class names for each rail type?
- Metadata encoding for ID 27 (plain rail):
  - meta 0-5: straight segments (NS/EW/ascending)?
  - meta 6-9: curved corners (NE/SE/SW/NW)?
- Does the rail auto-connect on placement (updates neighbors)?
- Powered rail (ID 28): meta 0-5 straight only (no curves); bit 3 = powered state?
- Detector rail (ID 66): emits redstone signal when minecart is above?
- Activator rail (ID 67): exists in 1.0? (may have been added in 1.5 with hoppers/droppers)
- canBlockStay: requires solid block directly below?
- Breaking: drops itself (1 rail item)?
- What block IDs for the rail items: 66/67 as items too?

**Expected deliverable:** `Specs/BlockRail_Spec.md`

---

## WorldServer_MinecraftServer
[STATUS:PROVIDED]
**Needed for:** `Core/Engine.cs` — the main game loop is wired but never properly ticks the
server side: chunk loading for players, auto-save timing, and time-of-day progression are stubs.
Several `IWorld` methods return stubs (e.g. `GameTime` is never incremented).

**Questions:**
- `MinecraftServer` or `Minecraft` (SP) — what manages the main tick loop?
- Game time (`ry.j` / `worldTime`): incremented by 1 per tick? Field name on World?
- Auto-save: does the server call `WorldInfo.setSaveVersion`, `SaveHandler.saveWorld`, and
  `SaveHandler.saveAllChunks` on a timer? What interval in ticks?
- Weather: `ry.e`/`f` (rainingStrength/thunderingStrength) — what randomises rain start/stop?
  Duration formula: rain for 12000–24000 ticks, then clear for 12000–180000 ticks?
- `WorldInfo` fields `rainTime`/`thunderTime`: countdown timers?
- Moon phase: derived from `(worldTime / 24000) % 8`?
- Difficulty changes: stored in WorldInfo or per-server config?
- Spawn point: set on first world creation to the first grass block above Y=64 near origin?

**Expected deliverable:** `Specs/WorldServer_Spec.md` — game-time tick, auto-save timer,
weather randomisation formula, spawn-point generation, moon phase formula.

---

## LivingEntity_Drowning_Fire
[STATUS:PROVIDED]
**Needed for:** `Core/LivingEntity.cs` — drowning and fire damage are currently stubs.
`Entity.cs` has `FireTicks` but the damage callback is never called. `AirSupply` is never
decremented.

**Questions:**
- Air supply (`ia.aS` or similar field): starting value? 300 ticks?
- Drowning tick logic in `nq.g()` (or wherever it lives):
  - Decrements air by 1 per tick when head block is water/lava?
  - At air == -20: deal 2 drowning damage and reset air to 0?
  - Recovers 5 air per tick when not submerged?
  - `Water Breathing` potion suppresses this completely?
- Fire damage tick: at what interval is `DamageSource.OnFire` damage applied? (1 damage / 1 tick?)
- Fire extinguish: water block extinguishes fire immediately?
- Fall damage: `Entity.fallDistance` — at what threshold does fall damage start? Formula?
  - `fallDistance > 3.0F` → `(fallDistance - 3.0F)` rounded damage?
  - `Jump Boost` potion reduces effective fall distance?
- Armour absorbs fall damage? Fire damage?

**Expected deliverable:** `Specs/LivingEntity_Survival_Spec.md` — drowning, fire damage tick,
fall damage formula, extinguish conditions.

---

## Entity_Physics_Move
[STATUS:PROVIDED]
**Needed for:** `Core/Entity.cs` — the `Move(dx, dy, dz)` method (sweep collision) is one of
the most critical simulation paths. It exists but several details are unconfirmed:
- Horizontal clip-up step (0.5-block auto-step for walking up blocks)
- Ladder/vine climb detection
- Entity push out of blocks (suffocation in wall)
- Slipperiness factor per-block (BlockIce = 0.98F)

**Questions:**
- `ia.b(float dx, float dy, float dz)` — the main movement method:
  - Does it call `world.a(entity, aabb.expand(dx,dy,dz))` to get list of block AABBs?
  - Clip-up step (stair-like): after horizontal collision, tries `dy = 0.5F`, re-tests?
  - What is the exact clip-up step height — 0.5F always, or `entity.stepHeight`?
  - After movement: updates `onGround`, `isCollidedHorizontally`, `isCollidedVertically`?
- Ladder/vine climb: how is `isOnLadder()` checked — specific block IDs (65, 106)?
  - When on ladder: clamp upward/downward velocity?
  - Sneak on ladder: hold position (no fall)?
- Slipperiness: where is `block.slipperiness` applied? In the entity tick? In `ia.b()`?
  - Default slipperiness: 0.6F; ice: 0.98F?
  - Formula: `motionX *= slipperiness * 0.91F`?
- Suffocation in blocks: is `isEntityInsideOpaqueBlock()` checked per-tick? What happens?
- Entity-entity push: are entities pushed apart when overlapping? In `ia` or `ry`?

**Expected deliverable:** `Specs/Entity_Physics_Spec.md` — full `ia.b()` sweep algorithm,
clip-up step, ladder/vine logic, slipperiness, suffocation check, entity-entity push.

---

## Rendering_BlockModel
[STATUS:PROVIDED]
**Needed for:** `Graphics/` — the renderer currently uses a simple face-culling approach.
Several block types need non-standard rendering that goes beyond simple 6-face cube rendering:
fences, panes, glass, slabs, stairs, crossed-face plants, torches, levers, etc.
This is a render-layer spec, not a Core spec.

**Questions:**
- `RenderBlocks` class (obf: `bx` or `jh`) — is there one method per special render type?
- Render type IDs: which integer (0-25?) maps to which block shape?
  - 0 = full cube?
  - 1 = cross/X sprite (plants, sapling, mushroom, flower)?
  - 2 = torch (leaning 4 directions + upright)?
  - 3 = fire (animated multi-face)?
  - 4 = fluid (variable height)?
  - 5 = redstone wire (L/T/+/straight thin quad)?
  - 6 = crops (multi-hash pattern)?
  - 7 = door (thin panel with UV rotation)?
  - 8 = ladder (flat face against wall)?
  - 9 = rail (flat ground quad, curved variants)?
  - 10 = stairs (two-AABB composite)?
  - 11 = fence (dynamic post + arms)?
  - 12 = lever (stick + base)?
  - 13 = cactus (inset faces)?
  - 14 = bed (low flat model)?
  - 15 = vine (multi-face on wall/ceiling)?
  - 16 = repeater (flat base + two torches)?
  - 17 = piston + extension?
  - 18 = glass pane / iron bars (thin cross)?
  - 19 = lilypad (flat ground)?
  - 20 = cauldron (hollow vessel)?
  - 21 = brewing stand (rod + base)?
  - 22 = end portal frame (with/without eye)?
  - 23 = dragon egg (stepped pyramid)?
  - 24 = cocoa pod (angled bump)?
  - 25 = enchantment table (low, open-book animation handled by TESR)?
  - Any custom TESR (TileEntitySpecialRenderer) for chests, enchantment table, signs?
- How does `Block.getRenderType()` return the render type integer?
- For cross/X sprites: two quads crossed at 45°? UV mapped from texture atlas?
- For fluid: variable-height quads using `getFluidLevel`?

**Expected deliverable:** `Specs/Rendering_BlockModel_Spec.md` — full render type integer table,
one-paragraph description per render type, which block IDs use each type, TESR list.


---

## MineshaftPieces
[STATUS:PROVIDED]
**Needed for:** `Core/WorldGen/MapGenMineshaft.cs` — the three mineshaft piece classes
(`aba` MineshaftCorridor, `ra` MineshaftCrossing, `id` MineshaftStaircase) are stubbed.
The corridor is the most complex (70% chance), the crossing is a 4-way junction (10%),
and the staircase is a descending segment (20%). `uk` (MineshaftStartPiece) is also a
stub — it may be identical to the corridor but with `isMain=true`.

**Questions:**
- `aba` MineshaftCorridor — bounding box size? Spec notes "wooden support every 5 blocks":
  what exactly is placed (planks, fence posts, log cross-beams)?
  Rail (ID 66) runs along the centre — full length or only when above ground?
  Cobwebs (ID 30): random placement frequency?
  Cave-spider spawner: `isMain+1/23` — what does `isMain` mean and where is it stored?
  Chest wagon loot: 1% per support — what items? Same table as dungeon?
  Torch placement: every N blocks? On wall or ceiling?
- `ra` MineshaftCrossing (4-way junction) — bounding box? Pillar/ceiling details?
  Does it always generate 4 exits or conditionally?
- `id` MineshaftStaircase — bounding box? How many blocks does it descend per piece?
  Stair geometry: actual BlockStairs (ID 53) or just air-carved diagonal?
- `uk` MineshaftStartPiece — is it identical to `aba` with `isMain=true`, or a different layout?
- Depth limit: classes.md says max depth=8. Is this checked per-piece or globally?
- Radius limit: classes.md says radius<=80. How is this measured (from start piece)?
- All pieces: do they call AdjustToGround() like village pieces, or generate at fixed Y?
- Torches, rails, chest wagons — any metadata details (torch facing, rail direction)?

**Expected deliverable:** `Specs/MineshaftPieces_Spec.md` — full bounding box and block
placement logic for `uk`/`aba`/`ra`/`id`; loot table; torch/rail/cobweb frequency; depth
and radius limits; `isMain` semantics.

---

## IWorldAccess
[STATUS:PROVIDED]
**Needed for:** `Core/World.cs` — `world.notifyRenderListeners` is stubbed throughout
the codebase (6+ call sites) with the comment `// bd spec pending`. `bd` is an interface
that the world notifies when blocks change, light updates, or entities are added/removed.
In the client, this drives chunk mesh rebuilds and particle/sound events.

**Questions:**
- `bd` interface — what methods does it declare?
  - `a(int x, int y, int z, int oldBlockId, int newBlockId)` — block changed?
  - `a(int x1, int y1, int z1, int x2, int y2, int z2)` — mark dirty range?
  - `b(...)` — light update notification?
  - Entity add/remove methods?
- How does `ry` (World) hold listeners — a `List<bd>` field? What is the field name?
- What method on `ry` adds/removes a listener? (e.g., `ry.a(bd)` / `ry.b(bd)`)
- `ry.notifyBlockChange(x, y, z, oldId, newId)` — calls `bd.a()` on all listeners,
  then also calls `notifyNeighbors(x, y, z, newId)`? Or separate?
- Which call sites in `ry` fire which `bd` method?
  - `setBlock` fires block-change notify?
  - `setBlockMetadata` fires block-change notify?
  - Light propagation fires light-update notify?
  - `addEntity`/`removeEntity` fire entity notify?
- On the client, is `bd` implemented by the `RenderGlobal` / `WorldRenderer` class?
  Does it rebuild chunk display lists on `a(x,y,z,...)`?

**Expected deliverable:** `Specs/IWorldAccess_Spec.md` — full `bd` interface method
signatures, semantics of each, how `ry` holds and notifies listeners, all call sites.

---

## SoundManager
[STATUS:PROVIDED]
**Needed for:** Multiple stubs across `Core/Blocks/` — `BlockFluid.cs`, `BlockFluidBase.cs`,
`BlockPortal.cs` etc. reference "sound spec pending". Sound calls appear as
`world.playSoundAtEntity(...)`, `world.playSound(x,y,z,name,vol,pitch)`,
`world.playAuxSFX(eventId, x,y,z,data)`. None of these are wired up.

**Questions:**
- `ry.a(ia, String name, float vol, float pitch)` — `playSoundAtEntity` — exact signature?
- `ry.a(double x, double y, double z, String name, float vol, float pitch)` — `playSound`?
- `ry.w(String, double x, double y, double z, float vol, float pitch)` — variant?
- `ry.b(int eventId, int x, int y, int z, int data)` — `playAuxSFX` / `playWorldEvent`:
  what event IDs are defined in 1.0? (1000=click, 1001=door, 1002=fire-extinguish,
  1003=record, 1004=bow, 1005=jukebox-insert, 2000=smoke, 2001=block-break, 2002=potion,
  2003=eye-of-ender — are these all correct?)
  What data value does each event use?
- Sound name strings — are they resource-path keys like "random.fizz", "step.wood", etc.?
  What string format exactly?
- Volume and pitch conventions: 1.0F = full volume, 2.0F louder? Pitch 1.0 = normal, 0.5 = half speed?
- Is there a SoundManager or SoundPool class separate from World?
  Or does World dispatch directly to the audio system?
- `world.playSoundEffect(x,y,z,name,vol,pitch)` — same as `playSound`?
  Or does `playSoundEffect` skip sending to the server?

**Expected deliverable:** `Specs/SoundManager_Spec.md` — all sound-call signatures on
`ry`, full `playAuxSFX` event ID table with data semantics, sound name string format,
volume/pitch conventions, server vs. client-only distinction.

---

## ParticleSystem
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockFluidBase.cs` — "Play random.fizz + 8 largesmoke
particles — stub (sound/particle spec pending)". Also referenced in enchantment table
block, portal, explosion, and fluid lava-water contact.

**Questions:**
- `ry.a(String particleName, double x, double y, double z, double vx, double vy, double vz)`
  — is this the main particle spawn method? Exact method signature and obfuscated name?
- Particle name strings in 1.0 — what names are valid?
  ("largesmoke", "smoke", "flame", "portal", "enchantmenttable", "explode",
  "blockcrack_N", "blockdust_N", "crit", "largeSplash", "splash", "bubble",
  "reddust", "snowballpoof" — which are present in 1.0?)
- For BlockFluidBase lava-water contact: exactly how many particles? Which names?
  Velocity distribution (random spread)?
- For BlockEnchantmentTable random-tick: particle fired toward bookshelf?
  Velocity toward bookshelf position, or random? How many per tick?
- For portal block: "4 portal particles per tick" — velocity? Position?
- Client-only: are particle spawns skipped on the dedicated server? How?
- EffectRenderer class (if exists) — obfuscated name?

**Expected deliverable:** `Specs/ParticleSystem_Spec.md` — main particle spawn method
signature, full particle name table with visual descriptions, velocity conventions,
server/client filtering, per-block usage examples.

---

## WorldGenLakes
[STATUS:PROVIDED]
**Needed for:** `Core/WorldGen/` — lake generation (water pools and lava pools) is part
of chunk population but no WorldGenLakes class exists yet. BiomeDecorator specced
springs (ib) but lakes are larger depressions filled with water/lava.

**Questions:**
- Is there a WorldGenLakes class (separate from WorldGenSpring)? Obfuscated name?
- How does lake generation work geometrically? Irregular blob carved into terrain?
  - Reads 2D noise or uses a fixed radius?
  - Carves air then fills with fluid?
  - Any minimum depth/width constraints?
- Water lake vs. lava lake — same generator with different block IDs?
  At what Y-range does each type generate?
- How often does each type appear per chunk (nextInt frequency)?
- Does a lake require solid blocks below? Any neighbor checks before placing?
- Does lava lake ignite adjacent flammable blocks?
- Is this called in ChunkProviderGenerate.b() (populateChunk) directly, or via biome decorator?
- What is the call order relative to ores, dungeons, and trees?

**Expected deliverable:** `Specs/WorldGenLakes_Spec.md` — obfuscated class name, full
algorithm, geometry, Y-range per fluid type, frequency, call site in terrain gen.

---

## BlockDispenser
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/` — BlockDispenser (`cu`, ID 23) is registered in BlockRegistry
but the dispense logic (what happens per item type when powered by redstone) is not specced.
TileEntityDispenser is already implemented (9 slots, NBT). The block behavior — selecting
a slot, firing/placing/spawning per item — is the missing part.

**Questions:**
- `cu.a(ry, int x, int y, int z)` — the dispense method: exact algorithm?
  - Picks a random non-empty slot? Or first non-empty?
- Per-item dispatch table in 1.0 — what does the dispenser do with each item?
  - Arrow (ID 262) → spawn EntityArrow?
  - Snowball (ID 332) → spawn EntitySnowball?
  - Egg (ID 344) → spawn EntityEgg?
  - Fire charge (ID 385) — exists in 1.0? → spawn EntitySmallFireball?
  - Bucket water (ID 326) → place water block in front?
  - Bucket lava (ID 327) → place lava block in front?
  - Empty bucket (ID 325) → pick up fluid block in front?
  - Flint and steel (ID 259) → place fire block in front?
  - Bone meal (ID 351 meta 15) → apply bonemeal effect?
  - Default (unknown item) → drop as EntityItem?
- Facing: meta bits 0-2 = facing (same as piston)?
- Projectile velocity from dispenser — same as player throw, or different?
- Sound: plays a dispense sound on fire? "click" when empty?

**Expected deliverable:** `Specs/BlockDispenser_Spec.md` — full `cu` dispense algorithm,
complete per-item dispatch table for 1.0, facing metadata layout, projectile velocity,
sound events, empty-dispenser behavior.

---

## BlockCocoaPlant
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/` — render type 24 (cocoa pod) is handled by RenderBlocks
but there is no BlockCocoaPlant implementation. ID 127 is listed in BlockRegistry.
The block has 3 growth stages and attaches to jungle log faces.

**Questions:**
- Obfuscated class name?
- Metadata layout:
  - Bits 0-1 = facing (N/S/E/W direction of the jungle log face)?
  - Bits 2-3 = growth stage (0=small, 1=medium, 2=large/ripe)?
- Bounding box: each stage has a different size — exact AABB values per stage?
- canBlockStay: must be adjacent to jungle log (ID 17 with meta bits 0-1 = 3)?
  Checks all 4 horizontal faces or only the stored facing?
- Random tick growth probability (nextInt(N)==0 — what N)?
- Drops:
  - Stage 0/1: nothing?
  - Stage 2: cocoa beans (dye ID 351 meta 3), how many (2-3)?
  - Fortune modifier?
- Hardness value?

**Expected deliverable:** `Specs/BlockCocoaPlant_Spec.md` — metadata layout, AABB per
stage, canBlockStay, growth tick, drops table, hardness.

---

## GameMode
[STATUS:PROVIDED]
**Needed for:** `Core/EntityPlayer.cs` — block-breaking mechanics are stubs.
`ItemInWorldManager` handles breaking progress (animation frames 0-9), reach distance,
and attack cooldown. No ItemInWorldManager exists in Core.

**Questions:**
- ItemInWorldManager — obfuscated class name? Fields?
  - curBlockX/Y/Z — coordinates of block being broken?
  - curBlockDamage — float [0.0, 1.0] breaking progress?
  - blockHitDelay — attack-swing cooldown in ticks?
  - isDestroyingBlock — boolean flag?
- Block-breaking tick: progress += miningSpeed / hardness / (underwater?5:1) / (onGround?1:5)?
  Completes at breakProgress >= 1.0F?
- Attack swing: cooldown on blockHitDelay — how many ticks?
- Reach distance: survival = 4.5 blocks, creative = 5.0?
- EnumGameType (or equivalent) — values in 1.0? (0=SURVIVAL, 1=CREATIVE, others?)
- Creative mode specifics:
  - Instant block break?
  - No durability loss?
  - Flight enabled (PlayerAbilities.allowFlying=true)?
  - No hunger?
  - Infinite items?
- Where is game mode stored — in EntityPlayer, WorldInfo, or on the server?

**Expected deliverable:** `Specs/GameMode_Spec.md` — ItemInWorldManager fields and
breaking-tick algorithm, reach distance values, EnumGameType values, creative mode flags,
block-break animation frames 0-9 timing.

---

## RenderManager
[STATUS:PROVIDED]
**Needed for:** `Graphics/` — entity rendering is not wired up. RenderManager dispatches
rendering to per-entity renderer classes, mapping entity class to renderer instance and
calling `doRender(entity, x, y, z, yaw, partialTick)`.

**Questions:**
- RenderManager — obfuscated class name? Singleton?
- entityRenderMap — type? HashMap<Class, Renderer>? How populated?
- What renderers exist in 1.0? (one per entity type)
  - RenderPlayer for EntityPlayer?
  - RenderBiped for zombie/skeleton?
  - RenderSpider, RenderCreeper, RenderGhast, RenderSlime?
  - RenderSnowball for throwables?
  - RenderItem for dropped items (EntityItem)?
  - RenderArrow, RenderBoat, RenderMinecart, RenderPainting, RenderXPOrb?
  Full list with obfuscated names.
- `doRenderEntity(entity, partialTick)` — translates to entity position interpolated by
  partialTick, calls the matching renderer?
- Shadow: flat shadow drawn under entities? Radius formula?
- Name tags — when rendered above entities?

**Expected deliverable:** `Specs/RenderManager_Spec.md` — obfuscated class name,
entity-renderer map, `doRenderEntity` call flow, full renderer list with obfuscated
names, shadow and name-tag rules.

---

## EntityRenderer
[STATUS:PROVIDED]
**Needed for:** `Graphics/` — the main game view is not wired. EntityRenderer handles
the camera, frustum, fog, sky, and render sequence (terrain → entities → particles →
weather → hand). This spec covers the render pipeline from `renderWorld(partialTick)` down.

**Questions:**
- EntityRenderer — obfuscated class name?
- `renderWorld(float partialTick)` call sequence:
  1. Clear buffers?
  2. Set up projection matrix (FOV, near=0.05, far=?)?
  3. Apply camera transform (player eye + head rotation interpolated)?
  4. Frustum cull?
  5. Render sky (sky colour, void colour)?
  6. Render terrain opaque pass?
  7. Render entities (RenderManager)?
  8. Render terrain transparent pass (water/glass)?
  9. Render particles?
  10. Render weather (rain/snow)?
  11. Render hand/held item?
  12. Render GUI overlay (HUD)?
- Camera position: player eye at posY + eyeHeight, interpolated by partialTick?
- FOV: default 70? Modified by speed potion / sprinting?
- Fog: distance fog start/end — formula in overworld vs. Nether vs. End?
  Nether red fog at short distance? End dark sky, no fog?
- Sky: day colour (0.5, 0.66, 1.0?), night colour, sunrise/sunset?
  Sun/moon rendered as flat billboard quads?
- Void fog (Y<32): darkens screen? How?
- Rain/snow overlay: directional particles streaking downward?
- Hand offset from camera: FOV 87 for hand? Depth-tested separately?

**Expected deliverable:** `Specs/EntityRenderer_Spec.md` — full renderWorld call sequence
with numbered steps, camera setup, FOV formula, fog start/end per dimension, sky colour
values, sun/moon quad geometry, void fog threshold, hand rendering transform.

---

## FontRenderer
[STATUS:PROVIDED]
**Needed for:** `Graphics/` — text rendering is needed for signs (TileEntitySign),
chat, GUI labels, and item tooltips. No FontRenderer class exists in Graphics/ yet.

**Questions:**
- FontRenderer — obfuscated class name?
- Texture source: `font/default.png` in the JAR? Dimensions? How many chars per row?
  ASCII (128 chars) or extended?
- Character width: fixed (8px) or variable with a width array?
  If variable: how are widths determined?
- `drawString(String text, int x, int y, int colour)` — left-aligned, no shadow?
- `drawStringWithShadow(String text, int x, int y, int colour)` — +1/+1 dark shadow?
- Colour codes: section-sign (S0-Sf) for chat colours? Bold/italic/reset in 1.0?
- `getStringWidth(String text)` — width in pixels?
- Line height: 9px (8px glyph + 1px gap)?
- For signs: max 4 lines, 15 chars — enforced by renderer or by the block?
- OpenGL state: 2D orthographic projection? Alpha test enabled?

**Expected deliverable:** `Specs/FontRenderer_Spec.md` — texture layout, character width
method (fixed or variable), all draw methods, colour code table, line height, sign
rendering rules, OpenGL state.

---

## GuiScreen
[STATUS:PROVIDED]
**Needed for:** `Graphics/` — the GUI system (inventory, main menu, pause screen, HUD)
has no implementation. GuiScreen is the base for all overlay screens. GuiIngame (HUD)
displays health, hunger, armor, XP bar, crosshair, and hotbar.

**Questions:**
- GuiScreen — obfuscated class name? Key virtual methods?
  - initGui() — called on open/resize?
  - drawScreen(mouseX, mouseY, partialTick)?
  - keyTyped(char c, int key)?
  - mouseClicked(x, y, button)?
  - onGuiClosed()?
  - doesGuiPauseGame()?
- Button system (GuiButton) — click detection, hover highlight?
- GuiIngame (HUD) — elements and screen positions?
  - Crosshair at centre?
  - Hotbar (9 slots) at bottom centre — slot size 20px?
  - Health bar (10 hearts) above hotbar left?
  - Hunger bar (10 chicken legs) above hotbar right?
  - Armor bar above health when armor > 0?
  - XP bar above hotbar centre?
  - Air bubbles when underwater?
  Exact pixel offsets (assumes 320x240 base scaled by scaleFactor)?
- GUI scale: scaleFactor — calculated from window size? Values 1/2/3/4?
- Texture source: gui/gui.png, gui/icons.png — dimensions and atlas layout?
- `drawTexturedModalRect(x, y, u, v, w, h)` — 2D blitted quad from GUI texture?
- Screen stack: only one screen at a time, or can multiple be open?

**Expected deliverable:** `Specs/GuiScreen_Spec.md` — GuiScreen base methods, GuiIngame
HUD element positions and texture sources, button system, scale factor formula, GUI
texture atlas layout, screen stack behaviour.

---

## EntityPlayerSP / ServerPlayer
[STATUS:PROVIDED]
**Needed for:** `Core/EntityPlayerSP.cs` — concrete server-side player class.
`EntityPlayer` (`vi`) is abstract; the game needs a concrete subclass that handles
respawn, block interaction dispatch, item use, and eating. Obfuscated name unknown.

**Questions:**
- What is the obfuscated class name for the concrete single-player entity?
  (Search tip: class that extends `vi`, is instantiated when a world is loaded,
  and overrides `onUpdate`/`b()` with hunger, sleep, XP, or respawn logic.)
- Does it extend `vi` directly or through an intermediate class?
- Key method overrides: which of these exist and what do they do?
  - Per-tick logic (`b()` / `onUpdate`) — hunger tick, sleep, XP gain?
  - Death / respawn — does it clear inventory, reset position, call `vi.respawnPlayer`?
  - `travelToDimension(int)` — portal dimension switch?
  - `openContainer(Container)` / `closeContainer()` — inventory screen open/close?
- How does block left-click (break) dispatch server-side?
  Which method is called on the player or a helper class?
- How does block right-click (place/interact) dispatch?
  Does the player call `Block.onBlockActivated` or go through a helper?
- Any fields on the concrete class not in `vi`?

**Expected deliverable:** `Specs/EntityPlayerSP_Spec.md`

---

## ItemInWorldManager (Block Breaking Progress)
[STATUS:PROVIDED]
**Needed for:** `Core/ItemInWorldManager.cs` — block-breaking progress tracker.
`EntityPlayer.StartUsingItem` is a stub. This class was not found in the first
analyst session (flagged in `GameMode_Spec.md`); a second targeted search is needed.

**Questions:**
- Obfuscated class name? Search hints: contains fields for current target block
  coordinates (curBlockX/Y/Z), a float break-progress counter (0.0 to 1.0),
  and a method that advances progress each tick based on mining speed.
- Fields: exact names/types for target block, progress counter, tick counter?
- Per-tick advance formula: `progress += miningSpeed / hardness / 30`? Or different?
- At progress >= 1.0: which method breaks the block, drops items, resets state?
  Does it call `world.setBlock(x,y,z,0)` or go through a dedicated break method?
- Creative mode path: instabuild=true → single tick, skip progress?
- Block crack animation stages 0-9: what calls `world.markBlockRangeForRenderUpdate`
  or a similar visual-update method for the crack overlay?
- Reset conditions: player moves too far, switches tool, block changes externally?
- How is this class held — field on the player, or standalone ticked separately?

**Expected deliverable:** `Specs/ItemInWorldManager_Spec.md`

---

## WorldServer SpawnSearch and Chunk Preloading
[STATUS:PROVIDED]
**Needed for:** `Core/World.cs` spawn initialization and `Core/Engine.cs` startup.
Currently spawn is hardcoded to (0, 64, 0). The original searches for a valid grass
surface and pre-loads 25 spawn chunks.

**Questions:**
- Which class/method runs the spawn search at world creation?
  Is it on `si` (WorldInfo), `ry` (World), or a dedicated WorldServer subclass?
- Search algorithm: random walk from (0,0,0) or deterministic scan?
  Validity condition: grass block (ID 2) on top? Not over water? Min Y?
- How many attempts before defaulting?
- Spawn chunk preloading: confirm 5x5 = 25 chunks preloaded synchronously.
  Which method drives this? Progress bar on the loading screen?
- `si` (WorldInfo) spawn fields: "SpawnX", "SpawnY", "SpawnZ" confirmed in level.dat?
  Is there a `spawnRadius` or equivalent that keeps spawn chunks loaded?
- Does `jz` (ChunkProviderServer) have a "force-loaded" set that prevents
  spawn chunks from being unloaded? If so, how is it populated?

**Expected deliverable:** `Specs/WorldSpawn_Spec.md`

---

## Chunk Loading Radius Loop
[STATUS:PROVIDED]
**Needed for:** `Core/ChunkProviderServer.cs` + `Core/Engine.cs` game loop.
Currently only chunk (0,0) is generated. Need the per-tick loop that generates
all chunks within render distance around each player.

**Questions:**
- Which class drives the per-tick chunk-load loop? (`MinecraftServer`, `WorldServer`,
  or a method on `jz` ChunkProviderServer itself?)
- Loop pattern: spiral out from player chunk position to render distance (10 chunks)?
  Or rectangular? Exact iteration order?
- Budget per tick: max new chunks generated before deferring the rest?
- Method signature: `jz.b(vi player)` or similar — what does it do exactly?
  Does it call `jz.loadChunk` / `provideChunk` for each position in range?
- Population trigger timing: does loading trigger population immediately, or
  is it deferred until all 4 quadrant-neighbours are loaded?
- Unloading: `jz.unloadChunksIfNotNearSpawn` or similar — cadence and distance?

**Expected deliverable:** `Specs/ChunkLoadingLoop_Spec.md`

---

## BlockCocoaPlant (Second Attempt)
[STATUS:PROVIDED]
**Needed for:** `Core/Blocks/BlockCocoaPlant.cs`. Class not found in first session.
The existing `BlockCocoaPlant_Spec.md` documents Cauldron/BrewingStand instead.

**Search hints:**
- Block ID 127. Attaches to side face of jungle log (ID 17, meta 3).
- 3 growth stages. Metadata likely encodes stage (2 bits) + facing direction (2 bits).
- `canBlockStay` must check adjacent jungle log.
- Drops cocoa beans (`acy.aX`?) — quantity by stage.
- Custom AABB smaller than full block, oriented to one of 4 faces.
- Search decompiled output for a class with these characteristics.

**Questions:**
- Obfuscated Java class name?
- Exact metadata bit layout (stage bits, facing bits)?
- Growth random-tick probability (standard 1/25 or different)?
- AABB dimensions for each stage and each facing direction?
- Item drop counts: stage 0, 1, 2?
- Block ID confirmed 127 in 1.0?

**Expected deliverable:** `Specs/BlockCocoaPlant_Spec.md` (new dedicated file,
replacing the Cauldron/BrewingStand content which belongs in a separate spec)

---

## Minecraft Main Class and Input Loop
[STATUS:PROVIDED]
**Needed for:** `Core/Engine.cs` — understanding vanilla game loop structure
to correctly wire SpectraEngine input → player action → world mutation.

**Questions:**
- Obfuscated class name for the main `Minecraft` singleton class?
- Game loop: single-threaded or separate game/render threads?
- Fixed-rate tick: 20 Hz timer-based, or frame-coupled with catchup?
- Order of operations per frame: input → ticks → render, or interleaved?
- `Minecraft.thePlayer` field: type — `EntityPlayerSP` or `vi` (EntityPlayer)?
- Timer class (obf `z`?): fields `elapsedTicks` (int) and `renderPartialTicks` (float)?
  How is `renderPartialTicks` computed for smooth interpolation?
- Right-click block placement end-to-end:
  Mouse input → which method → `ItemBlock.onItemUse`?
- Left-click block breaking end-to-end:
  Mouse input → `ItemInWorldManager.onPlayerDamageBlock` (or equivalent)?
- `Minecraft.currentScreen` field: null during gameplay, GuiScreen when open?
- How are key bindings stored — hardcoded or a `KeyBinding` registry?

**Expected deliverable:** `Specs/MinecraftMain_Spec.md`

---

## GuiIngame HUD Texture Layout
[STATUS:PROVIDED]
**Needed for:** `Graphics/GuiScreen.cs` — `GuiIngame.RenderGameOverlay` is a stub.
Need exact UV coordinates from `gui/icons.png` and `gui/gui.png`.

**Questions:**
- `gui/icons.png` dimensions and atlas UV coordinates for:
  - Crosshair?
  - Full/half/empty heart? (health bar)
  - Full/half/empty hunger icon (drumstick)?
  - Full/half/empty armor icon (chestplate)?
  - Air bubble / empty bubble?
  - XP bar background strip and filled strip?
  - Hotbar selection highlight box?
- `gui/gui.png` layout:
  - Hotbar strip UV and pixel size?
  - Individual slot positions within hotbar?
- Screen coordinate system (320×240 base × scaleFactor):
  - Hotbar Y offset from bottom?
  - Health bar position relative to hotbar?
  - Hunger bar position?
  - XP bar position?
  - Crosshair: exact screen centre?
- `drawTexturedModalRect(int x, y, u, v, w, h)` GL state requirements?
- Which texture is bound for icons.png vs. gui.png rendering?

**Expected deliverable:** `Specs/GuiIngameHUD_Spec.md`

---

## PlayerMovement — EntityPlayerSP Movement Physics
[STATUS:PROVIDED]
**Needed for:** `Core/EntityPlayerSP.cs` — `MovementInput` is applied via `AiForward`/`AiStrafe`
but the exact movement speed values, sprint multiplier, sneak multiplier, and jump horizontal
boost have not been sourced from the decompile. Currently using LivingEntity's default
`GroundSpeed = 0.1f` which may not match the player-specific path in `di`.

**Questions:**
- Does `di` (EntityPlayerSP) override `GroundSpeed` / `AirSpeed`, or does it call a different
  movement method than `LivingMove`?
- Player walking speed constant: is it still `0.1f` on-ground, or overridden to a different value?
- Sprinting speed: what multiplier is applied to `AiForward` when sprinting? (vanilla: ×1.3)
- Sneak speed: what factor is applied when `IsSneaking`? (vanilla: ×0.3 of normal)
- Jump horizontal boost when sprinting: does `Jump()` add extra horizontal impulse?
  What exact value? (vanilla: 0.2 × sin/cos of yaw)
- Is there a separate "moveEntityWithHeading" path for the player that differs from mob AI?
  Obfuscated method name?
- Sprint food-drain: does sprinting deplete food (`FoodStats.FoodExhaustion`) — what amount/tick?
- Sprint conditions: what fields control whether sprinting is possible?
  (food > 6 AND not sneaking AND moving forward — confirm)

**Expected deliverable:** `Specs/PlayerMovement_Spec.md`

---

## PlayerMovement — Mouse Look and Camera
[STATUS:PROVIDED]
**Needed for:** `Core/Engine.cs` — `RotationYaw`/`RotationPitch` are currently updated with a
placeholder sensitivity of 0.15f. Need the exact vanilla yaw/pitch update path and pitch clamp.

**Questions:**
- Where does vanilla read mouse delta? Is it in `Minecraft.x()` (the frame method) or in `di.e()`?
  Obfuscated method name for mouse-look update?
- Pitch clamp: vanilla clamps to [−90, +90] — confirmed?
- Sensitivity formula: is there a `GameSettings.mouseSensitivity` (obf: `ki.M`) field?
  What is the multiplier formula? (vanilla: `(sensitivity * 0.6 + 0.2)^3 * 8 * 0.15`)
- Yaw wraps to [0, 360] or is it unbounded? Does vanilla normalise it?
- Does vanilla invert Y by default? Is there an `invertMouse` setting field on `ki`?

**Expected deliverable:** `Specs/MouseLook_Spec.md` (can be a section in `MinecraftMain_Spec.md`)

---

## GuiInventory — Inventory Screen Layout
[STATUS:PROVIDED]
**Needed for:** `Graphics/GuiScreen.cs` — pressing E should open the player inventory.
Need slot positions, 2×2 crafting grid, and armour slot layout.

**Questions:**
- Obfuscated class name for the player inventory screen? (`gd` ContainerPlayer + what GuiScreen subclass?)
- How many slots total? (36 main + 4 armour + 4 crafting output + result = 45 slots?)
- UV source for the inventory background texture — is it `/gui/inventory.png`?
- Slot grid positions in the texture: top-left of each slot group?
- Armour slot positions (head/chest/legs/feet): exact screen XY in the inventory screen?
- Crafting 2×2 grid: top-left position?
- Output slot (result): position?
- Does pressing E (or the inventory key binding `ki.E`) open this screen?
  What is the key binding field name on `ki` (GameSettings)?

**Expected deliverable:** `Specs/GuiInventory_Spec.md`

---

## Raycast / Block Selection — MovingObjectPosition
[STATUS:PROVIDED]
**Needed for:** `Core/Engine.cs` — for block-break (left-click) and block-place (right-click)
the engine needs to raycast from the player's eye position in the look direction and return
the closest block face. `MovingObjectPosition` exists but the raycast call path is not wired.

**Questions:**
- What vanilla method casts the "object mouse over" ray? Obfuscated name in `Minecraft` or `EntityRenderer`?
  (Suspected: `Minecraft.k()` calls `u.b(world, player)` or similar — confirm)
- Max reach distance: 4.5 blocks (survival) / 5 blocks (creative)? Exact float value?
- Is the ray cast from `entity.PosX/Y+eyeHeight/Z` in the yaw/pitch direction?
  Exact direction vector formula from yaw/pitch?
- The result is stored in `Minecraft.z` (obf) — type `MovingObjectPosition`?
  How is block face (0–5) encoded in the result?
- Does the raycast call `Block.collisionRayTrace` or a world-level method?

**Expected deliverable:** `Specs/Raycast_Spec.md`

---

## Block Texture Rendering — Terrain Atlas UV Mapping
[STATUS:PROVIDED]
**Needed for:** `Core/Engine.cs` `BuildVoxelMeshes` and `LoadAssets` — currently only bridge
stub tiles are extracted from the terrain atlas. Core world blocks (Block.BlocksList) use
tile indices 0–255 from `terrain.png` but their textures are only registered if a matching
bridge stub happens to reference the same index. Blocks like gravel, sand, water, lava,
leaves, glass etc. are invisible in the world because their tile indices are never queued.

Additionally, UV mapping inside the 16×16 terrain atlas needs to be verified:
the atlas is a 256×256 PNG divided into 16×16 tiles (each tile = 16×16 px). The current
`TerrainAtlas.ExtractAndRegister` extracts a single tile as its own texture. This is
correct for individual block faces but we need to confirm the exact UV grid layout.

**Questions:**
- Exact terrain.png layout: 16×16 grid of 16×16-pixel tiles (indices 0..255 left-to-right,
  top-to-bottom)? Any special cases (animated tiles, overlays)?
- Which tile indices correspond to the most important 1.0 blocks?
  (stone=1, grass_top=0, dirt=2, grass_side=3, … complete list for blocks 1–49)
- Which blocks need biome tinting and which tint map do they use (grass/foliage)?
  Specifically: grass top (tile 0) = grass tint, leaves (tile 52/53) = foliage tint,
  grass side overlay = grass tint on the overlay portion only?
- How should water (tile 205/206) and lava (tile 237/238) handle animated frames?
  Are the first frame's tiles (205, 237) sufficient for a static rendering pass?
- Do any blocks use a different tile for top vs side vs bottom at the Core layer
  (not just the bridge stubs)? E.g. log, grass, piston, furnace?

**Expected deliverable:** `Specs/TerrainAtlas_Spec.md`
