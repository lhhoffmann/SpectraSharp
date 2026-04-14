namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Places a water or lava spring pocket in cave walls.
/// Spec: <c>ib</c> (WorldGenSpring).
///
/// Conditions (all must pass):
///   • Block above (y+1) must be stone.
///   • Block below (y-1) must be stone.
///   • Block at (x, y, z) must be air or stone.
///   • Exactly 3 of the 4 horizontal neighbours (W/E/N/S) must be stone.
///   • Exactly 1 of the 4 horizontal neighbours must be air.
///
/// Placement:
///   • Places the fluid block silently (world.g equivalent → SetBlockSilent).
///   • Calls Block.OnBlockAdded to schedule the fluid for ticking.
///
/// Block IDs: 8 = flowing water, 10 = flowing lava.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.4
/// </summary>
public sealed class WorldGenSpring(int blockId) : WorldGenerator
{
    private const int Stone = 1;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Vertical stone neighbours required
        if (world.GetBlockId(x, y + 1, z) != Stone) return false;
        if (world.GetBlockId(x, y - 1, z) != Stone) return false;

        // Current cell must be air or stone
        int here = world.GetBlockId(x, y, z);
        if (here != 0 && here != Stone) return false;

        // Count horizontal stone/air neighbours
        int stoneCount = 0;
        int airCount   = 0;

        if (world.GetBlockId(x - 1, y, z) == Stone) stoneCount++; else if (world.GetBlockId(x - 1, y, z) == 0) airCount++;
        if (world.GetBlockId(x + 1, y, z) == Stone) stoneCount++; else if (world.GetBlockId(x + 1, y, z) == 0) airCount++;
        if (world.GetBlockId(x, y, z - 1) == Stone) stoneCount++; else if (world.GetBlockId(x, y, z - 1) == 0) airCount++;
        if (world.GetBlockId(x, y, z + 1) == Stone) stoneCount++; else if (world.GetBlockId(x, y, z + 1) == 0) airCount++;

        if (stoneCount == 3 && airCount == 1)
        {
            // Place fluid silently (equiv. of world.g in spec), then notify via OnBlockAdded
            world.SetBlockSilent(x, y, z, blockId);
            Block.BlocksList[blockId]?.OnBlockAdded(world, x, y, z);
        }

        return true;
    }
}
