namespace SpectraEngine.Core;

/// <summary>
/// Bit-exact replica of the original <c>c</c> (AxisAlignedBB) class.
///
/// Axis-aligned bounding box used for all block and entity collision, physics sweep,
/// and ray intersection. Maintains a static object pool to avoid per-tick allocation.
///
/// Quirks preserved (see spec §9):
///   - Touching boxes (shared face) are NOT overlapping — guards use &lt;= / &gt;= exits.
///   - isVecInside uses open intervals; ray-face validators use closed intervals.
///   - Only <see cref="OffsetInPlace"/> mutates <c>this</c>; all other geometry methods
///     return a new (pooled) instance.
///   - Pool is globally shared static state — caller must manage reset cadence.
///   - addCoord with ±0.0 expands nothing (IEEE 754 −0.0 compares false for &lt; and &gt;).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/AxisAlignedBB_Spec.md
/// </summary>
public sealed class AxisAlignedBB
{
    // ── Instance fields (spec §2) — field order: minX, minY, minZ, maxX, maxY, maxZ ──

    public double MinX; // obf: a
    public double MinY; // obf: b
    public double MinZ; // obf: c
    public double MaxX; // obf: d
    public double MaxY; // obf: e
    public double MaxZ; // obf: f

    // ── Static pool (spec §2 / §4) ────────────────────────────────────────────

    private static readonly List<AxisAlignedBB> Pool = [];  // obf: g
    private static int _poolCursor;                          // obf: h

    // ── Pool API (spec §4) ────────────────────────────────────────────────────

    /// <summary>
    /// Clears the pool entirely. Spec: static <c>a()</c>.
    /// Call on world/level transitions to allow GC to reclaim pooled instances.
    /// </summary>
    public static void ClearPool()
    {
        Pool.Clear();
        _poolCursor = 0;
    }

    /// <summary>
    /// Resets the pool cursor without discarding instances. Spec: static <c>b()</c>.
    /// Call once per tick/physics-pass before any sweep or collision query.
    /// </summary>
    public static void ResetPool()
    {
        _poolCursor = 0;
    }

    /// <summary>
    /// Returns a pooled AABB configured with the given coordinates. Spec: static <c>b(6×double)</c>.
    /// The returned instance is only valid until the next <see cref="ResetPool"/> call.
    /// </summary>
    public static AxisAlignedBB GetFromPool(
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ)
    {
        if (_poolCursor >= Pool.Count)
            Pool.Add(new AxisAlignedBB(0, 0, 0, 0, 0, 0));

        AxisAlignedBB box = Pool[_poolCursor++];
        box.Set(minX, minY, minZ, maxX, maxY, maxZ);
        return box;
    }

    // ── Factories (spec §5) ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a new heap-allocated AABB that is NOT in the pool.
    /// Safe to hold across tick boundaries. Spec: static <c>a(6×double)</c>.
    /// </summary>
    public static AxisAlignedBB Create(
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ)
        => new(minX, minY, minZ, maxX, maxY, maxZ);

    // ── Constructor ───────────────────────────────────────────────────────────

    private AxisAlignedBB(
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ)
    {
        MinX = minX; MinY = minY; MinZ = minZ;
        MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
    }

    // ── Instance methods (spec §6) ────────────────────────────────────────────

    /// <summary>
    /// Reset all 6 fields in-place and return <c>this</c>. Spec: instance <c>c(6×double)</c>.
    /// Used by the pool to reconfigure a recycled instance.
    /// </summary>
    public AxisAlignedBB Set(
        double minX, double minY, double minZ,
        double maxX, double maxY, double maxZ)
    {
        MinX = minX; MinY = minY; MinZ = minZ;
        MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        return this;
    }

    /// <summary>
    /// Expand the box along the movement vector — returns a pooled box that contains
    /// both the original and the displaced copy. Spec: instance <c>a(double,double,double)</c>.
    /// IEEE 754 ±0.0 triggers no expansion on that axis (spec quirk 6).
    /// </summary>
    public AxisAlignedBB AddCoord(double dx, double dy, double dz)
    {
        double newMinX = MinX, newMinY = MinY, newMinZ = MinZ;
        double newMaxX = MaxX, newMaxY = MaxY, newMaxZ = MaxZ;

        if (dx < 0.0) newMinX += dx;
        if (dx > 0.0) newMaxX += dx;
        if (dy < 0.0) newMinY += dy;
        if (dy > 0.0) newMaxY += dy;
        if (dz < 0.0) newMinZ += dz;
        if (dz > 0.0) newMaxZ += dz;

        return GetFromPool(newMinX, newMinY, newMinZ, newMaxX, newMaxY, newMaxZ);
    }

    /// <summary>
    /// Symmetric inflation — grow by <paramref name="dx"/>/<paramref name="dy"/>/<paramref name="dz"/>
    /// on all sides. Returns a pooled instance. Spec: instance <c>b(double,double,double)</c>.
    /// </summary>
    public AxisAlignedBB Expand(double dx, double dy, double dz)
        => GetFromPool(MinX - dx, MinY - dy, MinZ - dz, MaxX + dx, MaxY + dy, MaxZ + dz);

