using SpectraSharp.Core.WorldGen;
using SpectraSharp.Core.WorldGen.NetherFortress;

namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>jv</c> (ChunkProviderHell) — Nether dimension terrain generator.
/// Implements <see cref="IChunkLoader"/>.
///
/// Generation sequence per chunk:
///   Pass 1 — 3D density noise → netherrack / lava column (<see cref="FillDensityTerrain"/>)
///   Pass 2 — Surface pass → bedrock, soul sand, gravel patches (<see cref="FillSurface"/>)
///   Pass 3 — Cave carving (<see cref="MapGenNetherCaves"/>)
///   Pass 4 — Fortress outlines (<see cref="MapGenNetherBridge"/>)
///
/// Y-shape curve: <c>cos(y * π * 6 / sizeY) * 2 − cubicPullDown(mirror)</c>
/// creates the characteristic double-floor Nether structure.
///
/// Quirks preserved (spec §13):
///   1. <c>rng.nextInt(1) == 0</c> always true → mushrooms placed on every chunk.
///   2. Dead shape noise arrays <c>g[]</c> / <c>h[]</c> are computed to advance RNG state.
///   3. Glowstone clusters grow downward only.
///   4. WorldGenGlowStone1 and WorldGenGlowStone2 are identical but separate instances.
///   5. Lava pool: temporarily sets world.SuppressUpdates = true during onBlockAdded.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderHell_Spec.md
/// </summary>
public sealed class ChunkProviderHell : IChunkLoader
{
    // ── Block IDs (spec §1) ───────────────────────────────────────────────────

    private const int AirId          = 0;
    private const int BedrockId      = 7;
    private const int LavaStillId    = 11;
    private const int LavaFlowingId  = 10;
    private const int FireId         = 51;
    private const int BrownMushroomId = 39;
    private const int RedMushroomId  = 40;
    private const int NetherrackId   = 87;
    private const int SoulSandId     = 88;
    private const int GlowstoneId    = 89;
    private const int GravelId       = 13;

    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    private readonly JavaRandom _rand;               // i

    // Density noise generators (construction order matches spec §3)
    private readonly NoiseGeneratorOctaves _noiseJ;  // j — 16 oct, density A
    private readonly NoiseGeneratorOctaves _noiseK;  // k — 16 oct, density B
    private readonly NoiseGeneratorOctaves _noiseL;  // l —  8 oct, blend
    private readonly NoiseGeneratorOctaves _noiseM;  // m —  4 oct, surface (q, r)
    private readonly NoiseGeneratorOctaves _noiseN;  // n —  4 oct, surface depth (s)
    private readonly NoiseGeneratorOctaves _noiseA;  // a — 10 oct, dead shape X
    private readonly NoiseGeneratorOctaves _noiseB;  // b — 16 oct, dead shape Z

    // Surface noise buffers (reused per chunk)
    private double[]? _surfaceQ;  // q
    private double[]? _surfaceR;  // r
    private double[]? _surfaceS;  // s

    // Density grid working buffers
    private double[]? _bufD, _bufE, _bufF, _bufG, _bufH;

    // Density grid result buffer (5 × gridY × 5)
    private double[]? _densityGrid; // p

    private World _world;
    private readonly Dictionary<long, Chunk> _chunks = new();

    // Cave carver and fortress generator
    private readonly MapGenNetherCaves    _caveCarver = new();
    private readonly MapGenNetherBridge   _fortressGen = new();

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    public ChunkProviderHell(long seed, World? world = null)
    {
        _world = world!;

        _rand = new JavaRandom(seed);

        // Construction order matters — each constructor call advances _rand (spec §3)
        _noiseJ = new NoiseGeneratorOctaves(_rand, 16);
        _noiseK = new NoiseGeneratorOctaves(_rand, 16);
        _noiseL = new NoiseGeneratorOctaves(_rand,  8);
        _noiseM = new NoiseGeneratorOctaves(_rand,  4);
        _noiseN = new NoiseGeneratorOctaves(_rand,  4);
        _noiseA = new NoiseGeneratorOctaves(_rand, 10); // dead shape X — consumed for RNG parity
        _noiseB = new NoiseGeneratorOctaves(_rand, 16); // dead shape Z — consumed for RNG parity

        _fortressGen.SetWorldSeed(seed);
    }

    /// <summary>Sets the world reference after construction.</summary>
    public void SetWorld(World world) => _world = world;

