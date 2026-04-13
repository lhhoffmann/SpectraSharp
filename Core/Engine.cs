using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using SpectraSharp.Bridge;
using SpectraSharp.Bridge.Overrides;
using SpectraSharp.Graphics;
using SpectraSharp.IO;

namespace SpectraSharp.Core;

/// <summary>
/// Main engine — owns the window and coordinates two threads:
///
///   Game thread  — fixed 20 Hz tick loop, writes <see cref="WorldSnapshot"/>
///   Main thread  — Windows message pump + uncapped render, reads snapshot
///
/// The snapshot is a <c>volatile</c> reference swap: the game thread never
/// blocks the render thread and vice versa.  Window drag no longer pauses
/// game logic because the game thread is completely independent of GLFW.
/// </summary>
public sealed class Engine(AssetManager assets, TextureRegistry textures, BridgeRegistry bridge)
{
    private const double TicksPerSecond = 20.0;
    private const double FixedDeltaTime = 1.0 / TicksPerSecond;  // 0.05 s
    private const double MaxAccumulator = 0.25;                    // spiral-of-death cap

    // Shared between threads — atomic reference swap via volatile
    private volatile WorldSnapshot _snapshot = WorldSnapshot.Empty;

    private readonly CancellationTokenSource _cts = new();
    private Camera3D _camera;

    // GPU resources — created after InitWindow, freed before CloseWindow
    private readonly Dictionary<string, Model> _blockModels = [];

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        Raylib.InitWindow(1280, 720, "SpectraSharp — Legacy Engine (Parity Protocol)");
        Raylib.SetTargetFPS(0);  // uncapped render

        SetWindowIcon();
        LoadAssets();
        SetupMaterials();
        SetupDebugWorld();
        SetupCamera();

        // Game logic runs on its own thread — completely independent of GLFW
        Thread gameThread = new(GameLoop)
        {
            Name         = "SpectraSharp-GameThread",
            IsBackground = true
        };
        gameThread.Start();

        Console.WriteLine("[Engine] Game thread started. Render thread: main.");

        // Main thread: Windows message pump + render
        while (!Raylib.WindowShouldClose())
            Render(_snapshot);

        // Shutdown
        _cts.Cancel();
        gameThread.Join(millisecondsTimeout: 500);

