import { test } from 'node:test'
import assert from 'node:assert/strict'
import fs from 'node:fs'
import os from 'node:os'
import path from 'node:path'
import { resolveInsideRepo, safeRemove, REPO_ROOT } from '../scripts/safe-rm.mjs'

// 这个测试存在的唯一理由：2026-11-04 用 `fs.rmSync('C:/' + name)` 删掉了
// C 盘根目录 39 项（rmSync 不进回收站，卷影服务 Stopped，无回滚）。
// 下面每一条 assert 都对应一种「会让路径跑到仓库外」的写法。

test('拒绝盘符路径：C:/bim（正是闯祸的那一种）', () => {
  assert.throws(() => resolveInsideRepo('C:/bim'), /盘符路径/)
})

test('拒绝盘符路径的各种写法', () => {
  for (const bad of ['C:/bim', 'c:\\bim', 'C:bim', 'D:/', 'z:foo']) {
    assert.throws(() => resolveInsideRepo(bad), /盘符路径/, `应拒绝 ${bad}`)
  }
})

test('拒绝 UNC / 网络路径', () => {
  for (const bad of ['\\\\server\\share', '//server/share']) {
    assert.throws(() => resolveInsideRepo(bad), /UNC/)
  }
})

test('拒绝绝对路径', () => {
  assert.throws(() => resolveInsideRepo('/etc/passwd'), /绝对路径/)
})

test('拒绝 .. 逃逸', () => {
  for (const bad of ['..', '../outside', 'scratch/../../outside', './../../x']) {
    assert.throws(() => resolveInsideRepo(bad), /不在 .* 内/)
  }
})

test('拒绝空目标', () => {
  assert.throws(() => resolveInsideRepo(''), /目标为空/)
  assert.throws(() => resolveInsideRepo('   '), /目标为空/)
})

test('仓库内的普通相对路径通过，且解析结果在仓库根下', () => {
  const abs = resolveInsideRepo('scratch/tmp-probe.txt')
  assert.equal(abs, path.join(REPO_ROOT, 'scratch', 'tmp-probe.txt'))
  assert.ok(abs.startsWith(REPO_ROOT))
})

test('safeRemove 真的能删仓库内文件，且不给 -r 就拒绝删目录', () => {
  const dir = fs.mkdtempSync(path.join(REPO_ROOT, 'scratch', 'safe-rm-'))
  const relFile = path.relative(REPO_ROOT, path.join(dir, 'a.txt'))
  fs.writeFileSync(path.join(dir, 'a.txt'), 'x')

  assert.throws(() => safeRemove(path.relative(REPO_ROOT, dir)), /需要显式 --recursive/)

  const r = safeRemove(relFile)
  assert.equal(r.removed, true)
  assert.equal(fs.existsSync(path.join(dir, 'a.txt')), false)

  const r2 = safeRemove(path.relative(REPO_ROOT, dir), { recursive: true })
  assert.equal(r2.removed, true)
  assert.equal(fs.existsSync(dir), false)
})

test('safeRemove 对盘符路径抛错，且什么都没删', () => {
  assert.throws(() => safeRemove('C:/nonexistent-should-not-matter'), /盘符路径/)
})

test('dry-run 不落盘', () => {
  const dir = fs.mkdtempSync(path.join(REPO_ROOT, 'scratch', 'safe-rm-dry-'))
  const rel = path.relative(REPO_ROOT, dir)
  const r = safeRemove(rel, { recursive: true, dryRun: true })
  assert.equal(r.removed, false)
  assert.equal(fs.existsSync(dir), true)
  fs.rmSync(dir, { recursive: true, force: true })
})

test('symlink 指向仓库外时拒绝（junction/软链接逃逸）', () => {
  const link = path.join(REPO_ROOT, 'scratch', 'safe-rm-link-probe')
  try {
    fs.rmSync(link, { force: true })
  } catch {}
  try {
    fs.symlinkSync(os.tmpdir(), link, 'junction')
  } catch {
    return // 无权限建链接就跳过，不把环境问题算成失败
  }
  try {
    assert.throws(() => resolveInsideRepo(path.relative(REPO_ROOT, link)), /在仓库外/)
  } finally {
    try {
      fs.unlinkSync(link)
    } catch {}
  }
})