using Raylib_cs;
using System.Numerics;
using SpectraSharp.IO;

namespace SpectraSharp.Graphics;

/// <summary>
/// Utilities for slicing individual block tiles out of terrain.png.
///
/// The atlas is a 256×256 PNG divided into a 16×16 grid of 16×16 tiles.
/// Tile index mapping: <c>col = index % 16</c>, <c>row = index / 16</c>.
/// </summary>
public static class TerrainAtlas
{
    public const int TileSize    = 16;
    public const int TilesPerRow = 16;

    /// <summary>
    /// Crops tile <paramref name="index"/> from a pre-loaded atlas image, uploads it
    /// to the GPU, and registers the result in <paramref name="registry"/> under
    /// <paramref name="key"/>.
    /// The caller owns <paramref name="atlas"/> and must unload it after all tiles are extracted.
    /// </summary>
    /// <param name="tint">
    /// Optional biome color multiplier.  Pass <c>default</c> (A=0) or white (255,255,255,255)
    /// to skip tinting.  Grass-top (index 0) and leaves (index 52) are stored gray in terrain.png
    /// and must be tinted with the appropriate biome color at load time.
    /// Multiplication: outChannel = (texChannel × tintChannel) / 255.
    /// </param>
    public static void ExtractAndRegister(
        int             index,
        Image           atlas,
        TextureRegistry registry,
        string          key,
        Color           tint = default)
    {
        int col = index % TilesPerRow;
        int row = index / TilesPerRow;

        // ImageCrop modifies in place — work on a copy so the atlas stays intact
        Image tile = Raylib.ImageCopy(atlas);
        Raylib.ImageCrop(ref tile, new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize));

        // Apply biome color multiplier before upload when the tile is stored gray.
        // tint.A == 0 means "no tint requested" (default(Color) sentinel).
        // White (255,255,255,255) is also a no-op but explicit callers may pass it.
        if (tint.A != 0 && (tint.R != 255 || tint.G != 255 || tint.B != 255))
            Raylib.ImageColorTint(ref tile, tint);

        // Raylib uploads image rows top-down but OpenGL expects bottom-up UV origin —
        // flip vertically so textures appear right-side up on cube faces.
        Raylib.ImageFlipVertical(ref tile);

        Texture2D tex = Raylib.LoadTextureFromImage(tile);
        Raylib.UnloadImage(tile);

        registry.Register(key, tex);
        string tintInfo = (tint.A != 0 && (tint.R != 255 || tint.G != 255 || tint.B != 255))
            ? $" tint=({tint.R},{tint.G},{tint.B})"
            : "";
        Console.WriteLine($"[TerrainAtlas] tile {index,3} (col={col}, row={row}) → \"{key}\"{tintInfo}");
    }
}
