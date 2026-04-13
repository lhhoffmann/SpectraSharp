# Item Spec
Source class: `acy.java`
Type: `class` (non-abstract — base is instantiable; subclasses add behaviour)
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`Item` (`acy`) is the base class for all non-block items. It holds the static registry array,
per-item metadata (name, icon, stack size, durability), and a set of virtual methods that
subclasses override for custom behaviour (placing, using, hitting, etc.). Item IDs start at
256 — block IDs occupy 0–255.

---

## 2. Static Registry

```
public static acy[] d = new acy[32000];
```

Indexed by **registry key = itemId + 256**. The constructor stores `this` at `d[256 + id]`.
Items with IDs 0–122 would alias block IDs — so real item IDs begin at 0 (arrow=6, etc.) and
the registry offset of 256 guarantees no collision with blocks.

> `d[]` size is 32000, not 256. Item IDs can therefore go from 0 up to 31743.
> In 1.0, defined items span IDs 0–2010 (records), with gaps.

---

## 3. Instance Fields

| Field | Type | Default | Semantics |
|---|---|---|---|
| `bM` | `int` (final, public) | `256 + id` | Registry index. `bM - 256` = item ID. |
| `bN` | `int` (protected) | `64` | Max stack size. |
| `a` | `int` (private) | `0` | Internal durability flag. When > 0 combined with `!bQ`, `h()` (isDamageable) returns true. Set by protected builder `i(int)`. |
| `bO` | `int` (protected) | `0` | Icon index in the items texture atlas. Packed as `iconCol + iconRow * 16`. |
| `bP` | `boolean` (protected) | `false` | `true` = has subtypes (different icon/behaviour per damage value). Set via `i()` no-arg. |
| `bQ` | `boolean` (protected) | `false` | Suppresses damageability even if `a > 0`. When `true`, `h()` returns false and crafting-remainder setup is allowed. Returned by `f()`. |
| `b` | `acy` (private) | `null` | Crafting remainder: item left in crafting grid after this item is consumed (e.g. empty bucket). |
| `bR` | `String` (private) | `null` | Auxiliary string data (used for fuel-type tag, potion ingredient key, etc.). Returned by `m()`. |
| `bS` | `String` (private) | — | Unlocalized name. Set as `"item." + name` by `a(String)` builder. |
| `c` (static) | `Random` (protected, static) | `new Random()` | Shared random. |

---

## 4. Constructor

```
protected acy(int id)
    bM = 256 + id
    if d[256 + id] != null: print "CONFLICT @ <id>"   // no exception — just console warning
    d[256 + id] = this
```

Conflict check is non-fatal: later registration silently overwrites.

---

## 5. Builder Methods (return `this`)

| Method | Sets | Notes |
|---|---|---|
| `a(int row, int col)` | `bO = row + col * 16` | Primary icon setter — atlas column × row packed flat. |
| `g(int val)` | `bO = val` | Direct icon index setter (alternative). |
| `h(int n)` | `bN = n` | setMaxStackSize. |
| `i()` *(no-arg)* | `bP = true` | Mark as having subtypes. |
| `i(int n)` *(protected)* | `a = n` | Set internal durability value (for `h()` isDamageable check). |
| `a(boolean flag)` *(protected)* | `bQ = flag` | Set the `bQ` flag (disables damage if true). |
| `a(String name)` | `bS = "item." + name` | setUnlocalizedName. |
| `a(acy item)` | `b = item` | setCraftingResult. Throws `IllegalArgumentException` if `bN > 1`. |
| `b(String s)` | `bR = s` | setAuxiliaryString (fuel key, potion ingredient marker, etc.). |

---

## 6. Getters

