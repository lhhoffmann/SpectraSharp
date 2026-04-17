namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aim</c> — portal link manager for dimension transitions.
/// Instantiated fresh per portal event (NOT a singleton — spec §9.1).
///
/// Responsibilities:
///   - End arrival: place/clear 5×5 obsidian platform + air column (§4.1.1).
///   - Nether/Overworld: find nearest existing portal (§4.2) or create one (§4.3).
///
/// Coordinate scaling (÷8 or ×8) is done by the CALLER before this is invoked.
///
/// Quirks preserved (spec §9):
///   1. New Random() per instance — portal orientation is non-deterministic.
///   2. Double b() call after c() — if b() fails on newly built portal, entity stays put.
///   3. End platform clears existing blocks (places air, no drops).
///   4. Frame built 4× in c() to trigger portal block activation via neighbor changes.
///   5. Axis centering in b() shifts ±0.5 per adjacent portal block (asymmetric shift is net 0).
///   6. world.SuppressUpdates set/cleared around each frame-placement pass.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PortalTravelAgent_Spec.md
/// </summary>
public sealed class PortalTravelAgent
{
    // ── Constants (spec §3) ──────────────────────────────────────────────────

    private const int PortalBlockId   = 90; // yy.bv.bM — BlockPortal ID
    private const int ObsidianBlockId = 49; // yy.ap.bM — Block.obsidian

    private const int SearchRadiusFind   = 128; // b() scan radius
    private const int SearchRadiusCreate = 16;  // c() scan radius

    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a</c> — RNG used only in <see cref="CreatePortal"/> for the starting
    /// orientation trial. Fresh <c>new Random()</c> per instance (non-deterministic).
    /// </summary>
    private readonly Random _rand = new();

    // ── Entry point ──────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>aim.a(ry world, ia entity)</c> — dispatches between End platform
    /// placement and Nether/Overworld portal logic.
    /// </summary>
    public void PlaceEntityInPortal(IWorld world, Entity entity)
    {
        if (world.DimensionId == 1)
            PlaceEndPlatform(world, entity);
        else
            PlaceNetherPortal(world, entity);
    }

    // ── 4.1.1 — End platform placement ──────────────────────────────────────

    private static void PlaceEndPlatform(IWorld world, Entity entity)
    {
        // Spec §4.1.1 variables
        int var3 = (int)Math.Floor(entity.PosX); // floor(entity.s)
        int var4 = (int)Math.Floor(entity.PosY) - 1; // floor(entity.t) − 1
        int var5 = (int)Math.Floor(entity.PosZ); // floor(entity.u)

        // 5×5 platform: var8 ∈ [-2,+2] (depth/Z), var9 ∈ [-2,+2] (width/X), var10 ∈ [-1,+2] (Y)
        for (int var8 = -2; var8 <= 2; var8++)       // Z depth
        {
            for (int var9 = -2; var9 <= 2; var9++)   // X width
            {
                for (int var10 = -1; var10 <= 2; var10++) // Y
                {
                    int bx = var3 + var9;
                    int by = var4 + var10;
                    int bz = var5 - var8;

                    // var10 < 0 → obsidian floor; var10 >= 0 → air
                    world.SetBlock(bx, by, bz, var10 < 0 ? ObsidianBlockId : 0);
                }
            }
        }

        // Entity repositioned to floor level; pitch reset to 0 (spec §4.1.1)
        entity.SetPosition(var3, var4, var5);
        entity.RotationPitch              = 0f;
        entity.MotionX = entity.MotionY   = entity.MotionZ = 0.0;
    }

    // ── 4.1.2 — Nether portal logic ──────────────────────────────────────────

    private void PlaceNetherPortal(IWorld world, Entity entity)
    {
        // Step 1: try to find an existing portal
        if (FindPortal(world, entity)) return;

        // Step 2: none found — create one, then find it again (spec §4.1.2 quirk 2)
        CreatePortal(world, entity);
        FindPortal(world, entity); // may still miss the portal (edge case preserved)
    }

