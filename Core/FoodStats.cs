namespace SpectraSharp.Core;

/// <summary>
/// Player food statistics. Replica of <c>eq</c> (FoodStats).
///
/// Persisted only if "foodLevel" key is present in the player compound.
/// If absent, construction defaults are used (foodLevel=20, saturation=5.0).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemFood_Spec.md §5
/// </summary>
public sealed class FoodStats
{
    // ── Fields (spec §5.1) ───────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — hunger level 0–20. Default 20.</summary>
    public int FoodLevel = 20;

    /// <summary>obf: <c>b</c> — saturation level (0.0–foodLevel). Default 5.0F.</summary>
    public float FoodSaturationLevel = 5.0f;

    /// <summary>obf: <c>c</c> — exhaustion accumulator (0.0–40.0). Default 0.</summary>
    public float FoodExhaustionLevel;

    /// <summary>obf: <c>d</c> — heal/starvation tick counter (shared). Default 0.</summary>
    public int FoodTickTimer;

    /// <summary>obf: <c>e</c> — previous foodLevel snapshot (set at tick start). Not persisted.</summary>
    public int PreviousFoodLevel = 20;

    // ── Eat (spec §5.2) ──────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(int heal, float satMod)</c> — called after eating. Restores hunger and saturation.
    /// Saturation cap uses the NEW (post-restore) foodLevel (quirk 1).
    /// </summary>
    public void AddFood(int healAmount, float saturationModifier)
    {
        FoodLevel            = Math.Min(FoodLevel + healAmount, 20);
        FoodSaturationLevel  = Math.Min(FoodSaturationLevel + healAmount * saturationModifier * 2.0f, FoodLevel);
    }

    // ── Tick (spec §5.3) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(vi player)</c> — per-tick update. Called once per tick by EntityPlayer.
    /// Handles exhaustion drain → saturation drain → hunger drain → heal/starvation.
    /// </summary>
    public void Tick(EntityPlayer player)
    {
        PreviousFoodLevel = FoodLevel;

        int difficulty = player.World?.Difficulty ?? 2;

        // Exhaustion drain: every 4 exhaustion units consume 1 saturation or 1 hunger.
        if (FoodExhaustionLevel > 4.0f)
        {
            FoodExhaustionLevel -= 4.0f;
            if (FoodSaturationLevel > 0.0f)
                FoodSaturationLevel = Math.Max(FoodSaturationLevel - 1.0f, 0.0f);
            else if (difficulty > 0)                  // not Peaceful
                FoodLevel = Math.Max(FoodLevel - 1, 0);
        }

        // Healing check: well-fed regeneration (food >= 18 and health < max).
        if (FoodLevel >= 18 && player.GetHealth() < player.GetMaxHealth())
        {
            FoodTickTimer++;
            if (FoodTickTimer >= 80)
            {
                player.Heal(1);
                FoodTickTimer = 0;
            }
        }
        // Starvation check: food = 0.
        else if (FoodLevel <= 0)
        {
            FoodTickTimer++;
            if (FoodTickTimer >= 80)
            {
                int health = player.GetHealth();
                if (health > 10 || difficulty >= 3 || (health > 1 && difficulty >= 2))
                    player.AttackEntityFrom(DamageSource.Starve, 1);
                FoodTickTimer = 0;
            }
        }
        else
        {
            // Mid-range: reset counter so neither window accumulates.
            FoodTickTimer = 0;
        }
    }

    // ── AddExhaustion (spec §5.4) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(float exhaustion)</c> — adds exhaustion, capped at 40.0F.
    /// </summary>
    public void AddExhaustion(float exhaustion)
    {
        FoodExhaustionLevel = Math.Min(FoodExhaustionLevel + exhaustion, 40.0f);
    }

    // ── Queries (spec §5.5–5.8) ───────────────────────────────────────────────

    /// <summary>obf: <c>c()</c> — returns true if foodLevel &lt; 20 (player can eat).</summary>
    public bool IsHungry() => FoodLevel < 20;

    /// <summary>obf: <c>a()</c> — getFoodLevel.</summary>
    public int GetFoodLevel() => FoodLevel;

    /// <summary>obf: <c>b()</c> — getPreviousFoodLevel (snapshot from tick start).</summary>
    public int GetPreviousFoodLevel() => PreviousFoodLevel;

    /// <summary>obf: <c>d()</c> — getSaturation.</summary>
    public float GetSaturation() => FoodSaturationLevel;

    // ── NBT (spec §5.1) ──────────────────────────────────────────────────────

    /// <summary>
    /// Writes food stats directly into <paramref name="tag"/> (no wrapper compound).
    /// Spec: <c>eq.b(ik tag)</c>.
    /// </summary>
    public void WriteToNbt(Nbt.NbtCompound tag)
    {
        tag.PutInt("foodLevel",             FoodLevel);
        tag.PutFloat("foodSaturationLevel", FoodSaturationLevel);
        tag.PutFloat("foodExhaustionLevel", FoodExhaustionLevel);
        tag.PutInt("foodTickTimer",         FoodTickTimer);
    }

    /// <summary>
    /// Reads food stats from <paramref name="tag"/>. Only called when "foodLevel" key is
    /// present. Spec: <c>eq.a(ik tag)</c>.
    /// </summary>
    public void ReadFromNbt(Nbt.NbtCompound tag)
    {
        FoodLevel           = tag.GetInt("foodLevel");
        FoodSaturationLevel = tag.GetFloat("foodSaturationLevel");
        FoodExhaustionLevel = tag.GetFloat("foodExhaustionLevel");
        FoodTickTimer       = tag.GetInt("foodTickTimer");
    }
}
