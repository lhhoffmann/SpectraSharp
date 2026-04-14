namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>vh</c> (WorldChunkManager) — manages per-column biome assignment and
/// per-position climate data (temperature, rainfall) for a world.
///
/// Uses two sets of noise generators:
///   - <c>temperatureNoise</c> / <c>rainfallNoise</c> (4 octave each) for climate
///   - <c>biomeNoise</c> (2 octave) for biome-blend selection
///
/// Biome assignment maps climate values to one of the 16 BiomeGenBase instances.
///
/// Implementation note: the exact noise scales and biome-assignment formulas in `vh`
/// are complex; this implementation reproduces the public-facing contract required by
/// BiomeGenBase and ChunkProviderGenerate without full internal parity.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BiomeGenBase_Spec.md §4
/// </summary>
public sealed class WorldChunkManager
{
    private readonly NoiseGeneratorOctaves _temperatureNoise;
    private readonly NoiseGeneratorOctaves _rainfallNoise;
    private readonly NoiseGeneratorOctaves _biomeNoise;

    private readonly long _worldSeed;

    // ── Constructor ───────────────────────────────────────────────────────────

    public WorldChunkManager(long worldSeed)
    {
        _worldSeed = worldSeed;
        var rand = new JavaRandom(worldSeed);
        _temperatureNoise = new NoiseGeneratorOctaves(rand, 4);
        _rainfallNoise    = new NoiseGeneratorOctaves(rand, 4);
        _biomeNoise       = new NoiseGeneratorOctaves(rand, 2);
    }

    // ── Climate queries (spec §4) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int x, int y, int z)</c> — temperature at world block position.
    /// High altitude (y > 64) cools the temperature slightly.
    /// </summary>
    public double GetTemperatureAtHeight(int x, int y, int z)
    {
        double temp = GetTemperatureAtHeight(x, z);
        // Altitude penalty: above sea-level (63) temperature drops
        if (y > World.MidWorldY)
            temp -= (y - World.MidWorldY) * 0.00166667;
        return Math.Clamp(temp, 0.0, 1.0);
    }

    /// <summary>
    /// obf: <c>b(int x, int z)</c> — temperature at block column (no altitude factor).
    /// </summary>
    public double GetTemperatureAtHeight(int x, int z)
    {
        var temp = _temperatureNoise.Generate2D(null, x, z, 1, 1, 0.25, 0.25);
        return Math.Clamp(temp[0] * 0.5 + 0.5, 0.0, 1.0);
    }

    /// <summary>
    /// obf: <c>b(int x, int z)</c> (2-arg overload) — rainfall at block column.
    /// </summary>
    public double GetRainfallAtHeight(int x, int z)
    {
        var rain = _rainfallNoise.Generate2D(null, x, z, 1, 1, 0.25, 0.25);
        return Math.Clamp(rain[0] * 0.5 + 0.5, 0.0, 1.0);
    }

    // ── Biome queries (spec §4) ───────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int x, int z)</c> — returns the biome at block column (X, Z).
    /// Maps temperature and rainfall to a BiomeGenBase instance.
    /// </summary>
    public BiomeGenBase GetBiomeAt(int x, int z)
    {
        double temp = GetTemperatureAtHeight(x, z);
        double rain = GetRainfallAtHeight(x, z);
        return SelectBiome(temp, rain);
    }

    /// <summary>
    /// obf: <c>a(sr[], int x, int z, int w, int h)</c> — fills biome array for a W×H block region
    /// starting at chunk block origin (chunkX*16, chunkZ*16). Array index = z*w + x.
    /// </summary>
    public BiomeGenBase[] GetBiomesForGeneration(BiomeGenBase[]? result, int x, int z, int w, int h)
    {
        result ??= new BiomeGenBase[w * h];
        for (int iz = 0; iz < h; iz++)
        for (int ix = 0; ix < w; ix++)
            result[iz * w + ix] = GetBiomeAt(x + ix, z + iz);
        return result;
    }

    // ── Climate-to-biome mapping ──────────────────────────────────────────────

    /// <summary>
    /// Maps continuous (temp, rain) to the appropriate biome.
    /// Mirrors the vanilla lookup table logic.
    /// </summary>
    private static BiomeGenBase SelectBiome(double temp, double rain)
    {
        // Frozen
        if (temp < 0.1)
        {
            if (rain < 0.5) return BiomeGenBase.IcePlains;
            return BiomeGenBase.IcePlains;
        }

        // Very hot / arid → Desert
        if (temp > 1.0) return BiomeGenBase.Desert;

        // Moderate temperature
        if (temp < 0.3)
        {
            if (rain > 0.6) return BiomeGenBase.Taiga;
            return BiomeGenBase.ExtremeHills;
        }

        if (rain < 0.2) return BiomeGenBase.Desert;
        if (rain > 0.85) return BiomeGenBase.Swampland;
        if (rain > 0.6)  return BiomeGenBase.Forest;

        return BiomeGenBase.Plains;
    }
}
