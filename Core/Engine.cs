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
/// Debug scene layout:
///   • Bridge block showcase — all BlockBase stubs in a line at Z = 0
///   • Core debug world      — 16×16 flat terrain at Z = +8; entities above it
/// </summary>
public sealed class Engine(AssetManager assets, TextureRegistry textures, BridgeRegistry bridge)
{
    private const double TicksPerSecond = 20.0;
    private const double FixedDeltaTime = 1.0 / TicksPerSecond;
    private const double MaxAccumulator = 0.25;

    // ── Scene offset for Core debug world (so it doesn't overlap bridge blocks) ─
    private const float WorldOffsetZ = 8f;

    // Shared between threads — atomic reference swap via volatile
    private volatile WorldSnapshot _snapshot = WorldSnapshot.Empty;

    private readonly CancellationTokenSource _cts = new();
    private Camera3D _camera;

    // GPU resources — created after InitWindow, freed before CloseWindow
    private readonly Dictionary<string, Model> _blockModels = [];

    // ── Core world (game thread only — never read directly by render thread) ──
    private World?              _world;
    private WorldProvider?      _worldProvider;
    private readonly List<Entity> _entities = [];
    private DebugMob?           _debugMob;

    // ── Entry point ───────────────────────────────────────────────────────────

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        Raylib.InitWindow(1280, 720, "SpectraSharp — Core Debug World");
        Raylib.SetTargetFPS(0);

        SetWindowIcon();
        LoadAssets();
        SetupMaterials();
        SetupBridgeBlocks();
        SetupCoreWorld();
        SetupCamera();

        Thread gameThread = new(GameLoop)
        {
            Name         = "SpectraSharp-GameThread",
            IsBackground = true
        };
        gameThread.Start();

        Console.WriteLine("[Engine] Game thread started. Render thread: main.");

        while (!Raylib.WindowShouldClose())
            Render(_snapshot);

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

            if (ticked)
                _snapshot = BuildSnapshot();

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
        // ── Bridge stubs ─────────────────────────────────────────────────────
        foreach (IBridgeStub stub in bridge.AllStubs)
            if (stub is BridgeStubBase b) b.OnTick(delta);

        // ── Core world tick ──────────────────────────────────────────────────
        _world?.MainTick();

        // ── Entity ticks (reset AABB pool before each physics pass) ─────────
        AxisAlignedBB.ResetPool();
        foreach (Entity entity in _entities)
        {
            if (!entity.IsDead) entity.Tick();
        }

