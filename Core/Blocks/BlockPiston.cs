using SpectraSharp.Core.TileEntity;

namespace SpectraSharp.Core.Blocks;

/// <summary>
/// Replica of <c>abr</c> (BlockPiston) — Block IDs 29 (sticky) and 33 (normal).
///
/// The piston base block. Reactive only — purely driven by neighbour-change and
/// placement events. Push/retract logic walks a block chain up to 13 blocks long.
///
/// Meta layout (spec §6):
///   bits 2-0: facing direction (0-5)
///   bit  3:   isExtended (0=retracted, 1=extended)
///
/// Quirk §10.1: static <c>cb</c> anti-reentrance suppresses all pistons during push/pull.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPiston_Spec.md §7.1-§7.10
/// </summary>
public sealed class BlockPiston : Block
{
    // ── Direction data (ot arrays, spec §3) ───────────────────────────────────

    private static readonly int[] OppFace = { 1, 0, 3, 2, 5, 4 }; // ot.a
    private static readonly int[] DirX    = {  0, 0,  0, 0, -1, 1 }; // ot.b
    private static readonly int[] DirY    = { -1, 1,  0, 0,  0, 0 }; // ot.c
    private static readonly int[] DirZ    = {  0, 0, -1, 1,  0, 0 }; // ot.d

    // ── Push limit (spec §5) ──────────────────────────────────────────────────

    private const int PushLimit = 13;

    // ── Quirk §10.1: class-level anti-reentrance guard ────────────────────────

    private static bool s_cb; // obf: cb — static, shared across ALL piston instances

    // ── Instance field ────────────────────────────────────────────────────────

    private readonly bool _isSticky; // obf: a — true for ID 29, false for ID 33

    // ── Construction ──────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="isSticky"/>: true = sticky (ID 29, texture 106), false = normal (ID 33, texture 107).
    /// </summary>
    public BlockPiston(int id, bool isSticky) : base(id, isSticky ? 106 : 107, Material.RockTransp)
    {
        _isSticky = isSticky;
        SetHardness(0.5f);
        SetBlockName("piston");
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Texture (spec §7.10) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face) => BlockIndexInTexture; // default; world-facing meta handled by renderer

    /// <summary>
    /// Texture lookup requiring meta context. Spec: <c>abr.a(int face, int meta)</c>.
    /// </summary>
    public int GetTextureForFace(int face, int meta)
    {
        int facing     = meta & 7;
        bool isExtended = (meta & 8) != 0;

        if (facing > 5) return BlockIndexInTexture;

        if (face == facing)
            return isExtended ? 110 : BlockIndexInTexture; // front: 110 when extended
        if (face == OppFace[facing]) return 109;           // back
        return 108;                                         // side
    }

    // ── Meta helpers ─────────────────────────────────────────────────────────

    private static int GetFacing(int meta)     => meta & 7;
    private static bool GetIsExtended(int meta) => (meta & 8) != 0;

