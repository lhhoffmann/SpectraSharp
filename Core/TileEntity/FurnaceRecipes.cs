namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Singleton smelting recipe table. Replica of <c>mt</c> (FurnaceRecipes).
///
/// <c>mt.a()</c> returns the singleton. <c>mt.a(inputItemId)</c> returns the output
/// <see cref="ItemStack"/> or null.
///
/// Item IDs used are standard Minecraft 1.0 values. Block-output recipes use the
/// block ID directly (e.g., Glass = block 20). Item-output recipes use the item's
/// registry index (e.g., Iron Ingot = 265 = 256+9).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §5.5
/// </summary>
public sealed class FurnaceRecipes
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static readonly FurnaceRecipes Instance = new();

    private FurnaceRecipes() { }

    // ── Recipe table (input itemId → output ItemStack) ────────────────────────

    private static readonly Dictionary<int, ItemStack> _recipes = new()
    {
        // Iron Ore (15) → Iron Ingot (265)
        [15]  = new ItemStack(265, 1, 0),
        // Gold Ore (14) → Gold Ingot (266)
        [14]  = new ItemStack(266, 1, 0),
        // Sand (12) → Glass block (20)
        [12]  = new ItemStack(20, 1, 0),
        // Clay block (82) → Brick block (45)
        [82]  = new ItemStack(45, 1, 0),
        // Raw Porkchop (319) → Cooked Porkchop (320)
        [319] = new ItemStack(320, 1, 0),
        // Raw Fish (349) → Cooked Fish (350)
        [349] = new ItemStack(350, 1, 0),
        // Raw Chicken (365) → Cooked Chicken (366)
        [365] = new ItemStack(366, 1, 0),
        // Raw Beef (363) → Steak (364)
        [363] = new ItemStack(364, 1, 0),
        // Wood log (17) → Charcoal (Coal item 263, damage=1)
        [17]  = new ItemStack(263, 1, 1),
        // Potato (392) → Baked Potato (393)
        [392] = new ItemStack(393, 1, 0),
        // Cactus (81) → Green Dye (item 351, damage=2, count=2)
        [81]  = new ItemStack(351, 2, 2),
        // Netherrack (87) → Nether Brick item (405)
        [87]  = new ItemStack(405, 1, 0),
    };

    // ── API ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the smelting result for <paramref name="inputItemId"/>, or null if
    /// no recipe exists. Spec: <c>mt.a(int inputItemId)</c>.
    /// The returned stack is a prototype — callers must Copy() before mutating.
    /// </summary>
    public ItemStack? GetSmeltingResult(int inputItemId)
        => _recipes.TryGetValue(inputItemId, out ItemStack? result) ? result : null;
}
