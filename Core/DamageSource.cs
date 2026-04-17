namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>pm</c> (DamageSource) — describes how an entity was hurt.
///
/// Carries a type string, flags (unblockable/fire/projectile), hunger exhaustion, and an
/// optional attacker entity reference (in subclasses). The damage pipeline in
/// <see cref="LivingEntity.AttackEntityFrom"/> reads these flags to decide armor bypass,
/// fire-resistance bypass, and hunger exhaustion.
///
/// Static singleton sources cover all non-entity damage. Entity attacks use the factory
/// methods <c>a(nq)</c>, <c>a(vi)</c>, etc., which return <see cref="EntityDamageSource"/>
/// or <see cref="EntityDamageSourceIndirect"/> instances.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/LivingEntityDamage_Spec.md §3
/// </summary>
public class DamageSource
{
    // ── Static singletons (spec §3.5) ────────────────────────────────────────

    /// <summary>obf: <c>pm.a</c> — "inFire"   — standing in fire block. Fire damage.</summary>
    public static readonly DamageSource InFire      = new DamageSource("inFire").SetFireDamage();

    /// <summary>obf: <c>pm.b</c> — "onFire"   — burning (fire ticks). Unblockable fire.</summary>
    public static readonly DamageSource OnFire      = new DamageSource("onFire").SetUnblockable().SetFireDamage();

    /// <summary>obf: <c>pm.c</c> — "lava"     — lava contact. Fire damage.</summary>
    public static readonly DamageSource Lava        = new DamageSource("lava").SetFireDamage();

    /// <summary>obf: <c>pm.d</c> — "inWall"   — suffocation. Unblockable.</summary>
    public static readonly DamageSource InWall      = new DamageSource("inWall").SetUnblockable();

    /// <summary>obf: <c>pm.e</c> — "drown"    — drowning. Unblockable.</summary>
    public static readonly DamageSource Drown       = new DamageSource("drown").SetUnblockable();

    /// <summary>obf: <c>pm.f</c> — "starve"   — starvation. Unblockable, no hunger cost.</summary>
    public static readonly DamageSource Starve      = new DamageSource("starve").SetUnblockable();

    /// <summary>obf: <c>pm.g</c> — "cactus"   — cactus contact. Blockable.</summary>
    public static readonly DamageSource Cactus      = new DamageSource("cactus");

    /// <summary>obf: <c>pm.h</c> — "fall"     — fall damage. Unblockable.</summary>
    public static readonly DamageSource Fall        = new DamageSource("fall").SetUnblockable();

    /// <summary>obf: <c>pm.i</c> — "outOfWorld" — void. Unblockable, bypasses invulnerability.</summary>
    public static readonly DamageSource OutOfWorld  = new DamageSource("outOfWorld").SetUnblockable().SetBypassesInvulnerability();

    /// <summary>obf: <c>pm.j</c> — "generic"  — generic / unknown. Unblockable.</summary>
    public static readonly DamageSource Generic     = new DamageSource("generic").SetUnblockable();

    /// <summary>obf: <c>pm.k</c> — "explosion" — explosion. Blockable.</summary>
    public static readonly DamageSource Explosion   = new DamageSource("explosion");

    /// <summary>obf: <c>pm.l</c> — "magic"    — magic / area potion. Unblockable.</summary>
    public static readonly DamageSource Magic       = new DamageSource("magic").SetUnblockable();

    // ── Instance fields (spec §3.1) ───────────────────────────────────────────

    /// <summary>obf: <c>m</c> — type string identifier (e.g. "mob", "fall", "fire").</summary>
    public readonly string TypeString;

    /// <summary>obf: <c>n</c> — isUnblockable: skip armor reduction.</summary>
    public bool IsUnblockable  { get; private set; }

    /// <summary>obf: <c>o</c> — bypassesInvulnerability: applied even when invulnerable.</summary>
    public bool BypassesInvulnerability { get; private set; }

    /// <summary>obf: <c>p</c> — hungerExhaustionAmount (0.3 default, 0.0 for unblockable).</summary>
    public float HungerExhaustion { get; private set; } = 0.3f;

    /// <summary>obf: <c>q</c> — isFireDamage: blocked by Fire Resistance.</summary>
    public bool IsFireDamage   { get; private set; }

    /// <summary>obf: <c>r</c> — isProjectile.</summary>
    public bool IsProjectile   { get; private set; }

    // ── Derived type helpers (used by Enchantment damage reduction) ───────────

    /// <summary>True when this source is fall damage (TypeString == "fall").</summary>
    public bool IsFallDamage        => TypeString == "fall";

    /// <summary>True when this source is explosion damage (TypeString == "explosion").</summary>
    public bool IsExplosionDamage   => TypeString == "explosion";

