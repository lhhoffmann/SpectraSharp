namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Places lily pads on water surfaces. 10 attempts per call.
/// Spec: <c>jj</c> (WorldGenLilyPad).
///
/// Placement conditions:
///   • Block at (bx, by, bz) must be air.
///   • LilyPad.canBlockStay must return true (requires water at y-1).
///
/// Spread: ±7 X/Z, ±3 Y. Call-site Y is pre-descended to the water surface
/// by the BiomeDecorator loop before invoking this generator (spec §4 step 10).
/// Placed silently (world.d = SetBlockSilent, no metadata).
///
/// Block ID: 111 = lily pad.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.8
/// </summary>
public sealed class WorldGenLilyPad : WorldGenerator
{
    private const int LilyPadId = 111;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        Block? lilyPad = Block.BlocksList[LilyPadId];

        for (int i = 0; i < 10; i++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y + rand.NextInt(4) - rand.NextInt(4);
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) == 0
                && (lilyPad?.CanBlockStay(world, bx, by, bz) ?? false))
            {
                world.SetBlockSilent(bx, by, bz, LilyPadId);
            }
        }

        return true;
    }
}
