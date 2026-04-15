using SpectraSharp.Core.WorldGen.Structure;

namespace SpectraSharp.Core.WorldGen.NetherFortress;

/// <summary>
/// Fallback terminator piece. Replica of <c>ld</c>.
///
/// Placed when: depth &gt; 30, radius &gt; 112, or all pieces at max count.
/// A 5×10×8 dead-end stub with random-length nether brick columns.
/// The random used for generation is seeded from a piece-level saved seed —
/// so the columns are deterministic regardless of world RNG state at generation time.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §3.5
/// </summary>
internal sealed class DeadEnd : FortressPiece
{
    private readonly int _savedSeed; // obf: this.a — captured at construction

    public DeadEnd(int ox, int oy, int oz, int orientation, int depth)
        : base(StructureBoundingBox.Create(ox, oy, oz, -1, -3, 0, 5, 10, 8, orientation), orientation, depth)
    {
        _savedSeed = (ox * 7 + oz * 13) ^ depth; // deterministic seed from position
    }

    public override void Generate(World world, JavaRandom rng, StructureBoundingBox bounds)
    {
        // Use a fresh random seeded from saved seed (spec §3.5)
        var localRng = new JavaRandom(_savedSeed);

        // Fill floor
        FillBox(world, bounds, 0, 3, 0, 4, 3, 7, NF.NetherBrick);
        // Outer walls
        FillBox(world, bounds, 0, 0, 0, 0, 9, 7, NF.NetherBrick);
        FillBox(world, bounds, 4, 0, 0, 4, 9, 7, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 0, 4, 9, 0, NF.NetherBrick);
        FillBox(world, bounds, 0, 0, 7, 4, 9, 7, NF.NetherBrick);
        // Interior clear
        ClearBox(world, bounds, 1, 4, 1, 3, 8, 6);

        // Random-length columns (spec §3.5 — uses localRng)
        for (int x = 0; x <= 4; x++)
        for (int z = 0; z <= 7; z += 7)
        {
            int height = localRng.NextInt(3);
            for (int y = 0; y <= height; y++)
                PlaceBlock(world, NF.NetherBrick, 0, x, 2 - y, z, bounds);
        }
    }
}
