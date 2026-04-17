namespace SpectraEngine.Core;

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
///   - PotionHelper (pk): metadata decode is stubbed — GetEffectsFromMeta returns empty list.
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
    /// <summary>
    /// obf: <c>aE</c> — pendingKnockback counter.
    /// Incremented when an attack lands; applied and reset on the next tick.
    /// </summary>
    public int PendingKnockback;       // obf: aE
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

    // ── Active potion effects (spec: abg/s) ───────────────────────────────────

    /// <summary>Active potion effects keyed by effect ID (spec: abg.a[]).</summary>
    private readonly Dictionary<int, PotionEffect> _activeEffects = new();
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
    public abstract int GetMaxHealth();

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
        InvulnerabilityCountdown = InvulnerabilityTicks / 2; // ac = aq/2
    }

    /// <summary>obf: <c>aM</c> accessor — current health (read-only for external callers).</summary>
    public int GetCurrentHealth() => Health;

    // ── Active effects (spec: nq / abg / s) ──────────────────────────────────

    /// <summary>
    /// obf: <c>nq.a(s effect)</c> — apply or combine a potion effect.
    /// Combines with existing effect if already active (spec §5.3).
    /// </summary>
    public void AddPotionEffect(PotionEffect effect)
    {
        if (_activeEffects.TryGetValue(effect.EffectId, out var existing))
            _activeEffects[effect.EffectId] = existing.Combine(effect);
        else
            _activeEffects[effect.EffectId] = effect;
    }

    /// <summary>Returns a snapshot of all currently active potion effects.</summary>
    public IReadOnlyCollection<PotionEffect> GetActivePotionEffects()
        => _activeEffects.Values;

    /// <summary>Returns true if this entity has the given effect active.</summary>
    public bool IsPotionActive(int effectId) => _activeEffects.ContainsKey(effectId);

    /// <summary>Removes all active potion effects (e.g. milk bucket). obf: nq equivalent.</summary>
    public void ClearAllPotionEffects() => _activeEffects.Clear();

    /// <summary>
    /// obf: <c>K()</c> override — isAlive = !isDead &amp;&amp; health > 0.
    /// </summary>
    public override bool IsEntityAlive() => !IsDead && Health > 0;

    /// <summary>
    /// obf: <c>H()</c> — canBePushed = !isDead.
    /// </summary>
    public virtual bool CanBePushed() => !IsDead;

    // ── Damage (spec §6 / LivingEntityDamage_Spec §6) ───────────────────────

    /// <summary>
    /// obf: <c>a(pm source, int amount)</c> — attackEntityFrom.
    ///
    /// Full pipeline per LivingEntityDamage_Spec §6:
    ///   1. Client-side / dead guard.
    ///   2. Fire-Resistance bypass for fire damage (stub: no potion check yet).
    ///   3. Invulnerability window: if still in immune half, absorb or partial hit.
    ///   4. Otherwise: full hit → set ac=aq, record bp, apply damage, hurt flash.
    ///   5. Track last attacker.
    ///   6. Knockback.
    ///   7. Hurt/death sound + death handler.
    /// </summary>
    public override bool AttackEntityFrom(DamageSource damageSource, int amount)
    {
        if (World != null && World.IsClientSide) return false;
        NaturalDespawnTicker = 0;
        if (Health <= 0) return false;

        // Fire-resistance bypass (potion check stubbed — no active potion system yet)
        // if (damageSource.IsFireDamage && HasFireResistancePotion()) return false;

        SwingProgress = 1.5f; // bb = 1.5F (hurt animation scale)
        bool fullHit  = true;

        if (InvulnerabilityCountdown > InvulnerabilityTicks / 2.0f)
        {
            // Still in immune half of window
            if (amount <= LastDamage) return false;
            // Partial hit: only the difference above the last hit applies
            ApplyDamage(damageSource, amount - LastDamage);
            LastDamage = amount;
            fullHit    = false;
        }
        else
        {
            LastDamage                = amount;
            PrevHealth                = Health;         // aN snapshot
            InvulnerabilityCountdown  = InvulnerabilityTicks; // ac = aq
            ApplyDamage(damageSource, amount);
            HurtTime = HurtDuration  = 10;
        }

        KnockbackAngle = 0.0f;

        if (fullHit)
        {
            // Track last attacker for XP attribution
            Entity? attacker = damageSource.GetAttacker();
            if (attacker is EntityPlayer player)
            {
                LastAttacker      = player;
                LastAttackerTicks = 60;
            }

            // Knockback
            if (attacker != null)
            {
                double dx = attacker.PosX - PosX;
                double dz = attacker.PosZ - PosZ;
                ApplyKnockback(attacker, amount, dx, dz);
            }
        }

        if (Health <= 0)
            OnDeath(damageSource);

        return true;
    }

    /// <summary>Overload accepting object for backward-compat with engine code.</summary>
    public virtual bool AttackEntityFrom(object damageSource, int amount)
        => AttackEntityFrom(damageSource as DamageSource ?? DamageSource.Generic, amount);

    /// <summary>
    /// obf: protected <c>b(pm source, int amount)</c> — applyDamage (inner).
    /// Applies armor reduction (<see cref="AbsorbArmor"/>) then subtracts from health.
    /// Source spec: LivingEntityDamage_Spec §7
    /// </summary>
    protected virtual void ApplyDamage(DamageSource damageSource, int amount)
    {
        amount  = AbsorbArmor(damageSource, amount);
        Health -= amount;
    }

    // Overload for legacy internal calls
    private void ApplyDamage(object damageSource, int amount)
        => ApplyDamage(damageSource as DamageSource ?? DamageSource.Generic, amount);

    /// <summary>
    /// obf: <c>c(pm source, int amount)</c> — armor absorption.
    /// Skipped entirely for unblockable sources.
    /// Quirk 3: carries remainder in <see cref="ArmorRemainder"/> across hits.
    /// Source spec: LivingEntityDamage_Spec §8
    /// </summary>
    protected virtual int AbsorbArmor(DamageSource damageSource, int amount)
    {
        if (damageSource.IsUnblockable) return amount;

        int armorValue  = GetTotalArmorValue(); // default 0
        int accumulated = amount * (25 - armorValue) + ArmorRemainder;
        OnArmorDamaged(amount);
        ArmorRemainder = accumulated % 25;  // carry forward (quirk 3)
        return accumulated / 25;
    }

    /// <summary>
    /// obf: <c>o_()</c> — getTotalArmorValue. 0 = unarmored base.
    /// </summary>
    protected virtual int GetTotalArmorValue() => 0;

    /// <summary>
    /// obf: <c>i(int)</c> — called in armor absorption; subclass may damage armor items.
    /// No-op base.
    /// </summary>
    protected virtual void OnArmorDamaged(int amount) { }

    /// <summary>
    /// obf: <c>a(pm source)</c> — onDeath callback.
    /// XP orb spawn (fk) is stubbed pending spec.
    /// </summary>
    protected virtual void OnDeath(DamageSource damageSource)
    {
        if (DiedFired) return;
        DiedFired = true;
        // XP orb spawn (fk spec pending)
    }

    // Legacy overload kept for EntityPlayer which still types the parameter as object
    protected virtual void OnDeath(object damageSource)
        => OnDeath(damageSource as DamageSource ?? DamageSource.Generic);

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

    /// <summary>
    /// Drops <paramref name="count"/> copies of item <paramref name="itemId"/> at this
    /// entity's position. Convenience wrapper for mob-specific death drops.
    /// Equivalent to Java <c>a(int, int)</c> (spawnDropItem).
    /// </summary>
    protected void SpawnDropItem(int itemId, int count)
    {
        if (World is not World concreteWorld) return;
        var entity = new EntityItem(concreteWorld, PosX, PosY, PosZ, new ItemStack(itemId, count, 0));
        entity.PickupDelay = 10;
        concreteWorld.SpawnEntity(entity);
    }

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
        // ── Drowning (spec LivingEntity_Survival §1) — runs before EntityBaseTick ──
        if (IsEntityAlive() && IsInWater)
        {
            // Decrement air by 1 each tick while head is in water
            short air = DataWatcher.GetShort(1);
            DataWatcher.UpdateObject(1, (short)(air - 1));

            if (DataWatcher.GetShort(1) == -20)
            {
                // Reset to 0 and deal 2 drowning damage (spec §1.3)
                DataWatcher.UpdateObject(1, (short)0);
                AttackEntityFrom(DamageSource.Drown, 2);
            }
        }
        else
        {
            // Restore air to full when not drowning
            DataWatcher.UpdateObject(1, (short)300);
        }

        // ── Suffocation in opaque blocks (spec LivingEntity_Survival §4) ─────────
        if (IsEntityAlive() && World != null)
        {
            double eyeX = PosX;
            double eyeY = PosY + GetEyeHeight();
            double eyeZ = PosZ;
            int bx = (int)Math.Floor(eyeX);
            int by = (int)Math.Floor(eyeY);
            int bz = (int)Math.Floor(eyeZ);
            int id = World.GetBlockId(bx, by, bz);
            if (id > 0 && Block.IsOpaqueCubeArr[id])
                AttackEntityFrom(DamageSource.InWall, 1);
        }

        EntityBaseTick(); // fire, void, prevPos, ticksExisted

        if (IsDead) return;

        // Step 1: fire resistance countdown
        if (FireResistTicks  > 0) FireResistTicks--;
        if (FireResistRender > 0) FireResistRender--;

        // Step 2: client interpolation — stub (network spec pending)

        // Step 3: countdown timers (spec: LivingEntityDamage §11)
        if (AttackCooldown          > 0) AttackCooldown--;
        if (HurtTime                > 0) HurtTime--;
        if (InvulnerabilityCountdown > 0) InvulnerabilityCountdown--;
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

        // Step 5: potion effects (spec §5.2)
        if (_activeEffects.Count > 0)
        {
            var toRemove = new List<int>();
            foreach (var (id, fx) in _activeEffects)
            {
                if (!fx.Tick(this)) toRemove.Add(id);
            }
            foreach (int id in toRemove) _activeEffects.Remove(id);
        }

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
        if (!onGround) return;
        float distance = FallDistance;
        int damage = (int)Math.Ceiling(distance - 3.0f);
        if (damage > 0)
            AttackEntityFrom(DamageSource.Fall, damage);
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

    /// <summary>
    /// obf: <c>el.b</c> — creature attribute UNDEAD.
    /// True for Zombie, Skeleton, ZombiePigman, Wither, Wither Skeleton.
    /// Used by Smite enchantment. Default: false.
    /// </summary>
    public virtual bool IsUndead => false;

    /// <summary>
    /// obf: <c>el.c</c> — creature attribute ARTHROPOD.
    /// True for Spider, CaveSpider, Silverfish, Endermite.
    /// Used by BaneOfArthropods enchantment. Default: false.
    /// </summary>
    public virtual bool IsArthropod => false;

    /// <summary>obf: <c>p_()</c> — getAmbientSoundInterval. Base returns 80 ticks.</summary>
    public virtual int GetAmbientSoundInterval() => 80;

    /// <summary>obf: <c>d()</c> — canDespawnNaturally. Base returns true; player overrides to false.</summary>
    public virtual bool CanDespawnNaturally() => true;

    // ── NBT hooks (stubs — ik spec pending) ──────────────────────────────────

    /// <summary>
    /// Writes living-entity fields. Spec: <c>nq.a(ik tag)</c>.
    /// Called by <see cref="Entity.SaveToNbt"/> dispatch chain.
    /// </summary>
    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("Health",     (short)Health);
        tag.PutShort("HurtTime",   (short)HurtTime);
        tag.PutShort("DeathTime",  (short)DeathTime);
        tag.PutShort("AttackTime", (short)AttackCooldown);

        // ActiveEffects list (spec §5.1 NBT)
        var effectList = new Nbt.NbtList();
        foreach (var fx in _activeEffects.Values)
        {
            var fxTag = new Nbt.NbtCompound();
            fxTag.PutByte("Id",        (byte)fx.EffectId);
            fxTag.PutByte("Amplifier", (byte)fx.Amplifier);
            fxTag.PutInt ("Duration",  fx.Duration);
            effectList.Add(fxTag);
        }
        tag.PutList("ActiveEffects", effectList);
    }

    /// <summary>
    /// Reads living-entity fields. Spec: <c>nq.b(ik tag)</c>.
    /// Called by <see cref="Entity.LoadFromNbt"/> dispatch chain.
    /// </summary>
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        Health         = tag.GetShort("Health");
        HurtTime       = tag.GetShort("HurtTime");
        DeathTime      = tag.GetShort("DeathTime");
        AttackCooldown = tag.GetShort("AttackTime");
        // ActiveEffects list (spec §5.1 NBT)
        _activeEffects.Clear();
        if (tag.GetList("ActiveEffects") is { } effectList)
        {
            for (int i = 0; i < effectList.Count; i++)
            {
                var fxTag = effectList.GetCompound(i);
                int id  = fxTag.GetByte("Id");
                int amp = fxTag.GetByte("Amplifier");
                int dur = fxTag.GetInt("Duration");
                _activeEffects[id] = new PotionEffect(id, dur, amp);
            }
        }
    }
}
