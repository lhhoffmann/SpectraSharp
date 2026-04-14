namespace SpectraSharp.Core.Mods;

/// <summary>
/// Concrete implementation of <see cref="IEngine"/> passed to every mod's
/// <see cref="ISpectraMod.OnLoad"/> callback. Provides safe, interface-bounded
/// access to the world and mod registries without exposing engine internals.
/// </summary>
public sealed class EngineHandle(
    IWorld          world,
    IModRegistry    registry,
    ICraftingManager crafting,
    ISmeltingManager smelting) : IEngine
{
    public IWorld            World    { get; } = world;
    public IModRegistry      Registry { get; } = registry;
    public ICraftingManager  Crafting { get; } = crafting;
    public ISmeltingManager  Smelting { get; } = smelting;
}
