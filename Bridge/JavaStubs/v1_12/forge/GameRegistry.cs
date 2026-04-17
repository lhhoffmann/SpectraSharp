// Stub for net.minecraftforge.fml.common.registry.GameRegistry — Minecraft Forge 1.12
// The primary registration API that 1.12 mods use to add blocks and items.

using SpectraEngine.Core.Mods;
using net.minecraft.block;
using net.minecraft.item;
using net.minecraft.util;
using CoreItemStack = SpectraEngine.Core.Mods.ItemStack;

namespace net.minecraftforge.fml.common.registry;

/// <summary>
/// MinecraftStubs v1_12 — GameRegistry.
///
/// Central registration API for Forge 1.12 mods:
///   GameRegistry.register(block)          — registers a block
///   GameRegistry.register(item)           — registers an item
///   GameRegistry.addShapedRecipe(...)     — adds a shaped crafting recipe
///   GameRegistry.addShapelessRecipe(...)  — adds a shapeless crafting recipe
///   GameRegistry.addSmelting(...)         — adds a smelting recipe
///
/// All calls route to the IEngine's registry/crafting services.
/// Engine is set by ForgeMod1_12Wrapper before OnLoad() fires.
/// </summary>
public static class GameRegistry
{
    // Set by ForgeMod1_12Wrapper before lifecycle methods are called.
    internal static IEngine? Engine { get; set; }

    static ICraftingManager Crafting => Engine?.Crafting
        ?? throw new InvalidOperationException(
            "GameRegistry called outside of mod initialization. " +
            "Ensure you register in FMLPreInitializationEvent/FMLInitializationEvent.");

    static ISmeltingManager Smelting => Engine?.Smelting
        ?? throw new InvalidOperationException(
            "GameRegistry called outside of mod initialization.");

    // ── Block registration ────────────────────────────────────────────────────

    /// <summary>
    /// Registers a block with its registry name.
    /// Allocates a numeric ID and wires the block into Core.Block.BlocksList.
    /// </summary>
    public static Block register(Block block)
    {
        if (block._registryName == null)
        {
            Console.Error.WriteLine(
                $"[GameRegistry] Block {block.GetType().Name} has no registry name — skipping.");
            return block;
        }

        if (block is IModBlockBehavior behavior)
            ModBlockBridge.TryCreate(block._registryName.ToString(), behavior);

        return block;
    }

    // ── Item registration ─────────────────────────────────────────────────────

    /// <summary>Registers an item with its registry name.</summary>
    public static Item register(Item item)
    {
        if (item._registryName == null)
        {
            Console.Error.WriteLine(
                $"[GameRegistry] Item {item.GetType().Name} has no registry name — skipping.");
            return item;
        }

        if (item is IModItemBehavior behavior)
            ModItemBridge.TryCreate(item._registryName.ToString(), behavior);

        return item;
    }

    // ── Recipe registration ───────────────────────────────────────────────────

    /// <summary>
    /// Shaped crafting recipe.
    /// Java: GameRegistry.addShapedRecipe(registryName, output, pattern..., mappings...)
    ///
    /// Pattern strings followed by char→item pairs:
    ///   GameRegistry.addShapedRecipe(name, output, "XXX", "X X", "XXX", 'X', Items.STICK)
    /// </summary>
    public static net.minecraft.item.crafting.IRecipe addShapedRecipe(
        ResourceLocation name, net.minecraft.item.ItemStack output, params object[] args)
    {
        var (pattern, key) = ParseShapedArgs(args);

        if (pattern.Length > 0)
        {
            Crafting.AddShapedRecipe(
                new CoreItemStack(output.itemID, output.stackSize),
                pattern, key);
        }

        return new DummyRecipe(name);
    }

    /// <summary>Shapeless crafting recipe.</summary>
    public static net.minecraft.item.crafting.IRecipe addShapelessRecipe(
        ResourceLocation name, net.minecraft.item.ItemStack output, params object[] ingredients)
    {
        var ids = ingredients.Select(ResolveId).Where(id => id >= 0).ToArray();
        if (ids.Length > 0)
            Crafting.AddShapelessRecipe(new CoreItemStack(output.itemID, output.stackSize), ids);

        return new DummyRecipe(name);
    }

    /// <summary>Smelting recipe.</summary>
    public static void addSmelting(net.minecraft.item.ItemStack input,
                                   net.minecraft.item.ItemStack output, float xp)
        => Smelting.AddSmeltingRecipe(
               input.itemID,
               new CoreItemStack(output.itemID, output.stackSize),
               xp);

    public static void addSmelting(int inputId,
                                   net.minecraft.item.ItemStack output, float xp)
        => Smelting.AddSmeltingRecipe(
               inputId,
               new CoreItemStack(output.itemID, output.stackSize),
               xp);

    // ── Helpers ───────────────────────────────────────────────────────────────

    static (string[] pattern, Dictionary<char, int> key) ParseShapedArgs(object[] args)
    {
        var pattern = new List<string>();
        var key     = new Dictionary<char, int>();
        int i = 0;

        while (i < args.Length && args[i] is string s) { pattern.Add(s); i++; }

        while (i + 1 < args.Length)
        {
            char c = args[i] switch
            {
                char ch              => ch,
                string cs when cs.Length == 1 => cs[0],
                _                    => '\0',
            };
            if (c != '\0')
            {
                int id = ResolveId(args[i + 1]);
                if (id >= 0) key[c] = id;
                i += 2;
            }
            else i++;
        }

        return ([.. pattern], key);
    }

    static int ResolveId(object obj) => obj switch
    {
        int id              => id,
        net.minecraft.item.ItemStack s => s.itemID,
        net.minecraft.block.Block b    => 0, // CODER: map block → item ID via IBlockRegistry
        net.minecraft.item.Item it     => it.itemID,
        _                              => -1,
    };
}

/// <summary>
/// Dummy IRecipe implementation — returned by addShapedRecipe/addShapelessRecipe
/// so mods that capture the return value don't get a null.
/// </summary>
public sealed class DummyRecipe(ResourceLocation name) : net.minecraft.item.crafting.IRecipe
{
    public ResourceLocation getRegistryName() => name;
}
