# MOD TRANSPILER — Full Pipeline Reference

The Mod Transpiler is a **standalone C# program** that ships with SpectraEngine.
It requires no AI, no API keys, and no internet connection.
The AI's role is only to **write this program** during development — once built,
it runs fully autonomously on the end-user's machine.

---

## Core Principle

```
DEVELOPMENT TIME (us, now):
  AI writes the Transpiler program once.

RUNTIME (end user, no AI involved):
  User drops mod.jar → Transpiler.exe → mymod.dll → engine loads it
```

---

## Architecture Overview

```
mods/mymod.jar
      │
      ▼
┌─────────────────────────────────────────────────────┐
│              ModTranspiler.exe                      │
│         (pure C# program, no AI, no network)        │
│                                                     │
│  Phase 1: Decompile                                 │
│    Vineflower (subprocess) → temp/mods/mymod/*.java │
│                                                     │
│  Phase 2: Diff                                      │
│    ModDiffer: mod classes vs vanilla class list     │
│    → NEW_CONTENT / OVERRIDE / PASSTHROUGH tags      │
│                                                     │
│  Phase 3: Parse                                     │
│    ANTLR4 Java grammar → Java AST per class         │
│                                                     │
│  Phase 4: Translate                                 │
│    JavaToCSharpTranslator:                          │
│    - API call rewriting (vanilla_api lookup table)  │
│    - Type mapping (int → int, float → float, etc.)  │
│    - Class → C# class skeleton                      │
│    - Method override → HarmonyLib patch             │
│                                                     │
│  Phase 5: Emit                                      │
│    Roslyn SourceGenerator → Bridge/Mods/mymod/*.cs  │
│                                                     │
│  Phase 6: Compile                                   │
│    Roslyn CSharpCompilation → mods/compiled/        │
│                                                     │
└─────────────────────────────────────────────────────┘
      │
      ▼
mods/compiled/mymod.dll
      │
      ▼
Engine: AssemblyLoadContext.LoadFromAssemblyPath()
        HarmonyLib.PatchAll(assembly)
        ISpectraMod.OnLoad(engine)
```

---

## Components to Build

### 1. ModTranspilerService (`Tools/ModTranspiler/`)

The main entry point. Can run:
- **Standalone:** `ModTranspiler.exe mods/mymod.jar`
- **Embedded:** called by the engine's `ModWatcher` on first detection of a new JAR

```
Tools/ModTranspiler/
├── ModTranspiler.csproj
├── Program.cs                  ← CLI entry point
├── Pipeline/
│   ├── Decompiler.cs           ← wraps Vineflower subprocess
│   ├── ModDiffer.cs            ← diffs mod classes vs vanilla list
│   ├── JavaParser.cs           ← ANTLR4 → Java AST
│   ├── Translator.cs           ← Java AST → C# source strings
│   ├── CodeEmitter.cs          ← writes .cs files to Bridge/Mods/
│   └── ModCompiler.cs          ← Roslyn: .cs files → .dll
├── Mappings/
│   ├── VanillaApiMap.cs        ← hardcoded Java→C# method translations
│   ├── VanillaClassList.cs     ← all known vanilla class names (for diff)
│   └── TypeMap.cs              ← Java type → C# type
└── Templates/
    ├── BlockTemplate.cs        ← generates BlockBase subclass source
    ├── ItemTemplate.cs         ← generates ItemBase subclass source
    ├── HookTemplate.cs         ← generates [HarmonyPatch] source
    └── EntryPointTemplate.cs   ← generates ISpectraMod source
```

---

### 2. VanillaApiMap (hardcoded lookup table)

This is the machine-readable version of `Documentation/Mods/Mappings/vanilla_api.md`.
Compiled directly into the transpiler — no runtime file reads needed.

```csharp
// Tools/ModTranspiler/Mappings/VanillaApiMap.cs
static class VanillaApiMap
{
    // Key:   Java method call as it appears in decompiled source
    // Value: C# equivalent call in SpectraEngine
    public static readonly Dictionary<string, string> MethodCalls = new()
    {
        ["world.getBlockId(?,?,?)"]          = "world.GetBlockId(?,?,?)",
        ["world.setBlock(?,?,?,?)"]          = "world.SetBlock(?,?,?,?)",
        ["world.setBlockWithNotify(?,?,?,?)"]= "world.SetBlockNotify(?,?,?,?)",
        ["world.getBlockMetadata(?,?,?)"]    = "world.GetBlockMeta(?,?,?)",
        ["world.scheduleBlockUpdate(?,?,?,?,?)"] = "world.ScheduleTick(?,?,?,?,?)",
        ["player.hurtPlayer(?,?)"]           = "player.Hurt(?,?)",
        ["player.sendChatMessage(?)"]        = "player.SendChat(?)",
        // ... full table derived from vanilla_api.md
    };

    public static readonly Dictionary<string, string> FieldAccess = new()
    {
        ["block.blockIndexInTexture"] = "block.TextureIndex",
        ["block.blockHardness"]       = "block.Hardness",
        ["player.posX"]               = "player.Position.X",
        ["player.posY"]               = "player.Position.Y",
        ["player.posZ"]               = "player.Position.Z",
        // ...
    };
}
```

