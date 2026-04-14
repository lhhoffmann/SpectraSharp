namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Universal ore / patch vein generator. Spec: <c>ky</c> (WorldGenMineable).
///
/// Places a series of overlapping spheres along a randomly-oriented capsule axis,
/// replacing only stone (ID 1) with the target block.
///
/// Key implementation notes:
///   • All placement via <see cref="IWorld.SetBlockSilent"/> — no neighbour notifications.
///   • Uses <see cref="MathHelper"/> sine/cosine (lookup table) for axis orientation (spec: <c>me.a/b</c>).
///   • Y start/end are offset by [−2, 0] relative to the call site (spec: nextInt(3) − 2).
///   • The sine-bulge formula peaks at the capsule midpoint.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenMinable_Spec.md
/// </summary>
public sealed class WorldGenMineable(int blockId, int veinSize) : WorldGenerator
{
    private readonly int _blockId  = blockId;
    private readonly int _veinSize = veinSize;

    /// <summary>
    /// Mod-compatibility overload — wraps <see cref="System.Random"/> as a <see cref="JavaRandom"/>
    /// so transpiled mod code can call this without modification.
    /// Note: RNG sequence will not match vanilla exactly.
    /// </summary>
    public void Generate(IWorld world, System.Random rng, int x, int y, int z)
    {
        var jrng = new JavaRandom();
        jrng.SetSeed(rng.NextInt64());
        Generate(world, jrng, x, y, z);
    }

    /// <summary>
    /// Places the ore vein centred near (x, y, z). Always returns <c>true</c>.
    /// Spec §4: capsule axis → b+1 sphere steps → ellipsoid inside-test → stone replacement.
    /// </summary>
    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        // Step 1 — random capsule axis (spec §4 Step 1)
        float theta = rand.NextFloat() * MathF.PI;

        // XZ endpoints — centred at (x+8, z+8), spread = veinSize/8 blocks
        double startX = (x + 8) + MathHelper.Sin(theta) * _veinSize / 8.0;
        double endX   = (x + 8) - MathHelper.Sin(theta) * _veinSize / 8.0;
        double startZ = (z + 8) + MathHelper.Cos(theta) * _veinSize / 8.0;
        double endZ   = (z + 8) - MathHelper.Cos(theta) * _veinSize / 8.0;

        // Y endpoints — independent, slightly below call site (spec: nextInt(3) − 2)
        double startY = y + rand.NextInt(3) - 2;
        double endY   = y + rand.NextInt(3) - 2;

        // Step 2 — sphere loop (b+1 iterations)
        for (int i = 0; i <= _veinSize; i++)
        {
            // 2a. Interpolated centre
            double cx = startX + (endX - startX) * i / _veinSize;
            double cy = startY + (endY - startY) * i / _veinSize;
            double cz = startZ + (endZ - startZ) * i / _veinSize;

            // 2b. Sphere diameter at this step (sine bulge, random scale)
            double sinePhase  = Math.Sin(i * Math.PI / _veinSize); // 0 at ends, 1 at midpoint
            double randFactor = rand.NextDouble() * _veinSize / 16.0;
            double diameter   = (sinePhase + 1.0) * randFactor + 1.0; // always ≥ 1.0
            double radius     = diameter / 2.0;

            // 2c. Integer bounding box
            int xMin = MathHelper.FloorDouble(cx - radius);
            int xMax = MathHelper.FloorDouble(cx + radius);
            int yMin = MathHelper.FloorDouble(cy - radius);
            int yMax = MathHelper.FloorDouble(cy + radius);
            int zMin = MathHelper.FloorDouble(cz - radius);
            int zMax = MathHelper.FloorDouble(cz + radius);

            // 2d. Block replacement — early-exit ellipsoid test (spec §4 Step 2d)
            for (int bx = xMin; bx <= xMax; bx++)
            {
                double nx = (bx + 0.5 - cx) / radius;
                if (nx * nx >= 1.0) continue; // outside in X

                for (int by = yMin; by <= yMax; by++)
                {
                    double ny = (by + 0.5 - cy) / radius;
                    if (nx * nx + ny * ny >= 1.0) continue; // outside in XY

                    for (int bz = zMin; bz <= zMax; bz++)
                    {
                        double nz = (bz + 0.5 - cz) / radius;
                        if (nx * nx + ny * ny + nz * nz < 1.0
                            && world.GetBlockId(bx, by, bz) == 1) // stone only
                        {
                            world.SetBlockSilent(bx, by, bz, _blockId);
                        }
                    }
                }
            }
        }

        return true; // always true (spec §4)
    }
}
