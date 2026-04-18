namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>bd</c> (IWorldAccess) — world event listener interface.
/// Registered on <see cref="World"/> via <see cref="World.AddWorldAccess"/> /
/// <see cref="World.RemoveWorldAccess"/>. Receives block-change, sound, particle,
/// and entity-lifecycle events.
///
/// Primary implementor: <c>afv</c> (WorldRenderer / RenderGlobal) which
/// marks render chunk sections dirty on block-change notifications.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/IWorldAccess_Spec.md
/// </summary>
public interface IWorldAccess
{
    // ── Block invalidation ────────────────────────────────────────────────────

    /// <summary>Single block changed — mark chunk section dirty. obf: <c>bd.a(x,y,z)</c></summary>
    void OnBlockChanged(int x, int y, int z);

    /// <summary>Region of blocks changed — mark all affected sections dirty. obf: <c>bd.a(x1,y1,z1,x2,y2,z2)</c></summary>
    void OnBlockRangeChanged(int x1, int y1, int z1, int x2, int y2, int z2);

    // ── Sound ─────────────────────────────────────────────────────────────────

    /// <summary>Play named sound at world position. obf: <c>bd.a(String,double,double,double,float,float)</c></summary>
    void PlaySound(string name, double x, double y, double z, float volume, float pitch);

    // ── Particles ─────────────────────────────────────────────────────────────

    /// <summary>Spawn named particle with velocity. obf: <c>bd.a(String,double*3,double*3)</c></summary>
    void SpawnParticle(string name, double x, double y, double z,
                       double velX, double velY, double velZ);

    // ── Entity lifecycle ──────────────────────────────────────────────────────

    /// <summary>Entity was added to the world. obf: <c>bd.a(ia)</c></summary>
    void OnEntityAdded(Entity entity);

    /// <summary>Entity was removed from the world. obf: <c>bd.b(ia)</c></summary>
    void OnEntityRemoved(Entity entity);

    // ── World events ──────────────────────────────────────────────────────────

    /// <summary>
    /// World event / auxiliary SFX. obf: <c>bd.a(String,int,int,int)</c>.
    /// See SoundManager_Spec for the event name table.
    /// </summary>
    void PlayWorldEvent(string name, int x, int y, int z);

    /// <summary>Play record / jukebox sound at position. obf: <c>bd.a(int,int,int,bq)</c></summary>
    void PlayRecord(int x, int y, int z, TileEntity.TileEntity? sound);

    /// <summary>
    /// Mark a dirty block region, optionally excluding updates caused by
    /// <paramref name="player"/> (self-render avoidance). obf: <c>bd.a(vi,int*6)</c>
    /// </summary>
    void MarkBlocksDirty(EntityPlayer? player,
                         int x1, int y1, int z1,
                         int x2, int y2, int z2);
}
