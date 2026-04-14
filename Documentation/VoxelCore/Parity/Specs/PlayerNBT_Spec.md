<!--
  SpectraSharp Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraSharp Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# PlayerNBT Spec
**Source classes:** `vi.java` (EntityPlayer, 1228 lines — methods a/b only),
  `nq.java` (LivingEntity, methods a/b), `ia.java` (Entity, methods d/e),
  `x.java` (InventoryPlayer, methods a/b), `eq.java` (FoodStats, methods a/b),
  `wq.java` (PlayerAbilities, methods a/b)
**Superclass:** n/a (documents NBT serialisation across the player inheritance chain)
**Analyst:** lhhoffmann
**Date:** 2026-04-14
**Status:** DRAFT
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Purpose

Documents the complete NBT serialisation of `vi` (EntityPlayer) as stored in `level.dat`
under the `"Player"` key. Covers all four layers of the inheritance chain:
`ia.d/e` → `nq.a/b` → `vi.a/b`, plus `x.a/b` (inventory) and `eq/wq` (food/abilities).

The player is **not** saved as a chunk entity. The save path is:
```
si.a(List<vi>)                // WorldInfo.save(players)
  → vi = players.get(0)
  → playerTag = new ik()
  → ia.d(playerTag)           // write base entity fields
      → abstract a(playerTag) → vi.a(playerTag)   // write player-specific
  → levelDatRoot["Player"] = playerTag
```

The load path is:
```
si(ik levelDatTag)            // WorldInfo constructor reads level.dat
  → si.h = levelDatTag["Player"]   // stored for later
  ...
  → player.e(si.h)            // when player joins: ia.e(tag) = base entity load
      → abstract b(tag) → vi.b(tag)   // player-specific load
```

---

## 2. Class Identifiers

| Obfuscated | Human name | Role in PlayerNBT |
|---|---|---|
| `ia` | `Entity` | Base entity fields (Pos, Motion, Rotation, Fire, etc.) |
| `nq` | `LivingEntity` | Health/invulnerability/death/potion effects |
| `vi` | `EntityPlayer` | Player-specific: inventory, XP, sleep, spawn, food, abilities |
| `x` | `InventoryPlayer` | 36-slot main + 4 armor; slot-byte encoding |
| `eq` | `FoodStats` | Hunger level, saturation, exhaustion, regen timer |
| `wq` | `PlayerAbilities` | Invulnerable, flying, mayfly, instabuild flags |

---

## 3. Write Call Chain

`ia.d(tag)` is called from `si.a(List)`. It writes all fields via the chain below
(method names in each class override the abstract stubs from the layer above):

```
ia.d(tag)
├── Pos           (TAG_List of 3 TAG_Double): [x, y+yOffset, z]   ← see Quirk §8.1
├── Motion        (TAG_List of 3 TAG_Double): [vx, vy, vz]
├── Rotation      (TAG_List of 2 TAG_Float):  [yaw, pitch]
├── FallDistance  (TAG_Float)
├── Fire          (TAG_Short)
├── Air           (TAG_Short)
├── OnGround      (TAG_Byte / boolean)
└── calls abstract a(tag) → dispatches to vi.a(tag)
        └── super.a(tag) = nq.a(tag)
                ├── Health          (TAG_Short)
                ├── HurtTime        (TAG_Short)
                ├── DeathTime       (TAG_Short)
                ├── AttackTime      (TAG_Short)
                └── ActiveEffects   (TAG_List, if non-empty)
            [back in vi.a(tag)]
            ├── Inventory           (TAG_List via x.a(new yi()))
            ├── Dimension           (TAG_Int)
            ├── Sleeping            (TAG_Byte / boolean)
            ├── SleepTimer          (TAG_Short)
            ├── XpP                 (TAG_Float)
            ├── XpLevel             (TAG_Int)
            ├── XpTotal             (TAG_Int)
            ├── SpawnX/Y/Z          (TAG_Int × 3, ONLY if bed spawn set)
            ├── abilities compound  (via wq.a(tag))
            └── food stats          (via eq.b(tag))
```

---

## 4. Read Call Chain

