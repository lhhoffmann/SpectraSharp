namespace SpectraEngine.Core;

// ── EnumArt ──────────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>sv</c> — painting variant enum.
/// Each value stores its display name, pixel dimensions, and texture sheet offset.
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityPainting_Spec.md §4
/// </summary>
public enum EnumArt
{
    Kebab, Aztec, Alban, Aztec2, Bomb, Plant, Wasteland,
    Pool, Courbet, Sea, Sunset, Creebet,
    Wanderer, Graham,
    Match, Bust, Stage, Void, SkullAndRoses,
    Fighters,
    Pointer, Pigscene, BurningSkull,
    Skeleton, DonkeyKong
}

/// <summary>
/// Extension methods that expose the spec-defined metadata for each <see cref="EnumArt"/> value.
/// </summary>
public static class EnumArtInfo
{
    // name, widthPx, heightPx, sheetX, sheetY
    private static readonly (string Name, int W, int H, int SX, int SY)[] Data =
    [
        ("Kebab",         16, 16,   0,   0),
        ("Aztec",         16, 16,  16,   0),
        ("Alban",         16, 16,  32,   0),
        ("Aztec2",        16, 16,  48,   0),
        ("Bomb",          16, 16,  64,   0),
        ("Plant",         16, 16,  80,   0),
        ("Wasteland",     16, 16,  96,   0),
        ("Pool",          32, 16,   0,  32),
        ("Courbet",       32, 16,  32,  32),
        ("Sea",           32, 16,  64,  32),
        ("Sunset",        32, 16,  96,  32),
        ("Creebet",       32, 16, 128,  32),
        ("Wanderer",      16, 32,   0,  64),
        ("Graham",        16, 32,  16,  64),
        ("Match",         32, 32,   0, 128),
        ("Bust",          32, 32,  32, 128),
        ("Stage",         32, 32,  64, 128),
        ("Void",          32, 32,  96, 128),
        ("SkullAndRoses", 32, 32, 128, 128),
        ("Fighters",      64, 32,   0,  96),
        ("Pointer",       64, 64,   0, 192),
        ("Pigscene",      64, 64,  64, 192),
        ("BurningSkull",  64, 64, 128, 192),
        ("Skeleton",      64, 48, 192,  64),
        ("DonkeyKong",    64, 48, 192, 112),
    ];

    /// <summary>Display / NBT name. obf: <c>sv.A</c>.</summary>
    public static string  GetName   (this EnumArt art) => Data[(int)art].Name;
    /// <summary>Width in pixels. obf: <c>sv.B</c>.</summary>
    public static int     GetWidthPx(this EnumArt art) => Data[(int)art].W;
    /// <summary>Height in pixels. obf: <c>sv.C</c>.</summary>
    public static int     GetHeightPx(this EnumArt art) => Data[(int)art].H;
    /// <summary>Texture-sheet X offset in pixels. obf: <c>sv.D</c>.</summary>
    public static int     GetSheetX (this EnumArt art) => Data[(int)art].SX;
    /// <summary>Texture-sheet Y offset in pixels. obf: <c>sv.E</c>.</summary>
    public static int     GetSheetY (this EnumArt art) => Data[(int)art].SY;

    /// <summary>
    /// Looks up a variant by its display name (case-sensitive).
    /// Returns null if not found.
    /// </summary>
    public static EnumArt? FromName(string name)
    {
        for (int i = 0; i < Data.Length; i++)
            if (Data[i].Name == name) return (EnumArt)i;
        return null;
    }
}

// ── EntityPainting ────────────────────────────────────────────────────────────

/// <summary>
/// Replica of <c>tj</c> (EntityPainting) — decorative wall painting entity.
/// Extends <see cref="Entity"/> directly (not LivingEntity — no health or AI).
/// Validates its placement every 100 ticks and removes itself if the wall disappears.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityPainting_Spec.md
/// </summary>
public class EntityPainting : Entity
{
    // ── obf field mapping ────────────────────────────────────────────────────
    // a = Facing (0=south 1=west 2=north 3=east)
    // b/c/d = TileX/TileY/TileZ  (anchor block)
    // e = Art (EnumArt variant)
    // f = _tickTimer

    /// <summary>Facing direction 0–3 (0=south, 1=west, 2=north, 3=east). obf: <c>a</c>.</summary>
    public int Facing;

