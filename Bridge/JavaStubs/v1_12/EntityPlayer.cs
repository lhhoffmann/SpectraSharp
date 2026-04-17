// Stub for net.minecraft.entity.player.EntityPlayer — Minecraft 1.12

using net.minecraft.entity;

namespace net.minecraft.entity.player;

/// <summary>
/// MinecraftStubs v1_12 — EntityPlayer.
/// The player entity. Mods interact with it through block callbacks and events.
/// </summary>
public class EntityPlayer : EntityLivingBase
{
    /// <summary>Item held in the main hand. Null = empty hand.</summary>
    public net.minecraft.item.ItemStack? getHeldItemMainhand() => null; // CODER: wire to IPlayer

    /// <summary>Item held in the off hand.</summary>
    public net.minecraft.item.ItemStack? getHeldItemOffhand()  => null; // CODER: wire to IPlayer

    public string getName() => entityName;

    public bool capabilities_allowFlying    { get; set; }
    public bool capabilities_isCreativeMode { get; set; }
    public bool isCreative() => capabilities_isCreativeMode;

    public override string getJavaClassName() => "net.minecraft.entity.player.EntityPlayer";
}
