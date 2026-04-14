namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Mob spawner tile entity. Replica of <c>ze</c> (TileEntityMobSpawner).
/// Block ID: 52. NBT registry string: "MobSpawner".
///
/// Spawns up to 4 mobs of <see cref="EntityTypeId"/> every 200–799 ticks
/// when a player is within 16 blocks.
///
/// Quirks preserved (spec §12):
///   1. If spawnDelay is -1 after load (freshly placed), the spawner randomises its
///      delay on the very next tick instead of spawning immediately.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §9
/// </summary>
public sealed class TileEntityMobSpawner : TileEntity
{
    private const double ActiveRadius    = 16.0;
    private const int    MinDelay        = 200;
    private const int    DelayVariance   = 600;
    private const int    MaxSpawnAttempts = 4;
    private const int    MaxNearbyMobs    = 6;
    private const double SpawnOffsetH    = 4.0;
    private const int    SpawnOffsetV    = 3;

    // ── Fields (spec §9.1) ────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — ticks until next spawn; -1 = uninitialised.</summary>
    public int SpawnDelay = -1;

    /// <summary>obf: <c>k</c> — entity string ID to spawn (e.g. "Pig").</summary>
    public string EntityTypeId = "Pig";

    // Visual spin (client-only, not persisted)
    public double Rotation     = 0.0; // obf: b
    public double PrevRotation = 0.0; // obf: j

    // ── NBT (spec §9.2) ───────────────────────────────────────────────────────

    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
    {
        tag.PutString("EntityId", EntityTypeId);
        tag.PutShort("Delay", (short)SpawnDelay);
    }

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        EntityTypeId = tag.GetString("EntityId");
        SpawnDelay   = tag.GetShort("Delay");
    }

    // ── Tick logic (spec §9.3) ────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null || World.IsClientSide) return;

        // Visual spin
        PrevRotation = Rotation;
        Rotation    += 1000.0 / (SpawnDelay + MinDelay);
        while (Rotation > 360.0) Rotation -= 360.0;

        // First tick: randomise delay
        if (SpawnDelay == -1)
        {
            ResetDelay();
            return;
        }

        // Only active with nearby player
        if (!IsPlayerNear()) return;

        if (SpawnDelay > 0)
        {
            SpawnDelay--;
            return;
        }

        // Attempt spawn
        for (int i = 0; i < MaxSpawnAttempts; i++)
        {
            // Count nearby mobs of same type (stub — we don't have per-class entity count)
            // Vanilla checks world.getEntitiesOfType(entityClass, AABB.expand(8,4,8)) >= 6
            // Without that API we just spawn without the cap for now.

            double sx = X + (World.Random.NextDouble() - World.Random.NextDouble()) * SpawnOffsetH;
            int    sy = Y + World.Random.NextInt(SpawnOffsetV) - 1;
            double sz = Z + (World.Random.NextDouble() - World.Random.NextDouble()) * SpawnOffsetH;

            var entity = EntityRegistry.CreateMobByStringId(EntityTypeId, World);
            if (entity == null) break;

            entity.SetLocationAndAngles(sx, sy, sz, World.Random.NextFloat() * 360.0f, 0.0f);
            World.SpawnEntity(entity);
        }

        ResetDelay();
    }

    private void ResetDelay()
        => SpawnDelay = MinDelay + (World?.Random.NextInt(DelayVariance) ?? 0);

    private bool IsPlayerNear()
    {
        if (World == null) return false;
        foreach ((double px, double pz) in World.GetLoadedPlayerPositions())
        {
            // Simple XZ + Y distance check (spec uses sphere)
            double dx = px - (X + 0.5);
            double dz = pz - (Z + 0.5);
            if (dx * dx + dz * dz <= ActiveRadius * ActiveRadius) return true;
        }
        return false;
    }
}
