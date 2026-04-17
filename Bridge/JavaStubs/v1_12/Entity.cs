// Stubs for net.minecraft.entity.* — Minecraft 1.12
// Covers the entity hierarchy that mod blocks and Forge events reference.
// EntityPlayer is in EntityPlayer.cs (separate file, separate file-scoped namespace).

using net.minecraft.util.math;
using net.minecraft.world;

namespace net.minecraft.entity;

/// <summary>
/// MinecraftStubs v1_12 — Entity.
/// Base class for all entities (players, mobs, items, projectiles, etc.).
/// </summary>
public class Entity
{
    public double posX { get; set; }
    public double posY { get; set; }
    public double posZ { get; set; }

    public double motionX { get; set; }
    public double motionY { get; set; }
    public double motionZ { get; set; }

    public float rotationYaw   { get; set; }
    public float rotationPitch { get; set; }

    public World? world    { get; internal set; }
    public int    entityId { get; internal set; }

    public bool isDead   { get; set; }
    public bool onGround { get; set; }

    protected string entityName { get; set; } = "";

    public BlockPos getPosition()
        => new((int)Math.Floor(posX), (int)Math.Floor(posY), (int)Math.Floor(posZ));

    public double getDistanceSq(double x, double y, double z)
    {
        double dx = posX - x, dy = posY - y, dz = posZ - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double getDistanceSq(Entity other)
        => getDistanceSq(other.posX, other.posY, other.posZ);

    public virtual string getJavaClassName() => "net.minecraft.entity.Entity";
}

/// <summary>Base for all living entities: health, damage, AI.</summary>
public class EntityLivingBase : Entity
{
    public float health    { get; set; } = 20f;
    public float maxHealth { get; set; } = 20f;

    public bool isAlive() => !isDead && health > 0f;

    public virtual float getHealth()    => health;
    public virtual float getMaxHealth() => maxHealth;

    public virtual void attackEntityFrom(DamageSource source, float amount)
    {
        health -= amount;
        if (health <= 0f) isDead = true;
    }

    public virtual bool isOnLadder() => false;
    public virtual bool isInWater()  => false;
    public virtual bool isBurning()  => false;

    public override string getJavaClassName() => "net.minecraft.entity.EntityLivingBase";
}

/// <summary>Simple damage source stub.</summary>
public sealed class DamageSource(string name)
{
    public static readonly DamageSource GENERIC = new("generic");
    public static readonly DamageSource FALL    = new("fall");
    public static readonly DamageSource DROWN   = new("drown");
    public static readonly DamageSource FIRE    = new("inFire");
    public static readonly DamageSource STARVE  = new("starve");

    public string damageType { get; } = name;
    public bool isFireDamage()  => damageType is "inFire" or "onFire";
    public bool isMagicDamage() => damageType is "magic";
}

/// <summary>Entity with AI goals (stub-only — goal sets not implemented).</summary>
public class EntityLiving : EntityLivingBase
{
    public override string getJavaClassName() => "net.minecraft.entity.EntityLiving";
}

/// <summary>Hostile mob base.</summary>
public class EntityMob : EntityLiving
{
    public override string getJavaClassName() => "net.minecraft.entity.monster.EntityMob";
}

/// <summary>Passive animal base.</summary>
public class EntityAnimal : EntityLiving
{
    public override string getJavaClassName() => "net.minecraft.entity.passive.EntityAnimal";
}
