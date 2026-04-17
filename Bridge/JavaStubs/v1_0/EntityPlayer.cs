// Minimal EntityPlayer stub for v1_0 block interaction methods

namespace net.minecraft.entity.player;

/// <summary>
/// MinecraftStubs v1_0 — EntityPlayer.
/// Passed to Block.onBlockActivated(); mod code casts to access inventory.
/// </summary>
public class EntityPlayer
{
    public string username { get; set; } = "";
    public virtual string getJavaClassName() => "net.minecraft.entity.player.EntityPlayer";
}
