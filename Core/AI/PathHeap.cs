namespace SpectraSharp.Core.AI;

/// <summary>
/// Replica of <c>zs</c> (PathHeap) — binary min-heap of <see cref="PathPoint"/> nodes,
/// sorted by <see cref="PathPoint.TotalCost"/> (f-cost). Used as the A* open set.
///
/// Heap invariants:
///   - Parent of index i:      (i - 1) >> 1
///   - Left child of index i:  1 + (i &lt;&lt; 1)
///   - Right child of index i: 2 + (i &lt;&lt; 1)
///
/// Quirks preserved (spec §5):
///   1. Initial array capacity 1024; doubles on overflow.
///   2. <see cref="PathPoint.HeapIndex"/> tracks the node's position in the backing array.
///   3. <see cref="Add"/> throws if node is already in the heap (HeapIndex >= 0).
///   4. <see cref="Poll"/> resets HeapIndex to -1 on the removed node.
///   5. <see cref="Clear"/> merely resets size to 0 — nodes stay in array (overwritten later).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §5
/// </summary>
internal sealed class PathHeap
{
    // ── Fields (spec §5) ─────────────────────────────────────────────────────

    /// <summary>obf: a — backing heap array; initial capacity 1024 (quirk 1).</summary>
    private PathPoint[] _heap = new PathPoint[1024];

    /// <summary>obf: b — current number of elements in the heap.</summary>
    private int _size;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns true when the heap contains no elements.</summary>
    public bool IsEmpty => _size == 0;

    /// <summary>
    /// obf: <c>zs.a(mo)</c> — inserts a node. Places at end, then sifts up.
    /// Throws <see cref="InvalidOperationException"/> if node is already in the heap (quirk 3).
    /// </summary>
    public void Add(PathPoint node)
    {
        if (node.HeapIndex >= 0)
            throw new InvalidOperationException(
                $"PathHeap.Add: node already in heap at index {node.HeapIndex}.");

        // Grow backing array if needed (quirk 1)
        if (_size == _heap.Length)
        {
            var newHeap = new PathPoint[_heap.Length * 2];
            Array.Copy(_heap, newHeap, _heap.Length);
            _heap = newHeap;
        }

        int i = _size++;
        _heap[i] = node;
        node.HeapIndex = i;
        SiftUp(i);
    }

    /// <summary>
    /// obf: <c>zs.c()</c> — removes and returns the minimum-cost node (root).
    /// Swaps root with last element, shrinks size, sifts down. Resets HeapIndex to -1 (quirk 4).
    /// </summary>
    public PathPoint Poll()
    {
        PathPoint root = _heap[0];
        root.HeapIndex = -1; // no longer in heap (quirk 4)

        int last = --_size;
        if (last > 0)
        {
            PathPoint moved = _heap[last];
            _heap[0] = moved;
            moved.HeapIndex = 0;
            SiftDown(0);
        }

        return root;
    }

    /// <summary>
    /// obf: <c>zs.a(mo, float)</c> — re-sorts a node after its TotalCost changed.
    /// Sets <see cref="PathPoint.TotalCost"/> to <paramref name="newTotalCost"/>, then
    /// sifts up (if cost decreased) or down (if cost increased).
    /// </summary>
    public void Update(PathPoint node, float newTotalCost)
    {
        float oldCost = node.TotalCost;
        node.TotalCost = newTotalCost;
        if (newTotalCost < oldCost)
            SiftUp(node.HeapIndex);
        else
            SiftDown(node.HeapIndex);
    }

    /// <summary>
    /// obf: <c>zs.b()</c> — resets the heap to empty (quirk 5).
    /// Does NOT null-out array slots; they are overwritten on next use.
    /// </summary>
    public void Clear() => _size = 0;

    // ── Heap mechanics ────────────────────────────────────────────────────────

    private void SiftUp(int i)
    {
        PathPoint node = _heap[i];
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            PathPoint p = _heap[parent];
            if (node.TotalCost >= p.TotalCost) break;
            // Swap node up
            _heap[i] = p;
            p.HeapIndex = i;
            i = parent;
        }
        _heap[i] = node;
        node.HeapIndex = i;
    }

    private void SiftDown(int i)
    {
        PathPoint node = _heap[i];
        while (true)
        {
            int left  = 1 + (i << 1);
            if (left >= _size) break;

            int right  = 2 + (i << 1);
            int minChild = (right < _size && _heap[right].TotalCost < _heap[left].TotalCost)
                           ? right : left;

            PathPoint child = _heap[minChild];
            if (node.TotalCost <= child.TotalCost) break;

            // Swap node down
            _heap[i] = child;
            child.HeapIndex = i;
            i = minChild;
        }
        _heap[i] = node;
        node.HeapIndex = i;
    }
}
