namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Places sugar cane (reed) columns next to water. 20 attempts per call.
/// Spec: <c>tw</c> (WorldGenReed).
///
/// Placement conditions:
///   • Block at (bx, by, bz) must be air.
///   • At least one of the 4 horizontal neighbours at y-1 must have water material.
///   • Reed.canBlockStay must return true for each column block.
///
/// Height: 2 + nextInt(nextInt(3) + 1) → [2, 4], biased toward 2.
/// Spread: ±3 X/Z (nextInt(4) - nextInt(4)). Y unchanged from call site.
/// Placed silently (world.d = SetBlockSilent, no metadata).
///
/// Block IDs: 83 = sugar cane (reed). Water material check uses Material.Water.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.5
/// </summary>
public sealed class WorldGenReed : WorldGenerator
{
    private const int ReedId = 83;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        Block? reedBlock = Block.BlocksList[ReedId];

        for (int i = 0; i < 20; i++)
        {
            int bx = x + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(4) - rand.NextInt(4);
            int by = y; // Y unchanged (spec §5.5)

            if (world.GetBlockId(bx, by, bz) != 0) continue;

            // Require water adjacent at y-1
            bool hasWater =
                world.GetBlockMaterial(bx - 1, by - 1, bz) == Material.Water ||
                world.GetBlockMaterial(bx + 1, by - 1, bz) == Material.Water ||
                world.GetBlockMaterial(bx, by - 1, bz - 1) == Material.Water ||
                world.GetBlockMaterial(bx, by - 1, bz + 1) == Material.Water;

            if (!hasWater) continue;

            // Height [2, 4], biased toward 2
            int height = 2 + rand.NextInt(rand.NextInt(3) + 1);

            for (int h = 0; h < height; h++)
            {
                if (reedBlock?.CanBlockStay(world, bx, by + h, bz) ?? false)
                    world.SetBlockSilent(bx, by + h, bz, ReedId);
            }
        }

        return true;
    }
}
