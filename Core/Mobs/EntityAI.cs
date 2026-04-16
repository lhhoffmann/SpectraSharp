using SpectraEngine.Core.AI;

namespace SpectraEngine.Core.Mobs;

/// <summary>
/// Replica of <c>ww</c> (EntityAI) — abstract base for all AI-driven mobs.
/// Extends <see cref="LivingEntity"/> with pathfinding, target management,
/// stroll (random wander), and the core AI tick <c>n()</c>.
///
/// Fields (spec §3):
///   a  = current path (PathEntity)
///   h  = current target (Entity)
///   i  = isAngry / in-attack-range flag
///   by = panic/speed-doubling timer
///
/// AI tick sequence each server tick (spec §8):
///   1. Decrement panic timer.
///   2. Update isAngry flag via az().
///   3. Target management: acquire / expire / clear dead target.
///   4. If target: attack or approach based on isInRange().
///   5. Path following (skip 1% of ticks via nextInt(100)==0).
///   6. Stroll (random wander) when no target and conditions met.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §8
/// </summary>
public abstract class EntityAI : LivingEntity
{
    // ── Fields (spec §3) ─────────────────────────────────────────────────────

    /// <summary>obf: a — current active path. Null when idle or path is exhausted.</summary>
    protected PathEntity? AiPath;           // obf: a

    /// <summary>obf: h — current AI target entity. Null when idle.</summary>
    protected Entity? AiTarget;             // obf: h

    /// <summary>obf: i — isAngry / in-attack-range flag; set each tick by az().</summary>
    protected bool IsAngry;                 // obf: i

    /// <summary>obf: by — panic/anger countdown. Doubles move speed while > 0.</summary>
    protected int PanicTimer;               // obf: by

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityAI(World world) : base(world) { }

    // ── Move speed with panic multiplier (spec §8 aw()) ──────────────────────

    /// <summary>
    /// obf: <c>aw()</c> — base speed, doubled while <see cref="PanicTimer"/> > 0 (spec §8).
    /// </summary>
    protected virtual float GetMoveSpeedMultiplier()
    {
        float speed = PushbackWidth; // bw = base speed (0.7 default)
        if (PanicTimer > 0) speed *= 2.0f;
        return speed;
    }

    // ── Virtual AI hooks ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>o()</c> — return the preferred target entity, or null if none.
    /// Overridden by EntityMonster (nearest player) and EntityAnimal (food/breed).
    /// </summary>
    protected virtual Entity? GetAITarget() => null;

    /// <summary>
    /// obf: <c>i(ia target)</c> — returns true if <paramref name="target"/> is within
    /// the attack/interaction range. Used to decide attack vs. approach each tick.
    /// Overridden by EntityMonster (melee distance) and EntityAnimal (breed distance).
    /// Default: false (never in range).
    /// </summary>
    protected virtual bool IsInRange(Entity target) => false;

    /// <summary>
    /// obf: <c>az()</c> — returns true when the mob is in the "angry" / in-attack-range
    /// state (controls yaw-steering during path following). Default: false.
    /// </summary>
    protected virtual bool IsInAttackState() => false;

    /// <summary>
    /// obf: <c>a(ia target, float dist)</c> — called when target is within range.
    /// Overridden in EntityMonster (melee attack) and EntityAnimal (breed / face-toward).
    /// </summary>
    protected virtual void OnTargetInRange(Entity target, float dist) { }

    /// <summary>
    /// obf: <c>b(ia target, float dist)</c> — called when target is outside range.
    /// Overridden in EntityMonster (no-op) and EntityAnimal (flee: empty in 1.0).
    /// </summary>
    protected virtual void OnTargetOutOfRange(Entity target, float dist) { }

    /// <summary>
    /// obf: <c>a(int x, int y, int z)</c> — position desirability score used by stroll.
    /// Default: 0.0. Overridden by EntityMonster (prefer dark) and EntityAnimal (prefer grass/light).
    /// </summary>
    protected virtual float GetPositionScore(int x, int y, int z) => 0.0f;

