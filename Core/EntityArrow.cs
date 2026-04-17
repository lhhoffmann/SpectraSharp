namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ro</c> (EntityArrow) — projectile entity. String ID "Arrow", int ID 10.
///
/// Physics per tick (§5.3):
///   B. Stuck in block: age counter; despawn at 1200 ticks.
///   C. In-flight: ray-trace block + entity hits; deal damage; apply gravity/drag.
///
/// Damage formula (§7.2):
///   baseDamage = ceil(speed × 2.0)
///   if critical: baseDamage += nextInt(baseDamage/2 + 2)
///
/// Quirks preserved (spec §9):
///   1. Shooter reference not serialised — lost on world save.
///   2. IsCritical not serialised — lost on reload.
///   3. Shake counts down from 7, independent of despawn timer.
///   4. If block changed while arrow is stuck, arrow is released.
///   5. Shooter collision excluded for first 5 ticksFlying.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md §5.2–5.4
/// </summary>
public sealed class EntityArrow : Entity
{
    // ── Fields (spec §3.2) ───────────────────────────────────────────────────

    public int     XTile         = -1;    // obf: e
    public int     YTile         = -1;    // obf: f
    public int     ZTile         = -1;    // obf: g
    public int     InTile        =  0;    // obf: h — block ID of stuck block
    public int     InData        =  0;    // obf: i — metadata of stuck block
    public bool    InGround      = false; // obf: aq
    public bool    IsPlayerArrow = false; // obf: a — shot by player
    public int     Shake         =  0;    // obf: b — post-impact wobble (0→7)
    public Entity? Shooter;               // obf: c — not serialised (spec §9.1)
    public int     TicksInGround =  0;    // obf: ar — despawn counter
    public int     TicksFlying   =  0;    // obf: as — exclude shooter for first 5
    public bool    IsCritical    = false; // obf: d — not serialised (spec §9.2)

    // ── Constants (spec §4.2) ────────────────────────────────────────────────

    private const float Gravity            = 0.05f;
    private const float AirDrag            = 0.99f;
    private const float WaterDrag          = 0.80f;
    private const float EntityExpandRadius = 0.30f;
    private const float EmbedBackstep      = 0.05f;
    private const int   DespawnTicks       = 1200;
    private const int   ShooterExclude     = 5;

    // ── Constructors ─────────────────────────────────────────────────────────

    /// <summary>
    /// Shooter constructor — positions at eye level and fires in the look direction.
    /// Spec: §5.2
    /// </summary>
    public EntityArrow(World world, LivingEntity shooter, float speed) : base(world)
    {
        Shooter       = shooter;
        IsPlayerArrow = shooter is EntityPlayer;

        SetSize(0.5f, 0.5f);

        float yawRad   =  shooter.RotationYaw   * MathF.PI / 180.0f;
        float pitchRad = -shooter.RotationPitch   * MathF.PI / 180.0f;

        // Position near shooter eye (spec §5.2 step 4)
        double startX = shooter.PosX - MathHelper.Sin(yawRad) * MathHelper.Cos(pitchRad) * 0.16;
        double startY = shooter.PosY + shooter.Height * 0.62 - 0.1;
        double startZ = shooter.PosZ - MathHelper.Cos(yawRad) * MathHelper.Cos(pitchRad) * 0.16;
        SetPosition(startX, startY, startZ);

        // Direction vector, then normalize + apply in setShootingVector
        double vx = -MathHelper.Sin(yawRad) * MathHelper.Cos(pitchRad);
        double vy =  MathHelper.Sin(pitchRad);
        double vz = -MathHelper.Cos(yawRad) * MathHelper.Cos(pitchRad);

        SetShootingVector(vx, vy, vz, speed * 1.5f, 1.0f);
    }

    /// <summary>Deserialization / test constructor.</summary>
    public EntityArrow(World world) : base(world) { SetSize(0.5f, 0.5f); }

    protected override void EntityInit() { }

    // ── setShootingVector (spec §5.2.1) ─────────────────────────────────────

