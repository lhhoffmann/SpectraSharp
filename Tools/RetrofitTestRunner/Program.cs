// Tools/RetrofitTestRunner/Program.cs
// Scans Bridge/Overrides/ and Core/ for classes that meet the 80/20 test criteria,
// finds their matching parity spec, and calls Claude to generate xUnit tests.
//
// Usage: RetrofitTestRunner.exe [--dry-run]
//   --dry-run   Print which files would be processed without calling the API.
//
// Set ANTHROPIC_API_KEY before running (source .env).
// Model: claude-haiku-4-5  |  Concurrency: 5 parallel requests
// Estimated cost: ~$0.60 for the full codebase

using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

const string SpecDir    = "Documentation/VoxelCore/Parity/Specs";
const string OutputDir  = "Tests/Retrofit";
const string Model      = "claude-sonnet-4-6";
const int    Concurrent  = 1;
const int    MaxRetries  = 3;
const int    DelayMs     = 4000; // 4s between requests — stays well under 30K tokens/min

// Priority whitelist — most critical files for parity correctness.
// Run this list first before considering the full scan.
HashSet<string> PriorityFiles =
[
    "JavaRandom",           // RNG parity — affects every world gen outcome
    "NoiseGeneratorOctaves",// noise pipeline
    "PerlinNoiseGenerator", // noise primitive
    "BlockSand",            // falling block — famous quirks
    "BlockFluid",           // fluid physics
    "BlockFire",            // fire spread
    "BlockRedstoneTorch",   // redstone
    "BlockRedstoneWire",    // redstone
    "BlockRedstoneDiode",   // redstone
    "BlockTNT",             // TNT trigger
    "Explosion",            // explosion sphere logic
    "PathFinder",           // AI pathfinding (recursive)
    "MapGenCaves",          // cave carving
    "TileEntityFurnace",    // furnace state machine
    "NbtIo",                // save/load correctness
];

const string SystemPrompt =
    """
    You are a Parity QA Expert for the SpectraSharp project — a clean-room C# reimplementation
    of Minecraft 1.0 logic.

    You will receive two inputs:
    1. An existing C# implementation file
    2. The relevant excerpt from the Analyst's parity specification (may be empty)

    Your task: write an xUnit test class that verifies the implementation against the
    SPECIFICATION — not against the code itself.

    Rules:
    1. The spec is ground truth. If the code diverges from the spec, write the test as the
       spec demands. The test will fail — that is intentional. A failing test is a documented
       parity bug.
    2. Mark every test expected to fail against the current implementation with:
       [Fact(Skip = "PARITY BUG — impl diverges from spec: <one-line description>")]
       This keeps CI green while making every known divergence visible.
    3. Framework: xUnit ([Fact], [Theory], [InlineData]).
    4. NO MOCKING LIBRARIES. Write hand-written fakes/stubs directly in the test file
       (e.g. class FakeWorld : IWorld { ... }).
    5. Determinism: 100% deterministic. Any Random must use a fixed seed.
    6. Golden Master: For world gen / chunk data, compare SHA-256 of the block array
       against expected Mojang parity constants derived from verified Minecraft 1.0 behaviour.
    7. Cover all quirks in Section 7 ("Known Quirks / Bugs to Preserve"). These are the
       highest-value tests.

    Output: C# test class only. No explanation. No markdown code fences.
    """;

bool dryRun = args.Contains("--dry-run");

// ── Locate repo root ─────────────────────────────────────────────────────────

string root = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(root, "SpectraSharp.csproj")))
{
    string? parent = Path.GetDirectoryName(root);
    if (parent is null || parent == root)
        throw new InvalidOperationException("Repo root not found (SpectraSharp.csproj missing).");
    root = parent;
}

string specDir = Path.Combine(root, SpecDir);
string outDir  = Path.Combine(root, OutputDir);

Console.WriteLine($"Repo root : {root}");
Console.WriteLine($"Model     : {Model}");
Console.WriteLine($"Dry run   : {dryRun}");
Console.WriteLine();

// ── 1. Collect candidates ────────────────────────────────────────────────────

