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
    public static void ExtractAndRegister(
        int             index,
        Image           atlas,
        TextureRegistry registry,
        string          key)
    {
        int col = index % TilesPerRow;
        int row = index / TilesPerRow;

        // ImageCrop modifies in place — work on a copy so the atlas stays intact
        Image tile = Raylib.ImageCopy(atlas);
        Raylib.ImageCrop(ref tile, new Rectangle(col * TileSize, row * TileSize, TileSize, TileSize));

        // Raylib uploads image rows top-down but OpenGL expects bottom-up UV origin —
        // flip vertically so textures appear right-side up on cube faces.
        Raylib.ImageFlipVertical(ref tile);

        Texture2D tex = Raylib.LoadTextureFromImage(tile);
        Raylib.UnloadImage(tile);

        registry.Register(key, tex);
        Console.WriteLine($"[TerrainAtlas] tile {index,3} (col={col}, row={row}) → \"{key}\"");
    }
}
