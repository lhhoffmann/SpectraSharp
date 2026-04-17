using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using SpectraEngine.Core.Mods;
using SpectraEngine.ModRuntime.AllocGuard;
using SpectraEngine.ModRuntime.Compiler;
using SpectraEngine.ModRuntime.Interop;
using SpectraEngine.ModRuntime.Mappings;
using SpectraEngine.ModRuntime.Sandbox;

namespace SpectraEngine.ModRuntime;

/// <summary>
/// Discovers, compiles (if needed), and loads mod JARs and DLLs.
///
/// Lifecycle per mod:
///   1. Scan mods/ for .jar files
///   2. VersionDetector → which MC version?
///   3. ModCompiler → JAR → DLL via ikvmc (skipped if DLL already up-to-date)
///   4. AssemblyLoadContext → load DLL into isolated context
///   5. Locate ISpectraMod entry point → call OnLoad(engine)
///   6. On unload: call OnUnload(), revert Harmony patches, unload context
///
/// The ModLoader owns all ModEntry instances and drives the tick boundary reset.
/// </summary>
public sealed class ModLoader
{
    readonly string    _modsDir;
    readonly string    _compiledDir;
    readonly IEngine   _engine;
    readonly List<ModEntry> _mods = [];
    readonly Dictionary<ModEntry, AssemblyLoadContext> _contexts = [];

    public IReadOnlyList<ModEntry> Mods => _mods;

    public ModLoader(string modsDir, string compiledDir, IEngine engine)
    {
        _modsDir     = modsDir;
        _compiledDir = compiledDir;
        _engine      = engine;
        Directory.CreateDirectory(modsDir);
        Directory.CreateDirectory(compiledDir);
    }

    // ── Discovery & Load ──────────────────────────────────────────────────────

    /// <summary>
    /// Scans the mods directory, compiles any JARs, and loads all DLLs.
    /// Call once at engine startup.
    /// </summary>
    public void LoadAll()
    {
        // Step 0: IKVM runtime + stubs must be ready before any mod DLL is touched.
        string stubsDir = Path.Combine(AppContext.BaseDirectory, "Stubs");
        IkvmBridge.Initialize(stubsDir);

        // Step 1: compile any JARs that don't have an up-to-date DLL
        foreach (string jar in Directory.GetFiles(_modsDir, "*.jar"))
            EnsureCompiled(jar);

        // Step 2: load all DLLs in compiled/
        foreach (string dll in Directory.GetFiles(_compiledDir, "*.dll"))
            LoadDll(dll);

        Console.WriteLine($"[ModLoader] {_mods.Count(m => m.IsAlive)} mod(s) loaded.");
    }

    // ── Per-tick ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Call at the END of every Engine.FixedUpdate().
    /// Resets AllocGuard pools and flushes deferred cross-thread calls.
    /// </summary>
    public void EndTick()
    {
        FramePool.EndFrame();
        AllocationMonitor.EndFrame();
        TickScheduler.Flush();
    }

    // ── Unload ────────────────────────────────────────────────────────────────

    /// <summary>Unloads all mods gracefully.</summary>
    public void UnloadAll()
    {
        foreach (var mod in _mods.ToList())
            Unload(mod);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    void EnsureCompiled(string jarPath)
    {
        string modName = Path.GetFileNameWithoutExtension(jarPath);
        string dllPath = Path.Combine(_compiledDir, modName + ".dll");

        // Skip if DLL is newer than JAR
        if (File.Exists(dllPath) &&
            File.GetLastWriteTimeUtc(dllPath) >= File.GetLastWriteTimeUtc(jarPath))
        {
            Console.WriteLine($"[ModLoader] {modName} — DLL up to date, skipping compile.");
            return;
        }

        VersionMapping? mapping = VersionDetector.Detect(jarPath);
        if (mapping == null)
        {
            Console.Error.WriteLine(
                $"[ModLoader] Skipping {modName} — unknown Minecraft version.");
            return;
        }

        try
        {
            ModCompiler.Compile(jarPath, mapping);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModLoader] Compile failed for {modName}: {ex.Message}");
        }
    }

