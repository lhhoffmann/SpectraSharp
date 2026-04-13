using System.Collections.Generic;

namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>ia</c> (Entity) — abstract base class for every in-world object.
///
/// Owns position, velocity, rotation, bounding box, fire/air timers, and mount/rider links.
/// World stores all entities in its entity list and ticks them by calling <see cref="Tick"/>.
///
/// Quirks preserved (see spec §13):
///   1. Web motion (isInWeb): blocking on ANY axis zeroes ALL three velocity components.
///   2. firstUpdate flag: set false at the END of entityBaseTick; true during the entire first tick.
///   3. Constructor calls d(0,0,0) BEFORE the abstract entityInit(); AABB is zero-size until
///      entityInit calls SetSize.
///   4. Fire ticks stored as int in C#, but written to NBT as a short (truncation).
///   5. Entity.c(ia) distance uses MathHelper.SqrtDouble (float precision from double squared dist).
///   6. Rider Y: mount.posY + mount.Height×0.75 + rider.YOffset (= 75% of mount height).
///   7. DataWatcher flag writes are non-atomic (read-modify-write on the flags byte).
///
/// Stubs (specs pending):
///   - NBT read/write (ik spec pending): abstract hooks remain but use object.
///   - EntityItem (ih) drop helper: uses ItemStack now; DropEntityItem stub.
///   - DamageSource (pm) fire/lava damage: no-op.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Entity_Spec.md
/// </summary>
public abstract class Entity
{
    // ── Static ID counter (spec §2) ───────────────────────────────────────────

    private static int _nextEntityId; // obf: a (static)

    // ── Identity ──────────────────────────────────────────────────────────────

    public int EntityId; // obf: j
    public World? World;  // obf: o (ry)

    // ── Position (spec §2) ────────────────────────────────────────────────────

    public double PrevPosX, PrevPosY, PrevPosZ;          // obf: p/q/r
    public double PosX,     PosY,     PosZ;               // obf: s/t/u
    public double MotionX,  MotionY,  MotionZ;            // obf: v/w/x
    public double LastTickPosX, LastTickPosY, LastTickPosZ; // obf: R/S/T

    // ── Rotation ──────────────────────────────────────────────────────────────

    public float RotationYaw,   RotationPitch;      // obf: y/z
    public float PrevRotYaw,    PrevRotPitch;        // obf: A/B

    // ── Bounding box (spec §2 / §7) ───────────────────────────────────────────

    /// <summary>
    /// obf: C — dedicated (non-pooled) AABB. Final reference, mutated in place.
    /// Initially zero-size at origin; sized by <see cref="SetSize"/> inside
    /// <see cref="EntityInit"/> (quirk 3).
    /// </summary>
    public readonly AxisAlignedBB BoundingBox = AxisAlignedBB.Create(0, 0, 0, 0, 0, 0);

    public float Width    = 0.6f;  // obf: M
    public float Height   = 1.8f;  // obf: N
    public float YOffset  = 0.0f;  // obf: L — feet below posY (positive = feet lower)
    public float YSize    = 0.0f;  // obf: U — vertical climbing expansion

    // ── Movement state flags (spec §2) ────────────────────────────────────────

    public  bool OnGround;               // obf: D
    public  bool HorizontalMoved;        // obf: E
    public  bool VerticalMoved;          // obf: F
    public  bool Moved;                  // obf: G = E || F
    public  bool IsCollidedHorizontally; // obf: H
    protected bool IsCollidedVertically; // obf: I
    public  bool NoClip = true;          // obf: J — false = zero-all-motion when blocked
    public  bool IsInWeb;                // obf: W

    // ── State flags ───────────────────────────────────────────────────────────

    public  bool IsDead;                 // obf: K
    protected bool IsImmuneToFire;       // obf: af
    protected bool IsInWater;            // obf: ab
    public  bool VelocityChanged;        // obf: ap
#pragma warning disable CS0414, CS0169
    private bool _firstUpdate = true;    // obf: d — true until end of first entityBaseTick (quirk 2)
    protected bool IsLiving;             // obf: l — set true by LivingEntity constructor
    private bool _unknownAo;            // obf: ao
#pragma warning restore CS0414, CS0169

