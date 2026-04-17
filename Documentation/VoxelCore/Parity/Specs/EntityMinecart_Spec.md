<!--
  SpectraEngine Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraEngine Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# EntityMinecart Spec
**Source class:** `vm.java` (EntityMinecart)
**Superclass:** `ia` (Entity) implements `de` (IInventory)
**EntityList string:** `"Minecart"` — integer ID **42**
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

`vm` is the minecart entity. It comes in three variants controlled by a type field:
- **Type 0** — Normal rideable minecart
- **Type 1** — Chest minecart (27-slot inventory)
- **Type 2** — Furnace minecart (powered by coal, self-propelled)

All three share the same class. The type determines behavior in tick, damage, NBT, and
right-click interaction.

---

## 2. Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `a` | `int` | 0 | Minecart type (0=normal, 1=chest, 2=furnace) |
| `b` | `double` | 0 | Furnace push direction X (normalized, set from player position) |
| `c` | `double` | 0 | Furnace push direction Z (normalized, set from player position) |
| `d[]` | `dk[27]` | null slots | Chest inventory (27 slots); used only when `a == 1` |
| `e` | `int` | 0 | Furnace fuel remaining (ticks); each coal adds 3600 ticks |
| `f` | `boolean` | false | Facing flip flag (yaw has been reversed 180° by direction reversal logic) |

### DataWatcher Slots

| Slot | Type | Default | Semantics |
|---|---|---|---|
| 16 | `byte` | 0 | Bit 0 = furnace active (smoke particles while > 0) |
| 17 | `int` | 0 | Shake timer (ticks remaining for shake animation) |
| 18 | `int` | 1 | Shake force direction multiplier |
| 19 | `int` | 0 | Damage accumulator (break threshold 40) |

---

## 3. Constants

| Value | Meaning |
|---|---|
| `0.98F × 0.7F` | Hitbox width × height |
| `0.35F` | Eye height (L = N / 2.0F ≈ 0.35) |
| `0.4` | Max XZ speed on rail (blocks/tick) |
| `0.6078125` (`0.4 × 0.75`) | Reduced max speed with passenger present |
| `0.96F` | XZ drag on rail (with passenger: `0.997F`) |
| `0.95F` | XZ/Y drag off rail (onGround) |
| `0.04F` | Gravity (Y velocity decrement per tick, off rail) |
| `0.0078125` | Slope acceleration per tick (inclined rail) |
| `40` | Damage threshold for cart destruction |
| `0.04` | Furnace thrust per tick |
| `3600` | Fuel ticks added per coal (`acy.l`) |
| `acy.ay` | Minecart item (dropped on break) |
| `yy.au` | Chest block (dropped if type==1, ID 54) |
| `yy.aB` | Powered rail block? (dropped if type==2) |
| `0.8F` | Furnace drag X when thrusting |
| `0.9F` | Furnace X drag when not thrusting |

---

## 4. Rail Direction Table

The static 3D array `g[10][2][3]` maps rail metadata values (0–9) to direction vectors:

| Meta | Rail shape | From vector | To vector |
|---|---|---|---|
| 0 | flat N-S | (0,0,-1) | (0,0,1) |
| 1 | flat E-W | (-1,0,0) | (1,0,0) |
| 2 | ascending E | (-1,-1,0) | (1,0,0) |
| 3 | ascending W | (-1,0,0) | (1,-1,0) |
| 4 | ascending N | (0,0,-1) | (0,-1,1) |
| 5 | ascending S | (0,-1,-1) | (0,0,1) |
| 6 | curved NE | (0,0,1) | (1,0,0) |
| 7 | curved SE | (0,0,1) | (-1,0,0) |
| 8 | curved SW | (0,0,-1) | (-1,0,0) |
| 9 | curved NW | (0,0,-1) | (1,0,0) |

Curved rails (6–9) are only valid on detector and normal rail; powered rails use 0–5 only.
If the block is a powered/detector rail (`((afr)yy.k[id]).s()`), meta is masked to `& 7`.

---

## 5. Tick Logic `a()`

### 5.1 Pre-tick

1. Decrement DW17 (shake timer) if > 0.
2. Decrement DW19 (damage) if > 0.
3. If furnace active (`g()` = true) and `Y.nextInt(4) == 0`: spawn `"largesmoke"` particle at `(s, t+0.8, u)`.

### 5.2 Client-side interpolation

If `h > 0` (interpolation steps remaining):
- Advance toward `(i, aq, ar)` by `1/h` each tick.
- Advance yaw/pitch toward `(as, at)` by `1/h`.
- Decrement `h`.

Else: hold position (client minecart does not apply own velocity).

### 5.3 Server-side rail physics

**Step 1 — Save previous position:**
`p = s, q = t, r = u`

