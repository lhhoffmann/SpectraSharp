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
| `acr` | `RenderBlocks` | Per-face OpenGL renderer, 5064 lines |
| `wu` | `Material` | Block material (stone, wood, cloth, …) |
| `zx` | `Chunk` | 16×16×128 chunk data |
| `ry` | `World` | World/level root object |
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
| `k` | `WorldProvider` | 120 lines; abstract; dimension rules; f[16]=brightness table; e=isNether; subclasses ix/aau/ol |
| `ix` | `WorldProviderSurface` | Overworld (dim 0) |
| `aau` | `WorldProviderHell` | Nether (dim −1) |
| `ol` | `WorldProviderEnd` | End (dim 1) |

---

*Add new mappings as classes are analyzed. Keep alphabetical by obfuscated name within each section.*
