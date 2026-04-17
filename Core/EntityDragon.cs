using System.Collections.Generic;

namespace SpectraEngine.Core;

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>adh</c> (EntityBoss) — thin LivingEntity wrapper that makes the boss
/// immune to all direct damage. Subclasses bypass the immunity via <see cref="ApplyDamageDirect"/>.
///
/// Spec §2:
///   - <c>bI</c> = maxHealth field (default 100; dragon sets 200).
///   - <c>a(pm, int)</c> always returns false — boss is immune.
///   - <c>e(pm, int)</c> = super.a(pm, int) — bypass entry point for subclasses.
///   - <c>a(vc, pm, int)</c> = default routes to blocked <c>a</c> — override in EntityDragon.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnderDragon_Spec.md §2
/// </summary>
public abstract class EntityBoss : LivingEntity
{
    // ── Max-health field (spec §2) ────────────────────────────────────────────

    protected int BossMaxHealth = 100; // obf: bI

    protected EntityBoss(World world) : base(world) { }

    // ── Immunity override ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(pm, int)</c> — always returns false; boss is immune to all direct damage.
    /// Subclasses must call <see cref="ApplyDamageDirect"/> to actually hurt the boss.
    /// </summary>
    public override bool AttackEntityFrom(DamageSource source, int amount) => false;

    /// <summary>
    /// obf: <c>e(pm, int)</c> — bypass: calls base LivingEntity damage pipeline directly.
    /// Call this from subclass damage routing to bypass the immunity.
    /// </summary>
    protected bool ApplyDamageDirect(DamageSource source, int amount)
        => base.AttackEntityFrom(source, amount);

    /// <summary>
    /// obf: <c>a(vc bodyPart, pm source, int damage)</c> — default implementation just
    /// calls the blocked <c>a(pm, int)</c> (i.e. no damage). Override in EntityDragon.
    /// </summary>
    public virtual bool OnBodyPartHit(EntityBodyPart bodyPart, DamageSource source, int amount)
        => AttackEntityFrom(source, amount); // blocked → false

    public override int GetMaxHealth() => BossMaxHealth;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>oo</c> (EntityDragon) — the Ender Dragon boss entity.
/// EntityList name "EnderDragon", ID 63.
///
/// Key systems:
///   - 7 <see cref="EntityBodyPart"/> proxies for hit detection (head, body, tail×3, wings×2).
///   - 64-entry ring buffer for trailing body-part positioning.
///   - Flying AI: target tracking, yaw steering, forward thrust, drag.
///   - Crystal healing / damage routing (§5).
///   - 200-tick death sequence with 20 000 XP and exit portal generation (§8/§9).
///   - Block destruction whitelist: only obsidian (49), bedrock (7), end portal (119) survive.
///
/// Quirks / open questions preserved in comments:
///   - §12.1 <c>az()</c> is dead code — declared but has no side effects.
///   - §13.2 <c>af</c> field (isMultipartEntity?) set true in constructor.
///   - §13.3 <c>aQ</c> (probably noClip state) gates wing/head collision.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnderDragon_Spec.md
/// </summary>
public sealed class EntityDragon : EntityBoss
{
    // ── Body part array (spec §3.3) ───────────────────────────────────────────

    /// <summary>obf: <c>f[]</c> — all 7 body parts.</summary>
    public readonly EntityBodyPart[] Parts;     // obf: f

    /// <summary>obf: <c>g</c> — head (6×6).</summary>
    public readonly EntityBodyPart Head;        // obf: g
    /// <summary>obf: <c>h</c> — body (8×8).</summary>
    public readonly EntityBodyPart Body;        // obf: h
    /// <summary>obf: <c>i</c> — tail segment 1 (4×4).</summary>
    public readonly EntityBodyPart Tail1;       // obf: i
    /// <summary>obf: <c>by</c> — tail segment 2 (4×4).</summary>
    public readonly EntityBodyPart Tail2;       // obf: by
    /// <summary>obf: <c>bz</c> — tail segment 3 (4×4).</summary>
    public readonly EntityBodyPart Tail3;       // obf: bz
    /// <summary>obf: <c>bA</c> — left wing (4×4).</summary>
    public readonly EntityBodyPart WingLeft;    // obf: bA
    /// <summary>obf: <c>bB</c> — right wing (4×4).</summary>
    public readonly EntityBodyPart WingRight;   // obf: bB

