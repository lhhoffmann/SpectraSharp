namespace SpectraEngine.Core.Mods;

/// <summary>
/// An item together with a count and damage value.
/// Mirrors Java's ItemStack — used in drop lists, recipes, inventories.
/// </summary>
public readonly record struct ItemStack(int ItemId, int Count, int Damage = 0)
{
    public static readonly ItemStack Empty = new(0, 0);
    public bool IsEmpty => ItemId == 0 || Count <= 0;
    public int Id => ItemId;  // C#-style alias for tests and mod stubs
}
