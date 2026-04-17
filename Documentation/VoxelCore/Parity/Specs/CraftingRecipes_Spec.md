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

# CraftingRecipes and FurnaceRecipes Spec
**Source classes:** `sl.java` (CraftingManager), `mt.java` (FurnaceRecipes),
`zq.java`, `kj.java`, `ady.java`, `air.java`, `do.java`, `xx.java`, `jc.java`
(recipe registration helpers — tool/armor/food recipes)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. CraftingManager — `sl`

Singleton: `sl.a()`.

### 1.1 Recipe Types

| Class | Type | Method |
|---|---|---|
| `aga` | ShapedRecipe | added via `sl.a(output, pattern...)` |
| `bc` | ShapelessRecipe | added via `sl.b(output, ingredients...)` |

Both implement `ue` interface with:
- `a(lm grid)` — `boolean` does this recipe match?
- `b(lm grid)` — `dk` return the result.

### 1.2 Recipe Lookup — `a(lm craftingGrid)`

Special case first: **tool repair** (2 same damageable items of count 1 each):
```
remaining = (itemA_remaining + itemB_remaining) + maxDurability * 10 / 100
newDamage = max(0, maxDurability - remaining)
```
Returns repaired item with new damage value.

Then: iterate recipe list in order, return first match.

### 1.3 Pattern Format (shaped recipes)

```java
a(output, "row1", "row2", "row3",  'A', ingredientA, 'B', ingredientB, ...)
```
- Pattern strings define the grid (1, 2, or 3 rows; width from string length).
- `' '` (space) = empty cell.
- Ingredients may be `acy` (Item → any damage), `yy` (Block → any metadata = -1), or `dk` (exact damage/meta).
- Mirrors: `aga` checks both normal and mirrored orientations.

### 1.4 Registration Helpers (subclasses)

| Class | Content |
|---|---|
| `zq` | Wood tool recipes (sword, pickaxe, axe, shovel, hoe) |
| `kj` | Stone tool recipes |
| `ady` | Iron tool recipes |
| `air` | Diamond tool recipes |
| `do` | Gold tool recipes |
| `xx` | Armor recipes (all tiers) |
| `jc` | Food/misc recipes (bread, cake, cookie, etc.) |

---

## 2. Shaped Crafting Recipes

All recipes registered directly in `sl` constructor (excludes tool/armor from helpers).

Notation: each recipe is `output(count) <- pattern [ingredient map]`

