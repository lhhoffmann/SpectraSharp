namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>afr</c> (BlockRail) base class and concrete subclasses.
///
/// Block IDs:
///   66 = Rail (normal, metadata 0-9 including curves)
///   27 = Powered Rail (metadata 0-5 + bit 3 = powered)
///   28 = Detector Rail (metadata 0-5 + bit 3 = minecart-present)
///
/// Metadata shape encoding:
///   0=Flat N-S, 1=Flat E-W, 2=Ascending E, 3=Ascending W, 4=Ascending N, 5=Ascending S
///   6=Curve NE, 7=Curve SE, 8=Curve SW, 9=Curve NW  (normal rail only)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRail_Spec.md
/// </summary>
public abstract class BlockRailBase : Block
{
    /// <summary>obf: <c>a</c> — true for powered/detector rail (no curves allowed).</summary>
    protected readonly bool _isSpecial;

    protected BlockRailBase(int id, int textureIndex, bool isSpecial)
        : base(id, textureIndex, Material.MatWeb_P)
    {
        _isSpecial = isSpecial;
        SetHardness(0.7f);
        SetStepSound(SoundStoneHighPitch2);
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 9;
    public override bool RenderAsNormalBlock() => false;
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => null; // no collision

    // ── Selection AABB (spec §6) ──────────────────────────────────────────────

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int meta  = world.GetBlockMetadata(x, y, z);
        int shape = _isSpecial ? (meta & 7) : meta;
        // Slope shapes (2-5): taller hitbox
        if (shape >= 2 && shape <= 5)
            return AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 0.625, z + 1);
        return AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 0.125, z + 1);
    }

    // ── Placement validity (spec §7) ─────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => world.GetBlockId(x, y - 1, z) != 0
           && IsOpaqueCubeArr[world.GetBlockId(x, y - 1, z)];

    // ── Placement hook (spec §8) ──────────────────────────────────────────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        base.OnBlockAdded(world, x, y, z);
        if (!world.IsClientSide)
            AutoConnect(world, x, y, z);
    }

    // ── Neighbor change (spec §9 / support check) ─────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborId)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItemWithChance(world, x, y, z, 0, 1.0f, 0);
            world.SetBlock(x, y, z, 0);
            return;
        }
        AutoConnect(world, x, y, z);
    }

    // ── Drops (spec §13) ─────────────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 1;

    // ── Static helpers (spec §2) ──────────────────────────────────────────────

    public static bool IsRailAt(IWorld world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        return id == 27 || id == 28 || id == 66;
    }

    public static bool IsRailId(int blockId)
        => blockId == 27 || blockId == 28 || blockId == 66;

    // ── Auto-connect (simplified aiq logic) ──────────────────────────────────

    /// <summary>
    /// Scans adjacent positions and updates the rail's metadata to the best-fit shape.
    /// Implements the core of the <c>aiq</c> RailLogic class (spec §12).
    /// </summary>
    protected void AutoConnect(IWorld world, int x, int y, int z)
    {
        // Check which of the 4 cardinal directions have an adjacent rail
        // (at same Y, Y+1 for slope approach, or Y-1 for slope descent)
        bool connN = HasRailNeighbor(world, x, y, z - 1); // -Z = North
        bool connS = HasRailNeighbor(world, x, y, z + 1); // +Z = South
        bool connE = HasRailNeighbor(world, x + 1, y, z); // +X = East
        bool connW = HasRailNeighbor(world, x - 1, y, z); // -X = West

        // Slope detection: check adjacent Y+1 positions
        bool slopeAscE = IsRailAt(world, x + 1, y + 1, z);
        bool slopeAscW = IsRailAt(world, x - 1, y + 1, z);
        bool slopeAscN = IsRailAt(world, x, y + 1, z - 1);
        bool slopeAscS = IsRailAt(world, x, y + 1, z + 1);

        int meta = ComputeShape(connN, connS, connE, connW,
                                slopeAscN, slopeAscS, slopeAscE, slopeAscW);

        // Preserve bit 3 for special rails (powered/detector state)
        if (_isSpecial)
            meta = (world.GetBlockMetadata(x, y, z) & 8) | (meta & 7);

        world.SetMetadata(x, y, z, meta);
    }

    private static bool HasRailNeighbor(IWorld world, int nx, int y, int nz)
        => IsRailAt(world, nx, y, nz)
           || IsRailAt(world, nx, y - 1, nz)
           || IsRailAt(world, nx, y + 1, nz);

    private int ComputeShape(bool n, bool s, bool e, bool w,
                             bool slopeN, bool slopeS, bool slopeE, bool slopeW)
    {
        // Slope takes priority
        if (slopeE && !slopeW) return 2; // ascending to East
        if (slopeW && !slopeE) return 3; // ascending to West
        if (slopeN && !slopeS) return 4; // ascending to North
        if (slopeS && !slopeN) return 5; // ascending to South

        // Count connections
        int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

        if (!_isSpecial && count == 2)
        {
            // Two connections — choose straight or curve
            if (n && s) return 0; // straight N-S
            if (e && w) return 1; // straight E-W
            if (n && e) return 6; // curve NE
            if (s && e) return 7; // curve SE
            if (s && w) return 8; // curve SW
            if (n && w) return 9; // curve NW
        }

        // Default to axis with most connections, or N-S
        if (e || w) return 1; // E-W if any E/W neighbor
        return 0;             // N-S default
    }
}

