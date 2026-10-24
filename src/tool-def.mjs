/**
 * 工具定义：优先用宿主 `@deepseek-ai/dsh-tools` 的 `defineTool`，拿不到用这里的内置等价实现。
 * ============================================================================
 * 🔴 2026-10-04（GitHub issue #5）：官方 dsh-desktop 上插件报 `failed to import` ——
 *    真实原因是 `index.js` 顶层**静态** import 了 `@deepseek-ai/dsh-tools` 与 `schemastery`，
 *    而它们是 **optional peer**（包管理器永远不装；宿主解析层在 desktop 上又喂不进来）
 *    ⇒ 模块**链接期**就失败，loader 拿不到 fiber。同一个坑本仓库在 dsh-llm 上踩过一次
 *    （见 src/user-message.mjs 头注释），所以这里沿用同一条路：**宿主优先，拿不到用内置**。
 *
 * 内置实现与宿主逐字对齐的三处关键（对不上会静默走样，"无宿主模拟"自检会当场红）：
 *   ① JSON Schema 的 **key 顺序**：标量 `{type, 然后 description/title/default/examples, enum, const}`；
 *      object `{type, 注解, additionalProperties, properties(声明了才有), required(非空才有)}`；
 *      array `{type, 注解, items}`；oneOf `{oneOf, 注解}`；`json` = 仅注解、无 type；
 *      属性上的 `required:true` 收进**父级** required 数组。
 *   ② 入参校验的**报错文案与路径**（宿主把它当模型可见信息）：
 *      `"arguments" must be an object` · `missing required property "x"` ·
 *      `"y" is not a declared property (additionalProperties: false)` · …（见 validateJsonSchemaValue）。
 *   ③ **作者面 schema 只接受宿主的子集**（见 assertSupportedJsonSchema）：
 *      未知关键字（`minimum`/`maximum`/`pattern`…）、`items`/`output.schema` 里的 `required`、
 *      与类型不符的 `enum`/`const`、`oneOf` 旁的关键字，一律在**定义期**抛 `JsonSchemaError`
 *      （内置只有同名、同 shape 的 `BuiltinJsonSchemaError`，拿不到宿主类）。
 *
 * ⚠️ 已知退化（任务书认可）：内置抛的 ToolArgsError / JsonSchemaError 不是宿主 `HarnessError` 子类
 *    （拿不到宿主类），宿主显示层会退化成通用错误；文案保持逐字一致。
 * ============================================================================
 */

const ANNOTATION_KEYS = ['description', 'title', 'default', 'examples']
/** 宿主 SCHEMA_TYPES 的顺序，报错文案里用 join('/') 对齐 */
const SUPPORTED_TYPES = ['object', 'array', 'string', 'number', 'integer', 'boolean', 'null']
const SCALAR_TYPE_LIST = ['string', 'number', 'integer', 'boolean', 'null']
const SCALAR_TYPES = new Set(SCALAR_TYPE_LIST)
const CONSTRAINT_KEYWORDS = new Set(['type', 'oneOf', 'properties', 'required', 'additionalProperties', 'items', 'enum', 'const'])
/** oneOf 旁边不允许出现的关键字（宿主 ONE_OF_SIBLING_KEYWORDS） */
const ONE_OF_SIBLINGS = ['properties', 'required', 'additionalProperties', 'items', 'enum', 'const']
const KEYWORD_SUBSET = 'type/oneOf/properties/required/additionalProperties/items/enum/const + annotations'

/** 宿主那份 `defineTool`（解析不到就是 null）；`loadNote` 供调用方记日志 */
export let hostDefineTool = null
export let loadNote = ''
try {
  const mod = await import('@deepseek-ai/dsh-tools')
  hostDefineTool = typeof mod?.defineTool === 'function' ? mod.defineTool : null
  if (!hostDefineTool) loadNote = '包里没有 defineTool 导出'
} catch (e) {
  loadNote = `解析不到 @deepseek-ai/dsh-tools（${e?.code ?? e?.message ?? e}）`
}

