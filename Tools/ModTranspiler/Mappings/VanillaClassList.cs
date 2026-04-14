namespace SpectraSharp.ModTranspiler.Mappings;

/// <summary>
/// All known obfuscated class names from the vanilla 1.0 JAR.
/// Used by ModDiffer to tag mod classes as NEW_CONTENT vs OVERRIDE.
/// Source: Documentation/VoxelCore/Parity/Mappings/classes.md
/// </summary>
static class VanillaClassList
{
    static readonly HashSet<string> Classes = new()
    {
        // ── Core game classes ────────────────────────────────────────────────
        "yy",   // Block
        "acr",  // RenderBlocks
        "adt",  // EntityRenderer
        "wu",   // Material
        "bj",   // MaterialLiquid
        "aeg",  // MaterialLogic
        "zx",   // Chunk
        "ry",   // World
        "gy",   // ChunkLoader
        "ia",   // EntityPlayer (large class)
        "vm",   // EntityLiving
        "vi",   // Entity base
        "nq",   // Item base  (sr is also Item — see below)
        "sr",   // Item (static registry)
        "aef",  // Entity base (256-slot array)
        "p",    // SoundType
        "n",    // WorldRenderer
        "ik",   // NBTTagCompound
        "vx",   // NBTBase / CompressedStreamTools
        "me",   // MathHelper (obfuscated)
        "bt",   // BufferUtils / GL helpers
        "fb",   // Vec3 pool

        // ── Block subclasses (all from yy.java static init) ─────────────────
        "gm",  "jb",  "agd", "aet", "ahx", "add", "cj",  "kb",
        "v",   "aip", "qo",  "aho", "aat", "yq",  "aab", "afr",
        "ags", "abr", "acu", "fr",  "wg",  "js",  "rs",  "xs",
        "abm", "ay",  "bg",  "wj",  "kk",  "au",  "kw",  "aha",
        "ni",  "eu",  "uc",  "afu", "aaa", "oc",  "ku",  "aif",
        "jk",  "ahq", "ow",  "pc",  "md",  "abl", "nz",  "nf",
        "et",  "mq",  "sk",  "sc",  "aem", "mz",  "wd",  "uh",
        "of",  "pu",  "ahl", "fp",  "ez",  "qi",  "vy",  "sy",
        "ahp", "ic",  "aid", "rl",  "aci", "jh",  "vf",  "mf",
        "cu",  "rn",  "mr",  "ain", "kv",  "jl",  "abr", "abm",
        "nk",  "pk",  "agw", "afv", "adb", "aec", "acl", "abx",
        "aby", "abg", "aav", "aaq", "aab",

        // ── Item subclasses / item-related ───────────────────────────────────
        "jb",  "qz",  "wg",  "kv",  "jl",
        "acy", "acx", "acz", "aco", // ItemTool, ItemSword, ItemSpade, ItemAxe, ItemHoe
        "dk",                        // ItemStack
        "sr",                        // Item (duplicate — explicit for clarity)

        // ── World / Level ────────────────────────────────────────────────────
        "zx",  "ry",  "gy",  "d",   "yy",

        // ── WorldGen ─────────────────────────────────────────────────────────
        "ky",  // WorldGenMineable (ore vein generator)
        "ig",  // WorldGenerator base class
        "kq",  // WorldGenTrees
        "aam", // WorldGenLakes

        // ── GUI ──────────────────────────────────────────────────────────────
        "acr", "adt",

        // ── Mob ──────────────────────────────────────────────────────────────
        "ia",  "vm",  "vi",

        // ── Net / Client ─────────────────────────────────────────────────────
        "net.minecraft.client.Minecraft",
        "net.minecraft.client.MinecraftApplet",

        // ── Third-party (always PASSTHROUGH) ─────────────────────────────────
        // These are never overridden by mods, included only for completeness.
        // ModDiffer checks IsLibraryPrefix() before consulting this set.
    };

    /// <summary>Returns true if the class name was present in vanilla 1.0.</summary>
    public static bool Contains(string obfuscatedName) =>
        Classes.Contains(obfuscatedName);

    /// <summary>Returns true for known third-party library packages that mods bundle.</summary>
    public static bool IsLibraryPrefix(string className) =>
        className.StartsWith("com.jcraft", StringComparison.Ordinal)  ||
        className.StartsWith("paulscode",  StringComparison.Ordinal)  ||
        className.StartsWith("org.lwjgl",  StringComparison.Ordinal)  ||
        className.StartsWith("javax.",     StringComparison.Ordinal)  ||
        className.StartsWith("java.",      StringComparison.Ordinal);

    /// <summary>Human-readable C# name for a vanilla obfuscated class name.</summary>
    static readonly Dictionary<string, string> HumanNames = new()
    {
        ["yy"]  = "Block",
        ["acr"] = "RenderBlocks",
        ["wu"]  = "Material",
        ["zx"]  = "Chunk",
        ["ry"]  = "World",
        ["gy"]  = "ChunkLoader",
        ["sr"]  = "Item",
        ["ia"]  = "EntityPlayer",
        ["vm"]  = "EntityLiving",
        ["vi"]  = "Entity",
        ["aef"] = "Entity",
        ["gm"]  = "BlockStone",
        ["jb"]  = "BlockGrass",
        ["agd"] = "BlockDirt",
        ["cj"]  = "BlockSand",
        ["kb"]  = "BlockGravel",
        ["v"]   = "BlockOre",
        ["aip"] = "BlockLog",
        ["qo"]  = "BlockLeaves",
        ["ahx"] = "BlockFluid",
        ["add"] = "BlockStationary",
        ["ahh"] = "BlockStairs",
        ["xs"]  = "BlockSlab",
        ["abm"] = "BlockTNT",
        ["bg"]  = "BlockTorch",
        ["wj"]  = "BlockFire",
        ["kk"]  = "BlockMobSpawner",
        ["au"]  = "BlockChest",
        ["kw"]  = "BlockRedstoneWire",
        ["eu"]  = "BlockFurnace",
        ["afr"] = "BlockRail",
        ["nz"]  = "BlockFence",
        ["nf"]  = "BlockPumpkin",
        ["sy"]  = "BlockEnchantmentTable",
        ["aci"] = "BlockDragonEgg",

        // ── Items ────────────────────────────────────────────────────────────
        ["sr"]  = "Item",
        ["acy"] = "ItemTool",
        ["acx"] = "ItemSword",
        ["acz"] = "ItemSpade",
        ["aco"] = "ItemHoe",
        ["dk"]  = "ItemStack",

        // ── WorldGen ─────────────────────────────────────────────────────────
        ["ky"]  = "WorldGenMineable",
        ["ig"]  = "WorldGenerator",
        ["kq"]  = "WorldGenTrees",
        ["aam"] = "WorldGenLakes",
    };

    public static string ToHumanName(string obfuscated) =>
        HumanNames.TryGetValue(obfuscated, out string? name) ? name : obfuscated;
}
