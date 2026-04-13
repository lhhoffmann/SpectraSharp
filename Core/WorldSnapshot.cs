using System.Numerics;
using Raylib_cs;

namespace SpectraSharp.Core;

/// <summary>
/// Immutable snapshot of a single block's render state.
/// Written by the game thread, read by the render thread — no locks needed.
/// </summary>
public sealed record BlockRenderData(
    Vector3 Position,
    Color   RenderColor,
    string  TextureKey,
    string  JavaClassName,
    long    TickCount
);

/// <summary>
/// Immutable snapshot of the full world state at a single tick boundary.
/// The game thread atomically replaces the engine's current snapshot after
/// every tick batch.  The render thread reads whichever snapshot is current
/// without ever blocking the game thread.
/// </summary>
public sealed record WorldSnapshot(
    IReadOnlyList<BlockRenderData> Blocks,
    long                           TotalTicks
)
{
    public static WorldSnapshot Empty { get; } = new([], 0);
}
