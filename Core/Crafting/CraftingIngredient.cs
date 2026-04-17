namespace SpectraEngine.Core.Crafting;

/// <summary>
/// A single ingredient slot in a crafting recipe.
/// Matches against an <see cref="ItemStack"/> in the crafting grid.
///
/// Corresponds to the vanilla approach of using <c>Item</c>, <c>Block</c>, or
/// <c>ItemStack</c> as ingredient references — mapped here as item registry ID + optional
/// exact damage.
///
/// <paramref name="ItemId"/> -1 means empty (air).
/// <paramref name="Damage"/> -1 means any damage / metadata accepted.
/// </summary>
public readonly record struct CraftingIngredient(int ItemId, int Damage = -1)
{
    /// <summary>Empty slot (air). Must be empty in the crafting grid to match.</summary>
    public static readonly CraftingIngredient Empty = new(-1, -1);

    /// <summary>True when this slot represents an empty / air requirement.</summary>
    public bool IsEmpty => ItemId < 0;

    /// <summary>
    /// Returns true if <paramref name="stack"/> satisfies this ingredient requirement.
    /// </summary>
    public bool Matches(ItemStack? stack)
    {
        if (IsEmpty) return stack == null || stack.StackSize <= 0;
        if (stack == null || stack.StackSize <= 0) return false;
        if (stack.ItemId != ItemId) return false;
        if (Damage >= 0 && stack.Damage != Damage) return false;
        return true;
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    /// <summary>Any-damage ingredient matching only by item ID (includes block items).</summary>
    public static CraftingIngredient Any(int itemId) => new(itemId, -1);

    /// <summary>Exact ingredient matching by both item ID and damage/meta.</summary>
    public static CraftingIngredient Exact(int itemId, int damage) => new(itemId, damage);
}