    void LoadDll(string dllPath)
    {
        string modId = Path.GetFileNameWithoutExtension(dllPath);

        // Detect version from the DLL's embedded manifest (written by ModCompiler)
        // For now fall back to a placeholder mapping — version is already baked into the DLL
        // at compile time (it was linked against the correct stubs).
        var mapping = new VersionMapping { StubsVersion = "unknown" };

        var entry = new ModEntry(modId, dllPath, mapping);

        var ctx = new AssemblyLoadContext(modId, isCollectible: true);
        Assembly asm;
        try
        {
            asm = ctx.LoadFromAssemblyPath(dllPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModLoader] Failed to load {dllPath}: {ex.Message}");
            return;
        }

        // Step A: Mixin scan + patch — must happen before OnLoad() fires.
        // Uses the same Harmony instance as the sandbox so UnpatchAll() covers both.
        var mixin = new MixinInterceptor(modId, entry.Sandbox.Harmony);
        int mixinCount = mixin.Intercept(asm);
        entry.Mixin = mixin;
        if (mixinCount > 0)
            Console.WriteLine($"[ModLoader] {modId}: {mixinCount} @Mixin class(es) patched.");

        // Also auto-register any [JavaClassName] attributes in this DLL.
        ClassMapping.ScanAssembly(asm);

        // Step B: Locate entry point — ISpectraMod directly, or Forge @Mod wrapper.
        ISpectraMod? mod = ResolveEntryPoint(asm, modId);

        if (mod == null)
        {
            Console.Error.WriteLine(
                $"[ModLoader] No entry point found in {modId} — " +
                "implement ISpectraMod, annotate with @Mod (Forge 1.7.10/1.12/1.16), " +
                "or implement ModInitializer (Fabric 1.16).");
            _mods.Add(entry);
            _contexts[entry] = ctx;
            return;
        }

        entry.DisplayName = mod.DisplayName;
        entry.Version     = mod.Version;

        // Step C: Run OnLoad inside the sandbox.
        bool ok = entry.Sandbox.Execute(() => mod.OnLoad(_engine));
        if (!ok)
            Console.Error.WriteLine($"[ModLoader] OnLoad failed for {modId}.");

        _mods.Add(entry);
        _contexts[entry] = ctx;
        Console.WriteLine($"[ModLoader] Loaded: {mod.DisplayName} v{mod.Version}");
    }

    // ── Entry-point resolution ────────────────────────────────────────────────

    /// <summary>
    /// Resolves the mod entry point from a loaded assembly.
    /// Priority:
    ///   1. Direct ISpectraMod implementation (1.0 transpiler mods)
    ///   2. Forge 1.12 @Mod-annotated class → wrapped in ForgeMod1_12Wrapper
    /// </summary>
    static ISpectraMod? ResolveEntryPoint(Assembly asm, string modId)
    {
        // 1) Direct ISpectraMod
        var directType = asm.GetTypes()
            .FirstOrDefault(t => typeof(ISpectraMod).IsAssignableFrom(t) && !t.IsAbstract);

        if (directType != null)
        {
            try   { return (ISpectraMod?)Activator.CreateInstance(directType); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ModLoader] Failed to instantiate {directType.Name}: {ex.Message}");
                return null;
            }
        }

        // 2) Forge @Mod detection (1.7.10, 1.12, 1.16)
        var forge = TryWrapForgeMod(asm, modId);
        if (forge != null) return forge;

