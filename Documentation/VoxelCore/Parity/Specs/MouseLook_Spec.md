# Spec: MouseLook / Camera Rotation

**Java class:** `adt` (EntityRenderer)
**Supporting class:** `hs` (MouseHelper)
**Status:** PROVIDED
**Canonical name:** EntityRenderer / MouseHelper

---

## Mouse Delta Source

`hs` wraps LWJGL mouse input:

```java
public class hs {
    public int a;    // getDX result
    public int b;    // getDY result

    public void c() {
        a = Mouse.getDX();
        b = Mouse.getDY();
    }
    public void a() { Mouse.setGrabbed(true); a = 0; b = 0; }
    public void b() { Mouse.setGrabbed(false); ... }
}
```

Held as `Minecraft.D` (type `hs`). Populated once per render frame — not per tick.

---

## Full Mouse-to-Rotation Pipeline

Located in `adt.b(float partialTick)` (the main render+input method), called each frame.
Guarded by `this.r.R` (mouse is grabbed / no GUI open).

```java
// adt.java lines 581–603
pf.a("mouse");
if (this.r.R) {
    this.r.D.c();                               // read raw deltas into D.a, D.b

    float var2 = this.r.A.c * 0.6F + 0.2F;     // A.c = ki.c = mouseSensitivity (0.0–1.0)
    float var3 = var2 * var2 * var2 * 8.0F;     // sensitivity factor

    float var4 = (float)this.r.D.a * var3;      // yaw delta (scaled)
    float var5 = (float)this.r.D.b * var3;      // pitch delta (scaled)

    byte var6 = 1;
    if (this.r.A.d) { var6 = -1; }              // A.d = invertMouse

    if (this.r.A.I) {                           // smooth camera mode
        this.H += var4;
        this.I += var5;
        float var7 = var1 - this.L;
        this.L = var1;
        var4 = this.J * var7;                   // use smoothed J/K instead
        var5 = this.K * var7;
        this.r.h.c(var4, var5 * (float)var6);
    } else {
        this.r.h.c(var4, var5 * (float)var6);  // direct (non-smooth)
    }
}
```

`this.r.h` = `EntityPlayerSP` (`di`). `c(yaw, pitch)` is defined on `ia` (Entity base).

---

## Sensitivity Formula

```
sensitivityFactor = (mouseSensitivity * 0.6 + 0.2)^3 * 8.0
```

`mouseSensitivity` = `ki.c` = 0.0–1.0 (from GameSettings slider).

| Setting | Factor |
|---|---|
| 0.0 | `0.2^3 * 8 = 0.0064` |
| 0.5 | `0.5^3 * 8 = 1.0` |
| 1.0 | `0.8^3 * 8 = 4.096` |

---

## Turn Method (`ia.c`)

```java
// ia.java lines 142–157
public void c(float yawDelta, float pitchDelta) {
    float prevPitch = this.z;
    float prevYaw   = this.y;
    this.y = (float)((double)this.y + (double)yawDelta * 0.15);
    this.z = (float)((double)this.z - (double)pitchDelta * 0.15);
    if (this.z < -90.0F) { this.z = -90.0F; }
    if (this.z >  90.0F) { this.z =  90.0F; }
    this.B = this.B + (this.z - prevPitch);   // yaw delta accumulator
    this.A = this.A + (this.y - prevYaw);     // pitch delta accumulator
}
```

Final per-frame delta to entity angles:

```
yaw   += (D.a * sensitivityFactor) * 0.15
pitch -= (D.b * sensitivityFactor * invertSign) * 0.15
```

`pitch` clamped to `[-90, 90]`. Yaw is unbounded (wraps naturally via trig).

---

## Smooth Camera Path

When `A.I` (smoothCamera) is enabled:

```
Each frame:
  H += rawYawDelta
  I += rawPitchDelta

Each tick (adt.a()):
  J = lerp(H, targetYaw,   0.05 * sensitivityFactor)   // via ne.a()
  K = lerp(I, targetPitch, 0.05 * sensitivityFactor)
  H = 0
  I = 0
```

`J`/`K` are the smoothed yaw/pitch values. The render frame then reads `J * deltaTime` and `K * deltaTime` instead of the raw deltas.

`ne` is a simple exponential smoother: `a(value, factor)` = lerp toward value by factor.

---

## InvertMouse

`ki.d` (GameSettings.invertMouse). When `true`, `var6 = -1` — pitch delta sign flips.

---

## Fields Summary (`adt`)

| Field | Type | Purpose |
|---|---|---|
| `H` | `float` | raw yaw delta accumulator (smooth mode) |
| `I` | `float` | raw pitch delta accumulator (smooth mode) |
| `J` | `float` | smoothed yaw output |
| `K` | `float` | smoothed pitch output |
| `L` | `float` | last partialTick used for smooth time delta |
| `v` / `w` | `ne` | exponential smoothers for J and K |

---

## C# Mapping

| Java | C# |
|---|---|
| `hs` | `MouseHelper` |
| `hs.a` / `hs.b` | `MouseHelper.DeltaX` / `DeltaY` |
| `hs.c()` | `MouseHelper.ReadDelta()` |
| `Minecraft.D` | `Engine.MouseHelper` |
| `ki.c` | `GameSettings.MouseSensitivity` (0–1) |
| `ki.d` | `GameSettings.InvertMouse` |
| `ki.I` | `GameSettings.SmoothCamera` |
| `ia.c(yaw, pitch)` | `Entity.Turn(yaw, pitch)` |
| `ia.y` | `Entity.Yaw` |
| `ia.z` | `Entity.Pitch` |
