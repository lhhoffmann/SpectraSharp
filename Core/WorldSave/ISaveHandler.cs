namespace SpectraSharp.Core.WorldSave;

/// <summary>
/// World-level save handler interface. Replica of <c>nh</c> (ISaveHandler).
/// Manages level.dat, session lock, and dimension-routed IChunkPersistence instances.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §13
/// </summary>
public interface ISaveHandler
{
    /// <summary>
    /// Loads WorldInfo from level.dat (with level.dat_old fallback).
    /// Returns null if no save exists (new world). Spec: <c>nh.c()</c>.
    /// </summary>
    WorldInfo? LoadWorldInfo();

    /// <summary>
    /// Reads the session.lock file and verifies it matches the timestamp written at open.
    /// Throws <see cref="SessionLockException"/> if another process has taken ownership.
    /// Spec: <c>nh.b()</c>.
    /// </summary>
    void VerifySessionLock();

    /// <summary>
    /// Returns the <see cref="IChunkPersistence"/> for the given WorldProvider's dimension.
    /// Creates the directory if needed. Spec: <c>nh.a(k provider)</c>.
    /// </summary>
    IChunkPersistence GetChunkPersistence(WorldProvider provider);

    /// <summary>
    /// Saves level.dat. If <paramref name="players"/> is non-empty, the first player's
    /// NBT data is written as the "Player" compound. Spec: <c>nh.a(si, List&lt;vi&gt;)</c>.
    /// </summary>
    void SaveLevelDat(WorldInfo info, IReadOnlyList<EntityPlayer> players);

    /// <summary>
    /// Saves level.dat without writing a player tag (uses cached tag in WorldInfo if present).
    /// Spec: <c>nh.a(si)</c>.
    /// </summary>
    void SaveLevelDat(WorldInfo info);

    /// <summary>Returns the world folder name. Spec: <c>nh.d()</c>.</summary>
    string WorldFolderName { get; }
}

/// <summary>
/// Thrown when the session.lock file has been modified by another process.
/// Replica of <c>adl</c> (SessionLockException).
/// </summary>
public sealed class SessionLockException : Exception
{
    public SessionLockException(string message) : base(message) { }
}
