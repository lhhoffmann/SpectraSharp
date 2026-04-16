namespace SpectraEngine.Core;

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

    /// <summary>
    /// Writes a block ID directly to the chunk without triggering light propagation
    /// or neighbour notifications. Used by world-gen passes (ore veins, cave carvers)
    /// where bulk writes must not cause cascading updates.
    /// Equivalent to vanilla's <c>world.d()</c> in generation context.
    /// </summary>
    void SetBlockSilent(int x, int y, int z, int blockId);

    // ── Snow / Ice helpers (SnowIce_Spec §9) ──────────────────────────────────

    /// <summary>obf: <c>ry.p(x,y,z)</c> — true if water at (x,y,z) should freeze to ice.</summary>
    bool CanFreezeAtLocation(int x, int y, int z);

    /// <summary>obf: <c>ry.r(x,y,z)</c> — true if air at (x,y,z) should receive a snow layer.</summary>
    bool CanSnowAtLocation(int x, int y, int z);

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

    // ── Dimension / notification helpers (spec: BlockFluid_Spec §16) ──────────

    /// <summary>
    /// True in the Nether dimension (<c>world.y.d == true</c>).
    /// Used by fluid blocks to choose lava flow speed (var7=1 Nether vs 2 Overworld).
    /// </summary>
    bool IsNether { get; }

    /// <summary>
    /// Suppresses entity notifications and certain side-effects during atomic block swaps.
    /// Spec: <c>world.t</c> — set true before still→flowing conversion, false after.
    /// </summary>
    bool SuppressUpdates { get; set; }

    /// <summary>
    /// Notifies the 6 axis-aligned neighbours of a block change.
    /// Spec: <c>world.j(int x, int y, int z, int sourceBlockId)</c>.
    /// </summary>
    void NotifyNeighbors(int x, int y, int z, int changedBlockId);

    // ── Light / sound / redstone helpers ──────────────────────────────────────

    /// <summary>
    /// Combined light level 0–15 at (x,y,z): max(sky − skyDarkening, block).
    /// Used by BlockCrops growth check. Spec: <c>world.getLightBrightness(x,y,z)</c>.
    /// </summary>
    int GetLightBrightness(int x, int y, int z);

    /// <summary>
    /// Plays an auxiliary sound/block-event at position.
    /// Event 1003 = door open/close sound. Null player = ambient (redstone trigger).
    /// Stub until SoundManager is implemented.
    /// Spec: <c>world.playAuxSFX(vi player, int eventId, int x, int y, int z, int data)</c>.
    /// </summary>
    void PlayAuxSFX(EntityPlayer? player, int eventId, int x, int y, int z, int data);

    /// <summary>
    /// Returns true if the block at (x,y,z) is receiving indirect redstone power.
    /// Used by iron doors. Always returns false until redstone is implemented.
    /// Spec: <c>world.isBlockIndirectlyReceivingPower(x, y, z)</c>.
    /// </summary>
    bool IsBlockIndirectlyReceivingPower(int x, int y, int z);

    // ── Weather / rain (spec: BlockFire_Spec §11) ──────────────────────────────

    /// <summary>
    /// True when it is raining (rain strength > 0.2).
    /// Spec: <c>world.E()</c> — <c>j(1.0F) > 0.2F</c>.
    /// </summary>
    bool IsRaining();

    /// <summary>
    /// True if the block at (x, y, z) is exposed to sky rainfall.
    /// Spec: <c>world.w(x,y,z)</c> — block is at or above the precipitation height map.
    /// Used by fire to determine if rain can extinguish it.
    /// </summary>
    bool IsBlockExposedToRain(int x, int y, int z);

    /// <summary>
    /// The dimension ID of the world's provider (0=Overworld, -1=Nether, 1=End).
    /// Spec: <c>world.y.g</c>.
    /// </summary>
    int DimensionId { get; }

    /// <summary>
    /// Creates an explosion centered at (x, y, z) with the given power and incendiary flag.
    /// Stub until <c>xp</c> (Explosion) is implemented. Spec: BlockBed_Spec §7, Explosion_Spec.
    /// </summary>
    void CreateExplosion(EntityPlayer? player, double x, double y, double z, float power, bool isIncendiary);
}
