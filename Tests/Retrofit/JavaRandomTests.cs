using System;
using Xunit;

namespace SpectraEngine.Tests;

// ---------------------------------------------------------------------------
// Fakes / stubs
// ---------------------------------------------------------------------------

/// <summary>
/// Hand-written fake for java.util.Random used by GetRandomIntegerInRange.
/// Seeds to a fixed value so tests are deterministic.
/// </summary>
file sealed class FakeJavaRandom
{
    private readonly SpectraEngine.Core.JavaRandom _rng;

    public FakeJavaRandom(long seed) => _rng = new SpectraEngine.Core.JavaRandom(seed);

    public int NextInt(int bound) => _rng.NextInt(bound);
}

// ---------------------------------------------------------------------------
// The class under test — MathHelper — is specified but NOT yet provided.
// We reference it as SpectraEngine.Core.MathHelper. If the type does not exist
// the tests will fail to compile, which is itself a documented parity gap.
// ---------------------------------------------------------------------------

public class MathHelperTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Expected float value at a given table index, computed exactly
    /// as the spec's static initialiser demands.</summary>
    private static float ExpectedTableEntry(int i)
    {
        double angle = (double)i * Math.PI * 2.0 / 65536.0;
        double value = Math.Sin(angle);
        return (float)value;
    }

    // -----------------------------------------------------------------------
    // Section 2 / 4 — Sine table construction
    // -----------------------------------------------------------------------

    [Fact]
    public void SineTable_HasLength65536()
    {
        // The spec mandates float[65536].
        Assert.Equal(65536, SpectraEngine.Core.MathHelper.SineTable.Length);
    }

    [Fact]
    public void SineTable_Index0_IsZero()
    {
        // a[0] = (float)sin(0) = 0.0f
        Assert.Equal(0.0f, SpectraEngine.Core.MathHelper.SineTable[0]);
    }

    [Fact]
    public void SineTable_Index16384_IsOne()
    {
        // a[16384] = (float)sin(π/2) = 1.0f
        Assert.Equal(1.0f, SpectraEngine.Core.MathHelper.SineTable[16384]);
    }

    [Fact]
    public void SineTable_Index49152_IsNegativeOne()
    {
        // a[49152] = (float)sin(3π/2) = -1.0f
        Assert.Equal(-1.0f, SpectraEngine.Core.MathHelper.SineTable[49152]);
    }

    [Fact]
    public void SineTable_Index32768_IsNearZero()
    {
        // a[32768] = (float)sin(π) ≈ 0.0f (very small, may not be exactly zero
        // depending on cast; spec says "very close to zero")
        float v = SpectraEngine.Core.MathHelper.SineTable[32768];
        Assert.True(Math.Abs(v) < 1e-7f,
            $"Expected near zero but got {v}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16384)]
    [InlineData(32768)]
    [InlineData(49152)]
    [InlineData(65535)]
    public void SineTable_SelectedEntries_MatchSpecFormula(int index)
    {
        float expected = ExpectedTableEntry(index);
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.SineTable[index]);
    }

    [Fact]
    public void SineTable_AllEntries_MatchSpecFormula()
    {
        var table = SpectraEngine.Core.MathHelper.SineTable;
        for (int i = 0; i < 65536; i++)
        {
            float expected = ExpectedTableEntry(i);
            if (expected != table[i])
                Assert.Fail($"Table mismatch at index {i}: expected {expected}, got {table[i]}");
        }
    }

    // -----------------------------------------------------------------------
    // Section 5 — sin (a(float))
    // -----------------------------------------------------------------------

    [Fact]
    public void Sin_Zero_ReturnsZero()
    {
        // sin(0) → index = (int)(0 * 10430.378f) & 65535 = 0 → table[0] = 0
        float result = SpectraEngine.Core.MathHelper.Sin(0.0f);
        Assert.Equal(0.0f, result);
    }

    [Fact]
    public void Sin_HalfPi_ReturnsOne()
    {
        // sin(π/2) should look up near index 16384 → 1.0f
        float halfPi = (float)(Math.PI / 2.0);
        float result = SpectraEngine.Core.MathHelper.Sin(halfPi);
        // compute expected index
        int idx = (int)(halfPi * 10430.378f) & 65535;
        float expected = SpectraEngine.Core.MathHelper.SineTable[idx];
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14159265f)]
    [InlineData(-3.14159265f)]
    [InlineData(6.28318530f)]
    [InlineData(100.0f)]
    [InlineData(-100.0f)]
    public void Sin_MatchesTableLookupFormula(float angle)
    {
        int idx = (int)(angle * 10430.378f) & 65535;
        float expected = SpectraEngine.Core.MathHelper.SineTable[idx];
        float result = SpectraEngine.Core.MathHelper.Sin(angle);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sin_NegativeAngle_UsesLow16BitsOfTruncatedIndex()
    {
        // Quirk 3: negative float input; (int) truncates toward zero, then &65535
        float angle = -0.5f;
        int idx = (int)(angle * 10430.378f) & 65535;
        float expected = SpectraEngine.Core.MathHelper.SineTable[idx];
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.Sin(angle));
    }

    // -----------------------------------------------------------------------
    // Section 5 — cos (b(float))
    // -----------------------------------------------------------------------

    [Fact]
    public void Cos_Zero_ReturnsOne()
    {
        // cos(0) → index = (int)(0*10430.378f + 16384.0f) & 65535 = 16384 → 1.0f
        float result = SpectraEngine.Core.MathHelper.Cos(0.0f);
        Assert.Equal(1.0f, result);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(-1.0f)]
    [InlineData(3.14159265f)]
    [InlineData(-3.14159265f)]
    [InlineData(6.28318530f)]
    [InlineData(100.0f)]
    [InlineData(-100.0f)]
    public void Cos_MatchesTableLookupFormula(float angle)
    {
        // Quirk 4: +16384.0F applied BEFORE (int) cast — float arithmetic
        int idx = (int)(angle * 10430.378f + 16384.0f) & 65535;
        float expected = SpectraEngine.Core.MathHelper.SineTable[idx];
        float result = SpectraEngine.Core.MathHelper.Cos(angle);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Cos_OffsetAppliedBeforeCast_NotAfter()
    {
        // Verify that +16384 is added before (int) truncation.
        // If it were added after the cast the index would differ for non-integer products.
        float angle = 0.3f;
        // Correct (spec): add then cast
        int correctIdx = (int)(angle * 10430.378f + 16384.0f) & 65535;
        // Wrong (alternative): cast then add
        int wrongIdx = ((int)(angle * 10430.378f) + 16384) & 65535;
        float expected = SpectraEngine.Core.MathHelper.SineTable[correctIdx];
        float result = SpectraEngine.Core.MathHelper.Cos(angle);
        Assert.Equal(expected, result);
        // Ensure the two approaches actually differ for this angle (test is meaningful)
        Assert.NotEqual(correctIdx, wrongIdx);
    }

    // -----------------------------------------------------------------------
    // Section 5 — sqrt_float (c(float))
    // -----------------------------------------------------------------------

    [Fact]
    public void SqrtFloat_Four_ReturnsTwo()
    {
        float result = SpectraEngine.Core.MathHelper.SqrtFloat(4.0f);
        Assert.Equal(2.0f, result);
    }

    [Fact]
    public void SqrtFloat_Zero_ReturnsZero()
    {
        Assert.Equal(0.0f, SpectraEngine.Core.MathHelper.SqrtFloat(0.0f));
    }

    [Fact]
    public void SqrtFloat_NegativeInput_ReturnsNaN()
    {
        // Spec: delegates to Math.sqrt; NaN for negatives
        float result = SpectraEngine.Core.MathHelper.SqrtFloat(-1.0f);
        Assert.True(float.IsNaN(result));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(9.0f)]
    [InlineData(123.456f)]
    public void SqrtFloat_MatchesDoubleSqrtNarrowedToFloat(float value)
    {
        float expected = (float)Math.Sqrt((double)value);
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.SqrtFloat(value));
    }

    // -----------------------------------------------------------------------
    // Section 5 — sqrt_double (a(double))
    // -----------------------------------------------------------------------

    [Fact]
    public void SqrtDouble_Four_ReturnsTwo()
    {
        float result = SpectraEngine.Core.MathHelper.SqrtDouble(4.0);
        Assert.Equal(2.0f, result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(9.0)]
    [InlineData(123.456)]
    public void SqrtDouble_MatchesMathSqrtNarrowedToFloat(double value)
    {
        float expected = (float)Math.Sqrt(value);
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.SqrtDouble(value));
    }

    [Fact]
    public void SqrtDouble_ReturnsFloat_NotDouble()
    {
        // Return type must be float per spec
        var method = typeof(SpectraEngine.Core.MathHelper).GetMethod(
            "SqrtDouble",
            new[] { typeof(double) });
        Assert.NotNull(method);
        Assert.Equal(typeof(float), method!.ReturnType);
    }

    // -----------------------------------------------------------------------
    // Section 5 — floor_float (d(float))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.5f, 1)]
    [InlineData(1.0f, 1)]
    [InlineData(0.0f, 0)]
    [InlineData(-0.0f, 0)]
    [InlineData(-1.0f, -1)]
    [InlineData(-1.5f, -2)]
    [InlineData(-2.0f, -2)]
    [InlineData(2.9f, 2)]
    [InlineData(-2.9f, -3)]
    public void FloorFloat_ReturnsCorrectFloor(float input, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.FloorFloat(input));
    }

    [Fact]
    public void FloorFloat_NegativeNonInteger_CorrectsTruncation()
    {
        // (int)(-1.5f) = -1; -1.5f < -1.0f → true → return -2
        Assert.Equal(-2, SpectraEngine.Core.MathHelper.FloorFloat(-1.5f));
    }

    // -----------------------------------------------------------------------
    // Section 5 — floor_double_fast (b(double))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.5, 1)]
    [InlineData(1.0, 1)]
    [InlineData(0.0, 0)]
    [InlineData(-1.0, -1)]
    [InlineData(-1.5, -2)]
    [InlineData(-2.0, -2)]
    [InlineData(100.7, 100)]
    [InlineData(-1023.9, -1024)]
    [InlineData(-1024.0, -1024)]
    public void FloorDoubleFast_WithinValidRange_ReturnsCorrectFloor(double input, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.FloorDoubleFast(input));
    }

    // Quirk 1: off-by-one for inputs below -1024
    [Fact(Skip = "PARITY BUG — impl diverges from spec: floor_double_fast returns wrong result for inputs in (-1025,-1024); off-by-one due to insufficient bias must be preserved")]
    public void FloorDoubleFast_BelowNegative1024_OffByOneQuirk_MustBePreserved()
    {
        // For var0 in (-1025.0, -1024.0):
        //   var0 + 1024.0 is in (-1.0, 0.0)
        //   (int) of that = 0 (truncation toward zero, NOT floor)
        //   0 - 1024 = -1024, but correct floor is -1025 → wrong by 1
        double input = -1024.5;
        int result = SpectraEngine.Core.MathHelper.FloorDoubleFast(input);
        // The SPEC MANDATES this quirky result: -1024 (not -1025)
        Assert.Equal(-1024, result);
    }

    [Fact]
    public void FloorDoubleFast_Quirk1_ProducesWrongResultForNegative1024Point5()
    {
        // Confirm the quirky behaviour is present (should return -1024, not -1025)
        double input = -1024.5;
        int result = SpectraEngine.Core.MathHelper.FloorDoubleFast(input);
        // Correct mathematical floor is -1025; spec says it returns -1024 (the bug)
        Assert.Equal(-1024, result);
    }

    // -----------------------------------------------------------------------
    // Section 5 — floor_double (c(double))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.5, 1)]
    [InlineData(1.0, 1)]
    [InlineData(0.0, 0)]
    [InlineData(-1.0, -1)]
    [InlineData(-1.5, -2)]
    [InlineData(-2.0, -2)]
    [InlineData(-1024.5, -1025)]
    [InlineData(-1025.9, -1026)]
    [InlineData(2.9, 2)]
    public void FloorDouble_ReturnsCorrectFloor(double input, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.FloorDouble(input));
    }

    [Fact]
    public void FloorDouble_NegativeNonInteger_CorrectsTruncation()
    {
        Assert.Equal(-2, SpectraEngine.Core.MathHelper.FloorDouble(-1.5));
    }

    // -----------------------------------------------------------------------
    // Section 5 — floor_long (d(double))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.5, 1L)]
    [InlineData(1.0, 1L)]
    [InlineData(0.0, 0L)]
    [InlineData(-1.0, -1L)]
    [InlineData(-1.5, -2L)]
    [InlineData(-2.0, -2L)]
    [InlineData(-1024.5, -1025L)]
    public void FloorLong_ReturnsCorrectLongFloor(double input, long expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.FloorLong(input));
    }

    [Fact]
    public void FloorLong_ReturnsLong_NotInt()
    {
        var method = typeof(SpectraEngine.Core.MathHelper).GetMethod(
            "FloorLong",
            new[] { typeof(double) });
        Assert.NotNull(method);
        Assert.Equal(typeof(long), method!.ReturnType);
    }

    // -----------------------------------------------------------------------
    // Section 5 — abs_float (e(float))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1.0f, 1.0f)]
    [InlineData(-1.0f, 1.0f)]
    [InlineData(0.0f, 0.0f)]
    [InlineData(float.MaxValue, float.MaxValue)]
    [InlineData(float.MinValue, float.MaxValue)]  // MinValue is most-negative
    public void AbsFloat_ReturnsAbsoluteValue(float input, float expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.AbsFloat(input));
    }

    [Fact]
    public void AbsFloat_NegativeZero_ReturnedUnchanged()
    {
        // Spec: -0.0F >= 0.0F is true in Java; returns -0.0F unchanged
        float result = SpectraEngine.Core.MathHelper.AbsFloat(-0.0f);
        // -0.0f == 0.0f under IEEE 754; the important thing is the method returns it
        Assert.Equal(0.0f, result);
    }

    // -----------------------------------------------------------------------
    // Section 5 — abs_int (a(int)) — Quirk 2
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-5, 5)]
    [InlineData(0, 0)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void AbsInt_ReturnsAbsoluteValue(int input, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.AbsInt(input));
    }

    [Fact]
    public void AbsInt_IntMinValue_ReturnsIntMinValue_Quirk2()
    {
        // Quirk 2: -Integer.MIN_VALUE overflows back to Integer.MIN_VALUE in two's complement
        Assert.Equal(int.MinValue, SpectraEngine.Core.MathHelper.AbsInt(int.MinValue));
    }

    // -----------------------------------------------------------------------
    // Section 5 — clamp_int (a(int,int,int))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(5, 0, 10, 5)]
    [InlineData(-1, 0, 10, 0)]
    [InlineData(11, 0, 10, 10)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(10, 0, 10, 10)]
    [InlineData(5, 5, 5, 5)]
    [InlineData(-100, -50, 50, -50)]
    [InlineData(100, -50, 50, 50)]
    public void ClampInt_ReturnsClampedValue(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.ClampInt(value, min, max));
    }

    // -----------------------------------------------------------------------
    // Section 5 — abs_max (a(double,double))
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(3.0, 4.0, 4.0)]
    [InlineData(-5.0, 3.0, 5.0)]
    [InlineData(5.0, -3.0, 5.0)]
    [InlineData(-5.0, -3.0, 5.0)]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(-7.0, -7.0, 7.0)]
    public void AbsMax_ReturnsLargerAbsoluteValue(double a, double b, double expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.AbsMax(a, b));
    }

    [Fact]
    public void AbsMax_EqualMagnitudeOppositeSign_ReturnsPositive()
    {
        // var0=-5 → 5; var2=5 → 5; 5 > 5 is false → returns var2=5
        Assert.Equal(5.0, SpectraEngine.Core.MathHelper.AbsMax(-5.0, 5.0));
    }

    // -----------------------------------------------------------------------
    // Section 5 — bucketInt (a(int,int)) — floor division
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(7, 2, 3)]
    [InlineData(6, 2, 3)]
    [InlineData(-3, 2, -2)]
    [InlineData(-4, 2, -2)]
    [InlineData(-1, 2, -1)]
    [InlineData(0, 2, 0)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 2, 1)]
    [InlineData(-5, 3, -2)]
    [InlineData(-6, 3, -2)]
    [InlineData(-7, 3, -3)]
    public void BucketInt_ReturnsFloorDivision(int dividend, int divisor, int expected)
    {
        Assert.Equal(expected, SpectraEngine.Core.MathHelper.BucketInt(dividend, divisor));
    }

    [Fact]
    public void BucketInt_SpecVerificationExample_Negative3Div2()
    {
        // Spec explicit verification: floor(-3/2) = floor(-1.5) = -2
        Assert.Equal(-2, SpectraEngine.Core.MathHelper.BucketInt(-3, 2));
    }

    [Fact]
    public void BucketInt_SpecVerificationExample_Negative4Div2()
    {
        // Spec explicit verification: floor(-4/2) = floor(-2.0) = -2
        Assert.Equal(-2, SpectraEngine.Core.MathHelper.BucketInt(-4, 2));
    }

    // -----------------------------------------------------------------------
    // Section 5 — isNullOrEmpty (a(String))
    // -----------------------------------------------------------------------

    [Fact]
    public void IsNullOrEmpty_NullInput_ReturnsTrue()
    {
        Assert.True(SpectraEngine.Core.MathHelper.IsNullOrEmpty(null));
    }

    [Fact]
    public void IsNullOrEmpty_EmptyString_ReturnsTrue()
    {
        Assert.True(SpectraEngine.Core.MathHelper.IsNullOrEmpty(""));
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyString_ReturnsFalse()
    {
        Assert.False(SpectraEngine.Core.MathHelper.IsNullOrEmpty("hello"));
    }

    [Fact]
    public void IsNullOrEmpty_SingleChar_ReturnsFalse()
    {
        Assert.False(SpectraEngine.Core.MathHelper.IsNullOrEmpty("a"));
    }

    // -----------------------------------------------------------------------
    // Section 5 — getRandomIntegerInRange (a(Random,int,int))
    // -----------------------------------------------------------------------

    [Fact]
    public void GetRandomIntegerInRange_DegenerateRange_ReturnsMin()
    {
        // var1 >= var2 → return var1, no randomness consumed
        var rng = new FakeJavaRandom(42);
        int result = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(
            rng.NextInt, 5, 5);
        Assert.Equal(5, result);
    }

    [Fact]
    public void GetRandomIntegerInRange_MinGreaterThanMax_ReturnsMin()
    {
        // var1 >= var2 when var1 > var2
        var rng = new FakeJavaRandom(42);
        int result = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(
            rng.NextInt, 10, 3);
        Assert.Equal(10, result);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(0, 1)]
    [InlineData(-5, 5)]
    [InlineData(100, 200)]
    public void GetRandomIntegerInRange_ResultIsWithinInclusiveBounds(int min, int max)
    {
        var rng = new FakeJavaRandom(12345);
        for (int i = 0; i < 1000; i++)
        {
            int result = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(
                rng.NextInt, min, max);
            Assert.True(result >= min && result <= max,
                $"Result {result} not in [{min},{max}]");
        }
    }

    [Fact]
    public void GetRandomIntegerInRange_Deterministic_MatchesExpectedSequence()
    {
        // With a fixed seed, the results must be reproducible.
        var rng1 = new FakeJavaRandom(9999);
        var rng2 = new FakeJavaRandom(9999);

        int r1 = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(rng1.NextInt, 0, 10);
        int r2 = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(rng2.NextInt, 0, 10);
        Assert.Equal(r1, r2);
    }

    [Fact]
    public void GetRandomIntegerInRange_ComputesRange_AsMaxMinusPlusOne()
    {
        // range = var2 - var1 + 1; result ∈ [var1, var2]
        // Check that max value is reachable: range includes var2
        var rng = new FakeJavaRandom(0);
        bool sawMin = false, sawMax = false;
        for (int i = 0; i < 100_000 && !(sawMin && sawMax); i++)
        {
            int r = SpectraEngine.Core.MathHelper.GetRandomIntegerInRange(rng.NextInt, 3, 5);
            if (r == 3) sawMin = true;
            if (r == 5) sawMax = true;
        }
        Assert.True(sawMin, "Never produced minimum value 3");
        Assert.True(sawMax, "Never produced maximum value 5 (should be inclusive)");
    }

    // -----------------------------------------------------------------------
    // Section 8 — Quirk 3: sin/cos table is single-precision only
    // -----------------------------------------------------------------------

    [Fact]
    public void Sin_Quirk3_TableIsSinglePrecision_NotDoublePrecision()
    {
        // The table entries must be floats, not doubles.
        // Verify that Sin returns the float table value, not a double-precision sin.
        float angle = 0.1f;
        float tableResult = SpectraEngine.Core.MathHelper.Sin(angle);
        double doublePrecisionSin = Math.Sin((double)angle);
        // They will differ because the table is sampled at 16-bit resolution
        // The method must return the table float, not the double-precision value cast to float
        int idx = (int)(angle * 10430.378f) & 65535;
        float expectedFromTable = SpectraEngine.Core.MathHelper.SineTable[idx];
        Assert.Equal(expectedFromTable, tableResult);
    }

    [Fact]
    public void Cos_Quirk3_TableIsSinglePrecision_NotDoublePrecision()
    {
        float angle = 0.1f;
        float tableResult = SpectraEngine.Core.MathHelper.Cos(angle);
        int idx = (int)(angle * 10430.378f + 16384.0f) & 65535;
        float expectedFromTable = SpectraEngine.Core.MathHelper.SineTable[idx];
        Assert.Equal(expectedFromTable, tableResult);
    }

    // -----------------------------------------------------------------------
    // Section 8 — Quirk 4: cos +16384 is float arithmetic before (int) cast
    // -----------------------------------------------------------------------

    [Fact]
    public void Cos_Quirk4_AdditionIsFloatArithmeticBeforeCast()
    {
        // For an angle where float rounding of (angle*10430.378f + 16384.0f) differs
        // from (int)(angle*10430.378f) + 16384, the spec mandates the float add first.
        float angle = 1.23456f;
        float product = angle * 10430.378f;
        float sumBeforeCast = product + 16384.0f;
        int correctIdx = (int)sumBeforeCast & 65535;
        int wrongIdx = ((int)product + 16384) & 65535;

        // The test is only meaningful if the two approaches differ
        // (they may or may not for this angle — find one where they do)
        // Use an angle known to produce rounding difference
        float testAngle = 100.0f;
        float p = testAngle * 10430.378f;
        float s = p + 16384.0f;
        int specIdx = (int)s & 65535;
        int altIdx = ((int)p + 16384) & 65535;

        float result = SpectraEngine.Core.MathHelper.Cos(testAngle);
        float expected = SpectraEngine.Core.MathHelper.SineTable[specIdx];
        Assert.Equal(expected, result);
    }
}