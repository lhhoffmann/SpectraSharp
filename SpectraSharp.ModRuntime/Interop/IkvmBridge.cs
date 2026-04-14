using System.Reflection;
using System.Runtime.Loader;

namespace SpectraSharp.ModRuntime.Interop;

/// <summary>
/// Prepares the IKVM.Runtime environment for mod hosting.
///
/// Must be called ONCE, before any IKVM-compiled mod assembly is loaded.
/// <see cref="ModLoader.LoadAll"/> calls this at engine startup.
///
/// Responsibilities:
///   1. Apply IKVM configuration via reflection (internal API — best-effort).
///      - RelaxedVerification   : accept mods that break Java bytecode rules.
///      - DisableEagerClassLoading: don't resolve the full class hierarchy at load.
///      IKVM auto-initialises on first Java type access, so explicit Init() is
///      not required; the flags merely tune behaviour before that first access.
///   2. Load MinecraftStubs DLLs into the Default ALC so that IKVM-compiled mod
///      assemblies can resolve <c>net.minecraft.*</c> type references.
///   3. Prime <see cref="ClassMapping"/> with all Core types and scan loaded stubs
///      for <c>[JavaClassName]</c> attributes.
///
/// Thread safety: single-threaded engine startup only.
/// </summary>
public static class IkvmBridge
{
    static bool _initialized;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the mod-hosting environment.
    ///
    /// <paramref name="stubsDir"/> is the directory that contains
    /// <c>MinecraftStubs.*.dll</c> files (default: <c>AppContext.BaseDirectory/Stubs/</c>).
    /// ModCompiler outputs stubs here after linking a mod JAR.
    /// </summary>
    public static void Initialize(string stubsDir)
    {
        if (_initialized)
        {
            Console.WriteLine("[IkvmBridge] Already initialised — skipping.");
            return;
        }

        Console.WriteLine("[IkvmBridge] Configuring IKVM runtime…");
        ConfigureJvm();

        Console.WriteLine("[IkvmBridge] Loading MinecraftStubs assemblies…");
        LoadStubs(stubsDir);

        Console.WriteLine("[IkvmBridge] Priming ClassMapping…");
        ClassMapping.RegisterCoreTypes();

        _initialized = true;
        Console.WriteLine("[IkvmBridge] Ready.");
    }

    /// <summary>True after <see cref="Initialize"/> has completed.</summary>
    public static bool IsReady => _initialized;

    // ── IKVM configuration ────────────────────────────────────────────────────

    /// <summary>
    /// Sets internal IKVM.Runtime.JVM configuration fields via reflection.
    ///
    /// These fields are internal in IKVM 8.x and not exposed by the ref assembly.
    /// Failure is non-fatal: IKVM defaults are conservative but functional.
    /// </summary>
    static void ConfigureJvm()
    {
        const BindingFlags staticFields =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        // Locate IKVM.Runtime in already-loaded assemblies (IKVM package adds it).
        var ikvmAsm = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "IKVM.Runtime");

        if (ikvmAsm == null)
        {
            Console.WriteLine(
                "[IkvmBridge] IKVM.Runtime not yet loaded — configuration deferred to first use.");
            return;
        }

        var jvmType = ikvmAsm.GetType("IKVM.Runtime.JVM");
        if (jvmType == null)
        {
            Console.Error.WriteLine("[IkvmBridge] IKVM.Runtime.JVM type not found — skipping config.");
            return;
        }

        SetField(jvmType, "RelaxedVerification",    staticFields, true,
            "relaxed bytecode verification (allows mods that break Java spec)");

        SetField(jvmType, "DisableEagerClassLoading", staticFields, true,
            "lazy class loading (avoids resolving full Minecraft hierarchy at load time)");
    }

    static void SetField(Type type, string fieldName, BindingFlags flags, object value, string description)
    {
        var field = type.GetField(fieldName, flags);
        if (field == null)
        {
            Console.Error.WriteLine($"[IkvmBridge] Field JVM.{fieldName} not found — skipping.");
            return;
        }

        try
        {
            field.SetValue(null, value);
            Console.WriteLine($"[IkvmBridge] JVM.{fieldName} = {value} ({description})");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[IkvmBridge] Could not set JVM.{fieldName}: {ex.Message}");
        }
    }

    // ── Stub loading ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads every <c>MinecraftStubs.*.dll</c> in <paramref name="stubsDir"/>
    /// into the Default ALC and registers their <c>[JavaClassName]</c> types.
    /// </summary>
    static void LoadStubs(string stubsDir)
    {
        if (!Directory.Exists(stubsDir))
        {
            Console.WriteLine(
                $"[IkvmBridge] Stubs directory not found: {stubsDir}" +
                " — no stubs loaded. Mods that reference Minecraft classes will fail at runtime.");
            return;
        }

        var defaultAlc = AssemblyLoadContext.Default;
        int loaded = 0;

        foreach (string dll in Directory.GetFiles(stubsDir, "MinecraftStubs.*.dll"))
        {
            string name = Path.GetFileName(dll);
            try
            {
                // Must load into Default ALC — IKVM-compiled mod DLLs are also in Default ALC
                // and type identity requires the same ALC.
                Assembly asm = defaultAlc.LoadFromAssemblyPath(dll);
                ClassMapping.ScanAssembly(asm);
                Console.WriteLine(
                    $"[IkvmBridge] Loaded {name} ({asm.GetTypes().Length} types)");
                loaded++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[IkvmBridge] Failed to load {name}: {ex.Message}");
            }
        }

        if (loaded == 0)
        {
            Console.WriteLine(
                "[IkvmBridge] No MinecraftStubs DLLs found in stubs directory." +
                " ModCompiler must run first.");
        }
    }
}
