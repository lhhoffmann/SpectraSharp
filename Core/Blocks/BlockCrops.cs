namespace SpectraSharp.Core.Blocks;

/// <summary>
/// Replica of <c>aha</c> (BlockCrops) — wheat growth, ID 59.
/// Extends <c>wg</c> (BlockFlower) behaviour which is implemented inline.
///
/// Growth stage 0–7 in metadata. Random-tick: stability check, then light ≥ 9 at y+1,
/// then probability roll based on surrounding moist farmland. Stage 7 = harvestable.
/// Drops: wheat (ID 296) at stage 7; seeds (ID 295) probabilistically via DropBlockAsItemWithChance.
/// No collision (returns null from GetCollisionBoundingBoxFromPool).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockCrops_Spec.md §A
/// </summary>
public class BlockCrops : Block
{
    // Item IDs (spec §A.5 a(int, Random, int) / harvestBlock)
    private const int WheatItemId = 296;  // acy.S.bM
    private const int SeedsItemId = 295;  // acy.R.bM

    // ── Constructor (spec §A.3) ───────────────────────────────────────────────

    public BlockCrops(int id, int textureIndex)
        : base(id, textureIndex, Material.Plants)
    {
        // b(true) is a no-op since RenderSpecial[id] is already true by default;
        // included for spec parity.
        SetBounds(0f, 0f, 0f, 1f, 0.25f, 1f); // full XZ, 0.25 tall (spec §A.3 step 4)
        SetLightOpacity(6); // spec §A.5 c()
    }

    // ── Properties ───────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;

    /// <summary>
    /// No collision AABB — entities walk through crops.
    /// Spec: <c>wg.b(ry)</c> returns null.
    /// </summary>
    public override AxisAlignedBB GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => null!; // null = no collision; caller must handle null (spec §A.8 quirk 4)

    // ── Survival / canBlockStay (spec §A wg.c / wg.d) ────────────────────────

    /// <summary>
    /// obf: <c>d(int blockId)</c> — crops survive only on farmland (ID 60).
    /// Overrides <c>wg.d()</c> which also allows grass/dirt.
    /// </summary>
    private bool CanBlockSurviveOn(int blockId) => blockId == 60; // farmland only

    /// <summary>
    /// obf: <c>c(ry, x, y, z)</c> — canBlockStay: requires farmland directly below.
    /// Crops are removed by <see cref="CheckAndDropBlock"/> if the survival check fails.
    /// </summary>
    public override bool CanBlockStay(IWorld world, int x, int y, int z)
        => CanBlockSurviveOn(world.GetBlockId(x, y - 1, z));

    // ── Stability check (spec §A wg.h) ───────────────────────────────────────

