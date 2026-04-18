# Spec: EntityPlayerSP

**Java class:** `di`
**Extends:** `vi` (EntityPlayer) directly
**Status:** PROVIDED
**Canonical name:** EntityPlayerSP

---

## Class Identity

`di` is the concrete single-player entity class. It extends `vi` (EntityPlayer) directly
(no intermediate class). It is instantiated by `ItemInWorldManager.b(ry world)` which
returns `new di(minecraft, world, sessionData, dimensionId)`.

`zb` is `EntityOtherPlayerMP` — the remote-player representation seen in multiplayer.
Both extend `vi`.

---

## Constructor

```java
public di(Minecraft var1, ry var2, dr var3, int var4)
```

- `var1` = Minecraft singleton
- `var2` = World
- `var3` = `dr` = session credentials (username + session token)
- `var4` = dimension ID

---

## Fields on `di` Not in `vi`

| Field | Type | Purpose |
|---|---|---|
| `b` | `agn` | MovementInput — keyboard state mapped to movement vectors |
| `c` | `Minecraft` | Game instance reference |
| `d` | `int` | Sprint cooldown timer (double-tap W detection) |
| `e` | `int` | Sleep timer — set to 600 when sleeping, counts down |
| `f, g, h, i` | `float` | Smoothed animation values (head bob / yaw interpolation) |
| `a, cj, ck` | `ne` | `AnimatedFloat` instances for various animations |

---

## Key Method Overrides

### Per-tick (`b()` / `onUpdate`)

- Portal fade logic via `c()` method (darkens screen when entering portal)
- Nausea effect processing (from `c` field increments)
- Flying toggle on double-space (if `mayfly` is set in `PlayerAbilities`)
- Sprint detection: `d` field counts down after W-press; second press within window = sprint
- Sleep timer: when sleeping, `e` field = 600 ticking down; wakes on e=0

### Dimension travel (`c(int dim)`)

```java
public void c(int var1) {
    // dim == 1: show end credits / End cutscene
    // dim == 0: return to Overworld
    // other: standard dimension portal
}
```

Called from `EntityPlayerSP.b()` when the portal transition completes. For `dim == 1`
(End), shows the credits screen instead of entering a world. For `dim == 0`, returns
from End/Nether. Otherwise, uses standard portal teleport.

The actual world switch (creating a new `ry` object) happens in `Minecraft.a(int)`.

### Block break / place dispatch

**Block breaking does NOT go through `di` directly.**
- Left-click dispatches to `Minecraft.c` (`ItemInWorldManager`) via `Minecraft.a(0, true)`.
- `Minecraft.c.c(x,y,z,face)` = damage tick
- `Minecraft.c.b(x,y,z,face)` = instant-break check
- `Minecraft.c.b()` = reset on key-up

**Block placement / interaction:**
- Right-click dispatches to `Minecraft.c.a(player, world, item, x,y,z,face)`.
- The `ItemInWorldManager` delegates to `Item.onItemUse` → `Block.onBlockActivated`.

There is no per-player `onPlayerDamageBlock` override in `di`.

### `openContainer` / GUI screen

Opening a container is driven by the server/world side (via `te` TileEntity interaction
or entity interaction). The result is a call to `Minecraft.a(GuiScreen)` which sets
`Minecraft.s` to the appropriate screen.

---

## Notable Differences from `vi`

| Feature | `vi` | `di` |
|---|---|---|
| Eye height | 1.62F | inherits from `vi` |
| No-clip | false | false (unlike `zb` which sets `W=true`) |
| Network interpolation | none | none (that is `zb`'s feature) |
| Hunger | absent | absent — no hunger in 1.0 |

---

## Multiplayer (`zb` — EntityOtherPlayerMP)

Key differences from `di`:

- `L = 0.0F` — eye height zero (camera not on this entity)
- `W = true` — noClip enabled (position set via network packets, not physics)
- Network interpolation: `c` counter field for smooth position lerp between packets

---

## C# Mapping

| Java | C# |
|---|---|
| `di` | `EntityPlayerSP` |
| `zb` | `EntityOtherPlayerMP` |
| `di.b` | `MovementInput` field |
| `di.d` | `sprintCooldown` |
| `di.e` | `sleepTimer` |
| `di.c(int)` | `TravelToDimension(int)` |
