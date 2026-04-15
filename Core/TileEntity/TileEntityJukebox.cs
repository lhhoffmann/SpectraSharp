namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Jukebox tile entity. Replica of <c>agc</c>. NBT registry key: "RecordPlayer".
///
/// Stores the item ID of the currently-loaded record as a single integer.
/// Field <see cref="RecordId"/> == 0 means the jukebox is empty.
///
/// NBT: "Record" (int tag) — only written when RecordId &gt; 0.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemRecord_Jukebox_Spec.md §2, §4.11, §4.12
/// </summary>
public sealed class TileEntityJukebox : TileEntity
{
    /// <summary>
    /// obf: <c>a</c> — item ID of the currently-loaded record (0 = empty).
    /// Written to and read from NBT tag "Record".
    /// </summary>
    public int RecordId;

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
    {
        // Spec §4.12: only write the tag when a disc is present (quirk 7.2 preservation)
        if (RecordId > 0)
            tag.PutInt("Record", RecordId);
    }

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        // Spec §4.11: absent tag returns 0 (default int) — field stays 0 = empty
        RecordId = tag.GetInt("Record");
    }
}
