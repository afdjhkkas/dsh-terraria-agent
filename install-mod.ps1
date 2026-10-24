<#
.SYNOPSIS
    Installs artifacts\DshTerrariaBridge.tmod into the local tModLoader Mods folder.

.DESCRIPTION
    Close tModLoader (and Terraria) first. While the game runs it keeps the .tmod and
    the Mods folder open, so the copy fails with a sharing violation and, worse, a
    mod that is overwritten under a running game is not reloaded until the next
    launch. The script detects the locked-file case and tells you so instead of
    leaving a half-copied file.

    Safety net: the build that is already installed is copied to
      <artifacts folder>\DshTerrariaBridge.previous-installed.tmod
    before it is replaced (the repo's .gitignore already covers that name). That
    copy is the one to re-install if a fresh build misbehaves:

      ./install-mod.ps1 -Artifact artifacts\DshTerrariaBridge.previous-installed.tmod -NoBackup

    The artifact is validated first: tModLoader's .tmod starts with the ASCII magic
    "TMOD" followed by the tModLoader version that packed it. A truncated rebuild (or
    a zip that was renamed to .tmod) is rejected here rather than producing a mod the
    game silently refuses to load.

.PARAMETER ModsDir
    Target Mods folder. Defaults to
    "%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods".

.PARAMETER Artifact
    .tmod to install. Defaults to artifacts\DshTerrariaBridge.tmod next to this script.

.PARAMETER NoBackup
    Do not keep a copy of the currently installed build.
#>
[CmdletBinding()]
param(
    [string]$ModsDir,
    [string]$Artifact,
    [switch]$NoBackup
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Artifact)) {
    $Artifact = Join-Path $PSScriptRoot 'artifacts\DshTerrariaBridge.tmod'
}
if ([string]::IsNullOrWhiteSpace($ModsDir)) {
    $documents = [Environment]::GetFolderPath('MyDocuments')
    if ([string]::IsNullOrWhiteSpace($documents)) {
        $documents = Join-Path $env:USERPROFILE 'Documents'
    }
    $ModsDir = Join-Path $documents 'My Games\Terraria\tModLoader\Mods'
}

if (-not (Test-Path -LiteralPath $Artifact -PathType Leaf)) {
    throw "Mod artifact was not found: $Artifact"
}
$Artifact = (Resolve-Path -LiteralPath $Artifact).Path

# ── Validate the artifact before touching the installed copy ────────────────────
$head = New-Object byte[] 48
$read = 0
$fs = [System.IO.File]::OpenRead($Artifact)
try {
    $read = $fs.Read($head, 0, $head.Length)
} finally {
    $fs.Dispose()
}
$magic = ''
if ($read -ge 4) { $magic = [System.Text.Encoding]::ASCII.GetString($head, 0, 4) }
if ($read -lt 5 -or $magic -ne 'TMOD') {
    throw ("Not a tModLoader .tmod file (expected the 'TMOD' magic at the start): $Artifact`n" +
        'If you just rebuilt, the packing step did not finish -- rebuild and copy the .tmod out of the tModLoader Mods folder again.')
}

# Bytes 5.. are the tModLoader build string that packed the file ("2026.8.3.0").
$packedBy = New-Object System.Text.StringBuilder
for ($i = 5; $i -lt $read -and $head[$i] -ge 32 -and $head[$i] -lt 127; $i++) {
    [void]$packedBy.Append([char]$head[$i])
}
if ($packedBy.Length -gt 0) {
    Write-Host "Artifact packed by tModLoader $($packedBy.ToString())"
}

# ── Install ─────────────────────────────────────────────────────────────────────
New-Item -ItemType Directory -Path $ModsDir -Force | Out-Null
$target = Join-Path $ModsDir 'DshTerrariaBridge.tmod'
$artifactDir = Split-Path -Parent $Artifact
$newHash = (Get-FileHash -LiteralPath $Artifact -Algorithm SHA256).Hash

$targetExists = Test-Path -LiteralPath $target -PathType Leaf
if ($targetExists) {
    # Prove up front that we can take an exclusive handle on the installed .tmod.
    # While the game runs it holds this file (and sometimes blocks even reading it),
    # and the raw "file is in use" error from Get-FileHash/Copy-Item does not tell the
    # user what to do about it.
    try {
        $lockProbe = [System.IO.File]::Open($target, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        $lockProbe.Dispose()
    } catch {
        throw ("Cannot replace $target because it is in use ($($_.Exception.Message)).`n" +
            'Close tModLoader and Terraria completely, then run this script again.')
    }

    $oldHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    if ($oldHash -eq $newHash) {
        Write-Host "Already installed: $target is byte-identical to the artifact (SHA-256 $newHash). Nothing copied."
        return
    }
    if (-not $NoBackup) {
        $backup = Join-Path $artifactDir 'DshTerrariaBridge.previous-installed.tmod'
        try {
            Copy-Item -LiteralPath $target -Destination $backup -Force
            Write-Host "Kept the previously installed build at $backup"
        } catch {
            Write-Warning "Could not keep a copy of the installed build at $backup ($($_.Exception.Message)). Continuing."
        }
    }
}

try {
    Copy-Item -LiteralPath $Artifact -Destination $target -Force
} catch {
    throw ("Could not write $target ($($_.Exception.Message)).`n" +
        'tModLoader or Terraria is most likely still running and holding the .tmod open. Close the game completely and run this script again.')
}

$item = Get-Item -LiteralPath $target
$hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($hash -ne $newHash) {
    throw "Copy verification failed: $target has SHA-256 $hash but the artifact has $newHash."
}

Write-Host "Installed $($item.Name) ($($item.Length) bytes)"
Write-Host "SHA-256: $hash"
Write-Host "Mods directory: $ModsDir"
Write-Host 'Start tModLoader, enable "DSH Terraria Bridge" in the mod list (it stays enabled once set), and enter a single-player world.'