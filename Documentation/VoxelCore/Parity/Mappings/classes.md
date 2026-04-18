# Class Name Mappings — Minecraft 1.0

Obfuscated name (as found in `temp/decompiled/`) → MCP/human-readable name.

> Source: inferred from class structure analysis. Not derived from any MCP release.

## Utility Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `c` | `AxisAlignedBB` | Axis-aligned bounding box; 6 double fields; static object pool; sweep collision; ray trace |
| `am` | `BlockPos` | Simple int triple (a=x, b=y, c=z); hashCode = `a*8976890 + b*981131 + c`; used as explosion block-set key |
| `dh` | `BlockTriple` | Simple int triple (a=x, b=y, c=z); returned by `aab.f()` (findWakeupPosition) to report the safe spawn coord; distinct from `am` |
| `bo` | `EnumMovingObjectType` | Java enum; two constants: `a`=TILE (0), `b`=ENTITY (1) |
| `fb` | `Vec3` | 3D double vector; static object pool; geometric ops; segment-plane intersection |
| `gv` | `MovingObjectPosition` | Ray-cast result; block-hit or entity-hit constructor; face ID 0–5 |
| `yy` | `Block` | Block base class; static registry k[256]; 8 parallel arrays; builder pattern; virtual behaviour |
| `me` | `MathHelper` | Static trig/numeric utilities; sine table (65536 entries); floor, sqrt, clamp, abs, floor-div, RNG range |
| `lf` | `TorchHistory` | Record of one torch-flip event; fields: x, y, z (int), worldTime (long); stored in `ku.cb` burnout list |
| `lz` | `Direction` (redstone helper) | Static arrays for redstone wire: `e[]={2,3,0,1}` opposite face mapping; `a[]`/`b[]` Z/X deltas for wire connectivity neighbours |
| `xb` | `EnumPressurePlateType` | 3-value Java enum: `a`=ALL_ENTITIES (wood plate ID 72), `b`=MOBS (stone plate ID 70), `c`=PLAYERS_ONLY (unused in 1.0) |

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
| `bg` | `BlockTorch` | `"torch"` (base class for torches); placement sets meta 1-5 from supported face (1=west wall, 2=east wall, 3=north wall, 4=south wall, 5=floor); canBlockStay checks 4 wall solids + solid below for floor; AABB: wall metas w=0.15F, floor meta 0.1F |
| `wj` | `BlockFire` | `"fire"` |
| `kk` | `BlockMobSpawner` | `"mobSpawner"` |
| `au` | `BlockChest` | `"chest"` |
| `kw` | `BlockRedstoneWire` | `"redstoneDust"` (ID 55); DFS propagation with anti-reentrance flag `a`; dirty-block HashSet `cb`; attenuation -1/block from 15; `f()` gets max wire-neighbor power; `h()` propagates to adjacent wire clusters; 0-crossing notifies neighbours; canBlockStay=solid below; canProvidePower=`this.a`; drops `acy.aB.bM` (redstone dust item) |
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
| `mz` | `BlockRedstoneDiode` | `"diode"` (IDs 93=off, 94=on); meta bits 0-1=facing (output direction; 0=south,1=west,2=north,3=east), bits 2-3=delay index; static delay array cb={1,2,3,4} → {2,4,6,8} ticks; input check `f()` reads world.l() + wire; right-click cycles delay; isProvidingWeakPower/StrongPower on output face only; drops `acy.ba.bM` (off-repeater item) |
| `aif` | `BlockSnow` | Snow layer (ID 78); `p.u` material; AABB height=(2*(1+layers))/16; collision only at layers≥3 (up to 0.5F); canBlockStay=solid renderNormal below; harvest drops 1 snowball; melt at blockLight>11 |
| `jk` | `BlockSnowBlock` | `"snow"` (block) |
| `ahq` | `BlockIce` | Ice (ID 79); `p.t` material; ca=0.98F slipperiness; opacity=1; drops nothing; melt at blockLight>10 → still water; mined over air/liquid → flowing water |
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
| `aaa` | `BlockLever` | `"lever"` (ID 69); meta bits 0-2=facing (1=east wall, 2=west wall, 3=south wall, 4=north wall, 5=floor-S, 6=floor-E, 7=ceiling); bit 3=isOn; floor metas 5/6 chosen at random on placement; `a()` toggles bit 3; `b()` weak power on all faces when on; `c()` strong power only on attached face when on; drops lever item |
| `oc` | `BlockOreRedstone` | `"oreRedstone"` (ID 73=normal, 74=glowing); touch/walk/interact → switch to ID 74; randomTick → revert to ID 73; drops 4-6 redstone dust; `c_()` returns normal ore |
| `ku` | `BlockRedstoneTorch` | `"notGate"` (extends `bg`; IDs 75=off, 76=on); isOn field `a`; STATIC burnout list `cb` shared across ALL torch instances (vanilla bug — can cross-contaminate other torches); tick delay 2; `g()` isPowered checks attached block; burnout: ≥8 entries in cb within 100-tick window → stays off; randomTick switches on↔off; drops always ID 76 (on-torch item) |
| `ahv` | `BlockButton` | `"button"` (ID 77=stone only in 1.0; ID 143=wood button ABSENT in 1.0, added Beta 1.7+); wall-only, no floor placement; meta bits 0-2=facing (1=east,2=west,3=south,4=north), bit 3=isPressed; tick rate 20 (auto-release); right-click presses; randomTick releases; isProvidingStrongPower on attached face+front when pressed; dead meta 5 in `c()` is unreachable code |
| `wx` | `BlockPressurePlate` | `"pressurePlate"` (ID 70=stone, ID 72=wood); field `a`=xb enum type; tick rate 20; sensor scan per type: a=all entities, b=living mobs only, c=players only; pressed=meta 1, unpressed=meta 0; canBlockStay also accepts wire (`yy.aZ`); strong power upward only (face 1) |
| `cu` | `BlockDispenser` | `"dispenser"` |
| `aat` | `BlockSandStone` | `"sandStone"` |
| `yq` | `BlockNote` | `"musicBlock"` |
| `aab` | `BlockBed` | `"bed"` (ID 26); metadata bits 0-1=facing, 2=occupied, 3=isHead; static dir array `a={{0,1},{-1,0},{0,-1},{1,0}}`; AABB 9/16 height; onBlockActivated redirects foot→head, Nether explosion power 5 incendiary, calls `vi.d()` (trySleep); drops `kn`(99) from foot half only; orphan half removed on neighbor change |
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
| `xp` | `Explosion` | Sphere ray-cast destructor; fields: `f`=power, `a`=isIncendiary, `b`=exploderEntity, `c/d/e`=XYZ, `g`=am HashSet of destroyed blocks; 1352 rays (16³ surface-only); local `Random h` for incendiary fire; called via `ry.world.a(entity, x, y, z, power[, isIncendiary])` |
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
| `ed` | `MapGenNetherBridge` | NetherFortress structure generator; extends `hl`; 1/3 placement chance per chunk; seed=(cX^(cZ<<4))^worldSeed; spawn list: Blaze(`qf`)/PigZombie(`jm`)/MagmaCube(`aea`) |
| `tg` | `NetherFortressStart` | StructureStart subclass; creates `gc` piece at (cX*16+2, cZ*16+2); generates within Y=[48,70], radius≤112 |
| `rp` | `NetherFortressPieceRegistry` | Static piece lists; corridor list: ac/bw/ui/bl/kf/xr; room list: hg/yj/lu/ahw/tr/acs/io |
| `gc` | `NF_EntryCollider` | Starting piece; extends `bw` (CorridorA); beginning of fortress generation tree |
| `ac` | `NF_CorridorA` | 5×5×7 straight corridor |
| `bw` | `NF_CorridorB` | 5×5×7 corridor with support beams |
| `ui` | `NF_CorridorC` | 5×5×7 corridor staircase |
| `bl` | `NF_CorridorD` | 5×5×7 corridor crossing |
| `kf` | `NF_BlazeSpawnerCorridor` | 5×5×11; places MobSpawner "Blaze" at local (5,6,3); boolean `a` prevents re-placement |
| `xr` | `NF_LavaFortressRoom` | 13×4×13 room with central lava pool; `world.f=true` flag wrap |
| `hg` | `NF_RoomA` | 7×9×7 room |
| `yj` | `NF_RoomB` | 9×7×9 room |
| `lu` | `NF_RoomC` | 9×7×9 room staircase |
| `ahw` | `NF_RoomD` | 5×5×7 room variant |
| `tr` | `NF_Crossing5` | 5-way crossing room |
| `acs` | `NF_Crossing5B` | 5-way crossing with central pillar |
| `io` | `NF_NetherWartRoom` | 13×14×13; soul sand (ID 88) + nether wart (ID 115) farm |
| `ld` | `NF_DeadEndTunnel` | Dead-end terminator piece; fallback when no piece fits |

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

