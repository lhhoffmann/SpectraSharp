# Spec: GuiIngame HUD Texture Layout

**Java class:** `qd extends ht`
**Status:** PROVIDED
**Canonical name:** GuiIngame

---

## Screen Coordinate System

Coordinates use scaled screen space via `ef` (ScaledResolution):

```java
ef var5 = new ef(this.g.A, this.g.d, this.g.e);  // (settings, screenW, screenH)
int screenW = var5.a();   // scaled width
int screenH = var5.b();   // scaled height
```

`ef` computes a scale factor from the game window size and the GUI scale setting.
All HUD positions are in scaled coordinates, not physical pixels.

The draw primitive is inherited from `ht`:
```java
b(int screenX, int screenY, int texU, int texV, int width, int height)
// = drawTexturedModalRect(x, y, u, v, width, height)
```

---

## Textures

Two texture files drive the entire HUD:

| File | Content |
|---|---|
| `/gui/gui.png` | Hotbar strip, hotbar selection |
| `/gui/icons.png` | All status icons (crosshair, hearts, food, armor, bubbles, XP bar) |

Binding is done with `GL11.glBindTexture(3553, textureManager.b(path))`.

---

## `/gui/gui.png` — Hotbar

### Hotbar strip

```
UV: (0, 0)   size: 182 × 22
Screen: (screenW/2 - 91,  screenH - 22)
```

The strip is centred horizontally and flush with the bottom edge.

### Hotbar selection highlight

```
UV: (0, 22)   size: 24 × 22
Screen: (screenW/2 - 91 - 1 + slot * 20,  screenH - 22 - 1)
```

`slot` = active hotbar slot index (0–8). The selection box is 1px left and 1px above
the strip to accommodate its 2px border.

### Hotbar item positions (for item rendering)

```
for slot = 0..8:
    itemX = screenW/2 - 90 + slot * 20 + 2
    itemY = screenH - 16 - 3   (= screenH - 19)
```

Items are rendered using the item renderer, not drawTexturedModalRect.

---

## `/gui/icons.png` — Status Icons Atlas

All status icons come from a single 256×256 atlas.

### Crosshair

```
UV: (0, 0)   size: 16 × 16
Screen: (screenW/2 - 7,  screenH/2 - 7)
```

Blending: `GL_ONE_MINUS_DST_COLOR` × `GL_ONE_MINUS_SRC_COLOR` (inverts background).

---

### XP Bar

```
Background:
  UV: (0, 64)   size: 182 × 5
  Screen: (screenW/2 - 91,  screenH - 29)

Fill (proportional):
  UV: (0, 69)   size: xpPixels × 5
  Screen: (screenW/2 - 91,  screenH - 29)
```

`xpPixels = floor(h.cf * 183)` where `cf` = XP progress 0.0–1.0.
The fill strip is drawn on top of the background; its width is 0–182 pixels.
Only drawn when player has XP bar (`c.a()` = isInSurvival or similar condition).

---

### Health Bar (10 heart icons, left side)

Row Y: `screenH - 39`
Positions: `screenW/2 - 91 + i*8` for i = 0..9 (left to right)

| Icon | UV | Condition |
|---|---|---|
| Heart background (normal) | (16, 0) 9×9 | always drawn |
| Heart background (flashing) | (25, 0) 9×9 | `var11` = hp<5 and frame parity |
| Full heart (normal) | (52, 0) 9×9 | `hp > i*2+1` |
| Half heart (normal) | (61, 0) 9×9 | `hp == i*2+1` |
| Full heart (Wither/Poison) | (88, 0) 9×9 | same but `var85=52` (alternate row) |
| Half heart (Wither/Poison) | (97, 0) 9×9 | same |
| Hardcore variant | same UV, y*9=45 | `var30=5` when `si.s()=true` (hardcore) |

`var85 = 16` normally; `var85 += 36` (→ 52) when player has a status effect that
changes heart colour (Wither/Poison — `h.a(abg.u)` condition).

When `hp <= 4` (2 hearts or less), each heart gets a random ±1 Y-jitter per frame.

---

### Armor Bar (10 icon slots, left side, above health)

