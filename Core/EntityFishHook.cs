namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ael</c> (EntityFishHook) — fish hook projectile.
///
/// NOT registered in EntityList — has no string entity ID (spec §9.4).
/// This means hooks are lost on world save/reload.
///
/// Lifecycle:
///   1. Cast by rod: travels as projectile (0.6 blocks/tick initial speed).
///   2. Hits water: submersion check, fish-bite RNG (1/500 or 1/300 with clear sky).
///   3. Player reels in: ReelIn() returns durability cost, removes hook.
///
/// Quirks preserved (spec §9):
///   4. Not in EntityList — no NBT persistence.
///   6. Auto-removes if owner dead/dismounted or hook out of range (>32 blocks).
///   8. Fish bite downward force runs during countdown AND moment of bite.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md §5.6–5.8
/// </summary>
public sealed class EntityFishHook : Entity
{
    // ── Fields (spec §3.4) ───────────────────────────────────────────────────

    public int     XTile        = -1;    // obf: d
    public int     YTile        = -1;    // obf: e
    public int     ZTile        = -1;    // obf: f
    public int     InTileId     =  0;    // obf: g — block ID when stuck
    public bool    InGround     = false; // obf: h
    public int     Shake        =  0;    // obf: a (public)
    public EntityPlayer? Owner;          // obf: b
    public int     TicksInGround =  0;  // obf: i — despawn at 1200
    public int     TicksFlying   =  0;  // obf: aq
    public int     BobCountdown  =  0;  // obf: ar — ticks until/remaining on fish bite
    public Entity? HookedEntity;         // obf: c

    // ── Constants (spec §4.4) ────────────────────────────────────────────────

    private const float AirDrag    = 0.92f;
    private const float GroundDrag = 0.50f;
    private const float Buoyancy   = 0.04f; // coefficient per submersion fraction
    private const float WaterYDrag = 0.80f;
    private const int   BiteCooldownMin = 10;
    private const int   BiteCooldownMax = 30; // nextInt(30) + 10 → [10, 39]
    private const float BiteVelocity   = 0.20f;
    private const int   DespawnTicks   = 1200;
    private const float MaxDistSq      = 1024.0f; // 32 blocks squared

    // Water block IDs
    private const int WaterFlowing = 8;
    private const int WaterStill   = 9;

    // ── Constructor (spec §5.6) ──────────────────────────────────────────────

    public EntityFishHook(World world, EntityPlayer owner) : base(world)
    {
        Owner        = owner;
        owner.FishHook = this;
        SetSize(0.25f, 0.25f);

        // Starting position near owner eye
        float yawRad   =  owner.RotationYaw   * MathF.PI / 180.0f;
        float pitchRad = -owner.RotationPitch   * MathF.PI / 180.0f;

        double startX = owner.PosX - MathHelper.Sin(yawRad) * MathHelper.Cos(pitchRad) * 0.16;
        double startY = owner.PosY + owner.Height * 0.62 - 0.1;
        double startZ = owner.PosZ - MathHelper.Cos(yawRad) * MathHelper.Cos(pitchRad) * 0.16;
        SetPosition(startX, startY, startZ);

        // Initial velocity: direction × 0.4, then ×1.5 in setShootingVector
        double vx = -MathHelper.Sin(yawRad) * MathHelper.Cos(pitchRad);
        double vy =  MathHelper.Sin(pitchRad);
        double vz = -MathHelper.Cos(yawRad) * MathHelper.Cos(pitchRad);

        double len = Math.Sqrt(vx * vx + vy * vy + vz * vz);
        if (len > 0) { vx /= len; vy /= len; vz /= len; }

        vx += EntityRandom.NextGaussian() * 0.0075; // spread = 1.0
        vy += EntityRandom.NextGaussian() * 0.0075;
        vz += EntityRandom.NextGaussian() * 0.0075;

        // speed = 0.4 × 1.5 = 0.6
        MotionX = vx * 0.6; MotionY = vy * 0.6; MotionZ = vz * 0.6;

        float hz = MathF.Sqrt((float)(vx * vx + vz * vz));
        RotationYaw   = (float)(Math.Atan2(vx, vz) * 180.0 / Math.PI);
        RotationPitch = (float)(Math.Atan2(vy, hz)  * 180.0 / Math.PI);
        PrevRotYaw    = RotationYaw;
        PrevRotPitch  = RotationPitch;
    }

