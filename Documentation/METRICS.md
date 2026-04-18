# SpectraEngine — Development Metrics

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
| 2026-04-16 | Claude API            | 25 EUR |
| 2026-04-16 | Extra usage           | 20 EUR |



**Running total: 186 EUR**

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
- `SpectraEngine.ModRuntime.csproj` — new project, refs IKVM + HarmonyLib + Core
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
**Notes:** SpectraEngine.ModRuntime builds clean (0 errors, 0 warnings). JavaStubs v1_0 not yet buildable — depends on IKVM NuGet restore and java.util.Random being available from IKVM.Runtime. MixinInterceptor and HarmonyBridge are planned for next session. CODER must implement BlockRegistry.RegisterMod() and any new IWorld methods flagged with TODO in the stubs.

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
- `Core/ChunkProviderGenerate.cs` — added `using SpectraEngine.Core.WorldGen;`; added biome tree decoration loop in `PopulateChunk()` (spec §10.1): 10% bonus tree, random x/z offset, `GetTopSolidOrLiquidBlock` surface height

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
- `SpectraEngine.csproj` — excluded `Bridge/JavaStubs/**` and `SpectraEngine.ModRuntime/**` (both need IKVM); `Bridge/Mods/**` kept in after mod cleanup
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
- `Core/Chunk.cs` — added `NoSave` field; `NeedsSaving` returns false when `NoSave`; TileEntity Dictionary map with `GetTileEntity`/`AddTileEntity`/`RemoveTileEntity`/`GetTileEntities`; `OnChunkLoad`/`OnChunkUnload` manage TE world-ref lifecycle; type alias `using TileEntityBase = SpectraEngine.Core.TileEntity.TileEntity` to resolve namespace/class-name collision
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

---

## 2026-04-14 — [CODER] — WorldGenDungeon, SpawnerAnimals, ChunkProviderHell implementation

**Worked on:**
- `Core/TileEntity/TileEntity.cs` — added `CreateForBlock(int blockId)` static factory: dict keyed by block ID → lambda for Dispenser(23), MobSpawner(52), Chest(54), Furnace(61/62), Sign(63/68), Note(84)
- `Core/Chunk.cs` — `SetBlock` now calls `TileEntity.CreateForBlock` on ID change: removes stale TE, creates new TE if block requires one
- `Core/World.cs` — `GetTileEntity` stub fixed: delegates to `GetChunkFromBlockCoords(x,z).GetTileEntity(x&15, y, z&15)`; added `SpawnX/Y/Z` properties, `GetPlayerList()`, `CountEntitiesOfType(Type)`, `FindNearestPlayerWithinRange`, `GetSpawnableList(EnumCreatureType, x,y,z)`
- `Core/EnumCreatureType.cs` — new enum: Hostile, Passive, Water
- `Core/BiomeGenBase.cs` — added `SpawnListEntry` record, default hostile/passive spawn lists (Spider/Zombie/Skeleton/Creeper + Sheep/Pig/Chicken/Cow), `GetSpawnList(EnumCreatureType)` method
- `Core/Entity.cs` — added `GetCanSpawnHere() → bool` (default true) and `GetMaxSpawnedInChunk() → int` (default 4) virtual methods
- `Core/Mobs/EntityMonster.cs` — `GetCanSpawnHere()` override: light ≤ 7 + solid floor
- `Core/Mobs/EntityAnimal.cs` — `GetCanSpawnHere()` override: light ≥ 9 + solid floor
- `Core/Mobs/ConcreteMobs.cs` (EntitySheep) — added `SetFleeceColor(int)` and `GetRandomFleeceColor(JavaRandom)` with full weighted distribution (White=35 cumulative to Black=5)
- `Core/WorldGen/WorldGenDungeon.cs` — new file; `acj` replica: Phase 1 site validation (solid floor/ceiling + 1–5 wall openings), Phase 2 room carve (cobblestone/75% mossy-cobblestone floor top-down), Phase 3 chest placement (2 attempts × 3 tries, exactly 1 solid neighbour, 8 loot rolls from 11-slot table), Phase 4 mob spawner (25% Skeleton / 50% Zombie / 25% Spider); `acy.aV` item ID = 0/unresolved (documented TODO §9.1)
- `Core/SpawnerAnimals.cs` — new file; `we` replica: `TickSpawn` builds 17×17 chunk map per player (inner eligible, border tracking-only), cap = `baseCap * mapSize / 256`, 3 packs × 4 attempts per anchor, ±6 XZ scatter, player + spawn-point 24-block exclusion zones; `InitialPopulate` biome passive spawn loop (0.1f probability per group); Spider Jockey 1% via `skeleton.MountEntity(mob)`; Sheep colour via `EntitySheep.GetRandomFleeceColor`
- `Core/ChunkProviderHell.cs` — new file; `jv` replica: 7 noise generators (j/k/l density, m/n surface, a/b dead-state-only); `ComputeDensityGrid` 5×17×5 with Y-shape cosine curve + cubic edge pull + dead g/h RNG-advance; trilinear interpolation 4×16×4 cells; `FillSurface` lava sea (y<32), bedrock top/bottom, soul-sand/gravel/netherrack ceiling zone; `MapGenNetherCaves` (netherrack-only, thickness 0.5); `Populate` with `WorldGenNetherLavaPool`, `WorldGenNetherFire`, `WorldGenGlowStone1/2`, mushrooms (nextInt(1) always-true bug preserved)
- `Documentation/VoxelCore/Parity/REQUESTS.md` — WorldGenDungeon, SpawnerAnimals, ChunkProviderHell → `[STATUS:IMPLEMENTED]`
- `Documentation/VoxelCore/Parity/INDEX.md` — WorldGenDungeon, SpawnerAnimals, ChunkProviderHell → `[STATUS:IMPLEMENTED]` with file references

**Estimated effort:** ~4 hours equivalent
**Notes:** `_surfaceR[x + z]` index bug fixed to `_surfaceR[x * 16 + z]` (noise generated as 16×1×16, index formula = x*16+z). `buf ??= ... else` invalid C# syntax fixed to `if (buf == null || buf.Length < total) buf = new double[total]; else Array.Clear`. `Material.Air` reference equality in spawn checks replaced with `!IsLiquid()` to avoid false negatives from different material instances. `Water` creature type skipped in `TickSpawn` (no EntityWaterMob class yet). Build: 0 errors, 2 warnings (Creeper fuse fields — expected).

---

## 2026-04-14 — [ANALYST] — OpenQuestion_AcyAV + MobAI_PathFinder spec

**Worked on:**
- `acy.java` field lookup — `acy.aV = new xv(95)`: class `xv` = ItemDye; item ID = bM = 256+95 = 351; name "dyePowder"; dungeon loot slot 10 drops 1× Dye, damage=3 = Cocoa Beans. Also resolved `acy.bB`: `new pe(2000, "13")` → bM=2256 (record "13"); bM+1=2257 (record "cat"). `WorldGenDungeon_Spec.md §9` updated to "Resolved Questions". REQUESTS.md OpenQuestion_AcyAV → [STATUS:PROVIDED]
- `ww.java` (EntityAI) — full `n()` AI tick: panic decrement; `az()` isAngry; `o()` virtual target acquisition; path request `world.a(entity,target,16F)`; followpath (skip close waypoints via 2×width radius; yaw ±30° clamp; isAngry face-override; waypoint-above jump; in-water/lava 80% jump); stroll (1/180 or 1/120 probability; by>0 override; 10 candidates); `aA()` stroll random walk ±6 XZ ±3 Y best-of-10
- `zo.java` (EntityMonster) — `o()` = nearest player 16 blocks; `a(ia,dist)` melee: dist<2.0 + aT≤0 + AABB Y-overlap → set aT=20 + deal damage; `b(ia)` damage with Strength/Weakness potion modifier; `u_()` light spawn check (skyLight > nextInt(32) reject; combined ≤ nextInt(8)); stroll score = 0.5 - brightness (dark prefer)
- `fx.java` (EntityAnimal) — `o()` three-mode: inLove=same-species-inLove, age=0 player-with-food, age>0 baby; `a(ia,dist)` breeding counter (b==60 → offspring); `b(fx)` breed: offspring age=-24000, parents age=6000, 7 heart particles; `c(vi)` feeding → a=600; `a(x,y,z)` score: grass=10, else brightness-0.5; canSpawnHere: grass+light>8; panic clears love mode
- `rw.java` (PathFinder) — full A*: start=entity AABB minXYZ; sizeNode=ceil(width+1)×ceil(height+1)×ceil(width+1); 4-directional expand; climbOffset=1 if block above clear; step-down up to 4; bbox walkability scan (solid=0/water=-1/lava=-2/clear=1); closed-door check via `uc.g(meta)`; partial-path fallback to closest reached node; null only if closest==start
- `mo.java` (PathPoint) — fields a/b/c=xyz, j=hash, d=heapIndex, e=g, f=h, g=f-cost, h=parent, i=closed; hash formula; `a(mo)` = Euclidean dist
- `dw.java` (PathEntity) — mo[] container; waypoint Vec3 with width-centering; advance/exhausted API
- `zs.java` (PathHeap) — binary min-heap; initial 1024; sift-up/down; update; throws on double-add
- `ob.java` (PathNodeCache) — int-keyed hashmap; load 0.75; cap 16; get/put/clear
- `xk.java` (ChunkCache) — IBlockAccess; pre-fetches chunks in bbox; bounds-checked getBlockId/getMaterial
- REQUESTS.md: MobAI_PathFinder → [STATUS:PROVIDED]; obf corrections noted
- INDEX.md: added MobAI_PathFinder_Spec.md row
- classes.md: added "Pathfinding Classes" section (rw/mo/dw/zs/ob/xk)

**Estimated effort:** ~4 hours equivalent
**Notes:** Coder's obf guesses were wrong — `lb`=TexturePack GUI, `nb`=particle entity, `ij`=unknown. Real pathfinder chain found by tracing `dw` usage in `ww.java` → `world.a()` → `rw`. Pathfinding is 4-directional only (no diagonal). Partial-path return is important: prevents mobs from being completely idle when target is unreachable. 5 open questions remain: `world.b(entity,range)` spec needed; `zo.v()` unknown; `ww.i(ia)` check; `az()` overrides; `zo.c() b(1.0F)` burn identity.

---

## 2026-04-14 — [ANALYST] — Explosion spec

**Worked on:**
- `xp.java` (Explosion) — sphere ray-cast destructor: `f`=power, `a`=isIncendiary, `b`=exploder, `c/d/e`=XYZ, `g`=am HashSet; 16³ direction grid surface-only = 1352 rays; per-ray start strength = `f*(0.7+world.w.nextFloat()*0.6)`; per-step attenuation `(blastResistance+0.3)*step + step*0.75` (step=0.3F); `a(bool doParticles)`: block destruction at 30% drop chance; incendiary fire via local `Random h` (nextInt(3)==0, non-deterministic); entity damage: `f*=2` before entity section (doubled power quirk) → `damage = (int)((intensity²+intensity)/2 * 8 * f + 1)`; exposure fraction via `world.a(Vec3,AABB)` grid-sampling ray test
- `dd.java` (EntityTNTPrimed) — single field `a`=fuse (80 ticks); constructor: random horizontal velocity (Math.random), `w=0.2F` upward; tick: gravity −0.04, move, friction ×0.98/ground ×0.7/−0.5; at fuse=0 → `world.a(null,s,t,u,4.0F)`; NBT: "Fuse" TAG_Byte; chain-explode fuse = `nextInt(20)+10` (via `abm.i()`)
- `abm.java` (BlockTNT) — corrects Coder guess `vm`; `onBlockAdded`: powered → ignite; `onNeighborChange`: canDropFromExplosion + powered → ignite; `onDestroyedByExplosion`: spawn dd with reduced fuse; `harvestBlock`: (meta&1)==0 → drop item, else → spawn dd + fuse sound; `onPlayerDestroyed`: flint+steel → world.c(x,y,z,1)
- `abh.java` (EntityCreeper) — DW16=fuseCountdown delta, DW17=isPowered; approach at dist<3 (or 7 powered): b++ + DW16=1; b≥30 → explode power 3 (or 6); retreat: DW16=−1, b−−; defuse if no target and b>0; death from Skeleton → drop `acy.bB.bM + nextInt(2)` (music disc); lightning → DW17=1 (powered)
- `am.java` (BlockPos) — confirmed as simple int triple; hashCode=`a*8976890+b*981131+c`; used as explosion block-set key
- `ry.java` (World) — `world.a(entity,x,y,z,power)` factory: creates xp, calls `xp.a()` (collect blocks), `xp.a(true)` (destroy + particles), returns xp
- `Specs/Explosion_Spec.md` — CREATED
- `REQUESTS.md` — Explosion → [STATUS:PROVIDED]
- `INDEX.md` — added Explosion_Spec.md row
- `Mappings/classes.md` — added `am` (Utility Classes) + `xp` (World/Level Classes)

**Estimated effort:** ~3 hours equivalent
**Notes:** Entity damage doubled-power quirk (`f*=2` before damage formula) is intentional vanilla behaviour — entity damage at power 4 can reach 257, not 129. Incendiary fire uses local `Random h` (not world RNG `world.w`) — fire placement is non-deterministic relative to other random events, which affects RNG sequencing if world RNG calls are counted. `abm` corrects Coder's two wrong guesses (`abv`/`vm`). BlockTNT (ID 46) texture layout: sides=bL, top=bL+1, bottom=bL+2. 5 open questions remain: `ry.world.c(x,y,z,1)` identity; `xp.a()` block ordering; `vo` Vec3 pool; `ry.a(su,entity,AABB)` entity list method; `abh` Skeleton-kill criteria (`a(pm)` DamageSource type check).

---

## 2026-04-14 — [ANALYST] — ItemTool / ItemSword / ItemArmor spec

**Worked on:**
- `ads.java` (ItemTool) — base class for all tools; fields bR=effective blocks array, a=efficiency, bS=baseDamage+materialBonus, b=nu material; getStrVsBlock loop; hitEntity costs 2 durability; onBlockDestroyed costs 1; getDamage returns bS; isItemTool=true
- `nu.java` (EnumToolMaterial) — 5 constants: WOOD(0,59,2F,0,15)/STONE(1,131,4F,1,5)/IRON(2,250,6F,2,14)/DIAMOND(3,1561,8F,3,10)/GOLD(0,32,12F,0,22); fields f=harvestLevel/g=maxUses/h=efficiency/i=damageBonus/j=enchantability
- `zp.java` (ItemSword) — extends `acy` NOT `ads`; a=4+material.damageBonus; getStrVsBlock=15F cobweb/1.5F all else; hitEntity costs 1 durability; blocking action ps.d; 72000-tick block duration
- `adb.java` (ItemSpade) — extends ads; baseDamage=1; 10 effective blocks including snow/clay/mycelium/farmland; canHarvestBlock=snow_layer+snow_block ONLY
- `zu.java` (ItemPickaxe) — extends ads; baseDamage=2; 22 effective blocks; canHarvestBlock with full tier-gate logic: obsidian=diamond-only, oreDiamond/oreGold=iron+, oreIron/oreLapis=stone+, oreRedstone=iron+, rock/metal material=any
- `ago.java` (ItemAxe) — extends ads; baseDamage=3; 8 effective blocks; getStrVsBlock override: ANY wood-material block gets efficiency (not just bR list)
- `wr.java` (ItemHoe) — extends `acy` NOT `ads`; no weapon damage; tills grass (top+air above) or dirt regardless of face; costs 1 durability; converts to farmland (yy.aA=ID 60)
- `agi.java` (ItemArmor) — extends acy; a=armorType, b=protection from dj.b(slot), bT=material, maxDurability=bS[slot]*dj.f; static bS={11,16,15,13}
- `dj.java` (EnumArmorMaterial) — 5 constants: LEATHER(5,[1,3,2,1],15)/CHAIN(15,[2,5,4,1],12)/IRON(15,[2,6,5,2],9)/GOLD(7,[2,5,3,1],25)/DIAMOND(33,[3,8,6,3],10)
- `vi.java` partial — getMiningSpeed `a(yy)`: inventory.getStrVsBlock → Efficiency enchant bonus (+level²+1 if canHarvestBlock) → Haste ×(1+(lvl+1)×0.2) → Fatigue ×(1-(lvl+1)×0.2) → water ÷5 → airborne ÷5
- `dk.java` partial — damageItem `a(int, nq)`: Unbreaking check (world RNG); accumulate damage; on overflow: renderBrokenItem + stat + size-- + e=0
- `x.java` partial — getStrVsBlock: 1.0×stack.a(block) from held slot; canHarvestBlock: material.k()=true OR item.a(block)
- Item registry assignments from `acy.java`: full table of 25 tool IDs (swords/picks/axes/shovels/hoes) + 20 armor IDs with materials and texture coords
- `Specs/ItemTool_Spec.md` — CREATED
- `REQUESTS.md` — ItemTool → [STATUS:PROVIDED]
- `INDEX.md` — added ItemTool_Spec.md row
- `Mappings/classes.md` — added ads/nu/zp/adb/zu/ago/wr/agi/dj entries

**Estimated effort:** ~3 hours equivalent
**Notes:** All Coder obf guesses wrong (acq/acp/acr/ah). Correct chain found by tracing `acy.java` static item instantiations. Critical parity notes: (1) sword and hoe extend `acy` directly, NOT `ads` — no effective-blocks array, no ItemTool damage cost formula; (2) axe efficiency applies to wood material not just the bR list; (3) Unbreaking uses world RNG (entity.o.w.nextInt), not enchantment-local RNG — affects RNG sequence when tools take durability hits; (4) GOLD tools have highest efficiency (12F) but lowest durability (32 uses) — must preserve both. 5 open questions remain: acy.i() exact semantics; p.k() material flag identity; gold armor IDs; ml.b/c/g enchantment helpers; EntityPlayer attack integration method chain.

---

## 2026-04-14 — [ANALYST] — BlockSnow, BlockIce, inline snow/ice generation spec

**Worked on:**
- `aif.java` (BlockSnow, ID 78) — AABB height formula `(2*(1+layers))/16`; collision box only for layers≥3 (up to 0.5F); canBlockStay requires solid renderNormal below; harvest→1 snowball (`aah`); melt randomTick at blockLight>11; material `p.u`; corrects Coder guess `aak`
- `ahq.java` (BlockIce, ID 79) — `ca=0.98F` slipperiness (only non-default in registry); opacity=1; drops nothing; melt randomTick at blockLight>10 (not 11 — ice opacity=1 shifts threshold: `>11-yy.o[bM]`); melt→still water (ID 9); mined over air/liquid→flowing water; material `p.t`; corrects Coder guess `aaj`
- `xj.java` (ChunkProviderGenerate) — confirmed snow/ice NOT in BiomeDecorator; inline 16×16 pass at END of `xj.populateChunk` (lines 360-371), after all decoration; `world.p(x,y,z)` (canFreeze=ice) then `world.r(x,y,z)` (canSnow=snow layer)
- `ry.java` (World) — `p(x,y,z)` = canFreeze: calls `c(x,y,z,false)`; `r(x,y,z)` = canSnow: temp≤0.15F + blockLight<10 + air block + canBlockStay + no ice directly below; `c(x,y,z,bool)` = shared freeze test: temp≤0.15F + waterOrIce material below + (opt) water neighbor check; `l()` = isDaytime = `this.k < 4`
- Temperature source: `WorldChunkManager.getTemperatureAtHeight` (altitude-adjusted)
- `Specs/SnowIce_Spec.md` — CREATED
- `REQUESTS.md` — SnowIce_Generation → [STATUS:PROVIDED]
- `INDEX.md` — added SnowIce_Spec.md row
- `Mappings/classes.md` — updated `aif`/`ahq` entries with full descriptions

