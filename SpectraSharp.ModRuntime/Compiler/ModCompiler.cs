using System.Diagnostics;
using System.IO.Compression;
using SpectraSharp.ModRuntime.Mappings;

namespace SpectraSharp.ModRuntime.Compiler;

/// <summary>
/// Compiles a mod JAR to a .NET DLL using ikvmc (IKVM's JAR→.NET compiler).
///
/// Pipeline per mod:
///   1. VersionDetector determines the MC version
///   2. We verify the JAR's Java bytecode version is supported by IKVM 8.x (≤ Java 8 / class 52)
///   3. We select the correct MinecraftStubs.{ver}.dll to link against
///   4. ikvmc compiles the JAR referencing our stubs + IKVM.Runtime
///   5. Output: mods/compiled/{ModName}.dll
///
/// Java version limit:
///   IKVM 8.x supports class-file major version ≤ 52 (Java 8).
///   Minecraft 1.17+ requires Java 16+ (class 60), 1.21 requires Java 21 (class 65).
///   These versions cannot be compiled by ikvmc 8.x.
///   Track: https://github.com/ikvm-revived/ikvm — IKVM 9.x adds Java 11+ support.
///   Until then, ModCompiler rejects JARs with unsupported bytecode versions early.
/// </summary>
public static class ModCompiler
{
    /// <summary>Directory where compiled mod DLLs are written.</summary>
    public static string OutputDir { get; set; } = "mods/compiled";

    /// <summary>Directory containing MinecraftStubs.{ver}.dll files.</summary>
    public static string StubsDir  { get; set; } = "Bridge/JavaStubs/bin";

    // Maximum Java class-file major version that IKVM 8.x can process.
    // 52 = Java 8.  Java 9 = 53, Java 16 = 60, Java 21 = 65.
    const int MaxSupportedClassVersion = 52;

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

        // Fail early with a clear message if the JAR targets a Java version IKVM 8.x cannot handle.
        int classVersion = DetectJavaBytecodeVersion(jarPath);
        if (classVersion > MaxSupportedClassVersion)
        {
            int javaVersion = classVersion - 44; // class major - 44 = Java version (Java 8 = 52-44=8)
            throw new NotSupportedException(
                $"[ModCompiler] {modName} requires Java {javaVersion} (class file version {classVersion}).\n" +
                $"IKVM 8.x only supports up to Java 8 (class file version {MaxSupportedClassVersion}).\n" +
                $"Minecraft 1.17+ mods require IKVM 9.x (Java 11–21 support) — not yet stable.\n" +
                $"Track: https://github.com/ikvm-revived/ikvm");
        }

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
        // IKVM NuGet places ikvmc in the package tools folder.
        // We search all installed IKVM versions under ~/.nuget/packages/ikvm/
        // so no hardcoded version number is needed.
        string nugetIkvm = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages", "ikvm");

        if (Directory.Exists(nugetIkvm))
        {
            // Pick the highest installed version (last lexicographic — works for SemVer).
            foreach (string ver in Directory.GetDirectories(nugetIkvm).OrderDescending())
            {
                // ikvmc ships as both a .exe (Windows) and a cross-platform tool.
                string[] candidates =
                [
                    Path.Combine(ver, "tools", "net8.0",  "any", "ikvmc.exe"),
                    Path.Combine(ver, "tools", "net8.0",  "any", "ikvmc"),
                    Path.Combine(ver, "tools", "net8.0",  "ikvmc.exe"),
                    Path.Combine(ver, "tools", "net8.0",  "ikvmc"),
                ];
                foreach (string c in candidates)
                    if (File.Exists(c)) return c;
            }
        }

        // Local tools/ override
        string localTool = Path.GetFullPath("tools/ikvmc/ikvmc.exe");
        if (File.Exists(localTool)) return localTool;

        // Last resort: assume ikvmc is on PATH
        return "ikvmc";
    }

    // ── Bytecode version detection ────────────────────────────────────────────

    /// <summary>
    /// Reads the Java class-file major version of the first .class file in the JAR.
    /// Java class files start with: 0xCAFEBABE (4 bytes) | minor (2) | major (2).
    /// Returns 0 if no class files are found (resource-only JAR).
    /// </summary>
    static int DetectJavaBytecodeVersion(string jarPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(jarPath);

            var classEntry = zip.Entries
                .FirstOrDefault(e => e.FullName.EndsWith(".class",
                    StringComparison.OrdinalIgnoreCase));

            if (classEntry == null) return 0;

            using var stream = classEntry.Open();
            Span<byte> header = stackalloc byte[8];
            if (stream.Read(header) < 8) return 0;

            // Bytes 0–3: magic 0xCAFEBABE
            if (header[0] != 0xCA || header[1] != 0xFE ||
                header[2] != 0xBA || header[3] != 0xBE)
                return 0;

            // Bytes 4–5: minor version (ignored)
            // Bytes 6–7: major version (big-endian)
            int major = (header[6] << 8) | header[7];
            int javaVer = major - 44;
            Console.WriteLine(
                $"[ModCompiler] JAR class-file version: {major} (Java {javaVer})");
            return major;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModCompiler] Could not detect bytecode version: {ex.Message}");
            return 0; // assume compatible
        }
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