    public EntityFishHook(World world) : base(world) { SetSize(0.25f, 0.25f); }

    protected override void EntityInit() { }

    // ── Tick (spec §5.8) ────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;
        if (World.IsClientSide) return; // server-side only (spec §5.8)

        // ── Auto-remove check ──────────────────────────────────────────────

        bool ownerValid = Owner != null && Owner.IsEntityAlive();
        bool heldRod    = ownerValid && Owner!.Inventory.GetStackInSelectedSlot()?.GetItem() is Items.ItemFishingRod;
        double distSq   = ownerValid
            ? (PosX - Owner!.PosX) * (PosX - Owner.PosX)
            + (PosY - Owner.PosY) * (PosY - Owner.PosY)
            + (PosZ - Owner.PosZ) * (PosZ - Owner.PosZ) : MaxDistSq + 1;

        if (!ownerValid || !heldRod || distSq > MaxDistSq)
        {
            RemoveHook();
            return;
        }

        // ── Hooked entity tracking ─────────────────────────────────────────

        if (HookedEntity != null)
        {
            if (!HookedEntity.IsEntityAlive())
            {
                HookedEntity = null;
            }
            else
            {
                // Lock hook to entity position
                PosX = HookedEntity.PosX;
                PosY = HookedEntity.BoundingBox.MinY + HookedEntity.Height * 0.8;
                PosZ = HookedEntity.PosZ;
                return;
            }
        }

        // ── Stuck in block ─────────────────────────────────────────────────

        if (InGround)
        {
            int curId = World.GetBlockId(XTile, YTile, ZTile);
            if (curId == InTileId)
            {
                TicksInGround++;
                if (TicksInGround >= DespawnTicks) RemoveHook();
                return;
            }

            // Block changed: unstick
            InGround = false;
            MotionX *= EntityRandom.NextFloat() * 0.2;
            MotionY *= EntityRandom.NextFloat() * 0.2;
            MotionZ *= EntityRandom.NextFloat() * 0.2;
            TicksInGround = 0;
        }

        // ── In-flight physics ──────────────────────────────────────────────

        TicksFlying++;

        var conWorld = (World)World;
        var start    = Vec3.GetFromPool(PosX, PosY, PosZ);
        var end      = Vec3.GetFromPool(PosX + MotionX, PosY + MotionY, PosZ + MotionZ);

        // Block + entity ray-trace
        var blockHit  = conWorld.RayTraceBlocks(start, end);
        var scanBox   = BoundingBox.Copy().Expand(MotionX, MotionY, MotionZ).Expand(1.0, 1.0, 1.0);
        var nearby    = conWorld.GetEntitiesWithinAABBExcluding(this, scanBox);

        Entity?  hitEntity = null;
        double   hitDist   = double.MaxValue;
        foreach (var e in nearby)
        {
            if (!e.IsEntityAlive() || e == Owner) continue;
            var mop = e.BoundingBox.Copy().Expand(0.3, 0.3, 0.3).RayTrace(start, end);
            if (mop == null) continue;
            double d = start.DistanceTo(mop.HitVec);
            if (d < hitDist) { hitDist = d; hitEntity = e; }
        }

        if (hitEntity != null &&
            (blockHit == null || hitDist < start.DistanceTo(blockHit.HitVec)))
        {
            // 0 damage — just attaches (spec §5.8)
            hitEntity.AttackEntityFrom(DamageSource.Thrown(this, Owner), 0);
            HookedEntity = hitEntity;
        }
        else if (blockHit != null)
        {
            XTile    = blockHit.BlockX;
            YTile    = blockHit.BlockY;
            ZTile    = blockHit.BlockZ;
            InTileId = World.GetBlockId(XTile, YTile, ZTile);
            InGround = true;
            MotionX  = blockHit.HitVec.X - PosX;
            MotionY  = blockHit.HitVec.Y - PosY;
            MotionZ  = blockHit.HitVec.Z - PosZ;
        }

        // ── Water submersion (spec §5.8 step 2) ───────────────────────────

        float submersion = ComputeSubmersion();

        // ── Fish bite logic (spec §5.8 step 3) ────────────────────────────

