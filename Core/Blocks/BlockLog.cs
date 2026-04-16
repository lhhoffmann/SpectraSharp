namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aip</c> (BlockLog) — Block ID 17.
///
/// Multi-face textures driven by face and metadata:
///   Top/bottom (faces 0/1): index 21 (log end — circular cross-section).
///   Sides: oak=20, spruce=116, birch=117, jungle=20 (same as oak in 1.0).
///   Wood type from meta bits 0–1 (meta &amp; 3).
///
/// Has tickable flag (l() called in registration) but no override of random tick — no-op.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ConcreteBlocks_Spec.md §4
/// </summary>
public sealed class BlockLog : Block
{
    // Material p.d = Material.Plants; StepSound c = Block.SoundWood (set in BlockRegistry)
    public BlockLog(int id) : base(id, Material.Plants) { }

    // ── Texture (spec §4 — Multi-Face Textures) ───────────────────────────────

    public override int GetTextureIndex(int face) => face switch
    {
        0 => 21, // bottom: log end
        1 => 21, // top: log end
        _ => 20  // sides: oak bark (default)
    };

    public override int GetTextureForFaceAndMeta(int face, int meta)
    {
        // Faces 0/1 (top/bottom): always log end (index 21)
        if (face == 0 || face == 1) return 21;

        // Side faces: bark texture by wood type (meta & 3)
        return (meta & 3) switch
        {
            1 => 116, // spruce bark (dark)
            2 => 117, // birch bark (white)
            _ => 20   // oak / jungle bark (both use 20 in 1.0)
        };
    }
}