    // ── 4.2 — findPortal ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>aim.b(ry world, ia entity)</c> — scans ±128 blocks for the nearest
    /// portal block column. If found, repositions entity at the portal centre.
    /// </summary>
    private static bool FindPortal(IWorld world, Entity entity)
    {
        int height  = world.GetHeight(); // 128
        int startX  = (int)Math.Floor(entity.PosX);
        int startZ  = (int)Math.Floor(entity.PosZ);

        double bestDist = -1.0;
        int bestX = 0, bestY = 0, bestZ = 0;

        for (int xi = startX - SearchRadiusFind; xi <= startX + SearchRadiusFind; xi++)
        {
            double xOff = (xi + 0.5) - entity.PosX; // var12

            for (int zi = startZ - SearchRadiusFind; zi <= startZ + SearchRadiusFind; zi++)
            {
                double zOff = (zi + 0.5) - entity.PosZ; // var15

                // Inner Y: top-to-bottom scan (spec §4.2, var17 loop)
                for (int yi = height - 1; yi >= 0; yi--)
                {
                    if (world.GetBlockId(xi, yi, zi) != PortalBlockId) continue;

                    // Walk down while portal continues — finds bottom of column
                    while (yi > 0 && world.GetBlockId(xi, yi - 1, zi) == PortalBlockId)
                        yi--;

                    double yOff  = (yi + 0.5) - entity.PosY;     // var18
                    double dist  = xOff * xOff + yOff * yOff + zOff * zOff; // var20

                    if (bestDist < 0.0 || dist < bestDist)
                    {
                        bestDist = dist;
                        bestX    = xi;
                        bestY    = yi;
                        bestZ    = zi;
                    }
                }
            }
        }

        if (bestDist < 0.0) return false;

        // Portal axis centering (spec §4.2): shift toward interior by ±0.5
        double cx = bestX + 0.5;
        double cy = bestY + 0.5;
        double cz = bestZ + 0.5;

        if (world.GetBlockId(bestX - 1, bestY, bestZ) == PortalBlockId) cx -= 0.5;
        if (world.GetBlockId(bestX + 1, bestY, bestZ) == PortalBlockId) cx += 0.5;
        if (world.GetBlockId(bestX, bestY, bestZ - 1) == PortalBlockId) cz -= 0.5;
        if (world.GetBlockId(bestX, bestY, bestZ + 1) == PortalBlockId) cz += 0.5;

        entity.SetPosition(cx, cy, cz);
        entity.RotationPitch              = 0f;
        entity.MotionX = entity.MotionY   = entity.MotionZ = 0.0;
        return true;
    }

