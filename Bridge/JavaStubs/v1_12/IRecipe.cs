// Stub for net.minecraft.item.crafting.IRecipe — Minecraft 1.12

using net.minecraft.util;

namespace net.minecraft.item.crafting;

/// <summary>
/// MinecraftStubs v1_12 — IRecipe.
/// Returned by GameRegistry.addShapedRecipe / addShapelessRecipe.
/// Mods sometimes capture this to register it further or chain calls.
/// </summary>
public interface IRecipe
{
    ResourceLocation getRegistryName();
}