`ia.e(tag)` is called when the player loads into a world:

```
ia.e(tag)
├── Motion[0..2]   (clamped to [-10, 10])
├── Pos[0..2]      (sets all position copies; y stored as y+yOffset — see Quirk §8.1)
├── Rotation[0..1]
├── FallDistance
├── Fire
├── Air
├── OnGround
├── setPosition(x,y,z)
├── setRotation(yaw,pitch)
└── calls abstract b(tag) → dispatches to vi.b(tag)
        └── super.b(tag) = nq.b(tag)
                ├── Health          (default = maxHealth if key absent)
                ├── HurtTime
                ├── DeathTime
                ├── AttackTime
                └── ActiveEffects   (if "ActiveEffects" key present)
            [back in vi.b(tag)]
            ├── Inventory           (via x.b(yi))
            ├── Dimension           (TAG_Int → bK field)
            ├── Sleeping            (boolean)
            ├── SleepTimer          (short)
            ├── XpP                 (float)
            ├── XpLevel             (int)
            ├── XpTotal             (int)
            ├── SpawnX/Y/Z          (only if ALL THREE keys present; creates dh spawn coord)
            ├── abilities compound  (via wq.b(tag); only if "abilities" key present)
            └── food stats          (via eq.a(tag); only if "foodLevel" key present)
```

---

## 5. Complete Field Reference

### 5.1 Base Entity Fields (from `ia.d` / `ia.e`)

From EntityNBT_Spec §5 and §6 (identical for all entities):

| NBT key | Tag type | Notes |
|---|---|---|
| `"Pos"` | TAG_List(TAG_Double) | [x, **y+yOffset**, z] — y drift quirk applies |
| `"Motion"` | TAG_List(TAG_Double) | [vx, vy, vz]; clamped to ±10 on read |
| `"Rotation"` | TAG_List(TAG_Float) | [yaw, pitch] |
| `"FallDistance"` | TAG_Float | |
| `"Fire"` | TAG_Short | Negative = fireproof |
| `"Air"` | TAG_Short | Default 300 (15 s) |
| `"OnGround"` | TAG_Byte (bool) | |

### 5.2 LivingEntity Fields (from `nq.a` / `nq.b`)

| NBT key | Tag type | Notes |
|---|---|---|
| `"Health"` | TAG_Short | If absent on read → defaults to `f_()` (= 20 for player) |
| `"HurtTime"` | TAG_Short | Invulnerability frames; default 0 |
| `"DeathTime"` | TAG_Short | Death animation counter; default 0 |
| `"AttackTime"` | TAG_Short | Attack cooldown; default 0 |
| `"ActiveEffects"` | TAG_List(TAG_Compound) | Only written if non-empty; see §5.3 |

### 5.3 Active Effects Format (from `nq.a` / `nq.b`)

Each effect compound in `"ActiveEffects"`:
| Key | Type | Value |
|---|---|---|
| `"Id"` | TAG_Byte | Potion effect ID |
| `"Amplifier"` | TAG_Byte | Level − 1 (0 = level I) |
| `"Duration"` | TAG_Int | Ticks remaining |

### 5.4 Player-Specific Fields (from `vi.a` / `vi.b`)

| NBT key | Tag type | Source field | Notes |
|---|---|---|---|
| `"Inventory"` | TAG_List(TAG_Compound) | `by.a(yi)` | All non-null slots; see §6 |
| `"Dimension"` | TAG_Int | `bK` | Dimension the player was in; used by WorldInfo constructor |
| `"Sleeping"` | TAG_Byte (bool) | `bT` | Whether player is in a bed |
| `"SleepTimer"` | TAG_Short | `a` | Ticks spent sleeping (target: 100) |
| `"XpP"` | TAG_Float | `cf` | XP progress within current level (0.0–1.0) |
| `"XpLevel"` | TAG_Int | `cd` | Current XP level |
| `"XpTotal"` | TAG_Int | `ce` | Total XP points accumulated |
| `"SpawnX"` | TAG_Int | `b.a` | Bed-respawn X; **only written if `b != null`** |
| `"SpawnY"` | TAG_Int | `b.b` | Bed-respawn Y; **only written if `b != null`** |
| `"SpawnZ"` | TAG_Int | `b.c` | Bed-respawn Z; **only written if `b != null`** |
| `"abilities"` | TAG_Compound | `cc.a(tag)` | See §7 |
| `"foodLevel"` etc. | (multiple) | `bB.b(tag)` | See §5.5 |

