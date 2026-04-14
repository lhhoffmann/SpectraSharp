namespace SpectraSharp.Core.Blocks;

/// <summary>
/// Still fluid block — replica of <c>add</c> (BlockStationary).
/// Stable fluid that does not tick. Converts back to flowing when a neighbour changes.
///
/// Behaviours (spec §10):
///   - OnNeighborBlockChange → immediately converts back to flowing (ID − 1) and schedules tick.
///   - Still lava random tick → random-walk fire-spread upward (spec §12).
///   - Inherits lava+water interaction (CheckLavaWaterInteraction) from <see cref="BlockFluidBase"/>.
///
/// Quirks (spec §15):
///   - Sets <c>world.SuppressUpdates = true</c> during conversion to suppress entity notifications.
///   - Schedules the flowing block (not itself) — still blocks never self-schedule.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFluid_Spec.md §10-§12
/// </summary>
public sealed class BlockStationary : BlockFluidBase
{
    private readonly bool _isLava;

    public BlockStationary(int blockId, Material material)
        : base(blockId, material)
    {
        _isLava = material == Material.Lava_;
        // Still water has no random-tick behaviour; still lava keeps it for fire spread (spec §12)
        if (!_isLava) ClearNeedsRandomTick();
    }

    // ── Neighbour-change → convert to flowing (spec §10) ─────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        // Inherited: lava + water → obsidian / cobblestone
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);

        // If still our block (not converted to obsidian/cobble above), become flowing
        if (world.GetBlockId(x, y, z) == BlockID)
            ConvertToFlowing(world, x, y, z);
    }

    /// <summary>
    /// obf: <c>add.j(world, x, y, z)</c> — convert still to flowing variant (ID − 1).
    /// Schedules the flowing block to spread on next tick. Spec §10.
    /// </summary>
    private void ConvertToFlowing(IWorld world, int x, int y, int z)
    {
        int meta       = world.GetBlockMetadata(x, y, z);
        int flowingId  = BlockID - 1; // still → flowing: 9→8, 11→10
        Block? flowing = Block.BlocksList[flowingId];
        int    delay   = flowing?.GetTickDelay() ?? 5;

        world.SuppressUpdates = true;  // world.t = true — suppress entity notifications
        world.SetBlockAndMetadata(x, y, z, flowingId, meta);
        // world.notifyRenderListeners — stub (bd spec pending)
        world.ScheduleBlockUpdate(x, y, z, flowingId, delay);
        world.SuppressUpdates = false; // world.t = false
    }

    // ── Lava random tick → fire spread (spec §12) ────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (!_isLava) return;

        // Random walk upward 0–2 steps, place fire if air + flammable neighbour found
        int steps = rng.NextInt(3);
        int fx = x, fy = y, fz = z;

        for (int i = 0; i < steps; i++)
        {
            fx += rng.NextInt(3) - 1; // ±1 lateral
            fy += 1;                   // always upward
            fz += rng.NextInt(3) - 1;

            int id = world.GetBlockId(fx, fy, fz);
            if (id == 0) // air
            {
                // Check 6 faces for flammable material
                if (HasFlammableNeighbor(world, fx, fy, fz))
                {
                    world.SetBlock(fx, fy, fz, 51); // fire (yy.ar = 51)
                    return;
                }
            }
            else if (Block.BlocksList[id]?.BlockMaterial?.BlocksMovement() == true)
            {
                return; // hit a solid — stop walk
            }
        }
    }

    private static bool HasFlammableNeighbor(IWorld world, int x, int y, int z)
    {
        foreach (var (dx, dy, dz) in new (int, int, int)[]
            { (1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1) })
        {
            Material? mat = world.GetBlockMaterial(x + dx, y + dy, z + dz);
            if (mat?.IsBurnable() == true) return true;
        }
        return false;
    }
}
