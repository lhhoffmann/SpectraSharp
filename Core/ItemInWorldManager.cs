namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>aes</c> (ItemInWorldManager) — abstract base for block-interaction managers.
/// Dispatches right-click (use/place) and left-click (break) through the game mode.
///
/// Concrete subclasses:
///   <see cref="SurvivalItemInWorldManager"/> (<c>dm</c>) — progressive block breaking.
///   <see cref="CreativeItemInWorldManager"/> (<c>uq</c>) — instant breaking.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemInWorldManager_Spec.md
/// </summary>
public abstract class ItemInWorldManager
{
    // ── Shared fields ─────────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — owning world.</summary>
    protected World World;

    /// <summary>obf: <c>b</c> — the player using this manager.</summary>
    protected EntityPlayer Player;

    // ── Constructor ───────────────────────────────────────────────────────────

    protected ItemInWorldManager(World world, EntityPlayer player)
    {
        World  = world;
        Player = player;
    }

    // ── Reach distance (spec: Raycast_Spec §Reach Distances) ─────────────────

    /// <summary>
    /// Block reach distance. obf: <c>aes.c()</c>.
    /// Survival = 4.0, Creative = 6.0.
    /// </summary>
    public virtual float GetReach() => 4.0f;

    // ── Abstract / virtual interface ─────────────────────────────────────────

    /// <summary>
    /// Called each game tick while a block is being broken.
    /// Spec: <c>d(vi player)</c> — advances break progress, plays dig sound.
    /// </summary>
    public abstract void UpdateBlockRemoving();

    /// <summary>
    /// Initiates block damage at the given position.
    /// Spec: <c>c(int x, int y, int z, int face)</c> — called on mouse-down.
    /// </summary>
    public abstract void OnBlockClicked(int x, int y, int z, int face);

    /// <summary>
    /// Cancels the current block-break operation.
    /// Spec: <c>b()</c> — called on mouse-up or when focus changes.
    /// </summary>
    public abstract void ResetBlockRemoving();

    /// <summary>
    /// Returns true if this game mode allows instant block breaking (Creative).
    /// Spec: <c>i()</c> on aes.
    /// </summary>
    public abstract bool IsCreative();

    // ── Shared: block removal (spec §4) ───────────────────────────────────────

    /// <summary>
    /// Removes the block at (x, y, z) and harvests drops if in Survival.
    /// Spec: <c>a(int x, int y, int z, int face)</c> on aes.
    /// </summary>
    public virtual void RemoveBlock(int x, int y, int z, int face)
    {
        Block? block = Block.BlocksList[World.GetBlockId(x, y, z)];
        if (block == null) return;

        block.OnBlockDestroyedByPlayer(World, x, y, z, World.GetBlockMetadata(x, y, z));

        if (!IsCreative())
            block.DropBlockAsItem(World, x, y, z, World.GetBlockMetadata(x, y, z), 0);

        World.SetBlock(x, y, z, 0);
    }

    // ── Shared: right-click / use ─────────────────────────────────────────────

    /// <summary>
    /// Attempts to use the held item against a block face.
    /// Spec: <c>a(vi player, ry world, ms item, int x, int y, int z, int face)</c>.
    /// </summary>
    public bool UseItem(EntityPlayer player, World world, ItemStack? heldItem,
                        int x, int y, int z, int face)
    {
        Block? block = Block.BlocksList[world.GetBlockId(x, y, z)];
        if (block != null && block.OnBlockActivated(world, x, y, z, player))
            return true;

        if (heldItem == null) return false;

        Item? item = Item.ItemsList[heldItem.ItemId];
        return item?.OnItemUse(heldItem, player, world, x, y, z, face) ?? false;
    }
}

/// <summary>
/// Replica of <c>dm</c> (ItemInWorldManagerSP / Survival) — progressive block breaking.
///
/// Break formula (spec §3):
///   Each tick: <c>blockDamage += player.GetMiningSpeed(block) / hardness / 30</c>
///   Sound fires every 4 ticks via <c>"dig." + stepSoundName</c>.
///   Block breaks when <c>blockDamage ≥ 1.0</c>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemInWorldManager_Spec.md
/// </summary>
public sealed class SurvivalItemInWorldManager : ItemInWorldManager
{
    // ── Break-state fields (spec §3) ──────────────────────────────────────────

