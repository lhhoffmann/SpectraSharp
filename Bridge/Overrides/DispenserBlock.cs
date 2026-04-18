using SpectraEngine.Core;
using SpectraEngine.Core.Items;
using SpectraEngine.Core.TileEntity;

namespace SpectraEngine.Bridge.Overrides;

/// <summary>
/// Replica of <c>cu</c> (BlockDispenser) — directional container that ejects items when
/// powered by a Redstone signal.
///
/// Facing is encoded in block metadata:
///   2 = North (−Z), 3 = South (+Z), 4 = West (−X), 5 = East (+X)
///
/// Dispense dispatch table (BlockDispenser_Spec.md):
///   Arrow        → EntityArrow (vel = facing × 6.6)
///   Snowball     → EntitySnowball
///   Egg          → EntityEgg
///   SplashPotion → EntityPotion (vel = facing × 4.125)
///   Other        → EntityItem drop
///   Empty        → event 1001 (click higher pitch)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockDispenser_Spec.md
/// </summary>
public sealed class DispenserBlock : BlockBase
{
    public override string JavaClassName => "net.minecraft.src.BlockDispenser";
    public override int    BlockId       => 23;
    public override int    TextureIndex  => 46; // dispenser front face

    // ── Redstone trigger ──────────────────────────────────────────────────────

    /// <summary>Called on a rising redstone edge (block power change).</summary>
    public void Dispense(World world, int x, int y, int z)
    {
        if (world.IsClientSide) return;

        var te = world.GetTileEntity(x, y, z) as TileEntityDispenser;
        if (te == null) return;

        int slot = te.PickRandomSlot();

        if (slot == -1)
        {
            // Empty dispenser — click events 1000 + 1001
            world.PlayAuxSFX(null, SoundEventId.DispenserFire,  x, y, z, 0);
            world.PlayAuxSFX(null, SoundEventId.DispenserEmpty, x, y, z, 0);
            return;
        }

        ItemStack? stack = te.Slots[slot];
        if (stack == null) return;

        int meta  = world.GetBlockMetadata(x, y, z);
        (double dx, double dz) = FacingVector(meta);
        double spawnX = x + 0.5 + dx * 0.5;
        double spawnY = y + 0.5;
        double spawnZ = z + 0.5 + dz * 0.5;

        DispatchItem(world, stack, spawnX, spawnY, spawnZ, dx, dz);

        // Reduce stack; clear slot if empty
        stack.StackSize--;
        if (stack.StackSize <= 0)
            te.Slots[slot] = null;

        // Smoke particles at dispenser face (event 2000)
        world.PlayAuxSFX(null, SoundEventId.DispenserSmoke, x, y, z, meta);
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    private static void DispatchItem(World world, ItemStack stack,
        double spawnX, double spawnY, double spawnZ, double dx, double dz)
    {
        int itemId = stack.ItemId;

        if (itemId == ItemRegistry.Arrow.RegistryIndex)
        {
            // Arrow: speed = 6.6 in facing direction
            var arrow = new EntityArrow(world);
            arrow.SetPosition(spawnX, spawnY, spawnZ);
            arrow.MotionX = dx * 6.6;
            arrow.MotionZ = dz * 6.6;
            arrow.IsPlayerArrow = false;
            world.SpawnEntity(arrow);
        }
        else if (itemId == ItemRegistry.Snowball.RegistryIndex)
        {
            var ball = new EntitySnowball(world, spawnX, spawnY, spawnZ);
            ball.MotionX = dx * 1.5;
            ball.MotionZ = dz * 1.5;
            world.SpawnEntity(ball);
        }
        else if (itemId == ItemRegistry.Egg.RegistryIndex)
        {
            var egg = new EntityEgg(world, spawnX, spawnY, spawnZ);
            egg.MotionX = dx * 1.5;
            egg.MotionZ = dz * 1.5;
            world.SpawnEntity(egg);
        }
        else if (itemId == ItemRegistry.Potion.RegistryIndex && IsSplashPotion(stack))
        {
            // Splash potion: speed = 4.125 in facing direction
            // EntityPotion not yet implemented — drop as item
            SpawnItemDrop(world, stack, spawnX, spawnY, spawnZ, dx, dz);
        }
        else
        {
            SpawnItemDrop(world, stack, spawnX, spawnY, spawnZ, dx, dz);
        }
    }

    private static void SpawnItemDrop(World world, ItemStack stack,
        double spawnX, double spawnY, double spawnZ, double dx, double dz)
    {
        var drop = new EntityItem(world, spawnX, spawnY, spawnZ, stack);
        drop.MotionX = dx * 0.3;
        drop.MotionZ = dz * 0.3;
        drop.MotionY = 0.1;
        drop.PickupDelay = 10;
        world.SpawnEntity(drop);
    }

    // ── Break behaviour ───────────────────────────────────────────────────────

    public void OnBlockBreak(World world, int x, int y, int z)
    {
        if (world.IsClientSide) return;
        var te = world.GetTileEntity(x, y, z) as TileEntityDispenser;
        if (te == null) return;

        foreach (var slot in te.Slots)
        {
            if (slot == null) continue;
            var drop = new EntityItem(world, x + 0.5, y + 0.5, z + 0.5, slot);
            drop.MotionX = world.Random.NextDouble() * 0.2 - 0.1;
            drop.MotionY = world.Random.NextDouble() * 0.2 + 0.1;
            drop.MotionZ = world.Random.NextDouble() * 0.2 - 0.1;
            world.SpawnEntity(drop);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (double dx, double dz) FacingVector(int meta) => meta switch
    {
        2 => (0.0,  -1.0), // North
        3 => (0.0,  +1.0), // South
        4 => (-1.0,  0.0), // West
        5 => (+1.0,  0.0), // East
        _ => (0.0,  +1.0), // default South
    };

    private static bool IsSplashPotion(ItemStack stack)
        => (stack.Damage & 0x4000) != 0; // bit 14 = splash flag
}