/** 诊断用：现在走的是哪条路（日志/自检会报） */
export const toolDefKind = () => (typeof hostDefineTool === 'function' ? 'host' : 'builtin')

/**
 * 定义工具：**优先宿主实现**（形状永远跟得上宿主），拿不到用内置等价实现。
 * 两条路产出的注册形状一致：`{name, description, parameters, output:{schema, render}, [timeoutMs], execute}`。
 */
export function defineTool(options) {
  return typeof hostDefineTool === 'function' ? hostDefineTool(options) : builtinDefineTool(options)
}

/* ───────────────────────── 错误类型 ───────────────────────── */

/**
 * 作者面 schema 不合法（宿主是 `JsonSchemaError extends HarnessError`）。
 * 拿不到宿主类，所以这里做成同名同 shape：`name`、`violations`、`unsupported JSON schema: ...`。
 */
export class BuiltinJsonSchemaError extends Error {
  constructor(violations) {
    super(`unsupported JSON schema: ${violations.join('; ')}`)
    this.name = 'JsonSchemaError'
    this.violations = violations
    this.code = 'INVALID_SCHEMA'
  }
}

function authorError(message) {
  throw new BuiltinJsonSchemaError([message])
}

function assertKeys(source, allowed, path) {
  for (const key of Object.keys(source)) {
    if (!allowed.includes(key)) authorError(`${path}.${key} is not supported by the value schema DSL（可用：${allowed.join('/')}）`)
  }
}

/** 按宿主编译器的固定顺序拷注解（description → title → default → examples） */
function copyAnnotations(source, target) {
  for (const key of ANNOTATION_KEYS) if (Object.hasOwn(source, key)) target[key] = source[key]
}

/** 标量是否匹配声明的类型（宿主 scalarMatches 的等价物） */
function scalarMatches(type, value) {
  switch (type) {
    case 'string': return typeof value === 'string'
    case 'number': return isJsonNumber(value)
    case 'integer': return isJsonNumber(value) && Number.isInteger(value)
    case 'boolean': return typeof value === 'boolean'
    case 'null': return value === null
    default: return false
  }
}

/* ───────────────────────── 作者面 spec → JSON Schema ───────────────────────── */

/**
 * 编译一个"值节点"（output.schema 的根 / array 的 items / object 的属性值 / oneOf 分支）。
 * `allowRequired` 只有"属性位置的节点"才为 true：宿主只允许属性上写 `required: true`，
 * 写在 items / output.schema / oneOf 分支上一律报错。
 */
