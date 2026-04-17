using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>rn</c> (BlockWorkbench) — Block ID 58.
/// No TileEntity. Right-click opens the 3×3 crafting grid GUI.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec.md §1
/// </summary>
public sealed class BlockWorkbench : Block
{
    public BlockWorkbench() : base(58, 59, Material.Plants)
    {
        SetHardness(2.5f);
        SetResistance(5.0f);
        SetStepSound(SoundWood);
        SetBlockName("workbench");
    }

    // ── Textures (spec §1.3) ─────────────────────────────────────────────────

    public override int GetTextureIndex(int face)
    {
        return face switch
        {
            0 => 4,      // bottom — oak planks
            1 => 43,     // top    — bL - 16 = grid pattern
            2 => 60,     // south  — bL + 1  = side with tool art
            4 => 60,     // west   — bL + 1  = side with tool art
            _ => 59      // north/east — bL  = blank side
        };
    }

    // ── Right-click (spec §1.4) ───────────────────────────────────────────────

    public override bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player)
    {
        if (world.IsClientSide) return true;

        // Opens the 3×3 crafting GUI at this block position.
        // Container_Spec will wire player.OpenCraftingInventory when implemented.
        player.OpenCraftingInventory(x, y, z);
        return true;
    }
}
