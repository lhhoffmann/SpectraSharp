# ROLE: Mod Transpiler — Internal Analysis Logic

> **This is NOT an AI workflow document.**
> It describes what the `ModTranspiler.exe` program does internally when it processes a mod JAR.
> Use this as the specification for building the analysis phases of `Tools/ModTranspiler/`.

---

## What the Transpiler Does (Phases 1–2)

When the transpiler receives a mod JAR it runs two analysis phases before translation:

### Phase 1 — Decompilation

Invoke Vineflower as a subprocess:

```
java -jar tools/decompiler/vineflower.jar <modJarPath> <outputDir>
```

- Input: `mods/<ModName>.jar`
- Output: `temp/mods/<ModName>/*.java`
- The output directory is temp (gitignored) — it exists only during processing

### Phase 2 — Class Diff

Compare every class in the mod JAR against the known vanilla class list
(`Tools/ModTranspiler/Mappings/VanillaClassList.cs`).

**Classification rules:**

| Condition | Tag | Action |
|---|---|---|
| Class name not in vanilla list | `NEW_CONTENT` | Translate as new Block/Item/Entity |
| Class name in vanilla list AND bytecode differs | `OVERRIDE` | Translate as HarmonyLib patch |
| Class name in vanilla list AND bytecode identical | `PASSTHROUGH` | Skip — mod just bundled vanilla |
| Class is in `com.jcraft`, `paulscode`, `org.lwjgl` | `LIBRARY` | Skip — third-party lib |

Output: an in-memory classification map passed to Phase 3.

---

## Class Type Detection (Phase 3 input)

For each `NEW_CONTENT` class, detect what kind of object it is by inspecting its superclass:

| Java superclass (obfuscated) | Human name | C# target |
|---|---|---|
| `yy` | `Block` | Generate `BlockBase` subclass |
| `sr` | `Item` | Generate `ItemBase` subclass |
| `aef` | `Entity` | Generate `EntityBase` subclass |
| `ry` | `World` (direct subclass rare) | Generate world hook |
| anything else | Unknown | Best-effort translation + TODO comment |

For each `OVERRIDE` class, detect which vanilla method was changed by diffing
the method bodies against the vanilla decompiled source in `temp/decompiled/`.

---

## Data Extraction per Class Type

### Block classes — extract these fields:

```
blockId          → first int argument to super constructor
textureFace      → value assigned to blockIndexInTexture field
hardness         → argument to .c() / setHardness() call
blastResistance  → argument to .b() / setResistance() call
lightEmission    → argument to .a(float) / setLightValue() call
lightOpacity     → argument to .h(int) / setLightOpacity() call
unlocalizedName  → argument to final .a(String) / setBlockName() call
material         → first yy.* static field argument
```

### Item classes — extract these fields:

```
itemId           → first int argument to super constructor
textureIndex     → value assigned to iconIndex field
maxStackSize     → argument to setMaxStackSize()
maxDamage        → argument to setMaxDamage()
unlocalizedName  → argument to setItemName()
```

### Override classes — extract injection points:

For each method that differs from vanilla:
```
targetClass      → the vanilla class being overridden
targetMethod     → the method name that changed
changeType       → PREPEND (new code at top) | APPEND (new code at bottom) | REPLACE
changedLogic     → the added/changed code block (as Java AST nodes, not text)
```

---

## Output of Analysis Phases

The analysis phases produce a `ModManifest` object — a structured in-memory representation
of everything the mod adds or changes. This is passed directly to the Translator (Phase 4).

```
ModManifest
├── NewBlocks[]        ← BlockDescriptor per NEW_CONTENT Block subclass
├── NewItems[]         ← ItemDescriptor per NEW_CONTENT Item subclass
├── NewEntities[]      ← EntityDescriptor per NEW_CONTENT Entity subclass
├── Overrides[]        ← InjectionDescriptor per OVERRIDE class
├── NewRecipes[]       ← RecipeDescriptor (extracted from CraftingManager calls)
└── WorldGenHooks[]    ← WorldGenDescriptor (extracted from generateSurface calls)
```

No files are written at this stage — the manifest is pure in-memory data.
