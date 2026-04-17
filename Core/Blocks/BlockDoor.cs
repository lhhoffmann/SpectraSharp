namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>uc</c> (BlockDoor) — two-block-tall door with open/closed state.
///
/// IDs: 64 (wood door, Material.Plants), 71 (iron door, Material.RockTransp2).
/// Bottom half: bits 0-1 = facing, bit 2 = isOpen. Top half: bit 3 = 1.
/// Wood doors toggle on right-click; iron doors only via redstone.
/// Height per half = 1.0 block; panel width = 0.1875 (3/16).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockDoor_Spec.md
/// </summary>
public class BlockDoor : Block
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly bool _isIron; // true = iron door (ID 71), false = wood (ID 64)

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    public BlockDoor(int id, Material material) : base(id, material)
    {
        _isIron = (material == Material.RockTransp2); // p.f = iron
        BlockIndexInTexture = _isIron ? 98 : 97;      // spec §3 step 2–3
        SetBounds(0f, 0f, 0f, 1f, 1f, 1f);           // spec §3 step 4 (full-cube default)
        SetLightOpacity(7);                             // spec §4.3
    }

    // ── Properties (spec §4.1–4.3) ───────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — isOpaqueCube. Always false.</summary>
    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 7;

    /// <summary>obf: <c>b()</c> — renderAsNormal. Always false.</summary>
    public override bool RenderAsNormalBlock() => false;

    // ── AABB helpers (spec §4 e/f) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>f(int meta)</c> — maps raw metadata bits 0-2 to the physical panel direction 0-3.
    /// Spec §4 table: closed = (meta-1)&amp;3, open = meta&amp;3.
    /// </summary>
    private static int ComputeEffectiveFacing(int meta)
        => (meta & 4) == 0 ? (meta - 1) & 3 : meta & 3;

    /// <summary>
    /// obf: <c>e(int effectiveFacing)</c> — sets shared block bounds (0.1875 thickness).
    /// Spec §4 / §7 AABB table. The initial reset to (0,0,0,1,2,1) is dead code (spec quirk 1).
    /// </summary>
    private void SetBoundsForFacing(int effectiveFacing)
    {
        const float T = 0.1875f;
        SetBounds(0f, 0f, 0f, 1f, 2f, 1f); // spec quirk 1: dead-code reset, preserved
        switch (effectiveFacing)
        {
            case 0: SetBounds(0f,   0f, 0f,   1f,   1f, T    ); break; // south face
            case 1: SetBounds(1f-T, 0f, 0f,   1f,   1f, 1f   ); break; // east face
            case 2: SetBounds(0f,   0f, 1f-T, 1f,   1f, 1f   ); break; // north face
            case 3: SetBounds(0f,   0f, 0f,   T,    1f, 1f   ); break; // west face
        }
    }

    // ── Bounds (spec §4 b(kq)/b(ry)/c_(ry)) ─────────────────────────────────

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        SetBoundsForFacing(ComputeEffectiveFacing(meta));
    }

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        SetBlockBoundsBasedOnState(world, x, y, z);
        return base.GetCollisionBoundingBoxFromPool(world, x, y, z);
    }

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        SetBlockBoundsBasedOnState(world, x, y, z);
        return base.GetSelectedBoundingBoxFromPool(world, x, y, z);
    }

    // ── Interaction (spec §4 a(ry...) / b(ry...) / a(ry,bool)) ──────────────

    /// <summary>
    /// obf: <c>a(ry, x, y, z, vi)</c> — right-click toggle.
    /// Iron doors: consumes click silently (spec quirk 2).
    /// Wood doors: delegates top-half clicks to bottom; toggles open bit on both halves.
    /// </summary>
    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (_isIron) return true; // spec quirk 2: iron door swallows click without acting

        int meta = world.GetBlockMetadata(x, y, z);

        if ((meta & 8) != 0) // top half
        {
            if (world.GetBlockId(x, y - 1, z) == BlockID)
                OnBlockActivated(world, x, y - 1, z, player);
            return true;
        }

        // Bottom half
        int topMeta = (meta ^ 4) + 8; // toggle open bit + mark as top half
        if (world.GetBlockId(x, y + 1, z) == BlockID)
            world.SetMetadata(x, y + 1, z, topMeta);

        world.SetMetadata(x, y, z, meta ^ 4);
        world.NotifyNeighbors(x, y, z, BlockID);
        world.PlayAuxSFX(player, 1003, x, y, z, 0); // door open/close sound
        return true;
    }

    /// <summary>
    /// obf: <c>b(ry, x, y, z, vi)</c> — entity-walking activation. Delegates to right-click.
    /// For wood doors this toggles the door; for iron doors it is a no-op (spec §4 / quirk 3 open Q).
    /// </summary>
    public override void OnEntityWalking(IWorld world, int x, int y, int z, Entity entity)
    {
        if (entity is EntityPlayer player)
            OnBlockActivated(world, x, y, z, player);
    }

    /// <summary>
    /// obf: <c>a(ry, x, y, z, bool open)</c> — redstone-driven state change.
    /// Only acts if state would change; both halves updated simultaneously.
    /// </summary>
    public void SetDoorState(IWorld world, int x, int y, int z, bool open)
    {
        int meta = world.GetBlockMetadata(x, y, z);

        if ((meta & 8) != 0) // top half: delegate to bottom
        {
            if (world.GetBlockId(x, y - 1, z) == BlockID)
                SetDoorState(world, x, y - 1, z, open);
            return;
        }

        bool current = (meta & 4) != 0;
        if (current == open) return; // already correct state

        if (world.GetBlockId(x, y + 1, z) == BlockID)
            world.SetMetadata(x, y + 1, z, (meta ^ 4) + 8);
        world.SetMetadata(x, y, z, meta ^ 4);
        world.NotifyNeighbors(x, y, z, BlockID);
        world.PlayAuxSFX(null, 1003, x, y, z, 0); // null = ambient (spec quirk 4)
    }

    // ── Structural integrity / redstone (spec §4 a(ry,x,y,z,int)) ────────────

    /// <summary>
    /// obf: <c>a(ry, x, y, z, int neighbourId)</c> — neighbour-change handler.
    /// Removes orphaned halves; handles redstone-driven open/close.
    /// </summary>
    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        int meta = world.GetBlockMetadata(x, y, z);

        if ((meta & 8) != 0) // TOP HALF
        {
            if (world.GetBlockId(x, y - 1, z) != BlockID)
                world.SetBlock(x, y, z, 0); // orphaned top: remove
            // redstone propagated to bottom when it changes
            return;
        }

        // BOTTOM HALF
        bool removed = false;

        if (world.GetBlockId(x, y + 1, z) != BlockID)
        {
            world.SetBlock(x, y, z, 0);
            removed = true;
        }

        // Check solid support below
        int belowId = world.GetBlockId(x, y - 1, z);
        bool hasSolidSupport = BlocksList[belowId]?.IsOpaqueCube() ?? false;
        if (!hasSolidSupport)
        {
            if (!removed) world.SetBlock(x, y, z, 0);
            removed = true;
            if (world.GetBlockId(x, y + 1, z) == BlockID)
                world.SetBlock(x, y + 1, z, 0);
        }

        if (removed)
        {
            if (!world.IsClientSide)
                DropBlockAsItemWithChance(world, x, y, z, meta, 1.0f, 0);
        }
        else if (neighbourId > 0)
        {
            bool powered = world.IsBlockIndirectlyReceivingPower(x, y, z)
                        || world.IsBlockIndirectlyReceivingPower(x, y + 1, z);
            SetDoorState(world, x, y, z, powered);
        }
    }

    // ── canBlockStay (spec §4 c()) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>c(ry, x, y, z)</c> — requires y &lt; 127, solid block below (spec §4 / quirk 5).
    /// </summary>
    public override bool CanBlockStay(IWorld world, int x, int y, int z)
    {
        if (y >= 127) return false;
        int belowId = world.GetBlockId(x, y - 1, z);
        return BlocksList[belowId]?.IsOpaqueCube() ?? false;
    }

    // ── Textures (spec §4 a(int face, int meta)) ──────────────────────────────

    /// <summary>
    /// obf: <c>a(int meta, int face)</c> — door face texture.
    /// Top/bottom faces return bL; lateral faces use mirroring formula.
    /// Negative return value = horizontally mirrored texture (spec §4.10).
    /// </summary>
    public override int GetTextureForFaceAndMeta(int face, int meta)
    {
        if (face == 0 || face == 1) return BlockIndexInTexture;

        int effectiveFacing = ComputeEffectiveFacing(meta);

        bool condition = ((effectiveFacing == 0 || effectiveFacing == 2) ^ (face <= 3));
        if (condition) return BlockIndexInTexture;

        int var4 = effectiveFacing / 2 + ((face & 1) ^ effectiveFacing);
        var4 += (meta & 4) / 4;                          // +1 if open
        int var5 = BlockIndexInTexture - (meta & 8) * 2; // top-half uses bL-16
        if ((var4 & 1) != 0) var5 = -var5;               // negative = mirrored
        return var5;
    }

    // ── Drops (spec §4 a(int meta, Random, int fortune)) ─────────────────────

    /// <summary>
    /// obf: <c>a(int meta, Random, int fortune)</c> — top half drops nothing (spec quirk 3).
    /// Bottom half drops the door block item.
    /// </summary>
    public override int IdDropped(int metadata, JavaRandom rng, int fortune)
        => (metadata & 8) != 0 ? 0 : BlockID;

    // ── Mobility (spec §4 i()) ────────────────────────────────────────────────

    /// <summary>obf: <c>i()</c> — getMobilityFlag. Returns 1 (normal pushable). Spec §4 / open Q §9.1.</summary>
    public override int GetMobilityFlag() => 1;

    // ── Static utility (spec §4 g(int meta)) ─────────────────────────────────

    /// <summary>
    /// obf: <c>g(int meta)</c> — true if the door is currently open. Used by rendering.
    /// </summary>
    public static bool IsOpen(int meta) => (meta & 4) != 0;
}
