namespace SpectraSharp.Core.WorldGen.Structure;

/// <summary>
/// Abstract base for all structure pieces. Replica of <c>nk</c> (StructurePiece).
///
/// Coordinate system:
///   Orientation 0 = south (+Z forward), 1 = west (−X forward),
///   2 = north (−Z forward), 3 = east (+X forward).
///
/// Local→world transform (stored in BBox):
///   GetWorldX(lx, lz): orientation 0→minX+lx, 1→maxX−lz, 2→maxX−lx, 3→minX+lz
///   GetWorldZ(lx, lz): orientation 0→minZ+lz, 1→minZ+lx, 2→maxZ−lz, 3→maxZ−lx
///   GetWorldY(ly)     : always minY+ly
/// </summary>
public abstract class StructurePiece
{
    // ── Fields (spec §3.1) ────────────────────────────────────────────────────

    public StructureBoundingBox BBox;    // obf: e — world-space AABB
    public int  Orientation;             // obf: f — 0=south,1=west,2=north,3=east
    public int  Depth;                   // obf: g — generation depth counter

    // ── Construction ──────────────────────────────────────────────────────────

    protected StructurePiece(StructureBoundingBox bbox, int orientation, int depth)
    {
        BBox        = bbox;
        Orientation = orientation;
        Depth       = depth;
    }

    // ── Coordinate transform (spec: nk.getWorldX/Y/Z) ────────────────────────

    /// <summary>Converts local X (and Z) to world X based on orientation.</summary>
    protected int GetWorldX(int localX, int localZ) => Orientation switch
    {
        0 => BBox.MinX + localX,
        1 => BBox.MaxX - localZ,
        2 => BBox.MaxX - localX,
        3 => BBox.MinX + localZ,
        _ => BBox.MinX + localX
    };

    /// <summary>Converts local Z (and X) to world Z based on orientation.</summary>
    protected int GetWorldZ(int localX, int localZ) => Orientation switch
    {
        0 => BBox.MinZ + localZ,
        1 => BBox.MinZ + localX,
        2 => BBox.MaxZ - localZ,
        3 => BBox.MaxZ - localX,
        _ => BBox.MinZ + localZ
    };

    /// <summary>Converts local Y to world Y (always bbox.minY + localY).</summary>
    protected int GetWorldY(int localY) => BBox.MinY + localY;

    // ── Block placement helpers ───────────────────────────────────────────────

    /// <summary>
    /// Places a single block at local (lx, ly, lz). Clipped to generationBounds.
    /// Spec: <c>nk.a(ry world, int blockId, int meta, int lx, int ly, int lz, nl bounds)</c>.
    /// </summary>
    protected void PlaceBlock(World world, int blockId, int meta, int lx, int ly, int lz, StructureBoundingBox generationBounds)
    {
        int wx = GetWorldX(lx, lz);
        int wy = GetWorldY(ly);
        int wz = GetWorldZ(lx, lz);
        if (generationBounds.Contains(wx, wy, wz))
            world.SetBlockAndMetadata(wx, wy, wz, blockId, meta);
    }

    /// <summary>
    /// Places a block only if within world bounds (no generation-bounds clip).
    /// Spec: <c>nk.b(ry, int, int, int, int, int, nl)</c> — foundation fill variant.
    /// </summary>
    protected void PlaceBlockIfInWorld(World world, int blockId, int meta, int lx, int ly, int lz, StructureBoundingBox generationBounds)
    {
        int wx = GetWorldX(lx, lz);
        int wy = GetWorldY(ly);
        int wz = GetWorldZ(lx, lz);
        if (wy >= 0 && wy < World.WorldHeight && generationBounds.Contains(wx, wy, wz))
            world.SetBlockAndMetadata(wx, wy, wz, blockId, meta);
    }

    /// <summary>
    /// Fills a local bounding box. If outlineId != fillId, uses fillId for interior
    /// and outlineId for the 6 faces. If keepAir, skips positions already holding air.
    /// Spec: <c>nk.a(ry, nl bounds, x1,y1,z1, x2,y2,z2, fillId, outlineId, keepAir)</c>.
    /// </summary>
    protected void FillBox(
        World world,
        StructureBoundingBox generationBounds,
        int lx1, int ly1, int lz1,
        int lx2, int ly2, int lz2,
        int fillId, int outlineId, bool keepAir)
    {
        for (int ly = ly1; ly <= ly2; ly++)
        for (int lx = lx1; lx <= lx2; lx++)
        for (int lz = lz1; lz <= lz2; lz++)
        {
            int wx = GetWorldX(lx, lz);
            int wy = GetWorldY(ly);
            int wz = GetWorldZ(lx, lz);
            if (!generationBounds.Contains(wx, wy, wz)) continue;
            if (keepAir && world.GetBlockId(wx, wy, wz) == 0) continue;

            bool isEdge = lx == lx1 || lx == lx2 || ly == ly1 || ly == ly2 || lz == lz1 || lz == lz2;
            world.SetBlockAndMetadata(wx, wy, wz, isEdge ? outlineId : fillId, 0);
        }
    }

    /// <summary>
    /// Fills a local box with a single block type everywhere (no outline distinction).
    /// </summary>
    protected void FillBox(
        World world, StructureBoundingBox generationBounds,
        int lx1, int ly1, int lz1,
        int lx2, int ly2, int lz2,
        int blockId)
        => FillBox(world, generationBounds, lx1, ly1, lz1, lx2, ly2, lz2, blockId, blockId, false);

    /// <summary>
    /// Clears a local box to air.
    /// </summary>
    protected void ClearBox(
        World world, StructureBoundingBox generationBounds,
        int lx1, int ly1, int lz1, int lx2, int ly2, int lz2)
        => FillBox(world, generationBounds, lx1, ly1, lz1, lx2, ly2, lz2, 0, 0, false);

    // ── Abstract interface ────────────────────────────────────────────────────

    /// <summary>
    /// Places all blocks of this piece into the world. Only writes within generationBounds.
    /// </summary>
    public abstract void Generate(World world, JavaRandom rng, StructureBoundingBox generationBounds);

    /// <summary>
    /// Called right after construction to add this piece's exits to the pending list.
    /// </summary>
    public virtual void AddExits(object startPiece, List<StructurePiece> pieces, JavaRandom rng) { }
}
