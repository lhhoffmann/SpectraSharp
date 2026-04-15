namespace SpectraSharp.Core.Items;

/// <summary>
/// Replica of <c>ou</c> (ItemFlintAndSteel) — flint and steel. Item ID 259.
///
/// Durability 64; max stack 1. Places fire on the clicked block's face.
/// Consumes 1 durability per use regardless of whether fire was placed (spec §6.2 quirk).
///
/// Portal ignition is indirect: fire placement triggers <c>BlockFire.OnBlockAdded</c>,
/// which calls <c>BlockPortal.TryToCreatePortal</c> if inside a valid obsidian frame.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockPortal_Spec.md §6
/// </summary>
public sealed class ItemFlintAndSteel : Item
{
    public ItemFlintAndSteel(int itemId) : base(itemId)
    {
        SetInternalDurability(64); // ou.i(64) — max damage 64
        MaxStackSize = 1;          // ou.bN = 1
    }

    public override int GetMaxDamage() => 64;

    /// <summary>
    /// obf: <c>a(stack, player, world, x, y, z, face)</c> — onItemUse.
    /// Offsets target by face, places fire at adjacent air, always damages 1.
    /// </summary>
    public override bool OnItemUse(ItemStack stack, object player, World world, int x, int y, int z, int face)
    {
        // Offset target position based on clicked face (spec §6.2)
        switch (face)
        {
            case 0: y -= 1; break;
            case 1: y += 1; break;
            case 2: z -= 1; break;
            case 3: z += 1; break;
            case 4: x -= 1; break;
            case 5: x += 1; break;
        }

        // Place fire if air (spec §6.2); ignite sound stub (audio not yet implemented)
        if (world.GetBlockId(x, y, z) == 0)
            world.SetBlock(x, y, z, 51); // ID 51 = Fire (yy.ar.bM)

        // Always consume durability (spec §8.2 quirk — always damages)
        stack.DamageItem(1);

        return true;
    }
}
