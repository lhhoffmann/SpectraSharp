using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using SpectraSharp.Core.Mods;
using SpectraSharp.ModRuntime.AllocGuard;
using SpectraSharp.ModRuntime.Compiler;
using SpectraSharp.ModRuntime.Interop;
using SpectraSharp.ModRuntime.Mappings;
using SpectraSharp.ModRuntime.Sandbox;

namespace SpectraSharp.ModRuntime;

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

        // Step B: Locate ISpectraMod entry point.
        // Fabric/Forge mods use their own entry point annotations — stubs route them to ISpectraMod.
        var modType = asm.GetTypes()
            .FirstOrDefault(t => typeof(ISpectraMod).IsAssignableFrom(t) && !t.IsAbstract);

        if (modType == null)
        {
            Console.Error.WriteLine(
                $"[ModLoader] No ISpectraMod found in {modId} — is the stub entry point wired?");
            _mods.Add(entry);
            _contexts[entry] = ctx;
            return;
        }

        ISpectraMod? mod;
        try { mod = (ISpectraMod?)Activator.CreateInstance(modType); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModLoader] Failed to instantiate {modType.Name}: {ex.Message}");
            return;
        }

        if (mod == null) return;

        entry.DisplayName = mod.DisplayName;
        entry.Version     = mod.Version;

        // Step C: Run OnLoad inside the sandbox.
        bool ok = entry.Sandbox.Execute(() => mod.OnLoad(_engine));
        if (!ok)
        {
            Console.Error.WriteLine($"[ModLoader] OnLoad failed for {modId}.");
        }

        _mods.Add(entry);
        _contexts[entry] = ctx;
        Console.WriteLine($"[ModLoader] Loaded: {mod.DisplayName} v{mod.Version}");
    }

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
