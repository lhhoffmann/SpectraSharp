// Stub for net.minecraftforge.fml.common.Mod — Forge 1.16.5
// Namespace is same as 1.12; lifecycle events changed to FMLCommonSetupEvent.

using SpectraEngine.Core.Mods;
using net.minecraftforge.registries;

namespace net.minecraftforge.fml.common
{
    /// <summary>
    /// MinecraftStubs v1_16 — @Mod annotation.
    /// Same namespace as 1.12 (net.minecraftforge.fml.common).
    /// Lifecycle event class names changed from FMLInitializationEvent to
    /// FMLCommonSetupEvent etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ModAttribute : Attribute
    {
        public string value { get; }  // modid
        public string modid => value;
        public ModAttribute(string value) => this.value = value;
    }

    /// <summary>Marks a method as receiving a lifecycle or bus event.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class EventHandlerAttribute : Attribute { }

    /// <summary>
    /// @Mod.EventBusSubscriber — class-level annotation for static @SubscribeEvent methods.
    /// In 1.16 this is nested in @Mod: @Mod.EventBusSubscriber(modid="...", bus=Bus.MOD)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EventBusSubscriberAttribute : Attribute
    {
        public string modid { get; init; } = "";
        public Bus    bus   { get; init; } = Bus.FORGE;

        public enum Bus { FORGE, MOD }
    }

    /// <summary>
    /// Wraps a 1.16.5 Forge @Mod class in ISpectraMod.
    /// Lifecycle: @Mod constructor is used, then FMLCommonSetupEvent handlers.
    /// DeferredRegisters are flushed during setup.
    /// </summary>
    public sealed class ForgeMod1_16Wrapper(object modInstance) : ISpectraMod
    {
        readonly System.Reflection.MethodInfo[] _setup =
            FindHandlers(modInstance,
                typeof(net.minecraftforge.fml.@event.lifecycle.FMLCommonSetupEvent));

        readonly List<object> _deferredRegisters = FindDeferredRegisters(modInstance);

        public string ModId =>
            modInstance.GetType().GetCustomAttributes(typeof(ModAttribute), false)
                .OfType<ModAttribute>().FirstOrDefault()?.modid
                ?? modInstance.GetType().Name;

        public string DisplayName => ModId;
        public string Version     => "1.16";

        public void OnLoad(IEngine engine)
        {
            foreach (var dr in _deferredRegisters)
                FlushDeferredRegister(dr);

            var evt = new net.minecraftforge.fml.@event.lifecycle.FMLCommonSetupEvent();
            foreach (var m in _setup) Invoke(m, evt);
        }

        public void OnUnload() { }

        void Invoke(System.Reflection.MethodInfo m, object arg)
        {
            try { m.Invoke(modInstance, [arg]); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ForgeMod1_16] {ModId}.{m.Name} threw: " +
                    $"{ex.InnerException?.Message ?? ex.Message}");
            }
        }

        static void FlushDeferredRegister(object dr)
        {
            var flush = dr.GetType().GetMethod("Flush",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            flush?.Invoke(dr, null);
        }

        static List<object> FindDeferredRegisters(object instance)
        {
            var result = new List<object>();
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public    |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static    |
                System.Reflection.BindingFlags.Instance;

            foreach (var field in instance.GetType().GetFields(flags))
            {
                var ft = field.FieldType;
                if (ft.IsGenericType &&
                    ft.GetGenericTypeDefinition() == typeof(DeferredRegister<>))
                {
                    var val = field.GetValue(field.IsStatic ? null : instance);
                    if (val != null) result.Add(val);
                }
            }
            return result;
        }

        static System.Reflection.MethodInfo[] FindHandlers(object instance, Type paramType)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public    |
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
}

namespace net.minecraftforge.fml.@event.lifecycle
{
    /// <summary>Replaces FMLInitializationEvent in 1.16 — fired for common setup.</summary>
    public sealed class FMLCommonSetupEvent { }

    /// <summary>Fired on dedicated server startup.</summary>
    public sealed class FMLDedicatedServerSetupEvent { }
}
