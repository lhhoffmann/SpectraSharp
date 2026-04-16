using SpectraEngine.Core.WorldGen.Structure;

namespace SpectraEngine.Core.WorldGen.NetherFortress;

/// <summary>
/// Nether Fortress structure generator. Replica of <c>ed</c> (MapGenNetherBridge).
///
/// Algorithm:
///   1. Per chunk column: 1/3 probability contains a fortress.
///   2. If yes, origin offset within chunk is [4, 11] in both X and Z.
///   3. Fortress expands recursively from a BridgeCrossing starting piece
///      until depth > 30 or radius > 112 from origin.
///   4. All pieces are generated constrained to Y = [48, 70].
///
/// Mob spawn list: Blaze (10), ZombiePigman (10), MagmaCube (3) — see §6.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/NetherFortress_Spec.md §4-§5
/// </summary>
public sealed class MapGenNetherBridge
{
    private readonly JavaRandom _structureRng = new(0);
    private long _worldSeed;

    // Cache of generated fortresses per 2×2 chunk region
    private readonly Dictionary<long, List<StructurePiece>> _structureCache = new();

    // ── Initialisation ────────────────────────────────────────────────────────

    public void SetWorldSeed(long seed) => _worldSeed = seed;

    // ── Populate hook — called from ChunkProviderHell.Populate ───────────────

    /// <summary>
    /// Generates fortress blocks into the world for the given chunk.
    /// Checks nearby chunk origins for fortresses that may overlap.
    /// </summary>
    public void Generate(World world, int chunkX, int chunkZ, JavaRandom rng)
    {
        // Check a 2×2 area of super-chunks around the requested chunk
        for (int sx = chunkX - 1; sx <= chunkX + 1; sx++)
        for (int sz = chunkZ - 1; sz <= chunkZ + 1; sz++)
        {
            long key = (long)sx << 32 | (uint)sz;
            if (!_structureCache.TryGetValue(key, out var pieces))
            {
                pieces = TryGenerateFortress(world, sx, sz);
                _structureCache[key] = pieces;
            }

            if (pieces.Count == 0) continue;

            // Generation bounds = current chunk column, Y [48, 70]
            var genBounds = new StructureBoundingBox(
                chunkX * 16, 48, chunkZ * 16,
                chunkX * 16 + 15, 70, chunkZ * 16 + 15);

            foreach (var piece in pieces)
                if (piece.BBox.Intersects(genBounds))
                    piece.Generate(world, rng, genBounds);
        }
    }

    // ── shouldGenerateHere (spec §4.1) ───────────────────────────────────────

    private bool ShouldGenerateHere(int chunkX, int chunkZ, out int expectedX, out int expectedZ)
    {
        // Seed formula (spec §4.1)
        long seed = ((long)(chunkX ^ (chunkZ << 4))) ^ _worldSeed;
        _structureRng.SetSeed(seed);

        _structureRng.NextInt(); // consume and discard (RNG advancement parity)

        if (_structureRng.NextInt(3) != 0) // 2/3 chance: no fortress
        {
            expectedX = expectedZ = 0;
            return false;
        }

        expectedX = (chunkX << 4) + 4 + _structureRng.NextInt(8);
        expectedZ = (chunkZ << 4) + 4 + _structureRng.NextInt(8);
        return true;
    }

    // ── Structure expansion (spec §5) ────────────────────────────────────────

    private List<StructurePiece> TryGenerateFortress(World world, int chunkX, int chunkZ)
    {
        if (!ShouldGenerateHere(chunkX, chunkZ, out int originX, out int originZ))
            return [];

        int orientation = _structureRng.NextInt(4);

        // Create starting piece (spec §5 — gc at (chunkX*16+2, 64, chunkZ*16+2))
        var startPiece = new StartingPiece(chunkX * 16 + 2, chunkZ * 16 + 2, orientation, 0);
        var pieces = new List<StructurePiece> { startPiece };

        // Seed the pending list from starting piece exits (spec §5 step 3)
        startPiece.AddExits(startPiece, pieces, _structureRng);

        // Expand pending list (spec §5 step 4)
        while (startPiece.Pending.Count > 0)
        {
            int idx = _structureRng.NextInt(startPiece.Pending.Count);
            StructurePiece next = startPiece.Pending[idx];
            startPiece.Pending.RemoveAt(idx);
            next.AddExits(startPiece, pieces, _structureRng);
        }

        return pieces;
    }

    // ── Mob spawn list (spec §6) ──────────────────────────────────────────────

    /// <summary>
    /// Returns spawn list entries for fortress-region mob spawning.
    /// Used by ChunkProviderHell to check if a chunk is inside a fortress.
    /// </summary>
    public bool IsInsideFortress(int blockX, int blockZ)
    {
        int cx = blockX >> 4;
        int cz = blockZ >> 4;
        for (int sx = cx - 1; sx <= cx + 1; sx++)
        for (int sz = cz - 1; sz <= cz + 1; sz++)
        {
            long key = (long)sx << 32 | (uint)sz;
            if (_structureCache.TryGetValue(key, out var pieces))
                foreach (var p in pieces)
                    if (p.BBox.Contains(blockX, 64, blockZ))
                        return true;
        }
        return false;
    }
}
