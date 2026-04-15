namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>k</c> (WorldProvider) — abstract base class encapsulating dimension-specific
/// rules: sky-light presence, brightness lookup table, sun/moon angle, sky colour, and
/// ChunkLoader factory.
///
/// Subclasses (spec §6):
///   <c>ix</c>  (dim  0) = <see cref="OverworldProvider"/>
///   <c>aau</c> (dim −1) = <see cref="NetherProvider"/>
///   <c>ol</c>  (dim  1) = <see cref="EndProvider"/>
///
/// Quirks preserved (see spec §7):
///   1. GetSunriseColor returns the shared <c>h[]</c> buffer by reference — not safe to cache.
///   2. GetSunAngle uses Math.Cos (double precision), not MathHelper.Cos.
///   3. Brightness formula is a non-linear perceptual curve — must match exactly.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldProvider_Spec.md
/// </summary>
public abstract class WorldProvider
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    public  World?  WorldRef;                  // obf: a — set by RegisterWorld
    // obf: b — WorldChunkManager (vh); created by CreateWorldChunkManager
    /// <summary>
    /// obf: c — sleeping-disabled flag. True in dimensions where beds explode (Nether, End).
    /// Checked by <see cref="EntityPlayer.TrySleep"/> — returns <see cref="EnumSleepResult.WrongDimension"/> when set.
    /// Spec: BlockBed_Spec §12.1.b.
    /// </summary>
    public  bool    SleepingDisabled;          // obf: c
    public  bool    IsNether;                  // obf: e — sky-light suppressed when true
    public  readonly float[] BrightnessTable = new float[16]; // obf: f[16]
    public  int     DimensionId;               // obf: g

    private readonly float[] _sunriseColorBuffer = new float[4]; // obf: h — reused RGBA buffer (quirk 1)

    // ── Registration (spec §4) ────────────────────────────────────────────────

    /// <summary>
    /// Called by World constructor. Sets world reference, creates WorldChunkManager,
    /// fills brightness table. Spec: <c>a(ry world)</c> (final).
    /// </summary>
    public void RegisterWorld(World world)
    {
        WorldRef = world;
        CreateWorldChunkManager();
        CalculateBrightnessTable();
    }

    /// <summary>
    /// Creates the WorldChunkManager for this dimension. Spec: protected <c>b()</c>.
    /// Overworld: new vh(WorldRef). Nether subclass would create a single-biome manager.
    /// </summary>
    protected virtual void CreateWorldChunkManager()
    {
        if (WorldRef != null)
            WorldRef.ChunkManager = new WorldChunkManager(WorldRef.WorldSeed);
    }

    /// <summary>
    /// Fills <see cref="BrightnessTable"/> using the perceptual gamma curve.
    /// Spec: protected <c>a()</c>. Quirk 3: formula is non-linear, must match exactly.
    ///
    /// Formula: <c>f[i] = (1−var3) / (var3×3+1) × (1−ambient) + ambient</c>
    /// where <c>var3 = 1 − i/15</c>.
    /// </summary>
    protected virtual void CalculateBrightnessTable()
    {
        float ambient = GetAmbientLight();
        for (int i = 0; i <= 15; i++)
        {
            float var3 = 1.0f - (float)i / 15.0f;
            BrightnessTable[i] = (1.0f - var3) / (var3 * 3.0f + 1.0f) * (1.0f - ambient) + ambient;
        }
    }

    /// <summary>
    /// Ambient minimum brightness used in <see cref="CalculateBrightnessTable"/>.
    /// Overworld = 0.0, Nether = 0.1 (prevents complete darkness).
    /// </summary>
    protected virtual float GetAmbientLight() => 0.0f;

    // ── ChunkLoader factory (spec §4) ─────────────────────────────────────────

    /// <summary>
    /// Creates the ChunkLoader for this dimension. Spec: <c>c()</c> → <c>ej</c>.
    /// Returns null — concrete implementation requires file I/O system (xj spec pending).
    /// </summary>
    public virtual IChunkLoader? CreateChunkLoader() => null;

    // ── Time / celestial (spec §4) ────────────────────────────────────────────

    /// <summary>
    /// Normalized celestial angle (0–1) for the given world time.
    /// Spec: <c>a(long worldTime, float partialTick)</c> → float. Quirk 2: uses Math.Cos.
    /// 0.0 = sunrise, 0.25 ≈ noon, 0.5 = sunset, 0.75 = midnight.
    /// </summary>
    public float GetSunAngle(long worldTime, float partialTick)
    {
        int  timeOfDay = (int)(worldTime % 24000L);
        float t        = (timeOfDay + partialTick) / 24000.0f - 0.25f;
        if (t < 0.0f) t += 1.0f;
        if (t > 1.0f) t -= 1.0f;
        // Quirk 2: Math.Cos (double precision), not MathHelper.Cos
        float skew = 1.0f - (float)((Math.Cos((double)t * Math.PI) + 1.0) / 2.0);
        return t + (skew - t) / 3.0f;
    }

    /// <summary>
    /// Moon phase 0–7, cycling every 8 in-game days. Spec: <c>b(long, float)</c> → int.
    /// </summary>
    public int GetMoonPhase(long worldTime, float partialTick)
        => (int)(worldTime / 24000L) % 8;

    /// <summary>
    /// Sunrise/sunset colour as RGBA float[4], or null outside the window (|sunY| > 0.4).
    /// Quirk 1: returns a reference to the reused private buffer — do not cache.
    /// Spec: <c>a(float sunAngle, float rain)</c> → float[] or null.
    /// </summary>
    public float[]? GetSunriseColor(float sunAngle, float rain)
    {
        float y = MathHelper.Sin(sunAngle * MathF.PI * 2.0f);
        if (y < -0.4f || y > 0.4f) return null;

        float t     = y / 0.4f * 0.5f + 0.5f;
        float alpha = 1.0f - (1.0f - MathHelper.Sin(t * MathF.PI)) * 0.99f;
        alpha *= alpha;
        _sunriseColorBuffer[0] = t * 0.3f + 0.7f;         // R
        _sunriseColorBuffer[1] = t * t * 0.7f + 0.2f;     // G
        _sunriseColorBuffer[2] = t * t * 0.0f + 0.2f;     // B
        _sunriseColorBuffer[3] = alpha;                    // A
        return _sunriseColorBuffer;
    }

    /// <summary>
    /// Overworld sky/fog base colour at the given sun angle, modulated by rain.
    /// Spec: <c>b(float sunAngle, float rain)</c> → Vec3.
    /// </summary>
    public Vec3 GetSkyColor(float sunAngle, float rain)
    {
        float brightness = MathHelper.Sin(sunAngle * MathF.PI * 2.0f) * 2.0f + 0.5f;
        brightness = Math.Clamp(brightness, 0.0f, 1.0f);
        float r = 0.7529412f * (brightness * 0.94f + 0.06f);
        float g = 0.84705883f * (brightness * 0.94f + 0.06f);
        float b = 1.0f * (brightness * 0.91f + 0.09f);
        return Vec3.GetFromPool(r, g, b);
    }

    // ── Dimension queries (spec §4) ───────────────────────────────────────────

    /// <summary>obf: d() — true if this dimension has sky-light. Default true (Overworld).</summary>
    public virtual bool HasSkyLight() => true;

    /// <summary>obf: e() — world height as float. Returns 128.0F.</summary>
    public virtual float GetWorldHeight() => 128.0f;

    /// <summary>obf: f() — unknown boolean, returns true in base class.</summary>
    public virtual bool UnknownF() => true;

    // ── Spawn validity (spec §4) ──────────────────────────────────────────────

    /// <summary>
    /// True if the top block at (x, z) is a grass block — valid overworld spawn.
    /// Spec: <c>a(int x, int z)</c> → bool. Requires world reference.
    /// </summary>
    public virtual bool IsValidSpawnBlock(int x, int z)
    {
        if (WorldRef == null) return false;
        // Block.BlocksList[2] = grass (id 2) — direct ID check
        int topY = WorldRef.GetChunkFromBlockCoords(x, z).GetHeightAt(x & 15, z & 15);
        if (topY == 0) return false;
        int id = WorldRef.GetBlockId(x, topY - 1, z);
        return id == 2; // grass block
    }

    // ── Static factory (spec §5) ──────────────────────────────────────────────

    /// <summary>
    /// Returns the WorldProvider for the given dimension ID.
    /// Spec: static <c>k.a(int dimensionId)</c>.
    /// </summary>
    public static WorldProvider Create(int dimensionId) => dimensionId switch
    {
        -1 => new NetherProvider(),
        1  => new EndProvider(),
        _  => new OverworldProvider(),  // 0 = Overworld; unknown dims fall through
    };
}

// ── Dimension subclasses (spec §6) ────────────────────────────────────────────

/// <summary>
/// <c>ix</c> — Overworld (dim 0). Default behaviour; all base-class values.
/// </summary>
public sealed class OverworldProvider : WorldProvider
{
    public OverworldProvider() => DimensionId = 0;
}

/// <summary>
/// <c>aau</c> — Nether (dim −1). No sky-light; higher ambient brightness (0.1).
/// </summary>
public sealed class NetherProvider : WorldProvider
{
    public NetherProvider() { DimensionId = -1; IsNether = true; SleepingDisabled = true; }
    public override bool HasSkyLight() => false;
    protected override float GetAmbientLight() => 0.1f; // prevents complete darkness
}

/// <summary>
/// <c>ol</c> — The End (dim 1). No sky-light; void fog sky.
/// </summary>
public sealed class EndProvider : WorldProvider
{
    public EndProvider() { DimensionId = 1; IsNether = true; SleepingDisabled = true; }
    public override bool HasSkyLight() => false;
}
