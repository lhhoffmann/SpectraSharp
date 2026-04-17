namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>no</c> (EntityBoat) — rideable boat entity.
/// Floats on water surfaces with buoyancy physics; breaks into planks and sticks on
/// hard wall impact or sufficient accumulated damage.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityBoat_Spec.md
/// </summary>
public class EntityBoat : Entity
{
    // ── DataWatcher slots (spec §2) ───────────────────────────────────────────
    private const int DwShakeTicks = 17;   // obf: c()  — ticks of shake remaining
    private const int DwShakeDir   = 18;   // obf: i()  — shake direction multiplier (±1)
    private const int DwDamage     = 19;   // obf: g()  — accumulated damage (break at 40)

    // ── Client interpolation fields (obf: a-f) ────────────────────────────────
    private int    _lerpSteps;
    private double _lerpX, _lerpY, _lerpZ;
    private double _lerpYaw, _lerpPitch;

    // ── Velocity (server internal copy, obf: g/h/i) ──────────────────────────
    private double _velX, _velY, _velZ;

    // ── Item RegistryIndex constants ──────────────────────────────────────────
    private const int PlanksId = 5;   // block ID 5 = wood planks → item RegistryIndex 5
    private const int StickId  = 280; // item rawId 24 → 256+24=280

    // ── Constructor ──────────────────────────────────────────────────────────

    public EntityBoat(World world) : base(world)
    {
        SetSize(1.5f, 0.6f);
        YOffset = Height / 2.0f; // 0.3F — eye height
    }

    protected override void EntityInit()
    {
        DataWatcher.Register(DwShakeTicks, 0);
        DataWatcher.Register(DwShakeDir,   1);
        DataWatcher.Register(DwDamage,     0);
    }

    // ── Convenience DW accessors ─────────────────────────────────────────────

    private int  GetShakeTicks()    => DataWatcher.GetInt(DwShakeTicks);
    private void SetShakeTicks(int v) => DataWatcher.UpdateObject(DwShakeTicks, v);
    private int  GetShakeDir()      => DataWatcher.GetInt(DwShakeDir);
    private void SetShakeDir(int v)   => DataWatcher.UpdateObject(DwShakeDir, v);
    private int  GetDamage()        => DataWatcher.GetInt(DwDamage);
    private void SetDamage(int v)     => DataWatcher.UpdateObject(DwDamage, v);

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick();

        // 4.1 — Decrement shake and damage counters
        if (GetShakeTicks() > 0) SetShakeTicks(GetShakeTicks() - 1);
        if (GetDamage()     > 0) SetDamage    (GetDamage()     - 1);

        if (World == null) return;

