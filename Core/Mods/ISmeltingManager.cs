namespace SpectraEngine.Core.Mods;

/// <summary>
/// Allows mods to register furnace smelting recipes at load time.
/// </summary>
public interface ISmeltingManager
{
    /// <summary>
    /// Registers a smelting recipe.
    /// </summary>
    /// <param name="inputId">Item/block ID consumed as fuel input.</param>
    /// <param name="output">The item produced.</param>
    /// <param name="xp">Experience points awarded per smelt operation.</param>
    void AddSmeltingRecipe(int inputId, ItemStack output, float xp);
}
