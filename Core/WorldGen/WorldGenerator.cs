namespace SpectraEngine.Core.WorldGen;

/// <summary>
/// Abstract base for all world feature generators. Spec: <c>ig</c> (WorldGenerator).
/// </summary>
public abstract class WorldGenerator
{
    /// <summary>
    /// Scaling hint — only used by <see cref="WorldGenBigTree"/>. Spec: <c>a(double, double, double)</c>.
    /// </summary>
    public virtual void SetScale(double scaleX, double scaleY, double scaleZ) { }

    /// <summary>
    /// Generate the feature centred at (x, y, z). Returns true if successful.
    /// Spec: <c>a(ry world, Random rand, int x, int y, int z)</c> → bool.
    /// </summary>
    public abstract bool Generate(IWorld world, JavaRandom rand, int x, int y, int z);
}
