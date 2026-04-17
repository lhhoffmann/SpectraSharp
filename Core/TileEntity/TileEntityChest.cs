namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Chest tile entity. Replica of <c>tu</c> (TileEntityChest).
///
/// Holds 27 item slots (indices 0–26). Implements <see cref="IInventory"/> so it can be
/// passed directly to <see cref="EntityPlayer.OpenInventory"/> and wrapped in
/// <see cref="InventoryLargeChest"/> for double chests.
///
/// No server-side tick logic.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §6
/// </summary>
public sealed class TileEntityChest : TileEntity, IInventory
{
    private const int SlotCount = 27;

    /// <summary>obf: <c>p</c> — inventory array (27 slots).</summary>
    public readonly ItemStack?[] Slots = new ItemStack?[SlotCount];

    // ── IInventory ────────────────────────────────────────────────────────────

    public int GetSizeInventory() => SlotCount;

    public ItemStack? GetStackInSlot(int slot) => Slots[slot];

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (Slots[slot] == null) return null;
        if (Slots[slot]!.StackSize <= count)
        {
            var stack = Slots[slot];
            Slots[slot] = null;
            OnInventoryChanged();
            return stack;
        }
        var split = Slots[slot]!.SplitStack(count);
        OnInventoryChanged();
        return split;
    }

    public void SetInventorySlotContents(int slot, ItemStack? stack)
    {
        Slots[slot] = stack;
        if (stack != null && stack.StackSize > GetInventoryStackLimit())
            stack.StackSize = GetInventoryStackLimit();
        OnInventoryChanged();
    }

    public string GetInvName() => "container.chest";

    public int GetInventoryStackLimit() => 64;

    public void OnInventoryChanged()
    {
        // Marks the tile entity dirty so it will be saved on next autosave.
        // Full client-sync handled by Container when implemented.
    }

    public bool IsUseableByPlayer(EntityPlayer player)
    {
        // Player must be within 8 blocks of chest centre (spec §9)
        if (World == null) return false;
        double dx = player.PosX - (X + 0.5);
        double dy = player.PosY - (Y + 0.5);
        double dz = player.PosZ - (Z + 0.5);
        return dx * dx + dy * dy + dz * dz < 64.0; // 8 blocks²
    }

    /// <summary>Increments numPlayersUsing for lid animation. Spec: <c>tu.j()</c>.</summary>
    public void OpenChest()  { /* lid animation counter — Graphics concern */ }

    /// <summary>Decrements numPlayersUsing for lid animation. Spec: <c>tu.k()</c>.</summary>
    public void CloseChest() { /* lid animation counter — Graphics concern */ }

    // ── NBT ───────────────────────────────────────────────────────────────────

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
        => WriteSlots(tag, Slots, SlotCount);

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
        => ReadSlots(tag, Slots, unsignedSlot: true);
}
