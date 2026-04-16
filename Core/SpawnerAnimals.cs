namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>we</c> (SpawnerAnimals) — static utility class that drives
/// ongoing mob population and initial chunk-populate animal placement.
///
/// Two entry points:
///   <see cref="TickSpawn"/> — called every server tick to maintain population caps.
///   <see cref="InitialPopulate"/> — called during chunk populate for initial animal placement.
///
/// Quirks preserved (spec §6):
///   1. distSq &lt; 576 check uses world SpawnPoint, NOT nearest-player distance.
///   2. Cap = type.Cap * chunkMapSize / 256 — includes border chunks in b.size().
///   3. world.Random is used directly (advances world RNG state).
///   4. Spider Jockey: 1% per Spider, per spawn attempt.
///   5. initialPopulate uses GetTopSolidOrLiquidBlock for surface Y.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/SpawnerAnimals_Spec.md
/// </summary>
public static class SpawnerAnimals
{
    // ── Creature type metadata (spec §3) ──────────────────────────────────────

    private static readonly (EnumCreatureType Type, int Cap, Type BaseClass)[] CreatureTypes =
    [
        (EnumCreatureType.Hostile, 70, typeof(Mobs.EntityMonster)),
        (EnumCreatureType.Passive, 15, typeof(Mobs.EntityAnimal)),
        (EnumCreatureType.Water,    5, typeof(Entity)),   // EntityWaterMob stub — no implemented class yet
    ];

    // ── Chunk coordinate accumulator (spec §2 field b) ───────────────────────

    // Reused per tick (spec: static HashMap b, cleared at start of each tick call).
    // Key = (chunkX, chunkZ), Value = isBorderOnly.
    private static readonly Dictionary<(int cx, int cz), bool> _chunkMap = [];

    // ── Constants (spec §5) ───────────────────────────────────────────────────

    private const int  ChunkRadius  = 8;       // 8 chunks → 17×17 area around each player
    private const float MinPlayerDistSq = 576f; // 24² — minimum dist from spawn point
    private const int  SpreadRange  = 6;       // pack scatter radius
    private const int  PackAttempts = 4;       // position attempts per pack member
    private const int  PacksPerAnchor = 3;     // pack groups per anchor position

    // ── TickSpawn (spec §4 method a(ry, bool, bool)) ─────────────────────────

