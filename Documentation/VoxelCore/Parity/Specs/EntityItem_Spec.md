# EntityItem Spec
Source class: `ih.java`
Type: `class`
Superclass: `ia` (Entity)

---

## 1. Purpose

`EntityItem` (`ih`) is the dropped-item entity. It is spawned by `Entity.a(dk, float)` (the
drop helper on Entity base class) and by world block-break logic. It floats in the world,
bobs visually, and can be picked up by players after a cooldown.

---

## 2. Fields

| Field | Type | Semantics |
|---|---|---|
| `a` | `dk` (ItemStack) | The carried item stack |
| `b` | `int` | Age in ticks. Entity despawns when `b >= 6000` |
| `c` | `int` | Pickup delay in ticks. Must be 0 before pickup is allowed |
| `d` | `float` | Initial random rotation (set in constructor, drives visual bob) |
| `e` | `int` | Internal counter (increments each tick; used for visual bob angle) |
| `f` | `int` | Health points. Initialised to `5`. Taking damage decrements this; `f <= 0` → `v()` (setDead) |

Note: the entity base fields `s/t/u` = posX/Y/Z, `v/w/x` = motionX/Y/Z, `C` = AABB,
`K` = isDead are inherited from `ia`.

---

## 3. Constructor

```
ih(World world, double posX, double posY, double posZ, dk itemStack)
```

Steps performed:
1. Call `super(world)` (Entity base constructor)
2. Call `a(0.25F, 0.25F)` — sets width=0.25, height=0.25
3. After size is set, `L` (eye height offset) is computed as `N/2.0F = 0.125F`
4. Set position via `d(posX, posY, posZ)` (Entity.setPosition)
5. Set `a = itemStack`
6. Set `b = 0` (age)
7. Set `c = 0` (no pickup delay by default)
8. Assign random initial rotation `d = random.nextFloat() * PI * 2`
9. Assign random initial XZ velocity: `v = ±random.nextFloat() * 0.1`, `x = ±random.nextFloat() * 0.1`
10. Assign upward initial Y velocity: `w = 0.2F`

When spawned via `Entity.a(dk, float yOffset)` the pickup delay `c` is set to `10` after
construction (i.e. the drop helper writes `entityItem.c = 10`).

---

## 4. Tick (`a()`)

Called every game tick (20 Hz) by the World.

```
prevX = s; prevY = t; prevZ = u;   // save previous pos (inherited)
w -= 0.04F;                         // gravity
```

**Lava check:**
```
if (block at (x, floor(t), z) is lava-type material) {
    w = 0.2F;
    v = (random - random) * 0.2F;   // random XZ bounce
    x = (random - random) * 0.2F;
}
```

**Move + friction:**
```
b(v, w, x);   // Entity sweep collision (moveEntity)
v *= 0.98F;   // air friction
w *= 0.98F;
x *= 0.98F;
```

**Ground friction (Y ≈ prevY, entity on ground):**
```
if (onGround) {
    v *= 0.58800006F;   // lateral ground friction
    x *= 0.58800006F;
    w *= -0.5F;         // bounce (vertical reflection)
}
```
> Ground friction coefficient is `0.58800006F` for default (no block override). When a
> friction-bearing block is below, the formula is `Block.ca * 0.98F` (block friction × air).

**Age + despawn:**
```
b++;
if (b >= 6000) v();   // setDead
```

**Visual counter:**
```
e++;   // drives bobbing angle computation in renderer
```

---

## 5. Pickup — `a(vi player)` (server-side only)

Called by the server when a player entity is within range. Pickup is gated by:
1. `c == 0` — no pickup delay
2. Player inventory accepts the item (`player.bf.a(a)` returns true) — stack must fit

If both conditions met:
```
world.a("random.pop", s, t, z, 0.2F, (random*0.7F+0.3F)*1.8F);  // play pop sound
player.a(this, a.a);   // notify player of pickup (triggers animation/stat)
if (a.a <= 0) v();     // setDead if stack exhausted
```

The stack-size field `a.a` (ItemStack.stackSize) is decremented by the inventory accept.
If reduced to 0, the entity is killed.

---

## 6. Damage

```
a(DamageSource, int amount) {
    f -= amount;
    if (f <= 0) v();   // setDead
}
```

`f` starts at `5`. Entity can be destroyed by fire, explosions, lava via this path.

---

## 7. DataWatcher Entries

`EntityItem` (`ih`) registers **no additional** DataWatcher indices beyond those from the
Entity base class (index 0 = flags byte, index 1 = air short). The carried ItemStack (`a`)
is synced by other means (initial spawn packet), not via DataWatcher updates.

---

## 8. Size / AABB

- Width = `0.25F` (M = 0.25F)
- Height = `0.25F` (N = 0.25F)
- Eye height L = `N / 2.0F` = `0.125F`
- AABB: `minX=posX-0.125, maxX=posX+0.125, minY=posY, maxY=posY+0.25`

---

## 9. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | `c` is set to `10` by the Entity drop helper after construction, not inside `ih`'s constructor. Code that constructs `ih` directly (without the drop helper) will have `c = 0` (immediate pickup allowed). |
| 2 | Ground friction constant is `0.58800006F` — this appears to be a floating-point artefact of `0.6F * 0.98F` computed at compile time. |
| 3 | `w *= -0.5F` bounce is applied **every tick** the entity is on the ground, not just on landing. This means a resting item stack perpetually oscillates by a tiny fraction each tick. |
| 4 | Despawn check is `b >= 6000` (5 minutes at 20 Hz), not `b > 6000`. |

---

## 10. Open Questions

1. Exact block-friction formula: spec says `Block.ca * 0.98F` but `ca` field name on Block
   needs confirmation. In practice the default path (`0.58800006F`) covers the common case.

---

*Spec written by Analyst AI from `ih.java` (147 lines). No C# implementation consulted.*
