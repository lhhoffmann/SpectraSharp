namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Ravine carver. Replica of <c>rf</c> (MapGenRavine), based on <c>bz</c> (MapGenBase).
///
/// Ravines are tall, narrow chasms carves into raw stone. Key differences from MapGenCaves:
///   • 2% source-chunk probability (nextInt(50)==0) vs 13% for caves.
///   • Y start range [20, 68] (mid-depth, avoids bedrock and surface).
///   • thicknessMult = 3.0 always → verRadius = horRadius × 3.
///   • Per-Y scale array d[] ∈ [1,4] → rough, irregular walls.
///   • Modified ellipsoid test: (normX²+normZ²)×d[y] + normY²/6 &lt; 1.
///   • No branching; no isMidpoint/room mode.
///   • Pitch damping always 0.7 (no straight-mode).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MapGenRavine_Spec.md
/// </summary>
public sealed class MapGenRavine
{
    private const int SearchRadius = 8; // bz.a — 17×17 source-chunk scan

    // Block IDs
    private const int StoneId        = 1;
    private const int GrassId        = 2;
    private const int DirtId         = 3;
    private const int WaterFlowingId = 8;
    private const int WaterStillId   = 9;
    private const int LavaStillId    = 11;

    // Per-Y scale array — size 128 (WorldHeight), recomputed per ravine (spec §4.2)
    private readonly float[] _yScaleArray = new float[World.WorldHeight];

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Carves ravines into <paramref name="blocks"/> for the target chunk.
    /// Called by ChunkProviderGenerate after caves, before the surface pass.
    /// Spec: <c>bz.a(provider, world, tgtChunkX, tgtChunkZ, blocks)</c>.
    /// </summary>
    public void Generate(World world, int tgtChunkX, int tgtChunkZ, byte[] blocks)
    {
        long worldSeed = world.WorldSeed;

        var baseRng = new JavaRandom(worldSeed);
        long r1 = baseRng.NextLong();
        long r2 = baseRng.NextLong();

        for (int srcX = tgtChunkX - SearchRadius; srcX <= tgtChunkX + SearchRadius; srcX++)
        for (int srcZ = tgtChunkZ - SearchRadius; srcZ <= tgtChunkZ + SearchRadius; srcZ++)
        {
            long seed = (long)srcX * r1 ^ (long)srcZ * r2 ^ worldSeed;
            baseRng.SetSeed(seed);
            GenerateFromSource(world, baseRng, srcX, srcZ, tgtChunkX, tgtChunkZ, blocks);
        }
    }

    // ── Source-chunk ravine spawning (spec §3) ────────────────────────────────

    private void GenerateFromSource(World world, JavaRandom rand,
        int srcX, int srcZ, int tgtX, int tgtZ, byte[] blocks)
    {
        // 2% probability per source chunk (spec §3)
        if (rand.NextInt(50) != 0) return;

        // Starting position (spec §3)
        float startX = srcX * 16 + rand.NextInt(16);
        float startY = rand.NextInt(rand.NextInt(40) + 8) + 20; // [20, 67]
        float startZ = srcZ * 16 + rand.NextInt(16);

        // count = 1; loop runs exactly once (spec §3: "count = 1")
        float yaw    = rand.NextFloat() * MathF.PI * 2f;
        float pitch  = (rand.NextFloat() - 0.5f) * 2f / 8f;           // [-0.25, 0.25]
        float radius = (rand.NextFloat() * 2f + rand.NextFloat()) * 2f; // [0, ~12)

        CarveSegment(world, rand.NextLong(), tgtX, tgtZ, blocks,
            startX, startY, startZ,
            radius, yaw, pitch,
            startStep: 0, totalSteps: 0, thicknessMult: 3.0f);
    }

    // ── Core segment carver (spec §4) ─────────────────────────────────────────

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

        // Initialise step range (spec §4.1 — identical to caves)
        if (totalSteps <= 0)
        {
            int range = SearchRadius * 16 - 16; // = 112
            totalSteps = range - rand.NextInt(range / 4); // [84, 111]
        }

        // branchPoint computed but never used (no branching in ravines — spec §4.1)
        _ = rand.NextInt(totalSteps / 2) + totalSteps / 4;

        // Per-Y scale array d[] (spec §4.2)
        float var27 = 1f;
        for (int sy = 0; sy < World.WorldHeight; sy++)
        {
            if (sy == 0 || rand.NextInt(3) == 0)
                var27 = 1f + rand.NextFloat() * rand.NextFloat(); // [1.0, 2.0)
            _yScaleArray[sy] = var27 * var27; // [1.0, 4.0)
        }

