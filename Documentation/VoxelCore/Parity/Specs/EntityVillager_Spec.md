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

# EntityVillager Spec
**Source class:** `ai.java`
**EntityList name:** `"Villager"` (Entity ID 120)
**Analyst:** lhhoffmann
**Date:** 2026-04-17
**Status:** `DRAFT`
**License:** [CC BY 4.0](../../../LICENSE.md)

---

## 1. Class Hierarchy

`ai` extends `ww`.

`ww` is the base for non-hostile AI creatures (wandering mobs). The villager does not extend
`zo` (EntityMonseter) — it is a passive mob.

---

## 2. Fields

| Field | Type | Meaning |
|---|---|---|
| `a` | `int` | Profession ID (0–4) |

---

## 3. Constructor

```
ai(ry world, int profession):
    super(world)
    this.a = profession
    this.ax()           // set texture from profession
    this.bw = 0.5F      // trade/follow range (or similar weight)
```

Default constructor `ai(ry world)` calls `this(world, 0)` (farmer).

---

## 4. Max Health — `f_()`

Returns `20` (10 full hearts).

---

## 5. Profession and Texture — `ax()`

Profession field `a` maps to texture paths:

| Value | Profession | Texture path |
|---|---|---|
| 0 | Farmer | `/mob/villager/farmer.png` |
| 1 | Librarian | `/mob/villager/librarian.png` |
| 2 | Priest | `/mob/villager/priest.png` |
| 3 | Blacksmith (Smith) | `/mob/villager/smith.png` |
| 4 | Butcher | `/mob/villager/butcher.png` |

Default fallback (none matched): `/mob/villager/villager.png`.

---

## 6. Sunlight Immunity — `d()`

Returns `false`. Villagers do NOT burn in sunlight (they are passive, not undead).

---

## 7. Sounds

| Method | Sound key |
|---|---|
| `e()` idle | `"mob.villager.default"` |
| `f()` hurt | `"mob.villager.defaulthurt"` |
| `g()` death | `"mob.villager.defaultdeath"` |

---

## 8. NBT

```
a(ik nbt):   nbt.a("Profession", this.a)   // int
b(ik nbt):   this.a = nbt.e("Profession")
             this.ax()   // refresh texture
```

Plus all base `ww` / `nq` entity NBT fields (Health, HurtTime, etc.).

---

## 9. AI (from `ww` base)

The `ai` class itself has minimal logic — all AI behaviour is inherited from `ww`:
- Wanders randomly.
- Avoids hostile mobs at night.
- Returns indoors at nightfall (village door tracking).
- `c()` method calls `super.c()` only — no additional tick logic in `ai`.

---

## 10. Spawn

Villagers spawn inside generated village buildings. Entity type: passive mob (`fx` or similar
creature type used by SpawnerAnimals). No natural wilderness spawning.

---

## 11. Open Questions

| # | Question |
|---|---|
| 11.1 | `ww` superclass — is this `EntityCreature` (open-world wander AI), or a specific villager base? Confirm class name and purpose. |
| 11.2 | Trading system in 1.0: does `ai` have a `MerchantRecipeList` field? Or was trading not implemented yet? |
| 11.3 | Does right-clicking a villager open a trade GUI in 1.0? What `player.a()` method is called? |
| 11.4 | Does the villager have a follow/flee range or pathfinding speed beyond `bw = 0.5F`? |
| 11.5 | Zombie villager: in 1.0, can a zombie convert a villager? (Likely not — this was a 1.4 feature.) |
| 11.6 | `this.ax()` at start — is the default `/mob/villager/villager.png` texture ever used, or always overridden by profession? |
