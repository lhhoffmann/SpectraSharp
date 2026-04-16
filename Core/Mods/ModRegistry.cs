namespace SpectraEngine.Core.Mods;

/// <summary>
/// Runtime registry mods use to register blocks, items, and world/tick hooks.
/// All registrations are accumulated here and applied to the engine at OnLoad time.
/// </summary>
public sealed class ModRegistry : IModRegistry
{
    private readonly Dictionary<int, object> _blocks = [];
    private readonly Dictionary<int, object> _items  = [];
    private readonly List<IWorldGenHook>     _worldGenHooks = [];
    private readonly List<ITickHook>         _tickHooks     = [];

    public IReadOnlyDictionary<int, object> Blocks => _blocks;
    public IReadOnlyDictionary<int, object> Items  => _items;
    public IReadOnlyList<IWorldGenHook>     WorldGenHooks => _worldGenHooks;
    public IReadOnlyList<ITickHook>         TickHooks     => _tickHooks;

    public void RegisterBlock(int id, object block)
    {
        if (id <= 0 || id >= 256) return;
        _blocks[id] = block;
    }

    public void RegisterItem(int id, object item)
    {
        if (id <= 0) return;
        _items[id] = item;
    }

    public void RegisterWorldGenHook(IWorldGenHook hook)
    {
        if (hook != null) _worldGenHooks.Add(hook);
    }

    public void RegisterTickHook(ITickHook hook)
    {
        if (hook != null) _tickHooks.Add(hook);
    }
}