        if (World.IsClientSide)
            TickClient();
        else
            TickServer();
    }

    // ── Client tick (4.2) ────────────────────────────────────────────────────

    private void TickClient()
    {
        if (_lerpSteps > 0)
        {
            double ratio = 1.0 / _lerpSteps;
            PosX += (_lerpX - PosX) * ratio;
            PosY += (_lerpY - PosY) * ratio;
            PosZ += (_lerpZ - PosZ) * ratio;
            RotationYaw   += (float)((_lerpYaw   - RotationYaw)   * ratio);
            RotationPitch += (float)((_lerpPitch  - RotationPitch) * ratio);
            _lerpSteps--;
        }
        else
        {
            PosX += MotionX;
            PosY += MotionY;
            PosZ += MotionZ;
        }

        if (OnGround) { MotionX *= 0.5; MotionY *= 0.5; MotionZ *= 0.5; }
        MotionX *= 0.99f;
        MotionY *= 0.95f;
        MotionZ *= 0.99f;
    }

    // ── Server tick (4.3) ────────────────────────────────────────────────────

    private void TickServer()
    {
        // Water fraction sampling (5 Y-slices)
        double waterFraction = ComputeWaterFraction();

        // Buoyancy Y velocity
        if (waterFraction < 1.0)
        {
            double push = 0.04f * (waterFraction * 2.0 - 1.0);
            _velY += push;
        }
        else
        {
            if (_velY < 0.0) _velY *= 0.5;
            _velY += 0.007f;
        }

        // Passenger XZ contribution
        if (Rider != null)
        {
            _velX += Rider.MotionX * 0.2;
            _velZ += Rider.MotionZ * 0.2;
        }

        // Speed cap
        _velX = System.Math.Clamp(_velX, -0.4, 0.4);
        _velZ = System.Math.Clamp(_velZ, -0.4, 0.4);

        // On-ground halving
        if (OnGround) { _velX *= 0.5; _velY *= 0.5; _velZ *= 0.5; }

        // Physics sweep
        Move(_velX, _velY, _velZ);

        // Wall collision break
        if (IsCollidedHorizontally && System.Math.Sqrt(_velX * _velX + _velZ * _velZ) > 0.2)
        {
            BreakBoat();
            return;
        }

        // Drag
        _velX *= 0.99f;
        _velY *= 0.95f;
        _velZ *= 0.99f;

        // Sync motion for base entity physics
        MotionX = _velX;
        MotionY = _velY;
        MotionZ = _velZ;

        // Yaw alignment toward movement direction
        double hspeed = System.Math.Sqrt(_velX * _velX + _velZ * _velZ);
        if (hspeed > 0.01)
        {
            double targetYaw = System.Math.Atan2(_velZ, _velX) * (180.0 / System.Math.PI);
            double yawDiff   = targetYaw - RotationYaw;
            // Normalise to [-180, 180]
            while (yawDiff > 180)  yawDiff -= 360;
            while (yawDiff < -180) yawDiff += 360;
            yawDiff = System.Math.Clamp(yawDiff, -20.0, 20.0);
            RotationYaw = (float)(RotationYaw + yawDiff);
        }

        // Snow block destruction at four corners
        DestroySnowUnderCorners();

        // Eject dead passenger
        if (Rider != null && Rider.IsDead) Rider = null;

        // Passenger positioning
        TickRiderPosition();

        // Boat-boat push
        var nearby = World!.GetEntitiesWithinAABB<EntityBoat>(BoundingBox.Expand(0.2f, 0.0f, 0.2f));
        foreach (var other in nearby)
        {
            if (other != this && other != Rider)
                ApplyBoatPush(other);
        }
    }

    // ── Water fraction (spec §4.3) ────────────────────────────────────────────

    private double ComputeWaterFraction()
    {
        if (World == null) return 0.0;
        const int   Slices       = 5;
        const double YCorrection = 0.125;
        double fraction = 0.0;
        double minY = BoundingBox.MinY;
        double maxY = BoundingBox.MaxY;
        double dy    = (maxY - minY) / Slices;

        for (int i = 0; i < Slices; i++)
        {
            double sampleY = minY + dy * i + YCorrection;
            int by = (int)System.Math.Floor(sampleY);
            int bxMin = (int)System.Math.Floor(BoundingBox.MinX);
            int bxMax = (int)System.Math.Floor(BoundingBox.MaxX);
            int bzMin = (int)System.Math.Floor(BoundingBox.MinZ);
            int bzMax = (int)System.Math.Floor(BoundingBox.MaxZ);
            bool hasWater = false;
            for (int bx = bxMin; bx <= bxMax && !hasWater; bx++)
            for (int bz = bzMin; bz <= bzMax && !hasWater; bz++)
            {
                int id = World.GetBlockId(bx, by, bz);
                if (id == 8 || id == 9) hasWater = true;
            }
            if (hasWater) fraction += 1.0 / Slices;
        }
        return fraction;
    }

    // ── Snow destruction at corners (spec §4.3) ───────────────────────────────

    private void DestroySnowUnderCorners()
    {
        if (World == null) return;
        int by = (int)System.Math.Floor(BoundingBox.MinY);
        int[] xs = [(int)System.Math.Floor(BoundingBox.MinX), (int)System.Math.Floor(BoundingBox.MaxX)];
        int[] zs = [(int)System.Math.Floor(BoundingBox.MinZ), (int)System.Math.Floor(BoundingBox.MaxZ)];
        foreach (int bx in xs)
        foreach (int bz in zs)
            if (World.GetBlockId(bx, by, bz) == 78) // 78 = snow layer (BlockSnow)
                World.SetBlock(bx, by, bz, 0);
    }

    // ── Boat-boat push (spec §8) ──────────────────────────────────────────────

    private void ApplyBoatPush(EntityBoat other)
    {
        double dx = other.PosX - PosX;
        double dz = other.PosZ - PosZ;
        double len = System.Math.Sqrt(dx * dx + dz * dz);
        if (len < 0.01) return;
        // Dot product against boat heading
        double headX = System.Math.Cos(RotationYaw * System.Math.PI / 180.0);
        double headZ = System.Math.Sin(RotationYaw * System.Math.PI / 180.0);
        double dot   = (dx / len) * headX + (dz / len) * headZ;
        if (System.Math.Abs(dot) < 0.8) return;
        double push  = 0.05 / len;
        other._velX += dx * push;
        other._velZ += dz * push;
    }

    // ── Damage and break (spec §6) ────────────────────────────────────────────

    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (World == null || World.IsClientSide || IsDead) return true;

        SetShakeDir(-GetShakeDir());
        SetShakeTicks(10);
        SetDamage(GetDamage() + amount * 10);

        if (GetDamage() > 40)
        {
            if (Rider != null) { Rider.Mount = null; Rider = null; }
            BreakBoat();
        }

        return true;
    }

    // ── Break / drop ─────────────────────────────────────────────────────────

    private void BreakBoat()
    {
        if (World == null) return;
        SetDead();
        // 3 planks
        for (int i = 0; i < 3; i++)
        {
            var planks = new EntityItem(World, PosX, PosY, PosZ, new ItemStack(PlanksId, 1, 0));
            planks.MotionX = EntityRandom.NextGaussian() * 0.05;
            planks.MotionY = 0.2 + EntityRandom.NextGaussian() * 0.04;
            planks.MotionZ = EntityRandom.NextGaussian() * 0.05;
            World.SpawnEntity(planks);
        }
        // 2 sticks
        for (int i = 0; i < 2; i++)
        {
            var stick = new EntityItem(World, PosX, PosY, PosZ, new ItemStack(StickId, 1, 0));
            stick.MotionX = EntityRandom.NextGaussian() * 0.05;
            stick.MotionY = 0.2 + EntityRandom.NextGaussian() * 0.04;
            stick.MotionZ = EntityRandom.NextGaussian() * 0.05;
            World.SpawnEntity(stick);
        }
    }

    // ── Passenger positioning (spec §5) ──────────────────────────────────────
    // Called from TickServer each tick to keep rider in front of boat.

    private void TickRiderPosition()
    {
        if (Rider == null) return;
        double offsetX = System.Math.Cos(RotationYaw * System.Math.PI / 180.0) * 0.4;
        double offsetZ = System.Math.Sin(RotationYaw * System.Math.PI / 180.0) * 0.4;
        Rider.SetPosition(PosX + offsetX, PosY - 0.3 + Rider.YOffset, PosZ + offsetZ);
    }

    // ── Client-side interpolation setter (for network updates) ───────────────

    public void SetPositionAndRotationDirect(double x, double y, double z, float yaw, float pitch, int steps)
    {
        _lerpX     = x; _lerpY = y; _lerpZ = z;
        _lerpYaw   = yaw; _lerpPitch = pitch;
        _lerpSteps = steps;
    }

    // ── NBT ──────────────────────────────────────────────────────────────────

    // No unique NBT fields per spec §9.
    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
