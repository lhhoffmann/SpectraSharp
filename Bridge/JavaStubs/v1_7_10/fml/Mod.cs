// Stub for cpw.mods.fml.common.Mod — Minecraft Forge 1.7.10
// In 1.7.10 FML lived under cpw.mods.fml, not net.minecraftforge.fml.

using SpectraEngine.Core.Mods;
using cpw.mods.fml.common.registry;

namespace cpw.mods.fml.common
{
    /// <summary>
    /// MinecraftStubs v1_7_10 — @Mod annotation.
    /// In 1.7.10, @Mod is in cpw.mods.fml.common (not net.minecraftforge.fml).
    /// Same pattern as 1.12: ModLoader detects [ModAttribute] by FQN via reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ModAttribute : Attribute
    {
        public string modid       { get; }
        public string name        { get; init; } = "";
        public string version     { get; init; } = "0.0.0";
        public string dependencies { get; init; } = "";

        public ModAttribute(string modid) => this.modid = modid;
    }

    /// <summary>Marks a method as a Forge lifecycle handler (1.7.10).</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class EventHandlerAttribute : Attribute { }

    /// <summary>
    /// Wraps a 1.7.10 Forge @Mod class in ISpectraMod.
    /// Same pattern as ForgeMod1_12Wrapper — differs only in event types.
    /// </summary>
    public sealed class ForgeMod1_7Wrapper(object modInstance) : ISpectraMod
    {
        readonly System.Reflection.MethodInfo[] _preInit  =
            FindHandlers(modInstance, typeof(cpw.mods.fml.common.@event.FMLPreInitializationEvent));
        readonly System.Reflection.MethodInfo[] _init     =
            FindHandlers(modInstance, typeof(cpw.mods.fml.common.@event.FMLInitializationEvent));
        readonly System.Reflection.MethodInfo[] _postInit =
            FindHandlers(modInstance, typeof(cpw.mods.fml.common.@event.FMLPostInitializationEvent));

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
            GameRegistry.Engine = engine;

            var pre  = new cpw.mods.fml.common.@event.FMLPreInitializationEvent();
            var init = new cpw.mods.fml.common.@event.FMLInitializationEvent();
            var post = new cpw.mods.fml.common.@event.FMLPostInitializationEvent();

            foreach (var m in _preInit)  Invoke(m, pre);
            foreach (var m in _init)     Invoke(m, init);
            foreach (var m in _postInit) Invoke(m, post);

            GameRegistry.Engine = null;
        }

        public void OnUnload() { }

        void Invoke(System.Reflection.MethodInfo m, object arg)
        {
            try { m.Invoke(modInstance, [arg]); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ForgeMod1_7] {ModId}.{m.Name} threw: " +
                    $"{ex.InnerException?.Message ?? ex.Message}");
            }
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

// ── FML lifecycle events ──────────────────────────────────────────────────────

namespace cpw.mods.fml.common.@event
{
    public sealed class FMLPreInitializationEvent  { }
    public sealed class FMLInitializationEvent     { }
    public sealed class FMLPostInitializationEvent { }
    public sealed class FMLServerStartingEvent     { }
}
