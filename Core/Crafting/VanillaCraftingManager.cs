using static SpectraEngine.Core.Crafting.CraftingIngredient;

namespace SpectraEngine.Core.Crafting;

/// <summary>
/// Replica of <c>sl</c> (CraftingManager) — singleton recipe registry for vanilla crafting.
///
/// Recipe lookup (spec §1.2):
///   1. Tool repair (2 same damageable items of count 1 → merge durability).
///   2. Iterate recipe list in registration order; return first match.
///
/// Item ID conventions (registry indices):
///   Blocks: ID = block ID (0–255, same as block registry).
///   Items:  ID = 256 + itemId.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/CraftingRecipes_Spec.md §1–3
/// </summary>
public sealed class VanillaCraftingManager
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static readonly VanillaCraftingManager Instance = new();

    private VanillaCraftingManager() { }

    // ── Recipe list ───────────────────────────────────────────────────────────

    private readonly List<ICraftingRecipe> _recipes = BuildRecipes();

    // ── Known item/block IDs ──────────────────────────────────────────────────
    // Block IDs (same as registry, 0-255)

    private const int Stone       = 1;
    private const int Planks      = 5;
    private const int Log         = 17;
    private const int Sand        = 12;
    private const int Cobblestone = 4;
    private const int Glass       = 20;
    private const int Wool        = 35;
    private const int Sandstone   = 24;
    private const int Bricks      = 45;
    private const int StoneBrick  = 98;
    private const int NetherBrick = 112;
    private const int Rail        = 66;
    private const int Dispenser   = 23;
    private const int NoteBlock   = 25;
    private const int Bookshelf   = 47;
    private const int Jukebox     = 84;
    private const int Fence       = 85;
    private const int FenceGate   = 107;
    private const int Piston      = 33;
    private const int StickyPiston = 29;
    private const int Chest       = 54;
    private const int Furnace     = 61;
    private const int Workbench   = 58;
    private const int OakStairs   = 53;
    private const int StoneStairs = 67;
    private const int BrickStairs = 108;
    private const int StoneBrickStairs = 109;
    private const int NetherBrickStairs = 114;
    private const int StoneSlab   = 44;  // meta 0=stone, 1=sandstone, 2=planks, 3=cobble, 4=brick, 5=stonebrick
    private const int IronBars    = 101;
    private const int GlassPane   = 102;
    private const int Painting    = 9;   // obf: yy.ab uses wool; block 9 is Painting? Actually painting is entity, not block ID
    private const int SugarCaneBlock = 83;
    private const int IronBlock   = 42;
    private const int GoldBlock   = 41;
    private const int DiamondBlock = 57;
    private const int Glowstone   = 89;
    private const int PoweredRail = 27;
    private const int DetectorRail = 28;
    private const int TNT         = 46;
    private const int Lever       = 69;
    private const int WoodButton  = 77;
    private const int Bed         = 26;
    private const int RedstoneBlock = 55; // actually 55 is redstone wire block, not the block form
    private const int NetherBrickBlock = 112;
    private const int NetherBrickFence = 113;

    // Item IDs (256 + itemId)
    private const int CoalItem    = 263;  // itemId=7, damage=0=coal, damage=1=charcoal
    private const int DiamondItem = 264;  // itemId=8
    private const int IronIngot   = 265;  // itemId=9
    private const int GoldIngot   = 266;  // itemId=10
    private const int IronSword   = 267;  // itemId=11
    private const int WoodSword   = 268;  // itemId=12
    private const int WoodShovel  = 269;  // itemId=13
    private const int WoodPickaxe = 270;  // itemId=14
    private const int WoodAxe     = 271;  // itemId=15
    private const int StoneSword  = 272;  // itemId=16
    private const int StoneShovel = 273;  // itemId=17
    private const int StonePickaxe = 274; // itemId=18
    private const int StoneAxe    = 275;  // itemId=19
    private const int DiamondSword = 276; // itemId=20
    private const int DiamondShovel = 277; // itemId=21
    private const int DiamondPickaxe = 278; // itemId=22
    private const int DiamondAxe  = 279;  // itemId=23
    private const int StickItem   = 280;  // itemId=24
    private const int BowItem     = 261;  // itemId=5... wait 261=256+5
    private const int ArrowItem   = 262;  // itemId=6
    private const int StringItem  = 287;  // itemId=31
    private const int FeatherItem = 288;  // itemId=32
    private const int GunpowderItem = 289; // itemId=33
    private const int WheatItem   = 296;  // itemId=40
    private const int BreadItem   = 297;  // itemId=41
    private const int FlintItem   = 318;  // itemId=62
    private const int RedstoneItem = 331; // itemId=75
    private const int SnowballItem = 332; // itemId=76
    private const int LeatherItem = 334;  // itemId=78
    private const int BrickItem_  = 336;  // itemId=80
    private const int ClayBallItem = 337; // itemId=81
    private const int SugarCaneItem = 338; // itemId=82
    private const int PaperItem   = 339;  // itemId=83
    private const int BookItem    = 340;  // itemId=84
    private const int SlimeballItem = 341; // itemId=85
    private const int EggItem     = 344;  // itemId=88
    private const int GoldSword   = 283;  // itemId=27
    private const int GoldShovel  = 284;  // itemId=28
    private const int GoldPickaxe = 285;  // itemId=29
    private const int GoldAxe     = 286;  // itemId=30
    private const int WoodHoe     = 290;  // itemId=34
    private const int StoneHoe    = 291;  // itemId=35
    private const int IronHoe     = 292;  // itemId=36
    private const int DiamondHoe  = 293;  // itemId=37
    private const int GoldHoe     = 294;  // itemId=38
    private const int IronShovel_ = 256;  // itemId=0
    private const int IronPickaxe_= 257;  // itemId=1
    private const int IronAxe_    = 258;  // itemId=2
    // Armors
    private const int LeatherHelmet  = 298; // itemId=42
    private const int LeatherChest   = 299; // itemId=43
    private const int LeatherLegs    = 300; // itemId=44
    private const int LeatherBoots   = 301; // itemId=45
    private const int ChainHelmet    = 302; // itemId=46
    private const int ChainChest     = 303; // itemId=47
    private const int ChainLegs      = 304; // itemId=48
    private const int ChainBoots     = 305; // itemId=49
    private const int IronHelmet     = 306; // itemId=50
    private const int IronChest      = 307; // itemId=51
    private const int IronLegs       = 308; // itemId=52
    private const int IronBoots      = 309; // itemId=53
    private const int DiamondHelmet  = 310; // itemId=54
    private const int DiamondChest   = 311; // itemId=55
    private const int DiamondLegs    = 312; // itemId=56
    private const int DiamondBoots   = 313; // itemId=57
    private const int GoldHelmet_    = 314; // itemId=58
    private const int GoldChest_     = 315; // itemId=59
    private const int GoldLegs_      = 316; // itemId=60
    private const int GoldBoots_     = 317; // itemId=61
    private const int SignItem        = 323; // itemId=67
    private const int GoldenAppleItem = 322; // itemId=66
    private const int FlintAndSteelItem = 259; // itemId=3
    private const int BowItem_       = 261;  // itemId=5... duplicate
    private const int PaintingItem   = 321;  // itemId=65 (painting entity is placed via item)
    private const int BucketItem     = 325;  // itemId=69
    private const int MinecartItem   = 328;  // itemId=72
    private const int SaddleItem     = 329;  // itemId=73
    private const int IronDoorItem   = 330;  // itemId=74
    private const int WoodDoorItem   = 324;  // itemId=68
    private const int BlazeRodItem   = 354;  // itemId=98
    private const int BlazePowderItem = 355; // itemId=99
    private const int EnderPearlItem  = 372; // itemId=116
    private const int EyeOfEnderItem  = 381; // itemId=125 (approximate)
    private const int ClockItem       = 347;  // itemId=91
    private const int CompassItem     = 345;  // itemId=89
    private const int DyeItem         = 351;  // itemId=95 (bonemeal=meta15, ink=meta0)
    private const int ShearsItem      = 359;  // itemId=103
    private const int NetherWartItem  = 371;  // itemId=115
    private const int GlassBottleItem = 374;  // itemId=118
    private const int NetherBrickItem = 405;  // itemId=149

    // ── Recipe lookup ─────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the output for the current grid contents (spec §1.2).
    /// Checks tool repair first, then iterates registered recipes.
    /// Returns null if no recipe matches.
    /// </summary>
    public ItemStack? FindMatchingRecipe(CraftingGrid grid)
    {
        // 1 — Tool repair
        var repaired = TryToolRepair(grid);
        if (repaired != null) return repaired;

        // 2 — Iterate recipe list
        foreach (var recipe in _recipes)
        {
            if (recipe.Matches(grid))
                return recipe.GetResult();
        }

        return null;
    }

    // ── Tool repair (spec §1.2) ───────────────────────────────────────────────

    private static ItemStack? TryToolRepair(CraftingGrid grid)
    {
        // Collect all non-empty slots
        ItemStack? a = null, b = null;
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                var s = grid.GetSlot(x, y);
                if (s == null || s.StackSize <= 0) continue;
                if      (a == null) a = s;
                else if (b == null) b = s;
                else                return null; // more than 2 items
            }
        }

        if (a == null || b == null) return null;
        if (a.ItemId != b.ItemId)   return null;
        if (a.StackSize != 1 || b.StackSize != 1) return null;

        var itemDef = Item.ItemsList[a.ItemId];
        if (itemDef == null || itemDef.GetMaxDamage() <= 0) return null;

        int maxDur   = itemDef.GetMaxDamage();
        int remainA  = maxDur - a.Damage;
        int remainB  = maxDur - b.Damage;
        int newRemain = remainA + remainB + maxDur * 10 / 100;
        int newDamage = Math.Max(0, maxDur - newRemain);

        return new ItemStack(a.ItemId, 1, newDamage);
    }

    // ── Recipe builder ────────────────────────────────────────────────────────

    private static List<ICraftingRecipe> BuildRecipes()
    {
        var list = new List<ICraftingRecipe>();

        // Helper: item ID → CraftingIngredient (any damage)
        static CraftingIngredient I(int id) => Any(id);
        // Helper: item ID + exact damage
        static CraftingIngredient IE(int id, int dmg) => Exact(id, dmg);
        // Empty slot
        static CraftingIngredient E() => CraftingIngredient.Empty;
        // Add shaped recipe
        void Shaped(ItemStack result, int w, int h, params CraftingIngredient[] pat)
            => list.Add(new VanillaShapedRecipe(w, h, pat, result));
        // Add shapeless recipe
        void Shapeless(ItemStack result, params CraftingIngredient[] ings)
            => list.Add(new VanillaShapelessRecipe(ings, result));

        // ── Materials / basics ────────────────────────────────────────────────

        // Log → 4 Planks (1×1)
        Shaped(new ItemStack(Planks, 4), 1, 1, I(Log));
        // Planks → 4 Sticks (1×2)
        Shaped(new ItemStack(StickItem, 4), 1, 2, I(Planks), I(Planks));
        // Torch ×4 — coal variant
        Shaped(new ItemStack(50, 4), 1, 2, IE(CoalItem, 0), I(StickItem));
        // Torch ×4 — charcoal variant
        Shaped(new ItemStack(50, 4), 1, 2, IE(CoalItem, 1), I(StickItem));
        // Paper ×3 (1×3 horizontal sugar cane)
        Shaped(new ItemStack(PaperItem, 3), 3, 1, I(SugarCaneItem), I(SugarCaneItem), I(SugarCaneItem));
        // Book (1×3 vertical paper)
        Shaped(new ItemStack(BookItem, 1), 1, 3, I(PaperItem), I(PaperItem), I(PaperItem));

        // ── Storage blocks (4-of-material → block) ───────────────────────────

        // Sandstone ×1 (2×2 sand)
        Shaped(new ItemStack(Sandstone, 1), 2, 2, I(Sand), I(Sand), I(Sand), I(Sand));
        // Glowstone ×1 from 4 glowstone dust (ID 348)
        Shaped(new ItemStack(Glowstone, 1), 2, 2, I(348), I(348), I(348), I(348));
        // Snow Block from 4 snowballs
        Shaped(new ItemStack(80, 1), 2, 2, I(SnowballItem), I(SnowballItem), I(SnowballItem), I(SnowballItem));
        // Clay Block from 4 clay balls
        Shaped(new ItemStack(82, 1), 2, 2, I(ClayBallItem), I(ClayBallItem), I(ClayBallItem), I(ClayBallItem));
        // Brick Block from 4 brick items
        Shaped(new ItemStack(Bricks, 1), 2, 2, I(BrickItem_), I(BrickItem_), I(BrickItem_), I(BrickItem_));

        // ── Ingot blocks (3×3 → block) ───────────────────────────────────────

        // Iron Block
        Shaped(new ItemStack(IronBlock, 1), 3, 3,
            I(IronIngot), I(IronIngot), I(IronIngot),
            I(IronIngot), I(IronIngot), I(IronIngot),
            I(IronIngot), I(IronIngot), I(IronIngot));
        // Gold Block
        Shaped(new ItemStack(GoldBlock, 1), 3, 3,
            I(GoldIngot), I(GoldIngot), I(GoldIngot),
            I(GoldIngot), I(GoldIngot), I(GoldIngot),
            I(GoldIngot), I(GoldIngot), I(GoldIngot));
        // Diamond Block
        Shaped(new ItemStack(DiamondBlock, 1), 3, 3,
            I(DiamondItem), I(DiamondItem), I(DiamondItem),
            I(DiamondItem), I(DiamondItem), I(DiamondItem),
            I(DiamondItem), I(DiamondItem), I(DiamondItem));
        // Lapis Block from 9 lapis dye (meta 4 = blue dye / lapis, ID 351)
        Shaped(new ItemStack(22, 1), 3, 3,
            IE(DyeItem, 4), IE(DyeItem, 4), IE(DyeItem, 4),
            IE(DyeItem, 4), IE(DyeItem, 4), IE(DyeItem, 4),
            IE(DyeItem, 4), IE(DyeItem, 4), IE(DyeItem, 4));

        // ── TNT ───────────────────────────────────────────────────────────────
        // Alternating gunpowder and sand
        Shaped(new ItemStack(TNT, 1), 3, 3,
            I(GunpowderItem), I(Sand),         I(GunpowderItem),
            I(Sand),          I(GunpowderItem), I(Sand),
            I(GunpowderItem), I(Sand),          I(GunpowderItem));

        // ── Wool from 4 string ───────────────────────────────────────────────
        Shaped(new ItemStack(Wool, 1), 2, 2,
            I(StringItem), I(StringItem),
            I(StringItem), I(StringItem));

        // ── Slabs ×3 ─────────────────────────────────────────────────────────
        Shaped(new ItemStack(StoneSlab, 3, 0), 3, 1, I(Stone),      I(Stone),      I(Stone));      // meta 0 = stone
        Shaped(new ItemStack(StoneSlab, 3, 1), 3, 1, I(Sandstone),  I(Sandstone),  I(Sandstone));  // meta 1 = sandstone
        Shaped(new ItemStack(StoneSlab, 3, 2), 3, 1, I(Planks),     I(Planks),     I(Planks));     // meta 2 = wood
        Shaped(new ItemStack(StoneSlab, 3, 3), 3, 1, I(Cobblestone),I(Cobblestone),I(Cobblestone));// meta 3 = cobblestone
        Shaped(new ItemStack(StoneSlab, 3, 4), 3, 1, I(Bricks),     I(Bricks),     I(Bricks));     // meta 4 = brick
        Shaped(new ItemStack(StoneSlab, 3, 5), 3, 1, I(StoneBrick), I(StoneBrick), I(StoneBrick)); // meta 5 = stone brick

        // ── Stairs ×4 ────────────────────────────────────────────────────────
        // Oak stairs (planks)
        Shaped(new ItemStack(OakStairs, 4), 3, 3,
            I(Planks),     E(),           E(),
            I(Planks),     I(Planks),     E(),
            I(Planks),     I(Planks),     I(Planks));
        // Stone (cobblestone) stairs
        Shaped(new ItemStack(StoneStairs, 4), 3, 3,
            I(Cobblestone), E(),            E(),
            I(Cobblestone), I(Cobblestone), E(),
            I(Cobblestone), I(Cobblestone), I(Cobblestone));
        // Brick stairs
        Shaped(new ItemStack(BrickStairs, 4), 3, 3,
            I(Bricks),  E(),       E(),
            I(Bricks),  I(Bricks), E(),
            I(Bricks),  I(Bricks), I(Bricks));
        // Stone brick stairs
        Shaped(new ItemStack(StoneBrickStairs, 4), 3, 3,
            I(StoneBrick), E(),          E(),
            I(StoneBrick), I(StoneBrick), E(),
            I(StoneBrick), I(StoneBrick), I(StoneBrick));
        // Nether brick stairs (ID 114)
        Shaped(new ItemStack(114, 4), 3, 3,
            I(NetherBrick), E(),            E(),
            I(NetherBrick), I(NetherBrick), E(),
            I(NetherBrick), I(NetherBrick), I(NetherBrick));

        // ── Fences ───────────────────────────────────────────────────────────
        // Wood fence ×2 (planks+stick pattern)
        Shaped(new ItemStack(Fence, 2), 3, 2,
            I(Planks),   I(StickItem), I(Planks),
            I(Planks),   I(StickItem), I(Planks));
        // Nether brick fence ×6 (3×2 nether brick)
        Shaped(new ItemStack(NetherBrickFence, 6), 3, 2,
            I(NetherBrick), I(NetherBrick), I(NetherBrick),
            I(NetherBrick), I(NetherBrick), I(NetherBrick));

        // ── Fence Gate ───────────────────────────────────────────────────────
        Shaped(new ItemStack(FenceGate, 1), 3, 2,
            I(StickItem), I(Planks), I(StickItem),
            I(StickItem), I(Planks), I(StickItem));

        // ── Rails ────────────────────────────────────────────────────────────
        // Normal Rail ×16 (6 iron ingots + 1 stick)
        Shaped(new ItemStack(Rail, 16), 3, 3,
            I(IronIngot), E(),           I(IronIngot),
            I(IronIngot), I(StickItem),  I(IronIngot),
            I(IronIngot), E(),           I(IronIngot));
        // Powered Rail ×6 (6 gold ingots + 1 stick + 1 redstone)
        Shaped(new ItemStack(PoweredRail, 6), 3, 3,
            I(GoldIngot), E(),            I(GoldIngot),
            I(GoldIngot), I(StickItem),   I(GoldIngot),
            I(GoldIngot), I(RedstoneItem),I(GoldIngot));
        // Detector Rail ×6 (6 iron ingots + stone pressure plate + redstone)
        Shaped(new ItemStack(DetectorRail, 6), 3, 3,
            I(IronIngot), E(),            I(IronIngot),
            I(IronIngot), I(70),          I(IronIngot),  // 70 = stone pressure plate block
            I(IronIngot), I(RedstoneItem),I(IronIngot));

        // ── Transport / crafting structures ──────────────────────────────────
        // Minecart ×1 (U-shape 5 iron ingots)
        Shaped(new ItemStack(MinecartItem, 1), 3, 2,
            I(IronIngot), E(),         I(IronIngot),
            I(IronIngot), I(IronIngot),I(IronIngot));
        // Boat (U-shape 5 planks)
        Shaped(new ItemStack(333, 1), 3, 2,
            I(Planks), E(),      I(Planks),
            I(Planks), I(Planks),I(Planks));
        // Workbench / Crafting Table
        Shaped(new ItemStack(Workbench, 1), 2, 2,
            I(Planks), I(Planks),
            I(Planks), I(Planks));
        // Chest (8 planks, 3×3 hollow)
        Shaped(new ItemStack(Chest, 1), 3, 3,
            I(Planks), I(Planks), I(Planks),
            I(Planks), E(),       I(Planks),
            I(Planks), I(Planks), I(Planks));
        // Furnace (8 cobblestone, 3×3 hollow)
        Shaped(new ItemStack(Furnace, 1), 3, 3,
            I(Cobblestone), I(Cobblestone), I(Cobblestone),
            I(Cobblestone), E(),             I(Cobblestone),
            I(Cobblestone), I(Cobblestone), I(Cobblestone));
        // Dispenser (8 cobblestone hollow + bow in center)
        Shaped(new ItemStack(Dispenser, 1), 3, 3,
            I(Cobblestone), I(Cobblestone), I(Cobblestone),
            I(Cobblestone), I(BowItem),     I(Cobblestone),
            I(Cobblestone), I(Cobblestone), I(Cobblestone));

        // ── Functional blocks ─────────────────────────────────────────────────
        // Bed (3 wool + 3 planks)
        Shaped(new ItemStack(Bed, 1), 3, 2,
            I(Wool), I(Wool), I(Wool),
            I(Planks), I(Planks), I(Planks));
        // Note block (8 planks + 1 redstone)
        Shaped(new ItemStack(NoteBlock, 1), 3, 3,
            I(Planks), I(Planks), I(Planks),
            I(Planks), I(RedstoneItem), I(Planks),
            I(Planks), I(Planks), I(Planks));
        // Jukebox (8 planks + 1 diamond)
        Shaped(new ItemStack(Jukebox, 1), 3, 3,
            I(Planks), I(Planks),     I(Planks),
            I(Planks), I(DiamondItem),I(Planks),
            I(Planks), I(Planks),     I(Planks));
        // Bookshelf (6 planks + 3 books)
        Shaped(new ItemStack(Bookshelf, 1), 3, 3,
            I(Planks), I(Planks), I(Planks),
            I(BookItem),I(BookItem),I(BookItem),
            I(Planks), I(Planks), I(Planks));

        // ── Redstone ─────────────────────────────────────────────────────────
        // Redstone Torch (redstone + stick)
        Shaped(new ItemStack(76, 1), 1, 2, I(RedstoneItem), I(StickItem));
        // Lever (stick + cobblestone)
        Shaped(new ItemStack(Lever, 1), 1, 2, I(StickItem), I(Cobblestone));
        // Stone Button (1×1 stone)
        Shaped(new ItemStack(77, 1), 1, 1, I(Stone));
        // Stone Pressure Plate (2 stone)
        Shaped(new ItemStack(70, 1), 2, 1, I(Stone), I(Stone));
        // Wood Pressure Plate (2 planks)
        Shaped(new ItemStack(72, 1), 2, 1, I(Planks), I(Planks));
        // Compass (4 iron + 1 redstone)
        Shaped(new ItemStack(CompassItem, 1), 3, 3,
            E(),           I(IronIngot), E(),
            I(IronIngot),  I(RedstoneItem), I(IronIngot),
            E(),           I(IronIngot), E());
        // Clock (4 gold + 1 redstone)
        Shaped(new ItemStack(ClockItem, 1), 3, 3,
            E(),           I(GoldIngot), E(),
            I(GoldIngot),  I(RedstoneItem), I(GoldIngot),
            E(),           I(GoldIngot), E());
        // Piston (3 planks + 4 cobblestone + 1 iron + 1 redstone)
        Shaped(new ItemStack(Piston, 1), 3, 3,
            I(Planks),     I(Planks),     I(Planks),
            I(Cobblestone),I(IronIngot),  I(Cobblestone),
            I(Cobblestone),I(RedstoneItem),I(Cobblestone));
        // Sticky Piston (slimeball + piston)
        Shaped(new ItemStack(StickyPiston, 1), 1, 2, I(SlimeballItem), I(Piston));
        // Ladder ×3 (7 sticks)
        Shaped(new ItemStack(65, 3), 3, 3,
            I(StickItem), E(),         I(StickItem),
            I(StickItem), I(StickItem),I(StickItem),
            I(StickItem), E(),         I(StickItem));
        // Iron Bars ×16 (6 iron ingots in 3×2)
        Shaped(new ItemStack(IronBars, 16), 3, 2,
            I(IronIngot), I(IronIngot), I(IronIngot),
            I(IronIngot), I(IronIngot), I(IronIngot));
        // Glass Pane ×16 (6 glass in 3×2)
        Shaped(new ItemStack(GlassPane, 16), 3, 2,
            I(Glass), I(Glass), I(Glass),
            I(Glass), I(Glass), I(Glass));
        // Iron Door ×1 (6 iron ingots in 2×3)
        Shaped(new ItemStack(IronDoorItem, 1), 2, 3,
            I(IronIngot), I(IronIngot),
            I(IronIngot), I(IronIngot),
            I(IronIngot), I(IronIngot));
        // Wood Door ×1 (6 planks in 2×3)
        Shaped(new ItemStack(WoodDoorItem, 1), 2, 3,
            I(Planks), I(Planks),
            I(Planks), I(Planks),
            I(Planks), I(Planks));

        // ── Tools ────────────────────────────────────────────────────────────
        // Flint and Steel (iron ingot + flint, diagonal)
        Shaped(new ItemStack(FlintAndSteelItem, 1), 2, 2,
            I(IronIngot), E(),
            E(),          I(FlintItem));
        // Bow (3 string + 3 sticks)
        Shaped(new ItemStack(BowItem, 1), 3, 3,
            E(),          I(StickItem),  I(StringItem),
            I(StickItem), E(),           I(StringItem),
            E(),          I(StickItem),  I(StringItem));
        // Arrow ×4 (flint + stick + feather)
        Shaped(new ItemStack(ArrowItem, 4), 1, 3,
            I(FlintItem), I(StickItem), I(FeatherItem));
        // Fishing Rod (3 sticks + 2 string)
        Shaped(new ItemStack(346 + 256, 1), 3, 3,       // 346+256? No, fishing rod is itemId=346... wait
            E(),           E(),           I(StickItem),
            E(),           I(StickItem),  I(StringItem),
            I(StickItem),  E(),           I(StringItem));
        // Shears (2 iron ingots diagonal)
        Shaped(new ItemStack(ShearsItem, 1), 2, 2,
            E(),           I(IronIngot),
            I(IronIngot),  E());
        // Sign ×3 (6 planks + 1 stick)
        Shaped(new ItemStack(SignItem, 3), 3, 3,
            I(Planks), I(Planks), I(Planks),
            I(Planks), I(Planks), I(Planks),
            E(),       I(StickItem), E());
        // Painting (8 sticks + 1 wool)
        Shaped(new ItemStack(PaintingItem, 1), 3, 3,
            I(StickItem), I(StickItem), I(StickItem),
            I(StickItem), I(Wool),      I(StickItem),
            I(StickItem), I(StickItem), I(StickItem));
        // Golden Apple (8 gold blocks + 1 apple)
        Shaped(new ItemStack(GoldenAppleItem, 1), 3, 3,
            I(GoldBlock), I(GoldBlock), I(GoldBlock),
            I(GoldBlock), I(256 + 4),  I(GoldBlock),   // apple = itemId=4 → 256+4=260
            I(GoldBlock), I(GoldBlock), I(GoldBlock));

        // ── Wood tools / swords ───────────────────────────────────────────────
        AddToolSet(list, Planks, WoodSword, WoodShovel, WoodPickaxe, WoodAxe, WoodHoe);
        // ── Stone tools ───────────────────────────────────────────────────────
        AddToolSet(list, Cobblestone, StoneSword, StoneShovel, StonePickaxe, StoneAxe, StoneHoe);
        // ── Iron tools ────────────────────────────────────────────────────────
        AddToolSet(list, IronIngot, IronSword, IronShovel_, IronPickaxe_, IronAxe_, IronHoe);
        // ── Diamond tools ─────────────────────────────────────────────────────
        AddToolSet(list, DiamondItem, DiamondSword, DiamondShovel, DiamondPickaxe, DiamondAxe, DiamondHoe);
        // ── Gold tools ────────────────────────────────────────────────────────
        AddToolSet(list, GoldIngot, GoldSword, GoldShovel, GoldPickaxe, GoldAxe, GoldHoe);

        // ── Armor ─────────────────────────────────────────────────────────────
        AddArmorSet(list, LeatherItem,  LeatherHelmet, LeatherChest, LeatherLegs, LeatherBoots);
        AddArmorSet(list, IronIngot,    IronHelmet,    IronChest,    IronLegs,    IronBoots);
        AddArmorSet(list, DiamondItem,  DiamondHelmet, DiamondChest, DiamondLegs, DiamondBoots);
        AddArmorSet(list, GoldIngot,    GoldHelmet_,   GoldChest_,   GoldLegs_,   GoldBoots_);

        // ── Food / misc ───────────────────────────────────────────────────────
        // Bread (3 wheat)
        list.Add(new VanillaShapedRecipe(3, 1,
            new[] { Any(WheatItem), Any(WheatItem), Any(WheatItem) },
            new ItemStack(BreadItem, 1)));
        // Sugar (1×1 sugar cane → sugar)
        list.Add(new VanillaShapedRecipe(1, 1,
            new[] { Any(SugarCaneItem) },
            new ItemStack(353, 1)));  // sugar = ID 353

        // ── Eye of Ender (shapeless: ender pearl + blaze powder) ──────────────
        Shapeless(new ItemStack(EyeOfEnderItem, 1), I(EnderPearlItem), I(BlazePowderItem));

        return list;
    }

    // ── Tool/armor pattern helpers ────────────────────────────────────────────

    private static void AddToolSet(List<ICraftingRecipe> list,
        int material, int sword, int shovel, int pickaxe, int axe, int hoe)
    {
        static CraftingIngredient I(int id) => Any(id);
        static CraftingIngredient E() => CraftingIngredient.Empty;
        int S = StickItem;

        // Sword: 2 material + 1 stick (1-wide, 3-tall)
        list.Add(new VanillaShapedRecipe(1, 3,
            new[] { I(material), I(material), I(S) },
            new ItemStack(sword, 1)));

        // Shovel: 1 material + 2 sticks (1-wide, 3-tall)
        list.Add(new VanillaShapedRecipe(1, 3,
            new[] { I(material), I(S), I(S) },
            new ItemStack(shovel, 1)));

        // Pickaxe: 3 material top, 2 sticks below (3-wide, 3-tall)
        list.Add(new VanillaShapedRecipe(3, 3,
            new[] { I(material), I(material), I(material),
                    E(),         I(S),         E(),
                    E(),         I(S),         E() },
            new ItemStack(pickaxe, 1)));

        // Axe: 2×3 shape (2-wide, 3-tall)
        list.Add(new VanillaShapedRecipe(2, 3,
            new[] { I(material), I(material),
                    I(material), I(S),
                    E(),         I(S) },
            new ItemStack(axe, 1)));

        // Hoe: 2×3 shape
        list.Add(new VanillaShapedRecipe(2, 3,
            new[] { I(material), I(material),
                    E(),         I(S),
                    E(),         I(S) },
            new ItemStack(hoe, 1)));
    }

    private static void AddArmorSet(List<ICraftingRecipe> list,
        int material, int helmet, int chest, int legs, int boots)
    {
        static CraftingIngredient I(int id) => Any(id);
        static CraftingIngredient E() => CraftingIngredient.Empty;

        // Helmet: 5 material (3-wide top, 2 sides second row)
        list.Add(new VanillaShapedRecipe(3, 2,
            new[] { I(material), I(material), I(material),
                    I(material), E(),          I(material) },
            new ItemStack(helmet, 1)));

        // Chestplate: 8 material (sides + 3×2 bottom, hole top-middle)
        list.Add(new VanillaShapedRecipe(3, 3,
            new[] { I(material), E(),          I(material),
                    I(material), I(material),   I(material),
                    I(material), I(material),   I(material) },
            new ItemStack(chest, 1)));

        // Leggings: 7 material
        list.Add(new VanillaShapedRecipe(3, 3,
            new[] { I(material), I(material), I(material),
                    I(material), E(),          I(material),
                    I(material), E(),          I(material) },
            new ItemStack(legs, 1)));

        // Boots: 4 material (2×2 with top-middle holes)
        list.Add(new VanillaShapedRecipe(2, 2,
            new[] { I(material), I(material),
                    I(material), I(material) },
            new ItemStack(boots, 1)));
    }

}
