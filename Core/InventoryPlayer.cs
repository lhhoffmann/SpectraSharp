namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>x</c> (InventoryPlayer) — the player's personal inventory.
/// Implements <see cref="IInventory"/> and is created inside <see cref="EntityPlayer"/>:
/// <code>public InventoryPlayer Inventory = new(this);</code>
///
/// Slot layout:
///   Slots  0–35  → <c>MainInventory[36]</c> (main items; 0–8 = hotbar)
///   Slots 36–39  → <c>ArmorInventory[4]</c>  (0=boots, 1=leggings, 2=chestplate, 3=helmet)
///
/// NBT armor-slot quirk: armor is serialised at indices 100–103, not 36–39.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/InventoryPlayer_Spec.md
/// </summary>
public sealed class InventoryPlayer : IInventory
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>a[36]</c> — main inventory; indices 0–8 are the hotbar.</summary>
    public readonly ItemStack?[] MainInventory = new ItemStack?[36];

    /// <summary>
    /// obf: <c>b[4]</c> — armor slots: 0=boots, 1=leggings, 2=chestplate, 3=helmet.
    /// </summary>
    public readonly ItemStack?[] ArmorInventory = new ItemStack?[4];

    /// <summary>obf: <c>c</c> — currently selected hotbar slot (0–8).</summary>
    public int CurrentItem;

    private readonly EntityPlayer _owner;

    // ── Constructor ───────────────────────────────────────────────────────────

    public InventoryPlayer(EntityPlayer owner)
    {
        _owner = owner;
    }

    // ── IInventory implementation (spec §3) ───────────────────────────────────

    /// <summary>obf: <c>c()</c> — 36 main + 4 armor = 40 total slots.</summary>
    public int GetSizeInventory() => 40;

    /// <summary>
    /// obf: <c>d(int slot)</c> — slots 0–35 → MainInventory; 36–39 → ArmorInventory.
    /// </summary>
    public ItemStack? GetStackInSlot(int slot)
        => slot < 36 ? MainInventory[slot] : ArmorInventory[slot - 36];

    /// <summary>
    /// obf: <c>a(int slot, int count)</c> — decrStackSize.
    /// See IInventory spec §3 for behaviour contract.
    /// </summary>
    public ItemStack? DecrStackSize(int slot, int count)
    {
        ItemStack?[] arr = slot < 36 ? MainInventory : ArmorInventory;
        int idx = slot < 36 ? slot : slot - 36;

        if (arr[idx] == null) return null;

        ItemStack stack = arr[idx]!;
        if (stack.StackSize <= count)
        {
            arr[idx] = null;
            OnInventoryChanged();
            return stack;
        }

        ItemStack split = stack.SplitStack(count);
        OnInventoryChanged();
        return split;
    }

    /// <summary>
    /// obf: <c>a(int slot, dk stack)</c> — setInventorySlotContents.
    /// Same 0–35 / 36–39 mapping as GetStackInSlot.
    /// </summary>
    public void SetInventorySlotContents(int slot, ItemStack? stack)
    {
        if (slot < 36)
            MainInventory[slot] = stack;
        else
            ArmorInventory[slot - 36] = stack;

        OnInventoryChanged();
    }

    /// <summary>obf: <c>d()</c> — display name.</summary>
    public string GetInvName() => "Inventory";

    /// <summary>obf: <c>e()</c> — maximum stack size = 64.</summary>
    public int GetInventoryStackLimit() => 64;

    /// <summary>obf: <c>h()</c> — no-op at the inventory level (no tile entity to sync).</summary>
    public void OnInventoryChanged() { }

    /// <summary>obf: <c>b_(vi)</c> — player always has access to own inventory.</summary>
    public bool IsUseableByPlayer(EntityPlayer player) => true;

    /// <summary>obf: <c>j()</c> — no-op.</summary>
    public void OpenChest() { }

    /// <summary>obf: <c>k()</c> — no-op.</summary>
    public void CloseChest() { }

    // ── Additional methods (spec §4) ──────────────────────────────────────────

    /// <summary>
    /// obf: <c>a()</c> — getStackInSelectedSlot.
    /// Returns the ItemStack in the currently selected hotbar slot.
    /// </summary>
    public ItemStack? GetStackInSelectedSlot() => MainInventory[CurrentItem];

    /// <summary>
    /// obf: <c>b()</c> — decrementAnimations.
    /// Decrements <c>UseTimer</c> on all main-slot stacks where it is > 0.
    /// Called each tick from EntityPlayer.
    /// </summary>
    public void DecrementAnimations()
    {
        for (int i = 0; i < MainInventory.Length; i++)
        {
            if (MainInventory[i] != null && MainInventory[i]!.UseTimer > 0)
                MainInventory[i]!.UseTimer--;
        }
    }

    /// <summary>
    /// obf: <c>g()</c> — dropAllItems. Called on player death.
    /// Drops every non-null stack from both arrays; nulls the slot afterwards.
    /// </summary>
    public void DropAllItems()
    {
        for (int i = 0; i < MainInventory.Length; i++)
        {
            if (MainInventory[i] != null)
            {
                _owner.DropItem(MainInventory[i]!, randomDirection: false);
                MainInventory[i] = null;
            }
        }

        for (int i = 0; i < ArmorInventory.Length; i++)
        {
            if (ArmorInventory[i] != null)
            {
                _owner.DropItem(ArmorInventory[i]!, randomDirection: false);
                ArmorInventory[i] = null;
            }
        }
    }

    /// <summary>
    /// obf: <c>a(dk, boolean)</c> — addItemStackToInventory.
    /// Tries to merge into existing partial stacks first, then fills empty slots.
    /// Returns true if the entire stack was absorbed.
    /// </summary>
    public bool AddItemStackToInventory(ItemStack incoming)
    {
        int maxStack = Math.Min(incoming.GetMaxStackSize(), GetInventoryStackLimit());

        // Pass 1: merge into existing partial stacks of the same item
        for (int i = 0; i < MainInventory.Length && incoming.StackSize > 0; i++)
        {
            ItemStack? slot = MainInventory[i];
            if (slot == null) continue;
            if (slot.ItemId != incoming.ItemId) continue;
            if (slot.GetMetadata() != incoming.GetMetadata()) continue;

            int space = maxStack - slot.StackSize;
            if (space <= 0) continue;

            int transfer = Math.Min(space, incoming.StackSize);
            slot.StackSize  += transfer;
            incoming.StackSize -= transfer;
            OnInventoryChanged();
        }

        // Pass 2: fill empty slots
        for (int i = 0; i < MainInventory.Length && incoming.StackSize > 0; i++)
        {
            if (MainInventory[i] != null) continue;
            MainInventory[i] = incoming.SplitStack(Math.Min(maxStack, incoming.StackSize));
            OnInventoryChanged();
        }

        return incoming.StackSize == 0;
    }

    // ── NBT serialization (spec: PlayerNBT_Spec §6) ───────────────────────────

    /// <summary>
    /// Writes all non-null inventory and armor slots into a TAG_List.
    /// Armor slots use NBT slot bytes 100–103 (spec §6.1).
    /// Spec: <c>x.a(yi list)</c>.
    /// </summary>
    public Nbt.NbtList WriteToNbt()
    {
        var list = new Nbt.NbtList();

        // Main inventory + hotbar (slots 0–35)
        for (int i = 0; i < MainInventory.Length; i++)
        {
            if (MainInventory[i] == null) continue;
            var entry = new Nbt.NbtCompound();
            entry.PutByte("Slot", (byte)i);
            MainInventory[i]!.SaveToNbt(entry);
            list.Add(entry);
        }

        // Armor slots (NBT bytes 100–103)
        for (int i = 0; i < ArmorInventory.Length; i++)
        {
            if (ArmorInventory[i] == null) continue;
            var entry = new Nbt.NbtCompound();
            entry.PutByte("Slot", (byte)(i + 100));
            ArmorInventory[i]!.SaveToNbt(entry);
            list.Add(entry);
        }

        return list;
    }

    /// <summary>
    /// Reads slot entries from a TAG_List, distributing to main (0–35) or armor (100–103).
    /// Slot bytes are read as unsigned (& 255). Other byte values are silently ignored.
    /// Spec: <c>x.b(yi list)</c>.
    /// </summary>
    public void ReadFromNbt(Nbt.NbtList? list)
    {
        if (list == null) return;

        // Reset both arrays
        System.Array.Clear(MainInventory);
        System.Array.Clear(ArmorInventory);

        for (int i = 0; i < list.Count; i++)
        {
            var entry = (Nbt.NbtCompound)list[i];
            int slot  = entry.GetByte("Slot") & 255;
            ItemStack? item = ItemStack.LoadFromNbt(entry);
            if (item == null) continue;

            if (slot >= 0 && slot < 36)
                MainInventory[slot] = item;
            else if (slot >= 100 && slot < 104)
                ArmorInventory[slot - 100] = item;
            // [36, 99] and [104, 255] silently ignored
        }
    }
}