    /// <summary>
    /// Maintains mob population caps across all loaded chunks.
    /// Called every server tick by the game loop.
    /// Returns the number of mobs successfully spawned this tick.
    /// Spec: <c>we.a(ry world, boolean spawnHostile, boolean spawnPassive)</c>.
    /// </summary>
    public static int TickSpawn(World world, bool spawnHostile, bool spawnPassive)
    {
        if (!spawnHostile && !spawnPassive) return 0;

        _chunkMap.Clear();

        // ── Step 1: Build 17×17 chunk map around each player ─────────────────
        foreach (Entity playerEntity in world.GetPlayerList())
        {
            int playerChunkX = (int)Math.Floor(playerEntity.PosX) >> 4;
            int playerChunkZ = (int)Math.Floor(playerEntity.PosZ) >> 4;

            for (int dx = -ChunkRadius; dx <= ChunkRadius; dx++)
            for (int dz = -ChunkRadius; dz <= ChunkRadius; dz++)
            {
                var coord = (playerChunkX + dx, playerChunkZ + dz);
                bool isBorder = (dx == -ChunkRadius || dx == ChunkRadius
                              || dz == -ChunkRadius || dz == ChunkRadius);

                if (!isBorder)
                    _chunkMap[coord] = false;           // inner: eligible for spawning
                else if (!_chunkMap.ContainsKey(coord))
                    _chunkMap[coord] = true;            // border: population tracking only
            }
        }

        // ── Step 2: For each creature type, check cap and spawn ───────────────
        int totalSpawned = 0;

        foreach (var (type, baseCap, baseClass) in CreatureTypes)
        {
            bool isPassive = type == EnumCreatureType.Passive || type == EnumCreatureType.Water;
            if (!spawnPassive && isPassive) continue;
            if (!spawnHostile && !isPassive) continue;

            // Water creature class not implemented — skip to avoid always-zero spawns
            if (type == EnumCreatureType.Water) continue;

            int currentCount = world.CountEntitiesOfType(baseClass);
            int cap = baseCap * _chunkMap.Count / 256; // quirk 2: b.size() includes border chunks

            if (currentCount > cap) continue;

            // Iterate only inner (eligible) chunks
            foreach (var ((cx, cz), isBorder) in _chunkMap)
            {
                if (isBorder) continue;

                int spawnX = cx * 16 + world.Random.NextInt(16);
                int spawnY = world.Random.NextInt(world.GetHeight());
                int spawnZ = cz * 16 + world.Random.NextInt(16);

                // Check basic validity at anchor
                if (world.IsOpaqueCube(spawnX, spawnY, spawnZ)) continue;
                if (!IsValidSpawnMaterial(type, world, spawnX, spawnY, spawnZ)) continue;

                // Attempt PacksPerAnchor packs from this anchor
                for (int pack = 0; pack < PacksPerAnchor; pack++)
                {
                    int px = spawnX, py = spawnY, pz = spawnZ;
                    List<BiomeGenBase.SpawnListEntry>? group = null;
                    int packCount = 0;

                    for (int attempt = 0; attempt < PackAttempts; attempt++)
                    {
                        // Random walk (quirk 3: uses world.Random directly)
                        px += world.Random.NextInt(SpreadRange) - world.Random.NextInt(SpreadRange);
                        py += world.Random.NextInt(1) - world.Random.NextInt(1);
                        pz += world.Random.NextInt(SpreadRange) - world.Random.NextInt(SpreadRange);

                        if (!IsValidSpawnPosition(type, world, px, py, pz)) continue;

                        float cx2 = px + 0.5f;
                        float cy2 = py;
                        float cz2 = pz + 0.5f;

                        // Distance check: must be > 24 blocks from nearest player
                        if (world.FindNearestPlayerWithinRange(cx2, cy2, cz2, 24.0) != null) continue;

                        // Distance check: must be > 24 blocks from world spawn point (quirk 1)
                        float dsx = cx2 - world.SpawnX, dsy = cy2 - world.SpawnY, dsz = cz2 - world.SpawnZ;
                        if (dsx * dsx + dsy * dsy + dsz * dsz < MinPlayerDistSq) continue;

                        // Lazy-init spawn group from biome
                        if (group == null)
                        {
                            group = world.GetSpawnableList(type, px, py, pz);
                            if (group == null) break;
                        }

                        // Pick random entry (weighted) and instantiate
                        var entry = PickWeightedRandom(group, world.Random);
                        if (entry == null) break;

                        Entity? mob;
                        try { mob = (Entity?)Activator.CreateInstance(entry.EntityType, world); }
                        catch { continue; }
                        if (mob == null) continue;

                        mob.SetLocationAndAngles(cx2, cy2, cz2,
                            world.Random.NextFloat() * 360.0f, 0.0f);

                        if (!mob.GetCanSpawnHere()) continue;

                        packCount++;
                        world.SpawnEntity(mob);
                        PostSpawnSetup(mob, world);

                        if (packCount >= mob.GetMaxSpawnedInChunk())
                            goto nextCoord;
                    }

                    totalSpawned += packCount;
                }
                nextCoord:;
            }
        }

        return totalSpawned;
    }

    // ── InitialPopulate (spec §4 method a(ry, sr, int, int, int, int, Random)) ─

