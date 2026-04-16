using SpectraEngine.Core.Nbt;

namespace SpectraEngine.Core.WorldSave;

/// <summary>
/// Level metadata container. Replica of <c>si</c> (WorldInfo).
/// Serialized to/from the <c>"Data"</c> TAG_Compound inside <c>level.dat</c>.
///
/// Field naming follows spec §14 accessor table.
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §6, §7, §14
/// </summary>
public sealed class WorldInfo
{
    // ── Fields (spec §14) ────────────────────────────────────────────────────

    public long   Seed           { get; set; }   // a — RandomSeed
    public int    SpawnX         { get; set; }   // b
    public int    SpawnY         { get; set; }   // c
    public int    SpawnZ         { get; set; }   // d
    public long   Time           { get; set; }   // e — game tick counter
    public long   SizeOnDisk     { get; set; }   // g — rolling file-size counter
    public string LevelName      { get; set; } = ""; // j
    public int    Version        { get; set; }   // k — save format version
    public long   LastPlayed     { get; set; }   // f — read from disk; not updated by code
    public bool   Thundering     { get; set; }   // n
    public int    ThunderTime    { get; set; }   // o
    public bool   Raining        { get; set; }   // l
    public int    RainTime       { get; set; }   // m
    public int    GameType       { get; set; }   // p — 0 = Survival, 1 = Creative
    public bool   MapFeatures    { get; set; } = true; // q — generate structures
    public bool   Hardcore       { get; set; }   // r

    /// <summary>Cached player TAG_Compound (si.h) — read from disk, re-written if still valid.</summary>
    public NbtCompound? CachedPlayerTag { get; set; }  // h

    /// <summary>Player's dimension ID (si.i) — read from CachedPlayerTag["Dimension"].</summary>
    public int PlayerDimension { get; set; }            // i

    // ── Default constructor (new world) ──────────────────────────────────────

    public WorldInfo() { }

    // ── Deserialise from "Data" compound (spec §7) ───────────────────────────

    public WorldInfo(NbtCompound data)
    {
        Seed        = data.GetLong("RandomSeed");
        GameType    = data.GetInt("GameType");
        MapFeatures = data.HasKey("MapFeatures") ? data.GetBoolean("MapFeatures") : true;
        SpawnX      = data.GetInt("SpawnX");
        SpawnY      = data.GetInt("SpawnY");
        SpawnZ      = data.GetInt("SpawnZ");
        Time        = data.GetLong("Time");
        SizeOnDisk  = data.GetLong("SizeOnDisk");
        LastPlayed  = data.GetLong("LastPlayed");
        LevelName   = data.GetString("LevelName");
        Version     = data.GetInt("version");
        Raining     = data.GetBoolean("raining");
        RainTime    = data.GetInt("rainTime");
        Thundering  = data.GetBoolean("thundering");
        ThunderTime = data.GetInt("thunderTime");
        Hardcore    = data.GetBoolean("hardcore");

        if (data.HasKey("Player"))
        {
            CachedPlayerTag = data.GetCompound("Player");
            PlayerDimension = CachedPlayerTag?.GetInt("Dimension") ?? 0;
        }
    }

    // ── Serialise into "Data" compound ───────────────────────────────────────

    /// <summary>
    /// Writes all fields into <paramref name="data"/> for use in level.dat.
    /// Spec: <c>si.a(ik, ik)</c> / <c>si.a(ik)</c>.
    /// </summary>
    public void Write(NbtCompound data)
    {
        data.PutLong("RandomSeed",  Seed);
        data.PutInt("GameType",     GameType);
        data.PutBoolean("MapFeatures", MapFeatures);
        data.PutInt("SpawnX",       SpawnX);
        data.PutInt("SpawnY",       SpawnY);
        data.PutInt("SpawnZ",       SpawnZ);
        data.PutLong("Time",        Time);
        data.PutLong("SizeOnDisk",  SizeOnDisk);
        data.PutLong("LastPlayed",  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        data.PutString("LevelName", LevelName);
        data.PutInt("version",      Version);
        data.PutBoolean("raining",  Raining);
        data.PutInt("rainTime",     RainTime);
        data.PutBoolean("thundering", Thundering);
        data.PutInt("thunderTime",  ThunderTime);
        data.PutBoolean("hardcore", Hardcore);
    }

    // ── Spawn setter (spec §14 setSpawn(x,y,z)) ──────────────────────────────

    public void SetSpawn(int x, int y, int z) { SpawnX = x; SpawnY = y; SpawnZ = z; }
}
