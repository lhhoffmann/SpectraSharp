using SpectraSharp.Core.Nbt;

namespace SpectraSharp.Core.WorldSave;

/// <summary>
/// Filesystem-backed save handler. Replica of <c>e</c> (SaveHandler).
/// Manages the world directory, session lock, level.dat, and per-dimension chunk loaders.
///
/// Directory layout: spec §3.
/// Session lock: spec §4.
/// level.dat atomic write: spec §6.
/// Dimension routing: spec §3 (DIM-1 / DIM1 / root).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §3–§8
/// </summary>
public sealed class SaveHandler : ISaveHandler
{
    private readonly string _worldDir;        // absolute path to world root
    private readonly long   _lockTimestamp;   // value written to session.lock at open

    // Cached chunk persistence per dimension directory
    private readonly Dictionary<string, DiskChunkLoader> _chunkLoaders = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Opens (or creates) the world at <paramref name="worldDir"/>.
    /// Writes a fresh session.lock immediately.
    /// </summary>
    public SaveHandler(string worldDir)
    {
        _worldDir = Path.GetFullPath(worldDir);
        Directory.CreateDirectory(_worldDir);

        _lockTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        WriteSessionLock();
    }

    public string WorldFolderName => Path.GetFileName(_worldDir);

    // ── Session lock (spec §4) ────────────────────────────────────────────────

    private string LockPath => Path.Combine(_worldDir, "session.lock");

    private void WriteSessionLock()
    {
        // 8-byte big-endian long — DataOutputStream.writeLong
        byte[] buf = new byte[8];
        long ts = _lockTimestamp;
        for (int i = 7; i >= 0; i--)
        {
            buf[i] = (byte)(ts & 0xFF);
            ts >>= 8;
        }
        File.WriteAllBytes(LockPath, buf);
    }

    /// <summary>
    /// Reads session.lock and throws <see cref="SessionLockException"/> if the timestamp
    /// has been changed by another process. Spec: <c>e.b()</c>.
    /// </summary>
    public void VerifySessionLock()
    {
        if (!File.Exists(LockPath))
            throw new SessionLockException("session.lock is missing — world may be corrupted.");

        byte[] buf = File.ReadAllBytes(LockPath);
        if (buf.Length < 8)
            throw new SessionLockException("session.lock is truncated.");

        long stored = 0;
        for (int i = 0; i < 8; i++) stored = (stored << 8) | buf[i];

        if (stored != _lockTimestamp)
            throw new SessionLockException(
                "The save is being accessed from another location, aborting");
    }

    // ── Chunk persistence (spec §3, §8) ──────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="DiskChunkLoader"/> for the given provider's dimension.
    /// Creates DIM-1 / DIM1 sub-directories as needed.
    /// </summary>
    public IChunkPersistence GetChunkPersistence(WorldProvider provider)
    {
        string chunkDir = GetDimensionDirectory(provider);
        if (!_chunkLoaders.TryGetValue(chunkDir, out var loader))
        {
            Directory.CreateDirectory(chunkDir);
            loader = new DiskChunkLoader(chunkDir, _worldDir);
            _chunkLoaders[chunkDir] = loader;
        }
        return loader;
    }

    private string GetDimensionDirectory(WorldProvider provider) => provider.DimensionId switch
    {
        -1 => Path.Combine(_worldDir, "DIM-1"),
        1  => Path.Combine(_worldDir, "DIM1"),
        _  => _worldDir   // Overworld: chunks live directly in world root
    };

    // ── level.dat load (spec §7) ──────────────────────────────────────────────

    /// <summary>
    /// Reads level.dat (fallback to level.dat_old). Returns null if neither exists.
    /// Spec: <c>e.c()</c>.
    /// </summary>
    public WorldInfo? LoadWorldInfo()
    {
        string primary = Path.Combine(_worldDir, "level.dat");
        string backup  = Path.Combine(_worldDir, "level.dat_old");

        foreach (string candidate in new[] { primary, backup })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                NbtCompound root = NbtIo.Read(candidate);
                NbtCompound? data = root.GetCompound("Data");
                if (data == null) continue;
                return new WorldInfo(data);
            }
            catch (Exception) { /* corrupted — try backup */ }
        }
        return null; // new world
    }

    // ── level.dat save (spec §6) ──────────────────────────────────────────────

    /// <summary>
    /// Saves level.dat with player data from the first entity in <paramref name="players"/>.
    /// Spec: <c>e.a(si, List&lt;vi&gt;)</c>.
    /// </summary>
    public void SaveLevelDat(WorldInfo info, IReadOnlyList<EntityPlayer> players)
    {
        NbtCompound? playerTag = null;
        if (players.Count > 0)
        {
            playerTag = new NbtCompound();
            players[0].SaveToNbt(playerTag);
        }
        WriteLevelDat(info, playerTag);
    }

    /// <summary>
    /// Saves level.dat using the cached player tag (si.h). Spec: <c>e.a(si)</c>.
    /// </summary>
    public void SaveLevelDat(WorldInfo info)
        => WriteLevelDat(info, info.CachedPlayerTag);

    private void WriteLevelDat(WorldInfo info, NbtCompound? playerTag)
    {
        var data = new NbtCompound();
        info.Write(data);
        if (playerTag != null)
            data.PutCompound("Player", playerTag);

        var root = new NbtCompound();
        root.PutCompound("Data", data);

        // Atomic write sequence (spec §6):
        // 1. Write to level.dat_new
        // 2. Rename level.dat → level.dat_old
        // 3. Rename level.dat_new → level.dat
        string path    = Path.Combine(_worldDir, "level.dat");
        string pathOld = Path.Combine(_worldDir, "level.dat_old");
        string pathNew = Path.Combine(_worldDir, "level.dat_new");

        NbtIo.Write(root, pathNew);

        if (File.Exists(pathOld)) File.Delete(pathOld);
        if (File.Exists(path))    File.Move(path, pathOld);
        if (File.Exists(path))    File.Delete(path); // if rename failed on some OSes
        File.Move(pathNew, path);
        if (File.Exists(pathNew)) File.Delete(pathNew);
    }
}
