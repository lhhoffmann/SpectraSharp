using SpectraEngine.Core.WorldGen;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>a</c> (ChunkProviderEnd) — End dimension terrain generator.
/// Implements <see cref="IChunkLoader"/>.
///
/// Generates the floating End island via a 3×33×3 density grid with circular shaping.
/// The island is centred on the origin (chunk 0,0). All blocks are pure End Stone (ID 121).
/// The surface pass is a no-op (§3.4 quirk): the density fill never places stone, so the
/// stone-replacement condition never triggers.
///
/// Population delegates to BiomeSky (uu) which places obsidian spikes (1/5 chance per chunk)
/// and spawns the Ender Dragon exactly once at chunk (0,0). Both are stub-implemented.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderEnd_Spec.md §3
/// </summary>
public sealed class ChunkProviderEnd : IChunkLoader
{
    // ── Block ID constants ────────────────────────────────────────────────────

    private const int EndStoneId = 121;    // yy.bJ.bM
    private const int ObsidianId = 49;     // yy.ap.bM
    private const int BedrockId  = 7;      // yy.z.bM

    // ── Fields (spec §3.1) ────────────────────────────────────────────────────

    private readonly JavaRandom              _rand;     // i
    private readonly NoiseGeneratorOctaves   _noiseJ;   // j — 16-oct 3D density A
    private readonly NoiseGeneratorOctaves   _noiseK;   // k — 16-oct 3D density B
    private readonly NoiseGeneratorOctaves   _noiseL;   // l —  8-oct 3D selector
    private readonly NoiseGeneratorOctaves   _noiseA;   // a — 10-oct 2D island-shape X
    private readonly NoiseGeneratorOctaves   _noiseB;   // b — 16-oct 2D island-shape Y (dead code: var18=0)

    private double[]? _densityGrid;        // n — density grid buffer (3×33×3)
    private double[]? _bufF;               // f — island-shape A noise buffer
    private double[]? _bufG;               // g — island-shape B noise buffer (var18 forced to 0)
    private double[]? _bufC;               // c — selector noise buffer
    private double[]? _bufD;               // d — density A noise buffer
    private double[]? _bufE;               // e — density B noise buffer

    // Unused int[32][32] — retained for field-layout parity with original (obf: h)
#pragma warning disable IDE0052
    private readonly int[] _hArray = new int[32 * 32];
#pragma warning restore IDE0052

    private World _world;

    private readonly Dictionary<long, Chunk> _chunks = new();

    // ── Construction (spec §3.1) ──────────────────────────────────────────────

    public ChunkProviderEnd(long seed, World world)
    {
        _world = world;

        // Construction order matters for RNG parity — each constructor call advances _rand
        _rand  = new JavaRandom(seed);
        _noiseJ = new NoiseGeneratorOctaves(_rand, 16);
        _noiseK = new NoiseGeneratorOctaves(_rand, 16);
        _noiseL = new NoiseGeneratorOctaves(_rand, 8);
        _noiseA = new NoiseGeneratorOctaves(_rand, 10);
        _noiseB = new NoiseGeneratorOctaves(_rand, 16);
    }

    public void SetWorld(World world) => _world = world;

