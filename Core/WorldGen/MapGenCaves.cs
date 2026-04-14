namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Underground cave carver. Spec: <c>ln</c> (MapGenCaves), <c>bz</c> (MapGenBase).
///
/// Operates on the raw <c>byte[]</c> block array produced by the terrain pass,
/// before any Chunk object exists. The 17×17 source-chunk neighbourhood means caves
/// are continuous across chunk boundaries.
///
/// Block array layout (matching Chunk): <c>index = (localX * 16 + localZ) * 128 + y</c>
///
/// Key quirks preserved:
///   • 87% of source chunks contribute zero caves (nextInt(15) != 0 zeroes count).
///   • Floor lava seam: Y &lt; 10 → lava_still (ID 11) instead of air.
///   • Floor guard: normY &lt;= -0.7 → skip (no flat cave floors).
///   • Water abort: skip entire carving step when any water detected in bbox.
///   • Grass surface restoration: exposed dirt below a former grass block becomes biome topBlock.
///   • Branch parent terminates after spawning two branches.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MapGenCaves_Spec.md
/// </summary>
public sealed class MapGenCaves
{
    // ── MapGenBase constants (spec §3) ────────────────────────────────────────

    private const int SearchRadius = 8; // bz.a — 17×17 source-chunk scan per target

    // Block IDs (spec §2 references to yy statics)
    private const int StoneId        = 1;
    private const int GrassId        = 2;
    private const int DirtId         = 3;
    private const int WaterFlowingId = 8;
    private const int WaterStillId   = 9;
    private const int LavaStillId    = 11;

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Carves caves into <paramref name="blocks"/> for the target chunk.
    /// Called by ChunkProviderGenerate after the density pass, before the surface pass.
    /// Spec: <c>bz.a(provider, world, tgtChunkX, tgtChunkZ, blocks)</c>.
    /// </summary>
    public void Generate(World world, int tgtChunkX, int tgtChunkZ, byte[] blocks)
    {
        long worldSeed = world.WorldSeed;

        // Derive two RNG constants from world seed (spec §3 MapGenBase entry point)
        var baseRng = new JavaRandom(worldSeed);
        long r1 = baseRng.NextLong();
        long r2 = baseRng.NextLong();

        // 17×17 source-chunk scan
        for (int srcX = tgtChunkX - SearchRadius; srcX <= tgtChunkX + SearchRadius; srcX++)
        for (int srcZ = tgtChunkZ - SearchRadius; srcZ <= tgtChunkZ + SearchRadius; srcZ++)
        {
            long seed = (long)srcX * r1 ^ (long)srcZ * r2 ^ worldSeed;
            baseRng.SetSeed(seed);
            GenerateFromSource(world, baseRng, srcX, srcZ, tgtChunkX, tgtChunkZ, blocks);
        }
    }

    // ── Source-chunk cave spawning (spec §4) ──────────────────────────────────

    private void GenerateFromSource(World world, JavaRandom rand,
        int srcX, int srcZ, int tgtX, int tgtZ, byte[] blocks)
    {
        // Cave count distribution (spec §4.1): triple-nested nextInt → very skewed
        int count = rand.NextInt(rand.NextInt(rand.NextInt(40) + 1) + 1);

        // 87% of source chunks contribute nothing
        if (rand.NextInt(15) != 0) count = 0;

        for (int cave = 0; cave < count; cave++)
        {
            // Starting position within source chunk (spec §4.2)
            float startX = srcX * 16 + rand.NextInt(16);
            float startY = rand.NextInt(rand.NextInt(World.WorldHeight - 8) + 8);
            float startZ = srcZ * 16 + rand.NextInt(16);

            int extraBranches = 1; // always at least 1 tunnel

            // Room + extra branches (25% chance)
            if (rand.NextInt(4) == 0)
            {
                // Room: single segment with thicknessMult = 0.5, startStep = -1
                CarveSegment(world, rand.NextLong(), tgtX, tgtZ, blocks,
                    startX, startY, startZ,
                    1.0f + rand.NextFloat() * 6.0f, // radius [1, 7]
                    rand.NextFloat() * MathF.PI * 2f,
                    (rand.NextFloat() - 0.5f) * 2f / 8f,
                    -1, 0, 0.5f);

                extraBranches += rand.NextInt(4); // 0–3 additional tunnels
            }

            // One or more tunnels from same start
            for (int t = 0; t < extraBranches; t++)
            {
                float yaw   = rand.NextFloat() * MathF.PI * 2f;
                float pitch = (rand.NextFloat() - 0.5f) * 2f / 8f;
                float radius = rand.NextFloat() * 2.0f + rand.NextFloat();

                // 10% chance: extra-wide cave
                if (rand.NextInt(10) == 0)
                    radius *= rand.NextFloat() * rand.NextFloat() * 3.0f + 1.0f;

                CarveSegment(world, rand.NextLong(), tgtX, tgtZ, blocks,
                    startX, startY, startZ,
                    radius, yaw, pitch,
                    0, 0, 1.0f);
            }
        }
    }

