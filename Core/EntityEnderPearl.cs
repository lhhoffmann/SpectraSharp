namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>tm</c> (EntityEnderPearl). EntityList "ThrownEnderpearl", ID 14.
///
/// Impact: teleports owner to impact position, resets fall distance, deals 5 fall damage.
/// Spawns 32 "portal" particles then removes self.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §5
/// </summary>
public sealed class EntityEnderPearl : ThrowableBase
{
    public EntityEnderPearl(World world, LivingEntity owner) : base(world, owner) { }

    public EntityEnderPearl(World world, double x, double y, double z) : base(world, x, y, z) { }

    /// <summary>World constructor required for NBT deserialisation.</summary>
    public EntityEnderPearl(World world) : base(world, 0.0, 0.0, 0.0) { }

    protected override void OnImpact(MovingObjectPosition hit)
    {
        if (hit.Type == HitType.Entity && hit.Entity is Entity target)
            target.AttackEntityFrom(DamageSource.Thrown(this, (Entity?)Owner ?? (Entity)this), 0);

        // 32 portal particles — stub (particle system not yet implemented)
        // World?.SpawnParticle("portal", PosX, PosY, PosZ, 32)

        if (World != null && !World.IsClientSide)
        {
            if (Owner != null)
            {
                Owner.SetPosition(PosX, PosY, PosZ);
                Owner.FallDistance = 0.0f;
                Owner.AttackEntityFrom(DamageSource.Fall, 5);
            }
            SetDead();
        }
    }
}
