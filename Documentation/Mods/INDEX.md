# SpectraSharp Mod Documentation — Index

This folder documents the **Mod Transpiler Pipeline**: the system that automatically
converts Java-based Minecraft 1.0 mods into native C# plugins for SpectraSharp.

## Pipeline Overview

```
User drops mod.jar into /mods/
        │
        ▼
[ModWatcher] detects new unprocessed JAR
        │
        ▼
[Vineflower] decompiles mod.jar → temp/mods/<ModName>/
        │
        ▼
[Mod Analyst AI]  reads ROLE_MOD_ANALYST.md
  - Diffs mod classes against vanilla (Documentation/VoxelCore/Parity/Mappings/)
  - Identifies: new content, hooks, overrides
  - Writes: Documentation/Mods/Specs/<ModName>.md
        │
        ▼
[Mod Coder AI]  reads ROLE_MOD_CODER.md
  - Reads Specs/<ModName>.md ONLY (air-gap maintained)
  - Writes: Bridge/Mods/<ModName>/
  - Uses HarmonyLib for runtime injection hooks
        │
        ▼
[ModCompiler] dotnet build → mods/compiled/<ModName>.dll
        │
        ▼
[ModLoader] AssemblyLoadContext.LoadFromAssemblyPath()
  Engine is now running the mod natively at AOT speed.
```

## Mod Registry

| Mod Name | JAR | Spec | Status |
|---|---|---|---|
| *(none yet)* | | | |

## Status Legend

| Status | Meaning |
|---|---|
| `[DETECTED]` | JAR found, not yet decompiled |
| `[DECOMPILED]` | Vineflower finished, ready for Analyst |
| `[SPECCED]` | Analyst wrote the Mod_Spec.md |
| `[CODED]` | Coder wrote the C# plugin |
| `[COMPILED]` | DLL built successfully |
| `[LOADED]` | Plugin active in engine |

## Key Files

| File | Purpose |
|---|---|
| [Protocols/ROLE_MOD_ANALYST.md](../VoxelCore/Protocols/ROLE_MOD_ANALYST.md) | Analyst role for mod analysis |
| [Protocols/ROLE_MOD_CODER.md](../VoxelCore/Protocols/ROLE_MOD_CODER.md) | Coder role for mod implementation |
| [Protocols/MOD_TRANSPILER.md](../VoxelCore/Protocols/MOD_TRANSPILER.md) | Full pipeline reference |
| [Mappings/vanilla_api.md](Mappings/vanilla_api.md) | Java → C# API translation table |
| Specs/ | One `.md` file per mod |