    /// <summary>Anchor tile X. obf: <c>b</c>.</summary>
    public int TileX;

    /// <summary>Anchor tile Y. obf: <c>c</c>.</summary>
    public int TileY;

    /// <summary>Anchor tile Z. obf: <c>d</c>.</summary>
    public int TileZ;

    /// <summary>Painting variant. obf: <c>e</c>.</summary>
    public EnumArt Art = EnumArt.Kebab;

    // Validity check counter. Spec §7: checked every 100 ticks. obf: f
    private int _tickTimer;

    // Painting item RegistryIndex: rawId 65 → 256+65=321. obf: acy.ar
    private const int PaintingItemId = 321;

    // ── Constructors ─────────────────────────────────────────────────────────

    /// <summary>
    /// Basic (world-only) constructor used by NBT factory.
    /// obf: <c>tj(World)</c>.
    /// </summary>
    public EntityPainting(World world) : base(world)
    {
        YOffset  = 0.0f;  // obf: L=0
        SetSize(0.5f, 0.5f);
    }

    /// <summary>
    /// Placement constructor — selects a random valid painting variant.
    /// obf: <c>tj(World, int x, int y, int z, int dir)</c>.
    /// </summary>
    public EntityPainting(World world, int x, int y, int z, int dir) : this(world)
    {
        TileX = x; TileY = y; TileZ = z;

        // Collect all variants that fit in this position
        var candidates = new System.Collections.Generic.List<EnumArt>();
        foreach (EnumArt variant in System.Enum.GetValues<EnumArt>())
        {
            Art = variant;
            ApplyDirectionAndAABB(dir);
            if (IsValidPlacement()) candidates.Add(variant);
        }

        if (candidates.Count == 0)
        {
            // No valid variant — caller should discard this entity
            SetDead();
            return;
        }

        Art = candidates[EntityRandom.NextInt(candidates.Count)];
        ApplyDirectionAndAABB(dir);
    }

    /// <summary>
    /// NBT-load / motif-specific constructor.
    /// obf: <c>tj(World, int x, int y, int z, int dir, String motiveName)</c>.
    /// </summary>
    public EntityPainting(World world, int x, int y, int z, int dir, string motiveName) : this(world)
    {
        TileX = x; TileY = y; TileZ = z;
        Art   = EnumArtInfo.FromName(motiveName) ?? EnumArt.Kebab;
        ApplyDirectionAndAABB(dir);
    }

    protected override void EntityInit() { }

    // ── ApplyDirectionAndAABB ─────────────────────────────────────────────────

    /// <summary>
    /// Sets facing, yaw, entity position, and AABB from the current anchor + Art variant.
    /// obf: <c>b(int dir)</c>.
    /// </summary>
    public void ApplyDirectionAndAABB(int dir)
    {
        Facing   = dir;
        RotationYaw = PrevRotYaw = dir * 90.0f;

        int widthPx  = Art.GetWidthPx();
        int heightPx = Art.GetHeightPx();

        float halfW = widthPx  / 32.0f;
        float halfH = heightPx / 32.0f;
        const float Depth   = 0.5f / 32.0f;  // 0.015625 — thin depth dimension
        const float Offset  = 0.5625f;        // painting protrudes 0.5625 blocks from wall
        const float Epsilon = -0.00625f;      // AABB shrunk inward by this amount on all sides

        // Centering offset for 32px and 64px dimensions
        float centerOffsetW = CenterOffset(widthPx);
        float centerOffsetH = CenterOffset(heightPx);

        // Base center = anchor tile center
        double cx = TileX + 0.5;
        double cy = TileY + 0.5;
        double cz = TileZ + 0.5;

        // Apply facing offset (painting protrudes from wall) + centering
        switch (dir)
        {
            case 0: // south (+Z face)
                cx -= centerOffsetW;
                cy += centerOffsetH;
                cz += Offset;
                break;
            case 1: // west (-X face)
                cx -= Offset;
                cy += centerOffsetH;
                cz += centerOffsetW;
                break;
            case 2: // north (-Z face)
                cx += centerOffsetW;
                cy += centerOffsetH;
                cz -= Offset;
                break;
            case 3: // east (+X face)
                cx += Offset;
                cy += centerOffsetH;
                cz -= centerOffsetW;
                break;
        }

        // Set entity position without AABB rebuild (we set AABB manually below)
        PosX = PrevPosX = LastTickPosX = cx;
        PosY = PrevPosY = LastTickPosY = cy;
        PosZ = PrevPosZ = LastTickPosZ = cz;

        // Compute AABB half-extents per axis
        double hx, hy, hz;
        switch (dir)
        {
            case 0: case 2: // south/north — thin on Z
                hx = halfW; hy = halfH; hz = Depth;
                break;
            default:        // west/east — thin on X
                hx = Depth; hy = halfH; hz = halfW;
                break;
        }

        BoundingBox.Set(
            cx - hx + Epsilon, cy - hy + Epsilon, cz - hz + Epsilon,
            cx + hx - Epsilon, cy + hy - Epsilon, cz + hz - Epsilon);
    }

