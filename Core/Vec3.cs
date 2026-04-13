namespace SpectraSharp.Core;

/// <summary>
/// Bit-exact replica of the original <c>fb</c> (Vec3) class.
///
/// Three-component double-precision vector used for positions, directions, and ray
/// endpoints. Maintains a static object pool (identical pattern to AxisAlignedBB).
///
/// Quirks preserved (see spec §8):
///   - Private constructor normalises -0.0 → +0.0 on all components.
///     The pool setter does NOT — pooled instances may carry -0.0.
///   - getIntermediate* guard uses the FLOAT literal 1E-7F (not double 1e-7).
///   - normalize / distanceTo / length use float-precision sqrt (MathHelper.SqrtDouble).
///   - Subtract returns var1 - this (points FROM this TOWARD var1), NOT this - var1.
///   - rotateAroundX/Y mutate in-place and return void.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Vec3_Spec.md
/// </summary>
public sealed class Vec3
{
    // ── Instance fields (spec §2) ─────────────────────────────────────────────

    public double X; // obf: a
    public double Y; // obf: b
    public double Z; // obf: c

    // ── Static pool (spec §2 / §3) ────────────────────────────────────────────

    private static readonly List<Vec3> Pool = []; // obf: d
    private static int _poolCursor;               // obf: e

    // ── Pool API (spec §3) ────────────────────────────────────────────────────

    /// <summary>Clears the pool entirely. Spec: static <c>a()</c>.</summary>
    public static void ClearPool()
    {
        Pool.Clear();
        _poolCursor = 0;
    }

    /// <summary>Resets pool cursor without discarding instances. Spec: static <c>b()</c>.</summary>
    public static void ResetPool()
    {
        _poolCursor = 0;
    }

    /// <summary>Returns a pooled Vec3. Spec: static <c>b(double, double, double)</c>.</summary>
    public static Vec3 GetFromPool(double x, double y, double z)
    {
        if (_poolCursor >= Pool.Count)
            Pool.Add(new Vec3(0.0, 0.0, 0.0));

        Vec3 v = Pool[_poolCursor++];
        v.SetNoNorm(x, y, z); // pool setter — no -0.0 normalisation (quirk 1)
        return v;
    }

    // ── Factories (spec §3) ───────────────────────────────────────────────────

    /// <summary>Heap-allocated Vec3, safe across pool resets. Spec: static <c>a(double,double,double)</c>.</summary>
    public static Vec3 Create(double x, double y, double z) => new(x, y, z);

    // ── Constructors (spec §4) ────────────────────────────────────────────────

    /// <summary>
    /// Private constructor. Normalises -0.0 → +0.0 on all axes (quirk 1).
    /// IEEE 754: (x == -0.0) is true for both +0.0 and -0.0 → all zeros become +0.0.
    /// </summary>
    private Vec3(double x, double y, double z)
    {
        // Spec §4: Java `var1 == -0.0` fires for both +0.0 and -0.0 (IEEE 754).
        if (x == -0.0) x = 0.0;
        if (y == -0.0) y = 0.0;
        if (z == -0.0) z = 0.0;
        X = x; Y = y; Z = z;
    }

    /// <summary>Pool setter — pure assignment, no normalisation. Spec: instance <c>e(double,double,double)</c>.</summary>
    private Vec3 SetNoNorm(double x, double y, double z)
    {
        X = x; Y = y; Z = z;
        return this;
    }

    // ── Methods (spec §5) ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the pooled vector pointing FROM <c>this</c> TOWARD <paramref name="other"/>
    /// (i.e. <c>other - this</c>). Spec: instance <c>a(fb)</c> (quirk 4 — direction is var1 - this).
    /// </summary>
    public Vec3 Subtract(Vec3 other)
        => GetFromPool(other.X - X, other.Y - Y, other.Z - Z);

    /// <summary>
    /// Returns the unit vector in the same direction. Float-precision sqrt (quirk 3).
    /// Returns (0,0,0) when length &lt; 1E-4F. Spec: instance <c>c()</c>.
    /// </summary>
    public Vec3 Normalize()
    {
        double sq = X * X + Y * Y + Z * Z;
        float len = MathHelper.SqrtDouble(sq); // float-precision (quirk 3)
        if (len < 1.0E-4F)                     // float < float comparison
            return GetFromPool(0.0, 0.0, 0.0);
        return GetFromPool(X / (double)len, Y / (double)len, Z / (double)len);
    }

    /// <summary>Dot product. Spec: instance <c>b(fb)</c>.</summary>
    public double Dot(Vec3 other)
        => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>Cross product this × other, returned as a pooled Vec3. Spec: instance <c>c(fb)</c>.</summary>
    public Vec3 Cross(Vec3 other)
        => GetFromPool(
            Y * other.Z - Z * other.Y,
            Z * other.X - X * other.Z,
            X * other.Y - Y * other.X);

