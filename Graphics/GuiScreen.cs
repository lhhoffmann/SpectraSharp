namespace SpectraEngine.Graphics;

/// <summary>
/// Replica of <c>ht</c> (base drawable) — abstract base for all GUI elements.
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiScreen_Spec.md
/// </summary>
public abstract class GuiBase
{
    public int Width;
    public int Height;
}

/// <summary>
/// Replica of <c>ct</c> (GuiButton) — a clickable GUI button.
///
/// Collision: <c>mouseX ∈ [X, X+Width)</c> and <c>mouseY ∈ [Y, Y+Height)</c>.
/// Default size 200×20 matching vanilla.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiScreen_Spec.md §GuiButton
/// </summary>
public class GuiButton
{
    public readonly int Id;
    public int    X, Y;
    public int    ButtonWidth  = 200;
    public int    ButtonHeight = 20;
    public bool   Enabled      = true;
    public bool   Visible      = true;
    public string DisplayString;

    public GuiButton(int id, int x, int y, string label)
        : this(id, x, y, 200, 20, label) { }

    public GuiButton(int id, int x, int y, int width, int height, string label)
    {
        Id            = id;
        X             = x;
        Y             = y;
        ButtonWidth   = width;
        ButtonHeight  = height;
        DisplayString = label;
    }

    public bool ContainsPoint(int mouseX, int mouseY)
        =>  mouseX >= X && mouseX < X + ButtonWidth
         && mouseY >= Y && mouseY < Y + ButtonHeight;

    /// <summary>Draws the button. Stub — actual GL calls pending.</summary>
    public virtual void Draw(FontRenderer font, int mouseX, int mouseY) { }
}

/// <summary>
/// Replica of <c>xe</c> (GuiScreen) — abstract base class for all GUI screens.
/// Screens own a button list, receive keyboard and mouse events, and draw each frame.
///
/// Lifecycle: <c>InitGui</c> → N× (<c>DrawScreen</c> / input events) → <c>OnGuiClosed</c>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiScreen_Spec.md §GuiScreen
/// </summary>
public abstract class GuiScreen : GuiBase
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>o</c> — registered button list.</summary>
    public readonly List<GuiButton> Buttons = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>Called once when the screen opens. Subclasses add buttons here.</summary>
    public virtual void InitGui() { }

    /// <summary>Called when the screen is dismissed.</summary>
    public virtual void OnGuiClosed() { }

    // ── Draw ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws buttons and screen contents. obf: <c>xe.a(int mouseX, int mouseY, float partial)</c>.
    /// Subclasses override and call <c>base.DrawScreen</c> for button rendering.
    /// </summary>
    public virtual void DrawScreen(int mouseX, int mouseY, float partialTick)
    {
        foreach (var btn in Buttons)
            if (btn.Visible) btn.Draw(null!, mouseX, mouseY);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Keyboard input. ESC (keyCode 1) closes the screen.
    /// obf: <c>xe.a(char typedChar, int keyCode)</c>.
    /// </summary>
    public virtual void KeyTyped(char typedChar, int keyCode)
    {
        if (keyCode == 1) // ESC
            CloseScreen();
    }

    /// <summary>
    /// Mouse button press. Finds the first enabled button under the cursor and
    /// plays a click sound, then calls <see cref="ActionPerformed"/>.
    /// obf: <c>xe.a(int mouseX, int mouseY, int button)</c>.
    /// </summary>
    public virtual void MouseClicked(int mouseX, int mouseY, int button)
    {
        if (button != 0) return;
        foreach (var btn in Buttons)
        {
            if (!btn.Enabled || !btn.Visible) continue;
            if (!btn.ContainsPoint(mouseX, mouseY)) continue;
            // PlaySound("random.click") — deferred to audio system
            ActionPerformed(btn);
            return;
        }
    }

    /// <summary>
    /// Mouse button release. Default is a no-op.
    /// obf: <c>xe.b(int mouseX, int mouseY, int button)</c>.
    /// </summary>
    public virtual void MouseReleased(int mouseX, int mouseY, int button) { }

    /// <summary>Called when a registered button is clicked.</summary>
    protected virtual void ActionPerformed(GuiButton button) { }

    /// <summary>Requests that this screen be dismissed.</summary>
    protected virtual void CloseScreen() { }
}

/// <summary>
/// Replica of <c>qd</c> (GuiIngame) — the always-visible HUD overlay.
/// Extends <see cref="GuiBase"/> (not <see cref="GuiScreen"/>).
/// Rendered every frame on top of world geometry.
///
/// Scale factor: largest integer N where windowWidth/N ≥ 320 and windowHeight/N ≥ 240, capped 4.
///
/// Textures (from JAR):
///   gui/gui.png   — hotbar strip (UV 0,0 182×22) and selection highlight (UV 0,22 24×22)
///   gui/icons.png — all status icons (crosshair, hearts, food, armour, air, XP)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/GuiIngameHUD_Spec.md
/// </summary>
public class GuiIngame : GuiBase
{
    // ── gui/gui.png UVs (spec §Hotbar) ────────────────────────────────────────

