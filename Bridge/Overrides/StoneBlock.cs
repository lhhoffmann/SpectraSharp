namespace SpectraEngine.Bridge.Overrides;

/// <summary>
/// Hand-written parity override keyed to <c>net.minecraft.src.BlockStone</c>.
///
/// In the 1.0 reference engine, stone occupies tile index 1 in terrain.png
/// (row 0, column 1 on a 256×256 atlas with 16×16 tiles).
///
/// This class proves the full Bridge pipeline:
///   1. It is discovered automatically via <see cref="BridgeRegistry"/> at boot.
///   2. Its <see cref="BlockBase.TextureKey"/> drives the TerrainAtlas extraction.
///   3. Its <see cref="BlockTick"/> is called exactly 20 times per second by the
///      Engine's FixedUpdate, mirroring Java's random-tick / update-tick rhythm.
///
/// Priority = 10 ensures this beats any future generated stub at priority 0.
/// </summary>
public sealed class StoneBlock : BlockBase
{
    // ── IBridgeStub identity ──────────────────────────────────────────────────

    public override int    BlockId       => 1;
    public override string JavaClassName => "net.minecraft.src.BlockStone";

    // ── Texture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Atlas tile index 1 = stone (row 0, column 1 of terrain.png).
    /// Drives <see cref="BlockBase.TextureKey"/> → <c>"block_1"</c>.
    /// </summary>
    public override int TextureIndex => 1;

    // ── Block colour (used when DrawCube renders without a full material) ──────

    /// <summary>
    /// Approximate stone mid-tone sampled from the vanilla palette.
    /// </summary>
    public override Raylib_cs.Color RenderColor => new(125, 125, 125, 255);

    // ── Tick logic ────────────────────────────────────────────────────────────

    /// <summary>
    /// Stone has no random-tick behaviour in the reference engine — this override
    /// exists purely to prove that the 20 Hz tick reaches the Bridge layer.
    /// Every 100 ticks (~5 s) it logs a heartbeat to stdout.
    /// </summary>
    protected override void BlockTick(double deltaSeconds)
    {
        if (TickCount % 100 == 0)
        {
            Console.WriteLine(
                $"[StoneBlock] tick #{TickCount,6}  " +
                $"(Δ={deltaSeconds * 1000:F1} ms, " +
                $"elapsed ≈ {TickCount / 20.0:F1} s)");
        }
    }
}
