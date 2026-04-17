// java.util.Random wrapper for Minecraft 1.7.10 callbacks.

namespace net.minecraft.src;

/// <summary>Thin wrapper around System.Random exposing java.util.Random API.</summary>
public sealed class JavaRandom(int seed)
{
    readonly Random _rng = new(seed);
    public JavaRandom() : this(Environment.TickCount) { }

    public int    nextInt()          => _rng.Next();
    public int    nextInt(int bound) => _rng.Next(bound);
    public float  nextFloat()        => (float)_rng.NextDouble();
    public double nextDouble()       => _rng.NextDouble();
    public bool   nextBoolean()      => _rng.Next(2) == 0;
    public long   nextLong()         => (long)_rng.Next() << 32 | (uint)_rng.Next();
}
