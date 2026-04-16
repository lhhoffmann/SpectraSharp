namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ku</c> (BlockRedstoneTorch) — Block IDs 75 (off) and 76 (on).
/// Inherits torch placement/attachment logic from BlockTorchBase.
///
/// Burnout: ≥8 flips in 100 ticks → torch stays off permanently.
/// The burnout history <see cref="s_history"/> is STATIC (shared across all torches — vanilla bug).
///
/// Meta encoding (inherited from bg):
///   1=west, 2=east, 3=north, 4=south, 5=floor
///
/// Quirks preserved (spec §12):
///   2. s_history is static — shared global list (cross-contamination bug).
///   3. Fizz sound/smoke only on 8th flip, not subsequent ones.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §5
/// </summary>
public sealed class BlockRedstoneTorch : BlockTorchBase
{
    // ── Burnout history (spec §5.1) ───────────────────────────────────────────

    /// <summary>
    /// obf: <c>cb</c> — STATIC shared burnout history list.
    /// Entries: (x, y, z, worldTime). Vanilla bug: all torches share this list.
    /// </summary>
    private static readonly List<(int X, int Y, int Z, long Time)> s_history = new();

    // ── Instance fields (spec §5.1) ──────────────────────────────────────────

    /// <summary>obf: <c>a</c> — isOn: true = active torch (ID 76), false = burnt-out (ID 75).</summary>
    private readonly bool _isOn;

    // ── Construction (spec §5.2) ──────────────────────────────────────────────

    public BlockRedstoneTorch(int id, int textureIndex, bool isOn) : base(id, textureIndex)
    {
        _isOn = isOn;
        if (isOn) SetLightValue(0.5f); // ID 76 emits light level 7 (0.5F → ~7/15)
        SetBlockName("notGate");
    }

    // ── Power output (spec §5.3, §5.4) ───────────────────────────────────────

    public override bool CanProvidePower() => true;

    /// <summary>
    /// obf: <c>b(kq,x,y,z,face)</c> — isProvidingWeakPower.
    /// Powers all faces EXCEPT the face toward the attached block. Spec §5.3.
    /// </summary>
    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
    {
        if (!_isOn) return false;
        int meta = world.GetBlockMetadata(x, y, z);
        // Torch doesn't power toward the block it is attached to
        if (meta == 5 && face == 1) return false; // floor torch: doesn't power up
        if (meta == 3 && face == 3) return false; // north wall: doesn't power north
        if (meta == 4 && face == 2) return false; // south wall: doesn't power south
        if (meta == 1 && face == 5) return false; // west wall: doesn't power west
        if (meta == 2 && face == 4) return false; // east wall: doesn't power east
        return true;
    }

    /// <summary>
    /// obf: <c>c(ry,x,y,z,face)</c> — isProvidingStrongPower. Only downward (face 0). Spec §5.4.
    /// </summary>
    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
        => face == 0 && IsProvidingWeakPower(world, x, y, z, face);

    // ── onBlockAdded (spec §5.6) ──────────────────────────────────────────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        if (meta == 0) base.OnBlockAdded(world, x, y, z); // run bg placement logic

        if (!_isOn || world.IsClientSide || world is not World w) return;
        // Notify all 6 neighbors
        w.NotifyBlock(x - 1, y, z, BlockID);
        w.NotifyBlock(x + 1, y, z, BlockID);
        w.NotifyBlock(x, y - 1, z, BlockID);
        w.NotifyBlock(x, y + 1, z, BlockID);
        w.NotifyBlock(x, y, z - 1, BlockID);
        w.NotifyBlock(x, y, z + 1, BlockID);
    }

    // ── onNeighborBlockChange (spec §5.7) ────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (world.IsClientSide) return;
        // Schedule a tick to check if power state needs to change (delay 2)
        world.ScheduleBlockUpdate(x, y, z, BlockID, GetTickDelay());
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);
    }

    public override int GetTickDelay() => 2;

    // ── randomTick / UpdateTick (spec §5.9) ──────────────────────────────────

    /// <summary>
    /// obf: <c>a(ry,x,y,z,Random)</c> — scheduled tick (randomTick alias here for scheduled).
    /// Handles burnout and state toggling. Spec §5.9.
    /// </summary>
    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (world is not World w) return;
        long worldTime = w.WorldTime;

        // Trim entries older than 100 ticks (spec §5.9)
        while (s_history.Count > 0 && worldTime - s_history[0].Time > 100)
            s_history.RemoveAt(0);

        bool powered = IsAttachedBlockPowered(w, x, y, z);

        if (_isOn)
        {
            if (powered)
            {
                // Torch is on but powered → turn off
                w.SetBlockAndMetadata(x, y, z, 75, w.GetBlockMetadata(x, y, z)); // switch to OFF (ID 75)
                if (IsBurnedOut(w, x, y, z, addEntry: true))
                {
                    // Play fizz + smoke particles (spec §5.9 quirk)
                    // Sound stub: "random.fizz" at (x+0.5, y+0.5, z+0.5), vol=0.5, pitch=2.6±0.8
                    // Particle stub: 5 "smoke" particles
                }
            }
        }
        else
        {
            if (!powered && !IsBurnedOut(w, x, y, z, addEntry: false))
            {
                // Torch is off but not powered and not burned out → turn on
                w.SetBlockAndMetadata(x, y, z, 76, w.GetBlockMetadata(x, y, z)); // switch to ON (ID 76)
            }
        }
    }

    // ── Drops (spec §5.10) ────────────────────────────────────────────────────

    /// <summary>Always drops the ON torch item (ID 76). Spec §5.10.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 76;

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Checks if the block the torch is attached to is providing power toward the torch.
    /// obf: <c>g(ry,x,y,z)</c>. Spec §5.5.
    /// </summary>
    private static bool IsAttachedBlockPowered(World world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        return meta switch
        {
            5 => world.GetPower(x, y - 1, z, 0),     // floor: block below powers face 0(down)
            3 => world.GetPower(x, y, z - 1, 2),     // north wall: block at z-1 powers face 2(south)
            4 => world.GetPower(x, y, z + 1, 3),     // south wall: block at z+1 powers face 3(north)
            1 => world.GetPower(x - 1, y, z, 4),     // west wall: block at x-1 powers face 4(east)
            2 => world.GetPower(x + 1, y, z, 5),     // east wall: block at x+1 powers face 5(west)
            _ => false
        };
    }

    /// <summary>
    /// Counts occurrences of (x,y,z) in history; returns true if ≥8 (burnout).
    /// Optionally adds a new entry. obf: <c>a(ry,x,y,z,bool)</c>. Spec §5.8.
    /// </summary>
    private static bool IsBurnedOut(World world, int x, int y, int z, bool addEntry)
    {
        if (addEntry)
            s_history.Add((x, y, z, world.WorldTime));
        int count = 0;
        foreach (var entry in s_history)
            if (entry.X == x && entry.Y == y && entry.Z == z)
                count++;
        return count >= 8;
    }
}
