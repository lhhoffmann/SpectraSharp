namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>abm</c> (BlockTNT) — Block ID 46.
/// Can be ignited by redstone, fire, or adjacent explosion.
/// When ignited, spawns <see cref="EntityTNTPrimed"/> and plays fuse sound.
/// When destroyed by explosion, chain-spawns primed TNT with shortened fuse.
///
/// Texture layout (spec §8):
///   face 0 (bottom): bL+2 — cross texture
///   face 1 (top):    bL+1 — cross texture (different from bottom)
///   faces 2–5 (sides): bL — TNT label texture
///
/// Open questions (spec §13):
///   1. world.v(x,y,z) — isBlockPowered (redstone). Stubbed as false until BlockRedstone_Spec.
///   2. world.c(x,y,z,meta) — setBlockMetadataWithNotify. Stubbed as direct meta set.
///   3. FlintAndSteel item ID — stubbed as 259 (vanilla ID).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Explosion_Spec.md §8
/// </summary>
public sealed class BlockTNT : Block
{
    // ── Construction (spec §8) ────────────────────────────────────────────────

    public BlockTNT(int id) : base(id, 8, Material.Mat_R)
    {
        SetHardness(0.0f); // instantly breakable
        ClearNeedsRandomTick();
        SetBlockName("tnt");
    }

    public override bool IsOpaqueCube() => true;
    public override bool RenderAsNormalBlock() => true;

    // ── Texture by face (spec §8 — b(int face)) ──────────────────────────────

    public override int GetTextureIndex(int face) => face switch
    {
        0 => BlockIndexInTexture + 2, // bottom: cross texture
        1 => BlockIndexInTexture + 1, // top: cross texture
        _ => BlockIndexInTexture      // sides: TNT label
    };

    // ── onBlockAdded (spec §8 — a(ry,x,y,z)) ────────────────────────────────

    public override void OnBlockAdded(IWorld world, int x, int y, int z)
    {
        base.OnBlockAdded(world, x, y, z);
        // If powered by redstone, ignite (spec §8 open question §1 — stubbed as false)
        // TODO: replace with world.IsBlockPowered(x,y,z) when BlockRedstone_Spec is implemented
        if (IsBlockPowered(world, x, y, z))
        {
            Ignite(world, x, y, z, 1);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── onNeighborBlockChange (spec §8 — a(ry,x,y,z,int)) ───────────────────

    public override void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId)
    {
        // Only ignite if the changing neighbour can drop from explosion (i.e. is real)
        // and the block is now receiving power. See spec open question §1.
        if (neighbourId > 0 && BlocksList[neighbourId] != null
            && IsBlockPowered(world, x, y, z))
        {
            Ignite(world, x, y, z, 1);
            world.SetBlock(x, y, z, 0);
        }
    }

    // ── drops (spec §8 — a(Random)) ──────────────────────────────────────────

    /// <summary>No normal drop — handled in <see cref="Ignite"/>. Spec §8.</summary>
    public override int IdDropped(int meta, JavaRandom rng, int fortune) => 0;

    /// <summary>
    /// Test stub: returns null per spec §8 c_(meta) — drops handled manually in e().
    /// </summary>
    public static object? GetItem(int meta) => null;

    // ── Ignite / harvestBlock (spec §8 — e(ry,x,y,z,meta)) ─────────────────

    /// <summary>
    /// obf: <c>abm.e(ry,x,y,z,meta)</c> — ignites or drops TNT block.
    /// If bit 0 of <paramref name="meta"/> is 0: drop item. If 1: spawn EntityTNTPrimed.
    /// Spec §8.
    /// </summary>
    public static void Ignite(IWorld world, int x, int y, int z, int meta)
    {
        if (world.IsClientSide) return;

        if ((meta & 1) == 0)
        {
            // Non-ignited break: drop block as item
            SpawnItemAt(world, x, y, z, new ItemStack(46, 1, 0));
        }
        else
        {
            // Ignited: spawn EntityTNTPrimed
            if (world is World concreteWorld)
            {
                var tnt = new EntityTNTPrimed(concreteWorld, x + 0.5, y + 0.5, z + 0.5);
                concreteWorld.SpawnEntity(tnt);
                // Sound stub: "random.fuse" — audio system not yet implemented
            }
        }
    }

    // ── onBlockDestroyedByExplosion (spec §8 — i(ry,x,y,z)) ─────────────────

    /// <summary>
    /// obf: <c>abm.i(ry,x,y,z)</c> — chain-spawn primed TNT with shortened fuse.
    /// Fuse = nextInt(20) + 10 = [10, 29] ticks (quirk 4: 0.5–1.45 s).
    /// Spec §8 / Explosion_Spec §12.4.
    /// </summary>
    public override void OnBlockDestroyedByExplosion(IWorld world, int x, int y, int z)
    {
        if (world.IsClientSide) return;
        if (world is not World concreteWorld) return;

        var tnt = new EntityTNTPrimed(concreteWorld, x + 0.5, y + 0.5, z + 0.5);
        // Chain fuse: nextInt(fuse/4) + fuse/8 at construction (fuse=80) = nextInt(20)+10
        tnt.Fuse = world.Random.NextInt(tnt.Fuse / 4) + tnt.Fuse / 8;
        concreteWorld.SpawnEntity(tnt);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Stub for world.v(x,y,z) — isBlockPowered (redstone).
    /// Always returns false until BlockRedstone_Spec is implemented.
    /// Open question spec §13.1.
    /// </summary>
    private static bool IsBlockPowered(IWorld world, int x, int y, int z) => false;

    /// <summary>Spawns an item entity at the block centre.</summary>
    private static void SpawnItemAt(IWorld world, int x, int y, int z, ItemStack stack)
    {
        if (world is not World concreteWorld) return;
        var entity = new EntityItem(concreteWorld, x + 0.5, y + 0.5, z + 0.5, stack);
        entity.PickupDelay = 10;
        concreteWorld.SpawnEntity(entity);
    }
}
