namespace SpectraSharp.Core.Mods;

/// <summary>
/// Allows mods to register crafting recipes at load time.
/// Shaped recipes use a 3x3 pattern string; shapeless recipes use an ingredient list.
/// </summary>
public interface ICraftingManager
{
    /// <summary>
    /// Registers a shaped crafting recipe.
    /// </summary>
    /// <param name="output">The item produced.</param>
    /// <param name="pattern">
    /// Three strings of up to three characters each, e.g. {"###", "# #", "###"}.
    /// Each character maps to an ingredient via <paramref name="key"/>.
    /// </param>
    /// <param name="key">Maps pattern characters to item IDs.</param>
    void AddShapedRecipe(ItemStack output, string[] pattern, Dictionary<char, int> key);

    /// <summary>
    /// Registers a shapeless crafting recipe (ingredient order does not matter).
    /// </summary>
    /// <param name="output">The item produced.</param>
    /// <param name="ingredients">Item IDs of every required ingredient.</param>
    void AddShapelessRecipe(ItemStack output, int[] ingredients);
}