var scanPaths = new[]
{
    Path.Combine(root, "Bridge", "Overrides"),
    Path.Combine(root, "Core"),
};

var candidates = new List<string>();

foreach (string dir in scanPaths)
{
    if (!Directory.Exists(dir)) continue;
    foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
    {
        string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
        if (rel.Contains("Generated/"))                  continue;
        if (rel.Contains("Tests/"))                      continue;
        if (Path.GetFileName(file) == "SimpleBlocks.cs") continue;
        // Skip pure interfaces — nothing to test
        if (Path.GetFileName(file).StartsWith("I") &&
            IsInterface(file))                           continue;

        string className = Path.GetFileNameWithoutExtension(file);
        if (PriorityFiles.Contains(className))
            candidates.Add(file);
    }
}

Console.WriteLine($"Found {candidates.Count} candidate(s):\n");
foreach (string c in candidates)
    Console.WriteLine($"  {Path.GetRelativePath(root, c)}");
Console.WriteLine();

if (dryRun || candidates.Count == 0)
{
    if (dryRun) Console.WriteLine("Dry run complete.");
    return;
}

// ── 2. Cost estimate & confirmation ──────────────────────────────────────────

int    inputEst  = candidates.Count * 3000;
int    outputEst = candidates.Count * 1200;
double costUsd   = (inputEst / 1_000_000.0 * 1.0) + (outputEst / 1_000_000.0 * 5.0);
double costEur   = costUsd * 0.93;

Console.WriteLine($"Estimated cost : ~${costUsd:F2} USD (~€{costEur:F2})");
Console.WriteLine($"Estimated time : ~{(int)Math.Ceiling(candidates.Count / (double)Concurrent * 8 / 60)} min ({Concurrent} parallel)");
Console.WriteLine("Starting...");

// ── 3. Run in parallel ───────────────────────────────────────────────────────

