namespace SpectraSharp.Core.Mobs;

/// <summary>
/// Replica of <c>fx</c> (EntityAnimal) — abstract base for all breedable animals.
/// Extends <see cref="EntityAI"/>; adds age (DataWatcher 12) and breeding timer.
///
/// Age encoding (DataWatcher entry 12):
///   0        = adult
///   negative = baby (starts −24000, counts up to 0 over ~20 min)
///   positive = breeding cooldown (set to 6000 after breeding)
///
/// NBT additional fields (spec §5.2):
///   "Age"    TAG_Int — persisted age value
///   "InLove" TAG_Int — breeding readiness timer
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityMobBase_Spec.md §5
/// </summary>
public abstract class EntityAnimal : EntityAI
{
    // ── Fields (spec §5.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> (on fx) — inLove timer (set to 600 on feed; counts to 0).</summary>
    protected int InLoveTimer;  // obf: a

    // Age stored in DataWatcher slot 12 (registered in EntityInit).

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityAnimal(World world) : base(world) { }

    // ── EntityInit ───────────────────────────────────────────────────────────

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(12, 0); // age int — type 2 (Integer)
    }

    // ── Age accessors (spec §5.1 m()/b(int)) ─────────────────────────────────

    /// <summary>obf: <c>m()</c> — reads age from DataWatcher.</summary>
    public int GetAge() => DataWatcher.GetInt(12);

    /// <summary>obf: <c>b(int)</c> — writes age to DataWatcher.</summary>
    public void SetAge(int age) => DataWatcher.UpdateObject(12, age);

    /// <summary>Returns true if this animal is a baby (age &lt; 0).</summary>
    public bool IsBaby() => GetAge() < 0;

    // ── On hit: panic + clear love (spec §5.5) ───────────────────────────────

    public override bool AttackEntityFrom(DamageSource damageSource, int amount)
    {
        PanicTimer = 60;    // sprint for 60 ticks
        AiTarget   = null;  // clear follow/breeding pursuit
        InLoveTimer = 0;    // clear inLove
        return base.AttackEntityFrom(damageSource, amount);
    }

    // ── NBT (spec §5.2) ──────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutInt("Age",    GetAge());
        tag.PutInt("InLove", InLoveTimer);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        SetAge(tag.GetInt("Age"));
        InLoveTimer = tag.GetInt("InLove");
    }
}
