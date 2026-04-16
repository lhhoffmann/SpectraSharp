namespace SpectraEngine.Core.Blocks;

/// <summary>
/// Replica of <c>xs</c> (BlockSlab) — handles both single slab (ID 44) and double slab (ID 43).
///
/// The same class is instantiated twice:
///   • <c>isDouble=false</c> — single slab (ID 44). AABB is bottom-half (y=0 to 0.5). Not opaque.
///   • <c>isDouble=true</c>  — double slab (ID 43). Full-cube. Fully opaque.
///
/// Metadata bits 0–2 select the material variant (stone/sandstone/wood/cobble/brick/smoothStoneBrick).
/// No top-half slab in 1.0 — single slab always occupies the bottom half (spec §6 quirk 3).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockSlab_Spec.md
/// </summary>
public sealed class BlockSlab : Block
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>cb</c> — true = double slab (ID 43); false = single slab (ID 44).</summary>
    private readonly bool _isDouble;

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="id"/> = 44 for single slab, 43 for double.
    /// </summary>
    public BlockSlab(int id, bool isDouble) : base(id, 6, Material.RockTransp)
    {
        _isDouble = isDouble;

        if (!isDouble)
        {
            // Single slab: bottom-half AABB only (spec §4 / §6 quirk 3)
            SetBounds(0f, 0f, 0f, 1f, 0.5f, 1f);
        }
        else
        {
            // Double slab: spec §4 — "m[43] = true" (opaque override)
            IsOpaqueCubeArr[id] = true;
        }

        // Both variants call h(255) — handled via IsOpaqueCubeArr in light system (spec §4/§9)
        ClearNeedsRandomTick();  // no tick behaviour (spec §7)
    }

    // ── Properties (spec §5.1–5.2) ───────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — isOpaqueCube. False for single slab, true for double.</summary>
    public override bool IsOpaqueCube() => _isDouble;

    /// <summary>obf: <c>b()</c> — renderAsNormal. False for single slab, true for double.</summary>
    public override bool RenderAsNormalBlock() => _isDouble;

    // ── Textures (spec §5.3) ──────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int meta, int face)</c> — getTextureForFaceWithMeta.
    /// Metadata bits 0–2 = variant (0=stone, 1=sandstone, 2=wood, 3=cobble, 4=brick, 5=smooth).
    /// </summary>
    public override int GetTextureForFaceAndMeta(int face, int meta)
    {
        int v = meta & 0x7;
        return face switch
        {
            0 => v <= 1 ? 6 : 5,                               // top/bottom
            1 => v == 0 ? 208 : v == 1 ? 176 : 192,           // side
            2 => 4,
            3 => 16,
            _ => v <= 1 ? 6 : 5                                // faces 4/5: fallback to top/bottom
        };
    }

    /// <summary>Inventory icon — delegates to face 0 texture. obf: <c>b(int meta)</c>.</summary>
    public override int GetTextureIndex(int face) => GetTextureForFaceAndMeta(0, 0);

    // ── Drops (spec §5.4–5.6) ────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int meta, Random, int fortune)</c> — always drops the single slab (ID 44).
    /// </summary>
    public override int IdDropped(int metadata, JavaRandom rng, int fortune) => 44;

    /// <summary>obf: <c>a(Random)</c> — double slab drops 2, single drops 1.</summary>
    public override int QuantityDropped(JavaRandom rng) => _isDouble ? 2 : 1;

    /// <summary>obf: <c>a(int meta)</c> — preserves variant metadata in the dropped item.</summary>
    public override int DamageDropped(int meta) => meta & 0x7;

    // ── No ShouldSideBeRendered override needed for now (spec §5.7 is render-only) ─
}
