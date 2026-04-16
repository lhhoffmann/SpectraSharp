namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Wide pine generator. Spec: <c>us</c> (WorldGenTaiga2).
/// Used by Taiga biome with 33% probability. Height: 7–11. Top-down expanding canopy.
/// Instantiated fresh per generation call (no shared instance).
/// </summary>
public sealed class WorldGenTaiga2 : WorldGenerator
{
    private const int LogId    = 17; // spruce log,    meta 1
    private const int LeavesId = 18; // spruce leaves, meta 1

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Spec §8.1
        int height        = rand.NextInt(5) + 7;                          // [7, 11]
        int foliageStart  = height - rand.NextInt(2) - 3;                // varies
        int foliageLayers = height - foliageStart;                        // 3–4
        int maxRadius     = 1 + rand.NextInt(foliageLayers + 1);

        // Y bounds
        if (y + height + 1 >= World.WorldHeight) return false;

        // Ground check
        int below = world.GetBlockId(x, y - 1, z);
        if (below != 2 && below != 3) return false;

        // Clearance: centre column
        for (int dy = 0; dy <= height + 1; dy++)
        {
            int id = world.GetBlockId(x, y + dy, z);
            if (id != 0 && id != LeavesId) return false;
        }

        // Convert ground to dirt
        world.SetBlock(x, y - 1, z, 3);

        // Canopy placement top-down (spec §8.2)
        int curRadius = 0;
        for (int layer = foliageLayers - 1; layer >= 0; layer--)
        {
            int lY = y + foliageStart + layer;
            PlaceSquare(world, x, lY, z, curRadius);

            if (curRadius >= 1 && layer == 1) curRadius--;       // shrink near bottom
            else if (curRadius < maxRadius)   curRadius++;        // grow downward
        }

        // Trunk placement (spec §8.3)
        int skipTop = rand.NextInt(3);
        for (int i = 0; i < height - 1; i++)
        {
            int id = world.GetBlockId(x, y + i, z);
            if (id == 0 || id == LeavesId)
                world.SetBlockAndMetadata(x, y + i, z, LogId, 1);
        }
        _ = skipTop; // vanilla skips top logs — trunk still placed from 0..height-1

        return true;
    }

    private static void PlaceSquare(IWorld world, int cx, int cy, int cz, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        for (int dz = -radius; dz <= radius; dz++)
        {
            if (radius > 0 && Math.Abs(dx) == radius && Math.Abs(dz) == radius) continue;
            int id = world.GetBlockId(cx + dx, cy, cz + dz);
            if (!Block.IsOpaqueCubeArr[id])
                world.SetBlockAndMetadata(cx + dx, cy, cz + dz, 18, 1); // spruce leaves
        }
    }
}