    // ── 4.3 — createPortal ───────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>aim.c(ry world, ia entity)</c> — finds a suitable flat location
    /// near the entity, then builds a 4×5 obsidian frame with a 2×3 portal interior.
    /// Falls back to Y=70 if no suitable site is found. Always completes (no return value used).
    /// </summary>
    private void CreatePortal(IWorld world, Entity entity)
    {
        int   height       = world.GetHeight(); // 128
        int   ex           = (int)Math.Floor(entity.PosX);
        int   ey           = (int)Math.Floor(entity.PosY);
        int   ez           = (int)Math.Floor(entity.PosZ);
        int   startOri     = _rand.Next(4); // var13 — random start orientation

        double bestScore   = -1.0;
        int    bestX       = ex;
        int    bestY       = ey;
        int    bestZ       = ez;
        int    bestOri     = 0;

        // ── Phase 1: 3D suitability scan (3-deep, 4 orientations) ───────────

        for (int xi = ex - SearchRadiusCreate; xi <= ex + SearchRadiusCreate; xi++)
        {
            double xOff = xi - entity.PosX;

            for (int zi = ez - SearchRadiusCreate; zi <= ez + SearchRadiusCreate; zi++)
            {
                double zOff = zi - entity.PosZ;

                // Find topmost air block in column
                int topY = height - 1;
                while (topY > 0 && world.GetBlockId(xi, topY, zi) != 0) topY--;
                while (topY > 0 && world.GetBlockId(xi, topY - 1, zi) == 0) topY--;

                for (int oi = startOri; oi < startOri + 4; oi++)
                {
                    GetOrientationVectors(oi % 4, out int fwdX, out int fwdZ);

                    // Check 3×4×5 volume: depth var24=0..2, width var25=0..3, height var26=-1..3
                    bool valid = true;
                    for (int d = 0; d < 3 && valid; d++)
                    {
                        for (int w = 0; w <= 3 && valid; w++)
                        {
                            for (int h = -1; h <= 3 && valid; h++)
                            {
                                int bx = xi + (w - 1) * fwdX + d * fwdZ;
                                int by = topY + h;
                                int bz = zi + (w - 1) * fwdZ - d * fwdX;

                                if (h < 0) // foundation must be solid
                                { if (!world.GetBlockMaterial(bx, by, bz).IsSolid()) valid = false; }
                                else       // interior must be air
                                { if (world.GetBlockId(bx, by, bz) != 0)             valid = false; }
                            }
                        }
                    }

                    if (valid)
                    {
                        double yOff  = (topY + 0.5) - entity.PosY;
                        double score = xOff * xOff + yOff * yOff + zOff * zOff;

                        if (bestScore < 0.0 || score < bestScore)
                        {
                            bestScore = score;
                            bestX     = xi;
                            bestY     = topY;
                            bestZ     = zi;
                            bestOri   = oi % 4;
                        }
                    }
                }
            }
        }

        // ── Phase 2: 2D fallback (1-deep, 2 orientations) ───────────────────

        if (bestScore < 0.0)
        {
            for (int xi = ex - SearchRadiusCreate; xi <= ex + SearchRadiusCreate; xi++)
            {
                double xOff = xi - entity.PosX;

                for (int zi = ez - SearchRadiusCreate; zi <= ez + SearchRadiusCreate; zi++)
                {
                    double zOff = zi - entity.PosZ;

                    int topY = height - 1;
                    while (topY > 0 && world.GetBlockId(xi, topY, zi) != 0) topY--;
                    while (topY > 0 && world.GetBlockId(xi, topY - 1, zi) == 0) topY--;

                    for (int oi = startOri; oi < startOri + 2; oi++)
                    {
                        GetOrientationVectors(oi % 2, out int fwdX, out int fwdZ);

                        // 1-deep check (no depth dimension — spec §4.3 Phase 2)
                        bool valid = true;
                        for (int w = 0; w <= 3 && valid; w++)
                        {
                            for (int h = -1; h <= 3 && valid; h++)
                            {
                                int bx = xi + (w - 1) * fwdX;
                                int by = topY + h;
                                int bz = zi + (w - 1) * fwdZ;

                                if (h < 0) { if (!world.GetBlockMaterial(bx, by, bz).IsSolid()) valid = false; }
                                else       { if (world.GetBlockId(bx, by, bz) != 0)             valid = false; }
                            }
                        }

                        if (valid)
                        {
                            double yOff  = (topY + 0.5) - entity.PosY;
                            double score = xOff * xOff + yOff * yOff + zOff * zOff;

                            if (bestScore < 0.0 || score < bestScore)
                            {
                                bestScore = score;
                                bestX     = xi;
                                bestY     = topY;
                                bestZ     = zi;
                                bestOri   = oi % 2;
                            }
                        }
                    }
                }
            }
        }

        // ── Emergency Y=70 fallback (spec §4.3) ─────────────────────────────

        if (bestScore < 0.0)
        {
            bestX = ex; bestY = ey; bestZ = ez;
            if (bestY < 70)              bestY = 70;
            if (bestY > height - 10)     bestY = height - 10;

            GetOrientationVectors(bestOri, out int fwdX, out int fwdZ);

            // Carve 2×3×4 clearance pocket with obsidian floor
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dw = 1; dw <= 2; dw++)
                {
                    for (int dh = -1; dh <= 2; dh++)
                    {
                        int bx = bestX + (dw - 1) * fwdX + dx * fwdZ;
                        int by = bestY + dh;
                        int bz = bestZ + (dw - 1) * fwdZ - dx * fwdX;
                        world.SetBlock(bx, by, bz, dh < 0 ? ObsidianBlockId : 0);
                    }
                }
            }
        }

        // ── Frame construction: 4 passes (spec §4.3 quirk 4) ────────────────

        GetOrientationVectors(bestOri, out int sideX, out int sideZ);

        for (int pass = 0; pass < 4; pass++)
        {
            // Step A — suppress update propagation
            world.SuppressUpdates = true;

            // Step B — place 4×5 frame
            for (int w = 0; w <= 3; w++)
            {
                for (int h = -1; h <= 3; h++)
                {
                    int bx   = bestX + (w - 1) * sideX;
                    int by   = bestY + h;
                    int bz   = bestZ + (w - 1) * sideZ;
                    bool edge = w == 0 || w == 3 || h == -1 || h == 3;
                    world.SetBlock(bx, by, bz, edge ? ObsidianBlockId : PortalBlockId);
                }
            }

            // Step C — re-enable
            world.SuppressUpdates = false;

            // Step D — trigger neighbour changes for every block in the frame
            for (int w = 0; w <= 3; w++)
            {
                for (int h = -1; h <= 3; h++)
                {
                    int bx = bestX + (w - 1) * sideX;
                    int by = bestY + h;
                    int bz = bestZ + (w - 1) * sideZ;
                    world.NotifyNeighbors(bx, by, bz, world.GetBlockId(bx, by, bz));
                }
            }
        }
    }

    // ── Orientation helper (spec §4.3 / §7) ─────────────────────────────────

    /// <summary>
    /// Maps orientation index 0–3 to forward axis vectors.
    /// Spec §7 table: 0=Z+, 1=X+, 2=Z−, 3=X−.
    /// fwdX = var22, fwdZ = var23.
    /// </summary>
    private static void GetOrientationVectors(int orientation, out int fwdX, out int fwdZ)
    {
        int a = orientation % 2;     // var22 = orientation % 2
        int b = 1 - a;               // var23 = 1 - var22
        if (orientation >= 2) { a = -a; b = -b; } // if var21 % 4 >= 2: negate
        fwdX = a;
        fwdZ = b;
    }
}
