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

    // ── Voxel terrain meshes (one Model per unique tile texture) ─────────────────
    private readonly Dictionary<string, Model> _voxelModels = [];

    // ── Core world (game thread only — never read directly by render thread) ──
    private World?               _world;
    private WorldProvider?       _worldProvider;
    private int                  _surfaceY = 64; // updated after chunk (0,0) generates
    private readonly List<Entity> _entities = [];
    private DebugMob?            _debugMob;


    // ── Entry point ───────────────────────────────────────────────────────────

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        Raylib.InitWindow(1280, 720, "SpectraSharp — Core Debug World");
        Raylib.SetTargetFPS(0);

        SetWindowIcon();
        BlockRegistry.Initialize(); // must run before LoadAssets so Core block face tiles are known
        Items.ItemRegistry.Initialize(); // must run after BlockRegistry (tool arrays reference block singletons)
        LoadAssets();
        SetupMaterials();
        SetupBridgeBlocks();
        SetupCoreWorld();
        BuildVoxelMeshes(); // must run AFTER SetupCoreWorld so chunk (0,0) is generated
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

        // ── Entity ticks ─────────────────────────────────────────────────────
        AxisAlignedBB.ResetPool();
        foreach (Entity entity in _entities)
        {
            if (!entity.IsDead) entity.Tick();
        }

        // Debug mob damage intentionally disabled — entity physics not yet fully wired
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

        // Terrain is a single pre-built mesh rendered directly in Render() — not in snapshot.

        // ── Entities ─────────────────────────────────────────────────────────
        var entities = new List<EntityRenderData>();
        foreach (Entity entity in _entities)
        {
            if (entity.IsDead) continue;
            string label;
            Color  col;

            if (entity is EntityItem item)
            {
                label = $"Item age={item.Age}";
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
        const long seed = 42L;

        // Build world using ChunkProviderServer wrapping the procedural generator.
        // Two-phase construction mirrors ChunkProviderGenerate.SetWorld: create the
        // server before World exists, then wire them together via SetWorld.
        _worldProvider = new OverworldProvider();
        var generator  = new ChunkProviderGenerate(seed);
        var server     = new ChunkProviderServer(WorldSave.NullSaveHandler.Instance.GetChunkPersistence(_worldProvider), generator);
        _world         = new World(server, seed, false, _worldProvider);
        server.SetWorld(_world);

        Console.WriteLine("[Engine] Generating terrain chunk (0, 0)...");
        // Force chunk (0,0) to generate now so surface heights are available for mesh building
        _ = _world.GetBlockId(0, 0, 0);

        Console.WriteLine($"[Engine] BrightnessTable[15] = {_worldProvider.BrightnessTable[15]:F4}");

        // Find surface height at chunk centre (8, _, 8) for entity placement
        _surfaceY = FindSurfaceY(8, 8);
        Console.WriteLine($"[Engine] Surface at (8,8) = Y{_surfaceY}");

        // Spawn 3 EntityItem floating just above the surface
        for (int i = 0; i < 3; i++)
        {
            var stack = new ItemStack(1, 1); // stone block item
            var item  = new EntityItem(_world, 4 + i * 4, _surfaceY + 3.0, 4 + i * 2, stack);
            item.PickupDelay = 0;
            _entities.Add(item);
        }

        // Spawn debug mob at surface
        _debugMob = new DebugMob(_world);
        _debugMob.SetPosition(8.0, _surfaceY + 1.0, 8.0);
        _entities.Add(_debugMob);

        Console.WriteLine($"[Engine] Core world ready. {_entities.Count} entities spawned.");
    }

    /// <summary>Returns the Y of the top non-air block at (x, z), or 0 if fully air.</summary>
    private int FindSurfaceY(int x, int z)
    {
        if (_world == null) return 1;
        for (int y = World.WorldHeight - 1; y >= 0; y--)
            if (_world.GetBlockId(x, y, z) != 0) return y;
        return 0;
    }

    private void SetupCamera()
    {
        // Look at the centre of the 16×16 terrain patch from above-behind.
        // _surfaceY is the actual terrain surface height (typically 63-70 for seed 42).
        float centerX  = 8f;
        float centerZ  = WorldOffsetZ + 8f; // middle of the 16-block terrain patch
        float sy       = _surfaceY;

        _camera = new Camera3D
        {
            // Diagonal view: above and slightly behind the terrain patch
            // so both the terrain surface and entity cubes are clearly visible
            Position   = new Vector3(centerX, sy + 12f, WorldOffsetZ - 10f),
            Target     = new Vector3(centerX, sy - 2f,  centerZ),
            Up         = Vector3.UnitY,
            FovY       = 70f,
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

        foreach (var key in _voxelModels.Keys.ToList())
        {
            Model m = _voxelModels[key];
            m.Materials[0].Maps[(int)MaterialMapIndex.Albedo].Texture = new Texture2D();
            Raylib.UnloadModel(m);
        }
        _voxelModels.Clear();
    }

    // ── 3D voxel terrain mesh ─────────────────────────────────────────────────────

    /// <summary>
    /// Iterates all blocks in chunk (0,0) and emits one quad per visible face (neighbour is air).
    /// Faces are grouped by tile texture key; one Mesh+Model is built per unique tile.
    /// Result: one draw call per unique tile texture (≈ 3–5 draw calls for typical terrain).
    ///
    /// Face convention (matches Core Block.GetTextureIndex):
    ///   face 0 = bottom (-Y),  face 1 = top (+Y)
    ///   face 2 = north (-Z),   face 3 = south (+Z)
    ///   face 4 = west (-X),    face 5 = east (+X)
    ///
    /// Vertex layout: 6 verts per face (2 triangles, no index buffer → glDrawArrays).
    /// </summary>
    private unsafe void BuildVoxelMeshes()
    {
        if (_world == null) return;

        // Build block-ID → tile-per-face lookup from Core blocks
        // (uses Block.GetTextureIndex which handles multi-face blocks like grass, log)
        var faceGroups = new Dictionary<string, (List<float> verts, List<float> uvs)>();

        bool IsAirAt(int bx, int by, int bz)
        {
            if (by < 0 || by >= World.WorldHeight) return true;
            if (bx < 0 || bx >= 16 || bz < 0 || bz >= 16) return true;
            return _world.GetBlockId(bx, by, bz) == 0;
        }

        void AddQuad(string key, float x0, float y0, float z0,
                                 float x1, float y1, float z1,
                                 float x2, float y2, float z2,
                                 float x3, float y3, float z3)
        {
            if (!faceGroups.TryGetValue(key, out var g))
                faceGroups[key] = g = (new List<float>(128), new List<float>(128));
            var (v, u) = g;
            // Triangle 1: v0, v1, v2
            v.Add(x0); v.Add(y0); v.Add(z0); u.Add(0f); u.Add(0f);
            v.Add(x1); v.Add(y1); v.Add(z1); u.Add(1f); u.Add(0f);
            v.Add(x2); v.Add(y2); v.Add(z2); u.Add(1f); u.Add(1f);
            // Triangle 2: v0, v2, v3
            v.Add(x0); v.Add(y0); v.Add(z0); u.Add(0f); u.Add(0f);
            v.Add(x2); v.Add(y2); v.Add(z2); u.Add(1f); u.Add(1f);
            v.Add(x3); v.Add(y3); v.Add(z3); u.Add(0f); u.Add(1f);
        }

        int totalFaces = 0;

        for (int bx = 0; bx < 16; bx++)
        for (int bz = 0; bz < 16; bz++)
        for (int by = 0; by < World.WorldHeight; by++)
        {
            int id = _world.GetBlockId(bx, by, bz);
            if (id == 0) continue;

            Block? blk = Block.BlocksList[id];
            int tileTop    = blk?.GetTextureIndex(1) ?? (blk?.BlockIndexInTexture ?? 1);
            int tileSide   = blk?.GetTextureIndex(2) ?? (blk?.BlockIndexInTexture ?? 1);
            int tileBottom = blk?.GetTextureIndex(0) ?? (blk?.BlockIndexInTexture ?? 1);

            float x = bx, y = by, z = bz + WorldOffsetZ;

            // Top face (+Y): face=1, neighbour above
            if (IsAirAt(bx, by + 1, bz))
            {
                string k = $"block_{tileTop}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x,y+1,z,  x+1,y+1,z,  x+1,y+1,z+1,  x,y+1,z+1);
                    totalFaces++;
                }
            }

            // Bottom face (-Y): face=0, neighbour below
            if (IsAirAt(bx, by - 1, bz))
            {
                string k = $"block_{tileBottom}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x,y,z+1,  x+1,y,z+1,  x+1,y,z,  x,y,z);
                    totalFaces++;
                }
            }

            // North face (-Z): face=2, neighbour at z-1
            if (IsAirAt(bx, by, bz - 1))
            {
                string k = $"block_{tileSide}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x+1,y+1,z,  x,y+1,z,  x,y,z,  x+1,y,z);
                    totalFaces++;
                }
            }

            // South face (+Z): face=3, neighbour at z+1
            if (IsAirAt(bx, by, bz + 1))
            {
                string k = $"block_{tileSide}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x,y+1,z+1,  x+1,y+1,z+1,  x+1,y,z+1,  x,y,z+1);
                    totalFaces++;
                }
            }

            // West face (-X): face=4, neighbour at x-1
            if (IsAirAt(bx - 1, by, bz))
            {
                string k = $"block_{tileSide}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x,y+1,z,  x,y+1,z+1,  x,y,z+1,  x,y,z);
                    totalFaces++;
                }
            }

            // East face (+X): face=5, neighbour at x+1
            if (IsAirAt(bx + 1, by, bz))
            {
                string k = $"block_{tileSide}";
                if (textures.TryGet(k) != null)
                {
                    AddQuad(k,  x+1,y+1,z+1,  x+1,y+1,z,  x+1,y,z,  x+1,y,z+1);
                    totalFaces++;
                }
            }
        }

        // Build one Mesh+Model per texture group
        foreach (var (key, (vertsL, uvsL)) in faceGroups)
        {
            if (vertsL.Count == 0) continue;
            Texture2D? tex = textures.TryGet(key);
            if (tex == null || tex.Value.Id == 0) continue;

            float[] vArr = vertsL.ToArray();
            float[] uArr = uvsL.ToArray();
            int vc = vArr.Length / 3;

            Mesh mesh;
            fixed (float* vp = vArr, up = uArr)
            {
                mesh = new Mesh
                {
                    VertexCount   = vc,
                    TriangleCount = vc / 3,
                    Vertices      = vp,
                    TexCoords     = up,
                };
                Raylib.UploadMesh(ref mesh, false);
                mesh.Vertices  = null;
                mesh.TexCoords = null;
            }

            Model model = Raylib.LoadModelFromMesh(mesh);
            Texture2D t = tex.Value;
            Raylib.SetMaterialTexture(ref model, 0, MaterialMapIndex.Albedo, ref t);
            _voxelModels[key] = model;
        }

        Console.WriteLine($"[Engine] Voxel mesh ready: {totalFaces} faces, {_voxelModels.Count} draw calls.");
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
        // Load biome colour lookup images (grasscolor.png, foliagecolor.png) first
        // so BiomeTintColor properties can use them when tiles are extracted below.
        TryLoadColorImage("grasscolor.png",   pixels => GrassColorizer.SetPixels(pixels));
        TryLoadColorImage("foliagecolor.png", pixels => FoliageColorizer.SetPixels(pixels));

        AssetData atlasData = assets.ExtractTerrainPng();
        Image     atlas     = Raylib.LoadImageFromMemory(".png", atlasData.Memory.ToArray());

        var white = new Raylib_cs.Color(255, 255, 255, 255);

        // ── Phase 1: collect unique (tileIdx, tint) per texture key ────────────
        // Separating data collection from GPU work avoids closing over Raylib Image
        // resources in local functions. Biome-tinted entries win over untinted ones
        // so GrassBlock's green tint for tile 0 beats the Metallurgy placeholder
        // stubs that also map to tile 0 with white tint.
        var tileQueue = new Dictionary<string, (int idx, Raylib_cs.Color tint)>();

        bool HasBiomeTint(Raylib_cs.Color c) =>
            c.A != 0 && (c.R != 255 || c.G != 255 || c.B != 255);

        foreach (IBridgeStub stub in bridge.AllStubs)
        {
            if (stub is not BlockBase block) continue;
            var biomeTint = block.BiomeTintColor;

            // For each (key, tint) pair: store it, but let a biome-tinted entry
            // overwrite a white entry for the same key.
            void TryQueue(int idx, string key, Raylib_cs.Color tint)
            {
                if (!tileQueue.TryGetValue(key, out var existing)
                    || (HasBiomeTint(tint) && !HasBiomeTint(existing.tint)))
                    tileQueue[key] = (idx, tint);
            }

            // Primary tile (covers all faces for most blocks)
            TryQueue(block.TextureIndex,       block.TextureKey,       biomeTint);
            // Top face (may differ from primary, e.g. WoodBlock top = log-end vs bark)
            TryQueue(block.TextureIndexTop,    block.TextureKeyTop,    biomeTint);
            // Side faces — NO biome tint: grass_side has dirt (brown) + gray gradient;
            // applying full tint makes the whole tile green, hiding the dirt portion.
            TryQueue(block.TextureIndexSide,   block.TextureKeySide,   white);
            // Bottom face — no biome tint
            TryQueue(block.TextureIndexBottom, block.TextureKeyBottom, white);
        }

        // ── Phase 2: extract and upload each unique tile ───────────────────────
        foreach (var (key, (idx, tint)) in tileQueue)
        {
            TerrainAtlas.ExtractAndRegister(idx, atlas, textures, key, tint);
            if (textures.TryGet(key) is Texture2D t)
                Raylib.SetTextureFilter(t, TextureFilter.Point);
        }

        Raylib.UnloadImage(atlas);
        Console.WriteLine($"[Engine] Assets loaded — {bridge.Count} stub(s), {tileQueue.Count} unique tile(s).");
    }

    /// <summary>
    /// Loads a 256×256 colour image from the JAR and passes its pixel data to the given setter.
    /// Silently skips if the entry is missing (older JAR versions) or cannot be decoded.
    /// </summary>
    private unsafe void TryLoadColorImage(string entryPath, Action<int[]> setPixels)
    {
        try
        {
            AssetData data  = assets.ExtractAsset(entryPath);
            Image     img   = Raylib.LoadImageFromMemory(".png", data.Memory.ToArray());
            int       count = img.Width * img.Height;
            int[]     pixels = new int[count];

            Color* ptr = (Color*)img.Data;
            for (int i = 0; i < count; i++)
            {
                Color c = ptr[i];
                pixels[i] = (c.R << 16) | (c.G << 8) | c.B;
            }

            Raylib.UnloadImage(img);
            setPixels(pixels);
            Console.WriteLine($"[Engine] Loaded {entryPath} ({count} pixels).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Engine] {entryPath} not available — using fallback colours. ({ex.Message})");
        }
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

        // 3D voxel terrain — one draw call per tile texture
        if (_voxelModels.Count > 0)
        {
            Rlgl.DisableBackfaceCulling();
            foreach (Model vm in _voxelModels.Values)
                Raylib.DrawModel(vm, Vector3.Zero, 1f, Color.White);
            Rlgl.EnableBackfaceCulling();
        }


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

        // ── top-left: Bridge block list (compact 2-column) ───────────────────
        const int px = 10, py = 10, pw = 380, lh = 16, pad = 8;

        var bridgeBlocks = snap.Blocks
            .Where(b => b.TextureKey.StartsWith("block_"))
            .ToList();

        int gridRows = (bridgeBlocks.Count + 1) / 2; // ceil(n/2)
        int ph       = pad * 2 + (gridRows + 2) * lh; // +2 for header + tick line

        Raylib.DrawRectangle(px, py, pw, ph, PanelBg);
        Raylib.DrawRectangleLines(px, py, pw, ph, Accent);
        Raylib.DrawText($"Bridge Blocks ({bridgeBlocks.Count})", px + pad, py + pad, 15, Accent);

        int colW    = (pw - pad * 2) / 2;
        int gridTop = py + pad + lh + 2;

        for (int i = 0; i < bridgeBlocks.Count; i++)
        {
            int col = i % 2, row = i / 2;
            var b   = bridgeBlocks[i];
            string name = b.JavaClassName.Contains('.')
                ? b.JavaClassName[(b.JavaClassName.LastIndexOf('.') + 1)..]
                : b.JavaClassName;
            // Strip "Block" prefix for even shorter display ("BlockGrass" → "Grass")
            if (name.StartsWith("Block")) name = name[5..];
            Raylib.DrawText($"#{b.TextureKey[6..],2}  {name}", px + pad + col * colW, gridTop + row * lh, 13, ValueColor);
        }

        if (bridgeBlocks.Count > 0)
        {
            long ticks = bridgeBlocks[0].TickCount;
            Raylib.DrawText($"  Ticks {ticks}  ({ticks / 20.0:F1} s)",
                            px + pad, gridTop + gridRows * lh + 2, 13, Dim);
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
