# Spec: GuiScreen (GUI Base Class)

**Java classes:** `xe` (GuiScreen base), `qd` (GuiIngame / HUD)
**Status:** PROVIDED
**Canonical name:** GuiScreen / GuiIngame

---

## Overview

`xe` is the abstract base class for all GUI screens in 1.0. It extends `ht` (base drawable).
Screens own a list of `ct` (GuiButton) objects and receive keyboard and mouse input dispatched
from the game loop.

`qd` extends `ht` (not `xe`) and is the always-visible HUD rendered during gameplay.

---

## GuiScreen Base (`xe`)

### Fields

| Field | Type | Meaning |
|---|---|---|
| `l` | `Minecraft` instance | Back-reference to game context |
| `m` | `int` | Screen width (pixels, scaled) |
| `n` | `int` | Screen height (pixels, scaled) |
| `o` | `List<ct>` | Registered buttons |

### Lifecycle

```
initGui()    → called once when screen opens; subclasses add buttons to `o`
drawScreen() → called every render frame
onGuiClosed()→ called when screen is dismissed
```

### Draw — `a(int mouseX, int mouseY, float partialTick)`

Iterates `o` and calls each button's draw method.
Subclasses override and call `super.a(...)` for button rendering.

### Keyboard — `a(char typedChar, int keyCode)`

- **ESC (keyCode 1):** closes the screen, returns to game.
- Subclasses override to intercept text input before calling `super.a(...)`.

### Mouse clicked — `a(int mouseX, int mouseY, int button)`

Iterates `o`; for each button that is enabled and contains `(mouseX, mouseY)`:
- Plays `"random.click"` sound.
- Calls `actionPerformed(button)`.

### Mouse released — `b(int mouseX, int mouseY, int button)`

Default: no-op. Subclasses override for drag/release handling.

### Button registration

Subclasses call `this.o.add(new ct(...))` in `initGui()`.

---

## GuiButton (`ct`)

| Field | Meaning |
|---|---|
| `id` (int) | Identifier passed to `actionPerformed` |
| `x`, `y` | Top-left position |
| `width`, `height` | Size (default 200×20) |
| `enabled` | Whether clickable |
| `visible` | Whether drawn |
| `displayString` | Label text |

Collision check: `mouseX ∈ [x, x+width)` and `mouseY ∈ [y, y+height)`.

---

## GuiIngame / HUD (`qd`)

`qd extends ht` — not a GuiScreen. Rendered every frame as an overlay during gameplay.

### Known elements

| Element | Condition |
|---|---|
| Sky-colour blend overlay | Sky visible |
| Pumpkin head overlay | Wearing pumpkin (`yy.ba`) as helmet |
| Hotbar | Always |
| Health / food / armour / air bars | Based on player state |
| Crosshair | When not in third person |
| Chat | When messages present |

### Scale factor (`ef`)

`qd` uses `ef` to compute the scaled resolution (`m`/`n` fields). Scale factor is the largest
integer N such that `windowWidth / N ≥ 320` and `windowHeight / N ≥ 240`, capped at a maximum
scale of 4 (or "auto" setting). This produces a pixelated 2× or 3× scaled GUI.

---

## C# Mapping

| Java | C# |
|---|---|
| `xe` | `GuiScreen` |
| `ct` | `GuiButton` |
| `qd` | `GuiIngame` |
| `ht` | `GuiBase` (drawable base) |
| `xe.l` | `GuiScreen.Minecraft` |
| `xe.m/n` | `GuiScreen.Width/Height` |
| `xe.o` | `GuiScreen.Buttons : List<GuiButton>` |
| `xe.a(char, int)` | `GuiScreen.KeyTyped(char c, int keyCode)` |
| `xe.a(int, int, int)` | `GuiScreen.MouseClicked(int x, int y, int button)` |
| `xe.a(int, int, float)` | `GuiScreen.DrawScreen(int mouseX, int mouseY, float partial)` |
| `xe.b(int, int, int)` | `GuiScreen.MouseReleased(int x, int y, int button)` |
| ESC keyCode | `KeyCode.Escape == 1` (LWJGL / keyboard constant) |

---

## Open Questions

- Which specific subclasses of `xe` exist (inventory screen, pause menu, chat, etc.) — not read.
- `qd` full element list beyond pumpkin/hotbar — only partially read.
- `ef` exact scale-factor algorithm (observed pattern but not confirmed from source).
- Whether `qd` has its own button list or uses a different input mechanism.
