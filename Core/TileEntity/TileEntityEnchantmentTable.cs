namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Replica of <c>rq</c> (TileEntityEnchantmentTable) — animated book on enchanting table.
/// Registry ID: "EnchantTable".
///
/// Handles the floating book visual: opening/closing based on player proximity,
/// rotation tracking toward nearest player, page-flip animation.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EnchantingXP_Spec.md §4
/// </summary>
public sealed class TileEntityEnchantmentTable : TileEntity
{
    // ── Animation fields (spec §4 field table) ────────────────────────────────

    /// <summary>obf: <c>a</c> — tick counter (monotonically increasing).</summary>
    public int TickCount;

    /// <summary>obf: <c>b</c> — current book open amount (0.0 = closed, 1.0 = fully open).</summary>
    public float BookOpen;

    /// <summary>obf: <c>j</c> — previous book open amount (for interpolation).</summary>
    public float PrevBookOpen;

    /// <summary>obf: <c>k</c> — target page index offset for page-flip animation.</summary>
    public float PageTarget;

    /// <summary>obf: <c>l</c> — page flip speed/momentum.</summary>
    public float PageSpeed;

    /// <summary>obf: <c>m</c> — current book lift (vertical bounce).</summary>
    public float BookLift;

    /// <summary>obf: <c>n</c> — previous book lift.</summary>
    public float PrevBookLift;

    /// <summary>obf: <c>o</c> — current book rotation (radians).</summary>
    public float BookRotation;

    /// <summary>obf: <c>p</c> — previous book rotation.</summary>
    public float PrevBookRotation;

    /// <summary>obf: <c>q</c> — target yaw toward nearest player.</summary>
    public float TargetYaw;

    // ── Tick (spec §4) ────────────────────────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;

        TickCount++;

        // Save previous state for interpolation
        PrevBookLift     = BookLift;
        PrevBookRotation = BookRotation;

        // Scan for nearest player within 3.0 blocks of block center
        var aabb = AxisAlignedBB.GetFromPool(
            X + 0.5 - 3.0, Y + 0.5 - 3.0, Z + 0.5 - 3.0,
            X + 0.5 + 3.0, Y + 0.5 + 3.0, Z + 0.5 + 3.0);

        var players = World.GetEntitiesWithinAABB<EntityPlayer>(aabb);

        EntityPlayer? nearest     = null;
        double        nearestDistSq = double.MaxValue;

        foreach (var p in players)
        {
            if (!p.IsEntityAlive()) continue;
            double dx = X + 0.5 - p.PosX;
            double dz = Z + 0.5 - p.PosZ;
            double d2 = dx * dx + dz * dz;
            if (d2 < nearestDistSq) { nearestDistSq = d2; nearest = p; }
        }

        if (nearest != null)
        {
            // Track player — target yaw = atan2(dz, dx) from block center to player
            double dxToPlayer = nearest.PosX - (X + 0.5);
            double dzToPlayer = nearest.PosZ - (Z + 0.5);
            TargetYaw = (float)Math.Atan2(dzToPlayer, dxToPlayer);

            // Open the book
            BookLift += 0.1f;

            // Page flip: every nextInt(40)==0 or if book is not yet half-open
            if (World.Random.NextInt(40) == 0 || BookLift < 0.5f)
                PageTarget += World.Random.NextInt(4) - World.Random.NextInt(4);
        }
        else
        {
            // No player — drift and close
            TargetYaw += 0.02f;
            BookLift  -= 0.1f;
        }

        // Clamp book lift to [0.0, 1.0]
        BookLift = Math.Clamp(BookLift, 0.0f, 1.0f);

        // Smoothly rotate toward target, keeping angle in (-π, π]
        float delta = TargetYaw - BookRotation;
        // Wrap delta to [-π, π]
        while (delta >= MathF.PI)  delta -= MathF.PI * 2.0f;
        while (delta < -MathF.PI) delta += MathF.PI * 2.0f;
        BookRotation += delta * 0.4f;

        // Page flip: save previous, then approach PageTarget
        PrevBookOpen = BookOpen;
        PageSpeed   += (PageTarget - BookOpen) * 0.4f;
        PageSpeed    = Math.Clamp(PageSpeed, -0.2f, 0.2f);
        BookOpen    += PageSpeed;
    }
}
