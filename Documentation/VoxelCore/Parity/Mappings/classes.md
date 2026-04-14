# Class Name Mappings — Minecraft 1.0

Obfuscated name (as found in `temp/decompiled/`) → MCP/human-readable name.

> Source: inferred from class structure analysis. Not derived from any MCP release.

## Utility Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `c` | `AxisAlignedBB` | Axis-aligned bounding box; 6 double fields; static object pool; sweep collision; ray trace |
| `bo` | `EnumMovingObjectType` | Java enum; two constants: `a`=TILE (0), `b`=ENTITY (1) |
| `fb` | `Vec3` | 3D double vector; static object pool; geometric ops; segment-plane intersection |
| `gv` | `MovingObjectPosition` | Ray-cast result; block-hit or entity-hit constructor; face ID 0–5 |
| `yy` | `Block` | Block base class; static registry k[256]; 8 parallel arrays; builder pattern; virtual behaviour |
| `me` | `MathHelper` | Static trig/numeric utilities; sine table (65536 entries); floor, sqrt, clamp, abs, floor-div, RNG range |

## Core Game Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `yy` | `Block` | Base class + static block registry (IDs 1–122) |
| `acy` | `Item` | Base item class; d[32000] registry (items at index 256+id); bN=maxStackSize; bO=iconIndex |
| `acr` | `RenderBlocks` | Per-face OpenGL renderer, 5064 lines |
| `wu` | `Material` | Block material (stone, wood, cloth, …) |
| `zx` | `Chunk` | 16×16×128 chunk data |
| `ry` | `World` | World/level root object |
| `nq` | `LivingEntity` | 1257 lines; abstract; health aM; invulnerability ac/aq; potion effects bh; friction movement d() |
| `gy` | `ChunkLoader` | Reads/writes chunk `.dat` files |

## Block Subclasses

