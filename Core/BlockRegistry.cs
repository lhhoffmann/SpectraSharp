using SpectraEngine.Core.Blocks;

namespace SpectraEngine.Core;

/// <summary>
/// Registers all 122 vanilla block singletons into <see cref="Block.BlocksList"/>.
///
/// Call <see cref="Initialize"/> once before the world is created. The Block constructors
/// auto-register each instance at <c>Block.BlocksList[id]</c>, so all physics, collision,
/// light-opacity and random-tick lookups work for every ID from 1 to 122.
///
/// Material column mapping (p.X → Material.X from Material.cs):
///   p.b=Grass_, p.c=Ground, p.d=Plants, p.e=RockTransp, p.f=RockTransp2,
///   p.g=Water, p.h=Lava_, p.i=Leaves, p.o=Mat_O, p.q=MatPass_Q, p.A=Portal_A
///
/// StepSound column mapping (X → Block.SoundX):
///   b=SoundStone, c=SoundWood, d=SoundGravel, e=SoundGrass,
///   f=SoundStoneHighPitch, g=SoundStoneHighPitch2, h=SoundGlass,
///   i=SoundCloth, j=SoundSand
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRegistry_Spec.md
/// </summary>
public static class BlockRegistry
{
    private static bool _initialized;

    /// <summary>
    /// Creates all block singletons. Safe to call multiple times — only runs once.
    /// Must be called before World is created.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        Register();

