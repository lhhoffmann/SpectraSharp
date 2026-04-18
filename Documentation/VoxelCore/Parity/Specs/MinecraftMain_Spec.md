# Spec: Minecraft Main Class and Input Loop

**Java class:** `net.minecraft.client.Minecraft` (abstract; keeps full package path in decompile)
**Concrete subclass:** `ahy` (the actual runnable Minecraft implementation)
**Status:** PROVIDED
**Canonical name:** Minecraft (singleton)

---

## Class Structure

```java
public abstract class Minecraft implements Runnable {
    public static Minecraft a;   // singleton instance
    ...
}
```

The concrete class `ahy extends Minecraft` is instantiated in `Minecraft.a(String, String, String)`
(the static entry point). `ahy` is the class run on the "Minecraft main thread".

---

## Key Fields

| Field | Type | Canonical name |
|---|---|---|
| `a` | `static Minecraft` | singleton |
| `c` | `aes` | ItemInWorldManager (GameMode) |
| `f` | `ry` | theWorld |
| `g` | `afv` | renderGlobal (RenderGlobal / WorldRenderer) |
| `h` | `di` | thePlayer (EntityPlayerSP) |
| `s` | `xe` | currentScreen (null during gameplay) |
| `u` | `adt` | entityRenderer (EntityRenderer) |
| `w` | `qd` | ingameGUI (GuiIngame HUD) |
| `p` | `zh` | textureManager |
| `q` | `abe` | fontRenderer (default, `/font/default.png`) |
| `r` | `abe` | fontRendererAlt (alternate, `/font/alternate.png`) |
| `C` | `ahm` | soundManager |
| `A` | `ki` | gameSettings |
| `z` | `gv` | objectMouseOver (MovingObjectPosition / RaycastResult) |
| `j` | particle system | (type: particle manager) |
| `X` | `aij` | timer (20.0F Hz) |
| `d`, `e` | `int` | display width/height |

---

## Timer (`aij`)

```java
private aij X = new aij(20.0F);
```

| Field | Type | Purpose |
|---|---|---|
| `b` | `int` | elapsed ticks since last frame (usually 0 or 1, 2+ if lagging) |
| `c` | `float` | renderPartialTicks (0.0–1.0, for smooth interpolation) |

`X.a()` is called once per frame to advance the timer. It calculates elapsed real time,
divides by tick length (50 ms), stores integer part in `b`, fractional part in `c`.

---

## Game Loop (`run()` → `x()`)

**Single-threaded** — game ticks and render frames run on the same thread.

```java
// In run():
while (running) {
    x();          // one frame
}

// In x():
X.a();            // advance timer (sets X.b and X.c)

for (int i = 0; i < X.b; i++) {
    k();          // one game tick (20 Hz logic)
}

// Render (uncapped, uses X.c for interpolation)
u.b(X.c);         // EntityRenderer.updateCameraAndRender(partialTicks)
```

**Order per frame:** advance timer → N game ticks → render.
Catchup: if frame was slow, `X.b` can be 2+ to run multiple ticks.

---

## Game Tick (`k()`)

One invocation of `k()` = one 20 Hz game tick:

1. Profiling markers
2. `al++; if (al == 30) { al = 0; f.g(h); }` — keep player chunks loaded
3. `z = u.b(world, player)` — raycast (object mouse over)
4. Input processing (see below)
5. ItemInWorldManager tick: `c.d(h)` → update break progress
6. Texture animation tick

---

## Mouse Input (`a(int button, boolean pressed)`)

```java
// Left click (button 0):
if (pressed) {
    if (z != null) c.c(z.b, z.c, z.d, z.e);   // damage block
} else {
    c.b();    // reset block damage
}

// Right click (button 1):
if (pressed && z != null) {
    if (z is entity) c.a(player, entity);       // interact with entity
    else c.a(player, world, heldItem, z.b, z.c, z.d, z.e);  // use/place
}
```

`z.b/c/d` = hit X/Y/Z; `z.e` = hit face.

---

## Keyboard Input

Key codes are LWJGL constants.

