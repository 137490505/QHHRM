import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)
const projectRoot = path.resolve(__dirname, '..')
const viewsRoot = path.join(projectRoot, 'src', 'views')
const outputPath = path.join(projectRoot, 'test-artifacts', 'button-analysis.json')

function walkVueFiles(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true })
  const files = []

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name)
    if (entry.isDirectory()) {
      files.push(...walkVueFiles(fullPath))
      continue
    }

    if (entry.isFile() && entry.name.endsWith('.vue')) {
      files.push(fullPath)
    }
  }

  return files
}

function stripTags(value) {
  return value
    .replace(/<template[\s\S]*?<\/template>/g, '')
    .replace(/<[^>]+>/g, ' ')
    .replace(/&nbsp;/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

function normalizeMethodExpression(rawExpression) {
  if (!rawExpression) {
    return null
  }

  const expression = rawExpression.trim()
  const match = expression.match(/^([A-Za-z_$][\w$]*)/)
  return match ? match[1] : expression
}

function extractAttribute(attributes, attributeName) {
  const patterns = [
    new RegExp(`${attributeName}="([^"]+)"`),
    new RegExp(`${attributeName}='([^']+)'`)
  ]

  for (const pattern of patterns) {
    const match = attributes.match(pattern)
    if (match) {
      return match[1]
    }
  }

  return null
}

function inferApiCalls(methodBody) {
  const apiCalls = []
  const apiPattern = /([A-Za-z_$][\w$]*)\.(\w+)\s*\(/g
  let match

  while ((match = apiPattern.exec(methodBody)) !== null) {
    const target = `${match[1]}.${match[2]}`
    if (/(Api|Store)$/.test(match[1])) {
      apiCalls.push(target)
    }
  }

  return [...new Set(apiCalls)]
}

function inferRouteChanges(methodBody) {
  const routes = []
  const routePattern = /router\.(push|replace)\(\s*['"`]([^'"`]+)['"`]/g
  let match

  while ((match = routePattern.exec(methodBody)) !== null) {
    routes.push(`${match[1]}:${match[2]}`)
  }

  return [...new Set(routes)]
}

function extractMethodBodies(scriptContent) {
  const methods = new Map()
  const methodPattern = /const\s+([A-Za-z_$][\w$]*)\s*=\s*(?:async\s*)?\(([\s\S]*?)\)\s*=>\s*\{([\s\S]*?)\n\};/g
  let match

  while ((match = methodPattern.exec(scriptContent)) !== null) {
    methods.set(match[1], {
      args: match[2].trim(),
      body: match[3]
    })
  }

  return methods
}

function analyzeVueFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf8')
  const templateMatch = content.match(/<template>([\s\S]*?)<\/template>/)
  const scriptMatch = content.match(/<script setup>([\s\S]*?)<\/script>/)

  if (!templateMatch || !scriptMatch) {
    return null
  }

  const template = templateMatch[1]
  const script = scriptMatch[1]
  const methods = extractMethodBodies(script)
  const interactiveTagPattern = /<(a-button|a-menu-item|button)\b([^>]*)@click="([^"]+)"([^>]*)>([\s\S]*?)<\/\1>/g
  const buttons = []
  let match

  while ((match = interactiveTagPattern.exec(template)) !== null) {
    const [, tagName, beforeClickAttrs, clickExpression, afterClickAttrs, innerHtml] = match
    const attributes = `${beforeClickAttrs} ${afterClickAttrs}`
    const methodName = normalizeMethodExpression(clickExpression)
    const methodInfo = methodName ? methods.get(methodName) : null
    const rawLabel = stripTags(innerHtml)
    const label = rawLabel || extractAttribute(attributes, 'key') || methodName || '未命名按钮'

    buttons.push({
      tagName,
      label,
      permissionCode: extractAttribute(attributes, 'v-permission'),
      clickExpression: clickExpression.trim(),
      methodName,
      apiCalls: methodInfo ? inferApiCalls(methodInfo.body) : [],
      routeChanges: methodInfo ? inferRouteChanges(methodInfo.body) : [],
      hasAsyncHandler: Boolean(methodInfo),
      sourceSnippet: match[0].replace(/\s+/g, ' ').trim()
    })
  }

  return {
    filePath,
    page: path.relative(viewsRoot, filePath).replace(/\\/g, '/'),
    buttonCount: buttons.length,
    buttons
  }
}

function main() {
  const vueFiles = walkVueFiles(viewsRoot)
  const pages = vueFiles
    .map(analyzeVueFile)
    .filter(Boolean)
    .filter(page => page.buttonCount > 0)
    .sort((a, b) => a.page.localeCompare(b.page, 'zh-CN'))

  const summary = {
    generatedAt: new Date().toISOString(),
    totalPages: pages.length,
    totalButtons: pages.reduce((sum, page) => sum + page.buttonCount, 0),
    pages
  }

  fs.mkdirSync(path.dirname(outputPath), { recursive: true })
  fs.writeFileSync(outputPath, JSON.stringify(summary, null, 2), 'utf8')

  console.log(`分析完成: ${summary.totalPages} 个页面, ${summary.totalButtons} 个按钮`)
  console.log(`输出文件: ${outputPath}`)

  for (const page of pages) {
    console.log(`- ${page.page}: ${page.buttonCount} 个按钮`)
  }
}

main()
