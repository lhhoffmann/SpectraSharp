namespace SpectraEngine.Core.Mods;

/// <summary>
/// Core.Item subclass that wraps an <see cref="IModItemBehavior"/> stub.
/// Created via <see cref="TryCreate"/> to guard the registry slot before
/// calling the base constructor (which auto-registers at ItemsList[256 + id]).
/// </summary>
public sealed class ModItemBridge : Item
{
    readonly IModItemBehavior _stub;

    // Private: always go through TryCreate.
    ModItemBridge(int id, IModItemBehavior stub) : base(id)
    {
        _stub = stub;
        SetMaxStackSize(stub.MaxStackSize);
        SetIconIndex(stub.IconIndex);
        if (stub.MaxDamage > 0) SetInternalDurability(stub.MaxDamage);
    }

    /// <summary>
    /// Registers a mod item at <c>ItemsList[256 + id]</c>.
    /// Returns null if the ID is invalid or the slot is already occupied by a vanilla item.
    /// Mod-vs-mod conflicts are logged and skipped.
    /// </summary>
    public static ModItemBridge? TryCreate(int id, IModItemBehavior stub)
    {
        if (id <= 0)
        {
            Console.Error.WriteLine($"[ModItemBridge] ID {id} invalid (must be > 0)");
            return null;
        }

        int slot = 256 + id;
        if (slot >= ItemsList.Length)
        {
            Console.Error.WriteLine($"[ModItemBridge] ID {id} out of range (slot {slot} >= {ItemsList.Length})");
            return null;
        }

        if (ItemsList[slot] != null && ItemsList[slot] is not ModItemBridge)
        {
            Console.Error.WriteLine($"[ModItemBridge] Slot {slot} occupied by vanilla item — skipping {stub.GetType().Name}");
            return null;
        }

        var bridge = new ModItemBridge(id, stub);
        Console.WriteLine($"[ModItemBridge] Registered mod item id={id} slot={slot} ({stub.GetType().Name})");
        return bridge;
    }

    /// <summary>
    /// Registers a mod item by string registry name (v1_12 / v1_16 style).
    /// Allocates a numeric ID via <see cref="ModNameRegistry"/>, then delegates to
    /// <see cref="TryCreate(int, IModItemBehavior)"/>.
    /// </summary>
    public static ModItemBridge? TryCreate(string registryName, IModItemBehavior stub)
    {
        int id = ModNameRegistry.GetOrAllocateItemId(registryName);
        if (id < 0) return null;
        return TryCreate(id, stub);
    }
}