        // ── Debug mob: take 1 damage every 2 seconds (40 ticks) ─────────────
        if (_debugMob != null && !_debugMob.IsDead
            && _world != null && _world.TotalWorldTime % 40 == 0)
        {
            _debugMob.AttackEntityFrom(null!, 1);
        }
    }

    private WorldSnapshot BuildSnapshot()
    {
        // ── Bridge blocks (line at Z=0) ───────────────────────────────────────
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

        // ── Core world surface blocks (grass at Y=1, rendered at Z+WorldOffsetZ) ─
        // Reuse bridge-block texture keys so the already-loaded textured models are used.
        if (_world != null)
        {
            for (int bx = 0; bx < 16; bx++)
            for (int bz = 0; bz < 16; bz++)
            {
                int id = _world.GetBlockId(bx, 1, bz); // grass layer
                if (id == 0) id = _world.GetBlockId(bx, 0, bz); // fall back to stone

                if (id == 0) continue;

                // Block center: block at world-Y occupies [Y, Y+1], so render center = Y+0.5
                int   worldY   = _world.GetBlockId(bx, 1, bz) != 0 ? 1 : 0;
                float renderY  = worldY + 0.5f;

                // Reuse bridge block texture models (already loaded in SetupMaterials)
                string texKey      = id == 2 ? "block_3" : "block_1"; // grass-side or stone
                string javaClass   = id == 2 ? "net.minecraft.src.BlockGrass"
                                             : "net.minecraft.src.BlockStone";

                blocks.Add(new BlockRenderData(
                    new Vector3(bx, renderY, bz + WorldOffsetZ),
                    new Color(200, 200, 200, 255),
                    texKey,
                    javaClass,
                    totalTicks));
            }
        }

        // ── Entities ─────────────────────────────────────────────────────────
        var entities = new List<EntityRenderData>();
        foreach (Entity entity in _entities)
        {
            if (entity.IsDead) continue;
            string label;
            Color  col;

            if (entity is EntityItem item)
            {
                label = $"Item  age={item.Age}";
                col   = new Color(255, 220, 50, 255); // gold
            }
            else if (entity is LivingEntity mob)
            {
                label = $"Mob  HP={mob.GetHealth()}/{mob.GetMaxHealth()}";
                col   = new Color(220, 80, 80, 255);  // red
            }
            else
            {
                label = entity.GetType().Name;
                col   = new Color(180, 180, 255, 255);
            }

            entities.Add(new EntityRenderData(
                new Vector3((float)entity.PosX, (float)entity.PosY, (float)entity.PosZ + WorldOffsetZ),
                col, label));
        }

        // ── World stats ───────────────────────────────────────────────────────
        long  worldTime        = _world?.WorldTime ?? 0;
        float brightnessSample = _worldProvider?.BrightnessTable[15] ?? 1.0f;
        int   mobHp            = _debugMob is { IsDead: false } ? _debugMob.GetHealth()    : 0;
        int   mobMax           = _debugMob?.GetMaxHealth() ?? 20;
        int   liveCount        = _entities.Count(e => !e.IsDead);

        return new WorldSnapshot(blocks, entities, totalTicks,
                                 worldTime, brightnessSample, mobHp, mobMax, liveCount);
    }

    // ── Scene setup ───────────────────────────────────────────────────────────

    private void SetupBridgeBlocks()
    {
        const float spacing = 2f;
        int i = 0;
        foreach (IBridgeStub stub in bridge.AllStubs)
            if (stub is BlockBase block)
                block.Position = new Vector3(i++ * spacing, 0f, 0f);
    }

    private void SetupCoreWorld()
    {
        // Build world
        _worldProvider = new OverworldProvider();
        var loader     = new DebugChunkLoader();
        _world         = new World(loader, 42L, false, _worldProvider);
        loader.SetWorld(_world);

        // Fill 16×16 flat terrain: stone at Y=0, grass at Y=1
        Console.WriteLine("[Engine] Building debug terrain...");
        for (int bx = 0; bx < 16; bx++)
        for (int bz = 0; bz < 16; bz++)
        {
            _world.SetBlock(bx, 0, bz, 1); // stone
            _world.SetBlock(bx, 1, bz, 2); // grass
        }

        // Verify chunk delegation
        int testId = _world.GetBlockId(8, 0, 8);
        Console.WriteLine($"[Engine] World.GetBlockId(8,0,8) = {testId} (expected 1 = stone)");
        Console.WriteLine($"[Engine] BrightnessTable[15] = {_worldProvider.BrightnessTable[15]:F4}");

        // Spawn 3 EntityItem above the terrain (grass at Y=1, so items start at Y=4)
        // ItemStack(itemId=1 = stone block item, count=1)
        for (int i = 0; i < 3; i++)
        {
            var stack  = new ItemStack(1, 1);  // stone block item
            var item   = new EntityItem(_world, 4 + i * 4, 5.0, 4 + i * 2, stack);
            item.PickupDelay = 0; // immediately pickable in theory
            _entities.Add(item);
        }

        // Spawn a debug mob (takes damage every 2 sec for health test)
        _debugMob = new DebugMob(_world);
        _debugMob.SetPosition(8.0, 2.0, 8.0);
        _entities.Add(_debugMob);

        Console.WriteLine($"[Engine] Core world ready. {_entities.Count} entities spawned.");
    }

    private void SetupCamera()
    {
        int   bridgeCount = bridge.AllStubs.Count(s => s is BlockBase);
        float bridgeEnd   = (bridgeCount - 1) * 2f;
        float sceneWidth  = Math.Max(bridgeEnd, 15f);
        float centerX     = sceneWidth / 2f;

        _camera = new Camera3D
        {
            Position   = new Vector3(centerX, 20f, -14f),
            Target     = new Vector3(centerX, 0f, 12f),
            Up         = Vector3.UnitY,
            FovY       = 65f,
            Projection = CameraProjection.Perspective
        };
    }

    // ── Material setup ────────────────────────────────────────────────────────

    private void SetupMaterials()
    {
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is not BlockBase block || _blockModels.ContainsKey(block.TextureKey)) continue;
            if (textures.TryGet(block.TextureKey) is not Texture2D tex || tex.Id == 0) continue;

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

        var seen = new HashSet<string>();
        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is not BlockBase block || !seen.Add(block.TextureKey)) continue;
            TerrainAtlas.ExtractAndRegister(block.TextureIndex, atlas, textures, block.TextureKey);

            if (textures.TryGet(block.TextureKey) is Texture2D tex)
                Raylib.SetTextureFilter(tex, TextureFilter.Point);
        }

        Raylib.UnloadImage(atlas);
        Console.WriteLine($"[Engine] Assets loaded — {bridge.Count} stub(s), {seen.Count} unique tile(s).");
    }

    // ── Render ────────────────────────────────────────────────────────────────

    private void Render(WorldSnapshot snap)
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);

        Raylib.BeginMode3D(_camera);
        Raylib.DrawGrid(30, 1f);

        foreach (BlockRenderData block in snap.Blocks)
            DrawBlock(block);

        foreach (EntityRenderData entity in snap.Entities)
            DrawEntity(entity);

        // Label: bridge showcase
        Raylib.DrawLine3D(new Vector3(-1f, 0.01f, 0f), new Vector3(16f, 0.01f, 0f), Color.Yellow);
        // Label: core world border
        Raylib.DrawLine3D(new Vector3(-1f, 0.01f, WorldOffsetZ), new Vector3(17f, 0.01f, WorldOffsetZ), Color.Green);
        Raylib.DrawLine3D(new Vector3(-1f, 0.01f, WorldOffsetZ + 16f), new Vector3(17f, 0.01f, WorldOffsetZ + 16f), Color.Green);

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
    }

    private static void DrawEntity(EntityRenderData entity)
    {
        bool isMob = entity.Label.StartsWith("Mob");
        float w = isMob ? 0.6f : 0.3f;
        float h = isMob ? 1.8f : 0.3f;
        // Position is entity feet (PosY = MinY of AABB) — render center is half-height up
        var center = entity.Position with { Y = entity.Position.Y + h / 2f };
        Raylib.DrawCube(center, w, h, w, entity.RenderColor);
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private static readonly Color PanelBg   = new(0,   0,   0,   160);
    private static readonly Color Accent     = new(255, 200,  50, 255);
    private static readonly Color Dim        = new(180, 180, 180, 255);
    private static readonly Color ValueColor = new(255, 255, 255, 255);
    private static readonly Color GreenColor = new( 80, 220,  80, 255);
    private static readonly Color RedColor   = new(220,  60,  60, 255);

    private void DrawHud(WorldSnapshot snap)
    {
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();

        // ── top-left: Bridge block list ───────────────────────────────────────
        const int px = 10, py = 10, pw = 380, lh = 18, pad = 8;

        // Collect only bridge stubs (TextureKey starts with "block_")
        var bridgeBlocks = snap.Blocks
            .Where(b => b.TextureKey.StartsWith("block_"))
            .ToList();

        int listRows = bridgeBlocks.Count + 2; // header + tick row
        int ph       = pad * 2 + listRows * lh;

        Raylib.DrawRectangle(px, py, pw, ph, PanelBg);
        Raylib.DrawRectangleLines(px, py, pw, ph, Accent);
        Raylib.DrawText($"Bridge Blocks ({bridgeBlocks.Count})", px + pad, py + pad, 16, Accent);

        int by2 = py + pad + lh + 2;
        foreach (var b in bridgeBlocks)
        {
            // Extract short class name: "net.minecraft.src.BlockGrass" → "BlockGrass"
            string shortName = b.JavaClassName.Contains('.')
                ? b.JavaClassName[(b.JavaClassName.LastIndexOf('.') + 1)..]
                : b.JavaClassName;
            HudRow(shortName, b.TextureKey, px + pad, by2);
            by2 += lh;
        }
        if (bridgeBlocks.Count > 0)
        {
            long ticks = bridgeBlocks[0].TickCount;
            Raylib.DrawText($"  Ticks: {ticks}  ({ticks / 20.0:F1} s)",
                            px + pad, by2, 14, Dim);
        }

        // ── top-left below: Core world stats ─────────────────────────────────
        int py2 = py + ph + 6;
        int ph2 = pad * 2 + 7 * lh + 4; // header + 6 rows + HP bar extra

        Raylib.DrawRectangle(px, py2, pw, ph2, PanelBg);
        Raylib.DrawRectangleLines(px, py2, pw, ph2, GreenColor);
        Raylib.DrawText("Core World", px + pad, py2 + pad, 18, GreenColor);

        int y2 = py2 + pad + lh + 4;
        HudRow("WorldTime", $"{snap.WorldTime} / 24000",       px + pad, y2); y2 += lh;
        HudRow("Brightness", $"{snap.BrightnessSample:F4}",    px + pad, y2); y2 += lh;
        HudRow("Entities",  $"{snap.LiveEntityCount} alive",   px + pad, y2); y2 += lh;

        // Health bar for debug mob
        int barX = px + pad;
        int barY = y2 + 2;
        float pct = snap.MobMaxHealth > 0 ? (float)snap.MobHealth / snap.MobMaxHealth : 0f;
        int barW  = pw - pad * 2;
        Raylib.DrawText("Mob HP:", barX, barY, 16, Dim);
        Raylib.DrawRectangle(barX + 70, barY, barW - 70, 14, new Color(60, 0, 0, 200));
        Raylib.DrawRectangle(barX + 70, barY, (int)((barW - 70) * pct), 14,
            pct > 0.5f ? GreenColor : pct > 0.25f ? Accent : RedColor);
        Raylib.DrawText($"{snap.MobHealth}/{snap.MobMaxHealth}", barX + 70 + (barW - 70) + 4, barY, 14, ValueColor);
        y2 += lh + 4;

        HudRow("Stubs",   $"{bridge.Count}",                   px + pad, y2); y2 += lh;
        HudRow("Ticks",   $"{snap.TotalTicks}",                px + pad, y2);

        // ── bottom-left: FPS ─────────────────────────────────────────────────
        int    fps = Raylib.GetFPS();
        double ft  = Raylib.GetFrameTime() * 1000.0;
        Color  fpsColor = fps >= 60 ? GreenColor : fps >= 30 ? Accent : RedColor;
        const int bw = 200, bh = 44;
        int bx = px, by = sh - bh - px;
        Raylib.DrawRectangle(bx, by, bw, bh, PanelBg);
        Raylib.DrawRectangleLines(bx, by, bw, bh, Accent);
        Raylib.DrawText($"FPS  {fps}",         bx + pad, by + pad,      18, fpsColor);
        Raylib.DrawText($"{ft:F2} ms / frame", bx + pad, by + pad + 20, 14, Dim);

        // ── top-right: legend ────────────────────────────────────────────────
        string[] legend =
        [
            "Yellow line = Bridge stubs",
            "Green border = Core World (16×16)",
            "Gold cubes  = EntityItem (bouncing)",
            "Red cube    = DebugMob (loses 1 HP / 2 s)"
        ];
        int tw = 280, ty = py + pad;
        for (int li = 0; li < legend.Length; li++)
        {
            Raylib.DrawText(legend[li], sw - tw - px, ty + li * lh, 14, Dim);
        }
    }

    private static void HudRow(string label, string value, int x, int y)
    {
        Raylib.DrawText($"{label,-12}", x,       y, 16, Dim);
        Raylib.DrawText(value,          x + 110, y, 16, ValueColor);
    }

    // ── DebugMob — minimal concrete LivingEntity for health testing ───────────

    private sealed class DebugMob(World world) : LivingEntity(world)
    {
        public override int GetMaxHealth() => 20;

        protected override void EntityInit()
        {
            base.EntityInit();       // registers DataWatcher index 8
            SetSize(0.6f, 1.8f);     // player-sized mob
        }
    }
}
