// Bridges SpectraEngine.Core.JavaRandom ↔ java.util.Random (IKVM)

using SpectraEngine.Core;

namespace net.minecraft.src;

/// <summary>
/// Wraps a SpectraEngine JavaRandom so mod code can call it as java.util.Random.
/// Created once per World stub; mod code receives it via world.rand.
/// </summary>
internal static class JavaRandomAdapter
{
    public static java.util.Random Wrap(JavaRandom rng) => new JavaRandomWrapper(rng);
}

/// <summary>
/// java.util.Random subclass that delegates to SpectraEngine.Core.JavaRandom.
/// IKVM sees this as a real java.util.Random — no mod code is aware of the wrapper.
/// </summary>
internal sealed class JavaRandomWrapper(JavaRandom _rng) : java.util.Random
{
    public override int nextInt(int bound) => _rng.NextInt(bound);
    public override int nextInt()          => _rng.NextInt();
    public override double nextDouble()    => _rng.NextDouble();
    public override float nextFloat()      => _rng.NextFloat();
    public override long nextLong()        => _rng.NextLong();
    public override bool nextBoolean()     => _rng.NextBoolean();
}