| Obfuscated | Human name | Block name string |
|---|---|---|
| `gm` | `BlockStone` | `"stone"` |
| `jb` | `BlockGrass` | `"grass"` |
| `agd` | `BlockDirt` | `"dirt"` |
| `aet` | `BlockSapling` | `"sapling"` |
| `ahx` | `BlockFluid` | `"water"` / `"lava"` (flowing) |
| `add` | `BlockStationary` | `"water"` / `"lava"` (still) |
| `cj` | `BlockSand` | `"sand"` |
| `kb` | `BlockGravel` | `"gravel"` |
| `v` | `BlockOre` | Gold/Iron/Coal/Diamond/Lapis/Redstone ore |
| `aip` | `BlockLog` | `"log"` |
| `qo` | `BlockLeaves` | `"leaves"` |
| `aho` | `BlockGlass` | `"glass"` |
| `ahh` | `BlockStairs` | Stair variants |
| `xs` | `BlockSlab` | `"stoneSlab"` |
| `abm` | `BlockTNT` | `"tnt"` |
| `bg` | `BlockTorch` | `"torch"` |
| `wj` | `BlockFire` | `"fire"` |
| `kk` | `BlockMobSpawner` | `"mobSpawner"` |
| `au` | `BlockChest` | `"chest"` |
| `kw` | `BlockRedstoneWire` | `"redstoneDust"` |
| `aha` | `BlockCrops` | `"crops"` |
| `ni` | `BlockFarmland` | `"farmland"` |
| `eu` | `BlockFurnace` | `"furnace"` |
| `afr` | `BlockRail` | `"rail"` / `"goldenRail"` |
| `ags` | `BlockDetectorRail` | `"detectorRail"` |
| `abr` | `BlockPiston` | `"pistonBase"` / `"pistonStickyBase"` |
| `acu` | `BlockPistonMoving` | piston arm |
| `fr` | `BlockCloth` | `"cloth"` (wool) |
| `wg` | `BlockFlower` | `"flower"` / `"rose"` |
| `js` | `BlockMushroom` | mushrooms |
| `rs` | `BlockMetalBlock` | Gold/Iron/Diamond block |
| `nz` | `BlockFence` | `"fence"` / `"netherFence"` |
| `nf` | `BlockPumpkin` | `"pumpkin"` / `"litpumpkin"` |
| `et` | `BlockNetherrack` | `"hellrock"` |
| `mq` | `BlockSoulSand` | `"hellsand"` |
| `sk` | `BlockGlowstone` | `"lightgem"` |
| `sc` | `BlockPortal` | `"portal"` |
| `aem` | `BlockCake` | `"cake"` |
| `mz` | `BlockRedstoneDiode` | `"diode"` |
| `aif` | `BlockSnow` | `"snow"` (layer) |
| `jk` | `BlockSnowBlock` | `"snow"` (block) |
| `ahq` | `BlockIce` | `"ice"` |
| `ow` | `BlockCactus` | `"cactus"` |
| `pc` | `BlockClay` | `"clay"` |
| `md` | `BlockReed` | `"reeds"` |
| `abl` | `BlockJukebox` | `"jukebox"` |
| `wd` | `BlockMushroomCap` | mushroom cap |
| `uh` | `BlockPane` | iron fence / thin glass |
| `of` | `BlockMelon` | `"melon"` |
| `pu` | `BlockStem` | pumpkin/melon stem |
| `ahl` | `BlockVine` | `"vine"` |
| `fp` | `BlockFenceGate` | `"fenceGate"` |
| `ez` | `BlockMycelium` | `"mycel"` |
| `qi` | `BlockLilyPad` | `"waterlily"` |
| `vy` | `BlockNetherWart` | `"netherStalk"` |
| `sy` | `BlockEnchantmentTable` | `"enchantmentTable"` |
| `ahp` | `BlockBrewingStand` | `"brewingStand"` |
| `ic` | `BlockCauldron` | `"cauldron"` |
| `aid` | `BlockEndPortal` | end portal (invisible) |
| `rl` | `BlockEndPortalFrame` | `"endPortalFrame"` |
| `aci` | `BlockDragonEgg` | `"dragonEgg"` |
| `jh` | `BlockStoneBrick` | `"stonebricksmooth"` |
| `vf` | `BlockSilverfish` | silverfish block |
| `mf` | `BlockTrapDoor` | `"trapdoor"` |
| `vj` | `BlockLockedChest` | `"lockedchest"` |
| `uc` | `BlockDoor` | `"doorWood"` / `"doorIron"` |
| `afu` | `BlockLadder` | `"ladder"` |
| `aaa` | `BlockLever` | `"lever"` |
| `oc` | `BlockOreRedstone` | `"oreRedstone"` |
| `ku` | `BlockRedstoneTorch` | `"notGate"` |
| `mf` | `BlockButton` | pressure plate / button |
| `cu` | `BlockDispenser` | `"dispenser"` |
| `aat` | `BlockSandStone` | `"sandStone"` |
| `yq` | `BlockNote` | `"musicBlock"` |
| `aab` | `BlockBed` | `"bed"` |
| `kv` | `BlockTallGrass` | `"tallgrass"` |
| `jl` | `BlockDeadBush` | `"deadbush"` |
| `ay` | `BlockBookshelf` | `"bookshelf"` |
| `ain` | `BlockObsidian` | `"obsidian"` |
| `rn` | `BlockWorkbench` | `"workbench"` |
| `mr` | `BlockSign` | `"sign"` |

## Material Classes

> **Correction:** `wu` is StepSound, not Material. `p` is Material. `bj`/`aeg` extend `wu` (StepSound), not Material.

| Obfuscated | Human name | String key |
|---|---|---|
| `p` | `Material` | base class — map color, liquid/solid/replaceable/mobility |
| `sn` | `MaterialLiquid` | extends `p`; `a()` = true (isLiquid) |
| `mw` | `MaterialLogic`? | extends `p`; subclass with unknown overrides |
| `br` | Material subclass | extends `p`; unknown overrides |
| `bk` | Material subclass | extends `p`; unknown overrides |
| `tx` | Material subclass | extends `p`; unknown overrides |
| `wu` | `StepSound` | base class — sound name, volume, pitch |
| `bj` | `StepSoundGlass` | extends `wu`; `a()` = "random.glass" |
| `aeg` | `StepSoundSand` | extends `wu`; `a()` = "step.gravel" |
| `aav` | `MapColor` | 14 colour constants, RGB int per entry |

