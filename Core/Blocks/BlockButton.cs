namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>ahv</c> (BlockButton) — Block ID 77 (stone button).
/// Wood button (ID 143) was added in Beta 1.7+; absent in 1.0.
///
/// Meta layout (spec §9.2):
///   bits 2-0 (meta &amp; 7): facing (1=west, 2=east, 3=north, 4=south; wall-only)
///   bit 3 (meta &amp; 8):   isPressed (0=off, 8=pressed)
///
/// Tick duration: 20 ticks (1 second) before auto-release.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §9
/// </summary>
public sealed class BlockButton : Block
{
    public BlockButton(int id) : base(id, 1, Material.RockTransp)
    {
        SetHardness(0.5f);
        SetStepSound(SoundStoneHighPitch);
        SetBlockName("button");
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;
    public override bool CanProvidePower() => true;
    public override int GetMobilityFlag() => 1;
    public override int GetTickDelay() => 20;

    // ── Bounds (spec §9.9) ────────────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z) => null;

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int meta    = world.GetBlockMetadata(x, y, z);
        int facing  = meta & 7;
        float depth = (meta & 8) != 0 ? 0.0625f : 0.125f; // pressed = thinner

        return facing switch
        {
            1 => AxisAlignedBB.GetFromPool(x,        y + 0.375f, z + 0.3125f, x + depth,   y + 0.625f, z + 0.6875f),
            2 => AxisAlignedBB.GetFromPool(x+1-depth,y + 0.375f, z + 0.3125f, x + 1.0f,    y + 0.625f, z + 0.6875f),
            3 => AxisAlignedBB.GetFromPool(x+0.3125f,y + 0.375f, z,           x + 0.6875f, y + 0.625f, z + depth),
            4 => AxisAlignedBB.GetFromPool(x+0.3125f,y + 0.375f, z + 1-depth, x + 0.6875f, y + 0.625f, z + 1.0f),
            _ => AxisAlignedBB.GetFromPool(x + 0.3125f, y + 0.375f, z + 0.3125f, x + 0.6875f, y + 0.625f, z + 0.6875f),
        };
    }

    // ── canBlockStay (spec §9.3) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => Block.IsOpaqueCubeArr[world.GetBlockId(x - 1, y, z) & 0xFF]
        || Block.IsOpaqueCubeArr[world.GetBlockId(x + 1, y, z) & 0xFF]
        || Block.IsOpaqueCubeArr[world.GetBlockId(x, y, z - 1) & 0xFF]
        || Block.IsOpaqueCubeArr[world.GetBlockId(x, y, z + 1) & 0xFF];

    // ── onBlockActivated (spec §9.5) ──────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;
        if (world is not World w) return true;

        int meta   = world.GetBlockMetadata(x, y, z);
        int onBit  = 8 - (meta & 8);
        if (onBit == 0) return true; // already pressed

        w.SetMetadataQuiet(x, y, z, (meta & 7) + onBit);
        w.NotifyNeighbors(x, y, z, BlockID);
        world.PlayAuxSFX(null, 1003, x, y, z, 1);
        w.NotifyBlock(x, y, z, BlockID);
        NotifyAttachedBlock(w, x, y, z, meta & 7);
        w.ScheduleBlockUpdate(x, y, z, BlockID, GetTickDelay()); // schedule auto-release
        return true;
    }

    // ── UpdateTick — auto-release (spec §9.6) ────────────────────────────────

    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (world is not World w) return;
        int meta   = w.GetBlockMetadata(x, y, z);
        if ((meta & 8) == 0) return; // already released

        w.SetMetadataQuiet(x, y, z, meta & 7); // clear on-bit
        w.NotifyBlock(x, y, z, BlockID);
        NotifyAttachedBlock(w, x, y, z, meta & 7);
        world.PlayAuxSFX(null, 1003, x, y, z, 0);
        w.NotifyNeighbors(x, y, z, BlockID);
    }

    // ── onBlockRemoved (spec §9.7) ────────────────────────────────────────────

    public override void OnBlockRemoved(IWorld world, int x, int y, int z)
    {
        if (world is not World w) return;
        int meta = w.GetBlockMetadata(x, y, z);
        if ((meta & 8) != 0)
        {
            w.NotifyBlock(x, y, z, BlockID);
            NotifyAttachedBlock(w, x, y, z, meta & 7);
        }
        base.OnBlockRemoved(world, x, y, z);
    }

    // ── Power output (spec §9.8) ──────────────────────────────────────────────

    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
        => (world.GetBlockMetadata(x, y, z) & 8) != 0;

    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
    {
        if ((world.GetBlockMetadata(x, y, z) & 8) == 0) return false;
        return (world.GetBlockMetadata(x, y, z) & 7) switch
        {
            4 => face == 2,  // south wall → power south
            3 => face == 3,  // north wall → power north
            2 => face == 4,  // east wall  → power east
            1 => face == 5,  // west wall  → power west
            _ => false
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void NotifyAttachedBlock(World w, int x, int y, int z, int facing)
    {
        switch (facing)
        {
            case 1: w.NotifyBlock(x - 1, y, z, 77); break;
            case 2: w.NotifyBlock(x + 1, y, z, 77); break;
            case 3: w.NotifyBlock(x, y, z - 1, 77); break;
            case 4: w.NotifyBlock(x, y, z + 1, 77); break;
        }
    }
}
