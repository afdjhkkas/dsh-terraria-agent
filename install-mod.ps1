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

New-Item -ItemType Directory -Path $ModsDir -Force | Out-Null
$target = Join-Path $ModsDir 'DshTerrariaBridge.tmod'

if ((Test-Path -LiteralPath $target -PathType Leaf) -and -not $NoBackup) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $backup = "$target.bak-$stamp"
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Write-Host "Backed up existing Mod to $backup"
}

Copy-Item -LiteralPath $Artifact -Destination $target -Force
$item = Get-Item -LiteralPath $target
$hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
Write-Host "Installed $($item.Name) ($($item.Length) bytes)"
Write-Host "SHA-256: $hash"
Write-Host "Mods directory: $ModsDir"