        UnloadMaterials();
        textures.Dispose();
        Raylib.CloseWindow();
    }

    // ── Game thread ───────────────────────────────────────────────────────────

    private void GameLoop()
    {
        Stopwatch clock = Stopwatch.StartNew();
        double accumulator = 0.0;
        double lastTime    = clock.Elapsed.TotalSeconds;

        while (!_cts.IsCancellationRequested)
        {
            double now       = clock.Elapsed.TotalSeconds;
            double frameTime = now - lastTime;
            lastTime = now;

            accumulator += frameTime;
            if (accumulator > MaxAccumulator) accumulator = MaxAccumulator;

            bool ticked = false;
            while (accumulator >= FixedDeltaTime)
            {
                FixedUpdate(FixedDeltaTime);
                accumulator -= FixedDeltaTime;
                ticked = true;
            }

            // Publish new snapshot after each tick batch
            if (ticked)
                _snapshot = BuildSnapshot();

            // Sleep until close to the next tick boundary (leave 2 ms for wake-up jitter)
            double timeToNext = FixedDeltaTime - accumulator - 0.002;
            if (timeToNext > 0.001)
                Thread.Sleep((int)(timeToNext * 1000));
            else
                Thread.Yield();
        }

        Console.WriteLine("[Engine] Game thread stopped.");
    }

    private void FixedUpdate(double delta)
    {
        foreach (IBridgeStub stub in bridge.AllStubs)
            if (stub is BridgeStubBase b) b.OnTick(delta);
    }

    private WorldSnapshot BuildSnapshot()
    {
        var blocks = new List<BlockRenderData>();
        long totalTicks = 0;

        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is BlockBase block)
            {
                blocks.Add(new BlockRenderData(
                    block.Position,
                    block.RenderColor,
                    block.TextureKey,
                    block.JavaClassName,
                    block.TickCount));

                totalTicks = Math.Max(totalTicks, block.TickCount);
            }
        }

        return new WorldSnapshot(blocks, totalTicks);
    }

    // ── Material setup ───────────────────────────────────────────────────────

    private void SetupMaterials()
    {
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is not BlockBase block) continue;
            if (textures.TryGet(block.TextureKey) is not Texture2D tex || tex.Id == 0) continue;
            if (_blockModels.ContainsKey(block.TextureKey)) continue;

            Model model = Raylib.LoadModelFromMesh(Raylib.GenMeshCube(1f, 1f, 1f));
            Raylib.SetMaterialTexture(ref model, 0, MaterialMapIndex.Albedo, ref tex);
            _blockModels[block.TextureKey] = model;
        }

        Console.WriteLine($"[Engine] Block models: {_blockModels.Count}");
    }

    private unsafe void UnloadMaterials()
    {
        foreach (Model model in _blockModels.Values)
        {
            // Clear texture reference before unloading — TextureRegistry owns lifetime
            Model m = model;
            m.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = new Texture2D();
            Raylib.UnloadModel(m);
        }
        _blockModels.Clear();
    }

    // ── Window icon ───────────────────────────────────────────────────────────

    private static void SetWindowIcon()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Branding", "SpectraSharpLogo256x256.png");
        if (!File.Exists(path)) return;

        Image icon = Raylib.LoadImage(path);
        Raylib.SetWindowIcon(icon);
        Raylib.UnloadImage(icon);
    }

    // ── Asset loading ─────────────────────────────────────────────────────────

    private void LoadAssets()
    {
        AssetData atlasData = assets.ExtractTerrainPng();
        Image     atlas     = Raylib.LoadImageFromMemory(".png", atlasData.Memory.ToArray());

        // Deduplicate by TextureKey — two blocks sharing a tile index only upload once
        var seen = new HashSet<string>();
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is not BlockBase block || !seen.Add(block.TextureKey)) continue;
            TerrainAtlas.ExtractAndRegister(block.TextureIndex, atlas, textures, block.TextureKey);

            // Point filtering — pixel-art textures must not be interpolated
            if (textures.TryGet(block.TextureKey) is Texture2D tex)
                Raylib.SetTextureFilter(tex, TextureFilter.Point);
        }

        Raylib.UnloadImage(atlas);
        Console.WriteLine($"[Engine] Assets loaded — {bridge.Count} stub(s), {seen.Count} unique tile(s).");
    }

    private void SetupDebugWorld()
    {
        const float spacing = 2f;
        int i = 0;
        foreach (IBridgeStub stub in bridge.AllStubs)
            if (stub is BlockBase block)
                block.Position = new Vector3(i++ * spacing, 0f, 0f);
    }

    // ── Camera ────────────────────────────────────────────────────────────────

    private void SetupCamera()
    {
        int   blockCount = bridge.AllStubs.Count(s => s is BlockBase);
        float rowWidth   = (blockCount - 1) * 2f;
        float centerX    = rowWidth / 2f;

        // Compute distance so the full row fits inside the horizontal FoV with 20% margin
        const float fovY        = 65f;
        float       halfFovRad  = fovY * 0.5f * MathF.PI / 180f;
        float       dist        = (rowWidth / 2f) / MathF.Tan(halfFovRad) * 1.2f;
        dist = MathF.Max(dist, 10f);

        _camera = new Camera3D
        {
            Position   = new Vector3(centerX, dist * 0.4f, dist),
            Target     = new Vector3(centerX, 0f, 0f),
            Up         = Vector3.UnitY,
            FovY       = fovY,
            Projection = CameraProjection.Perspective
        };
    }

    // ── Render (main thread, uncapped) ────────────────────────────────────────

    private void Render(WorldSnapshot snap)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);

        Raylib.BeginMode3D(_camera);
        Raylib.DrawGrid(20, 1f);

        foreach (BlockRenderData block in snap.Blocks)
            DrawBlock(block);

        Raylib.EndMode3D();

        DrawHud(snap);

        Raylib.EndDrawing();
    }

    private void DrawBlock(BlockRenderData block)
    {
        if (_blockModels.TryGetValue(block.TextureKey, out Model model))
            Raylib.DrawModelEx(model, block.Position, Vector3.UnitY, 0f, Vector3.One, Color.White);
        else
            Raylib.DrawCube(block.Position, 1f, 1f, 1f, block.RenderColor);

        Raylib.DrawCubeWires(block.Position, 1f, 1f, 1f, Color.Black);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private static readonly Color PanelBg    = new(0,   0,   0,   160);
    private static readonly Color Accent      = new(255, 200,  50, 255);
    private static readonly Color Dim         = new(180, 180, 180, 255);
    private static readonly Color ValueColor  = new(255, 255, 255, 255);

    private void DrawHud(WorldSnapshot snap)
    {
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();

        // ── top-left panel ────────────────────────────────────────────────────
        const int px = 10, py = 10, pw = 380, lh = 22, pad = 10;

        BlockRenderData? first = snap.Blocks.Count > 0 ? snap.Blocks[0] : null;
        int rows = first is not null ? 5 : 2;
        int ph   = pad * 2 + rows * lh;

        Raylib.DrawRectangle(px, py, pw, ph, PanelBg);
        Raylib.DrawRectangleLines(px, py, pw, ph, Accent);
        Raylib.DrawText("SpectraSharp  |  Debug", px + pad, py + pad, 18, Accent);

        if (first is not null)
        {
            int y = py + pad + lh + 4;
            HudRow("Texture",  first.TextureKey,                     px + pad, y); y += lh;
            HudRow("Parity",   first.JavaClassName,                  px + pad, y); y += lh;
            HudRow("Tick",     $"{first.TickCount}",                 px + pad, y); y += lh;
            HudRow("Elapsed",  $"{first.TickCount / 20.0:F1} s",     px + pad, y);
        }

        // ── bottom-left: FPS + frame time ────────────────────────────────────
        int    fps = Raylib.GetFPS();
        double ft  = Raylib.GetFrameTime() * 1000.0;

        Color fpsColor = fps >= 60 ? new Color(80,  220, 80,  255)
                       : fps >= 30 ? new Color(220, 180, 50,  255)
                                   : new Color(220, 60,  60,  255);

        const int bw = 200, bh = 44;
        int bx = px, by = sh - bh - px;
        Raylib.DrawRectangle(bx, by, bw, bh, PanelBg);
        Raylib.DrawRectangleLines(bx, by, bw, bh, Accent);
        Raylib.DrawText($"FPS  {fps}",         bx + pad, by + pad,      18, fpsColor);
        Raylib.DrawText($"{ft:F2} ms / frame", bx + pad, by + pad + 20, 14, Dim);

        // ── top-right: thread info ────────────────────────────────────────────
        string info = $"Stubs: {bridge.Count}  |  Ticks: {snap.TotalTicks}";
        int tw = Raylib.MeasureText(info, 16);
        Raylib.DrawText(info, sw - tw - px - pad, py + pad, 16, Dim);
    }

    private static void HudRow(string label, string value, int x, int y)
    {
        Raylib.DrawText($"{label,-10}", x,      y, 16, Dim);
        Raylib.DrawText(value,          x + 90, y, 16, ValueColor);
    }
}
