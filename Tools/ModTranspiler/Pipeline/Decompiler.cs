using System.Diagnostics;

namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 1 — Wraps Vineflower as a subprocess.
/// Input:  mod JAR path
/// Output: directory containing decompiled .java files
/// </summary>
static class Decompiler
{
    public static string Run(string jarPath, string tempRoot)
    {
        string modName = Path.GetFileNameWithoutExtension(jarPath);
        string outDir  = Path.Combine(tempRoot, modName);
        Directory.CreateDirectory(outDir);

        // Skip if already decompiled (incremental build support)
        if (Directory.GetFiles(outDir, "*.java", SearchOption.AllDirectories).Length > 0)
        {
            Console.WriteLine($"[Decompiler] Skipping — already decompiled: {outDir}");
            return outDir;
        }

        Console.WriteLine($"[Decompiler] Running Vineflower on {Path.GetFileName(jarPath)}...");

        var psi = new ProcessStartInfo("java",
            $"-jar tools/decompiler/vineflower.jar \"{jarPath}\" \"{outDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            WorkingDirectory       = AppContext.BaseDirectory,
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start java process.");

        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
            throw new Exception($"Vineflower exited with code {proc.ExitCode}:\n{stderr}");

        int count = Directory.GetFiles(outDir, "*.java", SearchOption.AllDirectories).Length;
        Console.WriteLine($"[Decompiler] {count} classes decompiled → {outDir}");
        return outDir;
    }
}
