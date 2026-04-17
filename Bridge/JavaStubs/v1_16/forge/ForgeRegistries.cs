// Stub for net.minecraftforge.registries.ForgeRegistries — Forge 1.16.5

using SpectraEngine.Core.Mods;
using net.minecraft.util;

namespace net.minecraftforge.registries;

/// <summary>
/// MinecraftStubs v1_16 — ForgeRegistries.
/// Static handles to the main registries — used by DeferredRegister.create().
/// </summary>
public static class ForgeRegistries
{
    public static readonly ConcreteForgeRegistry<net.minecraft.block.Block> BLOCKS = new();
    public static readonly ConcreteForgeRegistry<net.minecraft.item.Item>   ITEMS  = new();
}

/// <summary>Concrete implementation of IForgeRegistry routing to ModBlockBridge/ModItemBridge.</summary>
public sealed class ConcreteForgeRegistry<T> : IForgeRegistry<T> where T : class
{
    public void register(T entry)
    {
        switch (entry)
        {
            case net.minecraft.block.Block b when b._registryName != null:
                if (b is IModBlockBehavior blockBehavior)
                    ModBlockBridge.TryCreate(b._registryName.ToString(), blockBehavior);
                break;
            case net.minecraft.item.Item it when it._registryName != null:
                if (it is IModItemBehavior itemBehavior)
                    ModItemBridge.TryCreate(it._registryName.ToString(), itemBehavior);
                break;
            default:
                Console.WriteLine($"[ForgeRegistries1.16] Unhandled or unnamed {typeof(T).Name}");
                break;
        }
    }

    public T? getValue(ResourceLocation name) => null;
}