// Load .env from repo root if key is not already in the environment
if (Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is null)
{
    string envFile = Path.Combine(root, ".env");
    if (File.Exists(envFile))
        foreach (string line in File.ReadAllLines(envFile))
        {
            if (line.StartsWith('#') || !line.Contains('=')) continue;
            string[] parts = line.Split('=', 2);
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
}

string apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is not set and .env not found.");

var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
var client     = new AnthropicClient(new APIAuthentication(apiKey), httpClient);
var semaphore = new SemaphoreSlim(Concurrent, Concurrent);
var counter   = new ConcurrentDictionary<string, bool>();
var errors    = new ConcurrentBag<string>();
int total     = candidates.Count;

Directory.CreateDirectory(outDir);

var tasks = candidates.Select(async file =>
{
    await semaphore.WaitAsync();
    try
    {
        string className = Path.GetFileNameWithoutExtension(file);
        string outFile   = Path.Combine(outDir, $"{className}Tests.cs");

        // Skip already completed files (resume after rate-limit abort)
        if (File.Exists(outFile))
        {
            counter[file] = true;
            Console.WriteLine($"[skip] {className}Tests.cs already exists");
            return;
        }

        string code = await File.ReadAllTextAsync(file);
        string spec = FindSpec(specDir, className);

            var parameters = new MessageParameters
        {
            Model         = Model,
            MaxTokens     = 32000,
            SystemMessage = SystemPrompt,
            Messages      =
            [
                new Message
                {
                    Role    = RoleType.User,
                    Content = [new TextContent { Text = BuildUserMessage(className, code, spec) }],
                }
            ],
        };

        MessageResponse response = null!;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                response = await client.Messages.GetClaudeMessageAsync(parameters);
                break;
            }
            catch (Exception ex) when (ex.Message.Contains("rate_limit") && attempt < MaxRetries)
            {
                int wait = (int)Math.Pow(2, attempt + 1) * 10; // 20s, 40s, 80s…
                Console.WriteLine($"  [rate limit] {className} — waiting {wait}s...");
                await Task.Delay(wait * 1000);
            }
        }

        string testSource = ((TextContent)response.Content[0]).Text.Trim();
        testSource = Regex.Replace(testSource, @"^```\w*\r?\n", "", RegexOptions.Multiline);
        testSource = testSource.Replace("```", "").Trim();

        await File.WriteAllTextAsync(outFile, testSource, Encoding.UTF8);

        counter[file] = true;
        int done = counter.Count;
        Console.WriteLine($"[{done,3}/{total}] {className}Tests.cs");
        await Task.Delay(DelayMs);
    }
    catch (Exception ex)
    {
        errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
        Console.WriteLine($"[ERR] {Path.GetFileName(file)}: {ex.Message}");
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);

Console.WriteLine();
Console.WriteLine($"Done. {counter.Count}/{total} files written to {OutputDir}/");
if (errors.Count > 0)
{
    Console.WriteLine($"\n{errors.Count} error(s):");
    foreach (string e in errors) Console.WriteLine($"  {e}");
}

// ── Helpers ──────────────────────────────────────────────────────────────────

static bool IsInterface(string file)
{
    string src = File.ReadAllText(file);
    return Regex.IsMatch(src, @"^\s*(public\s+)?interface\s+\w", RegexOptions.Multiline)
        && !Regex.IsMatch(src, @"^\s*(public\s+)?(abstract\s+)?class\s+\w", RegexOptions.Multiline);
}

static bool MeetsCriteria(string file, string specDir)
{
    string src = File.ReadAllText(file);

    // 1 — Complex math
    if (Regex.IsMatch(src, @"\bMath\.(Sqrt|Sin|Cos|Pow|Log|Exp|Floor|Ceiling|Round)\b")) return true;
    if (Regex.IsMatch(src, @"\b(noise|perlin|simplex|lerp|hermite)\b", RegexOptions.IgnoreCase)) return true;

    // 2 — Recursion / complex traversal structures
    if (Regex.IsMatch(src, @"\bStack<|\bQueue<|\bLinkedList<")) return true;
    foreach (Match m in Regex.Matches(src, @"\b(void|int|float|bool|string)\s+(\w{3,})\s*\("))
    {
        string name = m.Groups[2].Value;
        int first = src.IndexOf(name + "(", StringComparison.Ordinal);
        int second = src.IndexOf(name + "(", first + 1, StringComparison.Ordinal);
        if (second > 0) return true;
    }

    // 3 — Critical state management (owns the data, not just references it)
    if (Regex.IsMatch(src, @"\b(Inventory|InventoryPlayer)\b.*\[|List<.*Item")) return true;
    if (Regex.IsMatch(src, @"class\s+\w*Chunk\w*|new\s+Chunk\b")) return true;
    if (Regex.IsMatch(src, @"\b(BinaryWriter|BinaryReader|NbtIo|FileStream)\b")) return true;

    // 4 — Spec with non-empty Section 7 quirks
    string spec = FindSpec(specDir, Path.GetFileNameWithoutExtension(file));
    if (spec.Length > 0 && HasQuirks(spec)) return true;

    return false;
}

static string FindSpec(string specDir, string className)
{
    if (!Directory.Exists(specDir)) return string.Empty;
    string baseName = Regex.Replace(className, @"(Block|Item|Entity|Mob|Manager|System)$", "");
    foreach (string specFile in Directory.GetFiles(specDir, "*_Spec.md"))
    {
        string content = File.ReadAllText(specFile);
        if (content.Contains(className, StringComparison.OrdinalIgnoreCase)) return content;
        if (Path.GetFileName(specFile).Contains(baseName, StringComparison.OrdinalIgnoreCase)) return content;
    }
    return string.Empty;
}

static bool HasQuirks(string specContent)
{
    var match = Regex.Match(specContent,
        @"##\s*7\.\s*Known Quirks.*?\n([\s\S]*?)(?=##\s*8\.|$)");
    return match.Success && match.Groups[1].Value.Trim().Length > 10;
}

static string BuildUserMessage(string className, string code, string spec)
{
    var sb = new StringBuilder();
    sb.AppendLine("--- INPUT 1: Implementation ---");
    sb.AppendLine(code);
    sb.AppendLine();
    sb.AppendLine("--- INPUT 2: Spec excerpt ---");
    sb.AppendLine(spec.Length > 0
        ? spec
        : "(No spec — test against the code as documented behaviour only.)");
    return sb.ToString();
}
