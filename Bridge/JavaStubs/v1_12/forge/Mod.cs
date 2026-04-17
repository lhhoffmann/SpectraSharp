// Stub for @Mod annotation and FML lifecycle — Minecraft Forge 1.12
// Java: net.minecraftforge.fml.common.Mod

using SpectraEngine.Core.Mods;
using net.minecraft.block;
using net.minecraft.item;
using net.minecraftforge.fml.common.registry;

namespace net.minecraftforge.fml.common;

/// <summary>
/// MinecraftStubs v1_12 — @Mod annotation stub.
///
/// IKVM translates Java annotations to .NET attributes.
/// A Forge mod class annotated with @Mod becomes a .NET class with [ModAttribute].
///
/// ModLoader discovers Forge mods by scanning for types carrying [ModAttribute]
/// and wraps them in a <see cref="ForgeMod1_12Wrapper"/> that implements ISpectraMod.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ModAttribute : Attribute
{
    public string modid   { get; }
    public string name    { get; init; } = "";
    public string version { get; init; } = "0.0.0";

    public ModAttribute(string modid) => this.modid = modid;
}

/// <summary>
/// Marks a method as a Forge lifecycle handler.
/// IKVM preserves this annotation as a .NET attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EventHandlerAttribute : Attribute { }

/// <summary>
/// @Mod.EventBusSubscriber — class-level annotation.
/// Classes carrying this attribute are auto-registered to the Forge EVENT_BUS.
/// Their static methods annotated with @SubscribeEvent are invoked when matching events fire.
///
/// ForgeMod1_12Wrapper detects this during OnLoad and calls EVENT_BUS.register().
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class EventBusSubscriberAttribute : Attribute
{
    /// <summary>The modid that owns this subscriber (optional — defaults to any).</summary>
    public string modid { get; init; } = "";
}

// ── FML lifecycle event types ─────────────────────────────────────────────────

/// <summary>
/// Fired during pre-initialization — mods register blocks, items, and sounds here.
/// Corresponds to Forge's FMLPreInitializationEvent.
/// </summary>
public sealed class FMLPreInitializationEvent
{
    // TODO: expose mod-specific logger once ILogger contract is defined
}

/// <summary>Fired during initialization — mods register recipes and ore dictionary entries.</summary>
public sealed class FMLInitializationEvent { }

/// <summary>Fired after all mods have initialized — for cross-mod integration.</summary>
public sealed class FMLPostInitializationEvent { }

/// <summary>Fired on dedicated server startup.</summary>
public sealed class FMLServerStartingEvent { }

// ── ISpectraMod bridge ────────────────────────────────────────────────────────

/// <summary>
/// Wraps a Forge @Mod class in ISpectraMod so ModLoader can drive the lifecycle.
///
/// Looks for @EventHandler-annotated methods accepting FMLPreInitializationEvent,
/// FMLInitializationEvent, FMLPostInitializationEvent and calls them in order.
/// </summary>
public sealed class ForgeMod1_12Wrapper(object modInstance) : ISpectraMod
{
    readonly System.Reflection.MethodInfo[] _preInit  = FindHandlers(modInstance, typeof(FMLPreInitializationEvent));
    readonly System.Reflection.MethodInfo[] _init     = FindHandlers(modInstance, typeof(FMLInitializationEvent));
    readonly System.Reflection.MethodInfo[] _postInit = FindHandlers(modInstance, typeof(FMLPostInitializationEvent));

    public string ModId =>
        modInstance.GetType().GetCustomAttributes(typeof(ModAttribute), false)
            .OfType<ModAttribute>().FirstOrDefault()?.modid ?? modInstance.GetType().Name;

    public string DisplayName =>
        modInstance.GetType().GetCustomAttributes(typeof(ModAttribute), false)
            .OfType<ModAttribute>().FirstOrDefault()?.name ?? ModId;

    public string Version =>
        modInstance.GetType().GetCustomAttributes(typeof(ModAttribute), false)
            .OfType<ModAttribute>().FirstOrDefault()?.version ?? "0.0.0";

    public void OnLoad(IEngine engine)
    {
        // Set GameRegistry engine reference so register() calls route to Core.
        GameRegistry.Engine = engine;

        // Auto-register all @Mod.EventBusSubscriber classes in the same assembly.
        // These classes use static @SubscribeEvent methods instead of instance @EventHandler.
        RegisterEventBusSubscribers(modInstance.GetType().Assembly);

        var pre  = new FMLPreInitializationEvent();
        var init = new FMLInitializationEvent();
        var post = new FMLPostInitializationEvent();

        foreach (var m in _preInit)  Invoke(m, pre);
        foreach (var m in _init)     Invoke(m, init);
        foreach (var m in _postInit) Invoke(m, post);

        GameRegistry.Engine = null;
    }

    public void OnUnload() { }

    static void RegisterEventBusSubscribers(System.Reflection.Assembly asm)
    {
        foreach (var type in asm.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(EventBusSubscriberAttribute), false).Length == 0)
                continue;

            // Register the TYPE OBJECT itself — ForgeEventBus.register(object) handles
            // both instance and static methods annotated with @SubscribeEvent.
            net.minecraftforge.common.MinecraftForge.EVENT_BUS.register(
                new StaticSubscriberProxy(type));
        }
    }

    /// <summary>
    /// Proxy passed to ForgeEventBus.register() for @Mod.EventBusSubscriber classes.
    /// Holds the Type so the bus can find static @SubscribeEvent methods on it.
    /// </summary>
    internal sealed class StaticSubscriberProxy(Type subscriberType)
    {
        internal Type SubscriberType => subscriberType;
    }

    void Invoke(System.Reflection.MethodInfo m, object arg)
    {
        try { m.Invoke(modInstance, [arg]); }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ForgeMod] {ModId}.{m.Name} threw: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    static System.Reflection.MethodInfo[] FindHandlers(object instance, Type paramType)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance;

        return instance.GetType()
            .GetMethods(flags)
            .Where(m => m.GetCustomAttributes(typeof(EventHandlerAttribute), false).Length > 0
                     && m.GetParameters().Length == 1
                     && m.GetParameters()[0].ParameterType == paramType)
            .ToArray();
    }
}