```
Paper ×3       <- "###" ['#'=acy.aI (sugar cane)]
Book ×1        <- "#" / "#" / "#" ['#'=acy.aJ (paper)]
Nether brick block ×2  <- "###"/"###" ['#'=acy.C]
Nether brick fence ×6  <- "###"/"###" ['#'=yy.bA]
yy.bv ×1       <- "#W#"/"#W#" ['#'=acy.C, 'W'=yy.x]
yy.aY ×1       <- "###"/"#X#"/"###" ['#'=yy.x, 'X'=acy.m]
Dispenser ×1   <- "###"/"#X#"/"###" ['#'=yy.x, 'X'=acy.aB]
yy.an ×1       <- "###"/"XXX"/"###" ['#'=yy.x, 'X'=acy.aK]
yy.aU ×1       <- "##"/"##" ['#'=acy.aC]    [stone slab type]
yy.aW ×1       <- "##"/"##" ['#'=acy.aH]
yy.al ×1       <- "##"/"##" ['#'=acy.aG]
yy.bd ×1       <- "##"/"##" ['#'=acy.aS]
Wool ×1        <- "##"/"##" ['#'=acy.J]     [string → wool]
yy.am ×1       <- "X#X"/"#X#"/"X#X" ['X'=acy.L, '#'=yy.E (sand)]   [TNT]
Slab ×3 (stone, meta 3) <- "###" ['#'=yy.w]   [oak slab?]
Slab ×3 (meta 0) <- "###" ['#'=yy.t]
Slab ×3 (meta 1) <- "###" ['#'=yy.Q]
Slab ×3 (meta 2) <- "###" ['#'=yy.x]
Slab ×3 (meta 4) <- "###" ['#'=yy.al]
Slab ×3 (meta 5) <- "###" ['#'=yy.bm]
Iron Bars ×16  <- "# #"/"###"/"# #" ['#'=acy.C]
Boat ×1        <- "##"/"##"/"##" ['#'=yy.x]  [crafted from planks]
Nether brick block ×2 <- "###"/"###" ['#'=yy.x]  [duplicate? or second slab type]
Leather Armor  <- "##"/"##"/"##" ['#'=acy.n]
Sign ×3        <- "###"/"###"/" X " ['#'=yy.x, 'X'=acy.C]   [plank+stick]
Eye of Ender ×1 <- "AAA"/"BEB"/"CCC" ['A'=acy.aF, 'B'=acy.aX, 'C'=acy.S, 'E'=acy.aO]
Rod ×1         <- "#" ['#'=acy.aI]   [blaze rod? or stick]
Plank ×4       <- "#" ['#'=yy.J]     [cobble→planks? or log→planks]
Stick ×4       <- "#"/"#" ['#'=yy.x] [actually planks→stick]
Torch ×4       <- "X"/"#" ['X'=acy.l, '#'=acy.C]  [coal+stick → 4 torches]
Torch ×4       <- "X"/"#" ['X'=new dk(acy.l,1,1), '#'=acy.C]  [charcoal variant]
yy.aq ×4       <- "# #"/" # " ['#'=yy.x]    [stone-form output, stone pressure plate]
Glass pane ×16 <- "# #"/" # " ['#'=yy.M]   [glass pane from glass]
yy.aG ×16      <- "X X"/"X#X"/"X X" ['X'=acy.n, '#'=acy.C]   [rail?]
Powered rail ×6<- "X X"/"X#X"/"XRX" ['X'=acy.o, 'R'=acy.aB, '#'=acy.C]
Detector rail ×6 <- "X X"/"X#X"/"XRX" ['X'=acy.n, 'R'=acy.aB, '#'=yy.aK]
Minecart ×1    <- "# #"/"###" ['#'=acy.n]
Powered minecart ×1 <- "# #"/"# #"/"###" ['#'=acy.n]  [furnace minecart]
Flint and steel <- "A "/"  B" ['A'=acy.n, 'B'=acy.ao]  [iron+flint]
yy.T ×1        <- "###" ['#'=acy.S]    [some block from ingredient S]
Oak stairs ×4  <- "#  "/"## "/"###" ['#'=yy.x]
Stone stairs ×4<- "#  "/"## "/"###" ['#'=yy.w]
yy.bw stairs ×4 <- "#  "/"## "/"###" ['#'=yy.al]
yy.bx stairs ×4 <- "#  "/"## "/"###" ['#'=yy.bm]
yy.bC stairs ×4 <- "#  "/"## "/"###" ['#'=yy.bA]
Painting ×1    <- "###"/"#X#"/"###" ['#'=acy.C, 'X'=yy.ab]   [stick+wool]
Golden apple ×1 <- "###"/"#X#"/"###" ['#'=yy.ah, 'X'=acy.i]  [gold block+apple]
Door ×1        <- "X"/"#" ['#'=yy.w, 'X'=acy.C]   [wood+stick=fence?]
Redstone torch ×1 <- "X"/"#" ['#'=acy.C, 'X'=acy.aB]
Clock ×1       <- "#X#"/"III" ['#'=yy.aQ, 'X'=acy.aB, 'I'=yy.t]  [enchant table + redstone + planks?]
Compass ×1     <- " # "/"#X#"/" # " ['#'=acy.o, 'X'=acy.aB]  [iron surround + redstone]
Piston ×1      <- " # "/"#X#"/" # " ['#'=acy.n, 'X'=acy.aB]
Sticky piston  <- "###"/"#X#"/"###" ['#'=acy.aJ, 'X'=acy.aP]  [paper+something]
Ladder ×3      <- "#"/"#" ['#'=yy.t]   [2 planks vertical = 3 ladders]
Stone slab ×2  <- "##" ['#'=yy.t]   [planks → slab?]
Wood slab ×2   <- "##" ['#'=yy.x]
Note block ×1  <- "###"/"#X#"/"#R#" ['#'=yy.w, 'X'=acy.j, 'R'=acy.aB]
Jukebox ×1     <- "TTT"/"#X#"/"#R#" ['#'=yy.w, 'X'=acy.n, 'R'=acy.aB, 'T'=yy.x]
yy.V ×1        <- "S"/"P" ['S'=acy.aL, 'P'=yy.Z]   [jukebox+something]
Bed ×1         <- "###"/"XXX" ['#'=yy.ab, 'X'=yy.x]
yy.bE ×1       <- " B "/"D#D"/"###" ['#'=yy.ap, 'B'=acy.aK, 'D'=acy.m]  [end portal frame?]
```

**Shapeless recipe:**
```
Eye of Ender ×1 (shapeless) <- acy.bm (ender pearl) + acy.bv (blaze powder)
```

---

## 3. Tool and Armor Recipes (from helper classes)

These use the same `a(output, pattern, ingredients)` API. General patterns by material:

**Sword** (2 material + 1 stick, vertical):
```
"#"
"#"
"X"   X=stick
```

**Pickaxe** (3 material top + 2 stick below):
```
"###"
" # "
" # "
```

**Axe** (2 material top-right + 2 stick below):
```
"##"
"#X"    or similar
" X"
```

**Shovel** (1 material + 2 stick):
```
"#"
"X"
"X"
```

**Hoe** (2 material + 2 stick):
```
"##"
" X"
" X"
```

Materials: wood planks (`yy.t` or `yy.x`?), stone (`yy.w`?), iron (`acy.C`?), gold (`acy.o`?), diamond (`acy.m`?).

