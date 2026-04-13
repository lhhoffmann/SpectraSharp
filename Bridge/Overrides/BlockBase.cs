using System.Numerics;

namespace SpectraSharp.Bridge.Overrides;

/// <summary>
/// Abstract base for every hand-written Block override in the compatibility layer.
/// Sits between <see cref="BridgeStubBase"/> and a concrete block class,
/// adding the properties common to all parity blocks:
/// texture atlas index, world position, and tick counter.
///
/// Hand-written overrides always have <see cref="IBridgeStub.Priority"/> = 10,
/// so they beat any generated stub at priority 0.
/// </summary>
public abstract class BlockBase : BridgeStubBase
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override int Priority => 10;

    // ── Texture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat tile index into terrain.png (left-to-right, top-to-bottom).
    /// Index 0 = grass top, 1 = stone, 2 = dirt, …
    /// </summary>
    public abstract int TextureIndex { get; }

    /// <summary>
    /// Stable key used to look up this block's tile inside
    /// <see cref="SpectraSharp.Graphics.TextureRegistry"/>.
    /// Derived automatically from <see cref="TextureIndex"/>.
    /// </summary>
    public string TextureKey => $"block_{TextureIndex}";

    // ── Rendering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat colour used by the Engine's DrawCube pass until a full material
    /// pipeline exists.  Override in concrete blocks to match the vanilla palette.
    /// </summary>
    public virtual Raylib_cs.Color RenderColor => new(200, 200, 200, 255);

    // ── World state ───────────────────────────────────────────────────────────

    /// <summary>Block's position in world space (block-grid coordinates).</summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    // ── Tick accounting ───────────────────────────────────────────────────────

    /// <summary>
    /// Total number of fixed ticks received since the engine started.
    /// Incremented inside <see cref="OnTick"/> — use this to verify 20 Hz parity.
    /// </summary>
    public long TickCount { get; private set; }

    /// <inheritdoc/>
    public override void OnTick(double deltaSeconds)
    {
        TickCount++;
        BlockTick(deltaSeconds);
    }

    /// <summary>
    /// Override this in concrete blocks to implement per-tick behaviour
    /// (random updates, scheduled block changes, etc.).
    /// <paramref name="deltaSeconds"/> is always exactly 0.05 s (20 Hz).
    /// </summary>
    protected virtual void BlockTick(double deltaSeconds) { }
}
