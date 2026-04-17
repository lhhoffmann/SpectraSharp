namespace SpectraEngine.Core.Mods;

/// <summary>
/// A <see cref="Block"/> subclass that delegates all tick behaviour to a
/// <see cref="IModBlockBehavior"/> implemented by the stub layer.
///
/// Created by <c>BlockListProxy.set</c> (v1_0) or <c>GameRegistry.registerBlock</c>
/// (v1_7_10) when a mod registers a block. The constructor auto-registers the bridge
/// in <see cref="Block.BlocksList"/> at the given ID — same as every other Core block.
///
/// Scalar properties (hardness, resistance, light) are copied from the stub once at
/// construction. Tick methods delegate to the stub on every call so mod overrides work.
/// </summary>
public sealed class ModBlockBridge(int blockId, IModBlockBehavior stub)
    : Block(blockId, Material.Ground)
{
    readonly IModBlockBehavior _stub = stub;

    // Copy scalar data from the stub
    static ModBlockBridge()  { /* static ctor placeholder — real init in primary ctor body */ }

    /// <summary>Creates the bridge and applies the stub's scalar properties.</summary>
    public static ModBlockBridge? TryCreate(int id, IModBlockBehavior stub)
    {
        if (id <= 0 || id >= BlocksList.Length)
        {
            Console.Error.WriteLine(
                $"[ModBlockBridge] ID {id} is out of range [1, {BlocksList.Length - 1}] — skipped.");
            return null;
        }
        if (BlocksList[id] != null)
        {
            Console.Error.WriteLine(
                $"[ModBlockBridge] Slot {id} already occupied by {BlocksList[id]!.GetType().Name} — " +
                "mod block skipped.");
            return null;
        }

        // Core.Block(int, Material) constructor auto-registers in BlocksList[id].
        var bridge = new ModBlockBridge(id, stub);

        // Apply scalar properties from the stub
        bridge.SetHardness(stub.Hardness);
        bridge.BlockResistance = stub.Resistance;   // bypass *3 quirk; mod already provides raw value
        bridge.SetLightValue(stub.LightFraction);
        bridge.SetLightOpacity(stub.LightOpacity);

        Console.WriteLine(
            $"[ModBlockBridge] Registered mod block id={id} " +
            $"({stub.GetType().Name}) hardness={stub.Hardness}");

        return bridge;
    }

    /// <summary>
    /// Registers a mod block by string registry name (v1_12 / v1_16 style).
    /// Allocates a numeric ID via <see cref="ModNameRegistry"/>, then delegates to
    /// <see cref="TryCreate(int, IModBlockBehavior)"/>.
    /// </summary>
    public static ModBlockBridge? TryCreate(string registryName, IModBlockBehavior stub)
    {
        int id = ModNameRegistry.GetOrAllocateBlockId(registryName);
        if (id < 0) return null;
        return TryCreate(id, stub);
    }

    // ── Tick dispatch ─────────────────────────────────────────────────────────

    public override void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng)
        => _stub.OnBlockTick(world, x, y, z, rng);

    public override void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng)
        => _stub.OnUpdateTick(world, x, y, z, rng);
}
