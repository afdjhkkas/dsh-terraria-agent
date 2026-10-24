import assert from 'node:assert/strict'
import test from 'node:test'
import { apply, inject, name } from '../index.js'
import { assertSupportedJsonSchema, parameterSchemaSpecToJsonSchema } from '../src/tool-def.mjs'

function loadTools(config, { logger } = {}) {
  const registered = []
  const dispose = apply({
    tools: {
      register(tool) {
        registered.push(tool)
      },
    },
    ...(logger ? { logger } : {}),
  }, config)
  return { registered, dispose }
}

test('exports a DSH plugin and registers the Terraria tools with schemas the DSH layer accepts', () => {
  assert.equal(name, 'dsh-terraria-agent')
  assert.deepEqual(inject, ['tools'])

  const { registered, dispose } = loadTools()
  try {
    assert.ok(registered.length >= 18, `expected >= 18 tools, got ${registered.length}`)
    assert.equal(new Set(registered.map(tool => tool.name)).size, registered.length)
    for (const tool of registered) {
      assert.match(tool.name, /^terraria_/)
      // Every registered schema must be inside the subset both the host and the
      // builtin shim accept — this catches `minimum`, a stray `required` inside
      // `items`, a mistyped enum, etc. at test time instead of on a user's box.
      assertSupportedJsonSchema(tool.parameters)
      assertSupportedJsonSchema(tool.output.schema)
      assert.equal(tool.parameters.type, 'object')
      assert.equal(tool.output.schema.type, 'string')
      // `required` belongs to the parent object, never to a property node.
      for (const node of Object.values(tool.parameters.properties ?? {})) {
        assert.equal('required' in node, false, `${tool.name}: property node carries required`)
        assert.equal('minimum' in node, false, `${tool.name}: property node carries minimum`)
        assert.equal('maximum' in node, false, `${tool.name}: property node carries maximum`)
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

test('a non-loopback bridge host degrades to loopback instead of disabling every tool', () => {
  // Throwing here used to abort apply(), so DSH logged a dead fiber and the model
  // silently lost every tool. A wrong host is now a warning plus loopback.
  const warnings = []
  const { registered, dispose } = loadTools({ host: '192.168.1.10' }, {
    logger: { warn: message => warnings.push(String(message)) },
  })
  try {
    assert.ok(registered.length >= 18, 'tools must still be registered')
    assert.ok(warnings.some(message => /192\.168\.1\.10/.test(message)), 'the ignored host must be reported')
  } finally {
    dispose()
  }
})

test('the parameter DSL rejects range keywords instead of silently dropping them', () => {
  // index.js used to pass {minimum}/{maximum} to a helper that discarded them,
  // so the README promised bounds the schema never enforced. If a future tool
  // reaches for a bound again, it must fail here rather than lie to the model.
  assert.throws(
    () => parameterSchemaSpecToJsonSchema({ radius: { type: 'integer', minimum: 1, maximum: 48 } }),
    /is not supported by the value schema DSL/,
  )
})

test('the parameter DSL rejects a required flag outside a property position', () => {
  assert.throws(
    () => parameterSchemaSpecToJsonSchema({ xs: { type: 'array', items: { type: 'integer', required: true } } }),
    /items\.required is not supported by the value schema DSL/,
  )
})