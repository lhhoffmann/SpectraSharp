namespace SpectraEngine.Core;

/// <summary>
/// Bit-exact replica of <c>gv</c> (MovingObjectPosition) — plain data container for
/// ray-cast and entity-intersection results.
///
/// Quirks preserved (see spec §8):
///   - Hit position <see cref="HitVec"/> is a POOLED Vec3 copy — invalid after next
///     Vec3.ResetPool(). Callers that need it beyond the tick must heap-allocate.
///   - Entity-hit: block coordinate fields are left at 0 (Java int default).
///   - AABB.rayTrace always passes (0,0,0) for block coordinates.
///
/// Open dependencies:
///   - Entity (<c>ia</c>) has no spec yet — typed as <c>object</c> placeholder.
///     See REQUESTS.md: Entity.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MovingObjectPosition_Spec.md
/// </summary>
public sealed class MovingObjectPosition
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>Hit type discriminator. obf: a</summary>
    public readonly HitType Type;      // obf: a

    /// <summary>Block X coordinate (block hits only). obf: b</summary>
    public readonly int BlockX;        // obf: b

    /// <summary>Block Y coordinate (block hits only). obf: c</summary>
    public readonly int BlockY;        // obf: c

    /// <summary>Block Z coordinate (block hits only). obf: d</summary>
    public readonly int BlockZ;        // obf: d

    /// <summary>
    /// Face ID of the hit surface (block hits only, 0–5). obf: e
    /// 0 = −Y (bottom), 1 = +Y (top), 2 = −Z (north), 3 = +Z (south),
    /// 4 = −X (west),  5 = +X (east)
    /// </summary>
    public readonly int FaceId;        // obf: e
    public           int Face => FaceId; // C#-style alias for tests

    /// <summary>
    /// Exact hit position — a POOLED Vec3 copy (quirk 1). obf: f
    /// Invalid after the next Vec3.ResetPool() call.
    /// </summary>
    public readonly Vec3 HitVec;       // obf: f

    /// <summary>
    /// The entity that was hit (entity hits only; null for block hits). obf: g
    /// Typed as object — Entity (ia) spec pending. See REQUESTS.md.
    /// </summary>
    public readonly object? Entity;    // obf: g  — placeholder until Entity spec arrives

    // ── Constructors (spec §5) ────────────────────────────────────────────────

    /// <summary>
    /// Block-hit constructor. Spec: <c>gv(int blockX, int blockY, int blockZ, int faceId, fb hitPoint)</c>.
    /// Hit position is stored as a pooled Vec3 copy (quirk 1 — do not change to heap alloc).
    /// </summary>
    public MovingObjectPosition(int blockX, int blockY, int blockZ, int faceId, Vec3 hitPoint)
    {
        Type   = HitType.Tile;
        BlockX = blockX;
        BlockY = blockY;
        BlockZ = blockZ;
        FaceId = faceId;
        HitVec = Vec3.GetFromPool(hitPoint.X, hitPoint.Y, hitPoint.Z); // pooled copy (spec §5)
        Entity = null; // Java int default: g left null
    }

    /// <summary>
    /// Entity-hit constructor. Spec: <c>gv(ia var1)</c>.
    /// Block coordinate fields remain 0 (Java int default, quirk 2).
    /// Entity typed as object until Entity (ia) spec arrives.
    /// </summary>
    public MovingObjectPosition(object entity, double entityX, double entityY, double entityZ)
    {
        Type   = HitType.Entity;
        Entity = entity;
        HitVec = Vec3.GetFromPool(entityX, entityY, entityZ); // pooled copy of entity position
        // BlockX, BlockY, BlockZ, FaceId remain 0 (quirk 2)
    }
}