    // Spec §6 helper c(pixels): centering offset for multi-block paintings.
    private static float CenterOffset(int pixels) => pixels is 32 or 64 ? 0.5f : 0.0f;

    // ── Tick ──────────────────────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick();

        _tickTimer++;
        if (_tickTimer < 100) return;
        _tickTimer = 0;

        if (World == null || World.IsClientSide) return;
        if (!IsValidPlacement())
        {
            SetDead();
            DropPainting();
        }
    }

    // ── Placement Validity ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the painting has valid wall backing and no entity collisions.
    /// obf: <c>g()</c>.
    /// </summary>
    public bool IsValidPlacement()
    {
        if (World == null) return false;

        // Step 1: no solid entity in AABB
        var inAABB = World.GetEntitiesWithinAABBExcluding(this, BoundingBox);
        foreach (var e in inAABB)
            if (e is EntityPainting) return false; // Step 3 check merged here

        // Step 2: each wall block behind painting must be solid
        int tileW = Art.GetWidthPx()  / 16;
        int tileH = Art.GetHeightPx() / 16;

        for (int col = 0; col < tileW; col++)
        for (int row = 0; row < tileH; row++)
        {
            // Compute wall-block position based on facing
            int wx = TileX, wy = TileY + row, wz = TileZ;
            switch (Facing)
            {
                case 0: wx = TileX - col; break;
                case 1: wz = TileZ + col; break;
                case 2: wx = TileX + col; break;
                case 3: wz = TileZ - col; break;
            }

            // Wall block is the block directly behind the painting face
            int wallX = wx, wallY = wy, wallZ = wz;
            switch (Facing)
            {
                case 0: wallZ = TileZ - 1; break;
                case 1: wallX = TileX + 1; break;
                case 2: wallZ = TileZ + 1; break;
                case 3: wallX = TileX - 1; break;
            }

            if (!World.IsOpaqueCube(wallX, wallY, wallZ)) return false;
        }

        return true;
    }

    // ── Damage / Push ─────────────────────────────────────────────────────────

    /// <summary>
    /// Any attack destroys the painting and drops the item.
    /// obf: <c>a(Entity, int)</c>.
    /// </summary>
    public override bool AttackEntityFrom(DamageSource source, int amount)
    {
        if (World == null || World.IsClientSide || IsDead) return true;
        SetDead();
        DropPainting();
        return true;
    }

    /// <summary>
    /// Any non-zero push destroys the painting. obf: <c>b(double,double,double)</c>.
    /// </summary>
    public override void Move(double dx, double dy, double dz)
    {
        if (World != null && !World.IsClientSide && (dx != 0 || dy != 0 || dz != 0))
        {
            SetDead();
            DropPainting();
        }
    }

    // ── Drop helper ───────────────────────────────────────────────────────────

    private void DropPainting()
    {
        if (World == null) return;
        var drop = new EntityItem(World, PosX, PosY, PosZ, new ItemStack(PaintingItemId, 1, 0));
        World.SpawnEntity(drop);
    }

    // ── NBT ───────────────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutByte("Dir",    (byte)Facing);
        tag.PutString("Motive", Art.GetName());
        tag.PutInt("TileX",  TileX);
        tag.PutInt("TileY",  TileY);
        tag.PutInt("TileZ",  TileZ);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        int dir = tag.GetByte("Dir");
        TileX   = tag.GetInt("TileX");
        TileY   = tag.GetInt("TileY");
        TileZ   = tag.GetInt("TileZ");

        string motiveName = tag.GetString("Motive");
        Art = EnumArtInfo.FromName(motiveName) ?? EnumArt.Kebab;

        ApplyDirectionAndAABB(dir);
    }
}
