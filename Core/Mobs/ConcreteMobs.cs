namespace SpectraSharp.Core.Mobs;

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
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>it</c> (EntitySkeleton) — ID 51.
/// Extends <see cref="EntityMonster"/>. Attacks with bow (stub — melee fallback).
/// No extra NBT fields.
/// Source spec: EntityMobBase_Spec §6.2
/// </summary>
public sealed class EntitySkeleton : EntityMonster
{
    public EntitySkeleton(World world) : base(world)
    {
        TexturePath    = "/mob/skeleton.png";
        AttackStrength = 2; // attacks via bow, melee is fallback
        // moveSpeed = 0.7F (nq default, not overridden)
    }

    public override int GetMaxHealth() => 20;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>vq</c> (EntitySpider) — ID 52.
/// Extends <see cref="EntityMonster"/>. AABB 1.4×0.9. DataWatcher 16 bit 0 = isClimbing (transient).
/// No extra NBT fields.
/// Source spec: EntityMobBase_Spec §6.3
/// </summary>
public sealed class EntitySpider : EntityMonster
{
    public EntitySpider(World world) : base(world)
    {
        TexturePath    = "/mob/spider.png";
        AttackStrength = 2;
        PushbackWidth  = 0.8f;  // bw moveSpeed
        SetSize(1.4f, 0.9f);
    }

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
/// Extends <see cref="EntityMonster"/>. Explodes when fuse reaches 30.
/// NBT: "powered" byte (written only when true).
/// Source spec: EntityMobBase_Spec §6.4
/// </summary>
public sealed class EntityCreeper : EntityMonster
{
    // Transient fuse state — NOT persisted
    private int _fuseCountdown;
    private int _prevFuseCountdown;

    public EntityCreeper(World world) : base(world)
    {
        TexturePath    = "/mob/creeper.png";
        AttackStrength = 2; // kills via explosion, not melee
        // moveSpeed = 0.7F (nq default)
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)255); // fuseCountdown sync (−1 = not fusing)
        DataWatcher.Register(17, (byte)0);   // isPowered (bit 0)
    }

    public override int GetMaxHealth() => 20;

    /// <summary>Returns true if this creeper was struck by lightning (powered).</summary>
    public bool IsPowered => (DataWatcher.GetByte(17) & 1) != 0;

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
public sealed class EntityCow : EntityAnimal
{
    public EntityCow(World world) : base(world)
    {
        TexturePath = "/mob/cow.png";
        SetSize(0.9f, 1.3f);
    }

    public override int GetMaxHealth() => 10;
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