## World / Level Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `ry` | `World` | 2788 lines; implements `kq`; root game object — chunks, entities, TEs, tick schedule |
| `zx` | `Chunk` | 781 lines; 16×128×16 block/light/entity column; block index `x<<11|z<<7|y` |
| `gy` | `ChunkLoader` | file-based chunk persistence |
| `ia` | `Entity` | 1214 lines; abstract base for all in-world objects; fields s/t/u=pos, v/w/x=motion, K=isDead |
| `ih` | `EntityItem` | 147 lines; dropped-item entity; extends ia; fields a=ItemStack, b=age, c=pickupDelay, f=health(5); despawn at age 6000 |
| `dk` | `ItemStack` | 346 lines; final class; fields c=itemId, a=stackSize, e=itemDamage (non-obvious naming); NBT via ik |
| `cr` | `DataWatcher` | 167 lines; per-entity sync data; 7 types; wire header typeId<<5|entryId; 0x7F terminator |
| `afh` | `WatchableObject` | Inner container for DataWatcher entries; fields typeId/entryId/value/dirty |
| `k` | `WorldProvider` | 120 lines; abstract; dimension rules; f[16]=brightness table; e=isNether; subclasses ix/aau/ol |
| `ix` | `WorldProviderSurface` | Overworld (dim 0) |
| `aau` | `WorldProviderHell` | Nether (dim −1) |
| `ol` | `WorldProviderEnd` | End (dim 1) |
| `vi` | `EntityPlayer` | abstract; extends nq; eye height L=1.62F; AABB 0.6×1.8; maxHealth 20; inventory `by`=x |
| `x` | `InventoryPlayer` | implements de; a[36]=main, b[4]=armor, c=hotbarSlot; NBT armor at indices 100–103 |
| `de` | `IInventory` | interface; 10 methods: c/d(int)/a(int,int)/a(int,dk)/d()/e()/h()/b_(vi)/j()/k() |
| `uw` | `ItemBlock` | extends acy; field a=blockId; onItemUse places block with 5 validity guards |
| `sr` | `BiomeGenBase` | abstract; 16 biomes (IDs 0–15); temp/rain/height/surface block fields |
| `mk` | `BiomeSwampland` | extends sr; special grass/foliage color blend formula |
| `vh` | `WorldChunkManager` | computes per-block temperature/rainfall; `kq.a()` returns this |
| `ha` | `ColorizerGrass` | static; 65536-entry lookup from grasscolor.png; `a(temp,rain)` → packed RGB |
| `db` | `ColorizerFoliage` | static; 65536-entry lookup from foliagecolor.png; + 3 hardcoded oak/birch/spruce values |
| `xj` | `ChunkProviderGenerate` | Overworld procedural generator; "RandomLevelSource" |
| `jv` | `ChunkProviderHell` | Nether generator; "HellRandomLevelSource" |
| `er` | `ChunkProviderFlat` | Debug/flat world chunk loader |
| `ej` | `IChunkProvider` | interface for all chunk providers |
| `eb` | `NoiseGeneratorOctaves` | N-octave Perlin noise; extends cs |
| `agk` | `PerlinNoiseGenerator` | single-octave Perlin; 512-entry permutation table; extends cs |
| `cs` | `NoiseGeneratorBase` | abstract base for noise generators |
| `ql` | `BiomeDecorator` | per-biome tree/flower/ore decoration; called from `sr.B.a()` |
| `wx` | `BlockPressurePlate` | pressure plate; IDs 70 (stone) and 72 (wood) |
| `wj` | `BlockFire` | fire block (ID 51); extends Block; a[]=flammability; cb[]=burnability; 40-tick age/spread; netherrack permanent |
| `agw` | `BlockFluidBase` | abstract fluid base; extends Block; isOpaqueCube=false; renderAsNormal=false; tick delays water:5/lava:30 |
| `ahx` | `BlockFluid` | flowing fluid (IDs 8/10); spreading tick; flood-fill direction; infinite water; lava+water→solid |
| `add` | `BlockStationary` | still fluid (IDs 9/11); converts to flowing on neighbour change; still lava random tick → fire spread |
| `rf` | `MapGenRavine` | ravine carver; 2% per chunk; Y [20-68]; large radius; thicknessMult 3.0; per-Y d[] array; extends bz |
| `ig` | `WorldGenerator` | abstract base for all feature generators; boolean silent flag |
| `gq` | `WorldGenTrees` | standard oak tree; height [4,6]; oak log/leaves meta 0 |
| `yd` | `WorldGenBigTree` | fancy/branching oak; golden-ratio branch algorithm |
| `jp` | `WorldGenForestTree` | birch tree; like gq but height [5,7], log/leaves meta 2 |
| `qj` | `WorldGenSwamp` | swamp oak; wider canopy, vine draping, water-descent |
| `ty` | `WorldGenTaiga1` | thin spruce/pine; cone shape; log/leaves meta 1 |
| `us` | `WorldGenTaiga2` | wide pine/spruce; top-down radius growth; log/leaves meta 1 |
| `fc` | `WorldGenSandDisc` | disk patch generator; replaces grass/dirt in circle with given block |
| `adp` | `WorldGenClay` | clay disk generator; replaces grass/clay with clay |
| `fo` | `BiomeForest` | Forest biome; z=10 trees, 20% birch/72% oak/8% fancy-oak |
| `qk` | `BiomeTaiga` | Taiga biome; z=10 trees, 67% thin-spruce/33% wide-pine |
| `ym` | `BiomePlains` | Plains biome; z=-999 (no trees) |
| `ada` | `BiomeDesert` | Desert biome; z=-999, t/u=sand, C=2 dead bushes |
| `az` | `BiomeExtremeHills` | Extreme Hills biome; no tree override |
| `ce` | `BiomeIce` | Ice Plains / Ice Mountains biome |
| `aev` | `BiomeMushroomIsland` | Mushroom Island / Shore biome |
| `aeq` | `BiomeOcean` | Ocean / FrozenOcean biome |
| `lq` | `BiomeRiver` | River / FrozenRiver biome |
| `av` | `BiomeHell` | Hell (Nether) biome |
| `gu` | `BiomeSky` | Sky (End) biome |
| `ahv` | `BlockButton` | stone button; ID 77 |
| `xb` | `PressurePlateType` | enum/constants for pressure plate sensitivity (a=wood, b=stone) |

