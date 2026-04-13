using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using SpectraSharp.Bridge;
using SpectraSharp.Bridge.Overrides;
using SpectraSharp.Graphics;
using SpectraSharp.IO;

namespace SpectraSharp.Core;

/// <summary>
/// Main engine loop.  Owns the window, drives the fixed 20 Hz tick, and
/// issues render calls.  All mutable game state lives in Bridge stubs —
/// the engine itself is stateless beyond the camera and tick counter.
/// </summary>
public sealed class Engine(AssetManager assets, TextureRegistry textures, BridgeRegistry bridge)
{
    private const double TicksPerSecond = 20.0;
    private const double FixedDeltaTime = 1.0 / TicksPerSecond;  // 0.05 s
    private const double MaxAccumulator = 0.25;                    // spiral-of-death cap

    private Camera3D _camera;
    private long     _totalTicks;

    public void Run()
    {
        Raylib.InitWindow(1280, 720, "SpectraSharp — Legacy Engine (Parity Protocol)");
        Raylib.SetTargetFPS(0);  // uncapped GPU

        LoadAssets();
        SetupCamera();

        Stopwatch clock = Stopwatch.StartNew();
        double accumulator = 0.0;
        double lastTime    = clock.Elapsed.TotalSeconds;

        while (!Raylib.WindowShouldClose())
        {
            double now       = clock.Elapsed.TotalSeconds;
            double frameTime = now - lastTime;
            lastTime = now;

            accumulator += frameTime;
            if (accumulator > MaxAccumulator) accumulator = MaxAccumulator;

            while (accumulator >= FixedDeltaTime)
            {
                FixedUpdate(FixedDeltaTime);
                accumulator -= FixedDeltaTime;
                _totalTicks++;
            }

            double alpha = accumulator / FixedDeltaTime;
            Render(alpha);
        }

        textures.Dispose();
        Raylib.CloseWindow();
    }

    // ── Asset loading ─────────────────────────────────────────────────────────

    private void LoadAssets()
    {
        AssetData atlas = assets.ExtractTerrainPng();

        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is BlockBase block)
                TerrainAtlas.ExtractAndRegister(block.TextureIndex, atlas, textures, block.TextureKey);
        }

        Console.WriteLine($"[Engine] Assets loaded — {bridge.Count} stub(s) registered.");
    }

    // ── Camera setup ──────────────────────────────────────────────────────────

    private void SetupCamera()
    {
        _camera = new Camera3D
        {
            Position   = new Vector3(3.5f, 3.5f, 3.5f),
            Target     = Vector3.Zero,
            Up         = Vector3.UnitY,
            FovY       = 55f,
            Projection = CameraProjection.Perspective
        };
    }

    // ── Fixed update (20 Hz) ──────────────────────────────────────────────────

    private void FixedUpdate(double delta)
    {
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is BridgeStubBase b) b.OnTick(delta);
        }
    }

    // ── Render (uncapped) ─────────────────────────────────────────────────────

    private void Render(double alpha)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);

        Raylib.BeginMode3D(_camera);
        Raylib.DrawGrid(20, 1f);

        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is BlockBase block)
            {
                Raylib.DrawCube(block.Position, 1f, 1f, 1f, StoneBlock.RenderColor);
                Raylib.DrawCubeWires(block.Position, 1f, 1f, 1f, Color.Black);
            }
        }

        Raylib.EndMode3D();

        // HUD
        BlockBase? first = null;
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is BlockBase b) { first = b; break; }
        }

        if (first is not null)
        {
            Raylib.DrawText($"TextureKey : {first.TextureKey}",    10, 10, 20, Color.White);
            Raylib.DrawText($"Parity     : {first.JavaClassName}", 10, 35, 20, Color.White);
            Raylib.DrawText($"Tick       : {first.TickCount}",     10, 60, 20, Color.White);
        }

        Raylib.DrawText($"FPS: {Raylib.GetFPS()}", 10, Raylib.GetScreenHeight() - 30, 20, Color.Yellow);

        Raylib.EndDrawing();
    }
}
