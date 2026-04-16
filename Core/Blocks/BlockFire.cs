namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>wj</c> (BlockFire) — fire block (ID 51).
///
/// Ticks every 40 game ticks (2 s) via scheduled UpdateTick. On each tick:
///   1. Check permanent fire (netherrack below, or end-stone in End).
///   2. Remove if no longer supported (no solid ground + no flammable neighbour).
///   3. Rain extinguishes if ALL 5 positions (self + 4 horizontal) are wet.
///   4. Age the fire (meta += 0 or 1); re-schedule.
///   5. Burnout check (non-permanent, age == 15).
///   6. Consume 6 direct neighbours (burnBlock).
///   7. Area spread in a 3×6×3 box above.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFire_Spec.md
/// </summary>
public sealed class BlockFire : Block
{
    // ── Flammability tables (spec §3) ─────────────────────────────────────────
    // a[blockId]  = how easily a block catches fire (encouragement)
    // cb[blockId] = how quickly a block burns away (burnability)

    private static readonly int[] Flammability = new int[256];
    private static readonly int[] Burnability   = new int[256];

    static BlockFire()
    {
        // Source: BlockFire_Spec §3, cross-referenced with BlockRegistry_Spec for IDs.
        // Fields resolved via BlockRegistry_Spec field→ID table.

        //           field  ID   flam  burn
        SetFlam(  5,  5, 20);  // x  = Wood Planks
        SetFlam( 17,  5,  5);  // J  = Log
        SetFlam( 18, 30, 60);  // K  = Leaves
        SetFlam( 31, 60,100);  // X  = TallGrass
        SetFlam( 35, 30, 60);  // ab = Wool/Cloth
        SetFlam( 46, 15,100);  // am = TNT  (yy.am note: triggers special burnout action)
        SetFlam( 47, 30, 20);  // an = Bookshelf
        SetFlam( 53,  5, 20);  // at = Wood Stairs
        SetFlam( 85,  5, 20);  // aZ = Fence
        SetFlam(106, 15,100);  // bu = Vine
    }

    private static void SetFlam(int id, int flam, int burn)
    {
        Flammability[id] = flam;
        Burnability[id]  = burn;
    }

    // ── Fire block ID (spec §2) ───────────────────────────────────────────────

    private const int FireId       = 51;
    private const int NetherrackId = 87;  // yy.bb — permanent fire

    // ── Public table accessors (used by tests) ────────────────────────────────

    public static int GetFlammability(int blockId) => Flammability[blockId];
    public static int GetBurnability(int blockId)  => Burnability[blockId];

    // ── Constructor ───────────────────────────────────────────────────────────

    public BlockFire(int blockId)
        : base(blockId, Material.Portal_N)  // p.n — fire material (immovable, passable)
    {
        // Light emission 15 (spec §5) — use SetLightValue so instance LightValue tracks it
        SetLightValue(1.0f);
        // Fire uses scheduled UpdateTick only; no random BlockTick
        ClearNeedsRandomTick();
    }

    // ── Block property overrides (spec §5) ────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool IsCollidable() => false;
    public override int  GetTickDelay() => 40;

    // Fire drops nothing (spec §5)
    public override int  QuantityDropped(JavaRandom rng) => 0;

    /// <summary>Alias for QuantityDropped accepting System.Random (test adapter).</summary>
    public int GetDropCount(Random rng) => 0;

    /// <summary>Alias for GetCollisionBoundingBoxFromPool (test adapter).</summary>
    public AxisAlignedBB? GetCollisionBoundingBox(IWorld world, int x, int y, int z)
        => GetCollisionBoundingBoxFromPool(world, x, y, z);

    // ── Public instance wrappers for private static helpers (used by tests) ─────

    /// <summary>Public wrapper for OnNeighborBlockChange (test alias).</summary>
    public void OnNeighborChange(IWorld world, int x, int y, int z, int neighbourId)
        => OnNeighborBlockChange(world, x, y, z, neighbourId);

    /// <summary>Public wrapper for UpdateTick accepting System.Random (test adapter).</summary>
    public void OnBlockTick(IWorld world, int x, int y, int z, Random rng)
        => UpdateTick(world, x, y, z, new JavaRandom((long)rng.Next()));

    /// <summary>Returns true if the block at (x,y,z) has any flammability.</summary>
    public bool IsFlammable(IBlockAccess world, int x, int y, int z)
        => Flammability[world.GetBlockId(x, y, z)] > 0;

