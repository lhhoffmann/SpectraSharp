# ROLE: Mod Runtime Builder

> **Activate with:** `ACT AS CODER` — then reference this file for all mod system work.
> No clean-room protocol. No air-gap. Standard software engineering.

---

## The Core Principle — Read This First

**The Mod Runtime is built BEFORE and INDEPENDENTLY of SpectraEngine.Core.**

Core is not finished and will not be finished for a long time — especially not for versions
above 1.0. That is intentional. The Mod Runtime defines the *interfaces* that Core must
eventually implement. Core grows to fit the Runtime, not the other way around.

```
Mod Runtime defines:   IWorld, IPlayer, IChunk, IBlockRegistry, ...
Core implements:       World : IWorld, Player : IPlayer, ...
```

MinecraftStubs NEVER import `SpectraEngine.Core` directly.
They ONLY reference `SpectraEngine.Contracts` (interfaces).
This means the entire Mod Runtime compiles and runs even when Core is empty.

---

## What You Are Building

The SpectraEngine Mod Runtime — a system that:

1. Takes any Minecraft mod `.jar` from any version
2. Compiles it to a `.dll` via IKVM (`ikvmc`)
3. Loads it at game runtime with full Java semantics via `IKVM.Runtime`
4. Routes all `net.minecraft.*` API calls through `MinecraftStubs` → `SpectraEngine.Contracts`
5. Intercepts Mixin/ASM patches and redirects them to Harmony patches on SpectraEngine classes
6. Catches allocations before they hit the GC (AllocGuard)
7. Isolates mods from each other and from the engine (ModSandbox)

---

## Full Architecture

### Install Time (once per mod)

```
User drops Mod.jar in mods/
      │
      ▼ VersionDetector
      determines: 1.0 / 1.7.10 / 1.12.2 / 1.16.5 / 1.18.2 / 1.21 / ...
      │
      ▼ MappingLoader
      loads Mappings/Data/{version}.json
      → ClassMap, MethodMap, FieldMap
      │
      ▼ ikvmc (subprocess)
        -reference: IKVM.Runtime.dll       (java.lang.*, java.util.*, reflection)
        -reference: MinecraftStubs.{ver}.dll  (net.minecraft.*)
      → Mod.dll  ✓  (no JAR needed at runtime)
```

### Game Runtime (every session)