    public void SetShootingVector(double vx, double vy, double vz, float speed, float spread)
    {
        double len = Math.Sqrt(vx * vx + vy * vy + vz * vz);
        if (len <= 0) return;
        vx /= len; vy /= len; vz /= len;

        vx += EntityRandom.NextGaussian() * 0.0075 * spread;
        vy += EntityRandom.NextGaussian() * 0.0075 * spread;
        vz += EntityRandom.NextGaussian() * 0.0075 * spread;

        vx *= speed; vy *= speed; vz *= speed;
        MotionX = vx; MotionY = vy; MotionZ = vz;

        float hz = MathF.Sqrt((float)(vx * vx + vz * vz));
        RotationYaw   = (float)(Math.Atan2(vx, vz) * 180.0 / Math.PI);
        RotationPitch = (float)(Math.Atan2(vy, hz)  * 180.0 / Math.PI);
        PrevRotYaw    = RotationYaw;
        PrevRotPitch  = RotationPitch;
        TicksInGround = 0;
    }

    // ── Tick (spec §5.3) ────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;

        // ── B. Stuck in block ──────────────────────────────────────────────

        if (InGround)
        {
            int curId   = World.GetBlockId(XTile, YTile, ZTile);
            int curMeta = World.GetBlockMetadata(XTile, YTile, ZTile);

            if (curId != InTile || curMeta != InData)
            {
                InGround      = false;
                MotionX      *= EntityRandom.NextFloat() * 0.2;
                MotionY      *= EntityRandom.NextFloat() * 0.2;
                MotionZ      *= EntityRandom.NextFloat() * 0.2;
                TicksInGround = 0;
                TicksFlying   = 0;
            }
            else
            {
                TicksInGround++;
                if (TicksInGround >= DespawnTicks) IsDead = true;
            }
            if (Shake > 0) Shake--;
            return;
        }

        // ── C. In-flight ───────────────────────────────────────────────────

        TicksFlying++;

        // 1. Block ray-trace
        var start    = Vec3.GetFromPool(PosX, PosY, PosZ);
        var end      = Vec3.GetFromPool(PosX + MotionX, PosY + MotionY, PosZ + MotionZ);
        var conWorld = (World)World;
        var blockHit = conWorld.RayTraceBlocks(start, end);

        // 2. Entity scan in expanded AABB
        double dx = MotionX, dy = MotionY, dz = MotionZ;
        var scanBox  = BoundingBox.Copy()
                                  .Expand(dx, dy, dz)
                                  .Expand(1.0, 1.0, 1.0);
        var entities = conWorld.GetEntitiesWithinAABBExcluding(this, scanBox);

        Entity?  nearestEntity = null;
        double   nearestDist   = double.MaxValue;

        foreach (var e in entities)
        {
            if (!e.IsEntityAlive() || !(e is LivingEntity)) continue;
            if (e == Shooter && TicksFlying < ShooterExclude) continue;

            var hitBox = e.BoundingBox.Copy()
                          .Expand(EntityExpandRadius, EntityExpandRadius, EntityExpandRadius);
            var mop    = hitBox.RayTrace(start, end);
            if (mop == null) continue;

            double dist = start.DistanceTo(mop.HitVec);
            if (dist < nearestDist) { nearestDist = dist; nearestEntity = e; }
        }

        // 3. Process hit
        bool hitEntity = nearestEntity != null
            && (blockHit == null || nearestDist < start.DistanceTo(blockHit.HitVec));