    // ── Targeting / waypoint fields (spec §3.2) ───────────────────────────────

    private double _targetX = 0.0;     // obf: a
    private double _targetY = 100.0;   // obf: b
    private double _targetZ = 0.0;     // obf: c

    // ── Ring buffer (spec §3.2 / §4.2) ───────────────────────────────────────

    /// <summary>obf: <c>d[64][3]</c> — trailing yaw/Y ring buffer.</summary>
    private readonly double[,] _ringBuffer = new double[64, 3]; // obf: d
    /// <summary>obf: <c>e</c> — current write index; -1 = not yet initialised.</summary>
    private int _ringIndex = -1;       // obf: e

    // ── Wing-flap animation (spec §3.2) ──────────────────────────────────────

    public float PrevFlapAmount;       // obf: bC
    public float FlapAmount;           // obf: bD

    // ── State flags (spec §3.2) ───────────────────────────────────────────────

    private bool _isStuck;             // obf: bE — triggers new waypoint pick
    private bool _inBlock;             // obf: bF — head or body touching solid

    // ── AI targeting (spec §3.2) ──────────────────────────────────────────────

    private Entity? _targetEntity;     // obf: bJ — current target player (or null)

    // ── Death sequence (spec §3.2 / §8) ──────────────────────────────────────

    private int _deathTick;            // obf: bG

    // ── Crystal healing (spec §3.2 / §5) ─────────────────────────────────────

    private EntityEnderCrystal? _focusedCrystal; // obf: bH

    // ── Constructor ───────────────────────────────────────────────────────────

    public EntityDragon(World world) : base(world)
    {
        BossMaxHealth = 200;
        SetSize(16.0f, 8.0f);
        IsImmuneToFire = true;   // obf: W = true
        NoClip         = true;   // explosion immune equivalent; dragon has no clip

        // §13.2: af = true (isMultipartEntity flag; no direct C# equivalent yet)

        Head      = new EntityBodyPart(world, this, "head",  6.0f, 6.0f);
        Body      = new EntityBodyPart(world, this, "body",  8.0f, 8.0f);
        Tail1     = new EntityBodyPart(world, this, "tail",  4.0f, 4.0f);
        Tail2     = new EntityBodyPart(world, this, "tail",  4.0f, 4.0f);
        Tail3     = new EntityBodyPart(world, this, "tail",  4.0f, 4.0f);
        WingLeft  = new EntityBodyPart(world, this, "wing",  4.0f, 4.0f);
        WingRight = new EntityBodyPart(world, this, "wing",  4.0f, 4.0f);

        Parts = [Head, Body, Tail1, Tail2, Tail3, WingLeft, WingRight];
    }

    protected override void EntityInit() { }

    // ── Tick (spec §4) ───────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;
        var w = (World)World;

        // ── Dead state (spec §4.1) ────────────────────────────────────────────
        if (Health <= 0)
        {
            DeathSequence(w);

            // Spawn random "largeexplode" particle (stub — particle system not yet impl)
            // world.spawnParticle("largeexplode", ...)
            return;
        }

        // ── Alive state ───────────────────────────────────────────────────────

        // a) Wing flap animation (spec §4.2 a)
        PrevFlapAmount = FlapAmount;
        double horizSpeed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        double wingSpeed  = 0.2 / (horizSpeed * 10.0 + 1.0) * Math.Pow(2.0, MotionY);
        if (_inBlock)
            FlapAmount += (float)(wingSpeed * 0.5);
        else
            FlapAmount += (float)wingSpeed;

        // b) Ring buffer update (spec §4.2 b)
        if (_ringIndex < 0)
        {
            for (int i = 0; i < 64; i++)
            {
                _ringBuffer[i, 0] = RotationYaw;
                _ringBuffer[i, 1] = PosY;
            }
        }
        _ringIndex = (_ringIndex + 1) % 64;
        _ringBuffer[_ringIndex, 0] = RotationYaw;
        _ringBuffer[_ringIndex, 1] = PosY;

