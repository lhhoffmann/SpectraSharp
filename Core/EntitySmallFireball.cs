namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>yn</c> (EntitySmallFireball) — Blaze projectile.
/// EntityList "SmallFireball", ID 13. Extends EntityFireball.
///
/// Differences from EntityFireball:
///   - Smaller hitbox: 0.3125×0.3125
///   - Cannot be targeted (e_() = false) and immune to all damage
///   - Block hit: places fire on adjacent air block; entity hit: 5 fire damage + 5s burn
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §7
/// </summary>
public sealed class EntitySmallFireball : EntityFireball
{
    public EntitySmallFireball(World world, LivingEntity owner, double dirX, double dirY, double dirZ)
        : base(world, owner, dirX, dirY, dirZ)
    {
        SetSize(0.3125f, 0.3125f);
    }

    public EntitySmallFireball(World world, double x, double y, double z, double dirX, double dirY, double dirZ)
        : base(world, x, y, z, dirX, dirY, dirZ)
    {
        SetSize(0.3125f, 0.3125f);
    }

    /// <summary>World constructor for NBT deserialisation.</summary>
    public EntitySmallFireball(World world) : base(world)
    {
        SetSize(0.3125f, 0.3125f);
    }

    /// <summary>Immune to all damage — cannot be destroyed by weapons (spec §7.1).</summary>
    public override bool AttackEntityFrom(DamageSource source, int amount) => false;

    protected override void OnImpact(MovingObjectPosition hit)
    {
        if (World == null || World.IsClientSide)
        {
            SetDead();
            return;
        }

        if (hit.Type == HitType.Entity && hit.Entity is Entity target)
        {
            if (!target.IsImmuneToFire)
            {
                target.AttackEntityFrom(DamageSource.Fireball(this, (Entity?)Owner ?? (Entity)this), 5);
                target.SetFire(5);
            }
        }
        else if (hit.Type == HitType.Tile)
        {
            // Determine the adjacent block position on the hit face
            int bx = hit.BlockX;
            int by = hit.BlockY;
            int bz = hit.BlockZ;

            switch (hit.Face)
            {
                case 0: by--; break; // down
                case 1: by++; break; // up
                case 2: bz--; break; // north
                case 3: bz++; break; // south
                case 4: bx--; break; // west
                case 5: bx++; break; // east
            }

            // Place fire if the adjacent block is replaceable (air/plants)
            int adjacentId = World.GetBlockId(bx, by, bz);
            Block? adjacentBlock = Block.BlocksList[adjacentId];
            if (adjacentId == 0 || (adjacentBlock != null && adjacentBlock.CanReplace(World, bx, by, bz)))
                World.SetBlock(bx, by, bz, 51); // 51 = fire
        }

        SetDead();
    }
}