| LWJGL key | Action |
|---|---|
| 59 (F1) | Toggle HUD: `A.D = !A.D` |
| 60 (F2) | Screenshot |
| 61 (F3) | Toggle debug overlay: `A.F = !A.F` |
| 63 (F5) | Cycle view mode 0→1→2→0: `A.E = (A.E+1)%3` |
| 66 (F8) | Toggle smooth camera: `A.I = !A.I` |
| 2–10 | Hotbar slots 0–8 (`h.by.c = i`) |
| In Creative: 11 = slot 0, 2–10 = slots 1–9 |

Other keybindings are stored in `ki` (GameSettings) as `lp` (KeyBinding) objects
and polled via `A.s.c()` (attack), `A.t.c()` (use), `A.u.c()` (jump), etc.

---

## `currentScreen` (`s`) Lifecycle

- `s == null` during gameplay (mouse captured, input to game)
- `s != null` when a GUI is open (mouse free, input to GUI)
- Opening a screen: `a(new SomeGuiScreen())` sets `s = screen`, releases mouse
- Closing a screen: `a(null)` clears `s`, re-captures mouse
- ESC during gameplay: `i()` → `a(new nd())` (opens pause/options menu)

---

## Right-Click End-to-End

```
Mouse button 1 pressed
→ Minecraft.a(1, true)
→ z (MovingObjectPosition) is block target
→ c.a(h, f, h.by.e(3), z.b, z.c, z.d, z.e)   // ItemInWorldManager.a
→ ItemBlock.onItemUse(player, world, x, y, z, face)
→ world.b(x, y, z, blockId)   // setBlock
```

---

## Left-Click Break End-to-End

```
Mouse button 0 pressed
→ Minecraft.a(0, true)
→ c.c(z.b, z.c, z.d, z.e)      // ItemInWorldManager.c = damageBlock per tick
→ dm.f += block.a(player)
→ at f >= 1.0: aes.a(x,y,z,face) breaks block
```

---

## World Load / Dimension Switch

### `a(int dim)` — dimension switch

```java
public void a(int dim) {
    // dim == -1: enter Nether (scale ×0.125 Overworld→Nether)
    // dim == 0:  leave Nether/End (scale ×8 Nether→Overworld)
    // dim == 1:  enter End (find end spawn via world.j())
    // create new ry, teleport player, load spawn
}
```

Overworld↔Nether coordinate scale: ×8 Nether→Overworld, ×0.125 Overworld→Nether.

### `a(ry, String, vi)` — load world

Sets `f = newWorld`, creates player via `c.b(world)`, centers chunk cache,
calls `c.d(player)` to initialise ItemInWorldManager with new player.

### `a(boolean, int, boolean)` — respawn

Creates fresh `di` via `c.b(world)`, optionally copies stats from old player (`h.d(old)`),
calls `e("Respawning")` to preload spawn chunks.

---

## Static Utility Methods

```java
static boolean r()   // !A.D → should render HUD (A.D = true means no HUD)
static boolean s()   // A.j = isSoundEnabled
static boolean t()   // A.k = isMusicEnabled
static boolean u()   // A.F = isDebugOverlay
```

---

## Version String

```java
var8.a("Minecraft 1.0.0 (" + this.M + ")", 2, 2, 16777215);
```

`M` = build string (e.g. git hash / build date). The version is hardcoded "Minecraft 1.0.0".

---

## C# Mapping

| Java | C# |
|---|---|
| `Minecraft.a` | `Engine.Instance` (static) |
| `Minecraft.c` | `Engine.GameMode` |
| `Minecraft.f` | `Engine.World` |
| `Minecraft.h` | `Engine.Player` |
| `Minecraft.s` | `Engine.CurrentScreen` |
| `Minecraft.X` (aij) | `Engine.Timer` |
| `aij.b` | `Timer.ElapsedTicks` |
| `aij.c` | `Timer.RenderPartialTicks` |
| `Minecraft.k()` | `Engine.FixedUpdate()` |
| `Minecraft.x()` | `Engine.GameLoop()` / frame method |
| `Minecraft.a(int,bool)` | `Engine.OnMouseButton(button, pressed)` |
| `Minecraft.a(int)` | `Engine.TravelToDimension(dim)` |
| `Minecraft.e(string)` | `Engine.PreloadSpawnChunks(label)` |
| `Minecraft.a(xe)` | `Engine.OpenScreen(screen)` |
