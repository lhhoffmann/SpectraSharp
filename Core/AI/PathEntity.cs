namespace SpectraEngine.Core.AI;

/// <summary>
/// Replica of <c>dw</c> (PathEntity) — an ordered array of <see cref="PathPoint"/> nodes
/// representing a completed A* path from start to target.
///
/// The entity AI advances through the path by calling <see cref="Advance"/> each time
/// a waypoint is reached, and checks <see cref="IsDone"/> to detect path exhaustion.
///
/// Fields (spec §3):
///   b = PathPoint[] nodes array  (start → target, index 0 = start)
///   a = total node count         (= b.length)
///   c = current position index   (advances via Advance())
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §7
/// </summary>
public sealed class PathEntity
{
    // ── Fields ────────────────────────────────────────────────────────────────

    /// <summary>obf: b — ordered array of nodes from start to target.</summary>
    private readonly PathPoint[] _nodes;

    /// <summary>obf: a — total number of nodes in the path.</summary>
    private readonly int _count;

    /// <summary>obf: c — index of the current waypoint (0-based).</summary>
    private int _index;

    // ── Constructor ───────────────────────────────────────────────────────────

    internal PathEntity(PathPoint[] nodes)
    {
        _nodes = nodes;
        _count = nodes.Length;
        _index = 0;
    }

    // ── Navigation API ────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>dw.a(ia entity)</c> — returns the current waypoint as a <see cref="Vec3"/>,
    /// offset by half the entity's occupied footprint width so the mob aims at block centre.
    ///
    /// halfWidth = (int)(entity.Width + 1.0F) * 0.5
    /// Returns null when path is exhausted.
    /// </summary>
    public Vec3? GetCurrentWaypoint(Entity entity)
    {
        if (IsDone) return null;

        PathPoint node = _nodes[_index];
        float halfWidth = (int)(entity.Width + 1.0f) * 0.5f;
        return Vec3.GetFromPool(node.X + halfWidth, node.Y, node.Z + halfWidth);
    }

    /// <summary>
    /// obf: <c>dw.a()</c> (void) — advance to the next waypoint node.
    /// </summary>
    public void Advance() => _index++;

    /// <summary>
    /// obf: <c>dw.b()</c> — returns true when <see cref="_index"/> has reached or passed
    /// the end of the path array (path exhausted).
    /// </summary>
    public bool IsDone => _index >= _count;
}
