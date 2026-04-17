namespace SpectraEngine.Core.Container;

/// <summary>
/// Replica of <c>abd</c> (ICrafting) — listener notified when a container's slot changes.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Container_Spec.md §1 (class map)
/// </summary>
public interface ICraftingListener
{
    /// <summary>
    /// obf: <c>a(pj container, int slotIndex, dk newStack)</c>
    /// Called by <see cref="Container.DetectAndSendChanges"/> for each changed slot.
    /// </summary>
    void OnContainerSlotChanged(Container container, int slotIndex, ItemStack? newStack);

    /// <summary>
    /// obf: <c>a(pj container, int dataId, int value)</c>
    /// Called for furnace progress / burn-time data sync (see ContainerFurnace).
    /// </summary>
    void OnContainerDataChanged(Container container, int dataId, int value) { }
}
