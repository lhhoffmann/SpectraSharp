namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>uw</c> (ItemBlock) — the default Item form of a placeable block.
///
/// Constructor: <c>uw(int itemId)</c> where <c>itemId = blockId − 256</c>.
///   The base <see cref="Item"/> constructor registers this at <c>ItemsList[256 + itemId]</c>.
///   <see cref="BlockId"/> stores <c>itemId + 256</c> = the actual block ID.
///
/// The <see cref="OnItemUse"/> override places the associated block into the world when
/// the player right-clicks a block face, subject to 5 validity guards (spec §4).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemBlock_Spec.md
/// </summary>
public class ItemBlock : Item
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a</c> — associated block ID (= itemId + 256).
    /// E.g. stone: itemId = 1 − 256 = −255 → blockId = −255 + 256 = 1.
    /// </summary>
    public readonly int BlockId;

    // ── Constructor (spec §2) ─────────────────────────────────────────────────

    /// <param name="itemId">Block-item ID = blockId − 256.</param>
    public ItemBlock(int itemId) : base(itemId)
    {
        BlockId = itemId + 256;
    }

    // ── Icon index (spec §3) ──────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>b(int face)</c> — returns the texture index for the inventory icon.
    /// Uses face 2 (south/front side) of the associated block.
    /// </summary>
    public virtual int GetTextureFromSide(int face)
    {
        Block? block = Block.BlocksList[BlockId];
        return block?.GetTextureIndex(face) ?? 0;
    }

    // ── onItemUse — place block (spec §4) ────────────────────────────────────

    /// <summary>
    /// obf: <c>a(dk, vi, ry, x, y, z, face)</c> — onItemUse.
    /// Places the associated block adjacent to the clicked face. Returns true on success.
    /// </summary>
    public override bool OnItemUse(ItemStack stack, object playerObj, World world, int x, int y, int z, int face)
    {
        // ── Step 1: adjust target position by clicked face ────────────────────
        switch (face)
        {
            case 0: y--; break; // down  → place below
            case 1: y++; break; // up    → place above
            case 2: z--; break; // north
            case 3: z++; break; // south
            case 4: x--; break; // west
            case 5: x++; break; // east
        }

        Block? blockToPlace = Block.BlocksList[BlockId];
        if (blockToPlace == null) return false;

        // ── Step 2: validity guards ───────────────────────────────────────────

        // Guard 1: no items left
        if (stack.StackSize <= 0) return false;

        // Guard 2: player reach / build permission (stub via virtual CanPlaceBlockAt)
        if (playerObj is EntityPlayer player && !player.CanPlaceBlockAt(world, x, y, z))
            return false;

        // Guard 3: height limit — cannot place a solid block at y=255
        if (y == 255 && (blockToPlace.BlockMaterial?.IsSolid() ?? true))
            return false;

        // Guard 4: canBlockStay — block-specific placement validity
        //   Stub: skip (world.a(x,y,z,block,true) not in IWorld interface)

        // Guard 5: target position must be replaceable (air = ID 0, or other passable blocks)
        if (!IsReplaceable(world, x, y, z)) return false;

        // ── Step 3: place the block ───────────────────────────────────────────
        world.SetBlock(x, y, z, BlockId);
        // onBlockPlaced and onBlockAdded hooks: stubs (not yet in IWorld interface)

        // ── Step 4: play placement sound ──────────────────────────────────────
        StepSound? sound = blockToPlace.StepSoundGroup;
        if (sound != null)
        {
            // world.playSoundEffect(x+0.5, y+0.5, z+0.5, sound.Name, vol, pitch)
            // Sound system not yet implemented — no-op stub
        }

        // ── Step 5: decrement stack ───────────────────────────────────────────
        stack.StackSize -= 1;

        return true;
    }

    /// <summary>
    /// Returns true if the block at (x, y, z) can be replaced by a placement.
    /// In vanilla: air (0), water (8/9), lava (10/11), tall grass (31), dead bush (32), etc.
    /// Stub: only accepts air for now.
    /// </summary>
    private static bool IsReplaceable(IWorld world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        return id == 0; // air only — canReplace spec not yet complete
    }
}
