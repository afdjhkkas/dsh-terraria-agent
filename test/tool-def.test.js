import assert from 'node:assert/strict'
import test from 'node:test'
import {
  assertSupportedJsonSchema,
  BuiltinJsonSchemaError,
  builtinDefineTool,
  coerceNumberArgs,
  isNumericParam,
  loosenNumberParams,
  parameterSchemaSpecToJsonSchema,
  validateJsonSchemaValue,
  valueSchemaSpecToJsonSchema,
} from '../src/tool-def.mjs'

/* ── oneOf ─────────────────────────────────────────────────────────────── */

test('oneOf compiles to a bare oneOf node and validates "exactly one branch"', () => {
  const schema = parameterSchemaSpecToJsonSchema({
    value: { oneOf: [{ type: 'string' }, { type: 'integer' }] },
  })
  assert.deepEqual(schema.properties.value, { oneOf: [{ type: 'string' }, { type: 'integer' }] })
  assert.deepEqual(validateJsonSchemaValue(schema, { value: 'x' }, ''), [])
  assert.deepEqual(validateJsonSchemaValue(schema, { value: 3 }, ''), [])
  assert.deepEqual(
    validateJsonSchemaValue(schema, { value: true }, ''),
    ['"value" must match exactly one oneOf branch (matched 0)'],
  )
})

test('oneOf reports an ambiguous match instead of picking a branch', () => {
  const schema = { oneOf: [{ type: 'number' }, { type: 'integer' }] }
  assert.deepEqual(validateJsonSchemaValue(schema, 2, ''), ['"arguments" must match exactly one oneOf branch (matched 2)'])
})

test('oneOf keeps annotations, honours required on a property, and rejects unsupported shapes', () => {
  const compiled = parameterSchemaSpecToJsonSchema({
    value: { required: true, description: 'either', oneOf: [{ type: 'string' }, { type: 'null' }] },
  })
  assert.deepEqual(compiled.required, ['value'])
  assert.deepEqual(compiled.properties.value, {
    oneOf: [{ type: 'string' }, { type: 'null' }],
    description: 'either',
  })

  assert.throws(
    () => valueSchemaSpecToJsonSchema({ type: 'string', oneOf: [{ type: 'string' }, { type: 'integer' }] }),
    /cannot declare both type and oneOf/,
  )
  assert.throws(
    () => valueSchemaSpecToJsonSchema({ oneOf: [{ type: 'string' }] }),
    /at least two/,
  )
  assert.throws(
    () => valueSchemaSpecToJsonSchema({ oneOf: [{ type: 'string' }, { type: 'integer' }], properties: {} }),
    /properties is not supported by the value schema DSL/,
  )
})

test('a tool whose parameter uses oneOf validates end to end', async () => {
  const tool = builtinDefineTool({
    name: 'probe',
    description: 'probe',
    parameters: { value: { oneOf: [{ type: 'string' }, { type: 'integer' }] } },
    output: { schema: { type: 'string' }, render: (_args, value) => [{ type: 'text', text: String(value) }] },
    async execute(args) { return typeof args.value },
  })
  assert.equal(await tool.execute({ value: 'x' }, {}), 'string')
  assert.equal(await tool.execute({ value: 3 }, {}), 'number')
  await assert.rejects(tool.execute({ value: true }, {}), /must match exactly one oneOf branch \(matched 0\)/)
})

/* ── required placement (host parity) ──────────────────────────────────── */

test('required is only legal as true on a property, and is lifted to the parent', () => {
  const compiled = parameterSchemaSpecToJsonSchema({ x: { type: 'integer', required: true }, y: { type: 'integer' } })
  assert.deepEqual(compiled.required, ['x'])
  assert.deepEqual(compiled.properties.x, { type: 'integer' })
  assert.equal('required' in compiled.properties.y, false)

  assert.throws(
    () => parameterSchemaSpecToJsonSchema({ x: { type: 'integer', required: false } }),
    /required 只能是 true/,
  )
  assert.throws(
    () => parameterSchemaSpecToJsonSchema({ xs: { type: 'array', items: { type: 'integer', required: true } } }),
    /items\.required is not supported by the value schema DSL/,
  )
  assert.throws(
    () => valueSchemaSpecToJsonSchema({ type: 'string', required: true }),
    /schema\.required is not supported by the value schema DSL/,
  )
  assert.throws(
    () => valueSchemaSpecToJsonSchema({ oneOf: [{ type: 'string', required: true }, { type: 'integer' }] }),
    /oneOf\[0\]\.required is not supported by the value schema DSL/,
  )
})