        // c) Client-side interpolation handled externally (not applicable server-only)

        // d) Server AI (spec §4.2 d)
        UpdateAI(w);

        // e) Body part AABB positioning (spec §4.3)
        UpdateBodyPartPositions();

        // f) Block destruction (spec §4.4)
        _inBlock = DestroyBlocksInAABB(w, Head.BoundingBox)
                 | DestroyBlocksInAABB(w, Body.BoundingBox);

        // g) Entity collision (spec §4.5 — guarded by aQ == 0)
        PushEntitiesFromWings(w);
        DamageEntitiesAtHead(w);

        // h) Crystal healing (spec §5)
        TickCrystalHealing(w);
    }

    // ── AI (spec §4.2 d) ─────────────────────────────────────────────────────

    private void UpdateAI(World w)
    {
        // Target update
        if (_targetEntity != null)
        {
            _targetX = _targetEntity.PosX;
            _targetZ = _targetEntity.PosZ;
            double dxT = _targetEntity.PosX - PosX;
            double dzT = _targetEntity.PosZ - PosZ;
            double dist = Math.Sqrt(dxT * dxT + dzT * dzT);
            double aboveOffset = Math.Min(0.4 + dist / 80.0 - 1.0, 10.0);
            _targetY = _targetEntity.BoundingBox.MinY + aboveOffset;
        }
        else
        {
            _targetX += EntityRandom.NextGaussian() * 2.0;
            _targetZ += EntityRandom.NextGaussian() * 2.0;
        }

        // Waypoint refresh trigger (spec §4.2 d)
        double dx = _targetX - PosX;
        double dy = _targetY - PosY;
        double dz = _targetZ - PosZ;
        double distSq = dx * dx + dy * dy + dz * dz;

        if (_isStuck || distSq < 100.0 || distSq > 22500.0 || IsInWater)
            PickNewWaypoint(w);

        // Y-axis velocity (spec §4.2 d)
        double horizDist = Math.Sqrt((_targetX - PosX) * (_targetX - PosX)
                                   + (_targetZ - PosZ) * (_targetZ - PosZ));
        if (horizDist > 0)
        {
            double yAdj = (_targetY - PosY) / horizDist;
            yAdj = Math.Max(-0.6, Math.Min(0.6, yAdj));
            MotionY += yAdj * 0.1;
        }

        // Yaw steering (spec §4.2 d)
        double targetYaw = 180.0 - Math.Atan2(dx, dz) * (180.0 / Math.PI);
        double deltaYaw  = targetYaw - RotationYaw;
        // Normalise to [-180, +180]
        while (deltaYaw >  180.0) deltaYaw -= 360.0;
        while (deltaYaw < -180.0) deltaYaw += 360.0;
        deltaYaw = Math.Max(-50.0, Math.Min(50.0, deltaYaw));

        double speed = Math.Sqrt(MotionX * MotionX + MotionZ * MotionZ);
        double var19 = Math.Min(speed + 1.0, 40.0);
        AiTurnRate  += (float)(deltaYaw * 0.7 / var19 / (speed + 1.0));
        RotationYaw += AiTurnRate * 0.1f;

        // Forward thrust (spec §4.2 d)
        float yawRad = RotationYaw * MathF.PI / 180.0f;
        double fwdX = -MathHelper.Sin(yawRad);
        double fwdZ = -MathHelper.Cos(yawRad);
        // forwardDir = (fwdX, MotionY, fwdZ) — but spec uses normalize(sin(y), w, -cos(y))
        double fLen = Math.Sqrt(fwdX * fwdX + MotionY * MotionY + fwdZ * fwdZ);
        if (fLen > 0) { fwdX /= fLen; /* MotionY component already unit */ fwdZ /= fLen; }

        double toTX = _targetX - PosX, toTY = _targetY - PosY, toTZ = _targetZ - PosZ;
        double toTLen = Math.Sqrt(toTX * toTX + toTY * toTY + toTZ * toTZ);
        if (toTLen > 0) { toTX /= toTLen; toTY /= toTLen; toTZ /= toTLen; }

        double alignment = fwdX * toTX + MotionY / (fLen > 0 ? fLen : 1) * toTY + fwdZ * toTZ;
        double var17     = Math.Max(0.0, Math.Min(1.0, (alignment + 0.5) / 1.5));

        double baseThrust   = 0.06;
        double speedFactor  = 2.0 / (speed + 1.0);
        double thrustMag    = baseThrust * (var17 * speedFactor + (1.0 - speedFactor));

        MotionX += fwdX * thrustMag;
        MotionZ += fwdZ * thrustMag;

        // Drag (spec §4.2 d)
        if (_inBlock)
        {
            MotionX *= 0.8; MotionY *= 0.8; MotionZ *= 0.8;
        }
        else
        {
            double velLen = Math.Sqrt(MotionX * MotionX + MotionY * MotionY + MotionZ * MotionZ);
            if (velLen > 0)
            {
                double dotFwd = (MotionX * fwdX + MotionZ * fwdZ) / velLen;
                double speedMult = 0.8 + 0.15 * (dotFwd + 1.0) / 2.0;
                MotionX *= speedMult;
                MotionZ *= speedMult;
            }
            MotionY *= 0.91;
        }

        // Apply velocity
        PosX += MotionX;
        PosY += MotionY;
        PosZ += MotionZ;
        SetPosition(PosX, PosY, PosZ);
    }

