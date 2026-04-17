using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>eu</c> (BlockFurnace) — Block IDs 61 (unlit) and 62 (lit).
/// Delegates to <see cref="TileEntityFurnace"/> for smelting logic.
///
/// Metadata encodes facing direction:
///   2 = North (-Z), 3 = South (+Z), 4 = West (-X), 5 = East (+X)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec.md §2
/// </summary>
public sealed class BlockFurnace : Block
{
    private readonly bool _isLit; // true = ID 62, false = ID 61

    // Static swap-guard flag matching spec §2.7 field cc
    private static bool s_swapping;

    public BlockFurnace(int id, bool isLit) : base(id, 45, Material.RockTransp)
    {
        _isLit = isLit;
        SetHardness(3.5f);
        SetStepSound(SoundStoneHighPitch);
        SetHasTileEntity();
        if (isLit) SetLightValue(0.875f); // light 13 = 0.875F
        SetBlockName("furnace");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    // Furnace is a full opaque cube — default implementations are correct.

    // ── Textures (spec §2.5) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face)
        => GetStaticTextureForFace(face, _isLit, 3); // default facing = south (3)

    public override int GetTextureForFaceAndMeta(int face, int meta)
        => GetStaticTextureForFace(face, _isLit, meta);

    private static int GetStaticTextureForFace(int face, bool lit, int facingMeta)
    {
        // Top and bottom: texture index 62 (bL + 17)
        if (face == 0 || face == 1) return 62;
        // Front face (face == facingMeta) differs
        if (face == facingMeta)
            return lit ? 61 : 44; // 61 = bL+16 (glowing), 44 = bL-1 (dark)
        // All other sides: 45 (bL)
        return 45;
    }

    // ── Placement (spec §2.4) ─────────────────────────────────────────────────

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
        world.SetMetadata(x, y, z, meta);
    }

    // ── Right-click (spec §2.6) ───────────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        if (world.GetTileEntity(x, y, z) is not TileEntityFurnace te) return true;
        player.OpenInventory(te);
        return true;
    }

    // ── Lit/Unlit transition (spec §2.7) ─────────────────────────────────────

    /// <summary>Called from TileEntityFurnace when lit state changes.</summary>
    public static void SetLitState(IWorld world, int x, int y, int z, bool isLit)
    {
        if (s_swapping) return;
        s_swapping = true;

        int meta  = world.GetBlockMetadata(x, y, z);
        var te    = world.GetTileEntity(x, y, z) as TileEntity.TileEntity;
        int newId = isLit ? 62 : 61;

        world.SetBlock(x, y, z, newId);
        world.SetMetadata(x, y, z, meta);
        // Re-attach TileEntity after block swap (spec §2.7 step 7)
        if (te != null && world is World concreteWorld)
            concreteWorld.SetTileEntity(x, y, z, te);

        s_swapping = false;
    }

    // ── Break (spec §2.9) ────────────────────────────────────────────────────

    public override void OnBlockPreDestroy(IWorld world, int x, int y, int z)
    {
        if (s_swapping) return; // do not scatter during lit↔unlit swap

        if (world.GetTileEntity(x, y, z) is not TileEntityFurnace te) return;

        for (int i = 0; i < te.Slots.Length; i++)
        {
            if (te.Slots[i] is { } stack)
            {
                te.SetInventorySlotContents(i, null);
                ScatterItem(world, x, y, z, stack);
            }
        }
    }

    // Always drops unlit furnace (ID 61) regardless of current state (spec §2.9)
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 61;

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