/* ── enum / const ──────────────────────────────────────────────────────── */

test('enum must be a non-empty array of values matching the declared type', () => {
  assert.deepEqual(
    valueSchemaSpecToJsonSchema({ type: 'string', enum: ['left', 'right'] }),
    { type: 'string', enum: ['left', 'right'] },
  )
  assert.throws(() => valueSchemaSpecToJsonSchema({ type: 'string', enum: [] }), /non-empty array of string values/)
  assert.throws(() => valueSchemaSpecToJsonSchema({ type: 'string', enum: ['a', 1] }), /non-empty array of string values/)
  assert.throws(() => valueSchemaSpecToJsonSchema({ type: 'integer', enum: [1, 1.5] }), /non-empty array of integer values/)
  assert.throws(() => valueSchemaSpecToJsonSchema({ type: 'boolean', enum: 'yes' }), /non-empty array of boolean values/)
})

test('const must match the declared type and any sibling enum', () => {
  assert.deepEqual(valueSchemaSpecToJsonSchema({ type: 'string', const: 'x' }), { type: 'string', const: 'x' })
  assert.throws(() => valueSchemaSpecToJsonSchema({ type: 'string', const: 1 }), /const must be a string value/)
  assert.throws(
    () => valueSchemaSpecToJsonSchema({ type: 'string', enum: ['a'], const: 'b' }),
    /const must be one of schema\.enum when both are declared/,
  )
})

/* ── arrays: holes are not dense JSON ──────────────────────────────────── */

test('a sparse array is rejected as not dense, while present elements are still validated', () => {
  const schema = { type: 'array', items: { type: 'integer' } }
  const sparse = [1, , 3] // eslint-disable-line no-sparse-arrays
  assert.deepEqual(validateJsonSchemaValue(schema, sparse, ''), ['"arguments" must be a dense lossless JSON array'])
  assert.deepEqual(validateJsonSchemaValue(schema, [1, 2, 3], ''), [])
  assert.deepEqual(validateJsonSchemaValue(schema, [1, 'x'], ''), ['"[1]" must be an integer'])
})

/* ── the subset assertion itself ───────────────────────────────────────── */

test('assertSupportedJsonSchema accepts every shape the compiler emits', () => {
  assert.doesNotThrow(() => assertSupportedJsonSchema({}))
  assert.doesNotThrow(() => assertSupportedJsonSchema({ oneOf: [{ type: 'string' }, { type: 'integer' }] }))
  assert.doesNotThrow(() => assertSupportedJsonSchema({
    type: 'object',
    properties: {
      a: { type: 'integer', description: 'a', default: 1, examples: [1] },
      b: { type: 'object', additionalProperties: false, properties: { c: { type: 'string', enum: ['x'] } } },
      d: { type: 'array', items: { type: 'boolean' } },
    },
    required: ['a'],
  }))
})

test('assertSupportedJsonSchema rejects the keywords the host rejects', () => {
  const cases = [
    [{ type: 'integer', minimum: 1 }, /minimum is not a supported keyword/],
    [{ type: 'integer', maximum: 9 }, /maximum is not a supported keyword/],
    [{ type: 'string', pattern: '^x' }, /pattern is not a supported keyword/],
    [{ type: 'array', items: { type: 'integer', required: true } }, /items\.required is not supported/],
    [{ type: 'object', required: ['nope'], properties: {} }, /required names "nope" which is not in properties/],
    [{ type: 'object', additionalProperties: 'yes' }, /additionalProperties must be a boolean/],
    [{ type: 'bogus' }, /type must be one of/],
    [{ type: 'string', enum: ['a', 2] }, /non-empty array of string values/],
    [{ type: 'string', items: { type: 'string' } }, /items is not supported on type "string"/],
    [{ required: true }, /requires type or oneOf/],
    [{ type: 'string', oneOf: [{ type: 'string' }, { type: 'integer' }] }, /cannot declare both type and oneOf/],
    [{ oneOf: [{ type: 'string' }, { type: 'integer' }], enum: ['a'] }, /enum is not supported beside oneOf/],
    [{ type: 'string', description: 42 }, /description must be a string/],
  ]
  for (const [schema, expected] of cases) {
    assert.throws(() => assertSupportedJsonSchema(schema), expected, JSON.stringify(schema))
  }
  assert.throws(() => assertSupportedJsonSchema({ type: 'string', minimum: 1 }), BuiltinJsonSchemaError)
  assert.throws(() => assertSupportedJsonSchema({ type: 'string', minimum: 1 }), error => {
    assert.equal(error.name, 'JsonSchemaError')
    assert.match(error.message, /^unsupported JSON schema: /)
    assert.ok(Array.isArray(error.violations) && error.violations.length > 0)
    return true
  })
})

