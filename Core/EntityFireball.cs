namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aad</c> (EntityFireball) — large Ghast projectile.
/// EntityList "Fireball", ID 12.
///
/// Physics: acceleration model (not velocity). 0.95F air drag, no gravity.
/// Owner-exclusion: 25 ticks.
/// Impact: 4 damage to entity + power-1.0 incendiary explosion.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §6
/// </summary>
public class EntityFireball : Entity
{
    // ── Fields (spec §6.1) ───────────────────────────────────────────────────

    public LivingEntity? Owner;            // obf: a

    protected double AccelX;              // obf: b
    protected double AccelY;              // obf: c
    protected double AccelZ;              // obf: d

    protected int XTile       = -1;       // obf: e
    protected int YTile       = -1;       // obf: f
    protected int ZTile       = -1;       // obf: g
    protected int InTileId    =  0;       // obf: h
    protected bool InGround   = false;    // obf: i
    protected int InGroundTicks = 0;      // obf: aq
    protected int FlightTicks   = 0;      // obf: ar

    protected override void EntityInit() { }

    private const float AirDrag      = 0.95f;
    private const float WaterDrag    = 0.80f;
    private const int   DespawnTicks = 1200;
    private const int   OwnerExclude = 25;

    // ── Constructors ─────────────────────────────────────────────────────────

    /// <summary>
    /// Owner-spawn constructor. Applies Gaussian spread (σ=0.4) to direction,
    /// normalises, scales to 0.1F per axis for the acceleration vector.
    /// </summary>
    public EntityFireball(World world, LivingEntity owner, double dirX, double dirY, double dirZ)
        : base(world)
    {
        Owner = owner;
        SetSize(1.0f, 1.0f);
        SetPosition(owner.PosX, owner.PosY, owner.PosZ);

        dirX += EntityRandom.NextGaussian() * 0.4;
        dirY += EntityRandom.NextGaussian() * 0.4;
        dirZ += EntityRandom.NextGaussian() * 0.4;

        double len = Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        if (len > 0)
        {
            AccelX = dirX / len * 0.1;
            AccelY = dirY / len * 0.1;
            AccelZ = dirZ / len * 0.1;
        }
    }

    /// <summary>Direct-position constructor (for world gen / natural spawn).</summary>
    public EntityFireball(World world, double x, double y, double z, double dirX, double dirY, double dirZ)
        : base(world)
    {
        SetSize(1.0f, 1.0f);
        SetPosition(x, y, z);

        double len = Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
        if (len > 0)
        {
            AccelX = dirX / len * 0.1;
            AccelY = dirY / len * 0.1;
            AccelZ = dirZ / len * 0.1;
        }
    }

    /// <summary>World constructor required for NBT deserialisation.</summary>
    public EntityFireball(World world) : base(world)
    {
        SetSize(1.0f, 1.0f);
    }

    // ── Tick (spec §6.3) ─────────────────────────────────────────────────────

    public override void Tick()
    {
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;

        base.Tick();

        if (InGround)
        {
            int currentId = World?.GetBlockId(XTile, YTile, ZTile) ?? 0;
            if (currentId == InTileId)
            {
                InGroundTicks++;
                if (InGroundTicks >= DespawnTicks)
                    SetDead();
            }
            else
            {
                InGround       = false;
                InGroundTicks  = 0;
                FlightTicks    = 0;
            }
            return;
        }

        FlightTicks++;

        // Accumulate velocity from acceleration
        MotionX += AccelX;
        MotionY += AccelY;
        MotionZ += AccelZ;

        // Collision check
        var hitResult = PerformCollisionCheck();
        if (hitResult != null)
        {
            OnImpact(hitResult);
            return;
        }

        // Move
        PosX += MotionX;
        PosY += MotionY;
        PosZ += MotionZ;
        SetPosition(PosX, PosY, PosZ);

        // Yaw/pitch from velocity
        double hSpeed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        RotationYaw   = (float)(Math.Atan2(MotionX, MotionZ) * 180.0 / Math.PI);
        RotationPitch = (float)(Math.Atan2(MotionY, hSpeed)  * 180.0 / Math.PI);

        // Drag — no gravity
        int blockAtPos = World?.GetBlockId((int)Math.Floor(PosX), (int)Math.Floor(PosY), (int)Math.Floor(PosZ)) ?? 0;
        float drag = (blockAtPos == 8 || blockAtPos == 9) ? WaterDrag : AirDrag;
        MotionX *= drag;
        MotionY *= drag;
        MotionZ *= drag;

        // Smoke particle each tick — stub (particle system not yet implemented)
        // World?.SpawnParticle("smoke", PosX, PosY + 0.5, PosZ, 1)
    }

    private MovingObjectPosition? PerformCollisionCheck()
    {
        if (World == null) return null;
        return World.RayTraceBlocks(
            Vec3.GetFromPool(PrevPosX, PrevPosY, PrevPosZ),
            Vec3.GetFromPool(PosX + MotionX, PosY + MotionY, PosZ + MotionZ));
    }

    protected virtual void OnImpact(MovingObjectPosition hit)
    {
        if (World == null || World.IsClientSide) return;

        if (hit.Type == HitType.Entity && hit.Entity is Entity target)
            target.AttackEntityFrom(DamageSource.Fireball(this, (Entity?)Owner ?? (Entity)this), 4);

        World.CreateExplosion(null, PosX, PosY, PosZ, 1.0f, isIncendiary: true);
        SetDead();
    }

    // ── Deflection (spec §6.6) ────────────────────────────────────────────────

    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (source.GetAttacker() is Entity attacker)
        {
            MotionX = attacker.MotionX;
            MotionY = attacker.MotionY;
            MotionZ = attacker.MotionZ;
            double len = Math.Sqrt(MotionX * MotionX + MotionY * MotionY + MotionZ * MotionZ);
            if (len > 0)
            {
                AccelX = MotionX / len * 0.1;
                AccelY = MotionY / len * 0.1;
                AccelZ = MotionZ / len * 0.1;
            }
        }
        return true;
    }

    // ── NBT (spec §6.5) ──────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("xTile",    (short)XTile);
        tag.PutShort("yTile",    (short)YTile);
        tag.PutShort("zTile",    (short)ZTile);
        tag.PutByte ("inTile",   (byte)InTileId);
        tag.PutByte ("inGround", (byte)(InGround ? 1 : 0));
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        XTile    = tag.GetShort("xTile");
        YTile    = tag.GetShort("yTile");
        ZTile    = tag.GetShort("zTile");
        InTileId = tag.GetByte("inTile") & 0xFF;
        InGround = tag.GetByte("inGround") != 0;
    }
}
