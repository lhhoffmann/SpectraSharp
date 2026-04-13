namespace SpectraSharp.Core;

/// <summary>
/// Placeholder interface for <c>ry</c> (World).
///
/// The mutable world — extends IBlockAccess with block mutation, entity spawning,
/// tick scheduling, and client/server discrimination. Full spec pending.
/// See REQUESTS.md: World (ry).
///
/// Known contracts (from Block_Spec.md §7):
///   I    → bool : isClientSide — true on client; block drops are server-only
///   w    → JavaRandom : world random
///   a(entity) → void : spawnEntityInWorld
///   a(x,y,z) → int  : getBlockId (inherited from IBlockAccess)
/// </summary>
public interface IWorld : IBlockAccess
{
    bool IsClientSide { get; }
    JavaRandom Random { get; }
    void SpawnEntity(object entity);
}
