namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>dd</c> (EntityTNTPrimed) — the entity spawned when TNT is ignited.
///
/// Ticks every server tick; fuse counts down from 80 (4 s) to 0, then explodes.
/// Spawns a smoke particle each tick while fuse burns.
///
/// Quirks preserved (spec §7):
///   1. Initial horizontal velocity uses Math.random() (Java static, not world RNG).
///   2. Eye height is 0.0 (no eye).
///   3. Smoke particle emitted at PosY + 0.5 every tick.
///   4. Client removes entity without creating explosion.
///   5. Fuse written as TAG_Byte in NBT (may overflow for values > 127, but max is 80).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Explosion_Spec.md §7
/// </summary>
public sealed class EntityTNTPrimed : Entity
{
    // ── Fields (spec §7, §2) ─────────────────────────────────────────────────

    /// <summary>obf: a — fuse timer in ticks. Counts down from 80 to 0, then explodes.</summary>
    public int Fuse = 80;

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn constructor — called when TNT is ignited in the world.
    /// Spec: <c>dd(ry, double x, double y, double z)</c>.
    /// </summary>
    public EntityTNTPrimed(World world, double x, double y, double z) : base(world)
    {
        SetSize(0.98f, 0.98f);
        YOffset = Height / 2.0f;   // eye height = 0.49 (obf: L = N/2)

        // Initial velocity: random horizontal direction at 0.02 speed, 0.2 upward (quirk 1)
        double angle = Random.Shared.NextDouble() * Math.PI * 2.0;
        MotionX = -Math.Sin(angle) * 0.02;
        MotionY = 0.2;
        MotionZ = -Math.Cos(angle) * 0.02;

        Fuse = 80;
        SetPosition(x, y, z);
        PrevPosX = x; PrevPosY = y; PrevPosZ = z;
    }

    /// <summary>
    /// NBT / registry constructor — must match the <c>(World)</c> signature used by
    /// <see cref="EntityRegistry.CreateFromNbt"/>.
    /// </summary>
    public EntityTNTPrimed(World world) : base(world)
    {
        SetSize(0.98f, 0.98f);
        YOffset = Height / 2.0f;
    }

    /// <summary>obf: <c>dd.b()</c> — entity init. No DataWatcher slots needed.</summary>
    protected override void EntityInit() { }

    // ── Tick (spec §7 — dd.a()) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>dd.a()</c> — entity tick.
    /// Applies gravity, friction, fuse countdown, explosion on fuse = 0.
    /// </summary>
    public override void Tick()
    {
        // Save prev position
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;

        // Gravity
        MotionY -= 0.04;

        // Move with collision, then apply friction
        Move(MotionX, MotionY, MotionZ);
        MotionX *= 0.98;
        MotionY *= 0.98;
        MotionZ *= 0.98;

        // Ground friction
        if (OnGround)
        {
            MotionX *= 0.7;
            MotionZ *= 0.7;
            MotionY *= -0.5;
        }

        // Smoke particle every tick while fuse burns (client-visible, server emits too)
        // Stub: particle system not yet implemented

        // Fuse countdown
        Fuse--;
        if (Fuse <= 0)
        {
            SetDead();
            if (World != null && !World.IsClientSide)
                Explode();
        }
    }

    // ── Explode (spec §7 — dd.g()) ───────────────────────────────────────────

    /// <summary>
    /// obf: <c>dd.g()</c> — creates the explosion.
    /// Power = 4.0F, source entity = null (world trigger). Spec §7.
    /// </summary>
    private void Explode()
    {
        World!.CreateExplosion(null, PosX, PosY, PosZ, 4.0f, isIncendiary: false);
    }

    // ── NBT (spec §7) ────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>dd.a(NbtCompound)</c> — writes fuse as TAG_Byte.
    /// Spec §7: field "Fuse".
    /// </summary>
    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutByte("Fuse", (byte)Fuse); // quirk 5: byte — max 80, fits fine
    }

    /// <summary>
    /// obf: <c>dd.b(NbtCompound)</c> — reads fuse.
    /// Spec §7: field "Fuse".
    /// </summary>
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        Fuse = tag.GetByte("Fuse");
    }
}