/* ── numeric-string mitigation (kept for index.js to wire) ─────────────── */

test('isNumericParam only claims number/integer declarations', () => {
  assert.equal(isNumericParam({ type: 'number' }), true)
  assert.equal(isNumericParam({ type: 'integer' }), true)
  assert.equal(isNumericParam({ type: 'string' }), false)
  assert.equal(isNumericParam({ type: 'json' }), false)
  assert.equal(isNumericParam(null), false)
  assert.equal(isNumericParam('number'), false)
})

test('loosenNumberParams swaps only numeric declarations for json, without mutating the input', () => {
  const params = {
    x: { type: 'number', description: 'x', required: true },
    n: { type: 'integer' },
    label: { type: 'string' },
    flag: { type: 'boolean' },
  }
  const loosened = loosenNumberParams(params)
  assert.deepEqual(loosened.x, { type: 'json', description: 'x', required: true })
  assert.deepEqual(loosened.n, { type: 'json' })
  assert.equal(loosened.label, params.label)
  assert.equal(loosened.flag, params.flag)
  assert.equal(params.x.type, 'number', 'input table must not be mutated')
  // the loosened table must still compile, i.e. the pair is usable when wired
  assert.doesNotThrow(() => parameterSchemaSpecToJsonSchema(loosened))
})

test('loosenNumberParams is only for bare numeric params: an enum would not compile once loosened', () => {
  // Documented limitation, not a silent one: `type:"json"` cannot carry `enum`
  // (the host rejects it too). index.js has no numeric enum, so this is only a
  // trap for whoever wires the helper onto one.
  const loosened = loosenNumberParams({ n: { type: 'integer', enum: [1, 2] } })
  assert.deepEqual(loosened.n, { type: 'json', enum: [1, 2] })
  assert.throws(() => parameterSchemaSpecToJsonSchema(loosened), /enum is not supported by the value schema DSL/)
})

test('coerceNumberArgs turns numeric strings into numbers and leaves everything else alone', () => {
  const params = {
    x: { type: 'integer' },
    y: { type: 'number' },
    label: { type: 'string' },
    flag: { type: 'boolean' },
  }
  assert.deepEqual(coerceNumberArgs(params, { x: '100', y: '-32.5', label: '123', flag: true }), {
    x: 100, y: -32.5, label: '123', flag: true,
  })
  assert.deepEqual(coerceNumberArgs(params, { x: ' 100 ' }), { x: 100 })
  // non-numeric text, empty text and non-numeric params stay untouched
  for (const args of [{ x: 'abc' }, { x: '' }, { x: '   ' }, { label: '1' }, { x: 5 }]) {
    assert.equal(coerceNumberArgs(params, args), args, JSON.stringify(args))
  }
  // defensively pass through non-objects
  const args = { x: '1' }
  assert.equal(coerceNumberArgs(null, args), args)
  assert.equal(coerceNumberArgs(params, null), null)
  const arrayLike = ['1']
  assert.equal(coerceNumberArgs(params, arrayLike), arrayLike)
})

/* ── parity with the real host package, when this machine can resolve it ── */

let host = null
try {
  host = await import('@deepseek-ai/dsh-tools')
} catch {
  host = null
}

test('oneOf compiles byte-identically to the host package', { skip: !host }, () => {
  const spec = { oneOf: [{ type: 'string' }, { type: 'integer', description: 'n' }] }
  assert.deepEqual(valueSchemaSpecToJsonSchema(spec), host.valueSchemaSpecToJsonSchema(spec))
})

test('the host accepts every schema this shim emits', { skip: !host }, () => {
  const parameterSpecs = [
    { a: { type: 'string', required: true, enum: ['x', 'y'] }, b: { type: 'integer' } },
    { v: { oneOf: [{ type: 'string' }, { type: 'null' }] }, o: { type: 'object', additionalProperties: true, properties: { k: { type: 'number' } } } },
    { arr: { type: 'array', items: { oneOf: [{ type: 'string' }, { type: 'integer' }] } } },
    {},
  ]
  for (const spec of parameterSpecs) {
    const mine = parameterSchemaSpecToJsonSchema(spec)
    assert.doesNotThrow(() => host.assertSupportedJsonSchema(mine))
    assert.deepEqual(mine, host.parameterSchemaSpecToJsonSchema(spec))
  }
})