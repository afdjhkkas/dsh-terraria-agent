<#
.SYNOPSIS
    One command from checked-in C# sources to a loadable, installed .tmod.

.DESCRIPTION
    本仓库的 C# Mod 有五步容易漏掉的环节，任何一步漏掉都会得到「装了但没用」
    或者「游戏报模组加载失败」：

      1. 游戏必须完全关闭 —— tModLoader 运行时会锁住 .tmod，复制会失败；
         更糟的是它可能把正在打包的目标打成空壳。
      2. 源码必须先同步到 ModSources\<ModName>\，tModLoader 只认那个目录。
      3. 必须 `dotnet build -c Release --no-incremental` —— 增量构建会漏掉
         新增的 using / 类型，得到「编译过了但行为还是旧的」。
      4. 打包必须带 **-eac**（external assembly copy）并指向刚 build 出来的 dll。
         不带 -eac 打出来的 .tmod 只有几百字节，游戏加载时直接报错。
      5. 打完必须校验体积：正常在 150 KB 以上，低于 50 KB 一律当空壳处理。

    脚本按上面顺序做，每一步失败都立即停止并打印可执行的修复建议，
    不会留下「装了一半」的状态。成功时打印 SHA-256 并同步一份到
    artifacts\DshTerrariaBridge.tmod，让仓库里的成品和源码保持一致。

    !!! 本次运行不会启动或重启游戏。装好之后由调用者决定何时启动。!!!

.PARAMETER ModName
    tModLoader 的工程名。默认 DshTerrariaBridge。

.PARAMETER CompileOnly
    同步源码并做 Release 编译检查，然后停止，不调用 tModLoader 打包。

.PARAMETER NoSyncArtifact
    不把成品同步回仓库 artifacts\；仍会执行 -build 并更新用户数据目录的 Mods 包。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1

.EXAMPLE
    # 只验证源码可编译，不打包、不碰 Mods 目录
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-mod.ps1 -CompileOnly
#>
[CmdletBinding()]
param(
    [string]$ModName = 'DshTerrariaBridge',
    [switch]$CompileOnly,
    [switch]$NoSyncArtifact
)

$ErrorActionPreference = 'Stop'

# 输出编码：脚本里有中文，默认 5.1 会按控制台代码页（GBK）写管道，
# 被 bash / node 父进程按 UTF-8 读时就是一片乱码。显式改成 UTF-8。
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

function Fail($message, $hint) {
    Write-Host ""
    Write-Host "构建失败：$message" -ForegroundColor Red
    if ($hint) { Write-Host "怎么修：$hint" -ForegroundColor Yellow }
    exit 1
}

function Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Ok($text) { Write-Host "   ok  $text" -ForegroundColor Green }
function Restore-PreviousPackage($targetPath, $backupPath) {
    if ($backupPath -and (Test-Path -LiteralPath $backupPath)) {
        Copy-Item -LiteralPath $backupPath -Destination $targetPath -Force
        Write-Host "   restored previous package from $backupPath" -ForegroundColor Yellow
    } elseif (Test-Path -LiteralPath $targetPath) {
        Remove-Item -LiteralPath $targetPath -Force
        Write-Host "   removed invalid package $targetPath" -ForegroundColor Yellow
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $repoRoot $ModName
if (-not (Test-Path $srcDir)) { Fail "找不到源码目录 $srcDir" "在仓库根目录下运行本脚本" }

# ---------------------------------------------------------------- 1. 游戏必须关
Step '1/6 检查游戏进程'
# tModLoader 是用 `dotnet tModLoader.dll` 启动的，进程名是 dotnet 而不是 tModLoader，
# 按名字查会永远查不到（这个坑让 TML003 报错看起来莫名其妙）。只能按命令行判定。
$running = @(
    Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -match 'tModLoader\.dll' }
)
if ($running.Count -gt 0) {
    $detail = ($running | ForEach-Object { "PID $($_.ProcessId)" }) -join ', '
    Fail "tModLoader 正在运行（$detail）" `
        '完整退出到桌面（不是回主菜单）再重跑。游戏没关时打包会报 TML003，而且复制 .tmod 会撞共享冲突。'
}
Ok '没有 tModLoader 在运行'

# ---------------------------------------------------------------- 2. 找工具链
Step '2/6 定位 tModLoader 与 dotnet'

# 两个不同的目录，很容易混淆：
#   $dataRoot —— 用户数据（Worlds/Players/ModSources/Mods），在「我的文档」下
#   $gameDir  —— 程序本体（tModLoader.dll），在 Steam 库下
# tModLoader.dll 绝不在数据目录里；以前把两者当同一个，脚本一定会在这里失败。
$dataRoot = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Terraria\tModLoader'
if (-not (Test-Path $dataRoot)) { Fail "找不到数据目录 $dataRoot" '先安装并至少运行一次 tModLoader' }
Ok "数据目录  $dataRoot"

function Find-GameDir {
    if ($env:TMODLOADER_DIR -and (Test-Path (Join-Path $env:TMODLOADER_DIR 'tModLoader.dll'))) {
        return $env:TMODLOADER_DIR
    }
    $candidates = New-Object System.Collections.Generic.List[string]
    # 常见的 Steam 库位置；D:\STEAM 这种自定义库名也要覆盖，所以按盘符逐个试
    foreach ($drive in (Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root)) {
        foreach ($sub in @('STEAM', 'Steam', 'SteamLibrary', 'Games\Steam', 'Program Files (x86)\Steam', 'Program Files\Steam')) {
            $candidates.Add((Join-Path $drive "$sub\steamapps\common\tModLoader"))
        }
    }
    foreach ($c in $candidates) {
        if (Test-Path (Join-Path $c 'tModLoader.dll')) { return $c }
    }
    return $null
}

$gameDir = Find-GameDir
if (-not $gameDir) {
    Fail '找不到 tModLoader.dll（已经扫过所有盘符下的常见 Steam 库位置）' `
        '把 tModLoader 安装目录设到环境变量 TMODLOADER_DIR 里再重跑，例如：$env:TMODLOADER_DIR="D:\STEAM\steamapps\common\tModLoader"'
}
$tmodDll = Join-Path $gameDir 'tModLoader.dll'
Ok "游戏目录  $gameDir"
Ok "tModLoader.dll  $tmodDll"

# dotnet：优先用机器上已装好的 .NET 8 SDK（tModLoader 自带的那个也能用）
$dotnetCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'),
    (Join-Path $dataRoot 'dotnet\dotnet.exe')
)
$dotnet = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $dotnet) {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { $dotnet = $cmd.Source }
}
if (-not $dotnet) { Fail '找不到 dotnet.exe' "装 .NET 8 SDK，或先运行一次 tModLoader 让它把 dotnet 放到 $dataRoot\dotnet" }
Ok "dotnet  $dotnet"

$modSources = Join-Path $dataRoot "ModSources\$ModName"
$modsDir = Join-Path $dataRoot 'Mods'

# ---------------------------------------------------------------- 3. 同步源码
Step "3/6 同步源码到 $modSources"
if (-not (Test-Path $modSources)) { New-Item -ItemType Directory -Path $modSources -Force | Out-Null }
# 只同步源码与工程文件；bin/obj 由 build 自己生成，build.txt 决定打包元数据
$patterns = @('*.cs', '*.csproj', 'build.txt', 'description.txt', 'icon.png')
$copied = 0
foreach ($pattern in $patterns) {
    Get-ChildItem -Path $srcDir -Filter $pattern -File -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item $_.FullName -Destination $modSources -Force
        $copied++
    }
}
if ($copied -eq 0) { Fail "没有从 $srcDir 复制到任何文件" '确认仓库里 DshTerrariaBridge 目录下确实有 .cs / .csproj' }
Ok "复制 $copied 个文件"

# ---------------------------------------------------------------- 4. 编译
# 禁用 tMLMod.targets 的自动 BuildMod target，明确把“编译”和“打包”分成两步。
# 这样流程与 HANDOFF.md 的硬规矩一致：先得到刚编译的 DLL，再显式用 -eac 打包。
Step '4/6 dotnet build -c Release --no-incremental（只编译）'
$buildLog = & $dotnet build $modSources -c Release --no-incremental -p:BuildMod=false 2>&1
$buildExit = $LASTEXITCODE
if ($buildExit -ne 0) {
    $tail = ($buildLog | Select-Object -Last 25) -join "`n"
    Write-Host $tail
    if ($buildLog -match 'TML003') {
        Fail '游戏还在跑（TML003）' '把 tModLoader 完整退出到桌面再重跑。只关到主菜单不算关。'
    }
    Fail "编译失败（exit $buildExit）" '看上面报错的第一条 CS / TML 错误；改完源码重跑本脚本'
}
$builtDll = Join-Path $modSources ("bin\Release\net8.0\$ModName.dll")
if (-not (Test-Path $builtDll)) { Fail "编译成功但找不到 $builtDll" '确认 .csproj 的 TargetFramework 是 net8.0' }
$dllSize = (Get-Item $builtDll).Length
Ok ("$ModName.dll  {0:N0} 字节" -f $dllSize)

if ($CompileOnly) {
    Write-Host "`nRelease 编译检查完成。未打包、未写入 Mods、未启动/重启游戏。" -ForegroundColor Green
    exit 0
}

