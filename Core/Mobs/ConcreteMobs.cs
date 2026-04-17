namespace SpectraEngine.Core.Mobs;

// ── Hostile mobs ─────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>gr</c> (EntityZombie) — ID 54.
/// Extends <see cref="EntityMonster"/>. Burns in daylight (tick stub).
/// No extra NBT fields.
/// Source spec: EntityMobBase_Spec §6.1
/// </summary>
public sealed class EntityZombie : EntityMonster
{
    public EntityZombie(World world) : base(world)
    {
        TexturePath    = "/mob/zombie.png";
        AttackStrength = 4;
        PushbackWidth  = 0.5f;  // bw moveSpeed
    }

    public override int GetMaxHealth() => 20;
    public override bool IsUndead => true;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>it</c> (EntitySkeleton) — ID 51.
/// Extends <see cref="EntityMonster"/>. Shoots arrows at targets.
///
/// Arrow attack (spec BowArrow_Spec §6):
///   60-tick cooldown between shots; speed=1.0, spread=12.0.
///
/// Sunburn (spec §6):
///   When daytime AND canSeeSky AND getBrightness > 0.5F: SetFire(8).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BowArrow_Spec.md §6
/// </summary>
public sealed class EntitySkeleton : EntityMonster
{
    private int _arrowCooldown;

    public EntitySkeleton(World world) : base(world)
    {
        TexturePath    = "/mob/skeleton.png";
        AttackStrength = 2; // melee fallback (bow is primary)
    }

    public override int GetMaxHealth() => 20;
    public override bool IsUndead => true;

    public override void Tick()
    {
        base.Tick();

        // Sunburn: daytime + sky exposure + bright enough → catch fire
        if (World != null && World.IsDaytime())
        {
            int bx = (int)Math.Floor(PosX);
            int by = (int)Math.Floor(PosY);
            int bz = (int)Math.Floor(PosZ);
            bool canSeeSky = World.GetHeightValue(bx, bz) <= by;
            float brightness = World.GetBrightness(bx, by, bz, 0);
            if (canSeeSky && brightness > 0.5f)
                SetFire(8);
        }

        if (_arrowCooldown > 0) _arrowCooldown--;
    }

    protected override void OnTargetInRange(Entity target, float dist)
    {
        if (_arrowCooldown > 0) return;
        if (World == null || target is not LivingEntity) return;

        _arrowCooldown = 60;

        double dx = target.PosX - PosX;
        double dy = target.BoundingBox.MinY + target.Height / 3.0 - (PosY + Height * 0.62);
        double dz = target.PosZ - PosZ;
        double distXZ = Math.Sqrt(dx * dx + dz * dz);

        var arrow = new EntityArrow(World, this, 1.0f);
        // Override with skeleton-specific aim: dy += distXZ * 0.2 (spec §6)
        dy += distXZ * 0.2;
        arrow.SetShootingVector(dx, dy, dz, 1.0f, 12.0f);

        World.PlaySoundAt(this, "random.bow", 1.0f, 1.0f / (EntityRandom.NextFloat() * 0.4f + 0.8f));
        World.SpawnEntity(arrow);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vq</c> (EntitySpider) — ID 52.
/// Extends <see cref="EntityMonster"/>. AABB 1.4×0.9. DataWatcher 16 bit 0 = isClimbing (transient).
/// No extra NBT fields.
/// Source spec: EntityMobBase_Spec §6.3
/// </summary>
public class EntitySpider : EntityMonster
{
    public EntitySpider(World world) : base(world)
    {
        TexturePath    = "/mob/spider.png";
        AttackStrength = 2;
        PushbackWidth  = 0.8f;  // bw moveSpeed
        SetSize(1.4f, 0.9f);
    }

    public override bool IsArthropod => true;

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)0); // isClimbing flag (bit 0)
    }

    public override int GetMaxHealth() => 16;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>abh</c> (EntityCreeper) — ID 50.
/// Extends <see cref="EntityMonster"/>. Fuses and explodes when target is within range.
/// Fuse advances 1/tick in range, retreats 1/tick out of range; explodes at 30.
///
/// NBT: "powered" byte (written only when true).
///
/// Quirks preserved (Explosion_Spec §12):
///   5. Fuse counter caps at [0, 30]; explosion fires exactly when fuse reaches 30.
///   6. Music disc (ID 2256 or 2257) drops only when killed by a Skeleton (EntitySkeleton).
///   7. Fuse start sound plays on both server (in attack range method) and client (DW16 update).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Explosion_Spec.md §9
/// </summary>
public sealed class EntityCreeper : EntityMonster
{
    // ── Fuse state — transient, NOT persisted (Explosion_Spec §9) ────────────

