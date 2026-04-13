#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys SpectraSharp as a Minecraft launcher version package.

.DESCRIPTION
    1. Resolves the SpectraSharp project directory (parent of this Tools\ folder).
    2. Copies SpectraSharp-1.0.jar + the patched JSON into:
         %APPDATA%\.minecraft\versions\SpectraSharp-1.0\
    3. Writes spectradir.txt so Bootstrap.java knows where to find the project.
    4. Optionally builds Bootstrap.jar if it is missing.

    After this runs, open the Minecraft Launcher -> Installations -> New ->
    select version "SpectraSharp-1.0" -> Save -> Play.

.PARAMETER SpectraDir
    Override the project directory.  Defaults to the solution root (parent of Tools\).

.PARAMETER SkipBuild
    Skip compiling Bootstrap.java even if the JAR is missing.

.PARAMETER MinecraftDir
    Override the Minecraft data directory.  Defaults to %APPDATA%\.minecraft.
#>
param(
    [string] $SpectraDir    = '',
    [switch] $SkipBuild,
    [string] $MinecraftDir  = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Resolve paths ─────────────────────────────────────────────────────────────
$ScriptDir   = $PSScriptRoot
$LauncherDir = Join-Path $ScriptDir 'launcher'

if (-not $SpectraDir) {
    $SpectraDir = (Resolve-Path (Join-Path $ScriptDir '..')).Path
}
$SpectraDir = $SpectraDir.TrimEnd('\', '/')

if (-not $MinecraftDir) {
    $MinecraftDir = Join-Path $env:APPDATA '.minecraft'
}

$VersionName  = 'SpectraSharp-1.0'
$VersionDir   = Join-Path $MinecraftDir "versions\$VersionName"
$TemplateJson = Join-Path $LauncherDir "$VersionName.template.json"
$BootstrapJar = Join-Path $LauncherDir "$VersionName.jar"

Write-Host ''
Write-Host 'SpectraSharp Install'
Write-Host "  SpectraDir   : $SpectraDir"
Write-Host "  MinecraftDir : $MinecraftDir"
Write-Host "  Target       : $VersionDir"
Write-Host ''

# ── Sanity checks ─────────────────────────────────────────────────────────────
if (-not (Test-Path $MinecraftDir)) {
    Write-Error @"
[Install] Minecraft data directory not found:
  $MinecraftDir

Launch the official Minecraft Launcher at least once to create it, then re-run Install.ps1.
"@
    exit 1
}

if (-not (Test-Path $TemplateJson)) {
    Write-Error "[Install] Template JSON not found: $TemplateJson"
    exit 1
}

# ── Build Bootstrap JAR if missing ───────────────────────────────────────────
if (-not (Test-Path $BootstrapJar)) {
    if ($SkipBuild) {
        Write-Error @"
[Install] $BootstrapJar not found and -SkipBuild was specified.
Run 'Tools\Build-Bootstrap.ps1' first, then re-run Install.ps1.
"@
        exit 1
    }

    Write-Host '[Install] Bootstrap JAR not found — building now...'
    $buildScript = Join-Path $ScriptDir 'Build-Bootstrap.ps1'
    & $buildScript
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# ── Create version directory ──────────────────────────────────────────────────
if (-not (Test-Path $VersionDir)) {
    New-Item -ItemType Directory -Path $VersionDir | Out-Null
    Write-Host "[Install] Created: $VersionDir"
}

# ── Write version JSON ────────────────────────────────────────────────────────
$jsonContent = Get-Content $TemplateJson -Raw -Encoding UTF8
$outJson = Join-Path $VersionDir "$VersionName.json"
[System.IO.File]::WriteAllText($outJson, $jsonContent, [System.Text.Encoding]::UTF8)
Write-Host "[Install] Written : $outJson"

# ── Copy Bootstrap JAR ───────────────────────────────────────────────────────
$outJar = Join-Path $VersionDir "$VersionName.jar"
Copy-Item $BootstrapJar $outJar -Force
Write-Host "[Install] Copied  : $outJar"

# ── Write spectradir.txt (Bootstrap reads this to find the project) ───────────
$outTxt = Join-Path $VersionDir 'spectradir.txt'
[System.IO.File]::WriteAllText($outTxt, $SpectraDir, [System.Text.Encoding]::UTF8)
Write-Host "[Install] Written : $outTxt  ($SpectraDir)"

# ── Done ─────────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '[Install] Installation complete!'
Write-Host ''
Write-Host 'Next steps:'
Write-Host '  1. Open the Minecraft Launcher'
Write-Host '  2. Go to Installations -> New installation'
Write-Host "  3. Select version: $VersionName"
Write-Host '  4. Save -> Play'
Write-Host '     The launcher will download 1.0 assets, then SpectraSharp starts automatically.'
Write-Host ''
