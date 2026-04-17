namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>ahh</c> (BlockStairs) — L-shaped stair block.
///
/// Stores a reference to a parent block and delegates textures, hardness, sound, and drops to it.
/// Provides two collision AABBs per orientation to form the step geometry.
/// Metadata bits 0–1 encode the ascent direction (0=east, 1=west, 2=south, 3=north).
///
/// Stair block IDs: 53 (wood), 67 (cobblestone), 108 (brick), 109 (stone brick), 114 (nether brick).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockStairs_Spec.md
/// </summary>
public class BlockStairs : Block
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — parent block whose properties this stair inherits.</summary>
    protected readonly Block ParentBlock;

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    public BlockStairs(int id, Block parentBlock)
        : base(id, parentBlock.BlockIndexInTexture, parentBlock.BlockMaterial ?? Material.RockTransp)
    {
        ParentBlock = parentBlock;

        // Copy parent properties (spec §3.3–3.6)
        BlockHardness   = parentBlock.BlockHardness;
        BlockResistance = parentBlock.BlockResistance / 3.0f; // spec §3.4 + quirk 3
        StepSoundGroup  = parentBlock.StepSoundGroup;

        // h(255) — stair light: handled via IsOpaqueCube()=false in neighbor-max light
        SetLightOpacity(10); // c() returns 10 (spec §4.3)
    }

    // ── Properties (spec §4.1–4.3) ───────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — isOpaqueCube. Always false for stairs.</summary>
    public override bool IsOpaqueCube() => false;
    public override int  GetRenderType()  => 10;

    /// <summary>obf: <c>b()</c> — renderAsNormal. Always false for stairs.</summary>
    public override bool RenderAsNormalBlock() => false;

    // ── Selection box (spec §4.4) — always full cube ──────────────────────────

    /// <summary>
    /// obf: <c>b(kq, x,y,z)</c> — selection highlight. Always a full cube (spec quirk 1).
    /// </summary>
    public override AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(x, y, z, x + 1.0, y + 1.0, z + 1.0);

    // ── Multi-AABB collision (spec §4.5) ──────────────────────────────────────

    /// <summary>
    /// obf: <c>a(ry,x,y,z,c,ArrayList)</c> — adds two half-block AABBs to form the L-shape.
    /// Orientation read from world metadata.
    /// </summary>
    public override void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB entityBox, System.Collections.Generic.List<AxisAlignedBB> list)
    {
        int meta = world.GetBlockMetadata(x, y, z) & 3;

        double xd = x, yd = y, zd = z;

        AxisAlignedBB boxA, boxB;
        switch (meta)
        {
            case 0: // ascending east — low step on west half, full height on east half
                boxA = AxisAlignedBB.GetFromPool(xd,       yd, zd,       xd+0.5, yd+0.5, zd+1.0);
                boxB = AxisAlignedBB.GetFromPool(xd+0.5,   yd, zd,       xd+1.0, yd+1.0, zd+1.0);
                break;
            case 1: // ascending west — full height on west half, low step on east half
                boxA = AxisAlignedBB.GetFromPool(xd,       yd, zd,       xd+0.5, yd+1.0, zd+1.0);
                boxB = AxisAlignedBB.GetFromPool(xd+0.5,   yd, zd,       xd+1.0, yd+0.5, zd+1.0);
                break;
            case 2: // ascending south — low step on north half, full height on south half
                boxA = AxisAlignedBB.GetFromPool(xd,       yd, zd,       xd+1.0, yd+0.5, zd+0.5);
                boxB = AxisAlignedBB.GetFromPool(xd,       yd, zd+0.5,   xd+1.0, yd+1.0, zd+1.0);
                break;
            default: // 3 — ascending north — full height on north half, low step on south half
                boxA = AxisAlignedBB.GetFromPool(xd,       yd, zd,       xd+1.0, yd+1.0, zd+0.5);
                boxB = AxisAlignedBB.GetFromPool(xd,       yd, zd+0.5,   xd+1.0, yd+0.5, zd+1.0);
                break;
        }

        if (entityBox.Intersects(boxA)) list.Add(boxA);
        if (entityBox.Intersects(boxB)) list.Add(boxB);

        // Reset shared AABB state to full cube after use (spec §4.5 / quirk 4)
        SetBounds(0f, 0f, 0f, 1f, 1f, 1f);
    }

    // ── Textures (spec §4.10) — delegate to parent (always face 0) ───────────

    /// <summary>
    /// obf: <c>a(int meta, int face)</c> — always delegates to parent block's face-0 texture.
    /// Spec quirk 2: face argument is discarded.
    /// </summary>
    public override int GetTextureForFaceAndMeta(int face, int meta)
        => ParentBlock.GetTextureForFaceAndMeta(0, meta);

    public override int GetTextureIndex(int face)
        => ParentBlock.GetTextureIndex(0);

    // ── Drops — delegate to parent ────────────────────────────────────────────

    public override int IdDropped(int metadata, JavaRandom rng, int fortune)
        => ParentBlock.IdDropped(metadata, rng, fortune);

    public override int QuantityDropped(JavaRandom rng)
        => ParentBlock.QuantityDropped(rng);
}
