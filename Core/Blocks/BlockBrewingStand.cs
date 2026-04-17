using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ahp</c> (BlockBrewingStand) — Block ID 117.
/// Has a <see cref="TileEntityBrewingStand"/> tile entity.
/// Right-click opens the brewing GUI.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec.md §4
/// </summary>
public sealed class BlockBrewingStand : Block
{
    public BlockBrewingStand() : base(117, 157, Material.RockTransp2)
    {
        SetHardness(0.5f);
        SetResistance(5.0f);
        SetStepSound(SoundStoneHighPitch2);
        SetLightValue(0.0625f); // light 1 ≈ 0.0625F
        SetHasTileEntity();
        SetBlockName("brewingStand");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Collision AABBs (spec §4.3) ───────────────────────────────────────────

    public override void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB queryBox, System.Collections.Generic.List<AxisAlignedBB> list)
    {
        // Central rod
        var rod = AxisAlignedBB.GetFromPool(
            x + 0.4375, y + 0.0, z + 0.4375,
            x + 0.5625, y + 0.875, z + 0.5625);
        if (queryBox.Intersects(rod)) list.Add(rod);

        // Base slab
        var slab = AxisAlignedBB.GetFromPool(
            x + 0.0, y + 0.0, z + 0.0,
            x + 1.0, y + 0.125, z + 1.0);
        if (queryBox.Intersects(slab)) list.Add(slab);
    }

    // ── Right-click (spec §4.4) ───────────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        if (world.GetTileEntity(x, y, z) is not TileEntityBrewingStand te) return true;
        player.OpenInventory(te);
        return true;
    }

    // ── Break (spec §4.6) ────────────────────────────────────────────────────

    public override void OnBlockPreDestroy(IWorld world, int x, int y, int z)
    {
        if (world.GetTileEntity(x, y, z) is not TileEntityBrewingStand te) return;

        for (int i = 0; i < te.Slots.Length; i++)
        {
            if (te.Slots[i] is { } stack)
            {
                te.Slots[i] = null;
                ScatterItem(world, x, y, z, stack);
            }
        }
    }

    // Drops the brewing stand item (spec §4.7 — acy.bx.bM)
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 379; // brewing stand item ID

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ScatterItem(IWorld world, int x, int y, int z, ItemStack stack)
    {
        if (world is not World concreteWorld) return;
        var rng   = world.Random;
        double ox = rng.NextFloat() * 0.8 + 0.1;
        double oy = rng.NextFloat() * 0.8 + 0.1;
        double oz = rng.NextFloat() * 0.8 + 0.1;
        var e     = new EntityItem(concreteWorld, x + ox, y + oy, z + oz, stack);
        e.MotionX = (float)(rng.NextGaussian() * 0.05);
        e.MotionY = (float)(rng.NextGaussian() * 0.05 + 0.2);
        e.MotionZ = (float)(rng.NextGaussian() * 0.05);
        concreteWorld.SpawnEntity(e);
    }
}
