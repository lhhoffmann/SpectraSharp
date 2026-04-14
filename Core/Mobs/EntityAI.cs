namespace SpectraSharp.Core.Mobs;

/// <summary>
/// Replica of <c>ww</c> (EntityAI) — abstract intermediate between <see cref="LivingEntity"/>
/// and all AI-driven mobs. Adds panic timer and move-speed multiplier.
///
/// Field `by` (int) — panic/anger timer. Set to 60 by <see cref="EntityAnimal"/> on hit.
/// While > 0 the effective move speed is doubled via <see cref="GetMoveSpeedMultiplier"/>.
/// Decrements each tick in <see cref="Tick"/>.
///
/// NBT: no additional fields — write/read delegate entirely to nq.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityMobBase_Spec.md §3
/// </summary>
public abstract class EntityAI : LivingEntity
{
    // ── Fields (spec §3.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>by</c> (int on ww, NOT the bx on nq) — panic/anger countdown.</summary>
    protected int PanicTimer;      // obf: by

    // Target and pathfinder (transient — not persisted)
    // obf: a = current path (dw), h = target (ia), i = isPanicking (bool)
    // Stubs — full pathfinding out of scope for this spec.
    protected Entity? AiTarget;

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityAI(World world) : base(world) { }

    // ── Move speed with panic multiplier (spec §3.3) ─────────────────────────

    /// <summary>
    /// obf: <c>aw()</c> — returns base move speed, doubled while <see cref="PanicTimer"/> > 0.
    /// </summary>
    protected virtual float GetMoveSpeedMultiplier()
    {
        float speed = PushbackWidth; // bw = base speed (0.7 default)
        if (PanicTimer > 0) speed *= 2.0f;
        return speed;
    }

    // ── NBT (spec §3.2 — delegates to super, no ww-specific fields) ──────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) => base.WriteEntityToNBT(tag);
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) => base.ReadEntityFromNBT(tag);

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick();
        if (PanicTimer > 0) PanicTimer--;
    }
}
