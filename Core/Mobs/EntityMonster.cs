namespace SpectraSharp.Core.Mobs;

/// <summary>
/// Replica of <c>zo</c> (EntityMonster) — abstract base for all hostile mobs.
/// Extends <see cref="EntityAI"/>; adds base attack strength and sets XP value to 5.
///
/// NBT: no additional fields — write/read delegate entirely to ww → nq.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityMobBase_Spec.md §4
/// </summary>
public abstract class EntityMonster : EntityAI
{
    // ── Fields (spec §4.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> (on zo) — base melee damage. Concrete subclasses override.</summary>
    protected int AttackStrength = 2;  // obf: a

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityMonster(World world) : base(world)
    {
        XpDropAmount = 5;  // aX = 5 — all hostile mobs drop 5 XP
    }

    // ── Melee attack (spec §4.4) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>b(ia target)</c> — deals <see cref="AttackStrength"/> melee damage to target.
    /// Called by AI when target is in range. Potion modifiers (Strength/Weakness) are stubbed.
    /// </summary>
    protected virtual void MeleeAttack(Entity target)
    {
        if (target is LivingEntity living)
            living.AttackEntityFrom(DamageSource.MobAttack(this), AttackStrength);
    }
}