        // Step loop (spec §4.3)
        for (int step = startStep; step < totalSteps; step++)
        {
            // A. Cross-section diameters (spec §4.3 A)
            float sinePhase = MathHelper.Sin(step * MathF.PI / totalSteps);
            float horRadius = 1.5f + sinePhase * radius;
            float verRadius = horRadius * thicknessMult; // = horRadius * 3.0

            horRadius *= rand.NextFloat() * 0.25f + 0.75f; // ±25% random scaling
            verRadius *= rand.NextFloat() * 0.25f + 0.75f;

            // B. Advance position
            float cosPitch = MathHelper.Cos(pitch);
            x += cosPitch * MathHelper.Cos(yaw);
            y += MathHelper.Sin(pitch);
            z += cosPitch * MathHelper.Sin(yaw);

            // C. Pitch damping + direction perturbation (spec §4.3 C — pitch always 0.7)
            pitch     *= 0.7f;
            pitch     += pitchSpeed * 0.05f;
            yaw       += yawSpeed   * 0.05f;
            pitchSpeed *= 0.8f;
            yawSpeed   *= 0.5f;
            pitchSpeed += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 2f;
            yawSpeed   += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 4f;

            // E. Skip 25% of iterations (spec §4.3 E — isMidpoint always false for ravines)
            if (rand.NextInt(4) == 0) continue;

            // F. Distance culling (spec §4.3 F — identical to caves)
            float dx = x - chunkCenterX;
            float dz = z - chunkCenterZ;
            float stepsRemaining = totalSteps - step;
            float maxReach = radius + 2f + 16f;
            if (dx * dx + dz * dz - stepsRemaining * stepsRemaining > maxReach * maxReach) return;

            // G. Bounds check (spec §4.3 G)
            if (x < chunkCenterX - 16 - horRadius * 2) continue;
            if (z < chunkCenterZ - 16 - horRadius * 2) continue;
            if (x > chunkCenterX + 16 + horRadius * 2) continue;
            if (z > chunkCenterZ + 16 + horRadius * 2) continue;

            // H. Bounding box (spec §4.3 H)
            int xMin = Math.Clamp((int)(x - horRadius) - tgtX * 16 - 1, 0, 16);
            int xMax = Math.Clamp((int)(x + horRadius) - tgtX * 16 + 1, 0, 16);
            int yMin = Math.Clamp((int)(y - verRadius) - 1, 1, World.WorldHeight - 8);
            int yMax = Math.Clamp((int)(y + verRadius) + 1, 1, World.WorldHeight - 8);
            int zMin = Math.Clamp((int)(z - horRadius) - tgtZ * 16 - 1, 0, 16);
            int zMax = Math.Clamp((int)(z + horRadius) - tgtZ * 16 + 1, 0, 16);

            // I. Water abort (spec §4.3 I — identical to caves: scan bbox border)
            bool hasWater = false;
            for (int bx = xMin; bx < xMax && !hasWater; bx++)
            for (int bz = zMin; bz < zMax && !hasWater; bz++)
            {
                for (int by = yMax + 1; by >= yMin - 1; by--)
                {
                    if (by >= World.WorldHeight) continue;
                    int idx2 = (bx * 16 + bz) * World.WorldHeight + by;
                    if (blocks[idx2] == WaterFlowingId || blocks[idx2] == WaterStillId)
                    {
                        hasWater = true;
                        break;
                    }
                    // Only scan one layer each side of bbox
                    if (by != yMax + 1 && bx != xMin && bx != xMax - 1
                        && bz != zMin && bz != zMax - 1)
                        by = yMin;
                }
            }
            if (hasWater) continue;

            // J. Carving with ravine-specific ellipsoid test (spec §4.3 J)
            for (int bx = xMin; bx < xMax; bx++)
            {
                float normX = (bx + tgtX * 16 + 0.5f - x) / horRadius;
                if (normX * normX >= 1f) continue;

                for (int bz = zMin; bz < zMax; bz++)
                {
                    float normZ = (bz + tgtZ * 16 + 0.5f - z) / horRadius;
                    if (normX * normX + normZ * normZ >= 1f) continue;

                    bool markedGrass = false;
                    int  baseIdx     = (bx * 16 + bz) * World.WorldHeight;

                    // Iterate Y downward from yMax-1 to yMin
                    for (int by = yMax - 1; by >= yMin; by--)
                    {
                        float normY = (by + 0.5f - y) / verRadius;

                        // *** RAVINE ellipsoid test ***
                        float scale = by >= 0 && by < World.WorldHeight
                            ? _yScaleArray[by] : 1f;
                        if ((normX * normX + normZ * normZ) * scale + normY * normY / 6f < 1f)
                        {
                            byte block = blocks[baseIdx + by];

                            if (block == GrassId) markedGrass = true;

                            if (block == StoneId || block == DirtId || block == GrassId)
                            {
                                if (by < 10)
                                {
                                    blocks[baseIdx + by] = LavaStillId; // lava seam
                                }
                                else
                                {
                                    blocks[baseIdx + by] = 0; // air

                                    // Surface grass restoration (spec §4.3 J)
                                    if (markedGrass && by > 0 && blocks[baseIdx + by - 1] == DirtId)
                                    {
                                        // Restore biome top block on the dirt one below
                                        // World biome at world coords (tgtX*16+bx, tgtZ*16+bz):
                                        int worldX = tgtX * 16 + bx;
                                        int worldZ = tgtZ * 16 + bz;
                                        byte topBlock = world.ChunkManager != null
                                            ? world.ChunkManager.GetBiomeAt(worldX, worldZ).TopBlockId
                                            : (byte)GrassId;
                                        blocks[baseIdx + by - 1] = topBlock;
                                        markedGrass = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
