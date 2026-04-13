namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>nq</c> (LivingEntity) — abstract base for all entities that have health,
/// can be damaged, play sounds, run basic AI, and participate in combat.
///
/// Extends <see cref="Entity"/> (<c>ia</c>) with:
///   health, invulnerability window, armor absorption, knockback, potion effects,
///   fall damage, limb animation, and a friction-aware movement method.
///
/// Quirks preserved (see spec §16):
///   1. SetHealth does NOT clamp aM — clamping branch writes to local only (possible vanilla bug).
///   2. Invulnerability filter: damage &lt;= LastDamage is rejected; only delta above LastDamage applies.
///   3. Armor absorption carries a remainder in ArmorRemainder across hits.
///   4. Friction constant 0.54600006F = 0.6F × 0.91F at compile time. Block is read twice per tick.
///   5. KnockbackAngle on receiver (aR on this) is separate from knockback applied to attacker.
///   6. PotionEffect particles spawn on only 50% of ticks (random.nextBoolean()).
///   7. DataWatcher index 8 is an int (full RGB), not a byte.
///
/// Open stubs (specs pending):
///   - DamageSource (pm): AttackEntityFrom is skeletal.
///   - PotionEffect (abg, s): effect system is a no-op stub.
///   - CreatureAttribute (el): GetCreatureAttribute returns default.
///   - ExperienceOrb (fk): XP drops are not spawned.
///   - AI / pathfinding: WanderingAI and DespawnCheck are stubs.
///   - Sound system: sound playback calls are no-ops.
///   - Player (vi): GetNearestPlayer / lastAttacker references typed as object.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/LivingEntity_Spec.md
/// </summary>
public abstract class LivingEntity : Entity
{
    // ── Health / combat fields (spec §2) ─────────────────────────────────────

    protected int Health;               // obf: aM — current health, init from GetMaxHealth()
    public    int PrevHealth;           // obf: aN — health before last damage (damage flash)
    protected int ArmorRemainder;       // obf: aO — armor absorption carry (quirk 3)
    public    int HurtTime;            // obf: aP — countdown: set to 10 on hit
    public    int HurtDuration = 10;   // obf: aQ — value written to HurtTime on hit
    public    int InvulnerabilityTicks = 20; // obf: aq — full invulnerability period
    public    float KnockbackAngle;    // obf: aR — degrees (set on hit)
    public    int DeathTime;           // obf: aS — increments each tick when dead; remove at 20
    public    int AttackCooldown;      // obf: aT — attack cooldown counter
    protected bool DiedFired;          // obf: aW — death callback has fired
    protected int LastDamage;          // obf: bp — last hit amount (invulnerability compare)
    protected int NaturalDespawnTicker;// obf: bq — ticks since near player (natural despawn)
    protected int XpDropAmount;        // obf: aX — XP value dropped on death
    protected int LootingBonus;        // obf: aF — looting bonus to pass to killer
    public    int ArrowCount = -1;     // obf: aY — arrows embedded (visual)

    // ── Movement / physics fields (spec §2) ──────────────────────────────────

    public    float GroundSpeed = 0.1f; // obf: aI — ground movement speed multiplier
    public    float AirSpeed    = 0.02f;// obf: aJ — air movement speed multiplier
    protected float AiLookPitch;        // obf: bv — AI wander look pitch
    protected float PushbackWidth = 0.7f; // obf: bw — pushback overlap width factor
    protected float AiForward;          // obf: br — AI movement input: forward
    protected float AiStrafe;           // obf: bs — AI movement input: strafe
    protected float AiTurnRate;         // obf: bt — AI turn rate
    protected bool  WantsToJump;        // obf: bu — AI jump request
    private   int   _jumpCooldown;      // obf: d  — ticks between jumps (max 10)

    // ── Animation / rendering fields (spec §2) ────────────────────────────────

    public  float WalkAnimPos;          // obf: ar — walking animation position (frame)
    public  float WalkAnimSpeed;        // obf: as — animation speed multiplier
    public  float BodyYaw;             // obf: at — rendered torso heading
    public  float PrevBodyYaw;         // obf: au — previous body yaw
    protected float PrevLimbSwing;     // obf: av
    protected float LimbSwing;         // obf: aw
    protected float LimbDistance;      // obf: ax — accumulated limb distance
    protected float PrevLimbDistance;  // obf: ay
    public  float PrevLimbDistPub;     // obf: aK
    public  float LimbDistPub;         // obf: aL
    public  float PrevSwingProgress;   // obf: ba
    public  float SwingProgress;       // obf: bb — arm swing towards target
    public  float LimbRotation;        // obf: bc
    public  float ChildScaleRandom;    // obf: aZ — random scale for child mob rendering
    public  float PrevHealthBar;       // obf: aU
    public  float HealthBar;           // obf: aV

