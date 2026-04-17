namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Smelting furnace tile entity. Replica of <c>oe</c> (TileEntityFurnace).
///
/// Block IDs: 61 = furnace off, 62 = furnace on.
/// Slots: 0 = input, 1 = fuel, 2 = output.
/// Tick logic: burn-down → re-fuel → cook progress → lit/unlit block swap.
///
/// Quirks preserved:
///   1. cookTime resets to 0 on any tick where the furnace is not burning or cannot smelt.
///   2. On load, currentItemBurnTime (b) is recomputed from the current fuel slot.
///   3. Block swaps between 61 and 62 using world.SetBlock at the TE's coordinates.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §5
/// </summary>
public sealed class TileEntityFurnace : TileEntity, IInventory
{
    private const int SlotCount  = 3;
    private const int CookTarget = 200;

    private const int BlockFurnaceOff = 61;
    private const int BlockFurnaceOn  = 62;

    // ── Fields (spec §5.1) ────────────────────────────────────────────────────

    public readonly ItemStack?[] Slots = new ItemStack?[SlotCount]; // obf: k

    private int _burnTime;            // obf: a — ticks of fuel remaining
    private int _currentItemBurnTime; // obf: b — total ticks of last fuel (for UI bar)
    private int _cookTime;            // obf: j — ticks current input has been cooking

    // Public accessors for ContainerFurnace data sync (spec §6.2-6.3)
    public int BurnTime        { get => _burnTime;            set => _burnTime            = value; }
    public int CurrentBurnTime { get => _currentItemBurnTime; set => _currentItemBurnTime = value; }
    public int CookTime        { get => _cookTime;            set => _cookTime            = value; }

    // ── NBT (spec §5.2) ───────────────────────────────────────────────────────

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
    {
        tag.PutShort("BurnTime", (short)_burnTime);
        tag.PutShort("CookTime", (short)_cookTime);
        WriteSlots(tag, Slots, SlotCount);
    }

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        ReadSlots(tag, Slots, unsignedSlot: false); // furnace: signed byte slot (spec §4 quirk)
        _burnTime = tag.GetShort("BurnTime");
        _cookTime = tag.GetShort("CookTime");
        // Recompute currentItemBurnTime from whatever is in slot 1 (quirk 2)
        _currentItemBurnTime = GetFuelValue(Slots[1]);
    }

    // ── Tick logic (spec §5.3) ────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null || World.IsClientSide) return;

        bool wasBurning = _burnTime > 0;
        bool changed    = false;

        // Burn-down
        if (_burnTime > 0) _burnTime--;

        // Re-fuel if burn ran out and we can smelt
        if (_burnTime == 0 && CanSmelt())
        {
            _currentItemBurnTime = _burnTime = GetFuelValue(Slots[1]);
            if (_burnTime > 0)
            {
                changed = true;
                if (Slots[1] != null)
                {
                    Slots[1]!.StackSize--;
                    if (Slots[1]!.StackSize <= 0) Slots[1] = null;
                }
            }
        }

        // Cook progress
        if (_burnTime > 0 && CanSmelt())
        {
            _cookTime++;
            if (_cookTime >= CookTarget)
            {
                _cookTime = 0;
                SmeltItem();
                changed = true;
            }
        }
        else
        {
            _cookTime = 0; // cook resets when not active (quirk 1)
        }

        // Lit / unlit block swap — routes through BlockFurnace.SetLitState (spec §2.7)
        bool isBurning = _burnTime > 0;
        if (wasBurning != isBurning)
        {
            changed = true;
            BlockFurnace.SetLitState(World, X, Y, Z, isBurning);
        }

        if (changed) MarkDirty();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private bool CanSmelt()
    {
        if (Slots[0] == null) return false;
        ItemStack? recipe = FurnaceRecipes.Instance.GetSmeltingResult(Slots[0]!.ItemId);
        if (recipe == null) return false;
        if (Slots[2] == null) return true;
        if (Slots[2]!.ItemId != recipe.ItemId) return false;
        return Slots[2]!.StackSize < Slots[2]!.GetMaxStackSize()
            && Slots[2]!.StackSize < recipe.GetMaxStackSize();
    }

    private void SmeltItem()
    {
        ItemStack? recipe = FurnaceRecipes.Instance.GetSmeltingResult(Slots[0]!.ItemId);
        if (recipe == null) return;

        if (Slots[2] == null) Slots[2] = recipe.Copy();
        else                  Slots[2]!.StackSize++;

        Slots[0]!.StackSize--;
        if (Slots[0]!.StackSize <= 0) Slots[0] = null;
    }

    private static int GetFuelValue(ItemStack? stack)
    {
        if (stack == null) return 0;
        int id = stack.ItemId;

        // Wooden blocks: any block with wood material (material == Material.Plants = p.d)
        if (id < 256)
        {
            var blk = Block.BlocksList[id];
            if (blk?.BlockMaterial == Material.Plants) return 300;
        }

        // Pure item fuel values (spec §5.4)
        // Sticks = item 280 (256+24, from acy.C=24)
        if (id == 280) return 100;  // sticks
        if (id == 263) return 1600; // coal (damage 0); charcoal (damage 1) also 1600 per spec? — same item
        // Lava bucket = item id TBD (using 327, the standard MC lava bucket ID)
        if (id == 327) return 20000;
        // Sapling = block 6
        if (id == 6)   return 100;
        // Blaze rod = item (using 369, the standard MC blaze rod ID)
        if (id == 369) return 2400;

        return 0;
    }

    // ── IInventory ────────────────────────────────────────────────────────────

    public int GetSizeInventory() => SlotCount;

    public ItemStack? GetStackInSlot(int slot) => Slots[slot];

    public ItemStack? DecrStackSize(int slot, int count)
    {
        if (Slots[slot] == null) return null;
        if (Slots[slot]!.StackSize <= count)
        {
            var s = Slots[slot]; Slots[slot] = null; MarkDirty(); return s;
        }
        var split = Slots[slot]!.SplitStack(count);
        MarkDirty();
        return split;
    }

    public void SetInventorySlotContents(int slot, ItemStack? stack)
    {
        Slots[slot] = stack;
        if (stack != null && stack.StackSize > GetInventoryStackLimit())
            stack.StackSize = GetInventoryStackLimit();
        MarkDirty();
    }

    public string GetInvName()             => "container.furnace";
    public int    GetInventoryStackLimit() => 64;
    public void   OnInventoryChanged()     => MarkDirty();

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
