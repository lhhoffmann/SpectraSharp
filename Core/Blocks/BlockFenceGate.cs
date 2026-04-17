namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>fp</c> (BlockFenceGate) — Block ID 107.
/// Right-click toggles open/closed. Open = no collision; closed = 1.5-tall barrier.
///
/// Metadata encoding (spec §3):
///   bits 0–1 (mask 0x3): facing direction (0=south, 1=west, 2=north, 3=east)
///   bit 2    (mask 0x4): open flag (0=closed, 1=open)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFenceGate_Spec.md
/// </summary>
public sealed class BlockFenceGate : Block
{
    public BlockFenceGate() : base(107, 4, Material.Plants)
    {
        SetHardness(2.0f);
        SetResistance(5.0f);
        SetStepSound(SoundWood);
        ClearNeedsRandomTick();
        SetBlockName("fenceGate");
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 21;
    public override bool RenderAsNormalBlock() => false;

    // ── Helper methods (spec §3) ─────────────────────────────────────────────

    private static bool IsOpen(int meta)    => (meta & 0x4) != 0;
    private static int  GetFacing(int meta) => meta & 0x3;

    // ── AABB / Collision (spec §4) ────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        if (IsOpen(meta)) return null;
        return AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 1.5, z + 1);
    }

    // ── Placement — set direction from placer yaw (spec §5) ─────────────────

    public override void OnBlockPlacedBy(IWorld world, int x, int y, int z, LivingEntity placer)
    {
        int dir = (int)Math.Floor(placer.RotationYaw * 4.0f / 360.0f + 0.5f) & 3;
        world.SetMetadata(x, y, z, dir);
    }

    // ── Right-click interaction (spec §6) ────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        int meta = world.GetBlockMetadata(x, y, z);

        if (IsOpen(meta))
        {
            // Close gate
            world.SetMetadata(x, y, z, meta & ~0x4);
        }
        else
        {
            // Open gate — orient toward player
            int playerDir = (int)Math.Floor(player.RotationYaw * 4.0f / 360.0f + 0.5f) & 3;
            int gateDir   = GetFacing(meta);
            if (gateDir == (playerDir + 2) % 4)
            {
                // Player faces the gate from opposite side — flip direction toward player
                meta = playerDir;
            }
            world.SetMetadata(x, y, z, meta | 0x4);
        }

        // Sound event 1003 (door creak). Spec §6 step 4.
        world.PlayAuxSFX(null, 1003, x, y, z, 0);
        return true;
    }
}