**Not persisted (confirmed absent from source):**
- `"Score"` — no score field in player NBT
- `"HealF"` — regeneration is tracked inside FoodStats, not separately
- `"playerGameType"` — game type is in level.dat (`"GameType"`), not player NBT
- `"SelectedItemSlot"` / hotbar cursor — resets to 0 on every login
- `"FoodLevel"` (capital F) — field is `"foodLevel"` (lowercase) via FoodStats

### 5.5 Food Stats (from `eq.b` write / `eq.a` read)

Written directly into the player compound (no wrapper):

| NBT key | Tag type | Default | Semantics |
|---|---|---|---|
| `"foodLevel"` | TAG_Int | 20 | Hunger level (0–20) |
| `"foodTickTimer"` | TAG_Int | 0 | Saturation/starvation tick accumulator |
| `"foodSaturationLevel"` | TAG_Float | 5.0 | Saturation (caps at foodLevel) |
| `"foodExhaustionLevel"` | TAG_Float | 0.0 | Exhaustion (0–4; at 4: decrement saturation/hunger) |

On **read**, all four are only loaded if `"foodLevel"` key is present. If absent, food stats
retain their construction defaults (foodLevel=20, saturation=5.0).

---

## 6. Inventory Serialisation (`x.a(yi)` / `x.b(yi)`)

The player inventory is stored as `"Inventory"` TAG_List in the player compound.
The list is written by `by.a(new yi())` and read back by `by.b(yi)`.

### 6.1 Slot Layout

| Slot range | Array | Semantic | NBT Slot byte |
|---|---|---|---|
| 0–8 | `a[0..8]` | Hotbar (slot 0 = far left) | 0–8 |
| 9–35 | `a[9..35]` | Main inventory | 9–35 |
| armor 0 | `b[0]` | Feet (boots) | 100 |
| armor 1 | `b[1]` | Legs (leggings) | 101 |
| armor 2 | `b[2]` | Chest (chestplate) | 102 |
| armor 3 | `b[3]` | Head (helmet) | 103 |

### 6.2 Write (`x.a(yi)`)

```
for i in 0..35 (main + hotbar):
    if a[i] != null:
        compound = new TAG_Compound()
        compound.put("Slot", (byte)i)
        a[i].b(compound)          // ItemStack write
        list.add(compound)

for i in 0..3 (armor):
    if b[i] != null:
        compound = new TAG_Compound()
        compound.put("Slot", (byte)(i + 100))
        b[i].b(compound)          // ItemStack write
        list.add(compound)

return list
```

### 6.3 Read (`x.b(yi)`)

```
a = new dk[36]                    // reset main inventory
b = new dk[4]                     // reset armor

for each compound in list:
    slotByte = compound.c("Slot") & 255     // unsigned byte
    item = dk.a(compound)                   // ItemStack factory; null if invalid
    if item != null:
        if 0 <= slotByte < 36:
            a[slotByte] = item
        if 100 <= slotByte < 104:
            b[slotByte - 100] = item
```

Any slot byte in [36, 99] or [104, 255] is silently ignored.

### 6.4 ItemStack Format (recap from EntityNBT_Spec §9)

Each slot compound:
```
"Slot"   → TAG_Byte  (slot index)
"id"     → TAG_Short (block/item ID)
"Count"  → TAG_Byte  (stack size)
"Damage" → TAG_Short (damage/metadata)
["tag"   → TAG_Compound, optional]
```

---

## 7. Player Abilities (`wq.a` write / `wq.b` read)

Written as a nested `"abilities"` TAG_Compound inside the player compound.

### 7.1 Fields