    // ── AI / targeting fields (spec §2) ──────────────────────────────────────

    protected string  TexturePath = "/mob/char.png"; // obf: aA
    public    bool    NoAI;                          // obf: aH — skip AI tick when true
    protected bool    HasAI = true;                  // obf: az
    protected float   AnimSpeedFactor = 1.0f;        // obf: aE
    protected object? LastAttacker;                  // obf: bd — last player to attack (vi)
    protected int     LastAttackerTicks;             // obf: be — countdown to clear LastAttacker
    public    int     FireResistTicks;               // obf: bf
    public    int     FireResistRender;              // obf: bg
#pragma warning disable CS0169
    private   Entity? _gazeTarget;                  // obf: e  — wander look target
    private   int     _gazeTicks;                   // obf: bx — ticks to keep gazing
#pragma warning restore CS0169

    // ── Client interpolation fields (spec §2) ────────────────────────────────

    protected int    InterpSteps;      // obf: bi
    protected double InterpX, InterpY, InterpZ;   // obf: bj/bk/bl
    protected double InterpYaw, InterpPitch;       // obf: bm/bn

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    protected LivingEntity(World world) : base(world)
    {
        // base(world) calls EntityInit() → registers DataWatcher index 8
        Health       = GetMaxHealth();
        IsLiving     = true;                                    // obf: l = true
        WalkAnimSpeed = (float)(EntityRandom.NextDouble() + 1.0) * 0.01f;
        SetPosition(PosX, PosY, PosZ);                         // recalculate AABB
        WalkAnimPos  = EntityRandom.NextFloat() * 12398.0f;
        RotationYaw  = EntityRandom.NextFloat() * MathF.PI * 2.0f;
        StepHeight   = 0.5f;                                    // obf: V = 0.5F
        ChildScaleRandom = (float)EntityRandom.NextDouble() * 0.9f + 0.1f;
    }

    // ── EntityInit override (spec §4) ─────────────────────────────────────────

    /// <summary>
    /// Registers DataWatcher index 8 (potion color int). Spec: <c>nq.b()</c>.
    /// Concrete subclasses MUST call base.EntityInit() before adding their own entries.
    /// </summary>
    protected override void EntityInit()
    {
        DataWatcher.Register(8, 0); // potion effect packed RGB color (int, typeId 2, quirk 7)
    }

    // ── Abstract: GetMaxHealth (spec §5) ─────────────────────────────────────

    /// <summary>
    /// obf: abstract <c>f_()</c> — returns this entity's maximum health.
    /// MUST be implemented by every concrete subclass.
    /// </summary>
    protected abstract int GetMaxHealth();

    // ── Health system (spec §6) ───────────────────────────────────────────────

    /// <summary>obf: <c>ag()</c> → current health.</summary>
    public int GetHealth() => Health;

    /// <summary>
    /// obf: <c>h(int val)</c> — direct health assignment.
    /// Quirk 1: clamping branch writes to local variable only — aM is NOT clamped.
    /// </summary>
    public void SetHealth(int val)
    {
        Health = val;
        int clampedCheck = val > GetMaxHealth() ? GetMaxHealth() : val; // local only (quirk 1)
        _ = clampedCheck; // suppress warning — intentional no-op per spec quirk 1
    }

    /// <summary>
    /// obf: <c>a_(int amount)</c> — heal. Clamped to maxHealth. Resets part of invulnerability.
    /// </summary>
    public void Heal(int amount)
    {
        if (Health <= 0) return;
        Health += amount;
        if (Health > GetMaxHealth()) Health = GetMaxHealth();
        // ac = aq / 2 — reset half the invulnerability counter
        // (Entity field 'ac' not yet mapped; skip for now — stub)
    }

    /// <summary>
    /// obf: <c>K()</c> override — isAlive = !isDead &amp;&amp; health > 0.
    /// </summary>
    public override bool IsEntityAlive() => !IsDead && Health > 0;

    /// <summary>
    /// obf: <c>H()</c> — canBePushed = !isDead.
    /// </summary>
    public virtual bool CanBePushed() => !IsDead;

