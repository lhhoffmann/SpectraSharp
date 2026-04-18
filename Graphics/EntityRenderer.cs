using System.Numerics;
using SpectraEngine.Core;

namespace SpectraEngine.Graphics;

/// <summary>
/// Replica of <c>adt</c> (EntityRenderer) — first-person perspective controller.
/// Manages the projection matrix, camera positioning, view bobbing, and hand/item rendering.
/// Does NOT render world geometry — that is <see cref="WorldRenderer"/> (<c>afv</c>).
///
/// Render pass order each frame:
///   1. Sky dome
///   2. World geometry (via WorldRenderer)
///   3. Weather (rain/snow)
///   4. Hand / held item (via ItemRenderer <c>n</c>)
///   5. HUD (GuiIngame)
///   6. GUI screen (if open)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityRenderer_Spec.md
/// </summary>
public class EntityRenderer
{
    // ── Constants (spec §3.1) ─────────────────────────────────────────────────

    private const float NearPlane  = 0.05f;
    private const float FarPlane   = 512.0f;
    private const float BaseFov    = 70.0f; // degrees

    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>B/C</c> — FOV modifier (animated, lerped each frame).</summary>
    public float FovModifier     = 4.0f;
    public float FovModifierPrev = 4.0f;

    // ── Dependencies ─────────────────────────────────────────────────────────

    public readonly ItemRenderer ItemRenderer;

    public EntityRenderer(ItemRenderer itemRenderer)
    {
        ItemRenderer = itemRenderer;
    }

    // ── Projection matrix (spec §3.1) ─────────────────────────────────────────

    /// <summary>
    /// Builds a perspective projection matrix. Equivalent to <c>gluPerspective</c>.
    /// </summary>
    public static Matrix4x4 CreateProjection(float fovDegrees, float aspectRatio)
    {
        float fovRad = fovDegrees * MathF.PI / 180.0f;
        return Matrix4x4.CreatePerspectiveFieldOfView(fovRad, aspectRatio, NearPlane, FarPlane);
    }

    /// <summary>Computes the effective FOV for the current frame.</summary>
    public float ComputeFov(EntityPlayer? player, float partialTick)
    {
        // Stub: sprint FOV, potion FOV modifiers not yet applied.
        return BaseFov;
    }

    // ── Camera position (spec §3.2) ───────────────────────────────────────────

    /// <summary>
    /// Returns the camera world position: player feet + eye height (1.62F).
    /// </summary>
    public static Vector3 GetCameraPosition(EntityPlayer player, float partialTick)
    {
        double x = player.PosX;
        double y = player.PosY + player.PlayerEyeHeight;
        double z = player.PosZ;
        return new Vector3((float)x, (float)y, (float)z);
    }

    // ── View bobbing (spec §3.3) ──────────────────────────────────────────────

    // Animated float trackers — stub; full bob implementation pending.
    public float BobAmountH;
    public float BobAmountV;
    public float BobTilt;
}

/// <summary>
/// Replica of <c>n</c> (ItemRenderer / hand renderer).
/// Renders the held item in first-person view.
/// Delegates to <see cref="RenderBlocks"/> for block items.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityRenderer_Spec.md §ItemRenderer
/// </summary>
public class ItemRenderer
{
    public readonly RenderBlocks BlockRenderer;

    public ItemRenderer(RenderBlocks blockRenderer)
    {
        BlockRenderer = blockRenderer;
    }

    /// <summary>
    /// obf: <c>n.a(nq, dk, int)</c> — renders the held item for render pass <paramref name="pass"/>.
    /// Stub — full implementation pending.
    /// </summary>
    public void RenderHeldItem(LivingEntity entity, ItemStack? heldItem, int pass)
    {
        // Stub — block items use BlockRenderer; tool items use 2D sprite; no item → fist.
    }
}
