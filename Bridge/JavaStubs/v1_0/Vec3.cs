// AllocGuard Tier 1 — Vec3 as stack-allocated value type
// Java equivalent: net.minecraft.src.Vec3 (obf: zb in 1.0)
//
// DESIGN: Java Vec3 is a heap-allocated mutable object, a massive GC source.
//         As a C# readonly record struct it lives entirely on the stack for all
//         transient calculations (entity movement, raycasting, look vectors).
//         Mods that store Vec3 in fields get a boxed copy — one alloc instead
//         of many.  Value equality is correct here: two positions are equal iff
//         their coordinates are equal.

namespace net.minecraft.src;

/// <summary>
/// MinecraftStubs v1_0 — Vec3 (obf: zb).
/// Three-component double vector used for entity positions and movement deltas.
///
/// Declared as <c>readonly record struct</c> — stack-allocated, zero GC for
/// transient calculations.  Matches the Java public API surface so mod bytecode
/// compiled against these stubs works without source changes.
/// </summary>
public readonly record struct Vec3(double xCoord, double yCoord, double zCoord)
{
    // ── Arithmetic ────────────────────────────────────────────────────────────

    /// <summary>Returns the Euclidean distance to another Vec3.</summary>
    public double distanceTo(Vec3 other)
    {
        double dx = other.xCoord - xCoord;
        double dy = other.yCoord - yCoord;
        double dz = other.zCoord - zCoord;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>Returns the squared distance (avoids sqrt — prefer for comparisons).</summary>
    public double squareDistanceTo(Vec3 other)
    {
        double dx = other.xCoord - xCoord;
        double dy = other.yCoord - yCoord;
        double dz = other.zCoord - zCoord;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>Returns length of this vector.</summary>
    public double lengthVector() =>
        Math.Sqrt(xCoord * xCoord + yCoord * yCoord + zCoord * zCoord);

    /// <summary>Returns this vector normalised to unit length.</summary>
    public Vec3 normalize()
    {
        double len = lengthVector();
        if (len < 1e-12) return new Vec3(0, 0, 0);
        return new Vec3(xCoord / len, yCoord / len, zCoord / len);
    }

    /// <summary>Dot product.</summary>
    public double dotProduct(Vec3 other) =>
        xCoord * other.xCoord + yCoord * other.yCoord + zCoord * other.zCoord;

    /// <summary>Cross product.</summary>
    public Vec3 crossProduct(Vec3 other) =>
        new(
            yCoord * other.zCoord - zCoord * other.yCoord,
            zCoord * other.xCoord - xCoord * other.zCoord,
            xCoord * other.yCoord - yCoord * other.xCoord);

    /// <summary>Component-wise addition.</summary>
    public Vec3 addVector(double x, double y, double z) =>
        new(xCoord + x, yCoord + y, zCoord + z);

    /// <summary>Returns intermediate point between this and another Vec3 at factor t.</summary>
    public Vec3 getIntermediateWithXValue(Vec3 other, double x)
    {
        if (Math.Abs(other.xCoord - xCoord) < 1e-12) return this;
        double t = (x - xCoord) / (other.xCoord - xCoord);
        if (t < 0 || t > 1) return this;
        return new Vec3(x,
            yCoord + (other.yCoord - yCoord) * t,
            zCoord + (other.zCoord - zCoord) * t);
    }

    public Vec3 getIntermediateWithYValue(Vec3 other, double y)
    {
        if (Math.Abs(other.yCoord - yCoord) < 1e-12) return this;
        double t = (y - yCoord) / (other.yCoord - yCoord);
        if (t < 0 || t > 1) return this;
        return new Vec3(
            xCoord + (other.xCoord - xCoord) * t, y,
            zCoord + (other.zCoord - zCoord) * t);
    }

    public Vec3 getIntermediateWithZValue(Vec3 other, double z)
    {
        if (Math.Abs(other.zCoord - zCoord) < 1e-12) return this;
        double t = (z - zCoord) / (other.zCoord - zCoord);
        if (t < 0 || t > 1) return this;
        return new Vec3(
            xCoord + (other.xCoord - xCoord) * t,
            yCoord + (other.yCoord - yCoord) * t, z);
    }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 1.0 mods call the static factory <c>Vec3.createVectorHelper(x,y,z)</c>
    /// instead of using <c>new Vec3</c>.
    /// </summary>
    public static Vec3 createVectorHelper(double x, double y, double z) =>
        new(x, y, z);

    // ── Java interop ──────────────────────────────────────────────────────────

    public override string ToString() =>
        $"({xCoord:F3}, {yCoord:F3}, {zCoord:F3})";
}