## Pathfinding Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `rw` | `PathFinder` | A* pathfinder; open set = `zs`; node cache = `ob`; world view = `xk`; 4-directional only; step-up/down; entity-bbox collision; partial-path fallback |
| `mo` | `PathPoint` | Path graph node; fields a=x, b=y, c=z, j=hash, d=heapIndex(-1=not in heap), e=g_cost, f=h_cost, g=f_cost, h=parent, i=closed |
| `dw` | `PathEntity` | Completed path container; b=mo[] ordered array, a=length, c=currentIndex; a(entity)=waypoint Vec3, a()=advance, b()=exhausted |
| `zs` | `PathHeap` | Binary min-heap open set; sorted by mo.g; initial capacity 1024 (doubles on overflow); add/poll/update/isEmpty/clear |
| `ob` | `PathNodeCache` | int-keyed hash map for node deduplication; load factor 0.75; initial capacity 16 |
| `xk` | `ChunkCache` | IBlockAccess snapshot; pre-fetches chunks in bbox; used as world view for `rw`; created by `world.a(entity,target,range)` |

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
| `ads` | `ItemTool` | Extends `acy`; bR=effective blocks array; a=efficiency (from material); bS=weaponDamage=baseDmg+materialBonus; b=nu material; hitEntity costs 2 durability, onBlockDestroyed costs 1 |
| `nu` | `EnumToolMaterial` | 5 constants: a=WOOD(0,59,2F,0,15), b=STONE(1,131,4F,1,5), c=IRON(2,250,6F,2,14), d=DIAMOND(3,1561,8F,3,10), e=GOLD(0,32,12F,0,22); fields f=harvestLevel, g=maxUses, h=efficiency, i=damageBonus, j=enchantability |
| `zp` | `ItemSword` | Extends `acy` (NOT ads); a=4+material.damageBonus; getStrVsBlock=15F for cobweb/1.5F else; blocking action `ps.d`; hitEntity costs 1 durability |
| `adb` | `ItemSpade` | Extends `ads`; baseDamage=1; 10 effective blocks (grass/dirt/sand/gravel/snow/clay/farmland/soulsand/mycelium); canHarvestBlock=snow_layer+snow_block only |
| `zu` | `ItemPickaxe` | Extends `ads`; baseDamage=2; 22 effective blocks (all stone/ore/metal/rail); canHarvestBlock with harvest-level tier gates (obsidian=diamond, oreDiamond/oreGold=iron+, oreIron/oreLapis=stone+, oreRedstone=iron+) |
| `ago` | `ItemAxe` | Extends `ads`; baseDamage=3; 8 effective blocks; efficiency for ANY wood-material block (overrides bR check) |
| `wr` | `ItemHoe` | Extends `acy` (NOT ads); no weapon damage; tills grass (top face, air above) or dirt → farmland; costs 1 durability per till |
| `agi` | `ItemArmor` | Extends `acy`; a=armorType(0-3), b=protection, bR=armorSlot, bT=dj material; maxDurability=bS[slot]*material.f; bS={11,16,15,13} |
| `dj` | `EnumArmorMaterial` | 5 constants: a=LEATHER(5,[1,3,2,1],15), b=CHAIN(15,[2,5,4,1],12), c=IRON(15,[2,6,5,2],9), d=GOLD(7,[2,5,3,1],25), e=DIAMOND(33,[3,8,6,3],10) |
| `sv` | `EnumArt` | **NOT ItemFood** — painting variant enum; 25 entries (Kebab…DonkeyKong) with width/height/atlas coords |
| `ps` | `EnumAction` | Item use action enum; 5 values (a–e); value `b` = eat animation |
| `qy` | `EnumSleepStatus` | 6-value enum returned by `vi.d()` (trySleep): `a`=OK, `b`=NOT_POSSIBLE_HERE (dimension), `c`=NOT_POSSIBLE_NOW (daytime), `d`=TOO_FAR_AWAY (>3 XZ / >2 Y), `e`=OTHER_PROBLEM (dead/already sleeping), `f`=NOT_SAFE (monster within 8×5×8) |
| `kn` | `ItemBed` | Extends `acy`; bM=355; itemId=99 (=acy.aZ); `a(dk,ry,vi,int,int,int,int,float)` = `onItemUse`; places foot then head block; no special fields |

