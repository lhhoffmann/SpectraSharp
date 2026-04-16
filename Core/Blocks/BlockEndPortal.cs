using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>aid</c> (BlockEndPortal) — the activated End Portal block. ID 119.
///
/// Properties:
///   - Very thin: AABB 0–1 × 0–0.0625 × 0–1 (1/16 high)
///   - No physical collision (addCollisionBoxes returns nothing)
///   - Non-opaque, does not render as normal block
///   - Drops nothing
///   - Teleports EntityPlayer to dimension 1 on contact (server side only)
///   - Self-destructs if placed outside the overworld (dimension != 0)
///   - Has TileEntity (yg — stub, likely for particle management)
///
/// Static spawn guard prevents recursive activation during 3×3 fill.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderEnd_Spec.md §7
/// </summary>
public sealed class BlockEndPortal : Block
{
    // ── Spawn guard (spec §7.1 / §7.12) ──────────────────────────────────────

    /// <summary>obf: <c>a</c> (static) — prevents recursive self-destruction during activation fill.</summary>
    public static bool SpawnGuard; // false by default

    // ── Construction (spec §7.2) ──────────────────────────────────────────────

    public BlockEndPortal(int blockId) : base(blockId, 0, Material.Portal_A)
    {
        SetLightValue(1.0f); // full-opacity light emission (spec §7.2 — a(1.0F) sets opacity)
        SetResistance(6000000.0f);
        SetUnbreakable();
    }

    // ── Geometry (spec §7.4 / §7.6) ──────────────────────────────────────────

    public override bool IsOpaqueCube() => false;

    public override bool RenderAsNormalBlock() => false;

    /// <summary>1/16-high AABB (spec §7.4).</summary>
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1.0, y + 0.0625, z + 1.0);

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        SetBounds(0.0f, 0.0f, 0.0f, 1.0f, 0.0625f, 1.0f);
    }

    /// <summary>No physical collision — entities and projectiles pass through (spec §7.6).</summary>
    public override void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB entityBox, List<AxisAlignedBB> list) { }

    // ── Drops nothing (spec §7.8) ─────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 0;

    // ── TileEntity (spec §7.3) ────────────────────────────────────────────────

    /// <summary>End portal has a TileEntity (yg — stub, handles particles).</summary>
    public override bool HasTileEntityVirtual() => true;

    // ── Entity collision → teleport (spec §7.9) ───────────────────────────────

    /// <summary>
    /// Teleports EntityPlayer to The End (dimension 1) on contact.
    /// Only triggers server-side; vehicles/riders excluded.
    /// TravelToDimension is a stub on EntityPlayer until dimension routing is implemented.
    /// </summary>
    public override void OnEntityCollidedWithBlock(IWorld world, int x, int y, int z, Entity entity)
    {
        if (entity.Mount != null || entity.Rider != null) return;
        if (entity is not EntityPlayer player)            return;
        if (world.IsClientSide)                           return;

        player.TravelToDimension(1); // vi.c(1) — stub
    }

    // ── OnBlockAdded: self-destruct outside overworld (spec §7.12) ───────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        if (!SpawnGuard && world is World concrete && concrete.WorldProvider?.DimensionId != 0)
            world.SetBlock(x, y, z, 0); // remove self
    }
}
