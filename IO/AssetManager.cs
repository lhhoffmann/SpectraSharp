using System.IO.Compression;

namespace SpectraSharp.IO;

/// <summary>
/// Reads assets at runtime from the user's own game JAR.
/// The JAR is opened read-only, in memory — nothing is extracted to disk.
/// </summary>
public sealed class AssetManager
{
    private static readonly string DefaultJarPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        @".minecraft\versions\1.0\1.0.jar");

    private readonly string _jarPath;

    public AssetManager(string? jarPath = null)
    {
        _jarPath = jarPath ?? DefaultJarPath;

        if (!File.Exists(_jarPath))
            throw new VanillaNotFoundException(_jarPath);

        Console.WriteLine($"[AssetManager] JAR found: {_jarPath}");
    }

    /// <summary>
    /// Extracts a single file from the JAR by its ZIP entry path.
    /// </summary>
    /// <param name="entryPath">Path inside the ZIP, e.g. "terrain.png".</param>
    public AssetData ExtractAsset(string entryPath)
    {
        using ZipArchive zip = ZipFile.OpenRead(_jarPath);

        ZipArchiveEntry entry = zip.GetEntry(entryPath)
            ?? throw new VanillaNotFoundException($"{_jarPath}!{entryPath}");

        byte[] buffer = new byte[entry.Length];
        using Stream stream = entry.Open();
        stream.ReadExactly(buffer);

        return new AssetData(buffer);
    }

    /// <summary>Convenience shortcut for the main texture atlas.</summary>
    public AssetData ExtractTerrainPng() => ExtractAsset("terrain.png");
}

/// <summary>
/// Immutable byte buffer for a single extracted asset.
/// Exposes both <see cref="Span"/> and <see cref="Memory"/> views without copying.
/// </summary>
public sealed class AssetData
{
    private readonly byte[] _data;

    internal AssetData(byte[] data) => _data = data;

    public ReadOnlySpan<byte>   Span   => _data.AsSpan();
    public ReadOnlyMemory<byte> Memory => _data.AsMemory();
    public int                  Length => _data.Length;
}
