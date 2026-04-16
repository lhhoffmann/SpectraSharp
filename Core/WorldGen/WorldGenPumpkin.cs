namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Places a small pumpkin patch on grass. 64 attempts per call.
/// Spec: <c>sz</c> (WorldGenPumpkin).
///
/// Placement conditions:
///   • Block at (bx, by, bz) must be air.
///   • Block at (bx, by-1, bz) must be grass (ID 2).
///   • Pumpkin.canBlockStay must return true.
///
/// Pumpkin is placed with a random facing metadata [0, 3]:
///   0=south, 1=west, 2=north, 3=east.
///
/// Spread: ±7 X/Z, ±3 Y. A new instance is created per call (spec §4 step 13).
/// Block ID: 86 = pumpkin.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.6
/// </summary>
public sealed class WorldGenPumpkin : WorldGenerator
{
    private const int PumpkinId = 86;
    private const int Grass     = 2;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        Block? pumpkin = Block.BlocksList[PumpkinId];

        for (int i = 0; i < 64; i++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) == 0
                && world.GetBlockId(bx, by - 1, bz) == Grass
                && (pumpkin?.CanBlockStay(world, bx, by, bz) ?? false))
            {
                world.SetBlockAndMetadata(bx, by, bz, PumpkinId, rand.NextInt(4));
            }
        }

        return true;
    }
}
