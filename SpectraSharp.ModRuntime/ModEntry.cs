using SpectraSharp.ModRuntime.Interop;
using SpectraSharp.ModRuntime.Mappings;
using SpectraSharp.ModRuntime.Sandbox;

namespace SpectraSharp.ModRuntime;

/// <summary>
/// Represents one loaded mod. Owns its sandbox, Mixin interceptor,
/// lifecycle state, and version mapping.
/// </summary>
public sealed class ModEntry
{
    public string         ModId      { get; }
    public string         DllPath    { get; }
    public VersionMapping Mapping    { get; }
    public ModSandbox     Sandbox    { get; }
    public bool           IsAlive    => Sandbox.IsAlive;

    /// <summary>
    /// Mixin interceptor for this mod.  Null until the DLL has been scanned.
    /// Patches registered through this use the same Harmony instance as the
    /// Sandbox, so RevertPatches() cleans up everything.
    /// </summary>
    public MixinInterceptor? Mixin { get; internal set; }

    // Populated after DLL is loaded
    public string  DisplayName { get; internal set; } = "";
    public string  Version     { get; internal set; } = "0.0.0";

    public ModEntry(string modId, string dllPath, VersionMapping mapping)
    {
        ModId   = modId;
        DllPath = dllPath;
        Mapping = mapping;
        Sandbox = new ModSandbox(modId);
    }
}
