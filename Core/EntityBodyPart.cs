namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>vc</c> (EntityBodyPart) — a multi-part collision entity owned by
/// <see cref="EntityDragon"/>. Seven instances exist per dragon (head, body, three tail
/// segments, left wing, right wing).
///
/// All damage received is routed through <see cref="EntityDragon.OnBodyPartHit"/> so the
/// dragon can apply its damage-scaling and immunity filter (spec §6).
///
/// Part names (spec §10): "head", "body", "tail", "wing" (wings share the name "wing").
///
/// Vanilla notes:
///   - <c>e_()</c> returns true — always within render distance.
///   - <c>h(ia other)</c> returns true if other is this part or the parent dragon.
///   - No NBT serialisation (crystals and body parts do not persist).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnderDragon_Spec.md §10
/// </summary>
public sealed class EntityBodyPart : Entity
{
    // ── Fields (spec §10) ────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — parent dragon that owns this part.</summary>
    public readonly EntityDragon Parent;    // obf: a

    /// <summary>obf: <c>b</c> — part name for identification ("head", "body", "tail", "wing").</summary>
    public readonly string PartName;        // obf: b

    // ── Constructor ───────────────────────────────────────────────────────────

    public EntityBodyPart(World world, EntityDragon parent, string partName, float width, float height)
        : base(world)
    {
        Parent   = parent;
        PartName = partName;
        SetSize(width, height);
    }

    protected override void EntityInit() { }

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        // Body parts are positioned by the parent dragon each tick.
        // No independent physics — no-op here.
    }

    // ── Damage routing (spec §10) ─────────────────────────────────────────────

    /// <summary>
    /// Delegates all damage to the parent dragon's <see cref="EntityDragon.OnBodyPartHit"/>.
    /// The dragon applies the quarter-damage reduction for non-head parts (spec §6).
    /// </summary>
    public override bool AttackEntityFrom(DamageSource source, int amount)
        => Parent.OnBodyPartHit(this, source, amount);

    // ── Collision / team checks ───────────────────────────────────────────────

    /// <summary>
    /// obf: <c>h(ia other)</c> — returns true if other is this part or the parent dragon.
    /// Prevents parts from pushing each other and the dragon from pushing its own parts.
    /// </summary>
    public bool IsSameTeam(Entity other)
        => other == this || other == Parent;

    // ── NBT (spec §10 — no persistence) ──────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
