namespace SpectraSharp.ModRuntime.Sandbox;

/// <summary>
/// Background thread that monitors a running mod call.
/// If the call exceeds <see cref="TimeoutMs"/> the watchdog fires <see cref="OnTimeout"/>.
/// Usage: wrap a single mod call with Start()/Stop().
/// Thread-safe. One instance per mod.
/// </summary>
sealed class ModWatchdog : IDisposable
{
    readonly string  _modId;
    readonly int     _timeoutMs;
    readonly Action  _onTimeout;

    Timer?    _timer;
    bool      _disposed;

    public ModWatchdog(string modId, int timeoutMs, Action onTimeout)
    {
        _modId     = modId;
        _timeoutMs = timeoutMs;
        _onTimeout = onTimeout;
    }

    /// <summary>Arms the watchdog. Call before executing mod code.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer = new Timer(_ =>
        {
            Console.Error.WriteLine(
                $"[ModWatchdog] '{_modId}' exceeded {_timeoutMs}ms — killing mod.");
            _onTimeout();
        }, null, _timeoutMs, Timeout.Infinite);
    }

    /// <summary>Disarms the watchdog. Call after mod code returns normally.</summary>
    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }
}