    /// <summary>Returns <c>this + (dx, dy, dz)</c> as a pooled Vec3. Spec: instance <c>c(double,double,double)</c>.</summary>
    public Vec3 Add(double dx, double dy, double dz)
        => GetFromPool(X + dx, Y + dy, Z + dz);

    /// <summary>
    /// Euclidean distance to <paramref name="other"/>. Float-precision sqrt (quirk 3).
    /// Spec: instance <c>d(fb)</c>.
    /// </summary>
    public double DistanceTo(Vec3 other)
    {
        double dx = other.X - X, dy = other.Y - Y, dz = other.Z - Z;
        return (double)MathHelper.SqrtDouble(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Squared distance to <paramref name="other"/>. No sqrt. Used by AxisAlignedBB.rayTrace
    /// to find the closest hit (quirk 6). Spec: instance <c>e(fb)</c>.
    /// </summary>
    public double SquaredDistanceTo(Vec3 other)
    {
        double dx = other.X - X, dy = other.Y - Y, dz = other.Z - Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>Squared distance to the point (x, y, z). Spec: instance <c>d(double,double,double)</c>.</summary>
    public double SquaredDistanceTo(double x, double y, double z)
    {
        double dx = x - X, dy = y - Y, dz = z - Z;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>Magnitude of this vector. Float-precision sqrt (quirk 3). Spec: instance <c>d()</c>.</summary>
    public double Length()
        => (double)MathHelper.SqrtDouble(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Find the point on segment [this, <paramref name="end"/>] where X = <paramref name="xValue"/>.
    /// Returns null if the segment is nearly parallel to the YZ plane or the intersection
    /// lies outside the segment. Spec: instance <c>a(fb, double)</c> (quirk 2 — float threshold).
    /// </summary>
    public Vec3? GetIntermediateWithXValue(Vec3 end, double xValue)
    {
        double dx = end.X - X, dy = end.Y - Y, dz = end.Z - Z;
        if (dx * dx < (double)1E-7F) return null; // float literal widened (quirk 2)
        double t = (xValue - X) / dx;
        if (t < 0.0 || t > 1.0) return null;
        return GetFromPool(X + dx * t, Y + dy * t, Z + dz * t);
    }

    /// <summary>
    /// Find the point on segment [this, <paramref name="end"/>] where Y = <paramref name="yValue"/>.
    /// Spec: instance <c>b(fb, double)</c>.
    /// </summary>
    public Vec3? GetIntermediateWithYValue(Vec3 end, double yValue)
    {
        double dx = end.X - X, dy = end.Y - Y, dz = end.Z - Z;
        if (dy * dy < (double)1E-7F) return null;
        double t = (yValue - Y) / dy;
        if (t < 0.0 || t > 1.0) return null;
        return GetFromPool(X + dx * t, Y + dy * t, Z + dz * t);
    }

    /// <summary>
    /// Find the point on segment [this, <paramref name="end"/>] where Z = <paramref name="zValue"/>.
    /// Spec: instance <c>c(fb, double)</c>.
    /// </summary>
    public Vec3? GetIntermediateWithZValue(Vec3 end, double zValue)
    {
        double dx = end.X - X, dy = end.Y - Y, dz = end.Z - Z;
        if (dz * dz < (double)1E-7F) return null;
        double t = (zValue - Z) / dz;
        if (t < 0.0 || t > 1.0) return null;
        return GetFromPool(X + dx * t, Y + dy * t, Z + dz * t);
    }

    /// <summary>
    /// Rotate around the X axis by <paramref name="angle"/> radians. Mutates in-place.
    /// Uses MathHelper.Cos/Sin (float-table trig). Spec: instance <c>a(float)</c> (quirk 5).
    /// </summary>
    public void RotateAroundX(float angle)
    {
        float cos = MathHelper.Cos(angle);
        float sin = MathHelper.Sin(angle);
        double newY = Y * (double)cos + Z * (double)sin;
        double newZ = Z * (double)cos - Y * (double)sin;
        Y = newY;
        Z = newZ;
        // X unchanged
    }

    /// <summary>
    /// Rotate around the Y axis by <paramref name="angle"/> radians. Mutates in-place.
    /// Spec: instance <c>b(float)</c> (quirk 5).
    /// </summary>
    public void RotateAroundY(float angle)
    {
        float cos = MathHelper.Cos(angle);
        float sin = MathHelper.Sin(angle);
        double newX = X * (double)cos + Z * (double)sin;
        double newZ = Z * (double)cos - X * (double)sin;
        X = newX;
        Z = newZ;
        // Y unchanged
    }

    /// <summary>Spec: <c>(X, Y, Z)</c></summary>
    public override string ToString() => $"({X}, {Y}, {Z})";
}
