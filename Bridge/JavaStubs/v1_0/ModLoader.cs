// Stub for ModLoader — Minecraft 1.0 static API

using SpectraSharp.Core.Mods;
using JavaBlock     = net.minecraft.block.Block;
using JavaItem      = net.minecraft.item.Item;
using JavaItemStack = net.minecraft.item.ItemStack;

namespace net.minecraft.src;

/// <summary>
/// MinecraftStubs v1_0 — ModLoader (static class).
/// Provides the static API mods call to register content:
///   ModLoader.AddRecipe(...)
///   ModLoader.AddShapelessRecipe(...)
///   ModLoader.AddSmelting(...)
///   ModLoader.RegisterEntityID(...)
///
/// All calls are routed to the current IEngine which was set at mod load time.
/// </summary>
public static class ModLoader
{
    // Set by SpectraSharp.ModRuntime.ModLoader before calling BaseMod.OnLoad()
    internal static IEngine? Engine { get; set; }

    static ICraftingManager Crafting =>
        Engine?.Crafting ?? throw new InvalidOperationException(
            "ModLoader.Engine not set — call from inside OnLoad() only.");

    static ISmeltingManager Smelting =>
        Engine?.Smelting ?? throw new InvalidOperationException(
            "ModLoader.Engine not set — call from inside OnLoad() only.");

    // ── Recipe registration ───────────────────────────────────────────────────

    /// <summary>
    /// Shaped crafting recipe.
    /// Java: ModLoader.AddRecipe(new dk(outputId, count), new Object[]{"XXX"," X "," X ",'X',ingId})
    /// </summary>
    public static void AddRecipe(JavaItemStack output, params object[] recipe)
    {
        var (pattern, key) = ParseShapedArgs(recipe);
        if (pattern.Length == 0) return;

        Crafting.AddShapedRecipe(
            new SpectraSharp.Core.Mods.ItemStack(output.itemID, output.stackSize),
            pattern,
            key);
    }

    /// <summary>
    /// Shapeless recipe.
    /// Java: ModLoader.AddShapelessRecipe(new dk(outputId, count), new Object[]{ing1, ing2})
    /// </summary>
    public static void AddShapelessRecipe(JavaItemStack output, params object[] ingredients)
    {
        var ids = ingredients
            .Select(ResolveId)
            .Where(id => id >= 0)
            .ToArray();

        if (ids.Length == 0) return;

        Crafting.AddShapelessRecipe(
            new SpectraSharp.Core.Mods.ItemStack(output.itemID, output.stackSize),
            ids);
    }

    /// <summary>
    /// Smelting recipe.
    /// Java: ModLoader.AddSmelting(inputId, new dk(outputId, count), xp)
    /// </summary>
    public static void AddSmelting(int inputId, JavaItemStack output, float xp)
    {
        Smelting.AddSmeltingRecipe(
            inputId,
            new SpectraSharp.Core.Mods.ItemStack(output.itemID, output.stackSize),
            xp);
    }

    /// <summary>Entity registration — no-op until EntityRegistry spec is done.</summary>
    public static void RegisterEntityID(java.lang.Class entityClass, string name, int id)
    {
        // TODO: delegate to IEntityRegistry once spec exists — see REQUESTS.md
        Console.WriteLine($"[ModLoader] RegisterEntityID: {name} id={id} (not yet implemented)");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static (string[] pattern, Dictionary<char, int> key) ParseShapedArgs(object[] args)
    {
        var pattern = new List<string>();
        var key     = new Dictionary<char, int>();

        int i = 0;
        while (i < args.Length && args[i] is string s) { pattern.Add(s); i++; }

        while (i + 1 < args.Length)
        {
            if (args[i] is char c || (args[i] is string cs && cs.Length == 1 && (c = cs[0]) != 0))
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
        int id            => id,
        JavaItemStack dk  => dk.itemID,
        JavaBlock b       => b.blockID,
        JavaItem it       => it.itemID,
        _                 => -1,
    };
}
