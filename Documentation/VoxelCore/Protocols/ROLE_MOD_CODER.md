# ROLE: Mod Transpiler Builder

> **Activate with:** `ACT AS CODER` — then reference this file for the mod transpiler task.
> No clean-room protocol. No air-gap. This is standard software engineering.

---

## What You Are Building

`Tools/ModTranspiler/` — a standalone C# program that reads a Java mod JAR and outputs
a compiled C# plugin (.dll) that runs natively in SpectraSharp. No AI involved at runtime.

This is a **compiler**, similar in concept to Roslyn or ANTLR themselves.
Reading Java source is the entire point of this program — there is no legal concern.

---

## Project Setup

```xml
<!-- Tools/ModTranspiler/ModTranspiler.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>ModTranspiler</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.*" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"  Version="4.*" />
    <PackageReference Include="Lib.Harmony" Version="2.3.*" />
    <ProjectReference Include="../../SpectraSharp.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- ANTLR4 Java grammar — MIT licensed -->
    <Antlr4 Include="Grammar/JavaLexer.g4" />
    <Antlr4 Include="Grammar/JavaParser.g4" />
  </ItemGroup>
</Project>
```

---

## File Structure to Build

```
Tools/ModTranspiler/
├── ModTranspiler.csproj
├── Program.cs                        ← CLI: ModTranspiler.exe mods/mymod.jar
├── Grammar/
│   ├── JavaLexer.g4                  ← ANTLR4 Java lexer grammar (MIT)
│   └── JavaParser.g4                 ← ANTLR4 Java parser grammar (MIT)
├── Pipeline/
│   ├── Decompiler.cs                 ← Phase 1: Vineflower subprocess wrapper
│   ├── ModDiffer.cs                  ← Phase 2: mod vs vanilla class diff
│   ├── ManifestBuilder.cs            ← Phase 3: Java AST → ModManifest
│   ├── Translator.cs                 ← Phase 4: ModManifest → C# source strings
│   ├── CodeEmitter.cs                ← Phase 5: write .cs files to Bridge/Mods/
│   └── ModCompiler.cs                ← Phase 6: Roslyn compile → .dll
├── Model/
│   ├── ModManifest.cs                ← root output of analysis
│   ├── BlockDescriptor.cs
│   ├── ItemDescriptor.cs
│   ├── EntityDescriptor.cs
│   ├── InjectionDescriptor.cs
│   ├── RecipeDescriptor.cs
│   └── WorldGenDescriptor.cs
├── Mappings/
│   ├── VanillaApiMap.cs              ← Java method call → C# equivalent
│   ├── VanillaClassList.cs           ← all known vanilla obfuscated class names
│   └── TypeMap.cs                    ← Java type → C# type
└── Templates/
    ├── BlockTemplate.cs              ← emits BlockBase subclass source
    ├── ItemTemplate.cs               ← emits ItemBase subclass source
    ├── EntityTemplate.cs             ← emits EntityBase subclass source
    ├── HookTemplate.cs               ← emits [HarmonyPatch] source
    ├── RecipeTemplate.cs             ← emits recipe registration source
    └── EntryPointTemplate.cs         ← emits ISpectraMod entry point source
```

---

## Phase 1 — Decompiler.cs

Wraps Vineflower as a subprocess. Returns the output directory path.

```csharp
static class Decompiler
{
    public static string Decompile(string jarPath, string outputRoot)
    {
        string modName = Path.GetFileNameWithoutExtension(jarPath);
        string outDir  = Path.Combine(outputRoot, modName);
        Directory.CreateDirectory(outDir);

        var psi = new ProcessStartInfo("java",
            $"-jar tools/decompiler/vineflower.jar \"{jarPath}\" \"{outDir}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };

        using var proc = Process.Start(psi)!;
        proc.WaitForExit();

        if (proc.ExitCode != 0)
            throw new Exception($"Vineflower failed: {proc.StandardError.ReadToEnd()}");

        return outDir;
    }
}
```

---

## Phase 2 — ModDiffer.cs

Reads class names from the mod JAR (without decompiling) and tags each one.

```csharp
enum ClassTag { NewContent, Override, Passthrough, Library }

static class ModDiffer
{
    public static Dictionary<string, ClassTag> Diff(string jarPath)
    {
        var result = new Dictionary<string, ClassTag>();

        using var zip = ZipFile.OpenRead(jarPath);
        foreach (var entry in zip.Entries.Where(e => e.Name.EndsWith(".class")))
        {
            string className = entry.FullName.Replace('/', '.').Replace(".class", "");

            if (IsLibrary(className))       { result[className] = ClassTag.Library;    continue; }
            if (!VanillaClassList.Contains(className)) { result[className] = ClassTag.NewContent; continue; }

            // vanilla class exists — check if bytecode differs
            result[className] = BytecodeDiffers(entry, className)
                ? ClassTag.Override
                : ClassTag.Passthrough;
        }
        return result;
    }

    static bool IsLibrary(string name) =>
        name.StartsWith("com.jcraft") ||
        name.StartsWith("paulscode")  ||
        name.StartsWith("org.lwjgl");
}
```