/// <summary>Normal Rail — ID 66. Supports curves (metadata 0-9).</summary>
public sealed class BlockRail : BlockRailBase
{
    public BlockRail() : base(66, 128, isSpecial: false)
    {
        SetBlockName("rail");
    }

    public override int GetTextureIndex(int face)
        => BlockIndexInTexture; // bL = 128

    public override int GetTextureForFaceAndMeta(int face, int meta)
        => (meta >= 6 && meta <= 9) ? BlockIndexInTexture - 16 : BlockIndexInTexture;
}

/// <summary>Powered Rail — ID 27. Boosts/brakes minecarts; no curves.</summary>
public sealed class BlockPoweredRail : BlockRailBase
{
    public BlockPoweredRail() : base(27, 179, isSpecial: true)
    {
        SetBlockName("goldenRail");
    }

    public override int GetTextureForFaceAndMeta(int face, int meta)
        => (meta & 8) == 0 ? BlockIndexInTexture - 16 : BlockIndexInTexture;

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborId)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItemWithChance(world, x, y, z, 0, 1.0f, 0);
            world.SetBlock(x, y, z, 0);
            return;
        }

        int meta = world.GetBlockMetadata(x, y, z);
        bool wasPowered = (meta & 8) != 0;

        // Check direct redstone power (spec §9 step 1)
        bool isPowered = world.IsBlockIndirectlyReceivingPower(x, y, z)
                      || world.IsBlockIndirectlyReceivingPower(x, y + 1, z)
                      || CheckRailPowerPropagation(world, x, y, z, meta & 7, depth: 0);

        if (isPowered != wasPowered)
        {
            int newMeta = (meta & 7) | (isPowered ? 8 : 0);
            world.SetMetadata(x, y, z, newMeta);
            world.NotifyNeighbors(x, y - 1, z, BlockId);
        }

        AutoConnect(world, x, y, z);
    }

    /// <summary>
    /// Scans up to 8 rail segments for a powered powered-rail (spec §10).
    /// Simplified: checks 1 step in each direction of current shape.
    /// </summary>
    private static bool CheckRailPowerPropagation(IWorld world, int x, int y, int z, int shape, int depth)
    {
        if (depth >= 8) return false;

        // Determine the two directions this rail segment connects
        (int dx1, int dz1, int dx2, int dz2) = shape switch
        {
            0 => (0, -1,  0,  1), // N-S
            1 => (-1, 0,  1,  0), // E-W
            2 => ( 1, 0, -1,  0), // ascending E
            3 => (-1, 0,  1,  0), // ascending W
            4 => ( 0,-1,  0,  1), // ascending N
            5 => ( 0, 1,  0, -1), // ascending S
            _ => (0, -1,  0,  1)
        };

        // Check each neighbor
        foreach (var (dx, dz) in new[] { (dx1, dz1), (dx2, dz2) })
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx, ny = y + dy, nz = z + dz;
                if (world.GetBlockId(nx, ny, nz) != 27) continue;

                int nMeta = world.GetBlockMetadata(nx, ny, nz);
                if ((nMeta & 8) != 0
                    && (world.IsBlockIndirectlyReceivingPower(nx, ny, nz)
                        || world.IsBlockIndirectlyReceivingPower(nx, ny + 1, nz)))
                    return true;

                return CheckRailPowerPropagation(world, nx, ny, nz, nMeta & 7, depth + 1);
            }
        }
        return false;
    }
}

/// <summary>Detector Rail — ID 28. Emits redstone when a minecart is on it.</summary>
public sealed class BlockDetectorRail : BlockRailBase
{
    public BlockDetectorRail() : base(28, 195, isSpecial: true)
    {
        SetBlockName("detectorRail");
    }

    public override int GetTextureForFaceAndMeta(int face, int meta)
        => (meta & 8) == 0 ? BlockIndexInTexture - 16 : BlockIndexInTexture;

    // Detector rail provides weak redstone power when bit 3 is set (minecart present)
    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
        => (world.GetBlockMetadata(x, y, z) & 8) != 0;

    public override bool CanProvidePower() => true;
}
