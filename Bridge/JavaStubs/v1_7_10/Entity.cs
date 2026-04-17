// Stubs for net.minecraft.entity.* — Minecraft 1.7.10

using net.minecraft.world;

namespace net.minecraft.entity;

/// <summary>MinecraftStubs v1_7_10 — Entity base.</summary>
public class Entity
{
    public double posX        { get; set; }
    public double posY        { get; set; }
    public double posZ        { get; set; }
    public double motionX     { get; set; }
    public double motionY     { get; set; }
    public double motionZ     { get; set; }
    public float  rotationYaw   { get; set; }
    public float  rotationPitch { get; set; }
    public World? worldObj    { get; internal set; }  // 1.7 uses "worldObj" not "world"
    public bool   isDead      { get; set; }
    public bool   onGround    { get; set; }
    protected string entityName { get; set; } = "";

    public virtual string getJavaClassName() => "net.minecraft.entity.Entity";
}

/// <summary>MinecraftStubs v1_7_10 — EntityLivingBase.</summary>
public class EntityLivingBase : Entity
{
    public float health    { get; set; } = 20f;
    public float maxHealth { get; set; } = 20f;
    public virtual float getHealth()    => health;
    public virtual float getMaxHealth() => maxHealth;
    public bool isEntityAlive() => !isDead && health > 0f;
    public override string getJavaClassName() => "net.minecraft.entity.EntityLivingBase";
}

/// <summary>MinecraftStubs v1_7_10 — EntityLiving.</summary>
public class EntityLiving : EntityLivingBase
{
    public override string getJavaClassName() => "net.minecraft.entity.EntityLiving";
}

/// <summary>MinecraftStubs v1_7_10 — EntityMob.</summary>
public class EntityMob : EntityLiving
{
    public override string getJavaClassName() => "net.minecraft.entity.monster.EntityMob";
}

/// <summary>MinecraftStubs v1_7_10 — EntityAnimal.</summary>
public class EntityAnimal : EntityLiving
{
    public override string getJavaClassName() => "net.minecraft.entity.passive.EntityAnimal";
}
