namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>mz</c> (BlockRedstoneDiode / Repeater) — Block IDs 93 (off) and 94 (on).
///
/// Meta layout (spec §6.3):
///   bits 1-0 (meta &amp; 3):  facing direction (output direction)
///   bits 3-2 (meta &gt;&gt; 2 &amp; 3): delay index (0-3 → 2/4/6/8 ticks)
///
/// Facing:
///   0 = North (-Z output, reads from south z+1)
///   1 = East (+X output, reads from west x-1)
///   2 = South (+Z output, reads from north z-1)
///   3 = West (-X output, reads from east x+1)
///
/// Delay table: {1,2,3,4} × 2 = {2,4,6,8} ticks.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §6
/// </summary>
public sealed class BlockRedstoneDiode : Block
{
    private static readonly int[] DelayTable = { 1, 2, 3, 4 }; // obf: cb[]

    private readonly bool _isOn; // obf: cc

    // ── Construction (spec §6.2) ──────────────────────────────────────────────

    public BlockRedstoneDiode(int id, bool isOn) : base(id, isOn ? 147 : 131, Material.Plants)
    {
        _isOn = isOn;
        SetHardness(0.0f);
        ClearNeedsRandomTick();
        if (isOn) SetLightValue(0.625f); // ID 94 emits light 9 (~0.625F)
        SetBlockName("diode");
    }

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => _isOn ? 17 : 16; // 16=unpowered repeater, 17=powered
    public override bool RenderAsNormalBlock() => false;
    public override bool CanProvidePower() => _isOn;
    public override int GetTickDelay() => 2;

    // ── Texture by face (spec §6.11) ─────────────────────────────────────────

    public override int GetTextureIndex(int face) => face switch
    {
        0 => _isOn ? 99  : 115, // bottom (torch on/off texture)
        1 => _isOn ? 147 : 131, // top
        _ => 5                   // stone side
    };

    // ── Bounds (spec §6.2) ───────────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 0.125f, z + 1);

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 0.125f, z + 1);

    // ── Power output (spec §6.7) ──────────────────────────────────────────────

    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
    {
        if (!_isOn) return false;
        int facing = world.GetBlockMetadata(x, y, z) & 3;
        return facing switch
        {
            0 => face == 3, // north output → face 3 (north)
            1 => face == 4, // east output  → face 4 (east)
            2 => face == 2, // south output → face 2 (south)
            3 => face == 5, // west output  → face 5 (west)
            _ => false
        };
    }

    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
        => IsProvidingWeakPower(world, x, y, z, face);

    // ── canBlockStay (spec §6.5) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => world is World w && w.IsBlockNormalCube(x, y - 1, z);

    // ── Input check (spec §6.6) ───────────────────────────────────────────────

    private bool HasInput(World world, int x, int y, int z, int meta)
    {
        int facing = meta & 3;
        return facing switch
        {
            0 => world.GetPower(x, y, z + 1, 3) || IsWireWithPower(world, x, y, z + 1),
            1 => world.GetPower(x - 1, y, z, 4) || IsWireWithPower(world, x - 1, y, z),
            2 => world.GetPower(x, y, z - 1, 2) || IsWireWithPower(world, x, y, z - 1),
            3 => world.GetPower(x + 1, y, z, 5) || IsWireWithPower(world, x + 1, y, z),
            _ => false
        };
    }

    private static bool IsWireWithPower(World world, int x, int y, int z)
        => world.GetBlockId(x, y, z) == 55 && world.GetBlockMetadata(x, y, z) > 0;

    // ── onBlockActivated (spec §6.8) — right-click cycles delay ──────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide || world is not World w) return true;
        int meta  = world.GetBlockMetadata(x, y, z);
        int delay = (((meta >> 2) + 1) * 4) & 12; // increment delay bits, wrap
        w.SetMetadataQuiet(x, y, z, delay | (meta & 3));
        return true;
    }

    // ── UpdateTick (spec §6.9) ────────────────────────────────────────────────

    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (world is not World w) return;
        int meta     = w.GetBlockMetadata(x, y, z);
        bool hasInput = HasInput(w, x, y, z, meta);
        int delayBits = (meta >> 2) & 3;
        int delay     = DelayTable[delayBits] * 2;

        if (_isOn)
        {
            if (!hasInput)
                w.SetBlockAndMetadata(x, y, z, 93, meta); // turn off
        }
        else
        {
            // Turn on (and schedule turn-off if no input)
            w.SetBlockAndMetadata(x, y, z, 94, meta);
            if (!hasInput)
                w.ScheduleBlockUpdate(x, y, z, 94, delay);
        }
    }

    // ── onNeighborBlockChange (spec §6.10) ───────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (world.IsClientSide || world is not World w) return;
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItemWithChance(world, x, y, z, world.GetBlockMetadata(x, y, z), 1.0f, 0);
            world.SetBlock(x, y, z, 0);
            return;
        }
        int meta      = w.GetBlockMetadata(x, y, z);
        bool hasInput = HasInput(w, x, y, z, meta);
        int delayBits = (meta >> 2) & 3;
        int delay     = DelayTable[delayBits] * 2;

        if (_isOn && !hasInput)
            w.ScheduleBlockUpdate(x, y, z, BlockID, delay);
        else if (!_isOn && hasInput)
            w.ScheduleBlockUpdate(x, y, z, BlockID, delay);

        if (_isOn) // notify block above (spec §6.10)
            w.NotifyBlock(x, y + 1, z, BlockID);
    }

    // ── Drops (spec §6.12) ────────────────────────────────────────────────────

    /// <summary>Drops repeater item (acy.ba → item ID ~356). Stub: returns 356.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 356;
}
