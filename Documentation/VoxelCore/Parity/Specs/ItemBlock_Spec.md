# ItemBlock Spec
Source: `uw.java` (85 lines); extends `acy` (Item)
Type: Class definition

---

## 1. Overview

`uw` is the `ItemBlock` class — the default Item form of a placeable block.
Every block gets an `uw` instance registered in `acy.d[blockId]` unless an explicit
custom Item class is provided (see BlockRegistry spec §7).

It extends `acy` (Item) and adds block-placement logic to `onItemUse`.

---

## 2. Constructor

```java
public uw(int itemId) {
    super(itemId);             // registers self in acy.d[256 + itemId]
    this.a = itemId + 256;     // stores block ID (itemId is negative for blocks: itemId = blockId - 256)
}
```

**Field layout:**
- `itemId` parameter = `blockId - 256` (e.g., stone blockId=1 → itemId=-255)
- `acy` base constructor registers at `d[256 + itemId] = d[256 + (blockId-256)] = d[blockId]`
- `this.a` stores **blockId** (= `itemId + 256`)

So `uw.a` = the associated block ID.

---

## 3. `b(int face)` — getTextureFromSide (icon selection)

Returns the texture index for the icon shown in the inventory:

```java
@Override
public int b(int face) {
    return yy.k[this.a].b(2);    // face=2 is the "south" side face
}
```

- Calls `Block.b(face=2)` which returns `bL` (the primary texture index) for most blocks.
- Blocks with multi-face textures (Log, GrassBlock) return their side texture (face 2).

---

## 4. `a(dk, vi, ry, x, y, z, face)` — onItemUse (place block)

Main placement logic. Signature:
```java
public boolean a(dk stack, vi player, ry world, int x, int y, int z, int face)
```

### Step 1 — Determine target position

Adjusts `x/y/z` by the clicked face direction so the block is placed adjacent to the clicked face:
```
face 0 (down):  y -= 1
face 1 (up):    y += 1
face 2 (north): z -= 1
face 3 (south): z += 1
face 4 (west):  x -= 1
face 5 (east):  x += 1
```

### Step 2 — Validity guards (abort if any fails)

1. `stack.a <= 0` → return false (no items left)
2. `!player.a_(world, x, y, z)` → return false (player cannot reach / place there)
3. `y == 255 && blockToPlace.bZ.d()` → return false (at height limit and block is solid)
4. `world.a(x, y, z, blockToPlace, true)` → canBlockStay check; if false, return false
5. `!world.k(x, y, z)` → return false (block cannot be replaced at that position)

### Step 3 — Place the block

```java
world.d(x, y, z, this.a);         // setBlock(x, y, z, blockId)
world.a(x, y, z, this.a);         // onBlockPlaced(x, y, z, blockId)
world.e(x, y, z, this.a);         // onBlockAdded(x, y, z, blockId)
```

### Step 4 — Play placement sound

```java
wu sound = yy.k[blockId].bX;     // StepSound of the placed block
world.a(x+0.5, y+0.5, z+0.5,
        sound.d(),                 // step sound name (e.g. "step.stone")
        (sound.f() + 1.0F) / 2.0F,  // volume (halved+0.5)
        sound.e() * 0.8F);          // pitch (slightly lowered)
```

### Step 5 — Decrement stack

```java
stack.a -= 1;
```

### Step 6 — Return true

---

## 5. Key Validity Conditions Summary

| Guard | Meaning |
|---|---|
| `stack.a <= 0` | No items left in hand |
| `!player.a_(world, x, y, z)` | Build height or chunk not loaded |
| `y == 255 && solid` | At world height limit |
| `!world.a(x,y,z,block,true)` | canBlockStay returned false (e.g. torch needs surface) |
| `!world.k(x,y,z)` | Target block is not replaceable (not air/grass/etc.) |

---

## 6. Placement Sound Notes

The step sound's `d()` method returns the sound key string.
`f()` = volume field; `e()` = pitch field.
Placement volume = `(vol + 1.0F) / 2.0F` — always between 0.5 and 1.0.
Placement pitch = `pitch * 0.8F` — slightly lower than walking pitch.

---

*Spec written by Analyst AI from `uw.java`. No C# implementation consulted.*