```
Mod.dll
  │
  ╔══════════════════════════════════════════════════════════╗
  ║                     ModSandbox                           ║
  ║  wraps EVERY mod call                                    ║
  ║  catch Exception      → DisableMod(), engine continues   ║
  ║  catch OOM            → KillMod()                        ║
  ║  catch StackOverflow  → KillMod()                        ║
  ║  Watchdog 500ms       → KillMod()                        ║
  ║                                                          ║
  ║  ┌────────────────────────────────────────────────────┐  ║
  ║  │                IKVM.Runtime                        │  ║
  ║  │  java.lang.*  → synchronized, static init order   │  ║
  ║  │  java.util.*  → HashMap, ArrayList, Iterator, ... │  ║
  ║  │  reflection   → getDeclaredField, setAccessible   │  ║
  ║  │  threading    → Thread, Runnable, wait/notify     │  ║
  ║  │  exceptions   → Java hierarchy on .NET exceptions │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ║                          │ net.minecraft.* calls          ║
  ║  ┌───────────────────────▼────────────────────────────┐  ║
  ║  │               ReflectionGuard                      │  ║
  ║  │  setAccessible on stub fields     → ALLOW          │  ║
  ║  │  setAccessible on Core internals  → BLOCK          │  ║
  ║  │  getClass().getName()             → Java name      │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ║                          │                               ║
  ║  ┌───────────────────────▼────────────────────────────┐  ║
  ║  │               ThreadGuard                          │  ║
  ║  │  world call on wrong thread?                       │  ║
  ║  │  → Engine.ScheduleNextTick(call)                   │  ║
  ║  │  never crashes, never silent                       │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ║                          │                               ║
  ║  ┌───────────────────────▼────────────────────────────┐  ║
  ║  │               AllocGuard                           │  ║
  ║  │                                                    │  ║
  ║  │  Tier 1 — Value Type Promotion (compile-time)      │  ║
  ║  │    BlockPos   → readonly record struct → stack     │  ║
  ║  │    Vec3       → readonly record struct → stack     │  ║
  ║  │    ChunkPos   → readonly record struct → stack     │  ║
  ║  │    EnumFacing → C# enum → stack                   │  ║
  ║  │    zero GC pressure, zero runtime cost             │  ║
  ║  │                                                    │  ║
  ║  │  Tier 2 — Frame Pool (thread-local, tick-reset)    │  ║
  ║  │    ItemStack        → FramePool.RentItemStack()    │  ║
  ║  │    BlockBreakEvent  → FramePool.RentEvent<T>()     │  ║
  ║  │    all Forge events → Pool                         │  ║
  ║  │    reset at Engine.FixedUpdate() boundary          │  ║
  ║  │                                                    │  ║
  ║  │  Tier 3 — AllocationMonitor (DEBUG builds only)    │  ║
  ║  │    counts remaining allocs per type per tick       │  ║
  ║  │    warns when type exceeds threshold               │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ║                          │                               ║
  ║  ┌───────────────────────▼────────────────────────────┐  ║
  ║  │          MinecraftStubs.{ver}.dll                  │  ║
  ║  │                                                    │  ║
  ║  │  net.minecraft.world.World                         │  ║
  ║  │    setBlock(x,y,z,id)  → IWorld.SetBlock()         │  ║
  ║  │    getBlockId(x,y,z)   → IWorld.GetBlockId()       │  ║
  ║  │    getChunk(cx,cz)     → IWorld.GetChunk()         │  ║
  ║  │                                                    │  ║
  ║  │  net.minecraft.block.Block                         │  ║
  ║  │    blocksList[id] = x  → IBlockRegistry.Register() │  ║
  ║  │    blocksList[id]      → IBlockRegistry.Get()      │  ║
  ║  │                                                    │  ║
  ║  │  net.minecraftforge.*                              │  ║
  ║  │    EVENT_BUS.post(ev)  → IEventBus.Post()          │  ║
  ║  │    GameRegistry.reg.() → IBlockRegistry / IItemReg │  ║
  ║  │                                                    │  ║
  ║  │  net.fabricmc.api.ModInitializer                   │  ║
  ║  │    onInitialize()      → ISpectraMod.OnLoad()      │  ║
  ║  │                                                    │  ║
  ║  │  sun.misc.Unsafe                                   │  ║
  ║  │    → .NET unsafe / Pointer<T> equivalents          │  ║
  ║  │                                                    │  ║
  ║  │  System.loadLibrary()                              │  ║
  ║  │    → Log.Warn + graceful skip (JNI not supported)  │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ║                          │                               ║
  ║  ┌───────────────────────▼────────────────────────────┐  ║
  ║  │             MixinInterceptor                       │  ║
  ║  │                                                    │  ║
  ║  │  @Mixin(World.class)                               │  ║
  ║  │    ClassMapping.Resolve("World") → IWorld impl     │  ║
  ║  │    HarmonyBridge.Apply(csType, mixinDescriptor)    │  ║
  ║  │                                                    │  ║
  ║  │  @Inject   → Harmony Prefix / Postfix              │  ║
  ║  │  @Overwrite→ Harmony Prefix + skip original        │  ║
  ║  │  @Redirect → Harmony Transpiler                    │  ║
  ║  │  @Shadow   → no patch (stub exposes the field)     │  ║
  ║  │  unknown target → Log.Warn + skip (no crash)       │  ║
  ║  └───────────────────────┬────────────────────────────┘  ║
  ╚═════════════════════════════════════════════════════════╝
                             │
                             ▼
                  SpectraEngine.Contracts
                  (interfaces only — no Core dependency)
                             │
                             ▼
                  SpectraEngine.Core
                  (implements contracts — built separately, later)
```

---

## Project Structure

