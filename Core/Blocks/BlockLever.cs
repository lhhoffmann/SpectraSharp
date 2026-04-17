namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aaa</c> (BlockLever) — Block ID 69.
///
/// Meta layout (spec §7.2):
///   bits 2-0 (meta &amp; 7): facing (1=west, 2=east, 3=north, 4=south, 5=floor-south, 6=floor-east)
///   bit 3 (meta &amp; 8):   isOn (8=on, 0=off)
///
/// Quirks preserved (spec §12):
///   7. Floor lever has two orientations (5/6) chosen randomly on placement.
///   8. Toggle: on_bit = 8 - (meta &amp; 8) — clean XOR.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §7
/// </summary>
public sealed class BlockLever : Block
{
    public BlockLever(int id) : base(id, 96, Material.Plants)
    {
        SetHardness(0.5f);
        SetStepSound(SoundStone);
        SetBlockName("lever");
    }

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 12;
    public override bool RenderAsNormalBlock() => false;
    public override bool CanProvidePower() => true;
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z) => null;

    // ── Bounds (spec §7.9) ────────────────────────────────────────────────────

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z) & 7;
        return meta switch
        {
            1 => AxisAlignedBB.GetFromPool(x,       y + 0.2f, z + 0.3125f, x + 0.375f, y + 0.8f, z + 0.6875f),
            2 => AxisAlignedBB.GetFromPool(x+0.625f,y + 0.2f, z + 0.3125f, x + 1.0f,  y + 0.8f, z + 0.6875f),
            3 => AxisAlignedBB.GetFromPool(x+0.3125f,y + 0.2f, z,          x + 0.6875f,y + 0.8f, z + 0.375f),
            4 => AxisAlignedBB.GetFromPool(x+0.3125f,y + 0.2f, z + 0.625f, x + 0.6875f,y + 0.8f, z + 1.0f),
            _ => AxisAlignedBB.GetFromPool(x + 0.25f,y,        z + 0.25f,  x + 0.75f,  y + 0.6f, z + 0.75f),
        };
    }

    // ── canBlockStay (spec §7.3) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => world is World w
        && (w.IsBlockNormalCube(x - 1, y, z)
        || w.IsBlockNormalCube(x + 1, y, z)
        || w.IsBlockNormalCube(x, y, z - 1)
        || w.IsBlockNormalCube(x, y, z + 1)
        || w.IsBlockNormalCube(x, y - 1, z));

    // ── onBlockActivated (spec §7.6) ──────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;
        if (world is not World w) return true;

        int meta   = world.GetBlockMetadata(x, y, z);
        int facing = meta & 7;
        int onBit  = 8 - (meta & 8); // toggle: 0→8 or 8→0

        w.SetMetadataQuiet(x, y, z, facing + onBit);
        w.NotifyNeighbors(x, y, z, BlockID);
        // PlayAuxSFX: "random.click" — stub (sound system pending)
        world.PlayAuxSFX(null, 1003, x, y, z, onBit > 0 ? 1 : 0);

        w.NotifyBlock(x, y, z, BlockID);
        NotifyAttachedBlock(w, x, y, z, facing);
        return true;
    }

    // ── onBlockRemoved (spec §7.7) ───────────────────────────────────────────

    public override void OnBlockRemoved(IWorld world, int x, int y, int z)
    {
        if (world is not World w) return;
        int meta = world.GetBlockMetadata(x, y, z);
        if ((meta & 8) != 0) // was ON
        {
            w.NotifyBlock(x, y, z, BlockID);
            NotifyAttachedBlock(w, x, y, z, meta & 7);
        }
        base.OnBlockRemoved(world, x, y, z);
    }

    // ── Power output (spec §7.8) ──────────────────────────────────────────────

    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
        => (world.GetBlockMetadata(x, y, z) & 8) != 0;

    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
    {
        if ((world.GetBlockMetadata(x, y, z) & 8) == 0) return false;
        int facing = world.GetBlockMetadata(x, y, z) & 7;
        return facing switch
        {
            6 => face == 1,
            5 => face == 1,
            4 => face == 2,
            3 => face == 3,
            2 => face == 4,
            1 => face == 5,
            _ => false
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void NotifyAttachedBlock(World w, int x, int y, int z, int facing)
    {
        switch (facing)
        {
            case 1: w.NotifyBlock(x - 1, y, z, 69); break;
            case 2: w.NotifyBlock(x + 1, y, z, 69); break;
            case 3: w.NotifyBlock(x, y, z - 1, 69); break;
            case 4: w.NotifyBlock(x, y, z + 1, 69); break;
            case 5:
            case 6: w.NotifyBlock(x, y - 1, z, 69); break;
        }
    }
}
