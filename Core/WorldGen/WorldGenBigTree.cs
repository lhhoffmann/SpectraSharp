namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Fancy branching oak tree generator. Spec: <c>yd</c> (WorldGenBigTree).
/// Selected 10% of the time by most biomes. Height: 5–16 blocks.
/// Uses DDA line-tracing for trunk and branches, ellipsoidal leaf clusters.
/// </summary>
public sealed class WorldGenBigTree : WorldGenerator
{
    private const int LogId    = 17; // oak log,    meta 0
    private const int LeavesId = 18; // oak leaves, meta 0

    // Fields (spec §4.1)
    private double _trunkSplitFraction  = 0.618; // g — trunk split at ~62% of height
    // i = 0.381 (branch start fraction) — stored in Java but algorithm uses hardcoded 0.3
    private double _branchDistMult      = 1.0;   // j — branch horizontal distance multiplier
    private double _sizeMult            = 1.0;   // k — branch/leaf size multiplier
    private int    _heightRange         = 12;    // m — nextInt(m) + 5 = total height
    private int    _leafClusterHeight   = 4;     // n — vertical extent of each leaf cluster

    // Per-generate state
    private IWorld     _world = null!;
    private JavaRandom _rand  = null!;
    private int _bx, _by, _bz, _treeHeight, _trunkTop;
    private readonly List<(int x, int y, int z)> _branches = [];

    public override void SetScale(double scaleX, double scaleY, double scaleZ)
    {
        _heightRange       = (int)(scaleX * 12.0);
        if (scaleX > 0.5) _leafClusterHeight = 5;
        _branchDistMult    = scaleY;
        _sizeMult          = scaleZ;
    }

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        _world = world; _rand = rand;
        _bx = x; _by = y; _bz = z;

        _treeHeight = 5 + rand.NextInt(_heightRange); // [5, 16] with default m=12
        _trunkTop   = (int)(_treeHeight * _trunkSplitFraction);

        // Ground + Y bounds check
        int below = world.GetBlockId(x, y - 1, z);
        if (below != 2 && below != 3) return false;
        if (y + _treeHeight + _leafClusterHeight >= World.WorldHeight) return false;

        // Clearance: centre column from base to top must be air or leaves
        for (int dy = 0; dy <= _treeHeight; dy++)
        {
            int id = world.GetBlockId(x, y + dy, z);
            if (id != 0 && id != LeavesId) return false;
        }

        // Convert ground to dirt (spec: world.d() — silent)
        world.SetBlockSilent(x, y - 1, z, 3);

        // Compute branch endpoints
        ComputeBranches();

        // Place leaf clusters at each branch endpoint
        foreach (var (ex, ey, ez) in _branches)
            PlaceLeafCluster(ex, ey, ez);

        // Place trunk (base → trunkTop) via DDA
        DrawLine(x, y, z, x, y + _trunkTop, z, LogId, 0);

        // Place branch trunks via DDA (skip first entry = trunk top)
        int baseAttach = y + _trunkTop;
        foreach (var (ex, ey, ez) in _branches)
        {
            if (ex == x && ez == z) continue; // trunk top — no branch trunk
            DrawLine(x, baseAttach, z, ex, ey, ez, LogId, 0);
        }

        return true;
    }

    // ── Branch endpoint computation (spec §4.2) ───────────────────────────────

    private void ComputeBranches()
    {
        _branches.Clear();

        // Trunk top is always a branch endpoint
        _branches.Add((_bx, _by + _treeHeight, _bz));

        double angle = _rand.NextDouble() * Math.PI * 2.0;
        int loopBottom = (int)(_treeHeight * 0.3);

        for (int dy = _trunkTop; dy >= loopBottom; dy--)
        {
            double heightFrac  = (double)dy / _treeHeight;
            double clusterSize = (0.5 + _sizeMult) * (1.0 - heightFrac);
            int    branchCount = (int)(1.382 + Math.Pow(_sizeMult * _treeHeight / 13.0, 2));
            if (branchCount < 1) branchCount = 1;

            for (int b = 0; b < branchCount; b++)
            {
                // Golden ratio angle increment (137.508°)
                angle += 2.399963; // radians ≈ 137.508°

                double dist = clusterSize * _branchDistMult;
                int    ex   = _bx + (int)(Math.Cos(angle) * dist);
                int    ey   = _by + dy;
                int    ez   = _bz + (int)(Math.Sin(angle) * dist);

                // Accept only if endpoint is air or leaves
                int id = _world.GetBlockId(ex, ey, ez);
                if (id == 0 || id == LeavesId)
                    _branches.Add((ex, ey, ez));
            }
        }
    }

    // ── Leaf cluster (spec §4.2 step 4) ──────────────────────────────────────

    private void PlaceLeafCluster(int cx, int cy, int cz)
    {
        // Radii per level: n=4 → [2,3,3,2]; n=5 → [2,3,3,3,2]
        Span<int> radii = _leafClusterHeight == 5
            ? stackalloc int[] { 2, 3, 3, 3, 2 }
            : stackalloc int[] { 2, 3, 3, 2 };

        for (int ly = 0; ly < _leafClusterHeight; ly++)
        {
            int r = radii[ly];
            int wy = cy + ly;
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (Math.Abs(dx) == r && Math.Abs(dz) == r) continue; // skip corners
                int bx = cx + dx, bz = cz + dz;
                if (!Block.IsOpaqueCubeArr[_world.GetBlockId(bx, wy, bz)])
                    _world.SetBlockSilent(bx, wy, bz, LeavesId); // meta=0, silent per spec
            }
        }
    }

    // ── DDA line block placement (spec §4.2 steps 5/6) ───────────────────────

    private void DrawLine(int x0, int y0, int z0, int x1, int y1, int z1, int blockId, int meta)
    {
        int dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
        int steps = Math.Max(Math.Max(Math.Abs(dx), Math.Abs(dy)), Math.Abs(dz));
        if (steps == 0)
        {
            int id = _world.GetBlockId(x0, y0, z0);
            if (id == 0 || id == LeavesId) _world.SetBlockSilent(x0, y0, z0, blockId); // meta always 0
            return;
        }

        double sx = dx / (double)steps;
        double sy = dy / (double)steps;
        double sz = dz / (double)steps;

        for (int i = 0; i <= steps; i++)
        {
            int bx = x0 + (int)(sx * i + 0.5);
            int by = y0 + (int)(sy * i + 0.5);
            int bz = z0 + (int)(sz * i + 0.5);
            int id = _world.GetBlockId(bx, by, bz);
            if (id == 0 || id == LeavesId)
                _world.SetBlockSilent(bx, by, bz, blockId); // meta always 0
        }
    }
}
