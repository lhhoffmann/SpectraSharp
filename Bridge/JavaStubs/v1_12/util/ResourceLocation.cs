// Stub for net.minecraft.util.ResourceLocation — Minecraft 1.12
// Used everywhere for block/item registry names: "mymod:myblock"

namespace net.minecraft.util;

/// <summary>
/// MinecraftStubs v1_12 — ResourceLocation.
/// A namespaced identifier: "modid:path" (e.g. "minecraft:stone", "mymod:myblock").
/// Mods use these as registry keys for blocks, items, recipes, etc.
/// </summary>
public sealed class ResourceLocation
{
    public string ResourceDomain { get; }
    public string ResourcePath   { get; }

    public ResourceLocation(string domain, string path)
    {
        ResourceDomain = domain.ToLowerInvariant();
        ResourcePath   = path.ToLowerInvariant();
    }

    /// <summary>Parses "domain:path" or "path" (domain defaults to "minecraft").</summary>
    public ResourceLocation(string combined)
    {
        int colon = combined.IndexOf(':');
        if (colon >= 0)
        {
            ResourceDomain = combined[..colon].ToLowerInvariant();
            ResourcePath   = combined[(colon + 1)..].ToLowerInvariant();
        }
        else
        {
            ResourceDomain = "minecraft";
            ResourcePath   = combined.ToLowerInvariant();
        }
    }

    public string getResourceDomain() => ResourceDomain;
    public string getResourcePath()   => ResourcePath;

    public override string ToString()  => $"{ResourceDomain}:{ResourcePath}";
    public override int    GetHashCode() => ToString().GetHashCode();
    public override bool   Equals(object? obj) =>
        obj is ResourceLocation r && r.ResourceDomain == ResourceDomain && r.ResourcePath == ResourcePath;
}
