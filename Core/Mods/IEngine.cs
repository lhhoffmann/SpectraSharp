namespace SpectraEngine.Core.Mods;

/// <summary>
/// Engine handle passed to ISpectraMod.OnLoad().
/// Gives mods access to registries without touching engine internals.
/// </summary>
public interface IEngine
{
    IWorld           World     { get; }
    IModRegistry     Registry  { get; }
    ICraftingManager Crafting  { get; }
    ISmeltingManager Smelting  { get; }
}
