using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>acu</c> (BlockPistonExtension) — Block ID 34.
///
/// The piston arm (head) placed in front of the piston base when extended.
/// Rendered as a face plate + shaft. Defers neighbour events to base piston.
///
/// Meta layout (spec §6):
///   bits 2-0: facing direction (0-5)
///   bit  3:   isSticky arm flag (1 = sticky texture bL-1)
///
/// Texture atlas (spec §7.11):
///   front face: 107 (normal) or 106 (sticky) or custom override
///   back face:  107
///   side faces: 108
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPiston_Spec.md §7.11-§7.15
/// </summary>
public sealed class BlockPistonExtension : Block
{
    // ── Direction data (ot arrays, spec §3) ───────────────────────────────────

    private static readonly int[] OppFace = { 1, 0, 3, 2, 5, 4 }; // ot.a
    private static readonly int[] DirX    = { 0, 0,  0, 0, -1, 1 }; // ot.b
    private static readonly int[] DirY    = {-1, 1,  0, 0,  0, 0 }; // ot.c
    private static readonly int[] DirZ    = { 0, 0, -1, 1,  0, 0 }; // ot.d

    // ── Face-plate and shaft AABB pairs per facing (spec §7.12) ──────────────

    private static readonly (float x0, float y0, float z0, float x1, float y1, float z1)[] FacePlates =
    [
        (0, 0, 0, 1, 0.25f, 1),                      // 0 down
        (0, 0.75f, 0, 1, 1, 1),                       // 1 up
        (0, 0, 0, 1, 1, 0.25f),                       // 2 north
        (0, 0, 0.75f, 1, 1, 1),                       // 3 south
        (0, 0, 0, 0.25f, 1, 1),                       // 4 west
        (0.75f, 0, 0, 1, 1, 1),                       // 5 east
    ];

    private static readonly (float x0, float y0, float z0, float x1, float y1, float z1)[] Shafts =
    [
        (0.375f, 0.25f, 0.375f, 0.625f, 1, 0.625f),  // 0 down
        (0.375f, 0, 0.375f, 0.625f, 0.75f, 0.625f),  // 1 up
        (0.25f, 0.375f, 0.25f, 0.75f, 0.625f, 1),    // 2 north
        (0.25f, 0.375f, 0, 0.75f, 0.625f, 0.75f),    // 3 south
        (0.375f, 0.25f, 0.25f, 0.625f, 0.75f, 1),    // 4 west
        (0, 0.375f, 0.25f, 0.75f, 0.625f, 0.75f),    // 5 east
    ];

    // ── Construction ──────────────────────────────────────────────────────────

    public BlockPistonExtension(int id) : base(id, 107, Material.RockTransp)
    {
        SetHardness(0.5f);
        SetBlockName("pistonHead");
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Texture (spec §7.11) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face)
    {
        // Default — actual lookup needs meta from world context.
        // Renderer must call GetTextureForFace(face, meta) for correct result.
        return 107;
    }

    /// <summary>
    /// Returns the texture atlas index for the given face and block metadata.
    /// Spec: <c>acu.a(int face, int meta)</c>.
    /// </summary>
    public int GetTextureForFace(int face, int meta)
    {
        int facing = meta & 7;
        if (facing > 5) return BlockIndexInTexture;

        if (face == facing)
        {
            // Custom override (field a ≥ 0): not used in 1.0 — always default
            if ((meta & 8) != 0) return 106; // sticky arm front
            return 107;                        // normal arm front
        }
        if (face == OppFace[facing]) return 107; // back into piston body
        return 108;                              // side
    }

    // ── Collision boxes (spec §7.12) ─────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        // Primary box is face plate — shaft is handled via GetCollisionBoxList
        int facing = world.GetBlockMetadata(x, y, z) & 7;
        if (facing > 5) return null;
        var (x0, y0, z0, x1, y1, z1) = FacePlates[facing];
        return AxisAlignedBB.GetFromPool(x + x0, y + y0, z + z0, x + x1, y + y1, z + z1);
    }

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int facing = world.GetBlockMetadata(x, y, z) & 7;
        if (facing > 5) return AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 1, z + 1);
        var (x0, y0, z0, x1, y1, z1) = FacePlates[facing];
        return AxisAlignedBB.GetFromPool(x + x0, y + y0, z + z0, x + x1, y + y1, z + z1);
    }

    // ── Shaft collision (spec §7.12 — two-part AABB) ─────────────────────────

    public override void AddCollisionBoxesToList(IWorld world, int x, int y, int z, AxisAlignedBB clipBox, List<AxisAlignedBB> list)
    {
        int facing = world.GetBlockMetadata(x, y, z) & 7;
        if (facing > 5) return;

        // Part 1 — face plate
        var fp = FacePlates[facing];
        var facePlate = AxisAlignedBB.GetFromPool(x + fp.x0, y + fp.y0, z + fp.z0, x + fp.x1, y + fp.y1, z + fp.z1);
        if (clipBox.Intersects(facePlate)) list.Add(facePlate);

        // Part 2 — shaft
        var sh = Shafts[facing];
        var shaft = AxisAlignedBB.GetFromPool(x + sh.x0, y + sh.y0, z + sh.z0, x + sh.x1, y + sh.y1, z + sh.z1);
        if (clipBox.Intersects(shaft)) list.Add(shaft);
    }

    // ── onBlockRemoved (spec §7.14) — retract base piston if extended ─────────

    public override void OnBlockRemoved(IWorld world, int x, int y, int z)
    {
        int facing = world.GetBlockMetadata(x, y, z) & 7;
        if (facing > 5) { base.OnBlockRemoved(world, x, y, z); return; }

        int bx = x - DirX[facing];
        int by = y - DirY[facing];
        int bz = z - DirZ[facing];

        int baseId = world.GetBlockId(bx, by, bz);
        if (baseId != 29 && baseId != 33) { base.OnBlockRemoved(world, x, y, z); return; }

        int baseMeta = world.GetBlockMetadata(bx, by, bz);
        bool isExtended = (baseMeta & 8) != 0;
        if (!isExtended) { base.OnBlockRemoved(world, x, y, z); return; }

        // Retract the base piston
        Block? baseBlock = BlocksList[baseId];
        baseBlock?.OnBlockActivated(world, bx, by, bz, null!);
        world.SetBlock(bx, by, bz, 0);

        base.OnBlockRemoved(world, x, y, z);
    }

    // ── onNeighborBlockChange (spec §7.15) — defer to base piston ────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (world.IsClientSide || world is not World w) return;

        int facing = world.GetBlockMetadata(x, y, z) & 7;
        if (facing > 5) return;

        int bx = x - DirX[facing];
        int by = y - DirY[facing];
        int bz = z - DirZ[facing];

        int baseId = world.GetBlockId(bx, by, bz);
        if (baseId != 29 && baseId != 33)
        {
            // Orphaned arm — remove it
            w.SetBlock(x, y, z, 0);
            return;
        }

        // Forward to base piston
        BlocksList[baseId]?.OnNeighborBlockChange(world, bx, by, bz, neighbourId);
    }

    // ── No drops (arm is dropped by base piston during retraction) ────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 0;
}