    // ── New waypoint (spec §7) ────────────────────────────────────────────────

    private void PickNewWaypoint(World w)
    {
        _isStuck = false;

        var players = w.GetPlayerList();
        if (EntityRandom.NextInt(2) == 0 && players.Count > 0)
        {
            _targetEntity = players[EntityRandom.NextInt(players.Count)];
        }
        else
        {
            _targetEntity = null;
            double ax, ay, az;
            do
            {
                ax = 0.0 + EntityRandom.NextFloat() * 120.0 - 60.0;
                ay = 70.0 + EntityRandom.NextFloat() * 50.0;
                az = 0.0 + EntityRandom.NextFloat() * 120.0 - 60.0;
                double ddx = PosX - ax, ddy = PosY - ay, ddz = PosZ - az;
            }
            while ((PosX - ax) * (PosX - ax)
                 + (PosY - ay) * (PosY - ay)
                 + (PosZ - az) * (PosZ - az) <= 100.0);

            _targetX = 0.0 + EntityRandom.NextFloat() * 120.0 - 60.0;
            _targetY = 70.0 + EntityRandom.NextFloat() * 50.0;
            _targetZ = 0.0 + EntityRandom.NextFloat() * 120.0 - 60.0;
        }
    }

    // ── Body part positioning (spec §4.3) ────────────────────────────────────

    private void UpdateBodyPartPositions()
    {
        float yawRad = RotationYaw * MathF.PI / 180.0f;

        // Body: 0.5 blocks behind the dragon along its yaw
        SetPartPosition(Body,  5.0f, 3.0f,
            PosX + MathHelper.Sin(yawRad)  * 0.5,
            PosY,
            PosZ - MathHelper.Cos(yawRad) * 0.5);

        // Wings
        SetPartPosition(WingLeft,  4.0f, 2.0f,
            PosX + MathHelper.Sin(yawRad)  * 4.5,
            PosY + 2.0,
            PosZ + MathHelper.Cos(yawRad) * 4.5);

        SetPartPosition(WingRight, 4.0f, 3.0f,
            PosX - MathHelper.Sin(yawRad)  * 4.5,
            PosY + 2.0,
            PosZ - MathHelper.Cos(yawRad) * 4.5);

        // Head: uses ring buffer entries 0 and 5 for neck pitch (spec §4.3)
        int  idx0     = (_ringIndex - 0 + 64) % 64;
        int  idx5     = (_ringIndex - 5 + 64) % 64;
        int  idx10    = (_ringIndex - 10 + 64) % 64;
        double pitchRaw = (_ringBuffer[idx5, 1] - _ringBuffer[idx10, 1]) * 10.0 * Math.PI / 180.0;
        double pitchRad = Math.Atan(pitchRaw);
        double cosPitch = Math.Cos(pitchRad);
        double sinPitch = -Math.Sin(pitchRad);
        float  yawAdj   = (RotationYaw - AiTurnRate * 0.01f) * MathF.PI / 180.0f;

        SetPartPosition(Head, 3.0f, 3.0f,
            PosX + MathHelper.Sin(yawAdj)  * 5.5 * cosPitch,
            PosY + (_ringBuffer[idx0, 1] - _ringBuffer[idx5, 1]) + sinPitch * 5.5,
            PosZ - MathHelper.Cos(yawAdj) * 5.5 * cosPitch);

        // Tail segments: ring buffer positions 12, 14, 16 (spec §4.3)
        PositionTailSegment(Tail1, 12, 14);
        PositionTailSegment(Tail2, 14, 16);
        PositionTailSegment(Tail3, 16, 18);
    }

