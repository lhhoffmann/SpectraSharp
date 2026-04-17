// Stub for java.util.Random (exposed as JavaRandom) — Minecraft 1.16.5
// Same API as v1_12 version; lives in net.minecraft.util for stub consistency.

namespace net.minecraft.util;

/// <summary>
/// MinecraftStubs v1_16 — JavaRandom.
/// Wraps System.Random with java.util.Random surface API.
/// </summary>
public sealed class JavaRandom
{
    readonly System.Random _rng;

    public JavaRandom()                    { _rng = new System.Random(); }
    public JavaRandom(long seed)           { _rng = new System.Random((int)(seed ^ (seed >> 32))); }

    public int     nextInt()              => _rng.Next();
    public int     nextInt(int bound)     => _rng.Next(0, bound);
    public long    nextLong()             => ((long)_rng.Next() << 32) | (uint)_rng.Next();
    public float   nextFloat()            => (float)_rng.NextDouble();
    public double  nextDouble()           => _rng.NextDouble();
    public bool    nextBoolean()          => _rng.Next(2) == 0;
    public void    setSeed(long seed)     { /* System.Random has no reseed; no-op */ }
}