## WorldGen Decoration Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `ql` | `BiomeDecorator` | per-biome decoration; 15-step RNG-ordered sequence; ore helpers; called from `sr.B.a()` |
| `bu` | `WorldGenFlowers` | flower/dandelion/rose placer; 64 attempts, ±7/±3 spread, `canBlockStay` check, silent |
| `ahu` | `WorldGenTallGrass` | tall grass / dead bush placer; descend to surface first; 128 attempts; notify |
| `mb` | `WorldGenShrub` | dead shrub placer; descend to surface; 4 attempts; silent |
| `ib` | `WorldGenSpring` | water/lava spring placer; requires 3 stone + 1 air neighbours; triggers `onBlockAdded` |
| `tw` | `WorldGenReed` | sugar cane placer; 20 attempts; adjacent water at y-1; height [2,4] |
| `sz` | `WorldGenPumpkin` | pumpkin placer; 64 attempts; grass below; random facing meta (0-3) |
| `ade` | `WorldGenCactus` | cactus placer; 10 attempts; height [1,3]; block ID 81 (`yy.aV`) |
| `jj` | `WorldGenLilyPad` | lily pad placer; 10 attempts; water below required; block ID 111 (`yy.aW`) |
| `acp` | `WorldGenHugeMushroom` | huge brown/red mushroom; type 0=brown/1=red; height [4,6]+3 stem; cap face meta bits 1–10 |
| `we` | `SpawnerAnimals` | mob spawning; NOT a snow/ice generator (corrects prior ChunkProviderGenerate_Spec §8 label) |
| `acj` | `WorldGenDungeon` | underground dungeon room; extends `ig`; 1–5 door openings validation; cobblestone/mossy-cobble construction; 2 chest attempts + mob spawner |
| `ey` | `WorldGenNetherLavaPool` | Nether lava pool; 4 netherrack + 1 air neighbour check before placing stored block; triggers `onBlockAdded` |
| `pl` | `WorldGenNetherFire` | Nether fire placer; 64 attempts on netherrack floor |
| `pt` | `WorldGenGlowStone` | glowstone cluster type 1; 1500 downward attempts from netherrack ceiling; places at exactly-1-glowstone-neighbour cells |
| `aew` | `WorldGenGlowStone2` | glowstone cluster type 2; identical logic to `pt`; separate class for RNG-state parity |
| `cz` | `NetherMapGenCaves` | Nether cave carver; extends `bz` (MapGenBase); tunnel thickness = 0.5 (vs overworld 1.0) |
| `ed` | `NetherFortressGenerator` | NetherFortress structure generator; spawn list: Blaze(`qf`)/PigZombie(`jm`)/MagmaCube(`aea`) |

