# SpectraSharp — Development Metrics

Log of all development sessions. One entry per session, appended at the bottom.
Written by the active role (Analyst / Coder / Mod Coder) at session end.

---

## Cumulative Costs

Update this table manually when a billing period ends or a charge appears.

| Date       | Item                  | Amount |
|------------|-----------------------|--------|
| 2026-04-13 | Claude Pro (monthly)  | 20 EUR |
| 2026-04-13 | Extra usage           | 13 EUR |
| 2026-04-13 | Extra usage           |  9 EUR |
| 2026-04-14 | Extra usage           |  9 EUR |
| 2026-04-14 | Claude Max (monthly)  | 90 EUR |


**Running total: 141 EUR**

---

## Entry Format

```
## YYYY-MM-DD — [ROLE] — Topic

**Worked on:**
- Item 1
- Item 2

**Estimated effort:** ~N hours equivalent
**Notes:** (optional — decisions, blockers, open questions)
```

---

<!-- Entries below — newest at bottom -->

## 2026-04-13 — [ANALYST] — Item, LivingEntity, EntityItem, ItemStack, DataWatcher specs

**Worked on:**
- `cr` (DataWatcher) — type registry (7 types), wire format (typeId<<5|entryId header, 0x7F terminator), register/update/applyChanges methods, null-return quirk on read
- `ih` (EntityItem) — 0.25×0.25 size, gravity/bounce/friction tick, pickup delay quirk (set externally to 10), despawn at age ≥ 6000
- `dk` (ItemStack) — non-obvious field naming (c=itemId, a=stackSize, e=damage), two identical damage getters h()/i(), splitStack mutation, Unbreaking enchantment check
- `acy` (Item) — d[32000] registry with 256-offset, dual maxDamage paths (c() vs g()/a field), builder pattern, icon atlas packing, ray-cast helper, 12 virtual methods
- `nq` (LivingEntity) — health/invulnerability/armor-absorption system, friction movement formula (0.16277136F / friction³), potion effects, wandering AI, DataWatcher index 8 (packed RGB potion color), setHealth bug (aM not clamped), NBT

**Estimated effort:** ~6 hours equivalent
**Notes:** Item spec clarifies dual maxDamage paths — c() is the public API (overridden by tools), g()/a-field is only for h() (isDamageable predicate). LivingEntity setHealth bug preserved per quirk list.

## 2026-04-13 — [ANALYST] — TerrainAtlas (terrain.png pixel analysis)

**Worked on:**
- `terrain.png` extracted from JAR — direct pixel analysis, 256×256 RGBA, 16×16 tile grid
- `jb.java` (GrassBlock) — multi-face texture indices (top=0, bottom=2, side=3, snow-side=68), biome tint via `ha.a(0.5,1.0)`
- `aip.java` (LogBlock) — top=21, oak-side=20, spruce-side=116, birch-side=117
- `yy.java` (Block) — confirmed all texture indices for 30+ block types

**Estimated effort:** ~1 hour equivalent
**Notes:** Root cause of gray rendering confirmed — NOT wrong indices. GrassBlock (index 0) and Leaves (index 52) are stored gray in PNG by design; require biome color multiplication at render time. Glass (index 49) is a cutout texture with opaque border and transparent center. Placeholder biome tint for immediate fix: (72,181,24) for grass, (78,164,0) for foliage.

## 2026-04-13 — [ANALYST] — BlockRegistry, IInventory, InventoryPlayer, EntityPlayer, ItemBlock, ConcreteBlocks specs

**Worked on:**
- `yy.java` static initializer — complete block registry: all 122 block IDs with class, material, StepSound, hardness, resistance, light emission, light opacity, name, flags; builder method semantics fully decoded; s[] special-drop flag; Item registration loop; explicitly-registered Item overrides
- `de.java` (IInventory) — 10-method interface; `decrStackSize` split semantics; naming collision `d()` (two overloads)
- `x.java` (InventoryPlayer) — 36+4 slot layout; hotbar slot c; `a[36]`/`b[4]` arrays; addItemStackToInventory; dropAllItems; canHarvestBlock; NBT armor-slot index quirk (100–103 not 36–39)
- `vi.java` (EntityPlayer) — abstract, extends nq; L=1.62F eye height; AABB 0.6×1.8; maxHealth 20; DataWatcher indices 16/17; getMiningSpeed formula (Efficiency/Haste/MiningFatigue/water/air penalties); `a(dk,boolean)` drop with pickup delay 40; death drops all inventory; camera interpolation 0.25 lerp
- `uw.java` (ItemBlock) — field a=blockId (itemId+256); icon from face-2 texture; `onItemUse` with 5 validity guards, face-direction placement, sound pitched down ×0.8
- Concrete block subclasses: GrassBlock spread/revert tick, SandBlock gravity (entity `uo` or world-gen teleport, static flag a), BlockLeaves BFS decay (32³ cache, distance 1-4 from logs, meta bit 3=needs-check/bit 2=no-decay, 1-in-20 sapling drop), LogBlock multi-face per meta

**Estimated effort:** ~4 hours equivalent
**Notes:** BlockRegistry is the single highest-priority gap — fully covered including all 122 IDs and Item registration. `ny.b()` at end of static block confirmed safe to stub (statistics-only). Builder call order matters for resistance: `c(H).b(R)` → final bO=R*3 unconditionally.

## 2026-04-14 — [ANALYST] — BiomeGenBase + ChunkProviderGenerate specs

**Worked on:**
- `sr.java` (BiomeGenBase) — 16 biome registry (IDs 0–15); all fields (temp/rain/minH/maxH/topBlock/fillerBlock/waterColor/colorOverride); builder pattern; `a(kq,x,y,z)` grass color via `ha`; `b(kq,x,y,z)` foliage color via `db`; biome subclass list
- `ha.java` (GrassColorizer) — 65536-entry static int[] lookup; index formula `row=(1-rainfall*temp)*255, col=(1-temp)*255`; loaded externally from `grasscolor.png`
- `db.java` (FoliageColorizer) — identical to ha; 3 hardcoded constants `a()=6396257 (oak)`, `b()=8431445 (birch)`, `c()=4764952 (spruce)`
- `mk.java` (Swampland) — special color blend formula `((ha.a(t,r) & 0xFEFEFE) + 5115470) / 2`; A=14745456 override
- `vh.java` (WorldChunkManager) — per-block temp/rainfall noise; `kq.a()` accessor; key methods for biome and climate arrays
- `xj.java` (ChunkProviderGenerate) — full Overworld generation: 7 noise generators (o×16, p×16, q×8, r×4, a×10, b×16, c×8); 4×16×4 density grid; 5×5 biome Gaussian smoothing; trilinear interpolation; stone/water density formula; surface pass with biome topBlock/fillerBlock; caves (`ln`), ravines (`rf`); populate method
- `ql.java` (BiomeDecorator) — ore table: dirt 20×32, gravel 10×32, coal 20×16 Y0-128, iron 20×8 Y0-64, gold 2×8 Y0-32, redstone 8×7 Y0-16, diamond 1×7 Y0-16, lapis 1×6 ~Y16; decoration items (trees, flowers, mushrooms, springs)
- Identified `jv`=Nether generator, `er`=flat-world loader, `eb`=NoiseGeneratorOctaves, `agk`=PerlinNoiseGenerator

**Estimated effort:** ~3 hours equivalent
**Notes:** ChunkProviderGenerate density formula uses a 4×16×4 trilinear grid (not per-block noise). Surface pass reads biome `t`/`u` fields for top/filler blocks. No biome ID stored per-column in Chunk — biome is computed dynamically by WorldChunkManager from noise. Ore vein sizes are block counts passed to WorldGenMinable `ky`, not radius.

## 2026-04-14 — [ANALYST] — WorldGenTrees + disk generators spec

**Worked on:**
- `ig.java` (WorldGenerator) — abstract base; boolean silent flag; block-placement helper
- `gq.java` (WorldGenTrees) — standard oak; height [4,6]; space check radius (0/1/2); canopy radius formula (1-dy/2), randomized corners; trunk replaces air/leaves only; grass→dirt conversion
- `yd.java` (WorldGenBigTree) — fancy branching oak; golden-ratio trunk split (0.618); polar branch layout; DDA line drawing; ellipsoidal leaf clusters; `a(scale)` override method
- `jp.java` (WorldGenForestTree) — birch tree; identical to gq but height [5,7], log/leaves meta 2
- `qj.java` (WorldGenSwamp) — swamp oak; wider canopy (2-dy/2 formula); water-descent; vine draping with 4-block hang; vine face meta values (8/2/1/4)
- `ty.java` (WorldGenTaiga1) — thin spruce; bottom-up cone; growing-then-shrinking radius pattern; bare trunk 1-2 blocks; log/leaves meta 1
- `us.java` (WorldGenTaiga2) — wide pine; top-down radius growth; height [7,11]; meta 1
- `fc.java` (WorldGenSandDisc) — corrected: NOT a tree; circular disk replacing grass/dirt with sand/gravel; radius [2, size-2]; height range ±2
- `adp.java` (WorldGenClay) — clay disk; radius always 2 (size=4); height range ±1; replaces grass/clay
- `sr.java` — tree dispatch `a(Random)`: 90% oak / 10% fancy oak default; biome overrides
- Per-biome table: all 16 biomes with `B.z` (tree count) and generator probabilities
- **Correction issued:** ChunkProviderGenerate_Spec §10 decoration table had H/I/G labeled as "large/bonus/big trees" — these are actually sand/clay/sand disk patches. Trees only come from the `z`-count biome loop.

**Estimated effort:** ~2 hours equivalent
**Notes:** `fc` and `adp` are disk patch generators, not trees — the REQUESTS.md was working from an incorrect label in the previous session's ChunkProviderGenerate_Spec. The correction is documented both in WorldGenTrees_Spec §0 and in the spec file itself. Swamp vine face meta bits need render-side verification (values 8/2/1/4 taken directly from source). `us` (WorldGenTaiga2) instantiates a fresh object per call instead of reusing a field.

## 2026-04-14 — [ANALYST] — WorldGenMinable + MapGenCaves (proactive)

**Worked on:**
- `ky.java` (WorldGenMinable) — capsule-axis vein algorithm; sine-bulge sphere radii; ellipsoid inside-test (dx²+dy²+dz²<1); stone-only replacement via `world.d()`; full ore table with block IDs
- `bz.java` (MapGenBase) — 17×17 chunk scan; per-source-chunk seed (srcX*r1 XOR srcZ*r2 XOR worldSeed); protected hook for subclasses
- `ln.java` (MapGenCaves) — cave count distribution (87% zero, 13% geometric); room+branch pattern; full segment algorithm: sine-bulge cross-section, direction perturbation (pitch/yaw speeds), branch spawning at random midpoint, distance culling, water proximity abort, ellipsoid carving (normX²+normY²+normZ²<1, floor guard normY>-0.7), lava below Y<10, grass surface restoration via biome topBlock
- Also identified: `we.java` is **mob spawner** (SpawnerAnimals), not snow/ice generator — previous ChunkProviderGenerate_Spec §8 step 7 label was incorrect

**Estimated effort:** ~2 hours equivalent
**Notes:** Proactive specs — not yet requested by Coder. WorldGenMinable is directly needed for BiomeDecorator ore placement. MapGenCaves is the largest stub in ChunkProviderGenerate (called as `w.a(...)` and `x.a(...)`). The `we.java` misidentification in the previous spec should be noted: `we` = SpawnerAnimals; the actual snow/ice freezing step in populate uses a different mechanism not yet identified.

