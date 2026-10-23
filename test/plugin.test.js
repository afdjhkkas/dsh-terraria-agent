import assert from 'node:assert/strict'
import test from 'node:test'
import { apply, inject, name } from '../index.js'

function loadTools(config) {
  const registered = []
  const dispose = apply({
    tools: {
      register(tool) {
        registered.push(tool)
      },
    },
  }, config)
  return { registered, dispose }
}

test('exports a DSH plugin and registers all Terraria tools with valid schemas', () => {
  assert.equal(name, 'dsh-terraria-agent')
  assert.deepEqual(inject, ['tools'])

  const { registered, dispose } = loadTools()
  try {
    assert.equal(registered.length, 18)
    assert.equal(new Set(registered.map(tool => tool.name)).size, registered.length)

    for (const tool of registered) {
      assert.equal(tool.parameters.type, 'object')
      assert.ok(tool.output?.schema)
      assert.equal(tool.output.schema.type, 'string')
      for (const node of Object.values(tool.parameters.properties ?? {})) {
        assert.equal('minimum' in node, false)
        assert.equal('maximum' in node, false)
        assert.equal('required' in node, false)
      }
    }

    const tile = registered.find(tool => tool.name === 'terraria_tile')
    assert.deepEqual(tile.parameters.required, ['x', 'y'])
    assert.equal(tile.parameters.properties.x.type, 'integer')

    const move = registered.find(tool => tool.name === 'terraria_move')
    assert.deepEqual(move.parameters.properties.direction.enum, ['left', 'right', 'up', 'down'])
  } finally {
    dispose()
  }
})

test('rejects non-loopback bridge hosts before registering tools', () => {
  assert.throws(() => loadTools({ host: '192.168.1.10' }), /127\.0\.0\.1/)
})
