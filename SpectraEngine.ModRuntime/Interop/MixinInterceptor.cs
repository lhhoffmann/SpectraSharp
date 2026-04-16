using System.Reflection;
using HarmonyLib;

namespace SpectraEngine.ModRuntime.Interop;

/// <summary>
/// Orchestrates the Mixin → Harmony pipeline for one mod assembly.
///
/// Flow:
///   1. <see cref="MixinScanner.Scan"/>   — locate all @Mixin-annotated types in the DLL
///   2. <see cref="ClassMapping.Resolve"/> — map each target Java class → SpectraEngine.Core type
///   3. <see cref="HarmonyBridge.Apply"/> — emit Harmony patches for every injection
///
/// All patches go through the caller-supplied <see cref="Harmony"/> instance, so that
/// <see cref="Sandbox.ModSandbox.RevertPatches"/> unloads Mixin patches and game-logic
/// patches atomically when a mod is unloaded or killed.
///
/// Call <see cref="Intercept"/> from <c>ModLoader.LoadDll()</c> BEFORE invoking
/// <c>ISpectraMod.OnLoad()</c> — mods that inject their own event hooks must have
/// those hooks in place before any game code fires.
/// </summary>
public sealed class MixinInterceptor
{
    readonly HarmonyBridge _bridge;
    int _patchCount;

    public int PatchCount => _patchCount;

    /// <param name="modId">
    ///   The mod identifier — used only in log messages. Must match the id used
    ///   for the Harmony instance so patch ownership is unambiguous.
    /// </param>
    /// <param name="harmony">
    ///   The Harmony instance owned by this mod's <see cref="Sandbox.ModSandbox"/>.
    ///   Sharing it means one <c>UnpatchAll(id)</c> call covers everything.
    /// </param>
    public MixinInterceptor(string modId, Harmony harmony)
    {
        _bridge = new HarmonyBridge(harmony);
        _ = modId; // stored for future diagnostic use; no field needed today
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans <paramref name="asm"/> for @Mixin annotations and applies all found patches.
    ///
    /// Returns the total number of <see cref="MixinDescriptor"/>s processed.
    /// Returns 0 immediately for mods with no Mixin classes (the common 1.0 case).
    /// </summary>
    public int Intercept(Assembly asm)
    {
        var descriptors = MixinScanner.Scan(asm);

        if (descriptors.Count == 0) return 0;

        Console.WriteLine(
            $"[MixinInterceptor] Found {descriptors.Count} @Mixin class(es) in" +
            $" {asm.GetName().Name}.");

        foreach (var desc in descriptors)
        {
            Console.WriteLine(
                $"[MixinInterceptor] {desc.MixinType.Name} → {desc.TargetJavaClass}" +
                $" ({desc.Injections.Count} injection(s))");

            _bridge.Apply(desc);
            _patchCount += desc.Injections.Count;
        }

        Console.WriteLine(
            $"[MixinInterceptor] Applied {_patchCount} patch(es) from" +
            $" {descriptors.Count} mixin(s).");

        return descriptors.Count;
    }
}