```
SpectraEngine.Contracts/              ← interfaces only, NO implementation
  IWorld.cs                          defines what world can do
  IChunk.cs
  IPlayer.cs
  IEntity.cs
  IBlockRegistry.cs
  IItemRegistry.cs
  IEntityRegistry.cs
  IEventBus.cs
  ICraftingManager.cs                (already exists in Core/Mods/)
  ISmeltingManager.cs                (already exists in Core/Mods/)
  IEngine.cs
  ISpectraMod.cs

SpectraEngine.ModRuntime/             ← the runtime, refs Contracts only
  ModLoader.cs                       discovers JARs/DLLs, dependency sort
  ModLifecycle.cs                    load / unload / reload per mod
  VersionDetector.cs                 JAR → GameVersion enum

  Sandbox/
    ModSandbox.cs                    exception + OOM + StackOverflow isolation
    ModWatchdog.cs                   500ms timeout → KillMod()
    ThreadGuard.cs                   wrong-thread calls → marshal to tick thread
    ReflectionGuard.cs               block Core internals, allow stub fields
    MemoryGuard.cs                   per-mod allocation budget

  Interop/
    IkvmBridge.cs                    IKVM.Runtime init, classloader config
    MixinInterceptor.cs              @Mixin → Harmony redirect
    HarmonyBridge.cs                 manages patches per mod, unpatches on unload
    AsmInterceptor.cs                direct ASM manipulation → Harmony

  AllocGuard/
    FramePool.cs                     thread-local pool, reset at tick boundary
    ValueTypePromotion.cs            registry of which Java types become structs
    ObjectPool.cs                    generic pool for mutable types
    AllocationMonitor.cs             DEBUG only — counts allocs per type/tick

  Mappings/
    ClassMapping.cs                  version-aware class name resolution
    MethodMapping.cs
    FieldMapping.cs
    VersionDetector.cs
    Data/
      1.0.json
      1.7.10.json
      1.12.2.json
      1.16.5.json
      1.18.2.json
      1.20.1.json
      1.21.json

Tools/ModRuntime/
  Compiler/
    ModCompiler.cs                   subprocess wrapper for ikvmc
    StubLinker.cs                    selects MinecraftStubs version for ikvmc link

Bridge/JavaStubs/                    MinecraftStubs — one folder per MC version
  v1_0/
    Block.cs  World.cs  Item.cs  BaseMod.cs  ModLoader.cs
  v1_12/
    Block.cs  World.cs  Item.cs  Chunk.cs  Entity.cs
    forge/
      GameRegistry.cs  MinecraftForge.cs  EventBus.cs  FMLCommonHandler.cs
  v1_16/
    Block.cs  Level.cs  Item.cs  Entity.cs
    forge/ ...
    fabric/
      ModInitializer.cs  FabricLoader.cs  FabricAPI.cs
  v1_21/
    Block.cs  Level.cs  Item.cs  Entity.cs
    forge/ ...
    fabric/ ...
    neoforge/ ...
```

---

## The Contracts Layer — Define It First

Before writing any stub, define the interface in `SpectraEngine.Contracts`.
This is the contract that Core must implement. Keep it minimal and stable.

```csharp
// SpectraEngine.Contracts/IWorld.cs
public interface IWorld
{
    int  GetBlockId(int x, int y, int z);
    void SetBlock(int x, int y, int z, int blockId);
    void SetBlockNotify(int x, int y, int z, int blockId);
    int  GetBlockMeta(int x, int y, int z);
    void SetBlockMeta(int x, int y, int z, int meta);
    bool IsAir(int x, int y, int z);
    bool CanSeeSky(int x, int y, int z);
    bool IsClientSide { get; }
    Random Random { get; }
    // ... extend as stubs need it
}
```

The stub then wraps this:

```csharp
// Bridge/JavaStubs/v1_0/World.cs
namespace net.minecraft.world
{
    public class World
    {
        internal readonly IWorld _core;
        public World(IWorld core) => _core = core;

        public int getBlockId(int x, int y, int z) => _core.GetBlockId(x, y, z);
        public void setBlock(int x, int y, int z, int id) => _core.SetBlock(x, y, z, id);
        // ...
    }
}
```

If Core does not implement a method yet → stub throws `NotImplementedException` with
a clear message. The game does not crash — ModSandbox catches it and disables that mod.

---

## Key Compatibility Rules

### Block.blocksList — proxy array

Mods write directly to static arrays in all versions up to 1.12:

```java
Block.blocksList[125] = new MyBlock(125);
```

The stub must present a proxy array:

```csharp
public static readonly BlockListProxy blocksList = new();

class BlockListProxy
{
    public BlockBase? this[int id]
    {
        get => _registry.Get(id);      // → IBlockRegistry.Get()
        set { if (value != null) _registry.Register(id, value); }
    }
    public int Length => 4096;
}
```

### getClass().getName() — Java identity

Every stub class must return its Java name:

```csharp
public class Block
{
    public virtual string getJavaClassName() => "net.minecraft.block.Block";
}
```

IKVM routes `getClass().getName()` through this method.

### Reflection on private fields

Stubs expose fields that mods may access via reflection:

```csharp
public class World
{
    // Accessible via java.lang.reflect — routes to IWorld
    [JavaField("loadedEntityList")]
    private readonly EntityListProxy loadedEntityList;
}
```

ReflectionGuard allows this. Access to `SpectraEngine.Core.*` internals is blocked.

### Thread violations

```csharp
public void setBlock(int x, int y, int z, int id)
{
    if (!Engine.IsTickThread)
    {
        Engine.ScheduleNextTick(() => _core.SetBlock(x, y, z, id));
        return;
    }
    _core.SetBlock(x, y, z, id);
}
```

### JNI / native libraries

```csharp
// In IKVM's java.lang.System stub override:
public static void loadLibrary(string name)
{
    Log.Warn($"[ModRuntime] Mod requested native library '{name}' — not supported, skipped");
    // Do not throw — some mods load optionally and handle the failure
}
```

---

## AllocGuard — Implementation Order

**Step 1 — Value types in stubs (do this first, biggest gain)**

```csharp
// Bridge/JavaStubs/v1_12/util/BlockPos.cs
public readonly record struct BlockPos(int X, int Y, int Z)
{
    public BlockPos up()    => this with { Y = Y + 1 };
    public BlockPos north() => this with { Z = Z - 1 };
    public BlockPos south() => this with { Z = Z + 1 };
    public BlockPos east()  => this with { X = X + 1 };
    public BlockPos west()  => this with { X = X - 1 };
    public long toLong()    => ((long)X & 0x3FFFFFF) << 38
                             | ((long)Z & 0x3FFFFFF) << 12
                             | ((long)Y & 0xFFF);
}
```

**Step 2 — Frame pool for ItemStack**

```csharp
// ModRuntime/AllocGuard/FramePool.cs
public static class FramePool
{
    const int PoolSize = 512;

    [ThreadStatic] static int _cursor;
    [ThreadStatic] static PooledItemStack[]? _stacks;

    public static PooledItemStack RentItemStack(int itemId, int count)
    {
        _stacks ??= new PooledItemStack[PoolSize];
        if (_cursor >= PoolSize) return new PooledItemStack(itemId, count); // fallback
        var obj = _stacks[_cursor] ??= new PooledItemStack();
        _cursor++;
        obj.Reset(itemId, count);
        return obj;
    }

    // Called by Engine.FixedUpdate() at end of every tick
    public static void EndFrame() => _cursor = 0;
}
```

**Step 3 — Event pool for Forge events**

Same pattern as ItemStack pool. One pool per event type.

**Step 4 — AllocationMonitor**

Only compiled in DEBUG. Tracks any type that escapes the pool.
Output: `[AllocGuard] WARNING: net.minecraft.item.ItemStack — 2847 allocs/tick`

---

## MixinInterceptor — Implementation

```csharp
// ModRuntime/Interop/MixinInterceptor.cs
public class MixinInterceptor
{
    readonly ClassMapping _mapping;
    readonly HarmonyBridge _harmony;

    // Called by IKVM when the Mixin framework tries to transform a class
    public void OnTransform(string javaClassName, MixinDescriptor mixin)
    {
        var csType = _mapping.Resolve(javaClassName);
        if (csType == null)
        {
            Log.Warn($"[Mixin] Target '{javaClassName}' not mapped — skipped");
            return;  // never crash
        }

        foreach (var injection in mixin.Injections)
        {
            switch (injection.Kind)
            {
                case MixinKind.Inject:    _harmony.ApplyPostfix(csType, injection); break;
                case MixinKind.Overwrite: _harmony.ApplyOverwrite(csType, injection); break;
                case MixinKind.Redirect:  _harmony.ApplyTranspiler(csType, injection); break;
                case MixinKind.Shadow:    break; // stub already exposes the field
                default:
                    Log.Warn($"[Mixin] Unknown injection kind {injection.Kind} — skipped");
                    break;
            }
        }
    }
}
```