    // ── Damage (spec §6) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(pm source, int amount)</c> — attackEntityFrom (apply damage).
    /// Handles invulnerability window (quirk 2), hurtTime, knockback, death.
    /// Stub: DamageSource (pm) typed as object; potion checks and sound skipped.
    /// </summary>
    public virtual bool AttackEntityFrom(object damageSource, int amount)
    {
        if (World != null && World.IsClientSide) return false;
        NaturalDespawnTicker = 0;
        if (Health <= 0) return false;

        SwingProgress = 1.5f; // arm swing

        int ac = 0; // TODO: map Entity field 'ac' (invulnerability counter)

        if (ac > InvulnerabilityTicks / 2)
        {
            if (amount <= LastDamage) return false;
            ApplyDamage(damageSource, amount - LastDamage);
            LastDamage = amount;
        }
        else
        {
            LastDamage = amount;
            PrevHealth = Health;
            // ac = InvulnerabilityTicks; — TODO
            ApplyDamage(damageSource, amount);
            HurtTime = HurtDuration = 10;
        }

        KnockbackAngle = 0.0f;

        if (Health <= 0) OnDeath(damageSource);

        return true;
    }

    /// <summary>
    /// obf: protected <c>b(pm source, int amount)</c> — applyDamage (inner).
    /// Applies armor absorption then subtracts from health.
    /// </summary>
    protected virtual void ApplyDamage(object damageSource, int amount)
    {
        amount = AbsorbArmor(damageSource, amount);
        Health -= amount;
    }

    /// <summary>
    /// obf: <c>c(pm source, int amount)</c> — armor absorption (quirk 3: carries remainder).
    /// Base: GetTotalArmorValue() = 0, so armor factor = 25 / 25 = full damage.
    /// </summary>
    protected virtual int AbsorbArmor(object damageSource, int amount)
    {
        int armorValue = GetTotalArmorValue(); // default 0
        int var3       = 25 - armorValue;
        int accumulated = amount * var3 + ArmorRemainder;
        OnArmorDamaged(amount); // hook for subclass (e.g. damage armor items)
        amount       = accumulated / 25;
        ArmorRemainder = accumulated % 25; // carry forward (quirk 3)
        return amount;
    }

    /// <summary>
    /// obf: <c>o_()</c> — getTotalArmorValue. 0 = unarmored base.
    /// </summary>
    protected virtual int GetTotalArmorValue() => 0;

    /// <summary>
    /// obf: <c>i(int)</c> — called in armor absorption for subclass to damage armor items.
    /// No-op base.
    /// </summary>
    protected virtual void OnArmorDamaged(int amount) { }

    /// <summary>
    /// obf: <c>a(pm source)</c> — onDeath callback. Called when health reaches 0.
    /// Drops items (XP orbs and loot). Stub: XP orb spawn (fk) pending.
    /// </summary>
    protected virtual void OnDeath(object damageSource)
    {
        if (DiedFired) return;
        DiedFired = true;
        // TODO: loot drop logic — requires item drop tables and DamageSource.GetAttacker()
        // TODO: XP orb spawn (fk spec pending)
    }

    // ── Knockback (spec §7) ───────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(ia target, int damage, double dx, double dz)</c> — apply knockback to this entity.
    /// </summary>
    public void ApplyKnockback(Entity attacker, int damage, double dx, double dz)
    {
        VelocityChanged = true;
        double dist = Math.Sqrt(dx * dx + dz * dz);
        if (dist == 0) return;
        const float strength = 0.4f;
        MotionX /= 2.0; MotionY /= 2.0; MotionZ /= 2.0;
        MotionX -= dx / dist * strength;
        MotionY += 0.4;
        MotionZ -= dz / dist * strength;
        if (MotionY > 0.4) MotionY = 0.4;
    }

    // ── Kill override (spec §13) ──────────────────────────────────────────────

    /// <summary>
    /// Called when health &lt;= 0 and DeathTime == 20. Drops loot and calls SetDead.
    /// </summary>
    protected override void Kill()
    {
        if (World != null && !World.IsClientSide)
        {
            DropItems(LastAttacker != null, 0); // looting=0 stub
        }
        SetDead();
    }

    /// <summary>
    /// obf: protected <c>a(boolean playerKill, int looting)</c> — drop items.
    /// XP drops: stub (fk/ExperienceOrb spec pending).
    /// </summary>
    protected virtual void DropItems(bool playerKilled, int looting) { }

