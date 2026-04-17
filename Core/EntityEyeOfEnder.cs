namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>bs</c> (EntityEyeOfEnder / EyeOfEnderSignal). EntityList "EyeOfEnderSignal", ID 15.
///
/// Flies toward a stronghold target. After 80 ticks: drops Eye of Ender item (4/5 chance)
/// or plays "eye of ender death" world event (1/5 chance).
/// No NBT persistence — despawns on chunk unload.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ThrowableEntities_Spec.md §8
/// </summary>
public sealed class EntityEyeOfEnder : Entity
{
    private const int EyeOfEnderId = 381; // Item ID for Eye of Ender

    private double _targetX;             // obf: b
    private double _targetY;             // obf: c
    private double _targetZ;             // obf: d
    private int    _despawnCounter;      // obf: e
    private bool   _dropItem;            // obf: f — true = drop item; false = break particles

    public EntityEyeOfEnder(World world) : base(world)
    {
        SetSize(0.25f, 0.25f);
    }

    protected override void EntityInit() { }

    // ── Target setting (spec §8.2) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(double targetX, int ?, double targetZ)</c>
    /// Sets the flight target and resets the despawn counter.
    /// </summary>
    public void SetTarget(double targetX, double targetZ)
    {
        double dx = targetX - PosX;
        double dz = targetZ - PosZ;
        double dist = Math.Sqrt(dx * dx + dz * dz);

        if (dist > 12.0)
        {
            // Cap at 12 blocks ahead
            _targetX = PosX + (dx / dist) * 12.0;
            _targetZ = PosZ + (dz / dist) * 12.0;
            _targetY = PosY + 8.0;
        }
        else
        {
            _targetX = targetX;
            _targetZ = targetZ;
            _targetY = PosY + 8.0;
        }

        _despawnCounter = 0;
        _dropItem = EntityRandom.NextInt(5) > 0; // 4/5 chance to drop item
    }

    // ── Tick (spec §8.3) ─────────────────────────────────────────────────────

    public override void Tick()
    {
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;

        base.Tick();

        // Apply velocity
        PosX += MotionX;
        PosY += MotionY;
        PosZ += MotionZ;
        SetPosition(PosX, PosY, PosZ);

        // Yaw/pitch from velocity (smoothed)
        double hSpeed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        float targetYaw   = (float)(Math.Atan2(MotionX, MotionZ) * 180.0 / Math.PI);
        float targetPitch = (float)(Math.Atan2(MotionY, hSpeed)  * 180.0 / Math.PI);
        RotationYaw   += (targetYaw   - RotationYaw)   * 0.2f;
        RotationPitch += (targetPitch - RotationPitch) * 0.2f;

        if (World == null || World.IsClientSide) return;

        // Steer toward target
        double xzDist = Math.Sqrt((PosX - _targetX) * (PosX - _targetX) +
                                  (PosZ - _targetZ) * (PosZ - _targetZ));
        double currentXzSpeed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        double newSpeed = currentXzSpeed + (xzDist - currentXzSpeed) * 0.0025;

        if (xzDist < 1.0)
        {
            newSpeed  *= 0.8;
            MotionY   *= 0.8;
        }

        double yaw = Math.Atan2(PosZ - _targetZ, PosX - _targetX) + Math.PI;
        MotionX = Math.Cos(yaw - Math.PI / 2.0) * newSpeed;
        MotionZ = Math.Sin(yaw - Math.PI / 2.0) * newSpeed;

        if (PosY < _targetY)
            MotionY += (1.0 - MotionY) * 0.015;
        else
            MotionY += (-1.0 - MotionY) * 0.015;

        _despawnCounter++;

        // Portal particle trail — stub (particle system not yet implemented)
        // World.SpawnParticle("portal", PosX - MotionX*0.25, PosY - MotionY*0.25 - 0.5, PosZ - MotionZ*0.25, ...)

        if (_despawnCounter > 80)
        {
            if (_dropItem)
            {
                var itemEntity = new EntityItem(
                    (World)World,
                    PosX, PosY, PosZ,
                    new ItemStack(EyeOfEnderId, 1, 0));
                World.SpawnEntity(itemEntity);
            }
            else
            {
                // World event 2003 = "eye of ender death particles"
                // World.PlayWorldEvent(2003, (int)PosX, (int)PosY, (int)PosZ, 0); // stub
            }
            SetDead();
        }
    }

    // ── No NBT (spec §8.5) ────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