---

## ModSandbox — Implementation

```csharp
// ModRuntime/Sandbox/ModSandbox.cs
public class ModSandbox(string modName, IEngine engine)
{
    public bool IsAlive { get; private set; } = true;

    public void Execute(Action modCode)
    {
        if (!IsAlive) return;

        using var watchdog = new Timer(_ => KillMod("tick timeout (>500ms)"),
                                       null, 500, Timeout.Infinite);
        try
        {
            modCode();
        }
        catch (OutOfMemoryException)        { KillMod("out of memory"); }
        catch (StackOverflowException)      { KillMod("stack overflow"); }
        catch (Exception ex)
        {
            Log.Error($"[{modName}] {ex.GetType().Name}: {ex.Message}");
            DisableMod();  // disable but don't kill engine
        }
    }

    void KillMod(string reason)
    {
        Log.Error($"[{modName}] KILLED — {reason}");
        HarmonyBridge.UnpatchAll(modName);      // remove all Harmony patches
        engine.Registries.UnregisterMod(modName); // remove blocks/items
        IsAlive = false;
    }

    void DisableMod()
    {
        Log.Warn($"[{modName}] Disabled due to error");
        IsAlive = false;
    }
}
```

---

## Version Mappings — Format

```json
// Mappings/Data/1.0.json
{
  "version": "1.0",
  "obfuscated": true,
  "classes": {
    "yy":  "net.minecraft.block.Block",
    "sr":  "net.minecraft.item.Item",
    "acy": "net.minecraft.item.ItemTool",
    "dk":  "net.minecraft.item.ItemStack",
    "ky":  "net.minecraft.world.gen.feature.WorldGenMineable",
    "ig":  "net.minecraft.world.gen.feature.WorldGenerator"
  },
  "stubs_version": "v1_0"
}
```

```json
// Mappings/Data/1.21.json
{
  "version": "1.21",
  "obfuscated": false,
  "mojmap": true,
  "loaders": ["forge", "fabric", "neoforge"],
  "stubs_version": "v1_21"
}
```

Mojmap (1.14+) is published by Mojang at:
`https://piston-meta.mojang.com/v1/packages/.../client_mappings.txt`
Download once per version, commit to `Mappings/Data/`.

---

## Implementation Order

Build in this sequence — each phase is independently useful:

| Phase | What | Result |
|---|---|---|
| **1** | `SpectraEngine.Contracts` interfaces | compile target for everything |
| **2** | `VersionDetector` + Mapping JSON for 1.0 | knows what version a JAR is |
| **3** | `ModCompiler` (ikvmc wrapper) | turns JAR → DLL |
| **4** | `MinecraftStubs v1_0` + `ModLoader` | 1.0 mods load |
| **5** | `AllocGuard` Tier 1 (value types) | no GC for BlockPos/Vec3 |
| **6** | `ModSandbox` + `ModWatchdog` | engine survives bad mods |
| **7** | `AllocGuard` Tier 2 (pools) | no GC for ItemStack/Events |
| **8** | `ReflectionGuard` + `ThreadGuard` | compatibility edge cases |
| **9** | `MixinInterceptor` + `HarmonyBridge` | Fabric/Forge 1.12+ mods |
| **10** | `MinecraftStubs v1_12` + mapping | 1.12 mods |
| **11** | `MinecraftStubs v1_21` + mapping | modern mods |

---

## What "100% Support" Means

| Category | Coverage | How |
|---|---|---|
| Java language features | 100% | IKVM.Runtime |
| java.util.*, java.lang.* | 100% | IKVM.Runtime |
| Java reflection | 100% | IKVM.Runtime + ReflectionGuard |
| Vanilla API calls | 100% | MinecraftStubs per version |
| ModLoader (1.0–1.6) | 100% | v1_0 stubs |
| Forge (1.6–1.20) | 100% | forge/ stubs |
| Fabric (1.14+) | 100% | fabric/ stubs |
| NeoForge (1.20.2+) | 100% | neoforge/ stubs |
| Mixin / SpongePowered | 100% | MixinInterceptor → Harmony |
| ASM direct manipulation | 100% | AsmInterceptor → Harmony |
| Thread violations | healed | ThreadGuard marshals |
| Mod crashes | isolated | ModSandbox, engine continues |
| sun.misc.Unsafe | ~90% | .NET unsafe equivalents |
| JNI native libraries | ~60% | known libs via DllImport, rest graceful skip |

