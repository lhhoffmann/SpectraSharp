namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Flowing fluid block — replica of <c>ahx</c> (BlockFluid).
/// Handles spreading for both water (IDs 8) and lava (ID 10).
///
/// Flow algorithm overview (spec §7):
///   1. Read current level; compute new level from 4 horizontal neighbours.
///   2. Check falling block above.
///   3. Infinite water source rule (≥2 adjacent sources + solid floor).
///   4. Lava slow-flow (Overworld): 75% chance to remain at current level.
///   5. Convert to still variant if level unchanged this tick.
///   6. Flow downward if possible; else lateral via nearest-drop flood-fill.
///
/// Quirks (spec §15):
///   - Water var7=1 (max reach 7); Overworld lava var7=2 (max reach 3-4); Nether lava var7=1.
///   - Lava 75% skip even when level would increase.
///   - falling bit (meta ≥ 8) → spreads laterally at level 1.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFluid_Spec.md §7-§9
/// </summary>
public sealed class BlockFluid : BlockFluidBase
{
    private readonly bool _isWater;

    // obf: this.a — source counter reset each tick (§7 step 2: tracks ≥2 adjacent sources)
    private int _sourceCount;

    public BlockFluid(int blockId, Material material)
        : base(blockId, material)
    {
        _isWater = material == Material.Water;
        // Flowing fluid uses scheduled UpdateTick only — no need for random BlockTick
        ClearNeedsRandomTick();
    }

    public override int GetTickDelay() => _isWater ? 5 : 30;

    // ── Block lifecycle ───────────────────────────────────────────────────────