    // ── IChunkLoader ──────────────────────────────────────────────────────────

    public Chunk GetChunk(int chunkX, int chunkZ)
    {
        long key = (long)chunkX << 32 | (uint)chunkZ;
        if (_chunks.TryGetValue(key, out Chunk? existing)) return existing;

        Chunk chunk = GenerateChunk(chunkX, chunkZ);
        _chunks[key] = chunk;
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

    // ── generateChunk / b(int, int) (spec §4) ────────────────────────────────

    private Chunk GenerateChunk(int chunkX, int chunkZ)
    {
        _rand.SetSeed((long)chunkX * 341873128712L + (long)chunkZ * 132897987541L);

        byte[] blocks = new byte[16 * World.WorldHeight * 16];

        FillDensityTerrain(chunkX, chunkZ, blocks);  // Pass 1
        FillSurface(chunkX, chunkZ, blocks);          // Pass 2
        _caveCarver.Generate(_world, chunkX, chunkZ, blocks); // Pass 3
        // Pass 4: fortress outlines — block placement happens in Populate

        var chunk = new Chunk(_world, blocks, chunkX, chunkZ);
        // Populate is called externally; not triggered here (same pattern as ChunkProviderGenerate)
        return chunk;
    }

    // ── Pass 1: Density terrain — a(int, int, byte[]) (spec §5) ─────────────

    private void FillDensityTerrain(int chunkX, int chunkZ, byte[] blocks)
    {
        const int GridX  = 5;
        const int GridZ  = 5;
        int       gridY  = World.WorldHeight / 8 + 1; // 17 for height=128
        const int LavaLevel = 32;

        // Build density grid (5 × 17 × 5)
        _densityGrid = ComputeDensityGrid(
            _densityGrid, chunkX * 4, 0, chunkZ * 4,
            GridX, gridY, GridZ);

        // Trilinear interpolation: 4 XZ cells × 16 Y cells, each 4×8 voxels
        for (int xCell = 0; xCell < 4; xCell++)
        for (int zCell = 0; zCell < 4; zCell++)
        for (int yCell = 0; yCell < World.WorldHeight / 8; yCell++)
        {
            double d000 = Density(xCell,     yCell,     zCell,     GridX, gridY, GridZ);
            double d001 = Density(xCell,     yCell,     zCell + 1, GridX, gridY, GridZ);
            double d100 = Density(xCell + 1, yCell,     zCell,     GridX, gridY, GridZ);
            double d101 = Density(xCell + 1, yCell,     zCell + 1, GridX, gridY, GridZ);
            double dY00 = (Density(xCell,     yCell + 1, zCell,     GridX, gridY, GridZ) - d000) * 0.125;
            double dY01 = (Density(xCell,     yCell + 1, zCell + 1, GridX, gridY, GridZ) - d001) * 0.125;
            double dY10 = (Density(xCell + 1, yCell + 1, zCell,     GridX, gridY, GridZ) - d100) * 0.125;
            double dY11 = (Density(xCell + 1, yCell + 1, zCell + 1, GridX, gridY, GridZ) - d101) * 0.125;

            for (int fy = 0; fy < 8; fy++)
            {
                double dX0   = d000;
                double dX1   = d001;
                double dZ0   = (d100 - d000) * 0.25;
                double dZ1   = (d101 - d001) * 0.25;

                for (int fx = 0; fx < 4; fx++)
                {
                    double dZ     = dX0;
                    double dZstep = (dX1 - dX0) * 0.25;

                    for (int fz = 0; fz < 4; fz++)
                    {
                        int absX = fx + xCell * 4;
                        int absY = yCell * 8 + fy;
                        int absZ = fz + zCell * 4;
                        int idx  = BlockIndex(absX, absY, absZ);

                        byte block = 0;
                        if (absY < LavaLevel) block = LavaStillId;
                        if (dZ > 0.0)         block = NetherrackId;  // overrides lava

                        blocks[idx] = block;
                        dZ += dZstep;
                    }
                    dX0 += dZ0;
                    dX1 += dZ1;
                }

                d000 += dY00; d001 += dY01; d100 += dY10; d101 += dY11;
            }
        }
    }

    // Read from density grid at grid cell (gx, gy, gz)
    private double Density(int gx, int gy, int gz, int sizeX, int sizeY, int sizeZ)
        => _densityGrid![gx * sizeZ * sizeY + gz * sizeY + gy];

    /// <summary>Fills the 5 × sizeY × 5 density grid for the given world-grid origin.</summary>
    private double[] ComputeDensityGrid(double[]? buf,
        int startX, int startY, int startZ,
        int sizeX, int sizeY, int sizeZ)
    {
        int total = sizeX * sizeY * sizeZ;
        if (buf == null || buf.Length < total) buf = new double[total];
        else Array.Clear(buf, 0, total);

        // Dead shape noise (spec §5 / quirk 2): advance RNG but do NOT use output
        _bufG = _noiseA.Generate3D(_bufG, startX, startY, startZ,
            sizeX, 1, sizeZ, 1.0, 0.0, 1.0);
        _bufH = _noiseB.Generate3D(_bufH, startX, startY, startZ,
            sizeX, 1, sizeZ, 100.0, 0.0, 100.0);
        // Note: _bufG and _bufH values are deliberately NOT used in density output.

        // Blend + density noise
        _bufD = _noiseL.Generate3D(_bufD, startX, startY, startZ,
            sizeX, sizeY, sizeZ,
            684.412 / 80.0, 2053.236 / 60.0, 684.412 / 80.0);  // ~8.555, ~34.22, ~8.555
        _bufE = _noiseJ.Generate3D(_bufE, startX, startY, startZ,
            sizeX, sizeY, sizeZ,
            684.412, 2053.236, 684.412);
        _bufF = _noiseK.Generate3D(_bufF, startX, startY, startZ,
            sizeX, sizeY, sizeZ,
            684.412, 2053.236, 684.412);

        // Pre-compute Y-shape curve (spec §5)
        double[] yShape = new double[sizeY];
        for (int y = 0; y < sizeY; y++)
        {
            yShape[y] = Math.Cos(y * Math.PI * 6.0 / sizeY) * 2.0;

            int mirror = y > sizeY / 2 ? sizeY - 1 - y : y;
            if (mirror < 4)
            {
                double pull = (4 - mirror);
                yShape[y] -= pull * pull * pull * 10.0; // cubic pull-down
            }
        }

        // Fill density grid
        int xzIdx = -1;
        for (int xCell = 0; xCell < sizeX; xCell++)
        for (int zCell = 0; zCell < sizeZ; zCell++)
        {
            xzIdx++;

            // Dead code: compute var17/var21 from _bufG/_bufH to match RNG state (spec quirk 2)
            double var17 = Math.Clamp((_bufG[xzIdx] + 256.0) / 512.0, 0.0, 1.0);
            double var21 = _bufH[xzIdx] / 8000.0;
            var21 = Math.Abs(var21) * 3.0 - 3.0;
            _ = (var17, var21); // output intentionally discarded — dead code

            for (int yCell = 0; yCell < sizeY; yCell++)
            {
                int xyzIdx = xCell * sizeZ * sizeY + zCell * sizeY + yCell;

                double densityA = _bufE[xyzIdx] / 512.0;
                double densityB = _bufF[xyzIdx] / 512.0;
                double blend    = Math.Clamp((_bufD[xyzIdx] / 10.0 + 1.0) / 2.0, 0.0, 1.0);

                double density;
                if      (blend <= 0.0) density = densityA;
                else if (blend >= 1.0) density = densityB;
                else                   density = densityA + (densityB - densityA) * blend;

                density -= yShape[yCell];

                // Top fade: last 4 Y cells (spec §5)
                if (yCell > sizeY - 4)
                {
                    double t = (yCell - (sizeY - 4)) / 3.0;
                    density = density * (1.0 - t) + (-10.0) * t;
                }

                buf[xyzIdx] = density;
            }
        }

        return buf;
    }

    // ── Pass 2: Surface + bedrock — b(int, int, byte[]) (spec §6) ────────────

    private void FillSurface(int chunkX, int chunkZ, byte[] blocks)
    {
        int ceilingRef = World.WorldHeight - 64; // = 64 for height=128

        // Surface noise for this chunk
        _surfaceQ = _noiseM.Generate3D(_surfaceQ, chunkX * 16, chunkZ * 16, 0,
            16, 16, 1, 0.03125, 0.03125, 1.0);
        _surfaceR = _noiseM.Generate3D(_surfaceR, chunkX * 16, 109,        chunkZ * 16,
            16, 1,  16, 0.03125, 1.0, 0.03125);
        _surfaceS = _noiseN.Generate3D(_surfaceS, chunkX * 16, chunkZ * 16, 0,
            16, 16, 1, 0.0625, 0.0625, 0.0625);

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        {
            bool soulSandFlag = _surfaceQ[x + z * 16] + _rand.NextDouble() * 0.2 > 0.0;
            bool gravelFlag   = _surfaceR[x * 16 + z] + _rand.NextDouble() * 0.2 > 0.0;
            double depthNoise = _surfaceS[x + z * 16];
            int depth         = (int)(depthNoise / 3.0 + 3.0 + _rand.NextDouble() * 0.25);

            int countdown    = -1;
            int surfaceBlock = NetherrackId;
            int fillBlock    = NetherrackId;

            for (int y = World.WorldHeight - 1; y >= 0; y--)
            {
                int idx = BlockIndex(x, y, z);

                // Bedrock: random thickness top and bottom (spec §6)
                if (y >= World.WorldHeight - 1 - _rand.NextInt(5))
                {
                    blocks[idx] = BedrockId;
                    continue;
                }
                if (y <= _rand.NextInt(5))
                {
                    blocks[idx] = BedrockId;
                    continue;
                }

                byte current = blocks[idx];

                if (current == AirId)
                {
                    countdown = -1; // air: reset surface countdown
                }
                else if (current == NetherrackId)
                {
                    if (countdown == -1)
                    {
                        // First netherrack from top
                        if (depth <= 0)
                        {
                            surfaceBlock = AirId;
                            fillBlock    = NetherrackId;
                        }
                        else if (y >= ceilingRef - 4 && y <= ceilingRef + 1)
                        {
                            // Ceiling surface zone
                            surfaceBlock = NetherrackId;
                            fillBlock    = NetherrackId;
                            if (gravelFlag)
                            {
                                surfaceBlock = GravelId;
                                fillBlock    = NetherrackId;
                            }
                            if (soulSandFlag) // soul sand overrides gravel (spec §6)
                            {
                                surfaceBlock = SoulSandId;
                                fillBlock    = SoulSandId;
                            }
                        }

                        // Below ceiling zone with no surface block → lava (spec §6)
                        if (y < ceilingRef && surfaceBlock == AirId)
                            surfaceBlock = LavaStillId;

                        countdown = depth;

                        if (y >= ceilingRef - 1)
                            blocks[idx] = (byte)surfaceBlock;
                        else
                            blocks[idx] = (byte)fillBlock;
                    }
                    else if (countdown > 0)
                    {
                        countdown--;
                        blocks[idx] = (byte)fillBlock;
                    }
                }
            }
        }
    }

    // ── Populate step — a(ej, int, int) (spec §9) ────────────────────────────

    /// <summary>
    /// Decorates an already-generated Nether chunk with lava pools, fire, glowstone,
    /// and mushrooms. Spec: <c>jv.a(ej provider, int chunkX, int chunkZ)</c>.
    /// </summary>
    public void Populate(int chunkX, int chunkZ)
    {
        bool oldSuppress = _world.SuppressUpdates; // save cj.a equivalent
        _world.SuppressUpdates = true;

        // Fortress block placement — first in populate (spec §9 / ChunkProviderHell_Spec §9)
        _fortressGen.Generate(_world, chunkX, chunkZ, _rand);

        int x, y, z;

        // 8 lava pool attempts (spec §9)
        for (int i = 0; i < 8; i++)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight - 8) + 4;
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            WorldGenNetherLavaPool.Generate(_world, _rand, x, y, z, LavaFlowingId);
        }