    private void PositionTailSegment(EntityBodyPart part, int bufIdx, int refIdx)
    {
        int  ai  = (_ringIndex - bufIdx + 64) % 64;
        int  ri  = (_ringIndex - refIdx + 64) % 64;
        double tailYaw = Math.Atan2(
            _ringBuffer[ri, 1] - _ringBuffer[ai, 1],
            _ringBuffer[ri, 0] - _ringBuffer[ai, 0]);
        float ty = (float)(tailYaw * 180.0 / Math.PI);
        float yr = ty * MathF.PI / 180.0f;

        SetPartPosition(part, 2.0f, 2.0f,
            PosX + MathHelper.Sin(yr)  * bufIdx * 0.5,
            _ringBuffer[ai, 1],
            PosZ - MathHelper.Cos(yr) * bufIdx * 0.5);
    }

    private static void SetPartPosition(EntityBodyPart part, float w, float h,
        double x, double y, double z)
    {
        part.Width  = w;
        part.Height = h;
        part.BoundingBox.SetBB(AxisAlignedBB.GetFromPool(
            x - w / 2.0, y,         z - w / 2.0,
            x + w / 2.0, y + h,     z + w / 2.0));
        part.PosX = x; part.PosY = y; part.PosZ = z;
    }

    // ── Block destruction (spec §4.4) ─────────────────────────────────────────

    private static bool DestroyBlocksInAABB(World w, AxisAlignedBB aabb)
    {
        int x0 = (int)Math.Floor(aabb.MinX), x1 = (int)Math.Ceiling(aabb.MaxX);
        int y0 = (int)Math.Floor(aabb.MinY), y1 = (int)Math.Ceiling(aabb.MaxY);
        int z0 = (int)Math.Floor(aabb.MinZ), z1 = (int)Math.Ceiling(aabb.MaxZ);

        bool hitIndestructible = false;

        for (int x = x0; x <= x1; x++)
        for (int y = y0; y <= y1; y++)
        for (int z = z0; z <= z1; z++)
        {
            int id = w.GetBlockId(x, y, z);
            if (id == 0) continue;

            // Whitelist: obsidian (49), bedrock (7), end portal (119) — indestructible
            if (id == 49 || id == 7 || id == 119)
            {
                hitIndestructible = true;
                continue;
            }

            w.SetBlock(x, y, z, 0);
            // particle "largeexplode" stub (not yet implemented)
        }

        return hitIndestructible;
    }

    // ── Entity collision (spec §4.5) ──────────────────────────────────────────

    private void PushEntitiesFromWings(World w)
    {
        // Inflate wing AABBs and push living entities outward (spec §4.5)
        PushFromAABB(w, WingLeft.BoundingBox,  inflateXZ: 4.0, inflateY: 2.0, shiftY: -2.0);
        PushFromAABB(w, WingRight.BoundingBox, inflateXZ: 4.0, inflateY: 2.0, shiftY: -2.0);
    }

