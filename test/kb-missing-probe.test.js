import test from 'node:test'
import assert from 'node:assert/strict'
import { SCHEMA, validate, loadCases } from '../src/testcases.js'

// 用例库最危险的状态不是"缺东西"，而是"明明不缺却还写着缺"：
// 离线预检会把能做的条目报成缺口，缺口清单跟着假，改代码的人照着假清单干活。
// 这两条测试把反向检查钉住：知识一旦入库，声明必须回来删。

const CATEGORY = '移动探索'

function docWith(kbMissing, kbMissingProbes, kb = ['world_biome']) {
  return {
    schema: SCHEMA,
    categories: { [CATEGORY]: { phase: 1 } },
    cases: [
      {
        id: 'T01',
        phase: 1,
        category: CATEGORY,
        task: '构造出来的用例，只用来验反查',
        needs: { commands: [], kb, kbAnchors: [], kbMissing, kbMissingProbes },
      },
    ],
  }
}

test('知识已入库时，残留的 kbMissing 声明必须被反查抓出来', async () => {
  const text = 'world_biome 没有微光池的分布规律'
  const doc = docWith([text], [{ text, keywords: ['微光池', '丛林'] }])
  const v = await validate(doc)
  assert.deepEqual(v.errors, [], v.errors.join('\n'))
  assert.ok(
    v.warnings.some((w) => w.includes('知识已入库') && w.includes('微光池')),
    `应当警告"知识已入库"，实际：${JSON.stringify(v.warnings)}`,
  )
})

test('反查探针与 kbMissing 不同步时按错误处理，不许静默放过', async () => {
  const doc = docWith(['某条已经删掉的声明'], [
    { text: '另一条对不上的文本', keywords: ['微光池'] },
  ])
  const v = await validate(doc)
  assert.ok(
    v.errors.some((e) => e.includes('两边不同步')),
    `应当报两边不同步，实际：${JSON.stringify(v.errors)}`,
  )
})

test('真实用例库当前没有过期声明：修完就该一直保持 0 条', async () => {
  const v = await validate()
  const stale = v.warnings.filter((w) => w.includes('知识已入库'))
  assert.deepEqual(stale, [], `用例库里还有过期知识声明：\n${stale.join('\n')}`)
})

test('每条 kbMissing 都带反查探针，防止下次又靠人肉核对', async () => {
  const doc = await loadCases()
  const naked = []
  for (const c of doc.cases) {
    const missing = c.needs?.kbMissing ?? []
    if (missing.length === 0) continue
    const probes = c.needs?.kbMissingProbes ?? []
    for (const text of missing) {
      if (!probes.some((p) => p?.text === text)) naked.push(`${c.id}: ${text}`)
    }
  }
  assert.deepEqual(naked, [], `这些 kbMissing 没写 kbMissingProbes，无法自动反查：\n${naked.join('\n')}`)
})
