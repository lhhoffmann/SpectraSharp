using SpectraEngine.Core.WorldGen;

namespace SpectraEngine.Core;

// ─────────────────────────────────────────────────────────────────────────────
// BlockSapling — aet, Block ID 6
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aet</c> (BlockSapling) — Block ID 6. Extends BlockGrassPlant.
///
/// Metadata encoding (spec §2.3):
///   bits 0–1 = tree type (0=Oak, 1=Spruce, 2=Birch)
///   bit  3   = growth flag (0=young, 1=ready to grow)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPlants_Spec.md §2
/// </summary>
public sealed class BlockSapling : BlockGrassPlant
{
    public BlockSapling() : base(6, 15)
    {
        SetBounds(0.5f - 0.4f, 0.0f, 0.5f - 0.4f, 0.5f + 0.4f, 0.8f, 0.5f + 0.4f);
        SetBlockName("sapling");
    }

    // ── Random tick (spec §2.5) ───────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        base.BlockTick(world, x, y, z, rng); // survival check first

        if (world.GetBlockId(x, y, z) != BlockID) return; // may have been removed by base
        if (world.IsClientSide) return;

        // Require light level >= 9 above the sapling
        if (world.GetLightBrightness(x, y + 1, z) < 9) return;

        // 1/7 chance per tick
        if (rng.NextInt(7) != 0) return;

