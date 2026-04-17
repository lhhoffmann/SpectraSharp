// Stub for net.minecraftforge.registries.DeferredRegister — Forge 1.16.5
// The primary registration mechanism since 1.14, widely used in 1.16.5 mods.

using net.minecraft.util;

namespace net.minecraftforge.registries;

/// <summary>
/// MinecraftStubs v1_16 — DeferredRegister&lt;T&gt;.
///
/// Usage in mods:
///   static final DeferredRegister&lt;Block&gt; BLOCKS =
///       DeferredRegister.create(ForgeRegistries.BLOCKS, "mymod");
///
///   static final RegistryObject&lt;Block&gt; MY_BLOCK =
///       BLOCKS.register("my_block", () -> new Block(...));
///
/// All entries accumulate until the FMLCommonSetupEvent fires, when they are
/// flushed to the underlying GameRegistry.
/// </summary>
public sealed class DeferredRegister<T> where T : class
{
    readonly string _modid;
    readonly IForgeRegistry<T> _registry;
    readonly List<(string name, Func<T> factory)> _entries = [];

    DeferredRegister(string modid, IForgeRegistry<T> registry)
    {
        _modid    = modid;
        _registry = registry;
    }

    /// <summary>Creates a DeferredRegister for the given registry and modid.</summary>
    public static DeferredRegister<T> create(IForgeRegistry<T> registry, string modid)
        => new(modid, registry);

    /// <summary>
    /// Queues a registry entry.  Returns a RegistryObject whose get() will
    /// work after the register event fires.
    /// </summary>
    public RegistryObject<T> register(string name, Func<T> factory)
    {
        var obj = new RegistryObject<T>($"{_modid}:{name}");
        _entries.Add((name, () =>
        {
            var value = factory();
            if (value is net.minecraft.block.Block b)
                b.setRegistryName(_modid, name);
            else if (value is net.minecraft.item.Item it)
                it.setRegistryName(_modid, name);
            obj.SetValue(value);
            return value;
        }));
        return obj;
    }

    /// <summary>
    /// Flushes all pending registrations.
    /// Called by ForgeMod1_16Wrapper during the FMLCommonSetupEvent.
    /// </summary>
    internal void Flush()
    {
        foreach (var (name, factory) in _entries)
        {
            try
            {
                var value = factory();
                _registry.register(value);
                Console.WriteLine($"[DeferredRegister] Registered {_modid}:{name}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[DeferredRegister] Failed to register {_modid}:{name}: {ex.Message}");
            }
        }
        _entries.Clear();
    }
}

// ── RegistryObject ────────────────────────────────────────────────────────────

/// <summary>
/// Lazy handle returned by DeferredRegister.register().
/// The value is null until the registration event fires.
/// </summary>
public sealed class RegistryObject<T>(string registryName) where T : class
{
    T? _value;

    public ResourceLocation getRegistryName() => new(registryName);

    /// <summary>Returns the registered object. Null before registration completes.</summary>
    public T? get() => _value;

    internal void SetValue(T value) => _value = value;

    public bool isPresent() => _value != null;
}

// ── IForgeRegistry ────────────────────────────────────────────────────────────

/// <summary>Generic Forge registry interface — implemented by ForgeRegistries entries.</summary>
public interface IForgeRegistry<T> where T : class
{
    void register(T entry);
    T? getValue(ResourceLocation name);
}
