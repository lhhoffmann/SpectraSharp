# Spec: BlockCocoaPlant (Second Attempt)

**Status:** PROVIDED — WITH CRITICAL FINDING
**Canonical name:** BlockCocoaPlant

---

## CRITICAL: Block ID 127 Does Not Exist in Minecraft 1.0

After exhaustive search of the decompiled block registry (`yy.java`), **there is no
block at ID 127 in Minecraft 1.0.0**. The block list ends at ID 122.

### Complete end of block registry (yy.java, fields `bJ`–`bK`)

```java
public static final yy bI = new rl(120).a(h).a(0.125F).c(-1.0F).a("endPortalFrame").l().b(6000000.0F);
public static final yy bJ = new yy(121, 175, p.e).c(3.0F).b(15.0F).a(f).a("whiteStone");    // End Stone
public static final yy bK = new aci(122, 167).c(3.0F).b(15.0F).a(f).a(0.125F).a("dragonEgg");
```

IDs 123, 124, 125, 126, 127 are unregistered — `yy.k[123]` through `yy.k[127]` are `null`.

---

## Cocoa Plant History

| Version | Status |
|---|---|
| **1.0.0** | Cocoa beans exist as a **dye item only** (ItemDye, ID 351 meta 3). No placed block. |
| **12w06a (pre-1.2)** | Cocoa plant added as a placed block on jungle trees. Block ID 127. |
| **1.2.1** | First release with cocoa plant block (ID 127). |

The Coder's request assumed cocoa plants are present in 1.0 — they are not.

---

## In 1.0: Cocoa Beans as Item

Cocoa beans exist in 1.0 as a dye ingredient only:
- Item: `acy` (ItemDye), the dye item class
- Meta 3 of the dye item = cocoa beans (brown)
- No growth stages, no placed block, no jungle log attachment
- Used only in crafting (cookie recipe, brown dye)

---

## Action for the Coder

Do **not** implement `BlockCocoaPlant.cs` as a 1.0 block. Options:

1. **Skip entirely for 1.0 target** — block ID 127 is not registered and the block
   does not exist in the 1.0 block array.
2. **Stub as version gate** — add a `[Version("1.2.1+")]` attribute and leave
   unimplemented until 1.2 parity work begins.
3. **Document in versioned folder** — future work goes in `Bridge/Overrides/v1_2/`.

The first prior `BlockCocoaPlant_Spec.md` documented Cauldron (`ic`, ID 118) and
BrewingStand (`ahp`, ID 117). Those are correct 1.0 blocks. This file supersedes
the original and clarifies that the cocoa plant search found nothing in 1.0.

---

## C# Mapping

| Java | C# |
|---|---|
| None in 1.0 | No `BlockCocoaPlant.cs` for 1.0 |
| Future: block ID 127 (1.2.1+) | Future: `Bridge/Overrides/v1_2/BlockCocoaPlant.cs` |
| Cocoa beans item (dye meta 3) | `ItemDye` with `SubtypeIndex = 3` |

---

## Open Questions (deferred to 1.2 parity)

- Exact obfuscated class name for cocoa plant in 1.2 decompile
- Metadata bit layout: growth stage (bits 0-1?) + facing direction (bits 2-3?)
- AABB sizes per stage and facing
- Drop counts per stage
