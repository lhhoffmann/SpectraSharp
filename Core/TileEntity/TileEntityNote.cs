namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Note block tile entity. Replica of <c>nj</c> (TileEntityNote).
/// Block ID: 25. NBT registry string: "Music".
///
/// Stores a pitch byte 0–24 (25 distinct pitches). Clamped to [0, 24] on load.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §10
/// </summary>
public sealed class TileEntityNote : TileEntity
{
    /// <summary>obf: <c>a</c> — note pitch 0–24.</summary>
    public byte Note;

    /// <summary>Cycles note to next pitch (0→…→24→0). Marks dirty. Spec: <c>a()</c>.</summary>
    public void IncrementNote()
    {
        Note = (byte)((Note + 1) % 25);
        MarkDirty();
    }

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
        => tag.PutByte("note", Note);

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        Note = tag.GetByte("note");
        if (Note > 24) Note = 24; // clamp (spec §10.2)
    }
}
