namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>di</c> (EntityPlayerSP) — the concrete single-player client-side player.
/// Extends <see cref="EntityPlayer"/> with movement input, sprint double-tap, sneak,
/// sleep timer, and dimension-travel dispatch.
///
/// Movement path (spec: PlayerMovement_Spec.md):
///   - <c>MovementInput.Forward/Strafe</c> feed directly into <c>AiForward</c>/<c>AiStrafe</c>.
///   - Sneak: both axes scaled ×0.2 before forwarding (effective speed 0.02).
///   - Sprint double-tap: two forward presses within 7 ticks; guards: on-ground, food>6, not sneaking.
///   - Sprint cancels when forward &lt;0.8, horizontal collision, or food≤6.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityPlayerSP_Spec.md
///              Documentation/VoxelCore/Parity/Specs/PlayerMovement_Spec.md
/// </summary>
public class EntityPlayerSP : EntityPlayer
{
    // ── Movement input (obf: b / agn on di) ──────────────────────────────────

    /// <summary>
    /// obf: <c>b</c> — raw movement input sampled each tick from key state.
    /// </summary>
    public MovementInput MovementInput = new();

    // ── Sprint cooldown (obf: d on di) ────────────────────────────────────────

    /// <summary>
    /// obf: <c>d</c> — ticks remaining before sprinting is allowed again.
    /// Decrements each tick.
    /// </summary>
    public int SprintCooldown;

    // ── Sprint double-tap window (spec §Sprint Activation) ────────────────────

    /// <summary>
    /// Ticks remaining in the double-tap-forward window (max 7).
    /// If a second forward press occurs before this reaches 0, sprinting activates.
    /// </summary>
    private int _sprintDoubleTapTimer;

    /// <summary>True during the tick when the forward key was just pressed.</summary>
    private bool _prevForwardHeld;

    // ── Engine reference (obf: c on di) ───────────────────────────────────────

    /// <summary>
    /// obf: <c>c</c> — reference to the engine/Minecraft singleton.
    /// Used to dispatch block-break and interaction through <see cref="ItemInWorldManager"/>.
    /// </summary>
    public readonly Engine? EngineRef;

    // ── Constructor ───────────────────────────────────────────────────────────

    public EntityPlayerSP(World world, Engine? engine = null) : base(world)
    {
        EngineRef = engine;
        Dimension = 0;
    }

    // ── Dimension travel ─────────────────────────────────────────────────────

    public override void TravelToDimension(int dimensionId)
    {
        Dimension = dimensionId;
    }

    // ── Per-tick ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called once per 20 Hz tick. Implements the full <c>di.n()</c> input path:
    /// sneak scaling, sprint activation/cancellation, movement forwarding, jump.
    /// </summary>
    public override void Tick()
    {
        float fwd    = MovementInput.ForwardSpeed;
        float strafe = MovementInput.StrafeSpeed;
        bool  sneak  = MovementInput.IsSneaking;
        bool  jump   = MovementInput.IsJumping;

        // Sneak: scale both axes to 20% (spec §Sneak)
        if (sneak)
        {
            fwd    *= 0.2f;
            strafe *= 0.2f;
        }

        // Sprint activation: double-tap-forward detection (spec §Sprint Activation)
        bool forwardHeld = fwd >= 0.8f;
        if (forwardHeld && !_prevForwardHeld)
        {
            // Forward key just pressed this tick
            if (_sprintDoubleTapTimer > 0
                && OnGround
                && !sneak
                && SprintCooldown == 0
                && FoodStats.FoodLevel > 6f)
            {
                IsSprinting = true;
            }
            _sprintDoubleTapTimer = 7; // reset double-tap window
        }
        _prevForwardHeld = forwardHeld;

        if (_sprintDoubleTapTimer > 0) _sprintDoubleTapTimer--;

        // Sprint cancellation (spec §Sprint Cancellation)
        if (IsSprinting && (fwd < 0.8f || IsCollidedHorizontally || FoodStats.FoodLevel <= 6f))
            IsSprinting = false;

        // Push input into LivingEntity movement slots — base.Tick reads them at step 8
        AiForward   = fwd;
        AiStrafe    = strafe;
        WantsToJump = jump;
        IsSneaking  = sneak;

        // base.Tick() also calls EntityPlayer.Tick() which resets GroundSpeed for sprint
        base.Tick();

        // Jump is edge-triggered — clear after base.Tick so held-space doesn't repeat
        MovementInput.IsJumping = false;

        if (SprintCooldown > 0) SprintCooldown--;

        if (IsSleeping && SleepTimer < 100) SleepTimer++;
    }
}

/// <summary>
/// Holds the raw per-tick movement input from keyboard/gamepad polling.
/// Replica of <c>agn</c> (PlayerMovementInput).
/// </summary>
public sealed class MovementInput
{
    /// <summary>Strafe: −1 = left, +1 = right.</summary>
    public float StrafeSpeed;

    /// <summary>Forward: +1 = forward, −1 = backward.</summary>
    public float ForwardSpeed;

    /// <summary>True if jump was pressed this tick.</summary>
    public bool IsJumping;

    /// <summary>True if sneak is held.</summary>
    public bool IsSneaking;

    public void Reset()
    {
        StrafeSpeed  = 0f;
        ForwardSpeed = 0f;
        IsJumping    = false;
        IsSneaking   = false;
    }
}