| Method | Returns | Semantics |
|---|---|---|
| `e()` | `bN` | getMaxStackSize |
| `c()` | `0` | getMaxDamage — base always 0. Tool/weapon subclasses override to return durability. |
| `g()` | `a` field | getInternalDurabilityValue — used internally; `h()` reads this, not `c()`. |
| `h()` | `a > 0 && !bQ` | isDamageable — true only when internal durability was set and `bQ` is false. |
| `f()` | `bQ` | Returns the `bQ` flag. |
| `a()` | `bP` | hasSubtypes |
| `a(int metadata)` | `bO` | getIconIndex(metadata) — ignores metadata in base class, always returns `bO`. |
| `b(int)` | `0` | getItemEnchantability — 0 = not enchantable in base. |
| `j()` | `b` | getCraftingResult (the remainder item, e.g. empty bucket). |
| `k()` | `b != null` | hasCraftingResult |
| `d()` | `bS` | getUnlocalizedName (e.g. `"item.arrow"`) |
| `l()` | `hj.a(bS + ".name")` | getLocalizedItemStackDisplayName |
| `m()` | `bR` | getAuxiliaryString |
| `n()` | `bR != null` | hasAuxiliaryString |
| `c(int meta)` | `16777215` | getColorFromItemStack — base returns white (0xFFFFFF). |

---

## 7. Virtual / Overridable Methods

All base implementations are no-ops or simple defaults unless noted.

### onItemUse — `a(dk stack, vi player, ry world, int x, int y, int z, int face)` → `boolean`
Called when item is right-clicked on a block. Base returns `false`. Overridden by placer items.

### getMiningSpeed — `a(dk stack, yy block)` → `float`
Returns `1.0F`. Tool subclasses return higher values for appropriate materials.

### onItemRightClick — `a(dk stack, ry world, vi player)` → `dk`
Called when player right-clicks air. Returns `stack` unchanged. Override to start using (e.g. bow draw).

### finishUsingItem — `c(dk stack, ry world, vi player)` → `dk`
Called when use duration expires. Returns `stack` unchanged. Override for food (eating result).

### getMaxItemUseDuration — `b(dk stack)` → `int`
Returns `0`. Override to return ticks the item needs to be held (e.g. 32 for food, 72000 for bow).

### getItemUseAction — `c(dk stack)` → `ps`
Returns `ps.a` (enum NONE). Override to return `ps.b` (EAT), `ps.c` (DRINK), `ps.d` (BLOCK), etc.

### onUpdate — `b(dk stack, ry world, vi player)` → `void`
Called every tick while item is in player inventory. No-op base.

### onEquippedUpdate — `a(dk stack, nq entity)` → `void`
Called per tick while item is equipped in armour slot. No-op base.

### hitEntity — `a(dk stack, nq target, nq attacker)` → `boolean`
Called on successful melee hit. Returns `false` base. Override to damage the item stack.

### onBlockDestroyed — `a(dk stack, int x, int y, int z, int face, nq player)` → `boolean`
Returns `false` base.

### canHarvestBlock — `a(yy block)` → `boolean`
Returns `false` base. Override to return true for blocks this tool can harvest.

### itemInteractionForEntity — `a(ia entity)` → `int`
Returns `1` base (interaction result codes: 0=nothing, 1=default, 2=consumed? — exact semantics are subclass-defined).

### onPlayerStoppedUsing — `a(dk stack, ry world, vi player, int ticksLeft)` → `void`
Called when player releases right-click before use duration expires (e.g. arrow fired here). No-op base.

### addInformation — `a(dk stack, List tooltip)` → `void`
Adds tooltip lines. No-op base.

### onCreated — `a(dk stack, ry world, vi player)` → `void` (overload of `a`)
Called when item is crafted. No-op base.

### getDamageVsEntity — `a(dk stack, int var2)` → `int`
Returns `var1.b()` (uses timer? unclear). Appears to return 0 effectively.

---

## 8. Icon Atlas Layout

The items texture atlas (`items.png`) is 16 columns × 16 rows (256 total slots).
`bO = iconCol + iconRow * 16`. Example: `.a(5, 2)` = column 5, row 2 → `bO = 37`.

---

## 9. Static Item Registry — Selected Entries

