namespace SpectraSharp.Core;

/// <summary>
/// Interface for <c>ry</c> (World) — the mutable world, extending the read-only
/// <see cref="IBlockAccess"/> with block writes, entity spawning, tick scheduling,
/// and client/server discrimination.
///
/// Confirmed contracts (World_Spec.md §3, §6, §10, §13):
///   I   → bool        : isClientSide
///   w   → JavaRandom  : world random (seeded)
///   a(entity)         : spawnEntityInWorld
///   b(x,y,z,id,meta)  : setBlockAndMetadata
///   d(x,y,z,id)       : setBlock (no meta, clears to 0)
///   c(x,y,z,meta)     : setMetadata
///   a(x,y,z,id,delay) : scheduleBlockUpdate
///   e(x,y,z,radius)   : isAreaLoaded
/// </summary>
public interface IWorld : IBlockAccess
{
    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>True on the client side; block drops and entity spawns are server-only.</summary>
    bool IsClientSide { get; }

    /// <summary>Shared world random. Callers must use the reference immediately (quirk 1).</summary>
    JavaRandom Random { get; }

    // ── Entity spawning ────────────────────────────────────────────────────────

    /// <summary>
    /// Adds an entity to the world. Spec: <c>a(ia entity)</c> → bool (returns false if chunk
    /// not loaded for non-player entities).
    /// </summary>
    void SpawnEntity(Entity entity);

    // ── Block writes ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sets block ID and metadata, propagates light, notifies neighbours.
    /// Spec: <c>b(int x, int y, int z, int blockId, int meta)</c> → bool.
    /// </summary>
    bool SetBlockAndMetadata(int x, int y, int z, int blockId, int meta);

    /// <summary>
    /// Sets block ID only (metadata cleared to 0).
    /// Spec: <c>d(int x, int y, int z, int blockId)</c> → bool.
    /// </summary>
    bool SetBlock(int x, int y, int z, int blockId);

    /// <summary>
    /// Sets block metadata, triggers neighbour notification.
    /// Spec: <c>c(int x, int y, int z, int meta)</c> → bool.
    /// </summary>
    bool SetMetadata(int x, int y, int z, int meta);

    // ── Tick scheduling ────────────────────────────────────────────────────────

    /// <summary>
    /// Schedules a block tick at (x, y, z) for <paramref name="blockId"/> after
    /// <paramref name="delay"/> ticks. Spec: <c>a(int x, int y, int z, int blockId, int delay)</c>.
    /// </summary>
    void ScheduleBlockUpdate(int x, int y, int z, int blockId, int delay);

    // ── Area queries ───────────────────────────────────────────────────────────

    /// <summary>
    /// True if all chunks within <paramref name="radius"/> blocks of (x, y, z) are loaded.
    /// Spec: <c>e(int x, int y, int z, int radius)</c> → bool.
    /// </summary>
    bool IsAreaLoaded(int x, int y, int z, int radius);
}
