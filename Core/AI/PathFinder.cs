namespace SpectraSharp.Core.AI;

/// <summary>
/// Replica of <c>rw</c> (PathFinder) — A* pathfinder used by all AI mobs.
///
/// Each instance is created fresh per path request (see World.GetPathToEntity /
/// World.GetPathToCoords) around a <see cref="ChunkCache"/> snapshot.
///
/// Algorithm overview (spec §4):
///   1. Build start, target and size nodes from entity AABB and target position.
///   2. A* main loop: poll min-f node, expand 4-directional neighbours.
///   3. Return reconstructed path, or best partial path if target unreachable.
///
/// Walkability codes (spec §4 checkWalkability):
///    1 = passable (clear)
///    0 = blocked (solid / closed door)
///   -1 = water (passable but avoided — not blocked in 1.0 base AI)
///   -2 = danger (lava — neighbour rejected in tryNeighbor)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §4
/// </summary>
public sealed class PathFinder
{
    // ── Block IDs for door check (spec §4 constants) ──────────────────────────

    private const int WoodDoorId = 64;
    private const int IronDoorId = 71;

    // ── Fields ────────────────────────────────────────────────────────────────

    /// <summary>obf: a — world snapshot (ChunkCache).</summary>
    private readonly ChunkCache _world;

    /// <summary>obf: b — open set heap (reused and cleared each search).</summary>
    private readonly PathHeap _heap = new();

    /// <summary>obf: c — node cache: HashKey → PathPoint (deduplication + closed set).</summary>
    private readonly Dictionary<int, PathPoint> _nodeCache = new();

    /// <summary>obf: d — scratch array for expanded neighbours (max 4 per expansion).</summary>
    private readonly PathPoint?[] _scratch = new PathPoint[32];

    // ── Constructor ───────────────────────────────────────────────────────────

    public PathFinder(ChunkCache world) => _world = world;

    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>rw.a(ia entity, ia target, float range)</c> — find path to a target entity.
    /// Builds start/target/size nodes from entity and target positions, delegates to A* loop.
    /// </summary>
    public PathEntity? FindPath(Entity entity, Entity target, float range)
        => FindPath(entity, target.PosX, target.PosY, target.PosZ, range);

    /// <summary>
    /// obf: <c>rw.a(ia entity, double tx, double ty, double tz, float range)</c> — core A*.
    /// Constructs start/target/size nodes and runs the main search loop.
    /// </summary>
    public PathEntity? FindPath(Entity entity, double targetX, double targetY, double targetZ,
                                float range)
    {
        // Start node: entity AABB minimum corner
        var start = GetOrCreate(
            (int)Math.Floor(entity.BoundingBox.MinX),
            (int)Math.Floor(entity.BoundingBox.MinY),
            (int)Math.Floor(entity.BoundingBox.MinZ));

        // Target node: centred on target position
        var targetNode = GetOrCreate(
            (int)Math.Floor(targetX - entity.Width / 2.0f),
            (int)Math.Floor(targetY),
            (int)Math.Floor(targetZ - entity.Width / 2.0f));

        // Size node: entity footprint for collision testing (not a graph node)
        var sizeNode = new PathPoint(
            (int)Math.Ceiling(entity.Width  + 1.0f),
            (int)Math.Ceiling(entity.Height + 1.0f),
            (int)Math.Ceiling(entity.Width  + 1.0f));

        return RunAStar(entity, start, targetNode, sizeNode, range);
    }

    // ── A* main loop ──────────────────────────────────────────────────────────

    private PathEntity? RunAStar(Entity entity, PathPoint start, PathPoint target,
                                  PathPoint sizeNode, float range)
    {
        start.PathCost     = 0f;
        start.HeuristicCost = start.EuclideanDistanceTo(target);
        start.TotalCost    = start.HeuristicCost;

        _heap.Clear();
        _nodeCache.Clear();
        _heap.Add(start);
        _nodeCache[start.HashKey] = start;

        PathPoint closest = start;

        while (!_heap.IsEmpty)
        {
            PathPoint current = _heap.Poll();

            // Goal reached
            if (current == target)
                return ReconstructPath(start, target);

            // Track closest node to target
            if (current.EuclideanDistanceTo(target) < closest.EuclideanDistanceTo(target))
                closest = current;

            current.Closed = true;

            int count = ExpandNeighbors(entity, current, sizeNode, target, range);

            for (int i = 0; i < count; i++)
            {
                PathPoint? nb = _scratch[i];
                if (nb == null) continue;
                if (nb.Closed) continue;
                if (nb.EuclideanDistanceTo(target) >= range) continue;

                float tentativeG = current.PathCost + current.EuclideanDistanceTo(nb);

                if (!nb.IsInHeap() || tentativeG < nb.PathCost)
                {
                    nb.Parent       = current;
                    nb.PathCost     = tentativeG;
                    nb.HeuristicCost = nb.EuclideanDistanceTo(target);

                    if (nb.IsInHeap())
                    {
                        _heap.Update(nb, tentativeG + nb.HeuristicCost);
                    }
                    else
                    {
                        nb.TotalCost = tentativeG + nb.HeuristicCost;
                        _heap.Add(nb);
                    }
                }
            }
        }

        // No direct path — return partial path or null
        if (closest == start) return null;
        return ReconstructPath(start, closest);
    }

