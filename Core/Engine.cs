using System.Diagnostics;
using System.Numerics;
using Raylib_cs;
using SpectraEngine.Bridge;
using SpectraEngine.Bridge.Overrides;
using SpectraEngine.Graphics;
using SpectraEngine.IO;

namespace SpectraEngine.Core;

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

    // ── Input (written by render thread, read by game thread) ────────────────

    /// <summary>
    /// Atomic snapshot of raw keyboard/mouse input captured each render frame.
    /// The render thread creates a new instance and stores it here; the game thread
    /// reads and applies it once per tick.
    /// </summary>
    private volatile InputSnapshot _inputSnapshot = new();

    // ── MinecraftMain parity (spec: MinecraftMain_Spec.md) ────────────────────

    /// <summary>Total game ticks elapsed. Replica of <c>aij.b</c> (ElapsedTicks).</summary>
    public long ElapsedTicks { get; private set; }

    /// <summary>
    /// The local single-player instance. Replica of <c>Minecraft.h</c> (thePlayer / di).
    /// Null until a world is loaded with a player.
    /// </summary>
    public EntityPlayerSP? Player { get; private set; }

    /// <summary>
    /// The currently open GUI screen. Null during active gameplay.
    /// Replica of <c>Minecraft.s</c> (currentScreen).
    /// </summary>
    public object? CurrentScreen { get; set; }

    /// <summary>
    /// The active game mode manager. Replica of <c>Minecraft.c</c> (ItemInWorldManager).
    /// </summary>
    public ItemInWorldManager? GameMode { get; private set; }

    /// <summary>
    /// Last block/entity raycast result. Replica of <c>Minecraft.z</c> (objectMouseOver).
    /// Updated each tick before input is processed.
    /// Spec: Raycast_Spec.md.
    /// </summary>
    public MovingObjectPosition? ObjectMouseOver { get; private set; }

    // ── Chunk keep-alive counter (spec: MinecraftMain_Spec §Game Tick §2) ─────
    private int _chunkKeepAliveTick;


    // ── Entry point ───────────────────────────────────────────────────────────

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.AlwaysRunWindow);
        Raylib.InitWindow(1280, 720, "SpectraEngine — Core Debug World");
        Raylib.SetTargetFPS(0);
        Raylib.DisableCursor();
        _mouseCaptured = true;

        SetWindowIcon();
        BlockRegistry.Initialize(); // must run before LoadAssets so Core block face tiles are known
        Items.ItemRegistry.Initialize(); // must run after BlockRegistry (tool arrays reference block singletons)
        LoadAssets();
        SetupMaterials();
        SetupCoreWorld();
        BuildVoxelMeshes(); // must run AFTER SetupCoreWorld so chunk (0,0) is generated
        SetupCamera();

        Thread gameThread = new(GameLoop)
        {
            Name         = "SpectraEngine-GameThread",
            IsBackground = true
        };
        gameThread.Start();

        Console.WriteLine("[Engine] Game thread started. Render thread: main.");

        var _rtClock = Stopwatch.StartNew();
        double _rtLastHb = 0;
        while (!Raylib.WindowShouldClose())
        {
            if (_rtClock.Elapsed.TotalSeconds - _rtLastHb >= 3.0)
            {
                _rtLastHb = _rtClock.Elapsed.TotalSeconds;
                Console.WriteLine($"[RT] alive  t={_rtLastHb:F1}s");
            }
            Render(_snapshot);
        }

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
        double lastHeartbeat = 0;

        while (!_cts.IsCancellationRequested)
        {
            if (clock.Elapsed.TotalSeconds - lastHeartbeat >= 3.0)
            {
                lastHeartbeat = clock.Elapsed.TotalSeconds;
                Console.WriteLine($"[GT] alive  tick={ElapsedTicks}  t={lastHeartbeat:F1}s");
            }
            double now       = clock.Elapsed.TotalSeconds;
            double frameTime = now - lastTime;
            lastTime = now;

            accumulator += frameTime;
            if (accumulator > MaxAccumulator) accumulator = MaxAccumulator;

            bool ticked = false;
            while (accumulator >= FixedDeltaTime)
            {
                try { FixedUpdate(FixedDeltaTime); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Engine] FATAL: FixedUpdate threw — {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                    return;
                }
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
        ElapsedTicks++;
        Vec3.ResetPool(); // reclaim pooled objects from the previous tick

        // ── Apply input to player (from render thread snapshot) ───────────────
        InputSnapshot inp = _inputSnapshot;
        if (Player != null)
        {
            Player.MovementInput.ForwardSpeed = inp.Forward;
            Player.MovementInput.StrafeSpeed  = inp.Strafe;
            Player.MovementInput.IsSneaking   = inp.Sneak;

            // Jump: fire once per key-press (set true, cleared in EntityPlayerSP.Tick)
            if (inp.Jump) Player.MovementInput.IsJumping = true;

            // Apply mouse-look via Entity.Turn (spec: MouseLook_Spec §Turn Method).
            // Sensitivity formula: (sens*0.6+0.2)^3 * 8.0  — spec §Sensitivity Formula.
            // Turn() internally multiplies by 0.15, so we pass the pre-scaled delta.
            if (inp.MouseDX != 0f || inp.MouseDY != 0f)
            {
                float s   = MouseSensitivity * 0.6f + 0.2f;
                float fac = s * s * s * 8.0f;
                Player.Turn(inp.MouseDX * fac, -inp.MouseDY * fac * (InvertMouse ? -1f : 1f));
                _accMouseDX = 0f;
                _accMouseDY = 0f;
            }
        }

        // ── Raycast (spec: Raycast_Spec §Reach Distances) ────────────────────
        var _hangSw = Stopwatch.StartNew();
        if (_world != null && Player != null && GameMode != null)
        {
            float reach = GameMode.GetReach();
            Vec3 eye  = Player.GetEyePosition(1.0f);
            Vec3 look = Player.GetLookVector(1.0f);
            Vec3 end  = Vec3.GetFromPool(
                eye.X + look.X * reach,
                eye.Y + look.Y * reach,
                eye.Z + look.Z * reach);
            ObjectMouseOver = _world.RayTraceBlocks(eye, end);
        }
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] RayTrace took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // ── Bridge stubs ─────────────────────────────────────────────────────
        _hangSw.Restart();
        foreach (IBridgeStub stub in bridge.AllStubs)
            if (stub is BridgeStubBase b) b.OnTick(delta);
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] BridgeTick took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // ── Core world tick ──────────────────────────────────────────────────
        _hangSw.Restart();
        _world?.MainTick();
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] MainTick took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // ── Block interaction (left/right click) ─────────────────────────────
        _hangSw.Restart();
        if (GameMode != null && Player != null && ObjectMouseOver != null)
        {
            int bx = ObjectMouseOver.BlockX;
            int by = ObjectMouseOver.BlockY;
            int bz = ObjectMouseOver.BlockZ;
            int face = ObjectMouseOver.FaceId;

            if (inp.LeftPressed)
                GameMode.OnBlockClicked(bx, by, bz, face);
            if (inp.LeftReleased)
                GameMode.ResetBlockRemoving();
            if (inp.RightPressed && _world != null)
                GameMode.UseItem(Player, _world, Player.Inventory.GetStackInSelectedSlot(), bx, by, bz, face);
        }
        else if (GameMode != null && inp.LeftReleased)
            GameMode.ResetBlockRemoving();
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] BlockClick took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // Clear latched click flags after game tick has consumed them
        _accLeftPressed  = false;
        _accLeftReleased = false;
        _accRightPressed = false;

        // ── Game-mode tick (block-break progress) ────────────────────────────
        _hangSw.Restart();
        GameMode?.UpdateBlockRemoving();
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] UpdateBlockRemoving took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // ── Entity ticks ─────────────────────────────────────────────────────
        _hangSw.Restart();
        AxisAlignedBB.ResetPool();
        foreach (Entity entity in _entities)
        {
            if (!entity.IsDead) entity.Tick();
        }
        if (_hangSw.ElapsedMilliseconds > 500)
            Console.WriteLine($"[GT-HANG] EntityTick took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");

        // ── Chunk keep-alive every 30 ticks (spec: MinecraftMain_Spec §Game Tick §2) ─
        if (_world != null && Player != null)
        {
            _chunkKeepAliveTick++;
            if (_chunkKeepAliveTick >= 30)
            {
                _chunkKeepAliveTick = 0;
                _hangSw.Restart();
                _world.EnsureChunksAroundPlayer(Player);
                if (_hangSw.ElapsedMilliseconds > 500)
                    Console.WriteLine($"[GT-HANG] EnsureChunks took {_hangSw.ElapsedMilliseconds}ms at tick {ElapsedTicks}");
            }
        }
    }

    // ── Mouse settings (spec: MouseLook_Spec §Sensitivity Formula) ───────────
    // Default sensitivity = 0.5 → factor = 0.5^3 * 8 = 1.0 (natural feel)
    private const float MouseSensitivity = 0.5f; // ki.c range [0, 1]
    private const bool  InvertMouse      = false; // ki.d

    private WorldSnapshot BuildSnapshot()
    {
        // ── Entities ─────────────────────────────────────────────────────────
        var entities = new List<EntityRenderData>();
        foreach (Entity entity in _entities)
        {
            if (entity.IsDead) continue;
            if (entity is EntityPlayerSP) continue;

            string label;
            Color  col;

            if (entity is EntityItem item)
            {
                label = $"Item";
                col   = new Color(255, 220, 50, 255);
            }
            else if (entity is LivingEntity mob)
            {
                label = $"HP={mob.GetHealth()}/{mob.GetMaxHealth()}";
                col   = new Color(220, 80, 80, 255);
            }
            else
            {
                label = entity.GetType().Name;
                col   = new Color(180, 180, 255, 255);
            }

            entities.Add(new EntityRenderData(
                new Vector3((float)entity.PosX, (float)entity.PosY, (float)entity.PosZ),
                col, label));
        }

        // ── World stats ───────────────────────────────────────────────────────
        long  worldTime        = _world?.WorldTime ?? 0;
        float brightnessSample = _worldProvider?.BrightnessTable[15] ?? 1.0f;
        int   liveCount        = _entities.Count(e => !e.IsDead);

        // ── Player state ──────────────────────────────────────────────────────
        bool    hasPlayer  = Player is { IsDead: false };
        Vector3 playerPos  = hasPlayer
            ? new Vector3((float)Player!.PosX, (float)Player.PosY, (float)Player.PosZ)
            : Vector3.Zero;

        // ── MouseOver highlight ───────────────────────────────────────────────
        bool    hasMouseOver  = ObjectMouseOver != null;
        Vector3 mouseOverPos  = hasMouseOver
            ? new Vector3(ObjectMouseOver!.BlockX, ObjectMouseOver.BlockY, ObjectMouseOver.BlockZ)
            : Vector3.Zero;

        return new WorldSnapshot([], entities, 0,
                                 worldTime, brightnessSample, 20, 20, liveCount,
                                 hasPlayer, playerPos,
                                 hasPlayer ? Player!.RotationYaw   : 0f,
                                 hasPlayer ? Player!.RotationPitch : 0f,
                                 hasPlayer ? Player!.GetHealth()   : 20,
                                 hasPlayer ? Player!.GetMaxHealth(): 20,
                                 hasPlayer ? Player!.FoodStats.FoodLevel : 20,
                                 hasPlayer ? Player!.XpLevel         : 0,
                                 hasMouseOver, mouseOverPos);
    }

    private void SetupCoreWorld()
    {
        const long seed = 42L;
        const int  spawnRadius = 2; // chunks — preloads (2*2+1)² = 25 chunks

        _worldProvider = new OverworldProvider();
        var generator  = new ChunkProviderGenerate(seed);
        var server     = new ChunkProviderServer(WorldSave.NullSaveHandler.Instance.GetChunkPersistence(_worldProvider), generator);
        _world         = new World(server, seed, false, _worldProvider);
        server.SetWorld(_world);

        // Preload chunks around spawn so surface heights and mesh data are available
        Console.WriteLine($"[Engine] Generating {(spawnRadius*2+1)*(spawnRadius*2+1)} spawn chunks...");
        for (int cx = -spawnRadius; cx <= spawnRadius; cx++)
        for (int cz = -spawnRadius; cz <= spawnRadius; cz++)
            _ = _world.GetBlockId(cx * 16, 0, cz * 16);

        // Place player at surface above world origin
        _surfaceY = FindSurfaceY(8, 8);
        Console.WriteLine($"[Engine] Surface at (8,8) = Y{_surfaceY}");

        Player   = new EntityPlayerSP(_world, this);
        GameMode = new SurvivalItemInWorldManager(_world, Player);
        Player.SetPosition(8.0, _surfaceY + 2.0, 8.0);
        Player.PlayerName = "Player";
        Player.SetHealth(20);
        _entities.Add(Player);

        Console.WriteLine("[Engine] World ready.");
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
        _camera = new Camera3D
        {
            Position   = new Vector3(8f, _surfaceY + 2f, 8f),
            Target     = new Vector3(9f, _surfaceY + 2f, 8f),
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

        var faceGroups = new Dictionary<string, (List<float> verts, List<float> uvs)>();

        bool IsAirAt(int wx, int wy, int wz)
        {
            if (wy < 0 || wy >= World.WorldHeight) return true;
            // Do NOT call GetChunk — that would trigger generation for neighbors outside
            // the loaded area. Treat unloaded chunks as transparent so border faces render.
            if (!_world.IsChunkLoaded(wx >> 4, wz >> 4)) return true;
            return _world.GetBlockId(wx, wy, wz) == 0;
        }

        void AddQuad(string key, float x0, float y0, float z0,
                                 float x1, float y1, float z1,
                                 float x2, float y2, float z2,
                                 float x3, float y3, float z3)
        {
            if (!faceGroups.TryGetValue(key, out var g))
                faceGroups[key] = g = (new List<float>(128), new List<float>(128));
            var (v, u) = g;
            v.Add(x0); v.Add(y0); v.Add(z0); u.Add(0f); u.Add(0f);
            v.Add(x1); v.Add(y1); v.Add(z1); u.Add(1f); u.Add(0f);
            v.Add(x2); v.Add(y2); v.Add(z2); u.Add(1f); u.Add(1f);
            v.Add(x0); v.Add(y0); v.Add(z0); u.Add(0f); u.Add(0f);
            v.Add(x2); v.Add(y2); v.Add(z2); u.Add(1f); u.Add(1f);
            v.Add(x3); v.Add(y3); v.Add(z3); u.Add(0f); u.Add(1f);
        }

        int totalFaces = 0;

        // Snapshot loaded chunks to avoid modification during iteration
        var loadedChunks = _world.GetLoadedChunkCoords().ToList();

        foreach (var (chunkX, chunkZ) in loadedChunks)
        {
            int startX = chunkX * 16;
            int startZ = chunkZ * 16;

        for (int lx = 0; lx < 16; lx++)
        for (int lz = 0; lz < 16; lz++)
        for (int by = 0; by < World.WorldHeight; by++)
        {
            int wx = startX + lx;
            int wz = startZ + lz;
            int id = _world.GetBlockId(wx, by, wz);
            if (id == 0) continue;

            Block? blk      = Block.BlocksList[id];
            int    fallback = blk?.BlockIndexInTexture ?? 1;
            int tileTop    = blk?.GetTextureIndex(1) ?? fallback;
            int tileBottom = blk?.GetTextureIndex(0) ?? fallback;
            int tileNorth  = blk?.GetTextureIndex(2) ?? fallback; // -Z
            int tileSouth  = blk?.GetTextureIndex(3) ?? fallback; // +Z
            int tileWest   = blk?.GetTextureIndex(4) ?? fallback; // -X
            int tileEast   = blk?.GetTextureIndex(5) ?? fallback; // +X

            float x = wx, y = by, z = wz;

            if (IsAirAt(wx, by + 1, wz))
            {
                string k = $"block_{tileTop}";
                if (textures.TryGet(k) != null) { AddQuad(k, x,y+1,z, x+1,y+1,z, x+1,y+1,z+1, x,y+1,z+1); totalFaces++; }
            }
            if (IsAirAt(wx, by - 1, wz))
            {
                string k = $"block_{tileBottom}";
                if (textures.TryGet(k) != null) { AddQuad(k, x,y,z+1, x+1,y,z+1, x+1,y,z, x,y,z); totalFaces++; }
            }
            if (IsAirAt(wx, by, wz - 1))
            {
                string k = $"block_{tileNorth}";
                if (textures.TryGet(k) != null) { AddQuad(k, x+1,y+1,z, x,y+1,z, x,y,z, x+1,y,z); totalFaces++; }
            }
            if (IsAirAt(wx, by, wz + 1))
            {
                string k = $"block_{tileSouth}";
                if (textures.TryGet(k) != null) { AddQuad(k, x,y+1,z+1, x+1,y+1,z+1, x+1,y,z+1, x,y,z+1); totalFaces++; }
            }
            if (IsAirAt(wx - 1, by, wz))
            {
                string k = $"block_{tileWest}";
                if (textures.TryGet(k) != null) { AddQuad(k, x,y+1,z, x,y+1,z+1, x,y,z+1, x,y,z); totalFaces++; }
            }
            if (IsAirAt(wx + 1, by, wz))
            {
                string k = $"block_{tileEast}";
                if (textures.TryGet(k) != null) { AddQuad(k, x+1,y+1,z+1, x+1,y+1,z, x+1,y,z, x+1,y,z+1); totalFaces++; }
            }
        }
        } // end foreach chunk

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
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Branding", "SpectraEngineLogo256x256.png");
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

        // ── Phase 1b: collect tiles from all Core blocks (Block.BlocksList) ──────
        // Spec §6: tile 0 = grass top (grass tint), tile 52 = leaves (foliage tint).
        // Tile 3 (grass side) is NOT tinted at load-time — applying tint to the
        // whole tile turns the dirt portion green. Biome-tinted entries still win.
        var grassTint   = new Raylib_cs.Color(72, 181,  24, 255);
        var foliageTint = new Raylib_cs.Color(78, 164,   0, 255);

        foreach (Block? blk in Block.BlocksList)
        {
            if (blk == null) continue;
            for (int face = 0; face < 6; face++)
            {
                int tileIdx = blk.GetTextureIndex(face);
                string key  = $"block_{tileIdx}";
                var tint    = tileIdx ==  0 ? grassTint
                            : tileIdx == 52 ? foliageTint
                            : white;
                if (!tileQueue.TryGetValue(key, out var existing)
                    || (HasBiomeTint(tint) && !HasBiomeTint(existing.tint)))
                    tileQueue[key] = (tileIdx, tint);
            }
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
        // ── Poll input (render thread owns Raylib input API) ─────────────────
        PollInput(snap);

        // ── Update follow camera from player snapshot ─────────────────────────
        if (snap.HasPlayer)
            UpdateFirstPersonCamera(snap);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.SkyBlue);

        Raylib.BeginMode3D(_camera);

        // Voxel terrain — one draw call per tile texture
        if (_voxelModels.Count > 0)
        {
            Rlgl.DisableBackfaceCulling();
            foreach (Model vm in _voxelModels.Values)
                Raylib.DrawModel(vm, Vector3.Zero, 1f, Color.White);
            Rlgl.EnableBackfaceCulling();
        }

        foreach (EntityRenderData entity in snap.Entities)
            DrawEntity(entity);

        // Block highlight wireframe
        if (snap.HasMouseOver)
        {
            var c = snap.MouseOverPos + new Vector3(0.5f, 0.5f, 0.5f);
            Raylib.DrawCubeWires(c, 1.002f, 1.002f, 1.002f, Color.Black);
        }

        Raylib.EndMode3D();

        DrawHud(snap);

        Raylib.EndDrawing();
    }

    // ── Input polling (render thread) ─────────────────────────────────────────

    private bool _mouseCaptured;
    private float _accMouseDX, _accMouseDY;
    private bool _accLeftPressed, _accLeftReleased, _accRightPressed;
    private bool _leftWasDown;

    private void PollInput(WorldSnapshot snap)
    {
        // Escape releases mouse capture (pause / menu)
        if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _mouseCaptured)
        {
            _mouseCaptured = false;
            Raylib.EnableCursor();
        }

        // Tab toggles mouse capture (re-capture after menu)
        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            _mouseCaptured = !_mouseCaptured;
            Raylib.DisableCursor();
            if (!_mouseCaptured) Raylib.EnableCursor();
        }

        float fwd = 0f, strafe = 0f;
        bool jump = false, sneak = false;

        if (snap.HasPlayer)
        {
            if (Raylib.IsKeyDown(KeyboardKey.W)) fwd   += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.S)) fwd   -= 1f;
            if (Raylib.IsKeyDown(KeyboardKey.A)) strafe += 1f;
            if (Raylib.IsKeyDown(KeyboardKey.D)) strafe -= 1f;
            if (Raylib.IsKeyPressed(KeyboardKey.Space)) jump = true;
            if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) sneak = true;
        }

        if (_mouseCaptured)
        {
            var delta = Raylib.GetMouseDelta();
            _accMouseDX += delta.X;
            _accMouseDY += delta.Y;
        }

        // Mouse buttons — latch pressed/released until game tick consumes them
        bool leftDown = _mouseCaptured && Raylib.IsMouseButtonDown(MouseButton.Left);
        if (leftDown && !_leftWasDown)  _accLeftPressed  = true;
        if (!leftDown && _leftWasDown)  _accLeftReleased = true;
        _leftWasDown = leftDown;
        if (_mouseCaptured && Raylib.IsMouseButtonPressed(MouseButton.Right)) _accRightPressed = true;

        _inputSnapshot = new InputSnapshot(fwd, strafe, jump, sneak, _accMouseDX, _accMouseDY,
                                           leftDown, _accLeftPressed, _accLeftReleased, _accRightPressed);
    }

    private void UpdateFirstPersonCamera(WorldSnapshot snap)
    {
        float yawRad   = snap.PlayerYaw   * MathF.PI / 180f;
        float pitchRad = snap.PlayerPitch * MathF.PI / 180f;

        float eyeX = (float)snap.PlayerPos.X;
        float eyeY = (float)snap.PlayerPos.Y + 1.62f;
        float eyeZ = (float)snap.PlayerPos.Z;

        // Look direction: (-sin(yaw)*cos(pitch), -sin(pitch), cos(yaw)*cos(pitch))
        float cosPitch = MathF.Cos(pitchRad);
        float lookX = -MathF.Sin(yawRad) * cosPitch;
        float lookY = -MathF.Sin(pitchRad);
        float lookZ =  MathF.Cos(yawRad) * cosPitch;

        _camera.Position = new Vector3(eyeX, eyeY, eyeZ);
        _camera.Target   = new Vector3(eyeX + lookX, eyeY + lookY, eyeZ + lookZ);
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
