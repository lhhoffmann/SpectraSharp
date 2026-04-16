using Raylib_cs;
using SpectraEngine.IO;

namespace SpectraEngine.Graphics;

/// <summary>
/// Owns all GPU-side textures for the lifetime of the window.
/// Textures are loaded once and unloaded on <see cref="Dispose"/>.
/// </summary>
public sealed class TextureRegistry : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = [];

    /// <summary>
    /// Loads a texture from an in-memory asset buffer and registers it under <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Lookup key (e.g. <c>"block_1"</c>).</param>
    /// <param name="data">Raw file bytes from <see cref="AssetManager"/>.</param>
    /// <param name="fileExtension">Extension including dot, e.g. <c>".png"</c>.</param>
    public void LoadFromAsset(string name, AssetData data, string fileExtension = ".png")
    {
        Image img = Raylib.LoadImageFromMemory(fileExtension, data.Memory.ToArray());
        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UnloadImage(img);
        _textures[name] = tex;
    }

    /// <summary>
    /// Registers an already-created <see cref="Texture2D"/> under <paramref name="name"/>.
    /// Used by <see cref="TerrainAtlas"/> after cropping individual tiles.
    /// </summary>
    public void Register(string name, Texture2D texture)
        => _textures[name] = texture;

    /// <summary>Returns the texture registered under <paramref name="name"/>, or null.</summary>
    public Texture2D? TryGet(string name)
        => _textures.TryGetValue(name, out Texture2D tex) ? tex : null;

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (Texture2D tex in _textures.Values)
            Raylib.UnloadTexture(tex);
        _textures.Clear();
    }
}