| Field (obf) | Semantics | Default |
|---|---|---|
| `a` | `invulnerable` — immune to damage | false |
| `b` | `flying` — currently flying | false |
| `c` | `mayfly` — allowed to fly | false |
| `d` | `instabuild` — instant block break (creative) | false |

### 7.2 Write (`wq.a(ik rootTag)`)

```
abilities = new TAG_Compound()
abilities.put("invulnerable", this.a)
abilities.put("flying", this.a)       ← BUG: writes 'a' (invulnerable) instead of 'b' (flying)
abilities.put("mayfly", this.c)
abilities.put("instabuild", this.d)
rootTag.put("abilities", abilities)
```

### 7.3 Read (`wq.b(ik rootTag)`)

```
if rootTag.hasKey("abilities"):
    abilities = rootTag.k("abilities")   // TAG_Compound getter
    this.a = abilities.m("invulnerable")
    this.b = abilities.m("flying")
    this.c = abilities.m("mayfly")
    this.d = abilities.m("instabuild")
```

Read is correct (reads each key into the right field). The write bug means that the `"flying"`
tag always equals `"invulnerable"`. On reload, flying state is loaded correctly from the
(incorrect) `"flying"` value — so a non-invulnerable creative player who was flying will appear
as not flying after reload.

---

## 8. Known Quirks / Bugs to Preserve

### 8.1 Y-Position Drift (inherited from entity base)

`ia.d(tag)` writes `Pos[1] = posY + yOffset` but `ia.e(tag)` reads `posY = Pos[1]` directly
without subtracting `yOffset`. For players, the yOffset may be non-zero (e.g., related to
eye height 1.62). This causes the player to spawn slightly higher on every reload.
Preserve exactly — do not add a yOffset subtraction on read.

### 8.2 `"flying"` Tag Always Equals `"invulnerable"`

`wq.a(ik)` writes `abilities.put("flying", this.a)` where `this.a` is `invulnerable`.
The `flying` field is `this.b` — the wrong variable is used. Flying state is therefore lost
on every save/load cycle for any player who is not invulnerable. This is a vanilla 1.0 bug.

### 8.3 SpawnX/Y/Z Only Written if Bed Spawn Set

If the player has no bed spawn (`b == null`), none of the three Spawn keys are written.
On read, all three keys must be present (`hasKey("SpawnX") AND hasKey("SpawnY") AND hasKey("SpawnZ")`);
if any one is missing, no spawn point is set (player spawns at world spawn).

### 8.4 Sleeping Triggers Bed Attachment

On read, if `bT == true` (Sleeping), the player calls `a(true, true, false)` which attempts
to attach the entity to the bed at the stored position coordinates. This may fail silently if
the bed block is no longer there.

### 8.5 ActiveEffects Only Written if Non-Empty

`nq.a(tag)` only writes `"ActiveEffects"` if `bh.isEmpty() == false`. If the player has no
active effects, the key is absent. On read, effects are only loaded if the key exists.

### 8.6 No Hotbar Slot Persistence

The current hotbar cursor (`x.c`, 0–8) is NOT written to NBT. On every login, the player's
selected hotbar slot resets to 0 (leftmost slot). This is vanilla 1.0 behaviour.

---

## 9. Open Questions

- **Player `U` (yOffset)**: the exact value for EntityPlayer in 1.0 not confirmed in this file.
  The Entity_Spec documents yOffset fields but did not give the exact player value. It may be
  1.62 (eye height) or 0. Relevant to the Y-drift magnitude.
- **`bK` (Dimension)**: confirmed as int, read/written as `"Dimension"`. The WorldInfo
  constructor reads `si.h.e("Dimension")` to determine which world the player was in.
  Exact values: 0 = Overworld, -1 = Nether, 1 = End.
- **SleepTimer target**: the game considers sleep complete at some threshold of `a` (SleepTimer).
  The exact threshold and whether it's checked at all on reload is not confirmed from this file.

---

*Spec written by Analyst AI from `vi.java` (methods a/b at lines 485–526), `nq.java` (methods a/b),
`ia.java` (methods d/e), `x.java` (methods a/b at lines 245–284), `eq.java` (54 lines),
`wq.java` (25 lines). No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — PlayerNBT)*