        if (hitEntity && nearestEntity is LivingEntity target)
        {
            float speed  = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            int   damage = (int)Math.Ceiling(speed * 2.0f);
            if (IsCritical) damage += EntityRandom.NextInt(damage / 2 + 2);

            if (target.AttackEntityFrom(DamageSource.Arrow(this, Shooter), damage))
            {
                target.PendingKnockback++;
                conWorld.PlaySoundAt(this, "random.bowhit", 1.0f,
                    1.2f / (EntityRandom.NextFloat() * 0.2f + 0.9f));
                IsDead = true;
            }
            else
            {
                // Blocked: reflect
                MotionX *= -0.1; MotionY *= -0.1; MotionZ *= -0.1;
                RotationYaw  += 180f; PrevRotYaw  += 180f;
                TicksFlying   = 0;
            }
        }
        else if (blockHit != null)
        {
            XTile  = blockHit.BlockX;
            YTile  = blockHit.BlockY;
            ZTile  = blockHit.BlockZ;
            InTile = World.GetBlockId(XTile, YTile, ZTile);
            InData = World.GetBlockMetadata(XTile, YTile, ZTile);

            double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len > 0)
            {
                SetPosition(
                    blockHit.HitVec.X - dx / len * EmbedBackstep,
                    blockHit.HitVec.Y - dy / len * EmbedBackstep,
                    blockHit.HitVec.Z - dz / len * EmbedBackstep);
            }

            MotionX = 0; MotionY = 0; MotionZ = 0;
            InGround   = true;
            Shake      = 7;
            IsCritical = false;
            conWorld.PlaySoundAt(this, "random.bowhit", 1.0f,
                1.2f / (EntityRandom.NextFloat() * 0.2f + 0.9f));
        }

        // 4. Critical particles — stub (particle system not yet implemented)

        // 5. Physics update
        PosX += MotionX; PosY += MotionY; PosZ += MotionZ;

        // Rotation smooth
        float hz     = MathF.Sqrt((float)(MotionX * MotionX + MotionZ * MotionZ));
        float newYaw = (float)(Math.Atan2(MotionX, MotionZ) * 180.0 / Math.PI);
        float newPit = (float)(Math.Atan2(MotionY, hz)       * 180.0 / Math.PI);
        RotationYaw   = PrevRotYaw   + (newYaw - PrevRotYaw)  * 0.2f;
        RotationPitch = PrevRotPitch + (newPit - PrevRotPitch) * 0.2f;
        PrevRotYaw    = RotationYaw;
        PrevRotPitch  = RotationPitch;

        // Water check (IDs 8=flowing, 9=still water)
        int blockAtPos = World.GetBlockId((int)Math.Floor(PosX), (int)Math.Floor(PosY), (int)Math.Floor(PosZ));
        float drag = (blockAtPos == 8 || blockAtPos == 9) ? WaterDrag : AirDrag;
        MotionX *= drag; MotionY *= drag; MotionZ *= drag;

        // Gravity
        MotionY -= Gravity;

        SetPosition(PosX, PosY, PosZ);
    }

    // ── Pickup check (spec §5.3 D) ────────────────────────────────────────────

    public void CheckPlayerPickup(EntityPlayer player)
    {
        if (!InGround || !IsPlayerArrow || Shake > 0) return;
        if (player.Inventory.AddItemStackToInventory(new ItemStack(262, 1)))
        {
            World?.SpawnEntity(null!); // sound stub via side-effect (no-op)
            IsDead = true;
        }
    }

    // ── NBT (spec §5.4) ─────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("xTile",    (short)XTile);
        tag.PutShort("yTile",    (short)YTile);
        tag.PutShort("zTile",    (short)ZTile);
        tag.PutByte( "inTile",   (byte)InTile);
        tag.PutByte( "inData",   (byte)InData);
        tag.PutByte( "shake",    (byte)Shake);
        tag.PutByte( "inGround", (byte)(InGround ? 1 : 0));
        tag.PutBoolean("player", IsPlayerArrow);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        XTile         = tag.GetShort("xTile");
        YTile         = tag.GetShort("yTile");
        ZTile         = tag.GetShort("zTile");
        InTile        = tag.GetByte("inTile")   & 255;
        InData        = tag.GetByte("inData")   & 255;
        Shake         = tag.GetByte("shake")    & 255;
        InGround      = tag.GetByte("inGround") == 1;
        IsPlayerArrow = tag.GetBoolean("player");
    }
}