    // ── onNeighborBlockChange / onBlockAdded (spec §7.3) ─────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        if (world.IsClientSide || world is not World w) return;
        if (s_cb) return; // anti-reentrance
        CheckAndTrigger(w, x, y, z);
    }

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        if (world.IsClientSide || world is not World w) return;
        if (s_cb) return;
        CheckAndTrigger(w, x, y, z);
    }

    // ── determineFacing (spec §7.1) ───────────────────────────────────────────

    private static int DetermineFacing(World world, int x, int y, int z, EntityPlayer player)
    {
        if (Math.Abs(player.PosX - x) < 2.0 && Math.Abs(player.PosZ - z) < 2.0)
        {
            double eyeY = player.PosY + 1.82 - player.PlayerEyeHeight;
            if (eyeY - y > 2.0) return 1;  // facing up
            if (y - eyeY > 0.0) return 0;  // facing down
        }

        int quadrant = (int)Math.Floor(player.RotationYaw * 4.0 / 360.0 + 0.5) & 3;
        return quadrant switch
        {
            0 => 2,   // north
            1 => 5,   // east
            2 => 3,   // south
            3 => 4,   // west
            _ => 0
        };
    }

    // ── checkAndTrigger (spec §7.4) ───────────────────────────────────────────

    private void CheckAndTrigger(World world, int x, int y, int z)
    {
        int  meta       = world.GetBlockMetadata(x, y, z);
        int  facing     = GetFacing(meta);
        bool isExtended = GetIsExtended(meta);

        if (meta == 7) return; // guard

        bool isPowered = IsPowered(world, x, y, z, facing);

        if (isPowered && !isExtended)
        {
            if (CanPush(world, x, y, z, facing))
                Activate(world, x, y, z, 0, facing);
            else
                world.SetMetadataQuiet(x, y, z, facing | 8); // cosmetic extend
        }
        else if (!isPowered && isExtended)
        {
            world.SetMetadataQuiet(x, y, z, facing);
            Activate(world, x, y, z, 1, facing);
        }
    }

    // ── isPowered (spec §7.5, 12 positions) ──────────────────────────────────

    private static bool IsPowered(World world, int x, int y, int z, int facing)
    {
        // Positions 1-6: all 6 faces except the one the arm faces
        if (facing != 0 && world.GetPower(x, y - 1, z, 0)) return true;
        if (facing != 1 && world.GetPower(x, y + 1, z, 1)) return true;
        if (facing != 2 && world.GetPower(x, y, z - 1, 2)) return true;
        if (facing != 3 && world.GetPower(x, y, z + 1, 3)) return true;
        if (facing != 5 && world.GetPower(x + 1, y, z, 5)) return true;
        if (facing != 4 && world.GetPower(x - 1, y, z, 4)) return true;

        // Positions 7-12: redstone on top-layer positions
        if (world.GetPower(x, y, z, 0))        return true; // directly on top
        if (world.GetPower(x, y + 2, z, 1))    return true;
        if (world.GetPower(x, y + 1, z - 1, 2)) return true;
        if (world.GetPower(x, y + 1, z + 1, 3)) return true;
        if (world.GetPower(x - 1, y + 1, z, 4)) return true;
        if (world.GetPower(x + 1, y + 1, z, 5)) return true;

        return false;
    }

    // ── canPush (spec §7.6) ───────────────────────────────────────────────────

    private static bool CanPush(World world, int x, int y, int z, int facing)
    {
        int nx = x + DirX[facing];
        int ny = y + DirY[facing];
        int nz = z + DirZ[facing];

        for (int i = 0; i < PushLimit; i++)
        {
            if (ny <= 0 || ny >= World.WorldHeight - 1) return false;

            int id = world.GetBlockId(nx, ny, nz);

            if (id == 0) return true; // air — space found

            if (!IsPushable(id, world, nx, ny, nz, true)) return false;

            // Liquid → displace (remove and treat as space)
            if (Block.BlocksList[id]?.BlockMaterial.IsLiquid() == true)
            {
                world.SetBlock(nx, ny, nz, 0);
                return true;
            }

            nx += DirX[facing];
            ny += DirY[facing];
            nz += DirZ[facing];

            if (i == PushLimit - 1) return false; // walked 13 without space
        }

        return false;
    }

    // ── pushability check (spec §7.7, 7 rules) ───────────────────────────────

    private static bool IsPushable(int blockId, World world, int x, int y, int z, bool checkSelf)
    {
        if (blockId == 7) return false;  // bedrock (ID 7)

        // Extended pistons cannot be pushed
        if (blockId == 29 || blockId == 33)
        {
            int pistonMeta = world.GetBlockMetadata(x, y, z);
            if (GetIsExtended(pistonMeta)) return false;
            return true;
        }

        Block? block = BlocksList[blockId];
        if (block == null) return true;

        if (block.BlockHardness < 0.0f) return false;             // hardness = -1 (unbreakable)
        if (Block.IsBlockContainer[blockId]) return false;         // BlockContainer (chest, jukebox, etc.)
        // Material type check: J==2 (portal-like, MaterialPushDestroys) — not pushable
        // J==1 (liquid) only allowed when checkSelf=true
        if (!checkSelf && block.BlockMaterial.IsLiquid()) return false;

        return true;
    }

    // ── Activate: extend/retract dispatch (spec §7.9) ─────────────────────────

    private void Activate(World world, int x, int y, int z, int type, int facing)
    {
        s_cb = true;
        try
        {
            if (type == 0) // extend
            {
                if (DoExtend(world, x, y, z, facing))
                {
                    world.SetMetadataQuiet(x, y, z, facing | 8);
                    world.NotifyNeighbors(x, y, z, BlockID);
                    world.PlayAuxSFX(null, 1000, x, y, z, 0); // piston out sound (stub eventId)
                }
                else
                {
                    world.SetMetadataQuiet(x, y, z, facing);
                }
            }
            else // retract (type == 1)
            {
                int ax = x + DirX[facing];
                int ay = y + DirY[facing];
                int az = z + DirZ[facing];

                // Instant-finalize arm if it is still animating
                if (world.GetTileEntity(ax, ay, az) is TileEntityPiston armTe)
                    armTe.InstantFinalize();

                // Place moving block at piston base position to animate retraction
                world.SetBlockAndMetadata(x, y, z, 36, facing);
                var baseTe = new TileEntityPiston(BlockID, world.GetBlockMetadata(x, y, z), facing, false, true);
                world.SetTileEntity(x, y, z, baseTe);

                if (_isSticky)
                {
                    // Try to pull the block 2 ahead
                    int px = x + DirX[facing] * 2;
                    int py = y + DirY[facing] * 2;
                    int pz = z + DirZ[facing] * 2;

                    int pullId   = world.GetBlockId(px, py, pz);
                    int pullMeta = world.GetBlockMetadata(px, py, pz);

                    // Handle moving block in the path (quirk §10.5)
                    if (pullId == 36 && world.GetTileEntity(px, py, pz) is TileEntityPiston movingTe
                        && movingTe.Facing == facing && movingTe.IsExtending)
                    {
                        movingTe.InstantFinalize();
                        pullId   = movingTe.StoredBlockId;
                        pullMeta = movingTe.StoredBlockMeta;
                    }

                    if (pullId > 0 && IsPushable(pullId, world, px, py, pz, false))
                    {
                        // Pull the block into arm position
                        world.SetBlockAndMetadata(ax, ay, az, 36, pullMeta);
                        var pullTe = new TileEntityPiston(pullId, pullMeta, facing, false, false);
                        world.SetTileEntity(ax, ay, az, pullTe);
                        world.SetBlock(px, py, pz, 0);
                    }
                    else
                    {
                        // Nothing to pull — just remove arm
                        world.SetBlock(ax, ay, az, 0);
                    }
                }
                else
                {
                    // Non-sticky — just remove arm
                    world.SetBlock(ax, ay, az, 0);
                }

                world.PlayAuxSFX(null, 1001, x, y, z, 0); // piston in sound (stub eventId)
            }
        }
        finally
        {
            s_cb = false;
        }
    }

    // ── doExtend (spec §7.8) ──────────────────────────────────────────────────

    private bool DoExtend(World world, int x, int y, int z, int facing)
    {
        int nx = x + DirX[facing];
        int ny = y + DirY[facing];
        int nz = z + DirZ[facing];

        // Phase 1 — find the endpoint (same walk as canPush)
        int endX = nx, endY = ny, endZ = nz;
        for (;;)
        {
            int id = world.GetBlockId(endX, endY, endZ);
            if (id == 0) break; // air found

            if (world.GetBlockMaterial(endX, endY, endZ).IsLiquid())
            {
                world.SetBlock(endX, endY, endZ, 0); // displace liquid
                break;
            }

            if (!IsPushable(id, world, endX, endY, endZ, true)) return false;

            endX += DirX[facing];
            endY += DirY[facing];
            endZ += DirZ[facing];
        }

        // Phase 2 — backward pass: slide each block forward by one
        int curX = endX, curY = endY, curZ = endZ;
        while (curX != x || curY != y || curZ != z)
        {
            int prevX = curX - DirX[facing];
            int prevY = curY - DirY[facing];
            int prevZ = curZ - DirZ[facing];

            if (prevX == x && prevY == y && prevZ == z)
            {
                // Previous is the piston base itself → place arm
                int armMeta = facing | (_isSticky ? 8 : 0);
                world.SetBlockAndMetadata(curX, curY, curZ, 36, armMeta);
                var armTe = new TileEntityPiston(34, armMeta, facing, true, true);
                world.SetTileEntity(curX, curY, curZ, armTe);
            }
            else
            {
                // Move the block from prev to cur
                int prevId   = world.GetBlockId(prevX, prevY, prevZ);
                int prevMeta = world.GetBlockMetadata(prevX, prevY, prevZ);
                world.SetBlockAndMetadata(curX, curY, curZ, 36, prevMeta);
                var blockTe = new TileEntityPiston(prevId, prevMeta, facing, true, false);
                world.SetTileEntity(curX, curY, curZ, blockTe);
            }

            curX = prevX;
            curY = prevY;
            curZ = prevZ;
        }

        return true;
    }

    // ── Drops ──────────────────────────────────────────────────────────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => _isSticky ? 106 : 107; // item IDs match block IDs (spec §5)
}
