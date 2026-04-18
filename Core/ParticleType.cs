namespace SpectraEngine.Core;

/// <summary>
/// Particle name string constants used with <see cref="World.SpawnParticle"/>.
/// The client-side WorldRenderer (afv) maps these names to EntityFX subclasses.
///
/// Names in the Confirmed section are directly observed in decompiled source.
/// Names in the Inferred section follow 1.0 standard conventions.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ParticleSystem_Spec.md
/// </summary>
public static class ParticleType
{
    // ── Confirmed (observed in afv WorldEvent handler) ────────────────────────

    /// <summary>Smoke puff — dispenser fire (event 2000), explosion aftermath (event 2004)</summary>
    public const string Smoke          = "smoke";

    /// <summary>Flame — post-explosion (event 2004)</summary>
    public const string Flame          = "flame";

    /// <summary>Block crack — event 2001; append block ID: "blockcrack_" + blockId</summary>
    public const string BlockCrackBase = "blockcrack_";

    /// <summary>Item icon crack — event 2002/2003; append item ID: "iconcrack_" + itemId</summary>
    public const string IconCrackBase  = "iconcrack_";

    /// <summary>Spell/magic mist — splash potion (event 2002)</summary>
    public const string Spell          = "spell";

    /// <summary>Portal ring — Eye of Ender break (event 2003)</summary>
    public const string Portal         = "portal";

    // ── Inferred (standard 1.0 particle conventions) ─────────────────────────

    public const string Bubble         = "bubble";
    public const string Splash         = "splash";
    public const string Wake           = "wake";
    public const string Suspend        = "suspend";
    public const string DepthSuspend   = "depthsuspend";
    public const string Crit           = "crit";
    public const string MagicCrit      = "magicCrit";
    public const string DripWater      = "dripWater";
    public const string DripLava       = "dripLava";
    public const string SnowballPoof   = "snowballpoof";
    public const string HugeExplosion  = "hugeexplosion";
    public const string LargeExplode   = "largeexplode";
    public const string Explode        = "explode";
    public const string Heart          = "heart";
    public const string AngryVillager  = "angryVillager";
    public const string HappyVillager  = "happyVillager";
    public const string Note           = "note";
    public const string EnchantTable   = "enchantmenttable";
    public const string SnowShovel     = "snowshovel";
    public const string Slime          = "slime";
    public const string RedDust        = "reddust";
    public const string TownAura       = "townaura";

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns "blockcrack_<blockId>" for use in block-break events.</summary>
    public static string BlockCrack(int blockId) => BlockCrackBase + blockId;

    /// <summary>Returns "iconcrack_<itemId>" for use in item-shatter events.</summary>
    public static string IconCrack(int itemId)   => IconCrackBase  + itemId;
}
