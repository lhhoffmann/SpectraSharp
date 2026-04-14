namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Chest tile entity. Replica of <c>tu</c> (TileEntityChest).
///
/// Internal array is 36 slots but only indices 0–26 are saved (and accessible via IInventory).
/// Indices 27–35 are reserved for double-chest adjacent-chest linking; never written to NBT.
///
/// No server-side tick logic.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §6
/// </summary>
public sealed class TileEntityChest : TileEntity
{
    private const int SavedSlots  = 27; // 0–26 are serialized
    private const int TotalSlots  = 36; // full array (27–35 = double-chest scratch)

    /// <summary>obf: <c>p</c> — inventory array. Only 0–26 are accessible and saved.</summary>
    public readonly ItemStack?[] Slots = new ItemStack?[TotalSlots];

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
        => WriteSlots(tag, Slots, SavedSlots);

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
        => ReadSlots(tag, Slots, unsignedSlot: true);
}
