namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>kb</c> (BlockGravel) — Block ID 13. Extends BlockSand (same gravity logic).
///
/// Drop override: 1/10 chance to drop flint (item ID 318); otherwise drops gravel (ID 13).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ConcreteBlocks_Spec.md §2
/// </summary>
public sealed class BlockGravel : BlockSand
{
    // Material p.c = Material.Ground; StepSound d = Block.SoundGravel (set in BlockRegistry)
    public BlockGravel(int id) : base(id, 19, Material.Ground) { }

    // ── Drops (spec §2 — GravelBlock drops) ──────────────────────────────────

    public override int QuantityDropped(JavaRandom rng) => 1;

    public override int IdDropped(int meta, JavaRandom rng, int fortune)
        => rng.NextInt(10) == 0 ? 318 : BlockID; // 1/10 → flint (318), otherwise gravel
}