    /// <summary>
    /// Translate the box, returning a new pooled instance. Does NOT mutate <c>this</c>.
    /// Spec: instance <c>c(double,double,double)</c>.
    /// </summary>
    public AxisAlignedBB Offset(double dx, double dy, double dz)
        => GetFromPool(MinX + dx, MinY + dy, MinZ + dz, MaxX + dx, MaxY + dy, MaxZ + dz);

    /// <summary>
    /// Translate the box IN-PLACE, mutating <c>this</c> and returning <c>this</c>.
    /// This is the ONLY method that mutates <c>this</c>. Spec: instance <c>d(double,double,double)</c>.
    /// Spec quirk 4 — do not confuse with <see cref="Offset"/>.
    /// </summary>
    public AxisAlignedBB OffsetInPlace(double dx, double dy, double dz)
    {
        MinX += dx; MinY += dy; MinZ += dz;
        MaxX += dx; MaxY += dy; MaxZ += dz;
        return this;
    }

    /// <summary>
    /// Symmetric contraction — inverse of <see cref="Expand"/>. Returns a pooled instance.
    /// Spec: instance <c>e(double,double,double)</c>.
    /// </summary>
    public AxisAlignedBB Contract(double dx, double dy, double dz)
        => GetFromPool(MinX + dx, MinY + dy, MinZ + dz, MaxX - dx, MaxY - dy, MaxZ - dz);

    /// <summary>
    /// Return a pooled copy of this box. Spec: instance <c>d()</c>.
    /// </summary>
    public AxisAlignedBB Copy()
        => GetFromPool(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);

    /// <summary>
    /// Sweep collision along X. Returns the maximum X movement <paramref name="other"/>
    /// can make before hitting <c>this</c>. Spec: instance <c>a(c, double)</c>.
    /// Guards use non-strict inequality — touching faces are NOT overlapping (quirk 1 &amp; 3).
    /// </summary>
    public double CalculateXOffset(AxisAlignedBB other, double deltaX)
    {
        if (other.MaxY <= MinY || other.MinY >= MaxY) return deltaX;
        if (other.MaxZ <= MinZ || other.MinZ >= MaxZ) return deltaX;

        if (deltaX > 0.0 && other.MaxX <= MinX)
        {
            double gap = MinX - other.MaxX;
            if (gap < deltaX) deltaX = gap;
        }
        if (deltaX < 0.0 && other.MinX >= MaxX)
        {
            double gap = MaxX - other.MinX;
            if (gap > deltaX) deltaX = gap;
        }
        return deltaX;
    }

    /// <summary>
    /// Sweep collision along Y. Spec: instance <c>b(c, double)</c>.
    /// </summary>
    public double CalculateYOffset(AxisAlignedBB other, double deltaY)
    {
        if (other.MaxX <= MinX || other.MinX >= MaxX) return deltaY;
        if (other.MaxZ <= MinZ || other.MinZ >= MaxZ) return deltaY;

        if (deltaY > 0.0 && other.MaxY <= MinY)
        {
            double gap = MinY - other.MaxY;
            if (gap < deltaY) deltaY = gap;
        }
        if (deltaY < 0.0 && other.MinY >= MaxY)
        {
            double gap = MaxY - other.MinY;
            if (gap > deltaY) deltaY = gap;
        }
        return deltaY;
    }

    /// <summary>
    /// Sweep collision along Z. Spec: instance <c>c(c, double)</c>.
    /// </summary>
    public double CalculateZOffset(AxisAlignedBB other, double deltaZ)
    {
        if (other.MaxX <= MinX || other.MinX >= MaxX) return deltaZ;
        if (other.MaxY <= MinY || other.MinY >= MaxY) return deltaZ;

        if (deltaZ > 0.0 && other.MaxZ <= MinZ)
        {
            double gap = MinZ - other.MaxZ;
            if (gap < deltaZ) deltaZ = gap;
        }
        if (deltaZ < 0.0 && other.MinZ >= MaxZ)
        {
            double gap = MaxZ - other.MinZ;
            if (gap > deltaZ) deltaZ = gap;
        }
        return deltaZ;
    }

    /// <summary>
    /// Returns true if the two boxes share any interior volume (open intervals).
    /// Touching boxes (shared face) return false. Spec: instance <c>a(c)</c> (quirk 1).
    /// </summary>
    public bool Intersects(AxisAlignedBB other)
    {
        if (other.MaxX <= MinX || other.MinX >= MaxX) return false;
        if (other.MaxY <= MinY || other.MinY >= MaxY) return false;
        if (other.MaxZ <= MinZ || other.MinZ >= MaxZ) return false;
        return true;
    }

    /// <summary>
    /// Returns arithmetic mean of the three side lengths. Spec: instance <c>c()</c>.
    /// </summary>
    public double AverageEdgeLength()
        => ((MaxX - MinX) + (MaxY - MinY) + (MaxZ - MinZ)) / 3.0;