    // ── Movement (spec §8) ────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>d(float fwd, float strafe)</c> — livingMove.
    /// Applies AI input with proper per-surface friction. Quirk 4: friction read twice per tick.
    /// </summary>
    public void LivingMove(float forward, float strafe)
    {
        if (World == null) return;

        // ── Water / lava path ────────────────────────────────────────────────
        if (IsInWater)
        {
            ApplyInputAcceleration(forward, strafe, 0.02f);
            Move(MotionX, MotionY, MotionZ);
            MotionX *= 0.8; MotionY *= 0.8; MotionZ *= 0.8;
            MotionY -= 0.02;
            return;
        }

        // ── Ground / air path ────────────────────────────────────────────────
        float friction = GetGroundFriction();

        // Speed factor from friction: 0.16277136 / (f³) (spec §8)
        float speedFactor = 0.16277136f / (friction * friction * friction);
        float inputScale  = OnGround ? GroundSpeed * speedFactor : AirSpeed;

        ApplyInputAcceleration(forward, strafe, inputScale);
        Move(MotionX, MotionY, MotionZ);

        // Second friction lookup after move (quirk 4: duplicate block read)
        friction = GetGroundFriction();

        // Climbing check (ladder, vine)
        if (IsOnLadder())
        {
            MotionX = Math.Clamp(MotionX, -0.15, 0.15);
            MotionZ = Math.Clamp(MotionZ, -0.15, 0.15);
            FallDistance = 0.0f;
            if (MotionY < -0.15) MotionY = -0.15;
            if (OnGround && MotionY < 0.0) MotionY = 0.0;
        }

        MotionY -= 0.08;
        MotionY *= 0.98;
        MotionX *= friction;
        MotionZ *= friction;
    }

    /// <summary>
    /// obf: <c>ak()</c> — jump. Sets upward impulse; sprint adds horizontal boost.
    /// </summary>
    protected void Jump()
    {
        MotionY = 0.42;
        // Jump Boost potion: stub (abg spec pending)
        // Sprint boost: stub (isSprinting check pending)
        VelocityChanged = true;
    }

    /// <summary>
    /// obf: <c>a(float fwd, float strafe, float speed)</c> — apply directional acceleration.
    /// </summary>
    protected void ApplyInputAcceleration(float forward, float strafe, float speed)
    {
        float dist = forward * forward + strafe * strafe;
        if (dist < 0.01f) return;
        dist = speed / (float)Math.Sqrt(dist);
        forward *= dist;
        strafe  *= dist;
        float sinYaw = MathHelper.Sin(RotationYaw * MathF.PI / 180.0f);
        float cosYaw = MathHelper.Cos(RotationYaw * MathF.PI / 180.0f);
        MotionX += forward * cosYaw - strafe * sinYaw;
        MotionZ += forward * sinYaw + strafe * cosYaw;
    }

    /// <summary>
    /// Returns the friction value for the block directly below the entity's feet.
    /// Spec §8: if on ground, use Block.SlipperinessMap[blockBelow] * 0.91F; else 0.91F.
    /// Quirk 4: 0.54600006F = 0.6F * 0.91F (compile-time constant for default block).
    /// </summary>
    private float GetGroundFriction()
    {
        if (!OnGround || World == null) return 0.91f;
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(BoundingBox.MinY) - 1;
        int bz = (int)Math.Floor(PosZ);
        int blockBelow = World.GetBlockId(bx, by, bz);
        if (blockBelow > 0)
            return Block.SlipperinessMap[blockBelow] * 0.91f;
        return 0.54600006f; // 0.6F * 0.91F — quirk 4
    }

    /// <summary>
    /// obf: <c>ah()</c> — isOnLadder (climbable block check).
    /// </summary>
    protected virtual bool IsOnLadder()
    {
        if (World == null) return false;
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(BoundingBox.MinY);
        int bz = (int)Math.Floor(PosZ);
        int id = World.GetBlockId(bx, by, bz);
        // Block.Ladder is registered later; compare by ID 65 (ladder block ID in 1.0)
        return id == 65;
    }

