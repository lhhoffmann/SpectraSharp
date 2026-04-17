namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>sy</c> (BlockEnchantmentTable) — enchanting table. Block ID 116.
///
/// Properties (spec §3):
///   - Hardness 5.0, blast resistance 2000 (very resistant to explosions).
///   - Partial height: AABB y=[0, 0.75].
///   - Has tile entity: TileEntityEnchantmentTable (animated book).
///
/// Random tick (spec §3): bookshelf particle — 1/16 chance per adjacent bookshelf
/// in the 5×5 ring at table Y level, with LOS check.
///
/// OnBlockActivated: opens enchanting GUI (stub — opens ContainerEnchantment server-side).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §3
/// </summary>
public sealed class BlockEnchantmentTable : Block
{
    public BlockEnchantmentTable(int blockId)
        : base(blockId, 122 /* texture index */, Material.RockTransp)
    {
        SetHardness(5.0f)
            .SetResistance(2000.0f)
            .SetStepSound(Block.SoundStoneHighPitch)
            .SetHasTileEntity()
            .SetBlockName("enchantmentTable");
    }

    // ── AABB (spec §3) ───────────────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(
        IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 0.75, z + 1);

    public override AxisAlignedBB? GetBlockBoundsFromPool()
        => AxisAlignedBB.GetFromPool(0, 0, 0, 1, 0.75, 1);

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 26; // enchantment table slab
    public override bool RenderAsNormalBlock() => false;

    // ── Random tick — bookshelf particles (spec §3) ──────────────────────────

    public override void RandomDisplayTick(
        World world, int x, int y, int z, JavaRandom rand)
    {
        // 5×5 ring (skip inner 3×3) — spec: var6 ∈ x-2..x+2, var7 ∈ z-2..z+2,
        // inner skip: when var6 > x-2 AND var6 < x+2 AND var7 == z-1, jump to z+2.
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                // Inner skip (spec §3 inner skip logic)
                if (dx > -2 && dx < 2 && dz == -1)
                {
                    dz = 1; // jump to z+1 (next iteration +1 = z+2)
                    continue;
                }

                if (rand.NextInt(16) != 0) continue;

                int bx = x + dx;
                int bz2 = z + dz;

                // Check bookshelf at y or y+1
                bool hasShelf = world.GetBlockId(bx, y, bz2) == 47
                             || world.GetBlockId(bx, y + 1, bz2) == 47;
                if (!hasShelf) continue;

                // LOS check: midpoint must be air
                int midX = (bx - x) / 2 + x;
                int midZ = (bz2 - z) / 2 + z;
                bool losAir = world.GetBlockId(midX, y, midZ) == 0;
                if (!losAir) continue;

                // Spawn "enchantmenttable" particle (stub — particle system pending)
                // world.spawnParticle("enchantmenttable", ...)
            }
        }
    }

    // ── OnBlockActivated (spec §3) ───────────────────────────────────────────

    public override bool OnBlockActivated(
        IWorld world, int x, int y, int z, EntityPlayer player, int face,
        float hitX, float hitY, float hitZ)
    {
        // Server-side: open enchanting interface (stub — GUI system pending)
        // In vanilla: player.openEnchantingGui(x, y, z)
        return true;
    }
}
