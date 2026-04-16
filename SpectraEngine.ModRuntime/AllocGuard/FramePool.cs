namespace SpectraEngine.ModRuntime.AllocGuard;

/// <summary>
/// Thread-local object pool reset at every tick boundary (Engine.FixedUpdate end).
/// Eliminates GC pressure from frequently-allocated mod objects (ItemStack, events).
///
/// Usage in stubs:
///   var stack = FramePool.RentItemStack(itemId, count);
///   // use stack ...
///   // return-to-pool happens automatically at EndFrame()
///
/// Lifecycle:
///   Engine.FixedUpdate() calls FramePool.EndFrame() at the end of each tick.
///   All rented objects from this tick are considered invalid after EndFrame().
///   Mods must NOT store rented objects across tick boundaries (they don't — Java code
///   recalculates per-tick, and holding cross-tick refs is a mod bug regardless).
/// </summary>
public static class FramePool
{
    // ── ItemStack pool ────────────────────────────────────────────────────────

    const int ItemStackPoolSize = 512;

    [ThreadStatic] static int _itemStackCursor;
    [ThreadStatic] static PooledItemStack[]? _itemStacks;

    public static PooledItemStack RentItemStack(int itemId, int count)
    {
        _itemStacks ??= new PooledItemStack[ItemStackPoolSize];

        if (_itemStackCursor >= ItemStackPoolSize)
        {
            // Pool exhausted — fall through to normal allocation.
            // AllocationMonitor will log this in DEBUG builds.
            AllocationMonitor.Track(typeof(PooledItemStack));
            return new PooledItemStack { ItemId = itemId, Count = count };
        }

        _itemStacks[_itemStackCursor] ??= new PooledItemStack();
        var obj = _itemStacks[_itemStackCursor++];
        obj.ItemId = itemId;
        obj.Count  = count;
        obj.Damage = 0;
        return obj;
    }

    // ── Generic event pool ────────────────────────────────────────────────────

    const int EventPoolSize = 64;

    [ThreadStatic] static int _eventCursor;
    [ThreadStatic] static PooledEvent?[]? _events;

    public static T RentEvent<T>() where T : PooledEvent, new()
    {
        _events ??= new PooledEvent[EventPoolSize];

        if (_eventCursor >= EventPoolSize)
        {
            AllocationMonitor.Track(typeof(T));
            return new T();
        }

        // Find a slot that holds a T or is null
        for (int i = _eventCursor; i < EventPoolSize; i++)
        {
            if (_events[i] is T existing)
            {
                _eventCursor = i + 1;
                existing.Reset();
                return existing;
            }
            if (_events[i] == null)
            {
                var fresh = new T();
                _events[i] = fresh;
                _eventCursor = i + 1;
                return fresh;
            }
        }

        AllocationMonitor.Track(typeof(T));
        return new T();
    }

    // ── Frame boundary ────────────────────────────────────────────────────────

    /// <summary>
    /// Call at the end of every Engine.FixedUpdate().
    /// Resets pool cursors — all rented objects from this tick are now invalid.
    /// </summary>
    public static void EndFrame()
    {
        _itemStackCursor = 0;
        _eventCursor     = 0;
    }
}

// ── Poolable types ────────────────────────────────────────────────────────────

/// <summary>Pooled ItemStack. Allocated once per pool slot, reused every tick.</summary>
public sealed class PooledItemStack
{
    public int ItemId { get; set; }
    public int Count  { get; set; }
    public int Damage { get; set; }
}

/// <summary>Base class for all poolable event objects.</summary>
public abstract class PooledEvent
{
    public bool Cancelled { get; set; }

    /// <summary>Reset mutable state before reuse. Override in each event type.</summary>
    public virtual void Reset() => Cancelled = false;
}
