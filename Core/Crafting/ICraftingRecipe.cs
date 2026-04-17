namespace SpectraEngine.Core.Crafting;

/// <summary>
/// Replica of <c>ue</c> — the crafting recipe interface.
/// Implemented by <see cref="VanillaShapedRecipe"/> and <see cref="VanillaShapelessRecipe"/>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/CraftingRecipes_Spec.md §1.1
/// </summary>
public interface ICraftingRecipe
{
    /// <summary>
    /// obf: <c>a(lm grid)</c> — returns true if this recipe matches the current grid contents.
    /// </summary>
    bool Matches(CraftingGrid grid);

    /// <summary>
    /// obf: <c>b(lm grid)</c> — returns the output ItemStack for this recipe.
    /// The returned stack is a prototype; callers must copy before mutating.
    /// </summary>
    ItemStack GetResult();
}
