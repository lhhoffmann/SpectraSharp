using System.Text;
using SpectraEngine.Bridge;
using SpectraEngine.Core;
using SpectraEngine.Graphics;
using SpectraEngine.IO;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("[SpectraEngine] Boot sequence starting...");

// ── IO ────────────────────────────────────────────────────────────────────────
AssetManager assets = new();

// ── Graphics ─────────────────────────────────────────────────────────────────
TextureRegistry textures = new();

// ── Bridge — discover all IBridgeStub implementations via reflection ──────────
IEnumerable<IBridgeStub> stubs = AppDomain.CurrentDomain
    .GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .Where(t => t.IsClass && !t.IsAbstract && typeof(IBridgeStub).IsAssignableFrom(t))
    .Select(t => (IBridgeStub)Activator.CreateInstance(t)!);

BridgeRegistry bridge = new(stubs);

// ── Core ──────────────────────────────────────────────────────────────────────
Engine engine = new(assets, textures, bridge);
engine.Run();