## Chunk Provider / Cache Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `ej` | `IChunkProvider` | Interface (10 methods): isLoaded, getChunk, populate, saveTick, tick, canSave, debug, getSpawns, findStructure |
| `jz` | `ChunkProviderServer` | Concrete `ej`; LongHashMap cache + disk loader + terrain generator; SP chunk manager |
| `wv` | `LongHashMap` | Custom hash map with `long` keys; load factor 0.75; initial capacity 16 |
| `acm` | `ChunkCoordIntPair` | (x,z) pair; static `a(x,z)` produces the long cache key: `x&0xFFFFFFFF | (z&0xFFFFFFFF)<<32` |
| `hn` | `EmptyChunk` | Extends `zx`; all-air (zero bytes); `r=true` = never save; returned for out-of-bounds |

## World Save / NBT Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `nh` | `ISaveHandler` | interface: level.dat read/write, session lock, IChunkLoader factory |
| `e` | `SaveHandler` | concrete `nh`; filesystem layout, session.lock, dimension routing |
| `bi` | `NullSaveHandler` | no-op `nh`; used for non-saving worlds |
| `d` | `IChunkLoader` | interface: load/save/flush per-chunk |
| `gy` | `ChunkLoader` | concrete `d`; per-chunk .dat files in base-36 two-level directory tree |
| `si` | `WorldInfo` | level.dat data container (seed/spawn/time/rain/player tag) |
| `vx` | `NbtIo` | static GZip ↔ NBT serializer |
| `um` | `NbtTag` | abstract NBT tag base (type ID, name, serialize/deserialize) |
| `ik` | `NbtCompound` | TAG_Compound (type 10); string→tag HashMap |
| `yi` | `NbtList` | TAG_List (type 9); element_type + List<NbtTag> |
| `hp` | `NbtEnd` | TAG_End (type 0); compound terminator |
| `xq` | `NbtByte` | TAG_Byte (type 1) |
| `cg` | `NbtShort` | TAG_Short (type 2) |
| `hx` | `NbtInt` | TAG_Int (type 3) |
| `vw` | `NbtLong` | TAG_Long (type 4) |
| `vd` | `NbtFloat` | TAG_Float (type 5) |
| `fg` | `NbtDouble` | TAG_Double (type 6) |
| `ca` | `NbtByteArray` | TAG_Byte_Array (type 7) |
| `yt` | `NbtString` | TAG_String (type 8); DataOutput.writeUTF framing |
| `ahn` | `ScheduledTick` | pending block tick; fields a/b/c=x/y/z, d=blockId, e=absoluteTick |

## Entity Classes (from EntityList)

