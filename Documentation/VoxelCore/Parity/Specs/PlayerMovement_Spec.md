# Spec: Player Movement Physics

**Java classes:** `vi` (EntityPlayer), `nq` (EntityLiving), `ia` (Entity base), `agn` (PlayerMovement / input)
**Status:** PROVIDED
**Canonical name:** EntityPlayer / PlayerMovement

---

## Class Hierarchy

```
ia (Entity base)
  └── nq (EntityLiving) — movement physics, velocity, jump
        └── vi (EntityPlayer) — speed fields, sprint, sneak, inventory
              └── di (EntityPlayerSP) — client-side input, camera interpolation
```

`di` does **not** override movement methods — all movement logic is in `vi` and `nq`.

---

## Input Class (`agn`)

```java
public class agn {
    public float a = 0.0F;   // strafe input (-1 left, +1 right)
    public float b = 0.0F;   // forward input (-1 back, +1 forward)
    public boolean c = false; // sneak
    public boolean d = false; // jump
    public boolean e = false; // swim (used for diving in swimming mode)
}
```

Held as `di.b` (field `b`). The actual key-state subclass overrides `agn.a(vi)`.

Each tick in `di.n()`:
```java
this.br = this.b.a;   // moveStrafe  ← agn.a
this.bs = this.b.b;   // moveForward ← agn.b
this.bu = this.b.d;   // jump input  ← agn.d
```

---

## Speed Constants

| Field | Class | Value | Purpose |
|---|---|---|---|
| `cg` | `vi` | `0.1F` | base ground speed (walkSpeed) |
| `ch` | `vi` | `0.02F` | base air speed |
| `aI` | `nq` | set from `cg` | effective ground acceleration this tick |
| `aJ` | `nq` | set from `ch` | effective air acceleration this tick |

Each tick in `vi.c()` (onLivingUpdate):
```java
this.aI = this.cg;      // reset to base
this.aJ = this.ch;
if (this.X()) {         // isSprinting (entity flag bit 3)
    this.aI += this.cg * 0.3F;  // → 0.13F
    this.aJ += this.ch * 0.3F;  // → 0.026F
}
```

**Sprint speed = `cg * 1.3 = 0.13F`** on ground.

---

## Sneak

When `az()` is true (sneaking, `cc.c == true`):
```java
this.b.a *= 0.2F;   // strafe → 20%
this.b.b *= 0.2F;   // forward → 20%
```

Effective sneak speed = `0.1F * 0.2 = 0.02F` (no additional sneak flag on `aI`).

Also: sneak prevents falling off edges — a specialised boundary check in `di.c(double, double, double)`.

---

## Sprint Activation / Cancellation

Sprint is stored as entity flag bit 3 (`X()` reads this).

**Activates** (double-tap-forward detection in `di.n()`):
1. First forward press: `d = 7` (7-tick window)
2. Second forward press within window: `a(true)` = setSprint

Conditions for activation:
- On ground (`D == true`)
- `b.b >= 0.8` (forward input strong)
- Not already sprinting
- Food level > 6 (`aO().a() > 6.0F`)
- Not sneaking (`!az()`)
- No Hunger effect (`!a(abg.q)`)

**Cancels** when:
- `b.b < 0.8` (released forward)
- `E == true` (horizontal collision)
- Food <= 6

---

## Ground Movement Formula (`nq.d(strafe, forward)`)

On ground:
```java
float slipperiness = 0.91F;
if (this.D) {
    slipperiness = 0.546F;  // = blockBelow.ca * 0.91
    // (default block slipperiness = 0.6 → 0.6 * 0.91 = 0.546)
}
float traction = 0.16277136F / (slipperiness * slipperiness * slipperiness);
float accel = this.D ? this.aI * traction : this.aJ;
this.a(strafe, forward, accel);   // add velocity in look direction
```

On **default stone/dirt ground** (`ca = 0.6`): `traction = 0.16277/0.546³ ≈ 1.0` → `accel = aI`.
On **ice** (`ca = 0.98`): `traction ≈ 0.23` → `accel = aI * 0.23` (very slippery, low acceleration).

After movement:
```java
this.v *= slipperiness;   // x-velocity drag
this.x *= slipperiness;   // z-velocity drag
```

---

## Jump

```java
// nq.ak() — triggered when bu (jump) pressed and on ground (D)
this.w = 0.42F;                  // upward velocity = 0.42 m/tick

// Jump Boost potion (abg.j):
this.w += (level + 1) * 0.1F;

// Sprint jump horizontal impulse:
if (this.X()) {
    this.v -= sin(yaw) * 0.2F;
    this.x += cos(yaw) * 0.2F;
}
```

Jump cooldown: `d = 10` ticks before next jump allowed.

Water/lava swim-up: `w += 0.04F` per tick while `bu` held and not on ground.

---

## Gravity and Vertical Drag

Applied in `nq.d()` (non-water path):
```java
this.w -= 0.08;         // gravity = 0.08 m/tick² downward
this.w *= 0.98F;        // vertical drag
```

In **water** path:
```java
this.w *= 0.8F;
this.w -= 0.02;
```

In **lava** path: same as water with `0.8 * 0.5` horizontal + separate y handling.

---

## Swimming Mode (`cc.b`)

When `cc.b == true` (player is swimming / flying in Creative):
- Override: `aJ = 0.05F` (reduced), call `super.d(strafe, forward)`, restore.
- Dive down: `b.e == true` → `w -= 0.15F` per tick.
- Swim up: `b.d == true` → `w += 0.15F` per tick.

---

## Food Drain (Walking vs Sprinting)

In `vi.j()` (called after each movement with distance vector):
- Sprinting: `g(0.099F * dist)` food exhaustion per tile moved (10× normal)
- Normal on ground: `g(0.01F * dist * 0.01F)` ≈ effectively very small per tile

`g(float)` = addExhaustion to `FoodStats`.

---

## C# Mapping

| Java | C# |
|---|---|
| `agn` | `PlayerInput` |
| `agn.a` | `PlayerInput.Strafe` |
| `agn.b` | `PlayerInput.Forward` |
| `agn.d` | `PlayerInput.Jump` |
| `agn.c` | `PlayerInput.Sneak` |
| `vi.cg` | `EntityPlayer.WalkSpeed` (0.1F) |
| `vi.ch` | `EntityPlayer.AirSpeed` (0.02F) |
| `nq.aI` | `EntityLiving.GroundAcceleration` |
| `nq.aJ` | `EntityLiving.AirAcceleration` |
| `nq.ak()` | `EntityLiving.Jump()` |
| `ia.X()` | `Entity.IsSprinting` |
| `ia.f(3)` | entity flag bit 3 = sprinting |
| `nq.br` | `EntityLiving.MoveForward` |
| `nq.bs` | `EntityLiving.MoveStrafe` |
| `nq.bu` | `EntityLiving.JumpInput` |
