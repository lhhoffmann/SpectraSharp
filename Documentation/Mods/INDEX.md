# SpectraEngine Mod Documentation — Index

This folder documents the **Mod Runtime**: the system that runs Java mod JARs of any
Minecraft version natively on .NET without decompilation or source translation.

> No clean-room protocol applies. Executing Java bytecode is standard software engineering —
> we are not reimplementing anything, just hosting existing compiled code.

---

## Architecture Overview

```
User drops mod.jar into /mods/
        │
        ▼
[IkvmBridge.Initialize()]          ← one-time at startup
  - Set IKVM RelaxedVerification
  - Load MinecraftStubs.v*.dll into Default ALC
  - Prime ClassMapping with Core types
        │
        ▼
[ModLoader.LoadAll()]
        │
        ├─ Phase 1: VersionDetector.Detect(jar)
        │           Fingerprint scan → which MC version / loader?
        │
        ├─ Phase 2: ModCompiler.Compile(jar, mapping)
        │           ikvmc (IKVM compiler) translates JAR → .NET DLL
        │           Links against: MinecraftStubs.v*.dll + IKVM.Runtime.dll
        │           Output: mods/compiled/<ModName>.dll
        │
        ├─ Phase 3: AssemblyLoadContext.LoadFromAssemblyPath(dll)
        │           Isolated per-mod ALC (collectible for unload)
        │
        ├─ Phase 4: MixinInterceptor.Intercept(asm)
        │           Scans for @Mixin annotations (Fabric/Forge mods)
        │           ClassMapping.Resolve(javaClass) → SpectraEngine.Core type
        │           HarmonyBridge applies Harmony patches BEFORE OnLoad()
        │
        ├─ Phase 5: ClassMapping.ScanAssembly(asm)
        │           Auto-registers [JavaClassName] attributes
        │
        └─ Phase 6: ISpectraMod.OnLoad(engine)
                    Runs inside ModSandbox (500ms watchdog, OOM/SOF guard)
                    FramePool resets every tick (AllocGuard)
```

---

## Layer Diagram

```
mod.jar  ──ikvmc──►  Mod.dll
                        │
                        │  references
                        ▼
               MinecraftStubs.v*.dll
               (net.minecraft.* stubs)
                        │
                        │  delegates
                        ▼
               SpectraEngine.Core
               (IWorld, IEngine, etc.)

  Mixin patches (Fabric/Forge mods only):
  MixinInterceptor → ClassMapping → HarmonyBridge → HarmonyLib
```

---

## Key Files

| File | Purpose |
|---|---|
| [../VoxelCore/Protocols/ROLE_MOD_CODER.md](../VoxelCore/Protocols/ROLE_MOD_CODER.md) | Full architecture + implementation order |
| [Mappings/vanilla_api.md](Mappings/vanilla_api.md) | Java → C# API translation reference |

### Source locations

| Component | Path |
|---|---|
| Mod Runtime (loader, sandbox, AllocGuard) | `SpectraEngine.ModRuntime/` |
| IKVM init + ClassMapping + Mixin pipeline | `SpectraEngine.ModRuntime/Interop/` |
| Mod compiler (ikvmc wrapper) | `SpectraEngine.ModRuntime/Compiler/` |
| Version mappings JSON | `SpectraEngine.ModRuntime/Mappings/Data/` |
| MinecraftStubs v1.0 | `Bridge/JavaStubs/v1_0/` |

---

## Version Support Matrix

| Minecraft Version | Loader | Stubs | Mappings | Status |
|---|---|---|---|---|
| 1.0 | ModLoader | `MinecraftStubs.v1_0` | `1.0.json` (obfuscated) | In progress |
| 1.12.2 | Forge | *(planned)* | `1.12.2.json` (Searge) | Planned |
| 1.21+ | Fabric / NeoForge | *(planned)* | `1.21.json` (Mojmap) | Planned |

---

## Mod Registry

| Mod Name | JAR | MC Version | Status |
|---|---|---|---|
| *(no mods loaded yet)* | | | |

## Status Legend

| Status | Meaning |
|---|---|
| `[QUEUED]` | JAR in /mods/, compiler not yet run |
| `[COMPILED]` | ikvmc produced DLL successfully |
| `[LOADED]` | Mod active — ISpectraMod.OnLoad() succeeded |
| `[DISABLED]` | Mod threw unhandled exception — sandbox disabled it |
| `[KILLED]` | Mod timed out / OOM / StackOverflow — patches reverted |
| `[FAILED]` | ikvmc compile failed — check ModCompiler log |
