// Stub for cpw.mods.fml.common.registry.GameRegistry — Minecraft Forge 1.7.10
// In 1.7.10 the registration API used registerBlock(block, name) not register(block).

using SpectraEngine.Core.Mods;
using net.minecraft.block;
using net.minecraft.item;
using ItemStack = net.minecraft.item.ItemStack;
using CoreItemStack = SpectraEngine.Core.Mods.ItemStack;

namespace cpw.mods.fml.common.registry
{
    /// <summary>
    /// MinecraftStubs v1_7_10 — GameRegistry.
    ///
    /// Key differences from v1_12:
    ///   - registerBlock(block, "name") instead of register(block.setRegistryName("..."))
    ///   - registerItem(item, "name") same pattern
    ///   - addShapedRecipe / addShapelessRecipe same as 1.12
    /// </summary>
    public static class GameRegistry
    {
        internal static IEngine? Engine { get; set; }

        static ICraftingManager Crafting => Engine?.Crafting
            ?? throw new InvalidOperationException("GameRegistry called outside mod initialization.");

        static ISmeltingManager Smelting => Engine?.Smelting
            ?? throw new InvalidOperationException("GameRegistry called outside mod initialization.");

        // ── Block registration ────────────────────────────────────────────────────

        /// <summary>Registers a block with a name. Classic 1.7.10 API.</summary>
        public static void registerBlock(Block block, string name)
        {
            // Writing to blocksList triggers ModBlockBridge.TryCreate() via the proxy setter.
            Block.blocksList[block.blockID] = block;
        }

        /// <summary>Registers a block with a custom ItemBlock class (overload).</summary>
        public static void registerBlock(Block block, Type itemBlockClass, string name)
            => registerBlock(block, name);

        // ── Item registration ─────────────────────────────────────────────────────

        /// <summary>Registers an item with a name.</summary>
        public static void registerItem(Item item, string name)
        {
            // Writing to itemsList triggers ModItemBridge.TryCreate() via the proxy setter.
            Item.itemsList[item.itemID] = item;
        }

        // ── Recipe registration ───────────────────────────────────────────────────

        /// <summary>Adds a shaped crafting recipe.</summary>
        public static net.minecraft.item.crafting.IRecipe addShapedRecipe(
            ItemStack output, params object[] args)
        {
            var (pattern, key) = ParseShapedArgs(args);
            if (pattern.Length > 0)
                Crafting.AddShapedRecipe(new CoreItemStack(output.itemID, output.stackSize), pattern, key);
            return new net.minecraft.item.crafting.DummyRecipe();
        }

        /// <summary>Adds a shapeless crafting recipe.</summary>
        public static net.minecraft.item.crafting.IRecipe addShapelessRecipe(
            ItemStack output, params object[] ingredients)
        {
            var ids = ingredients.Select(ResolveId).Where(id => id >= 0).ToArray();
            if (ids.Length > 0)
                Crafting.AddShapelessRecipe(new CoreItemStack(output.itemID, output.stackSize), ids);
            return new net.minecraft.item.crafting.DummyRecipe();
        }

        /// <summary>Adds a smelting recipe.</summary>
        public static void addSmelting(int inputId, ItemStack output, float xp)
            => Smelting.AddSmeltingRecipe(inputId, new CoreItemStack(output.itemID, output.stackSize), xp);

        public static void addSmelting(Block input, ItemStack output, float xp)
            => addSmelting(input.blockID, output, xp);

        public static void addSmelting(Item input, ItemStack output, float xp)
            => addSmelting(input.itemID, output, xp);

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
                    char ch                       => ch,
                    string cs when cs.Length == 1 => cs[0],
                    _                             => '\0',
                };
                if (c != '\0') { int id = ResolveId(args[i + 1]); if (id >= 0) key[c] = id; i += 2; }
                else i++;
            }
            return ([.. pattern], key);
        }

        static int ResolveId(object obj) => obj switch
        {
            int id       => id,
            ItemStack s  => s.itemID,
            Block b      => b.blockID,
            Item it      => it.itemID,
            _            => -1,
        };
    }
}

namespace net.minecraft.item.crafting
{
    public interface IRecipe { }
    public sealed class DummyRecipe : IRecipe { }
}
