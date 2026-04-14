using System.Reflection;

namespace SpectraSharp.ModRuntime.Interop;

/// <summary>
/// Maps Java/Minecraft class names (all versions, obfuscated and named) to
/// live SpectraSharp .NET types at runtime.
///
/// Used by MixinInterceptor to find the .NET type a @Mixin annotation targets,
/// and by MinecraftStubs to verify routing at load time.
///
/// Registration happens in two ways:
///   1. Static: known Core types registered at startup via Register()
///   2. Dynamic: stub assemblies self-register via [JavaClassName] attribute
/// </summary>
public static class ClassMapping
{
    // Java class name (fully-qualified, any version) → .NET Type
    static readonly Dictionary<string, Type> _map =
        new(StringComparer.Ordinal);

    // Also index by short name ("World", "Block") for convenience
    static readonly Dictionary<string, Type> _shortMap =
        new(StringComparer.Ordinal);

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a Java class name → .NET type mapping.
    /// Call from stub static constructors or ModRuntime startup.
    /// </summary>
    public static void Register(string javaName, Type csType)
    {
        _map[javaName] = csType;

        // Also register short name ("net.minecraft.world.World" → "World")
        string short_ = javaName.Contains('.')
            ? javaName[(javaName.LastIndexOf('.') + 1)..]
            : javaName;
        _shortMap.TryAdd(short_, csType);
    }

    /// <summary>
    /// Scans an assembly for types carrying [JavaClassName] attribute and
    /// auto-registers them. Called when a stub DLL or mod DLL is loaded.
    /// </summary>
    public static void ScanAssembly(Assembly asm)
    {
        foreach (var type in asm.GetTypes())
        {
            var attr = type.GetCustomAttribute<JavaClassNameAttribute>();
            if (attr != null)
                Register(attr.JavaName, type);
        }
    }

    // ── Resolution ────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a Java class name to a .NET type.
    /// Tries fully-qualified name first, then short name, then obfuscated 1.0 name.
    /// Returns null if unknown — callers must handle gracefully (log + skip).
    /// </summary>
    public static Type? Resolve(string javaName)
    {
        if (_map.TryGetValue(javaName, out var t))  return t;
        if (_shortMap.TryGetValue(javaName, out t)) return t;

        // Try to find a SpectraSharp.Core type whose name matches the short name
        // (e.g. "World" → SpectraSharp.Core.World)
        string shortName = javaName.Contains('.')
            ? javaName[(javaName.LastIndexOf('.') + 1)..]
            : javaName;

        var coreType = typeof(SpectraSharp.Core.IWorld).Assembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == shortName || t.Name == "I" + shortName);

        if (coreType != null)
        {
            Register(javaName, coreType); // cache for next time
            return coreType;
        }

        return null;
    }

    // ── Startup registration ──────────────────────────────────────────────────

    /// <summary>
    /// Registers all well-known SpectraSharp.Core types.
    /// Call once at engine startup before any mod loads.
    /// </summary>
    public static void RegisterCoreTypes()
    {
        // 1.0 obfuscated → Core type
        Register("ry",  typeof(SpectraSharp.Core.IWorld));
        Register("yy",  typeof(SpectraSharp.Core.Block));
        Register("kq",  typeof(SpectraSharp.Core.IBlockAccess));

        // Human-readable 1.0–1.7 names → Core type
        Register("net.minecraft.world.World",       typeof(SpectraSharp.Core.IWorld));
        Register("net.minecraft.block.Block",        typeof(SpectraSharp.Core.Block));
        Register("net.minecraft.world.IBlockAccess", typeof(SpectraSharp.Core.IBlockAccess));
        Register("net.minecraft.entity.Entity",      typeof(SpectraSharp.Core.Entity));

        // 1.12 names
        Register("net.minecraft.world.WorldServer",  typeof(SpectraSharp.Core.IWorld));
        Register("net.minecraft.world.chunk.Chunk",  typeof(SpectraSharp.Core.Chunk));

        // 1.17+ renamed
        Register("net.minecraft.world.level.Level",          typeof(SpectraSharp.Core.IWorld));
        Register("net.minecraft.world.level.block.Block",    typeof(SpectraSharp.Core.Block));
        Register("net.minecraft.world.level.chunk.LevelChunk", typeof(SpectraSharp.Core.Chunk));
    }
}

/// <summary>
/// Attribute for stub/Core types to self-declare their Java identity.
/// Placed on a C# class to say "I am the stub for this Java class."
/// ClassMapping.ScanAssembly() picks these up automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public sealed class JavaClassNameAttribute(string javaName) : Attribute
{
    public string JavaName { get; } = javaName;
}