## Piston Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `abr` | `BlockPiston` | IDs 29 (sticky) / 33 (normal); field `a`=isSticky; static `cb`=anti-reentrance guard; meta bits 0-2=facing, bit3=isExtended; isPowered: 12-position check; push limit=13; canPush walkforward loop; doExtend: backward-pass block shifting via qz proxy |
| `acu` | `BlockPistonExtension` | ID 34; field `a`=textureOverride (-1=default); two-part AABB (face plate + shaft) per facing; defers neighbor events to base piston |
| `qz` | `BlockMovingPiston` | ID 36; extends `ba` (BlockContainer); hardness=-1 (indestructible while moving); dropBlockAsItem uses stored block; AABB animated from agb progress |
| `agb` | `TileEntityPiston` | fields: a=storedBlockId, b=storedBlockMeta, j=facing, k=isExtending, l=isSource, m=currentProgress, n=prevProgress; tick advances m by 0.5F per tick; finalizes at 1.0F; NBT saves n (not m) — quirk; entity push uses static shared list `o` |
| `ot` | `DirectionArrays` | Static facing utility; b[]={0,0,0,0,-1,1} (Y offsets), c[]={-1,1,0,0,0,0} (X offsets), d[]={0,0,-1,1,0,0} (Z offsets); face 0=down, 1=up, 2=north(-Z), 3=south(+Z), 4=west(-X), 5=east(+X) |

## Overworld Structure Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `hl` | `MapGenStructureBase` | Abstract base for structure generators; extends `bz`; handles piece registration/query |
| `kd` | `MapGenMineshaft` | Mineshaft generator; extends `hl`; nextInt(100)==0 && nextInt(80)<max(|cX|,|cZ|); start=`ns` |
| `ns` | `MineshaftStart` | StructureStart for mineshafts; starting piece=`uk` |
| `aez` | `MineshaftPieceFactory` | Piece factory; aba=70%, ra=10%, id=20%; max depth=8, radius≤80 |
| `uk` | `MineshaftStartPiece` | Initial mineshaft corridor piece |
| `aba` | `MineshaftCorridor` | 70% of pieces; wooden support every 5 blocks (planks ID 5); fence posts ID 85; rails ID 66; cobweb ID 30; cave-spider spawner ~4.3% (when not isMain + 1/23 chance); chest wagon loot 1%/support |
| `ra` | `MineshaftCrossing` | 10% of pieces; 4-way junction |
| `id` | `MineshaftStaircase` | 20% of pieces; descending staircase segment |
| `dc` | `MapGenStronghold` | Stronghold generator; extends `hl`; 3 per world; initial angle=nextDouble()×π×2; spacing 2π/3; distance=(1.25+nextDouble())×32 chunks; 7 valid biomes; search radius=112 blocks |
| `kg` | `StrongholdStart` | StructureStart for strongholds; opening piece=`aeh` |
| `aeh` | `StrongholdStaircase` | Stronghold opening staircase piece; stone brick (ID 98) |
| `vn` | `StrongholdCorridor` | 5×5×N straight corridor piece in stronghold; **NOT** the generator (corrects Coder guess) |
| `xn` | `MapGenVillage` | Village generator; extends `hl`; 32-chunk grid; offset nextInt(24) X and Z; valid biomes sr.c+sr.d (plains+desert); cell RNG=world.x(gX,gZ,10387312); returns boolean suppressing dungeon |
| `yo` | `VillageStart` | StructureStart for villages; starting piece=`yp` |
| `yp` | `VillageStartPiece` | Village initial road/well piece |

