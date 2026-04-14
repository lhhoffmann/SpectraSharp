namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Scatters a single flower or mushroom block over a 15×7-block area.
/// Spec: <c>bu</c> (WorldGenFlowers). 64 attempts per call.
///
/// Spread: ±7 blocks on X/Z (nextInt(8) - nextInt(8)), ±3 on Y.
/// Placement condition: air block AND <c>Block.canBlockStay</c>.
/// Placed silently (world.d = SetBlockSilent, no metadata, no neighbour notifications).
///
/// Used for: dandelion (ID 37), rose (ID 38), brown mushroom (ID 39), red mushroom (ID 40).
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.1
/// </summary>
public sealed class WorldGenFlowers(int blockId) : WorldGenerator
{
    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        Block? block = Block.BlocksList[blockId];

        for (int i = 0; i < 64; i++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) == 0
                && (block?.CanBlockStay(world, bx, by, bz) ?? false))
            {
                world.SetBlockSilent(bx, by, bz, blockId);
            }
        }

        return true;
    }
}
