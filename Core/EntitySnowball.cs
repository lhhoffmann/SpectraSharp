using SpectraEngine.Core.Mobs;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aah</c> (EntitySnowball). EntityList "Snowball", ID 11.
///
/// Impact: deal 3 damage to Blaze, 0 damage to all other entities.
/// Spawns 8 "snowballpoof" particles on impact then removes self.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §3
/// </summary>
public sealed class EntitySnowball : ThrowableBase
{
    public EntitySnowball(World world, LivingEntity owner) : base(world, owner) { }

    public EntitySnowball(World world, double x, double y, double z) : base(world, x, y, z) { }

    /// <summary>World constructor required for NBT deserialisation.</summary>
    public EntitySnowball(World world) : base(world, 0.0, 0.0, 0.0) { }

    protected override void OnImpact(MovingObjectPosition hit)
    {
        if (hit.Type == HitType.Entity && hit.Entity is Entity target)
        {
            int damage = target is EntityBlaze ? 3 : 0;
            target.AttackEntityFrom(DamageSource.Thrown(this, (Entity?)Owner ?? (Entity)this), damage);
        }

        // 8 snowballpoof particles — stub (particle system not yet implemented)
        // World?.SpawnParticle("snowballpoof", PosX, PosY, PosZ, 8)

        if (World != null && !World.IsClientSide)
            SetDead();
    }
}
