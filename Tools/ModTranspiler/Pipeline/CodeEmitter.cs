namespace SpectraSharp.ModTranspiler.Pipeline;

/// <summary>
/// Phase 5 — Writes generated C# source files to Bridge/Mods/&lt;ModName&gt;/.
/// </summary>
static class CodeEmitter
{
    public static string Run(
        string modName,
        List<(string File, string Source)> sources,
        string outputRoot)
    {
        string modDir = Path.Combine(outputRoot, modName);
        Directory.CreateDirectory(modDir);

        Console.WriteLine($"[CodeEmitter] Writing {sources.Count} files → {modDir}");

        foreach (var (file, source) in sources)
        {
            string fullPath = Path.Combine(modDir, file);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, source);
        }

        // Write a .csproj from the template
        WriteCsproj(modName, modDir);

        return modDir;
    }

    static void WriteCsproj(string modName, string modDir)
    {
        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <AssemblyName>{modName}</AssemblyName>
                <RootNamespace>SpectraSharp.Bridge.Mods.{Sanitize(modName)}</RootNamespace>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="../../../SpectraSharp.csproj" />
                <PackageReference Include="Lib.Harmony" Version="2.3.6" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(modDir, $"{modName}.csproj"), csproj);
    }

    static string Sanitize(string name) =>
        string.Concat(name.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
