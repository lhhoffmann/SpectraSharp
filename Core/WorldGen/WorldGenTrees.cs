namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Standard oak tree generator. Spec: <c>gq</c> (WorldGenTrees).
/// Placed by most biomes (90% of tree attempts). Height: 4–6 blocks.
/// </summary>
public class WorldGenTrees(bool silent) : WorldGenerator
{
    private const int LogId    = 17; // yy.J — oak log,    meta 0
    private const int LeavesId = 18; // yy.K — oak leaves, meta 0

    // Virtual hooks for birch subclass
    protected virtual int LogMeta    => 0;
    protected virtual int LeavesMeta => 0;
    protected virtual int MinHeight  => 4;

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Step 1 — choose height (spec §3.2)
        int height = rand.NextInt(3) + MinHeight; // oak [4,6], birch [5,7]

        // Step 2 — Y bounds check
        if (y < 1 || y + height + 1 > World.WorldHeight) return false;

        // Step 3 — clearance check
        for (int dy = y; dy <= y + height + 1; dy++)
        {
            int radius;
            if (dy == y)                         radius = 0; // base: centre only
            else if (dy >= y + height - 1)       radius = 2; // top 2 canopy + 1 above: 5×5
            else                                 radius = 1; // mid trunk: 3×3

            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                int id = world.GetBlockId(x + dx, dy, z + dz);
                if (id != 0 && id != LeavesId) return false;
            }
        }

        // Step 4 — ground check (spec §3.2)
        int belowId = world.GetBlockId(x, y - 1, z);
        if (belowId != 2 && belowId != 3) return false;         // must be grass or dirt
        if (y >= World.WorldHeight - height - 1) return false;

        // Step 5 — convert ground to dirt
        world.SetBlock(x, y - 1, z, 3);

        // Step 6 — place leaves (canopy), 4 layers: dy in {-3, -2, -1, 0} relative to top
        for (int layer = 0; layer < 4; layer++)
        {
            int lY  = y + height - 3 + layer;
            int dy  = lY - (y + height);            // in {-3, -2, -1, 0}
            int radius = 1 - dy / 2;               // integer division: 2,2,1,1

            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                // Corner randomization (spec §3.2): skip 50% of corners on non-top layers
                bool isCorner = Math.Abs(dx) == radius && Math.Abs(dz) == radius;
                if (isCorner && dy != 0 && rand.NextInt(2) == 0) continue;

                int bx = x + dx, bz = z + dz;
                int existing = world.GetBlockId(bx, lY, bz);
                if (!Block.IsOpaqueCubeArr[existing])
                    world.SetBlockAndMetadata(bx, lY, bz, LeavesId, LeavesMeta);
            }
        }

        // Step 7 — place trunk
        for (int i = 0; i < height; i++)
        {
            int id = world.GetBlockId(x, y + i, z);
            if (id == 0 || id == LeavesId)
                world.SetBlockAndMetadata(x, y + i, z, LogId, LogMeta);
        }

        _ = silent; // silent flag noted but both paths use SetBlockAndMetadata for now
        return true;
    }
}
