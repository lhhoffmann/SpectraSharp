namespace SpectraEngine.Core;

/// <summary>
/// Bit-exact replica of <c>java.util.Random</c> (Java SE specification).
///
/// Algorithm: 48-bit linear congruential generator.
///   seed_n = (seed_{n-1} × 0x5DEECE66D + 0xB) mod 2^48
///   next(bits) = top <paramref name="bits"/> bits of seed_n
///
/// Initialisation scramble: stored_seed = (seed ^ multiplier) mod 2^48
/// This matches Java's constructor behaviour exactly.
///
/// Source: Java SE API specification (public, algorithm is normative).
/// This implementation was written from scratch; no bytecode was consulted.
/// </summary>
public sealed class JavaRandom
{
    // ── LCG constants (from Java SE specification) ────────────────────────────
    private const long Multiplier = 0x5DEECE66DL;
    private const long Addend     = 0xBL;
    private const long Mask       = (1L << 48) - 1;   // 2^48 − 1

    private long _seed;

    // ── Constructors ──────────────────────────────────────────────────────────

    public JavaRandom() : this(Environment.TickCount64) { }

    public JavaRandom(long seed)
    {
        SetSeed(seed);
    }

    // ── Seed ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reseeds the generator exactly as <c>java.util.Random.setSeed</c> does.
    /// Stored seed = (seed XOR multiplier) AND mask.
    /// </summary>
    public void SetSeed(long seed) => _seed = (seed ^ Multiplier) & Mask;

    // ── Core generator ────────────────────────────────────────────────────────

    /// <summary>
    /// Advances the seed one step and returns the top <paramref name="bits"/> bits.
    /// Equivalent to <c>java.util.Random.next(int bits)</c>.
    /// </summary>
    private int Next(int bits)
    {
        _seed = (_seed * Multiplier + Addend) & Mask;
        return (int)((long)((ulong)_seed >> (48 - bits)));
    }

    // ── Public API — mirrors java.util.Random exactly ─────────────────────────

    /// <returns>Uniformly distributed int over all 2^32 values.</returns>
    public int NextInt() => Next(32);

    /// <summary>
    /// Returns a value in [0, <paramref name="bound"/>).
    /// Matches Java's power-of-two fast path and rejection-sampling loop exactly.
    /// </summary>
    public int NextInt(int bound)
    {
        if (bound <= 0)
            throw new ArgumentOutOfRangeException(nameof(bound), "bound must be positive");

        // Fast path: bound is a power of two
        if ((bound & -bound) == bound)
            return (int)((bound * (long)Next(31)) >> 31);

        // Rejection sampling — identical loop to Java spec
        int bits, val;
        do
        {
            bits = Next(31);
            val  = bits % bound;
        }
        while (bits - val + (bound - 1) < 0);

        return val;
    }

    /// <returns>Uniformly distributed long over all 2^64 values.</returns>
    public long NextLong() => ((long)Next(32) << 32) + Next(32);

    /// <returns><c>true</c> with probability 0.5.</returns>
    public bool NextBoolean() => Next(1) != 0;

    /// <returns>Float in [0.0f, 1.0f).</returns>
    public float NextFloat() => Next(24) / (float)(1 << 24);

    /// <returns>Double in [0.0, 1.0).</returns>
    public double NextDouble() => (((long)Next(26) << 27) + Next(27)) / (double)(1L << 53);

    /// <summary>
    /// Gaussian sample with mean 0 and standard deviation 1.
    /// Uses Box-Muller transform — identical to Java's implementation.
    /// </summary>
    public double NextGaussian()
    {
        // Java caches the spare value; we replicate that behaviour.
        if (_haveNextNextGaussian)
        {
            _haveNextNextGaussian = false;
            return _nextNextGaussian;
        }

        double v1, v2, s;
        do
        {
            v1 = 2 * NextDouble() - 1;
            v2 = 2 * NextDouble() - 1;
            s  = v1 * v1 + v2 * v2;
        }
        while (s >= 1 || s == 0);

        double multiplier = Math.Sqrt(-2 * Math.Log(s) / s);
        _nextNextGaussian      = v2 * multiplier;
        _haveNextNextGaussian  = true;
        return v1 * multiplier;
    }

    private double _nextNextGaussian;
    private bool   _haveNextNextGaussian;
}
