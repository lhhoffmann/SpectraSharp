namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Scatters tall grass (or fern) over the surface. 128 attempts per call.
/// Spec: <c>ahu</c> (WorldGenTallGrass).
///
/// Algorithm:
///   1. Descend starting Y through air/leaves until hitting a solid or liquid block.
///   2. 128 scatter attempts: ±7 X/Z, ±3 Y.
///   3. Placement condition: air AND canBlockStay.
///   4. Placed with neighbour notifications (world.b = SetBlockAndMetadata).
///
/// Note: a new instance is created for each call in the BiomeDecorator loop (spec §4 step 8).
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.2
/// </summary>
public sealed class WorldGenTallGrass(int blockId, int meta) : WorldGenerator
{
    private const int Leaves = 18;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Descend to surface: skip air and leaves (spec §5.2)
        int id = world.GetBlockId(x, y, z);
        while ((id == 0 || id == Leaves) && y > 0)
        {
            y--;
            id = world.GetBlockId(x, y, z);
        }

        Block? block = Block.BlocksList[blockId];

        for (int i = 0; i < 128; i++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) == 0
                && (block?.CanBlockStay(world, bx, by, bz) ?? false))
            {
                world.SetBlockAndMetadata(bx, by, bz, blockId, meta);
            }
        }

        return true;
    }
}
