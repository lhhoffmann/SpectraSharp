using System.IO.Compression;
using System.Text.Json;

namespace SpectraEngine.ModRuntime.Mappings;

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
    ///
    /// Fingerprint syntax:
    ///   "some/path/Foo.class"        → file entry must exist in JAR
    ///   "META-INF/MANIFEST.MF:Key"  → MANIFEST.MF must contain the key string
    ///   "bytecode:cpw/mods/fml"     → any .class constant pool must contain the pattern
    ///   "file:fabric.mod.json"       → file entry exists (alias for plain path)
    /// </summary>
    public static VersionMapping? Detect(string jarPath)
    {
        using var zip = ZipFile.OpenRead(jarPath);
        var entryNames = zip.Entries.Select(e => e.FullName).ToHashSet();
        string? manifest = ReadManifest(zip);

        foreach (var mapping in _all.OrderByDescending(m => m.Version))
        {
            bool match = mapping.Fingerprints.All(fp => MatchFingerprint(fp, zip, entryNames, manifest));

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

    static bool MatchFingerprint(string fp, ZipArchive zip,
                                 HashSet<string> entryNames, string? manifest)
    {
        if (fp.StartsWith("bytecode:", StringComparison.Ordinal))
        {
            // Scan class-file constant pools for the given string pattern.
            // Java constant pool UTF-8 entries are stored as raw ASCII/MUTF-8 bytes,
            // so a simple byte search works for ASCII class names.
            string pattern = fp[9..];
            return ScanBytecodesForPattern(zip, pattern);
        }

        if (fp.StartsWith("manifest:", StringComparison.Ordinal))
        {
            string key = fp[9..];
            return manifest?.Contains(key, StringComparison.OrdinalIgnoreCase) == true;
        }

        // Legacy colon syntax: "META-INF/MANIFEST.MF:Key"
        int colon = fp.IndexOf(':');
        if (colon > 0 && !fp.StartsWith("file:", StringComparison.Ordinal))
        {
            string key = fp[(colon + 1)..];
            return manifest?.Contains(key) == true;
        }

        // Plain file path (strip "file:" prefix if present)
        string path = fp.StartsWith("file:", StringComparison.Ordinal) ? fp[5..] : fp;
        return entryNames.Contains(path);
    }

    /// <summary>
    /// Searches class-file constant pools in the JAR for <paramref name="pattern"/>
    /// as an ASCII byte sequence. Stops after the first match.
    ///
    /// This works because Java .class files store class/field/method names as
    /// raw UTF-8 strings in the constant pool — readable without full CP parsing.
    /// </summary>
    static bool ScanBytecodesForPattern(ZipArchive zip, string pattern)
    {
        byte[] needle = System.Text.Encoding.ASCII.GetBytes(pattern);
        // Scan up to 20 class files — enough to find a loader reference
        int scanned = 0;
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
                continue;
            if (scanned++ > 20) break;

            try
            {
                // Read up to 128 KB per file (constant pool is near the start)
                int maxBytes = (int)Math.Min(entry.Length, 131072);
                byte[] buf   = new byte[maxBytes];
                using var s  = entry.Open();
                int read     = s.Read(buf, 0, maxBytes);
                if (ContainsBytes(buf.AsSpan(0, read), needle)) return true;
            }
            catch { /* corrupt entry — skip */ }
        }
        return false;
    }

    static bool ContainsBytes(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        if (needle.IsEmpty) return true;
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
            if (haystack.Slice(i, needle.Length).SequenceEqual(needle)) return true;
        return false;
    }

    static string? ReadManifest(ZipArchive zip)
    {
        var entry = zip.GetEntry("META-INF/MANIFEST.MF");
        if (entry == null) return null;
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
