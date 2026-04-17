// Stub for net.minecraftforge.common.MinecraftForge — Forge 1.12 event bus

using System.Reflection;
using net.minecraft.util;

namespace net.minecraftforge.common;

/// <summary>
/// MinecraftStubs v1_12 — MinecraftForge.
///
/// Contains the static event bus instance.
/// Mods call:
///   MinecraftForge.EVENT_BUS.register(this)         — subscribe instance methods
///   MinecraftForge.EVENT_BUS.post(new SomeEvent())  — fire an event
/// </summary>
public static class MinecraftForge
{
    public static readonly ForgeEventBus EVENT_BUS      = new();
    public static readonly ForgeEventBus TERRAIN_GEN_BUS = new();
    public static readonly ForgeEventBus ORE_GEN_BUS     = new();
}

// ── ForgeEventBus ─────────────────────────────────────────────────────────────

/// <summary>
/// Forge event bus — dispatches events to @SubscribeEvent-annotated methods.
///
/// Supports both instance handlers (register(this)) and static handlers
/// (register(StaticSubscriberProxy)) from @Mod.EventBusSubscriber classes.
///
/// Dispatch is reflection-based. Methods are indexed by their event parameter type
/// at registration time so post() does a single dictionary lookup per call.
/// </summary>
public sealed class ForgeEventBus
{
    // eventType → ordered list of subscriptions
    readonly Dictionary<Type, List<Subscription>> _handlers = new();

    const BindingFlags AllMethods =
        BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Registers event handlers from <paramref name="handler"/>.
    /// For instance handlers: scans instance + static methods on the object's type.
    /// For <see cref="net.minecraftforge.fml.common.ForgeMod1_12Wrapper.StaticSubscriberProxy"/>:
    /// scans only static methods on the proxy's target type.
    /// </summary>
    public void register(object handler)
    {
        Type? staticType = null;

        // StaticSubscriberProxy — scan static methods only on its SubscriberType
        var proxyProp = handler.GetType().GetProperty("SubscriberType",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (proxyProp?.GetValue(handler) is Type t)
            staticType = t;

        Type scanType   = staticType ?? handler.GetType();
        object? instance = staticType != null ? null : handler;

        int count = 0;
        foreach (var method in scanType.GetMethods(AllMethods))
        {
            var subscribeAttr = method.GetCustomAttribute<SubscribeEventAttribute>();
            if (subscribeAttr == null) continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1) continue;
            if (!typeof(ForgeEvent).IsAssignableFrom(parameters[0].ParameterType)) continue;

            var eventType = parameters[0].ParameterType;
            if (!_handlers.TryGetValue(eventType, out var list))
                _handlers[eventType] = list = [];

            list.Add(new Subscription(method, instance, subscribeAttr.priority,
                                      subscribeAttr.receiveCanceled));
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            count++;
        }

        if (count > 0)
            Console.WriteLine(
                $"[ForgeEventBus] Registered {count} @SubscribeEvent handler(s)" +
                $" from {scanType.Name}");
        else
            Console.WriteLine(
                $"[ForgeEventBus] Registered handler: {scanType.Name} (no @SubscribeEvent methods found)");
    }

    /// <summary>Unregisters all handlers from the given object.</summary>
    public void unregister(object handler)
    {
        foreach (var list in _handlers.Values)
            list.RemoveAll(s => ReferenceEquals(s.Instance, handler));
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Posts <paramref name="evt"/> to all registered handlers whose event parameter
    /// type is assignable from the event's type.
    /// Returns true if the event was cancelled.
    /// </summary>
    public bool post(ForgeEvent evt)
    {
        var evtType = evt.GetType();

        foreach (var (handlerType, list) in _handlers)
        {
            if (!handlerType.IsAssignableFrom(evtType)) continue;

            foreach (var sub in list)
            {
                if (evt.isCanceled() && !sub.ReceiveCanceled) continue;
                try
                {
                    sub.Method.Invoke(sub.Instance, [evt]);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"[ForgeEventBus] Handler {sub.Method.DeclaringType?.Name}.{sub.Method.Name}" +
                        $" threw: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        return evt.isCanceled();
    }

    // ── Subscription record ───────────────────────────────────────────────────

    sealed record Subscription(
        MethodInfo   Method,
        object?      Instance,
        EventPriority Priority,
        bool         ReceiveCanceled);
}

// ── ForgeEvent ────────────────────────────────────────────────────────────────

/// <summary>Base class for all Forge events.</summary>
public abstract class ForgeEvent
{
    public bool isCanceled()               => _cancelled;
    public void setCanceled(bool value)    => _cancelled = value;
    private bool _cancelled;
}

// ForgeRegistries → ForgeRegistries.cs
