namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>mf</c> (BlockTrapDoor) — Block ID 96 (wooden) and 167 (iron, future).
/// Flat horizontal panel when closed; vertical panel when open.
/// Wooden trapdoor can be toggled by hand; iron only responds to redstone.
///
/// Metadata encoding (spec §3):
///   bits 0–1 (mask 0x3): hinge wall attachment
///     0 = north/+Z wall, 1 = south/-Z wall
///     2 = east/+X wall,  3 = west/-X wall
///   bit 2 (mask 0x4): open flag (0=closed, 1=open)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockTrapDoor_Spec.md
/// </summary>
public sealed class BlockTrapDoor : Block
{
    private readonly bool _isIron;

    public BlockTrapDoor(int id, Material material, bool isIron) : base(id, 84, material)
    {
        _isIron = isIron;
        SetHardness(3.0f);
        SetStepSound(SoundWood);
        ClearNeedsRandomTick();
        SetBlockName("trapdoor");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── AABB (spec §4) ───────────────────────────────────────────────────────

    private const float Thickness = 0.1875f;

    private void ApplyMetaBounds(int meta)
    {
        bool open = (meta & 0x4) != 0;
        if (!open)
        {
            SetBounds(0.0f, 0.0f, 0.0f, 1.0f, Thickness, 1.0f);
            return;
        }
        switch (meta & 0x3)
        {
            case 0: SetBounds(0.0f,           0.0f, 1.0f - Thickness, 1.0f, 1.0f, 1.0f); break; // +Z wall
            case 1: SetBounds(0.0f,           0.0f, 0.0f,             1.0f, 1.0f, Thickness); break; // -Z wall
            case 2: SetBounds(1.0f - Thickness,0.0f, 0.0f,            1.0f, 1.0f, 1.0f); break; // +X wall
            case 3: SetBounds(0.0f,           0.0f, 0.0f,             Thickness, 1.0f, 1.0f); break; // -X wall
        }
    }

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        ApplyMetaBounds(world.GetBlockMetadata(x, y, z));
        return base.GetCollisionBoundingBoxFromPool(world, x, y, z);
    }

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
        => ApplyMetaBounds(world.GetBlockMetadata(x, y, z));

    // ── Neighbor update — support validation + redstone (spec §5, §7) ─────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);
        if (world.IsClientSide) return;

        int meta = world.GetBlockMetadata(x, y, z);
        int dir  = meta & 0x3;

        // Determine the block this trapdoor is attached to
        int sx = x, sz = z;
        switch (dir)
        {
            case 0: sz = z + 1; break;
            case 1: sz = z - 1; break;
            case 2: sx = x + 1; break;
            case 3: sx = x - 1; break;
        }

        if (!IsValidSupport(world.GetBlockId(sx, y, sz)))
        {
            DropBlockAsItem(world, x, y, z, meta, 0);
            world.SetBlock(x, y, z, 0);
            return;
        }

        // Redstone: check indirect power and toggle if needed (spec §7)
        bool powered = world.IsBlockIndirectlyReceivingPower(x, y, z);
        bool isOpen  = (meta & 0x4) != 0;
        if (isOpen != powered)
        {
            world.SetMetadata(x, y, z, meta ^ 0x4);
            world.PlayAuxSFX(null, 1003, x, y, z, 0);
        }
    }

    /// <summary>
    /// Returns true if the given block ID can support a trapdoor.
    /// Solid full-cube blocks or door blocks (64/71) are valid.
    /// Spec: <c>mf.f(int blockId)</c>.
    /// </summary>
    private static bool IsValidSupport(int blockId)
    {
        if (blockId <= 0) return false;
        Block? block = BlocksList[blockId];
        if (block == null) return false;
        if ((block.BlockMaterial?.IsSolid() ?? false) && block.IsOpaqueCube()) return true;
        return blockId == 64 || blockId == 71; // door blocks (spec §8, quirk 10.3)
    }

    // ── Right-click interaction (spec §6) ────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (_isIron) return true; // iron trapdoor absorbs click but does nothing (quirk 10.2)

        int meta = world.GetBlockMetadata(x, y, z);
        world.SetMetadata(x, y, z, meta ^ 0x4);
        world.PlayAuxSFX(null, 1003, x, y, z, 0);
        return true;
    }
}
