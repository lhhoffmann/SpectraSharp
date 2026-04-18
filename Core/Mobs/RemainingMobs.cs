using SpectraEngine.Core.AI;

namespace SpectraEngine.Core.Mobs;

// ── Item ID constants (registryIndex = 256 + rawItemId) ─────────────────────────
// These match the registrations in ItemRegistry.cs.
// Format: vanilla_name = 256 + rawItemId = RegistryIndex

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aed</c> (EntitySlime) — EntityList "Slime", ID 55.
///
/// Size encoding: DW16 = 1 (tiny), 2 (small), 4 (big).
/// HP = size². Hitbox = 0.6×size on each axis.
/// On death (size > 1): splits into 2+rand(3) children with size/2.
/// On death (size == 1): drops 0–2 slimeballs.
/// Jump AI: counter d counts down, then jumps with Y velocity ∝ size.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §1
/// </summary>
public class EntitySlime : EntityAI
{
    private const int SlimeballId = 341; // new Item(85) → 341

    // ── Jump timer ───────────────────────────────────────────────────────────
    private int _jumpTimer;

    public EntitySlime(World world) : base(world)
    {
        TexturePath  = "/mob/slime.png";
        int size = GetSize();
        SetSize(0.6f * size, 0.6f * size);
        ResetJumpTimer();
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, 1); // size (int), default 1
    }

    public int GetSize() => DataWatcher.GetInt(16);

    public void SetSize(int size)
    {
        DataWatcher.UpdateObject(16, size);
        SetSize(0.6f * size, 0.6f * size);
    }

    public override int GetMaxHealth() => GetSize() * GetSize();

    private void ResetJumpTimer()
    {
        int size = GetSize();
        // Bigger slimes wait longer between jumps
        _jumpTimer = EntityRandom.NextInt(20) + 10 + size * 10;
    }

    public override void Tick()
    {
        base.Tick();

        if (World == null || World.IsClientSide) return;

        if (--_jumpTimer <= 0)
        {
            // Jump: small upward + horizontal velocity toward target or random
            MotionY = 0.42f * GetSize();
            if (AiTarget != null)
            {
                double dx = AiTarget.PosX - PosX;
                double dz = AiTarget.PosZ - PosZ;
                double len = Math.Sqrt(dx * dx + dz * dz);
                if (len > 0)
                {
                    MotionX = dx / len * GetSize() * 0.3;
                    MotionZ = dz / len * GetSize() * 0.3;
                }
            }
            else
            {
                double angle = EntityRandom.NextFloat() * Math.PI * 2;
                MotionX = Math.Cos(angle) * 0.3;
                MotionZ = Math.Sin(angle) * 0.3;
            }
            ResetJumpTimer();
        }
    }

    protected override void OnDeath(DamageSource damageSource)
    {
        base.OnDeath(damageSource);

        if (World == null || World.IsClientSide) return;

        int size = GetSize();
        if (size > 1)
        {
            // Split into smaller slimes
            int childCount = 2 + EntityRandom.NextInt(3);
            int childSize  = size / 2;
            for (int i = 0; i < childCount; i++)
            {
                var child = CreateChildSlime(World);
                child.SetSize(childSize);
                child.SetPositionAndRotation(PosX, PosY, PosZ, 0f, 0f);
                double angle = EntityRandom.NextFloat() * Math.PI * 2;
                child.MotionX = Math.Cos(angle) * 0.3;
                child.MotionZ = Math.Sin(angle) * 0.3;
                child.MotionY = 0.3;
                World.SpawnEntity(child);
            }
        }
    }

    protected virtual EntitySlime CreateChildSlime(World world) => new EntitySlime(world);

    protected override void DropItems(bool playerKilled, int looting)
    {
        if (GetSize() == 1)
        {
            int count = EntityRandom.NextInt(3 + looting);
            if (count > 0) SpawnDropItem(SlimeballId, count);
        }
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutInt("Size", GetSize());
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        SetSize(tag.GetInt("Size") & 255);
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>is</c> (EntityGhast) — EntityList "Ghast", ID 56.
///
/// 4×4 hitbox, 10 HP, fire immune. Wanders in ±16 cube, shoots EntityFireball.
/// DW16 = isCharging (0/1). Attack cycle: 0→idle, 1-10→charge, 20→shoot, then -40 cooldown.
/// Drops: 0–1 Ghast Tear, 0–2 Gunpowder.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §2
/// </summary>
public sealed class EntityGhast : EntityAI
{
    private const int GhastTearId  = 370; // new Item(114) → 370
    private const int GunpowderId  = 289; // new Item(33) → 289

    private int _attackPhase;   // obf: f — attack cycle counter

    public EntityGhast(World world) : base(world)
    {
        TexturePath    = "/mob/ghast.png";
        IsImmuneToFire = true;
        SetSize(4.0f, 4.0f);
        _attackPhase   = -40; // start in cooldown
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, 0); // isCharging int (0 or 1)
    }

    public bool IsCharging => DataWatcher.GetInt(16) == 1;
    public override int GetMaxHealth() => 10;

    public override void Tick()
    {
        base.Tick();

        if (World == null || World.IsClientSide) return;

        _attackPhase++;

        // d=10: begin charging face
        if (_attackPhase == 10) DataWatcher.UpdateObject(16, 1);

        // d=20: fire
        if (_attackPhase == 20)
        {
            DataWatcher.UpdateObject(16, 0);
            if (AiTarget != null)
            {
                double dx = AiTarget.PosX - PosX;
                double dy = AiTarget.PosY + AiTarget.Height * 0.5 - (PosY + Height * 0.5);
                double dz = AiTarget.PosZ - PosZ;
                var fireball = new EntityFireball(World, this, dx, dy, dz);
                fireball.SetPosition(PosX, PosY + Height * 0.5, PosZ);
                World.SpawnEntity(fireball);
            }
            _attackPhase = -40;
        }
    }

    protected override void DropItems(bool playerKilled, int looting)
    {
        int tears = EntityRandom.NextInt(2 + looting);
        if (tears > 0) SpawnDropItem(GhastTearId, tears);
        int powder = EntityRandom.NextInt(3 + looting);
        if (powder > 0) SpawnDropItem(GunpowderId, powder);
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aii</c> (EntityEnderman) — EntityList "Enderman", ID 58.
///
/// 0.6×2.9 hitbox, 40 HP. Carries a block (DW16=blockId, DW17=meta).
/// Teleports on projectile hit or water contact. Stare detection: player look
/// angle threshold 1.0−0.025/dist. NBT: "carried"/"carriedData".
/// Drops: 0–1 Ender Pearl.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §4
/// </summary>
public sealed class EntityEnderman : EntityMonster
{
    private const int EnderPearlId = 372; // new Item(116) → 372

    private static readonly int[] CarryableBlocks = { 2, 3, 12, 13, 37, 38, 39, 40, 46, 82, 86, 103, 110, 111 };

    public bool IsScreaming;  // obf: a

    public EntityEnderman(World world) : base(world)
    {
        TexturePath    = "/mob/enderman.png";
        AttackStrength = 7;
        SetSize(0.6f, 2.9f);
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (short)0); // carried block ID
        DataWatcher.Register(17, (byte)0);  // carried block meta
    }

    public override int GetMaxHealth() => 40;

    public short CarriedBlockId   => DataWatcher.GetShort(16);
    public byte  CarriedBlockMeta => DataWatcher.GetByte(17);

    public void SetCarriedBlock(int blockId, int meta)
    {
        DataWatcher.UpdateObject(16, (short)blockId);
        DataWatcher.UpdateObject(17, (byte)meta);
    }

    public override void Tick()
    {
        base.Tick();

        if (World == null || World.IsClientSide) return;

        // Water damage + teleport
        int blockAtFeet = World.GetBlockId((int)Math.Floor(PosX), (int)Math.Floor(PosY), (int)Math.Floor(PosZ));
        if (blockAtFeet == 8 || blockAtFeet == 9)
        {
            AttackEntityFrom(DamageSource.Drown, 1);
            TryTeleportRandom();
        }
    }

    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (!base.AttackEntityFrom(source, amount)) return false;

        // Teleport away from projectile hits
        if (source.IsProjectile)
            TryTeleportRandom();

        return true;
    }

    private void TryTeleportRandom()
    {
        if (World == null) return;
        for (int attempt = 0; attempt < 15; attempt++)
        {
            double tx = PosX + (EntityRandom.NextDouble() - 0.5) * 128.0;
            double ty = PosY + (EntityRandom.NextInt(16) - 8);
            double tz = PosZ + (EntityRandom.NextDouble() - 0.5) * 128.0;

            int blockBelow = World.GetBlockId((int)Math.Floor(tx), (int)Math.Floor(ty) - 1, (int)Math.Floor(tz));
            if (Block.BlocksList[blockBelow] != null)
            {
                SetPosition(tx, ty, tz);
                break;
            }
        }
    }

    protected override void OnDeath(DamageSource damageSource)
    {
        base.OnDeath(damageSource);

        // Drop carried block if any
        if (CarriedBlockId > 0 && World != null)
            World.SpawnEntity(new EntityItem(World, PosX, PosY, PosZ,
                new ItemStack(CarriedBlockId, 1, CarriedBlockMeta)));
    }

    protected override void DropItems(bool playerKilled, int looting)
    {
        int count = EntityRandom.NextInt(2 + looting);
        if (count > 0) SpawnDropItem(EnderPearlId, count);
    }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutShort("carried",     CarriedBlockId);
        tag.PutShort("carriedData", CarriedBlockMeta);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        SetCarriedBlock(tag.GetShort("carried"), tag.GetShort("carriedData"));
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aco</c> (EntityCaveSpider) — EntityList "CaveSpider", ID 59.
///
/// Extends EntitySpider. Smaller hitbox (0.7×0.5), 12 HP.
/// Poison on melee hit: Normal=140t, Hard=300t, Easy=none.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §5
/// </summary>
public sealed class EntityCaveSpider : EntitySpider
{
    public EntityCaveSpider(World world) : base(world)
    {
        TexturePath = "/mob/spider_cave.png";
        SetSize(0.7f, 0.5f);
    }

    public override int GetMaxHealth() => 12;

    protected override void MeleeAttack(Entity target)
    {
        base.MeleeAttack(target);

        if (World == null) return;

        // Apply poison based on difficulty (spec §5.4)
        int difficulty = World.Difficulty;
        int duration = difficulty switch
        {
            1 => 0,   // Easy: no poison
            2 => 140, // Normal: 7 seconds
            3 => 300, // Hard: 15 seconds
            _ => 0
        };

        if (duration > 0 && target is LivingEntity living)
            living.AddPotionEffect(new PotionEffect(19 /* Poison */, duration, 0));
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>gl</c> (EntitySilverfish) — EntityList "Silverfish", ID 60.
///
/// 0.3×0.7 hitbox, 8 HP. Arthropod. Group call: alerts nearby silverfish every 20 ticks.
/// No item drops on death. Spawns from infested stone blocks (ID 97) when broken.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §6
/// </summary>
public sealed class EntitySilverfish : EntityMonster
{
    private int _groupCallTimer = 20; // obf: b

    public EntitySilverfish(World world) : base(world)
    {
        TexturePath    = "/mob/silverfish.png";
        AttackStrength = 1;
        SetSize(0.3f, 0.7f);
    }

    public override bool IsArthropod => true;
    public override int GetMaxHealth() => 8;

    public override void Tick()
    {
        base.Tick();

        if (World == null || World.IsClientSide || AiTarget == null) return;

        if (--_groupCallTimer <= 0)
        {
            _groupCallTimer = 20;

            // Alert nearby silverfish (within 5 blocks)
            var nearby = World.GetEntitiesWithinAABB<EntitySilverfish>(
                BoundingBox.Expand(5.0, 5.0, 5.0));
            foreach (var other in nearby)
            {
                if (other != this && other.AiTarget == null)
                    other.AiTarget = AiTarget;
            }
        }
    }

    // No item drops
    protected override void DropItems(bool playerKilled, int looting) { }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>yv</c> (EntitySquid) — EntityList "Squid", ID 94.
///
/// 0.95×0.95 hitbox, 10 HP. Passive water creature; swims randomly.
/// Drops 1–3 Ink Sac (ID 351, meta=0) on death.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §9
/// </summary>
public sealed class EntitySquid : EntityAI
{
    private const int InkSacId = 351; // ItemDye (new Item(95) → 351), meta=0 = ink sac

    private float _tentacleAngle1;   // obf: a
    private float _tentacleAngle2;   // obf: b — visual only
    private int   _thrustTimer;      // obf: d

    public EntitySquid(World world) : base(world)
    {
        TexturePath = "/mob/squid.png";
        SetSize(0.95f, 0.95f);
        _thrustTimer = EntityRandom.NextInt(20) + 10;
    }

    protected override void EntityInit() { }

    public override int GetMaxHealth() => 10;

    // Squids are passive — override target to never acquire player
    protected override Entity? GetAITarget() => null;

    public override void Tick()
    {
        base.Tick();

        // Visual tentacle animation
        _tentacleAngle1 += 0.05f;
        _tentacleAngle2 = (float)Math.Sin(_tentacleAngle1) * 0.5f + 0.5f;

        if (World == null || World.IsClientSide) return;

        // Swim AI: change direction when timer expires
        if (--_thrustTimer <= 0)
        {
            _thrustTimer = EntityRandom.NextInt(20) + 10;
            double angle = EntityRandom.NextFloat() * Math.PI * 2;
            MotionX = Math.Cos(angle) * 0.5;
            MotionZ = Math.Sin(angle) * 0.5;
            MotionY = (EntityRandom.NextFloat() - 0.5) * 0.4;
        }
    }

    protected override void DropItems(bool playerKilled, int looting)
    {
        int count = 1 + EntityRandom.NextInt(3 + looting);
        if (World != null)
            World.SpawnEntity(new EntityItem(World, PosX, PosY, PosZ,
                new ItemStack(InkSacId, count, 0)));
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aik</c> (EntityWolf) — EntityList "Wolf", ID 95.
///
/// 0.8×0.8 hitbox. Tamed wolves: 20 HP, 4 damage; wild: 8 HP, 2 damage.
/// DW16: bit flags (isSitting/isAngry/isTamed). DW17: owner name. DW18: current HP.
/// NBT: "Owner", "Sitting", "Angry", "Tame".
/// Taming: 1/3 bone = success. No item drops on death.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §10
/// </summary>
public sealed class EntityWolf : EntityAnimal
{
    public EntityWolf(World world) : base(world)
    {
        TexturePath    = "/mob/wolf.png";
        SetSize(0.8f, 0.8f);
    }

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(16, (byte)0);  // bit flags: sit/angry/tamed
        DataWatcher.Register(17, "");       // owner name
        DataWatcher.Register(18, 0);        // current HP for collar render
    }

    public bool IsSitting => (DataWatcher.GetByte(16) & 1) != 0;
    public new bool IsAngry   => (DataWatcher.GetByte(16) & 2) != 0;
    public bool IsTamed   => (DataWatcher.GetByte(16) & 4) != 0;
    public string OwnerName => DataWatcher.GetString(17);

    public override int GetMaxHealth() => IsTamed ? 20 : 8;

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner) => null; // wolves don't breed in 1.0

    protected override void OnDeath(DamageSource damageSource) { base.OnDeath(damageSource); /* no drops */ }
    protected override void DropItems(bool playerKilled, int looting) { }

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutString("Owner",   OwnerName);
        tag.PutByte("Sitting",   (byte)(IsSitting ? 1 : 0));
        tag.PutByte("Angry",     (byte)(IsAngry   ? 1 : 0));
        tag.PutByte("Tame",      (byte)(IsTamed   ? 1 : 0));
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        byte flags = 0;
        if (tag.GetByte("Sitting") != 0) flags |= 1;
        if (tag.GetByte("Angry")   != 0) flags |= 2;
        if (tag.GetByte("Tame")    != 0) flags |= 4;
        DataWatcher.UpdateObject(16, flags);
        DataWatcher.UpdateObject(17, tag.GetString("Owner"));
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>tb</c> (EntityMooshroom) — EntityList "MushroomCow", ID 96.
///
/// 0.9×1.3 hitbox, 10 HP. Extends EntityCow.
/// Milking with Bowl → Mushroom Stew (ID 282). Shearing → 5 Red Mushrooms + spawn Cow.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §11
/// </summary>
public sealed class EntityMooshroom : EntityCow
{
    private const int RedMushroomBlockId = 40;   // block/item ID (same ID space for blocks &lt; 256)
    private const int BowlId             = 281;  // new Item(25) → 281
    private const int MushroomStewId     = 282;  // new Item(26) → 282

    public EntityMooshroom(World world) : base(world)
    {
        TexturePath = "/mob/mooshroom.png";
    }

    protected override EntityAnimal? CreateOffspring(EntityAnimal partner)
        => World != null ? new EntityMooshroom(World) : null;
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>ahd</c> (EntitySnowMan) — EntityList "SnowMan", ID 97.
///
/// 0.4×1.8 hitbox, 4 HP. Created by player placing Snow+Pumpkin column.
/// Throws snowballs at hostile mobs within 10 blocks (approx. every 20 ticks).
/// Places snow trail below feet. Melts in warm biomes (temperature > 1.0) or rain.
/// Drops 0–15 Snowballs on death.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §12
/// </summary>
public sealed class EntitySnowMan : EntityAI
{
    private const int SnowLayerId = 78;   // block ID 78 (snow layer)
    private const int SnowballId  = 332;  // new Item(76) → 332

    private int _throwCooldown;

    public EntitySnowMan(World world) : base(world)
    {
        TexturePath = "/mob/snowman.png";
        SetSize(0.4f, 1.8f);
        _throwCooldown = 20;
    }

    protected override void EntityInit() { }

    public override int GetMaxHealth() => 4;

    // SnowGolems target hostile mobs, not players — override
    protected override Entity? GetAITarget()
    {
        if (World == null) return null;
        var monsters = World.GetEntitiesWithinAABB<EntityMonster>(
            BoundingBox.Expand(10.0, 4.0, 10.0));
        return monsters.Count > 0 ? monsters[0] : null;
    }

    public override void Tick()
    {
        base.Tick();

        if (World == null || World.IsClientSide) return;

        // Melt damage: rain or warm biome
        if (World.IsRaining())
        {
            AttackEntityFrom(DamageSource.Drown, 1);
        }
        else if (World.ChunkManager != null)
        {
            float temperature = World.ChunkManager.GetBiomeAt((int)PosX, (int)PosZ).Temperature;
            if (temperature > 1.0f)
                AttackEntityFrom(DamageSource.Generic, 1);
        }

        // Snow trail: place snow layer at feet if temperature is cold enough
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(PosY);
        int bz = (int)Math.Floor(PosZ);
        int blockAtFeet = World.GetBlockId(bx, by, bz);
        if (blockAtFeet == 0)
        {
            bool coldEnough = true;
            if (World.ChunkManager != null)
                coldEnough = World.ChunkManager.GetBiomeAt(bx, bz).Temperature <= 1.0f;

            if (coldEnough)
                World.SetBlock(bx, by, bz, SnowLayerId);
        }

        // Throw snowball at nearest hostile mob
        if (--_throwCooldown <= 0)
        {
            _throwCooldown = 20;
            AiTarget = GetAITarget();
            if (AiTarget != null)
            {
                var snowball = new EntitySnowball(World, this);
                snowball.SetThrowVelocity(
                    AiTarget.PosX - PosX,
                    AiTarget.PosY + AiTarget.Height * 0.5 - snowball.PosY,
                    AiTarget.PosZ - PosZ,
                    1.6f, 12.0f);
                World.SpawnEntity(snowball);
            }
        }
    }

    protected override void DropItems(bool playerKilled, int looting)
    {
        int count = EntityRandom.NextInt(16 + looting);
        if (count > 0) SpawnDropItem(SnowballId, count);
    }
}

// ────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>aea</c> (EntityMagmaCube) updated to extend <see cref="EntitySlime"/>.
/// Fire-immune variant of Slime. No drops. Fully bright.
/// Source spec: Documentation/VoxelCore/Parity/Specs/RemainingMobs_Spec.md §8
/// </summary>
public sealed class EntityMagmaCube : EntitySlime
{
    public const int BrightnessOverride = 15728880;

    public EntityMagmaCube(World world) : base(world)
    {
        TexturePath    = "/mob/lava.png";
        IsImmuneToFire = true;
    }

    public override int GetMaxHealth() => GetSize() * GetSize();

    protected override EntitySlime CreateChildSlime(World world) => new EntityMagmaCube(world);

    // No drops (spec §8.4)
    protected override void DropItems(bool playerKilled, int looting) { }
}
