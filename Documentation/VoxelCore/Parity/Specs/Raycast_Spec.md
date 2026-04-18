# Spec: Raycast / MovingObjectPosition

**Java class:** `adt` (EntityRenderer) — raycast entry point
**Result class:** `gv` (MovingObjectPosition)
**Status:** PROVIDED
**Canonical name:** EntityRenderer.UpdateObjectMouseOver / RaycastResult

---

## Raycast Call Site

Located in `adt.a(float partialTick)`, called each render frame.
Result stored in `Minecraft.z` (`objectMouseOver`).

```java
// adt.java lines 111–166
public void a(float var1) {
    if (this.r.i != null && this.r.f != null) {
        double var2 = (double)this.r.c.c();    // reach distance from ItemInWorldManager
        this.r.z = this.r.i.a(var2, var1);     // entity.raycastBlock(reach, partialTick)

        double var4 = var2;
        fb var6 = this.r.i.e(var1);            // eye position (interpolated)

        if (this.r.z != null) {
            var4 = this.r.z.f.d(var6);         // distance to block hit
        }

        if (this.r.c.h()) {    // isCreative
            var2 = 6.0;
            var4 = 6.0;
        } else {
            if (var4 > 3.0) { var4 = 3.0; }   // cap entity search radius at 3.0
            var2 = var4;
        }

        // Entity hit check (within var2 = entity reach)
        fb var7 = this.r.i.f(var1);            // look vector (normalized)
        fb var8 = var6.c(var7.a * var2, var7.b * var2, var7.c * var2);   // end point

        this.u = null;
        float var9 = 1.0F;
        List var10 = this.r.f.b(this.r.i,     // entities in AABB
            this.r.i.C.a(var7 * var2).b(var9, var9, var9));

        double var11 = 0.0;
        for (entity in var10) {
            if (entity.e_()) {    // canBeCollided
                gv hit = entity.boundingBox.expand(Q).intersect(eyePos, endPos);
                if (boundingBox.contains(eyePos)) {
                    if (0.0 < var11 || var11 == 0.0) { nearest = entity; dist = 0.0; }
                } else if (hit != null) {
                    double d = eyePos.distanceTo(hit.f);
                    if (d < var11 || var11 == 0.0) { nearest = entity; dist = d; }
                }
            }
        }
        if (nearest != null) { this.r.z = new gv(nearest); }
    }
}
```

---

## Reach Distances

| Mode | Block Reach | Entity Reach |
|---|---|---|
| Survival | `dm.c()` = **4.0F** | min(blockHitDist, **3.0F**) |
| Creative | **6.0F** | **6.0F** |

In survival, entities are only searched up to the block hit distance capped at 3.0.
In creative, both block and entity reach are hard-set to 6.0.

---

## Direction Vectors

```java
fb eyePos  = entity.e(partialTick);   // eye position (interpolated)
fb lookVec = entity.f(partialTick);   // normalized look direction
```

`entity.f(partialTick)` returns a unit vector computed from interpolated yaw/pitch:

```java
// Standard look-vector formula (from entity interpolated angles):
float pitchRad = pitch * PI/180
float yawRad   = yaw   * PI/180
x = -sin(yaw)  * cos(pitch)
y = -sin(pitch)
z =  cos(yaw)  * cos(pitch)
```

---

## `gv` (MovingObjectPosition) Structure

```java
// Block hit:
gv.b = hitX     (int)
gv.c = hitY     (int)
gv.d = hitZ     (int)
gv.e = hitFace  (int, 0–5)
gv.f = fb       (exact hit vector, double precision)
gv.a = entity   (null for block hits)

// Entity hit:
gv.a = entity   (ia)
gv.f = hit point on entity bounding box
```

Face encoding (same as block faces throughout the engine):
```
0 = bottom (-Y)
1 = top    (+Y)
2 = north  (-Z)
3 = south  (+Z)
4 = west   (-X)
5 = east   (+X)
```

---

## Block Raycast (`entity.a(reach, partialTick)`)

The block raycast is performed by `ia.a(double reach, float partialTick)`.
It steps through blocks along the ray using an AABB–plane intersection approach
(vanilla DDA / block-face iteration). Returns the first solid block face hit within `reach`.

---

## Entity Expansion

Each entity's AABB is expanded by `Q()` before intersection test:

```java
float var15 = var14.Q();   // entity.getCollisionBorderSize()
c var16 = var14.C.b(var15, var15, var15);   // expand AABB by var15 on all sides
```

`Q()` returns a per-entity border size (usually 0.0 for most entities; 0.3 for some mobs).

---

## Accessing the Result

`Minecraft.z` (`objectMouseOver`) is read by:
- `Minecraft.k()` — input handling (click on block/entity)
- `qd` (GuiIngame) — HUD crosshair / block breaking progress overlay

Fields accessed at use sites:
```java
z.b, z.c, z.d   // block X/Y/Z
z.e              // hit face
z.a              // hit entity (null = block hit)
z.f              // exact hit position (for particle effects, etc.)
```

---

## C# Mapping

| Java | C# |
|---|---|
| `gv` | `RaycastResult` |
| `gv.a` | `RaycastResult.Entity` (null = block hit) |
| `gv.b/c/d` | `RaycastResult.BlockX/Y/Z` |
| `gv.e` | `RaycastResult.Face` (0–5) |
| `gv.f` | `RaycastResult.HitVec` |
| `adt.a(float)` | `EntityRenderer.UpdateObjectMouseOver(partialTick)` |
| `Minecraft.z` | `Engine.ObjectMouseOver` |
| `aes.c()` | `GameMode.GetReach()` — 4.0 Survival / 6.0 Creative |
| `ia.e(float)` | `Entity.GetEyePosition(partialTick)` |
| `ia.f(float)` | `Entity.GetLookVector(partialTick)` |