    /// <summary>
    /// Schedule first spread tick when flowing fluid is placed in the world.
    /// Inherits lava+water interaction from base.
    /// </summary>
    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        base.OnBlockAdded(world, x, y, z); // lava+water check
        if (world.GetBlockId(x, y, z) == BlockID) // might have been converted to obsidian
            world.ScheduleBlockUpdate(x, y, z, BlockID, GetTickDelay());
    }

    // ── Main tick (spec §7) ───────────────────────────────────────────────────

    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // Lava tick: skip if not scheduled (BlockTick is for random ticks; fluid uses UpdateTick)
        // Step 1: read state
        int currentLevel = GetFluidLevel(world, x, y, z);
        if (currentLevel < 0) return; // block removed or changed

        int var7 = 1; // decay per lateral step
        if (!_isWater && !world.IsNether) var7 = 2; // Overworld lava: double decay

        bool convertToStill = true;

        // Step 2: compute new level (only for non-source blocks)
        int newLevel = currentLevel;
        if (currentLevel > 0)
        {
            int neighborMin = -100;
            _sourceCount = 0;

            neighborMin = AggregateNeighborLevel(world, x - 1, y, z, neighborMin);
            neighborMin = AggregateNeighborLevel(world, x + 1, y, z, neighborMin);
            neighborMin = AggregateNeighborLevel(world, x, y, z - 1, neighborMin);
            neighborMin = AggregateNeighborLevel(world, x, y, z + 1, neighborMin);

            newLevel = neighborMin + var7;
            if (newLevel >= 8 || neighborMin < 0) newLevel = -1; // no valid source

            // Check block above for falling water
            int aboveLevel = GetFluidLevel(world, x, y + 1, z);
            if (aboveLevel >= 0)
            {
                newLevel = aboveLevel >= 8 ? aboveLevel : aboveLevel + 8;
            }

            // Infinite water source rule (water only, spec §7 step 2)
            if (_isWater && _sourceCount >= 2)
            {
                Material? belowMat = world.GetBlockMaterial(x, y - 1, z);
                if (belowMat?.BlocksMovement() == true)
                {
                    newLevel = 0; // solid floor → create source
                }
                else if (belowMat == Material.Water && world.GetBlockMetadata(x, y, z) == 0)
                {
                    newLevel = 0; // water below + self is source → keep source
                }
            }

            // Lava 75% slow-flow (Overworld only, spec §7 step 2 / quirk)
            if (!_isWater && currentLevel < 8 && newLevel < 8 && newLevel > currentLevel
                && rng.NextInt(4) != 0)
            {
                newLevel = currentLevel;
                convertToStill = false;
            }

            // Apply level change
            if (newLevel != currentLevel)
            {
                currentLevel = newLevel;
                if (newLevel < 0)
                {
                    world.SetBlock(x, y, z, 0); // remove
                    return;
                }
                else
                {
                    world.SetMetadata(x, y, z, newLevel);
                    world.ScheduleBlockUpdate(x, y, z, BlockID, GetTickDelay());
                    world.NotifyNeighbors(x, y, z, BlockID);
                }
            }
            else if (convertToStill)
            {
                ConvertToStill(world, x, y, z);
            }
        }
        else
        {
            // Source block — convert to still if stable
            ConvertToStill(world, x, y, z);
        }

        // Step 3: flow downward if possible
        if (CanFlowInto(world, x, y - 1, z))
        {
            // Lava + water below → stone interaction (spec §7 step 3)
            if (!_isWater && world.GetBlockMaterial(x, y - 1, z) == Material.Water)
            {
                world.SetBlock(x, y - 1, z, 1); // stone
                // Play fizz sound/particles — stub (sound spec pending)
                return;
            }

            if (currentLevel >= 8)
                PlaceFluid(world, x, y - 1, z, currentLevel);         // falling: keep level
            else
                PlaceFluid(world, x, y - 1, z, currentLevel + 8);     // start falling: level + 8
        }
        else if (currentLevel >= 0)
        {
            // Step 4: flow laterally (spec §7 step 4 / §8)
            bool[] dirs = GetFlowDirections(world, x, y, z);

            int spreadLevel = currentLevel + var7;
            if (currentLevel >= 8) spreadLevel = 1; // falling water spreads at level 1
            if (spreadLevel >= 8) return;            // can't spread further

            // X-, X+, Z-, Z+  (matching 4-direction iteration in spec §8)
            if (dirs[0]) PlaceFluid(world, x - 1, y, z, spreadLevel);
            if (dirs[1]) PlaceFluid(world, x + 1, y, z, spreadLevel);
            if (dirs[2]) PlaceFluid(world, x, y, z - 1, spreadLevel);
            if (dirs[3]) PlaceFluid(world, x, y, z + 1, spreadLevel);
        }
    }

    // ── Flow direction algorithm (spec §8) ───────────────────────────────────

    /// <summary>
    /// obf: <c>ahx.k(world, x, y, z)</c> — compute which of 4 horizontal directions fluid flows.
    /// Uses recursive flood-fill (max depth 4) to find nearest block from which it can fall.
    /// Returns a bool[4] indexed as [X-, X+, Z-, Z+].
    /// </summary>
    private bool[] GetFlowDirections(IWorld world, int x, int y, int z)
    {
        var distance = new int[] { 1000, 1000, 1000, 1000 };
        int[] dx = [-1, 1, 0, 0];
        int[] dz = [0, 0, -1, 1];

        for (int d = 0; d < 4; d++)
        {
            int nx = x + dx[d], nz = z + dz[d];
            if (IsBlocked(world, nx, y, nz))
            {
                distance[d] = 1000;
            }
            else if (world.GetBlockMaterial(nx, y, nz) == BlockMaterial
                     && world.GetBlockMetadata(nx, y, nz) == 0)
            {
                distance[d] = 1000; // source of same fluid → don't flow in
            }
            else if (!IsBlocked(world, nx, y - 1, nz))
            {
                distance[d] = 0; // can fall immediately from that cell → best
            }
            else
            {
                distance[d] = FloodFillDistance(world, nx, y, nz, 1, d);
            }
        }

        int minDist = Math.Min(Math.Min(distance[0], distance[1]),
                               Math.Min(distance[2], distance[3]));
        return [distance[0] == minDist, distance[1] == minDist,
                distance[2] == minDist, distance[3] == minDist];
    }

    /// <summary>
    /// Recursive flood-fill to find nearest horizontal drop.
    /// obf: <c>ahx.c(world, x, y, z, depth, fromDir)</c>.
    /// Returns depth at which a drop is found, or 1000 if beyond depth 4.
    /// </summary>
    private int FloodFillDistance(IWorld world, int x, int y, int z, int depth, int fromDir)
    {
        if (depth > 4) return 1000;

        int[] dx = [-1, 1, 0, 0];
        int[] dz = [0, 0, -1, 1];
        int reverseDir = fromDir ^ 1; // reverse: 0↔1 (X), 2↔3 (Z)

        int minDist = 1000;
        for (int d = 0; d < 4; d++)
        {
            if (d == reverseDir) continue; // don't backtrack
            int nx = x + dx[d], nz = z + dz[d];
            if (!IsBlocked(world, nx, y, nz)
                && !(world.GetBlockMaterial(nx, y, nz) == BlockMaterial
                     && world.GetBlockMetadata(nx, y, nz) == 0))
            {
                if (!IsBlocked(world, nx, y - 1, nz))
                    return depth; // found a drop at current depth

                if (depth < 4)
                {
                    int sub = FloodFillDistance(world, nx, y, nz, depth + 1, d);
                    if (sub < minDist) minDist = sub;
                }
            }
        }
        return minDist;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>ahx.f(world, x, y, z, bestSoFar)</c> — aggregate level from one neighbor.
    /// Updates <see cref="_sourceCount"/> when a source block (meta 0) is found.
    /// </summary>
    private int AggregateNeighborLevel(IWorld world, int x, int y, int z, int bestSoFar)
    {
        int level = GetFluidLevel(world, x, y, z);
        if (level < 0) return bestSoFar; // different material → ignore
        if (level == 0) _sourceCount++;  // count adjacent sources
        if (level >= 8) level = 0;       // treat falling as source for min tracking
        return bestSoFar < 0 ? level : Math.Min(bestSoFar, level);
    }

    /// <summary>
    /// obf: <c>ahx.g(world, x, y, z, id, level)</c> — place fluid at a position if possible.
    /// Drops items from displaced non-fluid blocks (water); plays fizz for lava. Spec §7 step 4.
    /// </summary>
    private void PlaceFluid(IWorld world, int x, int y, int z, int level)
    {
        if (!CanFlowInto(world, x, y, z)) return;

        int existingId = world.GetBlockId(x, y, z);
        if (existingId > 0)
        {
            if (!_isWater)
            {
                // Lava: fizz sound/particles when displacing — stub (sound spec pending)
            }
            else
            {
                // Water: drop items from displaced block — stub (item drop spec pending)
            }
        }
        world.SetBlockAndMetadata(x, y, z, BlockID, level);
    }

    /// <summary>
    /// obf: <c>ahx.j()</c> — convert flowing to still variant (ID + 1). Spec §9.
    /// </summary>
    private void ConvertToStill(IWorld world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        world.SetBlockAndMetadata(x, y, z, BlockID + 1, meta); // still variant = flowing + 1
        // world.notifyRenderListeners stub (bd spec pending)
        world.NotifyNeighbors(x, y, z, BlockID + 1);
    }
}