---

### 3. JavaToCSharpTranslator

The core translation engine. Walks the ANTLR4 Java AST and emits C# source.

**Translation rules (priority order):**

1. **Known API call** → look up in `VanillaApiMap`, replace directly
2. **Block subclass** → emit `BlockBase` subclass via `BlockTemplate`
3. **Item subclass** → emit `ItemBase` subclass via `ItemTemplate`
4. **Method override of vanilla class** → emit `[HarmonyPatch]` via `HookTemplate`
5. **New standalone class** → best-effort direct translation (field-by-field, method-by-method)
6. **Unknown construct** → emit `// TODO: MANUAL REVIEW REQUIRED — <original java>` comment

Rule 6 is the safety net: the transpiler never silently drops logic.
Untranslatable code becomes a visible TODO in the output .cs file.

---

### 4. ModCompiler (Roslyn)

Compiles the generated C# files into a DLL without requiring the dotnet CLI.

```csharp
// Tools/ModTranspiler/Pipeline/ModCompiler.cs
var compilation = CSharpCompilation.Create(
    assemblyName: modName,
    syntaxTrees: generatedSources.Select(src =>
        CSharpSyntaxTree.ParseText(src)),
    references: new[]
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(ISpectraMod).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Harmony).Assembly.Location),
    },
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
);

using var ms = new MemoryStream();
var result = compilation.Emit(ms);

if (!result.Success)
{
    // Write diagnostics — mod fails gracefully, engine continues
    foreach (var diag in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
        Console.Error.WriteLine(diag.ToString());
    return false;
}

File.WriteAllBytes(outputDllPath, ms.ToArray());
return true;
```

---

### 5. ModWatcher (Engine Integration)

Watches the `/mods/` folder. Triggers the transpiler automatically on new JARs.
Runs in a background thread — does NOT block the game loop.

```
Engine start
  → ModWatcher.Start()
      → scans mods/*.jar
      → for each JAR without matching compiled/*.dll:
            ModTranspilerService.ProcessAsync(jar)  ← background task
      → for each JAR with valid compiled/*.dll:
            ModLoader.Load(dll)                     ← immediate

While game runs:
  → FileSystemWatcher on mods/
      → new .jar dropped → trigger transpiler in background
      → new .dll appears  → hot-load into AssemblyLoadContext
```

---

## NuGet Dependencies

| Package | Version | Used for |
|---|---|---|
| `Antlr4.Runtime.Standard` | 4.13.* | Java grammar parsing |
| `Lib.Harmony` | 2.3.* | Runtime hook injection |
| `Microsoft.CodeAnalysis.CSharp` | 4.x | Roslyn code gen + compile |

The ANTLR4 Java grammar file (`Java9.g4`) is included in the repo under
`Tools/ModTranspiler/Grammar/` — it is MIT-licensed, not copyrighted game content.

---

## What the Transpiler Cannot Handle Automatically

These cases produce `// TODO: MANUAL REVIEW REQUIRED` comments in the output:

- Custom OpenGL rendering calls (LWJGL → Raylib mapping is not 1:1)
- Reflection-based mod loading (e.g. ModLoader API introspection)
- Mixin-style bytecode manipulation (ASM library usage)
- Multithreaded mod code with `synchronized` blocks
- Custom network packet classes (if multiplayer is out of scope)

For these, the developer reviews the generated TODO comments and completes them manually.
The transpiler handles ~80% of typical 1.0-era mods automatically.

---

## File Layout

```
SpectraEngine/
├── Tools/
│   └── ModTranspiler/              ← the transpiler program (committed, built once)
│       ├── ModTranspiler.csproj
│       ├── Grammar/Java9.g4
│       └── ...
├── mods/                           ← user drop zone (gitignored)
│   ├── mymod.jar
│   └── compiled/mymod.dll
├── temp/mods/                      ← decompiled mod source (gitignored)
└── Bridge/Mods/                    ← generated C# plugin source (committed)
    └── mymod/
        └── *.cs
```