        if (submersion > 0.0f)
        {
            if (BobCountdown > 0)
            {
                BobCountdown--;
                // Extra downward force during countdown AND at moment of bite
                MotionY -= EntityRandom.NextFloat()
                         * EntityRandom.NextFloat()
                         * EntityRandom.NextFloat()
                         * 0.2;
            }
            else
            {
                // Check for bite
                int rollMax = CanSeeSkyAtHook() ? 300 : 500;
                if (EntityRandom.NextInt(rollMax) == 0)
                {
                    BobCountdown = EntityRandom.NextInt(BiteCooldownMax) + BiteCooldownMin;
                    MotionY     -= BiteVelocity;
                    // Sound + particle stub: "random.splash"
                    conWorld.PlaySoundAt(this, "random.splash", 1.0f, 0.4f / (EntityRandom.NextFloat() * 0.4f + 0.8f));
                }
            }
        }

        // ── Buoyancy + drag (spec §5.8 step 4) ────────────────────────────

        if (submersion > 0.0f)
        {
            float buoyancy = submersion * 2.0f - 1.0f;
            MotionY += Buoyancy * buoyancy;
            MotionY *= WaterYDrag;
        }

        float drag = (OnGround || IsCollidedHorizontally) ? GroundDrag : AirDrag;
        MotionX *= drag; MotionY *= drag; MotionZ *= drag;

        if (!InGround)
        {
            PosX += MotionX; PosY += MotionY; PosZ += MotionZ;
            SetPosition(PosX, PosY, PosZ);
        }
    }

    // ── ReelIn — g() (spec §5.7) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>ael.g()</c> — reels in the hook. Returns rod durability cost.
    /// </summary>
    public int ReelIn()
    {
        int durability = 0;

        if (HookedEntity != null && Owner != null)
        {
            // Pull entity toward player
            double ex = Owner.PosX - PosX;
            double ey = Owner.PosY - PosY;
            double ez = Owner.PosZ - PosZ;
            double dist = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            HookedEntity.MotionX += ex * 0.1;
            HookedEntity.MotionY += ey * 0.1 + Math.Sqrt(dist) * 0.08;
            HookedEntity.MotionZ += ez * 0.1;
            durability = 3;
        }
        else if (BobCountdown > 0 && Owner != null)
        {
            // Fish on the line — spawn raw fish
            double ex = Owner.PosX - PosX;
            double ey = Owner.PosY - PosY;
            double ez = Owner.PosZ - PosZ;
            double dist = Math.Sqrt(ex * ex + ey * ey + ez * ez);

            var fishStack  = new ItemStack(349, 1); // Raw Fish
            var fishEntity = new EntityItem(World!, PosX, PosY, PosZ, fishStack);
            fishEntity.MotionX = ex * 0.1;
            fishEntity.MotionY = ey * 0.1 + Math.Sqrt(dist) * 0.08;
            fishEntity.MotionZ = ez * 0.1;
            World!.SpawnEntity(fishEntity);
            durability = 1;
        }

        if (InGround) durability = 2; // overrides fish/entity if stuck (spec §7.3 note)

        RemoveHook();
        return durability;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RemoveHook()
    {
        IsDead = true;
        if (Owner != null) Owner.FishHook = null;
    }

    private float ComputeSubmersion()
    {
        if (World == null) return 0f;
        float height  = (float)(BoundingBox.MaxY - BoundingBox.MinY);
        float total   = 5f;
        int   wetCount = 0;

        for (int i = 0; i < (int)total; i++)
        {
            float sampleY = (float)BoundingBox.MinY + height * (i / total);
            int bx = (int)Math.Floor(PosX);
            int by = (int)Math.Floor(sampleY);
            int bz = (int)Math.Floor(PosZ);
            int id = World.GetBlockId(bx, by, bz);
            if (id == WaterFlowing || id == WaterStill) wetCount++;
        }

        return wetCount / total;
    }

    private bool CanSeeSkyAtHook()
    {
        if (World is not World conWorld) return false;
        int hx = (int)Math.Floor(PosX);
        int hz = (int)Math.Floor(PosZ);
        int hy = (int)Math.Floor(PosY);
        return conWorld.GetHeightValue(hx, hz) <= hy;
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