| Field | ID | Class | Name string |
|---|---|---|---|
| `e` | 0 | `adb` (Shovel) | `"shovelIron"` |
| `f` | 1 | `zu` (Pickaxe) | `"pickaxeIron"` |
| `g` | 2 | `ago` (Axe) | `"hatchetIron"` |
| `h` | 3 | `ou` | `"flintAndSteel"` |
| `i` | 4 | `agu` (Food) | `"apple"` |
| `j` | 5 | `il` (Bow) | `"bow"` |
| `k` | 6 | `acy` | `"arrow"` |
| `l` | 7 | `pr` | `"coal"` |
| `p` | 11 | `zp` (Sword) | `"swordIron"` |
| `C` | 24 | `acy` | `"stick"` (hasSubtypes) |
| `aF` | 78 | `acy` | `"leather"` |
| `bc` | 102 | `bv` (Map) | `"map"` |
| `bB`–`bL` | 2000–2010 | `pe` (Record) | `"record"` variants |

Records use IDs 2000–2010 (stored at `d[2256]`–`d[2266]`).

---

## 10. Ray-Cast Helper — `a(ry world, vi player, boolean fluid)` → `gv`

Computes the look-direction ray from the player's eye position and performs a block ray-trace
up to **5.0 blocks**:

```
partial = 1.0F
pitch = player.B + (player.z - player.B) * partial
yaw   = player.A + (player.y - player.A) * partial
eyeX  = player.p + (player.s - player.p) * partial
eyeY  = player.q + (player.t - player.q) * partial + 1.62 - player.L
eyeZ  = player.r + (player.u - player.r) * partial

// Direction vector via MathHelper trig:
cosYaw   = me.b(-yaw   * PI/180 - PI)
sinYaw   = me.a(-yaw   * PI/180 - PI)
cosPitch = -me.b(-pitch * PI/180)
sinPitch =  me.a(-pitch * PI/180)
dirX = sinYaw * cosPitch
dirY = sinPitch
dirZ = cosYaw * cosPitch

range = 5.0
end = start.c(dirX * range, dirY * range, dirZ * range)
return world.a(start, end, fluid, !fluid)
```

`fluid=true` → includes fluid blocks in trace; `fluid=false` → ignores fluids.

---

## 11. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | Registry conflict is only a `System.out.println` — no exception. The last item registered at a given ID silently wins. |
| 2 | `c()` (getMaxDamage) returns `0` in the base class unconditionally. It is NOT backed by a field in `acy` — tool subclasses override the method. `g()` returns the private `a` field (also 0 by default), used only for the `h()` (isDamageable) predicate. The two paths are independent. |
| 3 | `setCraftingResult` (`a(acy)`) throws if `bN > 1`, but this check is on `bN` at the time of the call, not at registration. If `bN` is later changed, the constraint is not re-enforced. |
| 4 | `getIconIndex(int metadata)` ignores the metadata parameter in the base class — always returns `bO`. Subclasses (like dye) override to select a different icon per damage value. |
| 5 | Records use IDs 2000–2010 (well above the 256–31999 range implied by array size). These are stored at `d[2256]`–`d[2266]`. |

---

## 12. Open Questions

1. **`ps`** — enum for use-actions (EAT, DRINK, BLOCK, NONE). `ps.a` = NONE. Full enum values
   unclear from this file alone.
2. **`ja`** — item rarity enum. `ja.a` = COMMON, `ja.c` = UNCOMMON (based on enchantment check).
3. **`nu`** — ToolMaterial enum/class. `nu.a`=wood, `nu.b`=stone, `nu.c`=iron, `nu.d`=diamond, `nu.e`=gold.
4. **`pk`** — Potion ingredient registry. `pk.a`, `pk.b`, etc. are ingredient type keys.
5. **`dj`** — ArmorMaterial enum. `dj.a`=cloth, `dj.b`=chain, `dj.c`=iron, `dj.d`=gold, `dj.e`=diamond.
6. The exact meaning of `g()` (returns private `a` field) vs `c()` (overridden max damage) needs
   confirmation from tool subclasses — `g()` may be XP value, fuel time, or another property.

---

*Spec written by Analyst AI from `acy.java` (389 lines). No C# implementation consulted.*
