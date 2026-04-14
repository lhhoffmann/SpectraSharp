namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>eb</c> (NoiseGeneratorOctaves) — N-octave fractional Brownian motion Perlin noise.
///
/// Each octave is an independent <see cref="PerlinNoiseGenerator"/> seeded from the same
/// <see cref="JavaRandom"/> sequence (so all generators share the same seed chain as long as
/// the calling order matches the original). Each successive octave samples at double the
/// frequency and half the amplitude.
///
/// The <see cref="Generate3D"/> / <see cref="Generate2D"/> methods ACCUMULATE into the
/// supplied array if one is provided (pass null to allocate a fresh zeroed array).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderGenerate_Spec.md §11
/// </summary>
public sealed class NoiseGeneratorOctaves
{
    private readonly PerlinNoiseGenerator[] _generators;

    // ── Constructor (spec §11) ────────────────────────────────────────────────

    /// <param name="rand">Seeded random — each octave consumes several calls from it.</param>
    /// <param name="octaves">Number of octaves (e.g. 16, 8, 4, 10).</param>
    public NoiseGeneratorOctaves(JavaRandom rand, int octaves)
    {
        _generators = new PerlinNoiseGenerator[octaves];
        for (int i = 0; i < octaves; i++)
            _generators[i] = new PerlinNoiseGenerator(rand);
    }

    // ── 3D generation (spec §11) ──────────────────────────────────────────────

    /// <summary>
    /// Fills <paramref name="result"/> (allocates if null) with summed octave noise
    /// for a 3D grid of <c>sizeX × sizeY × sizeZ</c> samples starting at world
    /// coordinate (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>).
    ///
    /// Index layout: result[(ix * sizeZ + iz) * sizeY + iy].
    /// </summary>
    public double[] Generate3D(double[]? result,
                                double x, double y, double z,
                                int sizeX, int sizeY, int sizeZ,
                                double scaleX, double scaleY, double scaleZ)
    {
        result ??= new double[sizeX * sizeY * sizeZ];

        double amplitude = 1.0;
        double frequency = 1.0;
        for (int i = 0; i < _generators.Length; i++)
        {
            _generators[i].Fill(result,
                x, y, z,
                sizeX, sizeY, sizeZ,
                scaleX * frequency, scaleY * frequency, scaleZ * frequency,
                amplitude);
            frequency *= 2.0;
            amplitude /= 2.0;
        }
        return result;
    }

    // ── 2D generation (sizeY = 1) ─────────────────────────────────────────────

    /// <summary>
    /// Fills <paramref name="result"/> with 2D noise (Y is sampled at a fixed 0 position).
    /// Index layout: result[ix * sizeZ + iz].
    /// </summary>
    public double[] Generate2D(double[]? result,
                                double x, double z,
                                int sizeX, int sizeZ,
                                double scaleX, double scaleZ)
    {
        return Generate3D(result, x, 10.0, z, sizeX, 1, sizeZ, scaleX, 1.0, scaleZ);
    }
}
