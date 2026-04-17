// Minimal java.util.Random adapter for 1.0 mod blocks

namespace net.minecraft.src;

/// <summary>
/// MinecraftStubs v1_0 — JavaRandom.
/// Exposed to mod blocks as the random parameter in updateTick().
/// </summary>
public sealed class JavaRandom
{
    readonly System.Random _rng = new();
    public int    nextInt(int bound) => _rng.Next(0, bound);
    public int    nextInt()          => _rng.Next();
    public float  nextFloat()        => (float)_rng.NextDouble();
    public double nextDouble()       => _rng.NextDouble();
    public bool   nextBoolean()      => _rng.Next(2) == 0;
}
