namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>fm</c> — abstract base for all thrown items
/// (snowball, egg, ender pearl).
///
/// Physics: 0.99F air drag, 0.03F gravity, 0.25×0.25 hitbox.
/// Sticks in blocks after impact; despawns after 1200 ticks inGround.
/// Excludes owner from hit for first 5 flight ticks.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §2
/// </summary>
public abstract class ThrowableBase : Entity
{
    // ── Fields (spec §2.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>c</c> — thrower / owner. Not serialised.</summary>
    public LivingEntity? Owner;

    /// <summary>obf: <c>a</c> — true when embedded in a block.</summary>
    protected bool InGround;

    /// <summary>obf: <c>b</c> — shake counter (counts down after impact).</summary>
    protected int Shake;

    /// <summary>obf: <c>d/e/f</c> — block coordinates stuck in.</summary>
    protected int XTile = -1, YTile = -1, ZTile = -1;

    /// <summary>obf: <c>g</c> — block ID at stuck position.</summary>
    protected int InTileId;

    /// <summary>obf: <c>h</c> — despawn tick counter while inGround.</summary>
    protected int InGroundTicks;

    /// <summary>obf: <c>i</c> — flight tick counter for owner exclusion.</summary>
    protected int FlightTicks;

    protected override void EntityInit() { }

    // ── Constants (spec §2.2) ────────────────────────────────────────────────

    protected const float AirDrag       = 0.99f;
    protected const float WaterDrag     = 0.80f;
    protected const float Gravity       = 0.03f;
    protected const int   DespawnTicks  = 1200;
    protected const int   OwnerExclude  = 5;

    // ── Constructors ─────────────────────────────────────────────────────────

    /// <summary>Spawns at the owner's eye position and immediately fires.</summary>
    protected ThrowableBase(World world, LivingEntity owner) : base(world)
    {
        Owner = owner;
        SetSize(0.25f, 0.25f);

        // Position at eye level, slightly behind the owner
        double yawRad = owner.RotationYaw * Math.PI / 180.0;
        double spawnX = owner.PosX - Math.Sin(yawRad) * 0.16;
        double spawnZ = owner.PosZ + Math.Cos(yawRad) * 0.16;
        double spawnY = owner.PosY + owner.GetEyeHeight() - 0.1;
        SetPosition(spawnX, spawnY, spawnZ);
        YOffset = 0.0f;

        // Fire with default throw speed
        SetThrowVelocity(owner.MotionX, owner.MotionY, owner.MotionZ, GetThrowSpeed(), 1.0f);
    }

    /// <summary>World-position constructor (for deserialised entities).</summary>
    protected ThrowableBase(World world, double x, double y, double z) : base(world)
    {
        SetSize(0.25f, 0.25f);
        SetPosition(x, y, z);
    }

    /// <summary>Default throw speed (1.5F for most throwables). Override to change.</summary>
    protected virtual float GetThrowSpeed() => 1.5f;

    // ── Velocity initialisation (spec §2.3) ──────────────────────────────────

    /// <summary>
    /// obf: <c>a(double dx, double dy, double dz, float speed, float inaccuracy)</c>
    /// Normalises direction, adds Gaussian noise, then multiplies by speed.
    /// </summary>
    public void SetThrowVelocity(double dx, double dy, double dz, float speed, float inaccuracy)
    {
        double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (len == 0) return;
        dx /= len; dy /= len; dz /= len;

        dx += EntityRandom.NextGaussian() * 0.0075 * inaccuracy;
        dy += EntityRandom.NextGaussian() * 0.0075 * inaccuracy;
        dz += EntityRandom.NextGaussian() * 0.0075 * inaccuracy;

        MotionX = dx * speed;
        MotionY = dy * speed;
        MotionZ = dz * speed;

        double hSpeed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        PrevRotYaw = RotationYaw   = (float)(Math.Atan2(MotionX, MotionZ) * 180.0 / Math.PI);
        PrevRotPitch = RotationPitch = (float)(Math.Atan2(MotionY, hSpeed) * 180.0 / Math.PI);
    }

    // ── Tick (spec §2.4) ─────────────────────────────────────────────────────

    public override void Tick()
    {
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;

        base.Tick();

        if (Shake > 0) Shake--;

        if (InGround)
        {
            // Stuck in block — check if block still there
            int currentId = World?.GetBlockId(XTile, YTile, ZTile) ?? 0;
            if (currentId == InTileId)
            {
                InGroundTicks++;
                if (InGroundTicks >= DespawnTicks)
                    SetDead();
            }
            else
            {
                // Block changed — escape
                InGround = false;
                MotionX *= EntityRandom.NextFloat() * 0.2f;
                MotionY *= EntityRandom.NextFloat() * 0.2f;
                MotionZ *= EntityRandom.NextFloat() * 0.2f;
                InGroundTicks = 0;
                FlightTicks   = 0;
            }
            return;
        }

        FlightTicks++;

        // Ray-trace and entity collision
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
        float targetYaw   = (float)(Math.Atan2(MotionX, MotionZ) * 180.0 / Math.PI);
        float targetPitch = (float)(Math.Atan2(MotionY, hSpeed)  * 180.0 / Math.PI);
        RotationYaw   = targetYaw;
        RotationPitch = targetPitch;

        // Drag and gravity
        int blockAtPos = World?.GetBlockId((int)Math.Floor(PosX), (int)Math.Floor(PosY), (int)Math.Floor(PosZ)) ?? 0;
        bool inWater = blockAtPos == 8 || blockAtPos == 9;
        float drag = inWater ? WaterDrag : AirDrag;
        MotionX *= drag;
        MotionY *= drag;
        MotionZ *= drag;
        MotionY -= Gravity;
    }

    /// <summary>
    /// Simple ray-trace / entity collision check. Returns a hit result or null.
    /// Stub implementation — full sweep would need block AABB scanning.
    /// </summary>
    private MovingObjectPosition? PerformCollisionCheck()
    {
        if (World == null) return null;
        return World.RayTraceBlocks(
            Vec3.GetFromPool(PrevPosX, PrevPosY, PrevPosZ),
            Vec3.GetFromPool(PosX + MotionX, PosY + MotionY, PosZ + MotionZ));
    }

    /// <summary>
    /// Called when the projectile hits something (block or entity).
    /// Subclasses implement the impact effect.
    /// obf: <c>a(gv hitResult)</c>
    /// </summary>
    protected abstract void OnImpact(MovingObjectPosition hit);

    // ── Helpers ───────────────────────────────────────────────────────────────

    public override double GetEyeHeight() => Owner?.GetEyeHeight() ?? 0.5;

    // ── NBT (spec §2.5) ──────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("xTile",    (short)XTile);
        tag.PutShort("yTile",    (short)YTile);
        tag.PutShort("zTile",    (short)ZTile);
        tag.PutByte ("inTile",   (byte)InTileId);
        tag.PutByte ("shake",    (byte)Shake);
        tag.PutByte ("inGround", (byte)(InGround ? 1 : 0));
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        XTile    = tag.GetShort("xTile");
        YTile    = tag.GetShort("yTile");
        ZTile    = tag.GetShort("zTile");
        InTileId = tag.GetByte("inTile") & 0xFF;
        Shake    = tag.GetByte("shake");
        InGround = tag.GetByte("inGround") != 0;
    }
}
