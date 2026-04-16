namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>xp</c> (Explosion) — synchronous explosion algorithm.
///
/// Two-phase execution (always called in the same tick):
///   1. <see cref="ComputeAffectedBlocksAndDamageEntities"/> — populate block set, deal entity damage.
///   2. <see cref="DestroyBlocksAndSpawnParticles"/> — destroy blocks, spawn particles, place fire.
///
/// Quirks preserved (spec §12):
///   1. World RNG consumed 1352 times (once per ray) — advances world random state.
///   2. Entity damage uses doubled power (f *= 2 before entity bbox query).
///   3. Incendiary fire uses local Random (h = new Random()), NOT world RNG — non-deterministic.
///   4. TNT chain-fuse: nextInt(20)+10 ticks.
///   5. Creeper fuse caps at 30 ticks.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Explosion_Spec.md
/// </summary>
public sealed class Explosion
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: a — isIncendiary; places fire on exposed floors after block destruction.</summary>
    public readonly bool IsIncendiary;

    /// <summary>obf: b/c/d — explosion origin.</summary>
    public readonly double OriginX, OriginY, OriginZ;

    /// <summary>obf: e — source entity (null = world trigger, e.g. primed TNT).</summary>
    public readonly Entity? SourceEntity;

    /// <summary>obf: f — blast power (radius in blocks at full strength).</summary>
    public float Power;

    /// <summary>obf: g — set of block positions to destroy (populated in phase 1).</summary>
    private readonly HashSet<(int x, int y, int z)> _affectedBlocks = new();

    /// <summary>obf: h — local RNG for incendiary fire (NOT world RNG — quirk 3).</summary>
    private readonly Random _localRng = new();

    /// <summary>obf: i — world reference.</summary>
    private readonly World _world;

    // ── Constructor ───────────────────────────────────────────────────────────

    public Explosion(World world, Entity? source, double x, double y, double z,
                     float power, bool isIncendiary)
    {
        _world        = world;
        SourceEntity  = source;
        OriginX       = x;
        OriginY       = y;
        OriginZ       = z;
        Power         = power;
        IsIncendiary  = isIncendiary;
    }

    // ── Phase 1: ray-cast block collection + entity damage (spec §4) ─────────

    /// <summary>
    /// obf: <c>xp.a()</c> — phase 1.
    /// Casts 1352 rays from the 16³ surface grid, collects affected blocks,
    /// then damages all entities within the blast radius.
    /// </summary>
    public void ComputeAffectedBlocksAndDamageEntities()
    {
        const int GridSize = 16;
        const float StepSize = 0.3f;

        // ── Part 1: ray-cast block collection ────────────────────────────────

        for (int i = 0; i < GridSize; i++)
        for (int j = 0; j < GridSize; j++)
        for (int k = 0; k < GridSize; k++)
        {
            // Only surface voxels of the 16³ cube (quirk 1: 1352 rays total)
            if (i != 0 && i != 15 && j != 0 && j != 15 && k != 0 && k != 15)
                continue;

            // Normalised direction [-1, 1]³
            float dx = (float)i / (GridSize - 1) * 2.0f - 1.0f;
            float dy = (float)j / (GridSize - 1) * 2.0f - 1.0f;
            float dz = (float)k / (GridSize - 1) * 2.0f - 1.0f;
            float len = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            dx /= len; dy /= len; dz /= len;

            // Per-ray random starting strength (consumes world RNG — quirk 1)
            float strength = Power * (0.7f + _world.Random.NextFloat() * 0.6f);

            double rx = OriginX, ry = OriginY, rz = OriginZ;

            while (strength > 0f)
            {
                int bx = (int)Math.Floor(rx);
                int by = (int)Math.Floor(ry);
                int bz = (int)Math.Floor(rz);

                int blockId = _world.GetBlockId(bx, by, bz);
                if (blockId > 0)
                {
                    float blastRes = Block.BlocksList[blockId]!.GetExplosionResistance(SourceEntity);
                    strength -= (blastRes + 0.3f) * StepSize;
                }

                if (strength > 0f)
                    _affectedBlocks.Add((bx, by, bz));

                rx += dx * StepSize;
                ry += dy * StepSize;
                rz += dz * StepSize;
                strength -= StepSize * 0.75f; // fixed attenuation per step
            }
        }

        // ── Part 2: entity damage (spec §4, quirk 2) ─────────────────────────

        Power *= 2.0f; // doubled power for entity query bbox (quirk 2)

        var queryBox = AxisAlignedBB.GetFromPool(
            (int)Math.Floor(OriginX - Power - 1),
            (int)Math.Floor(OriginY - Power - 1),
            (int)Math.Floor(OriginZ - Power - 1),
            (int)Math.Floor(OriginX + Power + 1),
            (int)Math.Floor(OriginY + Power + 1),
            (int)Math.Floor(OriginZ + Power + 1));

        var entities = _world.GetEntitiesWithinAABBExcluding(SourceEntity, queryBox);
        var origin   = Vec3.GetFromPool(OriginX, OriginY, OriginZ);

        foreach (Entity entity in entities)
        {
            double dist = Math.Sqrt(entity.SquaredDistanceTo(OriginX, OriginY, OriginZ));
            double distRatio = dist / Power;
            if (distRatio > 1.0) continue;

            // Knockback direction
            double kx = entity.PosX - OriginX;
            double ky = entity.PosY - OriginY;
            double kz = entity.PosZ - OriginZ;
            double kLen = Math.Sqrt(kx * kx + ky * ky + kz * kz);
            if (kLen < 1e-6) { kx = 0; ky = 1; kz = 0; kLen = 1; } // straight up if at centre
            kx /= kLen; ky /= kLen; kz /= kLen;

            // Exposure fraction
            float exposure = _world.GetExplosionExposure(origin, entity.BoundingBox);

            double intensity = (1.0 - distRatio) * exposure;
            // damage = (intensity² + intensity) / 2 * 8 * f + 1  (f = doubled power — quirk 2)
            int damage = (int)((intensity * intensity + intensity) / 2.0 * 8.0 * Power + 1.0);

            if (entity is LivingEntity le)
                le.AttackEntityFrom(DamageSource.Explosion, damage);

            entity.MotionX += kx * intensity;
            entity.MotionY += ky * intensity;
            entity.MotionZ += kz * intensity;
        }

        Power /= 2.0f; // restore original power
    }

    // ── Phase 2: block destruction + particles + fire (spec §6) ──────────────

    /// <summary>
    /// obf: <c>xp.a(boolean doParticles)</c> — phase 2.
    /// Destroys all blocks in <see cref="_affectedBlocks"/>, spawns particles (if
    /// <paramref name="doParticles"/>), places incendiary fire.
    /// </summary>
    public void DestroyBlocksAndSpawnParticles(bool doParticles)
    {
        // Sound effect — pitch randomised with two world-RNG calls
        // (stub: just a console marker; audio system not yet implemented)
        _ = _world.Random.NextFloat(); // consume RNG for pitch var1
        _ = _world.Random.NextFloat(); // consume RNG for pitch var2

        // Iterate affected blocks in reverse list order (spec §6: reverse ArrayList iteration)
        var blockList = new List<(int x, int y, int z)>(_affectedBlocks);
        for (int idx = blockList.Count - 1; idx >= 0; idx--)
        {
            var (bx, by, bz) = blockList[idx];
            int blockId = _world.GetBlockId(bx, by, bz);

            if (doParticles && blockId > 0)
            {
                // Consume world RNG for particle position (3 calls) and 2 scale calls
                _ = _world.Random.NextFloat(); // px randomisation
                _ = _world.Random.NextFloat(); // py randomisation
                _ = _world.Random.NextFloat(); // pz randomisation
                _ = _world.Random.NextFloat(); // scale mul1
                _ = _world.Random.NextFloat(); // scale mul2
            }

            if (blockId > 0)
            {
                // Drop items at 30% chance (spec §6)
                Block.BlocksList[blockId]!.DropBlockAsItemWithChance(
                    _world, bx, by, bz,
                    _world.GetBlockMetadata(bx, by, bz),
                    0.3f, 0);

                // Remove block
                _world.SetBlock(bx, by, bz, 0);

                // Notify block (chain-TNT, etc.)
                Block.BlocksList[blockId]!.OnBlockDestroyedByExplosion(_world, bx, by, bz);
            }
        }

        // Incendiary fire pass (spec §6, quirk 3: local RNG, NOT world RNG)
        if (IsIncendiary)
        {
            foreach (var (bx, by, bz) in blockList)
            {
                int curId   = _world.GetBlockId(bx, by, bz);
                int floorId = _world.GetBlockId(bx, by - 1, bz);
                if (curId == 0 && Block.IsOpaqueCubeArr[floorId] && _localRng.Next(3) == 0)
                    _world.SetBlock(bx, by, bz, 51); // ID 51 = fire block
            }
        }
    }
}
