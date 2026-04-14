namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Scatters a single block type (dead bush) over a small surface area. 4 attempts per call.
/// Spec: <c>mb</c> (WorldGenShrub).
///
/// Same algorithm as <see cref="WorldGenTallGrass"/> but with only 4 attempts and
/// silent placement (world.d = SetBlockSilent, no metadata, no notifications).
///
/// Used for: dead bush (ID 32).
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.3
/// </summary>
public sealed class WorldGenShrub(int blockId) : WorldGenerator
{
    private const int Leaves = 18;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Descend to surface: skip air and leaves
        int id = world.GetBlockId(x, y, z);
        while ((id == 0 || id == Leaves) && y > 0)
        {
            y--;
            id = world.GetBlockId(x, y, z);
        }

        Block? block = Block.BlocksList[blockId];

        for (int i = 0; i < 4; i++)
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
