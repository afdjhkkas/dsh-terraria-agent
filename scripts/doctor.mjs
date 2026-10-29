#!/usr/bin/env node
/**
 * 环境体检：把「代码是对的，但机器上装的是旧的」这类问题提前查出来。
 * ============================================================================
 * 这个仓库被复制到另一台机器后最常坏的**不是代码**，而是这几件事：
 *
 *   - artifacts/ 里的 .tmod 是几天前打的，比源码旧 → 装上就是旧行为
 *   - Mods/ 里装的包又跟 artifacts/ 不一样 → 谁也不知道在跑什么
 *   - enabled.json 里还留着别的 mod → 玩家逻辑被别的 mod 改过，现象不可复现
 *   - 后台有 `-server` 的 tModLoader 占着世界文件 → 客户端进不去
 *   - 桥没起 / 版本对不上 / 游戏没进世界
 *
 * 每条都只读，不改任何东西。有 FAIL 时退出码 1，方便接进 CI 或装完自检。
 *
 *   node scripts/doctor.mjs            # 全部检查
 *   node scripts/doctor.mjs --quiet    # 只打印 FAIL / WARN
 *   node scripts/doctor.mjs --json     # 机器可读
 * ============================================================================
 */
import { readFileSync, statSync, existsSync, readdirSync, readFileSync as rf } from 'node:fs'
import { join, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { homedir } from 'node:os'
import { execSync } from 'node:child_process'
import net from 'node:net'

const here = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(here, '..')
const args = process.argv.slice(2)
const quiet = args.includes('--quiet')
const asJson = args.includes('--json')

const results = []
function record (level, name, detail, hint) {
  results.push({ level, name, detail: detail ?? '', hint: hint ?? '' })
}

function mtime (path) {
  try { return statSync(path).mtimeMs } catch { return null }
}
function sizeOf (path) {
  try { return statSync(path).size } catch { return null }
}
function newestIn (dir, exts) {
  let newest = null
  let where = null
  const walk = (d) => {
    let entries
    try { entries = readdirSync(d, { withFileTypes: true }) } catch { return }
    for (const e of entries) {
      if (e.name === 'bin' || e.name === 'obj' || e.name === '.git') continue
      const full = join(d, e.name)
      if (e.isDirectory()) { walk(full); continue }
      if (exts && !exts.some(x => e.name.endsWith(x))) continue
      const m = mtime(full)
      if (m !== null && (newest === null || m > newest)) { newest = m; where = full }
    }
  }
  walk(dir)
  return { mtime: newest, path: where }
}

const dataRoot = join(homedir(), 'Documents', 'My Games', 'Terraria', 'tModLoader')
const artifactsTmod = join(repoRoot, 'artifacts', 'DshTerrariaBridge.tmod')
const installedTmod = join(dataRoot, 'Mods', 'DshTerrariaBridge.tmod')
const enabledJson = join(dataRoot, 'Mods', 'enabled.json')
const srcDir = join(repoRoot, 'DshTerrariaBridge')

/* ── 1. 运行时底座 ─────────────────────────────────────────────── */
const nodeMajor = Number(process.versions.node.split('.')[0])
if (nodeMajor >= 20) record('pass', 'Node 版本', `v${process.versions.node}`)
else record('fail', 'Node 版本', `v${process.versions.node}`, '需要 Node 20 或更新；npm test 用 node --test')

if (existsSync(dataRoot)) record('pass', 'tModLoader 数据目录', dataRoot)
else record('fail', 'tModLoader 数据目录', '不存在', '先安装并至少运行一次 tModLoader')

/* ── 2. 包是不是比源码旧（最容易踩、最难发现的一条）─────────────── */
const srcNewest = newestIn(srcDir, ['.cs', '.csproj', '.txt'])
const artifactTime = mtime(artifactsTmod)
const installedTime = mtime(installedTmod)

if (srcNewest.mtime === null) {
  record('warn', '源码时间戳', '读不到源码文件')
} else if (artifactTime === null) {
  record('fail', 'artifacts 里的包', '不存在', 'npm run build:mod 从源码打一次')
} else if (artifactTime < srcNewest.mtime) {
  const behindH = ((srcNewest.mtime - artifactTime) / 36e5).toFixed(1)
  record('fail', 'artifacts 里的包落后于源码',
    `落后 ${behindH} 小时（最新源码 ${srcNewest.path?.split(/[\\/]/).pop()}）`,
    'artifacts/*.tmod 只是某次构建的快照。跑 npm run build:mod 重新打包，否则别人装到的是旧行为')
} else {
  record('pass', 'artifacts 里的包', '不落后于源码')
}

if (installedTime === null) {
  record('fail', 'Mods 里没有本 mod', '未安装', 'npm run build:mod -Install 或 npm run install:mod')
} else if (artifactTime !== null && Math.abs(installedTime - artifactTime) > 2000) {
  record('warn', 'Mods 里装的包与 artifacts 不一致',
    `相差 ${((installedTime - artifactTime) / 1000).toFixed(0)} 秒`,
    '两边不是同一次构建的产物。重跑 npm run build:mod 让它们一致')
} else {
  const s = sizeOf(installedTmod)
  if (s !== null && s < 50000) {
    record('fail', 'Mods 里装的是空壳', `${s} 字节`, '重新打包；不带 -eac 会打出空壳，游戏加载会直接报错')
  } else {
    record('pass', 'Mods 里装的包', `${s} 字节`)
  }
}

/* ── 3. enabled.json 只该有本 mod ──────────────────────────────── */
if (!existsSync(enabledJson)) {
  record('warn', 'enabled.json', '不存在', '首次进游戏后由 tModLoader 生成；确认只勾选 DSH Terraria Bridge')
} else {
  try {
    const list = JSON.parse(rf(enabledJson, 'utf8'))
    const extra = list.filter(x => x !== 'DshTerrariaBridge')
    if (extra.length === 0) record('pass', 'enabled.json', '只有 DshTerrariaBridge')
    else record('fail', 'enabled.json 里还有别的 mod', extra.join(', '),
      '别的 mod 会改玩家逻辑，桥的现象就不再可复现。编辑 enabled.json 只保留 DshTerrariaBridge，或把其它 mod 从 Mods/ 挪走')
  } catch (e) {
    record('warn', 'enabled.json 读不了', String(e.message))
  }
}

/* ── 4. 有没有 -server 的 tModLoader 占着世界文件 ───────────────── */
try {
  const out = execSync(
    'powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match \'tModLoader\\.dll\' } | Select-Object -ExpandProperty CommandLine"',
    { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }
  ).trim()
  if (!out) {
    record('warn', '游戏进程', '没有 tModLoader 在跑', '起游戏并进入单人（Host & Play）世界，桥才会监听')
  } else {
    const lines = out.split(/\r?\n/).filter(Boolean)
    const servers = lines.filter(l => /-server/.test(l))
    if (servers.length > 0) {
      record('fail', '有 -server 的 tModLoader 在跑', servers[0].slice(0, 90),
        '它独占世界文件，客户端会进不去世界。先完整结束这个进程，再启动客户端')
    } else {
      record('pass', '游戏进程', `${lines.length} 个（客户端，无 -server）`)
    }
  }
} catch {
  record('warn', '游戏进程', '查不到（powershell 不可用）')
}

/* ── 5. 桥是否可达 ─────────────────────────────────────────────── */
const port = 47777
await new Promise((resolve) => {
  const sock = net.connect({ host: '127.0.0.1', port })
  const done = (level, detail, hint) => {
    record(level, `桥 127.0.0.1:${port}`, detail, hint)
    sock.destroy()
    resolve()
  }
  sock.setTimeout(2500)
  sock.on('connect', () => done('pass', 'TCP 可连'))
  sock.on('timeout', () => done('warn', '连接超时', '游戏可能没进世界，或 mod 没启用'))
  sock.on('error', () => done('warn', '连不上', '游戏没起、没进世界，或 mod 没启用'))
})

/* ── 6. 文档与知识库一致性 ─────────────────────────────────────── */
for (const [label, cmd] of [
  ['知识库 lint', 'node scripts/kb-lint.mjs'],
  ['命令清单与源码一致', 'node scripts/kb-caps.mjs --check']
]) {
  try {
    execSync(cmd, { cwd: repoRoot, stdio: 'ignore' })
    record('pass', label)
  } catch {
    record('fail', label, '未通过', `跑 ${cmd} 看具体哪条不一致，按提示重生成`)
  }
}

/* ── 7. 落盘纪律：改过的文件是不是还在工作区里躺着没提交 ────────── */
// 规矩见 AGENTS.md §0.65：验证通过就当场落盘，不许攒。
// 攒批的代价是真实的——mount 下骑验证通过 53 分钟后，文档还写着「诚实拒绝」。
{
  let porcelain = ''
  try {
    porcelain = execSync('git status --porcelain', { cwd: repoRoot, encoding: 'utf8' })
  } catch {
    record('warn', '工作区状态', '读不到 git status', '确认这是 git 仓库')
    porcelain = null
  }
  if (porcelain !== null) {
    const lines = porcelain.split('\n').map(l => l.trim()).filter(Boolean)
    const code = lines.filter(l => /^[^\s]*\s+\S+\.(cs|csproj|js|mjs|json)$/.test(l))
    const docs = lines.filter(l => /^[^\s]*\s+.*\.md$/.test(l))
    if (lines.length === 0) {
      record('pass', '工作区', '干净，没有待落盘的改动')
    } else {
      const parts = []
      if (code.length) parts.push(`${code.length} 个代码文件`)
      if (docs.length) parts.push(`${docs.length} 个文档`)
      const other = lines.length - code.length - docs.length
      if (other > 0) parts.push(`${other} 个其他`)
      record('warn', '工作区有未提交的改动', parts.join(' + '),
        '按 AGENTS.md §0.65：验证通过就当场提交，代码/文档/用例/路由四条一起走。攒着最容易攒出「文档说不行、其实早行了」')
    }
  }
  // 本地领先/落后远端
  let ahead = ''
  try {
    ahead = execSync('git rev-list --left-right --count origin/main...HEAD',
      { cwd: repoRoot, encoding: 'utf8' }).trim()
  } catch { ahead = '' }
  if (ahead) {
    const [behind, lead] = ahead.split(/\s+/).map(Number)
    if (lead > 0) {
      record('warn', '本地领先远端', `${lead} 个 commit 没推`, 'GIT_TERMINAL_PROMPT=0 git push origin main')
    } else if (behind > 0) {
      record('warn', '本地落后远端', `${behind} 个 commit`, 'git pull --rebase')
    } else {
      record('pass', '与 origin/main 一致')
    }
  }
}

/* ── 输出 ─────────────────────────────────────────────────────── */
if (asJson) {
  console.log(JSON.stringify(results, null, 2))
} else {
  const icon = { pass: '  ok ', warn: ' WARN', fail: ' FAIL' }
  const counts = { pass: 0, warn: 0, fail: 0 }
  console.log('')
  for (const r of results) {
    counts[r.level]++
    if (quiet && r.level === 'pass') continue
    console.log(`${icon[r.level]}  ${r.name}${r.detail ? ' :: ' + r.detail : ''}`)
    if (r.hint && r.level !== 'pass') console.log(`        → ${r.hint}`)
  }
  console.log('')
  console.log(`  ${counts.pass} ok / ${counts.warn} warn / ${counts.fail} fail`)
  console.log('')
}

process.exit(results.some(r => r.level === 'fail') ? 1 : 0)
