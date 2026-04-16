namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Swamp oak with vine draping. Spec: <c>qj</c> (WorldGenSwamp).
/// Used exclusively in Swampland biome. Wider canopy than standard oak; allows water below.
/// </summary>
public sealed class WorldGenSwamp : WorldGenerator
{
    private const int LogId    = 17;  // oak log,    meta 0
    private const int LeavesId = 18;  // oak leaves, meta 0
    private const int VineId   = 106; // vine

    // Vine face meta bits (spec §6.1 note): bit0=south, bit1=west, bit2=north, bit3=east
    private static readonly int[] VineFaceMeta = [8, 2, 1, 4]; // west, east, north, south

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Water descent: lower y while standing on water (spec §6.1)
        while (y > 0 && world.GetBlockMaterial(x, y - 1, z) == Material.Water)
            y--;

        // Spec §6.1: height [5, 8]
        int height = rand.NextInt(4) + 5;
        if (y < 1 || y + height + 2 >= World.WorldHeight) return false;

        // Ground check
        int below = world.GetBlockId(x, y - 1, z);
        if (below != 2 && below != 3) return false;

        // Clearance check — allow water at or below base Y
        for (int dy = y; dy <= y + height + 1; dy++)
        {
            int r = dy >= y + height - 1 ? 3 : 2; // top layers: radius 3; others: radius 2
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                int id = world.GetBlockId(x + dx, dy, z + dz);
                // Allow air, leaves, and water at or below base
                if (id == 0 || id == LeavesId) continue;
                if (dy <= y && (id == 8 || id == 9)) continue; // water passable at base
                return false;
            }
        }

        // Convert ground to dirt
        world.SetBlock(x, y - 1, z, 3);

        // Place leaves — 4 layers, wider radius (spec §6.1: radius = 2 - dy/2)
        for (int layer = 0; layer < 4; layer++)
        {
            int lY     = y + height - 3 + layer;
            int dy     = lY - (y + height);        // {-3,-2,-1,0}
            int radius = 2 - dy / 2;               // 3,3,2,2 — wider than oak

            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                bool isCorner = Math.Abs(dx) == radius && Math.Abs(dz) == radius;
                if (isCorner && dy != 0 && rand.NextInt(2) == 0) continue;
                int bx = x + dx, bz = z + dz;
                if (!Block.IsOpaqueCubeArr[world.GetBlockId(bx, lY, bz)])
                    world.SetBlockAndMetadata(bx, lY, bz, LeavesId, 0);
            }
        }

        // Place trunk
        for (int i = 0; i < height; i++)
        {
            int id = world.GetBlockId(x, y + i, z);
            if (id == 0 || id == LeavesId)
                world.SetBlockAndMetadata(x, y + i, z, LogId, 0);
        }

        // Place vines: iterate canopy blocks and hang vines (spec §6.1)
        int canopyBot = y + height - 3;
        int canopyTop = y + height;
        for (int lY = canopyBot; lY <= canopyTop; lY++)
        {
            int dy     = lY - (y + height);
            int radius = 2 - dy / 2;
            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                if (world.GetBlockId(x + dx, lY, z + dz) != LeavesId) continue;
                // Each of 4 horizontal neighbours: 1/4 chance to hang vine
                TryHangVine(world, x + dx - 1, lY, z + dz, rand, VineFaceMeta[0]); // west
                TryHangVine(world, x + dx + 1, lY, z + dz, rand, VineFaceMeta[1]); // east
                TryHangVine(world, x + dx, lY, z + dz - 1, rand, VineFaceMeta[2]); // north
                TryHangVine(world, x + dx, lY, z + dz + 1, rand, VineFaceMeta[3]); // south
            }
        }

        return true;
    }

    /// <summary>
    /// With 25% chance, places a vine at (vx, vy, vz) and drapes it downward up to 4 blocks.
    /// Spec: private helper in qj.
    /// </summary>
    private static void TryHangVine(IWorld world, int vx, int vy, int vz, JavaRandom rand, int faceMeta)
    {
        if (rand.NextInt(4) != 0) return;
        if (world.GetBlockId(vx, vy, vz) != 0) return;

        world.SetBlockAndMetadata(vx, vy, vz, VineId, faceMeta);
        for (int depth = 4; depth > 0; depth--)
        {
            vy--;
            if (world.GetBlockId(vx, vy, vz) != 0) break;
            world.SetBlockAndMetadata(vx, vy, vz, VineId, faceMeta);
        }
    }
}