## Item Classes — Records / Jukebox

| Obfuscated | Human name | Notes |
|---|---|---|
| `pe` | `ItemRecord` | Extends `acy`; field `a`=String discName; bN=1 (no stacking); 11 discs acy.bB–bL (IDs 2256–2266); onItemUse inserts into jukebox (meta=0 check); fires event 1005 with bM; tooltip "C418 - <name>"; rarity RARE (aqua colour) |
| `agc` | `TileEntityJukebox` | Extends `bq`; field `a`=int recordItemId (0=empty); NBT key "Record" as int (not written when 0) |

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

## End Dimension Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `a` | `ChunkProviderEnd` | End chunk generator; implements `ej`; 5 noise generators (eb octaves); 3×33×3 density grid; island shaping `100-8*sqrt(x²+z²)`; `var18=0` dead code (noise `b` unused); pure end stone fill; surface pass is no-op; delegates populate to `uu` (BiomeSky) |
| `ol` | `WorldProviderEnd` | End dimension provider; dim=1; `g()=new dh(100,50,0)`; fog=0x808080×0.15F; `e=true` (no sky), `c=true` (sleeping disabled), `g=1`; `c()=new a(world,seed)` |
| `uu` | `BiomeSky` | End biome decorator; extends `ql`; field `L=new oh(yy.bJ.bM)` (spike generator); 1/5 chance spike per populate; spawns Ender Dragon `oo` at (0.0,128.0,0.0) only for chunk (0,0) |
| `oh` | `WorldGenEndSpike` | End obsidian pillar generator; validates end stone floor; random height nextInt(32)+6=[6,37], radius nextInt(4)+1=[1,4]; obsidian cylinder (yy.ap.bM=49); EntityEnderCrystal `sf` on top; bedrock cap (yy.z.bM=7) |
| `oo` | `EntityDragon` | Ender Dragon boss entity (EntityList "EnderDragon", ID 63); extends `adh` (EntityBoss); max health 200; 7-part body (vc); flying AI with 64-entry position ring buffer; wing knockback + head melee 10 dmg; crystal healing 1HP/10t; 200-tick death sequence; generates exit portal at Y=64 on death |
| `adh` | `EntityBoss` | Abstract boss base; extends `nq`; immune to all damage via `a(pm,int)=false`; `e(pm,int)=super.a()` bypass; `f_()=bI` maxHealth |
| `vc` | `EntityBodyPart` | Multi-part body segment; extends `ia`; holds parent reference `a` and name `b`; delegates all damage to `parent.a(this,pm,int)`; no NBT; `h(ia)=true` for self or parent |
| `sf` | `EntityEnderCrystal` | Ender Crystal entity; extends `ia`; hitbox 2.0×2.0; noClip; places fire block each tick at own position; 1-shot kill → explosion power 6.0; no NBT persistence; DataWatcher slot 8 = health/state |
| `sf` | `EntityEnderCrystal` | End crystal entity; placed on top of obsidian pillars by `oh` |
| `rl` | `BlockEndPortalFrame` | BlockEndPortalFrame (ID 120); texture 159; meta bits 0-1=facing, bit 2=hasEye; `e(meta)=(meta&4)!=0`; AABB 0–0.8125; hardness=-1 (unbreakable); light 0.125F; drops nothing; facing set from player yaw on placement |
| `aid` | `BlockEndPortal` | BlockEndPortal (ID 119); extends `ba` (TileEntityRegistry); TileEntity `yg`; AABB 1/16 thick; no collision; `onEntityCollided→player.c(1)` teleport; self-destructs in non-overworld on `onBlockAdded`; static `a` guard |
| `yg` | `TileEntityEndPortal` | TileEntity for BlockEndPortal (ID 119); registered as "Airportal"; minimal implementation |
| `aag` | `ItemEnderEye` | Eye of Ender item; `onItemUse`: validates frame+empty, sets meta\|4 (hasEye), checks 12-frame ring via `lz` arrays (3+3+3+3), fills 3×3 interior with yy.bH.bM; `onItemRightClick`: throws `bs` (EntityEnderEye) toward stronghold via `world.b("Stronghold",…)` |
| `bs` | `EntityEnderEye` | Thrown Eye of Ender entity; flies toward nearest stronghold |

## Portal / Travel Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `sc` | `BlockPortal` | Nether portal block (ID 90); extends `aaf`; no collision `b()=null`; visual AABB 0.25 thick; `g()` tryToCreatePortal: 10 obsidian minimum (corners optional), 4×5 scan (var7=-1..2, var8=-1..3), places 2×3 interior; onNeighborChange destroys invalid columns; entity contact calls `entity.S()`; 1% sound + 4 portal particles per tick |
| `aaf` | `BlockPortalBase` | Abstract base class for `sc` (BlockPortal); exact function unknown |
| `aim` | `PortalTravelAgent` | Portal link manager; `a(world,entity)`: for dim==1 places 5×5 obsidian floor+3-high air; for Nether calls `b()` find then `c()` create; `b()` radius=128; `c()` radius=16, 2-phase + emergency at Y=70, builds 4×5 obsidian frame with 2×3 portal interior |
| `ou` | `ItemFlintAndSteel` | Flint and Steel item (ID 259); bN=1; durability=64; `onItemUse` places fire on adjacent air; always damages 1 durability; portal ignition indirect via BlockFire→sc.g() |