    /// <summary>obf: b — current fuse countdown. Server-authoritative; client interpolates.</summary>
    private int _fuseCountdown;

    /// <summary>obf: c — previous fuse value saved each tick for client smooth interpolation.</summary>
    private int _prevFuseCountdown;

    // ── Constants (spec §3) ───────────────────────────────────────────────────

    private const int FuseExplodeThreshold = 30;       // explode when fuse reaches this value
    private const float NormalIgniteRange  = 3.0f;     // ignite distance (normal creeper)
    private const float PoweredIgniteRange = 7.0f;     // ignite distance (charged creeper)
    private const float NormalPower        = 3.0f;     // explosion power (normal)
    private const float PoweredPower       = 6.0f;     // explosion power (charged)

    // ── Constructor ───────────────────────────────────────────────────────────

    public EntityCreeper(World world) : base(world)
    {
        TexturePath    = "/mob/creeper.png";
        AttackStrength = 0; // Creeper does not melee — it fuses and explodes
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)255); // obf: DW16 = fuseCountdown delta (−1 = not fusing)
        DataWatcher.Register(17, (byte)0);   // obf: DW17 = isPowered (bit 0)
    }

    public override int GetMaxHealth() => 20;

    // ── DataWatcher accessors (spec §9) ──────────────────────────────────────

    /// <summary>obf: ax() — Returns true if this creeper was struck by lightning (powered).</summary>
    public bool IsPowered => DataWatcher.GetByte(17) == 1;

    /// <summary>obf: ay() — Returns DW16 fuse delta as signed byte (-1 / 0 / +1).</summary>
    private sbyte GetFuseDelta() => (sbyte)DataWatcher.GetByte(16);

    /// <summary>obf: b(int val) — writes DW16 (fuse delta sync).</summary>
    private void SetFuseDelta(int val) => DataWatcher.UpdateObject(16, (byte)val);

    // ── Tick (spec §9 — a()) ─────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>abh.a()</c> — entity tick override.
    /// Saves prev fuse, client-side DW16 → fuse update, super tick (AI), then
    /// auto-defuse if we lost our target.
    /// </summary>
    public override void Tick()
    {
        _prevFuseCountdown = _fuseCountdown;

        if (World != null && World.IsClientSide)
        {
            // Client: apply DW16 delta to local fuse counter
            int delta = GetFuseDelta();
            if (delta > 0 && _fuseCountdown == 0)
            {
                // Fuse start sound — stub (audio not yet implemented)
            }
            _fuseCountdown += delta;
            _fuseCountdown = Math.Clamp(_fuseCountdown, 0, FuseExplodeThreshold);
        }

        base.Tick(); // runs AI → may call OnTargetInRange / OnTargetOutOfRange

        // Server: if we lost our target but are still fusing, defuse
        if (World != null && !World.IsClientSide && AiTarget == null && _fuseCountdown > 0)
        {
            SetFuseDelta(-1);
            _fuseCountdown--;
            if (_fuseCountdown < 0) _fuseCountdown = 0;
        }
    }

    // ── In-range attack override (spec §9 — a(ia,dist)) ─────────────────────

    /// <summary>
    /// obf: <c>abh.a(ia target, float dist)</c> — called by AI when target is in attack range.
    /// Creeper does not melee — it advances the fuse instead.
    /// Spec: Explosion_Spec §9.
    /// </summary>
    protected override void OnTargetInRange(Entity target, float dist)
    {
        if (World == null || World.IsClientSide) return;

        float igniteRange = IsPowered ? PoweredIgniteRange : NormalIgniteRange;
        if (dist < igniteRange)
        {
            // Start sound when fuse begins
            if (_fuseCountdown == 0)
            {
                // Sound stub: "random.fuse" 1.0F 0.5F — audio not yet implemented
            }

            SetFuseDelta(1);    // notify client: fusing (quirk 7: server plays sound too)
            _fuseCountdown++;

            if (_fuseCountdown >= FuseExplodeThreshold)
            {
                // Explode (quirk 5: fires exactly when fuse reaches 30)
                SetDead();
                float power = IsPowered ? PoweredPower : NormalPower;
                World.CreateExplosion(null, PosX, PosY, PosZ, power, isIncendiary: false);
            }
        }
        else
        {
            // In nominal attack range but not ignite range — defuse
            Defuse();
        }
    }

    // ── Out-of-range override (spec §9 — b(ia,dist)) ─────────────────────────

    /// <summary>
    /// obf: <c>abh.b(ia target, float dist)</c> — called by AI when target is out of range.
    /// Spec: Explosion_Spec §9.
    /// </summary>
    protected override void OnTargetOutOfRange(Entity target, float dist)
    {
        if (World == null || World.IsClientSide) return;
        Defuse();
    }

    // ── Death: music disc on Skeleton kill (spec §9) ─────────────────────────

    /// <summary>
    /// obf: <c>abh.a(pm,int)</c> — on-death override.
    /// If killed by a Skeleton, drops a random music disc (ID 2256 or 2257).
    /// Spec: Explosion_Spec §9 / quirk 6.
    /// </summary>
    protected override void OnDeath(DamageSource damageSource)
    {
        base.OnDeath(damageSource);
        if (World == null || World.IsClientSide) return;

        if (damageSource.GetAttacker() is EntitySkeleton && World is World concreteWorld)
        {
            // acy.bB.bM = 2256 ("13"), +1 = 2257 ("cat") — nextInt(2) selects one
            int discId = 2256 + concreteWorld.Random.NextInt(2);
            SpawnDropItem(discId, 1);
        }
    }

    // ── Fuse render helper (spec §9 — g(float partialTick)) ─────────────────

    /// <summary>
    /// obf: <c>abh.g(float partial)</c> — fuse interpolation for renderer.
    /// Returns 0..1 (28/28 = 1.0 at fuse countdown = 28, slightly before 30).
    /// </summary>
    public float GetFuseInterpolated(float partialTick)
        => (_prevFuseCountdown + (_fuseCountdown - _prevFuseCountdown) * partialTick) / 28.0f;

    // ── onStruckByLightning (spec §9) ────────────────────────────────────────

    /// <summary>Charges the creeper when struck by lightning.</summary>
    public void OnStruckByLightning()
    {
        DataWatcher.UpdateObject(17, (byte)1); // set isPowered
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Defuse()
    {
        SetFuseDelta(-1);
        _fuseCountdown--;
        if (_fuseCountdown < 0) _fuseCountdown = 0;
    }

    // ── NBT (spec §9) ────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        // "powered" only written when true (spec §6.4)
        if (IsPowered)
            tag.PutBoolean("powered", true);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        if (tag.HasKey("powered") && tag.GetBoolean("powered"))
            DataWatcher.UpdateObject(17, (byte)1);
        // _fuseCountdown intentionally not persisted — resets to 0 on every load (spec quirk 1)
    }
}

