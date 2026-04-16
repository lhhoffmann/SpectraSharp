using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen.NetherFortress;

/// <summary>
/// Factory and metadata for a single piece type. Replica of <c>aaz</c>.
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §3.3
/// </summary>
internal sealed class PieceEntry
{
    public readonly int    Weight;         // obf: b
    public          int    Count;          // obf: c — resets to 0 per structure
    public readonly int    Max;            // obf: d — 0 = unlimited
    public readonly bool   IsTerminator;  // obf: e

    private readonly Func<int, int, int, int, int, StructurePiece?> _factory;

    public PieceEntry(int weight, int max, bool isTerminator,
        Func<int, int, int, int, int, StructurePiece?> factory)
    {
        Weight       = weight;
        Max          = max;
        IsTerminator = isTerminator;
        _factory     = factory;
    }

    public StructurePiece? Create(int wx, int wy, int wz, int orientation, int depth)
        => _factory(wx, wy, wz, orientation, depth);

    public PieceEntry Clone() => new(Weight, Max, IsTerminator, _factory) { Count = 0 };
}

/// <summary>
/// Holds the two static piece lists (corridor and room). Replica of <c>rp</c>.
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §3.4
/// </summary>
internal static class PieceRegistry
{
    // ── Corridor list (rp.a()) ────────────────────────────────────────────────

    private static readonly PieceEntry[] s_corridorEntries =
    [
        new(30, 0,  true,  (x,y,z,o,d) => new BridgeStraight       (x,y,z,o,d)),
        new(10, 4,  false, (x,y,z,o,d) => new BridgeCrossing        (x,y,z,o,d)),
        new(10, 4,  false, (x,y,z,o,d) => new BridgeCrossing3       (x,y,z,o,d)),
        new(10, 3,  false, (x,y,z,o,d) => new BridgeStaircase       (x,y,z,o,d)),
        new( 5, 2,  false, (x,y,z,o,d) => new BlazeSpawnerCorridor  (x,y,z,o,d)),
        new( 5, 1,  false, (x,y,z,o,d) => new FortressRoom          (x,y,z,o,d)),
    ];

    // ── Room list (rp.b()) ────────────────────────────────────────────────────

    private static readonly PieceEntry[] s_roomEntries =
    [
        new(25, 0,  true,  (x,y,z,o,d) => new RoomCrossing          (x,y,z,o,d)),
        new(15, 5,  false, (x,y,z,o,d) => new RoomCrossing3         (x,y,z,o,d)),
        new( 5, 10, false, (x,y,z,o,d) => new RoomCrossingRight     (x,y,z,o,d)),
        new( 5, 10, false, (x,y,z,o,d) => new RoomCrossingLeft      (x,y,z,o,d)),
        new(10, 3,  true,  (x,y,z,o,d) => new StaircaseDown         (x,y,z,o,d)),
        new( 7, 2,  false, (x,y,z,o,d) => new CorridorRoofed        (x,y,z,o,d)),
        new( 5, 2,  false, (x,y,z,o,d) => new NetherWartRoom        (x,y,z,o,d)),
    ];

    /// <summary>Returns a fresh corridor list with all counts reset to 0.</summary>
    public static List<PieceEntry> CreateCorridorList()
        => s_corridorEntries.Select(e => e.Clone()).ToList();

    /// <summary>Returns a fresh room list with all counts reset to 0.</summary>
    public static List<PieceEntry> CreateRoomList()
        => s_roomEntries.Select(e => e.Clone()).ToList();
}