function compileValueNode(spec, path, allowRequired = false) {
  if (spec === null || typeof spec !== 'object' || Array.isArray(spec)) authorError(`${path} 必须是对象`)
  const own = allowRequired ? ['required'] : []
  const type = Object.hasOwn(spec, 'type') ? spec.type : undefined
  const node = {}

  if (Object.hasOwn(spec, 'oneOf')) {
    // 宿主语义：oneOf 与 type 互斥；至少两个分支；分支内部不许再写 required。
    if (Object.hasOwn(spec, 'type')) authorError(`${path} cannot declare both type and oneOf`)
    assertKeys(spec, ['oneOf', ...ANNOTATION_KEYS, ...own], path)
    if (!Array.isArray(spec.oneOf) || spec.oneOf.length < 2) authorError(`${path}.oneOf must be an array of at least two schemas`)
    node.oneOf = spec.oneOf.map((branch, index) => compileValueNode(branch, `${path}.oneOf[${index}]`, false))
    copyAnnotations(spec, node)
    return node
  }
  if (type === 'json') {
    // 「任意 JSON 值」：宿主编译成**仅注解**的节点（无 type），校验走 lossless JSON 那条路
    assertKeys(spec, ['type', ...ANNOTATION_KEYS, ...own], path)
    copyAnnotations(spec, node)
    return node
  }
  if (type === 'object') {
    assertKeys(spec, ['type', ...ANNOTATION_KEYS, ...own, 'properties', 'additionalProperties'], path)
    if (typeof spec.additionalProperties !== 'boolean') authorError(`${path}.additionalProperties 必须显式写 true/false`)
    node.type = 'object'
    copyAnnotations(spec, node)
    node.additionalProperties = spec.additionalProperties
    if (Object.hasOwn(spec, 'properties')) {
      const compiled = compilePropertyMap(spec.properties, `${path}.properties`)
      node.properties = compiled.properties
      if (compiled.required !== undefined) node.required = compiled.required
    }
    return node
  }
  if (type === 'array') {
    assertKeys(spec, ['type', ...ANNOTATION_KEYS, ...own, 'items'], path)
    node.type = 'array'
    copyAnnotations(spec, node)
    if (Object.hasOwn(spec, 'items')) node.items = compileValueNode(spec.items, `${path}.items`, false)
    return node
  }
  if (SCALAR_TYPES.has(type)) {
    assertKeys(spec, ['type', ...ANNOTATION_KEYS, ...own, 'enum', 'const'], path)
    node.type = type
    copyAnnotations(spec, node)
    if (Object.hasOwn(spec, 'enum')) {
      const allowed = spec.enum
      if (!Array.isArray(allowed) || allowed.length === 0 || !allowed.every(entry => scalarMatches(type, entry))) {
        authorError(`${path}.enum must be a non-empty array of ${type} values`)
      }
      node.enum = Array.from(allowed)
    }
    if (Object.hasOwn(spec, 'const')) {
      if (!scalarMatches(type, spec.const)) authorError(`${path}.const must be a ${type} value`)
      else if (Object.hasOwn(node, 'enum') && !node.enum.includes(spec.const)) authorError(`${path}.const must be one of ${path}.enum when both are declared`)
      node.const = spec.const
    }
    return node
  }
  authorError(`${path}.type 必须是 string/number/integer/boolean/null/array/object/json（拿到 ${JSON.stringify(type)}）`)
}

/**
 * 编译一个属性表（`parameters` 的根 / 嵌套 object 的 properties）。
 * 🔴 属性上的 `required: true` 在这里收进**父级** required（按作者顺序）；属性节点本身不带 required。
 */
function compilePropertyMap(spec, path) {
  if (spec === null || typeof spec !== 'object' || Array.isArray(spec)) authorError(`${path} 必须是"属性名 → 值节点"的对象`)
  const properties = {}
  const required = []
  for (const [key, prop] of Object.entries(spec)) {
    if (prop === null || typeof prop !== 'object' || Array.isArray(prop)) authorError(`${path}.${key} 必须是值节点对象`)
    if (Object.hasOwn(prop, 'required') && prop.required !== true) authorError(`${path}.${key}.required 只能是 true`)
    if (prop.required === true) required.push(key)
    properties[key] = compileValueNode(prop, `${path}.${key}`, true)
  }
  const out = { properties }
  if (required.length > 0) out.required = required
  return out
}

/** `parameters`（属性表）→ 宿主形状的根 schema：`{type:'object', properties[, required]}` */
export function parameterSchemaSpecToJsonSchema(spec) {
  const compiled = compilePropertyMap(spec, 'parameters')
  const schema = { type: 'object', properties: compiled.properties }
  if (compiled.required !== undefined) schema.required = compiled.required
  assertSupportedJsonSchema(schema)
  return schema
}

/** `output.schema`（单值节点）→ 宿主形状的 raw schema */
export function valueSchemaSpecToJsonSchema(spec) {
  const schema = compileValueNode(spec, 'schema', false)
  assertSupportedJsonSchema(schema)
  return schema
}

/* ───────────────────────── 作者面 schema 只能是宿主的子集 ───────────────────────── */

/**
 * 校验一棵**已编译**的 raw schema 是否属于宿主支持的子集。
 * 逐条对齐宿主 `assertSupportedJsonSchema`：未知关键字、required 的位置/指向、
 * enum/const 与类型、oneOf 旁边的关键字、additionalProperties 必须是布尔。
 *
 * @param {unknown} schema
 * @throws {BuiltinJsonSchemaError}（宿主是 JsonSchemaError）
 */