// ── Passive / breedable mobs ──────────────────────────────────────────────────

/// <summary>
/// Replica of <c>fd</c> (EntityPig) — ID 90.
/// Extends <see cref="EntityAnimal"/>. AABB 0.9×0.9. DataWatcher 16 bit 0 = hasSaddle.
/// NBT: "Saddle" byte (beyond fx Age+InLove).
/// Source spec: EntityMobBase_Spec §6.5
/// </summary>
public sealed class EntityPig : EntityAnimal
{
    public EntityPig(World world) : base(world)
    {
        TexturePath = "/mob/pig.png";
        SetSize(0.9f, 0.9f);
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)0); // hasSaddle (bit 0)
    }

    public override int GetMaxHealth() => 10;

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner)
        => World != null ? new EntityPig(World) : null;

    public bool HasSaddle
    {
        get => (DataWatcher.GetByte(16) & 1) != 0;
        set => DataWatcher.UpdateObject(16, value ? (byte)1 : (byte)0);
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutBoolean("Saddle", HasSaddle);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        HasSaddle = tag.GetBoolean("Saddle");
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>hm</c> (EntitySheep) — ID 91.
/// Extends <see cref="EntityAnimal"/>. AABB 0.9×1.3.
/// DataWatcher 16: bits 3..0 = colour (0–15), bit 4 = isSheared.
/// NBT: "Sheared" byte + "Color" byte (beyond fx Age+InLove).
/// Source spec: EntityMobBase_Spec §6.6
/// </summary>
public sealed class EntitySheep : EntityAnimal
{
    public EntitySheep(World world) : base(world)
    {
        TexturePath = "/mob/sheep.png";
        SetSize(0.9f, 1.3f);
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)0); // colour + sheared flag
    }

    public override int GetMaxHealth() => 8;

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner)
        => World != null ? new EntitySheep(World) : null;

    /// <summary>Wool colour 0–15.</summary>
    public int WoolColour
    {
        get => DataWatcher.GetByte(16) & 0xF;
        set => DataWatcher.UpdateObject(16, (byte)((DataWatcher.GetByte(16) & 0xF0) | (value & 0xF)));
    }

    /// <summary>True if this sheep has been sheared.</summary>
    public bool IsSheared
    {
        get => (DataWatcher.GetByte(16) & 0x10) != 0;
        set
        {
            byte cur = DataWatcher.GetByte(16);
            DataWatcher.UpdateObject(16, value ? (byte)(cur | 0x10) : (byte)(cur & ~0x10));
        }
    }

    /// <summary>
    /// Sets wool colour. obf: <c>hm.b(int)</c>.
    /// Called by SpawnerAnimals.postSpawnSetup to randomise initial colour.
    /// </summary>
    public void SetFleeceColor(int color) => WoolColour = color & 0xF;

    /// <summary>
    /// Rolls a random initial fleece colour from the weighted vanilla distribution.
    /// obf: <c>hm.a(Random)</c>.
    /// Weights (sum 100): White=35, Orange=8, Magenta=8, LightBlue=8, Yellow=8,
    /// Lime=8, Pink=2, Gray=8, Silver=2, Cyan=8, Purple=2, Blue=2, Brown=4, Green=2,
    /// Red=2, Black=2.
    /// </summary>
    public static int GetRandomFleeceColor(JavaRandom rand)
    {
        // Simplified: pick from weighted distribution (spec §6.6)
        int r = rand.NextInt(100);
        if (r <  5) return 15; // black
        if (r < 10) return 14; // red
        if (r < 12) return 13; // green
        if (r < 14) return 12; // brown
        if (r < 16) return 11; // blue
        if (r < 18) return 10; // purple
        if (r < 26) return  9; // cyan
        if (r < 28) return  8; // silver (light gray)
        if (r < 36) return  7; // gray
        if (r < 38) return  6; // pink
        if (r < 46) return  5; // lime
        if (r < 54) return  4; // yellow
        if (r < 62) return  3; // light blue
        if (r < 70) return  2; // magenta
        if (r < 78) return  1; // orange
        return 0;              // white (most common)
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutBoolean("Sheared", IsSheared);
        tag.PutByte("Color", (byte)WoolColour);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        IsSheared  = tag.GetBoolean("Sheared");
        WoolColour = tag.GetByte("Color") & 0xF;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>adr</c> (EntityCow) — ID 92.
/// Extends <see cref="EntityAnimal"/>. AABB 0.9×1.3. No extra NBT fields.
/// Source spec: EntityMobBase_Spec §6.7
/// </summary>
public class EntityCow : EntityAnimal
{
    public EntityCow(World world) : base(world)
    {
        TexturePath = "/mob/cow.png";
        SetSize(0.9f, 1.3f);
    }

    public override int GetMaxHealth() => 10;

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner)
        => World != null ? new EntityCow(World) : null;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>qh</c> (EntityChicken) — ID 93.
/// Extends <see cref="EntityAnimal"/>. AABB 0.3×0.7. Lays eggs periodically.
/// eggLayTimer (g) is NOT persisted — resets to random [6000,12000) on every load (spec quirk 2).
/// No extra NBT fields beyond fx Age+InLove.
/// Source spec: EntityMobBase_Spec §6.8
/// </summary>
public sealed class EntityChicken : EntityAnimal
{
    /// <summary>Ticks until next egg. Transient — not persisted.</summary>
    private int _eggLayTimer;

    public EntityChicken(World world) : base(world)
    {
        TexturePath   = "/mob/chicken.png";
        SetSize(0.3f, 0.7f);
        _eggLayTimer  = EntityRandom.NextInt(6000) + 6000;
    }

    public override int GetMaxHealth() => 4;

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner)
        => World != null ? new EntityChicken(World) : null;

    public override void Tick()
    {
        base.Tick();

        // Egg laying (spec §6.8)
        if (!IsBaby() && World != null && !World.IsClientSide)
        {
            if (--_eggLayTimer <= 0)
            {
                // Drop 1 egg (item ID 344)
                if (World != null)
                    World.SpawnEntity(new EntityItem(World, PosX, PosY, PosZ, new ItemStack(344, 1)));
                _eggLayTimer = EntityRandom.NextInt(6000) + 6000;
            }
        }
    }
}

