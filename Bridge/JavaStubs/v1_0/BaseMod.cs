// Stub for BaseMod — Minecraft 1.0 ModLoader entry point

using SpectraEngine.Core.Mods;

namespace net.minecraft.src;

/// <summary>
/// MinecraftStubs v1_0 — BaseMod.
/// All 1.0 mods have a class mod_Xxx extending BaseMod.
/// This stub maps the ModLoader lifecycle to ISpectraMod.
///
/// The stub implements ISpectraMod so ModLoader discovers it via reflection.
/// </summary>
public abstract class BaseMod : ISpectraMod
{
    // ── ISpectraMod ───────────────────────────────────────────────────────────

    public virtual string ModId       => GetType().Name;
    public virtual string DisplayName => getName();
    public virtual string Version     => "1.0";

    public void OnLoad(IEngine engine)
    {
        // 1.0 mods register blocks/items in their constructor or load().
        // Stubs for Block.blocksList and Item.itemsList handle registration.
        // Call the ModLoader lifecycle methods in order:
        load();
        modsLoaded();
    }

    public void OnUnload() { }

    // ── ModLoader API (implemented by mod classes) ────────────────────────────

    /// <summary>Called once at mod load. Override to register blocks, items, recipes.</summary>
    public abstract void load();

    /// <summary>Called after ALL mods have loaded. Override for cross-mod setup.</summary>
    public virtual void modsLoaded() { }

    /// <summary>Human-readable mod name returned to ModLoader.</summary>
    public abstract string getName();

    /// <summary>Called each game tick. Override for per-tick logic.</summary>
    public virtual void onTickInGame() { }

    /// <summary>Called on world generation for each chunk.</summary>
    public virtual void generateSurface(SpectraEngine.Core.IWorld world,
                                         System.Random rng, int x, int z) { }
}
