namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>pe</c> (ItemRecord) — music disc item.
/// When used on an empty jukebox, inserts the disc and broadcasts world event 1005
/// to nearby clients so they begin playing the corresponding music track.
///
/// Fields:
///   <see cref="DiscName"/> (obf: a) — short disc name e.g. "13", "cat", "blocks".
///   MaxStackSize is overridden to 1 — records do not stack.
///
/// 11 disc instances exist in 1.0: IDs 2256–2266. "wait" is absent in 1.0.
///
/// Quirks preserved (spec §7):
///   7.3 — No "wait" disc (ID 2267) in 1.0.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemRecord_Jukebox_Spec.md §4
/// </summary>
public sealed class ItemRecord : Item
{
    /// <summary>
    /// obf: <c>a</c> — short disc name: "13", "cat", "blocks", etc.
    /// Used for tooltip ("C418 - &lt;name&gt;") and sound resource routing.
    /// </summary>
    public readonly string DiscName;

    /// <summary>
    /// Spec §2: constructor sets MaxStackSize=1 and registers at ItemsList[256 + id].
    /// obf: <c>pe(int id, String name)</c>.
    /// </summary>
    public ItemRecord(int id, string discName) : base(id)
    {
        DiscName     = discName;
        MaxStackSize = 1; // spec §2: bN overridden to 1
        SetUnlocalizedName("record_" + discName);
    }

    // ── onItemUse (spec §4.1) ─────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(dk, vi, ry, int, int, int, int)</c> — right-click on a block.
    /// Inserts this disc into an empty jukebox at (x,y,z) and broadcasts event 1005.
    /// Returns true if the disc was consumed.
    /// Spec §4.1.
    /// </summary>
    public override bool OnItemUse(ItemStack stack, object player, World world, int x, int y, int z, int face)
    {
        // Step 1: target must be the jukebox block (ID 84)
        if (world.GetBlockId(x, y, z) != 84) return false;

        // Step 2: jukebox must be empty (metadata == 0)
        if (world.GetBlockMetadata(x, y, z) != 0) return false;

        // Step 3: client side — acknowledge but do not act
        if (world.IsClientSide) return true;

        // Step 4: insert the disc via the BlockJukebox method
        if (Block.BlocksList[84] is not BlockJukebox jukebox) return false;
        jukebox.InsertRecord(world, x, y, z, RegistryIndex);

        // Step 5: broadcast event 1005 with this disc's registry index as data
        world.PlayAuxSFX(null, 1005, x, y, z, RegistryIndex);

        // Step 6: consume one disc from the stack
        stack.StackSize--;

        return true;
    }

    // ── addInformation (spec §4.2) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(dk, List)</c> — appends "C418 - &lt;DiscName&gt;" to the item tooltip.
    /// Spec §4.2.
    /// </summary>
    public void AddInformation(ItemStack stack, System.Collections.Generic.List<string> tooltip)
        => tooltip.Add("C418 - " + DiscName);

    // ── getRarity (spec §4.3) — RARE → aqua tooltip colour ───────────────────

    /// <summary>
    /// obf: <c>d(dk)</c> — returns RARE rarity so the item name renders in aqua.
    /// Spec §4.3.
    /// </summary>
    public ItemRarity GetRarity() => ItemRarity.Rare;
}