**Step 2 — Apply gravity:**
`w -= 0.04F`

**Step 3 — Compute block coords:**
```
var1 = floor(s)
var2 = floor(t)
var3 = floor(u)
```
If `afr.g(world, var1, var2-1, var3)` (block below is rail candidate): `var2--`.

**Step 4 — Check rail:**
`var8 = world.getBlockId(var1, var2, var3)`

**If rail block (`afr.e(var8)`):**

1. Compute Y position `t = var2`. (Snap to rail height.)
2. Read rail metadata `var10`.
3. If rail is powered rail type (`((afr)yy.k[var8]).s()`): `var10 &= 7` (strip upper bits).
4. Check powered-rail state:
   - `var11 = (var10 & 8) != 0` → boost mode.
   - `var12 = !var11` (var12 = brake mode for powered rail at rest).
   — Only for powered rail (`var8 == yy.T.bM`).
5. If slope metadata (2–5): `t = var2 + 1` (cart climbs up).
6. Apply slope acceleration: metadata 2→`v -= 0.0078125`, 3→`v += 0.0078125`, 4→`x += 0.0078125`, 5→`x -= 0.0078125`.
7. Look up `var13 = g[var10]` — the from/to direction pair.
8. Compute rail direction unit vector: `(dx, dz) = normalize(to - from)`.
9. Align XZ velocity to rail direction: `speed = sqrt(v² + x²); v = speed * dx; x = speed * dz`.
10. Brake mode (`var12 = true`): if speed < 0.03, zero all velocity; else halve v and x.
11. Compute position along rail (parametric), update `s` and `u`.
12. Set entity position: `d(s, t + L, u)`.
13. With passenger: reduce `var57 = v * 0.75, var58 = x * 0.75`.
14. Speed cap: clamp `v` and `x` to `[-0.4, 0.4]`.
15. `b(var57, 0.0, var58)` — sweep movement (Y=0 on rail).
16. Slope transition: if cart crosses into adjacent block that has a Y offset in `g[]`, lift cart by that Y delta.
17. **Passenger drag:** `v *= 0.997F; w *= 0; x *= 0.997F`.
18. **Empty cart drag:** `v *= 0.96F; w *= 0; x *= 0.96F`.
19. **Furnace cart (type 2) thrust:**
    - If `b*b + c*c > 0.01`: normalize (b,c). Apply: `v *= 0.8F, x *= 0.8F, v += b*0.04, x += c*0.04`.
    - Else: `v *= 0.9F, x *= 0.9F`.
    - Decrement `e` (fuel). If `e <= 0`: `b = c = 0`.
    - After drag: always `v *= 0.96F, w *= 0, x *= 0.96F`.
20. **Slope speed normalisation:** compare `fb` (position on rail) before/after and adjust speed proportionally to height change.
21. **Y-correction:** if `fb` result is not null and previous `fb` exists: lift cart to `fb.b` (rail Y center); adjust `v,x` for slope.
22. **Direction flip check:** if cart crosses block boundary and direction flips 170°+: flip `f` flag and add 180° to yaw.
23. **Boost mode (`var11 = true`):** if speed > 0.01, add `v += v/speed * 0.06, x += x/speed * 0.06`.
    - If speed ≈ 0 and `var10 == 1` (E-W flat): push slightly based on adjacent powered rail.

**If not on rail:**
- Clamp `v` and `x` to `[-0.4, 0.4]`.
- If `D == true` (onGround): halve all velocity.
- `b(v, w, x)` — standard sweep movement.
- If not on ground: `v *= 0.95F, w *= 0.95F, x *= 0.95F`.

**Step 5 — Yaw update:**
- Compute target yaw from `(p-s, r-u)` movement delta: `atan2(dz, dx) * 180/π`.
- If `f == true` (facing flipped): add 180°.
- Normalize delta to `[-180, 180)`.
- If `|delta| >= 170°`: add 180° to yaw, flip `f`.
- Apply yaw.

**Step 6 — Minecart-minecart collision:**
Query `world.b(this, C.expand(0.2, 0, 0.2))`. For each nearby `vm` entity:
- Call `entity.e(this)` — apply impulse push.
- Push logic considers minecart types (furnace carts push normal carts).

**Step 7 — Eject dead passenger:**
If `m != null && m.K`: if `m.n == this`, set `m.n = null`; set `m = null`.

**Step 8 — Fuel countdown:**
If `e > 0`: decrement `e`. If `e <= 0`: set `b = c = 0`.
Call `b(e > 0)` — update DW16 furnace-active flag.

---

## 6. Damage and Destruction `a(Entity attacker, int damage)`

