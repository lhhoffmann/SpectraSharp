namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Abstract base for fluid blocks. Replica of <c>agw</c> (BlockFluidBase).
/// Provides shared helpers used by both flowing (<see cref="BlockFluid"/>) and
/// still (<see cref="BlockStationary"/>) variants.
///
/// Key quirks (spec §15):
///   1. Flowing and still differ by exactly 1 ID: still = flowing + 1.
///   2. Lava source (meta 0) + adjacent water → obsidian; flowing (meta 1-4) → cobblestone.
///   3. isOpaqueCube = false; renderAsNormalBlock = false.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockFluid_Spec.md
/// </summary>
public abstract class BlockFluidBase : Block
{
    // IDs of thin blocks that fluid cannot flow through (spec §6.3 isBlocked list).
    // Signs, doors, trapdoor, fence gate — matches yy.{aE,aL,aD,aF,aX} from spec.
    private static readonly HashSet<int> ThinBlockIds = [63, 64, 68, 71, 96, 107];

    protected BlockFluidBase(int blockId, Material material)
        : base(blockId, material)
    {
        // Fluids are not opaque and have no collision box for entities
        LightOpacity[blockId] = 0;
    }

    // ── Block property overrides ──────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;
    public override bool IsCollidable() => false;
    public override int  GetTickDelay() => 5; // default (water); lava overrides

    // ── Flow helpers (spec §6) ────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>agw.g(world, x, y, z)</c> — return stored metadata if same fluid, else -1.
    /// </summary>
    protected int GetFluidLevel(IBlockAccess world, int x, int y, int z)
    {
        if (world.GetBlockMaterial(x, y, z) != BlockMaterial) return -1;
        return world.GetBlockMetadata(x, y, z);
    }

    /// <summary>
    /// obf: <c>agw.c(reader, x, y, z)</c> — effective level stripping the falling bit.
    /// Returns -1 if different fluid, 0 if falling (meta ≥ 8), else stored meta.
    /// </summary>
    protected int GetEffectiveLevel(IBlockAccess world, int x, int y, int z)
    {
        if (world.GetBlockMaterial(x, y, z) != BlockMaterial) return -1;
        int meta = world.GetBlockMetadata(x, y, z);
        if (meta >= 8) meta = 0;
        return meta;
    }

    /// <summary>
    /// obf: <c>agw.e(meta)</c> — level-to-height for rendering.
    /// Returns value in [1/9, 1.0]: source block fills to 1.0.
    /// </summary>
    public float LevelToHeight(int meta)
    {
        if (meta >= 8) meta = 0;
        return (meta + 1) / 9.0f;
    }

    /// <summary>
    /// obf: <c>ahx.l(world, x, y, z)</c> — true if this position blocks fluid flow.
    /// Blocks: thin blocks (signs/doors/gate), fire material, or solid material.
    /// </summary>
    protected bool IsBlocked(IWorld world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        if (ThinBlockIds.Contains(id)) return true;
        if (id == 0) return false;
        Material? mat = world.GetBlockMaterial(x, y, z);
        if (mat == null) return false;
        // Material.Portal_N (p.n = fire material) has BlocksMovement()=true,
        // so fire correctly blocks fluid flow without a special case.
        return mat.BlocksMovement();
    }

    /// <summary>
    /// obf: <c>ahx.m(world, x, y, z)</c> — true if fluid can flow into this position.
    /// Cannot flow into same-material or lava blocks, or blocked positions.
    /// </summary>
    protected bool CanFlowInto(IWorld world, int x, int y, int z)
    {
        Material? mat = world.GetBlockMaterial(x, y, z);
        if (mat == BlockMaterial) return false; // already same fluid
        if (mat == Material.Lava_) return false; // can't flow into lava
        return !IsBlocked(world, x, y, z);
    }

    // ── Lava + water interaction (spec §11) ───────────────────────────────────

    /// <summary>
    /// obf: <c>agw.j()</c> — lava adjacency check: converts lava to obsidian/cobblestone.
    /// Called from OnBlockAdded and OnNeighborBlockChange for both flowing and still lava.
    /// Spec: lava source (meta 0) + water → obsidian (49); lava flowing (meta 1-4) + water → cobble (4).
    /// </summary>
    protected void CheckLavaWaterInteraction(IWorld world, int x, int y, int z)
    {
        if (BlockMaterial != Material.Lava_) return;
        if (world.GetBlockId(x, y, z) != BlockID) return;

        bool waterNearby = false;
        foreach (var (dx, dy, dz) in new (int dx, int dy, int dz)[]
            { (0, 0, -1), (0, 0, 1), (-1, 0, 0), (1, 0, 0), (0, 1, 0) })
        {
            if (world.GetBlockMaterial(x + dx, y + dy, z + dz) == Material.Water)
            {
                waterNearby = true;
                break;
            }
        }

        if (!waterNearby) return;

        int meta = world.GetBlockMetadata(x, y, z);
        if (meta == 0)
            world.SetBlock(x, y, z, 49); // obsidian
        else if (meta <= 4)
            world.SetBlock(x, y, z, 4);  // cobblestone

        // Play "random.fizz" + 8 largesmoke particles — stub (sound/particle spec pending)
    }

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
        => CheckLavaWaterInteraction(world, x, y, z);

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
        => CheckLavaWaterInteraction(world, x, y, z);
}
