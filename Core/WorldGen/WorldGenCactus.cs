namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Places cactus columns. 10 attempts per call.
/// Spec: <c>ade</c> (WorldGenCactus).
///
/// Placement conditions:
///   • Block at (bx, by, bz) must be air.
///   • Cactus.canBlockStay must return true for each column block.
///
/// Height: 1 + nextInt(nextInt(3) + 1) → [1, 3], biased toward 1.
/// Spread: ±7 X/Z, ±3 Y.
/// Placed silently (world.d = SetBlockSilent, no metadata).
///
/// Block ID: 81 = cactus.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.7
/// </summary>
public sealed class WorldGenCactus : WorldGenerator
{
    private const int CactusId = 81;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        Block? cactus = Block.BlocksList[CactusId];

        for (int i = 0; i < 10; i++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) != 0) continue;

            // Height [1, 3], biased toward 1
            int height = 1 + rand.NextInt(rand.NextInt(3) + 1);

            for (int h = 0; h < height; h++)
            {
                if (cactus?.CanBlockStay(world, bx, by + h, bz) ?? false)
                    world.SetBlockSilent(bx, by + h, bz, CactusId);
            }
        }

        return true;
    }
}
