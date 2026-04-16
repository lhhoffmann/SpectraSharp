namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Birch tree generator. Spec: <c>jp</c> (WorldGenForestTree).
/// Identical to <see cref="WorldGenTrees"/> except height [5,7] and log/leaves meta 2 (birch).
/// Used exclusively in Forest biome (20% of tree attempts).
/// </summary>
public sealed class WorldGenForestTree(bool silent) : WorldGenTrees(silent)
{
    protected override int LogMeta    => 2; // birch log variant
    protected override int LeavesMeta => 2; // birch leaves variant
    protected override int MinHeight  => 5; // [5, 7] vs oak [4, 6]
}
