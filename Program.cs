using System.Text;
using SpectraSharp.Bridge;
using SpectraSharp.Core;
using SpectraSharp.Graphics;
using SpectraSharp.IO;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("[SpectraSharp] Boot sequence starting...");

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