export function assertSupportedJsonSchema(schema) {
  const violations = []
  const seen = new Set()

  const walk = (node, path) => {
    if (node === null || typeof node !== 'object' || Array.isArray(node)) {
      violations.push(`${path} must be a schema object`)
      return
    }
    if (seen.has(node)) {
      violations.push(`${path} is circular`)
      return
    }
    seen.add(node)

    for (const key of Object.keys(node)) {
      if (CONSTRAINT_KEYWORDS.has(key)) continue
      if (ANNOTATION_KEYS.includes(key)) {
        if (!safelyIsJsonValue(node[key])) violations.push(`${path}.${key} annotation must be lossless JSON data`)
        continue
      }
      violations.push(`${path}.${key} is not a supported keyword (subset: ${KEYWORD_SUBSET})`)
    }
    if (Object.hasOwn(node, 'description') && typeof node.description !== 'string') violations.push(`${path}.description must be a string`)
    if (Object.hasOwn(node, 'title') && typeof node.title !== 'string') violations.push(`${path}.title must be a string`)

    const hasType = Object.hasOwn(node, 'type')
    const hasOneOf = Object.hasOwn(node, 'oneOf')
    if (hasType && hasOneOf) {
      violations.push(`${path} cannot declare both type and oneOf`)
      seen.delete(node)
      return
    }
    if (!hasType && !hasOneOf) {
      // 仅注解的节点是合法的"任意 JSON 值"；但约束关键字必须有 type 或 oneOf 支撑
      for (const key of ONE_OF_SIBLINGS) if (Object.hasOwn(node, key)) violations.push(`${path}.${key} requires type or oneOf`)
      seen.delete(node)
      return
    }
    if (hasOneOf) {
      for (const key of ONE_OF_SIBLINGS) if (Object.hasOwn(node, key)) violations.push(`${path}.${key} is not supported beside oneOf`)
      const branches = node.oneOf
      if (!Array.isArray(branches) || branches.length < 2) violations.push(`${path}.oneOf must be an array of at least two schemas`)
      else branches.forEach((branch, index) => walk(branch, `${path}.oneOf[${index}]`))
      seen.delete(node)
      return
    }

    const type = node.type
    if (typeof type !== 'string' || !SUPPORTED_TYPES.includes(type)) {
      violations.push(Array.isArray(type)
        ? `${path}.type must be a single type string (type arrays are not supported)`
        : `${path}.type must be one of ${SUPPORTED_TYPES.join('/')}`)
      seen.delete(node)
      return
    }
    for (const [key, types] of Object.entries({
      properties: ['object'],
      required: ['object'],
      additionalProperties: ['object'],
      items: ['array'],
      enum: SCALAR_TYPE_LIST,
      const: SCALAR_TYPE_LIST,
    })) {
      if (Object.hasOwn(node, key) && !types.includes(type)) violations.push(`${path}.${key} is not supported on type "${type}"`)
    }

    if (type === 'object') {
      if (Object.hasOwn(node, 'properties')) {
        const properties = node.properties
        if (!isPlainJsonRecord(properties)) violations.push(`${path}.properties must be an object of schemas`)
        else for (const [key, child] of Object.entries(properties)) walk(child, `${path}.properties.${key}`)
      }
      if (Object.hasOwn(node, 'required')) {
        const required = node.required
        if (!Array.isArray(required) || required.some(entry => typeof entry !== 'string')) {
          violations.push(`${path}.required must be an array of strings`)
        } else {
          const declared = isPlainJsonRecord(node.properties) ? node.properties : {}
          for (const key of required) if (!Object.hasOwn(declared, key)) violations.push(`${path}.required names "${key}" which is not in properties`)
        }
      }
      if (Object.hasOwn(node, 'additionalProperties') && typeof node.additionalProperties !== 'boolean') {
        violations.push(`${path}.additionalProperties must be a boolean`)
      }
    } else if (type === 'array') {
      if (Object.hasOwn(node, 'items')) walk(node.items, `${path}.items`)
    } else {
      if (Object.hasOwn(node, 'enum')) {
        const allowed = node.enum
        if (!Array.isArray(allowed) || allowed.length === 0 || !allowed.every(entry => scalarMatches(type, entry))) {
          violations.push(`${path}.enum must be a non-empty array of ${type} values`)
        }
      }
      if (Object.hasOwn(node, 'const')) {
        if (!scalarMatches(type, node.const)) violations.push(`${path}.const must be a ${type} value`)
        else if (Array.isArray(node.enum) && !node.enum.includes(node.const)) violations.push(`${path}.const must be one of ${path}.enum when both are declared`)
      }
    }
    seen.delete(node)
  }

  walk(schema, 'schema')
  if (violations.length > 0) throw new BuiltinJsonSchemaError(violations)
}