| Obfuscated | Human name | Entity string ID | Integer ID |
|---|---|---|---|
| `ih` | `EntityItem` | `"Item"` | 1 |
| `fk` | `EntityXPOrb` | `"XPOrb"` | 2 |
| `tj` | `EntityPainting` | `"Painting"` | 9 |
| `ro` | `EntityArrow` | `"Arrow"` | 10 |
| `aah` | `EntitySnowball` | `"Snowball"` | 11 |
| `aad` | `EntityFireball` | `"Fireball"` | 12 |
| `yn` | `EntitySmallFireball` | `"SmallFireball"` | 13 |
| `tm` | `EntityEnderPearl` | `"ThrownEnderpearl"` | 14 |
| `bs` | `EntityEnderEye` | `"EyeOfEnderSignal"` | 15 |
| `dd` | `EntityTNTPrimed` | `"PrimedTnt"` | 20 |
| `uo` | `EntityFallingBlock` | `"FallingSand"` | 21 |
| `vm` | `EntityMinecart` | `"Minecart"` | 40 |
| `no` | `EntityBoat` | `"Boat"` | 41 |
| `nq` | `LivingEntity` | `"Mob"` | 48 (abstract) |
| `zo` | `EntityMob` | `"Monster"` | 49 (abstract) |
| `abh` | `EntityCreeper` | `"Creeper"` | 50 |
| `it` | `EntitySkeleton` | `"Skeleton"` | 51 |
| `vq` | `EntitySpider` | `"Spider"` | 52 |
| `abc` | `EntityGiant` | `"Giant"` | 53 |
| `gr` | `EntityZombie` | `"Zombie"` | 54 |
| `aed` | `EntitySlime` | `"Slime"` | 55 |
| `is` | `EntityGhast` | `"Ghast"` | 56 |
| `jm` | `EntityPigZombie` | `"PigZombie"` | 57 |
| `aii` | `EntityEnderman` | `"Enderman"` | 58 |
| `aco` | `EntityCaveSpider` | `"CaveSpider"` | 59 |
| `gl` | `EntitySilverfish` | `"Silverfish"` | 60 |
| `qf` | `EntityBlaze` | `"Blaze"` | 61 |
| `aea` | `EntityMagmaCube` | `"LavaSlime"` | 62 |
| `oo` | `EntityDragon` | `"EnderDragon"` | 63 |
| `fd` | `EntityPig` | `"Pig"` | 90 |
| `hm` | `EntitySheep` | `"Sheep"` | 91 |
| `adr` | `EntityCow` | `"Cow"` | 92 |
| `qh` | `EntityChicken` | `"Chicken"` | 93 |
| `yv` | `EntitySquid` | `"Squid"` | 94 |
| `aik` | `EntityWolf` | `"Wolf"` | 95 |
| `tb` | `EntityMooshroom` | `"MushroomCow"` | 96 |
| `ahd` | `EntitySnowman` | `"SnowMan"` | 97 |
| `ai` | `EntityVillager` | `"Villager"` | 120 |
| `sf` | `EntityEnderCrystal` | `"EnderCrystal"` | 200 |

## Geometry / Collision Block Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `xs` | `BlockSlab` | Single (ID 44, half-cube) and double (ID 43, full cube) via `cb` flag; 6 metadata variants; drops single slab item |
| `ahh` | `BlockStairs` | 2-AABB L-shape per meta 0-3; inherits texture/hardness/drops from parent block `a`; no top-half in 1.0 |
| `nz` | `BlockFence` | Height 1.5; dynamic AABB expands toward adjacent fence/gate/solid-non-glass; no metadata |
| `uc` | `BlockDoor` | Wood/iron door; 2-block tall; metadata encodes facing + open + hinge; thin AABB |
| `fp` | `BlockFenceGate` | Fence gate (ID 107); connected-to by fence (`yy.bv`); can be opened by player right-click |

## Plant / Agriculture Block Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `wg` | `BlockFlower` | Base plant block; material `p.j`; canBlockStay requires light≥8 or sky + valid soil; no collision (b(ry)=null) |
| `aha` | `BlockCrops` | Wheat crops (ID 59); extends `wg`; metadata 0-7 = growth stage; random-tick with growth factor; only survives on farmland |
| `ni` | `BlockFarmland` | Farmland (ID 60); metadata 0-7 = moisture; reverts to dirt dry+cropless; 0.9375 visual height, full collision; 25% trample chance |

