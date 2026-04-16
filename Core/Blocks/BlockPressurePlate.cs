namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>wx</c> (BlockPressurePlate) — Block IDs 70 (stone) and 72 (wood).
///
/// Meta: 0 = unpressed, 1 = pressed.
/// Sensor type: stone (ID 70) detects living mobs; wood (ID 72) detects all entities.
///
/// Quirks preserved (spec §12):
///   6. Can be placed on redstone wire (ID 55) — special floor check.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockRedstone_Spec.md §8
/// </summary>
public sealed class BlockPressurePlate : Block
{
    // ── Sensor type (spec §8.2) ───────────────────────────────────────────────

    /// <summary>True = all entities (wood plate, ID 72); false = living mobs only (stone plate, ID 70).</summary>
    private readonly bool _detectAllEntities;

    // ── Construction (spec §8.1) ──────────────────────────────────────────────

    public BlockPressurePlate(int id, int textureIndex, Material material, bool detectAllEntities)
        : base(id, textureIndex, material)
    {
        _detectAllEntities = detectAllEntities;
        SetHardness(0.5f);
        SetBlockName("pressurePlate");
    }

    public override bool IsOpaqueCube() => false;
    public override bool RenderAsNormalBlock() => false;
    public override bool CanProvidePower() => true;
    public override int GetMobilityFlag() => 1;
    public override int GetTickDelay() => 20;

    // ── Bounds (spec §8.9) ────────────────────────────────────────────────────

    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
    {
        bool pressed = world.GetBlockMetadata(x, y, z) == 1;
        float height = pressed ? 1.0f / 32f : 1.0f / 16f;
        return AxisAlignedBB.GetFromPool(x + 1.0f/16f, y, z + 1.0f/16f, x + 15.0f/16f, y + height, z + 15.0f/16f);
    }

    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x + 1.0f/16f, y, z + 1.0f/16f, x + 15.0f/16f, y + 0.5f, z + 15.0f/16f);

    // ── canBlockStay (spec §8.4) ──────────────────────────────────────────────

    public override bool CanBlockStay(IWorld world, int x, int y, int z)
    {
        int below = world.GetBlockId(x, y - 1, z);
        return Block.IsOpaqueCubeArr[below & 0xFF] || below == 55; // solid or redstone wire (spec quirk §12.6)
    }

    // ── Sensor tick (spec §8.6) ───────────────────────────────────────────────

    private void SensorTick(World world, int x, int y, int z)
    {
        bool wasPressed = world.GetBlockMetadata(x, y, z) == 1;

        // Detection AABB (spec §8.6: ±0.125 inset)
        var bbox = AxisAlignedBB.GetFromPool(x + 0.125f, y, z + 0.125f, x + 0.875f, y + 0.25f, z + 0.875f);
        bool isPressed;

        if (_detectAllEntities)
            isPressed = world.GetEntitiesWithinAABB<Entity>(bbox).Count > 0;
        else
            isPressed = world.GetEntitiesWithinAABB<LivingEntity>(bbox).Count > 0;

        if (isPressed && !wasPressed)
        {
            world.SetMetadataQuiet(x, y, z, 1);
            world.NotifyBlock(x, y, z, BlockID);
            world.NotifyBlock(x, y - 1, z, BlockID);
            world.NotifyNeighbors(x, y, z, BlockID);
            world.PlayAuxSFX(null, 1003, x, y, z, 1); // click on
        }
        else if (!isPressed && wasPressed)
        {
            world.SetMetadataQuiet(x, y, z, 0);
            world.NotifyBlock(x, y, z, BlockID);
            world.NotifyBlock(x, y - 1, z, BlockID);
            world.NotifyNeighbors(x, y, z, BlockID);
            world.PlayAuxSFX(null, 1003, x, y, z, 0); // click off
        }

        if (isPressed)
            world.ScheduleBlockUpdate(x, y, z, BlockID, GetTickDelay()); // reschedule
    }

    // ── Trigger hooks (spec §8.7) ─────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        // randomTick: if pressed, check if should release
        if (world.GetBlockMetadata(x, y, z) != 0 && world is World w)
            SensorTick(w, x, y, z);
    }

    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (world is World w) SensorTick(w, x, y, z);
    }

    public override void OnEntityWalking(IWorld world, int x, int y, int z, Entity entity)
    {
        // onEntityWalk: if not pressed, trigger sensor check
        if (world.GetBlockMetadata(x, y, z) != 1 && world is World w)
            SensorTick(w, x, y, z);
    }

    // ── Power output (spec §8.8) ──────────────────────────────────────────────

    public override bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face)
        => world.GetBlockMetadata(x, y, z) > 0;

    public override bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face)
        => world.GetBlockMetadata(x, y, z) != 0 && face == 1; // strong power upward only
}
