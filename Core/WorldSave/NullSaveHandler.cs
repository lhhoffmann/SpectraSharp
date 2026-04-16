namespace SpectraEngine.Core.WorldSave;

/// <summary>
/// No-op save handler for worlds that should not persist to disk.
/// Replica of <c>bi</c> (NullSaveHandler).
/// All methods are no-ops or return null/empty values.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §1
/// </summary>
public sealed class NullSaveHandler : ISaveHandler
{
    public static readonly NullSaveHandler Instance = new();

    public string WorldFolderName => "";

    public WorldInfo?          LoadWorldInfo()                                       => null;
    public void                VerifySessionLock()                                   { }
    public IChunkPersistence   GetChunkPersistence(WorldProvider provider)           => NullChunkPersistence.Instance;
    public void                SaveLevelDat(WorldInfo info, IReadOnlyList<EntityPlayer> players) { }
    public void                SaveLevelDat(WorldInfo info)                          { }
}

/// <summary>No-op chunk persistence paired with <see cref="NullSaveHandler"/>.</summary>
internal sealed class NullChunkPersistence : IChunkPersistence
{
    internal static readonly NullChunkPersistence Instance = new();

    public Chunk? LoadChunk(World world, int chunkX, int chunkZ) => null;
    public void   SaveChunk(World world, Chunk chunk)             { }
    public void   PostSave(World world, Chunk chunk)              { }
    public void   Flush()                                         { }
    public void   Close()                                         { }
}