    private const int HotbarStripU      = 0,  HotbarStripV      = 0;
    private const int HotbarStripW      = 182, HotbarStripH     = 22;
    private const int HotbarSelectU     = 0,  HotbarSelectV     = 22;
    private const int HotbarSelectW     = 24, HotbarSelectH     = 22;

    // ── gui/icons.png UVs (spec §Icons atlas) ─────────────────────────────────

    // Crosshair: UV (0,0) 16×16 at screen centre − 7
    private const int CrosshairU = 0,  CrosshairV = 0,  CrosshairSize = 16;

    // XP bar: background UV (0,64) 182×5; fill UV (0,69)
    private const int XpBgU = 0, XpBgV = 64, XpFillV = 69, XpBarW = 182, XpBarH = 5;

    // Hearts: row y = screenH − 39; i*8 from left; 9×9 icons
    private const int HeartBgU     = 16, HeartBgV     = 0;
    private const int HeartFullU   = 52, HeartFullV   = 0;
    private const int HeartHalfU   = 61, HeartHalfV   = 0;
    private const int HeartIconSize = 9;

    // Armour: row y = screenH − 49 (above health)
    private const int ArmorEmptyU = 16, ArmorEmptyV = 9;
    private const int ArmorHalfU  = 25, ArmorHalfV  = 9;
    private const int ArmorFullU  = 34, ArmorFullV  = 9;

    // Food: row y = screenH − 39 (right side, drawn right to left)
    private const int FoodBgU   = 16, FoodBgV   = 27;
    private const int FoodFullU = 52, FoodFullV = 27;
    private const int FoodHalfU = 61, FoodHalfV = 27;

    // Air bubbles: row y = screenH − 49 (right side)
    private const int BubbleFullU  = 25, BubbleFullV  = 18;
    private const int BubbleEmptyU = 16, BubbleEmptyV = 18;
    private const int BubbleSize   = 9;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>True when the HUD should be rendered (F1 not toggled). Spec: <c>!A.D</c>.</summary>
    public bool IsVisible { get; set; } = true;

    // ── Scale factor (spec §Scale factor) ─────────────────────────────────────

    /// <summary>Computes the GUI scale factor for the given window dimensions.</summary>
    public static int ComputeScaleFactor(int windowWidth, int windowHeight, int maxScale = 4)
    {
        int factor = 1;
        while (factor < maxScale
               && windowWidth  / (factor + 1) >= 320
               && windowHeight / (factor + 1) >= 240)
            factor++;
        return factor;
    }

    // ── Render ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders the HUD for the current frame.
    /// Delegates to element-specific helpers; each helper documents its UV source.
    /// Actual GL draw calls (DrawTexturedModalRect) are deferred until the
    /// 2D rendering pass is wired in the render thread.
    ///
    /// Spec: <c>qd.a(float partialTick, bool hasChatOpen, int mouseX, int mouseY)</c>.
    /// </summary>
    public virtual void RenderGameOverlay(float partialTick, int screenW, int screenH,
                                          int hotbarSlot, float xpProgress,
                                          int hp, int maxHp, int food, int armor, int air,
                                          bool isInSurvival)
    {
        if (!IsVisible) return;

        RenderHotbar(screenW, screenH, hotbarSlot);

        if (isInSurvival)
        {
            RenderXpBar(screenW, screenH, xpProgress);
            RenderHealthBar(screenW, screenH, hp);
            RenderFoodBar(screenW, screenH, food);
            if (armor > 0) RenderArmorBar(screenW, screenH, armor);
            // Air bubbles rendered when player is underwater — deferred
        }

        RenderCrosshair(screenW, screenH);
    }

    // ── Element helpers (all stubs pending GL draw wiring) ────────────────────

    /// <summary>
    /// Hotbar strip: gui/gui.png UV(0,0) 182×22 at (screenW/2−91, screenH−22).
    /// Selection: UV(0,22) 24×22 at (screenW/2−91−1 + slot*20, screenH−23).
    /// </summary>
    protected virtual void RenderHotbar(int screenW, int screenH, int slot)
    {
        // Strip
        int stripX = screenW / 2 - 91;
        int stripY = screenH - 22;
        DrawTexturedModalRect("gui/gui.png",
            stripX, stripY, HotbarStripU, HotbarStripV, HotbarStripW, HotbarStripH);

        // Selection highlight
        int selX = stripX - 1 + slot * 20;
        int selY = screenH - 23;
        DrawTexturedModalRect("gui/gui.png",
            selX, selY, HotbarSelectU, HotbarSelectV, HotbarSelectW, HotbarSelectH);
    }

