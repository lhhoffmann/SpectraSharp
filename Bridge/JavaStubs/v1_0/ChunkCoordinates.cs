// AllocGuard Tier 1 — ChunkCoordinates as stack-allocated value type
// Java equivalent: net.minecraft.src.ChunkCoordinates (obf: dn in 1.0)
//
// DESIGN: Java ChunkCoordinates was a heap-allocated tuple used to store
//         a block position (x, y, z as int).  Mods create these constantly
//         for pathfinding, structure generation, and entity targeting.
//         As a readonly record struct, all transient uses stay on the stack.

namespace net.minecraft.src;

/// <summary>
/// MinecraftStubs v1_0 — ChunkCoordinates (obf: dn).
/// Integer triple representing a block-grid position.
///
/// Declared as <c>readonly record struct</c> — stack-allocated, zero GC for
/// transient position calculations.  Value equality matches Java semantics
/// (two positions are equal iff x, y, z all match).
/// </summary>
public readonly record struct ChunkCoordinates(int posX, int posY, int posZ)
    : IComparable<ChunkCoordinates>
{
    // ── Distance helpers ──────────────────────────────────────────────────────

    /// <summary>Squared distance to another position (avoids sqrt).</summary>
    public float getDistanceSquaredToChunkCoordinates(ChunkCoordinates other)
    {
        int dx = other.posX - posX;
        int dy = other.posY - posY;
        int dz = other.posZ - posZ;
        return dx * dx + dy * dy + dz * dz;
    }

    /// <summary>Euclidean distance to another position.</summary>
    public float getDistanceToChunkCoordinates(ChunkCoordinates other) =>
        (float)Math.Sqrt(getDistanceSquaredToChunkCoordinates(other));

    // ── Comparison ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sorts by Y descending (highest position first) — used by pathfinding
    /// priority queues in 1.0 mods.
    /// </summary>
    public int CompareTo(ChunkCoordinates other) => other.posY.CompareTo(posY);

    // ── Mutation helpers (return new value — structs are immutable) ───────────

    /// <summary>Returns a copy with updated coordinates.</summary>
    public ChunkCoordinates set(int x, int y, int z) => new(x, y, z);

    // ── Java interop ──────────────────────────────────────────────────────────

    public override string ToString() => $"[{posX}, {posY}, {posZ}]";
}
