namespace SpectraSharp.Core;

/// <summary>
/// Bit-exact replica of the original <c>me</c> (MathHelper) static utility class.
///
/// Provides a single-precision sine/cosine lookup table (65536 entries, built once at
/// static-init time) plus numeric helpers used throughout physics, entity movement,
/// and world generation.
///
/// All quirks and overflow behaviours documented in the spec are intentionally preserved.
/// Source spec: Documentation/VoxelCore/Parity/Specs/MathHelper_Spec.md
/// </summary>
public static class MathHelper
{
    // ── Sine table ────────────────────────────────────────────────────────────
    // 65536 single-precision entries covering one full period [0, 2π).
    private static readonly float[] SinTable = new float[65536];

    /// Public accessor for the sine table. Used by tests.
    public static float[] SineTable => SinTable;

    static MathHelper()
    {
        // Spec §4: double arithmetic throughout; cast to float at the end.
        // Math.PI is the C# double equivalent of java.lang.Math.PI.
        for (int i = 0; i < 65536; i++)
        {
            double angle = (double)i * Math.PI * 2.0 / 65536.0;
            SinTable[i] = (float)Math.Sin(angle);
        }
    }

    // ── Trig ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 sin — <c>a(float)</c><br/>
    /// Lookup sine via the 65536-entry table.
    /// All arithmetic is single-precision, index wraps via <c>&amp; 65535</c>.
    /// </summary>
    public static float Sin(float angle)
    {
        // 1. float multiply
        // 2. truncate toward zero (C# explicit int cast matches Java (int) cast)
        // 3. mask low 16 bits → wraps negative and large indices
        // 4. table lookup
        return SinTable[(int)(angle * 10430.378F) & 65535];
    }

    /// <summary>
    /// Spec §5 cos — <c>b(float)</c><br/>
    /// Phase-shifted sine lookup: +16384 (quarter period) added in float before cast.
    /// The float addition happens before truncation — ordering is part of the spec.
    /// </summary>
    public static float Cos(float angle)
    {
        return SinTable[(int)(angle * 10430.378F + 16384.0F) & 65535];
    }

    // ── Square root ───────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 sqrt_float — <c>c(float)</c><br/>
    /// Widens to double, calls Math.Sqrt, narrows back to float.
    /// </summary>
    public static float SqrtFloat(float value)
    {
        return (float)Math.Sqrt(value);
    }

    /// <summary>
    /// Spec §5 sqrt_double — <c>a(double)</c><br/>
    /// Double-precision sqrt, result narrowed to float.
    /// </summary>
    public static float SqrtDouble(double value)
    {
        return (float)Math.Sqrt(value);
    }

    // ── Floor ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 floor_float — <c>d(float)</c><br/>
    /// Correct mathematical floor for all finite float inputs.
    /// Truncates then subtracts 1 for negative non-integers.
    /// </summary>
    public static int FloorFloat(float value)
    {
        int truncated = (int)value;
        return value < (float)truncated ? truncated - 1 : truncated;
    }

    /// <summary>
    /// Spec §5 floor_double_fast — <c>b(double)</c><br/>
    /// FAST floor using a +1024 bias trick. ONLY CORRECT for <c>value ≥ -1024.0</c>.
    /// <para>
    /// <b>Known bug preserved:</b> for inputs below -1024.0 the bias is insufficient;
    /// the result is off by one. Callers rely on speed within world-coordinate bounds.
    /// </para>
    /// </summary>
    public static int FloorDoubleFast(double value)
    {
        return (int)(value + 1024.0) - 1024;
    }

    /// <summary>
    /// Spec §5 floor_double — <c>c(double)</c><br/>
    /// Correct mathematical floor for all finite double inputs within int range.
    /// </summary>
    public static int FloorDouble(double value)
    {
        int truncated = (int)value;
        return value < (double)truncated ? truncated - 1 : truncated;
    }

    /// <summary>
    /// Spec §5 floor_long — <c>d(double)</c><br/>
    /// Mathematical floor returning long. Used for world-seed and chunk coordinates.
    /// </summary>
    public static long FloorLong(double value)
    {
        long truncated = (long)value;
        return value < (double)truncated ? truncated - 1L : truncated;
    }

    // ── Absolute value ────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 abs_float — <c>e(float)</c><br/>
    /// Manual abs; -0.0F passes through unchanged (spec §5, IEEE 754 consistent).
    /// </summary>
    public static float AbsFloat(float value)
    {
        return value >= 0.0F ? value : -value;
    }

    /// <summary>
    /// Spec §5 abs_int — <c>a(int)</c><br/>
    /// <b>Known bug preserved:</b> <c>AbsInt(int.MinValue)</c> returns
    /// <c>int.MinValue</c> (two's-complement overflow). Do not add a guard.
    /// </summary>
    public static int AbsInt(int value)
    {
        return value >= 0 ? value : -value;
    }

    // ── Clamp ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 clamp_int — <c>a(int, int, int)</c><br/>
    /// Clamps <paramref name="value"/> to [<paramref name="min"/>, <paramref name="max"/>].
    /// Undefined if <paramref name="min"/> &gt; <paramref name="max"/> (no guard in original).
    /// </summary>
    public static int ClampInt(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    // ── Misc ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec §5 abs_max — <c>a(double, double)</c><br/>
    /// Returns the larger of the two absolute values.
    /// Manual negation (not Math.Abs) — consistent with spec.
    /// </summary>
    public static double AbsMax(double a, double b)
    {
        if (a < 0.0) a = -a;
        if (b < 0.0) b = -b;
        return a > b ? a : b;
    }

    /// <summary>
    /// Spec §5 bucketInt — <c>a(int, int)</c><br/>
    /// Integer floor division: always rounds toward negative infinity.
    /// <paramref name="divisor"/> must be &gt; 0 (no guard in original).
    /// </summary>
    public static int BucketInt(int dividend, int divisor)
    {
        if (dividend < 0)
            return -((-dividend - 1) / divisor) - 1;

        return dividend / divisor;
    }

    /// <summary>
    /// Spec §5 isNullOrEmpty — <c>a(String)</c><br/>
    /// Returns true if the string is null or has length 0.
    /// </summary>
    public static bool IsNullOrEmpty(string? value)
    {
        return value is null || value.Length == 0;
    }

    /// <summary>
    /// Spec §5 getRandomIntegerInRange — <c>a(Random, int, int)</c><br/>
    /// Returns a random integer in [<paramref name="min"/>, <paramref name="max"/>] inclusive.
    /// Degenerate case (<paramref name="min"/> ≥ <paramref name="max"/>) returns <paramref name="min"/>.
    /// </summary>
    public static int GetRandomIntegerInRange(JavaRandom rng, int min, int max)
    {
        if (min >= max) return min;
        return rng.NextInt(max - min + 1) + min;
    }

    /// <summary>
    /// Overload that accepts a <c>Func&lt;int, int&gt;</c> delegate (e.g. a method group from test fakes).
    /// </summary>
    public static int GetRandomIntegerInRange(Func<int, int> nextInt, int min, int max)
    {
        if (min >= max) return min;
        return nextInt(max - min + 1) + min;
    }
}
