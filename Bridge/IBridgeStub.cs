namespace SpectraSharp.Bridge;

// ─────────────────────────────────────────────────────────────────────────────
//  SpectraSharp.Bridge — AOT Transpiler Scaffold
//
//  PURPOSE
//  -------
//  This namespace is the landing zone for Java→C# transpiled output.
//  Every class emitted by the transpiler should implement IBridgeStub so the
//  engine can discover, validate, and wire it up without reflection.
//
//  WORKFLOW
//  --------
//  1. The transpiler maps a source Java class (e.g. net.minecraft.src.Block)
//     to an equivalent C# implementation using its fully-qualified name as key.
//  2. It emits a C# file into Bridge/Generated/ implementing IBridgeStub.
//  3. The BridgeRegistry scans all IBridgeStub types at startup and registers
//     them with the appropriate Core subsystem.
//  4. Hand-written overrides live in Bridge/Overrides/ — they take precedence
//     over generated stubs via the Priority property.
//
//  NAMING CONVENTION
//  -----------------
//  Generated files:  Bridge/Generated/Compat_<JavaSimpleName>.cs
//  Override files:   Bridge/Overrides/<JavaSimpleName>.cs
//
//  NOTE ON JAVA CLASS NAMES
//  ------------------------
//  The strings stored in JavaClassName (e.g. "net.minecraft.src.Block") are
//  purely technical interop keys — equivalent to POSIX API names or NTFS
//  filesystem labels.  They are not user-visible branding and do not constitute
//  use of any trademark in a commercial context.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Marker interface that every transpiled (or hand-written override) Java class
/// must implement.  Gives the engine a common handle without requiring reflection.
/// </summary>
public interface IBridgeStub
{
    /// <summary>
    /// Fully qualified Java class name this stub represents.
    /// Example: <c>"net.minecraft.src.Block"</c>
    /// </summary>
    string JavaClassName { get; }

    /// <summary>
    /// Stub priority.  Higher values win when two stubs share the same
    /// <see cref="JavaClassName"/> (e.g. a hand-written override beats a
    /// generated stub).
    /// </summary>
    int Priority => 0;
}

/// <summary>
/// Optional base class for transpiled stubs that need lifecycle callbacks.
/// Derive from this if the Java class has <c>init()</c> / <c>tick()</c> patterns.
/// </summary>
public abstract class BridgeStubBase : IBridgeStub
{
    public abstract string JavaClassName { get; }

    /// <summary>
    /// Stub priority. Virtual so subclasses (e.g. <c>BlockBase</c>) can
    /// override it without hitting CS0115.  Defaults to 0 (generated stubs).
    /// </summary>
    public virtual int Priority => 0;

    /// <summary>Called once after the engine has finished booting.</summary>
    public virtual void OnEngineReady() { }

    /// <summary>
    /// Called every fixed tick (20 Hz).
    /// Mirrors Java's <c>updateTick</c> / <c>randomTick</c> patterns.
    /// </summary>
    public virtual void OnTick(double deltaSeconds) { }
}

/// <summary>
/// Scans all loaded assemblies for <see cref="IBridgeStub"/> implementations
/// and provides a keyed lookup by Java class name.
/// Respects <see cref="IBridgeStub.Priority"/> so overrides win automatically.
/// </summary>
public sealed class BridgeRegistry
{
    private readonly Dictionary<string, IBridgeStub> _stubs = [];

    public BridgeRegistry(IEnumerable<IBridgeStub> stubs)
    {
        foreach (IBridgeStub stub in stubs)
        {
            if (!_stubs.TryGetValue(stub.JavaClassName, out IBridgeStub? existing)
                || stub.Priority > existing.Priority)
            {
                _stubs[stub.JavaClassName] = stub;
                Console.WriteLine($"[Bridge] registered: {stub.JavaClassName} (priority {stub.Priority})");
            }
        }
    }

    /// <summary>Returns the registered stub for a Java class name, or null.</summary>
    public IBridgeStub? TryGet(string javaClassName)
        => _stubs.GetValueOrDefault(javaClassName);

    /// <summary>All registered stubs (one per Java class name, highest priority wins).</summary>
    public IEnumerable<IBridgeStub> AllStubs => _stubs.Values;

    /// <summary>Total number of registered stubs.</summary>
    public int Count => _stubs.Count;
}