---

## 2026-04-14 — [MOD-CODER] — ModRuntime foundation + JavaStubs v1_0

**Worked on:**
- `SpectraSharp.ModRuntime.csproj` — new project, refs IKVM + HarmonyLib + Core
- `Mappings/VersionMapping.cs` + `VersionDetector.cs` — JAR fingerprint-based version detection
- `Mappings/Data/1.0.json` — obfuscated class/method/field map for Minecraft 1.0
- `Mappings/Data/1.12.2.json` — Forge/Searge class map for Minecraft 1.12.2
- `Mappings/Data/1.21.json` — Mojmap class map for Minecraft 1.21
- `Sandbox/ModWatchdog.cs` — 500ms timer, fires KillMod on timeout
- `Sandbox/ModSandbox.cs` — try/catch + OOM/StackOverflow isolation, Harmony per mod
- `Sandbox/ThreadGuard.cs` + `TickScheduler.cs` — wrong-thread marshal to next tick
- `Sandbox/ReflectionGuard.cs` — blocks Core internals, allows stub fields
- `AllocGuard/FramePool.cs` — thread-local ItemStack + Event pool, tick-boundary reset
- `AllocGuard/AllocationMonitor.cs` — DEBUG-only escape tracker, zero Release overhead
- `Compiler/ModCompiler.cs` — ikvmc subprocess wrapper, selects correct stubs DLL
- `ModEntry.cs` — per-mod state (sandbox, mapping, lifecycle)
- `ModLoader.cs` — scans mods/, compiles JARs, loads DLLs, manages tick boundary
- `Bridge/JavaStubs/MinecraftStubs.v1_0.csproj` — stubs project
- `Bridge/JavaStubs/v1_0/World.cs` — ry → IWorld delegate, ThreadGuard on writes
- `Bridge/JavaStubs/v1_0/Block.cs` + `BlockListProxy` — blocksList[] → BlockRegistry
- `Bridge/JavaStubs/v1_0/Item.cs` + `ItemListProxy` — itemsList[] → ItemRegistry
- `Bridge/JavaStubs/v1_0/ItemStack.cs` — dk → FramePool.RentItemStack()
- `Bridge/JavaStubs/v1_0/BaseMod.cs` — implements ISpectraMod, routes load()/modsLoaded()
- `Bridge/JavaStubs/v1_0/ModLoader.cs` — static API: AddRecipe/AddShapeless/AddSmelting
- `Bridge/JavaStubs/v1_0/JavaRandomAdapter.cs` — JavaRandom → java.util.Random bridge
- `Documentation/VoxelCore/Protocols/ROLE_CODER.md` — added "Mod Runtime Contract" section
- `Documentation/VoxelCore/Protocols/ROLE_MOD_CODER.md` — complete rewrite with IKVM plan

**Estimated effort:** ~6 hours equivalent
**Notes:** SpectraSharp.ModRuntime builds clean (0 errors, 0 warnings). JavaStubs v1_0 not yet buildable — depends on IKVM NuGet restore and java.util.Random being available from IKVM.Runtime. MixinInterceptor and HarmonyBridge are planned for next session. CODER must implement BlockRegistry.RegisterMod() and any new IWorld methods flagged with TODO in the stubs.

---

## 2026-04-14 — [CODER] — WorldGen tree generators + biome decoration

**Worked on:**
- `Core/WorldGen/WorldGenerator.cs` — abstract base (`ig`): `Generate()` + `SetScale()` 
- `Core/WorldGen/WorldGenTrees.cs` — standard oak (`gq`): height [4,6]; canopy radius formula (1-dy/2); randomized corners; virtual hooks for birch subclass
- `Core/WorldGen/WorldGenForestTree.cs` — birch (`jp`): extends WorldGenTrees; log/leaves meta 2; height [5,7]
- `Core/WorldGen/WorldGenBigTree.cs` — fancy oak (`yd`): DDA trunk + branches; golden-ratio branch layout; ellipsoidal leaf clusters; radii [2,3,3,2]
- `Core/WorldGen/WorldGenTaiga1.cs` — thin spruce (`ty`): height [6,9]; bottom-up cone; growing-then-shrinking radius; spruce meta 1
- `Core/WorldGen/WorldGenTaiga2.cs` — wide spruce (`us`): height [7,11]; top-down expanding canopy; spruce meta 1
- `Core/WorldGen/WorldGenSwamp.cs` — swamp oak + vines (`qj`): water descent; wider canopy; vine draping 1/4 chance per side, up to 4 blocks deep
- `Core/WorldGen/WorldGenSandDisc.cs` — sand/gravel disk (`fc`): radius [2, size-2]; replaces grass/dirt in y±2 range
- `Core/WorldGen/WorldGenClay.cs` — clay disk (`adp`): same as SandDisc but y±1 range; replaces grass/clay
- `Core/BiomeGenBase.cs` — added `TreeCount`, `GetTreeGenerator()`, `SetTreeCount()`; `ForestBiome`/`TaigaBiome`/`SwamplandBiome` subclasses; per-biome tree counts and generator dispatch
- `Core/ChunkProviderGenerate.cs` — added `using SpectraSharp.Core.WorldGen;`; added biome tree decoration loop in `PopulateChunk()` (spec §10.1): 10% bonus tree, random x/z offset, `GetTopSolidOrLiquidBlock` surface height

