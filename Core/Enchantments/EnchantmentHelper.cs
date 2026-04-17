namespace SpectraEngine.Core.Enchantments;

/// <summary>
/// Replica of <c>ml</c> (EnchantmentHelper) — static helper for slot-level calculation
/// and enchantment selection.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §6
/// </summary>
public static class EnchantmentHelper
{
    // ── Slot level formula (spec §6 — ml.a(Random, int, int, dk)) ────────────

    /// <summary>
    /// obf: <c>ml.a(Random rng, int slot, int bookshelfBonus, dk item)</c>
    /// Returns the enchantment level cost displayed for the given slot (0=top, 2=bottom).
    ///
    /// Returns 0 if the item has no enchantability.
    /// </summary>
    public static int SlotLevel(JavaRandom rng, int slot, int bookshelfBonus, ItemStack item)
    {
        int enchantability = item.GetItem()?.GetEnchantability() ?? 0;
        if (enchantability <= 0) return 0;

        if (bookshelfBonus > 30) bookshelfBonus = 30;

        int @base  = 1 + rng.NextInt(bookshelfBonus / 2 + 1) + rng.NextInt(bookshelfBonus + 1);
        int noise  = rng.NextInt(5) + @base;

        return slot switch
        {
            0 => (noise >> 1) + 1,
            1 => noise * 2 / 3 + 1,
            2 => noise,
            _ => 0,
        };
    }

    // ── Enchantment selection (spec §6 — ml.a(Random, dk, int)) ─────────────

    /// <summary>
    /// obf: <c>ml.a(Random rng, dk item, int power)</c>
    /// Returns a weighted-random list of <see cref="EnchantmentData"/> to apply,
    /// or an empty list if none can be selected.
    /// </summary>
    public static List<EnchantmentData> SelectEnchantments(JavaRandom rng, ItemStack item, int power)
    {
        var result = new List<EnchantmentData>();

        int enchantability = item.GetItem()?.GetEnchantability() ?? 0;
        if (enchantability <= 0) return result;

        // Randomised power adjustment (spec §6)
        int @base     = 1 + rng.NextInt(enchantability / 2 + 1) + rng.NextInt(enchantability / 2 + 1);
        int adjusted  = @base + power;
        float fuzz    = (rng.NextFloat() + rng.NextFloat() - 1.0f) * 0.25f;
        int finalPower = (int)((float)adjusted * (1.0f + fuzz) + 0.5f);

        // Build candidate list: all enchantments applicable at finalPower
        var candidates = BuildCandidates(item, finalPower);
        if (candidates.Count == 0) return result;

        // Pick first enchantment (weighted)
        EnchantmentData first = WeightedRandom(rng, candidates);
        result.Add(first);

        // Try to add more (halving threshold each time)
        int threshold = finalPower / 2;
        while (rng.NextInt(50) <= threshold)
        {
            threshold >>= 1;

            // Remove enchantments incompatible with already-chosen ones
            candidates.RemoveAll(ed =>
            {
                foreach (var chosen in result)
                    if (!ed.Enchantment.IsCompatibleWith(chosen.Enchantment)
                        || !chosen.Enchantment.IsCompatibleWith(ed.Enchantment))
                        return true;
                return false;
            });

            if (candidates.Count == 0) break;
            result.Add(WeightedRandom(rng, candidates));
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the list of (enchantment, level) pairs where finalPower falls within
    /// [enchantment.MinPower(level), enchantment.MaxPower(level)] and the enchantment
    /// can be applied to the item.
    /// </summary>
    private static List<EnchantmentData> BuildCandidates(ItemStack item, int finalPower)
    {
        var list = new List<EnchantmentData>();

        foreach (var enc in Enchantment.EnchantmentsList)
        {
            if (enc == null) continue;
            if (!enc.CanApplyTo(item)) continue;

            for (int lvl = enc.GetMaxLevel(); lvl >= enc.GetMinLevel(); lvl--)
            {
                if (finalPower >= enc.GetMinPower(lvl) && finalPower <= enc.GetMaxPower(lvl))
                {
                    list.Add(new EnchantmentData(enc, lvl));
                    break; // highest applicable level wins
                }
            }
        }

        return list;
    }

    /// <summary>Picks a weighted-random element from a non-empty candidate list.</summary>
    private static EnchantmentData WeightedRandom(JavaRandom rng, List<EnchantmentData> candidates)
    {
        int totalWeight = 0;
        foreach (var ed in candidates) totalWeight += ed.Enchantment.Weight;

        int roll = rng.NextInt(totalWeight);
        foreach (var ed in candidates)
        {
            roll -= ed.Enchantment.Weight;
            if (roll < 0) return ed;
        }

        return candidates[^1]; // fallback (should not reach here)
    }
}