        // Fire patches (variable count, spec §9)
        int fireCount = _rand.NextInt(_rand.NextInt(10) + 1) + 1;
        for (int i = 0; i < fireCount; i++)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight - 8) + 4;
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            WorldGenNetherFire.Generate(_world, _rand, x, y, z);
        }

        // Glowstone type 1 — variable count 0..(nextInt(10)) (spec §9)
        int gsCount1 = _rand.NextInt(_rand.NextInt(10) + 1);
        for (int i = 0; i < gsCount1; i++)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight - 8) + 4;
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            WorldGenGlowStone1.Generate(_world, _rand, x, y, z);
        }

        // Glowstone type 2 — always 10 (spec §9, quirk 4)
        for (int i = 0; i < 10; i++)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight);
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            WorldGenGlowStone2.Generate(_world, _rand, x, y, z);
        }

        // Brown mushroom: nextInt(1) == 0 always true (spec quirk 1)
        if (_rand.NextInt(1) == 0)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight);
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            _world.SetBlock(x, y, z, BrownMushroomId);
        }

        // Red mushroom: nextInt(1) == 0 always true (spec quirk 1)
        if (_rand.NextInt(1) == 0)
        {
            x = chunkX * 16 + _rand.NextInt(16) + 8;
            y = _rand.NextInt(World.WorldHeight);
            z = chunkZ * 16 + _rand.NextInt(16) + 8;
            _world.SetBlock(x, y, z, RedMushroomId);
        }

        _world.SuppressUpdates = oldSuppress;
    }

    // ── Block array index helper ──────────────────────────────────────────────

    // Layout matches Chunk: (localX * 16 + localZ) * WorldHeight + y
    private static int BlockIndex(int x, int y, int z)
        => (x * 16 + z) * World.WorldHeight + y;
}

