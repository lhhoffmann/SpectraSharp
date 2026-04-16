using SpectraEngine.Core.Blocks;

namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Replica of <c>agb</c> (TileEntityPiston) — attached to BlockMovingPiston (ID 36).
///
/// Animates a single block being moved by a piston over 2 ticks (0→0.5→1.0 progress).
/// Stores the original block being moved and the facing direction of the push/pull.
///
/// Fields (spec §4):
///   a = storedBlockId, b = storedBlockMeta, j = facing, k = isExtending, l = isSource, m = progress, n = prevProgress
///
/// Quirk §10.2: NBT writes field <c>n</c> (prevProgress), not <c>m</c> (progress).
/// Quirk §10.3: Entity push uses static shared list <c>o</c>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPiston_Spec.md §7.18-§7.22
/// </summary>
public sealed class TileEntityPiston : TileEntity
{
    // ── Direction data (spec §3, ot arrays) ───────────────────────────────────

    private static readonly int[] DirX = {  0, 0,  0, 0, -1, 1 }; // ot.b
    private static readonly int[] DirY = { -1, 1,  0, 0,  0, 0 }; // ot.c
    private static readonly int[] DirZ = {  0, 0, -1, 1,  0, 0 }; // ot.d

    // ── Quirk §10.3: static shared entity list ─────────────────────────────────

    private static readonly List<Entity> s_entityPushList = []; // obf: o

    // ── Instance fields (spec §4 — agb) ───────────────────────────────────────

    public int   StoredBlockId;   // obf: a — ID of block being moved
    public int   StoredBlockMeta; // obf: b — metadata of block being moved
    public int   Facing;          // obf: j — direction of piston push (0-5)
    public bool  IsExtending;     // obf: k — true = push outward; false = retract
    public bool  IsSource;        // obf: l — true = this is the piston arm itself (NOT saved to NBT)
    public float Progress;        // obf: m — current animation target (0→1)
    public float PrevProgress;    // obf: n — previous frame progress (interpolation)

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>Parameterless constructor for NBT-based creation (TileEntity.Create factory).</summary>
    public TileEntityPiston() { }

    /// <summary>
    /// Parametered constructor called by the static factory <c>qz.a</c> (spec §7.22).
    /// </summary>
    public TileEntityPiston(int blockId, int blockMeta, int facing, bool isExtending, bool isSource)
    {
        StoredBlockId   = blockId;
        StoredBlockMeta = blockMeta;
        Facing          = facing;
        IsExtending     = isExtending;
        IsSource        = isSource;
        Progress        = 0.0f;
        PrevProgress    = 0.0f;
    }

    // ── Tick (spec §7.18) ─────────────────────────────────────────────────────

    /// <summary>
    /// Per-tick animation step. Progress advances 0.5F/tick. Finalizes at ≥1.0F.
    /// Spec: <c>agb.b()</c>.
    /// </summary>
    public override void Tick()
    {
        PrevProgress = Progress; // n = m

        if (PrevProgress >= 1.0f)
        {
            // Animation complete — push entities one last time, remove TE, commit block
            EntityPush(1.0f, 0.25f);
            World?.RemoveTileEntity(X, Y, Z);
            MarkDirty();
            if (World != null && World.GetBlockId(X, Y, Z) == 36)
                World.SetBlockAndMetadata(X, Y, Z, StoredBlockId, StoredBlockMeta);
        }
        else
        {
            Progress += 0.5f;
            if (Progress > 1.0f) Progress = 1.0f;

            if (IsExtending)
                EntityPush(Progress, Progress - PrevProgress + 0.0625f);
        }
    }

    // ── InstantFinalize (spec §7.19) ──────────────────────────────────────────

    /// <summary>
    /// Immediately completes the animation. Used when interrupted (spec: <c>agb.j()</c>).
    /// </summary>
    public void InstantFinalize()
    {
        if (PrevProgress >= 1.0f || World == null) return;

        PrevProgress = Progress = 1.0f;
        World.RemoveTileEntity(X, Y, Z);
        MarkDirty();
        if (World.GetBlockId(X, Y, Z) == 36)
            World.SetBlockAndMetadata(X, Y, Z, StoredBlockId, StoredBlockMeta);
    }

    // ── EntityPush (spec §7.20) ───────────────────────────────────────────────

    /// <summary>
    /// Pushes entities within the block's current AABB in the facing direction.
    /// Uses static shared list (vanilla quirk §10.3). Spec: <c>agb.a(float, float)</c>.
    /// </summary>
    private void EntityPush(float progress, float velocity)
    {
        if (World == null) return;

        float effectiveProgress = IsExtending ? (1.0f - progress) : (progress - 1.0f);

        AxisAlignedBB? aabb = BlockMovingPiston.GetMovingAABB(World, X, Y, Z, StoredBlockId, effectiveProgress, Facing);
        if (aabb == null) return;

        List<Entity> entities = World.GetEntitiesWithinAABB<Entity>(aabb);

        s_entityPushList.Clear();
        s_entityPushList.AddRange(entities);

        float dx = velocity * DirX[Facing];
        float dy = velocity * DirY[Facing];
        float dz = velocity * DirZ[Facing];

        foreach (Entity entity in s_entityPushList)
            entity.Move(dx, dy, dz);

        s_entityPushList.Clear();
    }

    // ── NBT (spec §7.21) ──────────────────────────────────────────────────────

    /// <summary>
    /// Quirk §10.2: writes field <c>n</c> (PrevProgress) as "progress", not <c>m</c>.
    /// Field <c>l</c> (IsSource) is intentionally NOT written.
    /// </summary>
    protected override void WriteTileEntityToNbt(Nbt.NbtCompound tag)
    {
        tag.PutInt  ("blockId",   StoredBlockId);
        tag.PutInt  ("blockData", StoredBlockMeta);
        tag.PutInt  ("facing",    Facing);
        tag.PutFloat("progress",  PrevProgress);   // quirk: writes n not m
        tag.PutBoolean("extending", IsExtending);
        // l (IsSource) NOT saved — spec §7.21
    }

    protected override void ReadTileEntityFromNbt(Nbt.NbtCompound tag)
    {
        StoredBlockId   = tag.GetInt    ("blockId");
        StoredBlockMeta = tag.GetInt    ("blockData");
        Facing          = tag.GetInt    ("facing");
        float saved     = tag.GetFloat  ("progress");
        PrevProgress    = saved;   // quirk: both n and m set to saved "progress" (which was n)
        Progress        = saved;
        IsExtending     = tag.GetBoolean("extending");
        // IsSource always defaults to false on load — spec §7.21
    }
}