    // ── Core AI tick (spec §8 n()) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>n()</c> — full AI tick, called server-side each tick.
    /// </summary>
    protected void RunAITick()
    {
        if (World == null || World.IsClientSide) return;

        // Step 1: Decrement panic timer
        if (PanicTimer > 0) PanicTimer--;

        // Step 2: Update isAngry flag
        IsAngry = IsInAttackState();

        // Step 3: Target management
        const float SearchRange = 16.0f;

        if (AiTarget == null)
        {
            AiTarget = GetAITarget();
            if (AiTarget != null)
                AiPath = World.GetPathToEntity(this, AiTarget, SearchRange);
        }
        else if (AiTarget.IsDead)
        {
            AiTarget = null;
        }

        // Step 4: Attack or approach
        if (AiTarget != null)
        {
            double dx  = AiTarget.PosX - PosX;
            double dy  = AiTarget.PosY - PosY;
            double dz  = AiTarget.PosZ - PosZ;
            float  dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (IsInRange(AiTarget))
                OnTargetInRange(AiTarget, dist);
            else
                OnTargetOutOfRange(AiTarget, dist);
        }

        // Step 5: Path following (skip 1% of ticks)
        if (AiPath != null && EntityRandom.NextInt(100) != 0)
        {
            int currentY = (int)Math.Floor(BoundingBox.MinY + 0.5);

            Vec3? waypoint = AiPath.GetCurrentWaypoint(this);

            // Skip waypoints already within 2*Width radius (XZ distance)
            while (waypoint != null)
            {
                double wpDx = waypoint.X - PosX;
                double wpDz = waypoint.Z - PosZ;
                double dist2d2 = wpDx * wpDx + wpDz * wpDz;
                double threshold = Width * 2;
                if (dist2d2 >= threshold * threshold) break;

                AiPath.Advance();
                if (AiPath.IsDone)
                {
                    waypoint = null;
                    AiPath   = null;
                    break;
                }
                waypoint = AiPath.GetCurrentWaypoint(this);
            }

            WantsToJump = false;

            if (waypoint != null)
            {
                double wpDx = waypoint.X - PosX;
                double wpDz = waypoint.Z - PosZ;
                double wpDy = waypoint.Y - currentY;

                // Rotate toward waypoint (clamped ±30°)
                float targetYaw = (float)(Math.Atan2(wpDz, wpDx) * (180.0 / Math.PI)) - 90.0f;
                float yawDelta  = NormalizeAngle(targetYaw - RotationYaw);
                yawDelta        = Math.Clamp(yawDelta, -30.0f, 30.0f);
                RotationYaw    += yawDelta;

                // If angry: face the target entity directly and compute strafe
                if (IsAngry && AiTarget != null)
                {
                    double tdx   = AiTarget.PosX - PosX;
                    double tdz   = AiTarget.PosZ - PosZ;
                    float  savedYaw = RotationYaw;
                    RotationYaw  = (float)(Math.Atan2(tdz, tdx) * (180.0 / Math.PI)) - 90.0f;
                    float  angleDiff = (float)((savedYaw - RotationYaw + 90.0f) * Math.PI / 180.0f);
                    AiStrafe  = (float)(-Math.Sin(angleDiff) * PushbackWidth);
                    AiForward = (float)( Math.Cos(angleDiff) * PushbackWidth);
                }

                // Jump up to reach a higher waypoint
                if (wpDy > 0.0) WantsToJump = true;
            }

            // Look at target
            if (AiTarget != null)
                LookAt(AiTarget, 30.0f, 30.0f);

            // Jump if horizontally blocked
            if (IsCollidedHorizontally)
                WantsToJump = true;

            // Jump in water/lava with 80% chance
            bool inLava = World?.GetBlockMaterial(
                (int)Math.Floor(PosX), (int)Math.Floor(PosY), (int)Math.Floor(PosZ))
                == Material.Lava_;
            if (EntityRandom.NextFloat() < 0.8f && (IsInWater || inLava))
                WantsToJump = true;
        }
        else if (AiPath != null)
        {
            // Skipped this tick — clear stale path if it's done
            AiPath = null;
        }

        // Step 6: Wander (stroll) when no target or occasional refresh
        bool shouldStroll = IsAngry
            || (AiTarget == null
                && (AiPath == null  && EntityRandom.NextInt(180) == 0
                    || EntityRandom.NextInt(120) == 0
                    || PanicTimer > 0));

        if (shouldStroll && !IsAngry && NaturalDespawnTicker < 100)
            Stroll();
    }

    // ── Stroll (random wander) (spec §8 aA()) ────────────────────────────────

    /// <summary>
    /// obf: <c>aA()</c> — tries 10 random nearby positions, picks the best-scoring one,
    /// and requests a path to it (range 10). Spec §8.
    /// </summary>
    protected void Stroll()
    {
        float bestScore = -99999f;
        int   bestX = -1, bestY = -1, bestZ = -1;
        bool  found = false;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            int rx = (int)Math.Floor(PosX) + EntityRandom.NextInt(13) - 6;
            int ry = (int)Math.Floor(PosY) + EntityRandom.NextInt(7)  - 3;
            int rz = (int)Math.Floor(PosZ) + EntityRandom.NextInt(13) - 6;

            float score = GetPositionScore(rx, ry, rz);
            if (score > bestScore)
            {
                bestScore = score;
                bestX = rx; bestY = ry; bestZ = rz;
                found = true;
            }
        }

        if (found)
            AiPath = World?.GetPathToCoords(this, bestX, bestY, bestZ, 10.0f);
    }

    // ── Look-at helper (spec §8 a(ia, float, float)) ─────────────────────────

    /// <summary>
    /// obf: <c>a(ia target, float maxYawChange, float maxPitchChange)</c> — rotates
    /// the entity's yaw/pitch toward a target entity, clamped per tick.
    /// </summary>
    protected void LookAt(Entity target, float maxYawChange, float maxPitchChange)
    {
        double dx  = target.PosX - PosX;
        double dy  = target.PosY + target.Height * 0.5 - (PosY + Height * 0.5);
        double dz  = target.PosZ - PosZ;

        double horizDist = Math.Sqrt(dx * dx + dz * dz);
        float  targetYaw   = (float)(Math.Atan2(dz, dx) * (180.0 / Math.PI)) - 90.0f;
        float  targetPitch = (float)-(Math.Atan2(dy, horizDist) * (180.0 / Math.PI));

        RotationYaw   = NormalizeAngle(RotationYaw
            + Math.Clamp(NormalizeAngle(targetYaw   - RotationYaw),   -maxYawChange,   maxYawChange));
        RotationPitch = RotationPitch
            + Math.Clamp(targetPitch - RotationPitch, -maxPitchChange, maxPitchChange);
    }

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick();
        if (!NoAI && World != null && !World.IsClientSide)
            RunAITick();
    }

    // ── NBT ───────────────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) => base.WriteEntityToNBT(tag);
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) => base.ReadEntityFromNBT(tag);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)  angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
