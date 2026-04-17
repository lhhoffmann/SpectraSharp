namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Stub tile entities that are registered in the factory but have no additional NBT
/// beyond the base id/x/y/z fields. See TileEntity_Spec §11.
/// </summary>

/// <summary>Brewing stand TE. obf: <c>tt</c>. Registry: "Cauldron".</summary>
public sealed class TileEntityBrewingStand : TileEntity, IInventory
{
    private const int SlotCount = 4; // 0=ingredient, 1-3=potion bottles

    public readonly ItemStack?[] Slots = new ItemStack?[SlotCount];

    // ── IInventory ────────────────────────────────────────────────────────────

    public int GetSizeInventory() => SlotCount;

    public ItemStack? GetStackInSlot(int slot) => Slots[slot];

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (Slots[slot] == null) return null;
        if (Slots[slot]!.StackSize <= count)
        {
            var s = Slots[slot]; Slots[slot] = null; OnInventoryChanged(); return s;
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

    public string GetInvName()             => "container.brewing";
    public int    GetInventoryStackLimit() => 64;
    public void   OnInventoryChanged()     { /* mark dirty */ }

    public bool IsUseableByPlayer(EntityPlayer player)
    {
        if (World == null) return false;
        double dx = player.PosX - (X + 0.5);
        double dy = player.PosY - (Y + 0.5);
        double dz = player.PosZ - (Z + 0.5);
        return dx * dx + dy * dy + dz * dz < 64.0;
    }

    public void OpenChest()  { }
    public void CloseChest() { }
}

/// <summary>End portal frame TE. obf: <c>yg</c>. Registry: "Airportal".</summary>
public sealed class TileEntityEndPortal : TileEntity { }
