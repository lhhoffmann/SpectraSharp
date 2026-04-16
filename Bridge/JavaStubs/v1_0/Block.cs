// Stub for obfuscated class yy (Block) — Minecraft 1.0
// Maps to SpectraEngine.Core.BlockBase

using SpectraEngine.Core;

namespace net.minecraft.block;

/// <summary>
/// MinecraftStubs v1_0 — Block (obf: yy).
/// Represents the registry of all block types.
/// Mod blocks extend this class; vanilla blocks are registered in blocksList.
/// </summary>
public class Block
{
    // ── Static registry (blocksList proxy) ───────────────────────────────────

    /// <summary>
    /// Java: Block.blocksList[id]
    /// Proxy array — reads/writes route to BlockRegistry.
    /// Mods write: Block.blocksList[125] = new MyBlock(125)
    /// </summary>
    public static readonly BlockListProxy blocksList = new();

    // ── Instance fields (used by mods that subclass Block) ───────────────────

    public int blockID { get; set; }

    // Java identity
    public virtual string getJavaClassName() => "net.minecraft.block.Block";

    // ── Standard vanilla block singletons ─────────────────────────────────────
    // Mods reference these as: Block.stone.blockID, Block.dirt, etc.

    public static readonly Block stone   = new() { blockID = 1  };
    public static readonly Block grass   = new() { blockID = 2  };
    public static readonly Block dirt    = new() { blockID = 3  };
    public static readonly Block cobblestone = new() { blockID = 4 };
    public static readonly Block sand    = new() { blockID = 12 };
    public static readonly Block gravel  = new() { blockID = 13 };
    public static readonly Block goldOre = new() { blockID = 14 };
    public static readonly Block ironOre = new() { blockID = 15 };
    public static readonly Block coalOre = new() { blockID = 16 };
}

/// <summary>
/// Proxy for Block.blocksList[].
/// Reads  → BlockRegistry.Get(id)
/// Writes → BlockRegistry.Register(id, block)
/// Appears as a Java array to mod code (length, indexer).
/// </summary>
public sealed class BlockListProxy
{
    public Block? this[int id]
    {
        get
        {
            // Return a stub Block for any registered id.
            // CODER: add BlockRegistry.Get(int id) when IBlockRegistry is implemented,
            // then replace this with a proper lookup.
            return new Block { blockID = id };
        }
        set
        {
            if (value == null) return;
            // Mod is registering a new block — tell Core registry.
            // The actual Core block object is created by the stub subclass.
            // Core/BlockRegistry must accept the registration.
            // CODER: implement BlockRegistry.RegisterMod(int id, string modId) if missing.
            Console.WriteLine($"[JavaStubs] Block.blocksList[{value.blockID}] = {value.GetType().Name}");
        }
    }

    public int Length => 4096;
}