    // ── IChunkLoader ──────────────────────────────────────────────────────────

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            chunk = GenerateChunk(chunkX, chunkZ);
            _chunks[key] = chunk;
        }
        return chunk;
    }

    public bool IsChunkLoaded(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        return _chunks.ContainsKey(key);
    }

    public void Tick() { }

    public IEnumerable<(int chunkX, int chunkZ)> GetLoadedChunkCoords()
    {
        foreach (long key in _chunks.Keys)
            yield return ((int)(key >> 32), (int)key);
    }

    // ── generateChunk / b(chunkX, chunkZ) (spec §3.2) ────────────────────────

    private Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        _rand.SetSeed((long)chunkX * 341873128712L + (long)chunkZ * 132897987541L);

        var blocks = new byte[16 * World.WorldHeight * 16];

        FillDensity(chunkX, chunkZ, blocks);   // a(...)
        SurfacePass(chunkX, chunkZ, blocks);   // b(...) — no-op for End (spec §3.4)

        return new Chunk(_world, blocks, chunkX, chunkZ);
    }

    // ── Pass 1: density fill — a(chunkX, chunkZ, bytes, biomes) (spec §3.3) ──

    private void FillDensity(int chunkX, int chunkZ, byte[] blocks)
    {
        // Grid dimensions: 3 × (worldHeight/4+1) × 3
        const int GridX = 3;
        const int GridZ = 3;
        int       gridY = World.WorldHeight / 4 + 1; // 33 for height=128

        int baseX = chunkX * 2; // noise coords in grid units (each grid cell = 4 blocks in Y, 8 in XZ)
        int baseZ = chunkZ * 2;

        // Sample noise into buffers (spec §3.3 Step 1)
        _bufF = _noiseA.Generate2D(_bufF, baseX, baseZ, GridX, GridZ, 1.121, 1.121);
        _bufG = _noiseB.Generate2D(_bufG, baseX, baseZ, GridX, GridZ, 200.0, 200.0);
        _bufC = _noiseL.Generate3D(_bufC, baseX, 0, baseZ, GridX, gridY, GridZ,
                    1368.824 / 80.0, 1368.824 / 160.0, 1368.824 / 80.0);
        _bufD = _noiseJ.Generate3D(_bufD, baseX, 0, baseZ, GridX, gridY, GridZ,
                    1368.824, 1368.824, 1368.824);
        _bufE = _noiseK.Generate3D(_bufE, baseX, 0, baseZ, GridX, gridY, GridZ,
                    1368.824, 1368.824, 1368.824);

        // Build the 3×33×3 density grid (spec §3.3 Step 2 + 3)
        _densityGrid = ComputeDensityGrid(_densityGrid, baseX, baseZ, GridX, gridY, GridZ);

        // Trilinear interpolation: 3×33×3 → 16×128×16 (spec §3.3 Step 4)
        // Each grid cell covers 8 blocks in XZ and 4 blocks in Y.
        int worldBitA = 7; // log2(128) - log2(16) ... actually just use flat index
        for (int gx = 0; gx < GridX - 1; gx++)
        for (int gz = 0; gz < GridZ - 1; gz++)
        for (int gy = 0; gy < gridY - 1; gy++)
        {
            // Corner densities
            double d000 = _densityGrid[Index3(gx,     gy,     gz,     gridY)];
            double d100 = _densityGrid[Index3(gx + 1, gy,     gz,     gridY)];
            double d010 = _densityGrid[Index3(gx,     gy + 1, gz,     gridY)];
            double d110 = _densityGrid[Index3(gx + 1, gy + 1, gz,     gridY)];
            double d001 = _densityGrid[Index3(gx,     gy,     gz + 1, gridY)];
            double d101 = _densityGrid[Index3(gx + 1, gy,     gz + 1, gridY)];
            double d011 = _densityGrid[Index3(gx,     gy + 1, gz + 1, gridY)];
            double d111 = _densityGrid[Index3(gx + 1, gy + 1, gz + 1, gridY)];

            // Steps for interpolation over 8×4×8 sub-cell
            double stepX00 = (d100 - d000) * 0.125; // 1/8
            double stepX10 = (d110 - d010) * 0.125;
            double stepX01 = (d101 - d001) * 0.125;
            double stepX11 = (d111 - d011) * 0.125;

            for (int lx = 0; lx < 8; lx++)
            {
                int worldX = gx * 8 + lx;
                if (worldX >= 16) continue;

                double dLX00 = d000 + stepX00 * lx;
                double dLX10 = d010 + stepX10 * lx;
                double dLX01 = d001 + stepX01 * lx;
                double dLX11 = d011 + stepX11 * lx;

                for (int lz = 0; lz < 8; lz++)
                {
                    int worldZ = gz * 8 + lz;
                    if (worldZ >= 16) continue;

                    double tZ = (double)lz / 8.0;
                    double dLXZ0 = dLX00 + (dLX01 - dLX00) * tZ;
                    double dLXZ1 = dLX10 + (dLX11 - dLX10) * tZ;

                    for (int ly = 0; ly < 4; ly++)
                    {
                        int worldY = gy * 4 + ly;
                        if (worldY >= World.WorldHeight) continue;

                        double tY      = (double)ly / 4.0;
                        double density = dLXZ0 + (dLXZ1 - dLXZ0) * tY;

                        if (density > 0.0)
                        {
                            // Block index layout: (x * 16 + z) * WorldHeight + y
                            int idx = (worldX * 16 + worldZ) * World.WorldHeight + worldY;
                            blocks[idx] = (byte)EndStoneId;
                        }
                    }
                }
            }
        }
    }

    // ── Density grid computation (spec §3.3 Steps 2-3) ───────────────────────

    private double[] ComputeDensityGrid(double[]? grid, int baseX, int baseZ,
                                         int sizeX, int sizeY, int sizeZ)
    {
        grid ??= new double[sizeX * sizeY * sizeZ];
        int halfY = World.WorldHeight / 4 / 2; // = 16

        int xzIndex = 0;
        for (int gx = 0; gx < sizeX; gx++)
        for (int gz = 0; gz < sizeZ; gz++, xzIndex++)
        {
            // Island shaping factor (spec §3.3 Step 2)
            double var16 = (_bufF![xzIndex] + 256.0) / 512.0;
            if (var16 < 0.0) var16 = 0.0;
            if (var16 > 1.0) var16 = 1.0;

            // var18 is computed from _bufG but then forcibly zeroed (spec §11.1 quirk)
            // double var18 = _bufG[xzIndex] / 8000.0;
            // ... normalisation ...
            // var18 = 0.0;  ← forced to zero in original
            double var18 = 0.0;

            var16 += 0.5; // now in [0.5, 1.5]

            // Circular island shaping: grid coords are (gx+baseX, gz+baseZ)
            double localX = gx + baseX;
            double localZ = gz + baseZ;
            double circleValue = 100.0 - Math.Sqrt(localX * localX + localZ * localZ) * 8.0;
            circleValue = Math.Clamp(circleValue, -100.0, 80.0);

            // Per-Y density (spec §3.3 Step 3)
            for (int gy = 0; gy < sizeY; gy++)
            {
                // Y deviation from midpoint
                double yDelta = (gy - halfY) * 8.0 / var16;
                if (yDelta < 0.0) yDelta = -yDelta;

                int idx3 = Index3(gx, gy, gz, sizeY);
                double densityA = _bufD![idx3] / 512.0;
                double densityB = _bufE![idx3] / 512.0;
                double selector = (_bufC![idx3] / 10.0 + 1.0) / 2.0;

                double density;
                if      (selector < 0.0) density = densityA;
                else if (selector > 1.0) density = densityB;
                else                     density = densityA + (densityB - densityA) * selector;

                density -= 8.0;        // bias downward
                density += circleValue;

                // Top ceiling pull: Y > 30 (spec §3.3)
                if (gy > halfY * 2 - 2)
                {
                    double pullFrac = Math.Clamp((gy - 30.0) / 64.0, 0.0, 1.0);
                    density = density * (1.0 - pullFrac) + -3000.0 * pullFrac;
                }

                // Bottom floor pull: Y < 8 (spec §3.3)
                if (gy < 8)
                {
                    double pullFrac = (8.0 - gy) / 7.0;
                    density = density * (1.0 - pullFrac) + -30.0 * pullFrac;
                }

                grid[idx3] = density;
            }
        }
        return grid;
    }

    // ── Pass 2: surface pass — b(chunkX, chunkZ, bytes, biomes) (spec §3.4) ──

    /// <summary>
    /// No-op for The End. Density fill places End Stone (ID 121), not Stone (ID 1),
    /// so the stone-check condition in this pass never fires. Retained for RNG parity.
    /// </summary>
    private static void SurfacePass(int chunkX, int chunkZ, byte[] blocks)
    {
        // The original iterates 16×16 columns but never writes anything.
        // No RNG is consumed here — safe to skip entirely.
    }

    // ── Populate — a(provider, chunkX, chunkZ) (spec §3.6) ──────────────────

    /// <summary>
    /// Decorates the End chunk: obsidian spikes (1/5 chance) via WorldGenEndSpike;
    /// Ender Dragon spawn at chunk (0,0) — stub (EntityEnderDragon spec pending).
    /// </summary>
    public void Populate(int chunkX, int chunkZ)
    {
        bool oldSuppress = _world.SuppressUpdates;
        _world.SuppressUpdates = true;

        // Obsidian spike: 1/5 chance per chunk (spec §4.2 Step 1)
        if (_rand.NextInt(5) == 0)
        {
            int x = chunkX * 16 + _rand.NextInt(16) + 8;
            int z = chunkZ * 16 + _rand.NextInt(16) + 8;
            int surfaceY = _world.GetHeightValue(x, z);
            if (surfaceY > 0)
                WorldGenEndSpike.Generate(_world, _rand, x, surfaceY, z, EndStoneId);
        }

        // Dragon spawn at chunk (0,0) — stub (EntityEnderDragon not yet implemented)
        // if (chunkX == 0 && chunkZ == 0) { ... spawn dragon ... }

        _world.SuppressUpdates = oldSuppress;
    }

    // ── Index helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// 3D density grid index: layout (gx * sizeZ + gz) * sizeY + gy.
    /// </summary>
    private static int Index3(int gx, int gy, int gz, int sizeY)
        => (gx * 3 + gz) * sizeY + gy;
}
