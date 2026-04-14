namespace SpectraSharp.ModRuntime.AllocGuard;

/// <summary>
/// Tracks allocations that escape the FramePool (DEBUG builds only).
/// Zero overhead in Release — all methods are no-ops compiled away.
///
/// Output example:
///   [AllocGuard] WARNING: PooledItemStack — 847 escapes/tick (pool exhausted)
///   [AllocGuard] WARNING: BlockBreakEvent — 12 escapes/tick
/// </summary>
public static class AllocationMonitor
{
#if DEBUG
    static readonly Dictionary<Type, int> _frameEscapes = new();
    static int _frameCount;
    const int ReportEveryFrames = 200; // report every 10s at 20Hz

    public static void Track(Type type)
    {
        lock (_frameEscapes)
            _frameEscapes[type] = _frameEscapes.GetValueOrDefault(type) + 1;
    }

    public static void EndFrame()
    {
        _frameCount++;
        if (_frameCount % ReportEveryFrames != 0) return;

        lock (_frameEscapes)
        {
            foreach (var (type, count) in _frameEscapes)
            {
                float perTick = (float)count / ReportEveryFrames;
                if (perTick > 1f)
                    Console.WriteLine(
                        $"[AllocGuard] WARNING: {type.Name} — {perTick:F1} escapes/tick");
            }
            _frameEscapes.Clear();
        }
    }
#else
    // Release: complete no-ops, JIT eliminates all call sites
    public static void Track(Type type) { }
    public static void EndFrame() { }
#endif
}