Server side only:
1. Knock back: `h(-m())`.
2. Shake: `c(10)`.
3. Accumulate: `b(i() + damage × 10)`.
4. Sound: `G()`.
5. If damage total > 40:
   - Eject passenger (if any).
   - Call `v()`.
   - Drop `acy.ay` (minecart item, 1×).
   - **Type 1 (chest):** scatter inventory items as `ih` entities with Gaussian velocities. Then drop `yy.au` (chest block, 1×).
   - **Type 2 (furnace):** drop `yy.aB` (furnace block or powered rail — confirm ID).

For `T()` (fall damage analog): same logic, damage multiplied by remaining HP.

---

## 7. Right-Click Interaction `c(EntityPlayer player)`

**Type 0 (normal):**
- If occupied by different player: return `true`.
- Else: mount player onto cart (`player.g(this)`).

**Type 1 (chest):**
- Open chest GUI: `player.a((de)this)`.

**Type 2 (furnace):**
- If player holds coal (`acy.l`): consume one coal, add 3600 fuel ticks (`e += 3600`).
- Set push direction: `b = s - player.s, c = u - player.u`.
- Returns `true`.

---

## 8. Minecart-Minecart Push `e(Entity other)`

If the other entity is NOT a passenger of this cart:
- Auto-mount: if `other` is a `nq` (LivingEntity, not player) and cart has no passenger, cart speed > 0.01, and `other.n == null`: auto-mount.
- Compute push vector from relative position (normalized, scaled by `1/distance`, clamped to 1.0, × 0.1).
- Special cart-vs-cart handling:
  - Furnace cart hits normal → normal gets launched (furnace: `v*=0.2, h(target.v - delta)` etc).
  - Normal hits furnace → furnace absorbs (normal slows).
  - Normal vs normal → exchange velocities (average).

---

## 9. Inventory (Type 1 only)

Implements `de` interface:
- `c()` → 27 (slot count).
- `d(int slot)` → `d[slot]` (get stack).
- `a(int slot, int count)` → split/remove stack.
- `a(int slot, dk stack)` → set slot.
- `e()` → 64 (max stack size).
- `d()` → `"Minecart"` (display name).
- `h()` / `j()` / `k()` — no-ops (open/close callbacks).

---

## 10. NBT

### Write `a(NbtCompound)`

Always:
| Key | Type | Value |
|---|---|---|
| `"Type"` | `int` | `a` (0/1/2) |

Type 2 additionally:
| Key | Type | Value |
|---|---|---|
| `"PushX"` | `double` | `b` |
| `"PushZ"` | `double` | `c` |
| `"Fuel"` | `short` | `(short) e` |

Type 1 additionally:
| Key | Type | Value |
|---|---|---|
| `"Items"` | `NbtList` | Array of item stacks; each compound has `"Slot"` (byte) + standard item fields |

### Read `b(NbtCompound)`

1. `a = tag.getInt("Type")`.
2. If type 2: read `b = PushX (double), c = PushZ (double), e = Fuel (short)`.
3. If type 1: read `"Items"` list; each entry: `slot = getByte("Slot") & 255`; load `dk.a(compound)` into `d[slot]`.

---

## 11. Quirks

**Quirk 11.1 — Facing flip:**
When a minecart reverses direction (delta yaw >= 170°), the yaw is flipped 180° and `f`
toggles. This can cause visual jitter if the cart oscillates.

**Quirk 11.2 — Y gravity zeroed on rail:**
When the cart is on a rail and moving, `w` is set to 0 during the rail-movement call
(`b(var57, 0.0, var58)`). Gravity only applies for off-rail flight (first tick off rail).

**Quirk 11.3 — Chest scatter velocity:**
When a chest minecart is destroyed, items scatter with Gaussian velocity (`nextGaussian() * 0.05F`)
plus +0.2F upward. This creates a "burst" effect.

**Quirk 11.4 — Furnace thrust direction is not re-computed:**
The furnace push direction `(b, c)` is set once when coal is added (pointing away from
player who fuelled it) and only cleared when fuel runs out. The cart will push in the
same XZ direction until fuel is exhausted or a new coal is added.

---

## 12. Open Questions

| # | Question |
|---|---|
| 12.1 | `yy.aB` (type 2 break drop) — is this a furnace block (ID 61) or powered rail (ID 27)? |
| 12.2 | `afr.g(world, x, y, z)` — exact method: checks if block at y is a rail, used to snap `var2` to rail Y. |
| 12.3 | `afr.e(blockId)` — exact method: returns true if block is a rail type. |
| 12.4 | `fb` (position-on-rail helper class) — what are fields `b` (Y) and what type is returned? Appears to be a 3D position struct. |
| 12.5 | Slope speed formula: exact `(var9.b - var60.b) * 0.05` — is `b` the Y coordinate of the rail curve midpoint? |
