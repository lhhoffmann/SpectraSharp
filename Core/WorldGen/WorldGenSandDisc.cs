namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Circular disk patch generator. Spec: <c>fc</c> (WorldGenSandDisc).
/// Replaces grass/dirt with the target block in a circle. Used for sand and gravel patches.
/// </summary>
public sealed class WorldGenSandDisc(int maxRadius, int replacementBlockId) : WorldGenerator
{
    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Step 1: abort if standing in water (spec §9.1)
        if (world.GetBlockMaterial(x, y, z) == Material.Water) return false;

        // Step 2: radius [2, maxRadius-2] (spec: nextInt(b-2)+2, for b=7 → [2,5])
        int radius = rand.NextInt(maxRadius - 2) + 2;

        // Step 3: place disk (spec §9.1)
        for (int bx = x - radius; bx <= x + radius; bx++)
        for (int bz = z - radius; bz <= z + radius; bz++)
        {
            int dx = bx - x, dz = bz - z;
            if (dx * dx + dz * dz > radius * radius) continue; // circle boundary

            for (int by = y - 2; by <= y + 2; by++)
            {
                int id = world.GetBlockId(bx, by, bz);
                if (id == 2 || id == 3) // grass or dirt
                    world.SetBlock(bx, by, bz, replacementBlockId);
            }
        }

        return true;
    }
}
