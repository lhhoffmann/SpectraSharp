// Stub for net.fabricmc.loader.api.FabricLoader — Fabric 1.16.5

namespace net.fabricmc.loader.api;

/// <summary>
/// MinecraftStubs v1_16 — FabricLoader.
/// Fabric mods call FabricLoader.getInstance() to get mod metadata,
/// config directory, and check for mod presence.
/// </summary>
public sealed class FabricLoader
{
    static readonly FabricLoader _instance = new();
    public static FabricLoader getInstance() => _instance;

    /// <summary>Returns true if the mod with the given mod ID is loaded.</summary>
    public bool isModLoaded(string modId)
    {
        // CODER: delegate to IEngine.ModLoader.IsLoaded(modId)
        return false;
    }

    /// <summary>Returns the game directory (where the mods folder lives).</summary>
    public string getGameDir() => AppContext.BaseDirectory;

    /// <summary>Returns the config directory.</summary>
    public string getConfigDir()
        => Path.Combine(AppContext.BaseDirectory, "config");

    /// <summary>Returns all currently loaded mods (stub — empty list).</summary>
    public IReadOnlyList<ModContainer> getAllMods() => [];
}

/// <summary>Minimal mod container stub.</summary>
public sealed class ModContainer(string modId, string version)
{
    public ModMetadata getMetadata() => new(modId, version);
}

/// <summary>Minimal mod metadata stub.</summary>
public sealed class ModMetadata(string id, string version)
{
    public string getId()      => id;
    public string getVersion() => version;
}