    // ── Core segment carver (spec §5) ─────────────────────────────────────────

    /// <summary>
    /// Recursively carves one cave/tunnel segment.
    ///   <paramref name="startStep"/> = -1 → room (single midpoint step, thicknessMult 0.5)
    ///   <paramref name="startStep"/> = 0  → fresh tunnel
    ///   <paramref name="startStep"/> > 0  → branch continuing at that step
    /// </summary>
    private void CarveSegment(World world, long seed, int tgtX, int tgtZ, byte[] blocks,
        float x, float y, float z,
        float radius, float yaw, float pitch,
        int startStep, int totalSteps, float thicknessMult)
    {
        float chunkCenterX = tgtX * 16 + 8;
        float chunkCenterZ = tgtZ * 16 + 8;

        float pitchSpeed = 0f;
        float yawSpeed   = 0f;

        var rand = new JavaRandom(seed);

        // Initialise step range (spec §5.1)
        if (totalSteps <= 0)
        {
            int range = SearchRadius * 16 - 16; // = 112
            totalSteps = range - rand.NextInt(range / 4);
        }

        bool isMidpoint = (startStep == -1);
        if (isMidpoint) startStep = totalSteps / 2;

        int branchPoint = rand.NextInt(totalSteps / 2) + totalSteps / 4;

        // 25% chance: straight tunnel (lower pitch damping)
        bool isStraight = rand.NextInt(6) == 0;

        // Step loop (spec §5.2)
        for (int step = startStep; step < totalSteps; step++)
        {
            // A. Cross-section diameters
            float sinePhase  = MathHelper.Sin(step * MathF.PI / totalSteps);
            float horDiam    = 1.5f + sinePhase * radius;
            float verDiam    = horDiam * thicknessMult;

            // B. Advance position
            x += MathHelper.Cos(pitch) * MathHelper.Cos(yaw);
            y += MathHelper.Sin(pitch);
            z += MathHelper.Cos(pitch) * MathHelper.Sin(yaw);

            // C. Perturb direction
            pitch   *= isStraight ? 0.92f : 0.70f;
            pitch   += pitchSpeed * 0.1f;
            yaw     += yawSpeed   * 0.1f;
            pitchSpeed *= 0.9f;
            yawSpeed   *= 0.75f;
            pitchSpeed += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 2.0f;
            yawSpeed   += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 4.0f;

            // D. Branch spawning (spec §5.2 D)
            if (!isMidpoint && step == branchPoint && radius > 1.0f && totalSteps > 0)
            {
                CarveSegment(world, rand.NextLong(), tgtX, tgtZ, blocks,
                    x, y, z,
                    rand.NextFloat() * 0.5f + 0.5f,
                    yaw - MathF.PI / 2f, pitch / 3f,
                    step, totalSteps, 1.0f);

                CarveSegment(world, rand.NextLong(), tgtX, tgtZ, blocks,
                    x, y, z,
                    rand.NextFloat() * 0.5f + 0.5f,
                    yaw + MathF.PI / 2f, pitch / 3f,
                    step, totalSteps, 1.0f);

                return; // parent terminates at branch point
            }

            // E. Skip carving 75% of steps (spec §5.2 E)
            if (rand.NextInt(4) == 0) continue;

            // F. Distance culling (spec §5.2 F)
            float dx = x - chunkCenterX;
            float dz = z - chunkCenterZ;
            float stepsLeft = totalSteps - step;
            float maxReach  = radius + 2.0f + 16.0f;
            if (dx * dx + dz * dz - stepsLeft * stepsLeft > maxReach * maxReach) return;

            // G. Bounds check (spec §5.2 G) — skip step if clearly outside chunk area
            if (x < chunkCenterX - 16 - horDiam * 2) continue;
            if (z < chunkCenterZ - 16 - horDiam * 2) continue;
            if (x > chunkCenterX + 16 + horDiam * 2) continue;
            if (z > chunkCenterZ + 16 + horDiam * 2) continue;

            // H. Compute integer bounding box clamped to chunk (spec §5.2 H)
            int xMin = Math.Clamp(MathHelper.FloorDouble(x - horDiam) - tgtX * 16 - 1, 0, 16);
            int xMax = Math.Clamp(MathHelper.FloorDouble(x + horDiam) - tgtX * 16 + 1, 0, 16);
            int yMin = Math.Clamp(MathHelper.FloorDouble(y - verDiam) - 1, 1, World.WorldHeight - 8);
            int yMax = Math.Clamp(MathHelper.FloorDouble(y + verDiam) + 1, 1, World.WorldHeight - 8);
            int zMin = Math.Clamp(MathHelper.FloorDouble(z - horDiam) - tgtZ * 16 - 1, 0, 16);
            int zMax = Math.Clamp(MathHelper.FloorDouble(z + horDiam) - tgtZ * 16 + 1, 0, 16);

            // H. Water proximity check (spec §5.2 H) — abort this step if water nearby
            if (HasWaterInBounds(blocks, xMin, xMax, yMin, yMax, zMin, zMax)) continue;

            // I. Carve blocks (spec §5.2 I)
            bool markedGrass = false;
            for (int bx = xMin; bx < xMax; bx++)
            {
                float nx = ((bx + tgtX * 16) + 0.5f - x) / (horDiam / 2.0f);
                if (nx * nx >= 1.0f) continue;

                for (int bz = zMin; bz < zMax; bz++)
                {
                    float nz = ((bz + tgtZ * 16) + 0.5f - z) / (horDiam / 2.0f);
                    if (nx * nx + nz * nz >= 1.0f) continue;

                    // Scan Y downward
                    for (int by = yMax - 1; by >= yMin; by--)
                    {
                        int idx = (bx * 16 + bz) * World.WorldHeight + by;
                        float ny = (by + 0.5f - y) / (verDiam / 2.0f);

                        // Floor guard (spec §5.2 I): skip bottom of ellipsoid
                        if (ny <= -0.7f) continue;

                        if (nx * nx + ny * ny + nz * nz < 1.0f)
                        {
                            byte block = blocks[idx];

                            if (block == GrassId) markedGrass = true;

                            if (block == StoneId || block == DirtId || block == GrassId)
                            {
                                if (by < 10)
                                {
                                    blocks[idx] = LavaStillId; // lava seam below Y=10
                                }
                                else
                                {
                                    blocks[idx] = 0; // air

                                    // Grass surface restoration (spec §5.2 I)
                                    if (markedGrass && idx >= 1 && blocks[idx - 1] == DirtId)
                                    {
                                        // idx - 1 = same XZ, Y-1 (array: (x*16+z)*128+y)
                                        byte topBlock = GetBiomeTopBlock(world, bx + tgtX * 16, bz + tgtZ * 16);
                                        blocks[idx - 1] = topBlock;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // J. Room exit (spec §5.2 J)
            if (isMidpoint) break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the bounding box border for water blocks (spec §5.2 H).
    /// Returns true if any water (flowing or still) is found.
    /// </summary>
    private static bool HasWaterInBounds(byte[] blocks,
        int xMin, int xMax, int yMin, int yMax, int zMin, int zMax)
    {
        for (int bx = xMin; bx < xMax; bx++)
        for (int bz = zMin; bz < zMax; bz++)
        {
            // Scan downward — inner columns can skip to yMin
            for (int by = yMax + 1; by >= yMin - 1; by--)
            {
                if (by < 0 || by >= World.WorldHeight) continue;
                int idx = (bx * 16 + bz) * World.WorldHeight + by;
                if (idx < 0 || idx >= blocks.Length) continue;

                byte b = blocks[idx];
                if (b == WaterFlowingId || b == WaterStillId) return true;

                // Skip inner column (not on any face): jump to yMin
                bool onFace = bx == xMin || bx == xMax - 1
                           || bz == zMin || bz == zMax - 1
                           || by == yMin - 1 || by == yMax + 1;
                if (!onFace) by = yMin; // jump to bottom of inner column
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the topBlock ID for the biome at world coordinates (wx, wz).
    /// Fallback = grass (ID 2) if WorldChunkManager is unavailable.
    /// </summary>
    private static byte GetBiomeTopBlock(World world, int wx, int wz)
    {
        if (world.ChunkManager != null)
        {
            BiomeGenBase biome = world.ChunkManager.GetBiomeAt(wx, wz);
            return biome.TopBlockId;
        }
        return GrassId;
    }
}