JNI is the only hard limit. Less than 1% of mods use it.

---

## What Must NOT Be Done

- Do NOT import `SpectraEngine.Core` from `MinecraftStubs` — only `SpectraEngine.Contracts`
- Do NOT throw unhandled exceptions from stubs — always catch and log
- Do NOT silently drop mod calls — either route them or log a warning
- Do NOT apply Harmony patches outside of `HarmonyBridge` — mod patches must be trackable and removable
- Do NOT assume Core is complete — write stubs against interfaces, throw `NotImplementedException` for unimplemented paths

---

---

### RULE: Automated Testing (xUnit) — Conditional Generation

To manage token limits and focus on correctness, follow a strict 80/20 testing rule.

Evaluate the C# class you just generated. You **MUST** generate an accompanying xUnit test
class (`[ClassName]Tests.cs`) in the same output **ONLY IF** the class meets **AT LEAST ONE**
of the following criteria:

**ModTranspiler criteria:**

1. **Silent misclassification risk (`ModDiffer`):** Any class that tags mod classes as
   `NEW_CONTENT / OVERRIDE / PASSTHROUGH / LIBRARY`. A wrong tag silently breaks the mod —
   either logic is dropped or a vanilla class is double-patched. Always test with known
   class name lists and verify the exact tag returned.

2. **Data extraction from AST (`ManifestBuilder`):** Any class that reads a Java AST and
   extracts structured data (block IDs, texture indices, method injection points). Wrong
   extraction produces a subtly incorrect mod with no compile error. Test with inline Java
   source strings and assert exact field values in the resulting descriptor.

3. **Code generation / templates (`Translator`, `BlockTemplate`, `ItemTemplate`, `HookTemplate`):**
   Any class that produces C# source strings. Output must be deterministic — test with a
   fixed input and compare against an expected output string constant. If the output changes,
   the constant must be deliberately updated (no silent drift).

4. **VanillaApiMap / VanillaClassList lookups:** The translation tables are hardcoded —
   test that known Java call signatures map to the correct C# equivalents, and that unknown
   calls produce a `// TODO:` comment rather than being silently dropped.

**MinecraftStubs criteria:**

5. **Stub delegation chain:** Any `MinecraftStub` class that delegates a Java API call to a
   `SpectraEngine.Contracts` interface. Test that the correct interface method is called with
   the correct arguments. Use a hand-written fake that implements the interface and records
   calls (e.g., `class FakeWorld : IWorld`).

**If the class meets none of these criteria** (e.g., it is a data model like `BlockDescriptor`,
a CLI argument parser, `Program.cs`, or `TypeMap`), **DO NOT GENERATE ANY TESTS.**

#### Technical Requirements for Tests (if generated)

- **Framework:** `xUnit` (`[Fact]`, `[Theory]`, `[InlineData]`).
- **NO MOCKING LIBRARIES:** Do NOT use `Moq`, `NSubstitute`, or any reflection-based mocking
  framework. Use ONLY hand-written fakes/stubs directly in the test file.
- **No subprocess calls:** Do NOT invoke Vineflower or `java` from within unit tests. The
  `IDecompiler` boundary must be faked with an implementation that returns a pre-written
  Java source string.
- **No file system dependency:** Tests must run without reading or writing disk. Pass Java
  source as inline strings, assert C# output as inline strings.
- **Golden Master:** For all code generation classes, store the expected C# output as a
  string constant in the test. Treat any unexpected change in output as a test failure.

---

## Session End Checklist

Append to `Documentation/METRICS.md`:

```
## YYYY-MM-DD — [MOD-CODER] — <topic>

**Worked on:**
- <component> — <one-line description>

**Estimated effort:** ~N hours equivalent
**Notes:** <decisions, blockers, open questions>
```
