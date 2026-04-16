using System.Reflection;
using System.Runtime.Loader;

namespace SpectraEngine.Core.Mods;

/// <summary>
/// Scans mods/compiled/ at startup, loads every mod DLL into its own
/// AssemblyLoadContext and calls ISpectraMod.OnLoad(engine).
/// </summary>
public sealed class ModLoader(IEngine engine)
{
    readonly List<(AssemblyLoadContext Ctx, ISpectraMod Mod)> _loaded = [];

    public IReadOnlyList<ISpectraMod> LoadedMods =>
        _loaded.Select(x => x.Mod).ToList();

    /// <summary>
    /// Loads all mod DLLs found in the compiled output directory.
    /// Safe to call multiple times — already-loaded mods are skipped.
    /// </summary>
    public void LoadAll(string compiledDir)
    {
        if (!Directory.Exists(compiledDir))
            return;

        foreach (string dll in Directory.EnumerateFiles(compiledDir, "*.dll"))
        {
            if (_loaded.Any(x => x.Ctx.Name == dll))
                continue;

            Load(dll);
        }
    }

    /// <summary>Loads a single mod DLL and calls OnLoad.</summary>
    public void Load(string dllPath)
    {
        var ctx  = new AssemblyLoadContext(dllPath, isCollectible: true);
        var asm  = ctx.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

        var modTypes = asm.GetTypes()
            .Where(t => typeof(ISpectraMod).IsAssignableFrom(t)
                     && !t.IsAbstract
                     && t.GetConstructor(Type.EmptyTypes) != null);

        foreach (var type in modTypes)
        {
            var mod = (ISpectraMod)Activator.CreateInstance(type)!;
            mod.OnLoad(engine);
            _loaded.Add((ctx, mod));
            Console.WriteLine($"[ModLoader] Loaded mod '{mod.DisplayName}' v{mod.Version} ({mod.ModId})");
        }
    }

    /// <summary>
    /// Calls OnUnload() on every loaded mod and releases the AssemblyLoadContexts.
    /// </summary>
    public void UnloadAll()
    {
        foreach (var (ctx, mod) in _loaded)
        {
            try   { mod.OnUnload(); }
            catch (Exception ex) { Console.Error.WriteLine($"[ModLoader] Error unloading '{mod.ModId}': {ex.Message}"); }

            ctx.Unload();
        }
        _loaded.Clear();
    }
}
