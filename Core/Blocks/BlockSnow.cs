namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aif</c> (BlockSnow) — Block ID 78.
/// Thin snow layer placed on cold-biome surfaces. Layer count in metadata bits 0-2 (0-7).
///
/// Quirks preserved (spec §12):
///   1. Snow melts silently on random tick (no item drop via quantityDropped=0).
///   2. Player harvest always drops exactly 1 snowball regardless of layer count.
///   3. No collision for layers 0-2; collision up to y=0.5 for layers 3-7.
///   4. Snow cannot be placed on ice (canBlockStay check: blockBelow != iceId).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/SnowIce_Spec.md §2-4
/// </summary>
public sealed class BlockSnow : Block
{
    private const int SnowballItemId = 332; // acy.aC.bM

    public BlockSnow(int id) : base(id, 66, Material.Snow) // p.u = snow material
    {
        Slipperiness = 0.6f;
        LightOpacity[id] = 0;
        IsOpaqueCubeArr[id] = false;
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    // ── Bounds (spec §4) ─────────────────────────────────────────────────────

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        int layers = world.GetBlockMetadata(x, y, z) & 7;
        float height = (2 * (1 + layers)) / 16.0f;
        SetBounds(0, 0, 0, 1, height, 1);
    }

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        int layers = world.GetBlockMetadata(x, y, z) & 7;
        if (layers >= 3)
            return AxisAlignedBB.GetFromPool(
                x + MinX, y + MinY, z + MinZ,
                x + MaxX, y + 0.5f,  z + MaxZ);
        return null; // no collision for thin snow (quirk 3)
    }

    // ── Stability (spec §4) ──────────────────────────────────────────────────

    /// <summary>Snow can stay only on solid, render-as-normal blocks (not ice).</summary>
    public override bool CanBlockStay(IWorld world, int x, int y, int z)
    {
        int below = world.GetBlockId(x, y - 1, z);
        if (below == 0) return false;                      // air below
        if (below == 79) return false;                     // no snow on ice (quirk 4)
        if (!IsOpaqueCubeArr[below]) return false;         // must render as normal
        return world.GetBlockMaterial(x, y - 1, z).IsSolid();
    }

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborId)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItemWithChance(world, x, y, z, world.GetBlockMetadata(x, y, z), 1.0f, 0);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── Random tick — melt (spec §4, quirk 1) ───────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // Block light comes from ReadLightWithNeighborMax(Block, x, y, z)
        int blockLight = world.GetLightValue(x, y, z, 0) & 0xF;
        if (blockLight > 11)
            world.SetBlock(x, y, z, 0); // silent melt, no item drop (quirk 1)
    }

    // ── Drops (spec §4) ─────────────────────────────────────────────────────

    /// <summary>Standard drop pipeline: quantityDropped=0 → no drop on melt.</summary>
    public override int QuantityDropped(JavaRandom rng) => 0;

    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 0;

    // ── OnBlockDestroyedByPlayer — harvest 1 snowball (spec §4, quirk 2) ────

    public override void OnBlockDestroyedByPlayer(IWorld world, int x, int y, int z, int meta)
    {
        // Always drop exactly 1 snowball regardless of layer count
        float jx = (float)world.Random.NextFloat() * 0.7f + 0.15f;
        float jy = (float)world.Random.NextFloat() * 0.7f + 0.15f;
        float jz = (float)world.Random.NextFloat() * 0.7f + 0.15f;
        var item = new EntityItem((World)world, x + jx, y + jy, z + jz,
            new ItemStack(SnowballItemId, 1, 0));
        item.PickupDelay = 10;
        world.SpawnEntity(item);
    }

    // ── IsSideSolid (spec §4) — solid on top face only ─────────────────────

    public override bool IsSideSolid(IBlockAccess world, int x, int y, int z, int face)
        => face == 1;
}