**Armor helmet** (5 material, 2 rows):
```
"###"
"# #"
```

**Chestplate** (8 material):
```
"# #"
"###"
"###"
```

**Leggings** (7 material):
```
"###"
"# #"
"# #"
```

**Boots** (4 material):
```
"# #"
"# #"
```

---

## 4. Furnace Recipes — `mt` (FurnaceRecipes)

Singleton `mt.a()`. Map from `input_block_or_item_ID → output ItemStack`.

| Input (obf) | Input (inferred) | Output (obf) | Output (inferred) |
|---|---|---|---|
| `yy.H.bM` | Iron Ore | `acy.n` | Iron Ingot |
| `yy.G.bM` | Gold Ore | `acy.o` | Gold Ingot |
| `yy.aw.bM` | Cobblestone (ID 4) | `acy.m` | Stone |
| `yy.E.bM` | Sand (ID 12) | `yy.M` | Glass block |
| `acy.ap.bM` | Raw Pork | `acy.aq` | Cooked Pork |
| `acy.bh.bM` | Raw Fish | `acy.bi` | Cooked Fish |
| `acy.bj.bM` | Raw Chicken | `acy.bk` | Cooked Chicken |
| `acy.aT.bM` | Raw Beef | `acy.aU` | Cooked Beef (Steak) |
| `yy.w.bM` | Wood Log (ID 17) | `new dk(yy.t)` | Charcoal (as item?) |
| `acy.aH.bM` | Clay item | `acy.aG` | Brick item |
| `yy.aV.bM` | Clay block | `new dk(acy.aV, 1, 2)` | Hardened Clay (meta 2) |
| `yy.J.bM` | Cobblestone? | `new dk(acy.l, 1, 1)` | Coal meta 1 (charcoal) |
| `yy.I.bM` | Some stone block | `new dk(acy.l)` | Coal meta 0 |
| `yy.aN.bM` | Nether quartz ore? | `acy.aB` | Some ingot/material |
| `yy.N.bM` | Some block | `new dk(acy.aV, 1, 4)` | Dye meta 4 (blue from lapis?) |

> Open Question: Several mappings above are inferred and need confirmation from BlockRegistry_Spec.
> Specifically confirm: `yy.aw` = cobblestone, `yy.J` = stone (not cobble), `yy.I` = stone brick?,
> `yy.aN` = quartz ore, `yy.N` = lapis ore block.

---

## 5. Known Item/Block ID Mappings (for recipe decoding)

From confirmed sources:

| Obfuscated | Description | Item/Block ID |
|---|---|---|
| `acy.aI` | Sugar cane item | 338 |
| `acy.aJ` | Paper | 339 |
| `acy.aK` | Book | 340 |
| `acy.av` | Bucket (empty) | 325 |
| `acy.aF` (item) | Milk bucket | 335 |
| `acy.as` | Golden apple | 322 |
| `acy.at` | Sign | 323 |
| `acy.bd` | Shears | 359 |
| `acy.aV` | Dye / Ink sac | 351 |
| `yy.E` | Sand | 12 |
| `yy.M` | Glass block | 20 |
| `yy.aD` | Standing sign (block) | 63 |
| `yy.aI` | Wall sign (block) | 68 |
| `yy.ay` | Workbench | 58 |
| `yy.aB` | Furnace (unlit) | 61 |
| `yy.aC` | Furnace (lit) | 62 |
| `yy.K` | Leaves | 18 |
| `yy.W` | Tall Grass | 31 |
| `yy.ab` | Wool | 35 |
| `yy.aG` | Rail | 66 |
| `yy.T` | PoweredRail | 27 |
| `yy.U` | DetectorRail | 28 |
| `yy.bA` | Nether Brick block | 112 |
| `yy.bB` | Nether Brick Fence | 113 |

---

## 6. Open Questions

| # | Question |
|---|---|
| 6.1 | `acy.C` — confirm item ID. Context: appears in iron bar recipe and as a generic material. Iron ingot = 265? |
| 6.2 | `acy.n`, `acy.o`, `acy.m` — iron ingot, gold ingot, diamond? Confirm from `acy.java` static initializer. |
| 6.3 | `acy.C` stick vs iron ingot — the fuel table shows `acy.C` burns for 100 ticks (= stick) but it's also used in "metal" recipes. Two different fields? |
| 6.4 | Tool/armor helper classes (`zq`, `kj`, etc.) — confirm all 5 material tiers produce tools with correct IDs. |
| 6.5 | Shapeless recipe count — is `Eye of Ender` the only shapeless recipe, or do the helpers add more? |
| 6.6 | Mirroring — does `aga` (ShapedRecipe) automatically check horizontal mirror? Confirm. |
| 6.7 | `yy.w` smelted to `new dk(yy.t)` — is this really log→planks (not charcoal)? Check if `yy.t` = planks block or charcoal item. |
