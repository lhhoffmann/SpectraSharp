using System.Reflection;

namespace SpectraEngine.ModRuntime.Interop;

/// <summary>How the mixin attaches to the target method.</summary>
public enum InjectionKind
{
    /// <summary>@Inject — calls mixin code at a specific point.</summary>
    Inject,
    /// <summary>@Overwrite — replaces target method body entirely.</summary>
    Overwrite,
    /// <summary>@Redirect — replaces a specific call site inside the target.</summary>
    Redirect,
    /// <summary>@Accessor — generated getter/setter stub (no runtime patch needed).</summary>
    Accessor,
    /// <summary>@Invoker — generated invoke stub (no runtime patch needed).</summary>
    Invoker,
}

/// <summary>Where in the target method body the injection fires.</summary>
public enum InjectionAt
{
    /// <summary>Before the first instruction (Harmony prefix).</summary>
    Head,
    /// <summary>Before every return instruction (Harmony postfix).</summary>
    Return,
    /// <summary>At a specific call site (Harmony transpiler).</summary>
    Invoke,
    /// <summary>At the tail of the method — treated as Return.</summary>
    Tail,
}

/// <summary>
/// Describes one injection from a @Mixin class onto a target method.
/// Produced by <see cref="MixinScanner"/>, consumed by <see cref="HarmonyBridge"/>.
/// </summary>
public sealed class MixinInjection
{
    /// <summary>Type of injection annotation found on the mixin method.</summary>
    public InjectionKind    Kind           { get; init; }

    /// <summary>Where in the target method to fire (for @Inject).</summary>
    public InjectionAt      At             { get; init; }

    /// <summary>
    /// Simple name of the target method as declared in the annotation.
    /// May be the Java name ("setBlock") — HarmonyBridge matches case-insensitively.
    /// </summary>
    public string           TargetMethod   { get; init; } = "";

    /// <summary>
    /// Java method descriptor "(IIII)Z" — used to disambiguate overloads.
    /// Empty if the annotation did not provide one.
    /// </summary>
    public string           TargetDesc     { get; init; } = "";

    /// <summary>
    /// For @Redirect only: the fully-qualified name of the call site to intercept
    /// (e.g. "net/minecraft/world/World.setBlock(IIII)Z").
    /// </summary>
    public string?          RedirectTarget { get; init; }

    /// <summary>The actual .NET method carrying this injection annotation.</summary>
    public MethodInfo?      MixinMethod    { get; init; }
}

/// <summary>
/// Describes one @Mixin-annotated class found in an IKVM-compiled mod assembly.
/// Contains all injections that class declares.
/// Produced by <see cref="MixinScanner"/>, consumed by <see cref="MixinInterceptor"/>.
/// </summary>
public sealed class MixinDescriptor
{
    /// <summary>Fully-qualified Java class name this mixin targets.</summary>
    public string               TargetJavaClass { get; init; } = "";

    /// <summary>All injection annotations found on this mixin type's methods.</summary>
    public List<MixinInjection> Injections      { get; init; } = [];

    /// <summary>The .NET type carrying the @Mixin annotation.</summary>
    public Type                 MixinType       { get; init; } = null!;
}