        int meta = world.GetBlockMetadata(x, y, z);
        if ((meta & 0x8) == 0)
        {
            // Set growth-ready flag
            world.SetBlockAndMetadata(x, y, z, BlockID, meta | 0x8);
        }
        else
        {
            // Attempt to grow a tree
            GrowTree(world, x, y, z, rng);
        }
    }

    /// <summary>Bonemeal: bypass the 1/7 chance gate and grow immediately.</summary>
    public override void BonemealGrow(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        if ((meta & 0x8) == 0)
            world.SetBlockAndMetadata(x, y, z, BlockID, meta | 0x8);
        else
            GrowTree(world, x, y, z, rng);
    }

    /// <summary>Attempts tree generation at the sapling position (spec §2.6).</summary>
    private void GrowTree(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        int treeType = world.GetBlockMetadata(x, y, z) & 0x3;
        world.SetBlock(x, y, z, 0); // remove sapling

        WorldGenerator gen = treeType switch
        {
            1 => new WorldGenTaiga2(),
            2 => new WorldGenForestTree(true),
            _ => rng.NextInt(10) == 0 ? (WorldGenerator)new WorldGenBigTree() : new WorldGenTrees(true),
        };

        if (!gen.Generate(world, rng, x, y, z))
        {
            // Restore sapling if placement failed
            world.SetBlockAndMetadata(x, y, z, BlockID, treeType);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// BlockMushroom — js, Block IDs 39 (brown) and 40 (red)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>js</c> (BlockMushroom) — Block IDs 39 and 40. Extends BlockGrassPlant.
///
/// Overrides valid-soil (any opaque block) and survival (mycelium or dim light).
/// Spreads randomly at 1/25 probability per tick.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPlants_Spec.md §3
/// </summary>
public sealed class BlockMushroom : BlockGrassPlant
{
    public BlockMushroom(int id, int texture) : base(id, texture)
    {
        // Hitbox: (0.3, 0.0, 0.3, 0.7, 0.4, 0.7) — §3.2
        const float v = 0.2f;
        SetBounds(0.5f - v, 0.0f, 0.5f - v, 0.5f + v, 0.4f, 0.5f + v);
        SetBlockName("mushroom");
    }

    // ── Valid soil: any opaque block (spec §3.3) ──────────────────────────────

    protected override bool IsValidSoil(int blockId)
        => blockId > 0 && Block.IsOpaqueCubeArr[blockId];

    // ── Survival: mycelium or dim light (spec §3.4) ───────────────────────────

    protected override bool CanSurviveAt(IWorld world, int x, int y, int z)
    {
        if (y < 0 || y >= World.WorldHeight) return false;
        int below = world.GetBlockId(x, y - 1, z);
        const int myceliumId = 110;
        if (below == myceliumId) return true;
        // Any solid block + light < 13
        return Block.IsOpaqueCubeArr[below]
            && world.GetLightBrightness(x, y, z) < 13;
    }

    // ── Random tick: 1/25 spread (spec §3.5) ─────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        base.BlockTick(world, x, y, z, rng); // survival check

        if (world.GetBlockId(x, y, z) != BlockID) return;
        if (rng.NextInt(25) != 0) return;

        // Density check — count mushrooms within ±4 XZ, ±1 Y
        int count = 0;
        for (int dx = -4; dx <= 4; dx++)
        for (int dz = -4; dz <= 4; dz++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (world.GetBlockId(x + dx, y + dy, z + dz) == BlockID) count++;
        }
        if (count >= 5) return;

        // Spread: 4 random walk steps
        int tx = x, ty = y, tz = z;
        for (int step = 0; step < 4; step++)
        {
            tx += rng.NextInt(3) - 1;
            ty += rng.NextInt(2) - rng.NextInt(2);
            tz += rng.NextInt(3) - 1;
        }

        if (world.GetBlockId(tx, ty, tz) == 0 && CanSurviveAt(world, tx, ty, tz))
        {
            world.SetBlock(tx, ty, tz, BlockID);
        }
    }

    /// <summary>Bonemeal: try to grow a huge mushroom at this position.</summary>
    public override void BonemealGrow(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        world.SetBlock(x, y, z, 0); // remove small mushroom
        var gen = BlockID == 39
            ? (WorldGenerator)new WorldGenHugeMushroom(0) // brown
            : new WorldGenHugeMushroom(1);                 // red
        if (!gen.Generate(world, rng, x, y, z))
            world.SetBlock(x, y, z, BlockID); // restore if generation failed
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// BlockReed — md, Block ID 83
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>md</c> (BlockReed) — Block ID 83. Extends Block directly.
///
/// Grows up to 3 blocks tall; requires water adjacent to ground-level base.
/// Drops Sugar Cane (item ID 338).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPlants_Spec.md §4
/// </summary>
public sealed class BlockReed : Block
{
    private const int SugarCaneItemId = 338;

    public BlockReed() : base(83, 73, Material.Plants)
    {
        SetHardness(0.0f);
        SetStepSound(SoundGrass);
        SetBounds(0.5f - 0.375f, 0.0f, 0.5f - 0.375f, 0.5f + 0.375f, 1.0f, 0.5f + 0.375f);
        SetBlockName("reeds");
    }

    public override bool IsOpaqueCube()       => false;
    public override int  GetRenderType()          => 1; // cross sprite (sapling, flowers, mushrooms)
    public override bool RenderAsNormalBlock() => false;
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z) => null;

    // ── Placement validity (spec §4.4) ────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
    {
        // Valid if block directly below is also a reed
        if (world.GetBlockId(x, y - 1, z) == BlockID) return true;

        // Otherwise: soil (grass=2, dirt=3, sand=12) AND adjacent water
        int below = world.GetBlockId(x, y - 1, z);
        if (below != 2 && below != 3 && below != 12) return false;

        return HasAdjacentWater(world, x, y - 1, z);
    }

    private static bool HasAdjacentWater(IBlockAccess world, int x, int y, int z)
    {
        int[] wa = [world.GetBlockId(x + 1, y, z),
                    world.GetBlockId(x - 1, y, z),
                    world.GetBlockId(x, y, z + 1),
                    world.GetBlockId(x, y, z - 1)];
        foreach (int id in wa)
            if (id == 8 || id == 9) return true;
        return false;
    }

    // ── Neighbor update (spec §4.6) ───────────────────────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborBlockId)
    {
        if (!CanBlockStay(world, x, y, z))
        {
            DropBlockAsItem(world, x, y, z, world.GetBlockMetadata(x, y, z), 0);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── Random tick: grow up to 3 tall (spec §4.5) ───────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (y + 1 >= World.WorldHeight) return;
        if (world.GetBlockId(x, y + 1, z) != 0) return; // space above must be air

        // Count height from base
        int height = 1;
        while (world.GetBlockId(x, y - height, z) == BlockID) height++;

        if (height >= 3) return; // max height 3

        int meta = world.GetBlockMetadata(x, y, z);
        if (meta < 15)
        {
            world.SetBlockAndMetadata(x, y, z, BlockID, meta + 1);
        }
        else
        {
            world.SetBlock(x, y + 1, z, BlockID);
            world.SetBlockAndMetadata(x, y, z, BlockID, 0);
        }
    }

    // ── Drops (spec §4.7) ────────────────────────────────────────────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => SugarCaneItemId;
}

// ─────────────────────────────────────────────────────────────────────────────
// BlockNetherWart — vy, Block ID 115
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vy</c> (BlockNetherWart) — Block ID 115. Extends BlockGrassPlant.
///
/// Only grows on Soul Sand (ID 88). Growth requires Nether biome (stubbed to chance only).
/// Drops 1 (young) or 2–4 (ripe) nether wart items (ID 372).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPlants_Spec.md §5
/// </summary>
public sealed class BlockNetherWart : BlockGrassPlant
{
    private const int NetherWartItemId = 372;
    private const int SoulSandId       = 88;

    // Base texture index for nether wart (texture 0 = offset 0; actual texture resolved by atlas)
    private readonly int _baseTexture;

    public BlockNetherWart(int textureBase = 0) : base(115, textureBase)
    {
        _baseTexture = textureBase;
        // Hitbox: flat cluster (0.0, 0.0, 0.0, 1.0, 0.25, 1.0)
        SetBounds(0.0f, 0.0f, 0.0f, 1.0f, 0.25f, 1.0f);
        SetBlockName("netherStalk");
    }

    // ── Valid soil: soul sand only (spec §5.3) ────────────────────────────────

    protected override bool IsValidSoil(int blockId) => blockId == SoulSandId;

    // ── Random tick (spec §5.5) ───────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        int meta = world.GetBlockMetadata(x, y, z);
        if (meta < 3 && rng.NextInt(15) == 0)
        {
            world.SetBlockAndMetadata(x, y, z, BlockID, meta + 1);
        }
        base.BlockTick(world, x, y, z, rng); // survival check
    }

    // ── Drops (spec §5.6) ────────────────────────────────────────────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune) => NetherWartItemId;

    public override void DropBlockAsItemWithChance(
        IWorld world, int x, int y, int z, int meta, float dropChance, int fortune)
    {
        if (world.IsClientSide) return;
        int qty = meta >= 3 ? 2 + world.Random.NextInt(3) + world.Random.NextInt(fortune + 1) : 1;
        for (int i = 0; i < qty; i++)
        {
            if (world.Random.NextFloat() > dropChance) continue;
            SpawnDropItem(world, x, y, z, NetherWartItemId);
        }
    }

    private static void SpawnDropItem(IWorld world, int x, int y, int z, int itemId)
    {
        if (world is not World concreteWorld) return;
        double ox = x + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        double oy = y + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        double oz = z + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        var item = new EntityItem(concreteWorld, ox, oy, oz, new ItemStack(itemId, 1, 0));
        item.PickupDelay = 10;
        concreteWorld.SpawnEntity(item);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// BlockStem — pu, Block IDs 104 (pumpkin) and 105 (melon)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>pu</c> (BlockStem) — Block IDs 104 and 105. Extends BlockGrassPlant.
///
/// Grows on farmland only. At stage 7 attempts to place produce (pumpkin/melon) in an
/// adjacent position. Drops seeds proportional to growth stage.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPlants_Spec.md §6
/// </summary>
public sealed class BlockStem : BlockGrassPlant
{
    private readonly int _produceBlockId;
    private readonly int _seedItemId;

    // Pumpkin stem: produce=86 (pumpkin), seed=361 (pumpkin seeds)
    // Melon stem:   produce=103 (melon),  seed=362 (melon seeds)
    public BlockStem(int id, int produceBlockId, int seedItemId) : base(id, 111)
    {
        _produceBlockId = produceBlockId;
        _seedItemId     = seedItemId;
        SetBlockName(id == 104 ? "pumpkinStem" : "melonStem");
    }

    // ── Valid soil: farmland only (spec §6.3) ─────────────────────────────────

    protected override bool IsValidSoil(int blockId) => blockId == 60;

    // ── Random tick (spec §6.5) ───────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        base.BlockTick(world, x, y, z, rng); // survival check
        if (world.GetBlockId(x, y, z) != BlockID) return;
        if (world.IsClientSide) return;

        if (world.GetLightBrightness(x, y + 1, z) < 9) return;

        int meta      = world.GetBlockMetadata(x, y, z);
        float fertility = ComputeFertility(world, x, y, z);
        int chance    = (int)(25.0f / fertility) + 1;

        if (rng.NextInt(chance) != 0) return;

        if (meta < 7)
        {
            world.SetBlockAndMetadata(x, y, z, BlockID, meta + 1);
        }
        else
        {
            // Attempt to place produce block in a random adjacent direction
            TryProduceCrop(world, x, y, z, rng);
        }
    }

    /// <summary>Bonemeal: force stem to stage 7 and try to place produce.</summary>
    public override void BonemealGrow(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        world.SetBlockAndMetadata(x, y, z, BlockID, 7);
        TryProduceCrop(world, x, y, z, rng);
    }

    private void TryProduceCrop(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // Check if produce already adjacent
        int[] dx = [1, -1, 0, 0];
        int[] dz = [0, 0, 1, -1];
        for (int d = 0; d < 4; d++)
            if (world.GetBlockId(x + dx[d], y, z + dz[d]) == _produceBlockId) return;

        int dir = rng.NextInt(4);
        int tx = x + dx[dir], tz = z + dz[dir];
        // Place if air above farmland
        if (world.GetBlockId(tx, y, tz) == 0 && world.GetBlockId(tx, y - 1, tz) == 60)
        {
            world.SetBlock(tx, y, tz, _produceBlockId);
        }
    }

    // ── Fertility (spec §6.6) ─────────────────────────────────────────────────

    private static float ComputeFertility(IBlockAccess world, int x, int y, int z)
    {
        float fertility = 1.0f;
        for (int dx = -1; dx <= 1; dx++)
        for (int dz = -1; dz <= 1; dz++)
        {
            int id = world.GetBlockId(x + dx, y - 1, z + dz);
            if (id != 60) continue;
            int meta = world.GetBlockMetadata(x + dx, y - 1, z + dz);
            float val = meta > 0 ? 3.0f : 1.0f;
            if (dx != 0 && dz != 0) val /= 4.0f; // diagonal
            fertility += val;
        }
        return fertility;
    }

    // ── Drops (spec §6.7) ────────────────────────────────────────────────────

    public override int IdDropped(int meta, JavaRandom rng, int fortune) => _seedItemId;

    public override void DropBlockAsItemWithChance(
        IWorld world, int x, int y, int z, int meta, float dropChance, int fortune)
    {
        if (world.IsClientSide) return;
        for (int i = 0; i < 3 && world.Random.NextInt(15) <= meta; i++)
        {
            if (world.Random.NextFloat() <= dropChance)
                SpawnSeedDrop(world, x, y, z);
        }
    }

    private void SpawnSeedDrop(IWorld world, int x, int y, int z)
    {
        if (world is not World concreteWorld) return;
        double ox = x + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        double oy = y + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        double oz = z + 0.5 + (concreteWorld.Random.NextDouble() - 0.5) * 0.7;
        var item = new EntityItem(concreteWorld, ox, oy, oz, new ItemStack(_seedItemId, 1, 0));
        item.PickupDelay = 10;
        concreteWorld.SpawnEntity(item);
    }

    // ── Stem color (spec §6.8) ────────────────────────────────────────────────

    /// <summary>
    /// Returns the packed RGB color for this stem at the given growth stage.
    /// Stage 0 (young) = green; stage 7 (mature) = yellow-green.
    /// </summary>
    public static int GetStemColor(int stage)
    {
        int r = stage * 32;
        int g = 255 - stage * 8;
        int b = stage * 4;
        return (r << 16) | (g << 8) | b;
    }
}
