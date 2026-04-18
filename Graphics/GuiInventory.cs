namespace SpectraEngine.Graphics;

/// <summary>
/// Slot descriptor used by GuiContainer — screen-space position of a container slot.
/// Replica of <c>vv</c> (Slot).
/// </summary>
public sealed class Slot(int index, int x, int y)
{
    /// <summary>Slot index in the backing container.</summary>
    public readonly int Index = index;

    /// <summary>Screen X relative to container top-left.</summary>
    public readonly int X = x;

    /// <summary>Screen Y relative to container top-left.</summary>
    public readonly int Y = y;
}

/// <summary>
/// Replica of <c>mg</c> (GuiContainer) — base class for all container screens.
/// Centers itself on screen; draws the container background texture and slots.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiInventory_Spec.md §Screen Setup
/// </summary>
public abstract class GuiContainer : GuiScreen
{
    // Container dimensions (spec §Screen Setup)
    protected int ContainerWidth  = 176;
    protected int ContainerHeight = 166;

    // Screen-space top-left of the container background (computed on init)
    protected int LeftX;
    protected int TopY;

    /// <summary>All slots registered by the subclass during <see cref="InitGui"/>.</summary>
    protected readonly List<Slot> Slots = new();

    public override void InitGui()
    {
        LeftX = (Width  - ContainerWidth)  / 2;
        TopY  = (Height - ContainerHeight) / 2;
        Slots.Clear();
        AddSlots();
    }

    /// <summary>Subclasses register their slots here via <see cref="AddSlot"/>.</summary>
    protected abstract void AddSlots();

    protected void AddSlot(int index, int relX, int relY)
        => Slots.Add(new Slot(index, LeftX + relX, TopY + relY));

    public override void DrawScreen(int mouseX, int mouseY, float partialTick)
    {
        // Background texture stub — drawn when DrawTexturedModalRect is wired
        DrawContainerBackground(mouseX, mouseY);
        base.DrawScreen(mouseX, mouseY, partialTick);
    }

    /// <summary>
    /// Draws the container background using /gui/&lt;textureName&gt;.
    /// UV (0, 0), size (ContainerWidth × ContainerHeight).
    /// Stub — pending GL 2D pass.
    /// </summary>
    protected virtual void DrawContainerBackground(int mouseX, int mouseY) { }
}

/// <summary>
/// Replica of <c>hw extends mg</c> (GuiInventory) — the player inventory screen.
///
/// Slot layout (all relative to container top-left):
///   Slot 0  — crafting output  (144, 36)
///   Slots 1–4  — crafting 2×2  (88+col*18, 26+row*18)
///   Slots 5–8  — armour (head/chest/legs/boots) (8, 8+i*18)
///   Slots 9–35 — main inventory  (8+col*18, 84+row*18), 3×9
///   Slots 36–44 — hotbar (8+col*18, 142)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiInventory_Spec.md
/// </summary>
public class GuiInventory : GuiContainer
{
    protected override void AddSlots()
    {
        // Crafting output (slot 0)
        AddSlot(0, 144, 36);

        // Crafting grid 2×2 (slots 1–4)
        for (int row = 0; row < 2; row++)
        for (int col = 0; col < 2; col++)
            AddSlot(col + row * 2 + 1, 88 + col * 18, 26 + row * 18);

        // Armour slots 5–8 (head → boots)
        for (int i = 0; i < 4; i++)
            AddSlot(5 + i, 8, 8 + i * 18);

        // Main inventory 3×9 (slots 9–35)
        for (int row = 0; row < 3; row++)
        for (int col = 0; col < 9; col++)
            AddSlot(col + (row + 1) * 9, 8 + col * 18, 84 + row * 18);

        // Hotbar (slots 36–44)
        for (int col = 0; col < 9; col++)
            AddSlot(col + 36, 8 + col * 18, 142);
    }

    protected override void DrawContainerBackground(int mouseX, int mouseY)
    {
        // DrawTexturedModalRect("gui/inventory.png", LeftX, TopY, 0, 0, ContainerWidth, ContainerHeight)
        // Pending GL 2D pass wiring
    }
}