// ── Nether cave carver (spec §7) ─────────────────────────────────────────────

/// <summary>
/// Nether variant of <see cref="MapGenCaves"/>.
/// Carves netherrack instead of stone. Tunnel thickness = 0.5× (spec §7).
/// Spec: <c>cz</c> (MapGenCaves Nether variant, extends <c>bz</c> MapGenBase).
/// </summary>
internal sealed class MapGenNetherCaves
{
    private const int SearchRadius   = 8;
    private const int NetherrackId   = 87;
    private const int LavaStillId    = 11;
    private const int BedrockId      = 7;

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
            GenerateFromSource(baseRng, srcX, srcZ, tgtChunkX, tgtChunkZ, blocks);
        }
    }

    private void GenerateFromSource(JavaRandom rand,
        int srcX, int srcZ, int tgtX, int tgtZ, byte[] blocks)
    {
        int count = rand.NextInt(rand.NextInt(rand.NextInt(40) + 1) + 1);
        if (rand.NextInt(15) != 0) count = 0;

        for (int cave = 0; cave < count; cave++)
        {
            float startX = srcX * 16 + rand.NextInt(16);
            float startY = rand.NextInt(rand.NextInt(World.WorldHeight - 8) + 8);
            float startZ = srcZ * 16 + rand.NextInt(16);

            int extraBranches = 1;
            if (rand.NextInt(4) == 0)
            {
                // Room (thicknessMult = 0.5 — spec §7: "Thickness multiplier: 0.5")
                CarveSegment(rand.NextLong(), tgtX, tgtZ, blocks,
                    startX, startY, startZ,
                    1.0f + rand.NextFloat() * 6.0f,
                    rand.NextFloat() * MathF.PI * 2f,
                    (rand.NextFloat() - 0.5f) * 2f / 8f,
                    -1, 0, 0.5f);
                extraBranches += rand.NextInt(4);
            }

            for (int t = 0; t < extraBranches; t++)
            {
                float yaw   = rand.NextFloat() * MathF.PI * 2f;
                float pitch = (rand.NextFloat() - 0.5f) * 2f / 8f;
                float radius = rand.NextFloat() * 2.0f + rand.NextFloat();
                if (rand.NextInt(10) == 0)
                    radius *= rand.NextFloat() * rand.NextFloat() * 3.0f + 1.0f;

                // Nether: thicknessMult = 0.5 (spec §7)
                CarveSegment(rand.NextLong(), tgtX, tgtZ, blocks,
                    startX, startY, startZ,
                    radius, yaw, pitch, 0, 0, 0.5f);
            }
        }
    }

    private static void CarveSegment(long seed, int tgtX, int tgtZ, byte[] blocks,
        float x, float y, float z,
        float radius, float yaw, float pitch,
        int startStep, int totalSteps, float thicknessMult)
    {
        float chunkCenterX = tgtX * 16 + 8;
        float chunkCenterZ = tgtZ * 16 + 8;
        float pitchSpeed = 0f, yawSpeed = 0f;
        var rand = new JavaRandom(seed);

        if (totalSteps <= 0)
        {
            int range = SearchRadius * 16 - 16;
            totalSteps = range - rand.NextInt(range / 4);
        }

        bool isMidpoint = (startStep == -1);
        if (isMidpoint) startStep = totalSteps / 2;

        int branchPoint = rand.NextInt(totalSteps / 2) + totalSteps / 4;
        bool isStraight = rand.NextInt(6) == 0;

        for (int step = startStep; step < totalSteps; step++)
        {
            float sinePhase = MathHelper.Sin(step * MathF.PI / totalSteps);
            float horDiam   = 1.5f + sinePhase * radius;
            float verDiam   = horDiam * thicknessMult;

            x += MathHelper.Cos(pitch) * MathHelper.Cos(yaw);
            y += MathHelper.Sin(pitch);
            z += MathHelper.Cos(pitch) * MathHelper.Sin(yaw);

            pitch   *= isStraight ? 0.92f : 0.70f;
            pitch   += pitchSpeed * 0.1f;
            yaw     += yawSpeed   * 0.1f;
            pitchSpeed *= 0.9f;
            yawSpeed   *= 0.75f;
            pitchSpeed += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 2.0f;
            yawSpeed   += (rand.NextFloat() - rand.NextFloat()) * rand.NextFloat() * 4.0f;

            if (!isMidpoint && step == branchPoint && radius > 1.0f)
            {
                CarveSegment(rand.NextLong(), tgtX, tgtZ, blocks, x, y, z,
                    rand.NextFloat() * 0.5f + 0.5f, yaw - MathF.PI / 2f, pitch / 3f,
                    step, totalSteps, 0.5f);
                CarveSegment(rand.NextLong(), tgtX, tgtZ, blocks, x, y, z,
                    rand.NextFloat() * 0.5f + 0.5f, yaw + MathF.PI / 2f, pitch / 3f,
                    step, totalSteps, 0.5f);
                return;
            }

            if (rand.NextInt(4) == 0) continue;

            float dx = x - chunkCenterX, dz = z - chunkCenterZ;
            float stepsLeft = totalSteps - step;
            float maxReach  = radius + 2.0f + 16.0f;
            if (dx * dx + dz * dz - stepsLeft * stepsLeft > maxReach * maxReach) return;

            if (x < chunkCenterX - 16 - horDiam * 2) continue;
            if (z < chunkCenterZ - 16 - horDiam * 2) continue;
            if (x > chunkCenterX + 16 + horDiam * 2) continue;
            if (z > chunkCenterZ + 16 + horDiam * 2) continue;

            int xMin = Math.Clamp(MathHelper.FloorDouble(x - horDiam) - tgtX * 16 - 1, 0, 16);
            int xMax = Math.Clamp(MathHelper.FloorDouble(x + horDiam) - tgtX * 16 + 1, 0, 16);
            int yMin = Math.Clamp(MathHelper.FloorDouble(y - verDiam) - 1, 1, World.WorldHeight - 8);
            int yMax = Math.Clamp(MathHelper.FloorDouble(y + verDiam) + 1, 1, World.WorldHeight - 8);
            int zMin = Math.Clamp(MathHelper.FloorDouble(z - horDiam) - tgtZ * 16 - 1, 0, 16);
            int zMax = Math.Clamp(MathHelper.FloorDouble(z + horDiam) - tgtZ * 16 + 1, 0, 16);

            for (int bx = xMin; bx < xMax; bx++)
            {
                float nx = ((bx + tgtX * 16) + 0.5f - x) / (horDiam / 2.0f);
                if (nx * nx >= 1.0f) continue;

                for (int bz = zMin; bz < zMax; bz++)
                {
                    float nz = ((bz + tgtZ * 16) + 0.5f - z) / (horDiam / 2.0f);
                    if (nx * nx + nz * nz >= 1.0f) continue;

                    for (int by = yMax - 1; by >= yMin; by--)
                    {
                        int idx = (bx * 16 + bz) * World.WorldHeight + by;
                        float ny = (by + 0.5f - y) / (verDiam / 2.0f);

                        if (ny <= -0.7f) continue;

                        if (nx * nx + ny * ny + nz * nz < 1.0f)
                        {
                            byte block = blocks[idx];
                            // Carve netherrack only — do not carve bedrock (spec §7 / quirk 6)
                            if (block == NetherrackId)
                                blocks[idx] = by < 10 ? (byte)LavaStillId : (byte)0;
                        }
                    }
                }
            }

            if (isMidpoint) break;
        }
    }
}

