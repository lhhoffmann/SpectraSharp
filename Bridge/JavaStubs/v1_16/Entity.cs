// Stub for net.minecraft.entity hierarchy — Minecraft 1.16.5
// Note: 1.16 renamed EntityLivingBase → LivingEntity, EntityPlayer → PlayerEntity

namespace net.minecraft.entity;

/// <summary>
/// MinecraftStubs v1_16 — Entity base class.
/// 1.16 renamed Entity fields to drop Hungarian notation (posX → getX(), etc.)
/// </summary>
public class Entity
{
    public double posX;
    public double posY;
    public double posZ;
    public float  rotationYaw;
    public float  rotationPitch;
    public bool   onGround;
    public bool   isDead;

    public net.minecraft.world.World? world;

    public double getX()         => posX;
    public double getY()         => posY;
    public double getZ()         => posZ;

    public void setPosition(double x, double y, double z)
    {
        posX = x; posY = y; posZ = z;
    }

    public virtual string getJavaClassName() => "net.minecraft.entity.Entity";
}

/// <summary>
/// MinecraftStubs v1_16 — LivingEntity.
/// Renamed from EntityLivingBase in 1.12.
/// </summary>
public class LivingEntity : Entity
{
    public float health     = 20f;
    public float maxHealth  = 20f;

    public virtual float  getHealth()    => health;
    public virtual float  getMaxHealth() => maxHealth;
    public virtual void   setHealth(float h) { health = h; }

    public virtual bool   isAlive()      => !isDead && health > 0f;

    public override string getJavaClassName() => "net.minecraft.entity.LivingEntity";
}

/// <summary>
/// MinecraftStubs v1_16 — MobEntity (was EntityLiving).
/// </summary>
public class MobEntity : LivingEntity
{
    public override string getJavaClassName() => "net.minecraft.entity.MobEntity";
}

/// <summary>
/// MinecraftStubs v1_16 — Monster base (was EntityMob).
/// </summary>
public class MonsterEntity : MobEntity
{
    public override string getJavaClassName() => "net.minecraft.entity.monster.MonsterEntity";
}

/// <summary>
/// MinecraftStubs v1_16 — AnimalEntity (was EntityAnimal).
/// </summary>
public class AnimalEntity : MobEntity
{
    public override string getJavaClassName() => "net.minecraft.entity.passive.AnimalEntity";
}
