using SpectraEngine.Core.Enchantments;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ahk</c> (ContainerEnchantment) — server-side logic for the enchanting
/// table interaction: bookshelf counting, slot-level calculation, and enchantment application.
///
/// Structure:
///   Slot 0       — enchantment input (single item)
///   Slots 1–36  — player inventory (3×9 + hotbar); not managed here
///   c[3]        — level costs for the three option slots
///   b (long)    — random seed sent to client for visual display
///   l (Random)  — per-container RNG (advances on each item change)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §5
/// </summary>
public sealed class ContainerEnchantment
{
    // ── obf field names ────────────────────────────────────────────────────────

    /// <summary>obf: <c>c[3]</c> — level cost for each of the three option slots.</summary>
    public readonly int[] SlotLevels = new int[3]; // c[3]

    /// <summary>obf: <c>b</c> — seed used by client for visual enchantment display.</summary>
    public long ClientSeed; // b

    /// <summary>obf: <c>l</c> — per-container RNG. Shared across seed and slot-level calls.</summary>
    private readonly JavaRandom _rng = new(); // l

    // ── Attached context ───────────────────────────────────────────────────────

    private readonly IWorld _world;
    private readonly int _x, _y, _z;    // table block position

    // ── Input slot ────────────────────────────────────────────────────────────

    /// <summary>The item currently placed in the enchanting input slot (slot 0).</summary>
    public ItemStack? InputItem;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ContainerEnchantment(IWorld world, int x, int y, int z)
    {
        _world = world;
        _x = x; _y = y; _z = z;
    }

    // ── Slot 0 change trigger (spec §5 — a(de)) ───────────────────────────────

    /// <summary>
    /// Called when the item in slot 0 changes.
    /// Refreshes the seed and recalculates all three slot levels.
    /// obf: <c>a(de)</c>.
    /// </summary>
    public void OnInputChanged()
    {
        if (InputItem == null || !CanEnchant(InputItem))
        {
            SlotLevels[0] = SlotLevels[1] = SlotLevels[2] = 0;
            return;
        }

        // Refresh seed (spec §5)
        ClientSeed = _rng.NextLong();

        // Count bookshelves (spec §5 bookshelf counting)
        int shelves = 0;

        if (_world is World conWorld && !conWorld.IsClientSide)
        {
            for (int var5 = -1; var5 <= 1; var5++)   // X offset (−1 / 0 / +1)
            {
                for (int var4 = -1; var4 <= 1; var4++) // Z offset (−1 / 0 / +1)
                {
                    if (var5 == 0 && var4 == 0) continue; // skip center

                    // Gap must be clear at both y and y+1
                    bool gapClear = _world.GetBlockId(_x + var5, _y, _z + var4) == 0
                                 && _world.GetBlockId(_x + var5, _y + 1, _z + var4) == 0;
                    if (!gapClear) continue;

                    // Primary bookshelf check at ×2 distance
                    if (_world.GetBlockId(_x + var5 * 2, _y, _z + var4 * 2) == 47) shelves++;
                    if (_world.GetBlockId(_x + var5 * 2, _y + 1, _z + var4 * 2) == 47) shelves++;

                    // Diagonal extra checks (spec §5)
                    if (var5 != 0 && var4 != 0)
                    {
                        if (_world.GetBlockId(_x + var5 * 2, _y, _z + var4) == 47) shelves++;
                        if (_world.GetBlockId(_x + var5 * 2, _y + 1, _z + var4) == 47) shelves++;
                        if (_world.GetBlockId(_x + var5, _y, _z + var4 * 2) == 47) shelves++;
                        if (_world.GetBlockId(_x + var5, _y + 1, _z + var4 * 2) == 47) shelves++;
                    }
                }
            }
        }

        // Calculate slot levels via EnchantmentHelper (spec §5 + §6)
        for (int slot = 0; slot < 3; slot++)
            SlotLevels[slot] = EnchantmentHelper.SlotLevel(_rng, slot, shelves, InputItem);
    }

    // ── Enchanting action (spec §5 — a(vi player, int slotIndex)) ────────────

    /// <summary>
    /// Attempts to enchant the item in slot 0 with the chosen option.
    /// Deducts levels from the player and applies enchantments to the item's NBT.
    /// obf: <c>a(vi player, int slotIndex)</c>.
    /// Returns true if enchanting was performed.
    /// </summary>
    public bool Enchant(EntityPlayer player, int slotIndex)
    {
        if (InputItem == null) return false;
        if (SlotLevels[slotIndex] <= 0) return false;
        if (player.XpLevel < SlotLevels[slotIndex]) return false;

        // Table must still be ID 116 and player within 8 blocks (spec §5)
        if (_world.GetBlockId(_x, _y, _z) != 116) return false;
        double dx = player.PosX - (_x + 0.5);
        double dy = player.PosY - (_y + 0.5);
        double dz = player.PosZ - (_z + 0.5);
        if (dx * dx + dy * dy + dz * dz > 64.0) return false;

        // Select enchantments via weighted RNG (spec §6)
        var enchantments = EnchantmentHelper.SelectEnchantments(_rng, InputItem, SlotLevels[slotIndex]);

        // Deduct levels (spec §5)
        player.DeductLevels(SlotLevels[slotIndex]);

        // Apply enchantments to item NBT (spec §8)
        foreach (var data in enchantments)
            InputItem.AddEnchantment(data.Enchantment, data.Level);

        // Refresh display after enchanting
        OnInputChanged();

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>dk.t()</c> — item is valid for enchanting: enchantable type AND not already enchanted.
    /// </summary>
    private static bool CanEnchant(ItemStack stack)
        => (stack.GetItem()?.GetEnchantability() ?? 0) > 0 && !stack.HasEnchantments();
}
