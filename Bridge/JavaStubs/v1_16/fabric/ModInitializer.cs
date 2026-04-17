// Stub for net.fabricmc.api.ModInitializer — Fabric 1.16.5
// Fabric's entry point interface — the mod implements this directly.

using SpectraEngine.Core.Mods;

namespace net.fabricmc.api;

/// <summary>
/// MinecraftStubs v1_16 — ModInitializer.
/// Fabric mods implement this interface and declare it as an entry point
/// in fabric.mod.json: "entrypoints": { "main": ["com.example.MyMod"] }
///
/// FabricModWrapper detects types implementing this interface and calls
/// onInitialize() wrapped as ISpectraMod.OnLoad().
/// </summary>
public interface ModInitializer
{
    void onInitialize();
}

// ── FabricModWrapper ──────────────────────────────────────────────────────────

/// <summary>
/// Wraps a Fabric ModInitializer in ISpectraMod.
/// Unlike Forge, Fabric uses no annotations — the interface itself is the entry point.
/// ModLoader detects ISpectraMod implementations first; this wrapper makes
/// ModInitializer-only mods visible to the engine.
/// </summary>
public sealed class FabricModWrapper(ModInitializer mod) : ISpectraMod
{
    public string ModId      => mod.GetType().Name;
    public string DisplayName => ModId;
    public string Version     => "1.16";

    public void OnLoad(IEngine engine)
    {
        try { mod.onInitialize(); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[FabricMod] {ModId}.onInitialize() threw: {ex.Message}");
        }
    }

    public void OnUnload() { }
}