---

## Phase 3 — ManifestBuilder.cs

Walks the ANTLR4 Java AST of each `NewContent` and `Override` class and builds the
`ModManifest`. See `MOD_ANALYSIS_INTERNALS.md` for the exact fields to extract per class type.

Key pattern for Block detection:

```csharp
// In the ANTLR4 visitor:
public override void EnterClassDeclaration(JavaParser.ClassDeclarationContext ctx)
{
    string superClass = ctx.typeType()?.GetText() ?? "";

    _currentDescriptor = superClass switch
    {
        "yy"  => new BlockDescriptor(),   // yy = Block
        "sr"  => new ItemDescriptor(),    // sr = Item
        "aef" => new EntityDescriptor(),  // aef = Entity
        _     => new UnknownDescriptor(),
    };
}
```

---

## Phase 4 — Translator.cs

Converts a `ModManifest` into a list of `(filename, sourceCode)` pairs using the Templates.

```csharp
static class Translator
{
    public static List<(string File, string Source)> Translate(ModManifest manifest, string modName)
    {
        var output = new List<(string, string)>();

        output.Add(EntryPointTemplate.Emit(modName, manifest));

        foreach (var block in manifest.NewBlocks)
            output.Add(BlockTemplate.Emit(modName, block));

        foreach (var item in manifest.NewItems)
            output.Add(ItemTemplate.Emit(modName, item));

        foreach (var hook in manifest.Overrides)
            output.Add(HookTemplate.Emit(modName, hook));

        foreach (var recipe in manifest.NewRecipes)
            output.Add(RecipeTemplate.Emit(modName, recipe));

        return output;
    }
}
```

---

## Phase 5 — CodeEmitter.cs

Writes the generated source files to `Bridge/Mods/<ModName>/`.

```csharp
static class CodeEmitter
{
    public static void Emit(string modName, List<(string File, string Source)> sources)
    {
        string root = Path.Combine("Bridge", "Mods", modName);
        Directory.CreateDirectory(root);

        foreach (var (file, source) in sources)
        {
            string path = Path.Combine(root, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, source);
        }
    }
}
```

---

## Phase 6 — ModCompiler.cs

Compiles the generated C# files to a DLL using Roslyn in-process (no dotnet CLI needed).

```csharp
static class ModCompiler
{
    public static bool Compile(string modName, string sourceRoot, string outputDll)
    {
        var sources = Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)));

        var refs = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ISpectraMod).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Harmony).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(modName, sources, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                Console.Error.WriteLine(d);
            return false;
        }

        File.WriteAllBytes(outputDll, ms.ToArray());
        return true;
    }
}
```

---

## VanillaApiMap — How to Build It

The `VanillaApiMap.cs` is populated directly from `Documentation/Mods/Mappings/vanilla_api.md`.
Every row in that table becomes one dictionary entry. The map is hardcoded at compile time —
no file reads at runtime.

The translator uses it like this:

```csharp
// In the ANTLR4 visitor for method calls:
string javaCall = $"{receiver}.{methodName}(?,?,?)";  // ? = argument placeholder

if (VanillaApiMap.MethodCalls.TryGetValue(javaCall, out string? csCall))
    return ReconstructWithArgs(csCall, args);
else
    return $"/* TODO: unknown call: {javaCall} */ {originalJava}";
```

---

## Session End Checklist

Before closing the session, append one entry to `Documentation/METRICS.md`:

```
## YYYY-MM-DD — [MOD-CODER] — <topic>

**Worked on:**
- <Phase / class / feature> — <one-line description>

**Estimated effort:** ~N hours equivalent
**Notes:** <decisions made, blockers, open questions — omit if none>
```

---

## Definition of Done

1. `ModTranspiler.exe mods/mymod.jar` runs without error on a simple test mod.
2. Output `Bridge/Mods/mymod/*.cs` compiles with zero errors.
3. Generated `mods/compiled/mymod.dll` loads via `AssemblyLoadContext`.
4. All untranslatable constructs produce `// TODO:` comments, never silent drops.
5. Program handles missing Vineflower or missing Java gracefully with a clear error message.
