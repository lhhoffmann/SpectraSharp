namespace SpectraSharp.Core.Mods;

/// <summary>
/// Base class for all mod items.
/// ModTranspiler generates a subclass per new Item found in the mod JAR.
/// </summary>
public abstract class ItemBase
{
    public abstract int    ItemId          { get; }
    public abstract string JavaClassName   { get; }
    public abstract int    ItemTextureIndex { get; }

    public virtual int MaxStackSize => 64;
    public virtual int MaxDamage    => 0;
    public bool IsStackable => MaxDamage == 0 && MaxStackSize > 1;

    public virtual float GetAttackDamage() => 1f;

    public virtual bool OnUseOnBlock(
        IWorld world, int x, int y, int z, Face face, ItemStack stack) => false;

    public virtual bool OnUseInAir(IWorld world, ItemStack stack) => false;

    public virtual void OnEntityHit(object entity, ItemStack stack) { }
}
