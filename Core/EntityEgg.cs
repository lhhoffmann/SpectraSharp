using SpectraEngine.Core.Mobs;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>qw</c> (EntityEgg).
/// NOT in EntityList — no NBT persistence (same as EntityFishHook).
///
/// Impact: 1/8 chance to spawn 1 baby chicken; 1/256 chance (1/8 × 1/32) to spawn 4 babies.
/// Spawns 8 "snowballpoof" particles then removes self.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §4
/// </summary>
public sealed class EntityEgg : ThrowableBase
{
    public EntityEgg(World world, LivingEntity owner) : base(world, owner) { }

    public EntityEgg(World world, double x, double y, double z) : base(world, x, y, z) { }

    protected override void OnImpact(MovingObjectPosition hit)
    {
        if (hit.Type == HitType.Entity && hit.Entity is Entity target)
            target.AttackEntityFrom(DamageSource.Thrown(this, (Entity?)Owner ?? (Entity)this), 0);

        if (World != null && !World.IsClientSide)
        {
            // 1/8 chance to spawn chicken(s)
            if (EntityRandom.NextInt(8) == 0)
            {
                int count = EntityRandom.NextInt(32) == 0 ? 4 : 1;
                for (int i = 0; i < count; i++)
                {
                    var chicken = new EntityChicken(World);
                    chicken.SetAge(-24000); // baby
                    chicken.SetPositionAndRotation(PosX, PosY, PosZ, RotationYaw, 0f);
                    World.SpawnEntity(chicken);
                }
            }

            // 8 snowballpoof particles — stub (particle system not yet implemented)
            // World.SpawnParticle("snowballpoof", PosX, PosY, PosZ, 8)

            SetDead();
        }
    }
}
