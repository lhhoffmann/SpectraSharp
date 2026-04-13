namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>kq</c> (IBlockAccess) — read-only interface over world data.
/// Used by Block rendering and bounds-query methods without depending on the
/// concrete World class. World (<c>ry</c>) implements this interface.
///
/// Methods marked [UNCERTAIN] have unconfirmed semantics from Block base class
/// usage alone — see spec §8 Open Questions.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/IBlockAccess_Spec.md
/// </summary>
public interface IBlockAccess
{
    // ── Confirmed methods ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: a(x,y,z) — block ID at position. 0 = air.
    /// Confirmed usage: Block.CanReplace.
    /// </summary>
    int GetBlockId(int x, int y, int z);

    /// <summary>
    /// obf: a(x,y,z,int) — combined light value at position given the block's own
    /// emission value. Returns a packed int used in lighting calculations.
    /// Confirmed usage: Block.GetLightBrightness.
    /// </summary>
    int GetLightValue(int x, int y, int z, int blockLightEmission);

    /// <summary>
    /// obf: b(x,y,z,int) — brightness float (0.0–1.0) at position given the block's
    /// own emission value. Used for ambient occlusion.
    /// Confirmed usage: Block.GetAmbientOcclusionLightValue.
    /// </summary>
    float GetBrightness(int x, int y, int z, int blockLightEmission);

    /// <summary>
    /// obf: d(x,y,z) — block metadata (0–15) at position.
    /// Confirmed usage: Block.GetTextureForFaceInWorld.
    /// </summary>
    int GetBlockMetadata(int x, int y, int z);

    /// <summary>
    /// obf: e(x,y,z) — Material of the block at position.
    /// Confirmed usage: Block.IsNormalCube (calls .IsSolid() on result).
    /// </summary>
    Material GetBlockMaterial(int x, int y, int z);

    /// <summary>
    /// obf: f(x,y,z) — true if the block at position is a fully opaque solid cube.
    /// Confirmed usage: Block.ShouldSideBeRendered (face render if adjacent NOT opaque).
    /// </summary>
    bool IsOpaqueCube(int x, int y, int z);

    /// <summary>
    /// obf: g(x,y,z) — true if the block at position is wet / submerged in liquid.
    /// Confirmed usage: Block.GetSlipperiness.
    /// </summary>
    bool IsWet(int x, int y, int z);

    // ── Inferred / uncertain methods [UNCERTAIN] ──────────────────────────────

    /// <summary>
    /// obf: b(x,y,z) — returns a TileEntity (bq) or null. Inferred type.
    /// [UNCERTAIN] — semantics not confirmed from Block base class. Requires bq spec.
    /// </summary>
    object? GetTileEntity(int x, int y, int z);

    /// <summary>
    /// obf: c(x,y,z) — returns a float at position. Possibly sky-light or biome value.
    /// [UNCERTAIN] — not observed in Block base class.
    /// </summary>
    float GetUnknownFloat(int x, int y, int z);

    /// <summary>
    /// obf: h(x,y,z) — returns a boolean at position. Semantics unknown.
    /// [UNCERTAIN] — not observed in Block base class.
    /// </summary>
    bool GetUnknownBool(int x, int y, int z);

    /// <summary>
    /// obf: a() — returns a context object (vh). Possibly WorldChunkManager.
    /// [UNCERTAIN] — not observed in Block base class. Requires vh spec.
    /// </summary>
    object GetContextObject();

    /// <summary>
    /// obf: b() — returns an int with no coordinates. Possibly world height (128).
    /// [UNCERTAIN] — not observed in Block base class.
    /// </summary>
    int GetHeight();
}