    private void PushFromAABB(World w, AxisAlignedBB partBox,
        double inflateXZ, double inflateY, double shiftY)
    {
        var scanBox = AxisAlignedBB.GetFromPool(
            partBox.MinX - inflateXZ, partBox.MinY - inflateY + shiftY,
            partBox.MinZ - inflateXZ,
            partBox.MaxX + inflateXZ, partBox.MaxY + inflateY + shiftY,
            partBox.MaxZ + inflateXZ);

        double cx = (partBox.MinX + partBox.MaxX) * 0.5;
        double cz = (partBox.MinZ + partBox.MaxZ) * 0.5;

        var entities = w.GetEntitiesWithinAABB<LivingEntity>(scanBox);
        foreach (var e in entities)
        {
            if (e == this) continue;

            double edx    = e.PosX - cx;
            double edz    = e.PosZ - cz;
            double distSq = edx * edx + edz * edz;
            if (distSq < 0.0001) distSq = 0.0001;

            e.MotionX += edx / distSq * 4.0;
            e.MotionY += 0.2;
            e.MotionZ += edz / distSq * 4.0;
        }
    }

    private void DamageEntitiesAtHead(World w)
    {
        var scanBox = AxisAlignedBB.GetFromPool(
            Head.BoundingBox.MinX - 1, Head.BoundingBox.MinY - 1,
            Head.BoundingBox.MinZ - 1,
            Head.BoundingBox.MaxX + 1, Head.BoundingBox.MaxY + 1,
            Head.BoundingBox.MaxZ + 1);

        var entities = w.GetEntitiesWithinAABB<LivingEntity>(scanBox);
        foreach (var e in entities)
        {
            if (e == this) continue;
            e.AttackEntityFrom(DamageSource.MobAttack(this), 10);
        }
    }

    // ── Crystal healing (spec §5) ─────────────────────────────────────────────

    private void TickCrystalHealing(World w)
    {
        // Every 10 ticks: scan for nearest crystal within 32 blocks
        if (EntityRandom.NextInt(10) == 0)
        {
            var scanBox = AxisAlignedBB.GetFromPool(
                PosX - 32, PosY - 32, PosZ - 32,
                PosX + 32, PosY + 32, PosZ + 32);

            var crystals = w.GetEntitiesWithinAABB<EntityEnderCrystal>(scanBox);
            EntityEnderCrystal? nearest = null;
            double nearestDist = double.MaxValue;
            foreach (var c in crystals)
            {
                double dx = c.PosX - PosX, dy = c.PosY - PosY, dz = c.PosZ - PosZ;
                double d2 = dx * dx + dy * dy + dz * dz;
                if (d2 < nearestDist) { nearestDist = d2; nearest = c; }
            }
            _focusedCrystal = nearest;
        }

        if (_focusedCrystal != null && !_focusedCrystal.IsDead)
        {
            if (TicksExisted % 10 == 0 && Health < GetMaxHealth())
                Health++;
        }
        else if (_focusedCrystal != null && _focusedCrystal.IsDead)
        {
            // Focused crystal was destroyed — take 10 damage (spec §5)
            OnBodyPartHit(Head, DamageSource.Magic, 10);
            _focusedCrystal = null;
        }
    }

    // ── Damage routing (spec §6) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(vc bodyPart, pm source, int damage)</c> — routes body-part hits.
    /// Non-head hits are quartered (min 1). Only player and fire sources bypass immunity.
    /// </summary>
    public override bool OnBodyPartHit(EntityBodyPart bodyPart, DamageSource source, int amount)
    {
        if (bodyPart != Head)
            amount = amount / 4 + 1;

        // Redirect waypoint toward random point near head
        float yawRad = RotationYaw * MathF.PI / 180.0f;
        _targetX = PosX + MathHelper.Sin(yawRad) * 5.0
                       + (EntityRandom.NextFloat() * 2.0 - 1.0);
        _targetY = PosY + EntityRandom.NextFloat() * 3.0 + 1.0;
        _targetZ = PosZ - MathHelper.Cos(yawRad) * 5.0
                       + (EntityRandom.NextFloat() * 2.0 - 1.0);
        _targetEntity = null;

        // Only player attacks and fire bypass the boss immunity
        bool playerAttack = source.GetAttacker() is EntityPlayer;
        bool fireSource   = source.IsFireDamage;

        if (playerAttack || fireSource)
            return ApplyDamageDirect(source, amount);

        return false;
    }

    // ── Death sequence (spec §8) ──────────────────────────────────────────────

