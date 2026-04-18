namespace SpectraEngine.Core;

/// <summary>
/// Immutable input state captured once per render frame by the render thread.
/// Written atomically to <c>Engine._inputSnapshot</c>; read by the game thread
/// at the start of each <c>FixedUpdate</c> tick.
/// </summary>
public sealed class InputSnapshot
{
    public readonly float Forward;
    public readonly float Strafe;
    public readonly bool  Jump;
    public readonly bool  Sneak;
    public readonly float MouseDX;
    public readonly float MouseDY;

    /// <summary>Left mouse button currently held down.</summary>
    public readonly bool LeftDown;

    /// <summary>Left button pressed since last tick (latched, cleared after tick).</summary>
    public readonly bool LeftPressed;

    /// <summary>Left button released since last tick (latched, cleared after tick).</summary>
    public readonly bool LeftReleased;

    /// <summary>Right button pressed since last tick (latched, cleared after tick).</summary>
    public readonly bool RightPressed;

    public InputSnapshot() { }

    public InputSnapshot(float forward, float strafe, bool jump, bool sneak,
                         float mouseDX, float mouseDY,
                         bool leftDown, bool leftPressed, bool leftReleased, bool rightPressed)
    {
        Forward      = forward;
        Strafe       = strafe;
        Jump         = jump;
        Sneak        = sneak;
        MouseDX      = mouseDX;
        MouseDY      = mouseDY;
        LeftDown     = leftDown;
        LeftPressed  = leftPressed;
        LeftReleased = leftReleased;
        RightPressed = rightPressed;
    }
}