    // ── Timers and counters ───────────────────────────────────────────────────

    public int  TicksExisted;            // obf: Z
    private int _fireTicks;              // obf: c (private) — positive = on fire
    public  int FireImmuneTicks = 1;     // obf: aa
    public  float FallDistance;          // obf: Q
    public  float PrevDistanceWalked;    // obf: O
    public  float DistanceWalked;        // obf: P
#pragma warning disable CS0414
    private int  _stepSoundTimer = 1;   // obf: b
#pragma warning restore CS0414
    public  float StepHeight;           // obf: V — auto-step max height (0=none, 0.5 for mobs)

    // ── Render ────────────────────────────────────────────────────────────────

    public double RenderDistanceWeight = 1.0; // obf: k
    public float  EntityCollisionReduction;   // obf: X

    // ── Mount / rider (spec §8) ───────────────────────────────────────────────

    public Entity? Mount; // obf: m — entity this entity is riding ON
    public Entity? Rider; // obf: n — entity riding ON TOP of this entity

    // ── Chunk tracking (spec §2) ──────────────────────────────────────────────

    public bool AddedToChunk; // obf: ah
    public int  ChunkCoordX;  // obf: ai
    public int  ChunkCoordY;  // obf: aj — entity bucket = floor(posY / 16)
    public int  ChunkCoordZ;  // obf: ak

    // ── DataWatcher (spec §6 / cr) ───────────────────────────────────────────

    // Index 0: entity flags byte (bits: 0=onFire,1=sneaking,2=riding,3=sprinting,4=eating)
    // Index 1: air supply short (default 300)
    protected readonly DataWatcher DataWatcher = new DataWatcher(); // obf: ag (cr)

    // ── Entity's own random ───────────────────────────────────────────────────

    protected readonly JavaRandom EntityRandom = new JavaRandom(); // obf: Y

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    /// <summary>
    /// Spec: <c>ia(ry world)</c>.
    /// Sets world, initialises AABB at origin (quirk 3), calls abstract <see cref="EntityInit"/>.
    /// </summary>
    protected Entity(World world)
    {
        EntityId = _nextEntityId++;
        World    = world;
        DataWatcher.Register(0, (byte)0);    // entity flags bit field
        DataWatcher.Register(1, (short)300); // air supply (default 300)
        SetPosition(0.0, 0.0, 0.0); // d(0,0,0) — sizes AABB from default M/N (quirk 3)
        EntityInit();
    }

    // ── Abstract hooks (spec §12) ─────────────────────────────────────────────

    /// <summary>
    /// obf: protected abstract <c>b()</c> — entityInit. Called from constructor.
    /// Subclasses call <see cref="SetSize"/> here to resize the bounding box.
    /// </summary>
    protected abstract void EntityInit();

    /// <summary>obf: protected abstract <c>b(ik)</c> — readEntityFromNBT. ik (NBT) spec pending.</summary>
    protected abstract void ReadEntityFromNBT(object nbt);

    /// <summary>obf: protected abstract <c>a(ik)</c> — writeEntityToNBT. ik (NBT) spec pending.</summary>
    protected abstract void WriteEntityToNBT(object nbt);

    // ── Core tick (spec §4) ───────────────────────────────────────────────────

    /// <summary>
    /// Main tick entry point. Default calls <see cref="EntityBaseTick"/>.
    /// Spec: <c>a()</c>.
    /// </summary>
    public virtual void Tick() => EntityBaseTick();

