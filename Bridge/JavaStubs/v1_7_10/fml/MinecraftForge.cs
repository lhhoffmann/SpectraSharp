// Stub for net.minecraftforge.common.MinecraftForge — Forge 1.7.10
// Same package as 1.12: net.minecraftforge.common

using System.Reflection;

namespace net.minecraftforge.common;

/// <summary>
/// MinecraftStubs v1_7_10 — MinecraftForge.
/// Provides EVENT_BUS — the global Forge event bus.
/// 1.7.10 mods register handlers via MinecraftForge.EVENT_BUS.register(this).
/// </summary>
public static class MinecraftForge
{
    public static readonly ForgeEventBus EVENT_BUS = new();
}

// ── Event bus ────────────────────────────────────────────────────────────────

/// <summary>
/// MinecraftStubs v1_7_10 — ForgeEventBus.
/// Dispatch is reflection-based: @ForgeSubscribe-annotated methods keyed by event type.
/// In 1.7.10 the annotation was @ForgeSubscribe (renamed to @SubscribeEvent in 1.8).
/// </summary>
public sealed class ForgeEventBus
{
    sealed record Subscription(MethodInfo Method, object? Instance, int Priority, bool ReceiveCanceled);

    readonly Dictionary<Type, List<Subscription>> _handlers = new();
    readonly object _lock = new();

    /// <summary>Register an event handler instance (or static subscriber via StaticSubscriberProxy).</summary>
    public void register(object handler)
    {
        if (handler is StaticSubscriberProxy proxy)
        {
            RegisterMethods(proxy.SubscriberType, null);
            return;
        }
        RegisterMethods(handler.GetType(), handler);
    }

    void RegisterMethods(Type type, object? instance)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                                 | BindingFlags.Instance | BindingFlags.Static;
        foreach (var m in type.GetMethods(flags))
        {
            // 1.7.10 uses @ForgeSubscribe
            var attr = (ForgeSubscribeAttribute?)m.GetCustomAttribute(typeof(ForgeSubscribeAttribute));
            if (attr == null) continue;
            var ps = m.GetParameters();
            if (ps.Length != 1) continue;
            var evtType = ps[0].ParameterType;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(evtType, out var list))
                    _handlers[evtType] = list = new();
                list.Add(new(m, instance, attr.priority, attr.receiveCanceled));
                list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }
    }

    public void unregister(object handler)
    {
        var target = handler is StaticSubscriberProxy p ? null : handler;
        var type   = handler is StaticSubscriberProxy p2 ? p2.SubscriberType : handler.GetType();
        lock (_lock)
        {
            foreach (var list in _handlers.Values)
                list.RemoveAll(s => s.Instance == target
                    && s.Method.DeclaringType == type);
        }
    }

    public bool post(ForgeEvent evt)
    {
        List<Subscription>? subs;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(evt.GetType(), out subs))
                subs = FindAssignableSubs(evt.GetType());
        }
        if (subs == null) return evt.isCanceled();
        foreach (var s in subs)
        {
            if (evt.isCanceled() && !s.ReceiveCanceled) continue;
            try { s.Method.Invoke(s.Instance, [evt]); }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ForgeEventBus1.7] {s.Method.DeclaringType?.Name}.{s.Method.Name} threw: "
                    + (ex.InnerException?.Message ?? ex.Message));
            }
        }
        return evt.isCanceled();
    }

    List<Subscription>? FindAssignableSubs(Type evtType)
    {
        var result = new List<Subscription>();
        foreach (var (key, list) in _handlers)
            if (key.IsAssignableFrom(evtType))
                result.AddRange(list);
        if (result.Count == 0) return null;
        result.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        return result;
    }
}

/// <summary>
/// Passed to ForgeEventBus.register() for @Mod.EventBusSubscriber-style static subscribers.
/// </summary>
public sealed class StaticSubscriberProxy(Type subscriberType)
{
    public Type SubscriberType => subscriberType;
}

// ── Event base ───────────────────────────────────────────────────────────────

/// <summary>MinecraftStubs v1_7_10 — ForgeEvent base.</summary>
public abstract class ForgeEvent
{
    bool _canceled;
    public bool isCanceled()          => _canceled;
    public void setCanceled(bool b)   { _canceled = b; }
    public bool isCancelable()        => false;
}

// ── @ForgeSubscribe annotation (1.7.10 name) ─────────────────────────────────

/// <summary>
/// 1.7.10 used @ForgeSubscribe; renamed to @SubscribeEvent in 1.8.
/// Both attributes are supported so mods compiled against either version work.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ForgeSubscribeAttribute : Attribute
{
    public int  priority        { get; init; } = 2; // EventPriority.NORMAL
    public bool receiveCanceled { get; init; } = false;
}