    /// <summary>
    /// Places initial passive animals during chunk populate.
    /// Spec: <c>we.a(ry world, sr biome, int x, int z, int width, int depth, Random rng)</c>.
    /// </summary>
    public static void InitialPopulate(
        World world, BiomeGenBase biome,
        int x, int z, int width, int depth,
        JavaRandom rng)
    {
        var spawnList = biome.GetSpawnList(EnumCreatureType.Passive);
        if (spawnList.Count == 0) return;

        while (rng.NextFloat() < 0.1f) // biome.spawnChance — default 0.1; biome spec pending
        {
            var entry = PickWeightedRandom(spawnList, world.Random);
            if (entry == null) break;

            int count = entry.MinCount + rng.NextInt(1 + entry.MaxCount - entry.MinCount);

            int startX = x + rng.NextInt(width);
            int startZ = z + rng.NextInt(depth);
            int spawnX = startX, spawnZ = startZ;

            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    int surfaceY = world.GetTopSolidOrLiquidBlock(spawnX, spawnZ);
                    if (IsValidSpawnPosition(EnumCreatureType.Passive, world, spawnX, surfaceY, spawnZ))
                    {
                        Entity? mob;
                        try { mob = (Entity?)Activator.CreateInstance(entry.EntityType, world); }
                        catch { break; }
                        if (mob == null) break;

                        mob.SetLocationAndAngles(spawnX + 0.5, surfaceY, spawnZ + 0.5,
                            rng.NextFloat() * 360.0f, 0.0f);
                        world.SpawnEntity(mob);
                        PostSpawnSetup(mob, world);
                        break;
                    }

                    // Random walk within [x, x+width) × [z, z+depth) (spec §4)
                    spawnX = startX + rng.NextInt(5) - rng.NextInt(5);
                    spawnZ = startZ + rng.NextInt(5) - rng.NextInt(5);
                    while (spawnX < x || spawnX >= x + width || spawnZ < z || spawnZ >= z + depth)
                    {
                        spawnX = startX + rng.NextInt(5) - rng.NextInt(5);
                        spawnZ = startZ + rng.NextInt(5) - rng.NextInt(5);
                    }
                }
            }
        }
    }

    // ── isValidSpawnPosition (private static, spec §4) ───────────────────────

    private static bool IsValidSpawnMaterial(EnumCreatureType type, IWorld world, int x, int y, int z)
    {
        if (type == EnumCreatureType.Water)
            return world.GetBlockMaterial(x, y, z).IsLiquid();
        // Land: anchor block must not be inside a solid opaque cube (validated by isSolidBlock already)
        // Additional check: block must not be liquid (land creatures can't spawn in water)
        return !world.GetBlockMaterial(x, y, z).IsLiquid();
    }

    private static bool IsValidSpawnPosition(EnumCreatureType type, IWorld world, int x, int y, int z)
    {
        if (type == EnumCreatureType.Water)
            return world.GetBlockMaterial(x, y, z).IsLiquid()
                && !world.IsOpaqueCube(x, y + 1, z);

        // Land creature: solid floor, not inside solid, not in liquid, 2-block headroom
        return world.GetBlockMaterial(x, y - 1, z).IsSolid()
            && !world.IsOpaqueCube(x, y, z)
            && !world.GetBlockMaterial(x, y, z).IsLiquid()
            && !world.IsOpaqueCube(x, y + 1, z);
    }

    // ── postSpawnSetup (private static, spec §4) ──────────────────────────────

    private static void PostSpawnSetup(Entity mob, World world)
    {
        // Spider Jockey: 1% chance per Spider spawn (quirk 4)
        if (mob is Mobs.EntitySpider && world.Random.NextInt(100) == 0)
        {
            var skeleton = new Mobs.EntitySkeleton(world);
            skeleton.SetLocationAndAngles(mob.PosX, mob.PosY, mob.PosZ,
                mob.RotationYaw, 0.0f);
            world.SpawnEntity(skeleton);
            // skeleton.setMountedEntity(spider): skeleton mounts onto spider
            skeleton.MountEntity(mob); // sets mob.Rider = skeleton, skeleton.Mount = mob
        }
        // Sheep colour randomisation
        else if (mob is Mobs.EntitySheep sheep)
        {
            sheep.SetFleeceColor(Mobs.EntitySheep.GetRandomFleeceColor(world.Random));
        }
    }

    // ── Weighted random pick (spec §4 nc.a) ───────────────────────────────────

    private static BiomeGenBase.SpawnListEntry? PickWeightedRandom(
        List<BiomeGenBase.SpawnListEntry> list, JavaRandom rand)
    {
        if (list.Count == 0) return null;

        int totalWeight = 0;
        foreach (var e in list) totalWeight += e.Weight;
        if (totalWeight <= 0) return null;

        int roll = rand.NextInt(totalWeight);
        foreach (var e in list)
        {
            roll -= e.Weight;
            if (roll < 0) return e;
        }
        return list[^1];
    }
}