    /// <summary>
    /// Copy all 6 coordinates from <paramref name="source"/> into <c>this</c> (in-place).
    /// Spec: instance <c>b(c)</c>.
    /// </summary>
    public void SetBB(AxisAlignedBB source)
    {
        MinX = source.MinX; MinY = source.MinY; MinZ = source.MinZ;
        MaxX = source.MaxX; MaxY = source.MaxY; MaxZ = source.MaxZ;
    }

    // ── Vec3-dependent methods (spec §6) ─────────────────────────────────────

    /// <summary>
    /// Test whether point <paramref name="vec"/> lies strictly inside this box.
    /// Interval type: OPEN — a point exactly on a face returns false (quirk 2).
    /// Spec: instance <c>a(fb)</c>.
    /// </summary>
    public bool IsVecInside(Vec3 vec)
    {
        if (vec.X <= MinX || vec.X >= MaxX) return false;
        if (vec.Y <= MinY || vec.Y >= MaxY) return false;
        if (vec.Z <= MinZ || vec.Z >= MaxZ) return false;
        return true;
    }

    /// <summary>
    /// Find the first intersection of segment [<paramref name="start"/>, <paramref name="end"/>]
    /// with any face of this box. Returns null if no face is hit.
    /// Hit-position Vec3 in the result is pooled — invalid after next Vec3.ResetPool().
    /// Spec: instance <c>a(fb, fb)</c>.
    /// </summary>
    public MovingObjectPosition? RayTrace(Vec3 start, Vec3 end)
    {
        // Step 1 — compute six face-plane intersection candidates
        Vec3? v3 = start.GetIntermediateWithXValue(end, MinX); // −X face
        Vec3? v4 = start.GetIntermediateWithXValue(end, MaxX); // +X face
        Vec3? v5 = start.GetIntermediateWithYValue(end, MinY); // −Y face
        Vec3? v6 = start.GetIntermediateWithYValue(end, MaxY); // +Y face
        Vec3? v7 = start.GetIntermediateWithZValue(end, MinZ); // −Z face
        Vec3? v8 = start.GetIntermediateWithZValue(end, MaxZ); // +Z face

        // Step 2 — validate each candidate (closed intervals — quirk 2)
        if (!IsOnYzFace(v3)) v3 = null;
        if (!IsOnYzFace(v4)) v4 = null;
        if (!IsOnXzFace(v5)) v5 = null;
        if (!IsOnXzFace(v6)) v6 = null;
        if (!IsOnXyFace(v7)) v7 = null;
        if (!IsOnXyFace(v8)) v8 = null;

        // Step 3 — pick closest candidate by squared distance; first in order wins ties
        Vec3? best = null;
        double bestSq = double.MaxValue;

        // Order: v3, v4, v5, v6, v7, v8 — strict less-than preserves tie-break order
        foreach (Vec3? candidate in (ReadOnlySpan<Vec3?>)[v3, v4, v5, v6, v7, v8])
        {
            if (candidate is null) continue;
            double sq = start.SquaredDistanceTo(candidate);
            if (sq < bestSq) { best = candidate; bestSq = sq; }
        }

        // Step 4 — no hit
        if (best is null) return null;

        // Step 5 — assign face ID (identity comparison; last match wins — spec §6, step 5)
        int faceId = -1;
        if (best == v3) faceId = 4;
        if (best == v4) faceId = 5;
        if (best == v5) faceId = 0;
        if (best == v6) faceId = 1;
        if (best == v7) faceId = 2;
        if (best == v8) faceId = 3;

        // Step 6 — return result (block coords 0,0,0 — callers supply real coords)
        return new MovingObjectPosition(0, 0, 0, faceId, best);
    }

    /// <summary>
    /// Alias for <see cref="RayTrace"/> — same calculation, name used in EntityArrow.
    /// obf: same as <c>a(fb, fb)</c>.
    /// </summary>
    public MovingObjectPosition? CalculateIntercept(Vec3 start, Vec3 end) => RayTrace(start, end);

    // Private ray-trace face validators — CLOSED intervals (spec §6 private helpers)
    // Opposite of IsVecInside which uses OPEN intervals (quirk 2).
    private bool IsOnYzFace(Vec3? v)
        => v is not null && v.Y >= MinY && v.Y <= MaxY && v.Z >= MinZ && v.Z <= MaxZ;

    private bool IsOnXzFace(Vec3? v)
        => v is not null && v.X >= MinX && v.X <= MaxX && v.Z >= MinZ && v.Z <= MaxZ;

    private bool IsOnXyFace(Vec3? v)
        => v is not null && v.X >= MinX && v.X <= MaxX && v.Y >= MinY && v.Y <= MaxY;

    // ── toString ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Spec: <c>box[minX, minY, minZ -&gt; maxX, maxY, maxZ]</c>
    /// </summary>
    public override string ToString()
        => $"box[{MinX}, {MinY}, {MinZ} -> {MaxX}, {MaxY}, {MaxZ}]";
}