        // Air slot (ID 0) is passable — set after all solid blocks registered
        Block.CanPassThrough[0] = true;
    }

    // ── Registration (IDs 1–122) ──────────────────────────────────────────────

    private static void Register()
    {
        // ── Tier 1: Basic terrain (1–20) ─────────────────────────────────────

        // 1 — Stone (gm): p.e, sound f, hard 1.5, res 10
        new Block(1, 1, Material.RockTransp)
            .SetHardness(1.5f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("stone");

        // 2 — Grass (jb): concrete class, p.e, sound e, hard 0.6
        new BlockGrass(2)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("grass");

        // 3 — Dirt (agd): p.c, sound d, hard 0.5, texture 2
        new Block(3, 2, Material.Ground)
            .SetHardness(0.5f)
            .SetStepSound(Block.SoundGravel)
            .SetBlockName("dirt");

        // 4 — Cobblestone (yy base): p.e, texture 16, hard 2.0, res 10
        new Block(4, 16, Material.RockTransp)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetBlockName("stonebrick");

        // 5 — Planks (yy base): p.d, texture 4, hard 2.0, res 5
        new Block(5, 4, Material.Plants)
            .SetHardness(2.0f).SetResistance(5.0f)
            .SetHasTileEntity()      // T flag
            .SetBlockName("wood");

        // 6 — Sapling (aet): p.e, texture 15, hard 0.0, sound e — stub (no growth tick)
        new Block(6, 15, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetHasTileEntity()
            .SetBlockName("sapling");

        // 7 — Bedrock (yy base): p.e, texture 17, indestructible, res 6000000, N flag
        new Block(7, 17, Material.RockTransp)
            .SetUnbreakable()
            .SetResistance(6000000.0f)
            .ClearNeedsRandomTick()
            .SetBlockName("bedrock");

        // 8 — Flowing Water (ahx): p.g, opacity 3, hard 100
        new BlockFluid(8, Material.Water)
            .SetHardness(100.0f)
            .SetLightOpacity(3)
            .SetBlockName("water");

        // 9 — Still Water (add): p.g, opacity 3, hard 100
        new BlockStationary(9, Material.Water)
            .SetHardness(100.0f)
            .SetLightOpacity(3)
            .SetBlockName("water");

        // 10 — Flowing Lava (ahx): p.h, light 15 (1.0→15), hard 0
        new BlockFluid(10, Material.Lava_)
            .SetHardness(0.0f)
            .SetLightValue(1.0f)
            .SetBlockName("lava");

        // 11 — Still Lava (add): p.h, light 15, hard 100
        new BlockStationary(11, Material.Lava_)
            .SetHardness(100.0f)
            .SetLightValue(1.0f)
            .SetBlockName("lava");

        // 12 — Sand (cj): concrete gravity class, p.o, sound j, hard 0.5, texture 18
        new BlockSand(12, 18, Material.Mat_O)
            .SetHardness(0.5f)
            .SetStepSound(Block.SoundSand)
            .SetBlockName("sand");

        // 13 — Gravel (kb): concrete gravity class, p.c, sound d, hard 0.6
        new BlockGravel(13)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGravel)
            .SetBlockName("gravel");

        // 14 — Gold Ore (v): p.e, texture 32, sound f, hard 3.0, res 5
        new Block(14, 32, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("oreGold");

        // 15 — Iron Ore (v): p.e, texture 33, sound f, hard 3.0, res 5
        new Block(15, 33, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("oreIron");

        // 16 — Coal Ore (v): p.e, texture 34, sound f, hard 3.0, res 5
        new Block(16, 34, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("oreCoal");

        // 17 — Log (aip): concrete multi-face class, p.d, sound c, hard 2.0, T flag
        new BlockLog(17)
            .SetHardness(2.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("log");

        // 18 — Leaves (qo): concrete decay class, p.e, sound e, hard 0.2, opacity 1, T flag
        new BlockLeaves(18, 52)
            .SetHardness(0.2f)
            .SetLightOpacity(1)
            .SetStepSound(Block.SoundGrass)
            .SetHasTileEntity()
            .SetBlockName("leaves");

        // 19 — Sponge (wh): p.e, texture from spec (first row), hard 0.6, sound e — stub
        new Block(19, 48, Material.RockTransp)    // texture 48 = sponge (from atlas row 3)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("sponge");

        // 20 — Glass (aho): p.q, texture 49, sound h, hard 0.3
        new Block(20, 49, Material.MatPass_Q)
            .SetHardness(0.3f)
            .SetStepSound(Block.SoundGlass)
            .SetBlockName("glass");

        // ── Tier 2: Ores, decorative stone, redstone (21–60) ─────────────────

        // 21 — Lapis Ore (v): p.e, texture 160, sound f, hard 3.0, res 5
        new Block(21, 160, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("oreLapis");

        // 22 — Lapis Block (yy): p.e, texture 144, hard 3.0, res 5
        new Block(22, 144, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetBlockName("blockLapis");

        // 23 — Dispenser (cu): p.e, sound f, hard 3.5, T flag — stub
        new Block(23, 45, Material.RockTransp)
            .SetHardness(3.5f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetHasTileEntity()
            .SetBlockName("dispenser");

        // 24 — Sandstone (aat): p.e, sound f, hard 0.8 — stub
        new Block(24, 192, Material.RockTransp)
            .SetHardness(0.8f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("sandStone");

        // 25 — NoteBlock (yq): hard 0.8, T flag — stub (no sound action)
        new Block(25, 74, Material.Plants)
            .SetHardness(0.8f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("musicBlock");

        // 26 — Bed (aab): aab class, p.m, tex 134, hard 0.2, N flag
        new BlockBed(26)
            .SetBlockName("bed");

        // 27 — Powered Rail (afr): texture 179, p.f(stone2), sound g, hard 0.7, T
        new Block(27, 179, Material.RockTransp2)
            .SetHardness(0.7f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetHasTileEntity()
            .SetBlockName("goldenRail");

        // 28 — Detector Rail (ags): texture 195, p.f, sound g, hard 0.7, T
        new Block(28, 195, Material.RockTransp2)
            .SetHardness(0.7f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetHasTileEntity()
            .SetBlockName("detectorRail");

        // 29 — Sticky Piston (abr): texture 106, hard 0.5
        new BlockPiston(29, isSticky: true);

        // 30 — Web (kc): texture 11, opacity 1, hard 4.0
        new Block(30, 11, Material.RockTransp)
            .SetHardness(4.0f)
            .SetLightOpacity(1)
            .SetBlockName("web");

        // 31 — Tall Grass (kv): texture 39, p.e, sound e, hard 0
        new Block(31, 39, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("tallgrass");

        // 32 — Dead Bush (jl): texture 55, p.e, sound e, hard 0
        new Block(32, 55, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("deadbush");

        // 33 — Piston (abr): texture 107, hard 0.5
        new BlockPiston(33, isSticky: false);

        // 34 — Piston Extension (acu): texture 107, hard 0.5
        new BlockPistonExtension(34);

        // 35 — Wool (fr): p.i, sound i, hard 0.8, T
        new Block(35, 64, Material.Leaves)
            .SetHardness(0.8f)
            .SetStepSound(Block.SoundCloth)
            .SetHasTileEntity()
            .SetBlockName("cloth");

        // 36 — Moving Piston proxy (qz): hardness -1.0 (unmined), T
        new BlockMovingPiston(36);

        // 37 — Dandelion (wg): texture 13, p.e, sound e, hard 0
        new Block(37, 13, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("flower");

        // 38 — Rose (wg): texture 12, p.e, sound e, hard 0
        new Block(38, 12, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("rose");

        // 39 — Brown Mushroom (js): texture 29, p.e, sound e, hard 0, light 1 (0.125F)
        new Block(39, 29, Material.RockTransp)
            .SetHardness(0.0f)
            .SetLightValue(0.125f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("mushroom");

        // 40 — Red Mushroom (js): texture 28, p.e, sound e, hard 0
        new Block(40, 28, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("mushroom");

        // 41 — Gold Block (rs): texture 23, p.f, sound g, hard 3.0, res 10
        new Block(41, 23, Material.RockTransp2)
            .SetHardness(3.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetBlockName("blockGold");

        // 42 — Iron Block (rs): texture 22, p.f, sound g, hard 5.0, res 10
        new Block(42, 22, Material.RockTransp2)
            .SetHardness(5.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetBlockName("blockIron");

        // 43 — Double Slab (xs): concrete BlockSlab, isDouble=true
        new BlockSlab(43, true)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("stoneSlab");

        // 44 — Single Slab (xs): concrete BlockSlab, isDouble=false (bottom half AABB)
        new BlockSlab(44, false)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("stoneSlab");

        // 45 — Brick (yy): texture 7, p.e, sound f, hard 2.0, res 10
        new Block(45, 7, Material.RockTransp)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("brick");

        // 46 — TNT (abm): texture 8, p.r (Mat_R = flammable), sound e, hard 0
        new BlockTNT(46)
            .SetStepSound(Block.SoundGrass);

        // 47 — Bookshelf (ay): texture 35, p.d, sound c, hard 1.5
        new Block(47, 35, Material.Plants)
            .SetHardness(1.5f)
            .SetStepSound(Block.SoundWood)
            .SetBlockName("bookshelf");

        // 48 — Mossy Cobblestone (yy): texture 36, p.e, sound f, hard 2.0, res 10
        new Block(48, 36, Material.RockTransp)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("stoneMoss");

        // 49 — Obsidian (ain): texture 37, p.e, sound f, hard 50.0, res 2000
        new Block(49, 37, Material.RockTransp)
            .SetHardness(50.0f).SetResistance(2000.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("obsidian");

        // 50 — Torch (bg): texture 80, p.d, sound c, hard 0, light 14 (0.9375F), T
        new Block(50, 80, Material.Plants)
            .SetHardness(0.0f)
            .SetLightValue(0.9375f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("torch");

        // ── Tier 3: Fire, spawner, stairs, containers, redstone (51–90) ──────

        // 51 — Fire (wj): BlockFire — material p.n, light 15, no hardness, no AABB, N
        new BlockFire(51)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundWood)
            .SetBlockName("fire");

        // 52 — Mob Spawner (kk): texture 65, p.f, sound g, hard 5.0, N
        new Block(52, 65, Material.RockTransp2)
            .SetHardness(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .ClearNeedsRandomTick()
            .SetBlockName("mobSpawner");

        // 53 — Wood Stairs (ahh): BlockStairs delegating to planks (ID 5)
        new BlockStairs(53, Block.BlocksList[5]!)
            .SetBlockName("stairsWood");

        // 54 — Chest (au): p.d, sound c, hard 2.5, T
        new Block(54, 26, Material.Plants)
            .SetHardness(2.5f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("chest");

        // 55 — Redstone Dust (kw): BlockRedstoneWire
        new BlockRedstoneWire(55);

        // 56 — Diamond Ore (v): p.e, texture 50, sound f, hard 3.0, res 5
        new Block(56, 50, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("oreDiamond");

        // 57 — Diamond Block (rs): p.f, texture 24, sound g, hard 5.0, res 10
        new Block(57, 24, Material.RockTransp2)
            .SetHardness(5.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetBlockName("blockDiamond");

        // 58 — Workbench (rn): p.d, sound c, hard 2.5
        new Block(58, 59, Material.Plants)
            .SetHardness(2.5f)
            .SetStepSound(Block.SoundWood)
            .SetBlockName("workbench");

        // 59 — Crops (aha): texture 88, plant material, sound grass, hard 0, random tick
        new BlockCrops(59, 88)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("crops");

        // 60 — Farmland (ni): earth material, sound gravel, hard 0.6, random tick
        new BlockFarmland(60)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGravel)
            .SetBlockName("farmland");

        // 61 — Furnace Off (eu): p.e, sound f, hard 3.5, T
        new Block(61, 45, Material.RockTransp)
            .SetHardness(3.5f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetHasTileEntity()
            .SetBlockName("furnace");

        // 62 — Furnace On (eu): p.e, sound f, hard 3.5, light 13 (0.875F), T
        new Block(62, 45, Material.RockTransp)
            .SetHardness(3.5f)
            .SetLightValue(0.875f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetHasTileEntity()
            .SetBlockName("furnace");

        // 63 — Standing Sign (mr): p.d, sound c, hard 1.0, N+T
        new Block(63, 4, Material.Plants)
            .SetHardness(1.0f)
            .SetStepSound(Block.SoundWood)
            .ClearNeedsRandomTick()
            .SetHasTileEntity()
            .SetBlockName("sign");

        // 64 — Wood Door (uc): p.d (plants = wood material), sound c, hard 3.0
        new BlockDoor(64, Material.Plants)
            .SetHardness(3.0f)
            .SetStepSound(Block.SoundWood)
            .ClearNeedsRandomTick()
            .SetBlockName("doorWood");

        // 65 — Ladder (afu): texture 83, p.d, sound c, hard 0.4, T
        new Block(65, 83, Material.Plants)
            .SetHardness(0.4f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("ladder");

        // 66 — Rail (afr): texture 128, p.f, sound g, hard 0.7, T
        new Block(66, 128, Material.RockTransp2)
            .SetHardness(0.7f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetHasTileEntity()
            .SetBlockName("rail");

        // 67 — Cobblestone Stairs (ahh): BlockStairs delegating to cobblestone (ID 4)
        new BlockStairs(67, Block.BlocksList[4]!)
            .SetBlockName("stairsStone");

        // 68 — Wall Sign (mr): p.d, sound c, hard 1.0, N+T
        new Block(68, 4, Material.Plants)
            .SetHardness(1.0f)
            .SetStepSound(Block.SoundWood)
            .ClearNeedsRandomTick()
            .SetHasTileEntity()
            .SetBlockName("sign");

        // 69 — Lever (aaa): BlockLever
        new BlockLever(69);

        // 70 — Stone Pressure Plate (wx): BlockPressurePlate (living mobs sensor)
        new BlockPressurePlate(70, 1, Material.RockTransp, detectAllEntities: false)
            .SetStepSound(Block.SoundStoneHighPitch);

        // 71 — Iron Door (uc): p.f (RockTransp2 = iron material), sound g, hard 5.0
        new BlockDoor(71, Material.RockTransp2)
            .SetHardness(5.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .ClearNeedsRandomTick()
            .SetBlockName("doorIron");

        // 72 — Wood Pressure Plate (wx): BlockPressurePlate (all entities sensor)
        new BlockPressurePlate(72, 4, Material.Plants, detectAllEntities: true)
            .SetStepSound(Block.SoundWood);

        // 73 — Redstone Ore Off (oc): BlockOreRedstone (normal)
        new BlockOreRedstone(73, isGlowing: false)
            .SetStepSound(Block.SoundStoneHighPitch);

        // 74 — Redstone Ore On (oc): BlockOreRedstone (glowing)
        new BlockOreRedstone(74, isGlowing: true)
            .SetStepSound(Block.SoundStoneHighPitch);

        // 75 — Redstone Torch Off (ku): BlockRedstoneTorch (off)
        new BlockRedstoneTorch(75, 115, isOn: false)
            .SetStepSound(Block.SoundWood);

        // 76 — Redstone Torch On (ku): BlockRedstoneTorch (on)
        new BlockRedstoneTorch(76, 99, isOn: true)
            .SetStepSound(Block.SoundWood);

        // 77 — Stone Button (ahv): BlockButton
        new BlockButton(77);

        // 78 — Snow Layer (aif) — full impl: BlockSnow
        new BlockSnow(78)
            .SetHardness(0.1f)
            .SetStepSound(Block.SoundCloth)
            .SetBlockName("snow");

        // 79 — Ice (ahq) — full impl: BlockIce
        new BlockIce(79)
            .SetHardness(0.5f)
            .SetStepSound(Block.SoundGlass)
            .SetBlockName("ice");

        // 80 — Snow Block (jk): texture 66, p.i, sound i, hard 0.2
        new Block(80, 66, Material.Leaves)
            .SetHardness(0.2f)
            .SetStepSound(Block.SoundCloth)
            .SetBlockName("snow");

        // 81 — Cactus (ow): texture 70, p.i, sound i, hard 0.4
        new Block(81, 70, Material.Leaves)
            .SetHardness(0.4f)
            .SetStepSound(Block.SoundCloth)
            .SetBlockName("cactus");

        // 82 — Clay (pc): texture 72, p.c, sound d, hard 0.6
        new Block(82, 72, Material.Ground)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGravel)
            .SetBlockName("clay");

        // 83 — Reed (md): texture 73, p.e, sound e, hard 0, N
        new Block(83, 73, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .ClearNeedsRandomTick()
            .SetBlockName("reeds");

        // 84 — Jukebox (abl): BlockJukebox, p.d (wood), sound f, hard 2.0, res 10, T
        new BlockJukebox(84);

        // 85 — Oak Fence (nz): concrete BlockFence, wood material
        new BlockFence(85, 4)
            .SetHardness(2.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundWood)
            .SetBlockName("fence");

        // 86 — Pumpkin (nf): texture 102, p.d, sound c, hard 1.0, T
        new Block(86, 102, Material.Plants)
            .SetHardness(1.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("pumpkin");

        // 87 — Netherrack (et): texture 103, p.e, sound f, hard 0.4
        new Block(87, 103, Material.RockTransp)
            .SetHardness(0.4f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("hellrock");

        // 88 — Soul Sand (mq): texture 104, p.o, sound j, hard 0.5
        new Block(88, 104, Material.Mat_O)
            .SetHardness(0.5f)
            .SetStepSound(Block.SoundSand)
            .SetBlockName("hellsand");

        // 89 — Glowstone (sk): texture 105, p.q, sound h, hard 0.3, light 15
        new Block(89, 105, Material.MatPass_Q)
            .SetHardness(0.3f)
            .SetLightValue(1.0f)
            .SetStepSound(Block.SoundGlass)
            .SetBlockName("lightgem");

        // 90 — Nether Portal (sc): BlockPortal
        new Blocks.BlockPortal(90)
            .SetBlockName("portal");

        // ── Tier 4: Less common (91–122) ──────────────────────────────────────

        // 91 — Jack-o-Lantern (nf): texture 102, p.d, sound c, hard 1.0, light 15, T
        new Block(91, 102, Material.Plants)
            .SetHardness(1.0f)
            .SetLightValue(1.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("litpumpkin");

        // 92 — Cake (aem): texture 121, p.i, sound i, hard 0.5, N+T
        new Block(92, 121, Material.Leaves)
            .SetHardness(0.5f)
            .SetStepSound(Block.SoundCloth)
            .ClearNeedsRandomTick()
            .SetHasTileEntity()
            .SetBlockName("cake");

        // 93 — Repeater Off (mz): BlockRedstoneDiode (off)
        new BlockRedstoneDiode(93, isOn: false)
            .SetStepSound(Block.SoundWood);

        // 94 — Repeater On (mz): BlockRedstoneDiode (on)
        new BlockRedstoneDiode(94, isOn: true)
            .SetStepSound(Block.SoundWood);

        // 95 — Locked Chest (vj): p.d, sound c, hard 0, light 15, T
        new Block(95, 26, Material.Plants)
            .SetHardness(0.0f)
            .SetLightValue(1.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("lockedchest");

        // 96 — Trapdoor (mf): p.d, sound c, hard 3.0, N+T
        new Block(96, 84, Material.Plants)
            .SetHardness(3.0f)
            .SetStepSound(Block.SoundWood)
            .ClearNeedsRandomTick()
            .SetHasTileEntity()
            .SetBlockName("trapdoor");

        // 97 — Silverfish (vf): p.e, hard 0.75 — stub
        new Block(97, 1, Material.RockTransp)
            .SetHardness(0.75f);

        // 98 — Stone Brick (jh): p.e, sound f, hard 1.5, res 10
        new Block(98, 54, Material.RockTransp)
            .SetHardness(1.5f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("stonebricksmooth");

        // 99 — Brown Mushroom Cap (wd): texture 142, p.d, sound c, hard 0.2, T
        new Block(99, 142, Material.Plants)
            .SetHardness(0.2f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("mushroom");

        // 100 — Red Mushroom Cap (wd): texture 142, p.d, sound c, hard 0.2, T
        new Block(100, 142, Material.Plants)
            .SetHardness(0.2f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("mushroom");

        // 101 — Iron Bars (uh): p.f, sound g, hard 5.0, res 10
        new Block(101, 85, Material.RockTransp2)
            .SetHardness(5.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch2)
            .SetBlockName("fenceIron");

        // 102 — Glass Pane (uh): texture 49, p.q, sound h, hard 0.3
        new Block(102, 49, Material.MatPass_Q)
            .SetHardness(0.3f)
            .SetStepSound(Block.SoundGlass)
            .SetBlockName("thinGlass");

        // 103 — Melon (of): p.d, sound c, hard 1.0
        new Block(103, 136, Material.Plants)
            .SetHardness(1.0f)
            .SetStepSound(Block.SoundWood)
            .SetBlockName("melon");

        // 104 — Pumpkin Stem (pu): p.d, sound c, hard 0, T
        new Block(104, 111, Material.Plants)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("pumpkinStem");

        // 105 — Melon Stem (pu): p.d, sound c, hard 0, T
        new Block(105, 111, Material.Plants)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("pumpkinStem");

        // 106 — Vines (ahl): p.e, sound e, hard 0.2, T
        new Block(106, 143, Material.RockTransp)
            .SetHardness(0.2f)
            .SetStepSound(Block.SoundGrass)
            .SetHasTileEntity()
            .SetBlockName("vine");

        // 107 — Fence Gate (fp): texture 4, p.d, sound c, hard 2.0, res 5, T
        new Block(107, 4, Material.Plants)
            .SetHardness(2.0f).SetResistance(5.0f)
            .SetStepSound(Block.SoundWood)
            .SetHasTileEntity()
            .SetBlockName("fenceGate");

        // 108 — Brick Stairs (ahh): BlockStairs delegating to bricks (ID 45)
        new BlockStairs(108, Block.BlocksList[45]!)
            .SetBlockName("stairsBrick");

        // 109 — Stone Brick Stairs (ahh): BlockStairs delegating to stone brick (ID 98)
        new BlockStairs(109, Block.BlocksList[98]!)
            .SetBlockName("stairsStoneBrickSmooth");

        // 110 — Mycelium (ez): p.e, sound e, hard 0.6
        new Block(110, 78, Material.RockTransp)
            .SetHardness(0.6f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("mycel");

        // 111 — Lily Pad (qi): texture 76, p.e, sound e, hard 0
        new Block(111, 76, Material.RockTransp)
            .SetHardness(0.0f)
            .SetStepSound(Block.SoundGrass)
            .SetBlockName("waterlily");

        // 112 — Nether Brick (yy): texture 224, p.e, sound f, hard 2.0, res 10
        new Block(112, 224, Material.RockTransp)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("netherBrick");

        // 113 — Nether Brick Fence (nz): concrete BlockFence with stone material
        new BlockFence(113, 224, Material.RockTransp)
            .SetHardness(2.0f).SetResistance(10.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("netherFence");

        // 114 — Nether Brick Stairs (ahh): BlockStairs delegating to nether brick (ID 112)
        new BlockStairs(114, Block.BlocksList[112]!)
            .SetBlockName("stairsNetherBrick");

        // 115 — Nether Wart (vy): T stub
        new Block(115, 0, Material.Plants)
            .SetHasTileEntity()
            .SetBlockName("netherStalk");

        // 116 — Enchanting Table (sy): p.e, sound f, hard 5.0, res 2000
        new Block(116, 122, Material.RockTransp)
            .SetHardness(5.0f).SetResistance(2000.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("enchantmentTable");

        // 117 — Brewing Stand (ahp): p.e, sound f, hard 0.5, light 1, T
        new Block(117, 157, Material.RockTransp)
            .SetHardness(0.5f)
            .SetLightValue(0.125f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetHasTileEntity()
            .SetBlockName("brewingStand");

        // 118 — Cauldron (ic): p.e, hard 2.0, T
        new Block(118, 154, Material.RockTransp)
            .SetHardness(2.0f)
            .SetHasTileEntity()
            .SetBlockName("cauldron");

        // 119 — End Portal (aid): BlockEndPortal
        new Blocks.BlockEndPortal(119)
            .SetBlockName("endPortal");

        // 120 — End Portal Frame (rl): BlockEndPortalFrame
        new Blocks.BlockEndPortalFrame(120)
            .SetBlockName("endPortalFrame");

        // 121 — End Stone (yy): texture 175, p.e, sound f, hard 3.0, res 15
        new Block(121, 175, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(15.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("whiteStone");

        // 122 — Dragon Egg (aci): texture 167, p.e, sound f, hard 3.0, res 15, light 1
        new Block(122, 167, Material.RockTransp)
            .SetHardness(3.0f).SetResistance(15.0f)
            .SetLightValue(0.125f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetBlockName("dragonEgg");
    }
}