    /// <summary>True when this source is a projectile (IsProjectile flag).</summary>
    public bool IsProjectileDamage  => IsProjectile;

    // ── Constructor ───────────────────────────────────────────────────────────

    protected DamageSource(string typeString)
    {
        TypeString = typeString;
    }

    // ── Builder methods (spec §3.2) — return this for chaining ───────────────

    /// <summary>obf: <c>h()</c> — marks unblockable and clears hunger exhaustion.</summary>
    private DamageSource SetUnblockable()
    {
        IsUnblockable     = true;
        HungerExhaustion  = 0.0f;
        return this;
    }

    /// <summary>obf: <c>i()</c> — marks as bypassing invulnerability.</summary>
    private DamageSource SetBypassesInvulnerability()
    {
        BypassesInvulnerability = true;
        return this;
    }

    /// <summary>obf: <c>j()</c> — marks as fire damage.</summary>
    private DamageSource SetFireDamage()
    {
        IsFireDamage = true;
        return this;
    }

    /// <summary>obf: <c>c()</c> — marks as projectile.</summary>
    public DamageSource MarkProjectile()
    {
        IsProjectile = true;
        return this;
    }

    // ── Attacker accessor (spec §3.3) ─────────────────────────────────────────

    /// <summary>obf: <c>a()</c> — returns the attacking entity, or null for non-entity sources.</summary>
    public virtual Entity? GetAttacker() => null;

    /// <summary>obf: <c>g()</c> — delegates to <see cref="GetAttacker"/>.</summary>
    public Entity? GetDirectAttacker() => GetAttacker();

    // ── Static factory methods (spec §3.4) ───────────────────────────────────

    /// <summary>obf: <c>pm.a(nq)</c> — mob melee attack.</summary>
    public static DamageSource MobAttack(LivingEntity attacker)
        => new EntityDamageSource("mob", attacker);

    /// <summary>obf: <c>pm.a(vi)</c> — player melee attack.</summary>
    public static DamageSource PlayerAttack(EntityPlayer player)
        => new EntityDamageSource("player", player);

    /// <summary>obf: <c>pm.a(ro, ia)</c> — arrow: projectile, owner is the attacker.</summary>
    public static DamageSource Arrow(Entity arrow, Entity owner)
        => new EntityDamageSourceIndirect("arrow", arrow, owner).MarkProjectile();

    /// <summary>obf: <c>pm.a(aad, ia)</c> — fireball: fire + projectile.</summary>
    public static DamageSource Fireball(Entity fireball, Entity owner)
        => new EntityDamageSourceIndirect("fireball", fireball, owner).SetFireDamage().MarkProjectile();

    /// <summary>obf: <c>pm.a(ia thrown, ia owner)</c> — thrown snowball/egg: projectile.</summary>
    public static DamageSource Thrown(Entity thrown, Entity owner)
        => new EntityDamageSourceIndirect("thrown", thrown, owner).MarkProjectile();

    /// <summary>obf: <c>pm.b(ia indirect, ia owner)</c> — indirect magic: unblockable.</summary>
    public static DamageSource IndirectMagic(Entity indirect, Entity owner)
    {
        var src = new EntityDamageSourceIndirect("indirectMagic", indirect, owner);
        src.IsUnblockable    = true;
        src.HungerExhaustion = 0.0f;
        return src;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>fq</c> (EntityDamageSource) — damage with a single attacker entity.
/// Used for melee attacks (mob, player).
/// Source spec: LivingEntityDamage_Spec §4
/// </summary>
public sealed class EntityDamageSource : DamageSource
{
    private readonly Entity _attacker;

    public EntityDamageSource(string typeString, Entity attacker) : base(typeString)
    {
        _attacker = attacker;
    }

    /// <summary>Returns the attacking entity.</summary>
    public override Entity GetAttacker() => _attacker;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>qq</c> (EntityDamageSourceIndirect) — damage with a projectile and an owner.
/// <see cref="GetAttacker"/> returns the <b>owner</b> (not the projectile itself).
/// Source spec: LivingEntityDamage_Spec §5
/// </summary>
public sealed class EntityDamageSourceIndirect : DamageSource
{
    private readonly Entity  _projectile; // accessible via cast if needed
    private readonly Entity? _owner;

    public EntityDamageSourceIndirect(string typeString, Entity projectile, Entity? owner)
        : base(typeString)
    {
        _projectile = projectile;
        _owner      = owner;
    }

    /// <summary>Returns the owner entity (not the projectile).</summary>
    public override Entity? GetAttacker() => _owner;

    /// <summary>Returns the projectile entity.</summary>
    public Entity GetProjectile() => _projectile;
}