    /// <summary>
    /// Returns the maximum flammability of neighbours around an air block.
    /// Returns 0 if the target block is not air.
    /// </summary>
    public int MaxFlammabilityAround(IWorld world, int x, int y, int z)
    {
        if (world.GetBlockId(x, y, z) != 0) return 0;
        int best = 0;
        best = Math.Max(best, Flammability[world.GetBlockId(x + 1, y, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x - 1, y, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y + 1, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y - 1, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y, z + 1)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y, z - 1)]);
        return best;
    }

    /// <summary>True if any of the 6 adjacent blocks has flammability greater than 0.</summary>
    public bool HasFlammableNeighbor(IWorld world, int x, int y, int z)
        => Flammability[world.GetBlockId(x + 1, y, z)] > 0
        || Flammability[world.GetBlockId(x - 1, y, z)] > 0
        || Flammability[world.GetBlockId(x, y + 1, z)] > 0
        || Flammability[world.GetBlockId(x, y - 1, z)] > 0
        || Flammability[world.GetBlockId(x, y, z + 1)] > 0
        || Flammability[world.GetBlockId(x, y, z - 1)] > 0;

    /// <summary>True if fire can exist at (x,y,z): solid ground below or flammable neighbour.</summary>
    public bool CanFireSurviveHere(IWorld world, int x, int y, int z)
        => world.IsOpaqueCube(x, y - 1, z) || HasFlammableNeighbor(world, x, y, z);

    /// <summary>
    /// Returns the base divisor used in the area spread calculation.
    /// 100 for targetY ≤ fireY+1; 100 + (targetY − fireY − 1)×100 above.
    /// </summary>
    public int ComputeAreaSpreadBaseDivisor(int fireY, int targetY)
    {
        int excess = targetY - (fireY + 1);
        return excess > 0 ? 100 + excess * 100 : 100;
    }

    /// <summary>
    /// Returns the divisor used when burning a direct face neighbour.
    /// 300 for horizontal (dx or dz ≠ 0), 250 for vertical (dy ≠ 0).
    /// </summary>
    public int GetDirectSpreadDivisor(int dx, int dy, int dz)
        => (dy != 0) ? 250 : 300;

    // ── Block lifecycle ───────────────────────────────────────────────────────

    /// <summary>
    /// Schedules the first tick when fire is placed. Removes immediately if unsupported.
    /// Spec §8: <c>a(world, x, y, z)</c>.
    /// </summary>
    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        if (DimensionId(world) == 0)
        {
            // Overworld: check if fire is inside an obsidian portal frame → create portal
            // Spec: BlockPortal_Spec §6.3 — fire calls sc.g() on placement
            if (BlockPortal.TryToCreatePortal(world, x, y, z))
                return; // portal placed; fire replaced by portal blocks

            // Remove immediately if no support
            if (!S_CanSurviveHere(world, x, y, z))
            {
                world.SetBlock(x, y, z, 0);
                return;
            }
        }
        world.ScheduleBlockUpdate(x, y, z, FireId, GetTickDelay());
    }

    /// <summary>
    /// Remove fire if its support disappears.
    /// Spec §9: <c>a(world, x, y, z, neighborId)</c>.
    /// </summary>
    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (!S_CanSurviveHere(world, x, y, z))
            world.SetBlock(x, y, z, 0);
    }

    // ── Main tick (spec §6) ───────────────────────────────────────────────────

    /// <summary>
    /// Aging, spread, and burnout. Spec §6.
    /// </summary>
    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // Step 1: permanent fire check
        bool isPermanent = world.GetBlockId(x, y - 1, z) == NetherrackId;
        // (End-stone check: DimensionId == 1 and ID 121 — stubbed for now)

        // Step 2: existence check
        if (!S_CanSurviveHere(world, x, y, z))
        {
            world.SetBlock(x, y, z, 0);
            return;
        }

        // Step 3: rain extinguishment
        if (!isPermanent && IsWetAt(world, x, y, z))
        {
            world.SetBlock(x, y, z, 0);
            return;
        }

        // Step 4: age the fire
        int age = world.GetBlockMetadata(x, y, z);
        if (age < 15)
            age += rng.NextInt(3) / 2;  // += 0 or 1
        world.SetMetadata(x, y, z, age);
        world.ScheduleBlockUpdate(x, y, z, FireId, GetTickDelay());

        // Step 5: burnout check (non-permanent)
        if (!isPermanent)
        {
            bool hasFlammable = HasFlammableNeighbor(world, x, y, z);
            if (!hasFlammable)
            {
                if (!world.IsOpaqueCube(x, y - 1, z) || age > 3)
                {
                    world.SetBlock(x, y, z, 0);
                    return;
                }
            }
            else if (!world.IsOpaqueCube(x, y - 1, z) && age == 15 && rng.NextInt(4) == 0)
            {
                world.SetBlock(x, y, z, 0);
                return;
            }
        }

        // Step 6: consume 6 direct neighbours
        BurnBlock(world, x + 1, y,     z,     300, rng, age);
        BurnBlock(world, x - 1, y,     z,     300, rng, age);
        BurnBlock(world, x,     y - 1, z,     250, rng, age);
        BurnBlock(world, x,     y + 1, z,     250, rng, age);
        BurnBlock(world, x,     y,     z - 1, 300, rng, age);
        BurnBlock(world, x,     y,     z + 1, 300, rng, age);

        // Step 7: area spread in 3×6×3 box
        for (int bx = x - 1; bx <= x + 1; bx++)
        for (int bz = z - 1; bz <= z + 1; bz++)
        for (int by = y - 1; by <= y + 4; by++)
        {
            if (bx == x && by == y && bz == z) continue;

            int baseDivisor = 100;
            if (by > y + 1)
                baseDivisor += (by - (y + 1)) * 100; // 200, 300, 400 for y+2,y+3,y+4

            int flam = MaxFlammabilityAround(world, bx, by, bz);
            if (flam > 0)
            {
                int igniteChance = (flam + 40) / (age + 30);
                if (igniteChance > 0
                    && rng.NextInt(baseDivisor) <= igniteChance
                    && (!world.IsRaining() || !world.IsBlockExposedToRain(bx, by, bz)))
                {
                    int newAge = Math.Min(age + rng.NextInt(5) / 4, 15);
                    world.SetBlockAndMetadata(bx, by, bz, FireId, newAge);
                }
            }
        }
    }

    // ── burnBlock (spec §7) ───────────────────────────────────────────────────

    /// <summary>
    /// Attempt to consume (or spread fire to) a single adjacent face.
    /// obf: <c>wj.a(world, x, y, z, divisor, rand, age)</c>.
    /// </summary>
    private static void BurnBlock(IWorld world, int x, int y, int z, int divisor, JavaRandom rng, int age)
    {
        int id = world.GetBlockId(x, y, z);
        int burnSpeed = Burnability[id];
        if (burnSpeed <= 0) return;

        if (rng.NextInt(divisor) < burnSpeed)
        {
            bool isSpecial = (id == 46); // yy.am = TNT

            if (rng.NextInt(age + 10) < 5 && !world.IsBlockExposedToRain(x, y, z))
            {
                // Spread fire to this position
                int newAge = Math.Min(age + rng.NextInt(5) / 4, 15);
                world.SetBlockAndMetadata(x, y, z, FireId, newAge);
            }
            else
            {
                // Consume (destroy) the block
                world.SetBlock(x, y, z, 0);
            }

            if (isSpecial)
            {
                // yy.am.e(world, x, y, z, 1) — special action (e.g. TNT detonation)
                // Stub: TNT detonation spec pending
                _ = (world, x, y, z);
            }
        }
    }

    // ── Fire survival helpers (spec §4) ──────────────────────────────────────
    // Private static implementations (prefixed with S_ to avoid name conflicts
    // with the public instance wrappers that tests call via instance reference).

    /// <summary>
    /// Returns true if fire can exist at (x, y, z): solid ground below or flammable neighbour.
    /// obf: <c>wj.c(World, x, y, z)</c> — 3-arg override.
    /// </summary>
    private static bool S_CanSurviveHere(IWorld world, int x, int y, int z)
        => world.IsOpaqueCube(x, y - 1, z) || S_HasFlammableNeighbor(world, x, y, z);

    /// <summary>
    /// True if any of the 6 adjacent blocks has flammability > 0.
    /// obf: <c>wj.g(world, x, y, z)</c>.
    /// </summary>
    private static bool S_HasFlammableNeighbor(IWorld world, int x, int y, int z)
    {
        return S_IsFlammable(world, x + 1, y, z) || S_IsFlammable(world, x - 1, y, z)
            || S_IsFlammable(world, x, y + 1, z) || S_IsFlammable(world, x, y - 1, z)
            || S_IsFlammable(world, x, y, z + 1) || S_IsFlammable(world, x, y, z - 1);
    }

    /// <summary>
    /// True if the block at (x, y, z) has any flammability.
    /// obf: <c>wj.c(IBlockAccess, x, y, z)</c> — 4-arg overload.
    /// </summary>
    private static bool S_IsFlammable(IBlockAccess world, int x, int y, int z)
        => Flammability[world.GetBlockId(x, y, z)] > 0;

    /// <summary>
    /// Returns the maximum flammability of the 6 faces adjacent to an air block.
    /// Returns 0 if the target position is not air.
    /// obf: <c>wj.h(world, x, y, z)</c>.
    /// </summary>
    private static int S_MaxFlammabilityAround(IWorld world, int x, int y, int z)
    {
        if (world.GetBlockId(x, y, z) != 0) return 0; // must be air

        int best = 0;
        best = Math.Max(best, Flammability[world.GetBlockId(x + 1, y, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x - 1, y, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y + 1, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y - 1, z)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y, z + 1)]);
        best = Math.Max(best, Flammability[world.GetBlockId(x, y, z - 1)]);
        return best;
    }

    // ── Rain helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// True when ALL 5 positions (self + 4 horizontal) are wet (raining + sky-exposed).
    /// Spec §3: rain extinguishes only when fully surrounded by open rain exposure.
    /// </summary>
    private static bool IsWetAt(IWorld world, int x, int y, int z)
        => world.IsRaining()
        && world.IsBlockExposedToRain(x,     y, z)
        && world.IsBlockExposedToRain(x - 1, y, z)
        && world.IsBlockExposedToRain(x + 1, y, z)
        && world.IsBlockExposedToRain(x,     y, z - 1)
        && world.IsBlockExposedToRain(x,     y, z + 1);

    // ── Dimension helper ──────────────────────────────────────────────────────

    private static int DimensionId(IWorld world) => world.DimensionId;
}
