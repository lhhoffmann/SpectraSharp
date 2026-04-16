namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Thin spruce (Christmas tree) generator. Spec: <c>ty</c> (WorldGenTaiga1).
/// Used by Taiga biome with 67% probability. Height: 6–9. Cone-shaped canopy.
/// </summary>
public sealed class WorldGenTaiga1(bool silent) : WorldGenerator
{
    private const int LogId    = 17; // spruce log,    meta 1
    private const int LeavesId = 18; // spruce leaves, meta 1

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Spec §7.1
        int height      = rand.NextInt(4) + 6;           // [6, 9]
        int bareTrunk   = 1 + rand.NextInt(2);           // [1, 2] bare levels below canopy
        int foliageLayers = height - bareTrunk;
        int maxRadius   = 2 + rand.NextInt(2);           // [2, 3]

        // Y bounds
        if (y + height + 1 >= World.WorldHeight) return false;

        // Ground check
        int below = world.GetBlockId(x, y - 1, z);
        if (below != 2 && below != 3) return false;

        // Clearance check (spec §7.2)
        for (int dy = 0; dy <= height + 1; dy++)
        {
            int r = dy < bareTrunk ? 0 : maxRadius;
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                int id = world.GetBlockId(x + dx, y + dy, z + dz);
                if (id != 0 && id != LeavesId) return false;
            }
        }

        // Convert ground to dirt
        world.SetBlock(x, y - 1, z, 3);

        // Canopy placement — growing/shrinking radius pattern (spec §7.3)
        int curRadius  = rand.NextInt(2); // start at 0 or 1
        int nextRadius = 1;
        int peakRadius = 0;

        for (int layer = 0; layer < foliageLayers; layer++)
        {
            int lY = y + bareTrunk + layer;
            PlaceSquare(world, rand, x, lY, z, curRadius);

            if (curRadius >= nextRadius)
            {
                curRadius  = peakRadius;
                peakRadius = 1;
                nextRadius  = Math.Min(nextRadius + 1, maxRadius);
            }
            else
            {
                curRadius++;
            }
        }

        // Trunk placement (spec §7.4)
        int skipBottom = rand.NextInt(3); // skip 0–2 bottom trunk blocks
        for (int i = 0; i < height - skipBottom; i++)
        {
            int id = world.GetBlockId(x, y + i, z);
            if (id == 0 || id == LeavesId)
                world.SetBlockAndMetadata(x, y + i, z, LogId, 1);
        }

        _ = silent;
        return true;
    }

    private static void PlaceSquare(IWorld world, JavaRandom rand, int cx, int cy, int cz, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        for (int dz = -radius; dz <= radius; dz++)
        {
            // Skip corners if radius > 0
            if (radius > 0 && Math.Abs(dx) == radius && Math.Abs(dz) == radius) continue;
            int id = world.GetBlockId(cx + dx, cy, cz + dz);
            if (!Block.IsOpaqueCubeArr[id])
                world.SetBlockAndMetadata(cx + dx, cy, cz + dz, 18, 1); // spruce leaves
        }
    }
}