Row Y: `screenH - 49` (= health row − 10)
Positions: `screenW/2 - 91 + i*8` for i = 0..9 (left to right)

| Icon | UV | Condition |
|---|---|---|
| Empty armor slot | (16, 9) 9×9 | `armor <= i*2` |
| Half armor | (25, 9) 9×9 | `armor == i*2+1` |
| Full armor | (34, 9) 9×9 | `armor > i*2+1` |

`armor = h.p()` = total armor points (0–20). Only drawn when `armor > 0`.

---

### Food Bar (10 icons, right side)

Row Y: `screenH - 39` (same row as health, but drawn right to left)
Positions: `screenW/2 + 91 - i*8 - 9` for i = 0..9 (right to left)

| Icon | UV | Condition |
|---|---|---|
| Food background (normal) | (16, 27) 9×9 | always drawn |
| Food background (jitter when starving) | (16, 27) then shifted | when `aO().d() <= 0` |
| Food background (hungry variant) | (133, 27) 9×9 | `h.a(abg.s)` = starving/hungry |
| Full food | (52, 27) 9×9 | `food > i*2+1` |
| Half food | (61, 27) 9×9 | `food == i*2+1` |
| Hungry full | (88, 27) 9×9 | hungry variant (var88=52) |
| Hungry half | (97, 27) 9×9 | hungry variant |

`food = h.aO().b()` = food level (0–20). `h.aO()` = FoodStats.

When starving (`h.aO().d() <= 0`): food bar jitters randomly.

---

### Air Bubbles (when underwater)

Row Y: `screenH - 49` (same as armor row, shown instead of armor when in water)
Positions: `screenW/2 + 91 - i*8 - 9` for i = 0..N (right to left)

| Icon | UV | Condition |
|---|---|---|
| Full bubble | (25, 18) 9×9 | remaining air > threshold |
| Empty bubble | (16, 18) 9×9 | depleted bubble slot |

`h.Z()` = current air supply (300 = full). Bubble count calculated from air supply.
Only shown when `h.a(p.g)` = player is in water/drowning condition.

---

## Vertical Layout Summary

```
screenH - 22   Hotbar strip (22px tall)
screenH - 29   XP bar (5px tall)
screenH - 39   Health row (left) / Food row (right)
screenH - 49   Armor row (left) / Air bubbles row (right)
screenH/2 - 7  Crosshair (centred)
```

---

## HUD Visibility

The HUD render is only executed when `!A.D` (F1 not toggled off) and when not in Creative
purely for XP/food (those bars are skipped in Creative via `c.a()` check).

---

## Chat / Title Rendering

### Chat messages

```java
// At y offset from screenH - 48, drawn upward
for message in e:
    drawString(message.text, 2, -(i * 9), colour + alphaFade)
```

Up to 10 messages (20 if chat screen open). Y-offset: `-(i * 9)` from baseline.

### Action bar / title (centred)

```java
if (k > 0) {
    float fade = k - partialTick;
    int alpha = clamp(fade * 256 / 20, 0, 255);
    fontRenderer.b(text, screenW/2 - textWidth/2, screenH - 48 - 4, colour | (alpha << 24));
}
```

---

## `drawTexturedModalRect` GL State

Caller's responsibility:
- Bind correct texture before calling `b(...)`.
- Enable `GL_BLEND` with `GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA` for normal icons.
- Crosshair uses special blend `GL_ONE_MINUS_DST_COLOR, GL_ONE_MINUS_SRC_COLOR`.
- Depth test (`GL_DEPTH_TEST`) must be disabled for HUD pass.

---

## C# Mapping

| Java | C# |
|---|---|
| `qd` | `GuiIngame` |
| `qd.a(float, bool, int, int)` | `GuiIngame.RenderGameOverlay(partialTicks, hasChatOpen, mouseX, mouseY)` |
| `ef` | `ScaledResolution` |
| `ef.a()` / `ef.b()` | `ScaledWidth` / `ScaledHeight` |
| `ht.b(x,y,u,v,w,h)` | `DrawTexturedModalRect(x,y,u,v,w,h)` |
| `/gui/gui.png` | `gui/gui.png` from JAR |
| `/gui/icons.png` | `gui/icons.png` from JAR |