// ── Populate sub-generators (spec §10) ───────────────────────────────────────

/// <summary>
/// Replica of <c>ey</c> (WorldGenNetherLavaPool) — places flowing lava in netherrack pockets.
/// Requires exactly 4 netherrack neighbours and 1 air neighbour (spec §10).
/// </summary>
internal static class WorldGenNetherLavaPool
{
    public static void Generate(World world, JavaRandom rand, int x, int y, int z, int blockId)
    {
        if (world.GetBlockId(x, y + 1, z) != 87) return; // must have netherrack above
        int bx = world.GetBlockId(x, y, z);
        if (bx != 0 && bx != 87) return; // must be air or netherrack at placement spot

        int netherrackCount = 0, airCount = 0;
        int id;
        id = world.GetBlockId(x - 1, y, z); if (id == 87) netherrackCount++; else if (id == 0) airCount++;
        id = world.GetBlockId(x + 1, y, z); if (id == 87) netherrackCount++; else if (id == 0) airCount++;
        id = world.GetBlockId(x, y, z - 1); if (id == 87) netherrackCount++; else if (id == 0) airCount++;
        id = world.GetBlockId(x, y, z + 1); if (id == 87) netherrackCount++; else if (id == 0) airCount++;
        id = world.GetBlockId(x, y - 1, z); if (id == 87) netherrackCount++; else if (id == 0) airCount++;

        if (netherrackCount == 4 && airCount == 1)
        {
            // Temporarily suppress updates during lava placement (spec quirk 5)
            bool old = world.SuppressUpdates;
            world.SuppressUpdates = true;
            world.SetBlock(x, y, z, blockId);
            world.SuppressUpdates = old;
        }
    }
}