## Mob / AI Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `ww` | `EntityAI` | Abstract; extends `nq`; pathfinder (`dw`), target (`h`), panic timer (`by`); base for all AI mobs |
| `zo` | `EntityMonster` | Abstract; extends `ww`; attackStrength `a`=2; aX=5 (XP); hostile; light-check spawn; target=nearest player 16 blocks |
| `fx` | `EntityAnimal` | Abstract; extends `ww`; breeding + age system; DataWatcher 12=age int; NBT: Age + InLove |
| `abh` | `EntityCreeper` | Extends `zo`; DW16=fuseCountdown, DW17=isPowered; NBT: "powered" boolean |
| `it` | `EntitySkeleton` | Extends `zo`; ranged bow attack; no extra NBT |
| `vq` | `EntitySpider` | Extends `zo`; DW16 bit0=isClimbing (not persisted); passive in daylight; Poison immune |
| `gr` | `EntityZombie` | Extends `zo`; attackStrength=4; burns in direct sunlight; no extra NBT |
| `fd` | `EntityPig` | Extends `fx`; DW16 bit0=hasSaddle; NBT: "Saddle" + Age + InLove |
| `hm` | `EntitySheep` | Extends `fx`; DW16 bits0-3=colour bits4=sheared; NBT: "Sheared" + "Color" + Age + InLove |
| `adr` | `EntityCow` | Extends `fx`; milk bucket interaction; no extra NBT beyond Age + InLove |
| `qh` | `EntityChicken` | Extends `fx`; egg timer `g` (transient); no extra NBT beyond Age + InLove |
| `pm` | `DamageSource` | Damage type container: type string, isUnblockable, isFireDamage, exhaustion; factory `a(nq)`, `a(vi)`, etc. |
| `fq` | `EntityDamageSource` | Extends `pm`; stores attacker entity; `a()` returns attacker |
| `qq` | `EntityDamageSourceIndirect` | Extends `fq`; stores owner entity; `a()` returns owner (not projectile) |
| `agu` | `ItemFood` | Extends `acy`; healAmount `b`, satMod `bR`, wolfFood `bS`; eat animation 32 ticks; on-eat potion support |
| `sv` | `EnumArt` | **NOT ItemFood** — painting variant enum; 25 entries (Kebab…DonkeyKong) with width/height/atlas coords |
| `ps` | `EnumAction` | Item use action enum; 5 values (a–e); value `b` = eat animation |

## Spawning / Creature Type Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `jf` | `EnumCreatureType` | 3-value enum: `a`=hostile (base `aey`, cap 70, material solid), `b`=passive (base `fx`, cap 15, material solid), `c`=water (base `dn`, cap 5, material water); `d()`=isPassive, `b()`=cap, `c()`=material, `a()`=baseClass |
| `aey` | `EntityMob` | Abstract hostile mob base class; used by `jf.a` for entity population counting |
| `dn` | `EntityWaterCreature` | Abstract water creature base class (likely Squid = `yv`); used by `jf.c` for water mob cap |

## TileEntity Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `bq` | `TileEntity` | Abstract TE base; fields world/x/y/z; `a(ik)` write, `b(ik)` read, `e()` tick; 11 subclasses registered |
| `ba` | `TileEntityRegistry` | Static factory; `c(ik)` reads "id" string, instantiates subclass, calls `b(tag)`, returns TE |
| `tu` | `TileEntityChest` | `"Chest"`; 36-slot internal (c()=27 visible); sparse "Items" TAG_List; no CustomName in 1.0 |
| `oe` | `TileEntityFurnace` | `"Furnace"`; 3 slots (input 0/fuel 1/output 2); "BurnTime" short + "CookTime" short; full 200-tick smelt; 15 hardcoded recipes via `mt` |
| `bp` | `TileEntityDispenser` | `"Trap"`; 9 slots; same slot format as chest; dispense-on-redstone logic |
| `u` | `TileEntitySign` | `"Sign"`; Text1–Text4 TAG_String; 15-char truncation on read; `j=false` (needsUpdate) set before super.b() |
| `ze` | `TileEntityMobSpawner` | `"MobSpawner"`; "EntityId" TAG_String + "Delay" TAG_Short; 4-mob spawn; 200–799 tick delay reset; player-proximity 16-block check |
| `nj` | `TileEntityNote` | `"Music"`; "note" TAG_Byte 0–24; instrument from block material below (harp/baseDrum/snare/hat/bass) |
| `mt` | `FurnaceRecipes` | Static smelting table; 15 hardcoded input→output pairs; accessed via `mt.a()` singleton |
| `eq` | `FoodStats` | Player food state; "foodLevel" int + "foodTickTimer" int + "foodSaturationLevel" float + "foodExhaustionLevel" float; only loaded if "foodLevel" key present |
| `wq` | `PlayerAbilities` | Player ability flags: a=invulnerable, b=flying, c=allowFlying, d=instantBuild; **BUG**: `a(ik)` writes `this.a` (invulnerable) to "flying" key; `b(ik)` reads correctly |

---

*Add new mappings as classes are analyzed. Keep alphabetical by obfuscated name within each section.*
