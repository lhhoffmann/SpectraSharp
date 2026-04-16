using System.Numerics;
using Raylib_cs;

namespace SpectraEngine.Core;

/// <summary>
/// Immutable snapshot of a single block's render state (Bridge stubs or World blocks).
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
/// Immutable snapshot of a single entity's render state.
/// </summary>
public sealed record EntityRenderData(
    Vector3 Position,
    Color   RenderColor,
    string  Label
);

/// <summary>
/// Immutable snapshot of the full world state at a single tick boundary.
/// The game thread atomically replaces the engine's current snapshot after
/// every tick batch.  The render thread reads whichever snapshot is current
/// without ever blocking the game thread.
/// </summary>
public sealed record WorldSnapshot(
    IReadOnlyList<BlockRenderData> Blocks,
    IReadOnlyList<EntityRenderData> Entities,
    long TotalTicks,
    long WorldTime,
    float BrightnessSample,
    int   MobHealth,
    int   MobMaxHealth,
    int   LiveEntityCount
)
{
    public static WorldSnapshot Empty { get; } = new([], [], 0, 0, 1.0f, 20, 20, 0);
}
