# Spec: EntityRenderer (First-Person View)

**Java class:** `adt`
**Status:** PROVIDED — PARTIAL
**Canonical name:** EntityRenderer

---

## Overview

`adt` manages first-person perspective rendering: the camera projection matrix, FOV calculation,
view bobbing, and the rendering of the player's held item (hand). It does NOT render world
geometry — that is `afv` (WorldRenderer).

---

## Class Structure (`adt`)

### Key fields

| Field | Type | Meaning |
|---|---|---|
| `r` | `Minecraft` | Back-reference to game context |
| `c` | `n` | ItemRenderer / hand renderer instance |
| `B` | `float` = 4.0F | FOV-related constant (base modifier) |
| `C` | `float` = 4.0F | FOV-related constant (current modifier) |

Multiple `ne` instances (float animation state trackers) for view bobbing and animation effects.

### Dependencies

- Uses **LWJGL** display and GL bindings directly.
- Uses **GLU** for `gluPerspective` (projection matrix setup).
- Holds a `FloatBuffer` for GL matrix operations.

---

## Responsibilities

### 1. Projection matrix

`adt` calls `gluPerspective(fov, aspect, nearPlane, farPlane)` each frame.

**FOV computation:**
- Base FOV: 70° (normal) or 110° (sprint) — exact values TBD.
- Modified by: potion effects, zoom (if any), spyglass (1.0 has no spyglass — N/A).
- `B`/`C` fields animate FOV transitions smoothly (lerped over frames).

**Near/far planes:** near ~0.05, far ~512 (render distance dependent).

### 2. Camera positioning

Camera placed at:
```
player.x, player.y + player.eyeHeight, player.z
```
where `player.L = 1.62F` (see `GameMode_Spec.md`).

Yaw and pitch applied via GL rotations (not a matrix multiply — immediate-mode GL).

### 3. View bobbing

Animation state `ne` instances track:
- Walking bobAmount (increases with horizontal speed)
- Strafe bob
- Camera tilt

Applied as a GL rotation/translation before scene render.

### 4. Hand / item rendering

Delegates to `c` (instance of `n` — ItemRenderer):

```java
c.a(EntityLiving entity, ItemStack heldItem, int pass)
```

The hand is rendered in a separate GL state after the world geometry, with depth cleared
to always appear on top of world geometry.

---

## ItemRenderer / Hand Renderer (`n`)

**Java class:** `n`

### Fields

| Field | Type | Meaning |
|---|---|---|
| `a` | `Minecraft` | Game context |
| `b` | `dk` | Currently displayed ItemStack (for animation lerp) |
| `e` | `acr` | RenderBlocks instance (for block-item rendering) |
| `f` | `sg` | (Unknown — possibly texture manager ref) |

### Method

```java
n.a(nq entity, dk heldItem, int renderPass)
```

Renders the item held in the player's main hand in first-person view.
- Block items: renders a 3D block model using `acr` (RenderBlocks).
- Tool/weapon items: renders the 2D item sprite rotated to appear 3D.
- No item: renders the fist/hand model.

---

## Render Passes

`adt` orchestrates the following rendering passes each frame:

1. **Sky** — sky dome, sun, moon, stars.
2. **World** — delegates to `afv` (WorldRenderer) for chunk geometry + entities.
3. **Weather** — rain/snow particles.
4. **Hand** — delegates to `n` (ItemRenderer).
5. **HUD** — delegates to `qd` (GuiIngame).
6. **GUI** — if a screen is open, delegates to `xe` subclass.

---

## C# Mapping

| Java | C# |
|---|---|
| `adt` | `EntityRenderer` (first-person view controller) |
| `n` | `ItemRenderer` |
| `adt.r` | `EntityRenderer.Minecraft` |
| `adt.c` | `EntityRenderer.ItemRenderer` |
| `adt.B/C` | `EntityRenderer.FovModifier` (animated) |
| `gluPerspective` | `Matrix4x4.CreatePerspectiveFieldOfView` or Raylib `SetCameraFovy` |
| View bobbing `ne` | `EntityRenderer.BobAnimation : AnimatedFloat` |

---

## Open Questions

- Exact FOV base values (70°, quake-pro 110°?).
- Full list of `ne` (animated float) fields and their purposes.
- Whether `adt` also manages third-person camera or a separate class handles that.
- How motion blur / drunk effect (Nausea potion) is applied — likely a GL screen warp.
- Exact near/far clip plane values.
