namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>ni</c> (BlockFarmland) — tilled soil beneath crops, ID 60.
///
/// Metadata 0 = dry; 1–7 = moist (only texture difference is 0 vs &gt;0).
/// Random tick: checks 9×2×9 water radius + rain exposure to maintain moisture;
/// without water dries 1 step/tick; at 0 with no crops above reverts to dirt (ID 3).
/// Entity walking: 25% chance per step to trample back to dirt.
/// Neighbor change: liquid placed above → immediate reversion to dirt.
///
/// AABB quirk (spec §B.8 quirk 5): visual/selection = 15/16 tall; collision = full cube.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockCrops_Spec.md §B
/// </summary>
public class BlockFarmland : Block
{
    // ── Constructor (spec §B.2) ───────────────────────────────────────────────

    public BlockFarmland(int id) : base(id, 87, Material.Ground)
    {
        SetBounds(0f, 0f, 0f, 1f, 0.9375f, 1f); // 15/16 tall — visual/selection only
        SetLightOpacity(255);                     // h(255) = neighbor-max light (spec §B.8 quirk 4)
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Collision AABB (spec §B.4 b(ry) / §B.8 quirk 5) ─────────────────────

    /// <summary>
    /// Full-cube world-space collision AABB (1×1×1).
    /// Entities cannot sink into farmland despite 15/16 visual height.
    /// </summary>
    public override AxisAlignedBB GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1, y + 1, z + 1);

    // ── Textures (spec §B.4 a(int face, int meta)) ───────────────────────────

    /// <summary>
    /// Top face: bL=87 (dry) when meta=0, bL-1=86 (moist) when meta>0.
    /// All other faces: texture 2 (dirt side).
    /// </summary>
    public override int GetTextureForFaceAndMeta(int face, int meta)
    {
        if (face == 1) // top face
            return meta > 0 ? BlockIndexInTexture - 1 : BlockIndexInTexture; // 86 moist / 87 dry
        return 2; // all sides: dirt texture
    }

    // ── Random tick (spec §B.4 a(ry, x, y, z, Random)) ──────────────────────

    /// <summary>
    /// Moisture management: hydrate to 7 if water is nearby or rain above;
    /// otherwise dry one step. At 0 with no crops above, revert to dirt (ID 3).
    /// </summary>
    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (IsWaterNearby(world, x, y, z) || world.IsBlockExposedToRain(x, y + 1, z))
        {
            world.SetMetadata(x, y, z, 7); // max moisture
        }
        else
        {
            int meta = world.GetBlockMetadata(x, y, z);
            if (meta > 0)
            {
                world.SetMetadata(x, y, z, meta - 1);
            }
            else if (!HasCropsAbove(world, x, y, z))
            {
                world.SetBlock(x, y, z, 3); // revert to dirt (yy.v = ID 3)
            }
        }
    }

    // ── Water detection (spec §B.4 h()) ──────────────────────────────────────

    /// <summary>
    /// obf: <c>h(ry, x, y, z)</c> — checks 9×2×9 area for water material.
    /// </summary>
    private static bool IsWaterNearby(IWorld world, int x, int y, int z)
    {
        for (int bx = x - 4; bx <= x + 4; bx++)
        for (int by = y; by <= y + 1; by++)
        for (int bz = z - 4; bz <= z + 4; bz++)
        {
            if (world.GetBlockMaterial(bx, by, bz).IsLiquid())
                return true; // water material (p.g) — includes flowing/still water
        }
        return false;
    }

    // ── Crops-above check (spec §B.4 g()) ────────────────────────────────────

    /// <summary>
    /// obf: <c>g(ry, x, y, z)</c> — true if block directly above is wheat (59),
    /// melon stem (106), or pumpkin stem (105).
    /// The loop-over-zero-range decompiler artefact (spec quirk 1) collapses to a single check.
    /// </summary>
    private static bool HasCropsAbove(IWorld world, int x, int y, int z)
    {
        int above = world.GetBlockId(x, y + 1, z);
        return above == 59   // wheat crops (yy.az)
            || above == 106  // melon stem  (yy.bt)
            || above == 105; // pumpkin stem (yy.bs)
    }

    // ── Entity walking / trampling (spec §B.4 b(ry,...,ia)) ──────────────────

    /// <summary>
    /// obf: <c>b(ry, x, y, z, ia)</c> — 25% chance per step to revert to dirt.
    /// Uses world.Random directly (spec §B.8 quirk 3).
    /// </summary>
    public override void OnEntityWalking(IWorld world, int x, int y, int z, Entity entity)
    {
        if (world.Random.NextInt(4) == 0)
            world.SetBlock(x, y, z, 3); // trample: revert to dirt
    }

    // ── Neighbor change (spec §B.4 a(ry, x, y, z, int)) ─────────────────────

    /// <summary>
    /// obf: <c>a(ry, x, y, z, int)</c> — liquid placed above → revert to dirt immediately.
    /// </summary>
    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        base.OnNeighborBlockChange(world, x, y, z, neighbourId);
        if (world.GetBlockMaterial(x, y + 1, z).IsLiquid())
            world.SetBlock(x, y, z, 3); // liquid above: revert to dirt
    }

    // ── Drops (spec §B.4 a(int meta, Random, int fortune)) ───────────────────

    /// <summary>obf: <c>a(int meta, Random, int fortune)</c> — always drops dirt (ID 3).</summary>
    public override int IdDropped(int metadata, JavaRandom rng, int fortune)
    {
        Block? dirt = BlocksList[3];
        return dirt?.IdDropped(0, rng, fortune) ?? 3;
    }
}