    /// <summary>
    /// Base per-tick logic called every tick. Spec: <c>w()</c>.
    /// Handles: dead-mount cleanup, ticksExisted, prevPos/prevRot copy, fire processing.
    /// Quirk 2: _firstUpdate set false at the END of this method.
    /// </summary>
    public void EntityBaseTick()
    {
        // Step 1: dismount dead mount
        if (Rider != null && Rider.IsDead) Rider = null;

        // Step 2: increment ticksExisted
        TicksExisted++;

        // Step 3: save previous state
        PrevDistanceWalked = DistanceWalked;
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;
        PrevRotPitch = RotationPitch; PrevRotYaw = RotationYaw;

        // Step 4: sprinting particle — stub (requires particle system)

        // Step 5: water check — stub (requires material overlap scan)
        IsInWater = false;

        // Step 6: fire tick processing (server-side only)
        if (World != null && !World.IsClientSide)
        {
            if (_fireTicks > 0)
            {
                if (IsImmuneToFire)
                    _fireTicks -= 4;
                else
                {
                    // Fire damage every 20 ticks — stub (DamageSource pm spec pending)
                    _fireTicks--;
                }
            }
        }

        // Step 7: lava check — stub
        // Step 8: void kill (posY < -64)
        if (PosY < -64.0)
        {
            Kill();
            return;
        }

        // Update DataWatcher flag bit 0 (isOnFire) and bit 2 (isRiding)
        SetFlagBit(0, _fireTicks > 0);
        SetFlagBit(2, Rider != null);

        // Quirk 2: firstUpdate cleared at END of method
        _firstUpdate = false;
    }

    /// <summary>
    /// Marks this entity as dead. Spec: <c>v()</c>.
    /// </summary>
    public void SetDead() => IsDead = true;

    /// <summary>Alias for <see cref="SetDead"/> used internally (falls into void, etc.).</summary>
    protected virtual void Kill() => SetDead();

    // ── Position / size (spec §11) ────────────────────────────────────────────

    /// <summary>
    /// Updates position AND resizes the bounding box around the new position.
    /// Always use this instead of setting PosX/PosY/PosZ directly.
    /// Spec: <c>d(double x, double y, double z)</c>.
    /// </summary>
    public void SetPosition(double x, double y, double z)
    {
        PosX = x; PosY = y; PosZ = z;
        float halfW = Width / 2.0f;
        BoundingBox.Set(
            x - halfW, y - YOffset + YSize,    z - halfW,
            x + halfW, y - YOffset + YSize + Height, z + halfW);
    }

    /// <summary>
    /// Sets size (width × height) for the bounding box. Called by <see cref="EntityInit"/>.
    /// Spec: protected <c>a(float width, float height)</c>.
    /// </summary>
    protected void SetSize(float width, float height)
    {
        Width  = width;
        Height = height;
    }

    /// <summary>
    /// Sets position and rotation, syncing last-tick position fields.
    /// Spec: <c>b(double x, double y, double z, float yaw, float pitch)</c>.
    /// </summary>
    public void SetLocationAndAngles(double x, double y, double z, float yaw, float pitch)
    {
        LastTickPosX = x; LastTickPosY = y; LastTickPosZ = z;
        RotationYaw = yaw; RotationPitch = pitch;
        YSize = 0.0f;
        SetPosition(x, y, z);
    }

    /// <summary>
    /// Sets all position fields (incl. prev) AND rotation. posY is offset by YOffset.
    /// Spec: <c>c(double x, double y, double z, float yaw, float pitch)</c>.
    /// </summary>
    public void SetPositionAndRotation(double x, double y, double z, float yaw, float pitch)
    {
        PrevPosX = LastTickPosX = PosX = x;
        PrevPosY = LastTickPosY = PosY = y + YOffset;
        PrevPosZ = LastTickPosZ = PosZ = z;
        RotationYaw = yaw; RotationPitch = pitch;
        SetPosition(x, y + YOffset, z);
    }

    // ── Movement (spec §5) ────────────────────────────────────────────────────