/* ── 入参校验（宿主 validateJsonSchemaValue 的子集，文案/路径逐字对齐） ── */

const diagnosticPath = (path) => (path === '' ? 'arguments' : path)
const propertyPath = (path, key) => (path === '' ? key : `${path}.${key}`)
const losslessViolation = (path) => [`"${diagnosticPath(path)}" must be a lossless JSON value`]

const isJsonNumber = (value) => typeof value === 'number' && Number.isFinite(value) && !Object.is(value, -0)

function isPlainJsonRecord(value) {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) return false
  try {
    const proto = Object.getPrototypeOf(value)
    return proto === Object.prototype || proto === null
  } catch { return false }
}

/** 无损 JSON 判定（宿主 isJsonValue 的精简版：覆盖 JSON.parse 产出的形态；Date/函数/NaN/-0/稀疏数组都拒） */
function isLosslessJsonValue(value, seen = new Set()) {
  if (value === null || typeof value === 'string' || typeof value === 'boolean') return true
  if (typeof value === 'number') return isJsonNumber(value)
  if (typeof value !== 'object') return false
  if (seen.has(value)) return false
  seen.add(value)
  let ok = true
  if (Array.isArray(value)) {
    // 稀疏数组 / 带非索引属性的数组都不是无损 JSON 数组
    if (Object.keys(value).length !== value.length) ok = false
    for (let i = 0; ok && i < value.length; i++) ok = Object.hasOwn(value, i) && isLosslessJsonValue(value[i], seen)
  } else if (isPlainJsonRecord(value)) {
    for (const key of Object.keys(value)) {
      if (!ok) break
      ok = isLosslessJsonValue(value[key], seen)
    }
  } else ok = false
  seen.delete(value)
  return ok
}

const safelyIsJsonValue = (value) => { try { return isLosslessJsonValue(value) } catch { return false } }

function checkScalarValue(node, value, path) {
  if (Object.hasOwn(node, 'enum') && !node.enum.includes(value)) return [`"${diagnosticPath(path)}" must be one of ${JSON.stringify(node.enum)}`]
  if (Object.hasOwn(node, 'const') && value !== node.const) return [`"${diagnosticPath(path)}" must be ${JSON.stringify(node.const)}`]
  return []
}

function checkObjectValue(node, value, path) {
  if (!isPlainJsonRecord(value)) return [`"${diagnosticPath(path)}" must be an object`]
  const properties = Object.hasOwn(node, 'properties') ? (node.properties ?? {}) : {}
  const required = Object.hasOwn(node, 'required') ? (node.required ?? []) : []
  const out = []
  for (const key of required) {
    if (!Object.hasOwn(value, key) || value[key] === undefined) out.push(`missing required property "${propertyPath(path, key)}"`)
  }
  for (const [key, child] of Object.entries(properties)) {
    if (!Object.hasOwn(value, key) || value[key] === undefined) continue
    for (const v of validateJsonSchemaValue(child, value[key], propertyPath(path, key))) out.push(v)
  }
  if (node.additionalProperties === false) {
    for (const key of Object.keys(value)) {
      if (!Object.hasOwn(properties, key)) out.push(`"${propertyPath(path, key)}" is not a declared property (additionalProperties: false)`)
    }
  }
  if (out.length > 0) return out
  return safelyIsJsonValue(value) ? [] : [`"${diagnosticPath(path)}" must be a lossless JSON object`]
}

