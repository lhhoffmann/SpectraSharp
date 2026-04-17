// Stub for net.minecraft.entity.player.PlayerEntity — Minecraft 1.16.5
// Note: 1.16 renamed EntityPlayer → PlayerEntity

using net.minecraft.entity;
using net.minecraft.item;

namespace net.minecraft.entity.player;

/// <summary>
/// MinecraftStubs v1_16 — PlayerEntity.
/// Renamed from EntityPlayer in 1.12; Hand enum unchanged.
/// </summary>
public class PlayerEntity : LivingEntity
{
    public PlayerInventory inventory = new();

    public ItemStack getHeldItemMainhand()
        => inventory.mainInventory.Length > 0
           ? inventory.mainInventory[inventory.currentItem]
           : ItemStack.EMPTY;

    public ItemStack getHeldItemOffhand()
        => ItemStack.EMPTY;

    public ItemStack getHeldItem(Hand hand)
        => hand == Hand.MAIN_HAND ? getHeldItemMainhand() : getHeldItemOffhand();

    public bool isCreative()  => false;
    public bool isSneaking()  => false;
    public bool isSprinting() => false;

    public override string getJavaClassName() => "net.minecraft.entity.player.PlayerEntity";
}

/// <summary>
/// MinecraftStubs v1_16 — PlayerInventory stub.
/// </summary>
public sealed class PlayerInventory
{
    public ItemStack[] mainInventory = new ItemStack[36];
    public int currentItem = 0;

    public PlayerInventory()
    {
        for (int i = 0; i < mainInventory.Length; i++)
            mainInventory[i] = ItemStack.EMPTY;
    }
}

/// <summary>Hand enum — unchanged from 1.12.</summary>
public enum Hand { MAIN_HAND, OFF_HAND }
