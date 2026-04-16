namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Sign tile entity. Replica of <c>u</c> (TileEntitySign).
/// Block IDs: 63 (wall sign), 68 (standing sign). NBT registry string: "Sign".
///
/// Quirks preserved (spec §8.3 / §12):
///   1. Text is truncated to 15 characters on read, not on write.
///   2. IsEditable (j) is set to false at the start of ReadFromNbt — before base read.
///      Any load makes the sign non-editable.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §8
/// </summary>
public sealed class TileEntitySign : TileEntity
{
    private const int MaxLineLength = 15;

    /// <summary>obf: <c>a</c> — four text lines.</summary>
    public readonly string[] Lines = { "", "", "", "" };

    /// <summary>obf: <c>b</c> — edit state: index of editing player, or -1 for none.</summary>
    public int EditingPlayer = -1;

    /// <summary>
    /// obf: <c>j</c> — editable flag. True by default; set false on first NBT load (quirk 2).
    /// </summary>
    public bool IsEditable = true;

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
    {
        tag.PutString("Text1", Lines[0]);
        tag.PutString("Text2", Lines[1]);
        tag.PutString("Text3", Lines[2]);
        tag.PutString("Text4", Lines[3]);
    }

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        IsEditable = false; // quirk 2: set before base coords are read (already called by parent)

        Lines[0] = Truncate(tag.GetString("Text1"));
        Lines[1] = Truncate(tag.GetString("Text2"));
        Lines[2] = Truncate(tag.GetString("Text3"));
        Lines[3] = Truncate(tag.GetString("Text4"));
    }

    private static string Truncate(string s)
        => s.Length > MaxLineLength ? s[..MaxLineLength] : s;
}