function checkArrayValue(node, value, path) {
  if (!Array.isArray(value)) return [`"${diagnosticPath(path)}" must be an array`]
  const out = []
  const items = Object.hasOwn(node, 'items') ? node.items : undefined
  if (items !== undefined) {
    // 只校验真实存在的下标：宿主用 flatMap 跳过空洞，空洞在最后统一报
    // "must be a dense lossless JSON array"，而不是冒充某个元素的类型错误。
    for (let i = 0; i < value.length; i++) {
      if (!Object.hasOwn(value, i)) continue
      for (const v of validateJsonSchemaValue(items, value[i], `${path}[${i}]`)) out.push(v)
    }
  }
  if (out.length > 0) return out
  return safelyIsJsonValue(value) ? [] : [`"${diagnosticPath(path)}" must be a dense lossless JSON array`]
}

/**
 * 校验一个值（宿主 validateJsonSchemaValue 的子集；root 调用的 path 用 `''`，报错里会显示成 `"arguments"`）。
 * @returns {string[]} 违规列表（空 = 合法）
 */
export function validateJsonSchemaValue(schema, value, path = '') {
  if (schema !== null && typeof schema === 'object' && Object.hasOwn(schema, 'oneOf')) {
    // 宿主语义：恰好一个分支匹配
    let matches = 0
    for (const branch of schema.oneOf) {
      if (validateJsonSchemaValue(branch, value, path).length === 0) matches += 1
    }
    return matches === 1 ? [] : [`"${diagnosticPath(path)}" must match exactly one oneOf branch (matched ${matches})`]
  }
  const nodeType = Object.hasOwn(schema, 'type') ? schema.type : undefined
  if (nodeType === undefined) return safelyIsJsonValue(value) ? [] : losslessViolation(path)
  switch (nodeType) {
    case 'object': return checkObjectValue(schema, value, path)
    case 'array': return checkArrayValue(schema, value, path)
    case 'string':
      return typeof value === 'string' ? checkScalarValue(schema, value, path) : [`"${diagnosticPath(path)}" must be a string`]
    case 'number': {
      if (typeof value !== 'number') return [`"${diagnosticPath(path)}" must be a number`]
      if (!isJsonNumber(value)) return [`"${diagnosticPath(path)}" must be a finite JSON number`]
      return checkScalarValue(schema, value, path)
    }
    case 'integer':
      return !isJsonNumber(value) || !Number.isInteger(value)
        ? [`"${diagnosticPath(path)}" must be an integer`]
        : checkScalarValue(schema, value, path)
    case 'boolean':
      return typeof value === 'boolean' ? checkScalarValue(schema, value, path) : [`"${diagnosticPath(path)}" must be a boolean`]
    case 'null':
      return value === null ? checkScalarValue(schema, value, path) : [`"${diagnosticPath(path)}" must be null`]
    default:
      authorError(`schema.type 不认识：${JSON.stringify(nodeType)}`)
  }
}

/* ───────── 数字参数的容错（2026-10-23 实机：模型把坐标写成字符串）─────────
 * 现象：`mc_act {mode:"place", x:-531, y:72, z:"-32"}` → 宿主按参数表里的 `type:"number"`
 *   当场拒掉（`invalid arguments: "z" must be a number`），一次完全合法的调用白费。
 * 做法分两步，**严格表一个字不改**（`numOf` 之类照旧要拿到数字）：
 *   ① `loosenNumberParams`：注册给宿主的那份表里，number/integer 参数换成 `type:"json"`
 *      （宿主里"任意 JSON 值"的标准写法，`mc_admin_config` 的 `value` 早就这么用），
 *      宿主就不再因为字符串而拒；
 *   ② `coerceNumberArgs`：`execute` 前照**严格表**把"看上去是数字的字符串"转成真数字，
 *      下游（`numOf`／直接读 `args.count` 的地方）永远拿到 number；`"abc"` 之类留在原地，
 *      由 `numOf` 报出原文。只动严格表里声明为 number/integer 的键 —— `name:"64"`、
 *      `text:"123"` 这类文本参数一个都不碰。
 *
 * ⚠️ 2026-10-24 审计：这两个函数目前**没有任何调用方**（index.js 只 import 了 defineTool）。
 *    行为已由 test/tool-def.test.js 固定，接入前请保持契约不变。
 */