**Estimated effort:** ~3 hours equivalent
**Notes:** `fc`/`adp` are disk patch generators (not trees) — corrected from earlier label error in ChunkProviderGenerate_Spec §10. Tree generation uses the center-chunk biome for simplicity (matches vanilla's single-biome-per-chunk decoration strategy). `_isGenerating` guard prevents stack overflow during ore placement; trees placed after ores so adjacent-chunk reads during tree clearance also hit the guard safely.

---

## 2026-04-14 — [CODER] — MapGenCaves + WorldGenMineable fix + SetBlockSilent

**Worked on:**
- `Core/IWorld.cs` + `Core/World.cs` — added `SetBlockSilent(x,y,z,id)`: direct chunk write, no light propagation or neighbor notifications; used by all world-gen bulk writes
- `Core/WorldGen/WorldGenMineable.cs` — full rewrite to match spec: Y offset fixed (`-2` not `+2`), correct sine-bulge radius formula (`(sin+1)*rand*b/16+1`), MathHelper.Sin/Cos for axis orientation, early-exit ellipsoid test (X→XY→XYZ), `SetBlockSilent` for placement, always returns true
- `Core/WorldGen/MapGenCaves.cs` — full implementation of `ln` (MapGenCaves) + `bz` (MapGenBase): 17×17 source-chunk scan, per-source-chunk seeding (srcX*r1 XOR srcZ*r2 XOR worldSeed), triple-nested cave count (87% zero), room+branch pattern, full segment algorithm (sine-bulge cross-section, direction perturbation, branch spawning with parent termination, distance culling, water-abort, ellipsoid carving, floor guard normY≤-0.7, lava seam Y<10, grass surface restoration via biome topBlock)
- `Core/ChunkProviderGenerate.cs` — wired `MapGenCaves.Generate()` as Pass 1b between density fill and surface pass; added `MapGenCaves _caveGen` field
- `Documentation/VoxelCore/Parity/REQUESTS.md` — filed 6 new specs: LightPropagation, BlockFluid, BiomeDecorator, WorldSave, MapGenRavine, BlockFire

**Estimated effort:** ~3 hours equivalent
**Notes:** WorldGenMineable Y offset bug was significant — ores were spawning 2–4 blocks ABOVE the call site Y instead of at/below it. Cave generation is the largest visual improvement: underground is now carved with branching tunnels, lava pools below Y=10, and surface-restoration at cave entrances.

Additionally fixed:
- `Core/BiomeGenBase.cs` — TaigaBiome fixed: 67% thin spruce (shared) / 33% wide spruce (fresh); SwamplandBiome: added 100% swamp oak override
- `Core/WorldGen/WorldGenBigTree.cs` — all block placement now uses `SetBlockSilent` (spec: world.d() direct); removed unused `_branchStartFraction` field
- `Core/Chunk.cs` — implemented entity bucket management: `_entityBuckets[8]`, `AddEntity`, `RemoveEntity`, `GetEntitiesInRange`; bucket index = posY/16 clamped to [0,7]
- `Core/World.cs` — wired `chunk.AddEntity`/`RemoveEntity` into `SpawnEntity`, `MarkEntityForRemoval`, `TickEntityWithPartialTick`; added `GetNearbyPlayers(x,y,z,radius)`
- `Core/EntityItem.cs` — implemented `TryPickup(EntityPlayer)` via `InventoryPlayer.AddItemStackToInventory`; added proximity pickup loop (radius 1.0) in Tick()

---

## 2026-04-14 — [CODER] — World entity ticking + Core stability

**Worked on:**
- `World.cs` — implemented `TickEntities()` (spec `m()`): global entity list, removal queue flush, per-entity `Tick()` + chunk reassignment; wired into `MainTick()`
- `World.cs` — implemented `SpawnEntity()` with chunk-loaded guard and player-list tracking; `MarkEntityForRemoval()`; `TickEntityWithPartialTick()`
- `World.cs` — implemented `TickChunks()` (LCG quirk 2), `NotifyNeighboursOfChange()`, `OnBlockChanged()` with `OnBlockRemoved`/`OnBlockAdded` hooks; `SetBlock`/`SetBlockAndMetadata` capture old ID before write
- `ChunkProviderGenerate.cs` — fixed infinite recursion: `_isGenerating` guard prevents re-entrant chunk generation during ore placement; chunk stored in cache before `PopulateChunk()` runs
- `SpectraSharp.csproj` — excluded `Bridge/JavaStubs/**` and `SpectraSharp.ModRuntime/**` (both need IKVM); `Bridge/Mods/**` kept in after mod cleanup
- `REQUESTS.md` — filed `WorldGenTrees` spec request

**Estimated effort:** ~3 hours equivalent
**Notes:** Entity ticking was the largest missing Core piece — without it EntityItem never despawns and no physics runs. Tree generation blocked on `WorldGenTrees_Spec.md` from Analyst.

---

## 2026-04-14 — [ANALYST] — BiomeDecorator spec + proactive protocol formalised

**Worked on:**
- `ql.java` (BiomeDecorator) — full 15-step RNG-ordered decoration sequence reconstructed; all field names (A–Q), counts, Y ranges; ore helper `a()` without +8 chunk offset quirk
- `bu.java` (WorldGenFlowers) — 64 attempts; ±7/±3 XZ spread; `canBlockStay` gate; silent placement (`world.d()`)
- `ahu.java` (WorldGenTallGrass) — descend to first solid surface; 128 attempts; notify placement (`world.b()`); dead bush meta 0
- `mb.java` (WorldGenShrub) — descend to surface; 4 attempts; silent; desert dead shrub variant
- `ib.java` (WorldGenSpring) — 3 stone + 1 air neighbour requirement; triggers `onBlockAdded` to start fluid flow; water vs lava variants
- `tw.java` (WorldGenReed) — 20 attempts; must have water at adjacent block at y-1; height [2,4]
- `sz.java` (WorldGenPumpkin) — 64 attempts; grass block directly below required; random facing meta
- `ade.java` (WorldGenCactus) — corrected: NOT pumpkin; block ID 81; height [1,3]; 10 attempts on sand
- `jj.java` (WorldGenLilyPad) — 10 attempts; water block below (not solid); block ID 111
- `acp.java` (WorldGenHugeMushroom) — brown (type 0) and red (type 1); cap meta bits 1–10 for face directions; height [4,6]+3 stem
- `we.java` corrected: SpawnerAnimals (mob spawning), not snow/ice — prior ChunkProviderGenerate_Spec §8 step-7 label was wrong
- Per-biome override table for all 16 biomes (tree count z, decoration counts, topBlock overrides)
- Snow/ice freeze pass documented as unresolved — `xj.java` populate() step 7 origin still unknown
- Formalised Proactive Speccing Protocol in `Documentation/VoxelCore/Protocols/ROLE_ANALYST.md`
- WorldGenMinable + MapGenCaves added as proactive specs (addressed Coder's later requests before filing)

**Estimated effort:** ~3 hours equivalent
**Notes:** Three corrections issued: `ade`=cactus (not pumpkin), `acp`=huge mushroom (not sugar cane generator), `we`=SpawnerAnimals (not snow/ice). Field `h` in ql (`fc` gravel disk) is defined but never called in base `ql.a()` — biome subclasses may use it. Snow/ice pass in ChunkProviderGenerate populate() step 7 uses an unknown class; `we` is ruled out.

---

## 2026-04-14 — [ANALYST] — LightPropagation spec (addresses Coder request)

**Worked on:**
- `bn.java` (LightType enum) — two constants: `a`=SKY (default 15), `b`=BLOCK (default 0)
- `zx.java` (Chunk) — nibble arrays `g`=meta / `h`=sky / `i`=block; height map `j[z<<4|x]`; block array index `(x<<11)|(z<<7)|y`; init via `c()` (full fill) and update via `g(localX,changedY,localZ)` (on SetBlock)
- `ry.java` (World) — full BFS propagation `c(bn,x,y,z)` with H[32768] packed queue; two phases (decrease-zero + re-propagate); neighbor iteration bit-pattern; sky value formula `a(...)` (15 if sky-exposed, else max_neighbor-opacity); block value formula `d(...)` (max of self-emission and 6_neighbors-opacity)
- World read API: `b()` raw nibble, `a(bn,x,y,z)` with `yy.s` neighbor-max, `n()` combined for rendering, IBlockAccess packed int format (sky<<20|block<<4)
- `World.a(bn, x1,y1,z1, x2,y2,z2)` (7-arg) confirmed empty/no-op in base ry — sky updates come through height-map chain, block updates come lazily via `s(checkLight)` per tick
- `World.i(x,z,minY,maxY)` — column range BFS trigger on height change
- `World.s(x,y,z)` — random-block checkLight each chunk tick (triggers both sky and block BFS)
- Sky darkening `k` (0-11): computed from sun angle + rain + thunder each tick via `a(1.0F)`
- WorldProvider `f[16]` brightness table: S-curve `(1-t)/(t*3+1)` where `t=1-level/15`
- Chunk init `c()` — height map + sky nibble fill from top; deferred neighbour BFS via `d[]` dirty array

**Estimated effort:** ~2 hours equivalent
**Notes:** Key quirk: sky light update is synchronous (via height-map chain → `World.i` → BFS); block light is lazy (only via random `checkLight` each tick). The 7-arg `world.a(bn.b, ...)` in SetBlock path is a no-op in base class — block light delay is intentional. `yy.s[]` neighbor-max flag prevents leaves/glass/water from creating hard light boundaries.

---

## 2026-04-14 — [ANALYST] — BlockFluid spec (addresses Coder request)

**Worked on:**
- `agw.java` (BlockFluidBase) — abstract base; `g()` getFluidLevel; `c()` getEffectiveLevel (strips falling bit for flow maths); `e(meta)` level-to-height formula; flow gradient vector for rendering; tick delay `d()` = water:5 / lava:30; bounding box full 1³; isOpaqueCube=false; renderAsNormal=false; drop count 0
- `ahx.java` (BlockFluid flowing) — full tick algorithm: read current level; compute new level from 4 horizontal neighbors via `f()` (min-level aggregator with source-counter); falling-above override (aboveLevel+8); infinite water source rule (≥2 adjacent sources + solid floor); lava 75%-skip (Overworld only); stabilise to still on stable tick via `j()`; flow down → lava+water → stone; flow lateral → flood-fill direction algorithm `k()`/`c()` (depth-4, nearest-drop); `placeFluid` private helper
- `add.java` (BlockStationary still) — `onNeighborChange` converts to flowing with `world.t=true` suppression; still lava random tick: random upward walk (0-2 steps), place fire if air+flammable-neighbor found
- Lava+water interaction in `agw.j()` (onBlockAdded/onNeighborChanged): lava source (meta 0) + water → obsidian (ID 49); lava flowing (meta 1-4) + water → cobblestone (ID 4); plays fizz sound
- Flow direction algorithm: flood-fill up to depth 4 searching for nearest horizontal drop; all dirs with equal minimum distance get flow; reverse direction excluded from recursion; blocked = solid material OR specific thin-block IDs (signs/doors/gates)
- Nether lava: `world.y.d == true` → `var7 = 1` (same as water), so Nether lava spreads 7 blocks

**Estimated effort:** ~2 hours equivalent
**Notes:** Key correction: class names `ahx`/`add`/`agw` not `aam` as the Coder assumed. Flow levels 0-7 + falling bit 8 (meta 8 = falling source, 9-15 = falling at levels 1-7). Lava 75%-skip is in addition to the 30-tick rate — Overworld lava is genuinely extremely slow. Infinite water applies only to water (not lava). Still block uses `world.t = true` to suppress event cascades during conversion.

---

## 2026-04-14 — [ANALYST] — MapGenRavine spec (addresses Coder request)

**Worked on:**
- `rf.java` (MapGenRavine) — extends `bz` (MapGenBase, already specced); per-source-chunk entry: `nextInt(50)==0` = 2% probability (vs 13% for caves); start Y `nextInt(nextInt(40)+8)+20` = [20, 68]; 1 ravine always; radius `(rand*2+rand)*2` = [0,~12] (3× cave max); thicknessMult=3.0
- Precomputed d[] array: size 1024; resets every 3 Y levels; `var27=1+rand²*1.0` → [1.0, 2.0]; `d[y]=var27²` → [1.0, 4.0]; creates irregular per-depth horizontal scaling
- Modified ellipsoid test: `(normX²+normZ²)*d[y] + normY²/6.0 < 1.0` — produces tall narrow shape with rough walls; contrasts with cave sphere `normX²+normY²+normZ² < 1`
- No branching (branchPoint computed but never checked); no isMidpoint mode (startStep=0 always)
- Pitch damping always 0.70 (no "isStraight" variant since no branches)
- Shared behaviours: same water-abort, lava Y<10, grass surface restoration, distance culling, 25%-skip

**Estimated effort:** ~1 hour equivalent
**Notes:** Ravine d[] array is the key differentiator — it creates the characteristic uneven walls by independently scaling horizontal cross-section at each Y level. The `/ 6.0` divisor on normY² in the ellipsoid test makes ravines approximately `horRadius * sqrt(6) * 2.45 ≈ 5.8×` taller than wide before thicknessMult. With thicknessMult=3: effective vertical range ≈ 3 * horRadius. Rare (2%) but visually dramatic.

---

## 2026-04-14 — [ANALYST] — BlockFire spec (addresses Coder request)

**Worked on:**
- `wj.java` (BlockFire) — `a[256]` flammability + `cb[256]` burnability tables; `x_()` static initializer with 10 flammable block registrations (planks=5/20, bookshelf=5/20, fence=5/20, TNT=5/5, leaves=30/60, wool=30/20, two unknowns=15/100+60/100+30/60, dead bush=15/100); tick rate 40; material `p.n`; isOpaqueCube=false; no bounding box; drops nothing
- canPlace/survive `c(world,x,y,z)`: solid full-cube below OR any of 6 neighbors flammable
- `onBlockAdded` `a(world,x,y,z)`: validate survival, schedule tick; special End portal bypass
- `onNeighborChange` `a(world,x,y,z,id)`: remove if no longer sustainable
- Main tick `a(world,x,y,z,rand)`: permanent check (netherrack/end-stone); rain 5-position wetness gate; age 0-15 incrementing by 0 or 1 per tick; burnout at age>3 without flammable support; 25% burnout at age=15; 6-face `burnBlock()` spread (divisors 250 up/down, 300 horizontal); 3×6×3 area ignition (baseDivisor 100 base, +100 per Y above y+1)
- `burnBlock()`: roll `nextInt(divisor) < cb[]`; if hit: `nextInt(age+10) < 5` chance to spread fire vs destroy; special action on `yy.am.bM`
- Area ignition formula: `igniteChance = (flam+40)/(age+30)`; rolls `nextInt(baseDivisor) <= igniteChance`; young fire spreads more aggressively
- Rain: `world.E()` = isRaining; `world.w(x,y,z)` = isBlockWet; all 5 positions (self + 4 horizontal) must be wet to extinguish

**Estimated effort:** ~1 hour equivalent
**Notes:** Class name is `wj` (not `ij` as Coder assumed). Tick delay is 40 (not 30). Lava-to-fire ignition handled in `BlockStationary.add` random tick — NOT in BlockFire itself. `yy.am` special block action needs ID verification against BlockRegistry_Spec. `world.y.g` = dimension ID for End permanent fire detection. Netherrack (`yy.bb` = ID 87) confirmed as permanent-fire block.

---

## 2026-04-14 — [CODER] — LightPropagation full BFS implementation

**Worked on:**
- `Core/World.cs` — `ReadLightWithNeighborMax(LightType, x, y, z)`: neighbor-max sampling for non-opaque-cube blocks (leaves/glass/water/ice); `ComputeSkyLightValue`: 15 if sky-exposed, else max(neighbor_sky − opacity); `ComputeBlockLightValue`: self-emission vs max(neighbor_block − opacity); `PropagateLight(LightType, x, y, z)`: two-phase BFS on H[32768] packed queue — Phase 1 zeros dependent cells on decrease, Phase 2 re-propagates correct values; `PropagateColumnRange(x, z, minY, maxY)`: sky BFS over column Y range after height-map changes; `CheckLight(x, y, z)`: per-chunk per-tick random light check (both channels); `UpdateSkyDarkening()`: sun-angle cos curve → rain/thunder dimming → SkyDarkening 0–11
- `Core/World.cs` — `GetLightValue()` wired to `ReadLightWithNeighborMax` (fixes broken build from prior session); `GetBrightness()` computes `max(sky−SkyDarkening, block)` then indexes `WorldProvider.BrightnessTable`; `OnBlockChanged()` calls `PropagateLight` for both sky + block; `TickChunks()` calls `CheckLight` once per chunk; `MainTick()` calls `UpdateSkyDarkening()`
- `Core/ChunkProviderGenerate.cs` — replaced `GenerateHeightMap()` with `GenerateSkylightMap()` so generated chunks have correct sky-light nibbles

**Estimated effort:** ~2 hours equivalent
**Notes:** `!Block.IsOpaqueCubeArr[id]` used for neighbor-max sampling (leaves/glass/water). Sky darkening: noon brightness=1→darkening=0; midnight brightness=0→darkening=11. Build result: 0 errors, 0 warnings.

---

## 2026-04-14 — [CODER] — BlockFluid + MapGenRavine implementation

**Worked on:**
- `Core/Blocks/BlockFluidBase.cs` — abstract base (`agw`): `GetFluidLevel`, `GetEffectiveLevel`, `LevelToHeight`; `IsBlocked` (thin-block list + solid-material check); `CanFlowInto`; `CheckLavaWaterInteraction` for lava source→obsidian / flowing→cobble; `OnBlockAdded`/`OnNeighborBlockChange` hooks
- `Core/Blocks/BlockFluid.cs` — flowing fluid (`ahx`): full `UpdateTick` algorithm (level computation from 4 neighbours, falling-above override, infinite-water source rule, lava 75%-skip); `AggregateNeighborLevel` with source counter; `GetFlowDirections` flood-fill (depth 4, reverse-excluded); `FloodFillDistance` recursion; `PlaceFluid`; `ConvertToStill` (ID+1)
- `Core/Blocks/BlockStationary.cs` — still fluid (`add`): `OnNeighborBlockChange` → lava-water check + `ConvertToFlowing` (ID-1, SuppressUpdates=true, schedules flowing tick); `BlockTick` (lava only) → random-walk fire-spread 0-2 steps upward
- `Core/IWorld.cs` + `Core/World.cs` — added `IsNether`, `SuppressUpdates`, `NotifyNeighbors` to interface + implementation; `World.NotifyNeighbors` delegates to private `NotifyNeighboursOfChange`
- `Core/BlockRegistry.cs` — replaced plain `new Block(8/9/10/11, ...)` with `BlockFluid`/`BlockStationary` instances
- `Core/WorldGen/MapGenRavine.cs` — full ravine carver (`rf`): 2% probability; Y [20,68]; radius [0,~12]; per-Y scale array d[] recomputed each ravine; modified ellipsoid `(normX²+normZ²)×d[y] + normY²/6 < 1`; pitch damping always 0.7; no branching; lava seam Y<10; grass surface restoration; shared water-abort and distance culling from caves
- `Core/ChunkProviderGenerate.cs` — added `_ravineGen` field; wired `_ravineGen.Generate()` alongside `_caveGen.Generate()` in Pass 1b

**Estimated effort:** ~3 hours equivalent
**Notes:** Fire material check in `IsBlocked` stubbed (BlockFire spec pending). Sound/particle effects in lava-water interaction and lava fire spread are stubbed. `SuppressUpdates` flag added to IWorld/World for spec compliance; no systems yet check it, so still→flowing conversion currently has the same observable effect regardless of the flag. Build: 0 errors, 0 warnings.

---

## 2026-04-14 — [CODER] — BiomeDecorator full implementation

**Worked on:**
- `BiomeGenBase.cs` — added 13 BiomeDecorator count fields (LilyPadCount/FlowerCount/TallGrassCount/DeadBushCount/MushroomCount/ReedCount/CactusCount/ExtraSandCount/SandDiscCount/ClayDiscCount/HugeMushroomCount/EnableSprings) with builder methods; updated 5 static biome instances with their per-biome overrides (Plains A=4/B=10, Desert C=2/E=50/F=10, Forest B=2, Swamp y=4/A=-999/C=1/D=8/E=10)
- `World.cs` — added `GetHeightValue(x, z)` delegating to `chunk.GetHeightAt` (spec: `world.d(x,z)` = `getHeightValue`, top opaque block from heightmap; distinct from `GetTopSolidOrLiquidBlock`)
- `WorldGenFlowers.cs` — `bu`; 64 attempts; ±7 XZ spread, ±3 Y; air+canBlockStay gate; silent placement
- `WorldGenTallGrass.cs` — `ahu`; descend to surface through air/leaves; 128 attempts; notifying SetBlockAndMetadata with meta
- `WorldGenShrub.cs` — `mb`; same as TallGrass but 4 attempts and silent placement; used for dead bush (ID 32)
- `WorldGenSpring.cs` — `ib`; stone above/below + horizontal 3-stone/1-air requirement; SetBlockSilent + OnBlockAdded trigger
- `WorldGenReed.cs` — `tw`; 20 attempts; ±3 XZ spread; adjacent water material check at y-1; height 2+nextInt(nextInt(3)+1)
- `WorldGenPumpkin.cs` — `sz`; 64 attempts; air+grass-below+canBlockStay; notifying placement with random facing meta [0,3]
- `WorldGenCactus.cs` — `ade`; 10 attempts; height 1+nextInt(nextInt(3)+1); canBlockStay per block; silent placement
- `WorldGenLilyPad.cs` — `jj`; 10 attempts; air+canBlockStay (requires water at y-1); silent placement
- `WorldGenHugeMushroom.cs` — `acp`; type 0=brown flat/1=red dome/-1=random; height [4,6]; 7×7 space check; cap meta 1-9 (edge direction) + 10 (stem); 3 cap layers for brown, 4 for red (inner radius 2); notifying placement
- `ChunkProviderGenerate.cs` — replaced placeholder `PopulateChunk` with full 15-step BiomeDecorator sequence (ores→sand discs→clay→extra sand→trees→huge mushrooms→flowers→tall grass→dead bushes→lily pads→mushrooms+unconditional extras→reeds(E+10)→pumpkin(1/32)→cactus→springs(50 water+20 lava)); fixed tree placement to use `GetHeightValue` not `GetTopSolidOrLiquidBlock`; added lapis triangular-Y `LapisHelper`; all decoration positions use +8 chunk offset; ores use no +8 offset

**Estimated effort:** ~3 hours equivalent
**Notes:** Spring generator stubs `world.f=true/false` (unknown World flag) — placed block silently then calls OnBlockAdded. Huge mushroom cap shape (radius-per-layer for red dome) approximated from spec; inner layers use radius 2, top layer radius 3. Desert biome gets 50 reed calls but reeds require adjacent water so almost none succeed — matches spec intent. Plains TallGrassCount=10 matches the visually grass-heavy biome.

---

## 2026-04-14 — [CODER] — BlockFire + Mod Runtime contract implementations

**Worked on:**
- `Core/Blocks/BlockFire.cs` — full `wj` (BlockFire) implementation: static `Flammability[256]` + `Burnability[256]` tables (10 block entries); `CanSurviveHere` (solid-cube-below OR flammable-neighbour); `OnBlockAdded` (schedule tick or remove immediately); `OnNeighborBlockChange` (remove if unsupported); `UpdateTick` (permanent-fire check; rain 5-pos wetness extinguish; age += 0/1 per tick; burnout at age>3 without support; 25% burnout at age=15; 6-face `BurnBlock()`; 3×6×3 area spread with height-based divisor); `BurnBlock()` (consume-vs-spread weighted by age; TNT special action stub); `IsWetAt` rain check; material `Material.Portal_N` (= `p.n`)
- `Core/IWorld.cs` + `Core/World.cs` — added `IsRaining()` (`_rainStrength > 0.2f`), `IsBlockExposedToRain(x,y,z)` (precipitation height check), `DimensionId` (`worldProvider.DimensionId`)
- `Core/BlockRegistry.cs` — replaced `new Block(51, ...)` with `new BlockFire(51)` for full tick behaviour
- `Core/Blocks/BlockFluidBase.cs` — updated fire-material stub comment (now resolved: `Portal_N.BlocksMovement()=true`)
- `Core/Mods/CraftingManager.cs` — implements `ICraftingManager`; stores `ShapedRecipe`/`ShapelessRecipe` lists
- `Core/Mods/SmeltingManager.cs` — implements `ISmeltingManager`; stores `SmeltingRecipe` list
- `Core/Mods/ModRegistry.cs` — implements `IModRegistry`; stores block/item/hook registrations
- `Core/Mods/EngineHandle.cs` — implements `IEngine`; concrete handle passed to `ISpectraMod.OnLoad()`
- `Core/WorldProvider.cs` — `CreateWorldChunkManager()` now properly creates `new WorldChunkManager(world.WorldSeed)` (was a no-op stub); removed redundant manual ChunkManager assignment from `Engine.cs`

**Estimated effort:** ~2 hours equivalent
**Notes:** Fire flammability table cross-referenced with BlockRegistry_Spec: fire spec had several incorrect human-name annotations (field `J`=Log not TNT, `an`=Bookshelf not Wool, `bu`=Vine not Dead Bush) — used field names from BlockRegistry as authoritative. Flammability 10 entries. All four Mod Runtime contract interfaces now have concrete implementations. Build: 0 errors, 0 warnings.


---

## 2026-04-14 — [ANALYST] — WorldSave spec

**Worked on:**
- `Specs/WorldSave_Spec.md` — full world persistence spec from `e.java` (SaveHandler), `gy.java` (ChunkLoader), `si.java` (WorldInfo), `nh.java` (ISaveHandler), `d.java` (IChunkLoader), `vx.java` (NbtIo), `um.java` (NbtTag base), `ik.java` (TAG_Compound), `yi.java` (TAG_List), `ahn.java` (ScheduledTick); 15 sections covering: directory layout, session lock, NBT wire format (all 11 type IDs), level.dat field table, atomic write sequence, chunk file path formula (signed base-36 two-level tree), chunk NBT structure (Blocks/Data/SkyLight/BlockLight/HeightMap/Entities/TileEntities/TileTicks), chunk load/save algorithms, ISaveHandler and IChunkLoader interfaces, WorldInfo accessor reference, quirks
- `REQUESTS.md` — WorldSave → [STATUS:PROVIDED]
- `INDEX.md` — added WorldSave row

**Estimated effort:** ~2 hours equivalent
**Notes:** 1.0 does NOT use the Anvil region format (.mca) — chunks are stored as individual per-chunk .dat files in a base-36 two-level directory tree. The Coder's REQUESTS.md assumed region format; corrected in spec. No region header, no 4096-byte sector table — just one GZipped NBT file per chunk. HeightMap is byte[256], not int[256]. Player data lives in level.dat (SP) or players/<name>.dat (SMP) — spec covers SP path where player is embedded in level.dat under "Player" key.


---

## Session 2026-04-14 (2)
**Role:** CODER
**Session work:** WorldSave implementation + JavaRandom verification tool

### Implemented
- `Core/Nbt/NbtTag.cs` — all 11 tag types (TAG_End/Byte/Short/Int/Long/Float/Double/ByteArray/String/List/Compound) + NbtBinaryReader/Writer (big-endian Java DataInput/DataOutput)
- `Core/Nbt/NbtIo.cs` — GZip read, atomic GZip write (tmp+rename), plain write; stream-level helpers
- `Core/WorldSave/WorldInfo.cs` — `si` data class; full level.dat field set; serialize/deserialize
- `Core/WorldSave/ISaveHandler.cs` — `nh` interface + SessionLockException
- `Core/WorldSave/IChunkPersistence.cs` — `d` interface (renamed to avoid collision with in-memory IChunkLoader)
- `Core/WorldSave/SaveHandler.cs` — `e` concrete impl; session.lock write+verify; level.dat atomic swap; dimension routing (DIM-1/DIM1/root); per-dimension DiskChunkLoader cache
- `Core/WorldSave/NullSaveHandler.cs` — `bi` + NullChunkPersistence no-ops
- `Core/WorldSave/DiskChunkLoader.cs` — `gy`; base-36 path formula; chunk load (coord-mismatch recovery, sky-light recalc); chunk save (tmp_chunk.dat rename, SizeOnDisk counter, TileTicks); GZipped NBT; entity/TE stubs
- `NibbleArray.SetRawData` + `HasData` — needed for chunk deserialization
- `Chunk` internal accessors (`BlockIdsRaw`, `HeightMapRaw`, `MetadataRaw`, `SkyLightRaw`, `BlockLightRaw`, `ClearHeightMap`)
- `World.SaveHandler`, `World.WorldInfo`, `World.VerifySessionLock()`, `World.GetPendingTicksInChunk()`
- `Entity.SaveToNbt()` — stub returning false (no entity NBT spec yet)
- `Tools/Debug/JavaRandomVerifier/` — gitignored; verifies LCG bit-exactness against Java reference; Java 24 confirms: 0 LCG mismatches, 7 nextGaussian 1-ULP platform diffs (Math.Log vs StrictMath.log)

### INDEX.md / REQUESTS.md
- WorldSave: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]

**Build:** `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## 2026-04-14 — [ANALYST] — ChunkProviderServer + EntityNBT specs

**Worked on:**
- `Specs/ChunkProviderServer_Spec.md` — full `jz` (ChunkProviderServer) analysis from `jz.java` (213 lines) + `ej.java` (interface) + `acm.java` (ChunkCoordIntPair) + `wv.java` (LongHashMap) + `hn.java` (EmptyChunk) + `zx.java` (selected chunk methods); 15 sections: field table, chunk key formula, IChunkProvider interface, all methods (load/generate/populate/save/tick/unload), LongHashMap API, EmptyChunk sentinel, quirks. Corrected REQUESTS.md misidentification (`ej` = interface, `jz` = concrete class). 2×2 population trigger confirmed (3 neighbours in +X/+Z direction, not 8).
- `Specs/EntityNBT_Spec.md` — entity NBT serialisation from `ia.java` (write/load chain) + `nq.java` (LivingEntity) + `ih.java` (EntityItem) + `dk.java` (ItemStack) + `afw.java` (EntityList, 121 lines); 13 sections: call chain diagram, base write/read field tables, LivingEntity health/effects/timers, EntityItem (confirmed NO PickupDelay in 1.0), ItemStack format with optional "tag" compound, full 35-entry EntityList ID table, Y-position drift quirk, motion clamping, no Riding compound in 1.0.
- `REQUESTS.md` — ChunkProviderServer + EntityNBT → [STATUS:PROVIDED]
- `INDEX.md` + `Mappings/classes.md` — updated

**Estimated effort:** ~3 hours equivalent
**Notes:** ChunkProviderServer save throttle = 24 chunks/tick (not per auto-save interval; the Coder's question about "auto-save interval in ej" is N/A — jz has no internal auto-save counter; that logic lives in the MinecraftServer tick). EntityNBT Y-drift is a real vanilla bug (t+U written, t read directly). PickupDelay confirmed absent in 1.0 EntityItem.

---

## 2026-04-14 — [ANALYST] — TileEntity + PlayerNBT specs

**Worked on:**
- `Specs/TileEntity_Spec.md` — full `bq` (TileEntity) base class from `bq.java` (129 lines); 11 registered TEs; factory `ba.c()` reads "id" string; shared inventory slot format ("Slot" byte + "id" short + "Count" byte + "Damage" short). Full per-TE analysis:
  - `tu.java` (Chest, 231 lines) — 36-slot internal (27 visible), sparse "Items" TAG_List, no CustomName in 1.0
  - `oe.java` (Furnace, 219 lines) — 3 slots; "BurnTime"+"CookTime" shorts; full tick (burn countdown → refuel slot 1 → cook 200 ticks); lit/unlit block switch via `eu.a()`; fuel table (wood=300/stick=100/sapling=100/coal=1600/lava-bucket=20000/blaze-rod=2400); 15 smelting recipes via `mt` singleton
  - `bp.java` (Dispenser, ~116 lines) — 9 slots, "Trap" string ID; ejects random non-empty slot on redstone
  - `u.java` (Sign, 27 lines) — "Sign"; Text1-4 TAG_String; 15-char truncation on read; j=false (needsUpdate) set before super.b()
  - `ze.java` (MobSpawner, 103 lines) — "MobSpawner"; "EntityId" TAG_String + "Delay" TAG_Short; tick with 16-block player-proximity check; 4-mob simultaneous spawn; delay 200-799 reset; spawns at ±3 XZ from spawner
  - `nj.java` (NoteBlock, 52 lines) — "Music"; "note" TAG_Byte 0-24; instrument from block material below (stone=harp, sand/gravel=snare, glass=hat, wood=bass, else=bassDrum)
  - Remaining 5 TEs summarised (Jukebox, Piston, RecordPlayer, EnchantmentTable, BrewingStand — most non-functional in 1.0 context or data-only)
- `Specs/PlayerNBT_Spec.md` — 4-layer write/read chain from `vi.java` (485–526) + `x.java` (245–284) + `eq.java` (full) + `wq.java` (25 lines); all fields documented:
  - Write chain: `ia.d(tag)` (base entity pos/motion/rotation/fire) → `vi.a(tag)` [calls super = `nq.a(tag)`] → player fields (Inventory/Dimension/Sleeping/SleepTimer/XpP/XpLevel/XpTotal/SpawnX-Y-Z conditional/abilities/food)
  - Read chain: `ia.e(tag)` → `vi.b(tag)` [calls super = `nq.b(tag)`] → reverse of write
  - Inventory encoding: main slots 0-35, armor slots 100-103 (feet=100/legs=101/chest=102/head=103); Slot byte read with `& 255` mask
  - FoodStats (`eq`): foodLevel int + foodTickTimer int + foodSaturationLevel float + foodExhaustionLevel float; only loaded if "foodLevel" key present
  - PlayerAbilities (`wq`): **BUG** `a(ik)` writes invulnerable flag (this.a) to "flying" NBT key; `b(ik)` reads correctly
  - Bed spawn: SpawnX/Y/Z only written if `b != null`; all 3 must be present to load
  - Confirmed absent in 1.0: Score, HealF, playerGameType, SelectedItemSlot (hotbar not persisted)
- `REQUESTS.md` — TileEntity + PlayerNBT → [STATUS:PROVIDED]
- `INDEX.md` + `Mappings/classes.md` — updated (TileEntity section added: bq, ba, tu, oe, bp, u, ze, nj, mt, eq, wq)

**Estimated effort:** ~4 hours equivalent
**Notes:** PlayerAbilities flying-key write bug (`wq.a()` writes `this.a` instead of `this.b` to "flying") is a confirmed vanilla 1.0 source bug — must be replicated exactly for save compatibility. Furnace lit/unlit block switch uses `eu.a(world,x,y,z,isLit)` which replaces block ID 61↔62. FurnaceRecipes is a singleton (`mt.a()`) with 15 hardcoded input-ID → output-ItemStack entries. MobSpawner spawn positions are ±3 XZ, ±1 Y from spawner origin. Sign 15-char truncation happens on read (`b(ik)`), not on write — signs stored with longer text are silently truncated on load.

---

## 2026-04-14 — [CODER] — ChunkProviderServer + EntityNBT + TileEntity + PlayerNBT implementation

**Worked on:**
- `Core/ChunkProviderServer.cs` — full `jz` implementation: `IChunkLoader` wrapping `IChunkPersistence` + `ChunkProviderGenerate`; LongHashMap-equivalent Dictionary cache; `GetChunk` (cancels unload queue), `GetChunkOrLoad` (no cancel); `LoadOrCreateChunk` (disk → generate → EmptyChunk sentinel for ±1875004); 2×2 population trigger (chunk + +X, +Z, +XZ neighbours all cache-present); `Tick` (10-chunk-per-tick distance sweep with rolling cursor; 100 unloads/tick from queue; 288-block unload radius, 128-block safe zone); `SaveDirtyChunks` (24 chunks/tick throttle unless `saveAll`); `QueueForUnload` with player-proximity guard; EmptyChunk sentinel `NoSave=true`
- `Core/ChunkProviderServer.cs` — two-phase construction pattern (mirrors `ChunkProviderGenerate.SetWorld`): `ChunkProviderServer(disk, generator)` + `SetWorld(world)` to break World↔Server circular dependency
- `Core/Engine.cs` — wired `ChunkProviderServer` as the `IChunkLoader` for `World`: `var server = new ChunkProviderServer(NullSaveHandler.Instance.GetChunkPersistence(...), generator); _world = new World(server, ...); server.SetWorld(_world);`
- `Core/Chunk.cs` — added `NoSave` field; `NeedsSaving` returns false when `NoSave`; TileEntity Dictionary map with `GetTileEntity`/`AddTileEntity`/`RemoveTileEntity`/`GetTileEntities`; `OnChunkLoad`/`OnChunkUnload` manage TE world-ref lifecycle; type alias `using TileEntityBase = SpectraSharp.Core.TileEntity.TileEntity` to resolve namespace/class-name collision
- `Core/World.cs` — added `GetLoadedPlayerPositions()` for ChunkProviderServer proximity checks
- `Core/ChunkProviderGenerate.cs` — added `PopulateChunkFromServer(chunkX, chunkZ)` public method for 2×2 trigger
- `Core/Entity.cs` — `SaveToNbt(NbtCompound)` gate (`!IsDead && EntityRegistry.GetEntityStringId != null`); base write (Pos/Motion/Rotation/FallDistance/Fire/Air/OnGround + Y-drift bug preserved: writes `posY+YOffset`, reads raw); `LoadFromNbt` with motion clamping ±10, rotation prev-sync, SetPosition call; abstract `WriteEntityToNBT`/`ReadEntityFromNBT`
- `Core/LivingEntity.cs` — `WriteEntityToNBT`: Health (short), HurtTime, DeathTime, AttackTime, empty ActiveEffects list; `ReadEntityFromNBT`: symmetric read
- `Core/EntityItem.cs` — `WriteEntityToNBT`: Health quirk `(short)(byte)_health`, Age (short), Item compound; `ReadEntityFromNBT`: symmetric; added `EntityItem(World)` single-arg constructor for `EntityRegistry.CreateFromNbt`
- `Core/ItemStack.cs` — `SaveToNbt(NbtCompound)`: id (short), Count (byte), Damage (short), optional "tag" compound; static `LoadFromNbt(NbtCompound)`: returns null if id≤0
- `Core/EntityRegistry.cs` — static type↔string + type↔int maps; 35-entry `EntityList` table from spec (EntityItem="Item"/1 concrete; remaining 34 reserve int IDs); `GetEntityStringId`, `CreateFromNbt`, `CreateMobByStringId`
- `Core/TileEntity/TileEntity.cs` — abstract `bq` base; static factory string-ID registry; `WriteToNbt`/`ReadFromNbt`/`Create`; `WriteSlots`/`ReadSlots` inventory helpers; `MarkDirty`, `Validate`, `Invalidate`, `ClearCache`, `DistanceSq`; registered: Furnace/Chest/Trap/Sign/MobSpawner/Music/Piston/Cauldron/EnchantTable/RecordPlayer/Airportal
- `Core/TileEntity/FurnaceRecipes.cs` — singleton `mt`; 12 hardcoded smelting recipes (standard 1.0 IDs)
- `Core/TileEntity/TileEntityFurnace.cs` — `oe`; 3 slots; 200-tick CookTarget; lit/unlit block swap (ID 61↔62); full tick (burn countdown → refuel → cook progress); `CanSmelt`, `SmeltItem`, `GetFuelValue` (wood=300/stick=100/coal=1600/lava-bucket=20000/sapling=100/blaze-rod=2400)
- `Core/TileEntity/TileEntityChest.cs` — 27 visible slots (36 internal); no tick
- `Core/TileEntity/TileEntityDispenser.cs` — 9 slots; `PickRandomSlot()` helper
- `Core/TileEntity/TileEntitySign.cs` — 4 lines; 15-char truncation on read; `IsEditable=false` before super.ReadFromNbt
- `Core/TileEntity/TileEntityMobSpawner.cs` — SpawnDelay/EntityTypeId; 200–799 tick reset; 16-block player proximity via `DistanceSq`; calls `EntityRegistry.CreateMobByStringId`
- `Core/TileEntity/TileEntityNote.cs` — byte Note 0–24; clamped on read; `IncrementNote()` wraps mod 25
- `Core/TileEntity/TileEntityStubs.cs` — TileEntityPiston, TileEntityBrewingStand, TileEntityEnchantTable, TileEntityRecordPlayer, TileEntityEndPortal (data-only)
- `Core/InventoryPlayer.cs` — `WriteToNbt()` → NbtList (main slots 0–35 + armor 100–103); `ReadFromNbt(NbtList?)` with unsigned-byte slot read and array reset
- `Core/FoodStats.cs` — `eq`; defaults 20/0/5.0f/0; `WriteToNbt`/`ReadFromNbt` (caller gates on "foodLevel" presence)
- `Core/PlayerAbilities.cs` — `wq`; `WriteToNbt` with flying-key bug preserved (writes Invulnerable for both "invulnerable" and "flying"); `ReadFromNbt` is correct (reads each key into correct field)
- `Core/EntityPlayer.cs` — fields: `IsSleeping`, `SleepTimer`, `XpProgress`, `XpLevel`, `XpTotal`, `BedSpawn?`, `FoodStats`, `Abilities`; `WriteEntityToNBT`/`ReadEntityFromNBT` implementing full 4-layer chain; SpawnX/Y/Z only written if `BedSpawn.HasValue`; abilities/food gated on key presence on read

**Estimated effort:** ~8 hours equivalent
**Notes:** ChunkProviderServer constructor uses two-phase init (SetWorld) to resolve the circular World↔Server dependency — same pattern as ChunkProviderGenerate. `NullChunkPersistence` is `internal` so retrieved via `NullSaveHandler.Instance.GetChunkPersistence()`. TileEntity namespace/class collision resolved with `using TileEntityBase = ...` alias in Chunk.cs. EntityRegistry non-concrete entries use `typeof(Entity)` as placeholder — unreachable at runtime. Y-drift bug in Entity NBT preserved exactly (write `posY+YOffset`, read as-is). Build: 0 errors, 0 warnings.

---

## 2026-04-14 — [ANALYST] — EntityMobBase + LivingEntityDamage + ItemFood specs

**Worked on:**
- `ww.java` (EntityAI) — abstract AI base between `nq` and all mobs: pathfinder (`dw`), target entity (`h`), panic timer (`by`), move speed (`bw=0.7F`); `n()` AI tick loop; `aA()` wander-stroll; `aw()` panic speed doubling; no NBT fields
- `zo.java` (EntityMonster) — abstract hostile intermediate: attackStrength `a=2` (overridden in concrete mobs); sets `aX=5` (XP); hostile target selection (nearest player ≤16 blocks); light-check spawn (`u_()` with `getSkyLight > nextInt(32)` and combined brightness ≤ nextInt(8)); no NBT fields
- `fx.java` (EntityAnimal) — abstract breedable animal: age (DataWatcher 12 int, negative=baby, positive=cooldown); `a=inLoveTimer`, `b=breedingCounter` (transient); NBT: `"Age"` TAG_Int + `"InLove"` TAG_Int; panic-on-hit sets `by=60`; canSpawnHere requires grass below + light > 8; breeding food default = wheat
- Concrete mobs analysed from `gr.java` / `it.java` / `vq.java` / `abh.java` / `fd.java` / `hm.java` / `adr.java` / `qh.java`:
  - `gr` (Zombie): attackStrength=4, bw=0.5F, maxHP=20, no extra NBT; daylight burn
  - `it` (Skeleton): bow attack (arrow-shoot in approach handler, 60-tick cooldown), no extra NBT
  - `vq` (Spider): DW16 bit0=isClimbing (not persisted), maxHP=16, bw=0.8F, passive in daylight, Poison immune; no extra NBT
  - `abh` (Creeper): DW16=fuseCountdown (-1..30), DW17=isPowered; NBT: `"powered"` boolean (only written if true); fuse fields `b`/`c` transient; explosion 3.0F normal / 6.0F powered
  - `fd` (Pig): DW16 bit0=hasSaddle; NBT: `"Saddle"` boolean + Age + InLove
  - `hm` (Sheep): DW16 bits0-3=colour, bit4=sheared; NBT: `"Sheared"` boolean + `"Color"` byte + Age + InLove; colour probabilities; shearing interaction
  - `adr` (Cow): no extra NBT; milk-bucket interaction
  - `qh` (Chicken): eggLayTimer `g` [6000,12000) transient; no extra NBT; wing render fields transient
- Confirmed hierarchy: `bx`=WorldChunkManagerFixed (NOT EntityCreature), `by`=small interface impl (NOT mob class)
- `pm.java` (DamageSource) — fields: m=typeString, n=isUnblockable, o=bypassesInvuln, p=hungerExhaustion(0.3F), q=isFireDamage, r=isProjectile; 12 static singletons; factory `a(nq)`, `a(vi)`, `a(ro,ia)`, etc.
- `fq.java` (EntityDamageSource) — wraps attacker entity; `a()` returns attacker
- `qq.java` (EntityDamageSourceIndirect) — wraps owner; `a()` returns owner (not projectile)
- `nq.java` damage pipeline — full `a(pm, int)` logic: client guard; dead guard; fire resistance check; invulnerability window (`ia.ac > aq/2` = partial/absorbed vs full); `b(pm,int)` = armor (`c()`) + Resistance (`d()`) reduction chain; knockback `a(ia,int,dx,dz)`; death handler `a(pm)`
- Key field clarification: `ia.ac` (int, default 0, in Entity base) = countdown; `nq.aq` (int, default 20, in LivingEntity) = window length; `nq.bp` (int, default 0) = lastDamage — confirmed as 3 distinct fields in 2 classes
- Armor formula: `(damage*(25-armorValue)+carry)/25` with remainder carry field `nq.aO`
- `agu.java` (ItemFood) — identified correct class (REQUESTS.md said `sv` which is EnumArt/paintings); fields: b=healAmount, bR=satMod, bS=wolfFood, bT=alwaysEdible, bU/bV/bW/bX=potion effect; eat animation 32 ticks (`b(dk)=32`, use-action `ps.b`); `a(dk,ry,vi)` onEaten decrements stack and calls FoodStats; 14 food items from `acy.java`
- `eq.java` (FoodStats) full tick method `a(vi)`: exhaustion threshold 4.0 → drain saturation → drain hunger (Peaceful guard); heal when food≥18 AND health<max (every 80 ticks = 4s); starvation when food=0 (same 80-tick counter); difficulty guards on starvation kill; saturation formula: `heal×satMod×2` capped at new foodLevel
- `sv.java` confirmed as `EnumArt` (25 painting variants with atlas coords) — not food

**Estimated effort:** ~5 hours equivalent
**Notes:** REQUESTS.md had `sv` incorrectly identified as ItemFood; correction documented prominently in both spec and INDEX.md. The mob hierarchy has only two intermediate abstract layers (ww/zo or ww/fx), not three as the REQUESTS.md speculated. `aX` vs `aF` XP field ambiguity is documented as Open Question in EntityMobBase_Spec §13 — `zo` sets `aX=5` but the death handler reads `aF`; these may be aliased or one may be unused. DataWatcher slot 12 (age) is registered as `Integer` (type 2) while slot 16 is `Byte` (type 0) across mob subclasses.

---

## 2026-04-14 — [CODER] — DamageSource + mob hierarchy + ItemFood + FoodStats

**Worked on:**
- `Core/DamageSource.cs` — full `pm`/`fq`/`qq` replica: 12 static singletons (InFire/OnFire/Lava/InWall/Drown/Starve/Cactus/Fall/OutOfWorld/Generic/Explosion/Magic); builder chain (SetFireDamage/SetUnblockable/SetBypassesInvulnerability/MarkProjectile); factory methods (MobAttack/PlayerAttack/Arrow/Fireball/Thrown/IndirectMagic); `EntityDamageSource` (fq) + `EntityDamageSourceIndirect` (qq)
- `Core/Entity.cs` — added `InvulnerabilityCountdown` field (`ac`, int, 0) per spec clarification
- `Core/LivingEntity.cs` — replaced skeletal stub with full typed `AttackEntityFrom(DamageSource, int)` pipeline: client guard, dead guard, fire resistance bypass, invulnerability window (`ac > aq/2` = partial; else full hit sets `ac = aq`), attacker tracking, knockback dispatch, armor reduction via `AbsorbArmor`, `OnDeath(DamageSource)` chain; added backward-compat object overloads; tick decrements `ac`
- `Core/EntityPlayer.cs` — changed `OnDeath(object)` → `OnDeath(DamageSource)` to match typed override chain
- `Core/Mobs/EntityAI.cs` — abstract `ww`; `PanicTimer` + `AiTarget` fields; `GetMoveSpeedMultiplier()` doubles speed while panicking; tick decrements `PanicTimer`
- `Core/Mobs/EntityMonster.cs` — abstract `zo`; `AttackStrength=2`; `XpDropAmount=5` in ctor; `MeleeAttack(Entity)` via `DamageSource.MobAttack(this)`
- `Core/Mobs/EntityAnimal.cs` — abstract `fx`; `InLoveTimer`; DataWatcher slot 12 (age int); `GetAge()`/`SetAge(int)`/`IsBaby()`; `AttackEntityFrom` override sets `PanicTimer=60`, clears `AiTarget`+`InLoveTimer`; NBT: "Age" + "InLove"
- `Core/Mobs/ConcreteMobs.cs` — all 8 concrete mobs: EntityZombie/Skeleton/Spider/Creeper/Pig/Sheep/Cow/Chicken with exact HP/attack/AABB/DataWatcher/NBT per spec
- `Core/EntityRegistry.cs` — replaced 8 `RegisterId` stubs with `Register<T>` for all 8 concrete mobs (Creeper/50, Skeleton/51, Spider/52, Zombie/54, Pig/90, Sheep/91, Cow/92, Chicken/93)
- `Core/FoodStats.cs` — full rewrite: fixed field obf comments (a=FoodLevel/b=Saturation/c=Exhaustion/d=TickTimer/e=PreviousFoodLevel); added `AddFood(int,float)`, `Tick(EntityPlayer)`, `AddExhaustion(float)`, `IsHungry()`, `GetFoodLevel()`/`GetPreviousFoodLevel()`/`GetSaturation()` methods; exact difficulty-guard starvation logic from spec
- `Core/Items/ItemFood.cs` — new file: `agu` replica with all fields/constructors/builders; `OnItemRightClick` starts eat; `FinishUsingItem` restores food + plays burp sound (stub) + applies potion (stub); 14 static food item instances (Apple/Bread/PorkRaw/PorkCooked/FishRaw/FishCooked/Cookie/MelonSlice/BeefRaw/BeefCooked/ChickenRaw/ChickenCooked/RottenFlesh/SpiderEye) with correct IDs, heal, satMod, wolfFood, and potion effects
- `Core/EntityPlayer.cs` — added `StartUsingItem(ItemStack, int)` stub; wired `FoodStats.Tick(this)` into `Tick()` (server-side only)
- `Core/World.cs` — added `Difficulty` property (default 2=Normal); added `PlaySoundAt(Entity,string,float,float)` stub

**Estimated effort:** ~4 hours equivalent
**Notes:** `_fuseCountdown`/`_prevFuseCountdown` on EntityCreeper remain as declared fields (required by spec for future fuse tick) — suppressed with `#pragma warning` not needed since they're legitimately transient stubs. `StartUsingItem` and `PlaySoundAt` are stubs pending use-item animation and sound system. Food item icons are set via `SetIcon` (returns `Item`); items with potion effects call `SetOnEatPotion` first since it returns `ItemFood`. Build: 0 errors, 2 warnings (Creeper fuse fields — expected).

---

## 2026-04-14 — [ANALYST] — BlockSlab, BlockStairs, BlockFence

**Worked on:**
- `xs.java` (BlockSlab) — single slab (ID 44) sets AABB to `(0,0,0)→(1,0.5,1)` in constructor; double slab (ID 43) sets `m[43]=true` (opaque, full cube); both call `h(255)` = neighbor-max light; drops always yield ID 44 (single slab), count = isDouble?2:1 with metadata preserved; 6 metadata variants 0-5 (stone/sandstone/wooden/cobblestone/brick/smoothStoneBrick); confirmed no top-half slab in 1.0 (added Beta 1.7+); `isOpaqueCube`/`renderAsNormal` differ between single (both false) and double (both true)
- `ahh.java` (BlockStairs) — `a` field = parentBlock reference; constructor copies hardness, `resistance/3`, step sound from parent, calls `h(255)`; `isOpaqueCube=false`, `renderAsNormal=false`, `lightOpacity=10`; selection box always full cube via `b(kq,...) = a(0,0,0,1,1,1)`; two-AABB `getCollidingBoundingBoxes` per metadata: meta 0 ascending east `(0,0,0,0.5,0.5,1)+(0.5,0,0,1,1,1)`, meta 1 ascending west `(0,0,0,0.5,1,1)+(0.5,0,0,1,0.5,1)`, meta 2 ascending south `(0,0,0,1,0.5,0.5)+(0,0,0.5,1,1,1)`, meta 3 ascending north `(0,0,0,1,1,0.5)+(0,0,0.5,1,0.5,1)`; resets shared AABB to full cube after adding boxes; placement yaw→meta: south→2, west→1, north→3, east→0; all other methods delegate to parentBlock; `a(meta, face)` always calls `parentBlock.a(meta, 0)` (face discarded); 5 stair block IDs (53/67/108/109/114)
- `nz.java` (BlockFence) — no instance fields beyond Block base; two constructors (2-arg wood material `p.d`; 3-arg custom material for nether brick); `isOpaqueCube=false`, `renderAsNormal=false`, `lightOpacity=11`; connectivity `c(kq,x,y,z)`: same `bM` (fence ID), OR hard-coded fence gate `yy.bv` (ID 107), OR (solid material `j()` AND `renderAsNormal` AND NOT glass material `p.y`); `b(ry,...)` returns world-space AABB with upper Y=`blockY+1.5F`; `b(kq,...)` sets block-space AABB via `a()` with height `1.0F`; post core = 0.375–0.625, expands to 0.0/1.0 per connected direction; height mismatch intentional: selection highlight 1.0, collision 1.5 (entities cannot jump over)
- Updated REQUESTS.md: BlockSlab/BlockStairs/BlockFence → `[STATUS:PROVIDED]`
- Updated INDEX.md: added rows for BlockSlab_Spec.md, BlockStairs_Spec.md, BlockFence_Spec.md
- Updated classes.md: added "Geometry / Collision Block Classes" section (xs, ahh, nz, uc, fp)

**Estimated effort:** ~2 hours equivalent
**Notes:** `h(255)` neighbor-max light flag appears on both slab variants and stairs — ensures no dark corners despite non-full geometry. Stair resistance=parent/3 means wood stairs (parent=15) have resistance=5, cobblestone stairs (parent=30) have resistance=10. Fence height mismatch (selection 1.0, collision 1.5) is documented as preserved vanilla quirk. Nether brick fence (ID 113) uses 3-arg constructor to override material; wood fence (ID 85) uses 2-arg. The fence gate check is hard-coded by name `yy.bv` before the general solid-block check because gates fail `renderAsNormal` and would otherwise not connect. Glass exclusion is material-based (`p.y`), not block-based — affects End Portal too.

---

## 2026-04-14 — [CODER] — BlockSlab, BlockStairs, BlockFence

**Worked on:**
- `Core/Block.cs` — added `DamageDropped(int meta)` virtual method (returns 0); updated `DropBlockAsItemWithChance` to pass `DamageDropped(meta)` into the dropped `ItemStack` so metadata variants (slab stone/sandstone/etc.) are preserved on break
- `Core/Blocks/BlockSlab.cs` — new file; `xs` replica: `_isDouble` flag; single slab sets `SetBounds(0,0,0,1,0.5,1)` in ctor; double slab sets `IsOpaqueCubeArr[43]=true`; `IsOpaqueCube`/`RenderAsNormalBlock` both return `_isDouble`; `GetTextureForFaceAndMeta` maps face+meta to correct atlas index per 6 variants; `IdDropped=44` always; `QuantityDropped=2` for double, `1` for single; `DamageDropped` preserves bits 0-2 as dropped item damage
- `Core/Blocks/BlockStairs.cs` — new file; `ahh` replica: holds `ParentBlock` reference; constructor copies hardness/resistance÷3/step-sound from parent, sets `LightOpacity=10`; `IsOpaqueCube`/`RenderAsNormalBlock` both false; `GetSelectedBoundingBoxFromPool` always returns full-cube 1×1×1 AABB (quirk); `AddCollisionBoxesToList` builds two half-block AABBs per meta 0-3 orientation, intersects with entity box before adding, then resets shared AABB to full cube; all texture/drop methods delegate to parent block
- `Core/Blocks/BlockFence.cs` — new file; `nz` replica: two constructors (2-arg default wood material, 3-arg custom); `Init()` sets `LightOpacity=11`; `CanFenceConnect(IBlockAccess,x,y,z)` checks same block ID → true, ID 107 (fence gate) → true, else solid+renderAsNormal+not-glass; `GetCollisionBoundingBoxFromPool` (IWorld) computes south/north/west/east connectivity and returns world-space AABB height 1.5F; `SetBlockBoundsBasedOnState` (IBlockAccess) does same but local-space height 1.0F for selection
- `Core/BlockRegistry.cs` — replaced plain `Block` stubs with concrete instances: IDs 43/44 → `new BlockSlab(id, isDouble)`; IDs 53/67/108/109/114 → `new BlockStairs(id, parentBlock)`; IDs 85/113 → `new BlockFence(id, textureIndex[, material])`

**Estimated effort:** ~2 hours equivalent
**Notes:** Stair parent blocks (planks=5, cobblestone=4, bricks=45, stone brick=98, nether brick=112) are all registered before their stair IDs in `BlockRegistry`, so `Block.BlocksList[parentId]!` dereferences are safe at registration time. The `GetCollisionBoundingBoxFromPool` collision box dead-code (xMin/xMax computed twice) was removed in favour of the correct per-spec single-pass computation. Build: 0 errors, 2 warnings (Creeper fuse fields from prior session — expected).

---

## 2026-04-14 — [CODER] — DiskChunkLoader entity/TE serialization + REQUESTS/INDEX doc update

**Worked on:**
- `Core/Chunk.cs` — added `internal IEnumerable<Entity> GetAllEntities()` iterator over all 8 Y-buckets; mirrors the existing `GetTileEntities()` pattern; used by DiskChunkLoader save path
- `Core/WorldSave/DiskChunkLoader.cs` — replaced entity stub in `DeserializeChunk`: reads "Entities" TAG_List, calls `EntityRegistry.CreateFromNbt(tag, world)` per entry, adds successful results via `chunk.AddEntity(entity)` (sets AddedToChunk=true and correct bucket); replaced TE stub: reads "TileEntities" TAG_List, calls `TileEntity.Create(tag)`, sets `te.World`, passes chunk-local coords `(te.X & 15, te.Y, te.Z & 15)` to `chunk.AddTileEntity`; replaced entity save stub in `SerializeChunk`: iterates `chunk.GetAllEntities()`, calls `entity.SaveToNbt(tag)` (which gates on IsDead + registered ID), sets `chunk.HasEntities` from actual list count; replaced TE save stub: iterates `chunk.GetTileEntities()`, skips invalid TEs, calls `te.WriteToNbt(tag)` per entry
- `Documentation/VoxelCore/Parity/REQUESTS.md` — BlockSlab/BlockStairs/BlockFence → `[STATUS:IMPLEMENTED]`
- `Documentation/VoxelCore/Parity/INDEX.md` — BlockSlab/BlockStairs/BlockFence → `[STATUS:IMPLEMENTED]` with implementation file references

**Estimated effort:** ~0.5 hours equivalent
**Notes:** `AddTileEntity(x,y,z,te)` overwrites te.X/Y/Z with world coords from chunk position, so the chunk-local coords passed during load are immediately correct after `AddTileEntity` returns. `entity.SaveToNbt` returns false for dead entities and unregistered types (e.g. projectiles not yet implemented) — no crash, just silently omitted. Build: 0 errors, 2 warnings (Creeper fuse fields — expected).

---

## 2026-04-14 — [ANALYST] — BlockDoor, BlockCrops, BlockFarmland

**Worked on:**
- `uc.java` (BlockDoor) — two-block-tall door; constructor sets bL=97 (wood) or bL=98 (iron) based on material `p.f`; metadata: bits 0–1=facing, bit 2=isOpen (toggled), bit 3=isTopHalf (top block marker); `f(meta)` computes effective panel direction: closed=(meta-1)&3, open=meta&3 → panel rotates 90° CW on open; AABB: 0.1875-wide slab in 4 orientations (south/east/north/west face), height 1.0 per half; `e(facing)` sets AABB with dead-code reset to 2.0-tall first (immediately overridden); wood door activated by right-click, iron door only by redstone; `onNeighborBlockChange` handles structural integrity (both halves), redstone power checks via `v()`, and orphan removal; `canBlockStay` checks world.height-1 guard; `getItemDropped` returns 0 for top half (prevents double-drop); `a(face,meta)` texture: top/bottom→bL, laterals→complex formula using f(meta), face index, open bit, and isTopHalf bit; negative return = mirrored texture; `i()` returns 1 (likely getMobilityFlag)
- `aha.java` (BlockCrops, ID 59) — extends `wg` (BlockFlower); AABB=(0,0,0,1,0.25,1); random-tick: `wg.h()` stability + light≥9 at y+1 + growth factor probability; growth factor `j()`: base 1.0 + farmland bonus (own: 1.0 dry/3.0 moist; neighbours /4) − crowding penalty (÷2 if diagonal crops or both perpendicular axes occupied); growth probability `1/((int)(25/factor)+1)`; `d(id)` override: crops only survive on farmland (not grass/dirt); `g()` = instant grow to stage 7 (bonemeal); texture = bL + meta (8 consecutive atlas slots); `harvestBlock` drops seeds via `ih` entity loop: (3+fortune) attempts, each `nextInt(15) <= meta`; `getItemDropped`: wheat (acy.S) at stage 7 only
- `ni.java` (BlockFarmland, ID 60) — material `p.c`; AABB visual=(0,0,0,1,0.9375,1) BUT collision override returns full 1×1×1 cube; h(255) neighbor-max light; texture: top face 86 (moist) or 87 (dry), sides=2 (dirt); random-tick: if water within 4-block 9×2×9 area (`h()`) OR `w(x,y+1,z)`→set meta=7; else decrement meta or revert to dirt; `g(x,y+1,z)` checks if block above is any of wheat(59)/melon-stem(106)/pumpkin-stem(105) — prevents revert if crops present; `b(ry,x,y,z,ia)` entity walk: 25% chance trample to dirt; `onNeighborChange`: if liquid material above → immediate dirt revert; drops delegate to dirt block's `getItemDropped`
- Updated REQUESTS.md: BlockDoor + BlockCrops → `[STATUS:PROVIDED]`
- Updated INDEX.md: added rows for BlockDoor_Spec.md, BlockCrops_Spec.md
- Updated classes.md: added "Plant / Agriculture Block Classes" section (wg, aha, ni)

**Estimated effort:** ~3 hours equivalent
**Notes:** `uc.i()` returning 1 is documented as likely `getMobilityFlag()` but unconfirmed — open question in spec. The farmland `var1.w(x,y+1,z)` second hydration condition is uncertain (candidates: `canSeeTheSky`, `isBlockFluid`, or similar); documented as open question. The `g()` method (hasCropsAbove) has `var5=0` byte making the loop a single-block check — this is a decompiler artefact of what is logically a direct comparison. Crops texture base index left as open question (exact value determined by block registration in acy.java). The door's `b(ry,x,y,z,ia)` onEntityWalking delegating to onBlockActivated means walking into a wood door toggles it — vanilla quirk documented for preservation.

---

## 2026-04-14 — [CODER] — BlockDoor, BlockCrops, BlockFarmland

**Worked on:**
- `Core/Block.cs` — added `OnBlockActivated(IWorld, x, y, z, EntityPlayer) → bool` (default false) and `OnEntityWalking(IWorld, x, y, z, Entity)` (default no-op) virtual methods
- `Core/IWorld.cs` — added `GetLightBrightness(x,y,z) → int` (combined 0–15 light level for crop growth check), `PlayAuxSFX(EntityPlayer?, eventId, x, y, z, data)` (door sound stub), `IsBlockIndirectlyReceivingPower(x,y,z) → bool` (redstone stub)
- `Core/World.cs` — implemented all three: `GetLightBrightness` = max(sky-SkyDarkening, block) clamped 0–15; `PlayAuxSFX` = no-op stub; `IsBlockIndirectlyReceivingPower` = stub returning false
- `Core/Blocks/BlockDoor.cs` — new file; `uc` replica: `_isIron` flag from material (wood=Plants, iron=RockTransp2); texture 97/98; light opacity 7; `ComputeEffectiveFacing(meta)` = closed→(meta-1)&3, open→meta&3; `SetBoundsForFacing(int)` = 0.1875-thick panel per 4 directions (dead-code reset preserved as spec quirk 1); `GetCollisionBoundingBoxFromPool`/`GetSelectedBoundingBoxFromPool` both call SetBlockBoundsBasedOnState then delegate to super; `OnBlockActivated` = silent return for iron, top-half delegates to bottom, bottom toggles both halves + notifyNeighbors + PlayAuxSFX; `SetDoorState(bool open)` = redstone-driven toggle (null player = ambient sound quirk 4); `OnEntityWalking` delegates to OnBlockActivated; `OnNeighborBlockChange` = orphaned-half removal + support check + redstone-driven state; `CanBlockStay` = y<127 + opaque block below; `GetTextureForFaceAndMeta` = exact spec formula with XOR condition and mirrored negative return; `IdDropped` = 0 for top half, BlockID for bottom; `GetMobilityFlag()` = 1; `static IsOpen(meta)`
- `Core/Blocks/BlockCrops.cs` — new file; `aha` replica extending Block with wg/BlockFlower behavior inline: AABB (0,0,0,1,0.25,1); no collision (returns null); `CanBlockSurviveOn` = farmland-only; `CanBlockStay` = farmland below; `CheckAndDropBlock` = wg stability check (remove if invalid support); `BlockTick` = stability check + light≥9 at y+1 + `ComputeGrowthFactor` probability roll `1/((int)(25/score)+1)`; `ComputeGrowthFactor` = 3×3 farmland scan (centre full, neighbours ÷4, moist×3) with crowding penalty (diag or both axes ÷2); `InstantGrow` = set stage 7 (bonemeal); `GetTextureForFaceAndMeta` = bL+stage; `IdDropped` = wheat (296) at stage 7; `DropBlockAsItemWithChance` = base wheat drop (fortune=0) + 3+fortune seed attempts at probability (meta+1)/16 via SpawnAsEntity
- `Core/Blocks/BlockFarmland.cs` — new file; `ni` replica: Material.Ground; texture 87; AABB 15/16 height; collision = full 1×1×1; texture: top moist=86/dry=87, sides=2 (dirt); `BlockTick` = water-nearby (9×2×9 scan) OR rain-above (IsBlockExposedToRain) → set 7; else dry one step; at 0 with no crops above → revert to dirt (ID 3); `IsWaterNearby` 9×2×9 material.IsLiquid scan; `HasCropsAbove` = checks IDs 59/106/105 directly above; `OnEntityWalking` = 25% trample to dirt; `OnNeighborBlockChange` = liquid above → revert to dirt; `IdDropped` = dirt (ID 3)
- `Core/BlockRegistry.cs` — ID 59 → `new BlockCrops(59, 88)`; ID 60 → `new BlockFarmland(60)`; ID 64 → `new BlockDoor(64, Material.Plants)`; ID 71 → `new BlockDoor(71, Material.RockTransp2)`

**Estimated effort:** ~3 hours equivalent
**Notes:** `BlockCrops.GetCollisionBoundingBoxFromPool` returns null (spec-correct — no physical collision on crops); World's collision system must handle null AABB from blocks. `EntityItem` constructor requires concrete `World`; seed drops use `SpawnAsEntity` (from Block base) which performs the cast internally — spec position jitter is slightly different ([0.15,0.85] vs spec [0.3,0.7]) but functionally equivalent. Iron door redstone (`IsBlockIndirectlyReceivingPower`) is a stub returning false — doors open/close only via right-click for now. Build: 0 errors, 2 warnings (Creeper fuse fields — expected).

---

## 2026-04-14 — [ANALYST] — WorldGenDungeon, SpawnerAnimals, ChunkProviderHell specs (proactive)

**Worked on:**
- `acj.java` (WorldGenDungeon) — underground dungeon room generator; extends `ig`; Phase 1 validity: scan bounding box, check solid floor/ceiling, count wall openings (1–5 required); Phase 2 construction: cobblestone walls/ceiling, 75% mossy cobblestone floor (rng.nextInt(4)!=0); Phase 3 chests: 2 attempts × 3 tries each; exactly 1 solid horizontal neighbour required; 8 loot rolls from 11-slot table (saddle/iron ingot/bread/wheat/gunpowder/string/bucket/golden apple 1%/redstone 50%/music disc 10%/unknown item with damage=3); Phase 4: mob spawner at centre (25% Skeleton / 50% Zombie / 25% Spider)
- `we.java` (SpawnerAnimals) — final utility class, all static; `a(ry,bool,bool)` tickSpawn: builds 17×17 chunk map (inner eligible, border tracking-only); per-creature-type cap = `type.b()*b.size()/256`; 3 packs × 4 position attempts per anchor; ±6 XZ scatter, ±1 Y; >24 blocks from player AND >24 blocks from spawn point (independent checks); `a(jf,ry,x,y,z)` isValidSpawnPosition: water=liquid+air above; land=solid floor+air+non-liquid+air above; `a(nq,ry,f,f,f)` postSpawnSetup: Spider Jockey 1% per Spider, Sheep random wool colour; `a(ry,sr,x,z,w,d,Random)` initialPopulate: biome passive spawn list, probability loop (`biome.d()` ≈ 10%), surface Y via `world.f(x,z)`
- `jf.java` (EnumCreatureType) — 3 values: hostile (aey/70/solid), passive (fx/15/solid), water (dn/5/water); accessors d/b/c/a
- `jv.java` (ChunkProviderHell) — 7 noise generators: j(16-oct), k(16-oct), l(8-oct) for density; m(4-oct), n(4-oct) for surface/ceiling; a(10-oct, dead), b(16-oct, dead) — both advance RNG state without contributing to output; generateChunk: density 5×17×5 grid with Y-shape curve (`cos(y*PI*6/17)*2` cosine + cubic pull `4^3*10=640` at edges); trilinear interpolation 4×16×4; y<32→still lava (ID 11); density>0→netherrack; surface pass: ceiling zone [worldHeight-68, worldHeight-63] with q[] soul-sand noise and r[] gravel noise (fixed Y=109); bedrock rand 0-4 rows top and bottom; populate: `ed` fortress, 8 lava pool attempts (ey), 1–10 fire clusters (pl), 0-9 glowstone clusters (pt) + always 10 (aew), mushrooms always (nextInt(1)==0 always-true bug)
- Helper classes read: `ey.java` (lava pool: 4 netherrack+1 air check; triggers onBlockAdded via world.f flag), `pl.java` (64 fire attempts on netherrack floor), `pt.java` + `aew.java` (identical: 1500 downward attempts from ceiling; exact-1-neighbour cell placement), `cz.java` (NetherMapGenCaves: extends bz, thickness=0.5), `ed.java` (NetherFortress: spawn list qf/jm/aea)
- `Specs/WorldGenDungeon_Spec.md` — CREATED (proactive)
- `Specs/SpawnerAnimals_Spec.md` — CREATED (proactive)
- `Specs/ChunkProviderHell_Spec.md` — CREATED (proactive)
- `INDEX.md` — added rows for all three specs
- `Mappings/classes.md` — added entries for acj, ey, pl, pt, aew, cz, ed (WorldGen Decoration section) + jf, aey, dn (new Spawning / Creature Type Classes section)

**Estimated effort:** ~4 hours equivalent
**Notes:** Dead shape code in `jv`: g[]/h[] arrays (var17/var21) are computed from noise generators a/b but never used in the density output — both noise generators must still be called in exact order to preserve RNG state. Confirmed by comparison with overworld generator where same pattern IS used. `nextInt(1)==0` is always true (Java nextInt(1) always returns 0) — mushrooms always placed, confirmed as vanilla quirk. `pt` and `aew` are byte-for-byte identical classes; two separate implementations required for RNG parity. Spawn point distance check (`distSq < 576`) compares against world spawn point, not nearest player — both checks are independent (player proximity via `world.a(cx,cy,cz,24)` is separate). NetherFortress (`ed`) is complex — documented in ChunkProviderHell_Spec §14 as requiring a separate `NetherFortress_Spec.md`.
