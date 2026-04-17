using SpectraEngine.Core.Mobs;

namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>xv</c> (ItemDye) — 16 dye variants (metadata 0–15).
///
/// Item ID: 351 (itemId=95). Stack size: 64.
/// Meta 15 = bonemeal — triggers instant growth on various plant blocks.
/// Other metas = dye colors applied to sheep via entity interaction.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemDye_Spec.md
/// </summary>
public sealed class ItemDye : Item
{
    // ── Color name / RGB tables (spec §2) ────────────────────────────────────

    /// <summary>obf: <c>a[]</c> — color name keys by meta 0–15.</summary>
    public static readonly string[] DyeNames =
    [
        "black", "red", "green", "brown", "blue", "purple", "cyan",
        "silver", "gray", "pink", "lime", "yellow", "lightBlue",
        "magenta", "orange", "white"
    ];

    /// <summary>obf: <c>b[]</c> — packed RGB ints by meta 0–15.</summary>
    public static readonly int[] DyeColors =
    [
        0x1E1B1B, 0xB3312C, 0x3B511A, 0x51301A, 0x253192, 0x7B2FBE, 0x287697,
        0x287697, // meta 7 (silver) shares value with meta 6 (cyan) — intentional per spec
        0x434343, 0xD88198, 0x41CD34, 0xDECD87, 0x6689D3, 0xC354CD, 0xEB8844, 0xF0F0F0
    ];

    public ItemDye() : base(95) // RegistryIndex = 351
    {
        MaxStackSize = 64;
        SetUnlocalizedName("dyePowder");
        HasSubtypes = true;
    }

    // ── Texture (spec §3) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int meta)</c> — texture index = bO + (meta % 8) * 16 + meta / 8.
    /// </summary>
    public override int GetIconIndex(int meta)
        => IconIndex + (meta % 8) * 16 + meta / 8;

    // ── Item name (spec §4) ───────────────────────────────────────────────────

    public string GetNameWithMeta(int meta)
    {
        int clamped = Math.Clamp(meta, 0, 15);
        return "item.dyePowder." + DyeNames[clamped];
    }

    // ── OnItemUse — bonemeal (meta 15) (spec §5) ─────────────────────────────

    public override bool OnItemUse(ItemStack stack, object playerObj, World world, int x, int y, int z, int face)
    {
        if (stack.Damage != 15) return false;   // only bonemeal activates on blocks
        if (world.IsClientSide) return false;

        int id = world.GetBlockId(x, y, z);
        var block = Block.BlocksList[id];
        if (block == null) return false;

        // Check if this block type supports bonemeal
        if (!SupportsBoneMeal(id)) return false;

        block.BonemealGrow(world, x, y, z, world.Random);
        stack.StackSize--;
        return true;
    }

    private static bool SupportsBoneMeal(int id)
        => id == 6    // sapling
        || id == 39   // brown mushroom
        || id == 40   // red mushroom
        || id == 59   // wheat/crops
        || id == 104  // pumpkin stem
        || id == 105  // melon stem
        || id == 2;   // grass block

    // ── Entity interaction — sheep dyeing (spec §6) ──────────────────────────

    public override int ItemInteractionForEntity(object entityObj)
    {
        if (entityObj is not EntitySheep sheep) return 0;

        int color = DyeMetaToWoolColor(/* meta passed via active item — stub */ 0);
        if (!sheep.IsSheared && sheep.WoolColour != color)
        {
            sheep.WoolColour = color;
            return 1; // consumed
        }
        return 0;
    }

    /// <summary>
    /// Converts dye metadata to wool color index.
    /// Dye meta is stored as (15 - woolColor) in vanilla: wool meta = 15 - dye meta.
    /// </summary>
    public static int DyeMetaToWoolColor(int dyeMeta) => 15 - dyeMeta;
}
