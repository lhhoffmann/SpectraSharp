// Stub for net.minecraftforge.registries.ForgeRegistries — Minecraft Forge 1.12

using net.minecraft.util;

namespace net.minecraftforge.registries;

/// <summary>
/// MinecraftStubs v1_12 — ForgeRegistries.
///
/// The 1.12.2 registry API. Mods use:
///   ForgeRegistries.BLOCKS.register(block)
///   ForgeRegistries.ITEMS.register(item)
///
/// Routes to GameRegistry.register() internally.
/// </summary>
public static class ForgeRegistries
{
    public static readonly ForgeRegistry<net.minecraft.block.Block> BLOCKS = new();
    public static readonly ForgeRegistry<net.minecraft.item.Item>   ITEMS  = new();
}

/// <summary>Generic Forge registry — wraps GameRegistry calls.</summary>
public sealed class ForgeRegistry<T> where T : class
{
    public void register(T entry)
    {
        switch (entry)
        {
            case net.minecraft.block.Block b:
                net.minecraftforge.fml.common.registry.GameRegistry.register(b);
                break;
            case net.minecraft.item.Item it:
                net.minecraftforge.fml.common.registry.GameRegistry.register(it);
                break;
            default:
                Console.WriteLine($"[ForgeRegistries] Unhandled type: {typeof(T).Name}");
                break;
        }
    }

    public T? getValue(ResourceLocation name)
    {
        // CODER: delegate to IBlockRegistry/IItemRegistry.Get(name) when implemented
        return null;
    }
}
