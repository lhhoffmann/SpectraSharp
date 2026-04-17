namespace SpectraEngine.Core.WorldGen.Structure;

/// <summary>
/// World-space axis-aligned bounding box for structure pieces. Replica of <c>nl</c>.
/// All coordinates are inclusive block positions.
/// </summary>
public sealed class StructureBoundingBox
{
    public int MinX, MinY, MinZ;
    public int MaxX, MaxY, MaxZ;

    public StructureBoundingBox(int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
    {
        MinX = minX; MinY = minY; MinZ = minZ;
        MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
    }

    // ── Factory (spec: nl.a(x, y, z, dx, dy, dz, w, h, d, orientation)) ─────

    /// <summary>
    /// Creates a world-space AABB from a local piece description.
    /// Parameters: origin (ox,oy,oz), min-corner offsets (dx,dy,dz),
    /// and dimensions (w×h×d) in local space. Orientation rotates the X/Z axes.
    /// </summary>
    public static StructureBoundingBox Create(
        int ox, int oy, int oz,
        int dx, int dy, int dz,
        int w, int h, int d,
        int orientation)
    {
        int minY = oy + dy;
        int maxY = oy + dy + h - 1;

        int minX, maxX, minZ, maxZ;
        switch (orientation)
        {
            default:
            case 0: // south (+Z forward)
                minX = ox + dx;         maxX = ox + dx + w - 1;
                minZ = oz + dz;         maxZ = oz + dz + d - 1;
                break;
            case 1: // west (-X forward): local Z→ −worldX, local X→ +worldZ
                minX = ox - dz - d + 1; maxX = ox - dz;
                minZ = oz + dx;         maxZ = oz + dx + w - 1;
                break;
            case 2: // north (-Z forward): both axes reversed
                minX = ox - dx - w + 1; maxX = ox - dx;
                minZ = oz - dz - d + 1; maxZ = oz - dz;
                break;
            case 3: // east (+X forward): local Z→ +worldX, local X→ −worldZ
                minX = ox + dz;         maxX = ox + dz + d - 1;
                minZ = oz - dx - w + 1; maxZ = oz - dx;
                break;
        }

        return new StructureBoundingBox(minX, minY, minZ, maxX, maxY, maxZ);
    }

    // ── Intersection ─────────────────────────────────────────────────────────

    /// <summary>Returns true if this box overlaps the given box (inclusive).</summary>
    public bool Intersects(StructureBoundingBox other)
        => MaxX >= other.MinX && MinX <= other.MaxX
        && MaxY >= other.MinY && MinY <= other.MaxY
        && MaxZ >= other.MinZ && MinZ <= other.MaxZ;

    /// <summary>Returns true if the world point (x,y,z) is inside this box.</summary>
    public bool Contains(int x, int y, int z)
        => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY && z >= MinZ && z <= MaxZ;

    // ── Expansion (for overall bounds calculation) ────────────────────────────

    /// <summary>Expands this box to also enclose the given box.</summary>
    public void ExpandToInclude(StructureBoundingBox other)
    {
        MinX = Math.Min(MinX, other.MinX);
        MinY = Math.Min(MinY, other.MinY);
        MinZ = Math.Min(MinZ, other.MinZ);
        MaxX = Math.Max(MaxX, other.MaxX);
        MaxY = Math.Max(MaxY, other.MaxY);
        MaxZ = Math.Max(MaxZ, other.MaxZ);
    }

    /// <summary>X span of this box in blocks (inclusive).</summary>
    public int SizeX => MaxX - MinX + 1;
    /// <summary>Y span of this box in blocks (inclusive).</summary>
    public int SizeY => MaxY - MinY + 1;
    /// <summary>Z span of this box in blocks (inclusive).</summary>
    public int SizeZ => MaxZ - MinZ + 1;

    /// <summary>
    /// Convenience factory: no local offsets, origin is the piece's world anchor.
    /// Equivalent to <c>Create(x, y, z, 0, 0, 0, w, h, d, orientation)</c>.
    /// </summary>
    public static StructureBoundingBox FromOrigin(int x, int y, int z, int w, int h, int d, int orientation)
        => Create(x, y, z, 0, 0, 0, w, h, d, orientation);

    /// <summary>Translates the bounding box by the given offset.</summary>
    public void Offset(int dx, int dy, int dz)
    {
        MinX += dx; MaxX += dx;
        MinY += dy; MaxY += dy;
        MinZ += dz; MaxZ += dz;
    }
}
