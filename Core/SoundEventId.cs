namespace SpectraEngine.Core;

/// <summary>
/// World-event ID constants dispatched via <see cref="IWorld.PlayAuxSFX"/>.
/// Handled by <c>afv</c> (WorldRenderer) on the client side.
/// Source spec: Documentation/VoxelCore/Parity/Specs/SoundManager_Spec.md — WorldEvent table.
/// </summary>
public static class SoundEventId
{
    // ── Sound-only events ─────────────────────────────────────────────────────

    /// <summary>Dispenser fires — plays "random.click" vol 1.0 pitch 1.0</summary>
    public const int DispenserFire  = 1000;

    /// <summary>Dispenser empty — plays "random.click" vol 1.0 pitch 1.2</summary>
    public const int DispenserEmpty = 1001;

    /// <summary>Bow fired — plays "random.bow" vol 1.0 pitch 1.2</summary>
    public const int BowFired       = 1002;

    /// <summary>Door toggle — data=0 → "random.door_open"; data=1 → "random.door_close"</summary>
    public const int DoorToggle     = 1003;

    /// <summary>Fire/lava fizz — plays "random.fizz" vol 0.5 pitch ~2.6</summary>
    public const int FireFizz       = 1004;

    /// <summary>Record inserted/stopped in jukebox — data=item ID (0 = stop)</summary>
    public const int JukeboxRecord  = 1005;

    /// <summary>Ghast charges fireball — plays "mob.ghast.charge" vol 10.0</summary>
    public const int GhastCharge    = 1007;

    /// <summary>Ghast shoots fireball — plays "mob.ghast.fireball" vol 10.0</summary>
    public const int GhastShoot     = 1008;

    /// <summary>Ghast fireball impact — plays "mob.ghast.fireball" vol 1.0</summary>
    public const int GhastImpact    = 1009;

    // ── Particle-only events ──────────────────────────────────────────────────

    /// <summary>Smoke puff from dispenser — particles only, no sound</summary>
    public const int DispenserSmoke = 2000;

    /// <summary>Block break — particles + block step sound with "dig." prefix; data = block ID</summary>
    public const int BlockBreak     = 2001;

    /// <summary>Splash potion shatter — plays "random.glass"; data = potion meta</summary>
    public const int PotionSplash   = 2002;

    /// <summary>Eye of Ender breaks — particles only, no sound</summary>
    public const int EyeOfEnderBreak = 2003;

    /// <summary>Explosion smoke and flame particles — particles only</summary>
    public const int ExplosionSmoke = 2004;
}

/// <summary>
/// Known sound name string constants used in <see cref="IWorld.PlaySoundAt"/> calls.
/// Source spec: Documentation/VoxelCore/Parity/Specs/SoundManager_Spec.md — Sound name table.
/// </summary>
public static class SoundName
{
    // ── UI / Random ───────────────────────────────────────────────────────────
    public const string Click      = "random.click";
    public const string Bow        = "random.bow";
    public const string DoorOpen   = "random.door_open";
    public const string DoorClose  = "random.door_close";
    public const string Fizz       = "random.fizz";
    public const string Glass      = "random.glass";
    public const string Fuse       = "random.fuse";
    public const string Pop        = "random.pop";
    public const string Burp       = "random.burp";
    public const string Splash     = "random.splash";

    // ── Liquid ────────────────────────────────────────────────────────────────
    public const string Water      = "liquid.water";

    // ── Footsteps ─────────────────────────────────────────────────────────────
    public const string StepGravel = "step.gravel";
    public const string StepStone  = "step.stone";
    public const string StepWood   = "step.wood";
    public const string StepGrass  = "step.grass";
    public const string StepSand   = "step.sand";
    public const string StepCloth  = "step.cloth";
    public const string StepSnow   = "step.snow";

    // ── Portal ────────────────────────────────────────────────────────────────
    public const string PortalTrigger = "portal.trigger";
    public const string PortalTravel  = "portal.travel";
}
