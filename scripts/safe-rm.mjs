#!/usr/bin/env node
/**
 * 安全删除守卫。
 *
 * 起因（2026-11-04 事故）：为了清掉一个名叫 `C:` 的垃圾目录，写了
 * `fs.rmSync('C:/' + name)`。Node 在 Windows 上把 `C:/bim` 当成**盘符绝对路径**，
 * 于是把 C 盘根目录 39 项（bim / bsk / fairy-* / node / NVIDIA / 桌面美化 …）
 * 永久删除——`rmSync` 不进回收站，卷影服务又是 Stopped，没有回滚。
 *
 * 本模块的唯一职责：**在删除之前证明目标在仓库工作区内**，
 * 任何盘符路径、UNC 路径、绝对路径、`..` 逃逸、junction/symlink 逃逸，一律拒绝。
 *
 * 用法（CLI）：
 *   node scripts/safe-rm.mjs scratch/tmp.txt
 *   node scripts/safe-rm.mjs scratch/tmpdir --recursive
 *   node scripts/safe-rm.mjs C:/bim          → 退出码 2，拒绝
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

export const REPO_ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')

/** 盘符路径（C:、c:/、C:\ 、C:foo 全算） */
const DRIVE_RE = /^[a-zA-Z]:/
/** UNC：\\server\share 或 //server/share */
const UNC_RE = /^(?:\\\\|\/\/)/

/**
 * 把 target 解析成一个确定在 root 内部的绝对路径；否则抛错（不删除任何东西）。
 * @param {string} target 用户给的相对路径
 * @param {string} root 允许的根（默认仓库根）
 * @returns {string} 仓库内的绝对路径
 */
export function resolveInsideRepo(target, root = REPO_ROOT) {
  if (typeof target !== 'string' || target.trim() === '') {
    throw new Error('拒绝：目标为空')
  }
  const raw = target.trim()

  // 1) 先按字面拦掉「看起来像盘符/网络路径」的写法。
  //    Windows 上 `path.resolve('C:/x')` 会解析成盘符路径，必须先拦，不能先 resolve。
  if (DRIVE_RE.test(raw)) {
    throw new Error(`拒绝：${raw} 是盘符路径（会指向真实磁盘，不是仓库内的相对路径）`)
  }
  if (UNC_RE.test(raw)) {
    throw new Error(`拒绝：${raw} 是 UNC/网络路径`)
  }
  if (path.isAbsolute(raw)) {
    throw new Error(`拒绝：${raw} 是绝对路径`)
  }

  // 2) 解析后必须在 root 内
  const rootAbs = path.resolve(root)
  const abs = path.resolve(rootAbs, raw)
  const rel = path.relative(rootAbs, abs)
  if (rel === '' || rel.startsWith('..') || path.isAbsolute(rel)) {
    throw new Error(`拒绝：${raw} 解析到 ${abs}，不在 ${rootAbs} 内`)
  }

  // 3) 再查真实路径，防 junction / symlink 逃逸（目标可能已存在）
  if (fs.existsSync(abs)) {
    const realRoot = fs.realpathSync(rootAbs)
    const realAbs = fs.realpathSync(abs)
    const realRel = path.relative(realRoot, realAbs)
    if (realRel === '' || realRel.startsWith('..') || path.isAbsolute(realRel)) {
      throw new Error(`拒绝：${raw} 实际指向 ${realAbs}，在仓库外`)
    }
  }

  return abs
}

/**
 * 安全删除。路径校验不通过就抛错，绝不落盘。
 * @param {string} target
 * @param {{recursive?: boolean, root?: string, dryRun?: boolean}} [opts]
 */
export function safeRemove(target, opts = {}) {
  const { recursive = false, root = REPO_ROOT, dryRun = false } = opts
  const abs = resolveInsideRepo(target, root)
  if (!fs.existsSync(abs)) return { removed: false, reason: 'not-found', path: abs }
  const st = fs.lstatSync(abs)
  if (st.isDirectory() && !recursive) {
    throw new Error(`拒绝：${abs} 是目录，需要显式 --recursive`)
  }
  if (!dryRun) {
    fs.rmSync(abs, { recursive, force: true, maxRetries: 2, retryDelay: 100 })
  }
  return { removed: !dryRun, reason: dryRun ? 'dry-run' : 'removed', path: abs, wasDirectory: st.isDirectory() }
}

// ── CLI ──────────────────────────────────────────────────────────────
const isMain = process.argv[1] && path.resolve(process.argv[1]) === path.resolve(fileURLToPath(import.meta.url))
if (isMain) {
  const args = process.argv.slice(2)
  const recursive = args.includes('--recursive') || args.includes('-r')
  const dryRun = args.includes('--dry-run')
  const target = args.find((a) => !a.startsWith('-'))
  if (!target) {
    console.error('用法: node scripts/safe-rm.mjs <仓库内相对路径> [--recursive] [--dry-run]')
    process.exit(2)
  }
  try {
    const r = safeRemove(target, { recursive, dryRun })
    console.log(JSON.stringify(r))
  } catch (e) {
    console.error(String(e.message ?? e))
    process.exit(2)
  }
}