export const NUMERIC_PARAM_TYPES = new Set(['number', 'integer'])

/** 这个属性节点是不是数字参数（声明在严格表里的 number/integer） */
export function isNumericParam(spec) {
  return Boolean(spec) && typeof spec === 'object' && NUMERIC_PARAM_TYPES.has(spec.type)
}

/** 注册给宿主用：数字参数 → `type:"json"`（其余原样透传，不改入参对象） */
export function loosenNumberParams(params) {
  if (!params || typeof params !== 'object' || Array.isArray(params)) return params
  const out = {}
  for (const [k, v] of Object.entries(params)) out[k] = isNumericParam(v) ? { ...v, type: 'json' } : v
  return out
}

/** 执行前：数字参数上的"数字字符串"转成 number（别的键一个都不碰；没改就返回原对象） */
export function coerceNumberArgs(params, args) {
  if (!args || typeof args !== 'object' || Array.isArray(args)) return args
  let touched = false
  const out = { ...args }
  for (const [k, v] of Object.entries(params ?? {})) {
    if (!isNumericParam(v)) continue
    const val = out[k]
    if (typeof val === 'string' && val.trim() !== '' && Number.isFinite(Number(val))) { out[k] = Number(val); touched = true }
  }
  return touched ? out : args
}

/** 内置的入参错误（宿主版是 HarnessError 子类；这里形状对齐但继承不到——见文件头"已知退化"） */
export class BuiltinToolArgsError extends Error {
  constructor(violations) {
    super(`invalid arguments: ${violations.join('; ')}`)
    this.name = 'ToolArgsError'
    this.violations = violations
    this.code = 'INVALID_ARGS'
  }
}

/**
 * 内置 defineTool：照宿主实现组装 `{name, description, parameters, output:{schema, render}, [timeoutMs], execute}`，
 * execute 先校验入参（违规抛 BuiltinToolArgsError）再调用户实现。
 * 宿主支持而这里没用到的 option（finalizeContent / presentCall / presentResult / isConcurrencySafe）**明确抛错** ——
 * 谁将来真要用，自检的"无宿主模拟"会当场红，而不是在两台机器上行为悄悄不同。
 */
export function builtinDefineTool(options) {
  for (const unsupported of ['finalizeContent', 'presentCall', 'presentResult', 'isConcurrencySafe']) {
    if (options[unsupported] !== undefined) authorError(`${unsupported} 暂不支持（内置 defineTool 兜底：宿主包不可解析的环境）`)
  }
  if (options.timeoutMs !== undefined && (!Number.isFinite(options.timeoutMs) || options.timeoutMs <= 0)) {
    throw new Error(`defineTool(${options.name}): timeoutMs must be a positive finite number`)
  }
  const userExecute = options.execute
  const userRender = options.output?.render
  const parameters = parameterSchemaSpecToJsonSchema(options.parameters)
  const outputSchema = valueSchemaSpecToJsonSchema(options.output?.schema)
  const validate = (args) => validateJsonSchemaValue(parameters, args, '')
  const tool = {
    name: options.name,
    description: options.description,
    parameters,
    output: {
      schema: outputSchema,
      /* 🟡 2026-10-15 审计（报告 E 第 11 条）：`options.output.render` 在宿主里是**可选**的，
       *   而这里原来直接 `return userRender(args, value)` —— 省略 render 的工具定义期不报错，
       *   等宿主真去渲染结果时才 `TypeError: userRender is not a function`（爆在离现场很远的地方）。 */
      render(args, value) { return (userRender ?? ((a, v) => v))(args, value) },
    },
    ...(options.timeoutMs !== undefined ? { timeoutMs: options.timeoutMs } : {}),
    async execute(args, exec) {
      const violations = validate(args)
      if (violations.length > 0) throw new BuiltinToolArgsError(violations)
      return userExecute(args, exec)
    },
  }
  return tool
}