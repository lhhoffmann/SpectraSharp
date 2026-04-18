# Spec: GuiInventory — Player Inventory Screen

**Java class:** `hw extends mg`
**Container class:** `gd extends pj`
**Status:** PROVIDED
**Canonical name:** GuiInventory / ContainerPlayer

---

## Screen Setup

`hw` extends `mg` (GuiContainer), which extends `xe` (GuiScreen):

```java
// mg fields
protected int b = 176;   // container width
protected int c = 166;   // container height

// Center on screen
this.e = (screenW - b) / 2;   // leftX
this.f = (screenH - c) / 2;   // topY
```

All slot coordinates below are **relative** to `(e, f)` — the container's top-left corner.

---

## Texture

`/gui/inventory.png` — single texture file drives the entire inventory UI.

Background drawn at container origin:
```java
this.b(var5, var6, 0, 0, this.b, this.c);   // screen(e,f), UV(0,0), size(176,166)
```

---

## Slot Layout (Container `gd`)

All slot positions are `(x, y)` relative to container top-left.

### Crafting Output (slot 0)

```
Position: (144, 36)
```
`afe` result slot — only accepts completed craft output.

### Crafting Grid 2×2 (slots 1–4)

```
for row = 0..1:
  for col = 0..1:
    slot index = col + row*2 + 1
    x = 88 + col * 18
    y = 26 + row * 18
```

| Slot | Position |
|---|---|
| 1 (top-left) | (88, 26) |
| 2 (top-right) | (106, 26) |
| 3 (bot-left) | (88, 44) |
| 4 (bot-right) | (106, 44) |

### Armor Slots (slots 5–8)

```
for armorSlot = 0..3:
    slot index = 5 + armorSlot
    x = 8
    y = 8 + armorSlot * 18
```

| Slot | Piece | Position |
|---|---|---|
| 5 | Helmet (slot 0) | (8, 8) |
| 6 | Chestplate (slot 1) | (8, 26) |
| 7 | Leggings (slot 2) | (8, 44) |
| 8 | Boots (slot 3) | (8, 62) |

Armor slot index `armorSlot` corresponds to `inventory.c() - 1 - armorSlot` in the player inventory.

### Main Inventory (slots 9–35)

```
for row = 0..2:
  for col = 0..8:
    slot index = col + (row+1)*9
    x = 8 + col * 18
    y = 84 + row * 18
```

3 rows × 9 columns, starting at (8, 84). Row 0 = top row.

### Hotbar (slots 36–44)

```
for col = 0..8:
    slot index = col + 36
    x = 8 + col * 18
    y = 142
```

---

## Player Model Render

The player's character model is rendered inside the inventory screen:

```java
// Container-local position:
x = e + 51
y = f + 75
scale = 30.0F

// Look direction follows mouse:
yawOffset   = atan(mouseOffsetX / 40.0) * 40°
pitchOffset = atan(mouseOffsetY / 40.0) * 20°
```

The model uses `wb.a.a(player, ...)` — `RenderPlayer`.

---

## Sub-Screens Opened From Inventory

| Button | Opens |
|---|---|
| Slot `f == 0` | `qx` (Creative inventory or similar) |
| Slot `f == 1` | `in` (survival crafting 2×2 or recipe book) |

---

## Active Effects Sidebar

When the player has active potion effects (`h.au()` non-empty), an effects sidebar is drawn
to the left of the inventory:

```java
// Sidebar drawn at:
x = e - 124
y = f      (top of container)

// Per effect:
this.b(x, y, 0, this.c, 140, 32);  // UV(0, 166) effect row
this.b(x+6, y+7, 0+iconU, this.c+32+iconV, 18, 18);  // potion icon
// Icon U/V from effect.e() / 8*18 atlas mapping
y += 33 (or compressed if many effects)
```

---

## C# Mapping

| Java | C# |
|---|---|
| `hw` | `GuiInventory` |
| `mg` | `GuiContainer` |
| `gd` | `ContainerPlayer` |
| `pj` | `Container` |
| `vv` | `Slot` |
| `vv.e` / `vv.f` | `Slot.X` / `Slot.Y` |
| `mg.e` / `mg.f` | `GuiContainer.LeftX` / `TopY` |
| `mg.b` / `mg.c` | `GuiContainer.Width(176)` / `Height(166)` |
| `/gui/inventory.png` | `gui/inventory.png` from JAR |
