namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Replica of <c>qv extends ig</c> (WorldGenLakes) — carves lake-shaped cavities and
/// fills them with a fluid block (water or lava).
///
/// Algorithm (WorldGenLakes_Spec.md):
///   1. Scan downward from y=255 for ground; baseY = found-y - 4.
///   2. Allocate 16×8×16 boolean working space.
///   3. Place 4-7 overlapping ellipsoids in the working space.
///   4. Validity check: top half must not touch open air; border of bottom half must be solid.
///   5. Carve: y&lt;4 → fluid; y≥4 → air.
///   6. Post-process: grass→dirt if water above; fire if lava at surface.
///
/// Spawn conditions from ChunkProviderGenerate:
///   Water: 1/4 chunks, random position
///   Lava:  1/8 chunks, doubly-biased-low Y (&lt;64 or 1/10 surface allowed)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenLakes_Spec.md
/// </summary>
public sealed class WorldGenLakes
{
    private const int SeaLevel = 64;

    private readonly int _fluidBlockId;

    public WorldGenLakes(int fluidBlockId) => _fluidBlockId = fluidBlockId;

    // ── Generate ──────────────────────────────────────────────────────────────

    public bool Generate(World world, JavaRandom rng, int originX, int originY, int originZ)
    {
        // Step 1 — find ground level
        int scanY = originY;
        while (scanY > 0 && world.GetBlockId(originX, scanY, originZ) == 0)
            scanY--;

        int baseY = scanY - 4;
        if (baseY <= 0) return false;

        // Step 2 — allocate 16×8×16 working space
        var cells = new bool[16, 8, 16];

        // Step 3 — place 4-7 random ellipsoids
        int ellipsoidCount = rng.NextInt(4) + 4; // 4-7
        for (int e = 0; e < ellipsoidCount; e++)
        {
            double cx = rng.NextInt(15) + 0.5;
            double cy = rng.NextInt(7)  + 0.5;
            double cz = rng.NextInt(15) + 0.5;
            double rx = rng.NextInt(7)  + 3.0;
            double ry = rng.NextInt(5)  + 2.0;
            double rz = rng.NextInt(7)  + 3.0;

            for (int x = 1; x < 15; x++)
            for (int y = 1; y <  7; y++)
            for (int z = 1; z < 15; z++)
            {
                double dx = (x - cx) / rx;
                double dyN = (y - cy) / ry;
                double dz = (z - cz) / rz;
                if (dx * dx + dyN * dyN + dz * dz < 1.0)
                    cells[x, y, z] = true;
            }
        }

        // Step 4 — validity check
        for (int x = 0; x < 16; x++)
        for (int y = 0; y <  8; y++)
        for (int z = 0; z < 16; z++)
        {
            if (!cells[x, y, z]) continue;

            int wx = originX + x;
            int wy = baseY   + y;
            int wz = originZ + z;

            if (y >= 4)
            {
                // Top half: no marked cell may be open air
                if (world.GetBlockId(wx, wy, wz) == 0)
                    return false;
            }
            else if (x == 0 || x == 15 || y == 0 || z == 0 || z == 15)
            {
                // Border of bottom half must be solid (non-air, non-fluid)
                int id = world.GetBlockId(wx, wy, wz);
                if (id == 0 || id == 8 || id == 9 || id == 10 || id == 11)
                    return false;
            }
        }

        // Step 5 — carve and fill
        for (int x = 0; x < 16; x++)
        for (int y = 0; y <  8; y++)
        for (int z = 0; z < 16; z++)
        {
            if (!cells[x, y, z]) continue;

            int wx = originX + x;
            int wy = baseY   + y;
            int wz = originZ + z;

            world.SetBlockSilent(wx, wy, wz, y < 4 ? _fluidBlockId : 0);
        }

        // Step 6 — post-processing
        for (int x = 0; x < 16; x++)
        for (int y = 0; y <  8; y++)
        for (int z = 0; z < 16; z++)
        {
            if (!cells[x, y, z]) continue;
            if (y >= 4) continue; // only fluid layer

            int wx = originX + x;
            int wy = baseY   + y;
            int wz = originZ + z;

            int above = world.GetBlockId(wx, wy + 1, wz);
            int cur   = world.GetBlockId(wx, wy,     wz);

            // Grass → Dirt if water above
            if (_fluidBlockId == 8 || _fluidBlockId == 9)
            {
                int below = world.GetBlockId(wx, wy - 1, wz);
                if (below == 2) // grass
                    world.SetBlockSilent(wx, wy - 1, wz, 3); // dirt
            }

            // Lava at surface + air above → fire
            if ((_fluidBlockId == 10 || _fluidBlockId == 11) && cur == _fluidBlockId && above == 0)
                world.SetBlockSilent(wx, wy + 1, wz, 51); // fire
        }

        return true;
    }
}