    /// <summary>
    /// XP bar: background UV(0,64) 182×5 then proportional fill UV(0,69) at screenH−29.
    /// xpProgress ∈ [0,1]; fill width = floor(xpProgress * 183).
    /// </summary>
    protected virtual void RenderXpBar(int screenW, int screenH, float xpProgress)
    {
        int x = screenW / 2 - 91;
        int y = screenH - 29;
        DrawTexturedModalRect("gui/icons.png", x, y, XpBgU, XpBgV, XpBarW, XpBarH);

        int fillW = (int)(xpProgress * 183);
        if (fillW > 0)
            DrawTexturedModalRect("gui/icons.png", x, y, XpBgU, XpFillV, fillW, XpBarH);
    }

    /// <summary>
    /// Health bar: 10 icons at screenH−39, left side, i*8 from screenW/2−91.
    /// Each icon: background (16,0) then full (52,0) or half (61,0) heart 9×9.
    /// </summary>
    protected virtual void RenderHealthBar(int screenW, int screenH, int hp)
    {
        int rowY = screenH - 39;
        for (int i = 0; i < 10; i++)
        {
            int ix = screenW / 2 - 91 + i * 8;
            DrawTexturedModalRect("gui/icons.png", ix, rowY, HeartBgU, HeartBgV, HeartIconSize, HeartIconSize);

            if (hp > i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, HeartFullU, HeartFullV, HeartIconSize, HeartIconSize);
            else if (hp == i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, HeartHalfU, HeartHalfV, HeartIconSize, HeartIconSize);
        }
    }

    /// <summary>
    /// Food bar: 10 icons at screenH−39, right side, drawn right-to-left.
    /// Positions: screenW/2+91 − i*8 − 9 for i=0..9.
    /// </summary>
    protected virtual void RenderFoodBar(int screenW, int screenH, int food)
    {
        int rowY = screenH - 39;
        for (int i = 0; i < 10; i++)
        {
            int ix = screenW / 2 + 91 - i * 8 - 9;
            DrawTexturedModalRect("gui/icons.png", ix, rowY, FoodBgU, FoodBgV, HeartIconSize, HeartIconSize);

            if (food > i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, FoodFullU, FoodFullV, HeartIconSize, HeartIconSize);
            else if (food == i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, FoodHalfU, FoodHalfV, HeartIconSize, HeartIconSize);
        }
    }

    /// <summary>
    /// Armour bar: 10 icons at screenH−49, left side (same X as health).
    /// Empty (16,9) / Half (25,9) / Full (34,9) armour icons 9×9.
    /// Only drawn when armor > 0.
    /// </summary>
    protected virtual void RenderArmorBar(int screenW, int screenH, int armor)
    {
        int rowY = screenH - 49;
        for (int i = 0; i < 10; i++)
        {
            int ix = screenW / 2 - 91 + i * 8;
            if (armor > i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, ArmorFullU, ArmorFullV, HeartIconSize, HeartIconSize);
            else if (armor == i * 2 + 1)
                DrawTexturedModalRect("gui/icons.png", ix, rowY, ArmorHalfU, ArmorHalfV, HeartIconSize, HeartIconSize);
            else
                DrawTexturedModalRect("gui/icons.png", ix, rowY, ArmorEmptyU, ArmorEmptyV, HeartIconSize, HeartIconSize);
        }
    }

    /// <summary>
    /// Crosshair: icons.png UV(0,0) 16×16 centred at screen centre.
    /// Blend mode: GL_ONE_MINUS_DST_COLOR × GL_ONE_MINUS_SRC_COLOR (colour inversion).
    /// </summary>
    protected virtual void RenderCrosshair(int screenW, int screenH)
    {
        DrawTexturedModalRect("gui/icons.png",
            screenW / 2 - 7, screenH / 2 - 7,
            CrosshairU, CrosshairV, CrosshairSize, CrosshairSize);
    }

    // ── Draw primitive (stub — actual GL pending) ─────────────────────────────

    /// <summary>
    /// Replica of <c>ht.b(x, y, u, v, w, h)</c> — drawTexturedModalRect.
    /// Stub: no-op until the 2D rendering pass is wired.
    /// </summary>
    protected virtual void DrawTexturedModalRect(string texture,
        int screenX, int screenY, int texU, int texV, int width, int height)
    {
        // GL draw deferred — wired when TextureRegistry is accessible from GUI layer
    }
}
