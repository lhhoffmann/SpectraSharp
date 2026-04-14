namespace SpectraSharp.Core.WorldSave;

/// <summary>
/// Per-dimension chunk disk I/O interface. Replica of <c>d</c> (IChunkLoader) in the save system.
/// Named IChunkPersistence to avoid collision with <see cref="IChunkLoader"/> (in-memory manager).
///
/// Each dimension (Overworld, Nether, End) gets its own instance pointing at the correct
/// sub-directory of the world folder (see SaveHandler dimension routing).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §12
/// </summary>
public interface IChunkPersistence
{
    /// <summary>
    /// Loads a chunk from disk. Returns null if the chunk file does not exist yet.
    /// Spec: <c>d.a(ry world, int x, int z)</c>.
    /// </summary>
    Chunk? LoadChunk(World world, int chunkX, int chunkZ);

    /// <summary>
    /// Saves a chunk to disk via the tmp_chunk.dat atomic rename.
    /// Calls world.VerifySessionLock() before writing.
    /// Spec: <c>d.a(ry world, zx chunk)</c>.
    /// </summary>
    void SaveChunk(World world, Chunk chunk);

    /// <summary>
    /// Post-save / secondary flush. No-op in <c>gy</c> (DiskChunkLoader).
    /// Spec: <c>d.b(ry world, zx chunk)</c>.
    /// </summary>
    void PostSave(World world, Chunk chunk);

    /// <summary>Flush / close. No-op in <c>gy</c>. Spec: <c>d.a()</c>.</summary>
    void Flush();

    /// <summary>Secondary close. No-op in <c>gy</c>. Spec: <c>d.b()</c>.</summary>
    void Close();
}
