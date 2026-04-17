namespace SpectraEngine.Core.Mods;

/// <summary>
/// Contract between a stub Item class (MinecraftStubs) and Core.
/// ModItemBridge reads these properties when registering the item in Core.Item.ItemsList.
/// </summary>
public interface IModItemBehavior
{
    int ItemId       { get; }
    int MaxStackSize { get; }
    int MaxDamage    { get; }
    int IconIndex    { get; }
}
