using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>my</c> (ItemSign) — places floor or wall signs.
///
/// Item ID: 323 (itemId=67). Stack size: 1.
///
/// Floor sign (ID 63) — placed on top face; metadata = 0–15 yaw step.
/// Wall sign (ID 68) — placed on side faces 2–5; metadata = face value.
///
/// Opens sign-editing GUI immediately after placement.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemSign_Spec.md
/// </summary>
public sealed class ItemSign : Item
{
    private const int FloorSignId = 63;
    private const int WallSignId  = 68;

    public ItemSign() : base(67) // RegistryIndex = 323
    {
        MaxStackSize = 1;
        SetUnlocalizedName("sign");
    }

    // ── OnItemUse (spec §3) ───────────────────────────────────────────────────

    public override bool OnItemUse(ItemStack stack, object playerObj, World world, int x, int y, int z, int face)
    {
        if (playerObj is not EntityPlayer player) return false;
        if (world.IsClientSide) return false;

        // Pre-check 1: cannot place on underside
        if (face == 0) return false;

        // Pre-check 2: target block must be solid
        var mat = world.GetBlockMaterial(x, y, z);
        if (!mat.IsSolid()) return false;

        // Adjust position by face
        switch (face)
        {
            case 1: y++; break;
            case 2: z--; break;
            case 3: z++; break;
            case 4: x--; break;
            case 5: x++; break;
        }

        // Pre-check 4: within player reach (stub — distance check not implemented)
        // Pre-check 5: target position can receive a sign
        var signBlock = Block.BlocksList[FloorSignId];
        if (signBlock == null || !signBlock.CanBlockStay(world, x, y, z)) return false;

        if (face == 1)
        {
            // Floor sign — quantise player yaw to 16 steps
            int meta = (MathHelper.FloorDouble((player.RotationYaw + 180.0) * 16.0 / 360.0 + 0.5)) & 15;
            world.SetBlockAndMetadata(x, y, z, FloorSignId, meta);
        }
        else
        {
            // Wall sign — metadata = face value
            world.SetBlockAndMetadata(x, y, z, WallSignId, face);
        }

        stack.StackSize--;

        // Open sign editing GUI
        if (world.GetTileEntity(x, y, z) is TileEntitySign te)
            player.OpenSignEditor(te);

        return true;
    }
}
