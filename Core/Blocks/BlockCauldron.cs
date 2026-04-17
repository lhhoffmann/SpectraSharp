namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ic</c> (BlockCauldron) — Block ID 118.
/// No TileEntity. Water level stored in metadata (0–3).
///
/// Metadata:
///   0 = empty, 1 = one-third, 2 = two-thirds, 3 = full
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec.md §3
/// </summary>
public sealed class BlockCauldron : Block
{
    private const int IdWaterBucket = 326; // acy.aw
    private const int IdEmptyBucket = 325; // acy.av

    public BlockCauldron() : base(118, 154, Material.RockTransp2)
    {
        SetHardness(2.0f);
        SetResistance(5.0f);
        SetStepSound(SoundStoneHighPitch2);
        SetBlockName("cauldron");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 20;
    public override bool RenderAsNormalBlock() => false;

    // ── Textures (spec §3.3) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face)
    {
        return face switch
        {
            0 => 155, // bottom
            1 => 138, // top
            _ => 154  // sides
        };
    }

    // ── Collision AABBs (spec §3.5) ───────────────────────────────────────────

    public override void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB queryBox, System.Collections.Generic.List<AxisAlignedBB> list)
    {
        // Bottom slab
        AddIfIntersects(x, y, z, 0.0, 0.0, 0.0, 1.0, 0.3125, 1.0, queryBox, list);
        // West wall
        AddIfIntersects(x, y, z, 0.0, 0.0, 0.0, 0.125, 1.0, 1.0, queryBox, list);
        // South wall (near, -Z)
        AddIfIntersects(x, y, z, 0.0, 0.0, 0.0, 1.0, 1.0, 0.125, queryBox, list);
        // East wall
        AddIfIntersects(x, y, z, 0.875, 0.0, 0.0, 1.0, 1.0, 1.0, queryBox, list);
        // North wall (far, +Z)
        AddIfIntersects(x, y, z, 0.0, 0.0, 0.875, 1.0, 1.0, 1.0, queryBox, list);
    }

    private static void AddIfIntersects(
        int bx, int by, int bz,
        double x0, double y0, double z0, double x1, double y1, double z1,
        AxisAlignedBB queryBox, System.Collections.Generic.List<AxisAlignedBB> list)
    {
        var bb = AxisAlignedBB.GetFromPool(bx + x0, by + y0, bz + z0,
                                           bx + x1, by + y1, bz + z1);
        if (queryBox.Intersects(bb))
            list.Add(bb);
    }

    // ── Right-click (spec §3.6) ───────────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        int meta   = world.GetBlockMetadata(x, y, z);
        var held   = player.Inventory.GetStackInSelectedSlot();
        int heldId = held?.ItemId ?? 0;

        // Water Bucket → fill cauldron to 3
        if (heldId == IdWaterBucket)
        {
            if (meta < 3)
            {
                ReplaceHeldItem(player, held!, new ItemStack(IdEmptyBucket));
                world.SetMetadata(x, y, z, 3);
            }
            return true;
        }

        // Glass Bottle (with water, ID 374) → take 1 level of water as water bottle (ID 373)
        // acy.bs = water bottle item ID; acy.br = water bottle result
        // Using standard 1.0 IDs: glass bottle = 374, water bottle = 373
        if (heldId == 374)
        {
            if (meta > 0)
            {
                AddOrDropItem(world, player, x, y, z, new ItemStack(373));
                held!.StackSize--;
                if (held.StackSize <= 0)
                    player.Inventory.SetInventorySlotContents(player.Inventory.CurrentItem, null);
                world.SetMetadata(x, y, z, meta - 1);
            }
            return true;
        }

        return true;
    }

    // ── Drops (spec §3.7) ────────────────────────────────────────────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 380; // acy.by.bM = cauldron item

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ReplaceHeldItem(EntityPlayer player, ItemStack old, ItemStack newItem)
    {
        old.StackSize--;
        if (old.StackSize <= 0)
            player.Inventory.SetInventorySlotContents(player.Inventory.CurrentItem, newItem);
        else
            player.Inventory.AddItemStackToInventory(newItem);
    }

    private static void AddOrDropItem(IWorld world, EntityPlayer player,
                                      int x, int y, int z, ItemStack stack)
    {
        if (!player.Inventory.AddItemStackToInventory(stack))
        {
            if (world is not World concreteWorld) return;
            var entity = new EntityItem(concreteWorld,
                player.PosX, player.PosY, player.PosZ, stack);
            concreteWorld.SpawnEntity(entity);
        }
    }
}
