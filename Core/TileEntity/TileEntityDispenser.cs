namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Dispenser tile entity. Replica of <c>bp</c> (TileEntityDispenser).
/// Block ID: 23. NBT registry string: "Trap".
///
/// 9-slot inventory (3×3 grid). Dispense logic triggered by redstone — not by own tick.
/// No server-side tick override.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §7
/// </summary>
public sealed class TileEntityDispenser : TileEntity
{
    private const int SlotCount = 9;

    /// <summary>obf: <c>a</c> — 9-slot inventory.</summary>
    public readonly ItemStack?[] Slots = new ItemStack?[SlotCount];

    // RNG for picking which slot to dispense from (obf: b). Not persisted.
    private readonly JavaRandom _random = new();

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
        => WriteSlots(tag, Slots, SlotCount);

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
        => ReadSlots(tag, Slots, unsignedSlot: true);

    /// <summary>
    /// Returns a random non-null slot index for dispensing, or -1 if all slots are empty.
    /// Not called from NBT code — included for completeness.
    /// </summary>
    public int PickRandomSlot()
    {
        var nonNull = new System.Collections.Generic.List<int>();
        for (int i = 0; i < SlotCount; i++)
            if (Slots[i] != null) nonNull.Add(i);
        if (nonNull.Count == 0) return -1;
        return nonNull[_random.NextInt(nonNull.Count)];
    }
}
