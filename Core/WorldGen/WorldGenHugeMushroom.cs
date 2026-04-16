namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Generates a giant brown or red mushroom.
/// Spec: <c>acp</c> (WorldGenHugeMushroom).
///
/// Constructor variants:
///   <c>acp(int type)</c> — fixed type: 0 = brown flat cap, 1 = red dome cap.
///   <c>acp()</c>         — type = −1 → random type chosen each Generate call.
///
/// Height: nextInt(3) + 4 → [4, 6].
///
/// Ground requirements:
///   • Block below must be dirt (ID 3), grass (ID 2), or mycelium (ID 110).
///   • Brown mushroom (ID 39) canBlockStay must return true at the target position.
///
/// Space check: 7×7 area (radius 3) is checked for opaque blocks at each level
/// from base (radius 0) up to height+1 (radius 3). Any opaque block → abort.
///
/// Cap placement (placed with notifications, SetBlockAndMetadata):
///   Brown (type 0): 3 layers from (y+height-2) to (y+height), radius 3.
///   Red  (type 1): 4 layers from (y+height-3) to (y+height),
///                  inner layers use radius 2, top layer radius 3.
///   Corner positions (|dx|==3 AND |dz|==3) are always skipped.
///
/// Cap meta (face-direction scheme, spec §5.9):
///   1=NW, 2=N, 3=NE, 4=W, 5=top, 6=E, 7=SW, 8=S, 9=SE, 10=stem, 0=interior.
///
/// Stem: placed with meta 10 from base to (y+height-1).
///
/// Block IDs: 99 = brown mushroom cap, 100 = red mushroom cap.
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeDecorator_Spec.md §5.9
/// </summary>
public sealed class WorldGenHugeMushroom(int type = -1) : WorldGenerator
{
    // Ground block IDs
    private const int Dirt      = 3;
    private const int Grass     = 2;
    private const int Mycelium  = 110;

    // Mushroom block IDs
    private const int BrownMushroom    = 39;  // canBlockStay source
    private const int BrownMushroomCap = 99;
    private const int RedMushroomCap   = 100;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        int resolvedType = type == -1 ? rand.NextInt(2) : type;
        int height       = rand.NextInt(3) + 4;

        // Ground check
        int below = world.GetBlockId(x, y - 1, z);
        if (below != Dirt && below != Grass && below != Mycelium) return false;

        // canBlockStay check using the brown mushroom block
        if (!(Block.BlocksList[BrownMushroom]?.CanBlockStay(world, x, y, z) ?? true)) return false;

        // Space check: from base (radius 0) to height+1 (radius 3)
        for (int dy = 0; dy <= height + 1; dy++)
        {
            int radius = dy == 0 ? 0 : 3;
            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                if (Block.IsOpaqueCubeArr[world.GetBlockId(x + dx, y + dy, z + dz)])
                    return false;
            }
        }

        // Convert ground to dirt (spec: world.d() silent)
        world.SetBlockSilent(x, y - 1, z, Dirt);

        int capBlockId = resolvedType == 0 ? BrownMushroomCap : RedMushroomCap;

        // Place cap layers
        int capStart  = resolvedType == 0 ? y + height - 2 : y + height - 3;
        int capEnd    = y + height;

        for (int capY = capStart; capY <= capEnd; capY++)
        {
            // Red mushroom: top layer uses radius 3, inner layers radius 2
            int capRadius = (resolvedType == 1 && capY < capEnd) ? 2 : 3;

            for (int dx = -capRadius; dx <= capRadius; dx++)
            for (int dz = -capRadius; dz <= capRadius; dz++)
            {
                // Always skip corners at max radius
                if (Math.Abs(dx) == 3 && Math.Abs(dz) == 3) continue;

                int bx = x + dx;
                int bz = z + dz;

                if (!Block.IsOpaqueCubeArr[world.GetBlockId(bx, capY, bz)])
                {
                    int meta = CapMeta(dx, dz, capRadius);
                    world.SetBlockAndMetadata(bx, capY, bz, capBlockId, meta);
                }
            }
        }

        // Place stem from base to height-1 (meta 10)
        for (int dy = 0; dy < height; dy++)
        {
            if (!Block.IsOpaqueCubeArr[world.GetBlockId(x, y + dy, z)])
                world.SetBlockAndMetadata(x, y + dy, z, capBlockId, 10);
        }

        return true;
    }

    /// <summary>
    /// Returns cap face meta for a block at (dx, dz) relative to stem.
    /// Edge positions expose the corresponding cardinal/diagonal face.
    /// Interior positions (not on the outer ring) get meta 5 (top face only).
    /// Spec: 1=NW, 2=N, 3=NE, 4=W, 5=top, 6=E, 7=SW, 8=S, 9=SE.
    /// </summary>
    private static int CapMeta(int dx, int dz, int radius)
    {
        int edgeX = dx == -radius ? -1 : dx == radius ? 1 : 0;
        int edgeZ = dz == -radius ? -1 : dz == radius ? 1 : 0;

        return (edgeX, edgeZ) switch
        {
            (-1, -1) => 1, // NW
            ( 0, -1) => 2, // N
            ( 1, -1) => 3, // NE
            (-1,  0) => 4, // W
            ( 0,  0) => 5, // top (interior)
            ( 1,  0) => 6, // E
            (-1,  1) => 7, // SW
            ( 0,  1) => 8, // S
            ( 1,  1) => 9, // SE
            _         => 5
        };
    }
}
