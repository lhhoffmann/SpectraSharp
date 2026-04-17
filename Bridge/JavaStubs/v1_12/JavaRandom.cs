// Stub for java.util.Random as used in Minecraft 1.12 block/entity callbacks.

namespace net.minecraft.util;

/// <summary>
/// Thin wrapper around System.Random that exposes the java.util.Random API
/// expected by Minecraft 1.12 block/world callbacks (randomTick, quantityDropped, etc.).
/// </summary>
public sealed class JavaRandom(int seed)
{
    readonly Random _rng = new(seed);

    public JavaRandom() : this(Environment.TickCount) { }

    public int    nextInt()             => _rng.Next();
    public int    nextInt(int bound)    => _rng.Next(bound);
    public long   nextLong()            => (long)_rng.Next() << 32 | (uint)_rng.Next();
    public float  nextFloat()           => (float)_rng.NextDouble();
    public double nextDouble()          => _rng.NextDouble();
    public bool   nextBoolean()         => _rng.Next(2) == 0;
    public void   setSeed(long seed)    { /* System.Random has no re-seed — ignored */ }
}
