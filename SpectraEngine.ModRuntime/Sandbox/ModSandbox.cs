using HarmonyLib;

namespace SpectraEngine.ModRuntime.Sandbox;

/// <summary>
/// Wraps every mod call with:
///   - Exception isolation (mod crash does not crash engine)
///   - OOM / StackOverflow isolation
///   - 500ms watchdog timeout
///
/// When a mod is killed its Harmony patches are reverted and it is marked dead.
/// The engine continues regardless.
/// </summary>
public sealed class ModSandbox
{
    const int WatchdogMs = 500;

    readonly string       _modId;
    readonly Harmony      _harmony;
    readonly ModWatchdog  _watchdog;

    public bool  IsAlive  { get; private set; } = true;
    public string ModId   => _modId;

    public ModSandbox(string modId)
    {
        _modId    = modId;
        _harmony  = new Harmony($"SpectraEngine.mod.{modId}");
        _watchdog = new ModWatchdog(modId, WatchdogMs, () => KillMod("tick timeout"));
    }

    /// <summary>
    /// Executes <paramref name="modCode"/> inside the sandbox.
    /// Returns false if the mod was killed or disabled.
    /// </summary>
    public bool Execute(Action modCode)
    {
        if (!IsAlive) return false;

        _watchdog.Start();
        try
        {
            modCode();
            return true;
        }
        catch (OutOfMemoryException)
        {
            KillMod("out of memory");
            return false;
        }
        catch (StackOverflowException)
        {
            KillMod("stack overflow — likely infinite recursion in mod");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"[ModSandbox] '{_modId}' threw {ex.GetType().Name}: {ex.Message}");
            Disable();
            return false;
        }
        finally
        {
            _watchdog.Stop();
        }
    }

    /// <summary>
    /// Hard kill: reverts all Harmony patches and marks mod dead permanently.
    /// Called for timeout, OOM, StackOverflow.
    /// </summary>
    public void KillMod(string reason)
    {
        if (!IsAlive) return;
        Console.Error.WriteLine($"[ModSandbox] KILLED '{_modId}' — {reason}");
        RevertPatches();
        IsAlive = false;
    }

    /// <summary>
    /// Soft disable: mod stops executing but patches already applied remain.
    /// Called for normal unhandled exceptions.
    /// </summary>
    public void Disable()
    {
        if (!IsAlive) return;
        Console.Error.WriteLine($"[ModSandbox] Disabled '{_modId}' due to unhandled exception.");
        IsAlive = false;
    }

    /// <summary>Reverts all Harmony patches applied by this mod.</summary>
    public void RevertPatches() => _harmony.UnpatchAll(_harmony.Id);

    /// <summary>Exposes Harmony instance for MixinInterceptor to apply patches.</summary>
    public Harmony Harmony => _harmony;
}
