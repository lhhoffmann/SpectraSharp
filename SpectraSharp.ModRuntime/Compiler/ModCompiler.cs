using System.Diagnostics;
using SpectraSharp.ModRuntime.Mappings;

namespace SpectraSharp.ModRuntime.Compiler;

/// <summary>
/// Compiles a mod JAR to a .NET DLL using ikvmc (IKVM's JAR→.NET compiler).
///
/// Pipeline per mod:
///   1. VersionDetector determines the MC version
///   2. We select the correct MinecraftStubs.{ver}.dll to link against
///   3. ikvmc compiles the JAR referencing our stubs + IKVM.Runtime
///   4. Output: mods/compiled/{ModName}.dll
///
/// ikvmc is part of the IKVM NuGet package and is located in the package tools folder.
/// </summary>
public static class ModCompiler
{
    /// <summary>Directory where compiled mod DLLs are written.</summary>
    public static string OutputDir { get; set; } = "mods/compiled";

    /// <summary>Directory containing MinecraftStubs.{ver}.dll files.</summary>
    public static string StubsDir  { get; set; } = "Bridge/JavaStubs/bin";

    /// <summary>
    /// Compiles <paramref name="jarPath"/> to a .dll.
    /// Returns the output DLL path, or throws on failure.
    /// </summary>
    public static string Compile(string jarPath, VersionMapping mapping)
    {
        string modName  = Path.GetFileNameWithoutExtension(jarPath);
        string stubsDll = Path.GetFullPath(
            Path.Combine(StubsDir, $"MinecraftStubs.{mapping.StubsVersion}.dll"));
        string outDll   = Path.GetFullPath(
            Path.Combine(OutputDir, $"{modName}.dll"));

        Directory.CreateDirectory(OutputDir);

        if (!File.Exists(stubsDll))
            throw new FileNotFoundException(
                $"MinecraftStubs not found for version {mapping.StubsVersion}.\n" +
                $"Expected: {stubsDll}\n" +
                $"Build Bridge/JavaStubs/{mapping.StubsVersion}/ first.");

        string ikvmc = FindIkvmc();

        // ikvmc arguments:
        //   -target:library              → produce a DLL
        //   -out:<path>                  → output path
        //   -r:<stubs>                   → reference our MinecraftStubs
        //   -r:<ikvm_runtime>            → reference IKVM.Runtime (java.* classes)
        //   -nowarn:0109                 → suppress "specified assembly not referenced" noise
        string ikvmRuntime = FindIkvmRuntime();
        string args = $"-target:library " +
                      $"-out:\"{outDll}\" " +
                      $"-r:\"{stubsDll}\" " +
                      $"-r:\"{ikvmRuntime}\" " +
                      $"-nowarn:0109 " +
                      $"\"{Path.GetFullPath(jarPath)}\"";

        Console.WriteLine($"[ModCompiler] Compiling {modName} with stubs {mapping.StubsVersion}...");

        var psi = new ProcessStartInfo(ikvmc, args)
        {
            RedirectStandardOutput = false,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ikvmc.");

        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        string stderr = stderrTask.Result;

        if (proc.ExitCode != 0)
            throw new Exception($"ikvmc failed (exit {proc.ExitCode}):\n{stderr}");

        Console.WriteLine($"[ModCompiler] Built → {outDll}");
        return outDll;
    }

    // ── Tool discovery ────────────────────────────────────────────────────────

    static string FindIkvmc()
    {
        // IKVM NuGet places ikvmc.exe in the package tools folder.
        // On NuGet restore, the path is predictable relative to the project.
        string[] candidates =
        [
            // Direct NuGet tools path (global packages)
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages", "ikvm", "8.9.0", "tools", "net8.0", "ikvmc.exe"),
            // Local build output (if ikvmc is copied to tools/)
            Path.GetFullPath("tools/ikvmc/ikvmc.exe"),
            // PATH
            "ikvmc",
        ];

        foreach (string c in candidates)
            if (c == "ikvmc" || File.Exists(c)) return c;

        throw new FileNotFoundException(
            "ikvmc not found. Install the IKVM NuGet package or add ikvmc to PATH.");
    }

    static string FindIkvmRuntime()
    {
        // IKVM.Runtime.dll is a NuGet package reference — .NET resolves it to the output dir.
        string candidate = Path.Combine(AppContext.BaseDirectory, "IKVM.Runtime.dll");
        if (File.Exists(candidate)) return candidate;

        // Fallback: locate via the loaded assembly
        var runtimeAsm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "IKVM.Runtime");
        if (runtimeAsm?.Location is { Length: > 0 } loc) return loc;

        throw new FileNotFoundException(
            "IKVM.Runtime.dll not found. Ensure the IKVM NuGet package is referenced.");
    }
}
