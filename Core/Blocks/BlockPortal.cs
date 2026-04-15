namespace SpectraSharp.Core.Blocks;

/// <summary>
/// Replica of <c>sc</c> (BlockPortal) — animated Nether Portal block. ID 90.
///
/// Properties:
///   - No physical collision (GetCollisionBoundingBox returns null)
///   - Non-opaque; thin visual AABB (0.25 wide in one axis)
///   - Emits light level 1
///   - Drops nothing
///   - Entity contact → calls player.InPortal() each tick
///   - Neighbor change → validates obsidian frame; self-destructs if invalid
///
/// TryToCreatePortal (g): called by BlockFire.OnBlockAdded when fire appears inside
/// an obsidian frame. Validates 2×3 interior + 10 obsidian walls, places portal blocks.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPortal_Spec.md §3
/// </summary>
public sealed class BlockPortal : Block
{
    private const int ObsidianId = 49;
    private const int FireId     = 51;

    public BlockPortal(int blockId) : base(blockId, 14, Material.Portal_A)
    {
        SetLightValue(1.0f / 15.0f); // light level = 1 of 15
    }

    // ── Geometry (spec §3.2 / §3.3) ──────────────────────────────────────────

    public override bool IsOpaqueCube() => false;

    public override bool RenderAsNormalBlock() => false;

    /// <summary>No physical collision — entities pass through (spec §3.2).</summary>
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => null;

    /// <summary>
    /// Visual AABB — 0.25 wide in the portal's perpendicular axis (spec §3.3).
    /// Orientation determined from neighbors.
    /// </summary>
    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        bool hasNeighborX = world.GetBlockId(x - 1, y, z) == BlockID
                         || world.GetBlockId(x + 1, y, z) == BlockID;
        if (hasNeighborX)
            SetBounds(0.0f, 0.0f, 0.375f, 1.0f, 1.0f, 0.625f); // X-facing: thin in Z
        else
            SetBounds(0.375f, 0.0f, 0.0f, 0.625f, 1.0f, 1.0f); // Z-facing: thin in X
    }

    // ── Drops nothing (spec §3.8) ─────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 0;

    // ── Entity collision → inPortal (spec §3.10) ─────────────────────────────

    /// <summary>
    /// Called each tick while an entity overlaps this block.
    /// Calls InPortal() on the player, which manages the teleport cooldown.
    /// </summary>
    public override void OnEntityCollidedWithBlock(IWorld world, int x, int y, int z, Entity entity)
    {
        if (entity.Mount != null || entity.Rider != null) return;
        if (entity is EntityPlayer player)
            player.InPortal();
    }

    // ── Neighbor change: validate frame (spec §3.6) ───────────────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighborId)
    {
        // Determine orientation from neighbors
        bool hasX = world.GetBlockId(x - 1, y, z) == BlockID
                 || world.GetBlockId(x + 1, y, z) == BlockID;
        bool hasZ = world.GetBlockId(x, y, z - 1) == BlockID
                 || world.GetBlockId(x, y, z + 1) == BlockID;

        // var6/var7: axis along which the portal runs (perpendicular to frame walls)
        int var6 = hasX ? 1 : 0; // Z-walk when X-neighbors, X-walk otherwise... inverted
        int var7 = hasX ? 0 : 1;

        // Walk to bottom of portal column
        int baseY = y;
        while (baseY > 0 && world.GetBlockId(x, baseY - 1, z) == BlockID)
            baseY--;

        // Must have obsidian below
        if (world.GetBlockId(x, baseY - 1, z) != ObsidianId)
        {
            world.SetBlock(x, y, z, 0);
            return;
        }

        // Count portal column height (must be exactly 3)
        int height = 0;
        while (height < 4 && world.GetBlockId(x, baseY + height, z) == BlockID)
            height++;

        if (height == 3 && world.GetBlockId(x, baseY + 3, z) == ObsidianId)
        {
            // Check both perpendicular portal neighbors (cross-axis contamination check)
            bool pX = world.GetBlockId(x - 1, y, z) == BlockID || world.GetBlockId(x + 1, y, z) == BlockID;
            bool pZ = world.GetBlockId(x, y, z - 1) == BlockID || world.GetBlockId(x, y, z + 1) == BlockID;
            if (pX && pZ)
            {
                world.SetBlock(x, y, z, 0); // invalid crossing
                return;
            }

            // Check that obsidian walls exist on both sides along portal axis
            bool wallOk = (world.GetBlockId(x - var7, y, z - var6) == ObsidianId
                        || world.GetBlockId(x + var7, y, z + var6) == ObsidianId);
            if (!wallOk)
            {
                world.SetBlock(x, y, z, 0);
                return;
            }
        }
        else
        {
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── TryToCreatePortal / g(world, x, y, z) (spec §3.5) ───────────────────

    /// <summary>
    /// Called by <see cref="BlockFire"/> when fire is placed inside an obsidian frame.
    /// Validates the 2×3 frame interior and 10-block obsidian walls; places portal blocks.
    /// Returns true if a portal was created.
    /// </summary>
    public static bool TryToCreatePortal(IWorld world, int x, int y, int z)
    {
        // Step 1: determine orientation (spec §3.5)
        bool obsX = world.GetBlockId(x - 1, y, z) == ObsidianId
                 || world.GetBlockId(x + 1, y, z) == ObsidianId;
        bool obsZ = world.GetBlockId(x, y, z - 1) == ObsidianId
                 || world.GetBlockId(x, y, z + 1) == ObsidianId;

        if (obsX == obsZ) return false; // both or neither → not a 1-axis frame

        // var5/var6: portal is along Z axis when obsX; along X axis when obsZ
        int var5 = obsX ? 1 : 0; // step along portal-width axis
        int var6 = obsX ? 0 : 1;

        // Step 2: find base corner — shift if one-axis neighbor is air (spec §3.5 Step 2)
        if (world.GetBlockId(x - var5, y, z - var6) == 0)
        {
            x -= var5;
            z -= var6;
        }

        // Step 3: validate 4-wide × 5-tall frame (var7 = width, var8 = height)
        for (int var7 = -1; var7 <= 2; var7++)
        for (int var8 = -1; var8 <= 3; var8++)
        {
            bool isCorner = (var7 == -1 || var7 == 2) && (var8 == -1 || var8 == 3);
            if (isCorner) continue; // corners unchecked

            bool isEdge = (var7 == -1 || var7 == 2 || var8 == -1 || var8 == 3);
            int blockId = world.GetBlockId(x + var5 * var7, y + var8, z + var6 * var7);

            if (isEdge)
            {
                if (blockId != ObsidianId) return false; // wall must be obsidian
            }
            else
            {
                if (blockId != 0 && blockId != FireId) return false; // interior must be air/fire
            }
        }

        // Step 4: place portal blocks (2 wide × 3 tall interior) (spec §3.5 Step 4)
        bool oldSuppress = (world is World w) ? w.SuppressUpdates : false;
        if (world is World worldConcrete) worldConcrete.SuppressUpdates = true;

        for (int var11 = 0; var11 <= 1; var11++)
        for (int var12 = 0; var12 <= 2; var12++)
            world.SetBlock(x + var5 * var11, y + var12, z + var6 * var11, 90); // portal ID 90

        if (world is World worldConcrete2) worldConcrete2.SuppressUpdates = oldSuppress;

        return true;
    }
}