    /// <summary>
    /// obf: <c>wg.h(ry, x, y, z)</c> — removes crop and drops item if survival check fails.
    /// Called at the start of every random tick (replaces wg's <c>a(ry,...)</c> random tick).
    /// </summary>
    private void CheckAndDropBlock(IWorld world, int x, int y, int z)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            world.SetBlock(x, y, z, 0);
            if (!world.IsClientSide)
                DropBlockAsItem(world, x, y, z, world.GetBlockMetadata(x, y, z), 0);
        }
    }

    // ── Random tick (spec §A.5 a(ry, x, y, z, Random)) ──────────────────────

    /// <summary>
    /// obf: <c>a(ry, x, y, z, Random)</c> — random tick: stability check + growth.
    /// </summary>
    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        CheckAndDropBlock(world, x, y, z);

        if (world.GetLightBrightness(x, y + 1, z) >= 9)
        {
            int stage = world.GetBlockMetadata(x, y, z);
            if (stage < 7)
            {
                float factor = ComputeGrowthFactor(world, x, y, z);
                if (rng.NextInt((int)(25.0f / factor) + 1) == 0)
                    world.SetMetadata(x, y, z, stage + 1);
            }
        }
    }

    // ── Growth factor (spec §A.5 j(ry, x, y, z)) ─────────────────────────────

    /// <summary>
    /// obf: <c>j(ry, x, y, z)</c> — farmland-quality score; higher = faster growth.
    /// Base 1.0; centre farmland +1.0 dry / +3.0 moist; each of 8 neighbours +0.25 dry / +0.75 moist.
    /// Halved if crowded (diagonal or both axes occupied by same crop).
    /// </summary>
    private float ComputeGrowthFactor(IWorld world, int x, int y, int z)
    {
        // Step 1 — survey adjacent crop presence
        bool westCrop  = world.GetBlockId(x - 1, y, z) == BlockID;
        bool eastCrop  = world.GetBlockId(x + 1, y, z) == BlockID;
        bool southCrop = world.GetBlockId(x, y, z - 1) == BlockID;
        bool northCrop = world.GetBlockId(x, y, z + 1) == BlockID;

        bool axisX = westCrop  || eastCrop;
        bool axisZ = southCrop || northCrop;
        bool diag  = world.GetBlockId(x - 1, y, z - 1) == BlockID
                  || world.GetBlockId(x - 1, y, z + 1) == BlockID
                  || world.GetBlockId(x + 1, y, z - 1) == BlockID
                  || world.GetBlockId(x + 1, y, z + 1) == BlockID;

        // Step 2 — accumulate farmland score
        float score = 1.0f;
        for (int bx = x - 1; bx <= x + 1; bx++)
        for (int bz = z - 1; bz <= z + 1; bz++)
        {
            int belowId = world.GetBlockId(bx, y - 1, bz);
            float contribution = 0.0f;
            if (belowId == 60) // farmland
            {
                contribution = 1.0f;
                if (world.GetBlockMetadata(bx, y - 1, bz) > 0) // moist
                    contribution = 3.0f;
            }
            if (bx != x || bz != z) // neighbouring tile (not centre)
                contribution /= 4.0f;
            score += contribution;
        }

        // Step 3 — crowding penalty
        if (diag || (axisX && axisZ))
            score /= 2.0f;

        return score;
    }

    // ── Instant grow / bonemeal (spec §A.5 g()) ──────────────────────────────

    /// <summary>obf: <c>g(ry, x, y, z)</c> — bonemeal: jump to stage 7.</summary>
    public void InstantGrow(IWorld world, int x, int y, int z)
        => world.SetMetadata(x, y, z, 7);

    // ── Textures (spec §A.5 a(int face, int meta)) ───────────────────────────

    /// <summary>
    /// obf: <c>a(int meta, int face)</c> — texture = base + stage.
    /// Negative meta (inventory render) uses stage 7 texture.
    /// </summary>
    public override int GetTextureForFaceAndMeta(int face, int meta)
    {
        if (meta < 0) meta = 7;
        return BlockIndexInTexture + meta;
    }

    // ── Drops (spec §A.5 a(int meta, Random, int fortune) / harvestBlock) ─────

    /// <summary>
    /// obf: <c>a(int meta, Random, int fortune)</c> — wheat (296) at stage 7, nothing otherwise.
    /// Seeds are always dropped via <see cref="DropBlockAsItemWithChance"/>.
    /// </summary>
    public override int IdDropped(int metadata, JavaRandom rng, int fortune)
        => metadata == 7 ? WheatItemId : -1;

    public override int QuantityDropped(JavaRandom rng) => 1;

    /// <summary>
    /// Overrides drop: calls base to drop wheat (if stage 7), then spawns seeds.
    /// Spec: <c>harvestBlock</c> §A.5 — 3+fortune attempts, chance = (meta+1)/16 per attempt.
    /// Fortune is passed as 0 to base (spec quirk 2).
    /// </summary>
    public override void DropBlockAsItemWithChance(
        IWorld world, int x, int y, int z, int meta, float chance, int fortune)
    {
        base.DropBlockAsItemWithChance(world, x, y, z, meta, chance, 0); // fortune=0 for wheat (spec quirk 2)

        if (world.IsClientSide) return;

        // Seeds: (3 + fortune) attempts, each succeeds with probability (meta+1)/16
        int attempts = 3 + fortune;
        for (int i = 0; i < attempts; i++)
        {
            if (world.Random.NextInt(15) <= meta) // spec quirk 3: <= meta (not < meta)
                SpawnAsEntity(world, x, y, z, new ItemStack(SeedsItemId, 1, 0));
        }
    }
}
