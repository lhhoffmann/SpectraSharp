namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Replica of <c>oh</c> (WorldGenEndSpike) — places a cylindrical obsidian pillar
/// with an End Crystal on top and a bedrock cap.
///
/// Spike parameters chosen per attempt:
///   height = nextInt(32) + 6     → [6, 37]
///   radius = nextInt(4)  + 1     → [1, 4]
///
/// Guards: footprint must be air at surface and end stone beneath;
/// all positions within radius must have end stone below.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderEnd_Spec.md §5
/// </summary>
public static class WorldGenEndSpike
{
    private const int ObsidianId = 49;
    private const int BedrockId  = 7;

    /// <summary>
    /// Attempt to place a spike at (x, y, z) where y is the first air block above ground.
    /// Returns true if the spike was placed.
    /// </summary>
    public static bool Generate(IWorld world, JavaRandom rng, int x, int y, int z, int endStoneId)
    {
        // Guard: surface block must be air, block below must be end stone (spec §5.2)
        if (world.GetBlockId(x, y, z) != 0) return false;
        if (world.GetBlockId(x, y - 1, z) != endStoneId) return false;

        int height = rng.NextInt(32) + 6;  // [6, 37]
        int radius = rng.NextInt(4) + 1;   // [1, 4]

        // Validate footprint: every position within circle must have end stone below (spec §5.2)
        for (int bx = x - radius; bx <= x + radius; bx++)
        for (int bz = z - radius; bz <= z + radius; bz++)
        {
            int dx = bx - x;
            int dz = bz - z;
            if (dx * dx + dz * dz <= radius * radius + 1)
            {
                if (world.GetBlockId(bx, y - 1, bz) != endStoneId)
                    return false;
            }
        }

        // Build obsidian cylinder (spec §5.2)
        for (int wy = y; wy < y + height; wy++)
        for (int bx = x - radius; bx <= x + radius; bx++)
        for (int bz = z - radius; bz <= z + radius; bz++)
        {
            int dx = bx - x;
            int dz = bz - z;
            if (dx * dx + dz * dz <= radius * radius + 1)
                world.SetBlock(bx, wy, bz, ObsidianId);
        }

        // Bedrock cap at top (spec §5.2) — End Crystal spawn is a stub (EntityEnderCrystal pending)
        world.SetBlock(x, y + height, z, BedrockId);

        return true;
    }
}