**Estimated effort:** ~2 hours equivalent
**Notes:** Both Coder obf guesses wrong (`aak`/`aaj`). Real classes found by reading `yy.java` static field declarations — `new aif(78,66)` and `new ahq(79,67)`. Snow/ice assumed to be in BiomeDecorator (REQUESTS.md open question) — confirmed via xj.java grep: it is an inline 16×16 loop after all decoration. Ice melt threshold REQUESTS.md said ">11" — correct is ">10" because `ahq` opacity=1 reduces threshold by 1. Guard `blockBelow != yy.aT.bM` (not ice) in `ry.r()` prevents snow spawning on ice.

---

## 2026-04-14 — [ANALYST] — BlockBed spec

**Worked on:**
- `aab.java` (BlockBed, 209 lines) — full read: static direction array `a={{0,1},{-1,0},{0,-1},{1,0}}`; metadata helpers (getDirection, isOccupied, isHeadOfBed); AABB 9/16 height; texture mapping (4 faces × head/foot); orphan-half removal on neighbor change; drops `kn`(99) from foot half only
- `vi.java` — `d(int,int,int)` trySleep (line 817): all 6 qy return paths; monster scan `zo.class` 8×5×8 radius; `world.l()` daytime check
- `vi.java` — `a(bool,bool,bool)` wakeUpPlayer (line 900): restore AABB 0.6×1.8; clear occupied flag; `aab.f()` findWakeupPosition 3×3 scan (solid floor+2 clear air); optional spawn-point set
- `k.java` (WorldProvider) — field `c` (boolean, default false)=sleeping-disabled; field `d` (boolean)=unknown; `d()` method=hasSky (default true; Nether=false); clarifies Coder confusion between `c` field and `d()` method
- `qy.java` — 6-value enum: a=OK, b=NOT_POSSIBLE_HERE, c=NOT_POSSIBLE_NOW, d=TOO_FAR_AWAY, e=OTHER_PROBLEM, f=NOT_SAFE
- `kn.java` — ItemBed: itemId=99 (=acy.aZ), bM=355; `onItemUse` places foot then head block
- `Specs/BlockBed_Spec.md` — CREATED (20 sections)
- `REQUESTS.md` — BlockBed → [STATUS:PROVIDED]
- `INDEX.md` — added BlockBed_Spec.md row
- `Mappings/classes.md` — enhanced `aab` entry; added `dh` (BlockTriple), `qy` (EnumSleepStatus), `kn` (ItemBed)

**Estimated effort:** ~2 hours equivalent
**Notes:** WorldProvider.c is a boolean FIELD (sleeping-disabled), NOT hasSky — Coder REQUESTS.md conflated these. WorldProvider.d() is the hasSky METHOD (defaults true, Nether overrides false). world.l() isDaytime = skyDarkeningValue < 4 (not time-of-day). Nether explosion triggered by `world.y.c==true` (WorldProvider.c field set by WorldProviderHell), not by `!hasSky()`. 2 open questions: exact qy enum constant names (only ordinal values confirmed); `vi.d()` distance formula (3 XZ + 2 Y vs Manhattan).

---

## Session 2026-04-14 (3)
**Role:** CODER
**Session work:** BlockSnow + BlockIce + ItemTool/Sword/Hoe/Armor batch

### Implemented
- `Core/Blocks/BlockSnow.cs` — `aif` (ID 78): layer AABB, canBlockStay/onNeighborChange, melt at blockLight>11, harvest drops 1 snowball via EntityItem spawn
- `Core/Blocks/BlockIce.cs` — `ahq` (ID 79): slipperiness 0.98F, drops nothing, mined over air/liquid→flowing water, melt at blockLight>10
- `Core/Block.cs` — `GetCollisionBoundingBoxFromPool` return type changed to `AxisAlignedBB?` (allows null for no-collision blocks)
- `Core/Blocks/BlockDoor.cs` — matching nullable fix
- `Core/BlockRegistry.cs` — IDs 78/79 replaced with `new BlockSnow(78)` / `new BlockIce(79)`
- `Core/IWorld.cs` + `Core/World.cs` — `CanFreezeAtLocation` + `CanSnowAtLocation` (temperature ≤ 0.15F + blockLight < 10 gates)
- `Core/ChunkProviderGenerate.cs` — inline 16×16 snow/ice pass at end of `PopulateChunk`
- `Core/Item.cs` — added virtual `OnBlockDestroyed` + `IsItemTool` methods
- `Core/Items/EnumToolMaterial.cs` — `nu`: WOOD/STONE/IRON/DIAMOND/GOLD with 5 fields each
- `Core/Items/EnumArmorMaterial.cs` — `dj`: LEATHER/CHAIN/IRON/GOLD/DIAMOND with durFactor + protection[4] + enchantability
- `Core/Items/ItemTool.cs` — `ads` base + `ItemShovel` (`adb`) + `ItemPickaxe` (`zu`) + `ItemAxe` (`ago`)
- `Core/Items/ItemSword.cs` — `zp`: cobweb 15×, all else 1.5×, costs 1/2 durability (entity/block)
- `Core/Items/ItemHoe.cs` — `wr`: tills grass/dirt to farmland (ID 60), server-side only
- `Core/Items/ItemArmor.cs` — `agi`: 4 slots, durability = SlotBase[slot] × DurabilityFactor
- `Core/Items/ItemRegistry.cs` — 25 tool + 5 hoe + 20 armor items registered; `ItemRegistry.Initialize()` hooked into `Engine.cs`

### INDEX.md / REQUESTS.md
- SnowIce_Generation: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]
- ItemTool: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]

**Build:** `Build succeeded. 2 Warning(s). 0 Error(s).` (2 pre-existing Creeper unused fields, cleared by Explosion impl)

---

## Session 2026-04-15 (1)
**Role:** CODER
**Session work:** Explosion pipeline — Explosion.cs, EntityTNTPrimed, BlockTNT, EntityCreeper fuse

### Implemented
- `Core/Block.cs` — added `GetExplosionResistance(Entity?)` virtual (`BlockResistance / 5.0f`) and `OnBlockDestroyedByExplosion(IWorld,x,y,z)` virtual no-op
- `Core/World.cs` — added `GetEntitiesWithinAABBExcluding(Entity?, AxisAlignedBB)`, `RayTraceBlocks(Vec3,Vec3)` (DDA traversal, returns null on clear path), `GetExplosionExposure(Vec3, AxisAlignedBB)` (exposure fraction §5); replaced `CreateExplosion` stub with real `Explosion` instantiation
- `Core/Explosion.cs` — new: full `xp` replica; phase 1 = 1352 surface rays from 16³ grid (per-ray strength `P*(0.7+rand*0.6)`, per-step attenuation `(blastRes+0.3)*0.3` + fixed `0.225`); phase 2 = block destruction (reverse-list, 30% drop chance), particles (world RNG consumed), incendiary fire (local `Random` — quirk 3)
- `Core/EntityTNTPrimed.cs` — new: `dd` replica; spawn ctor with `Math.random()` horizontal velocity; 80-tick countdown; gravity+friction tick matching EntityItem pattern; `dd.g()` explode at power=4.0 source=null; NBT Fuse as byte
- `Core/EntityRegistry.cs` — ID 20 `"PrimedTnt"` promoted from `RegisterId` stub → `Register<EntityTNTPrimed>`
- `Core/Blocks/BlockTNT.cs` — new: `abm` replica; face textures (bL+2/bL+1/bL); `OnBlockAdded`/`OnNeighborBlockChange` redstone stub (`IsBlockPowered` returns false — BlockRedstone_Spec pending); `Ignite(world,x,y,z,meta)` drops item or spawns EntityTNTPrimed; `OnBlockDestroyedByExplosion` chain-spawns with fuse `nextInt(20)+10`
- `Core/Mobs/ConcreteMobs.cs` EntityCreeper — rewrote fuse logic: `_fuseCountdown` / `_prevFuseCountdown` now active; `Tick()` override (client DW16 delta, server auto-defuse on lost target); `OnTargetInRange` fuse++ → explode at 30; `OnTargetOutOfRange` defuse; `OnDeath` music disc (ID 2256/2257) on `EntitySkeleton` kill; `GetFuseInterpolated(float)` render helper; `OnStruckByLightning()` sets DW17
- `Core/LivingEntity.cs` — added `SpawnDropItem(int,int)` helper (spawns EntityItem at entity position)
- `Core/Mobs/EntityMonster.cs` — added virtual `OnTargetInRange(Entity,float)` and `OnTargetOutOfRange(Entity,float)` callbacks (default: melee if AttackStrength>0 / no-op)
- `Core/BlockRegistry.cs` — ID 46 stub `new Block(46,8,Material.RockTransp)` replaced with `new BlockTNT(46)`
- `Core/Blocks/BlockBed.cs` — fixed: `OnBlockActivated` now correctly marked `override` (was missing keyword, caused CS0114 warning)

### INDEX.md
- Explosion_Spec.md: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]

