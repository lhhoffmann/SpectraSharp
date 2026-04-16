namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>rl</c> (BlockEndPortalFrame) — the stronghold frame block. ID 120.
///
/// Properties:
///   - Unbreakable (hardness = -1), resistance = 6000000 (bedrock-class)
///   - Emits a dim glow (light value = 1 / 16 ≈ 2 out of 15 → via SetLightValue(0.125F))
///   - Non-opaque (13/16 tall); AABB 0–1 × 0–0.8125 × 0–1
///   - hasEye flag stored in metadata bit 2; facing in bits 0-1
///   - Drops nothing (unbreakable anyway)
///
/// Textures:
///   - Top   (face 1): index 158 (= bL - 1 = 159 - 1)
///   - Bottom (face 0): index 175 (= bL + 16)
///   - Sides (faces 2-5): index 159 (= bL, constructor texture)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ChunkProviderEnd_Spec.md §6
/// </summary>
public sealed class BlockEndPortalFrame : Block
{
    // ── Metadata helpers (spec §6.3) ──────────────────────────────────────────

    /// <summary>Returns true if bit 2 of <paramref name="meta"/> is set (Eye of Ender inserted).</summary>
    public static bool HasEye(int meta) => (meta & 4) != 0;

    // ── Construction (spec §6.1) ──────────────────────────────────────────────

    public BlockEndPortalFrame(int blockId) : base(blockId, 159, Material.RockTransp)
    {
        SetStepSound(SoundStone);
        SetLightValue(0.125f); // ≈ level 2 of 15 (spec §6.6)
        SetUnbreakable();      // hardness = -1
        SetResistance(6000000.0f);
    }

    // ── Geometry (spec §6.7) ─────────────────────────────────────────────────

    public override bool IsOpaqueCube() => false;

    public override bool RenderAsNormalBlock() => false;

    /// <summary>AABB: 0–0.8125 in Y regardless of eye state; eye crystal adds upper AABB stub.</summary>
    public override AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1.0, y + 0.8125, z + 1.0);

    public override void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z)
    {
        SetBounds(0.0f, 0.0f, 0.0f, 1.0f, 0.8125f, 1.0f);
    }

    // ── Textures (spec §6.2) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face) => face switch
    {
        1 => 158, // top
        0 => 175, // bottom
        _ => 159, // sides
    };

    // ── Drops nothing (spec §6.8) ─────────────────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 0;

    // ── Facing on placement (spec §6.9) ──────────────────────────────────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        // Facing is set externally by item placement; no logic needed here.
        base.OnBlockAdded(world, x, y, z);
    }
}