# ---------------------------------------------------------------- 5. 显式打包 + 校验
Step '5/6 tModLoader -server -build -eac（显式打包并校验）'
$target = Join-Path $modsDir ($ModName + '.tmod')
$backup = $null
if (Test-Path $target) {
    # 备份放在 artifacts\.backup\ 而不是 artifacts\ 根目录：
    # 两者名字太接近（DshTerrariaBridge.tmod vs DshTerrariaBridge.previous-installed.tmod），
    # 放同一层很容易被当成"当前包"，提交上去或被新对话的 AI 误读。
    $backupDir = Join-Path $repoRoot 'artifacts\.backup'
    if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir -Force | Out-Null }
    $backup = Join-Path $backupDir ($ModName + '.previous-installed.tmod')
    Copy-Item -LiteralPath $target -Destination $backup -Force
    Ok "打包前备份旧包到 $backup"
}
$packLog = @()
Push-Location $gameDir
try {
    $packLog = & $dotnet tModLoader.dll -server -build $modSources -eac $builtDll 2>&1
    $packExit = $LASTEXITCODE
} finally { Pop-Location }
if ($packExit -ne 0) {
    $tail = ($packLog | Select-Object -Last 25) -join "`n"
    Write-Host $tail
    Restore-PreviousPackage $target $backup
    if ($packLog -match 'TML003') {
        Fail '游戏还在跑（TML003）' '把 tModLoader 完整退出到桌面再重跑。只关到主菜单不算关。'
    }
    Fail "打包失败（exit $packExit）" '确认 tModLoader.dll、ModSources 路径和刚生成的 Release DLL 都存在。'
}
$packText = $packLog -join "`n"
if ($packText -notmatch ("Packaging:\s*" + [regex]::Escape($ModName))) {
    Restore-PreviousPackage $target $backup
    Fail '打包进程退出成功，但日志没有确认 Packaging 目标' '检查 tModLoader 的构建输出；不要把未确认的新包用于游戏。'
}

$tmodOut = Join-Path $modsDir ($ModName + '.tmod')
if (-not (Test-Path $tmodOut)) {
    Restore-PreviousPackage $target $backup
    Fail "显式打包成功但没生成 $tmodOut" 'tModLoader 的 -build 默认把成品写入用户数据目录 Mods；确认命令带了 -eac <刚生成的 DLL>'
}

$tmodSize = (Get-Item $tmodOut).Length
# 空壳判定：正常包 150 KB 以上；不带 -eac 时会掉到几百字节
if ($tmodSize -lt 50000) {
    Restore-PreviousPackage $target $backup
    Fail ("打出来只有 {0:N0} 字节，是空壳" -f $tmodSize) '九成是漏了 -eac（csproj 里的 tMLMod.targets 应当自动带），或 dll 没编出来。不要把这个包装进游戏。'
}
# -AsByteStream 是 PowerShell 6+ 的参数，5.1 要用 -Encoding Byte；这里两种都兼容
$bytes = if ($PSVersionTable.PSVersion.Major -ge 6) {
    Get-Content $tmodOut -AsByteStream -TotalCount 4
} else {
    Get-Content $tmodOut -Encoding Byte -TotalCount 4
}
$magic = [System.Text.Encoding]::ASCII.GetString([byte[]]$bytes)
if ($magic -ne 'TMOD') {
    Restore-PreviousPackage $target $backup
    Fail "包魔数不是 TMOD（读到 '$magic'）" '产物损坏，删掉当前包并重打'
}
Ok ("$ModName.tmod  {0:N0} 字节  magic=TMOD" -f $tmodSize)

# ---------------------------------------------------------------- 6. 安装
Step '6/6 校验 Mods 成品并同步 artifacts'
$sha = (Get-FileHash $target -Algorithm SHA256).Hash
Ok "已打包到 Mods  $target"
Write-Host "   sha256  $sha" -ForegroundColor DarkGray

if (-not $NoSyncArtifact) {
    $artifacts = Join-Path $repoRoot 'artifacts'
    if (-not (Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts -Force | Out-Null }
    Copy-Item $tmodOut (Join-Path $artifacts ($ModName + '.tmod')) -Force
    Ok '已同步到 artifacts\'
}

Write-Host ""
Write-Host "完成。游戏没有启动/重启——要不要进游戏由你决定。" -ForegroundColor Green
Write-Host "装好后在 tModLoader 里确认只启用了 DSH Terraria Bridge 这一个 mod" -ForegroundColor DarkGray
Write-Host "（enabled.json 里留着别的 mod 会连带加载它们，桥的行为就不再可复现）。" -ForegroundColor DarkGray
