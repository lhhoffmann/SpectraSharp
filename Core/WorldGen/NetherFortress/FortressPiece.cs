using SpectraSharp.Core.WorldGen.Structure;

namespace SpectraSharp.Core.WorldGen.NetherFortress;

// ── Block ID constants used throughout all fortress pieces ────────────────────
// Spec §2
internal static class NF
{
    public const int NetherBrick      = 112;
    public const int NetherFence      = 113;
    public const int NetherStairs     = 114;
    public const int SoulSand         = 88;
    public const int NetherWart       = 115;
    public const int MobSpawner       = 52;
    public const int Lava             = 10;
    public const int Air              = 0;
}

/// <summary>
/// Base for all Nether Fortress structure pieces. Replica of <c>rh</c> (FortressPiece).
///
/// Extends StructurePiece with exit-spawning helpers (a/b/c) and the radius + depth guards.
/// Guards: depth &gt; 30 → dead end; |X or Z| from start origin &gt; 112 → dead end.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §3.2
/// </summary>
internal abstract class FortressPiece : StructurePiece
{
    protected FortressPiece(StructureBoundingBox bbox, int orientation, int depth)
        : base(bbox, orientation, depth) { }

    // ── Exit-spawning helpers (spec §3.2) ─────────────────────────────────────

    /// <summary>
    /// Tries to spawn a new piece in the FORWARD direction from this piece's front face.
    /// Spec: <c>rh.a(gc, list, rng, offsetForward, offsetSide, useRoomList)</c>.
    /// </summary>
    protected StructurePiece? SpawnForward(StartingPiece start, List<StructurePiece> pieces,
        JavaRandom rng, int entryZOffset, int entryXOffset, bool useRoomList)
    {
        // World position of the exit — one block past the forward face
        int lx = entryXOffset;
        int lz = BBox.SizeZ;          // one past the end in local Z
        int wx = GetWorldX(lx, lz);
        int wy = GetWorldY(0);
        int wz = GetWorldZ(lx, lz);

        return TryCreatePiece(start, pieces, rng, wx, wy, wz, Orientation, Depth + 1, useRoomList);
    }

    /// <summary>
    /// Tries to spawn a new piece to the LEFT of this piece.
    /// Spec: <c>rh.b(gc, list, rng, offsetSide, offsetForward, useRoomList)</c>.
    /// </summary>
    protected StructurePiece? SpawnLeft(StartingPiece start, List<StructurePiece> pieces,
        JavaRandom rng, int entryXOffset, int entryZOffset, bool useRoomList)
    {
        // Left face: local X = -1 (one past the left edge)
        int wx = GetWorldX(-1, entryZOffset);
        int wy = GetWorldY(0);
        int wz = GetWorldZ(-1, entryZOffset);
        int leftOrientation = (Orientation + 3) & 3; // turn left

        return TryCreatePiece(start, pieces, rng, wx, wy, wz, leftOrientation, Depth + 1, useRoomList);
    }

    /// <summary>
    /// Tries to spawn a new piece to the RIGHT of this piece.
    /// Spec: <c>rh.c(gc, list, rng, offsetSide, offsetForward, useRoomList)</c>.
    /// </summary>
    protected StructurePiece? SpawnRight(StartingPiece start, List<StructurePiece> pieces,
        JavaRandom rng, int entryXOffset, int entryZOffset, bool useRoomList)
    {
        // Right face: local X = width (one past the right edge)
        int width = BBox.SizeX;
        int wx = GetWorldX(width, entryZOffset);
        int wy = GetWorldY(0);
        int wz = GetWorldZ(width, entryZOffset);
        int rightOrientation = (Orientation + 1) & 3; // turn right

        return TryCreatePiece(start, pieces, rng, wx, wy, wz, rightOrientation, Depth + 1, useRoomList);
    }

    // ── Piece selection (spec §3.2) ────────────────────────────────────────────

    private static StructurePiece? TryCreatePiece(
        StartingPiece start, List<StructurePiece> pieces,
        JavaRandom rng, int wx, int wy, int wz,
        int orientation, int depth, bool useRoomList)
    {
        // Radius guard (spec §3.2)
        if (Math.Abs(wx - start.BBox.MinX) > 112 || Math.Abs(wz - start.BBox.MinZ) > 112)
            return CreateDeadEnd(wx, wy, wz, orientation, depth);

        // Depth guard (spec §3.2)
        if (depth > 30)
            return CreateDeadEnd(wx, wy, wz, orientation, depth);

        List<PieceEntry> list = useRoomList ? start.RoomList : start.CorridorList;

        // Weighted piece selection (spec §3.2, up to 5 attempts)
        for (int attempt = 0; attempt < 5; attempt++)
        {
            int totalWeight = 0;
            foreach (var entry in list)
                if (entry.Max == 0 || entry.Count < entry.Max)
                    totalWeight += entry.Weight;

            if (totalWeight <= 0) break;

            int roll = rng.NextInt(totalWeight);
            foreach (var entry in list)
            {
                if (entry.Max != 0 && entry.Count >= entry.Max) continue;
                roll -= entry.Weight;
                if (roll < 0)
                {
                    if (!entry.IsTerminator && entry == start.LastEntry) continue;

                    StructurePiece? piece = entry.Create(wx, wy, wz, orientation, depth);
                    if (piece == null) break;

                    // Reject if it overlaps an existing piece
                    bool overlap = false;
                    foreach (var existing in pieces)
                        if (existing.BBox.Intersects(piece.BBox)) { overlap = true; break; }
                    if (overlap) break;

                    entry.Count++;
                    start.LastEntry = entry;
                    pieces.Add(piece);
                    start.Pending.Add(piece);
                    return piece;
                }
            }
        }

        return CreateDeadEnd(wx, wy, wz, orientation, depth);
    }

    private static DeadEnd CreateDeadEnd(int wx, int wy, int wz, int orientation, int depth)
    {
        var de = new DeadEnd(wx, wy, wz, orientation, depth);
        return de;
    }
}