## Ranged Combat / Fishing Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `il` | `ItemBow` | Bow item (ID 261 = 256+5); durability 384; charge formula `power=(f²+2f)/3` where f=ticksCharged/20; arrow fired in `onPlayerStoppedUsing`; requires arrows in inventory (acy.k) or Creative; critical at power==1.0; use action ps.e; texture col5 row1 |
| `ro` | `EntityArrow` | Arrow entity (EntityList "Arrow", ID 10); hitbox 0.5×0.5; gravity 0.05/tick, air drag 0.99, water drag 0.8; fields: e/f/g=xTile/yTile/zTile, aq=inGround, a=isPlayerArrow, b=shake, c=shooter, ar=ticksInGround, d=isCritical; despawn ar==1200; pickup when aq+isPlayer+shake==0; shooter NOT saved to NBT |
| `hd` | `ItemFishingRod` | Fishing rod item (ID 346 = 256+90); durability 64; cast spawns ael; reel-in calls ael.g() → returns durability cost 0/1/2/3 |
| `ael` | `EntityFishHook` | Fish hook entity (NOT in EntityList, no NBT persistence); hitbox 0.25×0.25; cast velocity ≈0.6 b/t; fish bite: 1/500 roll (1/300 with sky), ar=nextInt(30)+10 countdown; reel-in: entity pull (cost 3) / raw fish (cost 1) / ground (cost 2) / empty (cost 0); player.ci = this |
| `it` | `EntitySkeleton` | Skeleton mob (EntityList "Skeleton", ID 51); fires ro at targets <10 blocks; arrow speed 1.6 spread 12; cooldown aT=60; holds acy.j (bow); drops 0–2 arrows + 0–2 bones; sunlight per-tick burn check; Sniper achievement at ≥50 block kill |

---

## Enchanting / XP Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `fk` | `EntityXPOrb` | XP orb entity (EntityList "XPOrb", ID 2); hitbox 0.5×0.5; gravity 0.03/tick; attracted to nearest player ≤8 blocks; despawn at age2≥6000 ticks; 10 value tiers via `h()` threshold array; xpValue=field `e`; NBT: Health/Age/Value |
| `sy` | `BlockEnchantmentTable` | Enchantment table block (ID 116); AABB 0–0.75 height; hardness 5.0 resistance 2000; TileEntity=`rq`; random-tick spawns "enchantmenttable" particles toward bookshelves with LOS check; onActivate opens `ahk` container |
| `rq` | `TileEntityEnchantmentTable` | Enchantment table TE — animated floating book; tracks nearest player within 3 blocks; fields: a=tickCounter, b/j=openAmount curr/prev, m/n=bookLift curr/prev, o/p=bookRotation curr/prev, q=targetAngle; renderer: `wn` |
| `wn` | `TileEntityEnchantmentTableRenderer` | Client-side book animation renderer; extends `du` (TileEntityRenderer); draws "/item/book.png" above the table |
| `ahk` | `ContainerEnchantment` | Enchantment container; 1 input slot + player inventory (36); `c[3]`=slot levels synced to clients; `b`=long seed (nextLong on item placement); bookshelf scan: 8-adjacent gap check then ±2 bookshelf positions; slot levels via `ml.a()`; enchanting deducts levels via `player.l()` |
| `ml` | `EnchantmentHelper` | Static enchantment utilities; `a(Random,slot,bookshelfBonus,item)` = slot level formula; `a(Random,item,power)` = weighted enchantment selection; various enchantment-level lookup helpers (getLevel, damage reduction, etc.) |
| `aef` | `Enchantment` | Abstract enchantment base; `b[]`=static registry[256]; fields: t=ID, a=weight, u=target, v=name; 19 registered instances (c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s); default min=1+L×10, max=min+5, maxLevel=1 |
| `ii` | `EnchantmentProtection` | Protection group (subtypes 0-4: Protection/FireProtection/FeatherFalling/BlastProtection/ProjectileProtection); maxLevel=4; FeatherFalling target overridden to boots only; group mutually exclusive except FeatherFalling can coexist |
| `vu` | `EnchantmentRespiration` | Respiration (ID 5, max 3); min=10×L, max=10×L+30; target: helmet |
| `adz` | `EnchantmentAquaAffinity` | Aqua Affinity (ID 6, max 1); min=1, max=41; target: helmet |
| `ap` | `EnchantmentDamage` | Sword damage group (subtypes 0/1/2: Sharpness/Smite/BaneOfArthropods); maxLevel=5; all three mutually exclusive; damage bonus applied in LivingEntity damage pipeline |
| `dz` | `EnchantmentKnockback` | Knockback (ID 19, max 2); min=5+20×(L-1), max=1+L×10+50 (uses super.a) |
| `aie` | `EnchantmentFireAspect` | Fire Aspect (ID 20, max 2); min=10+20×(L-1), max=1+L×10+50 (uses super.a) |
| `qn` | `EnchantmentLootBonus` | Looting (ID 21, sword, max 3) and Fortune (ID 35, tool, max 3); min=20+(L-1)×12, max=1+L×10+50; Looting and Fortune are each mutually exclusive with SilkTouch |
| `kr` | `EnchantmentDigging` | Efficiency (ID 32, max 5); min=1+15×(L-1), max=1+L×10+50 |
| `gi` | `EnchantmentUntouching` | Silk Touch (ID 33, max 1); min=25, max=61; mutually exclusive with Fortune |
| `dq` | `EnchantmentDurability` | Unbreaking (ID 34, max 3); min=5+(L-1)×10, max=1+L×10+50 |
| `vs` | `EnchantmentData` | Enchantment+level pair; extends `abt` (weighted item) with weight=enchantment.weight; used by weighted-random selection in `ml` |
| `q` | `EnumEnchantmentType` | Enum of enchantment targets; `b`=armor, `c`=boots, `d`=leggings, `e`=chestplate, `f`=helmet, `g`=sword, `h`=tool; `a(item)` dispatches by item type |

