namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>ahq</c> (BlockIce) — Block ID 79.
/// Frozen water surface. Slippery (friction 0.98F). Melts to still water (ID 9) at block light > 10.
/// When mined over air/liquid, places flowing water (ID 8) instead.
///
/// Quirks preserved (spec §12):
///   1. randomTick melt → still water (ID 9); mined over air/liquid → flowing water (ID 8).
///   2. quantityDropped=0 — ice always drops nothing.
///   3. Slipperiness 0.98F registered into Block.SlipperinessMap at construction.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/SnowIce_Spec.md §5-7
/// </summary>
public sealed class BlockIce : Block
{
    public BlockIce(int id) : base(id, 67, Material.Mat_T) // p.t = ice material
    {
        Slipperiness = 0.98f;
        SlipperinessMap[id] = 0.98f;
        LightOpacity[id] = 1; // slightly opaque (spec §7, melt threshold = 11-1 = 10)
    }

    // ── Drops (spec §7, quirk 2) ─────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 0;
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 0;

    // ── Harvest (spec §7, quirk 1) ───────────────────────────────────────────

    public override void OnBlockDestroyedByPlayer(IWorld world, int x, int y, int z, int meta)
    {
        // Ice drops nothing; determine what to place based on block below (spec §7, quirk 1)
        Material below = world.GetBlockMaterial(x, y - 1, z);
        if (below.IsLiquid() || world.GetBlockId(x, y - 1, z) == 0)
            world.SetBlock(x, y, z, 8); // place flowing water over void/liquid
        // else: solid below → block simply removed (no water replacement)
    }

    // ── Random tick — melt (spec §7) ─────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // threshold = 11 - lightOpacity(1) = 10 (spec §7)
        int blockLight = world.GetLightValue(x, y, z, 0) & 0xF;
        if (blockLight > 10)
            world.SetBlock(x, y, z, 9); // replace with still water
    }
}
