using SpectraSharp.Core.WorldGen.Structure;

namespace SpectraSharp.Core.WorldGen.NetherFortress;

/// <summary>
/// The root piece of every Nether Fortress. Replica of <c>gc</c> (StartingPiece).
///
/// Extends BridgeCrossing (same 19×10×19 geometry). Owns:
/// - CorridorList / RoomList: fresh per-structure copies with all counts=0.
/// - Pending: list of pieces waiting to expand their exits.
/// - LastEntry: last chosen piece entry (to avoid consecutive identical pieces unless terminator).
///
/// Construction: placed at (chunkX*16+2, 64, chunkZ*16+2), random orientation (0-3).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §5
/// </summary>
internal sealed class StartingPiece : BridgeCrossing
{
    public readonly List<PieceEntry>      CorridorList;
    public readonly List<PieceEntry>      RoomList;
    public readonly List<StructurePiece>  Pending = [];   // obf: gc.d
    public          PieceEntry?           LastEntry;      // obf: gc.a

    public StartingPiece(int originX, int originZ, int orientation, int depth)
        : base(originX, 64, originZ, orientation, depth)
    {
        CorridorList = PieceRegistry.CreateCorridorList();
        RoomList     = PieceRegistry.CreateRoomList();
    }
}