## Container / Inventory Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `pj` | `Container` | Abstract container base; `e`=List<Slot>, `d`=snapshots, `g`=ICrafting listeners; click handler (left/right/shift/slotId=-999 drop); merge helper |
| `vv` | `Slot` | One inventory cell in a container; fields `c`=IInventory, `a`=slotIndex, `e`=screenX, `f`=screenY; `b()`=getItem, `a(dk)`=canPutInSlot |
| `ace` | `ContainerWorkbench` | 3×3 crafting grid container (slot 0=SlotCrafting, 1–9=grid, 10–45=player); opened by BlockWorkbench |
| `gd` | `ContainerPlayer` | 2×2 crafting + 4 armor slots container; opened by E key; validity always true |
| `eg` | `ContainerFurnace` | Furnace container; slots 0=input(56,17), 1=fuel(56,53), 2=output(116,35); syncs cookTime/burnTime to clients via data IDs 0/1/2 |
| `ak` | `ContainerChest` | Chest container; variable rows `b=inventory.c()/9`; calls inventory.j()/k() on open/close |
| `afe` | `SlotCrafting` | Output slot for crafting grids; reads recipe result from `sl` on any change |
| `ie` | `SlotFurnaceOutput` | Output slot for furnace; only allows taking, not placing |
| `pi` | `SlotArmor` | Armor slot; accepts only matching armor type |
| `lm` | `CraftingInventory` | NxM grid IInventory for crafting grids; constructed as `lm(pj container, int width, int height)` |
| `iy` | `OutputInventory` | Single-slot output buffer for crafting result |
| `adv` | `InventoryLargeChest` | Combines two IInventory into 54-slot view; slots 0–26=left chest, 27–53=right |
| `sl` | `CraftingManager` | Shaped/shapeless recipe registry singleton; `a()` adds shaped recipe, `b()` adds shapeless; tool repair special-case; loads 7 helper classes |
| `mt` | `FurnaceRecipes` | Smelting recipe singleton; 15 input→output pairs |
| `abd` | `ICrafting` | Listener interface; notified on slot change via `a(pj, slotIndex, ItemStack)` |

## Potion / Item Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `abg` | `Potion` | Potion effect type registry; 19 effects in 1.0 (IDs 1–19), 20–31 null; fields H=ID, I=name, J=icon, K=isBad, L=factor, M=ambient, N=color; performEffect per-tick; shouldTrigger interval formula |
| `s` | `PotionEffect` | Active effect instance; fields a=effectId, b=durationTicks, c=amplifier; `a(nq)` tick method; `a(s)` combine (higher amp or longer duration) |
| `abk` | `ItemPotion` | Potion item (ID 373); stack 1; splash = `(meta & 16384) != 0`; drink applies `pk.b(meta,false)` effects; returns glass bottle `acy.bs` |
| `pk` | `PotionHelper` | Metadata decoder via formula strings; 32 prefix name strings for mundane potions |
| `py` | `InstantPotion` | Extends `abg`; overrides `a()`=true; used for Instant Health/Harm (IDs 6/7) |
| `en` | `ItemBucket` | Bucket item (ID 325=empty, 326=water, 327=lava); field `a`=fluid block ID; picks up still water/lava; places on right-click |
| `om` | `ItemMilkBucket` | Milk bucket item (ID 335); separate class from `en`; obtained from cow (`adr`); clears all potion effects on drink |
| `abo` | `ItemShears` | Shears item (ID 359); durability 238; speed 15F for leaves/tallgrass, 5F for wool; blocks `yy.K/W/X/bu` cost 1 durability |
| `my` | `ItemSign` | Sign item (ID 323); stack 1; face==1→floor sign `yy.aD` meta=`(yaw+180)*16/360` &15; face 2-5→wall sign `yy.aI` meta=face; opens sign GUI |
| `afk` | `ItemGoldenApple` | Golden apple item (ID 322); extends `agu` (ItemFood); `g(dk)=true` (always edible); `d(dk)=ja.d` (epic rarity) |
| `xv` | `ItemDye` | Dye item (ID 351); 16 metadata colors; meta 15=bonemeal (grows plants); other metas dye wool/sheep; texture = `bO + meta%8*16 + meta/8` |

