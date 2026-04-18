using SpectraEngine.Core;

namespace SpectraEngine.Graphics;

/// <summary>
/// Replica of <c>afv</c> (WorldRenderer / RenderGlobal) — client-side rendering coordinator.
/// Implements <see cref="IWorldAccess"/> to receive world change notifications and
/// manage dirty render-chunk sections.
///
/// Responsibilities:
///   - Track dirty 16×16×16 chunk sections for rebuild.
///   - Dispatch WorldEvent IDs to sound and particle systems.
///   - Render entities, tile entities, and particles.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RenderManager_Spec.md
/// </summary>
public class WorldRenderer : IWorldAccess
{
    // ── Dirty section tracking ────────────────────────────────────────────────

    private readonly HashSet<(int cx, int cy, int cz)> _dirtySections = new();

    private static (int, int, int) SectionOf(int x, int y, int z)
        => (x >> 4, y >> 4, z >> 4);

    public IReadOnlyCollection<(int cx, int cy, int cz)> DirtySections => _dirtySections;

    /// <summary>Clears the dirty-section set after all sections have been rebuilt this frame.</summary>
    public void ClearDirtySections() => _dirtySections.Clear();

    // ── IWorldAccess ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void OnBlockChanged(int x, int y, int z)
        => _dirtySections.Add(SectionOf(x, y, z));

    /// <inheritdoc/>
    public void OnBlockRangeChanged(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        int sx1 = x1 >> 4, sy1 = y1 >> 4, sz1 = z1 >> 4;
        int sx2 = x2 >> 4, sy2 = y2 >> 4, sz2 = z2 >> 4;
        for (int cx = sx1; cx <= sx2; cx++)
        for (int cy = sy1; cy <= sy2; cy++)
        for (int cz = sz1; cz <= sz2; cz++)
            _dirtySections.Add((cx, cy, cz));
    }

    /// <inheritdoc/>
    public void PlaySound(string name, double x, double y, double z, float volume, float pitch)
    {
        // Stub — routed to audio engine once SoundSystem is implemented.
    }

    /// <inheritdoc/>
    public void SpawnParticle(string name, double x, double y, double z,
                              double velX, double velY, double velZ)
    {
        // Stub — routed to particle engine once ParticleEngine is implemented.
    }

    /// <inheritdoc/>
    public void OnEntityAdded(Entity entity)
    {
        // Stub — allocate renderer resources for entity once EntityRenderer is implemented.
    }

    /// <inheritdoc/>
    public void OnEntityRemoved(Entity entity)
    {
        // Stub — free renderer resources for entity once EntityRenderer is implemented.
    }

    /// <inheritdoc/>
    public void PlayWorldEvent(string name, int x, int y, int z)
    {
        // Stub — dispatch to sound + particle handlers once those are implemented.
    }

    /// <inheritdoc/>
    public void PlayRecord(int x, int y, int z, SpectraEngine.Core.TileEntity.TileEntity? sound)
    {
        // Stub — jukebox record playback not yet implemented.
    }

    /// <inheritdoc/>
    public void MarkBlocksDirty(EntityPlayer? player,
                                int x1, int y1, int z1,
                                int x2, int y2, int z2)
        => OnBlockRangeChanged(x1, y1, z1, x2, y2, z2);
}