    // ── Main tick (spec §9) ───────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a()</c> override — living entity main tick.
    /// Calls base tick, then handles AI, movement, death counter, potion effects.
    /// </summary>
    public override void Tick()
    {
        EntityBaseTick(); // fire, void, prevPos, ticksExisted

        if (IsDead) return;

        // Step 1: fire resistance countdown
        if (FireResistTicks  > 0) FireResistTicks--;
        if (FireResistRender > 0) FireResistRender--;

        // Step 2: client interpolation — stub (network spec pending)

        // Step 3: countdown timers
        if (AttackCooldown > 0) AttackCooldown--;
        if (HurtTime      > 0) HurtTime--;
        if (LastAttackerTicks > 0)
        {
            LastAttackerTicks--;
            if (LastAttackerTicks == 0) LastAttacker = null;
        }

        // Step 4: death counter
        if (Health <= 0)
        {
            DeathTime++;
            if (DeathTime == 20)
            {
                Kill();
            }
            return;
        }

        // Step 5: potion effects — stub (abg/s spec pending)

        // Step 6: AI input decay
        AiForward  *= 0.98f;
        AiStrafe   *= 0.98f;
        AiTurnRate *= 0.9f;

        // Step 7: AI tick
        if (!NoAI && HasAI)
        {
            // Step 4 (spec): jump trigger
            if (WantsToJump && OnGround && _jumpCooldown == 0)
            {
                Jump();
                _jumpCooldown = 10;
            }
            if (_jumpCooldown > 0) _jumpCooldown--;

            WanderingAI(); // base wandering
        }

        // Step 8: movement with friction
        LivingMove(AiForward, AiStrafe);

        // Step 9: entity-push — stub (entity list management pending)

        // Step 10: limb animation update
        PrevLimbDistPub = LimbDistPub;
        PrevBodyYaw     = BodyYaw;
        PrevSwingProgress = SwingProgress;
        if (SwingProgress > 0) SwingProgress -= 0.4f;
        if (SwingProgress < 0) SwingProgress = 0;

        PrevHealthBar = HealthBar;
        HealthBar     = Health;
    }

    // ── Wandering AI (spec §10) — stub ───────────────────────────────────────

    /// <summary>
    /// obf: protected <c>n()</c> — base wandering AI. Stub: world.getNearestPlayer pending.
    /// </summary>
    protected virtual void WanderingAI()
    {
        NaturalDespawnTicker++;
        // Full AI: look for nearest player, random turn, proximity despawn
        // Stub until Player (vi) and World.GetNearestPlayer are available
    }

    // ── Fall damage (spec §12) ────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>c(float distance)</c> — onLanded override. Applies fall damage.
    /// Threshold: 3.0 blocks. Formula: ceil(height − 3). 4-block fall → 1 damage.
    /// </summary>
    protected override void OnLanded(double dy, bool onGround)
    {
        float distance = FallDistance;
        int damage = (int)Math.Ceiling(distance - 3.0f);
        if (damage > 0)
        {
            AttackEntityFrom(null!, damage); // DamageSource.fall — stub null
        }
    }

    // ── Eye height (spec §15) ─────────────────────────────────────────────────

    /// <summary>obf: <c>E()</c> — getEyeHeight = Height * 0.85F.</summary>
    public float GetEyeHeight() => Height * 0.85f;

    // ── Virtual surface (spec §15) ────────────────────────────────────────────

    /// <summary>obf: <c>k()</c> — getXPValue. Base returns 0; mobs override.</summary>
    public virtual int GetXPValue() => 0;

    /// <summary>obf: <c>f()</c> sound — getHurtSound. Base returns "damage.hurtflesh".</summary>
    public virtual string GetHurtSound() => "damage.hurtflesh";

    /// <summary>obf: <c>g()</c> sound — getDeathSound. Base returns "damage.hurtflesh".</summary>
    public virtual string GetDeathSound() => "damage.hurtflesh";

    /// <summary>obf: <c>e()</c> sound — getAmbientSound. Base returns null (silent).</summary>
    public virtual string? GetAmbientSound() => null;

    /// <summary>obf: <c>w_()</c> — getSoundVolume. Base returns 1.0F.</summary>
    public virtual float GetSoundVolume() => 1.0f;

    /// <summary>obf: <c>s()</c> — getEquippedItem (held item). Base returns null.</summary>
    public virtual ItemStack? GetEquippedItem() => null;

    /// <summary>obf: <c>q_()</c> — isSpecialMob (boss etc., suppresses natural despawn). Base false.</summary>
    public virtual bool IsSpecialMob() => false;

    /// <summary>obf: <c>p_()</c> — getAmbientSoundInterval. Base returns 80 ticks.</summary>
    public virtual int GetAmbientSoundInterval() => 80;

    /// <summary>obf: <c>d()</c> — canDespawnNaturally. Base returns true; player overrides to false.</summary>
    public virtual bool CanDespawnNaturally() => true;

    // ── NBT hooks (stubs — ik spec pending) ──────────────────────────────────

    protected override void ReadEntityFromNBT(object nbt)  { /* ik spec pending */ }
    protected override void WriteEntityToNBT(object nbt)   { /* ik spec pending */ }
}
