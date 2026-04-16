namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>agk</c> (PerlinNoiseGenerator) — single-octave classic Perlin noise.
///
/// Uses a 512-entry permutation table seeded from a <see cref="JavaRandom"/>.
/// Each instance has independent offsets (offsetX/Y/Z) so octaves produced from the
/// same seed sequence land at different positions in noise space.
///
/// The <see cref="Fill"/> method accumulates into an existing array (does not zero it first)
/// so that <see cref="NoiseGeneratorOctaves"/> can sum multiple octaves.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderGenerate_Spec.md §11
/// </summary>
public sealed class PerlinNoiseGenerator
{
    private readonly int[]   _p = new int[512];   // permutation table
    private readonly double  _offsetX;
    private readonly double  _offsetY;
    private readonly double  _offsetZ;

    // ── Constructor (spec §11) ────────────────────────────────────────────────

    public PerlinNoiseGenerator(JavaRandom rand)
    {
        _offsetX = rand.NextDouble() * 256.0;
        _offsetY = rand.NextDouble() * 256.0;
        _offsetZ = rand.NextDouble() * 256.0;

        // Fill identity, then Fisher-Yates shuffle
        for (int i = 0; i < 256; i++) _p[i] = i;
        for (int i = 0; i < 256; i++)
        {
            int j   = rand.NextInt(256 - i) + i;
            int tmp = _p[i]; _p[i] = _p[j]; _p[j] = tmp;
        }
        // Duplicate for wrap-around access p[256..511]
        for (int i = 0; i < 256; i++) _p[i + 256] = _p[i];
    }

    // ── Fill (spec §11) ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds noise values into <paramref name="result"/> (accumulating).
    /// Loop order: for x, for z, for y → index = (x * sizeZ + z) * sizeY + y.
    /// </summary>
    public void Fill(double[] result,
                     double baseX, double baseY, double baseZ,
                     int sizeX, int sizeY, int sizeZ,
                     double scaleX, double scaleY, double scaleZ,
                     double amplitude)
    {
        int idx = 0;
        for (int ix = 0; ix < sizeX; ix++)
        {
            double dx = (baseX + ix) * scaleX + _offsetX;
            for (int iz = 0; iz < sizeZ; iz++)
            {
                double dz = (baseZ + iz) * scaleZ + _offsetZ;
                for (int iy = 0; iy < sizeY; iy++)
                {
                    double dy = (baseY + iy) * scaleY + _offsetY;
                    result[idx++] += Noise3D(dx, dy, dz) * amplitude;
                }
            }
        }
    }

    // ── Core Perlin (spec §11) ────────────────────────────────────────────────

    private double Noise3D(double x, double y, double z)
    {
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;

        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);

        double u = Fade(x);
        double v = Fade(y);
        double w = Fade(z);

        int A  = _p[X]     + Y; int AA = _p[A]     + Z; int AB = _p[A + 1] + Z;
        int B  = _p[X + 1] + Y; int BA = _p[B]     + Z; int BB = _p[B + 1] + Z;

        return Lerp(w,
            Lerp(v,
                Lerp(u, Grad(_p[AA],     x,     y,     z),
                        Grad(_p[BA],     x - 1, y,     z)),
                Lerp(u, Grad(_p[AB],     x,     y - 1, z),
                        Grad(_p[BB],     x - 1, y - 1, z))),
            Lerp(v,
                Lerp(u, Grad(_p[AA + 1], x,     y,     z - 1),
                        Grad(_p[BA + 1], x - 1, y,     z - 1)),
                Lerp(u, Grad(_p[AB + 1], x,     y - 1, z - 1),
                        Grad(_p[BB + 1], x - 1, y - 1, z - 1))));
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6.0 - 15.0) + 10.0);

    private static double Lerp(double t, double a, double b) => a + t * (b - a);

    private static double Grad(int hash, double x, double y, double z)
    {
        int    h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
