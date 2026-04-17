// Stub for net.minecraft.util.ResourceLocation — Minecraft 1.16
// Same semantics as 1.12 version.

namespace net.minecraft.util;

/// <summary>MinecraftStubs v1_16 — ResourceLocation ("namespace:path").</summary>
public sealed class ResourceLocation
{
    public string Namespace { get; }
    public string Path      { get; }

    public ResourceLocation(string namespacedPath)
    {
        int colon = namespacedPath.IndexOf(':');
        if (colon >= 0) { Namespace = namespacedPath[..colon]; Path = namespacedPath[(colon+1)..]; }
        else            { Namespace = "minecraft";              Path = namespacedPath; }
    }

    public ResourceLocation(string ns, string path) { Namespace = ns; Path = path; }

    public string getNamespace() => Namespace;
    public string getPath()      => Path;
    public override string ToString() => $"{Namespace}:{Path}";
}