    private void DeathSequence(World w)
    {
        _deathTick++;

        // Particles in ticks 180–200 (stub — particle system not yet implemented)

        // XP orbs: ticks > 150, every 5 ticks — batches of 1000 XP (spec §8)
        if (_deathTick > 150 && _deathTick % 5 == 0)
            SpawnXpBatch(w, 1000);

        // Upward drift and spin
        MotionY += 0.1;
        RotationYaw += 20.0f;
        BodyYaw = RotationYaw;

        // Final tick 200
        if (_deathTick == 200)
        {
            SpawnXpBatch(w, 10000);           // bonus final XP batch
            GenerateExitPortal(w, (int)Math.Floor(PosX), (int)Math.Floor(PosZ));
            IsDead = true;
        }
    }

    private static void SpawnXpBatch(World w, int totalXp)
    {
        // Use EntityXPOrb.RoundDownToTier to split into correct orb sizes
        while (totalXp > 0)
        {
            int orbVal = EntityXPOrb.RoundDownToTier(totalXp);
            if (orbVal <= 0) orbVal = 1;
            totalXp -= orbVal;
            w.SpawnEntity(new EntityXPOrb(w,
                w.SpawnX + (w.Random.NextFloat() * 2.0 - 1.0) * 3.0,
                w.SpawnY + 2.0,
                w.SpawnZ + (w.Random.NextFloat() * 2.0 - 1.0) * 3.0,
                orbVal));
        }
    }

    // ── Exit portal generator (spec §9) ──────────────────────────────────────

    private static void GenerateExitPortal(World w, int x, int z)
    {
        // BlockEndPortal static flag to suppress entity teleport during construction
        // Blocks.BlockEndPortal.StaticActive = true — stub (flag not yet on BlockEndPortal)

        int var3 = World.WorldHeight / 2; // = 64

        // Circular disc of end portal + bedrock ring at Y=64, clear column above
        for (int dy = var3 - 1; dy <= var3 + 32; dy++)
        for (int dx = -4; dx <= 4; dx++)
        for (int dz = -4; dz <= 4; dz++)
        {
            double radius = Math.Sqrt(dx * dx + dz * dz);
            if (radius > 3.5) continue;

            if (dy < var3)          // Y = 63: inner bedrock disc (radius ≤ 2.5)
            {
                if (radius <= 2.5) w.SetBlock(x + dx, dy, z + dz, 7); // bedrock
            }
            else if (dy == var3)    // Y = 64: portal disc + bedrock ring
            {
                if (radius <= 2.5) w.SetBlock(x + dx, dy, z + dz, 119); // end portal
                else                w.SetBlock(x + dx, dy, z + dz, 7);   // bedrock ring
            }
            else                    // Y > 64: clear column
            {
                w.SetBlock(x + dx, dy, z + dz, 0); // air
            }
        }

        // Fixed center pillar with torches and dragon egg (spec §9)
        w.SetBlock(x, var3,     z, 7);   // bedrock
        w.SetBlock(x, var3 + 1, z, 7);   // bedrock
        w.SetBlock(x, var3 + 2, z, 7);   // bedrock
        w.SetBlock(x - 1, var3 + 2, z, 50); // torch
        w.SetBlock(x + 1, var3 + 2, z, 50); // torch
        w.SetBlock(x, var3 + 2, z - 1, 50); // torch
        w.SetBlock(x, var3 + 2, z + 1, 50); // torch
        w.SetBlock(x, var3 + 3, z, 7);   // bedrock
        w.SetBlock(x, var3 + 4, z, 122); // dragon egg

        // BlockEndPortal.StaticActive = false — restore
    }

    // ── Dead-code stub (spec §12.1) ───────────────────────────────────────────

    // obf: az() — called every 20 ticks when alive. Declares variables but performs
    // no side effects. Stub preserved for RNG-state parity; body intentionally empty.
    private void AzDeadCode() { }

    // ── Body parts for World entity list (spec §3.4) ──────────────────────────

    /// <summary>
    /// obf: <c>ab()</c> — returns the 7 body parts so the world entity list can track them.
    /// </summary>
    public EntityBodyPart[] GetParts() => Parts;

    // ── NBT (dragon does not save per-session state, so stubs only) ───────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
