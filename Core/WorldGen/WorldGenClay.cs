namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Clay disk patch generator. Spec: <c>adp</c> (WorldGenClay).
/// Replaces grass or clay with clay in a small circle. Used in riverbeds.
/// </summary>
public sealed class WorldGenClay(int maxRadius) : WorldGenerator
{
    private const int ClayId = 82; // yy.aW

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Abort if standing in water
        if (world.GetBlockMaterial(x, y, z) == Material.Water) return false;

        // Radius [2, maxRadius-2] (for maxRadius=4: always 2)
        int radius = rand.NextInt(maxRadius - 2) + 2;

        for (int bx = x - radius; bx <= x + radius; bx++)
        for (int bz = z - radius; bz <= z + radius; bz++)
        {
            int dx = bx - x, dz = bz - z;
            if (dx * dx + dz * dz > radius * radius) continue;

            for (int by = y - 1; by <= y + 1; by++) // narrower vertical range than sand disc
            {
                int id = world.GetBlockId(bx, by, bz);
                if (id == 2 || id == ClayId) // grass or clay → replace with clay
                    world.SetBlock(bx, by, bz, ClayId);
            }
        }

        return true;
    }
}