    /// <summary>
    /// Full sweep-collision movement. Spec: <c>b(double dx, double dy, double dz)</c>.
    ///
    /// Block collision is implemented. Entity-entity collision is stubbed
    /// (entity list query requires entity management spec).
    /// </summary>
    public virtual void Move(double dx, double dy, double dz)
    {
        if (World == null) return;

        // Step 1: web motion scaling (quirk 1)
        if (IsInWeb)
        {
            dx *= 0.25; dy *= 0.05; dz *= 0.25;
            MotionX = 0; MotionY = 0; MotionZ = 0;
        }

        double origDx = dx, origDy = dy, origDz = dz;

        // Step 3–4: get colliding block bounding boxes for expanded sweep volume
        var expanded = BoundingBox.Expand(dx, dy, dz);
        var colliders = World.GetCollidingBoundingBoxes(this, expanded);

        // Step 5: sweep Y
        foreach (var box in colliders) dy = box.CalculateYOffset(BoundingBox, dy);
        BoundingBox.OffsetInPlace(0, dy, 0);

        // Step 6: no-clip gate (quirk 1 for J == false)
        if (!NoClip && dy != origDy) { dx = 0; dy = 0; dz = 0; }

        // Step 7: sweep X
        foreach (var box in colliders) dx = box.CalculateXOffset(BoundingBox, dx);
        BoundingBox.OffsetInPlace(dx, 0, 0);

        // Step 8: no-clip gate
        if (!NoClip && dx != origDx) { dx = 0; dy = 0; dz = 0; }

        // Step 9: sweep Z
        foreach (var box in colliders) dz = box.CalculateZOffset(BoundingBox, dz);
        BoundingBox.OffsetInPlace(0, 0, dz);

        // Step 10: no-clip gate
        if (!NoClip && dz != origDz) { dx = 0; dy = 0; dz = 0; }

        // Step 11: step-assist (V > 0, blocked horizontally, on-ground or vertical-blocked)
        if (StepHeight > 0 && (OnGround || VerticalMoved) && (origDx != dx || origDz != dz))
        {
            var savedBox = BoundingBox.Copy();
            double savedDx = origDx, savedDz = origDz;

            // Reset and retry with step-up height
            BoundingBox.Set(
                BoundingBox.MinX - dx, BoundingBox.MinY - dy, BoundingBox.MinZ - dz,
                BoundingBox.MaxX - dx, BoundingBox.MaxY - dy, BoundingBox.MaxZ - dz);

            double stepDy = StepHeight;
            var sc2 = World.GetCollidingBoundingBoxes(this, BoundingBox.Expand(origDx, stepDy, origDz));
            foreach (var b in sc2) stepDy = b.CalculateYOffset(BoundingBox, stepDy);
            BoundingBox.OffsetInPlace(0, stepDy, 0);

            double stepDx = origDx;
            foreach (var b in sc2) stepDx = b.CalculateXOffset(BoundingBox, stepDx);
            BoundingBox.OffsetInPlace(stepDx, 0, 0);

            double stepDz = origDz;
            foreach (var b in sc2) stepDz = b.CalculateZOffset(BoundingBox, stepDz);
            BoundingBox.OffsetInPlace(0, 0, stepDz);

            // Step back down
            double downDy = -StepHeight;
            foreach (var b in sc2) downDy = b.CalculateYOffset(BoundingBox, downDy);
            BoundingBox.OffsetInPlace(0, downDy, 0);

            // Accept step if it gained more horizontal progress
            double newXZSq = stepDx * stepDx + stepDz * stepDz;
            double oldXZSq = savedDx * savedDx + savedDz * savedDz;
            if (newXZSq <= oldXZSq)
            {
                // Revert to pre-step state and keep original (dx,dy,dz)
                BoundingBox.Set(savedBox.MinX, savedBox.MinY, savedBox.MinZ,
                                savedBox.MaxX, savedBox.MaxY, savedBox.MaxZ);
            }
            else
            {
                YSize += (float)downDy; // accumulate vertical climb
            }
        }

        // Step 12: update position from AABB
        PosX = (BoundingBox.MinX + BoundingBox.MaxX) / 2.0;
        PosY = BoundingBox.MinY + YOffset - YSize;
        PosZ = (BoundingBox.MinZ + BoundingBox.MaxZ) / 2.0;

        // Step 13: flags
        HorizontalMoved = origDx != dx || origDz != dz;
        VerticalMoved   = origDy != dy;
        OnGround        = VerticalMoved && origDy < 0;
        Moved           = HorizontalMoved || VerticalMoved;
        IsCollidedHorizontally = HorizontalMoved;

        // Step 14: fall damage (stub — subclass implements)
        if (VerticalMoved) OnLanded(dy, OnGround);

        // Step 15: zero blocked velocity
        if (origDx != dx) MotionX = 0;
        if (origDy != dy) MotionY = 0;
        if (origDz != dz) MotionZ = 0;

        // Step 16–17: step sound, block notify — stub
    }

