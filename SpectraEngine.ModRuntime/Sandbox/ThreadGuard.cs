namespace SpectraEngine.ModRuntime.Sandbox;

/// <summary>
/// Detects and heals thread violations from mods.
/// Mods sometimes call world-mutating methods from render or async threads.
/// Instead of crashing, ThreadGuard marshals the call to the next engine tick.
/// </summary>
public static class ThreadGuard
{
    static int _tickThreadId = -1;

    /// <summary>Call once from Engine.FixedUpdate() to register the tick thread.</summary>
    public static void RegisterTickThread() =>
        _tickThreadId = Environment.CurrentManagedThreadId;

    /// <summary>True when called from the registered tick thread.</summary>
    public static bool IsTickThread =>
        Environment.CurrentManagedThreadId == _tickThreadId;

    /// <summary>
    /// If on the wrong thread, schedules <paramref name="action"/> for the next tick
    /// and returns false. If on the correct thread, does nothing and returns true.
    /// </summary>
    public static bool EnsureTickThread(Action action, string callerName = "")
    {
        if (IsTickThread) return true;

        Console.WriteLine(
            $"[ThreadGuard] Wrong-thread call{(callerName.Length > 0 ? $" in {callerName}" : "")} " +
            $"— marshalling to next tick.");
        TickScheduler.ScheduleNextTick(action);
        return false;
    }
}

/// <summary>
/// Minimal cross-thread tick scheduler.
/// Engine calls <see cref="Flush"/> at the start of each FixedUpdate.
/// </summary>
public static class TickScheduler
{
    static readonly Queue<Action> _pending = new();
    static readonly object _lock = new();

    public static void ScheduleNextTick(Action action)
    {
        lock (_lock) _pending.Enqueue(action);
    }

    /// <summary>Called by Engine.FixedUpdate() before mod hooks run.</summary>
    public static void Flush()
    {
        lock (_lock)
        {
            while (_pending.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"[TickScheduler] Deferred action threw: {ex.Message}");
                }
            }
        }
    }
}