        // 3) Fabric ModInitializer detection (1.16 Fabric)
        return TryWrapFabricMod(asm, modId);
    }

    /// <summary>
    /// Detects Forge @Mod-annotated types and wraps them in the appropriate version
    /// wrapper. Tries all known Forge attribute FQNs:
    ///   - cpw.mods.fml.common.ModAttribute        → ForgeMod1_7Wrapper  (1.7.10)
    ///   - net.minecraftforge.fml.common.ModAttribute → ForgeMod1_16Wrapper (1.16, tried first)
    ///   - net.minecraftforge.fml.common.ModAttribute → ForgeMod1_12Wrapper (1.12, fallback)
    /// </summary>
    static ISpectraMod? TryWrapForgeMod(Assembly asm, string modId)
    {
        // (attr FQN, wrapper FQN) pairs — order matters: 1.7.10 has a unique attr FQN;
        // 1.16 is tried before 1.12 so a 1.16 mod gets the right lifecycle events.
        (string attrFqn, string wrapperFqn)[] variants =
        [
            ("cpw.mods.fml.common.ModAttribute",           "cpw.mods.fml.common.ForgeMod1_7Wrapper"),
            ("net.minecraftforge.fml.common.ModAttribute", "net.minecraftforge.fml.common.ForgeMod1_16Wrapper"),
            ("net.minecraftforge.fml.common.ModAttribute", "net.minecraftforge.fml.common.ForgeMod1_12Wrapper"),
        ];

        foreach (var (attrFqn, wrapperFqn) in variants)
        {
            var result = TryWrapWith(asm, modId, attrFqn, wrapperFqn);
            if (result != null) return result;
        }
        return null;
    }

    /// <summary>
    /// Detects types implementing net.fabricmc.api.ModInitializer and wraps the first
    /// one in FabricModWrapper.
    /// </summary>
    static ISpectraMod? TryWrapFabricMod(Assembly asm, string modId)
    {
        const string ifaceFqn   = "net.fabricmc.api.ModInitializer";
        const string wrapperFqn = "net.fabricmc.api.FabricModWrapper";

        Type? ifaceType = FindTypeAcrossAssemblies(ifaceFqn);
        if (ifaceType == null) return null;

        var fabricModType = asm.GetTypes()
            .FirstOrDefault(t => !t.IsAbstract && ifaceType.IsAssignableFrom(t));
        if (fabricModType == null) return null;

        Type? wrapperType = FindTypeAcrossAssemblies(wrapperFqn);
        if (wrapperType == null)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: FabricModWrapper not found — " +
                "is MinecraftStubs.v1_16.dll present in the Stubs directory?");
            return null;
        }

        object? instance;
        try   { instance = Activator.CreateInstance(fabricModType); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: Failed to create ModInitializer {fabricModType.Name}: " +
                ex.Message);
            return null;
        }

        try   { return (ISpectraMod?)Activator.CreateInstance(wrapperType, [instance]); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: Failed to create FabricModWrapper: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tries to find a type in asm annotated with attrFqn, then wraps it with wrapperFqn.
    /// Returns null if either type isn't loaded or no annotated type is found.
    /// </summary>
    static ISpectraMod? TryWrapWith(Assembly asm, string modId, string attrFqn, string wrapperFqn)
    {
        Type? modAttrType = FindTypeAcrossAssemblies(attrFqn);
        if (modAttrType == null) return null;

        var forgeModTypes = asm.GetTypes()
            .Where(t => t.GetCustomAttributes(modAttrType, false).Length > 0)
            .ToArray();
        if (forgeModTypes.Length == 0) return null;

        if (forgeModTypes.Length > 1)
            Console.WriteLine(
                $"[ModLoader] {modId}: {forgeModTypes.Length} @Mod classes found — loading first only.");

        Type? wrapperType = FindTypeAcrossAssemblies(wrapperFqn);
        if (wrapperType == null)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: Wrapper {wrapperFqn} not found in loaded stubs.");
            return null;
        }

        object? instance;
        try   { instance = Activator.CreateInstance(forgeModTypes[0]); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: Failed to create {forgeModTypes[0].Name}: {ex.Message}");
            return null;
        }

        try   { return (ISpectraMod?)Activator.CreateInstance(wrapperType, [instance]); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModLoader] {modId}: Failed to create wrapper {wrapperFqn}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Searches all loaded assemblies for a type by fully-qualified name.</summary>
    static Type? FindTypeAcrossAssemblies(string fullName)
        => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try   { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.FullName == fullName);

    void Unload(ModEntry entry)
    {
        // Revert all Harmony patches this mod applied
        entry.Sandbox.RevertPatches();

        // Call OnUnload if we have the instance (not tracked here — stubs handle it)

        // Unload the AssemblyLoadContext (GC will collect the assembly)
        if (_contexts.TryGetValue(entry, out var ctx))
        {
            ctx.Unload();
            _contexts.Remove(entry);
        }

        _mods.Remove(entry);
        Console.WriteLine($"[ModLoader] Unloaded: {entry.ModId}");
    }
}