    /// <summary>
    /// Called after vertical movement resolves. Override for fall damage.
    /// Spec: <c>a(double dy, boolean onGround)</c>.
    /// </summary>
    protected virtual void OnLanded(double dy, bool onGround) { }

    // ── Velocity (spec §11) ───────────────────────────────────────────────────

    /// <summary>Adds to velocity and marks velocityChanged. Spec: <c>h(dx,dy,dz)</c>.</summary>
    public void AddVelocity(double dx, double dy, double dz)
    {
        MotionX += dx; MotionY += dy; MotionZ += dz;
        VelocityChanged = true;
    }

    // ── Mount / rider (spec §8) ───────────────────────────────────────────────

    /// <summary>obf: <c>g(ia)</c> — mountEntity. Establishes or dismounts rider link.</summary>
    public void MountEntity(Entity? target)
    {
        if (target == null || target == Rider)
        {
            // Dismount
            if (Rider != null)
            {
                Rider.Mount = null;
                SetPosition(PosX, PosY + Rider.Height * 0.75, PosZ);
            }
            Rider = null;
        }
        else
        {
            Rider = target;
            target.Mount = this;
        }
    }

    /// <summary>
    /// obf: <c>P()</c> — getRiderOffset: how high a rider sits above mount posY.
    /// Returns <c>Height × 0.75</c>.
    /// </summary>
    public virtual double GetRiderOffset() => Height * 0.75;

    /// <summary>obf: <c>O()</c> — getMountOffset: rider's own foot level = YOffset.</summary>
    public virtual double GetMountOffset() => YOffset;

    // ── Fire (spec §11) ───────────────────────────────────────────────────────

    /// <summary>Sets entity on fire for the given number of seconds. Spec: <c>e(int seconds)</c>.</summary>
    public void SetFire(int seconds)
    {
        int ticks = seconds * 20;
        if (_fireTicks < ticks) _fireTicks = ticks;
    }

    /// <summary>Extinguishes fire. Spec: <c>y()</c>.</summary>
    public void Extinguish() => _fireTicks = 0;

    /// <summary>True if entity is currently on fire. Spec: <c>V()</c> → bool.</summary>
    public bool IsOnFire() => _fireTicks > 0 || GetFlagBit(0);

    /// <summary>True if entity is alive (not dead). Spec: <c>K()</c> → bool.</summary>
    public virtual bool IsEntityAlive() => !IsDead;

    // ── Distance (spec §10) ───────────────────────────────────────────────────

    /// <summary>
    /// Euclidean distance to another entity. Float precision via MathHelper.SqrtDouble (quirk 5).
    /// Spec: <c>c(ia other)</c> → float.
    /// </summary>
    public float DistanceTo(Entity other)
    {
        double dx = PosX - other.PosX, dy = PosY - other.PosY, dz = PosZ - other.PosZ;
        return MathHelper.SqrtDouble(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>Squared distance to point. Spec: <c>f(double x, double y, double z)</c> → double.</summary>
    public double SquaredDistanceTo(double x, double y, double z)
    {
        double dx = PosX - x, dy = PosY - y, dz = PosZ - z;
        return dx * dx + dy * dy + dz * dz;
    }

    // ── Item drops (spec §9) ──────────────────────────────────────────────────

    // dropItem / dropItemStack: stub — requires EntityItem (ih) and ItemStack (dk) specs

    // ── DataWatcher flag helpers (spec §6) ────────────────────────────────────

    protected bool GetFlagBit(int bit)
    {
        return (DataWatcher.GetByte(0) & (1 << bit)) != 0;
    }

    protected void SetFlagBit(int bit, bool value)
    {
        // Quirk 7: read-modify-write (non-atomic)
        byte flags = DataWatcher.GetByte(0);
        if (value) flags |= (byte)(1 << bit);
        else       flags &= (byte)~(1 << bit);
        DataWatcher.UpdateObject(0, flags);
    }

    // ── toString ──────────────────────────────────────────────────────────────

    public override string ToString()
        => $"{GetType().Name}{{id={EntityId}, pos=({PosX:F1},{PosY:F1},{PosZ:F1})}}";
}
