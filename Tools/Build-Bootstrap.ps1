#Requires -Version 5.1
<#
.SYNOPSIS
    Compiles Bootstrap.java and packages it as SpectraSharp-1.0.jar.

.DESCRIPTION
    Locates javac on PATH or via JAVA_HOME, compiles the tiny Bootstrap class,
    then packages the resulting .class file as a JAR using .NET's ZIP support
    (no jar.exe dependency).

    Output: Tools\launcher\SpectraSharp-1.0.jar
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Paths ─────────────────────────────────────────────────────────────────────
$ScriptDir   = $PSScriptRoot
$LauncherDir = Join-Path $ScriptDir 'launcher'
$JavaSrc     = Join-Path $LauncherDir 'Bootstrap.java'
$OutJar      = Join-Path $LauncherDir 'SpectraSharp-1.0.jar'
$TmpDir      = Join-Path $ScriptDir  '.build-tmp'

# ── Locate javac ──────────────────────────────────────────────────────────────
function Find-Javac {
    # 1. Check PATH first
    $found = Get-Command javac -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }

    # 2. Fall back to JAVA_HOME
    if ($env:JAVA_HOME) {
        $candidate = Join-Path $env:JAVA_HOME 'bin\javac.exe'
        if (Test-Path $candidate) { return $candidate }
    }

    # 3. Check common install locations
    $commonRoots = @(
        'C:\Program Files\Eclipse Adoptium',
        'C:\Program Files\Java',
        'C:\Program Files\Microsoft',
        'C:\Program Files\BellSoft'
    )
    foreach ($root in $commonRoots) {
        if (Test-Path $root) {
            $hit = Get-ChildItem -Path $root -Filter 'javac.exe' -Recurse -ErrorAction SilentlyContinue |
                   Sort-Object FullName -Descending | Select-Object -First 1
            if ($hit) { return $hit.FullName }
        }
    }

    return $null
}

$javac = Find-Javac
if (-not $javac) {
    Write-Error @"
[Build-Bootstrap] Could not locate javac.
Install a JDK (e.g. Eclipse Temurin) and either:
  - add it to PATH, or
  - set JAVA_HOME to its install directory.
"@
    exit 1
}

Write-Host "[Build-Bootstrap] Using javac: $javac"

# ── Clean temp directory ──────────────────────────────────────────────────────
if (Test-Path $TmpDir) { Remove-Item $TmpDir -Recurse -Force }
New-Item -ItemType Directory -Path $TmpDir | Out-Null

# ── Compile ───────────────────────────────────────────────────────────────────
Write-Host '[Build-Bootstrap] Compiling Bootstrap.java...'
& $javac --release 8 -d $TmpDir $JavaSrc
if ($LASTEXITCODE -ne 0) {
    Write-Error '[Build-Bootstrap] javac failed — see errors above.'
    exit 1
}

# ── Package JAR (JAR = ZIP, use .NET compression to avoid jar.exe dependency) ─
Write-Host "[Build-Bootstrap] Packaging $OutJar..."
if (Test-Path $OutJar) { Remove-Item $OutJar -Force }
Add-Type -Assembly 'System.IO.Compression.FileSystem'
[System.IO.Compression.ZipFile]::CreateFromDirectory($TmpDir, $OutJar)

# ── Cleanup ───────────────────────────────────────────────────────────────────
Remove-Item $TmpDir -Recurse -Force

Write-Host ''
Write-Host "[Build-Bootstrap] Done!  Output: $OutJar"
Write-Host '[Build-Bootstrap] Run Install.ps1 to deploy to the Minecraft launcher.'