## Village Structure Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `xf` | `VillagePiece` | Abstract building piece base; extends `nk`; stores BoundingBox `e`, facing `f`, ground height `a`; `b(ry,nl)` scans ground height |
| `za` | `VillageRoadBase` | Abstract road piece base; extends `xf`; marker class only |
| `nk` | `StructurePiece` | Abstract piece for all structures; AABB, facing; `a(List,nl)` collision query; `a(yp,List,Random)` expansion |
| `aan` | `WeightedPiece` | Weight+count descriptor for village piece selection; fields a=class, b=weight, c=currentCount, d=maxCount |
| `xy` | `VillagePieceRegistry` | Static helper; `a(Random,depth)` builds weighted piece list; `c()` selects piece; road depth limit 3+b, building radius 112 |
| `yp` | `VillageStartPiece` | Village starting node; extends `aia`; fields a=WorldChunkManager, b=depth, c=lastPieceType, d=pieceList, h=roadQueue, i=buildingQueue |
| `yo` | `VillageStart` | StructureStart for villages; valid if >2 non-road pieces generated |
| `uy` | `VillageSmallHut` | 5×6×5 small hut; cobblestone walls, plank roof, random fence variant; weight 4 |
| `uz` | `VillageLargeHouse` | 5×12×9 large two-storey house; oak+glass; chest inside; weight 20 |
| `gs` | `VillageBlacksmith` | 9×9×6 open-front building; stone/sandstone walls; weight 20 |
| `wi` | `VillageHouseSmall2` | 4×6×5 small house; random `b` roof flag + `c` material variant; weight 3 |
| `acz` | `VillageLibrary` | 9×7×11 library; bookshelves, fence-enclosed; weight 15 |
| `ec` | `VillageFarmLarge` | 13×4×9 farm; farmland rows, fence perimeter, crops; weight 3 |
| `agr` | `VillageFarmSmall` | 7×4×9 small farm; weight 3 |
| `ko` | `VillageHouseLarge2` | 10×6×7 large house; stone-brick roof band; weight 15 |
| `tf` | `VillageChurch` | 9×7×12 tall building; weight 8 |
| `abj` | `VillageWell` | 3×4×2 well; cobblestone column + water cross pattern at top |
| `ahz` | `VillageStreet` | Road segment between buildings; extends `za`; spawns buildings on both sides, branches at ends |

## Stronghold Piece Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `os` | `StrongholdPiece` | Abstract base; stores BoundingBox, facing direction, door type (`mj`), depth counter |
| `mj` | `DoorType` | Door-type enum: `a`=open archway, `b`=wood door (ID 64), `c`=iron door (ID 71), `d`=iron bar grating (ID 101) |
| `tc` | `StrongholdPieceFactory` | Static factory; `c()` recursively extends branches; depth limit 50; XZ radius 112; `vn` fallback terminator |
| `ci` | `WeightedPiece` | Weight+count descriptor; fields: class, weight, maxCount; `iz` and `th` are subclasses for Library and PortalRoom |
| `aeh` | `StrongholdStart` | Extends `vl`; additional fields: `a`=last piece type used, `b`=portal room reference (`ir`), `c`=all-pieces list |
| `gp` | `SimpleCorridor` | 5×5×7; primary stone-brick corridor; cobwebs + torches; 1 forward exit |
| `vn` | `StraightCorridor` | 5×5×var; fallback terminator piece; variable length `a`; no loot |
| `fj` | `Prison` | 9×5×11; iron-bar cells; chest loot; 1 forward exit |
| `hq` | `LeftTurn` | 5×5×5; 90° left turn; no features |
| `xg` | `RightTurn` | 5×5×5; 90° right turn; no features |
| `jt` | `Crossing` | 11×7×11; up to 3 exits (F/L/R); torch + cobblestone column supports |
| `kt` | `LargeRoom` | 10×9×11; forced as first piece after `aeh`; 2 exits; chest loot |
| `so` | `SpiralStairs` | 5×11×8; stone-brick slab spiral; descends 7 blocks; 1 exit at bottom |
| `vl` | `StraightStairs` | 5×11×5; straight flight; descends 7 blocks; isStart=true → forces `kt` ahead |
| `ys` | `SmallRoom` | 5×5×7; dead-end alcove; possible chest |
| `zc` | `Library` | 14×11×15 (tall, c=false) or 14×6×15 (short, c=true); bookshelves + loot chest; max 2 per stronghold |
| `ir` | `PortalRoom` | 11×8×16; sets `((aeh)parent).b=this`; End Portal Frame (ID 120); water pool; silverfish spawner (`ze`); max 1 per stronghold |

## World Access / Sound / Particle Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `bd` | `IWorldAccess` | Interface; 9 overloads of method `a` + `b(ia)`; listener for world events: block change, sound, particle, entity add/remove; implemented by `afv` |
| `afv` | `WorldRenderer` / `RenderGlobal` | Implements `bd`; primary client-side renderer; chunk section list; WorldEvent dispatch handler `a(vi,int,int,int,int,int)`; events 1000–2004 |
| `qv` | `WorldGenLakes` | Lake generator; extends `ig`; field `a`=fluid block ID; 4-7 ellipsoids in 16×16×8 space; bottom=fluid / top=air; grass→dirt post-processing |

