namespace SpectraEngine.Core.Mobs;

/// <summary>
/// Replica of <c>zo</c> (EntityMonster) — abstract base for all hostile mobs.
/// Extends <see cref="EntityAI"/>; adds target acquisition (nearest player),
/// melee attack with cooldown, retarget-on-hit, and spawn-light checks.
///
/// Spec §9 behaviour:
///   - <c>o()</c>: target = nearest player within 16 blocks (GetClosestVulnerablePlayer).
///   - <c>a(target, dist)</c>: attack when dist &lt; 2 and aT (cooldown) == 0.
///   - <c>b(target)</c>: apply melee damage (attackStrength + potion modifiers, stubbed).
///   - <c>i(target)</c>: check dist &lt; 2 and Y-overlap for attack range.
///   - On-damage: retarget to attacker.
///   - Position score: 0.5 − world.brightness (prefer dark).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §9
/// </summary>
public abstract class EntityMonster : EntityAI
{
    // ── Fields (spec §9) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> (on zo) — base melee damage. Concrete subclasses override.</summary>
    protected int AttackStrength = 2;   // obf: a

    /// <summary>obf: <c>aT</c> — attack cooldown counter; decrements each tick.</summary>
    private int _attackCooldown;        // obf: aT (reuses LivingEntity.AttackCooldown)

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityMonster(World world) : base(world)
    {
        XpDropAmount = 5; // all hostile mobs drop 5 XP
    }

    // ── Tick override (spec §9 zo.c()) ───────────────────────────────────────

    public override void Tick()
    {
        // Sunlight burn check: lightDamage > 0.5 adds stun
        // (world.b(1.0F) — stubbed as not burning in 1.0 base monster)
        base.Tick(); // calls EntityAI.Tick → RunAITick

        if (_attackCooldown > 0) _attackCooldown--;
    }

    // ── Target acquisition (spec §9 o()) ─────────────────────────────────────

    protected override Entity? GetAITarget()
    {
        if (World == null) return null;
        EntityPlayer? player = World.GetClosestVulnerablePlayer(this, 16.0);
        if (player != null && IsTargetable(player)) return player;
        return null;
    }

    // ── Attack range check (spec §9 i(ia)) ───────────────────────────────────

    protected override bool IsInRange(Entity target)
    {
        double dx = target.PosX - PosX;
        double dy = target.PosY - PosY;
        double dz = target.PosZ - PosZ;
        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (dist >= 2.0) return false;
        // Y-overlap check
        return target.BoundingBox.MaxY > BoundingBox.MinY
            && target.BoundingBox.MinY < BoundingBox.MaxY;
    }

    // ── Attack behavior (spec §9 a(target, dist)) ────────────────────────────

    protected override void OnTargetInRange(Entity target, float dist)
    {
        if (_attackCooldown <= 0)
        {
            _attackCooldown = 20; // 1-second cooldown
            MeleeAttack(target);
        }
    }

    protected override void OnTargetOutOfRange(Entity target, float dist) { }

    // ── Melee attack (spec §9 b(ia target)) ──────────────────────────────────

    /// <summary>
    /// obf: <c>b(ia target)</c> — deals <see cref="AttackStrength"/> melee damage.
    /// Potion modifiers (Strength/Weakness) are stubbed.
    /// </summary>
    protected virtual void MeleeAttack(Entity target)
    {
        if (target is LivingEntity living)
            living.AttackEntityFrom(DamageSource.MobAttack(this), AttackStrength);
    }

    // ── On-damage: retarget to attacker (spec §9 zo.a(DamageSource,int)) ─────

    public override bool AttackEntityFrom(DamageSource damageSource, int amount)
    {
        if (!base.AttackEntityFrom(damageSource, amount)) return false;

        Entity? attacker = damageSource.GetAttacker();
        // Retarget: ignore self-damage and mount relationships
        if (attacker != null && attacker != this
            && attacker != Mount && attacker != Rider)
        {
            AiTarget = attacker;
        }
        return true;
    }

    // ── Position score (spec §9 a(x,y,z)) — prefer dark ─────────────────────

    protected override float GetPositionScore(int x, int y, int z)
    {
        if (World == null) return 0f;
        return 0.5f - World.GetBrightness(x, y, z, 0);
    }

    // ── Spawn check (spec §9 i() / u_()) ────────────────────────────────────

    public override bool GetCanSpawnHere()
    {
        if (World == null) return false;
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(PosY);
        int bz = (int)Math.Floor(PosZ);
        return World.GetLightBrightness(bx, by, bz) <= 7
            && World.GetBlockMaterial(bx, by - 1, bz).IsSolid();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns true if the player is a valid target (alive, not immune).</summary>
    private static bool IsTargetable(EntityPlayer player) => !player.IsDead;
}
