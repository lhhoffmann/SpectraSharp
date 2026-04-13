# SpectraSharp Mod Documentation — Index

This folder documents the **Mod Transpiler Pipeline**: the system that automatically
converts Java-based Minecraft 1.0 mods into native C# plugins for SpectraSharp.

> No clean-room protocol applies here. The Mod Transpiler is a compiler —
> it reads Java by design. Building it is standard software engineering.

---

## How It Works

```
User drops mod.jar into /mods/
        │
        ▼
[ModWatcher] detects new unprocessed JAR
        │
        ▼
[ModTranspilerService.ProcessAsync(jar)]
        │
        ├─ Phase 1: Vineflower decompiles mod.jar → temp/mods/<ModName>/*.java
        │
        ├─ Phase 2: ModDiffer tags each class:
        │           NEW_CONTENT / OVERRIDE / PASSTHROUGH / LIBRARY
        │
        ├─ Phase 3: ANTLR4 parses Java AST → ModManifest
        │           (BlockDescriptor, ItemDescriptor, InjectionDescriptor, ...)
        │
        ├─ Phase 4: Translator converts ModManifest → C# source strings
        │           using VanillaApiMap lookup table
        │
        ├─ Phase 5: CodeEmitter writes Bridge/Mods/<ModName>/*.cs
        │
        └─ Phase 6: Roslyn compiles → mods/compiled/<ModName>.dll
                │
                ▼
        ModLoader.Load(dll)
        AssemblyLoadContext → HarmonyLib patches active
        ISpectraMod.OnLoad(engine) → mod is running
```

---

## Key Reference Files

| File | Purpose |
|---|---|
| [Protocols/MOD_TRANSPILER.md](../VoxelCore/Protocols/MOD_TRANSPILER.md) | Full pipeline architecture |
| [Protocols/ROLE_MOD_CODER.md](../VoxelCore/Protocols/ROLE_MOD_CODER.md) | How to implement each pipeline phase |
| [Protocols/MOD_ANALYSIS_INTERNALS.md](../VoxelCore/Protocols/MOD_ANALYSIS_INTERNALS.md) | Internal analysis logic (what the program does) |
| [Mappings/vanilla_api.md](Mappings/vanilla_api.md) | Java → C# API translation table (source for VanillaApiMap.cs) |

---

## Mod Registry

| Mod Name | JAR | Status |
|---|---|---|
| *(none yet — transpiler not yet built)* | | |

## Status Legend

| Status | Meaning |
|---|---|
| `[QUEUED]` | JAR in /mods/, transpiler not yet run |
| `[PROCESSING]` | Transpiler currently running |
| `[COMPILED]` | DLL built successfully |
| `[LOADED]` | Plugin active in engine |
| `[FAILED]` | Transpiler produced errors — check TODO comments in Bridge/Mods/ |