/// <summary>
/// Replica of <c>pl</c> (WorldGenNetherFire) — places fire on netherrack floor.
/// 64 attempts, ±8 XZ, ±4 Y (spec §10).
/// </summary>
internal static class WorldGenNetherFire
{
    public static void Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            int tx = x + rand.NextInt(8) - rand.NextInt(8);
            int ty = y + rand.NextInt(4) - rand.NextInt(4);
            int tz = z + rand.NextInt(8) - rand.NextInt(8);
            if (world.GetBlockId(tx, ty, tz) == 0 && world.GetBlockId(tx, ty - 1, tz) == 87)
                world.SetBlock(tx, ty, tz, 51); // fire ID 51
        }
    }
}

/// <summary>
/// Replica of <c>pt</c> (WorldGenGlowStone1) — glowstone cluster hanging from netherrack ceiling.
/// Grows downward only. 1500 attempts, ±7 XZ, -12 Y (spec §10).
/// </summary>
internal static class WorldGenGlowStone1
{
    public static void Generate(World world, JavaRandom rand, int x, int y, int z)
    {
        if (world.GetBlockId(x, y, z) != 0) return;         // must be air
        if (world.GetBlockId(x, y + 1, z) != 87) return;   // must hang from netherrack

        world.SetBlock(x, y, z, 89); // seed glowstone block

        for (int attempt = 0; attempt < 1500; attempt++)
        {
            int bx = x + rand.NextInt(8) - rand.NextInt(8);
            int by = y - rand.NextInt(12); // downward only (spec quirk 3)
            int bz = z + rand.NextInt(8) - rand.NextInt(8);

            if (world.GetBlockId(bx, by, bz) != 0) continue;

            // Place only if exactly 1 glowstone neighbour (outer growth only)
            int glowCount = 0;
            if (world.GetBlockId(bx - 1, by, bz) == 89) glowCount++;
            if (world.GetBlockId(bx + 1, by, bz) == 89) glowCount++;
            if (world.GetBlockId(bx, by - 1, bz) == 89) glowCount++;
            if (world.GetBlockId(bx, by + 1, bz) == 89) glowCount++;
            if (world.GetBlockId(bx, by, bz - 1) == 89) glowCount++;
            if (world.GetBlockId(bx, by, bz + 1) == 89) glowCount++;

            if (glowCount == 1) world.SetBlock(bx, by, bz, 89);
        }
    }
}

/// <summary>
/// Replica of <c>aew</c> (WorldGenGlowStone2) — identical algorithm to <see cref="WorldGenGlowStone1"/>.
/// Separate instance to maintain independent RNG state (spec quirk 4).
/// </summary>
internal static class WorldGenGlowStone2
{
    public static void Generate(World world, JavaRandom rand, int x, int y, int z)
        => WorldGenGlowStone1.Generate(world, rand, x, y, z); // same algorithm
}
