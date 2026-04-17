namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>cj</c> (BlockSand) — Block ID 12. Gravity-based falling block.
///
/// On random tick: if the block below is air / water / lava / fire, the sand falls.
/// EntityFallingSand (<c>uo</c>) is not yet implemented; uses instant-fall (world-gen mode)
/// as a functional stub. TODO: spawn EntityFallingSand for animation.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ConcreteBlocks_Spec.md §2
/// </summary>
public class BlockSand : Block
{
    public BlockSand(int id, int texture, Material material) : base(id, texture, material) { }

    // ── Random tick (spec §2 — Gravity Fall Logic) ────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (y <= 0) return;
        if (!IsFallable(world, x, y - 1, z)) return;

        if (world is World concreteWorld)
        {
            // Spawn EntityFallingSand (spec EntityFallingSand §4 — entity-based gravity)
            concreteWorld.SetBlock(x, y, z, 0);
            var entity = new EntityFallingSand(concreteWorld, x + 0.5, y, z + 0.5, BlockID);
            concreteWorld.SpawnEntity(entity);
        }
        else
        {
            // Instant-fall fallback for headless / world-gen contexts
            int targetY = y - 1;
            while (targetY > 0 && IsFallable(world, x, targetY - 1, z))
                targetY--;
            world.SetBlock(x, y, z, 0);
            world.SetBlock(x, targetY, z, BlockID);
        }
    }

    /// <summary>
    /// Returns true if this block can fall into the block at (x, y, z).
    /// Air, water (8/9), lava (10/11), fire (51) are all fallable targets.
    /// </summary>
    protected static bool IsFallable(IBlockAccess world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        return id == 0 || id == 8 || id == 9 || id == 10 || id == 11 || id == 51;
    }

    /// <summary>
    /// Returns true if the block at (x, y, z) would itself fall (used by EntityFallingSand
    /// to prevent stacking a newly-landed block on top of another falling sand entity).
    /// Spec: EntityFallingSand §4 step 8 — <c>BlockSand.isFallingBelow(world, x, y-1, z)</c>.
    /// </summary>
    public static bool IsFallingBelow(IBlockAccess world, int x, int y, int z)
        => IsFallable(world, x, y, z);
}
