namespace SpectraEngine.Core.Mods;

/// <summary>
/// Behavior contract a mod stub block exposes so ModBlockBridge can delegate
/// ticks into the stub without knowing its concrete type.
///
/// Implemented by stub Block classes (v1_0, v1_7_10, …). The bridge copies
/// the scalar data properties at construction time, then dispatches ticks via
/// the methods on every call.
/// </summary>
public interface IModBlockBehavior
{
    // ── Scalar data (read once at bridge construction) ────────────────────────

    /// <summary>Hardness passed to Core.Block.SetHardness().</summary>
    float Hardness { get; }

    /// <summary>Raw blast resistance (NOT pre-multiplied; bridge sets BlockResistance directly).</summary>
    float Resistance { get; }

    /// <summary>Light emission as a [0, 1] fraction. Passed to Core.Block.SetLightValue().</summary>
    float LightFraction { get; }

    /// <summary>Light opacity 0–255. Passed to Core.Block.SetLightOpacity().</summary>
    int LightOpacity { get; }

    // ── Tick dispatch ─────────────────────────────────────────────────────────

    /// <summary>
    /// Random tick — maps to Java <c>a(World,x,y,z,Random)</c> / <c>updateTick</c>.
    /// Core calls this when the block's slot is selected for a random tick.
    /// </summary>
    void OnBlockTick(IWorld world, int x, int y, int z, JavaRandom rng);

    /// <summary>
    /// Scheduled tick — maps to Java <c>b(World,x,y,z,Random)</c> / <c>updateTick</c>.
    /// Core calls this when a scheduled update fires.
    /// </summary>
    void OnUpdateTick(IWorld world, int x, int y, int z, JavaRandom rng);
}
