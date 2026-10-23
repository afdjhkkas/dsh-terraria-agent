param(
    [string]$TModLoaderPath
)

$ErrorActionPreference = 'Stop'

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
