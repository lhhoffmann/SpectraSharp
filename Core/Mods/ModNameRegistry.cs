namespace SpectraEngine.Core.Mods;

/// <summary>
/// Maps string registry names (e.g. "mymod:myblock") to numeric Core IDs.
/// Used by v1_12 and v1_16 stubs where mods register by name, not numeric ID.
///
/// Block IDs are allocated from the mod-reserved range 200–255.
/// Item IDs are allocated from 5000–31743 (above vanilla item range).
/// Allocations are in-memory only — IDs are re-assigned on each restart.
/// </summary>
public static class ModNameRegistry
{
    // ── Block ID allocation ───────────────────────────────────────────────────

    const int BlockModMin = 200;
    const int BlockModMax = 255;

    static int _nextBlockId = BlockModMin;
    static readonly Dictionary<string, int> _blockNames = [];

    /// <summary>
    /// Returns the numeric block ID for <paramref name="registryName"/>,
    /// allocating a new one if it has not been seen before.
    /// Returns -1 if the mod-reserved block ID range is exhausted.
    /// </summary>
    public static int GetOrAllocateBlockId(string registryName)
    {
        if (_blockNames.TryGetValue(registryName, out int existing))
            return existing;

        if (_nextBlockId > BlockModMax)
        {
            Console.Error.WriteLine(
                $"[ModNameRegistry] Block ID range {BlockModMin}-{BlockModMax} exhausted " +
                $"— cannot register '{registryName}'");
            return -1;
        }

        int id = _nextBlockId++;
        _blockNames[registryName] = id;
        Console.WriteLine($"[ModNameRegistry] Allocated block id={id} for '{registryName}'");
        return id;
    }

    /// <summary>Looks up a previously allocated block ID. Returns -1 if not found.</summary>
    public static int GetBlockId(string registryName)
        => _blockNames.TryGetValue(registryName, out int id) ? id : -1;

    // ── Item ID allocation ────────────────────────────────────────────────────

    const int ItemModMin = 5000;
    const int ItemModMax = 31743;

    static int _nextItemId = ItemModMin;
    static readonly Dictionary<string, int> _itemNames = [];

    /// <summary>
    /// Returns the numeric item ID for <paramref name="registryName"/>,
    /// allocating a new one if it has not been seen before.
    /// Returns -1 if the mod-reserved item ID range is exhausted.
    /// </summary>
    public static int GetOrAllocateItemId(string registryName)
    {
        if (_itemNames.TryGetValue(registryName, out int existing))
            return existing;

        if (_nextItemId > ItemModMax)
        {
            Console.Error.WriteLine(
                $"[ModNameRegistry] Item ID range {ItemModMin}-{ItemModMax} exhausted " +
                $"— cannot register '{registryName}'");
            return -1;
        }

        int id = _nextItemId++;
        _itemNames[registryName] = id;
        Console.WriteLine($"[ModNameRegistry] Allocated item id={id} for '{registryName}'");
        return id;
    }

    /// <summary>Looks up a previously allocated item ID. Returns -1 if not found.</summary>
    public static int GetItemId(string registryName)
        => _itemNames.TryGetValue(registryName, out int id) ? id : -1;
}
