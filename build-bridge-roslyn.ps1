<#
.SYNOPSIS
    Compiles DshTerrariaBridge.dll from the checked-in C# sources with the Roslyn
    assemblies that ship inside the local tModLoader installation.

.DESCRIPTION
    This is a fast "does it still compile?" loop. It only produces the DLL; it does
    not pack the .tmod (tModLoader does that, see below).

    REQUIREMENT: PowerShell 7 or newer, running on .NET 8 or newer.
    Windows PowerShell 5.1 runs on .NET Framework 4.x and cannot load the net8.0
    assemblies under <tModLoader>\Libraries (Roslyn 4.12.0 and
    Basic.Reference.Assemblies.Net80 are compiled for .NET 8), so Add-Type fails with
    ReflectionTypeLoadException. This script detects that up front and stops with an
    actionable message instead of that stack trace.

    The verified alternative (this is how the shipping artifacts/DshTerrariaBridge.tmod
    is produced) is the .NET 8 SDK:

        cd "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge"
        & "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" build -c Release

    That build also runs tModLoader's own "BuildMod" target (tMLMod.targets:58-59 ->
    `dotnet tModLoader.dll -build <projectDir> -eac <TargetPath>`), which packs
    DshTerrariaBridge.tmod into
    "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\Mods\".
    Copy that file over artifacts\DshTerrariaBridge.tmod, then run install-mod.ps1.

.PARAMETER TModLoaderPath
    tModLoader installation folder. Defaults to $env:TML_PATH, then
    D:\STEAM\steamapps\common\tModLoader. Only the DLL and the reference assemblies
    under Libraries\ are read; nothing is installed or launched.
#>
param(
    [string]$TModLoaderPath
)

$ErrorActionPreference = 'Stop'

# ── Host runtime gate ────────────────────────────────────────────────────────────
# Must run before Add-Type: on PS 5.1 the Add-Type call below is the first thing that
# blows up, and its error (ReflectionTypeLoadException) does not tell the caller what
# to do instead.
$psRuntime = [System.Environment]::Version
$isPwsh = $PSVersionTable.PSEdition -eq 'Core'
if (-not $isPwsh -or $psRuntime.Major -lt 8) {
    $detail = "PowerShell $($PSVersionTable.PSVersion) (PSEdition=$($PSVersionTable.PSEdition)), CLR $psRuntime"
    $guidance = @'

Why: the Roslyn compiler and reference assemblies this script loads live under
<tModLoader>\Libraries\...\lib\net8.0 (Microsoft.CodeAnalysis 4.12.0,
Microsoft.CodeAnalysis.CSharp 4.12.0, Basic.Reference.Assemblies.Net80 1.5.0).
Those files target .NET 8. Windows PowerShell 5.1 runs on .NET Framework 4.x and
cannot load them, so Add-Type fails with ReflectionTypeLoadException before any
C# is compiled. Nothing about your mod source is wrong.

Use either of these instead:

  A) .NET 8 SDK - this is what produces the shipping artifacts\DshTerrariaBridge.tmod:

       cd "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\ModSources\DshTerrariaBridge"
       & "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe" build -c Release

     That compiles the mod and runs tModLoader's BuildMod target, which packs
     DshTerrariaBridge.tmod into
       "$env:USERPROFILE\Documents\My Games\Terraria\tModLoader\Mods\"
     Copy it over artifacts\DshTerrariaBridge.tmod, then run install-mod.ps1.

  B) PowerShell 7+ on .NET 8, if you have it:

       pwsh -NoProfile -ExecutionPolicy Bypass -File .\build-bridge-roslyn.ps1

     pwsh 7.2/7.3 run on .NET 6/7 and cannot load the net8.0 assemblies either;
     use pwsh 7.4+ (or any pwsh whose CLR major is >= 8).

'@
    throw "build-bridge-roslyn.ps1 cannot run in this shell: $detail`n$guidance"
}

Write-Verbose "Host: PowerShell $($PSVersionTable.PSVersion) on CLR $psRuntime (net8.0-capable)"

# ── Locate tModLoader ───────────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($TModLoaderPath)) {
    $TModLoaderPath = $env:TML_PATH
}
if ([string]::IsNullOrWhiteSpace($TModLoaderPath)) {
    $TModLoaderPath = 'D:\STEAM\steamapps\common\tModLoader'
}
if (-not (Test-Path -LiteralPath $TModLoaderPath -PathType Container)) {
    throw "tModLoader installation was not found: $TModLoaderPath. Pass -TModLoaderPath or set TML_PATH."
}

$tml = (Resolve-Path -LiteralPath $TModLoaderPath).Path
$root = Join-Path $PSScriptRoot 'DshTerrariaBridge'
$output = Join-Path $root 'bin\Debug\net8.0\DshTerrariaBridge.dll'
$libraries = Join-Path $tml 'Libraries'

foreach ($required in @(
    'microsoft.codeanalysis.common\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.dll',
    'microsoft.codeanalysis.csharp\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.CSharp.dll',
    'basic.reference.assemblies.net80\1.5.0\lib\netstandard2.0\Basic.Reference.Assemblies.Net80.dll'
)) {
    $probe = Join-Path $libraries $required
    if (-not (Test-Path -LiteralPath $probe -PathType Leaf)) {
        throw "Roslyn/reference assembly missing from this tModLoader build: $probe. The folder layout under Libraries\ changes between tModLoader releases; update the paths in this script or build with the .NET 8 SDK instead."
    }
}

Add-Type -Path (Join-Path $libraries 'microsoft.codeanalysis.common\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $libraries 'microsoft.codeanalysis.csharp\4.12.0\lib\net8.0\Microsoft.CodeAnalysis.CSharp.dll')
Add-Type -Path (Join-Path $libraries 'basic.reference.assemblies.net80\1.5.0\lib\netstandard2.0\Basic.Reference.Assemblies.Net80.dll')

$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::new([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp12)
$parseOptions = $parseOptions.WithPreprocessorSymbols([string[]]@('TML_2026_08', 'JETBRAINS_ANNOTATIONS'))
$trees = [System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
Get-ChildItem $root -Filter '*.cs' -File | ForEach-Object {
    $source = [System.IO.File]::ReadAllText($_.FullName)
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source, $parseOptions, $_.FullName))
}

$refs = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$refs.AddRange([Basic.Reference.Assemblies.Net80+References]::All)
$paths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$paths.Add((Join-Path $tml 'tModLoader.dll')) | Out-Null
Get-ChildItem $libraries -Filter '*.dll' -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\(Native|tModCodeAssist|runtime[^\\]*)\\' -and
    $_.Name -notmatch '\.resources\.dll$' -and
    $_.Name -notmatch '^Microsoft\.CodeAnalysis'
} | ForEach-Object { $paths.Add($_.FullName) | Out-Null }
foreach ($path in $paths) {
    if (Test-Path $path) { $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path)) }
}

$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('DshTerrariaBridge', $trees, $refs, $options)
$temp = "$output.new"
$stream = [System.IO.File]::Open($temp, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
try { $emit = $compilation.Emit($stream) } finally { $stream.Dispose() }
$emit.Diagnostics | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error } | ForEach-Object { Write-Output $_.ToString() }
if (-not $emit.Success) { Remove-Item -LiteralPath $temp -Force; throw 'Roslyn compilation failed.' }
Move-Item -LiteralPath $temp -Destination $output -Force
Get-Item $output | Select-Object FullName, LastWriteTime, Length
Write-Host 'Compiled the DLL only. Pack the .tmod with the .NET 8 SDK build (see the header) before publishing a release.'