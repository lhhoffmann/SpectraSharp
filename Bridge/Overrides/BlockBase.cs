using System.Numerics;
using SpectraSharp.Core;
using SpectraSharp.Core.Mods;

namespace SpectraSharp.Bridge.Overrides;

/// <summary>
/// Abstract base for every hand-written Block override in the compatibility layer.
/// Sits between <see cref="BridgeStubBase"/> and a concrete block class,
/// adding the properties common to all parity blocks:
/// texture atlas index, world position, and tick counter.
///
/// Hand-written overrides always have <see cref="IBridgeStub.Priority"/> = 10,
/// so they beat any generated stub at priority 0.
/// </summary>
public abstract class BlockBase : BridgeStubBase
{
    // ── Identity ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override int Priority => 10;

    /// <summary>Numeric block ID (0–255). Matches the vanilla block ID table.</summary>
    public abstract int BlockId { get; }

    // ── Physics ───────────────────────────────────────────────────────────────

    /// <summary>Hardness controls how long the block takes to break.</summary>
    protected virtual float Hardness => 1.0f;

    /// <summary>Blast resistance reduces damage from explosions.</summary>
    protected virtual float BlastResistance => 1.0f;

    // ── Interaction ───────────────────────────────────────────────────────────

    /// <summary>Called when a player right-clicks the block face.</summary>
    public virtual bool OnUse(IWorld world, object player, int x, int y, int z, Face face)
        => false;

    // ── Drops ─────────────────────────────────────────────────────────────────

    /// <summary>Returns the item stacks dropped when this block is broken.</summary>
    public virtual IEnumerable<SpectraSharp.Core.Mods.ItemStack> GetDrops(int meta, Random rng)
    {
        yield return new SpectraSharp.Core.Mods.ItemStack(BlockId, 1);
    }

    // ── Texture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat tile index into terrain.png (left-to-right, top-to-bottom).
    /// Index 0 = grass top, 1 = stone, 2 = dirt, …
    /// Used for all faces unless a face-specific override is provided.
    /// </summary>
    public abstract int TextureIndex { get; }

    /// <summary>Tile index for the top face. Defaults to <see cref="TextureIndex"/>.</summary>
    public virtual int TextureIndexTop    => TextureIndex;
    /// <summary>Tile index for side faces. Defaults to <see cref="TextureIndex"/>.</summary>
    public virtual int TextureIndexSide   => TextureIndex;
    /// <summary>Tile index for the bottom face. Defaults to <see cref="TextureIndex"/>.</summary>
    public virtual int TextureIndexBottom => TextureIndex;

    /// <summary>Texture registry key for the primary (top) face tile.</summary>
    public string TextureKey       => $"block_{TextureIndex}";
    public string TextureKeyTop    => $"block_{TextureIndexTop}";
    public string TextureKeySide   => $"block_{TextureIndexSide}";
    public string TextureKeyBottom => $"block_{TextureIndexBottom}";

    // ── Rendering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat colour used by the Engine's DrawCube pass until a full material
    /// pipeline exists.  Override in concrete blocks to match the vanilla palette.
    /// </summary>
    public virtual Raylib_cs.Color RenderColor => new(200, 200, 200, 255);

    /// <summary>
    /// Biome color multiplier applied to this block's texture tile at load time.
    /// Default is white (255,255,255,255) = no tint.
    /// Blocks whose terrain.png tile is stored gray (grass, leaves) override this
    /// with the appropriate biome color so the extracted GPU texture appears correct.
    ///
    /// Multiplication formula: outChannel = (texChannel * tintChannel) / 255
    /// Applied once by <see cref="SpectraSharp.Graphics.TerrainAtlas.ExtractAndRegister"/>.
    /// </summary>
    public virtual Raylib_cs.Color BiomeTintColor => new(255, 255, 255, 255);

    // ── World state ───────────────────────────────────────────────────────────

    /// <summary>Block's position in world space (block-grid coordinates).</summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    // ── Tick accounting ───────────────────────────────────────────────────────

    /// <summary>
    /// Total number of fixed ticks received since the engine started.
    /// Incremented inside <see cref="OnTick"/> — use this to verify 20 Hz parity.
    /// </summary>
    public long TickCount { get; private set; }

    /// <inheritdoc/>
    public override void OnTick(double deltaSeconds)
    {
        TickCount++;
        BlockTick(deltaSeconds);
    }

    /// <summary>
    /// Override this in concrete blocks to implement per-tick behaviour
    /// (random updates, scheduled block changes, etc.).
    /// <paramref name="deltaSeconds"/> is always exactly 0.05 s (20 Hz).
    /// </summary>
    protected virtual void BlockTick(double deltaSeconds) { }
}