    // ── Neighbour expansion ───────────────────────────────────────────────────

    private int ExpandNeighbors(Entity entity, PathPoint current, PathPoint sizeNode,
                                 PathPoint target, float range)
    {
        // Check for step-up opportunity above current position
        int climbOffset = CheckWalkability(current.X, current.Y + 1, current.Z, sizeNode) == 1
            ? 1 : 0;

        int count = 0;

        // 4-directional expansion (N/S/E/W)
        Span<(int dx, int dz)> dirs = stackalloc (int, int)[] { (0,1), (-1,0), (1,0), (0,-1) };
        foreach (var (dx, dz) in dirs)
        {
            PathPoint? nb = TryNeighbor(current.X + dx, current.Y, current.Z + dz,
                                         sizeNode, climbOffset);
            if (nb != null && !nb.Closed && nb.EuclideanDistanceTo(target) < range)
                _scratch[count++] = nb;
        }

        return count;
    }

    // ── tryNeighbor ───────────────────────────────────────────────────────────

    private PathPoint? TryNeighbor(int x, int y, int z, PathPoint sizeNode, int climbOffset)
    {
        PathPoint? node = null;

        if (CheckWalkability(x, y, z, sizeNode) == 1)
            node = GetOrCreate(x, y, z);

        if (node == null && climbOffset > 0
            && CheckWalkability(x, y + climbOffset, z, sizeNode) == 1)
        {
            node = GetOrCreate(x, y + climbOffset, z);
            y   += climbOffset;
        }

        if (node != null)
        {
            // Step-down: find solid floor
            int stepDown = 0;
            int floorResult;

            while (y > 0 && (floorResult = CheckWalkability(x, y - 1, z, sizeNode)) == 1)
            {
                stepDown++;
                if (stepDown >= 4) return null; // too far to fall
                y--;
                node = GetOrCreate(x, y, z);
            }

            // If the floor is lava, reject
            if (y > 0)
            {
                floorResult = CheckWalkability(x, y - 1, z, sizeNode);
                if (floorResult == -2) return null;
            }
        }

        return node;
    }

    // ── checkWalkability ─────────────────────────────────────────────────────

    /// <summary>
    /// Scans the entity's bounding box at (x, y, z) for obstructions.
    /// Returns: 1=clear, 0=blocked, -1=water, -2=lava.
    /// </summary>
    private int CheckWalkability(int x, int y, int z, PathPoint sizeNode)
    {
        for (int bx = x; bx < x + sizeNode.X; bx++)
        for (int by = y; by < y + sizeNode.Y; by++)
        for (int bz = z; bz < z + sizeNode.Z; bz++)
        {
            int blockId = _world.GetBlockId(bx, by, bz);
            if (blockId > 0)
            {
                // Door check: closed door blocks passage
                if (blockId == WoodDoorId || blockId == IronDoorId)
                {
                    int meta = _world.GetBlockMetadata(bx, by, bz);
                    bool isOpen = (meta & 4) != 0; // BlockDoor.isOpen(meta)
                    if (!isOpen) return 0;
                }
                else
                {
                    Material? mat = Block.BlocksList[blockId]?.BlockMaterial;
                    if (mat == null) continue;
                    if (mat.IsSolid()) return 0;
                    if (mat == Material.Water)  return -1;
                    if (mat == Material.Lava_) return -2;
                }
            }
        }
        return 1;
    }

    // ── Path reconstruction ───────────────────────────────────────────────────

    private static PathEntity ReconstructPath(PathPoint start, PathPoint end)
    {
        int length = 1;
        for (PathPoint? n = end; n?.Parent != null; n = n.Parent)
            length++;

        var nodes = new PathPoint[length];
        PathPoint? cur = end;
        for (int i = length - 1; i >= 0; i--)
        {
            nodes[i] = cur!;
            cur = cur!.Parent;
        }

        return new PathEntity(nodes);
    }

    // ── Node deduplication ────────────────────────────────────────────────────

    private PathPoint GetOrCreate(int x, int y, int z)
    {
        int key = PathPoint.ComputeHash(x, y, z);
        if (!_nodeCache.TryGetValue(key, out PathPoint? node))
        {
            node = new PathPoint(x, y, z);
            _nodeCache[key] = node;
        }
        return node;
    }
}
