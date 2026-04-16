namespace SpectraEngine.Core.Mods;

/// <summary>
/// Runtime registry mods use to register blocks, items, recipes, and hooks.
/// </summary>
public interface IModRegistry
{
    void RegisterBlock(int id, object block);
    void RegisterItem(int id, object item);
    void RegisterWorldGenHook(IWorldGenHook hook);
    void RegisterTickHook(ITickHook hook);
}

public interface IWorldGenHook
{
    void OnPopulateChunk(IWorld world, System.Random rng, int chunkX, int chunkZ);
}

public interface ITickHook
{
    void OnTick(IWorld world);
}