// ── Nether mobs ───────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>qf</c> (EntityBlaze) — ID 61.
/// Extends <see cref="EntityMonster"/>. Fire immune.
/// DataWatcher slot 16 bit 0 = isCharging flag.
/// Full-brightness rendering (stub — renderer reads BrightnessOverride).
/// Combat: melee + ranged fireball burst (AI stub — melee fallback only).
/// Drops: Blaze Rod (item 369).
/// Source spec: NetherFortress_Spec §8.1
/// </summary>
public sealed class EntityBlaze : EntityMonster
{
    /// <summary>
    /// Full-brightness override (spec §8.1: a(float) returns 15728880).
    /// Renderer reads this to skip light sampling.
    /// </summary>
    public const int BrightnessOverride = 15728880;

    public EntityBlaze(World world) : base(world)
    {
        TexturePath    = "/mob/fire.png";
        IsImmuneToFire = true;
        AttackStrength = 6;
        DataWatcher.Register(16, (byte)0); // isCharging (bit 0)
    }

    public override int GetMaxHealth() => 20;

    /// <summary>obf: ax() — Returns true while charging fireball burst.</summary>
    public bool IsCharging => (DataWatcher.GetByte(16) & 1) != 0;

    protected override void DropItems(bool playerKilled, int looting)
    {
        // 0 to (1+looting) Blaze Rods (spec §8.1)
        int count = EntityRandom.NextInt(2 + looting);
        if (count > 0) SpawnDropItem(369, count); // item 369 = Blaze Rod
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>jm</c> (EntityZombiePigman) — ID 57.
/// Extends <see cref="EntityMonster"/>. Fire immune.
/// Passive unless attacked; aggro spreads to nearby ZombiePigmen (stub — broadcasts anger).
/// NBT: "Anger" (short) — anger timer; "HurtBy" (int) entity ID of last attacker.
/// Drops: gold nugget (371) + cooked porkchop (320), 0 to (1+looting) each.
/// Source spec: NetherFortress_Spec §8.2
/// </summary>
public sealed class EntityZombiePigman : EntityMonster
{
    private short _anger;      // obf: d — ticks until calming down (400-800 when aggroed)
    private int   _hurtById;   // obf: c — entity ID of aggro source for broadcast

    public EntityZombiePigman(World world) : base(world)
    {
        TexturePath    = "/mob/pigzombie.png";
        IsImmuneToFire = true;
        AttackStrength = 9; // gold sword base
    }

    public override int GetMaxHealth() => 20;
    public override bool IsUndead => true;

    public new bool IsAngry => _anger > 0;

    public override void Tick()
    {
        base.Tick();
        if (_anger > 0) _anger--;
    }

    public override bool AttackEntityFrom(DamageSource damageSource, int amount)
    {
        if (!base.AttackEntityFrom(damageSource, amount)) return false;
        // Trigger anger (400–800 ticks) on being hit
        if (_anger == 0)
        {
            _anger    = (short)(EntityRandom.NextInt(400) + 400);
            _hurtById = damageSource.GetAttacker()?.EntityId ?? 0;
        }
        return true;
    }

    protected override void DropItems(bool playerKilled, int looting)
    {
        // 0 to (1+looting) rotten flesh + gold nuggets (spec §8.2)
        int flesh   = EntityRandom.NextInt(2 + looting);
        int nuggets = EntityRandom.NextInt(2 + looting);
        if (flesh   > 0) SpawnDropItem(367, flesh);   // rotten flesh (rawId 111 → 367)
        if (nuggets > 0) SpawnDropItem(371, nuggets); // gold nugget
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutShort("Anger", _anger);
        tag.PutInt("HurtBy", _hurtById);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        _anger    = tag.GetShort("Anger");
        _hurtById = tag.GetInt("HurtBy");
    }
}

