namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>sf</c> (EntityEnderCrystal) — pillar-top crystals in the End that heal the
/// Ender Dragon. EntityList name "EnderCrystal", ID 200.
///
/// Behaviour (spec §11):
///   - Tick counter <c>a</c> starts at a random offset so beams are staggered.
///   - Perpetually relights fire beneath itself each tick.
///   - Takes any damage (any source, any amount) and instantly explodes (power 6.0) and dies.
///   - No NBT serialisation.
///   - NoClip = true; always considered in render range.
///
/// Quirk (spec §12.6): in <c>a(pm,int)</c>, <c>this.b = 0</c> is set immediately before the
/// <c>if (this.b &lt;= 0)</c> check — the check is always true. Preserved for RNG-state parity.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnderDragon_Spec.md §11
/// </summary>
public sealed class EntityEnderCrystal : Entity
{
    // ── Fields (spec §11) ────────────────────────────────────────────────────

    private int _tickAge;   // obf: a — tick counter; starts at random offset
    private int _health;    // obf: b — health/state (synced via DataWatcher slot 8)

    // ── Constructor ───────────────────────────────────────────────────────────

    public EntityEnderCrystal(World world, double x, double y, double z) : base(world)
    {
        SetSize(2.0f, 2.0f);
        SetPosition(x, y, z);
        _tickAge = EntityRandom.NextInt(100000);
        _health  = 5;
        NoClip   = true;
    }

    /// <summary>Deserialization constructor.</summary>
    public EntityEnderCrystal(World world) : base(world)
    {
        SetSize(2.0f, 2.0f);
        _tickAge = EntityRandom.NextInt(100000);
        _health  = 5;
        NoClip   = true;
    }

    protected override void EntityInit() { }

    // ── Tick (spec §11) ─────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;

        // 1. Advance tick counter
        _tickAge++;

        // 2. DataWatcher sync (slot 8) — stub: actual network sync not yet wired
        // DataWatcher.UpdateObject(8, _health);

        // 3. Keep fire burning beneath crystal (block ID 51 = fire)
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(PosY);
        int bz = (int)Math.Floor(PosZ);
        if (World.GetBlockId(bx, by, bz) != 51)
        {
            World.SetBlock(bx, by, bz, 51);
        }
    }

    // ── Damage (spec §11) ────────────────────────────────────────────────────

    /// <summary>
    /// Any damage kills the crystal instantly and creates a power-6 explosion.
    /// Quirk §12.6: <c>_health = 0</c> is set before the always-true <c>&lt;= 0</c> check.
    /// </summary>
    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (IsDead) return true;

        _health = 0;         // quirk §12.6 — always triggers the branch below

        if (_health <= 0)    // always true (preserved for RNG-state parity)
        {
            IsDead = true;
            ((World)World!).CreateExplosion(null, PosX, PosY, PosZ, 6.0f, false);
        }

        return true;
    }

    // ── NBT (spec §11 — no persistence) ──────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag) { }
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag) { }
}
