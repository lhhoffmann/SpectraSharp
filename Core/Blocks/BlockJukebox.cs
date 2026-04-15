using SpectraSharp.Core.TileEntity;

namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>abl</c> (BlockJukebox) — Block ID 84.
/// Container block that holds one music disc. Players right-click to eject the disc.
/// The item (pe) calls <see cref="InsertRecord"/> when inserting a disc.
///
/// Metadata layout (spec §5):
///   Bit 0: hasRecord — 0 = empty, 1 = contains a disc.
///   Bits 1–3: unused in 1.0.
///
/// Texture layout (spec §4.4):
///   face 1 (top): BlockIndexInTexture + 1 = 75
///   all other faces: BlockIndexInTexture = 74
///
/// Quirks preserved (spec §7):
///   7.1 — Ejection position uses world RNG (not local), consuming 3 nextFloat calls.
///   7.2 — Disc ID stored in TileEntity, not in metadata.
///   7.4 — dropBlockAsItem always passes damage=0.
///   7.5 — Ejected disc pickup delay = 10 ticks (not the standard 40).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemRecord_Jukebox_Spec.md §4
/// </summary>
public sealed class BlockJukebox : Block
{
    // ── Construction (spec §3) ────────────────────────────────────────────────

    public BlockJukebox(int id) : base(id, 74, Material.Plants) // p.d = wood material
    {
        SetHardness(2.0f);
        SetResistance(10.0f);
        SetStepSound(SoundStoneHighPitch);
        SetHasTileEntity();
        SetBlockName("jukebox");
    }

    // ── Texture by face (spec §4.4) ───────────────────────────────────────────

    public override int GetTextureIndex(int face)
        => face == 1 ? BlockIndexInTexture + 1 : BlockIndexInTexture;

    // ── TileEntity ────────────────────────────────────────────────────────────

    public override bool IsOpaqueCube() => true;
    public override bool RenderAsNormalBlock() => true;

    // ── onBlockActivated (spec §4.5) ──────────────────────────────────────────

    /// <summary>
    /// Player right-clicks jukebox. If loaded, ejects the record. Spec §4.5.
    /// </summary>
    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.GetBlockMetadata(x, y, z) == 0) return false;  // empty — nothing to eject
        if (world.IsClientSide) return true;
        EjectRecord(world, x, y, z);
        return true;
    }

    // ── insertRecord (spec §4.6) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>f(ry, int, int, int, int)</c> — inserts a disc into the jukebox.
    /// Called by <see cref="ItemRecord.OnItemUse"/> after all validity checks pass.
    /// </summary>
    public void InsertRecord(IWorld world, int x, int y, int z, int recordItemId)
    {
        if (world.IsClientSide) return;
        if (world.GetTileEntity(x, y, z) is not TileEntityJukebox te) return;
        te.RecordId = recordItemId;
        te.MarkDirty();
        world.SetMetadata(x, y, z, 1); // meta=1: has disc
    }

    // ── ejectRecord (spec §4.7) ───────────────────────────────────────────────

    /// <summary>
    /// obf: <c>g(ry, int, int, int)</c> — stops playback and ejects the loaded disc.
    /// Called by <see cref="OnBlockActivated"/> and <see cref="OnBlockPreDestroy"/>.
    /// Ejection position uses world RNG (quirk 7.1).
    /// Pickup delay on ejected disc = 10 ticks (quirk 7.5).
    /// </summary>
    public void EjectRecord(IWorld world, int x, int y, int z)
    {
        if (world.IsClientSide) return;
        if (world.GetTileEntity(x, y, z) is not TileEntityJukebox te) return;
        int recordId = te.RecordId;
        if (recordId == 0) return; // already empty

        // Broadcast event 1005 with data=0 to stop playback on clients
        world.PlayAuxSFX(null, 1005, x, y, z, 0);

        // Clear tile entity state
        te.RecordId = 0;
        te.MarkDirty();
        world.SetMetadata(x, y, z, 0); // meta=0: empty

        // Compute ejection offset using world RNG (quirk 7.1 — must use world.Random)
        // nextFloat→X, nextFloat→Y, nextFloat→Z order is specified in spec §4.7
        const float factor = 0.7f;
        float offsetX = world.Random.NextFloat() * factor + (1.0f - factor) * 0.5f;
        float offsetY = world.Random.NextFloat() * factor + (1.0f - factor) * 0.2f + 0.6f;
        float offsetZ = world.Random.NextFloat() * factor + (1.0f - factor) * 0.5f;

        if (world is not World concreteWorld) return;
        var entity = new EntityItem(concreteWorld,
            x + offsetX, y + offsetY, z + offsetZ,
            new ItemStack(recordId, 1, 0));
        entity.PickupDelay = 10; // spec quirk 7.5
        concreteWorld.SpawnEntity(entity);
    }

    // ── onBlockPreDestroy (spec §4.8) ─────────────────────────────────────────

    /// <summary>
    /// obf: <c>d(ry, int, int, int)</c> — ejects disc before block is removed.
    /// Called while the tile entity is still accessible. Spec §4.8.
    /// </summary>
    public override void OnBlockPreDestroy(IWorld world, int x, int y, int z)
        => EjectRecord(world, x, y, z);

    // ── dropBlockAsItem (spec §4.9, quirk 7.4) ───────────────────────────────

    /// <summary>
    /// obf: <c>a(ry, int, int, int, int, float, int)</c>.
    /// Always calls super with damage=0 regardless of fortune (quirk 7.4).
    /// Disc is ejected separately by <see cref="OnBlockPreDestroy"/>.
    /// Spec §4.9.
    /// </summary>
    public override void DropBlockAsItemWithChance(IWorld world, int x, int y, int z, int meta, float chance, int fortune)
        => base.DropBlockAsItemWithChance(world, x, y, z, meta, chance, 0);
}
