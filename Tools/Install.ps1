#Requires -Version 5.1
<#
.SYNOPSIS
    Deploys SpectraSharp as a Minecraft launcher version package.

.DESCRIPTION
    1. Resolves the SpectraSharp project directory (parent of this Tools\ folder).
    2. Deploys Bootstrap JAR into:
         %APPDATA%\.minecraft\libraries\io\spectrasharp\bootstrap\1.0\
    3. Writes spectradir.txt alongside the library JAR so Bootstrap can find the project.
    4. Writes the version JSON to versions\SpectraSharp-1.0\.

    After this runs, create a new installation in the Minecraft Launcher and select
    version "SpectraSharp-1.0". Optionally builds Bootstrap.jar if it is missing.

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

# -- Resolve paths -------------------------------------------------------------
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
$LibDir       = Join-Path $MinecraftDir 'libraries\io\spectrasharp\bootstrap\1.0'
$LibJar       = Join-Path $LibDir 'bootstrap-1.0.jar'

Write-Host ''
Write-Host 'SpectraSharp Install'
Write-Host "  SpectraDir   : $SpectraDir"
Write-Host "  MinecraftDir : $MinecraftDir"
Write-Host "  VersionDir   : $VersionDir"
Write-Host "  LibDir       : $LibDir"
Write-Host ''

# -- Sanity checks -------------------------------------------------------------
if (-not (Test-Path $MinecraftDir)) {
    Write-Error "[Install] Minecraft data directory not found: $MinecraftDir"
    exit 1
}

if (-not (Test-Path $TemplateJson)) {
    Write-Error "[Install] Template JSON not found: $TemplateJson"
    exit 1
}

# -- Build Bootstrap JAR if missing -------------------------------------------
if (-not (Test-Path $BootstrapJar)) {
    if ($SkipBuild) {
        Write-Error "[Install] $BootstrapJar not found and -SkipBuild was specified."
        exit 1
    }

    Write-Host '[Install] Bootstrap JAR not found - building now...'
    & (Join-Path $ScriptDir 'Build-Bootstrap.ps1')
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# -- Write version JSON (no BOM) ----------------------------------------------
if (-not (Test-Path $VersionDir)) {
    New-Item -ItemType Directory -Path $VersionDir | Out-Null
}
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$outJson   = Join-Path $VersionDir "$VersionName.json"
[System.IO.File]::WriteAllText($outJson, (Get-Content $TemplateJson -Raw -Encoding UTF8), $utf8NoBom)
Write-Host "[Install] Written : $outJson"

# -- Deploy Bootstrap JAR to libraries/ ---------------------------------------
if (-not (Test-Path $LibDir)) {
    New-Item -ItemType Directory -Path $LibDir | Out-Null
}
Copy-Item $BootstrapJar $LibJar -Force
Write-Host "[Install] Copied  : $LibJar"

# -- Write spectradir.txt alongside the library JAR ---------------------------
$libTxt = Join-Path $LibDir 'spectradir.txt'
[System.IO.File]::WriteAllText($libTxt, $SpectraDir, $utf8NoBom)
Write-Host "[Install] Written : $libTxt"

# -- Register / update profile in launcher_profiles.json ---------------------
$profilesPath = Join-Path $MinecraftDir 'launcher_profiles.json'

if (Test-Path $profilesPath) {
    $raw        = [System.IO.File]::ReadAllText($profilesPath, [System.Text.Encoding]::UTF8)
    $profileKey = 'SpectraSharp-1.0'

    # Build icon value: base64 data URI if logo exists, else named icon
    $logoPng    = Join-Path $SpectraDir 'Assets/Branding/SpectraSharpLogo128x128.png'
    $iconValue  = 'Stone'
    if (Test-Path $logoPng) {
        $bytes     = [System.IO.File]::ReadAllBytes($logoPng)
        $b64       = [System.Convert]::ToBase64String($bytes)
        $iconValue = "data:image/png;base64,$b64"
        Write-Host "[Install] Logo    : embedded ($logoPng)"
    }

    if ($raw -match [regex]::Escape("`"$profileKey`"")) {
        # Profile exists -- replace icon only inside its own block using brace-depth tracking
        $lines   = $raw -split "`n"
        $inBlock = $false
        $depth   = 0
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if (-not $inBlock -and $lines[$i] -match [regex]::Escape("`"$profileKey`"")) {
                $inBlock = $true
                $depth   = 0
            }
            if ($inBlock) {
                $depth += ([regex]::Matches($lines[$i], '\{')).Count
                $depth -= ([regex]::Matches($lines[$i], '\}')).Count
                if ($lines[$i] -match '"icon"\s*:') {
                    $lines[$i] = $lines[$i] -replace '"icon"\s*:\s*"[^"]*"', ('"icon" : "' + $iconValue + '"')
                }
                if ($depth -le 0 -and $i -gt 0) { $inBlock = $false }
            }
        }
        $raw = $lines -join "`n"
        [System.IO.File]::WriteAllText($profilesPath, $raw, $utf8NoBom)
        Write-Host "[Install] Updated : profile $profileKey icon"
    } else {
        # Profile missing -- insert it
        $now   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
        $entry = "    `"$profileKey`" : {`n      `"created`" : `"$now`",`n      `"icon`" : `"$iconValue`",`n      `"lastUsed`" : `"$now`",`n      `"lastVersionId`" : `"$VersionName`",`n      `"name`" : `"SpectraSharp 1.0`",`n      `"type`" : `"custom`"`n    },"
        $raw   = $raw -replace '("profiles"\s*:\s*\{)', "`$1`n$entry"
        [System.IO.File]::WriteAllText($profilesPath, $raw, $utf8NoBom)
        Write-Host "[Install] Inserted: profile $profileKey"
    }
} else {
    Write-Warning "[Install] launcher_profiles.json not found - skipping profile registration."
}

# -- Done ---------------------------------------------------------------------
Write-Host ''
Write-Host '[Install] Done!'
Write-Host '[Install] In the Minecraft Launcher: create a new installation and select version "SpectraSharp-1.0".'
Write-Host ''
