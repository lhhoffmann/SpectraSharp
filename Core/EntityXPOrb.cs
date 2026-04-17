namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>fk</c> (EntityXPOrb) — experience point orb. String ID "XPOrb", int ID 2.
///
/// Physics:
///   - Gravity: 0.03F per tick downward.
///   - Attraction: scans ±8 blocks for nearest player; pulls toward them.
///   - Pickup: 0-tick pickup cooldown AND player invulnerability window = 0 → adds XP, removes orb.
///   - Despawn: field <c>b</c> (DespawnAge) reaches 6000 ticks (5 min).
///
/// XP tiers (spec §1): determine orb size for rendering.
///   Thresholds: [3, 7, 17, 37, 73, 149, 307, 617, 1237, 2477]
///
/// Quirks preserved (spec §1):
///   NBT "Age" stores DespawnAge (field b), not the orb age counter (a).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §1
/// </summary>
public sealed class EntityXPOrb : Entity
{
    // ── Tier thresholds (spec §1) ─────────────────────────────────────────────

    private static readonly int[] TierThresholds = { 3, 7, 17, 37, 73, 149, 307, 617, 1237, 2477 };

    // ── Fields (spec §1) ─────────────────────────────────────────────────────

    private int _age;         // obf: a — ticks alive (internal counter)
    private int _despawnAge;  // obf: b — NBT "Age" counter; remove at 6000
    private int _pickup;      // obf: c — pickup cooldown (0 = can be picked up)
    private int _health = 5;  // obf: d
    public  int XpValue;      // obf: e — the XP amount this orb grants

    // ── Constructor ──────────────────────────────────────────────────────────

    public EntityXPOrb(World world, double x, double y, double z, int value) : base(world)
    {
        SetSize(0.5f, 0.5f);
        SetPosition(x, y, z);
        XpValue = value;
    }

    public EntityXPOrb(World world) : base(world) { SetSize(0.5f, 0.5f); }

    protected override void EntityInit() { }

    // ── Tick ─────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;

        _age++;
        _despawnAge++;

        if (_despawnAge >= 6000) { IsDead = true; return; }
        if (_pickup > 0) _pickup--;

        // Gravity (spec §1 Physics)
        MotionY -= 0.03f;

        // Position update
        PosX += MotionX;
        PosY += MotionY;
        PosZ += MotionZ;
        SetPosition(PosX, PosY, PosZ);

        // Player attraction (spec §1 Physics)
        var conWorld = (World)World;
        var players  = conWorld.GetEntitiesWithinAABB<EntityPlayer>(
            BoundingBox.Copy().Expand(8.0, 8.0, 8.0));

        EntityPlayer? nearest = null;
        double nearestDist    = double.MaxValue;
        foreach (var p in players)
        {
            if (!p.IsEntityAlive()) continue;
            double dx  = PosX - p.PosX;
            double dy  = PosY - p.PosY;
            double dz  = PosZ - p.PosZ;
            double d2  = dx * dx + dy * dy + dz * dz;
            if (d2 < nearestDist) { nearestDist = d2; nearest = p; }
        }

        if (nearest != null && nearestDist < 64.0) // 8 blocks
        {
            double normDist = Math.Sqrt(nearestDist) / 8.0;
            double force    = (1.0 - normDist) * (1.0 - normDist) * 0.1;
            MotionX += (nearest.PosX - PosX) / Math.Sqrt(nearestDist) * force;
            MotionY += (nearest.PosY - PosY) / Math.Sqrt(nearestDist) * force;
            MotionZ += (nearest.PosZ - PosZ) / Math.Sqrt(nearestDist) * force;
        }

        // Drag
        MotionX *= 0.98; MotionY *= 0.98; MotionZ *= 0.98;

        // Pickup check (spec §1)
        if (_pickup == 0 && nearest != null && nearestDist < 1.5 * 1.5)
        {
            PickupByPlayer(nearest);
        }
    }

    private void PickupByPlayer(EntityPlayer player)
    {
        // player.bM == 0 — InvulnerabilityCountdown from base entity (check == 0)
        if (player.InvulnerabilityCountdown > 0) return;

        player.InvulnerabilityCountdown = 2; // player.bM = 2 (short delay)
        // Sound stub: "random.orb"
        ((World)World!).PlaySoundAt(this, "random.orb", 0.1f,
            0.5f * ((EntityRandom.NextFloat() - EntityRandom.NextFloat()) * 0.7f + 1.8f));

        player.AddXp(XpValue); // player.k(e)
        IsDead = true;
    }

    // ── Static helpers (spec §1) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>fk.h(int xp)</c> — returns size tier 0–10.
    /// </summary>
    public static int GetSizeTier(int xp)
    {
        for (int i = 0; i < TierThresholds.Length; i++)
            if (xp < TierThresholds[i]) return i;
        return TierThresholds.Length;
    }

    /// <summary>
    /// obf: <c>fk.b(int xp)</c> — rounds down to nearest tier threshold.
    /// </summary>
    public static int RoundDownToTier(int xp)
    {
        int result = 0;
        foreach (int t in TierThresholds)
            if (t <= xp) result = t; else break;
        return result;
    }

    // ── NBT (spec §1) ────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("Health", (short)_health);
        tag.PutShort("Age",    (short)_despawnAge); // "Age" = despawn counter (not _age!)
        tag.PutShort("Value",  (short)XpValue);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        _health     = tag.GetShort("Health");
        _despawnAge = tag.GetShort("Age");
        XpValue     = tag.GetShort("Value");
    }
}
