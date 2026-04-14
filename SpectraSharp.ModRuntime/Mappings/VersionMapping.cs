using System.IO.Compression;
using System.Text.Json;

namespace SpectraSharp.ModRuntime.Mappings;

/// <summary>
/// Loaded from Data/{version}.json.
/// Describes how Java class/method/field names map to the stub layer for a given MC version.
/// </summary>
public sealed class VersionMapping
{
    public string   Version      { get; init; } = "";
    public bool     Obfuscated   { get; init; }
    public bool     Mojmap       { get; init; }
    public string   StubsVersion { get; init; } = "";
    public string[] Loaders      { get; init; } = [];
    public string[] Fingerprints { get; init; } = [];

    public Dictionary<string, string> Classes { get; init; } = new();
    public Dictionary<string, string> Methods { get; init; } = new();
    public Dictionary<string, string> Fields  { get; init; } = new();

    // ── Human-readable class name → C# stub type (resolved at load time) ─────
    public Dictionary<string, string> ClassToCsStub { get; } = new();
}

/// <summary>
/// Detects Minecraft version from a JAR and loads the corresponding mapping.
/// </summary>
static class VersionDetector
{
    static readonly string MappingDir = Path.Combine(
        AppContext.BaseDirectory, "Mappings", "Data");

    static readonly List<VersionMapping> _all = [];

    static VersionDetector()
    {
        foreach (string file in Directory.GetFiles(MappingDir, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var map = JsonSerializer.Deserialize<VersionMapping>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (map != null) _all.Add(map);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[VersionDetector] Failed to load {file}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Opens the JAR and matches it against known version fingerprints.
    /// Returns null if no match — caller must handle unknown versions.
    /// </summary>
    public static VersionMapping? Detect(string jarPath)
    {
        using var zip = ZipFile.OpenRead(jarPath);
        var entryNames = zip.Entries.Select(e => e.FullName).ToHashSet();

        // Check MANIFEST.MF for loader version keys
        string? manifest = ReadManifest(zip);

        foreach (var mapping in _all.OrderByDescending(m => m.Version))
        {
            bool match = mapping.Fingerprints.All(fp =>
            {
                if (fp.Contains(':'))
                {
                    // "META-INF/MANIFEST.MF:ForgeVersion" → key in manifest
                    string key = fp[(fp.IndexOf(':') + 1)..];
                    return manifest?.Contains(key) == true;
                }
                return entryNames.Contains(fp);
            });

            if (match)
            {
                Console.WriteLine($"[VersionDetector] Detected Minecraft {mapping.Version}" +
                                  $" ({(mapping.Obfuscated ? "obfuscated" : "named")})" +
                                  $" loader: {string.Join('/', mapping.Loaders)}");
                return mapping;
            }
        }

        Console.Error.WriteLine("[VersionDetector] Unknown version — no fingerprint matched.");
        return null;
    }

    static string? ReadManifest(ZipArchive zip)
    {
        var entry = zip.GetEntry("META-INF/MANIFEST.MF");
        if (entry == null) return null;
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
