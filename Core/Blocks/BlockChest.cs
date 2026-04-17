using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>au</c> (BlockChest) — Block ID 54.
/// Single chest (27 slots) or double chest (54 slots via InventoryLargeChest).
///
/// Metadata encoding (spec §3): facing direction
///   2 = North (-Z), 3 = South (+Z), 4 = West (-X), 5 = East (+X)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockChest_Spec.md
/// </summary>
public sealed class BlockChest : Block
{
    public BlockChest() : base(54, 26, Material.Plants)
    {
        SetHardness(2.5f);
        SetStepSound(SoundWood);
        SetHasTileEntity();
        SetBlockName("chest");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Placement — set facing from placer yaw + handle double-chest (spec §4) ─

    public override void OnBlockPlacedBy(IWorld world, int x, int y, int z, LivingEntity placer)
    {
        int quadrant = (int)Math.Floor(placer.RotationYaw * 4.0f / 360.0f + 0.5f) & 3;
        int meta = quadrant switch
        {
            0 => 2, // north
            1 => 5, // east
            2 => 3, // south
            3 => 4, // west
            _ => 2
        };

        // Reorient if an adjacent chest exists so both face along the same axis
        if (IsChestAt(world, x - 1, y, z) || IsChestAt(world, x + 1, y, z))
            meta = 2; // both face N/S when neighbor is on X axis
        if (IsChestAt(world, x, y, z - 1) || IsChestAt(world, x, y, z + 1))
            meta = 4; // both face E/W when neighbor is on Z axis

        world.SetMetadata(x, y, z, meta);
        UpdateNeighborFacings(world, x, y, z, meta);
    }

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        base.OnBlockAdded(world, x, y, z);
        UpdateNeighborFacings(world, x, y, z, world.GetBlockMetadata(x, y, z));
    }

    /// <summary>Forces all adjacent chests to match the given facing.</summary>
    private void UpdateNeighborFacings(IWorld world, int x, int y, int z, int meta)
    {
        // When a new chest is placed next to another, both must align (spec §4 step 3)
        foreach (var (nx, nz) in new[] { (x-1,z), (x+1,z), (x,z-1), (x,z+1) })
        {
            if (IsChestAt(world, nx, y, nz))
                world.SetMetadata(nx, y, nz, meta);
        }
    }

    // ── Right-click (spec §6) ─────────────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        // Step 1 — get TileEntity
        if (world.GetTileEntity(x, y, z) is not TileEntityChest te) return true;

        // Step 2 — obstruction check: solid block directly above any involved chest
        if (IsBlockedAbove(world, x, y, z)) return true;

        // Check all 4 adjacent positions for a second chest
        TileEntityChest? neighbor = null;
        int nx = x, nz = z;
        bool zAxis = false;

        if (IsChestAt(world, x - 1, y, z)) { nx = x - 1; nz = z; }
        else if (IsChestAt(world, x + 1, y, z)) { nx = x + 1; nz = z; }
        else if (IsChestAt(world, x, y, z - 1)) { nx = x; nz = z - 1; zAxis = true; }
        else if (IsChestAt(world, x, y, z + 1)) { nx = x; nz = z + 1; zAxis = true; }

        if (nx != x || nz != z)
        {
            if (IsBlockedAbove(world, nx, y, nz)) return true;
            neighbor = world.GetTileEntity(nx, y, nz) as TileEntityChest;
        }

        // Step 3 — build inventory
        IInventory inv;
        if (neighbor != null)
        {
            // Order: z-axis → lower-Z is "upper" (left); x-axis → lower-X is "upper"
            bool thisIsUpper = zAxis ? nz > z : nx > x;
            inv = thisIsUpper
                ? new InventoryLargeChest("container.chestDouble", te, neighbor)
                : new InventoryLargeChest("container.chestDouble", neighbor, te);
        }
        else
        {
            inv = te;
        }

        // Step 4 — open GUI
        player.OpenInventory(inv);
        return true;
    }

    // ── Pre-break: scatter items (spec §7) ────────────────────────────────────

    public override void OnBlockPreDestroy(IWorld world, int x, int y, int z)
    {
        if (world.GetTileEntity(x, y, z) is not TileEntityChest te) return;

        for (int i = 0; i < te.GetSizeInventory(); i++)
        {
            ItemStack? stack = te.GetStackInSlot(i);
            if (stack == null) continue;

            te.SetInventorySlotContents(i, null);
            ScatterItem(world, x, y, z, stack);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsChestAt(IWorld world, int x, int y, int z)
        => world.GetBlockId(x, y, z) == 54;

    private static bool IsBlockedAbove(IWorld world, int x, int y, int z)
        => IsOpaqueCubeArr[world.GetBlockId(x, y + 1, z)];

    private static void ScatterItem(IWorld world, int x, int y, int z, ItemStack stack)
    {
        if (world is not World concreteWorld) return;
        var rng    = world.Random;
        double ox  = rng.NextFloat() * 0.8 + 0.1;
        double oy  = rng.NextFloat() * 0.8 + 0.1;
        double oz  = rng.NextFloat() * 0.8 + 0.1;
        var entity = new EntityItem(concreteWorld, x + ox, y + oy, z + oz, stack);
        entity.MotionX = (float)(rng.NextGaussian() * 0.05);
        entity.MotionY = (float)(rng.NextGaussian() * 0.05 + 0.2);
        entity.MotionZ = (float)(rng.NextGaussian() * 0.05);
        concreteWorld.SpawnEntity(entity);
    }
}
