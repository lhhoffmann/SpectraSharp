namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>jb</c> (BlockGrass) — Block ID 2.
///
/// Multi-face textures: top=0 (grass_top, biome-tinted), bottom=2 (dirt), sides=3 (grass_side).
/// Random tick: spreads to adjacent dirt blocks when lit; reverts to dirt when covered.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ConcreteBlocks_Spec.md §1
/// </summary>
public sealed class BlockGrass : Block
{
    // Material p.e = Material.RockTransp; StepSound e = Block.SoundGrass (set in BlockRegistry)
    public BlockGrass(int id) : base(id, Material.RockTransp) { }

    // ── Texture (spec §1 — Multi-Face Textures) ───────────────────────────────

    public override int GetTextureIndex(int face) => face switch
    {
        1 => 0,  // top: grass_top (index 0, gray in PNG — needs biome tint)
        0 => 2,  // bottom: dirt
        _ => 3   // sides: grass_side
    };

    public override int GetTextureForFaceAndMeta(int face, int meta) => GetTextureIndex(face);

    // ── Random tick (spec §1 — Tick Behaviour) ────────────────────────────────

    /// <summary>
    /// Spread tick: tries to spread to nearby dirt and reverts to dirt if covered.
    /// Light approximated via IsOpaqueCube checks (full BFS light engine pending).
    /// </summary>
    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
    {
        if (y + 1 >= 128) return;

        // Revert to dirt if block directly above is fully opaque (blocks all light)
        if (LightOpacity[world.GetBlockId(x, y + 1, z)] >= 255)
        {
            world.SetBlock(x, y, z, 3); // become dirt
            return;
        }

        // Attempt to spread grass to adjacent dirt blocks (3×5×3 volume, 4 tries per tick)
        for (int attempt = 0; attempt < 4; attempt++)
        {
            int tx = x + rng.NextInt(3) - 1;
            int ty = y + rng.NextInt(5) - 3;
            int tz = z + rng.NextInt(3) - 1;

            if (ty < 0 || ty >= 128) continue;
            if (world.GetBlockId(tx, ty, tz) != 3) continue;           // must be dirt
            if (LightOpacity[world.GetBlockId(tx, ty + 1, tz)] >= 255) continue; // must have some light above

            world.SetBlock(tx, ty, tz, 2); // dirt → grass
        }
    }
}