## Client / Rendering Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `adt` | `EntityRenderer` | First-person view controller; fields r=Minecraft, c=ItemRenderer(`n`), B/C=4.0F FOV modifiers; LWJGL+GLU; manages projection matrix, view bob, hand rendering |
| `n` | `ItemRenderer` | Hand/item renderer; fields a=Minecraft, b=held ItemStack, e=RenderBlocks(`acr`), f=sg; `a(nq,dk,int)` renders held item in first person |
| `xe` | `GuiScreen` | GUI screen base; extends `ht`; fields l=Minecraft, m/n=width/height, o=List<ct> buttons; drawScreen, keyTyped (ESC closes), mouseClicked (plays "random.click") |
| `ct` | `GuiButton` | GUI button; fields id/x/y/width/height/enabled/visible/displayString |
| `qd` | `GuiIngame` | In-game HUD overlay; extends `ht`; renders hotbar, health, pumpkin head, chat; uses `ef` scale factor |
| `mg` | `GuiContainer` | Abstract GUI container base; extends `xe`; fields b=176/c=166 (default W/H), d=Container, e/f=screenTopLeft; draws slots, held-item, tooltips |
| `hw` | `GuiInventory` | Player inventory screen; extends `mg`; uses `/gui/inventory.png`; player model at (e+51,f+75) scale 30; active effects sidebar; opens `gd` container |
| `zh` | `TextureManager` | NOT FontRenderer — texture atlas manager; manages GL texture binding and animated tile textures |

## Dispenser / Projectile Dispatch

| Obfuscated | Human name | Notes |
|---|---|---|
| `cu` | `BlockDispenser` | Dispenser block (ID 23); facing meta 2-5; dispatches arrow/snowball/egg/splash-potion as projectiles; falls back to EntityItem drop; fires worldEvent 2000 (smoke) on dispatch; drops inventory on break |
| `bp` | `TileEntityDispenser` | Already listed under TileEntity Classes — `a(Random)` selects random non-empty slot |

## Player / GameMode Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `vi` | `EntityPlayer` | (Updated) fields: by=InventoryPlayer, bz=Container(open), L=1.62F eye height, cc=PlayerAbilities(`wq`) |
| `wq` | `PlayerAbilities` | Player ability flags: a=invulnerable, b=flying, c=mayfly(can fly), d=instabuild(instant-break); NBT keys: invulnerable/flying/mayfly/instabuild |
| `di` | `EntityPlayerSP` | Extends `vi` directly; extra fields: b=agn(MovementInput), c=Minecraft, d=sprint timer, e=sleep timer (600 when sleeping); dimension travel via `c(int)` (dim==1→end credits); block-break dispatches through Minecraft.c (ItemInWorldManager) |
| `zb` | `EntityOtherPlayerMP` | Network player; extends `vi`; L=0.0F eye height, W=true noClip; position interpolation via `c` counter |
| `aes` | `ItemInWorldManager` | Abstract base; field a=Minecraft; `a(x,y,z,face)`=break block (event 2001 + setAir + drops); `b(ry)`=create EntityPlayerSP; `c()`=reach 4.0F |
| `dm` | `SurvivalItemInWorldManager` | Extends `aes`; fields c/d/e=target xyz (int, -1=none), f=blockDamage (0-1.0), g=prevDamage, h=soundCounter, i=breakCooldown(5); `c(x,y,z,face)` per-tick: f+=block.a(player), break at f>=1.0, sound every 4 ticks; `a(partial)`=update crack overlay |
| `uq` | `CreativeItemInWorldManager` | Extends `aes`; `i()`=true; static `b(vi)`=set creative abilities; instant-break in `c()` |

## Minecraft Main Loop Classes

| Obfuscated | Human name | Notes |
|---|---|---|
| `Minecraft` (in `net.minecraft.client`) | `Minecraft` | Abstract singleton (`static a`); holds all game state fields; run()→x() game loop; `k()`=one 20Hz tick; `ahy` is the concrete subclass |
| `ahy` | `MinecraftClient` | Concrete runnable Minecraft subclass; extends `Minecraft implements Runnable`; created on main thread |
| `aij` | `Timer` | Game timer; constructor param=ticksPerSecond (20.0F); field `b`=elapsed ticks (int), `c`=renderPartialTicks (float 0-1); `a()` advances timer |
| `ki` | `GameSettings` | Settings container; fields: A=soundEnabled, D=hideGUI, E=viewMode (0-2), F=debugOverlay, I=smoothCamera; holds KeyBinding array for all key actions |
| `agn` | `MovementInput` | Keyboard-to-movement mapper; field on `di.b`; maps WASD+Shift+Sprint state to forwardMove/strafe floats; fields a=strafe, b=forward, c=sneak, d=jump, e=dive |
| `hs` | `MouseHelper` | LWJGL mouse delta reader; fields a=getDX result, b=getDY result; `c()` reads Mouse.getDX/getDY; `a()` grabs mouse; held as `Minecraft.D` |
| `ne` | `SmoothValue` | Exponential smoother; `a(current, factor)` = lerp toward current by factor; used in `adt` for smooth camera J/K accumulators |
| `mv` | `ChunkProviderClient` | Client-side chunk provider; `d(cx, cz)`=centerOn (sets cache centre for chunk loading); used by `Minecraft.e()` spawn preloader and world load |
| `ef` | `ScaledResolution` | Computes GUI scale factor from window size + GUI scale setting; `a()`=scaledWidth, `b()`=scaledHeight |
| `ht` | `GuiScreenBase` | Base class for GUI rendering; `b(x,y,u,v,w,h)`=drawTexturedModalRect |
| `abe` | `FontRenderer` | Bitmap font renderer; fields: A=GameSettings, p=TextureManager; `q=new abe(A,"/font/default.png",p)` in Minecraft ctor; `a(String,x,y,color)`=drawString; `b(String,x,y,color)`=drawStringWithShadow |

---

*Add new mappings as classes are analyzed. Keep alphabetical by obfuscated name within each section.*
