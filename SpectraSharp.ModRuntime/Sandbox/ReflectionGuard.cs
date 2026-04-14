namespace SpectraSharp.ModRuntime.Sandbox;

/// <summary>
/// Intercepts java.lang.reflect calls from IKVM-compiled mod code.
/// Allows reflection on JavaStub fields/methods.
/// Blocks reflection on SpectraSharp.Core internals.
/// </summary>
public static class ReflectionGuard
{
    // Namespaces mods must never reflect into
    static readonly HashSet<string> Blocked =
    [
        "SpectraSharp.Core",
        "SpectraSharp.Graphics",
        "SpectraSharp.IO",
        "SpectraSharp.ModRuntime",
    ];

    // Namespaces always allowed (stub layer + IKVM java.*)
    static readonly HashSet<string> Allowed =
    [
        "net.minecraft",
        "net.minecraftforge",
        "net.fabricmc",
        "java.",
        "javax.",
    ];

    /// <summary>
    /// Returns true if reflection access to <paramref name="targetType"/> is permitted.
    /// Called by IKVM's setAccessible() stub.
    /// </summary>
    public static bool IsAllowed(Type targetType)
    {
        string ns = targetType.Namespace ?? "";

        if (Allowed.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal)))
            return true;

        if (Blocked.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal)))
        {
            Console.Error.WriteLine(
                $"[ReflectionGuard] BLOCKED reflection on {targetType.FullName}");
            return false;
        }

        // Unknown namespace — allow but log (mod-defined classes, third-party libs)
        return true;
    }
}