    /// <summary>obf: <c>c/d/e</c> — coordinates of the block currently being broken.</summary>
    private int _curX = -1, _curY = -1, _curZ = -1;

    /// <summary>obf: <c>f</c> — accumulated break progress 0.0 → 1.0.</summary>
    private float _blockDamage;

    /// <summary>obf: <c>g</c> — last sent damage value (for crack animation sync).</summary>
    private float _prevDamage;

    /// <summary>obf: <c>h</c> — tick counter driving the dig-sound cadence (every 4 ticks).</summary>
    private int _soundTick;

    /// <summary>obf: <c>i</c> — cooldown after a break before the next break can begin.</summary>
    private int _breakCooldown;

    // ── Constructor ───────────────────────────────────────────────────────────

    public SurvivalItemInWorldManager(World world, EntityPlayer player) : base(world, player) { }

    // ── ItemInWorldManager overrides ──────────────────────────────────────────

    public override bool IsCreative() => false;

    /// <summary>
    /// Called when the player presses left-click (mouse-down) on a block face.
    /// Spec: <c>c(int x, int y, int z, int face)</c>.
    /// </summary>
    public override void OnBlockClicked(int x, int y, int z, int face)
    {
        if (_breakCooldown > 0) return;

        Block? block = Block.BlocksList[World.GetBlockId(x, y, z)];
        if (block == null) return;

        // Indestructible blocks (hardness < 0) cannot be started
        if (block.BlockHardness < 0f) return;

        _curX         = x;
        _curY         = y;
        _curZ         = z;
        _blockDamage  = 0f;
        _prevDamage   = 0f;
        _soundTick    = 0;
    }

    /// <summary>
    /// Called each game tick while the left mouse button is held.
    /// Spec: <c>d(vi player)</c> on dm.
    /// </summary>
    public override void UpdateBlockRemoving()
    {
        if (_breakCooldown > 0) { _breakCooldown--; return; }
        if (_curX == -1) return;

        int blockId = World.GetBlockId(_curX, _curY, _curZ);
        Block? block = Block.BlocksList[blockId];
        if (block == null) { ResetBlockRemoving(); return; }

        float hardness = block.BlockHardness;
        if (hardness < 0f) { ResetBlockRemoving(); return; } // indestructible

        // Accumulate: GetMiningSpeed already incorporates tool efficiency
        _blockDamage += Player.GetMiningSpeed(block) / (hardness * 30f);

        // Dig sound: every 4 ticks
        _soundTick++;
        if (_soundTick >= 4)
        {
            _soundTick = 0;
            // Sound key: "dig." + stepSound name  (stub: no audio yet)
        }

        if (_blockDamage >= 1.0f)
        {
            RemoveBlock(_curX, _curY, _curZ, 0);
            _breakCooldown = 5; // brief cooldown before next break
            ResetBreakState();
        }
    }

    /// <summary>Cancels the current break — called on mouse-up. Spec: <c>b()</c>.</summary>
    public override void ResetBlockRemoving()
    {
        ResetBreakState();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void ResetBreakState()
    {
        _curX        = -1;
        _curY        = -1;
        _curZ        = -1;
        _blockDamage = 0f;
        _prevDamage  = 0f;
        _soundTick   = 0;
    }
}

/// <summary>
/// Replica of <c>uq</c> (ItemInWorldManagerCreative) — instant block breaking in Creative mode.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemInWorldManager_Spec.md
/// </summary>
public sealed class CreativeItemInWorldManager : ItemInWorldManager
{
    public CreativeItemInWorldManager(World world, EntityPlayer player) : base(world, player) { }

    public override bool IsCreative() => true;
    public override float GetReach()  => 6.0f;

    /// <summary>Instant break: remove the block immediately on click. Spec: <c>c</c> on uq.</summary>
    public override void OnBlockClicked(int x, int y, int z, int face)
        => RemoveBlock(x, y, z, face);

    /// <summary>No progressive update needed in Creative.</summary>
    public override void UpdateBlockRemoving() { }

    /// <summary>Nothing to reset in Creative.</summary>
    public override void ResetBlockRemoving() { }
}
