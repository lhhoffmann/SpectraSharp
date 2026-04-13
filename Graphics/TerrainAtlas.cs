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
    /// Crops tile <paramref name="index"/> from the full atlas image, uploads it
    /// to the GPU, and registers the result in <paramref name="registry"/> under
    /// <paramref name="key"/>.
    /// </summary>
    public static void ExtractAndRegister(
        int             index,
        AssetData       atlasData,
        TextureRegistry registry,
        string          key)
    {
        int col = index % TilesPerRow;
        int row = index / TilesPerRow;

        Image full = Raylib.LoadImageFromMemory(".png", atlasData.Memory.ToArray());

        Rectangle rect = new(
            col * TileSize,
            row * TileSize,
            TileSize,
            TileSize);

        Raylib.ImageCrop(ref full, rect);

        Texture2D tile = Raylib.LoadTextureFromImage(full);
        Raylib.UnloadImage(full);

        registry.Register(key, tile);
        Console.WriteLine($"[TerrainAtlas] Extracted tile {index} (col={col}, row={row}) → \"{key}\"");
    }
}