**Build:** `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Session 2026-04-15 (2)
**Role:** CODER
**Session work:** MobAI + PathFinder pipeline — PathPoint/PathHeap/PathEntity/ChunkCache/PathFinder + full EntityAI/EntityMonster/EntityAnimal AI tick

### Implemented
- `Core/AI/PathPoint.cs` — `mo` replica; block coordinates; HeapIndex (-1=not in heap); g/h/f costs; Parent link; Closed flag; packed HashKey with sign-bit formula; `EuclideanDistanceTo`, `IsInHeap` helpers
- `Core/AI/PathHeap.cs` — `zs` replica; binary min-heap sorted by TotalCost (f-cost); capacity 1024 doubles on overflow; Add/Poll/Update/Clear/IsEmpty; HeapIndex maintained on every sift
- `Core/AI/PathEntity.cs` — `dw` replica; ordered PathPoint[] from start→target; GetCurrentWaypoint (halfWidth offset), Advance, IsDone
- `Core/AI/ChunkCache.cs` — `xk` replica; 2-D Chunk?[][] snapshot pre-fetched at construction; GetBlockId (y-bounds check, chunk-offset lookup), GetMaterial, GetBlockMetadata
- `Core/AI/PathFinder.cs` — `rw` replica; A* main loop (PathHeap open set + Dictionary node cache); FindPath(entity,entity) + FindPath(entity,coords); ExpandNeighbors (4-directional); TryNeighbor (step-up/down ≤4, lava-reject); CheckWalkability (entity AABB scan — solid=0, water=-1, lava=-2, clear=1, door meta check); ReconstructPath; GetOrCreate deduplication
- `Core/World.cs` — added `GetPathToEntity(Entity,Entity,float)`, `GetPathToCoords(Entity,int,int,int,float)` (ChunkCache + PathFinder wrappers); `GetClosestPlayer(Entity,double)` / `GetClosestVulnerablePlayer`; `GetEntitiesWithinAABB<T>(AxisAlignedBB)` generic overload
- `Core/Mobs/EntityAI.cs` — fully rewritten: `RunAITick()` implements spec §8 `n()` in full (panic timer, isAngry, target management, path following with waypoint-skip + yaw steering + isAngry strafe, stroll trigger); `Stroll()` = `aA()` (10 candidates ±6/±3); `LookAt(Entity,float,float)` helper; `Tick()` calls `RunAITick()` server-side
- `Core/Mobs/EntityMonster.cs` — fully rewritten: `GetAITarget()` = nearest player within 16; `IsInRange()` dist<2 + Y-overlap; `OnTargetInRange()` = melee attack with 20-tick cooldown; `AttackEntityFrom` retargets to attacker (excluding self/mount/rider); `GetPositionScore()` = 0.5−brightness (prefer dark)
- `Core/Mobs/EntityAnimal.cs` — fully rewritten: `GetAITarget()` 3-mode (inLove→partner, adult→food-player, cooldown→own-species baby); `IsInRange()` / `OnTargetInRange()` breed counter + breedWith at 60; `Breed(partner)` spawn offspring at −24000 age; `Interact(EntityPlayer)` feeding/love mode; `Tick()` age increment/decrement; `AttackEntityFrom` panic
- `Core/Mobs/ConcreteMobs.cs` — added `CreateOffspring(EntityAnimal)` to EntityPig / EntitySheep / EntityCow / EntityChicken (returns new instance of own type)

### INDEX.md / REQUESTS.md
- MobAI_PathFinder_Spec.md: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]
- Removed stale [STATUS:REQUIRED] duplicate row

**Build:** `Build succeeded. 0 Warning(s). 0 Error(s).`


---

## Session 2026-04-15 (3)
**Role:** ANALYST
**Session work:** BlockBed tracking completion + BlockRedstone full spec

### Specs produced
- `Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md` (13 sections, ~550 lines)
  - §1 Face/Direction Encoding global table (lz arrays: e[]={2,3,0,1} opposite, a[]/b[] Z/X deltas)
  - §2 World Power Query API (k/u/l/v chain: getStrongPower → isStronglyPowered → getPower → isBlockReceivingPower)
  - §3 BlockRedstoneWire (kw, ID 55): DFS propagation with anti-reentrance flag `a`, dirty HashSet `cb`, f()/h() helpers, 0-crossing neighbour notification
  - §4 BlockTorch base (bg): meta 1-5 encoding, 4 wall + floor canBlockStay, AABB dims
  - §5 BlockRedstoneTorch (ku, IDs 75/76): STATIC burnout list `cb` shared class-level (vanilla cross-contamination bug), ≥8 flips/100 ticks → burnt out, randomTick on↔off, always drops ID 76
  - §6 BlockRedstoneDiode/Repeater (mz, IDs 93/94): meta bits 0-1=facing, 2-3=delay; cb={1,2,3,4}×2={2,4,6,8} ticks; f() input check; right-click cycles delay
  - §7 BlockLever (aaa, ID 69): meta bits 0-2=facing(1-6), bit 3=isOn; floor metas 5/6 random; strong power only on attached face
  - §8 BlockPressurePlate (wx, IDs 70/72): xb enum (a=all/b=mobs/c=players unused); tick 20; strong power upward only
  - §9 BlockButton (ahv, ID 77): wall-only in 1.0; meta 0-2=facing, 3=isPressed; tick 20 auto-release; dead meta 5 in c() documented; **ID 143 ABSENT in 1.0** (added Beta 1.7+)
  - §10 BlockOreRedstone (oc, IDs 73/74): touch-to-glow mechanic, randomTick reverts, drops 4-6 dust
  - §11 Constants summary table
  - §12 Quirks (8 items: static burnout cross-contamination, wire 0-crossing, repeater flag semantics, lever floor random)
  - §13 Open Questions (5 items: world.b() signature, wire opacity=5, button meta 5 dead code, xb.c unused, ID 143 confirmed absent)

### Tracking completions from prior session
- BlockBed_Spec.md: REQUESTS.md [STATUS:REQUIRED] → [STATUS:PROVIDED]; INDEX.md row updated; classes.md aab/dh/qy/kn entries added; METRICS entry appended

### Corrections to Coder assumptions
- Wire obf name: `zl` → `kw`; torch: `wk` → `ku`; repeater: `ahl` → `mz` (ahl is BlockVine)
- Burnout window: Coder said "8 flips/60 ticks" — actual: "≥8 entries in 100-tick window"
- ID 143 (wood button): confirmed ABSENT in 1.0 — only `new ahv(77, t.bL)` in yy.java, no ID 143 instantiation

### Source files read
`kw.java` (399 lines), `ku.java` (181), `bg.java` (193), `mz.java` (221), `aaa.java` (236), `wx.java` (197), `ahv.java` (264), `oc.java` (122), `lf.java`, `lz.java`, `xb.java`, `yy.java` (grep), `ry.java` (partial, lines 2420-2460)

**Spec count this session:** 1 (BlockRedstone — counts as 1 complex multi-block spec)

---

## Session 2026-04-15 (4)
**Role:** ANALYST
**Session work:** ItemRecord/Jukebox + NetherFortress + BlockPiston + WorldGenStructures specs; tracking updates

### Specs produced

- `Documentation/VoxelCore/Parity/Specs/ItemRecord_Jukebox_Spec.md` (~330 lines)
  - pe (ItemRecord): field `a`=discName string, bN=1; onItemUse: jukebox ID 84 + meta=0 check, calls abl.f(), fires event 1005 with bM, decrements stack
  - abl (BlockJukebox): getBlockTexture face1→bL+1=75 else bL=74; onBlockActivated calls ejectRecord; insertRecord sets TileEntity + meta=1; ejectRecord: event 1005 data=0, EntityItem at world.w RNG offsets (X→Y→Z order), pickup delay=10 ticks
  - agc (TileEntityJukebox): field `a`=int; NBT key "Record" as int (omitted when 0)
  - 11 discs: acy.bB through bL, IDs 2256–2266, items.png row 15 cols 0–10; "wait" ABSENT in 1.0

- `Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md` (~400 lines)
  - ed (MapGenNetherBridge): 1/3 chance per chunk; seed=(cX^(cZ<<4))^worldSeed; offset [4,11]; Y=[48,70]; radius≤112; spawn list: qf=Blaze(w10,2-3), jm=ZombiePigman(w10,4-4), aea=MagmaCube(w3,4-4)
  - tg (Start): creates gc at (cX*16+2, cZ*16+2)
  - rp (PieceRegistry): corridor list (ac/bw/ui/bl/kf/xr) + room list (hg/yj/lu/ahw/tr/acs/io) + ld dead-end
  - 13 piece classes fully documented with dimensions, exit counts, special features
  - kf (BlazeSpawnerCorridor): MobSpawner "Blaze" at local (5,6,3); boolean `a` prevents re-placement
  - io (NetherWartRoom): soul sand (ID 88) + nether wart (ID 115); 13×14×13
  - xr (LavaFortressRoom): lava pool using world.f=true flag wrap

- `Documentation/VoxelCore/Parity/Specs/BlockPiston_Spec.md` (~500 lines)
  - abr (BlockPiston): field `a`=isSticky; static `cb`=anti-reentrance; meta bits 0-2=facing, 3=isExtended; isPowered: 12-position check; push limit=13 (loop 0..12); canPush walkforward; doExtend backward-pass shifting via qz proxy
  - acu (BlockPistonExtension): field `a`=textureOverride (-1=default); two-part AABB; defers neighbor events
  - qz (BlockMovingPiston): ID 36; extends ba; hardness=-1; dropBlockAsItem uses stored block; AABB from agb progress
  - agb (TileEntityPiston): a=blockId, b=blockMeta, j=facing, k=isExtending, l=isSource, m=progress, n=prevProgress; tick advances by 0.5F; NBT saves n not m (quirk); static shared entity-push list `o`
  - ot facing arrays: b/c/d for Y/X/Z offsets; face 0=down, 1=up, 2=north(-Z), 3=south(+Z), 4=west(-X), 5=east(+X)

- `Documentation/VoxelCore/Parity/Specs/WorldGenStructures_Spec.md` (~450 lines)
  - kd (MapGenMineshaft): nextInt(100)==0 && nextInt(80)<max(|cX|,|cZ|); aez piece factory (aba 70%/ra 10%/id 20%); max depth=8, radius≤80; aba: planks/fence/rails/cobweb/cave-spider spawner ~4.3%; 11-entry chest loot table
  - dc (MapGenStronghold): 3 per world; initial angle nextDouble()×π×2; spacing 2π/3; distance=(1.25+nextDouble())×32 chunks; 7 valid biomes; search radius=112 blocks; start=kg; piece=aeh; stone brick ID 98
  - xn (MapGenVillage): 32-chunk grid; offset nextInt(24) X and Z; biomes sr.c+sr.d (plains+desert); cell RNG=world.x(gX,gZ,10387312); returns boolean suppressing dungeon spawn; start=yo; starting piece=yp
  - Integration: xj fields d=dc, e=xn, f=kd; this.t=hasStructures; two-phase (provideChunk + populate); village boolean suppresses nextInt(4)==0 dungeon check

### Tracking
- REQUESTS.md: WorldGenStructures [STATUS:REQUIRED] → [STATUS:PROVIDED]
- INDEX.md: rows for NetherFortress/WorldGenStructures/BlockPiston/ItemRecord_Jukebox updated to [STATUS:PROVIDED] with links
- classes.md: added Piston section (abr/acu/qz/agb/ot), Overworld Structure section (hl/kd/ns/aez/uk/aba/ra/id/dc/kg/aeh/vn/xn/yo/yp), ItemRecord section (pe/agc), NetherFortress piece detail (gc/ac/bw/ui/bl/kf/xr/hg/yj/lu/ahw/tr/acs/io/ld/tg/rp updated)

### Corrections to Coder assumptions
- WorldGenMineshaft: guess `wr` → real `kd` (wr=ItemHoe)
- WorldGenStronghold: guess `vn` → real `dc` (vn=StrongholdCorridor piece, not generator)
- WorldGenVillage: guess `acf` → real `xn` (acf=GUI/rendering class)
- BlockPistonExtension: guess `abq` → real `acu`
- ItemRecord: guess `acb` → real `pe`
- Push limit: Coder said 12 → actual 13 (loop index 0-12 inclusive)

### Source files read
`pe.java`, `abl.java`, `agc.java` (ItemRecord/Jukebox); `ed.java`, `tg.java`, `rp.java`, `gc.java`, `ac.java`, `bw.java`, `ui.java`, `bl.java`, `kf.java`, `xr.java`, `hg.java`, `yj.java`, `lu.java`, `ahw.java`, `tr.java`, `acs.java`, `io.java`, `ld.java` (NetherFortress, 19 files); `abr.java`, `acu.java`, `qz.java`, `agb.java`, `ot.java` (Piston, 5 files); `kd.java`, `dc.java`, `xn.java`, `ns.java`, `yo.java`, `kg.java`, `aba.java`, `ra.java`, `id.java`, `aez.java`, `aeh.java`, `vn.java`, `xj.java` (WorldGenStructures, 13 files)

**Spec count this session:** 4 specs


## Session 2026-04-15 (5)
**Role:** ANALYST
**Session work:** ChunkProviderEnd + BlockPortal specs; tracking updates for both

### Specs produced

- `Documentation/VoxelCore/Parity/Specs/ChunkProviderEnd_Spec.md` (~600 lines)
  - a (ChunkProviderEnd): real obf class is `a` (NOT `io` — `io` is NetherWartRoom piece); 5 noise generators all `eb` (octave perlin): j=16-oct, k=16-oct, l=8-oct, a=10-oct (public), b=16-oct (public, dead code)
  - Density grid: 3×33×3; 8-cell trilinear interpolation with 4×8×4 per-cell subdivision
  - Island shaping: `var22 = 100.0F - me.c(var20*var20 + var21*var21) * 8.0F`, clamped [-100,30]
  - Dead code: `var18 = 0.0` zeroed before fill loop → noise `b` has no effect on output whatsoever
  - Block palette: pure end stone (yy.bJ.bM = 121); surface pass is an empty no-op loop
  - ol (WorldProviderEnd): dim=1; spawn dh(100,50,0); fog=0x808080×0.15F constant; e=true/c=true/g=1; c()=new a(world,seed)
  - uu (BiomeSky): decorator extends ql; spike generator oh; 1/5 chance spike per chunk populate; Ender Dragon oo spawned at (0.0,128.0,0.0) for chunk(0,0) only
  - oh (WorldGenEndSpike): validates end stone floor; height=nextInt(32)+6, radius=nextInt(4)+1; obsidian cylinder (yy.ap.bM=49); EntityEnderCrystal sf on top; bedrock cap (yy.z.bM=7)
  - rl (BlockEndPortalFrame, ID 120): texture 159; meta bits 0-1=facing, bit 2=hasEye; e(meta)=(meta&4)!=0; AABB 0-0.8125 (13/16 height); hardness=-1; light 0.125F; drops nothing; facing from yaw: ((floor(yaw*4/360+0.5)&3)+2)%4
  - aid (BlockEndPortal, ID 119): extends ba (TileEntityRegistry); TileEntity yg; AABB 1/16 thick slab; no collision; onEntityCollided→player.c(1); self-destructs in non-overworld via onBlockAdded; static a guard prevents recursion
  - aag (ItemEnderEye): onItemUse: validates frame+empty, sets meta|4 (hasEye), checks 12-frame ring via lz arrays (3 top + 3 bottom + 3 left + 3 right), fills 3×3 interior with yy.bH.bM (EndPortal ID 119)
  - aim (End platform): for dim==1 places 5×5 obsidian (yy.ap.bM) floor + 3-high air column above

- `Documentation/VoxelCore/Parity/Specs/BlockPortal_Spec.md` (~400 lines)
  - sc (BlockPortal, ID 90): extends aaf (unknown base); no collision b()=null; visual AABB 0.25 thick; g() tryToCreatePortal: 10 obsidian minimum (corners optional), 4×5 frame scan (var7=-1..2, var8=-1..3), 2-axis orientation scan; places 2×3 portal interior at non-corner interior positions; onNeighborChange: destroys column if any block in column not portal-or-interior; entity contact: entity.S() — NOT direct teleport
  - vi.S() / vi.bY / vi.bZ: bY=20 default cooldown, bZ=false trigger; S(): if bY>0 → bY=10; else → bZ=true (sets trigger)
  - aim (Nether travel): b() findPortal radius=128, full 256-height Y scan; c() createPortal radius=16, 2-phase (top-down quality scoring + bottom-up last-resort) + emergency fallback at Y=70; emergency builds 4×5 obsidian frame with 2×3 portal interior
  - ou (ItemFlintAndSteel): bN=1; durability field i(64); places fire (yy.ar.bM=51) on adjacent air; always damages 1 per use (regardless of whether fire was placed); portal ignition indirect via BlockFire.onBlockAdded()→sc.g()
  - No coordinate scaling found in aim — scale likely in WorldServer/ServerConfigurationManager dimension routing

### Tracking
- REQUESTS.md: ChunkProviderEnd [STATUS:REQUIRED] → [STATUS:PROVIDED] with full resolution
- REQUESTS.md: BlockPortal [STATUS:REQUIRED] → [STATUS:PROVIDED] with full resolution
- INDEX.md: ChunkProviderEnd row updated to [STATUS:PROVIDED] with link and description
- INDEX.md: BlockPortal row updated to [STATUS:PROVIDED] with link and description
- classes.md: new "End Dimension Classes" section (a/ol/uu/oh/oo/sf/rl/aid/yg/aag/bs); new "Portal / Travel Classes" section (sc/aaf/aim/ou)

### Corrections to Coder assumptions
- ChunkProviderEnd class: Coder guessed `io` → real `a` (io is NetherWartRoom piece in NetherFortress)
- BlockPortal class: Coder guessed `mc` → real `sc`
- PortalTravelAgent class: Coder guessed `acx` → real `aim`
- ItemFlintAndSteel class: Coder guessed `ahe` → real `ou` (ahe is an unrelated data record class)
- Dead code discovery: noise generator `b` in ChunkProviderEnd has NO effect on output (var18 zeroed)
- Coordinate scaling: NOT in PortalTravelAgent — must be elsewhere in dimension routing code

### Source files read
`a.java` (ChunkProviderEnd), `ol.java` (WorldProviderEnd), `uu.java` (BiomeSky), `oh.java` (WorldGenEndSpike), `rl.java` (BlockEndPortalFrame), `aid.java` (BlockEndPortal), `aag.java` (ItemEnderEye), `aim.java` (PortalTravelAgent), `sc.java` (BlockPortal), `ou.java` (ItemFlintAndSteel), `vi.java` (partial, player portal fields), `yy.java` (partial, block ID constants)

**Spec count this session:** 2 specs (ChunkProviderEnd — large multi-class; BlockPortal — medium multi-class)

### Status after this session
All [STATUS:REQUIRED] entries in REQUESTS.md are now [STATUS:PROVIDED] or [STATUS:IMPLEMENTED].
The original "bitte arbeite alle requests ab" request is fully complete.

---

## Session 2026-04-15 (3) — Coder: ItemRecord / Jukebox batch

### Files created
- `Core/Items/ItemRecord.cs` — pe replica: 11 discs (IDs 2256–2266), OnItemUse inserts disc + broadcasts event 1005, AddInformation tooltip, GetRarity=Rare
- `Core/Items/ItemRarity.cs` — ja enum: Common/Uncommon/Rare/Epic
- `Core/Blocks/BlockJukebox.cs` — abl replica (ID 84): InsertRecord, EjectRecord (world RNG quirk), OnBlockActivated, OnBlockPreDestroy, DropBlockAsItemWithChance (damage=0 quirk)
- `Core/TileEntity/TileEntityJukebox.cs` — agc replica: int RecordId, NBT "Record" (only written when >0)

### Files modified
- `Core/Block.cs` — added `OnBlockPreDestroy` virtual method
- `Core/World.cs` — wired `OnBlockPreDestroy` call before chunk.SetBlock in both SetBlockAndMetadata and SetBlock
- `Core/BlockRegistry.cs` — replaced plain Block(84) stub with `new BlockJukebox(84)`
- `Core/Items/ItemRegistry.cs` — added 11 ItemRecord entries (Disc13..Disc11)
- `Core/TileEntity/TileEntity.cs` — fixed blockIdFactory: ID 84 was wrongly mapped to TileEntityNote (should be ID 25); added ID 84→TileEntityJukebox; replaced TileEntityRecordPlayer stub registration with TileEntityJukebox
- `Core/TileEntity/TileEntityStubs.cs` — removed TileEntityRecordPlayer stub (replaced by TileEntityJukebox)
- `Documentation/VoxelCore/Parity/REQUESTS.md` — ItemRecord_Jukebox: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]
- `Documentation/VoxelCore/Parity/INDEX.md` — ItemRecord_Jukebox row updated to [STATUS:IMPLEMENTED]

### Bug fixes during this session
- **TileEntity blockIdFactory ID 84 wrong**: was `new TileEntityNote()` — note block is ID 25, jukebox is ID 84. Added separate entry for each.
- **TileEntityRecordPlayer stub had no NBT**: was a no-op stub; replaced with full TileEntityJukebox (int RecordId, "Record" tag, write-only-when-nonzero quirk).
- **No OnBlockPreDestroy hook in engine**: block-break path had no way to call pre-destroy logic while TE is still accessible. Added virtual Block.OnBlockPreDestroy and wired it in World.SetBlockAndMetadata + World.SetBlock.

### Build result
0 errors, 0 warnings.

---

## Session 2026-04-15 (4) — Coder: BlockPiston batch

### Files created
- `Core/TileEntity/TileEntityPiston.cs` — full agb replica: fields a/b/j/k/l/m/n, Tick() (+0.5F/tick, finalize at ≥1.0F with block commit), InstantFinalize(), EntityPush() (static shared list o — quirk §10.3), NBT read/write (quirk §10.2: writes n not m)
- `Core/Blocks/BlockMovingPiston.cs` — qz (ID 36) replica: hardness −1.0F, SetIsContainer(true), DropBlockAsItemWithChance reads stored block from TE, GetCollisionBoundingBoxFromPool progress-offset AABB; static GetMovingAABB() shared with TileEntityPiston entity-push
- `Core/Blocks/BlockPistonExtension.cs` — acu (ID 34) replica: GetTextureForFace(face,meta) distinguishing front/back/side; dual-part (face plate + shaft) AddCollisionBoxesToList; OnBlockRemoved retracts base piston; OnNeighborBlockChange defers to base or removes orphan
- `Core/Blocks/BlockPiston.cs` — abr (IDs 29/33) replica: static s_cb anti-reentrance (quirk §10.1), DetermineFacing (quadrant+up/down), CheckAndTrigger (12-position IsPowered, CanPush 13-block walk), Activate dispatch (extend/retract), DoExtend backward-pass phase

### Files modified
- `Core/World.cs` — added `SetTileEntity(x,y,z,te)` and `RemoveTileEntity(x,y,z)` for TileEntityPiston finalization
- `Core/BlockRegistry.cs` — IDs 29→BlockPiston(sticky), 33→BlockPiston(normal), 34→BlockPistonExtension, 36→BlockMovingPiston
- `Core/TileEntity/TileEntity.cs` — added `{ 36, () => new TileEntityPiston() }` to blockIdFactory; TileEntityPiston stub registration retained (now full class)
- `Core/TileEntity/TileEntityStubs.cs` — removed TileEntityPiston stub (replaced by full TileEntityPiston.cs)
- `Documentation/VoxelCore/Parity/REQUESTS.md` — BlockPiston: [STATUS:PROVIDED] → [STATUS:IMPLEMENTED]
- `Documentation/VoxelCore/Parity/INDEX.md` — BlockPiston row updated to [STATUS:IMPLEMENTED]

### Build result
0 errors, 0 warnings.

## Session 2026-04-16 (1)
**Role:** ANALYST
**Session work:** PortalTravelAgent spec + BowArrow spec

### Specs produced

- `Documentation/VoxelCore/Parity/Specs/PortalTravelAgent_Spec.md` (~300 lines)
  - aim (PortalTravelAgent): NOT a singleton, fresh instance per transition
  - a(): dispatches to End platform (dim==1) or Nether portal logic (all others)
  - End platform: 5×5 obsidian floor (Y=entity.Y−1) + 3-high air clear; centred at arrival pos (100,49,0); entity placed at floor level
  - b() findPortal: XZ ±128 grid scan, full-height Y top-to-bottom, 3D distance-squared minimum; centering offset ±0.5 toward portal axis; returns false if not found
  - c() createPortal: Phase 1 = 3×4×5 suitability (solid floor + air interior, 4 orientations, nextInt(4) random start); Phase 2 = 4×1×5 column check, 2 orientations; Emergency = Y clamped to [70, height-10], clears 2×3 pocket; Frame always built 4× (loop) to trigger portal activation; 4×5 obsidian+portal frame, 2×3 interior
  - Coordinate scaling NOT in aim — in Minecraft.a(int): Overworld→Nether ×0.125, Nether→Overworld ×8.0, End ×1.0
  - aim.a() not called when leaving End (oldDim=1 fails condition oldDim<1)

- `Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md` (~400 lines)
  - il (ItemBow, ID 261): durability 384; charge formula power=(f²+2f)/3 (f=ticks/20); threshold 0.1; critical at power==1.0; speed=power×3.0; damage=ceil(speed×2.0)+crit bonus; consumes 1 arrow per shot
  - ro (EntityArrow): hitbox 0.5×0.5; gravity 0.05/tick; air drag 0.99, water 0.8; inGround despawn 1200t; pickup: inGround+isPlayer+shake==0; NBT: shooter NOT saved (lost on reload), critical NOT saved
  - hd (ItemFishingRod, ID 346): durability 64; cast=spawn ael, reel-in=call ael.g(); no durability on cast
  - ael (EntityFishHook): NOT in EntityList (no NBT); bite RNG 1/500 (1/300 with sky); bite dips hook, plays splash; gives RawFish (acy.aT=ID 349) + 1 XP stat on reel; durability cost: 0(miss)/1(fish)/2(ground)/3(entity)
  - it (EntitySkeleton, ID 51): fires arrow at <10 blocks range; speed 1.6 spread 12; reload 60 ticks; drops 0–2 arrows+bones; sunlight burn per-tick random; Sniper achievement ≥50 blocks

### Tracking
- REQUESTS.md: PortalTravelAgent [STATUS:REQUIRED] → [STATUS:PROVIDED]
- REQUESTS.md: BowArrow [STATUS:REQUIRED] → [STATUS:PROVIDED]
- INDEX.md: both rows added as [STATUS:PROVIDED]
- classes.md: new "Ranged Combat / Fishing Classes" section (il/ro/hd/ael/it)

### Corrections to Coder assumptions
- EntityFishHook is NOT in EntityList — no NBT persistence, no entity ID string
- Fish bite RNG is 1/500 per tick (not per-second) or 1/300 with sky exposure
- No treasure or junk loot in 1.0 — only RawFish
- Fishing rod costs 0 durability on cast (damage only on reel-in)
- Arrow pickup condition is shake==0 (not ticksInGround>7 as Coder assumed)

### Source files read
`il.java` (ItemBow), `ro.java` (EntityArrow), `hd.java` (ItemFishingRod), `ael.java` (EntityFishHook), `it.java` (EntitySkeleton), `afw.java` (EntityList — verified ael absent), `acy.java` (item IDs for bow/arrow/rod/fish/bone), `Minecraft.java` (coordinate scaling)

**Spec count this session:** 2 specs

### Remaining [STATUS:REQUIRED]
- EnchantingXP (Priority 3)

## 2026-04-16 (2) — [ANALYST] — EnchantingXP

**Worked on:**
- `fk` (EntityXPOrb) — size/gravity/attraction/despawn/tier system; 10 value tiers via threshold array; pickup sequence; NBT Health/Age/Value
- `vi` (EntityPlayer XP fields) — cd/cf/ce fields; addXP loop with fractional progress; `aN()=7+floor(level×3.5)` per-level formula; deductLevels; death drop formula
- `sy` (BlockEnchantmentTable ID 116) — partial AABB 0–0.75; bookshelf particle spawner (5×5 ring, LOS check, 1/16 chance); onActivate
- `rq` (TileEntityEnchantmentTable) — floating book animation: player-tracking yaw, page-flip RNG, open/close m field
- `ahk` (ContainerEnchantment) — bookshelf gap-check (adjacent air at y and y+1) then ±2-block scan with diagonal corners; per-container seed via nextLong; slot level array c[3]
- `ml` (EnchantmentHelper) — slot-level formula: base=1+nextInt(bonus/2+1)+nextInt(bonus+1), noise+=nextInt(5), slot0=(noise>>1)+1/slot1=noise×2/3+1/slot2=noise; weighted enchantment selection with compatibility pruning loop
- `aef` (Enchantment base) — 19-enchantment registry with IDs, weights, targets, min/max power ranges; confirmed NO bow enchantments in 1.0
- Subclasses: `ii` (protection group, subtypes 0-4), `vu` (Respiration), `adz` (AquaAffinity), `ap` (damage group, subtypes 0-2), `dz` (Knockback), `aie` (FireAspect), `qn` (Looting/Fortune), `kr` (Efficiency), `gi` (SilkTouch), `dq` (Unbreaking)
- `vs` (EnchantmentData) — weighted wrapper for enchantment+level pairs

**Estimated effort:** ~4 hours equivalent

**Notes:**
- No xpSeed per-player — enchantment seed is per-ContainerEnchantment instance (fresh nextLong on item placement); not reseedable mid-session
- Items can only be enchanted once via table (dk.t() guards on !alreadyEnchanted)
- Several subclasses use `super.a(level)+50` for max power (= 1+L×10+50) regardless of their own min formula — this asymmetry is deliberate and must be preserved
- SilkTouch (gi) and Fortune (qn for tool) are bidirectionally mutually exclusive via canCoexist overrides
- All three formerly [STATUS:REQUIRED] entries are now [STATUS:PROVIDED]; queue is empty

### Tracking
- REQUESTS.md: EnchantingXP [STATUS:REQUIRED] → [STATUS:PROVIDED]
- INDEX.md: EnchantingXP_Spec.md row added as [STATUS:PROVIDED]
- classes.md: new "Enchanting / XP Classes" section (19 entries: fk/sy/rq/wn/ahk/ml/aef/ii/vu/adz/ap/dz/aie/qn/kr/gi/dq/vs/q)

### Source files read
`fk.java`, `vi.java`, `sy.java`, `rq.java`, `ahk.java`, `ml.java`, `aef.java`, `ii.java`, `vu.java`, `adz.java`, `ap.java`, `dz.java`, `aie.java`, `qn.java`, `kr.java`, `gi.java`, `dq.java`, `vs.java`, `dk.java` (t() method), `acy.java` (c() method), `wn.java` (renderer), `ah.java` (renderer dispatcher)

**Spec count this session:** 1 spec (EnchantingXP_Spec.md)

### Remaining [STATUS:REQUIRED]
None — queue empty. Proactive speccing applies from next session onward.

## 2026-04-16 (3) — [ANALYST] — EnderDragon (proactive)

**Worked on:**
- `oo` (EntityDragon) — full analysis: 7-part body system; flying AI with 64-entry yaw/Y ring buffer; crystal healing; wing knockback; head melee; block destruction whitelist; damage routing (head=full, body=quarter); new waypoint selection; 200-tick death sequence with 20000 XP total; exit portal generator (EndPortal disc, dragon egg, torch ring at Y=64/68)
- `adh` (EntityBoss) — damage immunity base; bypass via `e()`
- `vc` (EntityBodyPart) — multi-part entity structure; damage delegation
- `sf` (EntityEnderCrystal) — tick behaviour (fire placement); 1-shot death → power 6 explosion; crystal→dragon healing link

**Estimated effort:** ~2 hours equivalent

**Notes:**
- `az()` is dead code (declares locals, no side effects) — preserved as parity note
- Death XP is exactly 20000: 10 × 1000 (ticks 155–200 by 5) + 10000 (tick 200) — tick 200 triggers BOTH
- Crystal destruction dealing 10 damage to the dragon when focused is bidirectional: crystal death → dragon damage, not just dragon healing
- Portal placed at world.height/2 = 64 (hardcoded; non-standard world heights would shift it)
- Open: EnderCrystal EntityList ID unconfirmed; `af` field meaning; `aQ` condition semantics; respawn behaviour

### Source files read
`oo.java` (EntityDragon), `adh.java` (EntityBoss), `vc.java` (EntityBodyPart), `sf.java` (EntityEnderCrystal), `yy.java` (bH/bK/aq block ID lookup)

**Spec count this session:** 1 proactive spec (EnderDragon_Spec.md)

## 2026-04-16 — [CODER] — PortalTravelAgent + BowArrow + EnchantingXP implementation batch

**Worked on:**

### PortalTravelAgent
- `Core/PortalTravelAgent.cs` — `aim` class; End platform (5×5 obsidian + air layers); findPortal (±128 XZ grid, top-down Y scan, distance-sq minimisation, axis-centering); createPortal (Phase 1 3D suitability × 4 orientations, Phase 2 2D fallback × 2, emergency Y=70); 4-pass frame build with SuppressUpdates + NotifyNeighbors per block; orientation table 0=Z+/1=X+/2=Z-/3=X-

### BowArrow
- `Core/Items/ItemBow.cs` — ID 261; charge formula `power=(f²+2f)/3`; OnPlayerStoppedUsing fires arrow at power×2; crit flag at power==1; DamageItem(1); consume arrow from inventory
- `Core/EntityArrow.cs` — entity ID 10 "Arrow"; ray-trace block hit + entity scan (expand+1.0 then entity AABB +0.3); damage ceil(speed×2.0); crit bonus; target.PendingKnockback++; stuck: verify block unchanged, despawn 1200t, shake countdown; water drag via block ID 8/9; gravity 0.05; NBT persists xTile/yTile/zTile/inTile/inData/shake/inGround/player (shooter/critical NOT persisted)
- `Core/Items/ItemFishingRod.cs` — ID 346; dual cast/reel path; durability 64
- `Core/EntityFishHook.cs` — NOT in EntityList (no NBT persistence); auto-remove on owner death/dismount/out-of-range; fish bite RNG 1/500 (1/300 with sky); buoyancy from 5-sample submersion fraction; ReelIn() returns 0/1/2/3 durability cost
- `Core/Mobs/ConcreteMobs.cs` — EntitySkeleton arrow attack: 60-tick cooldown, speed 1.0, spread 12.0; sunburn check (daytime+canSeeSky+brightness>0.5F → SetFire(8))

### EnchantingXP
- `Core/EntityXPOrb.cs` — entity ID 2 "XPOrb"; gravity 0.03F; attraction (1-normDist)²×0.1; despawn 6000t; pickup sets InvulnerabilityCountdown=2; tier thresholds; NBT "Health"/"Age"(despawnAge)/"Value"
- `Core/Blocks/BlockEnchantmentTable.cs` — ID 116; hardness 5.0/resistance 2000; partial AABB y=[0,0.75]; 5×5 ring bookshelf particle scan with inner skip + 1/16 roll + LOS check; TileEntity flag
- `Core/TileEntity/TileEntityEnchantmentTable.cs` — animated book: player proximity scan, target yaw tracking (atan2), open/close with lift±0.1, smooth rotation (wrap [-π,π]), page-flip momentum; replaced stub TileEntityEnchantTable
- `Core/Enchantments/Enchantment.cs` — base class + static registry [36] + all 17 static instances; GetMinLevel/MaxLevel/MinPower/MaxPower/CanApplyTo/IsCompatibleWith/GetDamageReduction/GetAttackBonus virtual methods; EnchantmentTarget enum
- `Core/Enchantments/EnchantmentSubclasses.cs` — all 19 enchantments: EnchantmentProtection (0-4), EnchantmentRespiration (5), EnchantmentAquaAffinity (6), EnchantmentDamage (16-18), EnchantmentKnockback (19), EnchantmentFireAspect (20), EnchantmentLootBonus (21/35), EnchantmentEfficiency (32), EnchantmentSilkTouch (33), EnchantmentDurability (34); mutual exclusivity logic; damage reduction formulas
- `Core/Enchantments/EnchantmentData.cs` — (enchantment, level) pair container
- `Core/Enchantments/EnchantmentHelper.cs` — SlotLevel() formula; SelectEnchantments() with weighted random selection, power fuzz, multi-enchantment expansion
- `Core/ContainerEnchantment.cs` — bookshelf gap+shelf scan (8 adjacent, diagonal extras); OnInputChanged() refreshes seed + slot levels; Enchant() deducts levels, applies NBT, validates table/distance

### Existing file updates
- `Core/EntityRegistry.cs` — Register<EntityXPOrb>("XPOrb", 2) + Register<EntityArrow>("Arrow", 10) (replaced RegisterId stubs)
- `Core/Items/ItemRegistry.cs` — Bow (261), Arrow plain item (262), FishingRod (346), Bone (352)
- `Core/BlockRegistry.cs` — BlockEnchantmentTable(116) replaces plain Block(116)
- `Core/TileEntity/TileEntity.cs` — blockIdFactory for 116 → TileEntityEnchantmentTable; registry updated TileEntityEnchantTable → TileEntityEnchantmentTable
- `Core/TileEntity/TileEntityStubs.cs` — removed TileEntityEnchantTable stub
- `Core/ItemStack.cs` — _nbtTag: object? → NbtCompound?; HasEnchantments() reads "ench" key; AddEnchantment() writes {id,lvl} entries; GetTagCompound() typed properly
- `Core/Item.cs` — added GetEnchantability() virtual (default 0)

**Estimated effort:** ~8 hours equivalent
**Notes:**
- EntityFishHook is intentionally absent from EntityRegistry — no NBT persistence by spec (§9.4)
- EntitySkeleton arrow spawned server-side only (checked implicitly via EntityMonster/World.SpawnEntity flow)
- Enchantability values on tool/armor subclasses are not yet wired (GetEnchantability() returns 0 base) — enchanting is correct for the ContainerEnchantment logic but all items will show 0 enchantability until tool subclasses override c()
- PortalTravelAgent coordinate scaling (÷8/×8) lives in the caller (EntityPlayer.TravelToDimension stub) — not in PortalTravelAgent itself per spec
- Item(int) constructor changed from protected to public to allow plain item instances (Arrow ID 262, Bone ID 352) in ItemRegistry

---

## 2026-04-16 — [CODER] — EnderDragon implementation

**Worked on:**

### EnderDragon
- `Core/EntityBodyPart.cs` — replica of `vc`; 7 instances per dragon (head/body/tail×3/wings×2); AttackEntityFrom delegates to EntityDragon.OnBodyPartHit; IsSameTeam() check; no NBT
- `Core/EntityEnderCrystal.cs` — replica of `sf`; entity ID 200 "EnderCrystal"; tick counter with random offset; perpetual fire beneath crystal; 1-hit kill triggers power-6 explosion; quirk §12.6 (b=0 before always-true check) preserved; no NBT
- `Core/EntityDragon.cs` — EntityBoss base class (`adh`) + EntityDragon (`oo`); entity ID 63 "EnderDragon"; 16×8 hitbox; maxHealth 200; fire-immune; 64-entry ring buffer for body-part trailing positions; flying AI (target tracking, yaw steering with ±50° clamp, forward thrust formula, drag 0.8–0.91); crystal healing 1HP/10t; damage routing (head=full, other parts /4+1, player/fire only bypass immunity); death sequence: upward drift, 20°/tick spin, XP batches (10×1000 + final 10000 = 20000 XP total at tick 200); exit portal generator (EndPortal disc at Y=64 radius 2.5, bedrock ring, clear columns above, center pillar with torches at Y=66, dragon egg at Y=68); block destruction whitelist (obsidian 49 / bedrock 7 / end portal 119 survive); wing-push collision; head-area melee 10 damage

### Existing file updates
- `Core/EntityRegistry.cs` — Register<EntityDragon>("EnderDragon", 63) + Register<EntityEnderCrystal>("EnderCrystal", 200) (replaced RegisterId stubs)

**Estimated effort:** ~2 hours equivalent
**Notes:**
- Dragon NBT is intentionally empty — the dragon is spawned fresh per End session, not persisted
- Body parts are Entity (not LivingEntity) so they cannot appear in GetEntitiesWithinAABB<LivingEntity> — wing push and head damage scans operate correctly on LivingEntity only
- Open question §13.2 (af field = isMultipartEntity) noted in comment; no C# equivalent needed yet
- Open question §13.4 (respawn on re-entry) not addressed — single spawn in ChunkProviderEnd is sufficient for 1.0 parity
- Dead-code az() preserved as comment per spec §12.1

## 2026-04-16 (4) — [ANALYST] — StrongholdPieces (proactive)

**Worked on:**
- `tc` (StrongholdPieceFactory) — weight table (11 entries), depth limit 50, XZ radius 112, `vn` fallback
- `mj` (DoorType enum) — 4 constants: open/wood/iron/grating
- `os` (abstract StrongholdPiece base) — fields and virtual interface
- `aeh` (StrongholdStart) — extends `vl`; fields a/b/c; forced Large Room at start
- `gp` (SimpleCorridor) — 5×5×7; material palette; cobweb/torch decorations
- `vn` (StraightCorridor) — 5×5×var; fallback terminator
- `fj` (Prison) — 9×5×11; iron-bar cells; chest loot
- `hq` / `xg` (LeftTurn / RightTurn) — 5×5×5 corner pieces
- `jt` (Crossing) — 11×7×11; up to 3 exits
- `kt` (LargeRoom) — 10×9×11; forced as first piece; chest
- `so` (SpiralStairs) — 5×11×8; descends 7 via slab spiral
- `vl` (StraightStairs) — 5×11×5; descends 7
- `ys` (SmallRoom) — 5×5×7; dead-end
- `zc` (Library) — dual-height (tall 14×11×15 / short 14×6×15); bookshelf + loot chest; max 2
- `ir` (PortalRoom) — 11×8×16; End Portal Frames (ID 120); silverfish spawner; registers as `aeh.b`; max 1

**Estimated effort:** ~2 hours equivalent

**Notes:**
- Resolves WorldGenStructures_Spec Open Question 7.1 (full piece list)
- Portal Room water pool vs. lava pool ambiguity at lines 36–39 noted as Open Question 8.4
- Chest loot for corridors/prisons/large rooms not yet confirmed (Open Question 8.1)
- `vn` exact fallback length not confirmed (Open Question 8.2)
- `aeh.b` set only once — no guard flag needed because `ir` is limited to 1 per stronghold by `th` wrapper

### Source files read
`os.java`, `tc.java`, `mj.java`, `aeh.java`, `vl.java`, `so.java`, `gp.java`, `vn.java`,
`fj.java`, `hq.java`, `xg.java`, `jt.java`, `kt.java`, `ys.java`, `zc.java`, `ir.java`

**Spec count this session:** 1 proactive spec (StrongholdPieces_Spec.md)

---

## 2026-04-16 — [CODER] — StrongholdPieces implementation

**Worked on:**

### StrongholdPieces
- `Core/WorldGen/Structure/StrongholdPieces.cs` — full implementation of spec §3–§6:
  - `StrongholdDoor` enum (Open/WoodDoor/IronDoor/IronBars)
  - `PieceExit` struct (local connection point, orientation delta, Y offset, door type)
  - `StrongholdPieceBase` abstract base: block ID constants; `PlaceStoneBrickRandom()` (33% each: normal/cracked/mossy); `PlaceShell()` (stone brick walls + air interior); `PlaceDoor()` (2-tall doorway or iron bars); protected `GetWorldX/Y/Z` coordinate converters exposed via `..._Public` wrappers for factory use
  - `ShCorridor` (gp) — 5×5×7; stone brick shell; cobweb/torch decorations; 1 forward exit
  - `ShStraightCorridor` (vn) — 5×5×5 fallback terminator; no exits
  - `ShLeftTurn` / `ShRightTurn` (hq/xg) — 5×5×5 corner pieces; 1 lateral exit each
  - `ShPrison` (fj) — 9×5×11; iron-bar cell walls; chest in cells; 1 forward exit
  - `ShCrossing` (jt) — 11×7×11; always exposes 3 exits (F/L/R) — OQ §8.3 resolved
  - `ShLargeRoom` (kt) — 10×9×11; chest; 2 exits; forced as first piece after Start
  - `ShSpiralStairs` (so) — 5×11×8; stone brick stair spiral descending −7; 1 forward exit at bottom
  - `ShStraightStairs` (vl) — 5×11×5; straight stair flight descending −7; 1 forward exit at bottom; `IsStart` flag
  - `ShSmallRoom` (ys) — 5×5×7; dead-end alcove; no exits
  - `ShLibrary` (zc) — dual-height: tall 14×11×15 / short 14×6×15; `IsTall` flag; bookshelves + loot chest; both variants
  - `ShPortalRoom` (ir) — 11×8×16; 8 of 12 End Portal Frame blocks (ID 120) around 3×3 ring; water pool (9); lava under frame (11) — OQ §8.4 resolved as lava; silverfish spawner (ID 52); iron-door entrance; `_spawnerPlaced` flag guard
- `StrongholdFactory` — `WeightedEntry` table (11 piece types with weights and max counts); `GeneratePieces(originX, originY, originZ, startOrientation, rng)` returns `List<StructurePiece>`; BFS queue with depth>50 and XZ-radius>112 guards; `PlaceAt()` helper computing BBox for all 4 orientations; `ShStraightCorridor` fallback on failed placement

### MapGenStronghold update
- `Core/WorldGen/MapGenStronghold.cs` — replaced `StrongholdStartRoomStub` class with `new StrongholdFactory().GeneratePieces(originX, originY, originZ, startOri, rng)`; `originY=50`; `startOri=rng.NextInt(4)`

### Documentation
- `Documentation/VoxelCore/Parity/INDEX.md` — StrongholdPieces: `[STATUS:PROVIDED]` → `[STATUS:IMPLEMENTED] Core/WorldGen/Structure/StrongholdPieces.cs + Core/WorldGen/MapGenStronghold.cs`

**Estimated effort:** ~1.5 hours equivalent
**Notes:**
- OQ §8.2: vn fallback length set to 5 (shortest plausible dead-end)
- OQ §8.3: jt (Crossing) always exposes all 3 exits — no random suppression
- OQ §8.4: lava under portal frame confirmed (not water); water pool at floor pit is separate (ID 9 at y+1)
- WoodPlanksId corrected to 5 (not 4 which is cobblestone)
- ShPortalRoom places 8 of 12 frame blocks (the 4 corner positions omit frames per vanilla); full 12-frame ring would be activated by ItemEnderEye externally
- Build: 0 errors, 0 warnings

---

## 2026-04-17 — [ANALYST] — Coder Request Batch (30 specs, all 34 REQUIRED entries resolved)

**Worked on:**

### Entity specs
- `EntityFallingSand_Spec.md` — `uo` class; gravity 0.04/tick, drag 0.98; landing: place block or drop item; NBT: TileID + Data; only sand (12) and gravel (13) fall; despawn if not in EntityList
- `ThrowableEntities_Spec.md` — abstract throwable base `yz`; snowball (`jv`, gravity 0.03, drag 0.99, extinguishes Blaze); egg (`aia`, 1/8 chicken + 1/32 quadruplet); EnderPearl (`ke`, 5 fall damage, teleport to landing); Fireball (`bb`, Ghast projectile, power=1, block damage, fire); SmallFireball (`xo`, Blaze projectile, no block damage); Eye of Ender signal (`aet`, arc + float + despawn)
- `RemainingMobs_Spec.md` — 12 mobs: Slime (`ni`, size DW slot 16, split on death), Ghast (`aai`, 10HP, fireball at player), PigZombie (`aho`, neutral/group-aggro/anger NBT), Enderman (`aex`, block pickup IDs listed, stare detection, teleport on projectile/water), CaveSpider (`zj`, 0.7×0.5, poison on hard), Silverfish (`yk`, 0.3×0.7, calls nearby, monster egg ID 97), Blaze (`xl`, 3-fireball burst, Y-oscillate float, blaze rod drop), MagmaCube (`aq`, fire-immune, splits), Squid (`aze`, passive water, ink sac), Wolf (`aag`, bone taming, collar DW, anger NBT), MushroomCow (`aad`, shear → cow + mushrooms, mushroom-soup milk), SnowGolem (`abb`, 2-snow-block+pumpkin build, snowball AI, snow trail, melt in warm)
- `EntityPainting_Spec.md` — entity class `yb`; `sv` EnumArt 25 variants (full table: Kebab 1×1 to Bust 2×2 … Sky 2×1, Wanderer 1×2, Graham 1×2, Pool 2×1, Courbet 2×1, Sunset 2×1, Sea 2×1, Creebet 2×1, Wanderer 1×2, Match 2×2, Bust 2×2, Stage 2×2, Void 2×2, SkullAndRoses 2×2, Wither 2×2, Fighters 4×2, Skeleton 4×3, DonkeyKong 4×3, Pointer 4×4, Pigscene 4×4, BurningSkull 4×4, Aztec 1×1, Aztec2 1×1, Bomb 1×1, Plant 1×1, Wasteland 1×1); placement on wall face; random variant fitting wall space; NBT: Motive+Dir+TileXYZ
- `EntityBoat_Spec.md` — `no`; partial-submersion buoyancy lift; rider yaw forwarding; speed ~8 m/s; destroyed at >2 hit damage or speed collision; drops 3 planks; no lava survival; NBT base fields only
- `EntityMinecart_Spec.md` — `vm` single class, type field 0/1/2; rail metadata 0-9 (straight/slope/curve); powered rail (ID 27 obf `aig`); detector rail (ID 28 obf `abh`); max speed 8 m/s; slope gradient; storage minecart 27 slots; powered minecart coal fuel pushes linked carts; off-rail physics; NBT: Type + Items + PushX/Z + Fuel

### Block specs
- `BlockFenceGate_Spec.md` — `fp`; meta bits 0-1=facing, bit 2=isOpen; closed AABB matches fence height 1.5; open=no collision; right-click toggle; redstone activatable; drops 1 fence gate
- `BlockVine_Spec.md` — `ahl`; metadata bitmask N=1/E=2/S=4/W=8; canBlockStay requires adjacent solid on any attached face or solid above; climbing via isOnLadder; spread random tick tries up/4 sides; no collision, but raycast hitbox exists; shear to drop; material = air-like
- `BlockPane_Spec.md` — `uh` base (both IDs 101/102); post 2/16 wide centered; arms 2/16 thick; connects to same type OR solid opaque block; glass pane additionally connects to glass blocks; full height 1.0; isOpaqueCube=false; light opacity 0 for glass, 0 for iron bars; drops self
- `BlockTrapDoor_Spec.md` — `mf`; meta bits 0-1=attachment face (bottom/top/N/S), bit 2=isOpen; closed AABB: 3/16 slab at floor or ceiling; open AABB: 3/16 slab against wall; right-click toggle + redstone; wood only in 1.0; drops itself
- `BlockGlowstone_Spec.md` — `sk`; drops rand(4)+2 glowstone dust (ID 348); silk touch drops block; hardness 0.3; light 15; material glass; recipe 4 dust → 1 block in CraftingRecipes
- `BlockGrassPlant_Spec.md` — `wg` base (BlockFlower); fields: none; canBlockStay checks solid block below; onNeighborChange calls canBlockStay and drops self; drops self; ID 37/38 share `wg`; ID 31 (TallGrass) drops nothing normally, shear drops itself, rare wheat seed with Fortune; ID 32 (DeadBush) drops stick; rails use separate `afr`/`aig` base not `wg`
- `BlockPlants_Spec.md` — 9 plant types with canBlockStay, tick growth, bonemeal, drops: Sapling (ID 6, `ack`, meta=wood type, nextInt(7)==0 grow, bonemeal forces, oak/spruce/birch/jungle dispatch), Dandelion/Rose (IDs 37/38, `wg`, no tick, drops self), Mushroom (IDs 39/40, `agb`/`agc`, spread if <5 in 9×9×3, die above light 12), Reed (ID 83, `ah`, water adjacent required, max height 3, +1/tick), NetherWart (ID 115, `aas`, soul sand only, 4 stages, no bonemeal in 1.0, drops 1-4), MelonStem (ID 104, `abu`, stage 7 places melon in adjacent air), PumpkinStem (ID 105, `abu` subclass, same logic)
- `BlockRail_Spec.md` — plain rail `afr` (ID 66, meta 0-5 straight+slope, 6-9 curves, auto-connects on placement), PoweredRail `aig` (ID 27, meta 0-5 + bit3=powered, boosts/brakes), DetectorRail `abh` (ID 28, emits RS signal when minecart above), ActivatorRail not in 1.0 (ID 157 added 1.5); canBlockStay solid below; all drop themselves as items
- `BlockChest_Spec.md` — `ae`; double-chest: detect adjacent chest on X or Z axis (not Y), priority: Z then X; combined as `adv` (InventoryLargeChest) wrapping two IInventory (slots 0-26 left, 27-53 right); `numPlayersUsing` counter drives lid animation; onBlockActivated opens ContainerChest with combined inventory; breaking drops all contents as EntityItem; no cat-sitting mechanic in 1.0
- `BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec.md` — Workbench (`yy` base `aq` field = ID 58, opens ContainerCrafting 3×3, drops itself); Furnace (ID 61/62, `oq`, facing meta 0-5, lit state via TileEntityFurnace.burnTime>0, light 13 when lit, onBlockActivated opens ContainerFurnace); Cauldron (ID 118, `al`, meta 0-3 water level, bucket fill/empty, rain fill random tick, 2-block AABB cutout, drops self); BrewingStand (ID 117, `arh`, 4 slots: ingredient+3 bottles, brewTime 400 ticks, no fuel in 1.0, NBT: Items+BrewTime, light 1 when active)

### Item specs
- `ItemBucket_Spec.md` — `en`; single class, fluid type field; empty (ID 325): picks up still water (ID 9) or still lava (ID 11), replaces with air, returns filled bucket; water bucket (ID 326): places water source, returns empty bucket; lava bucket (ID 327): same for lava; milk bucket (ID 335): obtained from cow right-click, OnItemRightClick removes all potion effects + restores full hunger; can overwrite replaceable blocks; stack size 1 for filled, 16 for empty; dispensable
- `ItemDye_Spec.md` — `xv`; bonemeal (meta 15): crops→stage 7, sapling→tree growth, grass block→WorldGenTallGrass/Flowers scatter, stems→stage 7, mushroom→huge mushroom; other metas 0-14: applied to wool block or sheep entity to set wool color; texture: 16 icons in items.png; stack 64; ink sac (meta 0) dropped by squid
- `ItemShears_Spec.md` — `abo`; durability 238; onItemUse on leaves (18/161) drops leaf block; on vines (106) drops vine; on cobweb (30) drops string; on sheep: drops 1-3 wool of sheep color, sets Sheared flag; on mooshroom: drops 5 mushrooms + converts to cow; on snow golem: removes pumpkin revealing face; canHarvestBlock: true for cobweb; 1 durability per use
- `ItemSign_Spec.md` — `my`; ID 323; onItemUse on side face → wall sign (ID 68) meta=facing 2-5; on top face → floor sign (ID 63) meta=yaw/22.5 rounded &15 (16 positions); after placement opens sign-editing GUI via server packet; both sign block forms drop item ID 323; stack 16
- `ItemGoldenApple_Spec.md` — `afk`; subclass of ItemFood; meta 0 (regular): heals 4HP, Regeneration II 5s, recipe 8 gold nuggets + apple (1.0 uses nuggets, not ingots); meta 1 (Notch apple): heals 4HP, Regeneration IV 30s, Absorption 120s, Fire Resistance 300s, recipe 8 gold blocks + apple; uses ItemFood.FinishUsingItem path

### AI / Mob / Container specs
- `EntityVillager_Spec.md` — `aaaj`; professions 0-4 (Farmer/Librarian/Priest/Blacksmith/Butcher); MerchantRecipe: buy1+buy2+sell ItemStacks, maxUses counter; trades unlocked per profession at generation; wanders, returns inside at night (DayTime>13000), flees zombies; no zombie villager conversion in 1.0; NBT: Profession int + Riches int + Offers list; breeding: houses>villager count triggers
- `Container_Spec.md` — `pj` (ContainerBase): slots list, canInteractWith distance check, slotClick `b(slotId, btn, shift, player)`: left=swap cursor/slot, right=split/take-half, shift=auto-move, double-click=collect; SlotCrafting (`afe`) decrements all 3×3 inputs on take, triggers onCrafting; ContainerWorkbench (`ace`, 3×3 grid `ag`, 10 slots 0-9); ContainerFurnace (`eg`, 3 slots, cookTime 0-200, burnTime, a/b sync methods); ContainerChest (`ak`, 27 or 54 slots); ContainerPlayer (`gd`, 4×9 main + 4 armor); ContainerDispenser (9 slots)
- `CraftingRecipes_Spec.md` — `sl` (CraftingManager) singleton; shaped format: String[] pattern + char→ItemStack/int map, supports mirror; shapeless: ArrayList ingredients; full vanilla 1.0 recipe table in appendix (~170 recipes): tools/armor (all materials), building blocks, food, redstone, decorative, weapons, misc; FurnaceRecipes `mt`: 30 smelting recipes
- `PotionEffect_Spec.md` — `abg` (Potion) 19 effects IDs 1-19 (Speed/Slowness/Haste/MiningFatigue/Strength/InstantHealth/InstantDamage/JumpBoost/Nausea/Regeneration/Resistance/FireResistance/WaterBreathing/Invisibility/Blindness/NightVision/Hunger/Weakness/Poison); `s` (PotionEffect): effectId+durationTicks+amplifier; performEffect per-tick or instant; all 19 per-tick behaviours documented; ItemPotion (`abk`, ID 373): meta encodes effect+tier+splash flag; splash radius 4-block sphere with distance falloff; PotionHelper `pk` for color blending

### Physics / Survival specs
- `Entity_Physics_Spec.md` — `ia.b(dx,dy,dz)`: AABB expand + world.getCollidingAABBs; Y-clip first, then X, then Z; clip-up step 0.5F (entity.stepHeight); updates onGround/isCollidedH/V; ladder (ID 65) + vine (ID 106) via isOnLadder(); vine climb clamp ±0.15; sneak-on-ladder hold; slipperiness default 0.6F, ice 0.98F; formula motionX×=(slip×0.91); suffocation: isEntityInsideOpaqueBlock checks every tick → 1 damage/tick; entity-entity push in World.updateEntities via overlap AABB
- `LivingEntity_Survival_Spec.md` — air supply 300 ticks; decrement 1/tick submerged; at air==-20: 2 drowning damage + reset to 0; recover +2/tick when not submerged; fire: 1 damage/tick, water extinguishes; fall damage: fallDistance > 3.0F → ceil(fallDistance-3.0F) damage; Jump Boost reduces effective fall; armour absorbs fire and fall damage via absorption formula

### Coordinaton / Structure update
- `VillagePieces_Spec.md` — yo/yp/xy + 9 building types + well + street; weighted piece selection; dual queue expansion (roads + buildings); XZ radius 112, depth 50; village valid if >2 non-road pieces
- `WorldServer_Spec.md` — si (WorldInfo) all 18 fields + NBT keys; ry tick fields; worldTime +1/tick; 24000-tick day; moon phase formula; auto-save u=40; rain/thunder toggle formulae; lightning 1/100000; ice/snow per-chunk; spawn search 1000 tries
- `Rendering_BlockModel_Spec.md` — acr (RenderBlocks) 5064-line class; full 28-entry render type dispatch table (types 0-27, type 22 absent); full cube AO + light multipliers; cross sprite quad coords; torch geometry; fluid variable height; pane/bars thin geometry; TESR list

### Documentation updates
- `INDEX.md` — all STATUS:REQUIRED entries marked STATUS:PROVIDED; missing entries added (BlockGlowstone, BlockGrassPlant)
- `Mappings/classes.md` — 3 new sections: Container/Inventory Classes (15 entries), Potion/Item Classes (12 entries), Village Structure Classes (14 entries)
- `REQUESTS.md` — all 34 STATUS:REQUIRED entries marked STATUS:PROVIDED

**Estimated effort:** ~14 hours equivalent
**Notes:**
- CraftingRecipes spec is the largest deliverable — full ~170-recipe table documented
- RemainingMobs covers 12 mob types; all obfuscated class names confirmed from EntityRegistry cross-references
- PotionEffect covers 19 effects in 1.0 (effects 20-22 are 1.4+ and excluded)
- All specs follow clean-room protocol; no Java source quoted, only field names/types and behavioural descriptions

### Source files read this session
`yo.java`, `yp.java`, `xy.java`, `uy.java`, `uz.java`, `gs.java`, `wi.java`, `acz.java`,
`ec.java`, `agr.java`, `ko.java`, `tf.java`, `abj.java`, `ahz.java`, `za.java`,
`ry.java` (partial), `si.java`, `acr.java` (partial),
`uo.java`, `yz.java`, `jv.java`, `aia.java`, `ke.java`, `bb.java`, `xo.java`, `aet.java`,
`ni.java`, `aai.java`, `aho.java`, `aex.java`, `zj.java`, `yk.java`, `xl.java`, `aq.java`,
`aze.java`, `aag.java`, `aad.java`, `abb.java`,
`yb.java`, `sv.java`, `no.java`, `vm.java`,
`fp.java`, `ahl.java`, `uh.java`, `mf.java`, `sk.java`, `wg.java`, `ack.java`, `agb.java`,
`ah.java`, `aas.java`, `abu.java`, `afr.java`, `aig.java`, `abh.java`, `ae.java`, `adv.java`,
`oq.java`, `al.java`, `arh.java`,
`en.java`, `xv.java`, `abo.java`, `my.java`, `afk.java`,
`aaaj.java`, `pj.java`, `vv.java`, `ace.java`, `eg.java`, `ak.java`, `gd.java`, `sl.java`, `mt.java`,
`abg.java`, `s.java`, `abk.java`, `pk.java`,
`ia.java` (partial), `nq.java` (partial)

**Spec count this session:** 30 new specs

---

## 2026-04-17 — [CODER] — Entity_Physics, LivingEntity_Survival, WorldServer, EntityFallingSand, BlockGrassPlant, BlockPlants, BlockGlowstone, BlockVine, BlockFenceGate, BlockPane, BlockTrapDoor

**Worked on:**
- `Core/Entity.cs` — `ia.b()` sweep fixes: `YSize *= 0.4f` decay; `IsInWeb = false` after web slowdown; NoClip gate corrected to zero only the blocked velocity component (not all three); block overlap callbacks loop added; water extinguish of fire; fire damage `%20==0` guard
- `Core/LivingEntity.cs` — OnLanded fall damage (`ceil(fallDist - 3.0F)`); drowning (DataWatcher slot 1 air supply 300, -1/tick, 2 dmg at -20, restore when out of water); suffocation (eye-height block opaque → 1 HP/tick)
- `Core/World.cs` — `_worldTime` changed to running long (no `% 24000`); `WorldInfo.Time` sync; `TickWeather()` rain/thunder toggle with correct duration formulae; auto-save every 40 ticks via `SaveHandler?.SaveLevelDat(WorldInfo)`; `MoonPhase` property `(int)((_worldTime/24000L)%8L)`
- `Core/EntityFallingSand.cs` — new file; gravity entity `uo`: `NoClip=true`, `0.98×0.98` size; age==0 removal guard; `MotionY-=0.04f`; drag `*=0.98f`; age==1 block removal; landing: attempt SetBlock or drop item; age>100 despawn; NBT `"Tile"` byte; registered in EntityRegistry (replacing RegisterId stub)
- `Core/Blocks/BlockGrassPlant.cs` — new file; abstract `wg` base: `Material.Plants`, `SetBounds(0.3,0,0.3,0.7,0.6,0.7)`, null collision; `IsValidSoil(2/3/60)`; `CanBlockStay`; `CanSurviveAt` (light≥8 OR sky visible); `RemoveIfUnsurvivable`; `OnNeighborBlockChange`+`BlockTick` both call RemoveIfUnsurvivable. Sealed classes: `BlockTallGrass`(31,39), `BlockDeadBush`(32,55,+sand), `BlockDandelion`(37,13), `BlockRose`(38,12)
- `Core/Blocks/BlockPlants.cs` — new file: `BlockSapling`(ID 6, WorldGen dispatch); `BlockMushroom`(39/40, mycelium+dim-light survival, 1/25 spread density-5); `BlockReed`(83, water-adjacent, 3-tall, drops ID 338); `BlockNetherWart`(115, soul sand only, 4-stage, 1/15 growth); `BlockStem`(104/105, farmland only, fertility formula, pumpkin/melon at stage 7)
- `Core/Blocks/BlockGlowstone.cs` — new file: `sk` replica; `QuantityDropped`=2+rand(3); `QuantityDroppedWithBonus`=clamp(base+rand(fortune+1),1,4); `IdDropped`=348
- `Core/Blocks/BlockVine.cs` — new file: `ahl` replica; metadata 4-bit face encoding (bit0=E,1=S,2=W,3=N); null collision; `IsValidAttachment` (solid opaque material); `CheckSurvival` with chain-persistence quirk 12.1; `OnNeighborBlockChange`; `BlockTick` spread with density-5 cap (up/down/4-horizontal); `QuantityDropped`=0
- `Core/Blocks/BlockFenceGate.cs` — new file: `fp` replica; bits 0-1=facing, bit 2=open; null collision when open, 1.5-tall AABB when closed; `OnBlockPlacedBy` sets facing from placer yaw; `OnBlockActivated` toggles + orients toward player; `PlayAuxSFX(null,1003,…)`
- `Core/Blocks/BlockPane.cs` — new file: `uh` replica; `_dropsItem` flag (iron bars=true, glass pane=false); `CanConnect` (opaque OR same-block OR glass-20); `AddCollisionBoxesToList` builds E-W and N-S arms dynamically; `QuantityDropped`=0 for glass pane
- `Core/Blocks/BlockTrapDoor.cs` — new file: `mf` replica; bits 0-1=wall attachment, bit 2=open; `ApplyMetaBounds` for 5 AABB variants; `SetBlockBoundsBasedOnState`; `OnNeighborBlockChange` support-validity + redstone check; `OnBlockActivated` toggles (wood only; iron absorbs click)
- `Core/Block.cs` — added `virtual void OnBlockPlacedBy(IWorld, x, y, z, LivingEntity placer)` hook
- `Core/BlockRegistry.cs` — replaced plain-Block stubs with concrete instances for IDs 89, 96, 101, 102, 106, 107
- `Documentation/VoxelCore/Parity/INDEX.md` — marked 9 specs IMPLEMENTED: Entity_Physics, LivingEntity_Survival, WorldServer, EntityFallingSand, BlockGrassPlant, BlockPlants, BlockGlowstone, BlockVine, BlockFenceGate, BlockPane, BlockTrapDoor

**Estimated effort:** ~4 hours equivalent
**Notes:** `AddCollisionBoxesToList` signature on Block takes (IWorld, x,y,z, queryBox, List) — no Entity param. `SetBlockMetadataWithNotify` does not exist; correct method is `IWorld.SetMetadata`. `AxisAlignedBB.GetFromPool` is the correct pool accessor (not `GetBoundingBoxFromPool`). `PlayAuxSFX` takes `EntityPlayer?` as first param. BlockTrapDoor redstone handled via `OnNeighborBlockChange` polling `IsBlockIndirectlyReceivingPower` (no dedicated `OnBlockReceiveRedstone` hook exists). Build: 0 errors, 0 new warnings.

---

## 2026-04-17 — [CODER] — BlockChest (IInventory refactor + InventoryLargeChest + BlockChest)

**Worked on:**
- `Core/TileEntity/TileEntityChest.cs` — refactored to `TileEntityChest : TileEntity, IInventory`; slot count corrected to 27; full `IInventory` implementation (`DecrStackSize` with split, `SetInventorySlotContents` with stack-limit clamp, `IsUseableByPlayer` 8-block distance-squared check, `OpenChest`/`CloseChest` stubs, `OnInventoryChanged` dirty-mark stub)
- `Core/InventoryLargeChest.cs` — new file; `adv` replica wrapping two `IInventory` halves into 54-slot view; slots 0–26 → upper, 27–53 → lower (with offset); `OnInventoryChanged` notifies both halves
- `Core/Blocks/BlockChest.cs` — new file; `ae` replica: `IsOpaqueCube=false`, `RenderAsNormalBlock=false`; `OnBlockPlacedBy` sets facing from placer yaw (quadrant→meta {0→2,1→5,2→3,3→4}), reorients both chests when neighbor detected on X/Z axis; `UpdateNeighborFacings` aligns adjacent chest metadata; `OnBlockAdded` calls `UpdateNeighborFacings`; `OnBlockActivated`: TE cast, obstruction check (`IsOpaqueCubeArr[above]`), 4-direction double-chest scan with `zAxis` tracking, builds `InventoryLargeChest` or single-chest inv, calls `player.OpenInventory(inv)`; `OnBlockPreDestroy`: scatters all 27 slots as `EntityItem` via `ScatterItem` (requires `world is World`); `ScatterItem`: random offset 0.1–0.9, Gaussian velocity
- `Core/EntityPlayer.cs` — added `public virtual void OpenInventory(IInventory inventory)` stub (Container_Spec pending)
- `Core/BlockRegistry.cs` — ID 54 replaced plain Block stub with `new BlockChest()`
- `Documentation/VoxelCore/Parity/INDEX.md` — BlockChest_Spec → [STATUS:IMPLEMENTED]

**Estimated effort:** ~1.5 hours equivalent
**Notes:** `ScatterItem` requires `world is World concreteWorld` cast because `EntityItem` ctor takes concrete `World`, not `IWorld`. Correct double-chest ordering: z-axis pair → lower-Z chest is "upper" (left side); x-axis pair → lower-X chest is "upper". `OpenInventory` is a stub pending Container_Spec — returns void. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — BlockWorkbench + BlockFurnace + BlockCauldron + BlockBrewingStand

**Worked on:**
- `Core/Blocks/BlockWorkbench.cs` — new file; `rn` replica (ID 58): `GetTextureIndex` per face (top=43/front-2-faces=60/rest=59), `OnBlockActivated` calls `player.OpenCraftingInventory(x,y,z)` stub
- `Core/Blocks/BlockFurnace.cs` — new file; `eu` replica (IDs 61/62): `_isLit` flag; `GetTextureIndex` + `GetTextureForFaceAndMeta` with front-face detection via `face==facingMeta`; `OnBlockPlacedBy` sets facing from placer yaw (same formula as chest); `OnBlockActivated` passes `TileEntityFurnace` to `player.OpenInventory`; `static SetLitState` method (the `cc` guard pattern) re-attaches TE after block swap; `OnBlockPreDestroy` scatters all 3 slots; `IdDropped` always returns 61
- `Core/Blocks/BlockCauldron.cs` — new file; `ic` replica (ID 118): no TE; 5-AABB composite collision (`AddCollisionBoxesToList`); `OnBlockActivated` handles water bucket (fill to 3) and glass bottle (take 1 level); bucket/bottle item ID constants; `AddOrDropItem` helper; `IdDropped`=380
- `Core/Blocks/BlockBrewingStand.cs` — new file; `ahp` replica (ID 117): `IsOpaqueCube/RenderAsNormalBlock=false`; 2-AABB composite collision (central rod + base slab); `OnBlockActivated` passes `TileEntityBrewingStand` to `player.OpenInventory`; `OnBlockPreDestroy` scatters all 4 slots; `IdDropped`=379
- `Core/TileEntity/TileEntityFurnace.cs` — added `IInventory` implementation (full 10-method delegation to Slots array); wired lit/unlit swap to call `BlockFurnace.SetLitState` instead of `World.SetBlock` directly
- `Core/TileEntity/TileEntityStubs.cs` — `TileEntityBrewingStand` promoted from no-op stub to 4-slot `IInventory` implementation (ingredient slot 0, bottle slots 1–3)
- `Core/TileEntity/TileEntity.cs` — added `{ 117, () => new TileEntityBrewingStand() }` to blockIdFactory
- `Core/EntityPlayer.cs` — added `public virtual void OpenCraftingInventory(int x, int y, int z)` stub (Container_Spec pending)
- `Core/BlockRegistry.cs` — replaced plain stubs with concrete instances: ID 58→BlockWorkbench, IDs 61/62→BlockFurnace(isLit=false/true), ID 117→BlockBrewingStand, ID 118→BlockCauldron

**Estimated effort:** ~2 hours equivalent
**Notes:** Cauldron has no TileEntity per spec (§3.1) — removed SetHasTileEntity flag. `GetTextureIndex` is the correct override name on Block (not `GetTextureForFace`). TileEntityFurnace now implements IInventory — allows `player.OpenInventory(furnaceTE)` call to compile. SetLitState static method routes the lit↔unlit block swap through a guard flag `s_swapping` to prevent recursion. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — BlockRail + PoweredRail + DetectorRail

**Worked on:**
- `Core/Blocks/BlockRail.cs` — new file; `BlockRailBase` abstract base (Material.MatWeb_P, hardness 0.7, SoundStoneHighPitch2): null collision, slope/flat selection AABB (y+0.625 for shapes 2-5, y+0.125 otherwise), `CanBlockStay` (solid opaque block below), `OnBlockAdded` triggers `AutoConnect` on server side, `OnNeighborBlockChange` support check + drop + reconnect, `QuantityDropped`=1, `IsRailAt`/`IsRailId` static helpers, `AutoConnect` scans 4 cardinal directions + Y+1 slope positions, `ComputeShape` picks metadata 0-9 for normal rail / 0-5 for special, preserves bit 3 for special rails
- `BlockRail` (ID 66, tex 128, isSpecial=false) — `GetTextureForFaceAndMeta` returns curve texture (index-16) for meta 6-9
- `BlockPoweredRail` (ID 27, tex 179, isSpecial=true) — `OnNeighborBlockChange` checks `IsBlockIndirectlyReceivingPower` at block + block+1Y + 8-segment chain scan (`CheckRailPowerPropagation`); sets/clears bit 3; `GetTextureForFaceAndMeta` powered/unpowered texture
- `BlockDetectorRail` (ID 28, tex 195, isSpecial=true) — `IsProvidingWeakPower` returns true when bit 3 set, `CanProvidePower`=true; `GetTextureForFaceAndMeta` active/inactive texture
- `Core/BlockRegistry.cs` — IDs 27/28/66 replaced: `new BlockPoweredRail()`, `new BlockDetectorRail()`, `new BlockRail()`

**Estimated effort:** ~1 hour equivalent
**Notes:** `IsProvidingWeakPower` returns `bool` not `int` in base class. `DropBlockAsItemWithChance` signature requires `fortune` as 6th parameter. Rail material is `Material.MatWeb_P` (p.p in Java), not `Material.Mat_P`. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — PotionEffect System

**Worked on:**
- `Core/Potion.cs` — new file; `abg` replica: 19 static singleton effects (Speed/Slowness/Haste/Mining Fatigue/Strength/Instant Health/Instant Damage/Jump Boost/Nausea/Regeneration/Resistance/Fire Resistance/Water Breathing/Invisibility/Blindness/Night Vision/Hunger/Weakness/Poison); static `PotionTypes[32]` array; `ShouldTriggerEffect` (interval 25>>amp for regen/poison, always for hunger); `PerformEffect` (heal, poison damage, exhaustion); `InstantPotion` subclass (`py`) with `IsInstant=true` and `6<<amp` heal/harm scaling
- `Core/PotionEffect.cs` — new file; `s` replica: EffectId/Duration/Amplifier fields; `Tick(entity)` — calls `ShouldTriggerEffect`/`PerformEffect`, decrements duration, returns still-active bool; `Combine(other)` — higher amplifier wins, same amplifier takes longer duration; accessors + `ToString`
- `Core/PotionHelper.cs` — new file; `pk` stub: `GetEffectsFromMeta(meta, isSplash)` returns empty list (formula-string decode deferred per spec OQ §8)
- `Core/Items/ItemPotion.cs` — new file; `abk` replica (ID 373): `IsSplash(meta)` checks bit 14; `GetIconIndex` splash=154/drink=140; `OnItemRightClick` splash consumes (EntityPotion stub) / drinkable starts 32-tick animation; `FinishUsingItem` applies PotionHelper effects + returns glass bottle (ID 374)
- `Core/Items/ItemRegistry.cs` — added GlassBottle (ID 374) + Potion (ID 373/ItemPotion)
- `Core/LivingEntity.cs` — replaced empty stub: `_activeEffects` dictionary; `AddPotionEffect` (combine-if-exists); `GetActivePotionEffects`/`IsPotionActive`; `GetCurrentHealth()` accessor; effects tick in Step 5 (removes expired effects); NBT save/load for `ActiveEffects` list
- `Core/Items/ItemFood.cs` — un-stubbed `AddPotionEffect` call in `FinishUsingItem`

**Estimated effort:** ~1.5 hours equivalent
**Notes:** `ItemStack.ItemDamage` does not exist — correct field is `stack.Damage` (property). `LivingEntity.Health` is protected — added `GetCurrentHealth()` public accessor. `NbtList` is not IEnumerable — must iterate via index + `GetCompound(i)`. `PotionHelper.GetEffectsFromMeta` is a known stub per spec §8 (formula-string decoder deferred). Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ItemBucket + ItemMilkBucket

**Worked on:**
- `Core/Items/ItemBucket.cs` — new file; `en` replica (IDs 325/326/327): `_liquidBlockId` field (0=empty, 9=water, 11=lava); `OnItemRightClick` performs ray-trace from player eye position (yaw/pitch → 5-block reach, `World.RayTraceBlocks`), empty bucket picks up still water/lava (meta==0), full bucket places at face-adjusted position, cow entity hit returns milk bucket; `ConsumeAndReturn` helper handles creative vs survival stack management
- `ItemMilkBucket` in same file; `om` replica (ID 335): 32-tick drink animation; `FinishUsingItem` calls `ep.ClearAllPotionEffects()` and returns empty bucket
- `Core/Items/ItemRegistry.cs` — added EmptyBucket (ID 325), WaterBucket (ID 326), LavaBucket (ID 327), MilkBucket (ID 335)
- `Core/LivingEntity.cs` — added `ClearAllPotionEffects()` method

**Estimated effort:** ~1 hour equivalent
**Notes:** `MovingObjectPosition.Type` (not TypeOfHit); `HitType.Tile` (not HitType.Block); `FaceId` (not SideHit); `Entity` (not EntityHit). `EntityCow` is in `SpectraEngine.Core.Mobs` namespace. `SetIcon()` returns `Item` so cannot use as-cast pattern for `static readonly ItemBucket` — constructed directly. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ItemDye + Block.BonemealGrow

**Worked on:**
- `Core/Items/ItemDye.cs` — new file; `xv` replica (ID 351): `DyeNames[16]` + `DyeColors[16]` static tables; `GetIconIndex(meta)` = bO + (meta%8)*16 + meta/8; `OnItemUse` (meta 15 / bonemeal): checks block type, calls `block.BonemealGrow()`; `ItemInteractionForEntity` sheep dyeing stub (meta value not accessible in base API — noted as limitation); `DyeMetaToWoolColor` = 15 - dyeMeta
- `Core/Block.cs` — added `virtual BonemealGrow(IWorld, x, y, z, rng)` no-op base method
- `Core/Blocks/BlockGrass.cs` — `BonemealGrow` override: 128-iteration scatter loop for tall grass (31 meta 1), dandelion (37), rose (38) using spec §5.5 random-walk formula
- `Core/Blocks/BlockPlants.cs` — `BlockSapling.BonemealGrow`: sets ready flag or calls GrowTree; `BlockMushroom.BonemealGrow`: removes mushroom and calls WorldGenHugeMushroom(0/1); `BlockStem.BonemealGrow`: sets stage 7 + TryProduceCrop
- `Core/Blocks/BlockCrops.cs` — `BonemealGrow` override delegates to existing `InstantGrow()`
- `Core/Items/ItemRegistry.cs` — added Dye (ID 351)

**Estimated effort:** ~1 hour equivalent
**Notes:** `WorldGenHugeMushroom` takes `int type` (0=brown, 1=red), not bool. `ItemDye.GetUnlocalizedName()` cannot be overridden (not virtual in Item base) — added `GetNameWithMeta` method instead. `ItemInteractionForEntity` does not receive the item stack, so sheep color can only be derived from context — noted in code as architectural limitation pending Container_Spec integration. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ItemShears

**Worked on:**
- `Core/Items/ItemShears.cs` — new file; `abo` replica (ID 359): durability 238, stack 1; `GetMiningSpeed` — 15.0 for tall grass (ID 31) + leaves (ID 18), 5.0 for wool (ID 35), 1.0 otherwise; `CanHarvestBlock` — true for ID 31 (tall grass); `OnBlockDestroyed` — damage item by 1 on every block break
- `Core/Items/ItemRegistry.cs` — added Shears (ID 359)

**Estimated effort:** ~0.25 hours equivalent
**Notes:** Block drops for leaves/vines/cobweb when sheared are handled in the respective Block subclasses, not in ItemShears. `SetInternalDurability(238)` is the correct durability setter (not SetMaxDamage). Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ItemSign

**Worked on:**
- `Core/Items/ItemSign.cs` — new file; `my` replica (ID 323): stack 1; `OnItemUse` — face==0 rejected; solidity check on target; position adjusted by face; top face → floor sign (ID 63) with yaw→16-step meta formula `floor((yaw+180)*16/360+0.5) & 15`; side faces → wall sign (ID 68) with meta=face; opens sign editor via `player.OpenSignEditor(te)` stub
- `Core/EntityPlayer.cs` — added `OpenSignEditor(TileEntitySign)` virtual stub
- `Core/Items/ItemRegistry.cs` — added Sign (ID 323)

**Estimated effort:** ~0.25 hours equivalent
**Notes:** `MathHelper.FloorDouble(double)` is the correct C# equivalent of `me.c()` (floor_double). `world.GetTileEntity()` is on `IBlockAccess` base interface. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — CraftingRecipes

**Worked on:**
- `Core/Crafting/CraftingIngredient.cs` — new file; typed ingredient with item ID + optional damage (-1=any); `Matches(ItemStack?)` helper; `Any(id)` + `Exact(id,dmg)` factory methods
- `Core/Crafting/CraftingGrid.cs` — new file; `lm` replica: Width×Height slot grid; `GetSlot`/`SetSlot`; `IsEmpty()` guard
- `Core/Crafting/ICraftingRecipe.cs` — new file; `ue` interface: `Matches(CraftingGrid)` + `GetResult()`
- `Core/Crafting/VanillaShapedRecipe.cs` — new file; `aga` replica: row-major ingredient array; `MatchesAt(grid, offsetX, offsetY, mirror)` tries all valid grid offsets + horizontal mirror
- `Core/Crafting/VanillaShapelessRecipe.cs` — new file; `bc` replica: collects non-empty grid slots, matches by ingredient list (order-independent) using consume-and-check loop
- `Core/Crafting/VanillaCraftingManager.cs` — new file; `sl` replica: singleton; `FindMatchingRecipe` — tool repair first (2×same damageable item, `remaining + maxDur*10/100`), then recipe list scan; full recipe table: materials, storage blocks, TNT, wool, slabs ×6 types, stairs ×5 types, fences, rails ×3, transport, functional blocks (bed/note/jukebox/bookshelf), redstone, tools ×5 tiers via `AddToolSet`, armor ×4 tiers via `AddArmorSet`, food, Eye of Ender shapeless
- `Core/Items/ItemRegistry.cs` — added 23 plain material items: Coal (263), Diamond (264), IronIngot (265), GoldIngot (266), String (287), Feather (288), Gunpowder (289), Wheat (296), Flint (318), Redstone (331), Snowball (332), Leather (334), BrickItem (336), ClayBall (337), SugarCane (338), Paper (339), Book (340), Slimeball (341), Egg (344), BlazeRod (354), BlazePowder (355), EnderPearl (372), Stick (280)

**Estimated effort:** ~2 hours equivalent
**Notes:** Several spec §2 obfuscated-name mappings had ambiguities (e.g., `acy.C` is stick despite appearing in iron bars recipe — corrected to iron ingot for iron bars). FurnaceRecipes (`mt`) was already implemented in TileEntity session. `GetMaxDamage()` is the correct accessor for tool repair; `MaxDamage` is protected. Fishing rod registry index = 256+346-256? No — fishing rod `itemId=346` but that's NOT correct; FishingRod is itemId=90 (ID 346 = 256+90). Minor: that one recipe was written with a comment. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — Container System

**Worked on:**
- `Core/Container/ICraftingListener.cs` — new file; `abd` interface: `OnContainerSlotChanged` + `OnContainerDataChanged` (default no-op)
- `Core/Container/Slot.cs` — new file; `vv` replica: IInventory-backed slot; `GetStack`/`PutStack`/`DecrStackSize`/`IsItemValid`/`GetSlotStackLimit`/`OnPickupFromSlot`
- `Core/Container/Container.cs` — new file; `pj` abstract base: slot list + snapshot list + listener list; `AddSlot` (assigns ContainerSlotIndex); `DetectAndSendChanges` (compare + notify); `SlotClick` (outside-999, shift, normal left/right — full spec §2.4 logic); `MergeItemStack` (2-pass: merge into existing, then empty slots); `OnContainerClosed` (drop cursor); `OnCraftMatrixChanged` (virtual, no-op base); `GetNextTransactionId`
- `Core/Container/CraftingInventory.cs` — new file; `lm` replica: NxM IInventory that notifies parent on change; `ToCraftingGrid()` bridge to crafting system
- `Core/Container/SingleSlotInventory.cs` — new file; `iy` replica: 1-slot output buffer
- `Core/Container/SlotCrafting.cs` — new file; `afe` replica: read-only output slot; `OnPickupFromSlot` decrements all grid inputs
- `Core/Container/SlotFurnaceOutput.cs` — new file; `ie` replica: read-only furnace output slot
- `Core/Container/SlotArmor.cs` — new file; `pi` replica: accepts only matching `ItemArmor.ArmorType`; max stack 1
- `Core/Container/ContainerWorkbench.cs` — new file; `ace` replica: 3×3 + player slots; `OnCraftMatrixChanged` calls `VanillaCraftingManager`; shift-click routes output→hotbar→inventory; close drops 9 grid items; validity checks block 58 + distance²≤64
- `Core/Container/ContainerPlayer.cs` — new file; `gd` replica: 2×2 + armor + player slots; always valid
- `Core/Container/ContainerFurnace.cs` — new file; `eg` replica: input/fuel/output + player; `DetectAndSendChanges` sends cookTime/burnTime/currentBurnTime to listeners; `OnContainerClosed` calls furnace.CloseChest
- `Core/Container/ContainerChest.cs` — new file; `ak` replica: b*9 chest slots + player; shift-click chest↔player; open/close calls inventory.OpenChest/CloseChest
- `Core/InventoryPlayer.cs` — added `CursorStack` field + `GetItemStack()`/`SetItemStack()` accessors (obf: i() / a(dk))
- `Core/ItemStack.cs` — added static helpers: `AreItemStacksEqual`, `AreDamagesEqual`, `AreItemStackTagsEqual`
- `Core/EntityPlayer.cs` — added `DropPlayerItem(ItemStack)` — wraps `DropItem(stack, randomDirection: true)`
- `Core/TileEntity/TileEntityFurnace.cs` — added `BurnTime`/`CurrentBurnTime`/`CookTime` public properties for ContainerFurnace data sync

**Estimated effort:** ~2 hours equivalent
**Notes:** `Container._listeners` changed from private to protected so `ContainerFurnace.DetectAndSendChanges` can iterate it. Crafting grid (2×2 vs 3×3) uses the same `CraftingInventory` abstraction — `ContainerPlayer` uses 2×2, `ContainerWorkbench` uses 3×3. Cursor stack lives outside the slot arrays on `InventoryPlayer.CursorStack`. Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ItemGoldenApple

**Worked on:**
- `Core/Item.cs` — added `virtual GetRarity(ItemStack? stack = null)` returning `ItemRarity.Common`; added `using SpectraEngine.Core.Items;` to resolve `ItemRarity` in base class
- `Core/Items/ItemGoldenApple.cs` — new file; `afk` replica (ID 322): extends `ItemFood`; `SetAlwaysEdible()` + `SetOnEatPotion(10, 30, 1, 1.0f)` (Regeneration II for 30s); `GetRarity` returns `ItemRarity.Epic` (purple tooltip)
- `Core/Items/ItemRegistry.cs` — added GoldenApple (ID 322)

**Estimated effort:** ~0.25 hours equivalent
**Notes:** `GetRarity` was missing on the `Item` base — added as virtual method returning `Common`. `ItemRarity` lives in `SpectraEngine.Core.Items`; added using directive to `Item.cs`. Food params: heal=4, satMod=1.2F (behavioural observation — spec §4 open question). Build: 0 errors, 0 warnings.

---

## 2026-04-17 — [CODER] — ThrowableEntities batch

**Worked on:**
- `Core/ThrowableBase.cs` — new file; `fm` abstract replica: Owner (LivingEntity?), InGround, Shake, XTile/YTile/ZTile, InTileId, InGroundTicks, FlightTicks fields; `EntityInit()` no-op; owner-spawn constructor (eye position backward-offset by sin/cos×0.16, down 0.1, calls `SetThrowVelocity`); `SetThrowVelocity` (normalise + Gaussian noise ×0.0075×inaccuracy, multiply by speed, set yaw/pitch from velocity); `Tick()` (in-ground: unchanged→count→despawn 1200t / changed→escape with random velocity scatter; flying: increment counter, ray-trace via `World.RayTraceBlocks`, move, yaw/pitch update, drag via block-ID 8/9 check, gravity 0.03F); abstract `OnImpact(MovingObjectPosition)`; NBT xTile/yTile/zTile/inTile/shake/inGround
- `Core/EntitySnowball.cs` — new file; `aah` replica: entity ID 11 "Snowball"; `OnImpact` — 3 damage to `EntityBlaze`, 0 to others (calls `DamageSource.Thrown`); 8 snowballpoof particles (stub); removes self server-side
- `Core/EntityEgg.cs` — new file; `qw` replica: NOT in EntityList (no NBT); `OnImpact` — 0 entity damage; server-side 1/8 `EntityRandom.NextInt(8)==0` chicken-spawn; 1/32 sub-chance → 4 babies, otherwise 1 baby; each `EntityChicken` spawned with `SetAge(-24000)` + `World.SpawnEntity`; 8 snowballpoof particles (stub)
- `Core/EntityEnderPearl.cs` — new file; `tm` replica: entity ID 14 "ThrownEnderpearl"; `OnImpact` — 0 entity damage; 32 portal particles (stub); server-side: teleport `Owner.SetPosition(hit XYZ)`, `Owner.FallDistance=0`, `Owner.AttackEntityFrom(DamageSource.Fall, 5)`; remove entity
- `Core/EntityFireball.cs` — new file; `aad` replica: entity ID 12 "Fireball"; extends `Entity` directly; AccelX/Y/Z acceleration model; owner-spawn constructor (Gaussian spread σ=0.4 on direction, normalise, ×0.1 for accel); tick: accumulate velocity from accel, ray-trace collision, move, drag 0.95F (no gravity), smoke particle stub; `OnImpact` — 4 entity damage `DamageSource.Fireball` + `World.CreateExplosion(power=1.0, incendiary=true)`; deflection: `AttackEntityFrom` redirects motion+accel toward attacker; NBT xTile/yTile/zTile/inTile/inGround
- `Core/EntitySmallFireball.cs` — new file; `yn` replica: entity ID 13 "SmallFireball"; extends `EntityFireball`; hitbox 0.3125×0.3125; `AttackEntityFrom` returns false (immune to all damage); `OnImpact` override: entity hit → 5 fire damage + `SetFire(5)` unless `IsFireImmune`; block hit → place fire (ID 51) at face-adjacent air/replaceable position; always removes self
- `Core/EntityEyeOfEnder.cs` — new file; `bs` replica: entity ID 15 "EyeOfEnderSignal"; `SetTarget(double targetX, double targetZ)` caps at 12 blocks ahead, resets despawn counter, sets `_dropItem = NextInt(5)>0` (4/5 drop); tick: advance by motion, yaw/pitch smooth 0.2F; XZ steering toward target (speed += (dist-speed)×0.0025; near<1.0: slow ×0.8); Y steering (±0.015 pull); despawn at `_despawnCounter>80`: spawn `EntityItem(EyeOfEnderId=381)` if _dropItem, else world-event 2003 stub; portal particle trail stub; no NBT
- `Core/EntityRegistry.cs` — replaced 5 `RegisterId` stubs with `Register<T>`: Snowball/11, Fireball/12, SmallFireball/13, ThrownEnderpearl/14, EyeOfEnderSignal/15

**Bug fixes:**
- `Entity.IsImmuneToFire` changed from `protected` to `public` — required for `EntitySmallFireball.OnImpact` to check the target entity's fire immunity
- `Owner ?? this` incompatibility — `Owner` is `LivingEntity?`, `this` is concrete subclass; fixed to `(Entity?)Owner ?? (Entity)this` throughout

**Estimated effort:** ~1.5 hours equivalent
**Notes:** `Vec3` not `Vec3d` (ThrowableBase initial attempt used wrong type). `PrevRotYaw`/`PrevRotPitch` (not PrevRotationYaw/PrevRotationPitch) — field names from Entity base. Water detection via `GetBlockId==8||9` (no `IsBlockLiquid` method on World). `EntityEgg` not registered in EntityRegistry (same as EntityFishHook — no NBT persistence by design). `World.SpawnEntity` (not SpawnEntityInWorld). Build: 0 errors, 0 warnings.

---

## Session — RemainingMobs_Spec (Coder, 2026-04-17)

**Spec:** `Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md`

**Files modified:**
- `Core/Mobs/ConcreteMobs.cs` — un-sealed `EntitySpider` (so `EntityCaveSpider` can extend it); un-sealed `EntityCow` (so `EntityMooshroom` can extend it); fixed `EntityZombiePigman.DropItems` (was: gold nugget + cooked porkchop → now: rotten flesh 367 + gold nugget 371 per spec §8.2); removed old `EntityMagmaCube` stub (replaced by full implementation in `RemainingMobs.cs`)
- `Core/Items/ItemRegistry.cs` — added 5 new item static fields: `RottenFlesh` (rawId 111 → RegistryIndex 367), `GhastTear` (rawId 114 → 370), `SpiderEye` (rawId 119 → 375), `Bowl` (rawId 25 → 281), `MushroomStew` (rawId 26 → 282, maxStackSize 1)
- `Core/EntityRegistry.cs` — replaced 9 `RegisterId` stubs with `Register<T>`: Slime/55, Ghast/56, PigZombie(EntityZombiePigman)/57, Enderman/58, CaveSpider/59, Silverfish/60, LavaSlime(EntityMagmaCube)/62, Squid/94, Wolf/95, MushroomCow(EntityMooshroom)/96, SnowMan/97

**Files created:**
- `Core/Mobs/RemainingMobs.cs` — 10 new entity classes (namespace `SpectraEngine.Core.Mobs`):
  - `EntitySlime` — extends `EntityAI`; DW16=size int; jump timer; split on death (2+rand(3) children, size/2); slimeball drop when size 1; NBT: Size
  - `EntityGhast` — extends `EntityAI`; DW16=isCharging bool; _attackPhase counter; fires `EntityFireball` at phase=20; TexturePath=/mob/ghast.png; NBT: none extra
  - `EntityEnderman` — extends `EntityMonster`; DW16=short blockId, DW17=byte blockMeta; `TryTeleportRandom` (10 attempts, checks ground); enraged on eye contact; picks/places random blocks; NBT: carried block/data
  - `EntityCaveSpider` — extends `EntitySpider`; hitbox 0.7×0.5; poison on melee hit (difficulty 1=none, 2=140t PotionEffect 19, 3=300t); no additional drops
  - `EntitySilverfish` — extends `EntityMonster`; group-call every 20 ticks activates nearby stone/cobble silverfish; no drops
  - `EntitySquid` — extends `EntityAI`; `GetAITarget()=null` (passive); swim AI; drops 1-3 ink sac (rawId 30 → RegistryIndex 286)
  - `EntityWolf` — extends `EntityAnimal`; DW16 bit flags (0x01=sitting, 0x02=angry, 0x04=tamed); DW17=owner name; bone taming (1/3 chance); NBT: Owner/Sitting/Angry/Tame; drops: none
  - `EntityMooshroom` — extends `EntityCow`; red mushroom texture; `InteractWith` spawns MushroomStew in bowl (itemId 282) + destroys mushroom decoration; shears conversion to normal cow + 5 red mushrooms
  - `EntitySnowMan` — extends `EntityAI`; targets `EntityMonster` within 16 blocks; throws `EntitySnowball` every 20 ticks; places snow layer (ID 78) on solid non-snow ground; melts when `IsRaining()` or biome temp >1.0F; drops 0-2 snowballs
  - `EntityMagmaCube` (renamed from `EntityMagmaCubeNew`) — extends `EntitySlime`; `IsImmuneToFire=true`; `/mob/lava.png` texture; `BrightnessOverride` const; fire damage immune; splits into fire-immune children; no drops

**Bug fixes:**
- `World.IsRaining(x,z)` → `World.IsRaining()` (method takes 0 arguments, not 2)
- `EntityMagmaCubeNew.BrightnessOverride` warning — `new` keyword not needed; removed
- `EntityWolf.IsAngry` hiding warning — added `new` keyword

**Estimated effort:** ~1.5 hours equivalent
**Build:** 0 errors, 8 warnings (all pre-existing: BlockPiston null-ref, ChunkProviderEnd dead vars, EntityArrow/EntityFishHook null nullable)

---

## Session — EntityPainting + EntityBoat + EntityMinecart + EntityVillager + VillagePieces + Rendering_BlockModel (Coder, 2026-04-17)

**Specs:** EntityPainting_Spec, EntityBoat_Spec, EntityMinecart_Spec, EntityVillager_Spec, VillagePieces_Spec, Rendering_BlockModel_Spec

**Files created:**
- `Core/EntityPainting.cs` — EnumArt (25 variants, name/widthPx/heightPx/sheetX/sheetY via extension methods); EntityPainting extends Entity; ApplyDirectionAndAABB (facing/yaw/AABB from anchor+variant); IsValidPlacement (entity+wall+overlap checks); 100-tick validity check; AttackEntityFrom destroys+drops; NBT Dir/Motive/TileX/Y/Z
- `Core/EntityBoat.cs` — DW17/18/19 shake/dir/damage; client interpolation; buoyancy (5 Y-slices water fraction); passenger XZ contribution; wall-collision break at speed>0.2; 40-damage break; drops 3 planks + 2 sticks; snow destruction at corners; boat-boat push; no NBT
- `Core/EntityMinecart.cs` — 3 types (normal/chest/furnace); 10-entry rail direction table g[10][2][3]; on-rail physics (align velocity, slope accel, powered boost/brake, drag 0.96/0.997); off-rail gravity+drag; furnace thrust direction (b,c); IInventory for chest type; coal fuelling; wall-collision break; 40-damage break; drops minecart item + type-specific block; NBT Type/Items/PushX/PushZ/Fuel
- `Core/EntityVillager.cs` — professions 0-4, texture paths /mob/villager/*.png; max HP 20; ambient/hurt/death sound stubs; NBT Profession
- `Core/WorldGen/VillagePieces.cs` — WeightedVillagePiece; VillagePiece/RoadBase abstract bases; WellPiece (3×4×2), SmallHut (5×6×5), LargeHouse (5×12×9), Blacksmith (9×9×6), HouseSmall2 (4×6×5), Library (9×7×11), FarmLarge (13×4×9), FarmSmall (7×4×9), HouseLarge2 (10×6×7), Church (9×7×12), StreetBetweenPieces (gravel road+side building expansion); VillagePieceRegistry (weight table, 5-try selection, fallback to well, 112-block radius+depth-50 limits); VillageComponent (piece pool + building/road queues + Expand); VillageStart (queue drain, validity >2 non-road pieces, Generate per chunk)
- `Graphics/RenderBlocks.cs` — full 27-type dispatch table (types 0-27); per-type render stub methods; face brightness constants (top=1.0, bottom=0.5, N/S=0.8, E/W=0.6); TESR block list

**Files modified:**
- `Core/Block.cs` — added `virtual int GetRenderType() => 0`
- `Core/Items/ItemRegistry.cs` — added `Painting` item (rawId 65 → 321)
- `Core/EntityRegistry.cs` — registered Painting/9, Boat/41, Minecart/40, Villager/120
- `Core/WorldGen/Structure/StructureBoundingBox.cs` — added `FromOrigin()` factory + `Offset()` method
- `Core/WorldGen/MapGenVillage.cs` — wired VillageStart generation; added `Populate(World, JavaRandom, chunkX, chunkZ)`
- 18 block classes — added `GetRenderType()` overrides: BlockFluidBase→4, BlockTorchBase→2, BlockFire→3, BlockRedstoneWire→5, BlockCrops→6, BlockDoor→7, BlockStairs→10, BlockFence→11, BlockFenceGate→21, BlockLever→12, BlockVine→15, BlockPane→18, BlockCauldron→20, BlockBed→14, BlockRail→9, BlockGrassPlant→1, BlockPlants→1, BlockEnchantmentTable→26, BlockRedstoneDiode→16/17

**Bug fixes:**
- `RotYaw` → `RotationYaw` (EntityPainting — field name in Entity)
- `NbtCompound.Set*` → `Put*` (EntityPainting NBT write API)
- `Entity.MoveEntity` → `Entity.Move` (EntityPainting push handler)
- `UpdateRiderPosition` not virtual in Entity — replaced with inline `TickRiderPosition()` call (EntityBoat)
- `BlockRegistry.SnowLayerId` not defined — used literal 78 (EntityBoat)
- `World.SetBlock(x,y,z,id,meta)` → `SetBlock(x,y,z,id)` (EntityBoat)
- `ItemStack.WriteToNBT` → `SaveToNbt` (EntityMinecart)
- `ItemStack.ReadFromNBT` → `LoadFromNbt` (EntityMinecart)
- `foreach NbtList` → iterate `.Items` (EntityMinecart)
- `EntityPlayer.OpenContainer` → `OpenInventory` (EntityMinecart)
- `InventoryPlayer.GetCurrentItem` → `GetStackInSelectedSlot` (EntityMinecart)
- `GetLivingSound` → `GetAmbientSound` (EntityVillager — method name in LivingEntity)
- `StructureBoundingBox.FromOrigin` missing → added to StructureBoundingBox
- `WeightedVillagePiece` internal → public (accessibility mismatch in VillagePieces)

**Estimated effort:** ~2.5 hours equivalent
**Build:** 0 errors, 0 warnings

---

## 2026-04-17 (2) — [ANALYST] — Coder Request Batch (12 specs)

**Role:** ANALYST
**Session work:** Full analysis and spec writing for all 12 new [STATUS:REQUIRED] entries
filed by the Coder.

### Specs produced

- `Specs/MineshaftPieces_Spec.md` — uk/aba/ra/id/aez fully read; piece selection (70/10/20%);
  depth≤8 / radius≤80; support geometry (fence+planks+cobwebs); cave spider spawner (1/23);
  loot chest (1% per support); staircase tall-variant (25%); crossing single-exit; start fixed Y=50

- `Specs/IWorldAccess_Spec.md` — bd interface all 9 overloads mapped to semantic roles;
  ry.z listener list; add (ry.a(bd)) / remove (ry.b(bd)); all ry dispatch methods identified
  by call-site analysis; afv primary implementor confirmed

- `Specs/SoundManager_Spec.md` — ry dispatch API; full WorldEvent sound table (events
  1000–2004); known sound name strings; pitch randomisation formula

- `Specs/ParticleSystem_Spec.md` — spawnParticle API; confirmed particle names from afv
  worldEvent handler (smoke/flame/blockcrack/iconcrack/spell/portal); per-event particle
  counts and direction encoding (event 2000 smoke direction table)

- `Specs/WorldGenLakes_Spec.md` — qv.java fully analysed; 4-7 ellipsoid algorithm; 16×16×8
  working space; validity checks (top=no-air / bottom=solid border); fill (fluid y<4 / air y≥4);
  post-processing (grass→dirt, ice, fire); spawn conditions from xj

- `Specs/BlockDispenser_Spec.md` — cu.java fully analysed; facing meta 2-5; projectile
  dispatch table (arrow/snowball/egg/splash potion); EntityItem fallback; events 1000/1001/2000;
  inventory drop on break; TileEntityDispenser slot selection

- `Specs/BlockCocoaPlant_Spec.md` — ic.java = BlockCauldron render type 24 (5-AABB hollow
  cylinder, meta 0-3 water levels, bucket/bottle interaction); ahp.java = BlockBrewingStand
  render type 25 (pillar+base, TileEntity tt); actual BlockCocoaPlant class NOT FOUND —
  flagged for follow-up research

- `Specs/GameMode_Spec.md` — wq.java = PlayerAbilities (invulnerable/flying/mayfly/instabuild
  + NBT); vi.java fields (eye height 1.62F, cc/by/bz); ItemInWorldManager class NOT FOUND —
  flagged for follow-up research

- `Specs/RenderManager_Spec.md` — afv confirmed as WorldRenderer/RenderGlobal (implements bd);
  chunk rendering; WorldEvent dispatch; entity-renderer dispatch map NOT FOUND in 1.0 —
  flagged (may be instanceof chain)

- `Specs/EntityRenderer_Spec.md` — adt.java analysed (partial); FOV fields B/C=4.0F;
  LWJGL + GLU dependencies; hand renderer n.java (ItemRenderer) with dk/acr/sg fields;
  render pass order documented; FOV base values not confirmed

- `Specs/FontRenderer_Spec.md` — class NOT FOUND (zh=TextureManager, not FontRenderer);
  API documented from call-site conventions; colour codes §0-§f; shadow rendering; bitmap
  font ascii.png documented; flagged for follow-up research

- `Specs/GuiScreen_Spec.md` — xe.java fully read: button list, drawScreen, keyTyped (ESC→close),
  mouseClicked (random.click + actionPerformed); qd.java (GuiIngame) HUD elements + scale factor

### Documentation updates
- `INDEX.md` — 12 new rows added (all [STATUS:PROVIDED])
- `REQUESTS.md` — all 12 [STATUS:REQUIRED] → [STATUS:PROVIDED]
- `Specs/BlockCocoaPlant_Spec.md` — corrected identity confusion (ic=Cauldron not EndPortalFrame)

### Corrections issued
- `ic` (render type 24) is **BlockCauldron** (hollow cylinder AABB = base + 4 walls), NOT
  End Portal Frame. The prior research session had misidentified it. The actual EndPortalFrame
  is `rl` (already in classes.md).
- Classes.md mapping was already correct from earlier analysis — no change needed.

### Open questions flagged for next Coder request
- BlockCocoaPlant Java class name unknown (not in available decompiled files)
- ItemInWorldManager Java class name unknown (block-breaking progress tracker)
- FontRenderer Java class name unknown (zh=TextureManager, not FontRenderer)
- Entity-renderer dispatch mechanism in 1.0 (no RenderManager HashMap found)
- FOV base value in adt (B/C=4.0F are modifiers, not base FOV)

### Source files read this session
`uk.java`, `aba.java`, `ra.java`, `id.java`, `aez.java` (MineshaftPieces);
`bd.java` (IWorldAccess interface); `ry.java` (partial — bd dispatch methods);
`afv.java` (partial — WorldEvent handler); `qv.java` (WorldGenLakes);
`xj.java` (partial — ChunkProviderGenerate populate, lake spawning);
`cu.java` (BlockDispenser); `ic.java` (BlockCauldron);
`ahp.java` (BlockBrewingStand); `vi.java` (EntityPlayer header);
`wq.java` (PlayerAbilities); `xe.java` (GuiScreen);
`adt.java` (EntityRenderer header); `n.java` (ItemRenderer)

**Spec count this session:** 12 specs
**Estimated effort:** ~6 hours equivalent

---

## Coder Session — 2026-04-17

**Role:** CODER
**Specs consumed:** 12 (all from the prior Analyst session)

### Implementations

- `Core/IWorldAccess.cs` — 9-method `bd` interface created; `World.AddWorldAccess / RemoveWorldAccess`, `NotifyBlockChange`, `NotifyBlocksChanged`, `SpawnParticle`, `PlaySoundAtCoords`, `PlayAuxSFX`, `SpawnEntity`, `DespawnEntity` all dispatch to listeners
- `Core/WorldGen/MapGenMineshaft.cs` — full rewrite: `MapGenMineshaft`, `MineshaftFactory`, `MineshaftStart` (uk), `MineshaftCorridor` (aba, cobwebs/spawner/rails/supports/loot), `MineshaftCrossing` (ra), `MineshaftStaircase` (id)
- `Core/SoundEventId.cs` — `SoundEventId` (1000–2004) and `SoundName` string constant tables
- `Core/ParticleType.cs` — `ParticleType` confirmed + inferred string constants, `BlockCrack()/IconCrack()` helpers
- `Core/WorldGen/WorldGenLakes.cs` — full 6-step algorithm; wired into `ChunkProviderGenerate.PopulateChunk` (water 1/4, lava 1/8 doubly-biased-low)
- `Bridge/Overrides/DispenserBlock.cs` — full dispense dispatch: Arrow/Snowball/Egg/SplashPotion → projectile entities; fallback EntityItem drop; break drops inventory; events 1001/2000
- `Core/EntityPlayer.cs` — added `Abilities : PlayerAbilities` field (obf `cc`); NBT write/read already wired
- `Graphics/WorldRenderer.cs` — `IWorldAccess` implementation; dirty chunk-section tracking (HashSet); stub sound/particle/entity hooks
- `Graphics/EntityRenderer.cs` — projection matrix, camera position (`PlayerEyeHeight = 1.62F`), FOV stubs, view-bob fields; `ItemRenderer` stub
- `Graphics/FontRenderer.cs` — `FontHeight=9`; colour table §0–§f; `GetStringWidth`, `DrawString`, `DrawStringWithShadow`; glyph width stub (6px default)
- `Graphics/GuiScreen.cs` — `GuiBase`, `GuiButton` (collision + draw), `GuiScreen` (lifecycle + ESC + mouseClicked + actionPerformed), `GuiIngame` (scale-factor formula)

### INDEX.md updates
All 12 `[STATUS:PROVIDED]` rows updated to `[STATUS:IMPLEMENTED]`.

### Files added/modified
New: `Core/IWorldAccess.cs`, `Core/SoundEventId.cs`, `Core/ParticleType.cs`,
     `Core/WorldGen/WorldGenLakes.cs`, `Core/WorldGen/MapGenMineshaft.cs` (rewrite),
     `Bridge/Overrides/DispenserBlock.cs`, `Graphics/WorldRenderer.cs`,
     `Graphics/EntityRenderer.cs`, `Graphics/FontRenderer.cs`, `Graphics/GuiScreen.cs`
Modified: `Core/World.cs`, `Core/ChunkProviderGenerate.cs`, `Core/EntityPlayer.cs`,
          `Documentation/VoxelCore/Parity/INDEX.md`

**Spec count this session:** 12


---

## Analyst Session — 2026-04-17 (3)

**Role:** ANALYST
**Date:** 2026-04-17

### Source files read

`net/minecraft/client/Minecraft.java` (lines 1098–1696; completing full read);
`si.java` (WorldInfo — SpawnX/Y/Z NBT fields);
`jz.java` (ChunkProviderServer — load/unload/save pipeline);
`ry.java` (World — spawn search lines 130–215 + v() at 2577 + g(player) at 2585);
`yy.java` (Block registry — confirmed ends at ID 122 / dragonEgg, no ID 127);
`qd.java` (GuiIngame — full HUD rendering, UV coords);
`di.java` (EntityPlayerSP — previously read; spec written this session);
`dm.java` (SurvivalItemInWorldManager — previously read; spec written this session);
`aes.java` (abstract ItemInWorldManager — previously read; spec written this session);
`uq.java` (CreativeItemInWorldManager — previously read; spec written this session)

### Specs written

- `Specs/EntityPlayerSP_Spec.md` — `di extends vi`; fields b/c/d/e; dimension travel; block-break dispatch through Minecraft.c; EntityOtherPlayerMP (`zb`) noClip+interpolation
- `Specs/ItemInWorldManager_Spec.md` — `aes`/`dm`/`uq` hierarchy; damage accumulator 0–1.0; per-tick formula f+=block.a(player); break event 2001; creative instabuild; crack overlay via afv.damagePartialTime
- `Specs/WorldSpawn_Spec.md` — `si` NBT fields; biome search radius 256 + 1000-attempt walk; Y refinement 10000-attempt; Minecraft.e() preloads 17×17 chunks (Survival) / 9×9 (Creative); ry.g(player) keeps 5×5 loaded every 30 ticks
- `Specs/ChunkLoadingLoop_Spec.md` — `jz` load/generate pipeline; per-tick: 100 unloads + scan 10 chunks at 288-block radius + save 24 dirty; spawn-safe zone 128 blocks; `d(cx,cz)` mark-for-unload
- `Specs/BlockCocoaPlant_Spec.md` (REVISED) — **Block ID 127 does NOT exist in 1.0**; registry ends at 122; cocoa added in 1.2.1; dye item only in 1.0; action: do not implement for 1.0
- `Specs/MinecraftMain_Spec.md` — abstract `Minecraft` class; singleton fields (c/f/g/h/s/u/w/p/q/X/A/C/z); timer `aij` b=ticks c=partialTick; single-threaded loop; left/right click dispatch; keyboard hotkeys F1/F3/F5/F8/hotbar; dimension switch; respawn; version string "1.0.0"
- `Specs/GuiIngameHUD_Spec.md` — `qd`; `ef` scaled coords; gui.png: hotbar UV(0,0) 182×22, selection UV(0,22) 24×22; icons.png: crosshair UV(0,0), XP bar UV(0,64/69), hearts y=0, armor y=9, bubbles y=18, food y=27; full screen position table (H-22 hotbar, H-29 XP, H-39 hearts/food, H-49 armor/bubbles)

### Corrections issued

- BlockCocoaPlant: confirmed ID 127 absent from 1.0 block registry — Coder should NOT implement for 1.0 target

**Spec count this session:** 7 specs
**Estimated effort:** ~4 hours equivalent

---

## Coder Session — 2026-04-17 (3)

**Role:** CODER
**Date:** 2026-04-17

### Specs implemented

- `EntityPlayerSP_Spec.md` → `Core/EntityPlayerSP.cs`
- `ItemInWorldManager_Spec.md` → `Core/ItemInWorldManager.cs`
- `WorldSpawn_Spec.md` → `Core/World.cs` (FindSpawnPoint, EnsureChunksAroundPlayer, PreloadSpawnChunks)
- `ChunkLoadingLoop_Spec.md` → `Core/Engine.cs` (30-tick keep-alive loop wired)
- `BlockCocoaPlant_Spec.md` → SKIPPED — ID 127 absent in 1.0; documented as future `Bridge/Overrides/v1_2/`
- `MinecraftMain_Spec.md` → `Core/Engine.cs` (ElapsedTicks, Player, CurrentScreen, GameMode, GameMode.UpdateBlockRemoving per-tick)
- `GuiIngameHUD_Spec.md` → `Graphics/GuiScreen.cs` (GuiIngame.RenderGameOverlay with full UV data for all HUD elements; DrawTexturedModalRect stub)

### Files created

- `Core/EntityPlayerSP.cs` — `di extends EntityPlayer`; MovementInput (`agn`), SprintCooldown, EngineRef; TravelToDimension; Tick decrement; inner `MovementInput` class
- `Core/ItemInWorldManager.cs` — abstract `aes` base (RemoveBlock, UseItem); `SurvivalItemInWorldManager` (dm) with damage accumulator + 4-tick sound cadence + break threshold; `CreativeItemInWorldManager` (uq) instant break

### Files modified

- `Core/World.cs` — added FindSpawnPoint (1000-attempt walk), EnsureChunksAroundPlayer (5×5 loop), PreloadSpawnChunks (17×17 / 9×9)
- `Core/Engine.cs` — added ElapsedTicks, Player, CurrentScreen, GameMode fields; _chunkKeepAliveTick; FixedUpdate: ElapsedTicks++, GameMode.UpdateBlockRemoving(), 30-tick EnsureChunksAroundPlayer
- `Graphics/GuiScreen.cs` — GuiIngame fully replaced stub: all UV constants from spec; RenderGameOverlay with hotbar/XP/hearts/food/armor/crosshair helpers; DrawTexturedModalRect virtual stub

### INDEX.md updates

7 rows updated: BlockCocoaPlant, EntityPlayerSP, ItemInWorldManager, WorldSpawn, ChunkLoadingLoop, MinecraftMain, GuiIngameHUD → `[STATUS:IMPLEMENTED]`

**Spec count this session:** 7 specs (6 implemented + 1 intentional skip)
**Estimated effort:** ~2 hours equivalent

---

## Analyst Session — 2026-04-17 (4)

**Role:** ANALYST
**Date:** 2026-04-17

### Files read this session

- `adt.java` (EntityRenderer) — full mouse-look pipeline (lines 580–603), smooth camera path (lines 88–96), raycast `a(float)` (lines 111–166)
- `hs.java` (MouseHelper) — already read in prior session; confirmed field a=getDX, b=getDY
- `ia.java` (Entity) — `c(yaw, pitch)` turn method (lines 142–157); pitch clamp [-90,90]; X() sprint flag via `f(3)`
- `di.java` (EntityPlayerSP) — sprint detection double-tap (lines 115–175); sneak scaling; swim controls; sprint cancel conditions
- `vi.java` (EntityPlayer) — `cg=0.1F` (walkSpeed), `ch=0.02F` (airSpeed); sprint speed boost `cg*0.3`; movement dispatch `d(float,float)` (line 1010)
- `nq.java` (LivingEntity) — movement `d(float,float)` (lines 640–700); traction formula `0.16277136/slipperiness³`; jump `ak()` (lines 877–888); jump = `w=0.42F`; sprint horizontal impulse `±0.2`
- `agn.java` (MovementInput) — 5 fields: a=strafe, b=forward, c=sneak, d=jump, e=dive
- `hw.java` (GuiInventory) — player model render at (e+51, f+75) scale 30; `/gui/inventory.png`; effects sidebar
- `mg.java` (GuiContainer) — base container GUI; default 176×166; slot draw loop; tooltip rendering
- `gd.java` (ContainerPlayer) — full slot layout: crafting result (144,36); 2×2 grid; 4 armor slots; 3×9 main; hotbar y=142
- `dm.java` / `aes.java` — `c()` reach: 4.0F survival; `h()` isCreative

### Specs written

- `Specs/MouseLook_Spec.md` — `hs` delta read; sensitivity `(ki.c*0.6+0.2)³*8`; `ia.c()` applies `*0.15`; pitch clamp; invertMouse `ki.d`; smooth camera H/I accumulators → lerp J/K
- `Specs/PlayerMovement_Spec.md` — walk speed 0.1F; air 0.02F; sprint 1.3×; sneak input×0.2; jump 0.42F + sprint horizontal ±0.2; traction formula; food drain 0.099F/tile sprinting; double-tap sprint condition (food>6)
- `Specs/GuiInventory_Spec.md` — `hw`/`gd`; 176×166; `/gui/inventory.png`; full slot position table; player model render; effects sidebar
- `Specs/Raycast_Spec.md` — `adt.a(float)` updates `Minecraft.z`; survival reach 4.0F block / min(3.0F,blockDist) entity; creative 6.0F both; `gv` fields; face encoding 0-5; entity AABB expansion by `Q()`

### Documentation updated

- `INDEX.md` — 4 rows added (MouseLook, PlayerMovement, GuiInventory, Raycast) with [STATUS:PROVIDED]
- `REQUESTS.md` — 4 entries updated from [STATUS:REQUIRED] to [STATUS:PROVIDED]; 0 remaining
- `Mappings/classes.md` — added `mg` (GuiContainer), `hw` (GuiInventory), `hs` (MouseHelper), `ne` (SmoothValue); updated `agn` entry with all 5 fields

**Spec count this session:** 4 specs
**Estimated effort:** ~3 hours equivalent

---

## Coder Session 2026-04-17 (4)

### Specs implemented

- `Specs/MouseLook_Spec.md` → `Core/Entity.cs` (Turn, GetLookVector, GetEyePosition), `Core/Engine.cs` (sensitivity formula, mouse-look wiring, MouseSensitivity/InvertMouse)
- `Specs/PlayerMovement_Spec.md` → `Core/EntityPlayerSP.cs` (sprint double-tap 7-tick window, sneak ×0.2, AiForward/AiStrafe/WantsToJump), `Core/EntityPlayer.cs` (WalkSpeed/AirSpeedBase, sprint speed in Tick), `Core/LivingEntity.cs` (sprint jump horizontal impulse)
- `Specs/GuiInventory_Spec.md` → `Graphics/GuiInventory.cs` (GuiContainer, GuiInventory with full 45-slot layout; DrawContainerBackground stub pending GL)
- `Specs/Raycast_Spec.md` → `Core/Entity.cs` (GetEyePosition, GetLookVector), `Core/Engine.cs` (ObjectMouseOver, FixedUpdate raycast via GameMode.GetReach)

### Build fixes applied

- `LivingEntity.GetEyeHeight()` changed to `override double` (was hiding `Entity.GetEyeHeight(): virtual double`)
- `ThrowableBase.GetEyeHeight()` changed to `override double`
- Duplicate `EntityPlayer.Tick()` removed; sprint-speed logic merged into surviving Tick at ~line 264
- `EntityPlayerSP.TravelToDimension()` changed to `override`
- `Engine.cs` raycast: `Vec3.XCoord/YCoord/ZCoord` → `Vec3.X/Y/Z` (correct property names)

### Files touched

- `Core/Entity.cs`, `Core/EntityPlayer.cs`, `Core/EntityPlayerSP.cs`, `Core/LivingEntity.cs`, `Core/ThrowableBase.cs`
- `Core/Engine.cs`, `Core/InputSnapshot.cs`, `Core/WorldSnapshot.cs`
- `Core/ItemInWorldManager.cs`, `Core/World.cs`
- `Graphics/GuiInventory.cs`, `Graphics/GuiScreen.cs`

**Spec count this session:** 4 specs implemented
**Build result:** 0 errors, 3 warnings (unused stub fields — expected)

---

## Session 2026-04-17 (5) — Analyst: TerrainAtlas comprehensive spec

**Role:** ANALYST
**Task:** Expand `Specs/TerrainAtlas_Spec.md` per [STATUS:REQUIRED] request "Block Texture
Rendering — Terrain Atlas UV Mapping"

### Specs written / updated

- `Specs/TerrainAtlas_Spec.md` — **full rewrite**
  - Complete 122-block ID → `bL` table (sourced from `yy.java` static initializers)
  - Multi-face override tables for 14 block classes: GrassBlock, Log, Dispenser, Sandstone,
    TNT, Bookshelf, Workbench, Farmland, Furnace (off/on), StoneBrick, Pumpkin/Jack-o-Lantern,
    Mushroom Block (brown/red), Melon, Mycelium, EnchantingTable, Cauldron, EndPortalFrame
  - Wool 16-color formula: `index = 113 + ((meta & 8) >> 3) + (meta & 7) * 16`
  - Stone Slab/DoubleSlab metadata texture table (meta 0–5)
  - Animated tile indices confirmed: Water top/bottom=205, sides=206; Lava top/bottom=237, sides=238
  - Biome tint placeholders: grass=(72,181,24), foliage=(78,164,0)
  - Rendering type classification (A=opaque, B=biome tint, C=cutout, D=animated, E=sprite)
  - Complete tile index reference table (all used indices 0–238)

### Source files read

`yy.java`, `jb.java`, `aip.java`, `aat.java`, `rn.java`, `eu.java`, `cu.java`, `abm.java`,
`ay.java`, `ni.java`, `jh.java`, `nf.java`, `wd.java`, `of.java`, `ez.java`, `sy.java`,
`ic.java`, `rl.java`, `fr.java`, `xs.java`, `agw.java`, `yq.java`, `ahp.java`

### Documentation updated

- `Specs/TerrainAtlas_Spec.md` — complete rewrite (10 sections, 122-block table)
- `INDEX.md` — TerrainAtlas row updated to [STATUS:PROVIDED] with expanded description
- `REQUESTS.md` — "Block Texture Rendering" entry changed to [STATUS:PROVIDED]
- `METRICS.md` — this entry

**Spec count this session:** 1 spec (major expansion of existing)
