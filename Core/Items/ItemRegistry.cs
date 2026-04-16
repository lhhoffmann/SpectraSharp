namespace SpectraEngine.Core.Items;

/// <summary>
/// Registers all tool, sword, hoe, and armor items into <see cref="Item.ItemsList"/>.
///
/// Each static readonly field auto-registers its item via the <see cref="Item(int)"/> constructor.
/// Access any field to trigger static initialization (call <see cref="Initialize"/> at startup).
///
/// Item ID convention: registryIndex = 256 + itemId, matching acy.bM.
/// All IDs from spec §14 table.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemTool_Spec.md §14
/// </summary>
public static class ItemRegistry
{
    private static bool _initialized;

    /// <summary>
    /// Forces static field initialization, registering all items.
    /// Must be called after <see cref="BlockRegistry.Initialize"/> (tool arrays reference blocks).
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        // Touch a field to trigger static initializer
        _ = IronShovel;
    }

    // ── Iron tools (IDs 0-2, 11) ─────────────────────────────────────────────

    /// <summary>Iron Shovel — itemId=0, tex=(2,5). obf: acy.e.</summary>
    public static readonly ItemShovel IronShovel
        = (ItemShovel)new ItemShovel(0, EnumToolMaterial.Iron).SetIcon(2, 5);

    /// <summary>Iron Pickaxe — itemId=1, tex=(2,6). obf: acy.f.</summary>
    public static readonly ItemPickaxe IronPickaxe
        = (ItemPickaxe)new ItemPickaxe(1, EnumToolMaterial.Iron).SetIcon(2, 6);

    /// <summary>Iron Axe — itemId=2, tex=(2,7). obf: acy.g.</summary>
    public static readonly ItemAxe IronAxe
        = (ItemAxe)new ItemAxe(2, EnumToolMaterial.Iron).SetIcon(2, 7);

    /// <summary>Iron Sword — itemId=11, tex=(2,4). obf: acy.p.</summary>
    public static readonly ItemSword IronSword
        = (ItemSword)new ItemSword(11, EnumToolMaterial.Iron).SetIcon(2, 4);

    // ── Wood tools (IDs 12-15) ───────────────────────────────────────────────

    /// <summary>Wood Sword — itemId=12, tex=(0,4). obf: acy.q.</summary>
    public static readonly ItemSword WoodSword
        = (ItemSword)new ItemSword(12, EnumToolMaterial.Wood).SetIcon(0, 4);

    /// <summary>Wood Shovel — itemId=13, tex=(0,5). obf: acy.r.</summary>
    public static readonly ItemShovel WoodShovel
        = (ItemShovel)new ItemShovel(13, EnumToolMaterial.Wood).SetIcon(0, 5);

    /// <summary>Wood Pickaxe — itemId=14, tex=(0,6). obf: acy.s.</summary>
    public static readonly ItemPickaxe WoodPickaxe
        = (ItemPickaxe)new ItemPickaxe(14, EnumToolMaterial.Wood).SetIcon(0, 6);

    /// <summary>Wood Axe — itemId=15, tex=(0,7). obf: acy.t.</summary>
    public static readonly ItemAxe WoodAxe
        = (ItemAxe)new ItemAxe(15, EnumToolMaterial.Wood).SetIcon(0, 7);

    // ── Stone tools (IDs 16-19) ──────────────────────────────────────────────

    /// <summary>Stone Sword — itemId=16, tex=(1,4). obf: acy.u.</summary>
    public static readonly ItemSword StoneSword
        = (ItemSword)new ItemSword(16, EnumToolMaterial.Stone).SetIcon(1, 4);

    /// <summary>Stone Shovel — itemId=17, tex=(1,5). obf: acy.v.</summary>
    public static readonly ItemShovel StoneShovel
        = (ItemShovel)new ItemShovel(17, EnumToolMaterial.Stone).SetIcon(1, 5);

    /// <summary>Stone Pickaxe — itemId=18, tex=(1,6). obf: acy.w.</summary>
    public static readonly ItemPickaxe StonePickaxe
        = (ItemPickaxe)new ItemPickaxe(18, EnumToolMaterial.Stone).SetIcon(1, 6);

    /// <summary>Stone Axe — itemId=19, tex=(1,7). obf: acy.x.</summary>
    public static readonly ItemAxe StoneAxe
        = (ItemAxe)new ItemAxe(19, EnumToolMaterial.Stone).SetIcon(1, 7);

    // ── Diamond tools (IDs 20-23) ────────────────────────────────────────────

    /// <summary>Diamond Sword — itemId=20, tex=(3,4). obf: acy.y.</summary>
    public static readonly ItemSword DiamondSword
        = (ItemSword)new ItemSword(20, EnumToolMaterial.Diamond).SetIcon(3, 4);

    /// <summary>Diamond Shovel — itemId=21, tex=(3,5). obf: acy.z.</summary>
    public static readonly ItemShovel DiamondShovel
        = (ItemShovel)new ItemShovel(21, EnumToolMaterial.Diamond).SetIcon(3, 5);

    /// <summary>Diamond Pickaxe — itemId=22, tex=(3,6). obf: acy.A.</summary>
    public static readonly ItemPickaxe DiamondPickaxe
        = (ItemPickaxe)new ItemPickaxe(22, EnumToolMaterial.Diamond).SetIcon(3, 6);

    /// <summary>Diamond Axe — itemId=23, tex=(3,7). obf: acy.B.</summary>
    public static readonly ItemAxe DiamondAxe
        = (ItemAxe)new ItemAxe(23, EnumToolMaterial.Diamond).SetIcon(3, 7);

    // ── Gold tools (IDs 27-30) ───────────────────────────────────────────────

    /// <summary>Gold Sword — itemId=27, tex=(4,4). obf: acy.F.</summary>
    public static readonly ItemSword GoldSword
        = (ItemSword)new ItemSword(27, EnumToolMaterial.Gold).SetIcon(4, 4);

    /// <summary>Gold Shovel — itemId=28, tex=(4,5). obf: acy.G.</summary>
    public static readonly ItemShovel GoldShovel
        = (ItemShovel)new ItemShovel(28, EnumToolMaterial.Gold).SetIcon(4, 5);

    /// <summary>Gold Pickaxe — itemId=29, tex=(4,6). obf: acy.H.</summary>
    public static readonly ItemPickaxe GoldPickaxe
        = (ItemPickaxe)new ItemPickaxe(29, EnumToolMaterial.Gold).SetIcon(4, 6);

    /// <summary>Gold Axe — itemId=30, tex=(4,7). obf: acy.I.</summary>
    public static readonly ItemAxe GoldAxe
        = (ItemAxe)new ItemAxe(30, EnumToolMaterial.Gold).SetIcon(4, 7);

    // ── Hoes (IDs 34-38) ────────────────────────────────────────────────────

    /// <summary>Wood Hoe — itemId=34, tex=(0,8). obf: acy.M.</summary>
    public static readonly ItemHoe WoodHoe
        = (ItemHoe)new ItemHoe(34, EnumToolMaterial.Wood).SetIcon(0, 8);

    /// <summary>Stone Hoe — itemId=35, tex=(1,8). obf: acy.N.</summary>
    public static readonly ItemHoe StoneHoe
        = (ItemHoe)new ItemHoe(35, EnumToolMaterial.Stone).SetIcon(1, 8);

    /// <summary>Iron Hoe — itemId=36, tex=(2,8). obf: acy.O.</summary>
    public static readonly ItemHoe IronHoe
        = (ItemHoe)new ItemHoe(36, EnumToolMaterial.Iron).SetIcon(2, 8);

    /// <summary>Diamond Hoe — itemId=37, tex=(3,8). obf: acy.P.</summary>
    public static readonly ItemHoe DiamondHoe
        = (ItemHoe)new ItemHoe(37, EnumToolMaterial.Diamond).SetIcon(3, 8);

    /// <summary>Gold Hoe — itemId=38, tex=(4,8). obf: acy.Q.</summary>
    public static readonly ItemHoe GoldHoe
        = (ItemHoe)new ItemHoe(38, EnumToolMaterial.Gold).SetIcon(4, 8);

    // ── Leather armor (IDs 42-45) ────────────────────────────────────────────

    /// <summary>Leather Helmet — itemId=42, tex=(0,0). obf: acy.U.</summary>
    public static readonly ItemArmor LeatherHelmet
        = (ItemArmor)new ItemArmor(42, EnumArmorMaterial.Leather, 0, 0).SetIcon(0, 0);

    /// <summary>Leather Chestplate — itemId=43, tex=(0,1). obf: acy.V.</summary>
    public static readonly ItemArmor LeatherChest
        = (ItemArmor)new ItemArmor(43, EnumArmorMaterial.Leather, 0, 1).SetIcon(0, 1);

    /// <summary>Leather Leggings — itemId=44, tex=(0,2). obf: acy.W.</summary>
    public static readonly ItemArmor LeatherLegs
        = (ItemArmor)new ItemArmor(44, EnumArmorMaterial.Leather, 0, 2).SetIcon(0, 2);

    /// <summary>Leather Boots — itemId=45, tex=(0,3). obf: acy.X.</summary>
    public static readonly ItemArmor LeatherBoots
        = (ItemArmor)new ItemArmor(45, EnumArmorMaterial.Leather, 0, 3).SetIcon(0, 3);

    // ── Chain armor (IDs 46-49) ──────────────────────────────────────────────

    /// <summary>Chain Helmet — itemId=46, tex=(1,0). obf: acy.Y.</summary>
    public static readonly ItemArmor ChainHelmet
        = (ItemArmor)new ItemArmor(46, EnumArmorMaterial.Chain, 1, 0).SetIcon(1, 0);

    /// <summary>Chain Chestplate — itemId=47, tex=(1,1). obf: acy.Z.</summary>
    public static readonly ItemArmor ChainChest
        = (ItemArmor)new ItemArmor(47, EnumArmorMaterial.Chain, 1, 1).SetIcon(1, 1);

    /// <summary>Chain Leggings — itemId=48, tex=(1,2). obf: acy.aa.</summary>
    public static readonly ItemArmor ChainLegs
        = (ItemArmor)new ItemArmor(48, EnumArmorMaterial.Chain, 1, 2).SetIcon(1, 2);

    /// <summary>Chain Boots — itemId=49, tex=(1,3). obf: acy.ab.</summary>
    public static readonly ItemArmor ChainBoots
        = (ItemArmor)new ItemArmor(49, EnumArmorMaterial.Chain, 1, 3).SetIcon(1, 3);

    // ── Iron armor (IDs 50-53) ───────────────────────────────────────────────

    /// <summary>Iron Helmet — itemId=50, tex=(2,0). obf: acy.ac.</summary>
    public static readonly ItemArmor IronHelmet
        = (ItemArmor)new ItemArmor(50, EnumArmorMaterial.IronMat, 2, 0).SetIcon(2, 0);

    /// <summary>Iron Chestplate — itemId=51, tex=(2,1). obf: acy.ad.</summary>
    public static readonly ItemArmor IronChest
        = (ItemArmor)new ItemArmor(51, EnumArmorMaterial.IronMat, 2, 1).SetIcon(2, 1);

    /// <summary>Iron Leggings — itemId=52, tex=(2,2). obf: acy.ae.</summary>
    public static readonly ItemArmor IronLegs
        = (ItemArmor)new ItemArmor(52, EnumArmorMaterial.IronMat, 2, 2).SetIcon(2, 2);

    /// <summary>Iron Boots — itemId=53, tex=(2,3). obf: acy.af.</summary>
    public static readonly ItemArmor IronBoots
        = (ItemArmor)new ItemArmor(53, EnumArmorMaterial.IronMat, 2, 3).SetIcon(2, 3);

    // ── Diamond armor (IDs 54-57) ────────────────────────────────────────────

    /// <summary>Diamond Helmet — itemId=54, tex=(3,0). obf: acy.ag.</summary>
    public static readonly ItemArmor DiamondHelmet
        = (ItemArmor)new ItemArmor(54, EnumArmorMaterial.Diamond, 3, 0).SetIcon(3, 0);

    /// <summary>Diamond Chestplate — itemId=55, tex=(3,1). obf: acy.ah.</summary>
    public static readonly ItemArmor DiamondChest
        = (ItemArmor)new ItemArmor(55, EnumArmorMaterial.Diamond, 3, 1).SetIcon(3, 1);

    /// <summary>Diamond Leggings — itemId=56, tex=(3,2). obf: acy.ai.</summary>
    public static readonly ItemArmor DiamondLegs
        = (ItemArmor)new ItemArmor(56, EnumArmorMaterial.Diamond, 3, 2).SetIcon(3, 2);

    /// <summary>Diamond Boots — itemId=57, tex=(3,3). obf: acy.aj.</summary>
    public static readonly ItemArmor DiamondBoots
        = (ItemArmor)new ItemArmor(57, EnumArmorMaterial.Diamond, 3, 3).SetIcon(3, 3);

    // ── Music discs (IDs 2000-2010 → registryIndex 2256-2266) ───────────────
    // obf: acy.bB through acy.bL. "wait" (ID 2011) is absent in 1.0 (spec §7.3).

    /// <summary>Music Disc 13 — itemId=2000, tex=(0,15). obf: acy.bB.</summary>
    public static readonly ItemRecord Disc13
        = (ItemRecord)new ItemRecord(2000, "13").SetIcon(0, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Cat — itemId=2001, tex=(1,15). obf: acy.bC.</summary>
    public static readonly ItemRecord DiscCat
        = (ItemRecord)new ItemRecord(2001, "cat").SetIcon(1, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Blocks — itemId=2002, tex=(2,15). obf: acy.bD.</summary>
    public static readonly ItemRecord DiscBlocks
        = (ItemRecord)new ItemRecord(2002, "blocks").SetIcon(2, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Chirp — itemId=2003, tex=(3,15). obf: acy.bE.</summary>
    public static readonly ItemRecord DiscChirp
        = (ItemRecord)new ItemRecord(2003, "chirp").SetIcon(3, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Far — itemId=2004, tex=(4,15). obf: acy.bF.</summary>
    public static readonly ItemRecord DiscFar
        = (ItemRecord)new ItemRecord(2004, "far").SetIcon(4, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Mall — itemId=2005, tex=(5,15). obf: acy.bG.</summary>
    public static readonly ItemRecord DiscMall
        = (ItemRecord)new ItemRecord(2005, "mall").SetIcon(5, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Mellohi — itemId=2006, tex=(6,15). obf: acy.bH.</summary>
    public static readonly ItemRecord DiscMellohi
        = (ItemRecord)new ItemRecord(2006, "mellohi").SetIcon(6, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Stal — itemId=2007, tex=(7,15). obf: acy.bI.</summary>
    public static readonly ItemRecord DiscStal
        = (ItemRecord)new ItemRecord(2007, "stal").SetIcon(7, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Strad — itemId=2008, tex=(8,15). obf: acy.bJ.</summary>
    public static readonly ItemRecord DiscStrad
        = (ItemRecord)new ItemRecord(2008, "strad").SetIcon(8, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc Ward — itemId=2009, tex=(9,15). obf: acy.bK.</summary>
    public static readonly ItemRecord DiscWard
        = (ItemRecord)new ItemRecord(2009, "ward").SetIcon(9, 15).SetUnlocalizedName("record");

    /// <summary>Music Disc 11 — itemId=2010, tex=(10,15). obf: acy.bL.</summary>
    public static readonly ItemRecord Disc11
        = (ItemRecord)new ItemRecord(2010, "11").SetIcon(10, 15).SetUnlocalizedName("record");

    // ── Gold armor (IDs 58-61) ────────────────────────────────────────────────

    /// <summary>Gold Helmet — itemId=58, tex=(4,0). Estimated ID from spec §14 note.</summary>
    public static readonly ItemArmor GoldHelmet
        = (ItemArmor)new ItemArmor(58, EnumArmorMaterial.GoldMat, 4, 0).SetIcon(4, 0);

    /// <summary>Gold Chestplate — itemId=59, tex=(4,1).</summary>
    public static readonly ItemArmor GoldChest
        = (ItemArmor)new ItemArmor(59, EnumArmorMaterial.GoldMat, 4, 1).SetIcon(4, 1);

    /// <summary>Gold Leggings — itemId=60, tex=(4,2).</summary>
    public static readonly ItemArmor GoldLegs
        = (ItemArmor)new ItemArmor(60, EnumArmorMaterial.GoldMat, 4, 2).SetIcon(4, 2);

    /// <summary>Gold Boots — itemId=61, tex=(4,3).</summary>
    public static readonly ItemArmor GoldBoots
        = (ItemArmor)new ItemArmor(61, EnumArmorMaterial.GoldMat, 4, 3).SetIcon(4, 3);

    // ── Flint and Steel (ID 259) ──────────────────────────────────────────────

    /// <summary>
    /// Flint and Steel — itemId=259, durability=64, stack=1.
    /// obf: acy.aj. Source spec: BlockPortal_Spec §6
    /// </summary>
    public static readonly ItemFlintAndSteel FlintAndSteel
        = (ItemFlintAndSteel)new ItemFlintAndSteel(259).SetIcon(5, 0).SetUnlocalizedName("flintAndSteel");
}
