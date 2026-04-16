namespace SpectraEngine.Core.AI;

/// <summary>
/// Replica of <c>mo</c> (PathPoint) — A* graph node used by <see cref="PathFinder"/>.
///
/// Stores block coordinates, heap position, g/h/f costs, parent link, and closed flag.
/// Instances are deduplicated by <c>PathNodeCache</c> — only one PathPoint per (x,y,z).
///
/// Quirks preserved (spec §3):
///   1. Hash key includes sign bit packing to handle negative coords (spec §3 mo.a formula).
///   2. HeapIndex = -1 means the node is not currently in the open set.
///   3. f-cost (TotalCost) = g-cost (PathCost) + h-cost (HeuristicCost).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §3
/// </summary>
public sealed class PathPoint
{
    // ── Coordinates (spec §3) ────────────────────────────────────────────────

    /// <summary>obf: a — X block coordinate.</summary>
    public readonly int X;

    /// <summary>obf: b — Y block coordinate.</summary>
    public readonly int Y;

    /// <summary>obf: c — Z block coordinate.</summary>
    public readonly int Z;

    // ── A* state ─────────────────────────────────────────────────────────────

    /// <summary>obf: d — index in PathHeap.a[]; -1 if not in open set (quirk 2).</summary>
    public int HeapIndex = -1;

    /// <summary>obf: e — g-cost: total path cost from start node to this node.</summary>
    public float PathCost;

    /// <summary>obf: f — h-cost: heuristic (Euclidean distance to target).</summary>
    public float HeuristicCost;

    /// <summary>obf: g — f-cost = PathCost + HeuristicCost. Key for heap ordering (quirk 3).</summary>
    public float TotalCost;

    /// <summary>obf: h — parent node. Null on start node. Used in path reconstruction.</summary>
    public PathPoint? Parent;

    /// <summary>obf: i — closed flag: true once this node has been expanded by A*.</summary>
    public bool Closed;

    // ── Packed hash key (spec §3 — mo.a static hash) ─────────────────────────

    /// <summary>
    /// obf: j — packed hash key computed once at construction (immutable).
    /// Formula: (y &amp; 0xFF) | ((x &amp; 32767) &lt;&lt; 8) | ((z &amp; 32767) &lt;&lt; 24)
    ///           | (x &lt; 0 ? int.MinValue : 0) | (z &lt; 0 ? 32768 : 0)
    /// </summary>
    public readonly int HashKey;

    // ── Constructor ───────────────────────────────────────────────────────────

    public PathPoint(int x, int y, int z)
    {
        X       = x;
        Y       = y;
        Z       = z;
        HashKey = ComputeHash(x, y, z);
    }

    // ── Hash formula (spec §3 — mo.a(int,int,int)) ───────────────────────────

    /// <summary>
    /// obf: static <c>mo.a(int x, int y, int z)</c> — packs coordinates into a single int key.
    /// Handles negative coords via sign-bit packing (quirk 1).
    /// </summary>
    public static int ComputeHash(int x, int y, int z)
        => (y & 0xFF) | ((x & 32767) << 8) | ((z & 32767) << 24)
           | (x < 0 ? int.MinValue : 0)
           | (z < 0 ? 32768 : 0);

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>mo.a(mo other)</c> — Euclidean distance to <paramref name="other"/>.
    /// Used for h-cost (heuristic) and step-cost calculation.
    /// </summary>
    public float EuclideanDistanceTo(PathPoint other)
    {
        float dx = X - other.X, dy = Y - other.Y, dz = Z - other.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// obf: <c>mo.a()</c> (boolean) — returns true when node is currently in the heap
    /// (HeapIndex &gt;= 0).
    /// </summary>
    public bool IsInHeap() => HeapIndex >= 0;

    public override string ToString() => $"PathPoint({X},{Y},{Z} g={PathCost:F2} f={TotalCost:F2})";
}
